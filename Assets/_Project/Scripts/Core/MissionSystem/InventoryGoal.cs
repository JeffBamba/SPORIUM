using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Sporae.Core
{
    [CreateAssetMenu(fileName = "GoalConfig", menuName = "Game/Goals/InventoryGoal")]
    public class InventoryGoal : GoalConfig
    {
        [field: SerializeField] public List<RequireSlot> Slots { get; set; }

        [Serializable]
        public struct RequireSlot
        {
            public ItemConfig Item;
            public int Quantity;
        }
    }
}