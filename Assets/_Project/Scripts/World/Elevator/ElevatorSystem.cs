using System;
using System.Collections.Generic;
using _Project;
using _Project.Player;
using _Project.Sporae.Core;
using _Project.World.VaultMap;
using Cinemachine;
using Sporae.DevTools;
using UnityEngine;

/// <summary>Direzione mostrata sui display dell'ascensore.</summary>
public enum ElevatorDirection
{
    None,
    Up,
    Down
}

public class ElevatorSystem : MonoBehaviour
{
    [Header("Elevator Configuration")]
    [SerializeField] private float elevatorSpeed = 1f;
    [SerializeField] private int startingLevelIndex;
    [SerializeField] private Transform[] levels;
    [SerializeField] private float teleportDelay = 0.1f;
    [SerializeField] private GameObject elevatorSection;

    [Header("Doors (Fase 2 — una coppia per piano, stesso ordine di levels[])")]
    [Tooltip("Coppia di ante per ogni piano. L'indice DEVE combaciare con levels[] (0=+1, 1=0, 2=-1, 3=-2).")]
    [SerializeField] private ElevatorDoorPair[] floorDoors;

    [Header("Displays (Fase 3 — etichette piano)")]
    [Tooltip("Etichette mostrate sui display, stesso ordine di levels[] (0=+1, 1=0, 2=-1, 3=-2). Se vuoto, usa i default di progetto.")]
    [SerializeField] private string[] floorLabels;

    [Header("Chiamata da display (Fase 4)")]
    [Tooltip("Secondi tra animazione direzione sui display e arrivo cabina (apertura porte).")]
    [SerializeField] private float callTravelDuration = 1.5f;

    [Header("Selezione in cabina (Fase 5)")]
    [Tooltip("Secondi di attesa dopo l'ultima pressione Su/Giù prima della partenza.")]
    [SerializeField] private float selectionDebounceSeconds = 1.2f;

    [Tooltip("Tolleranza verticale (unità mondo) tra player e livello per attivare la selezione in cabina.")]
    [SerializeField] private float cabinLevelVerticalTolerance = 1.75f;

    [Tooltip("Testo sui display quando il player entra in cabina (prima di premere Su/Giù o W/S).")]
    [SerializeField] private string cabinSelectionHint = "Usa \u2191 \u2193 o W S per scegliere il piano";

    [Header("Validation")]
    [SerializeField] private bool validateLevelsOnStart = true;

    [Header("Teleport Placement")]
    [Tooltip("Anchor di uscita per piano (stesso ordine di levels[]): davanti alle porte, dentro la walk area.")]
    [SerializeField] private Transform[] exitAnchors;

    [Tooltip("Walk area del pianerottolo per ogni levels[] (0=+1 …). Se vuoto, ricerca automatica per Y del piano.")]
    [SerializeField] private PerspectiveWalkArea2D[] floorLobbyWalkAreas;

    [Header("Viaggio in cabina (Fase 6)")]
    [Tooltip("Virtual Camera che segue il player; durante il viaggio segue elevatorSection.")]
    [SerializeField] private CinemachineVirtualCamera travelVirtualCamera;

    [Tooltip("Velocità verticale dello scroll camera lungo lo shaft (unità mondo/sec).")]
    [SerializeField] private float cabinTravelSpeed = 6f;

    [Tooltip("Nascondi lo sprite legacy ElevatorRoom durante il viaggio.")]
    [SerializeField] private bool hideElevatorRoomDuringTravel = true;

    [Tooltip("If true, teleport uses the target level Transform X as well as Y. This prevents landing inside walls when floors are not perfectly aligned in X.")]
    [SerializeField] private bool useTargetLevelXForTeleport = true;

    [Tooltip("Max allowed horizontal correction when using target-level X. If exceeded, we keep the starting X to avoid teleports into other rooms.")]
    [SerializeField] private float maxTeleportXCorrection = 1.25f;

    private static int WrapIndex(int i, int len)
    {
        if (len <= 0) return 0;
        int m = i % len;
        return m < 0 ? m + len : m;
    }

    private bool playerInside = false; // inside ELEV_UseZone trigger
    private Transform player;
    private bool isTeleporting = false;
    private Coroutine _teleportCoroutine;
    private int currentLevelIndex;
    private UINotification uiNotification;
    private bool _waitingForRuntimeServices;

    private readonly List<ElevatorFloorDisplay> _displays = new List<ElevatorFloorDisplay>();
    private readonly List<ElevatorCabinZone> _cabinZones = new List<ElevatorCabinZone>();
    private Coroutine _outOfServiceCoroutine;
    private Coroutine _callToFloorCoroutine;
    private Coroutine _departCoroutine;
    private Coroutine _hideAfterCabinDoorsCoroutine;

    // Fase 5 — selezione piano in cabina
    private bool _playerInsideCabinZone;
    private bool _cabinArrowSelectionActive;
    private int _cabinFloorIndex = -1;
    private int _cabinZoneOverlapCount;
    private int _targetIndex;
    private float _selectionDebounceRemaining = -1f;
    private bool _cabinInputBlocked;

    // Fase 6 — viaggio camera / hide player
    private Transform _savedCameraFollow;
    private SpriteRenderer _elevatorRoomRenderer;
    private SpriteRenderer[] _playerSpriteRenderers;
    private bool[] _playerSpriteStates;
    private Animator _playerAnimator;
    private bool _playerAnimatorState;
    private Collider2D[] _playerColliders;
    private bool[] _playerColliderStates;
    private Rigidbody2D _playerRigidbody;
    private bool _playerRigidbodySimulated;
    private bool _playerRigidbodySimulationOverridden;
    private bool _playerVisualCacheReady;
    private Transform _travelPlayer;
    private bool _playerHiddenForCabin;
    private int _playerHideDepth;
    private bool _postTravelArrival;
    private bool _postTravelReentryArmed;
    private int _suppressCabinActivationUntilExitFloor = -1;

    // Etichette di default (mappa di progetto). Usate se floorLabels non è compilato in Inspector.
    private static readonly string[] DefaultFloorLabels =
    {
        "Floor +1 \u00B7 Visitor Room & Seed Storage",
        "Floor 0 \u00B7 Serra & Lab",
        "Floor -1 \u00B7 BedRoom & Kitchen",
        "Floor -2 \u00B7 Out of Service",
    };

    void Start()
    {
        ValidateConfiguration();

        ResolveRuntimeDependencies();
        SubscribeToRuntimeServicesIfNeeded();

        currentLevelIndex = startingLevelIndex;
        _targetIndex = startingLevelIndex;

        ResetDisplaysToOwnFloors();
    }

    private void ValidateConfiguration()
    {
        if (validateLevelsOnStart)
        {
            if (levels == null || levels.Length == 0)
            {
                SporiumLogger.LogError(LogCategory.Core, "Nessun livello configurato!");
                enabled = false;
                return;
            }

            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] == null)
                {
                    SporiumLogger.LogError(LogCategory.Core, $"Livello {i} è null!");
                    enabled = false;
                    return;
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!enabled) return;

        if (other.CompareTag("Player"))
        {
            if (player != other.transform)
                InvalidatePlayerVisualCache();
            player = other.transform;
            playerInside = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!enabled) return;

        if (other.CompareTag("Player"))
        {
            if (!_playerInsideCabinZone && !isTeleporting)
                player = null;
            playerInside = false;
        }
    }

    private void Update()
    {
        if (!_playerInsideCabinZone || _postTravelArrival || isTeleporting || levels == null || levels.Length == 0)
            return;

        int cabinDelta = 0;
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            cabinDelta = -1;
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            cabinDelta = 1;

        if (cabinDelta != 0)
            BeginOrAdjustCabinSelection(cabinDelta);

        if (_cabinArrowSelectionActive && _selectionDebounceRemaining >= 0f)
        {
            _selectionDebounceRemaining -= Time.deltaTime;
            if (_selectionDebounceRemaining <= 0f)
                TryDepartToTarget();
        }
    }

    public void SetLevel(int levelIndex)
    {
        // Stop any running travel coroutine and restore world input / camera / player visibility.
        CancelCabinSelectionState(restoreWorldInput: true);

        if (_teleportCoroutine != null)
        {
            StopCoroutine(_teleportCoroutine);
            _teleportCoroutine = null;
            GameplayUiModalLock.SetBlockWorldInput(false);
            isTeleporting = false;
            RestoreCameraFollow();
            SetPlayerHidden(false);
            SetElevatorRoomVisible(true);
        }

        if (levels == null || levels.Length == 0)
            return;

        int idx = WrapIndex(levelIndex, levels.Length);
        currentLevelIndex = idx;
        _targetIndex = idx;
        elevatorSection.transform.position = new Vector3(
            elevatorSection.transform.position.x,
            levels[idx].position.y,
            elevatorSection.transform.position.z);

        ResetDisplaysToOwnFloors();
    }

    public void GoToLevel(int levelIndex)
    {
        if (!CanTeleportToLevel(levelIndex))
        {
            return;
        }

        if (!IsLevelUnlocked(levelIndex))
        {
            var toastManager = ServiceContainer.Instance?.Get<ToastNotificationManager>(suppressWarning: true);
            if (toastManager != null)
            {
                toastManager.ShowError("Sorry, out of order", "ELEVATOR-001");
            }
            else if (uiNotification != null)
            {
                uiNotification.ShowNotification("Sorry, out of order", 3, Color.red);
            }
            return;
        }

        if (player == null)
        {
            SporiumLogger.LogWarning(LogCategory.Core, "Player non trovato!");
            return;
        }

        // Stop any running coroutine before starting a new one and restore input.
        if (_teleportCoroutine != null)
        {
            StopCoroutine(_teleportCoroutine);
            _teleportCoroutine = null;
            GameplayUiModalLock.SetBlockWorldInput(false);
            isTeleporting = false;
            RestoreCameraFollow();
            SetPlayerHidden(false);
            SetElevatorRoomVisible(true);
        }

        _teleportCoroutine = StartCoroutine(TravelToFloorRoutine(currentLevelIndex, levelIndex));
    }

    /// <summary>
    /// Viaggio verticale: player nascosto, camera segue elevatorSection lungo lo shaft, teleport all'anchor di uscita.
    /// </summary>
    private System.Collections.IEnumerator TravelToFloorRoutine(int fromIndex, int toIndex)
    {
        Transform travelPlayer = player;
        if (travelPlayer == null || !IsValidLevelIndex(fromIndex) || !IsValidLevelIndex(toIndex) ||
            levels[fromIndex] == null || levels[toIndex] == null)
        {
            yield break;
        }

        _travelPlayer = travelPlayer;
        isTeleporting = true;
        GameplayUiModalLock.SetBlockWorldInput(true);

        if (teleportDelay > 0f)
            yield return new WaitForSeconds(teleportDelay);

        if (_travelPlayer == null || elevatorSection == null)
        {
            AbortTravel();
            yield break;
        }

        Transform shaftTarget = elevatorSection.transform;
        float startY = levels[fromIndex].position.y;
        float endY = levels[toIndex].position.y;
        float shaftX = shaftTarget.position.x;

        SetPlayerHidden(true);
        SetElevatorRoomVisible(false);

        shaftTarget.position = new Vector3(shaftX, startY, shaftTarget.position.z);
        BeginCameraTravelFollow();

        float speed = Mathf.Max(0.5f, cabinTravelSpeed > 0f ? cabinTravelSpeed : elevatorSpeed);
        while (Mathf.Abs(shaftTarget.position.y - endY) > 0.05f)
        {
            if (_travelPlayer == null || elevatorSection == null)
            {
                AbortTravel();
                yield break;
            }

            float nextY = Mathf.MoveTowards(shaftTarget.position.y, endY, speed * Time.deltaTime);
            shaftTarget.position = new Vector3(shaftX, nextY, shaftTarget.position.z);
            yield return null;
        }

        RepositionCabina(toIndex);

        float landingPlayerX = _travelPlayer.position.x;
        Vector2 landingAnchor = GetExitAnchorWorldPosition(toIndex, landingPlayerX);
        PerspectiveWalkArea2D landingArea = ResolveFloorLobbyWalkArea(toIndex, landingAnchor);
        Vector2 landingPosition = ResolveElevatorCabinArrivalLanding(toIndex, landingArea);

        player = _travelPlayer;
        EnsurePlayerVisualCache();
        if (_playerRigidbody != null)
            _playerRigidbody.simulated = true;

        var perspectiveMover = _travelPlayer.GetComponent<PlayerPerspectiveMover2D>();
        if (perspectiveMover != null)
        {
            perspectiveMover.TeleportToWorld(landingPosition, pickAreaByPoint: landingArea == null, landingArea);
            perspectiveMover.ReprojectUVFromCurrentPosition();
        }
        else
        {
            _travelPlayer.position = landingPosition;
            var rb2d = _travelPlayer.GetComponent<Rigidbody2D>();
            if (rb2d != null)
            {
                rb2d.position = landingPosition;
                rb2d.velocity = Vector2.zero;
            }
        }

        _travelPlayer.position = new Vector3(landingPosition.x, landingPosition.y, _travelPlayer.position.z);

        ForceShowPlayer();
        SetElevatorRoomVisible(true);
        EnterArrivalCabinState(toIndex);
        RestoreCameraFollow();

        yield return null;

        if (_playerRigidbody != null)
            _playerRigidbody.simulated = true;

        GameplayUiModalLock.SetBlockWorldInput(false);
        _cabinInputBlocked = false;

        isTeleporting = false;
        _teleportCoroutine = null;
        _travelPlayer = null;
    }

    private void AbortTravel()
    {
        RestoreCameraFollow();
        ForceShowPlayer();
        SetElevatorRoomVisible(true);
        if (_travelPlayer != null)
            player = _travelPlayer;
        isTeleporting = false;
        _teleportCoroutine = null;
        _travelPlayer = null;
        _suppressCabinActivationUntilExitFloor = -1;
        GameplayUiModalLock.SetBlockWorldInput(false);
    }

    private Vector2 GetExitAnchorWorldPosition(int floorIndex, float fallbackPlayerX)
    {
        if (exitAnchors != null && floorIndex >= 0 && floorIndex < exitAnchors.Length &&
            exitAnchors[floorIndex] != null)
            return exitAnchors[floorIndex].position;

        ElevatorCabinZone zone = FindCabinZone(floorIndex);
        if (zone != null)
        {
            Collider2D zoneCol = zone.GetComponent<Collider2D>();
            if (zoneCol != null)
            {
                Bounds b = zoneCol.bounds;
                float shallowY = b.min.y + b.size.y * 0.15f;
                return new Vector2(b.center.x, shallowY);
            }
        }

        if (levels == null || !IsValidLevelIndex(floorIndex) || levels[floorIndex] == null)
            return new Vector2(fallbackPlayerX, 0f);

        float startX = fallbackPlayerX;
        float requestedFinalX = useTargetLevelXForTeleport ? levels[floorIndex].position.x : startX;
        float xDelta = Mathf.Abs(requestedFinalX - startX);
        float finalX = (useTargetLevelXForTeleport && xDelta <= Mathf.Max(0.01f, maxTeleportXCorrection))
            ? requestedFinalX
            : startX;

        return new Vector2(finalX, levels[floorIndex].position.y);
    }

    private Vector2 GetCabinInteriorLandingPosition(int floorIndex)
    {
        ElevatorCabinZone zone = FindCabinZone(floorIndex);
        if (zone != null)
        {
            Collider2D zoneCol = zone.GetComponent<Collider2D>();
            if (zoneCol != null)
            {
                Bounds b = zoneCol.bounds;
                float depth = Mathf.Clamp01(zone.CabinaDepthFraction + 0.3f);
                return new Vector2(b.center.x, b.min.y + b.size.y * depth);
            }
        }

        return GetExitAnchorWorldPosition(floorIndex, 0f);
    }

    private Vector2 ResolveCabinInteriorLanding(int toIndex)
    {
        Vector2 interior = GetCabinInteriorLandingPosition(toIndex);
        PerspectiveWalkArea2D area = PlayerPerspectiveMover2D.FindWalkAreaForWorldPoint(interior);
        if (area != null && area.HasValidCorners && area.TryProjectWorldToUV(interior, out Vector2 uv))
            return area.MapToWorld(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y));

        return interior;
    }

    private void EnterArrivalCabinState(int floorIndex)
    {
        CancelCabinArrowSelection(restoreDisplays: false);

        _postTravelArrival = true;
        _postTravelReentryArmed = false;
        _suppressCabinActivationUntilExitFloor = -1;
        _cabinFloorIndex = floorIndex;
        _playerInsideCabinZone = false;
        _cabinZoneOverlapCount = 0;
        _cabinArrowSelectionActive = false;
        _selectionDebounceRemaining = -1f;
        _targetIndex = currentLevelIndex;
        _playerHiddenForCabin = false;
        _playerHideDepth = 0;
        _cabinInputBlocked = false;
        GameplayUiModalLock.SetBlockWorldInput(false);
    }

    private Vector2 ResolveLandingPosition(int toIndex, float playerX)
    {
        Vector2 anchor = GetExitAnchorWorldPosition(toIndex, playerX);
        PerspectiveWalkArea2D area = ResolveFloorLobbyWalkArea(toIndex, anchor);
        return ResolveElevatorArrivalLanding(toIndex, playerX, area);
    }

    private PerspectiveWalkArea2D ResolveFloorLobbyWalkArea(int floorIndex, Vector2 world)
    {
        if (floorLobbyWalkAreas != null && floorIndex >= 0 && floorIndex < floorLobbyWalkAreas.Length &&
            floorLobbyWalkAreas[floorIndex] != null)
            return floorLobbyWalkAreas[floorIndex];

        if (!IsValidLevelIndex(floorIndex) || levels[floorIndex] == null)
            return PlayerPerspectiveMover2D.FindWalkAreaForWorldPoint(world);

        return PlayerPerspectiveMover2D.FindWalkAreaForWorldPointAtLevel(world, levels[floorIndex].position.y);
    }

    /// <summary>Posizione di uscita cabina: anchor davanti alle porte, UV coerente con la walk area del piano.</summary>
    private Vector2 ResolveElevatorArrivalLanding(int toIndex, float playerX, PerspectiveWalkArea2D area)
    {
        Vector2 anchor = GetExitAnchorWorldPosition(toIndex, playerX);
        if (area != null && area.HasValidCorners && area.TryProjectWorldToUV(anchor, out Vector2 uv))
            return area.MapToWorld(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y));

        return anchor;
    }

    /// <summary>Arrivo viaggio: dentro cabina, ma sulla walk area del piano per mantenere UV/collisioni coerenti.</summary>
    private Vector2 ResolveElevatorCabinArrivalLanding(int toIndex, PerspectiveWalkArea2D area)
    {
        Vector2 interior = GetCabinInteriorLandingPosition(toIndex);
        if (area != null && area.HasValidCorners && area.TryProjectWorldToUV(interior, out Vector2 uv))
            return area.MapToWorld(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y));

        return interior;
    }

    private void BeginCameraTravelFollow()
    {
        if (travelVirtualCamera == null || elevatorSection == null)
            return;

        _savedCameraFollow = travelVirtualCamera.Follow;
        travelVirtualCamera.Follow = elevatorSection.transform;
        travelVirtualCamera.PreviousStateIsValid = false;
    }

    private void RestoreCameraFollow()
    {
        if (travelVirtualCamera == null)
            return;

        Transform restore = _savedCameraFollow != null ? _savedCameraFollow : player;
        if (restore != null)
            travelVirtualCamera.Follow = restore;

        travelVirtualCamera.PreviousStateIsValid = false;
        _savedCameraFollow = null;
    }

    private void EnsurePlayerVisualCache()
    {
        if (_playerVisualCacheReady || player == null)
            return;

        _playerSpriteRenderers = player.GetComponentsInChildren<SpriteRenderer>(true);
        _playerSpriteStates = new bool[_playerSpriteRenderers.Length];
        _playerAnimator = player.GetComponentInChildren<Animator>();
        _playerColliders = player.GetComponentsInChildren<Collider2D>(true);
        _playerColliderStates = new bool[_playerColliders.Length];
        _playerRigidbody = player.GetComponent<Rigidbody2D>();
        _playerVisualCacheReady = true;
    }

    private void InvalidatePlayerVisualCache()
    {
        _playerVisualCacheReady = false;
        _playerSpriteRenderers = null;
        _playerColliders = null;
    }

    private void SetPlayerHidden(bool hidden)
    {
        Transform target = _travelPlayer != null ? _travelPlayer : player;
        if (target == null)
            return;

        if (player != target)
            player = target;

        EnsurePlayerVisualCache();

        if (hidden)
        {
            if (_playerHideDepth > 0)
            {
                _playerHideDepth++;
                return;
            }

            for (int i = 0; i < _playerSpriteRenderers.Length; i++)
            {
                _playerSpriteStates[i] = _playerSpriteRenderers[i].enabled;
                _playerSpriteRenderers[i].enabled = false;
            }

            if (_playerAnimator != null)
            {
                _playerAnimatorState = _playerAnimator.enabled;
                _playerAnimator.enabled = false;
            }

            if (isTeleporting && _playerRigidbody != null && !_playerRigidbodySimulationOverridden)
            {
                _playerRigidbodySimulated = _playerRigidbody.simulated;
                _playerRigidbody.simulated = false;
                _playerRigidbody.velocity = Vector2.zero;
                _playerRigidbodySimulationOverridden = true;
            }

            _playerHideDepth = 1;
        }
        else
        {
            if (_playerHideDepth <= 0)
                return;

            _playerHideDepth--;
            if (_playerHideDepth > 0)
                return;

            for (int i = 0; i < _playerSpriteRenderers.Length; i++)
                _playerSpriteRenderers[i].enabled = _playerSpriteStates[i];

            if (_playerAnimator != null)
                _playerAnimator.enabled = _playerAnimatorState;

            if (_playerRigidbody != null && _playerRigidbodySimulationOverridden)
            {
                _playerRigidbody.simulated = _playerRigidbodySimulated;
                _playerRigidbodySimulationOverridden = false;
            }
        }
    }

    private void ForceShowPlayer()
    {
        CancelPendingCabinHide();

        Transform target = _travelPlayer != null ? _travelPlayer : player;
        if (target == null)
            return;

        if (player != target)
            player = target;

        EnsurePlayerVisualCache();

        for (int i = 0; i < _playerSpriteRenderers.Length; i++)
            _playerSpriteRenderers[i].enabled =
                _playerSpriteStates != null && i < _playerSpriteStates.Length && _playerSpriteStates[i];

        if (_playerAnimator != null)
            _playerAnimator.enabled = _playerAnimatorState;

        if (_playerRigidbody != null && _playerRigidbodySimulationOverridden)
        {
            _playerRigidbody.simulated = _playerRigidbodySimulated;
            _playerRigidbodySimulationOverridden = false;
        }

        _playerHideDepth = 0;
        _playerHiddenForCabin = false;
    }

    private void CancelPendingCabinHide()
    {
        if (_hideAfterCabinDoorsCoroutine == null)
            return;

        StopCoroutine(_hideAfterCabinDoorsCoroutine);
        _hideAfterCabinDoorsCoroutine = null;
    }

    private void SetElevatorRoomVisible(bool visible)
    {
        if (!hideElevatorRoomDuringTravel || elevatorSection == null)
            return;

        if (_elevatorRoomRenderer == null)
            _elevatorRoomRenderer = elevatorSection.GetComponent<SpriteRenderer>();

        if (_elevatorRoomRenderer != null)
            _elevatorRoomRenderer.enabled = visible;
    }
    private bool IsLevelUnlocked(int levelIndex)
    {
        return levelIndex < 3;
    }

    private bool CanTeleportToLevel(int levelIndex)
    {
        if (isTeleporting) return false;
        if (levelIndex < 0 || levelIndex >= levels.Length) return false;
        if (levelIndex == currentLevelIndex) return false;
        if (levels[levelIndex] == null) return false;
        if (player == null) return false;

        return true;
    }

    public bool IsPlayerInside => playerInside;
    public bool IsPlayerInsideCabinZone => _playerInsideCabinZone;
    public int CabinFloorIndex => _cabinFloorIndex;
    public int TargetLevelIndex => _targetIndex;
    public int AvailableLevels => levels != null ? levels.Length : 0;

    // ── Cabina / selezione (Fase 5) ────────────────────────────────────

    public void NotifyPlayerEnteredCabinZone(int floorIndex, Transform playerTransform)
    {
        ElevatorCabinZone zone = FindCabinZone(floorIndex);
        Collider2D zoneCol = zone != null ? zone.GetComponent<Collider2D>() : null;
        HandleCabinZoneContact(floorIndex, playerTransform, zoneCol);
    }

    /// <summary>
    /// Player nel trigger cabina: zona esterna → apre porte; zona profonda → chiude e mostra hint Su/Giù.
    /// </summary>
    public void HandleCabinZoneContact(int floorIndex, Transform playerTransform, Collider2D zoneCol)
    {
        if (!enabled || playerTransform == null || !IsValidLevelIndex(floorIndex))
            return;

        if (floorIndex != currentLevelIndex)
            return;

        float depthFraction = 0.45f;
        ElevatorCabinZone zone = FindCabinZone(floorIndex);
        if (zone != null)
            depthFraction = zone.CabinaDepthFraction;

        bool deepInCabina = IsPlayerDeepInCabina(zoneCol, playerTransform.position, depthFraction);

        if (_postTravelArrival)
        {
            bool sameArrivalFloor = floorIndex == _cabinFloorIndex || floorIndex == currentLevelIndex;
            if (sameArrivalFloor && !deepInCabina)
            {
                _postTravelReentryArmed = true;
            }
            else if (sameArrivalFloor && deepInCabina && _postTravelReentryArmed)
            {
                _postTravelArrival = false;
                _postTravelReentryArmed = false;
                _cabinFloorIndex = -1;
                ResetDisplaysToOwnFloors();
            }
            else if (sameArrivalFloor && deepInCabina)
                return;
            else
                return;
        }

        if (_playerInsideCabinZone && _cabinFloorIndex == floorIndex)
            return;

        if (_suppressCabinActivationUntilExitFloor == floorIndex)
        {
            player = playerTransform;
            if (!deepInCabina)
            {
                ResetDisplaysToOwnFloors();
                OpenDoorsApproach(floorIndex);
            }
            return;
        }

        if (!deepInCabina)
        {
            player = playerTransform;
            if (!_playerInsideCabinZone && !_postTravelArrival && floorIndex == currentLevelIndex)
                ResetDisplaysToOwnFloors();
            OpenDoorsApproach(floorIndex);
            return;
        }

        string blockReason = GetCabinActivationBlockReason(floorIndex, playerTransform);

        if (blockReason != null)
            return;

        ActivateCabinInterior(floorIndex, playerTransform);
    }

    private void ActivateCabinInterior(int floorIndex, Transform playerTransform)
    {
        if (_playerInsideCabinZone && _cabinFloorIndex == floorIndex)
            return;

        player = playerTransform;
        _cabinZoneOverlapCount++;

        if (_cabinZoneOverlapCount > 1)
            return;

        _playerInsideCabinZone = true;
        _cabinFloorIndex = floorIndex;
        _targetIndex = currentLevelIndex;
        _selectionDebounceRemaining = -1f;
        _cabinArrowSelectionActive = false;

        CloseDoors(floorIndex);
        ShowCabinHintOnAllDisplays();
        _playerHiddenForCabin = true;
        if (_hideAfterCabinDoorsCoroutine != null)
            StopCoroutine(_hideAfterCabinDoorsCoroutine);
        _hideAfterCabinDoorsCoroutine = StartCoroutine(HidePlayerAfterCabinDoorsClose(floorIndex));
    }

    private System.Collections.IEnumerator HidePlayerAfterCabinDoorsClose(int floorIndex)
    {
        ElevatorDoorPair doors = GetFloorDoors(floorIndex);
        if (doors != null)
        {
            while (doors.IsAnimating)
                yield return null;
        }

        if (_playerInsideCabinZone && _playerHiddenForCabin && _cabinFloorIndex == floorIndex)
            SetPlayerHidden(true);

        _hideAfterCabinDoorsCoroutine = null;
    }

    private void OpenDoorsApproach(int floorIndex)
    {
        if (!IsValidLevelIndex(floorIndex) || floorIndex != currentLevelIndex || _playerInsideCabinZone)
            return;

        ElevatorDoorPair doors = GetFloorDoors(floorIndex);
        if (doors != null && doors.IsOpen)
            return;

        ElevatorDoorPair pair = GetFloorDoors(floorIndex);
        if (pair != null)
            pair.Open();
    }

    private static bool IsPlayerDeepInCabina(Collider2D zoneCol, Vector3 playerPos, float depthFraction)
    {
        if (zoneCol == null)
            return true;

        Bounds b = zoneCol.bounds;
        float threshold = b.min.y + b.size.y * Mathf.Clamp01(depthFraction);
        return playerPos.y >= threshold;
    }

    private ElevatorCabinZone FindCabinZone(int floorIndex)
    {
        for (int i = 0; i < _cabinZones.Count; i++)
        {
            if (_cabinZones[i] != null && _cabinZones[i].FloorIndex == floorIndex)
                return _cabinZones[i];
        }

        return null;
    }

    public void NotifyPlayerExitedCabinZone(int floorIndex)
    {
        if (!IsValidLevelIndex(floorIndex))
            return;

        if (_suppressCabinActivationUntilExitFloor == floorIndex)
        {
            _suppressCabinActivationUntilExitFloor = -1;
            _playerInsideCabinZone = false;
            _cabinFloorIndex = -1;
            _cabinZoneOverlapCount = 0;
            _postTravelReentryArmed = false;
            CancelCabinArrowSelection(restoreDisplays: true);
            CloseDoors(floorIndex);
            return;
        }

        if (_playerHiddenForCabin || isTeleporting)
            return;

        if (_postTravelArrival)
        {
            if (floorIndex == _cabinFloorIndex || floorIndex == currentLevelIndex)
            {
                CloseDoors(floorIndex);
                _postTravelArrival = false;
                _postTravelReentryArmed = false;
                _cabinFloorIndex = -1;
                ResetDisplaysToOwnFloors();
            }

            return;
        }

        if (!_playerInsideCabinZone)
        {
            CancelCabinArrowSelection(restoreDisplays: true);
            return;
        }

        _cabinZoneOverlapCount = Mathf.Max(0, _cabinZoneOverlapCount - 1);

        if (_cabinZoneOverlapCount > 0)
            return;

        LeaveCabinZone(restoreDisplays: true);
    }

    private bool IsValidLevelIndex(int floorIndex)
    {
        return levels != null && floorIndex >= 0 && floorIndex < levels.Length;
    }

    /// <summary>Cabina al piano e player abbastanza dentro — le porte si chiudono dopo l'ingresso.</summary>
    private bool CanUseCabinSelectionAtFloor(int floorIndex, Transform playerTransform)
    {
        return GetCabinActivationBlockReason(floorIndex, playerTransform) == null;
    }

    /// <summary>null = ok per attivazione cabina profonda.</summary>
    private string GetCabinActivationBlockReason(int floorIndex, Transform playerTransform)
    {
        if (!IsValidLevelIndex(floorIndex))
            return "invalid_index";
        if (floorIndex != currentLevelIndex)
            return "wrong_floor";
        if (playerTransform == null || levels[floorIndex] == null)
            return "null_ref";

        float levelY = levels[floorIndex].position.y;
        float dy = Mathf.Abs(playerTransform.position.y - levelY);
        if (dy > Mathf.Max(0.25f, cabinLevelVerticalTolerance))
            return "dy_tolerance";

        if (GetFloorDoors(floorIndex) == null)
            return "no_doors";

        return null;
    }

    private int LastUnlockedLevelIndex =>
        levels != null && levels.Length > 0 ? Mathf.Min(2, levels.Length - 1) : 0;

    private void BeginOrAdjustCabinSelection(int delta)
    {
        if (!_cabinArrowSelectionActive)
        {
            _cabinArrowSelectionActive = true;
            _targetIndex = currentLevelIndex;
            SetCabinWorldInputBlocked(true);
        }

        AdjustTargetIndex(delta);
    }

    private void CancelCabinArrowSelection(bool restoreDisplays)
    {
        _cabinArrowSelectionActive = false;
        _selectionDebounceRemaining = -1f;
        _targetIndex = currentLevelIndex;

        if (_departCoroutine != null && !isTeleporting)
        {
            StopCoroutine(_departCoroutine);
            _departCoroutine = null;
        }

        if (!isTeleporting)
            SetCabinWorldInputBlocked(false);

        if (restoreDisplays)
            ResetDisplaysToOwnFloors();
    }

    private void LeaveCabinZone(bool restoreDisplays, bool closeDoorsOnExit = true)
    {
        bool wasInside = _playerInsideCabinZone;
        int floor = currentLevelIndex;

        _playerInsideCabinZone = false;
        _cabinFloorIndex = -1;
        _cabinZoneOverlapCount = 0;
        _postTravelReentryArmed = false;
        CancelCabinArrowSelection(restoreDisplays);

        if (wasInside && !isTeleporting && !_postTravelArrival && closeDoorsOnExit)
            CloseDoors(floor);

        if (wasInside && _playerHiddenForCabin)
            ForceShowPlayer();
    }

    private void AdjustTargetIndex(int delta)
    {
        if (levels == null || levels.Length == 0)
            return;

        _targetIndex = Mathf.Clamp(_targetIndex + delta, 0, LastUnlockedLevelIndex);
        _selectionDebounceRemaining = Mathf.Max(0f, selectionDebounceSeconds);
        RefreshSelectionDisplay();
    }

    private void RefreshSelectionDisplay()
    {
        if (!_playerInsideCabinZone || !_cabinArrowSelectionActive)
            return;

        ElevatorDirection direction = _targetIndex != currentLevelIndex
            ? GetDirectionToward(currentLevelIndex, _targetIndex)
            : ElevatorDirection.None;

        UpdateAllFloorDisplays(_targetIndex, direction);
    }

    private void TryDepartToTarget()
    {
        _selectionDebounceRemaining = -1f;

        if (_targetIndex == currentLevelIndex)
        {
            _suppressCabinActivationUntilExitFloor = currentLevelIndex;

            LeaveCabinZone(restoreDisplays: true, closeDoorsOnExit: false);
            OpenDoors(currentLevelIndex, deferCabinCheck: false);
            return;
        }

        if (!IsLevelUnlocked(_targetIndex))
        {
            UpdateAllFloorDisplays(_targetIndex, ElevatorDirection.None);
            if (_outOfServiceCoroutine != null)
                StopCoroutine(_outOfServiceCoroutine);
            _outOfServiceCoroutine = StartCoroutine(OutOfServiceSelectionRoutine());
            return;
        }

        if (_departCoroutine != null)
            StopCoroutine(_departCoroutine);

        int capturedTarget = _targetIndex;
        _departCoroutine = StartCoroutine(DepartToTargetRoutine(capturedTarget));
    }

    /// <summary>
    /// Partenza verso il target selezionato: viaggio camera + player nascosto + teleport all'anchor.
    /// </summary>
    private System.Collections.IEnumerator DepartToTargetRoutine(int targetIndex)
    {
        int fromIndex = currentLevelIndex;
        CloseDoors(fromIndex);
        UpdateAllFloorDisplays(targetIndex, GetDirectionToward(fromIndex, targetIndex));

        ElevatorDoorPair fromDoors = GetFloorDoors(fromIndex);
        if (fromDoors != null)
        {
            while (fromDoors.IsAnimating)
                yield return null;
        }

        yield return TravelToFloorRoutine(fromIndex, targetIndex);

        OpenDoors(targetIndex, deferCabinCheck: false);
        UpdateAllFloorDisplays(targetIndex, ElevatorDirection.None);
        _targetIndex = currentLevelIndex;
        _departCoroutine = null;
    }

    private System.Collections.IEnumerator OutOfServiceSelectionRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        _outOfServiceCoroutine = null;
        _targetIndex = currentLevelIndex;

        if (_playerInsideCabinZone && _cabinArrowSelectionActive)
            RefreshSelectionDisplay();
        else if (_playerInsideCabinZone)
            ShowCabinHintOnAllDisplays();
        else
            ResetDisplaysToOwnFloors();
    }

    private void SetCabinWorldInputBlocked(bool blocked)
    {
        if (_cabinInputBlocked == blocked)
            return;

        _cabinInputBlocked = blocked;
        GameplayUiModalLock.SetBlockWorldInput(blocked);
    }

    private void CancelCabinSelectionState(bool restoreWorldInput)
    {
        _selectionDebounceRemaining = -1f;
        _targetIndex = currentLevelIndex;

        if (_departCoroutine != null && !isTeleporting)
        {
            StopCoroutine(_departCoroutine);
            _departCoroutine = null;
        }

        if (restoreWorldInput)
            LeaveCabinZone(restoreDisplays: false);
    }

    private void OnDisable()
    {
        _suppressCabinActivationUntilExitFloor = -1;
        LeaveCabinZone(restoreDisplays: false);
        if (_cabinInputBlocked)
            SetCabinWorldInputBlocked(false);
    }

    /// <summary>Apre le porte del piano indicato (no-op se indice/riferimento non validi).</summary>
    public void OpenDoors(int floorIndex)
    {
        OpenDoors(floorIndex, deferCabinCheck: true);
    }

    private void OpenDoors(int floorIndex, bool deferCabinCheck)
    {
        ElevatorDoorPair pair = GetFloorDoors(floorIndex);
        if (pair != null) pair.Open();

        if (deferCabinCheck)
        {
            if (_cabinZoneCheckCoroutine != null)
                StopCoroutine(_cabinZoneCheckCoroutine);
            _cabinZoneCheckCoroutine = StartCoroutine(DeferredCabinZoneCheck(floorIndex));
        }
    }

    private Coroutine _cabinZoneCheckCoroutine;

    private System.Collections.IEnumerator DeferredCabinZoneCheck(int floorIndex)
    {
        yield return null;

        if (player == null || !IsValidLevelIndex(floorIndex) || floorIndex != currentLevelIndex ||
            _playerInsideCabinZone)
            yield break;

        for (int i = 0; i < _cabinZones.Count; i++)
        {
            ElevatorCabinZone zone = _cabinZones[i];
            if (zone == null || zone.FloorIndex != floorIndex)
                continue;

            Collider2D zoneCol = zone.GetComponent<Collider2D>();
            if (zoneCol == null || !zoneCol.bounds.Contains(player.position))
                continue;

            HandleCabinZoneContact(floorIndex, player, zoneCol);
            break;
        }

        _cabinZoneCheckCoroutine = null;
    }

    public void RegisterCabinZone(ElevatorCabinZone zone)
    {
        if (zone == null || _cabinZones.Contains(zone)) return;
        _cabinZones.Add(zone);
    }

    public void UnregisterCabinZone(ElevatorCabinZone zone)
    {
        if (zone == null) return;
        _cabinZones.Remove(zone);
    }

    /// <summary>Chiude le porte del piano indicato (no-op se indice/riferimento non validi).</summary>
    public void CloseDoors(int floorIndex)
    {
        ElevatorDoorPair pair = GetFloorDoors(floorIndex);
        if (pair != null) pair.Close();
    }

    /// <summary>Chiude immediatamente tutte le porte bindate (utile per stato iniziale/reset).</summary>
    public void CloseAllDoorsInstant()
    {
        if (floorDoors == null) return;
        for (int i = 0; i < floorDoors.Length; i++)
        {
            if (floorDoors[i] != null) floorDoors[i].SetInstant(false);
        }
    }

    private ElevatorDoorPair GetFloorDoors(int floorIndex)
    {
        if (floorDoors == null) return null;
        if (floorIndex < 0 || floorIndex >= floorDoors.Length) return null;
        return floorDoors[floorIndex];
    }

    // ── Display (Fase 3) ───────────────────────────────────────────────

    /// <summary>Registra un display (auto-registrazione dal componente, niente array manuale).</summary>
    public void RegisterDisplay(ElevatorFloorDisplay display)
    {
        if (display == null || _displays.Contains(display)) return;
        _displays.Add(display);
        // Stato a riposo: ogni display mostra l'etichetta del PROPRIO piano.
        display.SetContent(GetFloorLabel(display.FloorIndex), ElevatorDirection.None);
    }

    /// <summary>Annulla la registrazione di un display.</summary>
    public void UnregisterDisplay(ElevatorFloorDisplay display)
    {
        if (display == null) return;
        _displays.Remove(display);
    }

    /// <summary>Etichetta del piano: usa floorLabels (Inspector) se valida, altrimenti i default di progetto.</summary>
    public string GetFloorLabel(int floorIndex)
    {
        if (floorLabels != null && floorIndex >= 0 && floorIndex < floorLabels.Length
            && !string.IsNullOrEmpty(floorLabels[floorIndex]))
            return floorLabels[floorIndex];

        if (floorIndex >= 0 && floorIndex < DefaultFloorLabels.Length)
            return DefaultFloorLabels[floorIndex];

        return $"Floor {floorIndex}";
    }

    /// <summary>Stato a riposo: ogni display mostra l'etichetta del proprio piano, nessuna freccia.</summary>
    public void ResetDisplaysToOwnFloors()
    {
        for (int i = 0; i < _displays.Count; i++)
        {
            if (_displays[i] != null)
                _displays[i].SetContent(GetFloorLabel(_displays[i].FloorIndex), ElevatorDirection.None);
        }
    }

    /// <summary>Durante chiamata/viaggio: tutti i display mostrano lo stesso contenuto (Fase 4+).</summary>
    public void UpdateAllFloorDisplays(int shownFloorIndex, ElevatorDirection direction)
    {
        UpdateAllFloorDisplays(GetFloorLabel(shownFloorIndex), direction);
    }

    private void UpdateAllFloorDisplays(string label, ElevatorDirection direction)
    {
        for (int i = 0; i < _displays.Count; i++)
        {
            if (_displays[i] != null)
                _displays[i].SetContent(label, direction);
        }
    }

    private void ShowCabinHintOnAllDisplays()
    {
        string hint = string.IsNullOrWhiteSpace(cabinSelectionHint)
            ? "Usa \u2191 \u2193 o W S per scegliere il piano"
            : cabinSelectionHint;
        UpdateAllFloorDisplays(hint, ElevatorDirection.None);
    }

    /// <summary>
    /// Chiamata dell'ascensore dal display di un piano: il player resta fermo;
    /// i display mostrano direzione, la cabina logica si sposta e le porte del piano si aprono.
    /// </summary>
    public void CallToFloor(int floorIndex)
    {
        if (!enabled || !IsValidLevelIndex(floorIndex))
            return;

        // Chiamata dal display esterno: annulla selezione freccia e sblocca input (evita freeze su [E]).
        CancelCabinArrowSelection(restoreDisplays: false);

        if (!IsLevelUnlocked(floorIndex))
        {
            ShowOutOfServiceTemporarily(floorIndex);
            return;
        }

        if (floorIndex == currentLevelIndex)
        {
            OpenDoors(floorIndex);
            if (_playerInsideCabinZone)
                ShowCabinHintOnAllDisplays();
            else
                UpdateAllFloorDisplays(floorIndex, ElevatorDirection.None);
            return;
        }

        if (_callToFloorCoroutine != null)
            StopCoroutine(_callToFloorCoroutine);

        _callToFloorCoroutine = StartCoroutine(CallToFloorRoutine(floorIndex));
    }

    private System.Collections.IEnumerator CallToFloorRoutine(int floorIndex)
    {
        int fromIndex = currentLevelIndex;
        ElevatorDirection direction = GetDirectionToward(fromIndex, floorIndex);

        CloseDoors(fromIndex);
        UpdateAllFloorDisplays(floorIndex, direction);

        float wait = Mathf.Max(0f, callTravelDuration);
        if (wait > 0f)
            yield return new WaitForSeconds(wait);

        RepositionCabina(floorIndex);
        OpenDoors(floorIndex);
        UpdateAllFloorDisplays(floorIndex, ElevatorDirection.None);

        _callToFloorCoroutine = null;
    }

    /// <summary>Riposiziona la cabina logica senza resettare i display (EndDay usa SetLevel).</summary>
    private void RepositionCabina(int levelIndex)
    {
        if (levels == null || levels.Length == 0 || levels[levelIndex] == null || elevatorSection == null)
            return;

        currentLevelIndex = levelIndex;
        elevatorSection.transform.position = new Vector3(
            elevatorSection.transform.position.x,
            levels[levelIndex].position.y,
            elevatorSection.transform.position.z);
    }

    /// <summary>Indice crescente = piano fisicamente più basso (+1 → 0 → -1 → -2).</summary>
    private static ElevatorDirection GetDirectionToward(int fromIndex, int toIndex)
    {
        if (toIndex == fromIndex) return ElevatorDirection.None;
        return toIndex > fromIndex ? ElevatorDirection.Down : ElevatorDirection.Up;
    }

    private void ShowOutOfServiceTemporarily(int floorIndex)
    {
        if (_callToFloorCoroutine != null)
        {
            StopCoroutine(_callToFloorCoroutine);
            _callToFloorCoroutine = null;
        }

        if (_outOfServiceCoroutine != null)
            StopCoroutine(_outOfServiceCoroutine);
        _outOfServiceCoroutine = StartCoroutine(OutOfServiceRoutine(floorIndex));
    }

    private System.Collections.IEnumerator OutOfServiceRoutine(int floorIndex)
    {
        UpdateAllFloorDisplays(floorIndex, ElevatorDirection.None);
        yield return new WaitForSeconds(2f);
        _outOfServiceCoroutine = null;
        ResetDisplaysToOwnFloors();
    }

    // Metodo per aggiungere livelli dinamicamente
    public void AddLevel(Transform newLevel)
    {
        if (newLevel == null) return;

        System.Array.Resize(ref levels, levels.Length + 1);
        levels[levels.Length - 1] = newLevel;
    }

    // Metodo per rimuovere un livello
    public bool RemoveLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levels.Length) return false;

        for (int i = levelIndex; i < levels.Length - 1; i++)
        {
            levels[i] = levels[i + 1];
        }

        System.Array.Resize(ref levels, levels.Length - 1);
        return true;
    }

    /// <summary>
    /// Late binding per UINotification quando viene registrato (fallback toast "out of order").
    /// </summary>
    private void OnUINotificationRegistered(object service)
    {
        if (service is UINotification notification && uiNotification == null)
        {
            uiNotification = notification;
            TryUnsubscribeFromRuntimeServices();
        }
    }

    private void OnDestroy()
    {
        if (_callToFloorCoroutine != null)
            StopCoroutine(_callToFloorCoroutine);
        if (_outOfServiceCoroutine != null)
            StopCoroutine(_outOfServiceCoroutine);
        if (_departCoroutine != null)
            StopCoroutine(_departCoroutine);
        RestoreCameraFollow();
        SetPlayerHidden(false);
        SetElevatorRoomVisible(true);
        if (_cabinInputBlocked)
            GameplayUiModalLock.SetBlockWorldInput(false);
        UnsubscribeFromRuntimeServices();
    }

    private void ResolveRuntimeDependencies()
    {
        uiNotification = uiNotification ?? ServiceContainer.Instance?.Get<UINotification>(suppressWarning: true);
    }

    private void SubscribeToRuntimeServicesIfNeeded()
    {
        if (ServiceContainer.Instance == null)
            return;

        if (uiNotification != null || _waitingForRuntimeServices)
            return;

        ServiceContainer.Instance.OnServiceRegistered += OnUINotificationRegistered;
        _waitingForRuntimeServices = true;
    }

    private void TryUnsubscribeFromRuntimeServices()
    {
        if (uiNotification != null)
            UnsubscribeFromRuntimeServices();
    }

    private void UnsubscribeFromRuntimeServices()
    {
        if (!_waitingForRuntimeServices || ServiceContainer.Instance == null)
            return;

        ServiceContainer.Instance.OnServiceRegistered -= OnUINotificationRegistered;
        _waitingForRuntimeServices = false;
    }

    // Gizmos per debug
    void OnDrawGizmosSelected()
    {
        if (levels == null) return;

        Gizmos.color = Color.blue;
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] != null)
            {
                Gizmos.DrawWireSphere(levels[i].position, 0.5f);
                Gizmos.DrawLine(transform.position, levels[i].position);

                // Label del livello
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(levels[i].position + Vector3.up * 0.7f, $"Level {i}");
                #endif
            }
        }
    }
}
