using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI actionsText;
    [SerializeField] private TextMeshProUGUI cryText;
    
    [Header("Validation")]
    [SerializeField] private bool validateOnStart = true;
    [SerializeField] private bool showDebugLogs = false;

    private GameManager gameManager;
    private bool isInitialized = false;

    void Start()
    {
        if (validateOnStart)
        {
            if (!ValidateUIReferences())
            {
                Debug.LogError("[HUDController] Riferimenti UI mancanti! HUD disabilitato.");
                enabled = false;
                return;
            }
        }

        InitializeHUD();
    }

    private bool ValidateUIReferences()
    {
        bool isValid = true;
        
        if (dayText == null)
        {
            Debug.LogError("[HUDController] dayText non assegnato!");
            isValid = false;
        }
        
        if (actionsText == null)
        {
            Debug.LogError("[HUDController] actionsText non assegnato!");
            isValid = false;
        }
        
        if (cryText == null)
        {
            Debug.LogError("[HUDController] cryText non assegnato!");
            isValid = false;
        }
        
        return isValid;
    }

    private void InitializeHUD()
    {
        // Cerca il GameManager nella scena
        gameManager = FindObjectOfType<GameManager>();
        
        if (gameManager == null)
        {
            Debug.LogWarning("[HUDController] GameManager non trovato nella scena. HUD in modalità offline.");
            SetOfflineMode();
            return;
        }

        // Sottoscrivi agli eventi
        SubscribeToEvents();
        
        // Aggiorna UI iniziale
        UpdateAllUI();
        
        isInitialized = true;
        
        if (showDebugLogs)
        {
            Debug.Log("[HUDController] HUD inizializzato correttamente.");
        }
    }

    private void SubscribeToEvents()
    {
        if (gameManager == null) return;
        
        gameManager.OnDayChanged += UpdateDay;
        gameManager.OnActionsChanged += UpdateActions;
        gameManager.OnCRYChanged += UpdateCRY;
    }

    private void UnsubscribeFromEvents()
    {
        if (gameManager == null) return;
        
        gameManager.OnDayChanged -= UpdateDay;
        gameManager.OnActionsChanged -= UpdateActions;
        gameManager.OnCRYChanged -= UpdateCRY;
    }

    private void UpdateAllUI()
    {
        if (gameManager == null) return;
        
        UpdateDay(gameManager.CurrentDay);
        UpdateActions(gameManager.ActionsLeft);
        UpdateCRY(gameManager.CurrentCRY);
    }

    private void UpdateDay(int day)
    {
        if (dayText != null)
        {
            dayText.text = $"Giorno: {day}";
        }
    }

    private void UpdateActions(int actions)
    {
        if (actionsText != null)
        {
            actionsText.text = $"Azioni: {actions}";
            
            // Cambia colore in base alle azioni rimanenti
            if (actions <= 0)
            {
                actionsText.color = Color.red;
            }
            else if (actions <= 1)
            {
                actionsText.color = Color.yellow;
            }
            else
            {
                actionsText.color = Color.white;
            }
        }
    }

    private void UpdateCRY(int cry)
    {
        if (cryText != null)
        {
            cryText.text = $"CRY: {cry}";
            
            // Cambia colore in base ai CRY rimanenti
            if (cry <= 10)
            {
                cryText.color = Color.red;
            }
            else if (cry <= 25)
            {
                cryText.color = Color.yellow;
            }
            else
            {
                cryText.color = Color.white;
            }
        }
    }

    private void SetOfflineMode()
    {
        if (dayText != null) dayText.text = "Giorno: --";
        if (actionsText != null) actionsText.text = "Azioni: --";
        if (cryText != null) cryText.text = "CRY: --";
        
        // Disabilita interazioni
        if (dayText != null) dayText.raycastTarget = false;
        if (actionsText != null) actionsText.raycastTarget = false;
        if (cryText != null) cryText.raycastTarget = false;
    }

    public void RefreshHUD()
    {
        if (isInitialized && gameManager != null)
        {
            UpdateAllUI();
        }
    }

    public void SetDebugMode(bool enabled)
    {
        showDebugLogs = enabled;
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    void OnEnable()
    {
        if (isInitialized && gameManager != null)
        {
            SubscribeToEvents();
        }
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    // Metodo pubblico per aggiornare manualmente un elemento specifico
    public void ForceUpdateDay(int day)
    {
        UpdateDay(day);
    }

    public void ForceUpdateActions(int actions)
    {
        UpdateActions(actions);
    }

    public void ForceUpdateCRY(int cry)
    {
        UpdateCRY(cry);
    }
}
