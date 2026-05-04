---
name: add_plant_waves_task
overview: Aggiungere un nuovo task nel piano Dome+Lab per introdurre progressivamente le specie mancanti in wave successive, con criteri di priorità, test gate e dipendenze dal Task 4.
todos:
  - id: decide-placement-task11
    content: Definire posizione definitiva del Task 11 nella sequenza del roadmap (dopo Task 10 o prima dei Gate).
    status: pending
  - id: add-frontmatter-todo
    content: Aggiungere il nuovo todo task11-plant-waves-rollout nel frontmatter con status pending.
    status: pending
  - id: write-task11-section
    content: Scrivere la sezione Task 11 con obiettivo, wave 1-3, subtask, test e done criteria.
    status: pending
  - id: align-gates-and-dependencies
    content: Aggiornare gate/dipendenze del piano per riflettere il rollout progressivo delle nuove specie.
    status: pending
isProject: false
---

# Add Task 11 Plant Waves

## Obiettivo

Inserire in `[c:\Users\UTENTE\.cursor\plans\roadmap_dome_lab_100_069d5bdb.plan.md](c:\Users\UTENTE\.cursor\plans\roadmap_dome_lab_100_069d5bdb.plan.md)` un nuovo task dedicato al rollout progressivo delle piante non ancora implementate (oltre alle 3 attuali), mantenendo coerenza con la sequenza del piano esistente e con i gate tecnici.

## Modifica proposta al piano

- Aggiungere un nuovo todo nel frontmatter:
  - `id: task11-plant-waves-rollout`
  - `content: Introdurre le specie mancanti in wave progressive (MVP->mid->full roster), con authoring PlantData, wiring runtime/UI e test di bilanciamento per Active/Passive dal lvl 1 al 5.`
  - `status: pending`
- Aggiungere una nuova sezione markdown in coda al piano:
  - `## Task 11 — Rollout progressivo nuove specie (Wave Plan)`
  - Posizionamento: dopo Task 10 e prima dei Gate finali/appendici (oppure subito prima della sezione Gate, per mantenerlo nella catena esecutiva).

## Contenuto consigliato del Task 11

- **Wave 1 (MVP runtime):** consolidare le 3 specie esistenti con poteri runtime e scaling lvl 1->5 (base per tuning).
- **Wave 2 (copertura minima famiglie):** aggiungere 1 specie per famiglia (totale 6) per validare stacking/sinergie reali.
- **Wave 3 (roster completo):** introdurre tutte le rimanenti specie Standard/Pure/Evil con QA di regressione.
- **Subtask tecnici:**
  - authoring `PlantData` e registrazione in `PlantDatabase`
  - mapping nomi/codici in terminale e HUD
  - integrazione output Lab/Item dove necessario
  - tuning cap globali (muffe, mutazioni, efficienze, resa)
- **Test gate per wave:**
  - smoke test DayCycle + save/load
  - verifica coerenza UI vs runtime powers
  - test pH/passive cap (20%) e stacking limiti
  - test regressione su Dome core loop

## Dipendenze e ordine

- Vincolare Task 11 al completamento funzionale di Task 4 (poteri runtime) almeno per Wave 1.
- Consentire Wave 2 in parallelo leggero con Task 5/8 solo se i test gate passano.
- Rimandare Wave 3 completa a dopo stabilizzazione Task 7/9 per evitare rumore di tuning durante mutazioni/discovery.

## Criteri di done del nuovo task

- Tutte le specie previste nel GDD Sezione 3 sono presenti in runtime.
- Ogni specie ha Active/Passive effettivi, scalati lvl 1->5 e con cap verificati.
- UI (HUD/terminale/foundation feedback) e gameplay sono coerenti.
- Nessuna regressione su ciclo Dome+Lab e su save/load.

