using System.Collections;
using System.Collections.Generic;
using Sporae.Core.Localization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Sporae.UI.UIToolkit.ElevatorDisplay
{
    /// <summary>
    /// Pannello ascensore compatto (2 righe): direzione sopra, piano sotto (marquee).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ElevatorInGameDisplayRuntime : MonoBehaviour
    {
        private const string VisualTreeResourcePath = "UI/UIToolkit/ElevatorDisplay/ElevatorDisplay";
        private const string AnchorName = "ElevatorDisplayAnchor";
        private const string SurfaceCanvasName = "ElevatorDisplayCanvas";
        private const string SurfaceName = "ElevatorDisplaySurface";
        private const string UiHostName = "ElevatorDisplayUI_Runtime";
        private const int DeniedFloorIndex = 3;

        [Header("References")]
        [SerializeField] private Transform _displayAnchor;
        [SerializeField] private Canvas _surfaceCanvas;
        [SerializeField] private RawImage _surface;

        [Header("World Space Surface")]
        [SerializeField] private Vector3 _anchorDefaultLocalPosition = new(0f, 0.36f, -0.015f);
        [SerializeField] private Vector3 _anchorDefaultLocalScale = new(0.00285f, 0.00285f, 0.00285f);
        [SerializeField] private Vector2 _surfaceCanvasSize = new(240f, 132f);
        [SerializeField] private int _surfaceSortingOrder = -50;

        [Header("Floor Marquee")]
        [SerializeField] private float _floorMarqueeStepSeconds = 0.12f;
        [SerializeField] private int _floorMarqueeWindowChars = 14;
        [SerializeField] private float _floorMarqueeLoopDurationSeconds = 10f;

        [Header("Editor Preview")]
        [SerializeField] private bool _showEditorPreview = true;
        [SerializeField] private Color _previewFillColor = new(0.22f, 0.72f, 1f, 0.12f);
        [SerializeField] private Color _previewOutlineColor = new(0.22f, 0.72f, 1f, 0.95f);

        [Header("Render Texture")]
        [SerializeField] private Vector2Int _renderTextureSize = new(960, 528);
        [SerializeField] private FilterMode _renderTextureFilterMode = FilterMode.Trilinear;
        [SerializeField] private bool _renderTextureUseMipMap = true;
        [SerializeField] private int _renderTextureAntiAliasing = 2;
        [SerializeField] private bool _preferTextReadableSampling = false;

        private UIDocument _uiDocument;
        private PanelSettings _panelSettingsInstance;
        private RenderTexture _renderTexture;
        private VisualTreeAsset _visualTreeAsset;
        private bool _uiBound;

        private VisualElement _root;
        private Label _directionArrow;
        private Label _directionLabel;
        private Label _floorDisplayLabel;
        private VisualElement _floorMarqueeClip;

        private int _pendingHighlightIndex;
        private ElevatorDirection _pendingDirection = ElevatorDirection.None;
        private ElevatorDisplayMode _pendingMode = ElevatorDisplayMode.CallRemote;
        private IReadOnlyList<string> _pendingFloorLabels;

        private string _floorMarqueeBaseText = string.Empty;
        private string _appliedMarqueeBaseText = string.Empty;
        private float _floorMarqueeTimer;
        private int _floorMarqueeOffset;
        private float _floorMarqueeScrollPx;
        private float _floorMarqueeLoopWidthPx;
        private bool _floorMarqueeUsesTranslate;

        private void OnValidate()
        {
            if (_displayAnchor == null)
                _displayAnchor = transform.Find(AnchorName);
        }

        private void Start()
        {
            EnsureInitialized();
            if (_uiBound)
                ApplyPendingState();
        }

        private void Update()
        {
            if (!_uiBound)
                return;

            _floorMarqueeTimer += Time.deltaTime;
            if (_floorMarqueeUsesTranslate)
            {
                if (_floorMarqueeLoopWidthPx > 0.01f)
                {
                    float duration = Mathf.Max(0.5f, _floorMarqueeLoopDurationSeconds);
                    _floorMarqueeScrollPx += (_floorMarqueeLoopWidthPx / duration) * Time.deltaTime;
                    if (_floorMarqueeScrollPx >= _floorMarqueeLoopWidthPx)
                        _floorMarqueeScrollPx -= _floorMarqueeLoopWidthPx;
                }
            }
            else if (_floorMarqueeTimer >= Mathf.Max(0.05f, _floorMarqueeStepSeconds))
            {
                _floorMarqueeTimer = 0f;
                _floorMarqueeOffset++;
            }

            ApplyFloorMarquee();
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

        private void OnEnable()
        {
            if (EnsureInitialized() && _uiBound)
                ApplyPendingState();
        }

        public void SetState(int highlightFloorIndex, ElevatorDirection direction, IReadOnlyList<string> floorLabels, ElevatorDisplayMode mode)
        {
            _pendingHighlightIndex = highlightFloorIndex;
            _pendingDirection = direction;
            _pendingMode = mode;
            _pendingFloorLabels = floorLabels;

            if (!EnsureInitialized() || !_uiBound)
                return;

            ApplyPendingState();
        }

        private void ApplyPendingState()
        {
            if (!_uiBound || _root == null)
                return;

            ElevatorDisplayMode mode = _pendingMode;
            ElevatorDirection direction = _pendingDirection;
            int floorIndex = _pendingHighlightIndex;
            bool moving = mode == ElevatorDisplayMode.Normal && direction != ElevatorDirection.None;

            _root.EnableInClassList("elevd-state-moving", moving);
            _root.EnableInClassList("elevd-state-idle", mode == ElevatorDisplayMode.Normal && !moving);
            _root.EnableInClassList("elevd-state-call", mode == ElevatorDisplayMode.CallRemote);
            _root.EnableInClassList("elevd-state-enter", mode == ElevatorDisplayMode.Enter);
            _root.EnableInClassList("elevd-state-cabin", mode == ElevatorDisplayMode.CabinAtFloor);
            _root.EnableInClassList("elevd-state-denied", mode == ElevatorDisplayMode.OutOfService);

            string nextMarqueeBaseText;
            switch (mode)
            {
                case ElevatorDisplayMode.CallRemote:
                    SetDirectionRow(showArrow: false, text: CallLabel);
                    nextMarqueeBaseText = ElevatorLabel;
                    break;

                case ElevatorDisplayMode.Enter:
                    SetDirectionRow(showArrow: false, text: EnterLabel);
                    nextMarqueeBaseText = FormatFloorDisplay(floorIndex, _pendingFloorLabels);
                    break;

                case ElevatorDisplayMode.CabinAtFloor:
                    SetDirectionRow(showArrow: false, text: CurrentLocationLabel);
                    nextMarqueeBaseText = FormatFloorDisplay(floorIndex, _pendingFloorLabels);
                    break;

                case ElevatorDisplayMode.OutOfService:
                    SetDirectionRow(showArrow: false, text: string.Empty);
                    nextMarqueeBaseText = DeniedLabel;
                    break;

                default:
                    if (moving)
                    {
                        SetDirectionRow(
                            showArrow: true,
                            text: GetDirectionText(direction),
                            arrow: direction == ElevatorDirection.Down ? "\u25BC" : "\u25B2");
                    }
                    else
                    {
                        SetDirectionRow(showArrow: false, text: string.Empty);
                    }

                    nextMarqueeBaseText = FormatFloorDisplay(floorIndex, _pendingFloorLabels);
                    break;
            }

            if (!string.Equals(_appliedMarqueeBaseText, nextMarqueeBaseText, System.StringComparison.Ordinal))
            {
                _appliedMarqueeBaseText = nextMarqueeBaseText;
                _floorMarqueeOffset = 0;
                _floorMarqueeScrollPx = 0f;
                _floorMarqueeLoopWidthPx = 0f;
            }

            _floorMarqueeBaseText = nextMarqueeBaseText;
            ApplyFloorMarquee();
        }

        private void SetDirectionRow(bool showArrow, string text, string arrow = "\u25B2")
        {
            if (_directionArrow != null)
            {
                _directionArrow.style.display = showArrow ? DisplayStyle.Flex : DisplayStyle.None;
                if (showArrow)
                    _directionArrow.text = arrow;
            }

            if (_directionLabel != null)
                _directionLabel.text = text;
        }

        private void ApplyFloorMarquee()
        {
            if (_floorDisplayLabel == null)
                return;

            if (string.IsNullOrEmpty(_floorMarqueeBaseText))
            {
                _floorDisplayLabel.text = string.Empty;
                _floorDisplayLabel.style.translate = new Translate(0f, 0f);
                _floorMarqueeUsesTranslate = false;
                return;
            }

            if (TryApplyTranslateMarquee(_floorMarqueeBaseText))
                return;

            _floorMarqueeUsesTranslate = false;
            _floorDisplayLabel.style.translate = new Translate(0f, 0f);
            _floorDisplayLabel.text = BuildMarqueeText(_floorMarqueeBaseText);
        }

        private bool TryApplyTranslateMarquee(string baseText)
        {
            if (_floorMarqueeClip == null)
                return false;

            float clipWidth = _floorMarqueeClip.resolvedStyle.width;
            if (float.IsNaN(clipWidth) || clipWidth <= 1f)
                clipWidth = _floorMarqueeClip.layout.width;

            _floorDisplayLabel.text = baseText;
            _floorDisplayLabel.style.translate = new Translate(0f, 0f);

            float textWidth = MeasureLabelTextWidth(baseText);
            if (textWidth <= 0.01f)
                return false;

            if (clipWidth <= 1f || textWidth <= clipWidth + 2f)
            {
                _floorDisplayLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                _floorMarqueeUsesTranslate = false;
                return true;
            }

            _floorDisplayLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            string gap = new string(' ', 3);
            _floorDisplayLabel.text = baseText + gap + baseText;
            _floorMarqueeLoopWidthPx = textWidth + MeasureLabelTextWidth(gap);
            _floorMarqueeUsesTranslate = true;
            _floorDisplayLabel.style.translate = new Translate(
                new Length(-_floorMarqueeScrollPx, LengthUnit.Pixel),
                0f);

            return true;
        }

        private float MeasureLabelTextWidth(string text)
        {
            if (_floorDisplayLabel == null || string.IsNullOrEmpty(text))
                return 0f;

            Vector2 size = _floorDisplayLabel.MeasureTextSize(
                text,
                0f,
                VisualElement.MeasureMode.Undefined,
                0f,
                VisualElement.MeasureMode.Undefined);

            return size.x;
        }

        private void SetupFloorMarqueeClip()
        {
            if (_floorDisplayLabel == null || _floorMarqueeClip != null)
                return;

            VisualElement parent = _floorDisplayLabel.parent;
            if (parent == null)
                return;

            int index = parent.IndexOf(_floorDisplayLabel);
            if (index < 0)
                return;

            _floorMarqueeClip = new VisualElement { name = "elevd-floor-marquee-clip" };
            _floorMarqueeClip.AddToClassList("elevd-floor-marquee-clip");
            _floorMarqueeClip.pickingMode = PickingMode.Ignore;

            parent.Remove(_floorDisplayLabel);
            _floorMarqueeClip.Add(_floorDisplayLabel);
            parent.Insert(index, _floorMarqueeClip);
        }

        private string BuildMarqueeText(string baseText)
        {
            if (string.IsNullOrEmpty(baseText))
                return string.Empty;

            int windowChars = Mathf.Max(6, _floorMarqueeWindowChars);
            string padding = new string(' ', windowChars);
            string loop = padding + baseText + padding;
            int span = loop.Length - windowChars + 1;
            if (span <= 0)
                return baseText;

            int offset = Mathf.Abs(_floorMarqueeOffset) % span;
            return loop.Substring(offset, windowChars);
        }

        private static string CallLabel =>
            LocalizationManager.Pick("CHIAMA", "CALL");

        private static string EnterLabel =>
            LocalizationManager.Pick("ENTRA", "ENTER");

        private static string CurrentLocationLabel =>
            LocalizationManager.Pick("LOCATION ATTUALE", "CURRENT LOCATION");

        private static string ElevatorLabel =>
            LocalizationManager.Pick("ASCENSORE", "ELEVATOR");

        private static string DeniedLabel =>
            LocalizationManager.Pick("ACCESSO NEGATO", "ACCESS DENIED");

        private static string GetDirectionText(ElevatorDirection direction)
        {
            return direction switch
            {
                ElevatorDirection.Up => LocalizationManager.Pick("GOING UP", "GOING UP"),
                ElevatorDirection.Down => LocalizationManager.Pick("GOING DOWN", "GOING DOWN"),
                _ => string.Empty
            };
        }

        private static string FormatFloorDisplay(int floorIndex, IReadOnlyList<string> floorLabels)
        {
            if (floorLabels != null && floorIndex >= 0 && floorIndex < floorLabels.Count
                && !string.IsNullOrWhiteSpace(floorLabels[floorIndex]))
            {
                return NormalizeFloorLabelForDisplay(floorLabels[floorIndex]);
            }

            return GetPlaceholderFloorDisplay(floorIndex);
        }

        private static string NormalizeFloorLabelForDisplay(string fullLabel)
        {
            if (string.IsNullOrWhiteSpace(fullLabel))
                return string.Empty;

            if (fullLabel.StartsWith("Floor ", System.StringComparison.Ordinal))
                return "FLOOR " + fullLabel.Substring(6);

            return fullLabel;
        }

        private static string GetPlaceholderFloorDisplay(int floorIndex)
        {
            return floorIndex switch
            {
                0 => "FLOOR +1 \u00B7 Visitor Room & Seed Storage",
                1 => "FLOOR 0 \u00B7 Serra + Laboratorio",
                2 => "FLOOR -1 \u00B7 Dormitorio - Cucina",
                3 => "FLOOR -2 \u00B7 Out of Service",
                _ => $"FLOOR {floorIndex}"
            };
        }

        private bool EnsureInitialized()
        {
            if (_uiBound)
            {
                if (_root == null || _uiDocument == null || _uiDocument.rootVisualElement == null ||
                    !ReferenceEquals(_uiDocument.rootVisualElement, _root))
                {
                    _uiBound = false;
                    _root = null;
                }
                else
                {
                    return true;
                }
            }

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
                    {
                        ApplyPendingState();
                        yield break;
                    }
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
                Vector2Int effectiveSize = GetEffectiveRenderTextureSize();
                bool useTextReadableSampling = _preferTextReadableSampling;

                _renderTexture = new RenderTexture(effectiveSize.x, effectiveSize.y, 0, RenderTextureFormat.ARGB32)
                {
                    name = "ElevatorDisplay_RT_Runtime",
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

        private Vector2Int GetEffectiveRenderTextureSize()
        {
            int width = Mathf.Max(_renderTextureSize.x, Mathf.CeilToInt(_surfaceCanvasSize.x * 4f));
            int height = Mathf.Max(_renderTextureSize.y, Mathf.CeilToInt(_surfaceCanvasSize.y * 4f));
            return new Vector2Int(width, height);
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

            DisablePickingRecursive(_root);

            _directionArrow = _root.Q<Label>("elevd-direction-arrow");
            _directionLabel = _root.Q<Label>("elevd-direction-label");
            _floorDisplayLabel = _root.Q<Label>("elevd-floor-label");

            SetupFloorMarqueeClip();

            _uiBound = _directionLabel != null && _floorDisplayLabel != null;
        }

        private static void DisablePickingRecursive(VisualElement root)
        {
            if (root == null)
                return;

            root.pickingMode = PickingMode.Ignore;
            foreach (var child in root.Children())
                DisablePickingRecursive(child);
        }

        private void OnDrawGizmos()
        {
            if (!_showEditorPreview)
                return;

            var anchor = _displayAnchor != null ? _displayAnchor : transform.Find(AnchorName);
            if (anchor == null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.color = _previewOutlineColor;
                Gizmos.DrawWireCube(_anchorDefaultLocalPosition, _surfaceCanvasSize * _anchorDefaultLocalScale.x);
                return;
            }

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
    }
}
