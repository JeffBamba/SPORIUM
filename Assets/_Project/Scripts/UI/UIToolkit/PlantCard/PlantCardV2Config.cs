using UnityEngine;

namespace Sporae.UI.UIToolkit.PlantCard
{
    /// <summary>
    /// ScriptableObject per configurazione UI PlantCard V2.0.
    /// Contiene palette colori, thresholds per colori dinamici, e formattazione.
    /// Facilmente modificabile in Inspector senza toccare codice.
    /// </summary>
    [CreateAssetMenu(fileName = "PlantCardV2Config", menuName = "Sporae/UI/PlantCardV2Config")]
    public class PlantCardV2Config : ScriptableObject
    {
        [Header("Palette Colori")]
        [Tooltip("Verde LED - stabilità, genetica, OK")]
        public Color GreenLed = new Color(127f / 255f, 255f / 255f, 122f / 255f);
        
        [Tooltip("Blu - informazioni, acqua, neutro")]
        public Color BlueInfo = new Color(93f / 255f, 182f / 255f, 227f / 255f);
        
        [Tooltip("Rosso - warning, critico")]
        public Color RedWarning = new Color(211f / 255f, 95f / 255f, 95f / 255f);
        
        [Tooltip("Giallo - standard, notifiche")]
        public Color YellowStandard = new Color(230f / 255f, 201f / 255f, 111f / 255f);
        
        [Tooltip("Violetto - growth stage, manual systems")]
        public Color VioletGrowth = new Color(181f / 255f, 128f / 255f, 209f / 255f);
        
        [Tooltip("Grigio chiaro - testo secondario")]
        public Color GrayText = new Color(192f / 255f, 200f / 255f, 197f / 255f);
        
        [Tooltip("Background principale")]
        public Color BgDark = new Color(10f / 255f, 18f / 255f, 22f / 255f);
        
        [Tooltip("Background più scuro")]
        public Color BgDarker = new Color(15f / 255f, 20f / 255f, 22f / 255f);
        
        [Tooltip("Metallico chiaro - bordi pannelli")]
        public Color MetalLight = new Color(58f / 255f, 74f / 255f, 79f / 255f);
        
        [Tooltip("Metallico scuro - pannelli solidi")]
        public Color MetalDark = new Color(26f / 255f, 35f / 255f, 37f / 255f);
        
        [Header("Thresholds - Fertilizer Level")]
        [Tooltip("Range ottimale per colore verde (min)")]
        [Range(0, 100)]
        public int FertilizerOptimalMin = 60;
        
        [Tooltip("Range ottimale per colore verde (max)")]
        [Range(0, 100)]
        public int FertilizerOptimalMax = 90;
        
        [Tooltip("Range warning per colore giallo (min)")]
        [Range(0, 100)]
        public int FertilizerWarningMin = 50;
        
        [Tooltip("Range warning per colore giallo (max)")]
        [Range(0, 100)]
        public int FertilizerWarningMax = 100;
        
        [Header("Thresholds - Condition Score")]
        [Tooltip("Range ottimale per colore verde (min)")]
        [Range(0, 100)]
        public int ConditionOptimalMin = 70;
        
        [Tooltip("Range ottimale per colore verde (max)")]
        [Range(0, 100)]
        public int ConditionOptimalMax = 100;
        
        [Tooltip("Range warning per colore giallo (min)")]
        [Range(0, 100)]
        public int ConditionWarningMin = 60;
        
        [Tooltip("Range warning per colore giallo (max)")]
        [Range(0, 100)]
        public int ConditionWarningMax = 70;
        
        [Header("Thresholds - Mold Risk")]
        [Tooltip("Livello per badge LOW (0-1)")]
        public int MoldRiskLow = 1;
        
        [Tooltip("Livello per badge MEDIUM (2)")]
        public int MoldRiskMedium = 2;
        
        [Tooltip("Livello per badge HIGH (3)")]
        public int MoldRiskHigh = 3;
        
        [Header("Formattazione")]
        [Tooltip("Font size per valori grandi (vital parameters)")]
        public int FontSizeLarge = 36;
        
        [Tooltip("Font size per valori medi")]
        public int FontSizeMedium = 24;
        
        [Tooltip("Font size per valori piccoli")]
        public int FontSizeSmall = 11;
        
        [Tooltip("Letter spacing per labels")]
        public float LetterSpacing = 2f;
        
        /// <summary>
        /// Ottiene il colore per un livello di fertilizzante
        /// </summary>
        public Color GetFertilizerColor(int level)
        {
            if (level >= FertilizerOptimalMin && level <= FertilizerOptimalMax)
                return GreenLed;
            if ((level >= FertilizerWarningMin && level < FertilizerOptimalMin) || 
                (level > FertilizerOptimalMax && level <= FertilizerWarningMax))
                return YellowStandard;
            return RedWarning;
        }
        
        /// <summary>
        /// Ottiene il colore per uno score di condizione
        /// </summary>
        public Color GetConditionColor(int score)
        {
            if (score >= ConditionOptimalMin && score <= ConditionOptimalMax)
                return GreenLed;
            if (score >= ConditionWarningMin && score < ConditionOptimalMin)
                return YellowStandard;
            return RedWarning;
        }
        
        /// <summary>
        /// Ottiene il colore per un livello di mold risk
        /// </summary>
        public Color GetMoldRiskColor(int level)
        {
            if (level == 0)
                return GreenLed;
            if (level <= MoldRiskLow)
                return YellowStandard;
            if (level <= MoldRiskMedium)
                return YellowStandard;
            return RedWarning;
        }
        
        /// <summary>
        /// Ottiene il testo del badge per mold risk
        /// </summary>
        public string GetMoldRiskBadgeText(int level)
        {
            if (level == 0)
                return "LOW";
            if (level <= MoldRiskLow)
                return "LOW";
            if (level <= MoldRiskMedium)
                return "MEDIUM";
            if (level <= MoldRiskHigh)
                return "HIGH";
            return "CRITICAL";
        }
    }
}

