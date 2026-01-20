using System;
using System.Collections;
using System.Collections.Generic;
using _Project;
using _Project.Scripts.Core;
using _Project.Sporae.Core;
using UnityEngine;
using Sporae.Core;
using Sporae.DevTools;

/// <summary>
/// GameManager principale del gioco.
/// Eseguito con priorità -50 per essere dopo GamePlayInstaller (-100) ma prima degli altri componenti.
/// </summary>
[DefaultExecutionOrder(-50)]
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
        // Garantisce inizializzazione ServiceContainer
        ServiceContainer.Init();
        
        // Attendi un frame per assicurarsi che ServiceContainer sia completamente inizializzato
        // (necessario se GameManager viene creato prima di GamePlayInstaller)
        if (ServiceContainer.Instance == null)
        {
            SporiumLogger.LogWarning(LogCategory.Core, "ServiceContainer.Instance è ancora null dopo Init(). Tentativo di registrazione ritardata...");
            StartCoroutine(RegisterWhenReady());
            return;
        }
        
        // Registra GameManager nel ServiceContainer per dependency injection
        RegisterInServiceContainer();
        
        // Inizializza sistemi
        InitializeSystems();
    }
    
    /// <summary>
    /// Registra GameManager nel ServiceContainer
    /// </summary>
    private void RegisterInServiceContainer()
    {
        if (ServiceContainer.Instance != null)
        {
            // DEBUG_SAFE_FIX: Verifica se è già registrato prima di registrarlo di nuovo
            if (!ServiceContainer.Instance.Contains(typeof(GameManager)))
            {
                ServiceContainer.Instance.Register(this);
#if UNITY_EDITOR
                if (_showDebugLogs)
                    SporiumLogger.LogInfo(LogCategory.Core, "Registrato nel ServiceContainer");
#endif
            }
        }
        else
        {
            SporiumLogger.LogError(LogCategory.Core, "ServiceContainer.Instance è null! Impossibile registrare GameManager.");
        }
    }
    
    /// <summary>
    /// Coroutine per registrare GameManager quando ServiceContainer è pronto
    /// </summary>
    private IEnumerator RegisterWhenReady()
    {
        int maxAttempts = 10;
        int attempts = 0;
        
        while (ServiceContainer.Instance == null && attempts < maxAttempts)
        {
            yield return null;
            attempts++;
        }
        
        if (ServiceContainer.Instance != null)
        {
            RegisterInServiceContainer();
            
            // Inizializza sistemi dopo la registrazione
            InitializeSystems();
        }
        else
        {
            SporiumLogger.LogError(LogCategory.Core, $"ServiceContainer.Instance rimane null dopo {maxAttempts} tentativi!");
        }
    }
    
    /// <summary>
    /// Inizializza i sistemi del GameManager
    /// </summary>
    private void InitializeSystems()
    {
        // Inizializza sistemi
        _actionSystem = new ActionSystem(_actionsPerDay);
        _economySystem = new EconomySystem(_startingCRY);
        _condensationSystem = new CondensationSystem();
        _deteriorationSystem = new DeteriorationSystem(this);
        _dayCycleSystem = ServiceContainer.Instance?.Get<DayCycleSystem>();

        if (_dayCycleSystem != null)
        {
            _dayCycleSystem.OnDayChanged += HandleDayChanged;
        }
        else
        {
            SporiumLogger.LogWarning(LogCategory.Core, "DayCycleSystem non disponibile al momento. Verrà sottoscritto quando disponibile.");
            // Late binding: sottoscrivi quando disponibile
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered += OnServiceRegistered;
            }
        }
        
        // Inventario iniziale
        _playerInventory.Add(Items.Seed001, 2);  // Standard
        _playerInventory.Add(Items.Seed002, 2);  // Pure
        _playerInventory.Add(Items.Seed003, 2);  // Evil
        _playerInventory.Add(Items.SporeGeneric, 2);
        _playerInventory.Add(Items.Water, 2);
        _playerInventory.Add(Items.Fruits, 5);
        
        // BLK-03.01-T1: Fertilizzanti iniziali (2x ogni tipo)
        _playerInventory.Add(Items.FertilizerStandard, 2);
        _playerInventory.Add(Items.FertilizerPure, 2);
        _playerInventory.Add(Items.FertilizerProhibited, 2);
        
        // Additivi pH iniziali (sostituiscono lo spray come consumabile principale)
        // Nota: lo Spray legacy (STR-004) resta supportato per retrocompatibilità e potatura, ma non viene più seedato di default.
        _playerInventory.Add(Items.AdditiveBasic, 2);
        _playerInventory.Add(Items.AdditiveAcid, 1);
        
        // Sincronizza sistemi interni con valori esterni
#if UNITY_EDITOR
        if (_showDebugLogs)
            SporiumLogger.LogInfo(LogCategory.Core, $"Defaults set: Actions={_actionsPerDay}, CRY={_startingCRY}");
#endif
    }

    private void HandleDayChanged(int day)
    {   
        _economySystem.Spend(_dailyPowerCost);
        _actionSystem.ResetActions(_actionsPerDay);
        
        // FASE 3: Condensazione ora gestita da DayCycleController.ApplyCondensationSystem()
        // (chiamato dopo che tutte le piante sono state processate)
        
#if UNITY_EDITOR
        if (_showDebugLogs)
            SporiumLogger.LogInfo(LogCategory.Core, $"EndDay -> Day={day}, CRY={CurrentCRY}, Actions={ActionsLeft}");
#endif
    }

    public bool TrySpendCry(int amount)
    {
        if (!_economySystem.CanAfford(amount))
            return false;

        _economySystem.Spend(amount);
        
        return true;
    }
    
    public bool TrySpendAction(int amount = 1)
    {
        // DEBUG_SAFE_FIX: Se amount è 0 o negativo, usa 1 come default
        if (amount <= 0)
        {
            SporiumLogger.LogWarning(LogCategory.Core, $"TrySpendAction chiamato con amount={amount}! Usando 1 come default.");
            amount = 1;
        }
        
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

    /// <summary>
    /// FASE 4: Raccoglie condensazione e restituisce reward scalato basato su percentuale.
    /// - 0-49%: 5-10 WAT-RAW
    /// - 50-79%: 15-25 WAT-RAW
    /// - 80-100%: 30-40 WAT-RAW
    /// </summary>
    public int CollectCondensation()
    {
        float percentage = _condensationSystem.CurrentAccumulation;
        int reward = CalculateScaledReward(percentage);
        
        _condensationSystem.Reset();
        OnCondensationChanged?.Invoke(_condensationSystem.CurrentAccumulation);
        
        return reward;
    }
    
    /// <summary>
    /// FASE 4: Calcola reward scalato basato su percentuale condensazione.
    /// </summary>
    private int CalculateScaledReward(float percentage)
    {
        if (percentage < 50f)
            return UnityEngine.Random.Range(5, 11);      // 5-10 WAT-RAW
        if (percentage < 80f)
            return UnityEngine.Random.Range(15, 26);     // 15-25 WAT-RAW
        return UnityEngine.Random.Range(30, 41);         // 30-40 WAT-RAW
    }

    public float GetMaxCondensation()
    {
        // FASE 1: Sempre 100% nel nuovo sistema
        return _condensationSystem.GetMax(); // Restituisce 100
    }
    
    /// <summary>
    /// FASE 3: Notifica cambio condensazione (chiamato da DayCycleController dopo calcolo).
    /// </summary>
    public void NotifyCondensationChanged()
    {
        OnCondensationChanged?.Invoke(_condensationSystem.CurrentAccumulation);
    }
    
    /// <summary>
    /// Late binding: sottoscrive DayCycleSystem quando viene registrato
    /// </summary>
    private void OnServiceRegistered(object service)
    {
        if (service is DayCycleSystem dayCycle && _dayCycleSystem == null)
        {
            _dayCycleSystem = dayCycle;
            _dayCycleSystem.OnDayChanged += HandleDayChanged;
            
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered -= OnServiceRegistered;
            }
            
#if UNITY_EDITOR
            if (_showDebugLogs)
                SporiumLogger.LogInfo(LogCategory.Core, "DayCycleSystem sottoscritto con successo (late binding)");
#endif
        }
    }
    
    private void OnDestroy()
    {
        // Cleanup event subscription
        if (ServiceContainer.Instance != null)
        {
            ServiceContainer.Instance.OnServiceRegistered -= OnServiceRegistered;
        }
        
        if (_dayCycleSystem != null)
        {
            _dayCycleSystem.OnDayChanged -= HandleDayChanged;
        }
    }
    
    /// <summary>
    /// Debug: mostra stato attuale del GameManager
    /// </summary>
    [ContextMenu("Debug GameManager Status")]
    public void DebugGameManagerStatus()
    {
#if UNITY_EDITOR
        SporiumLogger.LogInfo(LogCategory.Core, "=== GAMEMANAGER DEBUG STATUS ===");
        SporiumLogger.LogInfo(LogCategory.Core, $"ActionSystem - Max: {_actionSystem?.MaxActions}, Left: {_actionSystem?.ActionsLeft}");
        SporiumLogger.LogInfo(LogCategory.Core, $"EconomySystem - Current: {_economySystem?.CurrentCRY}");
        SporiumLogger.LogInfo(LogCategory.Core, $"DayCycleSystem - Available: {_dayCycleSystem != null}");
        SporiumLogger.LogInfo(LogCategory.Core, "================================");
#endif
    }
}
