using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Sporae.DevTools;

namespace _Project.UI.UIToolkit.VoOverlay
{
    /// <summary>
    /// Overlay testo VO (typing, due registri colore, audio opzionale). Stesso stack Panel Settings del MainMenu.
    /// </summary>
    public enum VoRegister
    {
        /// <summary>Registro A — tono “manutentore” (cyan).</summary>
        RegisterA,
        /// <summary>Registro B — tono “pragmatico” (verde).</summary>
        RegisterB
    }

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-45)]
    public sealed class VoOverlayController : MonoBehaviour
    {
        private const string VisualTreeResourcePath = "UI/UIToolkit/VoOverlay/VoOverlay";
        private const string PanelSettingsResourcePath = "UI/UIToolkit/MainMenu/MainMenuPanelSettings";

        /// <summary>Sopra TopBar/Compact (200), Foundation (150); sotto PlantCard (600) e menu pausa (700).</summary>
        private const int SortingOrder = 650;

        /// <summary>Drift sul blocco intero: lento, un filo più leggibile in idle.</summary>
        private const float BlockGlitchRangePx = 0.65f;
        private const float BlockGlitchIntervalMin = 1.45f;
        private const float BlockGlitchIntervalMax = 2.85f;

        /// <summary>Frazione attesa ~30%: solo alcune parole hanno micro-glitch indipendente.</summary>
        private const float FragmentWiggleProbability = 0.30f;

        /// <summary>Micro-drift sulle parole “wiggle”: stesso ordine di grandezza del blocco, leggermente più vivo.</summary>
        private const float FragmentGlitchRangePx = 0.82f;
        private const float FragmentGlitchIntervalMin = 1.2f;
        private const float FragmentGlitchIntervalMax = 2.55f;

        [Header("Audio (opzionali)")]
        [SerializeField] private AudioClip _blockStartClip;
        [SerializeField] private AudioClip _blockEndClip;
        [SerializeField] private AudioClip _typingTickClip;
        [SerializeField, Range(0.04f, 0.45f)] private float _typingTickMinInterval = 0.09f;

        [Header("Typing")]
        [SerializeField, Range(8f, 90f)] private float _defaultCharsPerSecond = 28f;

        private UIDocument _document;
        private VisualElement _root;
        private VisualElement _textWrap;
        private VisualElement _organicHost;
        private Label _bodyLabel;
        private AudioSource _audio;

        private VoRegister _activeRegister;

        private Coroutine _typingRoutine;
        /// <summary>True dopo che la linea ha finito il typing: abilita il micro-movimento “glitch” sul wrap.</summary>
        private bool _idleGlitchActive;

        /// <summary>Countdown al prossimo offset “a scatti” sul blocco.</summary>
        private float _glitchStepTimer;
        private float _glitchOffX;
        private float _glitchOffY;

        private readonly List<Label> _fragmentLabels = new List<Label>();
        private readonly List<FragmentGlitch> _fragmentGlitch = new List<FragmentGlitch>();

        public bool IsShowing { get; private set; }
        public bool IsTyping => _typingRoutine != null;

        private struct FragmentGlitch
        {
            public bool Wiggles;
            public float StepTimer;
            public float OffX;
            public float OffY;
        }

        private void Awake()
        {
            _audio = GetComponent<AudioSource>();
            if (_audio == null)
                _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;

            BuildUi();
        }

        private void OnDestroy()
        {
            StopTypingInternal();
        }

        private void Update()
        {
            if (_textWrap == null || !IsShowing)
                return;

            if (!_idleGlitchActive || IsTyping)
            {
                ResetAllTranslates();
                return;
            }

            StepBlockGlitch();

            if (_fragmentLabels.Count > 0)
            {
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
            }

            _textWrap.style.translate = new Translate(
                new Length(_glitchOffX, LengthUnit.Pixel),
                new Length(_glitchOffY, LengthUnit.Pixel));
        }

        private void ResetAllTranslates()
        {
            _textWrap.style.translate = new Translate(0f, 0f);
            for (var i = 0; i < _fragmentLabels.Count; i++)
                _fragmentLabels[i].style.translate = new Translate(0f, 0f);
        }

        private void StepBlockGlitch()
        {
            _glitchStepTimer -= Time.unscaledDeltaTime;
            if (_glitchStepTimer > 0f)
                return;

            _glitchStepTimer = UnityEngine.Random.Range(BlockGlitchIntervalMin, BlockGlitchIntervalMax);

            if (UnityEngine.Random.value < 0.15f)
            {
                _glitchOffX = 0f;
                _glitchOffY = 0f;
                return;
            }

            float rx = UnityEngine.Random.Range(-BlockGlitchRangePx, BlockGlitchRangePx);
            float ry = UnityEngine.Random.Range(-BlockGlitchRangePx, BlockGlitchRangePx);
            if (UnityEngine.Random.value < 0.09f)
            {
                rx *= 1.3f;
                ry *= 1.3f;
            }

            float cap = BlockGlitchRangePx * 1.38f;
            _glitchOffX = SnapGlitchPx(Mathf.Clamp(rx, -cap, cap));
            _glitchOffY = SnapGlitchPx(Mathf.Clamp(ry, -cap, cap));
        }

        private void StepFragmentGlitch(ref FragmentGlitch g, int index)
        {
            g.StepTimer -= Time.unscaledDeltaTime;
            if (g.StepTimer > 0f)
                return;

            float spread = (index % 11) * 0.035f;
            g.StepTimer = UnityEngine.Random.Range(
                FragmentGlitchIntervalMin + spread,
                FragmentGlitchIntervalMax + spread);

            if (UnityEngine.Random.value < 0.14f)
            {
                g.OffX = 0f;
                g.OffY = 0f;
                return;
            }

            float rx = UnityEngine.Random.Range(-FragmentGlitchRangePx, FragmentGlitchRangePx);
            float ry = UnityEngine.Random.Range(-FragmentGlitchRangePx, FragmentGlitchRangePx);
            if (UnityEngine.Random.value < 0.08f)
            {
                rx *= 1.28f;
                ry *= 1.28f;
            }

            float cap = FragmentGlitchRangePx * 1.36f;
            g.OffX = SnapGlitchPx(Mathf.Clamp(rx, -cap, cap));
            g.OffY = SnapGlitchPx(Mathf.Clamp(ry, -cap, cap));
        }

        private static float SnapGlitchPx(float v) => Mathf.Round(v * 2f) / 2f;

        private void BuildUi()
        {
            var vta = Resources.Load<VisualTreeAsset>(VisualTreeResourcePath);
            if (vta == null)
            {
                Debug.LogError($"[VoOverlay] VisualTreeAsset non trovato: {VisualTreeResourcePath}");
                return;
            }

            var panelSettings = Resources.Load<PanelSettings>(PanelSettingsResourcePath);
            if (panelSettings == null)
            {
                Debug.LogError($"[VoOverlay] PanelSettings non trovato: {PanelSettingsResourcePath}");
                return;
            }

            _document = GetComponent<UIDocument>();
            if (_document == null)
                _document = gameObject.AddComponent<UIDocument>();

            _document.panelSettings = panelSettings;
            _document.visualTreeAsset = vta;
            _document.sortingOrder = SortingOrder;

            BindVisuals();
        }

        private void Start()
        {
            if (_document != null && (_root == null || _bodyLabel == null))
                BindVisuals();
        }

        private void BindVisuals()
        {
            if (_document == null)
                return;
            var ve = _document.rootVisualElement;
            if (ve == null)
                return;
            _root = ve.Q<VisualElement>("vo-overlay-root");
            _textWrap = ve.Q<VisualElement>("vo-text-wrap");
            _organicHost = ve.Q<VisualElement>("vo-organic-host");
            _bodyLabel = ve.Q<Label>("vo-body");
            if (_bodyLabel != null)
                _bodyLabel.text = string.Empty;
            if (_root != null)
                _root.style.display = DisplayStyle.None;
            if (_textWrap != null)
                _textWrap.style.translate = new Translate(0f, 0f);
            if (_organicHost != null)
                _organicHost.style.display = DisplayStyle.None;
        }

        /// <summary>Mostra una linea con effetto typing. Il movimento del player non viene bloccato (nessun lock qui).</summary>
        public void ShowLine(string text, VoRegister register, float? charsPerSecond = null, Action onComplete = null)
        {
            if (_bodyLabel == null || _root == null)
            {
#if UNITY_EDITOR
                SporiumLogger.LogWarning(LogCategory.UI, "[VoOverlay] UI non pronta — ShowLine ignorato.");
#endif
                return;
            }

            StopTypingInternal();

            _activeRegister = register;
            ApplyRegisterClass(register);
            _bodyLabel.text = string.Empty;
            ClearOrganicLayout();
            _idleGlitchActive = false;
            _glitchOffX = 0f;
            _glitchOffY = 0f;
            _glitchStepTimer = 0f;
            if (_textWrap != null)
            {
                _textWrap.style.translate = new Translate(0f, 0f);
                ClearTextWrapHeightConstraint();
            }

            _root.style.display = DisplayStyle.Flex;
            IsShowing = true;

            if (_blockStartClip != null)
                _audio.PlayOneShot(_blockStartClip);

            var fullText = text ?? string.Empty;
            float cps = charsPerSecond ?? _defaultCharsPerSecond;
            _typingRoutine = StartCoroutine(TypeLineRoutine(fullText, cps, onComplete));
        }

        /// <summary>Nasconde subito l’overlay e interrompe il typing.</summary>
        public void Hide()
        {
            StopTypingInternal();
            _idleGlitchActive = false;
            _glitchOffX = 0f;
            _glitchOffY = 0f;
            _glitchStepTimer = 0f;
            ClearOrganicLayout();
            if (_textWrap != null)
                _textWrap.style.translate = new Translate(0f, 0f);
            if (_root != null)
            {
                _root.style.display = DisplayStyle.None;
                IsShowing = false;
            }
            if (_bodyLabel != null)
                _bodyLabel.text = string.Empty;
        }

        private void StopTypingInternal()
        {
            if (_typingRoutine != null)
            {
                StopCoroutine(_typingRoutine);
                _typingRoutine = null;
            }
        }

        private IEnumerator TypeLineRoutine(string text, float charsPerSecond, Action onComplete)
        {
            if (text.Length == 0)
            {
                _typingRoutine = null;
                _idleGlitchActive = true;
                _glitchStepTimer = 0f;
                if (_blockEndClip != null)
                    _audio.PlayOneShot(_blockEndClip);
                onComplete?.Invoke();
                yield break;
            }

            float delay = 1f / Mathf.Max(4f, charsPerSecond);
            float tickAccum = 0f;

            for (var i = 0; i < text.Length; i++)
            {
                _bodyLabel.text += text[i];

                if (_typingTickClip != null)
                {
                    tickAccum += delay;
                    if (tickAccum >= _typingTickMinInterval)
                    {
                        tickAccum = 0f;
                        _audio.PlayOneShot(_typingTickClip);
                    }
                }

                yield return new WaitForSeconds(delay);
            }

            if (_blockEndClip != null)
                _audio.PlayOneShot(_blockEndClip);

            _typingRoutine = null;
            // Un pass di layout con il testo completo sulla singola Label, così layout.height è affidabile
            // prima dello swap verso le parole (evita scatto verticale con justify flex-end sul root).
            yield return null;
            SwitchToOrganicLayout();
            _idleGlitchActive = true;
            _glitchStepTimer = 0f;
            onComplete?.Invoke();
        }

        private void SwitchToOrganicLayout()
        {
            if (_organicHost == null || _bodyLabel == null || _textWrap == null)
                return;

            string fullText = _bodyLabel.text;
            if (string.IsNullOrWhiteSpace(fullText))
                return;

            var words = SplitWords(fullText);
            if (words.Count == 0)
                return;

            _organicHost.Clear();
            _fragmentLabels.Clear();
            _fragmentGlitch.Clear();

            string regClass = _activeRegister == VoRegister.RegisterA ? "vo-body--register-a" : "vo-body--register-b";

            float bodyBlockHeight = Mathf.Max(_bodyLabel.layout.height, _bodyLabel.worldBound.height);
            if (bodyBlockHeight < 0.5f && _bodyLabel.resolvedStyle.height > 0.5f)
                bodyBlockHeight = _bodyLabel.resolvedStyle.height;

            for (var i = 0; i < words.Count; i++)
            {
                string display = words[i];
                if (i < words.Count - 1)
                    display += " ";

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
                    Wiggles = wiggles,
                    StepTimer = wiggles ? UnityEngine.Random.Range(0f, 1.4f) : 0f,
                    OffX = 0f,
                    OffY = 0f
                });
            }

            _bodyLabel.text = string.Empty;
            _bodyLabel.style.display = DisplayStyle.None;
            _organicHost.style.display = DisplayStyle.Flex;

            _textWrap.style.minHeight = bodyBlockHeight;
            float capturedBodyH = bodyBlockHeight;
            _organicHost.schedule.Execute(() =>
            {
                if (_textWrap == null || _organicHost == null)
                    return;
                float hostH = Mathf.Max(_organicHost.layout.height, _organicHost.worldBound.height);
                if (hostH > capturedBodyH + 0.5f)
                    _textWrap.style.minHeight = hostH;
            }).ExecuteLater(0);
        }

        private static List<string> SplitWords(string text)
        {
            var list = new List<string>();
            var parts = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
                list.Add(p);
            return list;
        }

        private void ClearOrganicLayout()
        {
            _fragmentLabels.Clear();
            _fragmentGlitch.Clear();
            if (_organicHost != null)
            {
                _organicHost.Clear();
                _organicHost.style.display = DisplayStyle.None;
            }

            if (_bodyLabel != null)
                _bodyLabel.style.display = DisplayStyle.Flex;
            ClearTextWrapHeightConstraint();
        }

        private void ClearTextWrapHeightConstraint()
        {
            if (_textWrap == null)
                return;
            _textWrap.style.minHeight = StyleKeyword.Auto;
        }

        private void ApplyRegisterClass(VoRegister register)
        {
            if (_bodyLabel == null)
                return;
            _bodyLabel.RemoveFromClassList("vo-body--register-a");
            _bodyLabel.RemoveFromClassList("vo-body--register-b");
            _bodyLabel.AddToClassList(register == VoRegister.RegisterA ? "vo-body--register-a" : "vo-body--register-b");
        }
    }
}
