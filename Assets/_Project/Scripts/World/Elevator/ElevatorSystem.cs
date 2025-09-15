using System;
using System.Collections.Generic;
using System.Linq;
using _Project;
using UnityEngine;
using UnityEngine.UI;

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
    private GameManager gameManager;
    private int currentLevelIndex;
    private PlayerClickMover2D playerMover;
    private UINotification uiNotification;

    void Start()
    {
        ValidateConfiguration();
        
        // Trova il GameManager nella scena
        gameManager = FindObjectOfType<GameManager>();
        playerMover = FindObjectOfType<PlayerClickMover2D>();
        uiNotification = FindObjectOfType<UINotification>();
        
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
                Debug.LogError("[ElevatorSystem] Nessun livello configurato!");
                enabled = false;
                return;
            }

            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] == null)
                {
                    Debug.LogError($"[ElevatorSystem] Livello {i} è null!");
                    enabled = false;
                    return;
                }
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
            GoToLevel((currentLevelIndex + 1) % levels.Length);
        
        if (Input.GetKeyDown(KeyCode.S))
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
    
    public void GoToLevel(int levelIndex)
    {
        if (!CanTeleportToLevel(levelIndex))
            return;

        if (!IsLevelUnlocked(levelIndex))
        {
            uiNotification.ShowNotification("Sorry, out of order", 3);
            return;
        }
            
        
        if (gameManager == null)
        {
            Debug.LogWarning("[ElevatorSystem] GameManager non trovato!");
            return;
        }

        if (player == null)
        {
            Debug.LogWarning("[ElevatorSystem] Player non trovato!");
            return;
        }

        if (!gameManager.TrySpendCry(cryCost))
        {
            Debug.LogWarning($"[ElevatorSystem] Non hai abbastanza azioni o CRY per usare l'ascensore! (Costo: {cryCost})");
            return;
        }

        // Teleport con delay per evitare problemi di fisica
        StartCoroutine(TeleportPlayer(levelIndex));
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
            playerMover.StopMovement();
            
            //and suspend further movement 
            playerMover.SuspendMovement(true);

            // Delay per stabilizzare la fisica
            yield return new WaitForSeconds(teleportDelay);
            
            Vector3 targetPosition = new Vector3(
                player.position.x, 
                levels[levelIndex].position.y, 
                player.position.z
            );

            var diff = player.position - targetPosition;
            while (Vector3.Distance(player.position, targetPosition) > 0.05f)
            {
                player.position = Vector3.Lerp(player.position, targetPosition, Time.deltaTime * elevatorSpeed);
                elevatorSection.transform.position = new Vector3(
                    elevatorSection.transform.position.x,
                    player.position.y,
                    elevatorSection.transform.position.z);
                
                yield return null;
            }
            
            player.position = targetPosition;
        
            isTeleporting = false;
            playerMover.SuspendMovement(false);

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

    // Metodo per aggiornare il riferimento al GameManager
    public void RefreshGameManagerReference()
    {
        gameManager = FindObjectOfType<GameManager>();
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
