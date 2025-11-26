using UnityEngine;

namespace _Project
{
    /// <summary>
    /// Gestisce automaticamente gli AudioListener nella scena
    /// Rimuove duplicati e mantiene solo quello sulla Main Camera principale
    /// </summary>
    [DefaultExecutionOrder(-200)] // Esegue prima di altri script
    public class AudioListenerManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool autoFixOnStart = true;
        [SerializeField] private bool showDebugLogs = true;
        
        private void Awake()
        {
            if (autoFixOnStart)
            {
                FixAudioListeners();
            }
        }
        
        /// <summary>
        /// Rimuove AudioListener duplicati e mantiene solo quello sulla Main Camera
        /// </summary>
        public void FixAudioListeners()
        {
            AudioListener[] allListeners = FindObjectsOfType<AudioListener>();
            
            if (allListeners.Length <= 1)
            {
                if (showDebugLogs && allListeners.Length == 1)
                {
                    Debug.Log($"[AudioListenerManager] ✅ Un solo AudioListener trovato: {allListeners[0].gameObject.name}");
                }
                return;
            }
            
            if (showDebugLogs)
            {
                Debug.LogWarning($"[AudioListenerManager] ⚠️ Trovati {allListeners.Length} AudioListener nella scena. Rimozione duplicati...");
            }
            
            // Trova la Main Camera
            Camera mainCamera = Camera.main;
            AudioListener mainCameraListener = null;
            
            if (mainCamera != null)
            {
                mainCameraListener = mainCamera.GetComponent<AudioListener>();
            }
            
            // Se non c'è Main Camera o non ha AudioListener, usa il primo attivo
            if (mainCameraListener == null)
            {
                foreach (AudioListener listener in allListeners)
                {
                    if (listener.enabled && listener.gameObject.activeInHierarchy)
                    {
                        mainCameraListener = listener;
                        break;
                    }
                }
            }
            
            // Rimuovi tutti gli altri AudioListener
            int removedCount = 0;
            foreach (AudioListener listener in allListeners)
            {
                if (listener != mainCameraListener)
                {
                    if (showDebugLogs)
                    {
                        Debug.Log($"[AudioListenerManager] Rimozione AudioListener da {listener.gameObject.name}");
                    }
                    
                    Destroy(listener);
                    removedCount++;
                }
            }
            
            if (showDebugLogs)
            {
                if (mainCameraListener != null)
                {
                    Debug.Log($"[AudioListenerManager] ✅ AudioListener mantenuto su: {mainCameraListener.gameObject.name}");
                }
                Debug.Log($"[AudioListenerManager] ✅ Rimossi {removedCount} AudioListener duplicati");
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

