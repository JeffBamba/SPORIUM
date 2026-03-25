using _Project;
using Sporae.UI.UIToolkit.NotificationsFoundation;

namespace Sporae.Dome.PotSystem.Botanical
{
    /// <summary>
    /// Toast Foundation quando la tensione roster Arctic Hask (≥2 esemplari, pH fuori Neutro) si accende o si spegne.
    /// </summary>
    public static class BotanicalArcticTensionNotifier
    {
        private static int? _lastTensionOn; // null = non ancora valutato (nessun toast)

        /// <summary>Ripristina stato dopo load scena / nuova partita (prima valutazione senza toast).</summary>
        public static void ResetSessionState() => _lastTensionOn = null;

        public static void EvaluateAndNotify(PhSystem phSystem)
        {
            var snap = BotanicalRosterSnapshot.FromServices(phSystem);
            bool on = snap.TotalArcticHaskCount >= 2 && !snap.ArcticTensionMitigatedByPh;
            int code = on ? 1 : 0;

            if (_lastTensionOn == null)
            {
                _lastTensionOn = code;
                return;
            }

            if (_lastTensionOn.Value == code)
                return;

            _lastTensionOn = code;

            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation == null || !foundation.Enabled)
                return;

            if (on)
            {
                foundation.PostToast("PLT-ARCTIC-TENSION-ON",
                    new NotificationPayload()
                        .With("hask", snap.TotalArcticHaskCount.ToString())
                        .With("pressure", snap.SterilityPressurePercent.ToString()));
            }
            else
            {
                foundation.PostToast("PLT-ARCTIC-TENSION-OFF", new NotificationPayload());
            }
        }
    }
}
