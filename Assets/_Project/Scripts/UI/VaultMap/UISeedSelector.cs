using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Sporae.Core;
using Sporae.Dome.PotSystem.Growth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        
        private GameManager _gameManager;
        private Inventory _playerInventory;
        private PotSlot _targetPot;
        private List<GameObject> _seedButtons = new List<GameObject>();
        
        public event Action<string> OnSeedSelected; // seedTypeId
        public event Action OnCancelled;
        
        private void Awake()
        {
            _gameManager = FindObjectOfType<GameManager>();
            if (_gameManager != null)
            {
                _playerInventory = _gameManager.PlayerInventory;
                
                // Sottoscrivi all'evento di cambio inventario per aggiornare il pannello se aperto
                if (_playerInventory != null)
                {
                    _playerInventory.OnInventoryChanged += OnInventoryChanged;
                }
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
        
        private void OnDestroy()
        {
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
            
            // Se la UI non è stata creata, creala automaticamente
            if (selectorPanel == null)
            {
                Debug.LogWarning("[UISeedSelector] selectorPanel non assegnato. Creazione automatica UI...");
                CreateUI();
                
                // Se ancora null dopo la creazione, c'è un problema
                if (selectorPanel == null)
                {
                    Debug.LogError("[UISeedSelector] Impossibile creare la UI automaticamente!");
                    return;
                }
            }
            
            selectorPanel.SetActive(true);
            
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
                Debug.LogWarning("[UISeedSelector] PlayerInventory non trovato!");
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
                Debug.LogError("[UISeedSelector] seedButtonContainer non assegnato!");
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
                image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                
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
                text.color = Color.white;
                text.fontSize = 14;
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
            string baseText = seedTypeId.Replace("seed-", "Seme ").ToUpper();
            
            if (plantData != null)
            {
                string familyName = plantData.Family switch
                {
                    PlantFamily.Standard => "Standard",
                    PlantFamily.Pure => "Pure",
                    PlantFamily.Evil => "Evil",
                    _ => "Unknown"
                };
                
                return $"{baseText}\n{familyName} (x{quantity})\npH: {plantData.DailyPhDrift:+#;-#;0}/giorno";
            }
            
            return $"{baseText}\n(x{quantity})";
        }
        
        /// <summary>
        /// Gestisce il click su un pulsante seme
        /// </summary>
        private void OnSeedButtonClicked(string seedTypeId)
        {
            Debug.Log($"[UISeedSelector] Seme selezionato: {seedTypeId}");
            
            OnSeedSelected?.Invoke(seedTypeId);
            Hide();
        }
        
        /// <summary>
        /// Gestisce il click sul pulsante chiudi
        /// </summary>
        private void OnCloseClicked()
        {
            Debug.Log("[UISeedSelector] Selezione annullata");
            
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
        /// Crea automaticamente la struttura UI se mancante
        /// </summary>
        private void CreateUI()
        {
            // Trova o crea Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                Debug.Log("[UISeedSelector] Creato Canvas per UISeedSelector");
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
            panelRect.sizeDelta = new Vector2(400, 300);
            panelRect.anchoredPosition = Vector2.zero;
            
            Image panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            
            selectorPanel = panelGO;
            
            // Crea Container per pulsanti semi
            GameObject containerGO = new GameObject("SeedButtonContainer");
            containerGO.transform.SetParent(panelGO.transform, false);
            
            RectTransform containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0f, 0f);
            containerRect.anchorMax = new Vector2(1f, 1f);
            containerRect.offsetMin = new Vector2(10, 50);
            containerRect.offsetMax = new Vector2(-10, -10);
            
            // Aggiungi GridLayoutGroup per organizzare i pulsanti
            GridLayoutGroup gridLayout = containerGO.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(120, 100);
            gridLayout.spacing = new Vector2(10, 10);
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
            titleRect.sizeDelta = new Vector2(0, 40);
            titleRect.anchoredPosition = new Vector2(0, -5);
            
            TextMeshProUGUI titleTextComponent = titleGO.AddComponent<TextMeshProUGUI>();
            titleTextComponent.text = titleTextFormat;
            titleTextComponent.alignment = TextAlignmentOptions.Center;
            titleTextComponent.fontSize = 20;
            titleTextComponent.color = Color.white;
            
            titleText = titleTextComponent;
            
            // Crea No Seeds Text
            GameObject noSeedsGO = new GameObject("NoSeedsText");
            noSeedsGO.transform.SetParent(panelGO.transform, false);
            
            RectTransform noSeedsRect = noSeedsGO.AddComponent<RectTransform>();
            noSeedsRect.anchorMin = new Vector2(0.5f, 0.5f);
            noSeedsRect.anchorMax = new Vector2(0.5f, 0.5f);
            noSeedsRect.sizeDelta = new Vector2(300, 50);
            noSeedsRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI noSeedsTextComponent = noSeedsGO.AddComponent<TextMeshProUGUI>();
            noSeedsTextComponent.text = noSeedsMessage;
            noSeedsTextComponent.alignment = TextAlignmentOptions.Center;
            noSeedsTextComponent.fontSize = 16;
            noSeedsTextComponent.color = Color.yellow;
            noSeedsGO.SetActive(false);
            
            noSeedsText = noSeedsTextComponent;
            
            // Crea Close Button
            GameObject closeButtonGO = new GameObject("CloseButton");
            closeButtonGO.transform.SetParent(panelGO.transform, false);
            
            RectTransform closeButtonRect = closeButtonGO.AddComponent<RectTransform>();
            closeButtonRect.anchorMin = new Vector2(1f, 1f);
            closeButtonRect.anchorMax = new Vector2(1f, 1f);
            closeButtonRect.sizeDelta = new Vector2(30, 30);
            closeButtonRect.anchoredPosition = new Vector2(-5, -5);
            
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
            closeText.fontSize = 18;
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
            
            Debug.Log("[UISeedSelector] UI creata automaticamente con successo!");
        }
    }
}

