# 📋 PIANO MIGLIORAMENTO TESTABILITÀ
## SPORIUM - Roadmap Completa per Testabilità 9/10

**Versione:** 1.0  
**Data Creazione:** 2025-01-XX  
**Obiettivo:** Migliorare testabilità da **7.5/10** a **9/10**  
**Stato:** 📋 **PIANIFICATO** - Da implementare

---

## 🎯 OBIETTIVO FINALE

### Stato Attuale
- ⚠️ **Testabilità:** 7.5/10
- ❌ **Test Unitari:** 0
- ❌ **Test Coverage:** 0%
- ⚠️ **Dipendenze Hardcoded:** Presenti
- ⚠️ **Mocking:** Impossibile

### Stato Obiettivo
- ✅ **Testabilità:** 9/10
- ✅ **Test Unitari:** 50+ test cases
- ✅ **Test Coverage:** >80% sistemi core
- ✅ **Dependency Injection:** Completa
- ✅ **Mocking:** Facilmente implementabile

---

## 📊 ANALISI PROBLEMI ATTUALE

### 1. Dipendenze Hardcoded

**Problema:**
- `ServiceContainer.Instance.Get<T>()` direttamente nei costruttori
- Impossibile sostituire con mock durante i test
- Accoppiamento forte a Unity runtime

**File Affetti:**
- `EconomySystem.cs` → `DiaryStatistics` hardcoded
- `ActionSystem.cs` → `DiaryStatistics` hardcoded
- `CondensationSystem.cs` → `Resources.Load` hardcoded
- `Inventory.cs` → `ItemFabric.CreateItemByType()` statico

**Impatto:** 🔴 **ALTO** - Blocca completamente il testing

---

### 2. Classi Concrete invece di Interfacce

**Problema:**
- `EconomySystem`, `ActionSystem` sono classi concrete
- Impossibile sostituire con mock durante i test
- Difficile testare isolatamente

**File Affetti:**
- Tutti i sistemi core (`EconomySystem`, `ActionSystem`, `CondensationSystem`)
- Nessuna interfaccia pubblica disponibile

**Impatto:** 🔴 **ALTO** - Impossibile isolare unità di codice

---

### 3. MonoBehaviour Dependency

**Problema:**
- `GameManager` è MonoBehaviour
- Richiede Unity runtime per testare
- Logica di gioco mescolata con Unity lifecycle

**File Affetti:**
- `GameManager.cs` → Logica mescolata con `Awake()`, `Start()`
- `PotActions.cs` → Dipende da MonoBehaviour
- Altri componenti Unity

**Impatto:** 🟡 **MEDIO** - Richiede test PlayMode invece di EditMode

---

### 4. Static Dependencies

**Problema:**
- `ItemFabric.CreateItemByType()` è statico
- `PlantDatabase.Instance` è singleton
- Impossibile mockare

**File Affetti:**
- `ItemFabric.cs` → Metodi statici
- `PlantDatabase.cs` → Singleton pattern
- `AssetManager.cs` → Singleton pattern

**Impatto:** 🟡 **MEDIO** - Limita testabilità ma non blocca completamente

---

### 5. Nessun Test Framework

**Problema:**
- Nessun test unitario presente
- Nessun setup per testing
- Nessuna struttura test

**Impatto:** 🔴 **ALTO** - Nessuna base per iniziare

---

## ✅ SOLUZIONI PROPOSTE

### Fase 1: Creare Interfacce (1 settimana)

#### 1.1 Interfacce Sistemi Core

**File da Creare:**
- `Assets/_Project/Scripts/Core/Interfaces/IEconomySystem.cs`
- `Assets/_Project/Scripts/Core/Interfaces/IActionSystem.cs`
- `Assets/_Project/Scripts/Core/Interfaces/ICondensationSystem.cs`
- `Assets/_Project/Scripts/Core/Interfaces/IStatisticsTracker.cs`

**Contenuto Interfacce:**

```csharp
// IEconomySystem.cs
public interface IEconomySystem
{
    int CurrentCRY { get; }
    bool CanAfford(int amount);
    bool Add(int amount);
    bool Spend(int amount);
    event Action<int> OnCRYChanged;
}

// IActionSystem.cs
public interface IActionSystem
{
    int ActionsLeft { get; }
    int MaxActions { get; }
    bool CanSpendAction(int amount);
    bool SpendAction(int amount);
    void ResetActions(int specificAmount);
    event Action<int> OnActionsChanged;
}

// ICondensationSystem.cs
public interface ICondensationSystem
{
    float CondensationAmount { get; }
    float GetMax();
    void DayChanged();
    void Reset();
}

// IStatisticsTracker.cs
public interface IStatisticsTracker
{
    int CryEarned { get; set; }
    int CrySpent { get; set; }
    int ActionsSpent { get; set; }
}
```

**Priorità:** 🔴 **ALTA** - Base per tutto il resto

---

#### 1.2 Interfacce Factory e Manager

**File da Creare:**
- `Assets/_Project/Scripts/Core/Interfaces/IItemFactory.cs`
- `Assets/_Project/Scripts/Core/Interfaces/IAssetManager.cs`
- `Assets/_Project/Scripts/Core/Interfaces/IServiceContainer.cs`

**Contenuto Interfacce:**

```csharp
// IItemFactory.cs
public interface IItemFactory
{
    Item CreateItemByType(string typeId);
}

// IAssetManager.cs
public interface IAssetManager
{
    T LoadAsset<T>(string path) where T : ScriptableObject;
    T[] LoadAllAssets<T>(string path) where T : ScriptableObject;
}

// IServiceContainer.cs
public interface IServiceContainer
{
    void Register<T>(T service);
    T Get<T>();
    bool Contains<T>();
    event Action<object> OnServiceRegistered;
}
```

**Priorità:** 🔴 **ALTA** - Necessarie per dependency injection

---

### Fase 2: Refactor Sistemi Esistenti (2 settimane)

#### 2.1 Implementare Interfacce

**File da Modificare:**
- `EconomySystem.cs` → Implementa `IEconomySystem`
- `ActionSystem.cs` → Implementa `IActionSystem`
- `CondensationSystem.cs` → Implementa `ICondensationSystem`
- `DiaryStatistics.cs` → Implementa `IStatisticsTracker`
- `ItemFabric.cs` → Implementa `IItemFactory` (rimuovere static)
- `AssetManager.cs` → Implementa `IAssetManager`
- `ServiceContainer.cs` → Implementa `IServiceContainer`

**Modifiche Richieste:**

```csharp
// Prima
public class EconomySystem
{
    // ...
}

// Dopo
public class EconomySystem : IEconomySystem
{
    // ... stessa implementazione ...
}
```

**Priorità:** 🔴 **ALTA** - Abilita dependency injection

---

#### 2.2 Refactor Costruttori per Dependency Injection

**File da Modificare:**
- `EconomySystem.cs`
- `ActionSystem.cs`
- `CondensationSystem.cs`
- `Inventory.cs`
- `ItemFabric.cs`

**Modifiche Richieste:**

```csharp
// Prima
public EconomySystem(int startingCRY)
{
    _diaryStatistics = ServiceContainer.Instance.Get<DiaryStatistics>();
    CurrentCRY = Math.Max(0, startingCRY);
}

// Dopo
public EconomySystem(
    int startingCRY, 
    IStatisticsTracker statisticsTracker = null,
    IServiceContainer serviceContainer = null)
{
    serviceContainer = serviceContainer ?? ServiceContainer.Instance;
    
    _statisticsTracker = statisticsTracker ?? 
        serviceContainer?.Get<IStatisticsTracker>() ??
        serviceContainer?.Get<DiaryStatistics>();
    
    CurrentCRY = Math.Max(0, startingCRY);
}
```

**Priorità:** 🔴 **ALTA** - Abilita mocking nei test

---

#### 2.3 Rimuovere Static da Factory

**File da Modificare:**
- `ItemFabric.cs`

**Modifiche Richieste:**

```csharp
// Prima
public static class ItemFabric
{
    public static Item CreateItemByType(string typeId) { ... }
}

// Dopo
public class ItemFabric : IItemFactory
{
    private readonly IAssetManager _assetManager;
    
    public ItemFabric(IAssetManager assetManager = null)
    {
        _assetManager = assetManager ?? AssetManager.Instance;
    }
    
    public Item CreateItemByType(string typeId) { ... }
}
```

**Priorità:** 🟡 **MEDIA** - Migliora testabilità ma non bloccante

---

### Fase 3: Separare Logica da MonoBehaviour (1 settimana)

#### 3.1 Creare GameLogic Classe Pura

**File da Creare:**
- `Assets/_Project/Scripts/Core/GameLogic.cs`

**Contenuto:**

```csharp
namespace Sporae.Core
{
    /// <summary>
    /// Logica di gioco pura, separata da Unity MonoBehaviour.
    /// Testabile senza Unity runtime.
    /// </summary>
    public class GameLogic
    {
        private readonly IActionSystem _actionSystem;
        private readonly IEconomySystem _economySystem;
        private readonly ICondensationSystem _condensationSystem;
        
        public GameLogic(
            IActionSystem actionSystem,
            IEconomySystem economySystem,
            ICondensationSystem condensationSystem)
        {
            _actionSystem = actionSystem;
            _economySystem = economySystem;
            _condensationSystem = condensationSystem;
        }
        
        public bool TrySpendAction(int amount = 0) { ... }
        public bool TrySpendCry(int amount) { ... }
        public bool TrySpendActionAndCry(int amountAction, int amountCry) { ... }
        public float CollectCondensation() { ... }
    }
}
```

**Priorità:** 🟡 **MEDIA** - Migliora testabilità logica pura

---

#### 3.2 Refactor GameManager come Wrapper

**File da Modificare:**
- `GameManager.cs`

**Modifiche Richieste:**

```csharp
public class GameManager : MonoBehaviour
{
    private GameLogic _gameLogic;
    private ActionSystem _actionSystem;
    private EconomySystem _economySystem;
    
    // Proprietà per compatibilità
    public bool TrySpendAction(int amount = 0) => 
        _gameLogic?.TrySpendAction(amount) ?? false;
    
    private void Awake()
    {
        ServiceContainer.Init();
        InitializeSystems();
        
        // Crea GameLogic con sistemi reali
        _gameLogic = new GameLogic(_actionSystem, _economySystem, _condensationSystem);
    }
}
```

**Priorità:** 🟡 **MEDIA** - Migliora testabilità ma non critico

---

### Fase 4: Setup Test Framework (1 settimana)

#### 4.1 Installare Unity Test Framework

**Azioni:**
1. Aprire Package Manager
2. Installare "Unity Test Framework" (com.unity.test-framework)
3. Verificare installazione

**Priorità:** 🔴 **ALTA** - Base per tutti i test

---

#### 4.2 Creare Struttura Cartelle Test

**Struttura da Creare:**

```
Assets/
├── Tests/
│   ├── EditMode/
│   │   ├── Core/
│   │   │   ├── EconomySystemTests.cs
│   │   │   ├── ActionSystemTests.cs
│   │   │   ├── InventoryTests.cs
│   │   │   ├── CondensationSystemTests.cs
│   │   │   └── GameLogicTests.cs
│   │   ├── Helpers/
│   │   │   ├── TestBase.cs
│   │   │   ├── MockServiceContainer.cs
│   │   │   ├── MockStatisticsTracker.cs
│   │   │   ├── MockItemFactory.cs
│   │   │   └── MockAssetManager.cs
│   │   └── Utils/
│   │       └── TestHelpers.cs
│   └── PlayMode/
│       └── Integration/
│           └── GameManagerIntegrationTests.cs
```

**Priorità:** 🔴 **ALTA** - Organizzazione test

---

#### 4.3 Creare Mock Helpers

**File da Creare:**

**MockServiceContainer.cs:**
```csharp
namespace Sporae.Tests.Helpers
{
    public class MockServiceContainer : IServiceContainer
    {
        private Dictionary<Type, object> _services = new();
        public event Action<object> OnServiceRegistered;
        
        public void Register<T>(T service) { ... }
        public T Get<T>() { ... }
        public bool Contains<T>() { ... }
    }
}
```

**MockStatisticsTracker.cs:**
```csharp
namespace Sporae.Tests.Helpers
{
    public class MockStatisticsTracker : IStatisticsTracker
    {
        public int CryEarned { get; set; }
        public int CrySpent { get; set; }
        public int ActionsSpent { get; set; }
    }
}
```

**TestBase.cs:**
```csharp
namespace Sporae.Tests
{
    public abstract class TestBase
    {
        protected MockServiceContainer MockServiceContainer { get; private set; }
        
        [SetUp]
        public virtual void SetUp()
        {
            MockServiceContainer = new MockServiceContainer();
        }
        
        [TearDown]
        public virtual void TearDown()
        {
            MockServiceContainer = null;
        }
    }
}
```

**Priorità:** 🔴 **ALTA** - Base per tutti i test

---

### Fase 5: Scrivere Test Unitari (2 settimane)

#### 5.1 Test EconomySystem

**File da Creare:**
- `Assets/Tests/EditMode/Core/EconomySystemTests.cs`

**Test Cases da Implementare:**
1. ✅ `Add_ValidAmount_IncreasesCRY`
2. ✅ `Add_NegativeAmount_ReturnsFalse`
3. ✅ `Add_ZeroAmount_ReturnsFalse`
4. ✅ `Spend_ValidAmount_DecreasesCRY`
5. ✅ `Spend_InsufficientCRY_ReturnsFalse`
6. ✅ `CanAfford_SufficientCRY_ReturnsTrue`
7. ✅ `CanAfford_InsufficientCRY_ReturnsFalse`
8. ✅ `Add_UpdatesStatisticsTracker`
9. ✅ `Spend_UpdatesStatisticsTracker`
10. ✅ `Add_MaxCRY_ClampsToMax`

**Target:** 10+ test cases

**Priorità:** 🔴 **ALTA** - Sistema core critico

---

#### 5.2 Test ActionSystem

**File da Creare:**
- `Assets/Tests/EditMode/Core/ActionSystemTests.cs`

**Test Cases da Implementare:**
1. ✅ `SpendAction_ValidAmount_DecreasesActions`
2. ✅ `SpendAction_InsufficientActions_ReturnsFalse`
3. ✅ `CanSpendAction_SufficientActions_ReturnsTrue`
4. ✅ `ResetActions_SetsActionsToMax`
5. ✅ `AddActions_IncreasesActions`
6. ✅ `AddActions_MaxActions_ClampsToMax`
7. ✅ `GetActionPercentage_CalculatesCorrectly`
8. ✅ `SpendAction_UpdatesStatisticsTracker`
9. ✅ `SpendAction_InvokesOnActionsChanged`
10. ✅ `RestoreState_SetsCorrectState`

**Target:** 10+ test cases

**Priorità:** 🔴 **ALTA** - Sistema core critico

---

#### 5.3 Test Inventory

**File da Creare:**
- `Assets/Tests/EditMode/Core/InventoryTests.cs`

**Test Cases da Implementare:**
1. ✅ `Add_ValidTypeId_AddsItem`
2. ✅ `Add_MultipleItems_IncreasesQuantity`
3. ✅ `Has_ExistingItem_ReturnsTrue`
4. ✅ `Has_NonExistingItem_ReturnsFalse`
5. ✅ `Consume_ValidItem_DecreasesQuantity`
6. ✅ `Consume_InsufficientQuantity_ReturnsFalse`
7. ✅ `Consume_LastItem_RemovesFromInventory`
8. ✅ `Clear_RemovesAllItems`
9. ✅ `IsEmpty_EmptyInventory_ReturnsTrue`
10. ✅ `Add_InvokesOnInventoryChanged`

**Target:** 10+ test cases

**Priorità:** 🟡 **MEDIA** - Sistema importante ma non critico

---

#### 5.4 Test CondensationSystem

**File da Creare:**
- `Assets/Tests/EditMode/Core/CondensationSystemTests.cs`

**Test Cases da Implementare:**
1. ✅ `DayChanged_IncreasesCondensation`
2. ✅ `DayChanged_MaxCondensation_ClampsToMax`
3. ✅ `Reset_SetsToZero`
4. ✅ `GetMax_ReturnsConfigMax`
5. ✅ `CondensationAmount_Initial_IsZero`

**Target:** 5+ test cases

**Priorità:** 🟡 **MEDIA** - Sistema semplice

---

#### 5.5 Test GameLogic

**File da Creare:**
- `Assets/Tests/EditMode/Core/GameLogicTests.cs`

**Test Cases da Implementare:**
1. ✅ `TrySpendAction_ValidAmount_ReturnsTrue`
2. ✅ `TrySpendAction_InsufficientActions_ReturnsFalse`
3. ✅ `TrySpendCry_ValidAmount_ReturnsTrue`
4. ✅ `TrySpendCry_InsufficientCRY_ReturnsFalse`
5. ✅ `TrySpendActionAndCry_BothValid_ReturnsTrue`
6. ✅ `TrySpendActionAndCry_InsufficientActions_ReturnsFalse`
7. ✅ `TrySpendActionAndCry_InsufficientCRY_ReturnsFalse`
8. ✅ `CollectCondensation_ReturnsAmount`
9. ✅ `CollectCondensation_ResetsCondensation`
10. ✅ `GetMaxCondensation_ReturnsMax`

**Target:** 10+ test cases

**Priorità:** 🔴 **ALTA** - Logica core del gioco

---

#### 5.6 Test Integrazione

**File da Creare:**
- `Assets/Tests/PlayMode/Integration/GameManagerIntegrationTests.cs`

**Test Cases da Implementare:**
1. ✅ `GameManager_InitializesCorrectly`
2. ✅ `GameManager_RegistersInServiceContainer`
3. ✅ `GameManager_SystemsAreInitialized`
4. ✅ `GameManager_EndDay_ResetsActions`

**Target:** 5+ test cases

**Priorità:** 🟡 **MEDIA** - Test integrazione

---

## 📅 TIMELINE IMPLEMENTAZIONE

### Settimana 1: Interfacce
- **Giorno 1-2:** Creare interfacce sistemi core
- **Giorno 3-4:** Creare interfacce factory e manager
- **Giorno 5:** Review e documentazione

**Deliverable:** Tutte le interfacce create e documentate

---

### Settimana 2-3: Refactor Sistemi
- **Giorno 1-3:** Implementare interfacce nelle classi esistenti
- **Giorno 4-6:** Refactor costruttori per dependency injection
- **Giorno 7-10:** Rimuovere static da factory
- **Giorno 11-12:** Testing manuale e fix bug
- **Giorno 13-14:** Review e documentazione

**Deliverable:** Tutti i sistemi refactorizzati con dependency injection

---

### Settimana 4: Separare Logica
- **Giorno 1-2:** Creare GameLogic classe pura
- **Giorno 3-4:** Refactor GameManager come wrapper
- **Giorno 5:** Testing manuale e fix bug

**Deliverable:** Logica separata da MonoBehaviour

---

### Settimana 5: Setup Test Framework
- **Giorno 1:** Installare Unity Test Framework
- **Giorno 2:** Creare struttura cartelle test
- **Giorno 3-4:** Creare mock helpers
- **Giorno 5:** Creare TestBase classe

**Deliverable:** Framework test pronto per uso

---

### Settimana 6-7: Scrivere Test
- **Giorno 1-2:** Test EconomySystem (10+ test)
- **Giorno 3-4:** Test ActionSystem (10+ test)
- **Giorno 5-6:** Test Inventory (10+ test)
- **Giorno 7-8:** Test CondensationSystem (5+ test)
- **Giorno 9-10:** Test GameLogic (10+ test)
- **Giorno 11-12:** Test integrazione (5+ test)
- **Giorno 13-14:** Review, fix e documentazione

**Deliverable:** 50+ test unitari funzionanti

---

## 📊 METRICHE SUCCESSO

### Metriche Quantitative

| Metrica | Prima | Dopo | Target |
|---------|-------|------|--------|
| **Testabilità Score** | 7.5/10 | 9/10 | ✅ |
| **Test Unitari** | 0 | 50+ | ✅ |
| **Test Coverage** | 0% | >80% | ✅ |
| **Interfacce** | 2 | 8+ | ✅ |
| **Dipendenze Hardcoded** | 10+ | 0 | ✅ |
| **Classi Mockabili** | 0% | 100% | ✅ |

### Metriche Qualitative

- ✅ Tutti i sistemi core hanno interfacce
- ✅ Tutti i costruttori accettano dipendenze opzionali
- ✅ Logica pura separata da MonoBehaviour
- ✅ Test framework configurato e funzionante
- ✅ Mock helpers disponibili per tutti i sistemi
- ✅ Test coverage >80% per sistemi core

---

## ⚠️ RISCHI E MITIGAZIONI

### Rischio 1: Breaking Changes

**Problema:** Refactoring potrebbe rompere codice esistente

**Mitigazione:**
- Mantenere fallback a ServiceContainer per compatibilità
- Testare manualmente dopo ogni fase
- Implementare gradualmente

**Probabilità:** 🟡 Media  
**Impatto:** 🔴 Alto

---

### Rischio 2: Tempo Sottostimato

**Problema:** Refactoring potrebbe richiedere più tempo del previsto

**Mitigazione:**
- Buffer del 20% nel timeline
- Priorità su sistemi critici
- Fasi incrementali

**Probabilità:** 🟡 Media  
**Impatto:** 🟡 Medio

---

### Rischio 3: Complessità Test

**Problema:** Alcuni test potrebbero essere complessi da scrivere

**Mitigazione:**
- Iniziare con test semplici
- Usare mock helpers
- Documentare pattern di test

**Probabilità:** 🟢 Bassa  
**Impatto:** 🟡 Medio

---

## 📋 CHECKLIST IMPLEMENTAZIONE

### Fase 1: Interfacce
- [ ] Creare `IEconomySystem`
- [ ] Creare `IActionSystem`
- [ ] Creare `ICondensationSystem`
- [ ] Creare `IStatisticsTracker`
- [ ] Creare `IItemFactory`
- [ ] Creare `IAssetManager`
- [ ] Creare `IServiceContainer`
- [ ] Documentare tutte le interfacce

### Fase 2: Refactor Sistemi
- [ ] `EconomySystem` implementa `IEconomySystem`
- [ ] `ActionSystem` implementa `IActionSystem`
- [ ] `CondensationSystem` implementa `ICondensationSystem`
- [ ] `DiaryStatistics` implementa `IStatisticsTracker`
- [ ] `ItemFabric` implementa `IItemFactory`
- [ ] `AssetManager` implementa `IAssetManager`
- [ ] `ServiceContainer` implementa `IServiceContainer`
- [ ] Refactor costruttori per dependency injection
- [ ] Rimuovere static da `ItemFabric`
- [ ] Testing manuale completo

### Fase 3: Separare Logica
- [ ] Creare `GameLogic` classe pura
- [ ] Refactor `GameManager` come wrapper
- [ ] Testing manuale completo

### Fase 4: Setup Test Framework
- [ ] Installare Unity Test Framework
- [ ] Creare struttura cartelle test
- [ ] Creare `MockServiceContainer`
- [ ] Creare `MockStatisticsTracker`
- [ ] Creare `MockItemFactory`
- [ ] Creare `MockAssetManager`
- [ ] Creare `TestBase` classe

### Fase 5: Scrivere Test
- [ ] Test `EconomySystem` (10+ test)
- [ ] Test `ActionSystem` (10+ test)
- [ ] Test `Inventory` (10+ test)
- [ ] Test `CondensationSystem` (5+ test)
- [ ] Test `GameLogic` (10+ test)
- [ ] Test integrazione `GameManager` (5+ test)
- [ ] Review e fix test
- [ ] Documentazione test

---

## 🎯 RISULTATO FINALE ATTESO

### Prima (Attuale)
- ⚠️ Testabilità: **7.5/10**
- ❌ Test Unitari: **0**
- ❌ Test Coverage: **0%**
- ❌ Dipendenze Hardcoded: **Presenti**
- ❌ Mocking: **Impossibile**

### Dopo (Obiettivo)
- ✅ Testabilità: **9/10**
- ✅ Test Unitari: **50+**
- ✅ Test Coverage: **>80% sistemi core**
- ✅ Dependency Injection: **Completa**
- ✅ Mocking: **Facilmente implementabile**

---

## 📚 RISORSE E RIFERIMENTI

### Documentazione Unity
- [Unity Test Framework](https://docs.unity3d.com/Packages/com.unity.test-framework@1.1/manual/index.html)
- [NUnit Documentation](https://docs.nunit.org/)

### Pattern Utilizzati
- **Dependency Injection**: Iniettare dipendenze tramite costruttore
- **Interface Segregation**: Interfacce piccole e specifiche
- **Factory Pattern**: Creazione oggetti tramite interfacce
- **Mock Objects**: Oggetti fake per testing
- **Separation of Concerns**: Separare logica da framework

### Best Practices
- Test isolati e indipendenti
- Arrange-Act-Assert pattern
- Mock solo dipendenze esterne
- Test nomi descrittivi
- Un test = un comportamento

---

## 📝 NOTE IMPLEMENTAZIONE

### Compatibilità Retroattiva

**Importante:** Tutti i refactoring devono mantenere compatibilità con codice esistente.

**Strategia:**
- Fallback a ServiceContainer se dipendenza non fornita
- Parametri opzionali nei costruttori
- Implementazioni default per interfacce

### Ordine di Implementazione

**Raccomandato:**
1. Creare interfacce PRIMA di refactorare
2. Refactorare un sistema alla volta
3. Testare manualmente dopo ogni refactor
4. Scrivere test DOPO refactoring completo

### Testing Incrementale

**Approccio:**
- Testare manualmente dopo ogni fase
- Scrivere test per sistema refactorizzato
- Non aspettare fine implementazione per testare

---

## 🚀 PROSSIMI PASSI

1. **Review Piano** → Approvare piano con team
2. **Allocare Risorse** → Assegnare sviluppatori
3. **Setup Ambiente** → Preparare branch Git
4. **Iniziare Fase 1** → Creare interfacce
5. **Tracking Progress** → Aggiornare checklist settimanalmente

---

**Ultimo Aggiornamento:** 2025-01-XX  
**Versione:** 1.0  
**Stato:** 📋 **PIANIFICATO** - Pronto per implementazione

---

## 📎 ALLEGATI

### Esempi Codice Completi

Vedi file separato: `GUIDA_MIGLIORAMENTO_TESTABILITA.md` per esempi dettagliati di codice.

### Template Test

Vedi file separato: `TEMPLATE_TEST_UNITARI.md` per template di test da seguire.

---

**Fine Documento**

