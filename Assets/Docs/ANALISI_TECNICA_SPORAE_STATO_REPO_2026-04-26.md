# Analisi tecnica — Stato repo Sporae (codebase, architettura, allineamento demo)

**Data:** 2026-04-26  
**Scope:** Script C# sotto `Assets/_Project/Scripts`, ultimi DEV REPORT, confronto con analisi tecnica del 2026-03-19, indicatori statici di debito/performance, allineamento **locale** Demo vs Full (piano + codice). **Escluso:** esecuzione Unity, Profiler, test automatici, contenuto GDD Notion non presente nel repo.  
**Repo / branch:** `D:\Sporae_Build_Beta`, branch `main` (working tree con modifiche locali non committate su file `.cursor/rules/` al momento della misura).  
**Metodo:** analisi read-only su working tree; lettura file markdown; comandi PowerShell su file `*.cs` ricorsivi in `Assets/_Project/Scripts`. Nessun dato numerico riportato senza ricalcolo in questa sessione.

---

## Allineamento agli ultimi sviluppi (DEV REPORT)

Report letti (ordinamento per `NNNN` decrescente tra i file `DEV_REPORT_*.md` presenti in `Assets/Docs/REPORT/`, primi cinque):

| File | Titolo H1 |
|------|-----------|
| `DEV_REPORT_0094_ICONE_VARIANTI_INVENTARIO_SEEDSTORAGE_2026-04-25.md` | DEV REPORT 0094 — Icone varianti inventario, Seed Storage e allineamenti scena VaultMap |
| `DEV_REPORT_0093_FIX_HUD_VO_E_INVENTARIO_DEMO_2026-04-23.md` | DEV REPORT 0093 — Fix HUD fissa durante VO demo + lock inventario iniziale demo |
| `DEV_REPORT_0092_HUD_MODALI_SEED_STORAGE_PLANTCARD_LAB_FOOD_2026-04-21.md` | DEV REPORT 0092 — HUD fissa e overlay modali: Seed Storage, PlantCardV3, Lab Terminal, Food Synth; uniformità dim |
| `DEV_REPORT_0091_DEMO_VO_BEAT23_SEED_STORAGE_UX_2026-04-21.md` | DEV REPORT 0091 — Demo Alpha: VO post-colazione (beat 2/3), missione Seed Storage, fix UX overlay e blocchi narrativi |
| `DEV_REPORT_0090_DISPENSA_REFRIGERATA_UI_SEPARATA_WIRING_FIX_2026-04-21.md` | DEV REPORT 0090 — Dispensa Refrigerata separata da FoodRoom: UI dedicata, wiring scena e fix regressioni input/interazioni |

Sintesi rilevante per questa analisi:

- **UI Toolkit / modali:** consolidamento di `GameplayUiModalLock` (blocco input vs visibilità HUD fissa), overlay dim unificato, pannelli Seed Storage, Lab, Food Room, PlantCardV3 e Dispensa allineati al pattern modale; fix regressione VO che nascondeva la HUD fissa (0093).
- **Demo Alpha:** missione Seed Storage, VO/highlight, bootstrap inventario **solo demo** (`5x` acqua potabile + `2x` cibo sintetico) documentato in 0093; beat narrativi in 0091.
- **Icone inventario:** catalogo categoria+variante, `GlobalIconResolver`, righe Seed Storage e HUD inventario (0094); coerente con uso `Resources` documentato nel report per fallback item (verificato in sessione: `Resources.Load` presente in `GlobalIconResolver.cs` per il catalogo — vedi sezione Architettura).
- **Macchine / scena:** Dispensa dedicata e wiring `SCN_VaultMap` (0090).

**Contraddizioni DEV REPORT vs repo (questa sessione):** nessuna incongruenza strutturale verificata senza eseguire il gioco; i report descrivono modifiche coerenti con i path citati. Eventuali regressioni runtime vanno validate in Editor.

---

## Executive summary

- Il dominio script in `Assets/_Project/Scripts` è **cresciuto** (313 file `.cs` vs 264 nella baseline del 2026-03-19), con lavoro concentrato su **HUD Foundation**, **modali UI Toolkit**, **narrativa demo** e **pipeline icone**.
- L’uso di **`ServiceContainer.Instance?.Get`** è aumentato (**212** occorrenze vs **144** nella baseline): service locator resta asse centrale; va tenuto sotto controllo per testabilità e accoppiamento.
- **`FindObjectOfType` / `FindObjectsOfType`** risultano **leggermente aumentati** rispetto alla baseline (103/66 file e 40/22 file vs 100/64 e 39/21): **non** allineato alla regola architetturale che ne scoraggia l’introduzione in gameplay; priorità di remediation resta alta.
- Classi molto grandi persistono: **`PlantCardV3TerminalController.cs`** resta un hotspot (**6765** righe, in leggero calo vs 7105 baseline); **`DayCycleController`** e **`PotActions`** crescono in dimensione.
- **Performance runtime** e **budget frame**: **non misurabili** in questa sessione senza Profiler Unity.
- **GDD Notion:** **non consultato**; la tabella Demo/Full si basa su **artefatti di repo** (piano gap map, `DemoSessionState`, scena `SCN_VaultMap`).

**Valutazione qualitativa:** scala implicita “maturità infrastrutturale / debito tecnico” — **stabile con debito noto** (god class + scene discovery), **miglioramento UX/demo documentato** negli ultimi report, **regressione numerica minima** su anti-pattern `FindObject*`.

---

## Statistiche e contesto progress (gameplay / prodotto)

Blocco di sintesi **obbligatorio** (allineato a `.cursor/skills/analisi-tecnica-sporae/SKILL.md` e alla convenzione “statistiche nei report”): numeri solo se **ricalcolati o citati da DEV REPORT** in questa sessione; niente backlog inventato.

### Righe di codice

| Voce | Valore | Come misurato |
|------|--------|----------------|
| File `.cs` in `Assets/_Project/Scripts` | **313** | `Get-ChildItem -Path "...\Assets\_Project\Scripts" -Recurse -Filter *.cs -File` → `.Count` |
| **Righe totali** (somma su tutti quei file) | **73.565** | Per ogni file: `Get-Content -LiteralPath` + `Measure-Object -Line` → somma (PowerShell, sessione 2026-04-26) |
| `PotActions.cs` | **1.950** righe | `Measure-Object -Line` sul file |
| `SPOR-BLK-01-03A-DayCycleController.cs` | **2.773** righe | idem |
| `PlantCardV3TerminalController.cs` | **6.765** righe | idem |
| `DomeStatusHUDController.cs` | **1.198** righe | idem |

*Nota:* il totale **73.565** righe riguarda **solo** `Assets/_Project/Scripts`; altri `.cs` in `Assets/` (editor, test, package) **non** inclusi.

### Sistemi funzionanti *(evidenza repo + DEV REPORT; Play non eseguito qui)*

Macro-aree **presenti e descritte come operative** negli ultimi cinque DEV REPORT e nei path citati (validazione runtime completa = **DA VALIDARE IN EDITOR**):

- **HUD Foundation** (TopBar, barre compatte, navigazione, notifiche) con logica **modale** (`GameplayUiModalLock`: `HidesFixedHud` vs blocco input).
- **Dome Status HUD** (cupola: vasi + Cryo, tooltip, coerenza con lock modale).
- **Modali macchina** allineati: Seed Storage, PlantCardV3, Lab Terminal, Food Room (overlay dim unificato **0,65** come da report).
- **Dispensa refrigerata** — UI e opener dedicati, distinti da Food Synth (**0090**).
- **Demo Alpha:** `DemoSessionState` / beat, VO overlay (typewriter, highlight, sequenza “continua”), missione **Seed Storage**, inventario iniziale **solo demo** (**0091–0093**).
- **Inventario / icone:** catalogo `GlobalIconCatalog` + `GlobalIconResolver`, righe Seed Storage e HUD inventario con sprite risolti (**0094**).

### Bug risolti / regressioni chiuse *(finestra documentale: DEV 0090 → 0094)*

Non esiste nel repo un **conteggio unico** tipo issue tracker aggregato; qui si elencano **correzioni esplicitamente narrate** come bug o regressione in quei report (non è l’intero storico progetto):

| # | Origine (report) | Sintomo / tema | Esito documentato |
|---|------------------|----------------|---------------------|
| 1 | 0090 | Input globale bloccato (root panel sempre attivo) | Corretto (wiring/UI) |
| 2 | 0090 | Doppia apertura Dispensa + Food Synth | Corretto |
| 3 | 0091 | Click “continua” VO accettato troppo presto; `onComplete` prima dell’animazione di uscita | Sequenza VO sistemata |
| 4 | 0091 | Missione Seed Storage appariva prima del termine VO beat 3 | Append missione post-`onComplete` VO |
| 5 | 0093 | HUD fissa nascosta durante VO demo (regressione post-0092) | Separazione `HidesFixedHud` / `BlocksWorldInput` |
| 6 | 0093 | Inventario demo con starter “full game” | Branch `isDemo` in bootstrap inventario |

Altri interventi in **0092** / **0094** sono prevalentemente **UX, uniformità e feature** (non sempre classificati come “bug” nel testo del report).

**Totale tabella:** **6** voci di tipo bug/regressione nella finestra citata — **non** estendibile a “bug risolti nel mese” senza altra fonte.

### Progresso gameplay / prodotto *(linguaggio non tecnico)*

- **Demo:** flusso wake → cucina → VO e missioni più **leggibili**; obiettivo “vai al deposito semi” **allineato** al testo; partenza con **solo acqua e cibo** in inventario.
- **Schermate macchina:** stessa **oscurità** dietro al pannello; **HUD fissa** non copre più il modale; durante il **doppiaggio** la barra non sparisce per errore.
- **Dispensa** ha **schermata propria**, separata dal sintetizzatore cibo.
- **Inventario e deposito semi:** **icone** coerenti con tipo/variante oggetto; meno “scatole vuote” in lista.
- **Cupola:** stato vasi/Cryo e tooltip restano nel perimetro HUD documentato; nessun ritorno di `AlwaysVisiblePotHUD` negli script (**0** occorrenze).

---

## Lettura d’insieme — cosa racconta il progetto (e i voti)

*Questa sezione integra le tabelle e le metriche che seguono: serve a chi vuole capire **in parole** cosa significa lo stato del codice, senza dover leggere numeri e nomi di API. I voti sono su scala **1–10** (1 = gravemente insufficiente, 10 = eccellente / difficile da migliorare). Sono **giudizi qualitativi** dell’analisi del 2026-04-26, ancorati alle evidenze del documento — non sostituiscono misure Unity (Profiler, build player, test QA).*

### Struttura e organizzazione del codice — **7 / 10**

Sotto “struttura” intendiamo: *come è organizzato il lavoro tra file*, *quanto è chiaro dove vive una responsabilità*, *quanto il progetto si affida a pattern solidi*.

Il lato positivo è tangibile anche senza aprire Unity: esiste un **filo conduttore a servizi** (`ServiceContainer`, stato demo dedicato, registry per i vasi nella cupola). In pratica il gioco non è un insieme di script isolati che si cercano a caso: c’è un’idea di **“centralino”** da cui richiedere sistemi condivisi, e una distinzione esplicita tra **partita demo** e **partita completa** senza duplicare il prodotto in due progetti separati — coerente con la regola “Both” del repo.

Il lato che tiene il voto lontano dall’8–9 è il **peso di pochi file enormi** (in particolare il terminale vasi in UI Toolkit, ancora migliaia di righe) e la **persistenza di ricerche nella scena** (`FindObjectOfType` e simili): in linguaggio non tecnico, è come se, ogni volta che serve un oggetto, qualcuno gridasse nella stanza *“c’è in giro qualcuno che fa il mestiere X?”* invece di avere un elenco sulla scrivania. Funziona, ma **costa fragilezza** (cambi la scena e qualcosa smette di trovarsi) e, in teoria, **costo a runtime** quando queste ricerche capitano nei momenti sbagliati. Il numero di queste chiamate non è esploso, ma **non è nemmeno sceso** rispetto a marzo: la direzione preferita dal team è ancora quella di **ridurle**, non di conviverci.

In sintesi: **architettura “da prodotto serio” con debito di manutenzione concentrato** in pochi punti critici.

### Performance — **6 / 10** *(solo evidenze statiche; vedi nota)*

Qui serve una **nota obbligatoria**: in questa analisi **non** sono stati misurati FPS, tempo di frame né memoria in Play Mode. Il **6** non dice “il gioco gira a 60 fps” o “c’è lag”: dice come stanno le **condizioni strutturali** che di solito **favoriscono o ostacolano** le performance, guardando al codice.

Cosa depone a favore: uso diffuso di **logging strutturato** (`SporiumLogger`) invece di `Debug.Log` sparsi; il **terminale vaso** leggermente **più snello** in righe rispetto alla baseline di marzo (meno codice nello stesso file può, in media, rendere più facile evitare lavoro inutile ogni frame — ma è un **indizio**, non una misura). Cosa pesa: molte **ricerche per tipo nella scena**, classi **chilometriche** dove è facile che qualcosa venga aggiornato troppo spesso senza accorgersene, e la **pipeline icone** che ancora prevede percorsi di fallback verso `Resources` per gli item (utile per robustezza, da tenere d’occhio se diventasse il caso normale invece dell’eccezione).

**Se** in una sessione futura il Profiler mostrasse spazio di miglioramento, questo voto andrebbe **ricalibrato su dati reali**. Per ora è un **“promosso con riserva”**: niente allarme rosso dedotto solo dal testo, ma **area da validare in Editor** prima di dichiarare il capitolo chiuso.

### Progressi rispetto a marzo e al lavoro documentato — **8 / 10**

Qui il giudizio è più alto perché si misura **cosa il team ha effettivamente messo in campo** nelle settimane coperte dai DEV REPORT letti (0090–0094), rispetto alla fotografia tecnica del **19 marzo**.

In parole povere, il filone recente è: **rendere la demo giocabile e leggibile** — non solo “funzionante”. Si è lavorato perché, quando apri un macchinario a schermo intero, **non resti la barra HUD sopra** a disturbare la lettura; che l’**oscurità** dietro al pannello sia **la stessa** tra laboratorio, dispensa, seed storage e terminale vasi; che la **voce fuori campo** della demo **non faccia sparire** l’interfaccia fissa per errore di logica (bug sistemato separando “blocco input” da “nascondi HUD”); che in **demo** tu parta davvero con **solo acqua e cibo sintetici**, come chiede il design, senza rovinare l’inventario della **nuova partita** completa; che la **missione “vai al deposito semi”** arrivi al momento giusto rispetto al testo, con **highlight** sulle parole importanti; che le **icone** in inventario e deposito semi **riflettano il tipo di oggetto** (anche varianti come acqua grezza vs potabile). A parte si è **separata la Dispensa** dalla stanza cibo sintetici, con **UI propria** e meno confusione tra macchine.

Questo è **progresso percepibile dal giocatore** e dal tester, non solo refactor interno. Il voto non è 9–10 perché restano **aperti** i temi strutturali di cui sopra (ricerche in scena, file giganti): **il prodotto avanza visibilmente**, mentre **l’ossatura tecnica** chiede ancora **cure di fondo** in parallelo.

| Voce | Voto (1–10) | In una frase |
|------|-------------|----------------|
| Struttura / organizzazione | **7** | Servizi e demo ben impostati; pochi “mostri” e ricerche scena frenano l’eccellenza. |
| Performance *(indicatori statici)* | **6** | Condizioni né disastrose né ottimizzate; serve Profiler per un voto “vero” sul runtime. |
| Progressi documentati (UX, demo, UI) | **8** | Molto lavoro utente-centrico e coerente col piano Alpha; debito tecnico non sparisce da solo. |

---

## Metodologia e evidenze

Comandi eseguiti (PowerShell, directory di lavoro implicita `D:\Sporae_Build_Beta`):

1. Enumerazione file: `$base = "...\Assets\_Project\Scripts"; (Get-ChildItem -Path $base -Recurse -Filter *.cs -File).Count` → **313**.
2. Pattern su tutti i `.cs` sotto `$base` con `Select-String -SimpleMatch` (ove indicato) o `-Pattern` con escape per `Debug.Log(`.
3. Conteggio file distinti: filtro `Get-ChildItem` + `Select-String -Quiet` per presenza nel file.
4. Righe file: `Get-Content <path> | Measure-Object -Line` sui path verificati esistere.
5. `git rev-parse --abbrev-ref HEAD` → **main**.
6. **Righe totali** su tutti i `.cs` in `Assets/_Project/Scripts`: somma di `(Get-Content -LiteralPath $f | Measure-Object -Line).Lines` per ogni file → **73.565** (313 file).
7. Lettura parziale: baseline `Assets/Docs/ANALISI_TECNICA_E_COSA_PUO_FARE_IL_GIOCATORE_2026-03-19.md`, piano `.cursor/plans/demo_alpha_1_0_gap_map.plan.md` (sezioni campione), cinque DEV REPORT sopraelencati, `DemoSessionState.cs`, `GlobalIconResolver.cs` (grep `Resources.Load`).

---

## Metriche (tabelle)

| Metrica | Valore | Come misurato | Note |
|---------|--------|---------------|------|
| File `.cs` in `Assets/_Project/Scripts` | **313** | `Get-ChildItem -Recurse -Filter *.cs` | Baseline 2026-03-19: **264** |
| Righe totali `.cs` (stessa cartella) | **73.565** | Somma `Measure-Object -Line` per file | Vedi anche **Statistiche e contesto progress** |
| `ServiceContainer.Instance?.Get` | **212** | `Select-String -SimpleMatch` su tutti i `.cs` | Baseline: **144** (stesso pattern) |
| `ServiceContainer.Instance` (qualsiasi) | **476** | `Select-String -SimpleMatch` | Metrica più ampia della baseline; utile solo come ordine di grandezza uso `Instance` |
| `FindObjectOfType` | **103** match in **66** file | `Select-String` + conteggio file con match | Baseline: **100** in **64** file |
| `FindObjectsOfType` | **40** match in **22** file | idem | Baseline: **39** in **21** file |
| `FindObjectsByType` | **11** | `Select-String -SimpleMatch` | — |
| `AlwaysVisiblePotHUD` | **0** | `Select-String` su `$base` | Allineato a baseline (rimozione legacy) |
| `DomePotRegistry` | **28** match in **11** file | `Select-String` + file distinti | Baseline: **18** in **7** file |
| `CryoMachineController` | **43** match in **13** file | idem | Baseline: **28** in **10** file |
| `PhSystem` | **506** match in **29** file | idem | Baseline: **236** in **22** file |
| `SporiumLogger.` | **954** | `Select-String` | I log di prodotto non passano principalmente da `Debug.Log` |
| `Debug.Log(` | **6** | `Select-String` con pattern escaped | Conteggio stringa letterale |
| Righe `PotActions.cs` | **1950** | `Measure-Object -Line` | Path: `Assets/_Project/Scripts/Dome/PotActions.cs`. Baseline: **1932** |
| Righe `SPOR-BLK-01-03A-DayCycleController.cs` | **2773** | idem | Baseline: **2722** |
| Righe `PlantCardV3TerminalController.cs` | **6765** | idem | Baseline: **7105** |
| Righe `DomeStatusHUDController.cs` | **1198** | idem | Baseline: **786** |

---

## Architettura e sistemi

### Core e servizi

- **Cosa fa:** `ServiceContainer` e installer (`GamePlayInstaller` citato in XML doc) registrano servizi di sessione; `DemoSessionState` espone `IsDemo`, `CurrentBeat`, `DemoCompleted` e flag statico `StartNextSessionAsDemo` (`Assets/_Project/Scripts/Core/DemoSessionState.cs`).
- **Punti di forza:** separazione esplicita sessione demo; documentazione inline su bootstrap demo; accresciuto uso centralizzato di lock UI (`GameplayUiModalLock` nei DEV REPORT recenti).
- **Rischi / debito:** **212** occorrenze `ServiceContainer.Instance?.Get` aumentano accoppiamento globale; **103** `FindObjectOfType` violano la linea guida architetturale preferita (registry / serializzazione / `ServiceContainer`).
- **Raccomandazioni:** **P0** — congelare nuove occorrenze `FindObject*`; **P1** — mappare call site per migrazione a servizi o riferimenti serializzati; **P2** — valutare wrapper/facade per ridurre `Instance?.Get` ripetuti nello stesso flusso.

### Dome, vasi, giornata

- **Cosa fa:** `DomePotRegistry`, `PotActions`, `DayCycleController` orchestrano interazioni vaso e flusso giornaliero; metriche mostrano maggiore superficie di codice che referenzia registry/Cryo/Ph rispetto alla baseline.
- **Punti di forza:** registry e Cryo restano nodi condivisi; `AlwaysVisiblePotHUD` a **0** conferma rimozione HUD parallelo.
- **Rischi / debito:** `PotActions` e `DayCycleController` **crescono** in righe; complessità cumulativa su manutenzione e merge.
- **Raccomandazioni:** **P1** — estrarre sotto-moduli da `DayCycleController` (processor per fase giorno); **P2** — continuare decomposizione `PotActions` come da baseline.

### UI Toolkit (HUD, modali, icone)

- **Cosa fa:** Foundation HUD, terminali macchina, Seed Storage, Dispensa; risoluzione icone tramite `GlobalIconCatalog` / `GlobalIconResolver` (varianti categoria+variante, fallback `Resources` documentato in DEV_REPORT_0094).
- **Punti di forza:** parità modale (dim, lock), fix VO/HUD; pipeline icone più espressiva per inventario.
- **Rischi / debito:** `PlantCardV3TerminalController.cs` resta **6765** righe; `DomeStatusHUDController` è cresciuto a **1198** righe.
- **Raccomandazioni:** **P0** — nessun nuovo blocco monolitico in PlantCard senza estrazione; **P1** — spezzare per regione funzionale (missioni, inventario, tooltip) con partial class o servizi dedicati; **P2** — audit `Resources` fallback vs catalogo (coerenza performance — **DA VALIDARE IN EDITOR**).

### Save / demo / missioni

- **Cosa fa:** DEV_REPORT_0091/0093 citano `SaveManager`, missioni demo, `DemoStoryDirector`, `MissionManager`; inventario iniziale demo branchato in `GameManager` (0093).
- **Punti di forza:** allineamento piano “un solo binario” demo+full con flag `IsDemo`.
- **Rischi / debito:** logica narrativa e save devono restare sincronizzate ad ogni nuovo beat (complessità implicita, non quantificata qui).
- **Raccomandazioni:** **P1** — test manuali checklist per ogni beat dopo modifica a `DemoStoryDirector` o missioni; **P2** — test di caricamento save a metà demo.

---

## Code smell e anti-pattern

| Problema | Evidenza | Impatto | Azione suggerita |
|----------|----------|---------|-------------------|
| Scene discovery | 103 `FindObjectOfType` in 66 file | Accoppiamento a gerarchia scena, costo e fragilità | Migrare verso registry / DI già usata |
| God class | `PlantCardV3TerminalController.cs` **6765** righe | Merge conflict, regressione, onboarding lento | Estrazione moduli per area UI/logica |
| Crescita orchestratori | `DayCycleController` **2773** righe | Stesso effetto su area “giornata” | Processor o state machine esplicita |
| Service locator | `ServiceContainer.Instance?.Get` **212** | Test difficili, dipendenze implicite | Interfacce + punti di risoluzione unici per feature |

---

## Piano prioritizzato

1. **P0 — Stop the bleeding:** vietare nuove chiamate `FindObjectOfType` / `FindObjectsOfType` in codice gameplay (allineato a `.cursor/rules/architecture-runtime-services.mdc`).
2. **P1 — Riduzione hotspot:** piano incrementale sui file con più `FindObject*` (ordinare per file con `rg` e attaccare i top 5).
3. **P1 — PlantCard:** definire confini di estrazione (es. mission recap vs terminale vs inventario) e muovere blocchi in classi dedicate.
4. **P2 — Metriche CI:** opzionale script che fallisce la build se `FindObjectOfType` supera una soglia o aumenta in PR.

---

## Fuori scope / incertezze

- Comportamento runtime, frame time, allocazioni GC: **NON MISURABILE IN QUESTA SESSIONE (Profiler)**.
- Copertura test automatici, compilazione Unity completa, warning Editor: non eseguiti in questa sessione.
- Contenuto e priorità del **GDD Notion**: **NON VERIFICATO** senza export o accesso MCP autorizzato.

---

## Riferimenti file

- `Assets/Docs/REPORT/DEV_REPORT_0090_*.md` … `DEV_REPORT_0094_*.md`
- `Assets/Docs/ANALISI_TECNICA_E_COSA_PUO_FARE_IL_GIOCATORE_2026-03-19.md`
- `Assets/_Project/Scripts/Core/DemoSessionState.cs`
- `Assets/_Project/Scripts/Dome/PotActions.cs`
- `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`
- `Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs`
- `Assets/_Project/Scripts/UI/UIToolkit/DomeStatusHUD/DomeStatusHUDController.cs`
- `Assets/_Project/Scripts/UI/Icons/GlobalIconResolver.cs`
- `.cursor/plans/demo_alpha_1_0_gap_map.plan.md`
- `.cursor/rules/feature-both-demo-full-parity.mdc`
- `Assets/_Project/Scenes/SCN_VaultMap.unity`

---

## Progressi rispetto all’analisi tecnica precedente

**Baseline:** `Assets/Docs/ANALISI_TECNICA_E_COSA_PUO_FARE_IL_GIOCATORE_2026-03-19.md`, **data documento 2026-03-19**.

| Argomento / metrica | Baseline (2026-03-19) | Stato oggi (2026-04-26, evidenza sessione) | Esito |
|---------------------|----------------------|--------------------------------------------|--------|
| File `.cs` Scripts | 264 | **313** | Crescita codebase |
| `ServiceContainer.Instance?.Get` | 144 | **212** | Aumento uso locator |
| `FindObjectOfType` | 100 in 64 file | **103** in **66** file | Regressione numerica lieve |
| `FindObjectsOfType` | 39 in 21 file | **40** in **22** file | Invariato / lieve peggioramento |
| `AlwaysVisiblePotHUD` | 0 | **0** | Invariato (positivo) |
| `DomePotRegistry` (occorrenze / file) | 18 in 7 file | **28** in **11** file | Maggiore integrazione / superficie |
| `PlantCardV3TerminalController` righe | 7105 | **6765** | Progresso (riduzione) |
| `PotActions` righe | 1932 | **1950** | Lieve crescita |
| `DayCycleController` righe | 2722 | **2773** | Crescita |
| `DomeStatusHUDController` righe | 786 | **1198** | Crescita (nuove feature/fix HUD) |

**Sintesi (3–6 bullet):**

- **Migliorato:** UX modali e demo (documentato 0090–0094); **PlantCard** ridotto di centinaia di righe rispetto alla baseline numerica.
- **Peggiorato o stressato:** conteggio **`FindObject*`** e **`ServiceContainer.Instance?.Get`**; dimensioni **DayCycle**, **PotActions**, **DomeStatusHUD**.
- **Invariato:** assenza riferimenti **`AlwaysVisiblePotHUD`** negli script.
- **Aperto:** remediation sistematica **`FindObject*`**; **Profiler** per validare impatto UI e `Resources` su inventario.

---

## Performance e ottimizzazione (stato progetto)

**Cosa va bene (evidenza repo / statica):**

- Logging centralizzato via **`SporiumLogger`** (954 occorrenze) vs uso sporadico di `Debug.Log(` (6), coerente con strategia di logging strutturato.
- **`PlantCardV3TerminalController`** leggermente più piccolo che nella baseline (meno righe da parser/compilare; **beneficio reale runtime NON MISURABILE** qui).

**Cosa non va / debito (evidenza statica):**

- Presenza diffusa di **`FindObject*`** (oltre 140 match combinati con `FindObjectsByType`): rischio costi a runtime e dipendenza dalla scena.
- Classi molto grandi (**PlantCard** ancora >6k righe): rischio allocazioni e logiche per-frame difficili da auditare senza Profiler.

**Priorità interventi performance (P0 → P2):**

| Priorità | Intervento | Impatto atteso | Evidenza / metrica | Rischio / costo |
|----------|------------|----------------|---------------------|-----------------|
| P0 | Eliminare `FindObject*` dai hot path (Update / tick giornata) | Riduzione spike e GC da query scena | 103+40+11 pattern; **DA VALIDARE IN EDITOR** quali call site sono per-frame | Medio: richiede analisi call graph |
| P1 | Continuare modularizzazione **PlantCard** / **DayCycle** | Manutenzione e possibile riduzione lavoro per frame | 6765 / 2773 righe | Alto: refactor coordinato |
| P2 | Verificare fallback **`Resources.Load`** item vs catalogo | IO e cache sprite | DEV_REPORT_0094 + grep catalogo; **DA VALIDARE IN EDITOR** | Basso/Medio |

**Profiler Unity / frame budget / memoria runtime:** **NON MISURABILE IN QUESTA SESSIONE (Profiler).** Ogni affermazione su FPS o allocazioni in Play Mode resta **ipotesi da validare in Editor**.

---

## Status sviluppo: Demo e Full Game vs GDD (Notion)

**Fonte GDD:** **NON CONSULTATO** (nessun export Notion né lettura MCP in questa sessione).

**Allineamento progetto locale** (da `.cursor/plans/demo_alpha_1_0_gap_map.plan.md`, regola `feature-both-demo-full-parity.mdc`, `DemoSessionState`, scena `SCN_VaultMap`):

| Area / voce (artefatto locale) | Demo — stato | Full — stato | Evidenza repo / doc | Note |
|--------------------------------|--------------|--------------|---------------------|------|
| Un solo prodotto / scena | Previsto stesso build | Previsto stesso build | `feature-both-demo-full-parity.mdc` | Gating via flag sessione |
| Inventario iniziale | Lock 5 acqua + 2 cibo (report) | Starter completo (report) | DEV_REPORT_0093, `GameManager` citato | **NON** verificato eseguendo il gioco |
| Beat narrativi 1–8 | In evoluzione (piano) | N/A come scope separato | `demo_alpha_1_0_gap_map.plan.md` | Dettaglio beat = piano, non GDD |
| Bedroom PC desktop hub | Design / IA nel piano | Stesso principio Both | Sezioni 5.x piano gap map | Implementazione vs piano = **NON VERIFICATO** senza run |
| H / colazione / azioni | Specifica pre-flight lock 2026-04-18 nel piano | Stesso sistema evolutivo | Piano sez. 4 | Confronto implementazione codice = **NON VERIFICATO** in questa sessione |

**Gap noti (solo tracciabili da documenti letti):** il piano elenca molti workstream (desktop hub, tooltip Actions, ecc.); lo **stato di completamento** per ciascuno **non** è stato ricostruito con audit codice completo in questa sessione → **NON VERIFICATO** oltre ai DEV REPORT 0090–0094.

---

*Fine analisi tecnica 2026-04-26.*
