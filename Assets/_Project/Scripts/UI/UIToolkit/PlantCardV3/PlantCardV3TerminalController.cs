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
using Sporae.Dome.PotSystem.Growth;
using System.Collections.Generic;
using _Project.Player;
using Sporae.UI.UIToolkit.HUD.Components;
using Sporae.UI.UIToolkit.PlantCard.Helpers;
using Sporae.UI.UIToolkit.PlantCard.Components;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.SeedInventory;

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
            ConfirmingActionToQueue,
            ConfirmingExecuteOrDiscardQueue
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

        [Header("Behavior")]
        [SerializeField] private bool _startVisibleInEditor = false;

        [Header("Backdrop Blur")]
        [SerializeField] private bool _useBlurredBackdrop = true;
        [SerializeField] private bool _outsideShowsGameView = true;
        [SerializeField, Range(0.5f, 0.99f)] private float _backdropDimAlpha = 0.95f;
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
        [SerializeField, Range(0.05f, 1f)] private float _typewriterLongOutputMultiplier = 0.2f;
        [SerializeField, Range(0.05f, 1f)] private float _typewriterGlobalSpeedMultiplier = 0.2f;

        [Header("Typewriter SFX")]
        [SerializeField] private AudioSource _typewriterAudioSource;
        [SerializeField] private AudioClip _typewriterSfx;
        [SerializeField] private AudioClip _bootStartSfx;
        [SerializeField, Range(0.01f, 0.2f)] private float _typewriterSfxInterval = 0.035f;

        private VisualElement _root;
        private Button _btnClose;
        private ScrollView _consoleScroll;
        private Label _consoleText;
        private VisualElement _forecastConditionTooltip;
        private Label _forecastConditionTooltipText;
        private bool _shouldHideForecastConditionTooltip;
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
        private Label _inputHintOverlay;
        private VisualElement _promptRoot;
        private VisualElement _blinkCursor;
        private IVisualElementScheduledItem _blinkSchedule;
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
        private bool _bootSequenceActive;
        private bool _typewriterActive;
        private readonly Queue<string> _typewriterQueue = new();
        private Coroutine _typewriterRoutine;
        private float _nextTypewriterSfxTime;
        private float _typewriterCommandSpeedMultiplier = 1f;
        private int _typewriterCommandBlockMultiplier = 1;
        private Coroutine _protocolTypewriterRoutine;

        private readonly System.Collections.Generic.List<QueuedAction> _queue = new();
        private InputState _inputState = InputState.Idle;
        private QueuedAction _pendingConfirmAction;
        private readonly Dictionary<string, int> _reservedItems = new();

        private GameManager _gameManager;
        private FoundationNotificationService _foundation;
        private Inventory _inventory;
        private DayCycleSystem _dayCycleSystem;
        private PhSystem _phSystem;
        private PotSystemConfig _potSystemConfig;

        private SelectionContext _selection;

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
        private bool _wasPerspectiveMoverEnabled;
        private bool _wasRouterEnabled;

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
            // PotActionsMenu usa 400, AdditiveSelector 500: qui usiamo 600 per farlo modal.
            if (_uiDocument != null)
                _uiDocument.sortingOrder = 600;

            _root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (_root == null)
            {
                Debug.LogError("PlantCardV3TerminalController: rootVisualElement non trovato!");
                return;
            }

            _btnClose = _root.Q<Button>("pcv3-close-button");
            _consoleScroll = _root.Q<ScrollView>("pcv3-console-scroll");
            _consoleText = _root.Q<Label>("pcv3-console-text");
            _protocolScroll = _root.Q<ScrollView>("pcv3-protocol-scroll");
            _protocolText = _root.Q<Label>("pcv3-protocol-text");
            _consoleView = _root.Q<VisualElement>("pcv3-console-view");
            _protocolView = _root.Q<VisualElement>("pcv3-protocol-view");
            _detailView = _root.Q<VisualElement>("pcv3-detail-view");
            _promptRoot = _root.Q<VisualElement>("pcv3-prompt");
            _input = _root.Q<TextField>("pcv3-input");
            _apLabel = _root.Q<Label>("pcv3-ap-label");
            _queuedLabel = _root.Q<Label>("pcv3-queued-label");
            _potList = _root.Q<ScrollView>("pcv3-potlist");
            _backdrop = _root.Q<VisualElement>("pcv3-backdrop");
            _dimOverlay = _root.Q<VisualElement>("pcv3-dim");
            _outerGlow = _root.Q<VisualElement>("pcv3-outer-glow");

            // Custom scrollbars
            _potListScrollbar = _root.Q<VisualElement>("pcv3-potlist-scrollbar");
            _potListScrollbarTrack = _root.Q<VisualElement>("pcv3-potlist-scrollbar-track");
            _potListScrollbarThumb = _root.Q<VisualElement>("pcv3-potlist-scrollbar-thumb");

            // Inizializza scrollbar custom
            InitializeCustomScrollbars();

            ApplyConsoleFont();

            if (_consoleText != null)
                _consoleText.enableRichText = true;
            if (_protocolText != null)
                _protocolText.enableRichText = true;
            EnsureForecastConditionTooltip();

            if (_btnClose != null)
                _btnClose.clicked += RequestClose;

            // Click anywhere on terminal should re-focus command input
            _root.RegisterCallback<MouseDownEvent>(_ => FocusInput(), TrickleDown.TrickleDown);

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
        // #endregion

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

                Debug.Log($"[PlantCardV3TerminalController][FocusDebug #{_focusDebugSeq}] {msg} | visible={_isVisible} | focused={focusedType}:{focusedName}");
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
                // NOTE: the ">" is already rendered by pcv3-prompt-prefix in UXML. Avoid double ">".
                _inputHintOverlay = new Label("Type START for commands...");
                _inputHintOverlay.name = "pcv3-input-hint";
                _inputHintOverlay.pickingMode = PickingMode.Ignore;
                _promptRoot.Add(_inputHintOverlay);
            }

            // Style it to match the mock (green-ish, subtle) and keep it in the prompt row.
            _inputHintOverlay.style.position = Position.Absolute;
            // prefix label width is 16px; add a small gap so text doesn't touch the ">"
            _inputHintOverlay.style.left = 18;
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
            _gameManager = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            if (_gameManager == null)
                _gameManager = FindObjectOfType<GameManager>();

            _foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            _inventory = _gameManager != null ? _gameManager.PlayerInventory : null;
            _dayCycleSystem = ServiceContainer.Instance?.Get<DayCycleSystem>(suppressWarning: true);
            _phSystem = ServiceContainer.Instance?.Get<PhSystem>(suppressWarning: true);
            _potSystemConfig = Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");
            if (_potSystemConfig == null)
            {
                var allConfigs = Resources.LoadAll<PotSystemConfig>("Configs");
                if (allConfigs != null && allConfigs.Length > 0)
                    _potSystemConfig = allConfigs[0];
            }

            // Cache player mover for point&click suspension while terminal is open
            _playerMover = FindObjectOfType<PlayerClickMover2D>();
            _playerPerspectiveMover = FindObjectOfType<PlayerPerspectiveMover2D>();
            _playerMoverRouter = FindObjectOfType<PlayerMoverRouter2D>();

            if (_isVisible)
            {
                RenderWelcome(clearConsole: true);
                RefreshHeader();
                RefreshSidebar();
            }

            // Safety: se siamo nella stessa Canvas della HUD, forza PlantCardV3 dopo TopBar/BottomNav nella gerarchia.
            TryMoveAfterHud();
        }

        private void InitializeCustomScrollbars()
        {
            // Setup scrollbar per pot list
            if (_potList != null && _potListScrollbar != null && _potListScrollbarTrack != null && _potListScrollbarThumb != null)
            {
                SetupScrollbar(_potList, _potListScrollbar, _potListScrollbarTrack, _potListScrollbarThumb);
            }
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
                float dimAlpha = _outsideShowsGameView ? 0f : Mathf.Clamp01(_backdropDimAlpha);
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
            StopBootSequence();
            StopTypewriter();
            _outerGlowGenerator?.Dispose();
            _outerGlowGenerator = null;
            _outerGlowMaterialRuntime = null;
        }

        public void Open()
        {
            PrepareBackdrop();
            SetVisible(true);
            SwitchToConsole();
            if (_playBootSequence)
                StartBootSequence();
            else
                RenderWelcome(clearConsole: true);
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

            if (_root == null) return;

            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _root.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;

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
                _dimOverlay.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
                _backdrop.style.backgroundImage = new StyleBackground();
                _backdrop.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
                return;
            }

            if (!_useBlurredBackdrop) return;

            _dimOverlay.style.backgroundColor = new Color(0f, 0f, 0f, Mathf.Clamp01(_backdropDimAlpha));
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
                    _backdrop.style.unityBackgroundScaleMode = ScaleMode.ScaleAndCrop;
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

            // Basato su SceneHierarchy.txt: nomi reali dei GO UI.
            string[] goNames =
            {
                "HUD_TopBar",
                "HUD_BottomNavigation",
                "PlayerStatusPanel",
                "PlantCardV2",
                "PotActionsMenu",
                "SeedInventoryMenu",
                "IrrigationDialog",
                "UIAdditiveSelector",
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
                int left = _gameManager != null ? _gameManager.ActionsLeft : 0;
                int max = _gameManager != null && _gameManager.ActionSystem != null ? _gameManager.ActionSystem.MaxActions : 0;
                _apLabel.text = $"ACTION POINTS: {left}/{max}";
            }

            if (_queuedLabel != null)
            {
                _queuedLabel.text = $"QUEUED: {_queue.Count}";
            }
        }

        private void RefreshSidebar()
        {
            RefreshPotCards();
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
                var sub = new Label("Ready for cultivation");
                sub.AddToClassList("pcv3-potcard-subtext");
                info.Add(standby);
                info.Add(sub);
                body.Add(info);

                emptyRoot.Add(body);
                return emptyRoot;
            }

            if (_potCardTemplate == null)
            {
                Debug.LogError("PlantCardV3TerminalController: _potCardTemplate non assegnato!");
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
                    Debug.LogError("PlantCardV3TerminalController: Template non contiene pcv3-potcard!");
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
                titleLabel.text = $"{potId} -- {GetPlantDisplayName(state.PlantCode)}";
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
                plantImage.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
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
                string lightStressText = state != null && _potSystemConfig != null
                    ? $"{PlantCardCalculators.CalculateLightStressPercent(state.LightExposure, _potSystemConfig.MaxLightExposure)}%"
                    : "---";

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
                UpdateStatValue(rowIndex++, phDriftText, drift > 0 ? "pcv3-value-red" : "pcv3-value-blue");
                UpdateStatValue(rowIndex++, growthText, "pcv3-value-green");
                // Separator è già nel template, skip
                rowIndex++; // Skip separator
                UpdateStatValue(rowIndex++, hydrationText, "pcv3-value-blue");
                UpdateStatValue(rowIndex++, lightStressText, "pcv3-value-yellow");
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

            AppendRawLine("┌────────────────────────────────────────────────────────────────────────────┐");
            AppendRawLine("│ §TITLE§SPORIUM BOTANICAL INCUBATOR - AUTOMATED CULTIVATION MANAGEMENT v3.1§END§ │");
            AppendRawLine("│ Real-time cultivation monitoring, vital analysis & diary logging           │");
            AppendRawLine("└────────────────────────────────────────────────────────────────────────────┘");
            AppendRawLine("");
            AppendRawLine("△ §TITLE§POT MONITORING TERMINAL INITIALIZED§END§");
            AppendRawLine("<color=#E6C96F>──────────────────────────────────────────────────────────────────────────────</color>");
            AppendRawLine("▶ §CMD§TYPE START FOR COMMAND LIST§END§");
            AppendRawLine("<color=#E6C96F>──────────────────────────────────────────────────────────────────────────────</color>");
            AppendRawLine("<color=#79E679>△ ALL ACTIONS QUEUED UNTIL SEQUENCE CONFIRMATION</color>");
            AppendRawLine("<color=#7FFF7A>[</color><color=#7FFF7A>F</color><color=#7FFF7A>]</color> <color=#7FFF7A>QUICK ACCESS:</color> <color=#5DB6E3>FORECAST</color> <color=#7FFF7A>- Monitoring live Forecast</color>");
            AppendRawLine("<color=#7FFF7A>[</color><color=#7FFF7A>+</color><color=#7FFF7A>]</color> <color=#7FFF7A>TYPE</color> <color=#5DB6E3>PROTOCOL</color> <color=#7FFF7A>TO VIEW BIOLOGICAL PROTOCOL DOME_02</color>");
            AppendRawLine("");

            FlushConsole();
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
                "╔═══════════════════════════════════════════════════════════════════════════╗",
                "║              SPORIUM INCUBATOR CONTROL TERMINAL v3.1                      ║",
                "║              AUTOMATED CULTIVATION MANAGEMENT SYSTEM                      ║",
                "╚═══════════════════════════════════════════════════════════════════════════╝",
                "[BOOT] Initializing system...",
                "[OK] BIOS checksum verified",
                "[OK] Memory test passed",
                "[INIT] Loading cultivation modules...",
                "  ▸ HVAC-CTRL............ [ONLINE]",
                "  ▸ HYDRATION-SYS........ [ONLINE]",
                "  ▸ LED-SPECTRUM-A....... [ONLINE]",
                "  ▸ LED-SPECTRUM-B....... [ONLINE]",
                "  ▸ SOIL-SENSORS......... [ONLINE]",
                "  ▸ pH-MONITOR........... [ONLINE]",
                "[DB] Connecting to cultivation database...",
                "[OK] Database mounted: DOME_02_INCUBATOR",
                "[OK] POT records synchronized (6 units)",
                "[OK] Historical logs indexed",
                "[NET] Establishing Vault network link...",
                "[OK] Connected to SPORIUM-NET",
                "[OK] Action Queue system ready",
                "[READY] Incubator Control Terminal initialized"
            };

            foreach (var line in lines)
            {
                AppendRawLine($"<color=#E6C96F>{line}</color>");
                FlushConsole();
                float delay = IsBootSectionLine(line) ? _bootSectionDelay : _bootLineDelay;
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
            }

            AppendRawLine("");
            RenderWelcome(clearConsole: false);

            _bootSequenceActive = false;
            if (_input != null)
                _input.SetEnabled(true);
        }

        private IEnumerator TypewriterRoutine()
        {
            _nextTypewriterSfxTime = 0f;
            bool longOutputMode = IsLongOutputQueued();
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
                            FlushConsoleImmediate();
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
                    FlushConsoleImmediate();
                    TryPlayTypewriterSfx();
                    if (delay > 0f)
                        yield return new WaitForSeconds(delay);
                }

                _consoleBuffer.AppendLine();
                FlushConsoleImmediate();
            }

            _typewriterRoutine = null;
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

            bool debugLoggedFirst = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string plain = StripRichTextTags(lines[i] ?? string.Empty).TrimStart();
                if (!plain.StartsWith("Condition:", StringComparison.OrdinalIgnoreCase))
                    continue;

                string potId = null;
                bool foundHeaderArrow = false;
                for (int j = i; j >= 0; j--)
                {
                    string headerPlain = StripRichTextTags(lines[j] ?? string.Empty);
                    int arrow = headerPlain.IndexOf('►');
                    if (arrow < 0) continue;
                    foundHeaderArrow = true;

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

            int maxHydration = _potSystemConfig != null ? _potSystemConfig.MaxHydration : 10;
            int hydrationPercent = PlantCardCalculators.CalculateHydrationPercent(state.Hydration, maxHydration);

            bool waterOk = stageReq.IsHydrationInRange(hydrationPercent);

            // Light Stress (same metric used across HUD/PlantCardV2 tooltips)
            int consecutiveDays = state.GetConsecutiveLedDays();
            int maxDaysForFullStress = _potSystemConfig != null ? _potSystemConfig.MaxDaysForFullStress : 5;
            float stressPercentage = Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f;
            int lightStressPercent = Mathf.RoundToInt(stressPercentage);
            bool lightOk = stageReq.IsLightInRange(lightStressPercent);

            bool fertilizerOk = stageReq.IsFertilizerInRange(state.FertilizerLevel);

            string conditionName = ConditionNameForUi(MapScoreToConditionForUi(state.ConditionScore));
            PlantCondition currentCondition = (PlantCondition)state.ConditionLabel;
            sb.AppendLine($"<b>Condizione della Pianta: {conditionName}</b>");
            
            // FASE 1.1: Aggiungi informazioni sui modificatori crescita e produzione
            float growthMultiplier = ConditionGrowthModifier.GetGrowthSpeedMultiplier(currentCondition);
            float productionMultiplier = ConditionGrowthModifier.GetProductionMultiplier(currentCondition);
            
            if (growthMultiplier != 1.0f || productionMultiplier != 1.0f)
            {
                sb.AppendLine();
                sb.AppendLine("<b>Effetti sulla Pianta:</b>");
                
                if (growthMultiplier > 1.0f)
                {
                    float growthBonus = (growthMultiplier - 1.0f) * 100f;
                    sb.AppendLine($"  <color=#00FF00>+{growthBonus:F0}% velocità crescita</color>");
                }
                else if (growthMultiplier < 1.0f)
                {
                    float growthMalus = (1.0f - growthMultiplier) * 100f;
                    sb.AppendLine($"  <color=#FF0000>-{growthMalus:F0}% velocità crescita</color>");
                }
                
                if (productionMultiplier > 1.0f)
                {
                    float productionBonus = (productionMultiplier - 1.0f) * 100f;
                    sb.AppendLine($"  <color=#00FF00>+{productionBonus:F0}% produzione frutti</color>");
                }
                else if (productionMultiplier < 1.0f)
                {
                    float productionMalus = (1.0f - productionMultiplier) * 100f;
                    sb.AppendLine($"  <color=#FF0000>-{productionMalus:F0}% produzione frutti</color>");
                }
            }
            
            sb.AppendLine();
            sb.AppendLine("La pianta cresce quando si trova nel <color=#00FF00>range giusto</color> di:");
            sb.AppendLine();

            // Water
            string waterStatus = waterOk ? "<color=#00FF00>OK</color>" : "<color=#FF0000>NON OK</color>";
            sb.AppendLine($"• <color=#3F6FFF>Acqua (Water)</color>: {waterStatus}");
            if (!waterOk)
            {
                sb.AppendLine($"  Range ideale: {stageReq.hydrationMin}% - {stageReq.hydrationMax}%");
                sb.AppendLine($"  Attuale: {hydrationPercent}%");
            }
            sb.AppendLine();

            // Light
            string lightStatus = lightOk ? "<color=#00FF00>OK</color>" : "<color=#FF0000>NON OK</color>";
            sb.AppendLine($"• <color=#FFD700>Luce</color>: {lightStatus}");
            sb.AppendLine($"  Range ideale: <color=#00FF00>{stageReq.lightMin}%-{stageReq.lightMed}%-{stageReq.lightMax}%</color>");
            sb.AppendLine($"  Attuale: {(lightOk ? $"<color=#00FF00>{lightStressPercent}%</color>" : $"{lightStressPercent}%")}");
            sb.AppendLine();

            // Fertilizer (optional in Seed/Sprout)
            bool isFertilizerOptional = (currentStage == PlantStage.Seed || currentStage == PlantStage.Sprout);
            string fertilizerStatus = fertilizerOk ? "<color=#00FF00>OK</color>" : "<color=#FF0000>NON OK</color>";
            string fertilizerLabel = isFertilizerOptional
                ? $"• <color=#90EE90>Fertilizzante</color> (opzionale): {fertilizerStatus}"
                : $"• <color=#90EE90>Fertilizzante</color>: {fertilizerStatus}";
            sb.AppendLine(fertilizerLabel);
            if (!fertilizerOk)
            {
                sb.AppendLine($"  Range ideale: {stageReq.fertilizerMin}% - {stageReq.fertilizerMax}%");
                sb.AppendLine($"  Attuale: {state.FertilizerLevel}%");
            }
            if (isFertilizerOptional)
            {
                sb.AppendLine("  <color=#FFFF00>Nota: Negli stadi Seed e Sprout, il fertilizzante è opzionale per avanzare.</color>");
            }
            sb.AppendLine();

            // Giorni mancanti per avanzare (stessa logica PlantCardV2 tooltip)
            int daysInStage = state.DaysInCurrentStage;
            int requiredDays = stageReq.durationDays;
            int daysRemaining = Mathf.Max(0, requiredDays - daysInStage);

            if (daysRemaining > 0)
            {
                sb.AppendLine($"<color=#FFFF00>Giorni mancanti per avanzare:</color> <color=#FFFFFF>{daysRemaining}</color>");
                sb.AppendLine($"  (Giorni nello stadio: {daysInStage} / {requiredDays})");
            }
            else
            {
                sb.AppendLine("<color=#00FF00>✓ Giorni minimi raggiunti!</color>");
                if (waterOk && lightOk && fertilizerOk)
                {
                    sb.AppendLine("<color=#00FF00>✓ Tutti i parametri sono nel range ideale!</color>");
                    sb.AppendLine("<color=#00FF00>La pianta può avanzare al prossimo stadio.</color>");
                }
                else
                {
                    sb.AppendLine("<color=#FFFF00>⚠️ Metti tutti i parametri nel range ideale per avanzare.</color>");
                }
            }

            return sb.ToString();
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

        private void AutoScrollConsole()
        {
            if (_consoleScroll == null) return;

            void ScrollToBottom(string tag)
            {
                _consoleScroll.ScrollTo(_consoleText);
                var vs = _consoleScroll.verticalScroller;
                if (vs != null && vs.highValue > 0)
                {
                    vs.value = vs.highValue;
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
            if (evt.keyCode == KeyCode.F)
            {
                HandleCommand("FORECAST");
                evt.StopPropagation();
                return;
            }
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

            // State-gated input
            if (_inputState == InputState.SelectingItem)
            {
                HandleSelectingItem(upper);
                return;
            }
            if (_inputState == InputState.ConfirmingActionToQueue)
            {
                HandleConfirmToQueue(upper);
                return;
            }
            if (_inputState == InputState.ConfirmingExecuteOrDiscardQueue)
            {
                HandleConfirmExecuteOrDiscard(upper);
                return;
            }

            // Clear any forecast hover hotspots when running a new command.
            ClearForecastConditionHotspots();

            AppendRawLine($"> {trimmed}");
            FlushConsole();

            if (upper == "START" || upper == "HELP")
            {
                PrintStartCommands();
                FlushConsole();
                SwitchToConsole();
                return;
            }

            if (upper == "STATUS")
            {
                PrintStatusTable();
                FlushConsole();
                SwitchToConsole();
                return;
            }

            if (upper == "FORECAST" || upper == "F")
            {
                SwitchToConsole();
                PrintForecast();
                FlushConsole();
                ScheduleRebuildForecastConditionHotspots();
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
                    AppendRawLine("§ERROR§⚠ ERROR: POT ID REQUIRED. USAGE: OPEN [POT-ID]§END§");
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
                    AppendRawLine("§ERROR§⚠ ERROR: POT ID REQUIRED. USAGE: NOTE [POT-ID]§END§");
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
                    AppendRawLine("§ERROR§⚠ ERROR: POT ID REQUIRED. USAGE: UPROOT [POT-ID]§END§");
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
                    AppendRawLine("§ERROR§⚠ ERROR: POT ID REQUIRED. USAGE: PLANT [POT-ID]§END§");
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
                    AppendRawLine("§ERROR§⚠ ERROR: POT ID REQUIRED. USAGE: FERTILIZE [POT-ID]§END§");
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
                    AppendRawLine("§ERROR§⚠ ERROR: POT ID REQUIRED. USAGE: SPRAY [POT-ID]§END§");
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
                    AppendRawLine("§ERROR§⚠ ERROR: POT ID REQUIRED. USAGE: WATERING [POT-ID]§END§");
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
                    AppendRawLine("§ERROR§⚠ ERROR: POT ID REQUIRED. USAGE: LED RED [POT-ID]§END§");
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
                    AppendRawLine("§ERROR§⚠ ERROR: POT ID REQUIRED. USAGE: LED BLUE [POT-ID]§END§");
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
                    AppendRawLine("§ERROR§⚠ ERROR: POT ID REQUIRED. USAGE: HARVEST [POT-ID]§END§");
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
                    AppendRawLine("§ERROR§⚠ ERROR: POT ID REQUIRED. USAGE: PRUNE [POT-ID]§END§");
                    AppendRawLine("");
                    FlushConsole();
                    return;
                }
                BeginConfirmToggleAction(QueuedActionType.Prune, potId);
                return;
            }

            if (upper == "CLOSE")
            {
                // Contestuale: se siamo in protocol/detail, torna a console; altrimenti warning.
                if (_protocolView != null && _protocolView.style.display != DisplayStyle.None)
                {
                    AppendRawLine("§INFO§⚠ CLOSING DETAILED VIEW§END§");
                    AppendRawLine("");
                    SwitchToConsole();
                    FlushConsole();
                    return;
                }
                if (_detailView != null && _detailView.style.display != DisplayStyle.None)
                {
                    AppendRawLine("§INFO§⚠ CLOSING DETAILED VIEW§END§");
                    AppendRawLine("");
                    SwitchToConsole();
                    FlushConsole();
                    return;
                }
                AppendRawLine("§WARN§⚠ NOTHING TO CLOSE§END§");
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

                AppendRawLine("§ERROR§⚠ INVALID QUEUE COMMAND. USAGE: QUEUE SHOW§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            if (upper == "CLEAR")
            {
                if (_queue.Count == 0)
                {
                    AppendRawLine("§WARN§⚠ ACTION QUEUE IS ALREADY EMPTY§END§");
                    AppendRawLine("");
                }
                else
                {
                    _queue.Clear();
                    RebuildReservedItems();
                    AppendRawLine("§TITLE§✓ ACTION QUEUE CLEARED§END§");
                    AppendRawLine("§INFO§All queued actions removed§END§");
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

            AppendRawLine("§ERROR§⚠ INVALID COMMAND. TYPE START FOR HELP§END§");
            AppendRawLine("");
            FlushConsole();
        }

        private void PrintQueue()
        {
            AppendRawLine("╔═ ACTION QUEUE ═════════════════════════════════════════════════════════════╗");

            if (_queue.Count == 0)
            {
                AppendRawLine("║ §WARN§(empty)§END§                                                          ║");
                AppendRawLine("╚════════════════════════════════════════════════════════════════════════════╝");
                AppendRawLine("");
                return;
            }

            AppendRawLine("║ #  │ POT     │ ACTION        │ ITEM                │ AP                    ║");
            AppendRawLine("╟────┼─────────┼───────────────┼─────────────────────┼───────────────────────╢");

            for (int i = 0; i < _queue.Count; i++)
            {
                var a = _queue[i];
                if (a == null) continue;

                string idx = (i + 1).ToString().PadLeft(2);
                string pot = (a.PotId ?? "POT-???").PadRight(7).Substring(0, 7);
                string action = GetActionLabel(a.Type).PadRight(13).Substring(0, 13);
                
                // Converti ItemTypeId in nome leggibile se è un seed
                string itemDisplayName = string.IsNullOrEmpty(a.ItemTypeId) 
                    ? "-" 
                    : GetItemDisplayName(a.ItemTypeId);
                string item = itemDisplayName.PadRight(19);
                if (item.Length > 19) item = item.Substring(0, 19);
                
                string ap = $"{a.ApCost} AP".PadRight(21);
                if (ap.Length > 21) ap = ap.Substring(0, 21);

                AppendRawLine($"║ {idx} │ {pot} │ {action} │ {item} │ {ap} ║");
            }

            int totalAp = 0;
            foreach (var a in _queue) totalAp += a != null ? a.ApCost : 0;

            AppendRawLine("╚════════════════════════════════════════════════════════════════════════════╝");
            AppendRawLine($"§INFO§Total actions: {_queue.Count} | Total AP: {totalAp}§END§");
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
            AppendRawLine("§TITLE§MONITORING LIVE FORECAST§END§");
            AppendRawLine("§INFO§Growth Stage Prediction & Requirements Analysis§END§");
            AppendRawLine("");

            if (pots == null || pots.Count == 0)
            {
                AppendRawLine("§WARN§No pots found in scene.§END§");
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
                AppendRawLine("§WARN§No planted pots to forecast.§END§");
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
                _ => "■ STABLE"
            };

            AppendRawLine("CURRENT STATUS");
            AppendRawLine($"§DATA§Stage: {stageLabel}§END§");
            AppendRawLine($"Condition: {pot.ConditionScore}% [{conditionName}]");
            AppendRawLine($"Trend: {trendLabel}");
            AppendRawLine("");

            // STAGE PROGRESSION
            int requiredDays = f.EffectiveRequiredDays;
            if (requiredDays <= 0 && f.StageReq != null) requiredDays = Mathf.Max(1, f.StageReq.durationDays);
            string daysInStage = requiredDays > 0 ? $"{pot.DaysInCurrentStage}/{requiredDays} days" : $"{pot.DaysInCurrentStage}/— days";
            int barWidth = 20;
            int pct = Mathf.Clamp(f.ProgressPercent, 0, 100);
            int filled = Mathf.RoundToInt((pct / 100f) * barWidth);
            filled = Mathf.Clamp(filled, 0, barWidth);
            string bar = new string('█', filled) + new string('░', barWidth - filled);

            string eta = f.EstimatedDaysToAdvance.HasValue ? $"{f.EstimatedDaysToAdvance.Value} days remaining" : "—";

            AppendRawLine("STAGE PROGRESSION");
            AppendRawLine($"Days in Stage: {daysInStage}");
            AppendRawLine($"Progress: {bar} {pct}%");
            AppendRawLine($"Prossima: {f.SoonInConditionName}");
            AppendRawLine($"Estimated: {eta}");
            AppendRawLine("");

            // ADVANCEMENT REQUIREMENTS (real data)
            AppendRawLine("ADVANCEMENT REQUIREMENTS");

            // Condition threshold for target
            int requiredScore;
            string conditionReqText;
            bool conditionReqOk = true;
            if (f.Trend == ForecastDirection.Up)
            {
                // Target is higher/equal
                requiredScore = f.SoonInConditionName == "Rigogliosa" ? DifficultyCalibrationConfig.ConditionThresholdRigogliosa
                    : f.SoonInConditionName == "Sana" ? DifficultyCalibrationConfig.ConditionThresholdSana
                    : f.SoonInConditionName == "Appassita" ? DifficultyCalibrationConfig.ConditionThresholdAppassita
                    : 0;
                conditionReqOk = pot.ConditionScore >= requiredScore;
                conditionReqText = $"{(conditionReqOk ? "✓" : "✗")} Condition      {pot.ConditionScore}% | Required: >={requiredScore}%";
            }
            else if (f.Trend == ForecastDirection.Down)
            {
                // Target is lower/equal: define threshold as "below next band start"
                requiredScore = f.SoonInConditionName == "Sana" ? DifficultyCalibrationConfig.ConditionThresholdRigogliosa
                    : f.SoonInConditionName == "Appassita" ? DifficultyCalibrationConfig.ConditionThresholdSana
                    : f.SoonInConditionName == "Critica" ? DifficultyCalibrationConfig.ConditionThresholdAppassita
                    : 100;
                conditionReqOk = pot.ConditionScore < requiredScore;
                conditionReqText = $"{(conditionReqOk ? "✓" : "✗")} Condition      {pot.ConditionScore}% | Required: <{requiredScore}%";
            }
            else
            {
                conditionReqText = $"✓ Condition      {pot.ConditionScore}% | Required: (stable)";
            }

            AppendRawLine(conditionReqOk ? conditionReqText : $"§ERROR§{conditionReqText}§END§");

            // Hydration
            int maxHydration = _potSystemConfig != null ? _potSystemConfig.MaxHydration : 10;
            int hydrationPercent = maxHydration > 0 ? Mathf.RoundToInt((float)pot.Hydration / maxHydration * 100f) : 0;
            string hydrationReq = f.StageReq != null ? $"{f.StageReq.hydrationMin}-{f.StageReq.hydrationMax}%" : "—";
            string hydrationLine = $"{(f.HydrationOk ? "✓" : "✗")} Hydration      {hydrationPercent}% | Required: {hydrationReq}";
            AppendRawLine(f.HydrationOk ? hydrationLine : $"§ERROR§{hydrationLine}§END§");

            // Fertilizer
            string fertReq = f.StageReq != null ? $"{f.StageReq.fertilizerMin}-{f.StageReq.fertilizerMax}%" : "—";
            string fertLine = $"{(f.FertilizerOk ? "✓" : "✗")} Fertilizer     {pot.FertilizerLevel}% | Required: {fertReq}";
            AppendRawLine(f.FertilizerOk ? fertLine : $"§ERROR§{fertLine}§END§");

            // Light stress percent (UI metric) - BUG FIX: Usa GetConsecutiveLedDays invece di LightExposure
            int lightStressPercent = 0;
            if (_potSystemConfig != null)
            {
                int consecutiveDays = pot.GetConsecutiveLedDays();
                int maxDaysForFullStress = Mathf.Max(1, _potSystemConfig.MaxDaysForFullStress);
                lightStressPercent = Mathf.RoundToInt(Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f);
            }
            string lightReq = f.StageReq != null ? $"{f.StageReq.lightMin}-{f.StageReq.lightMax}%" : "—";
            string lightLine = $"{(f.LedOk ? "✓" : "✗")} Light Stress   {lightStressPercent}% | Required: {lightReq}";
            AppendRawLine(f.LedOk ? lightLine : $"§ERROR§{lightLine}§END§");

            // Mold risk level (0-3). Growth is blocked at >=2.
            int mold = Mathf.Clamp(pot.MoldRiskLevel, 0, 3);
            bool moldOk = mold < 2;
            string moldLine = $"{(moldOk ? "✓" : "✗")} Mold Risk      Level {mold} | Required: <2";
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
                AppendRawLine("§ERROR§► STATUS: BLOCKED§END§");
            }
            else if (f.HydrationOk && f.LedOk && f.FertilizerOk && f.DurationOk && f.OptimalDaysOk && f.PointsOk)
            {
                AppendRawLine("§TITLE§► STATUS: READY FOR ADVANCEMENT§END§");
            }
            else
            {
                AppendRawLine($"§ERROR§► STATUS: {unmet} REQUIREMENT(S) NOT MET§END§");
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
            string plantName = GetPlantDisplayName(pot.PlantCode);

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

            // LED OK: same special case when system OFF but stress% is in (0,100).
            result.LedOk = false;
            if (pot.LedSystemState == LedSystemState.Off)
            {
                int consecutiveDays = pot.GetConsecutiveLedDays();
                int maxDaysForFullStress = _potSystemConfig != null ? _potSystemConfig.MaxDaysForFullStress : 5;
                float stressPercentage = Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f;
                bool stressInOptimalRange = stressPercentage > 0f && stressPercentage < 100f;
                result.LedOk = stressInOptimalRange;
            }
            else
            {
                result.LedOk = stageReq.IsLedRequirementMet(pot.LedSystemState);
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
            const int innerWidth = 75;

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

            string Top(string title)
            {
                string label = $"═ {title} ";
                int fill = Mathf.Max(0, innerWidth - label.Length);
                return "╔" + label + new string('═', fill) + "╗";
            }

            string Bottom() => "╚" + new string('═', innerWidth) + "╝";

            void Line(string content)
            {
                int padding = Mathf.Max(0, innerWidth - 1 - VisibleLen(content));
                string pad = new string('\u00A0', padding);
                AppendRawLine("║ " + content + pad + "║");
            }

            string CmdLine(string cmd, string desc)
            {
                int cmdCol = 22;
                int pad = Mathf.Max(0, cmdCol - VisibleLen(cmd));
                return $"  {cmd}{new string(' ', pad)}- {desc}";
            }

            AppendRawLine(Top("AVAILABLE COMMANDS"));
            Line("");
            Line("§TITLE§▸ POT MANAGEMENT & MONITORING§END§");
            Line(CmdLine("§CMD§STATUS§END§", "§TITLE§Display all POT status & vitals§END§"));
            Line(CmdLine("§CMD§FORECAST§END§ or §CMD§[F]§END§", "§TITLE§Monitoring Live Forecast (stage prediction)§END§"));
            Line(CmdLine("§CMD§OPEN [POT-ID]§END§", "§TITLE§Open detailed POT analysis screen§END§"));
            Line(CmdLine("§CMD§NOTE [POT-ID]§END§", "§TITLE§Open POT diary notes viewer§END§"));
            Line(CmdLine("§CMD§PLANT [POT-ID]§END§", "§TITLE§Queue planting action (1 AP)§END§"));
            Line(CmdLine("§CMD§UPROOT [POT-ID]§END§", "§TITLE§Queue plant removal (1 AP)§END§"));
            Line("");
            Line("§PURPLE§▸ CULTIVATION OPERATIONS§END§");
            Line(CmdLine("§CMD§WATERING [POT-ID]§END§", "§TITLE§Toggle watering system ON/OFF (1 AP)§END§"));
            Line(CmdLine("§CMD§SPRAY [POT-ID]§END§", "§TITLE§Queue additive application (1 AP)§END§"));
            Line(CmdLine("§CMD§FERTILIZE [POT-ID]§END§", "§TITLE§Queue nutrient boost (1 AP)§END§"));
            Line(CmdLine("§CMD§PRUNE [POT-ID]§END§", "§TITLE§Queue pruning operation (1 AP)§END§"));
            Line(CmdLine("§CMD§LED RED [POT-ID]§END§", "§TITLE§Toggle red light spectrum ON/OFF (1 AP)§END§"));
            Line(CmdLine("§CMD§LED BLUE [POT-ID]§END§", "§TITLE§Toggle blue light spectrum ON/OFF (1 AP)§END§"));
            Line(CmdLine("§CMD§HARVEST [POT-ID]§END§", "§TITLE§Queue harvest operation (1 AP)§END§"));
            Line("");
            Line("§WARN§▸ SYSTEM CONTROLS§END§");
            Line(CmdLine("§CMD§PROTOCOL§END§", "§TITLE§View Biological Protocol DOME_02§END§"));
            Line(CmdLine("§CMD§QUEUE SHOW§END§", "§TITLE§Show queued actions (console)§END§"));
            Line(CmdLine("§CMD§START§END§", "§TITLE§Display this command reference§END§"));
            Line(CmdLine("§CMD§CLEAR§END§", "§TITLE§Clear action queue§END§"));
            Line(CmdLine("§CMD§CLOSE§END§", "§TITLE§Close detailed POT analysis§END§"));
            Line(CmdLine("§CMD§EXIT§END§", "§TITLE§Close terminal (asks Y/N if queue exists)§END§"));
            Line("");
            AppendRawLine(Bottom());
            AppendRawLine("");
        }

        private void PrintStatusTable()
        {
            var pots = FindPots();
            AppendRawLine("╔═ POT STATUS OVERVIEW ═════════════════════════════════════════════════════╗");
            AppendRawLine("║ ID       │ STATUS     │ PLANT NAME          │ STAGE        │ COND   │ HYDR ║");
            AppendRawLine("╟──────────┼────────────┼─────────────────────┼──────────────┼────────┼──────╢");

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
                    status = "§TITLE§EMPTY§END§";
                }
                else
                {
                    plantName = GetPlantDisplayName(state.PlantCode);
                    stage = PlantStageLabel(state.Stage);
                    int score = state.ConditionScore;
                    bool isCritical = score < 40;
                    status = isCritical ? "§ERROR§CRITICAL§END§" : "§DATA§OCCUPIED§END§";
                    condition = isCritical ? $"§ERROR§{score}%§END§" : $"§TITLE§{score}%§END§";

                    // TODO: in step successivo, usare PlantCardCalculators per percentuale reale.
                    int percentHyd = Mathf.Clamp(state.Hydration * 10, 0, 100);
                    int maxDots = 5;
                    int filled = Mathf.Clamp(Mathf.RoundToInt(percentHyd / 20f), 0, maxDots);
                    var filledDots = $"§TITLE§{new string('●', filled)}§END§";
                    var emptyDots = $"<color=#888888>{new string('○', maxDots - filled)}</color>";
                    hydDots = filledDots + emptyDots;
                }

                AppendRawLine($"║ {potId,-8} │ {status,-10} │ {plantName,-19} │ {stage,-12} │ {condition,-6} │ {hydDots,-4} ║");
            }

            AppendRawLine("╚═══════════════════════════════════════════════════════════════════════════╝");
            AppendRawLine("");
        }

        private void OpenDetail(string potId, bool diaryOnly)
        {
            var pot = FindPotById(potId);
            if (pot == null)
            {
                AppendRawLine("§ERROR§⚠ ERROR: POT ID NOT FOUND.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            var state = pot.PotActions != null ? pot.PotActions.PotState : null;
            if (state == null || state.IsEmpty || !state.HasPlant)
            {
                AppendRawLine($"§WARN§⚠ WARNING: {potId} IS EMPTY. NO DATA TO DISPLAY.§END§");
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
                Debug.LogError("PlantCardV3TerminalController: _detailPageTemplate non assegnato!");
                AppendRawLine("§ERROR§⚠ ERROR: DETAIL PAGE TEMPLATE NOT ASSIGNED.§END§");
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
                    Debug.LogError("PlantCardV3TerminalController: Template non contiene pcv3-detail-page!");
                    AppendRawLine("§ERROR§⚠ ERROR: INVALID DETAIL PAGE TEMPLATE.§END§");
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

            AppendRawLine("§INFO§Type CLOSE to return...§END§");
            AppendRawLine("");
            FlushConsole();
        }

        private void PopulateDetailPage(VisualElement detailPage, PotSlot pot, PotStateModel state, bool diaryOnly)
        {
            if (detailPage == null || state == null) return;

            var plantData = state.GetPlantData();
            bool hasCondition = TryGetCondition(state, plantData, out int conditionScore, out string conditionName);

            // Helper: dotted leaders
            static string Leader(string key, string value, int dots = 14)
            {
                if (string.IsNullOrEmpty(key)) key = "---";
                if (string.IsNullOrEmpty(value)) value = "---";
                string k = key;
                int pad = Mathf.Max(1, dots - k.Length);
                return $"{k}{new string('.', pad)}: {value}";
            }

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
            string plantName = GetPlantDisplayName(state.PlantCode).ToUpperInvariant();
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
                phDriftLine.text = $"pH DRIFT.........: <color=#D35F5F>{phDriftValue}</color>";
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
                string researchText = plantData != null && !string.IsNullOrWhiteSpace(plantData.ActivePower)
                    ? plantData.ActivePower
                    : (plantData != null && !string.IsNullOrWhiteSpace(plantData.Description)
                        ? plantData.Description
                        : "No research data available.");
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
                PlantDiaryManager.Instance.AddNote(state.PotId, new PlantDiaryNotes.DiaryNote(currentDay, text));

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
                AppendRawLine("§ERROR§⚠ ERROR: POT ID NOT FOUND.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }
            var state = pot.PotActions != null ? pot.PotActions.PotState : null;
            if (state == null || state.IsEmpty || !state.HasPlant)
            {
                AppendRawLine($"§ERROR§⚠ ERROR: {potId} IS EMPTY. NOTHING TO UPROOT.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            _pendingConfirmAction = new QueuedAction
            {
                Type = QueuedActionType.Uproot,
                PotId = potId,
                TargetLabel = GetPlantDisplayName(state.PlantCode),
                ApCost = 1
            };

            _inputState = InputState.ConfirmingActionToQueue;
            AppendRawLine("§TITLE§▸ CONFIRM ACTION§END§");
            AppendRawLine("╔═══════════════════════════════════════════════════════════════════════════╗");
            AppendRawLine("║ Action:  §DATA§UPROOT§END§                                                        ║");
            AppendRawLine($"║ Target:  §DATA§{potId}§END§                                                      ║");
            AppendRawLine($"║ Plant:   §DATA§{_pendingConfirmAction.TargetLabel}§END§                           ║");
            AppendRawLine("║ AP Cost: §VAL§1 AP§END§                                                            ║");
            AppendRawLine("╚═══════════════════════════════════════════════════════════════════════════╝");
            AppendRawLine("§INFO§Confirm? [§CMD§Y§INFO§/§CMD§N§INFO§]§END§");
            AppendRawLine("");
            FlushConsole();
        }

        private void BeginConfirmToggleAction(QueuedActionType type, string potId)
        {
            var pot = FindPotById(potId);
            if (pot == null)
            {
                AppendRawLine("§ERROR§⚠ ERROR: POT ID NOT FOUND.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }
            var state = pot.PotActions != null ? pot.PotActions.PotState : null;

            if (type == QueuedActionType.HydrationToggle)
            {
                bool hasPlantNow = state != null && state.HasPlant;
                bool hasPlantQueued = _queue.Exists(a => a != null && a.Type == QueuedActionType.Plant && string.Equals(a.PotId, potId, StringComparison.OrdinalIgnoreCase));
                var runner = FindObjectOfType<Sporae.Dome.PotAutomation.PotAutomationRunner>();
                bool hasPlantPending = runner != null && runner.HasPlantPendingOrRunning(potId);

                if (!hasPlantNow && !hasPlantQueued && !hasPlantPending)
                {
                    AppendRawLine($"§ERROR§⚠ ERROR: {potId} IS EMPTY. PLANT FIRST.§END§");
                    AppendRawLine("");
                    FlushConsole();
                    return;
                }
            }

            if (type != QueuedActionType.Harvest && type != QueuedActionType.Uproot && type != QueuedActionType.HydrationToggle
                && (state == null || state.IsEmpty || !state.HasPlant))
            {
                AppendRawLine($"§ERROR§⚠ ERROR: {potId} IS EMPTY.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }
            if (type == QueuedActionType.Harvest && (state == null || state.IsEmpty || !state.HasPlant))
            {
                AppendRawLine($"§ERROR§⚠ ERROR: {potId} IS EMPTY. NOTHING TO HARVEST.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            _pendingConfirmAction = new QueuedAction
            {
                Type = type,
                PotId = potId,
                TargetLabel = state != null ? GetPlantDisplayName(state.PlantCode) : "---",
                ApCost = 1
            };

            _inputState = InputState.ConfirmingActionToQueue;
            AppendRawLine("§TITLE§▸ CONFIRM ACTION§END§");
            AppendRawLine("╔═══════════════════════════════════════════════════════════════════════════╗");
            AppendRawLine($"║ Action:  §DATA§{GetActionLabel(type)}§END§                                                     ║");
            AppendRawLine($"║ Target:  §DATA§{potId}§END§                                                      ║");
            if (type == QueuedActionType.HydrationToggle)
            {
                bool isOn = pot.PotActions != null && pot.PotActions.IsWateringSystemOn();
                string status = isOn ? "ON" : "OFF";
                AppendRawLine($"║ System:  §DATA§{status}§END§                                                     ║");
            }
            if (type == QueuedActionType.LedRedToggle || type == QueuedActionType.LedBlueToggle)
            {
                bool ledOn = pot.PotActions != null && pot.PotActions.IsLedSystemOn();
                string status = ledOn ? "ON" : "OFF";
                AppendRawLine($"║ System:  §DATA§{status}§END§                                                     ║");
                var ledState = pot.PotActions != null ? pot.PotActions.GetLedSystemState() : LedSystemState.Off;
                string stateLabel = ledState.ToString().ToUpperInvariant();
                AppendRawLine($"║ State:   §DATA§{stateLabel}§END§                                                     ║");
            }
            AppendRawLine("║ AP Cost: §VAL§1 AP§END§                                                            ║");
            AppendRawLine("╚═══════════════════════════════════════════════════════════════════════════╝");
            AppendRawLine("§INFO§Confirm? [§CMD§Y§INFO§/§CMD§N§INFO§]§END§");
            AppendRawLine("");
            FlushConsole();
        }

        private void BeginSelectItemForAction(QueuedActionType type, string potId)
        {
            var pot = FindPotById(potId);
            if (pot == null)
            {
                AppendRawLine("§ERROR§⚠ ERROR: POT ID NOT FOUND.§END§");
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
                AppendRawLine($"§ERROR§⚠ ERROR: {potId} IS EMPTY. PLANT FIRST.§END§");
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

            _selection = new SelectionContext { Type = type, PotId = potId, OptionsTypeIds = options };
            _inputState = InputState.SelectingItem;

            AppendRawLine("§TITLE§▸ SELECT ITEM FROM INVENTORY§END§");
            AppendRawLine("╔═ AVAILABLE ITEMS ═════════════════════════════════════════════════════════╗");
            for (int i = 0; i < options.Count; i++)
            {
                string typeId = options[i];
                int qty = GetAvailableQuantity(typeId);
                string displayName = GetItemDisplayName(typeId);
                AppendRawLine($"║  §CMD§{i + 1}.§END§ §DATA§{displayName}§END§   Quantity: {qty}                                     ║");
            }
            AppendRawLine("╚═══════════════════════════════════════════════════════════════════════════╝");
            AppendRawLine("§INFO§Type item number or §CMD§N§INFO§ to cancel§END§");
            AppendRawLine("");
            FlushConsole();
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
                AppendRawLine("§ERROR§⚠ INVALID SELECTION. TYPE A NUMBER OR N§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            idx -= 1;
            if (idx < 0 || idx >= _selection.OptionsTypeIds.Count)
            {
                AppendRawLine("§ERROR§⚠ INVALID SELECTION.§END§");
                AppendRawLine("");
                FlushConsole();
                return;
            }

            string chosen = _selection.OptionsTypeIds[idx];
            // Converti in nome leggibile per la visualizzazione
            string chosenDisplayName = GetItemDisplayName(chosen);

            _pendingConfirmAction = new QueuedAction
            {
                Type = _selection.Type,
                PotId = _selection.PotId,
                TargetLabel = chosenDisplayName,
                ApCost = 1,
                ItemTypeId = chosen
            };

            _selection = null;
            _inputState = InputState.ConfirmingActionToQueue;

            AppendRawLine($"§TITLE§✓ SELECTED: {chosenDisplayName}§END§");
            AppendRawLine("§TITLE§▸ CONFIRM ACTION§END§");
            AppendRawLine("╔═══════════════════════════════════════════════════════════════════════════╗");
            AppendRawLine($"║ Action:  §DATA§{GetActionLabel(_pendingConfirmAction.Type)}§END§                                                   ║");
            AppendRawLine($"║ Target:  §DATA§{_pendingConfirmAction.PotId}§END§                                                      ║");
            AppendRawLine($"║ Item:    §DATA§{chosenDisplayName}§END§                                                     ║");
            AppendRawLine("║ AP Cost: §VAL§1 AP§END§                                                            ║");
            AppendRawLine("╚═══════════════════════════════════════════════════════════════════════════╝");
            AppendRawLine("§INFO§Confirm? [§CMD§Y§INFO§/§CMD§N§INFO§]§END§");
            AppendRawLine("");
            FlushConsole();
        }

        private List<string> GetInventoryOptions(QueuedActionType type)
        {
            var list = new List<string>();

            if (type == QueuedActionType.Plant)
            {
                if (GetAvailableQuantity(Items.Seed001) > 0) list.Add(Items.Seed001);
                if (GetAvailableQuantity(Items.Seed002) > 0) list.Add(Items.Seed002);
                if (GetAvailableQuantity(Items.Seed003) > 0) list.Add(Items.Seed003);
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
                if (GetAvailableQuantity(Items.SprayAntifungal) > 0) list.Add(Items.SprayAntifungal);
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

        private void HandleConfirmToQueue(string upper)
        {
            if (upper == "Y" || upper == "YES")
            {
                if (_pendingConfirmAction != null)
                {
                    _queue.Add(_pendingConfirmAction);
                    RebuildReservedItems();
                    AppendRawLine("§TITLE§✓ ACTION QUEUED SUCCESSFULLY§END§");
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
                AppendRawLine("§WARN§⚠ ACTION CANCELLED§END§");
                AppendRawLine("");
                _pendingConfirmAction = null;
                _inputState = InputState.Idle;
                FlushConsole();
                return;
            }

            AppendRawLine("§ERROR§⚠ INVALID INPUT. TYPE Y OR N§END§");
            AppendRawLine("");
            FlushConsole();
        }

        private void HandleConfirmExecuteOrDiscard(string upper)
        {
            if (upper == "Y" || upper == "YES")
            {
                AppendRawLine("§TITLE§✓ TERMINAL SESSION CLOSING§END§");
                AppendRawLine("§INFO§Returning action queue for sequence confirmation...§END§");
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
                AppendRawLine("§WARN§⚠ QUEUE DISCARDED§END§");
                AppendRawLine("");
                FlushConsole();
                _queue.Clear();
                RebuildReservedItems();
                _inputState = InputState.Idle;
                Close();
                return;
            }

            AppendRawLine("§ERROR§⚠ INVALID INPUT. TYPE Y OR N§END§");
            AppendRawLine("");
            FlushConsole();
        }

        private void TryStartAutomationRunner()
        {
            // Se c'è un runner in scena, passiamo la coda e confermiamo spendendo AP.
            var runner = FindObjectOfType<Sporae.Dome.PotAutomation.PotAutomationRunner>();
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
                    string msg = $"Insufficient item {kv.Key} x{kv.Value} for queued actions";
                    AppendRawLine($"§ERROR§⚠ ERROR: INSUFFICIENT ITEM {kv.Key} x{kv.Value}§END§");
                    AppendRawLine("");
                    FlushConsole();
                    _foundation?.PostToast("POT-AUTO-ERROR", new NotificationPayload().With("message", msg));
                    return;
                }
            }

            // Consume items now (design: item consumption on confirm)
            foreach (var kv in required)
            {
                _inventory.Consume(kv.Key, kv.Value);
            }

            var batch = new System.Collections.Generic.List<Sporae.Dome.PotAutomation.PotAutomationRunner.AutomationAction>();
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

                batch.Add(mapped);
            }

            bool ok = runner.EnqueueAndRun(batch);
            if (!ok)
            {
                _foundation?.PostToast("POT-AUTO-ERROR", new NotificationPayload().With("message", "Automation failed: insufficient AP"));
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
            return type switch
            {
                QueuedActionType.HydrationToggle => "WATERING",
                QueuedActionType.LedRedToggle => "LED RED",
                QueuedActionType.LedBlueToggle => "LED BLUE",
                _ => type.ToString().ToUpperInvariant()
            };
        }

        private static string ExtractPotIdArgument(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var parts = raw.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return null;
            return parts[1].Trim().ToUpperInvariant();
        }

        private static string PlantStageLabel(int stageInt)
        {
            try
            {
                var stage = (PlantStage)stageInt;
                return stage switch
                {
                    PlantStage.Seed => "SEED",
                    PlantStage.Sprout => "SPROUT",
                    PlantStage.Growth => "GROWTH",
                    PlantStage.Flowering => "FLOWER",
                    PlantStage.HarvestReady => "RIPE",
                    PlantStage.Resting => "REST",
                    _ => stage.ToString().ToUpperInvariant()
                };
            }
            catch
            {
                return $"STAGE {stageInt}";
            }
        }

        private static string GetPlantDisplayName(string plantCode)
        {
            if (string.IsNullOrEmpty(plantCode)) return "---";
            return plantCode.Replace("PLT-", "").Replace("-", " ");
        }

        /// <summary>
        /// Converte ItemTypeId in nome leggibile. Se è un seed, usa il nome della pianta.
        /// </summary>
        private static string GetItemDisplayName(string itemTypeId)
        {
            if (string.IsNullOrEmpty(itemTypeId))
                return itemTypeId;

            // Prova a convertire in nome seed leggibile
            string seedDisplayName = SeedInventoryMenu.GetSeedDisplayName(itemTypeId);
            
            // Se è diverso dal typeId originale, significa che è stato convertito (è un seed)
            if (seedDisplayName != itemTypeId)
                return seedDisplayName;

            // Altrimenti è un item normale, mostra il typeId
            return itemTypeId;
        }

        private static string FormatPlantFamilyBadge(string plantCode)
        {
            if (string.IsNullOrEmpty(plantCode)) return "---";
            return plantCode.StartsWith("PLT-", StringComparison.OrdinalIgnoreCase)
                ? plantCode.Substring(4)
                : plantCode;
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
            var pots = new System.Collections.Generic.List<PotSlot>(FindObjectsOfType<PotSlot>());
            pots.Sort((a, b) => string.Compare(a != null ? a.PotId : "", b != null ? b.PotId : "", StringComparison.Ordinal));
            return pots;
        }

        private static PotSlot FindPotById(string potId)
        {
            if (string.IsNullOrEmpty(potId)) return null;
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
            SwitchToProtocol();
            
            AppendRawLine("§CMD§PROTOCOL§END§");
            AppendRawLine("§INFO§Type CLOSE to return...§END§");
            AppendRawLine("");
            FlushConsole();

            string protocol = TryLoadProtocolFromProjectDocs();
            StartProtocolTypewriter(protocol);
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
            // Se siamo in una sub-view, chiudiamo quella (come comando CLOSE).
            if (_protocolView != null && _protocolView.style.display != DisplayStyle.None)
            {
                AppendRawLine("§INFO§⚠ CLOSING DETAILED VIEW§END§");
                AppendRawLine("");
                SwitchToConsole();
                FlushConsole();
                return;
            }
            if (_detailView != null && _detailView.style.display != DisplayStyle.None)
            {
                AppendRawLine("§INFO§⚠ CLOSING DETAILED VIEW§END§");
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
            AppendRawLine("§TITLE§✓ TERMINAL SESSION CLOSING§END§");
            AppendRawLine("§INFO§Returning action queue for sequence confirmation...§END§");
            AppendRawLine("");
            AppendRawLine($"§DATA§Total actions queued: {_queue.Count}§END§");
            AppendRawLine($"§DATA§Total AP cost: {totalAp} AP§END§");
            AppendRawLine("");
            AppendRawLine("§WARN§⚠ CONFIRMATION REQUIRED — EXECUTE QUEUE? [Y/N]§END§");
            AppendRawLine("");
            FlushConsole();
        }

        /// <summary>
        /// Converte tag custom §X§...§END§ in rich text <color>.
        /// </summary>
        private static string ParseColors(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return string.Empty;

            // Palette dal prompt
            const string green = "#7FFF7A";
            const string blue = "#5DB6E3";
            const string purple = "#B580D1";
            const string yellow = "#E6C96F";
            const string red = "#D35F5F";
            const string cyan = "#00FFFF";

            return raw
                .Replace("§TITLE§", $"<color={green}>")
                .Replace("§CMD§", $"<color={blue}>")
                .Replace("§INFO§", $"<color={blue}>")
                .Replace("§DATA§", $"<color={cyan}>")
                .Replace("§VAL§", $"<color={yellow}>")
                .Replace("§WARN§", $"<color={yellow}>")
                .Replace("§PURPLE§", $"<color={purple}>")
                .Replace("§ERROR§", $"<color={red}>")
                .Replace("§END§", "</color>");
        }
    }
}

