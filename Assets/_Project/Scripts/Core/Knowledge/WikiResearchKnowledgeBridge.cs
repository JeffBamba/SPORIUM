using _Project;
using _Project.Sporae.Core;

namespace _Project.Sporae.Core.Knowledge
{
    /// <summary>
    /// Collega sblocchi wiki/ricerca al motore Conoscenza (punti idempotenti per nodo).
    /// </summary>
    public static class WikiResearchKnowledgeBridge
    {
        public static void NotifyNodeUnlocked(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                return;

            var knowledge = ServiceContainer.Instance?.Get<KnowledgeProgressionService>(suppressWarning: true);
            if (knowledge == null)
                return;

            int points = knowledge.GetWikiPointsForNode(nodeId);
            if (points <= 0)
                return;

            knowledge.TryGrantOnce("wiki:" + nodeId.Trim(), points, KnowledgeDeltaReason.WikiResearch);
        }

        public static void NotifyCategoryUnlocked(string branch)
        {
            if (string.IsNullOrWhiteSpace(branch))
                return;

            var knowledge = ServiceContainer.Instance?.Get<KnowledgeProgressionService>(suppressWarning: true);
            if (knowledge == null)
                return;

            string nodeId = knowledge.GetWikiNodeIdForCategoryBranch(branch);
            if (!string.IsNullOrEmpty(nodeId))
            {
                NotifyNodeUnlocked(nodeId);
                return;
            }

            int points = knowledge.GetWikiPointsForCategoryBranch(branch);
            if (points > 0)
                knowledge.TryGrantOnce("wiki:cat:" + branch.Trim(), points, KnowledgeDeltaReason.WikiResearch);
        }

        public static void OnWikiUnlockServiceEntry(string id, bool isCategory)
        {
            if (isCategory)
            {
                string branch = id.StartsWith("cat:") ? id.Substring(4) : id;
                NotifyCategoryUnlocked(branch);
            }
            else
            {
                NotifyNodeUnlocked(id);
            }
        }
    }
}
