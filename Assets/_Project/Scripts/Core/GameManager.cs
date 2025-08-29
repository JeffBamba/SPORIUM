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
    [Tooltip("CRY iniziali per test BLK-01.03A (aumentato per permettere loop completo)")]
    public int startingCRY = 250;
    public int CurrentCRY { get; private set; }

    [Header("Inventory")]
    [SerializeField] private Inventory inventory = new Inventory();
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

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
        
        // BLK-01.03A: CRY aumentati per permettere test completo (Plant+Water+Light+EndDay × 5 giorni)

        // BLK-01.03A: Default HUD 4 azioni / 250 CRY
        ActionsLeft = 4; OnActionsChanged?.Invoke(ActionsLeft);
        CurrentCRY = 250; OnCRYChanged?.Invoke(CurrentCRY);
        
        // Sincronizza sistemi interni con valori esterni
        if (actionSystem != null) actionSystem.ResetActions(4);
        if (economySystem != null) economySystem.SetCRY(250);
        
        Debug.Log("[BLK-01.03A] Defaults set: Actions=4, CRY=250 (sistemi sincronizzati)");
        
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
        CurrentDay++;
        OnDayChanged?.Invoke(CurrentDay);         // 1) growth tick
        
        // 2) costo giornaliero usando sistema interno (sempre 20)
        int dailyCost = 20; // Costo fisso per BLK-01.03A
        if (economySystem != null) economySystem.Spend(dailyCost);
        CurrentCRY = economySystem?.CurrentCRY ?? CurrentCRY;
        OnCRYChanged?.Invoke(CurrentCRY);
        
        // 3) reset azioni usando sistema interno
        if (actionSystem != null) actionSystem.ResetActions(4);
        ActionsLeft = actionSystem?.ActionsLeft ?? 4;
        OnActionsChanged?.Invoke(ActionsLeft);
        
        Debug.Log($"[BLK-01.03A] EndDay -> Day={CurrentDay}, CRY={CurrentCRY}, Actions={ActionsLeft}");
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
    
    /// <summary>
    /// Forza aggiornamento UI (per debug e sincronizzazione)
    /// </summary>
    public void ForceUIUpdate()
    {
        if (showDebugLogs)
        {
            Debug.Log($"[GameManager] Force UI Update - Day: {CurrentDay}, Actions: {ActionsLeft}, CRY: {CurrentCRY}");
        }
        
        NotifyUI();
    }
    
    /// <summary>
    /// Debug: mostra stato attuale del GameManager
    /// </summary>
    [ContextMenu("Debug GameManager Status")]
    public void DebugGameManagerStatus()
    {
        Debug.Log("=== GAMEMANAGER DEBUG STATUS ===");
        Debug.Log($"Starting Values - Day: {startingDay}, Actions: {actionsPerDay}, CRY: {startingCRY}");
        Debug.Log($"Current Values - Day: {CurrentDay}, Actions: {ActionsLeft}, CRY: {CurrentCRY}");
        Debug.Log($"ActionSystem - Max: {actionSystem?.MaxActions}, Left: {actionSystem?.ActionsLeft}");
        Debug.Log($"EconomySystem - Current: {economySystem?.CurrentCRY}");
        Debug.Log("================================");
    }
}
