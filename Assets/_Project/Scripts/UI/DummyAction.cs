using UnityEngine;

public class DummyAction : MonoBehaviour
{
    [Header("Action Configuration")]
    [SerializeField] private ActionCost cost;
    [SerializeField] private string actionName = "Azione";
    [SerializeField] private bool showDebugLogs = true;
    
    [Header("Validation")]
    [SerializeField] private bool validateOnStart = true;

    private GameManager gameManager;
    private bool isInitialized = false;

    void Start()
    {
        if (validateOnStart)
        {
            if (!ValidateConfiguration())
            {
                Debug.LogError("[DummyAction] Configurazione non valida! DummyAction disabilitato.");
                enabled = false;
                return;
            }
        }
        
        InitializeDummyAction();
    }

    private bool ValidateConfiguration()
    {
        bool isValid = true;
        
        if (cost == null)
        {
            Debug.LogError("[DummyAction] ActionCost non assegnato!");
            isValid = false;
        }
        
        if (string.IsNullOrEmpty(actionName))
        {
            Debug.LogWarning("[DummyAction] Nome azione vuoto. Impostato nome di default.");
            actionName = "Azione";
        }
        
        return isValid;
    }

    private void InitializeDummyAction()
    {
        gameManager = FindObjectOfType<GameManager>();
        
        if (gameManager == null)
        {
            Debug.LogWarning("[DummyAction] GameManager non trovato nella scena!");
        }
        
        isInitialized = true;
    }

    public void DoAction()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[DummyAction] DummyAction non inizializzato!");
            return;
        }

        if (cost == null)
        {
            Debug.LogError("[DummyAction] ActionCost non disponibile!");
            return;
        }

        if (gameManager == null)
        {
            Debug.LogWarning("[DummyAction] GameManager non disponibile!");
            return;
        }

        // Verifica se l'azione può essere eseguita
        if (cost.CanPerform())
        {
            // Esegui l'azione
            if (cost.TryPerform())
            {
                OnActionSuccess();
            }
            else
            {
                OnActionFailed("Fallimento nell'esecuzione dell'azione");
            }
        }
        else
        {
            OnActionFailed("Risorse insufficienti");
        }
    }

    private void OnActionSuccess()
    {
        if (showDebugLogs)
        {
            Debug.Log($"[DummyAction] {actionName} eseguita con successo! " +
                     $"Azioni rimanenti: {gameManager.ActionsLeft}, CRY: {gameManager.CurrentCRY}");
        }
        
        // Qui puoi aggiungere effetti visivi, audio, etc.
        // OnActionSuccess?.Invoke();
    }

    private void OnActionFailed(string reason)
    {
        if (showDebugLogs)
        {
            Debug.LogWarning($"[DummyAction] {actionName} fallita: {reason}. " +
                           $"Azioni rimanenti: {gameManager.ActionsLeft}, CRY: {gameManager.CurrentCRY}");
        }
        
        // Qui puoi aggiungere feedback visivo per il fallimento
        // OnActionFailed?.Invoke(reason);
    }

    public bool CanPerformAction()
    {
        if (!isInitialized || cost == null) return false;
        return cost.CanPerform();
    }

    public string GetActionStatus()
    {
        if (!isInitialized || cost == null) return "Non inizializzato";
        
        if (cost.CanPerform())
        {
            return $"Disponibile - {cost.GetCostDescription()}";
        }
        else
        {
            return $"Non disponibile - {cost.GetCostDescription()}";
        }
    }

    public void SetActionName(string newName)
    {
        actionName = string.IsNullOrEmpty(newName) ? "Azione" : newName;
    }

    public void SetShowDebugLogs(bool show)
    {
        showDebugLogs = show;
    }

    public void RefreshActionCost()
    {
        if (cost != null)
        {
            cost.ResetCosts();
        }
    }

    // Metodo per forzare l'esecuzione dell'azione (bypass controlli)
    public void ForceDoAction()
    {
        if (!isInitialized || cost == null || gameManager == null) return;
        
        // Forza l'esecuzione senza controlli
        cost.TryPerform();
        
        if (showDebugLogs)
        {
            Debug.Log($"[DummyAction] {actionName} forzata! " +
                     $"Azioni rimanenti: {gameManager.ActionsLeft}, CRY: {gameManager.CurrentCRY}");
        }
    }

    // Metodo per ottenere informazioni dettagliate
    public string GetDetailedInfo()
    {
        if (!isInitialized) return "Non inizializzato";
        
        string info = $"Nome: {actionName}\n";
        info += $"Stato: {GetActionStatus()}\n";
        
        if (cost != null)
        {
            info += $"Costo: {cost.GetCostDescription()}\n";
            info += $"Valido: {cost.IsValidCost()}";
        }
        else
        {
            info += "Costo: Non assegnato";
        }
        
        return info;
    }
}
