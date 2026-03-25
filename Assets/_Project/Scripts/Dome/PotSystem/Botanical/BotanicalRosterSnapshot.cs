using System.Collections.Generic;
using _Project.Sporae.Core;
using Sporae.Dome;
using Sporae.Dome.PotSystem.Growth;
using UnityEngine;

namespace Sporae.Dome.PotSystem.Botanical
{
    /// <summary>
    /// Snapshot read-only del roster Dome (vasi attivi + cryo) per poteri botanici.
    /// Costruito da ServiceContainer — niente scan scena.
    /// </summary>
    public readonly struct BotanicalRosterSnapshot
    {
        public readonly bool AnyFerricFernActive;
        public readonly int ActiveArcticHaskCount;
        public readonly int CryoArcticHaskCount;
        public readonly int TotalArcticHaskCount;
        public readonly int GlasscapPassiveSlotCount;
        public readonly float GlasscapActiveMutationBonusSum;
        public readonly int SterilityPressurePercent;
        public readonly bool ArcticTensionMitigatedByPh;

        public BotanicalRosterSnapshot(
            bool anyFerricFernActive,
            int activeArcticHaskCount,
            int cryoArcticHaskCount,
            int glasscapPassiveSlotCount,
            float glasscapActiveMutationBonusSum,
            int sterilityPressurePercent,
            bool arcticTensionMitigatedByPh)
        {
            AnyFerricFernActive = anyFerricFernActive;
            ActiveArcticHaskCount = activeArcticHaskCount;
            CryoArcticHaskCount = cryoArcticHaskCount;
            TotalArcticHaskCount = activeArcticHaskCount + cryoArcticHaskCount;
            GlasscapPassiveSlotCount = glasscapPassiveSlotCount;
            GlasscapActiveMutationBonusSum = glasscapActiveMutationBonusSum;
            SterilityPressurePercent = sterilityPressurePercent;
            ArcticTensionMitigatedByPh = arcticTensionMitigatedByPh;
        }

        public static BotanicalRosterSnapshot FromServices(_Project.PhSystem phSystem)
        {
            bool ferric = false;
            int activeHask = 0;
            int cryoHask = 0;
            int glasscapPassive = 0;
            float glasscapImSum = 0f;

            var registry = ServiceContainer.Instance?.Get<DomePotRegistry>(suppressWarning: true);
            if (registry != null)
            {
                List<PotSlot> pots = registry.GetActivePotsSnapshot();
                for (int i = 0; i < pots.Count; i++)
                {
                    PotSlot slot = pots[i];
                    var state = slot != null && slot.PotActions != null ? slot.PotActions.PotState : null;
                    if (state == null || !state.HasPlant || state.Stage == (int)PlantStage.Empty)
                        continue;
                    string code = state.PlantCode;
                    if (BotanicalPlantCodes.IsFerricFern(code))
                        ferric = true;
                    if (BotanicalPlantCodes.IsArcticHask(code))
                        activeHask++;
                    if (BotanicalPlantCodes.IsGlasscap(code))
                        glasscapImSum += 0.10f * BotanicalPowerScaling.MultiplierForPlantLevel(Mathf.Max(1, state.PlantLevel));
                }
            }

            var cryo = ServiceContainer.Instance?.Get<CryoMachineController>(suppressWarning: true);
            var cryoSlots = cryo?.GetPassiveSlotsSnapshot();
            if (cryoSlots != null)
            {
                for (int i = 0; i < cryoSlots.Count; i++)
                {
                    var s = cryoSlots[i];
                    if (s == null || !s.IsOccupied || s.Payload == null) continue;
                    string code = s.Payload.PlantCode;
                    if (BotanicalPlantCodes.IsArcticHask(code))
                        cryoHask++;
                    if (BotanicalPlantCodes.IsGlasscap(code))
                        glasscapPassive++;
                }
            }

            int totalHask = activeHask + cryoHask;
            // Senza PhSystem non applicare tensione (evita penalità su bootstrap).
            bool phNeutral = phSystem == null || phSystem.EvaluateState() == _Project.PhSystem.PhBand.Neutral;
            bool tension = totalHask >= 2 && !phNeutral;
            int pressure = 0;
            if (tension)
            {
                float sum = 0f;
                AccumulateHaskPressure(registry, ref sum);
                AccumulateHaskPressureCryo(cryoSlots, ref sum);
                pressure = Mathf.Min(35, Mathf.RoundToInt(sum));
            }

            return new BotanicalRosterSnapshot(ferric, activeHask, cryoHask, glasscapPassive, Mathf.Clamp01(glasscapImSum), pressure, phNeutral);
        }

        private static void AccumulateHaskPressure(DomePotRegistry registry, ref float sumPercent)
        {
            if (registry == null) return;
            var pots = registry.GetActivePotsSnapshot();
            for (int i = 0; i < pots.Count; i++)
            {
                var state = pots[i]?.PotActions?.PotState;
                if (state == null || !state.HasPlant || state.Stage == (int)PlantStage.Empty) continue;
                if (!BotanicalPlantCodes.IsArcticHask(state.PlantCode)) continue;
                sumPercent += 10f * BotanicalPowerScaling.MultiplierForPlantLevel(Mathf.Max(1, state.PlantLevel));
            }
        }

        private static void AccumulateHaskPressureCryo(IReadOnlyList<CryoSlot> cryoSlots, ref float sumPercent)
        {
            if (cryoSlots == null) return;
            for (int i = 0; i < cryoSlots.Count; i++)
            {
                var s = cryoSlots[i];
                if (s == null || !s.IsOccupied || s.Payload == null) continue;
                if (!BotanicalPlantCodes.IsArcticHask(s.Payload.PlantCode)) continue;
                int lvl = Mathf.Max(1, s.Payload.PlantLevel);
                sumPercent += 10f * BotanicalPowerScaling.MultiplierForPlantLevel(lvl);
            }
        }
    }
}
