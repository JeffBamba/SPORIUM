# DEV REPORT 0071 — Task 2 Roadmap Dome+Lab (topologia 4+3, CryoMachine, comandi CRYO, HUD, save/load)

**Data:** 2026-03-19
**Oggetto:** Implementazione completa del **Task 2** del piano `roadmap_dome_lab_100_069d5bdb.plan.md` (topologia reale della Dome con 4 slot attivi e 3 slot passivi separati a livello runtime, CryoMachine con gestione trasferimenti, comandi CRYO nel terminale, HUD CryoMachine interagibile, save/load stato cryo).
**Riferimenti:** Roadmap `.cursor/plans/roadmap_dome_lab_100_069d5bdb.plan.md` — Task 2; piano di dettaglio `.cursor/plans/task_2_implementation_complete_f485810e.plan.md`; `PotSystemConfig`, `CryoMachineController`, `CryoSlot`, `CryoPlantPayload`, `PotActions`, `DayCycleController`, `SaveManager`, `PlantCardV3TerminalController`, `CryoMachinePanelController`.
**Report precedente:** `Assets/Docs/REPORT/DEV_REPORT_0070_TERMINAL_POT_HUD_COMANDI_SCROLLBAR_2026-03-18.md`

---

## 1. Contesto

- **Task 2 del piano Dome+Lab:** trasformare la Dome da sistema generico a 10 pots a una struttura coerente con il GDD che espone esplicitamente `4 slot attivi` (pot coltivazione ordinaria) e `3 slot passivi` (CryoMachine, piante Lvl 5, MaintenanceFree).
- **Scope effettivo (sessione):** infrastruttura runtime CryoMachine completa (`CryoPlantPayload`, `CryoSlot`, `CryoMachineController`); trasferimenti Pot↔Cryo e Cryo→Storage senza perdita metadata; comandi interattivi PASSIVE / CRYO SEND / CRYO EXTRACT / CRYO RESTORE nel terminale con refresh HUD immediato e toast Foundation; HUD CryoMachine UIToolkit con `CryoMachinePanelController` e `CryoMachineOpener` per interagibilità scena; serializzazione/deserializzazione stato cryo nel save di partita.
- **Perimetro escluso (rinviato a Task 3 e Task 4):** gli effetti gameplay reali di `ActivePower` e `PassivePower` (bonus pH, metagame vault, scaling per livello) restano scaffold documentato con `// TODO Task 3/4`.

---

## 2. Nuovi componenti

### 2.1 CryoPlantPayload

File: `Assets/_Project/Scripts/Dome/CryoPlantPayload.cs` (nuovo)

Classe `[Serializable]` che conserva l'intera identità runtime di una pianta Lvl 5 al momento del trasferimento in cryo: codice, livello, famiglia, display name, genetica (`GeneticType`), tratti candidati e selezionati, percentuale potenza tratti, reagente usato, etichette `ActivePowerLabel` / `PassivePowerLabel`, flag `IsHybrid` / `IsMutated`, nomi parenti, nome custom, display name sorgente. Usato da `CryoSlot`, `PotActions` e `SaveManager`.

### 2.2 CryoSlot

File: `Assets/_Project/Scripts/Dome/CryoSlot.cs` (nuovo)

`MonoBehaviour` che rappresenta un singolo slot passivo. Campi: `SlotId` (es. `CRY-01`), `IsOccupied`, `Payload` (`CryoPlantPayload`). Metodi: `Occupy(CryoPlantPayload)`, `Free()`. Non entra mai in `DomePotRegistry` né nel loop produttivo di `DayCycleController`.

### 2.3 CryoMachineController

File: `Assets/_Project/Scripts/Dome/CryoMachineController.cs` (nuovo)

`MonoBehaviour` centrale che gestisce l'array dei 3 `CryoSlot`. Si registra nel `ServiceContainer` su `Awake`. Metodi pubblici:

- `GetPassiveSlotsSnapshot()` — lista snapshot read-only degli slot
- `GetSlotById(string id)` — lookup per ID
- `TryOccupySlot(CryoPlantPayload)` — occupa il primo slot libero
- `FreeSlot(string slotId)` — libera uno slot per ID
- `OccupiedCount()` — conteggio slot occupati
- `CollectSaveData()` / `RestoreFromSave(List<CryoSlotSaveEntry>)` — integrazione save/load

---

## 3. Modifiche ai sistemi esistenti

### 3.1 PotSystemConfig

File: `Assets/_Project/Scripts/Dome/PotSystemConfig.cs`

Aggiunte due costanti:
- `public const int ACTIVE_SLOT_COUNT = 4;`
- `public const int PASSIVE_SLOT_COUNT = 3;`

`MAX_POTS_PER_ROOM = 10` rimane per compatibilità ma non governa più nessun core flow (usato solo in `ValidatePotCount`, non chiamato da nessun altro sistema).

### 3.2 DomePotRegistry

File: `Assets/_Project/Scripts/Dome/DomePotRegistry.cs`

Aggiunto metodo `public List<PotSlot> GetActivePotsSnapshot()` — alias di `GetPotsSnapshot()` con nome semanticamente esplicito, usato da `PotSlot.ClearAllSelections()` per iterare solo gli slot attivi.

### 3.3 PotSlot

File: `Assets/_Project/Scripts/Interactables/PotSlot.cs`

`ClearAllSelections()` aggiornata per usare `DomePotRegistry.GetActivePotsSnapshot()` invece di una scansione scena generica.

### 3.4 PotActions — trasferimenti Cryo

File: `Assets/_Project/Scripts/Dome/PotActions.cs`

Tre nuovi metodi pubblici:

- **`TransferToCryo()`**: verifica che la pianta nel pot sia Lvl 5, costruisce un `CryoPlantPayload` dai campi correnti del `PotStateModel`, chiama `CryoMachineController.TryOccupySlot()`, resetta il pot corrente, deregistra dal `DayCycleController`, invia toast Foundation (successo o errori: non Lvl5, cryo piena, pot vuoto).

- **`RestoreFromCryo(string cryoSlotId)`**: recupera il payload dallo slot, chiama `FreeSlot()`, applica tutti i campi del payload al `PotStateModel` corrente, re-registra il pot nel `DayCycleController`, invia toast Foundation. Se il pot di destinazione non è vuoto o lo slot non esiste restituisce messaggio di errore.

- **`ExtractFromCryoToStorage(string cryoSlotId)`**: recupera il payload, chiama `FreeSlot()`, costruisce un `Item` di tipo `WholePlant` arricchito con metadata Lvl 5 (livello, genetica, famiglia, poteri, nome custom), lo aggiunge all'inventario del player, invia toast Foundation. Il `WholePlant` è soggetto al sistema `DeteriorationSystem` (−1 Quality/giorno, degradazione a `OrganicScrap001`).

### 3.5 DayCycleController — scaffold ApplyPassivePowers

File: `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

Aggiunto metodo `private void ApplyPassivePowers(int dayIndex)` chiamato alla fine di `ExecuteDailyTick()`. Legge i `CryoSlot` occupati tramite `ServiceContainer.Get<CryoMachineController>()` e registra a debug il `PassivePowerLabel` di ogni slot. I CryoSlot non entrano mai in `_registeredPots`. Gli effetti gameplay reali sono `// TODO Task 3/4`.

### 3.6 SaveManager — cryoSlots

File: `Assets/_Project/Scripts/Core/SaveManager.cs`

- Aggiunto `public List<CryoSlotSaveEntry> cryoSlots;` alla classe `GameSaveData`.
- In `SaveGame()`: `saveData.cryoSlots = CollectCryoSlotStates()` che chiama `CryoMachineController.CollectSaveData()`.
- In `LoadGame()`: se `saveData.cryoSlots` non è vuota chiama `ApplyCryoSlotStates()` che chiama `CryoMachineController.RestoreFromSave()`.
- Entrambi i metodi gestiscono gracefully l'assenza del `CryoMachineController` (warn + skip senza crash).

---

## 4. Terminal Pot — comandi CRYO

File: `Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs`

### 4.1 PASSIVE — overview con protocollo esplicativo

`ExecutePassiveOverview()` ora stampa prima un blocco testuale "PROTOCOLLO CRYO" che spiega:
- cosa sono gli slot passivi e la CryoMachine;
- differenza tra ActivePower (attivo in pot) e PassivePower (attivo in cryo);
- descrizione sintetica dei tre comandi disponibili (CRYO SEND, CRYO EXTRACT, CRYO RESTORE);
- avvertenza sul deperimento item in inventario.

Seguito dal riepilogo stato dei 3 CryoSlot (ID, occupato/libero, nome pianta, livello, PassivePowerLabel).

### 4.2 CRYO SEND [POT-ID]

Comando diretto: `CRYO SEND POT-01` (o il pot attualmente selezionato). Chiama `PotActions.TransferToCryo()`. Risposta immediata con esito, toast Foundation, e refresh HUD del terminale.

### 4.3 CRYO EXTRACT — flow interattivo

`BeginCryoExtractSelection()`: lista gli slot occupati con indice numerico di scelta, stampa avvertenza "qualsiasi item organico in inventario perde 1 Quality al giorno — le piante Lvl 5 degradano a OrganicScrap", imposta `InputState.SelectingCryoSlotForExtract`.

`HandleCryoExtractSlotChoice(string upper)`: processa la scelta (numero o ID slot), chiama `PotActions.ExtractFromCryoToStorage()`, stampa esito, invia toast, ripristina `InputState.Idle`. Accetta "N" o input non valido per annullare.

### 4.4 CRYO RESTORE — flow interattivo

`BeginCryoRestoreSlotSelection()`: lista gli slot occupati con indice numerico, stampa avvertenza "il PassivePower si disattiverà — la pianta tornerà in modalità produzione attiva", imposta `InputState.SelectingCryoSlotForRestore`.

`HandleCryoRestoreSlotChoice(string upper)`: processa scelta slot, salva `_pendingCryoSlotId`, lista i pot attivi vuoti disponibili, imposta `InputState.SelectingTargetPotForRestore`.

`HandleCryoRestorePotChoice(string upper)`: processa scelta pot, chiama `PotActions.RestoreFromCryo()`, stampa esito, invia toast, ripristina `InputState.Idle`.

### 4.5 InputState e campi aggiuntivi

Enum `InputState` esteso con:
- `SelectingCryoSlotForExtract`
- `SelectingCryoSlotForRestore`
- `SelectingTargetPotForRestore`

Campi aggiunti:
- `string _pendingCryoSlotId` — ID slot cryo durante il flow Restore a due step
- `List<CryoSlot> _cryoSlotsForChoice` — snapshot slot durante la selezione
- `List<PotSlot> _emptyPotsForChoice` — snapshot pot vuoti durante la selezione

### 4.6 Refresh HUD immediato

Dopo ogni operazione CRYO SEND / CRYO EXTRACT / CRYO RESTORE completata con successo, vengono chiamati `UpdateHudSlotVisuals()` e `RefreshHudFromSelectedPot()` all'interno del terminale, senza necessità di uscire e rientrare nel pannello.

---

## 5. CryoMachine UI e interagibilità scena

### 5.1 CryoMachinePanel.uxml / CryoMachinePanel.uss

File: `Assets/_Project/UI/UIToolkit/CryoMachine/CryoMachinePanel.uxml` (nuovo)
File: `Assets/_Project/UI/UIToolkit/CryoMachine/CryoMachinePanel.uss` (nuovo)

Pannello UIToolkit che mostra i 3 slot cryo con header, badge stato (OCCUPATO/LIBERO), nome pianta, livello, famiglia e PassivePowerLabel. Pulsante di chiusura. Stile coerente con il resto della Foundation UI del progetto.

### 5.2 CryoMachinePanelController

File: `Assets/_Project/Scripts/UI/UIToolkit/CryoMachine/CryoMachinePanelController.cs` (nuovo)

`MonoBehaviour` che controlla il `UIDocument` del pannello cryo. Metodi pubblici:
- `Show()` — rende visibile il pannello e chiama `RefreshSlots()`
- `Hide()` — nasconde il pannello
- `RefreshSlots()` — legge `CryoMachineController` dal `ServiceContainer` e popola i 3 slot UI con dati aggiornati

Gestisce gracefully l'assenza di `PanelSettings` copiandolo da altri `UIDocument` presenti nella scena.

### 5.3 CryoMachineOpener

File: `Assets/_Project/Scripts/Interactables/CryoMachineOpener.cs` (nuovo)

`MonoBehaviour` da assegnare al GameObject `CryoMachine` in scena. Richiede il componente `Interactable` (namespace `_Project`) sullo stesso oggetto. Si sottoscrive all'evento `OnInteract` e chiama `CryoMachinePanelController.Show()` quando il player interagisce con la CryoMachine.

**Configurazione scena necessaria (completata manualmente):**
- `CryoMachine` GameObject: `SpriteRenderer` con sprite, `BoxCollider2D` trigger, `Interactable`, `CryoMachineController`, `CryoMachineOpener`.
- 3 GameObject figli `CRY-01` / `CRY-02` / `CRY-03`: componente `CryoSlot` con SlotId assegnato.
- `CryoMachineHUDPanel` GameObject separato: `UIDocument` con `CryoMachinePanel.uxml`, `CryoMachinePanelController`.

---

## 6. File modificati / creati (riepilogo)

| File | Tipo | Modifiche |
|------|------|-----------|
| `PotSystemConfig.cs` | Modifica | ACTIVE_SLOT_COUNT=4, PASSIVE_SLOT_COUNT=3 |
| `CryoPlantPayload.cs` | Nuovo | Payload serializzabile per piante in cryo |
| `CryoSlot.cs` | Nuovo | Singolo slot passivo con Occupy/Free |
| `CryoMachineController.cs` | Nuovo | Gestore 3 slot, ServiceContainer, save/load |
| `DomePotRegistry.cs` | Modifica | GetActivePotsSnapshot() |
| `PotSlot.cs` | Modifica | ClearAllSelections usa GetActivePotsSnapshot |
| `PotActions.cs` | Modifica | TransferToCryo, RestoreFromCryo, ExtractFromCryoToStorage |
| `SPOR-BLK-01-03A-DayCycleController.cs` | Modifica | ApplyPassivePowers scaffold, chiamato in ExecuteDailyTick |
| `SaveManager.cs` | Modifica | GameSaveData.cryoSlots, CollectCryoSlotStates, ApplyCryoSlotStates |
| `PlantCardV3TerminalController.cs` | Modifica | PASSIVE protocollo, CRYO SEND/EXTRACT/RESTORE interattivi, InputState esteso, refresh HUD, toast |
| `CryoMachinePanel.uxml` | Nuovo | Layout HUD 3 slot cryo (UIToolkit) |
| `CryoMachinePanel.uss` | Nuovo | Stile HUD cryo |
| `CryoMachinePanelController.cs` | Nuovo | Controller UIDocument pannello cryo |
| `CryoMachineOpener.cs` | Nuovo | Interactable → apre pannello cryo |

---

## 7. Riepilogo per QA

- **Topologia:** Dome espone 4 slot attivi (`DomePotRegistry`) e 3 slot passivi (`CryoMachineController`) come categorie runtime distinte. Nessun sistema tratta tutti gli slot come equivalenti.
- **Trasferimenti end-to-end:**
  - `CRYO SEND POT-01` (pianta Lvl 5) → slot cryo occupato, pot resettato, HUD aggiornato, toast.
  - `CRYO EXTRACT` → flow interattivo → pianta in inventario come `WholePlant` con metadata Lvl 5, slot liberato, toast.
  - `CRYO RESTORE` → flow interattivo (slot + pot) → pianta reintrodotta nel pot attivo con metadata intatti, slot liberato, toast.
- **CryoMachine interagibile:** click sul GameObject `CryoMachine` in scena apre l'HUD UIToolkit con stato dei 3 slot.
- **DayCycleController:** i CryoSlot non entrano mai nel loop produttivo (`_registeredPots`); `ApplyPassivePowers` è chiamato ma è solo scaffold di debug.
- **Save/Load:** salvare con slot cryo occupati, ricaricare: i payload sono ripristinati integralmente da `GameSaveData.cryoSlots`.
- **Deperimento:** un `WholePlant` estratto da cryo è soggetto a `DeteriorationSystem` (−1 Quality/giorno → `OrganicScrap001` a 0). Avvertenza stampata nel terminale prima della conferma.

---

## 8. Note tecniche

- **ActivePower / PassivePower:** sono etichette descrittive (`ActivePowerLabel`, `PassivePowerLabel` su `PotStateModel` e `CryoPlantPayload`). Gli effetti gameplay reali (bonus vault, cap pH, scaling livello) sono esplicitamente rimandati a **Task 3** (slot passivi reali) e **Task 4** (poteri runtime scalabili).
- **ApplyPassivePowers scaffold:** il metodo in `DayCycleController` esiste e viene chiamato ogni tick, ma al momento registra solo log di debug. È il punto di integrazione designato per Task 3/4.
- **DeteriorationSystem:** `Items.WholePlant` (`"whole-plant"`) è nell'array `k_itemsToDeterioration` di `DeteriorationSystem.cs`. Il sistema è già attivo e funzionante; la degradazione della pianta Lvl 5 estratta a inventario avviene automaticamente senza modifiche aggiuntive.
- **MAX_POTS_PER_ROOM:** rimane a 10 in `PotSystemConfig` per compatibilità con `ValidatePotCount()`, ma non è mai chiamato dal core flow dopo Task 2. Da rimuovere in un futuro task di pulizia tecnica.
- **CryoMachineController nel ServiceContainer:** richiede che il GameObject `CryoMachine` con il componente sia presente e attivo nella scena prima del primo `DayCycleController.ExecuteDailyTick()` e prima del primo `SaveManager.SaveGame()`. Se assente, entrambi i sistemi gestiscono la mancanza con warn e skip.

---

*Fine DEV REPORT 0071.*
