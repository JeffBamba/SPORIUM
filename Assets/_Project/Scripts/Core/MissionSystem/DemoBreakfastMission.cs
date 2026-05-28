using System;
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
        public const string DemoBreakfastMissionConfigName = "M_Demo_Breakfast";

        /// <summary>Chiamato quando mangia/bevi avanza il completamento (per aggiornare progress bar recap).</summary>
        public static event Action ProgressChanged;

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

        /// <summary>
        /// Ripristina il tracking dopo load (senza cambiare il flow missione).
        /// Se la missione non è attiva, disattiva tracking e resetta stato locale.
        /// </summary>
        public static void RestoreTrackingState(
            Inventory inventory,
            PlayerInventoryPanelController inventoryPanel,
            bool trackingActive,
            bool ateFood,
            bool drankWater)
        {
            Bind(inventory, inventoryPanel);
            _trackingActive = trackingActive;
            _ateFood = ateFood;
            _drankWater = drankWater;
            ProgressChanged?.Invoke();
        }

        public static bool HasActiveDemoBreakfastMission(MissionManager missionManager)
        {
            if (missionManager?.CurrentMissions == null)
                return false;
            foreach (var m in missionManager.CurrentMissions)
            {
                if (m?.Config == null || m.IsCompleted)
                    continue;
                if (IsDemoBreakfastConfig(m.Config))
                    return true;
            }
            return false;
        }

        public static void ExportTrackingState(out bool trackingActive, out bool ateFood, out bool drankWater)
        {
            trackingActive = _trackingActive;
            ateFood = _ateFood;
            drankWater = _drankWater;
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

            bool prevAte = _ateFood;
            bool prevDrank = _drankWater;
            if (IsSolidFood(typeId))
                _ateFood = true;
            if (IsDrink(typeId))
                _drankWater = true;

            if (_ateFood != prevAte || _drankWater != prevDrank)
                ProgressChanged?.Invoke();
        }

        /// <summary>Progress 0 / 0.5 / 1 per missione colazione (due passi: mangia + bevi).</summary>
        public static float GetObjectiveProgress01(MissionConfig cfg)
        {
            if (cfg == null || !IsDemoBreakfastConfig(cfg))
                return -1f;
            if (!_trackingActive)
                return 0f;
            int done = (_ateFood ? 1 : 0) + (_drankWater ? 1 : 0);
            return done * 0.5f;
        }

        public static bool IsDemoBreakfastConfig(MissionConfig cfg) =>
            cfg != null && string.Equals(cfg.name, DemoBreakfastMissionConfigName, StringComparison.Ordinal);

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
