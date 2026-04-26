---
name: ui-toolkit-builder-parity
description: >-
  Enforces 1:1 parity between Unity UI Toolkit panels and UI Builder for Sporae:
  every in-game panel must be authored in UXML/USS so the designer sees what the
  player sees; no parallel hardcoded UI trees. Use when adding or changing
  UIToolkit panels, UIDocument, UXML, USS, HUD, modal inventory, tooltips, or when
  the user mentions UI Builder, visual parity, or "no hardcoded UI".
---

# UI Toolkit — parità UI Builder ↔ runtime (progetto Sporae)

## Regola di progetto (fonte di verità)

La policy completa e i dettagli di propagazione (classi USS vs `style=""` vs `text=""`) stanno in:

`.cursor/rules/ui-hud-foundation-ui-builder-parity.mdc`

**Principio non negoziabile:** in UI Builder l’autore vede ciò che il giocatore vede a runtime — stessa gerarchia, stesse classi USS, stessi vincoli di layout. Se una modifica in Builder non si riflette in game (salvo dati dinamici), è un bug dell’implementazione.

## Cosa deve fare l’agente su ogni pannello UIToolkit

1. **Struttura in UXML** — Pannello, header, footer, liste *contenitori*, righe *se previste a design*, stati visivi (vuoto/caricato/disabled): tutto ciò che ha geometria o marca visiva vive in **UXML + USS**, non costruito ad hoc in C# come albero parallelo.
2. **Niente “secondo HUD” in codice** — Vietato duplicare lo stesso pannello solo per Builder (`*-sample` / `*-preview` separato dal runtime). L’elemento che il gioco usa deve essere quello editabile (come `dome-pot-card-0`… nel Dome HUD).
3. **`name` stabili** — Ogni nodo che il controller aggiorna deve avere `name` (o classi stabili) documentati; il codice fa `Q<>()` su quelli, non inventa gerarchie implicite.
4. **C# = dati e stato, non layout di marca** — Limitare `element.style` a visibilità, sprite, colori **dato-dipendenti**. Margini, font, bordi “di prodotto” restano in **USS** così il Builder li tweakka.
5. **Liste / tooltip 100% dinamici** — Se le righe non possono essere istanze statiche N nel UXML, è ammesso un blocco `*-builder-reference` (visibile in Builder, nascosto in Play con flag serializzato) con **placeholder che usano le stesse classi** delle righe generate a runtime. Il codice che fa `Clear()` sostituisce i placeholder mantenendo le classi. Riferimento: regola progetto sezione 5 e Dome HUD.

## Anti-pattern (vietati)

- Costruire righe/card intere solo con `new VisualElement()` / `new Label()` senza equivalente UXML+classi per l’autore (salvo eccezione builder-reference sopra).
- Stili **inline** sul campione o sul ramo “unico” che le istanze runtime non ereditano (rompono la parità).
- Testo placeholder nel UXML diverso dalle istanze runtime che mostrano lo stesso blocco — vanno **sincronizzati** quando cambia il copy del campione.

## Checklist rapida prima di chiudere una PR / un task UI

- [ ] Il file `.uxml` contiene tutto ciò che il designer deve ritoccare visivamente?
- [ ] Le classi USS coprono tutto ciò che oggi è “magic number” in C# per layout/spacing?
- [ ] Il controller non introduce gerarchie UI non rappresentate nel UXML (salvo lista dinamica coperta da builder-reference + classi identiche)?
- [ ] Per dati dinamici (testo, sprite, colore condizione): override documentato nel controller come non editabile da Builder se necessario?

## Riferimenti nel repo

- Esempio parità card + campione: `DomeStatusHUD.uxml`, `DomeStatusHUDController.cs`
- Esempio shell UXML + lista popolata: `PlayerInventoryPanel.uxml` + `PlayerInventoryPanelController.cs` (valutare builder-reference se serve parità riga-per-riga in Builder)
