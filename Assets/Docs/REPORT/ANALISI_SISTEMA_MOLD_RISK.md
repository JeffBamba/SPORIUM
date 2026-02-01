# Analisi Sistema Mold Risk - Funzionamento Completo

**Data:** 2026-01-17  
**Scopo:** Analisi completa del sistema Mold Risk (rischio muffe) e delle sue interazioni

---

## 📋 SOMMARIO ESECUTIVO

Il sistema **Mold Risk** gestisce il rischio di infestazione da muffe nelle piante, basandosi principalmente su **overwatering prolungato**. Il sistema ha 4 livelli (0-3) e può bloccare la crescita della pianta quando raggiunge livelli critici.

---

## 🔧 COMPONENTI DEL SISTEMA

### 1. **MoldSystem.cs** - Logica Core
**Path:** `Assets/_Project/Scripts/Dome/PotSystem/Mold/MoldSystem.cs`

#### Metodi Principali

##### `CalculateMoldRisk()`
Calcola il rischio muffe basato **SOLO su overwatering prolungato**:
```csharp
int daysOverThreshold = Mathf.Max(0, potState.DaysOverwateringConsecutive - config.overwateringDaysThreshold);
return Mathf.Clamp(daysOverThreshold, 0f, 3f);
```

**Formula:**
- Soglia default: **3 giorni** di overwatering consecutivi
- Ogni giorno oltre la soglia = +1 livello di rischio
- Esempio con soglia 3:
  - 3 giorni = Level 0 (ancora sotto soglia)
  - 4 giorni = Level 1 (1 giorno oltre)
  - 5 giorni = Level 2 (2 giorni oltre)
  - 6 giorni = Level 3 (3 giorni oltre)

##### `GetMoldRiskLevel()`
Converte il rischio calcolato in livello discreto:
- **0 = None** (nessun rischio)
- **1 = Mild** (≥1)
- **2 = Severe** (≥2)
- **3 = Critical** (≥3)

##### `CheckInfestation()`
Verifica se il rischio si materializza in infestazione:
```csharp
return moldRiskLevel == 3 && daysAtLevel3 >= 2;
```
**Regola**: Infestazione solo dopo **2 giorni consecutivi a livello 3**

##### `ApplyInfestation()`
Applica effetti quando la pianta diventa infestata:

**Mild (Level 1):**
- Riduce livello pianta di **1**
- Riduce Condition Score di **10**

**Severe/Critical (Level ≥2):**
- Riduce livello pianta di **3**
- Riduce Condition Score di **30**
- **Blocca avanzamento crescita**

##### `ReduceMoldRiskLevel()`
Riduce il livello di 1 (o azzera se ≤1):
- Usato da **Additivi Basici** (pH)
- Usato da **Potatura**
- Se scende sotto 3, resetta `DaysAtMoldRiskLevel3` e `IsInfested`

##### `IncreaseMoldRiskLevel()`
Aumenta il livello di 1 (clamp 0-3):
- Usato da **Additivi Acidi** (pH)
- Se già a livello 3, **propaga al pot vicino** (se disponibile)

##### `RemoveInfestation()`
Rimuove infestazione (chiamato da potatura):
- Chiama `ReduceMoldRiskLevel()`
- Resetta `DaysWithoutPruning = 0`

---

### 2. **MoldConfig.cs** - Configurazione
**Path:** `Assets/_Project/Scripts/Dome/PotSystem/Mold/MoldConfig.cs`

#### Valori Configurabili

**Soglie Rischio:**
- `mildRiskThreshold = 1`
- `severeRiskThreshold = 2`
- `criticalRiskThreshold = 3`

**Fattori Rischio:**
- `overwateringDaysThreshold = 3` (giorni consecutivi prima che inizi il rischio)
- `acidicPhThreshold = -20f` ⚠️ **DEPRECATO** (non più usato)
- `pruningNeglectAccumulation = 0.5f` ⚠️ **DEPRECATO** (non più usato)

**Effetti Infestazione:**
- `mildScorePenalty = 10`
- `severeScorePenalty = 30`
- `mildLevelReduction = 1`
- `severeLevelReduction = 3`

**File Config:** `Assets/Resources/Configs/MoldConfig.asset`

---

### 3. **PotStateModel.cs** - Stato
**Path:** `Assets/_Project/Scripts/Dome/PotStateModel.cs`

#### Proprietà Mold Risk

```csharp
public int MoldRiskLevel = 0;                    // Livello rischio (0-3)
public int DaysWithoutPruning = 0;              // Giorni senza potatura
public int DaysOverwateringConsecutive = 0;     // Giorni consecutivi in overwatering
public int DaysAtMoldRiskLevel3 = 0;            // Giorni consecutivi a livello 3
public bool IsInfested = false;                 // Flag infestazione (true dopo 2 giorni a livello 3)
```

---

## 🔄 FLUSSO GIORNALIERO

### End of Day (DayCycleController)

1. **Traccia Overwatering:**
   ```csharp
   bool isOverwateringForMold = PlantConditionSystem.IsOverwatering(pot, maxHydration);
   if (isOverwateringForMold)
       pot.DaysOverwateringConsecutive++;
   else
       pot.DaysOverwateringConsecutive = 0;  // Reset se non più in overwatering
   ```

2. **Incrementa Giorni Senza Potatura:**
   ```csharp
   pot.DaysWithoutPruning++;
   ```

3. **Calcola Mold Risk:**
   ```csharp
   pot.MoldRiskLevel = MoldSystem.GetMoldRiskLevel(pot, _phSystem, plantData, moldConfig);
   ```

4. **Tracking Giorni a Livello 3:**
   ```csharp
   if (pot.MoldRiskLevel == 3)
       pot.DaysAtMoldRiskLevel3++;
   else
       pot.DaysAtMoldRiskLevel3 = 0;  // Reset se non più a livello 3
   ```

5. **Verifica Infestazione:**
   ```csharp
   bool shouldInfest = MoldSystem.CheckInfestation(pot.MoldRiskLevel, pot.DaysAtMoldRiskLevel3);
   if (shouldInfest && !pot.IsInfested)
   {
       pot.IsInfested = true;
       MoldSystem.ApplyInfestation(pot, pot.MoldRiskLevel, moldConfig, levelConfig);
       // Mostra toast notifica
   }
   else if (!shouldInfest && pot.IsInfested)
   {
       pot.IsInfested = false;  // Rimossa se livello sceso sotto 3
   }
   ```

---

## 🚫 BLOCCAGGIO CRESCITA

### Quando Blocca l'Avanzamento

Il Mold Risk blocca l'avanzamento quando:
```csharp
bool isBlockedByMold = pot.MoldRiskLevel >= 2;  // Severe (2) o Critical (3)
```

**Effetti:**
- ❌ La pianta **non può avanzare** allo stadio successivo
- ✅ Continua a produrre frutti (se già in HarvestReady)
- ✅ Continua a subire effetti negativi

**Verifica in `ResolveGrowthForAllPots()`:**
```csharp
bool requirementsMet = !isBlockedByCondition && !isBlockedByMold &&
                     hydrationOk && ledOk && durationOk && optimalDaysOk && fertilizerOk && pointsOk;
```

---

## 🎮 INTERAZIONI CON AZIONI GIOCATORE

### 1. **Potatura (Pruning)**
**File:** `Assets/_Project/Scripts/Dome/PotActions.cs`

```csharp
MoldSystem.RemoveInfestation(_potState);
_potState.DaysWithoutPruning = 0;
```

**Effetti:**
- Riduce `MoldRiskLevel` di 1 (o azzera se ≤1)
- Resetta `DaysWithoutPruning`
- Rimuove infestazione se presente

### 2. **Additivi pH**

#### Additivo Basico
```csharp
MoldSystem.ReduceMoldRiskLevel(_potState);
```
- Riduce livello di 1 (o azzera se ≤1)
- Se scende sotto 3, rimuove infestazione

#### Additivo Acido
```csharp
MoldSystem.IncreaseMoldRiskLevel(_potState, FindNearestPot());
```
- Aumenta livello di 1
- Se già a livello 3, **propaga al pot vicino**

**Propagazione:**
- Se pot vicino ha livello < 3 → aumenta di 1
- Se pot vicino ha già livello 3 → incrementa `DaysAtMoldRiskLevel3`

---

## 📊 CALCOLO CONDITION SCORE

### Bonus: Nessun Mold Risk
**File:** `Assets/_Project/Scripts/Dome/PotSystem/Condition/PlantConditionSystem.cs`

```csharp
bool hasNoMoldRisk = (moldRiskLevel == 0);
if (hasNoMoldRisk)
{
    score += DifficultyCalibrationConfig.BonusNoMold;  // +5 punti
    contributors.Add(new ConditionContributor("Nessun Mold Risk", BonusNoMold, true));
}
```

### Malus: Infestazione
```csharp
if (potState.IsInfested)
{
    if (potState.MoldRiskLevel == 1)  // Mild
    {
        score -= DifficultyCalibrationConfig.MalusMoldMild;  // -10 punti
    }
    // NOTA: MalusMoldSevere rimosso perché già blocca l'avanzamento
}
```

---

## 🎨 VISUALIZZAZIONE UI

### 1. **PotDetailsWidget**
**File:** `Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs`

**Mold Risk Text:**
- **Lvl 0**: "Nessuno" (verde)
- **Lvl 1-2**: "Mild/Severe (Lvl X)" (arancione)
- **Lvl 3**: "Critical (Lvl 3)" (rosso)

**Badge INFESTATA:**
- Mostrato solo se `IsInfested == true`
- Colore: rosso se Lvl 3, arancione altrimenti

### 2. **PlantCardV2**
**File:** `Assets/_Project/Scripts/UI/UIToolkit/PlantCard/PlantCardV2DataBinder.cs`

**Vital Parameter Box:**
- Mostra livello 0-3
- Range info: "Range Ideale: 0"
- Badge colorato in base al livello

**Blocco Avanzamento:**
```csharp
bool isBlockedByMold = state.MoldRiskLevel >= 2;
if (isBlockedByMold)
{
    sb.AppendLine("<color=#FF0000>⚠️ Avanzamento BLOCCATO: Infestazione muffa grave</color>");
}
```

### 3. **PlantCardV3 Terminal**
**File:** `Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs`

**Check Requisiti:**
```csharp
int mold = Mathf.Clamp(pot.MoldRiskLevel, 0, 3);
bool moldOk = mold < 2;  // OK se < 2 (Severe)
string moldLine = $"{(moldOk ? "✓" : "✗")} Mold Risk      Level {mold} | Required: <2";
```

---

## 🔔 NOTIFICHE

### Toast Notifications
**File:** `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/NotificationTypeSpecDefaults.cs`

**Codici:**
- `MLD-RISK-CRIT`: "🚨 CRITICAL mold risk on {potId}."
- `MLD-INFESTED`: "🚨 Mold infestation on {potId}."
- `MLD-201`: "Muffa rilevata in {potId}"

**Watchers:**
- `FoundationNotificationsWatchersRunner` monitora `MoldRiskLevel >= 3`

---

## 📈 ESEMPIO FLUSSO COMPLETO

### Scenario: Overwatering Prolungato

**Giorno 1-3:**
- Overwatering attivo → `DaysOverwateringConsecutive = 1, 2, 3`
- `MoldRiskLevel = 0` (sotto soglia 3)

**Giorno 4:**
- `DaysOverwateringConsecutive = 4`
- `MoldRiskLevel = 1` (Mild) ← 1 giorno oltre soglia
- Condition Score: +5 (bonus no mold perso)

**Giorno 5:**
- `DaysOverwateringConsecutive = 5`
- `MoldRiskLevel = 2` (Severe) ← 2 giorni oltre soglia
- ❌ **Avanzamento BLOCCATO**

**Giorno 6:**
- `DaysOverwateringConsecutive = 6`
- `MoldRiskLevel = 3` (Critical) ← 3 giorni oltre soglia
- `DaysAtMoldRiskLevel3 = 1`
- ❌ **Avanzamento BLOCCATO**

**Giorno 7:**
- `DaysOverwateringConsecutive = 7`
- `MoldRiskLevel = 3` (Critical)
- `DaysAtMoldRiskLevel3 = 2`
- ✅ **Infestazione applicata!**
  - `IsInfested = true`
  - Livello pianta: -3
  - Condition Score: -30
  - Toast: "La pianta nel pot X è ora Infestata"

**Giorno 8:**
- Giocatore esegue **Potatura**
- `MoldSystem.RemoveInfestation()` → `MoldRiskLevel = 2`
- `IsInfested = false`
- `DaysAtMoldRiskLevel3 = 0`
- ❌ **Avanzamento ancora BLOCCATO** (Lvl 2)

**Giorno 9:**
- Giocatore applica **Additivo Basico**
- `MoldSystem.ReduceMoldRiskLevel()` → `MoldRiskLevel = 1`
- ✅ **Avanzamento sbloccato** (Lvl 1 < 2)

---

## 🔗 INTERAZIONI CON ALTRI SISTEMI

### 1. **PlantConditionSystem**
- **Bonus**: +5 punti se `MoldRiskLevel == 0`
- **Malus**: -10 punti se `IsInfested && MoldRiskLevel == 1`

### 2. **PlantGrowthSystem**
- **Blocco**: `MoldRiskLevel >= 2` blocca avanzamento
- **Verifica**: Controllato in `ResolveGrowthForAllPots()`

### 3. **PhSystem**
- ⚠️ **DEPRECATO**: `acidicPhThreshold` non più usato nel calcolo
- **Interazione**: Additivi pH modificano Mold Risk Level

### 4. **WateringSystem**
- **Tracking**: `DaysOverwateringConsecutive` calcolato da `IsOverwatering()`
- **Reset**: Quando overwatering termina, contatore si resetta

### 5. **PruningSystem**
- **Rimozione**: Potatura rimuove infestazione
- **Reset**: `DaysWithoutPruning = 0` dopo potatura

---

## ⚙️ CONFIGURAZIONE ATTUALE

**File:** `Assets/Resources/Configs/MoldConfig.asset`

```
mildRiskThreshold: 1
severeRiskThreshold: 2
criticalRiskThreshold: 3
overwateringDaysThreshold: 3
mildScorePenalty: 10
severeScorePenalty: 30
mildLevelReduction: 1
severeLevelReduction: 3
```

---

## 🐛 NOTE TECNICHE

### Valori Deprecati
- `acidicPhThreshold`: Non più usato nel calcolo
- `pruningNeglectAccumulation`: Non più usato nel calcolo

**Motivo**: Il sistema ora si basa **SOLO su overwatering prolungato**

### Separazione Responsabilità
- `MoldSystem.GetMoldRiskLevel()`: Calcola basandosi su condizioni
- `PotStateModel.MoldRiskLevel`: Valore persistente (può essere impostato manualmente)

**Nota**: PlantCardV2 usa direttamente `state.MoldRiskLevel` invece di ricalcolare, per rispettare valori impostati manualmente dalla debug console.

---

## 📝 RIEPILOGO

### Calcolo Rischio
- **Base**: Overwatering consecutivo
- **Formula**: `(DaysOverwateringConsecutive - threshold) = Level`
- **Range**: 0-3

### Livelli
- **0 = None**: Nessun rischio
- **1 = Mild**: Rischio lieve
- **2 = Severe**: Rischio grave → **Blocca avanzamento**
- **3 = Critical**: Rischio critico → **Blocca avanzamento**

### Infestazione
- **Condizione**: `MoldRiskLevel == 3 && DaysAtMoldRiskLevel3 >= 2`
- **Effetti**: Riduzione livello/score, blocco avanzamento

### Rimozione
- **Potatura**: Riduce livello di 1 (o azzera se ≤1)
- **Additivo Basico**: Riduce livello di 1 (o azzera se ≤1)
- **Additivo Acido**: Aumenta livello di 1 (può propagare)

### Blocco Crescita
- **Soglia**: `MoldRiskLevel >= 2`
- **Effetto**: Pianta non può avanzare allo stadio successivo
- **Persistenza**: Continua finché livello non scende sotto 2

---

## 🎯 CONCLUSIONI

Il sistema Mold Risk è **semplice ma efficace**:
- ✅ Calcolo chiaro basato su overwatering
- ✅ Livelli progressivi con effetti crescenti
- ✅ Meccanismo di infestazione con delay (2 giorni)
- ✅ Interazioni con azioni giocatore (potatura, additivi)
- ✅ Blocco crescita per livelli critici
- ✅ Propagazione tra vasi vicini

**Punti di forza:**
- Sistema prevedibile e comprensibile
- Integrazione con altri sistemi ben definita
- UI chiara e informativa

**Aree di miglioramento potenziali:**
- Valori deprecati potrebbero essere rimossi
- Documentazione inline potrebbe essere più dettagliata
- Test coverage per edge cases (propagazione, rimozione)
