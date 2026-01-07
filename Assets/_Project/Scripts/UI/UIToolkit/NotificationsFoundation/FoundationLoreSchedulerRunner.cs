using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _Project.Sporae.Core;

namespace Sporae.UI.UIToolkit.NotificationsFoundation
{
    /// <summary>
    /// Scheduler lore/ambient (ibrido): emette notifiche LORE a bassa priorità.
    /// Deve essere preemptato dal gameplay (gestito dal service).
    /// </summary>
    public sealed class FoundationLoreSchedulerRunner : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _enabled = true;
        [SerializeField] private float _tickIntervalSeconds = 2f;

        private FoundationNotificationService _service;
        private List<NotificationTypeSpec> _loreSpecs;
        private float _nextTick;

        private void Awake()
        {
            _service = ServiceContainer.Instance?.Get<FoundationNotificationService>(suppressWarning: true);
            _loreSpecs = NotificationTypeSpecResolver
                .GetAll()
                .Where(s => s != null && s.Channel == NotificationChannel.Lore)
                .ToList();

            // Evita emissioni al frame 0: primo tick solo dopo l'intervallo.
            _nextTick = Time.realtimeSinceStartup + _tickIntervalSeconds;
        }

        private void Update()
        {
            if (!_enabled) return;
            if (_service == null || !_service.Enabled) return;
            if (_loreSpecs == null || _loreSpecs.Count == 0) return;

            var now = Time.realtimeSinceStartup;
            if (now < _nextTick) return;
            _nextTick = now + _tickIntervalSeconds;

            if (!_service.CanEmitLore(now))
                return;

            // Scegli un topic random dal pool (il service gestisce cooldown/rate-limit).
            var idx = UnityEngine.Random.Range(0, _loreSpecs.Count);
            var spec = _loreSpecs[idx];
            _service.PostToast(spec.Code);
        }
    }
}


