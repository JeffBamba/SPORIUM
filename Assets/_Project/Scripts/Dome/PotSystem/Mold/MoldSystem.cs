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
        /// Riduce il livello rischio muffe di 1 (o azzera se <= 1).
        /// Usato da additivi basici e da alcune azioni di pulizia.
        /// </summary>
        public static void ReduceMoldRiskLevel(PotStateModel potState)
        {
            if (potState == null)
                return;

            int old = potState.MoldRiskLevel;
            if (old <= 1)
            {
                potState.MoldRiskLevel = 0;
            }
            else
            {
                potState.MoldRiskLevel = Mathf.Clamp(old - 1, 0, 3);
            }

            // Se scende sotto 3, reset tracking infestation
            if (potState.MoldRiskLevel < 3)
            {
                potState.DaysAtMoldRiskLevel3 = 0;
                potState.IsInfested = false;
            }

            // Non resettiamo DaysOverwateringConsecutive: è calcolato dal DayCycle.
            SporiumLogger.LogInfo(LogCategory.Pot, $"{potState.PotId}: MoldRiskLevel ridotto {old} -> {potState.MoldRiskLevel}");
        }

        /// <summary>
        /// Aumenta il livello rischio muffe di 1 (clamp 0-3).
        /// Se il pot è già a livello 3, tenta di aumentare/innescare rischio sul pot vicino.
        /// </summary>
        public static void IncreaseMoldRiskLevel(PotStateModel potState, PotStateModel nearbyPot = null)
        {
            if (potState == null)
                return;

            if (potState.MoldRiskLevel < 3)
            {
                int old = potState.MoldRiskLevel;
                potState.MoldRiskLevel = Mathf.Clamp(old + 1, 0, 3);

                if (potState.MoldRiskLevel == 3)
                {
                    // Inizia il contatore: DayCycle lo porterà a 2 se resta a 3 anche domani
                    potState.DaysAtMoldRiskLevel3 = Mathf.Max(1, potState.DaysAtMoldRiskLevel3);
                }

                SporiumLogger.LogWarning(LogCategory.Pot, $"{potState.PotId}: MoldRiskLevel aumentato {old} -> {potState.MoldRiskLevel}");
                return;
            }

            // Già a livello 3: tenta di propagare al pot vicino
            if (nearbyPot == null || nearbyPot == potState)
            {
                SporiumLogger.LogWarning(LogCategory.Pot, $"{potState.PotId}: MoldRiskLevel già 3, ma nessun pot vicino valido per propagazione");
                return;
            }

            int oldNearby = nearbyPot.MoldRiskLevel;
            if (nearbyPot.MoldRiskLevel < 3)
            {
                nearbyPot.MoldRiskLevel = Mathf.Clamp(nearbyPot.MoldRiskLevel + 1, 0, 3);
                if (nearbyPot.MoldRiskLevel == 3)
                {
                    nearbyPot.DaysAtMoldRiskLevel3 = Mathf.Max(1, nearbyPot.DaysAtMoldRiskLevel3);
                }
            }
            else
            {
                // Se anche lui è già a 3, almeno assicuriamo contatore >= 1
                nearbyPot.DaysAtMoldRiskLevel3 = Mathf.Max(1, nearbyPot.DaysAtMoldRiskLevel3);
            }

            SporiumLogger.LogWarning(LogCategory.Pot, $"{potState.PotId}: Propagazione muffe su pot vicino {nearbyPot.PotId} (Lvl {oldNearby} -> {nearbyPot.MoldRiskLevel})");
        }

        /// <summary>
        /// Calcola rischio muffe (0-3) basato SOLO su overwatering prolungato
        /// 1 livello per ogni giorno oltre la soglia (es. soglia 3: 4 giorni = Level 1, 5 giorni = Level 2, 6 giorni = Level 3)
        /// </summary>
        public static float CalculateMoldRisk(PotStateModel potState, PhSystem phSystem, PlantData plantData, MoldConfig config)
        {
            if (potState == null || config == null)
                return 0f;
            
            // Calcola giorni oltre la soglia: se soglia è 3, allora:
            // 3 giorni = 0 (ancora sotto soglia)
            // 4 giorni = 1 livello (1 giorno oltre)
            // 5 giorni = 2 livelli (2 giorni oltre)
            // 6 giorni = 3 livelli (3 giorni oltre)
            int daysOverThreshold = Mathf.Max(0, potState.DaysOverwateringConsecutive - config.overwateringDaysThreshold);
            
            // Clamp a 0-3 (massimo 3 livelli)
            return Mathf.Clamp(daysOverThreshold, 0f, 3f);
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

            // Nuovo comportamento: riduce di 1 (o azzera se <=1)
            ReduceMoldRiskLevel(potState);
            potState.DaysWithoutPruning = 0;

            SporiumLogger.LogInfo(LogCategory.Pot, $"{potState.PotId}: RemoveInfestation eseguito (riduzione rischio / reset daysWithoutPruning)");
        }
    }
}

