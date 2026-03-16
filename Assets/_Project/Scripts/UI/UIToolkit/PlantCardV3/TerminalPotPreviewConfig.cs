using UnityEngine;

namespace Sporae.UI.UIToolkit.PlantCardV3
{
    /// <summary>
    /// Set di sprite per la preview pianta in Zona 2 del Terminal Pot (stile incubator).
    /// Stesso sistema di cambio immagine del Dome Room (stadio + condizione) ma asset dedicati.
    /// </summary>
    [CreateAssetMenu(fileName = "TerminalPotPreviewConfig", menuName = "Sporae/UI/Terminal Pot Preview Config")]
    public class TerminalPotPreviewConfig : ScriptableObject
    {
        [Header("Pot vuoto")]
        [Tooltip("Immagine 'status vuoto' mostrata in preview quando il pot è vuoto.")]
        public Sprite statusVuotoSprite;

        [Header("Stadi condivisi (incubator style)")]
        public Sprite seedSprite;
        public Sprite sproutSprite;
        public Sprite deadSprite;

        [Header("Stadi adulti (fallback incubator, usato per Growth/Resting/Flowering/HarvestReady)")]
        public Sprite adultSprite;
        public Sprite floweringSprite;
    }
}
