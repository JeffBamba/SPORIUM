using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sporae.Core;

namespace _Project.Scripts.Core
{
    public class DeteriorationSystem
    {
        private readonly GameManager _gameManager;
        private readonly Inventory _inventory;

        private static readonly List<string> k_itemsToDeterioration = new()
        {
            "SDE-001", "SPORE_GENERIC"
        };
        
        public DeteriorationSystem(GameManager gameManager)
        {
            _gameManager = gameManager;
            _inventory = _gameManager.GetInventory();
            _gameManager.OnDayChanged += HandleDayChanged;
        }

        ~DeteriorationSystem()
        {
            _gameManager.OnDayChanged -= HandleDayChanged;
        }

        private void HandleDayChanged(int day)
        {
            foreach (
                var item in _inventory.Items
                    .ToList()
                    .Where(item => k_itemsToDeterioration.Contains(item.Id)))
            {
                item.Quality -= 1;
                if (item.Quality > 0)
                    continue;
                    
                _inventory.Add("ORG-SCR-001", item.Quantity);
                _inventory.Remove(item.Id, item.Quantity);
            }
        }
    }
}