using UnityEngine;
using Sporae.Dome.PotSystem.Mold;
using Sporae.Dome.PotSystem.Level;
using Sporae.Dome.PotSystem.Growth;
using _Project; // Per PhSystem
using Sporae.DevTools;

namespace Sporae.Dome.PotSystem.Mold
{
    /// <summary>
    /// Sistema muffe (BLK-07.01).
    /// Gestisce calcolo rischio, infestazione e applicazione effetti.
    /// </summary>
    public static class MoldSystem
    {
        /// <summary>
        /// Calcola rischio muffe (0-3) basato su fattori
        /// </summary>
        public static float CalculateMoldRisk(PotStateModel potState, PhSystem phSystem, PlantData plantData, MoldConfig config)
        {
            if (potState == null || config == null)
                return 0f;
            
            float risk = 0f;
            
            // 1. Overwatering prolungato: +1 per giorno oltre soglia
            if (potState.DaysOverwateringConsecutive >= config.overwateringDaysThreshold)
            {
                risk += 1f;
            }
            
            // 2. pH acido (≤-20): +1
            if (phSystem != null)
            {
                float currentPh = phSystem.CurrentPh;
                if (currentPh <= config.acidicPhThreshold)
                {
                    risk += 1f;
                }
            }
            
            // 3. Piante Evil: +1 bonus rischio
            if (plantData != null && plantData.Family == PlantFamily.Evil)
            {
                risk += 1f;
            }
            
            // 4. Mancata potatura: +0.5 per giorno senza potatura (accumulo)
            risk += potState.DaysWithoutPruning * config.pruningNeglectAccumulation;
            
            return Mathf.Clamp(risk, 0f, 3f);
        }
        
        /// <summary>
        /// Ottiene livello rischio muffe (0=None, 1=Mild, 2=Severe, 3=Critical)
        /// </summary>
        public static int GetMoldRiskLevel(PotStateModel potState, PhSystem phSystem, PlantData plantData, MoldConfig config)
        {
            if (potState == null || config == null)
                return 0;
            
            float risk = CalculateMoldRisk(potState, phSystem, plantData, config);
            
            if (risk >= config.criticalRiskThreshold)
                return 3; // Critical
            else if (risk >= config.severeRiskThreshold)
                return 2; // Severe
            else if (risk >= config.mildRiskThreshold)
                return 1; // Mild
            else
                return 0; // None
        }
        
        /// <summary>
        /// Verifica se rischio si materializza in infestazione
        /// BUG FIX: Infestazione solo dopo 2 giorni consecutivi a livello 3
        /// </summary>
        public static bool CheckInfestation(int moldRiskLevel, int daysAtLevel3)
        {
            // Infestazione solo se livello 3 E almeno 2 giorni consecutivi
            return moldRiskLevel == 3 && daysAtLevel3 >= 2;
        }
        
        /// <summary>
        /// Applica effetti infestazione
        /// </summary>
        public static void ApplyInfestation(PotStateModel potState, int moldRiskLevel, MoldConfig config, PlantLevelConfig levelConfig)
        {
            if (potState == null || config == null || moldRiskLevel < 1)
                return;
            
            if (moldRiskLevel == 1) // Mild
            {
                // Riduce livello di 1
                if (levelConfig != null)
                {
                    PlantLevelSystem.ReduceLevel(potState, config.mildLevelReduction);
                }
                
                // Riduce score di 10
                potState.ConditionScore = Mathf.Max(0, potState.ConditionScore - config.mildScorePenalty);
                
                SporiumLogger.LogWarning(LogCategory.Pot, $"{potState.PotId}: Infestazione Mild applicata (-{config.mildLevelReduction} livello, -{config.mildScorePenalty} score)");
            }
            else if (moldRiskLevel >= 2) // Severe o Critical
            {
                // Riduce livello di 3
                if (levelConfig != null)
                {
                    PlantLevelSystem.ReduceLevel(potState, config.severeLevelReduction);
                }
                
                // Riduce score di 30
                potState.ConditionScore = Mathf.Max(0, potState.ConditionScore - config.severeScorePenalty);
                
                SporiumLogger.LogWarning(LogCategory.Pot, $"{potState.PotId}: Infestazione Severe applicata (-{config.severeLevelReduction} livelli, -{config.severeScorePenalty} score, crescita bloccata)");
            }
        }
        
        /// <summary>
        /// Rimuove infestazione (chiamato da potatura o spray)
        /// </summary>
        public static void RemoveInfestation(PotStateModel potState)
        {
            if (potState == null)
                return;
            
            // #region agent log
            try {
                var logData = new { potId = potState.PotId, moldRiskLevelBefore = potState.MoldRiskLevel, daysWithoutPruningBefore = potState.DaysWithoutPruning, isInfestedBefore = potState.IsInfested, daysAtLevel3Before = potState.DaysAtMoldRiskLevel3 };
                var logJson = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"BUG1-B\",\"location\":\"MoldSystem.cs:RemoveInfestation\",\"message\":\"RemoveInfestation: Before reset\",\"data\":{JsonUtility.ToJson(logData)},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
                System.IO.File.AppendAllText(@"d:\Sporae_Build_Beta\.cursor\debug.log", logJson);
            } catch { }
            // #endregion
            
            potState.MoldRiskLevel = 0;
            potState.DaysWithoutPruning = 0;
            potState.IsInfested = false;
            potState.DaysAtMoldRiskLevel3 = 0;
            
            // #region agent log
            try {
                var logData2 = new { potId = potState.PotId, moldRiskLevelAfter = potState.MoldRiskLevel, daysWithoutPruningAfter = potState.DaysWithoutPruning, isInfestedAfter = potState.IsInfested, daysAtLevel3After = potState.DaysAtMoldRiskLevel3 };
                var logJson2 = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"BUG1-B\",\"location\":\"MoldSystem.cs:RemoveInfestation\",\"message\":\"RemoveInfestation: After reset\",\"data\":{JsonUtility.ToJson(logData2)},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
                System.IO.File.AppendAllText(@"d:\Sporae_Build_Beta\.cursor\debug.log", logJson2);
            } catch { }
            // #endregion
            
            SporiumLogger.LogInfo(LogCategory.Pot, $"{potState.PotId}: Infestazione rimossa");
        }
    }
}

