using UnityEngine;

namespace _Project.Sporae.Core
{
    [CreateAssetMenu(fileName = "ItemData", menuName = "Game/ItemData")]
    public class ItemConfig : ScriptableObject
    {
        [field: SerializeField] public string TypeId { get; private set; }
        [field: SerializeField] public int MaxQuality { get; private set; }
        [field: SerializeField] public int SellPrice { get; private set; }
        [field: SerializeField] public int BuyPrice { get; private set; }
        [field: SerializeField] public bool IsPerishable { get; private set; }
        [field: SerializeField] public float Stability { get; private set; }
        [field: SerializeField] public bool IsSeed { get; private set; }
    }
}