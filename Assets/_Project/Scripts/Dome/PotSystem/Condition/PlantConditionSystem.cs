using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sporae.Dome.PotSystem.Growth;
using Sporae.Dome.PotSystem.Mold;
using _Project; // Per PhSystem
using _Project.Sporae.Core;
using Sporae.DevTools; // Per DifficultyCalibrationConfig

namespace Sporae.Dome.PotSystem.Condition
{
    /// <summary>
    /// Sistema di calcolo della condizione della pianta (score 0-100)
    /// Calcola lo score basandosi su idratazione, luce, pH, watering, mold risk, burn stress
    /// </summary>
    public static class PlantConditionSystem
    {
        
        // Parametri ora configurabili via DifficultyCalibrationConfig
        
        /// <summary>
        /// Calcola lo score di condizione per una pianta (0-100)
        /// </summary>
        public static ConditionResult CalculateCondition(
            PotStateModel potState,
            PlantData plantData,
            PhSystem phSystem,
            PotSystemConfig potConfig,
            int currentDay,
            int previousDayScore = -1) // -1 se non disponibile (primo giorno)
        {
            if (potState == null || !potState.HasPlant)
            {
                // Vaso vuoto: nessuna condizione
                return new ConditionResult(0, PlantCondition.Critica, ForecastDirection.Stable, 0, 
                    new ConditionContributor[0]);
            }
            
            if (plantData == null)
            {
                // PlantData non disponibile: score neutro
                return new ConditionResult(DifficultyCalibrationConfig.BaseScore, PlantCondition.Sana, ForecastDirection.Stable, 0, 
                    new ConditionContributor[0]);
            }
            
            int score = DifficultyCalibrationConfig.BaseScore;
            List<ConditionContributor> contributors = new List<ConditionContributor>();
            
            // DEBUG: Log inizio calcolo (Ipotesi D: problema con stress percentage)
            int consecutiveDays = potState.GetConsecutiveLedDays();
            int maxDaysForFullStress = potConfig != null ? potConfig.MaxDaysForFullStress : 5;
            float stressPercentage = Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f;
            
            // Log critico: Inizio calcolo condizione
            SporiumLogger.LogDebugWithLocation(
                LogCategory.Pot,
                "PlantConditionSystem:CalculateCondition:START",
                $"START Calcolo - PotId={potState.PotId}, Day={currentDay}",
                new {
                    potId = potState.PotId,
                    day = currentDay,
                    baseScore = DifficultyCalibrationConfig.BaseScore,
                    consecutiveLedDays = consecutiveDays,
                    maxDaysForFullStress = maxDaysForFullStress,
                    stressPercentage = stressPercentage,
                    previousDayScore = previousDayScore
                },
                "D",
                "debug"
            );
            
            // === CONTRIBUTI POSITIVI ===
            
            // 1. Idratazione in range ottimale per stadio
            int maxHydration = potConfig != null ? potConfig.MaxHydration : 10;
            int hydrationPercent = maxHydration > 0 ? Mathf.RoundToInt((float)potState.Hydration / maxHydration * 100f) : 0;
            
            // Verifica range ottimale per stadio (50-75% per la maggior parte degli stadi)
            bool isHydrationOptimal = IsHydrationInOptimalRange(potState, plantData, maxHydration);
            
            // DEBUG: Verifica anche se l'idratazione è nel range accettabile (non solo ottimale)
            bool isHydrationInAcceptableRange = false;
            if (plantData != null)
            {
                PlantStage currentStage = (PlantStage)potState.Stage;
                var stageReq = plantData.GetStageRequirements(currentStage);
                if (stageReq != null)
                {
                    isHydrationInAcceptableRange = stageReq.IsHydrationInRange(hydrationPercent);
                }
            }
            
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_HYDRATION] {potState.PotId} Day={currentDay}: Hydration={potState.Hydration}/{maxHydration} ({hydrationPercent}%), IsOptimal={isHydrationOptimal}, IsInAcceptableRange={isHydrationInAcceptableRange}, WateringSystemOn={potState.WateringSystemOn}");
            int scoreBeforeHydration = score;
            if (isHydrationOptimal)
            {
                score += DifficultyCalibrationConfig.BonusHydrationOptimal;
                contributors.Add(new ConditionContributor("Idratazione ottimale", DifficultyCalibrationConfig.BonusHydrationOptimal, true));
                SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_STEP] {potState.PotId} Day={currentDay}: BONUS Idratazione ottimale = +{DifficultyCalibrationConfig.BonusHydrationOptimal}, Score: {scoreBeforeHydration} → {score}");
            }
            else
            {
                SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_STEP] {potState.PotId} Day={currentDay}: NO BONUS Idratazione (IsOptimal={isHydrationOptimal}, Hydration%={hydrationPercent}), Score: {score}");
            }
            
            // 2. Luce corretta per stadio (LED corretto)
            // BUG FIX: Il bonus viene applicato quando:
            // - LED è acceso E corretto per lo stadio E lo stress è nel range (tra 0% e 100%)
            // - Oppure quando lo stress è fuori range (0% o 100%) ma il LED è corretto
            // Il malus "Luce assente" viene applicato solo quando lo stress è 0% o 100%
            bool hasCorrectLight = IsLightCorrectForStage(potState, plantData, currentDay);
            bool isLedOn = potState.LedSystemState != LedSystemState.Off;
            
            // Applica bonus quando:
            // - LED è acceso E corretto E lo stress è nel range (tra 0% e 100%) → BONUS per avere la luce giusta nel range giusto
            // - Oppure quando lo stress è fuori range (0% o 100%) ma il LED è corretto → BONUS per avere la luce giusta anche fuori range
            // BUG FIX: Quando lo stress è nel range (tra 0% e 100%), il bonus viene applicato anche se il LED è spento
            // Questo evita che il bonus venga rimosso immediatamente quando si spegne il LED con stress nel range
            // Il bonus viene rimosso solo a fine giornata quando lo stress viene ricalcolato
            int scoreBeforeLight = score;
            if (hasCorrectLight && (isLedOn && (stressPercentage > 0f && stressPercentage < 100f) || (stressPercentage == 0f || stressPercentage >= 100f)))
            {
                score += DifficultyCalibrationConfig.BonusLightCorrect;
                contributors.Add(new ConditionContributor("Luce corretta (LED)", DifficultyCalibrationConfig.BonusLightCorrect, true));
                SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_STEP] {potState.PotId} Day={currentDay}: BONUS Luce corretta (LED) = +{DifficultyCalibrationConfig.BonusLightCorrect}, Score: {scoreBeforeLight} → {score}");
            }
            else if (!isLedOn && (stressPercentage > 0f && stressPercentage < 100f))
            {
                // DEBUG_SAFE_FIX: Quando lo stress è nel range ottimale e il LED è spento, dai comunque il bonus
                // Questo evita drop insensati di condizione quando il LED è spento ma lo stress è nel range
                // Il bonus viene dato perché lo stress è nel range ottimale, indipendentemente dallo stato del LED
                score += DifficultyCalibrationConfig.BonusLightCorrect;
                contributors.Add(new ConditionContributor("Luce corretta (stress nel range, LED OFF)", DifficultyCalibrationConfig.BonusLightCorrect, true));
                
                SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_STEP] {potState.PotId} Day={currentDay}: BONUS Luce corretta (LED OFF, stress in range) = +{DifficultyCalibrationConfig.BonusLightCorrect}, Score: {scoreBeforeLight} → {score}");
            }
            else
            {
                SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_STEP] {potState.PotId} Day={currentDay}: NO BONUS Luce (hasCorrectLight={hasCorrectLight}, isLedOn={isLedOn}, stress%={stressPercentage:F1}), Score: {score}");
            }
            
            // 3. Idratazione ottimale (bonus pieno se idratazione è in range ottimale, indipendentemente da WateringSystemOn)
            // DEBUG_SAFE_FIX: Il bonus viene dato quando l'idratazione è ottimale, indipendentemente dallo stato del sistema
            // I malus vengono applicati solo quando l'idratazione è fuori range (sotto o sopra)
            int scoreBeforeWateringBonus = score;
            if (isHydrationOptimal)
            {
                // BUG FIX: Il bonus deve essere aggiunto allo score PRIMA di controllare WateringSystemOn
                score += DifficultyCalibrationConfig.BonusWateringOn;
                if (potState.WateringSystemOn)
                {
                    contributors.Add(new ConditionContributor("Idratazione ottimale (Watering ON)", DifficultyCalibrationConfig.BonusWateringOn, true));
                    SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_STEP] {potState.PotId} Day={currentDay}: BONUS Idratazione ottimale (Watering ON) = +{DifficultyCalibrationConfig.BonusWateringOn}, Score: {scoreBeforeWateringBonus} → {score}");
                }
                else
                {
                    contributors.Add(new ConditionContributor("Idratazione ottimale (Watering OFF)", DifficultyCalibrationConfig.BonusWateringOn, true));
                    SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_STEP] {potState.PotId} Day={currentDay}: BONUS Idratazione ottimale (Watering OFF) = +{DifficultyCalibrationConfig.BonusWateringOn}, Score: {scoreBeforeWateringBonus} → {score}");
                }
            }
            else
            {
                SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_STEP] {potState.PotId} Day={currentDay}: NO BONUS Idratazione ottimale (IsOptimal={isHydrationOptimal}, Hydration%={hydrationPercent}, WateringSystemOn={potState.WateringSystemOn}), Score: {score}");
            }
            
            // 4. pH Dome in banda affinità pianta
            if (phSystem != null)
            {
                float currentPh = phSystem.CurrentPh;
                if (plantData.IsPhInOptimalRange(currentPh))
                {
                    score += DifficultyCalibrationConfig.BonusPhOptimal;
                    contributors.Add(new ConditionContributor("pH Dome in banda affinità", DifficultyCalibrationConfig.BonusPhOptimal, true));
                    
                    // Bonus graduale se pH molto vicino al centro del range
                    float rangeCenter = (plantData.OptimalPhMin + plantData.OptimalPhMax) / 2f;
                    float distanceFromCenter = Mathf.Abs(currentPh - rangeCenter);
                    float rangeSize = plantData.OptimalPhMax - plantData.OptimalPhMin;
                    float normalizedDistance = rangeSize > 0 ? (distanceFromCenter / (rangeSize / 2f)) : 0f;
                    
                    // Se molto vicino al centro (entro 25% del range), bonus extra
                    if (normalizedDistance < 0.25f)
                    {
                        score += DifficultyCalibrationConfig.BonusPhOptimalGradual;
                        contributors.Add(new ConditionContributor("pH molto vicino al centro range", DifficultyCalibrationConfig.BonusPhOptimalGradual, true));
                    }
                }
            }
            
            // 5. Nessun Mold Risk attivo
            MoldConfig moldConfig = Resources.Load<MoldConfig>("Configs/MoldConfig");
            int moldRiskLevel = 0;
            if (moldConfig != null)
            {
                moldRiskLevel = MoldSystem.GetMoldRiskLevel(potState, phSystem, plantData, moldConfig);
            }
            bool hasNoMoldRisk = (moldRiskLevel == 0);
            if (hasNoMoldRisk)
            {
                score += DifficultyCalibrationConfig.BonusNoMold;
                contributors.Add(new ConditionContributor("Nessun Mold Risk", DifficultyCalibrationConfig.BonusNoMold, true));
            }
            
            // === CONTRIBUTI NEGATIVI ===
            
            // Nota: stressPercentage è già calcolato all'inizio del metodo
            
            // 1. Idratazione fuori range (Dry/Wet)
            int scoreBeforeHydrationMalus = score;
            if (!isHydrationOptimal)
            {
                if (hydrationPercent < DifficultyCalibrationConfig.HydrationDryThreshold || hydrationPercent > DifficultyCalibrationConfig.HydrationWetThreshold)
                {
                    score -= DifficultyCalibrationConfig.MalusHydrationOutOfRange;
                    contributors.Add(new ConditionContributor("Idratazione fuori range", -DifficultyCalibrationConfig.MalusHydrationOutOfRange, false));
                    SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_STEP] {potState.PotId} Day={currentDay}: MALUS Idratazione fuori range = -{DifficultyCalibrationConfig.MalusHydrationOutOfRange}, Score: {scoreBeforeHydrationMalus} → {score}");
                }
                else
                {
                    SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_STEP] {potState.PotId} Day={currentDay}: NO MALUS Idratazione (IsOptimal={isHydrationOptimal}, Hydration%={hydrationPercent}, DryThreshold={DifficultyCalibrationConfig.HydrationDryThreshold}, WetThreshold={DifficultyCalibrationConfig.HydrationWetThreshold}), Score: {score}");
                }
            }
            
            // 2. Luce assente o spettro sbagliato
            // BUG FIX: Il malus viene applicato solo quando lo stress è fuori range (0% o 100%)
            // Quando lo stress è nel range (tra 0% e 100%), non viene applicato alcun malus per la luce
            
            // Applica malus solo quando stress è esattamente 0% (nessuna luce) o 100% (burned)
            // DEBUG_SAFE_FIX: Non applicare malus quando stress è 0% se i parametri sono comunque in range
            // Il malus viene applicato solo se lo stress è 0% E il LED è richiesto per lo stadio
            // Quando lo stress è nel range (tra 0% e 100%), non applicare malus per luce assente/spettro sbagliato
            int scoreBeforeLightMalus = score;
            if (!hasCorrectLight && stressPercentage >= 100f)
            {
                // Stress 100% (burned) → sempre malus
                score -= DifficultyCalibrationConfig.MalusLightWrongOrAbsent;
                contributors.Add(new ConditionContributor("Luce assente o spettro sbagliato (burned)", -DifficultyCalibrationConfig.MalusLightWrongOrAbsent, false));
                SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_STEP] {potState.PotId} Day={currentDay}: MALUS Luce assente (burned) = -{DifficultyCalibrationConfig.MalusLightWrongOrAbsent}, Score: {scoreBeforeLightMalus} → {score}");
            }
            else if (!hasCorrectLight && stressPercentage == 0f)
            {
                // Stress 0% → malus solo se LED è richiesto per lo stadio
                PlantStage currentStage = (PlantStage)potState.Stage;
                StageRequirements stageReq = plantData?.GetStageRequirements(currentStage);
                bool ledRequired = stageReq != null && stageReq.GetRequiredLed().HasValue;
                
                if (ledRequired)
                {
                    score -= DifficultyCalibrationConfig.MalusLightWrongOrAbsent;
                    contributors.Add(new ConditionContributor("Luce assente (LED richiesto)", -DifficultyCalibrationConfig.MalusLightWrongOrAbsent, false));
                    SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_STEP] {potState.PotId} Day={currentDay}: MALUS Luce assente (LED richiesto) = -{DifficultyCalibrationConfig.MalusLightWrongOrAbsent}, Score: {scoreBeforeLightMalus} → {score}");
                }
                else
                {
                    SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_STEP] {potState.PotId} Day={currentDay}: NO MALUS Luce assente (LED non richiesto, stress=0%), Score: {score}");
                }
                // Se LED non è richiesto, non applicare malus anche se stress è 0%
            }
            else
            {
                SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_STEP] {potState.PotId} Day={currentDay}: NO MALUS Luce (hasCorrectLight={hasCorrectLight}, stress%={stressPercentage:F1}), Score: {score}");
            }
            
            // 2b. BLK-02.08: LED incompatibile con famiglia (-5 per ogni giorno che è acceso)
            if (potState.LedSystemState != LedSystemState.Off && plantData != null)
            {
                LedCompatibility compatible = LedCompatibilityHelper.GetCompatibleLedTypes(plantData.Family);
                bool isLedIncompatible = !LedCompatibilityHelper.IsLedCompatible(potState.LedSystemState, compatible);
                
                if (isLedIncompatible)
                {
                    // -5 per ogni giorno che LED sbagliato è acceso
                    // Nota: consecutiveDays è già calcolato all'inizio del metodo
                    int malusAmount = DifficultyCalibrationConfig.MalusLedIncompatiblePerDay * consecutiveDays;
                    score += malusAmount;
                    string compatibleDisplay = LedCompatibilityHelper.GetCompatibleLedDisplay(compatible);
                    contributors.Add(new ConditionContributor($"LED incompatibile con famiglia ({compatibleDisplay} richiesto)", malusAmount, false));
                }
            }
            
            // 3. pH opposto alla pianta (Pure in acido, Evil in basico)
            if (phSystem != null)
            {
                float currentPh = phSystem.CurrentPh;
                PhSystem.PhBand phBand = phSystem.EvaluateState();
                
                
                // Pure preferisce basico, Evil preferisce acido
                if (plantData.Family == PlantFamily.Pure && (phBand == PhSystem.PhBand.UltraAcid || phBand == PhSystem.PhBand.StableAcid))
                {
                    score -= DifficultyCalibrationConfig.MalusPhOpposite;
                    contributors.Add(new ConditionContributor("pH opposto (Pure in acido)", -DifficultyCalibrationConfig.MalusPhOpposite, false));
                }
                else if (plantData.Family == PlantFamily.Evil && (phBand == PhSystem.PhBand.UltraBasic || phBand == PhSystem.PhBand.StableBasic))
                {
                    score -= DifficultyCalibrationConfig.MalusPhOpposite;
                    contributors.Add(new ConditionContributor("pH opposto (Evil in basico)", -DifficultyCalibrationConfig.MalusPhOpposite, false));
                }
                
                // 4. pH in banda Ultra (≤-80 o ≥+80) - malus estremo
                if (phBand == PhSystem.PhBand.UltraAcid || phBand == PhSystem.PhBand.UltraBasic)
                {
                    score -= DifficultyCalibrationConfig.MalusPhExtreme;
                    contributors.Add(new ConditionContributor("pH in banda Ultra estremo", -DifficultyCalibrationConfig.MalusPhExtreme, false));
                }
                else if (phBand == PhSystem.PhBand.StableAcid || phBand == PhSystem.PhBand.StableBasic)
                {
                    // pH Ultra ma non estremo: usa malus standard
                    score -= DifficultyCalibrationConfig.MalusPhUltra;
                    contributors.Add(new ConditionContributor("pH in banda Ultra", -DifficultyCalibrationConfig.MalusPhUltra, false));
                }
                
                // 5. pH fuori range ottimale (malus graduale basato su distanza)
                if (!plantData.IsPhInOptimalRange(currentPh))
                {
                    float phDistance = plantData.GetPhDistanceFromOptimal(currentPh);
                    int malusPhOutOfRange = Mathf.RoundToInt(Mathf.Lerp(
                        DifficultyCalibrationConfig.MalusPhOutOfRangeMin,
                        DifficultyCalibrationConfig.MalusPhOutOfRangeMax,
                        phDistance
                    ));
                    score -= malusPhOutOfRange;
                    contributors.Add(new ConditionContributor($"pH fuori range (distanza: {phDistance:F2})", -malusPhOutOfRange, false));
                }
            }
            
            // 5. Overwatering attivo (forza stato Stressata)
            int scoreBeforeOverwatering = score;
            bool isOverwatering = IsOverwatering(potState, maxHydration);
            if (isOverwatering)
            {
                score -= DifficultyCalibrationConfig.MalusOverwatering;
                contributors.Add(new ConditionContributor("Overwatering attivo", -DifficultyCalibrationConfig.MalusOverwatering, false));
                SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_STEP] {potState.PotId} Day={currentDay}: MALUS Overwatering = -{DifficultyCalibrationConfig.MalusOverwatering}, Score: {scoreBeforeOverwatering} → {score}");
            }
            else
            {
                SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_STEP] {potState.PotId} Day={currentDay}: NO MALUS Overwatering (IsOverwatering={isOverwatering}, Hydration%={hydrationPercent}), Score: {score}");
            }
            
            // 6. Mold Infestation (BUG FIX 2: solo se IsInfested = true, non basato su MoldRiskLevel)
            // NOTA: MalusMoldSevere rimosso dal calcolo perché già blocca l'avanzamento (MoldRiskLevel >= 2)
            if (potState.IsInfested)
            {
                // Applica malus solo per infestazione lieve (Mild)
                if (potState.MoldRiskLevel == 1) // Mild
                {
                    score -= DifficultyCalibrationConfig.MalusMoldMild;
                    contributors.Add(new ConditionContributor("Infestazione muffe lieve", -DifficultyCalibrationConfig.MalusMoldMild, false));
                }
                // Severe/Critical non applica malus qui perché già blocca l'avanzamento
            }
            
            // 7. Burn Stress attivo (solo quando stress è 0% o 100%)
            // DEBUG_SAFE_FIX: Il malus viene applicato solo quando lo stress è fuori range (0% = nessuna luce, 100% = burned)
            // Quando lo stress è nel range (tra 0% e 100%), non viene applicato alcun malus
            // Nota: consecutiveDays, maxDaysForFullStress e stressPercentage sono già calcolati all'inizio della sezione CONTRIBUTI NEGATIVI
            
            // Applica malus solo quando stress è esattamente 0% (nessuna luce) o 100% (burned)
            // DEBUG_SAFE_FIX: Non applicare malus quando stress è 0% se i parametri sono comunque in range
            // Il malus viene applicato solo se lo stress è 0% E il LED è richiesto per lo stadio
            if (stressPercentage >= 100f)
            {
                // Stress massimo (burned) → sempre malus
                score -= DifficultyCalibrationConfig.MalusBurnStress;
                contributors.Add(new ConditionContributor("Burn Stress attivo (100%)", -DifficultyCalibrationConfig.MalusBurnStress, false));
            }
            else if (stressPercentage == 0f)
            {
                // Stress 0% → malus solo se LED è richiesto per lo stadio
                PlantStage currentStage = (PlantStage)potState.Stage;
                StageRequirements stageReq = plantData?.GetStageRequirements(currentStage);
                bool ledRequired = stageReq != null && stageReq.GetRequiredLed().HasValue;
                
                if (ledRequired)
                {
                    score -= DifficultyCalibrationConfig.MalusBurnStress;
                    contributors.Add(new ConditionContributor("Nessuna luce (LED richiesto)", -DifficultyCalibrationConfig.MalusBurnStress, false));
                }
                // Se LED non è richiesto, non applicare malus anche se stress è 0%
            }
            
            // 8. Negligenza prolungata (nessuna cura per più giorni consecutivi)
            // BUGFIX (POT-CONDITION-REGRESSION): senza questo, una pianta può restare "Sana" per sempre anche con Hydration=0 e LED OFF.
            // Usiamo DaysNeglectedStreak che viene aggiornato ogni EndDay dal DayCycleController.
            int neglectThreshold = Mathf.Max(0, DifficultyCalibrationConfig.ConditionNeglectThresholdDays);
            int malusPerDay = Mathf.Max(0, DifficultyCalibrationConfig.MalusNeglectPerDay);
            if (malusPerDay > 0 && potState.DaysNeglectedStreak >= neglectThreshold && potState.DaysNeglectedStreak > 0)
            {
                // Applica malus crescente: al superamento soglia, 1x; poi 2x; ecc.
                int over = (potState.DaysNeglectedStreak - neglectThreshold) + 1;
                over = Mathf.Max(1, over);
                int malus = malusPerDay * over;
                
                score -= malus;
                contributors.Add(new ConditionContributor($"Negligenza ({potState.DaysNeglectedStreak} giorni)", -malus, false));
            }
            
            // Clamp score 0-100
            int scoreBeforeClamp = score;
            score = Mathf.Clamp(score, 0, 100);
            
            // DEBUG: Log finale con tutti i contributi (Ipotesi B: malus nascosto)
            // Usa SporiumLogger per assicurarsi che i log vengano scritti
            if (potState != null && potState.PotId != null)
            {
                SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_SUMMARY] {potState.PotId} Day={currentDay}: Base={DifficultyCalibrationConfig.BaseScore}, BeforeClamp={scoreBeforeClamp}, Final={score}, ThresholdSana={DifficultyCalibrationConfig.ConditionThresholdSana}, ThresholdAppassita={DifficultyCalibrationConfig.ConditionThresholdAppassita}, WateringSystemOn={potState.WateringSystemOn}, Hydration%={hydrationPercent}, IsOptimal={isHydrationOptimal}, LED={potState.LedSystemState}, Stress%={stressPercentage:F1}, Contributors: {contributors.Count} (Pos: {contributors.Count(c => c.IsPositive)}, Neg: {contributors.Count(c => !c.IsPositive)})");
                SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_CONTRIBUTORS] {potState.PotId} Day={currentDay}: TUTTI I CONTRIBUTI:");
                foreach (var c in contributors)
                {
                    SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_CONDITION_CONTRIBUTORS] {potState.PotId} Day={currentDay}:   {c.Source} = {(c.IsPositive ? "+" : "-")}{Mathf.Abs(c.Value)}");
                }
            }
            
            // Log critico: Risultato finale calcolo condizione
            SporiumLogger.LogDebugWithLocation(
                LogCategory.Pot,
                "PlantConditionSystem:CalculateCondition:FINAL",
                $"FINAL Score - PotId={potState.PotId}",
                new {
                    potId = potState.PotId,
                    baseScore = DifficultyCalibrationConfig.BaseScore,
                    scoreBeforeClamp = scoreBeforeClamp,
                    finalScore = score,
                    contributors = contributors.Select(c => new { source = c.Source, value = c.Value, isPositive = c.IsPositive }).ToArray(),
                    positiveCount = contributors.Count(c => c.IsPositive),
                    negativeCount = contributors.Count(c => !c.IsPositive),
                    thresholdSana = DifficultyCalibrationConfig.ConditionThresholdSana,
                    thresholdAppassita = DifficultyCalibrationConfig.ConditionThresholdAppassita
                },
                "B",
                "debug"
            );
            
            // DEBUG: Log dettagliato del calcolo
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (potState != null && potState.PotId != null)
            {
                SporiumLogger.LogDebug(LogCategory.Pot, $"DEBUG Calcolo {potState.PotId}: Base={DifficultyCalibrationConfig.BaseScore}, Score finale={score}, Contributi: {contributors.Count} (Pos: {System.Array.FindAll(contributors.ToArray(), c => c.IsPositive).Length}, Neg: {System.Array.FindAll(contributors.ToArray(), c => !c.IsPositive).Length})");
                foreach (var c in contributors)
                {
                    SporiumLogger.LogDebug(LogCategory.Pot, $"  - {c.Source}: {(c.IsPositive ? "+" : "")}{c.Value}");
                }
            }
            #endif
            
            // Mappa score a condizione
            PlantCondition condition = MapScoreToCondition(score, isOverwatering);
            
            // Calcola forecast (tendenza rispetto al giorno precedente)
            int scoreDelta = previousDayScore >= 0 ? score - previousDayScore : 0;
            ForecastDirection forecast = CalculateForecast(scoreDelta);
            
            return new ConditionResult(score, condition, forecast, scoreDelta, contributors.ToArray());
        }
        
        /// <summary>
        /// Verifica se l'idratazione è in range ottimale per lo stadio corrente
        /// Usa i requisiti specifici per stadio dalla PlantData invece di un range hardcoded
        /// </summary>
        private static bool IsHydrationInOptimalRange(PotStateModel potState, PlantData plantData, int maxHydration)
        {
            if (plantData == null || potState == null)
                return false;
            
            int hydrationPercent = maxHydration > 0 ? Mathf.RoundToInt((float)potState.Hydration / maxHydration * 100f) : 0;
            
            // Ottieni i requisiti per lo stadio corrente
            PlantStage currentStage = (PlantStage)potState.Stage;
            var stageReq = plantData.GetStageRequirements(currentStage);
            
            if (stageReq != null)
            {
                // DEBUG_SAFE_FIX: Range ottimale = range accettabile completo (hydrationMin - hydrationMax)
                // Se l'idratazione è nel range accettabile, è considerata ottimale
                // Questo evita drop insensati quando i parametri sono in range ma non esattamente al mediano
                return hydrationPercent >= stageReq.hydrationMin && hydrationPercent <= stageReq.hydrationMax;
            }
            
            // Fallback: se non ci sono requisiti specifici, usa range generico 50-75%
            return hydrationPercent >= 50 && hydrationPercent <= 75;
        }
        
        /// <summary>
        /// Verifica se la luce è corretta per lo stadio corrente
        /// BLK-02.07: Usa LedSystemState invece di LastLitDay per verificare stato corrente
        /// </summary>
        private static bool IsLightCorrectForStage(PotStateModel potState, PlantData plantData, int currentDay)
        {
            if (plantData == null || potState == null)
                return false;
            
            // Verifica se il LED è corretto per lo stadio
            PlantStage currentStage = (PlantStage)potState.Stage;
            StageRequirements stageReq = plantData.GetStageRequirements(currentStage);
            
            if (stageReq != null)
            {
                LedType? requiredLed = stageReq.GetRequiredLed();
                
                // Se non è richiesto LED per questo stadio, considera sempre corretto (nessun malus)
                if (!requiredLed.HasValue)
                    return true; // Nessun LED richiesto = sempre OK, nessun malus
                
                // Verifica se il LED corrente corrisponde al requisito
                // Usa LedSystemState per verificare lo stato corrente (non LastLitDay che può essere obsoleto)
                if (potState.LedSystemState != LedSystemState.Off)
                {
                    LedType currentLedType = potState.LedSystemState == LedSystemState.Blue ? LedType.Blue : LedType.Red;
                    return currentLedType == requiredLed.Value;
                }
                
                // LED è spento ma è richiesto → luce non corretta
                return false;
            }
            
            // Se non ci sono requisiti LED specifici, considera sempre corretto (nessun malus)
            return true;
        }
        
        /// <summary>
        /// Verifica se c'è overwatering (idratazione >= soglia configurabile)
        /// </summary>
        public static bool IsOverwatering(PotStateModel potState, int maxHydration)
        {
            if (potState == null || maxHydration <= 0)
                return false;
            
            int hydrationPercent = Mathf.RoundToInt((float)potState.Hydration / maxHydration * 100f);
            return hydrationPercent >= DifficultyCalibrationConfig.OverwateringThresholdPercent;
        }
        
        /// <summary>
        /// Mappa score (0-100) a condizione
        /// NOTA: Stressata rimosso dalla logica, mantenuto solo l'enum per retrocompatibilità
        /// </summary>
        private static PlantCondition MapScoreToCondition(int score, bool isOverwatering)
        {
            // Overwatering forza stato Sana se score >= 40 (nuovo threshold Sana)
            // Con i nuovi threshold, overwatering con score >= 40 risulterà in "Sana"
            if (isOverwatering && score >= DifficultyCalibrationConfig.ConditionThresholdSana)
            {
                return PlantCondition.Sana;  // Verrà mostrato come "Sana (Overwatering)" in GetConditionName
            }
            
            // Nuova logica senza Stressata:
            // Score >= 80 → Rigogliosa
            // Score >= 40 → Sana
            // Score >= 20 → Appassita
            // Score < 20 → Critica
            if (score >= DifficultyCalibrationConfig.ConditionThresholdRigogliosa)
                return PlantCondition.Rigogliosa;
            if (score >= DifficultyCalibrationConfig.ConditionThresholdSana)
                return PlantCondition.Sana;
            if (score >= DifficultyCalibrationConfig.ConditionThresholdAppassita)
                return PlantCondition.Appassita;
            return PlantCondition.Critica;
        }
        
        /// <summary>
        /// Calcola forecast (tendenza) basandosi su delta score
        /// </summary>
        private static ForecastDirection CalculateForecast(int scoreDelta)
        {
            if (scoreDelta > DifficultyCalibrationConfig.ForecastDeltaUp)
                return ForecastDirection.Up;
            if (scoreDelta < DifficultyCalibrationConfig.ForecastDeltaDown)
                return ForecastDirection.Down;
            return ForecastDirection.Stable;
        }
        
        /// <summary>
        /// Ottiene il nome della condizione in italiano
        /// NOTA: Stressata rimosso dalla logica, mantenuto solo l'enum per retrocompatibilità
        /// </summary>
        public static string GetConditionName(PlantCondition condition, bool isOverwatering = false)
        {
            if (condition == PlantCondition.Morta)
            {
                return "Morta";
            }
            
            // Overwatering ora forza "Sana" (con i nuovi threshold, score >= 40 → Sana)
            if (isOverwatering && condition == PlantCondition.Sana)
            {
                return "Sana (Overwatering)";
            }
            
            // Gestione retrocompatibilità: se per qualche motivo arriva Stressata (dati salvati vecchi), mostra "Sana"
            // Questo può accadere solo con dati salvati vecchi, il sistema non genera più Stressata
            if (condition == PlantCondition.Stressata)
            {
                return "Sana";  // Retrocompatibilità: Stressata → Sana
            }
            
            return condition switch
            {
                PlantCondition.Rigogliosa => "Rigogliosa",
                PlantCondition.Sana => "Sana",
                PlantCondition.Appassita => "Appassita",
                PlantCondition.Critica => "Critica",
                PlantCondition.Morta => "Morta",
                _ => "Sconosciuta"
            };
        }
        
        /// <summary>
        /// Ottiene il simbolo del forecast
        /// </summary>
        public static string GetForecastSymbol(ForecastDirection forecast)
        {
            return forecast switch
            {
                ForecastDirection.Up => "↑",
                ForecastDirection.Stable => "→",
                ForecastDirection.Down => "↓",
                _ => "?"
            };
        }
    }
}

