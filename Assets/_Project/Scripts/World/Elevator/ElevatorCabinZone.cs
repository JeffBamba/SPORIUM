using System.Collections.Generic;
using _Project.Player;
using _Project.World.VaultMap;
using UnityEngine;

/// <summary>
/// Trigger "dentro cabina" per un singolo piano (Fase 5).
/// Profondità corridoio/cabina letta dal <see cref="PerspectiveWalkArea2D"/> (UV v), non da Y mondo.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class ElevatorCabinZone : MonoBehaviour
{
    [Tooltip("Indice piano, stesso ordine di levels[] su ElevatorSystem (0=+1, 1=0, 2=-1, 3=-2).")]
    [SerializeField] private int floorIndex;

    [Tooltip("Riferimento all'ElevatorSystem. Se vuoto, viene risolto a runtime.")]
    [SerializeField] private ElevatorSystem elevator;

    [Header("Profondità cabina (UV walk area)")]
    [Tooltip("Trapezio 2.5D del pianerottolo/cabina. Se vuoto, risolto dal centro del trigger.")]
    [SerializeField] private PerspectiveWalkArea2D walkArea;

    [Tooltip("Soglia corridoio (v basso/near): sotto = shallow, aprono le porte.")]
    [SerializeField] [Range(0f, 1f)] private float cabinaShallowV = 0.22f;

    [Tooltip("Soglia interno cabina (v alto/far): oltre = deep, chiude porte e attiva selezione.")]
    [SerializeField] [Range(0f, 1f)] private float cabinaDeepV = 0.55f;

    [Tooltip("v usato per atterraggio viaggio dentro cabina. 0 = cabinaDeepV + 0.17.")]
    [SerializeField] [Range(0f, 1f)] private float interiorLandingV;

    [Header("Anchor opzionali (override UV)")]
    [SerializeField] private Transform exitApproach;
    [SerializeField] private Transform interiorLanding;

    [Header("Fallback legacy (solo se walk area assente o UV non proiettabile)")]
    [Tooltip("Frazione altezza trigger in Y mondo quando UV non disponibile.")]
    [SerializeField] [Range(0.2f, 0.9f)] private float cabinaDepthFraction = 0.45f;

    public int FloorIndex => floorIndex;
    public float CabinaDepthFraction => cabinaDepthFraction;
    public PerspectiveWalkArea2D WalkArea => walkArea;
    public float CabinaShallowV => cabinaShallowV;
    public float CabinaDeepV => cabinaDeepV;

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

    public PerspectiveWalkArea2D ResolveWalkArea()
    {
        if (walkArea != null && walkArea.HasValidCorners)
            return walkArea;

        Collider2D col = GetComponent<Collider2D>();
        Vector2 probe = col != null ? col.bounds.center : (Vector2)transform.position;
        return PlayerPerspectiveMover2D.FindWalkAreaForWorldPoint(probe);
    }

    public bool TryGetPlayerDepthV(Vector3 worldPos, out float v)
    {
        v = 0f;
        PerspectiveWalkArea2D area = ResolveWalkArea();
        if (area == null || !area.HasValidCorners || !area.TryProjectWorldToUV(worldPos, out Vector2 uv))
            return false;

        v = uv.y;
        return true;
    }

    public bool IsPlayerDeepInCabina(Vector3 playerPos, Collider2D zoneCol)
    {
        if (TryGetPlayerDepthV(playerPos, out float v))
            return v >= cabinaDeepV;

        return IsPlayerDeepInCabinaLegacy(zoneCol, playerPos);
    }

    public Vector2 GetExitApproachWorldPosition(float fallbackU = 0.5f)
    {
        if (exitApproach != null)
            return exitApproach.position;

        return ProjectZonePointToWalkArea(GetLegacyShallowWorldPosition(GetComponent<Collider2D>()));
    }

    public Vector2 GetInteriorLandingWorldPosition(float fallbackU = 0.5f)
    {
        if (interiorLanding != null)
            return interiorLanding.position;

        return ProjectZonePointToWalkArea(GetLegacyInteriorWorldPosition(GetComponent<Collider2D>()));
    }

    /// <summary>
    /// Punto mondo dal collider cabina, riproiettato sulla walk area (Report 112).
    /// Evita MapToWorld(u,v) al centro stanza — segue la posizione reale del trigger cabina.
    /// </summary>
    private Vector2 ProjectZonePointToWalkArea(Vector2 zoneWorldProbe)
    {
        PerspectiveWalkArea2D area = ResolveWalkArea();
        if (area != null && area.HasValidCorners && area.TryProjectWorldToUV(zoneWorldProbe, out Vector2 uv))
            return area.MapToWorld(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y));

        return zoneWorldProbe;
    }

    private bool IsPlayerDeepInCabinaLegacy(Collider2D zoneCol, Vector3 playerPos)
    {
        if (zoneCol == null)
            return true;

        Bounds b = zoneCol.bounds;
        float threshold = b.min.y + b.size.y * Mathf.Clamp01(cabinaDepthFraction);
        return playerPos.y >= threshold;
    }

    private Vector2 GetLegacyShallowWorldPosition(Collider2D zoneCol)
    {
        if (zoneCol != null)
        {
            Bounds b = zoneCol.bounds;
            return new Vector2(b.center.x, b.min.y + b.size.y * 0.15f);
        }

        return transform.position;
    }

    private Vector2 GetLegacyInteriorWorldPosition(Collider2D zoneCol)
    {
        if (zoneCol != null)
        {
            Bounds b = zoneCol.bounds;
            float depth = Mathf.Clamp01(cabinaDepthFraction + 0.3f);
            return new Vector2(b.center.x, b.min.y + b.size.y * depth);
        }

        return transform.position;
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
        Collider2D zoneCol = GetComponent<Collider2D>();
        elevator.HandleCabinZoneContact(floorIndex, other.transform, zoneCol);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || elevator == null)
            return;

        _playerContacts.Remove(other);
        // #region agent log
        DebugSessionLog_d2269f.Write("L", "ElevatorCabinZone.OnTriggerExit2D", "trigger exit",
            "{\"floor\":" + floorIndex + ",\"remainingContacts\":" + _playerContacts.Count + "}");
        // #endregion
        if (_playerContacts.Count > 0)
            return;

        elevator.NotifyPlayerExitedCabinZone(floorIndex);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;

        if (cabinaDeepV <= cabinaShallowV)
            cabinaDeepV = Mathf.Min(1f, cabinaShallowV + 0.1f);

        if (floorIndex < 0)
            Debug.LogWarning($"[{name}] ElevatorCabinZone: Floor Index deve essere 0=+1, 1=0, 2=-1, 3=-2 (non il numero del piano!).", this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.35f);
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Bounds b = col.bounds;
            Gizmos.DrawCube(b.center, b.size);
        }

        PerspectiveWalkArea2D area = ResolveWalkArea();
        if (area == null || !area.HasValidCorners)
            return;

        DrawDepthLine(area, cabinaShallowV, new Color(0.2f, 0.85f, 1f, 0.9f));
        DrawDepthLine(area, cabinaDeepV, new Color(1f, 0.45f, 0.1f, 0.9f));

        Collider2D zoneCol = GetComponent<Collider2D>();
        if (zoneCol != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(ProjectZonePointToWalkArea(GetLegacyShallowWorldPosition(zoneCol)), 0.12f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(ProjectZonePointToWalkArea(GetLegacyInteriorWorldPosition(zoneCol)), 0.12f);
        }

        if (exitApproach != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(exitApproach.position, 0.12f);
        }

        if (interiorLanding != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(interiorLanding.position, 0.12f);
        }
    }

    private static void DrawDepthLine(PerspectiveWalkArea2D area, float v, Color color)
    {
        Gizmos.color = color;
        Vector3 a = area.MapToWorld(0f, v);
        Vector3 b = area.MapToWorld(1f, v);
        Gizmos.DrawLine(a, b);
    }
#endif
}
