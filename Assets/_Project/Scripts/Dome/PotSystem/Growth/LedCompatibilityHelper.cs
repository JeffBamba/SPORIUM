namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// BLK-02.08: Enum per compatibilità LED con famiglia pianta
    /// </summary>
    public enum LedCompatibility
    {
        BlueOnly,    // Pure
        RedOnly,     // Evil
        Both         // Standard (e Pure+Evil ibrido)
    }
    
    /// <summary>
    /// BLK-02.08: Helper per determinare compatibilità LED con famiglie di piante
    /// </summary>
    public static class LedCompatibilityHelper
    {
        /// <summary>
        /// Ottiene i tipi LED compatibili per una famiglia di pianta
        /// </summary>
        public static LedCompatibility GetCompatibleLedTypes(PlantFamily family)
        {
            return family switch
            {
                PlantFamily.Pure => LedCompatibility.BlueOnly,
                PlantFamily.Evil => LedCompatibility.RedOnly,
                PlantFamily.Standard => LedCompatibility.Both,
                _ => LedCompatibility.Both
            };
        }
        
        /// <summary>
        /// BLK-02.08 (Q1 2026): Ottiene i tipi LED compatibili per un ibrido (placeholder per futuro)
        /// </summary>
        /// <param name="parent1">Famiglia primo genitore</param>
        /// <param name="parent2">Famiglia secondo genitore</param>
        /// <returns>Compatibilità LED per l'ibrido</returns>
        public static LedCompatibility GetCompatibleLedTypesHybrid(PlantFamily parent1, PlantFamily parent2)
        {
            // Pure + Evil → Both (neutro)
            if ((parent1 == PlantFamily.Pure && parent2 == PlantFamily.Evil) ||
                (parent1 == PlantFamily.Evil && parent2 == PlantFamily.Pure))
                return LedCompatibility.Both;
            
            // Pure + Standard → Blue
            if ((parent1 == PlantFamily.Pure && parent2 == PlantFamily.Standard) ||
                (parent1 == PlantFamily.Standard && parent2 == PlantFamily.Pure))
                return LedCompatibility.BlueOnly;
            
            // Evil + Standard → Red
            if ((parent1 == PlantFamily.Evil && parent2 == PlantFamily.Standard) ||
                (parent1 == PlantFamily.Standard && parent2 == PlantFamily.Evil))
                return LedCompatibility.RedOnly;
            
            return LedCompatibility.Both;
        }
        
        /// <summary>
        /// Verifica se uno stato LED è compatibile con la compatibilità specificata
        /// </summary>
        public static bool IsLedCompatible(LedSystemState ledState, LedCompatibility compatibility)
        {
            if (ledState == LedSystemState.Off)
                return true; // Off è sempre compatibile
            
            return compatibility switch
            {
                LedCompatibility.BlueOnly => ledState == LedSystemState.Blue,
                LedCompatibility.RedOnly => ledState == LedSystemState.Red,
                LedCompatibility.Both => true,
                _ => true
            };
        }
        
        /// <summary>
        /// Ottiene la stringa display per compatibilità LED (per UI)
        /// </summary>
        public static string GetCompatibleLedDisplay(LedCompatibility compatibility)
        {
            return compatibility switch
            {
                LedCompatibility.BlueOnly => "Blue",
                LedCompatibility.RedOnly => "Red",
                LedCompatibility.Both => "ALL",
                _ => "ALL"
            };
        }
    }
}

