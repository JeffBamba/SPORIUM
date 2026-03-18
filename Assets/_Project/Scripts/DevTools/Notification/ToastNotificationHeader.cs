using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sporae.DevTools;

namespace Sporae.DevTools
{
    /// <summary>
    /// Componente per header "SYSTEM NOTIFICATIONS" con toggle, badge contatore, chevron rotabile
    /// Colore adattivo basato su severità più alta dei toast attivi
    /// </summary>
    public class ToastNotificationHeader : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button _toggleButton;
        [SerializeField] private Image _headerBackground; // Colore adattivo
        [SerializeField] private Image _borderImage;
        [SerializeField] private Image _alertIcon; // AlertCircle sprite
        [SerializeField] private TextMeshProUGUI _headerText; // "SYSTEM NOTIFICATIONS"
        [SerializeField] private GameObject _badgeContainer;
        [SerializeField] private TextMeshProUGUI _badgeText; // Numero toast visibili
        [SerializeField] private Image _chevronIcon; // Arrow sprite
        [SerializeField] private RectTransform _chevronTransform;
        
        private bool _isExpanded = true;
        private ToastNotificationConfig _config;
        
        /// <summary>
        /// Evento emesso quando header viene toggle (espanso/contratto)
        /// </summary>
        public event System.Action<bool> OnToggleExpansion;
        
        
        /// <summary>
        /// Inizializza l'header con la configurazione
        /// </summary>
        public void Initialize(ToastNotificationConfig config)
        {
            _config = config;
            SetupUI();
            SetupToggleButton();
            
            // Sincronizza rotazione iniziale del chevron con stato espanso
            if (_chevronTransform != null)
            {
                float expectedRotation = _isExpanded ? 0f : 180f;
                _chevronTransform.localEulerAngles = new Vector3(0, 0, expectedRotation);
            }
        }
        
        private void SetupUI()
        {
            if (_headerText != null && _config != null)
            {
                // Testo "SYSTEM NOTIFICATIONS" uppercase, monospaced, tracking aumentato
                _headerText.text = "NOTIFICHE DI SISTEMA";
                _headerText.font = _config.MonospacedFont;
                _headerText.fontSize = _config.FontSize;
                _headerText.characterSpacing = _config.CharacterSpacing;
                _headerText.fontStyle = FontStyles.UpperCase;
            }
            
            // Setup pixel art style
            SetupPixelArtStyle();
        }
        
        private void SetupPixelArtStyle()
        {
            // Background
            if (_headerBackground != null)
                _headerBackground.color = ToastNotificationConfig.BACKGROUND_COLOR;
            
            // Texture filtering pixel-perfect
            if (_borderImage != null && _borderImage.sprite != null)
                _borderImage.sprite.texture.filterMode = FilterMode.Point;
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
        /// Toggle espansione header (mostra/nascondi toast container)
        /// </summary>
        public void ToggleExpansion()
        {
            _isExpanded = !_isExpanded;

            // Rotazione chevron 180°
            if (_chevronTransform != null)
            {
                float targetRotation = _isExpanded ? 0f : 180f;
                try
                {
                    _chevronTransform.DORotate(new Vector3(0, 0, targetRotation), 0.2f);
                }
                catch (System.Exception)
                {
                    // Gestisci errore silenziosamente
                }
            }

            // Evento per mostrare/nascondere toast container
            OnToggleExpansion?.Invoke(_isExpanded);
        }
        
        /// <summary>
        /// Aggiorna il colore dell'header in base alla severità più alta
        /// </summary>
        public void UpdateColor(Color32 color)
        {
            // Colore adattivo: applica a border, text, icon
            if (_borderImage != null)
                _borderImage.color = color;
            if (_headerText != null)
                _headerText.color = color;
            if (_alertIcon != null)
                _alertIcon.color = color;
        }
        
        /// <summary>
        /// Aggiorna il badge con il numero di toast visibili
        /// Mostra solo il numero (0, 1, 2, 3...)
        /// </summary>
        public void UpdateBadge(int count)
        {
            if (_badgeContainer != null)
            {
                // Badge sempre visibile, mostra sempre il numero (anche 0)
                _badgeContainer.SetActive(true);
                if (_badgeText != null)
                    _badgeText.text = count.ToString();
            }
        }
    }
}

