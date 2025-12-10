# 📋 PIANO IMPLEMENTAZIONE: SISTEMA LED PERSISTENTE
## Migrazione da Click Giornaliero a Toggle Persistente

**Data Creazione:** 2025-01-XX  
**Versione:** 1.0  
**BLK Code:** BLK-02.07  
**Status:** 📝 PIANO - Pronto per Implementazione  
**Sviluppatore:** Senior Developer Mode (AI Assistant)

---

## 🎯 OBIETTIVO

Migrare il sistema LED da **azione click giornaliera** a **sistema persistente toggle** (Off/Blue/Red), allineandolo al pattern del sistema di irrigazione (GDD AZ-11) già implementato.

### **Cambiamento Architetturale**

**DA (Sistema Attuale):**
- Click giornaliero → `DoLight(LedType?)` → effetto immediato
- Consumo: 1 Azione + 1 CRY + effetto pH istantaneo
- Traccia: `LastLitDay`, `LastLedType`
- Verifica requisiti: `IsLedRequirementMet(LastLedType)`

**A (Sistema Proposto):**
- Toggle persistente → `DoLight(LedSystemState?)` → configurazione stato
- Consumo: 1 Azione per toggle + consumo CRY notturno + effetti a fine giornata
- Traccia: `LedSystemState`, `DaysLedBlueConsecutive`, `DaysLedRedConsecutive`
- Verifica requisiti: `IsLedRequirementMet(LedSystemState)` (stato corrente, non storico)

---

## 📊 ANALISI DIPENDENZE

### **File Coinvolti (20 file totali)**

#### **🔴 CRITICI (Modifiche Strutturali)**
1. `Assets/_Project/Scripts/Dome/PotSystem/Growth/LedType.cs`
   - **Azione:** Estendere enum o creare nuovo enum `LedSystemState`
   - **Rischio:** Medio (breaking change per serializzazione)

2. `Assets/_Project/Scripts/Dome/PotStateModel.cs`
   - **Azione:** Aggiungere campi persistenti LED
   - **Rischio:** Alto (breaking change salvataggi)

3. `Assets/_Project/Scripts/Dome/PotActions.cs`
   - **Azione:** Riscrivere `DoLight()` come toggle
   - **Rischio:** Alto (tutti i riferimenti devono essere aggiornati)

4. `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`
   - **Azione:** Calcolo effetti LED a fine giornata
   - **Rischio:** Medio (logica crescita esistente)

5. `Assets/_Project/Scripts/Core/SaveManager.cs`
   - **Azione:** Salvataggio/caricamento nuovi campi
   - **Rischio:** Alto (migrazione salvataggi esistenti)

#### **🟡 IMPORTANTI (Modifiche Logiche)**
6. `Assets/_Project/Scripts/Core/PhSystem.cs`
   - **Azione:** Modificare `RegisterActionDrift()` per LED persistente
   - **Rischio:** Basso (già supporta azioni)

7. `Assets/_Project/Scripts/Dome/PotSystem/Growth/StageRequirements.cs`
   - **Azione:** Modificare `IsLedRequirementMet()` per stato corrente
   - **Rischio:** Medio (requisiti crescita)

8. `Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs`
   - **Azione:** Aggiornare UI per toggle (3 stati)
   - **Rischio:** Basso (UI esistente)

9. `Assets/_Project/Scripts/UI/VaultMap/PotHUDWidget.cs`
   - **Azione:** Aggiornare UI per toggle
   - **Rischio:** Basso (UI esistente)

10. `Assets/_Project/Scripts/Dev/GrowthDebugHotkeys.cs`
    - **Azione:** Aggiornare hotkey L per toggle
    - **Rischio:** Basso (solo DEV)

#### **🟢 MINORI (Documentazione/Test)**
11. `Assets/_Project/Editor/PopulateStageRequirements.cs` (Editor tool)
12. `Assets/Resources/Plants/*.asset` (Asset piante - nessuna modifica)
13. File di documentazione vari

---

## 🔍 ANALISI BREAKING CHANGES

### **1. Struttura Dati PotStateModel**

**Problema:** Aggiunta nuovi campi serializzabili

**Soluzione:**
- Aggiungere campi con default values
- Implementare migrazione in `SaveManager` per salvataggi vecchi
- Mantenere `LastLedType` per compatibilità temporanea

**Codice Proposto:**
```csharp
[Header("LED System (BLK-02.07 - Persistent Toggle)")]
[Tooltip("Stato sistema LED: Off, Blue, Red")]
public LedSystemState LedSystemState = LedSystemState.Off;
[Tooltip("Giorni consecutivi con BLUE LED attivo")]
public int DaysLedBlueConsecutive = 0;
[Tooltip("Giorni consecutivi con RED LED attivo")]
public int DaysLedRedConsecutive = 0;

// COMPATIBILITÀ: Mantenere LastLedType temporaneamente
[Header("LED Tracking (BLK-02.03 - Legacy, da deprecare)")]
[Tooltip("DEPRECATO: Usare LedSystemState. Mantenuto per compatibilità salvataggi")]
public LedType? LastLedType;  // Null se mai usato LED, Blue o Red se usato
public int LastLitDay;        // DEPRECATO: Usare DaysLedBlueConsecutive/DaysLedRedConsecutive
```

### **2. Metodo DoLight() Signature**

**Problema:** Cambio signature da `DoLight(LedType?)` a `DoLight(LedSystemState?)`

**Soluzione:**
- Creare overload per compatibilità temporanea
- Deprecare vecchio metodo con `[Obsolete]`
- Aggiornare tutti i riferimenti gradualmente

**Codice Proposto:**
```csharp
/// <summary>
/// DEPRECATO (BLK-02.07): Usare DoLight(LedSystemState?) invece
/// </summary>
[Obsolete("Usare DoLight(LedSystemState?) per nuovo sistema persistente")]
public bool DoLight(LedType? ledType = null)
{
    // Migrazione automatica: converti LedType a LedSystemState
    LedSystemState? newState = null;
    if (ledType.HasValue)
    {
        newState = ledType.Value == LedType.Blue ? LedSystemState.Blue : LedSystemState.Red;
    }
    return DoLight(newState);
}

/// <summary>
/// BLK-02.07: Toggle sistema LED persistente (Off/Blue/Red)
/// </summary>
public bool DoLight(LedSystemState? newState = null)
{
    // Implementazione nuova...
}
```

### **3. Verifica Requisiti Stage**

**Problema:** `IsLedRequirementMet()` usa `LastLedType` (storico), nuovo sistema usa stato corrente

**Soluzione:**
- Modificare `IsLedRequirementMet()` per accettare sia `LedType?` che `LedSystemState`
- Creare metodo helper per conversione
- Aggiornare chiamate in `DayCycleController`

**Codice Proposto:**
```csharp
// In StageRequirements.cs
public bool IsLedRequirementMet(LedType? lastUsedLed)
{
    // Metodo esistente - mantenere per compatibilità
}

public bool IsLedRequirementMet(LedSystemState currentState)
{
    LedType? required = GetRequiredLed();
    if (!required.HasValue) return true;
    
    // Converti LedSystemState a LedType per verifica
    if (currentState == LedSystemState.Off) return false;
    LedType currentLedType = currentState == LedSystemState.Blue ? LedType.Blue : LedType.Red;
    return currentLedType == required.Value;
}
```

### **4. Salvataggi Esistenti**

**Problema:** Salvataggi vecchi non hanno nuovi campi

**Soluzione:**
- Aggiungere migrazione automatica in `SaveManager.LoadGame()`
- Convertire `LastLedType` a `LedSystemState` se presente
- Inizializzare nuovi campi con default

**Codice Proposto:**
```csharp
// In SaveManager.cs - PotStateData
[Serializable]
private class PotStateData
{
    // Campi esistenti...
    public string lastLedType;  // Legacy
    
    // Nuovi campi (con default per migrazione)
    public string ledSystemState = "Off";  // Default per salvataggi vecchi
    public int daysLedBlueConsecutive = 0;
    public int daysLedRedConsecutive = 0;
}

// In ApplyPotStates()
if (!string.IsNullOrEmpty(potStateData.lastLedType))
{
    // MIGRAZIONE: Converti LastLedType a LedSystemState
    if (Enum.TryParse<LedType>(potStateData.lastLedType, out var ledType))
    {
        potState.LastLedType = ledType;  // Mantieni per compatibilità
        // Converti a nuovo sistema
        if (ledType == LedType.Blue)
            potState.LedSystemState = LedSystemState.Blue;
        else if (ledType == LedType.Red)
            potState.LedSystemState = LedSystemState.Red;
    }
}

// Applica nuovi campi (con default se mancanti)
if (Enum.TryParse<LedSystemState>(potStateData.ledSystemState, out var ledState))
    potState.LedSystemState = ledState;
potState.DaysLedBlueConsecutive = potStateData.daysLedBlueConsecutive;
potState.DaysLedRedConsecutive = potStateData.daysLedRedConsecutive;
```

---

## 📝 PIANO IMPLEMENTAZIONE STEP-BY-STEP

### **FASE 0: PREPARAZIONE (30 min)**

#### **Step 0.1: Backup e Branch**
- [ ] Creare branch Git: `feature/led-persistent-system`
- [ ] Backup salvataggi esistenti (se presenti)
- [ ] Documentare stato attuale sistema LED

#### **Step 0.2: Analisi Impatto**
- [ ] Verificare tutti i riferimenti a `DoLight()`
- [ ] Verificare tutti i riferimenti a `LastLedType`
- [ ] Verificare tutti i riferimenti a `UpdateLightingDay()`
- [ ] Creare checklist file da modificare

**Comando Verifica:**
```bash
# Cerca tutti i riferimenti
grep -r "DoLight" Assets/_Project/Scripts/
grep -r "LastLedType" Assets/_Project/Scripts/
grep -r "UpdateLightingDay" Assets/_Project/Scripts/
```

---

### **FASE 1: FONDAMENTA (1-2 ore)**

#### **Step 1.1: Creare Enum LedSystemState**

**File:** `Assets/_Project/Scripts/Dome/PotSystem/Growth/LedType.cs`

**Azione:** Aggiungere nuovo enum (NON modificare `LedType` esistente)

```csharp
namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// Tipo di LED utilizzato per l'illuminazione delle piante
    /// </summary>
    public enum LedType
    {
        Blue = 0,   // LED Blu: accelera Growth → Flowering, pH +5
        Red = 1    // LED Rosso: accelera Flowering → HarvestReady, pH -5
    }
    
    /// <summary>
    /// BLK-02.07: Stato sistema LED persistente (toggle Off/Blue/Red)
    /// </summary>
    public enum LedSystemState
    {
        Off = 0,   // Sistema LED spento
        Blue = 1,  // LED Blu attivo (Growth/stabilità)
        Red = 2    // LED Rosso attivo (Flowering/produzione)
    }
}
```

**Test:**
- [ ] Compilazione senza errori
- [ ] Enum serializzabile in Unity Inspector

**Rollback Point:** ✅ Commit dopo Step 1.1

---

#### **Step 1.2: Aggiungere Campi a PotStateModel**

**File:** `Assets/_Project/Scripts/Dome/PotStateModel.cs`

**Azione:** Aggiungere nuovi campi DOPO i campi esistenti LED

```csharp
[Header("LED Tracking (BLK-02.03)")]
[Tooltip("Ultimo tipo LED utilizzato (Blue/Red)")]
public LedType? LastLedType;  // Null se mai usato LED, Blue o Red se usato

[Header("LED System (BLK-02.07 - Persistent Toggle)")]
[Tooltip("Stato sistema LED: Off, Blue, Red")]
public LedSystemState LedSystemState = LedSystemState.Off;
[Tooltip("Giorni consecutivi con BLUE LED attivo")]
public int DaysLedBlueConsecutive = 0;
[Tooltip("Giorni consecutivi con RED LED attivo")]
public int DaysLedRedConsecutive = 0;
```

**Modifiche Costruttori:**
```csharp
public PotStateModel(string potId)
{
    // ... codice esistente ...
    LastLedType = null;
    LedSystemState = LedSystemState.Off;  // NUOVO
    DaysLedBlueConsecutive = 0;            // NUOVO
    DaysLedRedConsecutive = 0;            // NUOVO
}

public PotStateModel(string potId, int plantedDay)
{
    // ... codice esistente ...
    LastLedType = null;
    LedSystemState = LedSystemState.Off;  // NUOVO
    DaysLedBlueConsecutive = 0;            // NUOVO
    DaysLedRedConsecutive = 0;             // NUOVO
}

public void PlantSeed(int currentDay, string plantCode = null)
{
    // ... codice esistente ...
    LastLedType = null;
    LedSystemState = LedSystemState.Off;  // NUOVO
    DaysLedBlueConsecutive = 0;            // NUOVO
    DaysLedRedConsecutive = 0;             // NUOVO
}

public void ResetToEmpty()
{
    // ... codice esistente ...
    LastLedType = null;
    LedSystemState = LedSystemState.Off;  // NUOVO
    DaysLedBlueConsecutive = 0;            // NUOVO
    DaysLedRedConsecutive = 0;             // NUOVO
}
```

**Test:**
- [ ] Compilazione senza errori
- [ ] Nuovo vaso ha `LedSystemState = Off`
- [ ] Salvataggio/caricamento funziona (con default)

**Rollback Point:** ✅ Commit dopo Step 1.2

---

#### **Step 1.3: Helper Methods in PotStateModel**

**Azione:** Aggiungere metodi helper per gestione LED e Burn Risk

```csharp
/// <summary>
/// BLK-02.07: Aggiorna stato LED persistente
/// </summary>
public void SetLedSystemState(LedSystemState newState)
{
    LedSystemState = newState;
    
    // Reset contatori se cambiato tipo
    if (newState == LedSystemState.Blue)
        DaysLedRedConsecutive = 0;
    else if (newState == LedSystemState.Red)
        DaysLedBlueConsecutive = 0;
    else
    {
        DaysLedBlueConsecutive = 0;
        DaysLedRedConsecutive = 0;
    }
}

/// <summary>
/// BLK-02.07: Ottiene giorni consecutivi per stato LED corrente
/// </summary>
public int GetConsecutiveLedDays()
{
    if (LedSystemState == LedSystemState.Blue)
        return DaysLedBlueConsecutive;
    if (LedSystemState == LedSystemState.Red)
        return DaysLedRedConsecutive;
    return 0;
}

/// <summary>
/// BLK-02.07: Incrementa contatore giorni consecutivi (chiamato a fine giornata)
/// </summary>
public void IncrementConsecutiveLedDays()
{
    if (LedSystemState == LedSystemState.Blue)
        DaysLedBlueConsecutive++;
    else if (LedSystemState == LedSystemState.Red)
        DaysLedRedConsecutive++;
    // Off non incrementa
}

/// <summary>
/// BLK-02.07: Calcola livello Burn Risk in base a giorni consecutivi
/// </summary>
/// <returns>0 = Nessun rischio, 1 = Medio, 2 = Alto, 3 = Critico</returns>
public int GetBurnRiskLevel()
{
    int consecutiveDays = GetConsecutiveLedDays();
    
    if (LedSystemState == LedSystemState.Off || consecutiveDays <= 1)
        return 0;  // Nessun rischio
    
    if (consecutiveDays >= 2 && consecutiveDays <= 3)
        return 1;  // Rischio medio
    
    if (consecutiveDays >= 4 && consecutiveDays <= 5)
        return 2;  // Rischio alto
    
    if (consecutiveDays >= 6)
        return 3;  // Rischio critico (zona rossa)
    
    return 0;
}

/// <summary>
/// BLK-02.07: Verifica se pianta è in zona rossa (4+ giorni consecutivi)
/// </summary>
public bool IsInRedZone()
{
    return GetConsecutiveLedDays() >= 4 && LedSystemState != LedSystemState.Off;
}
```

**Test:**
- [ ] Metodi compilano correttamente
- [ ] Test manuale: `SetLedSystemState()` resetta contatori opposti
- [ ] `GetBurnRiskLevel()` restituisce valori corretti (0-3)
- [ ] `IsInRedZone()` restituisce true quando 4+ giorni

**Rollback Point:** ✅ Commit dopo Step 1.3

---

### **FASE 2: CORE LOGIC (2-3 ore)**

#### **Step 2.1: Riscrivere PotActions.DoLight()**

**File:** `Assets/_Project/Scripts/Dome/PotActions.cs`

**Azione:** Implementare nuovo sistema toggle con compatibilità legacy

```csharp
/// <summary>
/// DEPRECATO (BLK-02.07): Usare DoLight(LedSystemState?) invece
/// Mantenuto per compatibilità temporanea
/// </summary>
[Obsolete("Usare DoLight(LedSystemState?) per nuovo sistema persistente. Questo metodo sarà rimosso in BLK-02.08")]
public bool DoLight(LedType? ledType = null)
{
    if (showDebugLogs)
        Debug.LogWarning($"[PotActions][{potSlot?.PotId}] ⚠️ DoLight(LedType?) è deprecato. Usare DoLight(LedSystemState?)");
    
    // Migrazione automatica: converti LedType a LedSystemState
    LedSystemState? newState = null;
    if (ledType.HasValue)
    {
        newState = ledType.Value == LedType.Blue ? LedSystemState.Blue : LedSystemState.Red;
    }
    return DoLight(newState);
}

/// <summary>
/// BLK-02.07: Toggle sistema LED persistente (Off/Blue/Red)
/// Effetti applicati a fine giornata, non immediatamente
/// </summary>
/// <param name="newState">Stato desiderato. Se null, cicla: Off → Blue → Red → Off</param>
public bool DoLight(LedSystemState? newState = null)
{
    if (!CanLight())
    {
        string reason = GetLightFailureReason();
        PotEvents.EmitActionFailed(PotEvents.PotActionType.Light, potSlot, reason);
        return false;
    }
    
    // Consuma solo 1 Azione per il toggle (non CRY - consumo giornaliero)
    if (!TryConsumeResources())
    {
        PotEvents.EmitActionFailed(PotEvents.PotActionType.Light, potSlot, "Insufficient resources");
        return false;
    }
    
    // Toggle o set esplicito
    if (newState.HasValue)
    {
        _potState.SetLedSystemState(newState.Value);
    }
    else
    {
        // Ciclo: Off → Blue → Red → Off
        LedSystemState nextState = (LedSystemState)(((int)_potState.LedSystemState + 1) % 3);
        _potState.SetLedSystemState(nextState);
    }
    
    // COMPATIBILITÀ: Aggiorna LastLedType per sistemi legacy
    if (_potState.LedSystemState == LedSystemState.Blue)
        _potState.LastLedType = LedType.Blue;
    else if (_potState.LedSystemState == LedSystemState.Red)
        _potState.LastLedType = LedType.Red;
    else
        _potState.LastLedType = null;
    
    // NOTA: NON applicare effetti pH qui - vengono applicati a fine giornata
    // NOTA: NON incrementare LightExposure qui - viene fatto a fine giornata
    
    // Notifica il cambio stato
    PotEvents.EmitAction(PotEvents.PotActionType.Light, potSlot);
    PotEvents.EmitChanged(potSlot);
    
    if (showDebugLogs)
    {
        string stateMsg = _potState.LedSystemState.ToString();
        Debug.Log($"[ACT-003][{potSlot.PotId}] LED System Toggle: {stateMsg} (effetti a fine giornata)");
    }
    
    return true;
}

/// <summary>
/// BLK-02.07: Restituisce lo stato corrente del sistema LED
/// </summary>
public LedSystemState GetLedSystemState()
{
    return _potState != null ? _potState.LedSystemState : LedSystemState.Off;
}

/// <summary>
/// BLK-02.07: Verifica se sistema LED è attivo (Blue o Red)
/// </summary>
public bool IsLedSystemOn()
{
    return _potState != null && _potState.LedSystemState != LedSystemState.Off;
}
```

**Test:**
- [ ] Compilazione senza errori
- [ ] Toggle funziona: Off → Blue → Red → Off
- [ ] Set esplicito funziona: `DoLight(LedSystemState.Blue)`
- [ ] Metodo deprecato funziona (con warning)
- [ ] Eventi emessi correttamente

**Rollback Point:** ✅ Commit dopo Step 2.1

---

#### **Step 2.2: Calcolo Effetti a Fine Giornata**

**File:** `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

**Azione:** Aggiungere metodo `ApplyLedSystemEffects()` e chiamarlo in `ResolveGrowthForPot()`

**Posizione:** Dopo `ApplyWateringSystemEffects()` (circa linea 850)

```csharp
/// <summary>
/// BLK-02.07: Applica effetti sistema LED persistente a fine giornata
/// </summary>
private void ApplyLedSystemEffects(PotStateModel pot, int currentDay)
{
    if (pot.LedSystemState == LedSystemState.Off)
    {
        // Sistema OFF: decadimento graduale se era acceso
        if (pot.DaysLedBlueConsecutive > 0)
            pot.DaysLedBlueConsecutive = Mathf.Max(0, pot.DaysLedBlueConsecutive - 1);
        if (pot.DaysLedRedConsecutive > 0)
            pot.DaysLedRedConsecutive = Mathf.Max(0, pot.DaysLedRedConsecutive - 1);
        return;
    }
    
    // Incrementa contatori giorni consecutivi
    pot.IncrementConsecutiveLedDays();
    int consecutiveDays = pot.GetConsecutiveLedDays();
    
    // Calcola scaling effetti
    float effectMultiplier = GetLedEffectMultiplier(consecutiveDays);
    float malusMultiplier = GetLedMalusMultiplier(consecutiveDays);
    
    // Applica effetti crescita e pH
    ApplyLedEffects(pot, pot.LedSystemState, effectMultiplier, malusMultiplier, consecutiveDays);
    
    // Consumo CRY notturno
    int cryCost = GetNightlyCryCost(pot.LedSystemState, consecutiveDays);
    if (_gameManager != null && cryCost > 0)
    {
        if (_gameManager.SpendCRY(cryCost))
        {
            if (enableDebugLogs)
                Debug.Log($"[DayCycleController] {pot.PotId}: Consumo CRY notturno LED: {cryCost} CRY");
        }
        else
        {
            // CRY insufficiente: spegni sistema e notifica
            pot.SetLedSystemState(LedSystemState.Off);
            PotEvents.EmitToast($"LGT-002: Sistema LED {pot.PotId} spento - CRY insufficiente");
            if (enableDebugLogs)
                Debug.LogWarning($"[DayCycleController] {pot.PotId}: CRY insufficiente per LED, sistema spento");
        }
    }
    
    // Toast avviso zona rossa (4+ giorni)
    if (consecutiveDays >= 4)
    {
        PotEvents.EmitToast($"LGT-003: LED {pot.LedSystemState} attivo {consecutiveDays} giorni - Zona rossa!");
    }
}

/// <summary>
/// BLK-02.07: Calcola moltiplicatore effetti LED in base a giorni consecutivi
/// </summary>
private float GetLedEffectMultiplier(int consecutiveDays)
{
    if (consecutiveDays == 1) return 1.0f;      // x1
    if (consecutiveDays >= 2 && consecutiveDays <= 3) return 1.5f;  // x1.5
    if (consecutiveDays >= 4) return 2.0f;     // x2
    return 1.0f;
}

/// <summary>
/// BLK-02.07: Calcola moltiplicatore malus LED in base a giorni consecutivi
/// </summary>
private float GetLedMalusMultiplier(int consecutiveDays)
{
    if (consecutiveDays <= 3) return 1.0f;      // Malus base
    if (consecutiveDays >= 4) return 1.5f + (consecutiveDays - 4) * 0.2f;  // Crescita esponenziale
    return 1.0f;
}

/// <summary>
/// BLK-02.07: Calcola consumo CRY notturno per sistema LED
/// </summary>
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

/// <summary>
/// BLK-02.07: Applica effetti LED (pH, crescita, stress)
/// </summary>
private void ApplyLedEffects(PotStateModel pot, LedSystemState state, float effectMultiplier, float malusMultiplier, int consecutiveDays)
{
    if (state == LedSystemState.Off) return;
    
    // Converti LedSystemState a LedType per compatibilità
    LedType ledType = state == LedSystemState.Blue ? LedType.Blue : LedType.Red;
    
    // Effetti pH (con scaling)
    if (_phSystem != null)
    {
        float basePhDelta = ledType == LedType.Blue ? 5f : -5f;
        float phDelta = basePhDelta * effectMultiplier;
        string actionName = ledType == LedType.Blue ? "BlueLED" : "RedLED";
        _phSystem.RegisterActionDrift(phDelta, actionName, pot.PotId);
        
        if (enableDebugLogs)
            Debug.Log($"[DayCycleController] {pot.PotId}: LED {state} giorno {consecutiveDays} - pH {(phDelta > 0 ? "+" : "")}{phDelta:F1} (mult: {effectMultiplier:F1})");
    }
    
    // Effetti crescita (Light Exposure)
    int maxLightExposure = GetMaxLightExposureForPot(pot);
    if (pot.LightExposure < maxLightExposure)
    {
        pot.IncreaseLightExposure(maxLightExposure);
    }
    
    // TODO BLK-02.08: Applicare malus (Burn Stress, Mold Risk) quando sistemi saranno implementati
    // Per ora solo log
    if (consecutiveDays >= 4 && enableDebugLogs)
    {
        Debug.LogWarning($"[DayCycleController] {pot.PotId}: ⚠️ LED {state} attivo {consecutiveDays} giorni - Zona rossa! (Malus mult: {malusMultiplier:F1})");
    }
}
```

**Modificare `ResolveGrowthForPot()`:**
- Aggiungere chiamata `ApplyLedSystemEffects(pot, dayIndex);` dopo `ApplyWateringSystemEffects()`

**Test:**
- [ ] Compilazione senza errori
- [ ] Effetti applicati a fine giornata (non immediati)
- [ ] Scaling funziona (x1 → x1.5 → x2)
- [ ] Consumo CRY notturno funziona
- [ ] Toast avvisi funzionano

**Rollback Point:** ✅ Commit dopo Step 2.2

---

#### **Step 2.3: Aggiornare Verifica Requisiti Stage**

**File:** `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

**Azione:** Modificare verifica requisiti LED per usare stato corrente invece di storico

**Posizione:** Circa linea 510

```csharp
// PRIMA (vecchio):
bool ledOk = currentStageReq.IsLedRequirementMet(pot.LastLedType);

// DOPO (nuovo):
bool ledOk = currentStageReq.IsLedRequirementMet(pot.LedSystemState);
```

**File:** `Assets/_Project/Scripts/Dome/PotSystem/Growth/StageRequirements.cs`

**Azione:** Aggiungere overload `IsLedRequirementMet()` per `LedSystemState`

```csharp
/// <summary>
/// Verifica se il LED richiesto è stato utilizzato (legacy - usa LastLedType)
/// </summary>
public bool IsLedRequirementMet(LedType? lastUsedLed)
{
    LedType? required = GetRequiredLed();
    if (!required.HasValue) return true;
    return lastUsedLed.HasValue && lastUsedLed.Value == required.Value;
}

/// <summary>
/// BLK-02.07: Verifica se il LED richiesto è attivo (nuovo sistema - usa LedSystemState)
/// </summary>
public bool IsLedRequirementMet(LedSystemState currentState)
{
    LedType? required = GetRequiredLed();
    if (!required.HasValue) return true;  // Nessun LED richiesto
    
    if (currentState == LedSystemState.Off) return false;  // Sistema spento
    
    // Converti LedSystemState a LedType per verifica
    LedType currentLedType = currentState == LedSystemState.Blue ? LedType.Blue : LedType.Red;
    return currentLedType == required.Value;
}
```

**Test:**
- [ ] Compilazione senza errori
- [ ] Verifica requisiti funziona con nuovo sistema
- [ ] Piante avanzano correttamente quando LED richiesto è attivo

**Rollback Point:** ✅ Commit dopo Step 2.3

---

### **FASE 3: SALVATAGGIO/CARICAMENTO (1 ora)**

#### **Step 3.1: Aggiornare SaveManager**

**File:** `Assets/_Project/Scripts/Core/SaveManager.cs`

**Azione:** Aggiungere nuovi campi a `PotStateData` e implementare migrazione

```csharp
[Serializable]
private class PotStateData
{
    // Campi esistenti...
    public string lastLedType;  // Legacy
    
    // BLK-02.07: Nuovi campi sistema LED persistente
    public string ledSystemState = "Off";  // Default per salvataggi vecchi
    public int daysLedBlueConsecutive = 0;
    public int daysLedRedConsecutive = 0;
}
```

**In `SerializePotStates()`:**
```csharp
lastLedType = potState.LastLedType?.ToString(),
// BLK-02.07: Nuovi campi
ledSystemState = potState.LedSystemState.ToString(),
daysLedBlueConsecutive = potState.DaysLedBlueConsecutive,
daysLedRedConsecutive = potState.DaysLedRedConsecutive
```

**In `ApplyPotStates()` - MIGRAZIONE:**
```csharp
// MIGRAZIONE: Converti LastLedType a LedSystemState se presente
if (!string.IsNullOrEmpty(potStateData.lastLedType))
{
    if (Enum.TryParse<LedType>(potStateData.lastLedType, out var ledType))
    {
        potState.LastLedType = ledType;  // Mantieni per compatibilità
        
        // Converti a nuovo sistema
        if (ledType == LedType.Blue)
            potState.LedSystemState = LedSystemState.Blue;
        else if (ledType == LedType.Red)
            potState.LedSystemState = LedSystemState.Red;
        // Se null, rimane Off (default)
    }
}

// Applica nuovi campi (con default se mancanti - migrazione automatica)
if (Enum.TryParse<LedSystemState>(potStateData.ledSystemState, out var ledState))
    potState.LedSystemState = ledState;
else
    potState.LedSystemState = LedSystemState.Off;  // Default se parsing fallisce

potState.DaysLedBlueConsecutive = potStateData.daysLedBlueConsecutive;
potState.DaysLedRedConsecutive = potStateData.daysLedRedConsecutive;
```

**Test:**
- [ ] Salvataggio include nuovi campi
- [ ] Caricamento salvataggi vecchi funziona (migrazione automatica)
- [ ] Caricamento salvataggi nuovi funziona
- [ ] Nessun dato perso nella migrazione

**Rollback Point:** ✅ Commit dopo Step 3.1

---

### **FASE 4: UI/UX (1-2 ore)**

#### **🔦 Integrazione LightStress con Burn Risk**

**Obiettivo:** Utilizzare elemento `_lightStressText` già esistente per mostrare:
- Stato LED corrente (Off/Blue/Red)
- Giorni consecutivi di esposizione
- **Burn Risk indicator** con colori progressivi
- Avviso "Zona Rossa" quando 4+ giorni

**Vantaggi:**
- Riutilizza UI esistente (nessun nuovo elemento)
- Feedback visivo immediato del rischio
- Colori progressivi (giallo → arancione → rosso) per comunicare pericolo crescente

#### **Step 4.1: Aggiornare PotDetailsWidget**

**File:** `Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs`

**Azione:** Modificare UI per supportare 3 stati (Off/Blue/Red) invece di 2 pulsanti + Integrare Burn Risk in LightStress

**Opzione A (Conservativa):** Mantenere 2 pulsanti, aggiungere toggle Off
**Opzione B (Raccomandata):** Un pulsante che cicla Off → Blue → Red → Off

**Implementazione Opzione B:**
```csharp
// Rimuovere listener separati per Blue/Red
// if (_blueLedButton != null)
//     _blueLedButton.onClick.AddListener(() => OnLedButtonClicked(LedType.Blue));
// if (_redLedButton != null)
//     _redLedButton.onClick.AddListener(() => OnLedButtonClicked(LedType.Red));

// Aggiungere listener unico per toggle
if (_blueLedButton != null)
    _blueLedButton.onClick.AddListener(() => OnLedToggleClicked());
if (_redLedButton != null)
    _redLedButton.onClick.AddListener(() => OnLedToggleClicked());

// Nuovo metodo toggle
private void OnLedToggleClicked()
{
    PotSlot selectedPot = FindSelectedPot();
    if (selectedPot == null || selectedPot.PotActions == null)
    {
        Debug.LogWarning("[PotDetailsWidget] Nessun vaso selezionato");
        return;
    }
    
    // Toggle: Off → Blue → Red → Off
    bool success = selectedPot.PotActions.DoLight();  // Nessun parametro = toggle
    
    if (success)
    {
        LedSystemState newState = selectedPot.PotActions.GetLedSystemState();
        Debug.Log($"[PotDetailsWidget] LED System: {newState}");
        UpdateActionButtons(selectedPot);
        UpdateStageAndProgressUI(selectedPot);
    }
}

// Aggiornare UpdateActionButtons() per mostrare stato corrente
private void UpdateActionButtons(PotSlot pot)
{
    // ... codice esistente ...
    
    // LED: Mostra stato corrente invece di "Blue LED" / "Red LED"
    if (_blueLedButton != null)
    {
        LedSystemState currentState = pot.PotActions.GetLedSystemState();
        string buttonText = currentState == LedSystemState.Off ? "LED OFF" :
                           currentState == LedSystemState.Blue ? "LED BLUE" : "LED RED";
        UpdateButtonState(_blueLedButton, pot.PotActions.CanLight(), buttonText);
    }
    // Nascondere o disabilitare _redLedButton se non serve più
    if (_redLedButton != null)
        _redLedButton.gameObject.SetActive(false);  // O rimuovere completamente
}
```

**Integrazione Burn Risk in LightStress:**

Modificare `UpdatePlantStatsUI()` per mostrare:
- Stato LED corrente (Off/Blue/Red)
- Giorni consecutivi di esposizione
- Burn Risk indicator con colori progressivi
- Avviso "Zona Rossa" quando 4+ giorni

```csharp
// In UpdatePlantStatsUI() - Sostituire sezione Light Stress
if (_lightStressText != null)
{
    _lightStressText.richText = true;
    
    // Ottieni stato LED corrente
    LedSystemState ledState = _currentSelectedPot?.PotActions?.GetLedSystemState() ?? LedSystemState.Off;
    int consecutiveDays = state.GetConsecutiveLedDays();
    
    // Costruisci testo base
    string lightText = "";
    
    // Stato LED corrente
    string stateColor = ledState == LedSystemState.Off ? "#808080" : 
                       ledState == LedSystemState.Blue ? "#5DB6E3" : "#D35F5F";
    string stateName = ledState == LedSystemState.Off ? "OFF" : 
                      ledState == LedSystemState.Blue ? "BLUE" : "RED";
    lightText += $"<color=#CCCCCC>LED System:</color> <color={stateColor}>{stateName}</color>";
    
    // Giorni consecutivi (se attivo)
    if (ledState != LedSystemState.Off && consecutiveDays > 0)
    {
        lightText += $" <color=#CCCCCC>(×{consecutiveDays} giorni)</color>";
    }
    
    // Burn Risk Indicator (solo se LED attivo)
    if (ledState != LedSystemState.Off)
    {
        string burnRiskText = GetBurnRiskText(consecutiveDays);
        lightText += $" {burnRiskText}";
    }
    
    // Light Exposure percentuale (esistente)
    int maxLight = _currentSelectedPot?.PotActions?.GetMaxLightExposure() ?? 3;
    float lightPercentage = maxLight > 0 ? (float)state.LightExposure / maxLight * 100f : 0f;
    lightText += $"\n<color=#CCCCCC>Light Exposure:</color> <color=#FFFF00>{lightPercentage:F0}%</color>";
    
    // LED richiesto per stadio (esistente)
    if (!string.IsNullOrEmpty(state.PlantCode))
    {
        var plantDatabase = PlantDatabase.Instance;
        if (plantDatabase != null)
        {
            var plantData = plantDatabase.GetPlantDataByCode(state.PlantCode);
            if (plantData != null)
            {
                var stageReq = plantData.GetStageRequirements((PlantStage)state.Stage);
                if (stageReq != null)
                {
                    var requiredLed = stageReq.GetRequiredLed();
                    if (requiredLed.HasValue)
                    {
                        lightText += $" <color=#CCCCCC>(Required:</color> <color=#00FFFF>{requiredLed.Value}</color><color=#CCCCCC>)</color>";
                    }
                }
            }
        }
    }
    
    _lightStressText.text = lightText;
}

/// <summary>
/// BLK-02.07: Calcola testo Burn Risk in base a giorni consecutivi
/// Usa helper method da PotStateModel per coerenza
/// </summary>
private string GetBurnRiskText(int consecutiveDays)
{
    if (consecutiveDays <= 1)
        return "";  // Nessun rischio
    
    // Usa metodo helper se disponibile (più pulito)
    var potState = _currentSelectedPot?.PotActions?.GetCurrentState();
    if (potState != null)
    {
        int riskLevel = potState.GetBurnRiskLevel();
        switch (riskLevel)
        {
            case 1:  // Medio
                return $"<color=#E6C96F>⚠️ Burn Risk: Medium</color>";
            case 2:  // Alto
                return $"<color=#FF8C00>⚠️ Burn Risk: High</color>";
            case 3:  // Critico
                return $"<color=#FF0000>🔥 ZONA ROSSA - Burn Risk: Critical</color>";
            default:
                return "";
        }
    }
    
    // Fallback se helper non disponibile
    if (consecutiveDays == 2 || consecutiveDays == 3)
        return $"<color=#E6C96F>⚠️ Burn Risk: Medium</color>";
    if (consecutiveDays >= 4 && consecutiveDays < 6)
        return $"<color=#FF8C00>⚠️ Burn Risk: High</color>";
    if (consecutiveDays >= 6)
        return $"<color=#FF0000>🔥 ZONA ROSSA - Burn Risk: Critical</color>";
    
    return "";
}
```

**Test:**
- [ ] UI mostra stato corrente (Off/Blue/Red)
- [ ] Toggle funziona correttamente
- [ ] Giorni consecutivi mostrati correttamente
- [ ] Burn Risk indicator appare quando 2+ giorni
- [ ] Colori Burn Risk cambiano progressivamente (giallo → arancione → rosso)
- [ ] "Zona Rossa" appare quando 4+ giorni

**Rollback Point:** ✅ Commit dopo Step 4.1

---

#### **Step 4.2: Aggiornare PotHUDWidget**

**File:** `Assets/_Project/Scripts/UI/VaultMap/PotHUDWidget.cs`

**Azione:** Aggiornare chiamata `DoLight()` per nuovo sistema

```csharp
case PotEvents.PotActionType.Light:
    // Nuovo sistema: toggle senza parametri
    success = selectedPot.PotActions.DoLight();  // Toggle automatico
    break;
```

**Test:**
- [ ] Toggle funziona da HUD
- [ ] UI aggiornata correttamente

**Rollback Point:** ✅ Commit dopo Step 4.2

---

#### **Step 4.3: Aggiornare GrowthDebugHotkeys**

**File:** `Assets/_Project/Scripts/Dev/GrowthDebugHotkeys.cs`

**Azione:** Aggiornare hotkey L per toggle

```csharp
/// <summary>
/// L = Toggle LED sistema (Off → Blue → Red → Off)
/// </summary>
private void LightSelectedPot()
{
    selectedPot = FindSelectedPot();
    if (!selectedPot || !selectedPot.PotActions)
    {
        Debug.LogWarning("[BLK-01.03B] ❌ Nessun vaso selezionato");
        return;
    }
    
    LedSystemState oldState = selectedPot.PotActions.GetLedSystemState();
    Debug.Log($"[BLK-01.03B] 💡 Toggle LED sistema vaso {selectedPot.PotId} (stato attuale: {oldState})...");
    bool success = selectedPot.PotActions.DoLight();  // Toggle
    
    if (success)
    {
        LedSystemState newState = selectedPot.PotActions.GetLedSystemState();
        Debug.Log($"[BLK-01.03B] ✅ LED sistema vaso {selectedPot.PotId}: {oldState} → {newState}");
    }
    else
    {
        Debug.LogWarning($"[BLK-01.03B] ❌ Toggle LED sistema fallito!");
    }
}
```

**Test:**
- [ ] Hotkey L funziona
- [ ] Log mostra transizioni corrette

**Rollback Point:** ✅ Commit dopo Step 4.3

---

### **FASE 5: INTEGRAZIONE pH E TOOLTIP (1 ora)**

#### **Step 5.1: Verificare PhSystem**

**File:** `Assets/_Project/Scripts/Core/PhSystem.cs`

**Azione:** Verificare che `RegisterActionDrift()` funzioni con nuovo sistema

**Nota:** Dovrebbe già funzionare (usa stringa actionName), ma verificare che chiamate da `DayCycleController` funzionino.

**Test:**
- [ ] pH drift applicato correttamente a fine giornata
- [ ] Tooltip mostra contributo LED corretto

#### **Step 5.2: Aggiornare Tooltip pH per Mostrare LED**

**File:** `Assets/_Project/Scripts/Core/PhSystem.cs`

**Azione:** Verificare che `GetCalculationBreakdown()` mostri correttamente LED nel tooltip hover

**Nota:** Il metodo `GetCalculationBreakdown()` già mostra le azioni (linea 541-558), quindi LED dovrebbe apparire automaticamente se registrato come "BlueLED" o "RedLED". Verificare che:

1. Il tooltip mostri "LED Blu: +5,0" o "LED Rosso: -5,0" quando LED è attivo
2. Il tooltip mostri il potId se disponibile: "LED Blu: +5,0 (POT-001)"
3. Il tooltip mostri il moltiplicatore quando 2+ giorni: "LED Blu: +7,5 (×1.5)" o "LED Blu: +10,0 (×2)"

**Modifiche Proposte (se necessario):**

Se il tooltip non mostra il moltiplicatore, aggiungere info nel nome azione:

```csharp
// In ApplyLedEffects() - DayCycleController
string actionName = ledType == LedType.Blue ? "BlueLED" : "RedLED";
if (consecutiveDays >= 4)
    actionName += "_x2";  // Indica moltiplicatore x2
else if (consecutiveDays >= 2)
    actionName += "_x1.5";  // Indica moltiplicatore x1.5

_phSystem.RegisterActionDrift(phDelta, actionName, pot.PotId);
```

E aggiornare `GetActionDisplayName()` in PhSystem:

```csharp
private string GetActionDisplayName(string actionName)
{
    if (string.IsNullOrEmpty(actionName))
        return "Azione";
    
    actionName = actionName.ToLower();
    
    if (actionName.Contains("blueled"))
    {
        string multiplier = "";
        if (actionName.Contains("_x2")) multiplier = " (×2)";
        else if (actionName.Contains("_x1.5")) multiplier = " (×1.5)";
        return $"LED Blu{multiplier}";
    }
    if (actionName.Contains("redled"))
    {
        string multiplier = "";
        if (actionName.Contains("_x2")) multiplier = " (×2)";
        else if (actionName.Contains("_x1.5")) multiplier = " (×1.5)";
        return $"LED Rosso{multiplier}";
    }
    // ... resto del codice esistente
}
```

**Test:**
- [ ] Tooltip pH mostra "LED Blu: +5,0" quando Blue LED attivo 1 giorno
- [ ] Tooltip pH mostra "LED Blu: +7,5 (×1.5)" quando Blue LED attivo 2-3 giorni
- [ ] Tooltip pH mostra "LED Blu: +10,0 (×2)" quando Blue LED attivo 4+ giorni
- [ ] Tooltip pH mostra potId: "LED Blu: +5,0 (POT-001)"
- [ ] Tooltip pH mostra "LED Rosso: -5,0" quando Red LED attivo

**Rollback Point:** ✅ Commit dopo Step 5.2

---

### **FASE 6: DEBUG PANEL E TOAST (1-2 ore)**

#### **Step 6.1: Aggiungere Sezione LED Debug nel PotDebugConsole**

**File:** `Assets/_Project/Scripts/Debug/PotDebugConsole.cs`

**Azione:** Aggiungere sezione debug per sistema LED nel pannello (tasto P)

**Posizione:** Dopo sezione editing stadio (circa linea 350)

```csharp
// Sezione LED System Debug (BLK-02.07)
currentY += 20f; // Spazio
GUI.Label(new Rect(consoleX + 10f, currentY, consoleWidth - 20f, 25f), 
    "🔦 LED SYSTEM DEBUG", labelStyle);
currentY += 30f;

if (_selectedPot != null && _selectedPot.PotState != null)
{
    var potState = _selectedPot.PotState;
    
    // Stato LED corrente
    string ledStateText = $"Stato LED: {potState.LedSystemState}";
    int consecutiveDays = potState.GetConsecutiveLedDays();
    if (potState.LedSystemState != LedSystemState.Off)
    {
        ledStateText += $" (×{consecutiveDays} giorni)";
    }
    GUI.Label(new Rect(consoleX + 10f, currentY, consoleWidth - 20f, 25f), ledStateText, labelStyle);
    currentY += 30f;
    
    // Burn Risk
    int burnRisk = potState.GetBurnRiskLevel();
    string riskText = burnRisk == 0 ? "Nessun rischio" :
                     burnRisk == 1 ? "⚠️ Rischio Medio" :
                     burnRisk == 2 ? "⚠️ Rischio Alto" :
                     "🔥 Zona Rossa - Critico";
    Color riskColor = burnRisk == 0 ? Color.white :
                     burnRisk == 1 ? Color.yellow :
                     burnRisk == 2 ? new Color(1f, 0.5f, 0f) : Color.red;
    GUI.color = riskColor;
    GUI.Label(new Rect(consoleX + 10f, currentY, consoleWidth - 20f, 25f), $"Burn Risk: {riskText}", labelStyle);
    GUI.color = Color.white;
    currentY += 30f;
    
    // Pulsanti toggle LED
    if (GUI.Button(new Rect(consoleX + 10f, currentY, 150f, 30f), "LED: OFF", buttonStyle))
    {
        if (_selectedPot.DoLight(LedSystemState.Off))
            AddLog($"✅ {potState.PotId}: LED impostato a OFF");
    }
    if (GUI.Button(new Rect(consoleX + 170f, currentY, 150f, 30f), "LED: BLUE", buttonStyle))
    {
        if (_selectedPot.DoLight(LedSystemState.Blue))
            AddLog($"✅ {potState.PotId}: LED impostato a BLUE");
    }
    if (GUI.Button(new Rect(consoleX + 330f, currentY, 150f, 30f), "LED: RED", buttonStyle))
    {
        if (_selectedPot.DoLight(LedSystemState.Red))
            AddLog($"✅ {potState.PotId}: LED impostato a RED");
    }
    currentY += 40f;
    
    // Reset contatori (debug)
    if (GUI.Button(new Rect(consoleX + 10f, currentY, 200f, 30f), "Reset Contatori LED", buttonStyle))
    {
        potState.DaysLedBlueConsecutive = 0;
        potState.DaysLedRedConsecutive = 0;
        AddLog($"✅ {potState.PotId}: Contatori LED resettati");
    }
    currentY += 40f;
}
else
{
    GUI.Label(new Rect(consoleX + 10f, currentY, consoleWidth - 20f, 25f), 
        "Seleziona un POT per debug LED", labelStyle);
    currentY += 30f;
}
```

**Test:**
- [ ] Sezione LED appare nel pannello debug (tasto P)
- [ ] Mostra stato LED corrente e giorni consecutivi
- [ ] Mostra Burn Risk con colori
- [ ] Pulsanti toggle funzionano
- [ ] Reset contatori funziona

**Rollback Point:** ✅ Commit dopo Step 6.1

---

#### **Step 6.2: Aggiungere Sezione Watering Debug nel PotDebugConsole**

**File:** `Assets/_Project/Scripts/Debug/PotDebugConsole.cs`

**Azione:** Aggiungere sezione debug per sistema Watering nel pannello (tasto P)

**Posizione:** Dopo sezione LED Debug

```csharp
// Sezione Watering System Debug (GDD AZ-11)
currentY += 20f; // Spazio
GUI.Label(new Rect(consoleX + 10f, currentY, consoleWidth - 20f, 25f), 
    "💧 WATERING SYSTEM DEBUG", labelStyle);
currentY += 30f;

if (_selectedPot != null && _selectedPot.PotState != null)
{
    var potState = _selectedPot.PotState;
    
    // Stato Watering corrente
    string wateringStateText = $"Sistema Irrigazione: {(potState.WateringSystemOn ? "ON" : "OFF")}";
    if (potState.WateringSystemOn)
    {
        wateringStateText += $" (×{potState.DaysWateringSystemOn} giorni)";
    }
    GUI.Label(new Rect(consoleX + 10f, currentY, consoleWidth - 20f, 25f), wateringStateText, labelStyle);
    currentY += 30f;
    
    // Accumulatore WAT-RAW
    GUI.Label(new Rect(consoleX + 10f, currentY, consoleWidth - 20f, 25f), 
        $"Accumulatore WAT-RAW: {potState.WateringRawWaterAccumulator:F1}/1.0", labelStyle);
    currentY += 30f;
    
    // Pulsante toggle Watering
    string toggleText = potState.WateringSystemOn ? "Disattiva Irrigazione" : "Attiva Irrigazione";
    if (GUI.Button(new Rect(consoleX + 10f, currentY, 250f, 30f), toggleText, buttonStyle))
    {
        if (_selectedPot.DoWater())
        {
            string newState = potState.WateringSystemOn ? "ON" : "OFF";
            AddLog($"✅ {potState.PotId}: Sistema irrigazione impostato a {newState}");
        }
    }
    currentY += 40f;
    
    // Reset contatori (debug)
    if (GUI.Button(new Rect(consoleX + 10f, currentY, 200f, 30f), "Reset Contatori Watering", buttonStyle))
    {
        potState.DaysWateringSystemOn = 0;
        potState.WateringRawWaterAccumulator = 0f;
        AddLog($"✅ {potState.PotId}: Contatori Watering resettati");
    }
    currentY += 40f;
}
else
{
    GUI.Label(new Rect(consoleX + 10f, currentY, consoleWidth - 20f, 25f), 
        "Seleziona un POT per debug Watering", labelStyle);
    currentY += 30f;
}
```

**Test:**
- [ ] Sezione Watering appare nel pannello debug (tasto P)
- [ ] Mostra stato Watering corrente e giorni consecutivi
- [ ] Mostra accumulatore WAT-RAW
- [ ] Pulsante toggle funziona
- [ ] Reset contatori funziona

**Rollback Point:** ✅ Commit dopo Step 6.2

---

#### **Step 6.3: Aggiungere Toast Messages per LED**

**File:** `Assets/_Project/Scripts/Dome/PotActions.cs`

**Azione:** Aggiungere toast quando LED viene attivato/spento

**Posizione:** In `DoLight()` dopo toggle stato

```csharp
// In DoLight() - dopo SetLedSystemState()
// Toast notifica cambio stato
string toastMessage = "";
switch (_potState.LedSystemState)
{
    case LedSystemState.Off:
        toastMessage = $"LGT-001: Luce {potSlot.PotId} spenta";
        break;
    case LedSystemState.Blue:
        toastMessage = $"LGT-001: Luce BLUE attiva ({potSlot.PotId})";
        break;
    case LedSystemState.Red:
        toastMessage = $"LGT-001: Luce RED attiva ({potSlot.PotId})";
        break;
}

if (!string.IsNullOrEmpty(toastMessage))
{
    PotEvents.EmitToast(toastMessage);
}
```

**File:** `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

**Azione:** Verificare che toast esistenti (LGT-002, LGT-003) siano corretti

**Test:**
- [ ] Toast "LGT-001: Luce BLUE attiva (POT-001)" quando si attiva Blue LED
- [ ] Toast "LGT-001: Luce RED attiva (POT-001)" quando si attiva Red LED
- [ ] Toast "LGT-001: Luce POT-001 spenta" quando si spegne LED
- [ ] Toast "LGT-002: Sistema LED POT-001 spento - CRY insufficiente" quando CRY insufficiente
- [ ] Toast "LGT-003: LED Blue attivo 4 giorni - Zona rossa!" quando 4+ giorni

**Rollback Point:** ✅ Commit dopo Step 6.3

---

### **FASE 7: TEST E VALIDAZIONE (2-3 ore)**

#### **Step 6.1: Test Funzionali**

**Checklist Test:**

- [ ] **Test 1: Toggle Base**
  - Vaso vuoto → Toggle LED → Stato cambia Off → Blue → Red → Off
  - Consumo: 1 Azione per toggle (non CRY immediato)

- [ ] **Test 2: Effetti Fine Giornata**
  - Attiva Blue LED → End Day → pH aumenta (+5 base)
  - Attiva Red LED → End Day → pH diminuisce (-5 base)
  - Light Exposure aumenta

- [ ] **Test 3: Scaling Cumulativo**
  - Blue LED 1 giorno → pH +5 (x1)
  - Blue LED 2-3 giorni → pH +7.5 (x1.5)
  - Blue LED 4+ giorni → pH +10 (x2)

- [ ] **Test 4: Consumo CRY Notturno**
  - Blue LED giorno 1 → 1 CRY
  - Blue LED giorno 2 → 1 CRY
  - Blue LED giorno 3 → 2 CRY
  - Red LED giorno 1 → 2 CRY
  - Red LED giorno 2 → 3 CRY

- [ ] **Test 5: Requisiti Stage**
  - Pianta in Growth → Richiede Blue LED
  - Attiva Blue LED → Avanza a Flowering (se altri requisiti OK)
  - Attiva Red LED → Non avanza (LED sbagliato)

- [ ] **Test 6: Migrazione Salvataggi**
  - Carica salvataggio vecchio → LED migrato correttamente
  - Salva nuovo salvataggio → Campi presenti
  - Carica nuovo salvataggio → Funziona

- [ ] **Test 7: Decadimento**
  - Blue LED 3 giorni → Spegni → Giorno dopo: 2 giorni (decadimento -1)
  - Blue LED 1 giorno → Spegni → Giorno dopo: 0 giorni

- [ ] **Test 8: CRY Insufficiente**
  - CRY = 0 → LED attivo → End Day → Sistema spento automaticamente
  - Toast: "LGT-002: Sistema LED spento - CRY insufficiente"

- [ ] **Test 9: Zona Rossa**
  - LED 4+ giorni → Toast: "LGT-003: LED attivo X giorni - Zona rossa!"
  - LED 4+ giorni → LightStress mostra "🔥 ZONA ROSSA - Burn Risk: Critical" in rosso

- [ ] **Test 11: Burn Risk UI**
  - LED 1 giorno → Nessun indicatore Burn Risk
  - LED 2-3 giorni → Indicatore "⚠️ Burn Risk: Medium" (giallo)
  - LED 4-5 giorni → Indicatore "⚠️ Burn Risk: High" (arancione)
  - LED 6+ giorni → Indicatore "🔥 ZONA ROSSA - Burn Risk: Critical" (rosso)
  - LightStress mostra stato LED, giorni consecutivi, e Burn Risk

- [ ] **Test 12: Tooltip pH con LED**
  - Tooltip pH mostra "LED Blu: +5,0" quando Blue LED attivo 1 giorno
  - Tooltip pH mostra "LED Blu: +7,5 (×1.5)" quando Blue LED attivo 2-3 giorni
  - Tooltip pH mostra "LED Blu: +10,0 (×2)" quando Blue LED attivo 4+ giorni
  - Tooltip pH mostra potId: "LED Blu: +5,0 (POT-001)"

- [ ] **Test 13: Debug Panel (tasto P)**
  - Sezione LED System Debug appare
  - Mostra stato LED, giorni consecutivi, Burn Risk
  - Pulsanti toggle LED funzionano
  - Reset contatori funziona
  - Sezione Watering System Debug appare
  - Mostra stato Watering, giorni consecutivi, accumulatore WAT-RAW
  - Pulsante toggle Watering funziona

- [ ] **Test 14: Toast Messages**
  - Toast "LGT-001: Luce BLUE attiva (POT-001)" quando si attiva Blue LED
  - Toast "LGT-001: Luce RED attiva (POT-001)" quando si attiva Red LED
  - Toast "LGT-001: Luce POT-001 spenta" quando si spegne LED
  - Toast "LGT-002: Sistema LED POT-001 spento - CRY insufficiente" quando CRY insufficiente
  - Toast "LGT-003: LED Blue attivo 4 giorni - Zona rossa!" quando 4+ giorni

- [ ] **Test 10: Compatibilità Legacy**
  - Chiamata `DoLight(LedType.Blue)` → Warning deprecato ma funziona
  - Chiamata `DoLight(LedType.Red)` → Warning deprecato ma funziona

---

#### **Step 6.2: Test Regressione**

**Verificare che sistemi esistenti funzionino ancora:**

- [ ] Sistema crescita funziona
- [ ] Sistema pH funziona
- [ ] Sistema irrigazione funziona (non modificato)
- [ ] Sistema salvataggio funziona
- [ ] UI esistente funziona
- [ ] Debug tools funzionano

---

#### **Step 6.3: Test Performance**

- [ ] End Day non rallenta con nuovo sistema
- [ ] Salvataggio/caricamento non rallenta
- [ ] UI responsive

---

### **FASE 7: DOCUMENTAZIONE (1 ora)**

#### **Step 7.1: Aggiornare Documentazione**

**File da aggiornare:**
- [ ] `Assets/Docs/REPORT/STATUS_ECOSISTEMA_PIANTE.txt` (se esiste)
- [ ] Creare `Assets/Docs/REPORT/LED_PERSISTENT_SYSTEM_DOCUMENTATION.md`

**Contenuto:**
- Descrizione nuovo sistema
- Differenze con sistema vecchio
- Guida utilizzo
- Troubleshooting

---

#### **Step 7.2: Commenti Codice**

- [ ] Tutti i metodi nuovi hanno XML comments
- [ ] Metodi deprecati hanno `[Obsolete]` con messaggio
- [ ] TODO per future implementazioni (Burn Stress, Mold Risk)

---

## 🚨 GESTIONE RISCHI

### **Rischio 1: Breaking Changes Salvataggi**

**Probabilità:** Alta  
**Impatto:** Alto  
**Mitigazione:**
- Implementare migrazione automatica in `SaveManager`
- Testare con salvataggi vecchi
- Mantenere `LastLedType` per compatibilità temporanea

### **Rischio 2: Regressione Sistema Crescita**

**Probabilità:** Media  
**Impatto:** Alto  
**Mitigazione:**
- Test regressione completo
- Mantenere logica esistente dove possibile
- Rollback point dopo ogni fase

### **Rischio 3: Performance End Day**

**Probabilità:** Bassa  
**Impatto:** Medio  
**Mitigazione:**
- Ottimizzare calcoli scaling
- Cache valori se necessario
- Profiling se necessario

### **Rischio 4: UI Confusione**

**Probabilità:** Media  
**Impatto:** Basso  
**Mitigazione:**
- UI chiara (Off/Blue/Red)
- Tooltip esplicativi
- Toast notifications

---

## 📋 CHECKLIST FINALE

### **Pre-Implementazione**
- [ ] Branch Git creato
- [ ] Backup salvataggi
- [ ] Analisi dipendenze completata
- [ ] Piano approvato

### **Implementazione**
- [ ] Fase 1: Fondamenta ✅
- [ ] Fase 2: Core Logic ✅
- [ ] Fase 3: Salvataggio ✅
- [ ] Fase 4: UI/UX ✅
- [ ] Fase 5: Integrazione pH ✅
- [ ] Fase 6: Test ✅
- [ ] Fase 7: Documentazione ✅

### **Post-Implementazione**
- [ ] Tutti i test passati
- [ ] Documentazione aggiornata
- [ ] Codice review
- [ ] Merge a main
- [ ] Cleanup codice deprecato (futuro: BLK-02.08)

---

## 🔄 ROLLBACK PLAN

Se qualcosa va storto:

1. **Rollback Completo:**
   ```bash
   git checkout main
   git branch -D feature/led-persistent-system
   ```

2. **Rollback Parziale:**
   - Tornare a commit precedente fase problematica
   - Analizzare problema
   - Fix e riprovare

3. **Rollback Graduale:**
   - Disabilitare nuovo sistema con flag
   - Riattivare vecchio sistema temporaneamente
   - Fix e riattivare

---

## 📝 NOTE FINALI

### **Compatibilità Temporanea**

- Metodo `DoLight(LedType?)` mantenuto con `[Obsolete]` per compatibilità
- `LastLedType` mantenuto per compatibilità salvataggi
- Rimozione completa prevista per BLK-02.08 (dopo validazione estesa)

### **Future Implementazioni (BLK-02.08+)**

- **Burn Stress system** (calcolo effetti reali su crescita)
  - **Nota:** Burn Risk **indicator** già integrato in BLK-02.07 (solo visualizzazione UI)
  - BLK-02.08 implementerà effetti reali: regressione stage, -1 Livello, danni permanenti
- **Mold Risk system** (collegato a LED prolungato + overwatering)
- **Visual effects LED** (glow, colori dinamici in base a stato)
- **Audio feedback** (suoni LED on/off, warning zona rossa)
- **Animazioni UI** (pulsazione quando zona rossa, fade in/out)

### **Tempo Stimato Totale**

- **Fase 0:** 30 min
- **Fase 1:** 1-2 ore
- **Fase 2:** 2-3 ore
- **Fase 3:** 1 ora
- **Fase 4:** 1-2 ore
- **Fase 5:** 1 ora (tooltip pH)
- **Fase 6:** 1-2 ore (debug panel + toast)
- **Fase 7:** 2-3 ore (test)

**Totale:** 10-14 ore (1.5-2 giorni di lavoro)

---

## ✅ READY FOR IMPLEMENTATION

Piano completo e dettagliato. Pronto per implementazione domani.

**Status:** 🟢 APPROVATO PER IMPLEMENTAZIONE

---

*Documento creato da Senior Developer Mode (AI Assistant)*  
*Data: 2025-01-XX*  
*Versione: 1.0*

