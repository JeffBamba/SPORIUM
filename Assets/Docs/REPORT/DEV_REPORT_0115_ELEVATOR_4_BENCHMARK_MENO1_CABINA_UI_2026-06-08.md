# DEV REPORT 0115 — Elevator 4.0: benchmark -1 cabina fisica, confini tromba, UI lampeggiante

**Data:** 2026-06-08  
**Sprint / contesto:** Elevator 4.0 — STEPs 1–5 sul piano -1 (benchmark): ingresso cabina fisico, conferma viaggio con E, confini laterali tromba, etichette display e hint HUD.  
**Riferimento piano:** `elevator_4.0_a954a67b.plan.md` (STEPs 1–5); `elevator_4_ux_7fdd08a3.plan.md` (helper fisici)  
**Report precedente:** `DEV_REPORT_0114_WALK_AREA_ELEVATOR_FRONT_STEP0_2026-06-08.md`

---

## Sommario interventi

1. Introdotto `ElevatorCabinInteriorZone` e collegamento in `ElevatorSystem` per ingresso/uscita cabina via trigger fisico (benchmark -1; oggetti scena anche per +1 e 0).
2. Consolidato flusso Elevator 4.0 su -1: stati `ElevatorFlowState`, selezione piano con W/S solo a porte chiuse, partenza solo con **E**, hint in `CompactBottomBarController.zone-post-center`.
3. Aggiunti `BLK_CabinSide_L/R` su `ELEV_Doors_LVL_-1` con `SyncDoorWalkBlockers`: attivi solo a porte aperte; disattivi a porte chiuse per non bloccare bedroom/cucina.
4. Aggiornate etichette display ascensore (`elevd-direction-label` / riga piano): testi localizzati in maiuscolo, stati call/select/idle coerenti con UX 4.0.
5. Lampeggio opacità su `elevd-direction-label` (sempre quando visibile) e su hint ascensore in bottom bar (`UiToolkitOpacityBlinker`).
6. Rimossa strumentazione debug `DebugSessionLog_d2269f`; ritirato clamp UV laterale sul mover (causava scatto bed/cucina → ascensore).

---

## Statistiche e progresso

### Righe di codice

- **File `.cs` toccati (9):** **3918 righe** totali — `(Get-Content … | Measure-Object -Line).Lines` per file, 2026-06-08 (`ElevatorSystem` 1596, `ElevatorDoorPair` 372, `ElevatorCabinInteriorZone` 84, `ElevatorCabinZone` 223, `ElevatorFloorDisplay` 149, `ElevatorInGameDisplayRuntime` 576, `CompactBottomBarController` 714, `UiToolkitOpacityBlinker` 49, `PerspectiveWalkArea2D` 155).
- **Diff working tree vs HEAD:** **18 file**, **+1228 / −260** — `git diff HEAD --shortstat`, 2026-06-08.

### Sistemi funzionanti

- Chiamata ascensore, apertura/chiusura porte, ingresso cabina fisico su **-1** — verificato in Play Mode (sessione debug + conferma autore post-fix).
- Selezione piano in cabina (W/S / frecce) e conferma viaggio con **E** — verificato su benchmark -1.
- Confini laterali tromba (`BLK_CabinSide`) senza interferenza su navigazione bedroom/cucina a porte chiuse — verificato post-fix toggle porte.
- Etichette display e hint bottom bar lampeggianti — implementato; da rivedere visivamente in UI Builder / Play.

### Bug risolti

- **4** — elenco:
  1. Player usciva lateralmente dalla tromba visiva della cabina pur restando in logica ascensore (assenza muri laterali).
  2. Collider laterali sempre attivi bloccavano bedroom/cucina vicino all’ascensore.
  3. Clamp UV laterale su `ElevatorFrontWalkArea` teletrasportava il player all’ingresso da bed/cucina (regressione movimento).
  4. Flusso ingresso cabina instabile su soglia porte / timing (grace `cabinLobbyDeepV`, chiusura differita, input cabina solo a porte chiuse) — stabilizzato nel benchmark -1.

### Progresso gameplay / prodotto

- Il biologo entra in cabina al -1 in modo prevedibile: porte, trigger fisico e hint guidano il flusso fino alla conferma del piano.
- Non si esce più “di lato” dalla tromba dell’ascensore mentre si controlla il pannello.
- Bedroom e cucina restano percorribili quando l’ascensore è chiuso; i muri laterali compaiono solo con porte aperte.
- La riga direzione del display e l’hint in basso lampeggiano per distinguerli dal piano sotto e attirare l’attenzione su “Premi E…”.
- Benchmark -1 pronto come modello; rollout +1/0/-2 e pulizia legacy restano step successivi.

---

## 1. Ingresso cabina fisico (Elevator 4.0 STEP 2–3)

### Problema

`ElevatorCabinZone` derivava “dentro cabina” da soglie UV (`cabinaDeepV`) sul trapezio walk area — comportamento fragile e diverso per piano.

### Soluzione

- Nuovo `ElevatorCabinInteriorZone` (`BoxCollider2D` trigger, `floorIndex`, `NotifyInteriorZoneEnter/Exit`).
- `ElevatorSystem.HandlePhysicalInteriorEnter/Exit` attiva `ActivateCabinInterior` quando il player è deep sul pianerottolo (`IsPlayerDeepEnoughOnLobbyWalkArea` + `floorLobbyWalkAreas[2]` → `ElevatorFrontWalkArea_LVL_-1`).
- `ElevatorCabinZone` resta in scena ma **non gestisce** ingresso se `HasPhysicalInteriorZone(floorIndex)` è true.
- In scena: `ElevatorCabinInteriorZone_LVL_-1` (floorIndex 2); placeholder anche per `LVL_0` e `LVL_+1` (non ancora validati in Play).

**File interessati:**  
`ElevatorCabinInteriorZone.cs`, `ElevatorSystem.cs`, `ElevatorCabinZone.cs`, `SCN_VaultMap.unity`

---

## 2. Conferma E e stati espliciti (STEP 4–5 parziale)

### Problema

Partenza automatica da debounce e stati impliciti rendevano il flusso cabina opaco.

### Soluzione

- Enum `ElevatorFlowState` e `SetFlowState` lungo call / doors / cabin / travel / arrival.
- `UpdateCabinSelectionInput`: W/S e frecce solo con porte **chiuse**; `E` / Space chiama `TryDepartToTarget` quando la selezione è attiva.
- Hint cabina su `CompactBottomBarController.SetElevatorHint` / `ClearElevatorHint` (`cabinConfirmHint` serializzato su `ElevatorSystem`).
- Parametri scena -1: `cabinLobbyDeepV` 0.92, `minDoorsOpenBeforeCabinEntrySeconds` 0.45, `cabinDoorCloseDelaySeconds` 0.75.

**Nota:** `selectionDebounceSeconds` / `_selectionDebounceRemaining` restano in codice come residuo legacy (sempre −1); rimozione prevista in chiusura STEP 5.

**File interessati:**  
`ElevatorSystem.cs`, `CompactBottomBarController.cs`, `SCN_VaultMap.unity`

---

## 3. Confini laterali tromba cabina (BLK_CabinSide)

### Problema

Log `debug-d2269f`: con porte aperte il player raggiungeva `u≈0/1` su `ElevatorFrontWalkArea_LVL_-1` senza hit su `BLK_*` laterali (`hitLeft/hitRight: none`), uscendo visivamente dalla cabina.

### Soluzione

- Oggetti scena `BLK_CabinSide_L` / `BLK_CabinSide_R` (layer 7) sotto `ELEV_Doors_LVL_-1`.
- `ElevatorDoorPair.SyncDoorWalkBlockers`: `BLK_DoorThreshold` attivo a porte chiuse; `BLK_CabinSide_*` attivi **solo a porte completamente aperte**.
- Tentativo clamp UV su `PerspectiveWalkArea2D` + mover **rimosso** dopo regressione (scatto ~0.63 m al confine bed/cucina).

**Evidenza:** log pre-fix `u=1.000, v=0.435, hitLeft=none`; post-fix navigazione laterale bedroom/cucina libera a porte chiuse, blocco tromba a porte aperte.

**File interessati:**  
`ElevatorDoorPair.cs`, `PerspectiveWalkArea2D.cs` (API clamp opzionale, disattivata su -1), `SCN_VaultMap.unity`

---

## 4. Display ascensore e hint HUD lampeggianti

### Problema

Riga direzione (`elevd-direction-label`) poco distinguibile dalla riga piano sotto; hint “Premi E…” poco visibile in `zone-post-center`.

### Soluzione

- Testi display in maiuscolo via `ToDisplayUpper` in `ElevatorInGameDisplayRuntime` ( `-unity-text-transform` non affidabile su render texture).
- Stati UX: idle/cabin → “TI TROVI AL”; call → “OCCUPATO” (rosso); select → “STAI ANDANDO A”; moving con freccia.
- Nuovo `UiToolkitOpacityBlinker` (schedule UITK, ciclo ~420 ms, opacità 1 ↔ 0.38).
- Applicato a `elevd-direction-label` quando il testo è non vuoto; a `elevator-hint-label` quando `SetElevatorHint` è attivo.
- Classi USS documentative: `elevd-direction-label--blink`, `cbb-elevator-hint-label--blink`.

**File interessati:**  
`UiToolkitOpacityBlinker.cs`, `ElevatorInGameDisplayRuntime.cs`, `ElevatorDisplay.uss`, `ElevatorDisplay.uxml`, `ElevatorFloorDisplay.cs`, `CompactBottomBar.uss`, `CompactBottomBarController.cs`

---

## 5. Debug e cleanup

### Problema

Sessione debug `d2269f` e file `DebugSessionLog_d2269f` usati per validare ipotesi confini cabina.

### Soluzione

- Instrumentazione rimossa da `PlayerPerspectiveMover2D` e `ElevatorCabinInteriorZone`.
- File `Assets/_Project/Scripts/DevTools/DebugSessionLog_d2269f.cs` eliminato.

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/World/Elevator/ElevatorSystem.cs` | Flusso 4.0, stati, interior zone, hint, parametri cabina |
| `Assets/_Project/Scripts/World/Elevator/ElevatorCabinInteriorZone.cs` | **Nuovo** — trigger fisico cabina |
| `Assets/_Project/Scripts/World/Elevator/ElevatorCabinZone.cs` | Bypass se interior zone presente |
| `Assets/_Project/Scripts/World/Elevator/ElevatorDoorPair.cs` | `SyncDoorWalkBlockers`, `BLK_CabinSide_*` |
| `Assets/_Project/Scripts/World/Elevator/ElevatorFloorDisplay.cs` | Allineamento stati display |
| `Assets/_Project/Scripts/World/VaultMap/PerspectiveWalkArea2D.cs` | API clamp laterale opzionale (off su -1) |
| `Assets/_Project/Scripts/UI/UIToolkit/UiToolkitOpacityBlinker.cs` | **Nuovo** — lampeggio opacità UITK |
| `Assets/_Project/Scripts/UI/UIToolkit/ElevatorDisplay/ElevatorInGameDisplayRuntime.cs` | Testi maiuscoli, blink direction label |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/CompactBottomBarController.cs` | Blink hint ascensore |
| `Assets/_Project/Resources/UI/UIToolkit/ElevatorDisplay/ElevatorDisplay.uss` | Stili direction + blink |
| `Assets/_Project/Resources/UI/UIToolkit/ElevatorDisplay/ElevatorDisplay.uxml` | Struttura direction row |
| `Assets/_Project/UI/UIToolkit/HUD/CompactBottomBar.uss` | Stile hint blink |
| `Assets/_Project/Scenes/SCN_VaultMap.unity` | Interior zone, BLK laterali, binding `floorLobbyWalkAreas`, tuning |
| `Assets/_Project/Docs/SceneHierarchy.txt` | Aggiornamento gerarchia |
| `Assets/_Project/Scripts/DevTools/DebugSessionLog_d2269f.cs` | **Eliminato** |

---

## Regole / vincoli rispettati

- **Architettura runtime:** servizi via `ServiceContainer` (`CompactBottomBarController`); nessun nuovo `FindObjectOfType` in gameplay elevator.
- **Both (demo + full):** unica scena `SCN_VaultMap`.
- **UI Builder parity:** stili lampeggio in USS; override opacità documentato come funzionale in controller.
- **Elevator 4.0 incrementale:** benchmark -1; interior zone +1/0 in scena ma non validate; STEP 6 rollout pendente.

---

## Note operative (Unity)

1. Play su `SCN_VaultMap`, piano **-1**: chiamata → ingresso cabina → W/S → **E** → viaggio → uscita.
2. Verificare bedroom/cucina con porte **chiuse** (nessun blocco laterale) e tromba con porte **aperte**.
3. Controllare lampeggio `elevd-direction-label` e hint “Premi E…” in bottom bar.
4. **Non in questo report:** rollout `ElevatorFrontWalkArea` + BLK laterali su +1/0/-2; quinte sceniche; `landingPoint`; rimozione debounce legacy.

---

*Fine DEV REPORT 0115.*
