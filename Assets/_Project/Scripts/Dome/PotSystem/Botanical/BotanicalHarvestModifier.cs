using _Project;
using UnityEngine;

namespace Sporae.Dome.PotSystem.Botanical
{
    /// <summary>
    /// Moltiplicatore resa harvest da tensione roster Arctic Hask (altre piante, pH fuori banda Neutra).
    /// </summary>
    public static class BotanicalHarvestModifier
    {
        public static float GetArcticTensionYieldMultiplier(PotStateModel pot, PhSystem phSystem)
        {
            if (pot == null || string.IsNullOrEmpty(pot.PlantCode) || !pot.HasPlant)
                return 1f;
            if (BotanicalPlantCodes.IsArcticHask(pot.PlantCode))
                return 1f;

            var snap = BotanicalRosterSnapshot.FromServices(phSystem);
            if (snap.TotalArcticHaskCount < 2)
                return 1f;
            if (snap.ArcticTensionMitigatedByPh)
                return 1f;
            if (snap.SterilityPressurePercent <= 0)
                return 1f;

            return Mathf.Clamp01(1f - snap.SterilityPressurePercent / 100f);
        }
    }
}
