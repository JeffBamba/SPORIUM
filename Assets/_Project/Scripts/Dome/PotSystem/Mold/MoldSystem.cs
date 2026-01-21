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
        /// MOLD SYNERGY: Considera famiglia pianta per riduzione livello (EVIL: -1, PURE: -5, Standard: -3)
        /// </summary>
        public static void ApplyInfestation(PotStateModel potState, int moldRiskLevel, MoldConfig config, PlantLevelConfig levelConfig)
        {
            if (potState == null || config == null || moldRiskLevel < 1)
                return;
            
            // Ottieni famiglia pianta per applicare riduzione livello differenziata
            PlantData plantData = potState.GetPlantData();
            PlantFamily family = plantData != null ? plantData.Family : PlantFamily.Standard;
            
            int levelReduction;
            
            if (moldRiskLevel == 1) // Mild
            {
                // Mild: riduzione basata su famiglia
                switch (family)
                {
                    case PlantFamily.Evil:
                        levelReduction = 0; // EVIL: NO riduzione livello (o minima)
                        break;
                    case PlantFamily.Pure:
                        levelReduction = 2; // PURE: riduzione maggiore anche per Mild
                        break;
                    default:
                        levelReduction = config.mildLevelReduction; // Standard: -1
                        break;
                }
                
                if (levelConfig != null && levelReduction > 0)
                {
                    PlantLevelSystem.ReduceLevel(potState, levelReduction);
                }
                
                // Riduce score di 10
                potState.ConditionScore = Mathf.Max(0, potState.ConditionScore - config.mildScorePenalty);
                
                SporiumLogger.LogWarning(LogCategory.Pot, $"{potState.PotId}: Infestazione Mild applicata (Famiglia: {family}, -{levelReduction} livello, -{config.mildScorePenalty} score)");
            }
            else if (moldRiskLevel >= 2) // Severe o Critical
            {
                // Severe/Critical: riduzione basata su famiglia
                switch (family)
                {
                    case PlantFamily.Evil:
                        levelReduction = 1; // EVIL: riduzione minore (-1 invece di -3)
                        break;
                    case PlantFamily.Pure:
                        levelReduction = 5; // PURE: riduzione maggiore (-5 invece di -3)
                        break;
                    default:
                        levelReduction = config.severeLevelReduction; // Standard: -3
                        break;
                }
                
                if (levelConfig != null)
                {
                    PlantLevelSystem.ReduceLevel(potState, levelReduction);
                }
                
                // Riduce score di 30
                potState.ConditionScore = Mathf.Max(0, potState.ConditionScore - config.severeScorePenalty);
                
                SporiumLogger.LogWarning(LogCategory.Pot, $"{potState.PotId}: Infestazione Severe applicata (Famiglia: {family}, -{levelReduction} livelli, -{config.severeScorePenalty} score, crescita bloccata)");
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
        
        /// <summary>
        /// MOLD SYNERGY: Calcola bonus mutazioni basato su Mold Risk + Famiglia + pH
        /// EVIL prospera con muffe (bonus), PURE soffre doppiamente (penalità)
        /// </summary>
        /// <param name="moldRiskLevel">Livello rischio muffe (0-3)</param>
        /// <param name="family">Famiglia della pianta</param>
        /// <param name="phBand">Banda pH corrente</param>
        /// <returns>Bonus probabilità mutazioni (es. 0.15f = +15%)</returns>
        public static float GetMoldMutationBonus(int moldRiskLevel, PlantFamily family, PhSystem.PhBand phBand)
        {
            if (moldRiskLevel <= 0)
                return 0f; // Nessun bonus se non c'è Mold Risk
            
            switch (family)
            {
                case PlantFamily.Evil:
                    // EVIL con Mold Risk: bonus mutazioni
                    float baseBonus = moldRiskLevel == 3 ? 0.3f : 0.15f; // Level 3: +30%, Level 1-2: +15%
                    
                    // Bonus extra se anche in pH Basico (sinergia doppia)
                    if (phBand == PhSystem.PhBand.UltraBasic || phBand == PhSystem.PhBand.StableBasic)
                    {
                        baseBonus += 0.1f; // +10% aggiuntivo
                    }
                    
                    return baseBonus;
                    
                case PlantFamily.Pure:
                    // PURE con Mold Risk: penalità mutazioni
                    float basePenalty = moldRiskLevel == 3 ? 0.2f : 0.1f; // Level 3: -20%, Level 1-2: -10%
                    
                    // Penalità extra se anche in pH Acido (sinergia doppia)
                    if (phBand == PhSystem.PhBand.UltraAcid || phBand == PhSystem.PhBand.StableAcid)
                    {
                        basePenalty += 0.1f; // -10% aggiuntivo
                    }
                    
                    return -basePenalty;
                    
                case PlantFamily.Standard:
                default:
                    // Standard: nessun bonus/penalità
                    return 0f;
            }
        }
    }
}

