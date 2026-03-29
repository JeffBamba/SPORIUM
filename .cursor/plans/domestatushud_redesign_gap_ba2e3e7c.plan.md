---
name: DomeStatusHUD redesign gap
overview: Redesign solo estetica, layout e interazioni UI del DomeStatusHUD (Unity UI Toolkit). Nessuna modifica a dati di gioco, regole, soglie o al contenuto informativo prodotto oggi (stessi testi/calcoli di RefreshPots e BuildPotTooltipLines). Card, expand/collassa, delay/posizione tooltip, rimozione toggle globale, tema CRT; Foundation notifiche fuori scope.
todos:
  - id: clarify-wild-toggle-notify
    content: "Risolto: WILD=#E6C96F; toggle globale rimosso; notifiche fuori scope (Foundation)"
    status: pending
  - id: uxml-card-layout
    content: "Ridisegnare UXML: card POT con badge, chevron, area espansa, footer tip; empty state IT; nessun blocco notifications"
    status: completed
  - id: controller-expand-tooltip
    content: Stato espansione per pot, click header, tooltip delay/posizione; tooltip invariato (stesse righe da BuildPotTooltipLines, stessi colori)
    status: completed
  - id: uss-crt-theme
    content: "Riscrivere USS: palette, bordi 0px, stati hover, scrollbar, overlay scanline dove supportato"
    status: completed
  - id: family-status-copy
    content: "Solo resa visiva famiglia/condizione: stessi criteri e dati di oggi (es. logica allineata a PlantCardV3 senza nuove regole); WILD=#E6C96F solo se già derivabile dai dati esistenti"
    status: completed
isProject: false
---

# DomeStatusHUD: gap tra repo main e nuovo design

## Decisioni confermate (sessione 2026-03-29)


| Tema                                   | Scelta                                                                                                                                   |
| -------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| Colore **WILD**                        | Giallo/oro **#E6C96F** (allineato ai mock Allegati).                                                                                     |
| Toggle **« / »** (collasso intero HUD) | **Rimuovere**: body sempre visibile; eliminare anche auto-expand/collapse legato alla presenza piante se incoerente con “sempre aperto”. |
| **Notifiche** in cima (Allegato 3)     | **Fuori scope**: appartengono alla **Foundation**, non si integrano nel redesign DomeStatusHUD.                                          |
| Etichette **condizione**               | **Mantenere logica e testi attuali** (Rigogliosa / Sana / Appassita / Critica); solo miglioramento visivo (LED, badge, colori).          |
| **POT vuoto**                          | Testi in **italiano** (es. «VASO VUOTO» / «Pronto per la piantagione» o equivalente coerente col resto UI Dome).                         |


### Vincolo (confermato)

**Dati e logica di gioco restano identici a oggi.** Si cambiano solo: grafica (USS), struttura markup (UXML), interazioni UI (es. espansione card, hover tooltip con delay, assenza toggle collasso globale), e **copy statico** puramente decorativo/istruttivo (es. footer «usa il terminale…») dove non altera regole o calcoli.

- **Non** modificare: `PotStateModel`, processor, soglie condizione, `ConditionLabel` / `ConditionColor`, contenuto e regole di `BuildPotTooltipLines` e `BuildCryoTooltipLines`, logica cryo/botanica nel tooltip.
- **Sì** modificare: classi USS, gerarchia elementi, visibilità sezioni, binding agli **stessi** valori già letti in `RefreshPots` / tooltip (se l’area espansa mostra numeri, usare le **stesse** formule già usate nel controller per riga e tooltip, idealmente centralizzate per evitare drift).
- Il tooltip continua a essere popolato come oggi; eventuali miglioramenti = solo delay, posizione, larghezza, scanline **sul container**, non nuove righe o nuovi significati.

### Parità contenuti (obbligatoria)

**Sì:** dopo il rework devono restare accessibili **tutti** i contenuti informativi che l’HUD mostra oggi, più eventuali **aggiunte** che sono solo altre viste degli **stessi** dati (es. percentuali idratazione/fertilizzante/LED già usate nel tooltip, mostrate anche in card espansa).


| Oggi (POT, per slot)                            | Obbligo rework                                                                                                  |
| ----------------------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| Anteprima sprite / vuoto                        | Stesso dato (`pot.Sprite` / vuoto).                                                                             |
| Nome pianta o stato vuoto                       | Stesso testo logico (vuoto: copy IT a parità di significato «slot vuoto» + id POT ancora identificabile).       |
| Sottotitolo `Lvl                                | stadio                                                                                                          |
| Condizione + `ConditionScore%`                  | Invariati (`ConditionLabel` / `ConditionColor`).                                                                |
| Indicatore irrigazione `WateringSystemOn` (●/○) | Stesso significato; se si sposta in area espansa, resta visibile in collasso **oppure** aperta senza ambiguità. |
| Indicatore `LedSystemState` (●/○ colori)        | Stesso significato; stessa regola di visibilità.                                                                |
| Tab testo `POTS [n/4]` / `CRYO [n/3]`           | **Mantenuti** (stessi conteggi); possono essere resi più piccoli o accanto alle icone, non rimossi.             |
| Tooltip POT / CRYO (righe colorate)             | Invariato come output di `BuildPotTooltipLines` / `BuildCryoTooltipLines`.                                      |



| Oggi (CRYO, per slot)                                                                        | Obbligo rework                           |
| -------------------------------------------------------------------------------------------- | ---------------------------------------- |
| `SlotId`, nome pianta o `—`, riga dettaglio (pH drift/g, passive power) o «slot disponibile» | Stessi campi e stessa logica di stringa. |


**Aggiunte ammesse dal piano** (solo dati già esistenti nel modello / già calcolati nel controller per il tooltip): es. griglia H2O % / FERT % / LED in card espansa usando le **stesse** formule del tooltip; badge `POT-xxx` esplicito; colori famiglia da dati già noti; footer statico IT; chevron expand; delay/posizione tooltip.

---

## Come è fatto oggi (evidenza repo)

- **Stack:** [DomeStatusHUD.uxml](d:/Sporae_Build_Beta/Assets/_Project/UI/UIToolkit/DomeStatusHUD/DomeStatusHUD.uxml), [DomeStatusHUD.uss](d:/Sporae_Build_Beta/Assets/_Project/UI/UIToolkit/DomeStatusHUD/DomeStatusHUD.uss), [DomeStatusHUDController.cs](d:/Sporae_Build_Beta/Assets/_Project/Scripts/UI/UIToolkit/DomeStatusHUD/DomeStatusHUDController.cs).
- **Dati:** `DomePotRegistry`, `CryoMachineController`, `PhSystem`, `PotStateModel` (idratazione % da `Hydration * 10`, `FertilizerLevel`, `LedSystemState`, `ConditionScore`, `DaysConsecutiveOptimal`, `DaysSincePlant`, `ForecastDirection`, `PlantFamilyMetadata`, ecc.).
- **DEV REPORT:** [DEV_REPORT_0073](d:/Sporae_Build_Beta/Assets/Docs/REPORT/DEV_REPORT_0073_DOMEHUD_FONT_TOOLTIP_COLORI_DEBUG_SEED_2026-03-19.md); [DEV_REPORT_0074](d:/Sporae_Build_Beta/Assets/Docs/REPORT/DEV_REPORT_0074_TASK4_BOTANICAL_POWERS_E_BUGFIX_EOD_2026-03-25.md).

## Cosa coincide già (poca o nessuna logica nuova)


| Area                             | Stato                                                                  |
| -------------------------------- | ---------------------------------------------------------------------- |
| Tab POT / CRYO                   | Presente (`SwitchTab`, sezioni separate).                              |
| Lista pot (4) e cryo (3)         | Fissa in UXML.                                                         |
| Tooltip su hover (solo occupato) | `BuildPotTooltipLines` con REQUISITI / STATO ATTUALE, muffa, botanica. |
| Dati quick stats H2O/FERT/LED    | `PotStateModel` + `StageRequirements`.                                 |
| Servizi                          | `ServiceContainer`, niente `FindObjectOfType` nel controller.          |


## Differenze strutturali (dove si lavora di più)

1. **Collapse globale** — Oggi toggle + `dome-hud-collapsed` + auto open/close. **Da rimuovere** per allineamento a sidebar sempre visibile.
2. **Espansione per card** — Click sull’header POT occupato: mostrare in layout a griglia i **medesimi indicatori/numeri già derivati oggi** in `RefreshPots` (e, per percentuali allineate al tooltip, le stesse formule del tooltip — senza introdurre nuovi campi o soglie). Footer istruttivo statico IT ammesso. **Niente** aggiunta di trend o «giorni vita» se non sono già presenti nel testo/tooltip attuale.
3. **Layout card** — Chevron, badge `POT-xxx`, box icona, metadati; colori famiglia come **resa visiva** coerente con i dati già mostrati altrove (es. stessi `PlantFamily` / metadata di oggi). **WILD = #E6C96F** solo se il dato runtime già distingue WILD — altrimenti non inventare categorie nuove.
4. **Tab** — Stile mock (icone, bordi); contatori `[x/4]` e `[x/3]` **sempre presenti** come oggi (stessi numeri), eventualmente tipografia ridotta.
5. **POT vuoto** — Copy IT, tema info/blu, no expand, no tooltip.
6. **Condizione in lista** — Stessi quattro stati di `ConditionLabel`; presentazione tipo LED + colore come da mock, senza rinominare in PROSPERA/SANA mock.

## Fuori scope esplicito

- **Pannello / lista NOTIFICATIONS** (screenshot Allegato 3): non è parte del DomeStatusHUD; resta competenza **Foundation** / altri UI. Nessun nuovo blocco notifiche in questo redesign.

## Gap estetica / tech (Unity ≠ React)

- Animazioni: USS `transition` o schedule, non Framer Motion.
- Glow/scanline/radius 0: vincoli UI Toolkit; simulare dove necessario.
- Tooltip: delay ~200px, larghezza ~380px, posizionamento a sinistra della card se fattibile.
- Scroll: ok con 4 righe; `ScrollView` se il numero slot cresce.

## Tooltip

Nessun arricchimento semantico: stesso output di `BuildPotTooltipLines` / `BuildCryoTooltipLines`. Solo presentazione (delay, posizione, dimensioni, stili sul pannello).

## Riepilogo quantitativo

- **Controller:** stato espansione, click, tooltip delay/posizione, rimozione toggle/auto-expand globale; **nessuna** modifica ai metodi che costruiscono il significato delle righe tooltip o delle condizioni.
- **UXML/USS:** rifacimento layout/tema righe POT (e cryo se serve solo look).
- **Dati / logica:** invariati; colori WILD **#E6C96F** solo dove supportato dai dati esistenti senza nuove regole di classificazione.

