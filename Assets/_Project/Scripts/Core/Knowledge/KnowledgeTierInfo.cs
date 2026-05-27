namespace _Project.Sporae.Core.Knowledge
{
    public readonly struct KnowledgeTierInfo
    {
        public int Rank { get; }
        public int MinScore { get; }
        public string LabelKey { get; }
        public int ProjectBudgetBase { get; }

        public KnowledgeTierInfo(int rank, int minScore, string labelKey, int projectBudgetBase)
        {
            Rank = rank;
            MinScore = minScore;
            LabelKey = labelKey ?? string.Empty;
            ProjectBudgetBase = projectBudgetBase;
        }

        public bool EqualsTier(KnowledgeTierInfo other) => Rank == other.Rank;
    }
}
