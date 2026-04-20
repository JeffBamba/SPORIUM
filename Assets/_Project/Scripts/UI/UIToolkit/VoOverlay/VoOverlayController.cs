using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using _Project.Sporae.Core;
using UnityEngine;
using UnityEngine.UIElements;
using Sporae.DevTools;

namespace _Project.UI.UIToolkit.VoOverlay
{
    public enum VoRegister
    {
        RegisterA,
        RegisterB
    }

    public enum VoSentenceAdvanceMode
    {
        AutoReadingPause,
        ClickToContinue
    }

    public readonly struct VoLinePresentationOptions
    {
        public static VoLinePresentationOptions Default => new VoLinePresentationOptions(
            useMultiSentenceWhenSplit: true,
            VoSentenceAdvanceMode.AutoReadingPause,
            minReadSeconds: 0.55f,
            readSecondsPerChar: 0.042f,
            continueHintText: "Clicca o Spazio per continuare",
            highlightWords: null,
            highlightColorHex: null);

        public static VoLinePresentationOptions LegacySingleBlock => new VoLinePresentationOptions(
            useMultiSentenceWhenSplit: false,
            VoSentenceAdvanceMode.AutoReadingPause,
            minReadSeconds: 0.55f,
            readSecondsPerChar: 0.042f,
            continueHintText: "Clicca o Spazio per continuare",
            highlightWords: null,
            highlightColorHex: null);

        public VoLinePresentationOptions(
            bool useMultiSentenceWhenSplit,
            VoSentenceAdvanceMode advanceMode,
            float minReadSeconds,
            float readSecondsPerChar,
            string continueHintText,
            IReadOnlyList<string> highlightWords = null,
            string highlightColorHex = null,
            bool forceContinueAtEnd = true,
            bool lockWorldInputWhileVisible = false,
            bool enableCameraFocus = false,
            float cameraFocusOrthographicSize = 0f)
        {
            UseMultiSentenceWhenSplit = useMultiSentenceWhenSplit;
            AdvanceMode = advanceMode;
            MinReadSeconds = minReadSeconds;
            ReadSecondsPerChar = readSecondsPerChar;
            ContinueHintText = string.IsNullOrEmpty(continueHintText)
                ? "Clicca o Spazio per continuare"
                : continueHintText;
            HighlightWords = highlightWords;
            HighlightColorHex = string.IsNullOrWhiteSpace(highlightColorHex) ? null : highlightColorHex;
            ForceContinueAtEnd = forceContinueAtEnd;
            LockWorldInputWhileVisible = lockWorldInputWhileVisible;
            EnableCameraFocus = enableCameraFocus;
            CameraFocusOrthographicSize = cameraFocusOrthographicSize;
        }

        public bool UseMultiSentenceWhenSplit { get; }
        public VoSentenceAdvanceMode AdvanceMode { get; }
        public float MinReadSeconds { get; }
        public float ReadSecondsPerChar { get; }
        public string ContinueHintText { get; }
        public IReadOnlyList<string> HighlightWords { get; }
        public string HighlightColorHex { get; }
        public bool ForceContinueAtEnd { get; }
        public bool LockWorldInputWhileVisible { get; }
        public bool EnableCameraFocus { get; }
        public float CameraFocusOrthographicSize { get; }

        public static VoLinePresentationOptions ForDemoBeat(VoSentenceAdvanceMode advanceMode) =>
            new VoLinePresentationOptions(true, advanceMode, 0.55f, 0.042f,
                "Clicca o Spazio per continuare", null, null);

        public static VoLinePresentationOptions ForDemoBeat(VoSentenceAdvanceMode advanceMode,
            IReadOnlyList<string> highlightWords, string highlightColorHex) =>
            new VoLinePresentationOptions(true, advanceMode, 0.55f, 0.042f,
                "Clicca o Spazio per continuare", highlightWords, highlightColorHex);
    }

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-45)]
    public sealed class VoOverlayController : MonoBehaviour
    {
        private const string VisualTreeResourcePath = "UI/UIToolkit/VoOverlay/VoOverlay";
        private const string PanelSettingsResourcePath = "UI/UIToolkit/MainMenu/MainMenuPanelSettings";

        private const int SortingOrder = 650;

        // CRT glitch — ~3 volte al secondo
        private const float BlockGlitchRangePx = 0.65f;
        private const float BlockGlitchIntervalMin = 0.25f;
        private const float BlockGlitchIntervalMax = 0.35f;
        private const float FragmentWiggleProbability = 0.30f;
        private const float FragmentGlitchRangePx = 0.82f;
        private const float FragmentGlitchIntervalMin = 0.25f;
        private const float FragmentGlitchIntervalMax = 0.35f;

        // Enter/exit slide
        private const float EnterOffsetPx = 20f;
        private const float ExitOffsetPx  = 20f;

        // Cursore lampeggiante durante il typing
        private const string CursorChar = "▌";

        [Header("Audio (opzionali)")]
        [SerializeField] private AudioClip _blockStartClip;
        [SerializeField] private AudioClip _blockEndClip;
        [SerializeField] private AudioClip _typingTickClip;
        [SerializeField, Range(0.04f, 0.45f)] private float _typingTickMinInterval = 0.09f;

        [Header("Typing")]
        // 33 char/s ≈ 30ms per carattere
        [SerializeField, Range(8f, 90f)] private float _defaultCharsPerSecond = 33f;

        [Header("Message Timing")]
        [SerializeField, Range(3f, 20f)]  private float _totalMessageDuration = 10f;
        [SerializeField, Range(0.1f, 1f)] private float _enterExitDuration    = 0.5f;

        [Header("Focus (opzionale)")]
        [SerializeField, Range(0.45f, 1f)] private float _defaultFocusZoomScale = 0.72f;
        [SerializeField, Range(0.05f, 1f)] private float _focusZoomTweenSeconds = 0.25f;
        [SerializeField, Range(-1f, 1f)]   private float _focusPanOffsetY       = 0.28f;

        private UIDocument    _document;
        private VisualElement _root;
        private VisualElement _textWrap;
        private VisualElement _organicHost;
        private VisualElement _sentencesHost;
        private Label         _bodyLabel;
        private Label         _continueHint;
        private AudioSource   _audio;

        private VoRegister _activeRegister;
        private bool       _hideAfterTypingWithoutIdle;

        private Coroutine _typingRoutine;
        private Coroutine _cursorBlinkRoutine;
        private Coroutine _enterAnimRoutine;
        private Coroutine _focusTweenRoutine;

        private bool _idleGlitchActive;
        private bool _animatingEnterExit;

        // Stato cursore
        private bool                 _cursorVisible = true;
        private Label                _currentTypingLabel;
        private string               _currentTypingBasePlain = string.Empty;
        private IReadOnlyList<string> _currentTypingHighlightWords;
        private string               _currentTypingHighlightHex;

        private float _glitchStepTimer;
        private float _glitchOffX;
        private float _glitchOffY;

        private readonly List<Label>          _fragmentLabels = new List<Label>();
        private readonly List<FragmentGlitch> _fragmentGlitch = new List<FragmentGlitch>();

        private bool    _worldInputLockedByVo;
        private Camera  _focusCamera;
        private bool    _focusCameraWasOrthographic;
        private float   _focusOriginalOrthoSize;
        private float   _focusOriginalPerspectiveFov;
        private CameraFollow2D _focusFollow;
        private bool    _focusFollowUseOffset;
        private Vector3 _focusFollowOffset;

        private string _lastPlainTypedText = string.Empty;

        public bool IsShowing { get; private set; }
        public bool IsTyping  => _typingRoutine != null;

        private struct FragmentGlitch
        {
            public bool  Wiggles;
            public float StepTimer;
            public float OffX;
            public float OffY;
        }

        // ─── Unity lifecycle ─────────────────────────────────────────────────

        private void Awake()
        {
            _audio = GetComponent<AudioSource>();
            if (_audio == null)
                _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake  = false;
            _audio.spatialBlend = 0f;
            BuildUi();
        }

        private void OnDestroy() => StopAllRunningRoutines();

        private void Update()
        {
            if (_textWrap == null || !IsShowing)
                return;

            if (!_idleGlitchActive || IsTyping)
            {
                if (!_animatingEnterExit)
                    ResetAllTranslates();
                return;
            }

            StepBlockGlitch();

            for (var i = 0; i < _fragmentLabels.Count; i++)
            {
                var g = _fragmentGlitch[i];
                if (!g.Wiggles)
                {
                    _fragmentLabels[i].style.translate = new Translate(0f, 0f);
                    continue;
                }
                StepFragmentGlitch(ref g, i);
                _fragmentGlitch[i] = g;
                _fragmentLabels[i].style.translate = new Translate(
                    new Length(g.OffX, LengthUnit.Pixel),
                    new Length(g.OffY, LengthUnit.Pixel));
            }

            _textWrap.style.translate = new Translate(
                new Length(_glitchOffX, LengthUnit.Pixel),
                new Length(_glitchOffY, LengthUnit.Pixel));
        }

        // ─── Translate helpers ───────────────────────────────────────────────

        private void ResetAllTranslates()
        {
            if (_animatingEnterExit) return;
            _textWrap.style.translate = new Translate(0f, 0f);
            for (var i = 0; i < _fragmentLabels.Count; i++)
                _fragmentLabels[i].style.translate = new Translate(0f, 0f);
        }

        // ─── CRT glitch ──────────────────────────────────────────────────────

        private void StepBlockGlitch()
        {
            _glitchStepTimer -= Time.unscaledDeltaTime;
            if (_glitchStepTimer > 0f) return;

            _glitchStepTimer = UnityEngine.Random.Range(BlockGlitchIntervalMin, BlockGlitchIntervalMax);

            if (UnityEngine.Random.value < 0.15f) { _glitchOffX = 0f; _glitchOffY = 0f; return; }

            float rx  = UnityEngine.Random.Range(-BlockGlitchRangePx, BlockGlitchRangePx);
            float ry  = UnityEngine.Random.Range(-BlockGlitchRangePx, BlockGlitchRangePx);
            if (UnityEngine.Random.value < 0.09f) { rx *= 1.3f; ry *= 1.3f; }
            float cap = BlockGlitchRangePx * 1.38f;
            _glitchOffX = SnapGlitchPx(Mathf.Clamp(rx, -cap, cap));
            _glitchOffY = SnapGlitchPx(Mathf.Clamp(ry, -cap, cap));
        }

        private void StepFragmentGlitch(ref FragmentGlitch g, int index)
        {
            g.StepTimer -= Time.unscaledDeltaTime;
            if (g.StepTimer > 0f) return;

            float spread = (index % 11) * 0.035f;
            g.StepTimer = UnityEngine.Random.Range(FragmentGlitchIntervalMin + spread, FragmentGlitchIntervalMax + spread);

            if (UnityEngine.Random.value < 0.14f) { g.OffX = 0f; g.OffY = 0f; return; }

            float rx  = UnityEngine.Random.Range(-FragmentGlitchRangePx, FragmentGlitchRangePx);
            float ry  = UnityEngine.Random.Range(-FragmentGlitchRangePx, FragmentGlitchRangePx);
            if (UnityEngine.Random.value < 0.08f) { rx *= 1.28f; ry *= 1.28f; }
            float cap = FragmentGlitchRangePx * 1.36f;
            g.OffX = SnapGlitchPx(Mathf.Clamp(rx, -cap, cap));
            g.OffY = SnapGlitchPx(Mathf.Clamp(ry, -cap, cap));
        }

        private static float SnapGlitchPx(float v) => Mathf.Round(v * 2f) / 2f;

        // ─── UI build / bind ─────────────────────────────────────────────────

        private void BuildUi()
        {
            var vta = Resources.Load<VisualTreeAsset>(VisualTreeResourcePath);
            if (vta == null) { Debug.LogError($"[VoOverlay] VisualTreeAsset non trovato: {VisualTreeResourcePath}"); return; }

            var ps = Resources.Load<PanelSettings>(PanelSettingsResourcePath);
            if (ps == null) { Debug.LogError($"[VoOverlay] PanelSettings non trovato: {PanelSettingsResourcePath}"); return; }

            _document = GetComponent<UIDocument>();
            if (_document == null) _document = gameObject.AddComponent<UIDocument>();
            _document.panelSettings   = ps;
            _document.visualTreeAsset = vta;
            _document.sortingOrder    = SortingOrder;
            BindVisuals();
        }

        private void Start()
        {
            if (_document != null && (_root == null || _bodyLabel == null))
                BindVisuals();
        }

        private void BindVisuals()
        {
            if (_document == null) return;
            var ve = _document.rootVisualElement;
            if (ve == null) return;

            _root         = ve.Q<VisualElement>("vo-overlay-root");
            _textWrap     = ve.Q<VisualElement>("vo-text-wrap");
            _organicHost  = ve.Q<VisualElement>("vo-organic-host");
            _sentencesHost = ve.Q<VisualElement>("vo-sentences-host");
            _bodyLabel    = ve.Q<Label>("vo-body");
            _continueHint = ve.Q<Label>("vo-continue-hint");

            if (_bodyLabel    != null) _bodyLabel.text = string.Empty;
            if (_continueHint != null) { _continueHint.text = string.Empty; _continueHint.style.display = DisplayStyle.Flex; _continueHint.style.opacity = 0f; }
            if (_root         != null) _root.style.display = DisplayStyle.None;
            if (_textWrap     != null) { _textWrap.style.translate = new Translate(0f, 0f); _textWrap.style.opacity = 1f; }
            if (_organicHost  != null) _organicHost.style.display = DisplayStyle.None;
            if (_sentencesHost != null) _sentencesHost.style.display = DisplayStyle.None;
        }

        // ─── Enter / Exit animations ─────────────────────────────────────────

        /// <summary>Slide su dal basso + fade in in 0.5s.</summary>
        private IEnumerator EnterAnimRoutine()
        {
            _animatingEnterExit = true;
            float duration = Mathf.Max(0.05f, _enterExitDuration);
            float t = 0f;
            while (t < duration && _textWrap != null)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                _textWrap.style.opacity = s;
                _textWrap.style.translate = new Translate(
                    new Length(0f),
                    new Length(Mathf.Lerp(EnterOffsetPx, 0f, s), LengthUnit.Pixel));
                yield return null;
            }
            if (_textWrap != null) { _textWrap.style.opacity = 1f; _textWrap.style.translate = new Translate(0f, 0f); }
            _animatingEnterExit = false;
            _enterAnimRoutine   = null;
        }

        /// <summary>Slide verso l'alto + fade out in 0.5s. Da usare con yield.</summary>
        private IEnumerator ExitAnimRoutine()
        {
            _animatingEnterExit = true;
            if (_enterAnimRoutine != null) { StopCoroutine(_enterAnimRoutine); _enterAnimRoutine = null; }
            float duration = Mathf.Max(0.05f, _enterExitDuration);
            float t = 0f;
            while (t < duration && _textWrap != null)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                _textWrap.style.opacity = 1f - s;
                _textWrap.style.translate = new Translate(
                    new Length(0f),
                    new Length(-ExitOffsetPx * s, LengthUnit.Pixel));
                yield return null;
            }
            if (_textWrap != null) { _textWrap.style.opacity = 0f; _textWrap.style.translate = new Translate(0f, 0f); }
            _animatingEnterExit = false;
        }

        // ─── Cursore lampeggiante ─────────────────────────────────────────────

        private void StartCursorBlink(Label targetLabel, IReadOnlyList<string> highlightWords, string highlightHex)
        {
            StopCursorBlink();
            _currentTypingLabel          = targetLabel;
            _currentTypingHighlightWords = highlightWords;
            _currentTypingHighlightHex   = highlightHex;
            _currentTypingBasePlain      = string.Empty;
            _cursorVisible               = true;
            _cursorBlinkRoutine          = StartCoroutine(CursorBlinkRoutine());
        }

        private void StopCursorBlink()
        {
            if (_cursorBlinkRoutine != null) { StopCoroutine(_cursorBlinkRoutine); _cursorBlinkRoutine = null; }
            _cursorVisible = false;
            // Rimuovi il cursore dalla label
            if (_currentTypingLabel != null)
                _currentTypingLabel.text = ApplyHighlight(_currentTypingBasePlain, _currentTypingHighlightWords, _currentTypingHighlightHex);
            _currentTypingLabel = null;
        }

        private IEnumerator CursorBlinkRoutine()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(0.35f);
                _cursorVisible = !_cursorVisible;
                RefreshCursorOnLabel();
            }
        }

        /// <summary>Aggiorna il testo della label con o senza cursore in base a _cursorVisible.</summary>
        private void RefreshCursorOnLabel()
        {
            if (_currentTypingLabel == null) return;
            string cursor = _cursorVisible ? "<color=#FFFFFF>" + CursorChar + "</color>" : string.Empty;
            _currentTypingLabel.text = ApplyHighlight(_currentTypingBasePlain, _currentTypingHighlightWords, _currentTypingHighlightHex) + cursor;
        }

        /// <summary>Chiamato ad ogni carattere digitato: aggiorna il testo base e ridisegna con cursore.</summary>
        private void SetTypingText(string plainText)
        {
            _currentTypingBasePlain = plainText;
            RefreshCursorOnLabel();
        }

        // ─── ShowLine / Hide ─────────────────────────────────────────────────

        public void ShowLine(string text, VoRegister register, float? charsPerSecond = null, Action onComplete = null, bool hideAfterTypingWithoutIdle = false)
            => ShowLine(text, register, charsPerSecond, onComplete, hideAfterTypingWithoutIdle, VoLinePresentationOptions.Default);

        public void ShowLine(string text, VoRegister register, float? charsPerSecond, Action onComplete, bool hideAfterTypingWithoutIdle, VoLinePresentationOptions presentation)
        {
            if (_bodyLabel == null || _root == null)
            {
#if UNITY_EDITOR
                SporiumLogger.LogWarning(LogCategory.UI, "[VoOverlay] UI non pronta — ShowLine ignorato.");
#endif
                return;
            }

            StopAllRunningRoutines();
            ReleasePresentationRuntime();

            _hideAfterTypingWithoutIdle = hideAfterTypingWithoutIdle;
            _activeRegister = register;
            ApplyRegisterClass(register);
            _bodyLabel.text = string.Empty;
            ClearOrganicLayout();
            ClearSentenceHost();
            HideContinueHint();
            _idleGlitchActive   = false;
            _animatingEnterExit = false;
            _glitchOffX = _glitchOffY = 0f;
            _glitchStepTimer = 0f;

            // Stato iniziale animazione entrata
            if (_textWrap != null)
            {
                _textWrap.style.opacity   = 0f;
                _textWrap.style.translate = new Translate(new Length(0f), new Length(EnterOffsetPx, LengthUnit.Pixel));
            }

            _root.style.display = DisplayStyle.Flex;
            _root.style.opacity = 1f;
            IsShowing = true;
            ApplyPresentationRuntime(presentation);

            _enterAnimRoutine = StartCoroutine(EnterAnimRoutine());

            if (_blockStartClip != null) _audio.PlayOneShot(_blockStartClip);

            var fullText  = text ?? string.Empty;
            float cps     = charsPerSecond ?? _defaultCharsPerSecond;
            var sentences = SplitIntoSentences(fullText);
            bool useMulti = presentation.UseMultiSentenceWhenSplit && sentences.Count > 1;

            if (useMulti && _sentencesHost != null)
            {
                _bodyLabel.style.display    = DisplayStyle.None;
                _sentencesHost.style.display = DisplayStyle.Flex;
                _typingRoutine = StartCoroutine(MultiSentenceRoutine(sentences, cps, presentation, onComplete));
            }
            else
            {
                if (_sentencesHost != null) _sentencesHost.style.display = DisplayStyle.None;
                if (_bodyLabel     != null) _bodyLabel.style.display     = DisplayStyle.Flex;
                _typingRoutine = StartCoroutine(TypeLineRoutine(fullText, cps, presentation, onComplete));
            }
        }

        public void Hide()
        {
            StopAllRunningRoutines();
            ReleasePresentationRuntime();
            _idleGlitchActive   = false;
            _animatingEnterExit = false;
            _glitchOffX = _glitchOffY = 0f;
            _glitchStepTimer = 0f;
            ClearOrganicLayout();
            if (_textWrap != null) { _textWrap.style.translate = new Translate(0f, 0f); _textWrap.style.opacity = 1f; }
            if (_root     != null) { _root.style.display = DisplayStyle.None; _root.style.opacity = 1f; IsShowing = false; }
            if (_bodyLabel != null) { _bodyLabel.text = string.Empty; _bodyLabel.style.display = DisplayStyle.Flex; }
            ClearSentenceHost();
            HideContinueHint();
        }

        private void StopAllRunningRoutines()
        {
            if (_typingRoutine      != null) { StopCoroutine(_typingRoutine);      _typingRoutine      = null; }
            if (_cursorBlinkRoutine != null) { StopCoroutine(_cursorBlinkRoutine); _cursorBlinkRoutine = null; }
            if (_enterAnimRoutine   != null) { StopCoroutine(_enterAnimRoutine);   _enterAnimRoutine   = null; }
            if (_focusTweenRoutine  != null) { StopCoroutine(_focusTweenRoutine);  _focusTweenRoutine  = null; }
            _currentTypingLabel     = null;
            _currentTypingBasePlain = string.Empty;
        }

        // ─── Presentation runtime (lock input + camera) ──────────────────────

        private void ApplyPresentationRuntime(in VoLinePresentationOptions opt)
        {
            if (opt.LockWorldInputWhileVisible && !_worldInputLockedByVo)
            {
                GameplayUiModalLock.SetBlockWorldInput(true);
                _worldInputLockedByVo = true;
            }

            if (!opt.EnableCameraFocus) return;

            var cam = ResolveFocusCamera();
            if (cam == null) return;

            _focusCamera               = cam;
            _focusCameraWasOrthographic = cam.orthographic;
            float targetSize;
            if (_focusCameraWasOrthographic)
            {
                _focusOriginalOrthoSize = Mathf.Max(0.01f, cam.orthographicSize);
                targetSize = opt.CameraFocusOrthographicSize > 0f
                    ? opt.CameraFocusOrthographicSize
                    : _focusOriginalOrthoSize * Mathf.Clamp(_defaultFocusZoomScale, 0.45f, 1f);
            }
            else
            {
                _focusOriginalPerspectiveFov = Mathf.Max(1f, cam.fieldOfView);
                targetSize = _focusOriginalPerspectiveFov * Mathf.Clamp(_defaultFocusZoomScale, 0.45f, 1f);
            }

            _focusFollow = _focusCamera.GetComponent<CameraFollow2D>();
            if (_focusFollow != null)
            {
                _focusFollowUseOffset = _focusFollow.IsUsingOffset();
                _focusFollowOffset    = _focusFollow.GetOffset();
                _focusFollow.SetOffset(_focusFollowOffset + new Vector3(0f, _focusPanOffsetY, 0f));
                _focusFollow.SetUseOffset(true);
            }

            StartFocusTween(targetSize);
        }

        private void ReleasePresentationRuntime()
        {
            if (_worldInputLockedByVo) { GameplayUiModalLock.SetBlockWorldInput(false); _worldInputLockedByVo = false; }

            if (_focusFollow != null) { _focusFollow.SetOffset(_focusFollowOffset); _focusFollow.SetUseOffset(_focusFollowUseOffset); _focusFollow = null; }

            if (_focusCamera != null)
            {
                float restoreTarget = _focusCameraWasOrthographic ? _focusOriginalOrthoSize : _focusOriginalPerspectiveFov;
                StartFocusTween(restoreTarget);
            }
        }

        private void StartFocusTween(float targetSize)
        {
            if (_focusCamera == null) return;
            if (_focusTweenRoutine != null) { StopCoroutine(_focusTweenRoutine); _focusTweenRoutine = null; }
            _focusTweenRoutine = StartCoroutine(FocusTweenRoutine(targetSize));
        }

        private IEnumerator FocusTweenRoutine(float targetSize)
        {
            if (_focusCamera == null) yield break;
            float start    = _focusCameraWasOrthographic ? _focusCamera.orthographicSize : _focusCamera.fieldOfView;
            float duration = Mathf.Max(0.01f, _focusZoomTweenSeconds);
            float t = 0f;
            while (t < duration && _focusCamera != null)
            {
                t += Time.unscaledDeltaTime;
                float value = Mathf.Lerp(start, targetSize, Mathf.Clamp01(t / duration));
                if (_focusCameraWasOrthographic) _focusCamera.orthographicSize = value;
                else                             _focusCamera.fieldOfView       = value;
                yield return null;
            }
            if (_focusCamera != null)
            {
                if (_focusCameraWasOrthographic) _focusCamera.orthographicSize = targetSize;
                else                             _focusCamera.fieldOfView       = targetSize;
            }
            _focusTweenRoutine = null;
        }

        // ─── Typing routines ─────────────────────────────────────────────────

        private IEnumerator TypeLineRoutine(string text, float charsPerSecond, VoLinePresentationOptions opt, Action onComplete)
        {
            _lastPlainTypedText = string.Empty;
            float messageStartTime = Time.realtimeSinceStartup;

            if (text.Length == 0)
            {
                _typingRoutine   = null;
                _idleGlitchActive = true;
                _glitchStepTimer  = 0f;
                if (_blockEndClip != null) _audio.PlayOneShot(_blockEndClip);
                onComplete?.Invoke();
                yield break;
            }

            float delay    = 1f / Mathf.Max(4f, charsPerSecond);
            float tickAccum = 0f;
            var   plain    = new StringBuilder(text.Length);

            StartCursorBlink(_bodyLabel, opt.HighlightWords, opt.HighlightColorHex);

            for (var i = 0; i < text.Length; i++)
            {
                plain.Append(text[i]);
                SetTypingText(plain.ToString());

                if (_typingTickClip != null)
                {
                    tickAccum += delay;
                    if (tickAccum >= _typingTickMinInterval) { tickAccum = 0f; _audio.PlayOneShot(_typingTickClip); }
                }
                yield return new WaitForSeconds(delay);
            }

            _lastPlainTypedText = plain.ToString();
            StopCursorBlink();
            _typingRoutine = null;

            if (_blockEndClip != null) _audio.PlayOneShot(_blockEndClip);

            if (_hideAfterTypingWithoutIdle)
            {
                yield return StartCoroutine(ExitAnimRoutine());
                Hide();
                onComplete?.Invoke();
                yield break;
            }

            // Idle: block-CRT-shake sul _textWrap intero — nessun DOM reflow, nessun salto.
            _idleGlitchActive = true;
            _glitchStepTimer  = 0f;

            if (opt.ForceContinueAtEnd)
            {
                SetContinueHint(opt.ContinueHintText);
                yield return StartCoroutine(WaitForContinueInput());
                HideContinueHint();
                onComplete?.Invoke();
                yield return StartCoroutine(ExitAnimRoutine());
                Hide();
            }
            else
            {
                float typingTime = Time.realtimeSinceStartup - messageStartTime;
                float remaining  = Mathf.Max(0f, _totalMessageDuration - typingTime - _enterExitDuration);
                yield return new WaitForSecondsRealtime(remaining);
                onComplete?.Invoke();
                yield return StartCoroutine(ExitAnimRoutine());
                Hide();
            }
        }

        private IEnumerator MultiSentenceRoutine(List<string> sentences, float charsPerSecond, VoLinePresentationOptions opt, Action onComplete)
        {
            if (_sentencesHost == null) yield break;

            float messageStartTime = Time.realtimeSinceStartup;

            var line = new Label { text = string.Empty };
            line.AddToClassList("vo-body");
            line.AddToClassList("vo-sentence-line");
            ApplyRegisterToLabel(line, _activeRegister);
            _sentencesHost.Add(line);

            var plain = new StringBuilder();

            StartCursorBlink(line, opt.HighlightWords, opt.HighlightColorHex);

            for (var i = 0; i < sentences.Count; i++)
            {
                if (i > 0 && plain.Length > 0)
                    plain.Append(' ');

                yield return TypeIntoLabelRoutine(line, sentences[i], charsPerSecond, plain, opt);

                if (i >= sentences.Count - 1) continue;

                if (opt.AdvanceMode == VoSentenceAdvanceMode.ClickToContinue)
                {
                    SetContinueHint(opt.ContinueHintText);
                    yield return StartCoroutine(WaitForContinueInput());
                    HideContinueHint();
                }
                else
                {
                    float pause = Mathf.Max(opt.MinReadSeconds, sentences[i].Length * opt.ReadSecondsPerChar);
                    yield return new WaitForSecondsRealtime(pause);
                }
            }

            StopCursorBlink();
            _typingRoutine = null;

            if (_blockEndClip != null) _audio.PlayOneShot(_blockEndClip);

            if (_hideAfterTypingWithoutIdle)
            {
                yield return StartCoroutine(ExitAnimRoutine());
                Hide();
                onComplete?.Invoke();
                yield break;
            }

            _lastPlainTypedText = plain.ToString();

            // Idle: block-CRT-shake senza DOM reflow.
            _idleGlitchActive = true;
            _glitchStepTimer  = 0f;

            if (opt.ForceContinueAtEnd)
            {
                SetContinueHint(opt.ContinueHintText);
                yield return StartCoroutine(WaitForContinueInput());
                HideContinueHint();
                onComplete?.Invoke();
                yield return StartCoroutine(ExitAnimRoutine());
                Hide();
            }
            else
            {
                float typingTime = Time.realtimeSinceStartup - messageStartTime;
                float remaining  = Mathf.Max(0f, _totalMessageDuration - typingTime - _enterExitDuration);
                yield return new WaitForSecondsRealtime(remaining);
                onComplete?.Invoke();
                yield return StartCoroutine(ExitAnimRoutine());
                Hide();
            }
        }

        private IEnumerator TypeIntoLabelRoutine(Label lab, string text, float charsPerSecond, StringBuilder plain, VoLinePresentationOptions opt)
        {
            if (lab == null || string.IsNullOrEmpty(text)) yield break;

            // Assicura che il cursore sia agganciato alla label corrente
            _currentTypingLabel = lab;

            float delay    = 1f / Mathf.Max(4f, charsPerSecond);
            float tickAccum = 0f;

            for (var i = 0; i < text.Length; i++)
            {
                plain.Append(text[i]);
                SetTypingText(plain.ToString());

                if (_typingTickClip != null)
                {
                    tickAccum += delay;
                    if (tickAccum >= _typingTickMinInterval) { tickAccum = 0f; _audio.PlayOneShot(_typingTickClip); }
                }
                yield return new WaitForSeconds(delay);
            }
        }

        // ─── Highlight ───────────────────────────────────────────────────────

        private static string ApplyHighlight(string input, IReadOnlyList<string> phrases, string colorHex)
        {
            if (string.IsNullOrEmpty(input) || phrases == null || phrases.Count == 0 || string.IsNullOrWhiteSpace(colorHex))
                return input;

            var ordered = phrases.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).ToList();
            if (ordered.Count == 0) return input;
            ordered = ordered.OrderByDescending(s => s.Length).ToList();

            string pattern = "(" + string.Join("|", ordered.Select(Regex.Escape)) + ")";
            string open    = $"<color={colorHex}>";
            try { return Regex.Replace(input, pattern, m => $"{open}{m.Value}</color>", RegexOptions.IgnoreCase); }
            catch { return input; }
        }

        // ─── Continue hint ───────────────────────────────────────────────────

        private IEnumerator WaitForContinueInput()
        {
            yield return null;
            while (!Input.GetMouseButtonDown(0) && !Input.GetKeyDown(KeyCode.Space) && !Input.GetKeyDown(KeyCode.E))
                yield return null;
        }

        private void SetContinueHint(string message)
        {
            // Hint implicito: nessun testo visibile.
            // WaitForContinueInput resta attivo — click o Spazio avanzano comunque.
        }

        private void HideContinueHint()
        {
            if (_continueHint == null) return;
            _continueHint.text          = string.Empty;
            _continueHint.style.opacity = 0f;
        }

        // ─── Sentence / organic layout helpers ──────────────────────────────

        private void ClearSentenceHost()
        {
            if (_sentencesHost == null) return;
            _sentencesHost.Clear();
            _sentencesHost.style.display = DisplayStyle.None;
        }

        private static List<string> SplitIntoSentences(string text)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return result;
            var trimmed = text.Trim();
            foreach (var line in trimmed.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                foreach (var p in Regex.Split(line.Trim(), @"(?<=[\.!\?…])\s+"))
                {
                    var s = p.Trim();
                    if (s.Length > 0) result.Add(s);
                }
            }
            if (result.Count == 0) result.Add(trimmed);
            return result;
        }

        private void ApplyRegisterToLabel(Label lab, VoRegister register)
        {
            if (lab == null) return;
            lab.RemoveFromClassList("vo-body--register-a");
            lab.RemoveFromClassList("vo-body--register-b");
            lab.AddToClassList(register == VoRegister.RegisterA ? "vo-body--register-a" : "vo-body--register-b");
        }

        private void SwitchToOrganicLayout()
        {
            if (_organicHost == null || _bodyLabel == null) return;

            string fullText = !string.IsNullOrEmpty(_lastPlainTypedText) ? _lastPlainTypedText : _bodyLabel.text;
            if (string.IsNullOrWhiteSpace(fullText)) return;

            var words = SplitWords(fullText);
            if (words.Count == 0) return;

            _organicHost.Clear();
            _fragmentLabels.Clear();
            _fragmentGlitch.Clear();

            string regClass = _activeRegister == VoRegister.RegisterA ? "vo-body--register-a" : "vo-body--register-b";

            for (var i = 0; i < words.Count; i++)
            {
                string display = words[i] + (i < words.Count - 1 ? " " : string.Empty);
                var lab = new Label(display);
                lab.AddToClassList("vo-body");
                lab.AddToClassList(regClass);
                lab.AddToClassList("vo-fragment");
                lab.style.translate = new Translate(0f, 0f);
                _organicHost.Add(lab);
                _fragmentLabels.Add(lab);

                bool wiggles = UnityEngine.Random.value < FragmentWiggleProbability;
                _fragmentGlitch.Add(new FragmentGlitch
                {
                    Wiggles   = wiggles,
                    StepTimer = wiggles ? UnityEngine.Random.Range(0f, 1.4f) : 0f,
                    OffX = 0f, OffY = 0f
                });
            }

            _bodyLabel.text          = string.Empty;
            _bodyLabel.style.display = DisplayStyle.None;
            _organicHost.style.display = DisplayStyle.Flex;
        }

        private static List<string> SplitWords(string text)
        {
            var list = new List<string>();
            foreach (var p in text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                list.Add(p);
            return list;
        }

        private void ClearOrganicLayout()
        {
            _fragmentLabels.Clear();
            _fragmentGlitch.Clear();
            if (_organicHost  != null) { _organicHost.Clear(); _organicHost.style.display = DisplayStyle.None; }
            if (_bodyLabel    != null) _bodyLabel.style.display = DisplayStyle.Flex;
        }

        // ─── Register / Camera helpers ───────────────────────────────────────

        private void ApplyRegisterClass(VoRegister register)
        {
            if (_bodyLabel == null) return;
            ApplyRegisterToLabel(_bodyLabel, register);
        }

        private static Camera ResolveFocusCamera()
        {
            if (Camera.main != null) return Camera.main;
            Camera best = null;
            foreach (var c in Camera.allCameras)
            {
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) continue;
                if (best == null || c.depth > best.depth) best = c;
            }
            return best;
        }
    }
}
