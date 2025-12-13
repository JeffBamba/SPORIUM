using UnityEngine;
using _Project.Sporae.Core;
using Sporae.DevTools;

public class ActionCost : MonoBehaviour
{
    [Header("Cost Configuration")]
    [SerializeField] private int cryCost = 0;
    [SerializeField] private int actionCost = 1;
    [SerializeField] private bool requireBothResources = false;
    
    [Header("Validation")]
    [SerializeField] private bool validateOnStart = true;

    private GameManager _gameManager;

    private void Start()
    {
        if (validateOnStart)
        {
            ValidateConfiguration();
        }
        
        InitializeActionCost();
    }

    private void ValidateConfiguration()
    {
        if (cryCost < 0)
        {
            SporiumLogger.LogWarning(LogCategory.Core, "cryCost non può essere negativo. Impostato a 0.");
            cryCost = 0;
        }
        
        if (actionCost < 0)
        {
            SporiumLogger.LogWarning(LogCategory.Core, "actionCost non può essere negativo. Impostato a 0.");
            actionCost = 0;
        }
    }

    private void InitializeActionCost()
    {
        // Usa ServiceContainer invece di FindObjectOfType
        _gameManager = ServiceContainer.Instance?.Get<GameManager>();
        
        if (_gameManager == null)
        {
            SporiumLogger.LogWarning(LogCategory.Core, "GameManager non disponibile via ServiceContainer. Tentativo late binding...");
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered += OnGameManagerRegistered;
            }
        }
    }
    
    /// <summary>
    /// Late binding per GameManager quando viene registrato
    /// </summary>
    private void OnGameManagerRegistered(object service)
    {
        if (service is GameManager gameManager && _gameManager == null)
        {
            _gameManager = gameManager;
            
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered -= OnGameManagerRegistered;
            }
        }
    }
    
    private void OnDestroy()
    {
        // Cleanup ServiceContainer subscriptions
        if (ServiceContainer.Instance != null)
        {
            ServiceContainer.Instance.OnServiceRegistered -= OnGameManagerRegistered;
        }
    }

    public bool TryPerform()
    {
        if (_gameManager == null)
        {
            SporiumLogger.LogWarning(LogCategory.Core, "GameManager non disponibile!");
            return false;
        }

        if (requireBothResources)
        {
            // Richiede sia azioni che CRY
            return _gameManager.TrySpendAction(cryCost);
        }
        else
        {
            // Richiede solo azioni, CRY è opzionale
            return _gameManager.TrySpendAction(cryCost);
        }
    }

    public bool CanPerform()
    {
        if (_gameManager == null) return false;
        
        if (requireBothResources)
        {
            return _gameManager.ActionsLeft >= actionCost && _gameManager.CurrentCRY >= cryCost;
        }
        else
        {
            return _gameManager.ActionsLeft >= actionCost;
        }
    }

    public int GetTotalCost()
    {
        return cryCost;
    }

    public int GetActionCost()
    {
        return actionCost;
    }

    public void SetCryCost(int newCost)
    {
        cryCost = Mathf.Max(0, newCost);
    }

    public void SetActionCost(int newCost)
    {
        actionCost = Mathf.Max(0, newCost);
    }

    public void SetRequireBothResources(bool requireBoth)
    {
        requireBothResources = requireBoth;
    }

    // Metodo per ottenere informazioni sul costo
    public string GetCostDescription()
    {
        if (cryCost > 0 && actionCost > 0)
        {
            return $"Azione: {actionCost}, CRY: {cryCost}";
        }
        else if (cryCost > 0)
        {
            return $"CRY: {cryCost}";
        }
        else if (actionCost > 0)
        {
            return $"Azione: {actionCost}";
        }
        else
        {
            return "Gratuito";
        }
    }

    // Metodo per verificare se il costo è valido
    public bool IsValidCost()
    {
        return cryCost >= 0 && actionCost >= 0;
    }

    // Metodo per resettare i costi
    public void ResetCosts()
    {
        cryCost = 0;
        actionCost = 1;
        requireBothResources = false;
    }
}
