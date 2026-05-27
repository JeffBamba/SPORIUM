using System;

namespace _Project.Sporae.Core.LabBlueprint
{
    [Serializable]
    public sealed class LabBlueprintAllocation
    {
        public LabBlueprintField field;
        public string targetId;
        public string targetLabel;
        public int points;
        public int maxTicks = 5;

        public LabBlueprintAllocation Clone()
        {
            return new LabBlueprintAllocation
            {
                field = field,
                targetId = targetId,
                targetLabel = targetLabel,
                points = points,
                maxTicks = maxTicks
            };
        }
    }
}
