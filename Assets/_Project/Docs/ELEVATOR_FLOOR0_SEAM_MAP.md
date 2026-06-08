# Elevator 4.0 — Piano 0 seam map (STEP 0)

**Data:** 2026-06-08  
**Riferimento:** DEV_REPORT_0114 (triangolo bed / front / cucina su -1)

## Topologia (near row, world XY approx.)

| Corner / seam | World (x, y) | Ruolo |
|---------------|--------------|--------|
| `WalkAreaPerspective_DOME` **NearRight** | (-1.171, 2.21) | Seam ovest → front **NearLeft** (corner matched 2026-06-08) |
| `ElevatorFrontWalkArea_LVL_0` **NearLeft** | (-1.171, 2.21) | = bordo est dome |
| `ElevatorFrontWalkArea_LVL_0` **NearRight** | (2.830, 2.21) | Seam est → lab |
| `WalkAreaPerspective_lab` **NearLeft** | (2.830, 2.21) | = bordo ovest lab (corner matched 2026-06-08) |
| `ELEV_ExitAnchor_LVL_0` | (0.770, 3.390) | Dentro corridoio front (tra i seam) |

## Far row (profondità)

| Corner | World (x, y) |
|--------|----------------|
| DOME **FarRight** | (-1.178, 3.670) | = front **FarLeft** (corner matched) |
| Front **FarLeft** | (-1.178, 3.670) |
| Front **FarRight** | (2.820, 3.620) |
| LAB **FarLeft** | (2.820, 3.620) | = front **FarRight** (corner matched) |

## Collider AreaBounds (STEP 0b, come -1)

- `WalkAreaPerspective_DOME` e `WalkAreaPerspective_lab`: `BoxCollider2D` esteso in Y (offset y −1.37, size y 1.65) così la **near row** (~y 2.2) è dentro `ContainsWorldPoint` per lo switch outward-probe.
- **Overlap X ai seam** (pattern 0114): DOME collider esteso verso est (~fino a x≈0.5 mondo) e LAB verso ovest (~fino a x≈1.8) per overlap con `ElevatorFrontWalkArea_LVL_0` e switch `outward-probe` con `projectionError≈0`.
- `WalkAreaPerspective_lab` **FarLeft** y −0.55 allineato a front **FarRight** (y mondo ~3.62).

## Note

- **Visitor** (`WalkAreaPerspective_visitor`) è su Y ~8.7 — fuori dalla fascia corridoio dome/lab/ascensore (~2.2–3.7). Non nel triangolo seam.
- Larghezza corridoio front ~**4.0 m** (come -1 bed↔cucina).
- `limitLateralUWhenDeep: 0` su front; lab/dome invariati su clamp UV laterale.
- `floorLobbyWalkAreas[1]` → `ElevatorFrontWalkArea_LVL_0` (wired per fix atterraggio viaggio -1→0, stesso pattern 0116 su -1).
- `ELEV_CabinZone_LVL_0.walkArea` → front walk (non più `WalkAreaPerspective_lab`).

## Fase 2 — BLK laterali cabina (2026-06-08)

- `ELEV_Doors_LVL_0` figli: `BLK_CabinSide_L` / `BLK_CabinSide_R` (layer 7, stesso offset/size di -1).
- `ElevatorDoorPair.CacheCabinSideWalkBlockers` li rileva a runtime; attivi solo a **porte aperte**.
- `ElevatorCabinInteriorZone_LVL_0` resta **OFF** fino al flip cabina fisica (Fase 3).

## Play test (Fase 1c + 2)

1. **Piano 0:** dome → front → lab e ritorno, più volte.
2. Pass: no snap ~0.63 m, no shrink, movimento uniforme; con porte aperte i BLK laterali delimitano la tromba.
3. **Atterraggio:** viaggio -1 → 0 — player davanti ascensore su `ElevatorFrontWalkArea_LVL_0` (fix wiring 1d).
4. **Gate -1:** regressione rapida bedroom/cucina ↔ ascensore dopo ogni modifica su 0.
