---
name: Conversione tooltip in UXML
overview: Piano per spostare tutti i tooltip attualmente costruiti solo in C# nei rispettivi UXML, così da renderli modificabili in UI Builder e uniformi al progetto. Sono esclusi i tooltip non più utilizzati (PlantCard V2, HUDPhDisplay UGUI).
todos: []
isProject: false
---

# Piano: conversione tooltip da solo C# a UXML

## Scope e esclusioni

**Inclusi** (tooltip UIToolkit creati con `new VisualElement()` / `new Label()` da convertire):


| Contesto              | Tooltip                       | Controller                                                                                                                  | UXML di destinazione                                                                                |
| --------------------- | ----------------------------- | --------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| HUD Top Bar           | pH Drift                      | [TopBarController.cs](Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs)                                         | [TopBar.uxml](Assets/_Project/UI/UIToolkit/HUD/TopBar.uxml)                                         |
| HUD Top Bar           | Condensazione                 | TopBarController.cs                                                                                                         | TopBar.uxml                                                                                         |
| Lab Extractor         | Output slot (Spore Raw)       | [LabExtractorPanelController.cs](Assets/_Project/Scripts/UI/UIToolkit/Lab/LabExtractorPanelController.cs)                   | [LabExtractorPanel.uxml](Assets/_Project/UI/UIToolkit/Lab/LabExtractorPanel.uxml)                   |
| Lab Incubator         | Output semi                   | [LabIncubatorPanelController.cs](Assets/_Project/Scripts/UI/UIToolkit/Lab/LabIncubatorPanelController.cs)                   | [LabIncubatorPanel.uxml](Assets/_Project/UI/UIToolkit/Lab/LabIncubatorPanel.uxml)                   |
| Lab Fusion            | Output fusione                | [LabFusionPanelController.cs](Assets/_Project/Scripts/UI/UIToolkit/Lab/LabFusionPanelController.cs)                         | [LabFusionPanel.uxml](Assets/_Project/UI/UIToolkit/Lab/LabFusionPanel.uxml)                         |
| Lab Catalizzatore     | Output catalizzatore          | [LabCatalizzatorePanelController.cs](Assets/_Project/Scripts/UI/UIToolkit/Lab/LabCatalizzatorePanelController.cs)           | [LabCatalizzatorePanel.uxml](Assets/_Project/UI/UIToolkit/Lab/LabCatalizzatorePanel.uxml)           |
| Inventario            | Tooltip riga item             | [PlayerInventoryPanelController.cs](Assets/_Project/Scripts/UI/UIToolkit/PlayerInventory/PlayerInventoryPanelController.cs) | [PlayerInventoryPanel.uxml](Assets/_Project/UI/UIToolkit/PlayerInventory/PlayerInventoryPanel.uxml) |
| PlantCard V3 Terminal | Forecast/Condition (crescita) | [PlantCardV3TerminalController.cs](Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs)       | [PlantCardV3_Terminal.uxml](Assets/_Project/UI/UIToolkit/PlantCardV3/PlantCardV3_Terminal.uxml)     |


**Esclusi** (come richiesto):

- **PlantCard V2**: condition tooltip e growth tooltip — considerato sostituito da V3; non includerli nel piano.
- **HUDPhDisplay** (Canvas/UGUI): già deprecato in favore di TopBarController; tooltip creato con GameObject/TextMeshPro, non UIToolkit — non incluso.

---

## Pattern di conversione (uniforme)

Per ogni tooltip:

1. **UXML**: aggiungere nel punto corretto (stesso parent usato dal C# con `_root.Add(...)`) un blocco tipo:
  - Container: `VisualElement` con `name` uguale a quello usato nel C#, `position: absolute`, `display: none`, stile base (background, border, padding, min/max width) **oppure** delegato a una classe USS condivisa.
  - Figlio: `Label` con `name` uguale a quello usato nel C#, rich text abilitato, stile testo.
2. **C#**: sostituire la creazione con una **sola** query:
  - Se il controller oggi fa `if (_tooltip == null) { _tooltip = new VisualElement(); ... _root.Add(_tooltip); }`, sostituire con `_tooltip = _root.Q<VisualElement>("nome-tooltip"); _tooltipText = _tooltip?.Q<Label>("nome-tooltip-text");` (e rimuovere tutto il blocco che crea e aggiunge l’elemento).
  - Mantenere invariata la logica di show/hide, posizionamento e aggiornamento del testo.

Posizionamento e visibilità restano gestiti in C# (left/top, display Flex/None); stile “base” (colori, bordi, font-size) diventa modificabile da USS/UI Builder tramite classi opzionali.

---

## Dettaglio per contesto

### 1. TopBar.uxml (HUD)

- **Root attuale**: `top-bar` (wrapper unico sotto root del documento).
- **Dove aggiungere**: come ultimi figli di `top-bar`, prima di `glow-frame`, due blocchi:
  - `ph-tooltip` (VisualElement) contenente `ph-tooltip-text` (Label).
  - `condensation-tooltip` (VisualElement) contenente `condensation-tooltip-text` (Label), più il button `condensation-collect-button` già usato in codice (se si vuole spostare anche quello in UXML per coerenza).
- **C#**: in `SetupPhTooltip()` e `SetupCondensationTooltip()` rimuovere la creazione; usare solo `_root.Q<VisualElement>("ph-tooltip")` e `_root.Q<Label>("ph-tooltip-text")` (e analoghi per condensation). Eventuali stili inline critici (es. `pickingMode`) possono restare in C# o essere spostati in USS.

### 2. Lab panels (Extractor, Incubator, Fusion, Catalizzatore)

- **Root usato in C#**: overlay del pannello (es. `lab-ext-overlay` per Extractor). Verificare in ogni controller se `_root` è l’overlay o il panel interno; il tooltip va aggiunto come figlio dello stesso `_root` a cui oggi viene fatto `_root.Add(_outputTooltip)`.
- **UXML**: in ciascun file ([LabExtractorPanel.uxml](Assets/_Project/UI/UIToolkit/Lab/LabExtractorPanel.uxml), [LabIncubatorPanel.uxml](Assets/_Project/UI/UIToolkit/Lab/LabIncubatorPanel.uxml), [LabFusionPanel.uxml](Assets/_Project/UI/UIToolkit/Lab/LabFusionPanel.uxml), [LabCatalizzatorePanel.uxml](Assets/_Project/UI/UIToolkit/Lab/LabCatalizzatorePanel.uxml)) aggiungere un unico VisualElement tooltip come ultimo figlio del root (overlay):
  - Nomi: `lab-ext-output-tooltip` / `lab-inc-output-tooltip` / `lab-fus-output-tooltip` / `lab-cat-output-tooltip`.
  - Figlio: Label senza nome obbligatorio se il C# fa `_outputTooltip.Q<Label>(0)` oppure con nome esplicito (es. `lab-ext-output-tooltip-text`) e C# che fa `Q<Label>("...")`.
- **C#**: rimuovere `EnsureOutputTooltip()` / il blocco che crea `_outputTooltip` e `_outputTooltipText`; in `Start`/`OnEnable` assegnare `_outputTooltip = _root.Q<VisualElement>("..."); _outputTooltipText = _outputTooltip?.Q<Label>("...");`. Se il controller null-checka `_outputTooltip != null` prima di mostrare, lasciare quel check (fallback graceful se UXML non ha il nodo).

### 3. PlayerInventoryPanel.uxml

- **Root**: `inv-overlay` (o l’elemento che il controller usa come `_root` per `Add(_invTooltip)` — tipicamente il root del documento).
- **Dove aggiungere**: come ultimo figlio del root, un VisualElement `inv-tooltip` con figlio Label (es. `inv-tooltip-text`).
- **C#**: in `EnsureInvTooltip()` rimuovere la creazione; solo `_invTooltip = _root.Q<VisualElement>("inv-tooltip"); _invTooltipText = _invTooltip?.Q<Label>("inv-tooltip-text");`. Mantenere il null-check esistente prima di mostrare il tooltip.

### 4. PlantCardV3_Terminal.uxml

- **Root**: `pcv3-root` (primo elemento; `_root` è il rootVisualElement del UIDocument, che corrisponde a questo albero).
- **Dove aggiungere**: come ultimo figlio di `pcv3-root`, un VisualElement `pcv3-forecast-condition-tooltip` con figlio Label `pcv3-forecast-condition-tooltip-text`. Il controller già fa `_root.Q<VisualElement>("pcv3-forecast-condition-tooltip")` e crea solo se null; aggiungendo il nodo in UXML il ramo di creazione non verrà più eseguito.
- **C#**: rimuovere l’intero blocco `if (_forecastConditionTooltip == null) { ... new VisualElement(); ... _root.Add(...); }`. Tenere solo le query: `_forecastConditionTooltip = _root.Q<VisualElement>("pcv3-forecast-condition-tooltip"); _forecastConditionTooltipText = _forecastConditionTooltip?.Q<Label>("pcv3-forecast-condition-tooltip-text");`.

---

## Stile uniforme (opzionale ma consigliato)

- Creare una classe USS condivisa (es. `.sp-tooltip`, `.sp-tooltip-text`) in un foglio Foundation o in ciascun USS di pannello, con:
  - background scuro semi-trasparente, bordo verde (#7FFF7A o variabile), padding, min/max width, `position: absolute`, `display: none` (poi il C# imposta `display: flex` quando visibile).
  - Per il Label: font-size, colore, white-space normal, rich text.
- Applicare quella classe a tutti i nuovi nodi tooltip negli UXML, così da avere look uniforme e un solo posto dove modificare in UI Builder (stili).

---

## Ordine di esecuzione suggerito

1. **TopBar**: aggiungere i due tooltip in [TopBar.uxml](Assets/_Project/UI/UIToolkit/HUD/TopBar.uxml), poi adattare [TopBarController.cs](Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs) (SetupPhTooltip, SetupCondensationTooltip).
2. **Lab**: per ogni pannello Lab, aggiungere il nodo tooltip nel rispettivo UXML e rimuovere la creazione nel controller (EnsureOutputTooltip / equivalente).
3. **PlayerInventory**: aggiungere `inv-tooltip` in [PlayerInventoryPanel.uxml](Assets/_Project/UI/UIToolkit/PlayerInventory/PlayerInventoryPanel.uxml), adattare [PlayerInventoryPanelController.cs](Assets/_Project/Scripts/UI/UIToolkit/PlayerInventory/PlayerInventoryPanelController.cs) (EnsureInvTooltip).
4. **PlantCard V3**: aggiungere `pcv3-forecast-condition-tooltip` in [PlantCardV3_Terminal.uxml](Assets/_Project/UI/UIToolkit/PlantCardV3/PlantCardV3_Terminal.uxml), rimuovere il blocco di creazione in [PlantCardV3TerminalController.cs](Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs).

---

## Verifica finale

- Per ogni schermata: aprire in UI Builder l’UXML e confermare che il tooltip sia presente e modificabile.
- In gioco: hover/azioni che mostrano il tooltip e posizionamento a mouse/ancora devono funzionare come prima (solo la sorgente del markup cambia da C# a UXML).

