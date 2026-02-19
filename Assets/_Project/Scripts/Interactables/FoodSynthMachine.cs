using UnityEngine;
using _Project.Sporae.Core;
using Sporae.UI.UIToolkit.FoodRoom;

namespace _Project
{
    [RequireComponent(typeof(Interactable))]
    public class FoodSynthMachine : MonoBehaviour
    {
        [SerializeField] private FoodRoomPanelController _foodRoomPanel;

        private Interactable _interactable;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
            if (_foodRoomPanel == null)
                _foodRoomPanel = FindObjectOfType<FoodRoomPanelController>();
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
            if (_foodRoomPanel != null)
                _foodRoomPanel.Show();
        }
    }
}
