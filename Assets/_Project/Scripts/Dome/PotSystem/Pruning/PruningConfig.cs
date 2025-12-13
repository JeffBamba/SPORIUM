using UnityEngine;
using Sporae.Dome.PotSystem.Growth;

namespace Sporae.Dome.PotSystem.Pruning
{
    /// <summary>
    /// Configurazione per il sistema di potatura (AZ-13).
    /// Contiene probabilità di successo per stadio e bonus Spray.
    /// </summary>
    [CreateAssetMenu(menuName = "Sporae/PruningConfig", fileName = "PruningConfig")]
    public class PruningConfig : ScriptableObject
    {
        [Header("Probabilità Base per Stadio")]
        [Tooltip("Probabilità base di successo per ogni stadio (0-100%). Ordine: Seed, Sprout, Growth, Flowering, HarvestReady, Resting")]
        [Range(0f, 100f)]
        public float[] baseSuccessRateByStage = new float[]
        {
            10f,  // Seed
            15f,  // Sprout
            80f,  // Growth
            10f,  // Flowering
            12f,  // HarvestReady
            10f   // Resting
        };
        
        [Header("Bonus Spray per Stadio")]
        [Tooltip("Bonus percentuale aggiunto con Spray Antifungino per ogni stadio (0-100%). Ordine: Seed, Sprout, Growth, Flowering, HarvestReady, Resting")]
        [Range(0f, 100f)]
        public float[] sprayBonusByStage = new float[]
        {
            15f,  // Seed
            15f,  // Sprout
            10f,  // Growth
            10f,  // Flowering
            5f,   // HarvestReady
            10f   // Resting
        };
        
        [Header("Bonus Resa")]
        [Tooltip("Se true, applica +10% quantità. Se false, applica +1 frutto")]
        public bool usePercentageBonus = false;
        
        [Header("Costo")]
        [Tooltip("Costo in azioni per eseguire potatura")]
        public int actionCost = 1;
        
        /// <summary>
        /// Ottiene la probabilità base di successo per uno stadio
        /// </summary>
        public float GetBaseSuccessRate(PlantStage stage)
        {
            int index = (int)stage - 1; // Seed = 1 -> index 0, Sprout = 2 -> index 1, etc.
            if (index >= 0 && index < baseSuccessRateByStage.Length)
                return baseSuccessRateByStage[index];
            return 10f; // Default
        }
        
        /// <summary>
        /// Ottiene il bonus Spray per uno stadio
        /// </summary>
        public float GetSprayBonus(PlantStage stage)
        {
            int index = (int)stage - 1;
            if (index >= 0 && index < sprayBonusByStage.Length)
                return sprayBonusByStage[index];
            return 0f; // Default
        }
    }
}

