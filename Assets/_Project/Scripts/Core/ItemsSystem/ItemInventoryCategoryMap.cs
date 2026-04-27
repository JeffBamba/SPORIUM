using Sporae.Dome.PotSystem.Growth;
using Sporae.DevTools;
using UnityEngine;

namespace _Project.Sporae.Core
{
    /// <summary>Mappatura canonica <c>TypeId</c> → <see cref="ItemInventoryCategoryId"/>.</summary>
    public static class ItemInventoryCategoryMap
    {
        private static bool _warnedUnmapped;

        /// <summary>Nomi delle classi USS per accent scheda dettaglio (stesso set di <see cref="GetDetailAccentClass"/>). Usare per <c>RemoveFromClassList</c> senza accedere a <c>VisualElement.classList</c> (API non disponibile in tutte le versioni Unity).</summary>
        public static readonly string[] AllDetailAccentClassNames =
        {
            "inv-detail--cat-spores", "inv-detail--cat-seeds", "inv-detail--cat-organic", "inv-detail--cat-reagents",
            "inv-detail--cat-plants", "inv-detail--cat-fruits", "inv-detail--cat-tools", "inv-detail--cat-food", "inv-detail--cat-bio"
        };

        public static bool TryGetCategory(string typeId, out ItemInventoryCategoryId category)
        {
            category = ItemInventoryCategoryId.Organic;
            if (string.IsNullOrWhiteSpace(typeId))
                return false;

            if (typeId == Items.SporeGeneric)
            {
                category = ItemInventoryCategoryId.Spores;
                return true;
            }

            if (Items.IsFruitType(typeId))
            {
                category = ItemInventoryCategoryId.Fruits;
                return true;
            }

            if (typeId == Items.WholePlant)
            {
                category = ItemInventoryCategoryId.Plants;
                return true;
            }

            if (typeId == Items.PreSeed)
            {
                category = ItemInventoryCategoryId.Seeds;
                return true;
            }

            if (PlantDatabase.Instance != null && PlantDatabase.Instance.IsRegisteredSeedTypeId(typeId))
            {
                category = ItemInventoryCategoryId.Seeds;
                return true;
            }

            switch (typeId)
            {
                case Items.Water:
                    category = ItemInventoryCategoryId.Organic;
                    return true;
                case Items.WaterPotable:
                    category = ItemInventoryCategoryId.Food;
                    return true;
                case Items.ReagentX:
                case Items.ReagentY:
                    category = ItemInventoryCategoryId.Reagents;
                    return true;
                case Items.FertilizerStandard:
                case Items.FertilizerPure:
                case Items.FertilizerProhibited:
                case Items.AdditiveBasic:
                case Items.AdditiveAcid:
                    category = ItemInventoryCategoryId.Tools;
                    return true;
                case Items.FoodVegetable:
                case Items.FoodFungus:
                case Items.FoodMeat:
                    category = ItemInventoryCategoryId.Food;
                    return true;
                case Items.StemCellVegetable:
                case Items.StemCellFungus:
                case Items.StemCellAnimal:
                case Items.ProteinResidue:
                case Items.OrganicResidue:
                    category = ItemInventoryCategoryId.BioMaterials;
                    return true;
            }

            if (!_warnedUnmapped)
            {
                _warnedUnmapped = true;
                SporiumLogger.LogWarning(LogCategory.Inventory,
                    $"ItemInventoryCategoryMap: typeId non mappato '{typeId}' → ORGANIC (fallback). Aggiornare mappa.");
            }

            category = ItemInventoryCategoryId.Organic;
            return true;
        }

        public static string GetRowAccentClass(ItemInventoryCategoryId c)
        {
            switch (c)
            {
                case ItemInventoryCategoryId.Spores: return "inv-row--cat-spores";
                case ItemInventoryCategoryId.Seeds: return "inv-row--cat-seeds";
                case ItemInventoryCategoryId.Organic: return "inv-row--cat-organic";
                case ItemInventoryCategoryId.Reagents: return "inv-row--cat-reagents";
                case ItemInventoryCategoryId.Plants: return "inv-row--cat-plants";
                case ItemInventoryCategoryId.Fruits: return "inv-row--cat-fruits";
                case ItemInventoryCategoryId.Tools: return "inv-row--cat-tools";
                case ItemInventoryCategoryId.Food: return "inv-row--cat-food";
                case ItemInventoryCategoryId.BioMaterials: return "inv-row--cat-bio";
                default: return "inv-row--cat-organic";
            }
        }

        public static string GetDetailAccentClass(ItemInventoryCategoryId c)
        {
            switch (c)
            {
                case ItemInventoryCategoryId.Spores: return "inv-detail--cat-spores";
                case ItemInventoryCategoryId.Seeds: return "inv-detail--cat-seeds";
                case ItemInventoryCategoryId.Organic: return "inv-detail--cat-organic";
                case ItemInventoryCategoryId.Reagents: return "inv-detail--cat-reagents";
                case ItemInventoryCategoryId.Plants: return "inv-detail--cat-plants";
                case ItemInventoryCategoryId.Fruits: return "inv-detail--cat-fruits";
                case ItemInventoryCategoryId.Tools: return "inv-detail--cat-tools";
                case ItemInventoryCategoryId.Food: return "inv-detail--cat-food";
                case ItemInventoryCategoryId.BioMaterials: return "inv-detail--cat-bio";
                default: return "inv-detail--cat-organic";
            }
        }
    }
}
