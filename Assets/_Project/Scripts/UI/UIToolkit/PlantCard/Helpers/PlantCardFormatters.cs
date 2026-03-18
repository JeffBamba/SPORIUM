using UnityEngine;
using Sporae.Dome.PotSystem.Growth;
using Sporae.Dome.PotSystem.Condition;

namespace Sporae.UI.UIToolkit.PlantCard.Helpers
{
    /// <summary>
    /// Helper class per formattazione testo in PlantCard V2.0.
    /// Centralizza tutta la formattazione per consistenza.
    /// </summary>
    public static class PlantCardFormatters
    {
        /// <summary>
        /// Formatta pH drift come "+2/day" o "-2/day"
        /// </summary>
        public static string FormatPhDrift(float drift)
        {
            string sign = drift >= 0 ? "+" : "";
            return $"{sign}{drift:F0}/day";
        }
        
        /// <summary>
        /// Formatta nome condizione in uppercase
        /// </summary>
        public static string FormatConditionName(PlantCondition condition)
        {
            return condition switch
            {
                PlantCondition.Rigogliosa => "RIGOGLIOSA",
                PlantCondition.Sana => "HEALTHY",
                PlantCondition.Stressata => "STRESSED",
                PlantCondition.Appassita => "WILTED",
                PlantCondition.Critica => "CRITICAL",
                _ => "UNKNOWN"
            };
        }
        
        /// <summary>
        /// Formatta growth stage in uppercase
        /// </summary>
        public static string FormatGrowthStage(PlantStage stage)
        {
            return stage switch
            {
                PlantStage.Seed => "SEED",
                PlantStage.Sprout => "SPROUT",
                PlantStage.Growth => "ADULTA",
                PlantStage.Flowering => "FLOWERING",
                PlantStage.HarvestReady => "HARVEST READY",
                PlantStage.Resting => "RESTING",
                _ => "UNKNOWN"
            };
        }
        
        /// <summary>
        /// Formatta percentuale come "54%"
        /// </summary>
        public static string FormatPercentage(int value, int max)
        {
            if (max <= 0) return "0%";
            int percent = Mathf.RoundToInt((float)value / max * 100f);
            return $"{percent}%";
        }
        
        /// <summary>
        /// Formatta percentuale diretta (0-100)
        /// </summary>
        public static string FormatPercentageDirect(int percent)
        {
            return $"{percent}%";
        }
        
        /// <summary>
        /// Formatta range pH come "50-100"
        /// </summary>
        public static string FormatPhRange(float min, float max)
        {
            return $"{min:F0}-{max:F0}";
        }
        
        /// <summary>
        /// Formatta range ottimale come "45%-55%-65%"
        /// </summary>
        public static string FormatOptimalRange(int min, int optimal, int max)
        {
            return $"{min}%-{optimal}%-{max}%";
        }
        
        /// <summary>
        /// Formatta Specimen ID da PotId (es. "POT-001" → "PLT-001")
        /// </summary>
        public static string FormatSpecimenId(string potId)
        {
            if (string.IsNullOrEmpty(potId))
                return "PLT-000";
            
            // Sostituisci "POT-" con "PLT-"
            if (potId.StartsWith("POT-"))
                return potId.Replace("POT-", "PLT-");
            
            return potId;
        }
        
        /// <summary>
        /// Formatta nome famiglia pianta
        /// </summary>
        public static string FormatFamilyName(PlantFamily family)
        {
            return family switch
            {
                PlantFamily.Standard => "Standard",
                PlantFamily.Pure => "Pure",
                PlantFamily.Evil => "Evil",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Formatta sottotitolo pianta (Family · Growth Stage · Level)
        /// </summary>
        public static string FormatPlantSubtitle(PlantFamily family, PlantStage stage, int level)
        {
            string familyName = FormatFamilyName(family);
            string stageName = FormatGrowthStage(stage);
            return $"{familyName} · {stageName} · Level {level}";
        }
        
        /// <summary>
        /// Formatta status LED system
        /// </summary>
        public static string FormatLedStatus(LedSystemState state)
        {
            return state switch
            {
                LedSystemState.Off => "○ OFFLINE",
                LedSystemState.Blue => "● BLUE ACTIVE",
                LedSystemState.Red => "● RED ACTIVE",
                _ => "○ OFFLINE"
            };
        }
        
        /// <summary>
        /// Formatta status irrigazione
        /// </summary>
        public static string FormatIrrigationStatus(bool isOn)
        {
            return isOn ? "● ACTIVE" : "○ STANDBY";
        }
        
        /// <summary>
        /// Formatta nota diario con timestamp
        /// </summary>
        public static string FormatDiaryNote(int day, string text)
        {
            return $"Day {day}: {text}";
        }
        
        /// <summary>
        /// Formatta rarità pianta
        /// </summary>
        public static string FormatRarity(PlantRarity rarity)
        {
            return rarity switch
            {
                PlantRarity.Common => "Common",
                PlantRarity.Uncommon => "Uncommon",
                PlantRarity.Rare => "Rare",
                PlantRarity.Epic => "Epic",
                PlantRarity.Legendary => "Legendary",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Nome leggibile per un seed (da seedTypeId). Usato da terminale, HUD, inventory.
        /// </summary>
        public static string GetSeedDisplayName(string seedTypeId)
        {
            if (string.IsNullOrEmpty(seedTypeId))
                return seedTypeId;
            var plantData = PlantDatabase.Instance?.GetPlantDataBySeedTypeId(seedTypeId);
            if (plantData != null)
                return GetPlantDisplayNameForSeed(plantData);
            return seedTypeId;
        }

        private static string GetPlantDisplayNameForSeed(PlantData plantData)
        {
            if (plantData == null)
                return "Sconosciuto";
            string baseName = plantData.PlantCode switch
            {
                "PLT-STD-001" => "Ferric Fern",
                "PLT-PURE-001" => "Arctic Hask",
                "PLT-EVIL-001" => "Glasscap Fungus",
                _ => plantData.name.Replace("PLT-", "").Replace("-", " ")
            };
            return $"{baseName} Seed";
        }
    }
}

