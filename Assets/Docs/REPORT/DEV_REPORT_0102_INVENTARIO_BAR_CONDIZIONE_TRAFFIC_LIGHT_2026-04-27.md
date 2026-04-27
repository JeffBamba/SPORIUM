# DEV REPORT 0102 — Inventario: barra condizione (svuotamento), etichetta e colori soglia

**Data:** 2026-04-27  
**Sprint / contesto:** Affinamento UX del dettaglio item inventario VAULT-07: lettura immediata della **condizione** residua (`Quality` / `MaxQuality`) con barra che si svuota al deterioramento e codifica colore a soglie.  
**Riferimento piano:** `.cursor/plans/demo_alpha_1_0_gap_map.plan.md` (feature Both: inventario condiviso demo/full).  
**Report precedente:** `DEV_REPORT_0101_INVENTARIO_VAULT07_UI_PARITY_TOAST_FIX_2026-04-27.md`

---

## Sommario interventi

1. Invertita la semantica della barra accanto a «Vedi Dettaglio»: da «riempimento deperimento» a **condizione residua** (100% = integro, barra piena; calo = barra che si accorcia).
2. Aggiunta **etichetta localizzata** «Condizione» / «Condition» in header riga con la percentuale.
3. Introdotte **soglie colore** sulla percentuale arrotondata: verde sopra il 50%, giallo al 50%, rosso sotto il 50% (incluso 0%), applicate al fill via `StyleColor` a runtime.
4. Riorganizzato layout UXML (`inv-detail-decay-header` + riga track) e USS (placeholder fill a larghezza piena per authoring; colore fill documentato come override runtime).

---

## Statistiche e progresso

### Righe di codice

- Misurazione con comando (working tree vs `HEAD`, file di questo intervento):
  - `git diff --numstat HEAD -- Assets/_Project/Scripts/UI/UIToolkit/PlayerInventory/PlayerInventoryPanelController.cs Assets/_Project/UI/UIToolkit/PlayerInventory/PlayerInventoryPanel.uxml Assets/_Project/UI/UIToolkit/PlayerInventory/PlayerInventoryPanel.uss Assets/_Project/Scripts/Core/Localization/LocalizationManager.cs`
- Esito **numstat** (aggiunte / rimozioni):
  - `PlayerInventoryPanelController.cs` → **+115 / -23**
  - `PlayerInventoryPanel.uxml` → **+16 / -5**
  - `PlayerInventoryPanel.uss` → **+88 / -14**
  - `LocalizationManager.cs` → **+11 / -1**

### Sistemi funzionanti

- **Verificato da lint:** nessun errore segnalato su `PlayerInventoryPanelController.cs` dopo le modifiche.
- **Da validare in Editor (Play + UI Builder):**
  - selezione item con `MaxQuality` / deterioramento attivo: allineamento testo %, larghezza fill e transizioni colore alle soglie;
  - authoring: etichetta e barra visibili in UI Builder con placeholder coerente (fill USS al 100% finché il controller non sovrascrive a runtime).

### Bug risolti

- **0** — nessuno documentato come issue tracker; intervento di miglioramento semantico/UX su funzionalità già presente.

### Progresso gameplay / prodotto

- Il giocatore legge la **condizione** come «quanto resta», non come «quanto è peggiorato», allineato al linguaggio di conservazione.
- La **dicitura «Condizione»** rende esplicito il significato della barra e della percentuale.
- I **colori a soglia** danno feedback immediato (buono / attenzione / critico) senza aprire tooltip o scheda ispezione.
- Designer e autore UI Builder vedono una **barra piena** di default nel foglio di stile, coerente con item al massimo della qualità configurata.

---

## 1. Semantica barra e logica runtime

### Problema

- La barra era percepita come «crescita del deperimento» (fill che aumenta al peggiorare), mentre il design richiedeva **partenza al 100%** e **svuotamento** al deterioramento.
- Mancava un collegamento visivo chiaro tra percentuale, colore e stato «salute» dell’item.

### Soluzione

- Sostituito il calcolo «deperimento» con **condizione percentuale**: `Clamp01(Quality / MaxQuality) * 100`.
- `UpdateDecayBarUi` imposta `width` del fill in **percentuale di condizione** e il testo accanto alla stessa metrica (arrotondata).
- Aggiunto `GetConditionBarFillColor(int roundedPercent)` con soglie **> 50** verde, **== 50** giallo, **< 50** rosso; assegnazione `_detailDecayFill.style.backgroundColor = new StyleColor(...)`.

**File interessati:**  
`Assets/_Project/Scripts/UI/UIToolkit/PlayerInventory/PlayerInventoryPanelController.cs`

---

## 2. Etichetta, localizzazione e layout UI Toolkit

### Problema

- La percentuale da sola non chiariva se rappresentasse deperimento o integrità; serviva un’etichetta esplicita e traducibile.

### Soluzione

- Nuova chiave `inventory.detail.condition_label` in `LocalizationManager` (IT/EN).
- UXML: riga header con `inv-detail-decay-condition-lbl` + `inv-detail-decay-pct`, riga sotto con solo track/fill; `ApplyStaticChrome` imposta il testo dell’etichetta.
- USS: stili header/label; fill placeholder `width: 100%` per parità authoring; rimosso margine destro superfluo sulla track dopo lo spostamento della percentuale in header.

**File interessati:**  
`Assets/_Project/Scripts/Core/Localization/LocalizationManager.cs`  
`Assets/_Project/UI/UIToolkit/PlayerInventory/PlayerInventoryPanel.uxml`  
`Assets/_Project/UI/UIToolkit/PlayerInventory/PlayerInventoryPanel.uss`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/UI/UIToolkit/PlayerInventory/PlayerInventoryPanelController.cs` | Logica condizione %, colori fill, bind etichetta |
| `Assets/_Project/Scripts/Core/Localization/LocalizationManager.cs` | Chiave `inventory.detail.condition_label` |
| `Assets/_Project/UI/UIToolkit/PlayerInventory/PlayerInventoryPanel.uxml` | Header Condizione + %, struttura decay |
| `Assets/_Project/UI/UIToolkit/PlayerInventory/PlayerInventoryPanel.uss` | Stili header/label; fill e track |

---

## Regole / vincoli rispettati

- **Parità UI Builder ↔ runtime** (`.cursor/rules/ui-hud-foundation-ui-builder-parity.mdc`): struttura e classi USS in UXML; colori dinamici della barra documentati come override dipendente dai dati in codice.
- **Feature Both** (`.cursor/rules/feature-both-demo-full-parity.mdc`): nessun fork demo/full per questa UI.

---

## Note operative (Unity)

- Validare in **Play Mode** item `IsPerishable` / organici / frutta con `MaxQuality > 0` (stesso gating `ShouldShowDecayBar` esistente).
- Verificare transizione **49% → 50% → 51%** (arrotondamento intero) per confermare attesa designer sui confini colore.

---

*Fine DEV REPORT 0102.*
