using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using Sporae.UI.UIToolkit.HUD;
using Sporae.DevTools;
using _Project.Sporae.Core;
using _Project;
using Sporae.Core;

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
        
        // Suppress warning for unused field (used in Inspector)
        #pragma warning disable 0414
        
        // UI Elements
        private VisualElement _root;
        private SegmentedBarUI _actionsBar;
        private Label _actionsValueLabel;
        private VisualElement _phDisplay;
        private Label _phValueLabel;
        private VisualElement _phMarker;
        private VisualElement _phGradient;
        private VisualElement _phNeutralZone;
        private VisualElement _phSlider;
        private Label _condensationValueLabel;
        private Label _mutationValueLabel;
        private Label _cryValueLabel;
        private Label _grateValueLabel;
        
        // Tooltip
        private VisualElement _phTooltip;
        private Label _phTooltipText;
        
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
        
        // Colors
        private readonly Color _greenStable = new Color(0.498f, 1f, 0.478f, 1f); // #7FFF7A
        private readonly Color _yellowWarning = new Color(0.902f, 0.788f, 0.435f, 1f); // #E6C96F
        private readonly Color _redCritical = new Color(0.827f, 0.373f, 0.373f, 1f); // #D35F5F
        private readonly Color _blueInfo = new Color(0.365f, 0.714f, 0.890f, 1f); // #5DB6E3
        
        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
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
                
                // Collega PhSystem
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
                    SporiumLogger.LogWarning(LogCategory.UI, "TopBarController: PhSystem non trovato nel ServiceContainer.");
                }
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "TopBarController: GameManager non trovato. Usando valori mock.");
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
            _phTooltipText.style.fontSize = 11f;
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
            string tooltipContent = $"<b>pH DRIFT</b>\n<b>Banda: {bandName}</b>\n\n{breakdown}";
            
            _phTooltipText.text = tooltipContent;
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
            
            // Query UI elements
            var actionsBarContainer = _root.Q<VisualElement>("actions-bar");
            _actionsValueLabel = _root.Q<Label>("actions-value");
            _phDisplay = _root.Q<VisualElement>("ph-display");
            _phValueLabel = _root.Q<Label>("ph-value");
            _phSlider = _root.Q<VisualElement>("ph-slider");
            _phMarker = _root.Q<VisualElement>("ph-marker");
            _phGradient = _root.Q<VisualElement>("ph-gradient");
            _phNeutralZone = _root.Q<VisualElement>("ph-neutral-zone");
            
            // #region agent log
            var logPath = @"d:\Sporae_Build_Beta\.cursor\debug.log";
            try
            {
                var timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var guid = System.Guid.NewGuid().ToString().Substring(0, 8);
                var logLine = $"{{\"id\":\"log_{timestamp}_{guid}\",\"timestamp\":{timestamp},\"location\":\"TopBarController.cs:InitializeUI\",\"message\":\"pH elements queried\",\"data\":{{\"phDisplayExists\":{(_phDisplay != null).ToString().ToLower()},\"phSliderExists\":{(_phSlider != null).ToString().ToLower()},\"phMarkerExists\":{(_phMarker != null).ToString().ToLower()},\"phGradientExists\":{(_phGradient != null).ToString().ToLower()},\"phNeutralZoneExists\":{(_phNeutralZone != null).ToString().ToLower()}}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"E\"}}\n";
                System.IO.File.AppendAllText(logPath, logLine);
            }
            catch { }
            // #endregion
            _condensationValueLabel = _root.Q<Label>("condensation-value");
            _mutationValueLabel = _root.Q<Label>("mutation-value");
            _cryValueLabel = _root.Q<Label>("cry-value");
            _grateValueLabel = _root.Q<Label>("grate-value");
            
            // Setup tooltip per pH
            SetupPhTooltip();
            
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
            // #region agent log
            var logPath = @"d:\Sporae_Build_Beta\.cursor\debug.log";
            try
            {
                var timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var guid = System.Guid.NewGuid().ToString().Substring(0, 8);
                var logLine = $"{{\"id\":\"log_{timestamp}_{guid}\",\"timestamp\":{timestamp},\"location\":\"TopBarController.cs:UpdatePh\",\"message\":\"UpdatePh called\",\"data\":{{\"value\":{value},\"phLevelBefore\":{_phLevel}}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"A\"}}\n";
                System.IO.File.AppendAllText(logPath, logLine);
            }
            catch { }
            // #endregion
            
            _phLevel = value;
            
            // Converti da range PhSystem (-100/+100) a scala visualizzazione (0-14)
            // Mapping: -100 → 0, 0 → 7, +100 → 14
            float phVisualScale = ((value + 100f) / 200f) * 14f;
            phVisualScale = Mathf.Clamp(phVisualScale, 0f, 14f);
            
            // #region agent log
            try
            {
                var timestamp2 = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var guid2 = System.Guid.NewGuid().ToString().Substring(0, 8);
                var logLine2 = $"{{\"id\":\"log_{timestamp2}_{guid2}\",\"timestamp\":{timestamp2},\"location\":\"TopBarController.cs:UpdatePh\",\"message\":\"pH conversion calculated\",\"data\":{{\"phVisualScale\":{phVisualScale},\"normalizedPos\":{phVisualScale / 14f}}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"B\"}}\n";
                System.IO.File.AppendAllText(logPath, logLine2);
            }
            catch { }
            // #endregion
            
            if (_phValueLabel != null)
            {
                // Mostra pH nella scala 0-14
                _phValueLabel.text = phVisualScale.ToString("F1");
            }
            
            if (_phMarker != null && _phGradient != null)
            {
                // Posiziona marker: (phVisualScale / 14) * 100%
                // Il marker è largo 12px, quindi dobbiamo centrarlo: left = (normalizedPos * 100%) - (6px / sliderWidth)
                float normalizedPos = phVisualScale / 14f;
                
                // #region agent log
                try
                {
                    var timestamp3 = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var guid3 = System.Guid.NewGuid().ToString().Substring(0, 8);
                    var markerLeftBefore = _phMarker.resolvedStyle.left;
                    var sliderWidthLog = _phSlider != null ? _phSlider.resolvedStyle.width : 0f;
                    var logLine3 = $"{{\"id\":\"log_{timestamp3}_{guid3}\",\"timestamp\":{timestamp3},\"location\":\"TopBarController.cs:UpdatePh\",\"message\":\"Setting marker position\",\"data\":{{\"normalizedPos\":{normalizedPos},\"markerLeftPercent\":{normalizedPos * 100f},\"markerLeftBefore\":{markerLeftBefore},\"sliderWidth\":{sliderWidthLog},\"markerExists\":true,\"gradientExists\":true,\"sliderExists\":{(_phSlider != null).ToString().ToLower()}}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"C\"}}\n";
                    System.IO.File.AppendAllText(logPath, logLine3);
                }
                catch { }
                // #endregion
                
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
                
                // #region agent log
                try
                {
                    var timestamp4 = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var guid4 = System.Guid.NewGuid().ToString().Substring(0, 8);
                    var markerLeftAfter = _phMarker.resolvedStyle.left;
                    var markerMarginLeft = _phMarker.resolvedStyle.marginLeft;
                    var logLine4 = $"{{\"id\":\"log_{timestamp4}_{guid4}\",\"timestamp\":{timestamp4},\"location\":\"TopBarController.cs:UpdatePh\",\"message\":\"Marker position set\",\"data\":{{\"markerLeftAfter\":{markerLeftAfter},\"markerMarginLeft\":{markerMarginLeft}}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"D\"}}\n";
                    System.IO.File.AppendAllText(logPath, logLine4);
                }
                catch { }
                // #endregion
                
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
        
        private IEnumerator CondensationIdleAnimation()
        {
            while (true)
            {
                float delay = Random.Range(0.9f, 1.5f);
                yield return new WaitForSeconds(delay);
                
                // Variazione ±1%
                float variation = Random.Range(-1f, 1f);
                float displayValue = Mathf.Clamp(_condensation + variation, 0f, 100f);
                
                if (_condensationValueLabel != null)
                {
                    _condensationValueLabel.text = $"{Mathf.RoundToInt(displayValue)}%";
                }
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
                _mutationValueLabel.text = $"INDEX {_mutationIndex:F2}";
                
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
        
        private void OnDisable()
        {
            // Unsubscribe when disabled
            if (_actionSystem != null)
            {
                _actionSystem.OnActionsChanged -= OnActionsChanged;
            }
            
            if (_economySystem != null)
            {
                _economySystem.OnCRYChanged -= OnCRYChanged;
            }
            
            if (_phSystem != null)
            {
                _phSystem.OnPhChanged -= OnPhChanged;
            }
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
            
            if (_phSystem != null)
            {
                _phSystem.OnPhChanged += OnPhChanged;
            }
        }
        
        #pragma warning restore 0414
    }
}

