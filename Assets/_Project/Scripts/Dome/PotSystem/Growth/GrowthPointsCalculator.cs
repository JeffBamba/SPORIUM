using UnityEngine;
using Sporae.Dome.PotSystem.Growth;
using System.IO;
using System;
using Sporae.DevTools;

namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// BLK-03.01-T2: Calcolatore punti crescita basato su valori nel range ideale
    /// </summary>
    public static class GrowthPointsCalculator
    {
        // #region agent log
        // DEBUG: Helper per logging NDJSON
        private static void LogToDebugFile(string location, string message, object data, string hypothesisId = null, string runId = "debug")
        {
            try
            {
                string logPath = @"d:\Sporae_Build_Beta\.cursor\debug.log";
                var logEntry = new
                {
                    id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid().ToString().Substring(0, 8)}",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    location = location,
                    message = message,
                    data = data,
                    sessionId = "debug-session",
                    runId = runId,
                    hypothesisId = hypothesisId
                };
                string jsonLine = JsonUtility.ToJson(logEntry) + Environment.NewLine;
                File.AppendAllText(logPath, jsonLine);
            }
            catch { }
        }
        // #endregion
        
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
            
            // Ottieni requisiti per lo stadio corrente
            PlantStage currentStage = (PlantStage)pot.Stage;
            StageRequirements stageReq = plantData.GetStageRequirements(currentStage);
            
            if (stageReq == null)
            {
                // Se non ci sono requisiti, nessun punto
                return result;
            }
            
            // 1. Verifica water nel range ideale (hydrationPercent nel range)
            int maxHydration = potConfig != null ? potConfig.MaxHydration : 10;
            int hydrationPercent = maxHydration > 0 ? 
                Mathf.RoundToInt((float)pot.Hydration / maxHydration * 100f) : 0;
            
            // #region agent log
            bool hydrationInRange = stageReq.IsHydrationInRange(hydrationPercent);
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_POINTS] {pot.PotId} Water: {pot.Hydration}/{maxHydration} ({hydrationPercent}%), Range={stageReq.hydrationMin}-{stageReq.hydrationMax}, InRange={hydrationInRange}, WateringSystemOn={pot.WateringSystemOn}");
            
            LogToDebugFile(
                "GrowthPointsCalculator:CalculateDailyPoints:WATER_CHECK",
                $"Verifica Water Point - PotId={pot.PotId}",
                new {
                    potId = pot.PotId,
                    stage = currentStage.ToString(),
                    hydration = pot.Hydration,
                    maxHydration = maxHydration,
                    hydrationPercent = hydrationPercent,
                    hydrationMin = stageReq.hydrationMin,
                    hydrationMax = stageReq.hydrationMax,
                    hydrationMed = stageReq.hydrationMed,
                    isInRange = hydrationInRange,
                    waterPointBefore = result.WaterPoint
                },
                "F"
            );
            // #endregion
            
            if (hydrationInRange)
            {
                result.WaterPoint = 1;
                pot.GrowthPointsWater += 1;
            }
            
            // 2. Verifica light nel range ideale (LED corretto + intensità nel range)
            bool lightInRange = IsLightInOptimalRange(pot, plantData, stageReq, potConfig);
            
            // #region agent log
            bool isLedRequirementMet = stageReq.IsLedRequirementMet(pot.LedSystemState);
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_POINTS] {pot.PotId} Light: LED={pot.LedSystemState}, Required={stageReq.GetRequiredLed()?.ToString() ?? "None"}, Met={isLedRequirementMet}, InRange={lightInRange}");
            
            LogToDebugFile(
                "GrowthPointsCalculator:CalculateDailyPoints:LIGHT_CHECK",
                $"Verifica Light Point - PotId={pot.PotId}",
                new {
                    potId = pot.PotId,
                    stage = currentStage.ToString(),
                    ledSystemState = pot.LedSystemState.ToString(),
                    requiredLed = stageReq.GetRequiredLed()?.ToString() ?? "None",
                    isLedRequirementMet = isLedRequirementMet,
                    isLightInRange = lightInRange,
                    lightPointBefore = result.LightPoint
                },
                "F"
            );
            // #endregion
            
            if (lightInRange)
            {
                result.LightPoint = 1;
                pot.GrowthPointsLight += 1;
            }
            
            // 3. Verifica fertilizer nel range ideale (FertilizerLevel 0-100% nel range)
            bool fertilizerInRange = stageReq.IsFertilizerInRange(pot.FertilizerLevel);
            
            // #region agent log
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_POINTS] {pot.PotId} Fertilizer: {pot.FertilizerLevel}%, Range={stageReq.fertilizerMin}-{stageReq.fertilizerMax}, InRange={fertilizerInRange}");
            
            LogToDebugFile(
                "GrowthPointsCalculator:CalculateDailyPoints:FERTILIZER_CHECK",
                $"Verifica Fertilizer Point - PotId={pot.PotId}",
                new {
                    potId = pot.PotId,
                    stage = currentStage.ToString(),
                    fertilizerLevel = pot.FertilizerLevel,
                    fertilizerMin = stageReq.fertilizerMin,
                    fertilizerMax = stageReq.fertilizerMax,
                    fertilizerMed = stageReq.fertilizerMed,
                    isInRange = fertilizerInRange,
                    fertilizerPointBefore = result.FertilizerPoint
                },
                "F"
            );
            // #endregion
            
            if (fertilizerInRange)
            {
                result.FertilizerPoint = 1;
                pot.GrowthPointsFertilizer += 1;
            }
            
            // #region agent log
            SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_POINTS] {pot.PotId} RESULT: W={result.WaterPoint}, L={result.LightPoint}, F={result.FertilizerPoint}, Total={result.TotalPoints}, Accumulated: W={pot.GrowthPointsWater}, L={pot.GrowthPointsLight}, F={pot.GrowthPointsFertilizer}");
            
            LogToDebugFile(
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
                "F"
            );
            // #endregion
            
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
                // Quando LED è OFF, verifica se lo stress percentage è nel range ottimale (tra 0% e 100%)
                int consecutiveDays = pot.GetConsecutiveLedDays();
                int maxDaysForFullStress = potConfig != null ? potConfig.MaxDaysForFullStress : 5;
                float stressPercentage = Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f;
                
                // Stress è nel range ottimale se è tra 0% e 100% (esclusi gli estremi)
                // Questo è coerente con la logica di PlantConditionSystem
                bool stressInOptimalRange = stressPercentage > 0f && stressPercentage < 100f;
                
                SporiumLogger.LogDebug(LogCategory.Pot, $"[DEBUG_POINTS] {pot.PotId} Light OFF: ConsecutiveDays={consecutiveDays}, Stress%={stressPercentage:F1}, InOptimalRange={stressInOptimalRange}");
                
                // Se lo stress è nel range ottimale, considera OK anche se LED è spento
                return stressInOptimalRange;
            }
            
            // Verifica LED richiesto quando LED è acceso
            if (!stageReq.IsLedRequirementMet(pot.LedSystemState))
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

