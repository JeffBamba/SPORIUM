using System;
using System.Linq;
using System.IO;
using _Project.Sporae.Core;
using UnityEngine;
using Sporae.Dome.PotSystem.Growth;
using Sporae.Dome.PotSystem.Fertilizer;
using Sporae.Dome.PotSystem.Pruning;
using Sporae.Dome.PotSystem.Level;
using Sporae.Dome.PotSystem.Mold;
using Sporae.Dome.PotSystem.Condition;
using _Project;
using Sporae.DevTools;

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
    
    // DEBUG_SAFE_FIX: Guard per prevenire chiamate multiple nello stesso frame
    private bool _isPlantingInProgress = false;
    private bool _isLightingInProgress = false;
    private bool _isWateringInProgress = false;
    private bool _isSprayingInProgress = false;
    private bool _isHarvestingInProgress = false;
    private bool _isUprootingInProgress = false;

    // Terminal V3 / Automation: quando true, bypassa range check e consumi (AP + item).
    // Serve per esecuzione scenica ritardata dopo conferma dal terminale.
    private int _automationContextDepth = 0;
    private bool IsAutomationContext => _automationContextDepth > 0;

    private sealed class AutomationScope : System.IDisposable
    {
        private readonly PotActions _owner;
        public AutomationScope(PotActions owner) => _owner = owner;
        public void Dispose()
        {
            if (_owner == null) return;
            _owner._automationContextDepth = Mathf.Max(0, _owner._automationContextDepth - 1);
        }
    }

    /// <summary>
    /// Abilita contesto automazione: no range checks e no consumi (AP/item) durante l'esecuzione.
    /// Usare con using(...) nel runner.
    /// </summary>
    public System.IDisposable BeginAutomationContext()
    {
        _automationContextDepth++;
        return new AutomationScope(this);
    }
    
    // Proprietà pubbliche
    public PotSlot PotSlot => potSlot;
    public PotStateModel PotState => _potState;
    public bool HasPlant => _potState?.HasPlant ?? false;
    
    private bool IsDead()
    {
        if (_potState == null) return false;
        return (Sporae.Dome.PotSystem.Condition.PlantCondition)_potState.ConditionLabel ==
               Sporae.Dome.PotSystem.Condition.PlantCondition.Morta;
    }
    
    private void Awake()
    {
        _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
        
        // Fallback: carica PotSystemConfig se non assegnato
        if (config == null)
        {
            // Carica esplicitamente da Resources/Configs/ per evitare conflitti con file in altre cartelle
            config = Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");
            if (config == null)
            {
                // Se non trovato, cerca tutti i configs ma preferisci quello con MaxHydration corretto
                var allConfigs = Resources.LoadAll<PotSystemConfig>("Configs");
                if (allConfigs != null && allConfigs.Length > 0)
                {
                    // Preferisci config con MaxHydration != 4 (vecchio sistema)
                    foreach (var cfg in allConfigs)
                    {
                        if (cfg.MaxHydration != 4)
                        {
                            config = cfg;
                            break;
                        }
                    }
                    // Se tutti hanno MaxHydration=4, prendi il primo
                    if (config == null)
                        config = allConfigs[0];
                }
            }
        }
        
        // BUG FIX: Verifica che MaxHydration sia corretto (non 4 o 5 del vecchio sistema)
        if (config != null && config.MaxHydration <= 5)
        {
            if (showDebugLogs)
                SporiumLogger.LogInfo(LogCategory.Pot, "Config ha MaxHydration<=5 (vecchio sistema). Forzo ricaricamento da Resources...");
            // Forza ricaricamento per ottenere il valore aggiornato
            Resources.UnloadAsset(config);
            config = Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");
            if (config != null && showDebugLogs)
            {
                if (config.MaxHydration <= 5)
                    SporiumLogger.LogWarning(LogCategory.Pot, $"Config ricaricato ma MaxHydration è ancora <=5. Verifica che il file in Resources/Configs/PotSystemConfig.asset abbia MaxHydration corretto.");
                else
                    SporiumLogger.LogInfo(LogCategory.Pot, $"Config ricaricato: MaxHydration={config.MaxHydration}");
            }
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
        if (_gameManager == null)
        {
            SporiumLogger.LogError(LogCategory.Pot, "GameManager non trovato! Tentativo di recupero ritardato...");
            StartCoroutine(WaitForGameManager());
            return;
        }
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
            SporiumLogger.LogInfo(LogCategory.Pot, $"Inizializzato per {potSlot?.PotId ?? "vaso sconosciuto"}");
        
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
                SporiumLogger.LogWarning(LogCategory.Pot, $"DayCycleController non trovato per {potSlot?.PotId}. Il vaso non verrà registrato.");
                return;
            }
        }
        
        // Registra se ha già una pianta (per piante esistenti caricate)
        if (_potState.HasPlant)
        {
            dayCycleController.RegisterPot(_potState);
            if (showDebugLogs)
            {
                SporiumLogger.LogInfo(LogCategory.Pot, $"Vaso {potSlot?.PotId} con pianta esistente registrato nel DayCycleController (Stage: {_potState.Stage}, PlantCode: {_potState.PlantCode ?? "NULL"})");
            }
        }
        else if (showDebugLogs)
        {
            SporiumLogger.LogInfo(LogCategory.Pot, $"Vaso {potSlot?.PotId} vuoto, registrazione quando si pianta un seme");
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
                SporiumLogger.LogInfo(LogCategory.Pot, $"PhSystem trovato per {potSlot?.PotId}");
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
                SporiumLogger.LogInfo(LogCategory.Pot, $"PhSystem registrato, collegato a {potSlot?.PotId}");
        }
        
        // BUG FIX: Se GameManager viene registrato, recuperalo
        if (service is GameManager && _gameManager == null)
        {
            _gameManager = service as GameManager;
            _playerInventory = _gameManager.PlayerInventory;
            if (showDebugLogs)
                SporiumLogger.LogInfo(LogCategory.Core, $"GameManager recuperato per {potSlot?.PotId}");
        }
    }
    
    /// <summary>
    /// BUG FIX: Coroutine per attendere che GameManager sia disponibile
    /// </summary>
    private System.Collections.IEnumerator WaitForGameManager()
    {
        int maxAttempts = 30; // 30 frame = ~0.5 secondi a 60fps
        int attempts = 0;
        
        while (_gameManager == null && attempts < maxAttempts)
        {
            yield return null;
            _gameManager = FindObjectOfType<GameManager>();
            attempts++;
        }
        
        if (_gameManager != null)
        {
            _playerInventory = _gameManager.PlayerInventory;
            if (showDebugLogs)
                SporiumLogger.LogInfo(LogCategory.Core, $"GameManager recuperato dopo {attempts} tentativi per {potSlot?.PotId}");
        }
        else
        {
            SporiumLogger.LogError(LogCategory.Core, $"GameManager non trovato dopo {maxAttempts} tentativi! Il vaso potrebbe non funzionare correttamente.");
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
                SporiumLogger.LogDebug(LogCategory.Pot, $"Stato esistente trovato per {potSlot.PotId}: {_potState}");
        }
            
        // Crea nuovo solo se non esiste
        if (_potState != null)
            return;
        
        _potState = new PotStateModel(potSlot.PotId);
        if (showDebugLogs)
            SporiumLogger.LogDebug(LogCategory.Pot, $"Nuovo stato creato per {potSlot.PotId}: {_potState}");
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
        if (IsDead())
            return false;
        
        bool
            isEmpty = _potState.IsEmpty,
            // Terminal/Automation: il seme viene consumato alla conferma della queue, quindi qui non dobbiamo
            // richiedere che l'inventario contenga ancora un seme al momento dell'esecuzione ritardata.
            hasSeed = IsAutomationContext || IsPlayerHasSeed(),
            inRange = IsAutomationContext || IsPlayerInRange(),
            hasResources = IsAutomationContext || CanConsumeResources(),
            // Plant-day gating ha senso per l'interazione live; in automazione non deve bloccare la sequenza.
            notWateredOnThisDay = IsAutomationContext || _potState.LastWateredDay != _dayCycleSystem.CurrentDay;
        
        if (showDebugLogs)
            SporiumLogger.LogDebug(LogCategory.Pot, $"[{potSlot?.PotId}] CanPlant: Empty={isEmpty}, Seed={hasSeed}, Range={inRange}, Resources={hasResources}");
        
        return isEmpty && hasSeed && inRange && hasResources && notWateredOnThisDay;
    }

    public bool CanUproot()
    {
        if (_potState == null)
            return false;
        if (IsDead())
            return true;
        return _potState.HasPlantGrowing;
    }
    
    /// <summary>
    /// Verifica se è possibile attivare/disattivare il sistema irrigazione (GDD AZ-11 - Toggle Persistente)
    /// BUG1 FIX: Spegnere l'irrigazione è sempre permesso (non richiede azioni), accendere richiede azioni
    /// </summary>
    public bool CanWater()
    {
        if (_potState == null) 
            return false;
        if (IsDead())
            return false;
        
        // Precondizioni base: vaso ha pianta, player in range
        bool 
            hasPlant = _potState.HasPlantGrowing,
            inRange = IsPlayerInRange();
        
        if (!hasPlant || !inRange)
        {
            if (showDebugLogs)
                SporiumLogger.LogDebug(LogCategory.Pot, $"[{potSlot?.PotId}] CanWater: Plant={hasPlant}, Range={inRange} - BLOCKED");
            return false;
        }
        
        // BUG1 FIX: Se stiamo spegnendo (WateringSystemOn=true), non richiediamo azioni
        // Se stiamo accendendo (WateringSystemOn=false), richiediamo azioni
        if (_potState.WateringSystemOn)
        {
            // Spegnere: sempre permesso (non consuma azioni)
            if (showDebugLogs)
                SporiumLogger.LogDebug(LogCategory.Pot, $"[{potSlot?.PotId}] CanWater (Turn OFF): Plant={hasPlant}, Range={inRange} - ALLOWED (no resources needed)");
            return true;
        }
        else
        {
            // Accendere: richiede azioni
            bool hasResources = CanConsumeResources();
            if (showDebugLogs)
                SporiumLogger.LogDebug(LogCategory.Pot, $"[{potSlot?.PotId}] CanWater (Turn ON): Plant={hasPlant}, Range={inRange}, Resources={hasResources}");
            return hasResources;
        }
    }
    
    /// <summary>
    /// Verifica se è possibile illuminare la pianta (BLK-02.07: toggle LED persistente)
    /// </summary>
    public bool CanLight()
    {
        if (_potState == null)
            return false;
        if (IsDead())
            return false;
        
        // Precondizioni: vaso ha pianta, player in range, risorse sufficienti
        // MODIFICA: LED può essere acceso anche subito dopo aver piantato (stadio Seed)
        // NOTA: BLK-02.07 - Non verifica più lightNotMax (LED è toggle persistente, non incremento immediato)
        bool
            hasPlant = _potState.HasPlantGrowing,
            inRange = IsPlayerInRange(),
            hasResources = CanConsumeResources();
        
        if (showDebugLogs)
            SporiumLogger.LogDebug(LogCategory.Pot, $"[{potSlot?.PotId}] CanLight: Plant={hasPlant}, Range={inRange}, Resources={hasResources}, CurrentState={_potState.LedSystemState}, Stage={(PlantStage)_potState.Stage}");
        
        return hasPlant && inRange && hasResources;
    }
    
    /// <summary>
    /// Verifica se è possibile applicare Spray Antifungino (AZ-14)
    /// </summary>
    public bool CanSprayAntifungal()
    {
        // Retrocompat: stesso gating del nuovo sistema additivi
        return CanApplyAdditive();
    }

    /// <summary>
    /// Verifica se è possibile applicare un Additivo (sistema additivi pH).
    /// Precondizioni: vaso ha pianta, player in range, risorse sufficienti.
    /// </summary>
    public bool CanApplyAdditive()
    {
        if (_potState == null)
            return false;
        if (IsDead())
            return false;
        
        bool
            hasPlant = _potState.HasPlantGrowing,
            inRange = IsPlayerInRange(),
            hasResources = CanConsumeResources();
        
        if (showDebugLogs)
            SporiumLogger.LogDebug(LogCategory.Pot, $"[{potSlot?.PotId}] CanApplyAdditive: Plant={hasPlant}, Range={inRange}, Resources={hasResources}");
        
        return hasPlant && inRange && hasResources;
    }
    
    /// <summary>
    /// Verifica se è possibile raccogliere frutti dalla pianta
    /// </summary>
    public bool CanHarvest()
    {
        if (_potState == null)
            return false;
        if (IsDead())
            return false;
        
        // Precondizioni: vaso ha pianta in HarvestReady, ci sono frutti disponibili, player in range (o automation), risorse sufficienti (o automation)
        float amountFruitsValue = _potState?.AmountFruits ?? 0f;
        int stageValue = _potState?.Stage ?? -1;
        bool
            isHarvestReady = _potState.Stage == (int)PlantStage.HarvestReady,
            hasFruits = amountFruitsValue > 0f,
            inRange = IsAutomationContext || IsPlayerInRange(),
            hasResources = IsAutomationContext || CanConsumeResources();
        
        // #region agent log
        var logDataHarvest = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"H1\",\"location\":\"PotActions.CanHarvest\",\"message\":\"Verifica precondizioni harvest\",\"data\":{{\"potId\":\"{potSlot?.PotId}\",\"isAutomationContext\":{IsAutomationContext},\"isHarvestReady\":{isHarvestReady},\"hasFruits\":{hasFruits},\"inRange\":{inRange},\"hasResources\":{hasResources},\"stage\":{stageValue},\"stageExpected\":{(int)PlantStage.HarvestReady},\"amountFruits\":{amountFruitsValue},\"amountFruitsRaw\":\"{_potState?.AmountFruits}\",\"hasPlant\":{_potState?.HasPlant},\"isDead\":{IsDead()}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
        System.IO.File.AppendAllText(@"d:\Sporae_Build_Beta\.cursor\debug.log", logDataHarvest);
        // #endregion
        
        if (showDebugLogs)
            SporiumLogger.LogDebug(LogCategory.Pot, $"[{potSlot?.PotId}] CanHarvest: HarvestReady={isHarvestReady}, Fruits={hasFruits}, Range={inRange}, Resources={hasResources}, Automation={IsAutomationContext}");
        
        return isHarvestReady && hasFruits && inRange && hasResources;
    }
    
    /// <summary>
    /// BLK-03.01-T1: Verifica se è possibile applicare fertilizzante
    /// </summary>
    public bool CanFertilize()
    {
        if (_potState == null)
            return false;
        if (IsDead())
            return false;
        
        // Precondizioni: vaso ha pianta, player in range, risorse sufficienti
        bool
            hasPlant = _potState.HasPlantGrowing,
            inRange = IsPlayerInRange(),
            hasResources = CanConsumeResources();
        
        if (showDebugLogs)
            SporiumLogger.LogDebug(LogCategory.Pot, $"[{potSlot?.PotId}] CanFertilize: Plant={hasPlant}, Range={inRange}, Resources={hasResources}");
        
        return hasPlant && inRange && hasResources;
    }
    
    /// <summary>
    /// AZ-13: Verifica se è possibile eseguire potatura
    /// </summary>
    public bool CanPruning()
    {
        if (_potState == null)
            return false;
        if (IsDead())
            return false;
        
        // Precondizioni: vaso ha pianta, player in range, risorse sufficienti
        bool
            hasPlant = _potState.HasPlantGrowing,
            inRange = IsPlayerInRange(),
            hasResources = CanConsumeResources();
        
        if (showDebugLogs)
            SporiumLogger.LogDebug(LogCategory.Pot, $"[{potSlot?.PotId}] CanPruning: Plant={hasPlant}, Range={inRange}, Resources={hasResources}");
        
        return hasPlant && inRange && hasResources;
    }
    
    /// <summary>
    /// AZ-13: Verifica se è disponibile STR-004 (Spray Antifungino) in inventario
    /// </summary>
    public bool HasSprayAntifungal()
    {
        if (_playerInventory == null)
            return false;
        
        // Verifica presenza STR-004 nell'inventario
        bool hasItem = _playerInventory.Has(Items.SprayAntifungal, 1);
        return hasItem;
    }

    /// <summary>
    /// Verifica se è disponibile almeno un additivo (Basic o Acid) in inventario.
    /// </summary>
    public bool HasAdditive()
    {
        if (_playerInventory == null)
            return false;

        return _playerInventory.Has(Items.AdditiveBasic, 1) || _playerInventory.Has(Items.AdditiveAcid, 1);
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

        // In automation, la spesa item avviene alla conferma del terminale.
        if (IsAutomationContext)
            return true;

        return _playerInventory.Consume(seedTypeId);
    }

    /// <summary>
    /// Trova il pot più vicino a questo (escludendo se stesso) e restituisce il suo PotStateModel.
    /// </summary>
    private PotStateModel FindNearestPot()
    {
        if (potSlot == null)
            return null;

        PotSlot[] allPots = FindObjectsOfType<PotSlot>();
        if (allPots == null || allPots.Length == 0)
            return null;

        float bestDist = float.MaxValue;
        PotStateModel best = null;

        Vector3 myPos = potSlot.transform.position;
        foreach (var p in allPots)
        {
            if (p == null || p == potSlot)
                continue;

            float d = Vector3.Distance(myPos, p.transform.position);
            if (d >= bestDist)
                continue;

            var ps = p.PotActions != null ? p.PotActions.PotState : null;
            if (ps == null)
                continue;

            bestDist = d;
            best = ps;
        }

        return best;
    }
    
    /// <summary>
    /// Esegue l'azione di piantare un seme
    /// </summary>
    /// <param name="seedTypeId">TypeId del seme da piantare. Se null, cerca automaticamente il primo seme disponibile.</param>
    public bool DoPlant(string seedTypeId = null, bool irrigate = false)
    {
        // DEBUG_SAFE_FIX: Guard per prevenire chiamate multiple nello stesso frame
        if (_isPlantingInProgress)
        {
            SporiumLogger.LogWarning(LogCategory.Pot, $"[{potSlot?.PotId}] DoPlant già in esecuzione! Ignorando chiamata duplicata per seedTypeId: {seedTypeId}");
            return false;
        }
        
        _isPlantingInProgress = true;
        
        try
        {
            if (!CanPlant())
            {
                string reason = GetPlantFailureReason();
                PotEvents.EmitActionFailed(PotEvents.PotActionType.Plant, potSlot, reason);
                return false;
            }
            
            // DEBUG_SAFE_FIX: Log prima del consumo risorse per tracciare chiamate multiple
            int actionsBefore = _gameManager?.ActionsLeft ?? 0;
            SporiumLogger.LogDebug(LogCategory.Pot, $"[{potSlot?.PotId}] DoPlant chiamato - Azioni prima: {actionsBefore}, seedTypeId: {seedTypeId}, irrigate: {irrigate}");

            // Se seedTypeId non specificato, cerca automaticamente (compatibilità retroattiva)
            if (string.IsNullOrEmpty(seedTypeId))
            {
                seedTypeId = FindSeedTypeId();
                if (string.IsNullOrEmpty(seedTypeId))
                {
                    SporiumLogger.LogError(LogCategory.Inventory, "Impossible to find seed in inventory");
                    PotEvents.EmitActionFailed(PotEvents.PotActionType.Plant, potSlot, "Nessun seme disponibile");
                    return false;
                }
            }
            
            // Verifica che il seme specificato esista nell'inventario
            if (!IsAutomationContext && !_playerInventory.Has(seedTypeId))
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Seme '{seedTypeId}' non disponibile nell'inventario");
                PotEvents.EmitActionFailed(PotEvents.PotActionType.Plant, potSlot, $"Seme '{seedTypeId}' non disponibile");
                return false;
            }

            // Se irrigate=true, richiede 2x costo azioni e deve essere verificato prima di consumare.
            int baseActionsCost = GetActionsCost();
            int totalActionsCost = irrigate ? baseActionsCost * 2 : baseActionsCost;

            // Terminal/Automation: gli AP vengono già scalati dal runner alla conferma batch.
            // Qui non dobbiamo bloccare l'esecuzione ritardata in base ad ActionsLeft corrente.
            if (_gameManager == null || (!IsAutomationContext && _gameManager.ActionsLeft < totalActionsCost))
            {
                PotEvents.EmitActionFailed(PotEvents.PotActionType.Plant, potSlot, "Azioni insufficienti");
                return false;
            }

            // Consuma azioni (in un'unica spesa) per evitare doppi side-effect.
            // In automation, il costo AP viene scalato alla conferma del terminale.
            if (!IsAutomationContext)
            {
                bool spendOk = _gameManager.TrySpendAction(totalActionsCost);
                if (!spendOk)
                {
                    PotEvents.EmitActionFailed(PotEvents.PotActionType.Plant, potSlot, "Insufficient resources");
                    return false;
                }
            }

            int actionsAfter = _gameManager?.ActionsLeft ?? 0;
            SporiumLogger.LogDebug(LogCategory.Pot, $"[{potSlot?.PotId}] DoPlant - Azioni dopo consumo: {actionsAfter} (consumate: {actionsBefore - actionsAfter})");
            
            // Cerca PlantData dal database usando il TypeId del seme
            PlantData plantData = PlantDatabase.Instance?.GetPlantDataBySeedTypeId(seedTypeId);
            string plantCode = plantData?.PlantCode;
            
            if (plantData == null)
            {
                if (showDebugLogs)
                {
                    SporiumLogger.LogWarning(LogCategory.Pot, $"Nessun PlantData trovato per seme TypeId '{seedTypeId}'. La pianta non avrà drift pH.");
                }
                // IMPORTANTE: Anche se PlantData non è trovato, piantiamo comunque il seme
                // ma senza PlantCode, quindi non avrà drift pH
            }
            else
            {
                if (showDebugLogs)
                {
                    SporiumLogger.LogInfo(LogCategory.Pot, $"PlantData trovato: {plantData.PlantCode} ({plantData.Family}), drift pH: {plantData.DailyPhDrift}/giorno");
                }
                
                // Verifica che PlantCode non sia null o vuoto
                if (string.IsNullOrEmpty(plantCode))
                {
                    SporiumLogger.LogError(LogCategory.Pot, $"PlantData '{plantData.name}' ha PlantCode NULL o vuoto! La pianta non avrà drift pH.");
                }
            }
            
            // Consuma il seme dall'inventario (skip in automation: già consumato in conferma terminale)
            if (!IsAutomationContext)
            {
                if (!_playerInventory.Consume(seedTypeId))
                {
                    SporiumLogger.LogError(LogCategory.Inventory, "Impossible to consume seed");
                    return false;
                }
            }
            
            // Aggiorna lo stato del vaso (Stage 1 = Seed) con PlantCode
            _potState.PlantSeed(_dayCycleSystem.CurrentDay, plantCode);

            // Se richiesto, irrigazione immediata: imposta hydration al 40% del max.
            if (irrigate)
            {
                int maxHydration = GetMaxHydration();
                int targetHydration = Mathf.Clamp(Mathf.RoundToInt(maxHydration * 0.4f), 0, maxHydration);
                _potState.Hydration = targetHydration;
            }
            
            // DEBUG: Verifica che PlantCode sia stato salvato correttamente
            if (showDebugLogs)
            {
                if (string.IsNullOrEmpty(_potState.PlantCode))
                {
                    SporiumLogger.LogWarning(LogCategory.Pot, $"PlantCode NON salvato correttamente nel PotStateModel! PotId: {potSlot.PotId}, PlantCode passato: '{plantCode}'");
                }
                else
                {
                    SporiumLogger.LogInfo(LogCategory.Pot, $"PlantCode salvato correttamente: {_potState.PlantCode} per vaso {potSlot.PotId}");
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
                SporiumLogger.LogDebug(LogCategory.Pot, $"Verifica post-piantagione: HasPlant={_potState.HasPlant}, Stage={_potState.Stage}, PlantCode={_potState.PlantCode ?? "NULL"}, dayCycleController disponibile");
            }
        
            // Notifica il cambio stato
            PotEvents.EmitAction(PotEvents.PotActionType.Plant, potSlot);
            PotEvents.EmitChanged(potSlot);
            
            if (showDebugLogs)
            {
                string plantInfo = plantData != null ? $", PlantData: {plantData.PlantCode} ({plantData.Family})" : "";
                string irrigateInfo = irrigate ? ", irrigated=true" : "";
                SporiumLogger.LogInfo(LogCategory.Pot, $"[ACT-001][{potSlot.PotId}] Plant OK: seed planted{irrigateInfo}, state={_potState}{plantInfo}");
            }
            
            return true;
        }
        finally
        {
            // Reset del flag nel prossimo frame per permettere nuove chiamate
            StartCoroutine(ResetPlantingFlag());
        }
    }
    
    /// <summary>
    /// Reset del flag di planting nel prossimo frame
    /// </summary>
    private System.Collections.IEnumerator ResetPlantingFlag()
    {
        yield return null; // Aspetta un frame
        _isPlantingInProgress = false;
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
                SporiumLogger.LogInfo(LogCategory.Ph, $"Contributi pH rimossi per pianta nel vaso {potSlot.PotId} (PlantCode: {_potState.PlantCode})");
        }
        
        // Reset completo dello stato del vaso (importante per evitare che la pianta continui a influenzare il pH)
        _potState.ResetToEmpty();

        // BLK-02.07: Spegni subito le luci 2D dei LED (altrimenti rimangono fisicamente accese)
        if (ledLightController != null)
        {
            ledLightController.UpdateLights(LedSystemState.Off);
        }
        
        if (dayCycleController != null)
            dayCycleController.UnregisterPot(_potState);
        
        if (potGrowthController != null)
            potGrowthController.OnUprooted();

        _playerInventory.Add(Items.WholePlant);
        
        // Notifica il cambio stato
        PotEvents.EmitAction(PotEvents.PotActionType.Uproot, potSlot);
        PotEvents.EmitChanged(potSlot);
        
        if (showDebugLogs)
            SporiumLogger.LogInfo(LogCategory.Pot, $"UPROOT completato per vaso {potSlot.PotId}. Vaso resettato completamente.");
        
        return true;
    }

    /// <summary>
    /// Esegue l'azione di attivare/disattivare il sistema irrigazione (GDD AZ-11 - Toggle Persistente)
    /// NOTA: Consumo WAT-RAW e CRY avviene a fine giornata, non immediatamente
    /// </summary>
    public bool DoWater()
    {
        // DEBUG_SAFE_FIX: Guard per prevenire chiamate multiple nello stesso frame
        if (_isWateringInProgress)
        {
            SporiumLogger.LogWarning(LogCategory.Pot, $"[{potSlot?.PotId}] DoWater già in esecuzione! Ignorando chiamata duplicata.");
            return false;
        }
        
        _isWateringInProgress = true;
        
        try
        {
            // BUG1 FIX: Determina se stiamo accendendo o spegnendo PRIMA di qualsiasi controllo
            bool wasOn = _potState.WateringSystemOn;
            bool isTurningOn = !wasOn;
            
            if (!CanWater())
            {
                string reason = GetWaterFailureReason();
                PotEvents.EmitActionFailed(PotEvents.PotActionType.Water, potSlot, reason);
                return false;
            }
            
            // DEBUG_SAFE_FIX: Log prima del consumo risorse
            int actionsBefore = _gameManager?.ActionsLeft ?? 0;
            SporiumLogger.LogDebug(LogCategory.Pot, $"[{potSlot?.PotId}] DoWater chiamato - Azioni prima: {actionsBefore}, TurningOn: {isTurningOn}");
            
            // DESIGN UPDATE (Terminal V3): Qualsiasi azione costa 1 AP, incluso spegnere irrigazione.
            // DEBUG_SAFE_FIX: Manteniamo i log espliciti per verificare costo ON/OFF.
            if (!TryConsumeResources())
            {
                PotEvents.EmitActionFailed(PotEvents.PotActionType.Water, potSlot, "Insufficient resources");
                return false;
            }
            
            int actionsAfter = _gameManager?.ActionsLeft ?? 0;
            SporiumLogger.LogDebug(LogCategory.Pot, $"[{potSlot?.PotId}] DoWater - Azioni dopo consumo: {actionsAfter} (consumate: {actionsBefore - actionsAfter}), TurningOn: {isTurningOn}");
            
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
                SporiumLogger.LogInfo(LogCategory.Pot, $"[ACT-002][{potSlot.PotId}] Watering System Toggle: {stateMsg} (consumo risorse a fine giornata)");
            }
            
            return true;
        }
        finally
        {
            // Reset del flag nel prossimo frame per permettere nuove chiamate
            StartCoroutine(ResetWateringFlag());
        }
    }
    
    /// <summary>
    /// Reset del flag di watering nel prossimo frame
    /// </summary>
    private System.Collections.IEnumerator ResetWateringFlag()
    {
        yield return null; // Aspetta un frame
        _isWateringInProgress = false;
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
            SporiumLogger.LogWarning(LogCategory.Pot, $"[{potSlot?.PotId}] DoLight(LedType?) è deprecato. Usare DoLight(LedSystemState?)");
        
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
        // DEBUG_SAFE_FIX: Guard per prevenire chiamate multiple nello stesso frame
        if (_isLightingInProgress)
        {
            SporiumLogger.LogWarning(LogCategory.Pot, $"[{potSlot?.PotId}] DoLight già in esecuzione! Ignorando chiamata duplicata.");
            return false;
        }
        
        _isLightingInProgress = true;
        
        try
        {
            if (!CanLight())
            {
                string reason = GetLightFailureReason();
                PotEvents.EmitActionFailed(PotEvents.PotActionType.Light, potSlot, reason);
                return false;
            }
            
            // DEBUG_SAFE_FIX: Log prima del consumo risorse per tracciare chiamate multiple
            int actionsBefore = _gameManager?.ActionsLeft ?? 0;
            SporiumLogger.LogDebug(LogCategory.Pot, $"[{potSlot?.PotId}] DoLight chiamato - Azioni prima: {actionsBefore}, newState: {newState}");
            
            // Consuma solo 1 Azione per il toggle (non CRY - consumo giornaliero)
            if (!TryConsumeResources())
            {
                PotEvents.EmitActionFailed(PotEvents.PotActionType.Light, potSlot, "Insufficient resources");
                return false;
            }
            
            int actionsAfter = _gameManager?.ActionsLeft ?? 0;
            SporiumLogger.LogDebug(LogCategory.Pot, $"[{potSlot?.PotId}] DoLight - Azioni dopo consumo: {actionsAfter} (consumate: {actionsBefore - actionsAfter})");
            
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
                        SporiumLogger.LogDebug(LogCategory.Ph, $"{potSlot.PotId}: Contributo pH LED rimosso (LED spento: {oldState} → Off)");
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
                SporiumLogger.LogInfo(LogCategory.Pot, $"[ACT-003][{potSlot.PotId}] LED System Toggle: {stateMsg} (effetti a fine giornata)");
            }
            
            return true;
        }
        finally
        {
            // Reset del flag nel prossimo frame per permettere nuove chiamate
            StartCoroutine(ResetLightingFlag());
        }
    }
    
    /// <summary>
    /// Reset del flag di lighting nel prossimo frame
    /// </summary>
    private System.Collections.IEnumerator ResetLightingFlag()
    {
        yield return null; // Aspetta un frame
        _isLightingInProgress = false;
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
        // Wrapper retrocompatibile: prova prima AdditiveBasic, altrimenti accetta STR-004 legacy.
        if (_playerInventory != null && _playerInventory.Has(Items.AdditiveBasic, 1))
            return DoApplyAdditive(Items.AdditiveBasic);

        if (_playerInventory != null && _playerInventory.Has(Items.SprayAntifungal, 1))
        {
            SporiumLogger.LogWarning(LogCategory.Pot, $"[ACT-014][{potSlot?.PotId}] DoSprayAntifungal legacy: uso STR-004 come equivalente AdditiveBasic");
            return DoApplyAdditive(Items.SprayAntifungal);
        }

        // Fallback: tenta comunque (gestirà failure reason)
        return DoApplyAdditive(Items.AdditiveBasic);
    }

    /// <summary>
    /// Esegue l'azione applicazione additivo.
    /// - Basic: pH +5, riduce muffe
    /// - Acid:  pH -5, aumenta muffe (se già lvl 3, propaga a pot vicino)
    /// </summary>
    public bool DoApplyAdditive(string additiveTypeId)
    {
        if (!CanApplyAdditive())
        {
            string reason = GetApplyAdditiveFailureReason(additiveTypeId);
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Spray, potSlot, reason);
            return false;
        }
        
        if (string.IsNullOrEmpty(additiveTypeId))
        {
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Spray, potSlot, "Additivo non valido");
            return false;
        }

        bool isBasic = additiveTypeId == Items.AdditiveBasic || additiveTypeId == Items.SprayAntifungal; // legacy mapping
        bool isAcid = additiveTypeId == Items.AdditiveAcid;
        if (!isBasic && !isAcid)
        {
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Spray, potSlot, $"Additivo sconosciuto: {additiveTypeId}");
            return false;
        }

        if (!IsAutomationContext && (_playerInventory == null || !_playerInventory.Has(additiveTypeId, 1)))
        {
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Spray, potSlot, "Additivo non disponibile");
            return false;
        }

        // Consuma risorse (azione/CRY) - in automation già pagate nel terminale
        if (!IsAutomationContext)
        {
            if (!TryConsumeResources())
            {
                PotEvents.EmitActionFailed(PotEvents.PotActionType.Spray, potSlot, "Insufficient resources");
                return false;
            }
        }
        
        // Consuma item
        if (!IsAutomationContext)
        {
            if (!_playerInventory.Consume(additiveTypeId, 1))
            {
                PotEvents.EmitActionFailed(PotEvents.PotActionType.Spray, potSlot, "Impossibile consumare additivo");
                return false;
            }
        }

        // Effetti pH
        if (_phSystem != null)
        {
            float drift = isBasic ? 5f : -5f;
            string actionName = isBasic ? "AdditiveBasic" : "AdditiveAcid";
            _phSystem.RegisterActionDrift(drift, actionName, potSlot.PotId);
            if (showDebugLogs)
                SporiumLogger.LogInfo(LogCategory.Ph, $"[ACT-014][{potSlot.PotId}] {actionName} applicato: pH {(drift > 0 ? "+" : "")}{drift}");
        }
        
        // Effetti muffe
        if (isBasic)
        {
            MoldSystem.ReduceMoldRiskLevel(_potState);
        }
        else
        {
            MoldSystem.IncreaseMoldRiskLevel(_potState, FindNearestPot());
        }
        
        // Notifica cambio stato
        PotEvents.EmitAction(PotEvents.PotActionType.Spray, potSlot);
        PotEvents.EmitChanged(potSlot);
            
        if (showDebugLogs)
        {
            string label = isBasic ? "Additivo Basico" : "Additivo Acido";
            SporiumLogger.LogInfo(LogCategory.Pot, $"[ACT-014][{potSlot.PotId}] {label} OK: item consumato ({additiveTypeId}), pH aggiornato, muffe aggiornate");
        }
        
        return true;
    }
    
    /// <summary>
    /// AZ-13: Esegue l'azione di potatura
    /// </summary>
    /// <param name="useSpray">Se true, usa Spray Antifungino (STR-004) per bonus e reroll</param>
    public bool DoPruning(bool useSpray = false)
    {
        if (!CanPruning())
        {
            string reason = GetPruningFailureReason();
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Pruning, potSlot, reason);
            return false;
        }
        
        // Se richiesto Spray, verifica disponibilità
        if (useSpray && !IsAutomationContext && !HasSprayAntifungal())
        {
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Pruning, potSlot, "STR-004 (Spray Antifungino) non disponibile");
            return false;
        }
        
        // Consuma STR-004 se usato
        if (useSpray && !IsAutomationContext)
        {
            if (!_playerInventory.Consume(Items.SprayAntifungal, 1))
            {
                PotEvents.EmitActionFailed(PotEvents.PotActionType.Pruning, potSlot, "Impossibile consumare STR-004");
                return false;
            }
            if (showDebugLogs)
                SporiumLogger.LogInfo(LogCategory.Inventory, $"[ACT-013][{potSlot.PotId}] Consumato STR-004 per potatura");
        }
        
        // Consuma le risorse (1 azione) - in automation già pagate nel terminale
        if (!IsAutomationContext)
        {
            if (!TryConsumeResources())
            {
                PotEvents.EmitActionFailed(PotEvents.PotActionType.Pruning, potSlot, "Insufficient resources");
                return false;
            }
        }
        
        // Carica PruningConfig
        PruningConfig pruningConfig = Resources.Load<PruningConfig>("Configs/PruningConfig");
        if (pruningConfig == null)
        {
            SporiumLogger.LogError(LogCategory.Pot, $"[ACT-013][{potSlot.PotId}] PruningConfig non trovato in Resources/Configs/PruningConfig");
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Pruning, potSlot, "Configurazione potatura non trovata");
            return false;
        }
        
        // Ottieni stadio corrente
        PlantStage currentStage = (PlantStage)_potState.Stage;
        
        // Esegui potatura
        PruningResult result = PruningSystem.TryPrune(_potState, currentStage, useSpray, pruningConfig);
        
            // Gestisci risultato
            if (result.Success)
            {
                // BLK-07.01: Rimuove infestazione
                MoldSystem.RemoveInfestation(_potState);
                _potState.DaysWithoutPruning = 0;
                
                // Se è Growth pre-Flowering e non ha già bonus resa, applica bonus
                if (result.ResultType == PruningResultType.SuccessResa)
                {
                    if (PruningSystem.ApplyResaBonus(_potState, pruningConfig))
                    {
                        if (showDebugLogs)
                            SporiumLogger.LogInfo(LogCategory.Pot, $"[ACT-013][{potSlot.PotId}] Bonus resa applicato (Growth pre-Flowering)");
                    }
                }
                
                // Log feedback appropriato
                if (showDebugLogs)
                    SporiumLogger.LogInfo(LogCategory.Pot, $"[ACT-013][{potSlot.PotId}] {result.Message}");
            }
        else
        {
            // Log fallimento
            if (showDebugLogs)
                SporiumLogger.LogInfo(LogCategory.Pot, $"[ACT-013][{potSlot.PotId}] {result.Message}");
        }
        
        // Notifica il cambio stato
        PotEvents.EmitAction(PotEvents.PotActionType.Pruning, potSlot);
        PotEvents.EmitChanged(potSlot);
        
        return result.Success;
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
            // #region agent log
            var logDataHarvestFail = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"H1\",\"location\":\"PotActions.DoHarvest:FAILED\",\"message\":\"Harvest fallito\",\"data\":{{\"potId\":\"{potSlot?.PotId}\",\"reason\":\"{reason}\",\"isAutomationContext\":{IsAutomationContext},\"stage\":{_potState?.Stage},\"amountFruits\":{_potState?.AmountFruits ?? 0f}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"d:\Sporae_Build_Beta\.cursor\debug.log", logDataHarvestFail);
            // #endregion
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Harvest, potSlot, reason);
            return false;
        }
        
        // #region agent log
        var logDataHarvestStart = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"H1\",\"location\":\"PotActions.DoHarvest:START\",\"message\":\"Harvest iniziato\",\"data\":{{\"potId\":\"{potSlot?.PotId}\",\"isAutomationContext\":{IsAutomationContext},\"amountFruits\":{_potState?.AmountFruits ?? 0f}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
        System.IO.File.AppendAllText(@"d:\Sporae_Build_Beta\.cursor\debug.log", logDataHarvestStart);
        // #endregion
        
        // Consuma le risorse - in automation già pagate nel terminale
        if (!IsAutomationContext)
        {
            if (!TryConsumeResources())
            {
                PotEvents.EmitActionFailed(PotEvents.PotActionType.Harvest, potSlot, "Insufficient resources");
                return false;
            }
        }
        
        // BLK-02.02: Applica modificatori resa basati su livello
        float baseAmount = _potState.AmountFruits;
        PlantLevelConfig levelConfig = Resources.Load<PlantLevelConfig>("Configs/PlantLevelConfig");
        if (levelConfig != null && _potState.PlantLevel >= 3)
        {
            float modifier = levelConfig.GetQuantityModifier(_potState.PlantLevel);
            baseAmount = baseAmount * (1f + modifier / 100f); // Modifier è negativo (es. -15%)
            if (showDebugLogs)
                SporiumLogger.LogDebug(LogCategory.Pot, $"[ACT-005][{potSlot.PotId}] Modificatore resa Lvl {_potState.PlantLevel}: {modifier}% (quantità: {_potState.AmountFruits} → {baseAmount})");
        }
        
        // FASE 1.3: Applica modificatore produzione basato sulla condizione
        PlantCondition currentCondition = (PlantCondition)_potState.ConditionLabel;
        float conditionProductionMultiplier = ConditionGrowthModifier.GetProductionMultiplier(currentCondition);
        if (conditionProductionMultiplier != 1.0f)
        {
            float oldAmount = baseAmount;
            baseAmount *= conditionProductionMultiplier;
            if (showDebugLogs)
                SporiumLogger.LogDebug(LogCategory.Pot, $"[ACT-005][{potSlot.PotId}] Modificatore produzione condizione {currentCondition}: {conditionProductionMultiplier:F2} (quantità: {oldAmount:F2} → {baseAmount:F2})");
        }
        
        // FASE 2.3: Applica modificatore resa basato su pH
        float phYieldMultiplier = 1.0f;
        bool isSterile = false;
        PhSystem.PhBand phBand = PhSystem.PhBand.Neutral;
        PlantData plantData = _potState.GetPlantData();
        if (_phSystem != null && plantData != null)
        {
            phBand = _phSystem.EvaluateState();
            phYieldMultiplier = PhGrowthModifier.GetYieldMultiplier(phBand, plantData.Family);
            isSterile = PhGrowthModifier.IsSterile(phBand, plantData.Family);
            
            if (phYieldMultiplier != 1.0f)
            {
                float oldAmount = baseAmount;
                baseAmount *= phYieldMultiplier;
                if (showDebugLogs)
                    SporiumLogger.LogDebug(LogCategory.Pot, $"[ACT-005][{potSlot.PotId}] Modificatore resa pH {phBand} per {plantData.Family}: {phYieldMultiplier:F2} (quantità: {oldAmount:F2} → {baseAmount:F2})");
            }
            
            // Gestione sterilità Pure in Ultra Basico
            if (isSterile)
            {
                // Attiva sterilità per 3 giorni (se DaysSterile non esiste, aggiungerlo a PotStateModel)
                // Per ora, loggiamo solo
                if (showDebugLogs)
                    SporiumLogger.LogWarning(LogCategory.Pot, $"[ACT-005][{potSlot.PotId}] Pianta Pure in Ultra Basico: STERILE (resa x2 ma non può produrre nuovi frutti per 3 giorni)");
                // TODO: Aggiungere campo DaysSterile in PotStateModel se non esiste
            }
        }
        
        // MOLD SYNERGY: Applica modificatore resa basato su Mold Risk + Famiglia + pH
        float moldYieldMultiplier = 1.0f;
        if (plantData != null)
        {
            moldYieldMultiplier = PhGrowthModifier.GetMoldYieldModifier(_potState.MoldRiskLevel, _potState.IsInfested, plantData.Family, phBand);
            if (moldYieldMultiplier != 1.0f)
            {
                float oldAmount = baseAmount;
                baseAmount *= moldYieldMultiplier;
                if (showDebugLogs)
                    SporiumLogger.LogDebug(LogCategory.Pot, $"[ACT-005][{potSlot.PotId}] Modificatore resa Mold Risk Lvl {_potState.MoldRiskLevel} (Infestata: {_potState.IsInfested}) per {plantData.Family}: {moldYieldMultiplier:F2} (quantità: {oldAmount:F2} → {baseAmount:F2})");
            }
        }
        
        // Calcola quantità frutti da raccogliere (arrotondamento a intero)
        int fruitsToHarvest = Mathf.RoundToInt(baseAmount);
        if (fruitsToHarvest <= 0)
        {
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Harvest, potSlot, "Nessun frutto disponibile");
            return false;
        }
        
        // BLK-02.02: Calcola qualità frutti basata su livello
        float baseQuality = 0f;
        float finalQuality = 0f;
        ItemConfig fruitConfig = Resources.Load<ItemConfig>("Items/" + Items.Fruits);
        if (fruitConfig != null)
        {
            baseQuality = fruitConfig.MaxQuality;
            finalQuality = baseQuality;
            
            // Applica modificatore qualità se livello >= 3
            if (levelConfig != null && _potState.PlantLevel >= 3)
            {
                float qualityModifier = levelConfig.GetQualityModifier(_potState.PlantLevel);
                finalQuality = baseQuality * (1f + qualityModifier / 100f);
                // Clamp tra MaxQuality e MaxQuality * 2 (max +100%)
                finalQuality = Mathf.Clamp(finalQuality, baseQuality, baseQuality * 2f);
                
                if (showDebugLogs)
                    SporiumLogger.LogDebug(LogCategory.Pot, $"[ACT-005][{potSlot.PotId}] Qualità frutti Lvl {_potState.PlantLevel}: +{qualityModifier}% (qualità: {baseQuality} → {finalQuality:F1})");
            }
        }
        
        // Aggiungi frutti all'inventario con qualità personalizzata
        for (int i = 0; i < fruitsToHarvest; i++)
        {
            if (fruitConfig != null && levelConfig != null && _potState.PlantLevel >= 3)
            {
                // Crea item con qualità personalizzata
                Item fruitItem = ItemFabric.CreateItemWithQuality(Items.Fruits, finalQuality);
                if (fruitItem != null)
                {
                    _playerInventory.Add(fruitItem);
                }
                else
                {
                    // Fallback se CreateItemWithQuality fallisce
                    _playerInventory.Add(Items.Fruits);
                }
            }
            else
            {
                // Livelli 1-2: usa qualità base
                _playerInventory.Add(Items.Fruits);
            }
        }
        
        // Reset frutti nel vaso
        _potState.AmountFruits = 0f;
        _potState.DaysFruitsUnharvested = 0;
        
        // BLK-02.05: Dopo la raccolta, la pianta entra in Resting
        int oldStage = _potState.Stage;
        _potState.Stage = (int)PlantStage.Resting;
        _potState.DaysInHarvestReady = 0; // Reset contatore HarvestReady
        _potState.DaysInCurrentStage = 0; // Reset contatore stadio corrente
        _potState.HasPruningResaBonus = false; // AZ-13: Reset bonus resa per nuovo ciclo
        
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
            SporiumLogger.LogInfo(LogCategory.Pot, $"[ACT-005][{potSlot.PotId}] Harvest OK: raccolti {fruitsToHarvest} frutti, aggiunti all'inventario. Stadio cambiato: {oldStage} (HarvestReady) → {(int)PlantStage.Resting} (Resting)");
        }
        
        return true;
    }
    
    /// <summary>
    /// BLK-03.01-T1: Esegue l'azione di applicare fertilizzante
    /// </summary>
    /// <param name="fertilizerItemCode">ItemCode del fertilizzante da applicare (es. "fertilizer-standard")</param>
    public bool DoFertilize(string fertilizerItemCode)
    {
        if (!CanFertilize())
        {
            string reason = GetFertilizeFailureReason();
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Fertilize, potSlot, reason);
            return false;
        }
        
        // 1. Verifica vaso e pianta
        if (_potState == null || !_potState.HasPlant)
        {
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Fertilize, potSlot, "Vaso vuoto");
            return false;
        }
        
        // 2. Verifica fertilizzante nell'inventario
        if (!IsAutomationContext && !_playerInventory.Has(fertilizerItemCode))
        {
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Fertilize, potSlot, $"Fertilizzante '{fertilizerItemCode}' non disponibile");
            return false;
        }
        
        // 3. Determina tipo fertilizzante da ItemCode
        FertilizerType fertilizerType = GetFertilizerTypeFromItemCode(fertilizerItemCode);
        
        // 4. Ottieni PlantData per verificare famiglia
        var plantData = _potState.GetPlantData();
        if (plantData == null)
        {
            PotEvents.EmitActionFailed(PotEvents.PotActionType.Fertilize, potSlot, "PlantData non trovato");
            return false;
        }
        
        // 5. Verifica coerenza genetica (REGOLA CRITICA: MORTE IMMEDIATA)
        if (!FertilizerSystem.IsFertilizerCompatible(fertilizerType, plantData.Family))
        {
            // 🚨 MORTE IMMEDIATA della pianta
            SporiumLogger.LogError(LogCategory.Pot, $"Fertilizzante incompatibile! Pianta MUORE IMMEDIATAMENTE. Vaso: {potSlot.PotId}, Famiglia: {plantData.Family}, Fertilizzante: {fertilizerType}");
           
            // Morta persistente: non svuotare il pot. Rimane Morta finché non Uproot.
            _potState.ConditionLabel = (int)Sporae.Dome.PotSystem.Condition.PlantCondition.Morta;
            _potState.DaysCritical = 3;
            
            // Spegni sistemi persistenti per evitare consumi/side-effect post-morte.
            _potState.WateringSystemOn = false;
            _potState.SetLedSystemState(LedSystemState.Off);
            
            // Rimuovi contributi pH della pianta (se presenti) per evitare drift post-morte.
            if (_phSystem != null && !string.IsNullOrEmpty(_potState.PlantCode))
            {
                _phSystem.RemovePlantContributions(potSlot.PotId);
            }
            
            // Notifica evento morte pianta
            PotEvents.EmitPlantDied(potSlot.PotId, $"Fertilizzante incompatibile: {fertilizerType} su pianta {plantData.Family}");
            
            // Consuma comunque il fertilizzante (già usato) - in automation è già consumato.
            if (!IsAutomationContext)
                _playerInventory.Consume(fertilizerItemCode, 1);
            
            // Aggiorna le visuali del Pot (sprite dead)
            if (potGrowthController != null)
            {
                potGrowthController.UpdateVisuals();
            }
            
            // Notifica cambio stato
            PotEvents.EmitAction(PotEvents.PotActionType.Fertilize, potSlot);
            PotEvents.EmitChanged(potSlot);
            
            return false; // Operazione fallita (pianta morta)
        }
        
        // 6. Applica fertilizzante (aumenta FertilizerLevel)
        int fertilizerAmount = FertilizerSystem.GetFertilizerAmount(fertilizerType);
        _potState.FertilizerLevel = Mathf.Clamp(
            _potState.FertilizerLevel + fertilizerAmount,
            0, 100);
        
        // 7. Se Resting → Flowering
        if (_potState.Stage == (int)PlantStage.Resting)
        {
            int oldStage = _potState.Stage;
            _potState.Stage = (int)PlantStage.Flowering;
            _potState.DaysInCurrentStage = 0;
            _potState.HasPruningResaBonus = false; // AZ-13: Reset bonus resa per nuovo ciclo
            
            // BLK-02.02: Ciclo completo quando si riattiva da Resting → Flowering con fertilizzante
            // Incrementa cicli completati e verifica level up
            _potState.IncrementCompletedCycle();
            PlantLevelConfig levelConfig = Resources.Load<PlantLevelConfig>("Configs/PlantLevelConfig");
            if (levelConfig != null)
            {
                bool levelUp = PlantLevelSystem.CheckLevelUp(_potState, levelConfig);
                if (levelUp && showDebugLogs)
                {
                    SporiumLogger.LogInfo(LogCategory.Pot, $"[ACT-015][{potSlot.PotId}] Livello aumentato a Lvl {_potState.PlantLevel} (cicli completati: {_potState.CompletedCycles})!");
                }
                if (showDebugLogs)
                    SporiumLogger.LogInfo(LogCategory.Pot, $"[ACT-015][{potSlot.PotId}] Ciclo completo! Cicli completati: {_potState.CompletedCycles}");
            }
            
            // Notifica cambio stadio
            if (potGrowthController != null)
            {
                potGrowthController.OnStageChanged(PlantStage.Flowering);
            }
            PotEvents.EmitPlantStageChanged(potSlot.PotId, PlantStage.Flowering);
            
            if (showDebugLogs)
                SporiumLogger.LogInfo(LogCategory.Pot, $"{potSlot.PotId}: Transizione Resting → Flowering dopo fertilizzante");
        }
        
        // 8. Consuma fertilizzante dall'inventario (skip in automation: già consumato in conferma terminale)
        if (!IsAutomationContext)
        {
            if (!_playerInventory.Consume(fertilizerItemCode, 1))
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Impossibile consumare fertilizzante '{fertilizerItemCode}'");
                return false;
            }
        }
        
        // 9. Aggiorna tracking
        _potState.DaysFertilizerActive = 0; // Reset contatore (verrà incrementato a fine giornata se rimane attivo)
        
        // Notifica il cambio stato
        PotEvents.EmitAction(PotEvents.PotActionType.Fertilize, potSlot);
        PotEvents.EmitChanged(potSlot);
        
        if (showDebugLogs)
        {
            SporiumLogger.LogInfo(LogCategory.Pot, $"[ACT-015][{potSlot.PotId}] Fertilize OK: {fertilizerType} applicato (+{fertilizerAmount}%), livello totale: {_potState.FertilizerLevel}%");
        }
        
        return true;
    }
    
    /// <summary>
    /// BLK-03.01-T1: Mappa ItemCode → FertilizerType
    /// </summary>
    private FertilizerType GetFertilizerTypeFromItemCode(string itemCode)
    {
        // Mappa ItemCode → FertilizerType
        // Esempio: "fertilizer-standard" → Standard
        //          "fertilizer-pure" → Pure
        //          "fertilizer-prohibited" → Prohibited
        return itemCode switch
        {
            "fertilizer-standard" => FertilizerType.Standard,
            "fertilizer-pure" => FertilizerType.Pure,
            "fertilizer-prohibited" => FertilizerType.Prohibited,
            _ => FertilizerType.Standard  // Default fallback
        };
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Verifica se il player è in range per interagire
    /// </summary>
    private bool IsPlayerInRange()
    {
        if (IsAutomationContext)
            return true;

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
        if (IsAutomationContext)
            return true;

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
        if (IsAutomationContext)
            return true;

        if (!_gameManager) 
            return false;
        
        int actionsCost = GetActionsCost();
        
        // DEBUG_SAFE_FIX: Verifica che actionsCost sia valido
        if (actionsCost <= 0)
        {
            SporiumLogger.LogError(LogCategory.Pot, $"[{potSlot?.PotId}] TryConsumeResources: actionsCost è {actionsCost}! Dovrebbe essere > 0. Usando 1 come fallback.");
            actionsCost = 1;
        }
        
        int actionsBefore = _gameManager.ActionsLeft;
        SporiumLogger.LogDebug(LogCategory.Pot, $"[{potSlot?.PotId}] TryConsumeResources: actionsCost={actionsCost}, actionsBefore={actionsBefore}");
        
        // Usa il metodo TrySpendAction del GameManager esistente
        bool success = _gameManager.TrySpendAction(actionsCost);
        
        int actionsAfter = _gameManager.ActionsLeft;
        int consumed = actionsBefore - actionsAfter;
        SporiumLogger.LogDebug(LogCategory.Pot, $"[{potSlot?.PotId}] TryConsumeResources risultato: success={success}, actionsAfter={actionsAfter}, consumate={consumed}");
        
        // DEBUG_SAFE_FIX: Verifica che il consumo sia corretto
        if (success && consumed != actionsCost)
        {
            SporiumLogger.LogError(LogCategory.Pot, $"[{potSlot?.PotId}] TryConsumeResources: Consumo errato! Richiesto: {actionsCost}, Consumato: {consumed}");
        }
        
        return success;
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
        // Retrocompat: usa lo stesso reason del nuovo sistema additivi (basic)
        return GetApplyAdditiveFailureReason(Items.AdditiveBasic);
    }

    private string GetApplyAdditiveFailureReason(string additiveTypeId)
    {
        if (_potState == null) return "Stato vaso non valido";
        if (!_potState.HasPlantGrowing) return "Vaso vuoto";
        if (!IsPlayerInRange()) return "Troppo lontano";
        if (!CanConsumeResources()) return "Azioni o CRY insufficienti";

        if (_playerInventory != null && !string.IsNullOrEmpty(additiveTypeId))
        {
            // Legacy mapping: se richiesto basic ma c'è STR-004, lo gestisce il wrapper.
            if (additiveTypeId == Items.AdditiveBasic && !_playerInventory.Has(Items.AdditiveBasic, 1) && _playerInventory.Has(Items.SprayAntifungal, 1))
                return "Azione non permessa";

            if (!_playerInventory.Has(additiveTypeId, 1))
                return "Additivo non disponibile";
        }

        return "Azione non permessa";
    }
    
    private string GetHarvestFailureReason()
    {
        if (_potState == null) return "Stato vaso non valido";
        if (_potState.Stage != (int)PlantStage.HarvestReady) 
        {
            // #region agent log
            var logDataReason = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"H1\",\"location\":\"PotActions.GetHarvestFailureReason:STAGE\",\"message\":\"Stage non HarvestReady\",\"data\":{{\"potId\":\"{potSlot?.PotId}\",\"stage\":{_potState.Stage},\"expectedStage\":{(int)PlantStage.HarvestReady}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"d:\Sporae_Build_Beta\.cursor\debug.log", logDataReason);
            // #endregion
            return "Pianta non in HarvestReady";
        }
        if (_potState.AmountFruits <= 0f) 
        {
            // #region agent log
            var logDataReason = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"H1\",\"location\":\"PotActions.GetHarvestFailureReason:NO_FRUITS\",\"message\":\"Nessun frutto disponibile\",\"data\":{{\"potId\":\"{potSlot?.PotId}\",\"amountFruits\":{_potState.AmountFruits},\"amountFruitsRaw\":\"{_potState.AmountFruits}\",\"daysInHarvestReady\":{_potState.DaysInHarvestReady},\"daysFruitsUnharvested\":{_potState.DaysFruitsUnharvested}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"d:\Sporae_Build_Beta\.cursor\debug.log", logDataReason);
            // #endregion
            return "Nessun frutto disponibile";
        }
        if (!IsPlayerInRange()) return "Troppo lontano";
        if (!CanConsumeResources()) return "Azioni o CRY insufficienti";
        return "Azione non permessa";
    }
    
    private string GetFertilizeFailureReason()
    {
        if (_potState == null) return "Stato vaso non valido";
        if (!_potState.HasPlantGrowing) return "Vaso vuoto";
        if (!IsPlayerInRange()) return "Troppo lontano";
        if (!CanConsumeResources()) return "Azioni insufficienti";
        return "Azione non permessa";
    }
    
    private string GetPruningFailureReason()
    {
        if (_potState == null) return "Stato vaso non valido";
        if (!_potState.HasPlantGrowing) return "Vaso vuoto";
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
            SporiumLogger.LogDebug(LogCategory.Pot, $"Configurazione aggiornata per {potSlot?.PotId}");
        }
    }
    
    /// <summary>
    /// Restituisce il limite massimo di idratazione
    /// </summary>
    public int GetMaxHydration()
    {
        return config ? config.MaxHydration : 10; // Fallback a 10 step = 10% ciascuno
    }
    
    /// <summary>
    /// Restituisce il limite massimo di esposizione alla luce
    /// </summary>
    public int GetMaxLightExposure()
    {
        return config ? config.MaxLightExposure : 3;
    }
    
    /// <summary>
    /// Restituisce il limite massimo di giorni consecutivi LED per stress completo (100%)
    /// </summary>
    public int GetMaxDaysForFullStress()
    {
        return config ? config.MaxDaysForFullStress : 5;
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
