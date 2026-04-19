using UnityEngine;

namespace _Project.Sporae.Core
{
    /// <summary>
    /// Obiettivo completato quando <see cref="MissionFlagTracker"/> ha il flag impostato (es. apertura Armadio).
    /// </summary>
    [CreateAssetMenu(fileName = "MissionFlagGoal", menuName = "Game/Goals/MissionFlagGoal")]
    public sealed class MissionFlagGoal : GoalConfig
    {
        [SerializeField] private string _flagKey = "demo_wardrobe";

        public string FlagKey => _flagKey;
    }
}
