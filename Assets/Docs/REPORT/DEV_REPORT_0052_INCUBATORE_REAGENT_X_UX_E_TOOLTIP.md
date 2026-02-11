# DEV REPORT 0052 — Incubatore Reagente X, naming semi, tooltip tratti, picker inventario

**Data:** 2026-02-11  
**Scope:** Incubatore (Reagente X: famiglie/tratti possibili, nome pianta, bug Avvia), nomenclatura tooltip (Tratti Fissi/Stabili/Instabili), inventario picker ordinato, pipeline Lab e display semi.

---

## 1. Nomenclatura genetica: Tratti Fissi / Stabili / Instabili

- **Regola:** In tutti i tooltip, al posto di "Genetica: FIXED/STABLE/UNSTABLE" e "% mutazione futura" si usa:
  - **Tratti: Fissi** (0% di mutare), **Stabili** (25%), **Instabili** (50%).
  - Etichetta **"% di mutare"** al posto di "% mutazione futura".
- **ExtractorTooltipTexts.cs:** Aggiunti `GeneticTypeToTrattiLabel(GeneticType?)` e `GeneticTypeToPercentMutare(GeneticType?)`; tutti i tooltip (frutto, output spora raw, demo, unknown) usano la nuova nomenclatura.
- **PlayerInventoryPanelController:** Tooltip Pre-Seed, spore, generico e sottotesto spore usano `GeneticTypeToTrattiLabel` e "% di mutare".
- **LabCatalizzatorePanelController, LabFusionPanelController, LabIncubatorPanelController:** Output tooltip con "Tratti:" e "% di mutare" (Fusion/Incubator anche "Tratti selezionati" per i semi).
- **ExtractionResultSnapshot:** Campo `Tipo` e commenti aggiornati a "Fissi"/"Stabili"/"Instabili".
- **PotStateModel.cs:** Tooltip in inspector aggiornato alla nuova nomenclatura.

---

## 2. Incubatore: solo scelte possibili con Reagente X

- **Famiglia:** Con Reagente X il dropdown "Famiglia finale" mostra **solo le famiglie del Pre-Seed** (ParentFamilyA, ParentFamilyB). Rimosse le opzioni fisse STANDARD, PURE, EVIL, IPNOTICHE; non si può scegliere una famiglia non presente nelle spore sorgente.
- **Tratti:** Il dropdown tratti mostra **solo i tratti delle famiglie del Pre-Seed** (da `CandidateTraitsCsv` / `BuildCandidateTraitsCsv`). Nessun tratto generico aggiunto; se la lista è vuota si usa il fallback "BalancedGrowth" per evitare Avvia sempre disabilitato.
- **Validazione:** Con un solo tratto disponibile è consentito usare lo stesso tratto per entrambi i campi (un solo tratto fissato). `IsReagentXSelectionValid()` richiede famiglia, almeno tratto 1 e nome; se nome = "Nome personalizzato" richiede testo nel campo custom.

---

## 3. Nome seme e selettore nome (Reagente X)

- **Item.cs:** Aggiunta proprietà **`CustomPlantName`**; usata per il nome scelto dal giocatore (Incubatore con Reagente X).
- **SaveManager:** Aggiunto campo `customPlantName` in `InventoryItemData`; serializzazione e deserializzazione in lettura/scrittura inventario.
- **Stessa pianta** (stesso codice da due spore): un’unica opzione nel dropdown nome = nome della pianta madre (es. "Arctic Hask").
- **Ibrido** (due piante diverse): opzioni nome = combinazioni (es. "Arctic Fern", "Ferric Hask", "Fern Arctic", "Hask Ferric", "Ferric Fern × Arctic Hask", "Arctic Hask × Ferric Fern") + **"Nome personalizzato"** con TextField per nome libero (max 64 caratteri).
- **LabIncubatorPanel.uxml:** Aggiunta riga "Nome pianta" (DropdownField `lab-inc-x-name`) e riga "Nome personalizzato" (TextField `lab-inc-x-name-custom`, nascosta di default).
- **LabIncubatorPanelController:** `BuildNameOptionsForX`, `GetPlantBaseName`; binding nome/custom; passaggio `chosenPlantName` a `CreateSeedFromPreSeed`. Con Reagente Y o senza reagente il nome di default è la prima opzione da `BuildNameOptionsForX` (es. nome madre o primo ibrido).
- **ItemFabric.CreateSeedFromPreSeed:** Aggiunto parametro `chosenPlantName = null`; `item.CustomPlantName = chosenPlantName`.

---

## 4. Display "Seme di Pianta XX"

- **PlayerInventoryPanelController.GetItemDisplayName(string typeId, Item item = null):** Se `item != null` e `item.CustomPlantName` valorizzato, restituisce **"Seme di Pianta " + CustomPlantName**. Altrimenti si usa la logica esistente (PlantData / typeId).
- Utilizzo in inventario (righe semi) e nell’output dell’Incubatore (testo e tooltip).

---

## 5. Bug Incubatore: Pre-seed in input e Avvia con Reagente X

- **Pre-seed "già caricato":** La label non indica più "Pre-Seed (1)" generica ma **"In inventario: Pre-Seed xN"** (con N quantità effettiva), per chiarire che il Pre-Seed è in inventario e viene consumato solo a "Avvia".
- **Reagente X – Avvia non partiva:**  
  - **Causa 1:** In `RefreshDisplay` il pulsante veniva abilitato **prima** di `RefreshReagentXSelectors()`, quindi al primo giro `_selectedFamilyX`, `_selectedTrait1X`, `_selectedNameX` erano ancora null e `IsReagentXSelectionValid()` falliva.  
  - **Soluzione:** `RefreshReagentXSelectors()` viene chiamato **prima** del calcolo di `canAvvia` e dell’abilitazione del pulsante.  
  - **Causa 2:** Liste vuote (nome/famiglia/tratti) lasciavano le variabili di selezione null.  
  - **Soluzione:** Fallback in `BuildNameOptionsForX` (se `SourcePlantCodeMetadata` vuoto → opzione "Seme"); in `BuildFamilyOptionsForX` (se nessuna famiglia → "STANDARD"); in `BuildTraitOptionsForX` (se nessun tratto → "BalancedGrowth").
- **Reagente X – due tratti uguali:** Il secondo dropdown tratto usa `SetDropdownChoicesWithExclude` per avere default diverso dal primo, così con due tratti disponibili Avvia non restava disabilitato per tratti uguali.

---

## 6. Inventario picker: item adatti al macchinario in cima

- **PlayerInventoryPanelController:** Aggiunta lista **`_pickerAllowedTypesOrdered`** (ordine dei typeId ammessi passati da ogni Lab).
- **ShowAsPicker:** Oltre a `_pickerAllowedTypes` (HashSet) si salva `_pickerAllowedTypesOrdered = new List<string>(allowedTypeIds)`.
- **Rebuild (modalità picker):** Se `_pickerAllowedTypesOrdered` ha elementi, gli slot dell’inventario vengono costruiti in ordine: prima tutti gli slot il cui TypeId è in `_pickerAllowedTypesOrdered` (nell’ordine passato dal Lab), poi gli altri. Così da Extractor compaiono prima Frutti/Frutti conosciuti, da Catalizzatore le Spore Raw, da Pipette le Spore mature, da Incubatore Pre-Seed e Reagenti.

---

## 7. Pipeline Lab e tooltip Pre-Seed in inventario

- Verificato il flusso dati: Extractor → Catalizzatore → Fusione → Incubatore (genetica, famiglia, tratti, metadata) e salvataggio/caricamento inventario (inclusi i nuovi campi).
- **PlayerInventoryPanelController:** Tooltip dedicato per Pre-Seed in inventario: "Tratti (fissati Step 3)", "Famiglie sorgente", "Tratti compatibili" (con WrapValue).

---

## File modificati (principali)

| Area | File |
|------|------|
| Core items | `Item.cs` (CustomPlantName), `ItemFabric.cs` (CreateSeedFromPreSeed + chosenPlantName) |
| Save | `SaveManager.cs` (customPlantName in InventoryItemData) |
| Lab Incubatore | `LabIncubatorPanelController.cs`, `LabIncubatorPanel.uxml` |
| Tooltip / nomenclatura | `ExtractorTooltipTexts.cs`, `PlayerInventoryPanelController.cs`, `LabCatalizzatorePanelController.cs`, `LabFusionPanelController.cs`, `LabIncubatorPanelController.cs`, `ExtractionResultSnapshot.cs`, `PotStateModel.cs` |
| Inventario picker | `PlayerInventoryPanelController.cs` |

---

*Fine DEV REPORT 0052.*
