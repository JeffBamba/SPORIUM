# DEV REPORT 0056 — Food Room Panel: allineamento UI alla reference

**Data:** 2026-02-19  
**Scope:** Pannello Food Room (FoodSynthMachine) — layout, testi e colori allineati alla reference “Biological Food Synthesis”.  
**Riferimenti:** reference UI (screenshot), `FoodRoomPanel.uxml`, `FoodRoomPanel.uss`, `FoodRoomPanelController.cs`

---

## 1. Obiettivo

Portare il pannello della Food Room (sintesi biologica cibo) a corrispondere alla reference fornita: stessa disposizione delle colonne, sezioni, testi e palette (verde chiaro per titoli/bordi/costi, grigio per descrizioni, giallo-arancio per il pannello di warning).

---

## 2. Modifiche layout (UXML)

### 2.1 Colonne

- **Sinistra:** solo **A STEM CELL** (icona ⚗) e **GROWTH CHAMBERS** (icona ⚡). La sezione **HYDRATION LINE** è stata spostata nella colonna destra.
- **Centro:** cerchio **GROWTH TANKS IDLE** + messaggio breve + nuovo pannello **LIFE SUPPORT INDICATORS** con griglia a 4 voci (ELECTRICITY, CORE TEMP, RESERVOIR, NUTRIENT). Rimossa la vecchia sezione `center-metrics` (NUTRIENT FLOW, CELL STABILITY, PROTEIN YIELD).
- **Destra:** ordine: **HYDRATION LINE** (in alto) → **A SYSTEM COMMENT** (warning) → **PRODUCTION INFO**. Rimossa la sezione `system-params` (i dati sono confluiti in LIFE SUPPORT INDICATORS al centro).

### 2.2 LIFE SUPPORT INDICATORS

- Titolo con icona ⚡, stesso stile delle altre sezioni.
- Griglia 2×2: label + valore per ELECTRICITY (87.1%), CORE TEMP (42.8°C), RESERVOIR (64%), NUTRIENT (2.4 L/min). Nomi elementi: `ind-electricity-val`, `ind-coretemp-val`, `ind-reservoir-val`, `ind-nutrient-val`.

### 2.3 Footer (bottom bar)

- **Sinistra:** hint “Select a growth chamber to begin cultivation” (grigio).
- **Centro:** pulsante **ADVANCE DAY (DEBUG)** con icona ▶▶, in un container `bottom-bar-center`.
- **Destra:** pulsanti azione START GROWTH (▶), PURIFY (💧), HARVEST (✓), ABORT (✕).
- Rimosso il pulsante “?” (help).

### 2.4 Pulsanti azione

- Testo pulsanti aggiornato con icone: “▶ START GROWTH”, “💧 PURIFY”, “✓ HARVEST”, “✕ ABORT”.
- `btn-advance-day` spostato da dentro `production-info` al footer centrale (stesso `name` per il binding nel controller).

---

## 3. Testi (copy)

| Elemento | Prima | Dopo (reference) |
|----------|--------|-------------------|
| Titolo header | A BIOLOGICAL FOOD SYNTHESIS | **BIOLOGICAL FOOD SYNTHESIS** |
| Sottotitolo | Una sola riga | Due righe: seconda riga *“Ethical boundaries sold separately.”* |
| Stem cell | STEM CELL (Optional) | **A STEM CELL** |
| Icona stem cell | ◇ | ⚗ (flask) |
| Chambers icon | ◈ | ⚡ |
| Fungal desc | “Grows in the dark…” | *“Grown in the dark. Like your conscience.”* |
| Tank message (idle) | Testo lungo “No active biomass… Tanks maintained…” | *“Select a synthesis protocol to begin growth cycle.”* |

---

## 4. Colori e stili (USS)

### 4.1 Verde chiaro (`--sp-color-green-led`)

Usato per: titolo header e icona header, bordo e icona slot stem cell, titoli sezioni (A STEM CELL, GROWTH CHAMBERS, LIFE SUPPORT INDICATORS, HYDRATION LINE), nomi camere (VEGETAL/FUNGAL/MEAT SYNTHESIS), costi CRY, bordo cerchio tank e testo “GROWTH TANKS IDLE”, label e bordo hydration, titolo e valori LIFE SUPPORT, bordo e righe PRODUCTION INFO.

### 4.2 Slot stem cell

- Bordo e icona placeholder: da viola (`--sp-color-violet-growth`) a verde (`--sp-color-green-led`).
- Titolo sezione: classe `green` invece di `purple`.

### 4.3 Camere (chamber cards)

- Nomi e costi: tutti in verde chiaro (rimossi i colori distinti vegetal=verde, fungal=viola, meat=rosso).
- Dettagli e descrizioni restano grigi (`--sp-color-text-dim` / `--sp-color-text-muted`).

### 4.4 A SYSTEM COMMENT (warning)

- Bordo e icona ⚠: giallo-arancio `rgb(255, 180, 80)` invece di `--sp-color-yellow-standard`.
- Bordo `var(--sp-border-2)` per evidenziare il warning.

### 4.5 Nuove classi USS

- **life-support-indicators:** contenitore con bordo verde, padding, background scuro.
- **life-support-grid:** griglia flex wrap per label/valori.
- **life-support-label / life-support-value:** font 10, verde; valore allineato a destra (50% larghezza).
- **bottom-bar-center:** contenitore flex centrato per ADVANCE DAY (DEBUG).
- **bottom-bar:** layout esplicito (hint sinistra, centro advance day, pulsanti destra); `bottom-buttons` con `justify-content: flex-end`.
- Rimosse regole per `.param-row`, `.param-warning`, `.btn-help`, `.center-metrics`, `.metric-row`. `.system-params` rimosso (sezione non più presente).

---

## 5. Controller (FoodRoomPanelController.cs)

### 5.1 Binding Life Support

- Sostituiti i riferimenti a `metric-nutrient`, `metric-stability`, `metric-yield` (elementi UXML rimossi) con i label dei valori LIFE SUPPORT:
  - `_indElectricity` → `ind-electricity-val`
  - `_indCoreTemp` → `ind-coretemp-val`
  - `_indReservoir` → `ind-reservoir-val`
  - `_indNutrient` → `ind-nutrient-val`
- In `Refresh()`: aggiornamento dei quattro valori (placeholder 87.1%, 42.8°C, 64%, 2.4 L/min). Niente più aggiornamento di CELL STABILITY / PROTEIN YIELD.

### 5.2 Messaggio tank idle

- In stato idle il messaggio viene impostato a *“Select a synthesis protocol to begin growth cycle.”* (allineato alla reference e all’UXML).

### 5.3 ADVANCE DAY e help

- `btn-advance-day` è cercato per nome nella root; resta valido dopo lo spostamento nel footer (dentro `bottom-bar-center`).
- Nessun riferimento a `btn-help` nel controller; rimozione del pulsante dall’UXML non richiede modifiche al codice.

---

## 6. File modificati

| File | Modifiche |
|------|-----------|
| `Assets/_Project/UI/UIToolkit/FoodRoom/FoodRoomPanel.uxml` | Riorganizzazione colonne (hydration a destra, life-support al centro); nuovo blocco LIFE SUPPORT INDICATORS; footer a 3 zone con ADVANCE DAY al centro; testi e icone come da reference; rimozione system-params e btn-help. |
| `Assets/_Project/UI/UIToolkit/FoodRoom/FoodRoomPanel.uss` | Colori verde per stem cell, camere, tank, hydration, life-support, production-info; warning giallo-arancio; stili life-support-grid/label/value; layout footer (bottom-bar-center, bottom-buttons); rimozione stili obsoleti. |
| `Assets/_Project/Scripts/UI/UIToolkit/FoodRoom/FoodRoomPanelController.cs` | Binding a ind-electricity-val, ind-coretemp-val, ind-reservoir-val, ind-nutrient-val; rimossi metric-nutrient/stability/yield; messaggio tank idle breve; valori LIFE SUPPORT impostati in Refresh(). |

---

## 7. Note per QA

- Aprendo il pannello Food Room dalla macchina di sintesi cibo verificare: layout a 3 colonne (stem cell + chambers a sinistra; tank + LIFE SUPPORT al centro; hydration, warning, production info a destra).
- Verificare colori: titoli e bordi principali verdi, pannello “A SYSTEM COMMENT” con bordo giallo-arancio, testi secondari grigi.
- Footer: hint a sinistra, “ADVANCE DAY (DEBUG)” al centro, quattro pulsanti a destra con icone.
- Funzionalità esistenti (selezione camera, start growth, purify, harvest, abort, advance day debug, stem cell slot) devono restare operative.

---

## 8. Riepilogo

- **Layout:** HYDRATION spostata a destra; al centro introdotto LIFE SUPPORT INDICATORS (4 metriche); footer con ADVANCE DAY al centro e pulsanti a destra.
- **Copy:** Titolo “BIOLOGICAL FOOD SYNTHESIS”, “A STEM CELL”, “Grown in the dark. Like your conscience.”, messaggio tank idle breve.
- **Colori:** Verde chiaro per titoli, bordi e costi; giallo-arancio per il pannello warning; slot stem cell e nomi camere in verde.
- **Controller:** Aggiornato al nuovo UXML (Life Support indicators, messaggio tank); nessun riferimento a elementi rimossi.
