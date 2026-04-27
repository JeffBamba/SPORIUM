using UnityEngine;
using Sporae.UI.UIToolkit.FoodRoom;

namespace _Project
{
    [RequireComponent(typeof(Interactable))]
    public class CondenseTankMachine : MonoBehaviour
    {
        [SerializeField] private CondenseTankPanelController _condenseTankPanel;

        private Interactable _interactable;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
        }

        private void OnEnable()
        {
            if (_interactable != null)
                _interactable.OnInteract += OnInteractClicked;
        }

        private void OnDisable()
        {
            if (_interactable != null)
                _interactable.OnInteract -= OnInteractClicked;
        }

        private void OnInteractClicked()
        {
            if (_condenseTankPanel != null)
                _condenseTankPanel.Show();
        }
    }
}
