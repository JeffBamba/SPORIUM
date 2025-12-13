using _Project.Sporae.Core;
using UnityEngine;
using Sporae.DevTools;

/// <summary>
/// Helper di debug per testare la sincronizzazione tra GameManager e HUD
/// </summary>
public class GameManagerDebugHelper : MonoBehaviour
{
    [Header("Debug BLK-01.03A - Sincronizzazione")]
    [SerializeField] private bool enableDebug = true;
    [SerializeField] private KeyCode debugKey = KeyCode.F2;
    [SerializeField] private KeyCode forceUpdateKey = KeyCode.F3;
    
    private GameManager _gameManager;
    private HUDController _hudController;
    private DayCycleSystem _dayCycleSystem;
    
    private void Start()
    {
        SporiumLogger.LogDebug(LogCategory.Core, "Start() chiamato - Inizializzazione...");
        
        _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
        _gameManager = FindObjectOfType<GameManager>();
        _hudController = FindObjectOfType<HUDController>();
        
        if (_gameManager == null)
        {
            SporiumLogger.LogWarning(LogCategory.Core, "GameManager non trovato!");
        }
        else
        {
            SporiumLogger.LogInfo(LogCategory.Core, $"GameManager trovato: {_gameManager.name}");
        }
        
        if (_hudController == null)
        {
            SporiumLogger.LogWarning(LogCategory.UI, "HUDController non trovato!");
        }
        else
        {
            SporiumLogger.LogInfo(LogCategory.UI, $"HUDController trovato: {_hudController.name}");
        }
        
        SporiumLogger.LogInfo(LogCategory.Core, "Inizializzazione completata. Premi F2 per debug, F3 per sync.");
    }
    
    private void Update()
    {
        if (!enableDebug) return;
        
        // Test continuo per verificare che i tasti siano rilevati
        if (Input.GetKey(debugKey))
        {
            SporiumLogger.LogDebug(LogCategory.Core, "TASTO F2 TENUTO!");
        }
        
        if (Input.GetKey(forceUpdateKey))
        {
            SporiumLogger.LogDebug(LogCategory.Core, "TASTO F3 TENUTO!");
        }
        
        if (Input.GetKeyDown(debugKey))
        {
            SporiumLogger.LogDebug(LogCategory.Core, "TASTO F2 PREMUTO!");
            ShowDebugInfo();
        }
        
        if (Input.GetKeyDown(forceUpdateKey))
        {
            SporiumLogger.LogDebug(LogCategory.Core, "TASTO F3 PREMUTO!");
            ForceSynchronization();
        }
    }
    
    private void ShowDebugInfo()
    {
        SporiumLogger.LogDebug(LogCategory.Core, "=== GAMEMANAGER DEBUG HELPER ===");
        
        if (_gameManager)
        {
            SporiumLogger.LogDebug(LogCategory.Core, $"GameManager - Current CRY: {_gameManager.CurrentCRY}");
            SporiumLogger.LogDebug(LogCategory.Core, $"GameManager - Current Actions: {_gameManager.ActionsLeft}");
            SporiumLogger.LogDebug(LogCategory.Core, $"GameManager - Current Day: {_dayCycleSystem.CurrentDay}");
        }
        else
        {
            SporiumLogger.LogWarning(LogCategory.Core, "GameManager: NULL");
        }

        SporiumLogger.LogDebug(LogCategory.UI, _hudController ? "HUDController: Trovato" : "HUDController: NULL");

        SporiumLogger.LogDebug(LogCategory.Core, "================================");
    }
    
    private void ForceSynchronization()
    {
        SporiumLogger.LogDebug(LogCategory.UI, "=== FORZATURA SINCRONIZZAZIONE ===");
        
        if (_hudController != null)
        {
            _hudController.ForceUpdateAllUI();
            SporiumLogger.LogInfo(LogCategory.UI, "HUDController.ForceUpdateAllUI() chiamato");
        }
        
        SporiumLogger.LogDebug(LogCategory.UI, "==================================");
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
