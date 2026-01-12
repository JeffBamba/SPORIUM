using UnityEngine;

namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// Config condiviso per sprite globali del sistema POT (comuni a tutte le specie).
    /// </summary>
    [CreateAssetMenu(fileName = "PotSharedVisualsConfig", menuName = "Sporae/Pot/Shared Visuals Config")]
    public class PotSharedVisualsConfig : ScriptableObject
    {
        [Header("Shared Base Sprites (global)")]
        public Sprite emptyPotSprite;
        public Sprite seedSprite;
        public Sprite sproutSprite;
        public Sprite deadSprite; // Morta
    }
}

