using UnityEngine;
using _Project;
using _Project.Sporae.Core;
using Sporae.Core;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.NotificationsFoundation;

namespace _Project
{
    /// <summary>
    /// Punto di raccolta condensa (WAT-RAW) in Kitchen, vicino alla food machine.
    /// Stessa logica economica di <see cref="Sporae.UI.UIToolkit.HUD.TopBarController"/> collect + toast WATER-001.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class KitchenCondensationCollectPoint : MonoBehaviour
    {
        private Interactable _interactable;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
        }

        private void OnEnable()
        {
            if (_interactable != null)
                _interactable.OnInteract += OnInteract;
        }

        private void OnDisable()
        {
            if (_interactable != null)
                _interactable.OnInteract -= OnInteract;
        }

        private void OnInteract()
        {
            GameManager gm = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            if (gm == null || gm.CondensationSystem == null)
                return;
            if (gm.CondensationSystem.CurrentAccumulation <= 0f)
                return;

            int reward = gm.CollectCondensation();
            if (reward <= 0)
                return;

            gm.PlayerInventory.Add(Items.Water, reward);

            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
            {
                foundation.PostToast("WATER-001", new NotificationPayload().With("amount", reward.ToString()));
            }
            else
            {
                var toastManager = ServiceContainer.Instance?.Get<ToastNotificationManager>(suppressWarning: true);
                if (toastManager != null)
                    toastManager.ShowToast(ToastNotificationType.ResourceGained, $"You collected Rainwater: {reward}!", "WATER-001");
            }
        }
    }
}
