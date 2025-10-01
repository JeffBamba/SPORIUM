using System.Linq;
using UnityEngine;

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

            var gameManager = Object.FindObjectOfType<GameManager>();
            _playerInventory = gameManager.PlayerInventory;
        }
        
        public override bool Check() =>
            _goalConfig.Slots.All(slot => _playerInventory.Has(slot.Item.TypeId, slot.Quantity));
    }
}