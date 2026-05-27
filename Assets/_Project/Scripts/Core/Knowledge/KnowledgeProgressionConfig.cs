using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Sporae.Core.Knowledge
{
    [Serializable]
    public sealed class KnowledgeTierDefinition
    {
        public int MinScore;
        public string LabelKey = "knowledge.tier.neofita";
        public int ProjectBudgetBase = 8;
    }

    [Serializable]
    public sealed class WikiResearchNodeEntry
    {
        public string NodeId = "wiki.eod.Historical";
        [Tooltip("Se valorizzato, match anche UnlockCategory(branch) con questo id (es. Historical).")]
        public string CategoryBranch;
        public int KnowledgePoints = 3;
    }

    [CreateAssetMenu(fileName = "KnowledgeProgressionConfig", menuName = "Spore/Knowledge/Progression Config")]
    public sealed class KnowledgeProgressionConfig : ScriptableObject
    {
        public const string DefaultResourcePath = "Configs/KnowledgeProgressionConfig";

        public List<KnowledgeTierDefinition> Tiers = new();
        public List<WikiResearchNodeEntry> WikiNodes = new();

        [Header("Rewards")]
        public int LabProjectCompletePoints = 6;
        public int PotMilestonePoints = 1;

        [Header("Penalties")]
        public int LabProjectAbandonPenalty = 4;
        public int LabUnstableSeedPenalty = 3;

        public static KnowledgeProgressionConfig CreateRuntimeDefaults()
        {
            var cfg = CreateInstance<KnowledgeProgressionConfig>();
            cfg.Tiers = new List<KnowledgeTierDefinition>
            {
                new() { MinScore = 0, LabelKey = "knowledge.tier.neofita", ProjectBudgetBase = 8 },
                new() { MinScore = 8, LabelKey = "knowledge.tier.praticante", ProjectBudgetBase = 12 },
                new() { MinScore = 18, LabelKey = "knowledge.tier.ricercatore", ProjectBudgetBase = 16 },
                new() { MinScore = 32, LabelKey = "knowledge.tier.botanico", ProjectBudgetBase = 20 },
                new() { MinScore = 50, LabelKey = "knowledge.tier.senior", ProjectBudgetBase = 24 },
                new() { MinScore = 72, LabelKey = "knowledge.tier.maestro", ProjectBudgetBase = 28 }
            };
            cfg.WikiNodes = new List<WikiResearchNodeEntry>
            {
                new() { NodeId = "wiki.eod.Historical", CategoryBranch = "Historical", KnowledgePoints = 3 },
                new() { NodeId = "wiki.eod.Botanical", CategoryBranch = "Botanical", KnowledgePoints = 3 },
                new() { NodeId = "wiki.eod.Vault", CategoryBranch = "Vault", KnowledgePoints = 3 }
            };
            return cfg;
        }
    }
}
