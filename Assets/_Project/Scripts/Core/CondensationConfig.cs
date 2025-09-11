using UnityEngine;

namespace _Project
{
    [CreateAssetMenu(menuName = "Sporae/CondensationConfig")]
    public class CondensationConfig : ScriptableObject
    {
        [field: SerializeField] public float CondensationGrowthPerDay { get; private set; }
        [field: SerializeField] public float MaxCondensation { get; private set; }
    }
}