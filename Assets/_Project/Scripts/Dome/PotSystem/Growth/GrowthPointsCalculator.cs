using System;
using UnityEngine;
using Sporae.Dome.PotSystem.Growth;
using Sporae.DevTools;

namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// BLK-03.01-T2: Calcolatore punti crescita basato su valori nel range ideale
    /// </summary>
    public static class GrowthPointsCalculator
    {
        
        /// <summary>
        /// Calcola e assegna punti giornalieri basati su valori nel range ideale
        /// </summary>
        public static GrowthPointsResult CalculateDailyPoints(
            PotStateModel pot,
            PlantData plantData,
            PotSystemConfig potConfig)
        {
            var result = new GrowthPointsResult();
            
            if (pot == null || plantData == null || !pot.HasPlant)
            {
                return result;
            }
            
            // Ottieni requisiti per lo stadio corrente (specie seme vs genitore A/B se profilo Lab — Task 6)
            PlantStage currentStage = (PlantStage)pot.Stage;
            PlantData careData = LabHybridGameplayModifiers.ResolvePlantDataForCareRequirements(pot, plantData) ?? plantData;
            StageRequirements stageReq = careData != null
                ? careData.GetStageRequirements(currentStage)
                : null;
            
            if (stageReq == null)
            {
                // Se non ci sono requisiti, nessun punto
                return result;
            }
            
            // 1. Verifica water nel range ideale (hydrationPercent nel range)
            int maxHydration = potConfig != null ? potConfig.MaxHydration : 10;
            int hydrationPercent = maxHydration > 0 ? 
                Mathf.RoundToInt((float)pot.Hydration / maxHydration * 100f) : 0;
            
            bool hydrationInRange = stageReq.IsHydrationInRange(hydrationPercent);
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_POINTS] {pot.PotId} Water: {pot.Hydration}/{maxHydration} ({hydrationPercent}%), Range={stageReq.hydrationMin}-{stageReq.hydrationMax}, InRange={hydrationInRange}, WateringSystemOn={pot.WateringSystemOn}");
            
            if (hydrationInRange)
            {
                result.WaterPoint = 1;
                pot.GrowthPointsWater += 1;
            }
            
            // 2. Verifica light nel range ideale (LED corretto + intensità nel range)
            bool lightInRange = IsLightInOptimalRange(pot, plantData, stageReq, potConfig);
            
            bool isLedRequirementMet = stageReq.IsLedRequirementMet(pot.LedSystemState);
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_POINTS] {pot.PotId} Light: LED={pot.LedSystemState}, Required={stageReq.GetRequiredLed()?.ToString() ?? "None"}, Met={isLedRequirementMet}, InRange={lightInRange}");
            
            if (lightInRange)
            {
                result.LightPoint = 1;
                pot.GrowthPointsLight += 1;
            }
            
            // 3. Verifica fertilizer nel range ideale (FertilizerLevel 0-100% nel range)
            bool fertilizerInRange = stageReq.IsFertilizerInRange(pot.FertilizerLevel);
            
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_POINTS] {pot.PotId} Fertilizer: {pot.FertilizerLevel}%, Range={stageReq.fertilizerMin}-{stageReq.fertilizerMax}, InRange={fertilizerInRange}");
            
            if (fertilizerInRange)
            {
                result.FertilizerPoint = 1;
                pot.GrowthPointsFertilizer += 1;
            }
            
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_POINTS] {pot.PotId} RESULT: W={result.WaterPoint}, L={result.LightPoint}, F={result.FertilizerPoint}, Total={result.TotalPoints}, Accumulated: W={pot.GrowthPointsWater}, L={pot.GrowthPointsLight}, F={pot.GrowthPointsFertilizer}");
            
            // Log critico: Risultato finale calcolo punti
            SporiumLogger.LogDebugWithLocation(
                LogCategory.Pot,
                "GrowthPointsCalculator:CalculateDailyPoints:RESULT",
                $"Risultato Calcolo Punti - PotId={pot.PotId}",
                new {
                    potId = pot.PotId,
                    stage = currentStage.ToString(),
                    waterPoint = result.WaterPoint,
                    lightPoint = result.LightPoint,
                    fertilizerPoint = result.FertilizerPoint,
                    totalPoints = result.TotalPoints,
                    growthPointsWater = pot.GrowthPointsWater,
                    growthPointsLight = pot.GrowthPointsLight,
                    growthPointsFertilizer = pot.GrowthPointsFertilizer
                },
                "F",
                "debug"
            );
            
            return result;
        }
        
        private static bool IsLightInOptimalRange(
            PotStateModel pot, 
            PlantData plantData, 
            StageRequirements stageReq,
            PotSystemConfig potConfig = null)
        {
            // DEBUG_SAFE_FIX: Se LED è OFF, verifica se lo stress percentage è comunque nel range ottimale
            // Questo permette di considerare OK anche quando LED è spento ma i parametri sono in range
            if (pot.LedSystemState == LedSystemState.Off)
            {
                // Quando LED è OFF: parametro OK se stress già in range 20%-80% (sotto 20% non beneficia, sopra 80% burn risk)
                int consecutiveDays = pot.GetConsecutiveLedDays();
                int maxDaysForFullStress = potConfig != null ? potConfig.MaxDaysForFullStress : 5;
                float stressPercentage = Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f;
                const float LightStressOkMin = 20f;
                const float LightStressOkMax = 80f;
                bool stressInOptimalRange = stressPercentage >= LightStressOkMin && stressPercentage <= LightStressOkMax;
                SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_POINTS] {pot.PotId} Light OFF: ConsecutiveDays={consecutiveDays}, Stress%={stressPercentage:F1}, InOptimalRange={stressInOptimalRange}");
                return stressInOptimalRange;
            }
            
            // Verifica LED richiesto quando LED è acceso (ibridi LED_ADAPT: tolleranza Task 6)
            if (!LabHybridGameplayModifiers.IsLedRequirementMetWithHybridTolerance(stageReq, pot))
                return false;
            
            // Verifica intensità luce nel range (se implementato)
            // Per ora: solo verifica LED corretto
            // TODO: Aggiungere verifica intensità luce quando sistema sarà implementato
            return true;
        }
    }
    
    /// <summary>
    /// BLK-03.01-T2: Risultato calcolo punti giornalieri
    /// </summary>
    public struct GrowthPointsResult
    {
        public int WaterPoint;      // 0 o 1
        public int LightPoint;      // 0 o 1
        public int FertilizerPoint; // 0 o 1
        public int TotalPoints => WaterPoint + LightPoint + FertilizerPoint;
    }
}

