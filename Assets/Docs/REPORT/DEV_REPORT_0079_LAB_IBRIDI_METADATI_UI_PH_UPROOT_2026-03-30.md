# DEV REPORT 0079 — Task 6 (core): handoff Lab → Dome, ibridi runtime, metadati end-to-end

**Data:** 2026-03-30  
**Sprint / contesto:** Dome Lab — **Task 6 — Handoff Lab → Dome e ibridi runtime** (`roadmap_dome_lab_100_069d5bdb.plan.md` § Task 6); chiusura Gate “ibrido reale” con effetti gameplay e UI coerenti su tutta la catena seme → vaso → sistemi giornalieri.  
**Riferimento piano:** `.cursor/plans/roadmap_dome_lab_100_069d5bdb.plan.md` — **Task 6 (completato)**; aggiornamento testuale Task 6/7: `roadmap_task_6-7_update_18c6024c.plan.md` (home Cursor).  
**Report precedente:** `DEV_REPORT_0078_DOMEHUD_PARITA_UI_BUILDER_RUNTIME_CARD0_REGOLE_2026-03-29.md`

---

## Focus — Perché questo report è il Task 6

Il **Task 6** è il nucleo del lavoro descritto qui: non è un refinish cosmetico del Lab né solo una passata UI, ma la **canonizzazione del passaggio PreSeed → Seed → pianta runtime** con **profilo parametrico/ibrido** (`LabHybridGameplayModifiers`, `LabCareProfileMetadata`, tag, scaling) e **metadata su `Item` / `PotStateModel`** così che:

- la **specie effettiva** (`ResolvedPlantCodeMetadata`, inferenza da tratti/poteri dove serve) **non dipenda** dal solo `TypeId` `seed-00x`;
- **piantagione, giorno, pH, cure, automation e save** consumino la **stessa** verità;
- **Terminal, DomeStatusHUD, TopBar, inventario, Cryo/botanico** mostrino **nome custom, poteri e drift** dell’ibrido, non fallback sulla specie base.

Tutto il resto del sommario (HUD statistiche, colori pH, UPROOT → WholePlant, Arctic su label, ecc.) sono **collaterali necessari al completamento del Task 6** (Gate “due output Lab distinti → comportamento diverso in Dome” + leggibilità player).

---

## Sommario interventi

1. **Lab Incubator (Reagent X)** — snapshot delle scelte UI all’avvio incubazione (`PendingReagentXSnapshot`); nessun reset intermediario che corrompe nome custom / famiglia / tratti; dropdown **genoma dominante** per nome libero; `BuildSeedOutputFromIncubation` allineato allo snapshot e alla risoluzione `refPlant` (inferenza da poteri in modalità AUTO dove applicabile).
2. **Architettura item / PlantCode** — campo **`ResolvedPlantCodeMetadata`** su `Item` (gameplay canonico vs `TypeId` legacy `seed-00x`); serializzazione in **SaveManager** con fallback da `PlantDatabase` su save vecchi; **`ItemFabric`**: popolamento campo, `TryResolveReferencePlantCodeFromPowerChoices`, creazione seed coerente.
3. **Piantagione e UPROOT** — **`PotActions.DoPlant`** prioritizza metadati seme per risolvere `PlantCode`; **`DoUproot`** snapshot stato prima del reset, aggiunge **pianta intera** arricchita in inventario e notifica Collection (**FoundationNotificationService** / payload da item).
4. **UI nome e ibride** — **Terminal** (`PlantCardV3TerminalController`): etichette inventario e nome vaso da `GetItemDisplayName` / `GetPotDisplayName`; blocco note ricerca ibrido da metadati; **DomeStatusHUD** e **TopBar** tooltip pH: priorità `CustomPlantName`; **BotanicalPowerFacade**: etichette poteri da metadati vaso/Cryo ove presenti.
5. **pH e Arctic su ibridi** — **`DayCycleController`**: rilevamento **Arctic Purification** anche via `ActivePowerLabel` (non solo specie pura); allineamento drift/scaling con **`LabHybridGameplayModifiers`**; **DomeStatusHUD**: drift pH mostrato coerente con calcolo reale (inclusi bonus e scala ibrida).
6. **Inventario** — tooltip senza riga `Tipo: seed-00x` per semi registrati; **WholePlant** con nome custom mostrato come pianta intera; riga deterioramento per item organici.
7. **Automazione** — **`PotAutomationRunner`**: diary/metadata che rispettano `ResolvedPlantCodeMetadata` e nome custom dal payload azione.
8. **Strumentazione debug** — aggiunta e poi **rimossa** (`DebugNdjsonSessionLog` eliminato); log NDJSON solo durante la fase di verifica.
9. **Affinamento DomeStatusHUD (post-0078)** — riga **pH** espansa: colore da **`PhGradientDisplayColors`** (stessa logica gradiente TopBar); font **11px** su `.dome-pot-ph-line`; tooltip **STATO ATTUALE**: **Light stress** % con colorazione range **20–80%**; fertilizzante con testo **necessario / non necessario** per stadio; cella fert **NON NECESSARIO** neutra in **Seme/Germoglio**; requisiti cure da **`ResolvePlantDataForCareRequirements`** come in **`GrowthPointsCalculator`**.
10. **Refactor colore pH** — estratta classe **`PhGradientDisplayColors`**; **TopBarController** delega al fine di evitare duplicazione della mappa colori.

---

## 1. Lab Incubator — stato UI e output seme

### Problema
- Dopo `TrySpendAction` e refresh schermo, `RefreshReagentXSelectors` poteva azzerare lo stato (nome custom, modo nome, ecc.) mentre l’incubazione era attiva → seme con specie/name errati (es. Arctic Hask forzato).
- Nome “preset/mix” non doveva forzare `refPlantCode` in modo da sovrascrivere famiglia + tratti scelti.

### Soluzione
- **`PendingReagentXSnapshot`** catturato in **`OnAvviaClicked`** dopo validazione e prima del consumo risorse; `RefreshReagentXSelectors` non resetta i selettori se giorno incubazione attivo e snapshot presente senza pre-seed in inventario.
- **`BuildSeedOutputFromIncubation`**: uso consistente dello snapshot per famiglia, tratti, profilo cure, nome, genoma dominante; `refPlantOverride` da dominante esplicito o inferenza poteri (AUTO); niente override implicito da stringa nome per mix non custom.

**File:** `Assets/_Project/Scripts/UI/UIToolkit/Lab/LabIncubatorPanelController.cs` (e UXML associato ove modificato in sprint)

---

## 2. Item e salvataggio — `ResolvedPlantCodeMetadata`

### Problema
- Il gameplay e la UI inferivano la specie dal **`TypeId`** seme (`seed-001`…), disallineato dal concetto di “un solo tipo seme con metadati ereditati”.
- Caricamento save senza campo nuovo → rischio perdita risoluzione specie.

### Soluzione
- **`Item.ResolvedPlantCodeMetadata`**: PlantCode finale usato per lookup e sistemi.
- **`ItemFabric`**: valorizzazione in creazione seed / debug; **`TryResolveReferencePlantCodeFromPowerChoices`** per disambiguare genitore da righe potere; helper prima riga descrizione potere.
- **`SaveManager`**: lettura/scrittura `resolvedPlantCodeMetadata`; deserialize con fallback **`PlantDatabase.GetPlantDataBySeedTypeId`**.

**File:** `Item.cs`, `ItemFabric.cs`, `SaveManager.cs`

---

## 3. Pot — `DoPlant` e `DoUproot`

### Problema
- Piantagione risolveva ancora principalmente via `GetPlantDataBySeedTypeId` ignorando il PlantCode reale del seme ibrido.
- UPROOT aggiungeva item generico / resettava stato prima di copiare metadati → inventario senza ibrido; assenza Collection box coerente.

### Soluzione
- **`DoPlant`**: priorità a **`consumedSeedItem.ResolvedPlantCodeMetadata`** per `plantCode` / `PlantData`.
- **`DoUproot`**: lettura `PotStateModel` (nome custom, poteri, cure, ecc.) prima di **`ResetToEmpty`**; **`ItemFabric.CreateItemWithMetadata(Items.WholePlant, …)`** con metadati; notifica raccolta come per altri item.

**File:** `PotActions.cs`

---

## 4. Terminal, HUD, TopBar, poteri botanici — etichette giocatore

### Problema
- Dopo il fix Lab, il vaso mostrava ancora nomi/specie “base” (es. Glasscap) in Terminal (STATUS, code path, tooltip), tooltip pot DomeStatusHUD, tooltip modificatori pH TopBar, blocchi effetti globali.

### Soluzione
- **Terminal**: `PlayerInventoryPanelController.GetItemDisplayName` sulle liste; **`GetPotDisplayName`**; **`BuildResearchedNoteLines`** per ibride vs `PlantData.ResearchNotes`.
- **DomeStatusHUD**: `GetPotPlantDisplayName`; calcolo drift pH mostrato allineato a orchestrazione giornaliera + modificatori ibridi.
- **TopBar**: etichetta modificatore pH con nome custom da **`DomePotRegistry`** se presente.
- **BotanicalPowerFacade**: uso `ActivePowerLabel` / `PassivePowerLabel` dai modelli payload ove valorizzati.

**File:** `PlantCardV3TerminalController.cs`, `DomeStatusHUDController.cs`, `TopBarController.cs`, `BotanicalPowerFacade.cs`

---

## 5. pH drift — Arctic su ibridi e coerenza HUD

### Problema
- Bonus **Arctic Purification** (+5) applicato solo se specie pura Arctic Hask, non se l’ibrido espone lo stesso potere via metadato.
- Card Dome mostrava drift “raw” da `PlantData` senza stesso pipeline di **`DayCycleController`**.

### Soluzione
- **`HasArcticPurificationActive(pot, plantData)`** in DayCycle (e stessa logica lato HUD ove duplicata) che considera **`ActivePowerLabel`**.
- **`ComputeShownDailyPhDrift`** in DomeStatusHUD allineato al calcolo effettivo (Arctic + **`LabHybridGameplayModifiers.ScaleDailyPhDrift`**).

**File:** `SPOR-BLK-01-03A-DayCycleController.cs`, `DomeStatusHUDController.cs`, `LabHybridGameplayModifiers.cs` (contesto tag/scaling)

---

## 6. Inventario — tooltip e WholePlant

### Problema
- Tooltip mostrava `Tipo: seed-00x` ridondante; WholePlant con nome custom appariva come “seme”; assenza avviso deterioramento organici.

### Soluzione
- Tipologia nascosta per `TypeId` seme registrato in DB.
- Regola display **WholePlant + CustomPlantName** → etichetta pianta intera.
- Riga **“SI DETERIORA IN: … giorni”** per tipi organici con logica deterioramento esistente.

**File:** `PlayerInventoryPanelController.cs`

---

## 7. Automazione vasi

### Problema
- Diary/record azioni non rifletteva PlantCode risolto da metadati item in coda.

### Soluzione
- Priorità **`ResolvedPlantCodeMetadata`** e nome custom dal payload azione.

**File:** `PotAutomationRunner.cs`

---

## 8. Strumentazione temporanea e pulizia

### Problema
- Verifica runtime richiesta log NDJSON mirati; a fine iterazione va rimossa.

### Soluzione
- Rimossi `#region agent log` e **`DebugNdjsonSessionLog`** (file cancellato); logica di prodotto invariata.

**File:** vari script UI/Dome/Core (vedi tabella); eliminati `DebugNdjsonSessionLog.cs` e `.meta`.

---

## 9. DomeStatusHUD — pH colorato, stress luce, fertilizzante per stadio

### Problema
- Riga `dome-pot-stat-ph-*`: colore statico, font poco leggibile.
- Tooltip **STATO ATTUALE**: mancavano **Light stress** % e chiarezza fertilizzante **necessario vs opzionale**.
- Cella fertilizzante: **0%** in Seme/Germoglio appariva come errore (rosso) pur essendo **non richiesto** in quello stadio.

### Soluzione
- **`PhGradientDisplayColors`**: `GetColorFromDrift` / `GetColorFromScale` condivisi con TopBar; applicati a `_potStatPh[i].style.color` dal drift giornaliero **mostrato**.
- **USS** `.dome-pot-ph-line` → **11px**, bold.
- Tooltip: riga **`Light stress  : n% (20–80% ideale)`** con **`RangeColor`** su 20–80; fertilizzante con testo esplicito; requisiti con nota opzionale Seme/Germoglio.
- Celle: **`NON NECESSARIO`** + **`TipMuted`** per Seme/Sprout; altrimenti comportamento precedente con range.
- **`LabHybridGameplayModifiers.ResolvePlantDataForCareRequirements`** per `req` in tooltip e stats acqua/fert.

**File:** `PhGradientDisplayColors.cs`, `TopBarController.cs`, `DomeStatusHUDController.cs`, `DomeStatusHUD.uss`, `DomeStatusHUD.uxml`

---

## File modificati

| File | Tipo modifica |
|---|---|
| `Assets/_Project/Scripts/UI/UIToolkit/Lab/LabIncubatorPanelController.cs` | Snapshot Reagent X, build seed, genoma dominante, log rimossi |
| `Assets/_Project/Scripts/Core/ItemsSystem/Item.cs` | `ResolvedPlantCodeMetadata` |
| `Assets/_Project/Scripts/Core/ItemsSystem/ItemFabric.cs` | Seed metadata, inferenza ref plant, rimozione log debug |
| `Assets/_Project/Scripts/Core/SaveManager.cs` | Serialize/deserialize resolved plant code |
| `Assets/_Project/Scripts/Dome/PotActions.cs` | DoPlant resolution, DoUproot whole plant + notifica |
| `Assets/_Project/Scripts/Dome/PotAutomation/PotAutomationRunner.cs` | Diary metadata-aware |
| `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs` | Arctic ibrido, log debug rimossi |
| `Assets/_Project/Scripts/Dome/PotSystem/Growth/LabHybridGameplayModifiers.cs` | Contesto scaling/tag; log rimossi |
| `Assets/_Project/Scripts/Dome/PotSystem/Growth/GrowthPointsCalculator.cs` | Solo rimozione strumentazione agent |
| `Assets/_Project/Scripts/Dome/PotSystem/Botanical/BotanicalPowerFacade.cs` | Label poteri da metadati |
| `Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs` | Display name, note ricerca, log rimossi |
| `Assets/_Project/Scripts/UI/UIToolkit/DomeStatusHUD/DomeStatusHUDController.cs` | Nome pot, drift pH, tooltip, fert/stress, care PlantData, colore riga pH |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs` | Label modificatori pH; uso `PhGradientDisplayColors` |
| `Assets/_Project/Scripts/UI/UIToolkit/PlayerInventory/PlayerInventoryPanelController.cs` | Tooltip seme, whole plant, deterioramento |
| `Assets/_Project/Scripts/UI/UIToolkit/PhGradientDisplayColors.cs` | **Nuovo** — gradiente pH condiviso |
| `Assets/_Project/UI/UIToolkit/DomeStatusHUD/DomeStatusHUD.uxml` | Placeholder tooltip allineati |
| `Assets/_Project/UI/UIToolkit/DomeStatusHUD/DomeStatusHUD.uss` | `.dome-pot-ph-line` 11px bold |
| `Assets/_Project/Scripts/DevTools/Logging/DebugNdjsonSessionLog.cs` | **Rimosso** |
| `Assets/_Project/Scripts/DevTools/Logging/DebugNdjsonSessionLog.cs.meta` | **Rimosso** |

*Nota:* altri file di scena/asset (`SCN_VaultMap.unity`, ecc.) possono risultare dirty in branch se toccati in sessioni locali; l’elenco sopra focalizza il codice prodotto dall’arco funzionale descritto.

---

## Regole / vincoli rispettati

- **ServiceContainer / registry**: nessun nuovo `FindObjectOfType` introdotto per gameplay; risoluzione servizi coerente con `architecture-runtime-services.mdc`.
- **Parità UI Builder**: modifiche strutturali tooltip in UXML; stili “di marca” su USS (`.dome-pot-ph-line`); colori funzionali pH drift da codice dove richiesto (dato-dipendente).
- **Facade**: `PotActions` e `DayCycleController` restano punti di orchestrazione noti; interventi incrementali.

---

## Note operative (Unity)

- Verificare in Play: flusso Lab → seme in inventario → Terminal (nome e seed unico logico) → piantagione → DomeStatusHUD / tooltip / TopBar coerenti su nome e drift.
- **UPROOT**: in inventario deve comparire **WholePlant** con metadati; Collection box atteso insieme al toast se configurato così in Foundation.
- **Save**: dopo aggiornamento, vecchi save senza `resolvedPlantCodeMetadata` ripopolano da DB dove possibile.
- Aprire **`DomeStatusHUD.uxml`** in UI Builder per authoring su classi USS (non affidarsi a `style=` sulle card runtime se si vuole propagazione multi-card).

---

*Fine DEV REPORT 0079.*
