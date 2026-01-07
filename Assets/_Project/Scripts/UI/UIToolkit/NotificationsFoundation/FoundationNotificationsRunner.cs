using UnityEngine;
using _Project.Sporae.Core;
using Sporae.DevTools;

namespace Sporae.UI.UIToolkit.NotificationsFoundation
{
    /// <summary>
    /// Runner MonoBehaviour che ticka il FoundationNotificationService.
    /// Verrà creato/attivato via feature flag durante la fase di coexistence.
    /// </summary>
    public sealed class FoundationNotificationsRunner : MonoBehaviour
    {
        [SerializeField] private bool _enableLogs = false;

        private FoundationNotificationService _service;

        private void Awake()
        {
            _service = ServiceContainer.Instance?.Get<FoundationNotificationService>(suppressWarning: true);
            if (_service == null)
            {
                // Se non registrato, lo crea e lo registra (ex novo, non rompe nulla finché non è usato).
                _service = new FoundationNotificationService();
                ServiceContainer.Instance?.Register(_service);
                if (_enableLogs)
                    SporiumLogger.LogInfo(LogCategory.UI, "FoundationNotificationService creato e registrato dal Runner");
            }
        }

        private void Update()
        {
            _service?.Tick(Time.realtimeSinceStartup);
        }
    }
}


