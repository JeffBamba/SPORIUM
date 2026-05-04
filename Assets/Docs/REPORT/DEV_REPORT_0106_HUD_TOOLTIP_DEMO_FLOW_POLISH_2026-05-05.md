# DEV REPORT 0106 — HUD tooltip, DomeStatusHUD compatto, mission recap e piano demo choice-driven

**Data:** 2026-05-05  
**Sprint / contesto:** Polish UX demo/full — leggibilità HUD UI Toolkit, tooltip contestuali, stato iniziale DomeStatusHUD, piano narrativo post Beat 3.  
**Riferimento piano:** `.cursor/plans/demo_alpha_1_0_gap_map.plan.md`  
**Report precedente:** `DEV_REPORT_0105_DEMO_BEAT3_MISSIONI_VO_LAYERING_2026-05-03.md`

---

## Sommario interventi

1. Aggiornato il piano demo Alpha post Beat 3 in chiave **choice-driven**, con micro-contratto Mercante Ombra, rami cooperazione/rottura patto, `VO Prompt Choice`, flag demo e specifica Cetriolo d'Oro / Il Piacere Dimenticato.
2. Aggiunti piani operativi staged per **Dome/Lab roadmap 100%**, rollout piante a wave ed **Elevator 2.0**, più aggiornamenti demo/config e asset player collegati al pacchetto.
3. Migliorata la leggibilità di **VO Overlay** e **Mission Recap** con fondi opachi semi-trasparenti, bordi arrotondati e background scuro verde per missioni attive/completate.
4. Corretto il tooltip delle **toast notification**: sparisce insieme al toast sorgente e si posiziona a sinistra della notifica per ridurre sovrapposizioni.
5. Aggiunto warning visivo informativo alla barra **AZIONI**: l'ultimo quadratino disponibile lampeggia, usando il colore corrente delle azioni, quando il player rischia di perdere 1 azione al prossimo giorno se non mangia.
6. Aggiornato il tooltip **AZIONI**: nel breakdown giornaliero spiega il motivo del blink e la perdita prevista; la riga lunga ora va a capo senza sovrapporsi al valore `-1`.
7. Aggiunto messaggio di conferma dedicato per bere **acqua sporca**, con warning placeholder sul rischio contaminazione e futuri malus.
8. Rifinito **Mission Recap**: label `MISSIONI` e count allineati verticalmente ai pulsanti filtro.
9. Corretto **DomeStatusHUD**: in Play parte compatto/non aperto; si apre dalla linguetta o automaticamente quando un POT passa da vuoto a piantato.

---

## Statistiche e progresso

### Righe di codice

- **Ambito dichiarato:** file `.cs` toccati dall'intervento e inclusi in questo report.
- **Comando:** PowerShell `Get-Content <file> | Measure-Object -Line`.
- **Totale righe sui 6 file .cs:** **6475**.
  - `FoundationNotificationsPanelController.cs`: 442
  - `SegmentedBarUI.cs`: 106
  - `TopBarController.cs`: 2437
  - `PlayerInventoryPanelController.cs`: 1327
  - `DomeStatusHUDController.cs`: 1271
  - `LocalizationManager.cs`: 892
- **Delta `git diff` misurato sui .cs del report:** **+215 / -28**.
- **Delta staged complessivo al controllo report:** **+2830 / -193** su file testuali, con **1 asset binario** staged (`test_2.png`). Comando: `git diff --cached --numstat`.

### Sistemi funzionanti

- **Verifica statica eseguita:** `git diff --check` sui file toccati nei singoli interventi, con soli warning LF/CRLF.
- **Da validare in Editor:** Play Mode full/demo per DomeStatusHUD inizialmente compatto; piantagione in POT con auto-open; hover tooltip azioni; conferma acqua sporca; tooltip toast con scadenza del toast sorgente.

### Bug risolti

- **7** bug / regressioni UX documentate in sessione:
  1. Tooltip toast che restava visibile dopo la sparizione del toast sorgente.
  2. Tooltip toast sovrapposto a pile di notifiche multiple.
  3. VO poco leggibile su sfondi variabili.
  4. Riga lunga nel breakdown tooltip AZIONI sovrapposta al valore `-1`.
  5. Uso di acqua sporca senza warning di rischio contaminazione.
  6. Label `MISSIONI` non centrata verticalmente rispetto ai filtri.
  7. DomeStatusHUD aperto all'ingresso in Play nonostante il comportamento atteso fosse compatto.

### Progresso gameplay / prodotto

- Il player riceve un segnale visivo chiaro quando rischia di perdere un'azione domani per fame, e il tooltip spiega il perché.
- Le notifiche e i VO sono più leggibili e meno inclini a lasciare tooltip fantasma.
- L'inventario comunica meglio il rischio dell'acqua sporca senza bloccare ancora il consumo.
- Il Mission Recap appare più allineato e coerente con la top bar/filtro.
- Il DomeStatusHUD non invade più la scena all'avvio e si apre solo per scelta del player o quando il sistema ha qualcosa di utile da mostrare.
- Il piano demo post Beat 3 è più implementabile come sequenza di scelte piccole, flag e riconvergenze controllate.
- Il team ha anche piani staged per roadmap Dome/Lab, rollout nuove specie ed Elevator 2.0, utili come guida operativa successiva.

---

## 1. Piano demo post Beat 3 choice-driven

### Problema

- Il piano precedente conservava tracce superate sul Beat 4 e non rifletteva la direzione narrativa aggiornata: micro-contratto del Mercante Ombra, scelta di responsabilità, rami cooperazione/rottura patto e riconvergenza Lab/Dome/finale.
- Serviva chiarire che **Cetriolo d'Oro** è un item Fruit, mentre **Il Piacere Dimenticato** è la specie/item Plant demo-only.

### Soluzione

- Riscrittura del piano post Beat 3 con pattern `VO Prompt Choice`: prompt VO, scelta Yes/No, flag, conseguenza immediata e next objective.
- Inseriti flag demo dedicati: `willPreparePayment`, `willMeetMerchantToday`, `merchantPaymentOutcome`, `fruitsUse`, `finalOutcome`, `merchantTrustDelta`, `merchantChannelClosed`, `lateDebtAccepted`.
- Aggiunta specifica contenutistica su Mercante Ombra, Visitor Desk limitato alla missione del Cetriolo d'Oro, Lab/Dome scriptati e non trasformati in sistemi full trade/reputazione.

**File interessati:**  
`.cursor/plans/demo_alpha_1_0_gap_map.plan.md`, `Assets/_Project/Docs/VISITOR_DESK_SPEC.md`

---

## 2. Piani operativi staged per roadmap successive

### Problema

- Oltre al polish demo/HUD, il pacchetto staged contiene materiale di pianificazione che rischiava di restare fuori dal report: roadmap Dome/Lab 100%, rollout progressivo nuove specie ed Elevator 2.0.

### Soluzione

- Documentato nel report il perimetro staged dei piani.
- La roadmap Dome/Lab include task progressivi fino a pulizia tech debt, rollout specie e ciclo Harvest -> Frutto/Prodotto -> consumo.
- Il piano Elevator 2.0 registra un rollout incrementale a basso rischio partendo dalla baseline legacy.
- Il piano plant waves definisce wave e gate per introdurre specie mancanti senza esplodere il runtime.

**File interessati:**
`.cursor/plans/roadmap_dome_lab_100_069d5bdb.plan.md`,
`.cursor/plans/add_plant_waves_task_01f0b67e.plan.md`,
`.cursor/plans/elevator_2.0_13afcdbe.plan.md`

---

## 3. Leggibilità VO, Mission Recap e toast tooltip

### Problema

- Il VO poteva perdere contrasto contro fondali ricchi.
- Le card Mission Recap attive/completate avevano bisogno di un background più leggibile.
- Il tooltip dei toast restava visibile se il mouse rimaneva fermo sopra la zona dopo la rimozione del toast e poteva sovrapporsi a più notifiche.

### Soluzione

- Aggiunto fondo opaco ma non pieno al VO, con bordi arrotondati.
- Applicato background scuro virato verde alle missioni attive/completate nel recap.
- Aggiornato il controller Foundation Notifications per nascondere il tooltip quando il toast sorgente scompare e posizionarlo a sinistra della notifica.

**File interessati:**  
`Assets/_Project/Resources/UI/UIToolkit/VoOverlay/VoOverlay.uss`,  
`Assets/_Project/Resources/UI/UIToolkit/ActiveMissions/ActiveMissions.uss`,  
`Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/FoundationNotificationsPanelController.cs`,  
`Assets/_Project/UI/UIToolkit/NotificationsFoundation/NotificationsPanel.uss`,  
`Assets/_Project/UI/UIToolkit/NotificationsFoundation/NotificationsPanel.uxml`

---

## 4. TopBar AZIONI: blink informativo e tooltip esplicativo

### Problema

- Il player poteva vedere una perdita azioni solo a posteriori, senza segnale preventivo.
- Il blink iniziale era percepito come warning danger se forzato rosso, anche quando il cap era ancora alto.
- La spiegazione nel tooltip era necessaria ma la riga lunga del breakdown sovrapponeva testo e valore.

### Soluzione

- `TopBarController` ora legge lo stato fame esistente (`ConsecutiveDaysWithoutMeal`, `AteMealSincePreviousDawn`) senza cambiare la logica del budget.
- L'ultimo segmento pieno lampeggia solo quando il prossimo giorno senza pasto causerebbe perdita di cap; il colore è quello corrente della barra (verde/giallo/rosso secondo logica già esistente).
- `SegmentedBarUI` espone `SegmentCount` e `GetSegment(int)` per aggiornare solo il segmento interessato.
- Nel breakdown tooltip viene aggiunta una riga `Prossima alba senza pasto` con `-1` e dettaglio localizzato; il layout riga ora usa testo in colonna e valore a destra.

**File interessati:**  
`Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs`,  
`Assets/_Project/Scripts/UI/UIToolkit/HUD/Components/SegmentedBarUI.cs`,  
`Assets/_Project/UI/UIToolkit/HUD/TopBar.uss`,  
`Assets/_Project/Scripts/Core/Localization/LocalizationManager.cs`

---

## 5. Inventario: conferma acqua sporca

### Problema

- `Items.Water` e `Items.WaterPotable` erano entrambi consumabili, ma il messaggio di conferma non distingueva l'acqua sporca dal consumo sicuro.

### Soluzione

- `PlayerInventoryPanelController.RequestUse` seleziona una chiave localizzata dedicata quando `m.TypeId == Items.Water`.
- Aggiunta stringa IT/EN: avvisa del rischio contaminazione e dichiara che i malus su idratazione/azioni sono placeholder futuri.

**File interessati:**  
`Assets/_Project/Scripts/UI/UIToolkit/PlayerInventory/PlayerInventoryPanelController.cs`,  
`Assets/_Project/Scripts/Core/Localization/LocalizationManager.cs`

---

## 6. Mission Recap: allineamento header

### Problema

- Il testo `MISSIONI` e il count erano visivamente più alti/bassi rispetto ai bottoni filtro `ATTIVE` / `COMPLETATE`.

### Soluzione

- `active-missions-title` e `active-missions-count` hanno ora altezza `20px`, coerente coi filtri, e `-unity-text-align: middle-left`.

**File interessati:**  
`Assets/_Project/Resources/UI/UIToolkit/ActiveMissions/ActiveMissions.uss`

---

## 7. DomeStatusHUD: partenza compatta e auto-open su piantagione

### Problema

- All'ingresso in Play il DomeStatusHUD appariva già aperto, anche con tutti i POT vuoti.
- Un override di scena manteneva `_startHudExpanded: 1`.
- Cliccando POT/CRYO da collassato si rischiava di mostrare sezioni senza passare dalla linguetta.

### Soluzione

- Default `_startHudExpanded = false` nel controller e override scena portato a `0`.
- In `SetupUI`, in Play, `_startHudExpanded` viene forzato a `false` per evitare regressioni da Inspector/scene.
- Aggiunto tracking `_potHadPlantLastRefresh`: su `PotEvents.OnPotStateChanged`, se un POT passa da vuoto a occupato, il body si apre sulla tab POT.
- `SwitchTab` rispetta `_hudBodyExpanded`: cambia tab selezionato, ma non mostra sezioni se il body è collassato.

**File interessati:**  
`Assets/_Project/Scripts/UI/UIToolkit/DomeStatusHUD/DomeStatusHUDController.cs`,  
`Assets/_Project/Scenes/SCN_VaultMap.unity`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `.cursor/plans/add_plant_waves_task_01f0b67e.plan.md` | Piano rollout progressivo specie in wave |
| `.cursor/plans/demo_alpha_1_0_gap_map.plan.md` | Piano demo Alpha riscritto post Beat 3 in modalità choice-driven |
| `.cursor/plans/elevator_2.0_13afcdbe.plan.md` | Piano incrementale Elevator 2.0 |
| `.cursor/plans/roadmap_dome_lab_100_069d5bdb.plan.md` | Roadmap Dome/Lab verso implementazione completa |
| `Assets/Resources/Demo/DemoAlphaNarrativeConfig.asset` | Aggiornamenti configurazione narrativa demo |
| `Assets/_Project/Animations/Player_new/test_2.png` (+ meta) | Asset player staged |
| `Assets/_Project/Docs/VISITOR_DESK_SPEC.md` | Specifica Visitor Desk / Mercante Ombra demo |
| `Assets/_Project/Resources/UI/UIToolkit/VoOverlay/VoOverlay.uss` | Fondo VO semi-opaco con bordi arrotondati |
| `Assets/_Project/Resources/UI/UIToolkit/ActiveMissions/ActiveMissions.uss` | Background missioni attive/completate e allineamento header |
| `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/FoundationNotificationsPanelController.cs` | Tooltip toast: lifecycle e posizionamento |
| `Assets/_Project/UI/UIToolkit/NotificationsFoundation/NotificationsPanel.uss` | Stile tooltip notifiche |
| `Assets/_Project/UI/UIToolkit/NotificationsFoundation/NotificationsPanel.uxml` | Struttura/placeholder tooltip notifiche |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs` | Blink azioni, riga breakdown fame, layout righe tooltip |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/Components/SegmentedBarUI.cs` | Accesso controllato ai segmenti della barra |
| `Assets/_Project/UI/UIToolkit/HUD/TopBar.uss` | Classe stato segmento azioni instabile |
| `Assets/_Project/Scripts/UI/UIToolkit/PlayerInventory/PlayerInventoryPanelController.cs` | Conferma dedicata acqua sporca |
| `Assets/_Project/Scripts/Core/Localization/LocalizationManager.cs` | Nuove stringhe tooltip azioni e acqua sporca |
| `Assets/_Project/Scripts/UI/UIToolkit/DomeStatusHUD/DomeStatusHUDController.cs` | Stato iniziale compatto e auto-open su piantagione |
| `Assets/_Project/Scenes/SCN_VaultMap.unity` | Override `_startHudExpanded: 0` per DomeStatusHUD |

---

## Regole / vincoli rispettati

- **UI Toolkit Builder parity:** stili visuali inseriti in USS dove possibile; nessun albero UI parallelo introdotto.
- **Logica gameplay invariata:** la regola fame/azioni esistente non è stata modificata; il blink legge solo stato già presente.
- **Demo/full parity:** gli interventi HUD e inventario restano validi sia in full che in demo.
- **Dirty worktree:** non sono stati revertiti file estranei o modifiche preesistenti.
- **DEV REPORT:** sezione `Statistiche e progresso` presente con metriche reali o stato di validazione dichiarato.

---

## Note operative (Unity)

- Validare in Play Mode:
  - nuova partita demo e full: DomeStatusHUD deve partire compatto;
  - click linguetta DomeStatusHUD: apertura/chiusura manuale;
  - piantagione in POT: auto-open sulla tab POT;
  - topbar azioni in stato `ConsecutiveDaysWithoutMeal >= 2` e `AteMealSincePreviousDawn == false`: ultimo segmento lampeggia e tooltip mostra riga `-1`;
  - consumo `Items.Water`: conferma con warning contaminazione;
  - toast con tooltip: dopo timeout toast il tooltip deve sparire.
- `SCN_VaultMap.unity` contiene cambi staged serializzati di scena; per questo report la verifica funzionale prioritaria resta l'avvio compatto del DomeStatusHUD.

---

*Fine DEV REPORT 0106.*
