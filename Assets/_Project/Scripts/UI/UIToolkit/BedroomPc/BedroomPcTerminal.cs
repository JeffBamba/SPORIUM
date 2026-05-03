using _Project;
using _Project.BlackMarket;
using Sporae.DevTools;
using UnityEngine;

namespace Sporae.UI.UIToolkit.BedroomPc
{
    [RequireComponent(typeof(Interactable))]
    public sealed class BedroomPcTerminal : MonoBehaviour
    {
        [SerializeField] private BedroomPcDisplayController _displayController;
        [SerializeField] private UIBlackMarket _blackMarketUI;

        private Interactable _interactable;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
            if (_displayController == null)
                _displayController = GetComponent<BedroomPcDisplayController>();
        }

        private void OnEnable()
        {
            if (_interactable != null)
                _interactable.OnInteract += HandleInteract;

            if (_displayController != null)
            {
                _displayController.BlackMarketRequested += HandleBlackMarketRequested;
            }
        }

        private void OnDisable()
        {
            if (_interactable != null)
                _interactable.OnInteract -= HandleInteract;

            if (_displayController != null)
            {
                _displayController.BlackMarketRequested -= HandleBlackMarketRequested;
            }
        }

        private void HandleInteract()
        {
            if (_displayController == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "[BedroomPC] DisplayController non assegnato.");
                return;
            }

            _displayController.Show();
        }

        private void HandleBlackMarketRequested()
        {
            _displayController?.Hide();
            if (_blackMarketUI != null)
                _blackMarketUI.Show();
            else
                SporiumLogger.LogWarning(LogCategory.UI, "[BedroomPC] BlackMarket UI non assegnata.");
        }

    }
}
