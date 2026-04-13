using System.Collections;
using _Project;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Sporae.UI.UIToolkit.ExtractorDisplay
{
    [DisallowMultipleComponent]
    public sealed class ExtractorSporeProtoDisplayRuntime : MonoBehaviour
    {
        private const string VisualTreeResourcePath = "UI/UIToolkit/ExtractorDisplay/ExtractorSporeProtoDisplay";
        private const string AnchorName = "ExtractorDisplayAnchor_ProtoSpore";
        private const string SurfaceCanvasName = "ExtractorSporeProtoCanvas";
        private const string SurfaceName = "ExtractorSporeProtoSurface";
        private const string UiHostName = "ExtractorSporeProtoUI_Runtime";

        [Header("References")]
        [SerializeField] private Extractor _extractor;
        [SerializeField] private Transform _displayAnchor;
        [SerializeField] private Canvas _surfaceCanvas;
        [SerializeField] private RawImage _surface;

        [Header("World Space Surface")]
        [SerializeField] private Vector3 _anchorDefaultLocalPosition = new(-0.12f, 1.12f, -0.02f);
        [SerializeField] private Vector3 _anchorDefaultLocalScale = new(0.008f, 0.008f, 0.008f);
        [SerializeField] private Vector2 _surfaceCanvasSize = new(520f, 230f);
        [SerializeField] private int _surfaceSortingOrder = -50;

        [Header("Editor Preview")]
        [SerializeField] private bool _showEditorPreview = true;
        [SerializeField] private Color _previewFillColor = new(0.3f, 0.95f, 0.65f, 0.1f);
        [SerializeField] private Color _previewOutlineColor = new(0.3f, 0.95f, 0.65f, 0.8f);

        [Header("Render Texture")]
        [SerializeField] private Vector2Int _renderTextureSize = new(1280, 640);
        [SerializeField] private FilterMode _renderTextureFilterMode = FilterMode.Bilinear;
        [SerializeField] private bool _renderTextureUseMipMap = false;
        [SerializeField] private int _renderTextureAntiAliasing = 1;

        [Header("Animation")]
        [SerializeField] private float _idleScanCycleSeconds = 2.2f;
        [SerializeField] private float _processScanCycleSeconds = 1.2f;

        private UIDocument _uiDocument;
        private PanelSettings _panelSettingsInstance;
        private RenderTexture _renderTexture;
        private VisualTreeAsset _visualTreeAsset;
        private bool _uiBound;

        private VisualElement _root;
        private VisualElement _fruitZone;
        private VisualElement _fruitShell;
        private VisualElement _fruitCore;
        private VisualElement _fruitStem;
        private VisualElement _flowLine;
        private VisualElement _flowLineFill;
        private VisualElement _scanBand;
        private VisualElement _collectorRing;
        private VisualElement _collectorCore;
        private readonly VisualElement[] _sporeDots = new VisualElement[5];

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

        public void RefreshNow()
        {
            if (_extractor == null || !_uiBound)
                return;

            if (_extractor.State == ExtractorProcessState.InProgress)
            {
                float progress = Mathf.Clamp01(_extractor.ExtractionProgress);

                SetRootStateClass("extp-progress");
                ApplyProcessArt(progress);
                return;
            }

            if (_extractor.State == ExtractorProcessState.Completed)
            {
                SetRootStateClass("extp-ready");
                ApplyReadyArt();
                return;
            }

            SetRootStateClass("extp-idle");
            ApplyIdleArt();
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
            Gizmos.color = _previewFillColor;
            Gizmos.DrawCube(Vector3.zero, new Vector3(_surfaceCanvasSize.x, _surfaceCanvasSize.y, 0.02f));
            Gizmos.color = _previewOutlineColor;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(_surfaceCanvasSize.x, _surfaceCanvasSize.y, 0.02f));

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
                _renderTexture = new RenderTexture(_renderTextureSize.x, _renderTextureSize.y, 0, RenderTextureFormat.ARGB32)
                {
                    name = "ExtractorSporeProto_RT_Runtime",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = _renderTextureFilterMode,
                    useMipMap = _renderTextureUseMipMap,
                    autoGenerateMips = _renderTextureUseMipMap,
                    antiAliasing = Mathf.Max(1, _renderTextureAntiAliasing)
                };
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

            _fruitZone = _root.Q<VisualElement>("extp-fruit-zone");
            _fruitShell = _root.Q<VisualElement>("extp-fruit-shell");
            _fruitCore = _root.Q<VisualElement>("extp-fruit-core");
            _fruitStem = _root.Q<VisualElement>("extp-fruit-stem");
            _flowLine = _root.Q<VisualElement>("extp-flow-line");
            _flowLineFill = _root.Q<VisualElement>("extp-flow-line-fill");
            _scanBand = _root.Q<VisualElement>("extp-scan-band");
            _collectorRing = _root.Q<VisualElement>("extp-collector-ring");
            _collectorCore = _root.Q<VisualElement>("extp-collector-core");

            for (int i = 0; i < _sporeDots.Length; i++)
                _sporeDots[i] = _root.Q<VisualElement>($"extp-spore-dot-{i}");

            _uiBound = _fruitZone != null
                && _fruitShell != null
                && _fruitCore != null
                && _flowLine != null
                && _flowLineFill != null
                && _scanBand != null
                && _collectorRing != null
                && _collectorCore != null;
        }

        private void SetRootStateClass(string className)
        {
            if (_root.ClassListContains(className))
                return;

            _root.RemoveFromClassList("extp-idle");
            _root.RemoveFromClassList("extp-progress");
            _root.RemoveFromClassList("extp-ready");
            _root.AddToClassList(className);
        }

        private void ApplyIdleArt()
        {
            float t = Time.unscaledTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 1.6f);
            float scan = Mathf.PingPong(t / Mathf.Max(0.2f, _idleScanCycleSeconds), 1f);

            _fruitShell.style.opacity = 0.5f + pulse * 0.08f;
            _fruitCore.style.opacity = 0.24f + pulse * 0.16f;
            _fruitCore.style.width = Length.Percent(48f);
            _fruitCore.style.height = Length.Percent(42f);
            if (_fruitStem != null)
                _fruitStem.style.opacity = 0.5f;
            _flowLine.style.opacity = 0.1f;
            _flowLineFill.style.width = Length.Percent(0f);
            _collectorRing.style.opacity = 0.16f;
            _collectorCore.style.opacity = 0.08f;
            _collectorCore.style.width = Length.Percent(28f);
            _collectorCore.style.height = Length.Percent(28f);
            _scanBand.style.opacity = 0.16f;
            UpdateScanBandTop(scan);

            for (int i = 0; i < _sporeDots.Length; i++)
                SetDotVisual(i, 0.05f + (i == 0 ? pulse * 0.08f : 0f), 7f);
        }

        private void ApplyProcessArt(float progress)
        {
            float t = Time.unscaledTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 4.1f);
            float scan = Mathf.PingPong(t / Mathf.Max(0.15f, _processScanCycleSeconds), 1f);

            _fruitShell.style.opacity = Mathf.Lerp(0.62f, 0.22f, progress) + pulse * 0.08f;
            _fruitCore.style.opacity = Mathf.Lerp(0.64f, 0.1f, progress) + pulse * 0.1f;
            _fruitCore.style.width = Length.Percent(Mathf.Lerp(48f, 16f, progress));
            _fruitCore.style.height = Length.Percent(Mathf.Lerp(42f, 14f, progress));
            if (_fruitStem != null)
                _fruitStem.style.opacity = Mathf.Lerp(0.68f, 0.24f, progress);
            _flowLine.style.opacity = 0.22f + progress * 0.34f;
            _flowLineFill.style.width = Length.Percent(progress * 100f);
            _collectorRing.style.opacity = 0.24f + progress * 0.5f;
            _collectorCore.style.opacity = 0.08f + progress * 0.72f;
            _collectorCore.style.width = Length.Percent(Mathf.Lerp(24f, 74f, progress));
            _collectorCore.style.height = Length.Percent(Mathf.Lerp(24f, 74f, progress));
            _scanBand.style.opacity = 0.28f;
            UpdateScanBandTop(scan);

            for (int i = 0; i < _sporeDots.Length; i++)
            {
                float phase = Mathf.Clamp01((progress * 1.25f) - (i * 0.16f));
                float pulseMix = 0.7f + 0.3f * Mathf.Sin((t * 6f) + i * 0.6f);
                SetDotVisual(i, Mathf.Lerp(0.02f, 0.96f, phase) * pulseMix, Mathf.Lerp(7f, 13f, phase));
            }
        }

        private void ApplyReadyArt()
        {
            float t = Time.unscaledTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 2.3f);

            _fruitShell.style.opacity = 0.14f;
            _fruitCore.style.opacity = 0.08f;
            _fruitCore.style.width = Length.Percent(12f);
            _fruitCore.style.height = Length.Percent(10f);
            if (_fruitStem != null)
                _fruitStem.style.opacity = 0.18f;
            _flowLine.style.opacity = 0.22f;
            _flowLineFill.style.width = Length.Percent(100f);
            _collectorRing.style.opacity = 0.8f;
            _collectorCore.style.opacity = 0.72f + pulse * 0.18f;
            _collectorCore.style.width = Length.Percent(78f);
            _collectorCore.style.height = Length.Percent(78f);
            _scanBand.style.opacity = 0f;

            for (int i = 0; i < _sporeDots.Length; i++)
                SetDotVisual(i, 0.62f + pulse * 0.16f, 13f);
        }

        private void SetDotVisual(int index, float opacity, float sizePx)
        {
            if (index < 0 || index >= _sporeDots.Length || _sporeDots[index] == null)
                return;

            _sporeDots[index].style.opacity = Mathf.Clamp01(opacity);
            _sporeDots[index].style.width = new Length(sizePx, LengthUnit.Pixel);
            _sporeDots[index].style.height = new Length(sizePx, LengthUnit.Pixel);
        }

        private void UpdateScanBandTop(float scan)
        {
            if (_scanBand == null || _fruitZone == null)
                return;

            float zoneHeight = _fruitZone.resolvedStyle.height;
            float bandHeight = _scanBand.resolvedStyle.height;
            if (zoneHeight <= 0f)
                return;

            float minTop = zoneHeight * 0.18f;
            float maxTop = Mathf.Max(minTop, zoneHeight * 0.72f - bandHeight);
            _scanBand.style.top = new Length(Mathf.Lerp(minTop, maxTop, scan), LengthUnit.Pixel);
        }

        private void ApplyDisplayBlend()
        {
            if (_root == null || _surface == null)
                return;

            float t = Time.unscaledTime;
            float wave = 0.5f + 0.5f * Mathf.Sin(t * 1.4f);

            _root.style.opacity = Mathf.Lerp(0.88f, 0.95f, wave);
            float brightness = Mathf.Lerp(0.8f, 0.9f, wave);
            _surface.color = new Color(brightness, brightness * 0.98f, brightness * 0.96f, 0.94f);

            float jitterX = Mathf.Sin(t * 1.11f) * 0.28f;
            float jitterY = Mathf.Cos(t * 0.87f) * 0.18f;
            _surface.rectTransform.anchoredPosition = _surfaceBaseAnchoredPosition + new Vector2(jitterX, jitterY);
        }

        private static void DisablePickingRecursive(VisualElement root)
        {
            if (root == null)
                return;

            root.pickingMode = PickingMode.Ignore;
            foreach (var child in root.Children())
                DisablePickingRecursive(child);
        }
    }
}
