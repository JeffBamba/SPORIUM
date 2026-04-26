---
name: sviluppa
description: >-
  Runs the Sporae "SVILUPPA" workflow: reconnaissance, last five DEV REPORTs,
  repo constraints, phased plan, patches, verification, manual steps, system check.
  Use when the user starts a prompt with SVILUPPA:, asks for sviluppa mode, or
  wants the full development procedure before coding.
---

# Modalità SVILUPPA — workflow agente

## Trigger

Il prompt inizia con **`SVILUPPA: <titolo>`** (eventuali `#tag`). La regola sintetica è in `.cursor/rules/sviluppa.mdc`; questa skill espande i passi e le checklist.

## Obiettivo

Ricognizione completa prima del codice: riuso, niente duplicati, vincoli del repo rispettati, output strutturato.

---

## Fase 0 — Vincoli Cursor / repo (subito dopo contesto)

1. Leggi **`AGENTS.md`** in root.
2. Allinea a regole/skill per dominio (vedi regola `sviluppa.mdc` sezione **2bis**): UI Toolkit, DEV REPORT, gameplay, piani `.cursor/plans/`.

---

## Fase 1 — Ultimi 5 DEV REPORT (obbligatorio in ogni SVILUPPA)

1. Percorso: `Assets/Docs/REPORT/DEV_REPORT_*.md`.
2. Ordina i file per **`NNNN`** nel pattern `DEV_REPORT_NNNN_*.md` (numerico decrescente = dal più recente).
3. **Leggi almeno 5 file** dalla cima dell’elenco (se ce ne sono meno di 5, leggi tutti quelli presenti).
4. In **`# ANALISI`**: bullet sintetici su interventi recenti **rilevanti** per il task (file/aree già toccate, decisioni, regressioni evitate). Se nulla è pertinente, dichiaralo esplicitamente.

> Nota: per **scrivere** un nuovo DEV REPORT usa `.cursor/skills/dev-report-authoring/SKILL.md` + `.cursor/rules/dev-report.mdc` — non confondere con questa lettura di contesto.

---

## Fasi 1–11 (dettaglio in regola)

Esegui nell’ordine le fasi **1–11** descritte in `.cursor/rules/sviluppa.mdc` (include **2bis** e il punto **DEV REPORT** nello step 3):

1. Contesto & obiettivo  
2. Inventario progetto  
2bis. Vincoli repo (AGENTS, regole, piani)  
3. Ricognizione (DEV REPORT + GitHub + Notion + overlap)  
4. Rischi, vincoli, dipendenze  
5. Strategia (riuso > estendi > refactor > riscrivi)  
6. Piano tecnico  
7. Implementazione (solo dopo 1–6)  
8. Verifica  
9. Deliverables  
10. Step manuali  
11. Check di sistema  

## Formato di uscita obbligatorio

```text
# ANALISI
# DECISIONI
# PIANO
# PATCH
# VERIFICHE
# DELIVERABLES
# STEP MANUALI
# CHECK DI SISTEMA
```

## Qualità (promemoria)

- Contratti pubblici stabili o breaking versionati.
- Linter/CI; test happy path + edge; PR piccole.
- Se mancano GitHub/Notion: analisi locale + assunti + TODO.
- Implementazione simile esistente → estendi, non riscrivere.

## Esempio di invocazione

`SVILUPPA: Sistema salvataggi partite offline #gameplay #persistence`
