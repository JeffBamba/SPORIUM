# Sporae — note per agenti (Cursor / AI)

## Modalità SVILUPPA (`SVILUPPA: …`)

- Regola (trigger + passi sintetici): `.cursor/rules/sviluppa.mdc`
- Skill (workflow esteso, ultimi 5 DEV REPORT, checklist): `.cursor/skills/sviluppa/SKILL.md`

## UI Toolkit e parità UI Builder ↔ game

Per **qualsiasi** pannello o HUD fatto con **Unity UI Toolkit** (UXML/USS, `UIDocument`, modali, inventario, tooltip):

1. Leggere e applicare la regola di progetto:  
   `.cursor/rules/ui-hud-foundation-ui-builder-parity.mdc`
2. Leggere e applicare la skill dedicata (workflow e checklist):  
   `.cursor/skills/ui-toolkit-builder-parity/SKILL.md`

Obiettivo: **1:1** tra ciò che si vede in **UI Builder** e in **Play**; niente alberi UI paralleli hardcoded; eccezioni solo per liste/tooltip dinamici come da regola (`*-builder-reference`, stesse classi USS).

## Analisi tecnica (codebase / architettura)

- Regola evidenze fresche (sempre attiva): `.cursor/rules/analysis-no-suppositions-fresh-scan.mdc`
- Skill (template, **ultimi 5 DEV REPORT**, **statistiche e progresso gameplay** (blocco obbligatorio), confronto con **analisi tecnica precedente**, **performance P0–P2**, **Demo/Full vs GDD Notion** con fonte dichiarata; niente guessing): `.cursor/skills/analisi-tecnica-sporae/SKILL.md`  
  Riferimento struttura esempio: `ANALISI_TECNICA_COMPLETA_SPORIUM.md` (root repo — **non** fonte di numeri senza ricalcolo)

## Altro

- Report di sviluppo: `.cursor/rules/dev-report.mdc` e skill `.cursor/skills/dev-report-authoring/SKILL.md` (sezione fissa **`## Statistiche e progresso`**: righe codice, sistemi verificati, bug risolti, progresso gameplay; **N/D** se manca un dato)
- Architettura runtime: `.cursor/rules/architecture-runtime-services.mdc`
- **Recap commit / messaggio merge (sintesi concettuale, non elenco file):** `.cursor/rules/commit-recap-conceptual.mdc`
