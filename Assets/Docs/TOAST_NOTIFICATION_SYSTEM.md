# Sistema Toast Notifications - Documentazione Completa

## Panoramica

Il sistema Toast Notifications è un sistema centralizzato e standardizzato per mostrare notifiche temporanee all'utente. Include:

- **Auto-dismiss**: Ogni toast si auto-rimuove dopo 8 secondi
- **Max 3 toast visibili**: Gestione automatica con rimozione FIFO
- **Toggle espansione**: Header clickable per espandere/contrarre dettagli
- **Colore dinamico header**: Basato sulla severità più alta dei toast attivi
- **ID incrementale**: ID unici per ogni toast
- **Object Pooling**: Evita Instantiate/Destroy continuo
- **Console Debug (F9)**: Tool per triggerare toast, visualizzare history, filtri, statistics

## Architettura

### Componenti Principali

1. **ToastNotificationType**: Enum con tipi standardizzati e severità (0-4)
2. **ToastNotificationConfig**: ScriptableObject per configurazione centralizzata
3. **ToastNotificationHistory**: Sistema di tracking per debug
4. **ToastNotificationUIItem**: Componente UI per singolo toast
5. **ToastNotificationPool**: Object pooling per performance
6. **ToastNotificationHeader**: Header "NOTIFICATIONS" con toggle e badge
7. **ToastNotificationManager**: Manager centralizzato
8. **ToastNotificationDebugConsole**: Console debug (F9)

### Flusso di Utilizzo

```
Sistema → ToastNotificationManager.ShowToast() 
  → ToastNotificationPool.GetFromPool() 
  → ToastNotificationUIItem.Initialize() 
  → Auto-dismiss dopo 8s 
  → ReturnToPool()
```

## Setup

### 1. Creare ToastNotificationConfig Asset

1. In Unity Editor: `Assets > Create > Spore > ToastNotificationConfig`
2. Salvare in `Assets/Resources/Configs/ToastNotificationConfig.asset`
3. Configurare:
   - **Type Settings**: Colori, durate, code prefixes per ogni tipo
   - **UI Settings**: Width (306px), font monospaced, font size (10pt)
   - **Pixel Art Settings**: Border sprite, corner sprite, glow shader
   - **Icons**: AlertCircle, Info, Warning, Danger, Chevron
   - **Global Settings**: Default duration (3s), max history entries (100)

### 2. Creare Prefab ToastNotificationItem

1. Creare GameObject `ToastNotificationItem` con RectTransform (width=306, height=auto)
2. Aggiungere componente `ToastNotificationUIItem`
3. Struttura UI:
   - Background Image (#1E282A alpha 0.9)
   - Border Image (2px solid, FilterMode.Point)
   - 4 Corner Images (L-shape 3×3, FilterMode.Point)
   - Severity Icon (Info/Warning/Danger)
   - Code Text (monospaced, 10pt, #8B9894)
   - Message Text (monospaced, 10pt, white)
   - Expanded Content (timestamp, inizialmente disattivo)
   - CanvasGroup (per animazioni fade)
4. Salvare in `Assets/_Project/Prefabs/UI/ToastNotificationItem.prefab`

### 3. Creare Prefab ToastNotificationHeader

1. Creare GameObject `ToastNotificationHeader` con RectTransform
2. Aggiungere componente `ToastNotificationHeader`
3. Struttura UI:
   - Background Image (#1E282A alpha 0.9)
   - Border Image (2px solid, FilterMode.Point)
   - 4 Corner Images (L-shape 3×3, FilterMode.Point)
   - Alert Icon (AlertCircle sprite)
   - Header Text ("NOTIFICATIONS", uppercase, monospaced, tracking aumentato)
   - Badge Container con Text (numero toast)
   - Chevron Icon (Arrow sprite, rotabile)
   - Toggle Button (covers entire header)
4. Salvare in `Assets/_Project/Prefabs/UI/ToastNotificationHeader.prefab`

### 4. Setup Scena

1. Nella scena principale, trovare il `Canvas` principale (quello con `EventSystem` come child)
2. Sotto `Canvas`, creare GameObject `ToastNotificationSystem` come **sibling di `HUD`** (non dentro HUD, stesso livello gerarchico)
   - **Nota**: `HUD` è già presente nella scena, quindi `ToastNotificationSystem` va creato come suo sibling
3. Aggiungere `ToastNotificationManager` a `ToastNotificationSystem`
4. Aggiungere `ToastNotificationPool` a `ToastNotificationSystem` (assegnare prefab ToastNotificationItem)
5. Setup RectTransform root:
   - Anchor: Top-right (1, 1)
   - Pivot: (1, 1)
   - Position: (-24, -96)
   - Size: (306, auto)
6. Creare GameObject `Header` come child di `ToastNotificationSystem` (istanziare prefab ToastNotificationHeader)
7. Creare GameObject `ToastContainer` come child di `ToastNotificationSystem` (parent per toast attivi, VerticalLayoutGroup)
8. Collegare tutti i riferimenti in `ToastNotificationManager` inspector

**Struttura Gerarchia** (basata su SceneHierarchy.txt):
```
Canvas
├── EventSystem
├── BTN_EndDay
├── UISeedSelector
├── UI_Inventory
├── HUD (esistente)
│   ├── Condensation
│   ├── Missions
│   ├── UI_Resources
│   ├── UI_Notification (esistente - UINotification)
│   └── WikipediaButton
├── UI_PotDetails
├── ... (altri UI)
└── ToastNotificationSystem (NUOVO - sibling di HUD)
    ├── Header
    └── ToastContainer
```

## Utilizzo

### API Base

```csharp
// Ottieni manager
var manager = ServiceContainer.Instance.Get<ToastNotificationManager>();

// Mostra toast generico
manager.ShowToast(ToastNotificationType.Success, "Operazione completata!", "OP-001");

// Helper methods
manager.ShowSuccess("Messaggio successo", "SUCCESS-001");
manager.ShowError("Messaggio errore", "ERROR-001");
manager.ShowWarning("Messaggio warning", "WARNING-001");
manager.ShowInfo("Messaggio info", "INFO-001");

// Banner persistenti
manager.ShowBanner("Banner message", ToastNotificationType.Info, out System.Action clearCallback);
clearCallback(); // Per rimuovere banner
```

### Codici Standardizzati

#### Pot Actions
- `POT-ACTION-SUCCESS`: Azione vaso completata con successo
- `POT-ACTION-FAILED`: Azione vaso fallita
- `POT-WATER-SUCCESS`: Sistema irrigazione attivato/disattivato
- `POT-LIGHT-SUCCESS`: LED acceso/spento
- `POT-PLANT-SUCCESS`: Pianta piantata
- `POT-FERTILIZE-SUCCESS`: Fertilizzazione completata
- `POT-HARVEST-SUCCESS`: Raccolto completato
- `POT-SPRAY-SUCCESS`: Spray applicato
- `POT-UPROOT-SUCCESS`: Pianta sradicata

#### Day Cycle
- `STAGE-UP-001`: Cambio stadio pianta
- `LGT-002`: Sistema LED spento (CRY insufficiente)
- `LGT-003`: LED Blue attivo
- `LGT-004`: LED Red attivo
- `CND-001`: Condizione peggiorata
- `CND-002`: Condizione migliorata
- `MOLD-001`: Pianta infestata
- `PH-DEATH-001`: Morte per pH estremo
- `PH-COUNTDOWN-001`: Countdown morte pH estremo

#### Pot Details
- `PRUNE-SUCCESS-001`: Potatura completata
- `PRUNE-FAILED-001`: Potatura fallita
- `PRUNE-INFO-001`: Info potatura
- `PLANT-DEATH-001`: Pianta morta

#### Inventory & Resources
- `INV-FRUIT-001`: Frutto aggiunto all'inventario
- `SPORE-001`: Spora ottenuta
- `SPORE-CORRUPT-001`: Spora corrotta
- `SPORE-TRAIT-001`: Spora con tratti ottenuta
- `WATER-001`: Acqua raccolta

#### System
- `ELEVATOR-001`: Elevatore fuori servizio
- `SEED-STORAGE-001`: Errore seed storage
- `VISITOR-001`: Banner visitor

## Console Debug (F9)

**IMPORTANTE**: Basato sulla struttura reale della scena (`SceneHierarchy.txt`), la console debug deve essere creata come GameObject **root-level** (stesso livello di `GameManager`, `PotDebugConsole`, `GlobalStateInspector` - linee 2725-2756 in SceneHierarchy.txt).

La console debug permette di:

1. **Trigger Toast**: Dropdown tipo, input messaggio, input codice, pulsante "Show"
2. **Quick Actions**: Pulsanti per triggerare toast comuni (Success, Error, Warning, Info)
3. **History Viewer**: Lista ultimi 50 toast con filtri (tipo, codice, timestamp)
4. **Statistics**: Contatori per tipo, toast più frequenti, ultimo toast
5. **Settings**: Clear history, export history (JSON/CSV)

### Export History

- **JSON**: Esporta in `Application.persistentDataPath/toast_history_YYYYMMDD_HHMMSS.json`
- **CSV**: Esporta in `Application.persistentDataPath/toast_history_YYYYMMDD_HHMMSS.csv`

## Palette Colori

- **INFO** (#7FFF7A): Verde LED - Success/Info
- **WARNING** (#E6C96F): Giallo - Warning
- **DANGER** (#D35F5F): Rosso - Error/Critical
- **BLUE NEUTRAL** (#5DB6E3): Blu - Header neutro (nessun toast attivo)
- **Background** (#1E282A alpha 0.9): Background semi-trasparente
- **Text Secondary Light** (#C0C8C5): Testo secondario chiaro
- **Text Secondary Dark** (#8B9894): Testo secondario scuro

## Severità

- **0**: Success (Success, ActionSuccess, ItemCollected, ResourceGained)
- **1**: Info (Info, StageUp, ConditionImproved, SystemEnabled)
- **2**: Warning (Warning, ConditionDegraded, SystemDisabled, CountdownAlert)
- **3**: Error (Error, ActionFailed, ResourceInsufficient, InvalidOperation)
- **4**: Critical (Critical, PlantDied, ExtremePhDeath, SystemFailure)

## Animazioni DOTween

### Entrata Toast
- Fade: 0 → 1 (0.3s)
- Position: +100px → 0 (0.3s)
- Scale: 0.8 → 1 (0.3s)

### Uscita Toast
- Fade: 1 → 0 (0.3s)
- Position: 0 → +100px (0.3s)
- Scale: 1 → 0.8 (0.3s)

### Toggle Espansione
- Fade: 0.8 → 1 (0.3s) quando espanso
- Fade: 1 → 0.8 (0.3s) quando contratto

### Chevron Rotazione
- Rotazione: 0° → 180° (0.2s) quando contratto
- Rotazione: 180° → 0° (0.2s) quando espanso

## Retrocompatibilità

Il sistema mantiene retrocompatibilità con `UINotification`:

- Se `ToastNotificationManager` non è disponibile, fallback automatico a `UINotification`
- I sistemi esistenti possono continuare a usare `UINotification` direttamente
- Migrazione graduale possibile

## Performance

- **Object Pooling**: Pre-warm con 10 item iniziali, evita Instantiate/Destroy
- **Max 3 toast visibili**: Limita overhead rendering UI
- **Auto-dismiss 8s**: Gestione automatica ciclo vita toast
- **History limitata**: Max 100 entry (configurabile), zero overhead quando disabilitata
- **Console debug**: Solo Editor/Development build

## Troubleshooting

### Toast non appaiono

1. Verifica che `ToastNotificationManager` sia registrato in `ServiceContainer`
2. Verifica che `ToastNotificationConfig.asset` esista in `Resources/Configs/`
3. Verifica che prefab `ToastNotificationItem` sia assegnato al pool
4. Controlla Console per errori

### Colore header non cambia

1. Verifica che `ToastNotificationHeader` sia assegnato in `ToastNotificationManager`
2. Verifica che toast attivi abbiano severità corretta
3. Controlla che `UpdateHeaderColor()` venga chiamato

### Console debug non si apre (F9)

1. Verifica che `ToastNotificationDebugConsole` sia presente nella scena come **root-level GameObject** (stesso livello di `PotDebugConsole`, `GlobalStateInspector`)
2. Verifica che `enableDebugConsole` sia true
3. Solo Editor/Development build: Console disabilitata in Release
4. Verifica che il GameObject non sia dentro `Canvas` o altri container (deve essere root-level)

## Note Tecniche

- **FilterMode.Point**: Tutti gli sprite devono avere FilterMode.Point per pixel-perfect rendering
- **Font Monospaced**: Usare Courier New, Consolas, o custom pixel font
- **RectTransform Setup**: Anchor top-right (1,1), pivot (1,1), position (-24, -96)
- **VerticalLayoutGroup**: Toast container usa VerticalLayoutGroup per layout automatico

## File Modificati

### Nuovi File
- `Assets/_Project/Scripts/DevTools/Notification/ToastNotificationType.cs`
- `Assets/_Project/Scripts/DevTools/Notification/ToastNotificationConfig.cs`
- `Assets/_Project/Scripts/DevTools/Notification/ToastNotificationHistory.cs`
- `Assets/_Project/Scripts/DevTools/Notification/ToastNotificationUIItem.cs`
- `Assets/_Project/Scripts/DevTools/Notification/ToastNotificationPool.cs`
- `Assets/_Project/Scripts/DevTools/Notification/ToastNotificationHeader.cs`
- `Assets/_Project/Scripts/DevTools/Notification/ToastNotificationManager.cs`
- `Assets/_Project/Scripts/DevTools/Notification/ToastNotificationDebugConsole.cs`

### File Migrati
- `Assets/_Project/Scripts/UI/VaultMap/Pot/PotNotifications.cs`
- `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`
- `Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs`
- `Assets/_Project/Scripts/Interactables/PotSlot.cs`
- `Assets/_Project/Scripts/UI/VaultMap/LabMinigameExtractor.cs`
- `Assets/_Project/Scripts/UI/VaultMap/HUDCondensation.cs`
- `Assets/_Project/Scripts/UI/VaultMap/MicroscopeMinigame/MicroscopeHUDView.cs`
- `Assets/_Project/Scripts/UI/VaultMap/PruningDialog.cs`
- `Assets/_Project/Scripts/World/Elevator/ElevatorSystem.cs`
- `Assets/_Project/Scripts/UI/VaultMap/SeedStorage/SeedStorageUI.cs`
- `Assets/_Project/Scripts/Core/Visitors/Visitor.cs`

### File Aggiornati
- `Assets/_Project/Scripts/Core/Installers/GamePlayInstaller.cs`

---

**Versione**: 1.0  
**Data**: 2025-01-XX  
**Autore**: Sistema Toast Notifications Refactoring

