# DEV REPORT 0114 — Walk area: transizioni elevator front (STEP 0 Elevator 4.0)

**Data:** 2026-06-08  
**Sprint / contesto:** Elevator 4.0 — STEP 0 benchmark piano -1: passaggio fluido tra `WalkAreaPerspective` (bed, cucina, elevator front) prima del refactor cabina fisica.  
**Riferimento piano:** `elevator_4.0_a954a67b.plan.md` (STEP 0 — fix transizioni walk area)  
**Report precedente:** `DEV_REPORT_0113_ELEVATOR_DISPLAY_MASK_DEBUG_2026-06-08.md`

---

## Sommario interventi

1. Esteso `PlayerPerspectiveMover2D` con switch continuo tra aree prospettiche: probe outward, validazione proiezione e commit centralizzato (`TryCommitAreaSwitch`).
2. Allineati in scena i corner del trapezio `ElevatorFrontWalkArea_LVL_-1` ai bordi condivisi con bed (sinistra) e cucina (destra), eliminando salto di scala al confine.
3. Risolto lo scattino residuo al cambio area: rimosso branch `probe` che causava ping-pong in overlap; trigger rifiutato se la nuova area proietta peggio dell’attuale.
4. Verificato in Play Mode con log NDJSON `debug-d2269f` (sessione debug); strumentazione rimossa a fix confermato.
5. Normalizzato nome scena `ElevatorFrontWalkArea_LVL_-1` e aggiornato `BoxCollider2D` AreaBounds sul nuovo trapezio.

---

## Statistiche e progresso

### Righe di codice

- **`PlayerPerspectiveMover2D.cs`:** **788 righe** totali file — `(Get-Content … | Measure-Object -Line).Lines`, 2026-06-08.
- **Diff non committato (2 file toccati):** **188 inserimenti / 34 rimozioni** — `git diff --stat` su `PlayerPerspectiveMover2D.cs` e `SCN_VaultMap.unity`.
- **`SCN_VaultMap.unity`:** 38 righe YAML modificate (corner elevator, collider, rename nodo).

### Sistemi funzionanti

- Passaggio WASD **bed ↔ elevator front ↔ cucina** al piano -1 — verificato in Play Mode dall’autore (“sembra ok”) e da log runtime post-fix.
- Continuità **profondità / scala player** (`PlayerDepthScaleAndSort` via `CurrentV`) — verificata: niente shrink visibile ai confini dopo allineamento corner.
- Switch area **outward-probe** e **position** — verificati nei log (`projectionError:0`, `moveDelta≈0.08`).
- **Linter IDE:** nessun errore su `PlayerPerspectiveMover2D.cs` dopo rimozione instrumentation.

### Bug risolti

- **3** — elenco:
  1. Player bloccato al bordo UV senza entrare nell’area adiacente (overlap collider + logica switch).
  2. Salto di scala (“shrink”) al passaggio bed/kitchen ↔ elevator front (corner trapezio disallineati).
  3. Micro-scattino / salto posizione ~0.63 m al confine bed↔elevator (trigger prematuro + ping-pong `probe`).

### Progresso gameplay / prodotto

- Il biologo attraversa il corridoio davanti all’ascensore senza restare incollato al bordo della camera o della cucina.
- Il passaggio tra stanze non produce più un evidente rimpicciolimento del personaggio.
- Lo scattino al confine è sostanzialmente eliminato; il movimento resta a passo costante (~0.08 m/frame) durante i cambi area.
- Il piano -1 è utilizzabile come **benchmark** per replicare la configurazione walk area sugli altri livelli (STEP 6 piano Elevator 4.0).
- Base stabile per il prossimo step: cabina fisica (`ElevatorCabinInteriorZone`) senza dipendere da UV cabin zone fragile.

---

## 1. Switch continuo tra PerspectiveWalkArea2D

### Problema

- Con WASD il player restava al bordo UV (`u≈0` / `u≈1`) dell’area corrente: `FindAreaByWorldPoint` restituiva l’area stessa o `null` finché il corpo non era interamente nel collider adiacente.
- In overlap tra `AreaBounds`, lo switch via probe generico (`source:"probe"`) alternava bed ed elevator nello stesso frame, corrompendo l’UV.
- Il trigger `OnTriggerEnter2D` switchava con errore di proiezione elevato (~0.55 m): UV clampato a `u=0` su posizione mondo ancora “bed-centric”, generando un passo movimento anomalo (~0.63 m) al frame successivo.

### Soluzione

- Aggiunti flag serializzati `enableContinuousAreaSwitch` e `areaSwitchProbeDistance` (default 0.35 m).
- `BuildAreaSwitchProbe` proietta un punto avanti lungo gli assi UV locali dell’area corrente.
- `FindTransitionAreaWhenPushingOutward` attiva lo switch solo se input spinge oltre soglia UV (`edge=0.04`) verso un’area diversa trovata dal probe.
- `TrySwitchAreaForMovement` usa solo due sorgenti: **`position`** (corpo nella nuova area) e **`outward-probe`** (spinta al bordo); rimosso il branch **`probe`**.
- `TryCommitAreaSwitch` centralizza validazione (`IsAreaSwitchProjectionAcceptable`, `maxAreaSwitchProjectionError`) e, per **`trigger`**, rifiuta lo switch se `candidateError > currentError + 0.05`.
- Validazione outward-probe usa il punto probe (non la posizione player ancora nell’area precedente), evitando rigetto per `projectionError` fuori soglia.

**Evidenza runtime (pre-fix vs post-fix):**

| Metrica | Pre-fix (frame 834–835) | Post-fix (run verifica) |
|---------|-------------------------|-------------------------|
| `moveDelta` al confine | 0.633 m | ~0.08 m |
| `source:"probe"` | presente | 0 occorrenze |
| Switch per frame | 2 (ping-pong) | 1 |
| Trigger err 0.55 | applicato | rifiutato (`worse fit`) |

**File interessati:**  
`Assets/_Project/Scripts/Player/PlayerPerspectiveMover2D.cs`

---

## 2. Allineamento scena ElevatorFrontWalkArea_LVL_-1

### Problema

- Il trapezio elevator front era ~3 m largo con corner non coincidenti ai bordi bed/cucina: al switch, `v` e `metersPerU` saltavano (`v` 0.53→0.72, `metersPerU` 9.0→2.1), causando shrink percepito e discontinuità orizzontale.

### Soluzione

- Corner elevator allineati ai seam mondo condivisi:
  - `NearLeft` / `FarLeft` ← bordo destro `WalkAreaPerspective_Bed`
  - `NearRight` / `FarRight` ← bordo sinistro `WalkAreaPerspective_cucina`
- Aggiornati `BoxCollider2D` offset/size sul nuovo trapezio (~4.17 × 1.9 m locali).
- Rimosso spazio finale nel nome GameObject: `ElevatorFrontWalkArea_LVL_-1`.

**Coordinate mondo risultanti (approx.):**

| Corner | World (x, y) |
|--------|----------------|
| NearLeft | (-1.03, -7.85) |
| NearRight | (2.94, -7.95) |
| FarLeft | (-0.72, -6.25) |
| FarRight | (2.22, -6.48) |

**File interessati:**  
`Assets/_Project/Scenes/SCN_VaultMap.unity`

---

## 3. Debug session e cleanup

### Problema

- Serviva evidenza runtime per distinguere ipotesi su UV flip, reproject, ping-pong e stallo al bordo.

### Soluzione

- Instrumentazione NDJSON temporanea (`debug-d2269f.log`, session `d2269f`) su switch, reproject e trigger.
- Ipotesi **J2** (reproject tug) e **J4** (stallo) **respinte** (`reprojectDelta:{0,0}`, `stallGap:0`).
- Ipotesi **J3** (ping-pong) e **J5** (errore proiezione) **confermate**; fix applicato e verificato.
- Tutta l’instrumentazione rimossa da `PlayerPerspectiveMover2D.cs` a fix confermato dall’autore.

**File interessati:**  
`Assets/_Project/Scripts/Player/PlayerPerspectiveMover2D.cs` (solo cleanup log; nessun file log in repo)

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/Player/PlayerPerspectiveMover2D.cs` | Switch continuo aree, `TryCommitAreaSwitch`, outward-probe, rimozione branch probe, guard trigger worse-fit, cleanup debug |
| `Assets/_Project/Scenes/SCN_VaultMap.unity` | Corner elevator front LVL -1, BoxCollider AreaBounds, rename nodo |

---

## Regole / vincoli rispettati

- **Architettura runtime:** nessun nuovo `FindObjectOfType`; logica confinata al mover player esistente.
- **Elevator 4.0 incrementale:** un solo step (STEP 0 walk area); nessun refactor `ElevatorSystem` / UV cabin in questo report.
- **Both (demo + full):** modifiche su `SCN_VaultMap` unico, senza fork scena demo.
- **Debug mode:** fix solo dopo evidenza log; instrumentation rimossa post-verifica.

---

## Note operative (Unity)

1. Aprire `SCN_VaultMap`, piano -1, Play Mode.
2. Verificare WASD: bed → elevator front → cucina e ritorno, più volte per direzione.
3. Controllare assenza shrink e scattino al confine (movimento ~uniforme).
4. Prossimo step piano Elevator 4.0: **STEP 1** cleanup benchmark scena -1, poi **STEP 2** `ElevatorCabinInteriorZone` (zona fisica cabina).

---

*Fine DEV REPORT 0114.*
