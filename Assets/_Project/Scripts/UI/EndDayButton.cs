using UnityEngine;
using UnityEngine.UI;

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

    private GameManager gameManager;
    private bool isInitialized = false;

    void Start()
    {
        if (validateOnStart)
        {
            if (!ValidateConfiguration())
            {
                Debug.LogError("[EndDayButton] Configurazione non valida! EndDayButton disabilitato.");
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
            Debug.LogWarning("[EndDayButton] dailyPowerCost non può essere negativo. Impostato a 0.");
            dailyPowerCost = 0;
        }
        
        if (endDayButton == null)
        {
            endDayButton = GetComponent<Button>();
            if (endDayButton == null)
            {
                Debug.LogError("[EndDayButton] Button component non trovato!");
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
        gameManager = FindObjectOfType<GameManager>();
        
        if (gameManager == null)
        {
            Debug.LogWarning("[EndDayButton] GameManager non trovato nella scena!");
        }
        
        // Configura il button
        if (endDayButton != null)
        {
            endDayButton.onClick.AddListener(OnEndDayButtonClicked);
            UpdateButtonState();
        }
        
        isInitialized = true;
        
        if (showDebugLogs)
        {
            Debug.Log("[EndDayButton] EndDayButton inizializzato correttamente.");
        }
    }

    private void OnEndDayButtonClicked()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[EndDayButton] EndDayButton non inizializzato!");
            return;
        }

        if (gameManager == null)
        {
            Debug.LogWarning("[EndDayButton] GameManager non disponibile!");
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
            Debug.Log($"[EndDayButton] {confirmationMessage}");
        }
        
        // Simula conferma (in un'implementazione reale, questo sarebbe un UI dialog)
        EndDay();
    }

    public void EndDay()
    {
        if (!isInitialized || gameManager == null) return;

        // Verifica se il giocatore può permettersi di finire la giornata
        if (gameManager.CurrentCRY < dailyPowerCost)
        {
            Debug.LogWarning($"[EndDayButton] Non hai abbastanza CRY per finire la giornata! " +
                           $"Richiesto: {dailyPowerCost}, Disponibile: {gameManager.CurrentCRY}");
            
            // Mostra feedback visivo (rosso, shake, etc.)
            OnEndDayFailed("CRY insufficienti");
            return;
        }

        // Finisce la giornata
        gameManager.EndDay(dailyPowerCost);
        
        if (showDebugLogs)
        {
            Debug.Log($"[EndDayButton] Giornata finita! " +
                     $"Giorno: {gameManager.CurrentDay}, " +
                     $"Azioni: {gameManager.ActionsLeft}, " +
                     $"CRY: {gameManager.CurrentCRY}");
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
        
        Debug.LogWarning($"[EndDayButton] Fallimento nel finire la giornata: {reason}");
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
        if (endDayButton == null || gameManager == null) return;
        
        bool canEndDay = gameManager.CurrentCRY >= dailyPowerCost;
        endDayButton.interactable = canEndDay;
        
        if (buttonText != null)
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
        return isInitialized && gameManager != null && gameManager.CurrentCRY >= dailyPowerCost;
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
        if (isInitialized && gameManager != null)
        {
            UpdateButtonState();
        }
    }
}
