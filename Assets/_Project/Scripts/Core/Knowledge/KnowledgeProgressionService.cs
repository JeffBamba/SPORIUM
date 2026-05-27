using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Sporae.Core;
using Sporae.Core.Localization;

namespace _Project.Sporae.Core.Knowledge
{
    public sealed class KnowledgeProgressionService
    {
        public event Action<KnowledgeChangeEventArgs> OnKnowledgeChanged;

        private readonly KnowledgeProgressionConfig _config;
        private readonly HashSet<string> _grantedEventKeys = new(StringComparer.Ordinal);
        private readonly List<KnowledgeTierInfo> _sortedTiers = new();

        private int _totalScore;
        private bool _suppressChangeNotifications;

        public int TotalScore => _totalScore;
        public KnowledgeTierInfo CurrentTier => ResolveTier(_totalScore);
        public bool SuppressChangeNotifications => _suppressChangeNotifications;

        public KnowledgeProgressionService(KnowledgeProgressionConfig config = null)
        {
            _config = config ?? KnowledgeProgressionConfig.CreateRuntimeDefaults();
            RebuildTierCache();
        }

        public int LabProjectCompletePoints => _config.LabProjectCompletePoints;
        public int LabProjectAbandonPenalty => _config.LabProjectAbandonPenalty;
        public int LabUnstableSeedPenalty => _config.LabUnstableSeedPenalty;
        public int PotMilestonePoints => _config.PotMilestonePoints;

        public string GetTierLabelLocalized()
        {
            var tier = CurrentTier;
            return string.IsNullOrEmpty(tier.LabelKey)
                ? "—"
                : LocalizationManager.GetString(tier.LabelKey);
        }

        public string GetTierLabelLocalized(KnowledgeTierInfo tier)
        {
            return string.IsNullOrEmpty(tier.LabelKey)
                ? "—"
                : LocalizationManager.GetString(tier.LabelKey);
        }

        public int GetProjectBudgetBase() => CurrentTier.ProjectBudgetBase;

        public int GetReagentIncrementX(int? baseOverride = null)
        {
            int b = baseOverride ?? GetProjectBudgetBase();
            return EvenCeilPercent(b, 0.10f);
        }

        public int GetReagentIncrementY(int? baseOverride = null)
        {
            int b = baseOverride ?? GetProjectBudgetBase();
            return EvenCeilPercent(b, 0.25f);
        }

        public int ComputeProjectBudgetTotal(bool useReagentX, bool useReagentY)
        {
            int b = GetProjectBudgetBase();
            if (useReagentY) return b + GetReagentIncrementY(b);
            if (useReagentX) return b + GetReagentIncrementX(b);
            return b;
        }

        public bool TryGrantOnce(string eventKey, int points, KnowledgeDeltaReason reason)
        {
            if (string.IsNullOrWhiteSpace(eventKey) || points == 0)
                return false;
            if (_grantedEventKeys.Contains(eventKey))
                return false;

            _grantedEventKeys.Add(eventKey);
            ApplyDelta(points, reason, eventKey);
            return true;
        }

        public bool TryApplyPenaltyOnce(string eventKey, int penaltyPoints, KnowledgeDeltaReason reason)
        {
            if (string.IsNullOrWhiteSpace(eventKey) || penaltyPoints <= 0)
                return false;
            if (_grantedEventKeys.Contains(eventKey))
                return false;

            _grantedEventKeys.Add(eventKey);
            ApplyDelta(-penaltyPoints, reason, eventKey);
            return true;
        }

        public void ApplyDelta(int delta, KnowledgeDeltaReason reason, string contextId = null)
        {
            if (delta == 0)
                return;

            int oldScore = _totalScore;
            var oldTier = ResolveTier(oldScore);

            _totalScore = Math.Max(0, _totalScore + delta);
            var newTier = ResolveTier(_totalScore);

            if (!_suppressChangeNotifications)
            {
                OnKnowledgeChanged?.Invoke(new KnowledgeChangeEventArgs(
                    oldScore, _totalScore, delta, reason, contextId, oldTier, newTier));
            }
        }

        public void LoadFromSave(int totalScore, IEnumerable<string> grantedKeys, bool suppressNotifications = true)
        {
            _suppressChangeNotifications = suppressNotifications;
            _totalScore = Math.Max(0, totalScore);
            _grantedEventKeys.Clear();
            if (grantedKeys != null)
            {
                foreach (var key in grantedKeys)
                {
                    if (!string.IsNullOrWhiteSpace(key))
                        _grantedEventKeys.Add(key.Trim());
                }
            }

            _suppressChangeNotifications = false;
        }

        public KnowledgeSaveSnapshot ExportSaveSnapshot()
        {
            return new KnowledgeSaveSnapshot
            {
                TotalScore = _totalScore,
                GrantedEventKeys = _grantedEventKeys.OrderBy(k => k).ToList()
            };
        }

        public int GetWikiPointsForNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || _config.WikiNodes == null)
                return 0;
            var entry = _config.WikiNodes.FirstOrDefault(n =>
                string.Equals(n.NodeId, nodeId.Trim(), StringComparison.OrdinalIgnoreCase));
            return entry?.KnowledgePoints ?? 0;
        }

        public int GetWikiPointsForCategoryBranch(string branch)
        {
            if (string.IsNullOrWhiteSpace(branch) || _config.WikiNodes == null)
                return 0;
            var entry = _config.WikiNodes.FirstOrDefault(n =>
                string.Equals(n.CategoryBranch, branch.Trim(), StringComparison.OrdinalIgnoreCase));
            return entry?.KnowledgePoints ?? 0;
        }

        public string GetWikiNodeIdForCategoryBranch(string branch)
        {
            if (string.IsNullOrWhiteSpace(branch) || _config.WikiNodes == null)
                return null;
            return _config.WikiNodes
                .FirstOrDefault(n => string.Equals(n.CategoryBranch, branch.Trim(), StringComparison.OrdinalIgnoreCase))
                ?.NodeId;
        }

        private void RebuildTierCache()
        {
            _sortedTiers.Clear();
            if (_config.Tiers == null || _config.Tiers.Count == 0)
            {
                var defaults = KnowledgeProgressionConfig.CreateRuntimeDefaults();
                foreach (var t in defaults.Tiers)
                    _sortedTiers.Add(ToInfo(t, _sortedTiers.Count));
                return;
            }

            var ordered = _config.Tiers.OrderBy(t => t.MinScore).ToList();
            for (int i = 0; i < ordered.Count; i++)
                _sortedTiers.Add(ToInfo(ordered[i], i));
        }

        private static KnowledgeTierInfo ToInfo(KnowledgeTierDefinition def, int rank)
        {
            return new KnowledgeTierInfo(rank, def.MinScore, def.LabelKey, def.ProjectBudgetBase);
        }

        private KnowledgeTierInfo ResolveTier(int score)
        {
            if (_sortedTiers.Count == 0)
                return new KnowledgeTierInfo(0, 0, "knowledge.tier.neofita", 8);

            KnowledgeTierInfo best = _sortedTiers[0];
            for (int i = 0; i < _sortedTiers.Count; i++)
            {
                if (score >= _sortedTiers[i].MinScore)
                    best = _sortedTiers[i];
            }

            return best;
        }

        private static int EvenCeilPercent(int baseValue, float fraction)
        {
            if (baseValue <= 0)
                return 2;
            double raw = baseValue * fraction;
            int ceil = (int)Math.Ceiling(raw);
            if (ceil % 2 != 0)
                ceil++;
            return Math.Max(2, ceil);
        }
    }

    public sealed class KnowledgeChangeEventArgs
    {
        public int OldScore { get; }
        public int NewScore { get; }
        public int Delta { get; }
        public KnowledgeDeltaReason Reason { get; }
        public string ContextId { get; }
        public KnowledgeTierInfo OldTier { get; }
        public KnowledgeTierInfo NewTier { get; }
        public bool TierChanged => !OldTier.EqualsTier(NewTier);

        public KnowledgeChangeEventArgs(
            int oldScore,
            int newScore,
            int delta,
            KnowledgeDeltaReason reason,
            string contextId,
            KnowledgeTierInfo oldTier,
            KnowledgeTierInfo newTier)
        {
            OldScore = oldScore;
            NewScore = newScore;
            Delta = delta;
            Reason = reason;
            ContextId = contextId;
            OldTier = oldTier;
            NewTier = newTier;
        }
    }

    public sealed class KnowledgeSaveSnapshot
    {
        public int TotalScore;
        public List<string> GrantedEventKeys = new();
    }
}
