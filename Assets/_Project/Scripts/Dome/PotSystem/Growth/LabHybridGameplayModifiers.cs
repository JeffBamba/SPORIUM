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

        /// <summary>
        /// Peso 0–1 per applicare i modificatori da <see cref="PotStateModel.SelectedTraitsCsv"/>:
        /// 1 = profilo Lab/ibrido; ~0,58 = pianta mutata con tratti (Task 7); 0 = nessun effetto tag.
        /// </summary>
        public static float TraitModifierBlendWeight(PotStateModel pot)
        {
            if (pot == null) return 0f;
            if (PotHasLabHybridProfile(pot)) return 1f;
            /* Pianta mutata (anche solo TraitPower / genetica): stessa pipeline tag, blending ridotto. */
            if (pot.IsMutated) return 0.52f;
            return 0f;
        }

        /// <summary>Modula drift pH + micro-aggiustamenti da tag tratti.</summary>
        public static float ScaleDailyPhDrift(float baseDrift, PotStateModel pot)
        {
            float w = TraitModifierBlendWeight(pot);
            if (w <= 0f || pot == null) return baseDrift;
            float p = Mathf.Clamp(pot.TraitPowerPercent, 1, 100);
            float mult = Mathf.Lerp(0.92f, 1.08f, p / 100f);
            var tags = GetGameplayTags(pot);
            if (tags.Contains("PH_STABILITY")) mult *= 1.04f;
            if (tags.Contains("YIELD")) mult *= 0.98f;
            mult = Mathf.Lerp(1f, mult, w);
            return baseDrift * mult;
        }

        /// <summary>Moltiplicatore cumulativo sui punti crescita (dopo condizione/pH/muffa).</summary>
        public static float GetHybridGrowthMultiplier(PotStateModel pot)
        {
            float w = TraitModifierBlendWeight(pot);
            if (w <= 0f || pot == null) return 1f;
            var tags = GetGameplayTags(pot);
            float m = 1f;
            if (tags.Contains("GROWTH")) m += 0.04f;
            if (tags.Contains("VERSATILE")) m += 0.02f;
            if (PotHasLabHybridProfile(pot) &&
                !string.IsNullOrEmpty(pot.ReagentUsedMetadata) &&
                pot.ReagentUsedMetadata.IndexOf("REAG-X", StringComparison.OrdinalIgnoreCase) >= 0)
                m += 0.02f;
            m += 0.02f * Mathf.Clamp01(pot.TraitPowerPercent / 100f);
            m = Mathf.Clamp(m, 1f, 1.14f);
            return Mathf.Lerp(1f, m, w);
        }

        /// <summary>Attenua bonus/penalità muffa verso neutro (ibridi più prevedibili).</summary>
        public static float DampenMoldGrowthMultiplier(float moldGrowthModifier, PotStateModel pot)
        {
            float w = TraitModifierBlendWeight(pot);
            if (w <= 0f || pot == null) return moldGrowthModifier;
            var tags = GetGameplayTags(pot);
            float strength = Mathf.Clamp01(pot.TraitPowerPercent / 100f);
            float retain = tags.Contains("RESILIENCE") ? 0.55f : 0.72f;
            retain *= Mathf.Lerp(0.85f, 1f, strength);
            float damped = Mathf.Lerp(1f, moldGrowthModifier, retain);
            return Mathf.Lerp(moldGrowthModifier, damped, w);
        }

        /// <summary>Scala incremento giornaliero frutti in HarvestReady (base probabilistico).</summary>
        public static float GetHarvestDailyIncrementMultiplier(PotStateModel pot)
        {
            float w = TraitModifierBlendWeight(pot);
            if (w <= 0f || pot == null) return 1f;
            var tags = GetGameplayTags(pot);
            float m = 1f;
            if (tags.Contains("YIELD")) m += 0.08f;
            if (PotHasLabHybridProfile(pot))
                m += 0.03f * Mathf.Clamp01(pot.TraitPowerPercent / 100f);
            else
                m += 0.02f * Mathf.Clamp01(pot.TraitPowerPercent / 100f);
            m = Mathf.Clamp(m, 1f, 1.15f);
            return Mathf.Lerp(1f, m, w);
        }

        /// <summary>LED “compatibile” per ibridi con tag LED_ADAPT e potenza tratto sufficiente.</summary>
        public static bool IsLedRequirementMetWithHybridTolerance(StageRequirements stageReq, PotStateModel pot)
        {
            if (stageReq == null || pot == null) return true;
            if (stageReq.IsLedRequirementMet(pot.LedSystemState)) return true;
            if (pot.LedSystemState == LedSystemState.Off) return false;
            if (!PotHasLabHybridProfile(pot) && !pot.IsMutated) return false;
            var tags = GetGameplayTags(pot);
            if (!tags.Contains("LED_ADAPT")) return false;
            bool accepted = pot.TraitPowerPercent >= 52;
            return accepted;
        }
    }
}
