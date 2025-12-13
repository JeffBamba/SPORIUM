using UnityEngine;
using Sporae.DevTools;

/// <summary>
/// Helper di debug per verificare lo stato delle risorse durante il test BLK-01.03A
/// </summary>
public class CRYDebugHelper : MonoBehaviour
{
    [Header("Debug BLK-01.03A")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private KeyCode debugKey = KeyCode.F1;
    
    private GameManager gameManager;
    
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            SporiumLogger.LogWarning(LogCategory.Core, "GameManager non trovato!");
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(debugKey) && showDebugInfo)
        {
            ShowDebugInfo();
        }
    }
    
    private void ShowDebugInfo()
    {
        if (!gameManager)
            return;
        
        SporiumLogger.LogDebug(LogCategory.Core, "=== STATO RISORSE ===");
        SporiumLogger.LogDebug(LogCategory.Core, $"Azioni rimanenti: {gameManager.ActionsLeft}");
        SporiumLogger.LogDebug(LogCategory.Core, $"CRY disponibili: {gameManager.CurrentCRY}");
        SporiumLogger.LogDebug(LogCategory.Core, $"End Day possibile: {gameManager.CurrentCRY >= 20}");
        SporiumLogger.LogDebug(LogCategory.Core, "========================");
    }
    
    [ContextMenu("Mostra Info Debug")]
    private void ShowDebugInfoContextMenu()
    {
        ShowDebugInfo();
    }
}
