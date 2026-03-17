---
name: Piano unico Restore Italian + UX Terminal
overview: "Modificare il piano restore_italian_and_multilanguage inserendo la sezione 5.0 (UX Terminal POT già implementato, incluso STATUS Consiglio/Condizioni per Vaso da DEV REPORT 0064) e la nota in 5.2. Un solo file da modificare: il piano stesso."
todos: []
isProject: false
---

# Piano unico: aggiornare Restore Italian and Multilanguage

## File da modificare

Un solo file: [.cursor/plans/restore_italian_and_multilanguage_896f7970.plan.md](.cursor/plans/restore_italian_and_multilanguage_896f7970.plan.md).

## Modifica 1 – Inserire sezione 5.0

Dopo la riga:

```markdown
## Fase 5 – Terminale POT (PlantCardV3): UXML e C#

### 5.1 UXML Terminal e DetailPage
```

inserire **prima** di `### 5.1 UXML Terminal e DetailPage` il blocco seguente (in modo che 5.0 preceda 5.1):

```markdown
### 5.0 Comportamento e UX Terminal POT (già implementato)

Le seguenti funzionalità sono già state implementate; non vanno rifatte durante l'applicazione del piano. Servono solo come riferimento.

- **Loading UX**: dopo l'invio di un comando compaiono messaggi [SYS] in stile CRT ("Richiesta registrata nel sistema", "Loading framework per [contesto]..."), spinner (pallina ruotante) accanto al prompt, delay differenziati (comando valido ~1,8 s, errore ~0,65 s, step successivi ~0,95 s). Stesso flusso per tutti i comandi e per gli step (selezione item, conferma Y/N, esecuzione coda).
- **Testo di loading lampeggiante**: le due righe [SYS] alternano colore ogni 350 ms per feedback visivo.
- **Typewriter velocissimo**: l'output dei comandi (e degli step) usa effetto typewriter a gruppi di frasi (blocchi ~28 caratteri, delay ridotto), non carattere per carattere.
- **Righe decorative rimosse**: eliminate le cornici con caratteri ╔ ╗ ║ ╚ ╝ da elenco comandi, ACTION QUEUE, POT STATUS, AVAILABLE ITEMS, CONFIRM ACTION; restano solo testo, bullet (▸) e numeri.
- **Titoli di avvio**: le due righe "SPORIUM INCUBATOR CONTROL TERMINAL v3.1" e "AUTOMATED CULTIVATION MANAGEMENT SYSTEM" sono presenti in boot e in RenderWelcome (senza cornice); in Fase 5.2 vanno solo tradotte in italiano.
- **Bordo superiore box console**: il bordo top del box dove si legge il testo (`.pcv3-console-view`) è reso visibile (4px, colore 0.9); `overflow: visible` su `pcv3-right` e `pcv3-inner` evita il clipping.
- **STATUS – Consiglio e Condizioni per Vaso**: il comando STATUS include già le sezioni **▸ CONSIGLIO** (consigli contestuali per vaso, stile black humor Sporium) e **▸ CONDIZIONI PER VASO** (legenda tooltip). Non vanno reimplementate; in Fase 5.3/5.4 vanno solo tradotti in italiano titoli e messaggi di consiglio (riferimento: [Assets/Docs/REPORT/DEV_REPORT_0064_TERMINAL_STATUS_CONSIGLIO.md](Assets/Docs/REPORT/DEV_REPORT_0064_TERMINAL_STATUS_CONSIGLIO.md)).

```

Risultato: sotto "## Fase 5..." compaiono prima "### 5.0 Comportamento e UX..." e poi "### 5.1 UXML Terminal e DetailPage" (5.1, 5.2, 5.3, 5.4 restano invariati come numerazione).

## Modifica 2 – Nota in 5.2

Nella sezione **### 5.2 PlantCardV3TerminalController – Boot e Welcome**, aggiungere come primo paragrafo (subito dopo il titolo, prima dei bullet con i file):

```markdown
I due titoli di avvio sono già presenti e visibili (vedi 5.0); in questa fase vanno solo tradotti in italiano come sotto.
```

Poi lasciare invariati i bullet esistenti (array boot, RenderWelcome con titoli e corpo in italiano).

## Riepilogo


| Azione              | Dove nel file                                             |
| ------------------- | --------------------------------------------------------- |
| Inserire blocco 5.0 | Tra "## Fase 5..." e "### 5.1 UXML Terminal e DetailPage" |
| Aggiungere nota     | Inizio sezione 5.2, prima dei bullet                      |


Nessun altro file da creare o modificare: solo [.cursor/plans/restore_italian_and_multilanguage_896f7970.plan.md](.cursor/plans/restore_italian_and_multilanguage_896f7970.plan.md).