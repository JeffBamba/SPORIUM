using System;
using _Project;
using _Project.Scripts.Core;
using UnityEngine;
using Sporae.Core;
using UnityEngine.Serialization;

public class  GameManager : MonoBehaviour
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
    public event Action<float> OnCondensationChanged;
    
    // Sistema di azioni separato
    private ActionSystem actionSystem;
    private EconomySystem economySystem;
    private CondensationSystem condensationSystem;
    private DeteriorationSystem deteriorationSystem;
    
    void Awake()
    {
        // Inizializza sistemi
        actionSystem = new ActionSystem(actionsPerDay);
        economySystem = new EconomySystem(startingCRY);
        condensationSystem = new CondensationSystem();
        deteriorationSystem = new DeteriorationSystem(this);
        
        // Setup iniziale
        CurrentDay = startingDay;
        ActionsLeft = actionsPerDay;
        CurrentCRY = startingCRY;

        // Inventario iniziale
        AddItem("SDE-001", 3); // Generic Seed per BLK-01.02
        AddItem("SPORE_GENERIC", 2);
        AddItem("WAT-Raw", 2);
        
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

    public bool TrySpendCry(int amount)
    {
        if (!economySystem.CanAfford(amount))
            return false;

        economySystem.Spend(amount);
        
        CurrentCRY = economySystem.CurrentCRY;
        
        OnCRYChanged?.Invoke(CurrentCRY);
        
        return true;
    }
    
    public bool TrySpendAction(int amount = 0)
    {
        if (!actionSystem.CanSpendAction(amount)) 
            return false;

        actionSystem.SpendAction(amount);

        // Aggiorna stati locali
        ActionsLeft = actionSystem.ActionsLeft;

        // Notifica UI
        OnActionsChanged?.Invoke(ActionsLeft);

        return true;
    }

    public bool TrySpendActionAndCry(int amountAction, int amountCry)
    {
        if (!actionSystem.CanSpendAction(amountAction))
            return false;

        if (!economySystem.CanAfford(amountCry))
            return false;

        actionSystem.SpendAction(amountAction);
        economySystem.Spend(amountCry);
        
        // Aggiorna stati locali
        ActionsLeft = actionSystem.ActionsLeft;
        CurrentCRY = economySystem.CurrentCRY;
        
        // Notifica UI
        OnCRYChanged?.Invoke(CurrentCRY);
        OnActionsChanged?.Invoke(ActionsLeft);
        
        return true;
    }

    public float CollectCondensation()
    {
        var amount = condensationSystem.CondensationAmount;
        
        condensationSystem.Reset();
        OnCondensationChanged?.Invoke(condensationSystem.CondensationAmount);
        
        return amount;
    }

    public float GetMaxCondensation()
    {
        return condensationSystem.GetMax();
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
        
        // 4) accumulation of condensation
        condensationSystem.DayChanged();
        OnCondensationChanged?.Invoke(condensationSystem.CondensationAmount);
        
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
