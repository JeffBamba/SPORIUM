# DEV REPORT 0099 — EOD Snapshot: struttura dati, leggibilità operativa e persistenza trend

**Data:** 2026-04-27  
**Sprint / contesto:** Iterazione UI/UX su sequenza End Of Day (step Snapshot) con focus su chiarezza informativa per il player, coerenza runtime e continuità dati tra sessioni.  
**Riferimento piano:** `end_of_day_sequence_logic` + refinements emersi da playtest chat-driven.  
**Report precedente:** `DEV_REPORT_0098_TUNING_CRESCITA_STANDARD_VS_PURE_EVIL_2026-04-27.md`

---

## Sommario interventi

1. Rifatta la schermata **Snapshot EOD** in formato operativo a sezioni (`Alert`, `Panoramica Stanze`, `Dome`, `Cucina`, `Biologo`, `Inventario`, `Seed Storage`), con valori colorati e fallback leggibili.
2. Introdotta persistenza del confronto **MEGLIO / SIMILE / PEGGIO** tra giorni anche dopo save/load.
3. Collegato il dato di **Night Research** (giorno + ramo scelto) alla visualizzazione Snapshot.
4. Tracciate e mostrate in Snapshot le operazioni **Seed Storage** giornaliere (deposito/prelievo con dettaglio item).
5. Migliorata la resa UX della comparsa testo con effetto terminale a blocchi e revisione layout UXML/USS (header inline, larghezza pannello aumentata, pulizia elementi superflui).

---

## Statistiche e progresso

### Righe di codice

- Scope misurato con:
  - `git diff --numstat -- Assets/_Project/Scripts/UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs Assets/_Project/Scripts/Core/Diary/DayActivityLog.cs Assets/_Project/Scripts/Core/Diary/DiaryStatistics.cs Assets/_Project/Scripts/Core/WikiUnlockService.cs Assets/_Project/Scripts/Core/SaveManager.cs Assets/_Project/Scripts/Systems/SeedStorage/SeedStorageSystem.cs`
  - `git diff --numstat -- Assets/_Project/UI/UIToolkit/EndOfDay/EndOfDaySequence.uxml Assets/_Project/UI/UIToolkit/EndOfDay/EndOfDaySequence.uss`
- Totale file `.cs` toccati: **+711 / -152** linee.
- Totale file UI (`.uxml`/`.uss`) toccati: **+47 / -9** linee.

### Sistemi funzionanti

- **Verificato da codice/lint:** nessun errore linter sui file toccati.
- **Da validare in Editor (Play):**
  - Snapshot EOD con nuovo impianto sezioni e resa colore valori.
  - Persistenza trend `MEGLIO/SIMILE/PEGGIO` dopo save + reload.
  - Visualizzazione night research del giorno precedente.
  - Presenza operazioni Seed Storage nel recap giornaliero.
  - Nuovo comportamento animazione testo “a blocchi terminale”.

### Bug risolti

- **6 fix funzionali/UX principali** nella Snapshot EOD:
  1. Dati critici non leggibili/mescolati in testo lungo.
  2. Trend giornaliero non persistente.
  3. Night research non visibile come esito concreto.
  4. Operazioni Seed Storage assenti nel recap.
  5. Animazione testo troppo lenta e poco coerente col tema terminale.
  6. Header metadata snapshot non allineato al titolo secondo richiesta UX.

### Progresso gameplay / prodotto

- Il player legge in modo immediato “cosa è successo oggi” senza decodificare un blocco unico di testo.
- Le aree stanza sono esplicitate con struttura stabile, pronta per icone condivise con compact bottom bar.
- Il recap supporta decisioni del giorno successivo (alert espliciti, costi fissi, stato biologo, stato storage).
- Il confronto con il giorno precedente diventa affidabile tra sessioni grazie a serializzazione dedicata.
- Migliorata coerenza diegetica con resa testuale in stile terminale a blocchi.

---

## 1. Snapshot EOD — ristrutturazione informativa

### Problema

- Layout precedente troppo narrativo/compatto: dati importanti dispersi.
- Sezioni critiche (alert, biologo, storage, condizioni stanza) non esplicitate con sufficiente gerarchia visiva.

### Soluzione

- Refactor di `PopulateSnapshot()` con composizione a sezioni operative e helper dedicati:
  - `AppendDomeSection`
  - `AppendKitchenSection`
  - `AppendBiologoSection`
  - `AppendInventorySection`
  - `AppendSeedStorageSection`
  - `AppendSeedStorageDayTransfers`
- Introduzione palette semantica (`good/warn/bad/info/muted`) per separare titoli, valori e fallback “nessun evento”.

**File interessati:**  
`Assets/_Project/Scripts/UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs`

---

## 2. Persistenza trend giornaliero e snapshot metrics

### Problema

- `MEGLIO/SIMILE/PEGGIO` basato su stato runtime non persistente: perdita confronto al riavvio.

### Soluzione

- Esteso `DiaryStatistics` con `SnapshotMetricsData` persistibile.
- Salvataggio/ripristino metrics nel `SaveManager` (`SerializeDiaryStatistics`, restore in `ApplySaveData`).
- `BuildTrendLabel` aggiornato per leggere il precedente snapshot da `DiaryStatistics`.

**File interessati:**  
`Assets/_Project/Scripts/Core/Diary/DiaryStatistics.cs`  
`Assets/_Project/Scripts/Core/SaveManager.cs`  
`Assets/_Project/Scripts/UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs`

---

## 3. Night Research e tracciamento giornaliero

### Problema

- Lo sblocco ricerca notturna avveniva, ma senza visibilità chiara nel recap del giorno.

### Soluzione

- Esteso `WikiUnlockService` con storico giorno→ramo (`RecordNightResearch`, export/import save).
- Registrazione scelta ricerca in `OnResearchChosen`.
- Lettura “ricerca della notte precedente” in Snapshot.

**File interessati:**  
`Assets/_Project/Scripts/Core/WikiUnlockService.cs`  
`Assets/_Project/Scripts/UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs`

---

## 4. Seed Storage nel recap (deposito/prelievo)

### Problema

- Azioni Seed Storage compiute dal player non comparivano nel riepilogo giornaliero.

### Soluzione

- Nuova struttura `SeedStorageEntry` in `DayActivityLog`.
- Hook in `SeedStorageSystem` su deposito e prelievo con dettaglio item movimentati.
- Rendering nel blocco `Seed Storage` della Snapshot.

**File interessati:**  
`Assets/_Project/Scripts/Core/Diary/DayActivityLog.cs`  
`Assets/_Project/Scripts/Systems/SeedStorage/SeedStorageSystem.cs`  
`Assets/_Project/Scripts/UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs`

---

## 5. UX visuale Snapshot (animazione + layout UXML/USS)

### Problema

- Animazione comparsa testo percepita come lenta/non coerente.
- Metadati header e larghezza pannello non allineati al comportamento atteso.
- Presenza elementi superflui (`FRAMMENTO S.P.O.R.A.E`) e struttura placeholder icone non corretta rispetto ai titoli sezione.

### Soluzione

- Sostituito typewriter lineare con reveal “terminale DOS a blocchi” (`TerminalChunkReveal`).
- Header snapshot riorganizzato: titolo + `data | stato vault | pH` sulla stessa riga.
- Pannello Step 2 allargato e scroll area aumentata per ridurre scrolling verticale.
- Rimosso footer “Frammento SPORAE”.
- Inseriti placeholder area icon naming stabile (`eod-room-icon-*`) e successiva convergenza verso posizionamento accanto ai titoli paragrafo via struttura testuale corrente.

**File interessati:**  
`Assets/_Project/UI/UIToolkit/EndOfDay/EndOfDaySequence.uxml`  
`Assets/_Project/UI/UIToolkit/EndOfDay/EndOfDaySequence.uss`  
`Assets/_Project/Scripts/UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs` | Refactor Snapshot, sezioni operative, alert espliciti, animazione terminale a blocchi, helper dati |
| `Assets/_Project/Scripts/Core/Diary/DayActivityLog.cs` | Nuovi eventi stage-change e seed-storage giornalieri |
| `Assets/_Project/Scripts/Core/Diary/DiaryStatistics.cs` | Snapshot metrics persistibili per confronto tra giorni |
| `Assets/_Project/Scripts/Core/WikiUnlockService.cs` | Persistenza storico night research per giorno |
| `Assets/_Project/Scripts/Core/SaveManager.cs` | Serializzazione/deserializzazione `DiaryStatisticsData` estesa |
| `Assets/_Project/Scripts/Systems/SeedStorage/SeedStorageSystem.cs` | Tracking deposito/prelievo verso `DayActivityLog` |
| `Assets/_Project/UI/UIToolkit/EndOfDay/EndOfDaySequence.uxml` | Header snapshot inline + pulizia blocchi superflui + placeholders area |
| `Assets/_Project/UI/UIToolkit/EndOfDay/EndOfDaySequence.uss` | Layout più largo/alto, styling header/meta e adeguamento snapshot |

---

## Regole / vincoli rispettati

- Coerenza runtime services: uso `ServiceContainer` per servizi globali (`DiaryStatistics`, `DayActivityLog`, `WikiUnlockService`, `DomePotRegistry`).
- Refactor incrementale senza introdurre branch demo/full separati.
- Intervento UI Toolkit mantenuto su UXML/USS + controller esistente (nessun pannello runtime parallelo separato).
- Nessun comando distruttivo git; preservate modifiche preesistenti in working tree.

---

## Note operative (Unity)

- Validare in `SCN_VaultMap` il comportamento snapshot su:
  - giorni con eventi reali Dome/Lab/SeedStorage;
  - save/load intermedio per conferma persistenza trend;
  - vari livelli di densità testo nel pannello allargato.
- Prossimo step già previsto: portare il placeholder icona da forma testuale a binding visuale diretto per i titoli paragrafo della schermata successiva EOD.

---

*Fine DEV REPORT 0099.*
