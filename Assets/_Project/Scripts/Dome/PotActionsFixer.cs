using UnityEngine;

/// <summary>
/// Script di emergenza per aggiungere PotActions ai vasi esistenti.
/// Da rimuovere dopo il fix.
/// </summary>
public class PotActionsFixer : MonoBehaviour
{
    void Start()
    {
        Debug.Log("[PotActionsFixer] Inizio fix vasi...");
        FixExistingPots();
    }
    
    [ContextMenu("Fix PotActions")]
    public void FixExistingPots()
    {
        PotSlot[] allPots = FindObjectsOfType<PotSlot>();
        Debug.Log($"[PotActionsFixer] Trovati {allPots.Length} vasi da fixare");
        
        int fixedCount = 0;
        foreach (PotSlot pot in allPots)
        {
            if (pot.PotActions == null)
            {
                Debug.Log($"[PotActionsFixer] Aggiungendo PotActions a {pot.PotId}");
                
                // Aggiungi PotActions
                PotActions potActions = pot.gameObject.AddComponent<PotActions>();
                
                // Configura con PotSystemConfig se disponibile
                PotSystemConfig config = FindObjectOfType<PotSystemConfig>();
                if (config != null)
                {
                    potActions.SetConfig(config);
                    Debug.Log($"[PotActionsFixer] Configurato {pot.PotId} con PotSystemConfig");
                }
                
                fixedCount++;
            }
            else
            {
                Debug.Log($"[PotActionsFixer] {pot.PotId} ha già PotActions");
            }
        }
        
        Debug.Log($"[PotActionsFixer] Fix completato: {fixedCount} vasi sistemati");
    }
    
    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, "PotActionsFixer");
    }
    #endif
}
