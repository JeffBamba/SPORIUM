using System;
using System.Collections.Generic;

namespace _Project.Sporae.Core.LabBlueprint
{
    [Serializable]
    public sealed class LabBlueprintState
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public string projectId;
        public LabBlueprintStatus status = LabBlueprintStatus.Empty;
        public LabBlueprintInputKind inputKind = LabBlueprintInputKind.None;
        public LabBlueprintItemSnapshot inputItem;

        public string knowledgeTierLabel;
        public int knowledgeTierRank;
        public int baseBudget;
        public string reagentTypeId;
        public int reagentIncrement;
        public int totalBudget;
        public int allocatedPoints;
        public int freePoints;

        public List<LabBlueprintAllocation> allocations = new List<LabBlueprintAllocation>();

        public int draftCreatedDay;
        public int sealedDay;
        public LabBlueprintStep currentStep = LabBlueprintStep.None;
        public LabBlueprintOutcome outcome = LabBlueprintOutcome.None;
        public bool extractorStepSkipped;

        public bool HasDraftOrActiveProject =>
            status == LabBlueprintStatus.Draft ||
            status == LabBlueprintStatus.Sealed ||
            status == LabBlueprintStatus.InProgress ||
            status == LabBlueprintStatus.OutcomePending;

        public static LabBlueprintState Empty()
        {
            return new LabBlueprintState();
        }

        public LabBlueprintState Clone()
        {
            var clone = (LabBlueprintState)MemberwiseClone();
            clone.allocations = new List<LabBlueprintAllocation>();
            if (allocations != null)
            {
                foreach (var allocation in allocations)
                {
                    if (allocation != null)
                        clone.allocations.Add(allocation.Clone());
                }
            }

            return clone;
        }

        public void RecalculateBudget()
        {
            if (allocations == null)
                allocations = new List<LabBlueprintAllocation>();

            allocatedPoints = 0;
            foreach (var allocation in allocations)
            {
                if (allocation != null && allocation.points > 0)
                    allocatedPoints += allocation.points;
            }

            totalBudget = baseBudget + reagentIncrement;
            freePoints = Math.Max(0, totalBudget - allocatedPoints);
        }
    }
}
