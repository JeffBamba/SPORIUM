using UnityEngine;
using Sporae.DevTools;

namespace _Project
{
    /// <summary>
    /// Gestisce automaticamente gli AudioListener nella scena
    /// Disabilita duplicati e mantiene solo quello sulla Main Camera principale
    /// Previene il warning Unity "There are 2 audio listeners in the scene"
    /// </summary>
    [DefaultExecutionOrder(-10000)] // Esegue per primo per correggere i listener prima che Unity logghi il warning
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
        /// Controllo rapido che rimuove duplicati senza log.
        /// Disabilita subito i duplicati per fermare il warning Unity nello stesso frame (Destroy è a fine frame).
        /// </summary>
        private void EnsureSingleAudioListener()
        {
            AudioListener[] allListeners = FindObjectsOfType<AudioListener>(true);
            
            int enabledCount = 0;
            AudioListener firstEnabled = null;
            
            foreach (AudioListener listener in allListeners)
            {
                if (listener != null && listener.enabled && listener.gameObject.activeInHierarchy)
                {
                    enabledCount++;
                    if (firstEnabled == null)
                        firstEnabled = listener;
                }
            }
            
            if (enabledCount <= 1)
                return;
            
            Camera mainCamera = Camera.main;
            AudioListener mainCameraListener = mainCamera != null ? mainCamera.GetComponent<AudioListener>() : null;
            AudioListener listenerToKeep = (mainCameraListener != null && mainCameraListener.enabled) ? mainCameraListener : firstEnabled;
            
            if (listenerToKeep == null)
                return;
            
            // Disabilita subito i duplicati così Unity non logga il warning ogni frame (Destroy avviene a fine frame)
            foreach (AudioListener listener in allListeners)
            {
                if (listener != null && listener != listenerToKeep && listener.enabled)
                {
                    listener.enabled = false;
                    if (Application.isPlaying)
                        Destroy(listener);
                    else
                        DestroyImmediate(listener);
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
            
            Camera mainCamera = Camera.main;
            AudioListener mainCameraListener = mainCamera != null ? mainCamera.GetComponent<AudioListener>() : null;
            
            if (mainCameraListener != null && mainCameraListener.enabled)
                _mainListener = mainCameraListener;
            else
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
            
            int removedCount = 0;
            foreach (AudioListener listener in allListeners)
            {
                if (listener != null && listener != _mainListener)
                {
                    // Disabilita subito per fermare il warning Unity nello stesso frame
                    if (listener.enabled)
                    {
                        listener.enabled = false;
                        removedCount++;
                    }
                    if (Application.isPlaying)
                        Destroy(listener);
                    else
                        DestroyImmediate(listener);
                    if (showDebugLogs)
                        SporiumLogger.LogDebug(LogCategory.Audio, $"AudioListener rimosso da {listener.gameObject.name}");
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

