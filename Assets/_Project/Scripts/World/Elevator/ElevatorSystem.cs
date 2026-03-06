using System;
using System.Collections.Generic;
using System.Linq;
using _Project;
using _Project.Sporae.Core;
using UnityEngine;
using UnityEngine.UI;
using Sporae.DevTools;

public class ElevatorSystem : MonoBehaviour
{
    [Header("Elevator Configuration")]
    [SerializeField] private float elevatorSpeed = 1f;
    [SerializeField] private int startingLevelIndex;
    [SerializeField] private Transform[] levels;
    [SerializeField] private List<Button> levelsButtons;
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private int cryCost = 5;
    [SerializeField] private float teleportDelay = 0.1f;
    [SerializeField] private GameObject elevatorSection;
    
    [Header("Validation")]
    [SerializeField] private bool validateLevelsOnStart = true;

    [Header("Behavior")]
    [Tooltip("DEBUG_SAFE_FIX: If false, the elevator menu will NOT auto-open when the player enters the trigger. The player must press the open key while inside the trigger.")]
    [SerializeField] private bool openMenuOnTriggerEnter = true; // DEBUG_SAFE_FIX
    [SerializeField] private KeyCode openMenuKey = KeyCode.E; // DEBUG_SAFE_FIX
    [Tooltip("DEBUG_SAFE_FIX: If true, shows the existing PlayerInteractAdvice (\"Press E\") prompt while inside the elevator trigger and the menu is closed.")]
    [SerializeField] private bool showInteractAdviceWhileInside = true; // DEBUG_SAFE_FIX

    [Header("Teleport Placement")]
    [Tooltip("DEBUG_SAFE_FIX: If true, teleport uses the target level Transform X as well as Y. This prevents landing inside walls when floors are not perfectly aligned in X.")]
    [SerializeField] private bool useTargetLevelXForTeleport = true; // DEBUG_SAFE_FIX

    [Tooltip("DEBUG_SAFE_FIX: Max allowed horizontal correction when using target-level X. If exceeded, we keep the starting X to avoid teleports into other rooms.")]
    [SerializeField] private float maxTeleportXCorrection = 1.25f; // DEBUG_SAFE_FIX

    private static int WrapIndex(int i, int len)
    {
        if (len <= 0) return 0;
        int m = i % len;
        return m < 0 ? m + len : m;
    }

    private static string Bool01(bool v) => v ? "1" : "0";

    private bool playerInside = false; // inside trigger (not UI open)
    private bool uiOpen = false;
    private Transform player;
    private bool isTeleporting = false;
    private Coroutine _teleportCoroutine; // DEBUG_SAFE_FIX: Traccia la coroutine per poterla fermare
    private GameManager gameManager;
    private int currentLevelIndex;
    private PlayerClickMover2D playerMover;
    private UINotification uiNotification;
    private PlayerInteractAdvice interactAdvice; // DEBUG_SAFE_FIX: prompt "Press E"

    void Start()
    {
        ValidateConfiguration();
        
        // Trova il GameManager nella scena
        // Usa ServiceContainer invece di FindObjectOfType
        gameManager = ServiceContainer.Instance?.Get<GameManager>();
        if (gameManager == null)
        {
            SporiumLogger.LogWarning(LogCategory.Core, "GameManager non disponibile via ServiceContainer. Tentativo late binding...");
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered += OnGameManagerRegistered;
            }
        }
        playerMover = FindObjectOfType<PlayerClickMover2D>();
        interactAdvice = FindObjectOfType<PlayerInteractAdvice>();
        // Usa ServiceContainer invece di FindObjectOfType per UINotification
        uiNotification = ServiceContainer.Instance?.Get<UINotification>();
        if (uiNotification == null)
        {
            SporiumLogger.LogWarning(LogCategory.UI, "UINotification non disponibile via ServiceContainer. Tentativo late binding...");
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered += OnUINotificationRegistered;
            }
        }
        
        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }

        currentLevelIndex = startingLevelIndex; 
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

    private void Update()
    {
        // DEBUG_SAFE_FIX: Show "Press E" advice while we're inside the trigger and the elevator menu is closed.
        if (showInteractAdviceWhileInside && playerInside && !uiOpen && interactAdvice != null)
        {
            interactAdvice.AddInteractable();
        }

        // DEBUG_SAFE_FIX: If we don't auto-open, allow explicit open while inside trigger.
        if (!openMenuOnTriggerEnter && playerInside && !uiOpen && Input.GetKeyDown(openMenuKey))
        {
            ShowFloorOptions(true);
        }
        // Click come alternativa a E per aprire il menu quando si è dentro il trigger
        if (!openMenuOnTriggerEnter && playerInside && !uiOpen && Input.GetMouseButtonDown(0) && !UIBlocker.IsPointerOverUI())
        {
            Camera cam = Camera.main != null ? Camera.main : UnityEngine.Object.FindObjectOfType<Camera>();
            Vector2 worldPoint = cam != null ? (Vector2)cam.ScreenToWorldPoint(Input.mousePosition) : (Vector2)transform.position;
            Collider2D[] hits = Physics2D.OverlapCircleAll(worldPoint, 0.35f);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform.IsChildOf(transform) || hits[i].transform == transform)
                {
                    ShowFloorOptions(true);
                    break;
                }
            }
        }

        // DEBUG_SAFE_FIX: Up/Down (and W/S) floor selection must ONLY work when the elevator UI is open.
        bool upPressed = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        bool downPressed = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
        if (upPressed || downPressed)
        {
            bool canUseKeys = uiOpen && playerInside;

            if (canUseKeys)
            {
                int len = levels != null ? levels.Length : 0;
                if (len <= 0) return;

                int baseIdx = currentLevelIndex;
                if (baseIdx < 0 || baseIdx >= len)
                    baseIdx = WrapIndex(baseIdx, len);

                int next = upPressed ? WrapIndex(baseIdx + 1, len) : WrapIndex(baseIdx - 1, len);
                GoToLevel(next);
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

            if (openMenuOnTriggerEnter)
                ShowFloorOptions(true);
            else
                ShowFloorOptions(false);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!enabled) return;

        if (other.CompareTag("Player"))
        {
            player = null;
            playerInside = false;
            ShowFloorOptions(false);
        }
    }
    
    void ShowFloorOptions(bool state)
    {
        uiOpen = state;
        
        if (uiPanel != null)
        {
            // DEBUG_SAFE_FIX: Verifica e abilita il Canvas padre quando si mostra la HUD
            Canvas parentCanvas = uiPanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                if (!parentCanvas.enabled)
                {
                    SporiumLogger.LogWarning(LogCategory.UI, $"Canvas dell'ascensore era disabilitato! Abilitazione...");
                    parentCanvas.enabled = true;
                }
                
                // Verifica sorting order
                if (state)
                {
                    SporiumLogger.LogDebug(LogCategory.UI, $"Elevator HUD: Canvas enabled={parentCanvas.enabled}, sortingOrder={parentCanvas.sortingOrder}, renderMode={parentCanvas.renderMode}");
                }
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "Canvas padre non trovato per UI_ElevatorPanel!");
            }
            
            uiPanel.SetActive(state);
            
            if (state)
            {
                SporiumLogger.LogDebug(LogCategory.UI, $"Elevator HUD mostrata: uiPanel.activeSelf={uiPanel.activeSelf}, activeInHierarchy={uiPanel.activeInHierarchy}");
            }
        }

        if (state)
            UpdateAvailablesFloorOptions();
    }

    private void UpdateAvailablesFloorOptions()
    {
        for (int i = 0; i < levelsButtons.Count; ++i)
            levelsButtons[i].interactable = i != currentLevelIndex;
    }

    private void DisableAllFloorOptions()
    {
        foreach (var item in levelsButtons)
            item.interactable = false;
    }

    public void SetLevel(int levelIndex)
    {
        // DEBUG_SAFE_FIX: Ferma qualsiasi coroutine TeleportPlayer in corso e ripristina il movimento
        if (_teleportCoroutine != null)
        {
            StopCoroutine(_teleportCoroutine);
            _teleportCoroutine = null;
            // Assicurati che il movimento sia ripristinato se la coroutine è stata interrotta
            if (playerMover != null)
            {
                playerMover.SuspendMovement(false);
            }
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
    }
    
    public void GoToLevel(int levelIndex)
    {
        if (!CanTeleportToLevel(levelIndex))
        {
            return;
        }

        if (!IsLevelUnlocked(levelIndex))
        {
            if (uiNotification != null)
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
            }
            return;
        }
        
        // Prova a ottenere GameManager se non ancora disponibile
        if (gameManager == null)
        {
            gameManager = ServiceContainer.Instance?.Get<GameManager>();
        }
        
        if (gameManager == null)
        {
            SporiumLogger.LogWarning(LogCategory.Core, "GameManager non disponibile via ServiceContainer!");
            return;
        }

        if (player == null)
        {
            SporiumLogger.LogWarning(LogCategory.Core, "Player non trovato!");
            return;
        }

        if (!gameManager.TrySpendCry(cryCost))
        {
            SporiumLogger.LogWarning(LogCategory.Core, $"Non hai abbastanza azioni o CRY per usare l'ascensore! (Costo: {cryCost})");
            return;
        }

        // DEBUG_SAFE_FIX: Ferma qualsiasi coroutine in corso prima di iniziare una nuova
        if (_teleportCoroutine != null)
        {
            StopCoroutine(_teleportCoroutine);
            _teleportCoroutine = null;
            // Assicurati che il movimento sia ripristinato se la coroutine precedente è stata interrotta
            if (playerMover != null)
            {
                playerMover.SuspendMovement(false);
            }
            isTeleporting = false;
        }
        
        // Teleport con delay per evitare problemi di fisica
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
            
            DisableAllFloorOptions();
            
            //Prevent player transition to clicked position when quick floor(level) selection on elevator
            if (playerMover != null)
            {
                playerMover.StopMovement();
            }
            
            //and suspend further movement 
            if (playerMover != null)
            {
                playerMover.SuspendMovement(true);
            }

            // Delay per stabilizzare la fisica
            yield return new WaitForSeconds(teleportDelay);
            
            // Check null dopo yield (il player o levels potrebbero essere stati distrutti durante il delay)
            if (player == null)
            {
                SporiumLogger.LogError(LogCategory.Core, "Player è diventato null dopo WaitForSeconds!");
                isTeleporting = false;
                ShowFloorOptions(false); // DEBUG_SAFE_FIX: Ensure elevator menu is closed on failure
                yield break;
            }
            
            if (levels == null || levelIndex < 0 || levelIndex >= levels.Length || levels[levelIndex] == null)
            {
                SporiumLogger.LogError(LogCategory.Core, $"levels[{levelIndex}] è null dopo WaitForSeconds!");
                isTeleporting = false;
                ShowFloorOptions(false); // DEBUG_SAFE_FIX: Ensure elevator menu is closed on failure
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
                // Check null durante il loop (il player potrebbe essere stato distrutto)
                if (player == null)
                {
                    SporiumLogger.LogError(LogCategory.Core, "Player è diventato null durante il loop!");
                    isTeleporting = false;
                    ShowFloorOptions(false); // DEBUG_SAFE_FIX: Ensure elevator menu is closed on failure
                    yield break;
                }
                
                if (elevatorSection == null)
                {
                    SporiumLogger.LogError(LogCategory.Core, "elevatorSection è null!");
                    isTeleporting = false;
                    ShowFloorOptions(false); // DEBUG_SAFE_FIX: Ensure elevator menu is closed on failure
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
                // DEBUG_SAFE_FIX: Use the perspective mover's teleport API so internal UV and Rigidbody2D state stay consistent.
                // This prevents the first post-elevator input from "snapping" the player back to an old UV-projected location.
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
            _teleportCoroutine = null; // DEBUG_SAFE_FIX: Pulisci il riferimento alla coroutine
            if (playerMover != null)
            {
                playerMover.SuspendMovement(false);
            }

            currentLevelIndex = levelIndex;
            UpdateAvailablesFloorOptions();

            // DEBUG_SAFE_FIX: Close the elevator UI when we arrive at the destination floor.
            ShowFloorOptions(false);
        }
    }

    public bool IsPlayerInside => playerInside;
    public int AvailableLevels => levels != null ? levels.Length : 0;
    public int CurrentCryCost => cryCost;

    // Metodo per cambiare il costo dinamicamente
    public void SetCryCost(int newCost)
    {
        cryCost = Mathf.Max(0, newCost);
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
    /// Late binding per GameManager quando viene registrato
    /// </summary>
    private void OnGameManagerRegistered(object service)
    {
        if (service is GameManager gm && gameManager == null)
        {
            gameManager = gm;
            
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered -= OnGameManagerRegistered;
            }
        }
    }
    
    /// <summary>
    /// Late binding per UINotification quando viene registrato
    /// </summary>
    private void OnUINotificationRegistered(object service)
    {
        if (service is UINotification notification && uiNotification == null)
        {
            uiNotification = notification;
            
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered -= OnUINotificationRegistered;
            }
        }
    }
    
    // Metodo per aggiornare il riferimento al GameManager
    public void RefreshGameManagerReference()
    {
        // Usa ServiceContainer invece di FindObjectOfType
        gameManager = ServiceContainer.Instance?.Get<GameManager>();
        if (gameManager == null)
        {
            SporiumLogger.LogWarning(LogCategory.Core, "GameManager non disponibile via ServiceContainer. Tentativo late binding...");
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered += OnGameManagerRegistered;
            }
        }
    }
    
    private void OnDestroy()
    {
        // Cleanup ServiceContainer subscriptions
        if (ServiceContainer.Instance != null)
        {
            ServiceContainer.Instance.OnServiceRegistered -= OnGameManagerRegistered;
            ServiceContainer.Instance.OnServiceRegistered -= OnUINotificationRegistered;
        }
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
