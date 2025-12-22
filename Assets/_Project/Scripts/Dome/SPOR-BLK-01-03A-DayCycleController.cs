using System.Collections.Generic;
using _Project.Sporae.Core;
using UnityEngine;
using Sporae.Core;
using Sporae.Dome.PotSystem.Growth;
using Sporae.Dome.PotSystem.Condition;
using Sporae.Dome.PotSystem.Fertilizer;
using Sporae.Dome.PotSystem.Mold;
using Sporae.Dome.PotSystem.Level;
using UnityEngine.SceneManagement;
using _Project;
using Sporae.DevTools;

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

    private void Awake()
    {
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
        // DEBUG_SAFE_FIX: Usa TryGetUINotification che prova prima ServiceContainer poi FindObjectOfType
        // Inoltre cerca anche oggetti disattivati perché UINotification potrebbe non essere ancora attivo
        if (_uiNotification == null)
        {
            TryGetUINotification();
            
            // Se ancora non trovato, prova a cercare anche oggetti disattivati
            if (_uiNotification == null)
            {
                _uiNotification = Object.FindObjectOfType<UINotification>(true); // true = include inactive
            }
            
            // DEBUG_SAFE_FIX: Non mostrare warning se ServiceContainer non è ancora disponibile
            // Il sistema si collegherà automaticamente tramite OnServiceRegistered quando UINotification viene registrato
            if (enableDebugLogs && _uiNotification == null && ServiceContainer.Instance != null)
            {
                // Mostra warning solo se ServiceContainer è disponibile ma UINotification non è ancora registrato
                // Questo evita warning falsi quando ServiceContainer non è ancora inizializzato
                SporiumLogger.LogWarning(LogCategory.Dome, "UINotification non trovato in scena; i toast non verranno mostrati. Verrà collegato automaticamente quando registrato nel ServiceContainer.");
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
            var potSystemConfig = FindObjectOfType<PotSystemConfig>();
            if (potSystemConfig != null && potSystemConfig.GrowthConfig != null)
            {
                growthConfig = potSystemConfig.GrowthConfig;
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
        // ScriptableObject non può essere trovato con FindObjectOfType, cerca solo in Resources
        _potSystemConfig = Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");
        
        // Se non trovato con il nome esatto, cerca tutti i PotSystemConfig in Resources
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
        _dayCycleSystem = ServiceContainer.Instance?.Get<DayCycleSystem>();
        if (_dayCycleSystem != null)
            _dayCycleSystem.OnDayChanged += HandleDayChanged;
        
        // Cerca PhSystem per integrazione pH (con retry se non disponibile subito)
        TryGetPhSystem();
        
        // Cerca GameManager per consumo risorse watering system
        TryGetGameManager();
        
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
    
    /// <summary>
    /// Tenta di ottenere UINotification dal ServiceContainer o FindObjectOfType
    /// </summary>
    private void TryGetUINotification()
    {
        // Prova prima dal ServiceContainer
        if (ServiceContainer.Instance != null)
        {
            try
            {
                _uiNotification = ServiceContainer.Instance.Get<UINotification>(suppressWarning: true);
                if (_uiNotification != null && enableDebugLogs)
                {
                    SporiumLogger.LogInfo(LogCategory.Core, "UINotification trovato dal ServiceContainer!");
                    return;
                }
            }
            catch
            {
                // UINotification non nel ServiceContainer, prova FindObjectOfType
            }
        }
        
        // Fallback: cerca nella scena
        _uiNotification = Object.FindObjectOfType<UINotification>();
        if (_uiNotification != null && enableDebugLogs)
        {
            SporiumLogger.LogInfo(LogCategory.Core, "UINotification trovato nella scena!");
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
        }
        
        if (service is UINotification uiNotification && _uiNotification == null)
        {
            _uiNotification = uiNotification;
            if (enableDebugLogs)
            {
                SporiumLogger.LogInfo(LogCategory.UI, "UINotification registrato! Collegato per warning watering system.");
            }
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
            if (pot is { HasPlant: true })
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
        int maxHydration = _potSystemConfig != null ? _potSystemConfig.MaxHydration : 4; // DEBUG_SAFE_FIX: Fallback aggiornato da 3 a 4
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
        
        // BLK-03.01-T2: Aggiorna tracking giorni consecutivi ottimali
        // DEBUG_SAFE_FIX: Per Seed, consideriamo ottimali anche i giorni con solo water + light (2 punti)
        PlantStage currentStageForOptimal = (PlantStage)pot.Stage;
        int requiredOptimalPoints = (currentStageForOptimal == PlantStage.Seed) ? 2 : 3;  // Seed: 2 punti, altri: 3 punti
        
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
        
        if (enableDebugLogs)
        {
            SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Punti giornalieri - Water: {pointsResult.WaterPoint}, Light: {pointsResult.LightPoint}, Fertilizer: {pointsResult.FertilizerPoint}, Total: {pointsResult.TotalPoints}, DaysOptimal: {pot.DaysConsecutiveOptimal}");
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
                    float fruitsToAdd = (Random.Range(0f, 1f) < 0.3f) ? 2f : 1f;
                    pot.AmountFruits = Mathf.Min(pot.AmountFruits + fruitsToAdd, 3f);
                }
                else
                {
                    // Possibilità mancata produzione (20%) se pH fuori range
                    if (Random.Range(0f, 1f) >= 0.2f)
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
        if (_phSystem != null && plantData != null)
        {
            float currentPh = _phSystem.CurrentPh;
            PhSystem.PhBand phBand = _phSystem.EvaluateState();
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
        PlantCondition currentCondition = (PlantCondition)pot.ConditionLabel;
        if (ConditionGrowthModifier.BlocksAdvancement(currentCondition))
        {
            if (enableDebugLogs)
                SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Avanzamento bloccato - Condizione: {currentCondition}");
            // Non può avanzare, ma continua con il resto della logica (produzione frutti, etc.)
        }
        
        // BLK-07.01: Verifica blocco crescita per infestazione Severe
        if (pot.MoldRiskLevel >= 2) // Severe o Critical
        {
            if (enableDebugLogs)
                SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Avanzamento bloccato - Infestazione Severe (Mold Risk Level: {pot.MoldRiskLevel})");
            // Non può avanzare, ma continua con il resto della logica
        }
        
        // Verifica se i requisiti sono soddisfatti
        bool requirementsMet = false;
        if (currentStageReq != null)
        {
            // Verifica idratazione nel range
            bool hydrationOk = currentStageReq.IsHydrationInRange(hydrationPercent);
            
            // Verifica LED richiesto (BLK-02.07: usa LedSystemState invece di LastLedType)
            bool ledOk = currentStageReq.IsLedRequirementMet(pot.LedSystemState);
            
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
            // BUG FIX: Per Seed e Sprout (stadi pre-Growth), rendiamo il fertilizzante opzionale (non bloccante se è 0%)
            bool fertilizerOk = false;
            if (currentStage == PlantStage.Seed || currentStage == PlantStage.Sprout)
            {
                // Per Seed e Sprout: fertilizzante opzionale - OK se è nel range OPPURE se è 0% (non ancora applicato)
                fertilizerOk = currentStageReq.IsFertilizerInRange(pot.FertilizerLevel) || pot.FertilizerLevel == 0;
            }
            else
            {
                // Per Growth e stadi successivi: fertilizzante obbligatorio nel range
                fertilizerOk = currentStageReq.IsFertilizerInRange(pot.FertilizerLevel);
            }
            
            // BLK-03.01-T2: Verifica punti accumulati
            // BUG FIX: Per Seed e Sprout, richiediamo solo 2 punti (water + light), fertilizzante opzionale
            int totalPoints = pot.GrowthPointsWater + pot.GrowthPointsLight + pot.GrowthPointsFertilizer;
            int requiredPoints = (currentStage == PlantStage.Seed || currentStage == PlantStage.Sprout) ? 2 : 3;  // Seed/Sprout: 2 punti (water+light), altri: 3 punti
            bool pointsOk = totalPoints >= requiredPoints;
            
            // BLK-03.01-T2: Avanzamento richiede tutti i requisiti E non deve essere bloccato dalla condizione
            // BLK-07.01: Blocca anche se infestazione Severe
            bool isBlockedByCondition = ConditionGrowthModifier.BlocksAdvancement(currentCondition);
            bool isBlockedByMold = pot.MoldRiskLevel >= 2; // Severe o Critical
            requirementsMet = !isBlockedByCondition && !isBlockedByMold &&
                             hydrationOk && ledOk && durationOk && optimalDaysOk && fertilizerOk && pointsOk;
            
            if (enableDebugLogs)
            {
                int optimalDaysRequired = (currentStage == PlantStage.Seed) ? 1 : currentStageReq.durationDays;
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
            if (enableDebugLogs)
                SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Nessun requisito specifico per stage {currentStage}, avanzamento automatico");
        }
        
        // BLK-02.02: Avanzamento stadi con requisiti specifici
        if (requirementsMet)
        {
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
            
            // Toast cambio stadio
            if (_toastManager != null)
            {
                _toastManager.ShowToast(ToastNotificationType.StageUp, 
                    $"Stage up: {pot.PotId} → {(PlantStage)pot.Stage}", 
                    "STAGE-UP-001");
            }
            else if (_uiNotification != null)
            {
                _uiNotification.ShowNotification(
                    $"Stage up: {pot.PotId} → {(PlantStage)pot.Stage}",
                    3f,
                    Color.cyan);
            }
            else if (enableDebugLogs)
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"UINotification mancante: niente toast stage up per {pot.PotId} → {(PlantStage)pot.Stage}");
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
            
            // Emetti evento per UI (mostra toast warning)
            if (_toastManager != null)
            {
                _toastManager.ShowWarning(message, "LGT-002");
            }
            else if (_uiNotification != null)
            {
                _uiNotification.ShowNotification(message, 3f, Color.yellow);
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
        
        int maxHydration = _potSystemConfig != null ? _potSystemConfig.MaxHydration : 4;
        
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
                if (!_gameManager.PlayerInventory.Has(Items.Water))
                {
                    // FALLBACK: Disattiva sistema automaticamente - WAT-RAW insufficiente
                    pot.WateringSystemOn = false;
                    pot.WateringRawWaterAccumulator = 0f;
                    pot.DaysWateringSystemOn = 0;
                    
                    string message = $"💧 Sistema irrigazione {pot.PotId} disattivato: WAT-RAW insufficiente";
                    if (enableDebugLogs)
                        SporiumLogger.LogWarning(LogCategory.Pot, message);
                    
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
                    // Applica +20% idratazione (1 punto se max=5)
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
        
        if (pot.LedSystemState == LedSystemState.Off)
        {
            // Sistema OFF: decadimento graduale se era acceso
            bool hadBlueDays = pot.DaysLedBlueConsecutive > 0;
            bool hadRedDays = pot.DaysLedRedConsecutive > 0;
            
            if (pot.DaysLedBlueConsecutive > 0)
                pot.DaysLedBlueConsecutive = Mathf.Max(0, pot.DaysLedBlueConsecutive - 1);
            if (pot.DaysLedRedConsecutive > 0)
                pot.DaysLedRedConsecutive = Mathf.Max(0, pot.DaysLedRedConsecutive - 1);
            
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
        
        // Toast avviso zona rossa (4+ giorni)
        if (consecutiveDays >= 4)
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
        
        // TODO BLK-02.08: Applicare malus (Burn Stress, Mold Risk) quando sistemi saranno implementati
        // Per ora solo log
        if (consecutiveDays >= 4 && enableDebugLogs)
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
    /// BLK-02.07: Mostra notifica LED (helper per toast)
    /// </summary>
    private void ShowLedNotification(string message, Color color)
    {
        if (_toastManager != null)
        {
            // Determina tipo basato su colore o messaggio
            ToastNotificationType type = ToastNotificationType.Info;
            if (message.Contains("CRY insufficiente") || message.Contains("spento"))
                type = ToastNotificationType.SystemDisabled;
            
            string code = message.Contains("Blue") ? "LGT-003" : message.Contains("Red") ? "LGT-004" : "LGT-001";
            _toastManager.ShowToast(type, message, code);
        }
        else if (_uiNotification != null)
        {
            _uiNotification.ShowNotification(message, 3f, color);
        }
        else if (enableDebugLogs)
        {
            SporiumLogger.LogWarning(LogCategory.UI, $"UINotification non disponibile per: {message}");
        }
    }
    
    /// <summary>
    /// Trova PotSlot per un PotId (helper per eventi)
    /// </summary>
    private PotSlot FindPotSlot(string potId)
    {
        // Cerca PotSlot nel sistema
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
        int maxHydration = _potSystemConfig != null ? _potSystemConfig.MaxHydration : 4;
        
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
        // Cerca tutti i PotGrowthController nella scena
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
            
            // Calcola drift pH per questa pianta
            float plantDrift = plantData.GetDailyPhDrift();
            totalPhDrift += plantDrift;
            plantCount++;
            
            // Registra ogni pianta individualmente per tooltip dettagliato (con giorno di riferimento)
            if (plantDrift != 0f)
            {
                _phSystem.RegisterPlantDrift(plantDrift, plantData.PlantCode, pot.PotId, currentDay);
            }
            
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
        if (_phSystem == null || _potSystemConfig == null)
        {
            if (enableDebugLogs)
                SporiumLogger.LogWarning(LogCategory.Pot, "PhSystem o PotSystemConfig non disponibili per calcolo condizione");
            return;
        }
        
        foreach (var pot in _registeredPots)
        {
            if (pot == null || !pot.HasPlant)
                continue;
            
            // Salva score precedente per calcolo forecast
            int previousScore = pot.PreviousDayConditionScore >= 0 ? pot.PreviousDayConditionScore : pot.ConditionScore;
            bool isFirstCalculation = pot.PreviousDayConditionScore < 0;
            
            // Ottieni PlantData
            PlantData plantData = pot.GetPlantData();
            if (plantData == null)
            {
                if (enableDebugLogs)
                    SporiumLogger.LogWarning(LogCategory.Pot, $"{pot.PotId}: PlantData non trovato per calcolo condizione");
                continue;
            }
            
            // DEBUG: Log dati prima del calcolo
            if (enableDebugLogs)
            {
                int maxHydration = _potSystemConfig != null ? _potSystemConfig.MaxHydration : 5;
                int hydrationPercent = maxHydration > 0 ? Mathf.RoundToInt((float)pot.Hydration / maxHydration * 100f) : 0;
                float currentPh = _phSystem != null ? _phSystem.CurrentPh : 0f;
                bool isOverwatering = PlantConditionSystem.IsOverwatering(pot, maxHydration);
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
            
            // Salva score precedente prima di aggiornare
            pot.PreviousDayConditionScore = pot.ConditionScore;
            
            // Aggiorna score e condizione
            int oldCondition = pot.ConditionLabel;
            pot.ConditionScore = result.Score;
            pot.ConditionLabel = (int)result.Condition;
            pot.ForecastDirection = (int)result.Forecast;
            
            // Verifica cambio condizione per notifica Toast
            // Mostra il toast solo se il delta score è almeno ±20 (20%) per evitare spam su variazioni minime
            // BUG FIX: Verifica anche che il delta sia effettivamente negativo per "peggiorata" o positivo per "migliorata"
            if (!isFirstCalculation && oldCondition != pot.ConditionLabel && _uiNotification != null && Mathf.Abs(result.ScoreDelta) >= 20)
            {
                string conditionName = PlantConditionSystem.GetConditionName(result.Condition, 
                    PlantConditionSystem.IsOverwatering(pot, _potSystemConfig.MaxHydration));
                string forecastSymbol = PlantConditionSystem.GetForecastSymbol(result.Forecast);
                
                // Determina tipo notifica in base alla direzione del cambio
                Color notificationColor;
                
                // BUG FIX: Verifica che il delta sia effettivamente positivo per miglioramento o negativo per peggioramento
                // per evitare toast falsi quando il calcolo usa dati non aggiornati
                if (pot.ConditionLabel < oldCondition && result.ScoreDelta > 0) // Miglioramento (condizione migliore E score aumentato)
                {
                    notificationColor = Color.green;
                    string message = $"CND-002 - Condizione migliorata: {conditionName} ({result.Score}/100) {forecastSymbol}";
                    if (_toastManager != null)
                    {
                        _toastManager.ShowToast(ToastNotificationType.ConditionImproved, message, "CND-002");
                    }
                    else if (_uiNotification != null)
                    {
                        _uiNotification.ShowNotification(message, 3f, notificationColor);
                    }
                }
                else if (pot.ConditionLabel > oldCondition && result.ScoreDelta < 0) // Peggioramento (condizione peggiore E score diminuito)
                {
                    notificationColor = Color.yellow;
                    string message = $"CND-001 - Condizione peggiorata: {conditionName} ({result.Score}/100) {forecastSymbol}";
                    if (_toastManager != null)
                    {
                        _toastManager.ShowToast(ToastNotificationType.ConditionDegraded, message, "CND-001");
                    }
                    else if (_uiNotification != null)
                    {
                        _uiNotification.ShowNotification(message, 3f, notificationColor);
                    }
                }
                // Se il delta non corrisponde alla direzione del cambio condizione, non mostrare toast (evita falsi positivi)
                
                if (enableDebugLogs)
                {
                    SporiumLogger.LogInfo(LogCategory.Pot, $"{pot.PotId}: Condizione cambiata da {oldCondition} a {pot.ConditionLabel} ({conditionName}) - Score: {result.Score}/100, Forecast: {forecastSymbol}, Δ: {result.ScoreDelta}");
                }
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
                bool isOverwatering = PlantConditionSystem.IsOverwatering(pot, _potSystemConfig.MaxHydration);
                if (isOverwatering)
                {
                    pot.DaysOverwateringConsecutive++;
                }
                else
                {
                    pot.DaysOverwateringConsecutive = 0;
                }
                
                // Incrementa giorni senza potatura
                pot.DaysWithoutPruning++;
                
                // Calcola mold risk
                int oldMoldRiskLevel = pot.MoldRiskLevel;
                pot.MoldRiskLevel = MoldSystem.GetMoldRiskLevel(pot, _phSystem, plantData, moldConfig);
                
                // BUG FIX 2: Tracking giorni a livello 3 per infestazione
                if (pot.MoldRiskLevel == 3)
                {
                    pot.DaysAtMoldRiskLevel3++;
                }
                else
                {
                    pot.DaysAtMoldRiskLevel3 = 0; // Reset se non è più a livello 3
                }
                
                // BUG FIX 2: Infestazione solo dopo 2 giorni consecutivi a livello 3
                bool shouldInfest = MoldSystem.CheckInfestation(pot.MoldRiskLevel, pot.DaysAtMoldRiskLevel3);
                if (shouldInfest && !pot.IsInfested)
                {
                    // Prima infestazione: applica effetti e mostra toast
                    pot.IsInfested = true;
                    PlantLevelConfig levelConfig = Resources.Load<PlantLevelConfig>("Configs/PlantLevelConfig");
                    MoldSystem.ApplyInfestation(pot, pot.MoldRiskLevel, moldConfig, levelConfig);
                    
                    // Toast notifica infestazione
                    if (_toastManager != null)
                    {
                        _toastManager.ShowWarning($"La pianta nel pot {pot.PotId} è ora Infestata", "MOLD-001");
                    }
                    else if (_uiNotification != null)
                    {
                        _uiNotification.ShowNotification(
                            $"La pianta nel pot {pot.PotId} è ora Infestata",
                            4f,
                            Color.red);
                    }
                    
                    SporiumLogger.LogWarning(LogCategory.Pot, $"{pot.PotId}: Infestazione applicata dopo {pot.DaysAtMoldRiskLevel3} giorni a livello 3");
                }
                else if (!shouldInfest && pot.IsInfested)
                {
                    // Infestazione rimossa (livello sceso sotto 3)
                    pot.IsInfested = false;
                    SporiumLogger.LogInfo(LogCategory.Pot, $"{pot.PotId}: Infestazione rimossa (livello sceso a {pot.MoldRiskLevel})");
                }
                
                if (enableDebugLogs && pot.MoldRiskLevel > 0)
                {
                    SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Mold Risk Level: {pot.MoldRiskLevel} (DaysOverwatering: {pot.DaysOverwateringConsecutive}, DaysWithoutPruning: {pot.DaysWithoutPruning})");
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
        
        // Resetta stato pianta (come in DoFertilize per morte fertilizzante)
        pot.HasPlant = false;
        pot.PlantCode = null;
        pot.Stage = 0;
        pot.Hydration = 0;
        pot.LightExposure = 0;
        pot.FertilizerLevel = 0;
        pot.DaysSincePlant = 0;
        pot.DaysInCurrentStage = 0;
        pot.GrowthPoints = 0;
        pot.DaysFertilizerActive = 0;
        pot.DaysInExtremePh = 0;
        pot.ExtremePhDeathCountdown = -1;
        
        // Notifica evento morte pianta
        string reason = $"pH estremo opposto ({phBand}) per famiglia {plantData.Family}";
        PotEvents.EmitPlantDied(pot.PotId, reason);
        
        // Mostra Toast notifica morte
        if (_toastManager != null)
        {
            _toastManager.ShowToast(ToastNotificationType.ExtremePhDeath, 
                $"🚨 Pianta {plantData.PlantCode} morta per pH estremo!", 
                "PH-DEATH-001");
        }
        else if (_uiNotification != null)
        {
            _uiNotification.ShowNotification(
                $"🚨 Pianta {plantData.PlantCode} morta per pH estremo!",
                4f,
                new Color(1f, 0.2f, 0.2f)); // Rosso per morte
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
            string message = $"⚠️ La pianta {plantData.PlantCode} tra {countdown} giorni morirà a causa del pH estremo!";
            if (_toastManager != null)
            {
                _toastManager.ShowToast(ToastNotificationType.CountdownAlert, message, "PH-COUNTDOWN-001");
            }
            else if (_uiNotification != null)
            {
                _uiNotification.ShowNotification(
                    message,
                    4f,
                    new Color(1f, 0.5f, 0f)); // Arancione per allerta
            }
        }
    }
    
}

