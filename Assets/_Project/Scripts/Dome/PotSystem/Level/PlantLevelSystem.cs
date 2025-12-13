using UnityEngine;
using Sporae.Dome.PotSystem.Level;
using Sporae.DevTools;

namespace Sporae.Dome.PotSystem.Level
{
    /// <summary>
    /// Sistema di livelli piante (BLK-02.02).
    /// Gestisce progressione livelli basata su cicli completati e modificatori resa.
    /// </summary>
    public static class PlantLevelSystem
    {
        /// <summary>
        /// Verifica e applica salita livello se necessario
        /// </summary>
        /// <param name="potState">Stato del vaso</param>
        /// <param name="config">Configurazione livelli</param>
        /// <returns>True se livello aumentato</returns>
        public static bool CheckLevelUp(PotStateModel potState, PlantLevelConfig config)
        {
            if (potState == null || config == null)
                return false;
            
            // Max level è 5
            if (potState.PlantLevel >= 5)
                return false;
            
            // Ottieni cicli richiesti per livello successivo
            int cyclesRequired = config.GetCyclesRequired(potState.PlantLevel);
            
            // Verifica se può salire
            if (potState.CompletedCycles >= cyclesRequired)
            {
                potState.PlantLevel++;
                SporiumLogger.LogInfo(LogCategory.Pot, $"{potState.PotId}: Livello aumentato a Lvl {potState.PlantLevel} (cicli completati: {potState.CompletedCycles})");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Calcola progress percentuale verso livello successivo
        /// </summary>
        public static float GetLevelProgress(PotStateModel potState, PlantLevelConfig config)
        {
            if (potState == null || config == null)
                return 0f;
            
            // Max level raggiunto
            if (potState.PlantLevel >= 5)
                return 1f;
            
            int cyclesRequired = config.GetCyclesRequired(potState.PlantLevel);
            if (cyclesRequired <= 0)
                return 1f;
            
            return Mathf.Clamp01((float)potState.CompletedCycles / cyclesRequired);
        }
        
        /// <summary>
        /// Riduce livello (per effetti infestazione)
        /// </summary>
        /// <param name="potState">Stato del vaso</param>
        /// <param name="amount">Quantità di livelli da ridurre</param>
        public static void ReduceLevel(PotStateModel potState, int amount)
        {
            if (potState == null)
                return;
            
            int newLevel = Mathf.Max(1, potState.PlantLevel - amount); // Clamp minimo a 1
            int levelsLost = potState.PlantLevel - newLevel;
            
            if (levelsLost > 0)
            {
                potState.PlantLevel = newLevel;
                SporiumLogger.LogWarning(LogCategory.Pot, $"{potState.PotId}: Livello ridotto di {levelsLost} (Lvl {potState.PlantLevel + levelsLost} → Lvl {potState.PlantLevel})");
            }
        }
    }
}

