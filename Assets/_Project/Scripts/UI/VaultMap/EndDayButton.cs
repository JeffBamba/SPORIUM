using _Project;
using _Project.Sporae.Core;
using Sporae.Core;
using Sporae.UI.UIToolkit.NotificationsFoundation;
using UnityEngine;
using UnityEngine.UI;
using Sporae.DevTools;

public class EndDayButton : MonoBehaviour
{
    [Header("Day End Configuration")]
    [SerializeField] private int dailyPowerCost = 20;
    [SerializeField] private bool confirmBeforeEnding = true;
    [SerializeField] private string confirmationMessage = "Sei sicuro di voler finire la giornata?";
    
    [Header("UI References")]
    [SerializeField] private Button endDayButton;
    [SerializeField] private Text buttonText;
    
    [Header("Validation")]
    [SerializeField] private bool validateOnStart = true;
    [SerializeField] private bool showDebugLogs = true;

    [SerializeField] private DiaryUI _diaryUI;
    
    private DayCycleSystem _dayCycleSystem;
    private GameManager _gameManager;
    private bool _isInitialized;

    private void Awake()
    {
        _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
        _gameManager = FindObjectOfType<GameManager>();
    }
    
    private void Start()
    {
        if (validateOnStart)
        {
            if (!ValidateConfiguration())
            {
                SporiumLogger.LogError(LogCategory.UI, "Configurazione non valida! EndDayButton disabilitato.");
                enabled = false;
                return;
            }
        }
        
        InitializeEndDayButton();
    }

    private bool ValidateConfiguration()
    {
        bool isValid = true;
        
        if (dailyPowerCost < 0)
        {
            SporiumLogger.LogWarning(LogCategory.UI, "dailyPowerCost non può essere negativo. Impostato a 0.");
            dailyPowerCost = 0;
        }
        
        if (endDayButton == null)
        {
            endDayButton = GetComponent<Button>();
            if (endDayButton == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "Button component non trovato!");
                isValid = false;
            }
        }
        
        if (buttonText == null)
        {
            buttonText = GetComponentInChildren<Text>();
        }
        
        return isValid;
    }

    private void InitializeEndDayButton()
    {
        endDayButton.onClick.AddListener(OnEndDayButtonClicked);
        UpdateButtonState();
        
        _isInitialized = true;
        
        if (showDebugLogs)
        {
            SporiumLogger.LogInfo(LogCategory.UI, "EndDayButton inizializzato correttamente.");
        }
    }

    private void OnEndDayButtonClicked()
    {
        if (!_isInitialized)
        {
            SporiumLogger.LogWarning(LogCategory.UI, "EndDayButton non inizializzato!");
            return;
        }
        
        if (confirmBeforeEnding)
        {
            ShowConfirmationDialog();
        }
        else
        {
            EndDay();
        }
    }

    private void ShowConfirmationDialog()
    {
        // Implementa qui la logica per mostrare un dialog di conferma
        // Per ora, usa un semplice Debug.Log
        if (showDebugLogs)
        {
            SporiumLogger.LogDebug(LogCategory.UI, confirmationMessage);
        }
        
        // DEBUG_SAFE_FIX: Non chiamare EndDay() automaticamente
        // Deve aspettare la conferma reale dell'utente
        // EndDay();  // ← RIMOSSO: causava doppia chiamata
    }

    public void EndDay()
    {
        if (!_isInitialized || !_gameManager)
            return;
        
        if (_dayCycleSystem.CanEndDay())
        {
            // Salvataggio automatico prima di finire il giorno
            var saveManager = ServiceContainer.Instance?.Get<SaveManager>();
            if (saveManager != null)
            {
                bool saveSuccess = saveManager.SaveGame("default");
                if (showDebugLogs)
                {
                    if (saveSuccess)
                        SporiumLogger.LogInfo(LogCategory.Save, "Salvataggio automatico eseguito con successo");
                    else
                        SporiumLogger.LogWarning(LogCategory.Save, "Errore durante il salvataggio automatico");
                }
                if (saveSuccess)
                {
                    var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                    if (foundation != null && foundation.Enabled)
                        foundation.PostToast("SYS-003", new NotificationPayload());
                }
            }
            
            _diaryUI.Show();
        }
        else 
            OnEndDayFailed("CRY insufficienti");
        
        if (showDebugLogs)
        {
            SporiumLogger.LogInfo(LogCategory.Core, $"Giornata finita! " +
                     $"Giorno: {_dayCycleSystem.CurrentDay}, " +
                     $"Azioni: {_gameManager.ActionsLeft}, " +
                     $"CRY: {_gameManager.CurrentCRY}");
        }
        
        OnEndDaySuccess();
    }

    private void OnEndDaySuccess()
    {
        // Feedback visivo di successo
        if (endDayButton != null)
        {
            // Cambia temporaneamente colore per feedback
            var colors = endDayButton.colors;
            colors.normalColor = Color.green;
            endDayButton.colors = colors;
            
            // Reset colore dopo un delay
            Invoke(nameof(ResetButtonColor), 0.5f);
        }
        
        // Aggiorna stato del button
        UpdateButtonState();
    }

    private void OnEndDayFailed(string reason)
    {
        // Feedback visivo di fallimento
        if (endDayButton != null)
        {
            var colors = endDayButton.colors;
            colors.normalColor = Color.red;
            endDayButton.colors = colors;
            
            Invoke(nameof(ResetButtonColor), 0.5f);
        }
        
        SporiumLogger.LogWarning(LogCategory.UI, $"Fallimento nel finire la giornata: {reason}");
    }

    private void ResetButtonColor()
    {
        if (endDayButton != null)
        {
            var colors = endDayButton.colors;
            colors.normalColor = Color.white;
            endDayButton.colors = colors;
        }
    }

    private void UpdateButtonState()
    {
        if (!endDayButton || !_gameManager)
            return;
        
        bool canEndDay = _gameManager.CurrentCRY >= dailyPowerCost;
        endDayButton.interactable = canEndDay;
        
        if (buttonText)
        {
            buttonText.text = canEndDay ? 
                $"Fine Giornata ({dailyPowerCost} CRY)" : 
                $"CRY Insufficienti ({dailyPowerCost} richiesti)";
        }
    }

    public void SetDailyPowerCost(int newCost)
    {
        dailyPowerCost = Mathf.Max(0, newCost);
        UpdateButtonState();
    }

    public void SetConfirmBeforeEnding(bool confirm)
    {
        confirmBeforeEnding = confirm;
    }

    public void SetConfirmationMessage(string message)
    {
        confirmationMessage = string.IsNullOrEmpty(message) ? 
            "Sei sicuro di voler finire la giornata?" : message;
    }

    public int GetDailyPowerCost()
    {
        return dailyPowerCost;
    }

    public bool CanEndDay()
    {
        return _isInitialized && _gameManager != null && _gameManager.CurrentCRY >= dailyPowerCost;
    }

    void OnDestroy()
    {
        if (endDayButton != null)
        {
            endDayButton.onClick.RemoveListener(OnEndDayButtonClicked);
        }
    }

    void Update()
    {
        // Aggiorna stato del button in tempo reale
        if (_isInitialized && _gameManager != null)
        {
            UpdateButtonState();
        }
    }
}
