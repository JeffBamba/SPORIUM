using System.Linq;
using _Project.Sporae.Core;
using UnityEngine;
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
    [SerializeField] private DayCycleController dayCycleController;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Riferimenti ai sistemi
    private GameManager _gameManager;
    private Inventory _playerInventory;
    private PotStateModel _potState;
    private DayCycleSystem _dayCycleSystem;
    
    // Proprietà pubbliche
    public PotSlot PotSlot => potSlot;
    public PotStateModel PotState => _potState;
    public bool HasPlant => _potState?.HasPlant ?? false;
    
    private void Awake()
    {
        _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
        
        // Trova il PotSlot se non assegnato
        if (potSlot == null)
            potSlot = GetComponent<PotSlot>();
        
        // Trova il PotGrowthController se non assegnato
        if (potGrowthController == null)
            potGrowthController = GetComponent<PotGrowthController>();
        
        // Trova il DayCycleController se non assegnato
        if (dayCycleController == null)
            dayCycleController = FindObjectOfType<DayCycleController>();
        
        // Trova il GameManager
        _gameManager = FindObjectOfType<GameManager>();
        _playerInventory = _gameManager.PlayerInventory;
        
        // Inizializza lo stato del vaso
        InitializePotState();
        
        if (showDebugLogs)
            Debug.Log($"[PotActions] Inizializzato per {potSlot?.PotId ?? "vaso sconosciuto"}");
        
        // Registra il vaso nel sistema di crescita (BLK-01.03A)
        // NON registrare qui per evitare duplicazione con DoPlant
        if (showDebugLogs)
            Debug.Log($"[PotActions] {potSlot?.PotId} inizializzato, registrazione gestita da DoPlant");
    }
    
    private void InitializePotState()
    {
        if (potSlot == null) 
            return;
        
        // Cerca PotStateModel esistente prima di crearne uno nuovo
        var existingPotGrowthController = GetComponent<PotGrowthController>();
        if (existingPotGrowthController != null)
        {
            _potState = existingPotGrowthController.GetPotState();
            if (showDebugLogs && _potState != null)
                Debug.Log($"[PotActions] Stato esistente trovato per {potSlot.PotId}: {_potState}");
        }
            
        // Crea nuovo solo se non esiste
        if (_potState != null)
            return;
        
        _potState = new PotStateModel(potSlot.PotId);
        if (showDebugLogs)
            Debug.Log($"[PotActions] Nuovo stato creato per {potSlot.PotId}: {_potState}");
    }
    
    #region Action Validation Methods

    public bool IsPlayerHasSeed()
    {
        return _playerInventory.Items.Any(
            item => item.Items.Count > 0 && item.Items.ElementAt(0).ItemConfig.IsSeed
        );
    }
    
    /// <summary>
    /// Verifica se è possibile piantare un seme
    /// </summary>
    public bool CanPlant()
    {
        if (_potState == null) 
            return false;
        
        bool
            isEmpty = _potState.IsEmpty,
            hasSeed = IsPlayerHasSeed(),
            inRange = IsPlayerInRange(),
            hasResources = CanConsumeResources(),
            notWateredOnThisDay = _potState.LastWateredDay != _dayCycleSystem.CurrentDay;
        
        if (showDebugLogs)
            Debug.Log($"[PotActions][{potSlot?.PotId}] CanPlant: Empty={isEmpty}, Seed={hasSeed}, Range={inRange}, Resources={hasResources}");
        
        return isEmpty && hasSeed && inRange && hasResources && notWateredOnThisDay;
    }

    public bool CanUproot()
    {
        if (_potState == null)
            return false;
        
        bool hasPlant = _potState.HasPlantGrowing;
        return hasPlant;
    }
    
    /// <summary>
    /// Verifica se è possibile annaffiare la pianta
    /// </summary>
    public bool CanWater()
    {
        if (_potState == null) 
            return false;
        
        // Precondizioni: vaso ha pianta, idratazione non al massimo, player in range, risorse sufficienti
        bool 
            hasPlant = _potState.HasPlantGrowing,
            hydrationNotMax = !_potState.IsHydrationMax(GetMaxHydration()),
            inRange = IsPlayerInRange(),
            hasResources = CanConsumeResources(),
            hasWater = _playerInventory.Has(Items.Water);
        
        if (showDebugLogs)
            Debug.Log($"[PotActions][{potSlot?.PotId}] CanWater: Plant={hasPlant}, HydrationNotMax={hydrationNotMax}, Range={inRange}, Resources={hasResources}");
        
        return hasPlant && hydrationNotMax && hasWater && inRange && hasResources;
    }
    
    /// <summary>
    /// Verifica se è possibile illuminare la pianta
    /// </summary>
    public bool CanLight()
    {
        if (_potState == null)
            return false;
        
        // Precondizioni: vaso ha pianta, luce non al massimo, player in range, risorse sufficienti
        bool
            hasPlant = _potState.HasPlantGrowing,
            lightNotMax = !_potState.IsLightExposureMax(GetMaxLightExposure()),
            inRange = IsPlayerInRange(),
            hasResources = CanConsumeResources(),
            notPlantedOnThisDay = _potState.PlantedDay != _dayCycleSystem.CurrentDay;
        
        if (showDebugLogs)
            Debug.Log($"[PotActions][{potSlot?.PotId}] CanLight: Plant={hasPlant}, LightNotMax={lightNotMax}, Range={inRange}, Resources={hasResources}");
        
        return hasPlant && lightNotMax && inRange && hasResources && notPlantedOnThisDay;
    }
    
    #endregion
    
    #region Action Execution Methods

    private bool ConsumeSeed()
    {
        foreach (var item in _playerInventory.Items.ToList())
            if (item.Items.Count > 0 && item.Items.ElementAt(0).ItemConfig.IsSeed)
                return _playerInventory.Consume(item.TypeId);
        return false;
    }
    
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
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Plant, potSlot, "Insufficient resources");
            return false;
        }
        
        // Consuma il seme dall'inventario
        if (!ConsumeSeed())
        {
            Debug.LogError($"[PotActions] Impossible to consume seed");
            return false;
        }
        
        // Aggiorna lo stato del vaso (Stage 1 = Seed)
        _potState.PlantSeed(_dayCycleSystem.CurrentDay);
        
        // Notifica il sistema di crescita (BLK-01.03A)
        if (potGrowthController)
            potGrowthController.OnPlanted();
        
        // Registra il vaso nel sistema di crescita se non già fatto
        if (dayCycleController)
            dayCycleController.RegisterPot(_potState);
        
        // Notifica il cambio stato
        PotEvents.EmitAction(PotEvents.PotActionType.Plant, potSlot);
        PotEvents.EmitChanged(potSlot);
        
        if (showDebugLogs)
            Debug.Log($"[ACT-001][{potSlot.PotId}] Plant OK: seed planted, state={_potState}");
        
        return true;
    }
    
    public bool DoUproot()
    {
        if (!CanUproot())
            return false;
        
        _potState.Stage = 0;
     
        if (dayCycleController != null)
            dayCycleController.UnregisterPot(_potState);
        
        if (potGrowthController != null)
            potGrowthController.OnUprooted();

        _playerInventory.Add(Items.WholePlant);
        
        // Notifica il cambio stato
        PotEvents.EmitAction(PotEvents.PotActionType.Uproot, potSlot);
        PotEvents.EmitChanged(potSlot);
        
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
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Water, potSlot, "Insufficient resources");
            return false;
        }

        if (!_gameManager.PlayerInventory.Consume(Items.Water))
        {
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Water, potSlot, "Insufficient resources");
            return false;
        }
        
        // Aumenta l'idratazione
        if (!_potState.IncreaseHydration(GetMaxHydration()))
            return false;
        
        // Imposta timestamp per crescita 
        _potState.UpdateWateringDay(_dayCycleSystem.CurrentDay);
            
        // Notifica il cambio stato
        PotEvents.EmitAction(PotEvents.PotActionType.Water, potSlot);
        PotEvents.EmitChanged(potSlot);
            
        if (showDebugLogs)
            Debug.Log($"[ACT-002][{potSlot.PotId}] Water OK: hydration={_potState.Hydration}/{GetMaxHydration()}, timestamp aggiornato");
        
        return true;

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
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Light, potSlot, "Insufficient resources");
            return false;
        }
        
        // Aumenta l'esposizione alla luce
        if (!_potState.IncreaseLightExposure(GetMaxLightExposure()))
            return false;
        
        // Imposta timestamp per crescita
        _potState.UpdateLightingDay(_dayCycleSystem.CurrentDay);
            
        // Notifica il cambio stato
        PotEvents.EmitAction(PotEvents.PotActionType.Light, potSlot);
        PotEvents.EmitChanged(potSlot);
            
        if (showDebugLogs)
            Debug.Log($"[ACT-003][{potSlot.PotId}] Light OK: light={_potState.LightExposure}/{GetMaxLightExposure()}, timestamp aggiornato");
        
        return true;

    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Verifica se il player è in range per interagire
    /// </summary>
    private bool IsPlayerInRange()
    {
        if (!potSlot)
            return false;
        
        // Usa la distanza di interazione dal PotSlot
        float interactDistance = config ? config.InteractDistance : 2.0f;
        
        // Trova il player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (!player)
            return false;
        
        float distance = Vector2.Distance(player.transform.position, transform.position);
        return distance <= interactDistance;
    }
    
    /// <summary>
    /// Verifica se è possibile consumare le risorse necessarie
    /// </summary>
    private bool CanConsumeResources()
    {
        if (!_gameManager) 
            return false;
        
        int actionsCost = GetActionsCost();
        int cryCost = GetCryCost();
        
        return _gameManager.ActionsLeft >= actionsCost && _gameManager.CurrentCRY >= cryCost;
    }
    
    /// <summary>
    /// Tenta di consumare le risorse necessarie
    /// </summary>
    private bool TryConsumeResources()
    {
        if (!_gameManager) 
            return false;
        
        int actionsCost = GetActionsCost();
        
        // Usa il metodo TrySpendAction del GameManager esistente
        return _gameManager.TrySpendAction(actionsCost);
    }
    
    /// <summary>
    /// Restituisce il costo in azioni per un'azione
    /// </summary>
    private int GetActionsCost()
    {
        return config ? config.CostActionsPerPotAction : 1;
    }
    
    /// <summary>
    /// Restituisce il costo in CRY per un'azione
    /// </summary>
    private int GetCryCost()
    {
        return config ? config.CostCryPerPotAction : 1;
    }
    
    /// <summary>
    /// Restituisce il limite massimo di idratazione
    /// </summary>
    private int GetMaxHydration()
    {
        return config ? config.MaxHydration : 3;
    }
    
    /// <summary>
    /// Restituisce il limite massimo di esposizione alla luce
    /// </summary>
    private int GetMaxLightExposure()
    {
        return config ? config.MaxLightExposure : 3;
    }
    
    #endregion
    
    #region Failure Reason Methods
    
    private string GetPlantFailureReason()
    {
        if (_potState == null) return "Stato vaso non valido";
        if (!_potState.IsEmpty) return "Vaso non vuoto";
        if (!_playerInventory.Has(Items.Seed001)) return "Nessun seme disponibile";
        if (!IsPlayerInRange()) return "Troppo lontano";
        if (!CanConsumeResources()) return "Azioni o CRY insufficienti";
        return "Azione non permessa";
    }
    
    private string GetWaterFailureReason()
    {
        if (_potState == null) return "Stato vaso non valido";
        if (!_potState.HasPlantGrowing) return "Vaso vuoto";
        if (_potState.IsHydrationMax(GetMaxHydration())) return "Idratazione al massimo";
        if (!IsPlayerInRange()) return "Troppo lontano";
        if (!CanConsumeResources()) return "Azioni o CRY insufficienti";
        return "Azione non permessa";
    }
    
    private string GetLightFailureReason()
    {
        if (_potState == null) return "Stato vaso non valido";
        if (!_potState.HasPlantGrowing) return "Vaso vuoto";
        if (_potState.IsLightExposureMax(GetMaxLightExposure())) return "Luce al massimo";
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
        return _potState;
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
        if (_potState != null)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.8f, 
                $"H:{_potState.Hydration}/{GetMaxHydration()} L:{_potState.LightExposure}/{GetMaxLightExposure()}");
        }
    }
    #endif
}
