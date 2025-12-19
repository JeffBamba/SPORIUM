using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using _Project;
using _Project.Sporae.Core;
using Sporae.DevTools;

namespace Sporae.DevTools
{
    /// <summary>
    /// Manager centralizzato per sistema toast notifications
    /// Gestisce max 3 toast visibili, colore header dinamico, ID incrementale, object pooling
    /// </summary>
    public class ToastNotificationManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ToastNotificationPool _pool;
        [SerializeField] private RectTransform _toastContainer; // Parent per toast attivi (VerticalLayoutGroup)
        [SerializeField] private ToastNotificationHeader _header; // Header "NOTIFICATIONS"
        [SerializeField] private RectTransform _rootRectTransform; // Root container (opzionale, posizione gestita manualmente in Unity)
        
        private ToastNotificationConfig _config;
        private ToastNotificationHistory _history;
        private List<ToastNotificationUIItem> _activeToasts = new List<ToastNotificationUIItem>();
        private static int _nextToastId = 1; // Counter statico incrementale
        
        private const int MAX_VISIBLE_TOASTS = 3;
        private const float AUTO_DISMISS_DURATION = 8f;
        
        private void Awake()
        {
            SetupRectTransform();
            LoadConfig();
            InitializeHeader();
            
            if (ServiceContainer.Instance != null)
                ServiceContainer.Instance.Register(this);
        }
        
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
                string logLine = $"{{\"id\":\"log_{timestamp}\",\"timestamp\":{timestamp},\"location\":\"{location}\",\"message\":\"{message}\",\"data\":{dataJson},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"{hypothesisId}\"}}\n";
                System.IO.File.AppendAllText(@"d:\Sporae_Build_Beta\.cursor\debug.log", logLine);
            }
            catch { }
        }
        // #endregion

        private void SetupRectTransform()
        {
            // NOTA: La posizione, anchor, pivot e dimensioni del root RectTransform
            // devono essere configurate manualmente in Unity Editor.
            // Il codice non imposta più questi valori per permettere controllo completo.
            
            // VerticalLayoutGroup per toast container (necessario per il funzionamento)
            if (_toastContainer != null)
            {
                // #region agent log
                LogDebug("A", "ToastNotificationManager.cs:49", "ToastContainer RectTransform BEFORE setup", new System.Collections.Generic.Dictionary<string, object>
                {
                    { "anchorMin", $"{_toastContainer.anchorMin.x},{_toastContainer.anchorMin.y}" },
                    { "anchorMax", $"{_toastContainer.anchorMax.x},{_toastContainer.anchorMax.y}" },
                    { "pivot", $"{_toastContainer.pivot.x},{_toastContainer.pivot.y}" },
                    { "sizeDelta", $"{_toastContainer.sizeDelta.x},{_toastContainer.sizeDelta.y}" },
                    { "anchoredPosition", $"{_toastContainer.anchoredPosition.x},{_toastContainer.anchoredPosition.y}" },
                    { "rect", $"{_toastContainer.rect.width},{_toastContainer.rect.height}" }
                });
                // #endregion

                // DEBUG_SAFE_FIX: Configura RectTransform del ToastContainer per layout corretto
                // ToastContainer deve essere top-stretch (anchor top, stretch orizzontale) per allinearsi con header
                // E posizionato SOTTO l'header con offset Y negativo calcolato dinamicamente
                _toastContainer.anchorMin = new Vector2(0f, 1f); // Top-left
                _toastContainer.anchorMax = new Vector2(1f, 1f); // Top-right (stretch horizontal)
                _toastContainer.pivot = new Vector2(0.5f, 1f); // Top-center
                
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
                // Se headerHeight è 0, usa valore di fallback (circa 40px)
                if (headerHeight <= 0f) headerHeight = 40f;
                
                _toastContainer.anchoredPosition = new Vector2(0f, -headerHeight); // Sotto l'header
                _toastContainer.sizeDelta = new Vector2(0f, 0f); // Width: stretch, Height: auto (gestito da ContentSizeFitter)

                // Aggiungi ContentSizeFitter per adattare altezza al contenuto
                var contentSizeFitter = _toastContainer.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                if (contentSizeFitter == null)
                    contentSizeFitter = _toastContainer.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                contentSizeFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
                contentSizeFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

                var layoutGroup = _toastContainer.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup == null)
                    layoutGroup = _toastContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                
                layoutGroup.spacing = 4f; // Spacing tra toast
                layoutGroup.childAlignment = TextAnchor.UpperRight;
                layoutGroup.childControlHeight = false;
                layoutGroup.childControlWidth = true;
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.childForceExpandWidth = true;

                // Forza rebuild del layout per applicare ContentSizeFitter
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_toastContainer);

                // #region agent log
                LogDebug("B", "ToastNotificationManager.cs:107", "ToastContainer RectTransform AFTER setup", new System.Collections.Generic.Dictionary<string, object>
                {
                    { "anchorMin", $"{_toastContainer.anchorMin.x},{_toastContainer.anchorMin.y}" },
                    { "anchorMax", $"{_toastContainer.anchorMax.x},{_toastContainer.anchorMax.y}" },
                    { "pivot", $"{_toastContainer.pivot.x},{_toastContainer.pivot.y}" },
                    { "sizeDelta", $"{_toastContainer.sizeDelta.x},{_toastContainer.sizeDelta.y}" },
                    { "anchoredPosition", $"{_toastContainer.anchoredPosition.x},{_toastContainer.anchoredPosition.y}" },
                    { "rect", $"{_toastContainer.rect.width},{_toastContainer.rect.height}" },
                    { "headerHeight", headerHeight },
                    { "layoutGroupSpacing", layoutGroup.spacing },
                    { "layoutGroupChildControlWidth", layoutGroup.childControlWidth },
                    { "layoutGroupChildControlHeight", layoutGroup.childControlHeight },
                    { "contentSizeFitterHorizontalFit", contentSizeFitter.horizontalFit.ToString() },
                    { "contentSizeFitterVerticalFit", contentSizeFitter.verticalFit.ToString() }
                });
                // #endregion
            }
        }
        
        private void LoadConfig()
        {
            _config = Resources.Load<ToastNotificationConfig>("Configs/ToastNotificationConfig");
            if (_config == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "ToastNotificationConfig non trovato in Resources/Configs/! Usando valori default.");
            }
            
            _history = new ToastNotificationHistory(_config?.MaxHistoryEntries ?? 100);
        }
        
        private void InitializeHeader()
        {
            if (_header != null && _config != null)
            {
                _header.Initialize(_config);
                _header.OnToggleExpansion += OnHeaderToggle;
                
                // DEBUG_SAFE_FIX: Assicura che il container sia attivo all'inizio (header è espanso di default)
                // Sincronizza lo stato iniziale: header è espanso di default (_isExpanded = true)
                if (_toastContainer != null)
                {
                    _toastContainer.gameObject.SetActive(true);
                }
            }
        }
        
        private void OnHeaderToggle(bool isExpanded)
        {
            // Mostra/nascondi toast container
            if (_toastContainer != null)
            {
                _toastContainer.gameObject.SetActive(isExpanded);
                
                // DEBUG_SAFE_FIX: Se il container è espanso ma non ci sono toast nel container,
                // riaggiungi i toast dalla lista _activeToasts
                if (isExpanded && _activeToasts.Count > 0)
                {
                    int toastsInContainer = 0;
                    for (int i = 0; i < _activeToasts.Count; i++)
                    {
                        if (_activeToasts[i] != null && _activeToasts[i].transform.parent == _toastContainer)
                            toastsInContainer++;
                    }
                    
                    if (toastsInContainer == 0)
                    {
                        foreach (var toast in _activeToasts)
                        {
                            if (toast != null && toast.transform.parent != _toastContainer)
                            {
                                toast.transform.SetParent(_toastContainer);
                                toast.transform.SetAsFirstSibling();
                                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_toastContainer);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Mostra un toast notification
        /// </summary>
        public void ShowToast(ToastNotificationType type, string message, string code = null)
        {
            if (_config == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "ToastNotificationManager: Config non caricato, impossibile mostrare toast.");
                return;
            }
            
            int toastId = _nextToastId++;
            string finalCode = code ?? GenerateCode(type);
            
            // Limita a MAX_VISIBLE_TOASTS con sistema di priorità
            // Priorità: DANGER (severity 3-4) > WARNING (severity 2) > INFO (severity 0-1)
            // Le DANGER non vengono mai rimosse
            if (_activeToasts.Count >= MAX_VISIBLE_TOASTS)
            {
                // Trova la più vecchia tra INFO/WARNING (severity 0-2), escludi DANGER (severity 3-4)
                ToastNotificationUIItem toRemove = null;
                int toRemoveIndex = -1;
                
                for (int i = 0; i < _activeToasts.Count; i++)
                {
                    var toast = _activeToasts[i];
                    if (toast != null && toast.Severity <= 2) // Solo INFO (0-1) o WARNING (2)
                    {
                        toRemove = toast;
                        toRemoveIndex = i;
                        break; // Rimuovi la prima (più vecchia) tra INFO/WARNING
                    }
                }
                
                // Se non ci sono INFO/WARNING, rimuovi la più vecchia DANGER solo se necessario
                // (questo non dovrebbe mai accadere se MAX_VISIBLE_TOASTS = 3 e abbiamo priorità)
                if (toRemove == null && _activeToasts.Count > 0)
                {
                    toRemove = _activeToasts[0];
                    toRemoveIndex = 0;
                }
                
                if (toRemove != null && toRemoveIndex >= 0)
                {
                    _activeToasts.RemoveAt(toRemoveIndex);
                    if (_pool != null)
                        _pool.ReturnToPool(toRemove);
                }
            }
            
            // Crea nuovo toast dal pool
            if (_pool == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "ToastNotificationManager: Pool non assegnato!");
                return;
            }
            
            var toastItem = _pool.GetFromPool();
            if (toastItem == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "ToastNotificationManager: Impossibile ottenere toast dal pool!");
                return;
            }
            
            // Ottieni colore e icona dalla config
            var color32 = _config.GetColor32(type);
            var severityIcon = _config.GetSeverityIcon(type);
            
            // DEBUG_SAFE_FIX: Inizializza PRIMA di SetParent per correggere RectTransform
            toastItem.Initialize(toastId, type, message, finalCode, color32, severityIcon);
            
            if (_toastContainer != null)
            {
                // #region agent log
                LogDebug("D", "ToastNotificationManager.cs:196", "ToastContainer BEFORE adding toast", new System.Collections.Generic.Dictionary<string, object>
                {
                    { "containerRect", $"{_toastContainer.rect.width},{_toastContainer.rect.height}" },
                    { "containerSizeDelta", $"{_toastContainer.sizeDelta.x},{_toastContainer.sizeDelta.y}" },
                    { "containerAnchoredPosition", $"{_toastContainer.anchoredPosition.x},{_toastContainer.anchoredPosition.y}" },
                    { "activeToastsCount", _activeToasts.Count },
                    { "toastId", toastId }
                });
                // #endregion

                toastItem.transform.SetParent(_toastContainer);
                // LIFO: più recente in alto (SetAsFirstSibling invece di SetAsLastSibling)
                toastItem.transform.SetAsFirstSibling();
                
                // DEBUG_SAFE_FIX: Forza aggiornamento layout immediato DOPO Initialize e SetParent
                var layoutGroup = _toastContainer.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup != null)
                {
                    // Forza il layout group a ricalcolare le posizioni
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_toastContainer);
                }

                // #region agent log
                LogDebug("D", "ToastNotificationManager.cs:210", "ToastContainer AFTER adding toast", new System.Collections.Generic.Dictionary<string, object>
                {
                    { "containerRect", $"{_toastContainer.rect.width},{_toastContainer.rect.height}" },
                    { "containerSizeDelta", $"{_toastContainer.sizeDelta.x},{_toastContainer.sizeDelta.y}" },
                    { "containerAnchoredPosition", $"{_toastContainer.anchoredPosition.x},{_toastContainer.anchoredPosition.y}" },
                    { "activeToastsCount", _activeToasts.Count + 1 },
                    { "toastId", toastId },
                    { "toastRect", $"{toastItem.GetComponent<RectTransform>().rect.width},{toastItem.GetComponent<RectTransform>().rect.height}" }
                });
                // #endregion
            }
            
            // LIFO: aggiungi in testa alla lista (più recente prima)
            _activeToasts.Insert(0, toastItem);
            
            // Aggiorna colore header dinamico
            UpdateHeaderColor();
            
            // Aggiungi a history
            if (_config.EnableHistory && _history != null)
            {
                _history.Add(new ToastNotificationHistory.HistoryEntry
                {
                    Id = toastId,
                    Code = finalCode,
                    Type = type,
                    Message = message,
                    Color = color32,
                    Timestamp = System.DateTime.Now,
                    Source = GetCallerSource()
                });
            }
        }
        
        private void UpdateHeaderColor()
        {
            if (_header == null)
                return;
            
            if (_activeToasts.Count == 0)
            {
                // Nessun toast: usa colore neutro BLUE
                _header.UpdateColor(ToastNotificationConfig.COLOR_BLUE_NEUTRAL);
                _header.UpdateBadge(0);
                return;
            }
            
            // Trova severità più alta tra toast attivi
            int maxSeverity = _activeToasts.Max(t => t.Severity);
            var highestSeverityType = _activeToasts
                .First(t => t.Severity == maxSeverity)
                .Type;
            
            // Mappa severità a colore palette
            Color32 headerColor = maxSeverity switch
            {
                0 or 1 => ToastNotificationConfig.COLOR_INFO,      // Success/Info → Verde LED
                2 => ToastNotificationConfig.COLOR_WARNING,        // Warning → Giallo
                3 or 4 => ToastNotificationConfig.COLOR_DANGER,    // Error/Critical → Rosso
                _ => ToastNotificationConfig.COLOR_BLUE_NEUTRAL    // Default → Blu neutro
            };
            
            _header.UpdateColor(headerColor);
            _header.UpdateBadge(_activeToasts.Count);
        }
        
        private string GenerateCode(ToastNotificationType type)
        {
            if (_config == null)
                return $"TOAST-{_nextToastId:D3}";
            
            string prefix = _config.GetCodePrefix(type);
            return $"{prefix}{_nextToastId:D3}";
        }
        
        private string GetCallerSource()
        {
            try
            {
                // Stack trace per identificare chiamante
                var stackTrace = new StackTrace(2, true);
                var frame = stackTrace.GetFrame(0);
                if (frame != null)
                {
                    var fileName = frame.GetFileName();
                    var lineNumber = frame.GetFileLineNumber();
                    if (!string.IsNullOrEmpty(fileName))
                        return $"{System.IO.Path.GetFileName(fileName)}:{lineNumber}";
                }
            }
            catch
            {
                // Ignora errori stack trace
            }
            
            return "Unknown";
        }
        
        // Helper methods per tipi comuni
        public void ShowSuccess(string message, string code = null)
            => ShowToast(ToastNotificationType.Success, message, code);
        
        public void ShowError(string message, string code = null)
            => ShowToast(ToastNotificationType.Error, message, code);
        
        public void ShowWarning(string message, string code = null)
            => ShowToast(ToastNotificationType.Warning, message, code);
        
        public void ShowInfo(string message, string code = null)
            => ShowToast(ToastNotificationType.Info, message, code);
        
        /// <summary>
        /// Mostra toast Item Notification con layout speciale
        /// </summary>
        public void ShowItemNotification(string itemName, int quantity, string location, string code = null, Sprite itemIcon = null)
        {
            if (_config == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "ToastNotificationManager: Config non caricato, impossibile mostrare toast.");
                return;
            }
            
            int toastId = _nextToastId++;
            string finalCode = code ?? GenerateCode(ToastNotificationType.ItemCollected);
            var color32 = _config.GetColor32(ToastNotificationType.ItemCollected);
            
            // Limita a MAX_VISIBLE_TOASTS con sistema di priorità
            if (_activeToasts.Count >= MAX_VISIBLE_TOASTS)
            {
                // Trova la più vecchia tra INFO/WARNING (severity 0-2), escludi DANGER (severity 3-4)
                ToastNotificationUIItem toRemove = null;
                int toRemoveIndex = -1;
                
                for (int i = 0; i < _activeToasts.Count; i++)
                {
                    var toast = _activeToasts[i];
                    if (toast != null && toast.Severity <= 2) // Solo INFO (0-1) o WARNING (2)
                    {
                        toRemove = toast;
                        toRemoveIndex = i;
                        break;
                    }
                }
                
                if (toRemove == null && _activeToasts.Count > 0)
                {
                    toRemove = _activeToasts[0];
                    toRemoveIndex = 0;
                }
                
                if (toRemove != null && toRemoveIndex >= 0)
                {
                    _activeToasts.RemoveAt(toRemoveIndex);
                    if (_pool != null)
                        _pool.ReturnToPool(toRemove);
                }
            }
            
            // Crea nuovo toast dal pool
            if (_pool == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "ToastNotificationManager: Pool non assegnato!");
                return;
            }
            
            var toastItem = _pool.GetFromPool();
            if (toastItem == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "ToastNotificationManager: Impossibile ottenere toast dal pool!");
                return;
            }
            
            // Usa InitializeItemNotification per layout speciale
            toastItem.InitializeItemNotification(toastId, itemName, quantity, location, finalCode, color32, itemIcon);
            
            if (_toastContainer != null)
            {
                toastItem.transform.SetParent(_toastContainer);
                toastItem.transform.SetAsFirstSibling(); // LIFO: più recente in alto
                
                var layoutGroup = _toastContainer.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup != null)
                {
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_toastContainer);
                }
            }
            
            // LIFO: aggiungi in testa alla lista
            _activeToasts.Insert(0, toastItem);
            
            // Aggiorna colore header dinamico
            UpdateHeaderColor();
            
            // Aggiungi a history
            if (_config.EnableHistory && _history != null)
            {
                _history.Add(new ToastNotificationHistory.HistoryEntry
                {
                    Id = toastId,
                    Code = finalCode,
                    Type = ToastNotificationType.ItemCollected,
                    Message = $"+{quantity} {itemName} @ {location}",
                    Color = color32,
                    Timestamp = System.DateTime.Now,
                    Source = GetCallerSource()
                });
            }
        }
        
        // Banner (persistenti) - usa UINotification esistente
        public void ShowBanner(string message, ToastNotificationType type, out System.Action clearCallback)
        {
            var uiNotification = ServiceContainer.Instance?.Get<UINotification>();
            if (uiNotification != null && _config != null)
            {
                var color = _config.GetColor(type);
                uiNotification.ShowBanner(message, color, out clearCallback);
            }
            else
            {
                clearCallback = () => { };
            }
        }
        
        // History access
        public ToastNotificationHistory GetHistory() => _history;
        
        private void Update()
        {
            // Rimuovi toast completati (auto-dismiss)
            for (int i = _activeToasts.Count - 1; i >= 0; i--)
            {
                if (_activeToasts[i] == null || !_activeToasts[i].gameObject.activeSelf)
                {
                    var item = _activeToasts[i];
                    _activeToasts.RemoveAt(i);
                    if (_pool != null && item != null)
                        _pool.ReturnToPool(item);
                }
            }
            
            // Aggiorna header color se necessario
            if (_activeToasts.Count > 0)
                UpdateHeaderColor();
        }
    }
}

