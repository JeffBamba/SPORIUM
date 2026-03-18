---
name: refactor architettura guidato
overview: "Ridurre le dipendenze implicite e le classi monolitiche in modo incrementale: prima eliminazione controllata di `FindObjectOfType`, poi audit/caching dei lookup runtime, infine estrazione di validator/processor e formalizzazione delle regole Cursor per evitare ricadute future."
todos:
  - id: phase1-bootstrap-deps
    content: "Allineare bootstrap e dipendenze runtime: ServiceContainer/Inspector al posto dei FindObjectOfType più gratuiti in AppRoot, ElevatorSystem, PlantCardV3TerminalController e DayCycleController."
    status: completed
  - id: phase2-scene-registry
    content: Introdurre un registry di scena per i vasi e sostituire i FindObjectsOfType nei flussi di terminale e ciclo giornaliero.
    status: completed
  - id: phase3-runtime-audit
    content: Fare audit dei componenti con Update/OnGUI e cache dei GetComponent/lookup ripetuti solo dove realmente presenti.
    status: completed
  - id: phase4-godclass-split
    content: Estrarre validator e processor da PotActions e DayCycleController mantenendo invariata l’API pubblica verso il resto del gioco.
    status: completed
  - id: phase5-cursor-rules
    content: Chiudere con regole Cursor concise e sempre applicabili per fissare le nuove convenzioni architetturali e prevenire ricadute future.
    status: completed
isProject: false
---

# Refactor Architetturale a Fasi

## Obiettivo

Ridurre fragilità e debito tecnico nei punti più critici del gameplay senza rompere la build: prima rendere esplicite le dipendenze, poi togliere lookup runtime inutili, infine spezzare le classi monolitiche mantenendo stabili le API pubbliche.

## Stato Attuale Rilevante

- `AppRoot` espone wrapper generici basati su `FindObjectOfType` / `FindObjectsOfType`: [Assets/_Project/Scripts/Core/AppRoot.cs](d:\Sporae_Build_Beta\Assets_Project\Scripts\Core\AppRoot.cs)
- `PlantCardV3TerminalController` usa già `ServiceContainer` per alcuni servizi, ma continua a fare fallback `FindObjectOfType` per `GameManager` e per i mover: [Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs](d:\Sporae_Build_Beta\Assets_Project\Scripts\UI\UIToolkit\PlantCardV3\PlantCardV3TerminalController.cs)
- `DayCycleController` è ibrido: late binding parziale + `FindObjectOfType` / `FindObjectsOfType`: [Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs](d:\Sporae_Build_Beta\Assets_Project\Scripts\Dome\SPOR-BLK-01-03A-DayCycleController.cs)
- `ElevatorSystem` miscela `ServiceContainer` e lookup diretti: [Assets/_Project/Scripts/World/Elevator/ElevatorSystem.cs](d:\Sporae_Build_Beta\Assets_Project\Scripts\World\Elevator\ElevatorSystem.cs)
- `ServiceContainer` è già il posto giusto per i servizi runtime e supporta `OnServiceRegistered`: [Assets/_Project/Scripts/Core/ServiceLocator/ServiceContainer.cs](d:\Sporae_Build_Beta\Assets_Project\Scripts\Core\ServiceLocator\ServiceContainer.cs)
- `GamePlayInstaller` è il bootstrap naturale per registrare servizi di scena/globali: [Assets/_Project/Scripts/Core/Installers/GamePlayInstaller.cs](d:\Sporae_Build_Beta\Assets_Project\Scripts\Core\Installers\GamePlayInstaller.cs)

## Strategia

### Fase 1 — Stabilizzare le dipendenze senza cambiare comportamento

Obiettivo: togliere i `FindObjectOfType` più gratuiti e sostituirli con `ServiceContainer`, riferimenti serializzati o piccoli registry, mantenendo fallback temporanei dove necessario.

- `AppRoot`
  - Deprecare l’uso di `GetSystem<T>()` e `GetAllSystems<T>()` come API di comodo.
  - Portare `GetGameManager()` a preferire `ServiceContainer` e lasciare fallback solo temporaneo.
- `ElevatorSystem`
  - Ottenere `PlayerClickMover2D`, `PlayerInteractAdvice`, `UINotification` da `ServiceContainer` o campi serializzati.
  - Lasciare `Camera.main` come percorso standard e fallback controllato solo se indispensabile.
- `PlantCardV3TerminalController`
  - Eliminare i `FindObjectOfType` per `GameManager`, `PlayerClickMover2D`, `PlayerPerspectiveMover2D`, `PlayerMoverRouter2D`, `PotAutomationRunner`.
  - Passare a servizi registrati o campi serializzati, mantenendo un fallback solo nella prima fase se serve per compatibilità scene.
- `DayCycleController`
  - Rimuovere `FindObjectOfType<PotSystemConfig>()` e usare solo `Resources` / `AssetManager`.
  - Far arrivare `UINotification` / toast via `ServiceContainer` in modo coerente con `GamePlayInstaller`.
- Bootstrap
  - Estendere `GamePlayInstaller` per registrare i servizi runtime mancanti che oggi costringono ai lookup.

Output atteso:

- Dipendenze più esplicite.
- Minore fragilità all’avvio scena.
- Nessuna modifica funzionale percepibile per il giocatore.

### Fase 2 — Introdurre registry di scena e audit dei lookup ripetuti

Obiettivo: togliere `FindObjectsOfType` dai flussi di gioco e limitare lookup costosi nei path frequenti.

- Introdurre `DomePotRegistry` o equivalente per esporre la lista attuale di `PotSlot` e, se utile, `PotGrowthController`.
- Far leggere il registry a:
  - `DayCycleController`
  - `PlantCardV3TerminalController`
- Valutare se serve un piccolo registry anche per `PlantCardV3TerminalOpener` o altri oggetti runtime coordinati.
- Fare audit sui componenti con `Update()` / `OnGUI()` e cercare solo i casi reali di:
  - `GetComponent`
  - `GetComponentInChildren`
  - `FindObjectOfType`
  - accessi ripetuti a oggetti ottenibili una sola volta
- Spostare quei lookup in `Awake`, `Start` o `OnEnable` dove sicuro.

Output atteso:

- Meno scan completi di scena.
- Migliore prevedibilità dei dati usati da terminale e ciclo giornaliero.
- Micro-miglioramenti di performance nei loop runtime.

### Fase 3 — Spezzare le god class mantenendo API stabili

Obiettivo: aumentare leggibilità e manutenibilità senza rompere i chiamanti esistenti.

#### `PotActions`

Approccio: mantenerla come facade e spostare la logica nuova in moduli dedicati.

- Estrarre un `PotActionValidator` con check tipo:
  - `CanPlant`
  - `CanHarvest`
  - `CanWater`
  - `CanFertilize`
- Estrarre processor per responsabilità, uno alla volta:
  - `PotPlantProcessor`
  - `PotWateringProcessor`
  - `PotLightingProcessor`
  - `PotHarvestProcessor`
  - `PotFertilizeProcessor`
  - `PotPruningProcessor`
- `PotActions.Do*()` resta il punto d’ingresso pubblico e delega internamente.

#### `DayCycleController`

Approccio: trasformarlo in orchestratore.

- Estrarre processor/step chiari:
  - `GrowthResolutionProcessor`
  - `WateringResolutionProcessor`
  - `LedResolutionProcessor`
  - `ConditionResolutionProcessor`
  - eventuale `DaySummaryBuilder` / `DiarySnapshotBuilder`
- `DayCycleController` mantiene:
  - subscribe/unsubscribe eventi
  - ordine di esecuzione
  - coordinamento tra i processor
- Ogni processor riceve input espliciti e riduce la dipendenza dallo stato implicito della scena.

Output atteso:

- File più piccoli e leggibili.
- Refactor futuri meno rischiosi.
- Migliore testabilità logica.

### Fase 4 — Consolidare la nuova architettura con Cursor Rules

Obiettivo: evitare il ritorno al “coding artigianale”.

Creare almeno due regole in `.cursor/rules/`:

- Regola sempre attiva, architetturale
  - Vietare nuovi `FindObjectOfType` / `FindObjectsOfType` nel runtime salvo eccezioni motivate.
  - Preferire `ServiceContainer` per servizi globali.
  - Preferire riferimenti serializzati per oggetti di scena specifici.
  - Preferire registry dedicati per collezioni dinamiche di oggetti runtime.
  - Vietare nuova logica pesante in `PotActions` e `DayCycleController`; usare validator/processor.
- Regola file-specific per gameplay/runtime
  - Applicare a `**/Dome/**/*.cs`, `**/World/**/*.cs`, `**/UI/UIToolkit/PlantCardV3/**/*.cs`, `**/Core/**/*.cs`.
  - Ricordare dove va la logica nuova e come gestire dipendenze, caching e orchestrazione.

Le regole dovranno essere corte, operative e coerenti con il formato `.mdc` già presente in `.cursor/rules/`.

## Sequenza di esecuzione consigliata

1. Bootstrap e dipendenze semplici: `GamePlayInstaller`, `AppRoot`, `ElevatorSystem`.
2. Registry di scena: `DomePotRegistry` e integrazione in terminale/day cycle.
3. Audit/caching dei lookup nei componenti con loop runtime.
4. Refactor `PotActions` in facade + validator + primo processor.
5. Refactor `DayCycleController` in orchestratore + primo processor.
6. Cursor Rules finali per bloccare le vecchie scorciatoie.

## Rischi e mitigazioni

- Rischio principale: `null reference` su scene che oggi si appoggiano a fallback impliciti.
  - Mitigazione: fallback temporanei solo in Fase 1, poi rimossi quando il bootstrap è verificato.
- Rischio medio: regressioni nel ciclo giornaliero o nel terminale se si cambia insieme discovery oggetti + logica.
  - Mitigazione: separare “sostituzione dipendenze” da “estrazione processor”.
- Rischio alto solo se si tenta un refactor massivo di `PotActions` o `DayCycleController` in un’unica PR.
  - Mitigazione: un processor alla volta, mantenendo intatta l’API pubblica.

## Verifica per fase

- Fase 1
  - Apertura scena gameplay, bootstrap, interazioni base, terminale, ascensore.
- Fase 2
  - Lista vasi nel terminale, refresh header/sidebar, risoluzione day cycle su tutti i vasi.
- Fase 3
  - Regression test manuale su plant, watering, LED, harvest, fertilize, end day.
- Fase 4
  - Verifica che le rule siano concise, sempre applicabili dove serve e allineate alla nuova architettura.

## Non fare in questo piano

- Non introdurre contemporaneamente nuove feature gameplay.
- Non cambiare API pubbliche dei sistemi usati dal terminale se non strettamente necessario.
- Non sostituire tutti i fallback in un solo passaggio.

