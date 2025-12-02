# Setup UISeedSelector - Istruzioni per Creare il GameObject nella Scena

## Panoramica
`UISeedSelector` è il componente UI che appare quando premi il bottone "PIANTA" nel `PotDetailsWidget`. 
Ora deve essere creato manualmente nella scena Unity per permetterti di personalizzarne l'estetica.

## Struttura Gerarchica Richiesta

```
Canvas (o Canvas esistente nella HUD)
└── UISeedSelector (GameObject con componente UISeedSelector)
    └── SelectorPanel (GameObject con Image e RectTransform)
        ├── Title (GameObject con TextMeshProUGUI)
        ├── SeedButtonContainer (GameObject con GridLayoutGroup e RectTransform)
        ├── NoSeedsText (GameObject con TextMeshProUGUI, inizialmente disattivato)
        └── CloseButton (GameObject con Button, Image e RectTransform)
            └── Text (GameObject con TextMeshProUGUI per la "X")
```

## Passo-Passo: Creazione Manuale

### 1. Trova o Crea il Canvas Principale
- Se esiste già un Canvas nella scena (es. per la HUD), usa quello
- Altrimenti crea un nuovo Canvas:
  - **GameObject → UI → Canvas**
  - Nome: `Canvas` o `Canvas_HUD`
  - **Render Mode**: Screen Space - Overlay
  - **Sorting Order**: 200 (deve essere sopra altri elementi UI)

### 2. Crea il GameObject UISeedSelector
- **GameObject → Create Empty**
- Nome: `UISeedSelector`
- Posiziona come figlio del Canvas principale
- **Aggiungi Componente**: `UISeedSelector` (script)

### 3. Crea SelectorPanel
- **GameObject → UI → Panel** (oppure Create Empty + Image)
- Nome: `SelectorPanel`
- Posiziona come figlio di `UISeedSelector`
- **RectTransform**:
  - **Anchor**: Center (0.5, 0.5)
  - **Width**: 1400
  - **Height**: 1000
  - **Pos X**: 0
  - **Pos Y**: 0
- **Image Component**:
  - **Color**: (13, 13, 13, 250) - Sfondo scuro semi-trasparente
- **Inizialmente disattivato**: ✅ (uncheck nella Hierarchy)

### 4. Crea Title
- **GameObject → UI → Text - TextMeshPro**
- Nome: `Title`
- Posiziona come figlio di `SelectorPanel`
- **RectTransform**:
  - **Anchor**: Top Stretch (0, 1) to (1, 1)
  - **Height**: 100
  - **Pos Y**: -20
- **TextMeshProUGUI**:
  - **Text**: "Seleziona Seme"
  - **Alignment**: Center
  - **Font Size**: 56
  - **Color**: Bianco (255, 255, 255, 255)
  - **Outline**: Width 0.8, Color Nero (0, 0, 0, 255)

### 5. Crea SeedButtonContainer
- **GameObject → Create Empty**
- Nome: `SeedButtonContainer`
- Posiziona come figlio di `SelectorPanel`
- **RectTransform**:
  - **Anchor**: Stretch (0, 0) to (1, 1)
  - **Left**: 30
  - **Right**: -30
  - **Top**: -30
  - **Bottom**: 120
- **Aggiungi Componente**: `Grid Layout Group`
  - **Cell Size**: X=400, Y=300
  - **Spacing**: X=40, Y=40
  - **Constraint**: Fixed Column Count = 3
  - **Child Alignment**: Upper Left

### 6. Crea NoSeedsText
- **GameObject → UI → Text - TextMeshPro**
- Nome: `NoSeedsText`
- Posiziona come figlio di `SelectorPanel`
- **RectTransform**:
  - **Anchor**: Center (0.5, 0.5)
  - **Width**: 600
  - **Height**: 100
  - **Pos X**: 0
  - **Pos Y**: 0
- **TextMeshProUGUI**:
  - **Text**: "Nessun seme disponibile nell'inventario"
  - **Alignment**: Center
  - **Font Size**: 32
  - **Color**: Giallo (255, 255, 0, 255)
- **Inizialmente disattivato**: ✅ (uncheck nella Hierarchy)

### 7. Crea CloseButton
- **GameObject → UI → Button**
- Nome: `CloseButton`
- Posiziona come figlio di `SelectorPanel`
- **RectTransform**:
  - **Anchor**: Top Right (1, 1)
  - **Width**: 60
  - **Height**: 60
  - **Pos X**: -10
  - **Pos Y**: -10
- **Image Component**:
  - **Color**: Rosso scuro (204, 51, 51, 255)
- **Button Component**: Configurato automaticamente

#### 7.1. Crea Text dentro CloseButton
- **GameObject → UI → Text - TextMeshPro**
- Nome: `Text`
- Posiziona come figlio di `CloseButton`
- **RectTransform**: Stretch (0, 0) to (1, 1)
- **TextMeshProUGUI**:
  - **Text**: "X"
  - **Alignment**: Center
  - **Font Size**: 36
  - **Color**: Bianco (255, 255, 255, 255)

## Collegamento Riferimenti nell'Inspector

Seleziona il GameObject `UISeedSelector` e nell'Inspector collega:

### UI References
- **Selector Panel**: Trascina `SelectorPanel` (GameObject)
- **Seed Button Container**: Trascina `SeedButtonContainer` (Transform)
- **Close Button**: Trascina `CloseButton` (Button)
- **Title Text**: Trascina `Title` → componente `TextMeshProUGUI`
- **No Seeds Text**: Trascina `NoSeedsText` → componente `TextMeshProUGUI`

### Prefab (Opzionale)
- **Seed Button Prefab**: Se vuoi usare un prefab personalizzato per i pulsanti semi, crealo e trascinalo qui

### Settings
- **Title Text Format**: "Seleziona Seme" (default)
- **No Seeds Message**: "Nessun seme disponibile nell'inventario" (default)
- **Canvas Sorting Order**: 200 (default)
- **Improve Readability**: ✅ (default)

## Verifica Setup

1. **Play Mode**: Avvia il gioco e clicca su "PIANTA" su un vaso
2. **Console**: Controlla che non ci siano errori
3. **Visual**: Il pannello `SelectorPanel` dovrebbe apparire con i semi disponibili

## Personalizzazione Estetica

Ora puoi personalizzare liberamente:
- **Colori**: Modifica i colori di sfondo, testo, pulsanti
- **Dimensioni**: Aggiusta RectTransform per dimensioni e posizioni
- **Font**: Cambia font, dimensioni, stili
- **Layout**: Modifica GridLayoutGroup per organizzazione diversa
- **Animazioni**: Aggiungi animazioni di apertura/chiusura
- **Stile Visivo**: Aggiungi immagini, icone, effetti

## Note Importanti

- Il `SelectorPanel` deve essere **disattivato inizialmente** (uncheck nella Hierarchy)
- Il componente `UISeedSelector` deve essere nella scena, non come prefab istanziato
- Assicurati che il Canvas abbia **Sorting Order** sufficientemente alto (200+) per apparire sopra altri elementi UI
- Se modifichi la struttura gerarchica, aggiorna i riferimenti nell'Inspector

## Troubleshooting

**Errore: "selectorPanel non assegnato"**
- Verifica che `SelectorPanel` sia collegato nell'Inspector di `UISeedSelector`

**Errore: "seedButtonContainer non assegnato"**
- Verifica che `SeedButtonContainer` sia collegato nell'Inspector

**Il pannello non appare quando clicco PIANTA**
- Verifica che `UISeedSelector` esista nella scena
- Controlla la Console per errori
- Verifica che il Canvas abbia Sorting Order alto

**I pulsanti semi non appaiono**
- Verifica che `SeedButtonContainer` abbia il componente `GridLayoutGroup`
- Controlla che ci siano semi nell'inventario
- Verifica che `seedButtonPrefab` sia assegnato (se usi un prefab)

