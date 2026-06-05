using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger "dentro cabina" per un singolo piano (Fase 5).
/// Segnala ingresso/uscita player all'ElevatorSystem.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class ElevatorCabinZone : MonoBehaviour
{
    [Tooltip("Indice piano, stesso ordine di levels[] su ElevatorSystem (0=+1, 1=0, 2=-1, 3=-2).")]
    [SerializeField] private int floorIndex;

    [Tooltip("Riferimento all'ElevatorSystem. Se vuoto, viene risolto a runtime.")]
    [SerializeField] private ElevatorSystem elevator;

    [Tooltip("Frazione altezza del trigger (dal lato corridoio verso l'interno) oltre cui conta \"dentro cabina\".")]
    [SerializeField] [Range(0.2f, 0.9f)] private float cabinaDepthFraction = 0.45f;

    public int FloorIndex => floorIndex;
    public float CabinaDepthFraction => cabinaDepthFraction;

    private readonly HashSet<Collider2D> _playerContacts = new HashSet<Collider2D>();

    /// <summary>Collegamento controllato da ElevatorSystem (gerarchia ELEV_Elevator).</summary>
    public void BindElevator(ElevatorSystem system)
    {
        if (system == null)
            return;

        if (elevator == system)
            return;

        if (elevator != null && isActiveAndEnabled)
            elevator.UnregisterCabinZone(this);

        elevator = system;

        if (isActiveAndEnabled)
            elevator.RegisterCabinZone(this);
    }

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnEnable()
    {
        if (elevator != null)
            elevator.RegisterCabinZone(this);
    }

    private void OnDisable()
    {
        _playerContacts.Clear();

        if (elevator != null)
            elevator.UnregisterCabinZone(this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || elevator == null)
            return;

        _playerContacts.Add(other);
        elevator.HandleCabinZoneContact(floorIndex, other.transform, GetComponent<Collider2D>());
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || elevator == null)
            return;

        _playerContacts.Add(other);
        var zoneCol = GetComponent<Collider2D>();
        elevator.HandleCabinZoneContact(floorIndex, other.transform, zoneCol);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || elevator == null)
            return;

        _playerContacts.Remove(other);
        if (_playerContacts.Count > 0)
            return;

        elevator.NotifyPlayerExitedCabinZone(floorIndex);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        var col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;

        if (floorIndex < 0)
            Debug.LogWarning($"[{name}] ElevatorCabinZone: Floor Index deve essere 0=+1, 1=0, 2=-1, 3=-2 (non il numero del piano!).", this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.35f);
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            var b = col.bounds;
            Gizmos.DrawCube(b.center, b.size);
        }
    }
#endif
}
