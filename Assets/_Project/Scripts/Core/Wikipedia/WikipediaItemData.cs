using UnityEngine;

namespace _Project.Wikipedia
{
    [CreateAssetMenu(fileName = "WikipediaItemData", menuName = "Game/WikipediaItemData")]
    public class WikipediaItemData : ScriptableObject
    {
        public enum Sections
        {
            Spores,
            Reagents,
            Plants,
            Hybrids,
            Mutations, 
            FoodWater
        }
        
        [field: SerializeField] public Sections Section { get; private set; }
        [field: SerializeField] public Sprite Sprite { get; private set; }
        [field: SerializeField] public string Title { get; private set; }
        [field: SerializeField] public string Description { get; private set; }
    }
}