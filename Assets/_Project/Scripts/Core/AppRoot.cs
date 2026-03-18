using UnityEngine;
using System.Collections.Generic;
using _Project.Sporae.Core;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.NotificationsFoundation;

namespace Sporae.Core
{
    public class AppRoot : MonoBehaviour
    {
        [Header("App Configuration")]
        [SerializeField] private bool persistBetweenScenes = true;
        [SerializeField] private bool validateOnStart = true;
        [SerializeField] private bool showDebugLogs = true;
        
        [Header("Core Systems")]
        [SerializeField] private GameManager gameManagerPrefab;
        [SerializeField] private bool autoCreateGameManager = true;

        private static AppRoot _instance;
        private Dictionary<string, object> _globalData = new Dictionary<string, object>();
        private bool _isInitialized = false;

        public static AppRoot Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AppRoot>();
                    
                    if (_instance == null)
                    {
                        SporiumLogger.LogWarning(LogCategory.Core, "Nessuna istanza di AppRoot trovata nella scena!");
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                SporiumLogger.LogWarning(LogCategory.Core, "Duplicato di AppRoot trovato! Distruggo il duplicato.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            
            if (persistBetweenScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
            
            if (validateOnStart)
            {
                ValidateConfiguration();
            }
            
            InitializeAppRoot();
        }

        private void ValidateConfiguration()
        {
            if (gameManagerPrefab == null && autoCreateGameManager)
            {
                SporiumLogger.LogWarning(LogCategory.Core, "gameManagerPrefab non assegnato ma autoCreateGameManager è attivo!");
            }
        }

        private void InitializeAppRoot()
        {
            if (_isInitialized) return;
            
            // Verifica se c'è già un GameManager registrato o presente in scena
            GameManager existingGameManager = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            if (existingGameManager == null)
                existingGameManager = FindObjectOfType<GameManager>();
            if (existingGameManager != null)
            {
                _cachedGameManager = existingGameManager; // BUG FIX: Cache il GameManager trovato
                if (showDebugLogs)
                {
                    SporiumLogger.LogInfo(LogCategory.Core, $"GameManager già presente nella scena: {existingGameManager.name}");
                }
                // Non creare un nuovo GameManager se ne esiste già uno
                _isInitialized = true;
                return;
            }
            
            _isInitialized = true;
            
            if (showDebugLogs)
            {
                SporiumLogger.LogInfo(LogCategory.Core, "AppRoot inizializzato correttamente.");
            }
        }
        
        // Metodi per gestire dati globali
        public void SetGlobalData(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
            {
                SporiumLogger.LogWarning(LogCategory.Core, "Chiave non può essere null o vuota!");
                return;
            }
            
            _globalData[key] = value;
        }

        public T GetGlobalData<T>(string key, T defaultValue = default(T))
        {
            if (string.IsNullOrEmpty(key))
            {
                SporiumLogger.LogWarning(LogCategory.Core, "Chiave non può essere null o vuota!");
                return defaultValue;
            }
            
            if (_globalData.TryGetValue(key, out object value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }
                else
                {
                    SporiumLogger.LogWarning(LogCategory.Core, $"Tipo non corrispondente per chiave '{key}'. " +
                                   $"Atteso: {typeof(T)}, Trovato: {value?.GetType()}");
                }
            }
            
            return defaultValue;
        }

        public bool HasGlobalData(string key)
        {
            return !string.IsNullOrEmpty(key) && _globalData.ContainsKey(key);
        }

        public void RemoveGlobalData(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _globalData.Remove(key);
            }
        }

        public void ClearGlobalData()
        {
            _globalData.Clear();
        }

        // Metodi per gestire sistemi core
        private GameManager _cachedGameManager;
        public GameManager GetGameManager()
        {
            if (_cachedGameManager != null)
                return _cachedGameManager;

            _cachedGameManager = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);

            // Fallback legacy: evita rotture in scene non ancora migrate al ServiceContainer.
            if (_cachedGameManager == null)
            {
                _cachedGameManager = FindObjectOfType<GameManager>();
            }
            return _cachedGameManager;
        }

        public T GetSystem<T>() where T : Component
        {
            T service = ServiceContainer.Instance?.Get<T>(suppressWarning: true);
            if (service != null)
                return service;

            return FindObjectOfType<T>();
        }

        public T[] GetAllSystems<T>() where T : Component
        {
            T service = ServiceContainer.Instance?.Get<T>(suppressWarning: true);
            if (service != null)
                return new[] { service };

            return FindObjectsOfType<T>();
        }

        // Metodi di utilità
        public void QuitApplication()
        {
            if (showDebugLogs)
            {
                SporiumLogger.LogInfo(LogCategory.Core, "Uscita applicazione richiesta.");
            }
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        public void RestartApplication()
        {
            if (showDebugLogs)
            {
                SporiumLogger.LogInfo(LogCategory.Core, "Riavvio applicazione richiesto.");
            }
            
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }

        public void SetTimeScale(float timeScale)
        {
            Time.timeScale = Mathf.Clamp(timeScale, 0f, 10f);
            
            if (showDebugLogs)
            {
                SporiumLogger.LogDebug(LogCategory.Core, $"TimeScale impostato a: {Time.timeScale}");
            }
        }

        public float GetTimeScale()
        {
            return Time.timeScale;
        }

        // Metodi per debugging
        public string GetAppInfo()
        {
            string info = $"AppRoot Info:\n";
            info += $"Inizializzato: {_isInitialized}\n";
            info += $"Persistente: {persistBetweenScenes}\n";
            info += $"GameManager: {GetGameManager() != null}\n";
            info += $"Dati globali: {_globalData.Count}\n";
            info += $"TimeScale: {Time.timeScale}\n";
            info += $"Scena attiva: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}";
            
            return info;
        }

        public void LogGlobalData()
        {
            if (_globalData.Count == 0)
            {
                SporiumLogger.LogDebug(LogCategory.Core, "Nessun dato globale presente.");
                return;
            }
            
            SporiumLogger.LogDebug(LogCategory.Core, "Dati globali:");
            foreach (var kvp in _globalData)
            {
                SporiumLogger.LogDebug(LogCategory.Core, $"  {kvp.Key}: {kvp.Value} ({kvp.Value?.GetType()})");
            }
        }

        // Metodi per configurazione runtime
        public void SetPersistBetweenScenes(bool persist)
        {
            persistBetweenScenes = persist;
        }

        public void SetShowDebugLogs(bool show)
        {
            showDebugLogs = show;
        }

        public void SetAutoCreateGameManager(bool autoCreate)
        {
            autoCreateGameManager = autoCreate;
        }

        // Proprietà pubbliche
        public bool IsInitialized => _isInitialized;
        public bool IsPersistent => persistBetweenScenes;
        public int GlobalDataCount => _globalData.Count;

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // Salva automaticamente quando l'app va in pausa (mobile)
                var saveManager = ServiceContainer.Instance?.Get<SaveManager>();
                if (saveManager != null)
                {
                    bool saveSuccess = saveManager.SaveGame("default");
                    if (showDebugLogs)
                    {
                        if (saveSuccess)
                            SporiumLogger.LogInfo(LogCategory.Save, "Salvataggio automatico eseguito (pausa applicazione)");
                        else
                            SporiumLogger.LogWarning(LogCategory.Save, "Errore durante il salvataggio automatico (pausa)");
                    }
                }
            }
            
            if (showDebugLogs)
            {
                SporiumLogger.LogDebug(LogCategory.Core, $"Applicazione {(pauseStatus ? "in pausa" : "ripresa")}.");
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // Salva automaticamente quando l'app perde focus (desktop/mobile)
                var saveManager = ServiceContainer.Instance?.Get<SaveManager>();
                if (saveManager != null)
                {
                    bool saveSuccess = saveManager.SaveGame("default");
                    if (showDebugLogs)
                    {
                        if (saveSuccess)
                            SporiumLogger.LogInfo(LogCategory.Save, "Salvataggio automatico eseguito (perso focus)");
                        else
                            SporiumLogger.LogWarning(LogCategory.Save, "Errore durante il salvataggio automatico (focus)");
                    }
                    if (saveSuccess)
                    {
                        var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                        if (foundation != null && foundation.Enabled)
                            foundation.PostToast("SYS-003", new NotificationPayload());
                    }
                }
            }
            
            if (showDebugLogs)
            {
                SporiumLogger.LogDebug(LogCategory.Core, $"Applicazione {(hasFocus ? "in focus" : "perso focus")}.");
            }
        }
        
        void OnApplicationQuit()
        {
            // Salva automaticamente quando l'app viene chiusa
            var saveManager = ServiceContainer.Instance?.Get<SaveManager>();
            if (saveManager != null)
            {
                bool saveSuccess = saveManager.SaveGame("default");
                if (showDebugLogs)
                {
                    if (saveSuccess)
                        SporiumLogger.LogInfo(LogCategory.Save, "Salvataggio automatico eseguito (chiusura applicazione)");
                    else
                        SporiumLogger.LogWarning(LogCategory.Save, "Errore durante il salvataggio automatico (quit)");
                }
                if (saveSuccess)
                {
                    var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                    if (foundation != null && foundation.Enabled)
                        foundation.PostToast("SYS-003", new NotificationPayload());
                }
            }
        }
    }
}
