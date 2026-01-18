using Sporae.Dome.PotSystem.Condition;

namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// BLK-03.01-T2: Modificatore crescita basato sulla condizione della pianta
    /// </summary>
    public static class ConditionGrowthModifier
    {
        /// <summary>
        /// Calcola modificatore giorni in base alla condizione
        /// </summary>
        /// <param name="condition">Condizione corrente della pianta</param>
        /// <returns>Modificatore giorni (es. -1 per Rigogliosa = guadagna 1 giorno)</returns>
        public static int GetDaysModifier(PlantCondition condition)
        {
            return condition switch
            {
                PlantCondition.Rigogliosa => -1,  // -1 giorno (guadagna 1 giorno)
                PlantCondition.Sana => 0,         // Nessun modificatore
                PlantCondition.Stressata => 0,     // Nessun modificatore
                PlantCondition.Appassita => 0,    // Nessun modificatore (ma blocca avanzamento)
                PlantCondition.Critica => 0,      // Nessun modificatore (ma blocca avanzamento)
                _ => 0
            };
        }
        
        /// <summary>
        /// Verifica se la condizione blocca l'avanzamento
        /// </summary>
        /// <param name="condition">Condizione corrente della pianta</param>
        /// <returns>True se l'avanzamento è bloccato</returns>
        public static bool BlocksAdvancement(PlantCondition condition)
        {
            return condition == PlantCondition.Critica || 
                   condition == PlantCondition.Appassita ||
                   condition == PlantCondition.Morta;
        }
        
        /// <summary>
        /// Calcola moltiplicatore velocità crescita basato sulla condizione
        /// </summary>
        /// <param name="condition">Condizione corrente della pianta</param>
        /// <returns>Moltiplicatore crescita (es. 1.2f = +20%)</returns>
        public static float GetGrowthSpeedMultiplier(PlantCondition condition)
        {
            return condition switch
            {
                PlantCondition.Rigogliosa => 1.2f,  // +20% crescita
                PlantCondition.Sana => 1.0f,         // 0% (normale)
                PlantCondition.Stressata => 0.9f,     // -10% crescita
                PlantCondition.Appassita => 0.7f,     // -30% crescita
                PlantCondition.Critica => 1.0f,      // 0% (ma blocca avanzamento)
                PlantCondition.Morta => 0f,          // 0% (pianta morta)
                _ => 1.0f
            };
        }
        
        /// <summary>
        /// Calcola moltiplicatore produzione frutti basato sulla condizione
        /// </summary>
        /// <param name="condition">Condizione corrente della pianta</param>
        /// <returns>Moltiplicatore produzione (es. 1.15f = +15%)</returns>
        public static float GetProductionMultiplier(PlantCondition condition)
        {
            return condition switch
            {
                PlantCondition.Rigogliosa => 1.15f,  // +15% produzione
                PlantCondition.Sana => 1.0f,          // 0% (normale)
                PlantCondition.Stressata => 0.85f,     // -15% produzione
                PlantCondition.Appassita => 1.0f,     // 0% (ma blocca avanzamento)
                PlantCondition.Critica => 1.0f,      // 0% (ma blocca avanzamento)
                PlantCondition.Morta => 0f,          // 0% (pianta morta)
                _ => 1.0f
            };
        }
    }
}

