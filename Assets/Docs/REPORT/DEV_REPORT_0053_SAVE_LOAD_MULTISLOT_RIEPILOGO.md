# DEV REPORT 0053 — Save/Load multi-slot, riepilogo partita, popup Salva

**Data:** 2026-02-12  
**Scope:** Sistema salvataggio (slot multipli, riepilogo per UI), menu Save/Load (popup scelta slot, info partita), New Game vs auto-load.

---

## 1. Slot multipli e riepilogo in SaveManager

- **Slot disponibili:** Tre slot fissi: `"default"`, `"slot2"`, `"slot3"` (in UI: **Slot 1**, **Slot 2**, **Slot 3**). Costante `SaveManager.SlotNames` e helper `GetSlotDisplayName(slotName)`.
- **Riepilogo partita (per UI):** Struct pubblica **`SaveSlotSummary`** con:
  - `day` (giorno di gioco),
  - `cry` (CRY disponibili),
  - `plantsInDome` (numero di vasi con pianta),
  - `timestamp` (data/ora salvataggio).
- **Persistenza riepilogo:** In **SaveGame(slotName)** oltre a file e PlayerPrefs del save completo viene salvato il riepilogo in `PlayerPrefs` (chiave `Sporium_Save_{slot}_summary`) in JSON, così la schermata Load/Save può mostrare le info senza caricare l’intero save.
- **GetSaveSummary(slotName):** Restituisce `SaveSlotSummary?`: prima legge da PlayerPrefs; se assente (save vecchi) legge il file e deserializza per estrarre giorno, CRY e conteggio piante, poi restituisce il riepilogo.
- **DeleteSave:** Elimina anche la chiave `_summary` da PlayerPrefs.

---

## 2. Popup Save: scegliere su quale slot salvare

- **Comportamento:** Il pulsante **Save** del menu non salva più direttamente su "default"; apre lo stesso popup degli slot in **modalità Salva**.
- **UI:** Tre righe (Slot 1, Slot 2, Slot 3). Per ogni slot:
  - Testo: **"Slot N — Giorno X, Piante in Dome Y, CRY Z — data/ora"** se esiste un save, altrimenti **"Slot N — Vuoto (salva qui)"**.
  - Pulsante **Salva**: sovrascrive quello slot. Dopo il salvataggio: toast "Salvataggio completato" (SYS-003) e refresh della lista.
- **MainMenuOptions.HandleSave:** Apre il popup con `ShowSlotsPopup(forSave: true)` invece di chiamare `SaveGame("default")`.
- **MainMenuScreens:** `ShowSlotsPopup(bool forSave = false)` imposta la modalità sul controller (`SetSaveMode(forSave)`) e mostra il popup; il controller aggiorna le righe in base alla modalità.

---

## 3. Popup Load: riepilogo partita e Carica/Elimina

- **Comportamento:** Il pulsante **Load** apre il popup in **modalità Carica** con le stesse tre righe.
- **UI per slot:** Per ogni slot con save:
  - Testo: **"Slot N — Giorno X, Piante in Dome Y, CRY Z — data/ora"** (stesso formato del riepilogo).
  - Pulsante **Carica**: carica la partita e chiude il menu.
  - Pulsante **Elimina**: elimina il save e aggiorna la lista. Mostrato solo in modalità Carica e solo se lo slot ha un save.
- **Slot vuoti:** Testo "Slot N — Nessun salvataggio"; pulsante Carica nascosto.

---

## 4. SaveSlotsPopupController: multi-slot e modalità

- **Righe dinamiche:** Il prefab Menu ha un solo elemento "Slot" sotto il Panel del SlotsPopup; a runtime il controller **clona** il primo figlio del Panel per avere **3 righe** (Slot 1, 2, 3), senza modificare il prefab.
- **Modalità:** `SetSaveMode(bool isSaveMode)`: `true` = Salva (pulsante "Salva" per ogni slot), `false` = Carica (pulsante "Carica" e "Elimina" dove applicabile).
- **RefreshSlots():** Per ogni slot in `SaveManager.SlotNames` aggiorna label (da `GetSaveSummary` o testo "Vuoto"/"Nessun salvataggio"), abilita/nasconde pulsanti e registra listener (OnSaveSlot, OnLoadSlot, OnDeleteSlot).
- **EnsureSlotsController / chiamata da MainMenuScreens:** Se il popup non ha il componente, viene aggiunto a runtime; dopo l’apertura viene sempre chiamato `RefreshSlots()`.

---

## 5. Fix precedenti (riferimento)

- **New Game non deve caricare il save:** `GamePlayInstaller.SkipAutoLoad` (static): se `true` all’avvio della scena di gioco non viene eseguito l’auto-load del save "default". **HandleNewGame** imposta `SkipAutoLoad = true` prima di `LoadScene`, così la partita parte da zero.
- **Load mostrava schermata vuota:** Il popup slot non era collegato a SaveManager; introdotto **SaveSlotsPopupController** che all’apertura popola le righe da SaveManager e collega i pulsanti a Load/Save/Delete.

---

## File modificati (principali)

| Area | File |
|------|------|
| Core Save | `SaveManager.cs` (SlotNames, GetSlotDisplayName, SaveSlotSummary, persistenza _summary, GetSaveSummary, DeleteSave _summary) |
| Menu UI | `SaveSlotsPopupController.cs` (multi-slot, SetSaveMode, RefreshSlots, OnSaveSlot / OnLoadSlot / OnDeleteSlot) |
| Menu | `MainMenuScreens.cs` (ShowSlotsPopup(bool forSave)) |
| Menu | `MainMenuOptions.cs` (HandleSave apre popup; HandleLoad con forSave: false) |

---

*Fine DEV REPORT 0053.*
