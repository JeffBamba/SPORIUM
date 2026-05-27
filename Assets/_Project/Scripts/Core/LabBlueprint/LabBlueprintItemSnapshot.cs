using System;
using _Project.Sporae.Core;

namespace _Project.Sporae.Core.LabBlueprint
{
    [Serializable]
    public sealed class LabBlueprintItemSnapshot
    {
        public string typeId;
        public int itemId;
        public float quality;

        public bool hasGeneticType;
        public int geneticType;
        public bool hasSporeStage;
        public int sporeStage;

        public string familyMetadata;
        public string sourcePlantCodeMetadata;
        public string sourcePlantDisplayName;
        public int plantLevelMetadata;
        public string activePowerLabel;
        public string passivePowerLabel;
        public string parentFamilyA;
        public string parentFamilyB;
        public string candidateTraitsCsv;
        public string selectedTraitsCsv;
        public int traitPowerPercent;
        public string reagentUsedMetadata;
        public string labCareProfileMetadata;
        public string customPlantName;
        public string resolvedPlantCodeMetadata;

        public static LabBlueprintItemSnapshot FromItem(Item item)
        {
            if (item == null)
                return null;

            return new LabBlueprintItemSnapshot
            {
                typeId = item.TypeId,
                itemId = item.ItemId,
                quality = item.Quality,
                hasGeneticType = item.GeneticTypeValue.HasValue,
                geneticType = item.GeneticTypeValue.HasValue ? (int)item.GeneticTypeValue.Value : 0,
                hasSporeStage = item.SporeStageValue.HasValue,
                sporeStage = item.SporeStageValue.HasValue ? (int)item.SporeStageValue.Value : 0,
                familyMetadata = item.FamilyMetadata,
                sourcePlantCodeMetadata = item.SourcePlantCodeMetadata,
                sourcePlantDisplayName = item.SourcePlantDisplayName,
                plantLevelMetadata = item.PlantLevelMetadata,
                activePowerLabel = item.ActivePowerLabel,
                passivePowerLabel = item.PassivePowerLabel,
                parentFamilyA = item.ParentFamilyA,
                parentFamilyB = item.ParentFamilyB,
                candidateTraitsCsv = item.CandidateTraitsCsv,
                selectedTraitsCsv = item.SelectedTraitsCsv,
                traitPowerPercent = item.TraitPowerPercent,
                reagentUsedMetadata = item.ReagentUsedMetadata,
                labCareProfileMetadata = item.LabCareProfileMetadata,
                customPlantName = item.CustomPlantName,
                resolvedPlantCodeMetadata = item.ResolvedPlantCodeMetadata
            };
        }
    }
}
