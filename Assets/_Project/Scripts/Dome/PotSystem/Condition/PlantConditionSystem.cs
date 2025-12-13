using System.Collections.Generic;
using UnityEngine;
using Sporae.Dome.PotSystem.Growth;
using Sporae.Dome.PotSystem.Mold;
using _Project; // Per PhSystem
using _Project.Sporae.Core;

namespace Sporae.Dome.PotSystem.Condition
{
    /// <summary>
    /// Sistema di calcolo della condizione della pianta (score 0-100)
    /// Calcola lo score basandosi su idratazione, luce, pH, watering, mold risk, burn stress
    /// </summary>
    public static class PlantConditionSystem
    {
        private const int BASE_SCORE = 50; // Score neutro di partenza
        
        // Contributi positivi
        private const int BONUS_HYDRATION_OPTIMAL = 20;
        private const int BONUS_LIGHT_CORRECT = 15;
        private const int BONUS_WATERING_ON = 10;
        private const int BONUS_PH_OPTIMAL = 10;
        private const int BONUS_NO_MOLD = 5;
        
        // Contributi negativi
        private const int MALUS_HYDRATION_OUT_OF_RANGE = 15;
        private const int MALUS_LIGHT_WRONG_OR_ABSENT = 10;
        private const int MALUS_PH_OPPOSITE = 20;
        private const int MALUS_PH_ULTRA = 15;
        private const int MALUS_OVERWATERING = 25;
        private const int MALUS_MOLD_MILD = 10;
        private const int MALUS_MOLD_SEVERE = 30;
        private const int MALUS_BURN_STRESS = 20;
        
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
                return new ConditionResult(BASE_SCORE, PlantCondition.Sana, ForecastDirection.Stable, 0, 
                    new ConditionContributor[0]);
            }
            
            int score = BASE_SCORE;
            List<ConditionContributor> contributors = new List<ConditionContributor>();
            
            // === CONTRIBUTI POSITIVI ===
            
            // 1. Idratazione in range ottimale per stadio
            int maxHydration = potConfig != null ? potConfig.MaxHydration : 4;
            int hydrationPercent = maxHydration > 0 ? Mathf.RoundToInt((float)potState.Hydration / maxHydration * 100f) : 0;
            
            // Verifica range ottimale per stadio (50-75% per la maggior parte degli stadi)
            bool isHydrationOptimal = IsHydrationInOptimalRange(potState, plantData, maxHydration);
            if (isHydrationOptimal)
            {
                score += BONUS_HYDRATION_OPTIMAL;
                contributors.Add(new ConditionContributor("Idratazione ottimale", BONUS_HYDRATION_OPTIMAL, true));
            }
            
            // 2. Luce corretta per stadio (LED corretto)
            bool hasCorrectLight = IsLightCorrectForStage(potState, plantData, currentDay);
            if (hasCorrectLight)
            {
                score += BONUS_LIGHT_CORRECT;
                contributors.Add(new ConditionContributor("Luce corretta (LED)", BONUS_LIGHT_CORRECT, true));
            }
            
            // 3. Watering System ON e dosaggio corretto
            if (potState.WateringSystemOn && isHydrationOptimal)
            {
                score += BONUS_WATERING_ON;
                contributors.Add(new ConditionContributor("Watering ON e dosaggio corretto", BONUS_WATERING_ON, true));
            }
            
            // 4. pH Dome in banda affinità pianta
            if (phSystem != null)
            {
                float currentPh = phSystem.CurrentPh;
                if (plantData.IsPhInOptimalRange(currentPh))
                {
                    score += BONUS_PH_OPTIMAL;
                    contributors.Add(new ConditionContributor("pH Dome in banda affinità", BONUS_PH_OPTIMAL, true));
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
                score += BONUS_NO_MOLD;
                contributors.Add(new ConditionContributor("Nessun Mold Risk", BONUS_NO_MOLD, true));
            }
            
            // === CONTRIBUTI NEGATIVI ===
            
            // 1. Idratazione fuori range (Dry/Wet)
            if (!isHydrationOptimal)
            {
                if (hydrationPercent < 25 || hydrationPercent > 90)
                {
                    score -= MALUS_HYDRATION_OUT_OF_RANGE;
                    contributors.Add(new ConditionContributor("Idratazione fuori range", -MALUS_HYDRATION_OUT_OF_RANGE, false));
                }
            }
            
            // 2. Luce assente o spettro sbagliato
            if (!hasCorrectLight)
            {
                score -= MALUS_LIGHT_WRONG_OR_ABSENT;
                contributors.Add(new ConditionContributor("Luce assente o spettro sbagliato", -MALUS_LIGHT_WRONG_OR_ABSENT, false));
            }
            
            // 3. pH opposto alla pianta (Pure in acido, Evil in basico)
            if (phSystem != null)
            {
                float currentPh = phSystem.CurrentPh;
                PhSystem.PhBand phBand = phSystem.EvaluateState();
                
                // Pure preferisce basico, Evil preferisce acido
                if (plantData.Family == PlantFamily.Pure && (phBand == PhSystem.PhBand.UltraAcid || phBand == PhSystem.PhBand.StableAcid))
                {
                    score -= MALUS_PH_OPPOSITE;
                    contributors.Add(new ConditionContributor("pH opposto (Pure in acido)", -MALUS_PH_OPPOSITE, false));
                }
                else if (plantData.Family == PlantFamily.Evil && (phBand == PhSystem.PhBand.UltraBasic || phBand == PhSystem.PhBand.StableBasic))
                {
                    score -= MALUS_PH_OPPOSITE;
                    contributors.Add(new ConditionContributor("pH opposto (Evil in basico)", -MALUS_PH_OPPOSITE, false));
                }
                
                // 4. pH in banda Ultra (≤-80 o ≥+80)
                if (phBand == PhSystem.PhBand.UltraAcid || phBand == PhSystem.PhBand.UltraBasic)
                {
                    score -= MALUS_PH_ULTRA;
                    contributors.Add(new ConditionContributor("pH in banda Ultra", -MALUS_PH_ULTRA, false));
                }
            }
            
            // 5. Overwatering attivo (forza stato Stressata)
            bool isOverwatering = IsOverwatering(potState, maxHydration);
            if (isOverwatering)
            {
                score -= MALUS_OVERWATERING;
                contributors.Add(new ConditionContributor("Overwatering attivo", -MALUS_OVERWATERING, false));
            }
            
            // 6. Mold Risk (moldRiskLevel già calcolato sopra)
            if (moldRiskLevel == 1) // Mild
            {
                score -= MALUS_MOLD_MILD;
                contributors.Add(new ConditionContributor("Mold Risk lieve", -MALUS_MOLD_MILD, false));
            }
            else if (moldRiskLevel >= 2) // Severe
            {
                score -= MALUS_MOLD_SEVERE;
                contributors.Add(new ConditionContributor("Mold Risk severo", -MALUS_MOLD_SEVERE, false));
            }
            
            // 7. Burn Stress attivo (LED 4+ giorni consecutivi)
            int burnRiskLevel = potState.GetBurnRiskLevel();
            if (burnRiskLevel >= 2) // Alto o Critico
            {
                score -= MALUS_BURN_STRESS;
                contributors.Add(new ConditionContributor("Burn Stress attivo", -MALUS_BURN_STRESS, false));
            }
            
            // Clamp score 0-100
            score = Mathf.Clamp(score, 0, 100);
            
            // DEBUG: Log dettagliato del calcolo
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (potState != null && potState.PotId != null)
            {
                Debug.Log($"[PlantConditionSystem] 🔍 DEBUG Calcolo {potState.PotId}: Base={BASE_SCORE}, Score finale={score}, Contributi: {contributors.Count} (Pos: {System.Array.FindAll(contributors.ToArray(), c => c.IsPositive).Length}, Neg: {System.Array.FindAll(contributors.ToArray(), c => !c.IsPositive).Length})");
                foreach (var c in contributors)
                {
                    Debug.Log($"  - {c.Source}: {(c.IsPositive ? "+" : "")}{c.Value}");
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
                // Range ottimale: tra hydrationMin e hydrationMax (range accettabile per lo stadio)
                // Per il bonus "ottimale", usiamo un range più stretto intorno a hydrationMed (50-75% del range)
                int rangeSize = stageReq.hydrationMax - stageReq.hydrationMin;
                int optimalTolerance = Mathf.Max(5, rangeSize / 4); // 25% del range, minimo 5%
                int optimalMin = Mathf.Max(stageReq.hydrationMin, stageReq.hydrationMed - optimalTolerance);
                int optimalMax = Mathf.Min(stageReq.hydrationMax, stageReq.hydrationMed + optimalTolerance);
                
                return hydrationPercent >= optimalMin && hydrationPercent <= optimalMax;
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
        /// Verifica se c'è overwatering (idratazione >= 75%)
        /// </summary>
        public static bool IsOverwatering(PotStateModel potState, int maxHydration)
        {
            if (potState == null || maxHydration <= 0)
                return false;
            
            int hydrationPercent = Mathf.RoundToInt((float)potState.Hydration / maxHydration * 100f);
            return hydrationPercent >= 75;
        }
        
        /// <summary>
        /// Mappa score (0-100) a condizione
        /// </summary>
        private static PlantCondition MapScoreToCondition(int score, bool isOverwatering)
        {
            // Overwatering forza stato Stressata indipendentemente dallo score
            if (isOverwatering && score >= 40)
            {
                return PlantCondition.Stressata;
            }
            
            if (score >= 90)
                return PlantCondition.Rigogliosa;
            if (score >= 70)
                return PlantCondition.Sana;
            if (score >= 40)
                return PlantCondition.Stressata;
            if (score >= 20)
                return PlantCondition.Appassita;
            return PlantCondition.Critica;
        }
        
        /// <summary>
        /// Calcola forecast (tendenza) basandosi su delta score
        /// </summary>
        private static ForecastDirection CalculateForecast(int scoreDelta)
        {
            if (scoreDelta > 5)
                return ForecastDirection.Up;
            if (scoreDelta < -5)
                return ForecastDirection.Down;
            return ForecastDirection.Stable;
        }
        
        /// <summary>
        /// Ottiene il nome della condizione in italiano
        /// </summary>
        public static string GetConditionName(PlantCondition condition, bool isOverwatering = false)
        {
            if (isOverwatering && condition == PlantCondition.Stressata)
            {
                return "Stressata (Overwatering)";
            }
            
            return condition switch
            {
                PlantCondition.Rigogliosa => "Rigogliosa",
                PlantCondition.Sana => "Sana",
                PlantCondition.Stressata => "Stressata",
                PlantCondition.Appassita => "Appassita",
                PlantCondition.Critica => "Critica",
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

