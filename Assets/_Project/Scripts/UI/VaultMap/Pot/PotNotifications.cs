using UnityEngine;
using Sporae.Dome.PotSystem.Growth;
using _Project.Sporae.Core;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.NotificationsFoundation;

namespace _Project.Pot
{
    public class PotNotifications
    {
        private readonly UINotification _notification;
        private ToastNotificationManager _toastManager;
        
        public PotNotifications()
        {
            _notification = Object.FindObjectOfType<UINotification>();
            
            // Prova a ottenere ToastNotificationManager (può essere null se non ancora registrato)
            if (ServiceContainer.Instance != null)
            {
                _toastManager = ServiceContainer.Instance.Get<ToastNotificationManager>(suppressWarning: true);
            }

            PotEvents.OnPotAction += HandlePotAction;
            PotEvents.OnPotActionFailed += HandlePotFailed;
        }

        ~PotNotifications()
        {
            PotEvents.OnPotAction -= HandlePotAction;
            PotEvents.OnPotActionFailed -= HandlePotFailed;
        }

        private void HandlePotFailed(PotEvents.PotActionType type, PotSlot pot, string message)
        {
            string text;
            
            if (!string.IsNullOrEmpty(message))
            {
                text = message;
            }
            else
            {
                text = type switch
                {
                    PotEvents.PotActionType.Light => NotificationLocalization.Pick(
                        "Impossibile modificare il LED di crescita.",
                        "You cannot change the grow LED."),
                    PotEvents.PotActionType.Plant => NotificationLocalization.Pick(
                        "Impossibile completare la semina.",
                        "You cannot plant."),
                    PotEvents.PotActionType.Water => NotificationLocalization.Pick(
                        "Impossibile attivare o disattivare l’impianto a goccia.",
                        "You failed to toggle drip irrigation."),
                    PotEvents.PotActionType.Fertilize => NotificationLocalization.Pick(
                        "Impossibile fertilizzare la pianta.",
                        "You cannot fertilize the plant."),
                    PotEvents.PotActionType.Harvest => NotificationLocalization.Pick(
                        "Impossibile raccogliere.",
                        "You cannot harvest the plant."),
                    PotEvents.PotActionType.Spray => NotificationLocalization.Pick(
                        "Impossibile applicare lo spray.",
                        "You cannot spray the plant."),
                    PotEvents.PotActionType.Uproot => NotificationLocalization.Pick(
                        "Impossibile sradicare la pianta.",
                        "You cannot uproot the plant."),
                    _ => NotificationLocalization.Pick("Azione non riuscita.", "Action failed.")
                };
            }

            // Usa nuovo sistema toast se disponibile, altrimenti fallback a UINotification
            // DEBUG_SAFE_FIX: Riprova a ottenere ToastNotificationManager se null (potrebbe essere registrato dopo)
            if (_toastManager == null && ServiceContainer.Instance != null)
            {
                _toastManager = ServiceContainer.Instance.Get<ToastNotificationManager>(suppressWarning: true);
            }
            
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
            {
                // In Foundation, usiamo codici univoci per tipologia (in futuro li mapperemo in TypeSpec).
                string code = type switch
                {
                    PotEvents.PotActionType.Light => "POT-LIGHT-FAILED",
                    PotEvents.PotActionType.Plant => "POT-PLANT-FAILED",
                    PotEvents.PotActionType.Water => "POT-WATER-FAILED",
                    PotEvents.PotActionType.Fertilize => "POT-FERTILIZE-FAILED",
                    PotEvents.PotActionType.Harvest => "POT-HARVEST-FAILED",
                    PotEvents.PotActionType.Spray => "POT-SPRAY-FAILED",
                    PotEvents.PotActionType.Uproot => "POT-UPROOT-FAILED",
                    _ => "POT-ACTION-FAILED"
                };
                foundation.PostToast(code, new NotificationPayload().With("message", text), NotificationSeverity.Danger);
            }
            else if (_toastManager != null)
            {
                string code = type switch
                {
                    PotEvents.PotActionType.Light => "POT-LIGHT-FAILED",
                    PotEvents.PotActionType.Plant => "POT-PLANT-FAILED",
                    PotEvents.PotActionType.Water => "POT-WATER-FAILED",
                    PotEvents.PotActionType.Fertilize => "POT-FERTILIZE-FAILED",
                    PotEvents.PotActionType.Harvest => "POT-HARVEST-FAILED",
                    PotEvents.PotActionType.Spray => "POT-SPRAY-FAILED",
                    PotEvents.PotActionType.Uproot => "POT-UPROOT-FAILED",
                    _ => "POT-ACTION-FAILED"
                };
                _toastManager.ShowError(text, code);
            }
            else if (_notification != null)
            {
                _notification.ShowNotification(text, 2, Color.red);
            }
        }

        private void HandlePotAction(PotEvents.PotActionType type, PotSlot pot)
        {
            // BUG FIX: Se l'azione è Fertilize e la pianta è morta (HasPlant == false), 
            // non mostrare il Toast di successo (il Toast di morte verrà mostrato da OnPlantDied)
            if (type == PotEvents.PotActionType.Fertilize && pot != null && pot.PotActions != null)
            {
                var potState = pot.PotActions.GetCurrentState();
                if (potState != null && !potState.HasPlant)
                {
                    // Pianta morta: non mostrare Toast di successo (il Toast di morte verrà mostrato da OnPlantDied)
                    return;
                }
            }
            
            string text;
            
            if (type == PotEvents.PotActionType.Water)
            {
                if (pot != null && pot.PotActions != null && pot.PotActions.IsWateringSystemOn())
                {
                    text = NotificationLocalization.Pick(
                        $"Impianto a goccia attivo ({pot.PotId}).",
                        $"Drip irrigation on ({pot.PotId}).");
                }
                else
                {
                    string pid = pot != null ? pot.PotId : "?";
                    text = NotificationLocalization.Pick(
                        $"Impianto a goccia disattivato ({pid}).",
                        $"Drip irrigation off ({pid}).");
                }
            }
            else if (type == PotEvents.PotActionType.Light)
            {
                if (pot != null && pot.PotActions != null)
                {
                    var ledState = pot.PotActions.GetLedSystemState();
                    switch (ledState)
                    {
                        case LedSystemState.Off:
                            text = NotificationLocalization.Pick(
                                $"LED di crescita spento ({pot.PotId}).",
                                $"Grow LED off ({pot.PotId}).");
                            break;
                        case LedSystemState.Blue:
                            text = NotificationLocalization.Pick(
                                $"LED di crescita blu acceso ({pot.PotId}).",
                                $"Blue grow LED on ({pot.PotId}).");
                            break;
                        case LedSystemState.Red:
                            text = NotificationLocalization.Pick(
                                $"LED di crescita rosso acceso ({pot.PotId}).",
                                $"Red grow LED on ({pot.PotId}).");
                            break;
                        default:
                            text = NotificationLocalization.Pick(
                                $"LED di crescita aggiornato ({pot.PotId}).",
                                $"Grow LED updated ({pot.PotId}).");
                            break;
                    }
                }
                else
                {
                    text = NotificationLocalization.Pick(
                        "LED di crescita aggiornato.",
                        "Grow LED updated successfully.");
                }
            }
            else
            {
                text = type switch
                {
                    PotEvents.PotActionType.Plant => NotificationLocalization.Pick(
                        "Semina completata con successo.",
                        "Planting completed successfully."),
                    PotEvents.PotActionType.Fertilize => NotificationLocalization.Pick(
                        "Fertilizzazione applicata correttamente.",
                        "Fertilizer applied successfully."),
                    PotEvents.PotActionType.Harvest => NotificationLocalization.Pick(
                        "Raccolto completato con successo.",
                        "Harvest completed successfully."),
                    PotEvents.PotActionType.Spray => NotificationLocalization.Pick(
                        "Spray applicato con successo.",
                        "Spray applied successfully."),
                    PotEvents.PotActionType.Uproot => NotificationLocalization.Pick(
                        "Pianta sradicata con successo.",
                        "Uproot completed successfully."),
                    _ => NotificationLocalization.Pick(
                        "Azione completata con successo.",
                        "Action completed successfully.")
                };
            }

            // Usa nuovo sistema toast se disponibile, altrimenti fallback a UINotification
            // DEBUG_SAFE_FIX: Riprova a ottenere ToastNotificationManager se null (potrebbe essere registrato dopo)
            if (_toastManager == null && ServiceContainer.Instance != null)
            {
                _toastManager = ServiceContainer.Instance.Get<ToastNotificationManager>(suppressWarning: true);
            }
            
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
            {
                // Success/info routing: in Foundation useremo TypeSpec + payload (qui usiamo template generico {message}).
                string code = type switch
                {
                    PotEvents.PotActionType.Water => "POT-WATER-SUCCESS",
                    PotEvents.PotActionType.Light => "POT-LIGHT-SUCCESS",
                    PotEvents.PotActionType.Plant => "POT-PLANT-SUCCESS",
                    PotEvents.PotActionType.Fertilize => "POT-FERTILIZE-SUCCESS",
                    PotEvents.PotActionType.Harvest => "POT-HARVEST-SUCCESS",
                    PotEvents.PotActionType.Spray => "POT-SPRAY-SUCCESS",
                    PotEvents.PotActionType.Uproot => "POT-UPROOT-SUCCESS",
                    _ => "POT-ACTION-SUCCESS"
                };
                foundation.PostToast(code, new NotificationPayload().With("message", text), NotificationSeverity.Success);
            }
            else if (_toastManager != null)
            {
                string code = type switch
                {
                    PotEvents.PotActionType.Water => "POT-WATER-SUCCESS",
                    PotEvents.PotActionType.Light => "POT-LIGHT-SUCCESS",
                    PotEvents.PotActionType.Plant => "POT-PLANT-SUCCESS",
                    PotEvents.PotActionType.Fertilize => "POT-FERTILIZE-SUCCESS",
                    PotEvents.PotActionType.Harvest => "POT-HARVEST-SUCCESS",
                    PotEvents.PotActionType.Spray => "POT-SPRAY-SUCCESS",
                    PotEvents.PotActionType.Uproot => "POT-UPROOT-SUCCESS",
                    _ => "POT-ACTION-SUCCESS"
                };
                _toastManager.ShowSuccess(text, code);
            }
            else if (_notification != null)
            {
                _notification.ShowNotification(text, 2, Color.green);
            }
        }
    }
}