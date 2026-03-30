using UnityEngine;

namespace Sporae.UI.UIToolkit
{
    /// <summary>
    /// Gradiente pH (scala 0–14) e mapping da drift -100..+100, allineato alla barra TopBar.
    /// </summary>
    public static class PhGradientDisplayColors
    {
        private static readonly Color PH_RED = new Color(1f, 0.165f, 0.165f, 1f);
        private static readonly Color PH_ORANGE = new Color(1f, 0.667f, 0.2f, 1f);
        private static readonly Color PH_WHITE = new Color(0.961f, 0.969f, 0.980f, 1f);
        private static readonly Color PH_BLUE = new Color(0.2f, 0.722f, 1f, 1f);
        private static readonly Color PH_PURPLE = new Color(0.357f, 0.310f, 1f, 1f);

        public static Color GetColorFromScale(float phValue)
        {
            phValue = Mathf.Clamp(phValue, 0f, 14f);

            if (phValue <= 4f)
            {
                float t = phValue / 4f;
                return Color.Lerp(PH_RED, PH_ORANGE, t);
            }

            if (phValue <= 7f)
            {
                float t = (phValue - 4f) / 3f;
                return Color.Lerp(PH_ORANGE, PH_WHITE, t);
            }

            if (phValue <= 10f)
            {
                float t = (phValue - 7f) / 3f;
                return Color.Lerp(PH_WHITE, PH_BLUE, t);
            }

            {
                float t = (phValue - 10f) / 4f;
                return Color.Lerp(PH_BLUE, PH_PURPLE, t);
            }
        }

        /// <summary>Mappa drift pH (-100..+100) sul gradiente (stessa logica della barra tooltip).</summary>
        public static Color GetColorFromDrift(float driftPh)
        {
            float phVisualScale = ((driftPh + 100f) / 200f) * 14f;
            phVisualScale = Mathf.Clamp(phVisualScale, 0f, 14f);
            return GetColorFromScale(phVisualScale);
        }
    }
}
