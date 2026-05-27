namespace _Project.Sporae.Core.LabBlueprint
{
    public enum LabBlueprintInputKind
    {
        None = 0,
        Fruit = 1,
        Spore = 2
    }

    public enum LabBlueprintStatus
    {
        Empty = 0,
        Draft = 1,
        Sealed = 2,
        InProgress = 3,
        OutcomePending = 4,
        Retired = 5,
        Abandoned = 6
    }

    public enum LabBlueprintStep
    {
        None = 0,
        Extractor = 1,
        Catalizzatore = 2,
        Fusion = 3,
        Incubator = 4,
        Completed = 5
    }

    public enum LabBlueprintOutcome
    {
        None = 0,
        Positive = 1,
        Negative = 2,
        Unstable = 3
    }

    public enum LabBlueprintField
    {
        Line = 0,
        Family = 1,
        GeneticMutation = 2,
        PhDrift = 3,
        ActivePower = 4,
        PassivePower = 5
    }
}
