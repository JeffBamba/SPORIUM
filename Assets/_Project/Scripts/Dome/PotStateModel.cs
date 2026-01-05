using System;
using Sporae.Dome.PotSystem.Growth;
using UnityEngine;
using System.IO;
using Sporae.DevTools;

/// <summary>
/// Modello dati per lo stato di un vaso.
/// Contiene tutte le informazioni necessarie per tracciare lo stato della pianta,
/// idratazione, esposizione alla luce e timestamp delle azioni.
/// Compatibile con BLK-01.04 (crescita) e BLK-01.05 (frutto).
/// </summary>
[Serializable]
public class PotStateModel
{
    [Header("Pot Identification")]
    public string PotId;          // "POT-001", "POT-002"
    
    [Header("Plant State")]
    public bool HasPlant;         // True se il vaso contiene una pianta
    public int Stage;             // 0=Seeded (placeholder BLK-01.02), 1-3 per crescita futura
    public float AmountFruits;
    
    [Header("Plant Data (pH Integration)")]
    [Tooltip("Codice pianta (es. PLT-STD-001) per lookup PlantData")]
    public string PlantCode;      // Codice pianta per lookup PlantData
    
    [Header("Resource Levels")]
    public int Hydration;         // 0..MaxHydration
    public int LightExposure;     // 0..MaxLightExposure
    
    [Header("Growth System (BLK-01.03A)")]
    public int GrowthPoints;              // Progress interno allo stadio attuale
    public int DaysSincePlant;            // Giorni dalla semina
    public int DaysNeglectedStreak;       // Giorni consecutivi senza cura
    
    [Header("Stage Requirements Tracking (BLK-02.02)")]
    [Tooltip("Giorni consecutivi nello stesso stadio (per transizioni che richiedono più giorni)")]
    public int DaysInCurrentStage;        // Giorni consecutivi nello stadio corrente
    [Tooltip("Giorni in HarvestReady (per produzione frutti incrementale)")]
    public int DaysInHarvestReady;        // Giorni consecutivi in HarvestReady
    [Tooltip("Giorni frutti non raccolti (per decay dopo 3 giorni)")]
    public int DaysFruitsUnharvested;     // Giorni consecutivi con frutti non raccolti
    
    [Header("Timestamps (BLK-01.03A)")]
    public int PlantedDay;        // Giorno in cui è stato piantato il seme
    public int LastWateredDay;    // Ultimo giorno di annaffiatura
    public int LastLitDay;        // Ultimo giorno di illuminazione
    
    [Header("LED Tracking (BLK-02.03)")]
    [Tooltip("Ultimo tipo LED utilizzato (Blue/Red)")]
    public LedType? LastLedType;  // Null se mai usato LED, Blue o Red se usato
    
    [Header("LED System (BLK-02.07 - Persistent Toggle)")]
    [Tooltip("Stato sistema LED: Off, Blue, Red")]
    public LedSystemState LedSystemState = LedSystemState.Off;
    [Tooltip("Giorni consecutivi con BLUE LED attivo")]
    public int DaysLedBlueConsecutive = 0;
    [Tooltip("Giorni consecutivi con RED LED attivo")]
    public int DaysLedRedConsecutive = 0;
    
    [Header("Watering System (GDD AZ-11 - Toggle Persistente)")]
    [Tooltip("Sistema irrigazione a goccia ON/OFF persistente")]
    public bool WateringSystemOn;  // Stato toggle ON/OFF
    [Tooltip("Giorni consecutivi con sistema ON (per effetti accumulati)")]
    public int DaysWateringSystemOn;  // Contatore giorni ON
    [Tooltip("Accumulatore WAT-RAW per consumo 1 ogni 2 giorni (0.5 per giorno ON)")]
    public float WateringRawWaterAccumulator;  // Accumulo frazionario
    
    [Header("Plant Condition System")]
    [Tooltip("Score di condizione (0-100)")]
    public int ConditionScore = 50;  // Score neutro di default
    [Tooltip("Score del giorno precedente (per calcolo forecast)")]
    public int PreviousDayConditionScore = -1;  // -1 se non disponibile
    [Tooltip("Condizione attuale (Rigogliosa/Sana/Appassita/Critica) - Stressata rimosso dalla logica, mantenuto solo enum per retrocompatibilità")]
    public int ConditionLabel = 1;  // Default: Sana (enum PlantCondition)
    [Tooltip("Direzione forecast (Up/Stable/Down)")]
    public int ForecastDirection = 1;  // Default: Stable (enum ForecastDirection)
    
    [Header("Fertilizer System (BLK-03.01-T1)")]
    [Tooltip("Livello fertilizzante attuale (0-100)")]
    public int FertilizerLevel = 0;  // 0 = nessun fertilizzante
    [Tooltip("Giorni consecutivi con fertilizzante applicato")]
    public int DaysFertilizerActive = 0;
    
    [Header("Growth Points System (BLK-03.01-T2)")]
    [Tooltip("Punti crescita accumulati per idratazione (1 punto per giorno nel range ideale)")]
    public int GrowthPointsWater = 0;
    [Tooltip("Punti crescita accumulati per luce (1 punto per giorno nel range ideale)")]
    public int GrowthPointsLight = 0;
    [Tooltip("Punti crescita accumulati per fertilizzante (1 punto per giorno nel range ideale)")]
    public int GrowthPointsFertilizer = 0;
    
    [Header("Optimal Parameters Tracking (BLK-03.01-T2)")]
    [Tooltip("Giorni consecutivi con tutti i parametri ottimali (water + light + fertilizer nel range)")]
    public int DaysConsecutiveOptimal = 0;
    [Tooltip("Giorno in cui sono iniziati i parametri ottimali (-1 se non attivi)")]
    public int DayOptimalParametersStarted = -1;
    
    [Header("Pruning System (AZ-13)")]
    [Tooltip("Flag per cap bonus resa: non cumulabile più di 1 volta per ciclo")]
    public bool HasPruningResaBonus = false;
    
    [Header("Plant Level System (BLK-02.02)")]
    [Tooltip("Livello corrente pianta (1-5)")]
    public int PlantLevel = 1;  // Default: Lvl 1
    
    [Tooltip("Cicli completati (per progressione livello)")]
    public int CompletedCycles = 0;
    
    [Header("Mold System (BLK-07.01)")]
    [Tooltip("Livello rischio muffe (0=None, 1=Mild, 2=Severe, 3=Critical)")]
    public int MoldRiskLevel = 0;
    
    [Tooltip("Giorni consecutivi senza potatura")]
    public int DaysWithoutPruning = 0;
    
    [Tooltip("Giorni consecutivi in overwatering")]
    public int DaysOverwateringConsecutive = 0;
    
    [Tooltip("Giorni consecutivi a Mold Risk Level 3 (per infestazione dopo 2 giorni)")]
    public int DaysAtMoldRiskLevel3 = 0;
    
    [Tooltip("Flag per indicare se la pianta è infestata (true dopo 2 giorni a livello 3)")]
    public bool IsInfested = false;
    
    [Header("pH Extreme Death System")]
    [Tooltip("Giorni consecutivi in pH estremo opposto alla famiglia pianta (≥+80 per Evil, ≤-80 per Pure)")]
    public int DaysInExtremePh = 0;
    
    [Tooltip("Countdown giorni rimanenti prima della morte (-1 se non attivo)")]
    public int ExtremePhDeathCountdown = -1;
    
    [Header("Debug Console Manual Override (Console P)")]
    [Tooltip("Flag per indicare se Hydration è stato impostato manualmente dalla console P")]
    public bool IsHydrationManuallySet = false;
    
    [Tooltip("Valore base Hydration da cui partire per il decay (impostato manualmente)")]
    public int ManualHydrationBase = -1;
    
    [Tooltip("Flag per indicare se LightExposure è stato impostato manualmente dalla console P")]
    public bool IsLightExposureManuallySet = false;
    
    [Tooltip("Valore base LightExposure da cui partire per il decay (impostato manualmente)")]
    public int ManualLightExposureBase = -1;
    
    [Tooltip("Flag per indicare se FertilizerLevel è stato impostato manualmente dalla console P")]
    public bool IsFertilizerManuallySet = false;
    
    [Tooltip("Valore base FertilizerLevel da cui partire per il decay (impostato manualmente)")]
    public int ManualFertilizerBase = -1;
    
    /// <summary>
    /// Crea un nuovo stato di vaso vuoto
    /// </summary>
    public PotStateModel(string potId)
    {
        PotId = potId;
        HasPlant = false;
        Stage = 0;
        Hydration = 0;
        LightExposure = 0;
        GrowthPoints = 0;
        DaysSincePlant = 0;
        DaysNeglectedStreak = 0;
        DaysInCurrentStage = 0;
        DaysInHarvestReady = 0;
        DaysFruitsUnharvested = 0;
        PlantedDay = 0;
        LastWateredDay = 0;
        LastLitDay = 0;
        LastLedType = null;
        PlantCode = null;
        WateringSystemOn = false;
        DaysWateringSystemOn = 0;
        WateringRawWaterAccumulator = 0f;
        FertilizerLevel = 0;
        DaysFertilizerActive = 0;
        // BLK-03.01-T2: Inizializza campi punti crescita
        GrowthPointsWater = 0;
        GrowthPointsLight = 0;
        GrowthPointsFertilizer = 0;
        DaysConsecutiveOptimal = 0;
        DayOptimalParametersStarted = -1;
        DaysAtMoldRiskLevel3 = 0;
        IsInfested = false;
        DaysInExtremePh = 0;
        ExtremePhDeathCountdown = -1;
        IsHydrationManuallySet = false;
        ManualHydrationBase = -1;
        IsLightExposureManuallySet = false;
        ManualLightExposureBase = -1;
        IsFertilizerManuallySet = false;
        ManualFertilizerBase = -1;
    }
    
    /// <summary>
    /// Crea un nuovo stato di vaso con pianta
    /// </summary>
    public PotStateModel(string potId, int plantedDay)
    {
        PotId = potId;
        HasPlant = true;
        Stage = 1; // Seeded (1 = Seed, non 0 = Empty)
        Hydration = 0;
        LightExposure = 0;
        GrowthPoints = 0;
        DaysSincePlant = 0;
        DaysNeglectedStreak = 0;
        DaysInCurrentStage = 0;
        DaysInHarvestReady = 0;
        DaysFruitsUnharvested = 0;
        PlantedDay = plantedDay;
        LastWateredDay = 0;
        LastLitDay = 0;
        LastLedType = null;
        LedSystemState = LedSystemState.Off;
        DaysLedBlueConsecutive = 0;
        DaysLedRedConsecutive = 0;
        PlantCode = null;
        WateringSystemOn = false;
        DaysWateringSystemOn = 0;
        WateringRawWaterAccumulator = 0f;
        ConditionScore = 50;
        PreviousDayConditionScore = -1;
        ConditionLabel = 1;  // Sana
        ForecastDirection = 1;  // Stable
        FertilizerLevel = 0;
        DaysFertilizerActive = 0;
        // BLK-03.01-T2: Inizializza campi punti crescita
        GrowthPointsWater = 0;
        GrowthPointsLight = 0;
        GrowthPointsFertilizer = 0;
        DaysConsecutiveOptimal = 0;
        DayOptimalParametersStarted = -1;
        HasPruningResaBonus = false;
        PlantLevel = 1;
        CompletedCycles = 0;
        MoldRiskLevel = 0;
        DaysWithoutPruning = 0;
        DaysOverwateringConsecutive = 0;
        DaysInExtremePh = 0;
        ExtremePhDeathCountdown = -1;
    }
    
    /// <summary>
    /// Verifica se il vaso è vuoto
    /// </summary>
    public bool IsEmpty => !HasPlant;
    
    /// <summary>
    /// Verifica se il vaso ha una pianta
    /// </summary>
    public bool HasPlantGrowing => HasPlant && Stage >= 1; // 1 = Seed, 2 = Sprout, 3 = Growth, 4 = Flowering, 5 = HarvestReady, 6 = Resting
    
    /// <summary>
    /// Verifica se l'idratazione è al massimo
    /// </summary>
    public bool IsHydrationMax(int maxHydration) => Hydration >= maxHydration;
    
    /// <summary>
    /// Verifica se l'esposizione alla luce è al massimo
    /// </summary>
    public bool IsLightExposureMax(int maxLightExposure) => LightExposure >= maxLightExposure;
    
    /// <summary>
    /// Aumenta l'idratazione di 1, rispettando il limite massimo
    /// </summary>
    public bool IncreaseHydration(int maxHydration)
    {
        if (Hydration < maxHydration)
        {
            Hydration++;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Aumenta l'esposizione alla luce di 1, rispettando il limite massimo
    /// </summary>
    public bool IncreaseLightExposure(int maxLightExposure)
    {
        if (LightExposure < maxLightExposure)
        {
            LightExposure++;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Piantare un seme nel vaso
    /// </summary>
    public void PlantSeed(int currentDay, string plantCode = null)
    {
        HasPlant = true;
        Stage = 1; // Seeded (1 = Seed, non 0 = Empty)
        Hydration = 0;
        LightExposure = 0;
        GrowthPoints = 0;
        DaysSincePlant = 0;
        DaysNeglectedStreak = 0;
        DaysInCurrentStage = 0;
        DaysInHarvestReady = 0;
        DaysFruitsUnharvested = 0;
        PlantedDay = currentDay;
        LastWateredDay = 0;
        LastLitDay = 0;
        LastLedType = null;
        LedSystemState = LedSystemState.Off;
        DaysLedBlueConsecutive = 0;
        DaysLedRedConsecutive = 0;
        PlantCode = plantCode;
        WateringSystemOn = false;  // Nuova pianta = sistema OFF
        DaysWateringSystemOn = 0;
        WateringRawWaterAccumulator = 0f;
        ConditionScore = 50;
        PreviousDayConditionScore = -1;
        ConditionLabel = 1;  // Sana
        ForecastDirection = 1;  // Stable
        FertilizerLevel = 0;
        DaysFertilizerActive = 0;
        // BLK-03.01-T2: Inizializza campi punti crescita
        GrowthPointsWater = 0;
        GrowthPointsLight = 0;
        GrowthPointsFertilizer = 0;
        DaysConsecutiveOptimal = 0;
        DayOptimalParametersStarted = -1;
        HasPruningResaBonus = false;
        PlantLevel = 1;
        CompletedCycles = 0;
        MoldRiskLevel = 0;
        DaysWithoutPruning = 0;
        DaysOverwateringConsecutive = 0;
        DaysInExtremePh = 0;
        ExtremePhDeathCountdown = -1;
    }
    
    /// <summary>
    /// Aggiorna il timestamp dell'ultima annaffiatura
    /// </summary>
    public void UpdateWateringDay(int currentDay)
    {
        LastWateredDay = currentDay;
    }
    
    /// <summary>
    /// Aggiorna il timestamp dell'ultima illuminazione e il tipo LED utilizzato
    /// </summary>
    public void UpdateLightingDay(int currentDay, LedType? ledType = null)
    {
        LastLitDay = currentDay;
        if (ledType.HasValue)
        {
            LastLedType = ledType.Value;
        }
    }
    
    /// <summary>
    /// Resetta il vaso allo stato vuoto
    /// </summary>
    public void ResetToEmpty()
    {
        HasPlant = false;
        Stage = 0; // Empty (0 = Empty, 1 = Seed)
        Hydration = 0;
        LightExposure = 0;
        GrowthPoints = 0;
        DaysSincePlant = 0;
        DaysNeglectedStreak = 0;
        DaysInCurrentStage = 0;
        DaysInHarvestReady = 0;
        DaysFruitsUnharvested = 0;
        PlantedDay = 0;
        LastWateredDay = 0;
        LastLitDay = 0;
        LastLedType = null;
        LedSystemState = LedSystemState.Off;
        DaysLedBlueConsecutive = 0;
        DaysLedRedConsecutive = 0;
        PlantCode = null;
        WateringSystemOn = false;
        DaysWateringSystemOn = 0;
        WateringRawWaterAccumulator = 0f;
        ConditionScore = 50;
        PreviousDayConditionScore = -1;
        ConditionLabel = 1;  // Sana
        ForecastDirection = 1;  // Stable
        FertilizerLevel = 0;
        DaysFertilizerActive = 0;
        // BLK-03.01-T2: Inizializza campi punti crescita
        GrowthPointsWater = 0;
        GrowthPointsLight = 0;
        GrowthPointsFertilizer = 0;
        DaysConsecutiveOptimal = 0;
        DayOptimalParametersStarted = -1;
        HasPruningResaBonus = false;
        PlantLevel = 1;
        CompletedCycles = 0;
        MoldRiskLevel = 0;
        DaysWithoutPruning = 0;
        DaysOverwateringConsecutive = 0;
        DaysInExtremePh = 0;
        ExtremePhDeathCountdown = -1;
    }
    
    /// <summary>
    /// Incrementa il contatore di cicli completati
    /// </summary>
    public void IncrementCompletedCycle()
    {
        CompletedCycles++;
    }
    
    /// <summary>
    /// Verifica se può salire di livello
    /// </summary>
    public bool CanLevelUp(int cyclesRequired)
    {
        return CompletedCycles >= cyclesRequired;
    }
    
    /// <summary>
    /// BLK-02.07: Aggiorna stato LED persistente
    /// </summary>
    public void SetLedSystemState(LedSystemState newState)
    {
        LedSystemState oldState = LedSystemState;
        int oldBlueDays = DaysLedBlueConsecutive;
        int oldRedDays = DaysLedRedConsecutive;
        
        // DEBUG_SAFE_FIX: Non fare nulla se lo stato non è cambiato
        // Questo evita reset inutili dei contatori quando SetLedSystemState viene chiamato con lo stesso stato
        if (oldState == newState)
        {
            // Stato non cambiato: non fare nulla, mantieni i contatori
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_LED_STATE] {PotId}: SetLedSystemState chiamato con stesso stato ({oldState}), nessun reset");
            return;
        }
        
        LedSystemState = newState;
        
        // Reset contatori SOLO se cambiato tipo (Blue ↔ Red)
        // NON resettare quando si spegne (Off) per permettere decrescita graduale dello stress
        // DEBUG_SAFE_FIX: Reset solo se si cambia da un tipo LED all'altro (Blue ↔ Red)
        // NON resettare se si passa da Off a Blue/Red (i contatori partono da 0 comunque)
        if (newState == LedSystemState.Blue && oldState == LedSystemState.Red)
        {
            // Cambiato da Red a Blue: reset Red (cambio tipo)
            DaysLedRedConsecutive = 0;
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_LED_STATE] {PotId}: Cambio Red → Blue, reset Red counter (Blue mantiene {DaysLedBlueConsecutive})");
        }
        else if (newState == LedSystemState.Red && oldState == LedSystemState.Blue)
        {
            // Cambiato da Blue a Red: reset Blue (cambio tipo)
            DaysLedBlueConsecutive = 0;
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_LED_STATE] {PotId}: Cambio Blue → Red, reset Blue counter (Red mantiene {DaysLedRedConsecutive})");
        }
        else if (newState == LedSystemState.Off)
        {
            // Cambiato a Off: NON resettare i contatori!
            // I contatori verranno decrementati gradualmente a fine giornata in DayCycleController
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_LED_STATE] {PotId}: Cambio a Off, mantengo contatori (Blue: {DaysLedBlueConsecutive}, Red: {DaysLedRedConsecutive})");
        }
        else if ((newState == LedSystemState.Blue || newState == LedSystemState.Red) && oldState == LedSystemState.Off)
        {
            // Cambiato da Off a Blue/Red: NON resettare i contatori esistenti
            // I contatori verranno incrementati a fine giornata
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_LED_STATE] {PotId}: Cambio Off → {newState}, mantengo contatori (Blue: {DaysLedBlueConsecutive}, Red: {DaysLedRedConsecutive})");
        }
        // Se newState == Off: NON resettare i contatori!
        // I contatori verranno decrementati gradualmente a fine giornata in DayCycleController
        
        // #region agent log
        // DEBUG: Log cambio stato LED (per tracciare azzeramenti)
        try
        {
            string logPath = @"d:\Sporae_Build_Beta\.cursor\debug.log";
            var logEntry = new
            {
                id = $"log_{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{System.Guid.NewGuid().ToString().Substring(0, 8)}",
                timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                location = "PotStateModel:SetLedSystemState",
                message = $"LED State Changed - PotId={PotId}",
                data = new {
                    potId = PotId,
                    oldState = oldState.ToString(),
                    newState = newState.ToString(),
                    oldBlueDays = oldBlueDays,
                    newBlueDays = DaysLedBlueConsecutive,
                    oldRedDays = oldRedDays,
                    newRedDays = DaysLedRedConsecutive,
                    wasReset = (oldBlueDays > 0 && DaysLedBlueConsecutive == 0) || (oldRedDays > 0 && DaysLedRedConsecutive == 0)
                },
                sessionId = "debug-session",
                runId = "debug",
                hypothesisId = "G"
            };
            string jsonLine = JsonUtility.ToJson(logEntry) + System.Environment.NewLine;
            System.IO.File.AppendAllText(logPath, jsonLine);
        }
        catch { }
        // #endregion
    }
    
    /// <summary>
    /// BLK-02.07: Ottiene giorni consecutivi per stato LED corrente
    /// </summary>
    public int GetConsecutiveLedDays()
    {
        if (LedSystemState == LedSystemState.Blue)
            return DaysLedBlueConsecutive;
        if (LedSystemState == LedSystemState.Red)
            return DaysLedRedConsecutive;
        // LED spento: ritorna il massimo tra Blue e Red per mostrare stress residuo
        // Questo permette la decrescita graduale dello stress anche quando LED è spento
        return Mathf.Max(DaysLedBlueConsecutive, DaysLedRedConsecutive);
    }
    
    /// <summary>
    /// BLK-02.07: Incrementa contatore giorni consecutivi (chiamato a fine giornata)
    /// </summary>
    public void IncrementConsecutiveLedDays()
    {
        int oldBlueDays = DaysLedBlueConsecutive;
        int oldRedDays = DaysLedRedConsecutive;
        
        if (LedSystemState == LedSystemState.Blue)
        {
            DaysLedBlueConsecutive++;
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_LED_INCREMENT] {PotId}: Increment Blue counter: {oldBlueDays} → {DaysLedBlueConsecutive}");
        }
        else if (LedSystemState == LedSystemState.Red)
        {
            DaysLedRedConsecutive++;
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_LED_INCREMENT] {PotId}: Increment Red counter: {oldRedDays} → {DaysLedRedConsecutive}");
        }
        else
        {
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_LED_INCREMENT] {PotId}: LED è Off, nessun incremento (Blue: {DaysLedBlueConsecutive}, Red: {DaysLedRedConsecutive})");
        }
        // Off non incrementa
    }
    
    /// <summary>
    /// BLK-02.07: Calcola livello Burn Risk in base a giorni consecutivi (usando percentuali)
    /// </summary>
    /// <param name="maxDaysForFullStress">Massimo giorni per stress completo (100%)</param>
    /// <returns>0 = Nessun rischio, 1 = Medio, 2 = Alto, 3 = Critico</returns>
    public int GetBurnRiskLevel(int maxDaysForFullStress)
    {
        int consecutiveDays = GetConsecutiveLedDays();
        
        if (LedSystemState == LedSystemState.Off || consecutiveDays <= 1)
            return 0;  // Nessun rischio
        
        // Calcola percentuale stress
        float stressPercent = (float)consecutiveDays / maxDaysForFullStress * 100f;
        
        if (stressPercent < 40f)  // < 2 giorni
            return 0;
        if (stressPercent < 80f)  // 2-3 giorni (40-60%)
            return 1;  // Rischio medio
        if (stressPercent < 100f)  // 4 giorni (80%)
            return 2;  // Rischio alto
        return 3;  // 5+ giorni (100%+) - Rischio critico
    }
    
    /// <summary>
    /// BLK-02.07: Verifica se pianta è in zona rossa (100% stress = maxDaysForFullStress giorni consecutivi)
    /// </summary>
    public bool IsInRedZone(int maxDaysForFullStress)
    {
        return GetConsecutiveLedDays() >= maxDaysForFullStress && LedSystemState != LedSystemState.Off;
    }
    
    /// <summary>
    /// Ottiene il PlantData associato a questa pianta (se disponibile)
    /// </summary>
    public PlantData GetPlantData()
    {
        if (string.IsNullOrEmpty(PlantCode))
            return null;
        
        return PlantDatabase.Instance?.GetPlantDataByCode(PlantCode);
    }
    
    /// <summary>
    /// Restituisce una descrizione testuale dello stato
    /// </summary>
    public string GetStatusDescription()
    {
        if (!HasPlant)
        {
            return "Vaso vuoto";
        }
        
        string status = $"Pianta ({GetStageName(Stage)})";
        
        if (Hydration > 0)
        {
            status += $", Idratazione: {Hydration}";
        }
        
        if (LightExposure > 0)
        {
            status += $", Luce: {LightExposure}";
        }
        
        return status;
    }
    
    /// <summary>
    /// Restituisce una rappresentazione stringa del modello
    /// </summary>
    public override string ToString()
    {
        return $"[{PotId}] Plant:{HasPlant} Stage:{Stage}({GetStageName(Stage)}) H:{Hydration} L:{LightExposure} GP:{GrowthPoints} Day:{PlantedDay}";
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
