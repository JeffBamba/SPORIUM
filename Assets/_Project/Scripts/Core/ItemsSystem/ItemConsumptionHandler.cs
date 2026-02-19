using System;
using _Project.Sporae.Core;
using Sporae.Core;
using Sporae.DevTools;

namespace _Project.Sporae.Core
{
    /// <summary>Ascolta OnItemConsumed e applica effetti idratazione e bonus azioni.</summary>
    public class ItemConsumptionHandler
    {
        private readonly Inventory _inventory;
        private readonly PlayerHydrationSystem _hydration;
        private readonly ActionSystem _actionSystem;

        public ItemConsumptionHandler(Inventory inventory, PlayerHydrationSystem hydration, ActionSystem actionSystem)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _hydration = hydration ?? throw new ArgumentNullException(nameof(hydration));
            _actionSystem = actionSystem ?? throw new ArgumentNullException(nameof(actionSystem));
            _inventory.OnItemConsumed += OnItemConsumed;
        }

        public void Unsubscribe()
        {
            _inventory.OnItemConsumed -= OnItemConsumed;
        }

        private void OnItemConsumed(string typeId, int quantity)
        {
            if (string.IsNullOrEmpty(typeId) || quantity <= 0) return;

            if (typeId == Items.FoodVegetable)
            {
                _hydration.RecoverFromFood(quantity);
                _actionSystem.AddActions(1 * quantity);
                return;
            }
            if (typeId == Items.FoodFungus)
            {
                _hydration.RecoverFromFood(quantity);
                _actionSystem.AddActions(2 * quantity);
                return;
            }
            if (typeId == Items.FoodMeat)
            {
                _hydration.RecoverFromFood(quantity);
                _actionSystem.AddActions(3 * quantity);
                return;
            }
            if (typeId == Items.WaterPotable)
            {
                _hydration.RecoverFromWater(quantity, true);
                return;
            }
            if (typeId == Items.Water)
            {
                _hydration.RecoverFromWater(quantity, false);
                return;
            }
            if (typeId == Items.Fruits || typeId == Items.FruitsKnown)
            {
                bool isPure = typeId == Items.FruitsKnown;
                _hydration.RecoverFromFruit(quantity, isPure);
                return;
            }
        }

        public static bool IsConsumable(string typeId)
        {
            if (string.IsNullOrEmpty(typeId)) return false;
            return typeId == Items.FoodVegetable || typeId == Items.FoodFungus || typeId == Items.FoodMeat
                   || typeId == Items.WaterPotable || typeId == Items.Water
                   || typeId == Items.Fruits || typeId == Items.FruitsKnown;
        }
    }
}
