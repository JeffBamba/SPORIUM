using UnityEngine;

/// <summary>
/// Helper di debug per testare la sincronizzazione tra GameManager e HUD
/// </summary>
public class GameManagerDebugHelper : MonoBehaviour
{
    [Header("Debug BLK-01.03A - Sincronizzazione")]
    [SerializeField] private bool enableDebug = true;
    [SerializeField] private KeyCode debugKey = KeyCode.F2;
    [SerializeField] private KeyCode forceUpdateKey = KeyCode.F3;
    
    private GameManager gameManager;
    private HUDController hudController;
    
    void Start()
    {
        Debug.Log("[GameManagerDebugHelper] Start() chiamato - Inizializzazione...");
        
        gameManager = FindObjectOfType<GameManager>();
        hudController = FindObjectOfType<HUDController>();
        
        if (gameManager == null)
        {
            Debug.LogWarning("[GameManagerDebugHelper] GameManager non trovato!");
        }
        else
        {
            Debug.Log($"[GameManagerDebugHelper] GameManager trovato: {gameManager.name}");
        }
        
        if (hudController == null)
        {
            Debug.LogWarning("[GameManagerDebugHelper] HUDController non trovato!");
        }
        else
        {
            Debug.Log($"[GameManagerDebugHelper] HUDController trovato: {hudController.name}");
        }
        
        Debug.Log("[GameManagerDebugHelper] Inizializzazione completata. Premi F2 per debug, F3 per sync.");
    }
    
    void Update()
    {
        if (!enableDebug) return;
        
        // Test continuo per verificare che i tasti siano rilevati
        if (Input.GetKey(debugKey))
        {
            Debug.Log("[GameManagerDebugHelper] TASTO F2 TENUTO!");
        }
        
        if (Input.GetKey(forceUpdateKey))
        {
            Debug.Log("[GameManagerDebugHelper] TASTO F3 TENUTO!");
        }
        
        if (Input.GetKeyDown(debugKey))
        {
            Debug.Log("[GameManagerDebugHelper] TASTO F2 PREMUTO!");
            ShowDebugInfo();
        }
        
        if (Input.GetKeyDown(forceUpdateKey))
        {
            Debug.Log("[GameManagerDebugHelper] TASTO F3 PREMUTO!");
            ForceSynchronization();
        }
    }
    
    private void ShowDebugInfo()
    {
        Debug.Log("=== GAMEMANAGER DEBUG HELPER ===");
        
        if (gameManager != null)
        {
            Debug.Log($"GameManager - Starting CRY: {gameManager.startingCRY}, Current CRY: {gameManager.CurrentCRY}");
            Debug.Log($"GameManager - Starting Actions: {gameManager.actionsPerDay}, Current Actions: {gameManager.ActionsLeft}");
            Debug.Log($"GameManager - Current Day: {gameManager.CurrentDay}");
        }
        else
        {
            Debug.Log("GameManager: NULL");
        }
        
        if (hudController != null)
        {
            Debug.Log("HUDController: Trovato");
        }
        else
        {
            Debug.Log("HUDController: NULL");
        }
        
        Debug.Log("================================");
    }
    
    private void ForceSynchronization()
    {
        Debug.Log("=== FORZATURA SINCRONIZZAZIONE ===");
        
        if (gameManager != null)
        {
            gameManager.ForceUIUpdate();
            Debug.Log("GameManager.ForceUIUpdate() chiamato");
        }
        
        if (hudController != null)
        {
            hudController.ForceUpdateAllUI();
            Debug.Log("HUDController.ForceUpdateAllUI() chiamato");
        }
        
        Debug.Log("==================================");
    }
    
    [ContextMenu("Debug Status")]
    private void DebugStatusContextMenu()
    {
        ShowDebugInfo();
    }
    
    [ContextMenu("Force Sync")]
    private void ForceSyncContextMenu()
    {
        ForceSynchronization();
    }
}
