using UnityEngine;

namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// Set visual per singola specie pianta (data-driven).
    /// Regole:
    /// - Adult = usato sia per Growth che per Resting
    /// - Flowering = sprite dedicata
    /// - HarvestReady = Adult + FruitOverlay
    /// </summary>
    [CreateAssetMenu(fileName = "PlantVisualSet", menuName = "Sporae/Pot/Plant Visual Set")]
    public class PlantVisualSet : ScriptableObject
    {
        [Header("Per-species sprites")]
        public Sprite adultSprite;
        public Sprite floweringSprite;
        public Sprite fruitOverlaySprite;
    }
}

