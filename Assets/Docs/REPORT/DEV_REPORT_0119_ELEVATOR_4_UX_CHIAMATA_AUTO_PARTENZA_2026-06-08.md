# DEV REPORT 0119 — Elevator 4.0: gate chiamata display + auto-partenza cabina

**Data:** 2026-06-08  
**Sprint / contesto:** Elevator 4.0 — polish UX post-stabilizzazione viaggio 0↔-1 (`DEV_REPORT_0118`); chiusura superficie di interazione display durante viaggio/arrivo; conferma cabina con fallback automatico.  
**Riferimento piano:** Elevator 4.0 STEP 4–5 (chiamata display + selezione cabina)  
**Report precedente:** `DEV_REPORT_0118_ELEVATOR_4_PIANO_0_VIAGGIO_SEAM_LEZIONI_2026-06-08.md`

---

## Sommario interventi

1. **Gate chiamata display:** prompt «Premi E» e input sui pannelli piano disabilitati fuori da `IdleAtFloor` / fuori cabina / durante viaggio (`CanCallFromFloorDisplay`).
2. **`Interactable.SetInteractionAvailable`:** gate runtime riusabile (prompt + E/click + esclusione da disambiguazione nearest/focus).
3. **`ElevatorFloorDisplay`:** rimosso `SetRepeatInteractionWhileInRange(true)`; sync stato da `ElevatorSystem` in `LateUpdate`.
4. **Auto-partenza cabina:** dopo selezione W/S, partenza automatica dopo **2 s** se il player non preme E (solo piano diverso e sbloccato); timer resettato a ogni cambio piano.
5. Hint bottom bar aggiornato con messaggio di timeout opzionale (Inspector).

---

## Statistiche e progresso

### Righe di codice

- **File `.cs` toccati (3):** **2295 righe** totali — `ElevatorSystem.cs` **1792**, `Interactable.cs` **349**, `ElevatorFloorDisplay.cs` **154** (`Measure-Object -Line`, 2026-06-08).
- **Diff vs HEAD (solo questi 3 file):** **+146 / −34** — `git diff --stat`, 2026-06-08.

### Sistemi funzionanti

- Gate display: nessun «Premi E» durante chiamata/viaggio/arrivo; riabilitato a `IdleAtFloor` fuori cabina — **verificato** (conferma autore).
- Flusso cabina W/S + E invariato; auto-partenza dopo 2 s su piano diverso sbloccato — **verificato** (conferma autore).
- E immediato annulla timer; W/S resetta timer — **implementato**; da rivedere in Play se non già testato esplicitamente.
- Uscita cabina (target = piano corrente) e piano out-of-service: **solo E**, nessuna auto-partenza — per design.
- Viaggio 0↔-1, seam walk, overlap interior (0118) — **non modificati** in questa iterazione; regressione **da smoke test** post-commit.

### Bug risolti

- **1** — prompt «Premi E» attivo sui display durante arrivo/viaggio ascensore, con rischio doppia chiamata e coroutine `CallToFloor` interrotte (`SetRepeatInteractionWhileInRange(true)` su ogni `ElevatorFloorDisplay`).

### Progresso gameplay / prodotto

- Chiamare l’ascensore è un’azione singola: dopo la prima pressione E non compare più l’invito a interagire finché il ciclo non torna idle fuori cabina.
- In cabina, chi ha già scelto il piano può partire subito con E oppure attendere 2 secondi senza input aggiuntivo.
- Meno confusione tra «chiamata esterna» (display) e «conferma interna» (W/S + E / timeout).
- Modello UX pronto per rollout **+1** con stesso `ElevatorSystem` condiviso.

---

## 1. Gate interazione display (`CanCallFromFloorDisplay`)

### Problema

Ogni `ElevatorFloorDisplay` forzava `_repeatInteractionWhileInRange = true` in `Awake()`. Il prompt «Premi E» restava visibile mentre l’ascensore era in `CallingToFloor` / viaggio, e `CallToFloor` poteva essere richiamato (stop/restart coroutine).

### Soluzione

- **`ElevatorSystem.CanCallFromFloorDisplay()`:** `true` solo se `IdleAtFloor`, player fuori cabina, nessun `_callToFloorCoroutine` / `_departCoroutine` / teleport.
- **`CallToFloor`:** early return se gate chiuso.
- **`ElevatorFloorDisplay`:** `LateUpdate` → `_interactable.SetInteractionAvailable(elevator.CanCallFromFloorDisplay())`; rimosso repeat interaction.

**File interessati:**  
`ElevatorSystem.cs`, `ElevatorFloorDisplay.cs`

---

## 2. `Interactable.SetInteractionAvailable`

### Problema

Non esisteva un modo per sopprimere prompt/input su un singolo `Interactable` senza disabilitare il componente (armadio e altri oggetti usano repeat o one-shot standard).

### Soluzione

- Campo `_interactionAvailable` (default `true`), API `SetInteractionAvailable(bool)` / `IsInteractionAvailable`.
- `Update`: niente prompt né E/click se non disponibile.
- `GetNearestInteractableInRangeToPlayer` e `ResolveKeyboardTargetForCurrentState`: ignorano interactable non disponibili (evita che un display disabilitato vinca il focus E).

**File interessati:**  
`Interactable.cs`

---

## 3. Auto-partenza cabina dopo selezione piano

### Problema

Dopo stabilizzazione flusso 4.0, la conferma E in cabina è l’unico attrito residuo; richiesta di fallback automatico senza cambiare il flusso W/S → E.

### Soluzione

- Inspector: `enableCabinAutoDepartAfterSelection` (default **true**), `cabinAutoDepartDelaySeconds` (default **2**), `cabinAutoDepartConfirmHint` opzionale (`{0}` = secondi).
- `AdjustTargetIndex` → `ScheduleCabinAutoDepart()`; timer cancellato su ogni nuovo W/S, `TryDepartToTarget`, `CancelCabinArrowSelection`, `OnDisable`.
- **Non** auto-parte se: `_targetIndex == currentLevelIndex` (uscita cabina), piano non sbloccato, viaggio già in corso.
- Hint: *«Premi E per confermare il piano — partenza automatica tra 2 s»* (o stringa custom).

**File interessati:**  
`ElevatorSystem.cs`

---

## Tabella stati → interazione display

| Stato / condizione | Display [E] |
|--------------------|-------------|
| `IdleAtFloor`, fuori cabina | Sì |
| `CallingToFloor` / `Departing` / `Traveling` | No |
| `DoorsOpenWaitingEntry` (ingresso a piedi) | No |
| `CabinReadyForSelection` / in cabina | No (W/S + E / auto) |
| Dopo uscita → `IdleAtFloor` | Sì |

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/World/Elevator/ElevatorSystem.cs` | `CanCallFromFloorDisplay`, auto-partenza cabina, hint timeout |
| `Assets/_Project/Scripts/World/Elevator/ElevatorFloorDisplay.cs` | Sync `SetInteractionAvailable`, rimosso repeat |
| `Assets/_Project/Scripts/Interactables/Interactable.cs` | `SetInteractionAvailable`, filtri disambiguazione |
| `Assets/Docs/REPORT/DEV_REPORT_0119_ELEVATOR_4_UX_CHIAMATA_AUTO_PARTENZA_2026-06-08.md` | **Nuovo** — questo report |

---

## Regole / vincoli rispettati

- **Flusso 4.0 in cabina invariato:** W/S selezione, E conferma immediata; auto-partenza è opt-in Inspector.
- **Both (demo + full):** un solo `ElevatorSystem` su `SCN_VaultMap`.
- **Nessuna modifica** a seam walk, overlap interior, gate display esterno oltre quanto sopra.
- **Architettura:** nessun nuovo `FindObjectOfType`; gate localizzato a display + API minima su `Interactable`.

---

## Note operative (Unity)

1. **Display:** chiamata piano → durante attesa **no** prompt E → porte aperte → ingresso a piedi **no** E display.
2. **Cabina:** W/S → hint con timeout → E subito **oppure** attesa 2 s → partenza.
3. **Stesso piano / -2 locked:** solo E, no auto.
4. **Inspector:** disabilitare `enableCabinAutoDepartAfterSelection` per tornare al solo E.
5. **Gate -1 / 0:** smoke test viaggio + seam dopo commit.

---

*Fine DEV REPORT 0119.*
