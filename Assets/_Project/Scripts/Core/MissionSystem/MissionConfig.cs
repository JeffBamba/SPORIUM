using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Sporae.Core
{
    [CreateAssetMenu(fileName = "MissionConfig", menuName = "Game/MissionConfig")]
    public class MissionConfig : ScriptableObject
    {
        [field: SerializeField] public string Title { get; private set; }
        [field: SerializeField] public List<GoalOptions> Goals { get; private set; }

        [Serializable]
        public struct GoalOptions
        {
            public List<GoalConfig> Options;
        } 
    }
}