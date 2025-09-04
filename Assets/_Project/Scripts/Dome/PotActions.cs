using UnityEngine;
using Sporae.Core;
using Sporae.Dome.PotSystem.Growth;

/// <summary>
/// Gestisce le azioni base sui vasi: piantare, annaffiare e illuminare.
/// Implementa il gating per distanza, azioni disponibili e CRY.
/// Integra con GameManager per il consumo di risorse e inventario.
/// </summary>
public class PotActions : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PotSlot potSlot;
    [SerializeField] private PotSystemConfig config;
    [SerializeField] private PotGrowthController potGrowthController;
    [SerializeField] private SPOR_BLK_01_03A_DayCycleController dayCycleController;
    
    [Header("Seed Configuration")]
    [SerializeField] private string genericSeedCode = "SDE-001";
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Riferimenti ai sistemi
    private GameManager gameManager;
    private PotStateModel potState;
    
    // Proprietà pubbliche
    public PotSlot PotSlot => potSlot;
    public PotStateModel PotState => potState;
    public bool HasPlant => potState?.HasPlant ?? false;
    
    void Awake()
    {
        // Trova il PotSlot se non assegnato
        if (potSlot == null)
        {
            potSlot = GetComponent<PotSlot>();
        }
        
        // Trova il PotGrowthController se non assegnato
        if (potGrowthController == null)
        {
            potGrowthController = GetComponent<PotGrowthController>();
        }
        
        // Trova il DayCycleController se non assegnato
        if (dayCycleController == null)
        {
            dayCycleController = FindObjectOfType<SPOR_BLK_01_03A_DayCycleController>();
        }
        
        // Trova il GameManager
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("[PotActions] GameManager non trovato! Componente disabilitato.");
            enabled = false;
            return;
        }
        
        // Inizializza lo stato del vaso
        InitializePotState();
        
        if (showDebugLogs)
        {
            Debug.Log($"[PotActions] Inizializzato per {potSlot?.PotId ?? "vaso sconosciuto"}");
        }
        
        // Registra il vaso nel sistema di crescita (BLK-01.03A)
        // NON registrare qui per evitare duplicazione con DoPlant
        if (showDebugLogs)
        {
            Debug.Log($"[PotActions] {potSlot?.PotId} inizializzato, registrazione gestita da DoPlant");
        }
    }
    
    private void InitializePotState()
    {
        if (potSlot != null)
        {
            // Cerca PotStateModel esistente prima di crearne uno nuovo
            var existingPotGrowthController = GetComponent<PotGrowthController>();
            if (existingPotGrowthController != null)
            {
                potState = existingPotGrowthController.GetPotState();
                if (showDebugLogs && potState != null)
                {
                    Debug.Log($"[PotActions] Stato esistente trovato per {potSlot.PotId}: {potState}");
                }
            }
            
            // Crea nuovo solo se non esiste
            if (potState == null)
            {
                potState = new PotStateModel(potSlot.PotId);
                if (showDebugLogs)
                {
                    Debug.Log($"[PotActions] Nuovo stato creato per {potSlot.PotId}: {potState}");
                }
            }
        }
    }
    
    #region Action Validation Methods
    
    /// <summary>
    /// Verifica se è possibile piantare un seme
    /// </summary>
    public bool CanPlant()
    {
        if (potState == null) return false;
        
        // Precondizioni: vaso vuoto, seme disponibile, player in range, risorse sufficienti
        bool isEmpty = potState.IsEmpty;
        bool hasSeed = gameManager.HasItem(genericSeedCode, 1);
        bool inRange = IsPlayerInRange();
        bool hasResources = CanConsumeResources();
        bool notWateredOnThisDay = potState.LastWateredDay != gameManager.CurrentDay;
        
        if (showDebugLogs)
        {
            Debug.Log($"[PotActions][{potSlot?.PotId}] CanPlant: Empty={isEmpty}, Seed={hasSeed}, Range={inRange}, Resources={hasResources}");
        }
        
        return isEmpty && hasSeed && inRange && hasResources && notWateredOnThisDay;
    }
    
    /// <summary>
    /// Verifica se è possibile annaffiare la pianta
    /// </summary>
    public bool CanWater()
    {
        if (potState == null) return false;
        
        // Precondizioni: vaso ha pianta, idratazione non al massimo, player in range, risorse sufficienti
        bool hasPlant = potState.HasPlantGrowing;
        bool hydrationNotMax = !potState.IsHydrationMax(GetMaxHydration());
        bool inRange = IsPlayerInRange();
        bool hasResources = CanConsumeResources();
        bool notWateredOnThisDay = potState.LastWateredDay != gameManager.CurrentDay;
        bool notPlantedOnThisDay = potState.PlantedDay != gameManager.CurrentDay;
        
        if (showDebugLogs)
        {
            Debug.Log($"[PotActions][{potSlot?.PotId}] CanWater: Plant={hasPlant}, HydrationNotMax={hydrationNotMax}, Range={inRange}, Resources={hasResources}");
        }
        
        return hasPlant && hydrationNotMax && inRange && hasResources && notPlantedOnThisDay && notWateredOnThisDay;
    }
    
    /// <summary>
    /// Verifica se è possibile illuminare la pianta
    /// </summary>
    public bool CanLight()
    {
        if (potState == null) return false;
        
        // Precondizioni: vaso ha pianta, luce non al massimo, player in range, risorse sufficienti
        bool hasPlant = potState.HasPlantGrowing;
        bool lightNotMax = !potState.IsLightExposureMax(GetMaxLightExposure());
        bool inRange = IsPlayerInRange();
        bool hasResources = CanConsumeResources();
        bool notPlantedOnThisDay = potState.PlantedDay != gameManager.CurrentDay; 
        bool notLightedOnThisDay = potState.LastLitDay != gameManager.CurrentDay;
        
        if (showDebugLogs)
        {
            Debug.Log($"[PotActions][{potSlot?.PotId}] CanLight: Plant={hasPlant}, LightNotMax={lightNotMax}, Range={inRange}, Resources={hasResources}");
        }
        
        return hasPlant && lightNotMax && inRange && hasResources && notPlantedOnThisDay && notLightedOnThisDay;
    }
    
    #endregion
    
    #region Action Execution Methods
    
    /// <summary>
    /// Esegue l'azione di piantare un seme
    /// </summary>
    public bool DoPlant()
    {
        if (!CanPlant())
        {
            string reason = GetPlantFailureReason();
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Plant, potSlot, reason);
            return false;
        }
        
        // Consuma le risorse
        if (!TryConsumeResources())
        {
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Plant, potSlot, "Risorse insufficienti");
            return false;
        }
        
        // Consuma il seme dall'inventario
        if (!gameManager.ConsumeItem(genericSeedCode, 1))
        {
            Debug.LogError($"[PotActions] Impossibile consumare seme {genericSeedCode}");
            return false;
        }
        
        // Aggiorna lo stato del vaso (Stage 1 = Seed)
        potState.PlantSeed(gameManager.CurrentDay);
        
        // Notifica il sistema di crescita (BLK-01.03A)
        if (potGrowthController != null)
        {
            potGrowthController.OnPlanted();
        }
        
        // Registra il vaso nel sistema di crescita se non già fatto
        if (dayCycleController != null)
        {
            dayCycleController.RegisterPot(potState);
        }
        
        // Notifica il cambio stato
        PotEvents.EmitAction(PotEvents.PotActionType.Plant, potSlot);
        PotEvents.EmitChanged(potSlot);
        
        if (showDebugLogs)
        {
            Debug.Log($"[ACT-001][{potSlot.PotId}] Plant OK: seme piantato, stato={potState}");
        }
        
        return true;
    }
    
    /// <summary>
    /// Esegue l'azione di annaffiare la pianta
    /// </summary>
    public bool DoWater()
    {
        if (!CanWater())
        {
            string reason = GetWaterFailureReason();
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Water, potSlot, reason);
            return false;
        }
        
        // Consuma le risorse
        if (!TryConsumeResources())
        {
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Water, potSlot, "Risorse insufficienti");
            return false;
        }
        
        // Aumenta l'idratazione
        if (potState.IncreaseHydration(GetMaxHydration()))
        {
            // Imposta timestamp per crescita (BLK-01.03A)
            potState.UpdateWateringDay(gameManager.CurrentDay);
            
            // Notifica il cambio stato
            PotEvents.EmitAction(PotEvents.PotActionType.Water, potSlot);
            PotEvents.EmitChanged(potSlot);
            
            if (showDebugLogs)
            {
                Debug.Log($"[ACT-002][{potSlot.PotId}] Water OK: hydration={potState.Hydration}/{GetMaxHydration()}, timestamp aggiornato");
            }
            
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Esegue l'azione di illuminare la pianta
    /// </summary>
    public bool DoLight()
    {
        if (!CanLight())
        {
            string reason = GetLightFailureReason();
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Light, potSlot, reason);
            return false;
        }
        
        // Consuma le risorse
        if (!TryConsumeResources())
        {
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Light, potSlot, "Risorse insufficienti");
            return false;
        }
        
        // Aumenta l'esposizione alla luce
        if (potState.IncreaseLightExposure(GetMaxLightExposure()))
        {
            // Imposta timestamp per crescita (BLK-01.03A)
            potState.UpdateLightingDay(gameManager.CurrentDay);
            
            // Notifica il cambio stato
            PotEvents.EmitAction(PotEvents.PotActionType.Light, potSlot);
            PotEvents.EmitChanged(potSlot);
            
            if (showDebugLogs)
            {
                Debug.Log($"[ACT-003][{potSlot.PotId}] Light OK: light={potState.LightExposure}/{GetMaxLightExposure()}, timestamp aggiornato");
            }
            
            return true;
        }
        
        return false;
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Verifica se il player è in range per interagire
    /// </summary>
    private bool IsPlayerInRange()
    {
        if (potSlot == null) return false;
        
        // Usa la distanza di interazione dal PotSlot
        float interactDistance = config != null ? config.InteractDistance : 2.0f;
        
        // Trova il player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;
        
        float distance = Vector2.Distance(player.transform.position, transform.position);
        return distance <= interactDistance;
    }
    
    /// <summary>
    /// Verifica se è possibile consumare le risorse necessarie
    /// </summary>
    private bool CanConsumeResources()
    {
        if (gameManager == null) return false;
        
        int actionsCost = GetActionsCost();
        int cryCost = GetCryCost();
        
        return gameManager.ActionsLeft >= actionsCost && gameManager.CurrentCRY >= cryCost;
    }
    
    /// <summary>
    /// Tenta di consumare le risorse necessarie
    /// </summary>
    private bool TryConsumeResources()
    {
        if (gameManager == null) return false;
        
        int actionsCost = GetActionsCost();
        int cryCost = GetCryCost();
        
        // Usa il metodo TrySpendAction del GameManager esistente
        return gameManager.TrySpendAction(cryCost);
    }
    
    /// <summary>
    /// Restituisce il costo in azioni per un'azione
    /// </summary>
    private int GetActionsCost()
    {
        return config != null ? config.CostActionsPerPotAction : 1;
    }
    
    /// <summary>
    /// Restituisce il costo in CRY per un'azione
    /// </summary>
    private int GetCryCost()
    {
        return config != null ? config.CostCryPerPotAction : 1;
    }
    
    /// <summary>
    /// Restituisce il limite massimo di idratazione
    /// </summary>
    private int GetMaxHydration()
    {
        return config != null ? config.MaxHydration : 3;
    }
    
    /// <summary>
    /// Restituisce il limite massimo di esposizione alla luce
    /// </summary>
    private int GetMaxLightExposure()
    {
        return config != null ? config.MaxLightExposure : 3;
    }
    
    #endregion
    
    #region Failure Reason Methods
    
    private string GetPlantFailureReason()
    {
        if (potState == null) return "Stato vaso non valido";
        if (!potState.IsEmpty) return "Vaso non vuoto";
        if (!gameManager.HasItem(genericSeedCode, 1)) return "Nessun seme disponibile";
        if (!IsPlayerInRange()) return "Troppo lontano";
        if (!CanConsumeResources()) return "Azioni o CRY insufficienti";
        return "Azione non permessa";
    }
    
    private string GetWaterFailureReason()
    {
        if (potState == null) return "Stato vaso non valido";
        if (!potState.HasPlantGrowing) return "Vaso vuoto";
        if (potState.IsHydrationMax(GetMaxHydration())) return "Idratazione al massimo";
        if (!IsPlayerInRange()) return "Troppo lontano";
        if (!CanConsumeResources()) return "Azioni o CRY insufficienti";
        return "Azione non permessa";
    }
    
    private string GetLightFailureReason()
    {
        if (potState == null) return "Stato vaso non valido";
        if (!potState.HasPlantGrowing) return "Vaso vuoto";
        if (potState.IsLightExposureMax(GetMaxLightExposure())) return "Luce al massimo";
        if (!IsPlayerInRange()) return "Troppo lontano";
        if (!CanConsumeResources()) return "Azioni o CRY insufficienti";
        return "Azione non permessa";
    }
    
    #endregion
    
    #region Public Interface
    
    /// <summary>
    /// Imposta la configurazione del sistema
    /// </summary>
    public void SetConfig(PotSystemConfig newConfig)
    {
        config = newConfig;
        if (showDebugLogs)
        {
            Debug.Log($"[PotActions] Configurazione aggiornata per {potSlot?.PotId}");
        }
    }
    
    /// <summary>
    /// Restituisce lo stato corrente del vaso
    /// </summary>
    public PotStateModel GetCurrentState()
    {
        return potState;
    }
    
    /// <summary>
    /// Forza l'aggiornamento dello stato (utile per debugging)
    /// </summary>
    public void ForceStateUpdate()
    {
        if (potState != null)
        {
            PotEvents.EmitChanged(potSlot);
        }
    }
    
    /// <summary>
    /// DEPRECATO - La registrazione è ora gestita da DoPlant per evitare duplicazione
    /// </summary>
    [System.Obsolete("Usa la registrazione automatica in DoPlant invece")]
    private void RegisterPotInGrowthSystem()
    {
        if (showDebugLogs)
        {
            Debug.LogWarning($"[PotActions] {potSlot?.PotId} - RegisterPotInGrowthSystem è deprecato!");
        }
        
        // La registrazione è ora gestita automaticamente da DoPlant
        // per evitare duplicazione con RoomDomePotsBootstrap
    }
    
    #endregion
    
    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (potSlot == null) return;
        
        // Disegna raggio di interazione
        float interactDistance = config != null ? config.InteractDistance : 2.0f;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
        
        // Disegna stato del vaso
        if (potState != null)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.8f, 
                $"H:{potState.Hydration}/{GetMaxHydration()} L:{potState.LightExposure}/{GetMaxLightExposure()}");
        }
    }
    #endif
}
