# Analisi tecnica — Sporae: stato repo, architettura e allineamento demo

**Data:** 2026-05-05  
**Scope:** working tree attuale `D:\Sporae_Build_Beta` — script e asset di progetto sotto `Assets/_Project`, ultimi DEV REPORT in `Assets/Docs/REPORT/`, piani e regole in `.cursor/`, scena `SCN_VaultMap` citata come ancoraggio prodotto. **Escluso:** esecuzione Unity / Profiler / test automatici / contenuto GDD su Notion salvo dichiarazione esplicita.  
**Repo / branch:** `main` (evidenza: `git rev-parse --abbrev-ref HEAD` → `main`).  
**Metodo:** analisi read-only nella sessione 2026-05-05; lettura markdown; conteggi con PowerShell `Get-ChildItem` + `Select-String -SimpleMatch` / `Get-Content | Measure-Object -Line` sui path indicati. Ogni cifra strutturale richiamata sotto ha fonte comando; niente numeri copiati da chat o da analisi storiche senza ricalcolo.

---

## Allineamento agli ultimi sviluppi (DEV REPORT)

Ordinamento **NNNN** numerico **decrescente** su `DEV_REPORT_NNNN_*.md` in `Assets/Docs/REPORT/` (enumerazione file + ordinamento manuale su prefisso — primi cinque: **0108, 0107, 0106, 0105, 0104**). **Letti per intero o in sezioni sostanziali** nella sessione:

| File | Titolo H1 (dal documento) |
|------|---------------------------|
| `DEV_REPORT_0108_PLANTCARD4V_VO_BIOLOGO_REAZIONI_PH_AMBIENT_2026-05-05.md` | DEV REPORT 0108 — PlantCard4v: VO reazioni post-intervento e allineamento pH ambiente |
| `DEV_REPORT_0107_PLANTCARD4V_CARE_VIEW_UI_LOGIC_2026-05-05.md` | DEV REPORT 0107 — PlantCard4v care view, layout reference e logica interventi |
| `DEV_REPORT_0106_HUD_TOOLTIP_DEMO_FLOW_POLISH_2026-05-05.md` | DEV REPORT 0106 — HUD tooltip, DomeStatusHUD compatto, mission recap e piano demo choice-driven |
| `DEV_REPORT_0105_DEMO_BEAT3_MISSIONI_VO_LAYERING_2026-05-03.md` | DEV REPORT 0105 — Demo beat 3: missioni Resources, fix auto-completamento Seed Storage, layering VO vs PC camera |
| `DEV_REPORT_0104_BEDROOM_PC_ESC_MODALI_COSTI_2026-05-03.md` | DEV REPORT 0104 — PC Bedroom UI Toolkit, priorità Esc modali/menu, costi Food Room e wiring Vault |

**Bullet rilevanti per lo scope (sintesi dai report):**

- **PlantCard4v (0107–0108):** nuova card cura POT in UI Toolkit (UXML/USS), integrazione con `PotStateModel`, `PotActions`, `PhSystem`, VO in-card, modale con top/bottom bar visibili; reazioni VO post-azione (biologo satirico); correzione binding **Ambiente pH** su `PhSystem.CurrentPh` allineato alla TopBar; script `PlantCard4vBiologistReactionVo.cs` e wiring view model/controller.
- **HUD / tooltip / demo polish (0106):** piano post–Beat 3 **choice-driven** nel gap map; VO Overlay e Mission Recap più leggibili; tooltip toast legato alla vita del toast; avviso sul quadratino azioni quando si rischia −1 azione per fame; DomeStatusHUD **compatto all’ingresso** e apertura su linguetta o su passaggio POT vuoto→piantato; conferma consumo **acqua sporca**.
- **Beat 3 / missioni (0105):** missioni demo PC sotto `Assets/Resources/Missions/`; `DemoStoryDirector` con `Resources.Load` + fallback `LoadAll`; **`MissionChecker.Check()`** difensivo (niente auto-completamento su obiettivi vuoti); guard su completamento Seed Storage; layering VO vs Bedroom PC tramite `sortingOrder` e `PanelSettings` differenziati.
- **Bedroom PC / modali (0104):** terminale PC camera in UI Toolkit, `GameplayUiModalLock`, guard Esc (`BlocksWorldInput`) per evitare doppio consumo con menu; API `FoodRoomSystem` / costanti `SeedStorageSystem` per UI costi.

**Contraddizioni DEV REPORT vs repo (questa sessione):** nessuna contraddizione *strutturale* rilevata sui file citati; i **conteggi righe** nei report 0107/0108 sono **istantanee d’iterazione** e possono **differire** dalla working tree odierna (vedi metriche hotspot: `PlantCard4vController.cs` misurato oggi a **1019** righe — comando in sezione Metodologia). Eventuali regressioni runtime restano **DA VALIDARE IN EDITOR**.

**In sintesi per lettore non tecnico:** nelle ultime consegne il team ha reso più **curata e leggibile** l’esperienza intorno alla cupola (nuova scheda pianta, messaggi dopo le azioni, numeri pH coerenti con la barra principale) e ha **affinato** interfaccia e demo (tooltip che non “restano appesi”, cupola meno invadente all’avvio, filo narrativo che dopo il terzo fatto prepara **scelte** guidate). In parallelo ha **sistemato** bug strutturali del percorso demo verso il PC e le missioni, così il giocatore non dovrebbe più vedere obiettivi che si chiudono da soli o testi coperti dai pannelli. Questo lavoro è **documentato** nei report; **se** qualcosa non si comporta così in gioco, va verificato in Play, non dedotto dal solo testo.

---

## Executive summary

- Il codebase sotto `Assets/_Project/Scripts` è **cresciuto** (**328** file `.cs`, **82 996** righe totali sommate con `Measure-Object -Line`) rispetto alla fotografia del **2026-04-26** (**313** file, **73 565** righe nella baseline documentata allora).
- L’uso di **`ServiceContainer.Instance?.Get`** è **salito** da **212** a **250** occorrenze (stesso pattern di ricerca sulla cartella `Assets/_Project`).
- **`FindObjectOfType`** in `Assets/_Project/Scripts` risulta **108** occorrenze in **68** file (baseline 26/04: **103** in **66** file): **incremento numerico**, in tensione con la regola architetturale che ne scoraggia l’uso in gameplay.
- **PlantCardV3** resta un **monolite** (~**6764** righe); è comparsa l’area **PlantCard4v** (controller ~**1019** righe) senza ancora “alleggerire” il vecchio terminale nel dato metrico.
- **`DomeStatusHUDController`** cresce (**1268** righe vs **1198** nella baseline 26/04), coerente con i report su HUD compatto e trigger di apertura.
- **Performance runtime** e **Profiler**: **NON MISURABILE IN QUESTA SESSIONE**; solo ipotesi e priorità **DA VALIDARE IN EDITOR**.
- **GDD Notion:** **NON CONSULTATO**; stato Demo/Full ricostruibile solo da **piano locale**, regole Cursor e codice flag (`DemoSessionState`, `DemoStoryDirector`, ecc.).

**In sintesi per lettore non tecnico:** il prodotto sta **accumulando valore giocabile** visibile (cura piante, demo, PC in camera, meno attriti UI), ma il **peso del codice** e alcune abitudini tecniche (ricerca oggetti in scena, servizi globali molto usati, file molto lunghi) **non** stanno migliorando da soli: servono **cure pianificate** tra miglioramenti per il giocatore e sanezza interna per il team.

---

## Statistiche e contesto progress (gameplay / prodotto)

### Righe di codice

| Voce | Valore | Come misurato |
|------|--------|----------------|
| File `.cs` in `Assets/_Project/Scripts` | **328** | `Get-ChildItem -Path "Assets\_Project\Scripts" -Recurse -Filter *.cs -File` → `.Count` |
| Righe totali (somma righe fisiche su quei file) | **82 996** | Per ogni file: `Get-Content -LiteralPath` + `Measure-Object -Line` → somma in PowerShell |
| File `.cs` in tutto `Assets/_Project` (incl. Editor sotto `_Project`) | **344** | `Get-ChildItem -Path "Assets\_Project" -Recurse -Filter *.cs -File` → `.Count` |

### Sistemi funzionanti

Macro-aree **presenti nel repo e descritte come operative / da validare** negli ultimi cinque DEV REPORT letti; **validazione Play completa** = **NON MISURABILE IN QUESTA SESSIONE** (solo **DA VALIDARE IN EDITOR**):

- **PlantCard4v** — cura POT, VO, pH ambiente, modale.
- **HUD Foundation** — TopBar, azioni, tooltip toast, Mission Recap, DomeStatusHUD compatto.
- **Demo beat 3** — missioni Resources, Director, layering VO vs PC.
- **Bedroom PC** — UI Toolkit, lock modali, Esc.
- Orchestrazione **demo** — `DemoSessionState`, `DemoStoryDirector`, piano choice-driven nel gap map.

### Bug risolti

Non esiste un issue tracker aggregato obbligatorio nel repo per un conteggio globale. Nella **finestra DEV 0104–0108** i report narrano numeri **locali** (es. **11** UX in 0107, **7** in 0106, **3** in 0105, **1** in 0104 per temi specifici). **Totale progetto “bug risolti”:** **non quantificabile qui** senza altra fonte.

### Progresso gameplay / prodotto (linguaggio chiaro)

- Scheda **cura pianta** più **visiva** e con **feedback** immediato dopo irrigazione, luce, fertilizzante, ecc.
- **Interfaccia cupola** meno **ingombrante** all’inizio e più **logica** quando succede qualcosa di rilevante (es. nuova pianta).
- **Demo** con meno **trabocchetti** (missioni che si completano da sole, VO coperto dal PC).
- **PC in camera** e **scorciatoie tasto** più **prevedibili** (Esc non fa due cose insieme).
- **Messaggi e numeri** (pH) più **allineati** a ciò che il giocatore vede altrove nell’HUD.

**In sintesi per lettore non tecnico:** i numeri dicono che il team sta **scriendo più codice** nel perimetro progetto, e i report dicono che buona parte va nella **direzione “si capisce meglio giocando”**. Quello che **non** possiamo affermare da qui è se tutto sia **stabile** su ogni dispositivo o scenario: per quello serve **prova in Unity**, non solo lettura file.

---

## Metodologia e evidenze

Comandi PowerShell eseguiti con directory di lavoro `D:\Sporae_Build_Beta` (salvo ove indicato):

1. `git rev-parse --abbrev-ref HEAD` → `main`.
2. Conteggio file `.cs`: `Get-ChildItem -Recurse -Filter *.cs -File` su `Assets\_Project\Scripts` (**328**) e su `Assets\_Project` (**344**).
3. Somma righe: loop `Get-Content -LiteralPath` + `Measure-Object -Line` sui **328** file in `Scripts` → **82 996**.
4. Pattern su tutti i `.cs` sotto `Assets\_Project`:  
   `Get-ChildItem -Recurse -Filter *.cs | Select-String -SimpleMatch '<pattern>'` → `.Count` per occorrenze;  
   per file distinti con match: `Where-Object { Select-String -Path $_.FullName -SimpleMatch '<pattern>' -Quiet }` → `.Count`.
5. Righe file singoli: `Get-Content -LiteralPath <path> | Measure-Object -Line` → `.Lines` per `PotActions.cs`, `SPOR-BLK-01-03A-DayCycleController.cs`, `PlantCardV3TerminalController.cs`, `DomeStatusHUDController.cs`, `PlantCard4vController.cs`.
6. Asset UI: `Get-ChildItem -Path Assets\_Project -Recurse -Filter *.uxml -File` → **33**; `*.uss` → **38**.
7. Letti: cinque DEV REPORT elencati in Fase A; baseline `Assets/Docs/ANALISI_TECNICA_SPORAE_STATO_REPO_2026-04-26.md`; estratto `.cursor/plans/demo_alpha_1_0_gap_map.plan.md`; `DemoSessionState.cs`, `ServiceContainer.cs`, incipit `SaveManager.cs`.

**In sintesi per lettore non tecnico:** abbiamo preferito **comandi ripetibili** sul disco invece di “stime d’occhio”: così chi rilegge tra un mese può **rifare gli stessi passi** e capire se il trend è migliorato o peggiorato, senza fidarsi di ricordi o di vecchie analisi.

---

## Metriche (tabelle)

| Metrica | Valore | Come misurato | Note |
|---------|--------|---------------|------|
| File `.cs` in `Assets/_Project/Scripts` | **328** | `Get-ChildItem` ricorsitivo | Baseline 2026-04-26: **313** |
| Righe totali `.cs` in `Assets/_Project/Scripts` | **82 996** | Somma `Measure-Object -Line` | Baseline 26/04: **73 565** |
| File `.cs` in tutto `Assets/_Project` | **344** | `Get-ChildItem` su `_Project` | Include script sotto `Editor` in `_Project` |
| `ServiceContainer.Instance?.Get` (occorrenze) | **250** | `Select-String -SimpleMatch` su `Assets\_Project` `*.cs` | Baseline 26/04: **212** |
| `ServiceContainer.Instance` (qualsiasi) | **520** | idem | Ordine di grandezza uso locator |
| `FindObjectOfType` (occorrenze) | **119** | idem su `Assets\_Project` | In `Scripts` soltanto: **108** (vedi confronto con baseline che usava `Scripts`) |
| File con `FindObjectOfType` | **71** | `Where-Object … -Quiet` su `_Project` | In `Scripts`: **68** file |
| `FindObjectsOfType` (occorrenze) | **41** | `Select-String` | Baseline 26/04: **40** |
| `FindObjectsByType` (occorrenze) | **11** | `Select-String` | Baseline 26/04: **11** |
| `AlwaysVisiblePotHUD` | **0** | `Select-String` | Nessun match negli script misurati |
| `DomePotRegistry` | **33** occorrenze in **13** file | `Select-String` + `Where-Object` | Baseline 26/04: **28** / **11** |
| `PhSystem` (sottostringa) | **574** occorrenze | `Select-String -SimpleMatch 'PhSystem'` | Baseline 26/04: **506** — metodo identico non garantito; etichetta: **confronto indicativo** |
| `SporiumLogger.` | **963** | `Select-String` | Baseline 26/04: **954** |
| `Debug.Log(` | **73** | `Select-String -SimpleMatch 'Debug.Log('` | Baseline 26/04: **6** — verificare in code review se molti match sono **nuovi** o **commenti/stringhe**; il dato grezzo resta **73** |
| File `*.uxml` in `Assets/_Project` | **33** | `Get-ChildItem -Filter *.uxml` | Presenza massiccia UI Toolkit |
| File `*.uss` in `Assets/_Project` | **38** | `Get-ChildItem -Filter *.uss` | — |
| `PotActions.cs` — righe | **1938** | `Measure-Object -Line` | Baseline 26/04: **1950** |
| `SPOR-BLK-01-03A-DayCycleController.cs` — righe | **2726** | idem | Baseline 26/04: **2773** |
| `PlantCardV3TerminalController.cs` — righe | **6764** | idem | Baseline 26/04: **6765** |
| `DomeStatusHUDController.cs` — righe | **1268** | idem | Baseline 26/04: **1198** |
| `PlantCard4vController.cs` — righe | **1019** | idem | Nuovo hotspot per area PlantCard4v |

**In sintesi per lettore non tecnico:** il progetto ha **più righe** e **più punti** che toccano cupola, pH e servizi; parallelamente ci sono **più** punti che ancora **cercano oggetti nella scena** invece di usare col**legamenti espliciti**. Non è un giudizio morale sul codice: è un **termometro** — più rosso sui `FindObject*`, più attenzione serve nelle prossime milestone.

---

## Architettura e sistemi

### Core e servizi

- **Cosa fa:** `ServiceContainer` (`Assets/_Project/Scripts/Core/ServiceLocator/ServiceContainer.cs`) mantiene dizionario servizi per scena/globale; `DemoSessionState` (`Assets/_Project/Scripts/Core/DemoSessionState.cs`) espone `IsDemo`, `CurrentBeat`, `DemoCompleted`, `StartNextSessionAsDemo`, evento `BeatChanged`.
- **Punti di forza:** stato demo **centralizzato**; allineamento ai report su **un solo binario** demo+full; installer e convenzioni `GamePlayInstaller` citate nei DEV REPORT e nella documentazione inline di `DemoSessionState`.
- **Rischi / debito:** **250** occorrenze `ServiceContainer.Instance?.Get` → accoppiamento globale; **`FindObjectOfType`** in crescita nel sottoalbero `Scripts` (**108** vs **103** baseline 26/04).
- **Raccomandazioni:** **P0** — congelare nuovi `FindObject*` in gameplay; **P1** — mappare i call site più frequenti per migrare a registry / serializzazione; **P2** — dove possibile, ridurre chiamate ripetute al locator con entry-point per feature.

**In sintesi per lettore non tecnico:** il “centralino” dei servizi **funziona** per tenere insieme demo e partita normale, ma se tutti lo chiamano **continuamente** il rischio è di avere un gioco **difficile da testare e da modificare** senza effetti a catena. Convienne **puntare** le dipendenze dove servono, invece di chiederle al buio ogni volta.

---

### Cupola, vasi e ciclo giornata

- **Cosa fa:** `DomePotRegistry` (**33** riferimenti in **13** file) e orchestratori **`PotActions`**, **`SPOR-BLK-01-03A-DayCycleController`** gestiscono azioni sui vasi e fasi giornate; regole progetto (`gameplay-runtime-patterns.mdc`) spingono verso validator/processor invece di incollare logica nei facciate.
- **Punti di forza:** registry esplicito per i POT; volumi di codice su `DayCycleController` e `PotActions` **leggermente ridotti** in righe rispetto alla baseline 26/04 (vedi tabella metriche).
- **Rischi / debito:** file ancora **molto grandi**; accoppiamento scena non risolto solo dal registry se **`FindObject*`** resta diffuso altrove.
- **Raccomandazioni:** **P1** — continuare estrazioni da `DayCycleController` citate nei pattern interni; **P2** — verificare in Play che notifiche cupola e automazioni non facciano lavoro ridondante ogni frame (**DA VALIDARE IN EDITOR**).

**In sintesi per lettore non tecnico:** la cupola è il **cuore** del fantasy di giardinaggio; tecnicamente avete già un **registro dei vasi**, che è la strada giusta, ma i **grandi “quaderni” di codice** che orchestrano giornata e azioni restano un **punto delicato** per chi deve aggiungere una feature senza rompere le vecchie.

---

### UI Toolkit (HUD, PlantCard3/4, modali, notifiche)

- **Cosa fa:** **33** UXML e **38** USS sotto `Assets/_Project`; controller tipo `DomeStatusHUDController`, `TopBarController`, pannelli Lab/Food/Seed Storage, **PlantCardV3** e nuova **PlantCard4v** (report 0107–0108); `VoOverlayController` e Foundation notifications nei report recenti.
- **Punti di forza:** coerenza con regola **parità UI Builder** citata nei DEV REPORT (rimozione `style=""` su PlantCard4v in 0107); miglioramenti **tooltip** e **layering** documentati (0105–0106).
- **Rischi / debito:** `PlantCardV3TerminalController.cs` ~**6764** righe; `PlantCard4vController.cs` già ~**1000+** righe — rischio di **duplicazione** concettuale o manutenzione doppia finché entrambi convivono; `DomeStatusHUDController` in crescita.
- **Raccomandazioni:** **P0** — nessuna nuova “mega-classe” senza boundary; **P1** — roadmap **chiara** V3 vs V4 (chi viene usato dove in `SCN_VaultMap`, per quanto tempo); **P2** — audit performance UI (**DA VALIDARE IN EDITOR** con UI Profiler).

**In sintesi per lettore non tecnico:** dal punto di vista del giocatore, l’interfaccia sta diventando **più curata** (meno sovrapposizioni, più feedback); dal punto di vista interno, avete **tanto** codice in pochi file “giganti”, che rendono **costoso** ogni piccolo ritocco visivo o logico se non è ben incapsulato.

---

### Salvataggio, missioni e demo narrativa

- **Cosa fa:** `SaveManager` (`Assets/_Project/Scripts/Core/SaveManager.cs`) — salvataggio JSON/multi-slot documentato altrove; integrazione con `ServiceContainer`; `DemoStoryDirector` e missioni sotto `Resources` (0105); `MissionChecker` reso più difensivo.
- **Punti di forza:** fix a **missioni fantasma** e path **Resources** consolidati riducono disorientamento in demo; flag **`IsDemo`** e stato beat in `DemoSessionState`.
- **Rischi / debito:** narrativa **choice-driven** post–Beat 3 richiede nuove superfici UI e persistenza **solo demo** — superficie d’errore alta se non separata nettamente dai salvataggi full (come da piano gap map).
- **Raccomandazioni:** **P1** — checklist salvataggio a metà demo dopo ogni modifica a flag o Director; **P2** — test regression su “Nuova partita” non contaminata da contenuti demo-only.

**In sintesi per lettore non tecnico:** il gioco deve **ricordare** la partita e, in demo, **ricordare le scelte** senza “sporcare” la campagna completa. I report mostrano **attenzione** a questo problema; la prova del nove resta **giocare davvero** salvataggio/ricarico su entrambe le modalità.

---

## Code smell e anti-pattern

| Problema | Evidenza | Impatto | Azione suggerita |
|----------|----------|---------|-------------------|
| Scene discovery | `FindObjectOfType`: **108** occorrenze (**68** file) in `Scripts`; **119** in **71** file in tutto `_Project` | Fragilità scena, costo potenziale a runtime | Migrare verso registry / riferimenti serializzati / `ServiceContainer` |
| God class UI | `PlantCardV3TerminalController` **6764** righe; `PlantCard4vController` **1019** righe | Merge, regressioni, onboarding lenti | Estrarre moduli per area (tooltips, missioni, binding) |
| Service locator | **250** `ServiceContainer.Instance?.Get` | Test e accoppiamento | Interfacce + composizione feature-level |
| Possibile rumore logging | **73** `Debug.Log(` vs **6** in baseline 26/04 | Log non strutturati in build | Audit manuale: mantenere `SporiumLogger` come percorso principale |

**In sintesi per lettore non tecnico:** i “spigoli” principali sono **file troppo lunghi** e **troppa dipendenza dal “trovami qualcosa nella stanza”**. Questo si traduce in **tempo perso** quando si cambia una scena o un prefab, e talvolta in **micro-scatti** se qualcosa cerca oggetti nel momento sbagliato — da **confermare** con Profiler.

---

## Piano prioritizzato

1. **P0 — Stop alle nuove ricerche scena:** nessun nuovo `FindObjectOfType` / `FindObjectsOfType` in gameplay (allineato a `.cursor/rules/architecture-runtime-services.mdc`).
2. **P1 — PlantCard:** strategia esplicita V3 vs V4 + confini moduli per non duplicare la complessità.
3. **P1 — Demo post–Beat 3:** implementare pattern “VO choice” con salvataggio **solo demo** e QA su beat incrociati.
4. **P2 — Audit `Debug.Log`:** capire l’aumento **73** vs **6** (nuovo codice vs `Select-String` che colpisce stringhe non eseguibili) e riallineare a logging strutturato.

**In sintesi per lettore non tecnico:** le priorità dicono **prima** *non peggiorare l’ossatura*, **poi** scegliere con chiarezza **quale interfaccia pianta** portare avanti, **poi** aprire il capitolo delle **scelte narrative** con calma e test, **infine** ripulire i log — è un ordine di **investimento** che protegge sia la demo sia il lungo periodo.

---

## Fuori scope / incertezze

- **Profiler Unity**, **frame budget**, **GC**: non misurati; **DA VALIDARE IN EDITOR**.
- **Test automatizzati** / **CI Unity**: non eseguiti in questa sessione.
- **Comportamento ogni prefab** in `SCN_VaultMap.unity`: ispezione YAML **non** eseguita in profondità — **NON VERIFICATO** oltre ai riferimenti nei DEV REPORT.
- **Confronto punto-per-punto** con GDD Notion: **NON VERIFICATO** (Notion non consultato).

**In sintesi per lettore non tecnico:** questo documento è una **fotografia statica** del repo e della documentazione interna; non sostituisce **ore di playtest** né la **lettura del game design** ufficiale su Notion. Le zone grigie sono dichiarate apposta, per non **sparare sentenze** senza prove.

---

## Riferimenti file

Elenco **non esaustivo** ma navigabile: DEV REPORT **0104–0108**; baseline **ANALISI_TECNICA_SPORAE_STATO_REPO_2026-04-26.md**; **demo_alpha_1_0_gap_map.plan.md**; **DemoSessionState.cs**, **ServiceContainer.cs**, **SaveManager.cs**; hotspot **PotActions.cs**, **SPOR-BLK-01-03A-DayCycleController.cs**, **PlantCardV3TerminalController.cs**, **PlantCard4vController.cs**, **DomeStatusHUDController.cs**; cartelle **Assets/_Project/UI/UIToolkit/** e **Assets/_Project/Scripts/UI/UIToolkit/**.

Questi file sono i **ancoraggi** che collegano i numeri di questa analisi a ciò che il team sta effettivamente toccando (HUD, demo, cupola, salvataggio).

---

## Progressi rispetto all’analisi tecnica precedente

**Baseline:** `Assets/Docs/ANALISI_TECNICA_SPORAE_STATO_REPO_2026-04-26.md`, **data documento 2026-04-26** (scelta come **più recente** con data **strettamente precedente** a **2026-05-05** tra `ANALISI_TECNICA*.md` in `Assets/Docs/` e root).  
*Nota:* esistono anche `ANALISI_TECNICA_COMPLETA_SPORIUM.md` e varianti datate in root — **non** usate come baseline per la tabella seguente, per coerenza con la skill (precedente immediata alla data corrente).

| Argomento / metrica | Come era nella baseline (sintesi) | Stato oggi (evidenza fresca) | Esito |
|---------------------|-----------------------------------|------------------------------|--------|
| File `.cs` in `Scripts` | **313** | **328** (`Get-ChildItem`) | Crescita |
| Righe totali `Scripts` | **73 565** | **82 996** (somma `Measure-Object`) | Crescita |
| `ServiceContainer.Instance?.Get` | **212** | **250** (`Select-String` su `Assets\_Project`) | Aumento |
| `FindObjectOfType` in `Scripts` | **103** in **66** file | **108** in **68** file | Regressione numerica lieve |
| `FindObjectsOfType` | **40** | **41** (su `_Project`) | Invariato / lieve peggioramento |
| `AlwaysVisiblePotHUD` | **0** | **0** | Invariato (positivo) |
| `DomePotRegistry` (occorrenze / file) | **28** / **11** | **33** / **13** | Maggiore superficie |
| `PlantCardV3TerminalController` righe | **6765** | **6764** | Sostanzialmente invariato |
| `PotActions` righe | **1950** | **1938** | Lieve miglioramento |
| `DayCycleController` righe | **2773** | **2726** | Lieve miglioramento |
| `DomeStatusHUDController` righe | **1198** | **1268** | Crescita (feature HUD) |
| Nuova area PlantCard4v | Non trattata come file dedicato | `PlantCard4vController.cs` **1019** righe | Nuovo hotspot / prodotto |
| `SporiumLogger.` | **954** | **963** | Lieve aumento (uso logging strutturato) |
| `Debug.Log(` | **6** | **73** | Cambiamento forte — **verificare** contesto |

**Sintesi (bullet):**

- **Migliorato (documentato + metriche):** flussi **demo/missioni/PC** (0104–0105); **HUD/tooltip/entry cupola** (0106); **PlantCard4v** e fix **pH** (0107–0108); **DayCycle** e **PotActions** leggermente **più corti** in righe.
- **Peggiorato o stressato:** **`FindObject*`** e **`ServiceContainer.Instance?.Get`**; **`DomeStatusHUD`** più grande; **`Debug.Log`** in forte crescita nel match testuale — **da qualificare** con revisione umana.
- **Invariato nel segno positivo:** **0** `AlwaysVisiblePotHUD`.
- **Aperto:** Profiler; strategia **PlantCard V3 vs V4**; chiusura narrativa **choice-driven** senza contaminare full save.

**In sintesi per lettore non tecnico:** rispetto a fine aprile, il team ha **messo dentro** soprattutto **esperienza giocabile** (demo, schermate, cupola); i **numeri tecnici** dicono però che anche il **peso** del codice e alcune **abitudini rischiose** stanno **salendo**. Il giudizio complessivo non è “tutto male” o “tutto bene”: è **miglioramento del prodotto visibile** con **debito strutturale da gestire** nei prossimi cicli.

---

## Performance e ottimizzazione (stato progetto)

**Cosa va bene (solo evidenza statica questa sessione):**

- Logging strutturato ancora dominante: **`SporiumLogger.`** = **963** occorrenze vs **`Debug.Log(`** = **73** (quest’ultimo da **qualificare** — vedi code smell).
- **`PlantCardV3`** in righe **stabile** (~6764), **`DayCycle`** e **`PotActions`** leggermente **più corti**: indizio di **piccola** detassazione locale (beneficio runtime **NON MISURABILE** senza Profiler).

**Cosa non va / debito (evidenza statica):**

- **119** occorrenze combinate `FindObjectOfType` sotto `_Project` (**108** in `Scripts`) + **41** `FindObjectsOfType` + **11** `FindObjectsByType` → rischio costi e spike **DA VALIDARE IN EDITOR** su call graph.
- Classi **molto grandi** (PlantCardV3, HUD) → rischio lavoro per frame o allocations se logica pesante resta in `Update`/refresh UI **DA VALIDARE IN EDITOR**.

**Priorità interventi performance (P0 → P2):**

| Priorità | Intervento | Impatto atteso | Evidenza / metrica | Rischio / costo |
|----------|------------|----------------|---------------------|------------------|
| P0 | Eliminare / isolare `FindObject*` dai hot path (Update, tick giornata, refresh continuo UI) | Riduzione spike e query scena | **108**+ in `Scripts`; **DA VALIDARE IN EDITOR** quali call site sono caldi | Medio: analisi + refactor |
| P1 | Modularizzare **PlantCard** (V3 e/o V4) e ridurre refresh ridondanti | CPU/GC UI meno stressati | **6764** + **1019** righe controller | Alto: coordinamento design |
| P2 | Profilare lista inventario / icone / `Resources` vs catalogo | IO e memoria sprite | Report storici icone + **DA VALIDARE IN EDITOR** | Medio |

**Profiler Unity / frame budget / memoria runtime:** **NON MISURABILE IN QUESTA SESSIONE (Profiler).** Qualsiasi affermazione su FPS o allocazioni in Play resta **ipotesi DA VALIDARE IN EDITOR**.

**In sintesi per lettore non tecnico:** senza misure Unity non possiamo dire se il gioco **scatta** o meno; possiamo dire che il codice contiene **abitudini** che *spesso* fanno scattare i giochi se esagerano. Il team dovrebbe **pianificare** una sessione Profiler **prima** di dichiarare chiusa una milestone pesante su UI o cupola.

---

## Status sviluppo: Demo e Full Game vs GDD (Notion)

**Fonte GDD:** **NON CONSULTATO** (nessun export Notion né lettura MCP Notion in questa sessione).

**Allineamento da artefatti repo** (non sostituisce il GDD): `.cursor/plans/demo_alpha_1_0_gap_map.plan.md` (Principio 0 — `SCN_VaultMap` unico; `DemoSessionState` + `DemoStoryDirector`; traccia 9 milestone; dopo 2026-05-04 struttura **choice-driven** post–Beat 3; contenuti **Cetriolo d’Oro** / **Il Piacere Dimenticato** demo-only), `.cursor/rules/feature-both-demo-full-parity.mdc`, file `DemoSessionState.cs`, riferimenti scena nei DEV REPORT.

| Voce (dal piano / regole locali) | **Demo — stato (repo-only)** | **Full — stato (repo-only)** | Evidenza | Note |
|----------------------------------|------------------------------|------------------------------|----------|------|
| Un solo binario / scena | Previsto | Stesso | `feature-both-demo-full-parity.mdc`, gap map | **NON VERIFICATO** come build release |
| Beat 1–3 | Documentati come chiusi su main (piano agg. 2026-05-03) | Stesso codice, gating demo | Gap map header + DEV 0103–0105 | **NON VERIFICATO** senza playtest sistema |
| Beat 4+ choice-driven | **Piano** e report 0106 | N/A come percorso parallelo | Gap map §2.1, DEV 0106 | Implementazione completa vs piano = **NON VERIFICATO** |
| VO + layering PC | Fix documentati | Both | DEV 0105–0106 | **DA VALIDARE IN EDITOR** |
| PlantCard4v / cura POT | Implementazione recente | Both | DEV 0107–0108, codice | **DA VALIDARE IN EDITOR** |
| Contenuti demo-only (Cetriolo, ecc.) | **Richiesto** con flag sessione | Non deve contaminare full | Gap map 2026-05-04 | **NON VERIFICATO** con audit save/full |

**Gap noti:** confronto punti del GDD Notion **NON VERIFICATO**; la tabella riflette **solo** testo di piano e DEV REPORT.

**In sintesi per lettore non tecnico:** il **documento di design ufficiale** su Notion **non è stato** consultato qui: quindi **non** sappiamo da questa sessione se il gioco è “**allineato al GDD**” al cento per cento. Sappiamo che **in casa** avete uno **piano demo** molto dettagliato e codice/storie che **sembrano** seguirlo, ma la **parola finale** spetta a **Notion + playtest**, non a questo file.

---

*Fine analisi tecnica 2026-05-05.*
