using UnityEngine;
using _Project.Sporae.Core;
using Sporae.Dome;

namespace Sporae.UI.UIToolkit.NotificationsFoundation
{
    /// <summary>
    /// Toast Foundation quando l’IM attraversa le soglie intermedia/alta (allineate a TopBar / DomeMutationRuntimeService).
    /// Richiede runner Foundation attivo (stesso flag degli altri watcher).
    /// </summary>
    public sealed class FoundationMutationImWatcher : MonoBehaviour
    {
        private FoundationNotificationService _notifications;
        private DomeMutationRuntimeService _mutation;
        private int _lastBand = -1;
        private bool _primedFromService;

        private void Awake()
        {
            _notifications = ServiceContainer.Instance?.Get<FoundationNotificationService>(suppressWarning: true);
            _mutation = ServiceContainer.Instance?.Get<DomeMutationRuntimeService>(suppressWarning: true);
            if (_mutation != null)
                _mutation.OnDisplayMutationChanged += OnImChanged;
        }

        private void OnDestroy()
        {
            if (_mutation != null)
                _mutation.OnDisplayMutationChanged -= OnImChanged;
        }

        private void LateUpdate()
        {
            if (_mutation == null || _primedFromService) return;
            if (!_mutation.HasAuthoritativeSnapshot) return;
            _lastBand = DomeMutationRuntimeService.GetBandIndex(_mutation.DisplayNormalized);
            _primedFromService = true;
        }

        private void OnImChanged(float display01)
        {
            if (_notifications == null || !_notifications.Enabled) return;
            if (!_primedFromService)
                return;

            int band = DomeMutationRuntimeService.GetBandIndex(display01);
            if (band <= _lastBand)
            {
                _lastBand = band;
                return;
            }

            int pct = Mathf.RoundToInt(Mathf.Clamp01(display01) * 100f);
            var payload = new NotificationPayload().With("pct", pct.ToString());
            if (band >= 1 && _lastBand < 1)
                _notifications.PostToast("DOME-IM-MID", payload, dedupKey: "im:band:mid");
            if (band >= 2 && _lastBand < 2)
                _notifications.PostToast("DOME-IM-HIGH", payload, dedupKey: "im:band:high");
            _lastBand = band;
        }
    }
}
