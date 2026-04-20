using System.Collections;
using _Project;
using _Project.Sporae.Core;
using Sporae.Core;
using Sporae.DevTools;
using UnityEngine;

namespace Sporae.UI.UIToolkit.NotificationsFoundation
{
    /// <summary>
    /// Toast Foundation (fallback <see cref="ToastNotificationManager"/>) quando cambiano
    /// il cap giornaliero azioni o la fascia di penalità movimento per idratazione.
    /// </summary>
    public sealed class PlayerStatToastBridge : MonoBehaviour
    {
        [SerializeField] private float _startupSuppressSeconds = 2f;
        [SerializeField] private float _subscribeDelaySeconds = 0.6f;

        private ActionSystem _actionSystem;
        private PlayerHydrationSystem _hydration;
        private float _toastReadyRealtime;
        private int _lastWalkTier;
        private float _lastHydrationPercent;
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

            var gm = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            if (gm == null)
                yield break;

            _actionSystem = gm.ActionSystem;
            _hydration = gm.PlayerHydrationSystem;

            if (_hydration != null)
            {
                _lastWalkTier         = PlayerHydrationSystem.GetMovementSpeedTierIndex(_hydration.HydrationPercent);
                _lastHydrationPercent = _hydration.HydrationPercent;
            }

            if (_actionSystem != null)
                _actionSystem.OnDailyCapChanged += OnDailyCapChanged;

            if (_hydration != null)
                _hydration.OnHydrationChanged += OnHydrationChanged;

            _subscribed = true;
        }

        private void OnDestroy()
        {
            if (_actionSystem != null)
                _actionSystem.OnDailyCapChanged -= OnDailyCapChanged;
            if (_hydration != null)
                _hydration.OnHydrationChanged -= OnHydrationChanged;
        }

        private bool CanEmit => _subscribed && Time.realtimeSinceStartup >= _toastReadyRealtime;

        private void OnDailyCapChanged(int oldMax, int newMax)
        {
            if (!CanEmit || oldMax == newMax)
                return;

            var payload = new NotificationPayload()
                .With("prev", oldMax.ToString())
                .With("max", newMax.ToString());

            string fallback = $"Budget azioni giornaliere: {oldMax} → {newMax}.";
            PostFoundationOrFallback("PLY-ACT-CAP-CHG", payload, fallback, ToastNotificationType.Info);
        }

        private void OnHydrationChanged(float currentPercent, float _)
        {
            if (!CanEmit)
            {
                _lastHydrationPercent = currentPercent;
                return;
            }

            // Toast guadagno idratazione (bere/mangiare)
            float delta = currentPercent - _lastHydrationPercent;
            if (delta >= 0.5f)
            {
                int deltaInt   = Mathf.RoundToInt(delta);
                int currentInt = Mathf.RoundToInt(currentPercent);
                var gainPayload = new NotificationPayload()
                    .With("delta", deltaInt.ToString())
                    .With("h", currentInt.ToString());
                PostFoundationOrFallback(
                    "PLY-HYD-GAIN",
                    gainPayload,
                    $"H +{deltaInt}% (→ {currentInt}%)",
                    ToastNotificationType.ConditionImproved);
            }
            _lastHydrationPercent = currentPercent;

            // Toast cambio fascia velocità movimento
            int newTier = PlayerHydrationSystem.GetMovementSpeedTierIndex(currentPercent);
            if (newTier == _lastWalkTier)
                return;

            int prevTier = _lastWalkTier;
            _lastWalkTier = newTier;

            string hStr = Mathf.RoundToInt(currentPercent).ToString();
            var payload = new NotificationPayload().With("h", hStr);

            if (newTier > prevTier)
            {
                if (newTier >= 2)
                {
                    PostFoundationOrFallback(
                        "PLY-HYD-WALK-LOW",
                        payload,
                        $"Disidratazione: camminata molto rallentata (H {hStr}%).",
                        ToastNotificationType.Warning);
                }
                else
                {
                    PostFoundationOrFallback(
                        "PLY-HYD-WALK-MID",
                        payload,
                        $"Idratazione bassa: camminata più lenta (H {hStr}%).",
                        ToastNotificationType.Warning);
                }
            }
            else
            {
                if (newTier == 0)
                {
                    PostFoundationOrFallback(
                        "PLY-HYD-WALK-OK",
                        payload,
                        $"Velocità movimento normale (H {hStr}%).",
                        ToastNotificationType.ConditionImproved);
                }
                else if (newTier == 1 && prevTier >= 2)
                {
                    PostFoundationOrFallback(
                        "PLY-HYD-WALK-BETTER",
                        payload,
                        $"Penalità movimento ridotta (H {hStr}%).",
                        ToastNotificationType.ConditionImproved);
                }
            }
        }

        private static void PostFoundationOrFallback(
            string code,
            NotificationPayload payload,
            string fallbackMessage,
            ToastNotificationType legacyType)
        {
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
            {
                foundation.PostToast(code, payload);
                return;
            }

            var toast = ServiceContainer.Instance?.Get<ToastNotificationManager>(suppressWarning: true);
            if (toast != null)
                toast.ShowToast(legacyType, fallbackMessage, code);
        }
    }
}
