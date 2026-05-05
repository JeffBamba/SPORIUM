# DEV REPORT 0107 — PlantCard4v care view, layout reference e logica interventi

**Data:** 2026-05-05  
**Sprint / contesto:** UI Toolkit / Dome POT care view — nuova PlantCard4v, montaggio scena, VO ufficiale, logica interventi e rischi.  
**Riferimento piano:** `.cursor/plans/plantcard4v.plan.md`  
**Report precedente:** `DEV_REPORT_0106_HUD_TOOLTIP_DEMO_FLOW_POLISH_2026-05-05.md`

---

## Sommario interventi

1. Implementata la nuova **PlantCard4v** come pannello UI Toolkit UXML/USS per ispezione e cura ravvicinata del singolo POT.
2. Ricostruito il layout seguendo il concept visuale: pianta centrale dominante, pannelli laterali, header overlay sopra l'immagine, box VO basso e lista interventi.
3. Collegata la view a `PotStateModel`, `PlantData`, `StageRequirements`, `PotSystemConfig`, `PhSystem`, `DomePotRegistry` e `PotActions`.
4. Integrato il **VO ufficiale** nella posizione bassa della PlantCard4v, eliminando la seconda voce testuale locale come fonte narrativa parallela.
5. Aggiornata la gestione modale: PlantCard4v blocca input mondo ma mantiene visibili top bar e bottom bar, senza mostrare HUD contestuali non richiesti.
6. Aggiunta notifica toast per POT vuoto: la card spiega che la procedura `PLANT` resta nel Terminale POT.
7. Rifinita la logica degli **Interventi disponibili**: un solo intervento prioritario, altri tasti attivi/neutri, hover/press ripristinati e refresh realtime dopo azione.
8. Corretto il falso rischio **Stress da luce** appena dopo la piantagione: il rischio appare solo se dati reali lo giustificano.
9. Rimossa l'azione `OSSERVARE` dalla lista, come richiesto.
10. Eseguita pulizia parità UI Builder: niente `style=""` inline residui su PlantCard4v e struttura runtime editabile direttamente da UXML/USS.

---

## Statistiche e progresso

### Righe di codice

- **Ambito dichiarato:** file `.cs` coinvolti direttamente da PlantCard4v e relative integrazioni runtime.
- **Comando:** PowerShell `Get-Content <file> | Measure-Object -Line`.
- **Totale righe sui 6 file .cs:** **3218**.
  - `PlantCard4vController.cs`: 611
  - `PlantCard4vCareViewModel.cs`: 526
  - `GameplayUiModalLock.cs`: 44
  - `PotSlot.cs`: 305
  - `NotificationTypeSpecDefaults.cs`: 828
  - `VoOverlayController.cs`: 904
- **Authoring UI misurato separatamente:** `PlantCard4v.uxml` 197 righe, `PlantCard4v.uss` 808 righe.
- **Delta righe:** non misurato in modo affidabile, perché PlantCard4v è composta da nuovi file ancora untracked e il worktree contiene anche modifiche di scena/asset.

### Sistemi funzionanti

- **Build C# verificata:** `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal` completata con **0 errori / 0 avvisi**.
- **UXML verificato:** parse XML di `PlantCard4v.uxml` completato con esito OK.
- **Parità UI Builder verificata staticamente:** ricerca `rg "style=|sample|preview|authoring|builder-reference"` su `PlantCard4v.uxml` / `PlantCard4v.uss` senza risultati.
- **Da validare in Editor Play Mode:** resa visuale finale su tutti i POT, target `_targetPot`, ordinamento layer con scena reale, risposta input e asset image importati.

### Bug risolti

- **11** bug / regressioni UX documentate durante l'iterazione:
  1. PlantCard4v inizialmente troppo full-screen e poco compatta.
  2. Mancanza di placeholder icon coerenti con la reference.
  3. Doppio VO: box locale + VO ufficiale.
  4. HUD contestuali visibili quando dovevano restare visibili solo top bar e bottom bar.
  5. POT vuoto senza feedback: ora toast `POT-EMPTY`.
  6. Pulsante `OSSERVARE` non più necessario.
  7. Animazioni hover/press dei pulsanti azione perse.
  8. PlantCard4v non aggiornata abbastanza velocemente dopo intervento.
  9. Sovrapposizioni nel pannello rischi e nell'area centrale.
  10. Header overlay renderizzati sotto l'immagine centrale.
  11. Falso rischio `Stress da luce` con stress luce 0% appena dopo `PLANT`.

### Progresso gameplay / prodotto

- Il player ha una vista di cura ravvicinata più leggibile e meno terminale-testuale.
- La PlantCard4v suggerisce uno stato e un intervento prioritario senza trasformarsi in una checklist guidata.
- Le azioni restano strumenti disponibili, non step obbligati in sequenza.
- La procedura meccanica `PLANT` resta nel Terminale POT, mentre PlantCard4v comunica solo diagnosi e cura.
- Il VO ufficiale entra nel layout PlantCard4v senza creare una seconda voce narrativa.
- I rischi mostrati sono più aderenti ai dati reali del POT.

---

## 1. Nuovo pannello PlantCard4v UI Toolkit

### Problema

- Serviva una nuova schermata di cura per POT più visuale, leggibile e vicina alla reference allegata.
- Le prime iterazioni erano troppo grandi o troppo piccole, con pannelli sovrapposti e immagine centrale racchiusa in un box non coerente col concept.
- Alcune modifiche fatte da UI Builder avevano reintrodotto `style=""` inline, rischiando divergenza tra Builder e Play.

### Soluzione

- Creati `PlantCard4v.uxml` e `PlantCard4v.uss` come sorgente unica editabile in UI Builder.
- Il layout ora usa:
  - colonna sinistra per bisogni, pH, nutrimento, condizione e riepilogo;
  - camera centrale con immagine pianta full-area senza box interno;
  - header identità/stato in overlay sopra la camera;
  - colonna destra con rischi e interventi;
  - box VO basso.
- Spostate le correzioni visuali da inline UXML a USS, mantenendo parità Builder/Play.
- Risolto il problema di rendering sotto immagine spostando `pcv4-top-row` dopo `pcv4-main` nell'UXML e alzando il layer via USS.

**File interessati:**  
`Assets/_Project/UI/UIToolkit/PlantCard4v/PlantCard4v.uxml`,  
`Assets/_Project/UI/UIToolkit/PlantCard4v/PlantCard4v.uss`,  
`Assets/_Project/Art/Enviroments/Png/test_Plant.png`,  
`Assets/_Project/Art/Enviroments/Png/plantcard4/*`

---

## 2. Binding runtime e view model di cura

### Problema

- PlantCard4v doveva leggere il POT reale, non mostrare una card mock statica.
- Serviva distinguere stati vuoto, vivo, morto, pronto harvest, acqua bassa/alta, pH fuori range, muffa, LED e fertilizzante.
- La view doveva aggiornarsi solo per il POT target e non reagire agli eventi degli altri POT.

### Soluzione

- Aggiunto `PlantCard4vCareViewModel`, che costruisce un modello UI da `PotStateModel`, `PlantData`, `StageRequirements`, `PotSystemConfig` e `PhSystem`.
- Aggiunto `PlantCard4vController`, con binding stabile tramite `Q<>()` su nomi UXML.
- Il controller risolve il target tramite `_targetPot` o `DomePotRegistry.FindPotById(_potId)`.
- La view ascolta `PotEvents` rilevanti e filtra per POT proprietario.
- Le azioni passano da `PotActions`, senza duplicare logica gameplay.
- `PotSlot` emette anche `PotEvents.EmitSelected(this)` quando il POT viene selezionato, così PlantCard4v può aprirsi sul proprio POT.

**File interessati:**  
`Assets/_Project/Scripts/UI/UIToolkit/PlantCard4v/PlantCard4vCareViewModel.cs`,  
`Assets/_Project/Scripts/UI/UIToolkit/PlantCard4v/PlantCard4vController.cs`,  
`Assets/_Project/Scripts/Interactables/PotSlot.cs`

---

## 3. VO ufficiale e comportamento modale HUD

### Problema

- C'erano due sorgenti VO: il VO ufficiale Sporium e una trascrizione locale nella card.
- Quando PlantCard4v era aperta serviva mantenere visibili top bar e bottom bar, ma non gli HUD contestuali di notifiche/missioni richiesti nelle iterazioni precedenti.
- Il pannello doveva bloccare input mondo senza comportarsi come una macchina che nasconde tutta la HUD fissa.

### Soluzione

- `VoOverlayController` espone `SetPlantCard4vDocked(bool)`, che applica classi dedicate mentre PlantCard4v è aperta.
- `VoOverlay.uss` aggiunge il docking PlantCard4v: VO basso, centrato, dimensione compatta, pointer-ignore quando docked.
- `PlantCard4vController` svuota il testo locale e usa il VO ufficiale tramite `ShowLine`.
- `GameplayUiModalLock.SetMachineModalState` ora accetta `keepFixedHudVisible`, così PlantCard4v può bloccare input senza spegnere top/bottom bar.

**File interessati:**  
`Assets/_Project/Scripts/UI/UIToolkit/VoOverlay/VoOverlayController.cs`,  
`Assets/_Project/Resources/UI/UIToolkit/VoOverlay/VoOverlay.uss`,  
`Assets/_Project/Scripts/Core/GameplayUiModalLock.cs`,  
`Assets/_Project/Scripts/UI/UIToolkit/PlantCard4v/PlantCard4vController.cs`

---

## 4. POT vuoto e procedure Terminale POT

### Problema

- Aprendo PlantCard4v su un POT senza pianta, il player poteva non capire perché non ci fossero dati di cura.
- PlantCard4v non deve sostituire il Terminale POT per `PLANT`, `HARVEST` o `UPROOT`.

### Soluzione

- Il view model produce uno stato `VASO VUOTO` con copy dedicato.
- Il controller mostra una toast una sola volta per apertura card, usando il tipo `POT-EMPTY`.
- `NotificationTypeSpecDefaults` aggiunge la notifica:
  - "POT vuoto ({potId}) — usa il Terminale POT per piantare un seme."
  - tooltip: PlantCard4v è vista di cura, `PLANT` resta nel Terminale POT.
- I terminal-only action kind (`TerminalPlant`, `TerminalHarvest`, `TerminalUproot`) vengono visualizzati come informazione, non come bottoni eseguibili dalla card.

**File interessati:**  
`Assets/_Project/Scripts/UI/UIToolkit/PlantCard4v/PlantCard4vCareViewModel.cs`,  
`Assets/_Project/Scripts/UI/UIToolkit/PlantCard4v/PlantCard4vController.cs`,  
`Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/NotificationTypeSpecDefaults.cs`

---

## 5. Interventi disponibili: meno guida, più diagnosi

### Problema

- La prima logica esponeva `PrimaryAction` e `SecondaryAction` come `Priorita: PRIMA` / `Priorita: POI`.
- Questo guidava troppo il player, come una sequenza di bottoni da premere.
- Alcuni tasti neutri sembravano inattivi.
- Hover/active dei pulsanti erano stati persi e il feedback dopo intervento non appariva abbastanza realtime.

### Soluzione

- La UI evidenzia **solo un** `INTERVENTO PRIORITARIO`.
- Gli altri pulsanti restano attivi/neutri, con copy generico: `Gestione acqua`, `Controllo LED`, `Gestione acidita'`, `Cura tessuti`, `Nutrimento`.
- Rimossa la classe visuale secondaria `pcv4-action--secondary`.
- Ripristinate animazioni `:hover` e `:active`, inclusi cambi bordo/background/scale e feedback su icona/titolo/freccia.
- Dopo una `PotAction`, il controller usa una coroutine di refresh differito di un frame per leggere lo stato aggiornato.
- Rimosso il pulsante `OSSERVARE`.

**File interessati:**  
`Assets/_Project/Scripts/UI/UIToolkit/PlantCard4v/PlantCard4vController.cs`,  
`Assets/_Project/Scripts/UI/UIToolkit/PlantCard4v/PlantCard4vCareViewModel.cs`,  
`Assets/_Project/UI/UIToolkit/PlantCard4v/PlantCard4v.uxml`,  
`Assets/_Project/UI/UIToolkit/PlantCard4v/PlantCard4v.uss`

---

## 6. Rischi attivi e falso positivo Stress da luce

### Problema

- Dopo una piantagione dal Terminale POT, PlantCard4v mostrava `RISCHI ATTIVI > Stress da luce` anche se lo stress luce reale era 0%.
- Causa 1: la riga secondaria `Stress da luce` era un placeholder UXML sempre visibile.
- Causa 2: la logica trattava un LED richiesto/non allineato come rischio, anche quando era solo una possibile azione di orientamento.

### Soluzione

- Aggiunti a `PlantCard4vCareViewModel`:
  - `LightStressPercent`;
  - `HasSecondaryRisk`;
  - `SecondaryRiskTitle`;
  - `SecondaryRiskCause`.
- Il rischio luce viene mostrato solo quando:
  - il LED acceso è incompatibile con la famiglia della pianta;
  - oppure `LightStressPercent >= 80`.
- Se stress luce è 0% e il LED non è incompatibile, la riga secondaria rischio viene nascosta a runtime.
- Se serve una regolazione LED senza rischio attivo, PlantCard4v la presenta come orientamento/intervento, non come minaccia.

**File interessati:**  
`Assets/_Project/Scripts/UI/UIToolkit/PlantCard4v/PlantCard4vCareViewModel.cs`,  
`Assets/_Project/Scripts/UI/UIToolkit/PlantCard4v/PlantCard4vController.cs`,  
`Assets/_Project/UI/UIToolkit/PlantCard4v/PlantCard4v.uxml`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `.cursor/plans/plantcard4v.plan.md` | Piano operativo PlantCard4v / care view |
| `Assets/_Project/UI/UIToolkit/PlantCard4v/PlantCard4v.uxml` | Nuova struttura pannello UI Toolkit, nodi runtime e placeholder Builder |
| `Assets/_Project/UI/UIToolkit/PlantCard4v/PlantCard4v.uss` | Layout, stile reference, hover/active azioni, overlay, chamber e rischio |
| `Assets/_Project/Scripts/UI/UIToolkit/PlantCard4v/PlantCard4vController.cs` | Controller runtime, binding UI, azioni, refresh, VO, toast, rischio secondario |
| `Assets/_Project/Scripts/UI/UIToolkit/PlantCard4v/PlantCard4vCareViewModel.cs` | Modello dati POT, bisogni, rischi, interventi, stato vuoto/morto/harvest |
| `Assets/_Project/Scripts/Core/GameplayUiModalLock.cs` | Modal lock con opzione per mantenere visibile HUD fissa |
| `Assets/_Project/Scripts/Interactables/PotSlot.cs` | Emissione evento selezione POT verso `PotEvents` |
| `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/NotificationTypeSpecDefaults.cs` | Toast `POT-EMPTY` |
| `Assets/_Project/Scripts/UI/UIToolkit/VoOverlay/VoOverlayController.cs` | Dock PlantCard4v per VO ufficiale |
| `Assets/_Project/Resources/UI/UIToolkit/VoOverlay/VoOverlay.uss` | Classi USS per VO docked in PlantCard4v |
| `Assets/_Project/Scenes/SCN_VaultMap.unity` | Montaggio/serializzazione scena PlantCard4v nel working tree |
| `Assets/_Project/Art/Enviroments/Png/test_Plant.png` | Immagine chamber centrale |
| `Assets/_Project/Art/Enviroments/Png/plantcard4/*` | Asset visuali / icone / reference PlantCard4v |

---

## Regole / vincoli rispettati

- **UI Toolkit Builder parity:** PlantCard4v è costruita in UXML/USS, non con un albero parallelo hardcoded.
- **Niente inline style residui:** verifica statica `rg "style=|sample|preview|authoring|builder-reference"` senza risultati sui file PlantCard4v UXML/USS.
- **Runtime gameplay authoritative:** le azioni passano da `PotActions`; PlantCard4v non duplica le procedure di sistema.
- **Terminale POT preservato:** `PLANT`, `HARVEST`, `UPROOT` restano fuori dalla PlantCard4v.
- **Dirty worktree rispettato:** non sono state revertite modifiche esistenti o non correlate.
- **DEV REPORT:** sezione `Statistiche e progresso` presente con dati misurati o validazione dichiarata.

---

## Note operative (Unity)

- Da validare in Play Mode:
  - aprire PlantCard4v su POT appena piantato: nessun `Stress da luce` se stress luce è 0%;
  - aprire PlantCard4v su POT vuoto: toast `POT-EMPTY`;
  - clic su `IRRIGARE`, `LUCE`, `ADDITIVO pH`, `POTARE`, `FERTILIZZARE`: refresh UI dopo intervento;
  - hover/press su tutti i pulsanti azione;
  - overlay header sopra immagine centrale in tutte le risoluzioni target;
  - VO ufficiale docked nel box basso della PlantCard4v;
  - top bar e bottom bar visibili con PlantCard4v aperta, HUD contestuali non mostrati.
- Build C# già verificata fuori Editor con 0 errori / 0 avvisi.
- Se Unity rigenera `.meta` sugli asset immagine o report, controllare solo che i GUID siano unici e non sostituiscano asset esistenti.

---

*Fine DEV REPORT 0107.*
