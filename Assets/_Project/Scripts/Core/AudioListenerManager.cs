using UnityEngine;
using Sporae.DevTools;

namespace _Project
{
    /// <summary>
    /// Gestisce automaticamente gli AudioListener nella scena
    /// Disabilita duplicati e mantiene solo quello sulla Main Camera principale
    /// Previene il warning Unity "There are 2 audio listeners in the scene"
    /// </summary>
    [DefaultExecutionOrder(-200)] // Esegue prima di altri script
    public class AudioListenerManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool autoFixOnStart = true;
        [SerializeField] private bool continuousCheck = true; // Controlla continuamente durante il runtime
        [SerializeField] private float checkInterval = 1f; // Controlla ogni secondo
        [SerializeField] private bool showDebugLogs = false; // Disabilitato per default per evitare spam
        
        private float _lastCheckTime;
        private AudioListener _mainListener;
        
        private void Awake()
        {
            if (autoFixOnStart)
            {
                FixAudioListeners();
            }
        }
        
        private void Start()
        {
            // Assicurati che il fix venga applicato anche dopo Start
            FixAudioListeners();
        }
        
        private void Update()
        {
            // Controllo periodico durante il runtime per prevenire warning
            if (continuousCheck && Time.time - _lastCheckTime >= checkInterval)
            {
                _lastCheckTime = Time.time;
                EnsureSingleAudioListener();
            }
        }
        
        /// <summary>
        /// Controllo rapido che rimuove duplicati senza log
        /// </summary>
        private void EnsureSingleAudioListener()
        {
            AudioListener[] allListeners = FindObjectsOfType<AudioListener>(true); // Include anche quelli disabilitati
            
            int enabledCount = 0;
            AudioListener firstEnabled = null;
            
            foreach (AudioListener listener in allListeners)
            {
                if (listener != null && listener.enabled && listener.gameObject.activeInHierarchy)
                {
                    enabledCount++;
                    if (firstEnabled == null)
                    {
                        firstEnabled = listener;
                    }
                }
            }
            
            // Se ci sono più di un AudioListener abilitato, rimuovi i duplicati
            if (enabledCount > 1 && firstEnabled != null)
            {
                // Trova la Main Camera
                Camera mainCamera = Camera.main;
                AudioListener mainCameraListener = null;
                
                if (mainCamera != null)
                {
                    mainCameraListener = mainCamera.GetComponent<AudioListener>();
                }
                
                AudioListener listenerToKeep = mainCameraListener != null && mainCameraListener.enabled ? mainCameraListener : firstEnabled;
                
                // Rimuovi tutti gli altri
                foreach (AudioListener listener in allListeners)
                {
                    if (listener != null && listener != listenerToKeep && listener.enabled)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(listener);
                        }
                        else
                        {
                            DestroyImmediate(listener);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Disabilita AudioListener duplicati e mantiene solo quello sulla Main Camera
        /// </summary>
        public void FixAudioListeners()
        {
            AudioListener[] allListeners = FindObjectsOfType<AudioListener>(true);
            
            // Conta solo quelli abilitati
            int enabledCount = 0;
            foreach (AudioListener listener in allListeners)
            {
                if (listener.enabled && listener.gameObject.activeInHierarchy)
                {
                    enabledCount++;
                }
            }
            
            if (enabledCount <= 1)
            {
                // Trova e memorizza il listener principale
                foreach (AudioListener listener in allListeners)
                {
                    if (listener.enabled && listener.gameObject.activeInHierarchy)
                    {
                        _mainListener = listener;
                        break;
                    }
                }
                return;
            }
            
            // Trova la Main Camera
            Camera mainCamera = Camera.main;
            AudioListener mainCameraListener = null;
            
            if (mainCamera != null)
            {
                mainCameraListener = mainCamera.GetComponent<AudioListener>();
                if (mainCameraListener != null && mainCameraListener.enabled)
                {
                    _mainListener = mainCameraListener;
                }
            }
            
            // Se non c'è Main Camera o non ha AudioListener, usa il primo attivo
            if (_mainListener == null)
            {
                foreach (AudioListener listener in allListeners)
                {
                    if (listener.enabled && listener.gameObject.activeInHierarchy)
                    {
                        _mainListener = listener;
                        break;
                    }
                }
            }
            
            // Rimuovi fisicamente tutti gli altri AudioListener invece di solo disabilitarli
            int removedCount = 0;
            foreach (AudioListener listener in allListeners)
            {
                if (listener != _mainListener)
                {
                    // Rimuovi fisicamente il componente invece di solo disabilitarlo
                    if (Application.isPlaying)
                    {
                        Destroy(listener);
                    }
                    else
                    {
                        DestroyImmediate(listener);
                    }
                    removedCount++;
                    
                    if (showDebugLogs)
                    {
                        SporiumLogger.LogDebug(LogCategory.Audio, $"AudioListener rimosso da {listener.gameObject.name}");
                    }
                }
            }
            
            if (showDebugLogs && removedCount > 0)
            {
                if (_mainListener != null)
                {
                    SporiumLogger.LogInfo(LogCategory.Audio, $"✅ AudioListener mantenuto su: {_mainListener.gameObject.name}");
                }
                SporiumLogger.LogInfo(LogCategory.Audio, $"✅ Rimossi {removedCount} AudioListener duplicati");
            }
        }
        
        /// <summary>
        /// Metodo pubblico per ricontrollare manualmente
        /// </summary>
        [ContextMenu("Fix Audio Listeners")]
        public void FixAudioListenersManual()
        {
            FixAudioListeners();
        }
    }
}

