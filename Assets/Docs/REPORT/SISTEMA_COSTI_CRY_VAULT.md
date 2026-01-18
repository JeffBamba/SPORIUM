# SISTEMA COSTI CRY - VAULT E CICLO GIORNO

**Data Analisi:** 2026-01-17  
**Versione:** 1.0  
**Status:** ✅ Documentazione Completa

---

## 📋 INDICE

1. [Panoramica Sistema CRY](#panoramica-sistema-cry)
2. [Costi Fissi Giornalieri](#costi-fissi-giornalieri)
3. [Costi Operativi Vault (Fine Giornata)](#costi-operativi-vault-fine-giornata)
4. [Costi Azioni Manuali](#costi-azioni-manuali)
5. [Costi Speciali](#costi-speciali)
6. [Calcolo Totale Giornaliero](#calcolo-totale-giornaliero)
7. [Meccaniche di Calcolo](#meccaniche-di-calcolo)

---

## 🎯 PANORAMICA SISTEMA CRY

### **EconomySystem**
- **Classe:** `Assets/_Project/Scripts/Core/EconomySystem.cs`
- **Valuta:** CRY (Cryptocurrency)
- **Limite Massimo:** 999,999 CRY
- **Valore Iniziale:** 250 CRY (configurabile in `GameManager`)
- **Gestione:** Sistema centralizzato con eventi `OnCRYChanged`

### **Metodi Principali**
- `CanAfford(int amount)` - Verifica disponibilità
- `Add(int amount)` - Aggiunge CRY (con tracking statistiche)
- `Spend(int amount)` - Spende CRY (con tracking statistiche)
- `SetCRY(int amount)` - Imposta CRY (debug/admin)

---

## 💰 COSTI FISSI GIORNALIERI

### **1. End Day (Fine Giornata)**
- **Costo:** **20 CRY** (fisso)
- **Quando:** All'inizio del nuovo giorno (dopo fade to black)
- **Classe:** `DayCycleSystem.cs` → `GameManager.HandleDayChanged()`
- **Configurazione:** `GameManager._dailyPowerCost = 20`
- **Verifica:** `DayCycleSystem.CanEndDay()` controlla disponibilità prima di permettere fine giornata

**Codice:**
```csharp
// DayCycleSystem.cs
public int DailyPowerCost { get; set; } = 20;

// GameManager.cs - HandleDayChanged()
_economySystem.Spend(_dailyPowerCost); // 20 CRY
```

**⚠️ IMPORTANTE:** Questo costo viene dedotto **automaticamente** quando il giorno cambia. Se non hai abbastanza CRY, non puoi terminare il giorno.

---

## 🔧 COSTI OPERATIVI VAULT (FINE GIORNATA)

I costi operativi vengono calcolati e applicati **a fine giornata** durante `DayCycleController.HandleDayChanged()`.

### **1. Sistema di Irrigazione (Watering System)**

**Costo:** **2 CRY per vaso con sistema ON**

- **Quando:** A fine giornata, per ogni vaso con `WateringSystemOn = true`
- **Classe:** `SPOR-BLK-01-03A-DayCycleController.cs` → `ApplyWateringSystemEffects()`
- **Condizioni:**
  - Vaso deve avere pianta (`pot.HasPlant`)
  - Sistema irrigazione deve essere ON (`pot.WateringSystemOn`)
  - Deve avere WAT-RAW disponibile (0.5 per giorno, accumulato)
- **Fallback:** Se CRY insufficiente, il sistema continua a funzionare ma viene loggato un warning

**Codice:**
```csharp
// Linea 1293 - DayCycleController.cs
if (!_gameManager.TrySpendCry(2))
{
    SporiumLogger.LogWarning(LogCategory.Pot, 
        $"{pot.PotId}: CRY insufficiente per sistema irrigazione (richiesti 2)");
}
```

**Esempio:**
- 4 vasi con sistema ON = **8 CRY/giorno**

---

### **2. Sistema LED (Illuminazione)**

**Costo:** **Variabile in base a stato e giorni consecutivi**

#### **LED Blue (Blu)**
- **Formula:** `1 + (consecutiveDays / 2)`
- **Progressione:** 1, 1, 2, 2, 3, 3, 4, 4...
- **Esempio:**
  - Giorno 1: 1 CRY
  - Giorno 2: 1 CRY
  - Giorno 3: 2 CRY
  - Giorno 4: 2 CRY
  - Giorno 5: 3 CRY

#### **LED Red (Rosso)**
- **Formula:** `2 + consecutiveDays`
- **Progressione:** 2, 3, 4, 5, 6, 7...
- **Esempio:**
  - Giorno 1: 2 CRY
  - Giorno 2: 3 CRY
  - Giorno 3: 4 CRY
  - Giorno 4: 5 CRY
  - Giorno 5: 6 CRY

**Classe:** `SPOR-BLK-01-03A-DayCycleController.cs` → `GetNightlyCryCost()`

**Codice:**
```csharp
// Linea 1488-1499 - DayCycleController.cs
private int GetNightlyCryCost(LedSystemState state, int consecutiveDays)
{
    switch (state)
    {
        case LedSystemState.Blue:
            return 1 + (consecutiveDays / 2);  // 1, 1, 2, 2, 3...
        case LedSystemState.Red:
            return 2 + consecutiveDays;        // 2, 3, 4, 5... (più costoso)
        default:
            return 0;
    }
}
```

**⚠️ COMPORTAMENTO CRITICO:**
- Se CRY insufficiente, il sistema LED viene **automaticamente spento**
- Viene rimossa la contribuzione pH del LED
- Viene mostrata notifica: `"LGT-002: Sistema LED {potId} spento - CRY insufficiente"`

**Esempio Totale:**
- 2 vasi con LED Blue (giorno 3) = 2 × 2 = **4 CRY**
- 1 vaso con LED Red (giorno 5) = 1 × 7 = **7 CRY**
- **Totale LED:** 11 CRY/giorno

---

## 🎮 COSTI AZIONI MANUALI

Le azioni manuali sui vasi costano **1 CRY per azione** (oltre a 1 Azione giornaliera).

### **Azioni Base**
- **Plant (Piantare):** 1 CRY + 1 Azione
- **Water (Annaffiare):** 1 CRY + 1 Azione
- **Light (Illuminare):** 1 CRY + 1 Azione

**Classe:** `PotSystemConfig.cs` → `CostCryPerPotAction = 1`

**Codice:**
```csharp
// PotSystemConfig.cs - Linea 38
[SerializeField] public int CostCryPerPotAction = 1; // –1 CRY Dome action (GDD Blocking_01)

// PotActions.cs - Linea 1679-1682
private int GetCryCost()
{
    return config ? config.CostCryPerPotAction : 1;
}
```

**⚠️ NOTA:** Le azioni manuali consumano CRY **immediatamente** quando eseguite, non a fine giornata.

---

## 🧪 COSTI SPECIALI

### **1. Fertilizzanti**

I fertilizzanti hanno costi fissi e vengono applicati immediatamente:

| Tipo | Costo CRY | Effetto Fertilizzante |
|------|-----------|----------------------|
| **Standard** | 25 CRY | +25% |
| **Pure** | 75 CRY | +40% |
| **Prohibited** | 75 CRY | +40% |

**Classe:** `FertilizerSystem.cs`

**Codice:**
```csharp
// FertilizerSystem.cs - Linea 38-41
private const int COST_STANDARD = 25;   // CRY
private const int COST_PURE = 75;       // CRY
private const int COST_PROHIBITED = 75; // CRY
```

**⚠️ REGOLA CRITICA:** L'uso di fertilizzanti incompatibili con la famiglia della pianta causa **MORTE IMMEDIATA** della pianta!

---

### **2. Ascensore (Elevator)**

**Costo:** **5 CRY per utilizzo**

- **Classe:** `ElevatorSystem.cs`
- **Configurazione:** `[SerializeField] private int cryCost = 5;`
- **Quando:** Ogni volta che usi l'ascensore per cambiare livello

**Codice:**
```csharp
// ElevatorSystem.cs - Linea 18
[SerializeField] private int cryCost = 5;

// Linea 301
if (!gameManager.TrySpendCry(cryCost))
{
    SporiumLogger.LogWarning(LogCategory.Core, 
        $"Non hai abbastanza azioni o CRY per usare l'ascensore! (Costo: {cryCost})");
}
```

---

### **3. Minigioco Lab (LabMinigameExtractor)**

**Costo:** Variabile (configurabile per componente)
- **Azione:** Costo in azioni giornaliere
- **CRY:** Costo in CRY (solo se non hai già provato oggi)

**Classe:** `LabMinigameExtractor.cs`

---

## 📊 CALCOLO TOTALE GIORNALIERO

### **Formula Base:**
```
Costo Totale Giornaliero = 
    Costo Fisso End Day (20 CRY)
    + Costo Sistema Irrigazione (2 CRY × vasi ON)
    + Costo Sistema LED (variabile per vaso)
    + Costi Azioni Manuali (1 CRY × azioni eseguite)
    + Costi Speciali (fertilizzanti, ascensore, etc.)
```

### **Esempio Pratico - Scenario Tipico:**

**Setup:**
- 4 vasi con sistema irrigazione ON
- 2 vasi con LED Blue (giorno 3)
- 1 vaso con LED Red (giorno 2)
- 3 azioni manuali (Plant, Water, Light)
- 1 fertilizzante Standard

**Calcolo:**
```
Costo Fisso End Day:           20 CRY
Sistema Irrigazione (4×2):      8 CRY
LED Blue (2×2):                 4 CRY
LED Red (1×4):                  4 CRY
Azioni Manuali (3×1):           3 CRY
Fertilizzante Standard:       25 CRY
─────────────────────────────────────
TOTALE GIORNALIERO:            64 CRY
```

---

## ⚙️ MECCANICHE DI CALCOLO

### **1. Timing dei Costi**

#### **Costi Immediati:**
- Azioni manuali sui vasi (Plant, Water, Light)
- Fertilizzanti
- Ascensore
- Minigioco Lab

#### **Costi a Fine Giornata:**
- End Day (20 CRY) - **primo costo applicato**
- Sistema Irrigazione (2 CRY × vasi ON)
- Sistema LED (variabile per vaso)

**Ordine di Esecuzione (HandleDayChanged):**
```csharp
// GameManager.cs - Linea 165-177
private void HandleDayChanged(int day)
{   
    _economySystem.Spend(_dailyPowerCost); // 1. End Day (20 CRY)
    _actionSystem.ResetActions(_actionsPerDay);
    
    _condensationSystem.DayChanged();
    OnCondensationChanged?.Invoke(_condensationSystem.CondensationAmount);
}

// DayCycleController.cs - Linea 396-428
private void HandleDayChanged(int dayIndex)
{
    // 2. CheckWateringSystemResources()
    CheckWateringSystemResources();
    
    // 3. ResolveGrowthForAllPots(D)
    ResolveGrowthForAllPots(dayIndex);
    
    // 4. ApplyWateringSystemEffects() - Consumo 2 CRY per vaso ON
    ApplyWateringSystemEffects();
    
    // 5. ApplyLedSystemEffects() - Consumo variabile LED
    ApplyLedSystemEffects();
    
    // 6. Calcolo pH drift
    CalculateAndRegisterPhDrift(dayIndex);
    // ...
}
```

---

### **2. Verifica Disponibilità**

Tutti i costi verificano la disponibilità prima di essere applicati:

```csharp
// EconomySystem.cs - Linea 22-25
public bool CanAfford(int amount)
{
    return amount >= 0 && CurrentCRY >= amount;
}

// EconomySystem.cs - Linea 44-54
public bool Spend(int amount)
{
    if (!CanAfford(amount))
        return false;
    
    _diaryStatistics.CrySpent += amount;
    CurrentCRY -= amount;
    OnCRYChanged?.Invoke(CurrentCRY);
    return true;
}
```

---

### **3. Gestione Fallimenti**

#### **End Day:**
- Se CRY < 20, non puoi terminare il giorno
- `DayCycleSystem.CanEndDay()` ritorna `false`

#### **Sistema Irrigazione:**
- Se CRY insufficiente, il sistema continua ma viene loggato warning
- **NON** viene spento automaticamente

#### **Sistema LED:**
- Se CRY insufficiente, il sistema viene **automaticamente spento**
- Contributo pH viene rimosso
- Notifica all'utente

---

### **4. Tracking Statistiche**

Tutti i costi vengono tracciati in `DiaryStatistics`:

```csharp
// EconomySystem.cs - Linea 49
_diaryStatistics.CrySpent += amount;

// EconomySystem.cs - Linea 36
_diaryStatistics.CryEarned += amount;
```

Le statistiche sono consultabili nel Diary UI.

---

## 📝 NOTE IMPORTANTI

### **1. Costi Configurabili**
Molti costi sono configurabili tramite:
- `DifficultyCalibrationConfig` (runtime)
- `PotSystemConfig` (ScriptableObject)
- `GameManager` (Inspector Unity)

### **2. Limite Massimo CRY**
- **Max CRY:** 999,999
- Se raggiungi il limite, non puoi guadagnare più CRY (ma puoi ancora spenderli)

### **3. Valore Iniziale**
- **Default:** 250 CRY
- Configurabile in `GameManager._startingCRY`

### **4. Sistema di Eventi**
- `EconomySystem.OnCRYChanged` viene invocato ad ogni modifica
- UI si aggiorna automaticamente (TopBarController)

---

## 🔍 RIFERIMENTI CODICE

### **File Principali:**
- `Assets/_Project/Scripts/Core/EconomySystem.cs` - Sistema economico
- `Assets/_Project/Scripts/Core/DayCycleSystem.cs` - Sistema ciclo giorno
- `Assets/_Project/Scripts/Core/GameManager.cs` - Gestione centrale
- `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs` - Controller ciclo giorno
- `Assets/_Project/Scripts/Dome/PotSystemConfig.cs` - Configurazione costi vasi
- `Assets/_Project/Scripts/Dome/PotSystem/Fertilizer/FertilizerSystem.cs` - Sistema fertilizzanti
- `Assets/_Project/Scripts/World/Elevator/ElevatorSystem.cs` - Sistema ascensore
- `Assets/_Project/Scripts/Debug/DifficultyCalibrationConfig.cs` - Configurazione runtime

---

## ✅ RIEPILOGO COSTI

| Operazione | Costo CRY | Quando | Tipo |
|------------|-----------|--------|------|
| **End Day** | 20 | Fine giornata | Fisso |
| **Sistema Irrigazione** | 2 per vaso ON | Fine giornata | Per vaso |
| **LED Blue** | 1 + (giorni/2) | Fine giornata | Variabile |
| **LED Red** | 2 + giorni | Fine giornata | Variabile |
| **Azioni Vasi** | 1 per azione | Immediato | Per azione |
| **Fertilizzante Standard** | 25 | Immediato | Fisso |
| **Fertilizzante Pure** | 75 | Immediato | Fisso |
| **Fertilizzante Prohibited** | 75 | Immediato | Fisso |
| **Ascensore** | 5 | Immediato | Per utilizzo |

---

**FINE DOCUMENTAZIONE**
