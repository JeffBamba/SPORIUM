using System;
using UnityEngine;
using Sporae.Core;

public class GameManager : MonoBehaviour
{
    [Header("Day & Actions")]
    [Min(1)] public int startingDay = 1;
    [Min(1)] public int actionsPerDay = 4; // BLK-01.02: 4 azioni per test completo
    public int CurrentDay { get; private set; }
    public int ActionsLeft { get; private set; }

    [Header("Economy (CRY)")]
    public int startingCRY = 50;
    public int CurrentCRY { get; private set; }

    [Header("Inventory")]
    [SerializeField] private Inventory inventory = new Inventory();

    // Eventi per la UI
    public event Action<int> OnDayChanged;
    public event Action<int> OnActionsChanged;
    public event Action<int> OnCRYChanged;

    // Sistema di azioni separato
    private ActionSystem actionSystem;
    private EconomySystem economySystem;

    void Awake()
    {
        // Inizializza sistemi
        actionSystem = new ActionSystem(actionsPerDay);
        economySystem = new EconomySystem(startingCRY);
        
        // Setup iniziale
        CurrentDay = startingDay;
        ActionsLeft = actionsPerDay;
        CurrentCRY = startingCRY;

        // Inventario iniziale
        AddItem("SDE-001", 3); // Generic Seed per BLK-01.02
        AddItem("SPORE_GENERIC", 2);

        // Notifica UI
        NotifyUI();
    }

    public bool TrySpendAction(int costCRY = 0)
    {
        if (!actionSystem.CanSpendAction()) return false;
        if (!economySystem.CanAfford(costCRY)) return false;

        actionSystem.SpendAction();
        if (costCRY > 0) economySystem.Spend(costCRY);

        // Aggiorna stati locali
        ActionsLeft = actionSystem.ActionsLeft;
        CurrentCRY = economySystem.CurrentCRY;

        // Notifica UI
        OnActionsChanged?.Invoke(ActionsLeft);
        OnCRYChanged?.Invoke(CurrentCRY);

        return true;
    }

    public void EndDay(int dailyPowerCost = 20)
    {
        if (economySystem.CanAfford(dailyPowerCost))
        {
            economySystem.Spend(dailyPowerCost);
            CurrentDay++;
            actionSystem.ResetActions();
            
            // Aggiorna stati locali
            ActionsLeft = actionSystem.ActionsLeft;
            CurrentCRY = economySystem.CurrentCRY;
            
            // Notifica UI
            NotifyUI();
        }
        else
        {
            Debug.LogWarning("[GameManager] Non hai abbastanza CRY per finire la giornata!");
        }
    }

    public void AddCRY(int amount)
    {
        if (amount <= 0) return;
        
        economySystem.Add(amount);
        CurrentCRY = economySystem.CurrentCRY;
        OnCRYChanged?.Invoke(CurrentCRY);
    }

    public void SpendCRY(int amount)
    {
        if (amount <= 0) return;
        
        if (economySystem.CanAfford(amount))
        {
            economySystem.Spend(amount);
            CurrentCRY = economySystem.CurrentCRY;
            OnCRYChanged?.Invoke(CurrentCRY);
        }
    }

    // Wrappers inventario
    public bool HasItem(string id, int qty = 1) => inventory.Has(id, qty);
    public void AddItem(string id, int qty = 1) => inventory.Add(id, qty);
    public bool ConsumeItem(string id, int qty = 1) => inventory.Consume(id, qty);
    public Inventory GetInventory() => inventory;

    private void NotifyUI()
    {
        OnDayChanged?.Invoke(CurrentDay);
        OnActionsChanged?.Invoke(ActionsLeft);
        OnCRYChanged?.Invoke(CurrentCRY);
    }
}
