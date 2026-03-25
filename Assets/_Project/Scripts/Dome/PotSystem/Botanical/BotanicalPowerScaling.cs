using UnityEngine;

namespace Sporae.Dome.PotSystem.Botanical
{
    /// <summary>Curva moltiplicatori Lv1–Lv5 (Task 4 roadmap).</summary>
    public static class BotanicalPowerScaling
    {
        private static readonly float[] Mults = { 1f, 1.18f, 1.40f, 1.68f, 2f };

        public static float MultiplierForPlantLevel(int plantLevel)
        {
            int idx = Mathf.Clamp(plantLevel, 1, 5) - 1;
            return Mults[idx];
        }
    }
}
