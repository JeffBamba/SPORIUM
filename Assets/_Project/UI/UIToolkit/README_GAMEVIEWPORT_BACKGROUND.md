# Game Viewport Background Setup

## Panoramica

Sistema per impostare il background color della gameview di SPORIUM con gradiente secondo la palette ufficiale.

## Palette Colori

| Nome | HEX | RGB | Uso |
|------|-----|-----|-----|
| Blu-Nero scuro | `#0f1419` | `rgb(15, 20, 25)` | Background principale (inizio/fine gradiente) |
| Blu-Grigio metallico | `#1a2328` | `rgb(26, 35, 40)` | Background centrale (metà gradiente) |
| Nero-blu vignette | `#0a0f12` | `rgb(10, 15, 18)` | Overlay laterale scurimento |

## Setup nella Scena

### Passo 1: Creare GameObject per Background

1. Nella scena principale, crea un GameObject vuoto
2. Rinomina: `HUD_GameViewportBackground`
3. **Add Component** → `UIDocument`
4. **Add Component** → `Game Viewport Background Controller` (script)

### Passo 2: Configurare UIDocument

1. Seleziona `HUD_GameViewportBackground`
2. Nell'Inspector, trova il componente **UIDocument**:
   - **Source Asset**: Assegna `GameViewportBackground.uxml`
   - **Panel Settings**: Usa lo stesso PanelSettings delle altre HUD o creane uno nuovo
   - **Sorting Order**: Imposta un valore **basso** (es. `-10`) per stare dietro a tutti gli altri elementi UI

### Passo 3: Configurare Controller

1. Seleziona `HUD_GameViewportBackground`
2. Nell'Inspector, trova il componente **Game Viewport Background Controller**:
   - **Main Color**: `rgb(26, 35, 40)` (Blu-Grigio metallico - centro gradiente)
   - **Gradient Start End**: `rgb(15, 20, 25)` (Blu-Nero scuro - inizio/fine)
   - **Vignette Color**: `rgb(10, 15, 18)` (Nero-blu vignette)
   - **Vignette Opacity**: `0.3` (30% opacità)
   - **Gradient Texture**: (opzionale) Texture con gradiente pre-renderizzato
   - **Vignette Texture**: (opzionale) Texture con vignette radiale

## Implementazione Gradiente Completo

UI Toolkit non supporta gradient multi-stop nativamente. Per un gradiente completo, usa una texture:

### Opzione 1: Texture Gradiente (Consigliato)

1. **Crea texture in Photoshop/GIMP**:
   - Nuovo documento: `1920×1080px` (o `256×256px` per ripetizione)
   - Gradiente lineare verticale:
     - **Top**: `#0f1419` (Blu-Nero scuro)
     - **Center**: `#1a2328` (Blu-Grigio metallico)
     - **Bottom**: `#0f1419` (Blu-Nero scuro)
   - Esporta come PNG

2. **Importa in Unity**:
   - Salva in `Assets/_Project/Art/Textures/viewport_gradient.png`
   - Impostazioni Texture:
     - **Texture Type**: `Sprite (2D and UI)`
     - **Wrap Mode**: `Clamp` (per non ripetere)
     - **Filter Mode**: `Bilinear`

3. **Assegna texture al controller**:
   - Seleziona `HUD_GameViewportBackground`
   - Nel componente **Game Viewport Background Controller**
   - Trascina la texture in **Gradient Texture**

### Opzione 2: Texture Vignette

1. **Crea texture vignette in Photoshop/GIMP**:
   - Nuovo documento: `1920×1080px`
   - Gradiente radiale dal centro:
     - **Centro**: Trasparente (alpha 0)
     - **Bordi**: `#0a0f12` (alpha 0.5-0.8)
   - Esporta come PNG con alpha

2. **Importa in Unity**:
   - Salva in `Assets/_Project/Art/Textures/viewport_vignette.png`
   - Impostazioni Texture:
     - **Texture Type**: `Sprite (2D and UI)`
     - **Alpha Source**: `From Input`
     - **Alpha Is Transparency**: ✓

3. **Assegna texture al controller**:
   - Trascina la texture in **Vignette Texture**

## Nota Tecnica: UI Toolkit e Background Image

**Limitazione attuale**: UI Toolkit non supporta direttamente `background-image` con Texture2D in USS. Il controller usa `backgroundColor` come fallback.

**Soluzione futura**: Per usare texture, implementa un shader custom o usa un `VisualElement` con `Image` component (richiede codice C# aggiuntivo).

## Integrazione con HUD Esistente

Il background della gameview deve stare **dietro** a tutti gli altri elementi UI:

1. **TopBar**: Sorting Order `0` (o superiore)
2. **BottomNavigation**: Sorting Order `0` (o superiore)
3. **PlayerStatusPanel**: Sorting Order `0` (o superiore)
4. **GameViewportBackground**: Sorting Order `-10` (o inferiore)

## Test

1. Avvia **Play Mode**
2. Verifica che:
   - Il background copre tutto lo schermo
   - Il colore è `rgb(26, 35, 40)` (Blu-Grigio metallico) come fallback
   - La vignette overlay è visibile ai bordi (opacità 30%)
   - Il background sta dietro a TopBar e BottomNavigation

## Troubleshooting

### Background non visibile
- Verifica che `PanelSettings` sia assegnato al UIDocument
- Controlla che il Sorting Order sia negativo (es. `-10`)
- Verifica che il GameObject sia attivo nella Hierarchy

### Colore non corretto
- Controlla i valori RGB nel componente **Game Viewport Background Controller**
- Verifica che il file `.uss` non sovrascriva i colori

### Texture gradiente non funziona
- **Nota**: UI Toolkit non supporta direttamente texture in background-image via USS
- Usa il colore solido come fallback (già implementato)
- Per texture, implementa shader custom o usa approccio alternativo

