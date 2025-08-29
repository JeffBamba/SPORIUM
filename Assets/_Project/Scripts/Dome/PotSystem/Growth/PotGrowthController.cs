using UnityEngine;
using Sporae.Core;

namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// Controller per la crescita di un singolo vaso
    /// Gestisce la logica di avanzamento stadi e calcolo punti crescita
    /// </summary>
    public class PotGrowthController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PotStateModel potState;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private void Awake()
        {
            // Assicurati che il PotStateModel sia assegnato
            if (potState == null)
            {
                potState = GetComponent<PotStateModel>();
                if (potState == null)
                {
                    Debug.LogError($"[BLK-01.03A] PotGrowthController su {gameObject.name}: PotStateModel non trovato!");
                }
            }
        }

        /// <summary>
        /// Inizializza il vaso quando viene piantato un seme
        /// </summary>
        public void OnPlanted()
        {
            if (potState == null) return;

            potState.HasPlant = true;
            potState.Stage = (int)PlantStage.Seed;
            potState.GrowthPoints = 0;
            potState.DaysSincePlant = 0;
            potState.DaysNeglectedStreak = 0;
            // FIX BLK-01.03A: I flag giornalieri NON vengono mai resettati automaticamente!
            // HydrationConsumedToday e LightExposureToday rimangono attivi per il calcolo crescita

            if (enableDebugLogs)
                Debug.Log($"[BLK-01.03A] {potState.PotId}: Seme piantato, inizializzato come Seed");
        }

        /// <summary>
        /// Applica la crescita giornaliera al vaso
        /// </summary>
        public void ApplyDailyGrowth(PlantGrowthConfig growthConfig)
        {
            if (potState == null) 
            {
                if (enableDebugLogs)
                    Debug.Log($"[BLK-01.03A] {gameObject.name}: potState è NULL!");
                return;
            }

            if (enableDebugLogs)
                Debug.Log($"[BLK-01.03A] {potState.PotId}: ApplyDailyGrowth - HasPlant={potState.HasPlant}, Stage={potState.Stage}");

            if (!potState.HasPlant)
            {
                if (enableDebugLogs)
                    Debug.Log($"[BLK-01.03A] {potState.PotId}: Vaso vuoto, solo decay/reset");
                // Solo decadimento e reset per vasi vuoti
                DecayAndReset(growthConfig);
                return;
            }

            // FIX BLK-01.03A: Calcola punti crescita PRIMA del decay/reset
            // così i flag giornalieri sono ancora attivi
            int addedPoints = CalculateDailyGrowthPoints(growthConfig);
            
            if (enableDebugLogs)
                Debug.Log($"[BLK-01.03A] {potState.PotId}: Punti calcolati: {addedPoints} (H={potState.HydrationConsumedToday}, L={potState.LightExposureToday})");
            
            // Aggiorna contatori
            potState.GrowthPoints += addedPoints;
            potState.DaysSincePlant++;

            if (addedPoints == 0)
            {
                potState.DaysNeglectedStreak++;
                if (enableDebugLogs)
                    Debug.Log($"[BLK-01.03A] {potState.PotId}: Giorno {potState.DaysSincePlant}, nessuna cura - Neglect streak: {potState.DaysNeglectedStreak}");
            }
            else
            {
                potState.DaysNeglectedStreak = 0;
                if (enableDebugLogs)
                    Debug.Log($"[BLK-01.03A] {potState.PotId}: Giorno {potState.DaysSincePlant}, punti aggiunti: {addedPoints}, totali: {potState.GrowthPoints}");
            }

            // Prova ad avanzare di stadio
            TryAdvanceStage(growthConfig);

            // FIX BLK-01.03A: Decadimento e reset DOPO il calcolo punti
            DecayAndReset(growthConfig);

            // Evento crescita giornaliera
            PotEvents.RaiseOnPlantGrew(potState.PotId, (PlantStage)potState.Stage, addedPoints, potState.GrowthPoints);
        }

        /// <summary>
        /// Calcola i punti crescita per questo giorno basati sulla cura
        /// </summary>
        private int CalculateDailyGrowthPoints(PlantGrowthConfig growthConfig)
        {
            bool hasHydration = potState.Hydration > 0;
            bool hasLight = potState.LightExposure > 0;
            bool consumedHydration = potState.HydrationConsumedToday;
            bool consumedLight = potState.LightExposureToday;

            // Cura ideale: acqua + luce disponibili E consumate oggi
            if (hasHydration && hasLight && consumedHydration && consumedLight)
            {
                return growthConfig.pointsIdealCare;
            }
            
            // Cura parziale: uno dei due disponibile e consumato
            if ((hasHydration && consumedHydration) || (hasLight && consumedLight))
            {
                return growthConfig.pointsPartialCare;
            }

            // Nessuna cura
            return growthConfig.pointsNoCare;
        }

        /// <summary>
        /// Prova ad avanzare di stadio se i punti sono sufficienti
        /// </summary>
        private void TryAdvanceStage(PlantGrowthConfig growthConfig)
        {
            PlantStage currentStage = (PlantStage)potState.Stage;
            PlantStage newStage = currentStage;

            switch (currentStage)
            {
                case PlantStage.Seed:
                    if (potState.GrowthPoints >= growthConfig.pointsSeedToSprout)
                    {
                        newStage = PlantStage.Sprout;
                        potState.GrowthPoints = 0;
                        if (enableDebugLogs)
                            Debug.Log($"[BLK-01.03A] {potState.PotId}: Avanzamento Seed → Sprout!");
                    }
                    break;

                case PlantStage.Sprout:
                    if (potState.GrowthPoints >= growthConfig.pointsSproutToMature)
                    {
                        newStage = PlantStage.Mature;
                        potState.GrowthPoints = 0;
                        if (enableDebugLogs)
                            Debug.Log($"[BLK-01.03A] {potState.PotId}: Avanzamento Sprout → Mature!");
                    }
                    break;

                case PlantStage.Mature:
                    // Già maturo, nessun avanzamento
                    break;
            }

            // Aggiorna stadio se cambiato
            if (newStage != currentStage)
            {
                potState.Stage = (int)newStage;
                PotEvents.RaiseOnPlantStageChanged(potState.PotId, newStage);
                
                if (enableDebugLogs)
                    Debug.Log($"[BLK-01.03A] {potState.PotId}: Nuovo stadio: {newStage}");
            }
        }

        /// <summary>
        /// Applica decadimento (SENZA reset flag giornalieri!)
        /// </summary>
        private void DecayAndReset(PlantGrowthConfig growthConfig)
        {
            // Decadimento idratazione
            potState.Hydration = Mathf.Max(0, potState.Hydration - growthConfig.dailyHydrationDecay);
            
            // Reset esposizione luce
            potState.LightExposure = 0;

            // FIX BLK-01.03A: I flag giornalieri NON vengono mai resettati automaticamente!
            // HydrationConsumedToday e LightExposureToday rimangono attivi per il calcolo crescita

            if (enableDebugLogs && potState.HasPlant)
            {
                Debug.Log($"[BLK-01.03A] {potState.PotId}: Decay applicato - Hydration: {potState.Hydration}, Light: {potState.LightExposure}");
            }
        }

        /// <summary>
        /// Imposta il PotStateModel (per setup runtime)
        /// </summary>
        public void SetPotState(PotStateModel state)
        {
            potState = state;
        }

        /// <summary>
        /// Ottiene il PotStateModel corrente
        /// </summary>
        public PotStateModel GetPotState()
        {
            return potState;
        }
    }
}
