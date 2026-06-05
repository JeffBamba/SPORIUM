# Elevator 3.0 — riferimento operativo

**Stato:** chiuso (2026-06-03)  
**Piano:** `.cursor/plans/elevator_3.0_unified.plan.md`  
**Report:** `Assets/Docs/REPORT/DEV_REPORT_0112_ELEVATOR_3_0_CORE_VIAGGIO_BUGFIX_2026-06-03.md`

## Loop gameplay

1. **Chiamata esterna** — avvicinarsi al display laterale del piano → `E` → `CallToFloor`.
2. **Ingresso cabina** — entrare nella zona profonda (`ELEV_CabinZone_*`) → porte chiuse, hint Su/Giù/W/S.
3. **Selezione target** — frecce o W/S accumulano il piano destinazione; debounce **1.2s** → partenza.
4. **Viaggio** — porte sorgente chiuse, player nascosto, camera segue `elevatorSection`, teleport su exit anchor, porte destinazione aperte.
5. **Uscita** — uscire dalla zona cabina → porte chiuse, display a riposo, input sbloccato.

## Script

| Script | Ruolo |
|--------|--------|
| `ElevatorSystem.cs` | Orchestratore: viaggio, display, porte, input cabina |
| `ElevatorDoorPair.cs` | Ante sx/dx, slide orizzontale, walk blockers, sorting animazione |
| `ElevatorFloorDisplay.cs` | Display + `Interactable` → chiamata piano |
| `ElevatorCabinZone.cs` | Trigger cabina profonda, exit multi-collider |

## Scena (`SCN_VaultMap`)

Sotto `ELEV_Elevator`:

- `ELEV_UseZone` — `ElevatorSystem` + trigger legacy use zone
- `ELEV_Doors_LVL_{+1,0,-1}` — porte giocabili (piano -2: slot array vuoto, Out of Service)
- `ELEV_Display_LVL_{+1,0,-1}` — display interagibili
- `ELEV_CabinZone_LVL_{+1,0,-1}` — zone cabina
- `ELEV_ExitAnchor_LVL_{+1,0,-1,-2}` — punti atterraggio post-viaggio

## Binding Inspector (`ElevatorSystem`)

- `levels[]`, `floorDoors[]`, `exitAnchors[]`, `travelVirtualCamera`, `elevatorSection`
- `selectionDebounceSeconds` = 1.2
- `floorDoors[3]` (-2) opzionale / null

## Pulizia legacy

- Menu Unity: **Tools → Sporae → Elevator → Remove Legacy UI_ElevatorPanel**
- Rimuove `UI_ElevatorPanel` (menu bottoni piano obsoleto) dalla scena aperta.

## Smoke test (regressione)

1. Raggiungere ogni piano giocabile (+1, 0, -1) via cabina.
2. Nessun softlock movimento dopo uscita / piano corrente / viaggio.
3. `E` sui display e Su/Giù in cabina senza conflitti col mover.
4. `SetLevel` (EndDay → spawn) posiziona cabina e chiude porte.
5. Piano -2: display/chiamata mostra Out of Service, nessun viaggio.

## Decisioni escluse

- **Timer 3s ritorno cabina a piano 0** — rimosso dal design (non implementato).
- **Costo CRY** — rimosso; gating piano -2 via `IsLevelUnlocked` only.
