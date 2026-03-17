# DEV REPORT 0065 — Terminal STATUS: flusso scelta vaso, bug typewriter/output lungo, riduzione frequenza flush

**Data:** 2026-03-17  
**Oggetto:** Modifica comando STATUS (scelta vaso con tasto 1–4, riepilogo + approfondimento solo vaso scelto); analisi e mitigazione bug “solo primo POT visibile” con typewriter su output lungo; riduzione frequenza di flush nel typewriter (flush ogni N righe invece che a ogni blocco).  
**Riferimenti:** `PlantCardV3TerminalController.cs`, ScrollView/ScrollTo/scrollOffset, typewriter, STATUS.  
**Report precedente:** `Assets/Docs/REPORT/DEV_REPORT_0064_SCROLLBAR_TERMINAL_POT_E_ROTELLA.md`

---

## 1. Contesto

- **Bug storico:** Con il comando STATUS (e in passato FORECAST), quando l’output era lungo (più vasi con sezioni STATO CORRENTE, STAGE PROGRESSION, ADVANCEMENT REQUIREMENTS, CONSIGLIO) la console mostrava solo il primo POT; il resto appariva solo dopo aver eseguito un altro comando.
- **Tentativi di fix precedenti (in chat):** bypass typewriter per STATUS (risolveva il bug ma toglieva l’effetto battitura); reintroduzione typewriter con refresh/scroll a scadenze multiple (0, 50, 100, 200, 350 ms); rimozione riassegnazione `_consoleText.text` nei callback ritardati (per evitare reset scroll); impostazione `verticalScroller.value = highValue` anche quando negativo e uso di `scrollOffset`; flush in `finally` della coda typewriter rimanente; try/catch per riga (poi rimosso per CS1626). Il bug persisteva con typewriter attivo su output lungo.
- **Richiesta utente:** Cambiare il flusso STATUS: prima chiedere “per quale POT?” con 4 opzioni (tasto 1–4), poi mostrare solo **riepilogo generale stato vasi** + **approfondimento per il vaso scelto** (come in reference UI). In questo modo l’output per comando è più corto e si è provato a rintrodurre il typewriter; il bug si è ripresentato. Si è quindi applicata una riduzione della frequenza di flush durante il typewriter (flush ogni N righe invece che a ogni blocco/tag) per mitigare il problema.

---

## 2. Lavoro svolto

### 2.1 Nuovo flusso STATUS: scelta vaso (1–4) poi riepilogo + approfondimento

- **Stato e dati:**
  - Aggiunto `InputState.SelectingStatusPot` e lista `_potsForStatusChoice` per tenere l’elenco dei vasi quando si è in attesa della scelta.

- **ExecuteCommandBody — comando STATUS:**
  - Non si stampa più la tabella completa di tutti i vasi.
  - Si ottengono i vasi con `FindPots()`, si riempie `_potsForStatusChoice`, e se non ci sono vasi si mostra messaggio e return.
  - Si stampa il messaggio **“▸ QUALE VASO?”** con istruzione “Digita il numero del vaso (tasto 1–4)” e l’elenco **`[1] POT-001`**, **`[2] POT-002`**, ecc.
  - Si imposta `_inputState = InputState.SelectingStatusPot` e si torna.

- **HandleCommand — branch SelectingStatusPot:**
  - L’input successivo (es. “1”, “2”, “3”, “4” o ID vaso) viene gestito con loading “STATUS” e callback `HandleStatusPotChoice(upper)`.

- **HandleStatusPotChoice:**
  - Parsing: numero 1–4 come indice in `_potsForStatusChoice`, oppure ID vaso con `FindPotById`.
  - Se scelta non valida: messaggio di errore e return.
  - Altrimenti: si stampa **riepilogo generale** (tabella tutti i vasi + testo “CONDIZIONI PER VASO”) e **solo il dettaglio del vaso scelto** (STATO CORRENTE, STAGE PROGRESSION, ADVANCEMENT REQUIREMENTS, CONSIGLIO).

- **Refactoring stampa STATUS:**
  - **PrintStatusSummaryTable(List<PotSlot> pots):** stampa solo la tabella “RIEPILOGO STATO VASI” e il blocco “CONDIZIONI PER VASO” (come leggere i dati, requisiti per stadio).
  - **PrintStatusDetailForPot(PotSlot pot):** stampa solo per un vaso le sezioni dettaglio + CONSIGLIO (o “VUOTO” se senza pianta).
  - **PrintStatusTable():** invariato come “tutti i vasi”; ora implementato come `PrintStatusSummaryTable(FindPots())` + per ogni vaso `PrintStatusDetailForPot(pot)`.

### 2.2 Typewriter per output STATUS (dopo scelta vaso)

- In **HandleStatusPotChoice** è stato rimosso il bypass typewriter: non si chiama più `FlushTypewriterQueueImmediate()` né si imposta `_typewriterActive = false`. Si imposta `_typewriterActive = true` prima di `PrintStatusSummaryTable` e `PrintStatusDetailForPot`, poi `FlushConsole()`, così l’output va in coda al typewriter e viene mostrato con effetto battitura. Il bug “solo primo blocco visibile” si è ripresentato; si è quindi applicata la riduzione della frequenza di flush (vedi 2.3).

### 2.3 Riduzione frequenza flush nel typewriter

- **Problema ipotizzato:** Ogni `FlushConsoleImmediate()` assegna `_consoleText.text = _consoleBuffer.ToString()` e richiama `AutoScrollConsole()`. In UI Toolkit, cambiare il testo del contenuto può resettare lo scroll a 0. Con output lungo, le chiamate erano molto frequenti (dopo ogni blocco di caratteri e dopo ogni tag tipo `<color=...>`), causando molti “scroll in fondo” seguiti da “reset in cima” e layout instabile.

- **Modifiche in TypewriterRoutine():**
  - **Rimossi** tutti i `FlushConsoleImmediate()` **dentro** il ciclo di elaborazione della riga: sia dopo la scrittura di un tag (`<...>`), sia dopo ogni blocco di caratteri. Il buffer continua a essere riempito carattere per carattere (con delay e SFX), ma la Label non viene aggiornata in quei punti.
  - **Flush solo ogni N righe:** introdotto contatore `linesSinceFlush` e campo serializzato **`_typewriterFlushEveryNLines`** (default **3**, range 1–10, tooltip: “Aggiorna la Label ogni N righe”). Alla fine di ogni riga (`_consoleBuffer.AppendLine()`) si incrementa il contatore; si chiama `FlushConsoleImmediate()` solo quando `linesSinceFlush >= _typewriterFlushEveryNLines`, poi si azzera il contatore.
  - Il **`finally`** del typewriter continua a eseguire un ultimo `FlushConsoleImmediate()` e lo scroll (inclusi i callback ritardati 0, 50, 100, 200, 350 ms), oltre al flush della coda rimanente se la routine si interrompe.

- **Effetto:** Da molte decine di flush per schermata (uno a ogni blocco e a ogni tag) si passa a al massimo un flush ogni 3 righe (o al valore configurato in Inspector), riducendo le assegnazioni a `.text` e i reset di scroll.  
- **Esito:** La riduzione della frequenza di flush **non ha risolto** il bug “solo primo blocco visibile”; da verificare domani (bypass typewriter per STATUS o altre strade).

### 2.4 Fix compilazione CS1626 (try con catch e yield)

- In una versione intermedia era stato aggiunto un `try/catch` per riga nel typewriter; in C# non è consentito usare `yield return` nel corpo di un `try` che ha una clausola `catch` (errore CS1626). Il try/catch è stato rimosso; è rimasto solo il flush della coda rimanente nel `finally` in caso di interruzione.

### 2.5 Comportamento scroll (già presenti in codice)

- In **AutoScrollConsole** / **ScrollToBottom:** non si usa più `ScrollTo(_consoleText)` (portava la vista sull’inizio della Label). Si imposta solo `verticalScroller.value = highValue` e `scrollOffset = (scrollOffset.x, highValue)` quando il range è non nullo (anche con `highValue` negativo).
- Nel **`finally`** del typewriter vengono schedulati più `AutoScrollConsole()` a 0, 50, 100, 200, 350 ms per dare tempo al layout di aggiornarsi.

---

## 3. File modificati

| File | Modifica |
|------|----------|
| `Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs` | Enum `InputState`: aggiunto `SelectingStatusPot`. Campo `_potsForStatusChoice`. STATUS: messaggio “Quale vaso?” con [1]…[4], stato SelectingStatusPot. HandleCommand: branch per SelectingStatusPot con LoadingThenExecuteStep e HandleStatusPotChoice. HandleStatusPotChoice: parsing 1–4 o ID vaso, PrintStatusSummaryTable + PrintStatusDetailForPot per vaso scelto, typewriter attivo (nessun bypass). PrintStatusSummaryTable(pots), PrintStatusDetailForPot(pot), PrintStatusTable() refactor. TypewriterRoutine: rimossi FlushConsoleImmediate da tag e da blocco caratteri; contatore linesSinceFlush e flush solo ogni _typewriterFlushEveryNLines righe. Campo _typewriterFlushEveryNLines (default 3). ScrollToBottom: solo vs.value e scrollOffset, niente ScrollTo(Label). |

---

## 4. Riepilogo per QA

- **STATUS:** Digitare STATUS → appare “▸ QUALE VASO?” con [1] POT-001, [2] POT-002, ecc. Digitare 1, 2, 3 o 4 (o ID vaso): dopo il loading viene mostrato il riepilogo generale (tabella tutti i vasi + testo CONDIZIONI PER VASO) e l’approfondimento solo del vaso scelto (STATO CORRENTE, STAGE PROGRESSION, ADVANCEMENT REQUIREMENTS, CONSIGLIO). Effetto typewriter su questo output.
- **Typewriter / scroll:** Con output lungo (es. STATUS dopo scelta vaso) il bug può ancora manifestarsi (solo primo blocco visibile fino al comando successivo). La riduzione flush ogni N righe non l’ha risolto; da riprovare domani (es. bypass typewriter per STATUS o altre soluzioni). In Inspector `_typewriterFlushEveryNLines` resta modificabile per sperimentare.
- **Comportamento precedente “tutti i vasi”:** La funzione `PrintStatusTable()` (stampa tutti i vasi in un colpo) è ancora presente e usa Summary + Detail per ogni vaso; non è più invocata dal flusso STATUS utente, ma può essere usata da altri punti se necessario.

---

## 5. Note tecniche

- **Causa probabile del bug “solo primo POT visibile”:** Assegnazioni frequenti a `_consoleText.text` durante il typewriter; in UI Toolkit il cambio di testo può resettare la posizione di scroll della ScrollView. Riducendo il numero di flush (solo ogni N righe) si riducono le occasioni di reset.
- **Se il bug si ripresentasse:** Si può tornare al bypass typewriter per l’output di HandleStatusPotChoice (FlushTypewriterQueueImmediate, _typewriterActive = false, poi PrintStatusSummaryTable + PrintStatusDetailForPot, FlushConsole, scroll ritardati), rinunciando all’effetto battitura per quel solo blocco.

---

*Fine DEV REPORT 0065.*
