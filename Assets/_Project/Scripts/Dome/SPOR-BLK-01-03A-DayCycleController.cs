using System.Collections.Generic;
using _Project.Sporae.Core;
using UnityEngine;
using Sporae.Core;
using Sporae.Dome.PotSystem.Growth;
using UnityEngine.SceneManagement;
using _Project;

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

    private void Awake()
    {
        growthConfig = Resources.Load<PlantGrowthConfig>("Configs/PlantGrowthConfig");
        if (!growthConfig)
            Debug.LogWarning($"[{nameof(DayCycleSystem)}] PlantGrowthConfig non trovato in Resources/Configs/, verrà cercato in PotSystemConfig");

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
        
        // Cerca configurazione in PotSystemConfig se non trovata
        if (growthConfig == null)
        {
            var potSystemConfig = FindObjectOfType<PotSystemConfig>();
            if (potSystemConfig != null && potSystemConfig.GrowthConfig != null)
            {
                growthConfig = potSystemConfig.GrowthConfig;
                if (enableDebugLogs)
                    Debug.Log("[BLK-01.03A] DayCycleController: Configurazione caricata da PotSystemConfig");
            }
        }

        // Verifica configurazione
        if (growthConfig == null)
        {
            Debug.LogError("[BLK-01.03A] DayCycleController: Nessuna configurazione di crescita trovata!");
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
                    Debug.Log($"[BLK-01.03A] DayCycleController: PotSystemConfig trovato con nome alternativo '{_potSystemConfig.name}'");
            }
        }
        
        if (_potSystemConfig == null && enableDebugLogs)
        {
            Debug.LogWarning("[BLK-01.03A] DayCycleController: PotSystemConfig non trovato in Resources/Configs/, userò valori di default (MaxHydration=3, MaxLightExposure=3)");
        }
        else if (_potSystemConfig != null && enableDebugLogs)
        {
            Debug.Log($"[BLK-01.03A] DayCycleController: PotSystemConfig caricato - MaxHydration={_potSystemConfig.MaxHydration}, MaxLightExposure={_potSystemConfig.MaxLightExposure}");
        }

        _isInitialized = true;
        if (enableDebugLogs)
            Debug.Log($"[BLK-01.03A] DayCycleController: Inizializzato con config '{growthConfig.name}'");
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
        
        // Sottoscrivi all'evento OnServiceRegistered per quando PhSystem viene registrato dopo
        if (ServiceContainer.Instance != null)
        {
            ServiceContainer.Instance.OnServiceRegistered += OnServiceRegistered;
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
            _phSystem = ServiceContainer.Instance.Get<PhSystem>();
            if (_phSystem != null && enableDebugLogs)
            {
                Debug.Log("[DayCycleController] PhSystem trovato e collegato!");
            }
        }
        catch
        {
            // PhSystem non ancora registrato, sarà recuperato quando viene registrato
            _phSystem = null;
        }
    }
    
    /// <summary>
    /// Chiamato quando un servizio viene registrato nel ServiceContainer
    /// </summary>
    private void OnServiceRegistered(object service)
    {
        if (service is PhSystem phSystem && _phSystem == null)
        {
            _phSystem = phSystem;
            if (enableDebugLogs)
            {
                Debug.Log("[DayCycleController] PhSystem registrato! Collegato al sistema di crescita.");
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
                Debug.Log($"[BLK-01.03A] DayCycleController: Registrato vaso {pot.PotId}");
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
                Debug.Log($"[BLK-01.03A] DayCycleController: Rimosso vaso {pot.PotId}");
        }
    }

    /// <summary>
    /// Gestisce il cambio di giorno dal GameManager
    /// </summary>
    private void HandleDayChanged(int dayIndex)
    {
        if (enableDebugLogs)
            Debug.Log($"[BLK-01.03A] DayCycleController: HandleDayChanged chiamato per Day {dayIndex}");
        
        if (growthConfig == null)
        {
            Debug.LogError("[BLK-01.03A] DayCycleController: Nessuna configurazione di crescita trovata!");
            return;
        }
        
        // Pipeline End Day per il giorno D:
        // 1. ResolveGrowthForAllPots(D)
        ResolveGrowthForAllPots(dayIndex);
        
        // 2. Calcola e registra pH drift dalle piante (integrazione pH)
        CalculateAndRegisterPhDrift();
        
        // 3. ApplyDecayAndCleanup(D)
        ApplyDecayAndCleanup(dayIndex);
        
        // 4. AdvanceDayHUD() - gestito automaticamente dal GameManager esistente
        
        if (enableDebugLogs)
            Debug.Log($"[BLK-01.03A] DayCycleController: Growth tick completato per Day {dayIndex}");
    }

    /// <summary>
    /// Risolve la crescita per tutti i vasi registrati
    /// </summary>
    private void ResolveGrowthForAllPots(int dayIndex)
    {
        if (enableDebugLogs)
            Debug.Log($"[BLK-01.03A] DayCycleController: Applicazione crescita a {_registeredPots.Count} vasi per Day {dayIndex}");

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
                Debug.LogWarning($"[BLK-02.02] {pot.PotId}: PlantData non trovato, uso sistema base");
            // Fallback al sistema base se non c'è PlantData
            ResolveGrowthForPotLegacy(pot, dayIndex);
            return;
        }
        
        // Calcola idratazione percentuale (0-100%)
        int maxHydration = _potSystemConfig != null ? _potSystemConfig.MaxHydration : 3;
        int hydrationPercent = maxHydration > 0 ? Mathf.RoundToInt((float)pot.Hydration / maxHydration * 100f) : 0;
        
        // BLK-02.02: Fix - Confronta con il giorno precedente perché i timestamp
        // vengono impostati con gameManager.CurrentDay, ma dayIndex è il giorno corrente
        // dopo che EndDay ha già incrementato il giorno
        int previousDay = dayIndex - 1;
        bool hadHydration = (pot.LastWateredDay == previousDay);
        bool hadLight = (pot.LastLitDay == previousDay);
        
        // Incrementa giorni nello stadio corrente
        int oldStage = pot.Stage;
        pot.DaysInCurrentStage++;
        
        // Gestione produzione frutti in HarvestReady
        if (pot.Stage == (int)PlantStage.HarvestReady)
        {
            pot.DaysInHarvestReady++;
            
            // Produzione frutti incrementale: +1 frutto/giorno fino a 3 max
            if (pot.DaysInHarvestReady == 1)
            {
                // Primo giorno: inizializza a 1 frutto
                pot.AmountFruits = 1f;
            }
            else if (pot.DaysInHarvestReady > 1 && pot.AmountFruits < 3f)
            {
                // Giorni successivi: +1 frutto/giorno fino a 3 max
                pot.AmountFruits = Mathf.Min(pot.AmountFruits + 1f, 3f);
            }
            
            // Decay frutti dopo 3 giorni non raccolti
            if (pot.AmountFruits > 0f)
            {
                pot.DaysFruitsUnharvested++;
                if (pot.DaysFruitsUnharvested >= 3)
                {
                    // Decay: perde tutti i frutti dopo 3 giorni
                    pot.AmountFruits = 0f;
                    pot.DaysFruitsUnharvested = 0;
                    if (enableDebugLogs)
                        Debug.Log($"[BLK-02.02] {pot.PotId}: ⚠️ Frutti decaduti dopo 3 giorni non raccolti");
                }
            }
        }
        else
        {
            // Reset contatori frutti se non è in HarvestReady
            pot.DaysInHarvestReady = 0;
            pot.DaysFruitsUnharvested = 0;
        }
        
        // BLK-02.02: Verifica requisiti per avanzamento stadio
        bool stageChanged = false;
        PlantStage currentStage = (PlantStage)pot.Stage;
        
        // Ottieni requisiti per lo stadio corrente
        StageRequirements currentStageReq = plantData.GetStageRequirements(currentStage);
        
        // Verifica se i requisiti sono soddisfatti
        bool requirementsMet = false;
        if (currentStageReq != null)
        {
            // Verifica idratazione nel range
            bool hydrationOk = currentStageReq.IsHydrationInRange(hydrationPercent);
            
            // Verifica LED richiesto
            bool ledOk = currentStageReq.IsLedRequirementMet(pot.LastLedType);
            
            // Verifica giorni minimi nello stadio
            bool durationOk = pot.DaysInCurrentStage >= currentStageReq.durationDays;
            
            requirementsMet = hydrationOk && ledOk && durationOk;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[BLK-02.02] {pot.PotId}: Stage {currentStage} requisiti - Hydration: {hydrationPercent}% (range: {currentStageReq.hydrationMin}-{currentStageReq.hydrationMax}) [{hydrationOk}], LED: {pot.LastLedType} (richiesto: {currentStageReq.GetRequiredLed()}) [{ledOk}], Durata: {pot.DaysInCurrentStage}/{currentStageReq.durationDays} giorni [{durationOk}]");
            }
        }
        else
        {
            // Se non ci sono requisiti specifici, considera sempre soddisfatti
            requirementsMet = true;
            if (enableDebugLogs)
                Debug.Log($"[BLK-02.02] {pot.PotId}: Nessun requisito specifico per stage {currentStage}, avanzamento automatico");
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
                    stageChanged = true;
                    if (enableDebugLogs)
                        Debug.Log($"[BLK-02.02] {pot.PotId}: 🎉 Avanzamento Seed → Sprout!");
                    break;
                    
                case PlantStage.Sprout:
                    // Sprout → Growth: richiede requisiti soddisfatti
                    pot.Stage = (int)PlantStage.Growth;
                    pot.DaysInCurrentStage = 0;
                    stageChanged = true;
                    if (enableDebugLogs)
                        Debug.Log($"[BLK-02.02] {pot.PotId}: 🌱 Avanzamento Sprout → Growth!");
                    break;
                    
                case PlantStage.Growth:
                    // Growth → Flowering: richiede 2 giorni consecutivi con requisiti soddisfatti
                    // (verificato tramite durationDays >= 2)
                    pot.Stage = (int)PlantStage.Flowering;
                    pot.DaysInCurrentStage = 0;
                    stageChanged = true;
                    if (enableDebugLogs)
                        Debug.Log($"[BLK-02.02] {pot.PotId}: 🌸 Avanzamento Growth → Flowering!");
                    break;
                    
                case PlantStage.Flowering:
                    // Flowering → HarvestReady: richiede requisiti soddisfatti
                    pot.Stage = (int)PlantStage.HarvestReady;
                    pot.DaysInCurrentStage = 0;
                    pot.DaysInHarvestReady = 0; // Reset contatore HarvestReady
                    pot.AmountFruits = 0f; // Inizializza frutti
                    stageChanged = true;
                    if (enableDebugLogs)
                        Debug.Log($"[BLK-02.02] {pot.PotId}: 🍎 Avanzamento Flowering → HarvestReady!");
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
                    Debug.Log($"[BLK-02.02] {pot.PotId}: Trovato PotGrowthController, chiamando OnStageChanged...");
                potGrowthController.OnStageChanged((PlantStage)pot.Stage);
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[BLK-02.02] {pot.PotId}: PotGrowthController NON TROVATO! Le visuali non saranno aggiornate.");
            }
            
            // Emetti evento per l'UI
            PotEvents.EmitPlantStageChanged(pot.PotId, (PlantStage)pot.Stage);
            
            if (enableDebugLogs)
                Debug.Log($"[BLK-02.02] {pot.PotId}: Eventi emessi per cambio stadio {oldStage} → {pot.Stage}");
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
        int previousDay = dayIndex - 1;
        bool hadHydration = (pot.LastWateredDay == previousDay);
        bool hadLight = (pot.LastLitDay == previousDay);
        
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
    /// Applica decadimento e pulizia (SENZA reset dei timestamp!)
    /// </summary>
    private void ApplyDecayAndCleanup(int dayIndex)
    {
        foreach (var pot in _registeredPots)
        {
            if (pot != null && pot.HasPlant)
            {
                // Decadimento idratazione
                pot.Hydration = Mathf.Max(0, pot.Hydration - growthConfig.dailyHydrationDecay);
                
                // Reset esposizione luce (ma NON i timestamp!)
                pot.LightExposure = 0;

                if (enableDebugLogs)
                {
                    Debug.Log($"[BLK-01.03A] {pot.PotId}: Decay applicato - Hydration: {pot.Hydration}, Light: {pot.LightExposure}");
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
            Debug.Log($"[BLK-01.03A] DayCycleController: Nuova configurazione impostata: {config?.name ?? "NULL"}");
    }

    #if UNITY_EDITOR
    [ContextMenu("Log Registered Pots")]
    private void EditorLogRegisteredPots()
    {
        Debug.Log($"[BLK-01.03A] DayCycleController: Vasi registrati ({_registeredPots.Count}):");
        for (int i = 0; i < _registeredPots.Count; i++)
        {
            var pot = _registeredPots[i];
            if (pot != null)
            {
                string plantInfo = pot.HasPlant 
                    ? $" - {GetStageName(pot.Stage)} (Giorno {pot.DaysSincePlant})" 
                    : " - Vuoto";
                Debug.Log($"  [{i}] {pot.PotId}{plantInfo}");
            }
            else
            {
                Debug.Log($"  [{i}] NULL (da rimuovere)");
            }
        }
    }

    [ContextMenu("Cleanup Null Pots")]
    private void EditorCleanupNullPots()
    {
        _registeredPots.RemoveAll(pot => pot == null);
        if (enableDebugLogs)
            Debug.Log($"[BLK-01.03A] DayCycleController: Cleanup completato, {_registeredPots.Count} vasi validi");
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
    private void CalculateAndRegisterPhDrift()
    {
        if (_phSystem == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[DayCycleController] PhSystem non disponibile, impossibile calcolare drift pH");
            return;
        }
        
        // IMPORTANTE: Prima rimuovi i contributi delle piante che non sono più nei vasi registrati
        // Questo gestisce il caso in cui una pianta è stata rimossa con UPROOT ma i contributi sono ancora presenti
        CleanupRemovedPlantContributions();
        
        float totalPhDrift = 0f;
        int plantCount = 0;
        int skippedCount = 0;
        
        if (enableDebugLogs)
            Debug.Log($"[DayCycleController] Calcolo drift pH per {_registeredPots.Count} vasi registrati...");
        
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
                    Debug.Log($"[DayCycleController] {pot.PotId}: Vaso vuoto (HasPlant=false), saltato");
                skippedCount++;
                continue;
            }
            
            // IMPORTANTE: Verifica anche che lo stage non sia Empty (0) - doppio controllo
            if (pot.Stage == (int)PlantStage.Empty)
            {
                if (enableDebugLogs)
                    Debug.Log($"[DayCycleController] {pot.PotId}: Stage è Empty, saltato (pianta probabilmente rimossa con UPROOT)");
                skippedCount++;
                continue;
            }
            
            // DEBUG: Verifica PlantCode
            if (string.IsNullOrEmpty(pot.PlantCode))
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[DayCycleController] ⚠️ {pot.PotId}: PlantCode è NULL o vuoto! Stage: {pot.Stage} (HasPlant: {pot.HasPlant})");
                skippedCount++;
                continue;
            }
            
            // Ottieni PlantData dalla pianta
            PlantData plantData = pot.GetPlantData();
            if (plantData == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[DayCycleController] ⚠️ {pot.PotId}: PlantData non trovato per PlantCode '{pot.PlantCode}'");
                skippedCount++;
                continue;
            }
            
            // Calcola drift pH per questa pianta
            float plantDrift = plantData.GetDailyPhDrift();
            totalPhDrift += plantDrift;
            plantCount++;
            
            // Registra ogni pianta individualmente per tooltip dettagliato
            if (plantDrift != 0f)
            {
                _phSystem.RegisterPlantDrift(plantDrift, plantData.PlantCode, pot.PotId);
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"[DayCycleController] ✅ {pot.PotId}: {plantData.PlantCode} ({plantData.Family}) Stage:{pot.Stage} → drift pH: {plantDrift:F2}/giorno");
            }
        }
        
        // Log riepilogativo
        if (totalPhDrift != 0f && enableDebugLogs)
        {
            Debug.Log($"[DayCycleController] ✅ pH Drift totale da {plantCount} piante: {totalPhDrift:F2} → pH attuale: {_phSystem.CurrentPh:F2}");
        }
        else if (enableDebugLogs)
        {
            if (plantCount > 0)
            {
                Debug.Log($"[DayCycleController] ⚠️ Nessun drift pH da {plantCount} piante (tutte Standard o drift = 0)");
            }
            else if (skippedCount > 0)
            {
                Debug.LogWarning($"[DayCycleController] ⚠️ Nessuna pianta valida trovata! {skippedCount} vasi saltati (vuoti o senza PlantCode)");
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
            Debug.Log($"[DayCycleController] 🔍 Cleanup: {activePotIds.Count} vasi attivi su {_registeredPots.Count} registrati");
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
}

