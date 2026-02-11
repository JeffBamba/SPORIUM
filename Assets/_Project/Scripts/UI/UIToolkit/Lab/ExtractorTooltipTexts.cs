using System.Text;
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
            if (!string.IsNullOrWhiteSpace(firstFruit.SelectedTraitsCsv))
                sb.AppendLine($"Tratti conosciuti: {WrapValue(firstFruit.SelectedTraitsCsv)}");
            return sb.ToString();
        }

        /// <summary>Tooltip demo per "Frutto conosciuto" senza metadata: dati fissi Artic Hask, lvl 4, Tratti Stabili.</summary>
        public static string BuildFruitKnownDemoTooltip()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Nome frutto: {WrapValue("Artic Hask")}");
            sb.AppendLine($"Livello pianta madre: {WrapValue("4")}");
            sb.AppendLine($"Famiglia pianta madre: {WrapValue("STANDARD")}");
            sb.AppendLine($"Tratti: {WrapValue("Stabili")}");
            sb.AppendLine($"% di mutare: {WrapValue("25%")}");
            sb.AppendLine($"Origine: {WrapValue("Artic Hask Lvl 4")}");
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

    }
}
