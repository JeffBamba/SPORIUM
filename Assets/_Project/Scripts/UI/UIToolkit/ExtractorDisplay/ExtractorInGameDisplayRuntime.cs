using System.Collections;
using _Project;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Sporae.UI.UIToolkit.ExtractorDisplay
{
    [DisallowMultipleComponent]
    public sealed class ExtractorInGameDisplayRuntime : MonoBehaviour
    {
        private const string VisualTreeResourcePath = "UI/UIToolkit/ExtractorDisplay/ExtractorDisplay";
        private const string AnchorName = "ExtractorDisplayAnchor";
        private const string SurfaceCanvasName = "ExtractorDisplayCanvas";
        private const string SurfaceName = "ExtractorDisplaySurface";
        private const string UiHostName = "ExtractorDisplayUI_Runtime";

        [Header("References")]
        [SerializeField] private Extractor _extractor;
        [SerializeField] private Transform _displayAnchor;
        [SerializeField] private Canvas _surfaceCanvas;
        [SerializeField] private RawImage _surface;

        [Header("World Space Surface")]
        [SerializeField] private Vector3 _anchorDefaultLocalPosition = new(0f, 1.1f, -0.02f);
        [SerializeField] private Vector3 _anchorDefaultLocalScale = new(0.035f, 0.035f, 0.035f);
        [SerializeField] private Vector2 _surfaceCanvasSize = new(340f, 150f);
        [SerializeField] private int _surfaceSortingOrder = -50;

        [Header("Editor Preview")]
        [SerializeField] private bool _showEditorPreview = true;
        [SerializeField] private Color _previewFillColor = new(0.22f, 0.72f, 1f, 0.12f);
        [SerializeField] private Color _previewOutlineColor = new(0.22f, 0.72f, 1f, 0.95f);

        [Header("Render Texture")]
        [SerializeField] private Vector2Int _renderTextureSize = new(768, 320);
        [SerializeField] private FilterMode _renderTextureFilterMode = FilterMode.Trilinear;
        [SerializeField] private bool _renderTextureUseMipMap = true;
        [SerializeField] private int _renderTextureAntiAliasing = 2;
        [SerializeField] private bool _preferTextReadableSampling = false;

        [Header("Display Blend")]
        [SerializeField] private float _displayBreathCycleSeconds = 3.6f;
        [SerializeField] private float _displayOpacityMin = 0.9f;
        [SerializeField] private float _displayOpacityMax = 0.96f;
        [SerializeField] private float _displayBrightnessMin = 0.84f;
        [SerializeField] private float _displayBrightnessMax = 0.92f;
        [SerializeField] private float _displayMicroJitterPixels = 0.35f;

        [Header("Idle Animation")]
        [SerializeField] private float _idleMarqueeStepSeconds = 0.14f;
        [SerializeField] private int _idleMarqueeWindowChars = 13;

        private UIDocument _uiDocument;
        private PanelSettings _panelSettingsInstance;
        private RenderTexture _renderTexture;
        private VisualTreeAsset _visualTreeAsset;
        private bool _uiBound;

        private VisualElement _root;
        private VisualElement _progressWrap;
        private VisualElement _progressFill;
        private Label _machineLabel;
        private Label _stateLabel;
        private Label _detailLabel;
        private Label _progressLabel;
        private Label _outputLabel;

        private float _idleMarqueeTimer;
        private int _idleMarqueeOffset;
        private Vector2 _surfaceBaseAnchoredPosition;

        private void Awake()
        {
            if (_extractor == null)
                _extractor = GetComponent<Extractor>();
        }

        private void OnValidate()
        {
            if (_displayAnchor == null)
                _displayAnchor = transform.Find(AnchorName);
        }

        private void Start()
        {
            EnsureInitialized();
            RefreshNow();
        }

        private void Update()
        {
            if (!EnsureInitialized())
                return;

            _idleMarqueeTimer += Time.deltaTime;
            if (_idleMarqueeTimer >= Mathf.Max(0.05f, _idleMarqueeStepSeconds))
            {
                _idleMarqueeTimer = 0f;
                _idleMarqueeOffset++;
            }

            ApplyDisplayBlend();
            RefreshNow();
        }

        private void OnDestroy()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }

            if (_panelSettingsInstance != null)
                Destroy(_panelSettingsInstance);
        }

        public void Bind(Extractor extractor)
        {
            _extractor = extractor;
            EnsureInitialized();
            RefreshNow();
        }

        public void RefreshNow()
        {
            if (_extractor == null || !_uiBound)
                return;

            SetLabelText(_machineLabel, "ESTRATTORE");

            if (_extractor.State == ExtractorProcessState.InProgress)
            {
                SetRootStateClass("extd-progress");
                SetOptionalLabelText(_stateLabel, "ESTRAZIONE SPORE");
                if (_detailLabel != null)
                    SetOptionalLabelText(_detailLabel, BuildInputStatusText());

                SetDisplay(_progressWrap, DisplayStyle.Flex);
                float progress = Mathf.Clamp01(_extractor.ExtractionProgress);
                _progressFill.style.width = Length.Percent(progress * 100f);
                SetDisplay(_progressLabel, DisplayStyle.Flex);
                SetLabelText(_progressLabel, $"{Mathf.RoundToInt(progress * 100f)}%");
                SetLabelText(_outputLabel, BuildMarqueeText("ESTRAZIONE SPORE"));
                SetDisplay(_outputLabel, DisplayStyle.Flex);
                return;
            }

            if (_extractor.State == ExtractorProcessState.Completed)
            {
                SetRootStateClass("extd-ready");
                SetOptionalLabelText(_stateLabel, "SPORE PRONTE");
                if (_detailLabel != null)
                    SetOptionalLabelText(_detailLabel, $"SLOT PRONTI: {_extractor.CompletedCount()}");

                SetDisplay(_progressWrap, DisplayStyle.Flex);
                _progressFill.style.width = Length.Percent(100f);
                SetDisplay(_progressLabel, DisplayStyle.None);
                SetLabelText(_progressLabel, string.Empty);
                SetLabelText(_outputLabel, BuildMarqueeText(BuildCompletedOutputText()));
                SetDisplay(_outputLabel, DisplayStyle.Flex);
                return;
            }

            SetRootStateClass("extd-idle");
            ApplyIdleAnimation();
        }

        private bool EnsureInitialized()
        {
            if (_uiBound)
                return true;

            if (_extractor == null)
                _extractor = GetComponent<Extractor>();
            if (_extractor == null)
                return false;

            if (_visualTreeAsset == null)
                _visualTreeAsset = Resources.Load<VisualTreeAsset>(VisualTreeResourcePath);
            if (_visualTreeAsset == null)
                return false;

            EnsureDisplayAnchor();
            EnsureSurfaceCanvas();
            EnsureSurface();
            EnsureDocumentHost();

            if (_uiDocument == null)
                return false;

            if (_uiDocument.rootVisualElement == null)
            {
                StartCoroutine(BindWhenReady());
                return false;
            }

            BindUi();
            return _uiBound;
        }

        private IEnumerator BindWhenReady()
        {
            for (int i = 0; i < 30 && !_uiBound; i++)
            {
                if (_uiDocument != null && _uiDocument.rootVisualElement != null)
                {
                    BindUi();
                    if (_uiBound)
                        yield break;
                }
                yield return null;
            }
        }

        private void EnsureDisplayAnchor()
        {
            if (_displayAnchor != null)
                return;

            _displayAnchor = transform.Find(AnchorName);
            if (_displayAnchor != null)
                return;

            var go = new GameObject(AnchorName);
            _displayAnchor = go.transform;
            _displayAnchor.SetParent(transform, false);
            _displayAnchor.localPosition = _anchorDefaultLocalPosition;
            _displayAnchor.localRotation = Quaternion.identity;
            _displayAnchor.localScale = _anchorDefaultLocalScale;
        }

        private void OnDrawGizmos()
        {
            if (!_showEditorPreview)
                return;

            var anchor = _displayAnchor != null ? _displayAnchor : transform.Find(AnchorName);
            if (anchor == null)
                return;

            var previousMatrix = Gizmos.matrix;
            var previousColor = Gizmos.color;

            Gizmos.matrix = anchor.localToWorldMatrix;
            var previewSize = new Vector3(_surfaceCanvasSize.x, _surfaceCanvasSize.y, 0.02f);

            Gizmos.color = _previewFillColor;
            Gizmos.DrawCube(Vector3.zero, previewSize);

            Gizmos.color = _previewOutlineColor;
            Gizmos.DrawWireCube(Vector3.zero, previewSize);

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }

        private void EnsureSurfaceCanvas()
        {
            if (_displayAnchor == null)
                return;

            if (_surfaceCanvas == null)
            {
                var canvasTransform = _displayAnchor.Find(SurfaceCanvasName);
                if (canvasTransform != null)
                    _surfaceCanvas = canvasTransform.GetComponent<Canvas>();
            }

            if (_surfaceCanvas == null)
            {
                var go = new GameObject(SurfaceCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                go.transform.SetParent(_displayAnchor, false);
                _surfaceCanvas = go.GetComponent<Canvas>();
            }

            _surfaceCanvas.renderMode = RenderMode.WorldSpace;
            _surfaceCanvas.overrideSorting = true;
            _surfaceCanvas.sortingOrder = Mathf.Min(_surfaceSortingOrder, 0);

            var rect = _surfaceCanvas.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = _surfaceCanvasSize;
            rect.localScale = Vector3.one;

            var scaler = _surfaceCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            var raycaster = _surfaceCanvas.GetComponent<GraphicRaycaster>();
            raycaster.enabled = false;

            var canvasGroup = _surfaceCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = _surfaceCanvas.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.ignoreParentGroups = true;
        }

        private void EnsureSurface()
        {
            if (_surfaceCanvas == null)
                return;

            if (_surface == null)
            {
                var surfaceTransform = _surfaceCanvas.transform.Find(SurfaceName);
                if (surfaceTransform != null)
                    _surface = surfaceTransform.GetComponent<RawImage>();
            }

            if (_surface == null)
            {
                var go = new GameObject(SurfaceName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                go.transform.SetParent(_surfaceCanvas.transform, false);
                _surface = go.GetComponent<RawImage>();
            }

            var rect = _surface.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            _surfaceBaseAnchoredPosition = rect.anchoredPosition;

            _surface.color = Color.white;
            _surface.raycastTarget = false;
        }

        private void EnsureDocumentHost()
        {
            if (_uiDocument != null)
                return;

            var hostTransform = transform.Find(UiHostName);
            if (hostTransform == null)
            {
                var go = new GameObject(UiHostName, typeof(UIDocument));
                go.transform.SetParent(transform, false);
                hostTransform = go.transform;
            }

            _uiDocument = hostTransform.GetComponent<UIDocument>();
            if (_uiDocument == null)
                _uiDocument = hostTransform.gameObject.AddComponent<UIDocument>();

            if (_renderTexture == null)
            {
                Vector2Int effectiveTextureSize = GetEffectiveRenderTextureSize();
                bool useTextReadableSampling = _preferTextReadableSampling;

                _renderTexture = new RenderTexture(effectiveTextureSize.x, effectiveTextureSize.y, 0, RenderTextureFormat.ARGB32)
                {
                    name = "ExtractorDisplay_RT_Runtime",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = useTextReadableSampling ? FilterMode.Bilinear : _renderTextureFilterMode,
                    useMipMap = useTextReadableSampling ? false : _renderTextureUseMipMap,
                    autoGenerateMips = useTextReadableSampling ? false : _renderTextureUseMipMap,
                    antiAliasing = useTextReadableSampling ? 1 : Mathf.Max(1, _renderTextureAntiAliasing)
                };
                _renderTexture.anisoLevel = useTextReadableSampling ? 0 : (_renderTextureUseMipMap ? 2 : 0);
                _renderTexture.Create();
            }

            if (_panelSettingsInstance == null)
            {
                var template = FindPanelSettingsTemplate();
                if (template != null)
                {
                    _panelSettingsInstance = Instantiate(template);
                    _panelSettingsInstance.targetTexture = _renderTexture;
                }
            }

            _uiDocument.visualTreeAsset = _visualTreeAsset;
            _uiDocument.panelSettings = _panelSettingsInstance;
            _uiDocument.sortingOrder = -1000;

            if (_surface != null)
                _surface.texture = _renderTexture;
        }

        private static PanelSettings FindPanelSettingsTemplate()
        {
            var docs = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var doc in docs)
            {
                if (doc != null && doc.panelSettings != null)
                    return doc.panelSettings;
            }
            return null;
        }

        private void BindUi()
        {
            _root = _uiDocument.rootVisualElement;
            if (_root == null)
                return;

            _root.pickingMode = PickingMode.Ignore;
            DisablePickingRecursive(_root);

            _machineLabel = _root.Q<Label>("extd-machine-label");
            _stateLabel = _root.Q<Label>("extd-state-label");
            _detailLabel = _root.Q<Label>("extd-detail-label");
            _progressWrap = _root.Q<VisualElement>("extd-progress-wrap");
            _progressFill = _root.Q<VisualElement>("extd-progress-fill");
            _progressLabel = _root.Q<Label>("extd-progress-label");
            _outputLabel = _root.Q<Label>("extd-output-label");

            _uiBound = _machineLabel != null
                && _progressWrap != null
                && _progressFill != null
                && _progressLabel != null
                && _outputLabel != null;
        }

        private void SetRootStateClass(string className)
        {
            if (_root.ClassListContains(className))
                return;

            _root.RemoveFromClassList("extd-idle");
            _root.RemoveFromClassList("extd-progress");
            _root.RemoveFromClassList("extd-ready");
            _root.AddToClassList(className);
        }

        private bool HasSampleLoaded()
        {
            var inventory = _extractor.GetInventory();
            if (inventory?.Items == null)
                return false;

            foreach (var slot in inventory.Items)
            {
                if (slot != null && slot.Quantity > 0)
                    return true;
            }
            return false;
        }

        private Vector2Int GetEffectiveRenderTextureSize()
        {
            int width = Mathf.Max(_renderTextureSize.x, Mathf.CeilToInt(_surfaceCanvasSize.x * 4f));
            int height = Mathf.Max(_renderTextureSize.y, Mathf.CeilToInt(_surfaceCanvasSize.y * 4f));
            return new Vector2Int(width, height);
        }

        private void ApplyIdleAnimation()
        {
            if (_progressWrap == null || _progressFill == null || _progressLabel == null || _outputLabel == null)
                return;

            SetDisplay(_progressWrap, DisplayStyle.Flex);
            _progressFill.style.width = Length.Percent(0f);
            SetDisplay(_progressLabel, DisplayStyle.None);
            SetLabelText(_progressLabel, string.Empty);
            SetOptionalLabelText(_stateLabel, "SISTEMA IN ATTESA");
            if (_detailLabel != null)
                SetOptionalLabelText(_detailLabel, "INSERISCI FRUTTO");
            SetLabelText(_outputLabel, BuildMarqueeText("INSERISCI FRUTTO"));
            SetDisplay(_outputLabel, DisplayStyle.Flex);
        }

        private string BuildMarqueeText(string baseText)
        {
            int windowChars = Mathf.Max(6, _idleMarqueeWindowChars);
            string padding = new string(' ', windowChars);
            string loop = padding + baseText + padding;
            int span = loop.Length - windowChars + 1;
            if (span <= 0)
                return baseText;

            int offset = Mathf.Abs(_idleMarqueeOffset) % span;
            return loop.Substring(offset, windowChars);
        }

        private void ApplyDisplayBlend()
        {
            if (_root == null || _surface == null)
                return;

            float cycle = Mathf.Max(0.2f, _displayBreathCycleSeconds);
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (Mathf.PI * 2f / cycle));

            float opacity = Mathf.Lerp(_displayOpacityMin, _displayOpacityMax, wave);
            _root.style.opacity = opacity;

            float brightness = Mathf.Lerp(_displayBrightnessMin, _displayBrightnessMax, wave);
            _surface.color = new Color(brightness, brightness * 0.985f, brightness * 0.965f, opacity);

            float jitterAmount = Mathf.Max(0f, _displayMicroJitterPixels);
            float jitterX = Mathf.Sin(Time.unscaledTime * 1.37f) * jitterAmount;
            float jitterY = Mathf.Cos(Time.unscaledTime * 0.91f) * jitterAmount * 0.6f;
            _surface.rectTransform.anchoredPosition = _surfaceBaseAnchoredPosition + new Vector2(jitterX, jitterY);
        }

        private static void SetLabelText(Label label, string value)
        {
            if (label == null || label.text == value)
                return;

            label.text = value;
        }

        private static void SetOptionalLabelText(Label label, string value)
        {
            if (label == null)
                return;

            SetLabelText(label, value);
        }

        private static void SetDisplay(VisualElement element, DisplayStyle display)
        {
            if (element == null || element.resolvedStyle.display == display)
                return;

            element.style.display = display;
        }

        private static void DisablePickingRecursive(VisualElement root)
        {
            if (root == null)
                return;

            root.pickingMode = PickingMode.Ignore;
            foreach (var child in root.Children())
                DisablePickingRecursive(child);
        }

        private string BuildInputStatusText()
        {
            var inventory = _extractor.GetInventory();
            if (inventory?.Items == null)
                return "ESTRAZIONE IN CORSO";

            foreach (var slot in inventory.Items)
            {
                if (slot == null || slot.Quantity <= 0)
                    continue;

                return $"FRUTTO: {slot.TypeId} x{slot.Quantity}";
            }

            return "ESTRAZIONE IN CORSO";
        }

        private string BuildCompletedOutputText()
        {
            int spore = _extractor.PendingSporeCount;
            return spore > 1 ? $"RACCOGLI SPORE x{spore}" : "RACCOGLI SPORE";
        }
    }
}
