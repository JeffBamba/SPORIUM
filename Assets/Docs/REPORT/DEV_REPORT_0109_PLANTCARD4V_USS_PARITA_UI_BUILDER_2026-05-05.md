# DEV REPORT 0109 — PlantCard4v: layout e misure unificate tra USS e UI Builder

**Data:** 2026-05-05  
**Sprint / contesto:** UI Toolkit PlantCard4v — eliminazione degli stili inline su UXML per far coincidere authoring (UI Builder / USS) e rendering in Play.  
**Riferimento piano:** `.cursor/plans/demo_alpha_1_0_gap_map.plan.md` (Principio prodotto unico; polish HUD/UI “Both”)  
**Report precedente:** `DEV_REPORT_0108_PLANTCARD4V_VO_BIOLOGO_REAZIONI_PH_AMBIENT_2026-05-05.md`

---

## Sommario interventi

1. Rimossi tutti gli attributi **`style="..."`** da `PlantCard4v.uxml`, che non seguono il flusso “modifica classe nel foglio di stile” e creano divergenza tra canvas Builder e gioco.
2. Spostati in **`PlantCard4v.uss`** gli stessi valori (min-height righe bisogni, offset summary, card stack, pannello VO, card tecnica, bordi azioni, titolo VO nascosto) usando classi esistenti, integrazioni su `.pcv4-stack-card` / `.pcv4-condition-card` / `.pcv4-vo-panel` / `.pcv4-actions-wrap` e selettori **`#pcv4-need-row-*`** dove serve un nodo unico.
3. Allineato **`min-height`** delle righe meter (idratazione / fertilizzante) a **104px** nel USS, coerente con quanto già imposto in game tramite inline.

---

## Statistiche e progresso

### Righe di codice

- **Intervento:** solo **UXML + USS** (nessun `.cs` in questa iterazione).
- **Comando:** PowerShell `(Get-Content <path>).Count`.
- **`PlantCard4v.uxml`** — **222** righe (file intero al momento della misura).
- **`PlantCard4v.uss`** — **1132** righe (file intero al momento della misura).
- **Delta +/- isolato:** non quantificato riga per riga (refactor puramente dichiarativo).

### Sistemi funzionanti

- **PlantCard4v (layout visivo):** da validare in **Editor** confrontando **UI Builder** e **Play Mode** sulla stessa risoluzione / stesse **Panel Settings** dell’`UIDocument` in scena.

### Bug risolti

- **0** — nessun bug gameplay numerato; rimossa una **causa strutturale** di disallineamento tra editing USS e risultato in game (stili solo inline su UXML).

### Progresso gameplay / prodotto

- Chi cura il layout vede in **UI Builder** le **stesse** regole di misura e posizione che il giocatore vede in **Play**, salvo scaling del documento UI.
- Meno rischio di “tweak in USS che non si vede” sulle righe bisogni, VO, condizione e pannello interventi.
- Il flusso di authoring è allineato alla regola di progetto **parità UI Builder ↔ runtime** (`.cursor/rules/ui-hud-foundation-ui-builder-parity.mdc`).

---

## 1. Inline UXML vs foglio di stile

### Problema

- Attributi **`style` su nodi UXML** definiscono layout solo su quell’istanza: modifiche fatte dal pannello **StyleSheet** sulle classi non aggiornano quei valori, e l’anteprima Builder può non riflettere il modello mentale “tutto nell’USS”.

### Soluzione

- **Zero** `style=` residui su `PlantCard4v.uxml`.
- Valori migrati in regole USS riutilizzabili e tracciabili (classi + `#pcv4-need-row-summary`, `#pcv4-need-row-ph`, `#pcv4-need-row-cond`).

**File interessati:**  
`Assets/_Project/UI/UIToolkit/PlantCard4v/PlantCard4v.uxml`, `Assets/_Project/UI/UIToolkit/PlantCard4v/PlantCard4v.uss`

---

## 2. Valori di layout consolidati

### Problema

- Alcune regole USS (es. `min-height` su `.pcv4-stack-card` o `.pcv4-need-item--meter`) erano **inferiori** rispetto agli inline attivi in game, generando incertezza sulla fonte di verità.

### Soluzione

- `.pcv4-stack-card` → `min-height: 477px`.
- `.pcv4-need-item--meter` → `min-height: 104px`.
- `#pcv4-need-row-summary` → `position: relative; top: -11px`.
- `#pcv4-need-row-ph` → `min-height: 90px`; `#pcv4-need-row-cond` → `min-height: 80px`.
- `.pcv4-condition-card` → `position: relative; bottom: -32px`.
- `.pcv4-vo-panel` → `position: relative; bottom: -40px; margin-bottom: 7px`.
- `.pcv4-actions-wrap` → bordi a **0** (override del bordo panel).
- `.pcv4-vo-title` → `display: none` (etichetta interna non mostrata in game).

**File interessati:**  
`Assets/_Project/UI/UIToolkit/PlantCard4v/PlantCard4v.uss`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/UI/UIToolkit/PlantCard4v/PlantCard4v.uxml` | Rimozione attributi `style` (layout solo via classi/nomi) |
| `Assets/_Project/UI/UIToolkit/PlantCard4v/PlantCard4v.uss` | Regole layout parità Builder/game; selettori `#pcv4-need-row-*` |

---

## Regole / vincoli rispettati

- **Parità UI Builder ↔ game:** stili di marca e geometria dichiarativa in **USS**, non duplicati come albero UI parallelo.
- Nessuna modifica a runtime architecture (`FindObjectOfType`, `ServiceContainer`) in questa iterazione.

---

## Note operative (Unity)

- Verificare **Play Mode** vs **UI Builder** con le stesse **Panel Settings** (reference resolution, scale mode) dell’`UIDocument` che mostra PlantCard4v in `SCN_VaultMap` (o scena di test).
- Se persiste una piccola scala diversa, intervenire su **UIDocument** / canvas, non riducendo di nuovo stili inline su UXML.

---

*Fine DEV REPORT 0109.*
