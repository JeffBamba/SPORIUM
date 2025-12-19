using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Sporae.DevTools;

namespace Sporae.DevTools
{
    /// <summary>
    /// Componente UI per singolo toast notification con stile pixel art e animazioni DOTween
    /// Gestisce auto-dismiss (8s), toggle espansione, animazioni entrata/uscita
    /// </summary>
    public class ToastNotificationUIItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _borderImage; // 2px solid border
        [SerializeField] private Image _severityIcon; // Info/Warning/Danger icon
        [SerializeField] private TextMeshProUGUI _codeText;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private TextMeshProUGUI _timestampText;
        [SerializeField] private GameObject _expandedContent;
        [SerializeField] private VerticalLayoutGroup _expandedLayout;
        [SerializeField] private Button _expandButton; // Click per espandere
        
        [Header("Item Notification Layout (Optional)")]
        [SerializeField] private GameObject _standardLayoutContainer; // Container per layout standard
        [SerializeField] private GameObject _itemLayoutContainer; // Container per layout item (icona grande + location)
        [SerializeField] private Image _itemIconLarge; // Icona grande 40x40 per item notification
        [SerializeField] private TextMeshProUGUI _itemHeaderText; // "ADDED TO INVENTORY"
        [SerializeField] private TextMeshProUGUI _itemNameText; // "+X ItemName"
        [SerializeField] private TextMeshProUGUI _itemLocationText; // "📍 Location"
        
        private int _toastId;
        private ToastNotificationType _type;
        private string _message;
        private string _code;
        private Color32 _color;
        private bool _isExpanded = false;
        private Coroutine _autoDismissCoroutine;
        private Sequence _exitAnimation;
        private Sequence _glowPulseAnimation; // Animazione glow pulsante per i primi 0.5s
        
        public int ToastId => _toastId;
        public ToastNotificationType Type => _type;
        public int Severity => _type.GetSeverity();
        
        /// <summary>
        /// Inizializza il toast con i parametri specificati
        /// </summary>
        public void Initialize(int id, ToastNotificationType type, string message, string code, Color32 color, Sprite severityIcon)
        {
            _toastId = id;
            _type = type;
            _message = message;
            _code = code;
            _color = color;
            
            // DEBUG_SAFE_FIX: Corregge RectTransform per VerticalLayoutGroup
            // Per un elemento in VerticalLayoutGroup, deve avere anchor top-stretch e pivot top-center
            if (_rectTransform != null)
            {
                _rectTransform.anchorMin = new Vector2(0f, 1f); // Top-left
                _rectTransform.anchorMax = new Vector2(1f, 1f); // Top-right (stretch horizontal)
                _rectTransform.pivot = new Vector2(0.5f, 1f); // Top-center
                _rectTransform.anchoredPosition = Vector2.zero; // Reset position (layout group lo gestirà)
                _rectTransform.sizeDelta = new Vector2(0f, 100f); // Width: stretch, Height: preferita 100px
                
                // Forza aggiornamento layout
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
            }
            
            // Layout standard (ItemCollected può usare layout speciale solo se chiamato con InitializeItemNotification)
            SetupLayout(false);
            
            UpdateUI(severityIcon);
            SetupPixelArtStyle();
            SetupExpandButton();
            PlayEnterAnimation();
            StartGlowPulseAnimation();
            StartAutoDismiss();
        }
        
        /// <summary>
        /// Inizializza toast Item Notification con layout speciale
        /// </summary>
        public void InitializeItemNotification(int id, string itemName, int quantity, string location, string code, Color32 color, Sprite itemIcon)
        {
            _toastId = id;
            _type = ToastNotificationType.ItemCollected;
            _code = code ?? "INV-001";
            _color = color;
            
            // Setup RectTransform
            if (_rectTransform != null)
            {
                _rectTransform.anchorMin = new Vector2(0f, 1f);
                _rectTransform.anchorMax = new Vector2(1f, 1f);
                _rectTransform.pivot = new Vector2(0.5f, 1f);
                _rectTransform.anchoredPosition = Vector2.zero;
                _rectTransform.sizeDelta = new Vector2(0f, 100f);
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
            }
            
            // Usa layout Item Notification
            SetupLayout(true);
            
            // Popola UI Item Notification
            if (_itemHeaderText != null)
                _itemHeaderText.text = "ADDED TO INVENTORY";
            if (_itemNameText != null)
                _itemNameText.text = $"+{quantity} {itemName}";
            if (_itemLocationText != null)
                _itemLocationText.text = $"📍 {location}";
            if (_itemIconLarge != null && itemIcon != null)
            {
                _itemIconLarge.sprite = itemIcon;
                _itemIconLarge.color = color;
            }
            
            // Applica colori
            if (_borderImage != null)
            {
                if (!_borderImage.gameObject.activeSelf)
                    _borderImage.gameObject.SetActive(true);
                _borderImage.color = color;
            }
            
            SetupPixelArtStyle();
            SetupExpandButton();
            PlayEnterAnimation();
            StartGlowPulseAnimation();
            StartAutoDismiss();
        }
        
        /// <summary>
        /// Configura quale layout mostrare (standard o item notification)
        /// </summary>
        private void SetupLayout(bool useItemLayout)
        {
            if (_standardLayoutContainer != null)
                _standardLayoutContainer.SetActive(!useItemLayout);
            if (_itemLayoutContainer != null)
                _itemLayoutContainer.SetActive(useItemLayout);
        }
        
        private void UpdateUI(Sprite severityIcon)
        {
            if (_codeText != null)
                _codeText.text = _code ?? "N/A";
            if (_messageText != null)
                _messageText.text = _message;
            if (_timestampText != null)
                _timestampText.text = System.DateTime.Now.ToString("HH:mm:ss");
            
            // Colore dinamico per border, corner, icon
            if (_borderImage != null)
            {
                // DEBUG_SAFE_FIX: Assicura che il border GameObject sia attivo
                if (!_borderImage.gameObject.activeSelf)
                    _borderImage.gameObject.SetActive(true);
                
                _borderImage.color = _color;
            }
            
            if (_severityIcon != null)
            {
                _severityIcon.sprite = severityIcon;
                _severityIcon.color = _color;
            }
            
            // Testo secondario per codice e timestamp
            if (_codeText != null)
                _codeText.color = ToastNotificationConfig.TEXT_SECONDARY_DARK;
            if (_timestampText != null)
                _timestampText.color = ToastNotificationConfig.TEXT_SECONDARY_LIGHT;
        }
        
        private void SetupPixelArtStyle()
        {
            // Background semi-trasparente
            if (_backgroundImage != null)
                _backgroundImage.color = ToastNotificationConfig.BACKGROUND_COLOR;
            
            // Texture filtering pixel-perfect
            if (_borderImage != null && _borderImage.sprite != null)
                _borderImage.sprite.texture.filterMode = FilterMode.Point;
            
            // Font monospaced
            var config = Resources.Load<ToastNotificationConfig>("Configs/ToastNotificationConfig");
            if (config != null)
            {
                if (_codeText != null)
                {
                    _codeText.font = config.MonospacedFont;
                    _codeText.fontSize = config.FontSize;
                }
                if (_messageText != null)
                {
                    _messageText.font = config.MonospacedFont;
                    _messageText.fontSize = config.FontSize;
                }
                if (_timestampText != null)
                {
                    _timestampText.font = config.MonospacedFont;
                    _timestampText.fontSize = config.FontSize;
                }
            }
        }
        
        private void SetupExpandButton()
        {
            if (_expandButton != null)
            {
                _expandButton.onClick.RemoveAllListeners();
                _expandButton.onClick.AddListener(ToggleExpansion);
            }
        }
        
        private void PlayEnterAnimation()
        {
            if (_rectTransform == null || _canvasGroup == null)
                return;
            
            // Reset stato iniziale
            _canvasGroup.alpha = 0f;
            // DEBUG_SAFE_FIX: Animazione entrata da destra (offset relativo, non assoluto)
            var startPos = _rectTransform.anchoredPosition;
            _rectTransform.anchoredPosition = new Vector2(startPos.x + 100f, startPos.y); // Offset da destra
            _rectTransform.localScale = Vector3.one * 0.8f;
            
            // Animazione entrata (DOTween)
            _canvasGroup.DOFade(1f, 0.3f);
            _rectTransform.DOAnchorPosX(startPos.x, 0.3f); // Torna alla posizione originale (layout group)
            _rectTransform.DOScale(Vector3.one, 0.3f);
        }
        
        /// <summary>
        /// Avvia animazione glow pulsante per i primi 0.5s
        /// </summary>
        private void StartGlowPulseAnimation()
        {
            if (_glowPulseAnimation != null && _glowPulseAnimation.IsActive())
                _glowPulseAnimation.Kill();
            
            // Glow pulsante: aumenta intensità border/corner per 0.5s
            if (_borderImage != null)
            {
                var originalColor = _borderImage.color;
                _glowPulseAnimation = DOTween.Sequence();
                
                // Pulsa 2 volte in 0.5s (fade in/out rapido)
                _glowPulseAnimation.Append(_borderImage.DOColor(new Color32(
                    (byte)Mathf.Min(255, originalColor.r + 50),
                    (byte)Mathf.Min(255, originalColor.g + 50),
                    (byte)Mathf.Min(255, originalColor.b + 50),
                    (byte)(originalColor.a * 255)), 0.125f));
                _glowPulseAnimation.Append(_borderImage.DOColor(originalColor, 0.125f));
                _glowPulseAnimation.Append(_borderImage.DOColor(new Color32(
                    (byte)Mathf.Min(255, originalColor.r + 50),
                    (byte)Mathf.Min(255, originalColor.g + 50),
                    (byte)Mathf.Min(255, originalColor.b + 50),
                    (byte)(originalColor.a * 255)), 0.125f));
                _glowPulseAnimation.Append(_borderImage.DOColor(originalColor, 0.125f));
            }
        }
        
        /// <summary>
        /// Toggle espansione per mostrare/nascondere contenuto dettagliato
        /// </summary>
        public void ToggleExpansion()
        {
            _isExpanded = !_isExpanded;
            
            if (_expandedContent != null)
            {
                if (_isExpanded)
                {
                    _expandedContent.SetActive(true);
                    if (_expandedLayout != null)
                        _expandedLayout.enabled = true; // Force layout update
                    if (_canvasGroup != null)
                        _canvasGroup.DOFade(1f, 0.3f); // Fade in expanded content
                }
                else
                {
                    if (_canvasGroup != null)
                    {
                        _canvasGroup.DOFade(0.8f, 0.3f).OnComplete(() => {
                            if (_expandedContent != null)
                                _expandedContent.SetActive(false);
                        });
                    }
                    else
                    {
                        _expandedContent.SetActive(false);
                    }
                }
            }
        }
        
        private void StartAutoDismiss()
        {
            if (_autoDismissCoroutine != null)
                StopCoroutine(_autoDismissCoroutine);
            _autoDismissCoroutine = StartCoroutine(AutoDismissRoutine());
        }
        
        private IEnumerator AutoDismissRoutine()
        {
            yield return new WaitForSeconds(8f);
            OnDismiss();
        }
        
        /// <summary>
        /// Dismissa il toast con animazione uscita
        /// </summary>
        public void OnDismiss()
        {
            if (_autoDismissCoroutine != null)
            {
                StopCoroutine(_autoDismissCoroutine);
                _autoDismissCoroutine = null;
            }
            
            if (_rectTransform == null || _canvasGroup == null)
            {
                gameObject.SetActive(false);
                return;
            }
            
            // Animazione uscita (sequenza inversa)
            if (_exitAnimation != null && _exitAnimation.IsActive())
                _exitAnimation.Kill();
            
            _exitAnimation = DOTween.Sequence();
            _exitAnimation.Append(_canvasGroup.DOFade(0f, 0.3f));
            _exitAnimation.Join(_rectTransform.DOAnchorPosX(100, 0.3f).SetRelative(true));
            _exitAnimation.Join(_rectTransform.DOScale(Vector3.one * 0.8f, 0.3f));
            _exitAnimation.OnComplete(() => {
                gameObject.SetActive(false);
            });
        }
        
        /// <summary>
        /// Resetta il toast per il pool (chiamato prima di ReturnToPool)
        /// </summary>
        public void ReturnToPool()
        {
            if (_exitAnimation != null && _exitAnimation.IsActive())
                _exitAnimation.Kill();
            if (_glowPulseAnimation != null && _glowPulseAnimation.IsActive())
                _glowPulseAnimation.Kill();
            
            // Reset stato
            _isExpanded = false;
            if (_expandedContent != null)
                _expandedContent.SetActive(false);
            
            // Reset layout
            SetupLayout(false);
            
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;
            if (_rectTransform != null)
                _rectTransform.localScale = Vector3.one;
        }
        
        private void OnDestroy()
        {
            // Kill animazioni in corso
            if (_exitAnimation != null && _exitAnimation.IsActive())
                _exitAnimation.Kill();
            if (_glowPulseAnimation != null && _glowPulseAnimation.IsActive())
                _glowPulseAnimation.Kill();
        }
    }
}

