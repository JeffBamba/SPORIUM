# DEV REPORT 0070 — Task 1 Roadmap Dome+Lab (contratti dati, frutti specifici, save/load, metadata end-to-end) + Rifiniture Terminal Pot (HUD, comandi in avvio, scrollbar)

**Data:** 2026-03-18  
**Oggetto:** Implementazione completa del **Task 1** del piano `roadmap_dome_lab_100_069d5bdb.plan.md` (contratti dati, save/load, unlock meta, frutti specifici per specie, propagazione metadata da Lab a Dome a harvest e discovery) e, nella stessa sessione, rifiniture UX al **Terminal Pot** (HUD riga codice/livello/famiglia, lista comandi in avvio con fix flush, allineamento scrollbar).  
**Riferimenti:** Roadmap `.cursor/plans/roadmap_dome_lab_100_069d5bdb.plan.md` — Task 1; `PotStateModel`, `SaveManager`, `Item`/`ItemFabric`, `PotActions`, `PlantDatabase`, `WikiUnlockService`; `PlantCardV3TerminalController`, UXML/USS Terminal.  
**Report precedente:** `Assets/Docs/REPORT/DEV_REPORT_0069_STATO_ATTUALE_SISTEMI_PIANTE_VS_GDD40_2026-03-18.md`

---

## 1. Contesto

- **Task 1 del piano Dome+Lab:** rendere `PotStateModel`, save/load e item Lab solidi da sostenere il resto della roadmap: payload canonico del seme (famiglia, genetica, tratti, reagente, provenienza Lab, ibrido/mutato), serializzazione stato completo, passaggio item→pianta senza perdita metadata, unlock/discovery nel save di partita.
- **Estensione effettiva (sessione):** sostituzione frutti generici con **frutti specifici per specie** (Arctic Pod, Ferric Pod, Glass Pod) con metadata espliciti (ActivePowerLabel, PassivePowerLabel, provenienza); inventario iniziale pulito senza semi/spore placeholder; loop completo **frutto → Extractor → spora → Catalizzatore → spora maturata → Fusion → pre-seed → Incubator → seed → piantumazione in Dome → harvest → frutto** con persistenza metadata e save/load; discovery e wiki serializzati nel save; STATUS con barra frutti maturi; fix End of Day (bottone YES cliccabile); rifiniture Terminal Pot (HUD famiglia, lista comandi in avvio, scrollbar).

---

## 2. PARTE A — Task 1 Roadmap Dome+Lab

### 2.1 Contratti dati: Item e PotStateModel

- **Item** (`Item.cs`): estesi i campi per identità e poteri propagati in tutta la pipeline Lab/Dome: `SourcePlantDisplayName`, `ActivePowerLabel`, `PassivePowerLabel` (oltre a campi già presenti per famiglia, codice sorgente, parenti, tratti, reagente, nome custom).
- **PotStateModel** (`PotStateModel.cs`): allineato al payload canonico del seme con campi per famiglia, codici sorgente, parenti, tratti candidati/selezionati, percentuale potenza tratti, reagente usato, nome custom, display name sorgente, etichette poteri attivo/passivo, flag `IsHybrid`, `IsMutated`, `IsInPassiveSlot`. Aggiunto **`ApplySeedMetadata(Item seedItem, PlantData plantData)`** per popolare lo stato dalla pianta al momento della piantumazione; **`ClearSeedRuntimePayload()`** per reset.

### 2.2 Frutti specifici per specie (sostituzione frutti generici)

- **Items.cs:** introdotti `FruitFerricPod`, `FruitArcticPod`, `FruitGlassPod`; array `SpecificFruitTypeIds`, `LegacyFruitTypeIds`, `AllFruitTypeIds`; helper `IsSpecificFruitType()`, `IsLegacyFruitType()`, `IsFruitType()`. **`StarterInventoryTypeIds`**: solo frutti specifici e materiali base, **esclusi** semi/spore/pre-seed placeholder.
- **ItemFabric:** struttura **FruitDefinition** (TypeId, PlantCode, DisplayName, PassivePowerLabel) per i tre frutti; dizionario `_fruitDefinitionsByTypeId`. **`CreateItemWithMetadata`** esteso con `sourcePlantDisplayName`, `activePowerLabel`, `passivePowerLabel`. Propagazione metadata in `CreateSporeRawFromFruit`, `CreateSporeMaturedFromRaw`, `CreatePreSeedFromSpores`, `CreateSeedFromPreSeed`. Helper: `GetFruitDisplayNameByTypeId`, **`ResolveFruitTypeIdForPlant`** (da PlantCode/famiglia), `CopyPlantPowerMetadata`, `ApplyBaseFruitMetadata`, `ApplyPlantMetadataFromCode`, `ResolvePassivePowerLabel`, `CombineDistinctValues`. **`CloneSpore`** per minigame microscopio (copia metadata spora). Asset `ItemConfig` creati: `fruit-ferric-pod`, `fruit-arctic-pod`, `fruit-glass-pod`.
- **GameManager:** inventario iniziale popolato da **`Items.StarterInventoryTypeIds`** (non più `AllTypeIds`), quantità starter per frutti specifici e per gli altri item.

### 2.3 Passaggio item → pianta e harvest → frutto

- **PotActions:** `DoPlant(string seedTypeId, bool irrigate, Item seedItem)` — consuma da inventory con `TryRemoveFirst`, applica **`ApplySeedMetadata(consumedSeedItem, plantData)`** sul `PotStateModel`. `DoHarvest`: risoluzione `fruitTypeId` con `ItemFabric.ResolveFruitTypeIdForPlant`, creazione frutti con **`ItemFabric.CreateItemWithMetadata`** (sourcePlantDisplayName, activePowerLabel, passivePowerLabel da stato vaso).
- **PotSlot.CollectFruits:** crea item frutto specifici con `ItemFabric.CreateItemWithMetadata` e metadata da `PotStateModel` (non più `Add(Items.Fruits, amount)` generico).
- **PlantCardV3TerminalController — coda azioni:** `ExecuteQueuedActions` per azione Plant consuma **Item reale** da inventory (`TryRemoveFirst(..., out removedItem)`) e assegna `ItemPayload` a `AutomationAction`; rollback in caso di fallimento batch. **PotAutomationRunner:** `AutomationAction` con campo `Item ItemPayload`; in `RunAction` per Plant viene chiamato `DoPlant(..., action.ItemPayload)`.

### 2.4 Save/Load e migrazione legacy

- **SaveManager:** `InventoryItemData` con campi `sourcePlantDisplayName`, `activePowerLabel`, `passivePowerLabel`; serializzazione/deserializzazione inventory aggiornata; **versione inventory** bump per metadata. In **DeserializeInventory**: migrazione da `fruits-001` / `fruits-known-001` a frutti specifici (tramite `ItemFabric.CreateItemWithMetadata`) quando presenti metadata sufficienti. Serializzazione stato vaso tramite `JsonUtility.ToJson(potState)` (payload completo); deserializzazione con versioning e fallback per save vecchi.
- **SaveManager — Discovery e Wiki:** salvataggio e caricamento di **`discoveredPlantCodes`** (da `PlantDatabase.ExportDiscoveredPlantCodes`) e **`wikiUnlockedIds`** (da `WikiUnlockService.ExportUnlockedIds`). Al load: `PlantDatabase.ImportDiscoveredPlantCodes(saveData.discoveredPlantCodes, persistToPrefs: true)`, `WikiUnlockService.ImportUnlockedIds(saveData.wikiUnlockedIds)`.
- **PlantDatabase:** metodi **`ExportDiscoveredPlantCodes()`** e **`ImportDiscoveredPlantCodes(...)`** per integrazione con save slot. (Nota architetturale: internamente resta uso di `PlayerPrefs` per persistenza discovery; pulizia futura quando si chiuderà la UX Wiki/Night Research.)
- **WikiUnlockService:** **`ExportUnlockedIds()`** e **`ImportUnlockedIds(...)`** per salvataggio/caricamento nello slot.

### 2.5 Lab: Extractor, Catalizzatore, Fusion, Incubator e tooltip Provenienza

- **Extractor** (`Extractor.cs`): consumo frutto tramite helper che itera su `Items.AllFruitTypeIds` (non più solo `Items.Fruits`/`Items.FruitsKnown`); metadata passati all’output spora.
- **LabExtractorPanelController:** riconoscimento frutti con `Items.IsFruitType`, descrizione input con nomi da `GetItemDisplayName`.
- **ExtractorTooltipTexts:** **`GetOriginTraceLabel(Item item)`** per tooltip “Provenienza” (nome pianta + codice); deduplicazione codici (`NormalizeCombinedCodes`), risoluzione nome da codice (`ResolvePlantDisplayNameFromCode`, `NormalizeCombinedDisplayName`). Tooltip frutti con ActivePowerLabel e PassivePowerLabel.
- **PlayerInventoryPanelController:** tooltip spore/pre-seed con **Provenienza** e poteri ereditati; frutti con display name da `ItemFabric.GetFruitDisplayNameByTypeId` e metadata.
- **LabCatalizzatorePanelController, LabFusionPanelController, LabIncubatorPanelController:** aggiunta riga **Provenienza** negli output tooltip (spora maturata, pre-seed, seed) tramite `GetOriginTraceLabel`.
- **ExtractionResultSnapshot.FromFruit:** priorità a `SourcePlantDisplayName` per nome origine nello snapshot.

### 2.6 Altri allineamenti al loop e frutti specifici

- **ItemConsumptionHandler:** `IsConsumable` e `OnItemConsumed` per tutti i tipi frutto (`Items.IsFruitType`); logica Pure per `FruitArcticPod`.
- **GlobalIconResolver:** categoria “fruit” per tutti i frutti specifici (`Items.IsFruitType`).
- **LabMinigameExtractor** (legacy): consumo frutto da storage tramite helper su `AllFruitTypeIds`; output spora con **`ItemFabric.CreateSporeRawFromFruit(consumedFruit)`**.
- **LabMicroscope:** **`PeekInputSpore()`** per lettura metadata spora in input. **MicroscopeHUDView:** a esito positivo minigame aggiunge **`ItemFabric.CloneSpore(inputSpore)`** invece di item generico spore.
- **PipetteView:** disattivato path che generava `Seed001`; messaggio “Legacy path disattivato: usa Incubator”.
- **PotDebugConsole:** risoluzione frutto atteso con `ItemFabric.ResolveFruitTypeIdForPlant`.
- **GlobalStateInspector:** lista debug item aggiornata a frutti specifici, rimossi placeholder semi/spore.
- **EndOfDaySequenceController:** fix bottone YES non cliccabile — `pickingMode` su overlay/root a `Position` quando visibile e `Ignore` quando nascosto; **`RegisterModalButton`** per gestione click su tutti i pulsanti modale.

### 2.7 Terminal Pot — STATUS: barra frutti maturi

- **PlantCardV3TerminalController:** nella sezione VITAL PARAMETERS dell’output STATUS aggiunta **progress bar testuale** per “FRUTTI MATURI”: `x/3` con barra `[███░░░]` e contesto (maturi vs “disponibile a stadio MATURO”) in base a `pot.AmountFruits` e `PlantStage`.

---

## 3. PARTE B — Rifiniture Terminal Pot (HUD, comandi in avvio, scrollbar)

### 3.1 Riga HUD: codice, livello, famiglia

- **UXML:** nella riga `pcv3-hud-plant-code-level-row` aggiunto label **`pcv3-hud-plant-family`** (accanto a code e level).
- **USS:** stili per `.pcv3-hud-plant-family` e varianti colore (standard giallo, pure verde, evil rosso); colore **livello** portato a grigio `rgb(192, 200, 197)` come codice.
- **Controller:** campo `_hudPlantFamily`, query in Awake. In **RefreshHudFromSelectedPot**:
  - **Codice:** `$"[{FormatPlantFamilyBadge(state.PlantCode)}]"` → es. `[STD-001]`.
  - **Livello:** invariato come testo; colore da USS (grigio).
  - **Famiglia:** `_hudPlantFamily.enableRichText = true`; testo `"Famiglia: ---"` se vuoto, altrimenti `"Famiglia: " + "<color=#hex>Standard|Pure|Evil</color>"` (solo nome famiglia colorato). Helper **GetPlantFamilyForDisplay** (da PlantData o metadata PotStateModel), **GetPlantFamilyLabel** (nome in maiuscolo per uso testuale).

### 3.2 Lista comandi in automatico all’avvio

- Alla fine di **BootSequenceRoutine()** (dopo “[BOOT] Booting Sequence completato” e `RenderWelcome(clearConsole: false)`): chiamata **PrintStartCommands()**.
- In **Open()** con boot disattivato: dopo `RenderWelcome(clearConsole: true)` chiamata **PrintStartCommands()**.
- **Bug fix:** la lista non appariva perché **PrintStartCommands()** non chiamava **FlushConsole()**. Aggiunto **FlushConsole()** alla fine di `PrintStartCommands()` così l’output sia visibile sia in avvio sia quando si digita START/HELP.

### 3.3 Allineamento scrollbar verde

- **Problema:** thumb verde scrollbar console sforava a destra (track visivamente troppo stretta).
- **Causa:** in **ApplyConsoleScrollbarStyle()** era impostato `vScroller.style.width = 8`; in USS track 20px e thumb 12px → overflow.
- **Fix:** commentato `vScroller.style.width = 8` nel controller. In USS su `.pcv3-console-scroll .unity-scroll-view__vertical-scroller` aggiunti **padding-left/right 4px** e **box-sizing: border-box** per centrare il thumb 12px nella track 20px.

---

## 4. File modificati (riepilogo)

| File | Modifiche (Task 1 + Terminal) |
|------|-------------------------------|
| `Item.cs` | Campi SourcePlantDisplayName, ActivePowerLabel, PassivePowerLabel. |
| `Items.cs` | Frutti specifici, StarterInventoryTypeIds, helper IsFruitType / IsSpecificFruitType / IsLegacyFruitType, AllFruitTypeIds. |
| `ItemFabric.cs` | FruitDefinition, CreateItemWithMetadata esteso, propagazione metadata in pipeline spora/pre-seed/seed, ResolveFruitTypeIdForPlant, CloneSpore, ApplyBaseFruitMetadata, ecc. |
| `PotStateModel.cs` | Campi payload completo, ApplySeedMetadata, ClearSeedRuntimePayload. |
| `PotActions.cs` | DoPlant(Item seedItem), ApplySeedMetadata; DoHarvest con frutti specifici e CreateItemWithMetadata. |
| `PotSlot.cs` | CollectFruits con frutti specifici e metadata da PotStateModel. |
| `SaveManager.cs` | InventoryItemData con power labels; serializzazione/deserializzazione discovery e wiki; migrazione legacy fruit; versione inventory. |
| `GameManager.cs` | Starter inventory da StarterInventoryTypeIds. |
| `PlantDatabase.cs` | ExportDiscoveredPlantCodes, ImportDiscoveredPlantCodes. |
| `WikiUnlockService.cs` | ExportUnlockedIds, ImportUnlockedIds. |
| `PlantCardV3TerminalController.cs` | Coda Plant con ItemPayload; STATUS barra frutti; HUD famiglia (code [badge], livello grigio, Famiglia: Nome colorato); PrintStartCommands in boot/open e FlushConsole in PrintStartCommands; scrollbar senza override width. |
| `PotAutomationRunner.cs` | AutomationAction.ItemPayload, DoPlant con seed Item. |
| `Extractor.cs` | Consumo frutto da AllFruitTypeIds. |
| `LabExtractorPanelController.cs` | Riconoscimento frutti specifici. |
| `ExtractorTooltipTexts.cs` | GetOriginTraceLabel, NormalizeCombinedCodes, ResolvePlantDisplayNameFromCode; tooltip frutti con power labels. |
| `PlayerInventoryPanelController.cs` | Tooltip spore/pre-seed/frutti con Provenienza e power labels; GetItemDisplayName per frutti specifici. |
| `LabCatalizzatorePanelController.cs`, `LabFusionPanelController.cs`, `LabIncubatorPanelController.cs` | Provenienza in tooltip output. |
| `ExtractionResultSnapshot.cs` | FromFruit con SourcePlantDisplayName. |
| `ItemConsumptionHandler.cs` | IsFruitType per consumo. |
| `GlobalIconResolver.cs` | Categoria fruit per frutti specifici. |
| `LabMinigameExtractor.cs` | Consumo frutto e CreateSporeRawFromFruit. |
| `LabMicroscope.cs` | PeekInputSpore. |
| `MicroscopeHUDView.cs` | CloneSpore per output. |
| `PipetteView.cs` | Disattivato path Seed001. |
| `PotDebugConsole.cs` | ResolveFruitTypeIdForPlant. |
| `GlobalStateInspector.cs` | Lista item con frutti specifici, senza placeholder. |
| `EndOfDaySequenceController.cs` | Fix YES cliccabile (pickingMode, RegisterModalButton). |
| `PlantCardV3_Terminal.uxml` | Label pcv3-hud-plant-family. |
| `PlantCardV3_Terminal.uss` | Stili famiglia, livello grigio, scrollbar padding/box-sizing. |
| Asset `fruit-ferric-pod.asset`, `fruit-arctic-pod.asset`, `fruit-glass-pod.asset` | ItemConfig per i tre frutti. |

---

## 5. Riepilogo per QA

- **Task 1 — Loop end-to-end:** Frutto specifico → Lab (Extractor → Catalizzatore → Fusion → Incubator) → seed con metadata → piantumazione in Dome (anche da coda terminale) → crescita → harvest → frutto con stesso tipo e metadata. Save e reload: inventario, stato vasi, discovery e wiki restano coerenti. Inventario iniziale: solo frutti specifici e materiali, nessun seme/spora placeholder.
- **Terminal Pot — HUD:** Codice `[STD-001]`, livello grigio, “Famiglia: Standard/Pure/Evil” con solo il nome colorato (giallo/verde/rosso). Lista comandi visibile in automatico dopo boot (o welcome se boot off). Scrollbar verde centrata nella track.

---

## 6. Note tecniche

- **Discovery/Wiki:** SaveManager scrive e legge `discoveredPlantCodes` e `wikiUnlockedIds` nello slot; PlantDatabase usa ancora PlayerPrefs internamente — da considerare debito tecnico per quando si implementerà la UX Wiki/Night Research.
- **Rich text famiglia:** Label con `enableRichText = true` e `<color=#RRGGBB>` solo sul nome famiglia; “Famiglia: ” resta grigio da USS.
- **Flush console:** Ogni output che usa solo `AppendRawLine()` deve essere seguito da `FlushConsole()` per essere visibile sulla Label.

---

*Fine DEV REPORT 0070.*
