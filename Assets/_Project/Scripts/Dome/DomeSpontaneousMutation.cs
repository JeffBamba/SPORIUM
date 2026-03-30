using System;
using System.Collections.Generic;
using UnityEngine;
using _Project;
using _Project.Sporae.Core;
using Sporae.Dome.PotSystem.Condition;
using Sporae.Dome.PotSystem.Growth;
using Sporae.UI.UIToolkit.NotificationsFoundation;

namespace Sporae.Dome
{
    /// <summary>
    /// Task 7 — mutazione spontanea giornaliera: probabilità legata a IM Dome, genetica, muffa/stress, fascia pH;
    /// agisce su <see cref="PotStateModel"/> con tag gameplay già noti a <see cref="LabHybridGameplayModifiers"/>.
    /// Pool tratti: <see cref="MutationTraitCatalog"/> in Resources (data-driven) oppure fallback builtin.
    /// </summary>
    /// <remarks>
    /// <b>Stacking / deduplicazione (Task 7)</b>
    /// <list type="bullet">
    /// <item><description>Token in <see cref="PotStateModel.SelectedTraitsCsv"/>: nessun duplicato dello stesso tag in CSV (pick solo tag mancanti).</description></item>
    /// <item><description>Poteri nativi su PlantData restano; i tag runtime si combinano via <see cref="LabHybridGameplayModifiers"/> (blend pieno ibrido, ~0.52 se <see cref="PotStateModel.IsMutated"/>).</description></item>
    /// <item><description><see cref="PotStateModel.TraitPowerPercent"/>: solo incrementi controllati dal roll (clamp 1–160).</description></item>
    /// <item><description><see cref="PotStateModel.PlantGeneticType"/>: <see cref="GeneticType.Fixed"/> escluso dal roll; transizione esplicita Stable→Unstable in un esito.</description></item>
    /// </list>
    /// </remarks>
    public static class DomeSpontaneousMutation
    {
        static readonly (string tag, float wStd, float wPure, float wEvil)[] BuiltinWeightedTagPool =
        {
            ("GROWTH", 1f, 0.9f, 1.15f),
            ("YIELD", 1.1f, 1.05f, 0.85f),
            ("RESILIENCE", 1f, 1.25f, 1.1f),
            ("PH_STABILITY", 1f, 1.35f, 0.6f),
            ("LED_ADAPT", 1f, 0.85f, 1.2f),
            ("VERSATILE", 1f, 1f, 1f),
        };

        static readonly List<(string tag, float wStd, float wPure, float wEvil)> ScratchWeightedPool = new List<(string tag, float wStd, float wPure, float wEvil)>(12);

        static int _lastMutationWatchToastDay = int.MinValue;

        public static void ProcessEndOfDay(
            IReadOnlyList<PotStateModel> pots,
            float mutationIndex01,
            FoundationNotificationService notifications,
            DomePotRegistry registry,
            PhSystem phSystem = null,
            int gameDay = 0)
        {
            if (pots == null || pots.Count == 0)
                return;

            float im = Mathf.Clamp01(mutationIndex01);
            PhSystem.PhBand domeBand = phSystem != null ? phSystem.EvaluateState() : PhSystem.PhBand.Neutral;

            var catalog = Resources.Load<MutationTraitCatalog>("MutationTraitCatalog");
            int minLv = catalog != null ? Mathf.Max(1, catalog.minPlantLevelForSpontaneousMutation) : 1;
            int maxLv = catalog != null && catalog.maxPlantLevelForSpontaneousMutation > 0
                ? catalog.maxPlantLevelForSpontaneousMutation
                : int.MaxValue;

            int eligiblePressure = CountEligibleForMutationWindow(pots, minLv, maxLv);
            TryPostMutationWatchToast(notifications, catalog, gameDay, im, eligiblePressure);
            FillResolvedWeightedPool(catalog);

            foreach (var pot in pots)
            {
                if (pot == null || !pot.HasPlant) continue;
                if ((PlantCondition)pot.ConditionLabel == PlantCondition.Morta) continue;
                if (pot.Stage == (int)PlantStage.Empty) continue;
                if (pot.PlantGeneticType == GeneticType.Fixed) continue;
                if (pot.PlantLevel < minLv || pot.PlantLevel > maxLv) continue;

                float rollChance = Mathf.Lerp(0.0035f, 0.055f, im);
                if (pot.PlantGeneticType == GeneticType.Unstable)
                    rollChance *= 1.45f;
                if (pot.MoldRiskLevel >= 2)
                    rollChance += 0.012f;
                if (pot.IsInfested)
                    rollChance += 0.018f;
                if (pot.IsHybrid)
                    rollChance *= 0.72f;

                rollChance += GetPhPressureMutationBonus(domeBand, pot);

                rollChance = Mathf.Clamp(rollChance, 0f, 0.12f);
                if (UnityEngine.Random.value > rollChance)
                    continue;

                if (!TryApplyOneMutationOutcome(pot, out string detailIt))
                    continue;

                pot.IsMutated = true;
                RaisePotUiRefresh(pot.PotId, registry);
                PostFoundationMutationToast(notifications, pot, detailIt);
            }
        }

        static int CountEligibleForMutationWindow(IReadOnlyList<PotStateModel> pots, int minLv, int maxLv)
        {
            int n = 0;
            for (int i = 0; i < pots.Count; i++)
            {
                var pot = pots[i];
                if (pot == null || !pot.HasPlant) continue;
                if ((PlantCondition)pot.ConditionLabel == PlantCondition.Morta) continue;
                if (pot.Stage == (int)PlantStage.Empty) continue;
                if (pot.PlantGeneticType == GeneticType.Fixed) continue;
                if (pot.PlantLevel < minLv || pot.PlantLevel > maxLv) continue;
                n++;
            }

            return n;
        }

        static void TryPostMutationWatchToast(
            FoundationNotificationService notifications,
            MutationTraitCatalog catalog,
            int gameDay,
            float im,
            int eligiblePlants)
        {
            if (notifications == null || !notifications.Enabled || gameDay <= 0 || eligiblePlants <= 0)
                return;
            float minIm = catalog != null ? Mathf.Clamp01(catalog.watchToastMinIm) : 0.42f;
            float chance = catalog != null ? Mathf.Clamp01(catalog.watchToastChance) : 0.22f;
            if (im < minIm || UnityEngine.Random.value > chance)
                return;
            if (_lastMutationWatchToastDay == gameDay)
                return;
            _lastMutationWatchToastDay = gameDay;
            int pct = Mathf.RoundToInt(im * 100f);
            notifications.PostToast(
                "DOME-MUT-WATCH",
                new NotificationPayload().With("pct", pct.ToString()),
                dedupKey: $"dome-mut-watch-d{gameDay}");
        }

        static void FillResolvedWeightedPool(MutationTraitCatalog catalog)
        {
            ScratchWeightedPool.Clear();
            if (catalog != null && catalog.HasRuntimeRows)
            {
                for (int i = 0; i < catalog.Rows.Count; i++)
                {
                    var r = catalog.Rows[i];
                    if (r == null || string.IsNullOrWhiteSpace(r.gameplayTag)) continue;
                    string t = r.gameplayTag.Trim().ToUpperInvariant();
                    ScratchWeightedPool.Add((t, r.weightStandard, r.weightPure, r.weightEvil));
                }
            }

            if (ScratchWeightedPool.Count == 0)
            {
                for (int i = 0; i < BuiltinWeightedTagPool.Length; i++)
                    ScratchWeightedPool.Add(BuiltinWeightedTagPool[i]);
            }
        }

        static float GetPhPressureMutationBonus(PhSystem.PhBand domeBand, PotStateModel pot)
        {
            float bonus = 0f;
            switch (domeBand)
            {
                case PhSystem.PhBand.UltraAcid:
                case PhSystem.PhBand.UltraBasic:
                    bonus += 0.014f;
                    break;
                case PhSystem.PhBand.StableAcid:
                case PhSystem.PhBand.StableBasic:
                    bonus += 0.006f;
                    break;
            }

            PlantFamily fam = ResolvePotFamily(pot);
            if (domeBand == PhSystem.PhBand.UltraBasic && fam == PlantFamily.Pure)
                bonus += 0.004f;
            if (domeBand == PhSystem.PhBand.UltraAcid && fam == PlantFamily.Evil)
                bonus += 0.004f;

            return bonus;
        }

        static PlantFamily ResolvePotFamily(PotStateModel pot)
        {
            if (pot == null) return PlantFamily.Standard;
            if (PlantDatabase.Instance != null && !string.IsNullOrEmpty(pot.PlantCode))
            {
                var pd = PlantDatabase.Instance.GetPlantDataByCode(pot.PlantCode);
                if (pd != null) return pd.Family;
            }

            if (!string.IsNullOrWhiteSpace(pot.PlantFamilyMetadata))
            {
                string s = pot.PlantFamilyMetadata.Trim().ToUpperInvariant();
                if (s.Contains("PURE")) return PlantFamily.Pure;
                if (s.Contains("EVIL")) return PlantFamily.Evil;
            }

            return PlantFamily.Standard;
        }

        static bool TryApplyOneMutationOutcome(PotStateModel pot, out string detailIt)
        {
            detailIt = null;
            var existing = ItemFabric.ParseTraits(pot.SelectedTraitsCsv);
            var tagSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in existing)
                tagSet.Add(t.Trim().ToUpperInvariant());

            PlantFamily fam = ResolvePotFamily(pot);
            int mode = UnityEngine.Random.Range(0, 100);
            if (mode < 48)
            {
                string pick = PickMissingTagWeighted(tagSet, fam);
                if (pick == null)
                    mode = 48;
                else
                {
                    pot.SelectedTraitsCsv = string.IsNullOrWhiteSpace(pot.SelectedTraitsCsv)
                        ? pick
                        : pot.SelectedTraitsCsv.TrimEnd() + "," + pick;
                    detailIt = $"Nuovo tratto spontaneo: {pick}. Influenza crescita / drift / resa in modo leggero.";
                    return true;
                }
            }

            if (mode < 78)
            {
                int delta = UnityEngine.Random.Range(3, 9);
                pot.TraitPowerPercent = Mathf.Clamp(pot.TraitPowerPercent + delta, 1, 160);
                detailIt = $"Potenza tratti spostata di +{delta}% (TraitPower).";
                return true;
            }

            if (mode < 90 && pot.PlantGeneticType == GeneticType.Stable)
            {
                pot.PlantGeneticType = GeneticType.Unstable;
                detailIt = "Genetica spostata da Stabile a Instabile: maggiore variabilità futura.";
                return true;
            }

            int scoreDelta = UnityEngine.Random.Range(-7, 11);
            pot.ConditionScore = Mathf.Clamp(pot.ConditionScore + scoreDelta, 0, 100);
            detailIt = scoreDelta >= 0
                ? $"Condizione adattata (+{scoreDelta} score) dopo stress evolutivo."
                : $"Condizione sotto pressione ({scoreDelta} score).";
            return true;
        }

        static string PickMissingTagWeighted(HashSet<string> tagSet, PlantFamily fam)
        {
            float Weight((string tag, float wStd, float wPure, float wEvil) e)
            {
                return fam switch
                {
                    PlantFamily.Pure => e.wPure,
                    PlantFamily.Evil => e.wEvil,
                    _ => e.wStd,
                };
            }

            float sum = 0f;
            for (int i = 0; i < ScratchWeightedPool.Count; i++)
            {
                var e = ScratchWeightedPool[i];
                if (tagSet.Contains(e.tag)) continue;
                sum += Weight(e);
            }

            if (sum <= 0f) return null;
            float r = UnityEngine.Random.value * sum;
            for (int i = 0; i < ScratchWeightedPool.Count; i++)
            {
                var e = ScratchWeightedPool[i];
                if (tagSet.Contains(e.tag)) continue;
                float w = Weight(e);
                r -= w;
                if (r <= 0f) return e.tag;
            }

            return null;
        }

        static void RaisePotUiRefresh(string potId, DomePotRegistry registry)
        {
            if (registry == null || string.IsNullOrEmpty(potId)) return;
            var slot = registry.FindPotById(potId);
            if (slot != null)
                PotEvents.EmitChanged(slot);
        }

        static string ResolvePlantDisplayNameForToast(PotStateModel pot)
        {
            if (pot == null) return "Pianta";
            if (PlantDatabase.Instance != null && !string.IsNullOrWhiteSpace(pot.PlantCode))
            {
                var pd = PlantDatabase.Instance.GetPlantDataByCode(pot.PlantCode);
                if (pd != null && !string.IsNullOrWhiteSpace(pd.name))
                    return pd.name;
            }

            return string.IsNullOrWhiteSpace(pot.PlantCode) ? "Pianta" : pot.PlantCode;
        }

        static void PostFoundationMutationToast(
            FoundationNotificationService notifications,
            PotStateModel pot,
            string detailIt)
        {
            if (notifications == null || !notifications.Enabled) return;

            string plantName = !string.IsNullOrWhiteSpace(pot.CustomPlantName)
                ? pot.CustomPlantName.Trim()
                : ResolvePlantDisplayNameForToast(pot);

            var payload = new NotificationPayload()
                .With("plantName", plantName)
                .With("potId", pot.PotId ?? "—")
                .With("detail", detailIt);

            notifications.PostToast("DOME-MUT-PLANT", payload, dedupKey: $"mut:pot:{pot.PotId}");
        }
    }
}
