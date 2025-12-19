using _Project.Sporae.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sporae.DevTools;

public class HUDController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI actionsText;
    [SerializeField] private TextMeshProUGUI cryText;
    
    [Header("Validation")]
    [SerializeField] private bool validateOnStart = true;
    [SerializeField] private bool showDebugLogs = false;

    private DayCycleSystem _dayCycleSystem;
    private GameManager _gameManager;
    private bool _isInitialized = false;

    private void Awake()
    {
        _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
    }
    
    private void Start()
    {
        if (validateOnStart)
        {
            if (!ValidateUIReferences())
            {
                SporiumLogger.LogError(LogCategory.UI, "Riferimenti UI mancanti! HUD disabilitato.");
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
            SporiumLogger.LogError(LogCategory.UI, "dayText non assegnato!");
            isValid = false;
        }
        
        if (actionsText == null)
        {
            SporiumLogger.LogError(LogCategory.UI, "actionsText non assegnato!");
            isValid = false;
        }
        
        if (cryText == null)
        {
            SporiumLogger.LogError(LogCategory.UI, "cryText non assegnato!");
            isValid = false;
        }
        
        return isValid;
    }

    private void InitializeHUD()
    {
        // Cerca il GameManager nella scena
        _gameManager = FindObjectOfType<GameManager>();
        
        if (_gameManager == null)
        {
            SporiumLogger.LogWarning(LogCategory.UI, "GameManager non trovato nella scena. HUD in modalità offline.");
            SetOfflineMode();
            return;
        }

        // Sottoscrivi agli eventi
        SubscribeToEvents();
        
        // Aggiorna UI iniziale con delay per assicurarsi che GameManager sia pronto
        StartCoroutine(InitializeWithDelay());
        
        _isInitialized = true;
        
        if (showDebugLogs)
        {
            SporiumLogger.LogInfo(LogCategory.UI, "HUD inizializzato correttamente.");
        }
    }
    
    private System.Collections.IEnumerator InitializeWithDelay()
    {
        // Aspetta un frame per assicurarsi che GameManager sia completamente inizializzato
        yield return null;
        
        // Forza aggiornamento UI
        ForceUpdateAllUI();
        
        if (showDebugLogs)
        {
            SporiumLogger.LogDebug(LogCategory.UI, "UI aggiornata con delay per sincronizzazione GameManager");
        }
    }

    private void SubscribeToEvents()
    {
        if (!_gameManager)
            return;
        
        if (_dayCycleSystem != null)
            _dayCycleSystem.OnDayChanged += UpdateDay;
        
        if (_gameManager.ActionSystem != null)
            _gameManager.ActionSystem.OnActionsChanged += UpdateActions;
        
        if (_gameManager.EconomySystem != null)
            _gameManager.EconomySystem.OnCRYChanged += UpdateCRY;
    }

    private void UnsubscribeFromEvents()
    {
        if (_dayCycleSystem != null)
            _dayCycleSystem.OnDayChanged -= UpdateDay;
        
        if (!_gameManager)
            return;
        
        if (_gameManager.ActionSystem != null)
            _gameManager.ActionSystem.OnActionsChanged -= UpdateActions;
        
        if (_gameManager.EconomySystem != null)
            _gameManager.EconomySystem.OnCRYChanged -= UpdateCRY;
    }

    private void UpdateAllUI()
    {
        if (!_gameManager)
            return;
        
        UpdateDay(_dayCycleSystem.CurrentDay);
        UpdateActions(_gameManager.ActionsLeft);
        UpdateCRY(_gameManager.CurrentCRY);
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
        // Azioni ora gestite dalla TopBar UI Toolkit - nascondi il testo nella vecchia HUD
        if (actionsText != null)
        {
            actionsText.text = ""; // Rimuovi il testo "Azioni: X"
            actionsText.gameObject.SetActive(false); // Nascondi completamente l'elemento
        }
    }

    private void UpdateCRY(int cry)
    {
        // CRY ora gestito dalla TopBar UI Toolkit - nascondi il testo nella vecchia HUD
        if (cryText != null)
        {
            cryText.text = ""; // Rimuovi il testo "CRY: X"
            cryText.gameObject.SetActive(false); // Nascondi completamente l'elemento
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
        if (_isInitialized && _gameManager != null)
        {
            UpdateAllUI();
        }
    }
    
    /// <summary>
    /// Forza aggiornamento completo dell'HUD (per debug e sincronizzazione)
    /// </summary>
    public void ForceUpdateAllUI()
    {
        if (_gameManager == null) return;
        
        if (showDebugLogs)
        {
            SporiumLogger.LogInfo(LogCategory.UI, $"Force Update - Day: {_dayCycleSystem.CurrentDay}, Actions: {_gameManager.ActionsLeft}, CRY: {_gameManager.CurrentCRY}");
        }
        
        UpdateDay(_dayCycleSystem.CurrentDay);
        UpdateActions(_gameManager.ActionsLeft);
        UpdateCRY(_gameManager.CurrentCRY);
    }
    
    /// <summary>
    /// Debug: mostra stato attuale dell'HUD e del GameManager
    /// </summary>
    [ContextMenu("Debug HUD Status")]
    public void DebugHUDStatus()
    {
        SporiumLogger.LogInfo(LogCategory.UI, "=== HUD DEBUG STATUS ===");
        SporiumLogger.LogDebug(LogCategory.UI, $"HUD Initialized: {_isInitialized}");
        SporiumLogger.LogDebug(LogCategory.UI, $"GameManager Found: {_gameManager != null}");
        
        if (_gameManager != null)
        {
            SporiumLogger.LogDebug(LogCategory.UI, $"GameManager Values - Day: {_dayCycleSystem.CurrentDay}, Actions: {_gameManager.ActionsLeft}, CRY: {_gameManager.CurrentCRY}");
        }
        
        if (dayText != null) SporiumLogger.LogDebug(LogCategory.UI, $"Day Text: {dayText.text}");
        if (actionsText != null) SporiumLogger.LogDebug(LogCategory.UI, $"Actions Text: {actionsText.text}");
        if (cryText != null) SporiumLogger.LogDebug(LogCategory.UI, $"CRY Text: {cryText.text}");
        SporiumLogger.LogDebug(LogCategory.UI, "========================");
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
        if (_isInitialized && _gameManager != null)
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
