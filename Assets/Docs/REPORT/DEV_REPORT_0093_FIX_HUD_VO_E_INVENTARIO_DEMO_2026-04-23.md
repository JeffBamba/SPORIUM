# DEV REPORT 0093 — Fix HUD fissa durante VO demo + lock inventario iniziale demo

**Data:** 2026-04-23  
**Sprint / contesto:** Demo Alpha — bugfix regressione HUD fissa durante VO della missione Seed Storage + allineamento bootstrap inventario solo demo.  
**Riferimento piano:** `.cursor/plans/demo_alpha_1_0_gap_map.plan.md`  
**Report precedente:** `DEV_REPORT_0092_HUD_MODALI_SEED_STORAGE_PLANTCARD_LAB_FOOD_2026-04-21.md`

---

## Sommario interventi

1. **Fix regressione HUD fissa durante VO:** separato il concetto "blocca input mondo" da "nascondi HUD fissa", evitando che il VO demo nasconda la HUD.
2. **Comportamento modali macchina preservato:** i pannelli macchina continuano a nascondere la HUD fissa quando aperti.
3. **Inventario iniziale demo (runtime):** in demo il player parte solo con `5x WAT-POT` e `2x FOOD-101`; nessun altro item iniziale.

---

## 1. HUD fissa e VO demo

### Problema
- Dopo la modifica del report 0092, la HUD fissa veniva nascosta ogni volta che `GameplayUiModalLock.BlocksWorldInput` era `true`.
- Il VO demo usa `lockWorldInputWhileVisible: true`; quindi durante il VO "Vai al Seed Storage" la HUD fissa spariva, comportamento non voluto.

### Soluzione
- In `GameplayUiModalLock` e stata introdotta una separazione esplicita:
  - `BlocksWorldInput` = blocco input mondo.
  - `HidesFixedHud` = visibilita HUD fissa.
- I controller della HUD fissa ora leggono `HidesFixedHud` invece di `BlocksWorldInput`.

**File interessati:**  
`GameplayUiModalLock.cs`, `TopBarController.cs`, `CompactBottomBarController.cs`, `BottomNavigationController.cs`, `FoundationNotificationsPanelController.cs`, `DomeStatusHUDController.cs`

---

## 2. Modali macchina (comportamento confermato)

### Problema
- Serviva mantenere il comportamento introdotto in 0092: con pannelli macchina aperti la HUD fissa deve restare nascosta.

### Soluzione
- Aggiunto helper centralizzato `SetMachineModalState(bool isOpen)` in `GameplayUiModalLock`.
- I controller modali macchina ora usano questo helper in apertura/chiusura, impostando insieme blocco input mondo + hide HUD fissa.

**File interessati:**  
`SeedStoragePanelController.cs`, `LabTerminalPanelController.cs`, `FoodRoomPanelController.cs`, `PlantCardV3TerminalController.cs`

---

## 3. Inventario iniziale: lock solo demo

### Problema
- Il bootstrap inventario iniziale caricava lo starter inventory completo anche in sessione demo.
- Requisito UX demo: partire solo con acqua potabile e vegetali sintetici.

### Soluzione
- In `GameManager.InitializeSystems()` e stato applicato branching su `isDemo`:
  - **Demo:** `_playerInventory.Add(Items.WaterPotable, 5)` + `_playerInventory.Add(Items.FoodVegetable, 2)`.
  - **Full game:** invariato il percorso precedente con `Items.StarterInventoryTypeIds`.

**File interessato:**  
`GameManager.cs`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/Core/GameplayUiModalLock.cs` | Nuovo stato `HidesFixedHud` + helper `SetMachineModalState` |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs` | Hide HUD legato a `HidesFixedHud` |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/CompactBottomBarController.cs` | Hide HUD legato a `HidesFixedHud` |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/BottomNavigationController.cs` | Hide HUD legato a `HidesFixedHud` |
| `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/FoundationNotificationsPanelController.cs` | Hide HUD legato a `HidesFixedHud` |
| `Assets/_Project/Scripts/UI/UIToolkit/DomeStatusHUD/DomeStatusHUDController.cs` | Hide HUD legato a `HidesFixedHud` |
| `Assets/_Project/Scripts/UI/UIToolkit/SeedStorage/SeedStoragePanelController.cs` | Apertura/chiusura modale via `SetMachineModalState` |
| `Assets/_Project/Scripts/UI/UIToolkit/Lab/LabTerminalPanelController.cs` | Apertura/chiusura modale via `SetMachineModalState` |
| `Assets/_Project/Scripts/UI/UIToolkit/FoodRoom/FoodRoomPanelController.cs` | Apertura/chiusura modale via `SetMachineModalState` |
| `Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs` | Apertura/chiusura modale via `SetMachineModalState` |
| `Assets/_Project/Scripts/Core/GameManager.cs` | Bootstrap inventario demo-only (`5x WAT-POT`, `2x FOOD-101`) |

---

## Regole / vincoli rispettati

- Fix applicato senza revert delle migliorie del report 0092.
- Requisito utente rispettato: inventario iniziale custom **solo** in demo (`isDemo`), non funzionalita both.
- Nessuna nuova scena o fork runtime separato: stesso binario con gating demo.

---

## Note operative (Unity)

- **Test VO demo:** durante la missione "Vai al Seed Storage", al VO la HUD fissa deve restare visibile.
- **Test modali macchina:** con Seed Storage / PlantCardV3 / Lab Terminal / Food Room aperti, la HUD fissa deve sparire.
- **Test inventario demo:** avvio `Gioca Demo` da nuova sessione senza load -> inventario iniziale contiene solo `WAT-POT x5` e `FOOD-101 x2`.
- **Test full game:** `Nuova partita` non demo mantiene lo starter inventory standard.

---

*Fine DEV REPORT 0093.*
