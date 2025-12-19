using UnityEngine;
using UnityEngine.UIElements;

namespace Sporae.UI.UIToolkit
{
    /// <summary>
    /// Controller per il background della gameview con gradiente.
    /// Gestisce il background color della gameview secondo la palette SPORIUM.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class GameViewportBackgroundController : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Colore principale (centro gradiente): Blu-Grigio metallico #1a2328")]
        [SerializeField] private Color _mainColor = new Color(26f / 255f, 35f / 255f, 40f / 255f, 1f);
        
        [Tooltip("Colore inizio/fine gradiente: Blu-Nero scuro #0f1419")]
        [SerializeField] private Color _gradientStartEnd = new Color(15f / 255f, 20f / 255f, 25f / 255f, 1f);
        
        [Tooltip("Colore vignette: Nero-blu #0a0f12")]
        [SerializeField] private Color _vignetteColor = new Color(10f / 255f, 15f / 255f, 18f / 255f, 1f);
        
        [Tooltip("Opacità vignette overlay")]
        [SerializeField] [Range(0f, 1f)] private float _vignetteOpacity = 0.3f;
        
        [Tooltip("Usa texture gradiente invece di colore solido (se assegnata)")]
        [SerializeField] private Texture2D _gradientTexture;
        
        [Tooltip("Usa texture vignette invece di overlay solido (se assegnata)")]
        [SerializeField] private Texture2D _vignetteTexture;
        
        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _viewportGradient;
        private VisualElement _viewportVignette;
        
        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }
        
        private void OnEnable()
        {
            if (_uiDocument != null)
            {
                _uiDocument.rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }
        
        private void OnDisable()
        {
            if (_uiDocument != null && _root != null)
            {
                _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }
        
        private void Start()
        {
            // Imposta background color sulla camera principale invece di UI Toolkit
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // Usa il colore principale del gradiente come background della camera
                mainCamera.backgroundColor = _mainColor;
            }
            
            // Nascondi il background UI Toolkit perché ora usiamo il background della camera
            // Il background UI Toolkit copre la gameview, quindi lo disabilitiamo
            gameObject.SetActive(false);
        }
        
        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // Aggiorna dimensioni se necessario
            UpdateBackground();
        }
        
        private void InitializeUI()
        {
            if (_uiDocument == null || _uiDocument.rootVisualElement == null)
            {
                Debug.LogWarning("[GameViewportBackground] UIDocument o rootVisualElement non trovato!");
                return;
            }
            
            _root = _uiDocument.rootVisualElement;
            _viewportGradient = _root.Q<VisualElement>("viewport-gradient");
            _viewportVignette = _root.Q<VisualElement>("viewport-vignette");
            
            if (_viewportGradient == null)
            {
                Debug.LogWarning("[GameViewportBackground] Elemento 'viewport-gradient' non trovato in UXML!");
                return;
            }
            
            // Disabilita raycast sul background per non bloccare interazioni con gameview
            _root.pickingMode = PickingMode.Ignore;
            if (_viewportGradient != null)
                _viewportGradient.pickingMode = PickingMode.Ignore;
            if (_viewportVignette != null)
                _viewportVignette.pickingMode = PickingMode.Ignore;
            
            UpdateBackground();
        }
        
        private void UpdateBackground()
        {
            if (_viewportGradient == null)
                return;
            
            // Aggiorna background principale
            if (_gradientTexture != null)
            {
                // Usa texture gradiente se disponibile
                // Nota: UI Toolkit richiede Background object, non direttamente Texture2D
                // Per ora usa colore solido come fallback
                _viewportGradient.style.backgroundColor = new StyleColor(_mainColor);
            }
            else
            {
                // Usa colore solido centrale
                _viewportGradient.style.backgroundColor = new StyleColor(_mainColor);
            }
            
            // Aggiorna vignette
            if (_viewportVignette != null)
            {
                if (_vignetteTexture != null)
                {
                    // TODO: Usa texture vignette se disponibile
                    // Per ora usa overlay solido
                    _viewportVignette.style.backgroundColor = new StyleColor(_vignetteColor);
                    _viewportVignette.style.opacity = _vignetteOpacity;
                }
                else
                {
                    // Usa overlay solido
                    _viewportVignette.style.backgroundColor = new StyleColor(_vignetteColor);
                    _viewportVignette.style.opacity = _vignetteOpacity;
                }
            }
        }
        
        /// <summary>
        /// Aggiorna i colori del background (chiamabile da Inspector o script esterni)
        /// </summary>
        public void UpdateColors(Color mainColor, Color gradientStartEnd, Color vignetteColor, float vignetteOpacity)
        {
            _mainColor = mainColor;
            _gradientStartEnd = gradientStartEnd;
            _vignetteColor = vignetteColor;
            _vignetteOpacity = vignetteOpacity;
            UpdateBackground();
        }
        
        /// <summary>
        /// Imposta texture gradiente (chiamabile da Inspector o script esterni)
        /// </summary>
        public void SetGradientTexture(Texture2D texture)
        {
            _gradientTexture = texture;
            UpdateBackground();
        }
        
        /// <summary>
        /// Imposta texture vignette (chiamabile da Inspector o script esterni)
        /// </summary>
        public void SetVignetteTexture(Texture2D texture)
        {
            _vignetteTexture = texture;
            UpdateBackground();
        }
    }
}

