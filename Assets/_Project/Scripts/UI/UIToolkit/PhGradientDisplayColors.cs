using UnityEngine;
using _Project;

namespace Sporae.UI.UIToolkit
{
    /// <summary>
    /// Gradiente pH tipo striscia 0–14 (rosso → … → viola) e mapping da drift -100..+100 / bande <see cref="PhSystem.PhBand"/>.
    /// </summary>
    public static class PhGradientDisplayColors
    {
        /// <summary>15 campioni (0…14), interpolazione lineare tra indici consecutivi.</summary>
        private static readonly Color[] Stops =
        {
            new Color(1f, 0.05f, 0.05f, 1f),   // 0  bright red
            new Color(1f, 0.25f, 0.1f, 1f),   // 1  red-orange
            new Color(1f, 0.5f, 0.1f, 1f),    // 2  orange
            new Color(1f, 0.75f, 0.15f, 1f),  // 3  golden yellow
            new Color(1f, 0.95f, 0.2f, 1f),  // 4  bright yellow
            new Color(0.75f, 0.95f, 0.2f, 1f), // 5  lime / yellow-green
            new Color(0.45f, 0.85f, 0.35f, 1f), // 6  light green
            new Color(0.2f, 0.65f, 0.35f, 1f), // 7  medium green (neutro chimico)
            new Color(0.15f, 0.7f, 0.65f, 1f),  // 8  teal
            new Color(0.2f, 0.85f, 0.9f, 1f),   // 9  cyan
            new Color(0.3f, 0.75f, 1f, 1f),    // 10 sky blue
            new Color(0.2f, 0.45f, 0.95f, 1f), // 11 royal blue
            new Color(0.15f, 0.25f, 0.85f, 1f), // 12 deep blue
            new Color(0.45f, 0.2f, 0.85f, 1f),  // 13 purple
            new Color(0.35f, 0.1f, 0.55f, 1f),  // 14 dark purple
        };

        /// <summary>Posizioni rappresentative sulla scala 0–14 per ogni <see cref="PhSystem.PhBand"/> (allineamento Ultra/Stable/Neutro).</summary>
        private const float BandVisualUltraAcid = 1f;
        private const float BandVisualStableAcid = 4f;
        private const float BandVisualNeutral = 7f;
        private const float BandVisualStableBasic = 10f;
        private const float BandVisualUltraBasic = 13f;

        public static Color GetColorFromScale(float phValue)
        {
            phValue = Mathf.Clamp(phValue, 0f, 14f);
            int i = Mathf.FloorToInt(phValue);
            if (i >= 14)
                return Stops[14];
            float t = phValue - i;
            return Color.Lerp(Stops[i], Stops[i + 1], t);
        }

        /// <summary>Colore sintetico per la banda di gioco (stessa famiglia cromatica della striscia).</summary>
        public static Color GetColorForPhBand(PhSystem.PhBand band)
        {
            float v = band switch
            {
                PhSystem.PhBand.UltraAcid => BandVisualUltraAcid,
                PhSystem.PhBand.StableAcid => BandVisualStableAcid,
                PhSystem.PhBand.Neutral => BandVisualNeutral,
                PhSystem.PhBand.StableBasic => BandVisualStableBasic,
                PhSystem.PhBand.UltraBasic => BandVisualUltraBasic,
                _ => BandVisualNeutral
            };
            return GetColorFromScale(v);
        }

        /// <summary>Mappa drift pH (-100..+100) sulla posizione della striscia (come marker TopBar).</summary>
        public static Color GetColorFromDrift(float driftPh)
        {
            float phVisualScale = ((driftPh + 100f) / 200f) * 14f;
            phVisualScale = Mathf.Clamp(phVisualScale, 0f, 14f);
            return GetColorFromScale(phVisualScale);
        }

        /// <summary>Hex <c>#RRGGBB</c> per rich text Unity.</summary>
        public static string ToHtmlStringRgb(Color c)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(c);
        }
    }
}
