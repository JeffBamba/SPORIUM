# HUD System Setup Instructions

## File Creati

### UI Toolkit Assets
- `TopBar.uxml` - Layout barra superiore con metriche di gioco
- `TopBar.uss` - Stili barra superiore
- `BottomNavigation.uxml` - Layout barra inferiore con room navigation
- `BottomNavigation.uss` - Stili barra inferiore

### Scripts C#
- `TopBarController.cs` - Controller barra superiore
- `BottomNavigationController.cs` - Controller barra inferiore
- `Components/SegmentedBarUI.cs` - Componente riutilizzabile per barre segmentate
- `Components/PixelCornersUI.cs` - Componente decorativo per pixel corners
- `Components/MutationOrbitUI.cs` - Componente per animazione mutation orbit

## Setup nella Scena

### Passo 1: Creare GameObject per TopBar

1. Nella scena principale, crea un GameObject vuoto
2. Rinomina: `HUD_TopBar`
3. **Add Component** → `UIDocument`
4. **Add Component** → `Top Bar Controller` (script)
5. **Add Component** → `Mutation Orbit UI` (script, opzionale - per animazione orbit)

### Passo 2: Configurare TopBar UIDocument

1. Seleziona `HUD_TopBar`
2. Nell'Inspector, trova il componente **UIDocument**:
   - **Source Asset**: Assegna `TopBar.uxml`
   - **Panel Settings**: Crea o assegna un PanelSettings asset (vedi sotto)

### Passo 3: Creare GameObject per BottomNavigation

1. Crea un GameObject vuoto
2. Rinomina: `HUD_BottomNavigation`
3. **Add Component** → `UIDocument`
4. **Add Component** → `Bottom Navigation Controller` (script)

### Passo 4: Configurare BottomNavigation UIDocument

1. Seleziona `HUD_BottomNavigation`
2. Nell'Inspector, trova il componente **UIDocument**:
   - **Source Asset**: Assegna `BottomNavigation.uxml`
   - **Panel Settings**: Usa lo stesso PanelSettings di TopBar o creane uno nuovo

### Passo 5: Creare PanelSettings (se necessario)

1. Nella finestra **Project**, naviga a `Assets/_Project/UI/UIToolkit/HUD/`
2. Clic destro → **Create** → **UI Toolkit** → **Panel Settings Asset**
3. Rinomina: `HUDPanelSettings` (o nome a tua scelta)
4. Assegna questo asset ai **Panel Settings** di entrambi i UIDocument

**Nota**: Se vedi il warning "To display UI Document, assign a PanelSettings asset" nell'Inspector, completa questo passo.

### Passo 6: Configurare Valori Iniziali

#### TopBarController:
- **Actions Left**: 3 (default)
- **Max Actions**: 4 (default)
- **pH Level**: 7.2 (default)
- **Condensation**: 78 (default)
- **Mutation Index**: 0.42 (default)
- **Cry Balance**: 1245 (default)
- **Grate Value**: 12 (default)

#### BottomNavigationController:
- **Active Room**: "dome" (default - DOME sarà la room attiva)
- **Locked Rooms**: "restricted1", "restricted2" (default - le 2 restricted areas sono bloccate)

## Test

1. Avvia **Play Mode**
2. Verifica che:
   - TopBar appare in alto con tutte le metriche
   - BottomNavigation appare in basso con 8 room buttons
   - DOME è evidenziata come active (verde)
   - RESTRICTED areas sono locked (rosso, non cliccabili)
   - Hover su room available mostra effetto scale e cambio colore
   - Click su room available cambia la room attiva

## Integrazione Futura

### Collegare Sistemi Esistenti

Quando i sistemi di gioco saranno pronti, collegali ai controller:

#### TopBarController:
```csharp
// Esempio: Collegare Actions System
var actionsSystem = ServiceContainer.Instance.Get<ActionsSystem>();
if (actionsSystem != null)
{
    actionsSystem.OnActionsChanged += (current, max) => {
        topBarController.UpdateActions(current, max);
    };
}

// Esempio: Collegare pH System
var phSystem = ServiceContainer.Instance.Get<PhSystem>();
if (phSystem != null)
{
    phSystem.OnPhChanged += (value) => {
        topBarController.UpdatePh(value);
    };
}
```

#### BottomNavigationController:
```csharp
// Esempio: Collegare Room Navigation System
bottomNavController.OnRoomButtonClick += (roomId) => {
    var roomSystem = ServiceContainer.Instance.Get<RoomNavigationSystem>();
    if (roomSystem != null)
    {
        roomSystem.ChangeRoom(roomId);
    }
};
```

## Note Tecniche

### Glow Effects
I glow effects (box-shadow triple-layer) sono documentati come **TODO** nei file USS. Per implementarli:
- Usare `Outline` component via C# (Unity UI Toolkit support)
- Oppure creare shader custom per glow effect
- Vedi commenti `// TODO: Add glow effect` nei file USS

### Animazioni
- **Mutation Orbit**: Gestita via `MutationOrbitUI.cs` coroutine
- **pH Pulse**: Gestita via `TopBarController.cs` coroutine (se drift > 1.0)
- **Condensation Idle**: Gestita via `TopBarController.cs` coroutine (±1% variation)
- **Border Glow Scan**: TODO - implementazione futura

### Font
- Usare **Courier New** o **Roboto Mono** come font monospace
- Tutti i testi usano `font-family: monospace` in USS

### Scanline Overlay
- Implementato come `background-color` con opacità (fallback)
- `repeating-linear-gradient` non è completamente supportato in UI Toolkit USS

## Troubleshooting

### TopBar/BottomNavigation non visibili
- Verifica che `PanelSettings` sia assegnato ai UIDocument
- Verifica che i file `.uxml` siano assegnati correttamente
- Controlla che i GameObject siano attivi nella Hierarchy

### Animazioni non funzionano
- Verifica che `MutationOrbitUI` component sia presente su `HUD_TopBar`
- Controlla che le coroutines non siano interrotte

### Room buttons non cliccabili
- Verifica che `BottomNavigationController` sia presente e configurato
- Controlla che i button non siano disabilitati (locked rooms)

### Metriche non si aggiornano
- Verifica che i metodi pubblici dei controller siano chiamati correttamente
- Controlla i log di debug (abilita `_enableDebugLogs` nell'Inspector)

