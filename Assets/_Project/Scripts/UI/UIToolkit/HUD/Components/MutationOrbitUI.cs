using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace Sporae.UI.UIToolkit.HUD
{
    /// <summary>
    /// Icona mutation: sistema orbitale con nucleo viola pulsante e particella su orbita circolare.
    /// Colore: #D946EF (0%) → #C4B5FD (100%). Rotazione: 4s (0%) → 1.5s (100%) per giro.
    /// Sopra 50%: scia dietro la particella e nucleo pulsa continuamente.
    /// </summary>
    public class MutationOrbitUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Mutation (set by TopBarController)")]
        [SerializeField] private float _mutationIndex = 0.42f;

        private static readonly Color ColorFuchsia = new Color(0.851f, 0.275f, 0.937f, 1f);   // #D946EF @ 0%
        private static readonly Color ColorLilac = new Color(0.769f, 0.71f, 0.992f, 1f);     // #C4B5FD @ 100%
        private static readonly Color ColorValueSolid = new Color(0.604f, 0.361f, 0.71f, 1f); // #9A5CB5 tinta unita label

        private const float OrbitSecondsMin = 1.5f;  // 100% mutation
        private const float OrbitSecondsMax = 4f;    // 0% mutation
        private const float CenterPx = 16f;
        private const float RadiusPx = 11f;
        private const float DotSizePx = 6f;
        private const float TrailOffsetDeg = 28f;

        private VisualElement _root;
        private VisualElement _orbitContainer;
        private VisualElement _iconMutation;
        private VisualElement _nucleus;
        private VisualElement _orbitDot;
        private VisualElement _trail1;
        private VisualElement _trail2;
        private Label _valueLabel;

        private Color _currentColor;
        private float _orbitAngle;
        private Coroutine _orbitCoroutine;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            InitializeUI();
            ApplyColor(_mutationIndex);
            UpdateTrailVisibility();
            StartOrbitAnimation();
        }

        private void Update()
        {
            if (_mutationIndex <= 0.5f) return;
            if (_nucleus == null) return;
            float pulse = 0.92f + 0.16f * (0.5f + 0.5f * Mathf.Sin(Time.time * 3.2f));
            _nucleus.style.scale = new Scale(new Vector2(pulse, pulse));
        }

        private void InitializeUI()
        {
            if (_uiDocument == null) return;
            _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

            _iconMutation = _root.Q<VisualElement>("icon-mutation");
            _orbitContainer = _root.Q<VisualElement>("mutation-orbit");
            _nucleus = _root.Q<VisualElement>("mutation-nucleus");
            _orbitDot = _root.Q<VisualElement>("mutation-orbit-dot");
            _trail1 = _root.Q<VisualElement>("mutation-trail-1");
            _trail2 = _root.Q<VisualElement>("mutation-trail-2");
            _valueLabel = _root.Q<Label>("mutation-value");
        }

        /// <summary>
        /// Chiamato da TopBarController per aggiornare indice e colori.
        /// </summary>
        public void UpdateMutation(float index)
        {
            _mutationIndex = Mathf.Clamp01(index);
            ApplyColor(_mutationIndex);
            UpdateTrailVisibility();
        }

        private void ApplyColor(float index)
        {
            _currentColor = Color.Lerp(ColorFuchsia, ColorLilac, index);

            if (_iconMutation != null)
            {
                _iconMutation.style.borderTopColor = new StyleColor(_currentColor);
                _iconMutation.style.borderRightColor = new StyleColor(_currentColor);
                _iconMutation.style.borderBottomColor = new StyleColor(_currentColor);
                _iconMutation.style.borderLeftColor = new StyleColor(_currentColor);
            }
            if (_nucleus != null)
                _nucleus.style.backgroundColor = new StyleColor(_currentColor);
            if (_orbitDot != null)
                _orbitDot.style.backgroundColor = new StyleColor(_currentColor);
            if (_trail1 != null)
                _trail1.style.backgroundColor = new StyleColor(_currentColor);
            if (_trail2 != null)
                _trail2.style.backgroundColor = new StyleColor(_currentColor);
            if (_valueLabel != null)
            {
                int pct = Mathf.RoundToInt(_mutationIndex * 100f);
                _valueLabel.text = $"{pct}%";
                _valueLabel.style.color = new StyleColor(ColorValueSolid);
            }
        }

        private void UpdateTrailVisibility()
        {
            bool show = _mutationIndex > 0.5f;
            if (_trail1 != null)
                _trail1.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (_trail2 != null)
                _trail2.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void StartOrbitAnimation()
        {
            if (_orbitCoroutine != null)
                StopCoroutine(_orbitCoroutine);
            _orbitCoroutine = StartCoroutine(OrbitAnimation());
        }

        private float GetSecondsPerRevolution()
        {
            return Mathf.Lerp(OrbitSecondsMax, OrbitSecondsMin, _mutationIndex);
        }

        private IEnumerator OrbitAnimation()
        {
            float halfDot = DotSizePx * 0.5f;
            while (true)
            {
                float secPerRev = GetSecondsPerRevolution();
                _orbitAngle += (360f / secPerRev) * Time.deltaTime;
                if (_orbitAngle >= 360f) _orbitAngle -= 360f;
                if (_orbitAngle < 0f) _orbitAngle += 360f;

                float rad = _orbitAngle * Mathf.Deg2Rad;
                float x = CenterPx + RadiusPx * Mathf.Cos(rad) - halfDot;
                float y = CenterPx + RadiusPx * Mathf.Sin(rad) - halfDot;

                if (_orbitDot != null)
                {
                    _orbitDot.style.left = x;
                    _orbitDot.style.top = y;
                }

                if (_mutationIndex > 0.5f)
                {
                    float trailRad1 = (_orbitAngle - TrailOffsetDeg) * Mathf.Deg2Rad;
                    float trailRad2 = (_orbitAngle - TrailOffsetDeg * 2f) * Mathf.Deg2Rad;
                    float tx1 = CenterPx + RadiusPx * Mathf.Cos(trailRad1) - 2f;
                    float ty1 = CenterPx + RadiusPx * Mathf.Sin(trailRad1) - 2f;
                    float tx2 = CenterPx + RadiusPx * Mathf.Cos(trailRad2) - 2f;
                    float ty2 = CenterPx + RadiusPx * Mathf.Sin(trailRad2) - 2f;
                    if (_trail1 != null) { _trail1.style.left = tx1; _trail1.style.top = ty1; }
                    if (_trail2 != null) { _trail2.style.left = tx2; _trail2.style.top = ty2; }
                }

                yield return null;
            }
        }

        private void OnDestroy()
        {
            if (_orbitCoroutine != null)
                StopCoroutine(_orbitCoroutine);
        }
    }
}
