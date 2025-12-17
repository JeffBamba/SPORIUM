# Guida Completamento Toast Notification System

## ✅ Modifiche Implementate nel Codice

Tutte le modifiche al codice sono state completate. Ora devi completare il setup in Unity.

---

## 📋 Checklist Pre-Setup

Prima di iniziare, verifica che tu abbia:
- [ ] Prefab `ToastNotificationItem` esistente (creato nelle istruzioni precedenti)
- [ ] Prefab `ToastNotificationHeader` esistente
- [ ] ScriptableObject `ToastNotificationConfig` in `Resources/Configs/`
- [ ] `ToastNotificationSystem` nella scene sotto `Canvas` (come in SceneHierarchy.txt, linea 2287)
  - Verifica che abbia `Header`, `ToastContainer`, e `PoolParent` come children

---

## 🔧 Passo 1: Aggiornare Prefab ToastNotificationItem

Il prefab deve supportare sia il layout standard che il layout Item Notification.

### 1.1 Apri il Prefab ToastNotificationItem

1. Nella **Project** window, naviga a `Assets/_Project/Prefabs/UI/` (o dove hai salvato il prefab)
2. **Double-click** su `ToastNotificationItem` per aprirlo in **Prefab Mode**

### 1.2 Crea StandardLayoutContainer

1. Nella **Hierarchy** del prefab, seleziona `ToastNotificationItem` (root)
2. **Click destro** su `Content` → `Create Empty`
3. Rinomina in `StandardLayoutContainer`
4. Seleziona `StandardLayoutContainer`
5. Nel **Inspector**, **trascina** tutti i figli di `Content` dentro `StandardLayoutContainer`:
   - `IconBox`
   - `TextContainer`
6. **Verifica** che `StandardLayoutContainer` contenga:
   - `IconBox` (con child `SeverityIcon`)
   - `TextContainer` (con children: `CodeText`, `MessageText`, `ExpandedContent`)

### 1.3 Crea ItemLayoutContainer

1. **Click destro** su `Content` → `Create Empty`
2. Rinomina in `ItemLayoutContainer`
3. Seleziona `ItemLayoutContainer`
4. Nel **Inspector**, componente **GameObject**:
   - **Active**: **Deseleziona la checkbox** (inizialmente disattivo)

### 1.4 Crea Struttura Item Layout

1. **Click destro** su `ItemLayoutContainer` → `UI` → `Panel` (o `Create Empty` e aggiungi `Image`)
2. Rinomina in `ItemContent`
3. Seleziona `ItemContent`
4. Aggiungi componente `Horizontal Layout Group`:
   - **Spacing**: 8
   - **Child Alignment**: `Upper Left`
   - **Child Control Width**: Spunta
   - **Child Control Height**: Spunta
   - **Child Force Expand Width**: Non spuntato
   - **Child Force Expand Height**: Non spuntato

### 1.5 Crea ItemIconLarge dentro ItemContent

1. **Click destro** su `ItemContent` → `UI` → `Image`
2. Rinomina in `ItemIconLarge`
3. Seleziona `ItemIconLarge`
4. Nel **RectTransform**:
   - **Width**: 40
   - **Height**: 40
5. Nel **Inspector**, componente **Image**:
   - **Color**: Bianco (temporaneo, sarà colorato dal codice)

### 1.6 Crea ItemTextContainer dentro ItemContent

1. **Click destro** su `ItemContent` → `UI` → `Panel` (o `Create Empty`)
2. Rinomina in `ItemTextContainer`
3. Seleziona `ItemTextContainer`
4. Aggiungi componente `Vertical Layout Group`:
   - **Spacing**: 2
   - **Child Alignment**: `Upper Left`
   - **Child Control Width**: Spunta
   - **Child Control Height**: Non spuntato
   - **Child Force Expand Width**: Spunta
   - **Child Force Expand Height**: Non spuntato

### 1.7 Crea ItemHeaderText dentro ItemTextContainer

1. **Click destro** su `ItemTextContainer` → `UI` → `Text - TextMeshPro`
2. Rinomina in `ItemHeaderText`
3. Seleziona `ItemHeaderText`
4. Nel **Inspector**, componente **TextMeshProUGUI**:
   - **Text**: "ADDED TO INVENTORY" (temporaneo)
   - **Font Size**: 10
   - **Color**: Bianco
   - **Alignment**: Left, Top

### 1.8 Crea ItemNameText dentro ItemTextContainer

1. **Click destro** su `ItemTextContainer` → `UI` → `Text - TextMeshPro`
2. Rinomina in `ItemNameText`
3. Seleziona `ItemNameText`
4. Nel **Inspector**:
   - **Text**: "+3 ItemName" (temporaneo)
   - **Font Size**: 10
   - **Color**: Bianco
   - **Alignment**: Left, Top

### 1.9 Crea ItemLocationText dentro ItemTextContainer

1. **Click destro** su `ItemTextContainer` → `UI` → `Text - TextMeshPro`
2. Rinomina in `ItemLocationText`
3. Seleziona `ItemLocationText`
4. Nel **Inspector**:
   - **Text**: "📍 Location" (temporaneo)
   - **Font Size**: 10
   - **Color**: R=192, G=200, B=197 (grigio chiaro #C0C8C5)
   - **Alignment**: Left, Top

### 1.10 Collega Riferimenti nello Script

1. Seleziona `ToastNotificationItem` (root)
2. Nel **Inspector**, trova il componente `Toast Notification UI Item`
3. Trascina e rilascia i nuovi GameObject:
   - **Standard Layout Container**: Trascina `Content/StandardLayoutContainer`
   - **Item Layout Container**: Trascina `Content/ItemLayoutContainer`
   - **Item Icon Large**: Trascina `Content/ItemLayoutContainer/ItemContent/ItemIconLarge`
   - **Item Header Text**: Trascina `Content/ItemLayoutContainer/ItemContent/ItemTextContainer/ItemHeaderText`
   - **Item Name Text**: Trascina `Content/ItemLayoutContainer/ItemContent/ItemTextContainer/ItemNameText`
   - **Item Location Text**: Trascina `Content/ItemLayoutContainer/ItemContent/ItemTextContainer/ItemLocationText`

### 1.11 Verifica Struttura Finale

La struttura del prefab dovrebbe essere:

```
ToastNotificationItem (root)
├── Background
├── Border
├── Corner_TL, Corner_TR, Corner_BL, Corner_BR
├── Content
│   ├── StandardLayoutContainer (ACTIVE)
│   │   ├── IconBox
│   │   │   └── SeverityIcon
│   │   └── TextContainer
│   │       ├── CodeText
│   │       ├── MessageText
│   │       └── ExpandedContent (INACTIVE)
│   │           └── TimestampText
│   └── ItemLayoutContainer (INACTIVE)
│       └── ItemContent
│           ├── ItemIconLarge
│           └── ItemTextContainer
│               ├── ItemHeaderText
│               ├── ItemNameText
│               └── ItemLocationText
├── CanvasGroup
└── Button (opzionale)
```

### 1.12 Salva il Prefab

1. **Click** su `Overrides` in alto a destra del prefab
2. **Click** su `Apply All` per salvare tutte le modifiche

---

## 🔧 Passo 2: Aggiornare Prefab ToastNotificationHeader

### 2.1 Apri il Prefab ToastNotificationHeader

1. Nella **Project** window, naviga a `Assets/_Project/Prefabs/UI/`
2. **Double-click** su `ToastNotificationHeader` per aprirlo in **Prefab Mode**

### 2.2 Verifica HeaderText

1. Seleziona `HeaderText` (TextMeshPro)
2. Nel **Inspector**, componente **TextMeshProUGUI**:
   - **Text**: Dovrebbe essere `"SYSTEM NOTIFICATIONS"` (se non lo è, cambialo)
   - **Font**: Monospaced (da config)
   - **Font Size**: 10-11
   - **Character Spacing**: Aumentato (da config)

### 2.3 Verifica BadgeText

1. Seleziona `BadgeContainer/BadgeText` (TextMeshPro)
2. Nel **Inspector**:
   - **Text**: Il codice aggiornerà automaticamente al numero di notifiche (0, 1, 2, 3...), ma puoi impostare temporaneamente `"0"` per test

### 2.4 Salva il Prefab

1. **Click** su `Overrides` → `Apply All`

---

## 🔧 Passo 3: Verificare Configurazione ToastNotificationManager

### 3.1 Trova ToastNotificationSystem nella Scene

1. Nella **Hierarchy**, naviga a `Canvas` (quello con `EventSystem` come child, linea 29 di SceneHierarchy.txt)
2. Espandi `Canvas` e cerca `ToastNotificationSystem` (stesso livello di `HUD`, linea 2287)
3. Seleziona `ToastNotificationSystem`

**Nota**: Il GameObject si chiama `ToastNotificationSystem` e contiene il componente `ToastNotificationManager`.

### 3.2 Configura Posizione e Dimensioni (MANUALE)

**IMPORTANTE**: La posizione del `ToastNotificationSystem` deve essere configurata manualmente in Unity. Il codice non imposta più questi valori.

1. Seleziona `ToastNotificationSystem` nella **Hierarchy**
2. Nel **Inspector**, trova il componente **Rect Transform**
3. Configura manualmente:
   - **Anchor Presets**: Click sull'icona dell'ancora in alto a sinistra del RectTransform
     - Per posizione top-right: Tieni premuto `Shift + Alt` e click su `top-right`
     - Oppure configura manualmente:
       - **Anchor Min**: (1, 1)
       - **Anchor Max**: (1, 1)
       - **Pivot**: (1, 1)
   - **Pos X / Pos Y**: Imposta la posizione desiderata (es. -24, -96 per top-right con offset)
   - **Width**: 306 (o la larghezza desiderata)
   - **Height**: 0 o auto (sarà gestito dal VerticalLayoutGroup)

**Nota**: Puoi posizionare il sistema dove preferisci. La posizione non è più hardcoded nel codice.

### 3.3 Verifica Struttura della Gerarchia

Verifica che `ToastNotificationSystem` abbia questa struttura (come in SceneHierarchy.txt, linee 2287-2348):

```
ToastNotificationSystem
├── Header
│   └── ToastNotificationHeader (componente script + UI elements)
├── ToastContainer (VerticalLayoutGroup)
└── PoolParent
```

### 3.4 Verifica Riferimenti nel ToastNotificationManager

Nel **Inspector**, componente `Toast Notification Manager`, verifica che siano assegnati:
- [ ] **Config**: `ToastNotificationConfig` (da Resources/Configs/)
- [ ] **Header**: Trascina `Header/ToastNotificationHeader` (il GameObject con lo script)
- [ ] **Toast Container**: Trascina `ToastContainer` (child diretto di ToastNotificationSystem)
- [ ] **Toast Prefab**: `ToastNotificationItem` prefab (dal Project)
- [ ] **Pool Size**: 10 (o valore appropriato)

**Nota**: `ToastContainer` è un child diretto di `ToastNotificationSystem`, NON di `Header`.

### 3.5 Verifica Header è Espanso di Default

1. Nella **Hierarchy**, naviga a `ToastNotificationSystem` → `Header` → `ToastNotificationHeader`
2. Seleziona `ToastNotificationHeader`
3. Nel **Inspector**, componente `Toast Notification Header`:
   - Verifica che tutti i riferimenti UI siano assegnati (Background, Border, Corner_TL/TR/BL/BR, AlertIcon, HeaderText, BadgeContainer, ChevronIcon)
   - Il codice gestisce automaticamente lo stato espanso/collassato

---

## 🧪 Passo 4: Test del Sistema

### 4.1 Test Layout Standard

1. **Play** la scene
2. Usa la **Toast Notification Debug Console** (se disponibile) o chiama:
   ```csharp
   var toastManager = ServiceContainer.Instance.Get<ToastNotificationManager>();
   toastManager.ShowInfo("Test message", "TEST-001");
   ```
3. **Verifica**:
   - [ ] Toast appare in alto a destra
   - [ ] Header mostra "SYSTEM NOTIFICATIONS"
   - [ ] Badge mostra "1"
   - [ ] Code e Message sono su righe separate (verticale)
   - [ ] Glow pulsante per 0.5s
   - [ ] Toast più recente in alto (LIFO)

### 4.2 Test Layout Item Notification

1. **Play** la scene
2. Chiama:
   ```csharp
   var toastManager = ServiceContainer.Instance.Get<ToastNotificationManager>();
   toastManager.ShowItemNotification("Spore di Aloe", 3, "Laboratory", "INV-001");
   ```
3. **Verifica**:
   - [ ] Layout speciale appare (icona grande 40x40, "ADDED TO INVENTORY", "+3 Spore di Aloe", "📍 Laboratory")
   - [ ] Layout standard è nascosto
   - [ ] Colori applicati correttamente

### 4.3 Test Sistema di Priorità

1. **Play** la scene
2. Crea 4 toast (3 INFO + 1 DANGER):
   ```csharp
   toastManager.ShowInfo("Info 1", "INFO-001");
   toastManager.ShowInfo("Info 2", "INFO-002");
   toastManager.ShowInfo("Info 3", "INFO-003");
   toastManager.ShowError("Danger!", "DANGER-001");
   ```
3. **Verifica**:
   - [ ] Solo 3 toast visibili (MAX_VISIBLE_TOASTS = 3)
   - [ ] DANGER non viene rimosso (rimane visibile)
   - [ ] La più vecchia INFO viene rimossa quando arriva la 4a

### 4.4 Test Toggle Collapse/Expand

1. **Play** la scene
2. **Click** sull'header "SYSTEM NOTIFICATIONS"
3. **Verifica**:
   - [ ] Chevron ruota 180°
   - [ ] Toast container si nasconde (collapsed)
   - [ ] Badge rimane visibile con il numero di notifiche
   - [ ] Click di nuovo → toast riappaiono (expanded)

---

## 🔄 Passo 5: Aggiornare Chiamate Esistenti (Opzionale)

Se vuoi usare il layout speciale Item Notification per le chiamate esistenti:

### 5.1 LabMinigameExtractor.cs

**File**: `Assets/_Project/Scripts/UI/VaultMap/LabMinigameExtractor.cs`

**Linea ~201**: Cambia da:
```csharp
toastManager.ShowToast(ToastNotificationType.ItemCollected, "You got a spore!", "SPORE-001");
```

A:
```csharp
toastManager.ShowItemNotification("Spore Generic", 1, "Laboratory", "SPORE-001");
```

### 5.2 PotSlot.cs

**File**: `Assets/_Project/Scripts/Interactables/PotSlot.cs`

**Linea ~193**: Cambia da:
```csharp
toastManager.ShowToast(ToastNotificationType.ItemCollected, $"New Fruit added to Inventory: {amount}", "INV-FRUIT-001");
```

A:
```csharp
toastManager.ShowItemNotification("Fruits", amount, "Dome", "INV-FRUIT-001");
```

**Nota**: Se hai bisogno di un nome più specifico per l'item o la location, adatta di conseguenza.

---

## ✅ Checklist Finale

Prima di considerare completato il task, verifica:

- [ ] Prefab `ToastNotificationItem` aggiornato con layout Item Notification
- [ ] Prefab `ToastNotificationHeader` mostra "SYSTEM NOTIFICATIONS"
- [ ] Badge mostra solo il numero (0, 1, 2, 3...)
- [ ] Layout standard funziona (Code e Message verticali)
- [ ] Layout Item Notification funziona (icona grande, location)
- [ ] Sistema di priorità funziona (DANGER non rimosso)
- [ ] Ordine LIFO funziona (più recente in alto)
- [ ] Glow pulsante funziona (primi 0.5s)
- [ ] Toggle collapse/expand funziona
- [ ] Chiamate esistenti aggiornate (opzionale)

---

## 🐛 Troubleshooting

### Problema: Layout Item Notification non appare

**Soluzione**:
1. Verifica che `ItemLayoutContainer` sia creato e assegnato nello script
2. Verifica che `StandardLayoutContainer` e `ItemLayoutContainer` siano figli di `Content`
3. Controlla che `SetupLayout(true)` venga chiamato in `InitializeItemNotification()`

### Problema: Badge non mostra il numero corretto

**Soluzione**:
1. Verifica che `ToastNotificationHeader.cs` sia aggiornato con la nuova logica
2. Controlla che `UpdateBadge()` usi `count.ToString()` per mostrare solo il numero
3. Verifica che `_badgeContainer.SetActive(true)` sia chiamato per mantenere il badge sempre visibile

### Problema: Toast non in ordine LIFO

**Soluzione**:
1. Verifica che `SetAsFirstSibling()` sia usato invece di `SetAsLastSibling()`
2. Controlla che `_activeToasts.Insert(0, toastItem)` sia usato invece di `Add()`

---

## 📝 Note Finali

- Il layout Item Notification è **opzionale**: le chiamate esistenti continueranno a funzionare con il layout standard
- Puoi aggiornare le chiamate esistenti quando vuoi per usare il layout speciale
- Il sistema di priorità protegge automaticamente le DANGER notifications
- Il glow pulsante è automatico per tutti i toast (primi 0.5s)

---

**Task completato!** 🎉

