# DEV REPORT 0113 — Elevator display, maschera portelloni e debug cabina

**Data:** 2026-06-08  
**Sprint / contesto:** Elevator 3.x — display world-space UITK, iterazioni di maschera porte/cabina e debug runtime verso Elevator 4.0.  
**Riferimento piano:** N/D — report basato esclusivamente sul contenuto attualmente in staging.  
**Report precedente:** `DEV_REPORT_0112_ELEVATOR_3_0_CORE_VIAGGIO_BUGFIX_2026-06-03.md`

---

## Sommario interventi

1. Aggiunto il display ascensore in-game basato su UI Toolkit, con UXML/USS dedicati, runtime world-space e guida operativa per posizionamento in Unity.
2. Esteso `ElevatorFloorDisplay` e `ElevatorSystem` per supportare stati visuali del display, fallback TMP legacy e hint ascensore sulla compact bottom bar.
3. Aggiornato il setup scena `SCN_VaultMap` per display, anchor, porte livello -1, maschera `elevator_right_mask`, collider soglia e documentazione gerarchia.
4. Rafforzata la logica portelloni con maschera, gestione sorting, renderer nascosti a porte aperte, walk blocker e stati `IsOpening` / `IsClosing`.
5. Aggiunta strumentazione debug NDJSON `DebugSessionLog_d2269f` per investigare i bug residui di ingresso/uscita cabina.

---

## Statistiche e progresso

### Righe di codice

- **Diff staged complessivo:** 24 file, **2597 inserimenti / 192 rimozioni** — comando `git diff --cached --stat`, 2026-06-08.
- **Diff staged `.cs`:** **1816 inserimenti / 127 rimozioni** sui file C# in staging — comando `git diff --cached --numstat -- "*.cs" "Assets/**/*.cs"`.
- **Nota:** conteggio utile per tracciabilità del batch; include strumentazione debug temporanea e non equivale a LOC finale pulito.

### Sistemi funzionanti

- **Linter IDE:** nessun errore rilevato sui file C# staged principali (`ElevatorSystem`, `ElevatorDoorPair`, `ElevatorCabinZone`, `ElevatorFloorDisplay`, `ElevatorInGameDisplayRuntime`, HUD e logger debug).
- **Display Elevator UITK:** da validare in Unity Editor su `SCN_VaultMap` dopo refresh asset e posizionamento finale degli anchor.
- **Loop cabina/porte:** in debug attivo; non dichiarato stabile in questa iterazione.
- **Compilazione Unity:** da validare in Editor dopo import asset, sprite mask, UXML/USS e scena.

### Bug risolti

- **0 confermati come risolti definitivamente in questa iterazione.**
- Sono presenti fix/mitigazioni e strumentazione per bug cabina/portelloni, ma l'autore ha segnalato bug ancora aperti; il report non li considera chiusi.

### Progresso gameplay / prodotto

- Il display ascensore passa da TMP legacy a una superficie UI Toolkit authorable, più vicina agli standard HUD/UI del progetto.
- Il piano -1 diventa benchmark per posizionare e duplicare il display world-space su altri piani.
- I portelloni ricevono una maschera grafica dedicata per rendere più credibile lo scorrimento nel muro.
- La compact bottom bar può mostrare hint ascensore senza sovraccaricare il display world-space.
- Il debug runtime produce evidenza NDJSON sui casi ancora instabili, utile per progettare Elevator 4.0.

---

## 1. Display Elevator UI Toolkit

### Problema

- I display ascensore erano ancora legati a testo TMP legacy, poco adatti a una presentazione coerente con i pannelli UI Toolkit già usati nel progetto.
- Serviva un benchmark sul piano -1 per posizionare il pannello in-game sopra la grafica dello schermo ascensore e poi duplicarlo sugli altri piani.

### Soluzione

- Aggiunti `ElevatorDisplay.uxml` e `ElevatorDisplay.uss` sotto `Assets/_Project/Resources/UI/UIToolkit/ElevatorDisplay/`.
- Aggiunto `ElevatorInGameDisplayRuntime.cs`, che crea a runtime una superficie world-space con canvas, RawImage, RenderTexture e UIDocument collegato al display.
- Esteso `ElevatorFloorDisplay` con `uiDisplayRuntime`, `SetPanelState`, fallback TMP legacy e localizzazione per modalità `CallRemote`.
- Aggiunta guida operativa `GUIDA_ELEVATOR_DISPLAY_SETUP.md` con gerarchia, anchor e regole UI Builder.

**File interessati:**  
`ElevatorDisplay.uxml`, `ElevatorDisplay.uss`, `ElevatorInGameDisplayRuntime.cs`, `ElevatorFloorDisplay.cs`, `GUIDA_ELEVATOR_DISPLAY_SETUP.md`, `SCN_VaultMap.unity`

---

## 2. Stati visuali display e hint HUD

### Problema

- Il display laterale doveva distinguere stato normale, chiamata remota, ingresso, cabina al piano, out of service e direzione viaggio.
- L'hint di selezione cabina non doveva competere con il display world-space.

### Soluzione

- Introdotto `ElevatorDisplayMode` con modalità `Normal`, `CallRemote`, `Enter`, `CabinAtFloor`, `OutOfService`.
- `ElevatorSystem` aggiorna i display con `PushDisplayState`, `ResolveDisplayMode`, `RefreshIdleDisplayIfNeeded` e label localizzate.
- `CompactBottomBarController`, `CompactBottomBar.uxml` e `CompactBottomBar.uss` ricevono il supporto per hint ascensore in zona bottom bar.

**File interessati:**  
`ElevatorSystem.cs`, `ElevatorFloorDisplay.cs`, `CompactBottomBarController.cs`, `CompactBottomBar.uxml`, `CompactBottomBar.uss`

---

## 3. Maschera portelloni e gerarchia floor -1

### Problema

- I portelloni dovevano scorrere visivamente nel muro senza creare mismatch di sorting o occlusione sul player.
- La maschera non doveva essere scalata insieme ai portelloni, altrimenti diventava difficile controllare il risultato visivo.

### Soluzione

- Aggiunto asset `elevator_right_mask.png` e relativa `.meta`.
- In scena, i portelloni del floor -1 sono stati raccolti sotto `ELEV_Doors_LVL_-1_portelloni`, mentre `BLK_DoorThreshold` ed `elevator_mask` restano separati nella root `ELEV_Doors_LVL_-1`.
- `ElevatorDoorPair` supporta `elevatorMask`, sorting mask durante animazione e renderer portelloni nascosti a porte completamente aperte quando la mask è presente.
- Aggiornato `SceneHierarchy.txt` per documentare la gerarchia risultante.

**File interessati:**  
`Map_vault_27.png`, `elevator_right_mask.png`, `SCN_VaultMap.unity`, `SceneHierarchy.txt`, `ElevatorDoorPair.cs`

---

## 4. Timing porte, walk blocker e stato cabina

### Problema

- Le iterazioni di debug hanno evidenziato confusione tra porta in apertura, porta aperta, ingresso cabina e chiusura.
- Il player poteva arrivare in zona cabina mentre le porte erano ancora in movimento, generando comportamenti visivi instabili.

### Soluzione

- `ElevatorDoorPair` espone `IsOpening`, `IsClosing`, `IsAnimating` e mantiene `BLK_DoorThreshold` attivo durante apertura/chiusura, disattivandolo solo a porte completamente aperte.
- `ElevatorSystem` introduce `_entryArmedByDoorsOpen`, hold porte per ingresso, gate `doors_opening` / `doors_closing` / `doors_closed` e coroutine per chiudere porte prima dell'hide player.
- `ElevatorCabinZone` è stato esteso con proiezione UV, anchor opzionali e gizmo, ma questa strada è ancora considerata fragile e sarà sostituita da helper fisici in Elevator 4.0.

**File interessati:**  
`ElevatorDoorPair.cs`, `ElevatorSystem.cs`, `ElevatorCabinZone.cs`, `SCN_VaultMap.unity`

---

## 5. Strumentazione debug runtime

### Problema

- I bug residui di ingresso/uscita cabina richiedono evidenza runtime: ordine eventi trigger, profondità, stato porte, hidden player, arrival/suppress.

### Soluzione

- Aggiunto `DebugSessionLog_d2269f.cs`, logger NDJSON temporaneo che scrive `debug-d2269f.log`.
- Inseriti log in `ElevatorSystem.HandleCabinZoneContact`, `NotifyPlayerExitedCabinZone` e `ElevatorCabinZone.OnTriggerExit2D`.
- La strumentazione è ancora presente nello staged e va rimossa solo dopo verifica finale o quando verrà superata dal redesign Elevator 4.0.

**File interessati:**  
`DebugSessionLog_d2269f.cs`, `ElevatorSystem.cs`, `ElevatorCabinZone.cs`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Art/Enviroments/Png/Map_vault_27.png` | Aggiornamento asset mappa |
| `Assets/_Project/Art/Enviroments/Png/elevator_right_mask.png` | Nuovo asset maschera portelloni |
| `Assets/_Project/Art/Enviroments/Png/elevator_right_mask.png.meta` | Meta Unity nuovo asset |
| `Assets/_Project/Docs/GUIDA_ELEVATOR_DISPLAY_SETUP.md` | Nuova guida setup display ascensore |
| `Assets/_Project/Docs/GUIDA_ELEVATOR_DISPLAY_SETUP.md.meta` | Meta Unity documento |
| `Assets/_Project/Docs/SceneHierarchy.txt` | Aggiornamento gerarchia scena elevator |
| `Assets/_Project/Resources/UI/UIToolkit/ElevatorDisplay.meta` | Cartella risorse UITK display |
| `Assets/_Project/Resources/UI/UIToolkit/ElevatorDisplay/ElevatorDisplay.uss` | Nuovo stile UI Toolkit display |
| `Assets/_Project/Resources/UI/UIToolkit/ElevatorDisplay/ElevatorDisplay.uss.meta` | Meta Unity USS |
| `Assets/_Project/Resources/UI/UIToolkit/ElevatorDisplay/ElevatorDisplay.uxml` | Nuovo layout UI Toolkit display |
| `Assets/_Project/Resources/UI/UIToolkit/ElevatorDisplay/ElevatorDisplay.uxml.meta` | Meta Unity UXML |
| `Assets/_Project/Scenes/SCN_VaultMap.unity` | Wiring display, porte, mask, anchor e collider |
| `Assets/_Project/Scripts/DevTools/DebugSessionLog_d2269f.cs` | Nuovo logger debug temporaneo |
| `Assets/_Project/Scripts/DevTools/DebugSessionLog_d2269f.cs.meta` | Meta Unity logger |
| `Assets/_Project/Scripts/UI/UIToolkit/ElevatorDisplay.meta` | Cartella script runtime display |
| `Assets/_Project/Scripts/UI/UIToolkit/ElevatorDisplay/ElevatorInGameDisplayRuntime.cs` | Nuovo runtime display world-space |
| `Assets/_Project/Scripts/UI/UIToolkit/ElevatorDisplay/ElevatorInGameDisplayRuntime.cs.meta` | Meta Unity runtime display |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/CompactBottomBarController.cs` | Supporto hint ascensore |
| `Assets/_Project/Scripts/World/Elevator/ElevatorCabinZone.cs` | Estensione trigger cabina/proiezione/debug |
| `Assets/_Project/Scripts/World/Elevator/ElevatorDoorPair.cs` | Maschera, blocker, stati apertura/chiusura, sorting |
| `Assets/_Project/Scripts/World/Elevator/ElevatorFloorDisplay.cs` | Bridge display UITK + fallback TMP |
| `Assets/_Project/Scripts/World/Elevator/ElevatorSystem.cs` | Stato display, chiamata, ingresso, debug e timing porte |
| `Assets/_Project/UI/UIToolkit/HUD/CompactBottomBar.uss` | Stile hint ascensore |
| `Assets/_Project/UI/UIToolkit/HUD/CompactBottomBar.uxml` | Slot hint ascensore |

---

## Regole / vincoli rispettati

- **UI Toolkit Builder parity:** UXML/USS dedicati al display, con guida che vieta inline style per proprietà di marca e mantiene il layout editabile in UI Builder.
- **Runtime architecture:** il display usa binding da `ElevatorFloorDisplay` / `ElevatorSystem`; la strumentazione debug è temporanea e dichiarata.
- **No eccezioni per floor come obiettivo:** il benchmark è floor -1, ma il report evidenzia che il sistema attuale va rifondato in Elevator 4.0 per eliminare soglie/behavior fragili per piano.
- **Debug mode:** log runtime mantenuti nello staged perché i bug cabina non sono ancora dichiarati chiusi.

---

## Note operative (Unity)

- Aprire `SCN_VaultMap` e validare import asset maschera, UXML/USS e script runtime display.
- Verificare `ELEV_Display_LVL_-1` e `ELEV_Display_LVL_0`: componente `ElevatorInGameDisplayRuntime`, child `ElevatorDisplayAnchor`, fallback TMP nascosto a runtime.
- Verificare `ELEV_Doors_LVL_-1`: `ELEV_Doors_LVL_-1_portelloni`, `BLK_DoorThreshold`, `elevator_mask` sibling e parametri `ElevatorDoorPair`.
- Rieseguire test Play Mode prima di rimuovere `DebugSessionLog_d2269f`.
- Per Elevator 4.0, proseguire col piano dedicato a `ElevatorFrontWalkArea`, quinte sceniche e helper fisici inside/outside cabina.

---

*Fine DEV REPORT 0113.*
