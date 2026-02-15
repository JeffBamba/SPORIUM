# Dev Report 53 — End of Day Diary (Dome & Lab)

**Data:** 2025-02-15  
**Area:** End of Day sequence, Sporea Diary, Activity Summary  
**Riferimenti:** `END_OF_DAY_UI_SEQUENCE_SPEC.md`, `.cursor/plans/da fare/end_of_day_sequence_logic.plan.md`

---

## 1. Bug risolto: dettaglio Dome non visibile nel Diary

### Problema
Nella schermata Sporea Diary (Step 2 Snapshot / Step 3 Diario), dopo aver **avviato** (ma non concluso) due Plant Actions su due Pot, non compariva alcun dettaglio sotto la voce Dome (es. Plant Pot-001, Plant Pot-002), mentre per il Lab compariva correttamente il dettaglio (es. extractor).

### Causa (verificata con log)
- **Lab:** `RecordLabAction("Extractor")` viene chiamato **all'avvio** dell'azione (quando si avvia l'estrazione).
- **Dome:** `RecordWateringToggle` (e in generale le azioni vaso) venivano registrate solo **al completamento** (es. dopo conferma Water in DoWater). Se l'utente apriva il flusso (es. selettore semi, toggle irrigazione) ma non confermava, nessun pot veniva scritto nel log → lista Dome vuota nello snapshot.

### Fix
- Introduzione di **registrazione "all'avvio"** per le azioni Dome, in linea con il Lab:
  - **DayActivityLog**: nuovo metodo `RecordDomeActionStarted(potId)` e lista `_potIdsWithDomeActionStarted`; in `RecordWateringToggle(isNowOn == true)` viene aggiunta anche una voce Dome.
  - **PotActions**: chiamata a `RecordDomeActionStarted(potSlot.PotId)` all'inizio di `DoWater`, `DoPlant`, `DoLight`, `DoFertilize`, `DoPruning`.
  - **UI**: chiamata a `RecordDomeActionStarted` quando l'utente **avvia** il flusso (es. clic su "Plant" in PotActionsMenu, apertura OpenSeedSelector/OpenFertilizerSelector/OpenPruningDialog in PotDetailsWidget e PlantCardV2Controller, toggle irrigazione in PlantCardV2Controller).
- In **PopulateSnapshot** viene mostrata la riga **Dome** con l'elenco dei vasi per cui è stata avviata almeno un'azione (formato es. "Plant Pot-001, Plant Pot-002").

Risultato: tutto ciò che l'utente **avvia** come azione su un vaso (anche se non completata prima di premere Bed) compare nel riepilogo Dome del Diary.

---

## 2. Feature: testo descrittivo nel Diary (Opzione A)

### Obiettivo
Sostituire la sola elencazione di nomi (es. "Dome: Plant Pot-001, Plant Pot-002" e "Lab: Extractor") con **frasi descrittive in italiano** che spiegano cosa è stato fatto, usando dati reali (nome pianta, numero vaso, tipo input, quantità spore/cellule).

### Implementazione

#### 2.1 Modello dati (DayActivityLog)
- **DomeActivityEntry**  
  `PotId`, `ActionKind` (Plant | Water | Light | Fertilize | Pruning | Started), `PlantCode`, `PlantDisplayName`.
- **LabActivityEntry**  
  `LabType`, `InputDescription` (es. "frutto", "pianta intera"), `SporeOut`, `Cell001Out`, `Cell002Out`, `Cell003Out`.
- Nuove liste: `_domeEntries`, `_labEntries`; esposte come `DomeEntriesThisDay`, `LabEntriesThisDay`.
- **RecordDomeAction(DomeActivityEntry)** per azioni completate con contesto; **RecordDomeActionStarted(potId)** aggiunge una voce con `ActionKind = "Started"`.
- **RecordLabAction(LabActivityEntry)** per Lab con dettagli (Extractor); **RecordLabAction(string)** mantiene compatibilità per altri strumenti (Catalizzatore, Fusion, ecc.).

#### 2.2 Registrazione lato gameplay
- **PotActions**: a **completamento** di Plant / Water / Light / Fertilize / Pruning viene chiamato `RecordDomeAction(entry)` con i dati disponibili (es. per Plant: `PlantCode`, `PlantDisplayName` da `PlantData.name` o `PlantCode`). `RecordWateringToggle` aggiunge già una voce "Water".
- **Extractor**: all'avvio estrazione si rileva il tipo di input (frutto, pianta intera, scrap organico, residuo proteico) e gli output pianificati (spore, Cell001/002/003) e si chiama `RecordLabAction(LabActivityEntry)`.
- **LabMinigameExtractor**: stessa logica con entry strutturata (estrazione da frutto: 1 spora, 1 Cell002).

#### 2.3 Testo in PopulateSnapshot (EndOfDaySequenceController)
- **Raccolti:**  
  `"Hai raccolto {Amount} frutti di {PlantCode} (L{Level}) dal POT {numero}."`
- **Dome:**  
  Per ogni vaso viene considerata una sola azione "migliore" (priorità: Plant > Water > Light > Fertilize > Pruning > Started), poi:
  - Plant: `"Hai piantato un seme di {nome pianta} nel POT {numero}."`
  - Water: `"Hai attivato l'irrigazione nel POT {numero}."`
  - Light: `"Hai modificato le luci LED nel POT {numero}."`
  - Fertilize: `"Hai applicato fertilizzante nel POT {numero}."`
  - Pruning: `"Hai potato la pianta nel POT {numero}."`
  - Started: `"Azione avviata sul POT {numero}."`
- **Lab:**  
  - Extractor con dati: `"Hai estratto {N spore, X Cell001, ...} da {InputDescription}."`
  - Altri strumenti: `"Hai usato il {LabType}."`

Il numero vaso è ricavato da `PotId` (es. `POT-001` → `001`) tramite helper `FormatPotNumber`.

---

## 3. File modificati / toccati

| File | Modifiche |
|------|-----------|
| `Core/Diary/DayActivityLog.cs` | Struct `DomeActivityEntry`, `LabActivityEntry`; liste e metodi `RecordDomeAction`, `RecordDomeActionStarted`, `RecordLabAction(LabActivityEntry)`; `RecordWateringToggle` scrive anche una voce Dome "Water". |
| `UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs` | `PopulateSnapshot` riscritto su `DomeEntriesThisDay` e `LabEntriesThisDay` con frasi in italiano; helper `FormatPotNumber`. |
| `Dome/PotActions.cs` | Chiamate `RecordDomeActionStarted` all'inizio di DoWater/DoPlant/DoLight/DoFertilize/DoPruning; `RecordDomeAction(entry)` a completamento di Plant, Light, Pruning, Fertilize. |
| `Interactables/Extractor.cs` | Rilevamento input/output all'avvio estrazione; `RecordLabAction(LabActivityEntry)` al posto di `RecordLabAction("Extractor")`. |
| `UI/VaultMap/LabMinigameExtractor.cs` | `RecordLabAction(LabActivityEntry)` con InputDescription "frutto" e output 1 spora, 1 Cell002. |
| `UI/UIToolkit/PotActionsMenu/PotActionsMenu.cs` | `RecordDomeActionStarted(_currentPot.PotId)` in `OnPlantClicked`. |
| `UI/VaultMap/PotDetailsWidget.cs` | `RecordDomeActionStarted(targetPot.PotId)` in `OpenSeedSelector`, `OpenFertilizerSelector`, `OpenPruningDialog`. |
| `UI/UIToolkit/PlantCard/PlantCardV2Controller.cs` | `RecordDomeActionStarted` in `OpenSeedSelector`, `OpenFertilizerSelector`, `OpenPruningDialog`, `OnIrrigationToggle`. |

*(Eventuale instrumentation di debug in `DayActivityLog` e `EndOfDaySequenceController` può essere rimossa dopo verifica finale.)*

---

## 4. Note per QA / verifiche

- **Dome:** Avviare almeno un'azione su uno o più vasi (Plant, Water, Light, Fertilize, Pruning) e in alcuni casi **non** completarla (es. aprire selettore semi e andare a letto). Verificare che nello Snapshot EoD compaiano le frasi descrittive corrette per ogni vaso toccato e che le azioni completate mostrino il testo dettagliato (es. nome pianta per Plant).
- **Lab:** Avviare un'estrazione (da frutto o da altro input se disponibile) e, opzionalmente, andare a letto prima del termine. Verificare che compaia una riga del tipo "Hai estratto … da frutto." (o altro input) con quantità coerenti.
- **Raccolti:** Eseguire almeno un harvest e controllare la riga "Hai raccolto X frutti di … dal POT N." nello snapshot.

---

## 5. Riepilogo

- **Bug:** Il Diary non mostrava i vasi Dome quando l'azione era solo avviata e non completata; il Lab sì perché registrava all'avvio.  
  **Soluzione:** Registrazione all'avvio anche per le azioni Dome (log + UI) e uso delle stesse voci per il testo descrittivo.
- **Feature:** Il riepilogo giornaliero (Snapshot EoD) usa ora **voci strutturate** (DomeActivityEntry, LabActivityEntry) e **frasi in italiano** (es. "Hai piantato un seme di … nel POT 001", "Hai estratto N spore da frutto") al posto della sola lista di nomi/tipi.
