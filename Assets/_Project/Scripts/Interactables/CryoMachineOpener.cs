using UnityEngine;
using _Project;
using Sporae.UI.UIToolkit.CryoMachine;
using Sporae.DevTools;

/// <summary>
/// Aggiunge interattività alla Cryo Machine.
/// Richiede un componente <see cref="Interactable"/> sullo stesso GameObject.
/// Quando il player interagisce (tasto E o click nel range), apre il pannello HUD della Cryo Machine.
/// 
/// Setup Unity Editor:
///   1. Aggiungi questo script al GameObject della Cryo Machine.
///   2. Aggiungi il componente Interactable sullo stesso GameObject.
///   3. Assegna il campo PanelController con il GameObject che ha CryoMachinePanelController.
/// </summary>
[RequireComponent(typeof(Interactable))]
public class CryoMachineOpener : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Controller del pannello HUD Cryo Machine. Deve avere il componente CryoMachinePanelController.")]
    [SerializeField] private CryoMachinePanelController _panelController;

    private Interactable _interactable;

    private void Awake()
    {
        _interactable = GetComponent<Interactable>();
        if (_interactable != null)
            _interactable.OnInteract += HandleInteract;
        else
            SporiumLogger.LogError(LogCategory.UI, "[CryoMachineOpener] Componente Interactable non trovato sullo stesso GameObject.");
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
            SporiumLogger.LogError(LogCategory.UI, "[CryoMachineOpener] PanelController non assegnato. Assegnarlo nell'Inspector.");
            return;
        }
        _panelController.Show();
    }
}
