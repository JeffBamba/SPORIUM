using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using _Project.Sporae.Core;
using Sporae.DevTools;

namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// Task 6: profilo Lab/ibrido su <see cref="PotStateModel"/> influenza drift pH, crescita, muffa, resa e tolleranza LED
    /// tramite tag gameplay in <see cref="PotStateModel.SelectedTraitsCsv"/> (GROWTH, YIELD, …).
    /// </summary>
    public static class LabHybridGameplayModifiers
    {
        public static bool PotHasLabHybridProfile(PotStateModel pot)
        {
            if (pot == null) return false;
            if (pot.IsHybrid) return true;
            if (!string.IsNullOrEmpty(pot.SourcePlantCodesMetadata) && pot.SourcePlantCodesMetadata.Contains("|"))
                return true;
            if (!string.IsNullOrWhiteSpace(pot.PlantFamilyMetadata) &&
                pot.PlantFamilyMetadata.StartsWith("HYBRID-WEAK", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        /// <summary>
        /// Quale <see cref="PlantData"/> usare per idratazione / LED / fertilizzante (stadi) quando il seme ha profilo Lab.
        /// </summary>
        public static PlantData ResolvePlantDataForCareRequirements(PotStateModel pot, PlantData speciesPlantData)
        {
            if (pot == null || speciesPlantData == null) return speciesPlantData;
            string profile = pot.LabCareProfileMetadata?.Trim();
            if (string.IsNullOrEmpty(profile) ||
                string.Equals(profile, "BLEND", StringComparison.OrdinalIgnoreCase))
                return speciesPlantData;

            var codes = ParseSourcePlantCodes(pot.SourcePlantCodesMetadata);
            if (codes.Count == 0) return speciesPlantData;

            if (string.Equals(profile, "PARENT_A", StringComparison.OrdinalIgnoreCase))
            {
                var d = PlantDatabase.Instance != null ? PlantDatabase.Instance.GetPlantDataByCode(codes[0]) : null;
                return d ?? speciesPlantData;
            }

            if (string.Equals(profile, "PARENT_B", StringComparison.OrdinalIgnoreCase))
            {
                string code = codes.Count >= 2 ? codes[1] : codes[0];
                var d = PlantDatabase.Instance != null ? PlantDatabase.Instance.GetPlantDataByCode(code) : null;
                return d ?? speciesPlantData;
            }

            return speciesPlantData;
        }

        static List<string> ParseSourcePlantCodes(string meta)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(meta)) return list;
            foreach (var part in meta.Split('|'))
            {
                string t = part?.Trim();
                if (!string.IsNullOrEmpty(t)) list.Add(t);
            }
            return list;
        }

        public static HashSet<string> GetGameplayTags(PotStateModel pot)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (pot == null || string.IsNullOrWhiteSpace(pot.SelectedTraitsCsv)) return set;
            foreach (var tok in ItemFabric.ParseTraits(pot.SelectedTraitsCsv))
            {
                if (string.IsNullOrWhiteSpace(tok)) continue;
                set.Add(tok.Trim().ToUpperInvariant());
            }
            return set;
        }

        /// <summary>Modula drift pH + micro-aggiustamenti da tag tratti.</summary>
        public static float ScaleDailyPhDrift(float baseDrift, PotStateModel pot)
        {
            if (pot == null || !PotHasLabHybridProfile(pot)) return baseDrift;
            float p = Mathf.Clamp(pot.TraitPowerPercent, 1, 100);
            float mult = Mathf.Lerp(0.92f, 1.08f, p / 100f);
            var tags = GetGameplayTags(pot);
            if (tags.Contains("PH_STABILITY")) mult *= 1.04f;
            if (tags.Contains("YIELD")) mult *= 0.98f;
            float scaled = baseDrift * mult;
            return scaled;
        }

        /// <summary>Moltiplicatore cumulativo sui punti crescita (dopo condizione/pH/muffa).</summary>
        public static float GetHybridGrowthMultiplier(PotStateModel pot)
        {
            if (!PotHasLabHybridProfile(pot)) return 1f;
            var tags = GetGameplayTags(pot);
            float m = 1f;
            if (tags.Contains("GROWTH")) m += 0.04f;
            if (tags.Contains("VERSATILE")) m += 0.02f;
            if (!string.IsNullOrEmpty(pot.ReagentUsedMetadata) &&
                pot.ReagentUsedMetadata.IndexOf("REAG-X", StringComparison.OrdinalIgnoreCase) >= 0)
                m += 0.02f;
            m += 0.02f * Mathf.Clamp01(pot.TraitPowerPercent / 100f);
            return Mathf.Clamp(m, 1f, 1.14f);
        }

        /// <summary>Attenua bonus/penalità muffa verso neutro (ibridi più prevedibili).</summary>
        public static float DampenMoldGrowthMultiplier(float moldGrowthModifier, PotStateModel pot)
        {
            if (!PotHasLabHybridProfile(pot)) return moldGrowthModifier;
            var tags = GetGameplayTags(pot);
            float strength = Mathf.Clamp01(pot.TraitPowerPercent / 100f);
            float retain = tags.Contains("RESILIENCE") ? 0.55f : 0.72f;
            retain *= Mathf.Lerp(0.85f, 1f, strength);
            return Mathf.Lerp(1f, moldGrowthModifier, retain);
        }

        /// <summary>Scala incremento giornaliero frutti in HarvestReady (base probabilistico).</summary>
        public static float GetHarvestDailyIncrementMultiplier(PotStateModel pot)
        {
            if (!PotHasLabHybridProfile(pot)) return 1f;
            var tags = GetGameplayTags(pot);
            float m = 1f;
            if (tags.Contains("YIELD")) m += 0.08f;
            m += 0.03f * Mathf.Clamp01(pot.TraitPowerPercent / 100f);
            return Mathf.Clamp(m, 1f, 1.15f);
        }

        /// <summary>LED “compatibile” per ibridi con tag LED_ADAPT e potenza tratto sufficiente.</summary>
        public static bool IsLedRequirementMetWithHybridTolerance(StageRequirements stageReq, PotStateModel pot)
        {
            if (stageReq == null || pot == null) return true;
            if (stageReq.IsLedRequirementMet(pot.LedSystemState)) return true;
            if (pot.LedSystemState == LedSystemState.Off) return false;
            if (!PotHasLabHybridProfile(pot)) return false;
            var tags = GetGameplayTags(pot);
            if (!tags.Contains("LED_ADAPT")) return false;
            bool accepted = pot.TraitPowerPercent >= 52;
            return accepted;
        }
    }
}
