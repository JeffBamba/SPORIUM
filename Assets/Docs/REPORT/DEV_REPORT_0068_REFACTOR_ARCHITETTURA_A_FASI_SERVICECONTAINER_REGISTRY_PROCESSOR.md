# DEV REPORT 0068 — Refactor architettura a fasi: ServiceContainer, registry, validator/processor, rules Cursor

**Data:** 2026-03-18  
**Oggetto:** Esecuzione del piano di refactor architetturale a fasi per ridurre dipendenze implicite (`FindObjectOfType` / `FindObjectsOfType`), introdurre registry di scena, spostare la logica verso validator/processor mantenendo API pubbliche stabili, e fissare regole Cursor per le future funzionalità.  
**Riferimenti:** `AppRoot.cs`, `GamePlayInstaller.cs`, `ElevatorSystem.cs`, `PlantCardV3TerminalController.cs`, `SPOR-BLK-01-03A-DayCycleController.cs`, `PotActions.cs`, `DomePotRegistry.cs`, `PotActionValidator.cs`, `PotWateringProcessor.cs`, `CondensationDayProcessor.cs`, regole in `.cursor/rules/`.  
**Report precedente:** `Assets/Docs/REPORT/DEV_REPORT_0067_ANALISI_TECNICA_PLAYER_CAPABILITIES_E_CORREZIONE_STORYTELLING.md`

---

## 1. Contesto

- Dall’analisi tecnica era emersa una priorità alta su tre temi:
  1. ridurre `FindObjectOfType` / `FindObjectsOfType`,
  2. evitare lookup ripetuti nei path runtime,
  3. iniziare a spezzare le god class senza rompere la build.
- È stato concordato un approccio **incrementale**:
  - prima stabilizzare dipendenze e bootstrap,
  - poi introdurre un registry di scena per i vasi,
  - poi fare un primo split strutturale verso validator/processor,
  - infine fissare il tutto con regole Cursor.
- Vincolo fondamentale: **non rompere i flussi gameplay esistenti** e mantenere stabili le API pubbliche critiche (`PotActions`, `DayCycleController`, terminale, elevator).

---

## 2. Lavoro svolto

### 2.1 Fase 1 — Stabilizzazione dipendenze e bootstrap

- **`AppRoot.cs`**
  - `GetGameManager()` ora preferisce `ServiceContainer` prima del fallback legacy.
  - `GetSystem<T>()` e `GetAllSystems<T>()` provano prima a risolvere il servizio dal `ServiceContainer`.
  - Il fallback `FindObjectOfType` è stato mantenuto solo come compatibilità temporanea.

- **`ElevatorSystem.cs`**
  - `GameManager`, `PlayerClickMover2D`, `PlayerInteractAdvice`, `UINotification` vengono risolti via `ServiceContainer` con late binding su `OnServiceRegistered`.
  - Aggiunto cleanup centralizzato delle sottoscrizioni.
  - `Camera.main` viene ora preferita e cached, con fallback controllato solo se necessario.

- **`PlantCardV3TerminalController.cs`**
  - Risoluzione runtime per `GameManager`, `DayCycleSystem`, `PhSystem`, `PlayerClickMover2D`, `PlayerPerspectiveMover2D`, `PlayerMoverRouter2D`, `PotAutomationRunner`.
  - Aggiunto late binding tramite `OnServiceRegistered`.
  - Mantenuti fallback `FindObjectOfType` solo come rete di sicurezza per scene non ancora migrate completamente.

- **`SPOR-BLK-01-03A-DayCycleController.cs`**
  - Registrato nel `ServiceContainer`.
  - Rimosso l’uso di `FindObjectOfType<PotSystemConfig>()`; ora usa solo `Resources.Load` / `Resources.LoadAll`.
  - `UINotification` viene recuperato solo via `ServiceContainer` + late binding.

- **Registrazione servizi runtime**
  - `PlayerClickMover2D`, `PlayerPerspectiveMover2D`, `PlayerMoverRouter2D`, `PlayerInteractAdvice`, `PotAutomationRunner` sono stati registrati nel `ServiceContainer` nel loro `Awake`.

### 2.2 Fase 2 — Registry di scena per i vasi

- Creato **`DomePotRegistry.cs`**:
  - tiene la lista dei `PotSlot`,
  - tiene la lista dei `PotGrowthController`,
  - espone snapshot ordinati e lookup per `PotId`.

- **`GamePlayInstaller.cs`**
  - registra `DomePotRegistry` nel `ServiceContainer`.

- **`PotSlot.cs`**
  - registra / deregistra se stesso nel registry.
  - aggiorna il recupero di `GameManager` e `UINotification` preferendo `ServiceContainer`.

- **`PotGrowthController.cs`**
  - registra / deregistra se stesso nel registry.

- **`DayCycleController.cs`**
  - usa `DomePotRegistry` per `FindPotSlot()` e `FindPotGrowthController()`.

- **`PlantCardV3TerminalController.cs`**
  - usa `DomePotRegistry` per `FindPots()` e `FindPotById()`, riducendo gli scan completi di scena.

### 2.3 Fase 3 — Audit lookup ripetuti e caching mirato

- **`TopBarController.cs`**
  - `GameManager` ora passa prima da `ServiceContainer`.
  - `DayCycleController` recuperato da `ServiceContainer` invece di `FindObjectOfType`.
  - `MutationOrbitUI` cached in `Awake` invece di `GetComponent` al bisogno.

- **`ElevatorSystem.cs`**
  - `Camera.main` cached per evitare lookup ripetuti nel click-to-open.

- Audit manuale eseguito sui componenti con `Update()`/`OnGUI()` per evitare caching “a tappeto” non necessario; le modifiche sono state limitate ai casi davvero utili.

### 2.4 Fase 4 — Primo split strutturale delle god class

- Creato **`PotActionValidator.cs`**
  - incapsula la validazione per:
    - `CanPlant`
    - `CanWater`
    - `CanLight`
    - `CanApplyAdditive`
    - `CanHarvest`
    - `CanFertilize`
    - `CanPruning`
  - incapsula anche i principali failure reason.

- **`PotActions.cs`**
  - mantiene la facciata pubblica (`Do*`, `Can*`) già usata dal gioco.
  - delega la validazione e i failure reason a `PotActionValidator`.

- Creato **`PotWateringProcessor.cs`**
  - sposta fuori da `PotActions` la logica di toggle/stato/eventi del watering system.
  - `DoWater()` in `PotActions` ora delega a questo processor.

- Creato **`CondensationDayProcessor.cs`**
  - isola la logica di applicazione condensazione giornaliera.

- **`DayCycleController.cs`**
  - resta orchestratore del flusso giornaliero.
  - `ApplyCondensationSystem()` ora delega a `CondensationDayProcessor`.
  - mantenuta intatta l’API pubblica del controller.

### 2.5 Fase 5 — Rules Cursor

Sono ora presenti tre regole rilevanti:

- **`architecture-runtime-services.mdc`**
  - vieta nuovi `FindObjectOfType`/`FindObjectsOfType` runtime salvo fallback temporanei documentati,
  - promuove `ServiceContainer`, riferimenti serializzati e registry,
  - ricorda di mantenere `PotActions` come facade e `DayCycleController` come orchestratore.

- **`gameplay-runtime-patterns.mdc`**
  - indirizza la logica nuova verso validator/processor,
  - vieta nuova logica pesante diretta in `PotActions` / `DayCycleController`,
  - ricorda caching e uso di `DomePotRegistry`.

- **`new-feature-extension-paths.mdc`** (nuova in questa chiusura)
  - definisce in modo operativo dove mettere la logica delle **nuove funzionalità**:
    - bootstrap in installer,
    - validazione in validator,
    - esecuzione in processor,
    - coordinamento in facade/orchestratore,
    - collezioni dinamiche via registry.

---

## 3. File creati o modificati

### Nuovi file

| File | Scopo |
|------|-------|
| `Assets/_Project/Scripts/Dome/DomePotRegistry.cs` | Registry di scena per `PotSlot` e `PotGrowthController`. |
| `Assets/_Project/Scripts/Dome/PotActionValidator.cs` | Validazione centralizzata per azioni pot. |
| `Assets/_Project/Scripts/Dome/PotWateringProcessor.cs` | Processor dedicato alla logica di watering toggle. |
| `Assets/_Project/Scripts/Dome/Processors/CondensationDayProcessor.cs` | Processor per la condensazione giornaliera. |
| `.cursor/rules/architecture-runtime-services.mdc` | Regola sempre attiva per servizi/runtime architecture. |
| `.cursor/rules/gameplay-runtime-patterns.mdc` | Regola file-specific su Dome/World/Core/PlantCardV3. |
| `.cursor/rules/new-feature-extension-paths.mdc` | Regola file-specific per l’inserimento corretto delle nuove funzionalità. |

### File modificati

| File | Modifica principale |
|------|---------------------|
| `Assets/_Project/Scripts/Core/AppRoot.cs` | Preferenza per `ServiceContainer` prima dei fallback legacy. |
| `Assets/_Project/Scripts/Core/Installers/GamePlayInstaller.cs` | Registrazione `DomePotRegistry`. |
| `Assets/_Project/Scripts/World/Elevator/ElevatorSystem.cs` | Dipendenze via `ServiceContainer`, late binding, camera cached. |
| `Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs` | Dipendenze runtime via servizi, uso del registry, runner cached/late-bound. |
| `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs` | Registrazione nel `ServiceContainer`, config solo da Resources, uso registry, condensazione delegata. |
| `Assets/_Project/Scripts/Dome/PotActions.cs` | Delega a validator e watering processor. |
| `Assets/_Project/Scripts/Interactables/PotSlot.cs` | Registrazione nel registry, preferenza per servizi runtime. |
| `Assets/_Project/Scripts/Dome/PotSystem/Growth/PotGrowthController.cs` | Registrazione nel registry. |
| `Assets/_Project/Scripts/Player/PlayerClickMover2D.cs` | Registrazione nel `ServiceContainer`. |
| `Assets/_Project/Scripts/Player/PlayerPerspectiveMover2D.cs` | Registrazione nel `ServiceContainer`. |
| `Assets/_Project/Scripts/Player/PlayerMoverRouter2D.cs` | Registrazione nel `ServiceContainer`. |
| `Assets/_Project/Scripts/Player/PlayerInteractAdvice.cs` | Registrazione nel `ServiceContainer`. |
| `Assets/_Project/Scripts/Dome/PotAutomation/PotAutomationRunner.cs` | Registrazione nel `ServiceContainer`. |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs` | `DayCycleController` da `ServiceContainer`, `MutationOrbitUI` cached. |

---

## 4. Issue incontrati durante l’implementazione

- **`DomePotRegistry.cs`**
  - errore di compilazione `CS1513: } expected`
  - causa: mancava la parentesi finale di chiusura del namespace
  - fix: aggiunta la `}` finale

- **`PotActionValidator.cs`**
  - errore `CS0518: IsExternalInit is not defined`
  - causa: uso di proprietà `init` non compatibili con la versione C# del progetto Unity
  - fix: sostituzione delle proprietà `init` con campi normali nel contesto di validazione

- **`PotWateringProcessor.cs`**
  - errore `CS0246` su `DayActivityLog` e `DiaryStatistics`
  - causa: mancava `using _Project;`
  - fix: aggiunto il namespace corretto

Questi fix sono stati applicati immediatamente durante la sessione, senza cambiare il piano né la direzione architetturale.

---

## 5. Verifica

### Verifiche tecniche

- `ReadLints` sui file toccati: **nessun errore di lint** dopo i fix.
- Risolti i tre errori di compilazione emersi durante la prima integrazione.

### Verifica runtime

- L’utente ha eseguito:
  - prima run di gioco,
  - run con verifica mirata dei flussi principali,
  - conferma finale: **“sembra tutto funzionare bene”** / **“Già fatta”**.

### Flussi implicitamente validati in run

- bootstrap scena gameplay,
- wiring `ServiceContainer`,
- terminale Pot V3,
- registry dei vasi,
- day cycle / end day,
- elevator,
- top bar / condensazione,
- primi processor/validator senza regressioni visibili.

---

## 6. Impatto architetturale

- **Meno dipendenze implicite:** i sistemi principali sono meno dipendenti da scan della scena.
- **Più chiarezza:** `PotActions` e `DayCycleController` iniziano a diventare punti di orchestrazione/facciata, non contenitori monolitici di tutta la logica.
- **Base migliore per il futuro:** le nuove feature hanno ora:
  - un posto per la validazione,
  - un posto per l’esecuzione,
  - un posto per il coordinamento,
  - regole Cursor che lo ricordano in modo persistente.

---

## 7. Note finali

- Questa tranche di refactor è stata volutamente chiusa in uno stato **sicuro e compatibile**, non “massimalista”.
- Alcuni fallback legacy sono stati lasciati intenzionalmente dove servivano a non spezzare scene o wiring esistenti.
- Il refactor ha già prodotto valore reale senza richiedere una migrazione totale del progetto in un solo colpo.

Il prossimo eventuale step, se deciso in futuro, dovrebbe essere un **cleanup dei fallback temporanei rimasti** e un’ulteriore estrazione di processor da `PotActions` e `DayCycleController`, ma solo dopo aver consolidato questa base.

---

*Report generato a valle dell’implementazione del piano “Refactor Architetturale a Fasi”, con verifica runtime positiva e regole Cursor finali per le nuove funzionalità.*
