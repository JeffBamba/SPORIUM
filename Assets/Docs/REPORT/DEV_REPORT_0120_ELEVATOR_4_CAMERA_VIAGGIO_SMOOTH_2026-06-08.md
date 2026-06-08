# DEV REPORT 0120 — Elevator 4.0: camera viaggio fluida (ease + lock verticale)

**Data:** 2026-06-08  
**Sprint / contesto:** Elevator 4.0 — polish percezione viaggio in cabina post-UX 0119; eliminazione jitter/brusco sulla camera durante scroll verticale dello shaft, con asimmetria salita vs discesa risolta.  
**Riferimento piano:** Elevator 4.0 Fase 6 (viaggio camera / hide player)  
**Report precedente:** `DEV_REPORT_0119_ELEVATOR_4_UX_CHIAMATA_AUTO_PARTENZA_2026-06-08.md`

---

## Sommario interventi

1. **Easing verticale:** sostituito movimento lineare dello shaft con progresso curvato (`cabinTravelEase`, default `EaseInOut`) in `TravelToFloorRoutine`.
2. **Settle pre-atterraggio:** pausa configurabile (`cabinTravelEndSettleSeconds`, default **0,12 s**) tra fine corsa shaft e teleport player.
3. **Warp Cinemachine post-teleport:** `CinemachineCore.Instance.OnTargetObjectWarped` sul player dopo riposizionamento a fine viaggio.
4. **Lock camera in viaggio:** durante il travel, FramingTransposer disabilitato; offset Y catturato all’inizio; `ForceCameraPosition` in `LateUpdate` mantiene camera agganciata 1:1 allo shaft (fix discesa brusca da piano 0).
5. **Tuning Inspector:** `cabinTravelCameraYDamping` (default **0,05**, ripristinato a fine viaggio) per eventuale fine-tuning futuro se il lock venisse disabilitato.

---

## Statistiche e progresso

### Righe di codice

- **File `.cs` toccato (1):** `ElevatorSystem.cs` — **1870 righe** (`Measure-Object -Line`, 2026-06-08).
- **Diff vs HEAD:** non misurato in questa iterazione (working tree misto con altri artefatti Unity).

### Sistemi funzionanti

- Viaggio cabina 0↔-1 con camera fluida in **salita** — **verificato** (conferma autore + log debug sessione `d2269f`).
- Viaggio cabina in **discesa** (es. piano 0 → -1) senza scatto brusco iniziale — **verificato** (conferma autore post lock `LateUpdate`).
- Gate display, auto-partenza cabina, seam walk, overlap interior (0118–0119) — **non modificati**; regressione consigliata smoke test 0↔-1.
- Flusso W/S + E / auto-partenza — **non modificato**.

### Bug risolti

- **1** — camera tremolante / movimento brusco durante viaggio ascensore, in particolare **discesa** verso piano -1: FramingTransposer non seguiva lo shaft in partenza (gap iniziale ~1,1 unità tra `camY` e `shaftY` con shaft già in movimento).

### Progresso gameplay / prodotto

- Il viaggio in cabina si percepisce continuo: partenza e arrivo morbidi grazie all’ease, senza “strappo” visivo in discesa.
- La camera resta allineata allo scroll verticale dello shaft per tutta la corsa, simmetricamente in salita e in discesa.
- L’atterraggio non introduce snap evidenti sulla camera grazie al warp post-teleport player e al breve settle opzionale.
- Parametri viaggio camera esposti in Inspector per tuning senza toccare codice.
- Base pronta per rollout **+1** con lo stesso `TravelToFloorRoutine`.

---

## 1. Jitter e partenza brusca in discesa

### Problema

Durante `TravelToFloorRoutine`, lo shaft (`elevatorSection`) si muoveva con interpolazione lineare per frame; la Virtual Camera seguiva tramite `FramingTransposer` con damping Y (~0,3). In **discesa** da piano 0, i log di debug mostravano:

- `travel_start`: `shaftY=4,560`, `camY=3,440` (gap **−1,12**).
- Entro `t≈0,25`: lo shaft era già sceso di ~1,5 unità mentre la camera si era spostata di ~0,13 → effetto “mondo che cade” con camera indietro.

In **salita**, Cinemachine effettuava uno snap iniziale più rapido e il problema era meno evidente.

### Soluzione

- **Progresso eased:** `EvaluateTravelEase(t)` su `AnimationCurve` serializzata (`cabinTravelEase`).
- **Lock verticale dedicato:**
  - `BeginCameraTravelFollow(shaftStartY)`: salva follow, disabilita `CinemachineFramingTransposer`, imposta follow su `elevatorSection`, cattura `_travelCameraYOffset = camY − shaftStartY`.
  - `LateUpdate`: se `_travelCameraLockActive`, `ApplyTravelCameraLock(_travelShaftYForCamera)` via `travelVirtualCamera.ForceCameraPosition` (offset Y costante per tutta la corsa).
  - `RestoreCameraFollow()`: disattiva lock, ripristina damping e enabled del transposer, ripristina follow player.
- **Settle:** `WaitForSeconds(cabinTravelEndSettleSeconds)` prima del teleport player.
- **Warp player:** `OnTargetObjectWarped(_travelPlayer, playerWarpDelta)` dopo teleport landing.

**File interessati:**  
`ElevatorSystem.cs` — `TravelToFloorRoutine`, `BeginCameraTravelFollow`, `ApplyTravelCameraLock`, `RestoreCameraFollow`, `LateUpdate`, `EvaluateTravelEase`

---

## 2. Parametri Inspector (viaggio camera)

| Campo | Default | Ruolo |
|-------|---------|--------|
| `cabinTravelEase` | `EaseInOut(0,0,1,1)` | Curva partenza/arrivo morbida |
| `cabinTravelEndSettleSeconds` | `0.12` | Pausa a shaft fermo prima del teleport |
| `cabinTravelCameraYDamping` | `0.05` | Damping Y salvato/ripristinato (transposer off durante lock) |
| `cabinTravelSpeed` | `6` | Velocità shaft (invariata) |

---

## 3. Debug e compile fix (sessione interna)

### Problema

- Sessione debug con log NDJSON su `debug-d2269f.log` per validare ipotesi H1–H4.
- Typo post-cleanup instrumentation: `if (cabinTravelEndSettleSeconds > 0f))` → **CS1525**.

### Soluzione

- Instrumentation rimossa dopo conferma autore.
- Parentesi extra corretta; compilazione ripristinata.

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/World/Elevator/ElevatorSystem.cs` | Ease viaggio, lock camera `LateUpdate`, settle, warp player, API camera travel |
| `Assets/Docs/REPORT/DEV_REPORT_0120_ELEVATOR_4_CAMERA_VIAGGIO_SMOOTH_2026-06-08.md` | **Nuovo** — questo report |

---

## Regole / vincoli rispettati

- **Both (demo + full):** un solo `ElevatorSystem` su `SCN_VaultMap`; nessun fork demo.
- **Architettura:** nessun nuovo `FindObjectOfType`; riferimenti serializzati (`travelVirtualCamera`, `elevatorSection`) invariati.
- **Scope:** nessuna modifica a seam walk, display gate, auto-partenza, overlap interior.
- **UI Builder parity:** non applicabile (solo camera runtime).

---

## Note operative (Unity)

1. **Play test:** almeno un viaggio **0 → -1** e **-1 → 0**; verificare assenza scatto in discesa e continuità in salita.
2. **Inspector `ElevatorSystem`:** regolare `cabinTravelEase` / `cabinTravelEndSettleSeconds` se la corsa risulta troppo lenta o troppo “morbida” all’atterraggio.
3. **Virtual Camera:** deve restare collegata a `travelVirtualCamera`; componente `CinemachineFramingTransposer` richiesto (viene disabilitato solo durante il viaggio).
4. **Regressione:** smoke test ingresso/uscita cabina + walk DOME/LAB dopo multi-trip (checklist 0118).
5. **Rollout +1:** riusare stesso pattern `TravelToFloorRoutine` + lock camera; verificare Y `levels[]` e durata corsa.

---

*Fine DEV REPORT 0120.*
