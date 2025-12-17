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

    private bool playerInside = false;
    private Transform player;
    private bool isTeleporting = false;
    private Coroutine _teleportCoroutine; // DEBUG_SAFE_FIX: Traccia la coroutine per poterla fermare
    private GameManager gameManager;
    private int currentLevelIndex;
    private PlayerClickMover2D playerMover;
    private UINotification uiNotification;

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
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            GoToLevel((currentLevelIndex + 1) % levels.Length);
        
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            GoToLevel((currentLevelIndex - 1) % levels.Length);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!enabled) return;

        if (other.CompareTag("Player"))
        {
            player = other.transform;
            ShowFloorOptions(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!enabled) return;

        if (other.CompareTag("Player"))
        {
            player = null;
            ShowFloorOptions(false);
        }
    }
    
    void ShowFloorOptions(bool state)
    {
        playerInside = state;
        
        if (uiPanel != null)
            uiPanel.SetActive(state);

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
        
        currentLevelIndex = levelIndex; 
        elevatorSection.transform.position = new Vector3(
            elevatorSection.transform.position.x,
            levels[levelIndex].position.y,
            elevatorSection.transform.position.z);
    }
    
    public void GoToLevel(int levelIndex)
    {
        if (!CanTeleportToLevel(levelIndex))
            return;

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
                yield break;
            }
            
            if (levels == null || levelIndex < 0 || levelIndex >= levels.Length || levels[levelIndex] == null)
            {
                SporiumLogger.LogError(LogCategory.Core, $"levels[{levelIndex}] è null dopo WaitForSeconds!");
                isTeleporting = false;
                yield break;
            }
            
            Vector3 targetPosition = new Vector3(
                player.position.x, 
                levels[levelIndex].position.y, 
                player.position.z
            );

            while (Vector3.Distance(player.position, targetPosition) > 0.05f)
            {
                // Check null durante il loop (il player potrebbe essere stato distrutto)
                if (player == null)
                {
                    SporiumLogger.LogError(LogCategory.Core, "Player è diventato null durante il loop!");
                    isTeleporting = false;
                    yield break;
                }
                
                if (elevatorSection == null)
                {
                    SporiumLogger.LogError(LogCategory.Core, "elevatorSection è null!");
                    isTeleporting = false;
                    yield break;
                }
                
                player.position = Vector3.Lerp(player.position, targetPosition, Time.deltaTime * elevatorSpeed);
                elevatorSection.transform.position = new Vector3(
                    elevatorSection.transform.position.x,
                    player.position.y,
                    elevatorSection.transform.position.z);
                
                yield return null;
            }
            
            if (player != null)
            {
                player.position = targetPosition;
            }
        
            isTeleporting = false;
            _teleportCoroutine = null; // DEBUG_SAFE_FIX: Pulisci il riferimento alla coroutine
            if (playerMover != null)
            {
                playerMover.SuspendMovement(false);
            }

            currentLevelIndex = levelIndex;
            UpdateAvailablesFloorOptions();
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
