using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using _Project.Sporae.Core;
using _Project; // per Interactable
using Sporae.UI.UIToolkit.NotificationsFoundation;
using Sporae.Dome;
using Sporae.Dome.PotSystem;
using Sporae.Dome.PotSystem.Condition;
using Sporae.Dome.PotSystem.Fertilizer;
using Sporae.Dome.PotSystem.Growth; // LedSystemState
using Sporae.Dome.PotSystem.Botanical;
using System.Collections.Generic;
using _Project.Player;
using Sporae.UI.UIToolkit.HUD.Components;
using Sporae.UI.UIToolkit.PlantCard.Helpers;
using Sporae.UI.UIToolkit.PlantCard.Components;
using Sporae.UI.UIToolkit.PlayerInventory;
using Sporae.Core.Localization;
using Sporae.DevTools;
using Sporae.UI.UIToolkit;

namespace Sporae.UI.UIToolkit.PlantCardV3
{
    /// <summary>
    /// PlantCardV3: Terminale retro-DOS per gestione Pot.
    /// MVP: welcome text + parseColors + input START/PROTOCOL/FORECAST placeholder.
    /// Non distruttivo: non tocca flussi esistenti finché non viene cablato in scena.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PlantCardV3TerminalController : MonoBehaviour
    {
        private enum InputState
        {
            Idle,
            SelectingItem,
            SelectingStatusPot,
            ConfirmingPlantDrip,
            ConfirmingCryoSend,
            ConfirmingPlantLed,
            ConfirmingPlantLedType,
            ConfirmingActionToQueue,
            ConfirmingCriticalFertilize,
            ConfirmingExecuteOrDiscardQueue,
            SelectingCryoSlotForExtract,
            SelectingCryoSlotForRestore,
            SelectingTargetPotForRestore
        }

        private enum QueuedActionType
        {
            Plant,
            Fertilize,
            Spray,
            HydrationToggle,
            LedRedToggle,
            LedBlueToggle,
            Prune,
            Harvest,
            Uproot
        }

        private sealed class QueuedAction
        {
            public QueuedActionType Type;
            public string PotId;
            public string TargetLabel;
            public int ApCost;
            public string ItemTypeId; // seed/fertilizer/additive
        }

        private sealed class SelectionContext
        {
            public QueuedActionType Type;
            public string PotId;
            public List<string> OptionsTypeIds = new();
        }

        [Header("References")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private VisualTreeAsset _potCardTemplate;
        [SerializeField] private VisualTreeAsset _detailPageTemplate;

        [Header("Terminal HUD Zona 2 (preview incubator)")]
        [Tooltip("Set di sprite per la preview pianta in Zona 2 (stile incubator). Se non assegnato, la preview usa fallback da pot o rimane vuota.")]
        [SerializeField] private TerminalPotPreviewConfig _terminalPotPreviewConfig;

        [Header("Behavior")]
        [SerializeField] private bool _startVisibleInEditor = false;

        [Header("Backdrop Blur")]
        [SerializeField] private bool _useBlurredBackdrop = true;
        [SerializeField] private bool _outsideShowsGameView = true;
        private const float UnifiedModalDimAlpha = 0.65f;
        [SerializeField, Range(2, 16)] private int _backdropDownsample = 8;
        [SerializeField, Range(1, 6)] private int _backdropBlurRadius = 2;
        [SerializeField, Range(1, 4)] private int _backdropBlurIterations = 2;

        [Header("Console Font")]
        [Tooltip("Optional override for terminal text. Leave empty to inherit the global UI font from the runtime theme.")]
        [SerializeField] private Font _consoleMonoFont;

        [Header("Outer Glow Frame")]
        [SerializeField] private Material _outerGlowMaterial;
        [SerializeField] private bool _outerGlowLiveUpdate = false;

        [Header("Debug")]
        [SerializeField] private bool _logFocusDebug = false;

        [Header("Boot Sequence")]
        [SerializeField] private bool _playBootSequence = true;
        [SerializeField, Range(0.02f, 0.5f)] private float _bootLineDelay = 0.08f;
        [SerializeField, Range(0.05f, 1f)] private float _bootSectionDelay = 0.25f;

        [Header("Typewriter")]
        [SerializeField] private bool _useTypewriterOnCommand = true;
        [SerializeField, Range(0.001f, 0.05f)] private float _typewriterCharDelay = 0.005f;
        [SerializeField, Range(0.05f, 1f)] private float _typewriterLongLineMultiplier = 0.35f;
        [SerializeField, Range(0.05f, 1f)] private float _typewriterFrameLineMultiplier = 0.2f;
        [SerializeField, Range(40, 200)] private int _typewriterLongLineThreshold = 80;
        [SerializeField, Range(100, 2000)] private int _typewriterLongOutputChars = 700;
        [SerializeField, Range(1, 12)] private int _typewriterBlockSizeShort = 1;
        [SerializeField, Range(2, 24)] private int _typewriterBlockSizeLong = 6;
        [Tooltip("Aggiorna la Label ogni N righe (riduce flush e scroll reset su output lunghi).")]
        [SerializeField, Range(1, 10)] private int _typewriterFlushEveryNLines = 3;
        [SerializeField, Range(0.05f, 1f)] private float _typewriterLongOutputMultiplier = 0.2f;
        [SerializeField, Range(0.05f, 1f)] private float _typewriterGlobalSpeedMultiplier = 0.2f;

        [Header("Typewriter SFX")]
        [SerializeField] private AudioSource _typewriterAudioSource;
        [SerializeField] private AudioClip _typewriterSfx;
        [SerializeField] private AudioClip _bootStartSfx;
        [SerializeField, Range(0.01f, 0.2f)] private float _typewriterSfxInterval = 0.035f;

        [Header("Command loading UX (CRT feedback)")]
        [Tooltip("Secondi di attesa prima della risposta per comandi validi.")]
        [SerializeField, Range(0.5f, 3f)] private float _loadingDelaySuccess = 1.8f;
        [Tooltip("Secondi di attesa per errore (comando non valido o argomento mancante).")]
        [SerializeField, Range(0.2f, 1.5f)] private float _loadingDelayError = 0.65f;
        [Tooltip("Secondi di attesa per step successivi (selezione seme, conferma Y/N, esecuzione coda).")]
        [SerializeField, Range(0.2f, 1.5f)] private float _loadingDelayStep = 0.95f;
        [Tooltip("Secondi di attesa per step PLANT (domande WATER ON / LED): più breve per leggere subito le domande.")]
        [SerializeField, Range(0.05f, 0.5f)] private float _loadingDelayPlantFlowStep = 0.2f;

        private VisualElement _root;
        private Button _btnClose;
        private ScrollView _consoleScroll;
        private Label _consoleText;
        private VisualElement _forecastConditionTooltip;
        private Label _forecastConditionTooltipText;
#pragma warning disable CS0414
        private bool _shouldHideForecastConditionTooltip;
#pragma warning restore CS0414
        private VisualElement _forecastHotspotLayer;
        private VisualElement _forecastHoverAnchor;
        private readonly List<ForecastConditionHoverRow> _forecastConditionHoverRows = new();
        private int _forecastHoveredRowIndex = -1;
        private bool _lastOutputWasForecast;
        private ScrollView _protocolScroll;
        private Label _protocolText;
        private VisualElement _consoleView;
        private VisualElement _protocolView;
        private VisualElement _detailView;
        private VisualElement _currentDetailPage;
        private TextField _input;
        
        // Custom scrollbars
        private VisualElement _potListScrollbar;
        private VisualElement _potListScrollbarTrack;
        private VisualElement _potListScrollbarThumb;
        private int _scrollArrowDirection;
        private IVisualElementScheduledItem _scrollArrowRepeatSchedule;
        private Label _inputHintOverlay;
        private VisualElement _promptRoot;
        private VisualElement _blinkCursor;
        private IVisualElementScheduledItem _blinkSchedule;
        private VisualElement _loadingIndicator;
        private Label _loadingSpinnerLabel;
        private IVisualElementScheduledItem _loadingSpinnerSchedule;
        private Coroutine _loadingCoroutine;
        private IVisualElementScheduledItem _loadingBlinkSchedule;
        private bool _loadingBlinkActive;
        private bool _loadingBlinkBright;
        private int _loadingBufferLengthBeforeBlink;
        private string _loadingLine1Plain;
        private string _loadingLine2Plain;
        private Label _apLabel;
        private Label _queuedLabel;
        private ScrollView _potList;
        private VisualElement _backdrop;
        private VisualElement _dimOverlay;
        private Texture2D _backdropTexture;
        private VisualElement _outerGlow;
        private UiGlowFrameGenerator _outerGlowGenerator;
        private Material _outerGlowMaterialRuntime;
        private bool _backdropCapturePending;

        private readonly StringBuilder _consoleBuffer = new();
        private bool _isVisible;
        private Coroutine _bootRoutine;
#pragma warning disable CS0414
        private bool _bootSequenceActive;
#pragma warning restore CS0414
        private bool _typewriterActive;
        private readonly Queue<string> _typewriterQueue = new();
        private Coroutine _typewriterRoutine;
        private float _nextTypewriterSfxTime;
        private float _typewriterCommandSpeedMultiplier = 1f;
        private int _typewriterCommandBlockMultiplier = 1;
        private Coroutine _protocolTypewriterRoutine;

        private readonly System.Collections.Generic.List<QueuedAction> _queue = new();
        private InputState _inputState = InputState.Idle;
        private List<PotSlot> _potsForStatusChoice = new List<PotSlot>();
        private List<CryoSlot> _cryoSlotsForChoice = new List<CryoSlot>();
        private List<PotSlot>  _emptyPotsForChoice  = new List<PotSlot>();
        private string         _pendingCryoSlotId;
        private string         _pendingCryoSendPotId;
        private List<string> _statusLinesCollector;
        private List<string> _pendingStatusSecondHalf;
        private List<string> _pendingStatusResearchNotes;
        private Coroutine _statusSecondHalfRoutine;
        private Coroutine _environmentalPhRefreshRoutine;
        private const int StatusSecondHalfChunkSize = 3;
        private const float StatusSecondHalfChunkDelay = 0.22f;
        private QueuedAction _pendingConfirmAction;
        private bool _pendingPlantDrip;
        private int _pendingPlantLed; // 0 = none, 1 = red, 2 = blue
        private readonly Dictionary<string, int> _reservedItems = new();

        private GameManager _gameManager;
        private FoundationNotificationService _foundation;
        private Inventory _inventory;
        private DayCycleSystem _dayCycleSystem;
        private PhSystem _phSystem;
        private PotSystemConfig _potSystemConfig;
        private DomePotRegistry _potRegistry;

        private SelectionContext _selection;

        // Zona 2 (center HUD) e Zona 3 (vital stats)
        private VisualElement _pcv3Left;
        private VisualElement _pcv3Center;
        private VisualElement _pcv3Right;
        private VisualElement _hudPlantPreview;
        private VisualElement _hudLivePill;
        private VisualElement _hudLiveDot;
        private Label _hudLiveLabel;
        private Label _hudPlantName;
        private Label _hudPlantCode;
        private Label _hudPlantLevel;
        private Label _hudPlantFamily;
        private Label _hudPlantOneliner;
        private Label _hudEffectsSummary;
        private List<VisualElement> _hudPotSlots = new List<VisualElement>(4);
        private VisualElement _vitalBlock1;
        private VisualElement _vitalBlock2;
        private VisualElement _a9QuickPanel;
        private Label _a9SuggestionsLabel;
        private Button _quickWateringButton;
        private Button _quickLedBlueButton;
        private Button _quickLedRedButton;
        private Button _quickFertilizeButton;
        private Button _quickSprayButton;
        private Button _quickPruneButton;
        private bool _quickActionsBound;
        private int _selectedPotIndex = 0;

        private List<PotSlot> _hudPots = new List<PotSlot>(4);
        private bool _liveDotPulseLow;

        /// <summary>Incrementare quando si cambia layout/frame in UI Builder per usare sempre posizioni UXML e non quelle salvate.</summary>
        private const int TerminalLayoutVersion = 2;
        private const string PrefsKeyTerminalLayoutVersion = "Sporium_TerminalPot_LayoutVersion";

        private const string PrefsKeyZona2Left = "Sporium_TerminalPot_Zona2_Left";
        private const string PrefsKeyZona2Top = "Sporium_TerminalPot_Zona2_Top";
        private const string PrefsKeyZona3Block1Left = "Sporium_TerminalPot_Zona3_Block1_Left";
        private const string PrefsKeyZona3Block1Top = "Sporium_TerminalPot_Zona3_Block1_Top";
        private const string PrefsKeyZona3Block2Left = "Sporium_TerminalPot_Zona3_Block2_Left";
        private const string PrefsKeyZona3Block2Top = "Sporium_TerminalPot_Zona3_Block2_Top";
        private const string PrefsKeyBodyLeftLeft = "Sporium_TerminalPot_Body_Left_Left";
        private const string PrefsKeyBodyLeftTop = "Sporium_TerminalPot_Body_Left_Top";
        private const string PrefsKeyBodyCenterLeft = "Sporium_TerminalPot_Body_Center_Left";
        private const string PrefsKeyBodyCenterTop = "Sporium_TerminalPot_Body_Center_Top";
        private const string PrefsKeyBodyRightLeft = "Sporium_TerminalPot_Body_Right_Left";
        private const string PrefsKeyBodyRightTop = "Sporium_TerminalPot_Body_Right_Top";
        private VisualElement _draggingElement;
        private Vector2 _dragStartMouse;
        private float _dragStartLeft;
        private float _dragStartTop;
        private EventCallback<MouseMoveEvent> _dragMoveCallback;
        private EventCallback<MouseUpEvent> _dragUpCallback;

        [Header("Safety")]
        [SerializeField] private bool suppressDebugConsolesWhileTerminalOpen = true;
        private readonly List<(Behaviour comp, bool wasEnabled)> _suppressedDebugBehaviours = new();

        [SerializeField] private bool hideOtherUiWhileTerminalOpen = true;
        // IMPORTANT: We hide other UI by toggling UIDocument root visibility (NOT by disabling Behaviours),
        // to avoid OnEnable/Awake side-effects that can re-open menus (Seed selector, etc.) on restore.
        private readonly List<(UIDocument doc, DisplayStyle display, PickingMode picking)> _suppressedUiDocuments = new();

        // Prevent re-open/reset caused by Interactable hotkey (es. tasto E) mentre il terminale è visibile.
        private readonly List<(Interactable comp, bool wasEnabled)> _suppressedInteractables = new();

        // Input capture
        private PlayerClickMover2D _playerMover;
        private bool _wasPlayerMoverSuspended;
        private PlayerPerspectiveMover2D _playerPerspectiveMover;
        private PlayerMoverRouter2D _playerMoverRouter;
        private Sporae.Dome.PotAutomation.PotAutomationRunner _automationRunner;
        private bool _wasPerspectiveMoverEnabled;
        private bool _wasRouterEnabled;
        private bool _waitingForRuntimeServices;

        // Focus guard: make terminal always "type-ready" without mouse clicks.
        // Keeps blast radius minimal by only acting while terminal is visible.
        private bool _focusGuardInstalled;
        private IVisualElementScheduledItem _refocusSchedule;
        private int _focusDebugSeq;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();

            // Mettiamo il Terminale sopra HUD (50) e sopra PlantCardV2 (300).
            // Sopra HUD (50) e altri pannelli: qui usiamo 600 per farlo modal.
            if (_uiDocument != null)
                _uiDocument.sortingOrder = 600;

            _root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (_root == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "PlantCardV3TerminalController: rootVisualElement non trovato!");
                return;
            }

            _btnClose = _root.Q<Button>("pcv3-close-button");
            _consoleScroll = _root.Q<ScrollView>("pcv3-console-scroll");
            if (_consoleScroll != null)
            {
                _consoleScroll.RegisterCallback<AttachToPanelEvent>(_ => ApplyConsoleScrollbarStyle());
                _consoleScroll.schedule.Execute(ApplyConsoleScrollbarStyle).ExecuteLater(2);
            }
            _consoleText = _root.Q<Label>("pcv3-console-text");
            _protocolScroll = _root.Q<ScrollView>("pcv3-protocol-scroll");
            _protocolText = _root.Q<Label>("pcv3-protocol-text");
            _consoleView = _root.Q<VisualElement>("pcv3-console-view");
            _protocolView = _root.Q<VisualElement>("pcv3-protocol-view");
            _detailView = _root.Q<VisualElement>("pcv3-detail-view");
            _promptRoot = _root.Q<VisualElement>("pcv3-prompt");
            _input = _root.Q<TextField>("pcv3-input");
            _loadingIndicator = _root.Q<VisualElement>("pcv3-loading-indicator");
            _loadingSpinnerLabel = _root.Q<Label>("pcv3-loading-spinner");
            _apLabel = _root.Q<Label>("pcv3-ap-label");
            _queuedLabel = _root.Q<Label>("pcv3-queued-label");
            _potList = _root.Q<ScrollView>("pcv3-potlist");
            _backdrop = _root.Q<VisualElement>("pcv3-backdrop");
            _dimOverlay = _root.Q<VisualElement>("pcv3-dim");
            _outerGlow = _root.Q<VisualElement>("pcv3-outer-glow");

            // Zona 2 (center) e Zona 3 (left)
            _pcv3Left = _root.Q<VisualElement>("pcv3-left");
            _pcv3Center = _root.Q<VisualElement>("pcv3-center");
            _pcv3Right = _root.Q<VisualElement>("pcv3-right");
            _hudPlantPreview = _root.Q<VisualElement>("pcv3-hud-plant-preview");
            _hudLivePill = _root.Q<VisualElement>("pcv3-hud-live-pill");
            _hudLiveDot = _root.Q<VisualElement>("pcv3-hud-live-dot");
            _hudLiveLabel = _root.Q<Label>("pcv3-hud-live-label");
            _hudPlantName = _root.Q<Label>("pcv3-hud-plant-name");
            _hudPlantCode = _root.Q<Label>("pcv3-hud-plant-code");
            _hudPlantLevel = _root.Q<Label>("pcv3-hud-plant-level");
            _hudPlantFamily = _root.Q<Label>("pcv3-hud-plant-family");
            _hudPlantOneliner = _root.Q<Label>("pcv3-hud-plant-oneliner");
            _hudEffectsSummary = _root.Q<Label>("pcv3-hud-effects");
            _hudPotSlots.Clear();
            for (int i = 0; i < 4; i++)
            {
                var slot = _root.Q<VisualElement>($"pcv3-hud-pot-slot-{i}");
                if (slot != null) _hudPotSlots.Add(slot);
            }
            _vitalBlock1 = _root.Q<VisualElement>("pcv3-vital-stats-inner-1");
            _vitalBlock2 = _root.Q<VisualElement>("pcv3-vital-stats-inner-2");
            _a9QuickPanel = _root.Q<VisualElement>("pcv3-a9-quick-panel");
            _a9SuggestionsLabel = _root.Q<Label>("pcv3-a9-suggestions");
            _quickWateringButton = _root.Q<Button>("pcv3-quick-watering");
            _quickLedBlueButton = _root.Q<Button>("pcv3-quick-led-blue");
            _quickLedRedButton = _root.Q<Button>("pcv3-quick-led-red");
            _quickFertilizeButton = _root.Q<Button>("pcv3-quick-fertilize");
            _quickSprayButton = _root.Q<Button>("pcv3-quick-spray");
            _quickPruneButton = _root.Q<Button>("pcv3-quick-prune");

            // Custom scrollbars (opzionale se potlist rimosso)
            _potListScrollbar = _root.Q<VisualElement>("pcv3-potlist-scrollbar");
            _potListScrollbarTrack = _root.Q<VisualElement>("pcv3-potlist-scrollbar-track");
            _potListScrollbarThumb = _root.Q<VisualElement>("pcv3-potlist-scrollbar-thumb");

            // Inizializza scrollbar custom
            InitializeCustomScrollbars();
            SetupZona2AndZona3();

            ApplyConsoleScrollbarStyle();

            RegisterConsoleMouseWheelScroll();

            ApplyConsoleFont();

            if (_consoleText != null)
                _consoleText.enableRichText = true;
            if (_protocolText != null)
                _protocolText.enableRichText = true;
            EnsureForecastConditionTooltip();
            StartLiveDotPulse();

            if (_btnClose != null)
                _btnClose.clicked += RequestClose;

            // Click anywhere on terminal should re-focus command input (except when clicking on scrollbar: allow arrow/thumb interaction)
            _root.RegisterCallback<MouseDownEvent>(evt =>
            {
                var target = evt.target as VisualElement;
                if (target != null && IsDescendantOfConsoleScroller(target))
                    return;
                FocusInput();
            }, TrickleDown.TrickleDown);

            if (_input != null)
            {
                _input.RegisterCallback<KeyDownEvent>(OnInputKeyDown);
                _input.RegisterValueChangedCallback(evt =>
                {
                });
                _input.RegisterCallback<FocusInEvent>(_ =>
                {
                });
                _input.RegisterCallback<FocusOutEvent>(_ =>
                {
                });
                // Placeholder (UX)
                _input.value = string.Empty;
                ForceInputColorsRuntime();
            }

            // Enforce stable layout for prompt/input to prevent collapse/drift.
            ApplyPromptLayoutGuard();
            ApplyRightLayoutGuard();

            EnsureInputHintOverlay();
            EnsureBlinkCursor();
            InstallFocusGuard();

            SetupOuterGlowFrame();

            // Hide by default
            SetVisible(_startVisibleInEditor);
        }

        private void ApplyConsoleFont()
        {
            Font font = _consoleMonoFont;
            if (font == null) return;

            if (_consoleText != null)
                _consoleText.style.unityFont = font;
            if (_protocolText != null)
                _protocolText.style.unityFont = font;
        }

        private void LogLayoutSnapshot(string tag)
        {
            try
            {
                var win = _root?.Q<VisualElement>("pcv3-window");
                var prompt = _promptRoot;
                var input = _input;
                var scroll = _consoleScroll;

                string json = "{"
                              + "\"tag\":\"" + tag + "\""
                              + ",\"win\":{\"y\":" + (win?.layout.y ?? -1f) + ",\"h\":" + (win?.layout.height ?? -1f) + "}"
                              + ",\"prompt\":{\"y\":" + (prompt?.layout.y ?? -1f) + ",\"h\":" + (prompt?.layout.height ?? -1f) + "}"
                              + ",\"input\":{\"y\":" + (input?.layout.y ?? -1f) + ",\"h\":" + (input?.layout.height ?? -1f) + ",\"display\":\"" + (input?.resolvedStyle.display.ToString() ?? "null") + "\",\"picking\":\"" + (input?.pickingMode.ToString() ?? "null") + "\"}"
                              + ",\"scroll\":{\"y\":" + (scroll?.layout.y ?? -1f) + ",\"h\":" + (scroll?.layout.height ?? -1f) + ",\"contentH\":" + (scroll?.contentContainer?.layout.height ?? -1f) + "}"
                              + "}";
            }
            catch { }
        }

        private void DumpResolvedLayout(string tag)
        {
            try
            {
                var win = _root?.Q<VisualElement>("pcv3-window");
                var header = _root?.Q<VisualElement>("pcv3-header");
                var left = _root?.Q<VisualElement>("pcv3-left");
                var right = _root?.Q<VisualElement>("pcv3-right");
                var prompt = _promptRoot;
                var input = _input;
                var console = _consoleView;

                string json = "{"
                              + "\"tag\":\"" + tag + "\""
                              + ",\"root\":{\"w\":" + (_root?.resolvedStyle.width ?? -1f) + ",\"h\":" + (_root?.resolvedStyle.height ?? -1f) + "}"
                              + ",\"window\":{\"w\":" + (win?.resolvedStyle.width ?? -1f) + ",\"h\":" + (win?.resolvedStyle.height ?? -1f) + "}"
                              + ",\"header\":{\"h\":" + (header?.resolvedStyle.height ?? -1f) + "}"
                              + ",\"left\":{\"w\":" + (left?.resolvedStyle.width ?? -1f) + "}"
                              + ",\"right\":{\"w\":" + (right?.resolvedStyle.width ?? -1f) + ",\"h\":" + (right?.resolvedStyle.height ?? -1f) + "}"
                              + ",\"console\":{\"h\":" + (console?.resolvedStyle.height ?? -1f) + "}"
                              + ",\"prompt\":{\"h\":" + (prompt?.resolvedStyle.height ?? -1f) + "}"
                              + ",\"input\":{\"h\":" + (input?.resolvedStyle.height ?? -1f) + ",\"font\":" + (input?.resolvedStyle.fontSize ?? -1f) + "}"
                              + "}";
            }
            catch { }
        }

        private void ApplyPromptLayoutGuard()
        {
            // Fix sizes to avoid cumulative shrink when layout recalculates.
            if (_promptRoot != null)
            {
                _promptRoot.style.flexShrink = 0;
                _promptRoot.style.height = 44;
                _promptRoot.style.minHeight = 44;
                _promptRoot.style.maxHeight = 44;
                _promptRoot.style.alignItems = Align.Center;
            }

            if (_input != null)
            {
                _input.style.flexShrink = 0;
                _input.style.height = 44;
                _input.style.minHeight = 44;
                _input.style.maxHeight = 44;
                _input.style.marginTop = 0;
                _input.style.marginBottom = 0;
                _input.style.paddingLeft = 0;
                _input.style.paddingRight = 0;
                _input.style.paddingTop = 10;
                _input.style.paddingBottom = 10;
                _input.style.unityTextAlign = TextAnchor.MiddleLeft;
                _input.style.whiteSpace = WhiteSpace.NoWrap;
                _input.style.overflow = Overflow.Hidden;
                _input.style.flexGrow = 1;
                _input.style.minWidth = 0;
            }
        }

        private void InstallFocusGuard()
        {
            if (_focusGuardInstalled) return;
            if (_root == null || _input == null) return;
            _focusGuardInstalled = true;

            // When the UI attaches to panel (can happen after Awake/Start), refocus.
            _root.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                FocusDebug("AttachToPanelEvent");
                if (_isVisible) RequestRefocusSoon();
            });

            // If TextField (or its internal input) loses focus, restore it while terminal is open.
            _input.RegisterCallback<FocusOutEvent>(_ =>
            {
                FocusDebug("TextField FocusOutEvent");
                if (_isVisible) RequestRefocusSoon();
            });

            // If any key is pressed while terminal is open, ensure focus is on input.
            // TrickleDown so we catch it even if something else temporarily captures focus.
            _root.RegisterCallback<KeyDownEvent>(_ =>
            {
                if (!_isVisible) return;
                if (!IsFocusWithinInput())
                {
                    FocusDebug("KeyDownEvent: focus NOT within input -> refocus");
                    RequestRefocusSoon();
                }
            }, TrickleDown.TrickleDown);

            // Optional: log focus-in as well (useful to see if focus ever reaches internal element).
            _input.RegisterCallback<FocusInEvent>(_ => { FocusDebug("TextField FocusInEvent"); });
        }

        private void SetupOuterGlowFrame()
        {
            if (_outerGlow == null) return;

            if (_outerGlowMaterial == null)
            {
                var shader = Shader.Find("Sporae/UI/GlowFrame");
                if (shader != null)
                    _outerGlowMaterial = new Material(shader);
            }

            if (_outerGlowMaterial == null) return;

            _outerGlowMaterialRuntime = new Material(_outerGlowMaterial);
            ApplyOuterGlowDefaults(_outerGlowMaterialRuntime);
            _outerGlowGenerator = new UiGlowFrameGenerator(_outerGlow, _outerGlowMaterialRuntime);
            _outerGlowGenerator.Render();
        }

        private static void ApplyOuterGlowDefaults(Material mat)
        {
            if (mat == null) return;
            mat.SetFloat("_GradStrength", 0.0f);
            mat.SetFloat("_BorderThickness", 0.0f);
            mat.SetFloat("_BorderSoftness", 1.0f);
            mat.SetFloat("_EdgeMode", 0.0f);
            mat.SetFloat("_GlowMode", 1.0f); // outward only
            mat.SetFloat("_InnerPad", 0.0f);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void FocusDebugEditorOnly(string msg) { FocusDebug(msg); }

        private void FocusDebug(string msg)
        {
            if (!_logFocusDebug) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            try
            {
                _focusDebugSeq++;

                string focusedName = "null";
                string focusedType = "null";
                var fc = _input != null ? _input.focusController : null;
                var focused = fc != null ? fc.focusedElement : null;
                if (focused != null)
                {
                    focusedType = focused.GetType().Name;
                    if (focused is VisualElement ve)
                        focusedName = string.IsNullOrEmpty(ve.name) ? "(no-name)" : ve.name;
                    else
                        focusedName = "(non-VisualElement)";
                }

                SporiumLogger.LogDebug(LogCategory.UI, $"[PlantCardV3TerminalController][FocusDebug #{_focusDebugSeq}] {msg} | visible={_isVisible} | focused={focusedType}:{focusedName}");
            }
            catch
            {
                // never break gameplay due to debug logging
            }
#endif
        }

        private bool IsFocusWithinInput()
        {
            if (_input == null) return false;
            var fc = _input.focusController;
            var focused = fc != null ? fc.focusedElement : null;
            if (focused == null) return false;
            if (ReferenceEquals(focused, _input)) return true;
            // UI Toolkit often focuses the internal text element instead of the TextField itself.
            if (focused is VisualElement ve)
                return _input.Contains(ve);
            return false;
        }

        private VisualElement TryGetInternalTextInputElement()
        {
            if (_input == null) return null;
            // Common internal classes across Unity versions:
            // - unity-text-input (older)
            // - unity-base-text-field__input (newer)
            return _input.Q<VisualElement>(className: "unity-text-input")
                   ?? _input.Q<VisualElement>(className: "unity-base-text-field__input");
        }

        private void ForceFocusCommandInput()
        {
            if (_input == null) return;
            FocusDebug("ForceFocusCommandInput()");
            _input.Focus();
            TryGetInternalTextInputElement()?.Focus();
        }

        private void RequestRefocusSoon(int delayMs = 0)
        {
            if (_input == null) return;

            // Avoid scheduling multiple refocus jobs.
            _refocusSchedule?.Pause();
            _refocusSchedule = _input.schedule.Execute(() =>
            {
                if (_isVisible && _input != null)
                {
                    FocusDebug("RequestRefocusSoon(): tick1");
                    ForceFocusCommandInput();
                }

                // Second attempt shortly after (layout/focus race on some Unity versions)
                if (_input != null)
                {
                    _input.schedule.Execute(() =>
                    {
                        if (_isVisible && _input != null && !IsFocusWithinInput())
                        {
                            FocusDebug("RequestRefocusSoon(): tick2 (16ms) still not focused -> ForceFocus");
                            ForceFocusCommandInput();
                        }
                    }).ExecuteLater(16);
                }
            });
            _refocusSchedule.ExecuteLater(delayMs);
        }

        private void ForceInputColorsRuntime()
        {
            // Some Unity versions ignore USS overrides for TextField internal input, leaving it white.
            // Force it at runtime to ensure: black background + green text.
            if (_input == null) return;

            void ApplyTo(VisualElement ve)
            {
                if (ve == null) return;
                ve.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0f));
                ve.style.color = new StyleColor(new Color(127f / 255f, 255f / 255f, 122f / 255f, 1f));
                ve.style.borderLeftWidth = 0;
                ve.style.borderRightWidth = 0;
                ve.style.borderTopWidth = 0;
                ve.style.borderBottomWidth = 0;
                ve.style.overflow = Overflow.Hidden;
                ve.style.whiteSpace = WhiteSpace.NoWrap;
            }

            // Container
            ApplyTo(_input);

            // Common internal classes across Unity versions:
            var a = _input.Q<VisualElement>(className: "unity-text-input");
            var b = _input.Q<VisualElement>(className: "unity-base-text-field__input");
            ApplyTo(a);
            ApplyTo(b);
        }

        private void ApplyRightLayoutGuard()
        {
            // Keep the console/protocol/detail filling the space, prompt fixed height.
            if (_consoleView != null)
            {
                _consoleView.style.flexGrow = 1;
                _consoleView.style.flexShrink = 1;
                _consoleView.style.minHeight = 0;
            }
            if (_protocolView != null)
            {
                _protocolView.style.flexGrow = 1;
                _protocolView.style.flexShrink = 1;
                _protocolView.style.minHeight = 0;
            }
            if (_detailView != null)
            {
                _detailView.style.flexGrow = 1;
                _detailView.style.flexShrink = 1;
                _detailView.style.minHeight = 0;
            }
        }

        private void EnsureInputHintOverlay()
        {
            // We want: `> Type START for commands...` visually on the same input row when empty.
            // UI Toolkit TextField has no reliable placeholder across Unity versions, so we overlay a Label.
            if (_promptRoot == null || _input == null) return;

            _inputHintOverlay = _promptRoot.Q<Label>("pcv3-input-hint");
            if (_inputHintOverlay == null)
            {
                // NOTE: the ">" is already rendered by pcv3-prompt-prefix in UXML. Spacing + START in colore comando.
                const string cmdColor = "#5DB6E3";
                _inputHintOverlay = new Label("  Digita <color=" + cmdColor + ">START</color> per l'elenco comandi...");
                _inputHintOverlay.name = "pcv3-input-hint";
                _inputHintOverlay.pickingMode = PickingMode.Ignore;
                _inputHintOverlay.enableRichText = true;
                _promptRoot.Add(_inputHintOverlay);
            }

            // Style it to match the mock (green-ish, subtle) and keep it in the prompt row.
            _inputHintOverlay.style.position = Position.Absolute;
            // prefix ">" + margine: distanziare bene il testo dal carattere >
            _inputHintOverlay.style.left = 24;
            _inputHintOverlay.style.top = 0;
            _inputHintOverlay.style.bottom = 0;
            _inputHintOverlay.style.unityTextAlign = TextAnchor.MiddleLeft;
            _inputHintOverlay.style.color = new StyleColor(new Color(127f / 255f, 255f / 255f, 122f / 255f, 0.75f));
            _inputHintOverlay.style.unityFontStyleAndWeight = FontStyle.Normal;
            _inputHintOverlay.style.fontSize = 12;

            void Refresh()
            {
                bool show = string.IsNullOrEmpty(_input.value);
                _inputHintOverlay.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // When player types, hide it; when cleared, show again.
            _input.RegisterValueChangedCallback(_ => Refresh());
            _input.RegisterCallback<FocusInEvent>(_ => Refresh());
            _input.RegisterCallback<FocusOutEvent>(_ => Refresh());
            Refresh();
        }

        private void EnsureBlinkCursor()
        {
            if (_promptRoot == null) return;

            _blinkCursor = _promptRoot.Q<VisualElement>("pcv3-blink-cursor");
            if (_blinkCursor == null)
            {
                _blinkCursor = new VisualElement();
                _blinkCursor.name = "pcv3-blink-cursor";
                _blinkCursor.pickingMode = PickingMode.Ignore;
                _promptRoot.Add(_blinkCursor);
            }

            // DOS-like block cursor on the right side of the prompt row
            _blinkCursor.style.position = Position.Absolute;
            _blinkCursor.style.right = 14;
            _blinkCursor.style.top = 12;
            _blinkCursor.style.width = 10;
            _blinkCursor.style.height = 18;
            _blinkCursor.style.backgroundColor = new StyleColor(new Color(127f / 255f, 255f / 255f, 122f / 255f, 0.85f));
            _blinkCursor.style.display = DisplayStyle.Flex;
        }

        private void StartBlinkCursor()
        {
            if (_blinkCursor == null) return;
            if (_blinkSchedule != null) return;

            bool on = true;
            _blinkSchedule = _blinkCursor.schedule.Execute(() =>
            {
                if (_blinkCursor == null) return;
                _blinkCursor.style.opacity = on ? 1f : 0f;
                on = !on;
            }).Every(500);
        }

        private void StopBlinkCursor()
        {
            if (_blinkCursor != null)
                _blinkCursor.style.opacity = 0f;

            _blinkSchedule?.Pause();
            _blinkSchedule = null;
        }

        private void Start()
        {
            ResolveRuntimeDependencies();
            _foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            _inventory = _gameManager != null ? _gameManager.PlayerInventory : null;
            _potSystemConfig = Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");
            if (_potSystemConfig == null)
            {
                var allConfigs = Resources.LoadAll<PotSystemConfig>("Configs");
                if (allConfigs != null && allConfigs.Length > 0)
                    _potSystemConfig = allConfigs[0];
            }
            SubscribeToRuntimeServicesIfNeeded();
            GameLanguageSettings.OnLanguageChanged += OnPlantCardLanguageChanged;

            if (_isVisible)
            {
                RenderWelcome(clearConsole: true);
                RefreshHeader();
                RefreshSidebar();
            }

            // Safety: se siamo nella stessa Canvas della HUD, forza PlantCardV3 dopo TopBar/BottomNav nella gerarchia.
            TryMoveAfterHud();
        }

        private void OnPlantCardLanguageChanged(GameLanguage _)
        {
            if (_isVisible)
                RefreshHudFromSelectedPot();
        }

        private void ResolveRuntimeDependencies()
        {
            _gameManager = _gameManager ?? ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            _dayCycleSystem = _dayCycleSystem ?? ServiceContainer.Instance?.Get<DayCycleSystem>(suppressWarning: true);
            _phSystem = _phSystem ?? ServiceContainer.Instance?.Get<PhSystem>(suppressWarning: true);
            _potRegistry = _potRegistry ?? ServiceContainer.Instance?.Get<DomePotRegistry>(suppressWarning: true);
            _playerMover = _playerMover ?? ServiceContainer.Instance?.Get<PlayerClickMover2D>(suppressWarning: true);
            _playerPerspectiveMover = _playerPerspectiveMover ?? ServiceContainer.Instance?.Get<PlayerPerspectiveMover2D>(suppressWarning: true);
            _playerMoverRouter = _playerMoverRouter ?? ServiceContainer.Instance?.Get<PlayerMoverRouter2D>(suppressWarning: true);
            _automationRunner = _automationRunner ?? ServiceContainer.Instance?.Get<Sporae.Dome.PotAutomation.PotAutomationRunner>(suppressWarning: true);

            // Fallback temporanei per scene non ancora migrate completamente.
            _gameManager = _gameManager ?? FindObjectOfType<GameManager>();
            _playerMover = _playerMover ?? FindObjectOfType<PlayerClickMover2D>();
            _playerPerspectiveMover = _playerPerspectiveMover ?? FindObjectOfType<PlayerPerspectiveMover2D>();
            _playerMoverRouter = _playerMoverRouter ?? FindObjectOfType<PlayerMoverRouter2D>();
            _automationRunner = _automationRunner ?? FindObjectOfType<Sporae.Dome.PotAutomation.PotAutomationRunner>();

            if (_gameManager != null && _inventory == null)
                _inventory = _gameManager.PlayerInventory;
        }

        private void SubscribeToRuntimeServicesIfNeeded()
        {
            if (ServiceContainer.Instance == null || _waitingForRuntimeServices)
                return;

            bool needsLateBinding = _gameManager == null
                || _dayCycleSystem == null
                || _phSystem == null
                || _potRegistry == null
                || _playerMover == null
                || _playerPerspectiveMover == null
                || _playerMoverRouter == null
                || _automationRunner == null;

            if (!needsLateBinding)
                return;

            ServiceContainer.Instance.OnServiceRegistered += OnServiceRegistered;
            _waitingForRuntimeServices = true;
        }

        private void OnServiceRegistered(object service)
        {
            if (service is GameManager gm && _gameManager == null)
            {
                _gameManager = gm;
                _inventory = gm.PlayerInventory;
            }
            else if (service is DayCycleSystem dayCycle && _dayCycleSystem == null)
            {
                _dayCycleSystem = dayCycle;
            }
            else if (service is PhSystem phSystem && _phSystem == null)
            {
                _phSystem = phSystem;
            }
            else if (service is DomePotRegistry potRegistry && _potRegistry == null)
            {
                _potRegistry = potRegistry;
            }
            else if (service is PlayerClickMover2D playerMover && _playerMover == null)
            {
                _playerMover = playerMover;
            }
            else if (service is PlayerPerspectiveMover2D perspectiveMover && _playerPerspectiveMover == null)
            {
                _playerPerspectiveMover = perspectiveMover;
            }
            else if (service is PlayerMoverRouter2D moverRouter && _playerMoverRouter == null)
            {
                _playerMoverRouter = moverRouter;
            }
            else if (service is Sporae.Dome.PotAutomation.PotAutomationRunner automationRunner && _automationRunner == null)
            {
                _automationRunner = automationRunner;
            }

            if (_gameManager != null
                && _dayCycleSystem != null
                && _phSystem != null
                && _potRegistry != null
                && _playerMover != null
                && _playerPerspectiveMover != null
                && _playerMoverRouter != null
                && _automationRunner != null)
            {
                UnsubscribeFromRuntimeServices();
            }
        }

        private void UnsubscribeFromRuntimeServices()
        {
            if (!_waitingForRuntimeServices || ServiceContainer.Instance == null)
                return;

            ServiceContainer.Instance.OnServiceRegistered -= OnServiceRegistered;
            _waitingForRuntimeServices = false;
        }

        private void InitializeCustomScrollbars()
        {
            // Setup scrollbar per pot list
            if (_potList != null && _potListScrollbar != null && _potListScrollbarTrack != null && _potListScrollbarThumb != null)
            {
                SetupScrollbar(_potList, _potListScrollbar, _potListScrollbarTrack, _potListScrollbarThumb);
            }
        }

        /// <summary>
        /// Applica lo stile alla scrollbar della console (Terminal Pot):
        /// thumb verde stondato, niente linea gialla (track nascosto), frecce verdi visibili senza background, scrollbar più stretta.
        /// </summary>
        private void ApplyConsoleScrollbarStyle()
        {
            if (_consoleScroll == null) return;

            var vScroller = _consoleScroll.verticalScroller;
            if (vScroller == null) return;

            const float greenR = 127f / 255f, greenG = 255f / 255f, greenB = 122f / 255f;
            var green = new Color(greenR, greenG, greenB, 1f);

            // Non forzare width qui: l'USS definisce track 20px e thumb 12px centrato; override a 8px faceva sforare il thumb a destra.
            // vScroller.style.width = 8;

            void ApplyToThumb(VisualElement el)
            {
                if (el == null) return;
                el.style.backgroundColor = green;
                el.style.borderTopLeftRadius = el.style.borderTopRightRadius = 8;
                el.style.borderBottomLeftRadius = el.style.borderBottomRightRadius = 8;
                el.style.borderLeftWidth = el.style.borderRightWidth = el.style.borderTopWidth = el.style.borderBottomWidth = 0;
            }

            var thumb = vScroller.Q<VisualElement>(className: "unity-scroller__thumb");
            if (thumb != null)
            {
                ApplyToThumb(thumb);
                thumb.pickingMode = PickingMode.Position;
            }
            else
            {
                var tracker = vScroller.Q<VisualElement>(className: "unity-base-slider__tracker");
                var dragger = vScroller.Q<VisualElement>(className: "unity-base-slider__dragger");
                if (dragger != null)
                {
                    ApplyToThumb(dragger);
                    dragger.pickingMode = PickingMode.Position;
                    if (tracker != null && tracker != dragger)
                        tracker.style.display = DisplayStyle.None;
                }
                else if (tracker != null)
                {
                    ApplyToThumb(tracker);
                    tracker.pickingMode = PickingMode.Position;
                }
            }
            vScroller.pickingMode = PickingMode.Position;

            var track = vScroller.Q<VisualElement>(className: "unity-base-slider__track");
            if (track != null)
                track.style.display = DisplayStyle.None;

            var slider = vScroller.Q<VisualElement>(className: "unity-scroller__slider");
            if (slider != null)
            {
                slider.style.backgroundColor = Color.clear;
                slider.style.borderLeftWidth = slider.style.borderRightWidth = slider.style.borderTopWidth = slider.style.borderBottomWidth = 0;
            }

            foreach (var btn in new[] { vScroller.highButton, vScroller.lowButton })
            {
                if (btn == null) continue;
                btn.style.backgroundColor = Color.clear;
                btn.style.borderLeftWidth = btn.style.borderRightWidth = btn.style.borderTopWidth = btn.style.borderBottomWidth = 0;
                btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius = btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 4;
                btn.style.unityBackgroundImageTintColor = green;
            }

            RegisterConsoleScrollbarArrows(vScroller);
        }

        /// <summary>
        /// Abilita scroll tramite click e hold sulle frecce su/giù della scrollbar della console.
        /// </summary>
        private void RegisterConsoleScrollbarArrows(Scroller vScroller)
        {
            if (vScroller == null || _consoleScroll == null) return;

            float ScrollStepPx()
            {
                float viewportH = _consoleScroll.contentViewport?.resolvedStyle.height ?? 400f;
                return Mathf.Max(80f, viewportH * 0.35f);
            }

            void ScrollBy(float deltaPx)
            {
                var vs = _consoleScroll.verticalScroller;
                if (vs == null) return;
                float contentH = _consoleScroll.contentContainer.layout.height;
                float viewportH = _consoleScroll.contentViewport?.layout.height ?? 400f;
                float range = vs.highValue - vs.lowValue;
                if (range <= 0 || contentH <= viewportH) return;
                float step = deltaPx * range / (contentH - viewportH);
                vs.value = Mathf.Clamp(vs.value + step, vs.lowValue, vs.highValue);
            }

            void StartScrollRepeat(int direction, VisualElement button, PointerDownEvent evt)
            {
                if (_scrollArrowRepeatSchedule != null) _scrollArrowRepeatSchedule.Pause();
                _scrollArrowDirection = direction;
                button.CapturePointer(evt.pointerId);
                float step = ScrollStepPx() * 0.5f;
                _scrollArrowRepeatSchedule = button.schedule.Execute(() =>
                {
                    if (_scrollArrowDirection == 0) return;
                    ScrollBy(_scrollArrowDirection * step);
                }).Every(120).Until(() => _scrollArrowDirection == 0);
            }

            void StopScrollRepeat(VisualElement button, PointerUpEvent evt)
            {
                _scrollArrowDirection = 0;
                button.ReleasePointer(evt.pointerId);
                _scrollArrowRepeatSchedule?.Pause();
            }

            void StopScrollRepeatLeave(VisualElement button, PointerLeaveEvent evt)
            {
                if (button.HasPointerCapture(evt.pointerId))
                {
                    _scrollArrowDirection = 0;
                    button.ReleasePointer(evt.pointerId);
                    _scrollArrowRepeatSchedule?.Pause();
                }
            }

            if (vScroller.highButton != null)
            {
                var upBtn = vScroller.highButton;
                upBtn.RegisterCallback<ClickEvent>(_ => ScrollBy(-ScrollStepPx()));
                upBtn.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button != 0) return;
                    StartScrollRepeat(-1, upBtn, evt);
                });
                upBtn.RegisterCallback<PointerUpEvent>(evt => StopScrollRepeat(upBtn, evt));
                upBtn.RegisterCallback<PointerLeaveEvent>(evt => StopScrollRepeatLeave(upBtn, evt));
            }

            if (vScroller.lowButton != null)
            {
                var downBtn = vScroller.lowButton;
                downBtn.RegisterCallback<ClickEvent>(_ => ScrollBy(ScrollStepPx()));
                downBtn.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button != 0) return;
                    StartScrollRepeat(1, downBtn, evt);
                });
                downBtn.RegisterCallback<PointerUpEvent>(evt => StopScrollRepeat(downBtn, evt));
                downBtn.RegisterCallback<PointerLeaveEvent>(evt => StopScrollRepeatLeave(downBtn, evt));
            }
        }

        private bool IsDescendantOfConsoleScroller(VisualElement el)
        {
            if (el == null || _consoleScroll == null) return false;
            var vScroller = _consoleScroll.verticalScroller;
            if (vScroller == null) return false;
            for (var p = el; p != null; p = p.parent)
                if (p == vScroller) return true;
            return false;
        }

        private bool IsDescendantOfPotSlots(VisualElement el)
        {
            if (el == null || _root == null) return false;
            var container = _root.Q<VisualElement>("pcv3-hud-pot-slots");
            if (container == null) return false;
            for (var p = el; p != null; p = p.parent)
                if (p == container) return true;
            return false;
        }

        /// <summary>
        /// Abilita lo scroll con la rotella del mouse sulla console (e sull'area destra del terminale).
        /// </summary>
        private void RegisterConsoleMouseWheelScroll()
        {
            if (_consoleScroll == null) return;

            const float wheelScrollFactor = 280f;

            void OnWheel(WheelEvent evt)
            {
                if (!_isVisible || _consoleScroll == null) return;
                var vs = _consoleScroll.verticalScroller;
                if (vs == null) return;
                float delta = evt.delta.y * wheelScrollFactor;
                float newValue = Mathf.Clamp(vs.value + delta, vs.lowValue, vs.highValue);
                if (newValue != vs.value)
                {
                    vs.value = newValue;
                    evt.StopPropagation();
                }
            }

            _consoleScroll.RegisterCallback<WheelEvent>(OnWheel);
            if (_consoleView != null)
                _consoleView.RegisterCallback<WheelEvent>(OnWheel);
        }

        private void SetupScrollbar(ScrollView scrollView, VisualElement scrollbar, VisualElement track, VisualElement thumb)
        {
            if (scrollView == null || scrollbar == null || track == null || thumb == null)
                return;

            // Nascondi scrollbar se non c'è contenuto da scrollare
            UpdateScrollbarVisibility(scrollView, scrollbar);

            // Sincronizza scrollbar quando lo scroll cambia
            scrollView.RegisterCallback<GeometryChangedEvent>(_ => UpdateScrollbar(scrollView, track, thumb));
            scrollView.verticalScroller.valueChanged += _ => UpdateScrollbar(scrollView, track, thumb);

            // Permetti drag del thumb per controllare lo scroll
            bool isDragging = false;
            float dragStartY = 0f;
            float scrollStartValue = 0f;

            thumb.RegisterCallback<MouseDownEvent>(evt =>
            {
                isDragging = true;
                dragStartY = evt.localMousePosition.y;
                scrollStartValue = scrollView.verticalScroller.value;
                thumb.CaptureMouse();
                evt.StopPropagation();
            });

            thumb.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (isDragging && thumb.HasMouseCapture())
                {
                    float trackHeight = track.resolvedStyle.height;
                    float deltaY = evt.localMousePosition.y - dragStartY;
                    float scrollRange = scrollView.verticalScroller.highValue - scrollView.verticalScroller.lowValue;
                    float thumbHeight = thumb.resolvedStyle.height;
                    float maxThumbY = trackHeight - thumbHeight;

                    if (maxThumbY > 0 && scrollRange > 0)
                    {
                        float scrollDelta = (deltaY / maxThumbY) * scrollRange;
                        scrollView.verticalScroller.value = Mathf.Clamp(scrollStartValue + scrollDelta, scrollView.verticalScroller.lowValue, scrollView.verticalScroller.highValue);
                    }
                    evt.StopPropagation();
                }
            });

            thumb.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (isDragging)
                {
                    isDragging = false;
                    thumb.ReleaseMouse();
                    evt.StopPropagation();
                }
            });

            // Click sul track per scrollare
            track.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.target == track)
                {
                    float trackHeight = track.resolvedStyle.height;
                    float clickY = evt.localMousePosition.y;
                    float scrollRange = scrollView.verticalScroller.highValue - scrollView.verticalScroller.lowValue;
                    float thumbHeight = thumb.resolvedStyle.height;
                    float maxThumbY = trackHeight - thumbHeight;

                    if (maxThumbY > 0 && scrollRange > 0)
                    {
                        float normalizedY = clickY / trackHeight;
                        scrollView.verticalScroller.value = Mathf.Clamp(normalizedY * scrollRange, scrollView.verticalScroller.lowValue, scrollView.verticalScroller.highValue);
                    }
                    evt.StopPropagation();
                }
            });
        }

        private void UpdateScrollbar(ScrollView scrollView, VisualElement track, VisualElement thumb)
        {
            if (scrollView == null || track == null || thumb == null)
                return;

            float scrollValue = scrollView.verticalScroller.value;
            float scrollRange = scrollView.verticalScroller.highValue - scrollView.verticalScroller.lowValue;
            float contentHeight = scrollView.contentContainer.resolvedStyle.height;
            float viewportHeight = scrollView.contentViewport.resolvedStyle.height;

            if (contentHeight <= viewportHeight || scrollRange <= 0)
            {
                thumb.style.display = DisplayStyle.None;
                return;
            }

            thumb.style.display = DisplayStyle.Flex;

            float trackHeight = track.resolvedStyle.height;
            float thumbHeight = Mathf.Max(20f, (viewportHeight / contentHeight) * trackHeight);
            float maxThumbY = trackHeight - thumbHeight;
            float normalizedScroll = (scrollValue - scrollView.verticalScroller.lowValue) / scrollRange;
            float thumbY = normalizedScroll * maxThumbY;

            thumb.style.height = thumbHeight;
            thumb.style.top = thumbY;
        }

        private void UpdateScrollbarVisibility(ScrollView scrollView, VisualElement scrollbar)
        {
            if (scrollView == null || scrollbar == null)
                return;

            float contentHeight = scrollView.contentContainer.resolvedStyle.height;
            float viewportHeight = scrollView.contentViewport.resolvedStyle.height;

            if (contentHeight <= viewportHeight)
            {
                scrollbar.style.display = DisplayStyle.None;
            }
            else
            {
                scrollbar.style.display = DisplayStyle.Flex;
            }
        }

        private void Update()
        {
            if (!_isVisible) return;

            // Allow runtime tweaking of dim alpha in Inspector.
            if (_dimOverlay != null)
            {
                // Keep a consistent translucent black layer across all modal panels.
                float dimAlpha = UnifiedModalDimAlpha;
                _dimOverlay.style.backgroundColor = new Color(0f, 0f, 0f, dimAlpha);
            }

            if (_outerGlowLiveUpdate && _outerGlowGenerator != null && _outerGlowMaterialRuntime != null)
            {
                if (_outerGlowMaterial != null)
                    _outerGlowMaterialRuntime.CopyPropertiesFromMaterial(_outerGlowMaterial);
                ApplyOuterGlowDefaults(_outerGlowMaterialRuntime);
                _outerGlowGenerator.Render();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // ESC = close terminal. If queue exists, require confirmation (Y/N).
                RequestClose();
            }

            // Always keep command input ready without requiring clicks
            KeepInputFocused();

            RefreshHeader();
        }

        private void OnDestroy()
        {
            GameLanguageSettings.OnLanguageChanged -= OnPlantCardLanguageChanged;
            StopBootSequence();
            StopTypewriter();
            _outerGlowGenerator?.Dispose();
            _outerGlowGenerator = null;
            _outerGlowMaterialRuntime = null;
            UnsubscribeFromRuntimeServices();
        }

        public void Open()
        {
            PrepareBackdrop();
            SetVisible(true);
            SwitchToConsole();
            if (_playBootSequence)
                StartBootSequence();
            else
            {
                RenderWelcome(clearConsole: true);
                PrintStartCommands();
            }
            RefreshHeader();
            RefreshSidebar();
            FocusInput();
            // Some Unity/UI Toolkit setups require a next-tick focus to actually stick.
            RequestRefocusSoon();
        }

        public void Close()
        {
            StopProtocolTypewriter();
            StopBootSequence();
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            _isVisible = visible;
            GameplayUiModalLock.SetMachineModalState(visible);

            if (_root == null) return;

            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _root.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;

            if (visible)
                RefreshZona2AndZona3();

            if (!visible)
            {
                StopLoadingBlink();
                ShowLoadingSpinner(false);
                if (_loadingCoroutine != null) { StopCoroutine(_loadingCoroutine); _loadingCoroutine = null; }
                StopTypewriter();
                StopEnvironmentalPhRefresh();
            }

            // Visual requirement: when terminal is open, everything behind must be black / hidden.
            if (hideOtherUiWhileTerminalOpen)
            {
                if (visible) SuppressOtherUi();
                else RestoreOtherUi();
            }

            // Debug consoles (IMGUI) ascoltano Input.GetKeyDown anche mentre scrivi nel terminale.
            // Quindi, quando il terminale è aperto, le sospendiamo temporaneamente.
            if (suppressDebugConsolesWhileTerminalOpen)
            {
                if (visible) SuppressDebugConsoles();
                else RestoreDebugConsoles();
            }

            // Disable point&click movement while terminal is open (exclusive interaction)
            if (visible) SuspendPlayerMovement();
            else RestorePlayerMovement();

            // Disable Interactable hotkeys (es. E) that could re-open/reset the terminal while typing.
            if (visible) SuppressInteractables();
            else RestoreInteractables();

            if (visible)
                FocusInput();

            if (visible) StartBlinkCursor();
            else StopBlinkCursor();
        }

        private void PrepareBackdrop()
        {
            if (_backdrop == null || _dimOverlay == null) return;

            if (_outsideShowsGameView)
            {
                _dimOverlay.style.backgroundColor = new Color(0f, 0f, 0f, UnifiedModalDimAlpha);
                _backdrop.style.backgroundImage = new StyleBackground();
                _backdrop.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
                return;
            }

            if (!_useBlurredBackdrop) return;

            _dimOverlay.style.backgroundColor = new Color(0f, 0f, 0f, UnifiedModalDimAlpha);
            if (Screen.width < 64 || Screen.height < 64)
            {
                // Render target too small (minimized or not ready) -> skip capture.
                return;
            }

            if (!_backdropCapturePending)
                StartCoroutine(CaptureBackdropNextFrame());
        }

        private IEnumerator CaptureBackdropNextFrame()
        {
            _backdropCapturePending = true;
            yield return new WaitForEndOfFrame();

            Texture2D src = null;
            try
            {
                src = ScreenCapture.CaptureScreenshotAsTexture();
            }
            catch
            {
                src = null;
            }

            if (src != null)
            {
                try
                {
                    var down = DownsampleTexture(src, Mathf.Max(2, _backdropDownsample));
                    var blurred = BoxBlur(down, Mathf.Max(1, _backdropBlurRadius), Mathf.Max(1, _backdropBlurIterations));
                    if (_backdropTexture != null)
                        Destroy(_backdropTexture);
                    _backdropTexture = blurred;
                    _backdrop.style.backgroundImage = new StyleBackground(_backdropTexture);
                    _backdrop.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                }
                finally
                {
                    Destroy(src);
                }
            }

            _backdropCapturePending = false;
        }

        private Texture2D DownsampleTexture(Texture2D src, int factor)
        {
            int w = Mathf.Max(1, src.width / factor);
            int h = Mathf.Max(1, src.height / factor);
            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = h > 1 ? (float)y / (h - 1) : 0f;
                for (int x = 0; x < w; x++)
                {
                    float u = w > 1 ? (float)x / (w - 1) : 0f;
                    pixels[y * w + x] = src.GetPixelBilinear(u, v);
                }
            }
            dst.SetPixels(pixels);
            dst.Apply();
            return dst;
        }

        private Texture2D BoxBlur(Texture2D src, int radius, int iterations)
        {
            int w = src.width;
            int h = src.height;
            var srcPixels = src.GetPixels();
            var dstPixels = new Color[srcPixels.Length];

            int r = Mathf.Max(1, radius);
            for (int it = 0; it < iterations; it++)
            {
                for (int y = 0; y < h; y++)
                {
                    int yMin = Mathf.Max(0, y - r);
                    int yMax = Mathf.Min(h - 1, y + r);
                    for (int x = 0; x < w; x++)
                    {
                        int xMin = Mathf.Max(0, x - r);
                        int xMax = Mathf.Min(w - 1, x + r);
                        Color sum = Color.clear;
                        int count = 0;
                        for (int yy = yMin; yy <= yMax; yy++)
                        {
                            int row = yy * w;
                            for (int xx = xMin; xx <= xMax; xx++)
                            {
                                sum += srcPixels[row + xx];
                                count++;
                            }
                        }
                        dstPixels[y * w + x] = sum / Mathf.Max(1, count);
                    }
                }
                var tmp = srcPixels;
                srcPixels = dstPixels;
                dstPixels = tmp;
            }

            var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            outTex.SetPixels(srcPixels);
            outTex.Apply();
            Destroy(src);
            return outTex;
        }

        private void SuppressOtherUi()
        {
            if (_suppressedUiDocuments.Count > 0) return;

            // Nomi GO UI da nascondere quando il terminale è aperto.
            // PlayerStatusPanel resta visibile per allineamento con gli altri pannelli modali.
            string[] goNames =
            {
                "HUD_TopBar",
                "HUD_BottomNavigation",
                "Notifications Foundation",
                "HUD_GameViewportBackground"
            };

            foreach (var name in goNames)
            {
                var go = GameObject.Find(name);
                if (go == null) continue;

                // Hide only the UIDocument visuals and block picking, keeping scripts enabled.
                // This prevents "re-open on OnEnable" glitches when restoring.
                foreach (var doc in go.GetComponents<UIDocument>())
                {
                    if (doc == null) continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;

                    _suppressedUiDocuments.Add((doc, root.resolvedStyle.display, root.pickingMode));
                    root.style.display = DisplayStyle.None;
                    root.pickingMode = PickingMode.Ignore;
                }
            }
        }

        private void RestoreOtherUi()
        {
            foreach (var (doc, display, picking) in _suppressedUiDocuments)
            {
                if (doc == null) continue;
                var root = doc.rootVisualElement;
                if (root == null) continue;
                root.style.display = display;
                root.pickingMode = picking;
            }
            _suppressedUiDocuments.Clear();
        }

        private void SuppressInteractables()
        {
            if (_suppressedInteractables.Count > 0) return;

            // Limitiamoci ai terminal opener per non toccare altre interazioni di gioco.
            var openers = FindObjectsOfType<PlantCardV3TerminalOpener>();
            foreach (var op in openers)
            {
                if (op == null) continue;
                var inter = op.GetComponent<Interactable>();
                if (inter == null) continue;
                _suppressedInteractables.Add((inter, inter.enabled));
                inter.enabled = false;
            }
        }

        private void RestoreInteractables()
        {
            foreach (var (comp, wasEnabled) in _suppressedInteractables)
            {
                if (comp == null) continue;
                comp.enabled = wasEnabled;
            }
            _suppressedInteractables.Clear();
        }

        private void KeepInputFocused()
        {
            if (_input == null) return;

            // If focus is lost, restore it next frame (prevents accidental clicks stealing focus)
            if (!IsFocusWithinInput())
            {
                RequestRefocusSoon();
            }
        }

        private void SuspendPlayerMovement()
        {
            // IMPORTANT: In questa scena il movimento WASD è gestito da PlayerPerspectiveMover2D.
            // Inoltre PlayerMoverRouter2D può ripristinare/sovrascrivere il sospendi del clickMover.
            // Quindi: disabilitiamo temporaneamente Router + PerspectiveMover, e sospendiamo clickMover.

            if (_playerMoverRouter != null)
            {
                _wasRouterEnabled = _playerMoverRouter.enabled;
                _playerMoverRouter.enabled = false;
            }

            if (_playerPerspectiveMover != null)
            {
                _wasPerspectiveMoverEnabled = _playerPerspectiveMover.enabled;
                _playerPerspectiveMover.enabled = false;
                // stop residual physics drift
                var rb = _playerPerspectiveMover.GetComponent<Rigidbody2D>();
                if (rb != null) rb.velocity = Vector2.zero;
            }

            if (_playerMover != null)
            {
                _wasPlayerMoverSuspended = true;
                _playerMover.SuspendMovement(true);
                _playerMover.StopMovement();
            }
        }

        private void RestorePlayerMovement()
        {
            if (_playerMover != null && _wasPlayerMoverSuspended)
            {
                _playerMover.SuspendMovement(false);
                _wasPlayerMoverSuspended = false;
            }

            if (_playerPerspectiveMover != null)
                _playerPerspectiveMover.enabled = _wasPerspectiveMoverEnabled;

            if (_playerMoverRouter != null)
                _playerMoverRouter.enabled = _wasRouterEnabled;
        }

        private void SuppressDebugConsoles()
        {
            if (_suppressedDebugBehaviours.Count > 0) return;

            // IMPORTANT: Non referenziamo i tipi direttamente (alcuni sono #if UNITY_EDITOR/DEVELOPMENT_BUILD).
            // Usiamo FullName per matching runtime-safe.
            var targets = new HashSet<string>
            {
                "Sporae.DevTools.PotDebugConsole",
                "Sporae.DevTools.ToastNotificationDebugConsole",
                "Sporae.UI.UIToolkit.NotificationsFoundation.FoundationNotificationsDebugConsole",
                "Sporae.DevTools.PhSystemDebugConsole",
                "Sporae.DevTools.GlobalStateInspector",
                "Sporae.DevTools.DifficultyCalibrationConsole"
            };

            var all = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            foreach (var mb in all)
            {
                if (mb == null) continue;
                if (!mb.gameObject.scene.IsValid()) continue; // skip prefabs/assets

                var t = mb.GetType();
                var full = t.FullName;
                if (string.IsNullOrEmpty(full) || !targets.Contains(full)) continue;

                // Non toccare se già disabilitato
                bool wasEnabled = mb.enabled;
                if (!wasEnabled) continue;

                _suppressedDebugBehaviours.Add((mb, wasEnabled));
                mb.enabled = false;
            }
        }

        private void RestoreDebugConsoles()
        {
            foreach (var (comp, wasEnabled) in _suppressedDebugBehaviours)
            {
                if (comp == null) continue;
                comp.enabled = wasEnabled;
            }
            _suppressedDebugBehaviours.Clear();
        }

        private void TryMoveAfterHud()
        {
            // Nomi come da SceneHierarchy.txt
            var topBar = GameObject.Find("HUD_TopBar");
            var bottomNav = GameObject.Find("HUD_BottomNavigation");
            if (topBar == null && bottomNav == null)
                return;

            int maxIndex = -1;
            if (topBar != null) maxIndex = Mathf.Max(maxIndex, topBar.transform.GetSiblingIndex());
            if (bottomNav != null) maxIndex = Mathf.Max(maxIndex, bottomNav.transform.GetSiblingIndex());

            if (maxIndex >= 0)
                transform.SetSiblingIndex(maxIndex + 1);
        }

        private void RefreshHeader()
        {
            if (_apLabel != null)
            {
                const int maxActionsDisplay = 5;
                int left = _gameManager != null ? Math.Min(_gameManager.ActionsLeft, maxActionsDisplay) : 0;
                _apLabel.text = $"AZIONI: {left}/{maxActionsDisplay}";
            }

            if (_queuedLabel != null)
            {
                int effectiveCount = GetEffectiveQueueActionCount();
                _queuedLabel.text = $"IN CODA: {effectiveCount}";
            }
        }

        /// <summary>Numero di azioni "che costano" in coda: non conta WATERING/LED inclusi nel flow PLANT (ApCost 0).</summary>
        private int GetEffectiveQueueActionCount()
        {
            int n = 0;
            for (int i = 0; i < _queue.Count; i++)
            {
                var a = _queue[i];
                if (a != null && a.ApCost > 0) n++;
            }
            return n;
        }

        private void RefreshSidebar()
        {
            RefreshPotCards();
            RefreshZona2AndZona3();
        }

        private void SetupZona2AndZona3()
        {
            if (_pcv3Left != null) _pcv3Left.style.display = DisplayStyle.Flex;
            if (_pcv3Center != null) _pcv3Center.style.display = DisplayStyle.Flex;

            for (int i = 0; i < _hudPotSlots.Count; i++)
            {
                int index = i;
                var slot = _hudPotSlots[i];
                slot.pickingMode = PickingMode.Position;
                slot.RegisterCallback<ClickEvent>(_ => OnHudPotSlotClicked(index));
                slot.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 0)
                    {
                        OnHudPotSlotClicked(index);
                        evt.StopPropagation();
                        evt.PreventDefault();
                    }
                }, TrickleDown.TrickleDown);
                foreach (var child in slot.Children())
                    child.pickingMode = PickingMode.Ignore;
            }

            BindQuickActionButtons();

            _hudPots.Clear();
            var pots = FindPots();
            for (int i = 0; i < Mathf.Min(4, pots.Count); i++)
                _hudPots.Add(pots[i]);

            _selectedPotIndex = 0;
            UpdateHudSlotVisuals();
            RefreshHudFromSelectedPot();

            // Se il layout è stato aggiornato in UI Builder (frame/posizioni), usa sempre UXML e ignora posizioni salvate
            bool useSavedPositions = EnsureTerminalLayoutVersion();

            SetupDraggableGroup(_root.Q<VisualElement>("pcv3-hud-preview-group"), PrefsKeyZona2Left, PrefsKeyZona2Top, useSavedPositions);
            SetupDraggableGroup(_root.Q<VisualElement>("pcv3-vital-stats-block-1"), PrefsKeyZona3Block1Left, PrefsKeyZona3Block1Top, useSavedPositions);
            SetupDraggableGroup(_root.Q<VisualElement>("pcv3-vital-stats-block-2"), PrefsKeyZona3Block2Left, PrefsKeyZona3Block2Top, useSavedPositions);

            SetupDraggableGroup(_pcv3Left, PrefsKeyBodyLeftLeft, PrefsKeyBodyLeftTop, useSavedPositions, 0f, 0f);
            SetupDraggableGroup(_pcv3Center, PrefsKeyBodyCenterLeft, PrefsKeyBodyCenterTop, useSavedPositions, 230f, 0f);
            SetupDraggableGroup(_pcv3Right, PrefsKeyBodyRightLeft, PrefsKeyBodyRightTop, useSavedPositions, 560f, 0f);
        }

        /// <summary>Se la versione salvata è minore di TerminalLayoutVersion, cancella le posizioni salvate così in Play si usa il layout UXML/UI Builder. Ritorna true se si possono applicare le posizioni salvate.</summary>
        private bool EnsureTerminalLayoutVersion()
        {
            int saved = PlayerPrefs.GetInt(PrefsKeyTerminalLayoutVersion, 0);
            if (saved >= TerminalLayoutVersion)
                return true;
            PlayerPrefs.SetInt(PrefsKeyTerminalLayoutVersion, TerminalLayoutVersion);
            PlayerPrefs.DeleteKey(PrefsKeyZona2Left);
            PlayerPrefs.DeleteKey(PrefsKeyZona2Top);
            PlayerPrefs.DeleteKey(PrefsKeyZona3Block1Left);
            PlayerPrefs.DeleteKey(PrefsKeyZona3Block1Top);
            PlayerPrefs.DeleteKey(PrefsKeyZona3Block2Left);
            PlayerPrefs.DeleteKey(PrefsKeyZona3Block2Top);
            PlayerPrefs.DeleteKey(PrefsKeyBodyLeftLeft);
            PlayerPrefs.DeleteKey(PrefsKeyBodyLeftTop);
            PlayerPrefs.DeleteKey(PrefsKeyBodyCenterLeft);
            PlayerPrefs.DeleteKey(PrefsKeyBodyCenterTop);
            PlayerPrefs.DeleteKey(PrefsKeyBodyRightLeft);
            PlayerPrefs.DeleteKey(PrefsKeyBodyRightTop);
            PlayerPrefs.Save();
            return false;
        }

        private void SetupDraggableGroup(VisualElement group, string prefsKeyLeft, string prefsKeyTop, bool applySavedPositions, float defaultLeft = 0f, float defaultTop = 0f)
        {
            if (group == null) return;
            if (applySavedPositions)
            {
                float savedLeft = PlayerPrefs.GetFloat(prefsKeyLeft, float.MaxValue);
                float savedTop = PlayerPrefs.GetFloat(prefsKeyTop, float.MaxValue);
                if (savedLeft != float.MaxValue && savedTop != float.MaxValue)
                {
                    group.style.position = Position.Absolute;
                    group.style.left = savedLeft;
                    group.style.top = savedTop;
                    group.style.right = StyleKeyword.Auto;
                    group.style.bottom = StyleKeyword.Auto;
                }
            }

            group.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                if (IsDescendantOfPotSlots(evt.target as VisualElement) && (group.name == "pcv3-center" || group.name == "pcv3-hud-preview-group"))
                    return;
                _draggingElement = group;
                _dragStartMouse = evt.mousePosition;
                float startLeft = group.resolvedStyle.left;
                float startTop = group.resolvedStyle.top;
                if (float.IsNaN(startLeft) || float.IsNaN(startTop))
                {
                    startLeft = group.layout.x;
                    startTop = group.layout.y;
                    group.style.position = Position.Absolute;
                    group.style.left = startLeft;
                    group.style.top = startTop;
                    group.style.right = StyleKeyword.Auto;
                    group.style.bottom = StyleKeyword.Auto;
                }
                _dragStartLeft = startLeft;
                _dragStartTop = startTop;
                group.CaptureMouse();
                if (_dragMoveCallback == null) _dragMoveCallback = OnDragMove;
                if (_dragUpCallback == null) _dragUpCallback = OnDragUp;
                group.panel.visualTree.RegisterCallback(_dragMoveCallback);
                group.panel.visualTree.RegisterCallback(_dragUpCallback);
            });
        }

        private void OnDragMove(MouseMoveEvent evt)
        {
            if (_draggingElement == null) return;
            float deltaX = evt.mousePosition.x - _dragStartMouse.x;
            float deltaY = evt.mousePosition.y - _dragStartMouse.y;
            _draggingElement.style.left = _dragStartLeft + deltaX;
            _draggingElement.style.top = _dragStartTop + deltaY;
            _draggingElement.style.right = StyleKeyword.Auto;
            _draggingElement.style.bottom = StyleKeyword.Auto;
        }

        private void OnDragUp(MouseUpEvent evt)
        {
            if (_draggingElement == null || evt.button != 0) return;
            _draggingElement.ReleaseMouse();
            if (_draggingElement.panel != null && _draggingElement.panel.visualTree != null)
            {
                if (_dragMoveCallback != null) _draggingElement.panel.visualTree.UnregisterCallback(_dragMoveCallback);
                if (_dragUpCallback != null) _draggingElement.panel.visualTree.UnregisterCallback(_dragUpCallback);
            }
            string keyLeft = null, keyTop = null;
            if (_draggingElement.name == "pcv3-hud-preview-group") { keyLeft = PrefsKeyZona2Left; keyTop = PrefsKeyZona2Top; }
            else if (_draggingElement.name == "pcv3-vital-stats-block-1") { keyLeft = PrefsKeyZona3Block1Left; keyTop = PrefsKeyZona3Block1Top; }
            else if (_draggingElement.name == "pcv3-vital-stats-block-2") { keyLeft = PrefsKeyZona3Block2Left; keyTop = PrefsKeyZona3Block2Top; }
            else if (_draggingElement.name == "pcv3-left") { keyLeft = PrefsKeyBodyLeftLeft; keyTop = PrefsKeyBodyLeftTop; }
            else if (_draggingElement.name == "pcv3-center") { keyLeft = PrefsKeyBodyCenterLeft; keyTop = PrefsKeyBodyCenterTop; }
            else if (_draggingElement.name == "pcv3-right") { keyLeft = PrefsKeyBodyRightLeft; keyTop = PrefsKeyBodyRightTop; }
            if (keyLeft != null)
            {
                float l = _draggingElement.resolvedStyle.left;
                float t = _draggingElement.resolvedStyle.top;
                if (!float.IsNaN(l)) PlayerPrefs.SetFloat(keyLeft, l);
                if (!float.IsNaN(t)) PlayerPrefs.SetFloat(keyTop, t);
                PlayerPrefs.Save();
            }
            _draggingElement = null;
        }

        private bool IsPotSlotEmpty(int index)
        {
            if (index < 0 || index >= _hudPots.Count) return true;
            var pot = _hudPots[index];
            var state = pot?.PotActions?.PotState;
            return state == null || state.IsEmpty || !state.HasPlant;
        }

        private void UpdateHudSlotVisuals()
        {
            const string classStandard = "pcv3-hud-pot-slot-standard";
            const string classPure = "pcv3-hud-pot-slot-pure";
            const string classEvil = "pcv3-hud-pot-slot-evil";

            for (int i = 0; i < _hudPotSlots.Count; i++)
            {
                var slot = _hudPotSlots[i];
                var label = slot.Q<Label>(null, "pcv3-hud-pot-slot-label");
                bool empty = IsPotSlotEmpty(i);
                string potId = (i < _hudPots.Count) ? _hudPots[i].PotId : $"POT-{i + 1:D3}";
                if (label != null)
                    label.text = " " + potId;

                if (empty)
                {
                    slot.AddToClassList("pcv3-hud-pot-slot-empty");
                    slot.RemoveFromClassList("pcv3-hud-pot-slot-selected");
                    slot.RemoveFromClassList(classStandard);
                    slot.RemoveFromClassList(classPure);
                    slot.RemoveFromClassList(classEvil);
                }
                else
                {
                    slot.RemoveFromClassList("pcv3-hud-pot-slot-empty");
                    slot.RemoveFromClassList(classStandard);
                    slot.RemoveFromClassList(classPure);
                    slot.RemoveFromClassList(classEvil);
                    var state = _hudPots[i].PotActions?.PotState;
                    var plantData = state?.GetPlantData();
                    if (plantData != null)
                    {
                        switch (plantData.Family)
                        {
                            case PlantFamily.Standard: slot.AddToClassList(classStandard); break;
                            case PlantFamily.Pure: slot.AddToClassList(classPure); break;
                            case PlantFamily.Evil: slot.AddToClassList(classEvil); break;
                            default: slot.AddToClassList(classStandard); break;
                        }
                    }
                    if (i == _selectedPotIndex)
                        slot.AddToClassList("pcv3-hud-pot-slot-selected");
                    else
                        slot.RemoveFromClassList("pcv3-hud-pot-slot-selected");
                }
            }
        }

        private void OnHudPotSlotClicked(int index)
        {
            if (index < 0 || index >= _hudPotSlots.Count) return;
            if (IsPotSlotEmpty(index))
            {
                string msg = index < _hudPots.Count
                    ? $"§WARN§Non c'è alcuna pianta presente in quel pot {_hudPots[index].PotId}.§END§"
                    : "§WARN§In quello slot non è assegnato alcun vaso.§END§";
                AppendRawLine(msg);
                AppendRawLine("");
                FlushConsole();
                return;
            }
            _selectedPotIndex = index;
            for (int i = 0; i < _hudPotSlots.Count; i++)
            {
                if (i == index)
                    _hudPotSlots[i].AddToClassList("pcv3-hud-pot-slot-selected");
                else
                    _hudPotSlots[i].RemoveFromClassList("pcv3-hud-pot-slot-selected");
            }
            RefreshHudFromSelectedPot();
        }

        private void RefreshZona2AndZona3()
        {
            _hudPots.Clear();
            var pots = FindPots();
            for (int i = 0; i < Mathf.Min(4, pots.Count); i++)
                _hudPots.Add(pots[i]);
            if (_selectedPotIndex >= _hudPots.Count) _selectedPotIndex = Mathf.Max(0, _hudPots.Count - 1);
            if (IsPotSlotEmpty(_selectedPotIndex))
            {
                for (int i = 0; i < _hudPotSlots.Count; i++)
                {
                    if (!IsPotSlotEmpty(i)) { _selectedPotIndex = i; break; }
                }
            }
            UpdateHudSlotVisuals();
            RefreshHudFromSelectedPot();
        }

        private void BindQuickActionButtons()
        {
            if (_quickActionsBound) return;
            _quickActionsBound = true;

            // Ensure the A9 panel itself is always interactive
            if (_a9QuickPanel != null)
                _a9QuickPanel.pickingMode = PickingMode.Position;

            void BindBtn(Button btn, string command)
            {
                if (btn == null) return;
                // Explicit picking mode: pcv3-right console-view extends -97px left and can swallow clicks
                btn.pickingMode = PickingMode.Position;
                btn.clicked += () => ExecuteQuickAction(command);
                btn.RegisterCallback<MouseDownEvent>(e => e.StopPropagation());
                btn.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            }

            BindBtn(_quickWateringButton, "WATERING");
            BindBtn(_quickLedBlueButton, "LED BLUE");
            BindBtn(_quickLedRedButton, "LED RED");
            BindBtn(_quickFertilizeButton, "FERTILIZE");
            BindBtn(_quickSprayButton, "SPRAY");
            BindBtn(_quickPruneButton, "PRUNE");
        }

        private void ExecuteQuickAction(string baseCommand)
        {
            var pot = GetSelectedHudPot();
            if (pot == null || string.IsNullOrEmpty(pot.PotId))
            {
                AppendRawLine("§WARN§Seleziona prima un POT valido per usare i comandi rapidi.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            HandleCommand($"{baseCommand} {pot.PotId}");
        }

        private PotSlot GetSelectedHudPot()
        {
            if (_selectedPotIndex < 0 || _selectedPotIndex >= _hudPots.Count)
                return null;
            return _hudPots[_selectedPotIndex];
        }

        private void UpdateQuickActionPanel(PotSlot pot, PotStateModel state, PlantData plantData, bool empty)
        {
            if (_a9QuickPanel != null)
                _a9QuickPanel.style.display = DisplayStyle.Flex;

            bool hasPot = pot != null && !string.IsNullOrEmpty(pot.PotId);
            string selectedPotId = hasPot ? pot.PotId : "---";
            string disabledReason = empty
                ? LocalizationManager.GetString("plant_terminal.reason_empty_pot")
                : LocalizationManager.GetString("plant_terminal.reason_action_na");

            if (_a9SuggestionsLabel != null)
            {
                _a9SuggestionsLabel.enableRichText = true;
                _a9SuggestionsLabel.text = BuildA9SuggestionText(state, plantData, empty);
                _a9SuggestionsLabel.tooltip = LocalizationManager.GetString("plant_terminal.suggestions_tooltip", new Dictionary<string, string> { ["pot"] = selectedPotId });
            }

            var actions = pot?.PotActions;

            // Terminal context: range check always fails (player is remote via terminal).
            // Mirror the same gate used by BeginConfirmToggleAction/BeginSelectItemForAction:
            // enabled iff the pot has a living plant (dead or empty → disabled).
            bool hasPotPlant = !empty && state != null && state.HasPlant &&
                               (PlantCondition)state.ConditionLabel != PlantCondition.Morta;

            bool isWateringOn = actions != null && actions.IsWateringSystemOn();
            LedSystemState ledState = actions != null ? actions.GetLedSystemState() : LedSystemState.Off;
            bool isLedBlueOn = ledState == LedSystemState.Blue;
            bool isLedRedOn = ledState == LedSystemState.Red;

            SetQuickToggleButtonState(_quickWateringButton, isWateringOn, hasPotPlant, "WATERING ON", "WATERING OFF", selectedPotId, disabledReason);
            SetQuickToggleButtonState(_quickLedBlueButton, isLedBlueOn, hasPotPlant, "LED BLUE ON", "LED BLUE OFF", selectedPotId, disabledReason);
            SetQuickToggleButtonState(_quickLedRedButton, isLedRedOn, hasPotPlant, "LED RED ON", "LED RED OFF", selectedPotId, disabledReason);
            SetQuickActionButtonState(_quickFertilizeButton, hasPotPlant, "FERTILIZE", selectedPotId, disabledReason);
            SetQuickActionButtonState(_quickSprayButton, hasPotPlant, "SPRAY", selectedPotId, disabledReason);
            SetQuickActionButtonState(_quickPruneButton, hasPotPlant, "PRUNE", selectedPotId, disabledReason);
        }

        private void SetQuickToggleButtonState(Button button, bool isOn, bool enabled, string onLabel, string offLabel, string potId, string disabledReason)
        {
            if (button == null) return;
            button.text = isOn ? onLabel : offLabel;
            button.SetEnabled(enabled);
            button.tooltip = enabled ? $"{button.text} [{potId}]" : disabledReason;
            button.RemoveFromClassList("pcv3-quick-toggle-on");
            button.RemoveFromClassList("pcv3-quick-toggle-off");
            button.RemoveFromClassList("pcv3-quick-disabled");
            button.AddToClassList(isOn ? "pcv3-quick-toggle-on" : "pcv3-quick-toggle-off");
            if (!enabled)
                button.AddToClassList("pcv3-quick-disabled");
        }

        private void SetQuickActionButtonState(Button button, bool enabled, string label, string potId, string disabledReason)
        {
            if (button == null) return;
            button.text = label;
            button.SetEnabled(enabled);
            button.tooltip = enabled ? $"{label} [{potId}]" : disabledReason;
            button.RemoveFromClassList("pcv3-quick-toggle-on");
            button.RemoveFromClassList("pcv3-quick-toggle-off");
            button.RemoveFromClassList("pcv3-quick-disabled");
            if (!enabled)
                button.AddToClassList("pcv3-quick-disabled");
        }

        // ─── colori richtext A9 (palette ufficiale dal USS) ───
        private const string A9ColGreen  = "#7FFF7A";
        private const string A9ColYellow = "#E6C96F";
        private const string A9ColRed    = "#D35F5F";
        private const string A9ColBlue   = "#5DB6E3";
        private const string A9ColDim    = "#C0C8C5";

        private string BuildA9SuggestionText(PotStateModel state, PlantData plantData, bool empty)
        {
            if (empty || state == null)
                return $"<color={A9ColDim}>Nessuna pianta nel vaso. Pianta un seme per attivare il monitor.</color>";
            if (plantData == null)
                return $"<color={A9ColDim}>Dati pianta non disponibili.</color>";

            var sb = new System.Text.StringBuilder();

            // Riga 1: STATO ORA — condizione derivata da score live (ConditionLabel è stale, aggiornato solo a fine giornata)
            var condEnum = (PlantCondition)state.ConditionLabel == PlantCondition.Morta
                ? PlantCondition.Morta
                : MapScoreToConditionForUi(state.ConditionScore);
            string officialName = PlantConditionSystem.GetConditionName(condEnum).ToUpperInvariant();
            string condColor = condEnum switch
            {
                PlantCondition.Rigogliosa => A9ColGreen,
                PlantCondition.Sana       => A9ColGreen,
                PlantCondition.Appassita  => A9ColYellow,
                PlantCondition.Critica    => A9ColRed,
                PlantCondition.Morta      => A9ColRed,
                _                         => A9ColGreen,
            };
            string condLabel = $"{officialName} ({state.ConditionScore}%)";
            var trend = (ForecastDirection)state.ForecastDirection;
            string trendArrow = trend == ForecastDirection.Up ? "▲" : (trend == ForecastDirection.Down ? "▼" : "→");
            string trendColor = trend == ForecastDirection.Up ? A9ColGreen : (trend == ForecastDirection.Down ? A9ColRed : A9ColYellow);
            sb.AppendLine($"<color={A9ColDim}>STATO:</color> <color={condColor}>{condLabel}</color>  <color={trendColor}>{trendArrow}</color>");

            // Riga 1b: effetto gameplay della condizione (B.1 — dizionario condizioni)
            string effectHint = condEnum switch
            {
                PlantCondition.Rigogliosa => "Crescita +20% · Produzione +15%",
                PlantCondition.Appassita  => "Crescita -30% · Avanzamento bloccato",
                PlantCondition.Critica    => "Avanzamento bloccato · rischio morte",
                PlantCondition.Morta      => "Nessuna crescita — esegui UPROOT",
                _                         => string.Empty,
            };
            if (!string.IsNullOrEmpty(effectHint))
                sb.AppendLine($"<color={A9ColDim}>  ↳ {effectHint}</color>");

            // Riga 2: FATTORI — cause principali (B.2, nomi italiani ufficiali)
            var drivers = BuildQuickDrivers(state, plantData);
            if (drivers.Count > 0)
            {
                string sep = $"<color={A9ColDim}> | </color>";
                var coloredDrivers = drivers.Select(d =>
                    d.isCritical ? $"<color={A9ColRed}>{d.label}</color>" : $"<color={A9ColYellow}>{d.label}</color>");
                sb.AppendLine($"<color={A9ColDim}>FATTORI:</color> {string.Join(sep, coloredDrivers)}");
            }

            // Riga 3: SUGGERIMENTO — prima frase operativa (max ~90 char)
            var tips = BuildConsiglioForPot(state, plantData);
            if (tips != null && tips.Count > 0)
            {
                string tip = StripTerminalTokens(tips[0]);
                if (!string.IsNullOrWhiteSpace(tip))
                {
                    if (tip.Length > 90) tip = tip.Substring(0, 87) + "...";
                    sb.AppendLine($"<color={A9ColBlue}>▶</color> <color={A9ColDim}>{tip}</color>");
                }
            }

            // Riga pH sintetico (C.1): direzione + distanza qualitativa dal target pianta
            if (_phSystem != null && plantData != null)
            {
                float currentPh = _phSystem.CurrentPh;
                float phMin = plantData.OptimalPhMin;
                float phMax = plantData.OptimalPhMax;
                string phDir, phDist, phColor;
                if (currentPh < phMin)
                {
                    float dist = phMin - currentPh;
                    phDir = "ALZA";
                    phDist = dist < 10f ? "VICINO" : dist < 30f ? "MEDIO" : "LONTANO";
                    phColor = dist < 10f ? A9ColYellow : A9ColRed;
                }
                else if (currentPh > phMax)
                {
                    float dist = currentPh - phMax;
                    phDir = "ABBASSA";
                    phDist = dist < 10f ? "VICINO" : dist < 30f ? "MEDIO" : "LONTANO";
                    phColor = dist < 10f ? A9ColYellow : A9ColRed;
                }
                else
                {
                    phDir = "OK";
                    phDist = string.Empty;
                    phColor = A9ColGreen;
                }
                string phLine = string.IsNullOrEmpty(phDist)
                    ? $"<color={A9ColDim}>pH:</color> <color={phColor}>{phDir}</color>"
                    : $"<color={A9ColDim}>pH:</color> <color={phColor}>{phDir} · {phDist}</color>";
                sb.AppendLine(phLine);
            }

            // Riga 4: CTA
            sb.Append($"<color={A9ColDim}>→ Digita</color> <color={A9ColGreen}>STATUS</color> <color={A9ColDim}>per analisi completa</color>");

            return sb.ToString().Trim();
        }

        private List<(string label, bool isCritical)> BuildQuickDrivers(PotStateModel state, PlantData plantData)
        {
            var drivers = new List<(string, bool)>();
            if (_potSystemConfig == null || state == null || plantData == null) return drivers;

            PlantStage currentStage = (PlantStage)state.Stage;
            StageRequirements stageReq = plantData.GetStageRequirements(currentStage);
            if (stageReq == null) return drivers;

            int maxHydration = _potSystemConfig.MaxHydration;
            int hydrationPercent = PlantCardCalculators.CalculateHydrationPercent(state.Hydration, maxHydration);
            if (hydrationPercent < stageReq.hydrationMin)
                drivers.Add((LocalizationManager.GetString("plant_terminal.driver_hydration_low"), false));
            else if (hydrationPercent > stageReq.hydrationMax)
                drivers.Add((LocalizationManager.GetString("plant_terminal.driver_overwater"), false));

            int consecutiveDays = state.GetConsecutiveLedDays();
            int maxDays = Mathf.Max(1, _potSystemConfig.MaxDaysForFullStress);
            int lightStress = Mathf.RoundToInt(Mathf.Clamp01((float)consecutiveDays / maxDays) * 100f);
            if (lightStress > 80)
                drivers.Add((LocalizationManager.GetString("plant_terminal.driver_light_crit"), true));
            else if (lightStress > 50)
                drivers.Add((LocalizationManager.GetString("plant_terminal.driver_light_high"), false));

            bool isFertilizerLow = state.FertilizerLevel < stageReq.fertilizerMin;
            if (!FertilizerCarePolicy.ShouldTreatFertilizerAsOptional(currentStage, stageReq) && isFertilizerLow)
                drivers.Add((LocalizationManager.GetString("plant_terminal.driver_fertilizer_low"), false));
            else if (!FertilizerCarePolicy.ShouldTreatFertilizerAsOptional(currentStage, stageReq) && state.FertilizerLevel > stageReq.fertilizerMax)
                drivers.Add((LocalizationManager.GetString("plant_terminal.driver_fertilizer_high"), false));

            if (state.IsInfested)
                drivers.Add((LocalizationManager.GetString("plant_terminal.driver_infested"), true));
            else if (state.MoldRiskLevel >= 3)
                drivers.Add((LocalizationManager.GetString("plant_terminal.driver_mold_crit"), true));
            else if (state.MoldRiskLevel >= 2)
                drivers.Add((LocalizationManager.GetString("plant_terminal.driver_mold"), false));

            if (drivers.Count > 3) drivers = drivers.GetRange(0, 3);
            return drivers;
        }

        private static string StripTerminalTokens(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            string text = raw;
            string[] tokens =
            {
                "§TITLE§", "§CMD§", "§INFO§", "§DATA§", "§VAL§", "§WARN§", "§ERROR§",
                "§Y§", "§N§", "§WHITE§", "§PURPLE§", "§END§"
            };
            for (int i = 0; i < tokens.Length; i++)
                text = text.Replace(tokens[i], string.Empty);
            text = text.Replace("⚠️", string.Empty).Replace("⚠", string.Empty);
            return text.Trim();
        }

        private void RefreshHudFromSelectedPot()
        {
            PotSlot pot = _selectedPotIndex < _hudPots.Count ? _hudPots[_selectedPotIndex] : null;
            var state = pot != null && pot.PotActions != null ? pot.PotActions.PotState : null;
            var plantData = state != null ? state.GetPlantData() : null;
            bool empty = state == null || state.IsEmpty || !state.HasPlant;

            Sprite previewSprite = null;
            if (empty)
                previewSprite = _terminalPotPreviewConfig != null ? _terminalPotPreviewConfig.statusVuotoSprite : null;
            else
                previewSprite = ResolveIncubatorSprite(state, plantData);

            if (_hudPlantPreview != null)
            {
                if (previewSprite != null)
                {
                    _hudPlantPreview.style.backgroundImage = new StyleBackground(previewSprite);
                    _hudPlantPreview.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                }
                else
                    _hudPlantPreview.style.backgroundImage = new StyleBackground(StyleKeyword.Null);
            }

            if (_hudPlantName != null)
            {
                _hudPlantName.text = empty ? "---" : GetPotDisplayName(state, plantData);
                // Colore nome per famiglia: Giallo Standard, Verde Pure, Rosso Evil
                _hudPlantName.RemoveFromClassList("pcv3-hud-plant-name-standard");
                _hudPlantName.RemoveFromClassList("pcv3-hud-plant-name-pure");
                _hudPlantName.RemoveFromClassList("pcv3-hud-plant-name-evil");
                if (!empty && plantData != null)
                {
                    switch (plantData.Family)
                    {
                        case PlantFamily.Standard: _hudPlantName.AddToClassList("pcv3-hud-plant-name-standard"); break;
                        case PlantFamily.Pure: _hudPlantName.AddToClassList("pcv3-hud-plant-name-pure"); break;
                        case PlantFamily.Evil: _hudPlantName.AddToClassList("pcv3-hud-plant-name-evil"); break;
                        default: _hudPlantName.AddToClassList("pcv3-hud-plant-name-standard"); break;
                    }
                }
            }
            if (_hudPlantCode != null) _hudPlantCode.text = empty ? "---" : $"[{FormatPlantFamilyBadge(state.PlantCode)}]";
            if (_hudPlantLevel != null) _hudPlantLevel.text = empty ? "LEVEL ---" : $"LEVEL {state.PlantLevel}";
            if (_hudPlantFamily != null)
            {
                _hudPlantFamily.enableRichText = true;
                if (empty)
                    _hudPlantFamily.text = "Famiglia: ---";
                else
                {
                    var family = GetPlantFamilyForDisplay(plantData, state);
                    string familyName = family.ToString(); // "Standard", "Pure", "Evil"
                    string hex = ColorUtility.ToHtmlStringRGB(GetFamilyColor(family));
                    _hudPlantFamily.text = $"Famiglia: <color=#{hex}>{familyName}</color>";
                }
            }
            if (_hudPlantOneliner != null)
            {
                string desc = plantData != null && !string.IsNullOrWhiteSpace(plantData.Description) ? plantData.Description : "---";
                _hudPlantOneliner.text = desc;
            }
            if (_hudEffectsSummary != null)
            {
                if (empty || pot == null)
                    _hudEffectsSummary.text = "";
                else
                    _hudEffectsSummary.text = BotanicalPowerFacade.BuildPcv3CenterEffectsText(pot.PotId, _phSystem);
            }

            if (_hudLivePill != null)
            {
                _hudLivePill.RemoveFromClassList("pcv3-hud-live-pill-offline");
                if (empty) _hudLivePill.AddToClassList("pcv3-hud-live-pill-offline");
            }
            if (_hudLiveDot != null)
            {
                _hudLiveDot.RemoveFromClassList("pcv3-hud-live-dot-offline");
                if (empty) _hudLiveDot.AddToClassList("pcv3-hud-live-dot-offline");
            }
            if (_hudLiveLabel != null)
                _hudLiveLabel.text = empty ? "Offline" : "LIVE";

            RefreshVitalBlocks(state, plantData);
            UpdateQuickActionPanel(pot, state, plantData, empty);
        }

        private Sprite ResolveIncubatorSprite(PotStateModel potState, PlantData plantData)
        {
            if (_terminalPotPreviewConfig == null) return null;
            if (potState == null || !potState.HasPlant) return _terminalPotPreviewConfig.statusVuotoSprite;

            PlantCondition condition = (PlantCondition)potState.ConditionLabel;
            if (condition == PlantCondition.Morta && _terminalPotPreviewConfig.deadSprite != null)
                return _terminalPotPreviewConfig.deadSprite;

            int stage = potState.Stage;
            switch (stage)
            {
                case (int)PlantStage.Empty: return _terminalPotPreviewConfig.statusVuotoSprite;
                case (int)PlantStage.Seed: return _terminalPotPreviewConfig.seedSprite;
                case (int)PlantStage.Sprout: return _terminalPotPreviewConfig.sproutSprite;
                case (int)PlantStage.Growth:
                case (int)PlantStage.Resting:
                case (int)PlantStage.HarvestReady: return _terminalPotPreviewConfig.adultSprite;
                case (int)PlantStage.Flowering: return _terminalPotPreviewConfig.floweringSprite;
                default: return _terminalPotPreviewConfig.statusVuotoSprite;
            }
        }

        private void RefreshVitalBlocks(PotStateModel state, PlantData plantData)
        {
            bool empty = state == null || state.IsEmpty || !state.HasPlant;
            string conditionName = null;
            bool hasCondition = !empty && TryGetCondition(state, plantData, out int conditionScore, out conditionName);

            string stageText = !empty && state != null ? PlantCardFormatters.FormatGrowthStage((PlantStage)state.Stage) : "---";
            string levelText = !empty && state != null ? state.PlantLevel.ToString() : "---";
            string conditionText = !empty && state != null && hasCondition && conditionName != null ? conditionName.ToUpperInvariant() : "---";
            string phAffinityText = plantData != null ? PlantCardFormatters.FormatPhRange(plantData.OptimalPhMin, plantData.OptimalPhMax) : "---";
            string cicliVitaliText = !empty && state != null ? state.CompletedCycles.ToString() : "---";
            // BLK-02.08: LED compatibilità per famiglia (Blu / Rosso / Entrambi)
            string ledCompatText = "---";
            if (plantData != null && !empty)
            {
                var ledCompat = LedCompatibilityHelper.GetCompatibleLedTypes(plantData.Family);
                ledCompatText = ledCompat switch
                {
                    LedCompatibility.BlueOnly => "Blu",
                    LedCompatibility.RedOnly => "Rosso",
                    LedCompatibility.Both => "Entrambi",
                    _ => "Entrambi"
                };
            }
            float drift = plantData != null ? plantData.DailyPhDrift : 0f;
            string phDriftText = plantData != null ? PlantCardFormatters.FormatPhDrift(drift) : "---";
            string growthText = !empty && state != null ? $"W:{state.GrowthPointsWater} L:{state.GrowthPointsLight} F:{state.GrowthPointsFertilizer}" : "---";
            string hydrationText = !empty && state != null && _potSystemConfig != null
                ? $"{PlantCardCalculators.CalculateHydrationPercent(state.Hydration, _potSystemConfig.MaxHydration)}%"
                : "---";
            // Stress Luce: stesso calcolo di Status (GetConsecutiveLedDays / MaxDaysForFullStress)
            int lightStressPercent = 0;
            if (!empty && state != null && _potSystemConfig != null)
            {
                int consecutiveDays = state.GetConsecutiveLedDays();
                int maxDaysForFullStress = Mathf.Max(1, _potSystemConfig.MaxDaysForFullStress);
                lightStressPercent = Mathf.RoundToInt(Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f);
            }
            string lightStressText = !empty && state != null && _potSystemConfig != null ? $"{lightStressPercent}%" : "---";

            // Rischio Muffa (0-3): Nessuno / Lieve / Severo / Critico — quando POT vuoto mostrare ---
            string moldText = "---";
            string moldClass = "pcv3-value-green";
            if (!empty && state != null)
            {
                int mold = Mathf.Clamp(state.MoldRiskLevel, 0, 3);
                moldText = mold switch { 0 => "Nessuno", 1 => "Lieve", 2 => "Severo", 3 => "Critico", _ => "---" };
                moldClass = mold == 0 ? "pcv3-value-green" : (mold == 1 ? "pcv3-value-yellow" : "pcv3-value-red");
            }

            // Trend — quando POT vuoto mostrare ---
            string trendText = "---";
            string trendClass = "pcv3-value-yellow";
            if (!empty && state != null)
            {
                var trend = (ForecastDirection)state.ForecastDirection;
                trendText = trend switch
                {
                    ForecastDirection.Up => "▲ CRESCITA",
                    ForecastDirection.Down => "▼ CALO",
                    _ => "■ STABILE"
                };
                trendClass = trend == ForecastDirection.Up ? "pcv3-value-green" : (trend == ForecastDirection.Down ? "pcv3-value-red" : "pcv3-value-yellow");
            }

            void UpdateBlock(VisualElement container, string[] values, string[] valueClasses)
            {
                if (container == null || values == null) return;
                var statRows = container.Query<VisualElement>(className: "pcv3-potcard-stat-row").ToList();
                for (int i = 0; i < values.Length && i < statRows.Count; i++)
                {
                    var valueLabel = statRows[i].Q<Label>(className: "pcv3-potcard-stat-value");
                    if (valueLabel != null)
                    {
                        valueLabel.text = values[i];
                        valueLabel.RemoveFromClassList("pcv3-value-green");
                        valueLabel.RemoveFromClassList("pcv3-value-blue");
                        valueLabel.RemoveFromClassList("pcv3-value-yellow");
                        valueLabel.RemoveFromClassList("pcv3-value-red");
                        if (i < valueClasses.Length && !string.IsNullOrEmpty(valueClasses[i]))
                            valueLabel.AddToClassList(valueClasses[i]);
                    }
                }
            }

            UpdateBlock(_vitalBlock1,
                new[] { stageText, levelText, conditionText, phAffinityText, cicliVitaliText, ledCompatText },
                new[] { "pcv3-value-green", "pcv3-value-yellow", "pcv3-value-green", "pcv3-value-blue", "pcv3-value-green", "pcv3-value-yellow" });
            UpdateBlock(_vitalBlock2,
                new[] { hydrationText, lightStressText, phDriftText, growthText, moldText, trendText },
                new[] { "pcv3-value-blue", "pcv3-value-yellow", "", "pcv3-value-green", moldClass, trendClass });
            ApplyPhDriftStatColorFromDomeBand(_vitalBlock2, 2, drift);
        }

        /// <summary>Colore valore pH drift dalla banda Dome corrente (fallback classe rosso/blu se PhSystem assente).</summary>
        private void ApplyPhDriftStatColorFromDomeBand(VisualElement container, int statRowIndex, float driftSignFallback)
        {
            if (container == null) return;
            var statRows = container.Query<VisualElement>(className: "pcv3-potcard-stat-row").ToList();
            if (statRowIndex >= statRows.Count) return;
            var valueLabel = statRows[statRowIndex].Q<Label>(className: "pcv3-potcard-stat-value");
            if (valueLabel == null) return;
            valueLabel.RemoveFromClassList("pcv3-value-green");
            valueLabel.RemoveFromClassList("pcv3-value-blue");
            valueLabel.RemoveFromClassList("pcv3-value-yellow");
            valueLabel.RemoveFromClassList("pcv3-value-red");
            if (_phSystem != null)
                valueLabel.style.color = new StyleColor(PhGradientDisplayColors.GetColorForPhBand(_phSystem.EvaluateState()));
            else
            {
                valueLabel.style.color = StyleKeyword.Null;
                valueLabel.AddToClassList(driftSignFallback > 0 ? "pcv3-value-red" : "pcv3-value-blue");
            }
        }

        private void RefreshPotCards()
        {
            if (_potList == null) return;
            _potList.contentContainer.Clear();

            var pots = FindPots();
            foreach (var pot in pots)
            {
                _potList.Add(BuildPotCard(pot));
            }
        }

        private VisualElement BuildPotCard(PotSlot pot)
        {
            string potId = pot != null ? pot.PotId : "POT-???";
            var state = pot != null && pot.PotActions != null ? pot.PotActions.PotState : null;
            var plantData = state != null ? state.GetPlantData() : null;
            bool empty = state == null || state.IsEmpty || !state.HasPlant;

            if (empty)
            {
                var emptyRoot = new VisualElement();
                emptyRoot.AddToClassList("pcv3-potcard");
                emptyRoot.AddToClassList("pcv3-potcard-empty");

                var headerBar = new VisualElement();
                headerBar.AddToClassList("pcv3-potcard-headerbar");
                var headerText = new Label(potId);
                headerText.AddToClassList("pcv3-potcard-headertext");
                headerBar.Add(headerText);
                emptyRoot.Add(headerBar);

                var body = new VisualElement();
                body.AddToClassList("pcv3-potcard-body");

                var preview = new VisualElement();
                preview.AddToClassList("pcv3-potcard-preview");
                var lens = new VisualElement();
                lens.AddToClassList("pcv3-potcard-lens");
                preview.Add(lens);
                body.Add(preview);

                var info = new VisualElement();
                info.AddToClassList("pcv3-potcard-info");
                var standby = new Label("[STANDBY]");
                standby.AddToClassList("pcv3-potcard-standby");
                var sub = new Label("Pronto per la coltivazione");
                sub.AddToClassList("pcv3-potcard-subtext");
                info.Add(standby);
                info.Add(sub);
                body.Add(info);

                emptyRoot.Add(body);
                return emptyRoot;
            }

            if (_potCardTemplate == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "PlantCardV3TerminalController: _potCardTemplate non assegnato!");
                return new VisualElement();
            }

            var templateRoot = _potCardTemplate.Instantiate();
            // Se il template ha un solo elemento root, Instantiate() restituisce quell'elemento direttamente
            // Altrimenti cerca l'elemento con name="pcv3-potcard"
            VisualElement cardRoot = templateRoot;
            if (templateRoot.name != "pcv3-potcard")
            {
                cardRoot = templateRoot.Q<VisualElement>("pcv3-potcard");
                if (cardRoot == null)
                {
                    SporiumLogger.LogError(LogCategory.UI, "PlantCardV3TerminalController: Template non contiene pcv3-potcard!");
                    return templateRoot;
                }
            }

            bool hasCondition = TryGetCondition(state, plantData, out int conditionScore, out string conditionName);
            Color familyColor = GetFamilyColor(plantData != null ? plantData.Family : PlantFamily.Standard);

            var topbar = cardRoot.Q<VisualElement>("pcv3-potcard-topbar");
            var titleLabel = cardRoot.Q<Label>("pcv3-potcard-title");
            var badgeLabel = cardRoot.Q<Label>("pcv3-potcard-badge");
            var plantBox = cardRoot.Q<VisualElement>("pcv3-potcard-plantbox");
            var plantImage = cardRoot.Q<VisualElement>("pcv3-potcard-plant-image");
            var liveDot = cardRoot.Q<VisualElement>("pcv3-potcard-live-dot");
            var descLabel = cardRoot.Q<Label>("pcv3-potcard-desc");
            var statsContainer = cardRoot.Q<VisualElement>("pcv3-potcard-stats");
            var footer = cardRoot.Q<VisualElement>("pcv3-potcard-footer");
            var openButton = cardRoot.Q<Button>("pcv3-potcard-open");

            if (titleLabel != null)
                titleLabel.text = $"{potId} -- {GetPotDisplayName(state)}";
            if (badgeLabel != null)
                badgeLabel.text = FormatPlantFamilyBadge(state.PlantCode);

            if (plantBox != null)
            {
                plantBox.style.borderLeftColor = familyColor;
                plantBox.style.borderRightColor = familyColor;
                plantBox.style.borderTopColor = familyColor;
                plantBox.style.borderBottomColor = familyColor;
            }

            Sprite previewSprite = null;
            if (pot != null)
            {
                Transform windowContent = pot.transform.Find("WindowContent");
                if (windowContent != null)
                    previewSprite = windowContent.GetComponent<SpriteRenderer>()?.sprite;
                if (previewSprite == null)
                    previewSprite = pot.GetComponentInChildren<SpriteRenderer>()?.sprite;
            }

            if (plantImage != null && previewSprite != null)
            {
                plantImage.style.backgroundImage = new StyleBackground(previewSprite);
                plantImage.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            }

            if (liveDot != null)
            {
                bool dotOn = true;
                liveDot.schedule.Execute(() =>
                {
                    liveDot.style.opacity = dotOn ? 1f : 0.4f;
                    dotOn = !dotOn;
                }).Every(500);
            }

            if (descLabel != null)
            {
                string description = plantData != null && !string.IsNullOrWhiteSpace(plantData.Description) ? plantData.Description : "---";
                descLabel.text = description;
            }

            if (statsContainer != null)
            {
                // Popola i valori delle stat rows esistenti nel template invece di ricrearle
                string stageText = state != null ? PlantCardFormatters.FormatGrowthStage((PlantStage)state.Stage) : "---";
                string levelText = state != null ? state.PlantLevel.ToString() : "---";
                string conditionText = hasCondition ? conditionName.ToUpperInvariant() : "---";
                string phAffinityText = plantData != null ? PlantCardFormatters.FormatPhRange(plantData.OptimalPhMin, plantData.OptimalPhMax) : "---";
                float drift = plantData != null ? plantData.DailyPhDrift : 0f;
                string phDriftText = plantData != null ? PlantCardFormatters.FormatPhDrift(drift) : "---";
                string growthText = state != null ? $"W:{state.GrowthPointsWater} L:{state.GrowthPointsLight} F:{state.GrowthPointsFertilizer}" : "---";
                string hydrationText = state != null && _potSystemConfig != null
                    ? $"{PlantCardCalculators.CalculateHydrationPercent(state.Hydration, _potSystemConfig.MaxHydration)}%"
                    : "---";
                // Stress Luce: stesso calcolo di Status (GetConsecutiveLedDays / MaxDaysForFullStress)
                int lightStressPct = 0;
                if (state != null && _potSystemConfig != null)
                {
                    int consecutiveDays = state.GetConsecutiveLedDays();
                    int maxDaysForFullStress = Mathf.Max(1, _potSystemConfig.MaxDaysForFullStress);
                    lightStressPct = Mathf.RoundToInt(Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f);
                }
                string lightStressText = state != null && _potSystemConfig != null ? $"{lightStressPct}%" : "---";

                // Trova le stat rows esistenti e popola i valori
                var statRows = statsContainer.Query<VisualElement>(className: "pcv3-potcard-stat-row").ToList();
                int rowIndex = 0;
                
                void UpdateStatValue(int index, string value, string valueClass)
                {
                    if (index < statRows.Count)
                    {
                        var valueLabel = statRows[index].Q<Label>(className: "pcv3-potcard-stat-value");
                        if (valueLabel != null)
                        {
                            valueLabel.text = value;
                            // Rimuovi tutte le classi di colore e aggiungi quella corretta
                            valueLabel.RemoveFromClassList("pcv3-value-green");
                            valueLabel.RemoveFromClassList("pcv3-value-blue");
                            valueLabel.RemoveFromClassList("pcv3-value-yellow");
                            valueLabel.RemoveFromClassList("pcv3-value-red");
                            if (!string.IsNullOrEmpty(valueClass))
                                valueLabel.AddToClassList(valueClass);
                        }
                    }
                }

                UpdateStatValue(rowIndex++, stageText, "pcv3-value-green");
                UpdateStatValue(rowIndex++, levelText, "pcv3-value-yellow");
                UpdateStatValue(rowIndex++, conditionText, "pcv3-value-green");
                UpdateStatValue(rowIndex++, phAffinityText, "pcv3-value-blue");
                UpdateStatValue(rowIndex++, phDriftText, "");
                UpdateStatValue(rowIndex++, growthText, "pcv3-value-green");
                // Separator è già nel template, skip
                rowIndex++; // Skip separator
                UpdateStatValue(rowIndex++, hydrationText, "pcv3-value-blue");
                UpdateStatValue(rowIndex++, lightStressText, "pcv3-value-yellow");
                ApplyPhDriftStatColorFromDomeBand(statsContainer, 4, drift);
            }

            if (openButton != null)
            {
                openButton.clicked += () => HandleCommand($"OPEN {potId}");
            }

            return cardRoot;
        }

        private void FocusInput()
        {
            if (_input == null) return;
            ForceFocusCommandInput();
        }

        private void SwitchToConsole()
        {
            if (_consoleView != null) _consoleView.style.display = DisplayStyle.Flex;
            if (_protocolView != null) _protocolView.style.display = DisplayStyle.None;
            if (_detailView != null) _detailView.style.display = DisplayStyle.None;

            // Rimuovi detail page corrente se esiste
            if (_currentDetailPage != null)
            {
                _currentDetailPage.RemoveFromHierarchy();
                _currentDetailPage = null;
            }
        }

        private void SwitchToProtocol()
        {
            if (_consoleView != null) _consoleView.style.display = DisplayStyle.None;
            if (_protocolView != null) _protocolView.style.display = DisplayStyle.Flex;
            if (_detailView != null) _detailView.style.display = DisplayStyle.None;
        }

        private void RenderWelcome(bool clearConsole)
        {
            if (clearConsole)
                _consoleBuffer.Clear();

            if (clearConsole)
            {
                AppendRawLine("§TITLE§TERMINALE CONTROLLO INCUBATORE SPORIUM v3.1§END§");
                AppendRawLine("§TITLE§SISTEMA GESTIONE COLTIVAZIONE AUTOMATIZZATA§END§");
                AppendRawLine("");
            }
            AppendRawLine("§INFO§INCUBATORE BOTANICO SPORIUM - Monitoraggio coltivazione in tempo reale, analisi vitali e diario§END§");
            AppendRawLine("");
            AppendRawLine("△ §TITLE§TERMINALE MONITORAGGIO VASI INIZIALIZZATO§END§");
            AppendRawLine("<color=#E6C96F>──────────────────────────────────────────────────────────────────────────────</color>");
            AppendRawLine("▶ §CMD§DIGITA START PER L'ELENCO COMANDI§END§");
            AppendRawLine("<color=#E6C96F>──────────────────────────────────────────────────────────────────────────────</color>");
            AppendRawLine("<color=#79E679>△ TUTTE LE AZIONI IN CODA FINO A CONFERMA SEQUENZA</color>");
            AppendRawLine("<color=#7FFF7A>DIGITA</color> <color=#5DB6E3>STATUS</color> <color=#7FFF7A>- Stato, progressione e requisiti vasi</color>");
            AppendRawLine("<color=#7FFF7A>[</color><color=#7FFF7A>+</color><color=#7FFF7A>]</color> <color=#7FFF7A>DIGITA</color> <color=#5DB6E3>PROTOCOL</color> <color=#7FFF7A>PER PROTOCOLLO BIOLOGICO DOME_02</color>");
            string overview = BuildIncubatorOverviewLine();
            if (!string.IsNullOrEmpty(overview))
                AppendRawLine(overview);
            AppendRawLine("");

            FlushConsole();
        }

        /// <summary>
        /// Testo overview tra parentesi quadre: pot liberi, pot occupati con nome e codice pianta.
        /// </summary>
        private string BuildIncubatorOverviewLine()
        {
            var pots = FindPots();
            int free = 0;
            var occupied = new System.Collections.Generic.List<string>();
            foreach (var pot in pots)
            {
                var state = pot?.PotActions?.PotState;
                if (state == null || state.IsEmpty || !state.HasPlant)
                {
                    free++;
                    continue;
                }
                var plantData = state.GetPlantData();
                string name = GetPotDisplayName(state, plantData);
                string code = state.PlantCode ?? "---";
                occupied.Add($"{name} ({code})");
            }
            if (pots.Count == 0)
                return "<color=#B8B8B8>[Overview: nessun vaso rilevato]</color>";
            string freeStr = free == 1 ? "1 pot libero" : $"{free} pot liberi";
            string occStr = occupied.Count == 0 ? "nessun pot occupato" : (occupied.Count == 1 ? "1 pot occupato: " + occupied[0] : $"{occupied.Count} pot occupati: " + string.Join(", ", occupied));
            return $"<color=#B8B8B8>[Overview: {freeStr}, {occStr}]</color>";
        }

        private void StartBootSequence()
        {
            StopTypewriter();
            StopBootSequence();
            _bootRoutine = StartCoroutine(BootSequenceRoutine());
        }

        private void StopBootSequence()
        {
            if (_bootRoutine != null)
            {
                StopCoroutine(_bootRoutine);
                _bootRoutine = null;
            }
            _bootSequenceActive = false;
            if (_input != null)
                _input.SetEnabled(true);
        }

        private IEnumerator BootSequenceRoutine()
        {
            _bootSequenceActive = true;
            if (_input != null)
                _input.SetEnabled(false);

            PlayBootStartSfx();

            _consoleBuffer.Clear();
            FlushConsole();

            string[] lines =
            {
                "§TITLE§SPORIUM INCUBATOR CONTROL TERMINAL v3.1§END§",
                "§TITLE§AUTOMATED CULTIVATION MANAGEMENT SYSTEM§END§",
                "",
                "[BOOT] Inizializzazione sistema...",
                "[OK] Checksum BIOS verificato",
                "[OK] Test memoria superato",
                "[INIT] Caricamento moduli coltivazione...",
                "  ▸ HVAC-CTRL............ [ONLINE]",
                "  ▸ HYDRATION-SYS........ [ONLINE]",
                "  ▸ LED-SPECTRUM-A....... [ONLINE]",
                "  ▸ LED-SPECTRUM-B....... [ONLINE]",
                "  ▸ SOIL-SENSORS......... [ONLINE]",
                "  ▸ pH-MONITOR........... [ONLINE]",
                "[DB] Connessione al database coltivazione...",
                "[OK] Database montato: DOME_02_INCUBATOR",
                "[OK] Record POT sincronizzati (6 unità)",
                "[OK] Log storici indicizzati",
                "[NET] Collegamento rete Vault...",
                "[OK] Connesso a SPORIUM-NET",
                "[OK] Sistema coda azioni pronto",
                "[READY] Terminale controllo incubatore inizializzato"
            };

            foreach (var line in lines)
            {
                AppendRawLine($"<color=#E6C96F>{line}</color>");
                FlushConsole();
                float delay = IsBootSectionLine(line) ? _bootSectionDelay : _bootLineDelay;
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
            }

            // Collassa la sequenza di boot in una sola riga per liberare spazio
            _consoleBuffer.Clear();
            AppendRawLine("<color=#E6C96F>[BOOT] Booting Sequence completato</color>");
            AppendRawLine("");
            RenderWelcome(clearConsole: false);
            PrintStartCommands();

            _bootSequenceActive = false;
            if (_input != null)
                _input.SetEnabled(true);
        }

        private IEnumerator TypewriterRoutine()
        {
            try
            {
                _nextTypewriterSfxTime = 0f;
                bool longOutputMode = IsLongOutputQueued();
                int linesSinceFlush = 0;
                while (_typewriterQueue.Count > 0)
                {
                    string line = _typewriterQueue.Dequeue();
                    if (line == null) line = string.Empty;

                    float delay = GetTypewriterDelayForLine(line)
                                  * _typewriterGlobalSpeedMultiplier
                                  * (longOutputMode ? _typewriterLongOutputMultiplier : 1f)
                                  * _typewriterCommandSpeedMultiplier;
                    int blockSize = longOutputMode ? _typewriterBlockSizeLong : _typewriterBlockSizeShort;
                    blockSize = Mathf.Max(1, blockSize * _typewriterCommandBlockMultiplier);
                    blockSize = Mathf.Max(1, blockSize);
                    int i = 0;
                    while (i < line.Length)
                    {
                        char c = line[i];
                        if (c == '<')
                        {
                            int end = line.IndexOf('>', i);
                            if (end >= 0)
                            {
                                _consoleBuffer.Append(line, i, end - i + 1);
                                i = end + 1;
                                continue;
                            }
                        }

                        int wrote = 0;
                        while (i < line.Length && wrote < blockSize)
                        {
                            c = line[i];
                            if (c == '<')
                                break;
                            _consoleBuffer.Append(c);
                            i++;
                            wrote++;
                        }
                        TryPlayTypewriterSfx();
                        if (delay > 0f)
                            yield return new WaitForSeconds(delay);
                    }

                    _consoleBuffer.AppendLine();
                    linesSinceFlush++;
                    if (linesSinceFlush >= _typewriterFlushEveryNLines)
                    {
                        FlushConsoleImmediate();
                        linesSinceFlush = 0;
                    }
                }
            }
            finally
            {
                _typewriterRoutine = null;
                while (_typewriterQueue != null && _typewriterQueue.Count > 0)
                {
                    string line = _typewriterQueue.Dequeue();
                    if (line != null)
                        _consoleBuffer.AppendLine(line);
                }
                FlushConsoleImmediate();
                AutoScrollConsole();
                _consoleScroll?.schedule.Execute(() => AutoScrollConsole()).ExecuteLater(0);
                _consoleScroll?.schedule.Execute(() => AutoScrollConsole()).ExecuteLater(50);
                _consoleScroll?.schedule.Execute(() => AutoScrollConsole()).ExecuteLater(100);
                _consoleScroll?.schedule.Execute(() => AutoScrollConsole()).ExecuteLater(200);
                _consoleScroll?.schedule.Execute(() => AutoScrollConsole()).ExecuteLater(350);
                RequestRefocusSoon();
                if (_pendingStatusSecondHalf != null && _pendingStatusSecondHalf.Count > 0 && _statusSecondHalfRoutine == null)
                    _statusSecondHalfRoutine = StartCoroutine(StatusSecondHalfRoutine());
            }
        }

        private IEnumerator StatusSecondHalfRoutine()
        {
            var list = _pendingStatusSecondHalf;
            if (list == null || list.Count == 0)
            {
                _pendingStatusSecondHalf = null;
                _statusSecondHalfRoutine = null;
                AppendPendingStatusResearchNotes();
                yield break;
            }
            while (list.Count > 0)
            {
                int take = Mathf.Min(StatusSecondHalfChunkSize, list.Count);
                for (int i = 0; i < take; i++)
                {
                    string line = list[0];
                    list.RemoveAt(0);
                    if (!string.IsNullOrEmpty(line))
                        _consoleBuffer.AppendLine(ParseColors(line));
                }
                FlushConsoleImmediate();
                AutoScrollConsole();
                yield return new WaitForSeconds(StatusSecondHalfChunkDelay);
            }
            _pendingStatusSecondHalf = null;
            _statusSecondHalfRoutine = null;
            FlushConsoleImmediate();
            AutoScrollConsole();
            AppendPendingStatusResearchNotes();
            _consoleScroll?.schedule.Execute(() => AutoScrollConsole()).ExecuteLater(50);
            _consoleScroll?.schedule.Execute(() => AutoScrollConsole()).ExecuteLater(150);
        }

        private void StartEnvironmentalPhRefresh()
        {
            StopEnvironmentalPhRefresh();
            if (_phSystem == null || _consoleText == null) return;
            _environmentalPhRefreshRoutine = StartCoroutine(EnvironmentalPhRefreshRoutine());
        }

        private void StopEnvironmentalPhRefresh()
        {
            if (_environmentalPhRefreshRoutine != null)
            {
                StopCoroutine(_environmentalPhRefreshRoutine);
                _environmentalPhRefreshRoutine = null;
            }
        }

        /// <summary>Aggiorna la riga pH DOME in console con lo stesso valore oscillante di TopBar/tooltip (_phSystem.CurrentPh).</summary>
        private IEnumerator EnvironmentalPhRefreshRoutine()
        {
            const float interval = 0.4f;
            var wait = new WaitForSeconds(interval);
            while (_consoleText != null && _phSystem != null)
            {
                yield return wait;
                if (_consoleText == null || _phSystem == null) break;
                string current = _consoleText.text;
                if (string.IsNullOrEmpty(current) || current.IndexOf("pH DOME", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    _environmentalPhRefreshRoutine = null;
                    yield break;
                }
                float phValue = _phSystem.CurrentPh;
                string phStr = phValue.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                string phBand = GetPhBandNameForDisplay(phValue);
                string phColorHex = GetPhColorHexForDomePhBand();
                string phLineRaw = "§DATA§" + StatusDottedLabelOnly("pH DOME") + "§END§<color=" + phColorHex + ">" + phStr + " — " + phBand + "</color>";
                string newLine = ParseColors(phLineRaw);
                string[] lines = current.Split(new[] { "\n", "\r\n" }, StringSplitOptions.None);
                int idx = -1;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].IndexOf("pH DOME", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        idx = i;
                        break;
                    }
                }
                if (idx >= 0)
                {
                    lines[idx] = newLine;
                    string newText = string.Join("\n", lines);
                    _consoleText.text = newText;
                    // Mantieni il buffer in sync così un eventuale Flush non sovrascrive il valore
                    _consoleBuffer.Clear();
                    _consoleBuffer.Append(newText);
                }
            }
            _environmentalPhRefreshRoutine = null;
        }

        private bool IsLongOutputQueued()
        {
            if (_typewriterQueue.Count == 0)
                return false;

            int total = 0;
            foreach (var line in _typewriterQueue)
            {
                if (string.IsNullOrEmpty(line)) continue;
                total += StripRichTextTags(line).Length + 1;
                if (total >= _typewriterLongOutputChars)
                    return true;
            }
            return false;
        }

        private void TryPlayTypewriterSfx()
        {
            if (_typewriterAudioSource == null || _typewriterSfx == null)
                return;

            if (Time.unscaledTime < _nextTypewriterSfxTime)
                return;

            _nextTypewriterSfxTime = Time.unscaledTime + _typewriterSfxInterval;
            _typewriterAudioSource.PlayOneShot(_typewriterSfx);
        }

        private void PlayBootStartSfx()
        {
            if (_typewriterAudioSource == null || _bootStartSfx == null)
                return;

            _typewriterAudioSource.PlayOneShot(_bootStartSfx);
        }

        private float GetTypewriterDelayForLine(string richLine)
        {
            float baseDelay = _typewriterCharDelay;
            if (baseDelay <= 0f)
                return 0f;

            string plain = StripRichTextTags(richLine);
            if (IsFrameLine(plain))
                return baseDelay * _typewriterFrameLineMultiplier;

            if (plain.Length >= _typewriterLongLineThreshold)
                return baseDelay * _typewriterLongLineMultiplier;

            return baseDelay;
        }

        private static string StripRichTextTags(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var sb = new StringBuilder(input.Length);
            bool inTag = false;
            foreach (var c in input)
            {
                if (c == '<')
                {
                    inTag = true;
                    continue;
                }
                if (c == '>' && inTag)
                {
                    inTag = false;
                    continue;
                }
                if (!inTag)
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private static bool IsFrameLine(string plain)
        {
            if (string.IsNullOrWhiteSpace(plain))
                return false;

            foreach (char c in plain)
            {
                if (c == ' ') continue;
                if (IsFrameChar(c)) continue;
                return false;
            }
            return true;
        }

        private static bool IsFrameChar(char c)
        {
            return c == '╔' || c == '╗' || c == '╚' || c == '╝' || c == '╟'
                   || c == '╢' || c == '═' || c == '║' || c == '─' || c == '┌'
                   || c == '┐' || c == '└' || c == '┘' || c == '┼' || c == '┬'
                   || c == '┴' || c == '├' || c == '┤';
        }

        private static bool IsBootSectionLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return false;
            return line.StartsWith("[BOOT]", StringComparison.Ordinal)
                   || line.StartsWith("[INIT]", StringComparison.Ordinal)
                   || line.StartsWith("[DB]", StringComparison.Ordinal)
                   || line.StartsWith("[NET]", StringComparison.Ordinal)
                   || line.StartsWith("[READY]", StringComparison.Ordinal);
        }

        private System.IDisposable BeginTypewriterScope(float speedMultiplier, int blockMultiplier)
        {
            if (!_useTypewriterOnCommand)
                return null;

            _typewriterActive = true;
            _typewriterCommandSpeedMultiplier = Mathf.Max(0.05f, speedMultiplier);
            _typewriterCommandBlockMultiplier = Mathf.Max(1, blockMultiplier);
            return new TypewriterScope(this);
        }

        /// <summary>Esegue l'azione con typewriter velocissimo a gruppi di frasi (output comandi e step).</summary>
        private void RunWithFastTypewriter(System.Action action)
        {
            const float speedMultiplier = 0.12f;  // delay ~12% del normale = velocissimo
            const int blockMultiplier = 28;       // ~28 caratteri per tick = gruppi di frasi
            var scope = BeginTypewriterScope(speedMultiplier, blockMultiplier);
            try { action?.Invoke(); }
            finally { scope?.Dispose(); }
        }

        private sealed class TypewriterScope : System.IDisposable
        {
            private readonly PlantCardV3TerminalController _owner;
            public TypewriterScope(PlantCardV3TerminalController owner) => _owner = owner;
            public void Dispose()
            {
                if (_owner == null) return;
                _owner._typewriterActive = false;
                _owner._typewriterCommandSpeedMultiplier = 1f;
                _owner._typewriterCommandBlockMultiplier = 1;
                _owner.EnsureTypewriterRunning();
            }
        }

        private void AppendRawLine(string line)
        {
            if (_statusLinesCollector != null)
            {
                _statusLinesCollector.Add(line);
                return;
            }
            string parsed = ParseColors(line);
            if (_typewriterActive)
                _typewriterQueue.Enqueue(parsed);
            else
                _consoleBuffer.AppendLine(parsed);
        }

        private void EnsureForecastConditionTooltip()
        {
            if (_root == null) return;

            if (_forecastConditionTooltip != null && _forecastConditionTooltipText != null)
                return;

            _forecastConditionTooltip = _root.Q<VisualElement>("pcv3-forecast-condition-tooltip");
            _forecastConditionTooltipText = _forecastConditionTooltip?.Q<Label>("pcv3-forecast-condition-tooltip-text");
            if (_forecastConditionTooltip != null)
                _forecastConditionTooltip.pickingMode = PickingMode.Ignore;
        }

        private void StartLiveDotPulse()
        {
            if (_hudLiveDot == null) return;
            const string pulseClass = "pcv3-hud-live-dot-pulse";
            _hudLiveDot.schedule.Execute(() =>
            {
                if (_hudLiveDot == null || _hudLiveDot.panel == null) return;
                _liveDotPulseLow = !_liveDotPulseLow;
                if (_liveDotPulseLow)
                    _hudLiveDot.AddToClassList(pulseClass);
                else
                    _hudLiveDot.RemoveFromClassList(pulseClass);
            }).Every(600);
        }

        private void HideForecastConditionTooltip()
        {
            if (_forecastConditionTooltip != null)
                _forecastConditionTooltip.style.display = DisplayStyle.None;
            _shouldHideForecastConditionTooltip = false;
        }

        private void ShowForecastConditionTooltip(VisualElement anchor, PotStateModel pot, PlantData plantData)
        {
            if (anchor == null) return;
            EnsureForecastConditionTooltip();
            if (_forecastConditionTooltip == null || _forecastConditionTooltipText == null) return;

            _shouldHideForecastConditionTooltip = false;

            _forecastConditionTooltip.BringToFront();
            _forecastConditionTooltip.style.display = DisplayStyle.Flex;

            // Update content
            _forecastConditionTooltipText.text = BuildGrowthTooltipLikePlantCardV2(pot, plantData);

            // Position after layout so resolvedStyle.height is valid
            _forecastConditionTooltip.schedule.Execute(() =>
            {
                if (_forecastConditionTooltip == null || _root == null) return;
                _forecastConditionTooltip.BringToFront();

                var lineWorld = anchor.worldBound;
                var rootWorld = _root.worldBound;

                float tooltipWidth = 450f;
                float tooltipHeight = _forecastConditionTooltip.resolvedStyle.height > 0 ? _forecastConditionTooltip.resolvedStyle.height : 250f;

                float tooltipX = lineWorld.xMin + (lineWorld.width - tooltipWidth) / 2f;
                float tooltipY = lineWorld.yMin - tooltipHeight - 10f; // above line

                float localX = tooltipX - rootWorld.xMin;
                float localY = tooltipY - rootWorld.yMin;

                // If no room above, place below
                if (tooltipY < rootWorld.yMin)
                    localY = (lineWorld.yMax - rootWorld.yMin) + 10f;

                // Clamp inside root bounds
                if (localX + tooltipWidth > rootWorld.width)
                    localX = rootWorld.width - tooltipWidth - 10f;
                if (localX < 0)
                    localX = 10f;

                _forecastConditionTooltip.style.left = localX;
                _forecastConditionTooltip.style.top = localY;
            });
        }

        private void EnsureForecastHotspotLayer()
        {
            if (_consoleScroll == null) return;
            var parent = _consoleScroll.contentContainer;
            if (parent == null) return;

            if (_forecastHotspotLayer == null)
            {
                _forecastHotspotLayer = parent.Q<VisualElement>("pcv3-forecast-hotspot-layer");
                if (_forecastHotspotLayer == null)
                {
                    _forecastHotspotLayer = new VisualElement();
                    _forecastHotspotLayer.name = "pcv3-forecast-hotspot-layer";
                    _forecastHotspotLayer.style.position = Position.Absolute;
                    _forecastHotspotLayer.style.left = 0;
                    _forecastHotspotLayer.style.top = 0;
                    // NOTE: Some older UI Toolkit versions behave inconsistently with right/bottom sizing.
                    // We explicitly set width/height from parent layout instead (see GeometryChanged callback).
                    _forecastHotspotLayer.style.display = DisplayStyle.None;
                    _forecastHotspotLayer.pickingMode = PickingMode.Position;
                    _forecastHotspotLayer.focusable = false;
                    parent.Add(_forecastHotspotLayer);
                }

                // Keep layer size in sync with content (so it can receive hover anywhere in content).
                parent.RegisterCallback<GeometryChangedEvent>(_ =>
                {
                    if (_forecastHotspotLayer != null)
                    {
                        _forecastHotspotLayer.style.width = parent.layout.width;
                        _forecastHotspotLayer.style.height = parent.layout.height;
                    }
                });
            }

            if (_forecastHoverAnchor == null && _forecastHotspotLayer != null)
            {
                _forecastHoverAnchor = new VisualElement();
                _forecastHoverAnchor.name = "pcv3-forecast-hover-anchor";
                _forecastHoverAnchor.style.position = Position.Absolute;
                _forecastHoverAnchor.style.left = 0;
                _forecastHoverAnchor.style.top = 0;
                _forecastHoverAnchor.style.width = 0;
                _forecastHoverAnchor.style.height = 0;
                _forecastHoverAnchor.style.backgroundColor = new Color(0, 0, 0, 0);
                _forecastHoverAnchor.pickingMode = PickingMode.Ignore;
                _forecastHotspotLayer.Add(_forecastHoverAnchor);
            }

            // Robust hover detection: pointer move hit-testing on computed row ranges.
            if (_forecastHotspotLayer != null)
            {
                _forecastHotspotLayer.UnregisterCallback<PointerMoveEvent>(OnForecastPointerMove);
                _forecastHotspotLayer.UnregisterCallback<PointerLeaveEvent>(OnForecastPointerLeave);
                _forecastHotspotLayer.RegisterCallback<PointerMoveEvent>(OnForecastPointerMove);
                _forecastHotspotLayer.RegisterCallback<PointerLeaveEvent>(OnForecastPointerLeave);
            }
        }

        private readonly struct ForecastConditionHoverRow
        {
            public readonly float YMin;
            public readonly float YMax;
            public readonly float X;
            public readonly float Width;
            public readonly PotStateModel State;
            public readonly PlantData PlantData;

            public ForecastConditionHoverRow(float yMin, float yMax, float x, float width, PotStateModel state, PlantData plantData)
            {
                YMin = yMin;
                YMax = yMax;
                X = x;
                Width = width;
                State = state;
                PlantData = plantData;
            }
        }

        private void OnForecastPointerMove(PointerMoveEvent evt)
        {
            if (!_lastOutputWasForecast) return;
            if (_forecastHotspotLayer == null || _consoleText == null) return;
            if (_forecastConditionHoverRows.Count == 0) return;

            var local = evt.localPosition;
            float y = local.y;

            int hit = -1;
            for (int i = 0; i < _forecastConditionHoverRows.Count; i++)
            {
                var row = _forecastConditionHoverRows[i];
                if (y >= row.YMin && y <= row.YMax)
                {
                    hit = i;
                    break;
                }
            }

            if (hit == _forecastHoveredRowIndex)
                return;

            _forecastHoveredRowIndex = hit;

            if (hit < 0)
            {
                HideForecastConditionTooltip();
                return;
            }

            var r = _forecastConditionHoverRows[hit];
            if (_forecastHoverAnchor != null)
            {
                _forecastHoverAnchor.style.left = r.X;
                _forecastHoverAnchor.style.top = r.YMin;
                _forecastHoverAnchor.style.width = r.Width;
                _forecastHoverAnchor.style.height = Mathf.Max(6f, r.YMax - r.YMin);
            }
            ShowForecastConditionTooltip(_forecastHoverAnchor ?? _forecastHotspotLayer, r.State, r.PlantData);
        }

        private void OnForecastPointerLeave(PointerLeaveEvent evt)
        {
            _forecastHoveredRowIndex = -1;
            HideForecastConditionTooltip();
        }

        private void ClearForecastConditionHotspots()
        {
            _lastOutputWasForecast = false;
            _forecastHoveredRowIndex = -1;
            _forecastConditionHoverRows.Clear();
            if (_forecastHotspotLayer != null)
            {
                _forecastHotspotLayer.Clear();
                _forecastHotspotLayer.style.display = DisplayStyle.None;
            }
            HideForecastConditionTooltip();
        }

        private void ScheduleRebuildForecastConditionHotspots()
        {
            _lastOutputWasForecast = true;
            EnsureForecastHotspotLayer();
            if (_forecastHotspotLayer == null) return;

            _forecastHotspotLayer.Clear();
            _forecastHotspotLayer.style.display = DisplayStyle.Flex;
            _forecastHotspotLayer.BringToFront();

            void TryBuild()
            {
                if (!_lastOutputWasForecast) return;
                if (_consoleText == null || _consoleScroll == null) return;

                // If typewriter is active, wait until output is fully flushed.
                if (_typewriterQueue.Count > 0 || _typewriterRoutine != null)
                {
                    _forecastHotspotLayer.schedule.Execute(TryBuild).ExecuteLater(50);
                    return;
                }

                // Wait one layout tick to ensure _consoleText.layout is valid.
                _forecastHotspotLayer.schedule.Execute(RebuildForecastConditionHotspots).ExecuteLater(0);
            }

            _forecastHotspotLayer.schedule.Execute(TryBuild).ExecuteLater(0);
        }

        private void RebuildForecastConditionHotspots()
        {
            if (!_lastOutputWasForecast) return;
            if (_forecastHotspotLayer == null || _consoleText == null) return;

            _forecastHotspotLayer.Clear();
            HideForecastConditionTooltip();
            _forecastHotspotLayer.BringToFront();
            _forecastConditionHoverRows.Clear();
            _forecastHoveredRowIndex = -1;

            // Re-add hover anchor after Clear()
            if (_forecastHoverAnchor == null)
            {
                _forecastHoverAnchor = new VisualElement();
                _forecastHoverAnchor.name = "pcv3-forecast-hover-anchor";
                _forecastHoverAnchor.style.position = Position.Absolute;
                _forecastHoverAnchor.style.backgroundColor = new Color(0, 0, 0, 0);
                _forecastHoverAnchor.pickingMode = PickingMode.Ignore;
            }
            _forecastHotspotLayer.Add(_forecastHoverAnchor);

            string raw = _consoleText.text ?? string.Empty;
            if (string.IsNullOrEmpty(raw)) return;

            string[] lines = raw.Split('\n');

            // Some Unity UI Toolkit versions don't expose resolvedStyle.lineHeight.
            // Prefer measuring average line height from the label layout when possible.
            int lineCount = lines.Length;
            if (lineCount > 0 && string.IsNullOrEmpty(lines[^1]))
                lineCount -= 1; // ignore trailing newline empty line

            float lineHeight = 0f;
            if (_consoleText.layout.height > 0f && lineCount > 0)
                lineHeight = _consoleText.layout.height / lineCount;

            float fs = _consoleText.resolvedStyle.fontSize;
            float fallback = fs > 0 ? fs * 1.2f : 14f;
            if (lineHeight <= 1f)
                lineHeight = fallback;

            // Give a bit of extra hit area to make hover easier.
            float hitHeight = Mathf.Max(10f, lineHeight + 2f);

            float baseX = _consoleText.layout.x;
            float baseY = _consoleText.layout.y;
            float width = Mathf.Max(10f, _consoleText.layout.width);

            for (int i = 0; i < lines.Length; i++)
            {
                string plain = StripRichTextTags(lines[i] ?? string.Empty).TrimStart();
                if (!plain.StartsWith("Condizione:", StringComparison.OrdinalIgnoreCase))
                    continue;

                string potId = null;
                for (int j = i; j >= 0; j--)
                {
                    string headerPlain = StripRichTextTags(lines[j] ?? string.Empty);
                    int arrow = headerPlain.IndexOf('►');
                    if (arrow < 0) continue;

                    // Expected: "► POT-001 | ..."
                    string after = headerPlain[(arrow + 1)..].Trim();
                    int pipe = after.IndexOf('|');
                    potId = (pipe >= 0 ? after[..pipe] : after).Trim();
                    if (!string.IsNullOrEmpty(potId))
                        break;
                }
                if (string.IsNullOrEmpty(potId))
                {
                    continue;
                }

                var potSlot = FindPotById(potId);
                var state = potSlot != null && potSlot.PotActions != null ? potSlot.PotActions.PotState : null;
                if (state == null || state.IsEmpty || !state.HasPlant)
                    continue;

                var plantData = state.GetPlantData();
                if (plantData == null)
                    continue;

                float yMin = baseY + (i * lineHeight);
                float yMax = yMin + hitHeight;
                _forecastConditionHoverRows.Add(new ForecastConditionHoverRow(yMin, yMax, baseX, width, state, plantData));
            }

            RequestRefocusSoon();
        }

        private string BuildGrowthTooltipLikePlantCardV2(PotStateModel state, PlantData plantData)
        {
            var sb = new StringBuilder();

            if (_potSystemConfig == null || state == null || state.IsEmpty || !state.HasPlant || plantData == null)
            {
                sb.AppendLine("<b>Crescita: Informazioni non disponibili</b>");
                return sb.ToString();
            }

            PlantStage currentStage = (PlantStage)state.Stage;
            StageRequirements stageReq = plantData.GetStageRequirements(currentStage);
            if (stageReq == null)
            {
                sb.AppendLine("<b>Crescita: Requisiti stadio non disponibili</b>");
                return sb.ToString();
            }

            const string ColorStatic = "#B8B8B8";
            const string ColorOk = "#7FFF7A";
            const string ColorOkBright = "#00FF00";
            const string ColorWarn = "#FFA500";
            const string ColorBad = "#FF0000";
            const string ColorSectionParent = "#4FC3E8";
            const string ColorSectionChild = "#87CEEB";

            int maxHydration = _potSystemConfig != null ? _potSystemConfig.MaxHydration : 10;
            int hydrationPercent = PlantCardCalculators.CalculateHydrationPercent(state.Hydration, maxHydration);

            bool waterOk = stageReq.IsHydrationInRange(hydrationPercent);

            // Light Stress: range ideale 20%-80% (0-20% non beneficia, 80-100% rischio burn)
            const int LightStressOkMin = 20;
            const int LightStressOkMax = 70;
            int consecutiveDays = state.GetConsecutiveLedDays();
            int maxDaysForFullStress = _potSystemConfig != null ? _potSystemConfig.MaxDaysForFullStress : 5;
            float stressPercentage = Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f;
            int lightStressPercent = Mathf.RoundToInt(stressPercentage);
            bool lightOk = lightStressPercent >= LightStressOkMin && lightStressPercent <= LightStressOkMax;

            bool fertilizerOk = stageReq.IsFertilizerInRange(state.FertilizerLevel);

            string conditionName = ConditionNameForUi(MapScoreToConditionForUi(state.ConditionScore));
            PlantCondition currentCondition = (PlantCondition)state.ConditionLabel;
            PlantCondition conditionUi = MapScoreToConditionForUi(state.ConditionScore);
            string conditionValColor = conditionUi == PlantCondition.Rigogliosa ? ColorOkBright : (conditionUi == PlantCondition.Sana ? ColorOk : (state.ConditionScore >= 40 ? ColorWarn : ColorBad));
            sb.AppendLine($"<b><color={ColorStatic}>Condizione della Pianta:</color> <color={conditionValColor}>{conditionName}</color></b>");

            // FASE 1.1: Aggiungi informazioni sui modificatori crescita e produzione
            float growthMultiplier = ConditionGrowthModifier.GetGrowthSpeedMultiplier(currentCondition);
            float productionMultiplier = ConditionGrowthModifier.GetProductionMultiplier(currentCondition);

            if (growthMultiplier != 1.0f || productionMultiplier != 1.0f)
            {
                sb.AppendLine();
                sb.AppendLine($"<b><color={ColorStatic}>Effetti sulla Pianta:</color></b>");

                if (growthMultiplier > 1.0f)
                {
                    float growthBonus = (growthMultiplier - 1.0f) * 100f;
                    sb.AppendLine($"  <color={ColorStatic}>velocità crescita:</color> <color={ColorOk}>+{growthBonus:F0}%</color>");
                }
                else if (growthMultiplier < 1.0f)
                {
                    float growthMalus = (1.0f - growthMultiplier) * 100f;
                    sb.AppendLine($"  <color={ColorStatic}>velocità crescita:</color> <color={ColorBad}>-{growthMalus:F0}%</color>");
                }

                if (productionMultiplier > 1.0f)
                {
                    float productionBonus = (productionMultiplier - 1.0f) * 100f;
                    sb.AppendLine($"  <color={ColorStatic}>produzione frutti:</color> <color={ColorOk}>+{productionBonus:F0}%</color>");
                }
                else if (productionMultiplier < 1.0f)
                {
                    float productionMalus = (1.0f - productionMultiplier) * 100f;
                    sb.AppendLine($"  <color={ColorStatic}>produzione frutti:</color> <color={ColorBad}>-{productionMalus:F0}%</color>");
                }
            }
            
            sb.AppendLine();
            sb.AppendLine($"La pianta cresce quando si trova nel <color={ColorStatic}>range giusto</color> di:");
            sb.AppendLine();

            string StatusColorForValue(int value, int minVal, int maxVal)
            {
                if (value >= minVal && value <= maxVal) return ColorOk;
                int range = Mathf.Max(1, maxVal - minVal);
                int dist = value < minVal ? minVal - value : value - maxVal;
                return dist <= Mathf.Max(1, range / 3) ? ColorWarn : ColorBad;
            }

            // Sezioni: parent = celeste CRT, figli = celestino (stesso schema per tutte)
            bool waterOn = state.WateringSystemOn;
            string waterOnOff = waterOn ? $" <color={ColorSectionChild}>(Impianto: ON)</color>" : $" <color={ColorSectionChild}>(Impianto: OFF)</color>";
            string waterStatus = waterOk ? $"<color={ColorOk}>OK</color>" : $"<color={ColorBad}>NON OK</color>";
            sb.AppendLine($"• <color={ColorSectionParent}>Acqua (Water)</color>: {waterStatus}{waterOnOff}");
            sb.AppendLine($"  <color={ColorSectionChild}>Range ideale: {stageReq.hydrationMin}% - {stageReq.hydrationMax}%</color>");
            string waterValColor = waterOk ? ColorOk : StatusColorForValue(hydrationPercent, stageReq.hydrationMin, stageReq.hydrationMax);
            sb.AppendLine($"  <color={ColorSectionChild}>Attuale:</color> <color={waterValColor}>{hydrationPercent}%</color>");
            sb.AppendLine();

            var ledState = state.LedSystemState;
            bool ledCompatOk = ledState == LedSystemState.Off || (plantData != null && LedCompatibilityHelper.IsLedCompatible(ledState, LedCompatibilityHelper.GetCompatibleLedTypes(plantData.Family)));
            string ledLineVal = ledState == LedSystemState.Off
                ? $"<color={ColorSectionChild}>OFF (nessun controllo compatibilità quando spento)</color>"
                : (ledCompatOk ? $"<color={ColorOk}>OK (compatibile con famiglia)</color>" : $"<color={ColorBad}>NON OK - incompatibile con famiglia</color>");
            sb.AppendLine($"• <color={ColorSectionParent}>LUCE</color>: {ledLineVal}");
            string luceAccesaVal = ledState == LedSystemState.Off ? $"<color={ColorBad}>Nessuna</color>" : (ledState == LedSystemState.Blue ? $"<color={ColorOk}>Blu</color>" : $"<color={ColorOk}>Rosso</color>");
            sb.AppendLine($"  <color={ColorSectionChild}>LUCE ACCESA:</color> {luceAccesaVal}");
            string stressStatus = lightOk ? $"<color={ColorOk}>OK</color>" : $"<color={ColorBad}>NON OK</color>";
            sb.AppendLine($"  <color={ColorSectionChild}>Stress da luce:</color> {stressStatus}");
            sb.AppendLine($"  <color={ColorSectionChild}>Range ideale: 20% - 80% (sotto 20% la pianta non beneficia; sopra 80% rischio burn)</color>");
            string lightValColor = lightOk ? ColorOk : (lightStressPercent < LightStressOkMin || lightStressPercent > LightStressOkMax ? ColorBad : ColorWarn);
            sb.AppendLine($"  <color={ColorSectionChild}>Attuale:</color> <color={lightValColor}>{lightStressPercent}%</color>");
            sb.AppendLine();

            bool isFertilizerOptional = FertilizerCarePolicy.ShouldTreatFertilizerAsOptional(currentStage, stageReq);
            string fertilizerStatus = fertilizerOk ? $"<color={ColorOk}>OK</color>" : $"<color={ColorBad}>NON OK</color>";
            string fertilizerLabel = isFertilizerOptional
                ? $"• <color={ColorSectionParent}>Fertilizzante</color> (non richiesto in questa fase): {fertilizerStatus}"
                : $"• <color={ColorSectionParent}>Fertilizzante</color>: {fertilizerStatus}";
            sb.AppendLine(fertilizerLabel);
            sb.AppendLine($"  <color={ColorSectionChild}>Range ideale: {stageReq.fertilizerMin}% - {stageReq.fertilizerMax}%</color>");
            string fertValColor = fertilizerOk ? ColorOk : StatusColorForValue(state.FertilizerLevel, stageReq.fertilizerMin, stageReq.fertilizerMax);
            sb.AppendLine($"  <color={ColorSectionChild}>Attuale:</color> <color={fertValColor}>{state.FertilizerLevel}%</color>");
            if (isFertilizerOptional)
            {
                sb.AppendLine("  <color=#FFFF00>Nota: in questa fase il dato non impone una banda fertilizzante (min/max = 0).</color>");
            }
            sb.AppendLine();

            // Giorni mancanti per avanzare (stessa logica PlantCardV2 tooltip)
            int daysInStage = state.DaysInCurrentStage;
            int requiredDays = stageReq.durationDays;
            int daysRemaining = Mathf.Max(0, requiredDays - daysInStage);

            if (daysRemaining > 0)
            {
                sb.AppendLine($"<color={ColorStatic}>Giorni mancanti per avanzare:</color> <color={ColorStatic}>{daysRemaining}</color>");
                sb.AppendLine($"  <color={ColorStatic}>(Giorni nello stadio: {daysInStage} / {requiredDays})</color>");
            }
            else
            {
                sb.AppendLine($"<color={ColorOk}>✓ Giorni minimi raggiunti!</color>");
                if (waterOk && lightOk && fertilizerOk)
                {
                    sb.AppendLine($"<color={ColorOk}>✓ Tutti i parametri sono nel range ideale!</color>");
                    sb.AppendLine($"<color={ColorStatic}>La pianta può avanzare al prossimo stadio.</color>");
                }
                else
                {
                    sb.AppendLine($"<color={ColorWarn}>⚠️ Metti tutti i parametri nel range ideale per avanzare.</color>");
                }
            }

            return sb.ToString();
        }

        /// <summary>Riga per comando STATUS: indica se il LED acceso è OK o NON OK rispetto alla famiglia (BLK-02.08).</summary>
        private string GetLedStatusLineForPot(PotStateModel state, PlantData plantData)
        {
            if (plantData == null)
                return "§DATA§LED:§END§ dati pianta non disponibili.";
            if (state.LedSystemState == LedSystemState.Off)
                return "§DATA§LED:§END§ OFF (nessun controllo compatibilità quando spento).";
            var compatible = LedCompatibilityHelper.GetCompatibleLedTypes(plantData.Family);
            bool isOk = LedCompatibilityHelper.IsLedCompatible(state.LedSystemState, compatible);
            string richiesto = LedCompatibilityHelper.GetCompatibleLedDisplay(compatible);
            if (richiesto == "Blue") richiesto = "Blu";
            else if (richiesto == "Red") richiesto = "Rosso";
            else if (richiesto == "ALL") richiesto = "Entrambi";
            if (isOk)
                return "§DATA§LED:§END§ §TITLE§OK§END§ (compatibile con famiglia).";
            return $"§DATA§LED:§END§ §WARN§NON OK§END§ - incompatibile con famiglia (§TITLE§{richiesto}§END§ richiesto).";
        }

        /// <summary>Genera consigli in stile black humor Sporium in base ai valori attuali del vaso.</summary>
        private List<string> BuildConsiglioForPot(PotStateModel state, PlantData plantData)
        {
            var lines = new List<string>();
            if (_potSystemConfig == null || state == null || state.IsEmpty || !state.HasPlant || plantData == null)
            {
                lines.Add("§WARN§Dati insufficienti. Consiglio: pianta qualcosa, o non piantare. La scelta è tua.§END§");
                return lines;
            }

            PlantStage currentStage = (PlantStage)state.Stage;
            StageRequirements stageReq = plantData.GetStageRequirements(currentStage);
            if (stageReq == null)
            {
                lines.Add("§WARN§Requisiti stadio non disponibili. Il sistema non sa cosa consigliarti. Né noi.§END§");
                return lines;
            }

            const int LightStressOkMin = 20;
            const int LightStressOkMax = 80;
            int maxHydration = _potSystemConfig.MaxHydration;
            int hydrationPercent = PlantCardCalculators.CalculateHydrationPercent(state.Hydration, maxHydration);
            int consecutiveDays = state.GetConsecutiveLedDays();
            int maxDaysForFullStress = Mathf.Max(1, _potSystemConfig.MaxDaysForFullStress);
            int lightStressPercent = Mathf.RoundToInt(Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f);

            bool waterOk = stageReq.IsHydrationInRange(hydrationPercent);
            bool lightOk = lightStressPercent >= LightStressOkMin && lightStressPercent <= LightStressOkMax;
            bool fertilizerOk = stageReq.IsFertilizerInRange(state.FertilizerLevel);
            bool waterOn = state.WateringSystemOn;
            bool ledOn = state.LedSystemState != LedSystemState.Off;

            int requiredDays = stageReq.durationDays;
            int daysRemaining = Mathf.Max(0, requiredDays - state.DaysInCurrentStage);
            bool canAdvanceByDays = daysRemaining <= 0;
            bool allParamsOk = waterOk && lightOk && fertilizerOk;

            if (state.ConditionScore < 40)
                lines.Add("§WARN§Condizione critica. Il Dome non è un pronto soccorso: accendi acqua e/o LED se sono spenti, altrimenti aspetta. Se la pianta non reagisce, almeno avrai provato.§END§");

            if (!waterOk && !waterOn)
                lines.Add("§WARN§Idratazione fuori range e impianto a goccia spento. Accendilo (comando §CMD§WATERING [POT-ID]§END§). Altrimenti la pianta si disidrata in silenzio. Come tutti noi.§END§");
            else if (!waterOk && waterOn)
                lines.Add("§INFO§Acqua fuori range ma impianto già acceso. Aspetta un paio di giorni: le gocce fanno il loro lavoro. Quando vogliono loro.§END§");

            // Luce / LED: consigli in base a LED e stress 20%-80%. Accendi solo se !ledOn && !lightOk; se suggerisci spegni, mostra comando.
            if (!ledOn && !lightOk)
            {
                var compat = LedCompatibilityHelper.GetCompatibleLedTypes(plantData.Family);
                string cmdLed = compat == LedCompatibility.BlueOnly ? "§CMD§LED BLUE " + state.PotId + "§END§" : (compat == LedCompatibility.RedOnly ? "§CMD§LED RED " + state.PotId + "§END§" : "§CMD§LED BLUE " + state.PotId + "§END§ o §CMD§LED RED " + state.PotId + "§END§");
                lines.Add($"§WARN§Luce spenta e stress fuori range. Senza LED in range la pianta non passerà al prossimo stadio. Accendilo ({cmdLed} a seconda della famiglia).§END§");
            }
            else if (ledOn && !lightOk)
            {
                if (lightStressPercent < LightStressOkMin)
                    lines.Add("§WARN§Stress da luce sotto il 20%: la pianta non beneficia ancora. Tieni il LED acceso ancora qualche giorno per entrare in range (20%-80%).§END§");
                else if (lightStressPercent > LightStressOkMax)
                    lines.Add($"§WARN§Stress da luce sopra l'80%: rischio burn. Spegni il LED — comando §CMD§LED OFF {state.PotId}§END§ — o aspetta senza tenerlo acceso troppo a lungo.§END§");
            }

            if (!FertilizerCarePolicy.ShouldTreatFertilizerAsOptional(currentStage, stageReq) && !stageReq.IsFertilizerInRange(state.FertilizerLevel))
                lines.Add("§WARN§Fertilizzante fuori range. §CMD§FERTILIZE [POT-ID]§END§ se vuoi dare una mano. La pianta non ti ringrazierà, ma potrebbe crescere.§END§");

            if (allParamsOk && canAdvanceByDays)
                lines.Add("§TITLE§Tutto in range e giorni sufficienti. La pianta potrebbe avanzare di stadio. Il miracolo della scienza. O della negligenza controllata.§END§");
            else if (allParamsOk && !canAdvanceByDays)
                lines.Add("§INFO§Parametri a posto. Manca solo il tempo: attendi i giorni richiesti nello stadio. Nel frattempo, la pianta aspetta. Sempre.§END§");

            if (lines.Count == 0)
                lines.Add("§INFO§Monitora i valori. Nel Dome nessuno sente le piante urlare. Figurati.§END§");

            return lines;
        }

        private void FlushConsole()
        {
            if (_consoleText == null) return;

            if (_typewriterQueue.Count > 0)
            {
                EnsureTypewriterRunning();
                return;
            }

            _consoleText.text = _consoleBuffer.ToString();

            AutoScrollConsole();
        }

        private void FlushConsoleImmediate()
        {
            if (_consoleText == null) return;
            _consoleText.text = _consoleBuffer.ToString();
            AutoScrollConsole();
        }

        private void EnsureTypewriterRunning()
        {
            if (_typewriterRoutine != null) return;
            if (_typewriterQueue.Count == 0) return;
            _typewriterRoutine = StartCoroutine(TypewriterRoutine());
        }

        private void StopTypewriter()
        {
            if (_typewriterRoutine != null)
            {
                StopCoroutine(_typewriterRoutine);
                _typewriterRoutine = null;
            }
            _typewriterQueue.Clear();
            _typewriterActive = false;
        }

        /// <summary>Scrive subito in console tutto ciò che è ancora in coda al typewriter, così il prossimo comando mostra la risposta subito.</summary>
        private void FlushTypewriterQueueImmediate()
        {
            if (_typewriterQueue.Count == 0 && _typewriterRoutine == null) return;
            if (_typewriterRoutine != null)
            {
                StopCoroutine(_typewriterRoutine);
                _typewriterRoutine = null;
            }
            while (_typewriterQueue.Count > 0)
            {
                string line = _typewriterQueue.Dequeue();
                if (line != null)
                    _consoleBuffer.AppendLine(line);
            }
            _typewriterActive = false;
            FlushConsoleImmediate();
        }

        /// <summary>Scrive le domande del PLANT flow a blocchi (senza typewriter), per leggere subito domanda e risposta.</summary>
        private void WritePlantFlowBlock(System.Action appendLines)
        {
            FlushTypewriterQueueImmediate();
            _typewriterActive = false;
            appendLines?.Invoke();
            FlushConsole();
        }

        private void EnsureLoadingIndicator()
        {
            if (_loadingIndicator != null && _loadingIndicator.ClassListContains("pcv3-loading-visible"))
                return;
            if (_loadingIndicator != null)
                _loadingIndicator.RemoveFromClassList("pcv3-loading-visible");
        }

        private float _loadingSpinnerAngle;

        private void ShowLoadingSpinner(bool show)
        {
            if (_loadingIndicator == null || _loadingSpinnerLabel == null) return;

            if (_loadingSpinnerSchedule != null)
            {
                _loadingSpinnerSchedule.Pause();
                _loadingSpinnerSchedule = null;
            }

            // Rotellina disabilitata: non mostrare mai lo spinner, solo nascondere se era visibile
            if (show)
                return;
            _loadingIndicator.RemoveFromClassList("pcv3-loading-visible");
        }

        private void UpdateLoadingSpinnerRotation()
        {
            if (_loadingSpinnerLabel == null) return;
            _loadingSpinnerAngle += 45f;
            if (_loadingSpinnerAngle >= 360f) _loadingSpinnerAngle = 0f;
            _loadingSpinnerLabel.style.rotate = new Rotate(_loadingSpinnerAngle);
        }

        /// <summary>Stile CRT/Sporium: messaggi di sistema durante il "caricamento" del comando. Avvia anche il lampeggio (pulsante).</summary>
        private void AppendLoadingLines(string context)
        {
            string ctx = string.IsNullOrEmpty(context) ? "modulo" : context;
            _loadingLine1Plain = "[SYS] Richiesta registrata nel sistema.";
            _loadingLine2Plain = $"[SYS] Loading framework per {ctx}...";

            _loadingBufferLengthBeforeBlink = _consoleBuffer.Length;
            AppendRawLine("§INFO§" + _loadingLine1Plain + "§END§");
            AppendRawLine("§INFO§" + _loadingLine2Plain + "§END§");

            StartLoadingBlink();
        }

        private void StartLoadingBlink()
        {
            StopLoadingBlink();
            _loadingBlinkActive = true;
            _loadingBlinkBright = false;
            if (_root != null)
                _loadingBlinkSchedule = _root.schedule.Execute(DoLoadingBlinkTick).Every(350).Until(() => !_loadingBlinkActive);
        }

        private void StopLoadingBlink()
        {
            _loadingBlinkActive = false;
            if (_loadingBlinkSchedule != null)
            {
                _loadingBlinkSchedule.Pause();
                _loadingBlinkSchedule = null;
            }
        }

        private void DoLoadingBlinkTick()
        {
            if (!_loadingBlinkActive || _consoleBuffer == null || _loadingLine1Plain == null || _loadingLine2Plain == null)
                return;
            if (_loadingBufferLengthBeforeBlink < 0 || _loadingBufferLengthBeforeBlink > _consoleBuffer.Length)
                return;
            string tag = _loadingBlinkBright ? "§TITLE§" : "§INFO§";
            _consoleBuffer.Length = _loadingBufferLengthBeforeBlink;
            AppendRawLine(tag + _loadingLine1Plain + "§END§");
            AppendRawLine(tag + _loadingLine2Plain + "§END§");
            FlushConsole();
            _loadingBlinkBright = !_loadingBlinkBright;
        }

        /// <summary>Ritorna (delay in secondi, contesto per messaggio loading). Errore = delay breve.</summary>
        private (float delaySeconds, string context) GetLoadingDelayAndContext(string trimmed, string upper)
        {
            string context = "";
            bool isErrorPath = false;

            if (upper == "START" || upper == "HELP") { context = "HELP"; }
            else if (upper == "STATUS") { context = "STATUS"; }
            else if (upper == "PASSIVE") { context = "PASSIVE"; }
            else if (upper.StartsWith("CRYO SEND"))
            {
                string potId = ExtractArgAtIndex(trimmed, 2);
                if (string.IsNullOrEmpty(potId)) { isErrorPath = true; context = ""; }
                else { context = "Cryo Transfer"; }
            }
            else if (upper.StartsWith("CRYO EXTRACT"))
            {
                context = "Cryo Estrazione";
            }
            else if (upper.StartsWith("CRYO RESTORE"))
            {
                context = "Cryo Ripristino";
            }
            else if (upper == "FORECAST") { context = "FORECAST"; }
            else if (upper == "PROTOCOL") { context = "PROTOCOL"; }
            else if (upper.StartsWith("OPEN"))
            {
                string potId = ExtractPotIdArgument(trimmed);
                if (string.IsNullOrEmpty(potId)) { isErrorPath = true; context = ""; }
                else { context = "PlantPot"; }
            }
            else if (upper.StartsWith("NOTE"))
            {
                string potId = ExtractPotIdArgument(trimmed);
                if (string.IsNullOrEmpty(potId)) { isErrorPath = true; context = ""; }
                else { context = "PlantPot"; }
            }
            else if (upper.StartsWith("UPROOT") || upper.StartsWith("PLANT") || upper.StartsWith("FERTILIZE") || upper.StartsWith("SPRAY")
                     || upper.StartsWith("WATERING") || upper.StartsWith("LED RED") || upper.StartsWith("LED BLUE") || upper.StartsWith("HARVEST") || upper.StartsWith("PRUNE"))
            {
                string potId = ExtractPotIdArgument(trimmed.Replace("LED RED", "LED").Replace("LED BLUE", "LED"));
                if (string.IsNullOrEmpty(potId)) { isErrorPath = true; context = ""; }
                else { context = "PlantPot"; }
            }
            else if (upper == "CLOSE" || upper == "CLEAR" || upper == "EXIT")
            {
                context = upper == "CLOSE" ? "VIEW" : "";
            }
            else if (upper.StartsWith("QUEUE"))
            {
                var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1 || (parts.Length >= 2 && string.Equals(parts[1], "SHOW", StringComparison.OrdinalIgnoreCase)))
                { context = "QUEUE"; }
                else
                { isErrorPath = true; context = ""; }
            }
            else
            {
                isErrorPath = true;
            }

            float delay = isErrorPath ? _loadingDelayError : _loadingDelaySuccess;
            return (delay, context);
        }

        private IEnumerator LoadingThenExecute(string trimmed, string upper, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            StopLoadingBlink();
            ShowLoadingSpinner(false);
            if (_loadingCoroutine != null) { _loadingCoroutine = null; }
            RunWithFastTypewriter(() => ExecuteCommandBody(trimmed, upper));
        }

        /// <summary>Delay + spinner poi esegue l'azione (per step successivi: selezione, conferma, ecc.).</summary>
        private IEnumerator LoadingThenExecuteStep(float delaySeconds, string loadingContext, System.Action onComplete)
        {
            yield return new WaitForSeconds(delaySeconds);
            StopLoadingBlink();
            ShowLoadingSpinner(false);
            if (_loadingCoroutine != null) { _loadingCoroutine = null; }
            RunWithFastTypewriter(onComplete);
        }

        private void AutoScrollConsole()
        {
            if (_consoleScroll == null) return;

            void ScrollToBottom(string tag)
            {
                var vs = _consoleScroll.verticalScroller;
                if (vs != null && (vs.highValue - vs.lowValue) != 0f)
                {
                    vs.value = vs.highValue;
                    _consoleScroll.scrollOffset = new Vector2(_consoleScroll.scrollOffset.x, vs.highValue);
                }
            }

            _consoleScroll.schedule.Execute(() => ScrollToBottom("t0")).ExecuteLater(0);
            _consoleScroll.schedule.Execute(() => ScrollToBottom("t20")).ExecuteLater(20);
            _consoleScroll.schedule.Execute(() => ScrollToBottom("t60")).ExecuteLater(60);
        }

        private void OnInputKeyDown(KeyDownEvent evt)
        {
            if (!_isVisible) return;

            // Shortcut keys
            if (evt.character == '+')
            {
                // PROTOCOL shortcut
                HandleCommand("PROTOCOL");
                evt.StopPropagation();
                return;
            }

            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
                return;

            var cmd = _input != null ? _input.value : string.Empty;
            if (_input != null) _input.value = string.Empty;

            HandleCommand(cmd);
            // Keep the prompt always ready after executing a command.
            RequestRefocusSoon();
            evt.StopPropagation();
        }

        private void HandleCommand(string input)
        {
            string trimmed = (input ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed))
                return;

            string upper = trimmed.ToUpperInvariant();

            // Any command execution (typed or triggered by buttons) should leave the prompt type-ready.
            if (_isVisible) RequestRefocusSoon();

            // State-gated input (stesso feedback loading degli altri comandi)
            if (_inputState == InputState.SelectingItem)
            {
                AppendRawLine($"> {trimmed}");
                FlushConsole();
                AppendLoadingLines("Selezione");
                FlushConsole();
                ShowLoadingSpinner(true);
                float delay = (_pendingConfirmAction != null && _pendingConfirmAction.Type == QueuedActionType.Plant) ? _loadingDelayPlantFlowStep : _loadingDelayStep;
                _loadingCoroutine = StartCoroutine(LoadingThenExecuteStep(delay, "Selezione", () => HandleSelectingItem(upper)));
                return;
            }
            if (_inputState == InputState.ConfirmingPlantDrip)
            {
                AppendRawLine($"> {trimmed}");
                FlushConsole();
                AppendLoadingLines("Idratazione");
                FlushConsole();
                ShowLoadingSpinner(true);
                _loadingCoroutine = StartCoroutine(LoadingThenExecuteStep(_loadingDelayPlantFlowStep, "Idratazione", () => HandlePlantDripChoice(upper)));
                return;
            }
            if (_inputState == InputState.ConfirmingPlantLed)
            {
                AppendRawLine($"> {trimmed}");
                FlushConsole();
                AppendLoadingLines("LED");
                FlushConsole();
                ShowLoadingSpinner(true);
                _loadingCoroutine = StartCoroutine(LoadingThenExecuteStep(_loadingDelayPlantFlowStep, "LED", () => HandlePlantLedChoice(upper)));
                return;
            }
            if (_inputState == InputState.ConfirmingPlantLedType)
            {
                AppendRawLine($"> {trimmed}");
                FlushConsole();
                AppendLoadingLines("Tipo LED");
                FlushConsole();
                ShowLoadingSpinner(true);
                _loadingCoroutine = StartCoroutine(LoadingThenExecuteStep(_loadingDelayPlantFlowStep, "Tipo LED", () => HandlePlantLedTypeChoice(upper)));
                return;
            }
            if (_inputState == InputState.ConfirmingActionToQueue)
            {
                AppendRawLine($"> {trimmed}");
                FlushConsole();
                AppendLoadingLines("Conferma");
                FlushConsole();
                ShowLoadingSpinner(true);
                float delay = (_pendingConfirmAction != null && _pendingConfirmAction.Type == QueuedActionType.Plant) ? _loadingDelayPlantFlowStep : _loadingDelayStep;
                _loadingCoroutine = StartCoroutine(LoadingThenExecuteStep(delay, "Conferma", () => HandleConfirmToQueue(upper)));
                return;
            }
            if (_inputState == InputState.ConfirmingCriticalFertilize)
            {
                AppendRawLine($"> {trimmed}");
                FlushConsole();
                AppendLoadingLines("Verifica critica");
                FlushConsole();
                ShowLoadingSpinner(true);
                _loadingCoroutine = StartCoroutine(LoadingThenExecuteStep(_loadingDelayStep, "Verifica critica", () => HandleConfirmCriticalFertilize(upper)));
                return;
            }
            if (_inputState == InputState.ConfirmingExecuteOrDiscardQueue)
            {
                AppendRawLine($"> {trimmed}");
                FlushConsole();
                AppendLoadingLines("Esecuzione coda");
                FlushConsole();
                ShowLoadingSpinner(true);
                _loadingCoroutine = StartCoroutine(LoadingThenExecuteStep(_loadingDelayStep, "Esecuzione coda", () => HandleConfirmExecuteOrDiscard(upper)));
                return;
            }
            if (_inputState == InputState.SelectingStatusPot)
            {
                AppendRawLine($"> {trimmed}");
                FlushConsole();
                AppendLoadingLines("STATUS");
                FlushConsole();
                ShowLoadingSpinner(true);
                _loadingCoroutine = StartCoroutine(LoadingThenExecuteStep(_loadingDelayStep, "STATUS", () => HandleStatusPotChoice(upper)));
                return;
            }

            if (_inputState == InputState.SelectingCryoSlotForExtract)
            {
                AppendRawLine($"> {trimmed}");
                FlushConsole();
                AppendLoadingLines("Cryo Estrazione");
                FlushConsole();
                ShowLoadingSpinner(true);
                _loadingCoroutine = StartCoroutine(LoadingThenExecuteStep(_loadingDelayStep, "Cryo Estrazione", () => HandleCryoExtractSlotChoice(upper)));
                return;
            }

            if (_inputState == InputState.SelectingCryoSlotForRestore)
            {
                AppendRawLine($"> {trimmed}");
                FlushConsole();
                AppendLoadingLines("Cryo Ripristino");
                FlushConsole();
                ShowLoadingSpinner(true);
                _loadingCoroutine = StartCoroutine(LoadingThenExecuteStep(_loadingDelayStep, "Cryo Ripristino", () => HandleCryoRestoreSlotChoice(upper)));
                return;
            }

            if (_inputState == InputState.SelectingTargetPotForRestore)
            {
                AppendRawLine($"> {trimmed}");
                FlushConsole();
                AppendLoadingLines("Cryo Ripristino");
                FlushConsole();
                ShowLoadingSpinner(true);
                _loadingCoroutine = StartCoroutine(LoadingThenExecuteStep(_loadingDelayStep, "Cryo Ripristino", () => HandleCryoRestorePotChoice(upper)));
                return;
            }

            if (_inputState == InputState.ConfirmingCryoSend)
            {
                AppendRawLine($"> {trimmed}");
                FlushConsole();
                AppendLoadingLines("Cryo Invio");
                FlushConsole();
                ShowLoadingSpinner(true);
                _loadingCoroutine = StartCoroutine(LoadingThenExecuteStep(_loadingDelayStep, "Cryo Invio", () => HandleCryoSendConfirm(upper)));
                return;
            }

            // Clear any forecast hover hotspots when running a new command.
            ClearForecastConditionHotspots();

            // Evita comandi sovrapposti: se un comando è ancora in fase di loading, annullalo e esegui solo il nuovo.
            if (_loadingCoroutine != null)
            {
                StopCoroutine(_loadingCoroutine);
                _loadingCoroutine = null;
            }

            // Scrivi subito in console l'eventuale output ancora in coda (es. FORECAST), così la risposta al nuovo comando appare subito.
            FlushTypewriterQueueImmediate();

            AppendRawLine($"> {trimmed}");
            FlushConsole();

            var (delaySeconds, loadingContext) = GetLoadingDelayAndContext(trimmed, upper);
            AppendLoadingLines(loadingContext);
            FlushConsole();
            ShowLoadingSpinner(true);
            _loadingCoroutine = StartCoroutine(LoadingThenExecute(trimmed, upper, delaySeconds));
        }

        /// <summary>Esegue il comando dopo la fase di loading (messaggi CRT + spinner).</summary>
        private void ExecuteCommandBody(string trimmed, string upper)
        {
            if (upper == "START" || upper == "HELP")
            {
                PrintStartCommands();
                FlushConsole();
                SwitchToConsole();
                return;
            }

            if (upper == "STATUS")
            {
                FlushTypewriterQueueImmediate();
                _typewriterActive = false;
                var pots = FindPots();
                _potsForStatusChoice.Clear();
                _potsForStatusChoice.AddRange(pots);
                if (pots == null || pots.Count == 0)
                {
                    AppendRawLine("§WARN§Nessun vaso trovato in scena.§END§");
                    AppendRawLine("");
                    FlushConsole();
                    SwitchToConsole();
                    return;
                }
                AppendRawLine("§DATA§▸ QUALE VASO?§END§");
                AppendRawLine("§INFO§Digita il numero del vaso (tasto 1–4) per vedere riepilogo e approfondimento.§END§");
                AppendRawLine("");
                for (int i = 0; i < pots.Count; i++)
                {
                    var p = pots[i];
                    string potId = p != null ? p.PotId : "POT-???";
                    AppendRawLine($"  §CMD§[{i + 1}]§END§ {potId}");
                }
                AppendRawLine("");
                FlushConsole();
                SwitchToConsole();
                _inputState = InputState.SelectingStatusPot;
                return;
            }

            if (upper == "FORECAST")
            {
                AppendRawLine("§INFO§FORECAST è stato integrato in STATUS. Usa §CMD§STATUS§END§ per stato, progressione e requisiti per ogni vaso.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            if (upper == "PROTOCOL")
            {
                ShowProtocolFromDocs();
                return;
            }

            if (upper.StartsWith("OPEN"))
            {
                string potId = ExtractPotIdArgument(trimmed);
                if (string.IsNullOrEmpty(potId))
                {
                    AppendRawLine("§ERROR§⚠ ERRORE: ID VASO RICHIESTO. USO: OPEN [POT-ID]§END§");
                    AppendRawLine("");
                    FlushConsole();
                    return;
                }
                OpenDetail(potId, diaryOnly: false);
                return;
            }

            if (upper.StartsWith("NOTE"))
            {
                string potId = ExtractPotIdArgument(trimmed);
                if (string.IsNullOrEmpty(potId))
                {
                    AppendRawLine("§ERROR§⚠ ERRORE: ID VASO RICHIESTO. USO: NOTE [POT-ID]§END§");
                    AppendRawLine("");
                    FlushConsole();
                    return;
                }
                OpenDetail(potId, diaryOnly: true);
                return;
            }

            if (upper.StartsWith("UPROOT"))
            {
                string potId = ExtractPotIdArgument(trimmed);
                if (string.IsNullOrEmpty(potId))
                {
                    AppendRawLine("§ERROR§⚠ ERRORE: ID VASO RICHIESTO. USO: UPROOT [POT-ID]§END§");
                    AppendRawLine("");
                    FlushConsole();
                    return;
                }
                BeginConfirmQueueUproot(potId);
                return;
            }

            if (upper.StartsWith("PLANT"))
            {
                string potId = ExtractPotIdArgument(trimmed);
                if (string.IsNullOrEmpty(potId))
                {
                    AppendRawLine("§ERROR§⚠ ERRORE: ID VASO RICHIESTO. USO: PLANT [POT-ID]§END§");
                    AppendRawLine("");
                    FlushConsole();
                    return;
                }
                BeginSelectItemForAction(QueuedActionType.Plant, potId);
                return;
            }

            if (upper.StartsWith("FERTILIZE"))
            {
                string potId = ExtractPotIdArgument(trimmed);
                if (string.IsNullOrEmpty(potId))
                {
                    AppendRawLine("§ERROR§⚠ ERRORE: ID VASO RICHIESTO. USO: FERTILIZE [POT-ID]§END§");
                    AppendRawLine("");
                    FlushConsole();
                    return;
                }
                BeginSelectItemForAction(QueuedActionType.Fertilize, potId);
                return;
            }

            if (upper.StartsWith("SPRAY"))
            {
                string potId = ExtractPotIdArgument(trimmed);
                if (string.IsNullOrEmpty(potId))
                {
                    AppendRawLine("§ERROR§⚠ ERRORE: ID VASO RICHIESTO. USO: SPRAY [POT-ID]§END§");
                    AppendRawLine("");
                    FlushConsole();
                    return;
                }
                BeginSelectItemForAction(QueuedActionType.Spray, potId);
                return;
            }

            if (upper.StartsWith("WATERING"))
            {
                string potId = ExtractPotIdArgument(trimmed);
                if (string.IsNullOrEmpty(potId))
                {
                    AppendRawLine("§ERROR§⚠ ERRORE: ID VASO RICHIESTO. USO: WATERING [POT-ID]§END§");
                    AppendRawLine("");
                    FlushConsole();
                    return;
                }
                BeginConfirmToggleAction(QueuedActionType.HydrationToggle, potId);
                return;
            }

            if (upper.StartsWith("LED RED"))
            {
                string potId = ExtractPotIdArgument(trimmed.Replace("LED RED", "LED"));
                if (string.IsNullOrEmpty(potId))
                {
                    AppendRawLine("§ERROR§⚠ ERRORE: ID VASO RICHIESTO. USO: LED RED [POT-ID]§END§");
                    AppendRawLine("");
                    FlushConsole();
                    return;
                }
                BeginConfirmToggleAction(QueuedActionType.LedRedToggle, potId);
                return;
            }

            if (upper.StartsWith("LED BLUE"))
            {
                string potId = ExtractPotIdArgument(trimmed.Replace("LED BLUE", "LED"));
                if (string.IsNullOrEmpty(potId))
                {
                    AppendRawLine("§ERROR§⚠ ERRORE: ID VASO RICHIESTO. USO: LED BLUE [POT-ID]§END§");
                    AppendRawLine("");
                    FlushConsole();
                    return;
                }
                BeginConfirmToggleAction(QueuedActionType.LedBlueToggle, potId);
                return;
            }

            if (upper.StartsWith("HARVEST"))
            {
                string potId = ExtractPotIdArgument(trimmed);
                if (string.IsNullOrEmpty(potId))
                {
                    AppendRawLine("§ERROR§⚠ ERRORE: ID VASO RICHIESTO. USO: HARVEST [POT-ID]§END§");
                    AppendRawLine("");
                    FlushConsole();
                    return;
                }
                BeginConfirmToggleAction(QueuedActionType.Harvest, potId);
                return;
            }

            if (upper.StartsWith("PRUNE"))
            {
                string potId = ExtractPotIdArgument(trimmed);
                if (string.IsNullOrEmpty(potId))
                {
                    AppendRawLine("§ERROR§⚠ ERRORE: ID VASO RICHIESTO. USO: PRUNE [POT-ID]§END§");
                    AppendRawLine("");
                    FlushConsole();
                    return;
                }
                BeginConfirmToggleAction(QueuedActionType.Prune, potId);
                return;
            }

            if (upper == "CLOSE")
            {
                // Contestuale: se siamo in detail, torna a console; altrimenti warning. PROTOCOL è in console, non ha vista dedicata.
                if (_detailView != null && _detailView.style.display != DisplayStyle.None)
                {
                    AppendRawLine("§INFO§⚠ Chiusura vista dettaglio§END§");
                    AppendRawLine("");
                    SwitchToConsole();
                    FlushConsole();
                    return;
                }
                AppendRawLine("§WARN§⚠ Nulla da chiudere§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            // NOTE: FORECAST is handled earlier (interactive output with hover tooltip).

            if (upper.StartsWith("QUEUE"))
            {
                // Queue is console-driven (sidebar removed). Usage: QUEUE SHOW
                var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1 || (parts.Length >= 2 && string.Equals(parts[1], "SHOW", StringComparison.OrdinalIgnoreCase)))
                {
                    SwitchToConsole();
                    PrintQueue();
                    FlushConsole();
                    return;
                }

                AppendRawLine("§ERROR§⚠ COMANDO CODA NON VALIDO. USO: QUEUE SHOW§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            if (upper == "CLEAR")
            {
                if (_queue.Count == 0)
                {
                    AppendRawLine("§WARN§⚠ La coda azioni è già vuota§END§");
                    AppendRawLine("");
                }
                else
                {
                    _queue.Clear();
                    RebuildReservedItems();
AppendRawLine("§TITLE§✓ Coda azioni svuotata§END§");
            AppendRawLine("§INFO§Tutte le azioni in coda rimosse§END§");
                    AppendRawLine("");
                }
                FlushConsole();
                return;
            }

            if (upper == "EXIT")
            {
                RequestClose();
                return;
            }

            if (upper == "PASSIVE")
            {
                ExecutePassiveOverview();
                return;
            }

            if (upper.StartsWith("CRYO SEND"))
            {
                string potId = ExtractArgAtIndex(trimmed, 2);
                if (string.IsNullOrEmpty(potId))
                {
                    AppendRawLine("§ERROR§⚠ ERRORE: ID VASO RICHIESTO. USO: CRYO SEND [POT-ID]§END§");
                    AppendRawLine("");
                    FlushConsole();
                    return;
                }
                ExecuteCryoSend(potId);
                return;
            }

            if (upper.StartsWith("CRYO EXTRACT"))
            {
                BeginCryoExtractSelection();
                return;
            }

            if (upper.StartsWith("CRYO RESTORE"))
            {
                BeginCryoRestoreSlotSelection();
                return;
            }

            AppendRawLine("§ERROR§⚠ COMANDO NON VALIDO. DIGITA START PER AIUTO§END§");
            AppendRawLine("");
            FlushConsole();
        }

        /// <summary>
        /// Comando PASSIVE: mostra l'overview dei 3 slot della Cryo Machine con stato,
        /// pianta conservata, livello e PassivePowerLabel.
        /// </summary>
        private void ExecutePassiveOverview()
        {
            var cryo = ServiceContainer.Instance?.Get<CryoMachineController>(suppressWarning: true);

            // ── PROTOCOLLO ──────────────────────────────────────────────────────────
            AppendRawLine("§DATA§▸ CRYO MACHINE — PROTOCOLLO DI CONSERVAZIONE PASSIVA§END§");
            AppendRawLine("<color=#00AA00>────────────────────────────────────────────────────────────────────────────</color>");
            AppendRawLine("");
            AppendRawLine("§INFO§La Cryo Machine è un sistema di conservazione avanzato riservato§END§");
            AppendRawLine("§INFO§alle piante che raggiungono il Livello 5 — il massimo evolutivo.§END§");
            AppendRawLine("§INFO§Una pianta Lvl 5 in stato criogenico non richiede manutenzione§END§");
            AppendRawLine("§INFO§quotidiana (irrigazione, concimazione, raccolta) ma rimane§END§");
            AppendRawLine("§INFO§biologicamente attiva e influenza il Vault tramite i suoi POTERI PASSIVI.§END§");
            AppendRawLine("");
            AppendRawLine("§DATA§▸ POTERI ATTIVI vs POTERI PASSIVI§END§");
            AppendRawLine("");
            AppendRawLine("  §CMD§POTERI ATTIVI§END§");
            AppendRawLine("  §DIM§Operativi solo quando la pianta è in un pot attivo (POT-001…004).§END§");
            AppendRawLine("  §DIM§Influenzano produzione, qualità dei raccolti e cicli biologici.§END§");
            AppendRawLine("  §DIM§Si disattivano automaticamente al trasferimento in Cryo.§END§");
            AppendRawLine("");
            AppendRawLine("  §CMD§POTERI PASSIVI§END§");
            AppendRawLine("  §DIM§Operativi solo quando la pianta è in uno slot Cryo.§END§");
            AppendRawLine("  §DIM§Influenzano il Vault con effetti ambientali e bonus permanenti§END§");
            AppendRawLine("  §DIM§(atmosfera, resistenze, moltiplicatori Vault-wide).§END§");
            AppendRawLine("  §DIM§Si disattivano quando la pianta viene rimossa dallo slot Cryo.§END§");
            AppendRawLine("");
            AppendRawLine("§DATA§▸ OPERAZIONI DISPONIBILI§END§");
            AppendRawLine("");
            AppendRawLine("  §CMD§CRYO SEND [POT-ID]§END§");
            AppendRawLine("  §DIM§Trasferisce una pianta Lvl 5 da un pot attivo a uno slot Cryo libero.§END§");
            AppendRawLine("  §DIM§I poteri attivi si disattivano, quelli passivi si attivano.§END§");
            AppendRawLine("");
            AppendRawLine("  §CMD§CRYO EXTRACT§END§");
            AppendRawLine("  §DIM§Avvia la procedura guidata per rimuovere una pianta da un slot Cryo§END§");
            AppendRawLine("  §DIM§e conservarla nell'inventario come WholePlant (tutti i metadata Lvl 5§END§");
            AppendRawLine("  §DIM§preservati). I poteri passivi si disattivano.§END§");
            AppendRawLine("");
            AppendRawLine("  §CMD§CRYO RESTORE§END§");
            AppendRawLine("  §DIM§Avvia la procedura guidata per trasferire una pianta da uno slot Cryo§END§");
            AppendRawLine("  §DIM§a un pot attivo vuoto. I poteri passivi si disattivano, quelli attivi§END§");
            AppendRawLine("  §DIM§vengono ripristinati.§END§");
            AppendRawLine("");
            AppendRawLine("<color=#00AA00>────────────────────────────────────────────────────────────────────────────</color>");
            AppendRawLine("");

            // ── STATO SLOT ───────────────────────────────────────────────────────────
            AppendRawLine("§DATA§▸ STATO SLOT CRYO§END§");
            AppendRawLine("<color=#00AA00>────────────────────────────────────────────────────────────────────────────</color>");

            if (cryo == null)
            {
                AppendRawLine("§WARN§⚠ Cryo Machine non disponibile in questa scena.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            var slots = cryo.GetPassiveSlotsSnapshot();
            if (slots == null || slots.Count == 0)
            {
                AppendRawLine("§WARN§⚠ Nessuno slot cryo configurato.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            foreach (var slot in slots)
            {
                if (slot == null) continue;

                AppendRawLine($"§DATA§{slot.SlotId}§END§");

                if (!slot.IsOccupied)
                {
                    AppendRawLine("  §DIM§[ SLOT VUOTO — disponibile per trasferimento Lvl 5 ]§END§");
                }
                else
                {
                    var p = slot.Payload;
                    string plantName  = !string.IsNullOrWhiteSpace(p.CustomPlantName) ? p.CustomPlantName : p.PlantCode;
                    string family     = !string.IsNullOrWhiteSpace(p.PlantFamilyMetadata) ? p.PlantFamilyMetadata : "—";
                    string passive    = !string.IsNullOrWhiteSpace(p.PassivePowerLabel) ? p.PassivePowerLabel : "—";
                    string hybrid     = p.IsHybrid ? " §WARN§[IBRIDO]§END§" : "";
                    string mutated    = p.IsMutated ? " §PURPLE§[MUTATO]§END§" : "";

                    AppendRawLine($"  §TITLE§[ PASSIVE ACTIVE ]§END§{hybrid}{mutated}");
                    AppendRawLine($"  §CMD§Pianta:§END§   {plantName}  §DIM§({p.PlantCode})§END§");
                    AppendRawLine($"  §CMD§Livello:§END§  §VAL§Lvl {p.PlantLevel}§END§");
                    AppendRawLine($"  §CMD§Famiglia:§END§ {family}");
                    AppendRawLine($"  §CMD§Potere Passivo:§END§");
                    AppendRawLine($"  §INFO§{passive}§END§");
                }

                AppendRawLine("");
            }

            AppendRawLine($"§DIM§Slot occupati: {cryo.OccupiedCount()} / {slots.Count}§END§");
            AppendRawLine("<color=#00AA00>────────────────────────────────────────────────────────────────────────────</color>");
            AppendRawLine("");
            FlushConsole();
            SwitchToConsole();
        }

        /// <summary>CRYO SEND [POT-ID] — mostra il prompt di conferma prima di trasferire.</summary>
        private void ExecuteCryoSend(string potId)
        {
            var pot = FindPotById(potId);
            if (pot == null)
            {
                AppendRawLine($"§ERROR§⚠ ERRORE: Vaso '{potId}' non trovato nel registro.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            var state = pot.PotActions?.PotState;
            if (state == null || !state.HasPlant)
            {
                AppendRawLine($"§WARN§⚠ Il vaso {potId} è vuoto — nessuna pianta da trasferire.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            if (state.PlantLevel < 5)
            {
                AppendRawLine($"§WARN§⚠ Solo le piante Livello 5 possono essere trasferite alla Cryo Machine.§END§");
                AppendRawLine($"§DIM§Livello attuale: {state.PlantLevel}§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            string plantName = !string.IsNullOrWhiteSpace(state.CustomPlantName)
                ? state.CustomPlantName
                : (state.PlantCode ?? potId);

            AppendRawLine("§TITLE§⬡ CONFERMA TRASFERIMENTO CRYO§END§");
            AppendRawLine("");
            AppendRawLine($"§DATA§Pianta:§END§ §WHITE§{plantName}§END§  §DIM§(Lvl 5 — {potId})§END§");
            AppendRawLine("");
            AppendRawLine("§WARN§⚠ ATTENZIONE — Effetti del trasferimento:§END§");
            AppendRawLine("");
            AppendRawLine("  §DIM§• Il vaso {potId} si libera e torna disponibile per una nuova pianta.§END§".Replace("{potId}", potId));
            AppendRawLine("  §DIM§• La pianta NON produce più: niente crescita, niente resa giornaliera.§END§");
            AppendRawLine("  §DIM§• I Poteri Attivi si disattivano immediatamente.§END§");
            AppendRawLine("  §DIM§• I Poteri Passivi si attivano: la pianta influenza il Vault§END§");
            AppendRawLine("  §DIM§  con effetti latenti (pH drift, cap) finché rimane nello slot Cryo.§END§");
            AppendRawLine("  §DIM§• La manutenzione quotidiana (acqua, luce, fertilizzante) non è più necessaria.§END§");
            AppendRawLine("");
            AppendRawLine("§WHITE§Confermi il trasferimento? §CMD§S§END§ §WHITE§= Sì   §N§N§END§ §WHITE§= No§END§");
            AppendRawLine("");
            FlushConsole();
            SwitchToConsole();

            _pendingCryoSendPotId = potId;
            _inputState = InputState.ConfirmingCryoSend;
        }

        /// <summary>Gestisce la risposta S/N alla conferma CRYO SEND.</summary>
        private void HandleCryoSendConfirm(string upper)
        {
            if (upper == "N" || upper == "NO")
            {
                _inputState = InputState.Idle;
                _pendingCryoSendPotId = null;
                AppendRawLine("§DIM§Trasferimento annullato. La pianta rimane nel pot attivo.§END§");
                AppendRawLine("");
                FlushConsole();
                SwitchToConsole();
                return;
            }

            if (upper != "S" && upper != "SI" && upper != "SÌ" && upper != "YES" && upper != "Y")
            {
                AppendRawLine("§WARN§Risposta non riconosciuta. Digita §CMD§S§END§ per confermare o §N§N§END§ per annullare.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            string potId = _pendingCryoSendPotId;
            _inputState = InputState.Idle;
            _pendingCryoSendPotId = null;

            var pot = FindPotById(potId);
            if (pot == null)
            {
                AppendRawLine($"§ERROR§⚠ ERRORE: Vaso '{potId}' non più disponibile.§END§");
                AppendRawLine("");
                FlushConsole();
                SwitchToConsole();
                return;
            }

            bool ok = pot.PotActions.TransferToCryo();
            if (ok)
            {
                AppendRawLine("§TITLE§✓ TRASFERIMENTO COMPLETATO§END§");
                AppendRawLine($"§INFO§La pianta è stata trasferita con successo alla Cryo Machine.§END§");
                AppendRawLine($"§INFO§I poteri passivi sono ora attivi. Il vaso {potId} è disponibile.§END§");
                AppendRawLine($"§DIM§Digita PASSIVE per aggiornare lo stato degli slot.§END§");
                UpdateHudSlotVisuals();
                RefreshHudFromSelectedPot();
            }
            else
            {
                AppendRawLine("§ERROR§⚠ TRASFERIMENTO FALLITO — nessuno slot Cryo libero disponibile§END§");
                AppendRawLine("§DIM§oppure la Cryo Machine non è configurata in scena.§END§");
            }

            AppendRawLine("");
            FlushConsole();
            SwitchToConsole();
        }

        // ── CRYO EXTRACT — procedura guidata ────────────────────────────────────

        /// <summary>Passo 1: mostra la lista degli slot Cryo occupati e avvia la selezione.</summary>
        private void BeginCryoExtractSelection()
        {
            var cryo = ServiceContainer.Instance?.Get<CryoMachineController>(suppressWarning: true);
            if (cryo == null)
            {
                AppendRawLine("§ERROR§⚠ Cryo Machine non disponibile in questa scena.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            var slots = cryo.GetPassiveSlotsSnapshot();
            _cryoSlotsForChoice.Clear();
            foreach (var s in slots)
                if (s != null && s.IsOccupied) _cryoSlotsForChoice.Add(s);

            if (_cryoSlotsForChoice.Count == 0)
            {
                AppendRawLine("§WARN§⚠ Nessuna pianta presente negli slot Cryo — nulla da estrarre.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            AppendRawLine("§DATA§▸ CRYO EXTRACT — Scegli lo slot da estrarre§END§");
            AppendRawLine("<color=#00AA00>────────────────────────────────────────────────────────────────────────────</color>");
            for (int i = 0; i < _cryoSlotsForChoice.Count; i++)
            {
                var s = _cryoSlotsForChoice[i];
                var p = s.Payload;
                string name = !string.IsNullOrWhiteSpace(p.CustomPlantName) ? p.CustomPlantName : p.PlantCode;
                string passive = !string.IsNullOrWhiteSpace(p.PassivePowerLabel) ? p.PassivePowerLabel : "—";
                AppendRawLine($"  §CMD§{i + 1}.§END§ §DATA§{s.SlotId}§END§  {name}  §VAL§Lvl {p.PlantLevel}§END§");
                AppendRawLine($"     §DIM§Potere Passivo: {passive}§END§");
            }
            AppendRawLine("");
            AppendRawLine("§WARN§⚠ NOTA BENE — Deperimento organico§END§");
            AppendRawLine("§DIM§Qualsiasi organismo inserito nell'inventario è soggetto a deperimento§END§");
            AppendRawLine("§DIM§biologico: la pianta perde 1 punto Qualità per ogni giorno trascorso.§END§");
            AppendRawLine("§DIM§Quando la Qualità raggiunge 0, la pianta si decompone in Scarto Organico.§END§");
            AppendRawLine("§DIM§Una pianta Lvl 5 estratta dalla Cryo Machine è soggetta allo stesso processo.§END§");
            AppendRawLine("§DIM§Pianifica con attenzione prima di procedere.§END§");
            AppendRawLine("");
            AppendRawLine("§WHITE§Digita il §CMD§numero§END§ dello slot oppure §N§N§END§ per annullare§END§");
            FlushConsole();
            _inputState = InputState.SelectingCryoSlotForExtract;
        }

        /// <summary>Passo 2: processa la scelta dello slot e esegue l'estrazione.</summary>
        private void HandleCryoExtractSlotChoice(string upper)
        {
            if (upper == "N" || upper == "NO")
            {
                _inputState = InputState.Idle;
                _cryoSlotsForChoice.Clear();
                AppendRawLine("§DIM§Operazione annullata.§END§");
                AppendRawLine("");
                FlushConsole();
                SwitchToConsole();
                return;
            }

            if (!int.TryParse(upper, out int choice) || choice < 1 || choice > _cryoSlotsForChoice.Count)
            {
                AppendRawLine($"§ERROR§⚠ Scelta non valida. Digita un numero da 1 a {_cryoSlotsForChoice.Count} oppure N per annullare.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            var slot = _cryoSlotsForChoice[choice - 1];
            string cryoId = slot.SlotId;
            string plantName = slot.Payload != null
                ? (!string.IsNullOrWhiteSpace(slot.Payload.CustomPlantName) ? slot.Payload.CustomPlantName : slot.Payload.PlantCode)
                : cryoId;

            _inputState = InputState.Idle;
            _cryoSlotsForChoice.Clear();

            var activePot = _hudPots.Count > 0 ? _hudPots[0] : null;
            if (activePot?.PotActions == null)
            {
                AppendRawLine("§ERROR§⚠ Impossibile completare l'operazione: nessun pot attivo disponibile.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            bool ok = activePot.PotActions.ExtractFromCryoToStorage(cryoId);
            if (ok)
            {
                AppendRawLine("§TITLE§✓ ESTRAZIONE COMPLETATA§END§");
                AppendRawLine($"§INFO§{plantName} rimossa dalla Cryo Machine e conservata nell'inventario.§END§");
                AppendRawLine($"§INFO§I poteri passivi sono stati disattivati.§END§");
                AppendRawLine($"§DIM§Ricorda: la pianta deperisce in inventario. Usala o vendila al più presto.§END§");
                UpdateHudSlotVisuals();
                RefreshHudFromSelectedPot();
            }
            else
            {
                AppendRawLine("§ERROR§⚠ ESTRAZIONE FALLITA — verifica log di sistema.§END§");
            }

            AppendRawLine("");
            FlushConsole();
            SwitchToConsole();
        }

        // ── CRYO RESTORE — procedura guidata ────────────────────────────────────

        /// <summary>Passo 1: mostra la lista degli slot Cryo occupati e avvia la selezione.</summary>
        private void BeginCryoRestoreSlotSelection()
        {
            var cryo = ServiceContainer.Instance?.Get<CryoMachineController>(suppressWarning: true);
            if (cryo == null)
            {
                AppendRawLine("§ERROR§⚠ Cryo Machine non disponibile in questa scena.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            var slots = cryo.GetPassiveSlotsSnapshot();
            _cryoSlotsForChoice.Clear();
            foreach (var s in slots)
                if (s != null && s.IsOccupied) _cryoSlotsForChoice.Add(s);

            if (_cryoSlotsForChoice.Count == 0)
            {
                AppendRawLine("§WARN§⚠ Nessuna pianta presente negli slot Cryo — nulla da ripristinare.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            AppendRawLine("§DATA§▸ CRYO RESTORE — Scegli la pianta da ripristinare§END§");
            AppendRawLine("<color=#00AA00>────────────────────────────────────────────────────────────────────────────</color>");
            for (int i = 0; i < _cryoSlotsForChoice.Count; i++)
            {
                var s = _cryoSlotsForChoice[i];
                var p = s.Payload;
                string name = !string.IsNullOrWhiteSpace(p.CustomPlantName) ? p.CustomPlantName : p.PlantCode;
                string passive = !string.IsNullOrWhiteSpace(p.PassivePowerLabel) ? p.PassivePowerLabel : "—";
                string active  = !string.IsNullOrWhiteSpace(p.ActivePowerLabel)  ? p.ActivePowerLabel  : "—";
                AppendRawLine($"  §CMD§{i + 1}.§END§ §DATA§{s.SlotId}§END§  {name}  §VAL§Lvl {p.PlantLevel}§END§");
                AppendRawLine($"     §DIM§Potere Passivo (attuale): {passive}§END§");
                AppendRawLine($"     §DIM§Potere Attivo (verrà ripristinato): {active}§END§");
            }
            AppendRawLine("");
            AppendRawLine("§WARN§⚠ ATTENZIONE — Transizione da Criogenia a Pot Attivo§END§");
            AppendRawLine("§DIM§Il trasferimento in un pot attivo disattiva immediatamente tutti i poteri§END§");
            AppendRawLine("§DIM§passivi della pianta: la sua influenza sul Vault cesserà al completamento§END§");
            AppendRawLine("§DIM§dell'operazione. I poteri attivi torneranno operativi nel ciclo biologico§END§");
            AppendRawLine("§DIM§standard. La pianta richiederà nuovamente manutenzione quotidiana.§END§");
            AppendRawLine("");
            AppendRawLine("§WHITE§Digita il §CMD§numero§END§ della pianta oppure §N§N§END§ per annullare§END§");
            FlushConsole();
            _inputState = InputState.SelectingCryoSlotForRestore;
        }

        /// <summary>Passo 2: salva lo slot scelto e mostra i pot attivi vuoti disponibili.</summary>
        private void HandleCryoRestoreSlotChoice(string upper)
        {
            if (upper == "N" || upper == "NO")
            {
                _inputState = InputState.Idle;
                _cryoSlotsForChoice.Clear();
                AppendRawLine("§DIM§Operazione annullata.§END§");
                AppendRawLine("");
                FlushConsole();
                SwitchToConsole();
                return;
            }

            if (!int.TryParse(upper, out int choice) || choice < 1 || choice > _cryoSlotsForChoice.Count)
            {
                AppendRawLine($"§ERROR§⚠ Scelta non valida. Digita un numero da 1 a {_cryoSlotsForChoice.Count} oppure N per annullare.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            _pendingCryoSlotId = _cryoSlotsForChoice[choice - 1].SlotId;
            _cryoSlotsForChoice.Clear();

            // Mostra i pot attivi vuoti disponibili
            _emptyPotsForChoice.Clear();
            foreach (var pot in _hudPots)
            {
                if (pot?.PotActions?.PotState == null) continue;
                if (!pot.PotActions.PotState.HasPlant)
                    _emptyPotsForChoice.Add(pot);
            }

            if (_emptyPotsForChoice.Count == 0)
            {
                _inputState = InputState.Idle;
                _pendingCryoSlotId = null;
                AppendRawLine("§WARN§⚠ Nessun pot attivo disponibile (tutti occupati).§END§");
                AppendRawLine("§DIM§Rimuovi prima una pianta da un pot attivo, poi riprova.§END§");
                AppendRawLine("");
                FlushConsole();
                SwitchToConsole();
                return;
            }

            AppendRawLine("§DATA§▸ Scegli il pot attivo di destinazione§END§");
            AppendRawLine("<color=#00AA00>────────────────────────────────────────────────────────────────────────────</color>");
            for (int i = 0; i < _emptyPotsForChoice.Count; i++)
            {
                var pot = _emptyPotsForChoice[i];
                AppendRawLine($"  §CMD§{i + 1}.§END§ §DATA§{pot.PotId}§END§  §DIM§[SLOT VUOTO]§END§");
            }
            AppendRawLine("");
            AppendRawLine("§WHITE§Digita il §CMD§numero§END§ del pot oppure §N§N§END§ per annullare§END§");
            FlushConsole();
            _inputState = InputState.SelectingTargetPotForRestore;
        }

        /// <summary>Passo 3: esegue il ripristino nel pot selezionato.</summary>
        private void HandleCryoRestorePotChoice(string upper)
        {
            if (upper == "N" || upper == "NO")
            {
                _inputState = InputState.Idle;
                _emptyPotsForChoice.Clear();
                _pendingCryoSlotId = null;
                AppendRawLine("§DIM§Operazione annullata.§END§");
                AppendRawLine("");
                FlushConsole();
                SwitchToConsole();
                return;
            }

            if (!int.TryParse(upper, out int choice) || choice < 1 || choice > _emptyPotsForChoice.Count)
            {
                AppendRawLine($"§ERROR§⚠ Scelta non valida. Digita un numero da 1 a {_emptyPotsForChoice.Count} oppure N per annullare.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            var pot = _emptyPotsForChoice[choice - 1];
            string potId   = pot.PotId;
            string cryoId  = _pendingCryoSlotId;

            _inputState = InputState.Idle;
            _emptyPotsForChoice.Clear();
            _pendingCryoSlotId = null;

            bool ok = pot.PotActions.RestoreFromCryo(cryoId);
            if (ok)
            {
                AppendRawLine("§TITLE§✓ RIPRISTINO COMPLETATO§END§");
                AppendRawLine($"§INFO§La pianta è stata trasferita da {cryoId} al vaso {potId}.§END§");
                AppendRawLine($"§INFO§Poteri passivi disattivati — poteri attivi ripristinati.§END§");
                AppendRawLine($"§DIM§La pianta è ora nel ciclo attivo e richiede manutenzione quotidiana.§END§");
                UpdateHudSlotVisuals();
                RefreshHudFromSelectedPot();
            }
            else
            {
                AppendRawLine("§ERROR§⚠ RIPRISTINO FALLITO — verifica log di sistema.§END§");
            }

            AppendRawLine("");
            FlushConsole();
            SwitchToConsole();
        }

        private void PrintQueue()
        {
            AppendRawLine("§DATA§▸ CODA AZIONI§END§");

            if (_queue.Count == 0)
            {
                AppendRawLine("§WARN§(vuota)§END§");
                AppendRawLine("");
                return;
            }

            AppendRawLine("#  │ VASO    │ AZIONE         │ OGGETTO              │ AP");
            AppendRawLine("───┼─────────┼───────────────┼─────────────────────┼────");

            for (int i = 0; i < _queue.Count; i++)
            {
                var a = _queue[i];
                if (a == null) continue;

                string idx = (i + 1).ToString().PadLeft(2);
                string pot = (a.PotId ?? "POT-???").PadRight(7).Substring(0, 7);
                string action = GetActionLabel(a.Type).PadRight(13).Substring(0, 13);
                
                string itemDisplayName = string.IsNullOrEmpty(a.TargetLabel)
                    ? (string.IsNullOrEmpty(a.ItemTypeId) ? "-" : GetItemDisplayName(a.ItemTypeId))
                    : a.TargetLabel;
                string item = itemDisplayName.PadRight(19);
                if (item.Length > 19) item = item.Substring(0, 19);
                
                string ap = $"{a.ApCost} AP".PadRight(21);
                if (ap.Length > 21) ap = ap.Substring(0, 21);

                AppendRawLine($"{idx} │ {pot} │ {action} │ {item} │ {ap}");
            }

            int totalAp = 0;
            foreach (var a in _queue) totalAp += a != null ? a.ApCost : 0;

            AppendRawLine($"§INFO§Azioni in coda: {_queue.Count} | AP totali: {totalAp}§END§");
            AppendRawLine("");
        }

        private readonly struct EstimatedDailyPoints
        {
            public readonly int WaterPoint;
            public readonly int LightPoint;
            public readonly int FertilizerPoint;
            public int TotalPoints => WaterPoint + LightPoint + FertilizerPoint;

            public EstimatedDailyPoints(int water, int light, int fertilizer)
            {
                WaterPoint = water;
                LightPoint = light;
                FertilizerPoint = fertilizer;
            }
        }

        private sealed class StageForecast
        {
            public string PotId;
            public string PlantName;
            public PlantStage CurrentStage;
            public PlantStage? NextStage;
            public bool IsFinalOrManual;
            public int MoldRiskLevel;
            public string SoonInConditionName;
            public int ProgressPercent;
            public StageRequirements StageReq;
            public int ConditionScore;
            public ForecastDirection Trend;

            public bool BlocksAdvancement;
            public bool BlockedByCondition;
            public bool BlockedByMold;

            public bool HydrationOk;
            public bool LedOk;
            public bool FertilizerOk;
            public bool DurationOk;
            public bool OptimalDaysOk;
            public bool PointsOk;

            public int HydrationPercent;
            public int EffectiveRequiredDays;
            public int RequiredPoints;
            public int TotalPoints;
            public int RequiredOptimalDays;
            public int DaysInCurrentStage;
            public int DaysConsecutiveOptimal;

            public EstimatedDailyPoints EstimatedDailyPoints;

            public int? EstimatedDaysToAdvance; // null = cannot estimate under current conditions
        }

        private void PrintForecast()
        {
            var pots = FindPots();
            AppendRawLine("§DATA§PREVISIONE LIVE§END§");
            AppendRawLine("§INFO§Previsione stadio crescita e analisi requisiti§END§");
            AppendRawLine("");

            if (pots == null || pots.Count == 0)
            {
                AppendRawLine("§WARN§Nessun vaso trovato in scena.§END§");
                AppendRawLine("");
                return;
            }

            int printed = 0;
            foreach (var pot in pots)
            {
                if (pot == null || pot.PotActions == null) continue;
                var state = pot.PotActions.PotState;
                if (state == null || state.IsEmpty || !state.HasPlant) continue;

                var plantData = state.GetPlantData();
                var row = CalculateStageForecast(state, plantData);
                if (row == null) continue;

                PrintForecastForPot(pot, state, plantData, row);
                printed++;
            }

            if (printed == 0)
            {
                AppendRawLine("§WARN§Nessun vaso piantato da prevedere.§END§");
                AppendRawLine("");
            }
        }

        private void PrintForecastForPot(PotSlot potSlot, PotStateModel pot, PlantData plantData, StageForecast f)
        {
            AppendRawLine("────────────────────────────────────────────────────────────────────────────");
            AppendRawLine($"► §DATA§{f.PotId}§END§ | {f.PlantName} | {pot.PlantCode}");
            AppendRawLine("────────────────────────────────────────────────────────────────────────────");
            AppendRawLine("");

            // CURRENT STATUS
            string stageLabel = PlantStageLabel(pot.Stage);
            string conditionName = ConditionNameForUi(MapScoreToConditionForUi(pot.ConditionScore));
            string trendLabel = f.Trend switch
            {
                ForecastDirection.Up => "▲ CRESCITA",
                ForecastDirection.Down => "▼ CALO",
                _ => "■ STABILE"
            };

            AppendRawLine("STATO ATTUALE");
            AppendRawLine($"§DATA§Stadio: {stageLabel}§END§");
            AppendRawLine($"Condizione: {pot.ConditionScore}% [{conditionName}]");
            AppendRawLine($"Trend: {trendLabel}");
            AppendRawLine("");

            // STAGE PROGRESSION
            int requiredDays = f.EffectiveRequiredDays;
            if (requiredDays <= 0 && f.StageReq != null) requiredDays = Mathf.Max(1, f.StageReq.durationDays);
            string daysInStage = requiredDays > 0 ? $"{pot.DaysInCurrentStage}/{requiredDays} giorni" : $"{pot.DaysInCurrentStage}/— giorni";
            int barWidth = 20;
            int pct = Mathf.Clamp(f.ProgressPercent, 0, 100);
            int filled = Mathf.RoundToInt((pct / 100f) * barWidth);
            filled = Mathf.Clamp(filled, 0, barWidth);
            string bar = new string('█', filled) + new string('░', barWidth - filled);

            string eta = f.EstimatedDaysToAdvance.HasValue ? $"{f.EstimatedDaysToAdvance.Value} giorni rimanenti" : "—";

            AppendRawLine("PROGRESSIONE STADIO");
            AppendRawLine($"Giorni nello stadio: {daysInStage}");
            AppendRawLine($"Progresso: {bar} {pct}%");
            AppendRawLine($"Prossima: {f.SoonInConditionName}");
            AppendRawLine($"Stima: {eta}");
            AppendRawLine("");

            // ADVANCEMENT REQUIREMENTS (real data)
            AppendRawLine("REQUISITI AVANZAMENTO");

            // Condizione: avanzamento consentito se non bloccante (non critica/appassita). Il trend è solo informativo.
            bool conditionReqOk = !f.BlockedByCondition;
            string conditionReqText = $"{(conditionReqOk ? "✓" : "✗")} Condizione     {pot.ConditionScore}% | Richiesto: non critica/appassita";
            AppendRawLine(conditionReqOk ? conditionReqText : $"§ERROR§{conditionReqText}§END§");

            // Hydration
            int maxHydration = _potSystemConfig != null ? _potSystemConfig.MaxHydration : 10;
            int hydrationPercent = maxHydration > 0 ? Mathf.RoundToInt((float)pot.Hydration / maxHydration * 100f) : 0;
            string hydrationReq = f.StageReq != null ? $"{f.StageReq.hydrationMin}-{f.StageReq.hydrationMax}%" : "—";
            string hydrationLine = $"{(f.HydrationOk ? "✓" : "✗")} Idratazione    {hydrationPercent}% | Richiesto: {hydrationReq}";
            AppendRawLine(f.HydrationOk ? hydrationLine : $"§ERROR§{hydrationLine}§END§");

            // Fertilizer
            string fertReq = f.StageReq != null ? $"{f.StageReq.fertilizerMin}-{f.StageReq.fertilizerMax}%" : "—";
            string fertLine = $"{(f.FertilizerOk ? "✓" : "✗")} Fertilizzante  {pot.FertilizerLevel}% | Richiesto: {fertReq}";
            AppendRawLine(f.FertilizerOk ? fertLine : $"§ERROR§{fertLine}§END§");

            // Light stress percent (UI metric) - BUG FIX: Usa GetConsecutiveLedDays invece di LightExposure
            int lightStressPercent = 0;
            if (_potSystemConfig != null)
            {
                int consecutiveDays = pot.GetConsecutiveLedDays();
                int maxDaysForFullStress = Mathf.Max(1, _potSystemConfig.MaxDaysForFullStress);
                lightStressPercent = Mathf.RoundToInt(Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f);
            }
            string luceAccesa = pot.LedSystemState == LedSystemState.Off ? "Nessuna" : (pot.LedSystemState == LedSystemState.Blue ? "Blu" : "Rosso");
            string lightReq = "20-80%";
            string lightLine = $"{(f.LedOk ? "✓" : "✗")} Luce accesa: {luceAccesa} | Stress da luce: {lightStressPercent}% (range {lightReq})";
            AppendRawLine(f.LedOk ? lightLine : $"§ERROR§{lightLine}§END§");

            // Mold risk level (0-3). Growth is blocked at >=2.
            int mold = Mathf.Clamp(pot.MoldRiskLevel, 0, 3);
            bool moldOk = mold < 2;
            string moldLine = $"{(moldOk ? "✓" : "✗")} Rischio muffa  Liv. {mold} | Richiesto: <2";
            AppendRawLine(moldOk ? moldLine : $"§ERROR§{moldLine}§END§");

            AppendRawLine("");

            int unmet = 0;
            if (!conditionReqOk) unmet++;
            if (!f.HydrationOk) unmet++;
            if (!f.FertilizerOk) unmet++;
            if (!f.LedOk) unmet++;
            if (!moldOk) unmet++;

            if (f.BlocksAdvancement)
            {
                AppendRawLine("§ERROR§► STATO: BLOCCATO§END§");
            }
            else if (f.HydrationOk && f.LedOk && f.FertilizerOk && f.DurationOk && f.OptimalDaysOk && f.PointsOk)
            {
                AppendRawLine("§TITLE§► STATO: PRONTO PER AVANZAMENTO§END§");
            }
            else
            {
                AppendRawLine($"§ERROR§► STATO: {unmet} REQUISITO/I NON SODDISFATTI§END§");
            }

            AppendRawLine("");
        }

        private static PlantCondition MapScoreToConditionForUi(int score)
        {
            if (score >= DifficultyCalibrationConfig.ConditionThresholdRigogliosa) return PlantCondition.Rigogliosa;
            if (score >= DifficultyCalibrationConfig.ConditionThresholdSana) return PlantCondition.Sana;
            if (score >= DifficultyCalibrationConfig.ConditionThresholdAppassita) return PlantCondition.Appassita;
            return PlantCondition.Critica;
        }

        private static string ConditionNameForUi(PlantCondition condition)
        {
            return condition switch
            {
                PlantCondition.Rigogliosa => "Rigogliosa",
                PlantCondition.Sana => "Sana",
                PlantCondition.Appassita => "Appassita",
                PlantCondition.Critica => "Critica",
                PlantCondition.Stressata => "Sana", // retrocompat
                _ => condition.ToString()
            };
        }

        private static string GetSoonInConditionName(PotStateModel pot)
        {
            if (pot == null) return "—";

            var current = MapScoreToConditionForUi(pot.ConditionScore);
            var trend = (ForecastDirection)pot.ForecastDirection;

            PlantCondition target = current;
            if (trend == ForecastDirection.Up)
            {
                target = current switch
                {
                    PlantCondition.Critica => PlantCondition.Appassita,
                    PlantCondition.Appassita => PlantCondition.Sana,
                    PlantCondition.Sana => PlantCondition.Rigogliosa,
                    _ => PlantCondition.Rigogliosa
                };
            }
            else if (trend == ForecastDirection.Down)
            {
                target = current switch
                {
                    PlantCondition.Rigogliosa => PlantCondition.Sana,
                    PlantCondition.Sana => PlantCondition.Appassita,
                    PlantCondition.Appassita => PlantCondition.Critica,
                    _ => PlantCondition.Critica
                };
            }

            return ConditionNameForUi(target);
        }

        private StageForecast CalculateStageForecast(PotStateModel pot, PlantData plantData)
        {
            if (pot == null || !pot.HasPlant)
                return null;

            string potId = pot.PotId ?? "POT-???";
            string plantName = GetPotDisplayName(pot);

            var result = new StageForecast
            {
                PotId = potId,
                PlantName = plantName,
                CurrentStage = (PlantStage)pot.Stage,
                DaysInCurrentStage = pot.DaysInCurrentStage,
                DaysConsecutiveOptimal = pot.DaysConsecutiveOptimal,
                MoldRiskLevel = pot.MoldRiskLevel,
                SoonInConditionName = GetSoonInConditionName(pot),
                ConditionScore = pot.ConditionScore,
                Trend = (ForecastDirection)pot.ForecastDirection
            };

            // Manual/terminal stages: HarvestReady and Resting do not auto-advance in current gameplay.
            if (result.CurrentStage == PlantStage.HarvestReady || result.CurrentStage == PlantStage.Resting)
            {
                result.IsFinalOrManual = true;
                result.NextStage = null;
                result.EstimatedDaysToAdvance = null;
                result.HydrationOk = true;
                result.LedOk = true;
                result.FertilizerOk = true;
                result.DurationOk = true;
                result.OptimalDaysOk = true;
                result.PointsOk = true;
                result.ProgressPercent = 100;
                return result;
            }

            // Determine next stage (matches DayCycleController switch).
            result.NextStage = result.CurrentStage switch
            {
                PlantStage.Seed => PlantStage.Sprout,
                PlantStage.Sprout => PlantStage.Growth,
                PlantStage.Growth => PlantStage.Flowering,
                PlantStage.Flowering => PlantStage.HarvestReady,
                _ => (PlantStage?)null
            };

            if (plantData == null)
            {
                result.BlocksAdvancement = true;
                result.EstimatedDaysToAdvance = null;
                return result;
            }

            var stageReq = plantData.GetStageRequirements(result.CurrentStage);
            result.StageReq = stageReq;
            if (stageReq == null)
            {
                // If no requirements are defined, treat as unknown rather than mutating game logic.
                result.EstimatedDaysToAdvance = null;
                result.ProgressPercent = 0;
                return result;
            }

            int maxHydration = _potSystemConfig != null ? _potSystemConfig.MaxHydration : 10;
            int hydrationPercent = maxHydration > 0 ? Mathf.RoundToInt((float)pot.Hydration / maxHydration * 100f) : 0;
            result.HydrationPercent = hydrationPercent;

            // Match DayCycleController advancement checks (read-only, conservative).
            result.HydrationOk = stageReq.IsHydrationInRange(hydrationPercent);

            // LED OK: stress in range 20%-80% (sotto 20% non beneficia, sopra 80% burn). Con LED off conta se già in range.
            const int LightStressOkMin = 20;
            const int LightStressOkMax = 80;
            result.LedOk = false;
            int consecutiveDaysLed = pot.GetConsecutiveLedDays();
            int maxDaysForFullStress = _potSystemConfig != null ? _potSystemConfig.MaxDaysForFullStress : 5;
            float stressPercentage = Mathf.Clamp01((float)consecutiveDaysLed / maxDaysForFullStress) * 100f;
            bool stressInRange = stressPercentage >= LightStressOkMin && stressPercentage <= LightStressOkMax;
            if (pot.LedSystemState == LedSystemState.Off)
                result.LedOk = stressInRange;
            else
            {
                result.LedOk = stageReq.IsLedRequirementMet(pot.LedSystemState) && stressInRange;
            }

            PlantCondition currentCondition = (PlantCondition)pot.ConditionLabel;
            result.BlockedByCondition = ConditionGrowthModifier.BlocksAdvancement(currentCondition);
            result.BlockedByMold = pot.MoldRiskLevel >= 2;
            result.BlocksAdvancement = result.BlockedByCondition || result.BlockedByMold;

            int daysModifier = ConditionGrowthModifier.GetDaysModifier(currentCondition);
            int phDaysModifier = 0;
            if (_phSystem != null && plantData != null && plantData.IsPhInOptimalRange(_phSystem.CurrentPh))
            {
                phDaysModifier = -1;
            }

            int effectiveRequiredDays = stageReq.durationDays + daysModifier + phDaysModifier;
            if (effectiveRequiredDays < 0) effectiveRequiredDays = 0;
            result.EffectiveRequiredDays = effectiveRequiredDays;
            result.DurationOk = pot.DaysInCurrentStage >= effectiveRequiredDays;

            // Optimal days required (same as controller).
            int requiredOptimalDays = result.CurrentStage == PlantStage.Seed ? 1 : stageReq.durationDays;
            result.RequiredOptimalDays = requiredOptimalDays;
            result.OptimalDaysOk = pot.DaysConsecutiveOptimal >= requiredOptimalDays;

            // Fertilizer OK (same as controller).
            if (result.CurrentStage == PlantStage.Seed || result.CurrentStage == PlantStage.Sprout)
            {
                result.FertilizerOk = stageReq.IsFertilizerInRange(pot.FertilizerLevel)
                                      || pot.FertilizerLevel == 0
                                      || pot.FertilizerLevel > stageReq.fertilizerMax;
            }
            else
            {
                result.FertilizerOk = pot.FertilizerLevel >= stageReq.fertilizerMin;
            }

            // Points OK (same thresholds as controller).
            int totalPoints = pot.GrowthPointsWater + pot.GrowthPointsLight + pot.GrowthPointsFertilizer;
            int requiredPoints = (result.CurrentStage == PlantStage.Seed || result.CurrentStage == PlantStage.Sprout) ? 2 : 3;
            result.TotalPoints = totalPoints;
            result.RequiredPoints = requiredPoints;
            result.PointsOk = totalPoints >= requiredPoints;

            // Progress: use existing in-game counters (days + accumulated points W/L/F) against required totals.
            int denom = effectiveRequiredDays + requiredPoints;
            if (denom <= 0)
            {
                result.ProgressPercent = 0;
            }
            else
            {
                int numer = pot.DaysInCurrentStage + totalPoints;
                float pct = Mathf.Clamp01((float)numer / denom) * 100f;
                result.ProgressPercent = Mathf.RoundToInt(pct);
            }

            // Estimate daily points without mutating pot state.
            result.EstimatedDailyPoints = EstimateDailyGrowthPointsReadOnly(pot, stageReq, _potSystemConfig);

            // If any static requirement is currently not met, we can't estimate under "conditions stay as-is".
            if (result.BlocksAdvancement || !result.HydrationOk || !result.LedOk || !result.FertilizerOk)
            {
                result.EstimatedDaysToAdvance = null;
                return result;
            }

            // Duration always progresses daily.
            int remainingDuration = Mathf.Max(0, effectiveRequiredDays - pot.DaysInCurrentStage);

            // Optimal days and points progress only if today's estimated points meet required thresholds.
            int requiredOptimalPoints = requiredPoints;
            int? remainingOptimal = null;
            if (result.EstimatedDailyPoints.TotalPoints >= requiredOptimalPoints)
            {
                remainingOptimal = Mathf.Max(0, requiredOptimalDays - pot.DaysConsecutiveOptimal);
            }

            int? remainingPoints = null;
            if (totalPoints < requiredPoints)
            {
                int need = requiredPoints - totalPoints;
                int perDay = Mathf.Max(0, result.EstimatedDailyPoints.TotalPoints);
                if (perDay > 0)
                {
                    remainingPoints = Mathf.CeilToInt((float)need / perDay);
                }
            }
            else
            {
                remainingPoints = 0;
            }

            if (!remainingOptimal.HasValue)
            {
                result.EstimatedDaysToAdvance = null;
                return result;
            }

            // Conservative: need to satisfy all counters.
            int eta = remainingDuration;
            eta = Mathf.Max(eta, remainingOptimal.Value);
            if (remainingPoints.HasValue) eta = Mathf.Max(eta, remainingPoints.Value);

            result.EstimatedDaysToAdvance = eta;
            return result;
        }

        private static EstimatedDailyPoints EstimateDailyGrowthPointsReadOnly(PotStateModel pot, StageRequirements stageReq, PotSystemConfig config)
        {
            if (pot == null || stageReq == null || !pot.HasPlant)
                return new EstimatedDailyPoints(0, 0, 0);

            int maxHydration = config != null ? config.MaxHydration : 10;
            int hydrationPercent = maxHydration > 0 ? Mathf.RoundToInt((float)pot.Hydration / maxHydration * 100f) : 0;
            int water = stageReq.IsHydrationInRange(hydrationPercent) ? 1 : 0;

            int light = 0;
            if (pot.LedSystemState == LedSystemState.Off)
            {
                int consecutiveDays = pot.GetConsecutiveLedDays();
                int maxDaysForFullStress = config != null ? config.MaxDaysForFullStress : 5;
                float stressPercentage = Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f;
                bool stressInOptimalRange = stressPercentage > 0f && stressPercentage < 100f;
                light = stressInOptimalRange ? 1 : 0;
            }
            else
            {
                light = stageReq.IsLedRequirementMet(pot.LedSystemState) ? 1 : 0;
            }

            // Fertilizer points: only awarded when inside the defined stage range (same as GrowthPointsCalculator)
            int fertilizer = stageReq.IsFertilizerInRange(pot.FertilizerLevel) ? 1 : 0;

            return new EstimatedDailyPoints(water, light, fertilizer);
        }

        private void PrintStartCommands()
        {
            string VisibleText(string s)
            {
                return s.Replace("§TITLE§", "")
                    .Replace("§CMD§", "")
                    .Replace("§INFO§", "")
                    .Replace("§DATA§", "")
                    .Replace("§VAL§", "")
                    .Replace("§WARN§", "")
                    .Replace("§ERROR§", "")
                    .Replace("§END§", "");
            }

            int VisibleLen(string s) => VisibleText(s).Length;

            void Line(string content)
            {
                AppendRawLine(content);
            }

            string CmdLine(string cmd, string desc)
            {
                int cmdCol = 22;
                int pad = Mathf.Max(0, cmdCol - VisibleLen(cmd));
                return $"  {cmd}{new string(' ', pad)}- {desc}";
            }

            AppendRawLine("§DATA§▸ COMANDI DISPONIBILI§END§");
            Line("");
            Line("§DATA§▸ GESTIONE E MONITORAGGIO VASI§END§");
            Line(CmdLine("§CMD§STATUS§END§", "§TITLE§Stato, progressione e requisiti per tutti i vasi§END§"));
            Line(CmdLine("§CMD§NOTE [POT-ID]§END§", "§TITLE§Apri visualizzatore note diario vaso§END§"));
            Line(CmdLine("§CMD§PLANT [POT-ID]§END§", "§TITLE§Accoda azione piantare (1 AP)§END§"));
            Line(CmdLine("§CMD§UPROOT [POT-ID]§END§", "§TITLE§Accoda rimozione pianta (1 AP)§END§"));
            Line("");
            Line("§PURPLE§▸ OPERAZIONI COLTIVAZIONE§END§");
            Line(CmdLine("§CMD§WATERING [POT-ID]§END§", "§TITLE§Attiva/disattiva irrigazione (1 AP)§END§"));
            Line(CmdLine("§CMD§SPRAY [POT-ID]§END§", "§TITLE§Accoda applicazione additivo (1 AP)§END§"));
            Line(CmdLine("§CMD§FERTILIZE [POT-ID]§END§", "§TITLE§Accoda boost nutrienti (1 AP)§END§"));
            Line(CmdLine("§CMD§PRUNE [POT-ID]§END§", "§TITLE§Accoda potatura (1 AP)§END§"));
            Line(CmdLine("§CMD§LED RED [POT-ID]§END§", "§TITLE§Attiva/disattiva luce rossa (1 AP)§END§"));
            Line(CmdLine("§CMD§LED BLUE [POT-ID]§END§", "§TITLE§Attiva/disattiva luce blu (1 AP)§END§"));
            Line(CmdLine("§CMD§HARVEST [POT-ID]§END§", "§TITLE§Accoda raccolta (1 AP)§END§"));
            Line("");
            Line("§DATA§▸ SLOT PASSIVI (CRYO MACHINE)§END§");
            Line(CmdLine("§CMD§PASSIVE§END§", "§TITLE§Protocollo + overview slot passivi Cryo Machine§END§"));
            Line(CmdLine("§CMD§CRYO SEND [POT-ID]§END§", "§TITLE§Trasferisce pianta Lvl 5 dal pot allo slot Cryo§END§"));
            Line(CmdLine("§CMD§CRYO EXTRACT§END§", "§TITLE§Sposta pianta da Cryo → inventario (guida interattiva)§END§"));
            Line(CmdLine("§CMD§CRYO RESTORE§END§", "§TITLE§Sposta pianta da Cryo → pot attivo (guida interattiva)§END§"));
            Line("");
            Line("§DATA§▸ CONTROLLI SISTEMA§END§");
            Line(CmdLine("§CMD§PROTOCOL§END§", "§TITLE§Visualizza Protocollo Biologico DOME_02§END§"));
            Line(CmdLine("§CMD§QUEUE SHOW§END§", "§TITLE§Coda Azioni - Mostra azioni in coda§END§"));
            Line(CmdLine("§CMD§START§END§", "§TITLE§Mostra questo elenco comandi§END§"));
            Line(CmdLine("§CMD§CLEAR§END§", "§TITLE§Svuota coda azioni§END§"));
            Line(CmdLine("§CMD§CLOSE§END§", "§TITLE§Chiudi analisi dettagliata vaso§END§"));
            Line(CmdLine("§CMD§EXIT§END§", "§TITLE§Chiudi terminale (chiede S/N se c'è coda)§END§"));
            Line("");
            AppendRawLine("");
            FlushConsole();
        }

        /// <summary>Riepilogo generale: tabella tutti i vasi + testo CONDIZIONI PER VASO.</summary>
        private void PrintStatusSummaryTable(List<PotSlot> pots)
        {
            if (pots == null) return;
            AppendRawLine("§DATA§▸ RIEPILOGO STATO VASI§END§");
            AppendRawLine("ID       │ STATO      │ NOME PIANTA          │ STADIO       │ COND   │ IDR");
            AppendRawLine("─────────┼────────────┼─────────────────────┼──────────────┼────────┼──────");
            foreach (var pot in pots)
            {
                string potId = pot != null ? pot.PotId : "POT-???";
                var state = pot != null && pot.PotActions != null ? pot.PotActions.PotState : null;

                string status;
                string plantName = "---";
                string stage = "---";
                string condition = "---";
                string hydDots = "---";

                if (state == null || state.IsEmpty || !state.HasPlant)
                {
                    status = "§TITLE§VUOTO§END§";
                }
                else
                {
                    plantName = GetPotDisplayName(state);
                    stage = PlantStageLabel(state.Stage);
                    int score = state.ConditionScore;
                    bool isCritical = score < 40;
                    status = isCritical ? "§ERROR§CRITICO§END§" : "§DATA§OCCUPATO§END§";
                    condition = isCritical ? $"§ERROR§{score}%§END§" : $"§TITLE§{score}%§END§";

                    int percentHyd = Mathf.Clamp(state.Hydration * 10, 0, 100);
                    int maxDots = 5;
                    int filled = Mathf.Clamp(Mathf.RoundToInt(percentHyd / 20f), 0, maxDots);
                    var filledDots = $"§TITLE§{new string('●', filled)}§END§";
                    var emptyDots = $"<color=#888888>{new string('○', maxDots - filled)}</color>";
                    hydDots = filledDots + emptyDots;
                }

                AppendRawLine($"{potId,-8} │ {status,-10} │ {plantName,-19} │ {stage,-12} │ {condition,-6} │ {hydDots,-4}");
            }
        }

        /// <summary>Approfondimento per un solo vaso: STATO CORRENTE, STAGE PROGRESSION, ADVANCEMENT REQUIREMENTS, CONSIGLIO.</summary>
        private void PrintStatusDetailForPot(PotSlot pot)
        {
            if (pot == null) return;
            string potId = pot.PotId ?? "POT-???";
            var state = pot.PotActions != null ? pot.PotActions.PotState : null;
            var plantData = state != null ? state.GetPlantData() : null;
            if (state == null || state.IsEmpty || !state.HasPlant)
            {
                AppendRawLine($"§DATA§--- {potId} ---§END§ VUOTO");
                AppendRawLine("");
                return;
            }
            var f = CalculateStageForecast(state, plantData);
            if (f != null)
                PrintStatusPotSections(pot, state, plantData, f);
            else
                AppendRawLine($"§DATA§--- {potId} ---§END§");
            var consiglioLines = BuildConsiglioForPot(state, plantData);
            if (consiglioLines != null && consiglioLines.Count > 0)
            {
                AppendRawLine("");
                AppendRawLine("");
                AppendRawLine("");
                AppendRawLine("");
                AppendRawLine("");
                AppendRawLine("<color=#B88FC9>────────────────────────────────────────────────────────────────────────────</color>");
                AppendRawLine("§DATA§▸ CONSIGLIO§END§");
                AppendRawLine("§DATA§[§END§");
                foreach (var line in consiglioLines)
                {
                    string formatted = FormatConsiglioLineWithCommands(line);
                    AppendRawLine("§TITLE§      • " + formatted.Replace("§END§", "§END§§TITLE§") + "§END§");
                }
                AppendRawLine("§DATA§]§END§");
            }
            AppendRawLine("");

            // RESEARCHED NOTE (lore): quando si usa il collector per STATUS, non si aggiunge qui ma in coda a StatusSecondHalfRoutine
            if (_statusLinesCollector == null)
            {
                AppendRawLine("<color=#00AA00>────────────────────────────────────────────────────────────────────────────</color>");
                AppendRawLine("§DATA§▸ RESEARCHED NOTE§END§");
                var noteLines = BuildResearchedNoteLines(state, plantData);
                for (int i = 0; i < noteLines.Count; i++)
                    AppendRawLine("§TITLE§" + noteLines[i] + "§END§");
                AppendRawLine("");
            }
        }

        private void PrintStatusTable()
        {
            var pots = FindPots();
            PrintStatusSummaryTable(pots);
            foreach (var pot in pots)
                PrintStatusDetailForPot(pot);
        }

        private const int StatusLabelDotWidth = 22;

        /// <summary>Riga in stile reference: LABEL.......: value</summary>
        private static string StatusDotted(string label, string value)
        {
            int pad = Mathf.Max(0, StatusLabelDotWidth - label.Length);
            return label + new string('.', pad) + ": " + value;
        }

        /// <summary>Solo la parte label + puntini + ": " per colorare label e valore separatamente.</summary>
        private static string StatusDottedLabelOnly(string label)
        {
            int pad = Mathf.Max(0, StatusLabelDotWidth - label.Length);
            return label + new string('.', pad) + ": ";
        }

        /// <summary>Evidenzia nel testo del Potere Attivo le parti numeriche/effetti principali.</summary>
        private static string FormatActivePowerHighlight(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            string s = raw;
            s = s.Replace("scala +1/Lv (fino a +5)", "§TITLE§scala +1/Lv (fino a +5)§END§");
            s = s.Replace("scala -1/Lv (fino a -5)", "§TITLE§scala -1/Lv (fino a -5)§END§");
            s = s.Replace("+5 global pH", "§TITLE§+5 global pH§END§");
            s = s.Replace("cura muffe Dome ogni 2 giorni", "§TITLE§cura muffe Dome ogni 2 giorni§END§");
            s = s.Replace("−10% rischio muffe Dome", "§TITLE§−10% rischio muffe Dome§END§");
            s = s.Replace("-10% rischio muffe Dome", "§TITLE§-10% rischio muffe Dome§END§");
            s = s.Replace("+10% probabilità di mutazione Spore globali", "§TITLE§+10% probabilità di mutazione Spore globali§END§");
            return s;
        }

        /// <summary>Potere passivo per STATUS (da PlantData).</summary>
        private static string GetPassivePowerForDisplay(PlantData plantData)
        {
            if (plantData == null) return "";
            return string.IsNullOrWhiteSpace(plantData.PassivePower) ? "" : plantData.PassivePower;
        }

        /// <summary>Tag colore per valore condizione: verde acceso, verde spento, giallo, rosso.</summary>
        private static string StatusConditionTag(PlantCondition c)
        {
            switch (c)
            {
                case PlantCondition.Rigogliosa: return "§TITLE§";
                case PlantCondition.Sana: return "§DATA§";
                case PlantCondition.Appassita: return "§WARN§";
                case PlantCondition.Critica: return "§ERROR§";
                default: return "§DATA§";
            }
        }

        /// <summary>Layout STATUS stile reference: header, titolo pianta, PLANT STATUS (dotted), VITAL PARAMETERS (barre), REQUISITI E AVANZAMENTO.</summary>
        private void PrintStatusPotSections(PotSlot potSlot, PotStateModel pot, PlantData plantData, StageForecast f)
        {
            string plantName = GetPotDisplayName(pot, plantData);
            string shortCode = pot.PlantCode.Replace("PLT-", "").Replace("-", " ");
            string potId = f.PotId ?? "POT-???";

            // —— Header ——
            AppendRawLine("§TITLE§• SPECIMEN ID:§END§ §DATA§" + potId + "§END§    §DATA§[SYSTEM STATUS: ACTIVE]§END§");
            AppendRawLine("<color=#00AA00>────────────────────────────────────────────────────────────────────────────</color>");

            // —— Title block: nome in grande (font size + MAIUSCOLO + spazi), [code], description ——
            string nameBig = string.Join(" ", plantName.ToUpperInvariant().Select(c => c.ToString()));
            AppendRawLine("<size=18>§TITLE§" + nameBig + "§END§</size>");
            AppendRawLine("§DATA§[" + shortCode + "]§END§ §INFO§" + (plantData != null && !string.IsNullOrEmpty(plantData.Description) ? plantData.Description : "—") + "§END§");
            AppendRawLine("<color=#00AA00>────────────────────────────────────────────────────────────────────────────</color>");

            // —— ENVIRONMENTAL DATA (pH Dome = stesso valore oscillante di TopBar/tooltip, colore come PH drift bar, suffisso banda) ——
            AppendRawLine("§TITLE§ENVIRONMENTAL DATA§END§");
            float phValue = _phSystem != null ? _phSystem.CurrentPh : 0f;
            float condensationPct = _gameManager != null && _gameManager.CondensationSystem != null ? _gameManager.CondensationSystem.CurrentAccumulation : 0f;
            string phStr = phValue.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            string phBand = GetPhBandNameForDisplay(phValue);
            string phColorHex = GetPhColorHexForDomePhBand();
            string phLineRaw = "§DATA§" + StatusDottedLabelOnly("pH DOME") + "§END§<color=" + phColorHex + ">" + phStr + " — " + phBand + "</color>";
            AppendRawLine(phLineRaw);
            string condStr = Mathf.Clamp(Mathf.RoundToInt(condensationPct), 0, 100) + "%";
            AppendRawLine("§DATA§" + StatusDotted("CONDENSAZIONE", condStr) + "§END§");
            AppendRawLine("<color=#00AA00>────────────────────────────────────────────────────────────────────────────</color>");
            StartEnvironmentalPhRefresh();

            // —— PLANT STATUS: CONDITION, TREND, GIORNI STADIO con scala colori; Stadio Attuale e Prossimo Stadio in cima (cyan) ——
            string stageLabel = PlantStageLabel(pot.Stage);
            PlantCondition conditionUi = MapScoreToConditionForUi(pot.ConditionScore);
            string conditionName = ConditionNameForUi(conditionUi);
            string conditionTag = StatusConditionTag(conditionUi);
            string trendLabel = f.Trend == ForecastDirection.Up ? "▲ CRESCITA" : (f.Trend == ForecastDirection.Down ? "▼ CALO" : "■ STABILE");
            string trendTag = f.Trend == ForecastDirection.Up ? "§TITLE§" : (f.Trend == ForecastDirection.Down ? "§ERROR§" : "§DATA§");
            string nextStageLabel = f.NextStage.HasValue ? PlantStageLabel((int)f.NextStage.Value) : "—";
            int requiredDays = f.EffectiveRequiredDays;
            if (requiredDays <= 0 && f.StageReq != null) requiredDays = Mathf.Max(1, f.StageReq.durationDays);
            string daysInStage = requiredDays > 0 ? $"{pot.DaysInCurrentStage}/{requiredDays} giorni" : $"{pot.DaysInCurrentStage}/—";
            int progressPct = requiredDays > 0 ? Mathf.Clamp((pot.DaysInCurrentStage * 100) / requiredDays, 0, 100) : 0;
            string daysTag = progressPct >= 75 ? "§TITLE§" : (progressPct >= 50 ? "§DATA§" : (progressPct >= 25 ? "§WARN§" : "§DATA§"));

            AppendRawLine("§TITLE§PLANT STATUS§END§");
            AppendRawLine("§DATA§" + StatusDottedLabelOnly("CONDIZIONE") + "§END§" + conditionTag + $"{conditionName} ({pot.ConditionScore}%)" + "§END§");
            AppendRawLine("§DATA§" + StatusDottedLabelOnly("TREND") + "§END§" + trendTag + trendLabel + "§END§");
            AppendRawLine("§DATA§" + StatusDottedLabelOnly("GIORNI STADIO") + "§END§" + daysTag + daysInStage + "§END§");
            // Modificatori giorni: solo se condizione o pH modificano i giorni richiesti
            if (f.StageReq != null)
            {
                PlantCondition currentCondition = (PlantCondition)pot.ConditionLabel;
                int daysModifier = ConditionGrowthModifier.GetDaysModifier(currentCondition);
                int phDaysModifier = (_phSystem != null && plantData != null && plantData.IsPhInOptimalRange(_phSystem.CurrentPh)) ? -1 : 0;
                if (daysModifier != 0 || phDaysModifier != 0)
                {
                    int baseDays = f.StageReq.durationDays;
                    var modParts = new List<string>();
                    if (daysModifier != 0) modParts.Add("condizione " + (daysModifier > 0 ? "+" : "") + daysModifier);
                    if (phDaysModifier != 0) modParts.Add("pH " + (phDaysModifier > 0 ? "+" : "") + phDaysModifier);
                    AppendRawLine("§INFO§  (richiesti effettivi: " + f.EffectiveRequiredDays + " = base " + baseDays + (modParts.Count > 0 ? ", " + string.Join(", ", modParts) : "") + ")§END§");
                }
            }
            AppendRawLine("§DATA§" + StatusDotted("STADIO ATTUALE", stageLabel) + "§END§");
            AppendRawLine("§DATA§" + StatusDotted("PROSSIMO STADIO", nextStageLabel) + "§END§");
            AppendRawLine("<color=#00AA00>────────────────────────────────────────────────────────────────────────────</color>");

            // —— VITAL PARAMETERS (barre stile reference, distanziate in verticale) ——
            AppendRawLine("§TITLE§VITAL PARAMETERS§END§");
            int maxHydration = _potSystemConfig != null ? _potSystemConfig.MaxHydration : 10;
            int hydrationPercent = maxHydration > 0 ? Mathf.RoundToInt((float)pot.Hydration / maxHydration * 100f) : 0;
            string hydrationReq = f.StageReq != null ? $"{f.StageReq.hydrationMin}%-{f.StageReq.hydrationMax}%" : "—";
            int barWidth = 12;
            int hFilled = Mathf.Clamp((hydrationPercent * barWidth) / 100, 0, barWidth);
            string hBar = "[" + new string('█', hFilled) + new string('░', barWidth - hFilled) + "]";
            AppendRawLine("§DATA§IDRATAZIONE§END§ " + hBar + " §TITLE§" + hydrationPercent + "%§END§ §INFO§(" + hydrationReq + " ottimale)§END§");
            AppendRawLine("");

            string fertReq = f.StageReq != null ? $"{f.StageReq.fertilizerMin}%-{f.StageReq.fertilizerMax}%" : "—";
            int fertFilled = Mathf.Clamp((pot.FertilizerLevel * barWidth) / 100, 0, barWidth);
            string fertBar = "[" + new string('█', fertFilled) + new string('░', barWidth - fertFilled) + "]";
            AppendRawLine("§DATA§FERTILIZZANTE§END§ " + fertBar + " §TITLE§" + pot.FertilizerLevel + "%§END§ §INFO§(" + fertReq + " ottimale)§END§");
            AppendRawLine("");

            int lightStressPercent = 0;
            if (_potSystemConfig != null)
            {
                int consecutiveDays = pot.GetConsecutiveLedDays();
                int maxDaysForFullStress = Mathf.Max(1, _potSystemConfig.MaxDaysForFullStress);
                lightStressPercent = Mathf.RoundToInt(Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f);
            }
            int lightFilled = Mathf.Clamp((lightStressPercent * barWidth) / 100, 0, barWidth);
            string lightBar = "[" + new string('█', lightFilled) + new string('░', barWidth - lightFilled) + "]";
            AppendRawLine("§DATA§STRESS LUCE§END§ " + lightBar + " §TITLE§" + lightStressPercent + "%§END§ §INFO§(20%-80% ottimale)§END§");
            AppendRawLine("");

            int moldLevel = Mathf.Clamp(pot.MoldRiskLevel, 0, 3);
            int moldFilled = Mathf.Clamp((moldLevel * barWidth) / 3, 0, barWidth);
            string moldBar = "[" + new string('█', moldFilled) + new string('░', barWidth - moldFilled) + "]";
            string moldLabel = moldLevel switch { 0 => "Sicuro", 1 => "Lieve", 2 => "Severo", 3 => "Critico", _ => "—" };
            AppendRawLine("§DATA§RISCHIO MUFFA§END§ " + moldBar + " §TITLE§" + moldLabel + "§END§");
            AppendRawLine("");

            int fruitCount = Mathf.Clamp(Mathf.RoundToInt(pot.AmountFruits), 0, 3);
            int fruitFilled = Mathf.Clamp((fruitCount * barWidth) / 3, 0, barWidth);
            string fruitBar = "[" + new string('█', fruitFilled) + new string('░', barWidth - fruitFilled) + "]";
            string fruitInfo = pot.Stage == (int)PlantStage.HarvestReady
                ? $"({fruitCount}/3 maturi)"
                : "(disponibile a stadio MATURO)";
            AppendRawLine("§DATA§FRUTTI MATURI§END§ " + fruitBar + " §TITLE§" + fruitCount + "/3§END§ §INFO§" + fruitInfo + "§END§");
            AppendRawLine("");

            // —— Potere Attivo (a capo, con evidenziazione effetti) e Potere Passivo ——
            AppendRawLine("§DATA§Potere Attivo:§END§");
            string activePowerRaw = !string.IsNullOrWhiteSpace(pot.ActivePowerLabel)
                ? pot.ActivePowerLabel
                : (plantData != null && !string.IsNullOrWhiteSpace(plantData.ActivePower) ? plantData.ActivePower : "—");
            string activePowerFormatted = FormatActivePowerHighlight(activePowerRaw).Replace("§END§", "§END§§INFO§");
            AppendRawLine("§INFO§" + activePowerFormatted + "§END§");
            AppendRawLine("");
            AppendRawLine("§DATA§Potere Passivo:§END§");
            string passivePower = !string.IsNullOrWhiteSpace(pot.PassivePowerLabel)
                ? pot.PassivePowerLabel
                : (plantData != null && !string.IsNullOrWhiteSpace(GetPassivePowerForDisplay(plantData)) ? GetPassivePowerForDisplay(plantData) : "—");
            AppendRawLine("§INFO§" + passivePower + "§END§");
            AppendRawLine("<color=#00AA00>────────────────────────────────────────────────────────────────────────────</color>");

            var statusFx = new List<string>();
            BotanicalPowerFacade.AppendStatusEffectLinesForPot(statusFx, pot.PotId, _phSystem);
            for (int i = 0; i < statusFx.Count; i++)
                AppendRawLine(statusFx[i]);
            AppendRawLine("<color=#00AA00>────────────────────────────────────────────────────────────────────────────</color>");

            // —— LEGENDA (sopra Requisiti e Avanzamento) ——
            AppendRawLine("§DATA§▸ LEGENDA§END§");
            AppendRawLine("§TITLE§Come leggere i dati:§END§ §TITLE§Luce accesa§END§: quale LED è acceso (Blu/Rosso) o §TITLE§Nessuna§END§ in rosso. §TITLE§Stress da luce§END§: range ideale 20%-80% (sotto 20% la pianta non beneficia, sopra 80% rischio burn). Acqua e Condizione come prima.");
            AppendRawLine("§TITLE§I requisiti§END§ (idratazione, stress luminoso, fertilizzante) si riferiscono allo §TITLE§stadio di crescita attuale§END§ della pianta.");
            AppendRawLine("");
            // —— REQUISITI E AVANZAMENTO (condizione = non bloccante; trend solo informativo) ——
            AppendRawLine("§TITLE§REQUISITI E AVANZAMENTO§END§");
            bool conditionReqOk = !f.BlockedByCondition;
            string conditionReqText = $"{(conditionReqOk ? "✓" : "✗")} §DATA§Condizione§END§    {pot.ConditionScore}% | Richiesto: non critica/appassita";
            AppendRawLine(conditionReqOk ? conditionReqText : $"§ERROR§{conditionReqText}§END§");
            AppendRawLine(conditionReqOk ? "§DIM§------ Condizione non bloccante.§END§" : "§DIM§------ Condizione critica o appassita: avanzamento bloccato.§END§");

            string hydrationReqStr = f.StageReq != null ? $"{f.StageReq.hydrationMin}%-{f.StageReq.hydrationMax}%" : "—";
            string hydrationLine = $"{(f.HydrationOk ? "✓" : "✗")} §DATA§Idratazione§END§  {hydrationPercent}% | Richiesto: {hydrationReqStr}";
            AppendRawLine(f.HydrationOk ? hydrationLine : $"§ERROR§{hydrationLine}§END§");
            string hydrationHint = pot.WateringSystemOn
                ? (f.HydrationOk ? "------ Impianto goccia attivo, parametro in range." : "------ Impianto goccia attivo, situazione in miglioramento.")
                : "------ Impianto spento, usa comando WATERING per attivarlo.";
            AppendRawLine("§DIM§" + hydrationHint + "§END§");

            string fertReqStr = f.StageReq != null ? $"{f.StageReq.fertilizerMin}%-{f.StageReq.fertilizerMax}%" : "—";
            string fertLine = $"{(f.FertilizerOk ? "✓" : "✗")} §DATA§Fertilizzante§END§ {pot.FertilizerLevel}% | Richiesto: {fertReqStr}";
            AppendRawLine(f.FertilizerOk ? fertLine : $"§ERROR§{fertLine}§END§");
            AppendRawLine(f.FertilizerOk ? "§DIM§------ Fertilizzante in range.§END§" : "§DIM§------ Fuori range, usa comando FERTILIZE [POT-ID] se necessario.§END§");

            string lightLine = $"{(f.LedOk ? "✓" : "✗")} §DATA§Stress luce§END§  {lightStressPercent}% | Richiesto: 20%-80%";
            AppendRawLine(f.LedOk ? lightLine : $"§ERROR§{lightLine}§END§");
            bool ledOn = pot.LedSystemState != LedSystemState.Off;
            string lightHint = ledOn
                ? (f.LedOk ? "------ LED acceso, stress in range." : "------ LED acceso, situazione in miglioramento (o spegni se stress >80%).")
                : (f.LedOk ? "------ LED spento ma stress già in range." : "------ LED spento, usa comando LED BLUE/RED per attivarlo.");
            AppendRawLine("§DIM§" + lightHint + "§END§");

            bool moldOk = moldLevel < 2;
            string moldLine = $"{(moldOk ? "✓" : "✗")} §DATA§Rischio muffa§END§ {moldLabel} | Richiesto: <2";
            AppendRawLine(moldOk ? moldLine : $"§ERROR§{moldLine}§END§");
            AppendRawLine(moldOk ? "§DIM§------ Rischio sotto soglia.§END§" : "§DIM§------ Rischio elevato: ventila o riduci umidità.§END§");

            // Giorni nello stadio già in PLANT STATUS (GIORNI STADIO X/Y); qui solo giorni ottimali e punti
            string optimalLine = $"{(f.OptimalDaysOk ? "✓" : "✗")} §DATA§Giorni ottimali§END§   {pot.DaysConsecutiveOptimal} | Richiesti: {f.RequiredOptimalDays}";
            AppendRawLine(f.OptimalDaysOk ? optimalLine : $"§ERROR§{optimalLine}§END§");
            AppendRawLine(f.OptimalDaysOk ? "§DIM§------ Giorni consecutivi ottimali sufficienti.§END§" : "§DIM§------ Servono più giorni con tutti i parametri in range.§END§");

            string pointsLine = $"{(f.PointsOk ? "✓" : "✗")} §DATA§Punti crescita§END§  W:{pot.GrowthPointsWater} L:{pot.GrowthPointsLight} F:{pot.GrowthPointsFertilizer} | Richiesti: {f.RequiredPoints}";
            AppendRawLine(f.PointsOk ? pointsLine : $"§ERROR§{pointsLine}§END§");
            AppendRawLine(f.PointsOk ? "§DIM§------ Punti W+L+F sufficienti.§END§" : "§DIM§------ Servono più punti (acqua, luce, fertilizzante in range).§END§");

            int unmet = 0;
            if (!conditionReqOk) unmet++;
            if (!f.HydrationOk) unmet++;
            if (!f.FertilizerOk) unmet++;
            if (!f.LedOk) unmet++;
            if (!moldOk) unmet++;
            if (!f.DurationOk) unmet++;
            if (!f.OptimalDaysOk) unmet++;
            if (!f.PointsOk) unmet++;

            if (f.BlocksAdvancement)
            {
                AppendRawLine("§ERROR§► STATUS: BLOCCATO§END§");
                if (f.BlockedByCondition)
                    AppendRawLine("§ERROR§  • Avanzamento bloccato: condizione critica o appassita§END§");
                if (f.BlockedByMold)
                    AppendRawLine("§ERROR§  • Avanzamento bloccato: rischio muffa grave (livello ≥2)§END§");
                // Altri requisiti mancanti (senza ripetere condizione/muffa già spiegati sopra)
                if (!f.HydrationOk) AppendRawLine("§ERROR§  • Idratazione fuori range§END§");
                if (!f.FertilizerOk) AppendRawLine("§ERROR§  • Fertilizzante fuori range§END§");
                if (!f.LedOk) AppendRawLine("§ERROR§  • Stress luce fuori range (20%-80%)§END§");
                if (!moldOk && !f.BlockedByMold) AppendRawLine("§ERROR§  • Rischio muffa elevato (richiesto <2)§END§");
                if (!f.DurationOk) AppendRawLine("§ERROR§  • Giorni nello stadio insufficienti§END§");
                if (!f.OptimalDaysOk) AppendRawLine("§ERROR§  • Giorni consecutivi ottimali insufficienti§END§");
                if (!f.PointsOk) AppendRawLine("§ERROR§  • Punti crescita insufficienti (W+L+F)§END§");
            }
            else if (unmet == 0)
            {
                AppendRawLine("§TITLE§► STATUS: PRONTO PER AVANZAMENTO§END§");
            }
            else
            {
                AppendRawLine($"§ERROR§► STATUS: {unmet} REQUISITO/I NON SODDISFATTI§END§");
                if (!conditionReqOk) AppendRawLine("§ERROR§  • Condizione critica o appassita (avanzamento bloccato)§END§");
                if (!f.HydrationOk) AppendRawLine("§ERROR§  • Idratazione fuori range§END§");
                if (!f.FertilizerOk) AppendRawLine("§ERROR§  • Fertilizzante fuori range§END§");
                if (!f.LedOk) AppendRawLine("§ERROR§  • Stress luce fuori range (20%-80%)§END§");
                if (!moldOk) AppendRawLine("§ERROR§  • Rischio muffa elevato (richiesto <2)§END§");
                if (!f.DurationOk) AppendRawLine("§ERROR§  • Giorni nello stadio insufficienti§END§");
                if (!f.OptimalDaysOk) AppendRawLine("§ERROR§  • Giorni consecutivi ottimali insufficienti§END§");
                if (!f.PointsOk) AppendRawLine("§ERROR§  • Punti crescita insufficienti (W+L+F)§END§");
            }
        }

        private void OpenDetail(string potId, bool diaryOnly)
        {
            var pot = FindPotById(potId);
            if (pot == null)
            {
                AppendRawLine("§ERROR§⚠ ERRORE: ID VASO NON TROVATO.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            var state = pot.PotActions != null ? pot.PotActions.PotState : null;
            if (state == null || state.IsEmpty || !state.HasPlant)
            {
                AppendRawLine($"§WARN§⚠ ATTENZIONE: {potId} È VUOTO. NESSUN DATO DA MOSTRARE.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            if (_detailView != null) _detailView.style.display = DisplayStyle.Flex;
            if (_consoleView != null) _consoleView.style.display = DisplayStyle.None;
            if (_protocolView != null) _protocolView.style.display = DisplayStyle.None;

            // Rimuovi placeholder e detail page precedente se esistono
            var placeholder = _detailView?.Q<Label>("pcv3-detail-placeholder");
            if (placeholder != null)
                placeholder.RemoveFromHierarchy();

            if (_currentDetailPage != null)
            {
                _currentDetailPage.RemoveFromHierarchy();
                _currentDetailPage = null;
            }

            // Carica template detail page
            if (_detailPageTemplate == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "PlantCardV3TerminalController: _detailPageTemplate non assegnato!");
                AppendRawLine("§ERROR§⚠ ERRORE: TEMPLATE PAGINA DETTAGLIO NON ASSEGNATO.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            var templateRoot = _detailPageTemplate.Instantiate();
            VisualElement detailPage = templateRoot;
            if (templateRoot.name != "pcv3-detail-page")
            {
                detailPage = templateRoot.Q<VisualElement>("pcv3-detail-page");
                if (detailPage == null)
                {
                    SporiumLogger.LogError(LogCategory.UI, "PlantCardV3TerminalController: Template non contiene pcv3-detail-page!");
                    AppendRawLine("§ERROR§⚠ ERRORE: TEMPLATE PAGINA DETTAGLIO NON VALIDO.§END§");
                    AppendRawLine("");
                    FlushConsole();
                    return;
                }
            }

            _currentDetailPage = detailPage;
            if (_detailView != null)
                _detailView.Add(detailPage);

            // Popola dati
            PopulateDetailPage(detailPage, pot, state, diaryOnly);

            AppendRawLine("§INFO§Digita CLOSE per tornare...§END§");
            AppendRawLine("");
            FlushConsole();
        }

        private void PopulateDetailPage(VisualElement detailPage, PotSlot pot, PotStateModel state, bool diaryOnly)
        {
            if (detailPage == null || state == null) return;

            var plantData = state.GetPlantData();
            bool hasCondition = TryGetCondition(state, plantData, out int conditionScore, out string conditionName);

            // Helper: 20-char ascii bar
            static void SetBar(Label filled, Label empty, int percent)
            {
                int p = Mathf.Clamp(percent, 0, 100);
                int filledCount = Mathf.Clamp(Mathf.RoundToInt(p / 100f * 20f), 0, 20);
                int emptyCount = 20 - filledCount;
                if (filled != null) filled.text = new string('█', filledCount);
                if (empty != null) empty.text = new string('░', emptyCount);
            }

            // Header / identity
            string familyCode = FormatPlantFamilyBadge(state.PlantCode);
            string plantName = GetPotDisplayName(state).ToUpperInvariant();
            string oneLiner = plantData != null && !string.IsNullOrWhiteSpace(plantData.Description) ? plantData.Description : "---";

            var specimen = detailPage.Q<Label>("pcv3d-specimen");
            if (specimen != null) specimen.text = $"SPECIMEN ID: {state.PotId}";

            var family = detailPage.Q<Label>("pcv3d-family");
            if (family != null) family.text = familyCode;

            var nameLabel = detailPage.Q<Label>("pcv3d-plant-name");
            if (nameLabel != null) nameLabel.text = plantName;

            var speciesLevel = detailPage.Q<Label>("pcv3d-species-level");
            if (speciesLevel != null) speciesLevel.text = $"{familyCode} LEVEL {state.PlantLevel}";

            var oneLinerLabel = detailPage.Q<Label>("pcv3d-one-liner");
            if (oneLinerLabel != null) oneLinerLabel.text = oneLiner;

            // LED pulsing
            var led = detailPage.Q<Label>("pcv3d-led");
            if (led != null)
            {
                bool on = true;
                led.schedule.Execute(() =>
                {
                    led.style.opacity = on ? 1f : 0.5f;
                    on = !on;
                }).Every(1000); // 2s loop (0.5->1->0.5)
            }

            // Status lines (rich text for colored values)
            string conditionValue = hasCondition ? conditionName.ToUpperInvariant() : "---";
            string stageValue = PlantCardFormatters.FormatGrowthStage((PlantStage)state.Stage).ToUpperInvariant();
            string phDriftValue = plantData != null ? PlantCardFormatters.FormatPhDrift(plantData.DailyPhDrift) : "---";
            int totalGrowth = state.GrowthPointsWater + state.GrowthPointsLight + state.GrowthPointsFertilizer;
            string growthValue = $"+{totalGrowth}/day";

            var conditionLine = detailPage.Q<Label>("pcv3d-condition-line");
            if (conditionLine != null)
            {
                conditionLine.enableRichText = true;
                conditionLine.text = $"CONDITION........: <color=#7FFF7A>{conditionValue}</color>";
            }

            var stageLine = detailPage.Q<Label>("pcv3d-stage-line");
            if (stageLine != null)
            {
                stageLine.enableRichText = true;
                stageLine.text = $"STAGE............: <color=#5DB6E3>{stageValue}</color>";
            }

            var phDriftLine = detailPage.Q<Label>("pcv3d-phdrift-line");
            if (phDriftLine != null)
            {
                phDriftLine.enableRichText = true;
                phDriftLine.text = $"pH DRIFT.........: <color={GetPhColorHexForDomePhBand()}>{phDriftValue}</color>";
            }

            var growthLine = detailPage.Q<Label>("pcv3d-growth-line");
            if (growthLine != null)
            {
                growthLine.enableRichText = true;
                growthLine.text = $"GROWTH...........: <color=#7FFF7A>{growthValue}</color>";
            }

            // Vitals (hydration + light stress)
            int hydrationPercent = _potSystemConfig != null
                ? PlantCardCalculators.CalculateHydrationPercent(state.Hydration, _potSystemConfig.MaxHydration)
                : 0;

            int lightStressPercent = 0;
            if (_potSystemConfig != null)
            {
                int consecutiveDays = state.GetConsecutiveLedDays();
                int maxDaysForFullStress = Mathf.Max(1, _potSystemConfig.MaxDaysForFullStress);
                lightStressPercent = Mathf.RoundToInt(Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f);
            }

            SetBar(detailPage.Q<Label>("pcv3d-hydration-filled"), detailPage.Q<Label>("pcv3d-hydration-empty"), hydrationPercent);
            var hydValue = detailPage.Q<Label>("pcv3d-hydration-value");
            if (hydValue != null) hydValue.text = $"{hydrationPercent}%";

            SetBar(detailPage.Q<Label>("pcv3d-lightstress-filled"), detailPage.Q<Label>("pcv3d-lightstress-empty"), lightStressPercent);
            var lsValue = detailPage.Q<Label>("pcv3d-lightstress-value");
            if (lsValue != null) lsValue.text = $"{lightStressPercent}%";

            // Switch filled color green/yellow depending on stress
            var lsFilled = detailPage.Q<Label>("pcv3d-lightstress-filled");
            if (lsFilled != null)
            {
                lsFilled.RemoveFromClassList("pcv3d-bar-filled-green");
                lsFilled.RemoveFromClassList("pcv3d-bar-filled-yellow");
                lsFilled.AddToClassList(lightStressPercent > 50 ? "pcv3d-bar-filled-yellow" : "pcv3d-bar-filled-green");
            }

            if (lsValue != null)
            {
                lsValue.RemoveFromClassList("pcv3d-value-green");
                lsValue.RemoveFromClassList("pcv3d-value-yellow");
                lsValue.AddToClassList(lightStressPercent > 50 ? "pcv3d-value-yellow" : "pcv3d-value-green");
            }

            // Close button in-card
            var closeBtn = detailPage.Q<Button>("pcv3d-close");
            if (closeBtn != null)
            {
                closeBtn.clicked += () =>
                {
                    SwitchToConsole();
                    FlushConsole();
                };
            }

            // Research Notes (DOS block)
            var researchTextLabel = detailPage.Q<Label>("pcv3d-research-text");
            if (researchTextLabel != null)
            {
                var researchLines = BuildResearchedNoteLines(state, plantData);
                string researchText = researchLines != null && researchLines.Count > 0
                    ? string.Join("\n", researchLines)
                    : "No research data available.";
                researchTextLabel.text = researchText;
            }

            // Pot Diary (DOS block)
            var diaryScroll = detailPage.Q<ScrollView>("pcv3d-diary-scroll");
            var diaryList = detailPage.Q<VisualElement>("pcv3d-diary-list");
            var diaryInput = detailPage.Q<TextField>("pcv3d-diary-input");
            var diaryLogButton = detailPage.Q<Button>("pcv3d-diary-log");

            void RenderDiary()
            {
                if (diaryList == null) return;
                diaryList.Clear();

                if (PlantDiaryManager.Instance == null)
                {
                    var emptyRow = new Label("Diary system not available.");
                    emptyRow.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                    diaryList.Add(emptyRow);
                    return;
                }

                var notes = PlantDiaryManager.Instance.GetNotes(state.PotId);
                if (notes == null || notes.Count <= 0)
                {
                    var emptyRow = new Label("No diary entries yet.");
                    emptyRow.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                    diaryList.Add(emptyRow);
                    return;
                }

                foreach (var note in notes.OrderByDescending(n => n.Day))
                {
                    var row = new VisualElement();
                    row.AddToClassList("pcv3d-diary-row");

                    var prefix = new Label("▸");
                    prefix.AddToClassList("pcv3d-diary-prefix");

                    var text = new Label(note.Text ?? "");
                    text.AddToClassList("pcv3d-diary-text");

                    row.Add(prefix);
                    row.Add(text);
                    diaryList.Add(row);
                }
            }

            void SubmitDiaryNote()
            {
                if (diaryInput == null) return;
                if (PlantDiaryManager.Instance == null) return;

                string raw = diaryInput.value ?? string.Empty;
                string text = raw.Trim();
                if (string.IsNullOrWhiteSpace(text)) return;

                int currentDay = _dayCycleSystem != null ? _dayCycleSystem.CurrentDay : 1;
                PlantDiaryManager.Instance.AddNote(state.PotId, new PlantDiaryManager.DiaryNote(currentDay, text));

                diaryInput.value = string.Empty;
                RenderDiary();

                // Keep focus for rapid note-taking
                diaryInput.Focus();

                // Scroll to top (latest first)
                if (diaryScroll != null)
                    diaryScroll.scrollOffset = Vector2.zero;
            }

            // Initial render
            RenderDiary();

            // Bind UI events (detail page is instantiated per-open, so no accumulation)
            if (diaryLogButton != null)
            {
                diaryLogButton.clicked -= SubmitDiaryNote;
                diaryLogButton.clicked += SubmitDiaryNote;
            }

            if (diaryInput != null)
            {
                diaryInput.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        SubmitDiaryNote();
                        evt.StopPropagation();
                    }
                });

                // UX: placeholder-like hint
                if (string.IsNullOrEmpty(diaryInput.value))
                    diaryInput.value = string.Empty;

                if (diaryOnly)
                    diaryInput.Focus();
            }
        }

        private void BeginConfirmQueueUproot(string potId)
        {
            var pot = FindPotById(potId);
            if (pot == null)
            {
                AppendRawLine("§ERROR§⚠ ERRORE: ID VASO NON TROVATO.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }
            var state = pot.PotActions != null ? pot.PotActions.PotState : null;
            if (state == null || state.IsEmpty || !state.HasPlant)
            {
                AppendRawLine($"§ERROR§⚠ ERRORE: {potId} È VUOTO. NIENTE DA ESTIRPARE.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            _pendingConfirmAction = new QueuedAction
            {
                Type = QueuedActionType.Uproot,
                PotId = potId,
                TargetLabel = GetPotDisplayName(state),
                ApCost = 1
            };

            _inputState = InputState.ConfirmingActionToQueue;
            AppendRawLine("§TITLE§▸ CONFERMA AZIONE§END§");
            AppendRawLine($"  Action:  §DATA§UPROOT§END§");
            AppendRawLine($"  Target:  §DATA§{potId}§END§");
            AppendRawLine($"  Plant:   §DATA§{_pendingConfirmAction.TargetLabel}§END§");
            AppendRawLine("  AP Cost: §VAL§1 AP§END§");
            AppendRawLine("§WHITE§Conferma? [§Y§Y§END§/§N§N§END§]§END§");
            AppendRawLine("");
            FlushConsole();
        }

        private void BeginConfirmToggleAction(QueuedActionType type, string potId)
        {
            var pot = FindPotById(potId);
            if (pot == null)
            {
                AppendRawLine("§ERROR§⚠ ERRORE: ID VASO NON TROVATO.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }
            var state = pot.PotActions != null ? pot.PotActions.PotState : null;

            if (type == QueuedActionType.HydrationToggle)
            {
                bool hasPlantNow = state != null && state.HasPlant;
                bool hasPlantQueued = _queue.Exists(a => a != null && a.Type == QueuedActionType.Plant && string.Equals(a.PotId, potId, StringComparison.OrdinalIgnoreCase));
                ResolveRuntimeDependencies();
                bool hasPlantPending = _automationRunner != null && _automationRunner.HasPlantPendingOrRunning(potId);

                if (!hasPlantNow && !hasPlantQueued && !hasPlantPending)
                {
                    AppendRawLine($"§ERROR§⚠ ERRORE: {potId} È VUOTO. PIANTA PRIMA.§END§");
                    AppendRawLine("");
                    FlushConsole();
                    return;
                }
            }

            if (type != QueuedActionType.Harvest && type != QueuedActionType.Uproot && type != QueuedActionType.HydrationToggle
                && (state == null || state.IsEmpty || !state.HasPlant))
            {
                AppendRawLine($"§ERROR§⚠ ERRORE: {potId} È VUOTO.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }
            if (type == QueuedActionType.Harvest && (state == null || state.IsEmpty || !state.HasPlant))
            {
                AppendRawLine($"§ERROR§⚠ ERRORE: {potId} È VUOTO. NIENTE DA RACCOGLIERE.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            _pendingConfirmAction = new QueuedAction
            {
                Type = type,
                PotId = potId,
                TargetLabel = state != null ? GetPotDisplayName(state) : "---",
                ApCost = 1
            };

            _inputState = InputState.ConfirmingActionToQueue;
            AppendRawLine("§TITLE§▸ CONFERMA AZIONE§END§");
            AppendRawLine($"  Action:  §DATA§{GetActionLabel(type)}§END§");
            AppendRawLine($"  Target:  §DATA§{potId}§END§");
            if (type == QueuedActionType.HydrationToggle)
            {
                bool isOn = pot.PotActions != null && pot.PotActions.IsWateringSystemOn();
                string status = isOn ? "ON" : "OFF";
                AppendRawLine($"  System:  §DATA§{status}§END§");
            }
            if (type == QueuedActionType.LedRedToggle || type == QueuedActionType.LedBlueToggle)
            {
                bool ledOn = pot.PotActions != null && pot.PotActions.IsLedSystemOn();
                string status = ledOn ? "ON" : "OFF";
                AppendRawLine($"  System:  §DATA§{status}§END§");
                var ledState = pot.PotActions != null ? pot.PotActions.GetLedSystemState() : LedSystemState.Off;
                string stateLabel = ledState.ToString().ToUpperInvariant();
                AppendRawLine($"  State:   §DATA§{stateLabel}§END§");
            }
            AppendRawLine("  AP Cost: §VAL§1 AP§END§");
            AppendRawLine("§WHITE§Conferma? [§Y§Y§END§/§N§N§END§]§END§");
            AppendRawLine("");
            FlushConsole();
        }

        private void BeginSelectItemForAction(QueuedActionType type, string potId)
        {
            var pot = FindPotById(potId);
            if (pot == null)
            {
                AppendRawLine("§ERROR§⚠ ERRORE: ID VASO NON TROVATO.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            var state = pot.PotActions != null ? pot.PotActions.PotState : null;
            bool empty = state == null || state.IsEmpty || !state.HasPlant;

            if (type == QueuedActionType.Plant && !empty)
            {
                AppendRawLine($"§ERROR§⚠ ERROR: {potId} IS ALREADY OCCUPIED. HARVEST FIRST.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }
            if ((type == QueuedActionType.Fertilize || type == QueuedActionType.Spray) && empty)
            {
                AppendRawLine($"§ERROR§⚠ ERRORE: {potId} È VUOTO. PIANTA PRIMA.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            if (_inventory == null)
            {
                AppendRawLine("§ERROR§⚠ ERROR: INVENTORY NOT FOUND§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            var options = GetInventoryOptions(type);
            if (options.Count == 0)
            {
                AppendRawLine("§ERROR§⚠ ERROR: NO ITEMS AVAILABLE IN INVENTORY§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            var potState = pot.PotActions != null ? pot.PotActions.PotState : null;
            var potPlantData = potState != null ? potState.GetPlantData() : null;

            _selection = new SelectionContext { Type = type, PotId = potId, OptionsTypeIds = options };
            _inputState = InputState.SelectingItem;

            AppendRawLine("§TITLE§▸ SELEZIONA OGGETTO DALL'INVENTARIO§END§");
            AppendRawLine("§TITLE§▸ AVAILABLE ITEMS§END§");
            for (int i = 0; i < options.Count; i++)
            {
                string typeId = options[i];
                int qty = GetAvailableQuantity(typeId);
                Item displayItem = GetAvailableItemForDisplay(typeId);
                string displayName = PlayerInventoryPanelController.GetItemDisplayName(typeId, displayItem);

                // C.2: SPRAY — mostra delta pH e direzione vs target pianta
                if (type == QueuedActionType.Spray && _phSystem != null && potPlantData != null)
                {
                    float delta = typeId == Items.AdditiveAcid ? -5f : 5f;
                    float resultPh = _phSystem.CurrentPh + delta;
                    float phMin = potPlantData.OptimalPhMin;
                    float phMax = potPlantData.OptimalPhMax;
                    bool wasInRange = _phSystem.CurrentPh >= phMin && _phSystem.CurrentPh <= phMax;
                    bool willBeInRange = resultPh >= phMin && resultPh <= phMax;
                    string sign = delta > 0 ? "+" : "";
                    string direction = willBeInRange ? "§DATA§→ verso target§END§" :
                                       wasInRange ? "§WARN§⚠ fuori target§END§" : "§WARN§lontano dal target§END§";
                    AppendRawLine($"  §CMD§{i + 1}.§END§ §DATA§{displayName}§END§   Δ pH: {sign}{delta:F0}  {direction}   Qtà: {qty}");
                }
                else
                {
                    AppendRawLine($"  §CMD§{i + 1}.§END§ §DATA§{displayName}§END§   Quantità: {qty}");
                }
            }
            AppendRawLine("§WHITE§Digita il numero dell'oggetto o §N§N§END§ per annullare§END§");
            AppendRawLine("");
            FlushConsole();
        }

        private void HandleStatusPotChoice(string upper)
        {
            _inputState = InputState.Idle;
            PotSlot chosen = null;
            if (int.TryParse(upper.Trim(), out int num) && num >= 1 && num <= _potsForStatusChoice.Count)
                chosen = _potsForStatusChoice[num - 1];
            else
                chosen = FindPotById(upper.Trim());
            if (chosen == null)
            {
                AppendRawLine("§ERROR§⚠ Vaso non valido. Digita un numero da 1 a " + _potsForStatusChoice.Count + " o l'ID vaso.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }
            var pots = FindPots();
            var allLines = new List<string>();
            _statusLinesCollector = allLines;
            PrintStatusSummaryTable(pots);
            PrintStatusDetailForPot(chosen);
            _statusLinesCollector = null;

            // RESEARCHED NOTE: da mostrare sempre in fondo (append dopo second half)
            var stateChosen = chosen.PotActions != null ? chosen.PotActions.PotState : null;
            var plantDataChosen = stateChosen != null ? stateChosen.GetPlantData() : null;
            _pendingStatusResearchNotes = new List<string>();
            _pendingStatusResearchNotes.Add("<color=#00AA00>────────────────────────────────────────────────────────────────────────────</color>");
            _pendingStatusResearchNotes.Add("§DATA§▸ RESEARCHED NOTE§END§");
            var statusResearch = BuildResearchedNoteLines(stateChosen, plantDataChosen);
            for (int i = 0; i < statusResearch.Count; i++)
                _pendingStatusResearchNotes.Add("§TITLE§" + statusResearch[i] + "§END§");
            _pendingStatusResearchNotes.Add("");

            int total = allLines.Count;
            int half = total / 2;
            var firstHalf = new List<string>();
            var secondHalf = new List<string>();
            for (int i = 0; i < total; i++)
            {
                if (i < half)
                    firstHalf.Add(allLines[i]);
                else
                    secondHalf.Add(allLines[i]);
            }
            _pendingStatusSecondHalf = secondHalf.Count > 0 ? secondHalf : null;
            if (firstHalf.Count > 0)
            {
                _typewriterActive = true;
                foreach (string raw in firstHalf)
                    _typewriterQueue.Enqueue(ParseColors(raw));
                FlushConsole();
            }
            else if (_pendingStatusSecondHalf != null && _pendingStatusSecondHalf.Count > 0)
                _statusSecondHalfRoutine = StartCoroutine(StatusSecondHalfRoutine());
            else
                AppendPendingStatusResearchNotes();
            SwitchToConsole();
        }

        private void AppendPendingStatusResearchNotes()
        {
            if (_pendingStatusResearchNotes == null) return;
            foreach (string line in _pendingStatusResearchNotes)
                _consoleBuffer.AppendLine(ParseColors(line));
            FlushConsoleImmediate();
            AutoScrollConsole();
            _consoleScroll?.schedule.Execute(() => AutoScrollConsole()).ExecuteLater(50);
            _consoleScroll?.schedule.Execute(() => AutoScrollConsole()).ExecuteLater(150);
            _pendingStatusResearchNotes = null;
        }

        private void HandleSelectingItem(string upper)
        {
            if (_selection == null)
            {
                _inputState = InputState.Idle;
                return;
            }

            if (upper == "N")
            {
                AppendRawLine("§WARN§⚠ OPERATION CANCELLED§END§");
                AppendRawLine("");
                _selection = null;
                _inputState = InputState.Idle;
                FlushConsole();
                return;
            }

            if (!int.TryParse(upper, out int idx))
            {
                AppendRawLine("§ERROR§⚠ SELEZIONE NON VALIDA. DIGITA UN NUMERO O N§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            idx -= 1;
            if (idx < 0 || idx >= _selection.OptionsTypeIds.Count)
            {
                AppendRawLine("§ERROR§⚠ SELEZIONE NON VALIDA.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            string chosen = _selection.OptionsTypeIds[idx];
            // Converti in nome leggibile per la visualizzazione usando l'item reale (supporta nome custom seed).
            Item chosenItem = GetAvailableItemForDisplay(chosen);
            string chosenDisplayName = PlayerInventoryPanelController.GetItemDisplayName(chosen, chosenItem);

            _pendingConfirmAction = new QueuedAction
            {
                Type = _selection.Type,
                PotId = _selection.PotId,
                TargetLabel = chosenDisplayName,
                ApCost = 1,
                ItemTypeId = chosen
            };

            _selection = null;

            // C.3: FERTILIZE incompatibile — doppia conferma prima di procedere
            if (_pendingConfirmAction.Type == QueuedActionType.Fertilize)
            {
                var fertPot = FindPotById(_pendingConfirmAction.PotId);
                var fertState = fertPot?.PotActions?.PotState;
                var fertPlantData = fertState?.GetPlantData();
                if (fertPlantData != null)
                {
                    FertilizerType fertType = chosen switch
                    {
                        "fertilizer-pure"       => FertilizerType.Pure,
                        "fertilizer-prohibited" => FertilizerType.Prohibited,
                        _                       => FertilizerType.Standard,
                    };
                    PlantFamily family = fertPlantData.Family;
                    if (!FertilizerSystem.IsFertilizerCompatible(fertType, family))
                    {
                        _inputState = InputState.ConfirmingCriticalFertilize;
                        string compatDesc = FertilizerSystem.GetCompatibilityDescription(fertType, family);
                        WritePlantFlowBlock(() =>
                        {
                            AppendRawLine("§ERROR§⚠ PERICOLO CRITICO — FERTILIZZANTE INCOMPATIBILE§END§");
                            AppendRawLine($"§ERROR§  {compatDesc}§END§");
                            AppendRawLine($"§ERROR§  Famiglia pianta: {family}   Fertilizzante: {fertType}§END§");
                            AppendRawLine("§WARN§  Esito atteso: MORTE IMMEDIATA DELLA PIANTA§END§");
                            AppendRawLine("§WHITE§Confermi di voler procedere comunque? [§Y§Y§END§/§N§N§END§]§END§");
                            AppendRawLine("");
                        });
                        return;
                    }
                }
            }

            if (_pendingConfirmAction.Type == QueuedActionType.Plant)
            {
                _pendingPlantDrip = false;
                _pendingPlantLed = 0;
                _inputState = InputState.ConfirmingPlantDrip;
                WritePlantFlowBlock(() =>
                {
                    AppendRawLine($"§TITLE§✓ SELEZIONATO: {chosenDisplayName}§END§");
                    AppendRawLine("§TITLE§▸ IMPIANTO IDRATAZIONE A GOCCIA§END§");
                    AppendRawLine("§WHITE§Desidera attivare l'impianto di Idratazione a Goccia per questo vaso?§END§");
                    AppendRawLine("§WHITE§Attivato in questo step non costa un'azione aggiuntiva, ma avrà consumi di condensa e CRY previsti a fine giornata.§END§");
                    AppendRawLine("§WHITE§[§Y§Y§END§/§N§N§END§]§END§");
                    AppendRawLine("");
                });
                return;
            }

            _inputState = InputState.ConfirmingActionToQueue;

            WritePlantFlowBlock(() =>
            {
                AppendRawLine($"§TITLE§✓ SELEZIONATO: {chosenDisplayName}§END§");
                AppendRawLine("§TITLE§▸ CONFERMA AZIONE§END§");
                AppendRawLine($"  Action:  §DATA§{GetActionLabel(_pendingConfirmAction.Type)}§END§");
                AppendRawLine($"  Target:  §DATA§{_pendingConfirmAction.PotId}§END§");
                AppendRawLine($"  Item:    §DATA§{chosenDisplayName}§END§");
                AppendRawLine("  AP Cost: §VAL§1 AP§END§");
                AppendRawLine("§WHITE§Conferma? [§Y§Y§END§/§N§N§END§]§END§");
                AppendRawLine("");
            });
        }

        private List<string> GetInventoryOptions(QueuedActionType type)
        {
            var list = new List<string>();

            if (type == QueuedActionType.Plant)
            {
                var pdb = PlantDatabase.Instance;
                if (pdb != null)
                {
                    foreach (var seedTid in pdb.GetRegisteredSeedTypeIds())
                    {
                        if (GetAvailableQuantity(seedTid) > 0)
                            list.Add(seedTid);
                    }
                }
                else
                {
                    foreach (var tid in new[] { Items.Seed001, Items.Seed002, Items.Seed003 })
                    {
                        if (GetAvailableQuantity(tid) > 0)
                            list.Add(tid);
                    }
                }
                return list;
            }
            if (type == QueuedActionType.Fertilize)
            {
                if (GetAvailableQuantity(Items.FertilizerStandard) > 0) list.Add(Items.FertilizerStandard);
                if (GetAvailableQuantity(Items.FertilizerPure) > 0) list.Add(Items.FertilizerPure);
                if (GetAvailableQuantity(Items.FertilizerProhibited) > 0) list.Add(Items.FertilizerProhibited);
                return list;
            }
            if (type == QueuedActionType.Spray)
            {
                if (GetAvailableQuantity(Items.AdditiveBasic) > 0) list.Add(Items.AdditiveBasic);
                if (GetAvailableQuantity(Items.AdditiveAcid) > 0) list.Add(Items.AdditiveAcid);
                return list;
            }

            return list;
        }

        private int GetQuantity(string typeId)
        {
            if (_inventory == null || string.IsNullOrEmpty(typeId)) return 0;
            foreach (var slot in _inventory.Items)
            {
                if (slot != null && slot.TypeId == typeId)
                    return slot.Quantity;
            }
            return 0;
        }

        private int GetReservedQuantity(string typeId)
        {
            if (string.IsNullOrEmpty(typeId)) return 0;
            return _reservedItems.TryGetValue(typeId, out int count) ? count : 0;
        }

        private int GetAvailableQuantity(string typeId)
        {
            int total = GetQuantity(typeId);
            int reserved = GetReservedQuantity(typeId);
            return Mathf.Max(0, total - reserved);
        }

        private Item GetAvailableItemForDisplay(string typeId)
        {
            if (_inventory == null || string.IsNullOrEmpty(typeId))
                return null;

            int reserved = GetReservedQuantity(typeId);
            foreach (var slot in _inventory.Items)
            {
                if (slot == null || slot.TypeId != typeId)
                    continue;

                if (slot.Items == null || slot.Items.Count <= 0)
                    return null;

                return slot.Items.Skip(reserved).FirstOrDefault() ?? slot.Items.FirstOrDefault();
            }

            return null;
        }

        private void RebuildReservedItems()
        {
            _reservedItems.Clear();
            foreach (var action in _queue)
            {
                if (action == null || string.IsNullOrEmpty(action.ItemTypeId)) continue;
                _reservedItems.TryGetValue(action.ItemTypeId, out int cur);
                _reservedItems[action.ItemTypeId] = cur + 1;
            }
        }

        // C.3: Prima conferma per fertilizzante incompatibile — se Y, chiede seconda conferma definitiva.
        private void HandleConfirmCriticalFertilize(string upper)
        {
            if (upper == "Y")
            {
                _inputState = InputState.ConfirmingActionToQueue;
                WritePlantFlowBlock(() =>
                {
                    AppendRawLine("§ERROR§⚠ CONFERMA DEFINITIVA RICHIESTA§END§");
                    AppendRawLine("§ERROR§  Questa azione UCCIDERÀ la pianta in modo irreversibile.§END§");
                    AppendRawLine("§WARN§  Per continuare digita Y un'altra volta. N per annullare.§END§");
                    AppendRawLine("§WHITE§[§Y§Y§END§/§N§N§END§]§END§");
                    AppendRawLine("");
                });
            }
            else
            {
                _pendingConfirmAction = null;
                _inputState = InputState.Idle;
                AppendRawLine("§WARN§⚠ Operazione annullata. Pianta al sicuro.§END§");
                AppendRawLine("");
                FlushConsole();
            }
        }

        private void HandlePlantDripChoice(string upper)
        {
            if (upper == "Y" || upper == "YES") { _pendingPlantDrip = true; }
            else if (upper == "N" || upper == "NO") { _pendingPlantDrip = false; }
            else
            {
                WritePlantFlowBlock(() =>
                {
                    AppendRawLine("§ERROR§⚠ INPUT NON VALIDO. DIGITA §Y§Y§END§ O §N§N§END§");
                    AppendRawLine("");
                });
                return;
            }
            _inputState = InputState.ConfirmingPlantLed;
            WritePlantFlowBlock(() =>
            {
                AppendRawLine("§TITLE§▸ ACCENSIONE LED§END§");
                AppendRawLine("§WHITE§Vuole accendere il LED per questo vaso? Attivato in questo step non costa un'azione aggiuntiva, ma avrà consumi di condensa e CRY previsti a fine giornata.§END§");
                AppendRawLine("§WHITE§[§Y§Y§END§/§N§N§END§]§END§");
                AppendRawLine("");
            });
        }

        private void HandlePlantLedChoice(string upper)
        {
            if (upper == "Y" || upper == "YES")
            {
                _inputState = InputState.ConfirmingPlantLedType;
                WritePlantFlowBlock(() =>
                {
                    AppendRawLine("§TITLE§▸ TIPO LED§END§");
                    AppendRawLine("§WHITE§Quale? §BLUE§BLUE§END§ o §RED§RED§END§?§END§");
                    AppendRawLine("");
                });
                return;
            }
            if (upper == "N" || upper == "NO") { _pendingPlantLed = 0; }
            else
            {
                WritePlantFlowBlock(() =>
                {
                    AppendRawLine("§ERROR§⚠ INPUT NON VALIDO. DIGITA §Y§Y§END§ O §N§N§END§");
                    AppendRawLine("");
                });
                return;
            }
            ShowPlantFinalConfirm();
        }

        private void HandlePlantLedTypeChoice(string upper)
        {
            if (upper == "RED") { _pendingPlantLed = 1; }
            else if (upper == "BLUE") { _pendingPlantLed = 2; }
            else
            {
                WritePlantFlowBlock(() =>
                {
                    AppendRawLine("§ERROR§⚠ INPUT NON VALIDO. DIGITA §CMD§RED§END§ O §CMD§BLUE§END§");
                    AppendRawLine("");
                });
                return;
            }
            ShowPlantFinalConfirm();
        }

        private void ShowPlantFinalConfirm()
        {
            _inputState = InputState.ConfirmingActionToQueue;
            WritePlantFlowBlock(() =>
            {
                AppendRawLine("§TITLE§▸ CONFERMA AZIONE§END§");
                AppendRawLine($"  Action:  §DATA§{GetActionLabel(_pendingConfirmAction.Type)}§END§");
                AppendRawLine($"  Target:  §DATA§{_pendingConfirmAction.PotId}§END§");
                AppendRawLine($"  Item:    §DATA§{_pendingConfirmAction.TargetLabel}§END§");
                if (_pendingPlantDrip) AppendRawLine("  §INFO§+ Idratazione a goccia (attivata senza AP aggiuntivo)§END§");
                if (_pendingPlantLed == 1) AppendRawLine("  §INFO§+ LED Rosso (attivato senza AP aggiuntivo)§END§");
                if (_pendingPlantLed == 2) AppendRawLine("  §INFO§+ LED Blu (attivato senza AP aggiuntivo)§END§");
                AppendRawLine("  AP Cost: §VAL§1 AP§END§");
                AppendRawLine("§WHITE§Conferma? [§Y§Y§END§/§N§N§END§]§END§");
                AppendRawLine("");
            });
        }

        private void HandleConfirmToQueue(string upper)
        {
            if (upper == "Y" || upper == "YES")
            {
                if (_pendingConfirmAction != null)
                {
                    _queue.Add(_pendingConfirmAction);
                    if (_pendingConfirmAction.Type == QueuedActionType.Plant)
                    {
                        string potId = _pendingConfirmAction.PotId;
                        if (_pendingPlantDrip)
                        {
                            _queue.Add(new QueuedAction { Type = QueuedActionType.HydrationToggle, PotId = potId, TargetLabel = potId, ApCost = 0, ItemTypeId = null });
                            AppendRawLine($"§INFO§+ WATERING (idratazione a goccia) on {potId} [0 AP - incluso in PLANT]§END§");
                        }
                        if (_pendingPlantLed == 1)
                        {
                            _queue.Add(new QueuedAction { Type = QueuedActionType.LedRedToggle, PotId = potId, TargetLabel = potId, ApCost = 0, ItemTypeId = null });
                            AppendRawLine($"§INFO§+ LED RED on {potId} [0 AP - incluso in PLANT]§END§");
                        }
                        if (_pendingPlantLed == 2)
                        {
                            _queue.Add(new QueuedAction { Type = QueuedActionType.LedBlueToggle, PotId = potId, TargetLabel = potId, ApCost = 0, ItemTypeId = null });
                            AppendRawLine($"§INFO§+ LED BLUE on {potId} [0 AP - incluso in PLANT]§END§");
                        }
                        _pendingPlantDrip = false;
                        _pendingPlantLed = 0;
                    }
                    RebuildReservedItems();
                    AppendRawLine("§TITLE§✓ AZIONE AGGIUNTA ALLA CODA§END§");
                    AppendRawLine($"§INFO§+ {GetActionLabel(_pendingConfirmAction.Type)} on {_pendingConfirmAction.PotId} [1 AP]§END§");
                    AppendRawLine("");
                }
                _pendingConfirmAction = null;
                _inputState = InputState.Idle;
                FlushConsole();
                RefreshHeader();
                return;
            }
            if (upper == "N" || upper == "NO")
            {
                AppendRawLine("§WARN§⚠ AZIONE ANNULLATA§END§");
                AppendRawLine("");
                _pendingConfirmAction = null;
                _pendingPlantDrip = false;
                _pendingPlantLed = 0;
                _inputState = InputState.Idle;
                FlushConsole();
                return;
            }

            AppendRawLine("§ERROR§⚠ INPUT NON VALIDO. DIGITA §Y§Y§END§ O §N§N§END§§END§");
            AppendRawLine("");
            FlushConsole();
        }

        private void HandleConfirmExecuteOrDiscard(string upper)
        {
            if (upper == "Y" || upper == "YES")
            {
                AppendRawLine("§TITLE§✓ CHIUSURA TERMINALE§END§");
                AppendRawLine("§INFO§Restituzione coda azioni per conferma sequenza...§END§");
                AppendRawLine("");
                FlushConsole();
                _inputState = InputState.Idle;
                Close();
                // IMPORTANT: start automation only after closing the terminal, so Notifications Foundation
                // is visible and can show toasts (in-progress / error / success).
                TryStartAutomationRunner();
                return;
            }
            if (upper == "N" || upper == "NO")
            {
                AppendRawLine("§WARN§⚠ CODA ANNULLATA§END§");
                AppendRawLine("");
                FlushConsole();
                _queue.Clear();
                RebuildReservedItems();
                _inputState = InputState.Idle;
                Close();
                return;
            }

            AppendRawLine("§ERROR§⚠ INPUT NON VALIDO. DIGITA §Y§Y§END§ O §N§N§END§§END§");
            AppendRawLine("");
            FlushConsole();
        }

        private void TryStartAutomationRunner()
        {
            // Se c'è un runner in scena, passiamo la coda e confermiamo spendendo AP.
            ResolveRuntimeDependencies();
            var runner = _automationRunner;
            if (runner == null)
            {
                // Fallback: niente runner, quindi discard per non creare incoerenze (AP non spesi).
                AppendRawLine("§WARN§⚠ Automation runner not found in scene. Queue discarded.§END§");
                AppendRawLine("");
                _queue.Clear();
                RebuildReservedItems();
                FlushConsole();
                return;
            }

            // Pre-check item requirements (non consumiamo finché non è tutto ok)
            var required = new Dictionary<string, int>();
            foreach (var a in _queue)
            {
                if (a == null) continue;
                if (!string.IsNullOrEmpty(a.ItemTypeId))
                {
                    required.TryGetValue(a.ItemTypeId, out int cur);
                    required[a.ItemTypeId] = cur + 1;
                }
            }
            foreach (var kv in required)
            {
                if (_inventory == null || !_inventory.Has(kv.Key, kv.Value))
                {
                    string msg = NotificationLocalization.Pick(
                        $"Oggetto insufficiente {kv.Key} x{kv.Value} per le azioni in coda",
                        $"Insufficient item {kv.Key} x{kv.Value} for queued actions");
                    AppendRawLine($"§ERROR§⚠ ERROR: INSUFFICIENT ITEM {kv.Key} x{kv.Value}§END§");
                    AppendRawLine("");
                    FlushConsole();
                    _foundation?.PostToast("POT-AUTO-ERROR", new NotificationPayload().With("message", msg));
                    return;
                }
            }

            var batch = new System.Collections.Generic.List<Sporae.Dome.PotAutomation.PotAutomationRunner.AutomationAction>();
            var consumedItems = new List<Item>();
            foreach (var a in _queue)
            {
                if (a == null) continue;

                var mapped = new Sporae.Dome.PotAutomation.PotAutomationRunner.AutomationAction
                {
                    PotId = a.PotId,
                    ApCost = a.ApCost,
                    ItemTypeId = a.ItemTypeId
                };

                switch (a.Type)
                {
                    case QueuedActionType.Plant:
                        mapped.Type = Sporae.Dome.PotAutomation.PotAutomationRunner.AutomationActionType.Plant;
                        break;
                    case QueuedActionType.Fertilize:
                        mapped.Type = Sporae.Dome.PotAutomation.PotAutomationRunner.AutomationActionType.Fertilize;
                        break;
                    case QueuedActionType.Spray:
                        mapped.Type = Sporae.Dome.PotAutomation.PotAutomationRunner.AutomationActionType.Spray;
                        break;
                    case QueuedActionType.HydrationToggle:
                        mapped.Type = Sporae.Dome.PotAutomation.PotAutomationRunner.AutomationActionType.HydrationToggle;
                        break;
                    case QueuedActionType.LedRedToggle:
                        mapped.Type = Sporae.Dome.PotAutomation.PotAutomationRunner.AutomationActionType.LedRedToggle;
                        break;
                    case QueuedActionType.LedBlueToggle:
                        mapped.Type = Sporae.Dome.PotAutomation.PotAutomationRunner.AutomationActionType.LedBlueToggle;
                        break;
                    case QueuedActionType.Prune:
                        mapped.Type = Sporae.Dome.PotAutomation.PotAutomationRunner.AutomationActionType.Prune;
                        break;
                    case QueuedActionType.Harvest:
                        mapped.Type = Sporae.Dome.PotAutomation.PotAutomationRunner.AutomationActionType.Harvest;
                        break;
                    case QueuedActionType.Uproot:
                        mapped.Type = Sporae.Dome.PotAutomation.PotAutomationRunner.AutomationActionType.Uproot;
                        break;
                    default:
                        continue;
                }

                if (!string.IsNullOrEmpty(a.ItemTypeId))
                {
                    if (_inventory == null || !_inventory.TryRemoveFirst(a.ItemTypeId, out var removedItem) || removedItem == null)
                    {
                        foreach (var consumedItem in consumedItems)
                        {
                            _inventory?.Add(consumedItem);
                        }

                        AppendRawLine($"§ERROR§⚠ ERROR: IMPOSSIBLE TO RESOLVE ITEM PAYLOAD {a.ItemTypeId}§END§");
                        AppendRawLine("");
                        FlushConsole();
                        _foundation?.PostToast("POT-AUTO-ERROR", new NotificationPayload().With("message",
                            NotificationLocalization.Pick(
                                $"Payload oggetto mancante per {a.ItemTypeId}",
                                $"Item payload missing for {a.ItemTypeId}")));
                        return;
                    }

                    consumedItems.Add(removedItem);
                    if (a.Type == QueuedActionType.Plant)
                    {
                        mapped.ItemPayload = removedItem;
                    }
                }

                batch.Add(mapped);
            }

            bool ok = runner.EnqueueAndRun(batch);
            if (!ok)
            {
                foreach (var consumedItem in consumedItems)
                {
                    _inventory?.Add(consumedItem);
                }
                _foundation?.PostToast("POT-AUTO-ERROR", new NotificationPayload().With("message",
                    NotificationLocalization.Pick(
                        "Automazione non riuscita: punti azione insufficienti",
                        "Automation failed: insufficient AP")));
                AppendRawLine("§ERROR§⚠ Automation could not start (insufficient AP).§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            _queue.Clear();
            RebuildReservedItems();
            RefreshHeader();
        }

        private static string GetActionLabel(QueuedActionType type)
        {
            return NotificationLocalization.Pick(
                type switch
                {
                    QueuedActionType.Plant => "SEMINA",
                    QueuedActionType.Fertilize => "FERTILIZZAZIONE",
                    QueuedActionType.Spray => "SPRAY",
                    QueuedActionType.HydrationToggle => "IMPIANTO A GOCCIA",
                    QueuedActionType.LedRedToggle => "LED DI CRESCITA ROSSO",
                    QueuedActionType.LedBlueToggle => "LED DI CRESCITA BLU",
                    QueuedActionType.Prune => "POTATURA",
                    QueuedActionType.Harvest => "RACCOLTA",
                    QueuedActionType.Uproot => "SRADICAMENTO",
                    _ => type.ToString().ToUpperInvariant()
                },
                type switch
                {
                    QueuedActionType.Plant => "PLANT",
                    QueuedActionType.Fertilize => "FERTILIZE",
                    QueuedActionType.Spray => "SPRAY",
                    QueuedActionType.HydrationToggle => "DRIP IRRIGATION",
                    QueuedActionType.LedRedToggle => "RED GROW LED",
                    QueuedActionType.LedBlueToggle => "BLUE GROW LED",
                    QueuedActionType.Prune => "PRUNE",
                    QueuedActionType.Harvest => "HARVEST",
                    QueuedActionType.Uproot => "UPROOT",
                    _ => type.ToString().ToUpperInvariant()
                });
        }

        private static string ExtractPotIdArgument(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var parts = raw.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return null;
            return parts[1].Trim().ToUpperInvariant();
        }

        /// <summary>Estrae il token all'indice <paramref name="index"/> (0-based) dalla stringa splittata per spazi.
        /// Utile per comandi multi-parola come "CRYO SEND POT-001" (indice 2) o "CRYO RESTORE CRYO-01 POT-001".</summary>
        private static string ExtractArgAtIndex(string raw, int index)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var parts = raw.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length <= index) return null;
            return parts[index].Trim().ToUpperInvariant();
        }

        private static string PlantStageLabel(int stageInt)
        {
            try
            {
                var stage = (PlantStage)stageInt;
                return stage switch
                {
                    PlantStage.Seed => "SEME",
                    PlantStage.Sprout => "GERMOGLIO",
                    PlantStage.Growth => "ADULTA",
                    PlantStage.Flowering => "FIORE",
                    PlantStage.HarvestReady => "MATURO",
                    PlantStage.Resting => "RIPOSO",
                    _ => stage.ToString().ToUpperInvariant()
                };
            }
            catch
            {
                return $"STADIO {stageInt}";
            }
        }

        /// <summary>
        /// Nome visualizzabile della pianta: da PlantData se disponibile (come V2), altrimenti dal codice.
        /// </summary>
        private static string GetPlantDisplayName(PlantData plantData, string plantCode)
        {
            if (string.IsNullOrEmpty(plantCode)) return "---";
            if (plantData != null)
            {
                string plantName = plantData.name.Replace("PLT-", "").Replace("-", " ");
                switch (plantData.PlantCode)
                {
                    case "PLT-STD-001": return "Ferric Fern";
                    case "PLT-PURE-001": return "Arctic Hask";
                    case "PLT-EVIL-001": return "Glasscap Fungus";
                    default: return plantName;
                }
            }
            return plantCode.Replace("PLT-", "").Replace("-", " ");
        }

        private static string GetPlantDisplayName(string plantCode)
        {
            return GetPlantDisplayName(null, plantCode);
        }

        private static string GetPotDisplayName(PotStateModel state, PlantData plantData = null)
        {
            if (state == null) return "---";
            if (!string.IsNullOrWhiteSpace(state.CustomPlantName))
                return state.CustomPlantName;
            return GetPlantDisplayName(plantData, state.PlantCode);
        }

        private static List<string> BuildResearchedNoteLines(PotStateModel state, PlantData plantData)
        {
            var lines = new List<string>();
            if (state != null && LabHybridGameplayModifiers.PotHasLabHybridProfile(state))
            {
                lines.Add($"Specimen ibrido: {GetPotDisplayName(state, plantData)}");
                string lineage = string.IsNullOrWhiteSpace(state.SourcePlantCodesMetadata)
                    ? "—"
                    : state.SourcePlantCodesMetadata.Replace("|", " × ");
                lines.Add($"Lineage: {lineage}");
                if (!string.IsNullOrWhiteSpace(state.ActivePowerLabel))
                    lines.Add($"Attivo ereditato: {state.ActivePowerLabel}");
                if (!string.IsNullOrWhiteSpace(state.PassivePowerLabel))
                    lines.Add($"Passivo ereditato: {state.PassivePowerLabel}");
                if (!string.IsNullOrWhiteSpace(state.SelectedTraitsCsv))
                    lines.Add($"Tag gameplay: {state.SelectedTraitsCsv}");
                if (!string.IsNullOrWhiteSpace(state.LabCareProfileMetadata))
                    lines.Add($"Profilo cure: {state.LabCareProfileMetadata}");
                return lines;
            }

            if (state.IsMutated || state.TraitPowerPercent != 100 || !string.IsNullOrWhiteSpace(state.SelectedTraitsCsv) ||
                state.PlantGeneticType != GeneticType.Stable)
            {
                if (state.IsMutated)
                    lines.Add("Mutazione spontanea: effetti non garantiti al 100% — confronta tag in STATUS con il catalogo.");
                lines.Add($"Profilo runtime: {state.PlantGeneticType}, TraitPower {state.TraitPowerPercent}%");
                if (!string.IsNullOrWhiteSpace(state.SelectedTraitsCsv))
                    lines.Add($"Tag gameplay: {state.SelectedTraitsCsv}");
                lines.Add("—");
            }

            string researchNotes = plantData != null && !string.IsNullOrWhiteSpace(plantData.ResearchNotes) ? plantData.ResearchNotes : null;
            if (!string.IsNullOrEmpty(researchNotes))
            {
                lines.AddRange(researchNotes.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None));
                return lines;
            }

            lines.Add("—");
            return lines;
        }

        /// <summary>
        /// Converte ItemTypeId in nome leggibile. Se è un seed, usa il nome della pianta.
        /// </summary>
        private static string GetItemDisplayName(string itemTypeId)
        {
            if (string.IsNullOrEmpty(itemTypeId))
                return itemTypeId;
            return PlayerInventoryPanelController.GetItemDisplayName(itemTypeId, null);
        }

        private static string FormatPlantFamilyBadge(string plantCode)
        {
            if (string.IsNullOrEmpty(plantCode)) return "---";
            return plantCode.StartsWith("PLT-", StringComparison.OrdinalIgnoreCase)
                ? plantCode.Substring(4)
                : plantCode;
        }

        private static PlantFamily GetPlantFamilyForDisplay(PlantData plantData, PotStateModel state)
        {
            if (plantData != null)
                return plantData.Family;

            string familyMetadata = state != null ? state.PlantFamilyMetadata : null;
            if (!string.IsNullOrWhiteSpace(familyMetadata))
            {
                if (familyMetadata.Equals("PURE", StringComparison.OrdinalIgnoreCase))
                    return PlantFamily.Pure;
                if (familyMetadata.Equals("EVIL", StringComparison.OrdinalIgnoreCase))
                    return PlantFamily.Evil;
            }

            return PlantFamily.Standard;
        }

        private static string GetPlantFamilyLabel(PlantData plantData, PotStateModel state)
        {
            return GetPlantFamilyForDisplay(plantData, state).ToString().ToUpperInvariant();
        }

        private static Color GetFamilyColor(PlantFamily family)
        {
            return family switch
            {
                PlantFamily.Pure => new Color(0.498f, 1f, 0.478f, 1f),
                PlantFamily.Evil => new Color(0.827f, 0.373f, 0.373f, 1f),
                PlantFamily.Standard => new Color(0.902f, 0.788f, 0.435f, 1f),
                _ => new Color(0.72f, 0.72f, 0.72f, 1f)
            };
        }

        private bool TryGetCondition(PotStateModel state, PlantData plantData, out int score, out string conditionName)
        {
            score = 0;
            conditionName = "---";

            if (state == null || !state.HasPlant || plantData == null || _phSystem == null || _potSystemConfig == null)
                return false;

            int currentDay = _dayCycleSystem?.CurrentDay ?? 1;
            int previousDayScore = state.PreviousDayConditionScore >= 0 ? state.PreviousDayConditionScore : state.ConditionScore;
            ConditionResult result = PlantConditionSystem.CalculateCondition(
                state,
                plantData,
                _phSystem,
                _potSystemConfig,
                currentDay,
                previousDayScore
            );

            bool isOverwatering = PlantConditionSystem.IsOverwatering(state, _potSystemConfig.MaxHydration);
            conditionName = PlantConditionSystem.GetConditionName(result.Condition, isOverwatering);
            score = result.Score;
            return true;
        }

        private static System.Collections.Generic.List<PotSlot> FindPots()
        {
            var registry = ServiceContainer.Instance?.Get<DomePotRegistry>(suppressWarning: true);
            var pots = registry != null
                ? registry.GetPotsSnapshot()
                : new System.Collections.Generic.List<PotSlot>(FindObjectsOfType<PotSlot>());
            pots.Sort((a, b) => string.Compare(a != null ? a.PotId : "", b != null ? b.PotId : "", StringComparison.Ordinal));
            return pots;
        }

        private static PotSlot FindPotById(string potId)
        {
            if (string.IsNullOrEmpty(potId)) return null;
            var registry = ServiceContainer.Instance?.Get<DomePotRegistry>(suppressWarning: true);
            if (registry != null)
            {
                var registryPot = registry.FindPotById(potId);
                if (registryPot != null)
                    return registryPot;
            }

            var all = FindObjectsOfType<PotSlot>();
            foreach (var p in all)
            {
                if (p == null) continue;

                // Accept both PotSlot.PotId and PotStateModel.PotId (they can differ in formatting, e.g. "POT-001" vs "Pot001").
                if (string.Equals(p.PotId, potId, StringComparison.OrdinalIgnoreCase))
                    return p;

                var statePotId = p.PotActions != null && p.PotActions.PotState != null ? p.PotActions.PotState.PotId : null;
                if (!string.IsNullOrEmpty(statePotId) && string.Equals(statePotId, potId, StringComparison.OrdinalIgnoreCase))
                    return p;

                // Fallback: normalized compare (strip non-alphanum, uppercase) to handle "POT-001" == "Pot001".
                string a = NormalizePotIdForCompare(p.PotId);
                string b = NormalizePotIdForCompare(statePotId);
                string q = NormalizePotIdForCompare(potId);
                if (!string.IsNullOrEmpty(q) && (q == a || q == b))
                    return p;
            }
            return null;
        }

        private static string NormalizePotIdForCompare(string id)
        {
            if (string.IsNullOrEmpty(id)) return string.Empty;
            var sb = new StringBuilder(id.Length);
            foreach (char c in id)
            {
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToUpperInvariant(c));
            }
            return sb.ToString();
        }

        private void ShowProtocolFromDocs()
        {
            // Mostra il protocollo nella console (come STATUS), senza cambiare vista
            AppendRawLine("§CMD§PROTOCOL§END§");
            AppendRawLine("");

            string protocol = TryLoadProtocolFromProjectDocs();
            string[] lines = (protocol ?? string.Empty).Replace("\r\n", "\n").Split('\n');
            foreach (string line in lines)
                AppendRawLine(line);

            AppendRawLine("");
            FlushConsole();
        }

        private string TryLoadProtocolFromProjectDocs()
        {
            // Carichiamo il file richiesto dal prompt:
            // Assets/_Project/Docs/Protocollo_Pot terminale.txt
            // Runtime note: in build potrebbe non essere presente come file. In quel caso mostriamo fallback.
            try
            {
                string projectPath = Directory.GetParent(Application.dataPath)?.FullName;
                if (string.IsNullOrEmpty(projectPath))
                    return ParseColors("§ERROR§⚠ ERROR: PROJECT PATH NOT FOUND§END§");

                string rel = Path.Combine("Assets", "_Project", "Docs", "Protocollo_Pot terminale.txt");
                string full = Path.Combine(projectPath, rel);

                if (!File.Exists(full))
                    return ParseColors("§WARN§⚠ PROTOCOL FILE NOT FOUND (Editor path).§END§\n\n§INFO§Expected: Assets/_Project/Docs/Protocollo_Pot terminale.txt§END§");

                string text = File.ReadAllText(full);
                // Il file è già in stile box ASCII: lo renderizziamo come testo “raw”.
                // Applichiamo parseColors solo se in futuro si aggiungono tag.
                return ParseColors(text);
            }
            catch (Exception ex)
            {
                return ParseColors($"§ERROR§⚠ ERROR LOADING PROTOCOL: {ex.GetType().Name}: {ex.Message}§END§");
            }
        }

        private void StartProtocolTypewriter(string text)
        {
            if (_protocolText == null)
                return;

            StopProtocolTypewriter();
            _protocolText.text = string.Empty;
            _protocolTypewriterRoutine = StartCoroutine(ProtocolTypewriterRoutine(text ?? string.Empty));
        }

        private void StopProtocolTypewriter()
        {
            if (_protocolTypewriterRoutine != null)
            {
                StopCoroutine(_protocolTypewriterRoutine);
                _protocolTypewriterRoutine = null;
            }
        }

        private IEnumerator ProtocolTypewriterRoutine(string text)
        {
            if (_protocolText == null)
                yield break;

            string[] lines = (text ?? string.Empty).Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                _protocolText.text += lines[i] + "\n";
                if (_protocolScroll != null)
                    _protocolScroll.ScrollTo(_protocolText);

                if (_bootLineDelay > 0f)
                    yield return new WaitForSeconds(_bootLineDelay);
            }

            _protocolTypewriterRoutine = null;
        }

        private void RequestClose()
        {
            // Se siamo in una sub-view, chiudiamo quella (come comando CLOSE). PROTOCOL ora è in console, non ha vista dedicata.
            if (_detailView != null && _detailView.style.display != DisplayStyle.None)
            {
                AppendRawLine("§INFO§⚠ Chiusura vista dettaglio§END§");
                AppendRawLine("");
                SwitchToConsole();
                FlushConsole();
                return;
            }

            if (_queue.Count <= 0)
            {
                Close();
                return;
            }

            int totalAp = 0;
            foreach (var a in _queue) totalAp += a != null ? a.ApCost : 0;

            _inputState = InputState.ConfirmingExecuteOrDiscardQueue;
AppendRawLine("§TITLE§✓ CHIUSURA TERMINALE§END§");
                AppendRawLine("§INFO§Restituzione coda azioni per conferma sequenza...§END§");
            AppendRawLine("");
            AppendRawLine($"§DATA§Azioni in coda: {_queue.Count}§END§");
            AppendRawLine($"§DATA§Total AP cost: {totalAp} AP§END§");
            AppendRawLine("");
            AppendRawLine("§WARN§⚠ CONFIRMATION REQUIRED — EXECUTE QUEUE? [Y/N]§END§");
            AppendRawLine("");
            FlushConsole();
        }

        /// <summary>Hex rich text per pH Dome: colore dalla <see cref="PhSystem.PhBand"/> corrente (allineato TopBar/Dome HUD).</summary>
        private string GetPhColorHexForDomePhBand()
        {
            if (_phSystem == null)
                return "#888888";
            return PhGradientDisplayColors.ToHtmlStringRgb(PhGradientDisplayColors.GetColorForPhBand(_phSystem.EvaluateState()));
        }

        /// <summary>Banda pH per display (come tooltip TopBar: Acido / Neutrale / Basico).</summary>
        private static string GetPhBandNameForDisplay(float ph)
        {
            if (ph < -25f) return "Acido";
            if (ph > 25f) return "Basico";
            return "Neutrale";
        }

        /// <summary>
        /// Converte tag custom §X§...§END§ in rich text <color>.
        /// </summary>
        private static string ParseColors(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return string.Empty;

            // Palette: leggibilità e gerarchia (titoli ciano, positivo verde, info teal, comandi blu)
            const string greenBright = "#99FF99";  /* §TITLE§: OK, positivo, evidenze */
            const string blue = "#5DB6E3";        /* §CMD§: comandi */
            const string infoTeal = "#8FBC8F";   /* §INFO§: testo informativo (distinto da CMD) */
            const string cyan = "#00FFFF";       /* §DATA§: titoli sezione, ID vasi */
            const string purple = "#B580D1";
            const string yellow = "#E6C96F";     /* §WARN§: avvisi, consiglio */
            const string red = "#D35F5F";        /* §ERROR§: errori, NON OK */
            const string white = "#FFFFFF";
            const string dimGray = "#A8A8A8";   /* §DIM§: grigio chiaro per note esplicative */

            return raw
                .Replace("§DIM§", $"<color={dimGray}>")
                .Replace("§TITLE§", $"<color={greenBright}>")
                .Replace("§CMD§", $"<color={blue}>")
                .Replace("§INFO§", $"<color={infoTeal}>")
                .Replace("§DATA§", $"<color={cyan}>")
                .Replace("§VAL§", $"<color={yellow}>")
                .Replace("§WARN§", $"<color={yellow}>")
                .Replace("§PURPLE§", $"<color={purple}>")
                .Replace("§ERROR§", $"<color={red}>")
                .Replace("§Y§", $"<color={greenBright}>")
                .Replace("§N§", $"<color={red}>")
                .Replace("§WHITE§", $"<color={white}>")
                .Replace("§BLUE§", $"<color={blue}>")
                .Replace("§RED§", $"<color={red}>")
                .Replace("§END§", "</color>");
        }

        /// <summary>Rimuove i tag §...§ dal testo per ottenere plain text (es. per blocchi CONSIGLIO in un solo colore).</summary>
        private static string StripSectionTags(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            return System.Text.RegularExpressions.Regex.Replace(raw, "§[A-Za-z]+§", "");
        }

        /// <summary>Formatta una riga di consiglio: testo in plain, comandi §CMD§...§END§ mantenuti e racchiusi in [ ].</summary>
        private static string FormatConsiglioLineWithCommands(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            string s = raw;
            s = System.Text.RegularExpressions.Regex.Replace(s, "§(WARN|INFO|TITLE|ERROR|DATA)§", "");
            s = System.Text.RegularExpressions.Regex.Replace(s, "§CMD§(.*?)§END§", "§CMD§[$1]§END§");
            return s;
        }
    }
}

