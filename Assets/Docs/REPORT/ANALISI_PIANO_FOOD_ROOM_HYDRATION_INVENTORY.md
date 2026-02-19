# Analisi piano: Food Room + Player Hydration + Inventory Consumption

**Data analisi:** 2026-02-19  
**Piano:** `food_room_+_player_hydration_+_inventory_consumption_cede86db.plan.md`  
**Riferimenti:** DEV REPORT 0053 (Save/Load), 0054 (EoD Diary), 0055 (EoD sequenza Dawn/Snapshot), codice CondensationSystem, Inventory, SaveManager, DayCycleController, UI.

---

## 1. Riepilogo: il piano è aggiornato?

| Area | Stato | Note |
|------|--------|------|
| **Logica di gioco** | Parzialmente aggiornato | Allineato a Condensation e Inventory; da aggiornare numero di linea DayCycleController e ID residui (ORG-RES-001 vs RES-PROT-001). |
| **Savegames** | Non coperto | Manca completamente: FoodRoomSystem e PlayerHydrationSystem non sono menzionati per salvataggio/caricamento. |
| **UI** | Parzialmente aggiornato | Piano cita solo HUDInventoryItem/HUDInventory (uGUI); l’inventario principale è UIToolkit (PlayerInventoryPanelController). UIFoodRoom non specifica se UIToolkit o uGUI. |

---

## 2. CondensationSystem e integrazione WAT-RAW

**Comportamento attuale (verificato in codice):**

- `CondensationSystem.DayChanged(activePots, hasActiveLed)` aggiorna solo l’**accumulo percentuale** (0–100%). **Non** aggiunge WAT-RAW all’inventario.
- Il WAT-RAW entra in inventario quando il **giocatore clicca “Raccogli”** nella Top Bar:
  - `TopBarController.OnCondensationCollectClicked()` → `GameManager.CollectCondensation()` → `PlayerInventory.Add(Items.Water, reward)`.

**Piano:** Dice che “WAT-RAW viene prodotto automaticamente da CondensationSystem a fine giornata” e “Food Room consuma WAT-RAW dall’inventario”.

- **Interpretazione corretta:** La “produzione” è l’accumulo % a fine giornata; la conversione in item è al click su Raccogli. L’integrazione Food Room ↔ inventario (Food Room consuma WAT-RAW dall’inventario) è corretta.
- **Suggerimento:** Nel piano, chiarire che il WAT-RAW in inventario è “raccolto dal giocatore dalla Top Bar (pulsante Raccogli condensazione)” così non si pensa a un’aggiunta automatica a fine giornata.

**Conclusione:** Logica Condensation ↔ Food Room **allineata**; solo chiarezza testuale consigliata.

---

## 3. Inventory e Items

**Inventory.cs**

- Esiste `Consume(string typeId, int quantity)` (linee 84–97).
- **Non** esiste `ConsumeItem()` né evento `OnItemConsumed`.
- La Fase 4.1 del piano (estensione con `ConsumeItem()` e evento) è **ancora da fare** e coerente con il codice.

**Items.cs**

- **Già presenti:** `StemCellVegetable` (CELL-001), `StemCellFungus` (CELL-002), `StemCellAnimal` (CELL-003).
- **Residui:** In codice c’è `ProteinResidue = "RES-PROT-001"`. Nel piano si usa **"ORG-RES-001"** (Organic Residue). Va deciso un solo ID:
  - Opzione A: aggiungere `OrganicResidue = "ORG-RES-001"` e usare quello per la Food Room (residui da carne).
  - Opzione B: usare ovunque `ProteinResidue` / `RES-PROT-001` e aggiornare il piano.
- **Mancanti:** `FoodVegetable` (FOOD-101), `FoodFungus` (FOOD-201), `FoodMeat` (FOOD-301), `WaterPotable` (WAT-POT). Da aggiungere come nel piano (dopo le costanti esistenti; il piano dice “dopo linea 24” ma la struttura è cambiata: usare la posizione logica dopo gli item Lab/cellule).

**Conclusione:** Fase 1 e 4.1 restano valide; **allineare** il piano a un solo ID per i residui (ORG-RES-001 vs RES-PROT-001) e aggiornare `AllTypeIds` per i nuovi item.

---

## 4. End of Day e DayCycleController (DEV REPORT 0055)

**Flusso attuale:**

1. Giocatore clicca Sleep (Forecast) → `EndOfDaySequenceController` avvia coroutine.
2. Step 6 (Hibernation) → Step 7 (Day transition) → `_dayCycleSystem.EndDay()`.
3. `DayCycleSystem.EndDay()` invoca `OnDayChanged(CurrentDay)`.
4. Si eseguono in ordine di sottoscrizione: ad es. `GameManager.HandleDayChanged`, `DayCycleController.HandleDayChanged`, `EndOfDaySequenceController.OnDayChanged`, ecc.

**GameManager.HandleDayChanged (linee 160–172):**

- Attualmente: `_economySystem.Spend(_dailyPowerCost)` e `_actionSystem.ResetActions(_actionsPerDay)`.
- Il piano chiede: processare prima l’idratazione (`PlayerHydrationSystem.ProcessDailyConsumption()`), poi resettare le azioni con modificatore idratazione (`GetActionModifier()`). La **logica** del piano è corretta; va solo implementata.

**DayCycleController.HandleDayChanged:**

- Ordine attuale: CheckWatering → ResolveGrowth → ApplyWatering → ApplyLed → pH → Decay → CalculatePlantConditions → **ApplyCondensationSystem** (step 7, linee 434–436).
- Il piano dice: “Aggiungere dopo linea 427” il blocco Food Room. Oggi **la linea 427** è in un altro blocco (pH). Il punto giusto è **dopo `ApplyCondensationSystem(dayIndex);`** (dopo linea **436**), non 427.
- Codice da inserire (dopo linea 436):

```csharp
// 8. Processa Food Room (produzione, costi, harvest disponibili)
if (_foodRoomSystem != null)
{
    _foodRoomSystem.ProcessDailyProduction(dayIndex);
    _foodRoomSystem.ProcessDailyCosts();
}
```

**Conclusione:** Aggiornare il piano: **sostituire “dopo linea 427” con “dopo ApplyCondensationSystem(dayIndex); (dopo linea 436)”**. Inoltre DayCycleController dovrà avere un riferimento a `_foodRoomSystem` (ottenuto da ServiceContainer o GameManager, come per altri sistemi).

---

## 5. Savegames (SaveManager) – gap importante

**Contenuto attuale di `GameSaveData` (SaveManager):**

- `gameState`: currentDay, currentCRY, actionsLeft, condensationAmount.
- `inventory`, `pots`, `diaryStatistics`, `missions`, `diaryNotes`, `stemCellModuleUnlocked`, timestamp, versioni.

**Non salvati:**

- Stato **FoodRoomSystem**: slot di produzione (tipo, giorni rimanenti, giorno inizio, cellula staminale, stato), slot idrico (input WAT-RAW, output WAT-POT, attivo).
- Stato **PlayerHydrationSystem**: livello idratazione (es. percentuale 0–100% o stato).

**Cosa aggiungere al piano (nuova sottosezione “Save/Load”):**

1. **FoodRoomSystem**
   - In `CollectSaveData`: serializzare lista slot produzione (tipo, daysRemaining, startDay, hasStemCell, stemCellTypeId, state) e slot idrico (rawWaterInput, potableWaterOutput, isActive).
   - In `ApplySaveData`: ripristinare FoodRoomSystem da questi dati (dopo che GameManager e sistemi sono stati applicati).
   - Estendere `GameSaveData` con una classe tipo `FoodRoomSaveData` (o campi equivalenti).

2. **PlayerHydrationSystem**
   - In `CollectSaveData`: salvare `hydrationPercent` (o lo stato equivalente).
   - In `ApplySaveData`: ripristinare idratazione al caricamento.
   - Aggiungere in `GameStateData` un campo es. `hydrationPercent`, oppure una struct dedicata.

3. **Versioning**
   - Considerare un `foodRoomSaveVersion` o includere nella `gameVersion`/formato save per compatibilità futura.

**Conclusione:** Il piano **non** è aggiornato per il Save/Load. Va aggiunta una sezione esplicita per persistenza di FoodRoomSystem e PlayerHydrationSystem e relative modifiche a `SaveManager`/`GameSaveData`.

---

## 6. UI: UIToolkit vs uGUI e dove implementare il consumo

**Situazione attuale:**

- **Inventario principale (lista oggetti giocatore):**  
  `PlayerInventoryPanelController` (UIToolkit: UXML, USS, `PlayerInventoryPanel.uxml`). È il “componente unico e definitivo” per l’inventario (tasto INV / Biologo). Tooltip e nomi item sono gestiti lì (anche in base alla rule `item-tooltip.mdc`).
- **HUDInventory / HUDInventoryItem:** uGUI (VaultMap), con `HUDItemContainer`. Possono essere usati in altri contesti (es. HUD compatta).

**Piano attuale:**

- Fase 4.3 e 4.4 citano **HUDInventoryItem** (campo `IsConsumable`, `SetConsumable`, menu “Usa”) e **InventoryConsumeUI**, integrati in **HUDInventory**.

**Problema:**

- Se l’inventario “principale” con cui il giocatore mangia/beve è quello UIToolkit, il **consumo item (Mangia/Bevi)** va implementato soprattutto in **PlayerInventoryPanelController** (UIToolkit), non solo in HUDInventoryItem.

**Raccomandazioni:**

1. **Consumo item**
   - Aggiungere al piano:
     - In **PlayerInventoryPanelController** (modalità view): pulsante/azione “Usa” / “Mangia” / “Bevi” per gli item consumabili nella lista (es. nella riga item o in un menu contestuale); chiamata a `Inventory.ConsumeItem(typeId, 1)` e chiusura tooltip/aggiornamento lista.
   - Mantenere anche le modifiche a **HUDInventoryItem** e **HUDInventory** se quel pannello è ancora usato per mostrare/usare item in qualche flusso.
   - **ItemConsumptionHandler** e evento `OnItemConsumed` restano come nel piano (unico punto di applicazione effetti idratazione/azioni).

2. **UIFoodRoom**
   - Il piano colloca **UIFoodRoom** in `UI/VaultMap/FoodRoom/` (stile VaultMap/uGUI).
   - Nel progetto, End of Day e inventario principale sono in **UIToolkit**. Per coerenza e manutenibilità è ragionevole implementare la **UI Food Room in UIToolkit** (es. `UI/UIToolkit/FoodRoom/FoodRoomPanel.uxml` + `FoodRoomPanelController.cs`), a meno che non si voglia mantenere le macchine VaultMap tutte in uGUI.
   - Suggerimento: **aggiornare il piano** indicando “UIFoodRoom con UIToolkit (UXML/USS + controller)” e il path `UI/UIToolkit/FoodRoom/`, oppure documentare la scelta “solo uGUI” se è intenzionale.

**Conclusione:** Il piano è **parzialmente** aggiornato per la UI: va esplicitamente inclusa l’implementazione del **consumo** nell’inventario **UIToolkit** (PlayerInventoryPanelController) e definita la scelta UIToolkit vs uGUI per UIFoodRoom.

---

## 7. Checklist aggiornamenti al piano

- [ ] **Items / residui:** Decidere ID unico per residui proteici (ORG-RES-001 vs RES-PROT-001) e aggiornare piano e `Items.cs`/`AllTypeIds` di conseguenza.
- [ ] **DayCycleController:** Cambiare “dopo linea 427” in “dopo `ApplyCondensationSystem(dayIndex);` (dopo linea 436)” e riferimento a `_foodRoomSystem`.
- [ ] **Condensation:** Aggiungere una frase che chiarisca che il WAT-RAW in inventario è raccolto dal giocatore dalla Top Bar.
- [ ] **Savegames:** Aggiungere sezione “Save/Load” con: persistenza FoodRoomSystem (slot produzione + slot idrico), persistenza PlayerHydrationSystem (idratazione), estensione `GameSaveData` e modifiche a `CollectSaveData`/`ApplySaveData`.
- [ ] **UI inventario:** In Fase 4, includere **PlayerInventoryPanelController** (UIToolkit) per il consumo item (Usa/Mangia/Bevi); mantenere o meno HUDInventoryItem a seconda dell’uso reale del HUD.
- [ ] **UI Food Room:** Decidere e documentare se UIFoodRoom è UIToolkit (consigliato) o uGUI e aggiornare path/file nel piano.

---

*Fine analisi.*
