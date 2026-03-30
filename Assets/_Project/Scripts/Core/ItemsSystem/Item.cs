using System;

namespace _Project.Sporae.Core
{
    /// <summary>
    /// Genetic type for spores/fruits (GDD 42).
    /// </summary>
    public enum GeneticType { Fixed = 0, Stable = 1, Unstable = 2 }

    /// <summary>
    /// Stage for spores: Raw or Matured (GDD 42).
    /// </summary>
    public enum SporeStage { Raw = 0, Matured = 1 }

    [Serializable]
    public class Item
    {
        public ItemConfig ItemConfig { get; private set; }

        public Item(ItemConfig config, int itemId)
        {
            ItemConfig = config;
            ItemId = itemId;
            Quality = config.MaxQuality;
        }

        public string TypeId => ItemConfig.TypeId;
        public int ItemId { get; }

        public float Quality { set; get; }

        // GDD 42 / Fase 0: optional metadata for Fruits and SporeGeneric (harvest/lab)
        public GeneticType? GeneticTypeValue { get; set; }
        public SporeStage? SporeStageValue { get; set; }
        public string FamilyMetadata { get; set; }
        public string SourcePlantCodeMetadata { get; set; }
        public string SourcePlantDisplayName { get; set; }
        public int PlantLevelMetadata { get; set; }
        public string ActivePowerLabel { get; set; }
        public string PassivePowerLabel { get; set; }

        // Lab genetics pipeline metadata (Pre-Seed/Seed, Step 3-4)
        public string ParentFamilyA { get; set; }
        public string ParentFamilyB { get; set; }
        public string CandidateTraitsCsv { get; set; }
        public string SelectedTraitsCsv { get; set; }
        public int TraitPowerPercent { get; set; } = 100;
        public string ReagentUsedMetadata { get; set; }

        /// <summary>Profilo requisiti cure (Task 6): BLEND = specie seme; PARENT_A / PARENT_B = bande idratazione/LED/fertilizzante del genitore.</summary>
        public string LabCareProfileMetadata { get; set; }

        /// <summary>Nome scelto dal giocatore per il seme (Incubatore con Reagente X). Se valorizzato, display "Seme di Pianta {CustomPlantName}".</summary>
        public string CustomPlantName { get; set; }

        /// <summary>
        /// Specie runtime canonica del seed (PlantCode risolto in Incubatore/Lab).
        /// Serve per evitare dipendenze gameplay da TypeId legacy (es. seed-001/002/003).
        /// </summary>
        public string ResolvedPlantCodeMetadata { get; set; }
    }
}