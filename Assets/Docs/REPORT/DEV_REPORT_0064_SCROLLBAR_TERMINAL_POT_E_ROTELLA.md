# DEV REPORT 0064 — Scrollbar Terminal Pot (stile verde, rotella mouse)

**Data:** 2026-03-06  
**Oggetto:** Stile scrollbar del Terminal Pot (PlantCardV3): thumb verde lime stondato, rimozione linea gialla, scrollbar più stretta, frecce solo verde senza background; abilitazione scroll con rotella del mouse sulla console.  
**Riferimenti:** `PlantCardV3_Terminal.uss`, `PlantCardV3TerminalController.cs`, Unity UI Toolkit ScrollView/Scroller.  
**Report precedente:** `Assets/Docs/REPORT/DEV_REPORT_0063_UI_GAP_E_RIMOZIONE_LOG.md`

---

## 1. Contesto

- Il pannello **Terminal Pot** (SPORIUM INCUBATOR CONTROL TERMINAL v3.1) utilizza una ScrollView nativa Unity per l’area testo della console. La scrollbar predefinita appariva grigia e non rispettava lo stile richiesto (verde lime, stondata, senza linea interna, frecce solo verdi). Inoltre non era possibile scrollare con la rotella del mouse.
- Sono state applicate modifiche USS e C# per forzare lo stile (anche in presenza del tema predefinito Unity) e per gestire esplicitamente il `WheelEvent` sulla console.

---

## 2. Lavoro svolto

### 2.1 Stile scrollbar (thumb verde, stondato, senza linea gialla)

- **USS (`PlantCardV3_Terminal.uss`):**
  - **Vertical scroller:** `width: 8px !important` (circa 30% in meno rispetto a 12px), `border-radius: 8px` su tutto il contenitore.
  - **Rimozione linea gialla:** `.unity-base-slider__track` con `display: none !important`; `.unity-scroller__slider` e `.unity-base-slider__drag-container` con background trasparente e `border-width: 0`.
  - **Thumb / dragger:** `.unity-scroller__thumb` e `.unity-base-slider__dragger` con `background-color: rgb(127, 255, 122) !important`, `background-image: none`, `border-width: 0`, `border-radius: 8px !important`.
  - **Frecce su/giù:** `.unity-scroller__high-button` e `.unity-scroller__low-button` con `background-color: rgba(0,0,0,0) !important`, `border-width: 0`, `-unity-background-image-tint-color: rgb(127, 255, 122)`. **Non** è stato impostato `background-image: none` per non far scomparire l’icona della freccia.

- **C# (`PlantCardV3TerminalController.cs` — `ApplyConsoleScrollbarStyle()`):**
  - Larghezza forzata: `vScroller.style.width = 8`.
  - Thumb: colore verde lime, `border-radius: 8`, bordi a 0; ricerca del thumb tra `unity-scroller__thumb`, `unity-base-slider__tracker`, `unity-base-slider__dragger`; se il thumb è il dragger, il tracker (barra fissa) viene nascosto con `display: DisplayStyle.None` per eliminare la linea gialla.
  - Track: `unity-base-slider__track` con `display: DisplayStyle.None`.
  - Frecce: `backgroundColor = Color.clear`, `unityBackgroundImageTintColor = green`; **non** si imposta `backgroundImage = null` per mantenere visibile la freccia.

### 2.2 Scroll con rotella del mouse

- **Metodo `RegisterConsoleMouseWheelScroll()`:** registra un callback su `WheelEvent` per:
  - `_consoleScroll` (ScrollView della console);
  - `_consoleView` (area console), così lo scroll funziona anche con il puntatore sopra il testo.
- Logica: `verticalScroller.value` viene aggiornato con `value + evt.delta.y * 24f`, con clamp tra `lowValue` e `highValue`; in caso di scroll effettivo viene chiamato `evt.StopPropagation()`.
- Chiamata da `BindUI()` subito dopo `ApplyConsoleScrollbarStyle()`.

---

## 3. File modificati

| File | Modifica |
|------|----------|
| `Assets/_Project/UI/UIToolkit/PlantCardV3/PlantCardV3_Terminal.uss` | Scrollbar console: width 8px, track display:none, thumb/dragger verde lime radius 8 border 0, frecce background trasparente + tint verde (senza background-image: none), vertical scroller radius 8px. |
| `Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs` | `ApplyConsoleScrollbarStyle()`: width 8, thumb verde stondato, track e (se applicabile) tracker nascosti, frecce solo tint verde; nuovo `RegisterConsoleMouseWheelScroll()` con `WheelEvent` su console scroll e console view; chiamata a `RegisterConsoleMouseWheelScroll()` in `BindUI()`. |

---

## 4. Verifica

- Nessun errore di lint sui file modificati.
- In Play, apertura del Terminal Pot: scrollbar verde lime, thumb stondato, nessuna linea gialla visibile, frecce verdi senza rettangolo di background; scrollbar più stretta (8px).
- Rotella del mouse sopra l’area console o la scrollbar: contenuto scrolla in su/giù correttamente.

---

## 5. Note per QA

- **Terminal Pot:** Aprire il terminale (PlantCardV3), verificare che la scrollbar a destra sia verde lime, con estremità stondate e senza linea gialla; che le frecce su/giù siano solo icone verdi (nessuno sfondo grigio); che la barra sia sottile (8px).
- **Rotella:** Con il terminale aperto, posizionare il cursore sull’area testo della console e usare la rotella: il testo deve scrollare. Verificare anche scroll con click/drag sulla scrollbar.

---

## 6. Riferimenti

- Stile scrollbar Unity: classi USS `unity-scroll-view__vertical-scroller`, `unity-scroller__thumb`, `unity-base-slider__track`, `unity-base-slider__tracker`, `unity-base-slider__dragger`, `unity-scroller__high-button`, `unity-scroller__low-button`.
- Scroll con rotella: `WheelEvent`, `ScrollView.verticalScroller.value`.

---

*Fine DEV REPORT 0064.*
