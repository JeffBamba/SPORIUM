using System.Collections.Generic;
using System.Linq;
using _Project.Sporae.Core;
using Sporae.Dome.PotSystem.Growth;
using Sporae.UI.UIToolkit.NotificationsFoundation;

namespace _Project.Scripts.Core
{
    public class DeteriorationSystem
    {
        private readonly DayCycleSystem _dayCycleSystem;
        private readonly GameManager _gameManager;
        private readonly Inventory _inventory;

        private static readonly List<string> k_itemsToDeterioration = new()
        {
            Items.SporeGeneric,
            Items.WholePlant,
            Items.FoodVegetable,
            Items.FoodFungus,
            Items.FoodMeat
        };
        
        public DeteriorationSystem(GameManager gameManager)
        {
            _gameManager = gameManager;
            _inventory = _gameManager.PlayerInventory;
            
            _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
            _dayCycleSystem.OnDayChanged += HandleDayChanged;
        }

        ~DeteriorationSystem()
        {
            _dayCycleSystem.OnDayChanged -= HandleDayChanged;
        }

        private void HandleDayChanged(int day)
        {
            int degradedStillPresent = 0;
            int spoiledOrganic = 0;
            int spoiledProtein = 0;
            foreach (
                var inventorySlot in _inventory.Items
                    .ToList()
                    .Where(item => k_itemsToDeterioration.Contains(item.TypeId)
                                   || (PlantDatabase.Instance != null &&
                                       PlantDatabase.Instance.IsRegisteredSeedTypeId(item.TypeId))
                                   || Items.IsFruitType(item.TypeId))
            )
                DeteriorateInventorySlot(inventorySlot, ref degradedStillPresent, ref spoiledOrganic, ref spoiledProtein);

            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation == null || !foundation.Enabled)
                return;

            if (degradedStillPresent > 0)
            {
                foundation.PostToastImmediate(
                    "INV-DET-WARN",
                    new NotificationPayload().With("n", degradedStillPresent.ToString()));
            }
            if (spoiledOrganic > 0)
            {
                foundation.PostToastImmediate(
                    "INV-DET-ORG",
                    new NotificationPayload().With("n", spoiledOrganic.ToString()));
            }
            if (spoiledProtein > 0)
            {
                foundation.PostToastImmediate(
                    "INV-DET-PROT",
                    new NotificationPayload().With("n", spoiledProtein.ToString()));
            }
        }

        private void DeteriorateInventorySlot(
            InventorySlot slot,
            ref int degradedStillPresent,
            ref int spoiledOrganic,
            ref int spoiledProtein)
        {
            foreach (var item in slot.Items.ToList())
            {
                item.Quality -= 1;
                if (item.Quality > 0)
                {
                    degradedStillPresent++;
                    continue;
                }

                if (item.TypeId == Items.FoodMeat)
                {
                    _inventory.Add(Items.ProteinResidue);
                    spoiledProtein++;
                }
                else
                {
                    _inventory.Add(Items.OrganicResidue);
                    spoiledOrganic++;
                }
                _inventory.Consume(item.TypeId);
            }
        }
    }
}