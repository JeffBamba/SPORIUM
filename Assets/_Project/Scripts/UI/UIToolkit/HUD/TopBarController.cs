using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System;
using Sporae.UI.UIToolkit.HUD;
using Sporae.DevTools;
using _Project.Sporae.Core;
using _Project;
using Sporae.Core;
using Sporae.UI.UIToolkit.HUD.Components;
using Sporae.Dome.PotSystem.Growth;

namespace Sporae.UI.UIToolkit.HUD
{
    /// <summary>
    /// Controller per la TopBar HUD con tutte le metriche di gioco.
    /// </summary>
    public class TopBarController : MonoBehaviour
    {
        [Header("UI Toolkit References")]
        [SerializeField] private UIDocument _uiDocument;
        
        [Header("Game Metrics")]
        [SerializeField] private int _actionsLeft = 3;
        [SerializeField] private int _maxActions = 4;
        [SerializeField] private float _phLevel = 7.2f;
        [SerializeField] private float _condensation = 78f;
        [SerializeField] private float _mutationIndex = 0.42f;
        [SerializeField] private int _cryBalance = 1245;
        [SerializeField] private int _grateValue = 12;
        
        [Header("Configuration")]
        [SerializeField] private bool _enableDebugLogs = false;

        [Header("UI Glow Frame")]
        [SerializeField] private Material _glowFrameMaterial;
        [SerializeField] private bool _glowFrameLiveUpdate = true;
        
        // Suppress warning for unused field (used in Inspector)
        #pragma warning disable 0414
        
        // UI Elements
        private VisualElement _root;
        private SegmentedBarUI _actionsBar;
        private Label _actionsValueLabel;
        private VisualElement _phDisplay;
        private Label _phDriftLabel;
        private Label _phBandLabel;
        private VisualElement _phMarker;
        private VisualElement _phGradient;
        private VisualElement _phNeutralZone;
        private VisualElement _phSlider;
        private Label _condensationValueLabel;
        private VisualElement _condensationDisplay; // FASE 10: Per tooltip futuro
        private Label _mutationValueLabel;
        private Label _cryValueLabel;
        private Label _grateValueLabel;

        private VisualElement _glowFrame;
        private UiGlowFrameGenerator _glowFrameGenerator;
        private Material _glowFrameMaterialRuntime;
        private const string GlowShaderName = "Sporae/UI/GlowFrame";
        
        // Tooltip
        private VisualElement _phTooltip;
        private Label _phTooltipText;
        
        // FASE 8: Tooltip Condensation
        private VisualElement _condensationTooltip;
        private Label _condensationTooltipText;
        private Button _condensationCollectButton;
        
        // pH Gradient Texture (creata runtime)
        private Texture2D _phGradientTexture;
        
        // pH Color constants (0-14 scale)
        private static readonly Color PH_RED = new Color(1f, 0.165f, 0.165f, 1f); // #FF2A2A
        private static readonly Color PH_ORANGE = new Color(1f, 0.667f, 0.2f, 1f); // #FFAA33
        private static readonly Color PH_WHITE = new Color(0.961f, 0.969f, 0.980f, 1f); // #F5F7FA
        private static readonly Color PH_BLUE = new Color(0.2f, 0.722f, 1f, 1f); // #33B8FF
        private static readonly Color PH_PURPLE = new Color(0.357f, 0.310f, 1f, 1f); // #5B4FFF
        private static readonly Color PH_GLOW_RED = new Color(1f, 0.165f, 0.165f, 1f); // #FF2A2A
        private static readonly Color PH_GLOW_WHITE = new Color(0.847f, 1f, 0.898f, 1f); // #D8FFE5
        private static readonly Color PH_GLOW_BLUE = new Color(0.2f, 0.722f, 1f, 1f); // #33B8FF
        private static readonly Color PH_NEUTRAL_ZONE = new Color(0.847f, 1f, 0.898f, 0.35f); // #D8FFE5 35%
        
        // Game Systems
        private GameManager _gameManager;
        private ActionSystem _actionSystem;
        private EconomySystem _economySystem;
        private PhSystem _phSystem;
        
        // Animation coroutines
        private Coroutine _condensationIdleCoroutine;
        private Coroutine _phPulseCoroutine;
        
        // Condensation threshold tracking
        private float _previousCondensation = -1f; // -1 indica valore iniziale non ancora impostato
        
        // Colors
        private readonly Color _greenStable = new Color(0.498f, 1f, 0.478f, 1f); // #7FFF7A
        private readonly Color _yellowWarning = new Color(0.902f, 0.788f, 0.435f, 1f); // #E6C96F
        private readonly Color _redCritical = new Color(0.827f, 0.373f, 0.373f, 1f); // #D35F5F
        private readonly Color _blueInfo = new Color(0.365f, 0.714f, 0.890f, 1f); // #5DB6E3
        
        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            
            // DEBUG_SAFE_FIX: Imposta sortingOrder per HUD base (sotto PlantCard, sopra background)
            if (_uiDocument != null)
                _uiDocument.sortingOrder = 50;
        }
        
        private void Start()
        {
            InitializeUI();
            InitializeComponents();
            InitializeGameSystems();
            UpdateAllMetrics();
            StartIdleAnimations();
        }
        
        private void InitializeGameSystems()
        {
            // Cerca GameManager nella scena
            _gameManager = FindObjectOfType<GameManager>();
            
            if (_gameManager != null)
            {
                // Collega ActionSystem
                if (_gameManager.ActionSystem != null)
                {
                    _actionSystem = _gameManager.ActionSystem;
                    
                    // Sottoscrivi agli eventi
                    _actionSystem.OnActionsChanged += OnActionsChanged;
                    
                    // Aggiorna valori iniziali
                    _actionsLeft = _actionSystem.ActionsLeft;
                    _maxActions = _actionSystem.MaxActions;
                    UpdateActions(_actionsLeft, _maxActions);
                    
                    if (_enableDebugLogs)
                    {
                        SporiumLogger.LogInfo(LogCategory.UI, $"TopBarController: ActionSystem collegato - Actions: {_actionsLeft}/{_maxActions}");
                    }
                }
                
                // Collega EconomySystem
                if (_gameManager.EconomySystem != null)
                {
                    _economySystem = _gameManager.EconomySystem;
                    
                    // Sottoscrivi agli eventi
                    _economySystem.OnCRYChanged += OnCRYChanged;
                    
                    // Aggiorna valore iniziale
                    _cryBalance = _economySystem.CurrentCRY;
                    UpdateCryBalance(_cryBalance);
                    
                    if (_enableDebugLogs)
                    {
                        SporiumLogger.LogInfo(LogCategory.UI, $"TopBarController: EconomySystem collegato - CRY: {_cryBalance}");
                    }
                }
                
                // FASE 10: Collega CondensationSystem
                if (_gameManager.CondensationSystem != null)
                {
                    // Sottoscrivi agli eventi
                    _gameManager.OnCondensationChanged += OnCondensationChanged;
                    
                    // Aggiorna valore iniziale (imposta anche previous per evitare toast al primo caricamento)
                    _condensation = _gameManager.CondensationSystem.CurrentAccumulation;
                    _previousCondensation = _condensation; // Imposta come previous per evitare toast iniziale
                    UpdateCondensation(_condensation);
                    
                    if (_enableDebugLogs)
                    {
                        SporiumLogger.LogInfo(LogCategory.UI, $"TopBarController: CondensationSystem collegato - Condensation: {_condensation}%");
                    }
                }
                
                // Collega PhSystem
                TryConnectPhSystem();
                
                // Sottoscrivi all'evento OnServiceRegistered per collegarsi quando PhSystem viene registrato
                if (ServiceContainer.Instance != null)
                {
                    ServiceContainer.Instance.OnServiceRegistered += OnServiceRegistered;
                }
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "TopBarController: GameManager non trovato. Usando valori mock.");
            }
        }
        
        private void TryConnectPhSystem()
        {
            _phSystem = ServiceContainer.Instance?.Get<PhSystem>(suppressWarning: true);
            
            if (_phSystem != null)
            {
                // Sottoscrivi agli eventi
                _phSystem.OnPhChanged += OnPhChanged;
                
                // Aggiorna valore iniziale
                _phLevel = _phSystem.CurrentPh;
                UpdatePh(_phLevel);
                
                
                if (_enableDebugLogs)
                {
                    SporiumLogger.LogInfo(LogCategory.UI, $"TopBarController: PhSystem collegato - pH: {_phLevel}");
                }
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "TopBarController: PhSystem non trovato nel ServiceContainer. Riproverà quando verrà registrato.");
            }
        }
        
        private void OnServiceRegistered(object service)
        {
            
            // Se PhSystem viene registrato e non siamo ancora collegati, connettiamoci
            if (service is PhSystem && _phSystem == null)
            {
                TryConnectPhSystem();
            }
        }
        
        private void OnActionsChanged(int actionsLeft)
        {
            if (_actionSystem != null)
            {
                _actionsLeft = actionsLeft;
                _maxActions = _actionSystem.MaxActions;
                UpdateActions(_actionsLeft, _maxActions);
            }
        }
        
        private void OnCRYChanged(int cryAmount)
        {
            if (_economySystem != null)
            {
                _cryBalance = cryAmount;
                UpdateCryBalance(_cryBalance);
            }
        }
        
        private void OnPhChanged(float newPh, float delta)
        {
            
            if (_phSystem != null)
            {
                _phLevel = newPh;
                UpdatePh(_phLevel);
            }
        }
        
        /// <summary>
        /// FASE 10: Handler per cambio condensazione (riceve percentuale 0-100%).
        /// </summary>
        private void OnCondensationChanged(float percentage)
        {
            float previousValue = _previousCondensation;
            _condensation = percentage;
            
            // Rileva attraversamento soglie (solo se non è il primo valore)
            if (previousValue >= 0f)
            {
                CheckCondensationThresholds(previousValue, percentage);
            }
            
            _previousCondensation = percentage;
            UpdateCondensation(_condensation);
            
            // FASE 8: Aggiorna tooltip se visibile
            if (_condensationTooltip != null && _condensationTooltip.style.display == DisplayStyle.Flex)
            {
                UpdateCondensationTooltipContent();
            }
        }
        
        /// <summary>
        /// Verifica se la condensazione ha attraversato le soglie critiche (50%, 80%, 100%)
        /// ed emette toast notifications appropriate.
        /// </summary>
        private void CheckCondensationThresholds(float previousValue, float currentValue)
        {
            // Soglia 50%: Tank mezzo pieno (warning giallo)
            if (previousValue < 50f && currentValue >= 50f)
            {
                EmitCondensationToast("COND-005");
            }
            
            // Soglia 80%: Umidità impatta muffe (warning)
            if (previousValue < 80f && currentValue >= 80f)
            {
                EmitCondensationToast("COND-006");
            }
            
            // Soglia 100%: Tank pieno, umidità alta (danger)
            if (previousValue < 100f && currentValue >= 100f)
            {
                EmitCondensationToast("COND-008");
            }
        }
        
        /// <summary>
        /// Emette una toast notification per la condensazione.
        /// </summary>
        private void EmitCondensationToast(string toastCode)
        {
            var foundation = Sporae.UI.UIToolkit.NotificationsFoundation.FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
            {
                foundation.PostToast(toastCode, new Sporae.UI.UIToolkit.NotificationsFoundation.NotificationPayload());
            }
            else
            {
                var toastManager = ServiceContainer.Instance?.Get<ToastNotificationManager>(suppressWarning: true);
                if (toastManager != null)
                {
                    // Fallback con messaggi hardcoded se foundation non disponibile
                    string message = toastCode switch
                    {
                        "COND-005" => "⚠️ Tank mezzo pieno",
                        "COND-006" => "⚠️ Umidità impatta muffe",
                        "COND-008" => "🚨 Tank pieno, umidità alta",
                        _ => $"Condensation threshold: {toastCode}"
                    };
                    toastManager.ShowToast(ToastNotificationType.Warning, message, toastCode);
                }
            }
        }
        
        private void SetupPhTooltip()
        {
            if (_phDisplay == null)
                return;
            
            // Crea tooltip container
            _phTooltip = new VisualElement();
            _phTooltip.name = "ph-tooltip";
            _phTooltip.style.position = Position.Absolute;
            _phTooltip.style.display = DisplayStyle.None;
            _phTooltip.style.backgroundColor = new Color(0f, 0f, 0f, 0.9f);
            _phTooltip.style.borderTopWidth = 2f;
            _phTooltip.style.borderRightWidth = 2f;
            _phTooltip.style.borderBottomWidth = 2f;
            _phTooltip.style.borderLeftWidth = 2f;
            _phTooltip.style.borderTopColor = new Color(0.365f, 0.714f, 0.890f, 1f); // #5DB6E3
            _phTooltip.style.borderRightColor = new Color(0.365f, 0.714f, 0.890f, 1f);
            _phTooltip.style.borderBottomColor = new Color(0.365f, 0.714f, 0.890f, 1f);
            _phTooltip.style.borderLeftColor = new Color(0.365f, 0.714f, 0.890f, 1f);
            _phTooltip.style.paddingTop = 8f;
            _phTooltip.style.paddingRight = 8f;
            _phTooltip.style.paddingBottom = 8f;
            _phTooltip.style.paddingLeft = 8f;
            _phTooltip.style.width = 320f;
            _phTooltip.style.maxWidth = 320f;
            _phTooltip.style.minHeight = 100f;
            _phTooltip.pickingMode = PickingMode.Ignore; // Non bloccare interazioni
            
            // Crea testo tooltip
            _phTooltipText = new Label();
            _phTooltipText.name = "ph-tooltip-text";
            _phTooltipText.style.whiteSpace = WhiteSpace.Normal;
            _phTooltipText.style.color = new Color(0.961f, 0.969f, 0.980f, 1f); // Bianco
            _phTooltipText.style.fontSize = 16f; // Aumentato da 11f per leggibilità (BUG X)
            _phTooltipText.style.unityTextAlign = TextAnchor.UpperLeft;
            _phTooltipText.enableRichText = true;
            _phTooltip.Add(_phTooltipText);
            
            // Aggiungi tooltip al root (per posizionamento assoluto)
            _root.Add(_phTooltip);
            
            // Setup hover events
            _phDisplay.RegisterCallback<MouseEnterEvent>(OnPhHoverEnter);
            _phDisplay.RegisterCallback<MouseLeaveEvent>(OnPhHoverExit);
            _phDisplay.RegisterCallback<MouseMoveEvent>(OnPhHoverMove);
        }
        
        private void OnPhHoverEnter(MouseEnterEvent evt)
        {
            if (_phSystem != null && _phTooltip != null)
            {
                UpdatePhTooltipContent();
                _phTooltip.style.display = DisplayStyle.Flex;
            }
        }
        
        private void OnPhHoverExit(MouseLeaveEvent evt)
        {
            if (_phTooltip != null)
            {
                _phTooltip.style.display = DisplayStyle.None;
            }
        }
        
        private void OnPhHoverMove(MouseMoveEvent evt)
        {
            if (_phTooltip != null && _phTooltip.style.display == DisplayStyle.Flex && _phDisplay != null)
            {
                // Posiziona tooltip vicino al ph-display (sotto e a destra)
                var phDisplayBounds = _phDisplay.worldBound;
                var rootBounds = _root.worldBound;
                
                // Calcola posizione relativa al root
                float tooltipX = phDisplayBounds.xMax + 10f;
                float tooltipY = phDisplayBounds.yMax - 20f;
                
                // Assicurati che il tooltip non esca dallo schermo
                float tooltipWidth = 320f;
                float tooltipHeight = _phTooltip.resolvedStyle.height;
                
                if (tooltipX + tooltipWidth > rootBounds.width)
                {
                    tooltipX = phDisplayBounds.xMin - tooltipWidth - 10f;
                }
                
                if (tooltipY + tooltipHeight > rootBounds.height)
                {
                    tooltipY = phDisplayBounds.yMin - tooltipHeight - 10f;
                }
                
                _phTooltip.style.left = tooltipX;
                _phTooltip.style.top = tooltipY;
            }
        }
        
        private void UpdatePhTooltipContent()
        {
            if (_phSystem == null || _phTooltipText == null)
                return;
            
            // Ottieni breakdown del calcolo
            string breakdown = _phSystem.GetCalculationBreakdown();
            
            // Aggiungi banda pH
            string bandName = _phSystem.GetBandName();
            Color bandColor = _phSystem.GetBandColor();
            
            // FASE 2.1: Aggiungi informazioni sui modificatori crescita e resa per ogni famiglia
            PhSystem.PhBand phBand = _phSystem.EvaluateState();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>pH DRIFT</b>");
            sb.AppendLine($"<b>Banda: {bandName}</b>");
            sb.AppendLine();
            sb.AppendLine(breakdown);
            sb.AppendLine();
            sb.AppendLine("<b>Effetti per Famiglia:</b>");
            sb.AppendLine();
            
            // Pure
            float pureGrowth = PhGrowthModifier.GetGrowthMultiplier(phBand, PlantFamily.Pure);
            float pureYield = PhGrowthModifier.GetYieldMultiplier(phBand, PlantFamily.Pure);
            bool pureSterile = PhGrowthModifier.IsSterile(phBand, PlantFamily.Pure);
            sb.AppendLine($"<b>Pure:</b>");
            if (pureGrowth != 1.0f)
            {
                float growthPercent = (pureGrowth - 1.0f) * 100f;
                string growthSign = growthPercent > 0 ? "+" : "";
                sb.AppendLine($"  Crescita: {growthSign}{growthPercent:F0}%");
            }
            if (pureYield != 1.0f)
            {
                float yieldPercent = (pureYield - 1.0f) * 100f;
                string yieldSign = yieldPercent > 0 ? "+" : "";
                sb.AppendLine($"  Resa: {yieldSign}{yieldPercent:F0}%");
            }
            if (pureSterile)
            {
                sb.AppendLine($"  <color=#FF0000>STERILE (3 giorni)</color>");
            }
            sb.AppendLine();
            
            // Evil
            float evilGrowth = PhGrowthModifier.GetGrowthMultiplier(phBand, PlantFamily.Evil);
            float evilYield = PhGrowthModifier.GetYieldMultiplier(phBand, PlantFamily.Evil);
            sb.AppendLine($"<b>Evil:</b>");
            if (evilGrowth != 1.0f)
            {
                float growthPercent = (evilGrowth - 1.0f) * 100f;
                string growthSign = growthPercent > 0 ? "+" : "";
                sb.AppendLine($"  Crescita: {growthSign}{growthPercent:F0}%");
            }
            if (evilYield != 1.0f)
            {
                float yieldPercent = (evilYield - 1.0f) * 100f;
                string yieldSign = yieldPercent > 0 ? "+" : "";
                sb.AppendLine($"  Resa: {yieldSign}{yieldPercent:F0}%");
            }
            sb.AppendLine();
            
            // Standard
            float standardGrowth = PhGrowthModifier.GetGrowthMultiplier(phBand, PlantFamily.Standard);
            sb.AppendLine($"<b>Standard:</b>");
            if (standardGrowth != 1.0f)
            {
                float growthPercent = (standardGrowth - 1.0f) * 100f;
                string growthSign = growthPercent > 0 ? "+" : "";
                sb.AppendLine($"  Crescita: {growthSign}{growthPercent:F0}%");
            }
            else
            {
                sb.AppendLine($"  Nessun effetto");
            }
            
            _phTooltipText.text = sb.ToString();
        }
        
        private void InitializeUI()
        {
            if (_uiDocument == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "UIDocument non trovato su TopBarController!");
                return;
            }
            
            _root = _uiDocument.rootVisualElement;
            if (_root == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "Root VisualElement non trovato!");
                return;
            }

            _glowFrame = _root.Q<VisualElement>("glow-frame");
            SetupGlowFrame();
            
            // Query UI elements
            var actionsBarContainer = _root.Q<VisualElement>("actions-bar");
            _actionsValueLabel = _root.Q<Label>("actions-value");
            _phDisplay = _root.Q<VisualElement>("ph-display");
            _phDriftLabel = _root.Q<Label>("ph-drift-label");
            _phBandLabel = _root.Q<Label>("ph-band");
            _phSlider = _root.Q<VisualElement>("ph-slider");
            _phMarker = _root.Q<VisualElement>("ph-marker");
            _phGradient = _root.Q<VisualElement>("ph-gradient");
            _phNeutralZone = _root.Q<VisualElement>("ph-neutral-zone");
            
            _condensationValueLabel = _root.Q<Label>("condensation-value");
            _condensationDisplay = _root.Q<VisualElement>("condensation-display"); // FASE 10: Query per tooltip futuro
            _mutationValueLabel = _root.Q<Label>("mutation-value");
            _cryValueLabel = _root.Q<Label>("cry-value");
            _grateValueLabel = _root.Q<Label>("grate-value");
            
            // Setup tooltip per pH
            SetupPhTooltip();
            
            // FASE 8: Setup tooltip per condensazione
            SetupCondensationTooltip();
            
            // Crea texture gradiente pH (0-14 scale)
            CreatePhGradientTexture();
            
            // Registra callback per quando il layout è calcolato (per aggiornare posizione marker)
            if (_phSlider != null)
            {
                _phSlider.RegisterCallback<GeometryChangedEvent>(OnPhSliderGeometryChanged);
            }
            
            // Initialize Actions Bar
            if (actionsBarContainer != null)
            {
                _actionsBar = new SegmentedBarUI(
                    actionsBarContainer,
                    4,
                    _yellowWarning,
                    new Color(0.118f, 0.157f, 0.165f, 1f),
                    _yellowWarning
                );
            }
        }

        private void SetupGlowFrame()
        {
            if (_glowFrame == null) return;

            if (_glowFrameMaterial == null)
            {
                var shader = Shader.Find(GlowShaderName);
                if (shader != null)
                    _glowFrameMaterial = new Material(shader);
            }

            if (_glowFrameMaterial != null)
            {
                ApplyGlowDefaults(_glowFrameMaterial);
                _glowFrameMaterialRuntime = new Material(_glowFrameMaterial);
                _glowFrameGenerator = new UiGlowFrameGenerator(_glowFrame, _glowFrameMaterialRuntime);
            }

            _glowFrameLiveUpdate = true;

        }

        private static void ApplyGlowDefaults(Material mat)
        {
            // Glow frame should be transparent; bar background is handled by USS.
            var bg = new Color(0f, 0f, 0f, 0f);
            // Border: #7FFF7A @ 60%
            var border = new Color(127f / 255f, 255f / 255f, 122f / 255f, 0.60f);
            // Glow: same green, stronger alpha for bloom
            var glow = new Color(127f / 255f, 255f / 255f, 122f / 255f, 0.90f);

            mat.SetColor("_GradTop", bg);
            mat.SetColor("_GradBottom", bg);
            mat.SetFloat("_GradStrength", 0.0f);
            mat.SetColor("_BorderColor", border);
            mat.SetColor("_GlowColor", glow);
            mat.SetFloat("_BorderThickness", 1.5f); // Ridotto per border più sottile, mantenendo glow
            mat.SetFloat("_BorderSoftness", 0.5f); // Più netto per border più visibile come sottile
            mat.SetFloat("_GlowSize", 14.0f);
            mat.SetFloat("_GlowIntensity", 1.0f);
            mat.SetFloat("_GlowFalloff", 1.25f);
        }

        private void Update()
        {
            if (_glowFrameLiveUpdate && _glowFrameGenerator != null)
            {
                if (_glowFrameMaterialRuntime != null && _glowFrameMaterial != null)
                    _glowFrameMaterialRuntime.CopyPropertiesFromMaterial(_glowFrameMaterial);
                if (_glowFrameMaterialRuntime != null)
                {
                    _glowFrameMaterialRuntime.SetFloat("_EdgeMode", 2.0f); // bottom edge only
                    _glowFrameMaterialRuntime.SetFloat("_BorderThickness", 1.5f); // Border più sottile
                    _glowFrameMaterialRuntime.SetFloat("_BorderSoftness", 0.5f); // Più netto
                }
                _glowFrameGenerator.Render();
            }
        }

        private void OnDisable()
        {
            _glowFrameGenerator?.Dispose();
            _glowFrameGenerator = null;
        }
        
        private void InitializeComponents()
        {
            // Mutation Orbit è gestito come componente separato opzionale
            // Se presente, verrà aggiornato via UpdateMutation()
            // Se non presente, solo il label verrà aggiornato
        }
        
        private void UpdateAllMetrics()
        {
            UpdateActions(_actionsLeft, _maxActions);
            UpdatePh(_phLevel);
            UpdateCondensation(_condensation);
            UpdateMutation(_mutationIndex);
            UpdateCryBalance(_cryBalance);
            UpdateGrate(_grateValue);
        }
        
        /// <summary>
        /// Aggiorna la barra ACTIONS con colori dinamici basati su threshold.
        /// </summary>
        public void UpdateActions(int current, int max)
        {
            _actionsLeft = current;
            _maxActions = max;
            
            if (_actionsValueLabel != null)
            {
                _actionsValueLabel.text = $"{current}/{max}";
            }
            
            if (_actionsBar != null)
            {
                // Determina colore basato su threshold
                Color fillColor;
                if (current >= 3)
                {
                    fillColor = _greenStable; // 3-4 actions: green
                }
                else if (current == 2)
                {
                    fillColor = _yellowWarning; // 2 actions: yellow
                }
                else
                {
                    fillColor = _redCritical; // 0-1 actions: red
                }
                
                _actionsBar.SetColors(fillColor, new Color(0.118f, 0.157f, 0.165f, 1f), fillColor);
                _actionsBar.UpdateValue(current, max);
            }
        }
        
        /// <summary>
        /// Crea texture gradiente per pH (0-14 scale)
        /// </summary>
        private void CreatePhGradientTexture()
        {
            int width = 256;
            int height = 8;
            
            _phGradientTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            _phGradientTexture.wrapMode = TextureWrapMode.Clamp;
            _phGradientTexture.filterMode = FilterMode.Bilinear;
            
            // Crea gradiente: Rosso (0) → Arancione (4) → Bianco (7) → Azzurro (10) → Viola (14)
            for (int x = 0; x < width; x++)
            {
                float normalizedX = x / (float)(width - 1);
                float phValue = normalizedX * 14f; // 0-14 scale
                Color pixelColor = GetPhColorFromScale(phValue);
                
                for (int y = 0; y < height; y++)
                {
                    _phGradientTexture.SetPixel(x, y, pixelColor);
                }
            }
            
            _phGradientTexture.Apply();
            
            // Applica texture al ph-gradient VisualElement
            if (_phGradient != null)
            {
                _phGradient.style.backgroundImage = new StyleBackground(_phGradientTexture);
            }
        }
        
        /// <summary>
        /// Ottiene colore pH dalla scala 0-14 con interpolazione lineare
        /// </summary>
        private Color GetPhColorFromScale(float phValue)
        {
            phValue = Mathf.Clamp(phValue, 0f, 14f);
            
            if (phValue <= 4f)
            {
                // Rosso (0) → Arancione (4)
                float t = phValue / 4f;
                return Color.Lerp(PH_RED, PH_ORANGE, t);
            }
            else if (phValue <= 7f)
            {
                // Arancione (4) → Bianco (7)
                float t = (phValue - 4f) / 3f;
                return Color.Lerp(PH_ORANGE, PH_WHITE, t);
            }
            else if (phValue <= 10f)
            {
                // Bianco (7) → Azzurro (10)
                float t = (phValue - 7f) / 3f;
                return Color.Lerp(PH_WHITE, PH_BLUE, t);
            }
            else
            {
                // Azzurro (10) → Viola (14)
                float t = (phValue - 10f) / 4f;
                return Color.Lerp(PH_BLUE, PH_PURPLE, t);
            }
        }
        
        /// <summary>
        /// Callback quando il layout del ph-slider viene calcolato
        /// </summary>
        private void OnPhSliderGeometryChanged(GeometryChangedEvent evt)
        {
            // Aggiorna la posizione del marker quando il layout è pronto
            if (_phLevel != 0f || _phSystem != null)
            {
                UpdatePh(_phLevel);
            }
        }
        
        /// <summary>
        /// Aggiorna il pH slider con marker posizionato dinamicamente.
        /// </summary>
        public void UpdatePh(float value)
        {
            _phLevel = value;
            
            // Mostra direttamente il valore pH nel range -100/+100 (come il vecchio sistema)
            // NON convertire in scala 0-14 - il vecchio sistema mostrava currentPh direttamente
            float phDisplayValue = value;
            
            // Colore base per "PH DRIFT" label
            Color phDriftColor = new Color(0.392f, 0.565f, 0.933f, 0.85f); // rgba(83, 144, 255, 0.85)
            
            // Aggiorna label "PH DRIFT" (solo label, senza valore)
            if (_phDriftLabel != null)
            {
                _phDriftLabel.text = "PH DRIFT";
                _phDriftLabel.style.color = new StyleColor(phDriftColor);
            }
            
            // Aggiorna valore numerico pH (mostra valore diretto -100/+100, convertito in scala 0-14 per display)
            if (_phBandLabel != null)
            {
                // Converti da range PhSystem (-100/+100) a scala visualizzazione (0-14) per display
                // Mapping: -100 → 0, 0 → 7, +100 → 14
                float phVisualScale = ((value + 100f) / 200f) * 14f;
                phVisualScale = Mathf.Clamp(phVisualScale, 0f, 14f);
                
                // Mostra valore in scala 0-14 (come nell'immagine di riferimento: 7.8)
                _phBandLabel.text = $"{phVisualScale:F1}";
                
                // Colore basato sulla banda se PhSystem disponibile
                if (_phSystem != null)
                {
                    Color bandColor = _phSystem.GetBandColor();
                    _phBandLabel.style.color = new StyleColor(bandColor);
                }
                else
                {
                    // Fallback: usa colore default se PhSystem non disponibile
                    _phBandLabel.style.color = new StyleColor(phDriftColor);
                }
            }
            
            if (_phMarker != null && _phGradient != null)
            {
                // Converti da range PhSystem (-100/+100) a scala visualizzazione (0-14) SOLO per il marker
                // Mapping: -100 → 0, 0 → 7, +100 → 14
                float phVisualScale = ((value + 100f) / 200f) * 14f;
                phVisualScale = Mathf.Clamp(phVisualScale, 0f, 14f);
                
                // Posiziona marker: (phVisualScale / 14) * 100%
                // Il marker è largo 12px, quindi dobbiamo centrarlo: left = (normalizedPos * 100%) - (6px / sliderWidth)
                float normalizedPos = phVisualScale / 14f;
                
                // Usa left in percentuale per posizionare il marker
                // Il marker è largo 12px, quindi per centrarlo dobbiamo sottrarre metà della larghezza
                // In UI Toolkit, left in percentuale si riferisce al bordo sinistro del marker
                // Per centrare: left = (normalizedPos * 100%) - (markerWidth/2 / sliderWidth * 100%)
                
                // Prova a ottenere la larghezza del slider
                float sliderWidth = _phSlider != null ? _phSlider.resolvedStyle.width : float.NaN;
                
                float markerWidth = 12f;
                float leftPositionPercent = normalizedPos * 100f;
                
                // Se la larghezza del slider è disponibile, calcola l'offset per centrare il marker
                if (!float.IsNaN(sliderWidth) && sliderWidth > 0f)
                {
                    // Calcola offset in percentuale: (markerWidth/2 / sliderWidth) * 100
                    float offsetPercent = (markerWidth / 2f / sliderWidth) * 100f;
                    leftPositionPercent -= offsetPercent;
                }
                
                // Usa left in percentuale
                _phMarker.style.left = new StyleLength(new Length(leftPositionPercent, LengthUnit.Percent));
                
                // Reset margin-left per evitare conflitti
                _phMarker.style.marginLeft = 0f;
                
                // Colore marker interpolato dal gradiente
                Color markerColor = GetPhColorFromScale(phVisualScale);
                _phMarker.style.backgroundColor = new StyleColor(markerColor);
                
                // Glow contestuale sul track (ph-slider)
                UpdatePhGlow(phVisualScale);
                
                // Zona ottimale overlay (6-8)
                UpdatePhNeutralZone(phVisualScale);
                
                // Pulse animation se drift giornaliero > 1.0
                float totalDrift = CalculateTotalDailyDrift();
                if (Mathf.Abs(totalDrift) > 1.0f)
                {
                    if (_phPulseCoroutine == null)
                    {
                        _phPulseCoroutine = StartCoroutine(PhPulseAnimation());
                    }
                }
                else
                {
                    if (_phPulseCoroutine != null)
                    {
                        StopCoroutine(_phPulseCoroutine);
                        _phPulseCoroutine = null;
                        // Reset size
                        if (_phMarker != null)
                        {
                            _phMarker.style.width = 12f;
                            _phMarker.style.height = 12f;
                            _phMarker.style.marginLeft = 0f;
                            _phMarker.style.marginTop = 0f;
                        }
                    }
                }
            }
            
            // Aggiorna tooltip se visibile
            if (_phTooltip != null && _phTooltip.style.display == DisplayStyle.Flex)
            {
                UpdatePhTooltipContent();
            }
        }
        
        /// <summary>
        /// Calcola drift rate giornaliero totale
        /// </summary>
        private float CalculateTotalDailyDrift()
        {
            if (_phSystem == null)
                return 0f;
            
            var contrib = _phSystem.GetContributions();
            // Drift rate giornaliero = somma di tutti i drift accodati o già applicati
            // Per ora usiamo i contributi già applicati (plants, actions, events, daily)
            float totalDrift = contrib.PlantsDrift + contrib.ActionsDrift + contrib.EventsDrift + contrib.DailyDrift;
            return totalDrift;
        }
        
        /// <summary>
        /// Aggiorna glow contestuale sul track basato su zona pH
        /// </summary>
        private void UpdatePhGlow(float phValue)
        {
            if (_phSlider == null)
                return;
            
            Color glowColor;
            float glowIntensity;
            
            if (phValue < 5f)
            {
                // Rosso per pH < 5
                glowColor = PH_GLOW_RED;
            }
            else if (phValue >= 5f && phValue <= 9f)
            {
                // Bianco-ghiaccio per zona neutrale 5-9
                glowColor = PH_GLOW_WHITE;
            }
            else
            {
                // Blu per pH > 9
                glowColor = PH_GLOW_BLUE;
            }
            
            // Intensità proporzionale al drift rate: |totalDrift| / 3
            float totalDrift = Mathf.Abs(CalculateTotalDailyDrift());
            glowIntensity = Mathf.Clamp01(totalDrift / 3f);
            
            // Applica glow come border color con opacità
            Color glowWithIntensity = new Color(glowColor.r, glowColor.g, glowColor.b, glowIntensity);
            _phSlider.style.borderTopColor = glowWithIntensity;
            _phSlider.style.borderRightColor = glowWithIntensity;
            _phSlider.style.borderBottomColor = glowWithIntensity;
            _phSlider.style.borderLeftColor = glowWithIntensity;
        }
        
        /// <summary>
        /// Aggiorna overlay zona ottimale (6-8)
        /// </summary>
        private void UpdatePhNeutralZone(float phValue)
        {
            if (_phNeutralZone == null)
                return;
            
            // Mostra overlay solo quando pH è tra 6-8
            if (phValue >= 6f && phValue <= 8f)
            {
                _phNeutralZone.style.display = DisplayStyle.Flex;
            }
            else
            {
                _phNeutralZone.style.display = DisplayStyle.None;
            }
        }
        
        private Color GetPhColor(float phValue)
        {
            // Gradient: Red (0) -> Orange (4) -> White (7) -> Blue (10) -> Purple (14)
            if (phValue <= 4f)
            {
                float t = phValue / 4f;
                return Color.Lerp(new Color(1f, 0.165f, 0.165f, 1f), new Color(1f, 0.667f, 0.2f, 1f), t); // Red to Orange
            }
            else if (phValue <= 7f)
            {
                float t = (phValue - 4f) / 3f;
                return Color.Lerp(new Color(1f, 0.667f, 0.2f, 1f), new Color(0.961f, 0.969f, 0.980f, 1f), t); // Orange to White
            }
            else if (phValue <= 10f)
            {
                float t = (phValue - 7f) / 3f;
                return Color.Lerp(new Color(0.961f, 0.969f, 0.980f, 1f), new Color(0.2f, 0.722f, 1f, 1f), t); // White to Blue
            }
            else
            {
                float t = (phValue - 10f) / 4f;
                return Color.Lerp(new Color(0.2f, 0.722f, 1f, 1f), new Color(0.357f, 0.310f, 1f, 1f), t); // Blue to Purple
            }
        }
        
        private IEnumerator PhPulseAnimation()
        {
            float baseSize = 12f;
            
            while (_phMarker != null)
            {
                float elapsed = 0f;
                float duration = 1f;
                
                while (_phMarker != null && elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.PingPong(elapsed / duration, 1f);
                    float size = Mathf.Lerp(baseSize, baseSize * 1.05f, t);
                    _phMarker.style.width = size;
                    _phMarker.style.height = size;
                    // Aggiorna margin per mantenere centrato
                    float margin = -(size - baseSize) / 2f;
                    _phMarker.style.marginLeft = margin;
                    _phMarker.style.marginTop = margin;
                    yield return null;
                }
            }
        }
        
        /// <summary>
        /// Aggiorna il valore CONDENSATION con idle animation.
        /// </summary>
        public void UpdateCondensation(float value)
        {
            _condensation = value;
            
            if (_condensationValueLabel != null)
            {
                _condensationValueLabel.text = $"{Mathf.RoundToInt(value)}%";
            }
        }
        
        private void StartIdleAnimations()
        {
            _condensationIdleCoroutine = StartCoroutine(CondensationIdleAnimation());
        }
        
        /// <summary>
        /// FASE 9: Animazione fittizia continua per condensazione (variazione fluida ±0.5-1.5% come pH drift).
        /// </summary>
        private IEnumerator CondensationIdleAnimation()
        {
            while (true)
            {
                float baseValue = _condensation; // Valore reale
                float time = Time.time;
                // Oscilla ±1% con frequenza 0.5 (variazione fluida continua)
                float variation = Mathf.Sin(time * 0.5f) * 1.0f;
                float displayValue = Mathf.Clamp(baseValue + variation, 0f, 100f);
                
                if (_condensationValueLabel != null)
                {
                    _condensationValueLabel.text = $"{Mathf.RoundToInt(displayValue)}%";
                }
                
                yield return null; // Aggiorna ogni frame per movimento fluido
            }
        }
        
        /// <summary>
        /// Aggiorna l'indice MUTATION.
        /// </summary>
        public void UpdateMutation(float index)
        {
            _mutationIndex = Mathf.Clamp01(index);
            
            if (_mutationValueLabel != null)
            {
                // Mostra come percentuale (come nell'immagine di riferimento: 45%)
                int percentage = Mathf.RoundToInt(_mutationIndex * 100f);
                _mutationValueLabel.text = $"{percentage}%";
                
                // Colore dinamico
                Color color;
                if (_mutationIndex <= 0.33f)
                {
                    color = _greenStable; // Stable
                }
                else if (_mutationIndex <= 0.66f)
                {
                    color = _yellowWarning; // Warning
                }
                else
                {
                    color = _redCritical; // Critical
                }
                
                _mutationValueLabel.style.color = new StyleColor(color);
            }
            
            // Update MutationOrbitUI component
            var mutationOrbit = GetComponent<MutationOrbitUI>();
            if (mutationOrbit != null)
            {
                mutationOrbit.UpdateMutation(_mutationIndex);
            }
        }
        
        /// <summary>
        /// Aggiorna il CRY BALANCE con formattazione numerica.
        /// </summary>
        public void UpdateCryBalance(int value)
        {
            _cryBalance = value;
            
            if (_cryValueLabel != null)
            {
                _cryValueLabel.text = value.ToString("N0"); // Formato con virgole
            }
        }
        
        /// <summary>
        /// Aggiorna il valore GRATE.
        /// </summary>
        public void UpdateGrate(int value)
        {
            _grateValue = value;
            
            if (_grateValueLabel != null)
            {
                _grateValueLabel.text = $"+{value}";
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_actionSystem != null)
            {
                _actionSystem.OnActionsChanged -= OnActionsChanged;
            }
            
            if (_economySystem != null)
            {
                _economySystem.OnCRYChanged -= OnCRYChanged;
            }
            
            // FASE 10: Unsubscribe da CondensationSystem
            if (_gameManager != null)
            {
                _gameManager.OnCondensationChanged -= OnCondensationChanged;
            }
            
            // Unsubscribe from ServiceContainer event
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered -= OnServiceRegistered;
            }
            
            if (_phSystem != null)
            {
                _phSystem.OnPhChanged -= OnPhChanged;
            }
            
            // Unregister hover events
            if (_phDisplay != null)
            {
                _phDisplay.UnregisterCallback<MouseEnterEvent>(OnPhHoverEnter);
                _phDisplay.UnregisterCallback<MouseLeaveEvent>(OnPhHoverExit);
                _phDisplay.UnregisterCallback<MouseMoveEvent>(OnPhHoverMove);
            }
            
            // FASE 8: Unregister condensation hover events
            if (_condensationDisplay != null)
            {
                _condensationDisplay.UnregisterCallback<MouseEnterEvent>(OnCondensationHoverEnter);
                _condensationDisplay.UnregisterCallback<MouseLeaveEvent>(OnCondensationHoverExit);
                _condensationDisplay.UnregisterCallback<MouseMoveEvent>(OnCondensationHoverMove);
            }
            
            // FASE 8: Unregister tooltip hover events
            if (_condensationTooltip != null)
            {
                _condensationTooltip.UnregisterCallback<MouseEnterEvent>(OnCondensationTooltipHoverEnter);
                _condensationTooltip.UnregisterCallback<MouseLeaveEvent>(OnCondensationTooltipHoverExit);
            }
            
            // FASE 8: Unregister collect button
            if (_condensationCollectButton != null)
            {
                _condensationCollectButton.clicked -= OnCondensationCollectClicked;
            }
            
            // Unregister geometry changed callback
            if (_phSlider != null)
            {
                _phSlider.UnregisterCallback<GeometryChangedEvent>(OnPhSliderGeometryChanged);
            }
            
            // Cleanup texture
            if (_phGradientTexture != null)
            {
                Destroy(_phGradientTexture);
                _phGradientTexture = null;
            }
            
            if (_condensationIdleCoroutine != null)
                StopCoroutine(_condensationIdleCoroutine);
            
            if (_phPulseCoroutine != null)
                StopCoroutine(_phPulseCoroutine);
        }
        
        private void OnEnable()
        {
            // Re-subscribe when enabled
            if (_actionSystem != null)
            {
                _actionSystem.OnActionsChanged += OnActionsChanged;
            }
            
            if (_economySystem != null)
            {
                _economySystem.OnCRYChanged += OnCRYChanged;
            }
            
            // FASE 10: Re-subscribe a CondensationSystem
            if (_gameManager != null)
            {
                _gameManager.OnCondensationChanged += OnCondensationChanged;
            }
            
            // Re-subscribe to ServiceContainer event
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered += OnServiceRegistered;
            }
            
            // Try to reconnect PhSystem
            if (_phSystem == null)
            {
                TryConnectPhSystem();
            }
            else
            {
                _phSystem.OnPhChanged += OnPhChanged;
            }
        }
        
        /// <summary>
        /// FASE 8: Calcola giorni virtuali da condensazione (helper per tooltip).
        /// </summary>
        private float GetVirtualDaysFromCondensation(float percentage)
        {
            if (percentage < 50f)
                return 0f;
            if (percentage < 60f)
                return 0.5f;
            if (percentage < 80f)
                return 1.0f;
            return 1.5f; // 80-100%
        }
        
        /// <summary>
        /// FASE 8: Setup tooltip per condensazione (simile a SetupPhTooltip).
        /// </summary>
        private void SetupCondensationTooltip()
        {
            if (_condensationDisplay == null)
                return;
            
            // Crea tooltip container
            _condensationTooltip = new VisualElement();
            _condensationTooltip.name = "condensation-tooltip";
            _condensationTooltip.style.position = Position.Absolute;
            _condensationTooltip.style.display = DisplayStyle.None;
            _condensationTooltip.style.backgroundColor = new Color(0f, 0f, 0f, 0.9f);
            _condensationTooltip.style.borderTopWidth = 2f;
            _condensationTooltip.style.borderRightWidth = 2f;
            _condensationTooltip.style.borderBottomWidth = 2f;
            _condensationTooltip.style.borderLeftWidth = 2f;
            _condensationTooltip.style.borderTopColor = new Color(0.365f, 0.714f, 0.890f, 1f); // #5DB6E3
            _condensationTooltip.style.borderRightColor = new Color(0.365f, 0.714f, 0.890f, 1f);
            _condensationTooltip.style.borderBottomColor = new Color(0.365f, 0.714f, 0.890f, 1f);
            _condensationTooltip.style.borderLeftColor = new Color(0.365f, 0.714f, 0.890f, 1f);
            _condensationTooltip.style.paddingTop = 8f;
            _condensationTooltip.style.paddingRight = 8f;
            _condensationTooltip.style.paddingBottom = 8f;
            _condensationTooltip.style.paddingLeft = 8f;
            _condensationTooltip.style.width = 320f;
            _condensationTooltip.style.maxWidth = 320f;
            _condensationTooltip.style.minHeight = 100f;
            _condensationTooltip.pickingMode = PickingMode.Position; // Permette click sul button
            
            // Crea testo tooltip
            _condensationTooltipText = new Label();
            _condensationTooltipText.name = "condensation-tooltip-text";
            _condensationTooltipText.style.whiteSpace = WhiteSpace.Normal;
            _condensationTooltipText.style.color = new Color(0.961f, 0.969f, 0.980f, 1f); // Bianco
            _condensationTooltipText.style.fontSize = 16f;
            _condensationTooltipText.style.unityTextAlign = TextAnchor.UpperLeft;
            _condensationTooltipText.enableRichText = true;
            _condensationTooltip.Add(_condensationTooltipText);
            
            // FASE 8: Crea button Collect
            _condensationCollectButton = new Button();
            _condensationCollectButton.name = "condensation-collect-button";
            _condensationCollectButton.text = "Collect";
            _condensationCollectButton.style.marginTop = 8f;
            _condensationCollectButton.style.width = Length.Percent(100f);
            _condensationCollectButton.style.height = 32f;
            _condensationCollectButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f); // Grigio scuro
            _condensationCollectButton.style.color = new Color(0.961f, 0.969f, 0.980f, 1f); // Bianco
            _condensationCollectButton.style.fontSize = 14f;
            _condensationCollectButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            
            // Hover effect
            _condensationCollectButton.RegisterCallback<MouseEnterEvent>(evt =>
            {
                _condensationCollectButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            });
            _condensationCollectButton.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                _condensationCollectButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            });
            
            // Click handler
            _condensationCollectButton.clicked += OnCondensationCollectClicked;
            
            _condensationTooltip.Add(_condensationCollectButton);
            
            // Aggiungi tooltip al root
            _root.Add(_condensationTooltip);
            
            // Setup hover events sul display
            _condensationDisplay.RegisterCallback<MouseEnterEvent>(OnCondensationHoverEnter);
            _condensationDisplay.RegisterCallback<MouseLeaveEvent>(OnCondensationHoverExit);
            _condensationDisplay.RegisterCallback<MouseMoveEvent>(OnCondensationHoverMove);
            
            // Setup hover events sul tooltip (per mantenerlo aperto quando mouse è sopra)
            _condensationTooltip.RegisterCallback<MouseEnterEvent>(OnCondensationTooltipHoverEnter);
            _condensationTooltip.RegisterCallback<MouseLeaveEvent>(OnCondensationTooltipHoverExit);
        }
        
        /// <summary>
        /// FASE 8: Handler hover enter per condensazione display.
        /// </summary>
        private void OnCondensationHoverEnter(MouseEnterEvent evt)
        {
            if (_gameManager != null && _condensationTooltip != null)
            {
                UpdateCondensationTooltipContent();
                _condensationTooltip.style.display = DisplayStyle.Flex;
            }
        }
        
        /// <summary>
        /// FASE 8: Handler hover exit per condensazione display.
        /// Non chiude il tooltip se il mouse è ancora sopra il tooltip stesso.
        /// </summary>
        private void OnCondensationHoverExit(MouseLeaveEvent evt)
        {
            // Non chiudere immediatamente - il tooltip gestirà la chiusura quando il mouse esce anche da lì
            // Questo permette di spostare il mouse dal display al tooltip senza chiuderlo
        }
        
        /// <summary>
        /// FASE 8: Handler hover enter per tooltip condensazione.
        /// Mantiene il tooltip aperto quando il mouse entra nel tooltip.
        /// </summary>
        private void OnCondensationTooltipHoverEnter(MouseEnterEvent evt)
        {
            if (_condensationTooltip != null)
            {
                _condensationTooltip.style.display = DisplayStyle.Flex;
            }
        }
        
        /// <summary>
        /// FASE 8: Handler hover exit per tooltip condensazione.
        /// Chiude il tooltip solo quando il mouse esce anche dal tooltip.
        /// </summary>
        private void OnCondensationTooltipHoverExit(MouseLeaveEvent evt)
        {
            if (_condensationTooltip != null)
            {
                _condensationTooltip.style.display = DisplayStyle.None;
            }
        }
        
        /// <summary>
        /// FASE 8: Handler hover move per condensazione (posizionamento tooltip).
        /// </summary>
        private void OnCondensationHoverMove(MouseMoveEvent evt)
        {
            if (_condensationTooltip != null && _condensationTooltip.style.display == DisplayStyle.Flex && _condensationDisplay != null)
            {
                var displayBounds = _condensationDisplay.worldBound;
                var rootBounds = _root.worldBound;
                
                float tooltipX = displayBounds.xMax + 10f;
                float tooltipY = displayBounds.yMax - 20f;
                
                float tooltipWidth = 320f;
                float tooltipHeight = _condensationTooltip.resolvedStyle.height;
                
                if (tooltipX + tooltipWidth > rootBounds.width)
                {
                    tooltipX = displayBounds.xMin - tooltipWidth - 10f;
                }
                
                if (tooltipY + tooltipHeight > rootBounds.height)
                {
                    tooltipY = displayBounds.yMin - tooltipHeight - 10f;
                }
                
                _condensationTooltip.style.left = tooltipX;
                _condensationTooltip.style.top = tooltipY;
            }
        }
        
        /// <summary>
        /// FASE 8: Aggiorna contenuto tooltip condensazione con informazioni complete.
        /// </summary>
        private void UpdateCondensationTooltipContent()
        {
            if (_gameManager == null || _condensationTooltipText == null || _gameManager.CondensationSystem == null)
                return;
            
            var condensationSystem = _gameManager.CondensationSystem;
            float currentPercentage = condensationSystem.CurrentAccumulation;
            float dailyProduction = condensationSystem.DailyProduction;
            bool hasLed = false;
            
            // Verifica LED attivi (se DayCycleController disponibile)
            var dayCycleController = UnityEngine.Object.FindObjectOfType<DayCycleController>();
            if (dayCycleController != null)
            {
                // Usa reflection o metodo pubblico se disponibile (per ora semplificato
                hasLed = false; // TODO: Aggiungere metodo pubblico HasAnyActiveLed() in DayCycleController
            }
            
            // Calcola giorni virtuali
            float virtualDays = GetVirtualDaysFromCondensation(currentPercentage);
            
            // Calcola previsione produzione domani (stima basata su produzione odierna)
            float tomorrowProduction = dailyProduction; // Stima conservativa
            
            // Calcola reward stimato
            int estimatedRewardMin = 0, estimatedRewardMax = 0;
            if (currentPercentage < 50f)
            {
                estimatedRewardMin = 5;
                estimatedRewardMax = 10;
            }
            else if (currentPercentage < 80f)
            {
                estimatedRewardMin = 15;
                estimatedRewardMax = 25;
            }
            else
            {
                estimatedRewardMin = 30;
                estimatedRewardMax = 40;
            }
            
            var sb = new System.Text.StringBuilder();
            
            // Titolo
            sb.AppendLine($"💧 <color=#5DB6E3><b>CONDENSATION</b></color>");
            sb.AppendLine();
            
            // Current Level
            sb.AppendLine($"<b>CURRENT LEVEL</b> <color=#5DB6E3>{Mathf.RoundToInt(currentPercentage)}%</color>");
            sb.AppendLine();
            
            // Definizione
            sb.AppendLine($"<color=#5DB6E3>Condensation</color> is raw water (WAT-RAW) collected from plant transpiration (0-100%).");
            sb.AppendLine();
            
            // Effetto alto
            if (currentPercentage >= 50f)
            {
                sb.AppendLine($"Above 50%, ambient humidity adds <color=#FF0000>virtual days</color> to the <color=#FF0000>Mold Risk</color> of all plants (up to +1.5d/day).");
                if (virtualDays > 0f)
                {
                    sb.AppendLine($"<color=#FF0000>Currently adding +{virtualDays:F1} virtual days/day</color>");
                }
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine($"Above 50%, ambient humidity adds <color=#FF0000>virtual days</color> to the <color=#FF0000>Mold Risk</color> of all plants (up to +1.5d/day).");
                sb.AppendLine();
            }
            
            // Raccolta
            sb.AppendLine($"Collecting resets the %, removes virtual days, and produces raw water: <color=#FFA500>the longer you wait, the higher the reward but the greater the mold risk</color>.");
            sb.AppendLine();
            
            // Reward stimato
            sb.AppendLine($"<b>Estimated Reward:</b> {estimatedRewardMin}-{estimatedRewardMax} WAT-RAW");
            sb.AppendLine();
            
            // Produzione
            sb.AppendLine($"<b>Today's Production:</b> {dailyProduction:F1}%");
            if (tomorrowProduction > 0f)
            {
                sb.AppendLine($"<b>Tomorrow's Estimate:</b> ~{tomorrowProduction:F1}%");
            }
            sb.AppendLine();
            
            // LED Bonus
            if (hasLed)
            {
                sb.AppendLine($"✨ <color=#00FF00>LED boost active: +2 WAT-RAW</color>");
                sb.AppendLine();
            }
            
            // TIP
            sb.AppendLine($"💡 <color=#00FF00>TIP: Optimal range is 70-85%. Monitor daily to prevent issues.</color>");
            
            _condensationTooltipText.text = sb.ToString();
            
            // FASE 8: Aggiorna stato button Collect (visibile solo se c'è condensazione disponibile)
            if (_condensationCollectButton != null)
            {
                _condensationCollectButton.style.display = currentPercentage > 0f ? DisplayStyle.Flex : DisplayStyle.None;
                _condensationCollectButton.SetEnabled(currentPercentage > 0f);
            }
        }
        
        /// <summary>
        /// FASE 8: Handler click button Collect nel tooltip condensazione.
        /// </summary>
        private void OnCondensationCollectClicked()
        {
            if (_gameManager == null)
                return;
            
            // Raccoglie condensazione
            int reward = _gameManager.CollectCondensation();
            
            if (reward > 0)
            {
                // Aggiunge WAT-RAW all'inventario
                _gameManager.PlayerInventory.Add(Items.Water, reward);
                
                // Mostra notifica toast
                var foundation = Sporae.UI.UIToolkit.NotificationsFoundation.FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                if (foundation != null && foundation.Enabled)
                {
                    foundation.PostToast("WATER-001", new Sporae.UI.UIToolkit.NotificationsFoundation.NotificationPayload().With("amount", reward.ToString()));
                }
                else
                {
                    var toastManager = ServiceContainer.Instance?.Get<ToastNotificationManager>(suppressWarning: true);
                    if (toastManager != null)
                    {
                        toastManager.ShowToast(ToastNotificationType.ResourceGained, $"You collected Rainwater: {reward}!", "WATER-001");
                    }
                }
                
                // Aggiorna tooltip dopo raccolta
                UpdateCondensationTooltipContent();
            }
        }
        
        #pragma warning restore 0414
    }
}

