# DEV REPORT 0051 — Lab Extractor, Catalizzatore, Pipette: UX, 3 slot, toast e Pre-Seed

**Data:** 2026-02-01  
**Scope:** Laboratorio (Extractor, Catalizzatore, Pipette, Incubatore), inventario spore/Pre-Seed, Foundation Notifications.

---

## 1. Spore: nessun item senza status

- **Regola:** La "Spore Generica" senza status non esiste come item; ogni spora ha sempre uno status (Raw o Maturata + tipo genetico).
- **ItemFabric.cs:** `CreateItemByType(Items.SporeGeneric)` ora restituisce sempre una spora con metadata (Raw + Stabile) tramite `CreateSporeWithFallbackMetadata()`.
- **SaveManager.cs:** In deserializzazione inventario, le voci `SporeGeneric` vengono sempre caricate come spore con metadata (Raw + Stabile); rimossa dipendenza da `inventoryVersion` per le spore.
- **PlayerInventoryPanelController:** Per le spore non maturate la label in inventario è "Spora Raw" (non "Spora generica").

---

## 2. Fix UI pannelli Lab (X chiude, Seleziona apre inventario)

- **Problema:** Su Extractor, Catalizzatore e Pipette il pulsante X non chiudeva il pannello e "Seleziona" (INPUT) non apriva l’inventario.
- **Soluzione (stessa per tutti e tre):**
  - Pattern **TryBindUI()**: binding ritardato con aggiornamento di `_root` se l’albero è stato ricreato (es. dopo Hide/Show).
  - **Show():** all’inizio `gameObject.SetActive(true)` e subito dopo `TryBindUI()`.
  - **Pulsante chiudi:** sui figli del bottone `pickingMode = PickingMode.Ignore`; handler dedicato (es. `OnCloseClicked()`); `RegisterCallback<ClickEvent>(..., TrickleDown.TrickleDown)` con `StopPropagation()`.
- **File toccati:** `LabExtractorPanelController`, `LabCatalizzatorePanelController`, `LabFusionPanelController`. Pipette: anche fallback `panelSettings` da altro UIDocument in Awake.

---

## 3. Extractor: End of Day completa l’estrazione

- Se l’estrazione è in corso e viene triggerato l’End of Day, al giorno successivo l’estrazione risulta completata con output corretti e toast di successo.
- **Extractor.cs:** sottoscrizione a `DayCycleSystem.OnDayChanged`; in `HandleDayChanged` se uno slot è InProgress la coroutine viene fermata, gli output pianificati vengono applicati allo slot, stato → Completed, rimozione toast progress e invio toast LAB-EXT-DONE.

---

## 4. Pulsante "Seleziona INPUT" durante processo

- **Extractor:** "Seleziona INPUT" disabilitato solo durante estrazione (`!inProgress`). Con 3 slot, "Seleziona" è sempre abilitato così si può aggiungere frutta anche con estrazioni in corso.
- **Catalizzatore:** "Seleziona Input" sempre abilitato (per poter caricare fino a 3 spore in parallelo).

---

## 5. Extractor: 3 processi in parallelo

- **Modello:** Come il Catalizzatore, fino a 3 estrazioni contemporanee.
- **Extractor.cs:**
  - `_slotStates[3]` (0 = vuoto, 1 = in corso, 2 = completato), `_slotProgress[3]`, output per slot (`_slotSpore`, `_slotCell001/002/003`), `_slotCoroutines[3]`.
  - `TryStartExtraction()` usa il primo slot libero (`FreeSlotIndex()`), consuma 1 input + 1 azione, avvia la coroutine per quello slot.
  - `CollectOutput()` somma gli output di tutti gli slot completati e li assegna al giocatore, poi azzera gli slot.
- **LabExtractorPanelController:** "Avvia" abilitato se esiste almeno uno slot libero, c’è input e azioni; "Seleziona INPUT" sempre abilitato.

---

## 6. Catalizzatore: 3 slot di maturazione

- Fino a 3 spore in maturazione in parallelo; 1 azione per ogni "Avvia maturazione".
- **Stato:** `_slotStates[3]` (0 = vuoto, 1 = giorno 1, 2 = giorno 2, 3 = pronto). Transizione giorno 1 → 2 → 3 come prima (2 giorni).
- Toast di progresso e completamento **per slot:** chiavi `catalizzatore-progress-{i}` e `catalizzatore-done-{i}`.
- "Seleziona Input" sempre abilitato per poter riempire lo storage e avviare altri processi.

---

## 7. Pipette (Fusione): 2 slot obbligatori + processo 2 minuti

- **UX:** Due slot distinti in UI ("Slot 1 — Spora Maturata", "Slot 2 — Spora Maturata"), entrambi obbligatori per avviare la fusione.
- **UXML:** Sostituita la singola riga "Spore (2 richieste)" con due righe (`lab-fus-slot1-text`, `lab-fus-slot2-text`, `btn-select-slot1`, `btn-select-slot2`).
- **Logica:** Solo spore **Maturate**; picker con `ShowAsPicker(..., filterSporeStage: SporeStage.Matured)` e callback che usa `ConsumeSporeByStage(SporeStage.Matured, 1)` e `ItemFabric.CreateSporeMatured()` per lo storage Pipette.
- **Processo:** Durata 120 secondi; coroutine `RunFusion()` con toast LAB-FUS-PROGRESS (percent) e a fine LAB-FUS-DONE. Durante la fusione pulsanti slot e "Avvia fusione" disabilitati.

---

## 8. Pre-Seed come item distinto

- **Items.cs:** Aggiunta costante `Items.PreSeed = "pre-seed"` (non in `AllTypeIds`, quindi non in inventario iniziale).
- **Resources/Items/pre-seed.asset:** Creato `ItemConfig` per `pre-seed`.
- **Pipette ritiro:** In `OnRitiraClicked` si usa `PlayerInventory.Add(Items.PreSeed, _outputPreSeedCount)` al posto di `SporeGeneric`.
- **Incubatore:** Accetta e consuma `Items.PreSeed` (input "Pre-Seed", `IncubatorAllowedTypes()`, `Has(Items.PreSeed)`, `Consume(Items.PreSeed, 1)`).
- **Inventario:** `GetItemDisplayName(Items.PreSeed)` restituisce "Pre-Seed".

---

## 9. Toast di successo con quantità (x1, x2, x3)

- **Specifiche aggiornate (NotificationTypeSpecDefaults):**
  - LAB-EXT-DONE: "Estrazione completata. Ritira output (x{count}) dall'Extractor."
  - LAB-CAT-DONE: "Spora maturata pronta (x{count}). Ritira dal Catalizzatore."
  - LAB-FUS-DONE: "Fusione completata. Ritira Pre-Seed (x{count}) dalla Pipette."
- Chiamate ai toast (Extractor, Catalizzatore, Pipette) passano `With("count", ...)` o `With("amount", ...)` dove previsto.

---

## 10. Toast di progresso Extractor: uno per processo

- Invece di una sola chiave `extractor-progress`, ogni slot usa `extractor-progress-{slot}` (0, 1, 2).
- All’avvio di uno slot: `UpsertToast(ExtractorProgressToastKey(idx), "LAB-EXT-START", ...)`.
- In `RunExtraction(slotIndex)`: aggiornamento e rimozione solo del toast di quello slot. In `HandleDayChanged` rimozione del toast di progresso per ogni slot completato.

---

## 11. Toast di ritiro/raccolta con quantità

- **Extractor:** Aggiunto **LAB-EXT-RITIRA** ("Output ritirato (x{count}) dall'Extractor."). In `OnRitiraClicked` si usa `CompletedCount()` prima di `CollectOutput` e si invia LAB-EXT-RITIRA con `count`.
- **Catalizzatore:** LAB-CAT-RITIRA già con "Raccolta Spore Maturata x{amount}" e payload `amount`.
- **Pipette:** LAB-FUS-RITIRA già con "Pre-Seed ritirato (x{count}) dalla Pipette." e payload `count`.
- **Incubatore:** LAB-INC-OK aggiornato a "Incubazione riuscita! Semi ottenuti (x{count})." e in `OnRitiraClicked` si passa `With("count", count.ToString())`.

---

## 12. Foundation Notifications: 5 notifiche visibili

- **FoundationNotificationService.cs:** `MaxVisibleRows` da 3 a **5**.
- **FoundationNotificationsPanelController.cs:** `_rows` da 5 elementi; binding di `_rows[3]` e `_rows[4]` in `SetupUI()`.
- **NotificationsPanel.uxml:** Aggiunte le righe `nf-row-3` e `nf-row-4` con stessa struttura delle altre.

---

## 13. Pipette: toast Pre-Seed (non Spore)

- Toast di ritiro Pipette: da LAB-GRF-OK a **LAB-FUS-RITIRA** ("Pre-Seed ritirato (x{count}) dalla Pipette.") per coerenza con il macchinario e l’item.

---

## File modificati (principali)

| Area | File |
|------|------|
| Core items | `Items.cs`, `ItemFabric.cs`, `Inventory.cs`, `SaveManager.cs` |
| Lab UI | `LabExtractorPanelController.cs`, `LabCatalizzatorePanelController.cs`, `LabFusionPanelController.cs`, `LabIncubatorPanelController.cs` |
| Lab logic | `Extractor.cs` |
| Inventario | `PlayerInventoryPanelController.cs` |
| Notifications | `NotificationTypeSpecDefaults.cs`, `FoundationNotificationService.cs`, `FoundationNotificationsPanelController.cs`, `NotificationsPanel.uxml` |
| Assets | `LabFusionPanel.uxml`, `Resources/Items/pre-seed.asset` |

---

*Fine DEV REPORT 0051.*
