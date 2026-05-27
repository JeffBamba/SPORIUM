using System.Collections;
using _Project.Sporae.Core;
using Sporae.Core;
using Sporae.Core.Localization;
using Sporae.UI.UIToolkit.NotificationsFoundation;
using UnityEngine;

namespace _Project.Sporae.Core.Knowledge
{
    /// <summary>
    /// Toast Foundation (o legacy) per delta Conoscenza e cambio tier.
    /// </summary>
    public sealed class KnowledgeToastBridge : MonoBehaviour
    {
        [SerializeField] private float _startupSuppressSeconds = 2f;
        [SerializeField] private float _subscribeDelaySeconds = 0.6f;

        private KnowledgeProgressionService _knowledge;
        private float _toastReadyRealtime;
        private bool _subscribed;

        private void Start()
        {
            _toastReadyRealtime = Time.realtimeSinceStartup + _startupSuppressSeconds;
            StartCoroutine(SubscribeWhenReady());
        }

        private IEnumerator SubscribeWhenReady()
        {
            if (_subscribeDelaySeconds > 0f)
                yield return new WaitForSecondsRealtime(_subscribeDelaySeconds);

            _knowledge = ServiceContainer.Instance?.Get<KnowledgeProgressionService>(suppressWarning: true);
            if (_knowledge == null)
                yield break;

            _knowledge.OnKnowledgeChanged += OnKnowledgeChanged;
            _subscribed = true;
        }

        private void OnDestroy()
        {
            if (_knowledge != null && _subscribed)
                _knowledge.OnKnowledgeChanged -= OnKnowledgeChanged;
        }

        private bool CanEmit => _subscribed && Time.realtimeSinceStartup >= _toastReadyRealtime;

        private void OnKnowledgeChanged(KnowledgeChangeEventArgs e)
        {
            if (!CanEmit || e == null)
                return;

            if (e.Delta != 0)
                PostScoreDeltaToast(e);

            if (e.TierChanged)
                PostTierChangeToast(e);
        }

        private static void PostScoreDeltaToast(KnowledgeChangeEventArgs e)
        {
            string reasonShort = ReasonLabel(e.Reason);
            var payload = new NotificationPayload()
                .With("delta", FormatSigned(e.Delta))
                .With("reason", reasonShort);

            string code = e.Delta > 0 ? "KNW-GAIN" : "KNW-LOSS";
            string fallbackIt = e.Delta > 0
                ? $"Conoscenza +{e.Delta} ({reasonShort})"
                : $"Conoscenza {e.Delta} ({reasonShort})";
            string fallbackEn = e.Delta > 0
                ? $"Knowledge +{e.Delta} ({reasonShort})"
                : $"Knowledge {e.Delta} ({reasonShort})";

            PostFoundationOrFallback(code, payload, PickLocalized(fallbackIt, fallbackEn),
                e.Delta > 0 ? NotificationSeverity.Success : NotificationSeverity.Warning);
        }

        private static void PostTierChangeToast(KnowledgeChangeEventArgs e)
        {
            var knowledge = ServiceContainer.Instance?.Get<KnowledgeProgressionService>(suppressWarning: true);
            string oldLabel = knowledge != null
                ? knowledge.GetTierLabelLocalized(e.OldTier)
                : e.OldTier.LabelKey;
            string newLabel = knowledge != null
                ? knowledge.GetTierLabelLocalized(e.NewTier)
                : e.NewTier.LabelKey;

            var payload = new NotificationPayload()
                .With("old_label", oldLabel)
                .With("new_label", newLabel)
                .With("old_budget", e.OldTier.ProjectBudgetBase.ToString())
                .With("new_budget", e.NewTier.ProjectBudgetBase.ToString());

            bool tierUp = e.NewTier.Rank > e.OldTier.Rank;
            string code = tierUp ? "KNW-TIER-UP" : "KNW-TIER-DOWN";
            string fallbackIt = tierUp
                ? $"Livello Conoscenza: {oldLabel} → {newLabel} (budget progetto {e.OldTier.ProjectBudgetBase} → {e.NewTier.ProjectBudgetBase})"
                : $"Livello Conoscenza: {oldLabel} → {newLabel} (budget progetto {e.OldTier.ProjectBudgetBase} → {e.NewTier.ProjectBudgetBase})";
            string fallbackEn = tierUp
                ? $"Knowledge level: {oldLabel} → {newLabel} (project budget {e.OldTier.ProjectBudgetBase} → {e.NewTier.ProjectBudgetBase})"
                : $"Knowledge level: {oldLabel} → {newLabel} (project budget {e.OldTier.ProjectBudgetBase} → {e.NewTier.ProjectBudgetBase})";

            PostFoundationOrFallback(code, payload, PickLocalized(fallbackIt, fallbackEn),
                tierUp ? NotificationSeverity.Success : NotificationSeverity.Warning);
        }

        private static string ReasonLabel(KnowledgeDeltaReason reason)
        {
            return reason switch
            {
                KnowledgeDeltaReason.WikiResearch => PickLocalized("ricerca", "research"),
                KnowledgeDeltaReason.LabProjectComplete => PickLocalized("lab", "lab"),
                KnowledgeDeltaReason.LabProjectAbandon => PickLocalized("abbandono", "abandon"),
                KnowledgeDeltaReason.LabUnstableSeed => PickLocalized("seme instabile", "unstable seed"),
                KnowledgeDeltaReason.PotCareMilestone => PickLocalized("cura vaso", "pot care"),
                _ => PickLocalized("—", "—")
            };
        }

        private static string FormatSigned(int delta) =>
            delta > 0 ? $"+{delta}" : delta.ToString();

        private static string PickLocalized(string it, string en) =>
            GameLanguageSettings.GetEffectiveLanguage() == GameLanguage.Italian ? it : en;

        private static void PostFoundationOrFallback(
            string code,
            NotificationPayload payload,
            string fallbackMessage,
            NotificationSeverity severity)
        {
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
            {
                foundation.PostToast(code, payload, severity);
                return;
            }

            // Nessun fallback legacy: abilita Foundation notifications in scena per i toast KNW-*.
        }
    }
}
