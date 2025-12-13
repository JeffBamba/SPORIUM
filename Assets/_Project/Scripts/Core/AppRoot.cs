using UnityEngine;
using System.Collections.Generic;
using _Project.Sporae.Core;

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
                        Debug.LogWarning("[AppRoot] Nessuna istanza di AppRoot trovata nella scena!");
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[AppRoot] Duplicato di AppRoot trovato! Distruggo il duplicato.");
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
                Debug.LogWarning("[AppRoot] gameManagerPrefab non assegnato ma autoCreateGameManager è attivo!");
            }
        }

        private void InitializeAppRoot()
        {
            if (_isInitialized) return;
            
            // Verifica se c'è già un GameManager in scena
            GameManager existingGameManager = FindObjectOfType<GameManager>();
            if (existingGameManager != null)
            {
                _cachedGameManager = existingGameManager; // BUG FIX: Cache il GameManager trovato
                if (showDebugLogs)
                {
                    Debug.Log($"[AppRoot] GameManager già presente nella scena: {existingGameManager.name}");
                }
                // Non creare un nuovo GameManager se ne esiste già uno
                _isInitialized = true;
                return;
            }
            
            _isInitialized = true;
            
            if (showDebugLogs)
            {
                Debug.Log("[AppRoot] AppRoot inizializzato correttamente.");
            }
        }
        
        // Metodi per gestire dati globali
        public void SetGlobalData(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[AppRoot] Chiave non può essere null o vuota!");
                return;
            }
            
            _globalData[key] = value;
        }

        public T GetGlobalData<T>(string key, T defaultValue = default(T))
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[AppRoot] Chiave non può essere null o vuota!");
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
                    Debug.LogWarning($"[AppRoot] Tipo non corrispondente per chiave '{key}'. " +
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
            // BUG FIX: Cache per evitare FindObjectOfType ripetuti
            if (_cachedGameManager == null)
            {
                _cachedGameManager = FindObjectOfType<GameManager>();
            }
            return _cachedGameManager;
        }

        public T GetSystem<T>() where T : Component
        {
            return FindObjectOfType<T>();
        }

        public T[] GetAllSystems<T>() where T : Component
        {
            return FindObjectsOfType<T>();
        }

        // Metodi di utilità
        public void QuitApplication()
        {
            if (showDebugLogs)
            {
                Debug.Log("[AppRoot] Uscita applicazione richiesta.");
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
                Debug.Log("[AppRoot] Riavvio applicazione richiesto.");
            }
            
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }

        public void SetTimeScale(float timeScale)
        {
            Time.timeScale = Mathf.Clamp(timeScale, 0f, 10f);
            
            if (showDebugLogs)
            {
                Debug.Log($"[AppRoot] TimeScale impostato a: {Time.timeScale}");
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
                Debug.Log("[AppRoot] Nessun dato globale presente.");
                return;
            }
            
            Debug.Log("[AppRoot] Dati globali:");
            foreach (var kvp in _globalData)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value} ({kvp.Value?.GetType()})");
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
                            Debug.Log("[AppRoot] ✅ Salvataggio automatico eseguito (pausa applicazione)");
                        else
                            Debug.LogWarning("[AppRoot] ⚠️ Errore durante il salvataggio automatico (pausa)");
                    }
                }
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"[AppRoot] Applicazione {(pauseStatus ? "in pausa" : "ripresa")}.");
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
                            Debug.Log("[AppRoot] ✅ Salvataggio automatico eseguito (perso focus)");
                        else
                            Debug.LogWarning("[AppRoot] ⚠️ Errore durante il salvataggio automatico (focus)");
                    }
                }
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"[AppRoot] Applicazione {(hasFocus ? "in focus" : "perso focus")}.");
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
                        Debug.Log("[AppRoot] ✅ Salvataggio automatico eseguito (chiusura applicazione)");
                    else
                        Debug.LogWarning("[AppRoot] ⚠️ Errore durante il salvataggio automatico (quit)");
                }
            }
        }
    }
}
