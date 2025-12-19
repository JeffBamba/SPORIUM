# Guida Setup HUD Notifications 2.0

Guida passo-passo per configurare il nuovo sistema HUD Notifications 2.0 in Unity.

---

## Prerequisiti

- Unity Editor aperto con la scena principale
- Tutti gli script del sistema 2.0 compilati senza errori
- DOTween importato e configurato

---

## Passo 1: Creare ScriptableObject Config

### 1.1 Creare l'Asset Config

1. Nella **Project** window, naviga a `Assets/Resources/`
2. Se la cartella `Configs` non esiste, creala:
   - Click destro su `Resources` → `Create` → `Folder`
   - Rinomina in `Configs`
3. Click destro su `Configs` → `Create` → `Spore` → `HUD Notification Config 2.0`
4. Rinomina l'asset in `HUDNotificationConfig2.0`

### 1.2 Configurare i Valori Default

1. Seleziona `HUDNotificationConfig2.0` nella Project window
2. Nel **Inspector**, configura tutti i valori:

**Container Settings:**
- Container Width: `306`
- Container Top Offset: `96`
- Container Right Offset: `24`

**Header Settings:**
- Header Padding: `8`
- Header Border Width: `2`
- Header Margin Bottom: `6`
- Header Font Size: `10`
- Header Icon Size: `14`
- Header Chevron Size: `16`
- Header Badge Padding: `(6, 2)`
- Header Badge Font Size: `10`

**Toast Settings:**
- Toast Padding: `8`
- Toast Border Width: `2`
- Toast Gap: `6`
- Toast Icon Size: `14`
- Toast Code Font Size: `10`
- Toast Message Font Size: `11`

**Item Notification Settings:**
- Item Icon Size: `40`
- Item Icon Gap: `8`
- Item Header Font Size: `10`
- Item Name Font Size: `11`
- Item Location Font Size: `9`
- Item Package Icon Size: `20`

**Background & Effects:**
- Background Color: `#1E282A` con alpha `0.9` (90% opacità)
- Background Hover Color: `#1E282A` con alpha `0.95` (95% opacità)
- Enable Backdrop Blur: `true` (opzionale)

**Header Colors:**
- Color Idle: `#5DB6E3` (Blu)
- Color Danger: `#D35F5F` (Rosso)
- Color Warning: `#E6C96F` (Giallo)
- Color Info: `#7FFF7A` (Verde)

**Timing Settings:**
- Auto Dismiss Duration: `8`
- Overflow Dismiss Duration: `5`
- Max Visible Notifications: `3`

**Fonts & Sprites:**
- Monospaced Font: Assegna un font monospaced (es. Courier New, Consolas)
- Info Icon: Sprite icona "i" in cerchio (usata per notifiche Info/System)
- Chevron Icon: Sprite freccia/chevron
- Warning Icon: Sprite triangolo esclamazione (usata per notifiche Warning)
- Danger Icon: Sprite cerchio esclamazione (usata per notifiche Error/Critical)
- Success Icon: Sprite check o cerchio "i" (usata per notifiche Success)
- Border Sprite: Sprite bordo 2px
- Corner Sprite: Sprite corner decorativo (opzionale)

**Nota**: Il sistema seleziona automaticamente l'icona corretta in base al tipo di notifica. Assicurati di assegnare tutte le icone nella config per il corretto funzionamento.

**Animation Settings:**
- Enter Animation Duration: `0.3`
- Exit Animation Duration: `0.3`
- Chevron Rotation Duration: `0.2`

---

## Passo 2: Creare Prefab Header

### 2.1 Creare GameObject Header

1. Nella **Project** window, naviga a `Assets/_Project/Prefabs/UI/`
2. Se la cartella non esiste, creala
3. Click destro su `UI` → `Create` → `UI` → `Panel`
4. Rinomina in `HUDNotificationHeader2.0`

### 2.2 Struttura UI Header

1. Seleziona `HUDNotificationHeader2.0` nel prefab
2. Aggiungi componente `HUD Notification Header 2.0` (script)
3. Crea questa struttura come children:

```
HUDNotificationHeader2.0 (Panel)
├── Background (Image) - Colore #1E282A/90
├── Border (Image) - Bordo 2px, colore dinamico
├── Content (HorizontalLayoutGroup)
│   ├── InfoIcon (Image) - Icona "i" 14x14
│   ├── HeaderText (TextMeshProUGUI) - "NOTIFICATIONS" 10px
│   ├── BadgeContainer (GameObject)
│   │   └── BadgeText (TextMeshProUGUI) - Numero notifiche
│   └── ChevronIcon (Image) - Chevron 16x16
└── ToggleButton (Button) - Covers entire header
```

### 2.3 Configurare Componenti Header

**Background Image:**
- Color: `#1E282A` con alpha `0.9`
- Raycast Target: `true` (per hover)

**Border Image:**
- Color: `#5DB6E3` (colore idle, sarà dinamico)
- Sprite: Bordo 2px
- Filter Mode: `Point` (pixel-perfect)

**InfoIcon Image:**
- Sprite: Icona "i" in cerchio
- Color: `#5DB6E3`
- RectTransform Size: `(14, 14)`

**HeaderText TextMeshProUGUI:**
- Text: `NOTIFICATIONS`
- Font: Monospaced (dalla config)
- Font Size: `10`
- Color: `#5DB6E3`
- Alignment: Center

**BadgeContainer:**
- Background Image (opzionale)
- BadgeText TextMeshProUGUI:
  - Font: Monospaced
  - Font Size: `10`
  - Alignment: Center

**ChevronIcon Image:**
- Sprite: Chevron/Arrow
- RectTransform Size: `(16, 16)`
- Rotation iniziale: `180°` (chiuso)

**ToggleButton:**
- Component: Button
- Transition: None (o Color Tint)
- Covers entire header (stretch)

### 2.4 Collegare Riferimenti nello Script

Nel componente `HUD Notification Header 2.0`:
- Toggle Button: Trascina `ToggleButton`
- Header Background: Trascina `Background`
- Border Image: Trascina `Border`
- Info Icon: Trascina `InfoIcon`
- Header Text: Trascina `HeaderText`
- Badge Container: Trascina `BadgeContainer`
- Badge Text: Trascina `BadgeText`
- Chevron Icon: Trascina `ChevronIcon`
- Chevron Transform: Trascina `ChevronIcon` (RectTransform)

### 2.5 Salvare Prefab

1. Click su `Prefab` → `Save` (o Ctrl+S)
2. Verifica che il prefab sia salvato correttamente

---

## Passo 3: Creare Prefab Notification Item

### 3.1 Creare GameObject Item

1. Nella **Project** window, `Assets/_Project/Prefabs/UI/`
2. Click destro → `Create` → `UI` → `Panel`
3. Rinomina in `HUDNotificationItem2.0`

### 3.2 Struttura UI Item

1. Seleziona `HUDNotificationItem2.0`
2. Aggiungi componente `HUD Notification Item 2.0` (script)
3. Aggiungi `Canvas Group` (per animazioni fade)
4. Crea questa struttura:

```
HUDNotificationItem2.0 (Panel)
├── Background (Image) - #1E282A/90
├── Border (Image) - Bordo 2px, colore dinamico
├── StandardLayoutContainer (GameObject)
│   ├── IconBox (Image) - Icona severità 14x14
│   └── TextContainer (VerticalLayoutGroup)
│       ├── CodeText (TextMeshProUGUI) - Codice 10px
│       └── MessageText (TextMeshProUGUI) - Messaggio 11px
└── ItemLayoutContainer (GameObject) - Inizialmente disattivo
    ├── ItemIconLarge (Image) - Icona item 40x40
    ├── ItemHeaderText (TextMeshProUGUI) - "ADDED TO INVENTORY" 10px
    ├── ItemNameText (TextMeshProUGUI) - "+X ItemName" 11px
    └── ItemLocationText (TextMeshProUGUI) - "📍 Location" 9px
```

### 3.3 Configurare Componenti Item

**Background Image:**
- Color: `#1E282A` con alpha `0.9`

**Border Image:**
- Color: Dinamico (verrà impostato dallo script)
- Sprite: Bordo 2px
- Filter Mode: `Point`

**StandardLayoutContainer:**
- HorizontalLayoutGroup
- Spacing: `8` (gap tra icona e testo)
- Child Alignment: Middle Left

**IconBox Image:**
- RectTransform Size: `(14, 14)`
- Filter Mode: `Point`

**TextContainer VerticalLayoutGroup:**
- Spacing: `2`
- Child Alignment: Upper Left

**CodeText TextMeshProUGUI:**
- Font: Monospaced
- Font Size: `10` (ToastCodeFontSize)
- Color: **Dinamico** - Versione più chiara/saturata del colore principale della notifica
- **Funzione**: Mostra il codice come titolo (prima riga)
- **Esempio**: "PH-003", "SYS-100", "WRN-042"

**MessageText TextMeshProUGUI:**
- Font: Monospaced
- Font Size: `11` (ToastMessageFontSize)
- Color: **Dinamico** - Versione più scura/muted del colore principale della notifica
- **Funzione**: Mostra la descrizione come testo sotto (seconda riga)
- **Esempio**: "Dome pH unstable", "Condensation system optimal"

**ItemLayoutContainer:**
- Inizialmente **disattivo** (Active: false)
- HorizontalLayoutGroup
- Spacing: `8`

**ItemIconLarge Image:**
- RectTransform Size: `(40, 40)`
- Filter Mode: `Point`

**ItemHeaderText TextMeshProUGUI:**
- Font: Monospaced
- Font Size: `10`
- Text: `ADDED TO INVENTORY`

**ItemNameText TextMeshProUGUI:**
- Font: Monospaced
- Font Size: `11`
- Color: White

**ItemLocationText TextMeshProUGUI:**
- Font: Monospaced
- Font Size: `9`
- Color: `#8B9894`

### 3.4 Collegare Riferimenti nello Script

Nel componente `HUD Notification Item 2.0`:
- Rect Transform: Trascina il RectTransform del root
- Canvas Group: Trascina il componente CanvasGroup
- Background Image: Trascina `Background`
- Border Image: Trascina `Border`
- Severity Icon: Trascina `IconBox`
- Standard Layout Container: Trascina `StandardLayoutContainer`
- Code Text: Trascina `CodeText`
- Message Text: Trascina `MessageText`
- Item Layout Container: Trascina `ItemLayoutContainer`
- Item Icon Large: Trascina `ItemIconLarge`
- Item Header Text: Trascina `ItemHeaderText`
- Item Name Text: Trascina `ItemNameText`
- Item Location Text: Trascina `ItemLocationText`

### 3.5 Salvare Prefab

1. Click su `Prefab` → `Save`
2. Verifica che il prefab sia salvato

---

## Passo 4: Setup Scena

### 4.1 Trovare Canvas Principale

1. Nella **Hierarchy**, trova il `Canvas` principale
2. Verifica che abbia `EventSystem` come child (indica che è il canvas principale)

### 4.2 Creare HUDNotificationSystem2.0

1. Con `Canvas` selezionato, click destro → `Create Empty`
2. Rinomina in `HUDNotificationSystem2.0`
3. **IMPORTANTE**: Deve essere un **sibling di `HUD`** (stesso livello gerarchico)

**Struttura corretta (basata su SceneHierarchy.txt):**
```
Canvas (linea 29)
├── EventSystem (linea 34)
├── BTN_EndDay (linea 38)
├── UISeedSelector (linea 44)
├── UI_Inventory (linea 71)
├── HUD (linea 544 - esistente)
│   ├── Condensation (linea 546)
│   ├── Missions (linea 568)
│   ├── UI_Resources (linea 597)
│   ├── UI_Notification (linea 613 - esistente)
│   └── WikipediaButton (linea 635)
├── UI_PotDetails (linea 645)
├── ... (altri UI esistenti)
└── HUDNotificationSystem2.0 (NUOVO - sibling di HUD, stesso livello di ToastNotificationSystem linea 2287)
    ├── Header
    │   └── HUDNotificationHeader2.0 (prefab istanziato)
    └── NotificationContainer
```

**Nota**: `HUDNotificationSystem2.0` deve essere allo stesso livello gerarchico di `ToastNotificationSystem` (sistema vecchio), entrambi come siblings di `HUD` sotto `Canvas`.

### 4.3 Configurare RectTransform Root

1. Seleziona `HUDNotificationSystem2.0`
2. Nel **Inspector**, componente **Rect Transform**:
   - **Anchor Presets**: Click icona anchor → Tieni `Shift + Alt` → Click `top-right`
   - **Pos X**: `-24` (oppure usa `-ContainerRightOffset` dalla config)
   - **Pos Y**: `-96` (oppure usa `-ContainerTopOffset` dalla config)
   - **Width**: `306` (oppure usa `ContainerWidth` dalla config)
   - **Height**: `0` (auto)

### 4.4 Aggiungere Componenti al Root

1. Con `HUDNotificationSystem2.0` selezionato, `Add Component`:
   - `HUD Notification Feed Manager 2.0`
   - `HUD Notification Pool 2.0`

### 4.5 Creare Header

1. Click destro su `HUDNotificationSystem2.0` → `Create Empty`
2. Rinomina in `Header`
3. Trascina il prefab `HUDNotificationHeader2.0` dentro `Header`
   - Oppure istanzia il prefab come child di `Header`

### 4.6 Creare Notification Container

1. Click destro su `HUDNotificationSystem2.0` → `Create Empty`
2. Rinomina in `NotificationContainer`
3. Aggiungi componenti:
   - `Vertical Layout Group`
   - `Content Size Fitter`
4. Configura **Vertical Layout Group**:
   - Spacing: `6` (o `ToastGap` dalla config)
   - Child Alignment: `Upper Right`
   - Child Control Width: `true`
   - Child Control Height: `false`
   - Child Force Expand Width: `true`
   - Child Force Expand Height: `false`
5. Configura **Content Size Fitter**:
   - Horizontal Fit: `Unconstrained`
   - Vertical Fit: `Preferred Size`

### 4.7 Collegare Riferimenti nel Manager

Nel componente `HUD Notification Feed Manager 2.0`:
- **Pool**: Trascina il componente `HUD Notification Pool 2.0` (dallo stesso GameObject)
- **Notification Container**: Trascina `NotificationContainer` (RectTransform)
- **Header**: Trascina il componente `HUD Notification Header 2.0` (da `Header`)
- **Root Rect Transform**: Trascina `HUDNotificationSystem2.0` stesso (RectTransform)

### 4.8 Configurare Pool

Nel componente `HUD Notification Pool 2.0`:
- **Prefab**: Trascina il prefab `HUDNotificationItem2.0` (dalla Project window)
- **Initial Pool Size**: `8` (tra 5-10)

**Nota**: Il pool gestisce internamente i suoi item (non serve un `PoolParent` separato come nel sistema vecchio). Gli item pooled vengono mantenuti come children del GameObject con il componente `HUD Notification Pool 2.0`.

### 4.9 Verifica Struttura Finale

La struttura finale dovrebbe essere:

```
HUDNotificationSystem2.0
├── Header
│   └── HUDNotificationHeader2.0 (prefab con componente HUD Notification Header 2.0)
└── NotificationContainer (con VerticalLayoutGroup e ContentSizeFitter)
```

Il pool gestisce i suoi item internamente (non appare nella gerarchia come GameObject separato).

---

## Passo 5: Nascondere Sistema Vecchio

### 5.1 Aggiungere Script Hide

1. Nella **Hierarchy**, trova `ToastNotificationSystem` (sistema vecchio)
2. Seleziona `ToastNotificationSystem`
3. `Add Component` → `Hide Old Notification System`

### 5.2 Configurare Hide Script

Nel componente `Hide Old Notification System`:
- **Disable GameObject**: `true` (disattiva il GameObject, mantiene funzionante)
  - Oppure `false` per spostarlo fuori schermo
- **Offscreen Position**: `(10000, 10000)` (se disable è false)

### 5.3 Verificare

1. Entra in **Play Mode**
2. Verifica che `ToastNotificationSystem` sia nascosto
3. Verifica che il sistema vecchio non sia visibile ma funzioni ancora

---

## Passo 6: Test in Play Mode

### 6.1 Test Header Toggle

1. Entra in **Play Mode**
2. Click sull'header "NOTIFICATIONS"
3. Verifica che:
   - Il container si espanda/contragga
   - Il chevron ruoti 180°
   - Il badge mostri il numero corretto

### 6.2 Test Notifiche

Apri la console debug (F9) e usa la sezione "Trigger Toast" per testare:
- Notifica Success (verifica codice `OPR-XXX` e icona success/info)
- Notifica Error (verifica codice `ERR-XXX` e icona danger)
- Notifica Warning (verifica codice `WRN-XXX` e icona warning triangolo)
- Notifica Info (verifica codice `SYS-XXX` e icona info cerchio)
- Item Notification (verifica codice `INV-XXX`)

**Verifica layout:**
- Codice come prima riga (più chiaro)
- Descrizione come seconda riga (più scuro)
- Icona visibile a sinistra con colore corretto

### 6.3 Test Colori Dinamici

1. Triggera notifiche di severità diverse
2. Verifica che il colore header cambi:
   - Idle: Blu (#5DB6E3)
   - Con DANGER: Rosso (#D35F5F)
   - Con WARNING: Giallo (#E6C96F)
   - Solo INFO: Verde (#7FFF7A)

### 6.4 Test Timing

1. Triggera più di 3 notifiche
2. Verifica che:
   - Le notifiche più vecchie (non-DANGER) spariscono dopo 5s
   - Le notifiche standard spariscono dopo 8s
   - Massimo 3 notifiche visibili

### 6.5 Test Hover Effect

1. Passa il mouse sull'header
2. Verifica che l'opacità background cambi da 90% a 95%

### 6.6 Test Runtime Editor

1. Apri console debug (F9)
2. Vai alla sezione "6. HUD Notifications 2.0 Runtime Editor"
3. Modifica alcune dimensioni
4. Click "Apply Changes"
5. Verifica che le modifiche si applichino in tempo reale

---

## Passo 7: Integrazione con Codice Esistente

### 7.1 Sistema Codici Automatici

Il sistema genera automaticamente codici tematici sci-fi post-apocalittici per ogni notifica:
- **Success/ActionSuccess**: `OPR-XXX` (Operation)
- **ItemCollected**: `INV-XXX` (Inventory)
- **ResourceGained**: `RES-XXX` (Resource)
- **Info**: `SYS-XXX` (System)
- **Warning**: `WRN-XXX` (Warning)
- **Error**: `ERR-XXX` (Error)
- **Critical**: `CRI-XXX` (Critical)
- **PlantDied**: `PLT-DTH-XXX` (Plant Death)
- **ExtremePhDeath**: `PH-DTH-XXX` (pH Death)
- E molti altri...

I codici vengono generati automaticamente con formato `PREFIX-XXX` dove XXX è un numero incrementale a 3 cifre.

### 7.2 Layout Titolo/Descrizione

Ogni notifica mostra:
- **Prima riga (Codice)**: Colore più chiaro/saturato, font 10px
- **Seconda riga (Descrizione)**: Colore più scuro/muted, font 11px

Il sistema applica automaticamente i colori corretti in base alla severità della notifica.

### 7.3 Icone Automatiche

Il sistema seleziona automaticamente l'icona corretta:
- **Warning**: Triangolo esclamazione (WarningIcon)
- **Info/Success**: Cerchio "i" (InfoIcon/SuccessIcon)
- **Error/Critical**: Cerchio esclamazione (DangerIcon)

### 7.4 Esempio Utilizzo

```csharp
// Ottieni manager
var manager = ServiceContainer.Instance.Get<HUDNotificationFeedManager2_0>();

// Mostra notifica standard (codice generato automaticamente)
manager.ShowNotification(ToastNotificationType.Success, "Operazione completata!");
// Genera automaticamente: "OPR-001"

// Override codice manuale (opzionale)
manager.ShowNotification(ToastNotificationType.Success, "Operazione completata!", "CUSTOM-001");

// Helper methods (codici generati automaticamente)
manager.ShowSuccess("Messaggio successo"); // Genera: "OPR-002"
manager.ShowError("Messaggio errore");     // Genera: "ERR-001"
manager.ShowWarning("Messaggio warning");  // Genera: "WRN-001"
manager.ShowInfo("Messaggio info");        // Genera: "SYS-001"

// Item notification (codice generato automaticamente)
manager.ShowItemNotification("Spore", 5, "Laboratorio", itemIcon: itemIcon);
// Genera automaticamente: "INV-003"
```

### 7.2 Migrazione Graduale

Puoi migrare gradualmente i sistemi esistenti:
1. Mantieni il vecchio sistema nascosto
2. Sostituisci le chiamate al vecchio manager con il nuovo
3. Testa ogni sistema migrato
4. Una volta completata la migrazione, rimuovi il vecchio sistema

---

## Troubleshooting

### Header non appare

- Verifica che il prefab `HUDNotificationHeader2.0` sia istanziato
- Verifica che tutti i riferimenti siano collegati nel manager
- Verifica che la config sia in `Resources/Configs/`

### Notifiche non appaiono

- Verifica che il pool abbia il prefab assegnato
- Verifica che il container sia attivo quando l'header è espanso
- Controlla la console per errori

### Colori non cambiano

- Verifica che la config abbia i colori corretti
- Verifica che `UpdateHeader()` venga chiamato dopo ogni notifica

### Runtime Editor non funziona

- Verifica che il manager 2.0 sia registrato in ServiceContainer
- Verifica che la config sia caricata correttamente
- Controlla la console per errori

---

## Note Finali

- Il sistema 2.0 è completamente separato dal vecchio sistema
- Il vecchio sistema rimane funzionante ma nascosto
- Puoi rimuovere il vecchio sistema dopo aver completato la migrazione
- Tutte le dimensioni sono configurabili via config o runtime editor

---

**Buon lavoro!** 🚀

