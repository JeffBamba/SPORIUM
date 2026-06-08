using System.Collections.Generic;
using _Project;
using Sporae.Core.Localization;
using Sporae.UI.UIToolkit.ElevatorDisplay;
using TMPro;
using UnityEngine;

/// <summary>
/// Display laterale di un piano dell'ascensore (Fase 3).
/// È SIA indicatore (testo piano + freccia direzione) SIA oggetto interagibile:
/// premendo E / cliccando (via <see cref="Interactable"/>) chiama l'ascensore a questo piano.
/// Con <see cref="ElevatorInGameDisplayRuntime"/> attivo usa il pannello UITK sincronizzato;
/// altrimenti degrada sui TMP legacy.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Interactable))]
public class ElevatorFloorDisplay : MonoBehaviour
{
    [Header("Identità piano")]
    [Tooltip("Indice del piano, stesso ordine di levels[] su ElevatorSystem (0=+1, 1=0, 2=-1, 3=-2).")]
    [SerializeField] private int floorIndex;

    [Tooltip("Riferimento all'ElevatorSystem. Se vuoto, viene risolto a runtime.")]
    [SerializeField] private ElevatorSystem elevator;

    [Header("UI Toolkit (benchmark)")]
    [Tooltip("Pannello world-space UITK. Se assente, viene cercato sullo stesso GameObject.")]
    [SerializeField] private ElevatorInGameDisplayRuntime uiDisplayRuntime;

    [Header("UI legacy (TMP)")]
    [Tooltip("Testo principale: 'Floor X · ambienti'. Nascosto quando uiDisplayRuntime è attivo.")]
    [SerializeField] private TMP_Text labelText;

    [Tooltip("Testo freccia direzione. Nascosto quando uiDisplayRuntime è attivo.")]
    [SerializeField] private TMP_Text arrowText;

    [SerializeField] private string upGlyph = "\u25B2";   // ▲
    [SerializeField] private string downGlyph = "\u25BC"; // ▼

    private Interactable _interactable;

    public int FloorIndex => floorIndex;

    /// <summary>Collegamento controllato da ElevatorSystem (gerarchia ELEV_Elevator).</summary>
    public void BindElevator(ElevatorSystem system)
    {
        if (system == null)
            return;

        if (elevator == system)
            return;

        if (elevator != null && isActiveAndEnabled)
            elevator.UnregisterDisplay(this);

        elevator = system;

        if (isActiveAndEnabled)
            elevator.RegisterDisplay(this);
    }

    private void Awake()
    {
        _interactable = GetComponent<Interactable>();

        if (uiDisplayRuntime == null)
            uiDisplayRuntime = GetComponent<ElevatorInGameDisplayRuntime>();
    }

    private void OnEnable()
    {
        if (_interactable != null)
            _interactable.OnInteract += HandleInteract;
        if (elevator != null)
            elevator.RegisterDisplay(this);

        RefreshLegacyTmpVisibility();
    }

    private void OnDisable()
    {
        if (_interactable != null)
            _interactable.OnInteract -= HandleInteract;
        if (elevator != null)
            elevator.UnregisterDisplay(this);
    }

    private void LateUpdate()
    {
        if (_interactable == null || elevator == null)
            return;

        _interactable.SetInteractionAvailable(elevator.CanCallFromFloorDisplay());
    }

    private void HandleInteract()
    {
        if (elevator == null || !elevator.CanCallFromFloorDisplay())
            return;

        elevator.CallToFloor(floorIndex);
    }

    /// <summary>Aggiorna pannello UITK o fallback TMP. Chiamato dall'ElevatorSystem.</summary>
    public void SetPanelState(int highlightFloorIndex, ElevatorDirection direction, IReadOnlyList<string> floorLabels, ElevatorDisplayMode mode = ElevatorDisplayMode.Normal)
    {
        if (UsesUiToolkitDisplay())
        {
            SetLegacyTmpVisible(false);
            uiDisplayRuntime.SetState(highlightFloorIndex, direction, floorLabels, mode);
            return;
        }

        SetLegacyTmpVisible(true);
        if (mode == ElevatorDisplayMode.CallRemote)
        {
            string floorLabel = ResolveHighlightLabel(highlightFloorIndex, floorLabels);
            string busy = LocalizationManager.Pick("Occupato", "Busy");
            SetContent($"{busy}\n{floorLabel}", ElevatorDirection.None);
            return;
        }

        if (mode == ElevatorDisplayMode.CabinAtFloor)
        {
            string floorLabel = ResolveHighlightLabel(highlightFloorIndex, floorLabels);
            string youAreAt = LocalizationManager.Pick("Ti trovi al", "You are at");
            SetContent($"{youAreAt}\n{floorLabel}", ElevatorDirection.None);
            return;
        }

        if (mode == ElevatorDisplayMode.CabinSelectingTarget)
        {
            string floorLabel = ResolveHighlightLabel(highlightFloorIndex, floorLabels);
            string goingTo = LocalizationManager.Pick("Stai andando a", "You are going to");
            SetContent($"{goingTo}\n{floorLabel}", ElevatorDirection.None);
            return;
        }

        string label = ResolveHighlightLabel(highlightFloorIndex, floorLabels);
        if (mode == ElevatorDisplayMode.Normal && direction == ElevatorDirection.None)
        {
            string youAreAt = LocalizationManager.Pick("Ti trovi al", "You are at");
            SetContent($"{youAreAt}\n{label}", ElevatorDirection.None);
            return;
        }

        SetContent(label, direction);
    }

    /// <summary>Fallback TMP — testo piano e freccia direzione.</summary>
    public void SetContent(string label, ElevatorDirection direction)
    {
        if (labelText != null)
            labelText.text = string.IsNullOrEmpty(label) ? label : label.ToUpperInvariant();

        if (arrowText != null)
        {
            arrowText.text = direction == ElevatorDirection.Up ? upGlyph
                           : direction == ElevatorDirection.Down ? downGlyph
                           : string.Empty;
        }
    }

    private bool UsesUiToolkitDisplay() =>
        uiDisplayRuntime != null && uiDisplayRuntime.isActiveAndEnabled;

    private void RefreshLegacyTmpVisibility()
    {
        SetLegacyTmpVisible(!UsesUiToolkitDisplay());
    }

    private void SetLegacyTmpVisible(bool visible)
    {
        if (labelText != null)
            labelText.gameObject.SetActive(visible);
        if (arrowText != null)
            arrowText.gameObject.SetActive(visible);
    }

    private string ResolveHighlightLabel(int highlightFloorIndex, IReadOnlyList<string> floorLabels)
    {
        if (floorLabels != null && highlightFloorIndex >= 0 && highlightFloorIndex < floorLabels.Count)
            return floorLabels[highlightFloorIndex];

        if (elevator != null && highlightFloorIndex >= 0)
            return elevator.GetFloorLabel(highlightFloorIndex);

        return string.Empty;
    }
}
