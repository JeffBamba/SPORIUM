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
        
        // #region agent log
        private static void LogDebug(string hypothesisId, string location, string message, System.Collections.Generic.Dictionary<string, object> data)
        {
            try
            {
                long timestamp = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
                System.Text.StringBuilder dataJson = new System.Text.StringBuilder("{");
                bool first = true;
                foreach (var kvp in data)
                {
                    if (!first) dataJson.Append(",");
                    first = false;
                    dataJson.Append($"\"{kvp.Key}\":");
                    if (kvp.Value is bool) dataJson.Append(kvp.Value.ToString().ToLower());
                    else if (kvp.Value is string) dataJson.Append($"\"{kvp.Value}\"");
                    else if (kvp.Value == null) dataJson.Append("null");
                    else dataJson.Append(kvp.Value.ToString().Replace(",", "."));
                }
                dataJson.Append("}");
                string json = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"{hypothesisId}\",\"location\":\"{location}\",\"message\":\"{message}\",\"timestamp\":{timestamp},\"data\":{dataJson}}}";
                System.IO.File.AppendAllText(@"d:\Sporae_Build_Beta\.cursor\debug.log", json + "\n");
            }
            catch { }
        }
        // #endregion
        
        /// <summary>
        /// Inizializza l'header con la configurazione
        /// </summary>
        public void Initialize(ToastNotificationConfig config)
        {
            // #region agent log
            LogDebug("A", "ToastNotificationHeader.cs:38", "Initialize called", new System.Collections.Generic.Dictionary<string, object> { { "configNotNull", config != null }, { "toggleButtonNotNull", _toggleButton != null }, { "chevronTransformNotNull", _chevronTransform != null }, { "isExpanded", _isExpanded } });
            // #endregion
            _config = config;
            SetupUI();
            SetupToggleButton();
            
            // #region agent log
            LogDebug("E", "ToastNotificationHeader.cs:48", "Initialize completed - checking chevron initial rotation", new System.Collections.Generic.Dictionary<string, object> { { "chevronTransformNotNull", _chevronTransform != null }, { "chevronCurrentRotation", _chevronTransform != null ? (object)_chevronTransform.localEulerAngles.z : (object)(-1f) }, { "expectedRotation", 0f }, { "isExpanded", _isExpanded } });
            // #endregion
            
            // Sincronizza rotazione iniziale del chevron con stato espanso
            if (_chevronTransform != null)
            {
                float expectedRotation = _isExpanded ? 0f : 180f;
                _chevronTransform.localEulerAngles = new Vector3(0, 0, expectedRotation);
                // #region agent log
                LogDebug("E", "ToastNotificationHeader.cs:56", "Chevron initial rotation synchronized", new System.Collections.Generic.Dictionary<string, object> { { "setRotation", expectedRotation }, { "actualRotation", _chevronTransform.localEulerAngles.z }, { "isExpanded", _isExpanded } });
                // #endregion
            }
        }
        
        private void SetupUI()
        {
            if (_headerText != null && _config != null)
            {
                // Testo "SYSTEM NOTIFICATIONS" uppercase, monospaced, tracking aumentato
                _headerText.text = "SYSTEM NOTIFICATIONS";
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
            // #region agent log
            LogDebug("A", "ToastNotificationHeader.cs:98", "SetupToggleButton entry", new System.Collections.Generic.Dictionary<string, object> { { "toggleButtonNotNull", _toggleButton != null } });
            // #endregion
            if (_toggleButton != null)
            {
                // #region agent log
                LogDebug("A", "ToastNotificationHeader.cs:103", "Button state before setup", new System.Collections.Generic.Dictionary<string, object> { { "isInteractable", _toggleButton.interactable }, { "enabled", _toggleButton.enabled }, { "listenersCount", _toggleButton.onClick.GetPersistentEventCount() } });
                // #endregion
                _toggleButton.onClick.RemoveAllListeners();
                _toggleButton.onClick.AddListener(ToggleExpansion);
                // #region agent log
                LogDebug("B", "ToastNotificationHeader.cs:108", "Button listener added", new System.Collections.Generic.Dictionary<string, object> { { "listenersCount", _toggleButton.onClick.GetPersistentEventCount() }, { "isInteractable", _toggleButton.interactable } });
                // #endregion
            }
            else
            {
                // #region agent log
                LogDebug("A", "ToastNotificationHeader.cs:114", "ERROR: _toggleButton is null", new System.Collections.Generic.Dictionary<string, object>());
                // #endregion
            }
        }
        
        /// <summary>
        /// Toggle espansione header (mostra/nascondi toast container)
        /// </summary>
        public void ToggleExpansion()
        {
            // #region agent log
            LogDebug("B", "ToastNotificationHeader.cs:125", "ToggleExpansion CALLED", new System.Collections.Generic.Dictionary<string, object> { { "isExpandedBefore", _isExpanded } });
            // #endregion
            _isExpanded = !_isExpanded;

            // #region agent log
            LogDebug("C", "ToastNotificationHeader.cs:130", "Before chevron animation", new System.Collections.Generic.Dictionary<string, object> { { "isExpandedAfter", _isExpanded }, { "chevronTransformNotNull", _chevronTransform != null }, { "chevronCurrentRotation", _chevronTransform != null ? (object)_chevronTransform.localEulerAngles.z : (object)(-1f) } });
            // #endregion
            // Rotazione chevron 180°
            if (_chevronTransform != null)
            {
                float targetRotation = _isExpanded ? 0f : 180f;
                // #region agent log
                LogDebug("D", "ToastNotificationHeader.cs:136", "Starting DOTween animation", new System.Collections.Generic.Dictionary<string, object> { { "targetRotation", targetRotation }, { "currentRotation", _chevronTransform.localEulerAngles.z }, { "isExpanded", _isExpanded } });
                // #endregion
                try
                {
                    _chevronTransform.DORotate(new Vector3(0, 0, targetRotation), 0.2f);
                    // #region agent log
                    LogDebug("D", "ToastNotificationHeader.cs:142", "DOTween animation started successfully", new System.Collections.Generic.Dictionary<string, object> { { "targetRotation", targetRotation } });
                    // #endregion
                }
                catch (System.Exception ex)
                {
                    // #region agent log
                    LogDebug("D", "ToastNotificationHeader.cs:148", "ERROR: DOTween animation failed", new System.Collections.Generic.Dictionary<string, object> { { "error", ex.Message }, { "stackTrace", ex.StackTrace } });
                    // #endregion
                }
            }
            else
            {
                // #region agent log
                LogDebug("C", "ToastNotificationHeader.cs:155", "ERROR: _chevronTransform is null", new System.Collections.Generic.Dictionary<string, object>());
                // #endregion
            }

            // Evento per mostrare/nascondere toast container
            // #region agent log
            LogDebug("B", "ToastNotificationHeader.cs:161", "Invoking OnToggleExpansion event", new System.Collections.Generic.Dictionary<string, object> { { "isExpanded", _isExpanded }, { "hasListeners", OnToggleExpansion != null } });
            // #endregion
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

