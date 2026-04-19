using System;
using _Project;
using _Project.Sporae.Core;
using Sporae.DevTools;

namespace _Project.Sporae.Core
{
    /// <summary>Ascolta OnItemConsumed e applica effetti idratazione (le azioni giornaliere vengono dall’alba/colazione).</summary>
    public class ItemConsumptionHandler
    {
        private readonly Inventory _inventory;
        private readonly PlayerHydrationSystem _hydration;
        private readonly GameManager _gameManager;

        public ItemConsumptionHandler(Inventory inventory, PlayerHydrationSystem hydration, GameManager gameManager = null)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _hydration = hydration ?? throw new ArgumentNullException(nameof(hydration));
            _gameManager = gameManager;
            _inventory.OnItemConsumed += OnItemConsumed;
        }

        private static bool IsSolidFood(string typeId)
        {
            if (string.IsNullOrEmpty(typeId)) return false;
            if (typeId == Items.FoodVegetable || typeId == Items.FoodFungus || typeId == Items.FoodMeat) return true;
            return Items.IsFruitType(typeId);
        }

        public void Unsubscribe()
        {
            _inventory.OnItemConsumed -= OnItemConsumed;
        }

        private void OnItemConsumed(string typeId, int quantity)
        {
            if (string.IsNullOrEmpty(typeId) || quantity <= 0) return;

            if (IsSolidFood(typeId))
                _gameManager?.NotifySolidFoodConsumed();

            if (typeId == Items.FoodVegetable)
            {
                _hydration.RecoverFromFood(quantity);
                return;
            }
            if (typeId == Items.FoodFungus)
            {
                _hydration.RecoverFromFood(quantity);
                return;
            }
            if (typeId == Items.FoodMeat)
            {
                _hydration.RecoverFromFood(quantity);
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
            if (Items.IsFruitType(typeId))
            {
                bool isPure = typeId == Items.FruitArcticPod || typeId == Items.FruitsKnown;
                _hydration.RecoverFromFruit(quantity, isPure);
                return;
            }
        }

        public static bool IsConsumable(string typeId)
        {
            if (string.IsNullOrEmpty(typeId)) return false;
            return typeId == Items.FoodVegetable || typeId == Items.FoodFungus || typeId == Items.FoodMeat
                   || typeId == Items.WaterPotable || typeId == Items.Water
                   || Items.IsFruitType(typeId);
        }
    }
}
