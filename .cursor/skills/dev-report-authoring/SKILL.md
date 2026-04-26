---
name: dev-report-authoring
description: Produce and maintain project DEV REPORT documents in the canonical repository format. Use when the user asks for "DEV REPORT", report di sviluppo, changelog tecnico operativo, or asks to create/update files under Assets/Docs/REPORT/DEV_REPORT_*.md.
---

# Dev Report Authoring

## Purpose

Create DEV REPORT content aligned with this project's canonical format used in `Assets/Docs/REPORT/DEV_REPORT_*.md`.

## Quick Start

When asked to write a DEV REPORT:

1. Inspect one or more recent reports in `Assets/Docs/REPORT`.
2. Reuse the same structure and tone (Italian by default).
3. Choose report number:
   - Use user-provided number if explicitly given.
   - Otherwise use next progressive number from the latest `DEV_REPORT_*.md`.
4. Draft concise but complete sections with concrete technical details.
5. If requested, save to:
   - `Assets/Docs/REPORT/DEV_REPORT_NNNN_<SLUG>_<YYYY-MM-DD>.md`

## Required Structure

Use this order unless the user asks otherwise:

1. `# DEV REPORT NNNN — <titolo descrittivo>`
2. Meta block:
   - `**Data:** YYYY-MM-DD`
   - `**Sprint / contesto:** ...`
   - `**Riferimento piano:** ...` (if available)
   - `**Report precedente:** ...` (if available)
3. `## Sommario interventi` (numbered list, short bullets)
4. `## Statistiche e progresso` — **always present** immediately after the summary (before `## 1. ...`). Four subsections (use **N/D** or “non misurato in questa iterazione” when data is unavailable; **never fabricate counts**):
   - **Righe di codice:** measured lines on touched `.cs` (state scope + command) or N/D.
   - **Sistemi funzionanti:** bullet list of systems/flows **verified** this iteration; otherwise “da validare in Editor”.
   - **Bug risolti:** count + short list or issue refs; `0` / “nessuno documentato” if applicable.
   - **Progresso gameplay / prodotto:** 3–6 plain-language bullets on player-visible or tester-visible progress.
5. Numbered technical sections (`## 1. ...`, `## 2. ...`) with:
   - `### Problema`
   - `### Soluzione`
   - optional `**File interessati:** ...`
6. `## File modificati` table with columns: `Path | Tipo modifica`
7. `## Regole / vincoli rispettati` (only relevant constraints)
8. `## Note operative (Unity)` (tests/checklist if relevant)
9. Closing line: `*Fine DEV REPORT NNNN.*`

Keep section separators (`---`) if present in recent examples.

## Content Rules

- Use Italian unless user asks a different language.
- The **`## Statistiche e progresso`** block is mandatory for every new DEV REPORT (same rules as `.cursor/rules/dev-report.mdc`).
- Be factual and traceable to real changes (no speculative claims).
- Prefer exact identifiers for files, classes, methods, flags, and runtime behavior.
- For each fix, explain user-visible impact and technical implementation.
- Keep summaries concise; put detail in Problem/Solution blocks.

## Report Style Template

```markdown
# DEV REPORT NNNN — <titolo>

**Data:** YYYY-MM-DD  
**Sprint / contesto:** <contesto>  
**Riferimento piano:** `<path piano>`  
**Report precedente:** `<report precedente>`

---

## Sommario interventi

1. <intervento 1>
2. <intervento 2>

---

## Statistiche e progresso

### Righe di codice
- <es. N righe sui file elencati in “File modificati”, comando usato> oppure **N/D**

### Sistemi funzionanti
- <elenco sistemi / flussi verificati in Play o build> oppure **da validare in Editor**

### Bug risolti
- <N> — <elenco breve o riferimento issue> oppure **0 / nessuno documentato**

### Progresso gameplay / prodotto
- <bullet in linguaggio chiaro: cosa nota giocatore o tester>

---

## 1. <Area/fix>

### Problema
- <sintomo>
- <causa>

### Soluzione
- <soluzione tecnica>
- <impatto>

**File interessati:**  
`<file1>`, `<file2>`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `<path>` | `<modifica>` |

---

## Regole / vincoli rispettati

- <vincolo 1>

---

## Note operative (Unity)

- <test/validazione 1>

---

*Fine DEV REPORT NNNN.*
```

## Validation Checklist

Before final output:

- Number `NNNN` is correct and consistent in title/closing.
- File naming follows `DEV_REPORT_NNNN_<SLUG>_<YYYY-MM-DD>.md`.
- Structure matches recent project reports.
- **`## Statistiche e progresso` is present** with all four subsections (LOC, sistemi, bug, gameplay); no invented metrics.
- Every major intervention has Problem + Solution.
- `File modificati` table is complete and coherent.
- No second/alternative report format introduced.

## What Not To Do

- Do not invent a new DEV REPORT schema unrelated to existing repository reports.
- Do not omit **`## Statistiche e progresso`** or fabricate LOC/bug counts.
- Do not produce verbose narrative if the user asked for a short report.
- Do not omit concrete file references when describing implementation changes.
