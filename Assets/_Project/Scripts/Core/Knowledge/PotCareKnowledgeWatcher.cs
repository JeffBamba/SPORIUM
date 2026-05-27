using _Project.Sporae.Core;

namespace _Project.Sporae.Core.Knowledge
{
    /// <summary>
    /// Milestone Conoscenza su giorni consecutivi ottimali per vaso (10/20/30/40/50).
    /// </summary>
    public static class PotCareKnowledgeWatcher
    {
        private static readonly int[] Thresholds = { 10, 20, 30, 40, 50 };

        public static void CheckMilestonesForPot(PotStateModel pot)
        {
            if (pot == null || string.IsNullOrWhiteSpace(pot.PotId))
                return;

            var knowledge = ServiceContainer.Instance?.Get<KnowledgeProgressionService>(suppressWarning: true);
            if (knowledge == null)
                return;

            int days = pot.DaysConsecutiveOptimal;

            foreach (int threshold in Thresholds)
            {
                if (days < threshold)
                    continue;

                string key = $"pot:{pot.PotId}:optimal:{threshold}";
                knowledge.TryGrantOnce(key, knowledge.PotMilestonePoints, KnowledgeDeltaReason.PotCareMilestone);
            }
        }
    }
}
