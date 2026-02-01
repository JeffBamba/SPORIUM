# Analisi Sistema Condensation - RAW WATER e HUD

**Data:** 2026-01-17  
**Scopo:** Analisi completa del funzionamento del sistema di condensation per RAW WATER e indice HUD

---

## 📋 SOMMARIO ESECUTIVO

Il sistema di condensation gestisce la raccolta automatica di **RAW WATER (WAT-RAW)** attraverso un meccanismo di accumulo giornaliero. Il sistema è composto da:
- **CondensationSystem**: Logica core di accumulo
- **HUDCondensation**: UI per raccolta manuale
- **TopBarController**: Visualizzazione indice nella HUD principale

---

## 🔧 COMPONENTI DEL SISTEMA

### 1. **CondensationSystem.cs**
**Path:** `Assets/_Project/Scripts/Core/CondensationSystem.cs`

#### Funzionamento
- **Accumulo giornaliero**: Ogni giorno aumenta di `CondensationGrowthPerDay` (default: 3)
- **Massimo**: Clamp a `MaxCondensation` (default: 10)
- **Reset**: Quando viene raccolto, si resetta a 0

#### Configurazione
```csharp
// Valori default (se config non trovato)
DEFAULT_GROWTH_PER_DAY = 3f
DEFAULT_MAX_CONDENSATION = 10f

// Config attuale (CondensationConfig.asset)
CondensationGrowthPerDay: 3
MaxCondensation: 10
```

#### Metodi Principali
- `DayChanged()`: Incrementa condensation di `CondensationGrowthPerDay` ogni giorno
- `Reset()`: Resetta a 0 dopo la raccolta
- `GetMax()`: Restituisce il massimo configurabile

---

### 2. **GameManager.cs - Integrazione**
**Path:** `Assets/_Project/Scripts/Core/GameManager.cs`

#### Integrazione con Day Cycle
```csharp
private void HandleDayChanged(int day)
{   
    _economySystem.Spend(_dailyPowerCost);
    _actionSystem.ResetActions(_actionsPerDay);
    
    _condensationSystem.DayChanged();  // ← Incrementa condensation
    OnCondensationChanged?.Invoke(_condensationSystem.CondensationAmount);
}
```

#### Metodo di Raccolta
```csharp
public float CollectCondensation()
{
    var amount = _condensationSystem.CondensationAmount;
    _condensationSystem.Reset();  // ← Resetta dopo raccolta
    OnCondensationChanged?.Invoke(_condensationSystem.CondensationAmount);
    return amount;  // ← Restituisce quantità raccolta
}
```

**⚠️ NOTA IMPORTANTE**: Il metodo `CollectCondensation()` restituisce un `float`, ma viene convertito a `int` in `HUDCondensation.cs` (linea 117).

---

### 3. **HUDCondensation.cs - UI Raccolta**
**Path:** `Assets/_Project/Scripts/UI/VaultMap/HUDCondensation.cs`

#### Componenti UI
- **ProgressBar**: Mostra progresso verso il massimo
- **Button (_collectButton)**: Pulsante per raccogliere

#### Funzionamento
1. **Aggiornamento Progress Bar**:
   ```csharp
   _progressBar.Value = value / _gameManager.GetMaxCondensation();
   ```
   - Mostra percentuale: `condensationAmount / maxCondensation`
   - Esempio: 6/10 = 60% della barra

2. **Raccolta**:
   ```csharp
   int amountToCollect = (int)_gameManager.CollectCondensation();
   if (amountToCollect != 0)
   {
       _gameManager.PlayerInventory.Add(Items.Water, amountToCollect);
       // Mostra toast notification
   }
   ```
   - Converte `float` → `int` (troncamento)
   - Aggiunge WAT-RAW all'inventario
   - Mostra notifica toast

#### Eventi
- Sottoscrive `GameManager.OnCondensationChanged` per aggiornare la progress bar in tempo reale

---

### 4. **TopBarController.cs - Indice HUD**
**Path:** `Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs`

#### ⚠️ PROBLEMA CRITICO: Nessuna Sottoscrizione Eventi
**TopBarController NON si sottoscrive a `GameManager.OnCondensationChanged`!**

Questo significa che:
- Il valore mostrato è **statico** (valore serializzato di default: `78f`)
- **Non viene mai aggiornato** quando cambia la condensation reale
- L'animazione idle varia un valore che non corrisponde alla realtà

#### Visualizzazione
- **Label**: `condensation-value` mostra percentuale
- **Formato**: `"{Mathf.RoundToInt(value)}%"` (es. "78%")
- **Valore iniziale**: `[SerializeField] private float _condensation = 78f;` (hardcoded!)

#### Animazione Idle
```csharp
private IEnumerator CondensationIdleAnimation()
{
    while (true)
    {
        float delay = UnityEngine.Random.Range(0.9f, 1.5f);
        yield return new WaitForSeconds(delay);
        
        // Variazione ±1% per effetto "vivo"
        float variation = UnityEngine.Random.Range(-1f, 1f);
        float displayValue = Mathf.Clamp(_condensation + variation, 0f, 100f);
        
        _condensationValueLabel.text = $"{Mathf.RoundToInt(displayValue)}%";
    }
}
```

**⚠️ PROBLEMA IDENTIFICATO**: 
- L'animazione idle mostra una **percentuale (0-100%)** basata su `_condensation`
- Ma `_condensation` è un valore **assoluto (0-10)**, non una percentuale!
- Questo causa una **discrepanza** tra valore reale e visualizzazione

#### Metodo Update
```csharp
public void UpdateCondensation(float value)
{
    _condensation = value;  // ← Valore assoluto (0-10)
    
    if (_condensationValueLabel != null)
    {
        _condensationValueLabel.text = $"{Mathf.RoundToInt(value)}%";  // ← Mostra come %
    }
}
```

**🔴 BUG**: Se `value = 6` (condensation reale), viene mostrato "6%" invece di "60%" (6/10 = 60%)

---

## 📊 FLUSSO COMPLETO

### Giorno 1
1. **Start**: `CondensationAmount = 0`
2. **End of Day**: `DayChanged()` → `CondensationAmount = 3`
3. **HUD**: 
   - ProgressBar: 30% (3/10)
   - TopBar: "3%" ❌ (dovrebbe essere "30%")

### Giorno 2
1. **Start**: `CondensationAmount = 3`
2. **End of Day**: `DayChanged()` → `CondensationAmount = 6`
3. **HUD**: 
   - ProgressBar: 60% (6/10)
   - TopBar: "6%" ❌ (dovrebbe essere "60%")

### Giorno 3
1. **Start**: `CondensationAmount = 6`
2. **End of Day**: `DayChanged()` → `CondensationAmount = 9`
3. **HUD**: 
   - ProgressBar: 90% (9/10)
   - TopBar: "9%" ❌ (dovrebbe essere "90%")

### Giorno 4
1. **Start**: `CondensationAmount = 9`
2. **End of Day**: `DayChanged()` → `CondensationAmount = 10` (clamp)
3. **HUD**: 
   - ProgressBar: 100% (10/10)
   - TopBar: "10%" ❌ (dovrebbe essere "100%")

### Raccolta
1. **Click su Collect**: `CollectCondensation()` → restituisce `10`
2. **Inventario**: Aggiunge `10 WAT-RAW`
3. **Reset**: `CondensationAmount = 0`
4. **HUD**: ProgressBar = 0%, TopBar = "0%"

---

## 📚 CONFRONTO CON NOTION GDD

### Da Notion - Sezione 7 (ITEM livelli e COSTO IN CRY)

> **WAT-RAW — Acqua Grezza**
> - Risorsa primaria per irrigazione.
> - **+2 unità dal Condensation Collector a fine giornata**

### ⚠️ DISCREPANZA IDENTIFICATA

**GDD Notion dice:**
- **+2 unità** a fine giornata

**Implementazione attuale:**
- **+3 unità** al giorno (`CondensationGrowthPerDay = 3`)
- **Massimo 10 unità** prima della raccolta

**Domande da chiarire:**
1. Il GDD intende **+2 unità automatiche** nell'inventario a fine giornata (senza interazione)?
2. Oppure **+2 unità di accumulo** nel collector (che poi vanno raccolte manualmente)?
3. L'implementazione attuale (+3/giorno, max 10) è intenzionale o va allineata al GDD?

---

## 🐛 PROBLEMI IDENTIFICATI

### 1. **TopBarController Non Aggiornato**
**Severità**: 🔴 **ALTA**  
**Descrizione**: TopBarController non si sottoscrive a `OnCondensationChanged`, quindi mostra sempre il valore statico di default (78%) invece del valore reale

**Fix Richiesto**:
```csharp
// In InitializeGameSystems(), dopo il collegamento di EconomySystem:
if (_gameManager != null)
{
    // Sottoscrivi a OnCondensationChanged
    _gameManager.OnCondensationChanged += OnCondensationChanged;
    
    // Aggiorna valore iniziale
    if (_gameManager.CondensationSystem != null)
    {
        float currentCondensation = _gameManager.CondensationSystem.CondensationAmount;
        float maxCondensation = _gameManager.GetMaxCondensation();
        float percentage = (currentCondensation / maxCondensation) * 100f;
        UpdateCondensation(percentage);
    }
}

// Aggiungere metodo handler:
private void OnCondensationChanged(float value)
{
    if (_gameManager != null)
    {
        float maxCondensation = _gameManager.GetMaxCondensation();
        float percentage = (value / maxCondensation) * 100f;
        UpdateCondensation(percentage);
    }
}

// In OnDestroy():
if (_gameManager != null)
{
    _gameManager.OnCondensationChanged -= OnCondensationChanged;
}
```

### 2. **Bug Visualizzazione TopBar**
**Severità**: Media  
**Descrizione**: La TopBar mostra il valore assoluto (0-10) come percentuale, invece di calcolare la percentuale reale (0-100%)

**Fix Richiesto**:
```csharp
public void UpdateCondensation(float value)
{
    _condensation = value;
    
    if (_condensationValueLabel != null)
    {
        // Calcola percentuale reale
        float maxCondensation = GetMaxCondensation(); // o da GameManager
        float percentage = (value / maxCondensation) * 100f;
        _condensationValueLabel.text = $"{Mathf.RoundToInt(percentage)}%";
    }
}
```

### 3. **Troncamento Float → Int**
**Severità**: Bassa  
**Descrizione**: `CollectCondensation()` restituisce `float`, ma viene troncato a `int` in `HUDCondensation.cs`

**Impatto**: Se `CondensationAmount = 9.5`, viene raccolto solo `9` (perdita di 0.5)

**Fix Opzionale**: Usare `Mathf.RoundToInt()` invece di cast diretto

### 4. **Discrepanza GDD vs Implementazione**
**Severità**: Media  
**Descrizione**: GDD dice "+2 unità", implementazione usa "+3 unità/giorno"

**Azione**: Chiarire con design team se:
- Cambiare a +2/giorno
- Oppure aggiornare GDD a +3/giorno

---

## ✅ FUNZIONALITÀ CORRETTE

1. ✅ **Accumulo giornaliero**: Funziona correttamente
2. ✅ **Clamp al massimo**: Previene overflow
3. ✅ **Reset dopo raccolta**: Funziona correttamente
4. ✅ **Progress Bar HUDCondensation**: Calcola correttamente la percentuale
5. ✅ **Eventi OnCondensationChanged**: Aggiornamento in tempo reale funzionante
6. ✅ **Toast Notification**: Mostra correttamente la quantità raccolta

---

## 🔄 FLUSSO DATI

```
┌─────────────────┐
│ DayCycleController │
│  HandleDayChanged() │
└────────┬──────────┘
         │
         ▼
┌─────────────────┐
│   GameManager   │
│ HandleDayChanged()│
└────────┬──────────┘
         │
         ├─► CondensationSystem.DayChanged()
         │   └─► _condensationAmount += 3
         │
         └─► OnCondensationChanged?.Invoke(amount)
             │
             ├─► HUDCondensation.HandleChangeCondensation()
             │   └─► ProgressBar.Value = amount / max
             │
             └─► TopBarController.UpdateCondensation()
                 └─► Label.text = "{value}%" ❌ (BUG)
```

---

## 📝 RACCOMANDAZIONI

### Priorità Alta
1. **🔴 CRITICO: Collegare TopBarController a OnCondensationChanged**: Il valore non viene mai aggiornato!
2. **Fix visualizzazione TopBar**: Convertire valore assoluto in percentuale (dopo aver collegato gli eventi)
3. **Chiarire GDD**: Allineare +2 vs +3 unità/giorno

### Priorità Media
3. **Migliorare precisione**: Usare `Mathf.RoundToInt()` invece di cast diretto
4. **Documentazione**: Aggiungere commenti su unità di misura (assoluto vs percentuale)

### Priorità Bassa
5. **Animazione idle**: Correggere per usare percentuale invece di valore assoluto
6. **Test coverage**: Aggiungere test per edge cases (max, overflow, raccolta parziale)

---

## 📌 NOTE TECNICHE

### Unità di Misura
- **CondensationSystem**: Usa valori **assoluti** (0-10)
- **HUDCondensation**: Calcola **percentuale** per progress bar (corretto)
- **TopBarController**: Mostra valore assoluto come percentuale (❌ bug)

### Configurazione
- File: `Assets/Resources/Configs/CondensationConfig.asset`
- Valori: `CondensationGrowthPerDay = 3`, `MaxCondensation = 10`
- Fallback: Se config non trovato, usa valori default hardcoded

### Eventi
- `GameManager.OnCondensationChanged`: Emesso quando:
  - Cambia il giorno (`DayChanged()`)
  - Viene raccolta la condensation (`CollectCondensation()`)

---

## 🎯 CONCLUSIONI

Il sistema di condensation funziona correttamente per quanto riguarda:
- ✅ Accumulo giornaliero
- ✅ Raccolta manuale
- ✅ Aggiunta a inventario
- ✅ Progress bar nella HUD di raccolta

**Problemi da risolvere:**
- 🔴 **CRITICO**: TopBarController non si aggiorna mai (non sottoscritto agli eventi)
- ❌ Visualizzazione percentuale errata nella TopBar (valore assoluto mostrato come %)
- ❌ Discrepanza con GDD (+2 vs +3 unità)

**Prossimi passi:**
1. Fix bug visualizzazione TopBar
2. Chiarire con design team la discrepanza GDD
3. Test completo del flusso end-to-end
