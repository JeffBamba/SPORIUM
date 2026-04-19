using UnityEngine;
using UnityEngine.UIElements;

namespace Sporae.UI.UIToolkit.HUD
{
    /// <summary>
    /// Cursore OS con hint "?" per elementi HUD che espongono tooltip al passaggio del mouse.
    /// Gli elementi trigger devono avere la classe USS <see cref="TooltipHostUssClass"/>.
    /// </summary>
    public static class HudTooltipCursor
    {
        public const string TooltipHostUssClass = "hud-tooltip-host";

        private static Texture2D _generatedTexture;
        private static readonly Vector2Int DefaultHotspot = new Vector2Int(0, 0);

        public static Vector2Int Hotspot => DefaultHotspot;

        public static bool HasTooltipHostAncestor(VisualElement ve)
        {
            while (ve != null)
            {
                if (ve.ClassListContains(TooltipHostUssClass))
                    return true;
                ve = ve.parent;
            }

            return false;
        }

        /// <summary>
        /// True se il pick è sull’area tooltip TopBar: classe <see cref="TooltipHostUssClass"/> oppure
        /// antenato con <c>name</c> stabile (metriche / pannelli tooltip). Copre i casi in cui
        /// il pick colpisce un overlay tooltip o un nodo senza classe ancora applicata.
        /// </summary>
        public static bool IsUnderTopBarTooltipHost(VisualElement ve)
        {
            if (ve == null)
                return false;
            if (HasTooltipHostAncestor(ve))
                return true;

            for (var p = ve; p != null; p = p.parent)
            {
                string n = p.name;
                if (string.IsNullOrEmpty(n))
                    continue;
                switch (n)
                {
                    case "ph-display":
                    case "mutation-display":
                    case "condensation-display":
                    case "ph-tooltip":
                    case "condensation-tooltip":
                    case "mutation-tooltip":
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Texture 32×32 freccia chiara + "?" giallo; hotspot (0,0) punta in alto a sinistra.
        /// </summary>
        public static Texture2D GetOrCreateDefaultCursorTexture()
        {
            if (_generatedTexture != null)
                return _generatedTexture;

            const int w = 32;
            const int h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "HudTooltipCursor_Generated"
            };

            var clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, clear);

            // Freccia stile OS (punta a 0,0): colonna sinistra + diagonale
            var arrow = new Color(0.95f, 0.97f, 1f, 1f);
            for (int i = 0; i < 18 && i < h; i++)
                tex.SetPixel(0, i, arrow);
            for (int d = 0; d < 14; d++)
            {
                int x = 1 + d;
                int y = 1 + d;
                if (x < w && y < h)
                    tex.SetPixel(x, y, arrow);
            }

            // Contorno scuro per contrasto su sfondi chiari
            var outline = new Color(0.1f, 0.12f, 0.14f, 0.85f);
            for (int i = 1; i < 18 && i < h; i++)
                tex.SetPixel(1, i, outline);
            for (int d = 0; d < 14; d++)
            {
                int x = 2 + d;
                int y = 1 + d;
                if (x < w && y < h)
                    tex.SetPixel(x, y + 1, outline);
            }

            // "?" 5×7 pixel, offset (13, 3) — giallo/ambrato
            var q = new Color(1f, 0.92f, 0.45f, 1f);
            DrawQuestionMark5x7(tex, 13, 3, q);

            // Keep texture readable: Cursor.SetCursor rejects non-readable textures.
            tex.Apply(false, false);
            _generatedTexture = tex;
            return _generatedTexture;
        }

        private static void DrawQuestionMark5x7(Texture2D tex, int ox, int oy, Color c)
        {
            // Righe 0 = top in texture coords (y aumenta verso l'alto in UI: SetPixel y=0 è bottom)
            // Unity Texture2D: y=0 bottom — disegniamo da oy verso l'alto
            bool[,] on = new bool[7, 5]
            {
                { true, true, true, true, true },
                { true, false, false, false, true },
                { false, false, false, false, true },
                { false, false, true, true, true },
                { false, true, true, false, false },
                { false, false, false, false, false },
                { false, true, true, true, false }
            };

            for (int row = 0; row < 7; row++)
            for (int col = 0; col < 5; col++)
            {
                if (!on[row, col]) continue;
                int px = ox + col;
                int py = oy + (6 - row);
                if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                    tex.SetPixel(px, py, c);
            }
        }
    }
}
