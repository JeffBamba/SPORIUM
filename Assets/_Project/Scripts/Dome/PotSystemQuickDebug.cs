using UnityEngine;

/// <summary>
/// Debug rapido per identificare problemi del sistema vasi.
/// Da rimuovere dopo il testing.
/// </summary>
public class PotSystemQuickDebug : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== POT SYSTEM QUICK DEBUG START ===");
        
        // 1. Verifica componenti base
        CheckBasicComponents();
        
        // 2. Verifica vasi
        CheckPots();
        
        // 3. Verifica UI
        CheckUI();
        
        // 4. Verifica configurazione
        CheckConfiguration();
        
        Debug.Log("=== QUICK DEBUG COMPLETED ===");
    }
    
    private void CheckBasicComponents()
    {
        Debug.Log("--- CHECKING BASIC COMPONENTS ---");
        
        // GameManager
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            Debug.Log($"✓ GameManager trovato: CRY={gameManager.CurrentCRY}, Azioni={gameManager.ActionsLeft}");
        }
        else
        {
            Debug.LogError("✗ GameManager NON TROVATO!");
        }
        
        // Player
        GameObject player = GameObject.Find("PLY_Player");
        if (player != null)
        {
            Debug.Log($"✓ Player trovato: {player.name}");
        }
        else
        {
            Debug.LogError("✗ Player NON TROVATO!");
        }
    }
    
    private void CheckPots()
    {
        Debug.Log("--- CHECKING POTS ---");
        
        PotSlot[] allPots = FindObjectsOfType<PotSlot>();
        Debug.Log($"Vasi trovati: {allPots.Length}");
        
        if (allPots.Length == 0)
        {
            Debug.LogError("✗ NESSUN VASO TROVATO!");
            return;
        }
        
        foreach (PotSlot pot in allPots)
        {
            Debug.Log($"Vaso {pot.PotId}:");
            
            // Verifica PotActions
            if (pot.PotActions != null)
            {
                Debug.Log($"  ✓ PotActions presente");
                
                // Verifica stato
                PotStateModel state = pot.PotActions.GetCurrentState();
                if (state != null)
                {
                    Debug.Log($"  ✓ Stato: HasPlant={state.HasPlant}, H={state.Hydration}/3, L={state.LightExposure}/3");
                }
                else
                {
                    Debug.LogError($"  ✗ Stato vaso NULL!");
                }
            }
            else
            {
                Debug.LogError($"  ✗ PotActions MANCANTE!");
            }
            
            // Verifica InRange
            Debug.Log($"  InRange: {pot.InRange}");
        }
    }
    
    private void CheckUI()
    {
        Debug.Log("--- CHECKING UI ---");
        
        // Canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Debug.Log($"Canvas trovati: {canvases.Length}");
        
        foreach (Canvas canvas in canvases)
        {
            Debug.Log($"  Canvas: {canvas.name}, RenderMode: {canvas.renderMode}");
        }
        
        // PotHUDWidget
        PotHUDWidget widget = FindObjectOfType<PotHUDWidget>();
        if (widget != null)
        {
            Debug.Log($"✓ PotHUDWidget trovato: {widget.name}");
            
            // Verifica se è nel Canvas
            Canvas parentCanvas = widget.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                Debug.Log($"  ✓ Widget nel Canvas: {parentCanvas.name}");
            }
            else
            {
                Debug.LogError($"  ✗ Widget NON nel Canvas!");
            }
        }
        else
        {
            Debug.LogError("✗ PotHUDWidget NON TROVATO!");
        }
        
        // HUDController
        HUDController hud = FindObjectOfType<HUDController>();
        if (hud != null)
        {
            Debug.Log($"✓ HUDController trovato: {hud.name}");
        }
        else
        {
            Debug.LogWarning("⚠ HUDController non trovato (potrebbe essere normale)");
        }
    }
    
    private void CheckConfiguration()
    {
        Debug.Log("--- CHECKING CONFIGURATION ---");
        
        // PotSystemConfig
        PotSystemConfig config = FindObjectOfType<PotSystemConfig>();
        if (config != null)
        {
            Debug.Log($"✓ PotSystemConfig trovato: {config.name}");
            Debug.Log($"  Costi: {config.CostActionsPerPotAction} Azioni, {config.CostCryPerPotAction} CRY");
            Debug.Log($"  Distanza: {config.InteractDistance}, MaxH: {config.MaxHydration}, MaxL: {config.MaxLightExposure}");
        }
        else
        {
            Debug.LogError("✗ PotSystemConfig NON TROVATO!");
        }
        
        // PotSystemIntegration
        PotSystemIntegration integration = FindObjectOfType<PotSystemIntegration>();
        if (integration != null)
        {
            Debug.Log($"✓ PotSystemIntegration trovato: {integration.name}");
        }
        else
        {
            Debug.LogWarning("⚠ PotSystemIntegration non trovato");
        }
        
        // PotSystemAutoSetup
        PotSystemAutoSetup autoSetup = FindObjectOfType<PotSystemAutoSetup>();
        if (autoSetup != null)
        {
            Debug.Log($"✓ PotSystemAutoSetup trovato: {autoSetup.name}");
        }
        else
        {
            Debug.LogWarning("⚠ PotSystemAutoSetup non trovato");
        }
    }
    
    void Update()
    {
        // Debug con tasti
        if (Input.GetKeyDown(KeyCode.F1))
        {
            CheckBasicComponents();
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            CheckPots();
        }
        
        if (Input.GetKeyDown(KeyCode.F3))
        {
            CheckUI();
        }
        
        if (Input.GetKeyDown(KeyCode.F4))
        {
            CheckConfiguration();
        }
    }
}
