---
name: Sistema Interfacce Testabilità Completo
overview: Implementazione completa del sistema di interfacce per testabilità, analizzando tutte le funzionalità esistenti in Sporium e creando interfacce per tutti i sistemi principali per abilitare test unitari isolati e dependency injection completa.
todos:
  - id: create-core-interfaces
    content: "Creare 7 interfacce core: IServiceContainer, IGameManager, IEconomySystem, IActionSystem, IDayCycleSystem, ICondensationSystem, IInventory"
    status: pending
  - id: implement-core-classes
    content: "Far implementare le interfacce alle classi core: ServiceContainer, GameManager, EconomySystem, ActionSystem, DayCycleSystem, CondensationSystem, Inventory"
    status: pending
  - id: create-dome-interfaces
    content: "Creare 3 interfacce Dome: IPotStateModel, IPhSystem, IPotActions"
    status: pending
  - id: implement-dome-classes
    content: "Far implementare le interfacce alle classi Dome: PotStateModel, PhSystem, PotActions"
    status: pending
  - id: enhance-servicecontainer
    content: Aggiungere supporto registrazione per interfacce in ServiceContainer (overload Register<TInterface, TImplementation>)
    status: pending
  - id: refactor-potactions
    content: Refactorizzare PotActions.cs per usare IGameManager e IPhSystem invece di classi concrete
    status: pending
  - id: refactor-gamemanager
    content: Refactorizzare GameManager.cs per usare interfacce dei sistemi interni (IActionSystem, IEconomySystem, etc.)
    status: pending
  - id: refactor-ui-files
    content: Refactorizzare file UI principali (TopBarController, HUDInventory, PotDetailsWidget) per usare IGameManager
    status: pending
  - id: create-support-interfaces
    content: "Creare 4 interfacce support systems: ISaveManager, IAssetManager, IDiaryStatistics, IMissionManager"
    status: pending
  - id: implement-support-classes
    content: Far implementare le interfacce a SaveManager, AssetManager, DiaryStatistics, MissionManager
    status: pending
  - id: refactor-support-usage
    content: Refactorizzare ActionSystem e EconomySystem per usare IDiaryStatistics invece di classe concreta
    status: pending
  - id: verify-compilation
    content: Verificare compilazione, avvio gioco e funzionalità principali dopo tutte le modifiche
    status: pending
---

# Piano Implementazione Sistema Interfacce per Testabilità

## Analisi Funzionalità Esistenti

### Sistemi Core Identificati

**1. Game Management**

- `GameManager`: Gestione ciclo gioco, azioni giornaliere, economia, inventario
- `ActionSystem`: Gestione azioni disponibili (4/giorno)
- `EconomySystem`: Gestione valuta CRY
- `DayCycleSystem`: Gestione ciclo giornaliero
- `CondensationSystem`: Gestione condensazione
- `DeteriorationSystem`: Gestione deterioramento

**2. Dome Systems (Pot & Plants)**

- `PotActions`: Azioni sui vasi (piantare, annaffiare, illuminare, raccogliere)
- `PotStateModel`: Modello stato vaso (hydration, light, stage, condition)
- `DayCycleController`: Controller crescita piante
- `PotGrowthController`: Controller crescita individuale
- `PhSystem`: Sistema pH globale Dome (-100 a +100)
- `PlantConditionSystem`: Sistema condizioni piante
- `MoldSystem`: Sistema muffe
- `FertilizerSystem`: Sistema fertilizzanti
- `PruningSystem`: Sistema potatura
- `PlantLevelSystem`: Sistema livelli piante

**3. Inventory & Items**

- `Inventory`: Gestione inventario giocatore
- `ItemFabric`: Factory creazione item
- `Item`, `ItemConfig`: Modelli item

**4. Save & Load**

- `SaveManager`: Sistema salvataggio/caricamento completo

**5. Asset Management**

- `AssetManager`: Caricamento e caching ScriptableObjects

**6. UI Systems**

- Vari controller UI (TopBarController, PlantCardV3Controller, etc.)

**7. Mission & Statistics**

- `MissionManager`: Gestione missioni
- `DiaryStatistics`: Statistiche diario
- `GoalCheckers`: Checker obiettivi

**8. Other Systems**

- `EventSystem`: Sistema eventi centralizzato
- `ServiceContainer`: Service Locator pattern

## Interfacce da Creare

### Fase 1: Core Interfaces (Priorità Alta)

**1. IServiceContainer** (`Assets/_Project/Scripts/Core/Interfaces/IServiceContainer.cs`)

- Metodi: `Register<T>`, `Get<T>`, `Contains<T>`, `RegisterGlobal<T>`
- Evento: `OnServiceRegistered`
- Implementata da: `ServiceContainer`

**2. IGameManager** (`Assets/_Project/Scripts/Core/Interfaces/IGameManager.cs`)

- Proprietà: `ActionsLeft`, `CurrentCRY`, `PlayerInventory`
- Metodi: `TrySpendCry`, `TrySpendAction`, `TrySpendActionAndCry`
- Proprietà esposte: `EconomySystem`, `ActionSystem`, `CondensationSystem`
- Eventi: `OnCondensationChanged`
- Implementata da: `GameManager`

**3. IEconomySystem** (`Assets/_Project/Scripts/Core/Interfaces/IEconomySystem.cs`)

- Proprietà: `CurrentCRY`, `MaxCRY`
- Metodi: `CanAfford`, `Add`, `Spend`, `SetCRY`, `GetCRYPercentage`, `RestoreState`
- Eventi: `OnCRYChanged`
- Implementata da: `EconomySystem`

**4. IActionSystem** (`Assets/_Project/Scripts/Core/Interfaces/IActionSystem.cs`)

- Proprietà: `ActionsLeft`, `MaxActions`
- Metodi: `CanSpendAction`, `SpendAction`, `ResetActions`, `AddActions`, `GetActionPercentage`, `RestoreState`
- Eventi: `OnActionsChanged`
- Implementata da: `ActionSystem`

**5. IDayCycleSystem** (`Assets/_Project/Scripts/Core/Interfaces/IDayCycleSystem.cs`)

- Proprietà: `CurrentDay`, `DailyPowerCost`
- Metodi: `CanEndDay`, `EndDay`
- Eventi: `OnDayChanged`
- Implementata da: `DayCycleSystem`

**6. ICondensationSystem** (`Assets/_Project/Scripts/Core/Interfaces/ICondensationSystem.cs`)

- Proprietà: `CondensationAmount`
- Metodi: `DayChanged`, `Reset`, `GetMax`
- Implementata da: `CondensationSystem`

**7. IInventory** (`Assets/_Project/Scripts/Core/Interfaces/IInventory.cs`)

- Proprietà: `Items`, `UniqueItems`, `IsEmpty`
- Metodi: `Add(string, int)`, `Add(string)`, `Add(Item)`, `Has`, `Consume`, `Clear`
- Eventi: `OnInventoryChanged`
- Implementata da: `Inventory`

### Fase 2: Dome System Interfaces (Priorità Alta)

**8. IPotStateModel** (`Assets/_Project/Scripts/Dome/Interfaces/IPotStateModel.cs`)

- Proprietà principali: `PotId`, `HasPlant`, `Stage`, `AmountFruits`, `PlantCode`
- Proprietà risorse: `Hydration`, `LightExposure`, `GrowthPoints`
- Proprietà condizioni: `ConditionLabel`, `ConditionScore`, `MoldRiskLevel`
- Proprietà sistemi: `WateringSystemOn`, `LedSystemState`, `FertilizerLevel`
- Proprietà livelli: `PlantLevel`, `CompletedCycles`
- Metodi: `PlantSeed`, `ResetToEmpty`, `IncrementCompletedCycle`, `GetPlantData`
- Implementata da: `PotStateModel`

**9. IPhSystem** (`Assets/_Project/Scripts/Core/Interfaces/IPhSystem.cs`)

- Proprietà: `CurrentPh`
- Metodi: `SetPh`, `ApplyInstantDelta`, `RegisterActionDrift`, `RegisterPlantDrift`, `RemoveActionContribution`, `RemovePlantContributions`, `EvaluateState`, `ApplyQueuedDrifts`, `GetContributions`
- Eventi: `OnPhChanged`
- Implementata da: `PhSystem`

**10. IPotActions** (`Assets/_Project/Scripts/Dome/Interfaces/IPotActions.cs`)

- Proprietà: `PotSlot`, `PotState`, `HasPlant`
- Metodi validazione: `CanPlant`, `CanWater`, `CanLight`, `CanHarvest`, `CanFertilize`, `CanPruning`, `CanApplyAdditive`
- Metodi esecuzione: `DoPlant`, `DoWater`, `DoLight`, `DoHarvest`, `DoFertilize`, `DoPruning`, `DoApplyAdditive`, `DoUproot`
- Metodi helper: `GetMaxHydration`, `GetMaxLightExposure`, `GetCurrentState`
- Implementata da: `PotActions`

### Fase 3: Support Systems Interfaces (Priorità Media)

**11. ISaveManager** (`Assets/_Project/Scripts/Core/Interfaces/ISaveManager.cs`)

- Metodi: `SaveGame`, `LoadGame`, `DeleteSave`, `SaveExists`, `GetSaveTimestamp`
- Implementata da: `SaveManager`

**12. IAssetManager** (`Assets/_Project/Scripts/Core/Interfaces/IAssetManager.cs`)

- Metodi: `LoadAsset<T>`, `LoadAllAssets<T>`, `ClearCache`, `RemoveFromCache`, `PreloadCriticalAssets`
- Implementata da: `AssetManager`

**13. IDiaryStatistics** (`Assets/_Project/Scripts/Core/Interfaces/IDiaryStatistics.cs`)

- Proprietà: `CryEarned`, `CrySpent`, `ActionsSpent`
- Metodi: `Reset` (se presente)
- Implementata da: `DiaryStatistics`

**14. IMissionManager** (`Assets/_Project/Scripts/Core/Interfaces/IMissionManager.cs`)

- Metodi: `GetMission`, `CompleteMission`, `IsMissionComplete` (da verificare API esatta)
- Implementata da: `MissionManager`

### Fase 4: Factory & Item Interfaces (Priorità Bassa)

**15. IItemFactory** (`Assets/_Project/Scripts/Core/Interfaces/IItemFactory.cs`)

- Metodi: `CreateItemByType`, `CreateItemWithQuality`
- Implementata da: `ItemFabric` (refactor da static a istanza)

## Piano di Implementazione

### Step 1: Creare Interfacce Core (30 minuti)

**File da creare:**

1. `Assets/_Project/Scripts/Core/Interfaces/IServiceContainer.cs`
2. `Assets/_Project/Scripts/Core/Interfaces/IGameManager.cs`
3. `Assets/_Project/Scripts/Core/Interfaces/IEconomySystem.cs`
4. `Assets/_Project/Scripts/Core/Interfaces/IActionSystem.cs`
5. `Assets/_Project/Scripts/Core/Interfaces/IDayCycleSystem.cs`
6. `Assets/_Project/Scripts/Core/Interfaces/ICondensationSystem.cs`
7. `Assets/_Project/Scripts/Core/Interfaces/IInventory.cs`

**Contenuto:** Definire solo le interfacce con metodi e proprietà pubbliche delle classi esistenti.

### Step 2: Far Implementare le Classi Core (20 minuti)

**File da modificare:**

1. `ServiceContainer.cs`: Aggiungere `: IServiceContainer`
2. `GameManager.cs`: Aggiungere `: IGameManager`
3. `EconomySystem.cs`: Aggiungere `: IEconomySystem`
4. `ActionSystem.cs`: Aggiungere `: IActionSystem`
5. `DayCycleSystem.cs`: Aggiungere `: IDayCycleSystem`
6. `CondensationSystem.cs`: Aggiungere `: ICondensationSystem`
7. `Inventory.cs`: Aggiungere `: IInventory`

**Modifica:** Solo aggiungere `: IInterfaccia` dopo il nome classe. Nessun altro cambiamento.

### Step 3: Creare Interfacce Dome (20 minuti)

**File da creare:**

1. `Assets/_Project/Scripts/Dome/Interfaces/IPotStateModel.cs`
2. `Assets/_Project/Scripts/Core/Interfaces/IPhSystem.cs`
3. `Assets/_Project/Scripts/Dome/Interfaces/IPotActions.cs`

**Nota:** Creare cartella `Assets/_Project/Scripts/Dome/Interfaces/` se non esiste.

### Step 4: Far Implementare Classi Dome (15 minuti)

**File da modificare:**

1. `PotStateModel.cs`: Aggiungere `: IPotStateModel`
2. `PhSystem.cs`: Aggiungere `: IPhSystem`
3. `PotActions.cs`: Aggiungere `: IPotActions`

### Step 5: Refactoring ServiceContainer per Supportare Interfacce (10 minuti)

**File da modificare:** `ServiceContainer.cs`

**Modifica:** Aggiungere overload `Register<TInterface, TImplementation>` per registrare implementazioni con interfacce:

```csharp
public void Register<TInterface, TImplementation>(TImplementation service) 
    where TImplementation : TInterface
{
    Register<TInterface>(service);
    Register<TImplementation>(service); // Registra anche come tipo concreto
}
```

### Step 6: Refactoring Graduale File che Usano Classi Concrete (2-3 ore)

**File prioritari da refactorizzare:**

1. **PotActions.cs** (30 minuti)

   - Cambiare `private GameManager _gameManager` → `private IGameManager _gameManager`
   - Cambiare `ServiceContainer.Instance.Get<GameManager>()` → `ServiceContainer.Instance.Get<IGameManager>()`
   - Cambiare `private PhSystem _phSystem` → `private IPhSystem _phSystem`
   - Cambiare `ServiceContainer.Instance.Get<PhSystem>()` → `ServiceContainer.Instance.Get<IPhSystem>()`

2. **DayCycleController.cs** (20 minuti)

   - Cambiare riferimenti a `GameManager` → `IGameManager`
   - Cambiare riferimenti a `PhSystem` → `IPhSystem`

3. **GameManager.cs** (15 minuti)

   - Cambiare `private ActionSystem _actionSystem` → `private IActionSystem _actionSystem`
   - Cambiare `private EconomySystem _economySystem` → `private IEconomySystem _economySystem`
   - Cambiare `private CondensationSystem _condensationSystem` → `private ICondensationSystem _condensationSystem`
   - Cambiare `private DayCycleSystem _dayCycleSystem` → `private IDayCycleSystem _dayCycleSystem`

4. **File UI principali** (1 ora)

   - `TopBarController.cs`: Cambiare `GameManager` → `IGameManager`
   - `HUDInventory.cs`: Cambiare `Inventory` → `IInventory`
   - `PotDetailsWidget.cs`: Cambiare `GameManager` → `IGameManager`
   - Altri file UI che usano `ServiceContainer.Instance.Get<GameManager>()`

5. **Altri file core** (30 minuti)

   - `SaveManager.cs`: Cambiare `GameManager` → `IGameManager`
   - `DayCycleSystem.cs`: Cambiare `GameManager` → `IGameManager` (se usa direttamente)
   - `DeteriorationSystem.cs`: Cambiare `GameManager` → `IGameManager`

### Step 7: Creare Interfacce Support Systems (15 minuti)

**File da creare:**

1. `Assets/_Project/Scripts/Core/Interfaces/ISaveManager.cs`
2. `Assets/_Project/Scripts/Core/Interfaces/IAssetManager.cs`
3. `Assets/_Project/Scripts/Core/Interfaces/IDiaryStatistics.cs`
4. `Assets/_Project/Scripts/Core/Interfaces/IMissionManager.cs`

### Step 8: Far Implementare Support Systems (10 minuti)

**File da modificare:**

1. `SaveManager.cs`: Aggiungere `: ISaveManager`
2. `AssetManager.cs`: Aggiungere `: IAssetManager`
3. `DiaryStatistics.cs`: Aggiungere `: IDiaryStatistics`
4. `MissionManager.cs`: Aggiungere `: IMissionManager`

### Step 9: Refactoring File che Usano Support Systems (30 minuti)

**File da modificare:**

- File che usano `SaveManager.Instance` → `ISaveManager`
- File che usano `AssetManager.Instance` → `IAssetManager`
- `ActionSystem.cs`: Cambiare `DiaryStatistics` → `IDiaryStatistics`
- `EconomySystem.cs`: Cambiare `DiaryStatistics` → `IDiaryStatistics`

### Step 10: Verifica e Test Compilazione (15 minuti)

**Azioni:**

1. Compilare progetto Unity
2. Verificare che non ci siano errori
3. Testare che il gioco si avvii correttamente
4. Verificare che le funzionalità principali funzionino

## Struttura File Finale

```
Assets/_Project/Scripts/
├── Core/
│   ├── Interfaces/
│   │   ├── IServiceContainer.cs (NUOVO)
│   │   ├── IGameManager.cs (NUOVO)
│   │   ├── IEconomySystem.cs (NUOVO)
│   │   ├── IActionSystem.cs (NUOVO)
│   │   ├── IDayCycleSystem.cs (NUOVO)
│   │   ├── ICondensationSystem.cs (NUOVO)
│   │   ├── IInventory.cs (NUOVO)
│   │   ├── IPhSystem.cs (NUOVO)
│   │   ├── ISaveManager.cs (NUOVO)
│   │   ├── IAssetManager.cs (NUOVO)
│   │   ├── IDiaryStatistics.cs (NUOVO)
│   │   ├── IMissionManager.cs (NUOVO)
│   │   ├── IGameSystem.cs (ESISTENTE)
│   │   └── IInventorySystem.cs (ESISTENTE)
│   ├── ServiceContainer.cs (MODIFICATO: implementa IServiceContainer)
│   ├── GameManager.cs (MODIFICATO: implementa IGameManager)
│   ├── EconomySystem.cs (MODIFICATO: implementa IEconomySystem)
│   ├── ActionSystem.cs (MODIFICATO: implementa IActionSystem)
│   ├── DayCycleSystem.cs (MODIFICATO: implementa IDayCycleSystem)
│   ├── CondensationSystem.cs (MODIFICATO: implementa ICondensationSystem)
│   ├── SaveManager.cs (MODIFICATO: implementa ISaveManager)
│   ├── AssetManager.cs (MODIFICATO: implementa IAssetManager)
│   └── ...
├── Dome/
│   ├── Interfaces/
│   │   ├── IPotStateModel.cs (NUOVO)
│   │   └── IPotActions.cs (NUOVO)
│   ├── PotStateModel.cs (MODIFICATO: implementa IPotStateModel)
│   ├── PotActions.cs (MODIFICATO: implementa IPotActions, usa interfacce)
│   └── ...
└── Core/
    └── PhSystem.cs (MODIFICATO: implementa IPhSystem)
```

## Note di Implementazione

### Pattern da Seguire

1. **Interfacce Minimali**: Esporre solo metodi/proprietà pubblici necessari per test
2. **Backward Compatibility**: Le classi continuano a funzionare identicamente
3. **Gradual Refactoring**: Refactorizzare un file alla volta, testando dopo ogni modifica
4. **ServiceContainer Enhancement**: Supportare registrazione per interfaccia E tipo concreto

### Esempio Implementazione Interfaccia

```csharp
// IServiceContainer.cs
namespace _Project.Sporae.Core
{
    public interface IServiceContainer
    {
        void Register<T>(T service);
        T Get<T>(bool suppressWarning = false);
        bool Contains(Type type);
        void RegisterGlobal<T>(T service);
        bool ContainsGlobal(Type type);
        event System.Action<object> OnServiceRegistered;
    }
}
```

### Esempio Refactoring Classe

```csharp
// ServiceContainer.cs - PRIMA
public class ServiceContainer : MonoBehaviour
{
    // ... codice esistente ...
}

// ServiceContainer.cs - DOPO
public class ServiceContainer : MonoBehaviour, IServiceContainer
{
    // ... stesso codice identico ...
}
```

### Esempio Refactoring Uso

```csharp
// PotActions.cs - PRIMA
private GameManager _gameManager;
_gameManager = ServiceContainer.Instance.Get<GameManager>();

// PotActions.cs - DOPO
private IGameManager _gameManager;
_gameManager = ServiceContainer.Instance.Get<IGameManager>();
```

## Verifica Finale

Dopo l'implementazione, verificare:

1. ✅ Tutte le interfacce create
2. ✅ Tutte le classi implementano le interfacce
3. ✅ ServiceContainer supporta registrazione per interfaccia
4. ✅ File principali refactorizzati per usare interfacce
5. ✅ Progetto compila senza errori
6. ✅ Gioco si avvia e funziona correttamente
7. ✅ Pronto per scrivere test unitari

## Benefici Ottenuti

Dopo l'implementazione completa:

- ✅ Test unitari isolati possibili (mock di tutte le dipendenze)
- ✅ Dependency injection completa
- ✅ Refactoring più sicuro (interfacce stabili)
- ✅ Testabilità migliorata (da 6.5/10 a 9/10)
- ✅ Codice più modulare e manutenibile