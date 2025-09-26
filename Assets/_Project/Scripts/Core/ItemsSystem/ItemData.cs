using UnityEngine;

namespace _Project.Sporae.Core
{
    [CreateAssetMenu(fileName = "ItemData", menuName = "Game/ItemData")]
    public class ItemData : ScriptableObject
    {
        [field: SerializeField] public string TypeId { get; private set; }
        [field: SerializeField] public int MaxQuality { get; private set; }
    }
}