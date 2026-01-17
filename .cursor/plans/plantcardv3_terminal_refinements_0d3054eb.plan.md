---
name: plantcardv3_terminal_refinements
overview: "Allineare PlantCardV3 Terminal al design: rimuovere queue sidebar e conferme manuali, mantenere gestione queue via comandi testuali, implementare FORECAST reale (stima avanzamento stage) senza side-effects."
todos:
  - id: remove_queue_sidebar
    content: Rimuovere `pcv3-queue` da `PlantCardV3_Terminal.uxml` e ripulire stili `.pcv3-queue*` in `PlantCardV3_Terminal.uss`
    status: completed
  - id: remove_queue_rendering
    content: Rimuovere `RefreshQueueList()` e tutti i campi/eventi UI correlati alla queue dal `PlantCardV3TerminalController` mantenendo la queue come struttura dati interna
    status: completed
  - id: auto_execute_on_exit_esc
    content: "Modificare `RequestClose()`/flow di chiusura: su `EXIT`/`ESC` eseguire automaticamente la queue (niente prompt `[Y/N]`), mantenendo `CLEAR` per annullare"
    status: completed
  - id: add_queue_show_command
    content: Aggiungere comando `QUEUE SHOW` che stampa la queue in console con formattazione coerente (e riusa `CLEAR` come svuota-queue)
    status: completed
  - id: implement_forecast_readonly
    content: "Implementare `FORECAST`: calcolo forecast per pot (prossimo stage, requisiti, giorni stimati) con stima punti giornalieri senza side-effects (no mutazioni su `PotStateModel`) + `PrintForecast()` tabellare"
    status: completed
---

## Scope (aggiornato dopo audit codice)

- Rimuovere UI queue dalla sidebar (`pcv3-queue`) e tutta la logica di rendering correlata.
- Eliminare la conferma manuale su chiusura: su `EXIT`/`ESC` la queue viene **eseguita automaticamente** (nessun prompt `[Y/N]`).
- Implementare comando `FORECAST` reale (oggi è placeholder) e mantenerne lo shortcut `F`.
- Mantenere la queue consultabile via console: introdurre `QUEUE SHOW` (e opzionali alias minimi) al posto della sidebar.

## Stato attuale (evidenza)

- `pcv3-queue` è ancora presente in `Assets/_Project/UI/UIToolkit/PlantCardV3/PlantCardV3_Terminal.uxml`.
- `RefreshQueueList()` esiste e renderizza la queue in sidebar in `Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs`.
- `FORECAST` è cablato ma non implementato (stampa `Not implemented yet`).
- `BACK` non risulta nel parser attuale (rimuovere task “remove_back_command” dal plan).
- La conferma oggi avviene con prompt `[Y/N]` su `RequestClose()` quando `_queue.Count > 0` (da sostituire con auto-execute).
- Nota tecnica: `GrowthPointsCalculator.CalculateDailyPoints()` **muta** lo stato del `PotStateModel` (incrementa growth points). La stima forecast deve evitare side-effects.

## Decisioni di design

- **Queue**: UI sidebar rimossa; la queue resta come struttura dati interna. Per consultarla si usa `QUEUE SHOW` (stampa console) + si mantiene `CLEAR` (già esiste) come svuota-queue.
- **Chiusura**: `EXIT`/`ESC` = “commit” implicito (esegue queue). Se queue vuota, chiude e basta. Se l’utente vuole annullare, usa `CLEAR` prima di uscire.
- **Forecast**: stima conservativa “a condizioni attuali”, con tabella per tutti i pot con pianta. L’output indica requisiti mancanti e giorni stimati.

## Implementazione (file principali)

- UI:
- `Assets/_Project/UI/UIToolkit/PlantCardV3/PlantCardV3_Terminal.uxml`
- `Assets/_Project/UI/UIToolkit/PlantCardV3/PlantCardV3_Terminal.uss`
- Logica:
- `Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs`
- Dipendenze utili per forecast:
- `Assets/_Project/Scripts/Dome/PotSystem/Growth/PlantData.cs` (`GetStageRequirements`)
- `Assets/_Project/Scripts/Dome/PotSystem/Growth/GrowthPointsCalculator.cs` (logica punti giornalieri; attenzione side-effects)

## Criteri di accettazione (Definition of Done)

- In UI Builder e in runtime non esiste più alcun elemento `pcv3-queue` nella sidebar.
- Nessun riferimento a `_queueList` / `RefreshQueueList()` nel controller.
- `EXIT` e `ESC` chiudono il terminale e avviano l’automazione della queue **senza** chiedere `[Y/N]`.
- `QUEUE SHOW` stampa in console la lista azioni accodate (o messaggio “empty”).
- `FORECAST` stampa una tabella leggibile per tutti i pot con pianta:
- stage corrente, prossimo stage, giorni stimati, requisiti ✓/✗.
- nessun side-effect sui dati del pot (nessun incremento growth points).

## Rischi / edge cases

- Forecast deve gestire: pot senza pianta, pianta già all’ultimo stage, requisiti non definiti in `PlantData`, config pot non disponibile.
- Se la stima usa logica growth points, va implementata una variante “read-only” (copie locali o calcolo parallelo) per evitare mutazioni in `PotStateModel`.