---
name: Completamento Sistema Livelli Piante
overview: "Completare il sistema di progressione dei livelli (1-5) implementando: modifica resa frutti con riduzione quantità e aumento qualità proporzionale a partire dal Livello 3, verifica corretta dei cicli completati, e preparazione per slot passivi. Il sistema base è già presente ma mancano le parti relative a qualità frutti e la completa integrazione con il sistema di raccolta."
todos:
  - id: task-1-1
    content: Aggiungere metodo GetQualityModifier() a PlantLevelConfig per calcolare bonus qualità basato su livello
    status: completed
  - id: task-1-2
    content: Estendere ItemFabric con CreateItemWithQuality() per creare Item con qualità personalizzata
    status: completed
  - id: task-1-3
    content: Modificare DoHarvest() per calcolare e applicare qualità frutti basata su livello pianta
    status: completed
    dependencies:
      - task-1-1
      - task-1-2
  - id: task-2-1
    content: Verificare logica incremento cicli completati (quando viene considerato completo il ciclo)
    status: completed
  - id: task-2-2
    content: Correggere logica ciclo completo se necessario (spostare IncrementCompletedCycle da Harvest a Fertilize)
    status: completed
    dependencies:
      - task-2-1
  - id: task-3-1
    content: Verificare soglie cicli nel PlantLevelConfig corrispondono alle specifiche GDD
    status: completed
  - id: task-4-1
    content: Aggiungere metodo CanMoveToPassiveSlot() a PlantLevelSystem per check livello 5
    status: completed
  - id: task-5-1
    content: "Test completo sistema: quantità, qualità, progressione livelli per tutti i livelli 1-5"
    status: pending
    dependencies:
      - task-1-3
      - task-2-2
  - id: task-6-1
    content: "Estendere PotDebugConsole con funzionalità sistema livelli: visualizzazione modificatori quantità/qualità, qualità frutti attesa, check slot passivi"
    status: completed
    dependencies:
      - task-1-1
      - task-4-1
---

# Piano Implementazione: Q1 - Sistema Livelli Completo

**Nome Piano**: Q1 - Sistema Livelli Completo

## Stato Attuale

Il sistema di livelli è **parzialmente implementato**:

- ✅ `PlantLevelSystem.cs` - gestisce progressione livelli
- ✅ `PlantLevelConfig.cs` - configurazione soglie cicli e modificatori
- ✅ `PotStateModel` con `PlantLevel` (1-5) e `CompletedCycles`
- ✅ Tracking cicli completati alla raccolta (Harvest → Resting)
- ✅ Check level-up funzionante
- ⚠️ **Parziale**: Modifica resa quantità esiste ma non è completa
- ❌ **Mancante**: Sistema qualità frutti basato su livello
- ❌ **Mancante**: Integrazione qualità con inventario

## Obiettivi

1. **Completare sistema resa frutti** con riduzione quantità (-15%/livello da Lvl 3) e aumento qualità proporzionale
2. **Verificare e correggere** il calcolo dei cicli completati (ciclo valido = Flowering → HarvestReady → Resting → Fertilizzante → Flowering)
3. **Preparare integrazione** con slot passivi (requisito: solo Lvl 5)

---

## Fase 1: Sistema Qualità Frutti

### Task 1.1: Estendere PlantLevelConfig con Modificatore Qualità

**File**: [`Assets/_Project/Scripts/Dome/PotSystem/Level/PlantLevelConfig.cs`](Assets/_Project/Scripts/Dome/PotSystem/Level/PlantLevelConfig.cs)Aggiungere calcolo qualità proporzionale alla riduzione quantità:

- Metodo `GetQualityModifier(int level)`: ritorna bonus qualità per livello
- Formula: Lvl 3 = +15%, Lvl 4 = +30%, Lvl 5 = +45% (proporzionale alla riduzione quantità)
- Lvl 1-2: qualità base (0% bonus)

**Nota**: La qualità deve aumentare proporzionalmente alla riduzione di quantità per mantenere il valore totale dei frutti bilanciato.

### Task 1.2: Estendere ItemFabric per Qualità Personalizzata

**File**: [`Assets/_Project/Scripts/Core/ItemsSystem/ItemFabric.cs`](Assets/_Project/Scripts/Core/ItemsSystem/ItemFabric.cs)Aggiungere metodo per creare Item con qualità personalizzata:

```csharp
public static Item CreateItemWithQuality(string typeId, float quality)
```

Questo permetterà di creare frutti con qualità basata sul livello della pianta durante la raccolta.

### Task 1.3: Modificare Logica Raccolta per Applicare Qualità

**File**: [`Assets/_Project/Scripts/Dome/PotActions.cs`](Assets/_Project/Scripts/Dome/PotActions.cs) (linee 1087-1110)Modificare `DoHarvest()` per:

1. Calcolare quantità modificata (già presente, verificarne la correttezza)
2. **NUOVO**: Calcolare qualità basata su livello usando `PlantLevelConfig.GetQualityModifier()`
3. Creare frutti con qualità personalizzata usando `ItemFabric.CreateItemWithQuality()`
4. Aggiungere logging per debug (quantità e qualità applicate)

**Formula qualità**:

- Qualità base: `ItemConfig.MaxQuality`
- Qualità finale: `baseQuality * (1 + qualityModifier / 100)`
- Clamp tra `MaxQuality` e `MaxQuality * 2` (max +100%)

---

## Fase 2: Verifica e Correzione Cicli Completati

### Task 2.1: Verificare Logica Ciclo Valido

**File**: [`Assets/_Project/Scripts/Dome/PotActions.cs`](Assets/_Project/Scripts/Dome/PotActions.cs) (linea 1124)Verificare che `IncrementCompletedCycle()` venga chiamato solo quando:

- Pianta completa il ciclo: **Flowering → HarvestReady → Resting → Fertilizzante → Flowering**

**Analisi attuale**:

- ✅ Incremento chiamato dopo Harvest (HarvestReady → Resting)
- ⚠️ Verificare che il ciclo sia completo solo se Resting → Flowering avviene con fertilizzante

### Task 2.2: Correggere Logica Ciclo Completo

**File**: [`Assets/_Project/Scripts/Dome/PotActions.cs`](Assets/_Project/Scripts/Dome/PotActions.cs) (linea 1237)**Decisione confermata**: Il ciclo è completo quando si riattiva la pianta da Resting → Flowering con fertilizzante.**Modifiche da applicare**:

1. **Rimuovere** `IncrementCompletedCycle()` da `DoHarvest()` (linea ~1124)
2. **Aggiungere** `IncrementCompletedCycle()` in `DoFertilize()` quando `Stage == Resting` e avviene la transizione a Flowering (dopo linea ~1240)

La transizione Resting → Flowering avviene in `DoFertilize()` (linea 1237-1252).---

## Fase 3: Verifica Soglie Progressione

### Task 3.1: Correggere Soglie Cicli nel PlantLevelConfig

**File**: [`Assets/_Project/Scripts/Dome/PotSystem/Level/PlantLevelConfig.cs`](Assets/_Project/Scripts/Dome/PotSystem/Level/PlantLevelConfig.cs) (linea 14)**Soglie attuali**: `[1, 2, 3, 4]` → **ERRATASoglie corrette (sequenziali)**: `[1, 2, 2, 3]`

- Lvl 1→2: **1 ciclo** (1° ciclo)
- Lvl 2→3: **2 cicli** (2°, 3° ciclo - totale 3 cicli per Lvl 3)
- Lvl 3→4: **2 cicli** (4°, 5° ciclo)
- Lvl 4→5: **3 cicli** (6°, 7°, 8° ciclo)

**Totale cicli per raggiungere Lvl 5**: 8 cicli completi**Modifica da applicare**:

```csharp
// PRIMA:
public int[] cyclesThresholds = new int[] { 1, 2, 3, 4 };

// DOPO:
public int[] cyclesThresholds = new int[] { 1, 2, 2, 3 };
```

---

## Fase 4: Preparazione Slot Passivi

### Task 4.1: Aggiungere Check Livello per Slot Passivi

**File**: [`Assets/_Project/Scripts/Dome/PotSystem/Level/PlantLevelSystem.cs`](Assets/_Project/Scripts/Dome/PotSystem/Level/PlantLevelSystem.cs)Aggiungere metodo helper:

```csharp
public static bool CanMoveToPassiveSlot(PotStateModel potState)
{
    return potState != null && potState.HasPlant && potState.PlantLevel >= 5;
}
```

Questo metodo verrà utilizzato dal sistema slot passivi (da implementare separatamente nel task "Q1 - Slot Passivi").---

## Fase 6: Estensione Debug Console

### Task 6.1: Aggiungere Funzionalità Sistema Livelli a PotDebugConsole

**File**: [`Assets/_Project/Scripts/Debug/PotDebugConsole.cs`](Assets/_Project/Scripts/Debug/PotDebugConsole.cs)**Obiettivo**: Estendere la console di debug (tasto **P**) con funzionalità per testare e visualizzare il sistema livelli completo.**Modifiche da applicare**:

1. **Aggiungere visualizzazione modificatori resa** nella sezione "Plant Level System":

- Mostrare modificatore quantità per livello corrente (es. "Quantità: -15%")
- Mostrare modificatore qualità per livello corrente (es. "Qualità: +15%")
- Calcolare usando `PlantLevelConfig.GetQuantityModifier()` e `GetQualityModifier()`

2. **Aggiungere visualizzazione qualità frutti attesa**:

- Calcolare qualità base da `ItemConfig.MaxQuality` per "fruits-001"
- Applicare modificatore qualità basato su livello
- Mostrare: "Qualità frutti attesa: X.X (base: Y + Z%)"

3. **Aggiungere check slot passivi**:

- Mostrare: "Slot Passivi: Disponibile" / "Slot Passivi: Non disponibile (richiede Lvl 5)"
- Usare `PlantLevelSystem.CanMoveToPassiveSlot()` per verificare

4. **Aggiungere pulsante "Force Level Up"** (opzionale, per test rapidi):

- Forza l'incremento di 1 livello (fino a max 5)
- Utile per testare rapidamente progressione senza completare cicli

**Posizionamento UI**:

- Aggiungere dopo la visualizzazione attuale (livello, cicli, progress)
- Prima della sezione "Growth Points System"

**Esempio layout aggiunto**:

```javascript
=== Plant Level System ===
Livello: 3/5 | Cicli: 3/2 | Progress: 100%
[Imposta Level] [Imposta Cycles]

Modificatori Resa (Lvl 3):
- Quantità: -15% (3 frutti → 2.55)
- Qualità: +15% (4.0 → 4.6)
- Slot Passivi: Non disponibile (richiede Lvl 5)

[Force Level Up] [Calcola Resa Test]
```

Questo metodo verrà utilizzato dal sistema slot passivi (da implementare separatamente nel task "Q1 - Slot Passivi").---

## Fase 5: Testing e Validazione

### Task 5.1: Sequenza di Testing Completa

Questa sezione fornisce una sequenza dettagliata step-by-step per testare il sistema livelli in-game, verificando sia il codice che il gameplay atteso.**Nota**: Utilizzare `PotDebugConsole` (tasto **P** in Play Mode) per impostare rapidamente livello, cicli completati e stage delle piante durante i test.

#### Setup Iniziale

1. **Apri Unity Editor** e carica la scena `SCN_VaultMap` o `SCN_Bootstrap`
2. **Verifica presenza Debug Console**:

- Premere **P** per aprire `PotDebugConsole`
- Verificare che sia presente il GameObject con componente `PotDebugConsole` nella scena

3. **Prepara inventario**:

- Verifica di avere almeno 10 fertilizzanti compatibili (standard/pure/evil)
- Verifica di avere almeno 1 seme da piantare
- Verifica di avere WAT-RAW per l'irrigazione

#### Test 1: Verifica Resa Frutti Lvl 1-2 (Quantità e Qualità Invariate)

**Obiettivo**: Verificare che piante Lvl 1-2 producano frutti con quantità e qualità base.**Setup**:

1. Piantare un seme in un vaso (POT-001)
2. Usare `PotDebugConsole` (premi **P**):

- Selezionare POT-001
- Impostare **Livello = 1**
- Impostare **Cicli Completati = 0**
- Impostare **Stage = 5** (HarvestReady)

**Azioni**:

1. Impostare `AmountFruits = 3` nel debug console (o attendere che la pianta produca naturalmente)
2. Eseguire **Harvest** dalla UI del vaso

**Risultati Attesi**:

- ✅ **Quantità raccolta**: 3 frutti (nessuna riduzione)
- ✅ **Qualità frutti**: `MaxQuality` (da `ItemConfig`, tipicamente 4)
- ✅ **Console Unity Log**: Dovrebbe mostrare `"[ACT-005][POT-001] Harvest OK: raccolti 3 frutti..."`
- ✅ **Verifica Inventario**: Contare i frutti nell'inventario (deve aumentare di 3)

**Verifica Codice** (Console Unity):

```javascript
[Pot] [ACT-005][POT-001] Modificatore resa Lvl 1: 0% (quantità: 3 → 3)
```

**Ripetere per Lvl 2**:

- Impostare Livello = 2, ripetere raccolta
- Risultati attesi: identici a Lvl 1

---

#### Test 2: Verifica Resa Frutti Lvl 3 (Quantità -15%, Qualità +15%)

**Obiettivo**: Verificare che piante Lvl 3 producano frutti con quantità ridotta e qualità aumentata.**Setup**:

1. Usare la stessa pianta o piantarne una nuova
2. Usare `PotDebugConsole`:

- Selezionare vaso
- Impostare **Livello = 3**
- Impostare **Stage = 5** (HarvestReady)

**Azioni**:

1. Impostare `AmountFruits = 3` (o attendere produzione naturale)
2. Eseguire **Harvest**

**Risultati Attesi**:

- ✅ **Quantità base**: 3 frutti
- ✅ **Quantità modificata**: `3 * (1 - 0.15) = 2.55` → **2 frutti** (arrotondato)
- ✅ **Qualità base**: `MaxQuality` (es. 4)
- ✅ **Qualità modificata**: `4 * (1 + 0.15) = 4.6` → **4.6** (o arrotondato a 5 se arrotondato)
- ✅ **Console Unity Log**: 
  ```javascript
      [Pot] [ACT-005][POT-001] Modificatore resa Lvl 3: -15% (quantità: 3 → 2.55)
      [Pot] [ACT-005][POT-001] Qualità frutti Lvl 3: +15% (qualità: 4 → 4.6)
  ```


**Verifica Inventario**:

- Apri inventario e verifica che i frutti raccolti abbiano qualità > `MaxQuality`
- Se l'UI qualità non è visibile, verificare nel codice tramite breakpoint o log

---

#### Test 3: Verifica Resa Frutti Lvl 4 (Quantità -30%, Qualità +30%)

**Setup**:

1. Impostare **Livello = 4** nel debug console
2. Impostare **Stage = 5** (HarvestReady)
3. Impostare `AmountFruits = 3`

**Azioni**: Eseguire Harvest**Risultati Attesi**:

- ✅ **Quantità**: `3 * (1 - 0.30) = 2.1` → **2 frutti**
- ✅ **Qualità**: `4 * (1 + 0.30) = 5.2` → **5.2** (o arrotondato)

---

#### Test 4: Verifica Resa Frutti Lvl 5 (Quantità -45%, Qualità +45%)

**Setup**:

1. Impostare **Livello = 5** nel debug console
2. Impostare **Stage = 5** (HarvestReady)
3. Impostare `AmountFruits = 3`

**Azioni**: Eseguire Harvest**Risultati Attesi**:

- ✅ **Quantità**: `3 * (1 - 0.45) = 1.65` → **2 frutti** (arrotondato)
- ✅ **Qualità**: `4 * (1 + 0.45) = 5.8` → **5.8** (o arrotondato)

---

#### Test 5: Verifica Progressione Cicli Completati

**Obiettivo**: Verificare che i cicli completati incrementino correttamente quando si riattiva da Resting.**Setup**:

1. Piantare una nuova pianta
2. Impostare **Livello = 1**, **Cicli Completati = 0**
3. Impostare **Stage = 6** (Resting)

**Sequenza di Test**:**Ciclo 1 - Verifica Incremento**:

1. Applicare fertilizzante compatibile alla pianta in Resting
2. Verificare transizione: **Resting → Flowering**
3. Verificare nel debug console che **Cicli Completati = 1**
4. Verificare nel log:
   ```javascript
         [Pot] [ACT-015][POT-001] Ciclo completo! Cicli completati: 1
   ```


**Ciclo 2-3 - Progressione verso Lvl 3**:

1. Portare la pianta a **HarvestReady** (cura naturale o debug)
2. Eseguire **Harvest** (va in Resting, ma NON incrementa cicli)
3. Applicare fertilizzante (incrementa cicli e verifica level-up)
4. **Dopo 2° ciclo**: Verificare **Cicli Completati = 2**
5. **Dopo 3° ciclo**: 

- Verificare **Cicli Completati = 3`
- Verificare **Livello = 2** (level-up da 1→2 dopo 1 ciclo)
- Verificare **Livello = 3** (level-up da 2→3 dopo 2 cicli aggiuntivi, totale 3)

---

#### Test 6: Verifica Level-Up alle Soglie Corrette

**Obiettivo**: Verificare che i level-up avvengano alle soglie corrette `[1, 2, 2, 3]`.**Setup Sequenza Completa**:| Ciclo | Cicli Tot | Livello Atteso | Verifica ||-------|-----------|----------------|----------|| Iniziale | 0 | 1 | ✅ Partenza Lvl 1 || 1° ciclo completo | 1 | 2 | ✅ Level-up 1→2 (soglia: 1) || 2° ciclo completo | 2 | 2 | ⚠️ Ancora Lvl 2 (serve 2° ciclo) || 3° ciclo completo | 3 | 3 | ✅ Level-up 2→3 (soglia: 2 cicli) || 4° ciclo completo | 4 | 3 | ⚠️ Ancora Lvl 3 (serve 4° ciclo) || 5° ciclo completo | 5 | 4 | ✅ Level-up 3→4 (soglia: 2 cicli) || 6° ciclo completo | 6 | 4 | ⚠️ Ancora Lvl 4 (serve 6° ciclo) || 7° ciclo completo | 7 | 4 | ⚠️ Ancora Lvl 4 (serve 7° ciclo) || 8° ciclo completo | 8 | 5 | ✅ Level-up 4→5 (soglia: 3 cicli) |**Procedura**:

1. Impostare **Livello = 1**, **Cicli Completati = 0**
2. Per ogni ciclo:

- Portare a Resting (via debug o gameplay)
- Applicare fertilizzante
- Verificare incremento cicli e level-up nel log

3. Verificare che i level-up avvengano **esattamente** alle soglie indicate

**Log Attesi**:

```javascript
[Pot] [ACT-015][POT-001] Ciclo completo! Cicli completati: 1
[Pot] POT-001: Livello aumentato a Lvl 2 (cicli completati: 1)
[Pot] [ACT-015][POT-001] Ciclo completo! Cicli completati: 2
[Pot] [ACT-015][POT-001] Ciclo completo! Cicli completati: 3
[Pot] POT-001: Livello aumentato a Lvl 3 (cicli completati: 3)
```

---

#### Test 7: Verifica CanMoveToPassiveSlot()

**Obiettivo**: Verificare che solo piante Lvl 5 possano essere spostate negli slot passivi.**Setup**:

1. Creare 2 piante:

- Pianta A: **Livello = 4**
- Pianta B: **Livello = 5**

**Test**:

1. Chiamare `PlantLevelSystem.CanMoveToPassiveSlot(piantaA)` → **Risultato atteso: `false`**
2. Chiamare `PlantLevelSystem.CanMoveToPassiveSlot(piantaB)` → **Risultato atteso: `true`**

**Nota**: Questo test richiede l'implementazione del sistema slot passivi (task futuro), ma possiamo verificare il metodo helper in isolamento tramite unit test o console debug.---

#### Test 8: Test Gameplay Completo - Progressione Naturale

**Obiettivo**: Verificare la progressione completa senza uso del debug console (gameplay normale).**Sequenza Completa**:

1. **Piantare seme** in vaso
2. **Gestire crescita naturale**:

- Seed → Sprout → Growth → Flowering → HarvestReady
- Innaffiare e illuminare correttamente

3. **Raccogliere frutti** (HarvestReady → Resting)

- Verificare quantità e qualità in base al livello corrente

4. **Applicare fertilizzante** (Resting → Flowering)

- Verificare incremento cicli completati
- Verificare level-up se raggiunta soglia

5. **Ripetere** fino a raggiungere Lvl 5

**Verifiche durante gameplay**:

- UI mostra livello corretto nella Plant Card
- UI mostra cicli completati (se visualizzato)
- Toast/notifiche per level-up
- Inventario riceve frutti con qualità corretta

---

#### Checklist Finale di Verifica

- [ ] Lvl 1-2: quantità e qualità invariate
- [ ] Lvl 3: quantità -15%, qualità +15%
- [ ] Lvl 4: quantità -30%, qualità +30%
- [ ] Lvl 5: quantità -45%, qualità +45%
- [ ] Cicli completati incrementano solo dopo fertilizzante su Resting
- [ ] Level-up avviene alle soglie corrette: 1, 3, 5, 8 cicli
- [ ] Log debug mostrano modificatori quantità/qualità corretti
- [ ] Inventario riceve frutti con qualità corretta
- [ ] `CanMoveToPassiveSlot()` ritorna `true` solo per Lvl 5
- [ ] Progress bar livello mostra percentuale corretta (se presente in UI)

---

#### Risoluzione Problemi Comuni

**Problema**: Cicli completati non incrementano

- **Verifica**: Assicurarsi che `IncrementCompletedCycle()` sia in `DoFertilize()`, non in `DoHarvest()`

**Problema**: Level-up non avviene

- **Verifica**: Controllare che `cyclesThresholds = [1, 2, 2, 3] `in `PlantLevelConfig`
- **Verifica**: Controllare che `CheckLevelUp()` venga chiamato dopo `IncrementCompletedCycle()`

**Problema**: Qualità frutti non cambia

- **Verifica**: Assicurarsi che `ItemFabric.CreateItemWithQuality()` venga chiamato in `DoHarvest()`
- **Verifica**: Controllare che `GetQualityModifier()` ritorni valori corretti per livello >= 3

**Problema**: Quantità non viene modificata

- **Verifica**: Controllare che `GetQuantityModifier()` in `PlantLevelConfig` ritorni valori negativi per Lvl >= 3
- **Verifica**: Log in console Unity per vedere i calcoli applicati

---

## File da Modificare

1. [`Assets/_Project/Scripts/Dome/PotSystem/Level/PlantLevelConfig.cs`](Assets/_Project/Scripts/Dome/PotSystem/Level/PlantLevelConfig.cs) - Aggiungere `GetQualityModifier()`, correggere soglie `[1, 2, 2, 3]`
2. [`Assets/_Project/Scripts/Core/ItemsSystem/ItemFabric.cs`](Assets/_Project/Scripts/Core/ItemsSystem/ItemFabric.cs) - Aggiungere `CreateItemWithQuality()`
3. [`Assets/_Project/Scripts/Dome/PotActions.cs`](Assets/_Project/Scripts/Dome/PotActions.cs) - Modificare `DoHarvest()` per applicare qualità, spostare `IncrementCompletedCycle()` in `DoFertilize()`
4. [`Assets/_Project/Scripts/Dome/PotSystem/Level/PlantLevelSystem.cs`](Assets/_Project/Scripts/Dome/PotSystem/Level/PlantLevelSystem.cs) - Aggiungere `CanMoveToPassiveSlot()`
5. [`Assets/_Project/Scripts/Debug/PotDebugConsole.cs`](Assets/_Project/Scripts/Debug/PotDebugConsole.cs) - Estendere con visualizzazione modificatori resa, qualità attesa, check slot passivi

---

## Dipendenze

- Sistema inventario esistente (✅ già funzionante)
- Sistema raccolta esistente (✅ già funzionante, da estendere)
- Sistema fertilizzante esistente (✅ già funzionante)
- ItemConfig con `MaxQuality` (✅ già presente)

---

## Note di Implementazione

1. **Qualità frutti**: Il sistema qualità esiste (`Item.Quality`) ma attualmente viene inizializzato a `MaxQuality`. Dobbiamo permettere qualità superiore a `MaxQuality` per rappresentare il bonus qualità.
2. **Clamp qualità**: Considerare se limitare la qualità massima a `MaxQuality * 2` (100% bonus) o permettere valori maggiori.
3. **UI qualità**: Considerare se aggiungere visualizzazione qualità frutti nell'inventario (non incluso in questo task, ma da considerare per il futuro).
4. **Bilanciamento**: La formula quantità/qualità mantiene il valore totale dei frutti bilanciato (quantità * qualità = costante approssimativo).

---

## Criteri di Accettazione

- ✅ Lvl 1-2: quantità e qualità invariate
- ✅ Lvl 3+: quantità ridotta del 15%/livello
- ✅ Lvl 3+: qualità aumentata proporzionalmente (+15%/livello)
- ✅ Frutti creati con qualità corretta in base al livello
- ✅ Cicli completati incrementano correttamente
- ✅ Level-up avviene alle soglie corrette
- ✅ Metodo `CanMoveToPassiveSlot()` disponibile per sistema slot passivi
- ✅ Logging per debug quantità/qualità applicate