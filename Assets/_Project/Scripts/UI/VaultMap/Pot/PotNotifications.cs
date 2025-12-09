using UnityEngine;

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
                    _ => "You cannot uproot the plant."
                };
            }

            _notification.ShowNotification(text, 2, Color.red);
        }

        private void HandlePotAction(PotEvents.PotActionType type, PotSlot pot)
        {
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
            else
            {
                text = type switch
                {
                    PotEvents.PotActionType.Light => "You have successfully illuminated the plant.",
                    PotEvents.PotActionType.Plant => "You have successfully planted the plant.",
                    _ => "You have successfully uprooted the plant."
                };
            }

            _notification.ShowNotification(text, 2, Color.green);
        }
    }
}