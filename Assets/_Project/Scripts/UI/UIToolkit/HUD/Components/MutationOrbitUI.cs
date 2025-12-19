using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace Sporae.UI.UIToolkit.HUD
{
    /// <summary>
    /// Componente per visualizzare l'indice di mutazione con animazione orbit.
    /// </summary>
    public class MutationOrbitUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private UIDocument _uiDocument;
        
        [Header("Mutation Settings")]
        [SerializeField] private float _mutationIndex = 0.42f;
        [SerializeField] private float _orbitSpeed = 8f; // Secondi per rotazione completa
        
        private VisualElement _root;
        private VisualElement _orbitContainer;
        private VisualElement _centerIcon;
        private VisualElement _dot1;
        private VisualElement _dot2;
        private VisualElement _dot3;
        private Label _valueLabel;
        
        private Color _currentColor;
        private Coroutine _orbitCoroutine;
        
        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
        }
        
        private void Start()
        {
            InitializeUI();
            StartOrbitAnimation();
            UpdateMutation(_mutationIndex);
        }
        
        private void InitializeUI()
        {
            if (_uiDocument == null) return;
            
            _root = _uiDocument.rootVisualElement;
            if (_root == null) return;
            
            _orbitContainer = _root.Q<VisualElement>("mutation-orbit");
            _centerIcon = _root.Q<VisualElement>("icon-mutation");
            _dot1 = _root.Q<VisualElement>("orbit-dot-1");
            _dot2 = _root.Q<VisualElement>("orbit-dot-2");
            _dot3 = _root.Q<VisualElement>("orbit-dot-3");
            _valueLabel = _root.Q<Label>("mutation-value");
        }
        
        /// <summary>
        /// Aggiorna l'indice di mutazione e i colori.
        /// </summary>
        public void UpdateMutation(float index)
        {
            _mutationIndex = Mathf.Clamp01(index);
            
            // Determina colore basato su threshold
            if (_mutationIndex <= 0.33f)
            {
                _currentColor = new Color(0.498f, 1f, 0.478f, 1f); // #7FFF7A (Stable)
            }
            else if (_mutationIndex <= 0.66f)
            {
                _currentColor = new Color(0.902f, 0.788f, 0.435f, 1f); // #E6C96F (Warning)
            }
            else
            {
                _currentColor = new Color(0.827f, 0.373f, 0.373f, 1f); // #D35F5F (Critical)
            }
            
            // Aggiorna colori elementi
            if (_centerIcon != null)
            {
                _centerIcon.style.borderTopColor = new StyleColor(_currentColor);
                _centerIcon.style.borderRightColor = new StyleColor(_currentColor);
                _centerIcon.style.borderBottomColor = new StyleColor(_currentColor);
                _centerIcon.style.borderLeftColor = new StyleColor(_currentColor);
            }
            
            if (_dot1 != null) _dot1.style.backgroundColor = new StyleColor(_currentColor);
            if (_dot2 != null) _dot2.style.backgroundColor = new StyleColor(_currentColor);
            if (_dot3 != null) _dot3.style.backgroundColor = new StyleColor(_currentColor);
            
            // Aggiorna label valore
            if (_valueLabel != null)
            {
                _valueLabel.text = $"INDEX {_mutationIndex:F2}";
                _valueLabel.style.color = new StyleColor(_currentColor);
            }
        }
        
        private void StartOrbitAnimation()
        {
            if (_orbitCoroutine != null)
                StopCoroutine(_orbitCoroutine);
            
            _orbitCoroutine = StartCoroutine(OrbitAnimation());
        }
        
        private IEnumerator OrbitAnimation()
        {
            float angle = 0f;
            float radius = 18f; // Raggio orbit (48px container / 2 - 6px dot radius)
            float centerX = 24f; // Centro container (48px / 2)
            float centerY = 24f;
            
            while (true)
            {
                angle += (360f / _orbitSpeed) * Time.deltaTime;
                if (angle >= 360f) angle -= 360f;
                
                // Posiziona i 3 dots in orbita (120 gradi l'uno dall'altro)
                // Posizionamento assoluto dal centro del container
                if (_dot1 != null)
                {
                    float angle1 = angle * Mathf.Deg2Rad;
                    float x = centerX + radius * Mathf.Cos(angle1) - 3f; // -3px per centrare il dot (6px / 2)
                    float y = centerY + radius * Mathf.Sin(angle1) - 3f;
                    _dot1.style.left = x;
                    _dot1.style.top = y;
                }
                
                if (_dot2 != null)
                {
                    float angle2 = (angle + 120f) * Mathf.Deg2Rad;
                    float x = centerX + radius * Mathf.Cos(angle2) - 3f;
                    float y = centerY + radius * Mathf.Sin(angle2) - 3f;
                    _dot2.style.left = x;
                    _dot2.style.top = y;
                }
                
                if (_dot3 != null)
                {
                    float angle3 = (angle + 240f) * Mathf.Deg2Rad;
                    float x = centerX + radius * Mathf.Cos(angle3) - 3f;
                    float y = centerY + radius * Mathf.Sin(angle3) - 3f;
                    _dot3.style.left = x;
                    _dot3.style.top = y;
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

