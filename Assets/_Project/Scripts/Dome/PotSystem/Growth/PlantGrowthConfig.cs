using UnityEngine;

namespace Sporae.Dome.PotSystem.Growth
{
    [CreateAssetMenu(menuName = "Sporae/PlantGrowthConfig")]
    public class PlantGrowthConfig : ScriptableObject
    {
        [Header("Soglie Avanzamento")]
        [Tooltip("Punti richiesti per passare da Seed a Sprout (~giorno 1-2 con cura ideale)")]
        public int pointsSeedToSprout = 2;
        
        [Tooltip("Punti richiesti per passare da Sprout a Mature (~giorni 3-5 con cura ideale)")]
        public int pointsSproutToMature = 3;

        [Header("Punti Giornalieri")]
        [Tooltip("Punti per cura ideale (acqua + luce nello stesso giorno)")]
        public int pointsIdealCare = 2;
        
        [Tooltip("Punti per cura parziale (solo acqua o solo luce)")]
        public int pointsPartialCare = 1;
        
        [Tooltip("Punti per nessuna cura")]
        public int pointsNoCare = 0;

        [Header("Penalità e Decadimenti")]
        [Tooltip("Soglia per feature future (muffe, ecc.)")]
        public int neglectThreshold = 2;
        
        [Tooltip("L'acqua scende di questo valore a fine giorno")]
        public int dailyHydrationDecay = 1;
        
        [Tooltip("DEPRECATO - I flag giornalieri sono stati sostituiti da timestamp (BLK-01.03A)")]
        [System.Obsolete("Sostituito da sistema timestamp in BLK-01.03A")]
        public bool resetDailyExposureFlags = false;

        [Header("Futuro (hook pH Dome)")]
        [Tooltip("Moltiplicatore per integrazione futura pH")]
        public float phGrowthMultiplier = 1.0f;

        private void OnValidate()
        {
            // Validazione parametri
            pointsSeedToSprout = Mathf.Max(1, pointsSeedToSprout);
            pointsSproutToMature = Mathf.Max(1, pointsSproutToMature);
            pointsIdealCare = Mathf.Max(0, pointsIdealCare);
            pointsPartialCare = Mathf.Max(0, pointsPartialCare);
            pointsNoCare = Mathf.Max(0, pointsNoCare);
            neglectThreshold = Mathf.Max(1, neglectThreshold);
            dailyHydrationDecay = Mathf.Max(0, dailyHydrationDecay);
            phGrowthMultiplier = Mathf.Max(0.1f, phGrowthMultiplier);
        }
    }
}
