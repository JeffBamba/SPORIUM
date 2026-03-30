using System.Linq;
using System.Text;
using UnityEngine;
using _Project.Sporae.Core;
using Sporae.Dome.PotSystem.Growth;

namespace Sporae.UI.UIToolkit.Lab
{
    /// <summary>Testi per tooltip Extractor: preview frutto in picker e risultato output. Solo dati reali; "-" dove mancano.</summary>
    public static class ExtractorTooltipTexts
    {
        /// <summary>Colore rich-text per i valori nei tooltip (diverso dalle etichette).</summary>
        public const string TooltipValueColorHex = "#aaddee";

        /// <summary>Avvolge il valore in tag colore per distinguerlo dall'etichetta nel tooltip.</summary>
        public static string WrapValue(string value)
        {
            if (string.IsNullOrEmpty(value)) value = "—";
            return $"<color={TooltipValueColorHex}>{value}</color>";
        }

        /// <summary>Nomenclatura tratti: Fissi (0% mutare), Stabili (25%), Instabili (50%).</summary>
        public static string GeneticTypeToTrattiLabel(GeneticType? genetic)
        {
            if (!genetic.HasValue) return "—";
            return genetic.Value switch
            {
                GeneticType.Fixed => "Fissi",
                GeneticType.Stable => "Stabili",
                GeneticType.Unstable => "Instabili",
                _ => genetic.Value.ToString()
            };
        }

        /// <summary>% di mutare: Fissi=0%, Stabili=25%, Instabili=50%.</summary>
        public static string GeneticTypeToPercentMutare(GeneticType? genetic)
        {
            if (!genetic.HasValue) return "—";
            return genetic.Value switch
            {
                GeneticType.Fixed => "0%",
                GeneticType.Stable => "25%",
                GeneticType.Unstable => "50%",
                _ => "—"
            };
        }

        /// <summary>Preview frutto identificato in inventario player: solo campi concordati.</summary>
        public static string BuildFruitPreviewTooltip(Item firstFruit)
        {
            if (firstFruit == null) return "—";
            var sb = new StringBuilder();
            string plantName = GetFruitDisplayName(firstFruit);
            sb.AppendLine($"Nome frutto: {WrapValue(plantName)}");
            string level = GetFruitLevel(firstFruit);
            sb.AppendLine($"Livello pianta madre: {WrapValue(level)}");
            string family = GetFruitFamilyLabel(firstFruit);
            sb.AppendLine($"Famiglia pianta madre: {WrapValue(family)}");
            string tratti = GeneticTypeToTrattiLabel(firstFruit.GeneticTypeValue);
            string percentMutare = GeneticTypeToPercentMutare(firstFruit.GeneticTypeValue);
            sb.AppendLine($"Tratti: {WrapValue(tratti)}");
            if (firstFruit.GeneticTypeValue.HasValue)
                sb.AppendLine($"% di mutare: {WrapValue(percentMutare)}");
            string origin = !string.IsNullOrWhiteSpace(firstFruit.SourcePlantCodeMetadata)
                ? firstFruit.SourcePlantCodeMetadata
                : "—";
            sb.AppendLine($"Origine: {WrapValue(origin)}");
            if (!string.IsNullOrWhiteSpace(firstFruit.ActivePowerLabel))
                sb.AppendLine($"Potere attivo: {WrapValue(firstFruit.ActivePowerLabel)}");
            if (!string.IsNullOrWhiteSpace(firstFruit.PassivePowerLabel))
                sb.AppendLine($"Potere passivo: {WrapValue(firstFruit.PassivePowerLabel)}");
            if (!string.IsNullOrWhiteSpace(firstFruit.SelectedTraitsCsv))
                sb.AppendLine($"Tratti conosciuti: {WrapValue(firstFruit.SelectedTraitsCsv)}");
            sb.AppendLine();
            sb.AppendLine($"{WrapValue("Estrazione")}: possibile 1 o 2 Spore RAW; con due output, la seconda ha genetica tra le due categorie diverse da quella della madre (bias Evil/Pure).");
            return sb.ToString();
        }

        /// <summary>Tooltip demo per "Frutto conosciuto" senza metadata: dati fissi Artic Hask, lvl 4, Tratti Stabili.</summary>
        public static string BuildFruitKnownDemoTooltip()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Nome frutto: {WrapValue("Arctic Hask")}");
            sb.AppendLine($"Livello pianta madre: {WrapValue("4")}");
            sb.AppendLine($"Famiglia pianta madre: {WrapValue("STANDARD")}");
            sb.AppendLine($"Tratti: {WrapValue("Stabili")}");
            sb.AppendLine($"% di mutare: {WrapValue("25%")}");
            sb.AppendLine($"Origine: {WrapValue("Arctic Hask Lvl 4")}");
            sb.AppendLine($"Potere attivo: {WrapValue("Arctic Purification: rigenera +5 global pH e cura muffe Dome ogni 2 giorni")}");
            sb.AppendLine($"Potere passivo: {WrapValue("Permafrost Core: sostiene il recupero del pH e la purezza ambientale.")}");
            sb.AppendLine($"Tratti conosciuti: {WrapValue("BalancedGrowth, NeutralYield, Resilience")}");
            return sb.ToString();
        }

        /// <summary>Preview frutto sconosciuto in inventario player: stessi campi con valori ignoti.</summary>
        public static string BuildFruitUnknownPreviewTooltip(Item firstFruit)
        {
            if (firstFruit == null) return "—";
            var sb = new StringBuilder();
            string specimenId = firstFruit.ItemId > 0 ? $"Specimen #{firstFruit.ItemId}" : "???";
            sb.AppendLine($"Nome frutto: {WrapValue(specimenId + " (sconosciuto)")}");
            sb.AppendLine($"Livello pianta madre: {WrapValue("—")}");
            sb.AppendLine($"Famiglia pianta madre: {WrapValue("???")}");
            sb.AppendLine($"Tratti: {WrapValue("???")}");
            sb.AppendLine($"% di mutare: {WrapValue("???")}");
            sb.AppendLine($"Origine: {WrapValue("—")}");
            sb.AppendLine($"Tratti conosciuti: {WrapValue("non conosciuti")}");
            return sb.ToString();
        }

        /// <summary>Tooltip output Spore Raw (frutto conosciuto): formato richiesto GDD.</summary>
        public static string BuildOutputKnownTooltip(ExtractionResultSnapshot snap)
        {
            if (snap == null) return "—";
            var sb = new StringBuilder();
            sb.AppendLine($"Output: {WrapValue(snap.OutputSporeId ?? "Spore RAW")}");
            sb.AppendLine("[Icona spora rossa]");
            sb.AppendLine();
            sb.AppendLine($"Tratti: {WrapValue(snap.Tipo ?? "—")}");
            sb.AppendLine($"Famiglia: {WrapValue(snap.Famiglia ?? "—")}");
            sb.AppendLine($"Origine: {WrapValue(snap.Origine ?? "—")}");
            if (!string.IsNullOrEmpty(snap.Bonus))
            {
                sb.AppendLine();
                sb.AppendLine($"Bonus: {WrapValue(snap.Bonus)}");
            }
            return sb.ToString();
        }

        /// <summary>Tooltip output Spore Raw (frutto sconosciuto): formato richiesto GDD.</summary>
        public static string BuildOutputUnknownTooltip(ExtractionResultSnapshot snap)
        {
            if (snap == null) return "—";
            var sb = new StringBuilder();
            sb.AppendLine($"Output: {WrapValue((snap.OutputSporeId ?? "SPO-???-???") + " [UNKNOWN]")}");
            sb.AppendLine("[Icona spora grigia con ?]");
            sb.AppendLine();
            sb.AppendLine($"Tratti: {WrapValue("Instabili")}");
            sb.AppendLine($"% di mutare: {WrapValue("50%")}");
            sb.AppendLine($"Famiglia: {WrapValue("???")}");
            sb.AppendLine($"Colore: {WrapValue(snap.Colore ?? "—")}");
            sb.AppendLine();
            sb.AppendLine("⚠️ Completare Step 4 per");
            sb.AppendLine("   identificazione completa");
            return sb.ToString();
        }

        /// <summary>Tooltip slot output Extractor: campi concordati Spora Raw (Tratti Fissi/Stabili/Instabili, % di mutare, Famiglia, Stato).</summary>
        public static string BuildOutputSporeRawTooltipAgreed(ExtractionResultSnapshot snap)
        {
            if (snap == null)
                snap = new ExtractionResultSnapshot { GeneticTypeValue = GeneticType.Stable, Famiglia = "—" };
            string tratti = GeneticTypeToTrattiLabel(snap.GeneticTypeValue);
            string percentMutare = GeneticTypeToPercentMutare(snap.GeneticTypeValue);
            string family = string.IsNullOrEmpty(snap.Famiglia) ? "—" : snap.Famiglia;
            string stato = "Raw (non combinabile)";
            var sb = new StringBuilder();
            sb.AppendLine($"Tratti: {WrapValue(tratti)}");
            if (snap.GeneticTypeValue.HasValue)
                sb.AppendLine($"% di mutare: {WrapValue(percentMutare)}");
            sb.AppendLine($"Famiglia: {WrapValue(family)}");
            sb.AppendLine($"Stato: {WrapValue(stato)}");
            if (snap.OutputSporeCount >= 2)
            {
                sb.AppendLine();
                sb.AppendLine($"{WrapValue("Quantità prevista")}: {snap.OutputSporeCount}× RAW — la seconda sarà variante genetica rispetto alla madre.");
            }
            return sb.ToString();
        }

        /// <summary>Tooltip unico per slot output: usa snapshot (known vs unknown). Se snap è null (es. estrazione da pianta/scarto), mostra formato known con "—".</summary>
        public static string BuildOutputTooltipFromSnapshot(ExtractionResultSnapshot snap)
        {
            if (snap == null)
                return BuildOutputSporeRawTooltipAgreed(new ExtractionResultSnapshot { GeneticTypeValue = GeneticType.Stable, Famiglia = "—" });
            return BuildOutputSporeRawTooltipAgreed(snap);
        }

        public static bool IsUnknownFruit(Item item)
        {
            if (item == null) return true;
            return string.IsNullOrEmpty(item.SourcePlantCodeMetadata) && string.IsNullOrEmpty(item.FamilyMetadata);
        }

        /// <summary>Nome pianta per il frutto (es. "Ferric Fern"). Per tooltip e sottotitolo riga inventario.</summary>
        public static string GetFruitDisplayName(Item fruit)
        {
            if (fruit == null) return "—";
            if (!string.IsNullOrEmpty(fruit.SourcePlantDisplayName))
                return fruit.SourcePlantDisplayName;
            if (!string.IsNullOrEmpty(fruit.SourcePlantCodeMetadata) && PlantDatabase.Instance != null)
            {
                var plantData = PlantDatabase.Instance.GetPlantDataByCode(fruit.SourcePlantCodeMetadata);
                if (plantData != null)
                    return plantData.name;
            }
            return !string.IsNullOrEmpty(fruit.SourcePlantCodeMetadata) ? fruit.SourcePlantCodeMetadata : "???";
        }

        private static string GetFruitLevel(Item fruit)
        {
            if (fruit == null) return "—";
            if (fruit.PlantLevelMetadata > 0)
                return fruit.PlantLevelMetadata.ToString();
            return "—";
        }

        private static string GetFruitFamilyLabel(Item fruit)
        {
            if (fruit == null) return "—";
            if (!string.IsNullOrEmpty(fruit.FamilyMetadata))
                return fruit.FamilyMetadata.ToUpperInvariant();
            if (PlantDatabase.Instance != null && !string.IsNullOrEmpty(fruit.SourcePlantCodeMetadata))
            {
                var plantData = PlantDatabase.Instance.GetPlantDataByCode(fruit.SourcePlantCodeMetadata);
                if (plantData != null)
                    return plantData.Family.ToString().ToUpperInvariant();
            }
            return "—";
        }

        public static string GetOriginTraceLabel(Item item)
        {
            if (item == null)
                return "—";

            string displayName = string.IsNullOrWhiteSpace(item.SourcePlantDisplayName)
                ? null
                : item.SourcePlantDisplayName.Trim();
            string sourceCode = string.IsNullOrWhiteSpace(item.SourcePlantCodeMetadata)
                ? null
                : item.SourcePlantCodeMetadata.Trim();

            sourceCode = NormalizeCombinedCodes(sourceCode);
            displayName = NormalizeCombinedDisplayName(displayName, sourceCode);

            if (!string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(sourceCode))
                return $"{displayName} [{sourceCode}]";
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;
            if (!string.IsNullOrWhiteSpace(sourceCode))
                return sourceCode;
            return "—";
        }

        private static string NormalizeCombinedDisplayName(string displayName, string sourceCode)
        {
            if (!string.IsNullOrWhiteSpace(displayName) && !string.Equals(displayName, sourceCode))
                return displayName;

            if (string.IsNullOrWhiteSpace(sourceCode))
                return displayName;

            var codes = sourceCode.Split('|');
            var uniqueNames = new System.Collections.Generic.List<string>();
            for (int i = 0; i < codes.Length; i++)
            {
                string code = string.IsNullOrWhiteSpace(codes[i]) ? null : codes[i].Trim();
                string resolvedName = ResolvePlantDisplayNameFromCode(code);
                if (string.IsNullOrWhiteSpace(resolvedName))
                    resolvedName = code;
                if (string.IsNullOrWhiteSpace(resolvedName))
                    continue;

                if (!uniqueNames.Contains(resolvedName))
                    uniqueNames.Add(resolvedName);
            }

            return uniqueNames.Count > 0 ? string.Join(" | ", uniqueNames) : displayName;
        }

        private static string NormalizeCombinedCodes(string sourceCode)
        {
            if (string.IsNullOrWhiteSpace(sourceCode))
                return sourceCode;

            var uniqueCodes = sourceCode
                .Split('|')
                .Select(code => string.IsNullOrWhiteSpace(code) ? null : code.Trim())
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct()
                .ToArray();

            return uniqueCodes.Length > 0 ? string.Join("|", uniqueCodes) : sourceCode;
        }

        private static string ResolvePlantDisplayNameFromCode(string plantCode)
        {
            if (string.IsNullOrWhiteSpace(plantCode))
                return null;

            var normalizedCode = plantCode.Trim().ToUpperInvariant();
            switch (normalizedCode)
            {
                case "PLT-STD-001":
                    return "Ferric Fern";
                case "PLT-PURE-001":
                    return "Arctic Hask";
                case "PLT-EVIL-001":
                    return "Glasscap Fungus";
            }

            var plantData = Resources.Load<PlantData>("Plants/" + normalizedCode);
            if (plantData == null)
                return null;

            if (!string.IsNullOrWhiteSpace(plantData.ResearchNotes))
            {
                var note = plantData.ResearchNotes.Trim();
                int separatorIndex = note.IndexOf(" - ", System.StringComparison.Ordinal);
                if (separatorIndex <= 0)
                    separatorIndex = note.IndexOf(" — ", System.StringComparison.Ordinal);
                if (separatorIndex > 0)
                    return note.Substring(0, separatorIndex).Trim();
            }

            return !string.IsNullOrWhiteSpace(plantData.Description)
                ? plantData.Description.Trim()
                : normalizedCode;
        }

    }
}
