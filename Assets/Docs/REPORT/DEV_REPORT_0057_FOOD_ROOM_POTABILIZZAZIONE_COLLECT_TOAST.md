# DEV REPORT 0057 — Food Room: potabilizzazione acqua, Collect/Purify, toast harvest

**Data:** 2026-03-03  
**Scope:** Food Room — flusso potabilizzazione acqua (tempo reale 2 min/unità), pulsante Purify/Collect a tre stati, toast success/collect, harvest con toast multipli (carne + Res Protein), fix counter unità e salvataggio stato acqua.  
**Riferimenti:** `FoodRoomSystem.cs`, `FoodRoomPanelController.cs`, `WaterProductionSlot.cs`, `DayCycleSystem.cs`, `NotificationTypeSpecDefaults.cs`, `FoodRoomPanel.uxml/uss`

---

## 1. Potabilizzazione acqua: tempo reale e coda

### 1.1 Regole
- **2 minuti reali per unità** (`WaterProductionSlot.SecondsPerUnit = 120f`).
- Avanzamento ogni frame con `TickWaterProduction(Time.deltaTime)` chiamato dal panel.
- Quando un’unità termina, la successiva parte in automatico fino a esaurimento coda.
- **Nessuna** assegnazione automatica all’inventario: l’acqua potabile si ritira solo cliccando **Collect** in Kitchen.

### 1.2 WaterProductionSlot
- `RawWaterInput`: unità totali da processare.
- `PotableWaterOutput`: unità pronte da ritirare.
- `CurrentUnitProgress`: progresso 0–1 dell’unità corrente.
- `IsActive`: processo in corso o acqua pronta ancora non ritirata (slot “occupato” fino al collect).

### 1.3 FoodRoomSystem
- `StartWaterProduction(amount)`: consuma RAW water, imposta slot (Output=0, Progress=0, IsActive=true).
- `TickWaterProduction(deltaTime)`: avanza `CurrentUnitProgress`; a ogni unità completata incrementa `PotableWaterOutput`; quando tutte finite imposta `IsActive = false`, rimuove toast progress e invia **KTCH-WAT-DONE** (“Vai in Kitchen e clicca Collect”).
- `AdvanceWaterProductionByRealSeconds(seconds)`: usato in **DayCycleSystem.HandleFaded()** con 8 ore (8*3600f) così al mattino dopo End of Day la potabilizzazione risulta completata.
- `HarvestWater()`: condizione cambiata da `IsActive && PotableWaterOutput > 0` a solo `PotableWaterOutput > 0`, così si può ritirare anche dopo che il processo è finito (IsActive già false).

---

## 2. UI pulsante Purify / Collect (tre stati)

### 2.1 Stati
| Stato | Testo pulsante | Abilitato | Stile | Controlli −/+ |
|-------|----------------|-----------|--------|----------------|
| Processo in corso (nessuna unità ancora pronta) | 💧 COLLECT | No | Grigio | Disattivati |
| Acqua potabile pronta (PotableWaterOutput > 0) | 💧 COLLECT | Sì | Blu (outline + testo) | Disattivati |
| Nessun processo / nessuna acqua da ritirare | 💧 PURIFY | Sì se hasWater e unità ≥ 1 | Blu se abilitato | Attivi |

### 2.2 Logica Refresh()
- `waterProcessActive = IsActive && RawWaterInput > 0`
- `waterReadyToCollect = PotableWaterOutput > 0`
- `showCollectButton = waterProcessActive || waterReadyToCollect`
- Se `showCollectButton`: testo "COLLECT"; abilitato solo se `waterReadyToCollect`; altrimenti grigio e disabilitato.
- Se non showCollectButton: testo "PURIFY", abilitazione e stile come prima.
- Pulsanti −/+ abilitati solo quando `!showCollectButton`.

### 2.3 OnPurify()
- Se `PotableWaterOutput > 0` → chiama `HarvestWater()` e Refresh (collect).
- Altrimenti → avvia potabilizzazione come prima (`StartWaterProduction`).

### 2.4 Harvest
- **Solo cibo:** il pulsante HARVEST è abilitato solo se c’è almeno uno slot biomassa Ready; l’acqua non si ritira da HARVEST ma solo da Collect (ex Purify).

---

## 3. Barra progresso acqua e toast

### 3.1 Barra progresso (blu/celeste)
- Blocco `water-progress-block` in UXML (track, fill, shine) con stili `.water-progress-*` in USS (blu `rgb(93, 182, 227)`, shine celeste).
- Visibile quando `WaterSlot.IsActive && RawWaterInput > 0`.
- Fill = `(PotableWaterOutput + CurrentUnitProgress) / RawWaterInput`; aggiornato in Refresh() e in Update() quando il panel è visibile.
- Shine animato come per la coltivazione (fase left-to-right).

### 3.2 Toast
- **KTCH-WAT-PROGRESS:** template con `{percent}%`; payload `percent` e `count`; aggiornato durante il tick.
- **KTCH-WAT-DONE:** “Acqua potabile pronta (x{count}). Vai in Kitchen e clicca Collect per ritirare.” / “Go to Kitchen and click Collect to retrieve.”
- **KTCH-WAT-RITIRA:** “Raccolto: Acqua Potabile x{count}” / “Collected: Potable Water x{count}” (dopo click Collect).

---

## 4. Altre modifiche Food Room

### 4.1 Counter unità RAW WATER (Purify)
- **Default 0** all’apertura; abilitazione Purify solo con almeno 1 unità e acqua disponibile.
- Reset a 0 **solo in Show()** quando `!WaterSlot.IsActive`, non in Refresh(), per evitare di azzerare il counter a ogni +/− (bug precedente).

### 4.2 Pannello Residual Protein
- Pannello “RESIDUAL PROTEIN” / RES-PROT-001 **sempre nascosto** in Food Room (display: None); l’info sui residui è data dal toast di harvest.

### 4.3 Harvest con toast multipli
- In **Harvest(slotIndex)** dopo il prodotto principale e il toast KTCH-FOOD-RITIRA viene chiamato **AddHarvestBonusItemsAndToasts(type, foundation)**.
- Per **Meat:** aggiunge 1× RES-PROT-001 all’inventario e invia un secondo toast KTCH-FOOD-RITIRA con foodType "Res Protein" e count "1".
- Estendibile ad altri tipi in futuro.

---

## 5. Salvataggio / caricamento

### 5.1 FoodRoomSaveData
- Aggiunto `waterCurrentProgress` (float).
- Serialize: scrive `CurrentUnitProgress` da `WaterSlot`.
- Deserialize / RestoreState: ripristina `CurrentUnitProgress` (e altri campi acqua). Salvataggi vecchi senza il campo restano con 0.

---

## 6. File modificati (riepilogo)

| File | Modifiche |
|------|-----------|
| `WaterProductionSlot.cs` | Campi RawWaterInput, PotableWaterOutput, CurrentUnitProgress, IsActive, SecondsPerUnit. |
| `FoodRoomSystem.cs` | StartWaterProduction (output 0, progress 0); TickWaterProduction; AdvanceWaterProductionByRealSeconds; HarvestWater solo con PotableWaterOutput > 0; RefreshWaterToasts con percent; AddHarvestBonusItemsAndToasts (Meat → Res Protein); RestoreState con waterCurrentProgress. |
| `FoodRoomPanelController.cs` | Purify/Collect a tre stati; OnPurify con branch Collect; OnHarvest solo cibo; counter reset solo in Show(); ref water progress bar e shine; Update: TickWaterProduction, aggiornamento fill acqua. |
| `FoodRoomPanel.uxml` | Blocco water-progress-block (label, track, fill, shine). |
| `FoodRoomPanel.uss` | Stili water-progress-* (blu/celeste); btn-purify--enabled per outline/testo blu. |
| `DayCycleSystem.cs` | HandleFaded: dopo CurrentDay++ chiama FoodRoomSystem.AdvanceWaterProductionByRealSeconds(8*3600f). |
| `SaveManager.cs` | FoodRoomSaveData.waterCurrentProgress; serialize/deserialize e passaggio a RestoreState. |
| `NotificationTypeSpecDefaults.cs` | KTCH-WAT-PROGRESS con {percent}%; KTCH-WAT-DONE con “Collect”; KTCH-WAT-RITIRA “Raccolto: Acqua Potabile x{count}". |

---

## 7. Note per QA

- Avviare potabilizzazione con 1+ unità: pulsante diventa COLLECT disabilitato/grigio; barra blu con percentuale e toast progress con %.
- Attendere fine processo (o End of Day): toast success invita a “clicca Collect”; pulsante COLLECT diventa abilitato e blu; clic → inventario aggiornato, toast “Raccolto: Acqua Potabile x N”.
- Verificare che con processo attivo non si possa avviare un’altra potabilizzazione (counter −/+ disattivati, Purify non disponibile).
- Harvest carne: due toast (“Meat Synthetic x1” e “Res Protein x1”).
- Salvataggio/caricamento: stato potabilizzazione (unità, progresso, IsActive) ripristinato correttamente.

---

*Fine DEV REPORT 0057.*
