using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using _Project.UI.HUDNotifications2_0;

namespace _Project.UI.HUDNotifications2_0
{
    /// <summary>
    /// Componente header "NOTIFICATIONS" con toggle, badge contatore, chevron rotabile
    /// Due stati: chiuso/aperto (default: chiuso)
    /// Colore dinamico basato su severità più alta delle notifiche attive
    /// </summary>
    public class HUDNotificationHeader2_0 : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button _toggleButton;
        [SerializeField] private Image _headerBackground; // Background con hover effect
        [SerializeField] private Image _borderImage; // Bordo colorato
        [SerializeField] private Image _infoIcon; // Icona "i" in cerchio (non AlertCircle)
        [SerializeField] private TextMeshProUGUI _headerText; // "NOTIFICATIONS"
        [SerializeField] private GameObject _badgeContainer;
        [SerializeField] private TextMeshProUGUI _badgeText; // Numero notifiche visibili
        [SerializeField] private Image _chevronIcon; // Chevron sprite
        [SerializeField] private RectTransform _chevronTransform; // Per rotazione
        
        private bool _isExpanded = false; // Default: chiuso
        private HUDNotificationConfig2_0 _config;
        private EventTrigger _eventTrigger;
        private bool _isHovering = false;
        
        /// <summary>
        /// Evento emesso quando header viene toggle (espanso/contratto)
        /// </summary>
        public event System.Action<bool> OnToggleExpansion;
        
        /// <summary>
        /// Inizializza l'header con la configurazione
        /// </summary>
        public void Initialize(HUDNotificationConfig2_0 config)
        {
            _config = config;
            SetupUI();
            SetupToggleButton();
            SetupHoverEffect();
            SetupBackdropBlur();
            
            // Sincronizza rotazione iniziale del chevron (chiuso = 180°)
            if (_chevronTransform != null)
            {
                float expectedRotation = _isExpanded ? 0f : 180f;
                _chevronTransform.localEulerAngles = new Vector3(0, 0, expectedRotation);
            }
            
            // Colore iniziale: idle
            UpdateColor(_config.ColorIdle);
        }
        
        private void SetupUI()
        {
            if (_headerText != null && _config != null)
            {
                // Testo "NOTIFICATIONS" (non "SYSTEM NOTIFICATIONS")
                _headerText.text = "NOTIFICHE";
                _headerText.font = _config.MonospacedFont;
                _headerText.fontSize = _config.HeaderFontSize;
                _headerText.color = _config.ColorIdle;
            }
            
            // Setup icona info (non AlertCircle)
            if (_infoIcon != null && _config != null && _config.InfoIcon != null)
            {
                _infoIcon.sprite = _config.InfoIcon;
                _infoIcon.color = _config.ColorIdle;
                
                // Dimensione icona
                var iconRect = _infoIcon.GetComponent<RectTransform>();
                if (iconRect != null)
                {
                    iconRect.sizeDelta = new Vector2(_config.HeaderIconSize, _config.HeaderIconSize);
                }
            }
            
            // Setup chevron
            if (_chevronIcon != null && _config != null && _config.ChevronIcon != null)
            {
                _chevronIcon.sprite = _config.ChevronIcon;
                
                if (_chevronTransform != null)
                {
                    _chevronTransform.sizeDelta = new Vector2(_config.HeaderChevronSize, _config.HeaderChevronSize);
                }
            }
            
            // Setup badge
            if (_badgeText != null && _config != null)
            {
                _badgeText.font = _config.MonospacedFont;
                _badgeText.fontSize = _config.HeaderBadgeFontSize;
            }
            
            // Setup background
            if (_headerBackground != null && _config != null)
            {
                _headerBackground.color = _config.BackgroundColor; // 90% opacità
            }
            
            // Setup border
            if (_borderImage != null && _config != null)
            {
                _borderImage.color = _config.ColorIdle;
                
                // Texture filtering pixel-perfect
                if (_borderImage.sprite != null)
                    _borderImage.sprite.texture.filterMode = FilterMode.Point;
            }
        }
        
        private void SetupToggleButton()
        {
            if (_toggleButton != null)
            {
                _toggleButton.onClick.RemoveAllListeners();
                _toggleButton.onClick.AddListener(ToggleExpansion);
            }
        }
        
        /// <summary>
        /// Setup hover effect: background cambia opacità 90% → 95%
        /// </summary>
        private void SetupHoverEffect()
        {
            if (_headerBackground == null || _config == null) return;
            
            _eventTrigger = _headerBackground.gameObject.GetComponent<EventTrigger>();
            if (_eventTrigger == null)
                _eventTrigger = _headerBackground.gameObject.AddComponent<EventTrigger>();
            
            // Hover enter
            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((data) => {
                _isHovering = true;
                if (_headerBackground != null)
                    _headerBackground.color = _config.BackgroundHoverColor; // 95% opacità
            });
            _eventTrigger.triggers.Add(enterEntry);
            
            // Hover exit
            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((data) => {
                _isHovering = false;
                if (_headerBackground != null)
                    _headerBackground.color = _config.BackgroundColor; // 90% opacità
            });
            _eventTrigger.triggers.Add(exitEntry);
        }
        
        /// <summary>
        /// Setup backdrop blur (approccio semplice con CanvasGroup)
        /// </summary>
        private void SetupBackdropBlur()
        {
            if (!_config.EnableBackdropBlur) return;
            
            // Approccio semplice: CanvasGroup per trasparenza
            // Nota: Unity UI non supporta nativamente backdrop-blur,
            // serve post-processing o shader custom per effetto blur vero
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            // CanvasGroup già gestisce alpha tramite color background
            // Qui possiamo aggiungere logica aggiuntiva se necessario
        }
        
        /// <summary>
        /// Toggle espansione header (mostra/nascondi container notifiche)
        /// </summary>
        public void ToggleExpansion()
        {
            _isExpanded = !_isExpanded;
            
            // Rotazione chevron 180°
            if (_chevronTransform != null && _config != null)
            {
                float targetRotation = _isExpanded ? 0f : 180f; // 0° = aperto, 180° = chiuso
                _chevronTransform.DORotate(new Vector3(0, 0, targetRotation), _config.ChevronRotationDuration);
            }
            
            // Evento per mostrare/nascondere container
            OnToggleExpansion?.Invoke(_isExpanded);
        }
        
        /// <summary>
        /// Aggiorna il colore dell'header in base alla severità più alta
        /// Applica a border, text, icon
        /// </summary>
        public void UpdateColor(Color32 color)
        {
            if (_borderImage != null)
                _borderImage.color = color;
            
            if (_headerText != null)
                _headerText.color = color;
            
            if (_infoIcon != null)
                _infoIcon.color = color;
        }
        
        /// <summary>
        /// Aggiorna il badge con il numero di notifiche visibili
        /// </summary>
        public void UpdateBadge(int count)
        {
            if (_badgeContainer != null)
            {
                _badgeContainer.SetActive(true);
                if (_badgeText != null)
                    _badgeText.text = count.ToString();
            }
        }
        
        /// <summary>
        /// Ricarica layout dopo modifiche runtime (chiamato da console debug)
        /// </summary>
        public void RefreshLayout(HUDNotificationConfig2_0 config)
        {
            _config = config;
            SetupUI();
            
            // Riapplica colore corrente
            UpdateColor(_config.ColorIdle);
        }
    }
}

