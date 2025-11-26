using UnityEngine;
using _Project;
using Sporae.DevTools;

namespace _Project
{
    /// <summary>
    /// Script che crea automaticamente il setup del sistema pH al Play Mode
    /// Crea PhSystemDebugConsole e HUDPhDisplay se mancanti
    /// </summary>
    public class PhSystemAutoSetup : MonoBehaviour
    {
        [Header("Auto Setup Settings")]
        [SerializeField] private bool createDebugConsole = true;
        [SerializeField] private bool createHUDDisplay = true;
        [SerializeField] private bool createIdleOscillation = true;
        [SerializeField] private bool showDebugLogs = true;
        
        [Header("Debug Console Settings")]
        [SerializeField] private bool enableDebugConsole = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.Z;
        [SerializeField] private bool showConsoleOnStart = false;
        
        [Header("HUD Display Settings")]
        [SerializeField] private Vector2 hudPosition = new Vector2(0f, -30f);
        
        private void Awake()
        {
            SetupPhSystem();
        }
        
        private void SetupPhSystem()
        {
            if (showDebugLogs)
            {
                Debug.Log("[PhSystemAutoSetup] Inizializzazione setup sistema pH...");
            }
            
            // Crea PhSystemDebugConsole se richiesto e mancante
            if (createDebugConsole)
            {
                SetupDebugConsole();
            }
            
            // Crea HUDPhDisplay se richiesto e mancante
            if (createHUDDisplay)
            {
                SetupHUDDisplay();
            }
            
            // Crea PhSystemIdleOscillation se richiesto e mancante
            if (createIdleOscillation)
            {
                SetupIdleOscillation();
            }
            
            if (showDebugLogs)
            {
                Debug.Log("[PhSystemAutoSetup] Setup sistema pH completato!");
            }
        }
        
        private void SetupDebugConsole()
        {
            // Cerca se esiste già
            PhSystemDebugConsole existingConsole = FindObjectOfType<PhSystemDebugConsole>();
            
            if (existingConsole != null)
            {
                if (showDebugLogs)
                {
                    Debug.Log("[PhSystemAutoSetup] PhSystemDebugConsole già presente nella scena");
                }
                return;
            }
            
            // Crea GameObject per la console
            GameObject consoleGO = new GameObject("pH_DebugConsole");
            PhSystemDebugConsole console = consoleGO.AddComponent<PhSystemDebugConsole>();
            
            // Configura la console con i valori desiderati
            console.Configure(enableDebugConsole, toggleKey, showConsoleOnStart);
            
            if (showDebugLogs)
            {
                Debug.Log($"[PhSystemAutoSetup] Creato PhSystemDebugConsole su {consoleGO.name}");
                Debug.Log($"[PhSystemAutoSetup] Tasto toggle: {toggleKey}, Abilitato: {enableDebugConsole}");
            }
        }
        
        private void SetupHUDDisplay()
        {
            // Cerca se esiste già
            HUDPhDisplay existingHUD = FindObjectOfType<HUDPhDisplay>();
            
            if (existingHUD != null)
            {
                if (showDebugLogs)
                {
                    Debug.Log("[PhSystemAutoSetup] HUDPhDisplay già presente nella scena");
                }
                return;
            }
            
            // Crea GameObject per l'HUD
            GameObject hudGO = new GameObject("pH_HUDDisplay");
            HUDPhDisplay hud = hudGO.AddComponent<HUDPhDisplay>();
            
            if (showDebugLogs)
            {
                Debug.Log($"[PhSystemAutoSetup] Creato HUDPhDisplay su {hudGO.name}");
            }
            
            // L'HUD creerà automaticamente gli elementi UI se autoCreateUI è true
        }
        
        private void SetupIdleOscillation()
        {
            // Cerca se esiste già
            PhSystemIdleOscillation existingOscillation = FindObjectOfType<PhSystemIdleOscillation>();
            
            if (existingOscillation != null)
            {
                if (showDebugLogs)
                {
                    Debug.Log("[PhSystemAutoSetup] PhSystemIdleOscillation già presente nella scena");
                }
                return;
            }
            
            // Crea GameObject per l'oscillazione
            GameObject oscillationGO = new GameObject("pH_IdleOscillation");
            PhSystemIdleOscillation oscillation = oscillationGO.AddComponent<PhSystemIdleOscillation>();
            
            if (showDebugLogs)
            {
                Debug.Log($"[PhSystemAutoSetup] Creato PhSystemIdleOscillation su {oscillationGO.name}");
            }
        }
        
        /// <summary>
        /// Metodo pubblico per ricreare il setup manualmente
        /// </summary>
        [ContextMenu("Recreate Ph System Setup")]
        public void RecreateSetup()
        {
            // Distruggi componenti esistenti
            PhSystemDebugConsole[] existingConsoles = FindObjectsOfType<PhSystemDebugConsole>();
            foreach (var console in existingConsoles)
            {
                if (Application.isPlaying)
                    Destroy(console.gameObject);
                else
                    DestroyImmediate(console.gameObject);
            }
            
            HUDPhDisplay[] existingHUDs = FindObjectsOfType<HUDPhDisplay>();
            foreach (var hud in existingHUDs)
            {
                if (Application.isPlaying)
                    Destroy(hud.gameObject);
                else
                    DestroyImmediate(hud.gameObject);
            }
            
            PhSystemIdleOscillation[] existingOscillations = FindObjectsOfType<PhSystemIdleOscillation>();
            foreach (var oscillation in existingOscillations)
            {
                if (Application.isPlaying)
                    Destroy(oscillation.gameObject);
                else
                    DestroyImmediate(oscillation.gameObject);
            }
            
            // Ricrea
            SetupPhSystem();
        }
    }
}

