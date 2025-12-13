using UnityEngine;

namespace Sporae.Dome.PotSystem.Level
{
    /// <summary>
    /// Configurazione per il sistema di livelli piante (BLK-02.02).
    /// Contiene soglie cicli per livello e modificatori resa.
    /// </summary>
    [CreateAssetMenu(menuName = "Sporae/PlantLevelConfig", fileName = "PlantLevelConfig")]
    public class PlantLevelConfig : ScriptableObject
    {
        [Header("Soglie Cicli per Livello")]
        [Tooltip("Cicli richiesti per salire di livello. Array 4 elementi: Lvl 1→2, 2→3, 3→4, 4→5")]
        public int[] cyclesThresholds = new int[] { 1, 2, 3, 4 };
        
        [Header("Modificatori Resa per Livello")]
        [Tooltip("Riduzione percentuale quantità frutti per livello oltre 2 (es. Lvl 3: -15%, Lvl 4: -30%, Lvl 5: -45%)")]
        [Range(0f, 100f)]
        public float quantityReductionPerLevel = 15f;  // -15% per livello oltre 2
        
        /// <summary>
        /// Ottiene i cicli richiesti per salire da un livello al successivo
        /// </summary>
        public int GetCyclesRequired(int currentLevel)
        {
            int index = currentLevel - 1; // Lvl 1 → index 0, Lvl 2 → index 1, etc.
            if (index >= 0 && index < cyclesThresholds.Length)
                return cyclesThresholds[index];
            return 999; // Max level raggiunto
        }
        
        /// <summary>
        /// Ottiene il modificatore quantità resa per livello
        /// </summary>
        public float GetQuantityModifier(int level)
        {
            if (level <= 2)
                return 0f; // Nessuna riduzione per Lvl 1-2
            else
                return -quantityReductionPerLevel * (level - 2); // -15% per livello oltre 2
        }
    }
}

