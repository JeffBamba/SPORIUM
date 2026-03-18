using UnityEngine;
using _Project;
using Sporae.DevTools;

namespace Sporae.UI.UIToolkit.PlantCardV3
{
    /// <summary>
    /// Script da mettere sul GameObject Terminale (con Interactable) per aprire PlantCardV3.
    /// Pattern coerente con BlackMarketTerminal.
    /// </summary>
    public class PlantCardV3TerminalOpener : MonoBehaviour
    {
        [SerializeField] private PlantCardV3TerminalController _terminalUI;

        private Interactable _interactable;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
            if (_interactable != null)
                _interactable.OnInteract += HandleInteract;
            else
                SporiumLogger.LogWarning(LogCategory.UI, "PlantCardV3TerminalOpener: manca Interactable sullo stesso GameObject.");
        }

        private void OnDestroy()
        {
            if (_interactable != null)
                _interactable.OnInteract -= HandleInteract;
        }

        private void HandleInteract()
        {
            if (_terminalUI == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "PlantCardV3TerminalOpener: Terminal UI non assegnata!");
                return;
            }

            _terminalUI.Open();
        }
    }
}

