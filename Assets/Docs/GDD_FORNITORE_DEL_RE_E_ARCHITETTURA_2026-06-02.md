# FORNITORE DEL RE

Game Design Document completo + infrastruttura tecnica ragionata.

Versione consolidata: 2026-06-02
Stato: Draft operativo (base di lavoro)
Lingua: ITA

---

## 0) Concept sintetico

**Fornitore del Re** e un gestionale strategico medievale con:
- fase di mercato (dinamica, opportunita immediate),
- fase di preparazione (strategica, pianificazione),
- progressione sociale da mercante povero a fornitore indispensabile della Corona.

Il fantasy non e "costruire citta" o "combattere in prima linea", ma dominare:
merci, reputazione, filiere, logistica, informazione, relazioni e sotterfugio.

---

## 1) Fantasy del player

Il player parte senza prestigio e senza rete.
All'inizio sopravvive con baratto e beni comuni.
Nel medio periodo costruisce:
- accesso ai mercati giusti,
- reputazione verso emissari e castello,
- accordi di fornitura stabili,
- filiere artigianali,
- vantaggio sui rivali.

End goal: diventare il mercante che il potere non puo sostituire.

---

## 2) Struttura settimanale

Ogni settimana e divisa in due macro-fasi.

### Fase A - Mercato (lun/mar/mer)

Durante il mercato il player gestisce solo azioni legate al banco:
- vendite,
- baratti,
- scambi a catena,
- osservazione domanda/prezzi,
- intercetto emissari.

Banco:
- 5 slot espositivi,
- 1 slot scorta nel carretto.

Solo merci esposte = merci vendibili/barattabili/notabili.
Le merci possono essere rimpiazzate in giornata per creare catene di opportunita.

### Fase B - Preparazione (gio/ven/sab/dom)

Fase strategica.
Niente "punti azione rigidi" di base: il tempo avanza solo a **Fine Giornata**.

Obiettivo: preparare la settimana successiva tramite:
- rete fornitori,
- magazzino,
- filiere,
- logistica,
- informazioni,
- relazioni,
- sabotaggi base.

---

## 3) Modi per ottenere merci

### 3.1 Mercato
- baratto,
- compravendita,
- visitatori,
- opportunita dinamiche.

Fonte piu imprevedibile ma ad alta leva.

### 3.2 Altri mercanti (fuori mercato)
- compri/vendi/baratti tra giovedi e domenica.
- via rapida ma spesso piu cara.

### 3.3 Fornitori esterni

Regola cardine:
**non vendono a spot**.

Per ricevere merci serve un **accordo di fornitura attivo**.
Il gioco premia costruzione rete, non shopping estemporaneo.

---

## 4) Fornitori esterni

I fornitori sono risorse contese da player e rivali.

### Parametri core fornitore
1. Quantita
2. Prezzo/compenso
3. Qualita
4. Affidabilita

### Categorie
- Scarso
- Comune
- Pregiato

### Accordi di fornitura

Ogni accordo definisce:
- merce,
- quantita,
- compenso,
- ricorrenza,
- giorno consegna,
- affidabilita.

I migliori accordi si ottengono con:
- offerta migliore,
- reputazione,
- tempismo,
- continuita.

### Concorrenza

I rivali possono:
- offrire di piu,
- strappare fornitori,
- sabotare consegne,
- deteriorare relazioni.

---

## 5) Tempi (ricerca e consegne)

### Consegna accordi attivi
- conferma giovedi -> consegna sabato
- conferma venerdi -> consegna domenica
- conferma sabato/domenica -> settimana successiva

### Ricerca nuovi fornitori
- invio emissari fuori mura
- durata ricerca: 3 giorni
- trattativa immediata se fornitore trovato

Esito influenzato da:
- area,
- scenario,
- sicurezza rotte,
- reputazione,
- disponibilita merce,
- pressione rivali.

---

## 6) Azioni core fuori mercato (gio-dom)

1. Gestire accordi fornitura
2. Cercare nuovi fornitori
3. Gestire magazzino
4. Preparare banco/carretto
5. Raccogliere informazioni
6. Gestire artigiani/filiere
7. Organizzare logistica
8. Trattare con rivali
9. Sabotaggi base

---

## 7) Sistemi rimandati (futuro)

Non core immediato:
- merci non dichiarate/contrabbando avanzato,
- debiti e finanza avanzata,
- politica di corte avanzata.

---

## 8) Core loop

Leggi info -> gestisci fornitori -> organizzi merci -> prepari banco -> scegli mercato -> vendi/baratti -> intercetti emissari -> cresci reputazione -> accedi a bandi -> espandi rete e filiere.

---

## 9) Mercati della cittadella

Mercati multipli con focus diversi (es. Basso, Gilde, Chiesa, Nobile, Porte).

Il focus puo cambiare anche giornalmente in base a:
- eventi,
- meteo,
- guerre,
- scarsita,
- visite,
- rumors.

---

## 10) Emissari

Gli emissari collegano mercato e potere.
Si fermano dal player se c'e allineamento tra:
- mercato scelto,
- merce esposta,
- qualita,
- reputazione,
- prezzo,
- sospetto.

Ricompense:
- reputazione,
- fiducia,
- accesso bandi.

---

## 11) Baratto e denaro

Early game:
- oro raro,
- baratto dominante.

Mid/late:
- cresce ruolo denaro tramite nobili, bandi, grandi contratti.

Il baratto resta utile tutta la run.

---

## 12) Bandi nobiliari e reali

Contratti di fornitura con premi e rischi:
- successo: oro, prestigio, fiducia, bandi migliori.
- fallimento: penalita reputazione/fiducia, esclusioni, vantaggio rivali.

---

## 13) Filiere artigianali

Player non crafta a mano: coordina catena.

Livelli:
1. materie prime
2. beni lavorati
3. beni di pregio

Piu valore = piu costo, tempo e vulnerabilita logistica.

---

## 14) Artigiani

Risorse limitate e contese.
Attributi principali:
- specializzazione,
- qualita,
- costo,
- tempi,
- affidabilita,
- lealta.

---

## 15) Logistica

Trade-off costante:
**veloce / economico / sicuro** (non tutto insieme).

Rischi:
- ritardi,
- furti,
- deterioramento,
- blocchi rotte,
- sabotaggi.

---

## 16) Rivali

NPC mercanti con stili diversi.

Possono:
- occupare mercati,
- bloccare fornitori,
- competere su bandi,
- fare accordi opportunisti,
- sabotare.

---

## 17) Informazioni

Tre fonti:
- Gazzettino: certo ma parziale,
- Rumors: anticipati ma incerti,
- Informatori: costosi ma specifici.

---

## 18) Sotterfugio e Sospetto

Sotterfugio abilita sabotaggi/manipolazioni leggere.
Sospetto misura la reazione sociale/istituzionale al comportamento del player.

Sospetto alto penalizza:
- fiducia emissari,
- tolleranza guardie,
- relazioni con attori rispettabili.

---

## 19) Eventi randomici

Durante preparazione possono avvenire eventi che alterano priorita della settimana successiva.
Devono essere leggibili ma non totalmente prevedibili.

---

## 20) Scenari e run

Ogni run parte con scenario iniziale (pace, assedio, carestia, ecc.) e puo evolvere nel tempo per eventi e scelte.

---

## 21) Vittoria

Titolo di **Fornitore Reale** ottenuto tramite:
- grandi bandi riusciti,
- reputazione/fiducia/prestigio,
- rete fornitori robusta,
- filiere stabili,
- dominio strategico su crisi e rivali.

La vittoria e economica + sociale + politica.

---

## 22) Pillars

1. Tensione del banco
2. Adattamento strategico
3. Gestione umana
4. Logistica e filiere
5. Informazione
6. Rete di fornitura
7. Sotterfugio controllato

---

## 23) Formula finale

Merchant survival strategy medievale:
prepari rete e merci fuori mercato, poi le metti alla prova in tre giorni di banco.

---

## 24) Naming (working titles)

Preferenze emerse:
- **The King's Merchant**
- **The King's Supplier**

Alternative vicine:
- In Service of the King
- The King's Will
- The Ways of the Market

---

# INFRASTRUTTURA TECNICA RAGIONATA

Questa sezione traduce il design in architettura implementabile.

## A) Principi architetturali

1. **Simulation-first**: il gioco deve funzionare senza UI.
2. **Single source of truth**: stato centralizzato e osservabile.
3. **Data-driven**: contenuti e bilanciamento fuori dal codice hardcoded.
4. **Determinismo controllato**: RNG seedato e ripetibile per debug.
5. **Incrementale**: MVP verticale prima di espansione sistemi.

---

## B) Architettura a layer (consigliata)

### Domain (core)
- regole pure, zero dipendenze Unity/UI.

### Application
- use case, comandi, validazioni, orchestrazione regole.

### Infrastructure
- persistenza, random provider, import/export dati.

### Presentation
- UI, scene, input, feedback.

---

## C) Modello dati minimo (MVP)

## C.1 WorldState
- day/week/phase,
- scenario attivo,
- seed RNG,
- meteo/event flags.

## C.2 EconomyState
- prezzi per categoria/mercato,
- domanda/offerta locali.

## C.3 ActorState
- player,
- rivali,
- fornitori,
- artigiani,
- emissari.

## C.4 ContractState
- accordi fornitori,
- bandi,
- esiti/penali.

## C.5 LogisticsState
- rotte, rischio, tempi, capacita, consegne pianificate.

## C.6 IntelState
- notizie certe,
- rumors con affidabilita,
- info comprate.

---

## D) Comandi + Eventi (pattern consigliato)

Input player -> `Command`

Esempi:
- `NegotiateSupplierCommand`
- `AssignRouteCommand`
- `PrepareStallCommand`
- `RunSabotageCommand`

Output sistema -> `DomainEvent`

Esempi:
- `SupplierAgreementSigned`
- `DeliveryDelayed`
- `RumorDebunked`
- `EmissaryVisited`
- `BidFailed`

Vantaggi:
- testabilita alta,
- replay/debug,
- salvataggi robusti,
- telemetria naturale.

---

## E) Time & scheduling

State machine:
- `MarketPhase` (lun-mar-mer)
- `PreparationPhase` (gio-ven-sab-dom)

Scheduler unico per:
- consegne (2 giorni),
- ricerche fornitori (3 giorni),
- lavorazioni artigiani,
- eventi world-state.

---

## F) AI rivali (fase iniziale)

Approccio consigliato:
**Utility-based AI** (non GOAP complesso subito).

Utility functions principali:
- profitto,
- prestigio,
- resilienza rete,
- danno competitivo.

Azioni AI:
- overbid su fornitori,
- lock mercato,
- sabotage opportunistico,
- difesa filiera.

---

## G) Persistenza

Salvare:
- snapshot completo stato,
- coda scheduler,
- storico eventi recente,
- seed RNG.

Requisito:
- versioning save schema (migrazioni future).

---

## H) Dati e tuning

Configurazioni esterne (SO/JSON/csv):
- merci,
- mercati,
- fornitori/artigiani,
- eventi/scenari,
- emissari,
- curve reputazione/sospetto.

No valori gameplay critici hardcoded in controller UI.

---

## I) Telemetria interna (obbligatoria)

Tracciare almeno:
- variazioni prezzo per categoria/mercato,
- causa fallimento consegna,
- motivi accettazione/rifiuto emissario,
- trigger di perdita reputazione/fiducia,
- uso sabotaggi + impatto.

Serve per tuning, QA e bilanciamento.

---

## J) Vertical Slice MVP (consigliato)

Scope ridotto ma completo:
- 1 scenario,
- 2 mercati,
- 8-12 merci,
- 3 rivali,
- 6 fornitori,
- 4 artigiani,
- 1 tipo bando,
- 1 sabotaggio base.

Goal MVP:
una settimana completa giocabile end-to-end.

---

## K) Sequenza di sviluppo consigliata

1. Core state + scheduler
2. Market loop base
3. Supplier agreements + delivery pipeline
4. End-week flow + bando base
5. Rival AI utility base
6. Informazioni (gazzettino/rumors)
7. Sabotaggio base
8. Bilanciamento + UX pass

---

## L) Rischi principali e mitigazioni

### Rischio 1: complessita sistemica troppo presto
Mitigazione: vertical slice strettissima, feature gates.

### Rischio 2: UI iper-densa
Mitigazione: progressive disclosure, priorita "next best action".

### Rischio 3: tuning ingestibile
Mitigazione: data-driven + telemetry + test scenari seedati.

### Rischio 4: AI rivali poco leggibile
Mitigazione: utility semplice + log motivazioni AI.

---

## M) KPI di validazione alpha

1. Time-to-first-understanding del loop (< 10 minuti)
2. Numero run completate senza onboarding assistito
3. Chiarezza conseguenze scelte (survey/test)
4. Tasso uso sistemi core (fornitori, logistica, emissari)
5. Frizione UX in EoD e transizione fase

---

## N) Decisione strategica attuale

Prima si completa implementazione del piano previsto.
Poi si valuta:
- efficacia commerciale,
- livello di divertimento,
- priorita post-alpha.

---

Fine documento.

