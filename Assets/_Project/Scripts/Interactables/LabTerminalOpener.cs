using _Project;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.Lab;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class LabTerminalOpener : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LabTerminalPanelController _panelController;

    private Interactable _interactable;

    private void Awake()
    {
        _interactable = GetComponent<Interactable>();
        if (_interactable != null)
            _interactable.OnInteract += HandleInteract;
        else
            SporiumLogger.LogError(LogCategory.UI, "[LabTerminalOpener] Componente Interactable non trovato sullo stesso GameObject.");
    }

    private void OnDestroy()
    {
        if (_interactable != null)
            _interactable.OnInteract -= HandleInteract;
    }

    private void HandleInteract()
    {
        if (_panelController == null)
        {
            SporiumLogger.LogError(LogCategory.UI, "[LabTerminalOpener] PanelController non assegnato. Assegnarlo nell'Inspector.");
            return;
        }

        _panelController.Show();
    }
}
