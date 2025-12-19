using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using _Project.Sporae.Core;
using Sporae.DevTools;
using _Project.UI.HUDNotifications2_0;

namespace _Project.UI.HUDNotifications2_0
{
    /// <summary>
    /// Manager principale per sistema HUD Notifications 2.0
    /// Gestisce max 3 notifiche visibili, timing 8s/5s overflow, header toggle e colori dinamici
    /// </summary>
    public class HUDNotificationFeedManager2_0 : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HUDNotificationPool2_0 _pool;
        [SerializeField] private RectTransform _notificationContainer; // Parent per notifiche attive (VerticalLayoutGroup)
        [SerializeField] private HUDNotificationHeader2_0 _header; // Header "NOTIFICATIONS"
        [SerializeField] private RectTransform _rootRectTransform; // Root container
        
        private HUDNotificationConfig2_0 _config;
        private List<HUDNotificationItem2_0> _activeNotifications = new List<HUDNotificationItem2_0>();
        private static int _nextNotificationId = 1;
        
        private void Awake()
        {
            SetupRectTransform();
            LoadConfig();
            InitializeHeader();
            
            if (ServiceContainer.Instance != null)
                ServiceContainer.Instance.Register(this);
        }
        
        private void SetupRectTransform()
        {
            if (_rootRectTransform != null)
            {
                // Configura posizione top-right
                _rootRectTransform.anchorMin = new Vector2(1f, 1f); // Top-right
                _rootRectTransform.anchorMax = new Vector2(1f, 1f);
                _rootRectTransform.pivot = new Vector2(1f, 1f); // Top-right pivot
                
                // Applica offset dalla config (se disponibile)
                if (_config != null)
                {
                    _rootRectTransform.anchoredPosition = new Vector2(
                        -_config.ContainerRightOffset,
                        -_config.ContainerTopOffset
                    );
                    _rootRectTransform.sizeDelta = new Vector2(_config.ContainerWidth, 0f);
                }
            }
            
            // Setup container notifiche
            if (_notificationContainer != null)
            {
                _notificationContainer.anchorMin = new Vector2(0f, 1f); // Top-left
                _notificationContainer.anchorMax = new Vector2(1f, 1f); // Top-right (stretch horizontal)
                _notificationContainer.pivot = new Vector2(0.5f, 1f); // Top-center
                
                // Calcola offset Y negativo basato sull'altezza dell'header
                float headerHeight = 0f;
                if (_header != null)
                {
                    var headerRectTransform = _header.GetComponent<RectTransform>();
                    if (headerRectTransform != null)
                    {
                        headerHeight = headerRectTransform.rect.height;
                    }
                }
                
                if (headerHeight <= 0f) headerHeight = 40f; // Fallback
                
                float marginBottom = _config != null ? _config.HeaderMarginBottom : 6f;
                _notificationContainer.anchoredPosition = new Vector2(0f, -(headerHeight + marginBottom));
                _notificationContainer.sizeDelta = new Vector2(0f, 0f); // Width: stretch, Height: auto
                
                // ContentSizeFitter per adattare altezza
                var contentSizeFitter = _notificationContainer.GetComponent<ContentSizeFitter>();
                if (contentSizeFitter == null)
                    contentSizeFitter = _notificationContainer.gameObject.AddComponent<ContentSizeFitter>();
                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                
                // VerticalLayoutGroup per spacing
                var layoutGroup = _notificationContainer.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup == null)
                    layoutGroup = _notificationContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                
                float gap = _config != null ? _config.ToastGap : 6f;
                layoutGroup.spacing = gap;
                layoutGroup.childAlignment = TextAnchor.UpperRight;
                layoutGroup.childControlHeight = false;
                layoutGroup.childControlWidth = true;
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.childForceExpandWidth = true;
                
                LayoutRebuilder.ForceRebuildLayoutImmediate(_notificationContainer);
            }
        }
        
        private void LoadConfig()
        {
            _config = Resources.Load<HUDNotificationConfig2_0>("Configs/HUDNotificationConfig2.0");
            if (_config == null)
            {
                Debug.LogWarning("[HUDNotificationFeedManager2.0] Config non trovato in Resources/Configs/! Usando valori default.");
            }
        }
        
        private void InitializeHeader()
        {
            if (_header != null && _config != null)
            {
                _header.Initialize(_config);
                _header.OnToggleExpansion += OnHeaderToggle;
                
                // Container inizialmente nascosto (header chiuso di default)
                if (_notificationContainer != null)
                {
                    _notificationContainer.gameObject.SetActive(false);
                }
            }
        }
        
        private void OnHeaderToggle(bool isExpanded)
        {
            // Mostra/nascondi container notifiche
            if (_notificationContainer != null)
            {
                _notificationContainer.gameObject.SetActive(isExpanded);
                
                // Se espanso, riaggiungi notifiche al container
                if (isExpanded && _activeNotifications.Count > 0)
                {
                    foreach (var notification in _activeNotifications)
                    {
                        if (notification != null && notification.transform.parent != _notificationContainer)
                        {
                            notification.transform.SetParent(_notificationContainer);
                            notification.transform.SetAsFirstSibling();
                        }
                    }
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_notificationContainer);
                }
            }
        }
        
        /// <summary>
        /// Mostra una notifica standard
        /// </summary>
        public void ShowNotification(ToastNotificationType type, string message, string code = null)
        {
            if (_config == null)
            {
                Debug.LogWarning("[HUDNotificationFeedManager2.0] Config non caricato, impossibile mostrare notifica.");
                return;
            }
            
            int notificationId = _nextNotificationId++;
            string finalCode = code ?? GenerateCode(type);
            
            // Gestione overflow: se ci sono già 3 notifiche, accorcia la più vecchia non-DANGER
            bool isOverflow = _activeNotifications.Count >= _config.MaxVisibleNotifications;
            if (isOverflow)
            {
                HandleOverflow();
            }
            
            // Ottieni notifica dal pool
            if (_pool == null)
            {
                Debug.LogError("[HUDNotificationFeedManager2.0] Pool non assegnato!");
                return;
            }
            
            var notificationItem = _pool.GetFromPool();
            if (notificationItem == null)
            {
                Debug.LogError("[HUDNotificationFeedManager2.0] Impossibile ottenere notifica dal pool!");
                return;
            }
            
            // Ottieni colore e icona dalla config
            var color = _config.GetToastColor(type);
            var icon = _config.GetToastIcon(type);
            
            // Verifica che l'icona sia valida
            if (icon == null)
            {
                Debug.LogWarning($"[HUDNotificationFeedManager2.0] Icona null per tipo {type}, usando fallback.");
                // Usa InfoIcon come fallback
                icon = _config.InfoIcon;
            }
            
            // Inizializza notifica
            notificationItem.Initialize(notificationId, type, message, finalCode, color, icon, _config);
            
            // Aggiungi al container
            if (_notificationContainer != null)
            {
                notificationItem.transform.SetParent(_notificationContainer);
                notificationItem.transform.SetAsFirstSibling(); // LIFO: più recente in alto
                
                var layoutGroup = _notificationContainer.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_notificationContainer);
                }
            }
            
            // Aggiungi in testa alla lista (LIFO)
            _activeNotifications.Insert(0, notificationItem);
            
            // Avvia auto-dismiss
            float duration = isOverflow ? _config.OverflowDismissDuration : _config.AutoDismissDuration;
            notificationItem.StartAutoDismiss(duration, OnNotificationDismissed);
            
            // Aggiorna header
            UpdateHeader();
        }
        
        /// <summary>
        /// Mostra una notifica item (con icona grande)
        /// </summary>
        public void ShowItemNotification(string itemName, int quantity, string location, string code = null, Sprite itemIcon = null)
        {
            if (_config == null)
            {
                Debug.LogWarning("[HUDNotificationFeedManager2.0] Config non caricato, impossibile mostrare notifica.");
                return;
            }
            
            int notificationId = _nextNotificationId++;
            string finalCode = code ?? GenerateCode(ToastNotificationType.ItemCollected);
            
            // Gestione overflow
            bool isOverflow = _activeNotifications.Count >= _config.MaxVisibleNotifications;
            if (isOverflow)
            {
                HandleOverflow();
            }
            
            // Ottieni notifica dal pool
            if (_pool == null)
            {
                Debug.LogError("[HUDNotificationFeedManager2.0] Pool non assegnato!");
                return;
            }
            
            var notificationItem = _pool.GetFromPool();
            if (notificationItem == null)
            {
                Debug.LogError("[HUDNotificationFeedManager2.0] Impossibile ottenere notifica dal pool!");
                return;
            }
            
            var color = _config.GetToastColor(ToastNotificationType.ItemCollected);
            
            // Inizializza notifica item
            notificationItem.InitializeItem(notificationId, itemName, quantity, location, finalCode, color, itemIcon, _config);
            
            // Aggiungi al container
            if (_notificationContainer != null)
            {
                notificationItem.transform.SetParent(_notificationContainer);
                notificationItem.transform.SetAsFirstSibling();
                
                var layoutGroup = _notificationContainer.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_notificationContainer);
                }
            }
            
            // Aggiungi in testa alla lista
            _activeNotifications.Insert(0, notificationItem);
            
            // Avvia auto-dismiss
            float duration = isOverflow ? _config.OverflowDismissDuration : _config.AutoDismissDuration;
            notificationItem.StartAutoDismiss(duration, OnNotificationDismissed);
            
            // Aggiorna header
            UpdateHeader();
        }
        
        /// <summary>
        /// Gestisce overflow: accorcia la più vecchia notifica non-DANGER a 5s
        /// </summary>
        private void HandleOverflow()
        {
            // Trova la più vecchia non-DANGER (severity <= 2)
            HUDNotificationItem2_0 toAccelerate = null;
            int toAccelerateIndex = -1;
            
            for (int i = _activeNotifications.Count - 1; i >= 0; i--)
            {
                var notification = _activeNotifications[i];
                if (notification != null && notification.Severity <= 2) // Solo INFO/WARNING
                {
                    toAccelerate = notification;
                    toAccelerateIndex = i;
                    break;
                }
            }
            
            // Se non ci sono INFO/WARNING, rimuovi la più vecchia DANGER
            if (toAccelerate == null && _activeNotifications.Count > 0)
            {
                toAccelerate = _activeNotifications[_activeNotifications.Count - 1];
                toAccelerateIndex = _activeNotifications.Count - 1;
            }
            
            // Accelera o rimuovi
            if (toAccelerate != null && toAccelerateIndex >= 0)
            {
                // Se è DANGER, rimuovi direttamente
                if (toAccelerate.Severity >= 3)
                {
                    _activeNotifications.RemoveAt(toAccelerateIndex);
                    if (_pool != null)
                        _pool.ReturnToPool(toAccelerate);
                }
                else
                {
                    // Altrimenti accorcia il timer a 5s
                    toAccelerate.StartAutoDismiss(_config.OverflowDismissDuration, OnNotificationDismissed);
                }
            }
        }
        
        /// <summary>
        /// Callback quando una notifica viene dismissata
        /// </summary>
        private void OnNotificationDismissed(HUDNotificationItem2_0 notification)
        {
            if (notification == null) return;
            
            _activeNotifications.Remove(notification);
            if (_pool != null)
                _pool.ReturnToPool(notification);
            
            // Aggiorna header
            UpdateHeader();
        }
        
        /// <summary>
        /// Aggiorna header: colore dinamico e badge contatore
        /// </summary>
        private void UpdateHeader()
        {
            if (_header == null || _config == null) return;
            
            if (_activeNotifications.Count == 0)
            {
                // Nessuna notifica: colore idle
                _header.UpdateColor(_config.ColorIdle);
                _header.UpdateBadge(0);
                return;
            }
            
            // Trova severità più alta
            int maxSeverity = _activeNotifications.Max(n => n.Severity);
            Color32 headerColor = _config.GetHeaderColor(maxSeverity);
            
            _header.UpdateColor(headerColor);
            _header.UpdateBadge(_activeNotifications.Count);
        }
        
        /// <summary>
        /// Genera codice per notifica usando prefissi sci-fi post-apocalittici
        /// </summary>
        private string GenerateCode(ToastNotificationType type)
        {
            if (_config == null)
            {
                // Fallback se config non disponibile
                string prefix = type.ToString().Substring(0, Mathf.Min(3, type.ToString().Length)).ToUpper();
                return $"{prefix}-{_nextNotificationId:D3}";
            }
            
            // Usa prefisso tematico dalla config
            string codePrefix = _config.GetCodePrefix(type);
            return $"{codePrefix}-{_nextNotificationId:D3}";
        }
        
        /// <summary>
        /// Helper methods per tipi comuni
        /// </summary>
        public void ShowSuccess(string message, string code = null)
            => ShowNotification(ToastNotificationType.Success, message, code);
        
        public void ShowError(string message, string code = null)
            => ShowNotification(ToastNotificationType.Error, message, code);
        
        public void ShowWarning(string message, string code = null)
            => ShowNotification(ToastNotificationType.Warning, message, code);
        
        public void ShowInfo(string message, string code = null)
            => ShowNotification(ToastNotificationType.Info, message, code);
        
        /// <summary>
        /// Ricarica layout (chiamato da console debug dopo modifiche runtime)
        /// </summary>
        public void RefreshLayout()
        {
            SetupRectTransform();
            
            // Riapplica dimensioni a tutte le notifiche attive
            foreach (var notification in _activeNotifications)
            {
                if (notification != null)
                    notification.RefreshLayout(_config);
            }
            
            if (_header != null)
                _header.RefreshLayout(_config);
        }
        
        /// <summary>
        /// Ottiene la config corrente (per console debug)
        /// </summary>
        public HUDNotificationConfig2_0 GetConfig() => _config;
    }
}

