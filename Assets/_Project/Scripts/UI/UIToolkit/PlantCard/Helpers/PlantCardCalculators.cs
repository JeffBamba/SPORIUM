using UnityEngine;
using Sporae.Dome.PotSystem.Growth;

namespace Sporae.UI.UIToolkit.PlantCard.Helpers
{
    /// <summary>
    /// Helper class per calcoli in PlantCard V2.0.
    /// Centralizza tutti i calcoli per consistenza e testabilità.
    /// </summary>
    public static class PlantCardCalculators
    {
        /// <summary>
        /// Calcola percentuale idratazione (0-100%)
        /// </summary>
        public static int CalculateHydrationPercent(int hydration, int maxHydration)
        {
            if (maxHydration <= 0) return 0;
            return Mathf.RoundToInt((float)hydration / maxHydration * 100f);
        }
        
        /// <summary>
        /// Calcola percentuale light stress (0-100%)
        /// </summary>
        public static int CalculateLightStressPercent(int lightExposure, int maxLightExposure)
        {
            if (maxLightExposure <= 0) return 0;
            return Mathf.RoundToInt((float)lightExposure / maxLightExposure * 100f);
        }
        
        /// <summary>
        /// Calcola percentuale condizione (0-100%)
        /// </summary>
        public static int CalculateConditionPercent(int conditionScore)
        {
            return Mathf.Clamp(conditionScore, 0, 100);
        }
        
        /// <summary>
        /// Calcola percentuale mold risk (0-100%)
        /// </summary>
        public static int CalculateMoldRiskPercent(int moldRiskLevel)
        {
            // Mold risk level 0-3 mappato a 0-100%
            return Mathf.Clamp(moldRiskLevel * 33, 0, 100);
        }
        
        /// <summary>
        /// Ottiene testo range ottimale per idratazione da StageRequirements
        /// </summary>
        public static string GetHydrationOptimalRangeText(PlantData plantData, PotStateModel state, PotSystemConfig config)
        {
            if (plantData == null || config == null || state == null || !state.HasPlant)
                return "N/A";
            
            // Calcola range ottimale basato su stage requirements
            PlantStage currentStage = (PlantStage)state.Stage;
            StageRequirements stageReq = plantData.GetStageRequirements(currentStage);
            
            if (stageReq != null)
            {
                // Usa range da StageRequirements
                return $"Range Ideale: {stageReq.hydrationMin}% - {stageReq.hydrationMed}% - {stageReq.hydrationMax}%";
            }
            
            // Fallback: range fisso se StageRequirements non disponibile
            int maxHydration = config.MaxHydration;
            int min = Mathf.RoundToInt(maxHydration * 0.45f);
            int optimal = Mathf.RoundToInt(maxHydration * 0.55f);
            int max = Mathf.RoundToInt(maxHydration * 0.65f);
            
            return $"Range Ideale: {min}% - {optimal}% - {max}%";
        }
        
        /// <summary>
        /// Ottiene testo range ottimale per light stress da StageRequirements
        /// </summary>
        public static string GetLightStressOptimalRangeText(PlantData plantData, PotStateModel state, PotSystemConfig config)
        {
            if (config == null || plantData == null || state == null || !state.HasPlant)
                return "N/A";
            
            // Calcola range ottimale basato su stage requirements
            PlantStage currentStage = (PlantStage)state.Stage;
            StageRequirements stageReq = plantData.GetStageRequirements(currentStage);
            
            if (stageReq != null)
            {
                // Usa range da StageRequirements
                return $"Range Ideale: {stageReq.lightMin}% - {stageReq.lightMed}% - {stageReq.lightMax}%";
            }
            
            // Fallback: range fisso se StageRequirements non disponibile
            int maxLight = config.MaxLightExposure;
            int min = Mathf.RoundToInt(maxLight * 0.5f);
            int max = Mathf.RoundToInt(maxLight * 0.75f);
            
            return $"Range Ideale: {min}% - {max}%";
        }
        
        /// <summary>
        /// Ottiene testo range ottimale per fertilizzazione da StageRequirements
        /// </summary>
        public static string GetFertilizationOptimalRangeText(PlantData plantData, PotStateModel state)
        {
            if (plantData == null || state == null || !state.HasPlant)
                return "N/A";
            
            // Calcola range ottimale basato su stage requirements
            PlantStage currentStage = (PlantStage)state.Stage;
            StageRequirements stageReq = plantData.GetStageRequirements(currentStage);
            
            if (stageReq != null)
            {
                // Usa range da StageRequirements
                return $"Range Ideale: {stageReq.fertilizerMin}% - {stageReq.fertilizerMed}% - {stageReq.fertilizerMax}%";
            }
            
            // Fallback: range fisso se StageRequirements non disponibile
            return "Range Ideale: 0% - 50% - 100%";
        }
        
        /// <summary>
        /// Calcola numero segmenti filled per segmented bar (0-10)
        /// </summary>
        public static int CalculateSegmentedBarFilled(int value, int max, int segmentCount = 10)
        {
            if (max <= 0) return 0;
            float percent = (float)value / max;
            return Mathf.RoundToInt(percent * segmentCount);
        }
        
        /// <summary>
        /// Calcola numero segmenti filled per growth progress bar (0-7)
        /// </summary>
        public static int CalculateGrowthProgressFilled(int daysInStage, int maxDays, int segmentCount = 7)
        {
            if (maxDays <= 0) return 0;
            float percent = (float)daysInStage / maxDays;
            return Mathf.Clamp(Mathf.RoundToInt(percent * segmentCount), 0, segmentCount);
        }
        
        /// <summary>
        /// Verifica se un valore è nel range ottimale
        /// </summary>
        public static bool IsInOptimalRange(int value, int min, int max)
        {
            return value >= min && value <= max;
        }
    }
}

