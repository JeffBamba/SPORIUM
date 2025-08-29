using UnityEngine;

public class ActionCost : MonoBehaviour
{
    [Header("Cost Configuration")]
    [SerializeField] private int cryCost = 0;
    [SerializeField] private int actionCost = 1;
    [SerializeField] private bool requireBothResources = false;
    
    [Header("Validation")]
    [SerializeField] private bool validateOnStart = true;

    private GameManager gameManager;

    void Start()
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
            Debug.LogWarning("[ActionCost] cryCost non può essere negativo. Impostato a 0.");
            cryCost = 0;
        }
        
        if (actionCost < 0)
        {
            Debug.LogWarning("[ActionCost] actionCost non può essere negativo. Impostato a 0.");
            actionCost = 0;
        }
    }

    private void InitializeActionCost()
    {
        gameManager = FindObjectOfType<GameManager>();
        
        if (gameManager == null)
        {
            Debug.LogWarning("[ActionCost] GameManager non trovato nella scena!");
        }
    }

    public bool TryPerform()
    {
        if (gameManager == null)
        {
            Debug.LogWarning("[ActionCost] GameManager non disponibile!");
            return false;
        }

        if (requireBothResources)
        {
            // Richiede sia azioni che CRY
            return gameManager.TrySpendAction(cryCost);
        }
        else
        {
            // Richiede solo azioni, CRY è opzionale
            return gameManager.TrySpendAction(cryCost);
        }
    }

    public bool CanPerform()
    {
        if (gameManager == null) return false;
        
        if (requireBothResources)
        {
            return gameManager.ActionsLeft >= actionCost && gameManager.CurrentCRY >= cryCost;
        }
        else
        {
            return gameManager.ActionsLeft >= actionCost;
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
