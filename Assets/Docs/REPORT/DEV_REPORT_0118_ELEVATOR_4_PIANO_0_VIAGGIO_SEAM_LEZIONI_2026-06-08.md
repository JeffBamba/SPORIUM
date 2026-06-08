# DEV REPORT 0118 — Elevator 4.0 piano 0: viaggio 0↔-1, seam walk e lezioni per rollout +1

**Data:** 2026-06-08  
**Sprint / contesto:** Continuazione rollout **piano gioco 0** (`floorIndex` 1) dopo `DEV_REPORT_0117`; debug end-to-end viaggio cabina **0 ↔ -1**, seam DOME/LAB ↔ ascensore, chiusura regressione multi-trip.  
**Riferimento piano:** Elevator 4.0 STEP 6 / rollout piani (`DEV_REPORT_0116`, `DEV_REPORT_0117`); seam map `ELEVATOR_FLOOR0_SEAM_MAP.md`  
**Report precedente:** `DEV_REPORT_0117_ELEVATOR_4_ROLLOUT_PIANO_0_2026-06-08.md`

---

## Sommario interventi

1. Seam walk piano 0 stabilizzati con pattern **0114** (solo `outward-probe` + `TryCommitAreaSwitch`; revert patch mover speculative).
2. Viaggio cabina **0 ↔ -1**: atterraggio su lobby walk UV, stato cabina preservato, porte aperte all’arrivo, input WASD coerente con stato porte.
3. **Bug multi-trip:** dopo ripetuti su/giù 0↔-1, uscita cabina al piano 0 bloccava passaggio verso DOME/LAB — fix contatore `_interiorZoneOverlapCount` + uscita shallow di sicurezza.
4. Scena: estensioni collider DOME/LAB per overlap seam; `ElevatorCabinInteriorZone_LVL_0` attiva nel flusso fisico (con guard overlap).
5. Raccolta **lezioni apprese** e checklist per rollout **piano +1** (`floorIndex` 0).

---

## Statistiche e progresso

### Righe di codice

- **`ElevatorSystem.cs`:** file corrente **2081** righe; diff vs HEAD **+183** (`git diff --stat`, 2026-06-08).
- **`PlayerPerspectiveMover2D.cs`:** file corrente **970** righe; diff vs HEAD **+7** (nettamente sotto soglia refactor; revert patch + `SetCurrentUV` / helper minimi).
- **`SCN_VaultMap.unity`:** diff vs HEAD **+756 / −175** righe YAML (`git diff --stat`).
- **Instrumentation debug:** aggiunta in sessione `d2269f`, **rimossa** dopo conferma fix utente.

### Sistemi funzionanti

- Viaggio cabina **0 → -1** e **-1 → 0** con atterraggio in cabina su griglia lobby — **verificato** (Play Mode + log sessione debug).
- Uscita cabina a piedi e passaggio **DOME ↔ front ↔ LAB** al piano 0 — **verificato** dopo fix overlap (conferma autore post-debug).
- Selezione piano W/S in cabina, porte aperte/chiuse, `SyncCabinInputWithDoorState` — **verificato** nella stessa iterazione.
- Seam walk piano 0 senza snap ~0.63 m (pattern 0114) — **verificato** in play test precedente alla regressione multi-trip; **da ri-verificare** dopo commit scena finale.
- Benchmark **-1** bedroom/cucina ↔ ascensore — **da ri-verificare** come gate obbligatorio post-modifica `ELEV_Elevator`.

### Bug risolti

- **8** (raggruppati per area):
  1. Seam walk 0: snap/shrink / `projection_reject` spam (revert patch mover non supportate da log).
  2. Atterraggio viaggio su walk area sbagliata (wiring `floorLobbyWalkAreas` / `walkArea` — già 0117, confermato in viaggio).
  3. Player bloccato in front walk -1 dopo arrivo da 0 (landing UV mondo vs lobby).
  4. Spawn davanti portelloni / perdita focus cabina (`EnterArrivalCabinState` vs viaggio in-cabin).
  5. Player fuori griglia gialla in cabina (landing su centro collider interior vs lobby UV).
  6. Input congelato in cabina (`BlocksWorldInput` + porte aperte).
  7. Porte che non si riaprivano dopo viaggio.
  8. **Multi-trip 0↔-1:** impossibile andare verso DOME/LAB dopo uscita cabina al piano 0 (`_interiorZoneOverlapCount` stale → `LeaveCabinZone` mai chiamato → BLK laterali + stato cabina errato).

### Progresso gameplay / prodotto

- Il biologo può fare più viaggi consecutivi tra piano 0 e -1 senza restare “incastrato” in cabina o nel corridoio ascensore.
- Uscendo dall’ascensore al piano 0 si raggiungono di nuovo DOME e LAB camminando, come sul benchmark -1.
- Il movimento in cabina segue la griglia gialla (walk area lobby), non il centro del trigger interior.
- Le porte si aprono all’arrivo per permettere l’uscita a piedi; W/S selezionano il piano solo con porte chiuse.
- Il rollout piano 0 è utilizzabile in play test ripetuto; prossimo piano (+1) ha checklist documentata sotto.

---

## 1. Seam walk piano 0 (DOME / front / LAB)

### Problema

Dopo il wiring scena (0117), passaggi dome ↔ ascensore ↔ lab mostravano snap, shrink o `projection_reject` ripetuti. Patch al mover (multi-step probe, `lateralSeamBand`, `FindAreaByProjectionExcluding`) peggioravano il comportamento rispetto al benchmark -1 (0114).

### Soluzione

- **Codice:** ripristinato modello 0114 in `PlayerPerspectiveMover2D` — solo `FindTransitionAreaWhenPushingOutward` (edge UV 0.04) + `TryCommitAreaSwitch`; trigger con guard `trigger_worse_fit`.
- **Scena:** estensione `BoxCollider2D` DOME/LAB in overlap X verso `ElevatorFrontWalkArea_LVL_0` (pattern 0114 bed/cucina); corner near/far documentati in `ELEVATOR_FLOOR0_SEAM_MAP.md`.
- **`SetCurrentUV`:** usato solo per atterraggio forzato post-viaggio (non come patch seam generale).

**File interessati:**  
`PlayerPerspectiveMover2D.cs`, `SCN_VaultMap.unity`, `ELEVATOR_FLOOR0_SEAM_MAP.md`

---

## 2. Viaggio cabina 0 ↔ -1 (atterraggio, porte, input)

### Problema

Iterazioni sul flusso `inCabinTransfer` (`_travelDepartedFromInsideCabin && HasPhysicalInteriorZone`) avevano causato:

| Sintomo | Causa radice (evidenza log) |
|--------|-----------------------------|
| Spawn davanti portelloni, focus perso | `EnterArrivalCabinState` sovrascriveva stato cabina; `DepartToTargetRoutine` non distingueva partenza da dentro |
| Fuori griglia gialla | Landing su centro collider `ElevatorCabinInteriorZone` invece che su UV lobby (`v≈1.0` deep cabina) |
| Input bloccato con porte aperte | `SetCabinWorldInputBlocked(true)` non ribaltato da `SyncCabinInputWithDoorState` |
| Porte chiuse all’arrivo | `CloseDoors` post-travel invece di `OpenDoors` per exit a piedi |

### Soluzione

- **`FinishInCabinTravelArrival`:** mantiene `CabinReadyForSelection`, `_playerInsideCabinZone = true`; non passa da `ArrivalWaitingExit`.
- **Landing in-cabin:** `landingArea.MapToWorld(landingU, 1f)` sulla lobby walk + `SetCurrentUV` forzato; interior trigger = solo logica, superficie movimento = trapezio lobby.
- **`DepartToTargetRoutine`:** a arrivo con player ancora in cabina → `OpenDoors`, `_holdDoorsOpenForCabinEntry`, `SyncCabinInputWithDoorState()`.
- **`SyncCabinInputWithDoorState` (Update):** porte aperte → WASD; porte chiuse → W/S selezione piano.

**File interessati:**  
`ElevatorSystem.cs`, `ElevatorCabinZone.cs`, `SCN_VaultMap.unity`

---

## 3. Bug multi-trip: overlap interior stale → walk area “bloccata”

### Problema

Dopo **2+ viaggi** 0↔-1, arrivando al piano 0 e uscendo dalla cabina, il player **non poteva** più camminare verso DOME né LAB. Evidenza debug (`debug-d2269f`, sessione `d2269f`):

- `_interiorZoneOverlapCount` restava **≥ 1** dopo teleport (mancava `OnTriggerExit` sul piano di partenza durante `TravelToFloorRoutine`).
- `NotifyInteriorZoneExit` decrementava ma non raggiungeva 0 → **`HandlePhysicalInteriorExit` / `LeaveCabinZone` non chiamati**.
- `_playerInsideCabinZone` restava `true` con porte aperte → **BLK_CabinSide** attivi → movimento laterale verso stanze adienti impedito (sembrava “problema walk area”).

### Soluzione

1. **Reset overlap prima dell’atterraggio:** `_interiorZoneOverlapCount = 0` nel coroutine di viaggio, prima del teleport (il teleport salta `OnTriggerExit`).
2. **Reset su arrivo cabina:** stesso contatore azzerato in `FinishInCabinTravelArrival` e `AbortTravel`.
3. **Uscita shallow di sicurezza** in `NotifyInteriorZoneStay`: se lobby `uv.y ≤ cabinaShallowV` ma stato logico ancora in cabina → `LeaveCabinZone` + chiusura porte + overlap azzerato.  
   Necessario perché su piani con `ElevatorCabinInteriorZone`, `HandleCabinZoneContact` fa **early return** (`HasPhysicalInteriorZone`) e non replica la logica shallow di -1.

**File interessati:**  
`ElevatorSystem.cs`

---

## 4. Lezioni apprese — checklist rollout **piano +1** (`floorIndex` 0)

### Principi (non negoziabili)

| # | Lezione | Perché |
|---|---------|--------|
| L1 | **La lobby walk area è la superficie di movimento** (griglia gialla). I trigger interior/cabina sono logica, non proiettare il player sul centro del `BoxCollider2D` interior. | Evita spawn fuori UV e seam errati. |
| L2 | **Ogni teleport tra piani deve resettare i contatori overlap** (`_interiorZoneOverlapCount`, e analoghi). `OnTriggerExit` non è affidabile con player nascosto/teleportato. | Causa #1 regressione multi-trip. |
| L3 | **Piani con `ElevatorCabinInteriorZone` non usano `HandleCabinZoneContact`** per shallow exit. Serve parità in `NotifyInteriorZoneStay` (o equivalente). | Floor 0 e +1 hanno interior fisico; -1 legacy UV usa cabin zone contact. |
| L4 | **Non patchare il mover senza log** che provano la causa. Pattern approvato: **solo 0114** (`outward-probe` edge 0.04). | Ogni “miglioramento” extra ha causato regressioni. |
| L5 | **Prima scena, poi codice** per i seam: corner allineati + overlap `BoxCollider2D` X/Y sulle near row, poi play test. | Come 0114/0117; il codice non compensa seam geometrici sbagliati. |
| L6 | **`limitLateralUWhenDeep: 0`** sul front walk del piano; clamp laterale solo dove serve davvero (cabina stretta). | Evita blocchi UV laterali nel corridoio. |
| L7 | **Test matrix obbligatoria: multi-trip** (≥3 cicli A→B→A) per ogni coppia di piani con interior zone, non solo singolo viaggio. | Il bug overlap emerge solo al 2°+ viaggio. |
| L8 | **Wiring landing:** `floorLobbyWalkAreas[i]` + `ELEV_CabinZone_LVL_*.walkArea` → **stesso** front walk; verificare in log che `areaName` non sia una stanza adiacente (es. lab). | Errore già visto su 0 (0117). |
| L9 | **BLK_CabinSide** sotto `ELEV_Doors_LVL_*` per ogni piano rollout; attivi solo a porte aperte. Uscita cabina deve chiamare `LeaveCabinZone` o i BLK sembrano “walk area rotte”. | Sintomo laterale identico a seam rotto. |
| L10 | **Gate regressione** sul piano benchmark precedente dopo ogni modifica a `ELEV_Elevator` / `ElevatorSystem`. | Un solo prodotto, scena condivisa. |

### Checklist operativa rollout +1 (da eseguire in ordine)

1. **STEP 0 scena:** creare `ElevatorFrontWalkArea_LVL_+1`, allineare corner con stanze ovest/est; documentare in `ELEVATOR_FLOOR+1_SEAM_MAP.md` (stesso schema di `ELEVATOR_FLOOR0_SEAM_MAP.md`).
2. **Collider:** estendere walk area stanze adiacenti in overlap X (e Y near row) verso front walk.
3. **Wiring:** `floorLobbyWalkAreas[0]` → front walk +1; `ELEV_CabinZone_LVL_+1.walkArea` → stesso riferimento.
4. **BLK:** `BLK_CabinSide_L/R` su `ELEV_Doors_LVL_+1` (copia parametri -1/0).
5. **Interior zone:** se attiva, applicare **da subito** reset overlap su teleport + shallow leave in `NotifyInteriorZoneStay` (già in codice condiviso).
6. **Play test singolo:** ingresso cabina, viaggio verso 0 o -1, uscita a piedi, seam verso stanze.
7. **Play test multi-trip:** ≥3 cicli +1 ↔ piano adiacente; dopo ogni arrivo verificare passaggio verso stanze laterali **senza** rientrare in cabina.
8. **Gate:** ripetere smoke test -1 e 0 (bedroom/cucina; dome/lab).

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/World/Elevator/ElevatorSystem.cs` | Viaggio in-cabin, sync input/porte, reset `_interiorZoneOverlapCount`, shallow force leave, cleanup debug |
| `Assets/_Project/Scripts/Player/PlayerPerspectiveMover2D.cs` | Pattern seam 0114, `SetCurrentUV`, cleanup debug |
| `Assets/_Project/Scripts/World/Elevator/ElevatorCabinZone.cs` | UV profondità / landing (supporto 4.0) |
| `Assets/_Project/Scenes/SCN_VaultMap.unity` | Seam 0, collider overlap, interior zone LVL_0, BLK, wiring |
| `Assets/_Project/Docs/ELEVATOR_FLOOR0_SEAM_MAP.md` | Seam map e note overlap collider |
| `Assets/Docs/REPORT/DEV_REPORT_0118_ELEVATOR_4_PIANO_0_VIAGGIO_SEAM_LEZIONI_2026-06-08.md` | **Nuovo** — questo report |

---

## Regole / vincoli rispettati

- **Rollout incrementale** (un piano per iterazione; gate -1 documentato).
- **Both (demo + full):** unica scena `SCN_VaultMap`, nessun fork demo.
- **Architettura runtime:** nessun nuovo `FindObjectOfType`; servizi invariati.
- **UI HUD:** nessuna modifica UIToolkit in questa iterazione.
- **Debug:** fix confermati a runtime prima della rimozione instrumentation.

---

## Note operative (Unity)

1. **Multi-trip 0↔-1 (×3):** uscire cabina al 0 → DOME e LAB raggiungibili; ripetere dopo ogni modifica ascensore.
2. **Logica overlap:** dopo teleport, in Inspector/log interno verificare che `_playerInsideCabinZone` diventi `false` entro uscita shallow (corridoio).
3. **Seam 0:** dome ↔ front ↔ lab ×3 senza snap; confrontare con gate -1.
4. **Prossimo step:** rollout **piano +1** seguendo checklist §4; non attivare interior zone su +1 senza overlap reset già presente in `ElevatorSystem` (condiviso).
5. **Commit:** includere scena + `ElevatorSystem.cs`; evitare di committare `Library/` / artefatti Bee.

---

*Fine DEV REPORT 0118.*
