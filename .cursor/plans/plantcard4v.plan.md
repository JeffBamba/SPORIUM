---
name: PlantCard4v - Containment Care View
overview: "Nuova vista UI Toolkit per ispezione e cura quotidiana dei POT ufficiali: chamber centrale, dati semplificati, interventi prioritizzati e VO interno."
todos: []
isProject: true
---

# PlantCard4v - Containment Care View

## Decisioni

- PlantCard4v e' una nuova esperienza di cura ravvicinata del singolo POT, non un sostituto del Terminale POT.
- Il Terminale POT resta responsabile di procedure meccaniche: `PLANT`, `UPROOT`, `STATUS`, `HARVEST` e Cryo Machine.
- La V4 viene montata tramite GameObject dedicato per POT: prima istanza `POT VIEW-001`, poi duplicabile in `POT VIEW-002`, `POT VIEW-003`, `POT VIEW-004`.
- Ogni view ha `UIDocument`, `PlantCard4vController`, `_potId` e/o `_targetPot`.
- `OSSERVARE` e' readout narrativo/diagnostico: non consuma risorse e non sostituisce `STATUS`.
- Nessun volto del player/Biologo nel VO: il parlante resta ambiguo.
- Nessun banner VO superiore: unico punto narrativo basso `VO INTERNO`.
- Nessuna schermata extra di approfondimento in V4.
- Il modulo AI e' solo promessa visuale nel Terminale: non implementa automazioni.

## Visual Target

- Layout wide orizzontale.
- Camera di contenimento dominante al centro, con overlay scanline/diagnostica.
- Colonna sinistra: identita', stato, bisogno dominante e righe tecniche compatte.
- Colonna destra: rischio dominante con causa tecnica e barra segmentata.
- Barra interventi sotto la chamber: `PRIMA`, `POI`, poi azioni disponibili attenuate.
- Box basso `VO INTERNO`: waveform, typewriter locale, pulsante `RIPETI PENSIERO`.
- Footer con solo micro-copy dello stato corrente.
- Stile Sporium: nero semitrasparente, verde spento, ambra per bisogno, rosso/arancio per rischio, bordi sottili, glow controllato, niente look cozy/cartaceo.
- Font UI Toolkit sempre >= 10px.

## Implementazione

- Nuovi asset:
  - `Assets/_Project/UI/UIToolkit/PlantCard4v/PlantCard4v.uxml`
  - `Assets/_Project/UI/UIToolkit/PlantCard4v/PlantCard4v.uss`
  - `Assets/_Project/Scripts/UI/UIToolkit/PlantCard4v/PlantCard4vController.cs`
  - `Assets/_Project/Scripts/UI/UIToolkit/PlantCard4v/PlantCard4vCareViewModel.cs`
- `PlantCard4vCareViewModel` legge `PotStateModel`, `PlantData`, `StageRequirements`, `PotSystemConfig`, `PhSystem`, `PotActions`.
- Il controller risolve il target tramite `_targetPot` o `DomePotRegistry.FindPotById(_potId)`.
- Il controller ascolta solo eventi del proprio POT e passa le azioni sempre da `PotActions`.
- `PLANT`, `HARVEST`, `UPROOT` non vengono aggiunti alla card.

## VO Interno

- Usare il register introspettivo gia' presente nel `VoOverlayController`.
- Trigger: apertura, osservazione manuale, cambio bisogno/rischio dominante, azione riuscita importante, rischio peggiorato, frutto pronto.
- Evitare ripetizioni: memorizzare ultimo `VoHintId` mostrato per POT/giorno.
- La trascrizione resta nel box basso della card.

## Terminale POT

- Aggiungere messaggio visuale non cliccabile:
  - `MODULO AI AUTOMAZIONE: NON INSTALLATO`
  - `Gestione autonoma POT non disponibile`
  - `Acquisizione modulo richiesta`
- Non implementare AI Agent in V4.

## Verifiche

- `POT VIEW-001` mostra solo `POT-001`.
- Eventi di altri POT non aggiornano la view.
- Esc chiude PlantCard4v; secondo Esc puo' aprire il menu solo se nessun modale blocca input.
- HUD fissa nascosta quando PlantCard4v e' aperta.
- Stati chiave: vuoto, vivo sano, acqua bassa, luce stressante, harvest ready, morto.
- Demo: `Il Piacere Dimenticato` leggibile in PlantCard4v; raccolta `Cetriolo d'Oro` resta Terminale.
