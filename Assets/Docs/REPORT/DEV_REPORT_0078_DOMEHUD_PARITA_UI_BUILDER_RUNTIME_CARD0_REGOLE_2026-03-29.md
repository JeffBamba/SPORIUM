# DEV REPORT 0078 — DomeStatusHUD: parità UI Builder / runtime, card authoring unica, dati POT, regole Cursor

**Data:** 2026-03-29  
**Sprint / contesto:** Foundation HUD Dome — fix workflow autore + binding dati vaso  
**Riferimento piano:** `domestatushud_redesign_gap_ba2e3e7c.plan.md` (contesto storico)  
**Report precedente:** `DEV_REPORT_0077_COMPACT_BOTTOM_BAR_COLLECTION_ROOM_TOOLTIP_2026-03-28.md`

---

## Sommario interventi

1. **Dati HUD card POT** — idratazione, fertilizzante e LED allineati a `PotStateModel` / config esistenti (nessun servizio duplicato).
2. **Preview pianta** — sprite da `PlantData.VisualSet` (identità specie), non dallo `SpriteRenderer` scena del vaso.
3. **Etichette** — `IDRATAZIONE`, `FERTILIZZANTE`, `LUCE LED` sulle quattro card runtime.
4. **Architettura UI** — eliminato il campione parallelo `dome-pot-card-sample`; **`dome-pot-card-0`** è l’unica superficie di editing visivo coerente con il runtime; `SetupUI` collassa area espansa/condizione all’avvio Play.
5. **Regole Cursor** — aggiornata `ui-hud-foundation-ui-builder-parity.mdc` (vietati binari paralleli); `dev-report.mdc` allineato alla convenzione **`Assets/Docs/REPORT/DEV_REPORT_*.md`**.

---

## 1. Binding statistiche e tooltip

### Problema
- L’idratazione era calcolata come `Hydration * 10` senza usare `MaxHydration` da config → disallineamento rispetto a DayCycle / Plant Card.
- LED e testi tooltip dovevano essere leggibili come **BLUE / RED / OFF** in modo uniforme.

### Soluzione
- **`EnsurePotSystemConfig()`** — caricamento `PotSystemConfig` da `Resources` (stesso approccio di `PotActions`).
- **`GetHydrationPercent(state)`** — delega a **`PlantCardCalculators.CalculateHydrationPercent`**.
- **`FormatLedStatText(LedSystemState)`** — `BLUE` / `RED` / `OFF`; usato sia sulle label stat espansa sia nelle righe tooltip “STATO ATTUALE”.

**File:** `Assets/_Project/Scripts/UI/UIToolkit/DomeStatusHUD/DomeStatusHUDController.cs`

---

## 2. Preview icona card (icona bianca / errata)

### Problema
- Uso di `PotSlot.Sprite` (sprite renderer mondo) → a stadio seme spesso risultato visivamente errato per l’HUD.

### Soluzione
- Preview da **`plantData.VisualSet.adultSprite`** (fallback `floweringSprite`), poi placeholder `Resources` se assente.

**File:** `DomeStatusHUDController.cs` (blocco Refresh su `_potPreviews`).

---

## 3. UXML / USS — etichette e leggibilità stat

### Problema
- Disallineamento tra modifiche in UI Builder (sample) e runtime per **inline style** e **testo** solo sul campione.

### Soluzione
- Testi allineati su tutte le card: `FERTILIZZANTE`, `LUCE LED`.
- Stili “di marca” su **`.dome-pot-stat-label`** in **`DomeStatusHUD.uss`** (font-size, wrap, ecc.) invece che solo inline sul campione.

**File:** `DomeStatusHUD.uxml`, `DomeStatusHUD.uss`

---

## 4. Architettura: niente campione POT duplicato

### Problema
- Presenza di **`dome-pot-card-sample`** separato da **`dome-pot-card-0`…`3`** → l’autore editava un elemento che in Play non era quello mostrato al giocatore (o era nascosto nel blocco builder-reference).

### Soluzione
- **Rimosso** il blocco `dome-hud-builder-card-sample` / `dome-pot-card-sample`.
- **`dome-pot-card-0`** popolata con placeholder realistico (famiglia, nome, condizione, area espansa visibile in UXML per authoring).
- In **`SetupUI`**, per tutti i pot: `dome-pot-expanded-*` e `dome-pot-cond-row-*` impostati a **`display: none`** all’ingresso Play; poi **`RefreshPots`** sovrascrive testi/colori/classi.

**File:** `DomeStatusHUD.uxml`, `DomeStatusHUDController.cs`

**Nota:** il blocco `dome-hud-builder-reference` resta per altri campioni (es. tooltip pH) dove non esiste duplicato runtime statico — coerente con la regola aggiornata.

---

## 5. Regole Cursor

### 5.1 `ui-hud-foundation-ui-builder-parity.mdc`
- Aggiunta sezione **“NESSUN BINARIO PARALLELO”** — vietati campioni `*-sample` che duplicano card runtime; l’elemento runtime editabile in Builder è la superficie unica.

### 5.2 `dev-report.mdc`
- Allineamento esplicito al formato dei file in **`Assets/Docs/REPORT/DEV_REPORT_*.md`** (nessun template parallelo inventato in `.cursor`).

**File:** `.cursor/rules/ui-hud-foundation-ui-builder-parity.mdc`, `.cursor/rules/dev-report.mdc`

---

## File modificati

| File | Tipo modifica |
|---|---|
| `Assets/_Project/Scripts/UI/UIToolkit/DomeStatusHUD/DomeStatusHUDController.cs` | PotSystemConfig, hydration %, LED string, preview VisualSet, SetupUI collapse expanded/cond |
| `Assets/_Project/UI/UIToolkit/DomeStatusHUD/DomeStatusHUD.uxml` | Card 0 authoring; rimozione sample card; label FERTILIZZANTE / LUCE LED |
| `Assets/_Project/UI/UIToolkit/DomeStatusHUD/DomeStatusHUD.uss` | `.dome-pot-stat-label` e affini |
| `.cursor/rules/ui-hud-foundation-ui-builder-parity.mdc` | Regola anti-binario parallelo |
| `.cursor/rules/dev-report.mdc` | Convenzione DEV REPORT → `Assets/Docs/REPORT` |

---

## Regole architetturali rispettate

- `PotActions` / `DayCycleController` non sostituiti come facade; solo consumo dati e config esistenti.
- Nessun `FindObjectOfType` aggiunto per questo lavoro.
- Override `element.style` in controller limitati a dati di gioco (colori condizione, visibilità, sprite preview) — layout “di marca” in USS/UXML.

---

## Note operative (Unity)

- Aprire **`DomeStatusHUD.uxml`** in UI Builder e modificare **classi USS** e struttura su **`dome-pot-card-0`** per vedere le stesse regole sulle altre card (stesse classi).
- Inline solo sul nodo singolo: non propagano alle altre card — preferire StyleSheet.
- Opzionale: se serviva lo **sfondo CRT** che era inline sul vecchio header del sample, va spostato in **USS** su `.dome-pot-header` (non era nel runtime delle card ufficiali).

---

*Fine DEV REPORT 0078.*
