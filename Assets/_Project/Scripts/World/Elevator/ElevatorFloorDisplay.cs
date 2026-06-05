using _Project;
using TMPro;
using UnityEngine;

/// <summary>
/// Display laterale di un piano dell'ascensore (Fase 3).
/// È SIA indicatore (testo piano + freccia direzione) SIA oggetto interagibile:
/// premendo E / cliccando (via <see cref="Interactable"/>) chiama l'ascensore a questo piano.
/// A riposo mostra l'etichetta del proprio piano; durante chiamata/viaggio l'ElevatorSystem
/// può forzare tutti i display allo stesso contenuto.
/// Riferimenti opzionali: se un campo non è assegnato, i metodi degradano senza bloccare.
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

    [Header("UI (placeholder)")]
    [Tooltip("Testo principale: 'Floor X · ambienti'.")]
    [SerializeField] private TMP_Text labelText;

    [Tooltip("Testo freccia direzione (placeholder). Lasciare vuoto se non usato.")]
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
        if (_interactable != null)
            _interactable.SetRepeatInteractionWhileInRange(true);
    }

    private void OnEnable()
    {
        if (_interactable != null)
            _interactable.OnInteract += HandleInteract;
        if (elevator != null)
            elevator.RegisterDisplay(this);
    }

    private void OnDisable()
    {
        if (_interactable != null)
            _interactable.OnInteract -= HandleInteract;
        if (elevator != null)
            elevator.UnregisterDisplay(this);
    }

    private void HandleInteract()
    {
        if (elevator != null)
            elevator.CallToFloor(floorIndex);
    }

    /// <summary>Aggiorna testo piano e freccia direzione. Chiamato dall'ElevatorSystem.</summary>
    public void SetContent(string label, ElevatorDirection direction)
    {
        if (labelText != null)
            labelText.text = label;

        if (arrowText != null)
        {
            arrowText.text = direction == ElevatorDirection.Up ? upGlyph
                           : direction == ElevatorDirection.Down ? downGlyph
                           : string.Empty;
        }
    }
}
