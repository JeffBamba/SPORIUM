# PlantCard V2.0 - Setup Guide

## Panoramica

PlantCard V2.0 è una nuova HUD dettaglio pianta implementata con Unity UIToolkit (UI Builder + USS). Sostituisce gradualmente la UI legacy (`PotDetailsWidget`) con un'interfaccia più moderna e configurabile.

## Struttura File

### File UIToolkit
- `PlantCardV2.uxml` - Struttura UI completa
- `PlantCardV2.uss` - Stili CSS con palette colori, animazioni, effetti

### Scripts C# - Presentation Layer
- `PlantCardV2Controller.cs` - Controller principale
- `PlantCardV2DataBinder.cs` - Helper per data binding pulito
- `PlantCardV2Config.cs` - ScriptableObject configurazione UI

### Scripts C# - Component Layer
- `Components/RotaryKnobUI.cs` - Componente rotary knob riutilizzabile
- `Components/VitalParameterBox.cs` - Componente parametro vitale riutilizzabile
- `Components/PlantDiaryNotes.cs` - Sistema note diario
- `Components/PlantDiaryManager.cs` - Manager singleton per note

### Scripts C# - Helper Layer
- `Helpers/PlantCardFormatters.cs` - Formattazione testo
- `Helpers/PlantCardColorCalculator.cs` - Calcolo colori dinamici
- `Helpers/PlantCardCalculators.cs` - Calcoli (percentages, stress, etc.)

## Setup Step-by-Step

### Passo 1: Creare PlantCardV2Config Asset

1. **Apri Unity Editor** e naviga nella cartella `Assets/Resources/Configs/`
   - Se la cartella non esiste, creala: `Assets/Resources/Configs/`

2. **Crea PlantCardV2Config:**
   - Click destro in `Assets/Resources/Configs/`
   - Seleziona: `Create → Sporae → UI → PlantCardV2Config`
   - Rinomina: `PlantCardV2Config`

3. **Configura valori nell'Inspector:**
   - **Palette Colori**: Verifica che i colori siano corretti (verde LED, blu info, rosso warning, etc.)
   - **Thresholds - Fertilizer Level**: 
     - OptimalMin: `60`
     - OptimalMax: `90`
     - WarningMin: `50`
     - WarningMax: `100`
   - **Thresholds - Condition Score**:
     - OptimalMin: `70`
     - OptimalMax: `100`
     - WarningMin: `60`
     - WarningMax: `70`
   - **Thresholds - Mold Risk**: Verifica livelli (Low=1, Medium=2, High=3)

4. **Salva** l'asset (Ctrl+S)

### Passo 2: Creare GameObject PlantCardV2 nella Scena

1. **Nella Hierarchy**, click destro → `Create Empty`
   - Rinomina: `PlantCardV2`

2. **Aggiungi componente UIDocument:**
   - Seleziona `PlantCardV2`
   - Click `Add Component` → `UI → UI Document`
   - Nel campo **Source Asset**, assegna `Assets/_Project/UI/UIToolkit/PlantCard/PlantCardV2.uxml`

3. **Aggiungi componente PlantCardV2Controller:**
   - Click `Add Component` → Cerca `PlantCardV2Controller`
   - Nel campo **Config**, assegna `PlantCardV2Config` creato al Passo 1
   - Lascia **Current Pot Slot** vuoto (verrà assegnato dinamicamente)

### Passo 3: (Opzionale) Creare PanelSettings

1. **Crea PanelSettings:**
   - Click destro in `Assets/_Project/UI/UIToolkit/PlantCard/`
   - Seleziona: `Create → UI Toolkit → Panel Settings Asset`
   - Rinomina: `PlantCardV2Settings`

2. **Configura PanelSettings:**
   - **Target Display**: `Display 1`
   - **Scale Mode**: `Constant Pixel Size` o `Scale With Screen Size`
   - **Reference Resolution**: `1920x1080` (o risoluzione target)

3. **Assegna a UIDocument:**
   - Seleziona `PlantCardV2` GameObject
   - Nel componente **UIDocument**, campo **Panel Settings**, assegna `PlantCardV2Settings`

### Passo 4: Disabilitare UI Legacy (Transizione Graduale)

1. **Nella scena Unity**, seleziona il GameObject con `PotDetailsWidget`

2. **Nell'Inspector**, trova la sezione **"UI System Selection"**

3. **Deseleziona** il checkbox `Use Legacy UI` (imposta `_useLegacyUI = false`)
   - La UI legacy non si aprirà più quando si seleziona un pot
   - Fallback disponibile: basta riattivare `_useLegacyUI = true` se ci sono problemi

### Passo 5: Collegare PlantCardV2Controller con Selezione Pot

**IMPORTANTE**: Il GameObject con `PlantCardV2Controller` è stato creato al **Passo 2** (nome: `PlantCardV2`).

**Opzione A: Tramite Eventi (Consigliato - Già Implementato)**

Lo script `PlantCardV2Opener.cs` è già stato creato e si trova in:
`Assets/_Project/Scripts/UI/UIToolkit/PlantCard/PlantCardV2Opener.cs`

1. **Crea un nuovo GameObject** nella scena (es. "UI Manager" o "PlantCardV2Opener")
   - Click destro nella Hierarchy → `Create Empty`
   - Rinomina: `PlantCardV2Opener`

2. **Aggiungi componente PlantCardV2Opener:**
   - Seleziona il GameObject `PlantCardV2Opener`
   - Click `Add Component` → Cerca `PlantCardV2Opener`
   - Aggiungi il componente

3. **Assegna PlantCardV2Controller:**
   - Nel componente `PlantCardV2Opener`, campo **Plant Card Controller**
   - Trascina il GameObject `PlantCardV2` (creato al Passo 2) dal campo
   - Oppure selezionalo dal menu dropdown

**Opzione B: Modifica Diretta PotSlot**

- Modifica `PotSlot.cs` per chiamare `PlantCardV2Controller.ShowForPot()` invece di `PotDetailsWidget.ShowDetails()`

## Data Binding

Il sistema usa un **DataBinder** pulito che separa logica da presentazione:

- **PotStateModel** → Lettura sola (non modifica diretta)
- **PlantData** → Lookup tramite `PlantDatabase.Instance.GetPlantDataByCode()`
- **PotActions** → Chiamate ai metodi esistenti senza modificarli
- **PlantConditionSystem** → Calcolo condizione tramite `CalculateCondition()`
- **MoldSystem** → Calcolo mold risk tramite `GetMoldRiskLevel()`

### Aggiornamento Automatico

La UI si aggiorna automaticamente tramite eventi:
- `PotEvents.OnPotStateChanged` → Refresh completo
- `PotEvents.OnPlantStageChanged` → Aggiorna growth stage
- `PotEvents.OnPotActionFailed` → Mostra feedback errore

## Modificare UI in UI Builder

### Aprire UI Builder

1. **Seleziona** `PlantCardV2.uxml` in Project window
2. **Double-click** per aprire in UI Builder
3. **Modifica** struttura, stili, layout

### Modificare Stili USS

1. **Seleziona** `PlantCardV2.uss` in Project window
2. **Modifica** direttamente nel code editor o tramite UI Builder
3. **Palette Colori**: Modifica variabili CSS in `.plant-card-v2` selector

### Aggiungere Nuovi Elementi

1. **Apri UI Builder** con `PlantCardV2.uxml`
2. **Aggiungi** nuovi VisualElement/Button/Label nella hierarchy
3. **Assegna** classi USS esistenti o crea nuove
4. **Riferisci** nuovi elementi in `PlantCardV2DataBinder.cs` se necessario

## Configurazione Avanzata

### Modificare Thresholds Colori

1. **Seleziona** `PlantCardV2Config` asset
2. **Modifica** thresholds in Inspector:
   - Fertilizer: Range ottimale/warning
   - Condition: Range ottimale/warning
   - Mold Risk: Livelli LOW/MEDIUM/HIGH/CRITICAL

### Aggiungere Nuovi Parametri Vitali

1. **Aggiungi** nuovo elemento in `PlantCardV2.uxml` (tab Vital Parameters)
2. **Crea** nuovo `VitalParameterBox` in `PlantCardV2DataBinder.cs`
3. **Aggiorna** `BindVitalParameters()` per includere nuovo parametro

### Personalizzare Animazioni

Le animazioni sono definite in `PlantCardV2.uss`:
- **LED Pulse**: `@keyframes led-pulse` (2s ease-in-out infinite)
- **Knob Rotation**: `transition-property: rotate` (0.3s)
- **Tab Fade**: `transition-duration: 0.15s`
- **Button Hover/Press**: `:hover` e `:active` pseudo-states

Modifica direttamente nel file USS.

## Troubleshooting

### UI Non Si Apre

- Verifica che `PlantCardV2Controller` sia assegnato correttamente
- Verifica che `PlantCardV2Config` sia assegnato al controller
- Controlla console per errori

### Dati Non Aggiornati

- Verifica che eventi `PotEvents` siano emessi correttamente
- Controlla che `PotStateModel` sia valido
- Verifica che `PlantData` sia trovato tramite `PlantDatabase`

### Colori Non Corretti

- Verifica `PlantCardV2Config` thresholds
- Controlla che `PlantCardColorCalculator` usi config correttamente
- Verifica valori in `PotStateModel` (hydration, fertilizer, condition score)

### Rotary Knobs Non Funzionano

- Verifica che click areas siano presenti in UXML
- Controlla che `RotaryKnobUI` sia inizializzato correttamente
- Verifica che `PotActions.DoWater()` e `DoLight()` siano chiamati

## Architettura

### Layer Architecture

1. **Data Layer** (Read-Only): `PotStateModel`, `PlantData`, `PotSystemConfig`
2. **Business Logic Layer** (No Direct Modification): `PotActions`, `PlantConditionSystem`, `MoldSystem`
3. **Presentation Layer** (UI Binding): `PlantCardV2Controller`, `PlantCardV2DataBinder`, `PlantCardV2Config`
4. **Component Layer** (Reusable UI Components): `RotaryKnobUI`, `VitalParameterBox`, `PlantDiaryNotes`
5. **Helper Layer** (Utilities): `PlantCardFormatters`, `PlantCardColorCalculator`, `PlantCardCalculators`

### Vantaggi Architettura

- **Zero Modifiche ai Sistemi Esistenti**: Tutti i sistemi rimangono invariati
- **Transizione Graduale**: Flag `_useLegacyUI` permette disabilitare UI esistente senza rimuoverla
- **Configurabilità**: `PlantCardV2Config` ScriptableObject permette modifiche senza toccare codice
- **Separazione Responsabilità**: Helper classes separano logica da presentazione
- **Event-Driven**: Aggiornamenti automatici tramite eventi, nessun polling necessario
- **UI Builder Friendly**: Tutti gli elementi hanno classi USS modificabili in UI Builder

## Note Finali

- La UI è **fixed width 1400px** (desktop only come specificato)
- Font monospaced (Courier New o Consolas) configurato a livello progetto
- Box-shadow e glow effects usano approcci compatibili con UIToolkit
- Tutti gli elementi sono modificabili in UI Builder senza toccare codice C#

