using System;
using System.Collections;
using System.Collections.Generic;
using _Project;
using _Project.Scripts.Core;
using _Project.Sporae.Core;
using _Project.Systems.FoodRoom;
using _Project.Systems.SeedStorage;
using UnityEngine;
using UnityEngine.Serialization;
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
    [Tooltip("Azioni assegnate all’alba (colazione / budget giornaliero). Cap progetto: 5. Demo: tipicamente 1 finché non c’è UI colazione.")]
    [FormerlySerializedAs("_actionsPerDay")]
    [SerializeField] [Range(1, 5)] private int _dailyActionsFromBreakfast = 5;
    /// <summary>Override runtime per il prossimo cambio giorno (es. UI colazione). -1 = nessun override.</summary>
    private int _overrideNextDawnActions = -1;
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
    private PlayerHydrationSystem _playerHydrationSystem;
    private FoodRoomSystem _foodRoomSystem;
    private SeedStorageSystem _seedStorageSystem;
    private ItemConsumptionHandler _itemConsumptionHandler;

    /// <summary>Giorni consecutivi con H≈0% dopo il consumo passivo notturno. A 2 → game over disidratazione.</summary>
    private int _dehydrationZeroDayStreak;

    public event Action OnDehydrationGameOver;

    /// <summary>True solo nel Giorno 1 della Demo (tutorial mangia/bevi). Dal Giorno 2 in poi il sistema torna identico al Full Game.</summary>
    private bool _demoTutorialDayActive;

    /// <summary>True se il player ha consumato cibo solido (o frutta) dall’alba precedente.</summary>
    private bool _ateMealSincePreviousDawn;
    /// <summary>Giorni consecutivi senza pasto (cibo/frutta). All’alba: se ieri non hai mangiato, +1.</summary>
    private int _consecutiveDaysWithoutMeal;
    /// <summary>Giorni consecutivi con cap azioni = 1 e senza aver mangiato il giorno prima. A 3 → game over fame.</summary>
    private int _starvationDaysAtMinCapWithoutFood;

    public event Action OnStarvationGameOver;

    public EconomySystem EconomySystem => _economySystem;
    public ActionSystem ActionSystem => _actionSystem;
    public CondensationSystem CondensationSystem => _condensationSystem;
    public Inventory PlayerInventory => _playerInventory;
    public PlayerHydrationSystem PlayerHydrationSystem => _playerHydrationSystem;
    public FoodRoomSystem FoodRoomSystem => _foodRoomSystem;
    public SeedStorageSystem SeedStorageSystem => _seedStorageSystem;

    public int DehydrationZeroDayStreak => _dehydrationZeroDayStreak;
    public bool IsDemoTutorialDayActive => _demoTutorialDayActive;

    public int ConsecutiveDaysWithoutMeal => _consecutiveDaysWithoutMeal;
    public int StarvationDaysAtMinCapWithoutFood => _starvationDaysAtMinCapWithoutFood;
    public bool AteMealSincePreviousDawn => _ateMealSincePreviousDawn;

    /// <summary>Chiamato quando il player consuma cibo solido o frutta (non solo acqua).</summary>
    public void NotifySolidFoodConsumed()
    {
        _ateMealSincePreviousDawn = true;

        if (_actionSystem == null)
            return;

        // Boost immediato azioni solo nel Giorno 1 della Demo (tutorial mangia/bevi).
        // Dal Giorno 2 in poi si usa la logica standard (alba del giorno successivo).
        if (_demoTutorialDayActive)
        {
            const int demoBoostCap = 5;
            int breakfastBase = Mathf.Clamp(_dailyActionsFromBreakfast, 1, 5);
            if (_actionSystem.MaxActions < demoBoostCap || _actionSystem.ActionsLeft < demoBoostCap)
                _actionSystem.ResetActions(demoBoostCap);

            _actionBudgetLedger.AddOrReplace(
                ActionBudgetSource.Item,
                "Pasto (bonus demo)",
                Mathf.Max(0, demoBoostCap - breakfastBase),
                "Boost immediato dopo aver mangiato.");
            return;
        }

        int rawBreakfast = Mathf.Clamp(_dailyActionsFromBreakfast, 1, 5);
        bool isStarvationPenalizedNow = _actionSystem.MaxActions < rawBreakfast;
        if (!isStarvationPenalizedNow)
            return;

        // Recupero immediato: se mangi mentre sei penalizzato dalla fame, torni subito al cap pieno.
        _consecutiveDaysWithoutMeal = 0;
        _starvationDaysAtMinCapWithoutFood = 0;
        _actionSystem.ResetActions(rawBreakfast);
        SeedActionBudgetLedgerForDawn(rawBreakfast, penaltySteps: 0, wasOverrideBreakfast: false);
    }

    /// <summary>Imposta streak da save (solo load).</summary>
    public void SetDehydrationZeroDayStreakForLoad(int streak)
    {
        _dehydrationZeroDayStreak = Mathf.Max(0, streak);
    }

    /// <summary>Stato fame/sopravvivenza da save (solo load).</summary>
    public void SetMealSurvivalStateForLoad(int consecutiveDaysWithoutMeal, int starvationDaysAtMinCap, bool ateMealSincePreviousDawn)
    {
        _consecutiveDaysWithoutMeal = Mathf.Max(0, consecutiveDaysWithoutMeal);
        _starvationDaysAtMinCapWithoutFood = Mathf.Max(0, starvationDaysAtMinCap);
        _ateMealSincePreviousDawn = ateMealSincePreviousDawn;
    }

    /// <summary>
    /// Ripristina lo stato tutorial demo dopo load.
    /// Mantiene il flow: Giorno 1 demo = cap base 1 (boost su pasto), Giorno 2+ = comportamento full.
    /// </summary>
    public void SetDemoTutorialStateForLoad(bool demoSession, bool tutorialDayActive)
    {
        if (!demoSession)
        {
            _demoTutorialDayActive = false;
            return;
        }

        _demoTutorialDayActive = tutorialDayActive;
        _dailyActionsFromBreakfast = _demoTutorialDayActive ? 1 : 5;
    }

    /// <summary>Per UI colazione: quante azioni assegnare all’alba successiva (1–5).</summary>
    public void SetNextDawnActionsFromBreakfast(int actions)
    {
        _overrideNextDawnActions = Mathf.Clamp(actions, 1, 5);
    }

    /// <summary>Budget colazione usato all’alba (Inspector), 1–5.</summary>
    public int DailyBreakfastBudget => _dailyActionsFromBreakfast;

    /// <summary>
    /// Breakdown "di chi ha fornito le Azioni di oggi" (colazione, moduli, bonus ambiente, item).
    /// Sorgente unica per il tooltip Azioni della TopBar e per eventuali view fine-giornata.
    /// </summary>
    private readonly DailyActionBudgetLedger _actionBudgetLedger = new();
    public DailyActionBudgetLedger ActionBudgetLedger => _actionBudgetLedger;

    /// <summary>Debug/test: imposta H% del player (0–100). L’H modifica solo la velocità di movimento.</summary>
    public void DebugSetPlayerHydrationPercent(float percent) =>
        _playerHydrationSystem?.SetHydrationPercent(Mathf.Clamp(percent, 0f, 100f));

    /// <summary>Debug/test: giorni consecutivi con H≈0% (stesso effetto del load).</summary>
    public void DebugSetDehydrationZeroDayStreak(int streak) => SetDehydrationZeroDayStreakForLoad(streak);

    public void DebugSetConsecutiveDaysWithoutMeal(int days) => _consecutiveDaysWithoutMeal = Mathf.Max(0, days);

    public void DebugSetStarvationDaysAtMinCap(int days) => _starvationDaysAtMinCapWithoutFood = Mathf.Max(0, days);

    public void DebugNotifySolidFoodConsumed() => NotifySolidFoodConsumed();

    /// <summary>Debug/test: budget colazione serializzato (1–5), effetto dal prossimo reset alba senza override.</summary>
    public void DebugSetDailyBreakfastBudget(int value)
    {
        _dailyActionsFromBreakfast = Mathf.Clamp(value, 1, 5);
        _actionBudgetLedger.AddOrReplace(ActionBudgetSource.Breakfast, "Colazione (base)", _dailyActionsFromBreakfast);
    }

    /// <summary>Debug/test: azioni rimanenti e massimo oggi (max 1–5).</summary>
    public void DebugRestoreActions(int actionsLeft, int maxActions)
    {
        if (_actionSystem == null) return;
        int m = Mathf.Clamp(maxActions, 1, 5);
        int a = Mathf.Clamp(actionsLeft, 0, m);
        _actionSystem.RestoreState(a, m);
    }

    /// <summary>Modulo Cellule Staminali (Extractor) sbloccato acquistandolo dal Black Market.</summary>
    private bool _stemCellModuleUnlocked;
    public bool IsStemCellModuleUnlocked => _stemCellModuleUnlocked;
    public void UnlockStemCellModule() => _stemCellModuleUnlocked = true;
    public void SetStemCellModuleUnlocked(bool value) => _stemCellModuleUnlocked = value;
    
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
        // Baseline azioni:
        // - Demo: 1/5 (tutorial "mangia e bevi").
        // - Full game: 5/5 come comportamento standard.
        _dailyActionsFromBreakfast = Mathf.Clamp(_dailyActionsFromBreakfast, 1, 5);
        var demoSession = ServiceContainer.Instance?.Get<DemoSessionState>(suppressWarning: true);
        bool isDemo = demoSession != null && demoSession.IsDemo;
        if (isDemo)
        {
            _demoTutorialDayActive = true;
            _dailyActionsFromBreakfast = 1; // Giorno 1 tutorial: impara a mangiare/bere
        }
        else
        {
            _demoTutorialDayActive = false;
            _dailyActionsFromBreakfast = Mathf.Clamp(_dailyActionsFromBreakfast, 1, 5);
        }

        // Inizializza sistemi
        _actionSystem = new ActionSystem(Mathf.Clamp(_dailyActionsFromBreakfast, 1, 5));
        _economySystem = new EconomySystem(_startingCRY);
        _condensationSystem = new CondensationSystem();
        _deteriorationSystem = new DeteriorationSystem(this);
        _playerHydrationSystem = new PlayerHydrationSystem();
        if (isDemo)
            _playerHydrationSystem.SetHydrationPercent(75f);
        _foodRoomSystem = new FoodRoomSystem(_playerInventory, this);
        _seedStorageSystem = new SeedStorageSystem(this);
        _itemConsumptionHandler = new ItemConsumptionHandler(_playerInventory, _playerHydrationSystem, this);
        _dayCycleSystem = ServiceContainer.Instance?.Get<DayCycleSystem>();

        if (ServiceContainer.Instance != null)
        {
            if (!ServiceContainer.Instance.Contains(typeof(PlayerHydrationSystem)))
                ServiceContainer.Instance.Register(_playerHydrationSystem);
            if (!ServiceContainer.Instance.Contains(typeof(FoodRoomSystem)))
                ServiceContainer.Instance.Register(_foodRoomSystem);
            if (!ServiceContainer.Instance.Contains(typeof(SeedStorageSystem)))
                ServiceContainer.Instance.Register(_seedStorageSystem);
        }

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
        
        // Inventario iniziale pulito per il loop reale: frutti specifici e materiali base,
        // ma niente semi/spore/pre-seed placeholder.
        const int starterQuantity = 10;
        const int starterFruitQuantity = 3;
        foreach (string typeId in Items.StarterInventoryTypeIds)
            _playerInventory.Add(typeId, Items.IsSpecificFruitType(typeId) ? starterFruitQuantity : starterQuantity);
        
        // Seed iniziale del ledger (nessuna penalità fame al primo frame).
        SeedActionBudgetLedgerForDawn(rawBreakfast: Mathf.Clamp(_dailyActionsFromBreakfast, 1, 5), penaltySteps: 0, wasOverrideBreakfast: false);

        // Sincronizza sistemi interni con valori esterni
#if UNITY_EDITOR
        if (_showDebugLogs)
            SporiumLogger.LogInfo(LogCategory.Core, $"Defaults set: DailyActions={_dailyActionsFromBreakfast}, CRY={_startingCRY}");
#endif
    }

    /// <summary>Ledger tooltip: colazione base + eventuale penalità fame (importi negativi).</summary>
    private void SeedActionBudgetLedgerForDawn(int rawBreakfast, int penaltySteps, bool wasOverrideBreakfast)
    {
        rawBreakfast = Mathf.Clamp(rawBreakfast, 1, 5);
        penaltySteps = Mathf.Max(0, penaltySteps);
        _actionBudgetLedger.Clear();
        string breakfastLabel = wasOverrideBreakfast ? "Colazione (override)" : "Colazione (base)";
        _actionBudgetLedger.AddOrReplace(ActionBudgetSource.Breakfast, breakfastLabel, rawBreakfast);
        if (penaltySteps > 0)
        {
            _actionBudgetLedger.AddOrReplace(
                ActionBudgetSource.Malnutrition,
                "Penalità fame (giorni senza cibo)",
                -penaltySteps,
                $"{_consecutiveDaysWithoutMeal} gg. senza pasto");
        }
    }

    private void HandleDayChanged(int day)
    {
        // Dal Giorno 2 in poi la Demo usa esattamente le stesse regole del Full Game.
        if (_demoTutorialDayActive && day >= 2)
        {
            _demoTutorialDayActive = false;
            _dailyActionsFromBreakfast = 5;
        }

        _economySystem.Spend(_dailyPowerCost);

        if (_playerHydrationSystem != null)
        {
            _playerHydrationSystem.ProcessDailyConsumption();

            const float zeroEpsilon = 0.02f;
            if (_playerHydrationSystem.HydrationPercent <= zeroEpsilon)
            {
                _dehydrationZeroDayStreak++;
                if (_dehydrationZeroDayStreak >= 2)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    SporiumLogger.LogError(LogCategory.Core,
                        "[GameManager] Game over: disidratazione critica per 2 giorni consecutivi (H≈0%).");
#endif
                    OnDehydrationGameOver?.Invoke();
                    return;
                }
            }
            else
                _dehydrationZeroDayStreak = 0;
        }

        if (!ProcessDawnMealAndActionBudget(out int daily))
            return;

        _actionSystem.ResetActions(daily);

#if UNITY_EDITOR
        if (_showDebugLogs)
            SporiumLogger.LogInfo(LogCategory.Core,
                $"DayChanged -> Day={day}, CRY={CurrentCRY}, Actions={ActionsLeft}/{_actionSystem.MaxActions}, H={_playerHydrationSystem?.HydrationPercent ?? -1f}, dehydrStreak={_dehydrationZeroDayStreak}, noMealStreak={_consecutiveDaysWithoutMeal}, starvationMinCap={_starvationDaysAtMinCapWithoutFood}");
#endif
    }

    /// <summary>
    /// All’alba: aggiorna streak “senza pasto”, applica penalità sul cap (min 1/5), controlla game over fame.
    /// </summary>
    private bool ProcessDawnMealAndActionBudget(out int daily)
    {
        daily = 1;
        bool ateYesterday = _ateMealSincePreviousDawn;

        if (!ateYesterday)
            _consecutiveDaysWithoutMeal++;
        else
            _consecutiveDaysWithoutMeal = 0;

        int raw = GetRawBreakfastBudgetForDawn(out bool wasOverrideBreakfast);
        int penalty = Mathf.Max(0, _consecutiveDaysWithoutMeal - 2);
        daily = Mathf.Max(1, Mathf.Min(5, raw - penalty));

        if (daily == 1 && !ateYesterday)
            _starvationDaysAtMinCapWithoutFood++;
        else
            _starvationDaysAtMinCapWithoutFood = 0;

        if (_starvationDaysAtMinCapWithoutFood >= 3)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            SporiumLogger.LogError(LogCategory.Core,
                "[GameManager] Game over: fame — 3 giorni consecutivi a 1 azione senza cibo.");
#endif
            OnStarvationGameOver?.Invoke();
            return false;
        }

        _ateMealSincePreviousDawn = false;

        SeedActionBudgetLedgerForDawn(raw, penalty, wasOverrideBreakfast);
        return true;
    }

    private int GetRawBreakfastBudgetForDawn(out bool wasOverrideBreakfast)
    {
        wasOverrideBreakfast = false;
        if (_overrideNextDawnActions >= 1 && _overrideNextDawnActions <= 5)
        {
            wasOverrideBreakfast = true;
            int v = _overrideNextDawnActions;
            _overrideNextDawnActions = -1;
            return v;
        }

        return Mathf.Clamp(_dailyActionsFromBreakfast, 1, 5);
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
        _itemConsumptionHandler?.Unsubscribe();
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
