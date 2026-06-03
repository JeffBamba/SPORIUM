# DEV REPORT 0112 — Elevator 3.0: core viaggio, cabina e bugfix

**Data:** 2026-06-03  
**Sprint / contesto:** Elevator 3.0 — implementazione fasi 2–6 del piano unificato ascensore + iterazioni di debug/fix Play Mode.  
**Riferimento piano:** `.cursor/plans/elevator_3.0_unified.plan.md`  
**Report precedente:** `DEV_REPORT_0111_LAB40_GENOSCRITTORE_BLUEPRINT_UI_2026-05-27.md`

---

## Sommario interventi

1. Implementato il **core Elevator 3.0**: porte per piano, display interagibili, chiamata esterna, zona cabina, selezione target multi-piano, viaggio con camera + hide player + teleport su anchor.
2. Aggiunti i componenti **`ElevatorDoorPair`**, **`ElevatorFloorDisplay`**, **`ElevatorCabinZone`** e refactor esteso di **`ElevatorSystem`**.
3. Configurata la scena **`SCN_VaultMap`**: porte/display/zone cabina su +1/0/-1, exit anchor su tutti e 4 i piani, binding Cinemachine e walk areas.
4. Risolti **8 bug** di gameplay/input/visuale emersi in Play (atterraggio, timing hide, piano corrente, uscita cabina, display, collider multipli, sorting porte, debounce).
5. Rimossa la **strumentazione debug NDJSON** usata per il triage; ripristinato `using Sporae.DevTools` per logger/toast runtime.
6. **Escluso dal design** il timer 3s di ritorno automatico al piano 0 (decisione autore; piano aggiornato).

---

## Statistiche e progresso

### Righe di codice

- **1862 righe** sui 4 script ascensore — comando `(Get-Content …).Count`, 2026-06-03:
  - `ElevatorSystem.cs`: 1449
  - `ElevatorDoorPair.cs`: 227
  - `ElevatorCabinZone.cs`: 103
  - `ElevatorFloorDisplay.cs`: 83
- Modifiche scena su `SCN_VaultMap.unity` (porte, zone, anchor, parametri serializzati): **non conteggiate in LOC**.

### Sistemi funzionanti

- **Porte orizzontali per piano** (`OpenDoors` / `CloseDoors`, slide sx/dx, walk blockers).
- **Display laterali interagibili** (`CallToFloor`, `UpdateAllFloorDisplays`, freccia direzione).
- **Zona cabina profonda** con hint Su/Giù/W/S, target multi-piano, debounce **1.2s**, blocco input via `GameplayUiModalLock`.
- **Viaggio in cabina**: chiusura porte sorgente → scroll camera su `elevatorSection` → hide player → teleport su exit anchor → apertura porte destinazione → reveal player.
- **Uscita cabina**: chiusura porte, reset display, sblocco input.
- **Selezione piano corrente in cabina**: riapertura porte + uscita senza viaggio.
- **Validazione Play Mode** su flussi -1 ↔ 0 ↔ +1 e edge case piano corrente/uscita — confermata dall'autore in sessione debug.

### Bug risolti

- **8**
  1. Player atterrava **fuori dalla cabina** al piano +1 (collider zona troppo shallow + landing troppo vicino al fronte).
  2. Player **spariva prima** della fine animazione chiusura porte (hide non sincronizzato con `IsAnimating`).
  3. Selezionando il **piano corrente** in cabina le porte **non si riaprivano** e il player restava bloccato.
  4. Dopo riapertura piano corrente il player **non riceveva input WASD** (`Rigidbody2D.simulated` ripristinato in modo errato).
  5. Dopo uscita/viaggio i **display restavano in modalità selezione** (hint Su/Giù attivo).
  6. **Collider multipli** del player causavano exit prematuri dalla zona cabina (`OnTriggerExit2D` su un solo collider).
  7. Player **visibile sopra le porte** durante animazione chiusura/apertura (sorting order porte inferiore al player).
  8. **Attesa eccessiva** tra ultima pressione Su/Giù e partenza ascensore (debounce ridotto da 2.0s a 1.2s).

### Progresso gameplay / prodotto

- L'ascensore segue il loop UX 3.0: chiamata dal display → ingresso cabina → scelta piano → viaggio con scroll camera → arrivo dentro la cabina al piano scelto → uscita con chiusura porte.
- Su/Giù e W/S in cabina **non muovono più il player** nel mondo: selezionano solo il target ascensore.
- Il player **compare/scompare** in sync con le porte (hide dopo chiusura completa; show prima dell'apertura in arrivo).
- Se il player è già al piano desiderato, può **riaprire le porte** e uscire senza viaggio fantasma.
- Uscendo dall'ascensore i **comandi e i display tornano normali**; le porte si chiudono.
- Il piano +1 ora fa atterrare il player **dentro la zona cabina**, come -1 e 0.

---

## 1. Architettura Elevator 3.0 (fasi 2–6)

### Problema

- L'ascensore legacy usava menu UI piani, costo CRY e teleport player grezzo; mancavano porte per piano, display interattivi, zona cabina e viaggio camera coerente col layout 2.5D.

### Soluzione

- **`ElevatorDoorPair`**: coppia `PortelloneSx`/`PortelloneDx`, slide orizzontale, walk blockers (`BLK_DoorThreshold`), sorting order temporaneo durante animazione per occludere il player.
- **`ElevatorFloorDisplay`**: wrapper su `Interactable` → `CallToFloor(floorIndex)`; API `SetContent(label, direction)`.
- **`ElevatorCabinZone`**: trigger per piano con profondità cabina (`CabinaDepthFraction`); tracking multi-collider via `HashSet<Collider2D>` per exit definitivo.
- **`ElevatorSystem`**: orchestratore unico — registrazione display/zone, `CallToFloor`, selezione target in cabina, `DepartToTargetRoutine`, `TravelToFloorRoutine`, hide/show player, retarget Cinemachine su `elevatorSection`, landing su `exitAnchors[]` + `PerspectiveWalkArea2D`.
- Blocco input mondo: **`GameplayUiModalLock.SetBlockWorldInput`** (non `PlayerClickMover2D`).
- `SetLevel(int)` preservato per EndDay/spawn.

**File interessati:**  
`ElevatorSystem.cs`, `ElevatorDoorPair.cs`, `ElevatorFloorDisplay.cs`, `ElevatorCabinZone.cs`, `SCN_VaultMap.unity`

---

## 2. Bugfix atterraggio piano +1

### Problema

- Dopo viaggio verso +1 il player riappariva **fuori** dalla cabina (zona gialla), mentre -1 e 0 erano corretti.
- Causa: `ELEV_CabinZone_LVL_+1` con collider Y troppo basso (0.5) e landing calcolato troppo vicino al fronte porta.

### Soluzione

- In `GetCabinInteriorLandingPosition`: profondità landing aumentata (`CabinaDepthFraction + 0.3f`).
- In scena: `BoxCollider2D` di `ELEV_CabinZone_LVL_+1` ridimensionato (`Size.y` ~1.01, offset Y ~0.26).

**File interessati:**  
`ElevatorSystem.cs`, `SCN_VaultMap.unity`

---

## 3. Bugfix timing hide player (Animation A)

### Problema

- Il player spariva **prima** che le porte completassero la chiusura, risultando visibile "nel nulla" sopra le ante in movimento.

### Soluzione

- Aggiunta proprietà `ElevatorDoorPair.IsAnimating`.
- Coroutine `HidePlayerAfterCabinDoorsClose`: attende `doors.IsAnimating == false` prima di `SetPlayerHidden(true)`.
- `DepartToTargetRoutine`: stesso wait prima del viaggio.
- `CancelPendingCabinHide()` su interruzione/uscita forzata.

**File interessati:**  
`ElevatorSystem.cs`, `ElevatorDoorPair.cs`

---

## 4. Bugfix selezione piano corrente e uscita cabina

### Problema

- Se in cabina chiusa il player selezionava il **proprio piano**, le porte non si riaprivano o si riaprivano ma il player restava **senza movimento** e con **comandi ascensore ancora attivi**.
- Cause combinate: early return su target == current senza reopen; re-attivazione cabina da `OnTriggerStay`; reset errato di `Rigidbody2D.simulated`; exit parziale con collider multipli.

### Soluzione

- `TryDepartToTarget`: se `_targetIndex == currentLevelIndex` → `_suppressCabinActivationUntilExitFloor`, `LeaveCabinZone(..., closeDoorsOnExit: false)`, `OpenDoors`.
- `HandleCabinZoneContact`: early return se suppress attivo finché il player non esce del tutto.
- `NotifyPlayerExitedCabinZone`: cleanup completo su suppress (display, overlap, chiusura porte).
- `_playerRigidbodySimulationOverridden`: il sistema ripristina `simulated` solo se l'ha disabilitato lui.
- `ElevatorCabinZone`: exit notificato solo quando **tutti** i collider player hanno lasciato la zona.

**File interessati:**  
`ElevatorSystem.cs`, `ElevatorCabinZone.cs`

---

## 5. Bugfix display/comandi attivi dopo uscita

### Problema

- Dopo viaggio o uscita, i display restavano con hint "Usa ↑ ↓ o W S" e l'input ascensore sembrava ancora attivo.

### Soluzione

- `ResetDisplaysToOwnFloors()` su shallow approach e post-travel exit.
- `NotifyPlayerExitedCabinZone`: `CancelCabinArrowSelection(restoreDisplays: true)` anche se `_playerInsideCabinZone` era già false.
- Uscita cabina standard: `CloseDoors` + reset display.

**File interessati:**  
`ElevatorSystem.cs`

---

## 6. Bugfix occlusione visiva porte (sorting)

### Problema

- Durante chiusura/apertura il player restava **sopra** gli sportelloni (sort order porte < player).

### Soluzione

- `ElevatorDoorPair`: durante animazione alza temporaneamente `sortingOrder` delle sprite porte a `animationSortingOrder` (default 200), poi ripristina i valori originali.

**File interessati:**  
`ElevatorDoorPair.cs`

---

## 7. Tuning debounce e cleanup debug

### Problema

- Attesa 2s tra ultima pressione e partenza percepita troppo lenta.
- Log NDJSON temporanei (`AgentDebugLog`, `debug-4fed6a.log`) rimasti dopo il triage.

### Soluzione

- `selectionDebounceSeconds`: **2.0 → 1.2** (codice + scena).
- Rimossi tutti i blocchi `AgentDebugLog`, helper `BoolJson`/`F`, file log generato.
- Ripristinato `using Sporae.DevTools` (necessario per `SporiumLogger` / `ToastNotificationManager` runtime, non era strumentazione).

**File interessati:**  
`ElevatorSystem.cs`, `ElevatorDoorPair.cs`, `SCN_VaultMap.unity`

---

## 8. Decisione design: niente ritorno automatico a piano 0

### Problema

- Il piano prevedeva un timer 3s che riportava la cabina logica al piano 0 dopo l'uscita del player; in gameplay non aggiunge valore.

### Soluzione

- Funzionalità **non implementata** e **rimossa dal piano** (Fase 7 timer eliminata; hardening rinumerato).
- Comportamento attuale: la cabina resta al piano corrente; uscita = chiusura porte + display a riposo.

**File interessati:**  
`.cursor/plans/elevator_3.0_unified.plan.md`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/World/Elevator/ElevatorSystem.cs` | Refactor/estensione core 3.0 + bugfix stato cabina/viaggio/input |
| `Assets/_Project/Scripts/World/Elevator/ElevatorDoorPair.cs` | **Nuovo** — porte slide, blockers, sorting animazione |
| `Assets/_Project/Scripts/World/Elevator/ElevatorCabinZone.cs` | **Nuovo** — trigger cabina multi-collider |
| `Assets/_Project/Scripts/World/Elevator/ElevatorFloorDisplay.cs` | **Nuovo** — display + Interactable |
| `Assets/_Project/Scenes/SCN_VaultMap.unity` | Porte, display, zone cabina, exit anchor, parametri ascensore |
| `.cursor/plans/elevator_3.0_unified.plan.md` | Rimozione Fase 7 timer 3s; aggiornamento debounce e criteri |

---

## Regole / vincoli rispettati

- `SetLevel(int)` pubblico e invariato (EndDay/spawn).
- Blocco input via **`GameplayUiModalLock`**, non `PlayerClickMover2D`.
- Authoring manuale in scena (porte, display, zone, anchor); fallback non bloccanti su riferimenti null.
- Funzionalità **Both** (demo + gioco completo) sullo stesso binario `SCN_VaultMap`.
- Strumentazione debug temporanea rimossa a fine triage.

---

## Note operative (Unity)

- **Smoke test formali (Fase 0)** e **hardening scena/doc (Fase 7 piano)** restano da chiudere:
  - eliminare `UI_ElevatorPanel` dalla scena (oggi disattivato, non rimosso);
  - completare asset piano **-2** (porte/display/zona cabina) se richiesti visivamente;
  - aggiornare `SceneHierarchy.txt` con zone cabina e exit anchor;
  - 5 smoke test invarianti + flusso completo su tutti i piani giocabili.
- Verificare in Editor che `travelVirtualCamera`, `exitAnchors[]`, `floorDoors[]` e `floorLobbyWalkAreas[]` siano tutti bindati su `ElevatorSystem`.
- `ElevatorFloorDisplay` usa ancora `FindObjectOfType<ElevatorSystem>()` come fallback — da sostituire con ref serializzata o registry se si vuole allineamento pieno a `architecture-runtime-services`.

---

*Fine DEV REPORT 0112.*
