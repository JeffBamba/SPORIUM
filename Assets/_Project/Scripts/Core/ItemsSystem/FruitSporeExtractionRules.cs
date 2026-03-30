using Sporae.Dome.PotSystem.Growth;
using UnityEngine;

namespace _Project.Sporae.Core
{
    /// <summary>
    /// Regole Task 7: quantità spore RAW da frutto (1 o 2) e genetica della seconda spora
    /// (uniforme tra le due categorie non-madre, con pesi per famiglia).
    /// </summary>
    public static class FruitSporeExtractionRules
    {
        /// <summary>Probabilità doppia estrazione per famiglia madre.</summary>
        public const float DoubleSporeChanceStandard = 0.28f;
        public const float DoubleSporeChancePure = 0.22f;
        public const float DoubleSporeChanceEvil = 0.42f;

        public static PlantFamily ResolvePlantFamily(Item fruit)
        {
            if (fruit == null)
                return PlantFamily.Standard;
            if (!string.IsNullOrWhiteSpace(fruit.FamilyMetadata))
            {
                string s = fruit.FamilyMetadata.Trim().ToUpperInvariant();
                if (s.Contains("PURE"))
                    return PlantFamily.Pure;
                if (s.Contains("EVIL"))
                    return PlantFamily.Evil;
                return PlantFamily.Standard;
            }

            if (PlantDatabase.Instance != null && !string.IsNullOrWhiteSpace(fruit.SourcePlantCodeMetadata))
            {
                var pd = PlantDatabase.Instance.GetPlantDataByCode(fruit.SourcePlantCodeMetadata);
                if (pd != null)
                    return pd.Family;
            }

            return PlantFamily.Standard;
        }

        /// <summary>1 oppure 2 spore RAW per run di estrazione.</summary>
        public static int RollSporeRawCount(Item fruit)
        {
            PlantFamily fam = ResolvePlantFamily(fruit);
            float p = fam switch
            {
                PlantFamily.Evil => DoubleSporeChanceEvil,
                PlantFamily.Pure => DoubleSporeChancePure,
                _ => DoubleSporeChanceStandard,
            };
            return Random.value < p ? 2 : 1;
        }

        /// <summary>
        /// Genetica per la seconda spora: una delle due categorie diverse dalla madre.
        /// Evil tende a Instabile quando è tra le opzioni; Pure tende a Fissi.
        /// </summary>
        public static GeneticType PickAlternateGeneticType(GeneticType mother, PlantFamily family)
        {
            float r = Random.value;
            switch (mother)
            {
                case GeneticType.Fixed:
                    if (family == PlantFamily.Evil)
                        return r < 0.65f ? GeneticType.Unstable : GeneticType.Stable;
                    if (family == PlantFamily.Pure)
                        return r < 0.55f ? GeneticType.Stable : GeneticType.Unstable;
                    return r < 0.5f ? GeneticType.Stable : GeneticType.Unstable;

                case GeneticType.Stable:
                    if (family == PlantFamily.Evil)
                        return r < 0.65f ? GeneticType.Unstable : GeneticType.Fixed;
                    if (family == PlantFamily.Pure)
                        return r < 0.55f ? GeneticType.Fixed : GeneticType.Unstable;
                    return r < 0.5f ? GeneticType.Fixed : GeneticType.Unstable;

                default: // Unstable
                    if (family == PlantFamily.Evil)
                        return r < 0.55f ? GeneticType.Fixed : GeneticType.Stable;
                    if (family == PlantFamily.Pure)
                        return r < 0.55f ? GeneticType.Fixed : GeneticType.Stable;
                    return r < 0.5f ? GeneticType.Fixed : GeneticType.Stable;
            }
        }
    }
}
