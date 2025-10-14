using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Sporae.Core
{
    [CreateAssetMenu(fileName = "MissionConfig", menuName = "Game/MissionConfig")]
    public class MissionConfig : ScriptableObject
    {
        [field: SerializeField] public string Title { get; private set; }
        [field: SerializeField] public string Description { get; private set; }
        [field: SerializeField] public List<GoalOptions> Goals { get; private set; }

        [field: SerializeField] public Reward QuickPathReward { get; private set; }
        [field: SerializeField] public Reward FullPathReward { get; private set; }

        [Serializable]
        public struct Reward
        {
            public int CryReward;
            public List<RewardSlot> Rewards;
        }
        
        [Serializable]
        public struct RewardSlot
        {
            public ItemConfig Item;
            public int Quantity;
        }
            
        [Serializable]
        public struct GoalOptions
        {
            public List<GoalConfig> Options;
        } 
    }
}