using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Sporae.Core;
using Sporae.Dome.PotSystem.Growth;
using Sporae.UI.UIToolkit.SeedInventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sporae.DevTools;

namespace _Project
{
    /// <summary>
    /// UI per selezionare un seme dall'inventario quando si vuole piantare
    /// </summary>
    public class UISeedSelector : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject selectorPanel;
        [SerializeField] private Transform seedButtonContainer;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI noSeedsText;
        
        [Header("Prefab")]
        [SerializeField] private GameObject seedButtonPrefab;
        
        [Header("Settings")]
        [SerializeField] private string titleTextFormat = "Seleziona Seme";
        [SerializeField] private string noSeedsMessage = "Nessun seme disponibile nell'inventario";
        [SerializeField] private int canvasSortingOrder = 200; // Sopra la HUD della pianta (100) e inventario (150)
        [SerializeField] private bool improveReadability = true;
        [SerializeField] private float seedButtonFontSize = 22f; // Dimensione font per i pulsanti semi (modificabile dall'Inspector)
        
        private GameManager _gameManager;
        private Inventory _playerInventory;
        private PotSlot _targetPot;
        private List<GameObject> _seedButtons = new List<GameObject>();
        
        public event Action<string> OnSeedSelected; // seedTypeId
        public event Action OnCancelled;
        
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
                RefreshSeedButtons();
            }
        }
        
        /// <summary>
        /// Aggiorna i pulsanti semi con le quantità correnti dall'inventario
        /// </summary>
        private void RefreshSeedButtons()
        {
            // Trova tutti i semi disponibili nell'inventario (con quantità aggiornate)
            List<InventorySlot> availableSeeds = GetAvailableSeeds();
            
            if (availableSeeds.Count == 0)
            {
                ShowNoSeedsMessage();
                ClearSeedButtons();
                return;
            }
            
            HideNoSeedsMessage();
            CreateSeedButtons(availableSeeds);
        }
        
        /// <summary>
        /// Mostra il selettore di semi per un vaso specifico
        /// </summary>
        public void Show(PotSlot targetPot)
        {
            _targetPot = targetPot;
            
            // Verifica che i riferimenti UI siano assegnati
            if (selectorPanel == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "selectorPanel non assegnato! " +
                    "Devi assegnare il GameObject del pannello principale nell'Inspector. " +
                    "Vedi le istruzioni in Assets/Docs/UISeedSelector_Setup.md");
                return;
            }
            
            if (seedButtonContainer == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "seedButtonContainer non assegnato! " +
                    "Devi assegnare il Transform del container per i pulsanti semi nell'Inspector.");
                return;
            }
            
            selectorPanel.SetActive(true);
            
            // DEBUG_SAFE_FIX: Abilita il Canvas quando viene mostrato
            var canvas = selectorPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = true;
            }
            
            // Configura solo il Canvas sorting order (non modifica colori o dimensioni)
            // Le modifiche visive devono essere fatte manualmente nella scena Unity
            SetupCanvasSortingOrder();
            
            if (titleText != null)
            {
                titleText.text = titleTextFormat;
            }
            
            // Trova tutti i semi disponibili nell'inventario
            List<InventorySlot> availableSeeds = GetAvailableSeeds();
            
            if (availableSeeds.Count == 0)
            {
                ShowNoSeedsMessage();
                return;
            }
            
            HideNoSeedsMessage();
            CreateSeedButtons(availableSeeds);
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
            
            ClearSeedButtons();
            _targetPot = null;
        }
        
        /// <summary>
        /// Ottiene tutti i semi disponibili nell'inventario
        /// </summary>
        private List<InventorySlot> GetAvailableSeeds()
        {
            List<InventorySlot> seeds = new List<InventorySlot>();
            
            if (_playerInventory == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "PlayerInventory non trovato!");
                return seeds;
            }
            
            foreach (var slot in _playerInventory.Items)
            {
                if (slot.Items.Count > 0)
                {
                    var firstItem = slot.Items.ElementAt(0);
                    if (firstItem.ItemConfig != null && firstItem.ItemConfig.IsSeed)
                    {
                        seeds.Add(slot);
                    }
                }
            }
            
            return seeds;
        }
        
        /// <summary>
        /// Crea i pulsanti per ogni seme disponibile
        /// </summary>
        private void CreateSeedButtons(List<InventorySlot> seeds)
        {
            ClearSeedButtons();
            
            if (seedButtonContainer == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "seedButtonContainer non assegnato!");
                return;
            }
            
            foreach (var seedSlot in seeds)
            {
                if (seedSlot.Items.Count == 0)
                    continue;
                
                var seedItem = seedSlot.Items.ElementAt(0);
                string seedTypeId = seedItem.TypeId;
                int quantity = seedSlot.Quantity;
                
                // Cerca PlantData per mostrare informazioni aggiuntive
                PlantData plantData = PlantDatabase.Instance?.GetPlantDataBySeedTypeId(seedTypeId);
                
                // Crea pulsante
                GameObject buttonGO = CreateSeedButton(seedTypeId, quantity, plantData);
                if (buttonGO != null)
                {
                    // Migliora leggibilità del pulsante appena creato
                    if (improveReadability)
                    {
                        ImproveSeedButtonReadability(buttonGO);
                    }
                    _seedButtons.Add(buttonGO);
                }
            }
        }
        
        /// <summary>
        /// Crea un pulsante per un seme specifico
        /// </summary>
        private GameObject CreateSeedButton(string seedTypeId, int quantity, PlantData plantData)
        {
            GameObject buttonGO;
            
            if (seedButtonPrefab != null)
            {
                buttonGO = Instantiate(seedButtonPrefab, seedButtonContainer);
            }
            else
            {
                // Crea pulsante di default se prefab non disponibile
                buttonGO = new GameObject($"SeedButton_{seedTypeId}");
                buttonGO.transform.SetParent(seedButtonContainer);
                
                // Aggiungi componenti base
                Image image = buttonGO.AddComponent<Image>();
                image.color = new Color(0.15f, 0.15f, 0.15f, 0.95f); // Sfondo più scuro e opaco
                
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
                text.text = seedTypeId;
                text.alignment = TextAlignmentOptions.Center;
                text.color = new Color(1f, 1f, 1f, 1f); // Bianco puro
                text.fontSize = seedButtonFontSize; // Font configurabile dall'Inspector
                text.outlineWidth = 0.6f; // Outline scalato del 100%
                text.outlineColor = new Color(0f, 0f, 0f, 1f);
            }
            
            // Configura pulsante
            Button btn = buttonGO.GetComponent<Button>();
            if (btn != null)
            {
                // Crea testo descrittivo
                string buttonText = GetSeedButtonText(seedTypeId, quantity, plantData);
                
                // Cerca TextMeshProUGUI nel pulsante
                TextMeshProUGUI buttonTextComponent = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonTextComponent != null)
                {
                    buttonTextComponent.text = buttonText;
                // Migliora leggibilità del testo
                if (buttonTextComponent.fontSize < seedButtonFontSize)
                {
                    buttonTextComponent.fontSize = seedButtonFontSize; // Font configurabile dall'Inspector
                }
                buttonTextComponent.color = new Color(1f, 1f, 1f, 1f); // Bianco puro
                buttonTextComponent.outlineWidth = 0.6f; // Outline scalato del 100%
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
                btn.onClick.AddListener(() => OnSeedButtonClicked(seedTypeId));
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
        /// Genera il testo per il pulsante seme
        /// </summary>
        private string GetSeedButtonText(string seedTypeId, int quantity, PlantData plantData)
        {
            // Usa il nome leggibile del seed invece del codice
            string seedDisplayName = SeedInventoryMenu.GetSeedDisplayName(seedTypeId);
            
            if (plantData != null)
            {
                string familyName = plantData.Family switch
                {
                    PlantFamily.Standard => "Standard",
                    PlantFamily.Pure => "Pure",
                    PlantFamily.Evil => "Evil",
                    _ => "Unknown"
                };
                
                return $"{seedDisplayName}\n{familyName} (x{quantity})\npH: {plantData.DailyPhDrift:+#;-#;0}/giorno";
            }
            
            return $"{seedDisplayName}\n(x{quantity})";
        }
        
        /// <summary>
        /// Gestisce il click su un pulsante seme
        /// </summary>
        private void OnSeedButtonClicked(string seedTypeId)
        {
            SporiumLogger.LogDebug(LogCategory.UI, $"Seme selezionato: {seedTypeId}, TargetPot: {_targetPot?.PotId ?? "NULL"}");
            
            // Verifica che ci sia un target pot
            if (_targetPot == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "TargetPot è NULL! Impossibile piantare seme.");
                return;
            }
            
            // Emetti evento PRIMA di nascondere il pannello
            int subscriberCount = OnSeedSelected?.GetInvocationList()?.Length ?? 0;
            SporiumLogger.LogDebug(LogCategory.UI, $"[UISeedSelector] Emettendo evento OnSeedSelected per seme {seedTypeId} - Numero sottoscrittori: {subscriberCount}");
            OnSeedSelected?.Invoke(seedTypeId);
            SporiumLogger.LogDebug(LogCategory.UI, $"[UISeedSelector] Evento OnSeedSelected emesso per seme {seedTypeId}");
            
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
        /// Pulisce tutti i pulsanti seme creati
        /// </summary>
        private void ClearSeedButtons()
        {
            foreach (var button in _seedButtons)
            {
                if (button != null)
                {
                    Destroy(button);
                }
            }
            _seedButtons.Clear();
        }
        
        /// <summary>
        /// Mostra messaggio "nessun seme disponibile"
        /// </summary>
        private void ShowNoSeedsMessage()
        {
            if (noSeedsText != null)
            {
                noSeedsText.gameObject.SetActive(true);
                noSeedsText.text = noSeedsMessage;
            }
        }
        
        /// <summary>
        /// Nasconde il messaggio "nessun seme disponibile"
        /// </summary>
        private void HideNoSeedsMessage()
        {
            if (noSeedsText != null)
            {
                noSeedsText.gameObject.SetActive(false);
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
            
            // Se trovato, imposta sorting order più alto
            if (selectorCanvas != null)
            {
                selectorCanvas.sortingOrder = canvasSortingOrder;
                SporiumLogger.LogDebug(LogCategory.UI, $"Canvas sorting order impostato a {canvasSortingOrder} per renderlo sopra la HUD della pianta");
            }
        }
        
        [System.Obsolete("Usa SetupCanvasSortingOrder() invece. Le modifiche visive devono essere fatte manualmente nella scena Unity.")]
        private void SetupCanvasAndReadability()
        {
            SetupCanvasSortingOrder();
            // NON chiamare più ImprovePanelReadability() - interferisce con la personalizzazione manuale
        }
        
        /// <summary>
        /// Migliora la leggibilità del pannello e dei pulsanti
        /// </summary>
        private void ImprovePanelReadability()
        {
            if (selectorPanel == null) return;
            
            // NON modificare il colore del pannello - usa quello impostato manualmente nella scena
            // Il colore del pannello deve essere personalizzabile dall'utente
            
            // NON modificare automaticamente titolo, dimensioni o altri elementi UI
            // Tutti gli aspetti visivi devono essere personalizzabili dall'utente nella scena Unity
            // Le modifiche automatiche interferiscono con la personalizzazione manuale
            
            // Migliora solo i pulsanti dei semi se improveReadability è attivo
            // (questi vengono creati dinamicamente, quindi è accettabile modificarli)
            if (improveReadability && seedButtonContainer != null)
            {
                foreach (Transform child in seedButtonContainer)
                {
                    ImproveSeedButtonReadability(child.gameObject);
                }
            }
        }
        
        /// <summary>
        /// Migliora la leggibilità di un singolo pulsante seme
        /// </summary>
        private void ImproveSeedButtonReadability(GameObject buttonGO)
        {
            if (buttonGO == null) return;
            
            // Migliora tutti i testi nel pulsante
            TextMeshProUGUI[] texts = buttonGO.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                if (text != null)
                {
                    // Aumenta font size se troppo piccolo
                    if (text.fontSize < seedButtonFontSize)
                    {
                        text.fontSize = seedButtonFontSize; // Font configurabile dall'Inspector
                    }
                    
                    // Colore bianco brillante per massimo contrasto
                    text.color = new Color(1f, 1f, 1f, 1f);
                    
                    // Aggiungi outline più spesso per maggiore leggibilità
                    if (text.fontMaterial != null)
                    {
                        text.outlineWidth = 0.6f; // Outline scalato del 100%
                        text.outlineColor = new Color(0f, 0f, 0f, 1f);
                    }
                }
            }
            
            // Migliora il background del pulsante
            Image buttonImage = buttonGO.GetComponent<Image>();
            if (buttonImage != null)
            {
                // Sfondo più scuro e opaco
                Color bgColor = buttonImage.color;
                bgColor.a = 0.95f;
                bgColor.r = Mathf.Min(bgColor.r, 0.2f);
                bgColor.g = Mathf.Min(bgColor.g, 0.2f);
                bgColor.b = Mathf.Min(bgColor.b, 0.2f);
                buttonImage.color = bgColor;
            }
        }
        
        /// <summary>
        /// [DEPRECATO] Crea automaticamente la struttura UI se mancante
        /// Questo metodo non viene più chiamato - la UI deve essere creata manualmente nella scena Unity.
        /// Vedi Assets/Docs/UISeedSelector_Setup.md per le istruzioni.
        /// </summary>
        [System.Obsolete("La UI deve essere creata manualmente nella scena Unity. Vedi Assets/Docs/UISeedSelector_Setup.md")]
        private void CreateUI()
        {
            // Trova o crea Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas_SeedSelector");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = canvasSortingOrder; // Imposta sorting order alto
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                SporiumLogger.LogInfo(LogCategory.UI, $"Creato Canvas per UISeedSelector con sorting order {canvasSortingOrder}");
            }
            else
            {
                // Se esiste già un Canvas, assicurati che abbia sorting order alto
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
            panelRect.sizeDelta = new Vector2(1400, 1000); // Pannello scalato del 100% (raddoppiato)
            panelRect.anchoredPosition = Vector2.zero;
            
            Image panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0.05f, 0.05f, 0.05f, 0.98f); // Sfondo molto scuro e opaco per massimo contrasto
            
            selectorPanel = panelGO;
            
            // Crea Container per pulsanti semi
            GameObject containerGO = new GameObject("SeedButtonContainer");
            containerGO.transform.SetParent(panelGO.transform, false);
            
            RectTransform containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0f, 0f);
            containerRect.anchorMax = new Vector2(1f, 1f);
            containerRect.offsetMin = new Vector2(30, 120); // Padding scalato del 100%
            containerRect.offsetMax = new Vector2(-30, -30);
            
            // Aggiungi GridLayoutGroup per organizzare i pulsanti
            GridLayoutGroup gridLayout = containerGO.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(400, 300); // Pulsanti scalati del 100% (raddoppiati)
            gridLayout.spacing = new Vector2(40, 40); // Spazio tra pulsanti scalato del 100%
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            
            seedButtonContainer = containerGO.transform;
            
            // Crea Title
            GameObject titleGO = new GameObject("Title");
            titleGO.transform.SetParent(panelGO.transform, false);
            
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.sizeDelta = new Vector2(0, 100); // Area titolo scalata del 100%
            titleRect.anchoredPosition = new Vector2(0, -20);
            
            TextMeshProUGUI titleTextComponent = titleGO.AddComponent<TextMeshProUGUI>();
            titleTextComponent.text = titleTextFormat;
            titleTextComponent.alignment = TextAlignmentOptions.Center;
            titleTextComponent.fontSize = 56; // Font scalato del 100% (raddoppiato)
            titleTextComponent.color = new Color(1f, 1f, 1f, 1f); // Bianco puro
            // Aggiungi outline più spesso per maggiore leggibilità
            titleTextComponent.outlineWidth = 0.8f; // Outline scalato del 100%
            titleTextComponent.outlineColor = new Color(0f, 0f, 0f, 1f);
            
            titleText = titleTextComponent;
            
            // Crea No Seeds Text
            GameObject noSeedsGO = new GameObject("NoSeedsText");
            noSeedsGO.transform.SetParent(panelGO.transform, false);
            
            RectTransform noSeedsRect = noSeedsGO.AddComponent<RectTransform>();
            noSeedsRect.anchorMin = new Vector2(0.5f, 0.5f);
            noSeedsRect.anchorMax = new Vector2(0.5f, 0.5f);
            noSeedsRect.sizeDelta = new Vector2(600, 100); // Scalato del 100%
            noSeedsRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI noSeedsTextComponent = noSeedsGO.AddComponent<TextMeshProUGUI>();
            noSeedsTextComponent.text = noSeedsMessage;
            noSeedsTextComponent.alignment = TextAlignmentOptions.Center;
            noSeedsTextComponent.fontSize = 32; // Font scalato del 100%
            noSeedsTextComponent.color = Color.yellow;
            noSeedsGO.SetActive(false);
            
            noSeedsText = noSeedsTextComponent;
            
            // Crea Close Button
            GameObject closeButtonGO = new GameObject("CloseButton");
            closeButtonGO.transform.SetParent(panelGO.transform, false);
            
            RectTransform closeButtonRect = closeButtonGO.AddComponent<RectTransform>();
            closeButtonRect.anchorMin = new Vector2(1f, 1f);
            closeButtonRect.anchorMax = new Vector2(1f, 1f);
            closeButtonRect.sizeDelta = new Vector2(60, 60); // Pulsante chiudi scalato del 100%
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
            closeText.fontSize = 36; // Font scalato del 100%
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

