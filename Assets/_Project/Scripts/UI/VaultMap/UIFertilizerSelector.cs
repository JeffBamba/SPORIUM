using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Sporae.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.PlantCard;
using UIButton = UnityEngine.UI.Button;
using UIElements = UnityEngine.UIElements;

namespace _Project
{
    /// <summary>
    /// UI per selezionare un fertilizzante dall'inventario quando si vuole fertilizzare
    /// BLK-03.01-T1: Basato su UISeedSelector
    /// </summary>
    public class UIFertilizerSelector : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject selectorPanel;
        [SerializeField] private Transform fertilizerButtonContainer;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI noFertilizersText;
        
        [Header("Prefab")]
        [SerializeField] private GameObject fertilizerButtonPrefab;
        
        [Header("Settings")]
        [SerializeField] private string titleTextFormat = "Seleziona Fertilizzante";
        [SerializeField] private string noFertilizersMessage = "Nessun fertilizzante disponibile nell'inventario";
        [SerializeField] private int canvasSortingOrder = 200; // Sopra la HUD della pianta (100) e inventario (150)
        [SerializeField] private bool improveReadability = true;
        [SerializeField] private float fertilizerButtonFontSize = 22f;
        
        private GameManager _gameManager;
        private Inventory _playerInventory;
        private PotSlot _targetPot;
        private List<GameObject> _fertilizerButtons = new List<GameObject>();
        
        public event Action<string> OnFertilizerSelected; // fertilizerTypeId
        public event Action OnCancelled;
        
        /// <summary>
        /// Rimuove tutti i subscriber dagli eventi (per permettere a PlantCardV2Controller di sottoscriversi senza conflitti)
        /// </summary>
        public void ClearAllSubscribers()
        {
            OnFertilizerSelected = null;
            OnCancelled = null;
        }
        
        private void Awake()
        {
            // Usa ServiceContainer invece di FindObjectOfType
            _gameManager = ServiceContainer.Instance?.Get<GameManager>();
            if (_gameManager != null)
            {
                _playerInventory = _gameManager.PlayerInventory;
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "GameManager non disponibile via ServiceContainer. Tentativo late binding...");
                if (ServiceContainer.Instance != null)
                {
                    ServiceContainer.Instance.OnServiceRegistered += OnGameManagerRegistered;
                }
            }
            
            // Sottoscrivi all'evento di cambio inventario per aggiornare il pannello se aperto
            if (_playerInventory != null)
            {
                _playerInventory.OnInventoryChanged += OnInventoryChanged;
            }
            
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseClicked);
            }
            
            if (selectorPanel != null)
            {
                selectorPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// Late binding per GameManager quando viene registrato
        /// </summary>
        private void OnGameManagerRegistered(object service)
        {
            if (service is GameManager gameManager && _gameManager == null)
            {
                _gameManager = gameManager;
                _playerInventory = _gameManager.PlayerInventory;
                
                if (_playerInventory != null)
                {
                    _playerInventory.OnInventoryChanged += OnInventoryChanged;
                }
                
                if (ServiceContainer.Instance != null)
                {
                    ServiceContainer.Instance.OnServiceRegistered -= OnGameManagerRegistered;
                }
            }
        }
        
        private void OnDestroy()
        {
            // Cleanup ServiceContainer subscriptions
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered -= OnGameManagerRegistered;
            }
            
            // Cleanup
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseClicked);
            }
            
            // Rimuovi sottoscrizione all'evento inventario
            if (_playerInventory != null)
            {
                _playerInventory.OnInventoryChanged -= OnInventoryChanged;
            }
        }
        
        /// <summary>
        /// Chiamato quando l'inventario cambia - aggiorna il pannello se è aperto
        /// </summary>
        private void OnInventoryChanged()
        {
            // Se il pannello è aperto, aggiorna i pulsanti con le nuove quantità
            if (IsVisible && _targetPot != null)
            {
                RefreshFertilizerButtons();
            }
        }
        
        /// <summary>
        /// Aggiorna i pulsanti fertilizzante con le quantità correnti dall'inventario
        /// </summary>
        private void RefreshFertilizerButtons()
        {
            // Trova tutti i fertilizzanti disponibili nell'inventario (con quantità aggiornate)
            List<InventorySlot> availableFertilizers = GetAvailableFertilizers();
            
            if (availableFertilizers.Count == 0)
            {
                ShowNoFertilizersMessage();
                ClearFertilizerButtons();
                return;
            }
            
            HideNoFertilizersMessage();
            CreateFertilizerButtons(availableFertilizers);
        }
        
        /// <summary>
        /// Mostra il selettore di fertilizzanti per un vaso specifico
        /// </summary>
        public void Show(PotSlot targetPot)
        {
            _targetPot = targetPot;
            
            // Verifica che i riferimenti UI siano assegnati, altrimenti crea automaticamente
            if (selectorPanel == null || fertilizerButtonContainer == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "Riferimenti UI mancanti. Creazione automatica UI...");
                CreateUI();
            }
            
            // Verifica di nuovo dopo la creazione automatica
            if (selectorPanel == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "selectorPanel non assegnato dopo creazione automatica!");
                return;
            }
            
            if (fertilizerButtonContainer == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "fertilizerButtonContainer non assegnato dopo creazione automatica!");
                return;
            }
            
            selectorPanel.SetActive(true);
            
            // DEBUG_SAFE_FIX: Abilita il Canvas quando viene mostrato (come UISeedSelector)
            var canvas = selectorPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                // CRITICAL FIX: Il sorting order funziona solo con ScreenSpaceOverlay o ScreenSpaceCamera
                // Se il Canvas è in WorldSpace, il sorting order non funziona!
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.renderMode != RenderMode.ScreenSpaceCamera)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }
                
                canvas.enabled = true;
            }
            
            // Configura solo il Canvas sorting order
            SetupCanvasSortingOrder();
            
            if (titleText != null)
            {
                titleText.text = titleTextFormat;
            }
            
            // Trova tutti i fertilizzanti disponibili nell'inventario
            List<InventorySlot> availableFertilizers = GetAvailableFertilizers();
            
            if (availableFertilizers.Count == 0)
            {
                ShowNoFertilizersMessage();
                return;
            }
            
            HideNoFertilizersMessage();
            CreateFertilizerButtons(availableFertilizers);
        }
        
        /// <summary>
        /// Nasconde il selettore
        /// </summary>
        public void Hide()
        {
            if (selectorPanel != null)
            {
                selectorPanel.SetActive(false);
                
                // DEBUG_SAFE_FIX: NON disabilitare il Canvas - questo disabilita anche altre HUD!
                // Il problema è che questo canvas è condiviso con altre HUD (MinimalHUD, Condensation button)
                // Disabilitarlo causa la scomparsa di tutti gli elementi HUD
                // Il selectorPanel.SetActive(false) è sufficiente per nascondere il selettore
                // var canvas = selectorPanel.GetComponentInParent<Canvas>();
                // if (canvas != null)
                // {
                //     canvas.enabled = false;
                // }
            }
            
            ClearFertilizerButtons();
            _targetPot = null;
        }
        
        /// <summary>
        /// Ottiene tutti i fertilizzanti disponibili nell'inventario
        /// BLK-03.01-T1: Cerca Items.FertilizerStandard, Items.FertilizerPure, Items.FertilizerProhibited
        /// </summary>
        private List<InventorySlot> GetAvailableFertilizers()
        {
            List<InventorySlot> fertilizers = new List<InventorySlot>();
            
            if (_playerInventory == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "PlayerInventory non trovato!");
                return fertilizers;
            }
            
            // Lista dei TypeId dei fertilizzanti
            string[] fertilizerTypeIds = {
                Items.FertilizerStandard,
                Items.FertilizerPure,
                Items.FertilizerProhibited
            };
            
            foreach (var slot in _playerInventory.Items)
            {
                if (slot.Items.Count > 0)
                {
                    var firstItem = slot.Items.ElementAt(0);
                    string typeId = firstItem.TypeId;
                    
                    // Verifica se è un fertilizzante
                    if (fertilizerTypeIds.Contains(typeId))
                    {
                        fertilizers.Add(slot);
                    }
                }
            }
            
            return fertilizers;
        }
        
        /// <summary>
        /// Crea i pulsanti per ogni fertilizzante disponibile
        /// </summary>
        private void CreateFertilizerButtons(List<InventorySlot> fertilizers)
        {
            ClearFertilizerButtons();
            
            if (fertilizerButtonContainer == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "fertilizerButtonContainer non assegnato!");
                return;
            }
            
            foreach (var fertilizerSlot in fertilizers)
            {
                if (fertilizerSlot.Items.Count == 0)
                    continue;
                
                var fertilizerItem = fertilizerSlot.Items.ElementAt(0);
                string fertilizerTypeId = fertilizerItem.TypeId;
                int quantity = fertilizerSlot.Quantity;
                
                // Crea pulsante
                GameObject buttonGO = CreateFertilizerButton(fertilizerTypeId, quantity);
                if (buttonGO != null)
                {
                    // Migliora leggibilità del pulsante appena creato
                    if (improveReadability)
                    {
                        ImproveFertilizerButtonReadability(buttonGO);
                    }
                    _fertilizerButtons.Add(buttonGO);
                }
            }
        }
        
        /// <summary>
        /// Crea un pulsante per un fertilizzante specifico
        /// </summary>
        private GameObject CreateFertilizerButton(string fertilizerTypeId, int quantity)
        {
            GameObject buttonGO;
            
            if (fertilizerButtonPrefab != null)
            {
                buttonGO = Instantiate(fertilizerButtonPrefab, fertilizerButtonContainer);
            }
            else
            {
                // Crea pulsante di default se prefab non disponibile
                buttonGO = new GameObject($"FertilizerButton_{fertilizerTypeId}");
                buttonGO.transform.SetParent(fertilizerButtonContainer);
                
                // Aggiungi componenti base
                Image image = buttonGO.AddComponent<Image>();
                image.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
                
                Button button = buttonGO.AddComponent<Button>();
                button.targetGraphic = image;
                
                // Aggiungi testo
                GameObject textGO = new GameObject("Text");
                textGO.transform.SetParent(buttonGO.transform);
                RectTransform textRect = textGO.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                textRect.anchoredPosition = Vector2.zero;
                
                TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
                text.text = fertilizerTypeId;
                text.alignment = TextAlignmentOptions.Center;
                text.color = new Color(1f, 1f, 1f, 1f);
                text.fontSize = fertilizerButtonFontSize;
                text.outlineWidth = 0.6f;
                text.outlineColor = new Color(0f, 0f, 0f, 1f);
            }
            
            // Configura pulsante
            Button btn = buttonGO.GetComponent<Button>();
            if (btn != null)
            {
                // Crea testo descrittivo
                string buttonText = GetFertilizerButtonText(fertilizerTypeId, quantity);
                
                // Cerca TextMeshProUGUI nel pulsante
                TextMeshProUGUI buttonTextComponent = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonTextComponent != null)
                {
                    buttonTextComponent.text = buttonText;
                    if (buttonTextComponent.fontSize < fertilizerButtonFontSize)
                    {
                        buttonTextComponent.fontSize = fertilizerButtonFontSize;
                    }
                    buttonTextComponent.color = new Color(1f, 1f, 1f, 1f);
                    buttonTextComponent.outlineWidth = 0.6f;
                    buttonTextComponent.outlineColor = new Color(0f, 0f, 0f, 1f);
                }
                
                // Migliora anche il background del pulsante
                Image btnImage = buttonGO.GetComponent<Image>();
                if (btnImage != null)
                {
                    btnImage.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
                }
                
                // Aggiungi listener
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnFertilizerButtonClicked(fertilizerTypeId));
            }
            
            // Imposta layout
            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
            }
            
            return buttonGO;
        }
        
        /// <summary>
        /// Genera il testo per il pulsante fertilizzante
        /// </summary>
        private string GetFertilizerButtonText(string fertilizerTypeId, int quantity)
        {
            string fertilizerName = fertilizerTypeId switch
            {
                Items.FertilizerStandard => "Fertilizzante Standard",
                Items.FertilizerPure => "Fertilizzante Pure",
                Items.FertilizerProhibited => "Fertilizzante Proibito",
                _ => fertilizerTypeId
            };
            
            // Ottieni informazioni sul fertilizzante
            string info = fertilizerTypeId switch
            {
                Items.FertilizerStandard => "+25% Fertilizzante",
                Items.FertilizerPure => "+40% Fertilizzante",
                Items.FertilizerProhibited => "+40% Fertilizzante",
                _ => ""
            };
            
            return $"{fertilizerName}\n{info}\n(x{quantity})";
        }
        
        /// <summary>
        /// Gestisce il click su un pulsante fertilizzante
        /// </summary>
        private void OnFertilizerButtonClicked(string fertilizerTypeId)
        {
            SporiumLogger.LogDebug(LogCategory.UI, $"Fertilizzante selezionato: {fertilizerTypeId}, TargetPot: {_targetPot?.PotId ?? "NULL"}");
            
            // Verifica che ci sia un target pot
            if (_targetPot == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "TargetPot è NULL! Impossibile applicare fertilizzante.");
                return;
            }
            
            // Emetti evento PRIMA di nascondere il pannello
            SporiumLogger.LogDebug(LogCategory.UI, $"Emettendo evento OnFertilizerSelected per fertilizzante {fertilizerTypeId}");
            OnFertilizerSelected?.Invoke(fertilizerTypeId);
            
            // Nascondi dopo aver emesso l'evento
            Hide();
        }
        
        /// <summary>
        /// Gestisce il click sul pulsante chiudi
        /// </summary>
        private void OnCloseClicked()
        {
            SporiumLogger.LogDebug(LogCategory.UI, "Selezione annullata");
            
            OnCancelled?.Invoke();
            Hide();
        }
        
        /// <summary>
        /// Pulisce tutti i pulsanti fertilizzante creati
        /// </summary>
        private void ClearFertilizerButtons()
        {
            foreach (var button in _fertilizerButtons)
            {
                if (button != null)
                {
                    Destroy(button);
                }
            }
            _fertilizerButtons.Clear();
        }
        
        /// <summary>
        /// Mostra messaggio "nessun fertilizzante disponibile"
        /// </summary>
        private void ShowNoFertilizersMessage()
        {
            if (noFertilizersText != null)
            {
                noFertilizersText.gameObject.SetActive(true);
                noFertilizersText.text = noFertilizersMessage;
            }
        }
        
        /// <summary>
        /// Nasconde il messaggio "nessun fertilizzante disponibile"
        /// </summary>
        private void HideNoFertilizersMessage()
        {
            if (noFertilizersText != null)
            {
                noFertilizersText.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Verifica se il selettore è visibile
        /// </summary>
        public bool IsVisible => selectorPanel != null && selectorPanel.activeSelf;
        
        /// <summary>
        /// Configura il Canvas per renderlo sopra la HUD della pianta
        /// </summary>
        /// <summary>
        /// Imposta solo il sorting order del Canvas (senza modificare colori o dimensioni)
        /// </summary>
        private void SetupCanvasSortingOrder()
        {
            if (selectorPanel == null) return;
            
            // Trova il Canvas del selettore
            Canvas selectorCanvas = selectorPanel.GetComponentInParent<Canvas>();
            if (selectorCanvas == null)
            {
                selectorCanvas = selectorPanel.GetComponent<Canvas>();
                if (selectorCanvas == null)
                {
                    Transform parent = selectorPanel.transform.parent;
                    while (parent != null && selectorCanvas == null)
                    {
                        selectorCanvas = parent.GetComponent<Canvas>();
                        parent = parent.parent;
                    }
                }
            }
            
            // Se trovato, imposta sorting order più alto (come UISeedSelector)
            if (selectorCanvas != null)
            {
                selectorCanvas.sortingOrder = canvasSortingOrder;
                SporiumLogger.LogDebug(LogCategory.UI, $"Canvas sorting order impostato a {canvasSortingOrder} per renderlo sopra la HUD della pianta");
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "Canvas non trovato per UIFertilizerSelector. Il selettore potrebbe non essere visibile sopra la HUD del POT.");
            }
        }
        
        /// <summary>
        /// Migliora la leggibilità di un singolo pulsante fertilizzante
        /// </summary>
        private void ImproveFertilizerButtonReadability(GameObject buttonGO)
        {
            if (buttonGO == null) return;
            
            // Migliora tutti i testi nel pulsante
            TextMeshProUGUI[] texts = buttonGO.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                if (text != null)
                {
                    if (text.fontSize < fertilizerButtonFontSize)
                    {
                        text.fontSize = fertilizerButtonFontSize;
                    }
                    
                    text.color = new Color(1f, 1f, 1f, 1f);
                    
                    if (text.fontMaterial != null)
                    {
                        text.outlineWidth = 0.6f;
                        text.outlineColor = new Color(0f, 0f, 0f, 1f);
                    }
                }
            }
            
            // Migliora il background del pulsante
            Image buttonImage = buttonGO.GetComponent<Image>();
            if (buttonImage != null)
            {
                Color bgColor = buttonImage.color;
                bgColor.a = 0.95f;
                bgColor.r = Mathf.Min(bgColor.r, 0.2f);
                bgColor.g = Mathf.Min(bgColor.g, 0.2f);
                bgColor.b = Mathf.Min(bgColor.b, 0.2f);
                buttonImage.color = bgColor;
            }
        }
        
        /// <summary>
        /// Crea automaticamente la struttura UI se mancante
        /// BLK-03.01-T1: Basato su UISeedSelector.CreateUI()
        /// </summary>
        private void CreateUI()
        {
            // Trova o crea Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas_FertilizerSelector");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = canvasSortingOrder; // Imposta sorting order alto (come UISeedSelector)
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                SporiumLogger.LogInfo(LogCategory.UI, $"Creato Canvas per UIFertilizerSelector con sorting order {canvasSortingOrder}");
            }
            else
            {
                // Se esiste già un Canvas, assicurati che abbia sorting order alto (come UISeedSelector)
                if (canvas.sortingOrder < canvasSortingOrder)
                {
                    canvas.sortingOrder = canvasSortingOrder;
                    SporiumLogger.LogDebug(LogCategory.UI, $"Canvas esistente aggiornato con sorting order {canvasSortingOrder}");
                }
            }
            
            // Assicurati che questo GameObject sia figlio del Canvas
            if (transform.parent == null || transform.parent.GetComponent<Canvas>() == null)
            {
                transform.SetParent(canvas.transform, false);
            }
            
            // Crea Panel principale
            GameObject panelGO = new GameObject("SelectorPanel");
            panelGO.transform.SetParent(transform, false);
            
            RectTransform panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1400, 1000);
            panelRect.anchoredPosition = Vector2.zero;
            
            Image panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0.05f, 0.05f, 0.05f, 0.98f);
            
            selectorPanel = panelGO;
            
            // Crea Container per pulsanti fertilizzanti
            GameObject containerGO = new GameObject("FertilizerButtonContainer");
            containerGO.transform.SetParent(panelGO.transform, false);
            
            RectTransform containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0f, 0f);
            containerRect.anchorMax = new Vector2(1f, 1f);
            containerRect.offsetMin = new Vector2(30, 120);
            containerRect.offsetMax = new Vector2(-30, -30);
            
            // Aggiungi GridLayoutGroup per organizzare i pulsanti
            GridLayoutGroup gridLayout = containerGO.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(400, 300);
            gridLayout.spacing = new Vector2(40, 40);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            
            fertilizerButtonContainer = containerGO.transform;
            
            // Crea Title
            GameObject titleGO = new GameObject("Title");
            titleGO.transform.SetParent(panelGO.transform, false);
            
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.sizeDelta = new Vector2(0, 100);
            titleRect.anchoredPosition = new Vector2(0, -20);
            
            TextMeshProUGUI titleTextComponent = titleGO.AddComponent<TextMeshProUGUI>();
            titleTextComponent.text = titleTextFormat;
            titleTextComponent.alignment = TextAlignmentOptions.Center;
            titleTextComponent.fontSize = 56;
            titleTextComponent.color = new Color(1f, 1f, 1f, 1f);
            titleTextComponent.outlineWidth = 0.8f;
            titleTextComponent.outlineColor = new Color(0f, 0f, 0f, 1f);
            
            titleText = titleTextComponent;
            
            // Crea No Fertilizers Text
            GameObject noFertilizersGO = new GameObject("NoFertilizersText");
            noFertilizersGO.transform.SetParent(panelGO.transform, false);
            
            RectTransform noFertilizersRect = noFertilizersGO.AddComponent<RectTransform>();
            noFertilizersRect.anchorMin = new Vector2(0.5f, 0.5f);
            noFertilizersRect.anchorMax = new Vector2(0.5f, 0.5f);
            noFertilizersRect.sizeDelta = new Vector2(600, 100);
            noFertilizersRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI noFertilizersTextComponent = noFertilizersGO.AddComponent<TextMeshProUGUI>();
            noFertilizersTextComponent.text = noFertilizersMessage;
            noFertilizersTextComponent.alignment = TextAlignmentOptions.Center;
            noFertilizersTextComponent.fontSize = 32;
            noFertilizersTextComponent.color = Color.yellow;
            noFertilizersGO.SetActive(false);
            
            noFertilizersText = noFertilizersTextComponent;
            
            // Crea Close Button
            GameObject closeButtonGO = new GameObject("CloseButton");
            closeButtonGO.transform.SetParent(panelGO.transform, false);
            
            RectTransform closeButtonRect = closeButtonGO.AddComponent<RectTransform>();
            closeButtonRect.anchorMin = new Vector2(1f, 1f);
            closeButtonRect.anchorMax = new Vector2(1f, 1f);
            closeButtonRect.sizeDelta = new Vector2(60, 60);
            closeButtonRect.anchoredPosition = new Vector2(-10, -10);
            
            Image closeButtonImage = closeButtonGO.AddComponent<Image>();
            closeButtonImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            
            Button closeButtonComponent = closeButtonGO.AddComponent<Button>();
            closeButtonComponent.targetGraphic = closeButtonImage;
            
            // Testo X sul pulsante chiudi
            GameObject closeTextGO = new GameObject("Text");
            closeTextGO.transform.SetParent(closeButtonGO.transform, false);
            
            RectTransform closeTextRect = closeTextGO.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.sizeDelta = Vector2.zero;
            closeTextRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI closeText = closeTextGO.AddComponent<TextMeshProUGUI>();
            closeText.text = "X";
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.fontSize = 36;
            closeText.color = Color.white;
            
            closeButton = closeButtonComponent;
            
            // Sottoscrivi al click del pulsante chiudi
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(OnCloseClicked);
            }
            
            // Imposta il panel come non attivo inizialmente
            selectorPanel.SetActive(false);
            
            SporiumLogger.LogInfo(LogCategory.UI, "UI creata automaticamente con successo!");
        }
    }
}

