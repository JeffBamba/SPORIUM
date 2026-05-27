using _Project.Sporae.Core;

namespace _Project.Sporae.Core.Knowledge
{
    /// <summary>
    /// Hook lifecycle progetto seme Lab (legacy + futuro LAB 4.0).
    /// </summary>
    public static class SeedProjectKnowledgeHooks
    {
        public static void NotifyProjectAbandoned(string projectKey)
        {
            if (string.IsNullOrWhiteSpace(projectKey))
                return;

            var knowledge = ServiceContainer.Instance?.Get<KnowledgeProgressionService>(suppressWarning: true);
            if (knowledge == null)
                return;

            string key = "lab:abandon:" + projectKey.Trim();
            knowledge.TryApplyPenaltyOnce(key, knowledge.LabProjectAbandonPenalty, KnowledgeDeltaReason.LabProjectAbandon);
        }

        public static void NotifyProjectCompleted(string projectKey, GeneticType? seedGeneticType)
        {
            if (string.IsNullOrWhiteSpace(projectKey))
                return;

            var knowledge = ServiceContainer.Instance?.Get<KnowledgeProgressionService>(suppressWarning: true);
            if (knowledge == null)
                return;

            string completeKey = "lab:complete:" + projectKey.Trim();
            knowledge.TryGrantOnce(completeKey, knowledge.LabProjectCompletePoints, KnowledgeDeltaReason.LabProjectComplete);

            if (seedGeneticType == GeneticType.Unstable)
            {
                string unstableKey = "lab:unstable:" + projectKey.Trim();
                knowledge.TryApplyPenaltyOnce(unstableKey, knowledge.LabUnstableSeedPenalty, KnowledgeDeltaReason.LabUnstableSeed);
            }
        }
    }
}
