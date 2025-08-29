using UnityEngine;

/// <summary>
/// Debug temporaneo per il sistema dei vasi.
/// Da rimuovere dopo il testing.
/// </summary>
public class PotSystemDebugger : MonoBehaviour
{
    void Update()
    {
        // Debug con tasti
        if (Input.GetKeyDown(KeyCode.F1))
        {
            DebugSystemStatus();
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            TestPotSelection();
        }
        
        if (Input.GetKeyDown(KeyCode.F3))
        {
            TestPotActions();
        }
    }
    
    private void DebugSystemStatus()
    {
        Debug.Log("=== POT SYSTEM DEBUG ===");
        
        // Verifica componenti
        PotSlot[] pots = FindObjectsOfType<PotSlot>();
        Debug.Log($"Vasi trovati: {pots.Length}");
        
        foreach (PotSlot pot in pots)
        {
            Debug.Log($"Vaso {pot.PotId}: PotActions={pot.PotActions != null}, InRange={pot.InRange}");
        }
        
        // Verifica UI
        PotHUDWidget widget = FindObjectOfType<PotHUDWidget>();
        Debug.Log($"PotHUDWidget: {widget != null}");
        
        // Verifica configurazione
        PotSystemConfig config = FindObjectOfType<PotSystemConfig>();
        Debug.Log($"PotSystemConfig: {config != null}");
        
        // Verifica integrazione
        PotSystemIntegration integration = FindObjectOfType<PotSystemIntegration>();
        Debug.Log($"PotSystemIntegration: {integration != null}");
        
        Debug.Log("========================");
    }
    
    private void TestPotSelection()
    {
        Debug.Log("=== TESTING POT SELECTION ===");
        
        PotSlot[] pots = FindObjectsOfType<PotSlot>();
        if (pots.Length > 0)
        {
            PotSlot testPot = pots[0];
            Debug.Log($"Testando selezione vaso {testPot.PotId}");
            
            // Simula selezione
            if (testPot != null)
            {
                testPot.SelectPot();
            }
        }
        
        Debug.Log("============================");
    }
    
    private void TestPotActions()
    {
        Debug.Log("=== TESTING POT ACTIONS ===");
        
        PotSlot[] pots = FindObjectsOfType<PotSlot>();
        if (pots.Length > 0)
        {
            PotSlot testPot = pots[0];
            if (testPot.PotActions != null)
            {
                Debug.Log($"Testando azioni vaso {testPot.PotId}:");
                Debug.Log($"  CanPlant: {testPot.PotActions.CanPlant()}");
                Debug.Log($"  CanWater: {testPot.PotActions.CanWater()}");
                Debug.Log($"  CanLight: {testPot.PotActions.CanLight()}");
            }
        }
        
        Debug.Log("============================");
    }
}
