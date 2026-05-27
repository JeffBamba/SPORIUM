# DEV REPORT 0111 — LAB 4.0 Genoscrittore e blueprint seed

**Data:** 2026-05-27  
**Sprint / contesto:** LAB 4.0 — Schermata 1 Genoscrittore, blueprint seed, material gate e rimozione flusso legacy dal terminale Lab.  
**Riferimento piano:** `.cursor/plans/laboratorio_visione_e_repo_a60fd394.plan.md`  
**Report precedente:** `DEV_REPORT_0110_MOTORE_CONOSCENZA_2026-05-27.md`

---

## Sommario interventi

1. Introdotto il dominio **LabBlueprint** per gestire un progetto seme a slot singolo: stato, input biologico, allocazione punti, reagente, seal, outcome e persistenza.
2. Aggiunto **LabBlueprintReadinessService** per sostituire la vecchia analisi Replica/Ibrido/Nuovo profilo con un gate materiale coerente LAB 4.0: frutto oppure spora valida.
3. Aggiunto **LabBlueprintMaterialGateController** per aprire il picker inventario, validare la selezione e avviare il draft blueprint.
4. Riadattato **LabTerminalPanelController**: il Terminal Lab apre direttamente Schermata 1 Genoscrittore e il CTA avvia il gate materiale.
5. Riscritto **LabTerminalPanel.uxml / .uss** come pannello LAB 4.0 unico, 1480x900, centrato, non fullscreen, senza `lab-terminal-panel` legacy e senza schermata analisi progetto.

---

## Statistiche e progresso

### Righe di codice

- **Diff staged complessivo:** 24 file, **2206 inserimenti / 170 rimozioni** — comando `git diff --cached --stat`, 2026-05-27.
- **Nota:** dato utile per tracciabilita del batch, non equivalente a conteggio LOC finale puro; non misurato riga per riga per singolo file.

### Sistemi funzionanti

- **Linter IDE:** nessun errore rilevato su `LabTerminalPanelController.cs` dopo le modifiche.
- **Flusso runtime LAB 4.0:** da validare in Unity Editor su `SCN_VaultMap` (interazione Terminal Lab, apertura pannello 1480x900, CTA, picker inventario, ritorno/ESC).
- **Compilazione Unity:** da validare in Editor dopo refresh asset e import UXML/USS.

### Bug risolti

- **3** — il Terminal Lab apriva ancora il vecchio hub, il pulsante "Crea nuovo seme" poteva riaprire la schermata analisi legacy, e Schermata 1 veniva mostrata come overlay fullscreen/impaginato male invece che come pannello Sporium centrato.

### Progresso gameplay / prodotto

- Il player non passa piu dal vecchio hub Lab per iniziare il flow: l'interazione col Terminal Lab porta direttamente al **Protocollo LAB — Genoscrittore**.
- Il vecchio processo concettuale Replica/Ibrido/Nuovo Profilo viene bypassato dal percorso LAB 4.0.
- La Schermata 1 ora e una superficie UI Toolkit unica, editabile in UI Builder e usata a runtime.
- Il pannello e dimensionato a **1480x900**, lasciando visibile la HUD fissa della GameView fuori dalla modale.
- Il budget e lo stato Conoscenza esposti dal report 0110 possono alimentare la presentazione e i prossimi step di progettazione seme.

---

## 1. Dominio LabBlueprint

### Problema

- Il vecchio flusso Lab ragionava per analisi progetto e tipologie legacy, non per protocollo LAB 4.0.
- Mancava uno stato serializzabile che rappresentasse un draft seed con materiale di partenza, budget, allocazioni, reagente e avanzamento.

### Soluzione

- Aggiunta la cartella `Assets/_Project/Scripts/Core/LabBlueprint/`.
- Creati tipi e stato base: `LabBlueprintTypes`, `LabBlueprintItemSnapshot`, `LabBlueprintAllocation`, `LabBlueprintState`.
- Introdotto `LabBlueprintService` con API per `StartDraft`, allocazione punti, reagente, seal, avanzamento, outcome, abandon/load/export.
- Collegata la persistenza in `SaveManager` e registrato il servizio in `GamePlayInstaller`.

**File interessati:**  
`LabBlueprintTypes.cs`, `LabBlueprintItemSnapshot.cs`, `LabBlueprintAllocation.cs`, `LabBlueprintState.cs`, `LabBlueprintService.cs`, `SaveManager.cs`, `GamePlayInstaller.cs`

---

## 2. Gate materiale LAB 4.0

### Problema

- La UX "Crea nuovo seme" era ancora legata alla vecchia selezione logica di progetto, invece il piano LAB 4.0 richiede un avvio basato su materiale biologico reale.
- Serviva distinguere disponibilita e validita di frutti/spore senza replicare logiche nel controller UI.

### Soluzione

- Aggiunto `LabBlueprintReadinessService` per valutare inventario, progetto attivo, assenza materiali e tipo input ammesso.
- Aggiunto `LabBlueprintMaterialGateController` per coordinare inventory picker, toast/feedback, validazione selezione e avvio del draft nel `LabBlueprintService`.
- Aggiunte chiavi localizzate per picker e messaggi readiness.

**File interessati:**  
`LabBlueprintReadinessService.cs`, `LabBlueprintMaterialGateController.cs`, `LocalizationManager.cs`

---

## 3. Schermata 1 Genoscrittore

### Problema

- La reference LAB 4.0 richiede un onboarding visuale "Protocollo LAB — Genoscrittore", ma il UXML iniziale lasciava ancora visibile sotto il vecchio flow e non rispettava proporzioni/layout richiesti.
- L'overlay era stato trattato come fullscreen, mentre il pannello deve comportarsi come le altre modali Sporium e lasciare visibile la HUD fuori dal perimetro.

### Soluzione

- Riscritto `LabTerminalPanel.uxml` per contenere una sola superficie runtime LAB 4.0:
  - wrapper `lab-terminal-overlay` come centratore modale;
  - `lab40-screen1` come pannello reale;
  - nessun `lab-terminal-panel`, nessun hub macchinari, nessun blocco analisi progetto.
- Aggiornato `LabTerminalPanel.uss` con layout 1480x900:
  - header grande;
  - colonna sinistra con modulo, seed, conoscenza, registro;
  - centro tipo scheda/protocollo;
  - stepper operativo a destra;
  - footer VO + CTA "APRI GENOSCRITTORE".

**File interessati:**  
`LabTerminalPanel.uxml`, `LabTerminalPanel.uss`

---

## 4. Wiring Terminal Lab e rimozione fallback legacy

### Problema

- `Show()` poteva ancora mostrare l'overlay del vecchio hub.
- Il binding di `btn-create-project`, se presente, poteva ancora cadere su `StartProjectWithAnalysis()`.
- Il controller doveva aprire il nuovo onboarding anche se la gerarchia scena mette il gate su un GameObject separato dal pannello UI.

### Soluzione

- `Show()` ora apre sempre Schermata 1 e usa `lab-terminal-overlay` solo come wrapper di centratura.
- Il binding residuale di `btn-create-project` non richiama piu `StartProjectWithAnalysis()`.
- `LabTerminalPanelController` risolve il material gate e collega gli eventi `DraftStarted` / `MaterialSelectionCancelled`.
- `Hide()` e `ESC` chiudono correttamente Schermata 1 e rilasciano il lock modale.

**File interessati:**  
`LabTerminalPanelController.cs`, `SCN_VaultMap.unity`, `SceneHierarchy.txt`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/Core/LabBlueprint/LabBlueprintTypes.cs` | Nuovo dominio enum/tipi blueprint |
| `Assets/_Project/Scripts/Core/LabBlueprint/LabBlueprintItemSnapshot.cs` | Snapshot serializzabile item input |
| `Assets/_Project/Scripts/Core/LabBlueprint/LabBlueprintAllocation.cs` | Allocazione punti per campi progetto |
| `Assets/_Project/Scripts/Core/LabBlueprint/LabBlueprintState.cs` | Stato serializzabile single-slot |
| `Assets/_Project/Scripts/Core/LabBlueprint/LabBlueprintService.cs` | Servizio runtime blueprint |
| `Assets/_Project/Scripts/Core/LabBlueprint/LabBlueprintReadinessService.cs` | Gate readiness materiali |
| `Assets/_Project/Scripts/Core/Installers/GamePlayInstaller.cs` | Registrazione servizi LAB 4.0 |
| `Assets/_Project/Scripts/Core/SaveManager.cs` | Persistenza stato blueprint |
| `Assets/_Project/Scripts/Core/Localization/LocalizationManager.cs` | Chiavi LAB 4.0 / readiness |
| `Assets/_Project/Scripts/UI/UIToolkit/Lab/LabBlueprintMaterialGateController.cs` | Controller picker/material gate |
| `Assets/_Project/Scripts/UI/UIToolkit/Lab/LabTerminalPanelController.cs` | Apertura Schermata 1 e wiring gate |
| `Assets/_Project/UI/UIToolkit/Lab/LabTerminalPanel.uxml` | Rimozione legacy, UXML Schermata 1 unica |
| `Assets/_Project/UI/UIToolkit/Lab/LabTerminalPanel.uss` | Layout/stile pannello 1480x900 |
| `Assets/_Project/Scenes/SCN_VaultMap.unity` | Wiring scena Lab Terminal / componenti |
| `Assets/_Project/Docs/SceneHierarchy.txt` | Snapshot gerarchia scena aggiornato |
| `*.meta` correlati | Meta Unity per nuovi asset/script |

---

## Regole / vincoli rispettati

- **UI Toolkit Builder parity:** il pannello runtime e la superficie editabile in UI Builder coincidono; niente campione parallelo per Schermata 1.
- **No flusso UI parallelo:** rimosso dal UXML il vecchio `lab-terminal-panel` come esperienza runtime del Terminal Lab.
- **ServiceContainer:** i servizi LabBlueprint sono registrati come servizi globali; il controller UI resta orchestratore di presentazione/gate.
- **Both / Principio 0:** nessuna scena o UI separata solo demo; il Terminal Lab usa lo stesso pannello nel prodotto unico.

---

## Note operative (Unity)

1. Aprire Unity e lasciar reimportare UXML/USS/script.
2. In `SCN_VaultMap`, interagire con **Terminal Lab**: deve aprirsi direttamente Schermata 1 Genoscrittore.
3. Verificare che il pannello sia **1480x900 centrato** e non fullscreen, con HUD visibile fuori.
4. Premere **INDIETRO** o `ESC`: il terminale deve chiudersi e rilasciare il lock modale.
5. Premere **APRI GENOSCRITTORE**: deve partire il material gate e aprire il picker inventario se ci sono materiali validi.
6. Verificare in UI Builder che `LabTerminalPanel.uxml` mostri la stessa struttura runtime, senza dover modificare un campione separato.

---

*Fine DEV REPORT 0111.*
