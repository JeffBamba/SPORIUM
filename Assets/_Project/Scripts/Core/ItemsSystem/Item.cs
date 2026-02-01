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
    }
}