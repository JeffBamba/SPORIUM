using UnityEngine;
using Sporae.UI.UIToolkit.PlantCard;
using Sporae.Dome.PotSystem.Growth;

namespace Sporae.UI.UIToolkit.PlantCard.Helpers
{
    /// <summary>
    /// Helper class per calcolo colori dinamici in PlantCard V2.0.
    /// Usa PlantCardV2Config per thresholds configurabili.
    /// </summary>
    public static class PlantCardColorCalculator
    {
        /// <summary>
        /// Ottiene il colore per uno score di condizione
        /// </summary>
        public static Color GetConditionColor(int score, PlantCardV2Config config)
        {
            if (config == null)
                return Color.white;
            
            return config.GetConditionColor(score);
        }
        
        /// <summary>
        /// Ottiene il colore per un livello di fertilizzante
        /// </summary>
        public static Color GetFertilizerColor(int level, PlantCardV2Config config)
        {
            if (config == null)
                return Color.white;
            
            return config.GetFertilizerColor(level);
        }
        
        /// <summary>
        /// Ottiene il colore per un livello di mold risk
        /// </summary>
        public static Color GetMoldRiskColor(int level, PlantCardV2Config config)
        {
            if (config == null)
                return Color.white;
            
            return config.GetMoldRiskColor(level);
        }
        
        /// <summary>
        /// Ottiene il testo del badge per mold risk
        /// </summary>
        public static string GetMoldRiskBadgeText(int level, PlantCardV2Config config)
        {
            if (config == null)
                return "UNKNOWN";
            
            return config.GetMoldRiskBadgeText(level);
        }
        
        /// <summary>
        /// Ottiene il colore per uno stato LED
        /// </summary>
        public static Color GetLedColor(LedSystemState state, PlantCardV2Config config)
        {
            if (config == null)
                return Color.gray;
            
            return state switch
            {
                LedSystemState.Blue => config.BlueInfo,
                LedSystemState.Red => config.RedWarning,
                LedSystemState.Off => config.MetalLight,
                _ => config.MetalLight
            };
        }
        
        /// <summary>
        /// Ottiene il colore per stato irrigazione
        /// </summary>
        public static Color GetIrrigationColor(bool isOn, PlantCardV2Config config)
        {
            if (config == null)
                return Color.gray;
            
            return isOn ? config.GreenLed : config.BlueInfo;
        }
        
        /// <summary>
        /// Converte Color Unity a stringa hex per USS
        /// </summary>
        public static string ColorToHex(Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGB(color)}";
        }
        
        /// <summary>
        /// Converte Color Unity a stringa rgba per USS
        /// </summary>
        public static string ColorToRgba(Color color)
        {
            return $"rgba({(int)(color.r * 255)}, {(int)(color.g * 255)}, {(int)(color.b * 255)}, {color.a:F2})";
        }
    }
}

