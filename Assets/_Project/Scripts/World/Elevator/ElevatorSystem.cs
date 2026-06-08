using System;
using System.Collections.Generic;
using _Project;
using _Project.Player;
using _Project.Sporae.Core;
using _Project.World.VaultMap;
using Cinemachine;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.HUD;
using UnityEngine;

/// <summary>Direzione mostrata sui display dell'ascensore.</summary>
public enum ElevatorDirection
{
    None,
    Up,
    Down
}

/// <summary>Modalità visual del pannello ascensore compatto.</summary>
public enum ElevatorDisplayMode
{
    Normal,
    CallRemote,
    Enter,
    CabinAtFloor,
    CabinSelectingTarget,
    OutOfService
}

/// <summary>Stato esplicito del flusso ascensore (Elevator 4.0).</summary>
public enum ElevatorFlowState
{
    IdleAtFloor,
    CallingToFloor,
    DoorsOpenWaitingEntry,
    CabinReadyForSelection,
    Departing,
    Traveling,
    ArrivalWaitingExit
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
    [Tooltip("Tolleranza verticale (unità mondo) tra player e livello per attivare la selezione in cabina.")]
    [SerializeField] private float cabinLevelVerticalTolerance = 1.75f;

    [Tooltip("Testo sui display quando il player entra in cabina (prima di premere Su/Giù o W/S).")]
    [SerializeField] private string cabinSelectionHint = "Usa \u2191 \u2193 o W S per scegliere il piano";

    [Tooltip("Hint bottom bar dopo la selezione piano: conferma viaggio con E.")]
    [SerializeField] private string cabinConfirmHint = "Premi E per confermare il piano";

    [Tooltip("Soglia UV v sul pianerottolo (solo piani senza ElevatorCabinInteriorZone). Con interior zone fisica, l'ingresso è deciso dal trigger.")]
    [SerializeField] [Range(0.5f, 1f)] private float cabinLobbyDeepV = 0.92f;

    [Tooltip("Grace solo in soglia: se il player è già abbastanza deep, la cabina può attivarsi subito. Altrimenti attesa minima dopo apertura porte.")]
    [SerializeField] private float minDoorsOpenBeforeCabinEntrySeconds = 0.45f;

    [Tooltip("Ritardo dopo attivazione cabina prima di richiudere le porte.")]
    [SerializeField] private float cabinDoorCloseDelaySeconds = 0.75f;

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
    private readonly List<ElevatorCabinInteriorZone> _interiorZones = new List<ElevatorCabinInteriorZone>();
    private ElevatorFlowState _flowState = ElevatorFlowState.IdleAtFloor;
    private int _interiorZoneOverlapCount;
    private Coroutine _outOfServiceCoroutine;
    private Coroutine _callToFloorCoroutine;
    private Coroutine _departCoroutine;
    private Coroutine _hideAfterCabinDoorsCoroutine;
    private bool _holdDoorsOpenForCabinEntry;
    private Coroutine _closeDoorsForCabinCoroutine;
    // Ingresso cabina armato SOLO quando le porte si aprono da una chiamata (player provabilmente fuori).
    private bool _entryArmedByDoorsOpen;
    private float _doorsFullyOpenAtTime = -1f;

    // Fase 5 — selezione piano in cabina
    private bool _playerInsideCabinZone;
    private bool _cabinArrowSelectionActive;
    private int _cabinFloorIndex = -1;
    private int _cabinZoneOverlapCount;
    private int _targetIndex;
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
        "Floor 0 \u00B7 Serra + Laboratorio",
        "Floor -1 \u00B7 Dormitorio - Cucina",
        "Floor -2 \u00B7 Out of Service",
    };

    void Start()
    {
        ValidateConfiguration();
        BindElevatorSceneComponents();

        ResolveRuntimeDependencies();
        SubscribeToRuntimeServicesIfNeeded();

        currentLevelIndex = startingLevelIndex;
        _targetIndex = startingLevelIndex;

        ResetDisplaysToOwnFloors();
    }

    /// <summary>
    /// Late binding controllato sui figli di ELEV_Elevator (niente scan globale scena).
    /// </summary>
    private void BindElevatorSceneComponents()
    {
        Transform root = transform.parent != null ? transform.parent : transform;

        var displays = root.GetComponentsInChildren<ElevatorFloorDisplay>(true);
        for (int i = 0; i < displays.Length; i++)
        {
            if (displays[i] != null)
                displays[i].BindElevator(this);
        }

        var zones = root.GetComponentsInChildren<ElevatorCabinZone>(true);
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i] != null)
                zones[i].BindElevator(this);
        }

        var interiorZones = root.GetComponentsInChildren<ElevatorCabinInteriorZone>(true);
        for (int i = 0; i < interiorZones.Length; i++)
        {
            if (interiorZones[i] != null)
                interiorZones[i].BindElevator(this);
        }
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
        UpdateCabinSelectionInput();
        RefreshIdleDisplayIfNeeded();
    }

    private void UpdateCabinSelectionInput()
    {
        if (!_playerInsideCabinZone || _postTravelArrival || isTeleporting || levels == null || levels.Length == 0)
            return;

        // Ingresso visivo: finché i portelloni sono aperti, W/S muovono il player (non selezionano piano).
        if (AreDoorsOpenAtFloor(currentLevelIndex))
            return;

        int cabinDelta = 0;
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            cabinDelta = -1;
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            cabinDelta = 1;

        if (cabinDelta != 0)
            BeginOrAdjustCabinSelection(cabinDelta);

        if (_cabinArrowSelectionActive && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)))
            TryDepartToTarget();
    }

    private void RefreshIdleDisplayIfNeeded()
    {
        if (IsDisplaySequenceActive())
            return;

        PushDisplayState(currentLevelIndex, ElevatorDirection.None, keepCabinHint: false);
    }

    private bool IsDisplaySequenceActive()
    {
        return _playerInsideCabinZone || _cabinArrowSelectionActive || _postTravelArrival
            || _callToFloorCoroutine != null || _departCoroutine != null
            || isTeleporting || _outOfServiceCoroutine != null;
    }

    private int ResolvePlayerLevelIndex()
    {
        Transform playerTransform = player;
        if (playerTransform == null)
        {
            var mover = UnityEngine.Object.FindFirstObjectByType<PlayerPerspectiveMover2D>(FindObjectsInactive.Exclude);
            playerTransform = mover != null ? mover.transform : null;
        }

        if (playerTransform == null || levels == null || levels.Length == 0)
            return -1;

        float playerY = playerTransform.position.y;
        int bestIndex = currentLevelIndex;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] == null)
                continue;

            float distance = Mathf.Abs(levels[i].position.y - playerY);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private ElevatorDisplayMode ResolveDisplayMode(int highlightFloorIndex, ElevatorDirection direction)
    {
        if (_outOfServiceCoroutine != null && highlightFloorIndex == 3)
            return ElevatorDisplayMode.OutOfService;

        if (direction != ElevatorDirection.None)
            return ElevatorDisplayMode.Normal;

        if (_playerInsideCabinZone && !_cabinArrowSelectionActive)
            return ElevatorDisplayMode.CabinAtFloor;

        if (_cabinArrowSelectionActive && _flowState == ElevatorFlowState.CabinReadyForSelection)
            return ElevatorDisplayMode.CabinSelectingTarget;

        int playerFloor = ResolvePlayerLevelIndex();
        if (playerFloor >= 0 && playerFloor != currentLevelIndex)
            return ElevatorDisplayMode.CallRemote;

        if (playerFloor >= 0 && playerFloor == currentLevelIndex && AreDoorsOpenAtFloor(playerFloor))
            return ElevatorDisplayMode.Enter;

        return ElevatorDisplayMode.Normal;
    }

    private bool AreDoorsOpenAtFloor(int floorIndex)
    {
        if (!IsValidLevelIndex(floorIndex))
            return false;

        ElevatorDoorPair doors = GetFloorDoors(floorIndex);
        return doors != null && doors.IsOpen;
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

        CloseAllDoorsInstant();
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
        SetFlowState(ElevatorFlowState.Traveling);
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
        ElevatorCabinZone landingZone = FindCabinZone(toIndex);
        Vector2 landingAnchor = landingZone != null
            ? landingZone.GetInteriorLandingWorldPosition()
            : GetExitAnchorWorldPosition(toIndex, landingPlayerX);
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
            return zone.GetExitApproachWorldPosition();

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
        ElevatorCabinInteriorZone interiorZone = FindInteriorZone(floorIndex);
        if (interiorZone != null)
        {
            if (interiorZone.LandingPoint != null)
                return interiorZone.LandingPoint.position;

            Collider2D interiorCol = interiorZone.GetComponent<Collider2D>();
            if (interiorCol != null)
                return interiorCol.bounds.center;
        }

        ElevatorCabinZone zone = FindCabinZone(floorIndex);
        if (zone != null)
            return zone.GetInteriorLandingWorldPosition();

        return GetExitAnchorWorldPosition(floorIndex, 0f);
    }

    private Vector2 ResolveCabinInteriorLanding(int toIndex)
    {
        Vector2 interior = GetCabinInteriorLandingPosition(toIndex);
        PerspectiveWalkArea2D area = ResolveFloorLobbyWalkArea(toIndex, interior);
        if (area != null && area.HasValidCorners && area.TryProjectWorldToUV(interior, out Vector2 uv))
            return area.MapToWorld(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y));

        return interior;
    }

    private void EnterArrivalCabinState(int floorIndex)
    {
        CancelCabinArrowSelection(restoreDisplays: false);

        _postTravelArrival = true;
        SetFlowState(ElevatorFlowState.ArrivalWaitingExit);
        _postTravelReentryArmed = false;
        _suppressCabinActivationUntilExitFloor = -1;
        _cabinFloorIndex = floorIndex;
        _playerInsideCabinZone = false;
        _cabinZoneOverlapCount = 0;
        _cabinArrowSelectionActive = false;
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

        ElevatorCabinZone zone = FindCabinZone(floorIndex);
        if (zone != null)
        {
            PerspectiveWalkArea2D zoneArea = zone.ResolveWalkArea();
            if (zoneArea != null)
                return zoneArea;
        }

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

        ElevatorCabinZone zone = FindCabinZone(floorIndex);
        if (HasPhysicalInteriorZone(floorIndex))
            return;

        bool deepInCabina = zone != null
            ? zone.IsPlayerDeepInCabina(playerTransform.position, zoneCol)
            : IsPlayerDeepInCabinaLegacy(zoneCol, playerTransform.position, 0.45f);

        if (_postTravelArrival)
        {
            bool sameArrivalFloor = floorIndex == _cabinFloorIndex || floorIndex == currentLevelIndex;
            if (!sameArrivalFloor)
                return;

            player = playerTransform;

            if (!deepInCabina)
            {
                CloseDoors(floorIndex, force: true);
                _postTravelArrival = false;
                _postTravelReentryArmed = false;
                _cabinFloorIndex = -1;
                _suppressCabinActivationUntilExitFloor = floorIndex;
                ResetDisplaysToOwnFloors();
            }

            return;
        }

        if (_playerInsideCabinZone && _cabinFloorIndex == floorIndex)
        {
            if (!deepInCabina)
            {
                if (_closeDoorsForCabinCoroutine != null || _hideAfterCabinDoorsCoroutine != null || _playerHiddenForCabin)
                    return;

                LeaveCabinZone(restoreDisplays: true, closeDoorsOnExit: true);
            }

            return;
        }

        if (_suppressCabinActivationUntilExitFloor == floorIndex)
        {
            player = playerTransform;
            if (!deepInCabina)
                ResetDisplaysToOwnFloors();
            return;
        }

        if (!deepInCabina)
        {
            // Modello "call": niente apertura per prossimità. Le porte si aprono solo da CallToFloor.
            player = playerTransform;
            if (!_playerInsideCabinZone && !_postTravelArrival && floorIndex == currentLevelIndex)
                ResetDisplaysToOwnFloors();
            return;
        }

        string blockReason = GetCabinActivationBlockReason(floorIndex, playerTransform);
        if (blockReason != null)
            return;

        ActivateCabinInterior(floorIndex, playerTransform);
    }

    public bool HasPhysicalInteriorZone(int floorIndex) => FindInteriorZone(floorIndex) != null;

    public bool IsCabAtFloor(int floorIndex) =>
        IsValidLevelIndex(floorIndex) && floorIndex == currentLevelIndex;

    public bool IsPlayerInsideCabinOnFloor(int floorIndex) =>
        _playerInsideCabinZone && _cabinFloorIndex == floorIndex;

    public void RegisterInteriorZone(ElevatorCabinInteriorZone zone)
    {
        if (zone == null || _interiorZones.Contains(zone))
            return;

        _interiorZones.Add(zone);
        DisarmLegacyCabinZoneAtFloor(zone.FloorIndex);
    }

    private void DisarmLegacyCabinZoneAtFloor(int floorIndex)
    {
        for (int i = 0; i < _cabinZones.Count; i++)
        {
            ElevatorCabinZone legacy = _cabinZones[i];
            if (legacy == null || legacy.FloorIndex != floorIndex)
                continue;

            Collider2D col = legacy.GetComponent<Collider2D>();
            if (col != null)
                col.enabled = false;

            legacy.enabled = false;
            return;
        }
    }

    public void UnregisterInteriorZone(ElevatorCabinInteriorZone zone)
    {
        if (zone == null)
            return;

        _interiorZones.Remove(zone);
    }

    public void NotifyInteriorZoneEnter(int floorIndex, Transform playerTransform)
    {
        if (!enabled || playerTransform == null || !IsValidLevelIndex(floorIndex))
            return;

        if (!IsCabAtFloor(floorIndex))
            return;

        _interiorZoneOverlapCount++;
        if (_interiorZoneOverlapCount > 1)
            return;

        HandlePhysicalInteriorEnter(floorIndex, playerTransform);
    }

    public void NotifyInteriorZoneStay(int floorIndex, Transform playerTransform)
    {
        if (!enabled || playerTransform == null || !IsValidLevelIndex(floorIndex) || !IsCabAtFloor(floorIndex))
            return;

        HandlePhysicalInteriorEnter(floorIndex, playerTransform);
    }

    public void NotifyInteriorZoneExit(int floorIndex)
    {
        if (!IsValidLevelIndex(floorIndex))
            return;

        _interiorZoneOverlapCount = Mathf.Max(0, _interiorZoneOverlapCount - 1);
        if (_interiorZoneOverlapCount > 0)
            return;

        HandlePhysicalInteriorExit(floorIndex);
    }

    private void HandlePhysicalInteriorEnter(int floorIndex, Transform playerTransform)
    {
        if (floorIndex != currentLevelIndex)
            return;

        if (_postTravelArrival)
        {
            bool sameArrivalFloor = floorIndex == _cabinFloorIndex || floorIndex == currentLevelIndex;
            if (!sameArrivalFloor)
                return;

            player = playerTransform;
            return;
        }

        if (_playerInsideCabinZone && _cabinFloorIndex == floorIndex)
            return;

        if (_suppressCabinActivationUntilExitFloor == floorIndex)
        {
            player = playerTransform;
            ResetDisplaysToOwnFloors();
            return;
        }

        if (GetCabinActivationBlockReason(floorIndex, playerTransform) != null)
            return;

        ActivateCabinInterior(floorIndex, playerTransform);
    }

    private void HandlePhysicalInteriorExit(int floorIndex)
    {
        if (_suppressCabinActivationUntilExitFloor == floorIndex)
        {
            _suppressCabinActivationUntilExitFloor = -1;
            _playerInsideCabinZone = false;
            _cabinFloorIndex = -1;
            _cabinZoneOverlapCount = 0;
            _postTravelReentryArmed = false;
            CancelCabinArrowSelection(restoreDisplays: true);
            _holdDoorsOpenForCabinEntry = false;
            _entryArmedByDoorsOpen = false;
            CloseDoors(floorIndex, force: true);
            if (_playerHiddenForCabin)
                ForceShowPlayer();
            SetFlowState(ElevatorFlowState.IdleAtFloor);
            return;
        }

        if (isTeleporting)
            return;

        if (_postTravelArrival)
        {
            if (floorIndex == _cabinFloorIndex || floorIndex == currentLevelIndex)
            {
                CloseDoors(floorIndex, force: true);
                _postTravelArrival = false;
                _postTravelReentryArmed = false;
                _cabinFloorIndex = -1;
                ResetDisplaysToOwnFloors();
                SetFlowState(ElevatorFlowState.IdleAtFloor);
            }

            if (_playerHiddenForCabin)
                ForceShowPlayer();

            return;
        }

        if (!_playerInsideCabinZone)
        {
            if (floorIndex != currentLevelIndex)
                return;

            _entryArmedByDoorsOpen = false;

            if (AreDoorsOpenAtFloor(floorIndex) && !isTeleporting)
            {
                if (_holdDoorsOpenForCabinEntry)
                    _holdDoorsOpenForCabinEntry = false;

                CloseDoors(floorIndex);
            }

            if (_playerHiddenForCabin)
                ForceShowPlayer();

            CancelCabinArrowSelection(restoreDisplays: true);
            SetFlowState(ElevatorFlowState.IdleAtFloor);
            return;
        }

        LeaveCabinZone(restoreDisplays: true);
    }

    private ElevatorCabinInteriorZone FindInteriorZone(int floorIndex)
    {
        for (int i = 0; i < _interiorZones.Count; i++)
        {
            if (_interiorZones[i] != null && _interiorZones[i].FloorIndex == floorIndex)
                return _interiorZones[i];
        }

        return null;
    }

    private void SetFlowState(ElevatorFlowState state)
    {
        _flowState = state;
    }

    private void ActivateCabinInterior(int floorIndex, Transform playerTransform)
    {
        if (_playerInsideCabinZone && _cabinFloorIndex == floorIndex)
            return;

        if (_closeDoorsForCabinCoroutine != null || _hideAfterCabinDoorsCoroutine != null)
            return;

        player = playerTransform;
        _cabinZoneOverlapCount++;

        if (_cabinZoneOverlapCount > 1)
            return;

        _playerInsideCabinZone = true;
        _cabinFloorIndex = floorIndex;
        _targetIndex = currentLevelIndex;
        _cabinArrowSelectionActive = false;
        _entryArmedByDoorsOpen = false;
        SetFlowState(ElevatorFlowState.CabinReadyForSelection);

        ShowCabinHintOnBottomBar();
        _playerHiddenForCabin = true;
        if (_closeDoorsForCabinCoroutine != null)
            StopCoroutine(_closeDoorsForCabinCoroutine);
        _closeDoorsForCabinCoroutine = StartCoroutine(CloseDoorsForCabinEntry(floorIndex));
        if (_hideAfterCabinDoorsCoroutine != null)
            StopCoroutine(_hideAfterCabinDoorsCoroutine);
        _hideAfterCabinDoorsCoroutine = StartCoroutine(HidePlayerAfterCabinDoorsClose(floorIndex));
    }

    private System.Collections.IEnumerator HidePlayerAfterCabinDoorsClose(int floorIndex)
    {
        ElevatorDoorPair doors = GetFloorDoors(floorIndex);
        if (doors != null)
        {
            // Attende fine apertura (se in corso) e poi chiusura completa prima di nascondere il player.
            while (doors.IsAnimating || doors.IsOpen)
                yield return null;
        }

        if (_playerInsideCabinZone && _playerHiddenForCabin && _cabinFloorIndex == floorIndex)
        {
            SetCabinWorldInputBlocked(true);
            EnsurePlayerVisualCache();
            if (_playerRigidbody != null)
                _playerRigidbody.velocity = Vector2.zero;
            SetPlayerHidden(true);
        }

        _hideAfterCabinDoorsCoroutine = null;
    }

    private System.Collections.IEnumerator CloseDoorsForCabinEntry(int floorIndex)
    {
        ElevatorDoorPair doors = GetFloorDoors(floorIndex);
        if (doors != null)
        {
            while (doors.IsAnimating)
                yield return null;
        }

        if (cabinDoorCloseDelaySeconds > 0f)
            yield return new WaitForSeconds(cabinDoorCloseDelaySeconds);

        _holdDoorsOpenForCabinEntry = false;
        _doorsFullyOpenAtTime = -1f;
        CloseDoors(floorIndex, force: true, bypassHold: true);
        _closeDoorsForCabinCoroutine = null;
    }

    private static bool IsPlayerDeepInCabinaLegacy(Collider2D zoneCol, Vector3 playerPos, float depthFraction)
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

        if (HasPhysicalInteriorZone(floorIndex))
            return;

        if (_suppressCabinActivationUntilExitFloor == floorIndex)
        {
            _suppressCabinActivationUntilExitFloor = -1;
            _playerInsideCabinZone = false;
            _cabinFloorIndex = -1;
            _cabinZoneOverlapCount = 0;
            _postTravelReentryArmed = false;
            CancelCabinArrowSelection(restoreDisplays: true);
            _holdDoorsOpenForCabinEntry = false;
            _entryArmedByDoorsOpen = false;
            CloseDoors(floorIndex, force: true);
            if (_playerHiddenForCabin)
                ForceShowPlayer();
            return;
        }

        if (isTeleporting)
            return;

        if (_postTravelArrival)
        {
            if (floorIndex == _cabinFloorIndex || floorIndex == currentLevelIndex)
            {
                CloseDoors(floorIndex, force: true);
                _postTravelArrival = false;
                _postTravelReentryArmed = false;
                _cabinFloorIndex = -1;
                ResetDisplaysToOwnFloors();
            }

            if (_playerHiddenForCabin)
                ForceShowPlayer();

            return;
        }

        if (!_playerInsideCabinZone)
        {
            _entryArmedByDoorsOpen = false;

            if (floorIndex == currentLevelIndex && AreDoorsOpenAtFloor(floorIndex) && !isTeleporting)
            {
                if (_holdDoorsOpenForCabinEntry)
                    _holdDoorsOpenForCabinEntry = false;

                CloseDoors(floorIndex);
            }

            if (_playerHiddenForCabin)
                ForceShowPlayer();
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

    /// <summary>null = ok per attivazione cabina profonda.</summary>
    private string GetCabinActivationBlockReason(int floorIndex, Transform playerTransform)
    {
        if (!IsValidLevelIndex(floorIndex))
            return "invalid_index";
        if (floorIndex != currentLevelIndex)
            return "wrong_floor";
        if (playerTransform == null || levels[floorIndex] == null)
            return "null_ref";

        if (GetFloorDoors(floorIndex) == null)
            return "no_doors";

        ElevatorDoorPair doors = GetFloorDoors(floorIndex);
        if (doors.IsClosing)
            return "doors_closing";
        if (!doors.IsOpen && !doors.IsAnimating)
            return "doors_closed";
        // Issue 1: non attivare (e quindi non chiudere) finché l'apertura non è completata.
        if (doors.IsOpening)
            return "doors_opening";

        if (!_entryArmedByDoorsOpen && !_postTravelArrival && !_postTravelReentryArmed)
            return "not_called";

        SyncDoorsFullyOpenTimestamp(doors);
        if (_entryArmedByDoorsOpen && _doorsFullyOpenAtTime >= 0f
            && Time.time < _doorsFullyOpenAtTime + minDoorsOpenBeforeCabinEntrySeconds)
        {
            if (HasPhysicalInteriorZone(floorIndex))
                return "doors_open_grace";

            if (!IsPlayerDeepEnoughOnLobbyWalkArea(floorIndex, playerTransform.position))
                return "doors_open_grace";
        }

        return null;
    }

    private void SyncDoorsFullyOpenTimestamp(ElevatorDoorPair doors)
    {
        if (doors != null && doors.IsOpen && !doors.IsAnimating)
        {
            if (_doorsFullyOpenAtTime < 0f)
                _doorsFullyOpenAtTime = Time.time;
        }
        else
            _doorsFullyOpenAtTime = -1f;
    }

    private bool IsPlayerDeepEnoughOnLobbyWalkArea(int floorIndex, Vector3 playerPos)
    {
        PerspectiveWalkArea2D lobby = ResolveFloorLobbyWalkArea(floorIndex, playerPos);
        if (lobby != null && lobby.HasValidCorners && lobby.TryProjectWorldToUV(playerPos, out Vector2 uv))
            return uv.y >= cabinLobbyDeepV;

        ElevatorCabinZone zone = FindCabinZone(floorIndex);
        Collider2D zoneCol = zone != null ? zone.GetComponent<Collider2D>() : null;
        return zone != null
            ? zone.IsPlayerDeepInCabina(playerPos, zoneCol)
            : IsPlayerDeepInCabinaLegacy(zoneCol, playerPos, 0.45f);
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
            ClearCabinHintOnBottomBar();
        }

        AdjustTargetIndex(delta);
    }

    private void CancelCabinArrowSelection(bool restoreDisplays)
    {
        _cabinArrowSelectionActive = false;
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
        _entryArmedByDoorsOpen = false;
        CancelCabinArrowSelection(restoreDisplays);

        if (wasInside && !isTeleporting && !_postTravelArrival && closeDoorsOnExit)
        {
            _holdDoorsOpenForCabinEntry = false;
            CloseDoors(floor, force: true);
        }

        if (wasInside && _playerHiddenForCabin)
            ForceShowPlayer();

        if (wasInside)
            SetFlowState(ElevatorFlowState.IdleAtFloor);
    }

    private void AdjustTargetIndex(int delta)
    {
        if (levels == null || levels.Length == 0)
            return;

        _targetIndex = Mathf.Clamp(_targetIndex + delta, 0, LastUnlockedLevelIndex);
        SetFlowState(ElevatorFlowState.CabinReadyForSelection);
        RefreshSelectionDisplay();
        ShowCabinConfirmHintOnBottomBar();
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
        SetFlowState(ElevatorFlowState.Departing);
        _departCoroutine = StartCoroutine(DepartToTargetRoutine(capturedTarget));
    }

    /// <summary>
    /// Partenza verso il target selezionato: viaggio camera + player nascosto + teleport all'anchor.
    /// </summary>
    private System.Collections.IEnumerator DepartToTargetRoutine(int targetIndex)
    {
        int fromIndex = currentLevelIndex;
        _entryArmedByDoorsOpen = false;
        CloseDoors(fromIndex, force: true, bypassHold: true);
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
            ShowCabinHintOnBottomBar();
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
        if (floorIndex == currentLevelIndex && !_playerInsideCabinZone)
        {
            CancelPendingCabinHide();
            _playerHiddenForCabin = false;
            _entryArmedByDoorsOpen = false;
            _doorsFullyOpenAtTime = -1f;
            if (_closeDoorsForCabinCoroutine != null)
            {
                StopCoroutine(_closeDoorsForCabinCoroutine);
                _closeDoorsForCabinCoroutine = null;
            }
        }

        ElevatorDoorPair pair = GetFloorDoors(floorIndex);
        if (pair != null) pair.Open();

        if (floorIndex == currentLevelIndex && !isTeleporting)
            _holdDoorsOpenForCabinEntry = true;

        if (deferCabinCheck && !HasPhysicalInteriorZone(floorIndex))
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

        if (HasPhysicalInteriorZone(floorIndex)
            || player == null || !IsValidLevelIndex(floorIndex) || floorIndex != currentLevelIndex
            || _playerInsideCabinZone)
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
    public void CloseDoors(int floorIndex, bool force = false, bool bypassHold = false)
    {
        if (!bypassHold && _holdDoorsOpenForCabinEntry && floorIndex == currentLevelIndex)
            return;

        if (bypassHold)
            _holdDoorsOpenForCabinEntry = false;

        ElevatorDoorPair pair = GetFloorDoors(floorIndex);
        if (pair != null) pair.Close();
    }

    /// <summary>Chiude immediatamente tutte le porte bindate (utile per stato iniziale/reset).</summary>
    public void CloseAllDoorsInstant()
    {
        _holdDoorsOpenForCabinEntry = false;
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
        PushDisplayState(currentLevelIndex, ElevatorDirection.None, keepCabinHint: _playerInsideCabinZone && !_cabinArrowSelectionActive);
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

    /// <summary>Stato a riposo: tutti i display evidenziano il piano della cabina.</summary>
    public void ResetDisplaysToOwnFloors()
    {
        ClearCabinHintOnBottomBar();
        PushDisplayState(currentLevelIndex, ElevatorDirection.None, keepCabinHint: false);
    }

    /// <summary>Durante chiamata/viaggio/selezione: tutti i display mostrano lo stesso pannello sincronizzato.</summary>
    public void UpdateAllFloorDisplays(int highlightFloorIndex, ElevatorDirection direction)
    {
        PushDisplayState(highlightFloorIndex, direction, keepCabinHint: false);
    }

    private void PushDisplayState(int highlightFloorIndex, ElevatorDirection direction, bool keepCabinHint)
    {
        if (!keepCabinHint)
            ClearCabinHintOnBottomBar();

        ElevatorDisplayMode mode = ResolveDisplayMode(highlightFloorIndex, direction);
        string[] floorLabels = BuildFloorLabelsSnapshot();
        for (int i = 0; i < _displays.Count; i++)
        {
            if (_displays[i] != null)
                _displays[i].SetPanelState(highlightFloorIndex, direction, floorLabels, mode);
        }
    }

    private string[] BuildFloorLabelsSnapshot()
    {
        int count = levels != null && levels.Length > 0 ? levels.Length : DefaultFloorLabels.Length;
        var labels = new string[count];
        for (int i = 0; i < count; i++)
            labels[i] = GetFloorLabel(i);
        return labels;
    }

    private void ShowCabinHintOnBottomBar()
    {
        string hint = string.IsNullOrWhiteSpace(cabinSelectionHint)
            ? "Usa \u2191 \u2193 o W S per scegliere il piano"
            : cabinSelectionHint;

        var bottomBar = ServiceContainer.Instance?.Get<CompactBottomBarController>(suppressWarning: true);
        bottomBar?.SetElevatorHint(hint);
        PushDisplayState(currentLevelIndex, ElevatorDirection.None, keepCabinHint: true);
    }

    private void ShowCabinConfirmHintOnBottomBar()
    {
        string hint = string.IsNullOrWhiteSpace(cabinConfirmHint)
            ? "Premi E per confermare il piano"
            : cabinConfirmHint;

        var bottomBar = ServiceContainer.Instance?.Get<CompactBottomBarController>(suppressWarning: true);
        bottomBar?.SetElevatorHint(hint);
    }

    private void ClearCabinHintOnBottomBar()
    {
        var bottomBar = ServiceContainer.Instance?.Get<CompactBottomBarController>(suppressWarning: true);
        bottomBar?.ClearElevatorHint();
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
            SetFlowState(ElevatorFlowState.DoorsOpenWaitingEntry);
            // Chiamata al piano corrente: arma l'ingresso (porte erano chiuse, player provabilmente fuori).
            if (!_playerInsideCabinZone)
                _entryArmedByDoorsOpen = true;
            if (_playerInsideCabinZone)
                ShowCabinHintOnBottomBar();
            else
                UpdateAllFloorDisplays(floorIndex, ElevatorDirection.None);
            return;
        }

        if (_callToFloorCoroutine != null)
            StopCoroutine(_callToFloorCoroutine);

        SetFlowState(ElevatorFlowState.CallingToFloor);
        _callToFloorCoroutine = StartCoroutine(CallToFloorRoutine(floorIndex));
    }

    private System.Collections.IEnumerator CallToFloorRoutine(int floorIndex)
    {
        int fromIndex = currentLevelIndex;
        ElevatorDirection direction = GetDirectionToward(fromIndex, floorIndex);

        CloseDoors(fromIndex, force: true, bypassHold: true);
        UpdateAllFloorDisplays(floorIndex, direction);

        float wait = Mathf.Max(0f, callTravelDuration);
        if (wait > 0f)
            yield return new WaitForSeconds(wait);

        RepositionCabina(floorIndex);
        OpenDoors(floorIndex);
        SetFlowState(ElevatorFlowState.DoorsOpenWaitingEntry);
        // Cabina arrivata dopo chiamata remota: arma l'ingresso (player fuori, deve camminare dentro).
        if (!_playerInsideCabinZone)
            _entryArmedByDoorsOpen = true;
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
