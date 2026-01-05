using UnityEngine;

namespace Sporae.DevTools
{
    /// <summary>
    /// Configurazione runtime per calibrazione difficoltà DOME.
    /// Tutti i parametri sono modificabili in runtime durante il gioco.
    /// </summary>
    public static class DifficultyCalibrationConfig
    {
        // ============================================
        // OVERWATERING SYSTEM
        // ============================================
        public static float OverwateringThresholdPercent = 75f;  // default 75%
        public static float OverwateringRemovalPercent = 50f;   // default 50%
        public static float OverwateringPhDrift = -5f;         // default -5
        public static float WateringAccumulator = 0.5f;        // default 0.5
        
        // ============================================
        // MALUS CRESCITA (PlantConditionSystem)
        // ============================================
        public static int BaseScore = 50;                      // default 50
        public static int MalusHydrationOutOfRange = 9;        // default 9 (ridotto del 40% da 15)
        public static int MalusLightWrongOrAbsent = 6;         // default 6 (ridotto del 40% da 10)
        public static int MalusPhOpposite = 12;                // default 12 (ridotto del 40% da 20)
        public static int MalusPhUltra = 9;                    // default 9 (ridotto del 40% da 15)
        public static int MalusPhOutOfRangeMin = 5;           // default 5 - Malus minimo fuori range (mantenuto)
        public static int MalusPhOutOfRangeMax = 21;           // default 21 (ridotto del 40% da 35) - Malus massimo fuori range
        public static int MalusPhExtreme = 30;                  // default 30 (ridotto del 40% da 50) - Malus per pH estremi (≥+80 o ≤-80)
        public static int MalusOverwatering = 15;              // default 15 (ridotto del 40% da 25)
        public static int MalusMoldMild = 10;                  // default 10 (mantenuto)
        public static int MalusMoldSevere = 0;                 // default 0 (rimosso dal calcolo, già blocca l'avanzamento - mantenuto per retrocompatibilità)
        public static int MalusBurnStress = 12;                // default 12 (ridotto del 40% da 20)
        public static int MalusLedIncompatiblePerDay = -5;     // BLK-02.08: default -5 per ogni giorno che LED sbagliato è acceso (mantenuto)
        
        // ============================================
        // BONUS CRESCITA (PlantConditionSystem)
        // ============================================
        public static int BonusHydrationOptimal = 20;          // default 20
        public static int BonusLightCorrect = 15;              // default 15
        public static int BonusWateringOn = 10;                // default 10
        public static int BonusPhOptimal = 10;                 // default 10
        public static int BonusPhOptimalGradual = 5;            // default 5 - Bonus extra se pH molto vicino al centro range
        public static int BonusNoMold = 5;                     // default 5
        
        // ============================================
        // SISTEMA pH - BANDE (PhSystem)
        // ============================================
        public static float PhThresholdUltraAcid = -80f;       // default -80
        public static float PhThresholdStableAcid = -30f;      // default -30
        public static float PhThresholdStableBasic = 30f;      // default +30
        public static float PhThresholdUltraBasic = 80f;       // default +80
        
        // ============================================
        // pH DRIFT DA AZIONI (DayCycleController)
        // ============================================
        public static float PhDriftOverwatering = -5f;         // default -5
        public static float PhDriftLedBlue = 5f;                // default +5
        public static float PhDriftLedRed = -5f;               // default -5
        public static float PhDriftSpray = 5f;                 // default +5
        
        // ============================================
        // SISTEMA LED - MOLTIPLICATORI (DayCycleController)
        // ============================================
        public static float LedMultiplierDay1 = 1.0f;          // default 1.0
        public static float LedMultiplierDays2_3 = 1.5f;       // default 1.5
        public static float LedMultiplierDay4Plus = 2.0f;      // default 2.0
        public static float LedMalusBase = 1.0f;               // default 1.0 (≤3 giorni)
        public static float LedMalusGrowth = 1.5f;             // default 1.5 (≥4 giorni)
        public static float LedMalusIncrementPerDay = 0.2f;    // default 0.2
        
        // ============================================
        // SISTEMA CRESCITA (PlantGrowthConfig)
        // ============================================
        public static int PointsSeedToSprout = 4;              // default 4
        public static int PointsSproutToMature = 4;            // default 4
        public static int PointsIdealCare = 2;                 // default 2
        public static int PointsPartialCare = 1;               // default 1
        public static int PointsNoCare = 0;                    // default 0
        public static int DailyHydrationDecay = 1;             // default 1
        public static int NeglectThreshold = 2;                // default 2
        public static float PhGrowthMultiplier = 1.0f;         // default 1.0
        
        // ============================================
        // MOLTIPLICATORI CRESCITA PER STADIO (PotGrowthController)
        // ============================================
        public static float GrowthMultiplierEmpty = 1.00f;      // default 1.00
        public static float GrowthMultiplierSeed = 1.05f;      // default 1.05
        public static float GrowthMultiplierSprout = 1.12f;    // default 1.12
        public static float GrowthMultiplierHarvestReady = 1.20f; // default 1.20
        public static float GrowthMultiplierResting = 1.20f;   // default 1.20
        
        // ============================================
        // SISTEMA FERTILIZZANTE (FertilizerSystem)
        // ============================================
        public static int FertilizerStandardAmount = 25;       // default 25%
        public static int FertilizerPureAmount = 40;           // default 40%
        public static int FertilizerProhibitedAmount = 40;     // default 40%
        public static int FertilizerStandardCost = 25;         // default 25 CRY
        public static int FertilizerPureCost = 75;             // default 75 CRY
        public static int FertilizerProhibitedCost = 75;       // default 75 CRY
        public static float FertilizerDecayRate = 5f;          // default 5%
        
        // ============================================
        // SISTEMA MUFFE (MoldConfig)
        // ============================================
        public static int MoldMildThreshold = 1;               // default 1
        public static int MoldSevereThreshold = 2;             // default 2
        public static int MoldCriticalThreshold = 3;           // default 3
        public static int MoldOverwateringDaysThreshold = 3;    // default 3
        public static float MoldAcidicPhThreshold = -20f;       // default -20
        public static float MoldPruningNeglectAccumulation = 0.5f; // default 0.5
        public static int MoldMildScorePenalty = 10;           // default 10
        public static int MoldSevereScorePenalty = 30;         // default 30
        public static int MoldMildLevelReduction = 1;           // default 1
        public static int MoldSevereLevelReduction = 3;         // default 3
        
        // ============================================
        // SOGLIE CONDIZIONE (PlantConditionSystem)
        // ============================================
        public static int ConditionThresholdRigogliosa = 80;   // default 80 (ridotto da 90)
        public static int ConditionThresholdSana = 40;          // default 40 (ridotto da 70) - ora copre 40-80
        public static int ConditionThresholdStressata = 40;     // default 40 (mantenuto per retrocompatibilità, ma non più usato nella logica)
        public static int ConditionThresholdAppassita = 20;    // default 20 (invariato)
        public static int ForecastDeltaUp = 5;                 // default >5
        public static int ForecastDeltaDown = -5;              // default <-5
        
        // ============================================
        // RANGE IDRATAZIONE FUORI RANGE (PlantConditionSystem)
        // ============================================
        public static int HydrationDryThreshold = 25;          // default <25%
        public static int HydrationWetThreshold = 90;          // default >90%
        
        // ============================================
        // METODI UTILITY
        // ============================================
        
        /// <summary>
        /// Resetta tutti i parametri ai valori di default
        /// </summary>
        public static void ResetToDefaults()
        {
            // OVERWATERING
            OverwateringThresholdPercent = 75f;
            OverwateringRemovalPercent = 50f;
            OverwateringPhDrift = -5f;
            WateringAccumulator = 0.5f;
            
            // MALUS
            BaseScore = 50;
            MalusHydrationOutOfRange = 9;      // Ridotto del 40% da 15
            MalusLightWrongOrAbsent = 6;       // Ridotto del 40% da 10
            MalusPhOpposite = 12;              // Ridotto del 40% da 20
            MalusPhUltra = 9;                  // Ridotto del 40% da 15
            MalusPhOutOfRangeMin = 5;          // Mantenuto
            MalusPhOutOfRangeMax = 21;         // Ridotto del 40% da 35
            MalusPhExtreme = 30;               // Ridotto del 40% da 50
            MalusOverwatering = 15;            // Ridotto del 40% da 25
            MalusMoldMild = 10;                // Mantenuto
            MalusMoldSevere = 0;               // Rimosso dal calcolo (già blocca l'avanzamento), mantenuto per retrocompatibilità
            MalusBurnStress = 12;              // Ridotto del 40% da 20
            MalusLedIncompatiblePerDay = -5;   // Mantenuto
            
            // BONUS
            BonusHydrationOptimal = 20;
            BonusLightCorrect = 15;
            BonusWateringOn = 10;
            BonusPhOptimal = 10;
            BonusPhOptimalGradual = 5;
            BonusNoMold = 5;
            
            // pH BANDE
            PhThresholdUltraAcid = -80f;
            PhThresholdStableAcid = -30f;
            PhThresholdStableBasic = 30f;
            PhThresholdUltraBasic = 80f;
            
            // pH DRIFT AZIONI
            PhDriftOverwatering = -5f;
            PhDriftLedBlue = 5f;
            PhDriftLedRed = -5f;
            PhDriftSpray = 5f;
            
            // LED
            LedMultiplierDay1 = 1.0f;
            LedMultiplierDays2_3 = 1.5f;
            LedMultiplierDay4Plus = 2.0f;
            LedMalusBase = 1.0f;
            LedMalusGrowth = 1.5f;
            LedMalusIncrementPerDay = 0.2f;
            
            // CRESCITA
            PointsSeedToSprout = 4;
            PointsSproutToMature = 4;
            PointsIdealCare = 2;
            PointsPartialCare = 1;
            PointsNoCare = 0;
            DailyHydrationDecay = 1;
            NeglectThreshold = 2;
            PhGrowthMultiplier = 1.0f;
            
            // MOLTIPLICATORI STADIO
            GrowthMultiplierEmpty = 1.00f;
            GrowthMultiplierSeed = 1.05f;
            GrowthMultiplierSprout = 1.12f;
            GrowthMultiplierHarvestReady = 1.20f;
            GrowthMultiplierResting = 1.20f;
            
            // FERTILIZZANTE
            FertilizerStandardAmount = 25;
            FertilizerPureAmount = 40;
            FertilizerProhibitedAmount = 40;
            FertilizerStandardCost = 25;
            FertilizerPureCost = 75;
            FertilizerProhibitedCost = 75;
            FertilizerDecayRate = 5f;
            
            // MUFFE
            MoldMildThreshold = 1;
            MoldSevereThreshold = 2;
            MoldCriticalThreshold = 3;
            MoldOverwateringDaysThreshold = 3;
            MoldAcidicPhThreshold = -20f;
            MoldPruningNeglectAccumulation = 0.5f;
            MoldMildScorePenalty = 10;
            MoldSevereScorePenalty = 30;
            MoldMildLevelReduction = 1;
            MoldSevereLevelReduction = 3;
            
            // CONDIZIONE
            ConditionThresholdRigogliosa = 80;    // Ridotto da 90
            ConditionThresholdSana = 40;            // Ridotto da 70 (ora copre 40-80)
            ConditionThresholdStressata = 40;       // Mantenuto per retrocompatibilità (non più usato nella logica)
            ConditionThresholdAppassita = 20;       // Invariato
            ForecastDeltaUp = 5;
            ForecastDeltaDown = -5;
            
            // RANGE IDRATAZIONE
            HydrationDryThreshold = 25;
            HydrationWetThreshold = 90;
        }
        
        /// <summary>
        /// Ottiene un dizionario con tutti i parametri e i loro valori default per export
        /// </summary>
        public static System.Collections.Generic.Dictionary<string, object> GetAllParameters()
        {
            return new System.Collections.Generic.Dictionary<string, object>
            {
                // OVERWATERING
                { "OverwateringThresholdPercent", OverwateringThresholdPercent },
                { "OverwateringRemovalPercent", OverwateringRemovalPercent },
                { "OverwateringPhDrift", OverwateringPhDrift },
                { "WateringAccumulator", WateringAccumulator },
                
                // MALUS
                { "BaseScore", BaseScore },
                { "MalusHydrationOutOfRange", MalusHydrationOutOfRange },
                { "MalusLightWrongOrAbsent", MalusLightWrongOrAbsent },
                { "MalusPhOpposite", MalusPhOpposite },
                { "MalusPhUltra", MalusPhUltra },
                { "MalusPhOutOfRangeMin", MalusPhOutOfRangeMin },
                { "MalusPhOutOfRangeMax", MalusPhOutOfRangeMax },
                { "MalusPhExtreme", MalusPhExtreme },
                { "MalusOverwatering", MalusOverwatering },
                { "MalusMoldMild", MalusMoldMild },
                { "MalusMoldSevere", MalusMoldSevere },  // Mantenuto per retrocompatibilità (sempre 0, non più usato nel calcolo)
                { "MalusBurnStress", MalusBurnStress },
                { "MalusLedIncompatiblePerDay", MalusLedIncompatiblePerDay },
                
                // BONUS
                { "BonusHydrationOptimal", BonusHydrationOptimal },
                { "BonusLightCorrect", BonusLightCorrect },
                { "BonusWateringOn", BonusWateringOn },
                { "BonusPhOptimal", BonusPhOptimal },
                { "BonusPhOptimalGradual", BonusPhOptimalGradual },
                { "BonusNoMold", BonusNoMold },
                
                // pH BANDE
                { "PhThresholdUltraAcid", PhThresholdUltraAcid },
                { "PhThresholdStableAcid", PhThresholdStableAcid },
                { "PhThresholdStableBasic", PhThresholdStableBasic },
                { "PhThresholdUltraBasic", PhThresholdUltraBasic },
                
                // pH DRIFT
                { "PhDriftOverwatering", PhDriftOverwatering },
                { "PhDriftLedBlue", PhDriftLedBlue },
                { "PhDriftLedRed", PhDriftLedRed },
                { "PhDriftSpray", PhDriftSpray },
                
                // LED
                { "LedMultiplierDay1", LedMultiplierDay1 },
                { "LedMultiplierDays2_3", LedMultiplierDays2_3 },
                { "LedMultiplierDay4Plus", LedMultiplierDay4Plus },
                { "LedMalusBase", LedMalusBase },
                { "LedMalusGrowth", LedMalusGrowth },
                { "LedMalusIncrementPerDay", LedMalusIncrementPerDay },
                
                // CRESCITA
                { "PointsSeedToSprout", PointsSeedToSprout },
                { "PointsSproutToMature", PointsSproutToMature },
                { "PointsIdealCare", PointsIdealCare },
                { "PointsPartialCare", PointsPartialCare },
                { "PointsNoCare", PointsNoCare },
                { "DailyHydrationDecay", DailyHydrationDecay },
                { "NeglectThreshold", NeglectThreshold },
                { "PhGrowthMultiplier", PhGrowthMultiplier },
                
                // MOLTIPLICATORI STADIO
                { "GrowthMultiplierEmpty", GrowthMultiplierEmpty },
                { "GrowthMultiplierSeed", GrowthMultiplierSeed },
                { "GrowthMultiplierSprout", GrowthMultiplierSprout },
                { "GrowthMultiplierHarvestReady", GrowthMultiplierHarvestReady },
                { "GrowthMultiplierResting", GrowthMultiplierResting },
                
                // FERTILIZZANTE
                { "FertilizerStandardAmount", FertilizerStandardAmount },
                { "FertilizerPureAmount", FertilizerPureAmount },
                { "FertilizerProhibitedAmount", FertilizerProhibitedAmount },
                { "FertilizerStandardCost", FertilizerStandardCost },
                { "FertilizerPureCost", FertilizerPureCost },
                { "FertilizerProhibitedCost", FertilizerProhibitedCost },
                { "FertilizerDecayRate", FertilizerDecayRate },
                
                // MUFFE
                { "MoldMildThreshold", MoldMildThreshold },
                { "MoldSevereThreshold", MoldSevereThreshold },
                { "MoldCriticalThreshold", MoldCriticalThreshold },
                { "MoldOverwateringDaysThreshold", MoldOverwateringDaysThreshold },
                { "MoldAcidicPhThreshold", MoldAcidicPhThreshold },
                { "MoldPruningNeglectAccumulation", MoldPruningNeglectAccumulation },
                { "MoldMildScorePenalty", MoldMildScorePenalty },
                { "MoldSevereScorePenalty", MoldSevereScorePenalty },
                { "MoldMildLevelReduction", MoldMildLevelReduction },
                { "MoldSevereLevelReduction", MoldSevereLevelReduction },
                
                // CONDIZIONE
                { "ConditionThresholdRigogliosa", ConditionThresholdRigogliosa },
                { "ConditionThresholdSana", ConditionThresholdSana },
                { "ConditionThresholdStressata", ConditionThresholdStressata },
                { "ConditionThresholdAppassita", ConditionThresholdAppassita },
                { "ForecastDeltaUp", ForecastDeltaUp },
                { "ForecastDeltaDown", ForecastDeltaDown },
                
                // RANGE IDRATAZIONE
                { "HydrationDryThreshold", HydrationDryThreshold },
                { "HydrationWetThreshold", HydrationWetThreshold }
            };
        }
    }
}

