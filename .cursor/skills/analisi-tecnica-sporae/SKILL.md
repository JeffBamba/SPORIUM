---
name: analisi-tecnica-sporae
description: >-
  Sporae technical analysis from fresh evidence: latest DEV REPORTs, delta vs prior
  analysis doc, prioritized performance/optimization, Demo vs Full status vs Notion GDD
  when data available. No guessing. Use for periodic analisi tecnica or architecture audit.
---

# Analisi tecnica Sporae — skill agente

## Quando usarla

Richieste tipo: *analisi tecnica*, *revisione architetturale*, *stato del codebase*, *audit*, *gap su codice*, *analisi periodica*, *come ANALISI_TECNICA_COMPLETA_SPORIUM*, *valutazione infrastrutturale*.

## Stato dell’arte al momento della richiesta (non negoziabile)

L’output deve riflettere **solo** ciò che è verificabile **in questa sessione**, sulla working tree attuale, alla **data indicata nel documento**.  
**Vietato** presentare come fatti: ricordi di chat, numeri copiati da `ANALISI_TECNICA_COMPLETA_SPORIUM.md` o da altre analisi storiche, “circa / ~ / order of magnitude” **senza** misura esplicita etichettata come **STIMA** con metodo dichiarato.

- Ogni **numero** (occorrenze, righe, conteggio file): **ricalcolo** con comando read-only + citazione (es. `rg` con path, o conteggio righe su file elencati).
- Ogni **affermazione strutturale** (“esiste”, “non c’è più”, “è così”): **file o simbolo citato** (path + riga o estratto breve) oppure sezione **NON VERIFICATO**.
- Se qualcosa non è misurabile da CLI (es. solo in Editor Unity): scrivi **NON MISURABILE IN QUESTA SESSIONE** e non inventare cifre.

## Fase A — Ultimi DEV REPORT (sempre, prima del resto)

**Prima** di metriche o architettura sul codice:

1. Percorso: `Assets/Docs/REPORT/DEV_REPORT_*.md`.
2. Ordina per **`NNNN`** in `DEV_REPORT_NNNN_*.md` (numerico **decrescente** = dal più recente).
3. **Leggi almeno i primi 5** file dell’elenco (se ce ne sono meno di 5, **leggi tutti** quelli presenti).
4. Nel documento di analisi includi una sezione **## Allineamento agli ultimi sviluppi (DEV REPORT)** con:
   - elenco dei report letti (nome file + titolo H1 se presente);
   - bullet sintetici: cosa è stato fatto/chiuso di recente **rilevante per lo scope** dell’analisi;
   - se un’osservazione tecnica **contraddice** un DEV REPORT recente, segnalalo esplicitamente (“il report X dice …; il repo oggi mostra …”) con evidenza.

Questo passo serve a **non** proporre raccomandazioni già superate o in conflitto con lavoro documentato.

## Fase B — Baseline per confronto progressi (analisi tecnica precedente)

1. Cerca documenti **`ANALISI_TECNICA*.md`** in **`Assets/Docs/`** e nella **root** del repo; se l’utente indica un file baseline, usa quello.
2. **Baseline “precedente”:** preferisci il file con **data nel nome** più recente **tra quelli strettamente precedenti** alla data dell’analisi corrente; se ambiguo, **chiedi all’utente** quale usare.
3. **Se non esiste** alcuna analisi precedente: sezione finale **Progressi** = dichiarazione esplicita *“Nessuna analisi tecnica precedente trovata nel repo; confronto non applicabile.”* (nessun inventario).
4. Per ogni punto del documento precedente che vuoi contrastare: **ricalcola** sul repo oggi (come per Fase “stato dell’arte”) — **non** assumere che il vecchio testo sia ancora vero.

## Fase C — GDD Notion (Demo / Full)

- Il testo del GDD su **Notion** non è nel repo: **non** inventare percentuali o checklist “completate” senza fonte.
- **Se** l’utente fornisce link/pagine Notion, export, oppure accesso MCP Notion autorizzato: mappa **Demo** vs **Full** con evidenza (citazioni o estratti consentiti dalla fonte).
- **Se** Notion non è disponibile: sezione **Status vs GDD** = tabella con colonne *Voce GDD* | *Stato* = **NON VERIFICATO (Notion non consultato)** + elenco punti da validare appena hai il GDD; in alternativa usa **solo** artefatti repo tracciabili (es. `.cursor/plans/demo_alpha_1_0_gap_map.plan.md`, `.cursor/rules/feature-both-demo-full-parity.mdc`, `DemoSessionState`, scene `SCN_VaultMap`) etichettati chiaramente come **allineamento progetto locale**, non come sostituto del GDD Notion.

## Vincoli (obbligatori)

1. **Analisi** nel senso di `.cursor/rules/analysis-no-suppositions-fresh-scan.mdc`: evidenza fresca, mai supposizioni su versioni precedenti del repo o del documento.
2. Se i dati mancano o lo scope è troppo vasto: **dichiara cosa non hai verificato**; non riempire con ipotesi.
3. Allinea raccomandazioni ai vincoli progetto quando pertinenti: `architecture-runtime-services`, `feature-both-demo-full-parity`, regole UI se il task tocca UIToolkit.

## Prosa discorsiva per sezione / area (obbligatorio)

Senza **paragrafi in prosa** l’analisi **non è completa**: servono a spiegare **in parole semplici** cosa è progredito, cosa resta delicato e cosa conta per gioco/prodotto anche per **chi non è tecnico**.

- **Cosa:** dopo i dati, tabelle e bullet di **una** sezione o **un’**area di analisi, **prima** del titolo della sezione successiva, includere **almeno un paragrafo breve** (prosa continua, non solo elenchi): cosa **migliora**, cosa **resta critico o aperto**, linguaggio **chiaro**.
- **Dove:** in coda a ogni blocco di evidenze di quell’area — vedi template sotto (etichetta suggerita: *In sintesi per lettore non tecnico* o equivalente).
- **Ambito:** vale per ogni **`##`** di contenuto analitico del template (es. Allineamento DEV REPORT, Executive summary, Statistiche, Metodologia, Metriche, sotto-architetture `###`, Code smell, Piano, Fuori scope, Progressi vs baseline, Performance, Status Demo/Full). Per sezioni solo elenco (es. **Riferimenti file**) aggiungere **almeno una** frase di contesto che colleghi l’elenco al messaggio dell’analisi.

## Documento di riferimento (struttura)

Per tono, sezioni e **chiusura narrativa di ogni area** ispirati al template storico del repo (non copiare numeri obsoleti da lì). La struttura attesa combina **evidenze strutturate + paragrafo narrativo** prima di passare all’area successiva.

- `ANALISI_TECNICA_COMPLETA_SPORIUM.md` (root repo, esempio di struttura completa — da integrare mentalmente con la regola *paragrafo per sezione/area* sopra)

## Dove salvare l’output

- Preferenza team: **`Assets/Docs/`** con nome tipo `ANALISI_TECNICA_<slug>_<YYYY-MM-DD>.md`, oppure in root solo se il task lo richiede esplicitamente.
- Chiedi all’utente se non ha indicato path.

## Template output (italiano)

Usa titoli chiari; emoji opzionali (il vecchio doc ne usava molte — ok ridurle per leggibilità).

```markdown
# Analisi tecnica — <titolo>

**Data:** YYYY-MM-DD  
**Scope:** <cosa include / cosa esclude>  
**Repo / branch:** <se noto>  
**Metodo:** analisi read-only su working tree alla data sopra; DEV REPORT letti + comandi citati sotto. Nessun dato storico non ricalcolato.

---

## Allineamento agli ultimi sviluppi (DEV REPORT)

*(Obbligatorio: vedi Fase A nella skill.)*

**In sintesi per lettore non tecnico (obbligatorio):** *(paragrafo breve in prosa: cosa il team ha appena consegnato che conta per questa analisi e cosa resta allineato o in tensione con il codice odierno.)*

---

## Executive summary

- Sintesi in 5–10 righe.
- Valutazione qualitativa (senza inventare punteggi se non hai criteri oggettivi condivisi; se usi una scala, definiscila).

**In sintesi per lettore non tecnico (obbligatorio):** *(paragrafo breve: messaggio complessivo in linguaggio semplice.)*

---

## Statistiche e contesto progress (gameplay / prodotto)

Blocco strutturato + prosa: obbligatori **i bullet sotto** e, in coda alla sezione, il paragrafo *In sintesi per lettore non tecnico* (vedi **Prosa discorsiva**). Allineato allo spirito dei DEV REPORT: **numeri veri o N/D** e **cosa significa per chi gioca**. Stesse regole anti-invenzione della skill dev-report.

- **Righe di codice:** sintesi (es. totale `.cs` in scope + comando) o **N/D** / non misurato.
- **Sistemi funzionanti:** elenco di sistemi o macro-flussi **osservabili dal repo o dai DEV REPORT letti**; ciò che richiede Play → **NON MISURABILE IN QUESTA SESSIONE** o **da validare in Editor**.
- **Bug risolti:** solo se ricostruibili da report/commit di sessione; altrimenti **non quantificabile qui** (non inventare conteggi).
- **Progresso gameplay / prodotto:** 3–8 bullet in linguaggio non tecnico (cosa sblocca o migliora l’esperienza), coerente con Fase A.

**In sintesi per lettore non tecnico (obbligatorio):** *(paragrafo breve: cosa significano i numeri/N/D per chi gioca o per le milestone prodotto, senza ripetere solo la tabella.)*

---

## Metodologia e evidenze

- Elenco comandi eseguiti (o da eseguire) e cosa hanno misurato.
- Per ogni metrica numerica: **fonte** (es. output `rg --count`, `wc`, Unity non misurabile da CLI → dichiaralo).

**In sintesi per lettore non tecnico (obbligatorio):** *(paragrafo breve: perché queste misure sono state raccolte e cosa si può dedurre in modo semplice.)*

---

## Metriche (tabelle)

| Metrica | Valore | Come misurato | Note |
|---------|--------|---------------|------|
| ... | ... | `rg ...` | ... |

**In sintesi per lettore non tecnico (obbligatorio):** *(paragrafo breve: lettura d’insieme delle metriche, senza jargon non spiegato.)*

---

## Architettura e sistemi

Per ogni area concordata nello scope (es. Core, Dome, UI Toolkit, Save, …): duplicare il blocco seguente **per ogni** `###` — il paragrafo *In sintesi* chiude quell’area **prima** del successivo `###` o della sezione **Code smell**.

### Nome area

- **Cosa fa** (2–4 righe, verificabile da file elencati).
- **Punti di forza** (con riferimento file/classi).
- **Rischi / debito** (con evidenza).
- **Raccomandazioni** (priorità P0/P1/P2, allineate alle regole progetto).

**In sintesi per lettore non tecnico (obbligatorio):** *(paragrafo breve per quest’area: cosa funziona bene in pratica, dove il rischio si fa sentire nel prodotto, cosa fare per prime — in parole semplici.)*

---

## Code smell e anti-pattern

Tabella o elenco: problema → evidenza → impatto → azione suggerita.

**In sintesi per lettore non tecnico (obbligatorio):** *(paragrafo breve: quali “spigoli” del codice pesano di più sull’esperienza o sulla velocità del team.)*

---

## Piano prioritizzato

1. …
2. …

**In sintesi per lettore non tecnico (obbligatorio):** *(paragrafo breve: ordine delle priorità spiegato come scelte di investimento, non come lista tecnica.)*

---

## Fuori scope / incertezze

- Cosa non è stato ispezionato e perché.

**In sintesi per lettore non tecnico (obbligatorio):** *(paragrafo breve: cosa non si sa ancora e perché può influenzare decisioni.)*

---

## Riferimenti file

Elenco path toccati dall’analisi (per navigazione rapida).

*(Obbligatorio: almeno una frase di contesto che dica in cosa questi file sono rilevanti per il messaggio dell’analisi.)*

---

## Progressi rispetto all’analisi tecnica precedente

**Baseline:** `<path file precedente>`, data `<YYYY-MM-DD>` (o: nessuna baseline trovata).

| Argomento / metrica | Come era nella baseline (citazione sintetica o sezione doc) | Stato oggi (evidenza fresca: comando / file) | Esito |
|---------------------|-------------------------------------------------------------|-----------------------------------------------|--------|
| … | … | … | Progresso / Invariato / Regressione / N.A. |

- Sintesi in 3–6 bullet: **cosa è migliorato**, **cosa è peggiorato o invariato**, **cosa resta aperto**.

**In sintesi per lettore non tecnico (obbligatorio):** *(paragrafo breve: arco del progresso raccontato come storia del prodotto, non solo tabella.)*

---

## Performance e ottimizzazione (stato progetto)

**Cosa va bene** (solo con evidenza repo o misura di questa sessione): elenco puntato + riferimenti.

**Cosa non va / debito** (evidenza: pattern, hotspot file, anti-pattern architetturali): elenco puntato + riferimenti.

**Priorità interventi performance** (ordine **P0 → P1 → P2**):

| Priorità | Intervento | Impatto atteso | Evidenza / metrica | Rischio / costo |
|----------|------------|------------------|---------------------|-----------------|
| P0 | … | … | … | … |
| P1 | … | … | … | … |
| P2 | … | … | … | … |

- **Profiler Unity / frame budget / memoria runtime:** se non hai catturato dati in questa sessione, sezione o riga dedicata: **NON MISURABILE IN QUESTA SESSIONE (Profiler)** — proponi solo ipotesi da verificare in Play, etichettate come **DA VALIDARE IN EDITOR**.

**In sintesi per lettore non tecnico (obbligatorio):** *(paragrafo breve: impatto su fluidità, caricamenti o scalabilità spiegato in termini di esperienza; cosa è certo vs cosa va provato in Editor.)*

---

## Status sviluppo: Demo e Full Game vs GDD (Notion)

**Fonte GDD:** `<Notion / export / NON CONSULTATO>`.

| Area / voce (dal GDD o dal piano locale) | Demo — stato | Full — stato | Evidenza repo / doc | Note |
|------------------------------------------|--------------|----------------|---------------------|------|
| … | … | … | … | … |

- **Gap noti** solo se tracciabili (piani, codice, flag demo). Se il GDD Notion non è stato letto: dichiaralo e non compilare percentuali fittizie.

**In sintesi per lettore non tecnico (obbligatorio):** *(paragrafo breve: cosa significa per demo vs gioco completo in linguaggio di prodotto, inclusi limiti se il GDD non è stato consultato.)*

```

## Checklist prima di consegnare

- [ ] **Prosa per area:** ogni sezione del template (e ogni `###` sotto Architettura) include **almeno un paragrafo breve in prosa** dopo evidenze/dati e **prima** della sezione successiva, per lettori non tecnici (vedi **Prosa discorsiva per sezione / area**).
- [ ] **Statistiche e contesto progress:** sezione presente (righe, sistemi, bug se ricostruibili, progresso gameplay in linguaggio chiaro; **N/D** dove non misurabile).
- [ ] **Fase A:** letti e documentati gli **ultimi 5** `DEV_REPORT_*.md` (o tutti se in cartella ce ne sono meno di cinque), con sezione dedicata nel markdown.
- [ ] **Fase B:** individuata **baseline** analisi precedente **o** dichiarato esplicitamente che non c’è; tabella **Progressi** compilata senza assumere verità del doc vecchio senza ricalcolo.
- [ ] **Performance:** sezioni “va bene / non va” + tabella **P0–P2**; Profiler solo se dati reali, altrimenti etichetta **NON MISURABILE** / **DA VALIDARE IN EDITOR**.
- [ ] **Demo / Full vs GDD:** fonte Notion dichiarata; se non consultata, stato **NON VERIFICATO** + uso eventualmente solo piani/repo con etichetta chiara.
- [ ] Ogni affermazione **quantitativa** ha **evidenza di questa sessione** (comando + risultato) oppure è **STIMA** con metodo; **mai** guessing mascherato da precisione.
- [ ] Nessun numero o conclusione presa **solo** da `ANALISI_TECNICA_COMPLETA_SPORIUM.md`, chat passate o DEV REPORT **senza** aver verificato il file/pattern nel repo **ora**.
- [ ] Raccomandazioni coerenti con `ServiceContainer` / no `FindObjectOfType` dove la regola architettura lo vieta.
- [ ] Scope, limiti e voci **NON VERIFICATO** / **NON MISURABILE IN QUESTA SESSIONE** espliciti dove serve.

## Integrazione con SVILUPPA

Se il task è anche `SVILUPPA:`, dopo l’analisi tecnica separata incorpora i risultati rilevanti in `# ANALISI` secondo `.cursor/rules/sviluppa.mdc` (inclusi ultimi 5 DEV REPORT).
