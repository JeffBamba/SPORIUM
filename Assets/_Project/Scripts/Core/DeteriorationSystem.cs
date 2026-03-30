using System.Collections.Generic;
using System.Linq;
using _Project.Sporae.Core;
using Sporae.Dome.PotSystem.Growth;

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
            Items.WholePlant
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
            foreach (
                var inventorySlot in _inventory.Items
                    .ToList()
                    .Where(item => k_itemsToDeterioration.Contains(item.TypeId)
                                   || (PlantDatabase.Instance != null &&
                                       PlantDatabase.Instance.IsRegisteredSeedTypeId(item.TypeId)))
            )
                DeteriorateInventorySlot(inventorySlot);
        }

        private void DeteriorateInventorySlot(InventorySlot slot)
        {
            foreach (var item in slot.Items.ToList())
            {
                item.Quality -= 1;
                if (item.Quality > 0)
                    continue;

                _inventory.Add(Items.OrganicScrap001);
                _inventory.Consume(item.TypeId);
            }
        }
    }
}