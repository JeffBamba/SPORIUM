# DEV REPORT 0092 — HUD fissa e overlay modali: Seed Storage, PlantCardV3, Lab Terminal, Food Synth; uniformità dim

**Data:** 2026-04-21  
**Sprint / contesto:** Demo Alpha — UX UI Toolkit per pannelli modali (EXT-002 Seed Storage e terminali collegati), coerenza HUD visibile e layer scuro su game view.  
**Riferimento piano:** `demo_alpha_1_0_gap_map` (traccia feature; beat narrativi non oggetto di questo report).  
**Report precedente:** `DEV_REPORT_0091_DEMO_VO_BEAT23_SEED_STORAGE_UX_2026-04-21.md`

---

## Sommario interventi

1. **HUD fissa (TopBar, Compact Bottom Bar, Bottom Navigation legacy, pannello Notifications Foundation):** nascosta quando `GameplayUiModalLock.BlocksWorldInput` è attivo, così con pannelli modali aperti resta visibile solo il contenuto modale oltre a Mission Recap e Player Status (già gestiti a parte).
2. **`DomeStatusHUD`:** già allineato al lock modale (nascosto quando un modale blocca input mondo).
3. **`SeedStoragePanelController` / interazione:** apertura/chiusura con `GameplayUiModalLock` (coerenza con altri pannelli).
4. **`PlantCardV3TerminalController`:** allineamento lock modale su apertura/chiusura terminale; `SuppressOtherUi` non nasconde più `PlayerStatusPanel` (stesso comportamento degli altri modali: Mission Recap + Player Status).
5. **`LabTerminalPanelController` e `FoodRoomPanelController`:** su `Show()` / `Hide()` impostato `GameplayUiModalLock` per coerenza con Seed Storage e PlantCardV3.
6. **Overlay nero semi-trasparente:** uniformato a **`rgba(0, 0, 0, 0.65)`** su Seed Storage, Lab Terminal, Food Room (root) e dim runtime PlantCardV3 (`UnifiedModalDimAlpha`).

---

## 1. HUD fissa e lock modale

### Problema
- Con pannelli come Seed Storage o terminali aperti, restavano visibili elementi della HUD fissa (barre superiore/inferiore, notifiche laterali), riducendo la leggibilità del modale e allineamento con l’intento “solo pannello + game view dietro”.

### Soluzione
- I controller della HUD fissa leggono `GameplayUiModalLock.BlocksWorldInput` e impostano `display: none` sul root (e dove serve chiudono tooltip flottanti) per evitare residui sopra il modale.
- I pannelli modali rilevanti impostano il lock in `Show`/`Open` e lo rilasciano in `Hide`/`Close`.

**File:** `TopBarController.cs`, `CompactBottomBarController.cs`, `BottomNavigationController.cs`, `FoundationNotificationsPanelController.cs`, `SeedStoragePanelController.cs`, `PlantCardV3TerminalController.cs`, `LabTerminalPanelController.cs`, `FoodRoomPanelController.cs`

---

## 2. PlantCardV3 — Player Status visibile come gli altri modali

### Problema
- La soppressione UI del terminale nascondeva anche `PlayerStatusPanel`, mentre gli altri modali lasciavano visibile lo stato giocatore accanto al Mission Recap.

### Soluzione
- Rimosso `PlayerStatusPanel` dalla lista dei GameObject la cui `UIDocument` viene nascosta in `SuppressOtherUi()`.

**File:** `PlantCardV3TerminalController.cs`

---

## 3. Uniformità livello scuro (dim) sopra la game view

### Problema
- Opacità dell’overlay diversa tra pannelli (es. Lab più chiaro, Seed Storage più scuro, PlantCard con alpha runtime variabile).

### Soluzione
- Valore unico **`0.65`** di alpha sul nero per overlay USS e per il dim runtime del terminale Pot (`UnifiedModalDimAlpha`), così la percezione è coerente tra tutti i pannelli citati.

**File:** `SeedStoragePanel.uss`, `LabTerminalPanel.uss`, `FoodRoomPanel.uss` (già a 0.65 — verificato), `PlantCardV3_Terminal.uss`, `PlantCardV3TerminalController.cs`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs` | Nascondi root HUD + ph-tooltip quando lock modale |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/CompactBottomBarController.cs` | Nascondi root + tooltip CRY/room quando lock modale |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/BottomNavigationController.cs` | Nascondi root quando lock modale |
| `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/FoundationNotificationsPanelController.cs` | Nascondi root + tooltip toast quando lock modale |
| `Assets/_Project/Scripts/UI/UIToolkit/DomeStatusHUD/DomeStatusHUDController.cs` | Nascondi HUD cupola quando lock modale |
| `Assets/_Project/Scripts/UI/UIToolkit/SeedStorage/SeedStoragePanelController.cs` | Lock modale show/hide |
| `Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs` | Lock modale, dim unificato, PlayerStatus non soppresso |
| `Assets/_Project/Scripts/UI/UIToolkit/Lab/LabTerminalPanelController.cs` | Lock modale show/hide |
| `Assets/_Project/Scripts/UI/UIToolkit/FoodRoom/FoodRoomPanelController.cs` | Lock modale show/hide |
| `Assets/_Project/Resources/UI/UIToolkit/SeedStorage/SeedStoragePanel.uss` | Overlay alpha 0.65 |
| `Assets/_Project/UI/UIToolkit/Lab/LabTerminalPanel.uss` | Overlay alpha 0.65 |
| `Assets/_Project/UI/UIToolkit/FoodRoom/FoodRoomPanel.uss` | Verifica coerenza (0.65) |
| `Assets/_Project/UI/UIToolkit/PlantCardV3/PlantCardV3_Terminal.uss` | `.pcv3-dim` alpha 0.65 |

---

## Regole / vincoli rispettati

- **`GameplayUiModalLock`** come segnale unico per “modale attivo” e blocco input mondo (coerente con `Interactable`, mover, ecc.).
- **Nessun nuovo formato inventato** per report: struttura allineata a `Assets/Docs/REPORT/DEV_REPORT_*.md`.

---

## Note operative (Unity)

- Verificare in play che, aprendo **Seed Storage**, **PlantCardV3**, **Lab Terminal**, **Food Synth** (Food Room panel), la HUD fissa sparisca e l’overlay scuro risulti **omogeneo** tra i pannelli.
- Se in scena esiste ancora `BottomNavigationController` attivo oltre alla Compact Bottom Bar, entrambi ora rispettano il lock (nessun doppione HUD involontario oltre a configurazione scena).

---

*Fine DEV REPORT 0092.*
