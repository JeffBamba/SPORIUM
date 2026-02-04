# Analisi cleanup: Assets, Scripts e UI obsoleti

Analisi basata su **SceneHierarchy.txt**, repository e piani di migrazione (UI Toolkit, HUD, Lab).  
Obiettivo: individuare cosa può essere **rimosso** o **disattivato** in sicurezza (script, codice, UI Canvas legacy, oggetti di scena).

---

## 1. Script potenzialmente obsoleti o non usati

### 1.1 Probabilmente non referenziati in scena

| Script | Motivo | Azione suggerita |
|--------|--------|-------------------|
| **CameraFollow2D.cs** | La scena usa **Cinemachine** (Virtual Camera, CinemachineConfiner2D). Nessun riferimento a CameraFollow2D nei prefab/scene. | **Rimuovere** lo script (e .meta). Verificare che nessun prefab lo usi. |
| **IntroCutScene.cs** | Mai referenziato in SCN_VaultMap.unity. Probabile relitto di una vecchia intro. | **Rimuovere** se non usato in altre scene; altrimenti spostare in cartella _Deprecated. |
| **SPOR-BLK-01-03A-SystemTest.cs** | Script di test (Dome). Non compare nella gerarchia di scena. | **Spostare** in cartella Editor/Test o **rimuovere** se i test non sono più necessari. |
| **MoldRiskSynergyTest.cs** | Test di sinergia mold. Non in hierarchy. | **Spostare** in Editor/Test o rimuovere se obsoleto. |

### 1.2 Solo sviluppo / debug (da escludere in build, non necessariamente da cancellare)

| Script | Uso | Azione |
|--------|-----|--------|
| **SPOR-BLK-01-03A-GrowthDebugger.cs** | F6 per stampare stato vasi (doc BLK-01.03A). | Tenere; eventualmente wrappare in `#if UNITY_EDITOR` o mettere su GameObject disattivabile. |
| **PhSystemDebugConsole.cs** | Debug pH. Referenziato da PhSystemAutoSetup, HUDPhDisplay, PlantCardV3TerminalController. | Tenere; usato da altri script. |
| **DifficultyCalibrationConsole.cs** | Console calibrazione difficoltà. In hierarchy come GAMEPLAY_Balancing_Console. | Tenere; disattivabile in build. |
| **ToastNotificationDebugConsole.cs** | Debug toast. | Tenere per sviluppo. |
| **GlobalStateInspector.cs** | Console F1. | Tenere per sviluppo. |
| **FoundationNotificationsDebugConsole.cs** | Debug Notifications Foundation. | Tenere per sviluppo. |
| **PotDebugConsole.cs** | Debug pot. | Tenere per sviluppo. |
| **UISeedSelectorAutoSetup.cs** | Editor/setup UISeedSelector. | Tenere se usi setup automatico; altrimenti valutare rimozione. |

### 1.3 DummyAction / ActionCost

- **DummyAction.cs** e **ActionCost** sono usati su **BTN_DummyAction** sotto **UI_Notification** (HUD → UI_Notification).
- Servono per test/demo del costo azioni nel vecchio pannello notifiche.
- **Azione**: Se il flusso notifiche passa tutto a **ToastNotificationManager** + **FoundationNotificationsPanelController**, si può rimuovere il GameObject **BTN_DummyAction** dalla scena e, in seguito, gli script **DummyAction** / **ActionCost** se non usati altrove.

---

## 2. UI e Canvas legacy (candidati a rimozione dopo migrazione)

La scena ha **due mondi UI**:
- **Canvas (uGUI)** con molti pannelli vecchi (HUDInventory, PotDetailsWidget, UISeedSelector, UI_Notification, pannelli Lab con prefab Canvas).
- **UIToolkit (UIDocument)** con HUD_TopBar, HUD_BottomNavigation, PlayerStatusPanel, PlantCardV2/V3, PotActionsMenu, SeedInventoryMenu, IrrigationDialog, UI_LabExtractorPanel, UI_PlayerInventoryPanel, Notifications Foundation.

I piani (es. `modifica_interazione_pot_e_ristrutturazione_hud_-_ui_toolkit`) e il codice (es. `PotDetailsWidget._useLegacyUI`) indicano una migrazione verso **solo UIToolkit**.

### 2.1 UI_PotDetails (PotDetailsWidget)

- **PotDetailsWidget** ha il flag `_useLegacyUI`; se `false` si usa **PlantCardV2** (e in futuro PlantCardV3 Terminal).
- **AlwaysVisiblePotHUD** e altri ancora referenziano PotDetailsWidget per tooltip/condizione; **PlantCardV2Controller** e **PlantCardV3TerminalController** evitano di far gestire il click a PotDetailsWidget quando aprono loro il pannello.
- **Azione**:  
  - Se in progetto si decide di usare **solo** PlantCardV2/PlantCardV3 e PotActionsMenu, si può **disattivare** il GameObject **UI_PotDetails** e impostare `_useLegacyUI = false` ovunque.  
  - **Rimuovere** del tutto UI_PotDetails e PotDetailsWidget solo dopo aver spostato eventuali logiche ancora usate (es. tooltip, condizione) in PlantCard/AlwaysVisiblePotHUD e aver verificato i flussi.

### 2.2 UI_Inventory (HUDInventory)

- Usato da: **PlayerStatusPanelController** (fallback), **LabMinigameExtractor**, **SeedStorageUI**, **LabCatalizzatore**, **LabPippete**, **LabMicroscope**, **SeedStorage**, **PotDetailsWidget** (indirettamente).
- **PlayerInventoryPanelController** (UIToolkit) è il pannello inventario “nuovo”; il vecchio HUDInventory è ancora il fallback e usato dai Lab.
- **Azione**: Non rimuovere finché i Lab e il flusso principale non usano **solo** **PlayerInventoryPanelController**. Poi si può disattivare **UI_Inventory** e rimuovere i riferimenti a HUDInventory negli script.

### 2.3 UISeedSelector (Canvas)

- Usato da **PotDetailsWidget** e **PlantCardV2Controller** per la scelta del seme.
- **Azione**: Tenere finché PlantCard V2/V3 non ha un flusso seed interamente UIToolkit. Poi deprecare/rimuovere il GameObject e lo script.

### 2.4 UI_Notification (UINotification)

- **UINotification** è ancora usato in molti punti: DayCycleController, PotSlot, Visitor, ElevatorSystem, PotNotifications, PruningDialog, HUDPhDisplay, HUDCondensation, GamePlayInstaller. **ToastNotificationManager** usa UINotification per i banner persistenti.
- **Azione**: **Non rimuovere** finché il nuovo sistema toast (ToastNotificationManager + FoundationNotificationsPanelController) non diventa l’unico canale e tutti i riferimenti a UINotification non sono stati migrati.

### 2.5 Pannelli Lab legacy (Canvas)

Dalla **GUIDA_LAB_MACCHINARIO_PER_MACCHINARIO.md** e dalla gerarchia:

- **UI_LabMinigame** (LabMinigameExtractor) – Extractor  
- **UI_LabMicroscope** (LabMicroscope) – Microscope  
- **UI_LabPippete** (LabPipette) – Pipette  
- **UI_Catalizzatore** (LabCatalizzatore) – Catalizzatore  
- **UI_Incubator** (IncubatorUI) – Incubator  

**Extractor.cs** preferisce **LabExtractorPanelController** (UIToolkit); se non assegnato, fa fallback a **LabMinigameExtractor**. Stessa logica va verificata per Catalizzatore, Pipette, Microscope, Incubator (controller UIToolkit vs legacy).

- **Azione**:  
  1. In scena, assegnare a ogni macchinario (Extractor, Catalizzatore, Pipette, Microscope, Incubator) il rispettivo **pannello UIToolkit** (LabExtractorPanelController, LabCatalizzatorePanelController, ecc.).  
  2. Verificare in Play che non si apra più nessun pannello Canvas legacy.  
  3. Poi si possono **disattivare** o **rimuovere** i GameObject: UI_LabMinigame, UI_LabMicroscope, UI_LabPippete, UI_Catalizzatore, UI_Incubator (e relativi script legacy se non più referenziati).

### 2.6 Duplicato nome Pipette: UI_LabPippete vs UI_Pippete

- In gerarchia ci sono **UI_LabPippete** (LabPipette – intro/menu) e **UI_Pippete** (PipetteView + PipetteGame – minigame).
- Typo: "Pippete" invece di "Pipette".
- **Azione**: Consolidare i nomi (es. rinominare in **UI_LabPipette** e **UI_PipetteMinigame**) e documentare quale è legacy e quale è usato dal flusso attuale; dopo migrazione a UIToolkit, rimuovere i legacy come sopra.

### 2.7 Altri elementi Canvas da valutare

| Elemento | Note | Azione |
|----------|------|--------|
| **BTN_EndDay** | Potrebbe essere duplicato da TopBar/BottomNav (UIToolkit). | Verificare se EndDay è solo su TopBar; in caso affermativo rimuovere BTN_EndDay dalla scena. |
| **HUD / UI_Resources** (HUDController, TXT_Day, TXT_Actions, TXT_Cry) | Risorse e giorno potrebbero essere già in HUD_TopBar. | Verificare sovrapposizione con TopBarController; disattivare/rimuovere il blocco Canvas duplicato. |
| **GrowthTooltipPanel** | Solo Canvas; tooltip crescita potrebbero essere in PlantCard V2/V3. | Se tutti i tooltip crescita passano a PlantCard, rimuovere il GameObject GrowthTooltipPanel. |
| **WikipediaButton** (sotto HUD) | Esiste già flusso Wikipedia; verificare se c’è duplicato in UIToolkit. | Verificare e rimuovere duplicato se presente. |

---

## 3. Oggetti di scena (GameObject) – riepilogo

### 3.1 Da rimuovere / disattivare (dopo verifiche)

- **PlantCardV2** + **PlantCardV2Opener**: Se il gioco usa **solo** PlantCardV3 (Terminal) per i pot, si può rimuovere dalla scena il GameObject PlantCardV2 e PlantCardV2Opener. **Attenzione**: PotActionsMenu e il piano HUD prevedono ancora “INSPECT → PlantCardV2”; se il flusso è “INSPECT → PlantCardV3”, allora V2 può essere rimosso dalla scena (gli script PlantCardV2* si possono tenere fino a migrazione completa).
- **Room_Dome_Placeholder**: Placeholder; rimuovere se la room reale (ROOM_Dome) è sempre usata e il placeholder non serve più.
- **PH DEBUG** (HUDPhDisplay): Solo debug. Disattivare in build o spostare sotto un parent “Debug” disattivabile.
- **PotDebugConsole**, **GAMEPLAY_Balancing_Console**, **ToastNotificationDebugConsole**, **GlobalStateInspector**, **Notifications Foundation - debug**: Solo sviluppo. Disattivare in build o rimuovere dalla scena di produzione.
- **BTN_DummyAction**: Rimuovibile se non si usa più il pannello notifiche legacy per test azioni (vedi DummyAction/ActionCost).

### 3.2 Da tenere (ancora in uso)

- **AlwaysVisiblePotHUD**: Usato per HUD minimali sui pot; referenzia PotDetailsWidget. Tenere fino a eventuale riscrittura con UIToolkit.
- **Canvas** (root), **EventSystem**, **HUD_GameViewportBackground**, **ToastNotificationSystem**, **Menu** (MainMenu), tutti gli oggetti di gameplay (PLY_*, ELEV_*, ROOM_*, GameManager, Virtual Camera, ecc.): Mantenere.

---

## 4. Script Editor / Config

- **Editor/** (CleanupURP, CreateLabItemConfigs, ExportHierarchyToTxt, ToastNotificationConfigSetup, ecc.): Tenere; utili per pipeline.
- **HUDNotifications2.0** (HUDNotificationFeedManager2_0, Pool, Header, Item, Config): Usati da **ToastNotificationManager** e da **ToastNotificationDebugConsole**. Non rimuovere.
- **UINotification.cs**: Ancora centrale per molti toast; non rimuovere fino a migrazione completa al nuovo sistema.

---

## 5. Ordine consigliato di interventi

1. **Sicuro subito**  
   - Rimuovere **CameraFollow2D.cs** (e .meta) se nessun prefab/scena lo usa.  
   - Valutare rimozione **IntroCutScene.cs** (e .meta) se non usato.  
   - Spostare **SPOR-BLK-01-03A-SystemTest.cs** e **MoldRiskSynergyTest.cs** in cartella Editor/Test o rimuoverli se obsoleti.

2. **Dopo verifica in scena**  
   - Assegnare a tutti i macchinari Lab i controller UIToolkit; poi disattivare/rimuovere i GameObject Lab legacy (UI_LabMinigame, UI_LabMicroscope, UI_LabPippete, UI_Catalizzatore, UI_Incubator).  
   - Se il flusso Pot è solo PlantCardV3 + PotActionsMenu: disattivare **UI_PotDetails** e rimuovere **BTN_EndDay** se duplicato da TopBar.  
   - Rinominare **UI_LabPippete** / **UI_Pippete** per chiarezza (Pipette) e aggiornare riferimenti.

3. **In seguito (migrazione completa)**  
   - Sostituire tutti gli usi di **UINotification** con il sistema toast/Foundation e poi rimuovere **UI_Notification** e **UINotification**.  
   - Sostituire **HUDInventory** con **PlayerInventoryPanelController** ovunque e rimuovere **UI_Inventory**.  
   - Sostituire **UISeedSelector** con flusso UIToolkit e rimuovere il pannello Canvas.  
   - Rimuovere **PotDetailsWidget** e **UI_PotDetails** dopo aver spostato le logiche residue in PlantCard/AlwaysVisiblePotHUD.

---

## 6. Riepilogo file/asset candidati a rimozione

| Tipo | Nome | Priorità |
|------|------|----------|
| Script | CameraFollow2D.cs | Alta (probabile inutilizzato) |
| Script | IntroCutScene.cs | Media (verificare altre scene) |
| Script | SPOR-BLK-01-03A-SystemTest.cs | Media (test) |
| Script | MoldRiskSynergyTest.cs | Media (test) |
| GameObject | UI_PotDetails (intero) | Dopo migrazione a PlantCard V2/V3 |
| GameObject | UI_LabMinigame, UI_LabMicroscope, UI_LabPippete, UI_Catalizzatore, UI_Incubator | Dopo assegnazione panel UIToolkit ai macchinari |
| GameObject | PlantCardV2, PlantCardV2Opener | Se si usa solo PlantCardV3 |
| GameObject | BTN_EndDay | Se duplicato da TopBar |
| GameObject | BTN_DummyAction | Se non si usa più UINotification per test |
| GameObject | Room_Dome_Placeholder | Se non più usato |
| GameObject | PH DEBUG, PotDebugConsole, GAMEPLAY_Balancing_Console, ToastNotificationDebugConsole, GlobalStateInspector, Notifications Foundation - debug | Solo build: disattivare o rimuovere dalla scena release |

---

*Documento generato dall’analisi di SceneHierarchy.txt, codice in `Assets/_Project` e piani in `.cursor/plans`.*
