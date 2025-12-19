# Player Status Panel - Setup Instructions

## File Creati

### UI Toolkit Assets
- `PlayerStatusPanel.uxml` - Layout principale del pannello
- `PlayerStatusPanel.uss` - Stili principali e animazioni
- `StatBar.uss` - Stili specifici per le barre dinamiche

### Scripts C#
- `PlayerStatusPanelController.cs` - Controller principale
- `StatBarController.cs` - Componente riutilizzabile per barre
- `PixelCornerDecorator.cs` - Decorazioni angoli (opzionale)

## Setup nella Scena

### Passo 1: Creare GameObject
1. Nella scena principale, trova il GameObject **Canvas** (quello principale con EventSystem, non quello dentro PLY_Player)
2. Clic destro su **Canvas** → **Create Empty**
3. Rinomina il nuovo GameObject: `PlayerStatusPanel`
4. **IMPORTANTE**: Assicurati che `PlayerStatusPanel` sia figlio diretto di **Canvas**, non di altri elementi UI

**Nota**: Se preferisci organizzarlo meglio, puoi anche crearlo come figlio di **HUD** (se esiste nella tua scena), ma deve comunque essere sotto un Canvas per funzionare correttamente.

### Passo 2: Creare PanelSettings (se non esiste già)
**IMPORTANTE**: UI Toolkit richiede un `PanelSettings` asset per funzionare.

1. Nella finestra **Project**, naviga a `Assets/_Project/UI/UIToolkit/`
2. Clic destro nella cartella → **Create** → **UI Toolkit** → **Panel Settings Asset**
3. Rinomina il file: `PlayerStatusPanelSettings` (o un nome a tua scelta)
4. Se hai già un PanelSettings nel progetto, puoi riutilizzarlo

**Nota**: Se vedi il warning "To display UI Document, assign a PanelSettings asset" nell'Inspector, significa che devi completare questo passo.

### Passo 3: Aggiungere Componenti
1. Seleziona il GameObject `PlayerStatusPanel`
2. **Add Component** → `UIDocument`
3. **Add Component** → `Player Status Panel Controller` (script)

### Passo 5: Collegare File UI Toolkit
1. Seleziona il GameObject `PlayerStatusPanel`
2. Nell'Inspector, trova il componente **UIDocument**:
   - **Panel Settings**: Trascina il `PanelSettings` asset creato nel Passo 2 (es. `PlayerStatusPanelSettings`)
   - **Source Asset**: Trascina `Assets/_Project/UI/UIToolkit/PlayerStatusPanel.uxml`
   
**Nota**: Gli style sheets (`PlayerStatusPanel.uss` e `StatBar.uss`) sono già referenziati direttamente nel file UXML, quindi non è necessario aggiungerli manualmente nell'Inspector.

### Passo 6: Configurare Controller
1. Nell'Inspector, trova il componente **Player Status Panel Controller**:
   - **UI Document**: Dovrebbe essere collegato automaticamente
   - **Mock Data**: Valori di test (modificabili per testare diversi stati)
   - **Enable Debug Logs**: Attiva per vedere log in console

## Test

1. **Play Mode**: Avvia la scena
2. Il pannello dovrebbe apparire sul lato sinistro dello schermo, verticalmente centrato
3. Le barre si aggiornano automaticamente ogni 2 secondi (mock data)
4. Testa diversi valori mock per vedere:
   - Cambio colori alle soglie
   - Animazione pulsing per hydration critica (0-25%)
   - Smooth lerp quando valori cambiano

## Mock Data Testing

Modifica i valori nell'Inspector per testare:

### Health
- 85/100 → Verde (normale)
- 50/100 → Giallo (warning)
- 30/100 → Rosso (critico)

### Energy
- 80/100 → Blu (normale)
- 45/100 → Giallo (warning)
- 20/100 → Rosso (critico)

### Hydration
- 90/100 → Verde con glow enhanced (well-hydrated)
- 60/100 → Verde normale
- 40/100 → Giallo (warning)
- 20/100 → Rosso con pulsing (critico)

## Integrazione Futura

Quando i sistemi player saranno implementati:

1. **PlayerHydrationSystem**: Chiama `UpdateHydration(current, max)` quando hydration cambia
2. **Health System**: Chiama `UpdateHealth(current, max)` quando health cambia
3. **Energy System**: Chiama `UpdateEnergy(current, max)` quando energy cambia

Oppure usa eventi:
```csharp
// Nel controller, quando sistemi saranno disponibili:
SubscribeToPlayerEvents();
```

## Note Tecniche

- **Font**: Usa font monospace di sistema (Courier New) se TextMeshPro non configurato
- **Glow Effects**: Implementati via border color (UI Toolkit non supporta box-shadow nativamente)
- **Scanlines**: Overlay con repeating-linear-gradient in USS
- **Performance**: Aggiornamenti solo quando valori cambiano, non ogni frame

## Troubleshooting

### Pannello non visibile
- **Verifica che PanelSettings sia assegnato** nel componente UIDocument (questo è il problema più comune!)
- Verifica che UIDocument abbia Source Asset collegato
- Verifica che il GameObject sia attivo nella scena
- **Nota**: UI Toolkit NON richiede un Canvas padre (a differenza di uGUI). Il PanelSettings gestisce il rendering.
- Controlla console per errori

### Posizionamento errato
- Se il pannello non appare sul lato sinistro, verifica che il Canvas abbia **Render Mode: Screen Space - Overlay**
- Se usi un Canvas con **Screen Space - Camera**, potrebbe essere necessario regolare il posizionamento in USS

### Barre non si aggiornano
- Verifica che PlayerStatusPanelController sia collegato
- Controlla Enable Debug Logs per vedere messaggi
- Verifica che i nomi degli elementi in UXML corrispondano a quelli cercati nel controller

### Colori non cambiano
- Verifica che StatBar.uss sia aggiunto a Additional Style Sheets
- Controlla che le classi CSS siano applicate correttamente

