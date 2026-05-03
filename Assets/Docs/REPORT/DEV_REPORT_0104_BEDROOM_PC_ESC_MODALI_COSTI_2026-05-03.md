# DEV REPORT 0104 — PC Bedroom UI Toolkit, priorità Esc modali/menu, costi Food Room e wiring Vault

**Data:** 2026-05-03  
**Sprint / contesto:** UX modali Vault, terminale PC camera, allineamento input Esc con `GameplayUiModalLock`, dati esposti per pannello costi macchinari; documentazione workflow SVILUPPA.  
**Riferimento piano:** `.cursor/plans/demo_alpha_1_0_gap_map.plan.md` (Principio 0 — un solo prodotto; UX modali coerente)  
**Report precedente:** `DEV_REPORT_0103_DEMO_BEAT3_UI_FLOW_CAMERA_RUNTIME_2026-04-28.md`

---

## Sommario interventi

1. Introdotto il **terminale PC bedroom** in UI Toolkit (UXML/USS, `BedroomPcPanelSettings`, sprite cornice `PC-bedroom.png`), controller display + wiring scena `SCN_VaultMap` e componente `BedroomPcTerminal` per apertura/chiusura.
2. Corretto il **doppio consumo di Esc** nello stesso frame (chiusura pannello + toggle menu in-game): guard su `GameplayUiModalLock.BlocksWorldInput` in `MainMenuUIToolkitController` e `MainMenuScreens`; lock su pannelli che non lo impostavano (Cryo, microscopio, inventario canvas legacy); documentazione in `sviluppa.mdc` e promemoria tipografia in skill `sviluppa`.
3. **Chiusura con Esc** su Condensation Tank, Food Synth e terminale laboratorio; sul terminale lab gestione a due livelli (analisi / scelta tipo vs chiusura).
4. Esposti in **`FoodRoomSystem`** metodi di supporto per costi/giorno e conteggi (slot synth attivi, dispensa, CRY) per UI control plane; costanti tier CRY in **`SeedStorageSystem`** rese pubbliche per lettura da UI.

---

## Statistiche e progresso

### Righe di codice

- Fonte: `git diff --cached --numstat` (solo **staged**).
- **Totale repo (tutti i path staged):** **+1765** / **-155** righe.
- **Script C#** sotto `Assets/_Project/Scripts` (+ menu): **+740** aggiunte / **-9** rimosse (somma dai `numstat` sui `.cs` elencati in tabella più sotto).
- **UI Toolkit testo/stile:** `BedroomPcDisplay.uss` **+560**, `BedroomPcDisplay.uxml` **+175** (nuovi file).
- **Scena Unity:** `SCN_VaultMap.unity` **+53** / **-143** (delta YAML).

### Sistemi funzionanti

- **Non misurato in questa iterazione** in Play Mode / build da parte dell’agente: da **validare in Editor** (Esc sequenziale modale → menu, PC bedroom, Cryo, microscopio, inventario legacy, Food Room / Condense / Lab terminal, pannello costi se collegato ai nuovi API).

### Bug risolti

- **1** (documentato in sessione): **Esc** apriva/toggliava il **menu in-game** nello stesso frame in cui un **modale gameplay** gestiva la chiusura — risolto con guard su `BlocksWorldInput` e lock mancanti su alcuni overlay.

### Progresso gameplay / prodotto

- Il giocatore può usare il **terminale in camera** con UI coerente al resto del Vault (Toolkit + asset dedicato).
- **Primo Esc** chiude il pannello modale; **secondo Esc** accede al menu pausa, senza “salti” doppi nella stessa pressione.
- **Cryo**, **microscopio** e **inventario canvas** partecipano allo stesso contratto di lock input del resto delle modali.
- **Condense tank**, **sintetizzatore cibo** e **terminale lab** si chiudono con Esc in linea con gli altri macchinari.
- La UI dei **costi CRY** può leggere dati più completi da `FoodRoomSystem` e costanti seed storage esposte.

---

## 1. PC Bedroom — UI Toolkit e runtime

### Problema

- Mancava un terminale dedicato in camera operatore, integrato alla mappa Vault e al sistema modali HUD.

### Soluzione

- Aggiunti `BedroomPcDisplay.uxml` / `.uss`, `BedroomPcPanelSettings.asset`, sprite `PC-bedroom.png` (+ meta).
- `BedroomPcDisplayController`: binding UI, viste home/detail/pannello controllo, orologio, integrazione `GameplayUiModalLock.SetMachineModalState`, eventi verso ricerca/black market/FAQ/control plane.
- `BedroomPcTerminal`: entry point serializzabile per `Show`/`Hide` dal mondo.
- Scena `SCN_VaultMap.unity`: riferimenti UIDocument / asset aggiornati per il PC bedroom.

**File interessati:**  
`Assets/_Project/UI/UIToolkit/BedroomPc/*`, `Assets/_Project/Scripts/UI/UIToolkit/BedroomPc/*`, `Assets/_Project/Art/UI/PC-bedroom.png`, `Assets/_Project/Scenes/SCN_VaultMap.unity`

---

## 2. Esc — priorità modale vs menu in-game

### Problema

- `MainMenuUIToolkitController` (ordine di esecuzione anticipato) intercettava **Esc** senza verificare se un modale gameplay era ancora “in lock”, generando toggle menu e chiusura pannello nello stesso frame.

### Soluzione

- Su Esc verso il menu: **ritorno anticipato** se `GameplayUiModalLock.BlocksWorldInput` è true (`MainMenuUIToolkitController`, `MainScreenScreens` legacy).
- **CryoMachinePanelController:** `SetMachineModalState(visible)` in `SetVisible`.
- **MicroscopeHUDView:** `SetBlockWorldInput` all’apertura delle viste minigioco, reset in `Hide`.
- **HUDInventory:** `SetBlockWorldInput` in `Show`/`Hide` (inventario canvas).
- **GameplayUiModalLock:** commento XML su `BlocksWorldInput` (rapporto con Esc / menu).
- **Regole progetto:** `sviluppa.mdc` sezione 2bis — bullet dedicato a priorità Esc + lock; sezione qualità — font UI minimo 10px. **Skill** `sviluppa`: promemoria font 10px (Fase 0 e Qualità).

**File interessati:**  
`MainMenuUIToolkitController.cs`, `MainMenuScreens.cs`, `CryoMachinePanelController.cs`, `MicroscopeHUDView.cs`, `HUDInventory.cs`, `GameplayUiModalLock.cs`, `.cursor/rules/sviluppa.mdc`, `.cursor/skills/sviluppa/SKILL.md`

---

## 3. Esc su Condense Tank, Food Synth, terminale laboratorio

### Problema

- Pannelli macchina aperti senza scorciatoia Esc coerente con Seed Storage / Dispensa / altri HUD.

### Soluzione

- **CondenseTankPanelController:** in `Update`, se visibile → `Hide()` su Esc (lock già presente in Show/Hide).
- **FoodRoomPanelController:** Esc → `Hide()` se visibile; se aperto il **picker** inventario stem cell (`PlayerInventoryPanelController.IsVisible`), non si intercetta Esc (resta al picker).
- **LabTerminalPanelController:** `TryConsumeLabTerminalEscape()` — durante analisi in corso → `CancelProjectTypeSelection()`; su schermata scelta tipo post-analisi → ritorno al project board; altrimenti `Hide()`.

**File interessati:**  
`CondenseTankPanelController.cs`, `FoodRoomPanelController.cs`, `LabTerminalPanelController.cs`

---

## 4. Dati sistema per UI costi / control plane

### Problema

- La UI del control plane (es. costi giornalieri CRY legati a synth e dispensa) necessitava API leggibili senza duplicare logica nel controller.

### Soluzione

- **FoodRoomSystem:** `CountActiveFoodSynthSlots`, `CountPantryItems`, `ComputeFoodSynthDailyCryCost`, `ComputePantryDailyCryCost`.
- **SeedStorageSystem:** costanti tier (`Tier1SlotCount`, `CryTier1Occupied`, `CryTier2Occupied`) rese **public** per binding UI.

**File interessati:**  
`FoodRoomSystem.cs`, `SeedStorageSystem.cs`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `.cursor/rules/sviluppa.mdc` | Tipografia min 10px; priorità Esc modale vs menu (2bis + qualità) |
| `.cursor/skills/sviluppa/SKILL.md` | Promemoria font 10px (Fase 0 + Qualità) |
| `Assets/_Project/Art/UI/PC-bedroom.png` (+ `.meta`) | Nuovo asset UI cornice PC |
| `Assets/_Project/Scenes/SCN_VaultMap.unity` | Wiring PC bedroom / UIDocument |
| `Assets/_Project/Scripts/Core/GameplayUiModalLock.cs` | Doc `BlocksWorldInput` |
| `Assets/_Project/Scripts/Systems/FoodRoom/FoodRoomSystem.cs` | API conteggi/costi giornalieri |
| `Assets/_Project/Scripts/Systems/SeedStorage/SeedStorageSystem.cs` | Costanti CRY tier pubbliche |
| `Assets/_Project/Scripts/UI/MainMenu/MainMenuScreens.cs` | Guard Esc + `BlocksWorldInput` |
| `Assets/_Project/Scripts/UI/UIToolkit/BedroomPc/*` | Nuovo modulo PC bedroom (controller, terminal, meta) |
| `Assets/_Project/Scripts/UI/UIToolkit/CryoMachine/CryoMachinePanelController.cs` | `SetMachineModalState` su visibilità |
| `Assets/_Project/Scripts/UI/UIToolkit/FoodRoom/CondenseTankPanelController.cs` | Chiusura Esc |
| `Assets/_Project/Scripts/UI/UIToolkit/FoodRoom/FoodRoomPanelController.cs` | Chiusura Esc; rispetto picker inventario |
| `Assets/_Project/Scripts/UI/UIToolkit/Lab/LabTerminalPanelController.cs` | Esc a livelli (analisi / tipo / chiusura) |
| `Assets/_Project/Scripts/UI/UIToolkit/MainMenu/MainMenuUIToolkitController.cs` | Guard Esc + `BlocksWorldInput` |
| `Assets/_Project/Scripts/UI/VaultMap/HUDInventory.cs` | Lock su Show/Hide |
| `Assets/_Project/Scripts/UI/VaultMap/MicroscopeMinigame/MicroscopeHUDView.cs` | Lock su show/hide minigioco |
| `Assets/_Project/UI/UIToolkit/BedroomPc/*` | UXML, USS, PanelSettings, meta |

---

## Regole / vincoli rispettati

- **Architecture / servizi:** nessun nuovo `FindObjectOfType` aggiunto in questi diff; uso di `ServiceContainer` / sistemi esistenti dove già previsto.
- **Modali HUD:** `GameplayUiModalLock` per blocco input e coerenza con menu in-game su Esc (allineato a regola aggiornata in `sviluppa.mdc`).
- **UI Toolkit / parità:** authoring in UXML/USS per il pannello PC bedroom; vincolo **font ≥ 10px** riflesso in regola/skill SVILUPPA.

---

## Note operative (Unity)

- Play test in `SCN_VaultMap`: aprire PC bedroom, macchinari elencati, Cryo, microscopio, inventario legacy; verificare **Esc** (solo chiusura modale) poi **Esc** (menu).
- Sul **terminale lab**, verificare Esc durante **analisi progetto**, sulla **scheda tipi** e a **terminale principale**.
- Con **picker** stem cell dal Food Synth, verificare che il **primo** Esc chiuda l’inventario picker, non il synth dietro (se ancora aperto).

---

*Fine DEV REPORT 0104.*
