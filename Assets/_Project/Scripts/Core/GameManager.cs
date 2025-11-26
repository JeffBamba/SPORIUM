using System;
using _Project;
using _Project.Scripts.Core;
using _Project.Sporae.Core;
using UnityEngine;
using Sporae.Core;

public class GameManager : MonoBehaviour
{
    [SerializeField] private bool _showDebugLogs = true;
    
    [Header("Day & Actions")]
    
    [SerializeField] [Min(1)] private int _actionsPerDay = 4;
    [SerializeField] private int _startingCRY = 250;
    [SerializeField] private int _dailyPowerCost = 20;
    
    public int ActionsLeft => _actionSystem.ActionsLeft;
    public int CurrentCRY => _economySystem.CurrentCRY;
    
    private readonly Inventory _playerInventory = new();

    public event Action<float> OnCondensationChanged;
    
    private ActionSystem _actionSystem;
    private EconomySystem _economySystem;
    private CondensationSystem _condensationSystem;
    private DeteriorationSystem _deteriorationSystem;
    private DayCycleSystem _dayCycleSystem;
    
    public EconomySystem EconomySystem => _economySystem;
    public ActionSystem ActionSystem => _actionSystem;
    public CondensationSystem CondensationSystem => _condensationSystem;
    public Inventory PlayerInventory => _playerInventory;
    
    private void Awake()
    {
        // Inizializza sistemi
        _actionSystem = new ActionSystem(_actionsPerDay);
        _economySystem = new EconomySystem(_startingCRY);
        _condensationSystem = new CondensationSystem();
        _deteriorationSystem = new DeteriorationSystem(this);
        _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();

        _dayCycleSystem.OnDayChanged += HandleDayChanged;
        
        // Inventario iniziale
        _playerInventory.Add(Items.Seed001, 2);  // Standard
        _playerInventory.Add(Items.Seed002, 2);  // Pure
        _playerInventory.Add(Items.Seed003, 2);  // Evil
        _playerInventory.Add(Items.SporeGeneric, 2);
        _playerInventory.Add(Items.Water, 2);
        _playerInventory.Add(Items.Fruits, 5);
        
        // Sincronizza sistemi interni con valori esterni
        if (_showDebugLogs)
            Debug.Log($"[{nameof(GameManager)}] Defaults set: Actions={_actionsPerDay}, CRY={_startingCRY}");
    }

    private void HandleDayChanged(int day)
    {   
        _economySystem.Spend(_dailyPowerCost);
        _actionSystem.ResetActions(_actionsPerDay);
        
        _condensationSystem.DayChanged();
        OnCondensationChanged?.Invoke(_condensationSystem.CondensationAmount);
        
        Debug.Log($"[{nameof(GameManager)}] EndDay -> Day={day}, CRY={CurrentCRY}, Actions={ActionsLeft}");
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
    
    /// <summary>
    /// Debug: mostra stato attuale del GameManager
    /// </summary>
    [ContextMenu("Debug GameManager Status")]
    public void DebugGameManagerStatus()
    {
        Debug.Log("=== GAMEMANAGER DEBUG STATUS ===");
        Debug.Log($"ActionSystem - Max: {_actionSystem?.MaxActions}, Left: {_actionSystem?.ActionsLeft}");
        Debug.Log($"EconomySystem - Current: {_economySystem?.CurrentCRY}");
        Debug.Log("================================");
    }
}
