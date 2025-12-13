using UnityEngine;
using Sporae.Dome.PotSystem.Growth;

namespace _Project.Pot
{
    public class PotNotifications
    {
        private readonly UINotification _notification;
        
        public PotNotifications()
        {
            _notification = Object.FindObjectOfType<UINotification>();

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
            
            // GDD AZ-11: Usa il messaggio specifico se disponibile, altrimenti usa messaggio generico
            if (!string.IsNullOrEmpty(message))
            {
                text = message;
            }
            else
            {
                // Fallback a messaggi generici se message è vuoto
                text = type switch
                {
                    PotEvents.PotActionType.Light => "You cannot illuminate the plant.",
                    PotEvents.PotActionType.Plant => "You cannot plant the plant.",
                    PotEvents.PotActionType.Water => "You failed to water the plant",
                    PotEvents.PotActionType.Fertilize => "You cannot fertilize the plant.",  // BLK-03.01-T1
                    PotEvents.PotActionType.Harvest => "You cannot harvest the plant.",
                    PotEvents.PotActionType.Spray => "You cannot spray the plant.",
                    PotEvents.PotActionType.Uproot => "You cannot uproot the plant.",
                    _ => "Action failed."
                };
            }

            _notification.ShowNotification(text, 2, Color.red);
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
                // GDD AZ-11: Messaggio per toggle sistema irrigazione
                if (pot != null && pot.PotActions != null && pot.PotActions.IsWateringSystemOn())
                {
                    text = "Il sistema di Irrigazione a goccia è attivo";
                }
                else
                {
                    text = "Il sistema di Irrigazione a goccia è disattivato";
                }
            }
            else if (type == PotEvents.PotActionType.Light)
            {
                // BLK-02.07: Messaggio specifico per toggle LED
                if (pot != null && pot.PotActions != null)
                {
                    var ledState = pot.PotActions.GetLedSystemState();
                    switch (ledState)
                    {
                        case LedSystemState.Off:
                            text = $"Hai spento il LED ({pot.PotId})";
                            break;
                        case LedSystemState.Blue:
                            text = $"Hai acceso il LED Blu ({pot.PotId})";
                            break;
                        case LedSystemState.Red:
                            text = $"Hai acceso il LED Rosso ({pot.PotId})";
                            break;
                        default:
                            text = "Hai modificato il LED";
                            break;
                    }
                }
                else
                {
                    text = "You have successfully illuminated the plant.";
                }
            }
            else
            {
                text = type switch
                {
                    PotEvents.PotActionType.Plant => "You have successfully planted the plant.",
                    PotEvents.PotActionType.Fertilize => "Hai fertilizzato la pianta in maniera corretta",  // BLK-03.01-T1
                    PotEvents.PotActionType.Harvest => "You have successfully harvested the plant.",
                    PotEvents.PotActionType.Spray => "You have successfully sprayed the plant.",
                    PotEvents.PotActionType.Uproot => "You have successfully uprooted the plant.",
                    _ => "Action completed successfully."
                };
            }

            _notification.ShowNotification(text, 2, Color.green);
        }
    }
}