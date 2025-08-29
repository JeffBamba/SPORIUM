using UnityEngine;

/// <summary>
/// Setup automatico per il sistema dei vasi.
/// Da attaccare a un GameObject nella scena per configurare automaticamente
/// tutti i componenti necessari per BLK-01.02.
/// </summary>
public class PotSystemAutoSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool runOnStart = true;
    [SerializeField] private bool createMissingComponents = true;
    [SerializeField] private bool showDebugLogs = true;
    
    [Header("Configuration")]
    [SerializeField] private PotSystemConfig potSystemConfig;
    
    void Start()
    {
        if (runOnStart)
        {
            SetupPotSystem();
        }
    }
    
    [ContextMenu("Setup Pot System")]
    public void SetupPotSystem()
    {
        if (showDebugLogs)
        {
            Debug.Log("[PotSystemAutoSetup] Inizializzazione sistema vasi...");
        }
        
        // 1. Crea o trova PotSystemConfig
        SetupPotSystemConfig();
        
        // 2. Crea o trova PotHUDWidget
        SetupPotHUDWidget();
        
        // 3. Crea o trova PotSystemIntegration
        SetupPotSystemIntegration();
        
        // 4. Configura i vasi esistenti
        ConfigureExistingPots();
        
        if (showDebugLogs)
        {
            Debug.Log("[PotSystemAutoSetup] Setup completato!");
        }
    }
    
    private void SetupPotSystemConfig()
    {
        if (potSystemConfig == null)
        {
            // Cerca configurazione esistente
            potSystemConfig = FindObjectOfType<PotSystemConfig>();
            
            if (potSystemConfig == null && createMissingComponents)
            {
                // Crea configurazione predefinita
                potSystemConfig = PotSystemConfig.CreateDefaultConfig();
                Debug.Log("[PotSystemAutoSetup] Creata configurazione predefinita");
            }
        }
        
        if (potSystemConfig != null)
        {
            Debug.Log($"[PotSystemAutoSetup] Configurazione trovata: {potSystemConfig.name}");
        }
        else
        {
            Debug.LogError("[PotSystemAutoSetup] Impossibile trovare o creare PotSystemConfig!");
        }
    }
    
    private void SetupPotHUDWidget()
    {
        // Cerca PotHUDWidget esistente
        PotHUDWidget existingWidget = FindObjectOfType<PotHUDWidget>();
        
        if (existingWidget == null && createMissingComponents)
        {
            // Cerca Canvas principale
            Canvas mainCanvas = FindMainCanvas();
            
            if (mainCanvas != null)
            {
                // Crea PotHUDWidget
                GameObject widgetGO = new GameObject("PotHUDWidget");
                widgetGO.transform.SetParent(mainCanvas.transform, false);
                
                PotHUDWidget newWidget = widgetGO.AddComponent<PotHUDWidget>();
                
                Debug.Log("[PotSystemAutoSetup] Creato PotHUDWidget nel Canvas principale");
            }
            else
            {
                Debug.LogError("[PotSystemAutoSetup] Impossibile trovare Canvas principale!");
            }
        }
        else if (existingWidget != null)
        {
            Debug.Log("[PotSystemAutoSetup] PotHUDWidget già presente");
        }
    }
    
    private void SetupPotSystemIntegration()
    {
        // Cerca PotSystemIntegration esistente
        PotSystemIntegration existingIntegration = FindObjectOfType<PotSystemIntegration>();
        
        if (existingIntegration == null && createMissingComponents)
        {
            // Crea PotSystemIntegration
            GameObject integrationGO = new GameObject("PotSystemIntegration");
            PotSystemIntegration newIntegration = integrationGO.AddComponent<PotSystemIntegration>();
            
            // Configura riferimenti
            if (potSystemConfig != null)
            {
                // Usa reflection per impostare il campo privato
                var configField = typeof(PotSystemIntegration).GetField("potSystemConfig", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (configField != null)
                {
                    configField.SetValue(newIntegration, potSystemConfig);
                }
            }
            
            Debug.Log("[PotSystemAutoSetup] Creato PotSystemIntegration");
        }
        else if (existingIntegration != null)
        {
            Debug.Log("[PotSystemAutoSetup] PotSystemIntegration già presente");
        }
    }
    
    private void ConfigureExistingPots()
    {
        // Trova tutti i vasi esistenti
        PotSlot[] allPots = FindObjectsOfType<PotSlot>();
        
        if (allPots.Length == 0)
        {
            Debug.LogWarning("[PotSystemAutoSetup] Nessun vaso trovato nella scena!");
            return;
        }
        
        Debug.Log($"[PotSystemAutoSetup] Configurando {allPots.Length} vasi...");
        
        foreach (PotSlot pot in allPots)
        {
            // Verifica che abbia PotActions
            if (pot.PotActions == null)
            {
                Debug.LogWarning($"[PotSystemAutoSetup] Vaso {pot.PotId} manca di PotActions!");
                continue;
            }
            
            // Configura con PotSystemConfig
            if (potSystemConfig != null)
            {
                pot.PotActions.SetConfig(potSystemConfig);
                Debug.Log($"[PotSystemAutoSetup] Configurato vaso {pot.PotId}");
            }
        }
    }
    
    private Canvas FindMainCanvas()
    {
        // Cerca prima nell'HUD esistente
        HUDController hudController = FindObjectOfType<HUDController>();
        if (hudController != null)
        {
            Canvas hudCanvas = hudController.GetComponentInParent<Canvas>();
            if (hudCanvas != null)
            {
                return hudCanvas;
            }
        }
        
        // Cerca qualsiasi Canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        if (canvases.Length > 0)
        {
            // Preferisci il primo Canvas trovato
            return canvases[0];
        }
        
        return null;
    }
    
    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showDebugLogs) return;
        
        // Disegna indicatori per il setup
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        
        // Label
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, "PotSystemAutoSetup");
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"Config: {(potSystemConfig != null ? "OK" : "MISSING")}");
    }
    #endif
}
