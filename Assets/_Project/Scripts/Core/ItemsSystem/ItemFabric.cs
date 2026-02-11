using UnityEngine;
using Sporae.DevTools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace _Project.Sporae.Core
{
    public static class ItemFabric
    {
        private static int _uniqueId = 0;
        
        /// <summary>
        /// Crea un Item dal typeId. Per SporeGeneric restituisce sempre una spora con status (Raw + Stabile):
        /// la spora senza status non esiste come item.
        /// </summary>
        /// <returns>Item creato, o null se il config non esiste (eccetto SporeGeneric che usa fallback).</returns>
        public static Item CreateItemByType(string typeId)
        {
            if (typeId == Items.SporeGeneric)
                return CreateSporeWithFallbackMetadata();

            var config = Resources.Load<ItemConfig>("Items/" + typeId);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config by id: {typeId}");
                return null;
            }

            var item = new Item(config, _uniqueId++);
            return item;
        }
        
        /// <summary>
        /// Crea un Item dal typeId specificato con qualità personalizzata.
        /// </summary>
        /// <param name="typeId">ID dell'item da creare</param>
        /// <param name="quality">Qualità da impostare (normalmente MaxQuality, ma può essere maggiore per bonus livello)</param>
        /// <returns>Item creato con qualità personalizzata, o null se il config non esiste</returns>
        public static Item CreateItemWithQuality(string typeId, float quality)
        {
            var config = Resources.Load<ItemConfig>("Items/" + typeId);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config by id: {typeId}");
                return null;
            }

            var item = new Item(config, _uniqueId++);
            item.Quality = quality;
            return item;
        }

        /// <summary>
        /// Crea un Item (es. frutto) con metadata da harvest (GDD 42 Fase 0).
        /// </summary>
        public static Item CreateItemWithMetadata(string typeId, float quality,
            GeneticType? geneticType, string family, string sourcePlantCode, int plantLevel = 0)
        {
            var config = Resources.Load<ItemConfig>("Items/" + typeId);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config by id: {typeId}");
                return null;
            }
            var item = new Item(config, _uniqueId++);
            item.Quality = quality;
            item.GeneticTypeValue = geneticType;
            item.FamilyMetadata = family;
            item.SourcePlantCodeMetadata = sourcePlantCode;
            item.PlantLevelMetadata = Mathf.Max(0, plantLevel);
            return item;
        }

        /// <summary>
        /// Crea una spora con metadata fallback per save vecchi (Raw + STABLE).
        /// </summary>
        public static Item CreateSporeWithFallbackMetadata()
        {
            var config = Resources.Load<ItemConfig>("Items/" + Items.SporeGeneric);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config for {Items.SporeGeneric}");
                return null;
            }
            var item = new Item(config, _uniqueId++);
            item.GeneticTypeValue = GeneticType.Stable;
            item.SporeStageValue = SporeStage.Raw;
            return item;
        }

        /// <summary>
        /// Crea una spora maturata (output Catalizzatore). Metadata: Matured + Stable.
        /// </summary>
        public static Item CreateSporeMatured()
        {
            var config = Resources.Load<ItemConfig>("Items/" + Items.SporeGeneric);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config for {Items.SporeGeneric}");
                return null;
            }
            var item = new Item(config, _uniqueId++);
            item.GeneticTypeValue = GeneticType.Stable;
            item.SporeStageValue = SporeStage.Matured;
            return item;
        }

        public static Item CreateSporeRawFromFruit(Item fruit)
        {
            var config = Resources.Load<ItemConfig>("Items/" + Items.SporeGeneric);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config for {Items.SporeGeneric}");
                return null;
            }

            var item = new Item(config, _uniqueId++);
            item.SporeStageValue = SporeStage.Raw;
            item.GeneticTypeValue = fruit?.GeneticTypeValue ?? GeneticType.Stable;
            item.FamilyMetadata = !string.IsNullOrWhiteSpace(fruit?.FamilyMetadata)
                ? fruit.FamilyMetadata
                : (fruit?.TypeId == Items.FruitsKnown ? "STANDARD" : null);
            item.SourcePlantCodeMetadata = !string.IsNullOrWhiteSpace(fruit?.SourcePlantCodeMetadata)
                ? fruit.SourcePlantCodeMetadata
                : (fruit?.TypeId == Items.FruitsKnown ? "Artic Hask" : null);
            item.PlantLevelMetadata = fruit != null ? Mathf.Max(0, fruit.PlantLevelMetadata) : (fruit?.TypeId == Items.FruitsKnown ? 4 : 0);
            return item;
        }

        public static Item CreateSporeMaturedFromRaw(Item rawSpore)
        {
            var config = Resources.Load<ItemConfig>("Items/" + Items.SporeGeneric);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config for {Items.SporeGeneric}");
                return null;
            }

            var item = new Item(config, _uniqueId++);
            item.SporeStageValue = SporeStage.Matured;
            item.GeneticTypeValue = rawSpore?.GeneticTypeValue ?? GeneticType.Stable;
            item.FamilyMetadata = rawSpore?.FamilyMetadata;
            item.SourcePlantCodeMetadata = rawSpore?.SourcePlantCodeMetadata;
            item.PlantLevelMetadata = rawSpore != null ? Mathf.Max(0, rawSpore.PlantLevelMetadata) : 0;
            return item;
        }

        public static GeneticType CombineGeneticTypeForPreSeed(GeneticType left, GeneticType right)
        {
            if (left == GeneticType.Fixed && right == GeneticType.Fixed)
                return GeneticType.Fixed;
            if ((left == GeneticType.Fixed && right == GeneticType.Unstable) ||
                (left == GeneticType.Unstable && right == GeneticType.Fixed))
                return GeneticType.Stable;
            if ((left == GeneticType.Fixed && right == GeneticType.Stable) ||
                (left == GeneticType.Stable && right == GeneticType.Fixed))
                return GeneticType.Stable;
            if (left == GeneticType.Stable && right == GeneticType.Stable)
                return GeneticType.Stable;
            if ((left == GeneticType.Stable && right == GeneticType.Unstable) ||
                (left == GeneticType.Unstable && right == GeneticType.Stable))
                return GeneticType.Unstable;
            return GeneticType.Unstable;
        }

        public static Item CreatePreSeedFromSpores(Item sporeA, Item sporeB)
        {
            var config = Resources.Load<ItemConfig>("Items/" + Items.PreSeed);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config for {Items.PreSeed}");
                return null;
            }

            var item = new Item(config, _uniqueId++);
            var gA = sporeA?.GeneticTypeValue ?? GeneticType.Stable;
            var gB = sporeB?.GeneticTypeValue ?? GeneticType.Stable;
            item.GeneticTypeValue = CombineGeneticTypeForPreSeed(gA, gB);
            item.ParentFamilyA = NormalizeFamily(sporeA?.FamilyMetadata);
            item.ParentFamilyB = NormalizeFamily(sporeB?.FamilyMetadata);
            item.CandidateTraitsCsv = BuildCandidateTraitsCsv(item.ParentFamilyA, item.ParentFamilyB);
            item.SourcePlantCodeMetadata = $"{sporeA?.SourcePlantCodeMetadata}|{sporeB?.SourcePlantCodeMetadata}";
            return item;
        }

        public static string ResolveFamilyWithReagentY(string familyA, string familyB)
        {
            string a = NormalizeFamily(familyA);
            string b = NormalizeFamily(familyB);
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
                return a;

            // Symmetric matrix (deterministic)
            if (PairIs(a, b, "PURE", "STANDARD")) return "PURE";
            if (PairIs(a, b, "PURE", "EVIL")) return "STANDARD";
            if (PairIs(a, b, "PURE", "IPNOTICHE")) return "PURE";
            if (PairIs(a, b, "STANDARD", "EVIL")) return "EVIL";
            if (PairIs(a, b, "STANDARD", "IPNOTICHE")) return "IPNOTICHE";
            if (PairIs(a, b, "EVIL", "IPNOTICHE")) return "EVIL";
            return a.CompareTo(b) <= 0 ? a : b;
        }

        public static Item CreateSeedFromPreSeed(Item preSeed, string resolvedFamily, string selectedTraitsCsv, int traitPowerPercent, string reagentTypeId, string chosenPlantName = null)
        {
            string normalizedFamily = NormalizeFamily(resolvedFamily);
            string seedTypeId = FamilyToSeedTypeId(normalizedFamily);
            var config = Resources.Load<ItemConfig>("Items/" + seedTypeId);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config for {seedTypeId}");
                return null;
            }

            var item = new Item(config, _uniqueId++);
            item.GeneticTypeValue = preSeed?.GeneticTypeValue ?? GeneticType.Stable;
            item.FamilyMetadata = string.IsNullOrWhiteSpace(resolvedFamily) ? normalizedFamily : resolvedFamily;
            item.ParentFamilyA = preSeed?.ParentFamilyA;
            item.ParentFamilyB = preSeed?.ParentFamilyB;
            item.CandidateTraitsCsv = preSeed?.CandidateTraitsCsv;
            item.SelectedTraitsCsv = selectedTraitsCsv;
            item.TraitPowerPercent = Mathf.Clamp(traitPowerPercent, 1, 100);
            item.ReagentUsedMetadata = reagentTypeId;
            item.SourcePlantCodeMetadata = preSeed?.SourcePlantCodeMetadata;
            item.CustomPlantName = chosenPlantName;
            return item;
        }

        public static string NormalizeFamily(string family)
        {
            if (string.IsNullOrWhiteSpace(family)) return "STANDARD";
            string norm = family.Trim().ToUpperInvariant();
            if (norm.Contains("PURE")) return "PURE";
            if (norm.Contains("EVIL")) return "EVIL";
            if (norm.Contains("IPNO")) return "IPNOTICHE";
            return "STANDARD";
        }

        public static string BuildCandidateTraitsCsv(string familyA, string familyB)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var trait in GetDefaultTraitsByFamily(familyA))
                set.Add(trait);
            foreach (var trait in GetDefaultTraitsByFamily(familyB))
                set.Add(trait);
            return string.Join(",", set);
        }

        public static List<string> ParseTraits(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return new List<string>();
            return csv
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IEnumerable<string> GetDefaultTraitsByFamily(string family)
        {
            switch (NormalizeFamily(family))
            {
                case "PURE":
                    return new[] { "GrowthSpeed", "PurityAura", "YieldBoost" };
                case "EVIL":
                    return new[] { "ToxinAffinity", "AggressiveSpread", "DarkResilience" };
                case "IPNOTICHE":
                    return new[] { "MindBloom", "PheromonePulse", "NeuralEcho" };
                default:
                    return new[] { "BalancedGrowth", "NeutralYield", "Resilience" };
            }
        }

        private static string FamilyToSeedTypeId(string normalizedFamily)
        {
            switch (NormalizeFamily(normalizedFamily))
            {
                case "PURE":
                    return Items.Seed002;
                case "EVIL":
                    return Items.Seed003;
                default:
                    return Items.Seed001;
            }
        }

        private static bool PairIs(string a, string b, string x, string y)
        {
            return (string.Equals(a, x, StringComparison.OrdinalIgnoreCase) && string.Equals(b, y, StringComparison.OrdinalIgnoreCase))
                || (string.Equals(a, y, StringComparison.OrdinalIgnoreCase) && string.Equals(b, x, StringComparison.OrdinalIgnoreCase));
        }
    }
}