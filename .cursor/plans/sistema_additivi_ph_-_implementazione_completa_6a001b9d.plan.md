---
name: Sistema Additivi pH - Implementazione Completa
overview: Implementazione del sistema Additivi (Basico/Acido) che sostituisce lo Spray Antifungino, con selezione da inventario, integrazione con sistema muffe, pH e HUD esistenti, seguendo i pattern consolidati del codebase.
todos:
  - id: items-constants
    content: Aggiungere costanti AdditiveBasic e AdditiveAcid in Items.cs
    status: pending
  - id: items-assets
    content: Creare ItemConfig assets STR-004-Basic.asset e STR-004-Acid.asset in Resources/Items/
    status: pending
    dependencies:
      - items-constants
  - id: mold-reduce
    content: Implementare MoldSystem.ReduceMoldRiskLevel() per ridurre livello muffe di 1 (o azzerare se ≤1)
    status: pending
  - id: mold-increase
    content: Implementare MoldSystem.IncreaseMoldRiskLevel() con logica infestazione pot vicino a livello 3
    status: pending
  - id: mold-remove-infestation
    content: Modificare MoldSystem.RemoveInfestation() per usare ReduceMoldRiskLevel()
    status: pending
    dependencies:
      - mold-reduce
  - id: potactions-find-nearest
    content: Aggiungere metodo FindNearestPot() in PotActions per trovare pot più vicino
    status: pending
  - id: potactions-do-apply
    content: Implementare DoApplyAdditive() in PotActions con logica pH e muffe
    status: pending
    dependencies:
      - mold-reduce
      - mold-increase
      - potactions-find-nearest
  - id: potactions-validation
    content: "Modificare metodi validazione: CanApplyAdditive(), HasAdditive(), GetApplyAdditiveFailureReason()"
    status: pending
  - id: potactions-retrocompat
    content: Mantenere DoSprayAntifungal() come wrapper retrocompatibile
    status: pending
    dependencies:
      - potactions-do-apply
  - id: ui-selector-create
    content: Creare UIAdditiveSelector come pannello UI Toolkit (UXML/USS) basato su Foundation + Controller C#
    status: pending
    dependencies:
      - items-constants
  - id: ui-selector-integration
    content: Integrare UIAdditiveSelector UI Toolkit in PlantCardV2Controller (OnSprayButtonClicked) + setup UIDocument in scena
    status: pending
    dependencies:
      - ui-selector-create
      - potactions-do-apply
  - id: testing-base
    content: "Test funzionalità base: applicazione additivi, selezione inventario, consumo risorse"
    status: pending
    dependencies:
      - ui-selector-integration
  - id: testing-edge
    content: "Test edge cases: pot livello 3, pot senza vicini, inventario vuoto"
    status: pending
    dependencies:
      - testing-base
  - id: testing-integration
    content: "Test integrazione: HUD pH, tooltip, eventi, retrocompatibilità"
    status: pending
    dependencies:
      - testing-base
---

# Piano Implementazione: Sistema Additivi pH

## Obiettivo

Trasformare lo Spray Antifungino in un sistema di Additivi selezionabili (Basico/Acido) che bilancia il gameplay pH, integrandosi con tutti i sistemi esistenti senza rompere funzionalità attuali.

## Architettura e Pattern Esistenti

### Pattern da Seguire

- **Selector Pattern**: `UISeedSelector` / `UIFertilizerSelector` come template
- **Item System**: `ItemFabric.CreateItemByType()` + `ItemConfig` assets in `Resources/Items/`
- **Action System**: `PotActions.DoXxx()` con validazione e consumo risorse
- **Event System**: `PotEvents.EmitAction()` / `PotEvents.EmitChanged()` per aggiornamento UI
- **HUD Updates**: Eventi `OnPhChanged`, `OnInventoryChanged` per refresh automatico

### Sistemi da Integrare

- **PhSystem**: `RegisterActionDrift()` per modifiche pH
- **MoldSystem**: Nuovi metodi per gestione livelli muffe
- **PlantCardV2Controller**: Handler per nuovo selector
- **HUDPhDisplay**: Aggiornamento automatico via eventi
- **AlwaysVisiblePotHUD**: Mostra pH drift (già implementato)

---

## Fase 1: Preparazione Item System

### Task 1.1: Aggiungere Costanti Items

**File**: `Assets/_Project/Scripts/Core/ItemsSystem/Items.cs`

- Aggiungere `AdditiveBasic = "STR-004-Basic"`
- Aggiungere `AdditiveAcid = "STR-004-Acid"`
- Mantenere `SprayAntifungal` per retrocompatibilità (deprecato)

### Task 1.2: Creare ItemConfig Assets

**Directory**: `Assets/Resources/Items/`

- Creare `STR-004-Basic.asset` (Additivo Basico)
- TypeId: `STR-004-Basic`
- Stesso costo di `STR-004` originale
- CanStack: true
- Creare `STR-004-Acid.asset` (Additivo Acido)
- TypeId: `STR-004-Acid`
- Stesso costo di `STR-004` originale
- CanStack: true

**Nota**: Gli asset devono essere creati manualmente in Unity Editor seguendo il pattern degli altri item.---

## Fase 2: Estensione MoldSystem

### Task 2.1: Metodo ReduceMoldRiskLevel

**File**: `Assets/_Project/Scripts/Dome/PotSystem/Mold/MoldSystem.cs`

- Aggiungere `ReduceMoldRiskLevel(PotStateModel potState)`
- Logica: Se livello ≤ 1 → azzera tutto, altrimenti riduce di 1
- Reset `DaysAtMoldRiskLevel3` e `IsInfested` se scende sotto 3
- Logging appropriato

### Task 2.2: Metodo IncreaseMoldRiskLevel

**File**: `Assets/_Project/Scripts/Dome/PotSystem/Mold/MoldSystem.cs`

- Aggiungere `IncreaseMoldRiskLevel(PotStateModel potState, PotStateModel nearbyPot = null)`
- Logica: Se livello < 3 → aumenta di 1 (clamp 0-3)
- Se livello = 3 → infesta pot vicino (se fornito e valido)
- Se pot vicino raggiunge livello 3, inizia contatore `DaysAtMoldRiskLevel3 = 1`
- Logging appropriato con warning per infestazione pot vicino

### Task 2.3: Modificare RemoveInfestation

**File**: `Assets/_Project/Scripts/Dome/PotSystem/Mold/MoldSystem.cs`

- Modificare `RemoveInfestation()` per chiamare `ReduceMoldRiskLevel()`
- Mantenere retrocompatibilità con codice esistente (potatura, etc.)

---

## Fase 3: Modifiche PotActions

### Task 3.1: Helper FindNearestPot

**File**: `Assets/_Project/Scripts/Dome/PotActions.cs`

- Aggiungere metodo privato `FindNearestPot()`
- Usa `FindObjectsOfType<PotSlot>()` per trovare tutti i pot
- Calcola distanza con `Vector3.Distance()`
- Restituisce `PotStateModel` del pot più vicino (escluso se stesso)
- Gestisce null safety

### Task 3.2: Metodo DoApplyAdditive

**File**: `Assets/_Project/Scripts/Dome/PotActions.cs`

- Sostituire `DoSprayAntifungal()` con `DoApplyAdditive(string additiveTypeId)`
- Validazione: verifica item in inventario, risorse, vaso valido
- Consumo: risorse (azione) + item additivo
- Effetti pH: `RegisterActionDrift(+5 o -5, actionName, potId)`
- Effetti muffe:
- Basico: `MoldSystem.ReduceMoldRiskLevel(_potState)`
- Acido: `MoldSystem.IncreaseMoldRiskLevel(_potState, FindNearestPot())`
- Eventi: `PotEvents.EmitAction()` / `PotEvents.EmitChanged()`
- Logging dettagliato

### Task 3.3: Metodi di Validazione

**File**: `Assets/_Project/Scripts/Dome/PotActions.cs`

- Modificare `CanSprayAntifungal()` → `CanApplyAdditive()`
- Aggiungere `HasAdditive()` (verifica Basic o Acid disponibili)
- Modificare `GetSprayAntifungalFailureReason()` → `GetApplyAdditiveFailureReason()`
- Mantenere `HasSprayAntifungal()` per retrocompatibilità (potatura)

### Task 3.4: Retrocompatibilità DoSprayAntifungal

**File**: `Assets/_Project/Scripts/Dome/PotActions.cs`

- Mantenere `DoSprayAntifungal()` come wrapper che chiama `DoApplyAdditive(Items.AdditiveBasic)`
- Aggiungere deprecation warning nel log
- Questo permette a codice legacy (potatura) di continuare a funzionare

---

## Fase 4: UI Additive Selector (UI Toolkit + Foundation)

### Task 4.1: Creare UIAdditiveSelector (UI Toolkit + Foundation)

**Prerequisito**: usare la Foundation UI Toolkit:

- `Assets/_Project/UI/UIToolkit/Foundation/SP-Foundation.uss`
- `Assets/_Project/UI/UIToolkit/Foundation/SP-Panel-Base.uss`
- `Assets/_Project/UI/UIToolkit/Foundation/Components/...`

**Files** (NUOVI):

- `Assets/_Project/UI/UIToolkit/AdditiveSelector/AdditiveSelector.uxml`
- `Assets/_Project/UI/UIToolkit/AdditiveSelector/AdditiveSelector.uss`
- `Assets/_Project/Scripts/UI/UIToolkit/AdditiveSelector/AdditiveSelectorController.cs`

#### UI (UXML/USS)

- Import USS in ordine (Foundation flow):

1. `Foundation/SP-Foundation.uss`
2. `Foundation/SP-Panel-Base.uss`
3. `Foundation/Components/SP-Button.uss` (e/o altri necessari)
4. `AdditiveSelector.uss` (layout/override locali)

- Target estetico: **uguale al selector semi UI Toolkit**:
- Reference file: `Assets/_Project/UI/UIToolkit/SeedInventory/SeedInventoryMenu.uxml`
- Reference stile: `Assets/_Project/UI/UIToolkit/SeedInventory/SeedInventoryMenu.uss`
- Struttura/feeling da replicare:
- overlay fullscreen scuro
- pannello centrale con griglia overlay
- header (iconbox + title/subtitle + close)
- ScrollView lista
- cancel button in fondo

- Root overlay fullscreen (es. `addsel-overlay`) + pannello centrale (es. `sp-panel sp-panel--dialog`)
- Elementi minimi:
- Titolo
- Lista/ScrollView per i bottoni additivi
- Messaggio “inventario vuoto”
- Pulsante chiudi/cancel
- Nota UI: anche se la Foundation è la base, qui è richiesto che il layout/spacing e gli stati visivi risultino indistinguibili dal `SeedInventoryMenu` (stesso “look & feel” del flow POT OPS).

#### Controller (C#)

- Responsabilità:
- `Show(PotSlot targetPot)` / `Hide()`
- Costruire la lista con 2 opzioni (Basic/Acid) se quantità > 0
- Aggiornare in realtime su `Inventory.OnInventoryChanged` quando visibile
- Emettere eventi:
- `OnAdditiveSelected(string additiveTypeId)`
- `OnCancelled`
- Recupero Inventory: pattern “ServiceContainer late binding” come `UIFertilizerSelector` / `UISeedSelector`.
- Sorting: usare `UIDocument.sortingOrder = 200`.

### Task 4.2: Integrazione PlantCardV2Controller

**File**: `Assets/_Project/Scripts/UI/UIToolkit/PlantCard/PlantCardV2Controller.cs`

- Modificare `OnSprayButtonClicked()`:
- Invece di chiamare direttamente `DoSprayAntifungal()`
- Chiamare `OpenAdditiveSelector(_currentPotSlot)`
- Aggiungere `OpenAdditiveSelector(PotSlot targetPot)`:
- Pattern identico a `OpenFertilizerSelector()`
- Usa `FindObjectOfType<AdditiveSelectorController>()`
- Sottoscrive eventi `OnAdditiveSelected` / `OnCancelled`
- Aggiungere `OnAdditiveSelected(string additiveTypeId)`:
- Chiama `_potActions.DoApplyAdditive(additiveTypeId)`
- Refresh UI dopo applicazione (come spray attuale)

### Task 4.3: Setup UIAdditiveSelector in Scena

**Nota Manuale Unity**:

- Creare cartella `Assets/_Project/UI/UIToolkit/AdditiveSelector/` e aggiungere `AdditiveSelector.uxml/.uss`
- In `SCN_VaultMap` aggiungere GameObject `UIAdditiveSelector` con:
- `UIDocument` (assegna `VisualTreeAsset` = `AdditiveSelector.uxml`)
- `AdditiveSelectorController`
- Impostare `UIDocument.sortingOrder = 200`
- Verificare `PanelSettings` assegnato (stesso usato dagli altri UI Toolkit runtime)
- Test: apri PlantCard → click Spray → compare selector → seleziona Basic/Acid → applica → UI si aggiorna

---

## Fase 5: Integrazione HUD e Tooltip

### Task 5.1: Verifica HUDPhDisplay

**File**: `Assets/_Project/Scripts/UI/VaultMap/HUDPhDisplay.cs`

- **Nessuna modifica necessaria**: già sottoscritto a `OnPhChanged`
- Il tooltip pH si aggiorna automaticamente quando viene applicato additivo
- `GetCalculationBreakdown()` mostra già "AdditiveBasic" o "AdditiveAcid" nel breakdown

### Task 5.2: Verifica AlwaysVisiblePotHUD

**File**: `Assets/_Project/Scripts/UI/VaultMap/AlwaysVisiblePotHUD.cs`

- **Nessuna modifica necessaria**: mostra già pH drift della pianta
- Non mostra azioni applicate (solo drift pianta)

### Task 5.3: Verifica PlantCardV2DataBinder

**File**: `Assets/_Project/Scripts/UI/UIToolkit/PlantCard/PlantCardV2DataBinder.cs`

- **Nessuna modifica necessaria**: tooltip pH usa `PhSystem.GetCalculationBreakdown()`
- Mostra automaticamente "AdditiveBasic" o "AdditiveAcid" nel breakdown

### Task 5.4: Verifica TopBarController

**File**: `Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs`

- **Nessuna modifica necessaria**: già sottoscritto a `OnPhChanged`
- Aggiornamento automatico del display pH

---

## Fase 6: Testing e Validazione

### Task 6.1: Test Funzionalità Base

- Test applicazione Additivo Basico: pH +5, muffe ridotte
- Test applicazione Additivo Acido: pH -5, muffe aumentate
- Test selezione da inventario (UIAdditiveSelector)
- Test consumo risorse e item

### Task 6.2: Test Edge Cases

- Test pot già a livello 3 muffe + additivo acido → infestazione pot vicino
- Test pot livello 1 muffe + additivo basico → azzeramento
- Test pot senza vicini + additivo acido livello 3 → warning log
- Test inventario vuoto → messaggio appropriato

### Task 6.3: Test Integrazione

- Test aggiornamento HUD pH dopo applicazione
- Test tooltip pH mostra contributo additivo
- Test retrocompatibilità `DoSprayAntifungal()` (potatura)
- Test eventi `PotEvents` emessi correttamente

### Task 6.4: Test Non-Regressione

- Verificare che sistema muffe esistente funzioni ancora
- Verificare che sistema pH esistente funzioni ancora
- Verificare che altre azioni (fertilizzante, potatura) funzionino ancora
- Verificare che HUD esistenti non si rompano

---

## Note Implementative

### Retrocompatibilità

- `DoSprayAntifungal()` mantenuto come wrapper per non rompere codice esistente
- `HasSprayAntifungal()` mantenuto per potatura
- `Items.SprayAntifungal` mantenuto (deprecato ma funzionante)

### Pattern Consistency

- Seguire esattamente il pattern di `UIFertilizerSelector` per `UIAdditiveSelector`
- Usare stesso sistema di eventi e cleanup
- Stesso approccio late binding con `ServiceContainer`

### Logging

- Log dettagliati per debug (come sistema esistente)
- Warning quando pot vicino non trovato a livello 3
- Info per ogni applicazione additivo con effetti

### Error Handling

- Null safety su tutti i riferimenti
- Validazione input (additiveTypeId valido)
- Fallback graceful se sistemi non disponibili

---

## File Modificati/Creati

### File Modificati

1. `Assets/_Project/Scripts/Core/ItemsSystem/Items.cs`
2. `Assets/_Project/Scripts/Dome/PotSystem/Mold/MoldSystem.cs`
3. `Assets/_Project/Scripts/Dome/PotActions.cs`
4. `Assets/_Project/Scripts/UI/UIToolkit/PlantCard/PlantCardV2Controller.cs`

### File Creati

1. `Assets/_Project/Scripts/UI/VaultMap/UIAdditiveSelector.cs`
2. `Assets/Resources/Items/STR-004-Basic.asset` (manuale Unity)
3. `Assets/Resources/Items/STR-004-Acid.asset` (manuale Unity)

### File Non Modificati (Verificati)

- `HUDPhDisplay.cs` - aggiornamento automatico via eventi
- `AlwaysVisiblePotHUD.cs` - mostra solo drift pianta
- `PlantCardV2DataBinder.cs` - usa `PhSystem.GetCalculationBreakdown()`
- `TopBarController.cs` - aggiornamento automatico via eventi

---

## Ordine di Implementazione Consigliato

1. **Fase 1**: Item System (costanti + assets manuali)