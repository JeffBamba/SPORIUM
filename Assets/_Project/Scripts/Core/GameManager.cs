using System;
using _Project;
using _Project.Scripts.Core;
using UnityEngine;
using Sporae.Core;
using UnityEngine.Serialization;

public class  GameManager : MonoBehaviour
{
    [SerializeField] private bool _showDebugLogs = true;
    
    [Header("Day & Actions")]
    
    [SerializeField] [Min(1)] private int _startingDay = 1;
    [SerializeField] [Min(1)] private int _actionsPerDay = 4;
    [SerializeField] private int _startingCRY = 250;
    
    public int CurrentDay { get; private set; }
    public int ActionsLeft => _actionSystem.ActionsLeft;
    public int CurrentCRY => _economySystem.CurrentCRY;
    
    private readonly Inventory _inventory = new();

    public event Action<int> OnDayChanged;
    public event Action<float> OnCondensationChanged;
    
    private ActionSystem _actionSystem;
    private EconomySystem _economySystem;
    private CondensationSystem _condensationSystem;
    private DeteriorationSystem _deteriorationSystem;
    
    public EconomySystem EconomySystem => _economySystem;
    public ActionSystem ActionSystem => _actionSystem;
    public CondensationSystem CondensationSystem => _condensationSystem;
    
    void Awake()
    {
        // Inizializza sistemi
        _actionSystem = new ActionSystem(_actionsPerDay);
        _economySystem = new EconomySystem(_startingCRY);
        _condensationSystem = new CondensationSystem();
        _deteriorationSystem = new DeteriorationSystem(this);
        
        // Setup iniziale
        CurrentDay = _startingDay;

        // Inventario iniziale
        AddItem("SDE-001", 4);
        AddItem("SPORE_GENERIC", 2);
        AddItem("WAT-Raw", 2);
        
        // Sincronizza sistemi interni con valori esterni
        if (_showDebugLogs)
            Debug.Log($"[{nameof(GameManager)}] Defaults set: Actions={_actionsPerDay}, CRY={_startingCRY}");
        
        // Notifica UI
        NotifyUI();
    }

    public bool TrySpendCry(int amount)
    {
        if (!_economySystem.CanAfford(amount))
            return false;

        _economySystem.Spend(amount);
        
        return true;
    }
    
    public bool TrySpendAction(int amount = 0)
    {
        if (!_actionSystem.CanSpendAction(amount)) 
            return false;

        _actionSystem.SpendAction(amount);

        return true;
    }

    public bool TrySpendActionAndCry(int amountAction, int amountCry)
    {
        if (!_actionSystem.CanSpendAction(amountAction))
            return false;

        if (!_economySystem.CanAfford(amountCry))
            return false;

        _actionSystem.SpendAction(amountAction);
        _economySystem.Spend(amountCry);
        
        return true;
    }

    public float CollectCondensation()
    {
        var amount = _condensationSystem.CondensationAmount;
        
        _condensationSystem.Reset();
        OnCondensationChanged?.Invoke(_condensationSystem.CondensationAmount);
        
        return amount;
    }

    public float GetMaxCondensation()
    {
        return _condensationSystem.GetMax();
    }

    public void EndDay(int dailyPowerCost = 20)
    {
        CurrentDay++;
        OnDayChanged?.Invoke(CurrentDay); 
        
        _economySystem.Spend(dailyPowerCost);
        _actionSystem.ResetActions(_actionsPerDay);
        
        _condensationSystem.DayChanged();
        OnCondensationChanged?.Invoke(_condensationSystem.CondensationAmount);
        
        Debug.Log($"[{nameof(GameManager)}] EndDay -> Day={CurrentDay}, CRY={CurrentCRY}, Actions={ActionsLeft}");
    }


    public bool HasItem(string id, int qty = 1) => _inventory.Has(id, qty);
    public void AddItem(string id, int qty = 1) => _inventory.Add(id, qty);
    public bool ConsumeItem(string id, int qty = 1) => _inventory.Consume(id, qty);
    public Inventory GetInventory() => _inventory;

    private void NotifyUI()
    {
        OnDayChanged?.Invoke(CurrentDay);
    }
    
    /// <summary>
    /// Forza aggiornamento UI (per debug e sincronizzazione)
    /// </summary>
    public void ForceUIUpdate()
    {
        if (_showDebugLogs)
            Debug.Log($"[{nameof(GameManager)}] Force UI Update - Day: {CurrentDay}, Actions: {ActionsLeft}, CRY: {CurrentCRY}");
        
        NotifyUI();
    }
    
    /// <summary>
    /// Debug: mostra stato attuale del GameManager
    /// </summary>
    [ContextMenu("Debug GameManager Status")]
    public void DebugGameManagerStatus()
    {
        Debug.Log("=== GAMEMANAGER DEBUG STATUS ===");
        Debug.Log($"Starting Values - Day: {_startingDay}, Actions: {_actionsPerDay}, CRY: {_startingCRY}");
        Debug.Log($"Current Values - Day: {CurrentDay}, Actions: {ActionsLeft}, CRY: {CurrentCRY}");
        Debug.Log($"ActionSystem - Max: {_actionSystem?.MaxActions}, Left: {_actionSystem?.ActionsLeft}");
        Debug.Log($"EconomySystem - Current: {_economySystem?.CurrentCRY}");
        Debug.Log("================================");
    }
}
