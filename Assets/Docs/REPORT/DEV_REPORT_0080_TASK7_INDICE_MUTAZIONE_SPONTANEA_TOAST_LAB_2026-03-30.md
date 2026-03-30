# DEV REPORT 0080 — Task 7: indice mutazione (IM), mutazioni spontanee, estrazione spore, toast Foundation, fix picker Lab

**Data:** 2026-03-30  
**Sprint / contesto:** Dome Lab — **Task 7** (indice mutazione, pressione evolutiva, sporazione da frutto, notifiche IM/mutazione, coerenza genetica fino al Catalizzatore). Allineamento a `roadmap_dome_lab_100_069d5bdb.plan.md` e aggiornamenti testuali Task 6/7 ove presenti in repo.  
**Riferimento piano:** `.cursor/plans/roadmap_dome_lab_100_069d5bdb.plan.md` (Task 7 — ambito IM + mutazioni + Lab complementare).  
**Report precedente:** `DEV_REPORT_0079_LAB_IBRIDI_METADATI_UI_PH_UPROOT_2026-03-30.md`

---

## Sommario interventi

1. **`DomeMutationRuntimeService`** — servizio registrato in **`GamePlayInstaller`**: base designer 0–1, bonus botanico (Glasscap) da **`PhSystem`**, `DisplayNormalized` clampato, soglie fascia **`BandStableMax` / `BandBalancedMax`** (0.33 / 0.66) condivise con HUD e pass fine giorno.
2. **TopBar** — slider/base designer sincronizzata sul servizio (`SyncDisplay`); tooltip IM con banda italiana e contributi coerenti col runtime.
3. **Toast Foundation IM** — `DOME-IM-MID`, `DOME-IM-HIGH` in **`NotificationTypeSpecDefaults`**; **`FoundationMutationImWatcher`** ascolta `OnDisplayMutationChanged` e posta toast solo su **salita di fascia** (dopo prime da snapshot autoritativo).
4. **Mutazioni spontanee** — **`DomeSpontaneousMutation.ProcessEndOfDay`**: roll giornaliero post-pipeline **`DayCycleController`** con IM, genetica (`Fixed` escluso, `Stable`→`Unstable` tra gli esiti), muffa/infestazione, ibrido (penalità), bonus fascia **pH** dome; esiti su **`PotStateModel`** (CSV tratti, `TraitPowerPercent`, `ConditionScore`); documentazione **stacking** in XML classe.
5. **`MutationTraitCatalog`** — `ScriptableObject` data-driven (`Resources/MutationTraitCatalog`): finestra livello pianta min/max, parametri toast **DOME-MUT-WATCH** (`watchToastMinIm`, `watchToastChance`), righe pesate per famiglia (Standard/Pure/Evil); fallback builtin se righe assenti.
6. **`DOME-MUT-PLANT`** / **`DOME-MUT-WATCH`** — toast su mutazione applicata e preavviso giornaliero (max 1/giorno, candidati in finestra livello, condizioni IM + random da catalogo).
7. **`DayCycleController`** — `ProcessSpontaneousMutations(dayIndex)` con `gameDay` passato al mut pass; **`GetPotStatesForMutationPass`** con fallback **`DomePotRegistry`** se `_registeredPots` vuoto.
8. **Estrazione frutto (Task 7)** — 1–2 spore RAW; **`FruitSporeExtractionRules`**, **`ExtractionResultSnapshot`**; seconda spora con genetica alternata via **`ItemFabric.CreateSporeRawFromFruit(..., geneticOverride)`**; wiring in **`Extractor`**.
9. **Bugfix Lab — picker inventario** — la callback del picker passava solo `typeId`/stage e tutti i flussi usavano **`TryRemoveFirstSporeByStage` / `TryRemoveFirst`**, rimuovendo spesso **l’istanza sbagliata** quando più item condividevano type/stage. Aggiunti **`Inventory.TryRemoveExactItem`**, overload **`ShowAsPicker(..., Action<string, SporeStage?, Item>)`**, righe spora/frutto che inviano l’**`Item`** selezionato; aggiornati **Catalizzatore**, **Fusione**, **Extractor**. La maturazione **`CreateSporeMaturedFromRaw`** preservava già `GeneticTypeValue`; il bug era la **Raw sbagliata** in input.
10. **Debug** — **`PhSystemDebugConsole`** (tasto **Z**): sezione **valori rapidi IM** + campo manuale (`SyncDisplay`) per testare notifiche IM senza dipendere solo dalla HUD. Strumentazione NDJSON temporanea Task 7 / Catalizzatore **rimossa** dopo verifica (nessun `DebugAgentNdjsonLog` residuo in repo).

---

## 1. Runtime IM e TopBar

### Problema
- L’indice mutazione mostrato in più punti (TopBar, EoD, pass mutazioni) rischiava di divergere se calcolato in silos.

### Soluzione
- **`DomeMutationRuntimeService`** come fonte unica: `PushDesignerBase` / `RefreshFromPh` / `SyncDisplay`; `HasAuthoritativeSnapshot` per evitare IM fantasma a EoD.
- **`TopBarController`**: aggiornamento servizio allineato al valore designer e al **PhSystem** della scena.

**File:** `DomeMutationRuntimeService.cs`, `TopBarController.cs`, `GamePlayInstaller.cs`

---

## 2. Toast IM (Foundation)

### Problema
- Serviva feedback player quando l’IM attraversa le soglie “bilanciato / elevato” coerenti con la HUD.

### Soluzione
- Spec **`DOME-IM-MID`**, **`DOME-IM-HIGH`** con placeholder `{pct}`.
- **`FoundationMutationImWatcher`**: priming banda iniziale; toast solo su **incremento** indice banda (0→1, 1→2).

**File:** `NotificationTypeSpecDefaults.cs`, `FoundationMutationImWatcher.cs` (+ prefab/scena con componente watcher se presente in progetto)

---

## 3. Mutazioni spontanee e catalogo tratti

### Problema
- Pool tratti e gate livello hardcoded non editabili; mancava preavviso “pressione mutazionale” data-driven.

### Soluzione
- **`MutationTraitCatalog`** in `Resources` con righe `gameplayTag` + pesi; `minPlantLevelForSpontaneousMutation`, `maxPlantLevelForSpontaneousMutation` (0 = nessun tetto).
- **`DomeSpontaneousMutation`**: `Resources.Load` catalogo, `FillResolvedWeightedPool` una volta per pass; gate livello; roll chance da IM + modificatori; **`DOME-MUT-WATCH`** con dedup giornaliero.
- Toast **`DOME-MUT-PLANT`** su esito applicato (payload `plantName`, `potId`, `detail`).

**File:** `MutationTraitCatalog.cs`, `Resources/MutationTraitCatalog.asset`, `DomeSpontaneousMutation.cs`, `NotificationTypeSpecDefaults.cs`

---

## 4. Orchestrazione giornaliera e registry

### Problema
- Se i vasi non erano registrati in `_registeredPots`, il pass mutazioni non vedeva stati.

### Soluzione
- **`GetPotStatesForMutationPass`**: se lista registrata vuota, costruzione snapshot da **`DomePotRegistry`** (volumi non duplicati).
- **`ProcessSpontaneousMutations(int dayIndex)`** invoca mut pass con **`gameDay`**.

**File:** `SPOR-BLK-01-03A-DayCycleController.cs`

---

## 5. Estrazione spore da frutto (1–2 RAW)

### Problema
- Allineare output Lab al GDD: possibilità di doppia linea genetica dalla stessa estrazione.

### Soluzione
- Regole quantità e genetica seconda spora centralizzate; **`Extractor`** produce snapshot coerente al ritiro; **`CreateSporeRawFromFruit`** con `geneticOverride` per la variante.

**File:** `FruitSporeExtractionRules.cs`, `ExtractionResultSnapshot.cs`, `ItemFabric.cs`, `Extractor.cs` (e pannelli UI Extractor ove toccati)

---

## 6. Picker inventario e Catalizzatore / Fusione / Extractor

### Problema
- Selezionando una riga spora (o frutto) specifica, l’inventario rimuoveva il **primo** item compatibile per type/stage → spora **instabile** mostrata ma **stabile** processata (maturazione corretta sulla Raw effettivamente rimossa).

### Soluzione
- **`TryRemoveExactItem`**, callback picker a tre argomenti con **`Item`**; Catalizzatore / Fusione / Extractor usano l’istanza scelta.

**File:** `Inventory.cs`, `PlayerInventoryPanelController.cs`, `LabCatalizzatorePanelController.cs`, `LabFusionPanelController.cs`, `LabExtractorPanelController.cs`

---

## 7. Strumentazione QA IM (console Z)

### Problema
- Testare **DOME-IM-*** senza alzare manualmente tutti i controlli di scena.

### Soluzione
- Preset IM 0%…100% e soglie MID/HIGH (~34%, ~67%) che chiamano `SyncDisplay` sul servizio con **`PhSystem`** della console.

**File:** `PhSystemDebugConsole.cs`

---

## File modificati / rilevanti (tabella)

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/Dome/DomeMutationRuntimeService.cs` | Servizio IM Task 7 |
| `Assets/_Project/Scripts/Dome/DomeSpontaneousMutation.cs` | Pass mutazioni, catalogo, watch toast, stacking doc |
| `Assets/_Project/Scripts/Dome/MutationTraitCatalog.cs` | **Nuovo** — catalogo ScriptableObject |
| `Assets/_Project/Resources/MutationTraitCatalog.asset` | Istanza Resources + righe default |
| `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs` | Mut pass + dayIndex + fallback registry |
| `Assets/_Project/Scripts/Core/Installers/GamePlayInstaller.cs` | Registrazione `DomeMutationRuntimeService` |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs` | Sync IM + tooltip |
| `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/NotificationTypeSpecDefaults.cs` | Spec DOME-IM-*, DOME-MUT-* |
| `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/FoundationMutationImWatcher.cs` | Toast su attraversamento fascia |
| `Assets/_Project/Scripts/Core/ItemsSystem/Inventory.cs` | `TryRemoveExactItem` |
| `Assets/_Project/Scripts/Core/ItemsSystem/ItemFabric.cs` | Spore da frutto + override genetico |
| `Assets/_Project/Scripts/Core/ItemsSystem/FruitSporeExtractionRules.cs` | Regole 1–2 spore |
| `Assets/_Project/Scripts/Core/ItemsSystem/ExtractionResultSnapshot.cs` | Quantità / varianti estrazione |
| `Assets/_Project/Scripts/Interactables/Extractor.cs` | Output Task 7 |
| `Assets/_Project/Scripts/UI/UIToolkit/PlayerInventory/PlayerInventoryPanelController.cs` | Picker con `Item` |
| `Assets/_Project/Scripts/UI/UIToolkit/Lab/LabCatalizzatorePanelController.cs` | Picker esatto + maturazione |
| `Assets/_Project/Scripts/UI/UIToolkit/Lab/LabFusionPanelController.cs` | Picker spore mature |
| `Assets/_Project/Scripts/UI/UIToolkit/Lab/LabExtractorPanelController.cs` | Picker frutto/item |
| `Assets/_Project/Scripts/Debug/PhSystemDebugConsole.cs` | Preset IM debug |
| `Assets/_Project/Scripts/UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs` | Uso servizio IM in riepilogo (contesto Task 7) |
| `Assets/_Project/Scripts/Dome/PotSystem/Growth/LabHybridGameplayModifiers.cs` | Note scaling/tag mutazione (contesto gameplay) |
| `Assets/_Project/Scripts/Dome/PotSystem/Botanical/BotanicalPowerFacade.cs` | Riepilogo genetica/mutazione HUD |

*Lista non esaustiva di ogni touch minore (icone, card, scene)* — focalizzata sul percorso Task 7 principale.

---

## Regole / vincoli rispettati

- **`ServiceContainer`** per `DomeMutationRuntimeService`, `FoundationNotificationService`; nessun nuovo `FindObjectOfType` per gameplay nel percorso IM/mutazioni (la console debug usa ancora `FindObjectOfType` dove già previsto per tool legacy).
- **`PotActions` / `DayCycleController`**: orchestrazione conservata; mutazioni come pass esplicito dopo tick giornaliero.
- **Parità authoring**: toast testuali in defaults; nessuna duplicazione “campione” HUD Foundation fuori convenzione del progetto.

---

## Note operative (Unity)

- Verificare in Play: **TopBar IM** ↔ **toast MID/HIGH** salendo con preset console Z o slider; **fine giorno** con piante in finestra livello e IM sopra soglia catalogo per **DOME-MUT-WATCH** (probabilità); condizioni rare per **DOME-MUT-PLANT**.
- **Due spore RAW** da frutto: ritiro Extractor → inventario due righe → Catalizzatore con **Seleziona** sulla riga corretta → maturazione mantiene **Stabile/Instabile** atteso (`CreateSporeMaturedFromRaw`).
- Asset **`MutationTraitCatalog`**: path `Resources/MutationTraitCatalog`; modifiche a righe/pesi senza rebuild codice.

---

*Fine DEV REPORT 0080.*
