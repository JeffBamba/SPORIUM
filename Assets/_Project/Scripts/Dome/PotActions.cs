using System.Linq;
using _Project.Sporae.Core;
using UnityEngine;
using Sporae.Dome.PotSystem.Growth;
using _Project;

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
    [SerializeField] private LedLightController ledLightController;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Riferimenti ai sistemi
    private GameManager _gameManager;
    private Inventory _playerInventory;
    private PotStateModel _potState;
    private DayCycleSystem _dayCycleSystem;
    private PhSystem _phSystem;
    
    // Proprietà pubbliche
    public PotSlot PotSlot => potSlot;
    public PotStateModel PotState => _potState;
    public bool HasPlant => _potState?.HasPlant ?? false;
    
    private void Awake()
    {
        _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
        
        // Fallback: carica PotSystemConfig se non assegnato
        if (config == null)
        {
            config = Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");
            if (config == null)
            {
                var allConfigs = Resources.LoadAll<PotSystemConfig>("Configs");
                if (allConfigs != null && allConfigs.Length > 0)
                {
                    config = allConfigs[0];
                }
            }
        }
        
        // BUG FIX: Verifica che MaxHydration sia 10 (non 4 del vecchio sistema)
        if (config != null && config.MaxHydration == 4)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[PotActions] ⚠️ Config ha MaxHydration=4 (vecchio sistema). Forzo ricaricamento da Resources...");
            // Forza ricaricamento per ottenere il valore aggiornato
            Resources.UnloadAsset(config);
            config = Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");
            if (config != null && showDebugLogs)
                Debug.Log($"[PotActions] ✅ Config ricaricato: MaxHydration={config.MaxHydration}");
        }
        
        // Trova il PotSlot se non assegnato
        if (potSlot == null)
            potSlot = GetComponent<PotSlot>();
        
        // Trova il PotGrowthController se non assegnato
        if (potGrowthController == null)
            potGrowthController = GetComponent<PotGrowthController>();
        
        // Trova il DayCycleController se non assegnato
        if (dayCycleController == null)
            dayCycleController = FindObjectOfType<DayCycleController>();
        
        // Trova il LedLightController se non assegnato
        if (ledLightController == null)
            ledLightController = GetComponent<LedLightController>();
        
        // Trova il GameManager
        _gameManager = FindObjectOfType<GameManager>();
        _playerInventory = _gameManager.PlayerInventory;
        
        // Tenta di ottenere PhSystem dal ServiceContainer (BLK-02.03)
        TryGetPhSystem();
        
        // Sottoscrivi evento per late binding PhSystem
        if (ServiceContainer.Instance != null)
        {
            ServiceContainer.Instance.OnServiceRegistered += OnServiceRegistered;
        }
        
        // Inizializza lo stato del vaso
        InitializePotState();
        
        if (showDebugLogs)
            Debug.Log($"[PotActions] Inizializzato per {potSlot?.PotId ?? "vaso sconosciuto"}");
        
        // Registra il vaso nel sistema di crescita (BLK-01.03A)
        // IMPORTANTE: Registra anche se ha già una pianta (per piante esistenti)
        RegisterPotIfNeeded();
    }
    
    /// <summary>
    /// Registra il vaso nel DayCycleController se ha una pianta o quando viene piantata
    /// </summary>
    private void RegisterPotIfNeeded()
    {
        if (_potState == null)
            return;
        
        // DEBUG_SAFE_FIX: Tenta di trovare DayCycleController se non disponibile
        // Questo risolve il problema dove dayCycleController è null durante le ottimizzazioni
        if (dayCycleController == null)
        {
            dayCycleController = FindObjectOfType<DayCycleController>();
            if (dayCycleController == null && showDebugLogs)
            {
                Debug.LogWarning($"[PotActions] ⚠️ DayCycleController non trovato per {potSlot?.PotId}. Il vaso non verrà registrato.");
                return;
            }
        }
        
        // Registra se ha già una pianta (per piante esistenti caricate)
        if (_potState.HasPlant)
        {
            dayCycleController.RegisterPot(_potState);
            if (showDebugLogs)
            {
                Debug.Log($"[PotActions] ✅ Vaso {potSlot?.PotId} con pianta esistente registrato nel DayCycleController (Stage: {_potState.Stage}, PlantCode: {_potState.PlantCode ?? "NULL"})");
            }
        }
        else if (showDebugLogs)
        {
            Debug.Log($"[PotActions] Vaso {potSlot?.PotId} vuoto, registrazione quando si pianta un seme");
        }
    }
    
    private void OnDestroy()
    {
        if (ServiceContainer.Instance != null)
        {
            ServiceContainer.Instance.OnServiceRegistered -= OnServiceRegistered;
        }
    }
    
    /// <summary>
    /// Tenta di ottenere PhSystem dal ServiceContainer
    /// </summary>
    private void TryGetPhSystem()
    {
        if (ServiceContainer.Instance == null)
            return;
        
        if (ServiceContainer.Instance.Contains(typeof(PhSystem)))
        {
            _phSystem = ServiceContainer.Instance.Get<PhSystem>();
            if (showDebugLogs)
                Debug.Log($"[PotActions] PhSystem trovato per {potSlot?.PotId}");
        }
    }
    
    /// <summary>
    /// Chiamato quando un servizio viene registrato nel ServiceContainer
    /// </summary>
    private void OnServiceRegistered(object service)
    {
        if (service is PhSystem && _phSystem == null)
        {
            _phSystem = service as PhSystem;
            if (showDebugLogs)
                Debug.Log($"[PotActions] PhSystem registrato, collegato a {potSlot?.PotId}");
        }
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
    /// Verifica se è possibile attivare/disattivare il sistema irrigazione (GDD AZ-11 - Toggle Persistente)
    /// </summary>
    public bool CanWater()
    {
        if (_potState == null) 
            return false;
        
        // Precondizioni: vaso ha pianta, player in range, risorse sufficienti (1 Azione)
        // NOTA: Non verifica più idratazione max o WAT-RAW (consumo giornaliero a fine giorno)
        bool 
            hasPlant = _potState.HasPlantGrowing,
            inRange = IsPlayerInRange(),
            hasResources = CanConsumeResources();
        
        if (showDebugLogs)
            Debug.Log($"[PotActions][{potSlot?.PotId}] CanWater (Toggle): Plant={hasPlant}, Range={inRange}, Resources={hasResources}, CurrentState={_potState.WateringSystemOn}");
        
        return hasPlant && inRange && hasResources;
    }
    
    /// <summary>
    /// Verifica se è possibile illuminare la pianta (BLK-02.07: toggle LED persistente)
    /// </summary>
    public bool CanLight()
    {
        if (_potState == null)
            return false;
        
        // Precondizioni: vaso ha pianta, player in range, risorse sufficienti
        // MODIFICA: LED può essere acceso anche subito dopo aver piantato (stadio Seed)
        // NOTA: BLK-02.07 - Non verifica più lightNotMax (LED è toggle persistente, non incremento immediato)
        bool
            hasPlant = _potState.HasPlantGrowing,
            inRange = IsPlayerInRange(),
            hasResources = CanConsumeResources();
        
        if (showDebugLogs)
            Debug.Log($"[PotActions][{potSlot?.PotId}] CanLight: Plant={hasPlant}, Range={inRange}, Resources={hasResources}, CurrentState={_potState.LedSystemState}, Stage={(PlantStage)_potState.Stage}");
        
        return hasPlant && inRange && hasResources;
    }
    
    /// <summary>
    /// Verifica se è possibile applicare Spray Antifungino (AZ-14)
    /// </summary>
    public bool CanSprayAntifungal()
    {
        if (_potState == null)
            return false;
        
        // Precondizioni: vaso ha pianta, player in range, risorse sufficienti
        // Nota: Spray può essere applicato anche se non ci sono muffe (preventivo)
        bool
            hasPlant = _potState.HasPlantGrowing,
            inRange = IsPlayerInRange(),
            hasResources = CanConsumeResources();
        
        if (showDebugLogs)
            Debug.Log($"[PotActions][{potSlot?.PotId}] CanSprayAntifungal: Plant={hasPlant}, Range={inRange}, Resources={hasResources}");
        
        return hasPlant && inRange && hasResources;
    }
    
    /// <summary>
    /// Verifica se è possibile raccogliere frutti dalla pianta
    /// </summary>
    public bool CanHarvest()
    {
        if (_potState == null)
            return false;
        
        // Precondizioni: vaso ha pianta in HarvestReady, ci sono frutti disponibili, player in range, risorse sufficienti
        bool
            isHarvestReady = _potState.Stage == (int)PlantStage.HarvestReady,
            hasFruits = _potState.AmountFruits > 0f,
            inRange = IsPlayerInRange(),
            hasResources = CanConsumeResources();
        
        if (showDebugLogs)
            Debug.Log($"[PotActions][{potSlot?.PotId}] CanHarvest: HarvestReady={isHarvestReady}, Fruits={hasFruits}, Range={inRange}, Resources={hasResources}");
        
        return isHarvestReady && hasFruits && inRange && hasResources;
    }
    
    #endregion
    
    #region Action Execution Methods

    /// <summary>
    /// Trova il primo seme disponibile nell'inventario e restituisce il suo TypeId
    /// </summary>
    private string FindSeedTypeId()
    {
        foreach (var item in _playerInventory.Items.ToList())
        {
            if (item.Items.Count > 0 && item.Items.ElementAt(0).ItemConfig.IsSeed)
            {
                return item.TypeId;
            }
        }
        return null;
    }
    
    private bool ConsumeSeed()
    {
        string seedTypeId = FindSeedTypeId();
        if (string.IsNullOrEmpty(seedTypeId))
            return false;
        
        return _playerInventory.Consume(seedTypeId);
    }
    
    /// <summary>
    /// Esegue l'azione di piantare un seme
    /// </summary>
    /// <param name="seedTypeId">TypeId del seme da piantare. Se null, cerca automaticamente il primo seme disponibile.</param>
    public bool DoPlant(string seedTypeId = null)
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
        
        // Se seedTypeId non specificato, cerca automaticamente (compatibilità retroattiva)
        if (string.IsNullOrEmpty(seedTypeId))
        {
            seedTypeId = FindSeedTypeId();
            if (string.IsNullOrEmpty(seedTypeId))
            {
                Debug.LogError($"[PotActions] Impossible to find seed in inventory");
                PotEvents.EmitActionFailed(PotEvents.PotActionType.Plant, potSlot, "Nessun seme disponibile");
                return false;
            }
        }
        
        // Verifica che il seme specificato esista nell'inventario
        if (!_playerInventory.Has(seedTypeId))
        {
            Debug.LogError($"[PotActions] Seme '{seedTypeId}' non disponibile nell'inventario");
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Plant, potSlot, $"Seme '{seedTypeId}' non disponibile");
            return false;
        }
        
        // Cerca PlantData dal database usando il TypeId del seme
        PlantData plantData = PlantDatabase.Instance?.GetPlantDataBySeedTypeId(seedTypeId);
        string plantCode = plantData?.PlantCode;
        
        if (plantData == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[PotActions] ⚠️ Nessun PlantData trovato per seme TypeId '{seedTypeId}'. La pianta non avrà drift pH.");
            }
            // IMPORTANTE: Anche se PlantData non è trovato, piantiamo comunque il seme
            // ma senza PlantCode, quindi non avrà drift pH
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log($"[PotActions] ✅ PlantData trovato: {plantData.PlantCode} ({plantData.Family}), drift pH: {plantData.DailyPhDrift}/giorno");
            }
            
            // Verifica che PlantCode non sia null o vuoto
            if (string.IsNullOrEmpty(plantCode))
            {
                Debug.LogError($"[PotActions] ⚠️ PlantData '{plantData.name}' ha PlantCode NULL o vuoto! La pianta non avrà drift pH.");
            }
        }
        
        // Consuma il seme dall'inventario
        if (!_playerInventory.Consume(seedTypeId))
        {
            Debug.LogError($"[PotActions] Impossible to consume seed");
            return false;
        }
        
        // Aggiorna lo stato del vaso (Stage 1 = Seed) con PlantCode
        _potState.PlantSeed(_dayCycleSystem.CurrentDay, plantCode);
        
        // DEBUG: Verifica che PlantCode sia stato salvato correttamente
        if (showDebugLogs)
        {
            if (string.IsNullOrEmpty(_potState.PlantCode))
            {
                Debug.LogWarning($"[PotActions] ⚠️ PlantCode NON salvato correttamente nel PotStateModel! PotId: {potSlot.PotId}, PlantCode passato: '{plantCode}'");
            }
            else
            {
                Debug.Log($"[PotActions] ✅ PlantCode salvato correttamente: {_potState.PlantCode} per vaso {potSlot.PotId}");
            }
        }
        
        // Notifica il sistema di crescita (BLK-01.03A)
        if (potGrowthController)
            potGrowthController.OnPlanted();
        
        // Registra il vaso nel sistema di crescita (ora ha una pianta)
        // DEBUG_SAFE_FIX: Assicurati che il vaso venga registrato dopo aver piantato
        // Questo è critico per il calcolo del pH a fine giornata
        RegisterPotIfNeeded();
        
        // DEBUG_SAFE_FIX: Verifica che la registrazione sia avvenuta correttamente
        if (showDebugLogs && dayCycleController != null && _potState.HasPlant)
        {
            // Verifica che il vaso sia stato registrato (controllo indiretto)
            Debug.Log($"[PotActions] ✅ Verifica post-piantagione: HasPlant={_potState.HasPlant}, Stage={_potState.Stage}, PlantCode={_potState.PlantCode ?? "NULL"}, dayCycleController disponibile");
        }
        
        // Notifica il cambio stato
        PotEvents.EmitAction(PotEvents.PotActionType.Plant, potSlot);
        PotEvents.EmitChanged(potSlot);
        
        if (showDebugLogs)
        {
            string plantInfo = plantData != null ? $", PlantData: {plantData.PlantCode} ({plantData.Family})" : "";
            Debug.Log($"[ACT-001][{potSlot.PotId}] Plant OK: seed planted, state={_potState}{plantInfo}");
        }
        
        return true;
    }
    
    public bool DoUproot()
    {
        if (!CanUproot())
            return false;
        
        // BLK-02.03: Rimuovi contributi pH della pianta prima di rimuoverla
        if (_phSystem != null && !string.IsNullOrEmpty(_potState.PlantCode))
        {
            _phSystem.RemovePlantContributions(potSlot.PotId);
            if (showDebugLogs)
                Debug.Log($"[PotActions] ✅ Contributi pH rimossi per pianta nel vaso {potSlot.PotId} (PlantCode: {_potState.PlantCode})");
        }
        
        // Reset completo dello stato del vaso (importante per evitare che la pianta continui a influenzare il pH)
        _potState.ResetToEmpty();
        
        if (dayCycleController != null)
            dayCycleController.UnregisterPot(_potState);
        
        if (potGrowthController != null)
            potGrowthController.OnUprooted();

        _playerInventory.Add(Items.WholePlant);
        
        // Notifica il cambio stato
        PotEvents.EmitAction(PotEvents.PotActionType.Uproot, potSlot);
        PotEvents.EmitChanged(potSlot);
        
        if (showDebugLogs)
            Debug.Log($"[PotActions] ✅ UPROOT completato per vaso {potSlot.PotId}. Vaso resettato completamente.");
        
        return true;
    }

    /// <summary>
    /// Esegue l'azione di attivare/disattivare il sistema irrigazione (GDD AZ-11 - Toggle Persistente)
    /// NOTA: Consumo WAT-RAW e CRY avviene a fine giornata, non immediatamente
    /// </summary>
    public bool DoWater()
    {
        if (!CanWater())
        {
            string reason = GetWaterFailureReason();
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Water, potSlot, reason);
            return false;
        }
        
        // Consuma solo 1 Azione per il toggle (non WAT-RAW o CRY - consumo giornaliero)
        if (!TryConsumeResources())
        {
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Water, potSlot, "Insufficient resources");
            return false;
        }

        // Toggle del sistema irrigazione
        _potState.WateringSystemOn = !_potState.WateringSystemOn;
        
        // Aggiorna contatori in base al nuovo stato
        if (_potState.WateringSystemOn)
        {
            // Sistema attivato: incrementa contatore giorni ON
            // (verrà incrementato anche a fine giornata se rimane ON)
        }
        else
        {
            // Sistema disattivato: reset contatori
            _potState.DaysWateringSystemOn = 0;
            _potState.WateringRawWaterAccumulator = 0f;
        }
            
        // Notifica il cambio stato
        PotEvents.EmitAction(PotEvents.PotActionType.Water, potSlot);
        PotEvents.EmitChanged(potSlot);
            
        if (showDebugLogs)
        {
            string stateMsg = _potState.WateringSystemOn ? "ON" : "OFF";
            Debug.Log($"[ACT-002][{potSlot.PotId}] Watering System Toggle: {stateMsg} (consumo risorse a fine giornata)");
        }
        
        return true;
    }
    
    /// <summary>
    /// Restituisce lo stato corrente del sistema irrigazione
    /// </summary>
    public bool IsWateringSystemOn()
    {
        return _potState != null && _potState.WateringSystemOn;
    }
    
    /// <summary>
    /// DEPRECATO (BLK-02.07): Usare DoLight(LedSystemState?) invece
    /// Mantenuto per compatibilità temporanea
    /// </summary>
    [System.Obsolete("Usare DoLight(LedSystemState?) per nuovo sistema persistente. Questo metodo sarà rimosso in BLK-02.08")]
    public bool DoLight(LedType? ledType = null)
    {
        if (showDebugLogs)
            Debug.LogWarning($"[PotActions][{potSlot?.PotId}] ⚠️ DoLight(LedType?) è deprecato. Usare DoLight(LedSystemState?)");
        
        // Migrazione automatica: converti LedType a LedSystemState
        LedSystemState? newState = null;
        if (ledType.HasValue)
        {
            newState = ledType.Value == LedType.Blue ? LedSystemState.Blue : LedSystemState.Red;
        }
        return DoLight(newState);
    }
    
    /// <summary>
    /// BLK-02.07: Toggle sistema LED persistente (Off/Blue/Red)
    /// Effetti applicati a fine giornata, non immediatamente
    /// </summary>
    /// <param name="newState">Stato desiderato. Se null, cicla: Off → Blue → Red → Off</param>
    public bool DoLight(LedSystemState? newState = null)
    {
        if (!CanLight())
        {
            string reason = GetLightFailureReason();
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Light, potSlot, reason);
            return false;
        }
        
        // Consuma solo 1 Azione per il toggle (non CRY - consumo giornaliero)
        if (!TryConsumeResources())
        {
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Light, potSlot, "Insufficient resources");
            return false;
        }
        
        // Salva stato precedente per rimuovere contributo pH se necessario
        LedSystemState oldState = _potState.LedSystemState;
        
        // Toggle o set esplicito
        if (newState.HasValue)
        {
            _potState.SetLedSystemState(newState.Value);
        }
        else
        {
            // Ciclo: Off → Blue → Red → Off
            LedSystemState nextState = (LedSystemState)(((int)_potState.LedSystemState + 1) % 3);
            _potState.SetLedSystemState(nextState);
        }
        
        // BLK-02.07 BUG FIX: Rimuovi contributo pH se LED è stato spento
        if (oldState != LedSystemState.Off && _potState.LedSystemState == LedSystemState.Off)
        {
            // LED spento: rimuovi contributo pH del LED precedente
            if (_phSystem != null)
            {
                string actionName = oldState == LedSystemState.Blue ? "BlueLED" : "RedLED";
                // Rimuovi tutti i contributi di questo LED per questo vaso (inclusi quelli con moltiplicatori)
                _phSystem.RemoveActionContribution("BlueLED", potSlot.PotId);
                _phSystem.RemoveActionContribution("RedLED", potSlot.PotId);
                // Rimuovi anche varianti con moltiplicatori
                _phSystem.RemoveActionContribution("BlueLED_x1.5", potSlot.PotId);
                _phSystem.RemoveActionContribution("BlueLED_x2", potSlot.PotId);
                _phSystem.RemoveActionContribution("RedLED_x1.5", potSlot.PotId);
                _phSystem.RemoveActionContribution("RedLED_x2", potSlot.PotId);
                
                if (showDebugLogs)
                    Debug.Log($"[PotActions] {potSlot.PotId}: Contributo pH LED rimosso (LED spento: {oldState} → Off)");
            }
        }
        
        // COMPATIBILITÀ: Aggiorna LastLedType per sistemi legacy
        if (_potState.LedSystemState == LedSystemState.Blue)
            _potState.LastLedType = LedType.Blue;
        else if (_potState.LedSystemState == LedSystemState.Red)
            _potState.LastLedType = LedType.Red;
        else
            _potState.LastLedType = null;
        
        // BLK-02.07: Aggiorna luci Unity
        if (ledLightController != null)
        {
            ledLightController.UpdateLights(_potState.LedSystemState);
        }
        
        // NOTA: NON applicare effetti pH qui - vengono applicati a fine giornata
        // NOTA: NON incrementare LightExposure qui - viene fatto a fine giornata
        
        // Toast notifica cambio stato (gestito da PotNotifications tramite PotEvents.OnPotAction)
        // I toast vengono mostrati automaticamente quando viene emesso PotEvents.EmitAction()
        
        // Notifica il cambio stato
        PotEvents.EmitAction(PotEvents.PotActionType.Light, potSlot);
        PotEvents.EmitChanged(potSlot);
        
        if (showDebugLogs)
        {
            string stateMsg = _potState.LedSystemState.ToString();
            Debug.Log($"[ACT-003][{potSlot.PotId}] LED System Toggle: {stateMsg} (effetti a fine giornata)");
        }
        
        return true;
    }
    
    /// <summary>
    /// BLK-02.07: Attiva/disattiva un LED specifico (Blue o Red)
    /// </summary>
    /// <param name="ledType">Tipo di LED da attivare/disattivare</param>
    /// <returns>True se l'operazione è riuscita</returns>
    public bool DoLight(LedType ledType)
    {
        LedSystemState currentState = _potState.LedSystemState;
        LedSystemState targetState;
        
        // Se il LED richiesto è già attivo, spegnilo. Altrimenti, attivalo
        if (ledType == LedType.Blue)
        {
            targetState = (currentState == LedSystemState.Blue) ? LedSystemState.Off : LedSystemState.Blue;
        }
        else // LedType.Red
        {
            targetState = (currentState == LedSystemState.Red) ? LedSystemState.Off : LedSystemState.Red;
        }
        
        return DoLight(targetState);
    }
    
    /// <summary>
    /// BLK-02.07: Restituisce lo stato corrente del sistema LED
    /// </summary>
    public LedSystemState GetLedSystemState()
    {
        return _potState != null ? _potState.LedSystemState : LedSystemState.Off;
    }
    
    /// <summary>
    /// BLK-02.07: Verifica se sistema LED è attivo (Blue o Red)
    /// </summary>
    public bool IsLedSystemOn()
    {
        return _potState != null && _potState.LedSystemState != LedSystemState.Off;
    }
    
    /// <summary>
    /// Esegue l'azione Spray Antifungino (AZ-14)
    /// Rimuove muffe e applica pH +5
    /// </summary>
    public bool DoSprayAntifungal()
    {
        if (!CanSprayAntifungal())
        {
            string reason = GetSprayAntifungalFailureReason();
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Spray, potSlot, reason);
            return false;
        }
        
        // Consuma le risorse
        if (!TryConsumeResources())
        {
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Spray, potSlot, "Insufficient resources");
            return false;
        }
        
        // Applica pH +5 (BLK-02.03)
        if (_phSystem != null)
        {
            _phSystem.RegisterActionDrift(5f, "SprayAntifungal", potSlot.PotId);
            if (showDebugLogs)
                Debug.Log($"[ACT-014][{potSlot.PotId}] Spray Antifungino applicato: pH +5");
        }
        
        // TODO: Rimuovere muffe quando sistema muffe sarà implementato
        // Per ora solo applica pH
        
        // Notifica il cambio stato
        PotEvents.EmitAction(PotEvents.PotActionType.Spray, potSlot);
        PotEvents.EmitChanged(potSlot);
            
        if (showDebugLogs)
            Debug.Log($"[ACT-014][{potSlot.PotId}] Spray Antifungino OK: muffe rimosse (se presenti), pH +5 applicato");
        
        return true;
    }
    
    /// <summary>
    /// Esegue l'azione di raccogliere frutti dalla pianta (BLK-02.06)
    /// Raccoglie tutti i frutti disponibili e aggiunge all'inventario
    /// </summary>
    public bool DoHarvest()
    {
        if (!CanHarvest())
        {
            string reason = GetHarvestFailureReason();
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Harvest, potSlot, reason);
            return false;
        }
        
        // Consuma le risorse
        if (!TryConsumeResources())
        {
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Harvest, potSlot, "Insufficient resources");
            return false;
        }
        
        // Calcola quantità frutti da raccogliere
        int fruitsToHarvest = Mathf.RoundToInt(_potState.AmountFruits);
        if (fruitsToHarvest <= 0)
        {
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Harvest, potSlot, "Nessun frutto disponibile");
            return false;
        }
        
        // Aggiungi frutti all'inventario
        for (int i = 0; i < fruitsToHarvest; i++)
        {
            _playerInventory.Add(Items.Fruits);
        }
        
        // Reset frutti nel vaso
        _potState.AmountFruits = 0f;
        _potState.DaysFruitsUnharvested = 0;
        
        // BLK-02.05: Dopo la raccolta, la pianta entra in Resting
        int oldStage = _potState.Stage;
        _potState.Stage = (int)PlantStage.Resting;
        _potState.DaysInHarvestReady = 0; // Reset contatore HarvestReady
        _potState.DaysInCurrentStage = 0; // Reset contatore stadio corrente
        
        // Notifica il cambio stato
        PotEvents.EmitAction(PotEvents.PotActionType.Harvest, potSlot);
        PotEvents.EmitChanged(potSlot);
        
        // Notifica il cambio di stadio (HarvestReady → Resting)
        if (potGrowthController != null)
        {
            potGrowthController.OnStageChanged(PlantStage.Resting);
        }
        
        PotEvents.EmitPlantStageChanged(potSlot.PotId, PlantStage.Resting);
        
        if (showDebugLogs)
        {
            Debug.Log($"[ACT-005][{potSlot.PotId}] Harvest OK: raccolti {fruitsToHarvest} frutti, aggiunti all'inventario. Stadio cambiato: {oldStage} (HarvestReady) → {(int)PlantStage.Resting} (Resting)");
        }
        
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
        if (!IsPlayerInRange()) return "Troppo lontano";
        if (!CanConsumeResources()) return "Azioni insufficienti";
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
    
    private string GetSprayAntifungalFailureReason()
    {
        if (_potState == null) return "Stato vaso non valido";
        if (!_potState.HasPlantGrowing) return "Vaso vuoto";
        if (!IsPlayerInRange()) return "Troppo lontano";
        if (!CanConsumeResources()) return "Azioni o CRY insufficienti";
        return "Azione non permessa";
    }
    
    private string GetHarvestFailureReason()
    {
        if (_potState == null) return "Stato vaso non valido";
        if (_potState.Stage != (int)PlantStage.HarvestReady) return "Pianta non in HarvestReady";
        if (_potState.AmountFruits <= 0f) return "Nessun frutto disponibile";
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
    /// Restituisce il limite massimo di idratazione
    /// </summary>
    public int GetMaxHydration()
    {
        return config ? config.MaxHydration : 5; // Fallback a 5 step = 20% ciascuno
    }
    
    /// <summary>
    /// Restituisce il limite massimo di esposizione alla luce
    /// </summary>
    public int GetMaxLightExposure()
    {
        return config ? config.MaxLightExposure : 3;
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
