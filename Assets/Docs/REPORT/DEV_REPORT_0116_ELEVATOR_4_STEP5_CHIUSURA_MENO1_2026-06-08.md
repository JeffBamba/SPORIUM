# DEV REPORT 0116 — Elevator 4.0: chiusura STEP 5 benchmark -1 (solo cabina fisica)

**Data:** 2026-06-08  
**Sprint / contesto:** Elevator 4.0 — **STEP 5** sul piano **-1** (benchmark): disinnesco UV legacy, cabina gestita solo da `ElevatorCabinInteriorZone` + collider scena.  
**Riferimento piano:** `elevator_4.0_a954a67b.plan.md` (STEP 5 — cleanup legacy su -1)  
**Report precedente:** `DEV_REPORT_0115_ELEVATOR_4_BENCHMARK_MENO1_CABINA_UI_2026-06-08.md`

---

## Sommario interventi

1. Chiuso STEP 5 su **-1**: ingresso/uscita cabina affidati al **trigger fisico** (`ElevatorCabinInteriorZone`), senza gate UV su enter/exit.
2. `ElevatorCabinZone` legacy disinnescato su -1 (scena + `DisarmLegacyCabinZoneAtFloor` a runtime alla registrazione interior zone).
3. `ResolveFloorLobbyWalkArea` preferisce `floorLobbyWalkAreas[2]` (`ElevatorFrontWalkArea_LVL_-1`) prima del walk area del legacy zone.
4. Grace post-apertura porte su piani con interior zone: blocco uniforme per `minDoorsOpenBeforeCabinEntrySeconds`, senza check `cabinLobbyDeepV`.
5. `OpenDoors` / `DeferredCabinZoneCheck` saltano il path UV legacy se `HasPhysicalInteriorZone(floorIndex)`.
6. Rimosso `selectionDebounceSeconds` residuo dalla scena; `ElevatorCabinZone` documentato come fallback 3.x per piani senza interior zone.

---

## Statistiche e progresso

### Righe di codice

- **File toccati in questa chiusura STEP 5 (3):** **1600** (`ElevatorSystem.cs`) + **223** (`ElevatorCabinZone.cs`) + scena YAML — `(Get-Content … | Measure-Object -Line).Lines`, 2026-06-08.
- **Diff vs HEAD (solo i 3 file sopra):** **+54 / −52** — `git diff HEAD --shortstat`, 2026-06-08.

### Sistemi funzionanti

- Flusso Elevator 4.0 su **-1** (call → ingresso fisico → W/S a porte chiuse → **E** → viaggio) — **da validare in Editor** dopo chiusura STEP 5 (non rieseguito Play test in questa iterazione).
- Confini tromba `BLK_CabinSide_*` e navigazione bedroom/cucina — ereditati da 0115; **da ri-verificare** insieme al nuovo bypass UV.
- Piani **+1 / 0 / -2** — ancora su logica legacy o rollout incompleto (STEP 6 pendente).

### Bug risolti

- **0** in questa iterazione — intervento di **chiusura architetturale** STEP 5, non bugfix isolato. I fix confini/snap documentati in 0115 restano la base; questo report rimuove il doppio binario UV su -1.

### Progresso gameplay / prodotto

- Sul **-1** la cabina non dipende più da soglie UV sul trapezio walk area: entrare/uscire segue il volume fisico della cabina.
- Il pianerottolo ascensore usa la walk area corretta (`ElevatorFrontWalkArea_LVL_-1`), non quella del legacy zone legata al bedroom.
- Il grace dopo l’apertura porte è coerente con la cabina fisica (tempo fisso, non “deep UV”).
- Il benchmark **-1** è formalmente chiuso come modello Elevator 4.0; il rollout agli altri piani può partire da STEP 6.
- Nessun debounce auto-partenza residuo in scena: il viaggio resta solo su conferma **E**.

---

## 1. Disinnesco `ElevatorCabinZone` su -1

### Problema

Su -1 coesistevano **due** meccanismi di “dentro cabina”: `ElevatorCabinInteriorZone` (fisico) e `ELEV_CabinZone_LVL_-1` (UV legacy). Il legacy zone puntava al walk area sbagliato (`walkArea` bedroom) e poteva riattivare logiche UV in parallelo.

### Soluzione

- In scena: `ELEV_CabinZone_LVL_-1` — MonoBehaviour e `BoxCollider2D` **disabilitati**; `walkArea` allineato a `ElevatorFrontWalkArea_LVL_-1` per coerenza authoring.
- In codice: `RegisterInteriorZone` chiama `DisarmLegacyCabinZoneAtFloor(floorIndex)` — disabilita collider e componente del `ElevatorCabinZone` sullo stesso `floorIndex`.
- Commento XML su `ElevatorCabinZone`: marcato **legacy 3.x**, attivo solo su piani senza interior zone.

**File interessati:**  
`ElevatorSystem.cs`, `ElevatorCabinZone.cs`, `SCN_VaultMap.unity`

---

## 2. Ingresso/uscita cabina solo fisico (benchmark -1)

### Problema

`HandlePhysicalInteriorEnter` e `HandlePhysicalInteriorExit` applicavano ancora gate UV (`IsPlayerDeepEnoughOnLobbyWalkArea`) su enter/exit, duplicando il trigger e rischiando disallineamento con i confini `BLK_CabinSide_*`.

### Soluzione

- **Enter:** su piani con `HasPhysicalInteriorZone`, il trigger fisico + `GetCabinActivationBlockReason` (porte, grace, call) decidono l’attivazione cabina — niente check UV deep in ingresso.
- **Exit:** uscita dalla interior zone segue solo il trigger (niente “ignora exit se ancora deep UV”).
- **Grace:** se `HasPhysicalInteriorZone(floorIndex)`, durante `minDoorsOpenBeforeCabinEntrySeconds` dopo apertura porte si restituisce sempre `doors_open_grace` (senza eccezione “già deep UV”).
- Piani **senza** interior zone: comportamento legacy invariato (`cabinLobbyDeepV`, `DeferredCabinZoneCheck`, `ElevatorCabinZone`).

**File interessati:**  
`ElevatorSystem.cs`

---

## 3. Walk area e landing cabina

### Problema

`ResolveFloorLobbyWalkArea` risolveva prima il walk area del legacy `ElevatorCabinZone`, potendo usare l’area bedroom invece del front walk ascensore su -1.

### Soluzione

- Ordine risoluzione: `floorLobbyWalkAreas[floorIndex]` → legacy zone walk area → fallback Y livello / `FindWalkAreaForWorldPoint`.
- `GetCabinInteriorLandingPosition`: preferisce `ElevatorCabinInteriorZone.LandingPoint`, poi centro collider interior, poi fallback legacy zone.
- `ResolveCabinInteriorLanding` / arrivo viaggio usano `ResolveFloorLobbyWalkArea` aggiornato.
- `ElevatorFrontWalkArea_LVL_-1`: `limitLateralUWhenDeep: 0` (nessun clamp UV laterale sul mover).

**File interessati:**  
`ElevatorSystem.cs`, `SCN_VaultMap.unity`

---

## 4. Cleanup debounce e scena

### Problema

`selectionDebounceSeconds` restava serializzato in scena (residuo Elevator 3.x / auto-partenza), già rimosso dal codice in STEP 4.

### Soluzione

- Rimosso `selectionDebounceSeconds: 1.2` dal componente `ElevatorSystem` in `SCN_VaultMap.unity`.
- Conferma viaggio resta esclusivamente su **E** + `TryDepartToTarget` (STEP 4).

**File interessati:**  
`SCN_VaultMap.unity`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/World/Elevator/ElevatorSystem.cs` | STEP 5: disarm legacy, enter/exit fisico, grace, walk area, landing, skip deferred UV |
| `Assets/_Project/Scripts/World/Elevator/ElevatorCabinZone.cs` | Documentazione legacy 3.x |
| `Assets/_Project/Scenes/SCN_VaultMap.unity` | `ELEV_CabinZone_LVL_-1` off, walkArea front -1, rimosso `selectionDebounceSeconds` |
| `Assets/Docs/REPORT/DEV_REPORT_0116_ELEVATOR_4_STEP5_CHIUSURA_MENO1_2026-06-08.md` | **Nuovo** — questo report |

---

## Regole / vincoli rispettati

- **Elevator 4.0 incrementale:** chiusura STEP 5 **solo -1**; legacy UV resta per +1/0/-2 fino a STEP 6.
- **Nessun clamp UV laterale** reintrodotto sul mover (regressione bed/cucina documentata in 0115).
- **Architettura runtime:** nessun nuovo `FindObjectOfType`; binding scena + registrazione zone esistenti.
- **Both (demo + full):** unica scena `SCN_VaultMap`.

---

## Note operative (Unity)

1. Play su `SCN_VaultMap`, piano **-1**: chiamata → attendere grace ~0.45 s → entrare in cabina (trigger fisico) → porte chiuse → W/S → **E** → viaggio → uscita.
2. Verificare che **non** compaiano log/contatti da `ELEV_CabinZone_LVL_-1` (componente disabilitato).
3. Regressione **bedroom/cucina ↔ ascensore**: niente snap; `BLK_CabinSide_*` solo a porte aperte.
4. **Prossimo step:** STEP 6 — rollout interior zone + front walk + BLK laterali su **0 → +1 → -2**, un piano alla volta.

---

*Fine DEV REPORT 0116.*
