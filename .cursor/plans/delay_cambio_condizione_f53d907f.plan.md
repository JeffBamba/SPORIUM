---
name: DELAY CAMBIO CONDIZIONE
overview: Introduzione di un sistema di anticipo che mostra al player la condizione prevista per domani quando la pianta sta peggiorando, senza modificare la logica di gameplay esistente che usa ConditionLabel.
todos:
  - id: extend-potstatemodel
    content: Aggiungere campi ProjectedCondition e ProjectedConditionScore a PotStateModel.cs con inizializzazione a -1
    status: pending
  - id: implement-projection-calculation
    content: Creare metodo CalculateProjectedCondition() in PlantConditionSystem.cs che stima condizione di domani basandosi su ForecastDirection e ScoreDelta
    status: pending
    dependencies:
      - extend-potstatemodel
  - id: integrate-daycycle
    content: Integrare calcolo ProjectedCondition in DayCycleController.CalculatePlantConditions() solo quando ForecastDirection == Down
    status: pending
    dependencies:
      - implement-projection-calculation
  - id: update-ui-plantcard
    content: Modificare PlantCardV2DataBinder.BindCondition() per mostrare formato 'Attuale → Prevista' quando ProjectedCondition è peggiore
    status: pending
    dependencies:
      - integrate-daycycle
  - id: update-ui-potdetails
    content: Modificare PotDetailsWidget.UpdateConditionUI() per mostrare condizione prevista con stesso formato
    status: pending
    dependencies:
      - integrate-daycycle
  - id: handle-save-load
    content: Aggiungere ProjectedCondition e ProjectedConditionScore a PotStateData in SaveManager.cs con gestione retrocompatibilità
    status: pending
    dependencies:
      - extend-potstatemodel
  - id: test-implementation
    content: "Testare: previsione appare/scompare correttamente, salvataggi vecchi funzionano, ConditionLabel non è influenzato"
    status: pending
    dependencies:
      - update-ui-plantcard
      - update-ui-potdetails
      - handle-save-load
---

# DELAY CAMBIO CONDIZIONE - Sistema di Anticipo Condizione Pianta

## Obiettivo

Implementare un sistema che anticipa al player quando la condizione della pianta sta per peggiorare, mostrando una "condizione prevista" nella UI **senza modificare** la logica di gameplay esistente che usa `ConditionLabel` per bloccare avanzamento e modificare crescita.

## Strategia: Separazione Condizione Effettiva / Prevista

**Principio fondamentale**: `ConditionLabel` rimane invariato e continua a essere usato per tutti i calcoli di gameplay. Aggiungiamo un campo separato `ProjectedCondition` solo per visualizzazione UI.

```javascript
┌─────────────────────────────────────────────────────────┐
│  CONDIZIONE EFFETTIVA (ConditionLabel)                  │
│  - Usata per: Blocco avanzamento, Modificatori crescita │
│  - Aggiornata: Immediatamente quando score cambia       │
│  - Non modificare questo comportamento                  │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│  CONDIZIONE PREVISTA (ProjectedCondition) - NUOVO      │
│  - Usata per: Solo visualizzazione UI                  │
│  - Calcolata: Solo se ForecastDirection == Down        │
│  - Mostrata: "Sana → Stressata (previsto domani)"     │
└─────────────────────────────────────────────────────────┘
```



## File da Modificare

### 1. `Assets/_Project/Scripts/Dome/PotStateModel.cs`

**Modifiche**:

- Aggiungere campo `ProjectedCondition` (int, default -1)
- Aggiungere campo `ProjectedConditionScore` (int, default -1)
- Inizializzare in `PlantSeed()` e costruttore

**Righe da modificare**: ~74-76 (dopo `ForecastDirection`)

### 2. `Assets/_Project/Scripts/Dome/PotSystem/Condition/PlantConditionSystem.cs`

**Modifiche**:

- Aggiungere metodo pubblico `CalculateProjectedCondition()` che stima condizione di domani
- Aggiungere metodo helper `GetWarningColor()` per colore UI quando prevista è peggiore

**Logica previsione**:

- Se `ForecastDirection == Down`: stima score di domani usando stesso delta attuale
- Se `ForecastDirection == Up` o `Stable`: non calcolare (nessun warning necessario)

### 3. `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

**Modifiche**:

- In `CalculatePlantConditions()` (riga ~1810-1826), dopo calcolo condizione attuale:
- Se `result.Forecast == ForecastDirection.Down && result.ScoreDelta < -5`:
    - Calcola `ProjectedCondition` usando nuovo metodo
    - Salva in `pot.ProjectedCondition` e `pot.ProjectedConditionScore`
- Altrimenti: reset a -1 (nessuna previsione)

**ATTENZIONE**: Non modificare la logica esistente che aggiorna `ConditionLabel` (righe 1823-1826)

### 4. `Assets/_Project/Scripts/UI/UIToolkit/PlantCard/PlantCardV2DataBinder.cs`

**Modifiche**:

- In `BindCondition()` (riga ~357-403):
- Dopo calcolo `conditionResult`, verificare se `state.ProjectedCondition >= 0` e `state.ProjectedCondition > state.ConditionLabel`
- Se sì: mostrare formato "CondizioneAttuale → CondizionePrevista" con colore warning
- Altrimenti: comportamento normale

**Formato UI**: `"{conditionName} → {projectedName}"` con tooltip che spiega "Previsto per domani se la tendenza continua"

### 5. `Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs`

**Modifiche**:

- In `UpdateConditionUI()` (riga ~2750-2764):
- Stessa logica di `PlantCardV2DataBinder`: mostrare condizione prevista se disponibile e peggiore

### 6. `Assets/_Project/Scripts/Core/SaveManager.cs`

**Modifiche**:

- In `PotStateData` class (riga ~521-550):
- Aggiungere `public int projectedCondition = -1;`
- Aggiungere `public int projectedConditionScore = -1;`
- In `CollectPotStates()` (riga ~316-343):
- Salvare `projectedCondition` e `projectedConditionScore`
- In `ApplyPotStates()` (riga ~353+):
- Caricare `projectedCondition` e `projectedConditionScore` con default -1 per retrocompatibilità

### 7. `Assets/_Project/Scripts/UI/VaultMap/AlwaysVisiblePotHUD.cs` (se usato)

**Modifiche**:

- Stessa logica di visualizzazione condizione prevista

## Implementazione Step-by-Step

### Step 1: Estendere PotStateModel

- Aggiungere campi `ProjectedCondition` e `ProjectedConditionScore`
- Inizializzare a -1 in tutti i costruttori/metodi di reset

### Step 2: Implementare Calcolo Previsione

- Creare `PlantConditionSystem.CalculateProjectedCondition()`
- Logica: usa `ForecastDirection` e `ScoreDelta` attuale per stimare score di domani
- Mappare score previsto a condizione usando `MapScoreToCondition()`

### Step 3: Integrare Calcolo in DayCycleController

- Dopo aggiornamento `ConditionLabel` (riga ~1825), aggiungere calcolo `ProjectedCondition`
- Solo se `ForecastDirection == Down` e `ScoreDelta < -5`
- Reset a -1 se condizioni non soddisfatte

### Step 4: Aggiornare UI

- Modificare `PlantCardV2DataBinder.BindCondition()` per mostrare previsione
- Modificare `PotDetailsWidget.UpdateConditionUI()` per mostrare previsione
- Formato: "Attuale → Prevista" con colore warning (giallo/arancione)

### Step 5: Gestire Salvataggi

- Aggiungere campi a `PotStateData`
- Gestire default -1 per salvataggi vecchi (retrocompatibilità)

## Rischi e Mitigazioni

### Rischio 1: Confusione Player

**Mitigazione**: UI chiara con formato "Sana → Stressata (previsto domani)" e tooltip esplicativo

### Rischio 2: Previsione Errata

**Mitigazione**: Previsione si aggiorna ogni giorno. Se player migliora parametri, previsione scompare automaticamente

### Rischio 3: Salvataggi Vecchi

**Mitigazione**: Default -1, ricalcolo automatico al primo calcolo dopo caricamento

### Rischio 4: Performance

**Mitigazione**: Calcolo solo se `ForecastDirection == Down`, nessun overhead se non necessario

## Test Necessari

1. **Test Previsione**: Verificare che previsione appaia quando condizione peggiora (ForecastDirection == Down)
2. **Test Scomparsa**: Verificare che previsione scompaia quando player migliora parametri
3. **Test Salvataggi**: Verificare compatibilità con salvataggi vecchi (default -1)
4. **Test UI**: Verificare visualizzazione in PlantCardV2, PotDetailsWidget, AlwaysVisiblePotHUD
5. **Test Gameplay**: Verificare che ConditionLabel non sia influenzato (blocchi avanzamento funzionano come prima)

## Note Importanti

- **NON modificare** la logica esistente di `ConditionLabel` per gameplay
- **NON usare** `ProjectedCondition` per calcoli di crescita o blocchi avanzamento
- Previsione è **solo informativa** per il player
- Se previsione è migliore o uguale a condizione attuale, non mostrare nulla (solo warning per peggioramento)

## Dipendenze

- Nessuna dipendenza esterna
- Compatibile con sistema esistente