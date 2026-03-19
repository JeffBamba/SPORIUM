# DEV REPORT 0072 — Task 3: Slot Passivi Reali — pH Cryo Channel
**Data:** 2026-03-19
**Task:** Task 3 — Slot Passivi Reali
**Roadmap:** `roadmap_dome_lab_100_069d5bdb`

---

## Contesto

Task 3 trasforma gli slot passivi della CryoMachine da segnaposto visivo a meccanica di gameplay reale. Ogni tick giornaliero, le piante Lvl 5 in cryo applicano un drift pH specifico e un cap sul pH della Dome. I valori sono authorati per pianta nei PlantData ScriptableObject e letti a runtime tramite PlantDatabase. L'effetto è rimosso immediatamente quando una pianta lascia il cryo.

---

## Flusso dati end-to-end implementato

```
PlantData (passivePhDrift, passivePhCap, passivePower)
    └─► PlantDatabase.GetPlantDataByCode()
            └─► DayCycleController.ApplyPassivePowers()  [dopo ApplyQueuedDrifts()]
                    └─► PhSystem.RegisterCryoPassiveDrift()
                    └─► PhSystem.ApplyCryoPassiveCaps()
                            └─► TopBarController  (tooltip pH — SLOT PASSIVI CRYO)
                            └─► AlwaysVisiblePotHUD  (riga CRYO STATUS)
```

---

## Modifiche ai file

### 1. `PlantData.cs`

Aggiunto il gruppo `[Header("Passive Slot (CryoMachine)")]` con tre nuovi campi serializzati:

| Campo | Tipo | Default | Descrizione |
|---|---|---|---|
| `passivePower` | `string` (TextArea) | — | Label descrittiva visibile in HUD e tooltip |
| `passivePhDrift` | `float` | 0 | Drift pH/giorno quando in CryoSlot |
| `passivePhCap` | `float` | 0 | Floor (Pure) o ceiling (Evil) del pH; 0 = nessun cap |

Proprietà pubbliche aggiunte: `PassivePower`, `PassivePhDrift`, `PassivePhCap`.
Metodo helper aggiunto: `GetPassivePhDrift()`.

### 2. `PhSystem.cs`

**Struct privata** `CryoContribution` (SlotId, PassivePowerLabel, DailyDrift, PhCap, Day).

**Struct pubblica** `CryoPassiveModifier` (SlotId, PassivePowerLabel, DailyDrift, PhCap).

**Campi aggiunti:**
- `_cryoContributions` — lista delle contribuzioni cryo attive
- `_queuedCryoDrift` — drift cryo da applicare al prossimo tick

**Metodi pubblici aggiunti:**
- `RegisterCryoPassiveDrift()` — registra/aggiorna il contributo di uno slot
- `RemoveCryoPassiveContribution()` — rimuove il contributo immediatamente alla liberazione dello slot
- `ApplyCryoPassiveCaps()` — applica floor/ceiling dopo `ApplyQueuedDrifts()`
- `GetCryoPassiveModifiers()` — letto da TopBar e HUD

**`ApplyQueuedDrifts()`** — include ora `_queuedCryoDrift` nel totale e lo azzera dopo.

**`Reset()`** — pulisce `_cryoContributions` e `_queuedCryoDrift`.

### 3. `SPOR-BLK-01-03A-DayCycleController.cs`

Sostituito lo scaffold `ApplyPassivePowers()` (Task 2 placeholder) con l'implementazione reale:

1. Itera i CryoSlot occupati via `CryoMachineController.GetPassiveSlotsSnapshot()`
2. Per ogni slot: risolve `drift`, `cap` e `label` da `PlantDatabase.GetPlantDataByCode()`
3. Chiama `PhSystem.RegisterCryoPassiveDrift()`
4. Dopo il loop: chiama `PhSystem.ApplyCryoPassiveCaps()`

Ordine nel tick garantito: `CalculateAndRegisterPhDrift` → `ApplyQueuedDrifts` → `ApplyPassivePowers`.

### 4. `PotActions.cs`

`RestoreFromCryo()` e `ExtractFromCryoToStorage()` chiamano ora `PhSystem.RemoveCryoPassiveContribution(cryoSlotId)` immediatamente dopo `cryo.FreeSlot()`. Il contributo passivo viene rimosso senza aspettare il prossimo tick giornaliero.

### 5. `TopBarController.cs`

Il blocco **"SLOT PASSIVI CRYO"** nel tooltip pH è stato aggiornato:

- **Fonte primaria:** `_phSystem.GetCryoPassiveModifiers()` — mostra `±drift/g (cap ±X)` con delta numerico reale
- **Fallback:** se PhSystem non ha ancora contributi (giorno 1 prima del primo tick), legge da `CryoMachineController` mostrando solo label descrittiva
- Formato riga: `[SlotId]  PassivePowerLabel  +1.0/g (cap -20)`

### 6. `AlwaysVisiblePotHUD.cs`

Aggiunta label TMP `_cryoStatusText` ancorata in basso a destra, sopra le 4 HUD dei pot attivi.

- **`CreateCryoStatusLabel()`** — crea il GameObject con RectTransform e colore ciano
- **`UpdateCryoStatusLabel()`** — aggiornata ogni 0.5s; mostra:
  - `CRYO: —` se nessuno slot occupato con drift registrato
  - `CRYO: [2/3] pH +1.0/g -1.0/g` con conteggio slot e lista delta

---

## Authoring PlantData (Unity Editor)

Valori placeholder compilati nei 3 ScriptableObject in `Assets/Resources/Plants/`:

| PlantData | Passive Power | passivePhDrift | passivePhCap |
|---|---|---|---|
| PLT-STD-001 Ferric Fern | "Stabilizzazione baseline pH (placeholder)" | 0 | 0 |
| PLT-PURE-001 Arctic Hask | "Deriva basica latente +1.0/g — floor pH -20" | +1.0 | -20 |
| PLT-EVIL-001 Glasscap Fungus | "Deriva acida latente -1.0/g — ceiling pH +20" | -1.0 | +20 |

I valori sono placeholder bilanciabili via Inspector. I valori definitivi (da GDD Notion) sono obiettivo di Task 4.

---

## Criteri "Done quando" verificati

| Criterio | Stato |
|---|---|
| Pianta Lvl 5 in cryo NON produce come attiva | Garantito (Task 2) |
| Applica bonus latenti (drift pH) | `RegisterCryoPassiveDrift()` in ogni tick |
| Contributo pH cappato correttamente | `ApplyCryoPassiveCaps()` dopo `ApplyQueuedDrifts()` |
| Save/load rispetta lo stato | CryoSlot persiste (Task 2); drift e cap ricalcolati ogni tick |
| UI riflette lo stato | TopBar tooltip (delta numerico) + AlwaysVisiblePotHUD (riga cryo) |

---

## Note tecniche

- Il canale cryo in `PhSystem` è separato dal canale piante attive: non interferisce con `_plantContributions`, `_queuedPlantsDrift` né con il tooltip "MODIFICATORI ATTIVI" esistente.
- `ApplyCryoPassiveCaps()` opera direttamente su `_currentPh` dopo che `ApplyQueuedDrifts()` ha già consolidato tutti i drift — garantisce che il cap sia applicato sul valore finale della giornata.
- La rimozione immediata del contributo in `PotActions` evita che un cryo slot appena liberato continui ad influenzare il pH fino al prossimo tick.
