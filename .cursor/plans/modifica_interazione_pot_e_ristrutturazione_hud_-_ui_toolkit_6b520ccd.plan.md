---
name: Modifica Interazione POT e Ristrutturazione HUD - UI Toolkit
overview: Implementare un nuovo sistema di interazione con i POT usando UI Toolkit (UI Builder). Creare PotActionsMenu in UXML/USS, modificare il flusso di click per mostrare il menu invece dell'HUD completa. L'HUD completa (PlantCardV2) viene mostrata solo quando si clicca INSPECT. Aggiungere dialog di irrigazione e applicare stili retro-futuristici tramite USS.
todos:
  - id: create-pot-actions-menu-uxml
    content: "Creare PotActionsMenu.uxml in UI Builder con struttura menu retro-futuristico: container principale, header con titolo e status indicator, lista bottoni verticale (PLANT, INSPECT, HARVEST, REMOVE), overlay background"
    status: pending
  - id: create-pot-actions-menu-uss
    content: "Creare PotActionsMenu.uss con stili retro-futuristici: riutilizzare CSS variables da PlantCardV2.uss, glow effects, pixel corners, grid pattern, stili bottoni con stati (normal, hover, disabled), color coding azioni"
    status: pending
    dependencies:
      - create-pot-actions-menu-uxml
  - id: create-pot-actions-menu-cs
    content: "Creare PotActionsMenu.cs con UIDocument: metodo ShowForPot() che determina stato pot, aggiorna visibilità bottoni tramite style.display e SetEnabled(), mostra status indicator, handlers click azioni"
    status: pending
    dependencies:
      - create-pot-actions-menu-uxml
      - create-pot-actions-menu-uss
  - id: create-irrigation-dialog-uxml
    content: "Creare IrrigationDialog.uxml in UI Builder: panel principale con bordo blu glow, titolo IRRIGATION PROTOCOL, testo domanda, due bottoni (SÌ IRRIGA blu, NO SOLO PIANTA giallo)"
    status: pending
  - id: create-irrigation-dialog-uss
    content: "Creare IrrigationDialog.uss con stili retro-futuristici coerenti con menu: riutilizzare CSS variables, glow effects blu, pixel corners"
    status: pending
    dependencies:
      - create-irrigation-dialog-uxml
  - id: create-irrigation-dialog-cs
    content: "Creare IrrigationDialog.cs con UIDocument: metodo Show(), Hide(), evento OnDialogResult(bool irrigate), handlers click bottoni"
    status: pending
    dependencies:
      - create-irrigation-dialog-uxml
      - create-irrigation-dialog-uss
  - id: create-pot-actions-menu-opener
    content: Creare PotActionsMenuOpener.cs che si sottoscrive a PotSlot.OnPotSelected e chiama PotActionsMenu.ShowForPot() al click
    status: pending
    dependencies:
      - create-pot-actions-menu-cs
  - id: modify-plant-card-opener
    content: "Modificare PlantCardV2Opener.cs: aggiungere flag _autoOpenOnPotSelected=false, rimuovere sottoscrizione automatica, aggiungere metodo pubblico OpenForInspect()"
    status: pending
  - id: modify-do-plant-irrigation
    content: Modificare PotActions.DoPlant() per accettare parametro bool irrigate=false, impostare idratazione 40% se true, consumare 2 azioni invece di 1, mostrare toast appropriato
    status: pending
  - id: integrate-irrigation-dialog-plant
    content: Modificare PlantCardV2Controller.OnPlantButtonClicked() per aprire IrrigationDialog prima di seed selector e passare risultato a DoPlant()
    status: pending
    dependencies:
      - create-irrigation-dialog-cs
      - modify-do-plant-irrigation
  - id: implement-inspect-action
    content: "Implementare azione INSPECT in PotActionsMenu: chiudere menu con Hide() e aprire PlantCardV2Controller.ShowForPot() quando si clicca INSPECT"
    status: pending
    dependencies:
      - create-pot-actions-menu-cs
      - modify-plant-card-opener
  - id: update-plant-card-data-binder-visibility
    content: "Modificare PlantCardV2DataBinder per gestire visibilità bottoni: nascondere plant-button/plant-action-button quando HasPlant=true, mostrare quando IsEmpty=true, nascondere remove-button quando IsEmpty=true"
    status: pending
  - id: verify-retro-futuristic-styles
    content: "Verificare PlantCardV2.uss: assicurarsi che tutti gli stili retro-futuristici siano presenti (glow, pixel corners, grid pattern, text glow, color coding parametri), aggiungere classi mancanti se necessario"
    status: pending
  - id: test-complete-flow
    content: "Testare flusso completo: click POT vuoto/occupato, azioni PLANT/INSPECT/HARVEST/REMOVE, dialog irrigazione, consumo azioni, visualizzazione HUD con stili"
    status: pending
    dependencies:
      - create-pot-actions-menu-cs
      - create-irrigation-dialog-cs
      - create-pot-actions-menu-opener
      - modify-do-plant-irrigation
      - integrate-irrigation-dialog-plant
      - implement-inspect-action
      - update-plant-card-data-binder-visibility
      - verify-retro-futuristic-styles
---

# Modifica Interazione POT e Ristrutturazione HUD - UI Toolkit

## Obiettivo

Modificare il flusso di interazione con i POT usando UI Toolkit (UI Builder). Creare PotActionsMenu in UXML/USS che mostra un menu "POT OPS" al click. L'HUD completa (PlantCardV2) viene mostrata solo quando si clicca su INSPECT. Aggiungere dialog di irrigazione durante il plant e applicare stili retro-futuristici tramite USS.

## Architettura UI Toolkit

### Flusso Attuale

```javascript
Click POT → PotSlot.OnPotSelected → PlantCardV2Opener → PlantCardV2Controller.ShowForPot()
                                      PotDetailsWidget.ShowDetails() (se legacy UI)
```



### Nuovo Flusso

```javascript
Click POT → PotSlot.OnPotSelected → PotActionsMenu.ShowForPot()
                                      ↓
                              Mostra menu "POT OPS" (UXML/USS)
                                      ↓
                    ┌─────────────────┴─────────────────┐
                    │                                   │
              PLANT (vuoto)                    INSPECT/HARVEST/REMOVE (occupato)
                    │                                   │
                    ↓                                   ↓
         IrrigationDialog                    Azione eseguita
         (UI Toolkit o GameObject)                    │
                    │                                   ↓
                    ↓                    PotActionsMenu.Hide()
         SeedSelector (esistente)        PlantCardV2Controller.ShowForPot()
                    │
                    ↓
         DoPlant(irrigate)
```



## File da Creare (UI Toolkit)

### 1. PotActionsMenu.cs

**Path:** `Assets/_Project/Scripts/UI/UIToolkit/PotActionsMenu/PotActionsMenu.cs`Componente MonoBehaviour con UIDocument che gestisce il menu:

- Riferimento a UIDocument
- Metodo `ShowForPot(PotSlot pot)` che:
- Determina stato pot (`IsEmpty` o `HasPlant`)
- Aggiorna visibilità bottoni in base allo stato tramite classi USS
- Mostra status indicator appropriato ("• EMPTY" / "• OCCUPIED")
- Mostra overlay del menu
- Metodo `Hide()` per nascondere il menu
- Handlers per click bottoni:
- `OnPlantClicked()` → Apri IrrigationDialog → SeedSelector → DoPlant(irrigate)
- `OnInspectClicked()` → Hide menu → PlantCardV2Controller.ShowForPot()
- `OnHarvestClicked()` → PotActions.DoHarvest()
- `OnRemoveClicked()` → PotActions.DoUproot()

### 2. PotActionsMenu.uxml

**Path:** `Assets/_Project/UI/UIToolkit/PotActionsMenu/PotActionsMenu.uxml`Template UI Toolkit per il menu con:

- Container principale con classe `pot-actions-menu`
- Header con `pot-ops-title` e `pot-status-indicator`
- Lista bottoni verticale:
- `action-button-plant` (visibile solo se empty)
- `action-button-inspect` (visibile solo se occupied)
- `action-button-harvest` (visibile solo se occupied, disabled se canHarvest=false)
- `action-button-remove` (visibile solo se occupied)
- Overlay per background scuro

### 3. PotActionsMenu.uss

**Path:** `Assets/_Project/UI/UIToolkit/PotActionsMenu/PotActionsMenu.uss`Stili retro-futuristici per il menu:

- Riutilizza CSS variables da PlantCardV2.uss (`--green-led`, `--blue-info`, etc.)
- Glow effects con `box-shadow` (se supportato) o border-color con opacity
- Pixel corners usando classi `.pixel-corner` (riutilizza da PlantCardV2.uss)
- Grid pattern background opzionale
- Stili bottoni con stati (normal, hover, disabled)
- Color coding per azioni (verde per PLANT, giallo per INSPECT/HARVEST, rosso per REMOVE)

### 4. IrrigationDialog.cs

**Path:** `Assets/_Project/Scripts/UI/UIToolkit/IrrigationDialog/IrrigationDialog.cs`**Opzione A (UI Toolkit - Consigliata):**

- Componente MonoBehaviour con UIDocument
- UXML/USS per dialog modale
- Evento `OnDialogResult(bool irrigate)`
- Stile retro-futuristico coerente con menu

**Opzione B (GameObject Unity - Come PruningDialog):**

- Componente simile a PruningDialog.cs
- Prefab con UI Canvas
- Mantiene compatibilità con sistema esistente

### 5. IrrigationDialog.uxml (se Opzione A)

**Path:** `Assets/_Project/UI/UIToolkit/IrrigationDialog/IrrigationDialog.uxml`Template UI Toolkit per dialog:

- Panel principale con bordo blu glow
- Titolo "IRRIGATION PROTOCOL" con icona goccia
- Testo domanda e descrizione
- Due bottoni con icone e sottotitoli:
- "SÌ, IRRIGA" (blu)
- "NO, SOLO PIANTA" (giallo)

### 6. IrrigationDialog.uss (se Opzione A)

**Path:** `Assets/_Project/UI/UIToolkit/IrrigationDialog/IrrigationDialog.uss`Stili retro-futuristici per dialog:

- Riutilizza CSS variables da PlantCardV2.uss
- Glow effects blu per bordo principale
- Pixel corners

## File da Modificare

### 1. PotSlot.cs

**Path:** `Assets/_Project/Scripts/Interactables/PotSlot.cs`**Modifiche:**

- Mantenere emissione di `OnPotSelected` (non modificare comportamento esistente)

### 2. PlantCardV2Opener.cs

**Path:** `Assets/_Project/Scripts/UI/UIToolkit/PlantCard/PlantCardV2Opener.cs`**Modifiche:**

- Rimuovere sottoscrizione automatica a `PotSlot.OnPotSelected` (o aggiungere flag per disabilitarla)
- Aggiungere metodo pubblico `OpenForInspect(PotSlot pot)` chiamabile da PotActionsMenu

### 3. PotActionsMenuOpener.cs (NUOVO)

**Path:** `Assets/_Project/Scripts/UI/UIToolkit/PotActionsMenu/PotActionsMenuOpener.cs`Nuovo script che:

- Si sottoscrive a `PotSlot.OnPotSelected`
- Chiama `PotActionsMenu.ShowForPot(pot)` al click
- Sostituisce PlantCardV2Opener nel flusso principale

### 4. PotActions.cs

**Path:** `Assets/_Project/Scripts/Dome/PotActions.cs`**Modifiche:**

- Modificare `DoPlant(string seedTypeId = null)` per accettare parametro opzionale `bool irrigate = false`
- Se `irrigate == true`, dopo aver piantato:
- Calcolare 40% di MaxHydration: `int targetHydration = Mathf.RoundToInt(config.MaxHydration * 0.4f)`
- Impostare `_potState.Hydration = targetHydration`
- Consumare 1 azione aggiuntiva (totale 2 azioni) - verificare disponibilità prima
- Mostrare toast "Seed planted and irrigated"
- Se `irrigate == false`, mantenere comportamento attuale (1 azione, toast "Seed planted successfully")

### 5. PlantCardV2Controller.cs

**Path:** `Assets/_Project/Scripts/UI/UIToolkit/PlantCard/PlantCardV2Controller.cs`**Modifiche:**

- In `OnPlantButtonClicked()`, aprire IrrigationDialog prima di seed selector
- Gestire risultato dialog e passare `irrigate` a `DoPlant()`
- **NOTA**: Gli stili retro-futuristici sono già presenti in PlantCardV2.uss, verificare che siano completi

### 6. PlantCardV2DataBinder.cs

**Path:** `Assets/_Project/Scripts/UI/UIToolkit/PlantCard/PlantCardV2DataBinder.cs`**Modifiche:**

- Gestire visibilità di `plant-button` e `plant-action-button`:
- Nascondere quando `state.HasPlant == true` (usare `style.display = DisplayStyle.None`)
- Mostrare quando `state.IsEmpty == true` (usare `style.display = DisplayStyle.Flex`)
- Gestire visibilità di `remove-button`:
- Nascondere quando `state.IsEmpty == true`
- Mostrare quando `state.HasPlant == true`

### 7. PlantCardV2.uss

**Path:** `Assets/_Project/UI/UIToolkit/PlantCard/PlantCardV2.uss`**Modifiche:**

- Verificare che tutti gli stili retro-futuristici siano presenti:
- Glow effects (box-shadow se supportato, altrimenti border-color con opacity)
- Pixel corners (già presenti)
- Grid pattern (già presente)
- Text glow (text-shadow se supportato)
- Color coding parametri (già presente con CSS variables)
- Aggiungere classi per color coding parametri se mancanti:
- `.param-water` (blu #5DB6E3)
- `.param-fertilizer` (giallo #E6C96F)
- `.param-light-stress` (verde/giallo/rosso in base a valore)
- `.param-growth-stage` (verde #7FFF7A)
- `.param-quality-stable` (verde), `.param-quality-standard` (giallo), `.param-quality-unstable` (rosso)

## Implementazione Dettagliata

### Fase 1: Creazione PotActionsMenu (UI Toolkit)

1. **Creare PotActionsMenu.uxml:**

- Container principale con classe `pot-actions-menu`
- Header con `pot-ops-title` e `pot-status-indicator`
- Lista bottoni con classi `action-button`, `action-button-plant`, etc.
- Overlay per background scuro

2. **Creare PotActionsMenu.uss:**

- Importare CSS variables da PlantCardV2.uss (o definire localmente)
- Definire stili per `.pot-actions-menu` con bordo verde glow
- Definire stili per bottoni con stati (normal, hover, disabled)
- Riutilizzare classi `.pixel-corner` da PlantCardV2.uss o definirle localmente

3. **Creare PotActionsMenu.cs:**

- Metodo `ShowForPot(PotSlot pot)` che:
    - Determina stato pot
    - Aggiorna visibilità bottoni usando `style.display` e `SetEnabled()`
    - Aggiorna status indicator text
    - Mostra overlay
- Handlers per click bottoni

### Fase 2: Creazione IrrigationDialog (UI Toolkit)

1. **Creare IrrigationDialog.uxml:**

- Panel principale con classe `irrigation-dialog`
- Titolo e testo domanda
- Due bottoni con classi `dialog-button-irrigate` e `dialog-button-plant-only`

2. **Creare IrrigationDialog.uss:**

- Stili retro-futuristici coerenti con menu
- Glow effects blu per bordo principale

3. **Creare IrrigationDialog.cs:**

- Metodo `Show()` per mostrare dialog
- Metodo `Hide()` per nascondere
- Evento `OnDialogResult(bool irrigate)`
- Handlers per click bottoni

### Fase 3: Modifica Flusso Click Pot

1. **Creare PotActionsMenuOpener.cs:**

- Sottoscriversi a `PotSlot.OnPotSelected`
- Chiamare `PotActionsMenu.ShowForPot(pot)`

2. **Modificare PlantCardV2Opener:**

- Aggiungere flag `[SerializeField] private bool _autoOpenOnPotSelected = false;`
- Se `_autoOpenOnPotSelected == false`, non aprire automaticamente
- Aggiungere metodo pubblico `OpenForInspect(PotSlot pot)`

### Fase 4: Modifica DoPlant per Irrigazione

1. **Modificare PotActions.DoPlant():**

- Aggiungere parametro `bool irrigate = false`
- Se `irrigate == true`:
    - Verificare disponibilità 2 azioni prima di procedere
    - Calcolare e impostare idratazione 40%
    - Consumare 2 azioni totali
    - Mostrare toast appropriato

2. **Modificare PlantCardV2Controller:**

- In `OnPlantButtonClicked()`, aprire IrrigationDialog prima di seed selector
- Passare risultato dialog a `DoPlant()`

### Fase 5: Applicazione Stili Retro-Futuristici

1. **Verificare PlantCardV2.uss:**

- Assicurarsi che tutti gli stili siano presenti
- Aggiungere classi mancanti per color coding parametri

2. **Creare PotActionsMenu.uss:**

- Riutilizzare CSS variables da PlantCardV2.uss
- Applicare stili retro-futuristici coerenti

## Note Tecniche UI Toolkit

- **CSS Variables**: Riutilizzare quelle esistenti in PlantCardV2.uss (`--green-led`, `--blue-info`, etc.)
- **Box-shadow**: UI Toolkit supporta limitatamente box-shadow, usare border-color con opacity per glow effects
- **Text-shadow**: Supportato in UI Toolkit, usare per text glow
- **Grid Pattern**: Usare `background-image` con `repeating-linear-gradient` se supportato, altrimenti background-color con opacity
- **Pixel Corners**: Riutilizzare classi `.pixel-corner` da PlantCardV2.uss o definirle localmente
- **Visibility**: Usare `style.display = DisplayStyle.None/Flex` per mostrare/nascondere elementi
- **Enabled State**: Usare `SetEnabled(false/true)` per disabilitare/abilitare bottoni

## Test Cases

1. **Click su POT vuoto:**

- Mostra menu con solo bottone PLANT
- Status indicator mostra "• EMPTY"
- Altri bottoni non visibili (`display: none`)

2. **Click su PLANT (vuoto):**

- Apre IrrigationDialog (UI Toolkit)
- Seleziona "SÌ, IRRIGA" → Pianta seme, idratazione 40%, 2 azioni consumate
- Seleziona "NO, SOLO PIANTA" → Pianta seme, idratazione 0%, 1 azione consumata

3. **Click su POT occupato:**

- Mostra menu con INSPECT, HARVEST, REMOVE
- Status indicator mostra "• OCCUPIED"
- PLANT non visibile
- HARVEST disabled se `canHarvest == false` (`SetEnabled(false)`)

4. **Click su INSPECT:**

- Chiude menu (`Hide()`)
- Apre PlantCardV2Controller con HUD completa
- HUD mostra stili retro-futuristici (già presenti)

5. **Click su HARVEST:**

- Esegue DoHarvest()
- Chiude menu dopo successo

6. **Click su REMOVE:**

- Esegue DoUproot()
- Chiude menu dopo successo

## Dipendenze

- UI Toolkit già in uso per PlantCardV2
- CSS variables già definite in PlantCardV2.uss
- Sistema di toast esistente per notifiche
- PotActions esistente per logica azioni
- PlantCardV2Controller esistente per HUD completa

## Istruzioni Manuali per Unity Editor

### Setup PotActionsMenu

1. **Creare GameObject PotActionsMenu:**

- Nella Hierarchy, crea un nuovo GameObject vuoto
- Rinominalo: `PotActionsMenu`
- Aggiungi componente `UIDocument` (Add Component → UI Toolkit → UI Document)
- Aggiungi componente `PotActionsMenu` (lo script creato)

2. **Configurare UIDocument:**

- Seleziona il GameObject `PotActionsMenu`
- Nell'Inspector, nel componente `UIDocument`:
    - Assegna `PotActionsMenu.uxml` al campo **Source Asset**
    - Verifica che **Panel Settings** sia assegnato (usa lo stesso di PlantCardV2 se esiste)

3. **Configurare PotActionsMenu Script:**

- Nel componente `PotActionsMenu`:
    - Verifica che il riferimento a `UIDocument` sia assegnato automaticamente
    - Se necessario, assegna manualmente il componente `UIDocument` dello stesso GameObject

### Setup IrrigationDialog (se Opzione A - UI Toolkit)

1. **Creare GameObject IrrigationDialog:**

- Nella Hierarchy, crea un nuovo GameObject vuoto
- Rinominalo: `IrrigationDialog`
- Aggiungi componente `UIDocument`
- Aggiungi componente `IrrigationDialog` (lo script creato)

2. **Configurare UIDocument:**

- Seleziona il GameObject `IrrigationDialog`
- Nell'Inspector, nel componente `UIDocument`:
    - Assegna `IrrigationDialog.uxml` al campo **Source Asset**
    - Verifica che **Panel Settings** sia assegnato

3. **Configurare IrrigationDialog Script:**

- Nel componente `IrrigationDialog`:
    - Verifica che il riferimento a `UIDocument` sia assegnato automaticamente

### Setup PotActionsMenuOpener

1. **Creare GameObject PotActionsMenuOpener:**

- Nella Hierarchy, crea un nuovo GameObject vuoto (o usa uno esistente per UI Managers)
- Rinominalo: `PotActionsMenuOpener`
- Aggiungi componente `PotActionsMenuOpener` (lo script creato)

2. **Configurare PotActionsMenuOpener Script:**

- Nel componente `PotActionsMenuOpener`:
    - Assegna il GameObject `PotActionsMenu` al campo **Pot Actions Menu** (riferimento al componente PotActionsMenu)

### Modificare PlantCardV2Opener

1. **Trovare GameObject PlantCardV2Opener:**

- Cerca nella Hierarchy il GameObject che ha il componente `PlantCardV2Opener`
- Selezionalo

2. **Configurare Flag:**

- Nell'Inspector, nel componente `PlantCardV2Opener`:
    - Imposta **Auto Open On Pot Selected** a `false` (dovrebbe essere il default dopo la modifica)

### Verificare Setup Scena

1. **Verificare che tutti i GameObject siano attivi:**

- `PotActionsMenu` → GameObject attivo (checkbox in alto a sinistra nell'Inspector)
- `IrrigationDialog` → GameObject attivo (se creato)
- `PotActionsMenuOpener` → GameObject attivo

2. **Verificare ordine di esecuzione (opzionale):**

- Se ci sono problemi di inizializzazione, verifica che `PotActionsMenuOpener` sia prima di `PotActionsMenu` nell'ordine di esecuzione (Script Execution Order in Edit → Project Settings → Script Execution Order)

### Test Manuale in Unity

1. **Avvia Play Mode:**

- Premi Play in Unity Editor

2. **Test Click su POT Vuoto:**

- Clicca su un POT vuoto nel gioco
- Verifica che appaia il menu "POT OPS" con solo il bottone PLANT
- Verifica che lo status indicator mostri "• EMPTY"

3. **Test Click su PLANT:**

- Clicca sul bottone PLANT nel menu
- Verifica che appaia il dialog "IRRIGATION PROTOCOL"
- Testa entrambe le opzioni (SÌ IRRIGA e NO SOLO PIANTA)
- Verifica che il seme venga piantato correttamente
- Verifica il consumo azioni (2 per irrigazione, 1 senza)

4. **Test Click su POT Occupato:**

- Clicca su un POT con pianta
- Verifica che appaia il menu con INSPECT, HARVEST, REMOVE
- Verifica che lo status indicator mostri "• OCCUPIED"
- Verifica che PLANT non sia visibile

5. **Test Click su INSPECT:**

- Clicca su INSPECT nel menu
- Verifica che il menu si chiuda
- Verifica che si apra PlantCardV2Controller con HUD completa

6. **Test Click su HARVEST:**

- Clicca su HARVEST (se disponibile)
- Verifica che l'azione venga eseguita correttamente
- Verifica che il menu si chiuda dopo il successo

7. **Test Click su REMOVE:**

- Clicca su REMOVE
- Verifica che la pianta venga rimossa
- Verifica che il menu si chiuda dopo il successo

### Troubleshooting

1. **Menu non appare al click:**

- Verifica che `PotActionsMenuOpener` sia attivo e abbia il riferimento a `PotActionsMenu`
- Verifica che `PotActionsMenu` abbia il componente `UIDocument` con `PotActionsMenu.uxml` assegnato
- Controlla la Console per errori

2. **Dialog non appare:**

- Verifica che `IrrigationDialog` sia attivo
- Verifica che il riferimento a `IrrigationDialog` sia assegnato in `PotActionsMenu.cs` o `PlantCardV2Controller.cs`
- Controlla la Console per errori

3. **Bottoni non funzionano:**

- Verifica che i nomi degli elementi UI in UXML corrispondano a quelli cercati nel codice C# (`Q<Button>("nome-bottone")`)
- Verifica che i bottoni abbiano la classe CSS corretta
- Controlla la Console per errori di riferimento null

4. **Stili non applicati:**

- Verifica che `PotActionsMenu.uss` sia assegnato nel UXML (`<Style src="...">`)
- Verifica che le CSS variables siano definite correttamente
- Controlla che i nomi delle classi CSS corrispondano tra UXML e USS

5. **HUD completa non si apre da INSPECT:**

- Verifica che `PlantCardV2Controller` sia presente nella scena
- Verifica che il riferimento a `PlantCardV2Controller` sia assegnato in `PotActionsMenu.cs`
- Verifica che `PlantCardV2Opener` abbia `_autoOpenOnPotSelected = false`

### Note Importanti

- **Panel Settings**: Assicurati che tutti i UIDocument usino lo stesso PanelSettings per evitare problemi di rendering