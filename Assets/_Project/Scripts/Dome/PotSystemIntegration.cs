using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Esempio di integrazione del sistema dei vasi con il GameManager esistente.
/// Questo script mostra come collegare i vasi al sistema di azioni e CRY.
/// DA IMPLEMENTARE IN BLK-01.02+ (per ora è solo un esempio)
/// </summary>
public class PotSystemIntegration : MonoBehaviour
{
    [Header("Integration Settings")]
    [SerializeField] private bool enableIntegration = false;
    [SerializeField] private int plantSeedCost = 5; // CRY per piantare
    [SerializeField] private int waterPlantCost = 2; // CRY per annaffiare
    [SerializeField] private int lightPlantCost = 3; // CRY per illuminare
    
    [Header("References")]
    [SerializeField] private RoomDomePotsBootstrap potsBootstrap;
    
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
        if (pot == null) return;
        
        Debug.Log($"[PotSystemIntegration] Azioni disponibili per {pot.PotId}:");
        
        switch (pot.State)
        {
            case PotState.Empty:
                Debug.Log($"  - Piantare seme (Costo: {plantSeedCost} CRY)");
                Debug.Log($"    Azioni richieste: 1");
                break;
                
            case PotState.Occupied:
                Debug.Log($"  - Annaffiare pianta (Costo: {waterPlantCost} CRY)");
                Debug.Log($"    Azioni richieste: 1");
                break;
                
            case PotState.Growing:
                Debug.Log($"  - Annaffiare pianta (Costo: {waterPlantCost} CRY)");
                Debug.Log($"  - Illuminare pianta (Costo: {lightPlantCost} CRY)");
                Debug.Log($"    Azioni richieste: 1 per azione");
                break;
                
            case PotState.Mature:
                Debug.Log($"  - Raccogliere pianta (Costo: 0 CRY)");
                Debug.Log($"    Azioni richieste: 1");
                break;
        }
    }
    
    /// <summary>
    /// Tenta di piantare un seme nel vaso
    /// DA IMPLEMENTARE IN BLK-01.02
    /// </summary>
    public bool TryPlantSeed(PotSlot pot, string seedId)
    {
        if (!enableIntegration || pot == null) return false;
        
        if (pot.State != PotState.Empty)
        {
            Debug.LogWarning($"[PotSystemIntegration] Impossibile piantare: vaso {pot.PotId} non vuoto.");
            return false;
        }
        
        // Controlla se il player può permettersi l'azione
        if (!gameManager.TrySpendAction(plantSeedCost))
        {
            Debug.LogWarning($"[PotSystemIntegration] Azione non permessa: CRY insufficienti o azioni esaurite.");
            return false;
        }
        
        // Controlla se il player ha il seme
        if (!gameManager.HasItem(seedId, 1))
        {
            Debug.LogWarning($"[PotSystemIntegration] Seme {seedId} non disponibile nell'inventario.");
            return false;
        }
        
        // Consuma il seme
        gameManager.ConsumeItem(seedId, 1);
        
        // Cambia stato del vaso
        pot.SetState(PotState.Occupied);
        
        Debug.Log($"[PotSystemIntegration] Seme {seedId} piantato in {pot.PotId}. CRY spesi: {plantSeedCost}");
        
        return true;
    }
    
    /// <summary>
    /// Tenta di annaffiare la pianta nel vaso
    /// DA IMPLEMENTARE IN BLK-01.02
    /// </summary>
    public bool TryWaterPlant(PotSlot pot)
    {
        if (!enableIntegration || pot == null) return false;
        
        if (pot.State == PotState.Empty)
        {
            Debug.LogWarning($"[PotSystemIntegration] Impossibile annaffiare: vaso {pot.PotId} vuoto.");
            return false;
        }
        
        // Controlla se il player può permettersi l'azione
        if (!gameManager.TrySpendAction(waterPlantCost))
        {
            Debug.LogWarning($"[PotSystemIntegration] Azione non permessa: CRY insufficienti o azioni esaurite.");
            return false;
        }
        
        // Logica di annaffiatura (da implementare in BLK-01.04)
        Debug.Log($"[PotSystemIntegration] Pianta in {pot.PotId} annaffiata. CRY spesi: {waterPlantCost}");
        
        return true;
    }
    
    /// <summary>
    /// Tenta di illuminare la pianta nel vaso
    /// DA IMPLEMENTARE IN BLK-01.02
    /// </summary>
    public bool TryLightPlant(PotSlot pot)
    {
        if (!enableIntegration || pot == null) return false;
        
        if (pot.State == PotState.Empty)
        {
            Debug.LogWarning($"[PotSystemIntegration] Impossibile illuminare: vaso {pot.PotId} vuoto.");
            return false;
        }
        
        // Controlla se il player può permettersi l'azione
        if (!gameManager.TrySpendAction(lightPlantCost))
        {
            Debug.LogWarning($"[PotSystemIntegration] Azione non permessa: CRY insufficienti o azioni esaurite.");
            return false;
        }
        
        // Logica di illuminazione (da implementare in BLK-01.04)
        Debug.Log($"[PotSystemIntegration] Pianta in {pot.PotId} illuminata. CRY spesi: {lightPlantCost}");
        
        return true;
    }
    
    /// <summary>
    /// Tenta di raccogliere la pianta matura
    /// DA IMPLEMENTARE IN BLK-01.02
    /// </summary>
    public bool TryHarvestPlant(PotSlot pot)
    {
        if (!enableIntegration || pot == null) return false;
        
        if (pot.State != PotState.Mature)
        {
            Debug.LogWarning($"[PotSystemIntegration] Impossibile raccogliere: pianta in {pot.PotId} non matura.");
            return false;
        }
        
        // Controlla se il player ha azioni disponibili
        if (!gameManager.TrySpendAction(0)) // 0 CRY per raccogliere
        {
            Debug.LogWarning($"[PotSystemIntegration] Azione non permessa: azioni esaurite.");
            return false;
        }
        
        // Logica di raccolta (da implementare in BLK-01.04)
        // Aggiungi ricompense all'inventario
        
        // Reset stato del vaso
        pot.SetState(PotState.Empty);
        
        Debug.Log($"[PotSystemIntegration] Pianta raccolta da {pot.PotId}. Vaso ora vuoto.");
        
        return true;
    }
    
    /// <summary>
    /// Restituisce il costo totale per tutte le azioni disponibili
    /// </summary>
    public int GetTotalActionCost(PotSlot pot)
    {
        if (pot == null) return 0;
        
        int totalCost = 0;
        
        switch (pot.State)
        {
            case PotState.Empty:
                totalCost += plantSeedCost;
                break;
            case PotState.Occupied:
                totalCost += waterPlantCost;
                break;
            case PotState.Growing:
                totalCost += waterPlantCost + lightPlantCost;
                break;
            case PotState.Mature:
                totalCost = 0; // Raccolta gratuita
                break;
        }
        
        return totalCost;
    }
    
    /// <summary>
    /// Restituisce il numero di azioni richieste per tutte le azioni disponibili
    /// </summary>
    public int GetTotalActionsRequired(PotSlot pot)
    {
        if (pot == null) return 0;
        
        switch (pot.State)
        {
            case PotState.Empty:
                return 1; // Piantare
            case PotState.Occupied:
                return 1; // Annaffiare
            case PotState.Growing:
                return 2; // Annaffiare + Illuminare
            case PotState.Mature:
                return 1; // Raccogliere
            default:
                return 0;
        }
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
