# Istruzioni Setup Toast Notification System - Guida Completa per Principianti

Questa guida ti accompagnerà passo-passo nella creazione e configurazione del sistema Toast Notifications in Unity. Ogni passaggio è spiegato in dettaglio.

---

## Passo 1: Creare ToastNotificationConfig Asset

### 1.1 Apri Unity Editor
1. Apri Unity Editor
2. Assicurati che il progetto sia aperto

### 1.2 ⚡ CONFIGURAZIONE AUTOMATICA (CONSIGLIATO)
**Prima di configurare manualmente, prova il tool automatico!**

1. Crea prima l'asset `ToastNotificationConfig` (vedi 1.4)
2. Vai al menu: `Tools` → `Sporae` → `Setup ToastNotificationConfig`
3. Lo script configurerà automaticamente:
   - ✅ Tutti i TypeSettings (colori, durate, code prefix)
   - ✅ UI Settings (width, position, font size, spacing)
   - ✅ Pixel Art Settings (FilterMode.Point)
   - ✅ Global Settings (duration, history)
   - ✅ Cerca automaticamente sprite e font se esistono nel progetto
4. Controlla la Console per vedere cosa è stato trovato automaticamente
5. Assegna manualmente solo gli asset mancanti (sprite, font) se necessario

**Se preferisci configurare manualmente, continua con i passi seguenti.**

### 1.3 Crea la cartella Resources/Configs (se non esiste)
1. Nel **Project window** (in basso), naviga fino a `Assets/Resources/`
2. Se la cartella `Resources` non esiste:
   - Click destro su `Assets` → `Create` → `Folder`
   - Rinomina la cartella in `Resources`
3. Dentro `Resources`, crea una cartella `Configs`:
   - Click destro su `Resources` → `Create` → `Folder`
   - Rinomina in `Configs`

### 1.4 Crea l'asset ToastNotificationConfig
1. Nel **Project window**, naviga a `Assets/Resources/Configs/`
2. Click destro su `Configs` → `Create` → `Spore` → `Toast Notification Config`
   - **Nota**: Se non vedi il menu "Spore", significa che lo script `ToastNotificationConfig.cs` non è stato compilato correttamente. Verifica che non ci siano errori nella Console.
3. Unity creerà un nuovo asset. Rinominalo in `ToastNotificationConfig` (se non ha già questo nome)
4. L'asset dovrebbe essere salvato come `Assets/Resources/Configs/ToastNotificationConfig.asset`

### 1.6 Configura l'asset ToastNotificationConfig (SOLO se non hai usato il tool automatico)
1. Seleziona l'asset `ToastNotificationConfig` nel Project window
2. Nel **Inspector window** (a destra), vedrai diverse sezioni:

#### Sezione "Type Settings"
1. Espandi la sezione **Type Settings**
2. Clicca sul campo **Size** e imposta un numero (es. 20) per avere abbastanza slot
3. Per ogni slot nell'array:
   - **Type**: Seleziona un tipo dal dropdown (es. Success, Error, Warning, Info)
   - **Color**: Click sul quadrato colorato e seleziona un colore. Usa questi valori:
     - Success/Info: R=127, G=255, B=122, A=255 (verde LED #7FFF7A)
     - Warning: R=230, G=201, B=111, A=255 (giallo #E6C96F)
     - Error/Critical: R=211, G=95, B=95, A=255 (rosso #D35F5F)
   - **Default Duration**: Imposta un valore (es. 3.0 per 3 secondi)
   - **Code Prefix**: Inserisci un prefisso (es. "POT-", "CND-", "LGT-")
   - **Severity Icon**: Assegna uno sprite (vedi Passo 2 per creare gli sprite)

#### Sezione "UI Settings"
1. Espandi la sezione **UI Settings**
2. **Fixed Width**: 306
3. **Position Offset**: X = -24, Y = -96
4. **Monospaced Font**: Assegna un TMP_FontAsset
   - **Come creare TMP_FontAsset**: 
     - Seleziona un font nel Project (es. Courier New, Consolas)
     - Menu: `Window` → `TextMeshPro` → `Font Asset Creator`
     - Click `Generate Font Atlas`
     - Click `Save` e salva in `Assets/_Project/Fonts/`
     - Assegna questo TMP_FontAsset al campo Monospaced Font
5. **Font Size**: 10
6. **Character Spacing**: 2

#### Sezione "Pixel Art Settings"
1. Espandi la sezione **Pixel Art Settings**
2. **Border Sprite**: Assegna lo sprite del bordo (vedi Passo 2)
3. **Corner Sprite**: Assegna lo sprite del corner (vedi Passo 2)
4. **Glow Shader**: Opzionale, lascia vuoto se non hai uno shader custom
5. **Texture Filter Mode**: Seleziona `Point` dal dropdown

#### Sezione "Icons"
1. Espandi la sezione **Icons**
2. Assegna gli sprite per:
   - **Alert Circle Icon**: Icona per header
   - **Info Icon**: Icona "i" per severity Info
   - **Warning Icon**: Icona triangolo per severity Warning
   - **Danger Icon**: Icona alert circle per severity Danger
   - **Chevron Icon**: Icona arrow per toggle
   - (Vedi Passo 2 per creare gli sprite)

#### Sezione "Global Settings"
1. Espandi la sezione **Global Settings**
2. **Default Duration**: 3
3. **Max History Entries**: 100
4. **Enable History**: Spunta la checkbox

### 1.6 Salva l'asset
1. Click su `Ctrl+S` (o `Cmd+S` su Mac) per salvare
2. Verifica che l'asset sia in `Assets/Resources/Configs/ToastNotificationConfig.asset`

---

## Passo 2: Creare Sprites (se non esistono)

### 2.1 Crea la cartella per gli sprite
1. Nel **Project window**, naviga a `Assets/_Project/Art/`
2. Se non esiste, crea la cartella `Sprites`:
   - Click destro su `Art` → `Create` → `Folder` → Rinomina in `Sprites`
3. Dentro `Sprites`, crea la cartella `UI`:
   - Click destro su `Sprites` → `Create` → `Folder` → Rinomina in `UI`

### 2.2 Crea gli sprite necessari

**Nota**: Se non hai gli sprite pronti, puoi creare texture temporanee o usare sprite placeholder. Gli sprite devono essere piccoli (2-16 pixel) per lo stile pixel art.

#### Sprite Border_2px_Solid
1. Crea una texture 2x2 pixel (o usa un editor esterno come Paint, GIMP, Photoshop)
2. Colore: bianco o qualsiasi colore (verrà colorato dinamicamente)
3. Salva come `Border_2px_Solid.png` in `Assets/_Project/Art/Sprites/UI/`
4. In Unity, seleziona la texture
5. Nel **Inspector**:
   - **Texture Type**: `Sprite (2D and UI)`
   - **Filter Mode**: `Point (no filter)` ← IMPORTANTE per pixel-perfect
   - **Compression**: `None`
   - Click `Apply`
6. Rinomina lo sprite in `Border_2px_Solid`

#### Sprite Corner_LShape_3x3
1. Crea una texture 3x3 pixel con forma L (angolo)
2. Salva come `Corner_LShape_3x3.png` in `Assets/_Project/Art/Sprites/UI/`
3. In Unity, seleziona la texture
4. Nel **Inspector**:
   - **Texture Type**: `Sprite (2D and UI)`
   - **Filter Mode**: `Point (no filter)`
   - **Compression**: `None`
   - Click `Apply`
5. Rinomina lo sprite in `Corner_LShape_3x3`

#### Sprite Icon_AlertCircle
1. Crea o importa un'icona alert circle (circa 16x16 pixel)
2. Salva in `Assets/_Project/Art/Sprites/UI/Icon_AlertCircle.png`
3. In Unity, configura come sopra (Sprite, Point filter, None compression)

#### Sprite Icon_Info, Icon_Warning, Icon_Danger
1. Crea o importa le icone:
   - **Info**: Icona "i" (16x16 pixel)
   - **Warning**: Icona triangolo (16x16 pixel)
   - **Danger**: Icona alert circle (16x16 pixel)
2. Salva in `Assets/_Project/Art/Sprites/UI/`
3. Configura tutte come Sprite con Filter Mode Point e Compression None

#### Sprite Icon_Chevron
1. Crea o importa un'icona arrow/chevron (16x16 pixel)
2. Salva in `Assets/_Project/Art/Sprites/UI/Icon_Chevron.png`
3. Configura come sopra

**Nota Temporanea**: Se non hai gli sprite pronti, puoi:
- Usare sprite placeholder temporanei
- Lasciare i campi vuoti nell'asset config (il sistema funzionerà comunque, ma senza sprite)
- Aggiungere gli sprite successivamente

---

## Passo 3: Creare Prefab ToastNotificationItem

### 3.1 Crea la cartella per i prefab
1. Nel **Project window**, naviga a `Assets/_Project/Prefabs/`
2. Se non esiste, crea la cartella `UI`:
   - Click destro su `Prefabs` → `Create` → `Folder` → Rinomina in `UI`

### 3.2 Crea il GameObject base
1. Nella **Hierarchy window** (sinistra), click destro → `Create Empty`
2. Rinomina il GameObject in `ToastNotificationItem`
3. Seleziona `ToastNotificationItem` nella Hierarchy

### 3.3 Aggiungi RectTransform
1. Nel **Inspector**, vedrai già un componente `Transform`
2. Click sul menu a 3 puntini (⋮) accanto a `Transform` → `Replace` → `Rect Transform`
   - **Nota**: Se non vedi questa opzione, aggiungi un componente `Rect Transform` manualmente
3. Configura il RectTransform:
   - **Anchor Presets**: Click sull'icona dell'ancora in alto a sinistra del RectTransform
   - Tieni premuto `Shift + Alt` e click su `top-right` (angolo in alto a destra)
   - Questo imposta anchor e pivot a (1, 1)
   - **Width**: 306
   - **Height**: 100 (temporaneo, sarà auto dopo)

### 3.4 Aggiungi il componente script
1. Con `ToastNotificationItem` selezionato, nel **Inspector** click `Add Component`
2. Cerca `Toast Notification UI Item` e selezionalo
3. Il componente verrà aggiunto

### 3.5 Crea la struttura UI - Background
1. Con `ToastNotificationItem` selezionato, click destro su di esso → `UI` → `Image`
2. Rinomina in `Background`
3. Seleziona `Background`
4. Nel **Inspector**, componente **Image**:
   - **Color**: Click sul quadrato colorato
   - Imposta: R=30, G=40, B=42, A=230 (circa #1E282A con alpha 0.9)
5. Nel **RectTransform** di Background:
   - Click destro sull'icona anchor → `Stretch Stretch` (si espande a tutto il parent)

### 3.6 Crea Border Image
1. Con `ToastNotificationItem` selezionato, click destro → `UI` → `Image`
2. Rinomina in `Border`
3. Seleziona `Border`
4. Nel **Inspector**, componente **Image**:
   - **Source Image**: Assegna lo sprite `Border_2px_Solid` (se creato)
   - **Color**: Bianco (verrà colorato dinamicamente)
5. Nel **RectTransform**:
   - **Anchor Presets**: `Stretch Stretch` (si espande a tutto il parent)
   - **Left, Top, Right, Bottom**: Tutti a 0

### 3.7 Crea i 4 Corner Images
1. Per ogni angolo (TL, TR, BL, BR):
   - Click destro su `ToastNotificationItem` → `UI` → `Image`
   - Rinomina in `Corner_TL` (Top-Left), `Corner_TR` (Top-Right), `Corner_BL` (Bottom-Left), `Corner_BR` (Bottom-Right)
   - Seleziona il corner
   - Nel **Inspector**:
     - **Source Image**: Assegna lo sprite `Corner_LShape_3x3`
     - **Color**: Bianco
   - Nel **RectTransform**:
     - **Anchor Presets**: Per TL usa `top-left`, per TR `top-right`, etc.
     - **Width**: 3
     - **Height**: 3
     - Posiziona manualmente agli angoli

### 3.8 Crea Content Container
1. Click destro su `ToastNotificationItem` → `UI` → `Panel` (o `Create Empty` e aggiungi `Image`)
2. Rinomina in `Content`
3. Seleziona `Content`
4. Aggiungi componente `Horizontal Layout Group`:
   - **Inspector** → `Add Component` → Cerca `Horizontal Layout Group`
5. Configura Horizontal Layout Group:
   - **Spacing**: 8
   - **Child Alignment**: `Upper Left`
   - **Child Control Width**: Spunta
   - **Child Control Height**: Spunta
   - **Child Force Expand Width**: Non spuntato
   - **Child Force Expand Height**: Non spuntato

### 3.9 Crea IconBox dentro Content
1. Click destro su `Content` → `UI` → `Image`
2. Rinomina in `IconBox`
3. Seleziona `IconBox`
4. Nel **RectTransform**:
   - **Width**: 16
   - **Height**: 16
5. Crea child di IconBox:
   - Click destro su `IconBox` → `UI` → `Image`
   - Rinomina in `SeverityIcon`
   - Questo mostrerà l'icona Info/Warning/Danger

### 3.10 Crea TextContainer dentro Content
1. Click destro su `Content` → `UI` → `Panel` (o `Create Empty`)
2. Rinomina in `TextContainer`
3. Seleziona `TextContainer`
4. Aggiungi componente `Vertical Layout Group`:
   - **Spacing**: 2
   - **Child Alignment**: `Upper Left`
   - **Child Control Width**: Spunta
   - **Child Control Height**: Non spuntato
   - **Child Force Expand Width**: Spunta
   - **Child Force Expand Height**: Non spuntato

### 3.11 Crea CodeText dentro TextContainer
1. Click destro su `TextContainer` → `UI` → `Text - TextMeshPro`
   - **Nota**: Se ti chiede di importare TMP Essentials, click `Import TMP Essentials`
2. Rinomina in `CodeText`
3. Seleziona `CodeText`
4. Nel **Inspector**, componente **TextMeshProUGUI**:
   - **Text**: "TEST-001" (temporaneo)
   - **Font Size**: 10
   - **Color**: R=139, G=152, B=148 (grigio scuro #8B9894)
   - **Alignment**: Left, Top

### 3.12 Crea MessageText dentro TextContainer
1. Click destro su `TextContainer` → `UI` → `Text - TextMeshPro`
2. Rinomina in `MessageText`
3. Seleziona `MessageText`
4. Nel **Inspector**:
   - **Text**: "Test message" (temporaneo)
   - **Font Size**: 10
   - **Color**: Bianco
   - **Alignment**: Left, Top

### 3.13 Crea ExpandedContent dentro TextContainer
1. Click destro su `TextContainer` → `Create Empty`
2. Rinomina in `ExpandedContent`
3. Seleziona `ExpandedContent`
4. Nel **Inspector**, componente **GameObject**:
   - **Active**: **Deseleziona la checkbox** (inizialmente disattivo)
5. Dentro ExpandedContent, crea TimestampText:
   - Click destro su `ExpandedContent` → `UI` → `Text - TextMeshPro`
   - Rinomina in `TimestampText`
   - **Text**: "12:34:56" (temporaneo)
   - **Font Size**: 10
   - **Color**: R=192, G=200, B=197 (grigio chiaro #C0C8C5)

### 3.14 Aggiungi CanvasGroup
1. Seleziona `ToastNotificationItem` (root)
2. Nel **Inspector**, `Add Component` → Cerca `Canvas Group`
3. Configura:
   - **Alpha**: 1
   - **Interactable**: Spunta
   - **Blocks Raycasts**: Spunta

### 3.15 Aggiungi Button per toggle (opzionale)
1. Seleziona `ToastNotificationItem` (root)
2. `Add Component` → `Button`
3. Questo permetterà di clickare sul toast per espanderlo

### 3.16 Collega tutti i riferimenti nello script
1. Seleziona `ToastNotificationItem` (root)
2. Nel **Inspector**, trova il componente `Toast Notification UI Item`
3. Trascina e rilascia i GameObject dalla Hierarchy ai campi:
   - **Rect Transform**: Trascina `ToastNotificationItem` stesso
   - **Canvas Group**: Trascina `ToastNotificationItem` stesso
   - **Background Image**: Trascina `Background`
   - **Border Image**: Trascina `Border`
   - **Corner Images**: Trascina tutti e 4 i corner (TL, TR, BL, BR) nell'array
   - **Severity Icon**: Trascina `Content/IconBox/SeverityIcon`
   - **Code Text**: Trascina `Content/TextContainer/CodeText`
   - **Message Text**: Trascina `Content/TextContainer/MessageText`
   - **Timestamp Text**: Trascina `Content/TextContainer/ExpandedContent/TimestampText`
   - **Expanded Content**: Trascina `Content/TextContainer/ExpandedContent`
   - **Expanded Layout**: Trascina `Content/TextContainer` (che ha VerticalLayoutGroup)
   - **Expand Button**: Trascina `ToastNotificationItem` stesso (se hai aggiunto Button)

### 3.17 Salva come Prefab
1. Seleziona `ToastNotificationItem` nella Hierarchy
2. Trascina dalla Hierarchy al **Project window** in `Assets/_Project/Prefabs/UI/`
3. Unity creerà il prefab. Rinominalo se necessario
4. **IMPORTANTE**: Dopo aver creato il prefab, puoi eliminare il GameObject dalla scena (non serve più nella scena, solo il prefab)

---

## Passo 4: Creare Prefab ToastNotificationHeader

### 4.1 Crea il GameObject base
1. Nella **Hierarchy**, click destro → `Create Empty`
2. Rinomina in `ToastNotificationHeader`
3. Aggiungi `Rect Transform` (come nel Passo 3.3)

### 4.2 Aggiungi il componente script
1. `Add Component` → `Toast Notification Header`

### 4.3 Crea struttura UI (simile a Passo 3)
1. Crea `Background` (Image, colore #1E282A alpha 0.9)
2. Crea `Border` (Image con BorderSprite)
3. Crea 4 `Corner` (TL, TR, BL, BR con CornerSprite)
4. Crea `AlertIcon` (Image con AlertCircleIcon)
5. Crea `HeaderText` (TextMeshPro, testo "NOTIFICATIONS", uppercase, monospaced)
6. Crea `BadgeContainer` (GameObject vuoto)
   - Dentro: `BadgeText` (TextMeshPro, testo "0")
7. Crea `ChevronIcon` (Image con ChevronIcon)
   - Dentro: `ChevronTransform` (RectTransform per rotazione)
8. Aggiungi `Button` al root (covers entire header)

### 4.4 Collega riferimenti
1. Nel componente `Toast Notification Header`, trascina tutti i riferimenti:
   - **Toggle Button**: Trascina `ToastNotificationHeader` stesso
   - **Header Background**: Trascina `Background`
   - **Border Image**: Trascina `Border`
   - **Corner Images**: Trascina i 4 corner
   - **Alert Icon**: Trascina `AlertIcon`
   - **Header Text**: Trascina `HeaderText`
   - **Badge Container**: Trascina `BadgeContainer`
   - **Badge Text**: Trascina `BadgeContainer/BadgeText`
   - **Chevron Icon**: Trascina `ChevronIcon`
   - **Chevron Transform**: Trascina `ChevronIcon`

### 4.5 Salva come Prefab
1. Trascina `ToastNotificationHeader` dalla Hierarchy a `Assets/_Project/Prefabs/UI/`
2. Elimina il GameObject dalla scena

---

## Passo 5: Setup Scena - Creare ToastNotificationSystem

### 5.1 Trova il Canvas principale
1. Nella **Hierarchy window** (sinistra), cerca il GameObject chiamato `Canvas`
   - **IMPORTANTE**: Ci sono DUE Canvas nella scena:
     - **Canvas principale** (linea 29 in SceneHierarchy.txt): Ha `EventSystem` come child diretto (linea 34)
     - **Canvas dentro PLY_Player** (linea 20): Questo NON è quello che serve
2. Per identificare il Canvas corretto:
   - Espandi `Canvas` nella Hierarchy
   - Se vedi `EventSystem` come primo child diretto, questo è il Canvas corretto
   - Se vedi `Text (TMP)` come child, questo è il Canvas dentro PLY_Player (sbagliato)
3. Seleziona il **Canvas principale** (quello con EventSystem come child)

### 5.2 Crea ToastNotificationSystem
1. Con il **Canvas principale** selezionato, click destro su `Canvas` → `Create Empty`
2. Rinomina in `ToastNotificationSystem`
3. **IMPORTANTE - Posizionamento corretto**:
   - `ToastNotificationSystem` deve essere un **sibling diretto di `HUD`** (stesso parent `Canvas`)
   - `HUD` è già presente nella scena (linea 544 in SceneHierarchy.txt)
   - La struttura corretta dovrebbe essere:
     ```
     Canvas
     ├── EventSystem
     ├── BTN_EndDay
     ├── UISeedSelector
     ├── UI_Inventory
     ├── HUD (esistente - linea 544)
     │   ├── Condensation
     │   ├── Missions
     │   ├── UI_Resources
     │   ├── UI_Notification (esistente - linea 613)
     │   └── WikipediaButton
     ├── UI_PotDetails
     ├── ... (altri UI)
     └── ToastNotificationSystem (NUOVO - deve essere qui, sibling di HUD)
     ```
4. **Verifica la posizione**:
   - Nella Hierarchy, `ToastNotificationSystem` deve essere allo stesso livello indentato di `HUD`
   - Se per sbaglio l'hai creato dentro `HUD`, trascinalo fuori:
     - Click e tieni premuto su `ToastNotificationSystem` nella Hierarchy
     - Trascinalo fuori da `HUD` e rilascialo direttamente sotto `Canvas`
     - Verifica che sia allo stesso livello di `HUD` (stessa indentazione)

### 5.3 Aggiungi RectTransform a ToastNotificationSystem
1. Seleziona `ToastNotificationSystem`
2. Se non ha già RectTransform, aggiungilo:
   - `Add Component` → `Rect Transform`
3. Configura RectTransform:
   - **Anchor Presets**: Click sull'icona anchor → Tieni `Shift + Alt` → Click `top-right`
   - **Pos X**: -24
   - **Pos Y**: -96
   - **Width**: 306
   - **Height**: 0 (sarà auto)

### 5.4 Aggiungi componenti a ToastNotificationSystem
1. Con `ToastNotificationSystem` selezionato, `Add Component` → `Toast Notification Manager`
2. `Add Component` → `Toast Notification Pool`

### 5.5 Crea Header
1. Click destro su `ToastNotificationSystem` → `Create Empty`
2. Rinomina in `Header`
3. Seleziona `Header`
4. Nel **Inspector**, click `Add Component` → `Rect Transform`
5. **OPZIONE A - Usa Prefab**:
   - Trascina il prefab `ToastNotificationHeader` dalla Project window alla Hierarchy, dentro `Header`
   - Oppure: Click destro su `Header` → `UI` → `Image` (per creare manualmente)
6. **OPZIONE B - Crea Manualmente**:
   - Crea la struttura UI come descritto nel Passo 4

### 5.6 Crea ToastContainer
1. Click destro su `ToastNotificationSystem` → `Create Empty`
2. Rinomina in `ToastContainer`
3. Seleziona `ToastContainer`
4. Aggiungi `Rect Transform`
5. Aggiungi `Vertical Layout Group`:
   - `Add Component` → `Vertical Layout Group`
   - Configura:
     - **Spacing**: 4
     - **Child Alignment**: `Upper Right`
     - **Child Control Width**: Spunta
     - **Child Control Height**: Non spuntato
     - **Child Force Expand Width**: Spunta
     - **Child Force Expand Height**: Non spuntato

### 5.7 Crea PoolParent (opzionale)
1. Click destro su `ToastNotificationSystem` → `Create Empty`
2. Rinomina in `PoolParent`
3. Questo è solo per organizzazione, i toast del pool saranno nascosti qui

### 5.8 Configura ToastNotificationPool
1. Seleziona `ToastNotificationSystem`
2. Nel **Inspector**, trova il componente `Toast Notification Pool`
3. Trascina i riferimenti:
   - **Prefab**: Trascina il prefab `ToastNotificationItem` da `Assets/_Project/Prefabs/UI/`
   - **Pool Parent**: Trascina `PoolParent` (o lascia vuoto per usare ToastNotificationSystem stesso)
   - **Initial Pool Size**: 10

### 5.9 Configura ToastNotificationManager
1. Con `ToastNotificationSystem` selezionato, trova il componente `Toast Notification Manager`
2. Trascina i riferimenti:
   - **Pool**: Trascina il componente `Toast Notification Pool` (dallo stesso GameObject, o seleziona `ToastNotificationSystem`)
   - **Toast Container**: Trascina `ToastContainer` (il RectTransform)
   - **Header**: Trascina il componente `Toast Notification Header` (da `Header` o dal prefab istanziato)
   - **Root Rect Transform**: Trascina `ToastNotificationSystem` stesso (il RectTransform)

### 5.10 Verifica struttura gerarchia
La struttura finale dovrebbe corrispondere a SceneHierarchy.txt:

**Sotto Canvas (linea 29)**:
```
Canvas
├── EventSystem (linea 34)
├── BTN_EndDay (linea 38)
├── UISeedSelector (linea 44)
├── UI_Inventory (linea 71)
├── HUD (linea 544 - esistente)
│   ├── Condensation (linea 546)
│   ├── Missions (linea 568)
│   ├── UI_Resources (linea 597)
│   ├── UI_Notification (linea 613 - esistente - UINotification)
│   └── WikipediaButton (linea 635)
├── UI_PotDetails (linea 645)
├── UI_ElevatorPanel (linea 852)
├── UI_LabMinigame (linea 895)
├── ... (altri UI esistenti)
└── ToastNotificationSystem (NUOVO - sibling di HUD)
    ├── Header
    │   └── (prefab ToastNotificationHeader o struttura manuale)
    ├── ToastContainer
    └── PoolParent
```

**Root Level (stesso livello di GameManager, linea 2725)**:
```
(root level - nessun parent)
├── GameManager (linea 2725)
├── Virtual Camera (linea 2729)
├── AlwaysVisiblePotHUD (linea 2738)
├── PH DEBUG (linea 2741)
├── AUTO SCRIPTS RUNTIME (linea 2744)
├── PotDebugConsole (linea 2748)
├── GAMEPLAY_Balancing_Console (linea 2751)
├── GlobalStateInspector (linea 2754)
└── ToastNotificationDebugConsole (NUOVO - root-level)
```

**Come verificare**:
1. Nella Hierarchy, espandi `Canvas`
2. Verifica che `ToastNotificationSystem` sia direttamente sotto `Canvas` (stesso livello di `HUD`)
3. Scrolla fino in fondo nella Hierarchy
4. Verifica che `ToastNotificationDebugConsole` sia root-level (stesso livello di `GameManager`)

---

## Passo 6: Aggiungere Console Debug (Opzionale)

### 6.1 Crea ToastNotificationDebugConsole
1. Nella **Hierarchy window**, scrolla fino in fondo (o cerca con Ctrl+F)
2. Trova i GameObject root-level esistenti:
   - `GameManager` (linea 2725 in SceneHierarchy.txt)
   - `PotDebugConsole` (linea 2748)
   - `GlobalStateInspector` (linea 2754)
3. **IMPORTANTE**: `ToastNotificationDebugConsole` deve essere allo **stesso livello** di questi GameObject
   - Non deve essere dentro `Canvas` o altri container
   - Deve essere root-level (nessun parent)
4. Click destro su uno spazio vuoto nella Hierarchy (non dentro nessun GameObject)
5. `Create Empty`
6. Rinomina in `ToastNotificationDebugConsole`
7. **Verifica la posizione**:
   - Nella Hierarchy, `ToastNotificationDebugConsole` deve essere allo stesso livello indentato di `GameManager`
   - La struttura corretta dovrebbe essere:
     ```
     (root level - nessun parent)
     ├── GameManager (linea 2725)
     ├── Virtual Camera
     ├── AlwaysVisiblePotHUD
     ├── PH DEBUG
     ├── AUTO SCRIPTS RUNTIME
     ├── PotDebugConsole (linea 2748)
     ├── GAMEPLAY_Balancing_Console
     ├── GlobalStateInspector (linea 2754)
     └── ToastNotificationDebugConsole (NUOVO - deve essere qui)
     ```

### 6.2 Aggiungi componente
1. Seleziona `ToastNotificationDebugConsole`
2. `Add Component` → `Toast Notification Debug Console`

### 6.3 Configura
1. Nel **Inspector**, componente `Toast Notification Debug Console`:
   - **Enable Debug Console**: Spunta la checkbox
   - **Toggle Key**: Seleziona `F9` dal dropdown
   - **Show On Start**: Lascia deselezionato

---

## Passo 7: Verifica Setup

### 7.1 Controlla che tutto sia collegato
1. Seleziona `ToastNotificationSystem`
2. Nel **Inspector**, verifica che tutti i campi del `Toast Notification Manager` siano assegnati:
   - Pool: ✓
   - Toast Container: ✓
   - Header: ✓
   - Root Rect Transform: ✓
3. Verifica che `Toast Notification Pool` abbia:
   - Prefab: ✓

### 7.2 Controlla che l'asset config esista
1. Nel **Project window**, naviga a `Assets/Resources/Configs/`
2. Verifica che esista `ToastNotificationConfig.asset`
3. Selezionalo e verifica che sia configurato

### 7.3 Test in Play Mode
1. Click sul pulsante **Play** in alto (triangolo)
2. Durante il gioco, premi **F9** per aprire la console debug
3. Nella console, prova a triggerare un toast:
   - Seleziona un tipo dal dropdown
   - Inserisci un messaggio
   - Click "Show Toast"
4. Verifica che il toast appaia in alto a destra

### 7.4 Test toast in-game
1. Esegui azioni che dovrebbero triggerare toast:
   - Azioni vaso (water, light, etc.)
   - Day cycle events
   - Inventory events
2. Verifica che i toast appaiano correttamente

---

## Troubleshooting Dettagliato

### "ToastNotificationConfig non trovato"
**Sintomo**: Console mostra errore "ToastNotificationConfig non trovato"

**Soluzione passo-passo**:
1. Apri **Console window** (`Window` → `General` → `Console`)
2. Verifica l'errore esatto
3. Nel **Project window**, naviga a `Assets/Resources/Configs/`
4. Verifica che esista `ToastNotificationConfig.asset`
5. Se non esiste, crealo seguendo il Passo 1
6. Se esiste ma ha nome diverso, rinominalo esattamente in `ToastNotificationConfig`
7. Verifica che sia in `Resources/Configs/` (non in altre cartelle)
8. **IMPORTANTE**: Il nome deve essere esatto, case-sensitive

### "Prefab non assegnato"
**Sintomo**: Console mostra errore "Prefab non assegnato" o "ToastNotificationPool: Prefab non assegnato!"

**Soluzione passo-passo**:
1. Seleziona `ToastNotificationSystem` nella Hierarchy
2. Nel **Inspector**, trova `Toast Notification Pool`
3. Verifica che il campo **Prefab** non sia vuoto
4. Se è vuoto:
   - Nel **Project window**, naviga a `Assets/_Project/Prefabs/UI/`
   - Verifica che esista `ToastNotificationItem.prefab`
   - Se non esiste, crealo seguendo il Passo 3
   - Trascina il prefab dal Project window al campo **Prefab** nell'Inspector

### "Toast non appaiono"
**Sintomo**: I toast non vengono visualizzati quando dovrebbero

**Soluzione passo-passo**:
1. Verifica che `ToastNotificationSystem` sia sotto il **Canvas principale** (quello con EventSystem come child, linea 29)
   - **NON** deve essere dentro `HUD` o altri container
   - Deve essere sibling diretto di `HUD` (stesso parent `Canvas`)
2. Per verificare la posizione corretta:
   - Nella Hierarchy, espandi `Canvas`
   - `ToastNotificationSystem` deve essere allo stesso livello indentato di `HUD`
   - Se è dentro `HUD`, trascinalo fuori
3. Seleziona `ToastNotificationSystem`
3. Nel **Inspector**, verifica il **RectTransform**:
   - **Anchor**: Deve essere top-right (1, 1)
   - **Pivot**: Deve essere (1, 1)
   - **Pos X**: -24
   - **Pos Y**: -96
4. Verifica che `ToastNotificationSystem` sia **attivo** (checkbox in alto a sinistra dell'Inspector)
5. Apri **Console** e verifica che non ci siano errori
6. Verifica che `ToastNotificationManager` sia registrato:
   - Durante Play Mode, apri console debug (F9)
   - Se la console non si apre, il manager potrebbe non essere registrato
7. Verifica che l'asset config sia caricato:
   - Seleziona `ToastNotificationConfig` nel Project
   - Verifica che tutti i campi siano configurati

### "Colore header non cambia"
**Sintomo**: L'header rimane sempre dello stesso colore

**Soluzione passo-passo**:
1. Seleziona `ToastNotificationSystem`
2. Nel **Inspector**, verifica che `Toast Notification Manager` → **Header** sia assegnato
3. Se è vuoto:
   - Trascina il componente `Toast Notification Header` da `Header` (o dal prefab)
4. Verifica che i toast abbiano severità corretta:
   - Apri console debug (F9)
   - Triggera toast di tipo diverso (Success, Error, Warning)
   - Verifica che l'header cambi colore
5. Verifica che `UpdateHeaderColor()` venga chiamato:
   - Aggiungi un breakpoint o log per debug (opzionale)

### "Console debug non si apre (F9)"
**Sintomo**: Premendo F9 non succede nulla

**Soluzione passo-passo**:
1. Verifica che `ToastNotificationDebugConsole` esista nella Hierarchy come **root-level**
   - **NON** deve essere dentro `Canvas` o altri container
   - Deve essere allo stesso livello di `GameManager` (linea 2725), `PotDebugConsole` (linea 2748), `GlobalStateInspector` (linea 2754)
2. Per verificare la posizione:
   - Nella Hierarchy, scrolla fino in fondo
   - `ToastNotificationDebugConsole` deve essere allo stesso livello indentato di `GameManager`
   - Se è dentro `Canvas` o altri container, trascinalo fuori (root-level, nessun parent)
3. Seleziona `ToastNotificationDebugConsole`
3. Nel **Inspector**, verifica:
   - **Enable Debug Console**: Deve essere spuntato
   - **Toggle Key**: Deve essere `F9`
4. Verifica che il GameObject sia **attivo** (checkbox in alto)
5. **Solo Editor/Development build**: La console è disabilitata in Release build
   - Verifica le impostazioni di build: `File` → `Build Settings` → `Development Build` deve essere spuntato
6. Prova a cambiare il tasto toggle temporaneamente per testare

### "Errore: Cannot convert Font to TMP_FontAsset"
**Sintomo**: Errori di compilazione relativi a Font

**Soluzione passo-passo**:
1. Seleziona `ToastNotificationConfig` nel Project
2. Nel **Inspector**, trova il campo **Monospaced Font**
3. **IMPORTANTE**: Questo campo richiede un `TMP_FontAsset`, non un `Font` normale
4. Per creare TMP_FontAsset:
   - Seleziona un font nel Project (es. Courier New, Consolas)
   - Menu: `Window` → `TextMeshPro` → `Font Asset Creator`
   - Click `Generate Font Atlas`
   - Click `Save` e salva in `Assets/_Project/Fonts/`
   - Assegna questo TMP_FontAsset al campo Monospaced Font

---

## Checklist Finale

Prima di considerare il setup completato, verifica:

- [ ] `ToastNotificationConfig.asset` esiste in `Assets/Resources/Configs/`
- [ ] `ToastNotificationItem.prefab` esiste in `Assets/_Project/Prefabs/UI/`
- [ ] `ToastNotificationHeader.prefab` esiste in `Assets/_Project/Prefabs/UI/`
- [ ] `ToastNotificationSystem` esiste nella scena sotto `Canvas` principale (quello con EventSystem, linea 29)
- [ ] `ToastNotificationSystem` è sibling diretto di `HUD` (stesso livello, NON dentro HUD)
- [ ] Verifica nella Hierarchy: `ToastNotificationSystem` ha la stessa indentazione di `HUD` sotto `Canvas`
- [ ] Tutti i riferimenti in `ToastNotificationManager` sono assegnati
- [ ] `ToastNotificationPool` ha il prefab assegnato
- [ ] `ToastNotificationDebugConsole` esiste come root-level (stesso livello di GameManager, linea 2725) - opzionale
- [ ] Verifica nella Hierarchy: `ToastNotificationDebugConsole` ha la stessa indentazione di `GameManager` (root-level, nessun parent)
- [ ] Test in Play Mode: Console debug (F9) si apre
- [ ] Test in Play Mode: Toast appaiono quando triggerati
- [ ] Test in Play Mode: Toast si auto-rimuovono dopo 8 secondi
- [ ] Test in Play Mode: Max 3 toast visibili contemporaneamente
- [ ] Test in Play Mode: Header cambia colore in base a severità

---

**Nota**: Queste istruzioni richiedono Unity Editor per creare asset e prefab. Il codice è già implementato e pronto all'uso. Se incontri problemi, verifica sempre la Console di Unity per messaggi di errore dettagliati.
