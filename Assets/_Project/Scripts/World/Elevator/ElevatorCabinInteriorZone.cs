using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger fisico "dentro cabina" per un singolo piano (Elevator 4.0).
/// Sostituisce la rilevazione UV fragile quando configurato; il legacy <see cref="ElevatorCabinZone"/> resta come fallback.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class ElevatorCabinInteriorZone : MonoBehaviour
{
    [Tooltip("Indice piano, stesso ordine di levels[] su ElevatorSystem (0=+1, 1=0, 2=-1, 3=-2).")]
    [SerializeField] private int floorIndex;

    [Tooltip("Riferimento all'ElevatorSystem. Se vuoto, viene risolto a runtime.")]
    [SerializeField] private ElevatorSystem elevator;

    [Tooltip("Punto opzionale di atterraggio interno cabina (override posizione mondo).")]
    [SerializeField] private Transform landingPoint;

    private readonly HashSet<Collider2D> _playerContacts = new HashSet<Collider2D>();

    public int FloorIndex => floorIndex;
    public Transform LandingPoint => landingPoint;

    public void BindElevator(ElevatorSystem system)
    {
        if (system == null)
            return;

        if (elevator == system)
            return;

        if (elevator != null && isActiveAndEnabled)
            elevator.UnregisterInteriorZone(this);

        elevator = system;

        if (isActiveAndEnabled)
            elevator.RegisterInteriorZone(this);
    }

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnEnable()
    {
        if (elevator != null)
            elevator.RegisterInteriorZone(this);
    }

    private void OnDisable()
    {
        _playerContacts.Clear();

        if (elevator != null)
            elevator.UnregisterInteriorZone(this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || elevator == null || !elevator.IsCabAtFloor(floorIndex))
            return;

        _playerContacts.Add(other);
        elevator.NotifyInteriorZoneEnter(floorIndex, other.transform);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || elevator == null || !elevator.IsCabAtFloor(floorIndex))
            return;

        _playerContacts.Add(other);
        elevator.NotifyInteriorZoneStay(floorIndex, other.transform);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || elevator == null)
            return;

        _playerContacts.Remove(other);
        if (_playerContacts.Count > 0)
            return;

        if (!elevator.IsCabAtFloor(floorIndex) && !elevator.IsPlayerInsideCabinOnFloor(floorIndex))
            return;

        elevator.NotifyInteriorZoneExit(floorIndex);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;

        if (floorIndex < 0)
            Debug.LogWarning($"[{name}] ElevatorCabinInteriorZone: Floor Index deve essere 0=+1, 1=0, 2=-1, 3=-2.", this);
    }
#endif
}
