using System.Linq;
using UnityEngine;
using Sporae.DevTools;

namespace _Project.Sporae.Core
{
    [SpecificGoalChecker(GoalType = typeof(InventoryGoal))]
    public class InventoryGoalChecker : GoalChecker
    {
        private readonly Inventory _playerInventory;
        private readonly InventoryGoal _goalConfig;
        
        public InventoryGoalChecker(GoalConfig goalConfig)
        {
            _goalConfig = (InventoryGoal)goalConfig;

            // Usa ServiceContainer invece di FindObjectOfType
            var gameManager = ServiceContainer.Instance?.Get<GameManager>();
            if (gameManager != null)
            {
                _playerInventory = gameManager.PlayerInventory;
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.Core, "GameManager non disponibile via ServiceContainer!");
            }
        }
        
        public override bool Check() =>
            _goalConfig.Slots.All(slot => _playerInventory.Has(slot.Item.TypeId, slot.Quantity));
    }
}