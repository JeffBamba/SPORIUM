using UnityEngine;
using _Project;
using Sporae.UI.UIToolkit.DispensaRefrigerata;
using Sporae.DevTools;

/// <summary>
/// Opener del pannello HUD della Dispensa Refrigerata (cucina).
/// Il GameObject deve avere anche un Interactable: all'interazione (E / click)
/// apre il <see cref="DispensaPanelController"/> dedicato.
/// </summary>
[RequireComponent(typeof(Interactable))]
public class DispensaRefrigerataOpener : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Controller del pannello Dispensa Refrigerata (HUD).")]
    [SerializeField] private DispensaPanelController _dispensaPanel;

    private Interactable _interactable;

    private void Awake()
    {
        _interactable = GetComponent<Interactable>();
        if (_dispensaPanel == null)
            _dispensaPanel = FindObjectOfType<DispensaPanelController>();
    }

    private void OnEnable()
    {
        if (_interactable != null)
            _interactable.OnInteract += HandleInteract;
    }

    private void OnDisable()
    {
        if (_interactable != null)
            _interactable.OnInteract -= HandleInteract;
    }

    private void HandleInteract()
    {
        if (_dispensaPanel == null)
        {
            SporiumLogger.LogError(LogCategory.UI, "[DispensaRefrigerataOpener] DispensaPanelController non assegnato.");
            return;
        }
        _dispensaPanel.Show();
    }
}
