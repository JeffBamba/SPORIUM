using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using _Project.Sporae.Core;
using _Project; // per Interactable
using Sporae.UI.UIToolkit.NotificationsFoundation;
using Sporae.Dome;
using Sporae.Dome.PotSystem.Growth;
using System.Collections.Generic;
using _Project.Player;
using Sporae.UI.UIToolkit.HUD.Components;

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

        [Header("Behavior")]
        [SerializeField] private bool _startVisibleInEditor = false;

        [Header("Backdrop Blur")]
        [SerializeField] private bool _useBlurredBackdrop = true;
        [SerializeField, Range(0.5f, 0.99f)] private float _backdropDimAlpha = 0.95f;
        [SerializeField, Range(2, 16)] private int _backdropDownsample = 8;
        [SerializeField, Range(1, 6)] private int _backdropBlurRadius = 2;
        [SerializeField, Range(1, 4)] private int _backdropBlurIterations = 2;

        [Header("Console Font")]
        [SerializeField] private Font _consoleMonoFont;

        [Header("Outer Glow Frame")]
        [SerializeField] private Material _outerGlowMaterial;
        [SerializeField] private bool _outerGlowLiveUpdate = false;

        [Header("Debug")]
        [SerializeField] private bool _logFocusDebug = false;

        private VisualElement _root;
        private Button _btnClose;
        private ScrollView _consoleScroll;
        private Label _consoleText;
        private ScrollView _protocolScroll;
        private Label _protocolText;
        private VisualElement _consoleView;
        private VisualElement _protocolView;
        private VisualElement _detailView;
        private TextField _input;
        private Label _inputHintOverlay;
        private VisualElement _promptRoot;
        private VisualElement _blinkCursor;
        private IVisualElementScheduledItem _blinkSchedule;
        private Label _apLabel;
        private Label _queuedLabel;
        private ScrollView _potList;
        private ScrollView _queueList;
        private Button _btnQueueClear;
        private VisualElement _backdrop;
        private VisualElement _dimOverlay;
        private Texture2D _backdropTexture;
        private VisualElement _outerGlow;
        private UiGlowFrameGenerator _outerGlowGenerator;
        private Material _outerGlowMaterialRuntime;

        private readonly StringBuilder _consoleBuffer = new();
        private bool _isVisible;

        private readonly System.Collections.Generic.List<QueuedAction> _queue = new();
        private InputState _inputState = InputState.Idle;
        private QueuedAction _pendingConfirmAction;

        private GameManager _gameManager;
        private FoundationNotificationService _foundation;
        private Inventory _inventory;

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
            _queueList = _root.Q<ScrollView>("pcv3-queue-list");
            _btnQueueClear = _root.Q<Button>("pcv3-queue-clear");
            _backdrop = _root.Q<VisualElement>("pcv3-backdrop");
            _dimOverlay = _root.Q<VisualElement>("pcv3-dim");
            _outerGlow = _root.Q<VisualElement>("pcv3-outer-glow");

            ApplyConsoleFont();

            if (_consoleText != null)
                _consoleText.enableRichText = true;
            if (_protocolText != null)
                _protocolText.enableRichText = true;

            if (_btnClose != null)
                _btnClose.clicked += RequestClose;
            if (_btnQueueClear != null)
                _btnQueueClear.clicked += () => HandleCommand("CLEAR");

            // Click anywhere on terminal should re-focus command input
            _root.RegisterCallback<MouseDownEvent>(_ => FocusInput(), TrickleDown.TrickleDown);

            if (_input != null)
            {
                _input.RegisterCallback<KeyDownEvent>(OnInputKeyDown);
                _input.RegisterValueChangedCallback(evt =>
                {
                    DebugLog("INPUT", "ValueChanged", "change", "{\"old\":\"" + (evt.previousValue ?? "") + "\",\"new\":\"" + (evt.newValue ?? "") + "\"}");
                });
                _input.RegisterCallback<FocusInEvent>(_ =>
                {
                    DebugLog("FOCUS", "InputFocus", "in", "{}");
                });
                _input.RegisterCallback<FocusOutEvent>(_ =>
                {
                    DebugLog("FOCUS", "InputFocus", "out", "{}");
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

        // #region agent log helper
        private void DebugLog(string hypothesisId, string location, string message, string dataJson)
        {
            try
            {
                System.IO.File.AppendAllText("d:\\Sporae_Build_Beta\\.cursor\\debug.log",
                    "{\"sessionId\":\"debug-session\",\"runId\":\"pre-fix\",\"hypothesisId\":\"" + hypothesisId + "\",\"location\":\"" + location + "\",\"message\":\"" + message + "\",\"data\":" + (string.IsNullOrEmpty(dataJson) ? "{}" : dataJson) + ",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n");
            }
            catch { }
        }

        private void ApplyConsoleFont()
        {
            Font font = _consoleMonoFont;
            if (font == null)
            {
                font = FindLoadedFont("PixelOperatorMono_Bold")
                    ?? FindLoadedFont("IBMPlexMono-Regular")
                    ?? FindLoadedFont("IBMPlexMono-Medium")
                    ?? FindLoadedFont("IBMPlexMono");
            }

            if (font == null) return;

            if (_consoleText != null)
                _consoleText.style.unityFont = font;
            if (_protocolText != null)
                _protocolText.style.unityFont = font;
        }

        private static Font FindLoadedFont(string containsName)
        {
            try
            {
                var fonts = Resources.FindObjectsOfTypeAll<Font>();
                foreach (var f in fonts)
                {
                    if (f == null) continue;
                    if (!string.IsNullOrEmpty(f.name) && f.name.Contains(containsName))
                        return f;
                }
            }
            catch { }
            return null;
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

                DebugLog("LAYOUT", "Snapshot", "layout", json);
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

                DebugLog("LAYOUT", "DumpResolvedLayout", "resolved", json);
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

            // Cache player mover for point&click suspension while terminal is open
            _playerMover = FindObjectOfType<PlayerClickMover2D>();
            _playerPerspectiveMover = FindObjectOfType<PlayerPerspectiveMover2D>();
            _playerMoverRouter = FindObjectOfType<PlayerMoverRouter2D>();

            if (_isVisible)
            {
                RenderWelcome();
                RefreshHeader();
                RefreshSidebar();
            }

            // Safety: se siamo nella stessa Canvas della HUD, forza PlantCardV3 dopo TopBar/BottomNav nella gerarchia.
            TryMoveAfterHud();
        }

        private void Update()
        {
            if (!_isVisible) return;

            // Allow runtime tweaking of dim alpha in Inspector.
            if (_dimOverlay != null)
            {
                _dimOverlay.style.backgroundColor = new Color(0f, 0f, 0f, Mathf.Clamp01(_backdropDimAlpha));
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
                // In MVP chiude subito. In fase parser completo: aprirà prompt conferma queue.
                RequestClose();
            }

            // Always keep command input ready without requiring clicks
            KeepInputFocused();

            RefreshHeader();
        }

        private void OnDestroy()
        {
            _outerGlowGenerator?.Dispose();
            _outerGlowGenerator = null;
            _outerGlowMaterialRuntime = null;
        }

        public void Open()
        {
            PrepareBackdrop();
            SetVisible(true);
            SwitchToConsole();
            RenderWelcome();
            RefreshHeader();
            RefreshSidebar();
            FocusInput();
            // Some Unity/UI Toolkit setups require a next-tick focus to actually stick.
            RequestRefocusSoon();

            // Capture resolved layout after UI settles (for UI Builder parity).
            _root?.schedule.Execute(() => DumpResolvedLayout("open_t0")).ExecuteLater(0);
            _root?.schedule.Execute(() => DumpResolvedLayout("open_t100")).ExecuteLater(100);
        }

        public void Close()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            _isVisible = visible;

            if (_root == null) return;

            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _root.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;

            DebugLog("FOCUS", "SetVisible", visible ? "open" : "close", "{\"visible\":" + (visible ? "true" : "false") + "}");

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
            if (!_useBlurredBackdrop || _backdrop == null || _dimOverlay == null) return;

            _dimOverlay.style.backgroundColor = new Color(0f, 0f, 0f, Mathf.Clamp01(_backdropDimAlpha));

            Texture2D src = null;
            try
            {
                src = ScreenCapture.CaptureScreenshotAsTexture();
            }
            catch
            {
                src = null;
            }

            if (src == null) return;

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
            RefreshQueueList();
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
            var root = new VisualElement();
            root.AddToClassList("pcv3-potcard");
            root.style.borderLeftWidth = 2;
            root.style.borderRightWidth = 2;
            root.style.borderTopWidth = 2;
            root.style.borderBottomWidth = 2;

            string potId = pot != null ? pot.PotId : "POT-???";
            var state = pot != null && pot.PotActions != null ? pot.PotActions.PotState : null;
            bool empty = state == null || state.IsEmpty || !state.HasPlant;
            int score = !empty ? state.ConditionScore : 0;
            bool critical = !empty && score < 40;

            if (empty)
            {
                root.AddToClassList("pcv3-potcard-empty");

                var headerBar = new VisualElement();
                headerBar.AddToClassList("pcv3-potcard-headerbar");
                var headerText = new Label(potId);
                headerText.AddToClassList("pcv3-potcard-headertext");
                headerBar.Add(headerText);
                root.Add(headerBar);

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

                root.Add(body);
                return root;
            }

            Color border = critical ? new Color(0.827f, 0.373f, 0.373f, 1f) : new Color(0.365f, 0.714f, 0.890f, 1f);
            root.style.borderLeftColor = border;
            root.style.borderRightColor = border;
            root.style.borderTopColor = border;
            root.style.borderBottomColor = border;

            // Existing (non-empty) card layout
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;

            var header = new Label($"{potId} - {GetPlantDisplayName(state.PlantCode)}");
            header.style.color = border;
            header.style.fontSize = 12;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(header);

            var line1 = new Label($"STAGE: {PlantStageLabel(state.Stage)}");
            line1.style.color = new Color(0.78f, 0.78f, 0.78f, 1f);
            line1.style.fontSize = 11;
            line1.style.marginTop = 6;
            root.Add(line1);

            var line2 = new Label($"CONDITION: {score}%");
            line2.style.color = critical ? border : new Color(0.498f, 1f, 0.478f, 1f);
            line2.style.fontSize = 11;
            root.Add(line2);

            var btn = new Button(() => HandleCommand($"OPEN {potId}"));
            btn.text = "[+] OPEN DETAIL";
            btn.style.marginTop = 8;
            btn.style.height = 28;
            btn.style.backgroundColor = new Color(0, 0, 0, 0);
            btn.style.borderLeftWidth = 2;
            btn.style.borderRightWidth = 2;
            btn.style.borderTopWidth = 2;
            btn.style.borderBottomWidth = 2;
            btn.style.borderLeftColor = border;
            btn.style.borderRightColor = border;
            btn.style.borderTopColor = border;
            btn.style.borderBottomColor = border;
            btn.style.color = border;
            btn.style.fontSize = 11;
            root.Add(btn);

            return root;
        }

        private void RefreshQueueList()
        {
            if (_queueList == null) return;
            _queueList.contentContainer.Clear();

            if (_queue.Count == 0)
            {
                var empty = new Label("No actions queued");
                empty.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                empty.style.fontSize = 11;
                _queueList.Add(empty);
                return;
            }

            foreach (var action in _queue)
            {
                if (action == null) continue;

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 6;

                var txt = new Label($"🔹 {action.Type.ToString().ToUpperInvariant()} on {action.PotId} [{action.ApCost} AP]");
                txt.style.color = new Color(0.71f, 0.50f, 0.82f, 1f);
                txt.style.fontSize = 11;
                txt.style.flexGrow = 1;
                row.Add(txt);

                var remove = new Button(() =>
                {
                    _queue.Remove(action);
                    RefreshHeader();
                    RefreshQueueList();
                });
                remove.text = "×";
                remove.style.width = 28;
                remove.style.height = 22;
                remove.style.backgroundColor = new Color(0, 0, 0, 0);
                remove.style.borderLeftWidth = 2;
                remove.style.borderRightWidth = 2;
                remove.style.borderTopWidth = 2;
                remove.style.borderBottomWidth = 2;
                remove.style.borderLeftColor = new Color(0.71f, 0.50f, 0.82f, 1f);
                remove.style.borderRightColor = new Color(0.71f, 0.50f, 0.82f, 1f);
                remove.style.borderTopColor = new Color(0.71f, 0.50f, 0.82f, 1f);
                remove.style.borderBottomColor = new Color(0.71f, 0.50f, 0.82f, 1f);
                remove.style.color = new Color(0.71f, 0.50f, 0.82f, 1f);
                row.Add(remove);

                _queueList.Add(row);
            }
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
        }

        private void SwitchToProtocol()
        {
            if (_consoleView != null) _consoleView.style.display = DisplayStyle.None;
            if (_protocolView != null) _protocolView.style.display = DisplayStyle.Flex;
            if (_detailView != null) _detailView.style.display = DisplayStyle.None;
        }

        private void RenderWelcome()
        {
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

        private void AppendRawLine(string line)
        {
            _consoleBuffer.AppendLine(ParseColors(line));
        }

        private void FlushConsole()
        {
            if (_consoleText == null) return;

            _consoleText.text = _consoleBuffer.ToString();

            // #region agent log
            try
            {
                System.IO.File.AppendAllText("d:\\Sporae_Build_Beta\\.cursor\\debug.log",
                    "{\"sessionId\":\"debug-session\",\"runId\":\"pre-fix\",\"hypothesisId\":\"SCROLL\",\"location\":\"PlantCardV3TerminalController.FlushConsole\",\"message\":\"flush\",\"data\":{\"len\":" + _consoleText.text?.Length + "},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n");
            }
            catch { }
            // #endregion

            AutoScrollConsole();
        }

        private void AutoScrollConsole()
        {
            if (_consoleScroll == null) return;
            // #region agent log
            try
            {
                System.IO.File.AppendAllText("d:\\Sporae_Build_Beta\\.cursor\\debug.log",
                    "{\"sessionId\":\"debug-session\",\"runId\":\"pre-fix\",\"hypothesisId\":\"SCROLL\",\"location\":\"PlantCardV3TerminalController.AutoScrollConsole\",\"message\":\"autoschedule\",\"data\":{\"contentHeight\":" + (_consoleScroll.contentContainer?.layout.height ?? -1f) + ",\"viewHeight\":" + (_consoleScroll.layout.height) + "},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n");
            }
            catch { }
            // #endregion

            void ScrollToBottom(string tag)
            {
                _consoleScroll.ScrollTo(_consoleText);
                var vs = _consoleScroll.verticalScroller;
                if (vs != null && vs.highValue > 0)
                {
                    vs.value = vs.highValue;
                }

                // #region agent log
                try
                {
                    var vsLog = _consoleScroll.verticalScroller;
                    float val = vsLog != null ? vsLog.value : -1f;
                    System.IO.File.AppendAllText("d:\\Sporae_Build_Beta\\.cursor\\debug.log",
                        "{\"sessionId\":\"debug-session\",\"runId\":\"pre-fix\",\"hypothesisId\":\"SCROLL\",\"location\":\"PlantCardV3TerminalController.AutoScrollConsole\",\"message\":\"scrollto_" + tag + "\",\"data\":{\"vscroll\":" + val + "},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n");
                }
                catch { }
                // #endregion
            }

            _consoleScroll.schedule.Execute(() => ScrollToBottom("t0")).ExecuteLater(0);
            _consoleScroll.schedule.Execute(() => ScrollToBottom("t20")).ExecuteLater(20);
            _consoleScroll.schedule.Execute(() => ScrollToBottom("t60")).ExecuteLater(60);

            // Layout snapshot after scheduling scrolls (current frame)
            LogLayoutSnapshot("autoschedule");
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

            DebugLog("INPUT", "OnInputKeyDown", "enter", "{\"key\":\"" + evt.keyCode + "\",\"valBefore\":\"" + (_input != null ? _input.value : "") + "\"}");

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

            DebugLog("CMD", "HandleCommand", "start", "{\"raw\":\"" + trimmed + "\",\"upper\":\"" + upper + "\",\"state\":\"" + _inputState + "\"}");

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

            AppendRawLine($"> {trimmed}");
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

            if (upper.StartsWith("HYDRATION"))
            {
                string potId = ExtractPotIdArgument(trimmed);
                if (string.IsNullOrEmpty(potId))
                {
                    AppendRawLine("§ERROR§⚠ ERROR: POT ID REQUIRED. USAGE: HYDRATION [POT-ID]§END§");
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

            if (upper == "FORECAST" || upper == "F")
            {
                AppendRawLine("§WARN§Not implemented yet§END§");
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
            Line(CmdLine("§CMD§HYDRATION [POT-ID]§END§", "§TITLE§Toggle hydration system ON/OFF (1 AP)§END§"));
            Line(CmdLine("§CMD§SPRAY [POT-ID]§END§", "§TITLE§Queue additive application (1 AP)§END§"));
            Line(CmdLine("§CMD§FERTILIZE [POT-ID]§END§", "§TITLE§Queue nutrient boost (1 AP)§END§"));
            Line(CmdLine("§CMD§PRUNE [POT-ID]§END§", "§TITLE§Queue pruning operation (1 AP)§END§"));
            Line(CmdLine("§CMD§LED RED [POT-ID]§END§", "§TITLE§Toggle red light spectrum ON/OFF (1 AP)§END§"));
            Line(CmdLine("§CMD§LED BLUE [POT-ID]§END§", "§TITLE§Toggle blue light spectrum ON/OFF (1 AP)§END§"));
            Line(CmdLine("§CMD§HARVEST [POT-ID]§END§", "§TITLE§Queue harvest operation (1 AP)§END§"));
            Line("");
            Line("§WARN§▸ SYSTEM CONTROLS§END§");
            Line(CmdLine("§CMD§PROTOCOL§END§", "§TITLE§View Biological Protocol DOME_02§END§"));
            Line(CmdLine("§CMD§START§END§", "§TITLE§Display this command reference§END§"));
            Line(CmdLine("§CMD§CLEAR§END§", "§TITLE§Clear action queue§END§"));
            Line(CmdLine("§CMD§CLOSE§END§", "§TITLE§Close detailed POT analysis§END§"));
            Line(CmdLine("§CMD§EXIT§END§", "§TITLE§Close terminal & confirm sequence§END§"));
            Line("");
            AppendRawLine(Bottom());
            AppendRawLine("");
        }

        private void PrintStatusTable()
        {
            var pots = FindPots();
            AppendRawLine("╔═ POT STATUS OVERVIEW ═════════════════════════════════════════════════════╗");
            AppendRawLine("║ ID       │ STATUS     │ PLANT NAME          │ STAGE        │ HEALTH │ HYDR ║");
            AppendRawLine("╟──────────┼────────────┼─────────────────────┼──────────────┼────────┼──────╢");

            foreach (var pot in pots)
            {
                string potId = pot != null ? pot.PotId : "POT-???";
                var state = pot != null && pot.PotActions != null ? pot.PotActions.PotState : null;

                string status;
                string plantName = "---";
                string stage = "---";
                string health = "---";
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
                    health = isCritical ? $"§ERROR§{score}%§END§" : $"§TITLE§{score}%§END§";

                    // TODO: in step successivo, usare PlantCardCalculators per percentuale reale.
                    int percentHyd = Mathf.Clamp(state.Hydration * 10, 0, 100);
                    int maxDots = 5;
                    int filled = Mathf.Clamp(Mathf.RoundToInt(percentHyd / 20f), 0, maxDots);
                    var filledDots = $"§TITLE§{new string('●', filled)}§END§";
                    var emptyDots = $"<color=#888888>{new string('○', maxDots - filled)}</color>";
                    hydDots = filledDots + emptyDots;
                }

                AppendRawLine($"║ {potId,-8} │ {status,-10} │ {plantName,-19} │ {stage,-12} │ {health,-6} │ {hydDots,-4} ║");
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

            var placeholder = _root.Q<Label>("pcv3-detail-placeholder");
            if (placeholder != null)
            {
                string plantName = GetPlantDisplayName(state.PlantCode);
                placeholder.text = diaryOnly
                    ? $"POT DIARY (TODO) — {potId} — {plantName}"
                    : $"DETAILED ANALYSIS (TODO) — {potId} — {plantName}";
            }

            AppendRawLine("§INFO§Type CLOSE to return...§END§");
            AppendRawLine("");
            FlushConsole();
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

            if (type != QueuedActionType.Harvest && type != QueuedActionType.Uproot && (state == null || state.IsEmpty || !state.HasPlant))
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
            AppendRawLine($"║ Action:  §DATA§{type.ToString().ToUpperInvariant()}§END§                                                     ║");
            AppendRawLine($"║ Target:  §DATA§{potId}§END§                                                      ║");
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
                int qty = GetQuantity(typeId);
                AppendRawLine($"║  §CMD§{i + 1}.§END§ §DATA§{typeId}§END§   Quantity: {qty}                                     ║");
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

            _pendingConfirmAction = new QueuedAction
            {
                Type = _selection.Type,
                PotId = _selection.PotId,
                TargetLabel = chosen,
                ApCost = 1,
                ItemTypeId = chosen
            };

            _selection = null;
            _inputState = InputState.ConfirmingActionToQueue;

            AppendRawLine($"§TITLE§✓ SELECTED: {chosen}§END§");
            AppendRawLine("§TITLE§▸ CONFIRM ACTION§END§");
            AppendRawLine("╔═══════════════════════════════════════════════════════════════════════════╗");
            AppendRawLine($"║ Action:  §DATA§{_pendingConfirmAction.Type.ToString().ToUpperInvariant()}§END§                                                   ║");
            AppendRawLine($"║ Target:  §DATA§{_pendingConfirmAction.PotId}§END§                                                      ║");
            AppendRawLine($"║ Item:    §DATA§{chosen}§END§                                                     ║");
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
                if (_inventory.Has(Items.Seed001)) list.Add(Items.Seed001);
                if (_inventory.Has(Items.Seed002)) list.Add(Items.Seed002);
                if (_inventory.Has(Items.Seed003)) list.Add(Items.Seed003);
                return list;
            }
            if (type == QueuedActionType.Fertilize)
            {
                if (_inventory.Has(Items.FertilizerStandard)) list.Add(Items.FertilizerStandard);
                if (_inventory.Has(Items.FertilizerPure)) list.Add(Items.FertilizerPure);
                if (_inventory.Has(Items.FertilizerProhibited)) list.Add(Items.FertilizerProhibited);
                return list;
            }
            if (type == QueuedActionType.Spray)
            {
                if (_inventory.Has(Items.AdditiveBasic)) list.Add(Items.AdditiveBasic);
                if (_inventory.Has(Items.AdditiveAcid)) list.Add(Items.AdditiveAcid);
                if (_inventory.Has(Items.SprayAntifungal)) list.Add(Items.SprayAntifungal);
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

        private void HandleConfirmToQueue(string upper)
        {
            if (upper == "Y" || upper == "YES")
            {
                if (_pendingConfirmAction != null)
                {
                    _queue.Add(_pendingConfirmAction);
                    AppendRawLine("§TITLE§✓ ACTION QUEUED SUCCESSFULLY§END§");
                    AppendRawLine($"§INFO§+ {_pendingConfirmAction.Type.ToString().ToUpperInvariant()} on {_pendingConfirmAction.PotId} [1 AP]§END§");
                    AppendRawLine("");
                }
                _pendingConfirmAction = null;
                _inputState = InputState.Idle;
                FlushConsole();
                RefreshHeader();
                RefreshQueueList();
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
            // MVP: se c'è un runner in scena, passiamo la coda (solo UPROOT per ora) e confermiamo spendendo AP.
            var runner = FindObjectOfType<Sporae.Dome.PotAutomation.PotAutomationRunner>();
            if (runner == null)
            {
                // Fallback: niente runner, quindi discard per non creare incoerenze (AP non spesi).
                AppendRawLine("§WARN§⚠ Automation runner not found in scene. Queue discarded.§END§");
                AppendRawLine("");
                _queue.Clear();
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
                    AppendRawLine($"§ERROR§⚠ ERROR: INSUFFICIENT ITEM {kv.Key} x{kv.Value}§END§");
                    AppendRawLine("");
                    FlushConsole();
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

            _queue.Clear();
            RefreshHeader();
            RefreshQueueList();

            runner.EnqueueBatch(batch);
            bool ok = runner.ConfirmAndRun();
            if (!ok)
            {
                AppendRawLine("§ERROR§⚠ Automation could not start (insufficient AP).§END§");
                AppendRawLine("");
                FlushConsole();
            }
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
                if (p != null && string.Equals(p.PotId, potId, StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            return null;
        }

        private void ShowProtocolFromDocs()
        {
            SwitchToProtocol();
            
            AppendRawLine("§CMD§PROTOCOL§END§");
            AppendRawLine("§INFO§Type CLOSE to return...§END§");
            AppendRawLine("");
            FlushConsole();

            string protocol = TryLoadProtocolFromProjectDocs();
            if (_protocolText != null)
                _protocolText.text = protocol;
            
            if (_protocolScroll != null && _protocolText != null)
                _protocolScroll.ScrollTo(_protocolText);
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

