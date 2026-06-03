using System;
using System.Collections.Generic;
using _Project;
using _Project.Sporae.Core;
using UnityEngine;
using Sporae.DevTools;

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

    [Header("Validation")]
    [SerializeField] private bool validateLevelsOnStart = true;

    [Header("Teleport Placement")]
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
    private Coroutine _outOfServiceCoroutine;
    private Coroutine _callToFloorCoroutine;

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
            player = other.transform;
            playerInside = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!enabled) return;

        if (other.CompareTag("Player"))
        {
            player = null;
            playerInside = false;
        }
    }

    public void SetLevel(int levelIndex)
    {
        // Stop any running TeleportPlayer coroutine and restore world input.
        if (_teleportCoroutine != null)
        {
            StopCoroutine(_teleportCoroutine);
            _teleportCoroutine = null;
            GameplayUiModalLock.SetBlockWorldInput(false);
            isTeleporting = false;
        }

        if (levels == null || levels.Length == 0)
            return;

        int idx = WrapIndex(levelIndex, levels.Length);
        currentLevelIndex = idx;
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
        }

        _teleportCoroutine = StartCoroutine(TeleportPlayer(levelIndex));
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

    private System.Collections.IEnumerator TeleportPlayer(int levelIndex)
    {
        if (player != null && levels[levelIndex] != null)
        {
            isTeleporting = true;

            // Block world movement/interaction for the duration of the travel.
            // NOTE: PlayerPerspectiveMover2D (the active mover in VaultMap) respects GameplayUiModalLock,
            // whereas PlayerClickMover2D is already suspended by PlayerMoverRouter2D and is NOT the active mover.
            GameplayUiModalLock.SetBlockWorldInput(true);

            // Delay per stabilizzare la fisica
            yield return new WaitForSeconds(teleportDelay);

            if (player == null)
            {
                SporiumLogger.LogError(LogCategory.Core, "Player è diventato null dopo WaitForSeconds!");
                isTeleporting = false;
                _teleportCoroutine = null;
                GameplayUiModalLock.SetBlockWorldInput(false);
                yield break;
            }

            if (levels == null || levelIndex < 0 || levelIndex >= levels.Length || levels[levelIndex] == null)
            {
                SporiumLogger.LogError(LogCategory.Core, $"levels[{levelIndex}] è null dopo WaitForSeconds!");
                isTeleporting = false;
                _teleportCoroutine = null;
                GameplayUiModalLock.SetBlockWorldInput(false);
                yield break;
            }

            float startX = player.position.x;
            float requestedFinalX = useTargetLevelXForTeleport ? levels[levelIndex].position.x : startX;
            float xDelta = Mathf.Abs(requestedFinalX - startX);
            float finalX = (useTargetLevelXForTeleport && xDelta <= Mathf.Max(0.01f, maxTeleportXCorrection)) ? requestedFinalX : startX;

            // IMPORTANT: animate vertically only to avoid sweeping into colliders on the source floor.
            Vector3 animTargetPosition = new Vector3(
                startX,
                levels[levelIndex].position.y,
                player.position.z
            );

            // Final snap/teleport target (can include X correction).
            Vector3 finalTargetPosition = new Vector3(
                finalX,
                levels[levelIndex].position.y,
                player.position.z
            );

            while (Vector3.Distance(player.position, animTargetPosition) > 0.05f)
            {
                if (player == null)
                {
                    SporiumLogger.LogError(LogCategory.Core, "Player è diventato null durante il loop!");
                    isTeleporting = false;
                    _teleportCoroutine = null;
                    GameplayUiModalLock.SetBlockWorldInput(false);
                    yield break;
                }

                if (elevatorSection == null)
                {
                    SporiumLogger.LogError(LogCategory.Core, "elevatorSection è null!");
                    isTeleporting = false;
                    _teleportCoroutine = null;
                    GameplayUiModalLock.SetBlockWorldInput(false);
                    yield break;
                }

                player.position = Vector3.Lerp(player.position, animTargetPosition, Time.deltaTime * elevatorSpeed);
                elevatorSection.transform.position = new Vector3(
                    elevatorSection.transform.position.x,
                    player.position.y,
                    elevatorSection.transform.position.z);

                yield return null;
            }

            if (player != null)
            {
                // Use the perspective mover's teleport API so internal UV and Rigidbody2D state stay consistent.
                var perspectiveMover = player.GetComponent<_Project.Player.PlayerPerspectiveMover2D>();
                if (perspectiveMover != null)
                {
                    perspectiveMover.TeleportToWorld(new Vector2(finalTargetPosition.x, finalTargetPosition.y), pickAreaByPoint: true);
                }
                else
                {
                    player.position = finalTargetPosition;
                    var rb2d = player.GetComponent<Rigidbody2D>();
                    if (rb2d != null)
                    {
                        rb2d.position = new Vector2(finalTargetPosition.x, finalTargetPosition.y);
                        rb2d.velocity = Vector2.zero;
                    }
                }
            }

            isTeleporting = false;
            _teleportCoroutine = null;
            GameplayUiModalLock.SetBlockWorldInput(false);

            currentLevelIndex = levelIndex;
        }
    }

    public bool IsPlayerInside => playerInside;
    public int AvailableLevels => levels != null ? levels.Length : 0;

    /// <summary>Apre le porte del piano indicato (no-op se indice/riferimento non validi).</summary>
    public void OpenDoors(int floorIndex)
    {
        ElevatorDoorPair pair = GetFloorDoors(floorIndex);
        if (pair != null) pair.Open();
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
        string label = GetFloorLabel(shownFloorIndex);
        for (int i = 0; i < _displays.Count; i++)
        {
            if (_displays[i] != null)
                _displays[i].SetContent(label, direction);
        }
    }

    /// <summary>
    /// Chiamata dell'ascensore dal display di un piano: il player resta fermo;
    /// i display mostrano direzione, la cabina logica si sposta e le porte del piano si aprono.
    /// </summary>
    public void CallToFloor(int floorIndex)
    {
        if (!enabled || levels == null || floorIndex < 0 || floorIndex >= levels.Length)
            return;

        if (!IsLevelUnlocked(floorIndex))
        {
            ShowOutOfServiceTemporarily(floorIndex);
            return;
        }

        if (floorIndex == currentLevelIndex)
        {
            OpenDoors(floorIndex);
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
