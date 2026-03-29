using System;
using System.Collections.Generic;
using System.Text;
using _Project;
using _Project.Sporae.Core;
using Sporae.Dome;
using Sporae.Dome.PotSystem.Growth;
using UnityEngine;

namespace Sporae.Dome.PotSystem.Botanical
{
    /// <summary>
    /// Testi e riepiloghi UI condivisi (STATUS, HUD, tooltip TopBar) — stessa semantica del roster snapshot.
    /// </summary>
    public static class BotanicalPowerFacade
    {
        /// <summary>
        /// Tooltip TopBar — «Effetti globali»: testi da PlantData solo per l’ambito attuale.
        /// Vaso attivo → solo potere Attivo; cryo passivo → solo potere Passivo. Blocchi senza testo applicabile omessi.
        /// </summary>
        public static void AppendDomeGlobalPlantPowersTooltipLines(List<string> lines, PhSystem phSystem = null)
        {
            if (lines == null) return;

            var registry = ServiceContainer.Instance?.Get<DomePotRegistry>(suppressWarning: true);
            var cryoCtrl = ServiceContainer.Instance?.Get<CryoMachineController>(suppressWarning: true);
            var cryoSlots = cryoCtrl?.GetPassiveSlotsSnapshot();

            var task4Active = new List<(string potId, string plantCode)>();
            if (registry != null)
            {
                var pots = registry.GetActivePotsSnapshot();
                for (int i = 0; i < pots.Count; i++)
                {
                    var slot = pots[i];
                    var state = slot != null && slot.PotActions != null ? slot.PotActions.PotState : null;
                    if (state == null || !state.HasPlant || state.Stage == (int)PlantStage.Empty)
                        continue;
                    string code = state.PlantCode;
                    if (!IsGlobalDomeBotanicalCode(code))
                        continue;
                    string potId = string.IsNullOrEmpty(slot.PotId) ? "—" : slot.PotId;
                    task4Active.Add((potId, code));
                }
            }

            task4Active.Sort(CompareTask4PotEntries);

            bool any = false;
            for (int i = 0; i < task4Active.Count; i++)
            {
                var (potId, plantCode) = task4Active[i];
                if (AppendGlobalEffectBlockForActivePot(lines, potId, plantCode))
                    any = true;
            }

            int activeArctic = 0;
            for (int i = 0; i < task4Active.Count; i++)
            {
                if (BotanicalPlantCodes.IsArcticHask(task4Active[i].plantCode))
                    activeArctic++;
            }

            var cryoArcticSlotIds = new List<string>();
            if (cryoSlots != null)
            {
                for (int i = 0; i < cryoSlots.Count; i++)
                {
                    var s = cryoSlots[i];
                    if (s == null || !s.IsOccupied || s.Payload == null)
                        continue;
                    if (BotanicalPlantCodes.IsGlasscap(s.Payload.PlantCode))
                    {
                        if (AppendGlobalEffectBlockForCryoSlot(lines, s.SlotId, BotanicalPlantCodes.GlasscapFungus))
                            any = true;
                    }

                    if (BotanicalPlantCodes.IsArcticHask(s.Payload.PlantCode))
                        cryoArcticSlotIds.Add(string.IsNullOrEmpty(s.SlotId) ? "—" : s.SlotId);
                }
            }

            cryoArcticSlotIds.Sort(StringComparer.OrdinalIgnoreCase);
            if (cryoArcticSlotIds.Count > 0)
            {
                if (activeArctic <= 0)
                {
                    var data = PlantDatabase.Instance != null
                        ? PlantDatabase.Instance.GetPlantDataByCode(BotanicalPlantCodes.ArcticHask)
                        : null;
                    for (int i = 0; i < cryoArcticSlotIds.Count; i++)
                    {
                        if (AppendGlobalEffectBlockForCryoArcticSlot(lines, cryoArcticSlotIds[i], data))
                            any = true;
                    }
                }
                else
                {
                    lines.Add("  • Arctic Hask in cryo passivo: " + string.Join(", ", cryoArcticSlotIds) +
                              " (conta per tensione roster; effetto «Attivo» solo nei vasi)");
                    any = true;
                }
            }

            if (!any)
                lines.Add("  <color=#8FA0A6>Nessuna specie Task 4 con poteri globali Dome nei vasi o in cryo passivo.</color>");

            // Tensione roster Arctic Hask: warning persistente se attiva (≥2 esemplari + pH fuori Neutra)
            var snap = BotanicalRosterSnapshot.FromServices(phSystem);
            if (snap.TotalArcticHaskCount >= 2 && !snap.ArcticTensionMitigatedByPh)
            {
                lines.Add("");
                lines.Add($"  <color=#D46060>⚠ TENSIONE ARCTIC HASK ATTIVA ({snap.TotalArcticHaskCount} esemplari)</color>");
                lines.Add($"  <color=#D46060>  Penalità raccolto ~{snap.SterilityPressurePercent}% su piante non-Arctic.</color>");
                lines.Add("  <color=#8FA0A6>  Mitiga portando pH in Neutra o riducendo gli Arctic attivi.</color>");
            }
        }

        private static int CompareTask4PotEntries((string potId, string plantCode) a, (string potId, string plantCode) b)
        {
            int oa = Task4SpeciesSortOrder(a.plantCode);
            int ob = Task4SpeciesSortOrder(b.plantCode);
            int c = oa.CompareTo(ob);
            if (c != 0) return c;
            return string.Compare(a.potId, b.potId, StringComparison.OrdinalIgnoreCase);
        }

        private static int Task4SpeciesSortOrder(string code)
        {
            if (BotanicalPlantCodes.IsFerricFern(code)) return 0;
            if (BotanicalPlantCodes.IsArcticHask(code)) return 1;
            if (BotanicalPlantCodes.IsGlasscap(code)) return 2;
            return 99;
        }

        private static bool AppendGlobalEffectBlockForActivePot(List<string> lines, string potId, string plantCode)
        {
            string species = BotanicalPlantCodes.GetSpeciesUiDisplayName(plantCode) ?? plantCode;
            var data = PlantDatabase.Instance != null ? PlantDatabase.Instance.GetPlantDataByCode(plantCode) : null;
            if (data == null)
            {
                lines.Add($"  <color=#C8F5C8>{species} - {potId}</color>");
                lines.Add("  • (PlantData non disponibile)");
                return true;
            }

            if (string.IsNullOrWhiteSpace(NormalizeTooltipCopy(data.ActivePower)))
                return false;

            lines.Add($"  <color=#C8F5C8>{species} - {potId}</color>");
            AppendWrappedBulletLines(lines, "Attivo", data.ActivePower);
            return true;
        }

        private static bool AppendGlobalEffectBlockForCryoSlot(List<string> lines, string slotId, string plantCode)
        {
            string species = BotanicalPlantCodes.GetSpeciesUiDisplayName(plantCode) ?? plantCode;
            string sid = string.IsNullOrEmpty(slotId) ? "—" : slotId;
            var data = PlantDatabase.Instance != null ? PlantDatabase.Instance.GetPlantDataByCode(plantCode) : null;
            if (data == null)
            {
                lines.Add($"  <color=#C8F5C8>{species} - {sid}</color> <color=#8FA0A6>(cryo passivo)</color>");
                lines.Add("  • (PlantData non disponibile)");
                return true;
            }

            if (string.IsNullOrWhiteSpace(NormalizeTooltipCopy(data.PassivePower)))
                return false;

            lines.Add($"  <color=#C8F5C8>{species} - {sid}</color> <color=#8FA0A6>(cryo passivo)</color>");
            AppendWrappedBulletLines(lines, "Passivo (cryo)", data.PassivePower);
            return true;
        }

        private static bool AppendGlobalEffectBlockForCryoArcticSlot(List<string> lines, string slotId, PlantData data)
        {
            string species = BotanicalPlantCodes.GetSpeciesUiDisplayName(BotanicalPlantCodes.ArcticHask);
            string sid = string.IsNullOrEmpty(slotId) ? "—" : slotId;
            if (data == null)
            {
                lines.Add($"  <color=#C8F5C8>{species} - {sid}</color> <color=#8FA0A6>(cryo passivo)</color>");
                lines.Add("  • (PlantData non disponibile)");
                return true;
            }

            if (string.IsNullOrWhiteSpace(NormalizeTooltipCopy(data.PassivePower)))
                return false;

            lines.Add($"  <color=#C8F5C8>{species} - {sid}</color> <color=#8FA0A6>(cryo passivo)</color>");
            AppendWrappedBulletLines(lines, "Passivo (cryo)", data.PassivePower);
            return true;
        }

        /// <summary>Corregge sequenze letterali tipo \xE0 da asset/testo legacy.</summary>
        private static string NormalizeTooltipCopy(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("\\xE0", "à", StringComparison.OrdinalIgnoreCase)
                .Replace("\\xE8", "è", StringComparison.OrdinalIgnoreCase)
                .Replace("\\xEC", "ì", StringComparison.OrdinalIgnoreCase)
                .Replace("\\xF2", "ò", StringComparison.OrdinalIgnoreCase)
                .Replace("\\xF9", "ù", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>A capo con rientro allineato sotto il testo dopo «• Attivo: ».</summary>
        private static void AppendWrappedBulletLines(List<string> lines, string label, string text)
        {
            text = NormalizeTooltipCopy(text);
            if (string.IsNullOrWhiteSpace(text))
                return;

            const int totalLineWidth = 52;
            string head = $"  • {label}: ";
            string indent = new string(' ', head.Length);
            var words = text.Trim().Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            var current = new StringBuilder();
            bool isFirstPhysicalLine = true;

            void FlushLine()
            {
                if (current.Length == 0) return;
                lines.Add(isFirstPhysicalLine ? head + current : indent + current);
                current.Clear();
                isFirstPhysicalLine = false;
            }

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                string trial = current.Length == 0 ? word : current + " " + word;
                string prefix = isFirstPhysicalLine ? head : indent;
                if (prefix.Length + trial.Length > totalLineWidth && current.Length > 0)
                    FlushLine();
                if (current.Length > 0) current.Append(' ');
                current.Append(word);
            }

            FlushLine();
        }

        private static bool IsGlobalDomeBotanicalCode(string code) =>
            BotanicalPlantCodes.IsFerricFern(code) || BotanicalPlantCodes.IsArcticHask(code) || BotanicalPlantCodes.IsGlasscap(code);

        public static void AppendStatusEffectLinesForPot(List<string> outLines, string potId, PhSystem phSystem)
        {
            if (outLines == null || string.IsNullOrEmpty(potId)) return;

            var registry = ServiceContainer.Instance?.Get<DomePotRegistry>(suppressWarning: true);
            PotSlot potSlot = registry?.FindPotById(potId);
            var self = potSlot?.PotActions?.PotState;
            var selfData = self?.GetPlantData();
            var snap = BotanicalRosterSnapshot.FromServices(phSystem);

            outLines.Add("§TITLE§EFFETTI (questa pianta)§END§");
            if (self == null || !self.HasPlant || selfData == null)
            {
                outLines.Add("§INFO§—§END§");
                return;
            }

            bool emitted = false;
            if (BotanicalPlantCodes.IsFerricFern(self.PlantCode))
            {
                emitted = true;
                outLines.Add("§INFO§  • Aura: −10% (×0,9 floor) giorni muffa oltre soglia su tutti i vasi attivi§END§");
            }
            if (BotanicalPlantCodes.IsArcticHask(self.PlantCode))
            {
                emitted = true;
                outLines.Add("§INFO§  • +5 pH/g da questo vaso (oltre al drift famiglia)§END§");
                outLines.Add("§INFO§  • Ogni 2 giorni: −1 livello rischio muffa su ogni vaso attivo§END§");
            }
            if (BotanicalPlantCodes.IsGlasscap(self.PlantCode))
            {
                emitted = true;
                float m = 0.10f * BotanicalPowerScaling.MultiplierForPlantLevel(Mathf.Max(1, self.PlantLevel));
                outLines.Add($"§INFO§  • +{Mathf.RoundToInt(m * 100f)}% IM globale (additivo, clamp)§END§");
            }
            if (!emitted)
                outLines.Add("§INFO§  • (nessun potere attivo Task 4 su questa specie)§END§");

            outLines.Add("§TITLE§SUBITI (da altre piante / cryo)§END§");
            bool any = false;
            if (snap.AnyFerricFernActive && !BotanicalPlantCodes.IsFerricFern(self.PlantCode))
            {
                any = true;
                outLines.Add("§INFO§  • Ferric Fern altrove: muffa oltre soglia ridotta (×0,9)§END§");
            }
            int otherHask = Mathf.Max(0, snap.ActiveArcticHaskCount - (BotanicalPlantCodes.IsArcticHask(self.PlantCode) ? 1 : 0));
            if (otherHask > 0)
            {
                any = true;
                outLines.Add($"§INFO§  • Altri Arctic Hask attivi ({otherHask}): +5 pH/g per vaso; pulizia muffa globale ogni 2 giorni se c'\u00E8 almeno un Hask attivo§END§");
            }
            if (snap.GlasscapPassiveSlotCount > 0)
            {
                any = true;
                outLines.Add($"§INFO§  • Glasscap in cryo ({snap.GlasscapPassiveSlotCount}): +15% peso giorni muffa ciascuno§END§");
            }
            if (snap.GlasscapActiveMutationBonusSum > 0.0001f && !BotanicalPlantCodes.IsGlasscap(self.PlantCode))
            {
                any = true;
                outLines.Add("§INFO§  • Glasscap attivo altrove: bonus IM globale§END§");
            }
            if (!BotanicalPlantCodes.IsArcticHask(self.PlantCode) && snap.TotalArcticHaskCount >= 2 && !snap.ArcticTensionMitigatedByPh)
            {
                any = true;
                outLines.Add($"§WARN§  • Tensione Arctic Hask: penalità resa raccolto ~{snap.SterilityPressurePercent}% (rientra con pH Neutro)§END§");
            }
            if (!any)
                outLines.Add("§INFO§  • Nessun effetto incrociato rilevante§END§");
        }

        public static string BuildPcv3CenterEffectsText(string potId, PhSystem phSystem)
        {
            var lines = new List<string>();
            AppendPlainPotEffectLines(lines, potId, phSystem);
            return string.Join("\n", lines);
        }

        private static void AppendPlainPotEffectLines(List<string> lines, string potId, PhSystem phSystem)
        {
            var registry = ServiceContainer.Instance?.Get<DomePotRegistry>(suppressWarning: true);
            PotSlot potSlot = registry?.FindPotById(potId);
            var self = potSlot?.PotActions?.PotState;
            var selfData = self?.GetPlantData();
            var snap = BotanicalRosterSnapshot.FromServices(phSystem);

            lines.Add("── Potere ──");
            if (self == null || !self.HasPlant || selfData == null)
            {
                lines.Add("—");
                return;
            }
            bool em = false;
            if (BotanicalPlantCodes.IsFerricFern(self.PlantCode))
            {
                em = true;
                lines.Add("• Aura −muffa dome (×0,9 giorni oltre soglia)");
            }
            if (BotanicalPlantCodes.IsArcticHask(self.PlantCode))
            {
                em = true;
                lines.Add("• +5 pH/g (vaso)");
                lines.Add("• −1 muffa tutti i vasi / 2 giorni");
            }
            if (BotanicalPlantCodes.IsGlasscap(self.PlantCode))
            {
                em = true;
                float m = 0.10f * BotanicalPowerScaling.MultiplierForPlantLevel(Mathf.Max(1, self.PlantLevel));
                lines.Add($"• IM globale +{Mathf.RoundToInt(m * 100f)}%");
            }
            if (!em)
                lines.Add("(vedi PlantData)");

            lines.Add("── Subiti ──");
            bool any = false;
            if (snap.AnyFerricFernActive && !BotanicalPlantCodes.IsFerricFern(self.PlantCode))
            {
                lines.Add("• Riduzione muffa da Ferric altrove");
                any = true;
            }
            if (snap.GlasscapPassiveSlotCount > 0)
            {
                lines.Add($"• +muffa da Glasscap cryo (×{snap.GlasscapPassiveSlotCount})");
                any = true;
            }
            if (!BotanicalPlantCodes.IsArcticHask(self.PlantCode) && snap.TotalArcticHaskCount >= 2 && !snap.ArcticTensionMitigatedByPh)
            {
                lines.Add($"• Tensione Hask: −{snap.SterilityPressurePercent}% resa raccolto");
                any = true;
            }
            if (!any)
                lines.Add("—");
        }

        /// <summary>Dome Status HUD — vaso attivo: solo Attivo (PlantData); Subiti solo se applicabili ora.</summary>
        public static void AppendDomeHudTooltipLines(List<BotanicalHudTooltipLine> lines, PotStateModel state, PlantData plantData, in BotanicalRosterSnapshot snap)
        {
            if (lines == null || state == null) return;

            bool t4 = IsGlobalDomeBotanicalCode(state.PlantCode);
            string activeTxt = plantData != null ? NormalizeTooltipCopy(plantData.ActivePower) : null;
            bool hasActiveCopy = !string.IsNullOrWhiteSpace(activeTxt);

            if (t4 || hasActiveCopy)
            {
                lines.Add(new BotanicalHudTooltipLine("── Poteri (vaso attivo) ──", BotanicalHudTooltipPalette.TipPhCyan, true));
                if (hasActiveCopy)
                    lines.Add(new BotanicalHudTooltipLine($"  Attivo: {activeTxt}", BotanicalHudTooltipPalette.TipMuted));
                else if (t4)
                    lines.Add(new BotanicalHudTooltipLine("  (nessun testo Attivo su PlantData)", BotanicalHudTooltipPalette.TipMuted));
            }

            lines.Add(new BotanicalHudTooltipLine("── Subiti (adesso) ──", BotanicalHudTooltipPalette.TipPhCyan, true));
            bool sub = false;
            bool moldRelevant = state.MoldRiskLevel >= 1 || state.DaysOverwateringConsecutive > 0;

            if (snap.AnyFerricFernActive && !BotanicalPlantCodes.IsFerricFern(state.PlantCode) && moldRelevant)
            {
                lines.Add(new BotanicalHudTooltipLine("    • Muffa: beneficio da Ferric Fern attivo altrove (×0,9 giorni oltre soglia)", BotanicalHudTooltipPalette.TipMuted));
                sub = true;
            }
            if (snap.GlasscapPassiveSlotCount > 0 && moldRelevant)
            {
                lines.Add(new BotanicalHudTooltipLine($"    • Muffa: {snap.GlasscapPassiveSlotCount}× Glasscap in cryo (×1,15 sul peso giorni)", BotanicalHudTooltipPalette.TipYellow));
                sub = true;
            }
            if (!BotanicalPlantCodes.IsArcticHask(state.PlantCode) && snap.TotalArcticHaskCount >= 2 && !snap.ArcticTensionMitigatedByPh)
            {
                lines.Add(new BotanicalHudTooltipLine($"    • Raccolto: −{snap.SterilityPressurePercent}% (tensione Hask; mitiga con pH Neutro)", BotanicalHudTooltipPalette.TipRed));
                sub = true;
            }
            if (snap.GlasscapActiveMutationBonusSum > 0.0001f && !BotanicalPlantCodes.IsGlasscap(state.PlantCode))
            {
                lines.Add(new BotanicalHudTooltipLine("    • IM: bonus globale da Glasscap attivo altrove", BotanicalHudTooltipPalette.TipMuted));
                sub = true;
            }
            if (!sub)
                lines.Add(new BotanicalHudTooltipLine("    • —", BotanicalHudTooltipPalette.TipMuted));
        }
    }

    public static class BotanicalHudTooltipPalette
    {
        public static readonly UnityEngine.Color TipGreen = new Color(0.498f, 1f, 0.478f, 1f);
        /// <summary>Ciano tooltip TopBar ph-drift / DomeStatusHUD.</summary>
        public static readonly UnityEngine.Color TipPhCyan = new Color(80f / 255f, 200f / 255f, 220f / 255f, 1f);
        public static readonly UnityEngine.Color TipMuted = new Color(0.62f, 0.66f, 0.68f, 1f);
        public static readonly UnityEngine.Color TipYellow = new Color(0.902f, 0.788f, 0.435f, 1f);
        public static readonly UnityEngine.Color TipRed = new Color(0.827f, 0.373f, 0.373f, 1f);
    }

    public readonly struct BotanicalHudTooltipLine
    {
        public readonly string Text;
        public readonly UnityEngine.Color Color;
        public readonly bool Bold;

        public BotanicalHudTooltipLine(string text, UnityEngine.Color color, bool bold = false)
        {
            Text = text;
            Color = color;
            Bold = bold;
        }
    }
}
