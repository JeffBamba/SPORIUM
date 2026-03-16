using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Sporae.DevTools;
using _Project.UI.HUDNotifications2_0;

namespace _Project.UI.HUDNotifications2_0
{
    /// <summary>
    /// Componente UI per singola notifica HUD 2.0
    /// Gestisce layout standard/item, auto-dismiss configurabile, animazioni entrata/uscita
    /// </summary>
    public class HUDNotificationItem2_0 : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _borderImage; // 2px solid border
        [SerializeField] private Image _severityIcon; // Info/Warning/Danger icon
        
        [Header("Standard Layout")]
        [SerializeField] private GameObject _standardLayoutContainer;
        [SerializeField] private TextMeshProUGUI _codeText;
        [SerializeField] private TextMeshProUGUI _messageText;
        
        [Header("Item Layout")]
        [SerializeField] private GameObject _itemLayoutContainer;
        [SerializeField] private Image _itemIconLarge; // Icona grande 40x40
        [SerializeField] private TextMeshProUGUI _itemHeaderText; // "ADDED TO INVENTORY"
        [SerializeField] private TextMeshProUGUI _itemNameText; // "+X ItemName"
        [SerializeField] private TextMeshProUGUI _itemLocationText; // "📍 Location"
        
        private int _notificationId;
        private ToastNotificationType _type;
        private string _message;
        private string _code;
        private Color32 _color;
        private HUDNotificationConfig2_0 _config;
        private Coroutine _autoDismissCoroutine;
        private Sequence _exitAnimation;
        private System.Action<HUDNotificationItem2_0> _onDismissedCallback;
        
        public int NotificationId => _notificationId;
        public ToastNotificationType Type => _type;
        public int Severity => _type.GetSeverity();
        
        /// <summary>
        /// Inizializza notifica standard
        /// </summary>
        public void Initialize(int id, ToastNotificationType type, string message, string code, Color32 color, Sprite icon, HUDNotificationConfig2_0 config)
        {
            _notificationId = id;
            _type = type;
            _message = message;
            _code = code;
            _color = color;
            _config = config;
            
            SetupRectTransform();
            SetupLayout(false); // Layout standard
            UpdateUI(icon);
            SetupPixelArtStyle();
            PlayEnterAnimation();
        }
        
        /// <summary>
        /// Inizializza notifica item (con icona grande)
        /// </summary>
        public void InitializeItem(int id, string itemName, int quantity, string location, string code, Color32 color, Sprite itemIcon, HUDNotificationConfig2_0 config)
        {
            _notificationId = id;
            _type = ToastNotificationType.ItemCollected;
            _code = code ?? "INV-001";
            _color = color;
            _config = config;
            
            SetupRectTransform();
            SetupLayout(true); // Layout item
            
            // Popola UI item
            if (_itemHeaderText != null)
                _itemHeaderText.text = "AGGIUNTO ALL'INVENTARIO";
            if (_itemNameText != null)
                _itemNameText.text = $"+{quantity} {itemName}";
            if (_itemLocationText != null)
                _itemLocationText.text = $"📍 {location}";
            if (_itemIconLarge != null && itemIcon != null)
            {
                _itemIconLarge.sprite = itemIcon;
                _itemIconLarge.color = color;
                
                // Dimensione icona
                var iconRect = _itemIconLarge.GetComponent<RectTransform>();
                if (iconRect != null && _config != null)
                {
                    iconRect.sizeDelta = new Vector2(_config.ItemIconSize, _config.ItemIconSize);
                }
            }
            
            // Applica colori
            if (_borderImage != null)
            {
                if (!_borderImage.gameObject.activeSelf)
                    _borderImage.gameObject.SetActive(true);
                _borderImage.color = color;
            }
            
            SetupPixelArtStyle();
            PlayEnterAnimation();
        }
        
        private void SetupRectTransform()
        {
            if (_rectTransform != null)
            {
                _rectTransform.anchorMin = new Vector2(0f, 1f); // Top-left
                _rectTransform.anchorMax = new Vector2(1f, 1f); // Top-right (stretch horizontal)
                _rectTransform.pivot = new Vector2(0.5f, 1f); // Top-center
                _rectTransform.anchoredPosition = Vector2.zero;
                
                // Width: stretch, Height: auto (gestito da ContentSizeFitter)
                if (_config != null)
                {
                    _rectTransform.sizeDelta = new Vector2(0f, 0f);
                }
                else
                {
                    _rectTransform.sizeDelta = new Vector2(0f, 60f); // Fallback
                }
                
                LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
            }
        }
        
        private void SetupLayout(bool useItemLayout)
        {
            if (_standardLayoutContainer != null)
                _standardLayoutContainer.SetActive(!useItemLayout);
            if (_itemLayoutContainer != null)
                _itemLayoutContainer.SetActive(useItemLayout);
        }
        
        private void UpdateUI(Sprite icon)
        {
            // Layout titolo/descrizione: codice come prima riga (chiaro), messaggio come seconda riga (scuro)
            
            // Codice come titolo (prima riga)
            if (_codeText != null)
            {
                _codeText.text = _code ?? "N/A";
                
                // Colore codice: versione più chiara/saturata del colore principale
                Color32 codeColor = _color;
                // Aumenta luminosità e saturazione per il codice (titolo)
                float h, s, v;
                Color.RGBToHSV(_color, out h, out s, out v);
                v = Mathf.Min(1f, v + 0.15f); // Aumenta luminosità
                s = Mathf.Min(1f, s + 0.1f);  // Aumenta saturazione
                codeColor = Color.HSVToRGB(h, s, v);
                codeColor.a = 255; // Full alpha
                _codeText.color = codeColor;
                
                if (_config != null)
                {
                    _codeText.font = _config.MonospacedFont;
                    _codeText.fontSize = _config.ToastCodeFontSize;
                }
            }
            
            // Messaggio come descrizione (seconda riga)
            if (_messageText != null)
            {
                _messageText.text = _message;
                
                // Colore descrizione: versione più scura/muted del colore principale
                Color32 messageColor = _color;
                // Riduce luminosità per la descrizione
                float h, s, v;
                Color.RGBToHSV(_color, out h, out s, out v);
                v = Mathf.Max(0.3f, v - 0.2f); // Riduce luminosità
                s = Mathf.Max(0.3f, s - 0.15f); // Riduce saturazione
                messageColor = Color.HSVToRGB(h, s, v);
                messageColor.a = 200; // Leggermente più trasparente
                _messageText.color = messageColor;
                
                if (_config != null)
                {
                    _messageText.font = _config.MonospacedFont;
                    _messageText.fontSize = _config.ToastMessageFontSize;
                }
            }
            
            // Colore dinamico per border
            if (_borderImage != null)
            {
                if (!_borderImage.gameObject.activeSelf)
                    _borderImage.gameObject.SetActive(true);
                _borderImage.color = _color;
            }
            
            // Icona: assicurarsi che sia sempre visibile e attiva
            if (_severityIcon != null)
            {
                // Attiva l'icona se non lo è già
                if (!_severityIcon.gameObject.activeSelf)
                    _severityIcon.gameObject.SetActive(true);
                
                // Assegna sprite e colore
                if (icon != null)
                {
                    _severityIcon.sprite = icon;
                    _severityIcon.color = _color;
                    
                    // Dimensione icona
                    if (_config != null)
                    {
                        var iconRect = _severityIcon.GetComponent<RectTransform>();
                        if (iconRect != null)
                        {
                            iconRect.sizeDelta = new Vector2(_config.ToastIconSize, _config.ToastIconSize);
                        }
                    }
                }
                else
                {
                    // Se icona è null, nascondi l'icona (non dovrebbe accadere con il nuovo sistema)
                    _severityIcon.gameObject.SetActive(false);
                }
            }
            
            // Item layout font sizes
            if (_config != null)
            {
                if (_itemHeaderText != null)
                {
                    _itemHeaderText.font = _config.MonospacedFont;
                    _itemHeaderText.fontSize = _config.ItemHeaderFontSize;
                }
                if (_itemNameText != null)
                {
                    _itemNameText.font = _config.MonospacedFont;
                    _itemNameText.fontSize = _config.ItemNameFontSize;
                }
                if (_itemLocationText != null)
                {
                    _itemLocationText.font = _config.MonospacedFont;
                    _itemLocationText.fontSize = _config.ItemLocationFontSize;
                }
            }
        }
        
        private void SetupPixelArtStyle()
        {
            // Background
            if (_backgroundImage != null && _config != null)
                _backgroundImage.color = _config.BackgroundColor;
            
            // Texture filtering pixel-perfect
            if (_borderImage != null && _borderImage.sprite != null)
                _borderImage.sprite.texture.filterMode = FilterMode.Point;
            
            if (_severityIcon != null && _severityIcon.sprite != null)
                _severityIcon.sprite.texture.filterMode = FilterMode.Point;
        }
        
        private void PlayEnterAnimation()
        {
            if (_rectTransform == null || _canvasGroup == null || _config == null)
                return;
            
            // Reset stato iniziale
            _canvasGroup.alpha = 0f;
            var startPos = _rectTransform.anchoredPosition;
            _rectTransform.anchoredPosition = new Vector2(startPos.x + 100f, startPos.y); // Offset da destra
            _rectTransform.localScale = Vector3.one * 0.8f;
            
            // Animazione entrata (DOTween)
            _canvasGroup.DOFade(1f, _config.EnterAnimationDuration);
            _rectTransform.DOAnchorPosX(startPos.x, _config.EnterAnimationDuration);
            _rectTransform.DOScale(Vector3.one, _config.EnterAnimationDuration);
        }
        
        /// <summary>
        /// Avvia auto-dismiss con durata configurabile
        /// </summary>
        public void StartAutoDismiss(float duration, System.Action<HUDNotificationItem2_0> onDismissed = null)
        {
            _onDismissedCallback = onDismissed;
            
            if (_autoDismissCoroutine != null)
                StopCoroutine(_autoDismissCoroutine);
            
            _autoDismissCoroutine = StartCoroutine(AutoDismissRoutine(duration));
        }
        
        private IEnumerator AutoDismissRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            OnDismiss();
        }
        
        /// <summary>
        /// Dismissa la notifica con animazione uscita
        /// </summary>
        public void OnDismiss()
        {
            if (_autoDismissCoroutine != null)
            {
                StopCoroutine(_autoDismissCoroutine);
                _autoDismissCoroutine = null;
            }
            
            if (_rectTransform == null || _canvasGroup == null || _config == null)
            {
                gameObject.SetActive(false);
                _onDismissedCallback?.Invoke(this);
                return;
            }
            
            // Animazione uscita
            if (_exitAnimation != null && _exitAnimation.IsActive())
                _exitAnimation.Kill();
            
            _exitAnimation = DOTween.Sequence();
            _exitAnimation.Append(_canvasGroup.DOFade(0f, _config.ExitAnimationDuration));
            _exitAnimation.Join(_rectTransform.DOAnchorPosX(100, _config.ExitAnimationDuration).SetRelative(true));
            _exitAnimation.Join(_rectTransform.DOScale(Vector3.one * 0.8f, _config.ExitAnimationDuration));
            _exitAnimation.OnComplete(() => {
                gameObject.SetActive(false);
                _onDismissedCallback?.Invoke(this);
            });
        }
        
        /// <summary>
        /// Resetta la notifica per il pool
        /// </summary>
        public void ResetForPool()
        {
            if (_exitAnimation != null && _exitAnimation.IsActive())
                _exitAnimation.Kill();
            
            if (_autoDismissCoroutine != null)
            {
                StopCoroutine(_autoDismissCoroutine);
                _autoDismissCoroutine = null;
            }
            
            // Reset stato
            SetupLayout(false);
            
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;
            if (_rectTransform != null)
                _rectTransform.localScale = Vector3.one;
            
            _onDismissedCallback = null;
        }
        
        /// <summary>
        /// Ricarica layout dopo modifiche runtime (chiamato da console debug)
        /// </summary>
        public void RefreshLayout(HUDNotificationConfig2_0 config)
        {
            _config = config;
            SetupRectTransform();
            UpdateUI(_severityIcon != null ? _severityIcon.sprite : null);
            SetupPixelArtStyle();
        }
        
        private void OnDestroy()
        {
            // Kill animazioni in corso
            if (_exitAnimation != null && _exitAnimation.IsActive())
                _exitAnimation.Kill();
        }
    }
}

