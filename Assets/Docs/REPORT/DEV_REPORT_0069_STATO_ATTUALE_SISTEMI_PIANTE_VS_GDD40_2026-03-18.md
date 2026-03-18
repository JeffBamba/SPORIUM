# DEV REPORT 0069 — Stato attuale sistemi piante vs GDD 40

**Data:** 2026-03-18  
**Oggetto:** Valutazione dello stato attuale dei sistemi piante rispetto al report storico `ANALISI_SISTEMI_PIANTE_GDD40_vs_REPOMAIN.txt` e rispetto al GDD 40, con focus su cosa è migliorato davvero, cosa oggi è solido nel loop gameplay e quali gap strutturali restano ancora aperti.  
**Riferimenti:** `Assets/Docs/REPORT/ANALISI_SISTEMI_PIANTE_GDD40_vs_REPOMAIN.txt`, `Assets/Docs/REPORT/CONFRONTO_GDD40_vs_IMPLEMENTAZIONE_COMPLETO_2026-01-04.md`, `Assets/_Project/Scripts/Dome/`, `Assets/_Project/Scripts/Core/PhSystem.cs`.  
**Report precedente:** `Assets/Docs/REPORT/DEV_REPORT_0068_REFACTOR_ARCHITETTURA_A_FASI_SERVICECONTAINER_REGISTRY_PROCESSOR.md`

---

## 1. Contesto

- È stata richiesta una valutazione qualitativa dello stato attuale del gioco lato **sistemi piante**, confrontandolo con il vecchio report `ANALISI_SISTEMI_PIANTE_GDD40_vs_REPOMAIN.txt`.
- L’obiettivo non è produrre un audit “meccanico” file-per-file, ma esprimere un giudizio concreto su:
  - come eravamo nel report storico,
  - come siamo messi oggi,
  - cosa è davvero entrato nel loop giocabile,
  - cosa manca ancora per poter dire “conforme al GDD 40” in senso pieno.

---

## 2. Valutazione sintetica

### Giudizio breve

Il mio giudizio attuale è questo:

- **prima:** il sistema piante aveva fondamenta forti ma molte parti avanzate erano ancora deboli, parziali o solo predisposte;
- **oggi:** il **loop core delle piante è solido**, leggibile e molto più vicino al design reale;
- **restano scoperti soprattutto i sistemi avanzati o endgame**: mutazioni, slot passivi, ibridi, compost/lab completo e una chiusura totale della conformità fertilizzanti.

### Valutazione complessiva

- **Stato storico percepito:** area ~60–65% del sistema piante “realmente giocabile” rispetto alla visione GDD.
- **Stato attuale percepito:** area ~75–85%, a seconda di quanto peso si dà alle feature avanzate non ancora presenti.

In altre parole:

- se guardiamo il **gioco che il player può già vivere**, siamo molto più avanti del report storico;
- se guardiamo la **piena aderenza al GDD 40**, ci sono ancora gap importanti.

---

## 3. Cosa è migliorato davvero rispetto al report storico

### 3.1 Il loop core piante è oggi credibile e stabile

Rispetto al report storico, le parti che oggi considero **davvero consolidate** sono:

- stadi di crescita;
- requisiti per stadio;
- watering persistente;
- harvest;
- pruning;
- mold risk / infestazione;
- buona parte del sistema livelli;
- condizione/stress come leva gameplay;
- integrazione pH molto più incisiva;
- LED persistente molto più maturo.

Quindi il passaggio più importante non è solo “abbiamo più codice”: è che **il gameplay delle piante oggi regge molto meglio come sistema coerente**.

### 3.2 pH: da presenza di base a sistema che incide sul comportamento

Nel report storico il pH era presente ma ancora giudicato solo parzialmente conforme per gli effetti estremi e i modificatori avanzati.

Oggi il codice mostra:

- `PhGrowthModifier` dedicato;
- uso dei modificatori pH su crescita e resa;
- sterilità in casi specifici;
- sinergia con il sistema mold;
- lettura e presentazione più integrata anche nel terminale/UI.

Giudizio attuale:

- non più “base con lacune grosse”;
- oggi è **quasi completo / sostanzialmente completo per il loop corrente**.

### 3.3 condizioni pianta: da score a meccanica di gameplay

Nel report storico il sistema condizioni era ancora percepito come presente ma parziale lato impatto reale e visualizzazione.

Oggi vedo:

- `ConditionGrowthModifier`;
- modificatori espliciti su crescita e produzione;
- blocco avanzamento in certe condizioni;
- forecast e lettura più chiara nel terminale.

Giudizio attuale:

- il sistema non è più solo informativo;
- oggi è **parte reale del gameplay**.

### 3.4 LED: salto netto rispetto al passato

Nel report storico il LED era ancora molto incompleto: mancavano Burn Stress serio, giorni consecutivi valorizzati e visibilità piena.

Oggi risultano presenti:

- tracking giorni consecutivi;
- `DaysBurnStressConsecutive`;
- regressione stage e riduzione livello in caso di abuso;
- integrazione giornaliera nel `DayCycleController`;
- migliore leggibilità in terminale/HUD.

Giudizio attuale:

- il sistema LED oggi è **molto più vicino al GDD**;
- non lo considero perfettamente chiuso, ma sicuramente non più “parziale debole”.

### 3.5 livelli pianta: più maturi e più integrati nel gameplay

Nel report storico mancava ancora una parte importante della resa basata sul livello.

Oggi risultano presenti:

- `CompletedCycles`;
- progressione tramite `PlantLevelSystem`;
- level up integrato al ciclo fertilizzazione;
- modificatori resa/qualità in `DoHarvest()`;
- check per passive slot a livello 5.

Giudizio attuale:

- oggi il sistema livelli è **quasi completo**;
- il vero buco residuo è sugli slot passivi.

---

## 4. Cosa oggi considero ancora parziale

### 4.1 Fertilizzanti

È migliorato, ma non lo considero ancora chiuso.

Oggi risultano presenti:

- `FertilizerType`;
- costi e quantità;
- compatibilità codificata in `FertilizerSystem`;
- tracking `FertilizerLevel` e `DaysFertilizerActive`;
- decay giornaliero;
- transizione `Resting -> Flowering`.

Tuttavia restano dubbi o gap sul fatto che il tutto sia **pienamente enforced nel flow gameplay reale** e non solo presente come infrastruttura.

Giudizio attuale:

- **meglio di prima**;
- ma ancora **parziale** rispetto al GDD completo.

### 4.2 Visualizzazione e leggibilità alcune aree

Anche se il terminale ha fatto un salto grosso, non tutto il sistema piante è ancora espresso in modo uniforme in HUD e flussi esterni.

Questa non è più la criticità principale, ma resta una zona che può essere rifinita.

---

## 5. Cosa manca ancora davvero

Qui stanno i veri gap che impediscono di dire “sistema piante completo rispetto al GDD 40”.

### 5.1 Mutazioni

Questo resta il buco più grosso.

Non emerge un vero sistema completo con:

- `MutationSystem`;
- `MutationScore`;
- trigger strutturati;
- applicazione effetti;
- UI dedicata.

Giudizio:

- **mancante**.

### 5.2 Slot passivi

Oggi c’è il gating base (`CanMoveToPassiveSlot()`), ma il sistema non esiste ancora davvero:

- niente slot reali;
- niente UI dedicata;
- niente bonus passivi;
- niente cap reale al pH drift.

Giudizio:

- **mancante come feature reale**.

### 5.3 Compost / LAB-CMP-001

Si vedono placeholder e predisposizioni, ma non un flusso completo di produzione fertilizzanti via compost.

Giudizio:

- **mancante**.

### 5.4 Ibridi

Ancora fuori dal loop reale.

Giudizio:

- **mancante**.

### 5.5 Chiusura fine del sistema LED / fertilizzanti

Anche se molto migliorati, restano aree di fine tuning:

- scaling diretto LED come da GDD;
- eventuali costi notturni specifici;
- enforcement totale delle compatibilità fertilizzanti nel flusso gameplay.

Giudizio:

- **quasi completi ma non del tutto chiusi**.

---

## 6. Confronto sintetico “prima vs oggi”

| Area | Report storico | Giudizio oggi |
|------|----------------|---------------|
| Stadi crescita | Completi | Completi |
| Stage requirements | Completi | Completi |
| Watering | Completo | Completo e solido |
| Harvest | Completo | Completo |
| Pruning | Completo | Completo |
| Mold system | Completo | Completo |
| pH | Base buona ma ancora parziale sugli effetti estremi | Molto più completo e incisivo |
| Condizioni pianta | Presente ma parziale | Oggi parte reale del gameplay |
| LED persistente | Parziale | Quasi completo / molto più maturo |
| Livelli pianta | Parziale | Quasi completo |
| Fertilizzanti | Parziale | Ancora parziale, ma più avanti |
| Mutazioni | Mancanti | Mancanti |
| Slot passivi | Mancanti | Mancanti (solo gating base) |
| Ibridi | Mancanti | Mancanti |
| Compost | Mancante | Mancante |

---

## 7. Conclusione

Il punto chiave è questo:

**oggi siamo messi bene sul “gioco delle piante che funziona davvero”, ma non ancora sul “gioco delle piante completo come immaginato integralmente nel GDD 40”.**

Quindi:

- il **cuore del sistema** oggi esiste ed è molto più robusto di quanto risultava dal report storico;
- il **debito residuo** non è più sul loop base, ma sulle feature avanzate / sistemiche:
  - mutazioni,
  - slot passivi,
  - ibridi,
  - compost,
  - chiusura totale del ramo fertilizzanti.

Se devo riassumerlo in una frase operativa:

> Il sistema piante non è più “fondamenta + promesse”, ma è ormai un sistema giocabile e credibile; ciò che manca oggi è soprattutto la profondità avanzata prevista dal GDD.

---

## 8. Raccomandazione

Se il prossimo obiettivo è avvicinarsi davvero al GDD 40, il percorso più sensato non è rifinire ancora il core base, ma aprire nell’ordine:

1. **slot passivi**,  
2. **mutazioni**,  
3. **chiusura fertilizzanti/compost**,  
4. **ibridi**.

Questi quattro punti sono quelli che oggi separano il sistema piante “solido” dal sistema piante “completo”.

---

*Report redatto come valutazione qualitativa dello stato attuale dei sistemi piante rispetto al report storico GDD40 vs REPOMAIN e al codice oggi presente nel repository.*
