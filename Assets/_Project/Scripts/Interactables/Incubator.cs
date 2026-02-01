using Sporae.UI.UIToolkit.Lab;
using UnityEngine;

namespace _Project
{
    [RequireComponent(typeof(Interactable))]
    public class Incubator : MonoBehaviour
    {
        [Header("Lab UI — prefer Foundation UIToolkit")]
        [SerializeField] private LabIncubatorPanelController _labIncubatorPanel;
        [SerializeField] private IncubatorUI _legacyIncubatorUI;

        private Interactable _interactable;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
            if (_interactable != null)
                _interactable.OnInteract += HandleInteract;
        }

        private void OnDestroy()
        {
            if (_interactable != null)
                _interactable.OnInteract -= HandleInteract;
        }

        private void HandleInteract()
        {
            if (_labIncubatorPanel != null)
                _labIncubatorPanel.Show();
            else if (_legacyIncubatorUI != null)
                _legacyIncubatorUI.ShowEvening();
        }
    }
}
