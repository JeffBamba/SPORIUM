using System;
using System.Text;
using UnityEngine;
using _Project.Sporae.Core;

namespace Sporae.UI.Icons
{
    public static class GlobalIconResolver
    {
        private const string CatalogResourcePath = "UI/GlobalIconCatalog";
        private const string ItemsResourcePath = "Icons/Items/";
        private const string ActionsResourcePath = "Icons/Actions/";
        private const string DefaultIconName = "default";

        private static GlobalIconCatalog _catalog;
        private static bool _catalogTriedLoad;

        public static Sprite GetItemIcon(string typeId)
        {
            string category = string.IsNullOrWhiteSpace(typeId) ? "misc" : ResolveItemCategory(typeId);
            string variant = string.IsNullOrWhiteSpace(typeId) ? string.Empty : ResolveItemVariantKey(typeId);

            var catalog = GetCatalog();
            if (catalog != null)
            {
                if (catalog.TryGetTypeIcon(typeId, out var typeIcon)) return typeIcon;

                if (!string.IsNullOrEmpty(variant) &&
                    catalog.TryGetCategoryVariantIcon(category, variant, out var variantIcon))
                    return variantIcon;

                if (catalog.TryGetCategoryIcon(category, out var categoryIcon)) return categoryIcon;

                if (catalog.DefaultItemIcon != null) return catalog.DefaultItemIcon;
            }

            if (!string.IsNullOrWhiteSpace(typeId))
            {
                var byType = Resources.Load<Sprite>(ItemsResourcePath + typeId);
                if (byType != null) return byType;

                if (!string.IsNullOrEmpty(variant))
                {
                    var byCatVar = Resources.Load<Sprite>(ItemsResourcePath + category + "-" + variant);
                    if (byCatVar != null) return byCatVar;
                }
            }

            return Resources.Load<Sprite>(ItemsResourcePath + DefaultIconName);
        }

        public static Sprite GetPlantIcon(string plantCode = null)
        {
            var catalog = GetCatalog();
            if (catalog != null)
            {
                if (catalog.TryGetPlantCodeIcon(plantCode, out var plantCodeIcon)) return plantCodeIcon;
                if (catalog.TryGetCategoryIcon("plant", out var plantCategoryIcon)) return plantCategoryIcon;
                if (catalog.DefaultPlantIcon != null) return catalog.DefaultPlantIcon;
                if (catalog.DefaultItemIcon != null) return catalog.DefaultItemIcon;
            }

            return Resources.Load<Sprite>(ItemsResourcePath + DefaultIconName);
        }

        public static Sprite GetActionIcon(string actionKeyOrDisplayName)
        {
            string normalized = NormalizeKey(actionKeyOrDisplayName);
            var catalog = GetCatalog();
            if (catalog != null)
            {
                if (catalog.TryGetActionIcon(normalized, out var actionIcon)) return actionIcon;
                if (catalog.TryGetActionIcon(actionKeyOrDisplayName, out var rawActionIcon)) return rawActionIcon;
                if (catalog.TryGetCategoryIcon("action", out var actionCategoryIcon)) return actionCategoryIcon;
                if (catalog.DefaultActionIcon != null) return catalog.DefaultActionIcon;
                if (catalog.DefaultItemIcon != null) return catalog.DefaultItemIcon;
            }

            if (!string.IsNullOrWhiteSpace(normalized))
            {
                var byAction = Resources.Load<Sprite>(ActionsResourcePath + normalized);
                if (byAction != null) return byAction;
            }

            return Resources.Load<Sprite>(ItemsResourcePath + DefaultIconName);
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
            if (typeId == Items.AdditiveAcid || typeId == Items.AdditiveBasic || typeId == Items.SprayAntifungal) return "additive";
            if (typeId == Items.ReagentX || typeId == Items.ReagentY) return "reagent";
            if (typeId == Items.StemCellVegetable || typeId == Items.StemCellFungus || typeId == Items.StemCellAnimal) return "stemcell";
            if (typeId == Items.ProteinResidue || typeId == Items.OrganicResidue) return "protein";
            if (typeId == Items.FoodVegetable || typeId == Items.FoodFungus || typeId == Items.FoodMeat) return "food";
            if (typeId == Items.WholePlant) return "plant";

            return "misc";
        }

        /// <summary>
        /// Sotto-chiave per <see cref="GlobalIconCatalog.TryGetCategoryVariantIcon"/> e per sprite in Resources
        /// (<c>Icons/Items/{category}-{variant}.png</c>, es. <c>water-potable</c>, <c>fertilizer-pure</c>).
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

            if (typeId == Items.SprayAntifungal) return "spray";
            if (typeId == Items.AdditiveBasic) return "basic";
            if (typeId == Items.AdditiveAcid) return "acid";

            if (typeId == Items.ReagentX) return "x";
            if (typeId == Items.ReagentY) return "y";

            if (typeId == Items.StemCellVegetable) return "vegetable";
            if (typeId == Items.StemCellFungus) return "fungus";
            if (typeId == Items.StemCellAnimal) return "animal";

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
