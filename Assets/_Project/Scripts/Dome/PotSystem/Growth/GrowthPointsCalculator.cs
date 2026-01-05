using UnityEngine;
using Sporae.Dome.PotSystem.Growth;

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
            
            if (stageReq.IsHydrationInRange(hydrationPercent))
            {
                result.WaterPoint = 1;
                pot.GrowthPointsWater += 1;
            }
            
            // 2. Verifica light nel range ideale (LED corretto + intensità nel range)
            if (IsLightInOptimalRange(pot, plantData, stageReq))
            {
                result.LightPoint = 1;
                pot.GrowthPointsLight += 1;
            }
            
            // 3. Verifica fertilizer nel range ideale (FertilizerLevel 0-100% nel range)
            if (stageReq.IsFertilizerInRange(pot.FertilizerLevel))
            {
                result.FertilizerPoint = 1;
                pot.GrowthPointsFertilizer += 1;
            }
            
            return result;
        }
        
        private static bool IsLightInOptimalRange(
            PotStateModel pot, 
            PlantData plantData, 
            StageRequirements stageReq)
        {
            // Verifica LED richiesto
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

