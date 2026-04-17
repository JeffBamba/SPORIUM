# DEV REPORT 0084 — FUN IMPROVEMENTS v.01 (Workstream A->E completati)

**Data:** 2026-04-17  
**Sprint / contesto:** completamento iterazione UX/gameplay readability su Pot Terminal e Lab Terminal con focus su chiarezza causale, guida operativa e mantenimento profondita hardcore.  
**Riferimento piano:** `.cursor/plans/fun_improvements_v.01_77d00442.plan.md`  
**Report precedente:** `DEV_REPORT_0083_EXTRACTOR_VERTICAL_SLICE_DISPLAY_WORLDSPACE_PROTO_2026-04-13.md`

---

## Sommario interventi

1. Completati e chiusi nel piano i Workstream A, B, C, D, E e `cross-qa-kpi`.
2. Potenziata la UX del Terminal Pot (quick actions, chiarezza stato/driver, pH/additivi/fertilizzanti) mantenendo le regole runtime esistenti.
3. Introdotto il Terminale Lab come orchestratore del flow `CREA NUOVO SEME` senza duplicare pannelli macchina o logiche gameplay.
4. Implementata schermata fullscreen di analisi progetto con tipologie `Replica / Ibrido / Nuovo Profilo`, dettagli requisiti e selezione non vincolante.
5. Esteso il gate `collect output pronto` a tutti gli step del flow Lab con blocco progressione e guidance visiva sui pulsanti macchina.
6. Migliorata la leggibilita terminale (palette stati, outcome box evidenziato, copy operativo schematico).

---

## 1. Workstream A — Contratto azioni e quick panel operativo Pot

### Problema
Le azioni principali sul pot non erano esposte con un modello uniforme `preview -> conferma -> esito`, e il player non aveva una guida rapida coerente col terminale profondo.

### Soluzione
- Allineata la superficie `PlantCardV3` con quick actions e microcopy causale.
- Definita coerenza tra azioni rapide e azioni terminal-only.
- Mantenuto il terminale profondo come layer hardcore (`STATUS`, comandi avanzati, dettaglio tecnico).

**File principali:** `PlantCardV3TerminalController.cs`, `PlantCardV3_Terminal.uxml`, `PlantCardV3_Terminal.uss`

---

## 2. Workstream B/C — Condition clarity + pH/additivi/fertilizzanti

### Problema
Serviva una lettura piu immediata di stato pianta, driver e rischio, senza alterare la simulazione esistente.

### Soluzione
- Esposizione sintetica di condizione/trend/driver con rimando al dettaglio.
- Migliorata UX pH: direzione e distanza qualitativa.
- Integrati suggerimenti contestuali su additivi e compatibilita fertilizzanti.
- Rafforzato il fail-safe UX su incompatibilita critiche (warning + conferma esplicita).

**File principali:** `PlantCardV3TerminalController.cs`, `DomeStatusHUDController.cs` (wiring/stato visuale)

---

## 3. Workstream D — Terminale Lab dual mode (standalone + progetto)

### Problema
Il loop Lab era percepito come frammentato; mancava un punto unico per orchestrare i pannelli macchina esistenti.

### Soluzione
- Introdotto `LabTerminalPanelController` come regia UI del loop, senza nuova simulazione.
- Collegate aperture dirette ai pannelli esistenti (`Extractor`, `Catalizzatore`, `Fusion`, `Incubator`).
- Aggiunta lavagna digitale con stati step (`Da fare`, `In corso`, `Completato`, `Bloccato`).
- Gestita modalita standalone separata dal flow progetto.

**File principali:** `LabTerminalPanelController.cs`, `LabTerminalPanel.uxml`, `LabTerminalPanel.uss`, `LabTerminalOpener.cs`

---

## 4. Workstream E — Analisi progetto + intento non vincolante

### Problema
All'avvio `CREA NUOVO SEME` mancava un momento di analisi strategica e una spiegazione operativa delle tipologie progetto.

### Soluzione
- Schermata fullscreen dedicata `ANALISI PROGETTO SEME` attivata solo dopo click su `CREA NUOVO SEME`.
- Analisi inventario player + `SeedStorage` (frutti/reagenti) con progress bar.
- Tipologie supportate:
  - `REPLICA`
  - `IBRIDO`
  - `NUOVO PROFILO`
- Dettaglio schematico per tipologia selezionata:
  - `Progetto`
  - `Item necessari`
  - `Status` (`Item presenti/mancanti`, `Progetto eseguibile/non eseguibile`)
- Selezione non vincolante, cambio direzione consentito, pulsante `ANNULLA SCELTA` per uscita dal flow.

**File principali:** `LabTerminalPanelController.cs`, `LabTerminalPanel.uxml`, `LabTerminalPanel.uss`

---

## 5. Gating progressione per collect output (tutti gli step Lab)

### Problema
Il player poteva avanzare allo step successivo anche con output pronto ma non raccolto nello step precedente, generando confusione operativa.

### Soluzione
- Introdotto gating di progressione basato su `collect` obbligatorio per ogni step:
  - Extractor
  - Catalizzatore
  - Fusion
  - Incubator
- `APRI STEP CORRENTE` disabilitato finche l'output pronto non viene raccolto.
- CTA dinamica su bottone guida: `RITIRA OUTPUT DA <MACCHINA>`.
- Pulse e accento colore su tutti i pulsanti `APRI` delle macchine con output pronto.
- Chiusura progetto aggiornata su collect finale (seed ritirato), non solo su stato ready.

**File principali:** `LabTerminalPanelController.cs`, `LabTerminalPanel.uss`

---

## 6. Outcome box e leggibilita terminale

### Problema
La sezione `Outcome` risultava poco leggibile e troppo simile a testo descrittivo continuo.

### Soluzione
- Outcome trasformato in box terminale ad alto contrasto (background/border/padding/font emphasis).
- Evidenziazione valori con palette Sporium coerente:
  - verde per risultato/tratti chiave
  - viola per indice/percentuali sensibili
  - cyan per campi tecnici (stage/famiglia/origine)
- Copy orientato a sintassi operativa da terminale (non narrativa lunga).

**File principali:** `LabTerminalPanelController.cs`, `LabTerminalPanel.uss`

---

## File modificati (tabella)

| Path | Tipo modifica |
|------|----------------|
| `.cursor/plans/fun_improvements_v.01_77d00442.plan.md` | Aggiornamento piano, estensione Workstream E, chiusura stati a `completed` |
| `Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs` | Wiring UX Pot terminal, stato/azioni/feedback |
| `Assets/_Project/UI/UIToolkit/PlantCardV3/PlantCardV3_Terminal.uxml` | Aggiornamenti layout Pot terminal |
| `Assets/_Project/UI/UIToolkit/PlantCardV3/PlantCardV3_Terminal.uss` | Aggiornamenti stile Pot terminal |
| `Assets/_Project/Scripts/UI/UIToolkit/DomeStatusHUD/DomeStatusHUDController.cs` | Allineamenti stato/tooltip/coerenza visuale |
| `Assets/_Project/Scripts/UI/UIToolkit/Lab/LabCatalizzatorePanelController.cs` | Esposizione stato runtime read-only per orchestrazione terminale |
| `Assets/_Project/Scripts/UI/UIToolkit/Lab/LabFusionPanelController.cs` | Esposizione stato runtime read-only per orchestrazione terminale |
| `Assets/_Project/Scripts/UI/UIToolkit/Lab/LabIncubatorPanelController.cs` | Esposizione stato runtime read-only per orchestrazione terminale |
| `Assets/_Project/Scripts/UI/UIToolkit/Lab/LabTerminalPanelController.cs` | Orchestrazione Workstream D/E, analisi progetto, gating collect, guidance e outcome |
| `Assets/_Project/UI/UIToolkit/Lab/LabTerminalPanel.uxml` | UI Terminale Lab, schermata analisi fullscreen, azioni flow |
| `Assets/_Project/UI/UIToolkit/Lab/LabTerminalPanel.uss` | Tema/stati/pulse/box outcome e leggibilita terminale |
| `Assets/_Project/Scripts/Interactables/LabTerminalOpener.cs` | Apertura Terminale Lab da interagibile scena |

---

## Regole / vincoli rispettati

- Nessuna nuova simulazione gameplay introdotta: solo orchestrazione UX su sistemi esistenti.
- Riuso dei pannelli Lab esistenti, senza duplicazione funzionale.
- Mantenuto pattern runtime con `ServiceContainer` per dipendenze globali UI/runtime.
- Rispettata separazione tra authoring UXML/USS e override dinamici runtime.
- Applicata coerenza cromatica stati (`in corso`, `completato`, `bloccato`) e guidance operativa.

---

## Note operative (Unity / QA)

- Verificare in Play Mode:
  1. avvio `CREA NUOVO SEME` -> analisi fullscreen;
  2. scelta tipologia + dettaglio requisiti;
  3. collect obbligatorio su ogni step prima del successivo;
  4. pulse sul pulsante `APRI` della macchina con output pronto;
  5. outcome box evidenziato dopo collect.
- Controllare save/load su progetto seme in corso con output intermedi pronti.

---

*Fine DEV REPORT 0084.*
