using UnityEngine;

namespace _Project.Sporae.Core
{
    [CreateAssetMenu(fileName = "GoalConfig", menuName = "Game/Goals/BaseGoal")]
    public class GoalConfig : ScriptableObject
    {
        [field: SerializeField] public string Title { get; private set; }
    }
}