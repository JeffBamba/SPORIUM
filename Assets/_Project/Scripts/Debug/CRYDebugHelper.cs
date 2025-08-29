using UnityEngine;

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
            Debug.LogWarning("[CRYDebugHelper] GameManager non trovato!");
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
        if (gameManager == null) return;
        
        Debug.Log($"[CRYDebugHelper] === STATO RISORSE ===");
        Debug.Log($"[CRYDebugHelper] Giorno: {gameManager.CurrentDay}");
        Debug.Log($"[CRYDebugHelper] Azioni rimanenti: {gameManager.ActionsLeft}");
        Debug.Log($"[CRYDebugHelper] CRY disponibili: {gameManager.CurrentCRY}");
        Debug.Log($"[CRYDebugHelper] End Day possibile: {gameManager.CurrentCRY >= 20}");
        Debug.Log($"[CRYDebugHelper] =========================");
    }
    
    [ContextMenu("Mostra Info Debug")]
    private void ShowDebugInfoContextMenu()
    {
        ShowDebugInfo();
    }
}
