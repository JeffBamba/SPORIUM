# ANALISI IMPATTI: MIGRAZIONE WATERING DA CLICK A TOGGLE PERSISTENTE

**Data Analisi:** 2025-12-09  
**Modalità:** DEV (Analisi Pre-Implementazione)  
**Obiettivo:** Allineare sistema Watering al GDD (AZ-11) - Toggle ON/OFF persistente con consumo giornaliero

---

## 📋 PANORAMICA DEL CAMBIAMENTO

### **Da (Sistema Attuale):**
- Azione istantanea click → annaffia immediatamente
- Consumo: 1 Azione + 1 CRY + 1 Items.Water per click
- Effetto: +1 idratazione immediato
- Overwatering: pH -5 se idratazione >= max-1

### **A (Sistema GDD):**
- Toggle ON/OFF persistente → configurazione giornaliera
- Consumo: 1 Azione per toggle + consumo giornaliero automatico (0.5 WAT-RAW + 2 CRY per vaso ON)
- Effetto: +25% idratazione a fine giornata (se ON), -25% se OFF
- Saturazione: gestita da toggle OFF (non più overwatering detection)

---

## 🎯 SISTEMI COINVOLTI

### **1. CORE DATA MODEL**

#### **1.1 PotStateModel.cs** ⚠️ **CRITICO**
**File:** `Assets/_Project/Scripts/Dome/PotStateModel.cs`

**Modifiche Richieste:**
- ✅ Aggiungere proprietà `bool WateringSystemOn` (default: false)
- ✅ Aggiungere proprietà `int DaysWateringSystemOn` (contatore giorni consecutivi ON)
- ✅ Aggiungere proprietà `float WateringRawWaterAccumulator` (accumulo per consumo 1 ogni 2 giorni)
- ✅ Modificare costruttori per inizializzare `WateringSystemOn = false`, `DaysWateringSystemOn = 0`, `WateringRawWaterAccumulator = 0f`
- ✅ Modificare `ResetToEmpty()` per resettare tutti i valori watering
- ✅ Modificare `PlantSeed()` per resettare `WateringSystemOn = false` (nuova pianta = sistema OFF)

**Impatto:**
- **Alto**: Modifica struttura dati serializzabile
- **Rischio**: Breaking change per salvataggi esistenti (necessario migration)
- **Dipendenze**: SaveManager, DayCycleController, PotActions

**Codice da Aggiungere:**
```csharp
[Header("Watering System (GDD AZ-11)")]
[Tooltip("Sistema irrigazione a goccia ON/OFF persistente")]
public bool WateringSystemOn;  // Stato toggle ON/OFF
[Tooltip("Giorni consecutivi con sistema ON (per effetti accumulati)")]
public int DaysWateringSystemOn;  // Contatore giorni ON
[Tooltip("Accumulatore WAT-RAW per consumo 1 ogni 2 giorni (0.5 per giorno ON)")]
public float WateringRawWaterAccumulator;  // Accumulo frazionario
```

---

### **2. ACTION SYSTEM**

#### **2.1 PotActions.cs** ⚠️ **CRITICO**
**File:** `Assets/_Project/Scripts/Dome/PotActions.cs`

**Modifiche Richieste:**

**A. Metodo `CanWater()` - DA REIMPLEMENTARE:**
- ❌ Rimuovere check `hasWater = _playerInventory.Has(Items.Water)` (non più necessario per toggle)
- ❌ Rimuovere check `hydrationNotMax` (non più necessario per toggle)
- ✅ Cambiare logica: verifica se può fare toggle (sempre possibile se ha pianta e in range)
- ✅ **MANTENERE NOME**: `CanWater()` con nuova logica toggle

**B. Metodo `DoWater()` - DA REIMPLEMENTARE:**
- ❌ Rimuovere consumo immediato `Items.Water` (ora consumo giornaliero)
- ❌ Rimuovere consumo immediato CRY (ora consumo giornaliero)
- ❌ Rimuovere `IncreaseHydration()` immediato (ora a fine giornata)
- ✅ **MANTENERE** overwatering detection (spostata a fine giornata in `ApplyWateringSystemEffects()`)
- ❌ Rimuovere `UpdateWateringDay()` (non più timestamp, ma stato persistente)
- ✅ Implementare toggle: `_potState.WateringSystemOn = !_potState.WateringSystemOn`
- ✅ Incrementare/resettare `DaysWateringSystemOn` in base a toggle
- ✅ Consumare solo 1 Azione per toggle
- ✅ Emettere evento `PotEvents.EmitAction(PotEvents.PotActionType.Water, potSlot)`
- ✅ **MANTENERE NOME**: `DoWater()` con nuova logica toggle

**C. Nuovo Metodo Helper:**
- ✅ `GetWateringSystemState()` → restituisce stato ON/OFF
- ✅ `IsWateringSystemOn()` → check rapido

**Impatto:**
- **Altissimo**: Riscrittura completa logica watering
- **Rischio**: Tutti i riferimenti a `DoWater()` devono essere aggiornati
- **Dipendenze**: UI, Minigioco, Debug tools

---

#### **2.2 PotEvents.cs**
**File:** `Assets/_Project/Scripts/Dome/PotEvents.cs`

**Modifiche Richieste:**
- ⚠️ Verificare se `PotActionType.Water` è ancora appropriato o serve nuovo tipo
- ✅ Eventi esistenti dovrebbero funzionare (solo cambio logica interna)

**Impatto:**
- **Basso**: Nessuna modifica necessaria (eventi già compatibili)

---

### **3. GROWTH SYSTEM**

#### **3.1 DayCycleController.cs** ⚠️ **CRITICO**
**File:** `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

**Modifiche Richieste:**

**A. Metodo `ResolveGrowthForPot()`:**
- ❌ Rimuovere logica `hadHydration = (pot.LastWateredDay == previousDay)`
- ✅ Nuova logica: `hadHydration = pot.WateringSystemOn` (se ON = aveva idratazione)
- ✅ Aggiornare contatore: `pot.DaysWateringSystemOn++` se ON, reset se OFF

**B. Nuovo Metodo `ApplyWateringSystemEffects()`:**
- ✅ Chiamato in `HandleDayChanged()` dopo `ResolveGrowthForAllPots()`
- ✅ Per ogni vaso con `WateringSystemOn == true`:
  - Applica +25% idratazione (1 punto se max=4)
  - **Overwatering Detection**: Se idratazione >= max-1 dopo applicazione → pH -5
  - Sistema accumulo WAT-RAW: ogni giorno ON = +0.5, ogni 2 giorni = 1 WAT-RAW consumato
  - Consuma 2 CRY per vaso ON
- ✅ Per ogni vaso con `WateringSystemOn == false`:
  - Applica -25% idratazione (1 punto se max=4, ma non sotto 0) - **EVAPORAZIONE**
  - Nessun consumo risorse
- ✅ **IMPORTANTE**: Decay naturale viene applicato DOPO effetti watering system

**C. Metodo `ApplyDecayAndCleanup()`:**
- ✅ Decay naturale rimane invariato (applicato sempre)
- ✅ Evaporazione OFF viene applicata in `ApplyWateringSystemEffects()` (prima del decay)
- ✅ Ordine: Evaporazione OFF → Idratazione ON → Decay naturale

**Impatto:**
- **Altissimo**: Modifica logica crescita e consumo risorse
- **Rischio**: Calcolo crescita potrebbe cambiare comportamento
- **Dipendenze**: PotStateModel, Inventory, EconomySystem

**Ordine Esecuzione End of Day:**
```
1. ResolveGrowthForAllPots() → calcola crescita (usa WateringSystemOn)
2. ApplyWateringSystemEffects() → applica idratazione/evaporazione + consumo risorse + overwatering detection
3. ApplyDecayAndCleanup() → decay naturale (sempre applicato)
4. CalculateAndRegisterPhDrift() → pH drift
```

**Dettaglio ApplyWateringSystemEffects():**
```
Per ogni vaso:
  Se WateringSystemOn == true:
    - Applica +25% idratazione (1 punto se max=4)
    - Se idratazione >= max-1 → pH -5 (OVERWATERING)
    - Accumula 0.5 WAT-RAW (WateringRawWaterAccumulator += 0.5)
    - Se accumulatore >= 1.0 → consuma 1 WAT-RAW, reset accumulatore
    - Consuma 2 CRY
    - Incrementa DaysWateringSystemOn
  Se WateringSystemOn == false:
    - Applica -25% idratazione (1 punto, min 0) - EVAPORAZIONE
    - Reset DaysWateringSystemOn = 0
    - Reset WateringRawWaterAccumulator = 0
```

---

### **4. INVENTORY SYSTEM**

#### **4.1 Inventory.cs**
**File:** `Assets/_Project/Scripts/Core/ItemsSystem/Inventory.cs`

**Modifiche Richieste:**
- ⚠️ **PROBLEMA CRITICO**: Inventario attuale gestisce solo quantità intere
- ❌ Consumo 0.5 WAT-RAW richiede sistema frazionario o accumulo
- ✅ **SOLUZIONE 1**: Sistema accumulo (ogni 2 giorni = 1 WAT-RAW consumato)
- ✅ **SOLUZIONE 2**: Sistema frazionario (modificare Inventory per supportare float)
- ✅ **SOLUZIONE 3**: Consumo 1 WAT-RAW ogni 2 giorni (più semplice)

**Raccomandazione:** Soluzione 3 (consumo 1 WAT-RAW ogni 2 giorni per vaso ON)

**Impatto:**
- **Medio**: Modifica logica consumo inventario
- **Rischio**: Se si usa Soluzione 2, breaking change per tutto l'inventario
- **Dipendenze**: DayCycleController, GameManager

---

#### **4.2 Items.cs**
**File:** `Assets/_Project/Scripts/Core/ItemsSystem/Items.cs`

**Modifiche Richieste:**
- ✅ Verificare che `Items.Water` sia corretto (GDD usa WAT-RAW)
- ⚠️ Se diverso, allineare nomenclatura

**Impatto:**
- **Basso**: Verifica nomenclatura

---

### **5. ECONOMY SYSTEM**

#### **5.1 EconomySystem.cs**
**File:** `Assets/_Project/Scripts/Core/EconomySystem.cs`

**Modifiche Richieste:**
- ✅ Nessuna modifica diretta (DayCycleController gestisce consumo)
- ⚠️ Verificare che `Spend()` supporti consumo multiplo (2 CRY per vaso)

**Impatto:**
- **Basso**: Nessuna modifica necessaria

---

#### **5.2 GameManager.cs**
**File:** `Assets/_Project/Scripts/Core/GameManager.cs`

**Modifiche Richieste:**
- ✅ Nessuna modifica diretta
- ⚠️ Verificare che `HandleDayChanged()` non interferisca con nuovo sistema

**Impatto:**
- **Basso**: Nessuna modifica necessaria

---

### **6. UI SYSTEM**

#### **6.1 PotHUDWidget.cs** ⚠️ **CRITICO**
**File:** `Assets/_Project/Scripts/UI/VaultMap/PotHUDWidget.cs`

**Modifiche Richieste:**
- ❌ Rimuovere chiamata `DoWater()` in `ExecuteAction()`
- ✅ Implementare `ToggleWatering()` che chiama `PotActions.DoWater()` (rinominato)
- ✅ Modificare `UpdateActionButtons()` per mostrare stato ON/OFF
- ✅ Cambiare testo bottone: "Annaffiare" → "Irrigazione ON/OFF" o icona toggle
- ✅ Mostrare indicatore visivo stato (ON/OFF) nel widget

**Impatto:**
- **Alto**: Modifica UI e UX
- **Rischio**: Confusione utente se non chiaro
- **Dipendenze**: PotActions

---

#### **6.2 PotDetailsWidget.cs** ⚠️ **CRITICO**
**File:** `Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs`

**Modifiche Richieste:**
- ❌ Rimuovere minigioco `WateringMinigame.Show()`
- ✅ Implementare toggle button invece di click
- ✅ Mostrare stato ON/OFF nel widget dettagli
- ✅ Aggiornare `UpdateActionButtons()` per toggle

**Impatto:**
- **Alto**: Modifica UI dettagliata
- **Rischio**: Rimozione minigioco potrebbe confondere utenti esistenti
- **Dipendenze**: PotActions, WateringMinigame (da deprecare)

---

#### **6.3 WateringMinigame.cs** ⚠️ **DA DEPRECARE**
**File:** `Assets/_Project/Scripts/UI/VaultMap/Watering/WateringMinigame.cs`

**Modifiche Richieste:**
- ❌ **DEPRECARE**: GDD specifica che minigioco è deprecato (MG-04)
- ✅ Rimuovere riferimento in `PotDetailsWidget.cs`
- ⚠️ **OPZIONE**: Mantenere file ma non usarlo (per compatibilità futura)

**Impatto:**
- **Medio**: Rimozione feature esistente
- **Rischio**: Utenti potrebbero aspettarsi minigioco
- **Dipendenze**: PotDetailsWidget

---

### **7. SAVE/LOAD SYSTEM**

#### **7.1 SaveManager.cs** ⚠️ **CRITICO**
**File:** `Assets/_Project/Scripts/Core/SaveManager.cs`

**Modifiche Richieste:**

**A. Classe `PotStateData`:**
- ✅ Aggiungere `public bool wateringSystemOn;`
- ✅ Aggiungere `public int daysWateringSystemOn;`
- ✅ Aggiungere `public float wateringRawWaterAccumulator;` (per sistema accumulo 0.5 WAT-RAW)

**B. Metodo `SerializePotStates()`:**
- ✅ Aggiungere serializzazione:
  ```csharp
  wateringSystemOn = potState.WateringSystemOn,
  daysWateringSystemOn = potState.DaysWateringSystemOn,
  wateringRawWaterAccumulator = potState.WateringRawWaterAccumulator,
  ```

**C. Metodo `ApplyPotStates()`:**
- ✅ Aggiungere deserializzazione:
  ```csharp
  potState.WateringSystemOn = potStateData.wateringSystemOn;
  potState.DaysWateringSystemOn = potStateData.daysWateringSystemOn;
  potState.WateringRawWaterAccumulator = potStateData.wateringRawWaterAccumulator;
  ```

**D. Migration System:**
- ⚠️ **CRITICO**: Salvataggi esistenti non hanno `wateringSystemOn`, `daysWateringSystemOn`, `wateringRawWaterAccumulator`
- ✅ Implementare migration: se campo mancante → default `false`, `0`, `0f`
- ✅ Gestire compatibilità backward (salvataggi vecchi funzionano, sistema parte OFF)

**Impatto:**
- **Alto**: Modifica formato salvataggio
- **Rischio**: Breaking change per salvataggi esistenti
- **Dipendenze**: PotStateModel

---

### **8. INTEGRATION SYSTEM**

#### **8.1 PotSystemIntegration.cs**
**File:** `Assets/_Project/Scripts/Dome/PotSystemIntegration.cs`

**Modifiche Richieste:**
- ✅ Aggiornare `ShowAvailableActions()` per riflettere toggle
- ✅ Aggiornare `GetTotalActionCost()` (non più costo per click, ma per toggle)
- ⚠️ Verificare logica costi (ora è 1 Azione per toggle, non per annaffiatura)

**Impatto:**
- **Basso**: Aggiornamento logica costi

---

### **9. DEBUG TOOLS**

#### **9.1 GrowthDebugHotkeys.cs**
**File:** `Assets/_Project/Scripts/Dev/GrowthDebugHotkeys.cs`

**Modifiche Richieste:**
- ✅ Aggiornare `WaterSelectedPot()` per chiamare `ToggleWatering()` invece di `DoWater()`
- ✅ Aggiornare help text: "H = Toggle watering system"

**Impatto:**
- **Basso**: Aggiornamento tool debug

---

#### **9.2 PhSystemDebugConsole.cs**
**File:** `Assets/_Project/Scripts/Debug/PhSystemDebugConsole.cs`

**Modifiche Richieste:**
- ⚠️ Verificare se "Overwatering" button è ancora rilevante
- ✅ Mantenere per test, ma documentare che non è più parte del gameplay normale

**Impatto:**
- **Basso**: Nessuna modifica necessaria (tool di test)

---

#### **9.3 SPOR-BLK-01-03A-GrowthDebugger.cs**
**File:** `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-GrowthDebugger.cs`

**Modifiche Richieste:**
- ✅ Aggiornare display per mostrare `WateringSystemOn` invece di `LastWateredDay`
- ✅ Mostrare `DaysWateringSystemOn` nel debug output

**Impatto:**
- **Basso**: Aggiornamento display debug

---

### **10. TEST SYSTEM**

#### **10.1 SPOR-BLK-01-03A-SystemTest.cs**
**File:** `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-SystemTest.cs`

**Modifiche Richieste:**
- ✅ Aggiornare test per verificare `WateringSystemOn` invece di `LastWateredDay`
- ✅ Aggiungere test per toggle ON/OFF
- ✅ Aggiungere test per consumo giornaliero

**Impatto:**
- **Medio**: Aggiornamento suite test

---

## 📊 TABELLA RIEPILOGATIVA IMPATTI

| Sistema | File | Priorità | Complessità | Rischio | Dipendenze |
|---------|------|----------|-------------|---------|------------|
| **Data Model** | PotStateModel.cs | ⚠️ CRITICO | Media | Alto | SaveManager |
| **Actions** | PotActions.cs | ⚠️ CRITICO | Alta | Altissimo | UI, Minigioco |
| **Growth** | DayCycleController.cs | ⚠️ CRITICO | Alta | Altissimo | PotStateModel, Inventory |
| **Inventory** | Inventory.cs | ⚠️ MEDIO | Media | Medio | DayCycleController |
| **Save/Load** | SaveManager.cs | ⚠️ CRITICO | Media | Alto | PotStateModel |
| **UI Main** | PotHUDWidget.cs | ⚠️ CRITICO | Media | Alto | PotActions |
| **UI Details** | PotDetailsWidget.cs | ⚠️ CRITICO | Media | Alto | PotActions, Minigioco |
| **Minigioco** | WateringMinigame.cs | ⚠️ BASSO | Bassa | Basso | PotDetailsWidget |
| **Integration** | PotSystemIntegration.cs | ⚠️ BASSO | Bassa | Basso | PotActions |
| **Debug Tools** | GrowthDebugHotkeys.cs | ⚠️ BASSO | Bassa | Basso | PotActions |

---

## ⚠️ RISCHI IDENTIFICATI

### **1. RISCHI CRITICI**

#### **1.1 Breaking Change Salvataggi** 🔴
- **Problema**: Salvataggi esistenti non hanno `WateringSystemOn`
- **Soluzione**: Implementare migration automatica (default `false`)
- **Impatto**: Utenti esistenti perderanno stato watering (accettabile, sistema nuovo)

#### **1.2 Consumo 0.5 WAT-RAW** ✅ **RISOLTO**
- **Problema**: Inventario gestisce solo quantità intere
- **Soluzione**: ✅ **DECISO** - Sistema accumulo interno (ogni giorno ON = +0.5, ogni 2 giorni = 1 consumato)
- **Implementazione**: `WateringRawWaterAccumulator` in `PotStateModel`
- **Impatto**: Bilanciamento identico al GDD (0.5 per giorno, consumo 1 ogni 2 giorni)

#### **1.3 Cambio Comportamento Crescita** 🟡
- **Problema**: `LastWateredDay` non più usato per crescita
- **Soluzione**: Usare `WateringSystemOn` direttamente
- **Impatto**: Crescita potrebbe cambiare (da timestamp a stato persistente)

#### **1.4 Rimozione Minigioco** 🟡
- **Problema**: Utenti potrebbero aspettarsi minigioco
- **Soluzione**: ✅ **DECISO** - Rimuovere completamente
- **Impatto**: Cambio UX significativo (accettato)

### **2. RISCHI MEDI**

#### **2.1 Overwatering Detection** ✅ **MANTENUTO**
- **Decisione**: ✅ **MANTENERE** overwatering detection (pH -5)
- **Implementazione**: Se idratazione >= max-1 E sistema ON → pH -5
- **Motivazione**: Mantenere impatti gameplay identici, solo cambia interazione
- **Impatto**: Nessuna perdita feature, comportamento coerente

#### **2.2 UI Confusione** 🟡
- **Problema**: Toggle ON/OFF potrebbe non essere chiaro
- **Soluzione**: UI chiara con indicatori visivi ON/OFF
- **Impatto**: Curva apprendimento per utenti

### **3. RISCHI BASSI**

#### **3.1 Debug Tools** 🟢
- **Problema**: Tool debug potrebbero non funzionare
- **Soluzione**: Aggiornare tutti i tool debug
- **Impatto**: Solo sviluppo, non produzione

---

## 🔧 PIANO DI IMPLEMENTAZIONE CONSIGLIATO

### **FASE 1: PREPARAZIONE** (1-2 ore)
1. ✅ Creare backup salvataggi esistenti
2. ✅ Documentare stato attuale sistema
3. ✅ Preparare test case

### **FASE 2: DATA MODEL** (2-3 ore)
1. ✅ Modificare `PotStateModel.cs` (aggiungere `WateringSystemOn`, `DaysWateringSystemOn`)
2. ✅ Aggiornare costruttori e metodi reset
3. ✅ Test unitari per nuovo modello

### **FASE 3: CORE LOGIC** (4-6 ore)
1. ✅ Riscrivere `PotActions.DoWater()` → `ToggleWatering()`
2. ✅ Riscrivere `PotActions.CanWater()` → logica toggle
3. ✅ Implementare `DayCycleController.ApplyWateringSystemEffects()`
4. ✅ Modificare `DayCycleController.ResolveGrowthForPot()` (usare `WateringSystemOn`)
5. ✅ Test integrazione crescita

### **FASE 4: INVENTORY & ECONOMY** (2-3 ore)
1. ✅ Implementare sistema accumulo WAT-RAW (0.5 per giorno ON, consumo 1 ogni 2 giorni)
2. ✅ Implementare consumo 2 CRY per vaso ON a fine giornata
3. ✅ Implementare overwatering detection a fine giornata (pH -5 se idratazione >= max-1)
4. ✅ Test consumo risorse e overwatering

### **FASE 5: UI** (3-4 ore)
1. ✅ Modificare `PotHUDWidget.cs` (toggle button)
2. ✅ Modificare `PotDetailsWidget.cs` (rimuovere minigioco, aggiungere toggle)
3. ✅ Aggiungere indicatori visivi ON/OFF
4. ✅ Test UI

### **FASE 6: SAVE/LOAD** (2-3 ore)
1. ✅ Modificare `SaveManager.cs` (serializzazione/deserializzazione)
2. ✅ Implementare migration per salvataggi esistenti
3. ✅ Test save/load

### **FASE 7: DEBUG & TEST** (2-3 ore)
1. ✅ Aggiornare tutti i tool debug
2. ✅ Aggiornare test suite
3. ✅ Test end-to-end completo

### **FASE 8: CLEANUP** (1-2 ore)
1. ✅ Deprecare/rimuovere `WateringMinigame.cs`
2. ✅ Rimuovere codice obsoleto
3. ✅ Documentazione finale

**TOTALE STIMATO: 17-26 ore**

---

## 📝 NOTE IMPLEMENTATIVE

### **Decisioni Tecniche APPROVATE:**

1. **Consumo 0.5 WAT-RAW:** ✅ **DECISO**
   - **Soluzione**: 1 WAT-RAW ogni 2 giorni per vaso ON
   - **Implementazione**: Sistema accumulo interno (ogni giorno ON = +0.5, ogni 2 giorni = 1 consumato)

2. **Nomenclatura Metodi:** ✅ **DECISO**
   - **Soluzione**: Mantenere `DoWater()` e `CanWater()` con nuova logica toggle
   - **Motivazione**: Meno breaking changes, compatibilità con codice esistente

3. **Minigioco:** ✅ **DECISO**
   - **Soluzione**: Rimuovere completamente `WateringMinigame.cs`
   - **Motivazione**: GDD specifica deprecato (MG-04), Micro-Operations 2.0

4. **Overwatering & Evaporazione:** ✅ **DECISO**
   - **Soluzione**: **MANTENERE** overwatering detection (pH -5) + evaporazione giornaliera
   - **Motivazione**: Il sistema cambia solo l'interazione (toggle vs click), ma gli impatti gameplay rimangono identici
   - **Implementazione**:
     - Overwatering: Se idratazione >= max-1 E sistema ON → pH -5
     - Evaporazione: Se sistema OFF → -25% idratazione a fine giornata (oltre al decay naturale)
     - Il player deve gestire attivamente ON/OFF per evitare perdita controllo idratazione

---

## ✅ CHECKLIST PRE-IMPLEMENTAZIONE

- [ ] Backup salvataggi esistenti
- [ ] Documentazione stato attuale completa
- [ ] Test case preparati
- [ ] Decisioni tecniche prese (consumo WAT-RAW, nomenclatura, minigioco)
- [ ] Piano implementazione approvato
- [ ] Team informato del cambiamento

---

**Documento generato:** 2025-12-09  
**Versione GDD:** 40 v.08/12/2025  
**Sezione GDD:** AZ-11 — Watering  
**Stato:** ✅ Analisi Completa - Decisioni Approvate - Pronto per Implementazione

---

## ✅ DECISIONI FINALI APPROVATE

1. **Consumo WAT-RAW**: Sistema accumulo (0.5 per giorno ON, consumo 1 ogni 2 giorni) ✅
2. **Nomenclatura**: Mantenere `DoWater()` e `CanWater()` con nuova logica ✅
3. **Minigioco**: Rimuovere completamente `WateringMinigame.cs` ✅
4. **Overwatering & Evaporazione**: **MANTENERE** entrambi (pH -5 + evaporazione -25%) ✅

**Principio Guida**: Il sistema cambia solo l'interazione (toggle vs click), ma gli impatti gameplay rimangono identici per mantenere coerenza e bilanciamento.

