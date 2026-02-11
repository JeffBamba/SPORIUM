using System;
using Sporae.Dome.PotSystem.Growth;

namespace _Project.Sporae.Core
{
    /// <summary>Snapshot del risultato di un'estrazione (frutto consumato) per il tooltip output dell'Extractor.</summary>
    public class ExtractionResultSnapshot
    {
        public bool IsUnknown { get; set; }
        /// <summary>Tipo genetico della spora (per tooltip concordato).</summary>
        public GeneticType? GeneticTypeValue { get; set; }
        /// <summary>Es. "Stabili", "Instabili" (nomenclatura tooltip: Tratti Fissi/Stabili/Instabili).</summary>
        public string Tipo { get; set; }
        public string Famiglia { get; set; }
        /// <summary>Es. "Ferric Fern Lvl 3".</summary>
        public string Origine { get; set; }
        /// <summary>Es. "+1× CELL-002 (Fungina)".</summary>
        public string Bonus { get; set; }
        /// <summary>Per frutto sconosciuto: es. "Verdastro-scuro".</summary>
        public string Colore { get; set; }
        /// <summary>Per unknown: es. "SPO-???-247".</summary>
        public string OutputSporeId { get; set; }

        /// <summary>Crea snapshot da un frutto consumato. Se item è null o senza metadata utili, restituisce snapshot "unknown".</summary>
        public static ExtractionResultSnapshot FromFruit(Item fruit)
        {
            var snap = new ExtractionResultSnapshot();
            if (fruit == null)
            {
                snap.IsUnknown = true;
                snap.GeneticTypeValue = GeneticType.Unstable;
                snap.OutputSporeId = "SPO-???-???";
                snap.Tipo = "Instabili";
                snap.Famiglia = "???";
                snap.Colore = "—";
                return snap;
            }

            bool unknown = string.IsNullOrEmpty(fruit.SourcePlantCodeMetadata) && string.IsNullOrEmpty(fruit.FamilyMetadata);
            snap.IsUnknown = unknown;

            if (unknown)
            {
                snap.OutputSporeId = fruit.ItemId > 0 ? $"SPO-???-{fruit.ItemId}" : "SPO-???-???";
                snap.GeneticTypeValue = GeneticType.Unstable;
                snap.Tipo = "Instabili";
                snap.Famiglia = "???";
                snap.Colore = "—"; // TODO: da Item se aggiunto in futuro
                snap.Origine = null;
                snap.Bonus = null;
                return snap;
            }

            snap.OutputSporeId = "Spore RAW";
            snap.GeneticTypeValue = fruit.GeneticTypeValue;
            snap.Famiglia = !string.IsNullOrEmpty(fruit.FamilyMetadata) ? fruit.FamilyMetadata : "STANDARD";
            if (fruit.GeneticTypeValue.HasValue)
            {
                snap.Tipo = fruit.GeneticTypeValue.Value switch
                {
                    GeneticType.Fixed => "Fissi",
                    GeneticType.Stable => "Stabili",
                    GeneticType.Unstable => "Instabili",
                    _ => fruit.GeneticTypeValue.Value.ToString()
                };
            }
            else
            {
                snap.Tipo = "—";
            }

            string plantName = "—";
            string level = "";
            if (PlantDatabase.Instance != null && !string.IsNullOrEmpty(fruit.SourcePlantCodeMetadata))
            {
                var plantData = PlantDatabase.Instance.GetPlantDataByCode(fruit.SourcePlantCodeMetadata);
                if (plantData != null)
                {
                    plantName = plantData.name;
                    if (fruit.PlantLevelMetadata > 0)
                        level = $" Lvl {fruit.PlantLevelMetadata}";
                }
            }
            snap.Origine = plantName + level;
            snap.Bonus = null; // TODO: da LabUpgradesConfig / modulo Cellule quando disponibile (es. "+1× CELL-002 (Fungina)")
            snap.Colore = null;
            return snap;
        }
    }
}
