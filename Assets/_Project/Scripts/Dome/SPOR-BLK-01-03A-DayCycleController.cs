using System.Collections.Generic;
using _Project.Sporae.Core;
using UnityEngine;
using Sporae.Core;
using Sporae.Dome;
using Sporae.Dome.PotSystem.Growth;
using Sporae.Dome.PotSystem.Condition;
using Sporae.Dome.PotSystem.Fertilizer;
using Sporae.Dome.PotSystem.Mold;
using Sporae.Dome.PotSystem.Botanical;
using Sporae.Dome.PotSystem.Level;
using UnityEngine.SceneManagement;
using _Project;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.NotificationsFoundation;
using System;

/// <summary>
/// Controller per il ciclo giornaliero del sistema di crescita delle piante.
/// Implementa il sistema deterministico basato su timestamp invece di flag volatili.
/// Si iscrive a GameManager.OnDayChanged e gestisce la crescita di tutti i vasi registrati.
/// </summary>
public class DayCycleController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PlantGrowthConfig growthConfig;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Lista dei vasi registrati per la crescita
    private readonly List<PotStateModel> _registeredPots = new();
    private bool _isInitialized;
    
    private DayCycleSystem _dayCycleSystem;
    private PhSystem _phSystem;
    private PotSystemConfig _potSystemConfig;
    private GameManager _gameManager;
    private UINotification _uiNotification;
    private ToastNotificationManager _toastManager;
    private DomePotRegistry _potRegistry;
    private readonly CondensationDayProcessor _condensationDayProcessor = new();
    private bool _arcticTensionCallbacksHooked;

    private static bool IsDead(PotStateModel pot)
    {
        if (pot == null) return false;
        return (PlantCondition)pot.ConditionLabel == PlantCondition.Morta;
    }


    private void Awake()
    {
        ServiceContainer.Instance?.Register(this);
        BotanicalArcticTensionNotifier.ResetSessionState();

        growthConfig = Resources.Load<PlantGrowthConfig>("Configs/PlantGrowthConfig");
        if (!growthConfig)
            SporiumLogger.LogWarning(LogCategory.Dome, $"PlantGrowthConfig non trovato in Resources/Configs/, verrà cercato in PotSystemConfig");

        SceneManager.sceneLoaded += (_, _) =>
        {
            SubscribeToEvents();
        };
    }

    private void Start()
    {
        InitializeSystem();
    }

    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    /// <summary>
    /// Inizializza il sistema e si iscrive agli eventi
    /// </summary>
    private void InitializeSystem()
    {
        if (_isInitialized)
            return;
        
        // Trova UINotification se disponibile (per toast HUD)
        // La risoluzione avviene via ServiceContainer; il late binding copre i casi di bootstrap tardivo.
        if (_uiNotification == null)
        {
            TryGetUINotification();

            if (enableDebugLogs && _uiNotification == null && ServiceContainer.Instance != null)
            {
                SporiumLogger.LogWarning(LogCategory.Dome, "UINotification non disponibile nel ServiceContainer; verrà collegato automaticamente quando registrato.");
            }
            else if (enableDebugLogs && _uiNotification != null)
            {
                SporiumLogger.LogInfo(LogCategory.Dome, "UINotification trovato e collegato per toast notifications.");
            }
        }
        
        // Prova a ottenere ToastNotificationManager (nuovo sistema)
        if (_toastManager == null)
        {
            TryGetToastManager();
        }
        
        // Cerca configurazione in PotSystemConfig se non trovata
        if (growthConfig == null)
        {
            EnsurePotSystemConfigLoaded();
            if (_potSystemConfig != null && _potSystemConfig.GrowthConfig != null)
            {
                growthConfig = _potSystemConfig.GrowthConfig;
                if (enableDebugLogs)
                    SporiumLogger.LogInfo(LogCategory.Dome, "DayCycleController: Configurazione caricata da PotSystemConfig");
            }
        }

        // Verifica configurazione
        if (growthConfig == null)
        {
            SporiumLogger.LogError(LogCategory.Dome, "DayCycleController: Nessuna configurazione di crescita trovata!");
            return;
        }
        
        // Cerca PotSystemConfig per ottenere MaxHydration e MaxLightExposure
        EnsurePotSystemConfigLoaded();
        
        if (_potSystemConfig == null && enableDebugLogs)
        {
            SporiumLogger.LogWarning(LogCategory.Dome, "DayCycleController: PotSystemConfig non trovato in Resources/Configs/, userò valori di default (MaxHydration=4, MaxLightExposure=3)");
        }
        else if (_potSystemConfig != null && enableDebugLogs)
        {
            SporiumLogger.LogInfo(LogCategory.Dome, $"DayCycleController: PotSystemConfig caricato - MaxHydration={_potSystemConfig.MaxHydration}, MaxLightExposure={_potSystemConfig.MaxLightExposure}");
        }

        _isInitialized = true;
        if (enableDebugLogs)
            SporiumLogger.LogInfo(LogCategory.Dome, $"DayCycleController: Inizializzato con config '{growthConfig.name}'");
    }

    /// <summary>
    /// Si iscrive agli eventi necessari
    /// </summary>
    private void SubscribeToEvents()
    {
        // Unsubscribe first to prevent double-subscription when DayCycleSystem instance changes across scene reloads
        if (_dayCycleSystem != null)
            _dayCycleSystem.OnDayChanged -= HandleDayChanged;

        _dayCycleSystem = ServiceContainer.Instance?.Get<DayCycleSystem>();
        if (_dayCycleSystem != null)
            _dayCycleSystem.OnDayChanged += HandleDayChanged;
        
        // Cerca PhSystem per integrazione pH (con retry se non disponibile subito)
        TryGetPhSystem();
        
        // Cerca GameManager per consumo risorse watering system
        TryGetGameManager();
        TryGetPotRegistry();
        
        // Cerca UINotification per mostrare warning
        TryGetUINotification();
        TryGetToastManager();
        
        // Sottoscrivi all'evento OnServiceRegistered per quando PhSystem viene registrato dopo
        if (ServiceContainer.Instance != null)
        {
            ServiceContainer.Instance.OnServiceRegistered += OnServiceRegistered;
            
            // DEBUG_SAFE_FIX: Ritenta la ricerca di UINotification ora che ServiceContainer è disponibile
            if (_uiNotification == null)
            {
                TryGetUINotification();
            }
            if (_toastManager == null)
            {
                TryGetToastManager();
            }
        }

        TryHookArcticTensionCallbacks();
    }

    private void TryHookArcticTensionCallbacks()
    {
        if (!_arcticTensionCallbacksHooked)
        {
            PotEvents.OnPotStateChanged += OnPotStateChangedForArcticTension;
            _arcticTensionCallbacksHooked = true;
        }
        HookPhSystemArcticTension();
    }

    private void HookPhSystemArcticTension()
    {
        if (_phSystem == null) return;
        _phSystem.OnPhChanged -= OnPhChangedForArcticTension;
        _phSystem.OnPhChanged += OnPhChangedForArcticTension;
    }

    private void OnPotStateChangedForArcticTension(PotSlot _)
    {
        BotanicalArcticTensionNotifier.EvaluateAndNotify(_phSystem);
    }

    private void OnPhChangedForArcticTension(float _, float __)
    {
        BotanicalArcticTensionNotifier.EvaluateAndNotify(_phSystem);
    }
    
    /// <summary>
    /// Tenta di ottenere PhSystem dal ServiceContainer
    /// </summary>
    private void TryGetPhSystem()
    {
        if (ServiceContainer.Instance == null)
            return;
        
        try
        {
            // GDD AZ-11: Prova a ottenere PhSystem senza generare warning se non disponibile
            _phSystem = ServiceContainer.Instance.Get<PhSystem>(suppressWarning: true);
            if (_phSystem != null && enableDebugLogs)
            {
                SporiumLogger.LogInfo(LogCategory.Dome, "PhSystem trovato e collegato!");
            }
            if (_phSystem != null)
                HookPhSystemArcticTension();
        }
        catch
        {
            // PhSystem non ancora registrato, sarà recuperato quando viene registrato
            _phSystem = null;
        }
    }
    
    /// <summary>
    /// Tenta di ottenere GameManager dal ServiceContainer
    /// </summary>
    private void TryGetGameManager()
    {
        if (ServiceContainer.Instance == null)
            return;
        
        try
        {
            _gameManager = ServiceContainer.Instance.Get<GameManager>();
            if (_gameManager != null && enableDebugLogs)
            {
                SporiumLogger.LogInfo(LogCategory.Core, "GameManager trovato e collegato!");
            }
        }
        catch
        {
            // GameManager non ancora registrato, sarà recuperato quando viene registrato
            _gameManager = null;
        }
    }

    private void TryGetPotRegistry()
    {
        if (ServiceContainer.Instance == null)
            return;

        _potRegistry = ServiceContainer.Instance.Get<DomePotRegistry>(suppressWarning: true);
    }
    
    /// <summary>
    /// Tenta di ottenere UINotification dal ServiceContainer o FindObjectOfType
    /// </summary>
    private void TryGetUINotification()
    {
        if (ServiceContainer.Instance != null)
        {
            _uiNotification = ServiceContainer.Instance.Get<UINotification>(suppressWarning: true);
            if (_uiNotification != null && enableDebugLogs)
            {
                SporiumLogger.LogInfo(LogCategory.Core, "UINotification trovato dal ServiceContainer!");
            }
        }
    }

    private void EnsurePotSystemConfigLoaded()
    {
        if (_potSystemConfig != null)
            return;

        _potSystemConfig = Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");
        if (_potSystemConfig == null)
        {
            var allConfigs = Resources.LoadAll<PotSystemConfig>("Configs");
            if (allConfigs != null && allConfigs.Length > 0)
            {
                _potSystemConfig = allConfigs[0];
                if (enableDebugLogs)
                    SporiumLogger.LogInfo(LogCategory.Dome, $"DayCycleController: PotSystemConfig trovato con nome alternativo '{_potSystemConfig.name}'");
            }
        }
    }
    
    /// <summary>
    /// Chiamato quando un servizio viene registrato nel ServiceContainer
    /// </summary>
    /// <summary>
    /// Tenta di ottenere ToastNotificationManager dal ServiceContainer
    /// </summary>
    private void TryGetToastManager()
    {
        if (ServiceContainer.Instance == null)
            return;
        
        try
        {
            _toastManager = ServiceContainer.Instance.Get<ToastNotificationManager>(suppressWarning: true);
            if (_toastManager != null && enableDebugLogs)
            {
                SporiumLogger.LogInfo(LogCategory.UI, "ToastNotificationManager trovato dal ServiceContainer!");
            }
        }
        catch
        {
            // ToastNotificationManager non nel ServiceContainer, ignora
        }
    }
    
    private void OnServiceRegistered(object service)
    {
        if (service is PhSystem phSystem && _phSystem == null)
        {
            _phSystem = phSystem;
            if (enableDebugLogs)
            {
                SporiumLogger.LogInfo(LogCategory.Ph, "PhSystem registrato! Collegato al sistema di crescita.");
            }
            HookPhSystemArcticTension();
        }
        
        if (service is UINotification uiNotification && _uiNotification == null)
        {
            _uiNotification = uiNotification;
            if (enableDebugLogs)
            {
                SporiumLogger.LogInfo(LogCategory.UI, "UINotification registrato! Collegato per warning watering system.");
            }
        }

        if (service is DomePotRegistry potRegistry && _potRegistry == null)
        {
            _potRegistry = potRegistry;
        }
        
        if (service is ToastNotificationManager toastManager && _toastManager == null)
        {
            _toastManager = toastManager;
            if (enableDebugLogs)
            {
                SporiumLogger.LogInfo(LogCategory.UI, "ToastNotificationManager registrato! Collegato al sistema di crescita.");
            }
        }
        
        if (service is GameManager gameManager && _gameManager == null)
        {
            _gameManager = gameManager;
            if (enableDebugLogs)
            {
                SporiumLogger.LogInfo(LogCategory.Core, "GameManager registrato! Collegato per consumo risorse watering.");
            }
        }
    }

    /// <summary>
    /// Rimuove le iscrizioni agli eventi
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (_dayCycleSystem != null)
            _dayCycleSystem.OnDayChanged -= HandleDayChanged;
        
        // Rimuovi sottoscrizione a OnServiceRegistered
        if (ServiceContainer.Instance != null)
        {
            ServiceContainer.Instance.OnServiceRegistered -= OnServiceRegistered;
        }

        if (_arcticTensionCallbacksHooked)
        {
            PotEvents.OnPotStateChanged -= OnPotStateChangedForArcticTension;
            _arcticTensionCallbacksHooked = false;
        }
        if (_phSystem != null)
            _phSystem.OnPhChanged -= OnPhChangedForArcticTension;
    }

    /// <summary>
    /// Registra un vaso nel sistema di crescita
    /// </summary>
    public void RegisterPot(PotStateModel pot)
    {
        if (pot == null) return;

        if (!_registeredPots.Contains(pot))
        {
            _registeredPots.Add(pot);
            if (enableDebugLogs)
                SporiumLogger.LogDebug(LogCategory.Pot, $"Registrato vaso {pot.PotId}");
        }
    }

    /// <summary>
    /// Rimuove un vaso dal sistema di crescita
    /// </summary>
    public void UnregisterPot(PotStateModel pot)
    {
        if (pot == null) return;

        if (_registeredPots.Remove(pot))
        {
            if (enableDebugLogs)
                SporiumLogger.LogDebug(LogCategory.Pot, $"Rimosso vaso {pot.PotId}");
        }
    }

    /// <summary>
    /// Gestisce il cambio di giorno dal GameManager
    /// </summary>
    private void HandleDayChanged(int dayIndex)
    {
        if (enableDebugLogs)
            SporiumLogger.LogDebug(LogCategory.Core, $"HandleDayChanged chiamato per Day {dayIndex}");
        
        if (growthConfig == null)
        {
            SporiumLogger.LogError(LogCategory.Core, "Nessuna configurazione di crescita trovata!");
            return;
        }
        
        // All'inizio del nuovo giorno svuota i contributi azioni/eventi del giorno precedente,
        // così il tooltip mostrerà solo quelli applicati in questo cambio giorno (LED, Overwatering, ecc.)
        if (_phSystem != null)
            _phSystem.ClearDailyModifierContributions();
        
        // Pipeline End Day per il giorno D:
        // 1. CheckWateringSystemResources() - Warning preventivo
        CheckWateringSystemResources();
        
        // 2. ResolveGrowthForAllPots(D) - Calcola crescita (usa WateringSystemOn)
        ResolveGrowthForAllPots(dayIndex);
        
        // 3. ApplyWateringSystemEffects() - Applica effetti watering + consumo risorse + fallback
        ApplyWateringSystemEffects();
        
        // 3b. ApplyLedSystemEffects() - Applica effetti LED persistente + consumo CRY + scaling (BLK-02.07)
        ApplyLedSystemEffects();
        
        // 4. Calcola e registra pH drift dalle piante (integrazione pH)
        CalculateAndRegisterPhDrift(dayIndex);
        
        // 4b. Applica in un'unica soluzione tutti i drift accodati (piante + azioni + eventi)
        if (_phSystem != null)
        {
            _phSystem.ApplyQueuedDrifts();
        }
        
        // 5. ApplyDecayAndCleanup(D) - Decay naturale
        ApplyDecayAndCleanup(dayIndex);
        
        // 6. CalculatePlantConditions(D) - Calcola score condizione per tutte le piante (all'alba)
        CalculatePlantConditions(dayIndex);
        
        // 7. FASE 3: Calcola condensazione basata su piante attive e LED
        ApplyCondensationSystem(dayIndex);

        // 8. Processa Food Room (produzione, costi, harvest disponibili)
        var foodRoom = _gameManager?.FoodRoomSystem;
        if (foodRoom != null)
        {
            foodRoom.ProcessDailyProduction(dayIndex);
            foodRoom.ProcessDailyCosts();
        }

        // 9. ApplyPassivePowers — Task 3: registra drift pH passivo dei CryoSlot e applica cap.
        //    Chiamato DOPO ApplyQueuedDrifts così i cap agiscono sul pH già aggiornato.
        //    I CryoSlot non entrano mai in _registeredPots.
        ApplyPassivePowers(dayIndex);

        BotanicalArcticTensionNotifier.EvaluateAndNotify(_phSystem);
        
        // 4. AdvanceDayHUD() - gestito automaticamente dal GameManager esistente
        
        if (enableDebugLogs)
            SporiumLogger.LogDebug(LogCategory.Core, $"Growth tick completato per Day {dayIndex}");
    }

    /// <summary>
    /// Risolve la crescita per tutti i vasi registrati
    /// </summary>
    private void ResolveGrowthForAllPots(int dayIndex)
    {
        if (enableDebugLogs)
            SporiumLogger.LogDebug(LogCategory.Core, $"Applicazione crescita a {_registeredPots.Count} vasi per Day {dayIndex}");

        foreach (var pot in _registeredPots)
        {
            if (pot is { HasPlant: true } && !IsDead(pot))
            {
                ResolveGrowthForPot(pot, dayIndex);
            }
        }
    }

    /// <summary>
    /// Risolve la crescita per un singolo vaso
    /// BLK-02.02: Implementa sistema di crescita a 6 stadi con requisiti specifici per pianta
    /// </summary>
    private void ResolveGrowthForPot(PotStateModel pot, int dayIndex)
    {
        // Ottieni PlantData per verificare i requisiti
        PlantData plantData = pot.GetPlantData();
        if (plantData == null)
        {
            if (enableDebugLogs)
                SporiumLogger.LogWarning(LogCategory.Pot, $"{pot.PotId}: PlantData non trovato, uso sistema base");
            // Fallback al sistema base se non c'è PlantData
            ResolveGrowthForPotLegacy(pot, dayIndex);
            return;
        }
        
        // Calcola idratazione percentuale (0-100%)
        int maxHydration = _potSystemConfig != null ? _potSystemConfig.MaxHydration : 10;
        int hydrationPercent = maxHydration > 0 ? Mathf.RoundToInt((float)pot.Hydration / maxHydration * 100f) : 0;
        
        // GDD AZ-11: Usa WateringSystemOn invece di LastWateredDay per determinare idratazione
        // Il sistema è persistente (toggle ON/OFF), non più basato su timestamp
        bool hadHydration = pot.WateringSystemOn;
        bool hadLight = (pot.LastLitDay == dayIndex - 1); // Light rimane basato su timestamp
        
        // Incrementa giorni nello stadio corrente
        int oldStage = pot.Stage;
        pot.DaysInCurrentStage++;
        
        // BLK-03.01-T2: Calcola punti giornalieri basati su valori nel range
        var pointsResult = GrowthPointsCalculator.CalculateDailyPoints(
            pot, plantData, _potSystemConfig);
        
        // DEBUG: Log calcolo punti giornalieri (per capire perché DaysConsecutiveOptimal non incrementa)
        PlantStage currentStageForOptimal = (PlantStage)pot.Stage;
        // DEBUG_SAFE_FIX: Per Seed e Sprout, consideriamo ottimali anche i giorni con solo water + light (2 punti)
        // perché il fertilizzante è opzionale per questi stadi
        int requiredOptimalPoints = (currentStageForOptimal == PlantStage.Seed || currentStageForOptimal == PlantStage.Sprout) ? 2 : 3;
        int oldDaysConsecutiveOptimal = pot.DaysConsecutiveOptimal;
        
        // Log critico: Calcolo punti giornalieri completo
        SporiumLogger.LogDebugWithLocation(
            LogCategory.Pot,
            "DayCycleController:ResolveGrowthForPot:POINTS_CALCULATION",
            $"Calcolo Punti Giornalieri - PotId={pot.PotId}, Day={dayIndex}",
            new {
                potId = pot.PotId,
                day = dayIndex,
                stage = currentStageForOptimal.ToString(),
                hydrationPercent = hydrationPercent,
                wateringSystemOn = pot.WateringSystemOn,
                ledSystemState = pot.LedSystemState.ToString(),
                fertilizerLevel = pot.FertilizerLevel,
                waterPoint = pointsResult.WaterPoint,
                lightPoint = pointsResult.LightPoint,
                fertilizerPoint = pointsResult.FertilizerPoint,
                totalPoints = pointsResult.TotalPoints,
                requiredOptimalPoints = requiredOptimalPoints,
                oldDaysConsecutiveOptimal = oldDaysConsecutiveOptimal,
                willIncrement = pointsResult.TotalPoints >= requiredOptimalPoints
            },
            "F",
            "debug"
        );
        
        // BLK-03.01-T2: Aggiorna tracking giorni consecutivi ottimali
        // DEBUG_SAFE_FIX: Per Seed e Sprout, consideriamo ottimali anche i giorni con solo water + light (2 punti)
        // perché il fertilizzante è opzionale per questi stadi
        
        if (pointsResult.TotalPoints >= requiredOptimalPoints)
        {
            pot.DaysConsecutiveOptimal++;
            if (pot.DayOptimalParametersStarted < 0)
            {
                pot.DayOptimalParametersStarted = dayIndex;
            }
        }
        else
        {
            // Reset se non abbastanza parametri sono ottimali
            pot.DaysConsecutiveOptimal = 0;
            pot.DayOptimalParametersStarted = -1;
        }
        
        // DEBUG: Log dopo aggiornamento DaysConsecutiveOptimal
        bool wasIncremented = pointsResult.TotalPoints >= requiredOptimalPoints;
        bool wasReset = pointsResult.TotalPoints < requiredOptimalPoints;
        
        SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_OPTIMAL_DAYS] {pot.PotId} Day={dayIndex}: Stage={currentStageForOptimal}, Points={pointsResult.TotalPoints}/{requiredOptimalPoints} (W={pointsResult.WaterPoint}, L={pointsResult.LightPoint}, F={pointsResult.FertilizerPoint}), OldDays={oldDaysConsecutiveOptimal} → NewDays={pot.DaysConsecutiveOptimal}, Incremented={wasIncremented}, Reset={wasReset}");
        
        // Log critico: Tracking giorni ottimali consecutivi
        SporiumLogger.LogDebugWithLocation(
            LogCategory.Pot,
            "DayCycleController:ResolveGrowthForPot:OPTIMAL_DAYS_UPDATE",
            $"Aggiornamento Giorni Ottimali - PotId={pot.PotId}, Day={dayIndex}",
            new {
                potId = pot.PotId,
                day = dayIndex,
                oldDaysConsecutiveOptimal = oldDaysConsecutiveOptimal,
                newDaysConsecutiveOptimal = pot.DaysConsecutiveOptimal,
                dayOptimalParametersStarted = pot.DayOptimalParametersStarted,
                wasIncremented = wasIncremented,
                wasReset = wasReset
            },
            "F",
            "debug"
        );
        
        if (enableDebugLogs)
        {
            SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Punti giornalieri - Water: {pointsResult.WaterPoint}, Light: {pointsResult.LightPoint}, Fertilizer: {pointsResult.FertilizerPoint}, Total: {pointsResult.TotalPoints}, DaysOptimal: {pot.DaysConsecutiveOptimal}");
        }
        
        // FASE 1.2: Applica modificatore crescita basato sulla condizione
        // Il moltiplicatore viene applicato ai punti accumulati per accelerare la crescita
        PlantCondition currentCondition = (PlantCondition)pot.ConditionLabel;
        float conditionGrowthMultiplier = ConditionGrowthModifier.GetGrowthSpeedMultiplier(currentCondition);
        
        // FASE 2.2: Applica modificatore crescita basato su pH
        float phGrowthMultiplier = 1.0f;
        PhSystem.PhBand phBand = PhSystem.PhBand.Neutral;
        if (_phSystem != null && plantData != null)
        {
            phBand = _phSystem.EvaluateState();
            phGrowthMultiplier = PhGrowthModifier.GetGrowthMultiplier(phBand, plantData.Family);
        }
        
        // MOLD SYNERGY: Applica modificatore crescita basato su Mold Risk + Famiglia + pH
        float moldGrowthMultiplier = 1.0f;
        if (plantData != null)
        {
            moldGrowthMultiplier = PhGrowthModifier.GetMoldGrowthModifier(pot.MoldRiskLevel, plantData.Family, phBand);
        }
        
        // Moltiplicatori cumulativi (moltiplicativi, non additivi)
        float totalGrowthMultiplier = conditionGrowthMultiplier * phGrowthMultiplier * moldGrowthMultiplier;
        
        if (totalGrowthMultiplier != 1.0f && pointsResult.TotalPoints > 0)
        {
            // Calcola punti aggiuntivi basati sul moltiplicatore totale
            // Esempio: se abbiamo 3 punti e moltiplicatore 1.2, otteniamo 3.6 → 4 punti (arrotondato)
            float totalPointsFloat = pointsResult.TotalPoints * totalGrowthMultiplier;
            int additionalPoints = Mathf.RoundToInt(totalPointsFloat) - pointsResult.TotalPoints;
            
            if (additionalPoints > 0)
            {
                // Distribuisci i punti aggiuntivi proporzionalmente tra Water, Light, Fertilizer
                // Basato sui punti già guadagnati
                if (pointsResult.WaterPoint > 0)
                {
                    int waterBonus = Mathf.RoundToInt((float)additionalPoints * (pointsResult.WaterPoint / (float)pointsResult.TotalPoints));
                    pot.GrowthPointsWater += waterBonus;
                }
                if (pointsResult.LightPoint > 0)
                {
                    int lightBonus = Mathf.RoundToInt((float)additionalPoints * (pointsResult.LightPoint / (float)pointsResult.TotalPoints));
                    pot.GrowthPointsLight += lightBonus;
                }
                if (pointsResult.FertilizerPoint > 0)
                {
                    int fertilizerBonus = additionalPoints - 
                        (pointsResult.WaterPoint > 0 ? Mathf.RoundToInt((float)additionalPoints * (pointsResult.WaterPoint / (float)pointsResult.TotalPoints)) : 0) -
                        (pointsResult.LightPoint > 0 ? Mathf.RoundToInt((float)additionalPoints * (pointsResult.LightPoint / (float)pointsResult.TotalPoints)) : 0);
                    pot.GrowthPointsFertilizer += fertilizerBonus;
                }
                
                SporiumLogger.LogDebug(LogCategory.Pot, $"[GROWTH_MODIFIER] {pot.PotId}: Condizione {currentCondition} (x{conditionGrowthMultiplier:F2}) + pH (x{phGrowthMultiplier:F2}) + Mold Risk (x{moldGrowthMultiplier:F2}) = Totale x{totalGrowthMultiplier:F2}. Punti aggiuntivi: {additionalPoints} (totale: {pointsResult.TotalPoints} → {pointsResult.TotalPoints + additionalPoints})");
            }
            else if (additionalPoints < 0)
            {
                // Se il moltiplicatore è < 1.0, riduci i punti (ma non rimuovere punti già accumulati)
                // Per semplicità, non riduciamo i punti già accumulati, solo non aggiungiamo bonus
                SporiumLogger.LogDebug(LogCategory.Pot, $"[GROWTH_MODIFIER] {pot.PotId}: Condizione {currentCondition} (x{conditionGrowthMultiplier:F2}) + pH (x{phGrowthMultiplier:F2}) + Mold Risk (x{moldGrowthMultiplier:F2}) = Totale x{totalGrowthMultiplier:F2} (riduzione crescita, nessun bonus punti)");
            }
        }
        
        // Gestione produzione frutti in HarvestReady
        if (pot.Stage == (int)PlantStage.HarvestReady)
        {
            pot.DaysInHarvestReady++;
            
            // BLK-02.05: Logica corretta per produzione e decay frutti incrementale
            // Il decay graduale inizia DOPO 3 giorni completi con frutti non raccolti
            // Sequenza corretta:
            // - Giorno 1: AmountFruits = 1, DaysFruitsUnharvested = 1
            // - Giorno 2: AmountFruits = 2, DaysFruitsUnharvested = 2
            // - Giorno 3: AmountFruits = 3, DaysFruitsUnharvested = 3
            // - Giorno 4: DaysFruitsUnharvested = 4 → decay graduale: AmountFruits = 2 (perde 1)
            // - Giorno 5: DaysFruitsUnharvested = 5 → decay graduale: AmountFruits = 1 (perde 1)
            // - Giorno 6: DaysFruitsUnharvested = 6 → decay graduale: AmountFruits = 0 (perde 1)
            
            // Produzione frutti incrementale: +1 frutto/giorno fino a 3 max
            // IMPORTANTE: Produciamo PRIMA i frutti, poi gestiamo il decay
            bool phInRange = _phSystem != null && plantData != null && 
                             plantData.IsPhInOptimalRange(_phSystem.CurrentPh);
            bool isFirstFruit = false;
            if (pot.AmountFruits == 0f)
            {
                // Se i frutti sono appena decaduti o è il primo giorno, inizializza
                if (pot.DaysInHarvestReady == 1 || pot.DaysFruitsUnharvested == 0)
                {
                    // Primo giorno o dopo decay completo: inizializza a 1 frutto
                    pot.AmountFruits = 1f;
                    pot.DaysFruitsUnharvested = 0; // Reset contatore quando si produce il primo frutto
                    isFirstFruit = true;
                }
            }
            else if (pot.AmountFruits > 0f && pot.AmountFruits < 3f)
            {
                // Giorni successivi: produzione basata su pH
                if (phInRange)
                {
                    // 30% possibilità doppio frutto se pH in range
                    float fruitsToAdd = (UnityEngine.Random.Range(0f, 1f) < 0.3f) ? 2f : 1f;
                    pot.AmountFruits = Mathf.Min(pot.AmountFruits + fruitsToAdd, 3f);
                }
                else
                {
                    // Possibilità mancata produzione (20%) se pH fuori range
                    if (UnityEngine.Random.Range(0f, 1f) >= 0.2f)
                    {
                        pot.AmountFruits = Mathf.Min(pot.AmountFruits + 1f, 3f);
                    }
                }
            }
            
            // DOPO la produzione, gestisci il decay se ci sono frutti
            if (pot.AmountFruits > 0f)
            {
                // Incrementa il contatore dei giorni con frutti non raccolti
                // Questo contatore viene incrementato ogni giorno che ci sono frutti
                // IMPORTANTE: Se è stato prodotto il primo frutto in questo ciclo, il contatore è già stato resettato a 0
                // quindi l'incremento lo porta a 1 (primo giorno con frutti)
                // Altrimenti, viene semplicemente incrementato
                if (!isFirstFruit)
                {
                    pot.DaysFruitsUnharvested++;
                }
                else
                {
                    // Se è stato prodotto il primo frutto, il contatore è già stato resettato a 0
                    // quindi lo impostiamo a 1 per indicare il primo giorno con frutti
                    pot.DaysFruitsUnharvested = 1;
                }
                
                // Controlla decay graduale DOPO aver incrementato il contatore
                // Il decay graduale inizia dopo 3 giorni completi (DaysFruitsUnharvested > 3)
                // Sequenza corretta:
                // - Giorno 1: DaysFruitsUnharvested = 0 → incrementa a 1 → controlla (1 > 3? NO) → OK, AmountFruits = 1
                // - Giorno 2: DaysFruitsUnharvested = 1 → incrementa a 2 → controlla (2 > 3? NO) → OK, AmountFruits = 2
                // - Giorno 3: DaysFruitsUnharvested = 2 → incrementa a 3 → controlla (3 > 3? NO) → OK, AmountFruits = 3
                // - Giorno 4: DaysFruitsUnharvested = 3 → incrementa a 4 → controlla (4 > 3? SÌ) → decay -1, AmountFruits = 2
                // - Giorno 5: DaysFruitsUnharvested = 4 → incrementa a 5 → controlla (5 > 3? SÌ) → decay -1, AmountFruits = 1
                // - Giorno 6: DaysFruitsUnharvested = 5 → incrementa a 6 → controlla (6 > 3? SÌ) → decay -1, AmountFruits = 0
                if (enableDebugLogs)
                {
                    SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Giorno {pot.DaysInHarvestReady}, Frutti: {pot.AmountFruits:F0}, DaysFruitsUnharvested: {pot.DaysFruitsUnharvested} (prima incremento)");
                }
                
                if (pot.DaysFruitsUnharvested > 3)
                {
                    // Decay graduale: perde 1 frutto al giorno dopo 3 giorni completi
                    float oldAmount = pot.AmountFruits;
                    pot.AmountFruits = Mathf.Max(0f, pot.AmountFruits - 1f);
                    
                    // Se i frutti sono finiti, resetta i contatori
                    if (pot.AmountFruits <= 0f)
                    {
                        pot.AmountFruits = 0f;
                        pot.DaysFruitsUnharvested = 0;
                        pot.DaysInHarvestReady = 0; // Reset anche questo contatore per ripartire da capo
                        if (enableDebugLogs)
                            SporiumLogger.LogWarning(LogCategory.Pot, $"{pot.PotId}: Tutti i frutti decaduti dopo {pot.DaysFruitsUnharvested + 1} giorni non raccolti");
                    }
                    else
                    {
                        if (enableDebugLogs)
                            SporiumLogger.LogWarning(LogCategory.Pot, $"{pot.PotId}: DECAY GRADUALE applicato! Giorno {pot.DaysFruitsUnharvested}, {oldAmount:F0} → {pot.AmountFruits:F0} frutti (perso 1)");
                    }
                }
                else
                {
                    if (enableDebugLogs)
                    {
                        SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Nessun decay (DaysFruitsUnharvested={pot.DaysFruitsUnharvested} <= 3)");
                    }
                }
            }
        }
        else
        {
            // Reset contatori frutti se non è in HarvestReady
            pot.DaysInHarvestReady = 0;
            pot.DaysFruitsUnharvested = 0;
        }
        
        // Verifica pH estremo (≥+80 o ≤-80) opposto alla famiglia pianta
        // phBand è già dichiarato e valutato sopra alla riga 582-585, riutilizziamo quello
        if (_phSystem != null && plantData != null)
        {
            float currentPh = _phSystem.CurrentPh;
            // phBand è già stato valutato sopra (riga 585), riutilizziamo quello
            // Se il blocco sopra non è stato eseguito, phBand è Neutral (default), ma questo blocco
            // ha la stessa condizione, quindi se siamo qui phBand è già stato valutato correttamente
            bool isExtremePh = (phBand == PhSystem.PhBand.UltraAcid || 
                                phBand == PhSystem.PhBand.UltraBasic);
            
            // Verifica se pH è opposto alla famiglia pianta
            bool isOppositeToFamily = (plantData.Family == PlantFamily.Pure && 
                                        phBand == PhSystem.PhBand.UltraAcid) ||
                                       (plantData.Family == PlantFamily.Evil && 
                                        phBand == PhSystem.PhBand.UltraBasic);
            
            if (isExtremePh && isOppositeToFamily)
            {
                pot.DaysInExtremePh++;
                pot.ExtremePhDeathCountdown = 3 - pot.DaysInExtremePh;
                
                // Se countdown raggiunge 0, pianta muore
                if (pot.ExtremePhDeathCountdown <= 0)
                {
                    // Morte pianta
                    KillPlantFromExtremePh(pot, plantData, phBand);
                }
                else
                {
                    // Mostra notifica countdown
                    ShowExtremePhCountdownNotification(pot, plantData, pot.ExtremePhDeathCountdown);
                }
            }
            else
            {
                // Reset se pH non è più estremo o non opposto
                pot.DaysInExtremePh = 0;
                pot.ExtremePhDeathCountdown = -1;
            }
        }
        
        // BLK-02.02: Verifica requisiti per avanzamento stadio
        bool stageChanged = false;
        PlantStage currentStage = (PlantStage)pot.Stage;
        
        // Ottieni requisiti per lo stadio corrente
        StageRequirements currentStageReq = plantData.GetStageRequirements(currentStage);
        
        // BLK-03.01-T2: Ottieni condizione corrente e verifica se blocca avanzamento
        // Nota: currentCondition è già definito sopra alla riga 574, riutilizziamo quella variabile
        if (ConditionGrowthModifier.BlocksAdvancement(currentCondition))
        {
            if (enableDebugLogs)
                SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Avanzamento bloccato - Condizione: {currentCondition}");
            // Non può avanzare, ma continua con il resto della logica (produzione frutti, etc.)
        }
        
        // MOLD SYNERGY: Verifica blocco crescita per Mold Risk (considera famiglia)
        // EVIL: NON viene bloccata da Mold Risk (solo da altre condizioni)
        // PURE: bloccata a Mold Risk Level ≥1 (più sensibile)
        // Standard: bloccata a Mold Risk Level ≥2 (sistema attuale)
        bool isBlockedByMoldRisk = false;
        if (plantData != null)
        {
            switch (plantData.Family)
            {
                case PlantFamily.Evil:
                    // EVIL: NON bloccata da Mold Risk
                    isBlockedByMoldRisk = false;
                    break;
                case PlantFamily.Pure:
                    // PURE: bloccata a Mold Risk Level ≥1 (più sensibile)
                    isBlockedByMoldRisk = pot.MoldRiskLevel >= 1;
                    break;
                case PlantFamily.Standard:
                default:
                    // Standard: bloccata a Mold Risk Level ≥2 (sistema attuale)
                    isBlockedByMoldRisk = pot.MoldRiskLevel >= 2;
                    break;
            }
        }
        else
        {
            // Fallback: usa sistema attuale se PlantData non disponibile
            isBlockedByMoldRisk = pot.MoldRiskLevel >= 2;
        }
        
        if (isBlockedByMoldRisk)
        {
            if (enableDebugLogs)
                SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Avanzamento bloccato - Mold Risk Level: {pot.MoldRiskLevel} (Famiglia: {plantData?.Family ?? PlantFamily.Standard})");
            // Non può avanzare, ma continua con il resto della logica
        }
        
        // Verifica se i requisiti sono soddisfatti
        bool requirementsMet = false;
        if (currentStageReq != null)
        {
            // Verifica idratazione nel range
            bool hydrationOk = currentStageReq.IsHydrationInRange(hydrationPercent);
            
            // Verifica LED richiesto (BLK-02.07: usa LedSystemState invece di LastLedType)
            // DEBUG_SAFE_FIX: Se LED è OFF ma lo stress è nel range ottimale, considera OK
            bool ledOk = false;
            if (pot.LedSystemState == LedSystemState.Off)
            {
                // Quando LED è OFF, verifica se lo stress è nel range ottimale (tra 0% e 100%)
                int consecutiveDays = pot.GetConsecutiveLedDays();
                int maxDaysForFullStress = _potSystemConfig != null ? _potSystemConfig.MaxDaysForFullStress : 5;
                float stressPercentage = Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f;
                // Stress è nel range ottimale se è tra 0% e 100% (esclusi gli estremi)
                bool stressInOptimalRange = stressPercentage > 0f && stressPercentage < 100f;
                ledOk = stressInOptimalRange; // OK se stress nel range anche con LED OFF
                
                SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_ADVANCEMENT] {pot.PotId} LED OFF: ConsecutiveDays={consecutiveDays}, Stress%={stressPercentage:F1}, InOptimalRange={stressInOptimalRange}, LedOk={ledOk}");
            }
            else
            {
                ledOk = currentStageReq.IsLedRequirementMet(pot.LedSystemState);
            }
            
            // BLK-03.01-T2: Verifica giorni minimi con modificatore condizione
            int daysModifier = ConditionGrowthModifier.GetDaysModifier(currentCondition);
            int phDaysModifier = 0;
            if (_phSystem != null && plantData != null)
            {
                float currentPh = _phSystem.CurrentPh;
                if (plantData.IsPhInOptimalRange(currentPh))
                {
                    phDaysModifier = -1; // Riduce di 1 giorno se pH in range
                }
            }
            int effectiveRequiredDays = currentStageReq.durationDays + daysModifier + phDaysModifier;
            bool durationOk = pot.DaysInCurrentStage >= effectiveRequiredDays;
            
            // BLK-03.01-T2: Verifica anche giorni consecutivi ottimali
            // DEBUG_SAFE_FIX: Per Seed, rendiamo i giorni consecutivi ottimali meno stringenti (almeno 1 giorno)
            bool optimalDaysOk = false;
            if (currentStage == PlantStage.Seed)
            {
                // Per Seed: almeno 1 giorno ottimale (più flessibile)
                optimalDaysOk = pot.DaysConsecutiveOptimal >= 1;
            }
            else
            {
                // Per altri stadi: richiedi giorni consecutivi >= durationDays
                optimalDaysOk = pot.DaysConsecutiveOptimal >= currentStageReq.durationDays;
            }
            
            // BLK-03.01-T2: Verifica anche fertilizzante nel range
            // DEBUG_SAFE_FIX: Per Seed e Sprout (stadi pre-Growth), rendiamo il fertilizzante opzionale (non bloccante se è 0%)
            // DEBUG_SAFE_FIX: Il fertilizzante over range NON blocca, solo under range blocca
            bool fertilizerOk = false;
            if (currentStage == PlantStage.Seed || currentStage == PlantStage.Sprout)
            {
                // Per Seed e Sprout: fertilizzante opzionale - OK se è nel range OPPURE se è 0% (non ancora applicato) OPPURE se è over range
                fertilizerOk = currentStageReq.IsFertilizerInRange(pot.FertilizerLevel) || pot.FertilizerLevel == 0 || pot.FertilizerLevel > currentStageReq.fertilizerMax;
            }
            else
            {
                // Per Growth e stadi successivi: fertilizzante OK se è >= min (over range è OK, solo under range blocca)
                fertilizerOk = pot.FertilizerLevel >= currentStageReq.fertilizerMin;
            }
            
            // BLK-03.01-T2: Verifica punti accumulati
            // BUG FIX: Per Seed e Sprout, richiediamo solo 2 punti (water + light), fertilizzante opzionale
            int totalPoints = pot.GrowthPointsWater + pot.GrowthPointsLight + pot.GrowthPointsFertilizer;
            int requiredPoints = (currentStage == PlantStage.Seed || currentStage == PlantStage.Sprout) ? 2 : 3;  // Seed/Sprout: 2 punti (water+light), altri: 3 punti
            bool pointsOk = totalPoints >= requiredPoints;
            
            // BLK-03.01-T2: Avanzamento richiede tutti i requisiti E non deve essere bloccato dalla condizione
            // MOLD SYNERGY: Blocco Mold Risk considerato sopra (isBlockedByMoldRisk già calcolato)
            bool isBlockedByCondition = ConditionGrowthModifier.BlocksAdvancement(currentCondition);
            requirementsMet = !isBlockedByCondition && !isBlockedByMoldRisk &&
                             hydrationOk && ledOk && durationOk && optimalDaysOk && fertilizerOk && pointsOk;
            
            // DEBUG: Log requisiti avanzamento (per capire perché non avanza)
            int optimalDaysRequired = (currentStage == PlantStage.Seed) ? 1 : currentStageReq.durationDays;
            bool blocksAdvancement = ConditionGrowthModifier.BlocksAdvancement(currentCondition);
            // isBlockedByMoldRisk è già definita sopra alla riga 817
            
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_ADVANCEMENT] {pot.PotId} Day={dayIndex} Stage={currentStage}: Hydration={hydrationOk} ({hydrationPercent}%), LED={ledOk} ({pot.LedSystemState}), Duration={durationOk} ({pot.DaysInCurrentStage}/{effectiveRequiredDays}), OptimalDays={optimalDaysOk} ({pot.DaysConsecutiveOptimal}/{optimalDaysRequired}), Fertilizer={fertilizerOk} ({pot.FertilizerLevel}%, range={currentStageReq.fertilizerMin}-{currentStageReq.fertilizerMax}), Points={pointsOk} ({totalPoints}/{requiredPoints}), Condition={currentCondition} (blocks={blocksAdvancement}), MoldBlock={isBlockedByMoldRisk} (Level={pot.MoldRiskLevel}, Family={plantData?.Family ?? PlantFamily.Standard}), RequirementsMet={requirementsMet}");
            
            // DEBUG: Log quando l'avanzamento è bloccato
            if (!requirementsMet)
            {
                SporiumLogger.LogWarning(LogCategory.Pot, $"[DEBUG_ADVANCEMENT_FAILED] {pot.PotId} Day={dayIndex} Stage={currentStage}: AVANZAMENTO BLOCCATO - Hydration={hydrationOk}, LED={ledOk}, Duration={durationOk}, OptimalDays={optimalDaysOk}, Fertilizer={fertilizerOk}, Points={pointsOk}, BlockedByCondition={isBlockedByCondition}, BlockedByMoldRisk={isBlockedByMoldRisk} (Level={pot.MoldRiskLevel}, Family={plantData?.Family ?? PlantFamily.Standard})");
            }
            
            // Log critico: Verifica requisiti avanzamento stadio
            SporiumLogger.LogDebugWithLocation(
                LogCategory.Pot,
                "DayCycleController:ResolveGrowthForPot:ADVANCEMENT_CHECK",
                $"Verifica Avanzamento - PotId={pot.PotId}, Day={dayIndex}",
                new {
                    potId = pot.PotId,
                    day = dayIndex,
                    stage = currentStage.ToString(),
                    hydrationOk = hydrationOk,
                    ledOk = ledOk,
                    durationOk = durationOk,
                    daysInCurrentStage = pot.DaysInCurrentStage,
                    effectiveRequiredDays = effectiveRequiredDays,
                    optimalDaysOk = optimalDaysOk,
                    daysConsecutiveOptimal = pot.DaysConsecutiveOptimal,
                    optimalDaysRequired = optimalDaysRequired,
                    fertilizerOk = fertilizerOk,
                    fertilizerLevel = pot.FertilizerLevel,
                    pointsOk = pointsOk,
                    totalPoints = totalPoints,
                    requiredPoints = requiredPoints,
                    currentCondition = currentCondition.ToString(),
                    blocksAdvancement = blocksAdvancement,
                    requirementsMet = requirementsMet
                },
                "H",
                "debug"
            );
            
            if (enableDebugLogs)
            {
                SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Stage {currentStage} requisiti - " +
                         $"Hydration: {hydrationPercent}% (range: {currentStageReq.hydrationMin}-{currentStageReq.hydrationMax}) [{hydrationOk}], " +
                         $"LED: {pot.LedSystemState} (richiesto: {currentStageReq.GetRequiredLed()}) [{ledOk}], " +
                         $"Durata: {pot.DaysInCurrentStage}/{effectiveRequiredDays} giorni (mod: {daysModifier}) [{durationOk}], " +
                         $"OptimalDays: {pot.DaysConsecutiveOptimal}/{optimalDaysRequired} [{optimalDaysOk}], " +
                         $"Fertilizer: {pot.FertilizerLevel}% (range: {currentStageReq.fertilizerMin}-{currentStageReq.fertilizerMax}, opzionale per Seed: {currentStage == PlantStage.Seed}) [{fertilizerOk}], " +
                         $"Points: {totalPoints}/{requiredPoints} [{pointsOk}], " +
                         $"Condition: {currentCondition} (blocks: {ConditionGrowthModifier.BlocksAdvancement(currentCondition)})");
            }
        }
        else
        {
            // Se non ci sono requisiti specifici, considera sempre soddisfatti
            requirementsMet = true;
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_ADVANCEMENT] {pot.PotId} Day={dayIndex}: Nessun requisito specifico per stage {currentStage}, avanzamento automatico");
            if (enableDebugLogs)
                SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Nessun requisito specifico per stage {currentStage}, avanzamento automatico");
        }
        
        // BLK-02.02: Avanzamento stadi con requisiti specifici
        if (requirementsMet)
        {
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_ADVANCEMENT_SUCCESS] {pot.PotId} Day={dayIndex}: TUTTI I REQUISITI SODDISFATTI per avanzare da {currentStage}");
            switch (currentStage)
            {
                case PlantStage.Seed:
                    // Seed → Sprout: richiede requisiti soddisfatti
                    pot.Stage = (int)PlantStage.Sprout;
                    pot.DaysInCurrentStage = 0; // Reset contatore giorni
                    // BLK-03.01-T2: Reset contatori punti dopo avanzamento
                    pot.GrowthPointsWater = 0;
                    pot.GrowthPointsLight = 0;
                    pot.GrowthPointsFertilizer = 0;
                    pot.DaysConsecutiveOptimal = 0;
                    pot.DayOptimalParametersStarted = -1;
                    stageChanged = true;
                    if (enableDebugLogs)
                        SporiumLogger.LogInfo(LogCategory.Pot, $"{pot.PotId}: Avanzamento Seed → Sprout!");
                    break;
                    
                case PlantStage.Sprout:
                    // Sprout → Growth: richiede requisiti soddisfatti
                    pot.Stage = (int)PlantStage.Growth;
                    pot.DaysInCurrentStage = 0;
                    // BLK-03.01-T2: Reset contatori punti dopo avanzamento
                    pot.GrowthPointsWater = 0;
                    pot.GrowthPointsLight = 0;
                    pot.GrowthPointsFertilizer = 0;
                    pot.DaysConsecutiveOptimal = 0;
                    pot.DayOptimalParametersStarted = -1;
                    stageChanged = true;
                    if (enableDebugLogs)
                        SporiumLogger.LogInfo(LogCategory.Pot, $"{pot.PotId}: Avanzamento Sprout → Growth!");
                    break;
                    
                case PlantStage.Growth:
                    // Growth → Flowering: richiede 2 giorni consecutivi con requisiti soddisfatti
                    // (verificato tramite durationDays >= 2)
                    pot.Stage = (int)PlantStage.Flowering;
                    pot.DaysInCurrentStage = 0;
                    // BLK-03.01-T2: Reset contatori punti dopo avanzamento
                    pot.GrowthPointsWater = 0;
                    pot.GrowthPointsLight = 0;
                    pot.GrowthPointsFertilizer = 0;
                    pot.DaysConsecutiveOptimal = 0;
                    pot.DayOptimalParametersStarted = -1;
                    stageChanged = true;
                    if (enableDebugLogs)
                        SporiumLogger.LogInfo(LogCategory.Pot, $"{pot.PotId}: Avanzamento Growth → Flowering!");
                    break;
                    
                case PlantStage.Flowering:
                    // Flowering → HarvestReady: richiede requisiti soddisfatti
                    pot.Stage = (int)PlantStage.HarvestReady;
                    pot.DaysInCurrentStage = 0;
                    pot.DaysInHarvestReady = 0; // Reset contatore HarvestReady
                    pot.AmountFruits = 0f; // Inizializza frutti
                    // BLK-03.01-T2: Reset contatori punti dopo avanzamento
                    pot.GrowthPointsWater = 0;
                    pot.GrowthPointsLight = 0;
                    pot.GrowthPointsFertilizer = 0;
                    pot.DaysConsecutiveOptimal = 0;
                    pot.DayOptimalParametersStarted = -1;
                    stageChanged = true;
                    if (enableDebugLogs)
                        SporiumLogger.LogInfo(LogCategory.Pot, $"{pot.PotId}: Avanzamento Flowering → HarvestReady!");
                    break;
                    
                case PlantStage.HarvestReady:
                    // HarvestReady → Resting: dopo un certo numero di giorni (gestito da durationDays)
                    // Per ora rimane in HarvestReady fino a raccolta manuale
                    // Il passaggio a Resting sarà gestito dall'azione di raccolta
                    break;
                    
                case PlantStage.Resting:
                    // Resting → Empty: dopo un certo numero di giorni (gestito da durationDays)
                    // Per ora rimane in Resting fino a rimozione manuale
                    break;
            }
        }
        
        // BLK-02.02: Emetti eventi per notificare crescita e/o cambio di stadio
        if (stageChanged)
        {
            // Notifica il PotGrowthController per aggiornare le visuali
            var potGrowthController = FindPotGrowthController(pot.PotId);
            if (potGrowthController != null)
            {
                if (enableDebugLogs)
                    SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Trovato PotGrowthController, chiamando OnStageChanged...");
                potGrowthController.OnStageChanged((PlantStage)pot.Stage);
            }
            else
            {
                if (enableDebugLogs)
                    SporiumLogger.LogWarning(LogCategory.Pot, $"{pot.PotId}: PotGrowthController NON TROVATO! Le visuali non saranno aggiornate.");
            }
            
            // Emetti evento per l'UI
            PotEvents.EmitPlantStageChanged(pot.PotId, (PlantStage)pot.Stage);
            
            // Toast cambio stadio via Foundation
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
            {
                foundation.PostToast(
                    "STAGE-UP-001",
                    new NotificationPayload()
                        .With("potId", pot.PotId)
                        .With("stage", ((PlantStage)pot.Stage).ToString()));
            }
            
            if (enableDebugLogs)
                SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Eventi emessi per cambio stadio {oldStage} → {pot.Stage}");
        }
        
        // BLK-02.02: Emetti evento di crescita (sempre, per aggiornare progress bar)
        PotEvents.RaiseOnPlantGrew(pot.PotId, (PlantStage)pot.Stage, 0, pot.DaysInCurrentStage);

        // Aggiorna contatori
        pot.DaysSincePlant++;
        if (!hadHydration && !hadLight)
        {
            pot.DaysNeglectedStreak++;
        }
        else
        {
            pot.DaysNeglectedStreak = 0;
        }
    }
    
    /// <summary>
    /// Sistema legacy di crescita basato su punti (fallback se PlantData non disponibile)
    /// </summary>
    private void ResolveGrowthForPotLegacy(PotStateModel pot, int dayIndex)
    {
        // GDD AZ-11: Usa WateringSystemOn invece di LastWateredDay per coerenza
        bool hadHydration = pot.WateringSystemOn;
        bool hadLight = (pot.LastLitDay == dayIndex - 1);
        
        int gained = 0;
        if (hadHydration && hadLight)
        {
            gained = growthConfig.pointsIdealCare;
        }
        else if (hadHydration || hadLight)
        {
            gained = growthConfig.pointsPartialCare;
        }
        else
        {
            gained = growthConfig.pointsNoCare;
        }
        
        pot.GrowthPoints += gained;
        
        bool stageChanged = false;
        int oldStage = pot.Stage;
        
        if (pot.Stage == (int)PlantStage.Seed && pot.GrowthPoints >= growthConfig.pointsSeedToSprout)
        {
            pot.GrowthPoints -= growthConfig.pointsSeedToSprout;
            pot.Stage = (int)PlantStage.Sprout;
            stageChanged = true;
        }
        else if (pot.Stage == (int)PlantStage.Sprout && pot.GrowthPoints >= growthConfig.pointsSproutToMature)
        {
            pot.GrowthPoints -= growthConfig.pointsSproutToMature;
            pot.Stage = (int)PlantStage.HarvestReady;
            stageChanged = true;
        }

        if (pot.Stage == (int)PlantStage.HarvestReady && !stageChanged)
            pot.AmountFruits = (pot.AmountFruits + 0.5f) % 10;
        
        if (stageChanged)
        {
            var potGrowthController = FindPotGrowthController(pot.PotId);
            if (potGrowthController != null)
                potGrowthController.OnStageChanged((PlantStage)pot.Stage);
            
            PotEvents.EmitPlantStageChanged(pot.PotId, (PlantStage)pot.Stage);
        }
        
        if (gained > 0 || stageChanged)
        {
            PotEvents.RaiseOnPlantGrew(pot.PotId, (PlantStage)pot.Stage, gained, pot.GrowthPoints);
        }

        pot.DaysSincePlant++;
        if (gained == 0)
        {
            pot.DaysNeglectedStreak++;
        }
        else
        {
            pot.DaysNeglectedStreak = 0;
        }
    }

    /// <summary>
    /// Verifica risorse disponibili per sistemi irrigazione e mostra warning preventivo (GDD AZ-11)
    /// </summary>
    private void CheckWateringSystemResources()
    {
        if (_gameManager == null)
        {
            TryGetGameManager();
            if (_gameManager == null)
            {
                if (enableDebugLogs)
                    SporiumLogger.LogWarning(LogCategory.Core, "GameManager non disponibile per verifica risorse watering");
                return;
            }
        }
        
        int vasiOnCount = 0;
        int vasiDaDisattivare = 0;
        List<string> vasiDaDisattivareList = new List<string>();
        
        foreach (var pot in _registeredPots)
        {
            if (pot != null && pot.HasPlant && pot.WateringSystemOn)
            {
                vasiOnCount++;
                
                // BUG FIX: Verifica WAT-RAW disponibile (non solo se accumulatore >= 1.0)
                // Se non c'è WAT-RAW, il sistema verrà disattivato
                if (!_gameManager.PlayerInventory.Has(Items.Water))
                {
                    vasiDaDisattivare++;
                    vasiDaDisattivareList.Add(pot.PotId);
                }
            }
        }
        
        if (vasiDaDisattivare > 0)
        {
            // Mostra warning toast (se disponibile sistema UI)
            string message = $"⚠️ WAT-RAW insufficiente. {vasiDaDisattivare} sistemi irrigazione verranno disattivati.";
            if (enableDebugLogs)
            {
                SporiumLogger.LogWarning(LogCategory.Pot, $"{message} Vasi: {string.Join(", ", vasiDaDisattivareList)}");
            }

            // Notifications Foundation (preferred). Fallback: old toast/banner.
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
            {
                // Pre-warning: deve restare un TOAST (non persistente), anche se LGT-002 è severity Danger di default.
                foundation.PostToast("LGT-002",
                    new NotificationPayload().With("message", message),
                    severityOverride: NotificationSeverity.Warning,
                    dedupKey: "WATRAW:WILL_DISABLE");
            }
        }
    }
    
    /// <summary>
    /// Applica effetti del sistema irrigazione a fine giornata (GDD AZ-11 - Toggle Persistente)
    /// Gestisce consumo risorse, idratazione, evaporazione, overwatering e fallback automatico
    /// </summary>
    private void ApplyWateringSystemEffects()
    {
        if (_gameManager == null)
        {
            TryGetGameManager();
            if (_gameManager == null)
            {
                if (enableDebugLogs)
                    SporiumLogger.LogWarning(LogCategory.Core, "GameManager non disponibile per applicazione effetti watering");
                return;
            }
        }
        
        int maxHydration = _potSystemConfig != null ? _potSystemConfig.MaxHydration : 10;
        
        // Notifications Foundation: se WAT-RAW è tornato disponibile, risolvi eventuali danger persistenti di auto-spegnimento.
        var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
        bool hasRawWater = _gameManager.PlayerInventory.Has(Items.Water);
        if (foundation != null && foundation.Enabled && hasRawWater)
        {
            foreach (var p in _registeredPots)
            {
                if (p == null) continue;
                foundation.ResolveDanger($"WATRAW:OFF:{p.PotId}");
            }
        }
        
        foreach (var pot in _registeredPots)
        {
            if (pot == null || !pot.HasPlant)
                continue;
            
            // Salva l'idratazione di inizio tick per gestire overwatering persistente
            int hydrationStart = pot.Hydration;
            
            // Sistema ON
            if (pot.WateringSystemOn)
            {
                // BUG FIX: Controlla PRIMA se c'è WAT-RAW disponibile (anche se accumulatore < 1.0)
                // Se non c'è WAT-RAW, disattiva immediatamente il sistema
                if (!hasRawWater)
                {
                    // FALLBACK: Disattiva sistema automaticamente - WAT-RAW insufficiente
                    pot.WateringSystemOn = false;
                    pot.WateringRawWaterAccumulator = 0f;
                    pot.DaysWateringSystemOn = 0;
                    
                    // Messaggio corto (UI Foundation max 3 righe)
                    string message = $"Irrigazione spenta nel {pot.PotId}";
                    if (enableDebugLogs)
                        SporiumLogger.LogWarning(LogCategory.Pot, message);

                    // Notifications Foundation: DANGER persistente finché non torna WAT-RAW disponibile.
                    if (foundation != null && foundation.Enabled)
                        foundation.UpsertDanger($"WATRAW:OFF:{pot.PotId}", "LGT-002", new NotificationPayload().With("message", message));
                    
                    // Rimuovi eventuali contributi overwatering se presenti
                    if (_phSystem != null)
                    {
                        _phSystem.RemoveActionContribution("Overwatering", pot.PotId);
                    }
                    
                    // Emetti evento per UI (solo se PotSlot trovato)
                    PotSlot potSlot = FindPotSlot(pot.PotId);
                    if (potSlot != null)
                    {
                        PotEvents.EmitActionFailed(PotEvents.PotActionType.Water, 
                            potSlot, 
                            "Sistema disattivato: WAT-RAW insufficiente");
                    }
                    else if (enableDebugLogs)
                    {
                        SporiumLogger.LogWarning(LogCategory.Pot, $"PotSlot non trovato per {pot.PotId}, evento UI non emesso");
                    }
                    
                    // Salta al prossimo vaso (sistema disattivato)
                    continue;
                }
                
                // Sistema ON: accumula WAT-RAW e applica idratazione
                pot.WateringRawWaterAccumulator += Sporae.DevTools.DifficultyCalibrationConfig.WateringAccumulator;
                
                // Se accumulatore >= 1.0, consuma 1 WAT-RAW
                if (pot.WateringRawWaterAccumulator >= 1.0f)
                {
                    // WAT-RAW già verificato sopra, quindi consuma
                    _gameManager.PlayerInventory.Consume(Items.Water, 1);
                    pot.WateringRawWaterAccumulator -= 1.0f;
                    
                    if (enableDebugLogs)
                        SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Consumato 1 WAT-RAW (accumulatore: {pot.WateringRawWaterAccumulator:F1})");
                }
                
                // Applica effetti (WAT-RAW già verificato e disponibile)
                if (pot.WateringSystemOn)
                {
                    // Applica +10% idratazione (1 punto se max=10)
                    bool hydrationIncreased = pot.IncreaseHydration(maxHydration);
                    
                    // Consumo CRY (sempre, anche se accumulatore < 1.0)
                    if (!_gameManager.TrySpendCry(2))
                    {
                        if (enableDebugLogs)
                            SporiumLogger.LogWarning(LogCategory.Pot, $"{pot.PotId}: CRY insufficiente per sistema irrigazione (richiesti 2)");
                    }
                    
                    // Incrementa contatore giorni ON
                    pot.DaysWateringSystemOn++;
                    
                    if (enableDebugLogs)
                    {
                        string hydrationMsg = hydrationIncreased ? $"Idratazione: {pot.Hydration}/{maxHydration}" : "Idratazione già al massimo";
                        SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Sistema ON - {hydrationMsg}, Giorni ON: {pot.DaysWateringSystemOn}");
                    }
                }
            }
            else
            {
                // Sistema OFF: nessuna evaporazione qui (il decay avviene in ApplyDecayAndCleanup)
                // Reset contatori
                pot.DaysWateringSystemOn = 0;
                pot.WateringRawWaterAccumulator = 0f;
            }
            
            // Overwatering / rimozione:
            // - Usa l'idratazione di inizio tick per garantire che l'overwatering persista anche se il sistema è OFF e l'idratazione decresce di 1.
            // - Per applicare overwatering dovuto a un aumento (sistema ON che porta sopra soglia), considera anche l'idratazione finale.
            int hydrationForOverCheck = Mathf.Max(hydrationStart, pot.Hydration);
            int overwateringThreshold = Mathf.CeilToInt(maxHydration * Sporae.DevTools.DifficultyCalibrationConfig.OverwateringThresholdPercent / 100f);
            int removalThreshold = Mathf.FloorToInt(maxHydration * Sporae.DevTools.DifficultyCalibrationConfig.OverwateringRemovalPercent / 100f);
            float hydrationPercentForOver = maxHydration > 0 ? (float)hydrationForOverCheck / maxHydration * 100f : 0f;
            float hydrationPercentStart = maxHydration > 0 ? (float)hydrationStart / maxHydration * 100f : 0f;
            
            if (_phSystem != null)
            {
                if (hydrationForOverCheck >= overwateringThreshold)
                {
                    // Applica drift giornaliero configurabile SEMPRE finché condizione attiva
                    _phSystem.RegisterActionDrift(Sporae.DevTools.DifficultyCalibrationConfig.OverwateringPhDrift, "Overwatering", pot.PotId);
                    
                    if (enableDebugLogs)
                        SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: OVERWATERING attivo → pH -5 accodato (HydrationStart:{hydrationStart}/{maxHydration} = {hydrationPercentStart:F0}%, Check:{hydrationForOverCheck}/{maxHydration} = {hydrationPercentForOver:F0}%)");
                }
                else if (hydrationStart < removalThreshold)
                {
                    _phSystem.RemoveActionContribution("Overwatering", pot.PotId);
                    if (enableDebugLogs)
                        SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Overwatering rimosso (HydrationStart:{hydrationStart}/{maxHydration} = {hydrationPercentStart:F0}%)");
                }
            }
        }
    }
    
    /// <summary>
    /// BLK-02.07: Applica effetti sistema LED persistente a fine giornata
    /// </summary>
    private void ApplyLedSystemEffects()
    {
        if (_gameManager == null)
        {
            TryGetGameManager();
            if (_gameManager == null)
            {
                if (enableDebugLogs)
                    SporiumLogger.LogWarning(LogCategory.Core, "GameManager non disponibile per applicazione effetti LED");
                return;
            }
        }
        
        foreach (var pot in _registeredPots)
        {
            if (pot == null || !pot.HasPlant)
                continue;
            
            ApplyLedSystemEffectsForPot(pot);
        }
    }
    
    /// <summary>
    /// BLK-02.07: Applica effetti sistema LED persistente per un singolo vaso
    /// </summary>
    private void ApplyLedSystemEffectsForPot(PotStateModel pot)
    {
        // Salva stato precedente per verificare se è stato spento
        LedSystemState stateBeforeCheck = pot.LedSystemState;
        int oldBlueDays = pot.DaysLedBlueConsecutive;
        int oldRedDays = pot.DaysLedRedConsecutive;
        
        int currentDay = _dayCycleSystem?.CurrentDay ?? 1;
        
        if (pot.LedSystemState == LedSystemState.Off)
        {
            // Sistema OFF: decadimento graduale se era acceso
            bool hadBlueDays = pot.DaysLedBlueConsecutive > 0;
            bool hadRedDays = pot.DaysLedRedConsecutive > 0;

            // DEBUG_SAFE_FIX: Non azzerare completamente i contatori quando LED è OFF
            // Mantieni almeno 1 giorno per evitare che lo stress si azzeri completamente
            // Questo evita drop insensati di condizione quando i parametri sono comunque in range
            if (pot.DaysLedBlueConsecutive > 0)
            {
                pot.DaysLedBlueConsecutive = Mathf.Max(1, pot.DaysLedBlueConsecutive - 1); // Mantieni almeno 1
            }
            if (pot.DaysLedRedConsecutive > 0)
            {
                pot.DaysLedRedConsecutive = Mathf.Max(1, pot.DaysLedRedConsecutive - 1); // Mantieni almeno 1
            }

            if (enableDebugLogs && (hadBlueDays || hadRedDays))
            {
                SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: LED System OFF - Decadimento contatori (Blue: {pot.DaysLedBlueConsecutive}, Red: {pot.DaysLedRedConsecutive})");
            }
            return;
        }
        
        // Incrementa contatori giorni consecutivi
        pot.IncrementConsecutiveLedDays();
        int consecutiveDays = pot.GetConsecutiveLedDays();
        
        // Calcola scaling effetti
        float effectMultiplier = GetLedEffectMultiplier(consecutiveDays);
        float malusMultiplier = GetLedMalusMultiplier(consecutiveDays);
        
        // Applica effetti crescita e pH
        ApplyLedEffects(pot, pot.LedSystemState, effectMultiplier, malusMultiplier, consecutiveDays);
        
        // Consumo CRY notturno
        int cryCost = GetNightlyCryCost(pot.LedSystemState, consecutiveDays);
        if (cryCost > 0)
        {
            if (_gameManager.TrySpendCry(cryCost))
            {
                if (enableDebugLogs)
                    SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Consumo CRY notturno LED: {cryCost} CRY");
            }
            else
            {
                // CRY insufficiente: spegni sistema e notifica
                LedSystemState oldState = pot.LedSystemState;
                pot.SetLedSystemState(LedSystemState.Off);
                
                // BLK-02.07 BUG FIX: Rimuovi contributo pH quando LED viene spento per CRY insufficiente
                if (_phSystem != null && oldState != LedSystemState.Off)
                {
                    // Rimuovi tutti i contributi LED per questo vaso
                    _phSystem.RemoveActionContribution("BlueLED", pot.PotId);
                    _phSystem.RemoveActionContribution("RedLED", pot.PotId);
                    _phSystem.RemoveActionContribution("BlueLED_x1.5", pot.PotId);
                    _phSystem.RemoveActionContribution("BlueLED_x2", pot.PotId);
                    _phSystem.RemoveActionContribution("RedLED_x1.5", pot.PotId);
                    _phSystem.RemoveActionContribution("RedLED_x2", pot.PotId);
                    
                    if (enableDebugLogs)
                        SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Contributo pH LED rimosso (CRY insufficiente, LED spento: {oldState} → Off)");
                }
                
                ShowLedNotification($"LGT-002: Sistema LED {pot.PotId} spento - CRY insufficiente", Color.yellow);
                if (enableDebugLogs)
                    SporiumLogger.LogWarning(LogCategory.Pot, $"{pot.PotId}: CRY insufficiente per LED, sistema spento");
            }
        }
        
        // Toast avviso zona rossa (100% stress = maxDaysForFullStress giorni)
        int maxDaysForFullStress = GetMaxDaysForFullStress();
        if (consecutiveDays >= maxDaysForFullStress)
        {
            ShowLedNotification($"LGT-003: LED {pot.LedSystemState} attivo {consecutiveDays} giorni - Zona rossa!", Color.red);
        }
    }
    
    /// <summary>
    /// BLK-02.07: Calcola moltiplicatore effetti LED in base a giorni consecutivi
    /// </summary>
    private float GetLedEffectMultiplier(int consecutiveDays)
    {
        if (consecutiveDays == 1) return Sporae.DevTools.DifficultyCalibrationConfig.LedMultiplierDay1;
        if (consecutiveDays >= 2 && consecutiveDays <= 3) return Sporae.DevTools.DifficultyCalibrationConfig.LedMultiplierDays2_3;
        if (consecutiveDays >= 4) return Sporae.DevTools.DifficultyCalibrationConfig.LedMultiplierDay4Plus;
        return Sporae.DevTools.DifficultyCalibrationConfig.LedMultiplierDay1;
    }
    
    /// <summary>
    /// BLK-02.07: Calcola moltiplicatore malus LED in base a giorni consecutivi
    /// </summary>
    private float GetLedMalusMultiplier(int consecutiveDays)
    {
        if (consecutiveDays <= 3) return Sporae.DevTools.DifficultyCalibrationConfig.LedMalusBase;
        if (consecutiveDays >= 4) return Sporae.DevTools.DifficultyCalibrationConfig.LedMalusGrowth + (consecutiveDays - 4) * Sporae.DevTools.DifficultyCalibrationConfig.LedMalusIncrementPerDay;
        return Sporae.DevTools.DifficultyCalibrationConfig.LedMalusBase;
    }
    
    /// <summary>
    /// BLK-02.07: Calcola consumo CRY notturno per sistema LED
    /// </summary>
    private int GetNightlyCryCost(LedSystemState state, int consecutiveDays)
    {
        switch (state)
        {
            case LedSystemState.Blue:
                return 1 + (consecutiveDays / 2);  // 1, 1, 2, 2, 3...
            case LedSystemState.Red:
                return 2 + consecutiveDays;        // 2, 3, 4, 5... (più costoso)
            default:
                return 0;
        }
    }
    
    /// <summary>
    /// BLK-02.07: Applica effetti LED (pH, crescita, stress)
    /// </summary>
    private void ApplyLedEffects(PotStateModel pot, LedSystemState state, float effectMultiplier, float malusMultiplier, int consecutiveDays)
    {
        if (state == LedSystemState.Off) return;
        
        // Converti LedSystemState a LedType per compatibilità
        LedType ledType = state == LedSystemState.Blue ? LedType.Blue : LedType.Red;
        
        // Effetti pH (con scaling)
        if (_phSystem != null)
        {
            float basePhDelta = ledType == LedType.Blue ? Sporae.DevTools.DifficultyCalibrationConfig.PhDriftLedBlue : Sporae.DevTools.DifficultyCalibrationConfig.PhDriftLedRed;
            float phDelta = basePhDelta * effectMultiplier;
            string actionName = ledType == LedType.Blue ? "BlueLED" : "RedLED";
            
            // Aggiungi moltiplicatore al nome azione per tooltip
            if (consecutiveDays >= 4)
                actionName += "_x2";
            else if (consecutiveDays >= 2)
                actionName += "_x1.5";
            
            _phSystem.RegisterActionDrift(phDelta, actionName, pot.PotId);
            if (enableDebugLogs)
                SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: LED {state} giorno {consecutiveDays} - pH {(phDelta > 0 ? "+" : "")}{phDelta:F1} (mult: {effectMultiplier:F1})");
        }
        
        // Effetti crescita (Light Exposure)
        // BUG FIX: Se LightExposure è stato impostato manualmente, preserva il valore base
        // Il LED può aumentare LightExposure sopra il valore base, ma il valore base viene preservato
        int maxLightExposure = GetMaxLightExposureForPot(pot);
        if (pot.LightExposure < maxLightExposure)
        {
            pot.IncreaseLightExposure(maxLightExposure);
            
            // Se LightExposure è stato impostato manualmente, aggiorna il valore base se è più basso del valore attuale
            // Questo permette al LED di aumentare LightExposure sopra il valore base, ma preserva il valore base per quando LED è spento
            if (pot.IsLightExposureManuallySet && pot.ManualLightExposureBase >= 0)
            {
                // Il valore base rimane quello impostato manualmente, ma LightExposure può essere aumentato dal LED
                // Quando LED è spento, LightExposure tornerà al valore base
            }
        }
        
        // FASE 3.1: Applicazione completa Burn Stress
        int maxDaysForFullStress = GetMaxDaysForFullStress();
        
        // Verifica se Burn Stress è attivo (stress = 100%)
        bool isBurnStressActive = consecutiveDays >= maxDaysForFullStress;
        
        if (isBurnStressActive)
        {
            // Incrementa contatore giorni Burn Stress consecutivi
            pot.DaysBurnStressConsecutive++;
            
            if (enableDebugLogs)
                SporiumLogger.LogWarning(LogCategory.Pot, $"[BURN_STRESS] {pot.PotId}: Burn Stress attivo - {consecutiveDays} giorni consecutivi (max: {maxDaysForFullStress}), DaysBurnStress: {pot.DaysBurnStressConsecutive}");
            
            // FASE 3.2: Effetti estremi dopo 3 giorni consecutivi
            if (pot.DaysBurnStressConsecutive >= 3)
            {
                PlantStage currentStage = (PlantStage)pot.Stage;
                var foundationBurn = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                
                // Regressione stage (torna allo stadio precedente)
                if (currentStage > PlantStage.Seed)
                {
                    PlantStage previousStage = currentStage - 1;
                    pot.Stage = (int)previousStage;
                    pot.DaysInCurrentStage = 0;
                    
                    if (foundationBurn != null && foundationBurn.Enabled)
                    {
                        foundationBurn.PostToast("BURN-STAGE-REGRESS",
                            new NotificationPayload()
                                .With("potId", pot.PotId)
                                .With("oldStage", currentStage.ToString())
                                .With("newStage", previousStage.ToString()));
                    }
                    if (enableDebugLogs)
                        SporiumLogger.LogWarning(LogCategory.Pot, $"[BURN_STRESS_EXTREME] {pot.PotId}: Regressione stage da {currentStage} a {previousStage} (Burn Stress {pot.DaysBurnStressConsecutive} giorni)");
                }
                
                // Riduzione livello (-1 livello, minimo 1)
                if (pot.PlantLevel > 1)
                {
                    int oldLevel = pot.PlantLevel;
                    pot.PlantLevel--;
                    if (foundationBurn != null && foundationBurn.Enabled)
                    {
                        foundationBurn.PostToast("PLT-LVL-DOWN",
                            new NotificationPayload()
                                .With("potId", pot.PotId)
                                .With("plantCode", pot.PlantCode ?? "?")
                                .With("oldLevel", oldLevel.ToString())
                                .With("newLevel", pot.PlantLevel.ToString())
                                .With("reason", "Burn Stress"));
                    }
                    if (enableDebugLogs)
                        SporiumLogger.LogWarning(LogCategory.Pot, $"[BURN_STRESS_EXTREME] {pot.PotId}: Riduzione livello da {oldLevel} a {pot.PlantLevel} (Burn Stress {pot.DaysBurnStressConsecutive} giorni)");
                }
                
                // Reset contatore dopo aver applicato effetti estremi (per evitare applicazione multipla)
                pot.DaysBurnStressConsecutive = 0;
            }
        }
        else
        {
            // Reset contatore se Burn Stress non è più attivo
            pot.DaysBurnStressConsecutive = 0;
        }
        if (consecutiveDays >= maxDaysForFullStress && enableDebugLogs)
        {
            SporiumLogger.LogWarning(LogCategory.Pot, $"{pot.PotId}: LED {state} attivo {consecutiveDays} giorni - Zona rossa! (Malus mult: {malusMultiplier:F1})");
        }
    }
    
    /// <summary>
    /// BLK-02.07: Ottiene max light exposure per un vaso (helper)
    /// </summary>
    private int GetMaxLightExposureForPot(PotStateModel pot)
    {
        return _potSystemConfig != null ? _potSystemConfig.MaxLightExposure : 3;
    }
    
    /// <summary>
    /// BLK-02.07: Ottiene max days for full stress (helper)
    /// </summary>
    private int GetMaxDaysForFullStress()
    {
        return _potSystemConfig != null ? _potSystemConfig.MaxDaysForFullStress : 5;
    }
    
    /// <summary>
    /// BLK-02.07: Mostra notifica LED via Foundation
    /// </summary>
    private void ShowLedNotification(string message, Color color)
    {
        var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
        if (foundation != null && foundation.Enabled)
        {
            string code = message.Contains("Blue") ? "LGT-003" : message.Contains("Red") ? "LGT-004" : "LGT-001";
            var sev = (message.Contains("CRY insufficiente") || message.Contains("spento"))
                ? NotificationSeverity.Warning
                : NotificationSeverity.Info;
            foundation.PostToast(code, new NotificationPayload().With("message", message), sev);
        }
    }
    
    /// <summary>
    /// Trova PotSlot per un PotId (helper per eventi)
    /// </summary>
    private PotSlot FindPotSlot(string potId)
    {
        TryGetPotRegistry();
        if (_potRegistry != null)
            return _potRegistry.FindPotById(potId);

        var allPots = FindObjectsOfType<PotSlot>();
        foreach (var pot in allPots)
        {
            if (pot != null && pot.PotId == potId)
                return pot;
        }
        return null;
    }
    
    /// <summary>
    /// Applica decadimento e pulizia (SENZA reset dei timestamp!)
    /// GDD AZ-11: Decadimento idratazione applicato SOLO se sistema irrigazione è OFF
    /// </summary>
    private void ApplyDecayAndCleanup(int dayIndex)
    {
        int maxHydration = _potSystemConfig != null ? _potSystemConfig.MaxHydration : 10;
        
        foreach (var pot in _registeredPots)
        {
            if (pot != null && pot.HasPlant)
            {
                // GDD AZ-11: Decadimento idratazione SOLO se sistema irrigazione è OFF
                // Se sistema è ON, il decadimento è già compensato dall'aumento di idratazione
                // BUG FIX: Se Hydration è stato impostato manualmente, applica decay partendo dal valore base salvato
                if (!pot.WateringSystemOn)
                {
                    int oldHydration = pot.Hydration;
                    
                    if (pot.IsHydrationManuallySet && pot.ManualHydrationBase >= 0)
                    {
                        // Decay partendo dal valore base manuale
                        int newHydration = Mathf.Max(0, pot.ManualHydrationBase - growthConfig.dailyHydrationDecay);
                        pot.Hydration = newHydration;
                        pot.ManualHydrationBase = newHydration; // Aggiorna base per il prossimo giorno
                        
                        if (enableDebugLogs && oldHydration != pot.Hydration)
                        {
                            SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Decay applicato (MANUALE, base={pot.ManualHydrationBase}) - Hydration: {oldHydration} → {pot.Hydration}/{maxHydration}");
                        }
                    }
                    else
                    {
                        // Decay normale
                        pot.Hydration = Mathf.Max(0, pot.Hydration - growthConfig.dailyHydrationDecay);
                        
                        if (enableDebugLogs && oldHydration != pot.Hydration)
                        {
                            SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Decay applicato (sistema OFF) - Hydration: {oldHydration} → {pot.Hydration}/{maxHydration}");
                        }
                    }
                }
                else
                {
                    if (enableDebugLogs)
                    {
                        SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Decay saltato (sistema ON) - Hydration: {pot.Hydration}/{maxHydration}");
                    }
                }
                
                // BUG FIX: Reset esposizione luce SOLO se non è stato impostato manualmente
                // Se è stato impostato manualmente, preserva il valore base quando LED è spento
                if (pot.IsLightExposureManuallySet && pot.ManualLightExposureBase >= 0)
                {
                    // Se LED è spento, ripristina il valore base manuale
                    // Se LED è acceso, il valore è già stato aumentato da ApplyLedSystemEffects, quindi non lo tocchiamo
                    if (pot.LedSystemState == LedSystemState.Off)
                    {
                        pot.LightExposure = pot.ManualLightExposureBase;
                        
                        if (enableDebugLogs)
                        {
                            SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: LightExposure ripristinato a valore base manuale (LED spento) - LightExposure: {pot.LightExposure} (base={pot.ManualLightExposureBase})");
                        }
                    }
                    else
                    {
                        // LED è acceso, LightExposure è già stato aumentato da ApplyLedSystemEffects
                        // Il valore base rimane preservato per quando LED sarà spento
                        if (enableDebugLogs)
                        {
                            SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: LightExposure preservato (MANUALE, LED acceso) - LightExposure: {pot.LightExposure} (base={pot.ManualLightExposureBase})");
                        }
                    }
                }
                else
                {
                    // Reset esposizione luce (ma NON i timestamp!)
                    pot.LightExposure = 0;
                }
                
                // BLK-03.01-T1: Decadimento fertilizzante giornaliero (-5% al giorno)
                // BUG FIX: Se FertilizerLevel è stato impostato manualmente, applica decay partendo dal valore base salvato
                if (pot.FertilizerLevel > 0 || (pot.IsFertilizerManuallySet && pot.ManualFertilizerBase >= 0))
                {
                    int oldFertilizerLevel = pot.FertilizerLevel;
                    
                    if (pot.IsFertilizerManuallySet && pot.ManualFertilizerBase >= 0)
                    {
                        // Decay partendo dal valore base manuale
                        float decayAmount = pot.ManualFertilizerBase * 0.05f; // 5% del valore base
                        int newFertilizerLevel = Mathf.Max(0, Mathf.RoundToInt(pot.ManualFertilizerBase - decayAmount));
                        pot.FertilizerLevel = newFertilizerLevel;
                        pot.ManualFertilizerBase = newFertilizerLevel; // Aggiorna base per il prossimo giorno
                        
                        if (enableDebugLogs && oldFertilizerLevel != pot.FertilizerLevel)
                        {
                            SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Decadimento fertilizzante (MANUALE, base={pot.ManualFertilizerBase}) - {oldFertilizerLevel}% → {pot.FertilizerLevel}%");
                        }
                    }
                    else
                    {
                        // Decay normale
                        FertilizerSystem.ApplyDailyDecay(pot, decayRate: 5f);
                        
                        if (enableDebugLogs && oldFertilizerLevel != pot.FertilizerLevel)
                        {
                            SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Decadimento fertilizzante - {oldFertilizerLevel}% → {pot.FertilizerLevel}%");
                        }
                    }
                    
                    // Incrementa contatore giorni con fertilizzante attivo (se ancora > 0)
                    if (pot.FertilizerLevel > 0)
                    {
                        pot.DaysFertilizerActive++;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Ottiene il numero di vasi registrati
    /// </summary>
    public int GetRegisteredPotCount()
    {
        return _registeredPots.Count;
    }

    /// <summary>
    /// Task 3: registra il drift pH passivo dei CryoSlot occupati nel PhSystem e applica i cap.
    /// I CryoSlot non entrano mai in _registeredPots né nel loop produttivo.
    /// </summary>
    private void ApplyPassivePowers(int dayIndex)
    {
        if (_phSystem == null) return;
        var cryo = ServiceContainer.Instance?.Get<CryoMachineController>(suppressWarning: true);
        if (cryo == null) return;

        var db = PlantDatabase.Instance;
        var slots = cryo.GetPassiveSlotsSnapshot();

        foreach (var slot in slots)
        {
            if (slot == null || !slot.IsOccupied || slot.Payload == null) continue;

            var payload = slot.Payload;
            float drift = 0f;
            float cap   = 0f;
            string label = payload.PassivePowerLabel ?? "—";

            if (db != null && !string.IsNullOrEmpty(payload.PlantCode))
            {
                var pd = db.GetPlantDataByCode(payload.PlantCode);
                if (pd != null)
                {
                    drift = pd.GetPassivePhDrift();
                    cap   = pd.PassivePhCap;
                    if (!string.IsNullOrEmpty(pd.PassivePower))
                        label = pd.PassivePower;
                }
            }

            _phSystem.RegisterCryoPassiveDrift(drift, slot.SlotId, label, cap, dayIndex);

            if (enableDebugLogs)
                SporiumLogger.LogDebug(LogCategory.Dome,
                    $"[Day {dayIndex}] PassivePower — {slot.SlotId}: {payload.PlantCode} Lv{payload.PlantLevel}" +
                    $" | drift={drift:+0.0;-0.0;0} cap={cap} | {label}");
        }

        _phSystem.ApplyCryoPassiveCaps();
    }

    /// <summary>
    /// Ottiene la configurazione di crescita corrente
    /// </summary>
    public PlantGrowthConfig GetGrowthConfig()
    {
        return growthConfig;
    }

    /// <summary>
    /// Imposta la configurazione di crescita
    /// </summary>
    public void SetGrowthConfig(PlantGrowthConfig config)
    {
        growthConfig = config;
        if (enableDebugLogs)
            SporiumLogger.LogInfo(LogCategory.Core, $"DayCycleController: Nuova configurazione impostata: {config?.name ?? "NULL"}");
    }

    #if UNITY_EDITOR
    [ContextMenu("Log Registered Pots")]
    private void EditorLogRegisteredPots()
    {
        SporiumLogger.LogDebug(LogCategory.Pot, $"DayCycleController: Vasi registrati ({_registeredPots.Count}):");
        for (int i = 0; i < _registeredPots.Count; i++)
        {
            var pot = _registeredPots[i];
            if (pot != null)
            {
                string plantInfo = pot.HasPlant 
                    ? $" - {GetStageName(pot.Stage)} (Giorno {pot.DaysSincePlant})" 
                    : " - Vuoto";
                SporiumLogger.LogDebug(LogCategory.Pot, $"  [{i}] {pot.PotId}{plantInfo}");
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.Pot, $"  [{i}] NULL (da rimuovere)");
            }
        }
    }

    [ContextMenu("Cleanup Null Pots")]
    private void EditorCleanupNullPots()
    {
        _registeredPots.RemoveAll(pot => pot == null);
        if (enableDebugLogs)
            SporiumLogger.LogDebug(LogCategory.Pot, $"DayCycleController: Cleanup completato, {_registeredPots.Count} vasi validi");
    }
    #endif
    
    /// <summary>
    /// BLK-01.04: Trova il PotGrowthController per un vaso specifico
    /// </summary>
    private PotGrowthController FindPotGrowthController(string potId)
    {
        TryGetPotRegistry();
        if (_potRegistry != null)
            return _potRegistry.FindGrowthController(potId);

        PotGrowthController[] controllers = FindObjectsOfType<PotGrowthController>();
        foreach (var controller in controllers)
        {
            var potState = controller.GetPotState();
            if (potState != null && potState.PotId == potId)
            {
                return controller;
            }
        }
        return null;
    }

    /// <summary>
    /// Restituisce le condizioni attive (muffa/infestazione) per il report EoD.
    /// Ogni elemento: (potId, moldRiskLevel, isInfested).
    /// </summary>
    public System.Collections.Generic.List<(string PotId, int MoldRiskLevel, bool IsInfested)> GetActiveConditionsForReport()
    {
        var list = new System.Collections.Generic.List<(string, int, bool)>();
        foreach (var pot in _registeredPots)
        {
            if (pot == null || !pot.HasPlant || pot.Stage == (int)PlantStage.Empty) continue;
            if (pot.MoldRiskLevel >= 1 || pot.IsInfested)
                list.Add((pot.PotId, pot.MoldRiskLevel, pot.IsInfested));
        }
        return list;
    }

    /// <summary>
    /// Restituisce il drift pH previsto per il prossimo giorno in base alle piante attualmente nei vasi registrati.
    /// Usato dal Forecast EoD per mostrare "Predicted pH Drift" senza registrare nulla.
    /// </summary>
    public float GetPredictedPhDriftForNextDay()
    {
        float total = 0f;
        foreach (var pot in _registeredPots)
        {
            if (pot == null || !pot.HasPlant || pot.Stage == (int)PlantStage.Empty || string.IsNullOrEmpty(pot.PlantCode))
                continue;
            var plantData = pot.GetPlantData();
            if (plantData == null) continue;
            float d = plantData.GetDailyPhDrift();
            if (BotanicalPlantCodes.IsArcticHask(plantData.PlantCode))
                d += 5f;
            total += d;
        }
        return total;
    }

    /// <summary>
    /// Calcola il drift pH totale da tutte le piante e lo registra nel PhSystem
    /// IMPORTANTE: Solo le piante nei POT hanno impatto sul pH, non quelle in Inventory o Seed Storage
    /// </summary>
    private void CalculateAndRegisterPhDrift(int currentDay = 0)
    {
        if (_phSystem == null)
        {
            if (enableDebugLogs)
                SporiumLogger.LogWarning(LogCategory.Ph, "PhSystem non disponibile, impossibile calcolare drift pH");
            return;
        }
        
        // IMPORTANTE: Prima rimuovi i contributi delle piante che non sono più nei vasi registrati
        // Questo gestisce il caso in cui una pianta è stata rimossa con UPROOT ma i contributi sono ancora presenti
        CleanupRemovedPlantContributions();
        
        float totalPhDrift = 0f;
        int plantCount = 0;
        int skippedCount = 0;
        
        if (enableDebugLogs)
            SporiumLogger.LogDebug(LogCategory.Ph, $"Calcolo drift pH per {_registeredPots.Count} vasi registrati...");
        
        foreach (var pot in _registeredPots)
        {
            if (pot == null)
            {
                skippedCount++;
                continue;
            }
            
            // IMPORTANTE: Verifica che il vaso abbia ancora una pianta (potrebbe essere stato rimosso con UPROOT)
            if (!pot.HasPlant)
            {
                if (enableDebugLogs)
                    SporiumLogger.LogDebug(LogCategory.Ph, $"{pot.PotId}: Vaso vuoto (HasPlant=false), saltato");
                skippedCount++;
                continue;
            }
            
            // IMPORTANTE: Verifica anche che lo stage non sia Empty (0) - doppio controllo
            if (pot.Stage == (int)PlantStage.Empty)
            {
                if (enableDebugLogs)
                    SporiumLogger.LogDebug(LogCategory.Ph, $"{pot.PotId}: Stage è Empty, saltato (pianta probabilmente rimossa con UPROOT)");
                skippedCount++;
                continue;
            }
            
            // DEBUG: Verifica PlantCode
            if (string.IsNullOrEmpty(pot.PlantCode))
            {
                if (enableDebugLogs)
                    SporiumLogger.LogWarning(LogCategory.Ph, $"{pot.PotId}: PlantCode è NULL o vuoto! Stage: {pot.Stage} (HasPlant: {pot.HasPlant})");
                skippedCount++;
                continue;
            }
            
            // Ottieni PlantData dalla pianta
            PlantData plantData = pot.GetPlantData();
            if (plantData == null)
            {
                if (enableDebugLogs)
                    SporiumLogger.LogWarning(LogCategory.Ph, $"{pot.PotId}: PlantData non trovato per PlantCode '{pot.PlantCode}'");
                skippedCount++;
                continue;
            }
            
            // Calcola drift pH per questa pianta (+ Arctic Purification +5/g per ogni Hask attivo nel vaso)
            float plantDrift = plantData.GetDailyPhDrift();
            if (BotanicalPlantCodes.IsArcticHask(plantData.PlantCode))
                plantDrift += 5f;
            totalPhDrift += plantDrift;
            plantCount++;
            
            // Registra ogni pianta individualmente per tooltip (anche con drift 0, per mostrare STANDARD in Active Modifiers)
            _phSystem.RegisterPlantDrift(plantDrift, plantData.PlantCode, pot.PotId, currentDay);
            
            if (enableDebugLogs)
            {
                SporiumLogger.LogDebug(LogCategory.Ph, $"{pot.PotId}: {plantData.PlantCode} ({plantData.Family}) Stage:{pot.Stage} → drift pH: {plantDrift:F2}/giorno");
            }
        }
        
        // Log riepilogativo
        if (totalPhDrift != 0f && enableDebugLogs)
        {
            SporiumLogger.LogInfo(LogCategory.Ph, $"pH Drift totale da {plantCount} piante: {totalPhDrift:F2} → pH attuale: {_phSystem.CurrentPh:F2}");
        }
        else if (enableDebugLogs)
        {
            if (plantCount > 0)
            {
                SporiumLogger.LogDebug(LogCategory.Ph, $"Nessun drift pH da {plantCount} piante (tutte Standard o drift = 0)");
            }
            else if (skippedCount > 0)
            {
                SporiumLogger.LogWarning(LogCategory.Ph, $"Nessuna pianta valida trovata! {skippedCount} vasi saltati (vuoti o senza PlantCode)");
            }
        }
    }
    
    /// <summary>
    /// Pulisce i contributi delle piante che non sono più nei vasi registrati
    /// IMPORTANTE: Solo le piante nei POT hanno impatto sul pH, non quelle in Inventory o Seed Storage
    /// </summary>
    private void CleanupRemovedPlantContributions()
    {
        if (_phSystem == null)
            return;
        
        // Ottieni la lista dei potId dei vasi registrati che hanno ancora piante
        System.Collections.Generic.HashSet<string> activePotIds = new System.Collections.Generic.HashSet<string>();
        foreach (var pot in _registeredPots)
        {
            if (pot != null && pot.HasPlant && pot.Stage != (int)PlantStage.Empty && !string.IsNullOrEmpty(pot.PlantCode))
            {
                activePotIds.Add(pot.PotId);
            }
        }
        
        // Rimuovi i contributi delle piante che non sono più nei vasi attivi
        _phSystem.CleanupPlantContributions(activePotIds);
        
        if (enableDebugLogs)
        {
            SporiumLogger.LogDebug(LogCategory.Pot, $"Cleanup: {activePotIds.Count} vasi attivi su {_registeredPots.Count} registrati");
        }
    }
    
    /// <summary>
    /// Restituisce il nome localizzato per uno stadio
    /// </summary>
    private string GetStageName(int stage)
    {
        switch (stage)
        {
            case 0: return "Empty";
            case 1: return "Seed";
            case 2: return "Sprout";
            case 3: return "Growth";
            case 4: return "Flowering";
            case 5: return "HarvestReady";
            case 6: return "Resting";
            default: return $"Stadio {stage}";
        }
    }
    
    /// <summary>
    /// Calcola lo score di condizione per tutte le piante (all'alba)
    /// </summary>
    private void CalculatePlantConditions(int dayIndex)
    {
        // BUGFIX (POT-CONDITION-TICK): la condizione deve calcolarsi ogni EndDay anche se PhSystem non è ancora disponibile.
        // Il PlantConditionSystem gestisce phSystem==null (semplicemente non applica bonus/malus pH).
        if (_potSystemConfig == null && enableDebugLogs)
        {
            SporiumLogger.LogWarning(LogCategory.Pot, "PotSystemConfig non disponibile per calcolo condizione. Uso valori di default (MaxHydration=10, MaxDaysForFullStress=5).");
        }

        BotanicalRosterSnapshot botanicalSnapshot = BotanicalRosterSnapshot.FromServices(_phSystem);
        
        foreach (var pot in _registeredPots)
        {
            if (pot == null || !pot.HasPlant)
                continue;
            
            // Morta è persistente: non ricalcolare né sovrascrivere. Rimane finché non Uproot.
            if (IsDead(pot))
                continue;

            // Snapshot per capire se dobbiamo notificare le UI a fine tick.
            int preConditionLabel = pot.ConditionLabel;
            int preConditionScore = pot.ConditionScore;
            int preMoldRisk = pot.MoldRiskLevel;
            bool preInfested = pot.IsInfested;
            bool preSterile = pot.IsSterile;
            
            // Salva score precedente per calcolo forecast
            int previousScore = pot.PreviousDayConditionScore >= 0 ? pot.PreviousDayConditionScore : pot.ConditionScore;
            bool isFirstCalculation = pot.PreviousDayConditionScore < 0;
            
            // Ottieni PlantData
            PlantData plantData = pot.GetPlantData();
            if (plantData == null)
            {
                // Non bloccare il tick: calcoliamo comunque la condizione in fallback (senza PlantData).
                // Nota: in PlantConditionSystem, plantData==null restituisce uno score neutro.
                if (enableDebugLogs)
                    SporiumLogger.LogWarning(LogCategory.Pot, $"{pot.PotId}: PlantData non trovato per calcolo condizione (fallback).");
            }
            
            // DEBUG: Log dati INPUT prima del calcolo (Ipotesi A: parametri diversi da UI)
            int maxHydration = _potSystemConfig != null ? _potSystemConfig.MaxHydration : 10;
            int hydrationPercent = maxHydration > 0 ? Mathf.RoundToInt((float)pot.Hydration / maxHydration * 100f) : 0;
            float currentPh = _phSystem != null ? _phSystem.CurrentPh : 0f;
            bool isOverwatering = PlantConditionSystem.IsOverwatering(pot, maxHydration);
            int consecutiveLedDays = pot.GetConsecutiveLedDays();
            float stressPercentage = _potSystemConfig != null ? Mathf.Clamp01((float)consecutiveLedDays / _potSystemConfig.MaxDaysForFullStress) * 100f : 0f;
            
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_INPUT] {pot.PotId} Day={dayIndex}: Score={pot.ConditionScore}, PrevScore={pot.PreviousDayConditionScore}, Cond={pot.ConditionLabel}, Hydration={pot.Hydration}/{maxHydration} ({hydrationPercent}%), pH={currentPh:F1}, Overwatering={isOverwatering}, WateringON={pot.WateringSystemOn}, LED={pot.LedSystemState}, ConsecutiveDays={consecutiveLedDays}, Stress%={stressPercentage:F1}, Stage={pot.Stage}, Fertilizer={pot.FertilizerLevel}, MoldRisk={pot.MoldRiskLevel}, Infested={pot.IsInfested}");
            
            // Log critico: Input completo per calcolo condizione
            SporiumLogger.LogDebugWithLocation(
                LogCategory.Pot,
                "DayCycleController:CalculatePlantConditions:INPUT",
                $"INPUT Calcolo Condizione - PotId={pot.PotId}, Day={dayIndex}",
                new {
                    potId = pot.PotId,
                    day = dayIndex,
                    previousConditionScore = pot.ConditionScore,
                    previousDayConditionScore = pot.PreviousDayConditionScore,
                    previousConditionLabel = pot.ConditionLabel,
                    hydration = pot.Hydration,
                    maxHydration = maxHydration,
                    hydrationPercent = hydrationPercent,
                    currentPh = currentPh,
                    isOverwatering = isOverwatering,
                    wateringSystemOn = pot.WateringSystemOn,
                    ledSystemState = pot.LedSystemState.ToString(),
                    consecutiveLedDays = consecutiveLedDays,
                    stressPercentage = stressPercentage,
                    stage = pot.Stage,
                    fertilizerLevel = pot.FertilizerLevel,
                    moldRiskLevel = pot.MoldRiskLevel,
                    isInfested = pot.IsInfested,
                    plantCode = pot.PlantCode,
                    daysInCurrentStage = pot.DaysInCurrentStage
                },
                "A",
                "debug"
            );
            
            // DEBUG: Log dati prima del calcolo
            if (enableDebugLogs)
            {
                SporiumLogger.LogDebug(LogCategory.Pot, $"DEBUG Calcolo Condizione {pot.PotId}: Hydration={pot.Hydration}/{maxHydration} ({hydrationPercent}%), Overwatering={isOverwatering}, pH={currentPh:F1}, WateringON={pot.WateringSystemOn}, LED={pot.LedSystemState}, Stage={pot.Stage}");
            }
            
            // Calcola condizione
            ConditionResult result = PlantConditionSystem.CalculateCondition(
                pot, 
                plantData, 
                _phSystem, 
                _potSystemConfig, 
                dayIndex, 
                previousScore);
            
            // DEBUG: Log OUTPUT dopo calcolo (Ipotesi C: PreviousDayConditionScore non salvato correttamente)
            int oldCondition = pot.ConditionLabel;
            int oldScore = pot.ConditionScore;
            
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_OUTPUT] {pot.PotId} Day={dayIndex}: OldCond={oldCondition} (Score={oldScore}) → NewCond={(int)result.Condition} (Score={result.Score}), Delta={result.ScoreDelta}, Forecast={result.Forecast}, Contributors={result.Contributors?.Length ?? 0}");
            
            if (oldCondition != (int)result.Condition)
            {
                SporiumLogger.LogWarning(LogCategory.Pot, $"[DEBUG_CONDITION_CHANGE] {pot.PotId} Day={dayIndex}: CONDIZIONE CAMBIATA da {oldCondition} (Score={oldScore}) a {(int)result.Condition} (Score={result.Score}), Delta={result.ScoreDelta}");
            }
            
            // Log critico: Output completo calcolo condizione
            SporiumLogger.LogDebugWithLocation(
                LogCategory.Pot,
                "DayCycleController:CalculatePlantConditions:OUTPUT",
                $"OUTPUT Calcolo Condizione - PotId={pot.PotId}, Day={dayIndex}",
                new {
                    potId = pot.PotId,
                    day = dayIndex,
                    oldConditionLabel = oldCondition,
                    oldConditionScore = oldScore,
                    newConditionLabel = (int)result.Condition,
                    newConditionScore = result.Score,
                    scoreDelta = result.ScoreDelta,
                    forecast = result.Forecast.ToString(),
                    previousDayScoreUsed = previousScore,
                    isFirstCalculation = isFirstCalculation,
                    contributorsCount = result.Contributors != null ? result.Contributors.Length : 0
                },
                "C",
                "debug"
            );
            
            // Salva score precedente prima di aggiornare
            pot.PreviousDayConditionScore = pot.ConditionScore;
            
            // Aggiorna score e condizione
            pot.ConditionScore = result.Score;
            pot.ConditionLabel = (int)result.Condition;
            pot.ForecastDirection = (int)result.Forecast;
            
            // Trigger Morta: condizione resta in Critica per >2 giorni (3 consecutivi).
            // BUG FIX: Verifica la condizione effettiva (result.Condition) invece di solo lo score,
            // per garantire che DaysCritical venga incrementato solo quando la condizione è realmente Critica.
            bool isConditionCritical = result.Condition == PlantCondition.Critica;
            if (isConditionCritical)
                pot.DaysCritical++;
            else
                pot.DaysCritical = 0;
            
            if (pot.DaysCritical >= 3)
            {
                pot.ConditionLabel = (int)PlantCondition.Morta;
                
                // Spegni sistemi persistenti per evitare consumi/side-effect post-morte.
                pot.WateringSystemOn = false;
                pot.LedSystemState = LedSystemState.Off;
                pot.IsSterile = false;
                
                // Notifica morte via Foundation
                int criticalThreshold = Sporae.DevTools.DifficultyCalibrationConfig.ConditionThresholdAppassita;
                string deathReason = $"Condizione Critica per {pot.DaysCritical} giorni (score<{criticalThreshold})";
                PotEvents.EmitPlantDied(pot.PotId, deathReason);
                
                var foundationDeath = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                if (foundationDeath != null && foundationDeath.Enabled)
                {
                    foundationDeath.PostToast("PLANT-DEATH-001",
                        new NotificationPayload()
                            .With("reason", $"{pot.PlantCode ?? "Pianta"} — Condizione Critica ({pot.DaysCritical} giorni)"));
                }
                
                // Aggiorna visual/UI
                PotSlot potSlot = FindPotSlot(pot.PotId);
                if (potSlot != null)
                {
                    var potGrowthController = potSlot.GetComponent<PotGrowthController>();
                    if (potGrowthController != null)
                        potGrowthController.UpdateVisuals();
                    
                    PotEvents.EmitChanged(potSlot);
                }
                
                // Non calcolare muffe/altro dopo la morte.
                continue;
            }
            
            
            // Verifica cambio condizione per notifica Toast Foundation
            // Mostra il toast solo se il delta score è almeno ±20 (20%) per evitare spam su variazioni minime
            if (!isFirstCalculation && oldCondition != pot.ConditionLabel && Mathf.Abs(result.ScoreDelta) >= 20)
            {
                string conditionName = PlantConditionSystem.GetConditionName(result.Condition, 
                    PlantConditionSystem.IsOverwatering(pot, _potSystemConfig.MaxHydration));
                string forecastSymbol = PlantConditionSystem.GetForecastSymbol(result.Forecast);
                var foundationCnd = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                
                if (pot.ConditionLabel < oldCondition && result.ScoreDelta > 0) // Miglioramento
                {
                    string message = $"{conditionName} ({result.Score}/100) {forecastSymbol}";
                    if (foundationCnd != null && foundationCnd.Enabled)
                        foundationCnd.PostToast("CND-002", new NotificationPayload().With("details", message));
                }
                else if (pot.ConditionLabel > oldCondition && result.ScoreDelta < 0) // Peggioramento
                {
                    string message = $"{conditionName} ({result.Score}/100) {forecastSymbol}";
                    if (foundationCnd != null && foundationCnd.Enabled)
                        foundationCnd.PostToast("CND-001", new NotificationPayload().With("details", message));
                }
                
                if (enableDebugLogs)
                    SporiumLogger.LogInfo(LogCategory.Pot, $"{pot.PotId}: Condizione cambiata da {oldCondition} a {pot.ConditionLabel} ({conditionName}) - Score: {result.Score}/100, Forecast: {forecastSymbol}, Δ: {result.ScoreDelta}");
            }
            
            if (enableDebugLogs)
            {
                SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Condizione calcolata - Score: {result.Score}/100, Condizione: {result.Condition}, Forecast: {result.Forecast}, Δ: {result.ScoreDelta}");
            }
            
            // BLK-07.01: Calcolo mold risk giornaliero
            MoldConfig moldConfig = Resources.Load<MoldConfig>("Configs/MoldConfig");
            if (moldConfig != null && plantData != null)
            {
                // Traccia overwatering consecutivo
                bool isOverwateringForMold = PlantConditionSystem.IsOverwatering(pot, _potSystemConfig.MaxHydration);
                if (isOverwateringForMold)
                {
                    pot.DaysOverwateringConsecutive++;
                }
                else
                {
                    pot.DaysOverwateringConsecutive = 0;
                }
                
                // FASE 5: Ottieni percentuale condensazione (usata per giorni virtuali e infestazione)
                float condensationPercentage = 0f;
                if (_gameManager != null && _gameManager.CondensationSystem != null)
                {
                    condensationPercentage = _gameManager.CondensationSystem.CurrentAccumulation;
                }
                
                // FASE 5: Aggiungi giorni virtuali da condensazione (se >50%)
                // Nota: DaysOverwateringConsecutive è int, quindi arrotondiamo i giorni virtuali
                float virtualDays = GetVirtualDaysFromCondensation(condensationPercentage);
                if (virtualDays > 0f)
                {
                    // Converti giorni virtuali a int (arrotondamento per compatibilità)
                    // Per mantenere precisione frazionaria, arrotondiamo: 0.5 → 1, 1.0 → 1, 1.5 → 2
                    int virtualDaysInt = Mathf.RoundToInt(virtualDays);
                    pot.DaysOverwateringConsecutive += virtualDaysInt;
                    
                    if (enableDebugLogs)
                    {
                        SporiumLogger.LogDebug(LogCategory.Pot, 
                            $"{pot.PotId}: Aggiunti {virtualDays:F1} giorni virtuali da condensazione ({condensationPercentage:F1}%) → +{virtualDaysInt} giorni");
                    }
                }
                
                // Incrementa giorni senza potatura
                pot.DaysWithoutPruning++;
                
                // Calcola mold risk (Task 4: eccesso giorni modificato da Ferric Fern attivo / Glasscap cryo)
                int oldMoldRiskLevel = pot.MoldRiskLevel;
                int rawExcess = Mathf.Max(0, pot.DaysOverwateringConsecutive - moldConfig.overwateringDaysThreshold);
                int adjustedExcess = BotanicalMoldModifiers.ApplyToRawExcess(rawExcess, botanicalSnapshot);
                int computedMoldRisk = MoldSystem.GetMoldRiskLevelFromAdjustedExcess(adjustedExcess, moldConfig);

                // Se il livello muffa e' stato impostato via debug (livello > 0 ma nessun overwatering storico/reale),
                // evita reset immediato a 0 al primo EndOfDay e lascia che i sistemi di riduzione lo consumino gradualmente.
                bool looksLikeManualMoldOverride =
                    oldMoldRiskLevel > 0 &&
                    pot.DaysOverwateringConsecutive == 0 &&
                    rawExcess == 0 &&
                    !isOverwateringForMold &&
                    computedMoldRisk == 0;

                pot.MoldRiskLevel = looksLikeManualMoldOverride ? oldMoldRiskLevel : computedMoldRisk;

                // Arctic Hask attivo: −1 livello muffa su ogni vaso ogni 2 giorni di calendario
                bool arcticReducedMold = false;
                if (botanicalSnapshot.ActiveArcticHaskCount > 0 && dayIndex > 0 && dayIndex % 2 == 0)
                {
                    int beforeArcticReduce = pot.MoldRiskLevel;
                    MoldSystem.ReduceMoldRiskLevel(pot);
                    arcticReducedMold = pot.MoldRiskLevel < beforeArcticReduce;
                }

                // Toast: mold level gained (one per level)
                if (pot.MoldRiskLevel > oldMoldRiskLevel)
                {
                    var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                    for (int lvl = oldMoldRiskLevel + 1; lvl <= pot.MoldRiskLevel; lvl++)
                    {
                        if (foundation != null && foundation.Enabled)
                        {
                            foundation.PostToast("MOLD-GAIN",
                                new NotificationPayload()
                                    .With("potId", pot.PotId)
                                    .With("level", lvl.ToString()));
                        }
                    }
                }
                // Toast: mold level reduced (with cause)
                else if (pot.MoldRiskLevel < oldMoldRiskLevel)
                {
                    // Determine the most relevant cause for the toast tooltip:
                    // Arctic periodic pulse > Ferric Fern adjustment > overwatering resolved
                    string cause;
                    if (arcticReducedMold)
                        cause = "Arctic Hask Effect";
                    else if (botanicalSnapshot.AnyFerricFernActive && rawExcess < (oldMoldRiskLevel))
                        cause = "Ferric Fern Effect";
                    else
                        cause = "Overwatering rientrato";

                    var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                    if (foundation != null && foundation.Enabled)
                    {
                        foundation.PostToast("MOLD-REDUCE",
                            new NotificationPayload()
                                .With("potId", pot.PotId)
                                .With("cause", cause));
                    }
                }

                // BUG FIX 2: Tracking giorni a livello 3 per infestazione
                if (pot.MoldRiskLevel == 3)
                {
                    pot.DaysAtMoldRiskLevel3++;
                }
                else
                {
                    pot.DaysAtMoldRiskLevel3 = 0; // Reset se non è più a livello 3
                }
                
                // FASE 5: Modifica logica infestazione se condensazione = 100%
                
                // Calcola giorni richiesti per infestazione (ridotti se condensazione = 100%)
                int requiredDaysAtLevel3 = 2; // Default: 2 giorni
                bool immediateInfestation = false;
                
                if (condensationPercentage >= 100f)
                {
                    // Se condensazione = 100%: riduce giorni richiesti (2 → 1 → 0)
                    // Se già a livello 3 da almeno 1 giorno: infestazione immediata
                    if (pot.MoldRiskLevel == 3 && pot.DaysAtMoldRiskLevel3 >= 1)
                    {
                        immediateInfestation = true;
                        if (enableDebugLogs)
                        {
                            SporiumLogger.LogWarning(LogCategory.Pot, 
                                $"{pot.PotId}: Infestazione immediata per condensazione 100% (DaysAtLevel3={pot.DaysAtMoldRiskLevel3})");
                        }
                    }
                    else
                    {
                        // Riduce giorni richiesti da 2 a 1
                        requiredDaysAtLevel3 = 1;
                        if (enableDebugLogs)
                        {
                            SporiumLogger.LogDebug(LogCategory.Pot, 
                                $"{pot.PotId}: Condensazione 100% - giorni richiesti ridotti da 2 a {requiredDaysAtLevel3}");
                        }
                    }
                }
                
                // BUG FIX 2: Infestazione solo dopo giorni consecutivi a livello 3 (modificato per condensazione 100%)
                bool shouldInfest = immediateInfestation || 
                    (pot.MoldRiskLevel == 3 && pot.DaysAtMoldRiskLevel3 >= requiredDaysAtLevel3);
                if (shouldInfest && !pot.IsInfested)
                {
                    // Prima infestazione: applica effetti e mostra toast Foundation
                    pot.IsInfested = true;
                    PlantLevelConfig levelConfig = Resources.Load<PlantLevelConfig>("Configs/PlantLevelConfig");
                    int levelBefore = pot.PlantLevel;
                    MoldSystem.ApplyInfestation(pot, pot.MoldRiskLevel, moldConfig, levelConfig);
                    int levelLost = levelBefore - pot.PlantLevel;
                    
                    var foundationInfest = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                    if (foundationInfest != null && foundationInfest.Enabled)
                    {
                        foundationInfest.PostToast("MLD-INFESTED",
                            new NotificationPayload()
                                .With("potId", pot.PotId)
                                .With("levelLost", levelLost.ToString()));
                        
                        if (levelLost > 0)
                        {
                            foundationInfest.PostToast("PLT-LVL-DOWN",
                                new NotificationPayload()
                                    .With("potId", pot.PotId)
                                    .With("plantCode", pot.PlantCode ?? "?")
                                    .With("oldLevel", levelBefore.ToString())
                                    .With("newLevel", pot.PlantLevel.ToString())
                                    .With("reason", "Infestazione muffe"));
                        }
                    }
                    SporiumLogger.LogWarning(LogCategory.Pot, $"{pot.PotId}: Infestazione applicata dopo {pot.DaysAtMoldRiskLevel3} giorni a livello 3 (livello perso: {levelLost})");
                }
                else if (!shouldInfest && pot.IsInfested)
                {
                    // Infestazione rimossa (livello sceso sotto 3)
                    pot.IsInfested = false;
                    var foundationInfestClear = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                    if (foundationInfestClear != null && foundationInfestClear.Enabled)
                    {
                        foundationInfestClear.PostToast("MLD-INFESTED-CLEARED",
                            new NotificationPayload().With("potId", pot.PotId));
                    }
                    SporiumLogger.LogInfo(LogCategory.Pot, $"{pot.PotId}: Infestazione rimossa (livello sceso a {pot.MoldRiskLevel})");
                }
                
                if (enableDebugLogs && pot.MoldRiskLevel > 0)
                {
                    // NOTA: Mold Risk ora calcolato SOLO da overwatering prolungato (1 livello per ogni giorno oltre soglia)
                    SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Mold Risk Level: {pot.MoldRiskLevel} (DaysOverwatering: {pot.DaysOverwateringConsecutive}, Threshold: {moldConfig.overwateringDaysThreshold})");
                }
            }

            // Calcolo sterilità: Pure in pH Ultra Basico → sterile
            if (plantData != null && _phSystem != null)
            {
                PhSystem.PhBand phBandNow = _phSystem.EvaluateState();
                bool shouldBeSterile = PhGrowthModifier.IsSterile(phBandNow, plantData.Family);
                if (shouldBeSterile != pot.IsSterile)
                {
                    pot.IsSterile = shouldBeSterile;
                    var foundationSterile = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                    if (foundationSterile != null && foundationSterile.Enabled)
                    {
                        string sterileCode = shouldBeSterile ? "STERILE-001" : "STERILE-CLEARED";
                        foundationSterile.PostToast(sterileCode,
                            new NotificationPayload()
                                .With("potId", pot.PotId)
                                .With("plantCode", pot.PlantCode ?? plantData.PlantCode ?? "?"));
                    }
                }
            }

            // BUGFIX (POT-CONDITION-UI): se parametri/condizione cambiano a fine giornata, notifica le UI
            // anche quando il player non ha compiuto azioni (Water/LED/Spray...).
            bool anyChanged =
                preConditionLabel != pot.ConditionLabel ||
                preConditionScore != pot.ConditionScore ||
                preMoldRisk != pot.MoldRiskLevel ||
                preInfested != pot.IsInfested ||
                preSterile != pot.IsSterile;

            if (anyChanged)
            {
                PotSlot potSlot = FindPotSlot(pot.PotId);
                if (potSlot != null)
                {
                    PotEvents.EmitChanged(potSlot);
                }
            }
        }
    }
    
    /// <summary>
    /// Uccide la pianta a causa di pH estremo opposto alla famiglia
    /// </summary>
    private void KillPlantFromExtremePh(PotStateModel pot, PlantData plantData, PhSystem.PhBand phBand)
    {
        SporiumLogger.LogError(LogCategory.Pot, $"Pianta morta per pH estremo! Vaso: {pot.PotId}, Famiglia: {plantData.Family}, pH Band: {phBand}");
        
        // Morta persistente: non svuotare il pot. Rimane Morta finché non Uproot.
        pot.ConditionLabel = (int)PlantCondition.Morta;
        pot.DaysCritical = 3; // coerente con trigger critico
        pot.DaysInExtremePh = 0;
        pot.ExtremePhDeathCountdown = -1;
        
        // Spegni sistemi persistenti per evitare consumi/side-effect post-morte.
        pot.WateringSystemOn = false;
        pot.LedSystemState = LedSystemState.Off;
        pot.IsSterile = false;
        
        // Notifica evento morte pianta
        string reason = $"pH estremo opposto ({phBand}) per famiglia {plantData.Family}";
        PotEvents.EmitPlantDied(pot.PotId, reason);
        
        // Toast Foundation — unico canale per la morte da pH
        var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
        if (foundation != null && foundation.Enabled)
        {
            foundation.PostToast("PH-DEATH-001",
                new NotificationPayload().With("plant", plantData.PlantCode ?? "Plant"));
        }
        
        // Cerca PotSlot per aggiornare visuali
        PotSlot potSlot = FindPotSlot(pot.PotId);
        if (potSlot != null)
        {
            var potGrowthController = potSlot.GetComponent<PotGrowthController>();
            if (potGrowthController != null)
            {
                potGrowthController.UpdateVisuals();
            }
            
            // Notifica cambio stato
            PotEvents.EmitChanged(potSlot);
        }
    }
    
    /// <summary>
    /// Mostra notifica Toast countdown per morte imminente
    /// </summary>
    private void ShowExtremePhCountdownNotification(PotStateModel pot, PlantData plantData, int countdown)
    {
        // Mostra notifica solo quando countdown cambia (evita spam)
        if (countdown > 0)
        {
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
            {
                // usa stessa key del watcher per evitare duplicati
                foundation.UpsertDanger(
                    $"PH:RISK:{pot.PotId}",
                    "PH-RISK-COUNTDOWN",
                    new NotificationPayload()
                        .With("potId", pot.PotId)
                        .With("plant", plantData.PlantCode ?? "Plant")
                        .With("days", countdown.ToString()));
            }
        }
    }
    
    /// <summary>
    /// FASE 3: Applica sistema condensazione basato su piante attive e LED.
    /// Calcola produzione giornaliera e aggiorna accumulo.
    /// </summary>
    private void ApplyCondensationSystem(int dayIndex)
    {
        var result = _condensationDayProcessor.Apply(_gameManager, _registeredPots);
        if (!result.Applied)
        {
            if (enableDebugLogs)
                SporiumLogger.LogWarning(LogCategory.Core, "GameManager o CondensationSystem non disponibili per calcolo condensazione");
            return;
        }

        if (enableDebugLogs)
        {
            SporiumLogger.LogDebug(LogCategory.Core, 
                $"Condensazione Day {dayIndex}: Produzione={result.Production:F1}%, Accumulo={result.Accumulation:F1}%, LED attivo={result.HasActiveLed}");
        }
    }
    
    /// <summary>
    /// FASE 5: Calcola giorni virtuali da aggiungere a DaysOverwateringConsecutive basato su percentuale condensazione.
    /// - 0-49%: 0 giorni
    /// - 50-59%: 0.5 giorni
    /// - 60-79%: 1.0 giorni
    /// - 80-100%: 1.5 giorni
    /// </summary>
    private float GetVirtualDaysFromCondensation(float percentage)
    {
        if (percentage < 50f)
            return 0f;
        if (percentage < 60f)
            return 0.5f;
        if (percentage < 80f)
            return 1.0f;
        return 1.5f; // 80-100%
    }
    
}

