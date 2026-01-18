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
        [Tooltip("Se attivo, usa il background della Camera e disabilita questo GameObject (modalità legacy). Lascia OFF per usare il background UI Toolkit (es. mappa placeholder).")]
        [SerializeField] private bool _useCameraBackgroundAndDisableUI = false;
        
        [Tooltip("Se OFF, disabilita completamente il background gradient (utile se vuoi solo l'overlay PNG).")]
        [SerializeField] private bool _enableBackgroundGradient = false;
        
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

        [Header("Game Viewport Overlay (Static PNG)")]
        [Tooltip("Se ON, mostra l'overlay statico sulla gameview.")]
        [SerializeField] private bool _enableOverlay = false;

        [Tooltip("Texture PNG statica da usare come overlay (es. effetto CRT, scanlines, etc.).")]
        [SerializeField] private Texture2D _overlayTexture;

        [Tooltip("Opacità overlay (0.0 = trasparente, 1.0 = opaco). Regolabile in runtime.")]
        [SerializeField] [Range(0f, 1f)] private float _overlayOpacity = 0.5f;
        
        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _viewportGradient;

        // Overlay element
        private VisualElement _overlay;
        
        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            
            // Imposta sorting order per stare sopra il gioco ma sotto l'UI (HUD ha 300+)
            // 100 è un buon compromesso: sopra il gioco, sotto l'UI
            if (_uiDocument != null)
            {
                _uiDocument.sortingOrder = 100;
            }
        }
        
        private void OnEnable()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            
            InitializeUI();
            
            if (_root != null)
                _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
        
        private void OnDisable()
        {
            if (_root != null)
                _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnValidate()
        {
            // Aggiorna l'overlay quando i parametri cambiano nell'Inspector (anche in Play mode)
            if (Application.isPlaying && _overlay != null)
            {
                UpdateOverlay();
            }
        }
        
        private void Start()
        {
            if (!_useCameraBackgroundAndDisableUI)
                return;
            
            // Modalità legacy: imposta background color sulla camera principale invece di UI Toolkit
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
            _overlay = _root.Q<VisualElement>("overlay");
            
            // Pulisci eventuali elementi vecchi del sistema CRT (se esistono ancora)
            var oldCrtOverlay = _root.Q<VisualElement>("crt-overlay");
            if (oldCrtOverlay != null)
            {
                oldCrtOverlay.style.display = DisplayStyle.None;
                Debug.Log("[GameViewportBackground] Rimosso vecchio elemento crt-overlay");
            }
            
            if (_viewportGradient == null)
            {
                Debug.LogWarning("[GameViewportBackground] Elemento 'viewport-gradient' non trovato in UXML!");
                return;
            }
            
            // Disabilita raycast sul background per non bloccare interazioni con gameview
            _root.pickingMode = PickingMode.Ignore;
            if (_viewportGradient != null)
                _viewportGradient.pickingMode = PickingMode.Ignore;
            if (_overlay != null)
                _overlay.pickingMode = PickingMode.Ignore;
            
            UpdateBackground();
        }
        
        private void UpdateBackground()
        {
            if (_viewportGradient == null)
                return;
            
            // Se il background gradient è disabilitato, nascondilo completamente
            if (!_enableBackgroundGradient)
            {
                _viewportGradient.style.display = DisplayStyle.None;
                _viewportGradient.style.backgroundColor = new StyleColor(Color.clear);
            }
            else
            {
                _viewportGradient.style.display = DisplayStyle.Flex;
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
            }
            
            UpdateOverlay();
        }

        private void UpdateOverlay()
        {
            if (_overlay == null)
            {
                Debug.LogWarning("[GameViewportBackground] Elemento 'overlay' non trovato in UXML!");
                return;
            }

            bool shouldShow = _enableOverlay && _overlayTexture != null;
            
            if (!shouldShow)
            {
                _overlay.style.display = DisplayStyle.None;
                _overlay.style.backgroundImage = null;
                Debug.Log($"[GameViewportBackground] Overlay disabilitato: EnableOverlay={_enableOverlay}, Texture={(_overlayTexture != null ? _overlayTexture.name : "NULL")}");
                return;
            }

            // Forza visibilità e configurazione
            _overlay.style.display = DisplayStyle.Flex;
            _overlay.style.opacity = _overlayOpacity;
            _overlay.style.visibility = Visibility.Visible;
            
            // Assicurati che l'elemento copra tutto lo schermo
            _overlay.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            _overlay.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            _overlay.style.position = Position.Absolute;
            _overlay.style.top = 0;
            _overlay.style.left = 0;
            
            // Applica la texture
            try
            {
                if (_overlayTexture == null)
                {
                    Debug.LogError("[GameViewportBackground] Overlay texture è NULL!");
                    return;
                }
                
                var background = Background.FromTexture2D(_overlayTexture);
                
                if (background == null)
                {
                    Debug.LogError("[GameViewportBackground] Background.FromTexture2D ha restituito NULL!");
                    return;
                }
                
                _overlay.style.backgroundImage = background;
                
                // Verifica che sia stato applicato
                var appliedBg = _overlay.style.backgroundImage.value;
                Debug.Log($"[GameViewportBackground] Overlay aggiornato: Enabled={_enableOverlay}, Texture={_overlayTexture.name} ({_overlayTexture.width}x{_overlayTexture.height}), Opacity={_overlayOpacity}, Display={_overlay.style.display.value}, Width={_overlay.resolvedStyle.width}, Height={_overlay.resolvedStyle.height}, BackgroundApplied={appliedBg != null}, SortingOrder={(_uiDocument != null ? _uiDocument.sortingOrder : -1)}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameViewportBackground] Errore applicando texture overlay: {e.Message}\n{e.StackTrace}");
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

        /// <summary>
        /// Imposta texture overlay statica (chiamabile da Inspector o script esterni)
        /// </summary>
        public void SetOverlayTexture(Texture2D texture)
        {
            _overlayTexture = texture;
            UpdateOverlay();
        }

        /// <summary>
        /// Imposta opacità overlay (chiamabile da Inspector o script esterni, funziona in runtime)
        /// </summary>
        public void SetOverlayOpacity(float opacity)
        {
            _overlayOpacity = Mathf.Clamp01(opacity);
            UpdateOverlay();
        }

        /// <summary>
        /// Abilita/disabilita overlay (chiamabile da Inspector o script esterni, funziona in runtime)
        /// </summary>
        public void SetOverlayEnabled(bool enabled)
        {
            _enableOverlay = enabled;
            UpdateOverlay();
        }

        /// <summary>
        /// Forza pulizia completa di tutti gli overlay (utile per rimuovere effetti residui)
        /// </summary>
        [ContextMenu("Force Clean All Overlays")]
        public void ForceCleanAllOverlays()
        {
            if (_root == null)
                return;

            // Nascondi tutti gli elementi overlay possibili
            var elementsToClean = new[] { "overlay", "crt-overlay", "crt-scanlines", "crt-refresh-band", "crt-vignette" };
            foreach (var name in elementsToClean)
            {
                var elem = _root.Q<VisualElement>(name);
                if (elem != null)
                {
                    elem.style.display = DisplayStyle.None;
                    elem.style.backgroundImage = null;
                    elem.style.backgroundColor = StyleKeyword.None;
                }
            }

            // Disabilita overlay
            _enableOverlay = false;
            _overlayTexture = null;
            
            Debug.Log("[GameViewportBackground] Pulizia completa overlay eseguita");
        }
    }
}

