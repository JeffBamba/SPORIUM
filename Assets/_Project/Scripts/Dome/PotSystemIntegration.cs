using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Integrazione del sistema dei vasi con il GameManager esistente.
/// Bridge tra HUD, PotSlot selezionato e PotActions.
/// Gestisce la selezione dei vasi e aggiorna l'UI.
/// </summary>
public class PotSystemIntegration : MonoBehaviour
{
    [Header("Integration Settings")]
    [SerializeField] private bool enableIntegration = true;
    [SerializeField] private PotSystemConfig potSystemConfig;
    
    [Header("References")]
    [SerializeField] private RoomDomePotsBootstrap potsBootstrap;
    [SerializeField] private PotHUDWidget potHUDWidget;
    
    private GameManager gameManager;
    private List<PotSlot> allPots;
    
    void Start()
    {
        if (!enableIntegration) return;
        
        // Trova il GameManager
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogWarning("[PotSystemIntegration] GameManager non trovato. Integrazione disabilitata.");
            enabled = false;
            return;
        }
        
        // Trova il bootstrap dei vasi
        if (potsBootstrap == null)
        {
            potsBootstrap = FindObjectOfType<RoomDomePotsBootstrap>();
        }
        
        if (potsBootstrap != null)
        {
            // Sottoscrivi agli eventi dei vasi
            PotSlot.OnPotSelected += OnPotSelected;
            
            // Ottieni tutti i vasi
            allPots = new List<PotSlot>(potsBootstrap.GetAllPots());
            
            Debug.Log($"[PotSystemIntegration] Integrazione attivata con {allPots.Count} vasi.");
        }
        else
        {
            Debug.LogWarning("[PotSystemIntegration] Bootstrap vasi non trovato. Integrazione disabilitata.");
            enabled = false;
        }
    }
    
    void OnDestroy()
    {
        // Annulla sottoscrizione
        PotSlot.OnPotSelected -= OnPotSelected;
    }
    
    private void OnPotSelected(PotSlot pot)
    {
        if (!enableIntegration) return;
        
        Debug.Log($"[PotSystemIntegration] Vaso {pot.PotId} selezionato per azioni.");
        
        // Mostra opzioni disponibili (da implementare in BLK-01.02)
        ShowAvailableActions(pot);
    }
    
    /// <summary>
    /// Mostra le azioni disponibili per il vaso selezionato
    /// DA IMPLEMENTARE IN BLK-01.02
    /// </summary>
    private void ShowAvailableActions(PotSlot pot)
    {
        if (pot == null || pot.PotActions == null) return;
        
        Debug.Log($"[PotSystemIntegration] Azioni disponibili per {pot.PotId}:");
        
        // Usa il nuovo sistema PotActions per determinare le azioni disponibili
        if (pot.PotActions.CanPlant())
        {
            Debug.Log($"  - Piantare seme (Costo: {potSystemConfig?.CostCryPerPotAction ?? 1} CRY)");
        }
        
        if (pot.PotActions.CanWater())
        {
            Debug.Log($"  - Annaffiare pianta (Costo: {potSystemConfig?.CostCryPerPotAction ?? 1} CRY)");
        }
        
        if (pot.PotActions.CanLight())
        {
            Debug.Log($"  - Illuminare pianta (Costo: {potSystemConfig?.CostCryPerPotAction ?? 1} CRY)");
        }
        
        Debug.Log($"    Azioni richieste: 1 per azione");
    }
    
    /// <summary>
    /// Configura i vasi con la configurazione del sistema
    /// </summary>
    public void ConfigurePots()
    {
        if (potsBootstrap == null) return;
        
        PotSlot[] allPots = potsBootstrap.GetAllPots();
        foreach (PotSlot pot in allPots)
        {
            if (pot.PotActions != null && potSystemConfig != null)
            {
                pot.PotActions.SetConfig(potSystemConfig);
                Debug.Log($"[PotSystemIntegration] Configurato vaso {pot.PotId}");
            }
        }
    }
    
    /// <summary>
    /// Restituisce il costo totale per tutte le azioni disponibili
    /// </summary>
    public int GetTotalActionCost(PotSlot pot)
    {
        if (pot == null || pot.PotActions == null) return 0;
        
        int totalCost = 0;
        
        if (pot.PotActions.CanPlant()) totalCost += potSystemConfig?.CostCryPerPotAction ?? 1;
        if (pot.PotActions.CanWater()) totalCost += potSystemConfig?.CostCryPerPotAction ?? 1;
        if (pot.PotActions.CanLight()) totalCost += potSystemConfig?.CostCryPerPotAction ?? 1;
        
        return totalCost;
    }
    
    /// <summary>
    /// Restituisce il numero di azioni richieste per tutte le azioni disponibili
    /// </summary>
    public int GetTotalActionsRequired(PotSlot pot)
    {
        if (pot == null || pot.PotActions == null) return 0;
        
        int actions = 0;
        if (pot.PotActions.CanPlant()) actions++;
        if (pot.PotActions.CanWater()) actions++;
        if (pot.PotActions.CanLight()) actions++;
        
        return actions;
    }
    
    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!enableIntegration) return;
        
        // Disegna indicatori per l'integrazione
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);
        
        // Label
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, "PotSystemIntegration");
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.3f, $"Enabled: {enableIntegration}");
    }
    #endif
}
