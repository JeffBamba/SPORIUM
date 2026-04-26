# DEV REPORT 0095 — Localizzazione IT/EN code-first, overlay Opzioni UI Toolkit e superfici HUD/menu

**Data:** 2026-04-26  
**Sprint / contesto:** Demo Alpha / testi visibili al giocatore, preferenza lingua da Opzioni, allineamento Main Menu UI Toolkit e Foundation HUD.  
**Riferimento piano:** `.cursor/plans/demo_alpha_1_0_gap_map.plan.md`  
**Report precedente:** `DEV_REPORT_0094_ICONE_VARIANTI_INVENTARIO_SEEDSTORAGE_2026-04-25.md`

---

## Sommario interventi

1. Esteso il **core code-first** di localizzazione (`LocalizationManager`: chiavi dotted, `Pick`, `Format` / token `{nome}`, `GetString` dove serve) mantenendo compatibilità con chiavi legacy.
2. Introdotta/rafforzata la mappa **nomi item** lato display (`ItemDisplayNameLocalization`) collegata a inventario, payload raccolta e altre superfici senza rimuovere i `typeId` tecnici dalle logiche.
3. **Opzioni** res disponibili come **overlay UI Toolkit** nel Main Menu (`MainMenu.uxml` / `MainMenu.uss`), con selezione lingua, ESC e wiring da `MainMenuUIToolkitController` e `MainMenuOptions` (fallback uGUI ridotto / log di avviso se Toolkit non pronto).
4. Dalla **Compact bottom bar** in game: apertura menu ingame + overlay opzioni allineato allo stesso percorso Toolkit (`CompactBottomBarController`).
5. **Notifiche Foundation**: `TooltipEn` su `NotificationTypeSpec`, risoluzione tooltip per lingua in `NotificationLocalization` / `FoundationNotificationsPanelController`, default aggiornati in `NotificationTypeSpecDefaults`.
6. **Fix compilazione** in `PlayerInventoryPanelController`: uso di `fruit` fuori scope nel ramo frutti (CS0103) — `displayName` calcolato nel `foreach` corretto.
7. Passate testuali/localizzazione su **Player Status**, **Collection** (factory/stack), **Lab** (pannelli catalizzatore/fusione), **Extractor** display, **PlantCardV3**, **PruningDialog** / **LabMinigameExtractor**, più consolidamenti **Dome** (`PotActions`, `PotActionValidator`), **FoodRoom**, **SaveManager**, **PhSystem**, inspector dev.
8. **Asset icone** in `Resources/Items` (PNG + meta), aggiornamento `GlobalIconCatalog.asset`, icone UI Condenza; ritocchi `PlayerInventoryPanel.uss`, `DomeStatusHUD.uxml`, prefab `PruningDialog`.
9. Documentazione operativa **`LOCALIZATION.md`** aggiornata allo stato attuale del sistema.

---

## Statistiche e progresso

### Righe di codice

- **`Assets/_Project/Scripts` (C#):** da `git diff --numstat HEAD` sui path sotto `Assets/_Project/Scripts`, **33 file `.cs`**, **+839** inserimenti e **-306** rimozioni (churn **1145** righe). Il file `ItemDisplayNameLocalization.cs.meta` (**+11** inserimenti, **0** rimozioni) è escluso da questo aggregato C#; il totale inserimenti nella cartella Scripts incluso meta è **+850**.
- **Intero albero `Assets/_Project` (tutti i tipi di file nel diff):** `git diff --shortstat HEAD -- Assets/_Project` → **83 file**, **+3515** inserimenti, **-355** rimozioni.
- **Comando build:** `dotnet build Sporae_Build_Beta.sln` (output: **0 errori, 0 avvisi**).

### Sistemi funzionanti

- Compilazione **MSBuild** della solution Unity: **verificata** in questa iterazione (vedi sopra).
- **Smoke Play in Editor** (apertura overlay Opzioni da menu e da HUD, cambio lingua end-to-end, regressioni save slot): **non misurato in questa iterazione** — da checklist manuale su `SCN_VaultMap`.

### Bug risolti

1. **CS0103** — `PlayerInventoryPanelController`: nome `fruit` non in scope nel rendering righe frutto; correzione spostando/derivando il display name nel ciclo dove `fruit` è definito.

### Progresso gameplay / prodotto

- Il giocatore può impostare la **lingua** da un overlay **coerente con il Main Menu UI Toolkit** invece di dipendere dal solo popup uGUI legacy.
- **Inventario** e altre etichette mostrano **nomi item localizzati** dove migrato, mantenendo identificativi tecnici interni.
- I **tooltip delle notifiche** possono riflettere la lingua selezionata (IT/EN) laddove specificato negli spec.
- **Barra compatta** in game apre le opzioni sullo stesso stack visivo del menu principale quando il runtime Toolkit è pronto.
- Restano **aree con copy hardcoded** (es. parti di Lab, End-of-day, altri pannelli non toccati in questo changeset): backlog per passate incrementali successive.

---

## 1. LocalizationManager e documentazione

### Problema

- Serviva un unico punto **code-first** per stringhe IT/EN con formattazione token e chiavi stabili, senza rompere le chiamate esistenti a `GetString`.

### Soluzione

- Estesi metodi e lookup (chiavi dotted, `Pick` per lingua corrente, `Format` con sostituzione token) centralizzati in `LocalizationManager`.
- Aggiornato `LOCALIZATION.md` con lo stato reale del flusso (chiavi, sessione, superfici collegate).

**File interessati:**  
`Assets/_Project/Scripts/Core/Localization/LocalizationManager.cs`,  
`Assets/_Project/Docs/LOCALIZATION.md`

---

## 2. Nomi item visibili (ItemDisplayNameLocalization)

### Problema

- I `typeId` restano la verità di sistema; i **nomi mostrati** devono essere traducibili e coerenti tra pannelli.

### Soluzione

- Uso di `ItemDisplayNameLocalization` (e integrazioni in fabbrica item / resolver dove già previsto dal filone corrente) per risolvere il display name in base alla lingua attiva.

**File interessati:**  
`Assets/_Project/Scripts/Core/Localization/ItemDisplayNameLocalization.cs` (+ `.meta`),  
`Assets/_Project/Scripts/Core/ItemsSystem/ItemFabric.cs`,  
`Assets/_Project/Scripts/Core/ItemsSystem/Items.cs`

---

## 3. Overlay Opzioni Main Menu (UI Toolkit)

### Problema

- Le opzioni erano ancora legate in larga parte al **popup uGUI** legacy; serviva parità visiva con il **Main Menu UITK** e un solo percorso preferito a runtime.

### Soluzione

- Aggiunti nodi `options-overlay` in `MainMenu.uxml` e stili in `MainMenu.uss`.
- `MainMenuUIToolkitController`: apertura/chiusura overlay, ESC, binding lingua (`GameLanguageSettings`) e sync con `NotificationLocalization`, highlight pulsanti lingua.
- `MainMenuOptions.HandleOptions()`: se il Toolkit runtime è pronto, apre l’overlay; altrimenti log di avviso (niente doppio binario parallelo come default principale).

**File interessati:**  
`Assets/_Project/Resources/UI/UIToolkit/MainMenu/MainMenu.uxml`,  
`Assets/_Project/Resources/UI/UIToolkit/MainMenu/MainMenu.uss`,  
`Assets/_Project/Scripts/UI/UIToolkit/MainMenu/MainMenuUIToolkitController.cs`,  
`Assets/_Project/Scripts/UI/MainMenu/MainMenuOptions.cs`,  
`Assets/_Project/Scripts/UI/MainMenu/OptionsPopupController.cs` (deprecazione / messaggi / percorso non primario)

---

## 4. Opzioni da HUD: Compact bottom bar

### Problema

- Il pulsante opzioni in game non allineava l’esperienza al nuovo overlay Toolkit.

### Soluzione

- `CompactBottomBarController`: su click opzioni, `ShowInGameMenu()` + `OpenOptionsOverlay()` sul controller Main Menu Toolkit; rimossi riferimenti non usati al popup legacy.

**File interessato:**  
`Assets/_Project/Scripts/UI/UIToolkit/HUD/CompactBottomBarController.cs`

---

## 5. Notifiche Foundation: tooltip bilingue

### Problema

- Tooltip notifiche legati solo a copy italiano negli spec.

### Soluzione

- `NotificationTypeSpec.TooltipEn`, risoluzione in `NotificationLocalization.ResolveTooltip`, uso in `FoundationNotificationsPanelController`; default aggiornati in `NotificationTypeSpecDefaults`.

**File interessati:**  
`Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/NotificationTypeSpec.cs`,  
`Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/NotificationLocalization.cs`,  
`Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/FoundationNotificationsPanelController.cs`,  
`Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/NotificationTypeSpecDefaults.cs`,  
`Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/NotificationPayload.cs`,  
`Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/FoundationNotificationService.cs`,  
`Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/NotificationItemIconResolver.cs`

---

## 6. Inventario, stato giocatore, raccolta, altre superfici UI

### Problema

- Stringhe hardcoded e bug di scope nel ramo **frutti** dell’inventario; allineamento copy su più pannelli HUD.

### Soluzione

- `PlayerInventoryPanelController`: fix CS0103 e passaggio a chiavi/display name localizzati dove previsto.
- `PlayerStatusPanelController`, `CollectionPayloadFactory`, `CollectionBoxStackController`: allineamenti copy/localizzazione.
- Ritocchi UI correlati (`PlayerInventoryPanel.uss`, `DomeStatusHUD.uxml`).

**File interessati:**  
`Assets/_Project/Scripts/UI/UIToolkit/PlayerInventory/PlayerInventoryPanelController.cs`,  
`Assets/_Project/Scripts/UI/UIToolkit/PlayerStatusPanelController.cs`,  
`Assets/_Project/Scripts/UI/UIToolkit/HUD/CollectionPayloadFactory.cs`,  
`Assets/_Project/Scripts/UI/UIToolkit/HUD/CollectionBoxStackController.cs`,  
`Assets/_Project/UI/UIToolkit/PlayerInventory/PlayerInventoryPanel.uss`,  
`Assets/_Project/UI/UIToolkit/DomeStatusHUD/DomeStatusHUD.uxml`

---

## 7. Lab, Vault, gameplay collaterali

### Problema

- Piccole incongruenze copy o wiring in pannelli Lab / Vault / Dome / Food collegate allo stesso filone di chiarezza e localizzazione.

### Soluzione

- Aggiornamenti puntuali in `LabCatalizzatorePanelController`, `LabFusionPanelController`, `PlantCardV3TerminalController`, `PruningDialog`, `LabMinigameExtractor`, `ExtractorInGameDisplayRuntime`, `ExtractorSporeProtoDisplayRuntime`, `Extractor`, `PotActions`, `PotActionValidator`, `FoodRoomSystem`, `SaveManager`, `PhSystem`, `GlobalStateInspector`.

**File interessati:** (elenco nei path della tabella sotto)

---

## 8. Icone item e catalogo

### Problema

- Allineamento risorse grafiche item e catalogo icone al resolver globale (continuità con report 0094).

### Soluzione

- Nuovi/aggiornati PNG in `Assets/_Project/Resources/Items/` con relativi `.meta`.
- Aggiornamento `GlobalIconCatalog.asset`, `GlobalIconCatalog.cs`, `GlobalIconResolver.cs`.
- Icone UI aggiuntive in `Assets/_Project/Art/UI/Icone ITEMS & Actions/`.

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/Core/Localization/LocalizationManager.cs` | API localizzazione estesa (`Pick`, `Format`, chiavi dotted) |
| `Assets/_Project/Scripts/Core/Localization/ItemDisplayNameLocalization.cs` (+ `.meta`) | Display name item per lingua |
| `Assets/_Project/Scripts/Core/ItemsSystem/ItemFabric.cs`, `Items.cs` | Integrazione display name / categorie |
| `Assets/_Project/Docs/LOCALIZATION.md` | Documentazione aggiornata |
| `Assets/_Project/Resources/UI/UIToolkit/MainMenu/MainMenu.uxml` | Overlay opzioni + struttura UITK |
| `Assets/_Project/Resources/UI/UIToolkit/MainMenu/MainMenu.uss` | Stili overlay opzioni / lingua |
| `Assets/_Project/Scripts/UI/UIToolkit/MainMenu/MainMenuUIToolkitController.cs` | Overlay, lingua, ESC, save slot copy |
| `Assets/_Project/Scripts/UI/MainMenu/MainMenuOptions.cs` | Routing opzioni verso Toolkit |
| `Assets/_Project/Scripts/UI/MainMenu/OptionsPopupController.cs` | Percorso legacy non primario / avvisi |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/CompactBottomBarController.cs` | Opzioni in game → overlay Toolkit |
| `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/*.cs` | Tooltip EN, resolver, default, payload/service |
| `Assets/_Project/Scripts/UI/UIToolkit/PlayerInventory/PlayerInventoryPanelController.cs` | Fix CS0103 + stringhe localizzate |
| `Assets/_Project/Scripts/UI/UIToolkit/PlayerStatusPanelController.cs` | Copy localizzato |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/CollectionPayloadFactory.cs`, `CollectionBoxStackController.cs` | Allineamento raccolta / notifiche |
| `Assets/_Project/UI/UIToolkit/PlayerInventory/PlayerInventoryPanel.uss` | Stili inventario |
| `Assets/_Project/UI/UIToolkit/DomeStatusHUD/DomeStatusHUD.uxml` | Ritocco gerarchia/testi |
| `Assets/_Project/Scripts/UI/UIToolkit/Lab/LabCatalizzatorePanelController.cs`, `LabFusionPanelController.cs` | Copy Lab |
| `Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs` | Copy terminale |
| `Assets/_Project/Scripts/UI/UIToolkit/ExtractorDisplay/ExtractorInGameDisplayRuntime.cs`, `ExtractorSporeProtoDisplayRuntime.cs` | Copy display |
| `Assets/_Project/Scripts/UI/VaultMap/PruningDialog.cs`, `LabMinigameExtractor.cs` | Copy / messaggi |
| `Assets/_Project/Scripts/Dome/PotActions.cs`, `PotActionValidator.cs` | Messaggi / validazione |
| `Assets/_Project/Scripts/Interactables/Extractor.cs` | Copy interazione |
| `Assets/_Project/Scripts/Systems/FoodRoom/FoodRoomSystem.cs` | Copy sistema |
| `Assets/_Project/Scripts/Core/SaveManager.cs`, `PhSystem.cs` | Stringhe / messaggi |
| `Assets/_Project/Scripts/DevTools/Inspector/GlobalStateInspector.cs` | Etichette dev |
| `Assets/_Project/Scripts/UI/Icons/GlobalIconCatalog.cs`, `GlobalIconResolver.cs` | Catalogo / risoluzione |
| `Assets/_Project/Resources/UI/GlobalIconCatalog.asset` | Mapping serializzato |
| `Assets/_Project/Resources/Items/*.png` (+ `.meta`) | Icone item Resources |
| `Assets/_Project/Art/UI/Icone ITEMS & Actions/Icona Condenza*.png` | Icone UI |
| `Assets/_Project/Prefabs/UI/PruningDialog.prefab` | Prefab allineato |

*Elenco completo dei path nel diff: `git diff --name-only HEAD -- Assets/_Project` (83 file).*

---

## Regole / vincoli rispettati

- **Both (demo + full):** nessun fork scena dedicata; overlay e stringhe condivisibili tra sessioni demo e partita piena salvo gating già previsto altrove.
- **UI Toolkit / UI Builder:** opzioni aggiunte in UXML/USS del Main Menu esistente, non albero parallelo “solo runtime”.
- **Architettura:** nessun nuovo `FindObjectOfType` introdotto per questi flussi; risoluzione servizi tramite percorsi già in uso nei controller menu/HUD.
- **`typeId`:** mantenuti come chiavi di gioco; localizzazione applicata al **layer display**.

---

## Note operative (Unity)

1. Smoke in **Play Mode** su `SCN_VaultMap`: aprire **Opzioni** da Main Menu (`btn-settings`) e da **HUD compact bar**; verificare **ESC** chiuda overlay; cambiare **IT/EN** e controllare inventario, notifiche e save slot.
2. Confermare che, se il documento Main Menu Toolkit non è in scena, il log di avviso sia accettabile e non blocchi il flusso critico.
3. Continuare la **passata incrementale** su pannelli ancora hardcoded (Lab avanzato, End-of-day, Cryo/Dispensa, ecc.) usando le stesse convenzioni di `LocalizationManager`.

---

*Fine DEV REPORT 0095.*
