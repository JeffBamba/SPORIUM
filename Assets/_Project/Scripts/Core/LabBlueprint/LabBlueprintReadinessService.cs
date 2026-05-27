using System;
using System.Collections.Generic;
using _Project.Sporae.Core;

namespace _Project.Sporae.Core.LabBlueprint
{
    public enum LabBlueprintReadinessStatus
    {
        Ready = 0,
        NoMaterial = 1,
        ProjectAlreadyActive = 2,
        NoInventory = 3
    }

    public readonly struct LabBlueprintReadinessResult
    {
        public readonly LabBlueprintReadinessStatus Status;
        public readonly int FruitCount;
        public readonly int SporeCount;

        public bool HasFruit => FruitCount > 0;
        public bool HasSpore => SporeCount > 0;
        public bool IsReady => Status == LabBlueprintReadinessStatus.Ready;

        public LabBlueprintReadinessResult(LabBlueprintReadinessStatus status, int fruitCount, int sporeCount)
        {
            Status = status;
            FruitCount = fruitCount;
            SporeCount = sporeCount;
        }

        public static LabBlueprintReadinessResult Ready(int fruitCount, int sporeCount) =>
            new LabBlueprintReadinessResult(LabBlueprintReadinessStatus.Ready, fruitCount, sporeCount);
    }

    /// <summary>
    /// Verifica materiale idoneo (frutto XOR spora in inventario giocatore) per avviare un draft LAB 4.0.
    /// Sostituisce la logica Replica/Hybrid/NewProfile di <c>BuildProjectTypeAnalysis</c> per il gate pre-progettazione.
    /// </summary>
    public sealed class LabBlueprintReadinessService
    {
        public LabBlueprintReadinessResult Evaluate(Inventory inventory, LabBlueprintService blueprint = null)
        {
            if (blueprint != null && blueprint.HasDraftOrActiveProject)
                return new LabBlueprintReadinessResult(LabBlueprintReadinessStatus.ProjectAlreadyActive, 0, 0);

            if (inventory == null)
                return new LabBlueprintReadinessResult(LabBlueprintReadinessStatus.NoInventory, 0, 0);

            int fruitCount = CountFruitInstances(inventory);
            int sporeCount = CountSporeInstances(inventory);

            if (fruitCount == 0 && sporeCount == 0)
                return new LabBlueprintReadinessResult(LabBlueprintReadinessStatus.NoMaterial, 0, 0);

            return LabBlueprintReadinessResult.Ready(fruitCount, sporeCount);
        }

        public static string GetLocalizationKey(LabBlueprintReadinessStatus status)
        {
            switch (status)
            {
                case LabBlueprintReadinessStatus.NoMaterial:
                    return "lab_blueprint.readiness.no_material";
                case LabBlueprintReadinessStatus.ProjectAlreadyActive:
                    return "lab_blueprint.readiness.project_active";
                case LabBlueprintReadinessStatus.NoInventory:
                    return "lab_blueprint.readiness.no_inventory";
                default:
                    return null;
            }
        }

        public static LabBlueprintInputKind ResolveInputKind(string typeId)
        {
            if (string.IsNullOrWhiteSpace(typeId))
                return LabBlueprintInputKind.None;
            if (typeId == Items.SporeGeneric)
                return LabBlueprintInputKind.Spore;
            if (Items.IsFruitType(typeId, includeLegacy: true))
                return LabBlueprintInputKind.Fruit;
            return LabBlueprintInputKind.None;
        }

        public static bool IsEligiblePickerType(string typeId) =>
            ResolveInputKind(typeId) != LabBlueprintInputKind.None;

        public static bool TryValidateSelectedItem(Item item, out LabBlueprintInputKind inputKind)
        {
            inputKind = LabBlueprintInputKind.None;
            if (item == null || string.IsNullOrWhiteSpace(item.TypeId))
                return false;

            inputKind = ResolveInputKind(item.TypeId);
            return inputKind != LabBlueprintInputKind.None;
        }

        /// <summary>
        /// TypeId presenti in inventario, ordinati per il picker LAB (frutti poi spora).
        /// </summary>
        public static List<string> BuildPickerAllowedTypeIds(Inventory inventory)
        {
            var allowed = new List<string>();
            if (inventory == null)
                return allowed;

            foreach (var fruitTypeId in Items.AllFruitTypeIds)
            {
                if (CountFruitInstancesOfType(inventory, fruitTypeId) > 0)
                    allowed.Add(fruitTypeId);
            }

            if (CountSporeInstances(inventory) > 0)
                allowed.Add(Items.SporeGeneric);

            return allowed;
        }

        public static int CountFruitInstances(Inventory inventory)
        {
            if (inventory == null)
                return 0;

            int total = 0;
            foreach (var slot in inventory.Items)
            {
                if (slot == null || string.IsNullOrWhiteSpace(slot.TypeId))
                    continue;
                if (!Items.IsFruitType(slot.TypeId, includeLegacy: true))
                    continue;
                total += slot.Items != null ? slot.Items.Count : 0;
            }

            return total;
        }

        public static int CountSporeInstances(Inventory inventory)
        {
            if (inventory == null)
                return 0;

            if (!inventory.Has(Items.SporeGeneric))
                return 0;

            foreach (var slot in inventory.Items)
            {
                if (slot == null || slot.TypeId != Items.SporeGeneric)
                    continue;
                return slot.Items != null ? slot.Items.Count : 0;
            }

            return 0;
        }

        private static int CountFruitInstancesOfType(Inventory inventory, string fruitTypeId)
        {
            if (inventory == null || string.IsNullOrWhiteSpace(fruitTypeId))
                return 0;

            foreach (var slot in inventory.Items)
            {
                if (slot == null || !string.Equals(slot.TypeId, fruitTypeId, StringComparison.OrdinalIgnoreCase))
                    continue;
                return slot.Items != null ? slot.Items.Count : 0;
            }

            return 0;
        }
    }
}
