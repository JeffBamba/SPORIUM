using System;
using UnityEngine;
using Sporae.Core;   // usa Inventory e InventoryItem

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    [Header("Day & Actions")]
    [Min(1)] public int startingDay = 1;
    [Min(1)] public int actionsPerDay = 3;
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

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        // 🔧 assicurati che sia ROOT prima del DontDestroyOnLoad
        transform.SetParent(null, worldPositionStays: true);

        DontDestroyOnLoad(gameObject);

        CurrentDay = startingDay;
        ActionsLeft = actionsPerDay;
        CurrentCRY = startingCRY;

        AddItem("SEED_GENERIC", 1);
        AddItem("SPORE_GENERIC", 2);

        OnDayChanged?.Invoke(CurrentDay);
        OnActionsChanged?.Invoke(ActionsLeft);
        OnCRYChanged?.Invoke(CurrentCRY);
    }



    public bool TrySpendAction(int costCRY = 0)
    {
        if (ActionsLeft <= 0) return false;
        if (CurrentCRY < costCRY) return false;
        ActionsLeft--;
        if (costCRY > 0) SpendCRY(costCRY);
        OnActionsChanged?.Invoke(ActionsLeft);
        return true;
    }

    public void EndDay(int dailyPowerCost = 20)
    {
        SpendCRY(dailyPowerCost);
        CurrentDay++;
        ActionsLeft = actionsPerDay;
        OnDayChanged?.Invoke(CurrentDay);
        OnActionsChanged?.Invoke(ActionsLeft);
    }

    public void AddCRY(int amount)
    {
        if (amount <= 0) return;
        CurrentCRY += amount;
        OnCRYChanged?.Invoke(CurrentCRY);
    }

    public void SpendCRY(int amount)
    {
        if (amount <= 0) return;
        CurrentCRY = Mathf.Max(0, CurrentCRY - amount);
        OnCRYChanged?.Invoke(CurrentCRY);
    }

    // Wrappers inventario
    public bool HasItem(string id, int qty = 1) => inventory.Has(id, qty);
    public void AddItem(string id, int qty = 1) => inventory.Add(id, qty);
    public bool ConsumeItem(string id, int qty = 1) => inventory.Consume(id, qty);
    public Inventory GetInventory() => inventory;
}
