using System.Collections.Generic;
using _Project.Sporae.Core;

namespace Sporae.Core.Localization
{
    /// <summary>
    /// Nomi item mostrati al giocatore (IT/EN), indipendenti dal typeId tecnico.
    /// Lingua effettiva: <see cref="GameLanguageSettings.GetEffectiveLanguage"/>.
    /// </summary>
    public static class ItemDisplayNameLocalization
    {
        private static readonly Dictionary<string, (string It, string En)> ByTypeId = new Dictionary<string, (string, string)>
        {
            { Items.FruitFerricPod, ("Frutto di Ferric Fern", "Ferric Fern Fruit") },
            { Items.FruitArcticPod, ("Frutto di Arctic Hask", "Arctic Hask Fruit") },
            { Items.FruitGlassPod, ("Frutto di GlassCap", "GlassCap Fruit") },
            { Items.Fruits, ("Frutto", "Fruit") },
            { Items.FruitsKnown, ("Frutto conosciuto", "Known fruit") },
            { Items.Water, ("Acqua Sporca", "Dirty Water") },
            { Items.WaterPotable, ("Acqua Potabile", "Drinking Water") },
            { Items.FertilizerStandard, ("Fertilizzante Generico", "Generic Fertilizer") },
            { Items.FertilizerPure, ("Fertilizzante Puro", "Pure Fertilizer") },
            { Items.FertilizerProhibited, ("Fertilizzante Oscuro", "Dark Fertilizer") },
            { Items.AdditiveBasic, ("Additivo Basico", "Basic Additive") },
            { Items.AdditiveAcid, ("Additivo Acido", "Acidic Additive") },
            { Items.ProteinResidue, ("Residui Proteici", "Protein Residue") },
            { Items.ReagentX, ("Reagente X", "Reagent X") },
            { Items.ReagentY, ("Reagente Y", "Reagent Y") },
            { Items.OrganicResidue, ("Residui Organici", "Organic Residue") },
            { Items.OrganicScrap001, ("Scarti organici", "Organic scrap") },
            { Items.WholePlant, ("Pianta intera", "Whole plant") },
            { Items.SporeGeneric, ("Spora", "Spore") },
            { Items.PreSeed, ("Pre-seme", "Pre-seed") },
            { Items.StemCellVegetable, ("Cellula staminale vegetale", "Vegetable stem cell") },
            { Items.StemCellFungus, ("Cellula staminale fungina", "Fungal stem cell") },
            { Items.StemCellAnimal, ("Cellula staminale animale", "Animal stem cell") },
            { Items.FoodVegetable, ("Vegetali sintetici", "Vegetable Synthetic") },
            { Items.FoodFungus, ("Funghi sintetici", "Fungal Synthetic") },
            { Items.FoodMeat, ("Carne sintetica", "Meat Synthetic") }
        };

        private static bool IsItalian() =>
            GameLanguageSettings.GetEffectiveLanguage() == GameLanguage.Italian;

        public static bool TryGetByTypeId(string typeId, out string displayName)
        {
            displayName = null;
            if (string.IsNullOrWhiteSpace(typeId))
                return false;
            if (!ByTypeId.TryGetValue(typeId, out var pair))
                return false;
            displayName = IsItalian() ? pair.It : pair.En;
            return true;
        }

        /// <summary>Titolo riga inventario per spore-generic in base allo stadio.</summary>
        public static string GetSporeTitle(SporeStage? stage)
        {
            if (!stage.HasValue)
                return TryGetByTypeId(Items.SporeGeneric, out var generic) ? generic : Items.SporeGeneric;
            if (stage.Value == SporeStage.Raw)
                return IsItalian() ? "Spora [Grezza]" : "Spore [Raw]";
            return IsItalian() ? "Spora [Matura]" : "Spore [Mature]";
        }

        public static string GetSporeStageSubLabel(SporeStage stage)
        {
            if (stage == SporeStage.Raw)
                return IsItalian() ? "Grezza" : "Raw";
            return IsItalian() ? "Matura" : "Mature";
        }

        public static string GetWholePlantWithSpecies(string customPlantName)
        {
            if (string.IsNullOrWhiteSpace(customPlantName))
                return TryGetByTypeId(Items.WholePlant, out var fallback) ? fallback : Items.WholePlant;
            return IsItalian()
                ? "Pianta intera " + customPlantName
                : "Whole plant: " + customPlantName;
        }

        public static string GetSeedWithSpecies(string customPlantName)
        {
            if (string.IsNullOrWhiteSpace(customPlantName))
                return IsItalian() ? "Seme" : "Seed";
            return IsItalian()
                ? "Seme di Pianta " + customPlantName
                : "Plant seed: " + customPlantName;
        }

        public static string GetPreSeedTooltipTitleFallback()
        {
            return TryGetByTypeId(Items.PreSeed, out var n) ? n : "Pre-seed";
        }
    }
}
