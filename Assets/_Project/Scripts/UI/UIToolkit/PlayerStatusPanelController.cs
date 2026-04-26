using UnityEngine;
using UnityEngine.UIElements;
using Sporae.UI.UIToolkit;
using Sporae.UI.UIToolkit.PlayerInventory;
using Sporae.Core.Localization;
using Sporae.DevTools;
using System.Linq;
using _Project;
using _Project.Sporae.Core;

namespace Sporae.UI.UIToolkit
{
    /// <summary>
    /// Controller principale per il Player Status Panel.
    /// Gestisce Health, Energy e Hydration bars con dati mock per ora.
    /// </summary>
    public class PlayerStatusPanelController : MonoBehaviour
    {
        [Header("UI Toolkit References")]
        [SerializeField] private UIDocument _uiDocument;
        
        [Header("Mock Data (for testing)")]
        [SerializeField] private float _mockHealth = 85f;
        [SerializeField] private float _mockMaxHealth = 100f;
        [SerializeField] private float _mockEnergy = 62f;
        [SerializeField] private float _mockMaxEnergy = 100f;
        [SerializeField] private float _mockHydration = 45f;
        [SerializeField] private float _mockMaxHydration = 100f;
        
        [Header("Configuration")]
        [SerializeField] private StatBarThresholds _thresholds = new StatBarThresholds();
        [SerializeField] private bool _enableDebugLogs = false;
        
        [Header("External References")]
        [Tooltip("Componente unico inventario (INV). Se assegnato, il tasto Inventario apre questo pannello.")]
        [SerializeField] private PlayerInventoryPanelController _playerInventoryPanel;
        [Tooltip("Fallback se PlayerInventoryPanel non assegnato (legacy).")]
        [SerializeField] private HUDInventory _hudInventory;
        
        // UI Elements
        private VisualElement _root;
        private SegmentedStatBarController _hydrationBar;
        private Label _lowTextLabel;
        private Label _warningLabel;
        private Button _inventoryButton;
        private Button _reputationButton;
        private Button _diaryButton;
        
        // Mock data update (per test) - disattivato quando collegato a PlayerHydrationSystem
        private float _mockUpdateTimer = 0f;
        private const float MOCK_UPDATE_INTERVAL = 2f;
        private bool _hydrationConnectedToRealSystem;

        private void Awake()
        {
            // Crea UIDocument se non presente
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
                if (_uiDocument == null)
                {
                    _uiDocument = gameObject.AddComponent<UIDocument>();
                    if (_enableDebugLogs)
                        SporiumLogger.LogWarning(LogCategory.UI, "UIDocument creato automaticamente su PlayerStatusPanelController");
                }
            }
            
            // DEBUG_SAFE_FIX: Imposta sortingOrder per HUD base (sotto PlantCard, sopra background)
            if (_uiDocument != null)
                _uiDocument.sortingOrder = 50;
        }
        
        private void Start()
        {
            InitializeUI();
            InitializeBars();
            TryConnectHydrationSystem();
            UpdateAllBars();
        }

        private void OnEnable()
        {
            GameLanguageSettings.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            GameLanguageSettings.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(GameLanguage _)
        {
            InitializeBars();
        }

        private void TryConnectHydrationSystem()
        {
            if (_hydrationConnectedToRealSystem) return;
            var gm = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            var hydration = gm?.PlayerHydrationSystem;
            if (hydration != null)
            {
                hydration.OnHydrationChanged += OnHydrationChanged;
                _hydrationConnectedToRealSystem = true;
                UpdateHydrationFromSystem(hydration);
                if (_enableDebugLogs)
                    SporiumLogger.LogInfo(LogCategory.UI, "PlayerStatusPanel: collegato a PlayerHydrationSystem");
            }
        }

        private void UpdateHydrationFromSystem(PlayerHydrationSystem hydration)
        {
            UpdateHydration(hydration.HydrationPercent, 100f);
            ApplyHydrationMovementWarning(hydration);
        }

        private void OnHydrationChanged(float current, float max)
        {
            UpdateHydration(current, max);
            var hydration = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true)?.PlayerHydrationSystem;
            if (hydration != null)
                ApplyHydrationMovementWarning(hydration);
        }

        private void ApplyHydrationMovementWarning(PlayerHydrationSystem hydration)
        {
            if (_warningLabel == null) return;

            float h = hydration.HydrationPercent;
            if (h > 50f)
                _warningLabel.text = "";
            else if (h > 25f)
                _warningLabel.text = "Sotto il 50% H: leggera riduzione velocità di movimento";
            else
                _warningLabel.text = "Sotto il 25% H: velocità molto ridotta; rischio game over se resti a 0% H";

            _warningLabel.style.display = string.IsNullOrEmpty(_warningLabel.text) ? DisplayStyle.None : DisplayStyle.Flex;
            if (_warningLabel.style.display == DisplayStyle.Flex)
            {
                _warningLabel.style.height = StyleKeyword.Auto;
                _warningLabel.style.overflow = Overflow.Visible;
            }
            else
            {
                _warningLabel.style.height = 0f;
                _warningLabel.style.overflow = Overflow.Hidden;
            }
        }
        
        private void Update()
        {
            if (_hydrationConnectedToRealSystem) return;
            _mockUpdateTimer += Time.deltaTime;
            if (_mockUpdateTimer >= MOCK_UPDATE_INTERVAL)
            {
                _mockUpdateTimer = 0f;
                _mockHealth = Mathf.Clamp(_mockHealth + Random.Range(-2f, 2f), 0f, _mockMaxHealth);
                _mockEnergy = Mathf.Clamp(_mockEnergy + Random.Range(-3f, 3f), 0f, _mockMaxEnergy);
                _mockHydration = Mathf.Clamp(_mockHydration + Random.Range(-5f, 5f), 0f, _mockMaxHydration);
                UpdateAllBars();
            }
        }
        
        private void InitializeUI()
        {
            if (_uiDocument == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "UIDocument non trovato!");
                return;
            }
            
            _root = _uiDocument.rootVisualElement;
            
            if (_root == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "Root VisualElement non trovato! Assicurati che il file UXML sia collegato.");
                return;
            }
            
            if (_enableDebugLogs)
                SporiumLogger.LogInfo(LogCategory.UI, "Player Status Panel UI inizializzato");
        }
        
        private void InitializeBars()
        {
            if (_root == null) return;
            
            // Hydration Bar (segmented) - Nome cambiato a "segmented-bar" come nel prompt
            var hydrationContainer = _root.Q<VisualElement>("hydration-bar-container");
            var segmentedBar = _root.Q<VisualElement>("segmented-bar");
            var hydrationValue = _root.Q<Label>("hydration-value");
            
            // Query nuovi elementi
            _lowTextLabel = _root.Q<Label>("low-text");
            _warningLabel = _root.Q<Label>("warning-text");
            _inventoryButton = _root.Q<Button>("inventory-btn");
            _reputationButton = _root.Q<Button>("reputation-btn");
            _diaryButton = _root.Q<Button>("diary-btn");

            var hydrationLabel = _root.Q<Label>("hydration-label");
            if (hydrationLabel != null)
                hydrationLabel.text = LocalizationManager.GetString("player_status.hydration");
            var diaryLabel = _root.Q<Label>("diary-label");
            if (diaryLabel != null)
                diaryLabel.text = LocalizationManager.GetString("player_status.diary");
            
            if (_warningLabel != null && !_hydrationConnectedToRealSystem)
                _warningLabel.text = LocalizationManager.GetString("player_status.low_action_warning");
            
            // Setup buttons
            if (_inventoryButton != null)
            {
                _inventoryButton.clicked += OnInventoryClick;
            }
            
            if (_reputationButton != null)
            {
                _reputationButton.clicked += OnReputationClick;
            }
            
            if (_diaryButton != null)
            {
                _diaryButton.clicked += OnDiaryClick;
            }
            
            if (segmentedBar != null && hydrationValue != null)
            {
                _hydrationBar = new SegmentedStatBarController(segmentedBar, hydrationValue, "hydration", _thresholds, this);
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "Segmented bar elements non trovati!");
            }
        }
        
        private void UpdateAllBars()
        {
            // Update Hydration only
            if (_hydrationBar != null)
            {
                _hydrationBar.UpdateValues(_mockHydration, _mockMaxHydration);
                
                // Mostra/nascondi low-text e warning-text quando <40%
                float percentage = _hydrationBar.Percentage;
                bool isLow = percentage < 40f;
                
                if (_lowTextLabel != null)
                {
                    if (isLow)
                    {
                        _lowTextLabel.style.display = DisplayStyle.Flex;
                        _lowTextLabel.style.height = StyleKeyword.Auto; /* Ripristina altezza normale */
                        _lowTextLabel.style.overflow = Overflow.Visible;
                    }
                    else
                    {
                        _lowTextLabel.style.display = DisplayStyle.None;
                        _lowTextLabel.style.height = 0f; /* Altezza zero quando nascosto */
                        _lowTextLabel.style.overflow = Overflow.Hidden;
                    }
                }
                
                if (_warningLabel != null)
                {
                    if (isLow)
                    {
                        _warningLabel.style.display = DisplayStyle.Flex;
                        _warningLabel.style.height = StyleKeyword.Auto; /* Ripristina altezza normale */
                        _warningLabel.style.overflow = Overflow.Visible;
                    }
                    else
                    {
                        _warningLabel.style.display = DisplayStyle.None;
                        _warningLabel.style.height = 0f; /* Altezza zero quando nascosto */
                        _warningLabel.style.overflow = Overflow.Hidden;
                    }
                }
            }
        }
        
        // ============================================
        // Metodi pubblici per integrazione futura
        // ============================================
        
        /// <summary>
        /// Aggiorna il valore Hydration (per integrazione con PlayerHydrationSystem).
        /// </summary>
        public void UpdateHydration(float current, float max)
        {
            _mockHydration = current;
            _mockMaxHydration = max;
            
            if (_hydrationBar != null)
            {
                _hydrationBar.UpdateValues(current, max);
                
                // Mostra/nascondi low-text e warning-text quando <40%
                float percentage = _hydrationBar.Percentage;
                bool isLow = percentage < 40f;
                
                if (_lowTextLabel != null)
                {
                    if (isLow)
                    {
                        _lowTextLabel.style.display = DisplayStyle.Flex;
                        _lowTextLabel.style.height = StyleKeyword.Auto;
                        _lowTextLabel.style.overflow = Overflow.Visible;
                    }
                    else
                    {
                        _lowTextLabel.style.display = DisplayStyle.None;
                        _lowTextLabel.style.height = 0f;
                        _lowTextLabel.style.overflow = Overflow.Hidden;
                    }
                }
                
                if (_warningLabel != null)
                {
                    if (isLow)
                    {
                        _warningLabel.style.display = DisplayStyle.Flex;
                        _warningLabel.style.height = StyleKeyword.Auto;
                        _warningLabel.style.overflow = Overflow.Visible;
                    }
                    else
                    {
                        _warningLabel.style.display = DisplayStyle.None;
                        _warningLabel.style.height = 0f;
                        _warningLabel.style.overflow = Overflow.Hidden;
                    }
                }
            }
        }
        
        // ============================================
        // Button Click Handlers
        // ============================================
        
        private void OnInventoryClick()
        {
            if (_playerInventoryPanel == null)
                _playerInventoryPanel = FindObjectOfType<PlayerInventoryPanelController>();
            if (_playerInventoryPanel != null)
            {
                _playerInventoryPanel.Toggle();
                if (_enableDebugLogs)
                    SporiumLogger.LogInfo(LogCategory.UI, "Inventory button clicked - PlayerInventoryPanel.Toggle() chiamato");
                return;
            }
            if (_hudInventory == null)
                _hudInventory = FindObjectOfType<HUDInventory>();
            if (_hudInventory != null)
            {
                _hudInventory.Toggle();
                if (_enableDebugLogs)
                    SporiumLogger.LogInfo(LogCategory.UI, "Inventory button clicked - HUDInventory.Toggle() (fallback)");
            }
            else
                SporiumLogger.LogWarning(LogCategory.UI, "PlayerInventoryPanel e HUDInventory non trovati! Assegna PlayerInventoryPanel per l'inventario definitivo.");
        }
        
        private void OnReputationClick()
        {
            // TODO: Show reputation panel
            if (_enableDebugLogs)
                SporiumLogger.LogInfo(LogCategory.UI, "Reputation button clicked");
        }
        
        private void OnDiaryClick()
        {
            // TODO: Open SPORAE Diary
            if (_enableDebugLogs)
                SporiumLogger.LogInfo(LogCategory.UI, "Diary button clicked");
        }
        
        // ============================================
        // Event Handlers (per integrazione futura)
        // ============================================
        
        public void SubscribeToPlayerEvents()
        {
            TryConnectHydrationSystem();
        }

        private void OnDestroy()
        {
            if (_hydrationConnectedToRealSystem)
            {
                var gm = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
                if (gm?.PlayerHydrationSystem != null)
                    gm.PlayerHydrationSystem.OnHydrationChanged -= OnHydrationChanged;
            }
        }
    }
}

