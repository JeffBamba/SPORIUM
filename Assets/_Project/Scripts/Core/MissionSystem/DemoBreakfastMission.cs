using Sporae.UI.UIToolkit.PlayerInventory;

namespace _Project.Sporae.Core
{
    /// <summary>
    /// Hook missione colazione demo:
    /// - richiede almeno un "Mangia" (cibo/frutto) e un "Bevi" (acqua),
    /// - completa solo quando il player chiude l'inventario dopo aver fatto entrambe le azioni.
    /// </summary>
    public static class DemoBreakfastMission
    {
        public const string DemoBreakfastCompletedFlagKey = "demo_breakfast_completed";

        private static Inventory _inventory;
        private static PlayerInventoryPanelController _inventoryPanel;
        private static bool _isBound;
        private static bool _trackingActive;
        private static bool _ateFood;
        private static bool _drankWater;

        public static void BeginTracking(Inventory inventory, PlayerInventoryPanelController inventoryPanel)
        {
            Bind(inventory, inventoryPanel);
            _ateFood = false;
            _drankWater = false;
            _trackingActive = true;
            ServiceContainer.Instance?.Get<MissionFlagTracker>(suppressWarning: true)
                ?.ClearFlag(DemoBreakfastCompletedFlagKey);
        }

        private static void Bind(Inventory inventory, PlayerInventoryPanelController inventoryPanel)
        {
            if (_isBound && (_inventory != inventory || _inventoryPanel != inventoryPanel))
                Unbind();

            if (_isBound)
                return;

            _inventory = inventory;
            _inventoryPanel = inventoryPanel;
            if (_inventory == null || _inventoryPanel == null)
                return;

            _inventory.OnItemConsumed += HandleItemConsumed;
            _inventoryPanel.OnClosed += HandleInventoryClosed;
            _isBound = true;
        }

        private static void Unbind()
        {
            if (_inventory != null)
                _inventory.OnItemConsumed -= HandleItemConsumed;
            if (_inventoryPanel != null)
                _inventoryPanel.OnClosed -= HandleInventoryClosed;
            _inventory = null;
            _inventoryPanel = null;
            _isBound = false;
        }

        private static void HandleItemConsumed(string typeId, int quantity)
        {
            if (!_trackingActive || quantity <= 0)
                return;

            if (IsSolidFood(typeId))
                _ateFood = true;
            if (IsDrink(typeId))
                _drankWater = true;
        }

        private static void HandleInventoryClosed()
        {
            if (!_trackingActive || !_ateFood || !_drankWater)
                return;

            ServiceContainer.Instance?.Get<MissionFlagTracker>(suppressWarning: true)
                ?.SetFlag(DemoBreakfastCompletedFlagKey);
            _trackingActive = false;
        }

        private static bool IsDrink(string typeId) =>
            typeId == Items.WaterPotable || typeId == Items.Water;

        private static bool IsSolidFood(string typeId) =>
            typeId == Items.FoodVegetable ||
            typeId == Items.FoodFungus ||
            typeId == Items.FoodMeat ||
            Items.IsFruitType(typeId);
    }
}
