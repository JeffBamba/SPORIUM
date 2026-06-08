# DEV REPORT 0117 — Elevator 4.0: rollout piano 0 (STEP 0 + wiring + atterraggio)

**Data:** 2026-06-08  
**Sprint / contesto:** Elevator 4.0 — rollout **piano gioco 0** (`floorIndex` 1) da benchmark -1 (0116), un passo per iterazione, gate regressione -1.  
**Riferimento piano:** STEP 6 / rollout piani in `DEV_REPORT_0116` (prossimo dopo chiusura -1)  
**Report precedente:** `DEV_REPORT_0116_ELEVATOR_4_STEP5_CHIUSURA_MENO1_2026-06-08.md`

---

## Sommario interventi

1. Creato `ElevatorFrontWalkArea_LVL_0` con seam allineati a dome (ovest) e lab (est); `limitLateralUWhenDeep: 0`.
2. Aggiornati corner `WalkAreaPerspective_lab` sul bordo ovest corridoio ascensore (split seam triangolo dome/front/lab).
3. Fix atterraggio viaggio **-1 → 0**: `floorLobbyWalkAreas[1]` e `ELEV_CabinZone_LVL_0.walkArea` → front walk (non più lab).
4. Aggiunti `BLK_CabinSide_L` / `BLK_CabinSide_R` sotto `ELEV_Doors_LVL_0` (pattern -1).
5. `ElevatorCabinInteriorZone_LVL_0` lasciata **disattivata**; cabina piano 0 ancora su legacy UV fino al flip fisico.
6. Documentazione seam: `Assets/_Project/Docs/ELEVATOR_FLOOR0_SEAM_MAP.md`.

---

## Statistiche e progresso

### Righe di codice

- **`SCN_VaultMap.unity`:** diff vs HEAD **+~420 / −9** righe YAML (front walk, seam lab, wiring landing, BLK LVL_0) — `git diff --shortstat`, 2026-06-08.
- **C# toccato in questa iterazione:** solo rimozione instrumentation debug in `ElevatorSystem.cs` (nessuna modifica logica permanente oltre al debug session).
- **`ELEVATOR_FLOOR0_SEAM_MAP.md`:** **N/D** righe totali non ricalcolate; file nuovo/aggiornato in repo.

### Sistemi funzionanti

- Atterraggio viaggio **-1 → 0** su `ElevatorFrontWalkArea_LVL_0` — **verificato** in Play Mode (conferma autore + log `debug-d2269f`: `areaSource: floorLobbyWalkAreas`, `delta: 0`).
- Triangolo seam dome/front/lab (STEP 0 scena) — **da validare in Editor** (passaggio WASD Fase 1c).
- `BLK_CabinSide_*` piano 0 — **da validare in Editor** (porte aperte/chiuse, regressione -1).
- Benchmark **-1** (STEP 5) — **da ri-verificare** dopo modifiche scena condivise su `ELEV_Elevator`.

### Bug risolti

- **1** — atterraggio sbagliato su piano 0 dopo viaggio da -1 (player proiettato su corner `WalkAreaPerspective_lab` invece che front ascensore).

### Progresso gameplay / prodotto

- Arrivando dal -1 al piano 0 il biologo spawna davanti all’ascensore nel corridoio corretto, non nel lab.
- Il corridoio ascensore al piano 0 ha una walk area dedicata allineata a dome e lab, come il modello -1 bed/cucina.
- I confini laterali della tromba cabina al piano 0 seguono lo stesso schema BLK del benchmark -1.
- Il piano 0 resta su cabina legacy UV (interior zone off): prossimo step è play test movimento + eventuale flip fisico.
- Rollout incrementale rispettato: nessun patch al mover senza evidenza; fix atterraggio solo wiring scena.

---

## 1. STEP 0 — `ElevatorFrontWalkArea_LVL_0` e seam

### Problema

Al piano 0 mancava una walk area ascensore dedicata; i passaggi dome ↔ ascensore ↔ lab rischiavano snap/shrink come risolto su -1 in 0114.

### Soluzione

- Nuovo nodo `ElevatorFrontWalkArea_LVL_0` sotto `ELEV_Elevator` (4 corner, `AreaBounds`, `limitLateralUWhenDeep: 0`).
- Corner near/far allineati a `WalkAreaPerspective_DOME` (ovest) e `WalkAreaPerspective_lab` (est, west edge spostato).
- Mappa seam: `ELEVATOR_FLOOR0_SEAM_MAP.md`.

**File interessati:**  
`SCN_VaultMap.unity`, `ELEVATOR_FLOOR0_SEAM_MAP.md`

---

## 2. Fix atterraggio -1 → 0

### Problema

Log runtime (`travel_cabin_landing`): `areaSource: cabinZoneWalkArea`, `areaName: WalkAreaPerspective_lab`, `landingPos: 2.820,3.780`, `interiorUv: 0.000,1.000`. Il teleport era preciso (`delta: 0`) ma il target UV era sull’angolo del lab.

### Soluzione

- `floorLobbyWalkAreas[1]` → `ElevatorFrontWalkArea_LVL_0` (3198001004).
- `ELEV_CabinZone_LVL_0.walkArea` → stesso front walk (non `WalkAreaPerspective_lab`).
- Post-fix log: `areaSource: floorLobbyWalkAreas`, `moverArea: ElevatorFrontWalkArea_LVL_0`, `landingPos: 0.755,3.646`.

**File interessati:**  
`SCN_VaultMap.unity`

---

## 3. BLK laterali `ELEV_Doors_LVL_0`

### Problema

Su piano 0 mancavano i collider `BLK_CabinSide_*` per delimitare la tromba cabina a porte aperte (presenti su -1 da 0115).

### Soluzione

- Aggiunti `BLK_CabinSide_L` e `BLK_CabinSide_R` come figli di `ELEV_Doors_LVL_0` (layer 7, size 0.15×2.5, offset identici a -1).
- `ElevatorDoorPair` li auto-cache da nome; `cabinSideWalkBlockers` in scena resta vuoto (comportamento uguale a -1).

**File interessati:**  
`SCN_VaultMap.unity`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scenes/SCN_VaultMap.unity` | Front walk LVL_0, seam lab, wiring landing, BLK cabin side LVL_0 |
| `Assets/_Project/Docs/ELEVATOR_FLOOR0_SEAM_MAP.md` | Seam map + checklist play test |
| `Assets/_Project/Scripts/World/Elevator/ElevatorSystem.cs` | Rimossa instrumentation debug (sessione atterraggio) |
| `Assets/Docs/REPORT/DEV_REPORT_0117_ELEVATOR_4_ROLLOUT_PIANO_0_2026-06-08.md` | **Nuovo** — questo report |

---

## Regole / vincoli rispettati

- **Rollout incrementale:** un passo scena alla volta; gate -1 documentato in seam map.
- **Nessun clamp UV laterale** sul front walk piano 0 (`limitLateralUWhenDeep: 0`).
- **Interior zone 0 OFF** fino al flip cabina fisica (no doppio binario con legacy).
- **Both (demo + full):** unica scena `SCN_VaultMap`.
- **Architettura runtime:** nessun nuovo `FindObjectOfType`; fix atterraggio senza patch mover.

---

## Note operative (Unity)

1. **Piano 0:** Play su `SCN_VaultMap` — WASD dome ↔ front ↔ lab (×3); verificare assenza snap ~0.63 m e shrink.
2. **Atterraggio:** da -1 chiamare ascensore → piano 0 → confermare spawn su front walk.
3. **BLK:** aprire porte LVL_0 — player non deve uscire lateralmente dalla tromba; a porte chiuse BLK off + threshold attivo.
4. **Gate -1:** bedroom/cucina ↔ ascensore ancora fluido (STEP 5 non regressione).
5. **Prossimo step:** play test Fase 1c/2 OK → valutare flip `ElevatorCabinInteriorZone_LVL_0` + disarm legacy (come 0116 su -1).

---

*Fine DEV REPORT 0117.*
