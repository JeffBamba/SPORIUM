using System;
using System.Text;
using UnityEngine;
using _Project.Sporae.Core;

namespace Sporae.UI.Icons
{
    public static class GlobalIconResolver
    {
        private const string CatalogResourcePath = "UI/GlobalIconCatalog";

        private static GlobalIconCatalog _catalog;
        private static bool _catalogTriedLoad;

        /// <summary>Solo voci del <see cref="GlobalIconCatalog"/> (type / categoria+variante / categoria). Nessun Resources né default item.</summary>
        /// <param name="sporeStage">Solo per <see cref="Items.SporeGeneric"/>: varianti catalogo <c>spore-raw</c> / <c>spore-matured</c>; ignorato per altri typeId.</param>
        public static Sprite GetItemIcon(string typeId, SporeStage? sporeStage = null)
        {
            if (string.IsNullOrWhiteSpace(typeId))
                return null;

            string category = ResolveItemCategory(typeId);
            string variant = ResolveItemVariantKey(typeId);
            if (typeId == Items.SporeGeneric && sporeStage.HasValue)
                variant = sporeStage.Value == SporeStage.Raw ? "raw" : "matured";

            var catalog = GetCatalog();
            if (catalog == null)
                return null;

            if (catalog.TryGetTypeIcon(typeId, out var typeIcon))
                return typeIcon;

            if (!string.IsNullOrEmpty(variant) &&
                catalog.TryGetCategoryVariantIcon(category, variant, out var variantIcon))
                return variantIcon;

            if (catalog.TryGetCategoryIcon(category, out var categoryIcon))
                return categoryIcon;

            return null;
        }

        /// <summary>Override per <c>PlantCode</c> nel catalogo, altrimenti solo <see cref="GlobalIconCatalog.DefaultPlantIcon"/> (nessuna categoria <c>plant</c>, Resources o default item).</summary>
        public static Sprite GetPlantIcon(string plantCode = null)
        {
            var catalog = GetCatalog();
            if (catalog == null)
                return null;

            if (!string.IsNullOrWhiteSpace(plantCode) &&
                catalog.TryGetPlantCodeIcon(plantCode, out var plantCodeIcon))
                return plantCodeIcon;

            return catalog.DefaultPlantIcon;
        }

        /// <summary>Solo mappa azioni del catalogo (chiave normalizzata o raw). Nessuna categoria action, default né Resources.</summary>
        public static Sprite GetActionIcon(string actionKeyOrDisplayName)
        {
            var catalog = GetCatalog();
            if (catalog == null)
                return null;

            string normalized = NormalizeKey(actionKeyOrDisplayName);
            if (!string.IsNullOrWhiteSpace(normalized) &&
                catalog.TryGetActionIcon(normalized, out var actionIcon))
                return actionIcon;

            if (!string.IsNullOrWhiteSpace(actionKeyOrDisplayName) &&
                catalog.TryGetActionIcon(actionKeyOrDisplayName, out var rawActionIcon))
                return rawActionIcon;

            return null;
        }

        public static string ResolveItemCategory(string typeId)
        {
            if (string.IsNullOrWhiteSpace(typeId))
                return "misc";

            if (typeId == Items.SporeGeneric) return "spore";
            if (typeId == Items.PreSeed) return "preseed";
            if (typeId == Items.Seed001 || typeId == Items.Seed002 || typeId == Items.Seed003) return "seed";
            if (typeId.StartsWith("seed-", StringComparison.OrdinalIgnoreCase)) return "seed";
            if (Items.IsFruitType(typeId)) return "fruit";
            if (typeId == Items.Water || typeId == Items.WaterPotable) return "water";
            if (typeId == Items.FertilizerStandard || typeId == Items.FertilizerPure || typeId == Items.FertilizerProhibited) return "fertilizer";
            if (typeId == Items.AdditiveAcid || typeId == Items.AdditiveBasic) return "additive";
            if (typeId == Items.ReagentX || typeId == Items.ReagentY) return "reagent";
            if (typeId == Items.StemCellVegetable || typeId == Items.StemCellFungus || typeId == Items.StemCellAnimal) return "stemcell";
            if (typeId == Items.ProteinResidue || typeId == Items.OrganicResidue) return "protein";
            if (typeId == Items.FoodVegetable || typeId == Items.FoodFungus || typeId == Items.FoodMeat) return "food";
            if (typeId == Items.WholePlant) return "plant";

            return "misc";
        }

        /// <summary>
        /// Sotto-chiave per <see cref="GlobalIconCatalog.TryGetCategoryVariantIcon"/> e per sprite in Resources
        /// Chiavi varianti per il catalogo (es. <c>water-potable</c>, <c>spore-matured</c> in <see cref="GlobalIconCatalog"/>).
        /// Stringa vuota = nessuna variante (solo icona di categoria o per-typeId).
        /// </summary>
        public static string ResolveItemVariantKey(string typeId)
        {
            if (string.IsNullOrWhiteSpace(typeId))
                return string.Empty;

            if (typeId == Items.Water) return "raw";
            if (typeId == Items.WaterPotable) return "potable";

            if (typeId == Items.FertilizerStandard) return "standard";
            if (typeId == Items.FertilizerPure) return "pure";
            if (typeId == Items.FertilizerProhibited) return "prohibited";

            if (typeId == Items.AdditiveBasic) return "basic";
            if (typeId == Items.AdditiveAcid) return "acid";

            if (typeId == Items.ReagentX) return "x";
            if (typeId == Items.ReagentY) return "y";

            if (typeId == Items.StemCellVegetable) return "vegetable";
            if (typeId == Items.StemCellFungus) return "fungus";
            if (typeId == Items.StemCellAnimal) return "animal";

            if (typeId == Items.FoodVegetable) return "vegetable";
            if (typeId == Items.FoodFungus) return "fungus";
            if (typeId == Items.FoodMeat) return "meat";

            return string.Empty;
        }

        public static string NormalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            var sb = new StringBuilder(key.Length);
            for (int i = 0; i < key.Length; i++)
            {
                char c = char.ToLowerInvariant(key[i]);
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private static GlobalIconCatalog GetCatalog()
        {
            if (_catalogTriedLoad)
                return _catalog;

            _catalogTriedLoad = true;
            _catalog = Resources.Load<GlobalIconCatalog>(CatalogResourcePath);
            return _catalog;
        }
    }
}
