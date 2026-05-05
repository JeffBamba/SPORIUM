using UnityEngine;

namespace Sporae.UI.UIToolkit.HUD
{
    /// <summary>
    /// Oscillazione numerica del pH Dome allineata alla TopBar (Perlin + stesse costanti).
    /// </summary>
    public static class PhLiveDisplayMath
    {
        public const float PhCursorOscillationAmplitude = 7.02f;
        public const float PhCursorStepSize = 5f;
        public const float PhValueOscillationAmplitude = 0.5f;
        public const float PhCursorOscillationSpeed = 0.25f;
        public const float PhCursorOscillationSeed = 47.3f;

        public static float ComputeOscillatedDisplayPh(float currentPh, float time)
        {
            float noise = Mathf.PerlinNoise(time * PhCursorOscillationSpeed, PhCursorOscillationSeed);
            float offsetValue = (noise * 2f - 1f) * PhValueOscillationAmplitude;
            return Mathf.Clamp(currentPh + offsetValue, -100f, 100f);
        }

        public static float ComputeOscillatedCursorPh(float currentPh, float time)
        {
            float noise = Mathf.PerlinNoise(time * PhCursorOscillationSpeed, PhCursorOscillationSeed);
            float offsetCursor = (noise * 2f - 1f) * PhCursorOscillationAmplitude;
            return Mathf.Clamp(currentPh + offsetCursor, -100f, 100f);
        }

        public static float ComputeCursorPhStepped(float currentPh, float time)
        {
            float cursorPh = ComputeOscillatedCursorPh(currentPh, time);
            float stepped = Mathf.Round(cursorPh / PhCursorStepSize) * PhCursorStepSize;
            return Mathf.Clamp(stepped, -100f, 100f);
        }
    }
}
