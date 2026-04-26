# Localizzazione (multilanguage) – SPORIUM

## Stato attuale

- **Lingue supportate**: `Auto`, `Italian`, `English`.
- **Sistema code-first**: le stringhe player-facing migrate passano da chiavi C# centralizzate, non da Unity Localization Package.
- **Copertura attuale**: menu opzioni / menu principale UI Toolkit, slot save/load, nomi item, inventario player (incl. riga deterioramento organico in tooltip), Player Status, **HUD navigazione (CompactBottomBar, TopBar)**, **Dome Status HUD**, **Food Room panel**, **Dispensa refrigerata** (HUD overlay: stati, costi, log, camere), **tutti i pannelli Lab** (Catalizzatore, Fusion, Extractor, Incubator, **Kitchen Terminal**), **notifiche Foundation** (tooltip EN allineati), **PruningDialog** (UGUI), **Cryo machine panel**, **Seed Storage panel**, **etichetta outfit armadio** (`PlayerOutfitController` + refresh su cambio lingua), **PlantCardV3** (pannello A9: tooltip suggerimenti, driver rapidi, messaggi disabilitazione azioni), **End of day** (snapshot, note, forecast, diario, alba + tooltip), messaggi **PotActions** (`EmitActionFailed`) e **Extractor** (etichetta mondo 3D + descrizioni input log lab) passano da `LocalizationManager` / `ItemDisplayNameLocalization` dove applicato.
- **Migrazione progressiva**: restano stringhe IT (o miste) in **PlantCardV3 terminale/console** (comandi, output DOS, copy non ancora mappata), **SaveManager** solo log dev, altri messaggi one-off. Verificare in play con **English** effettivo (non Auto su OS italiano).

## Architettura

- **`GameLanguage`** (`Core/Localization/GameLanguage.cs`): enum `Auto`, `Italian`, `English`.
- **`GameLanguageSettings`** (`Core/Localization/GameLanguageSettings.cs`): legge/scrive la lingua in **PlayerPrefs** (`Sporium_Language`). Usato da Opzioni e dal sistema notifiche.
- **`LocalizationManager`** (`Core/Localization/LocalizationManager.cs`): punto centrale per chiave → stringa (IT/EN), con `GetString(key)`, `GetString(key,args)`, `Pick(it,en)`, `Format(template,args)` e `Register(key,it,en)`.
- **`ItemDisplayNameLocalization`** (`Core/Localization/ItemDisplayNameLocalization.cs`): nomi item player-facing per `typeId`, incluse varianti spora grezza/matura e nomi pianta/seme da metadata.
- **`NotificationLocalization`** (notifiche): facade compatibile per template/toast, usa `GameLanguageSettings` e risolve anche `TooltipIt` / `TooltipEn`.
- **`OptionsPopupController`** (`UI/MainMenu/OptionsPopupController.cs`): salva la lingua e sincronizza notifiche. Se il prefab non ha controlli lingua assegnati, crea a runtime una riga **Auto / IT / EN**; se sono assegnati dropdown o bottoni, usa quelli.

## Menu Opzioni

Il player può cambiare lingua da **Opzioni**:

- Se il prefab contiene un **TMP_Dropdown** assegnato a `Language Dropdown`, il controller mostra `Auto / Italiano / English`.
- In alternativa si possono assegnare bottoni ai campi `Btn Language It` / `Btn Language En`.
- Se non c’è nessun riferimento serializzato, il controller crea automaticamente una riga runtime con bottoni `Auto / IT / EN`.

## Estendere le stringhe localizzate

- Per nuove stringhe generiche: aggiungere la coppia in `LocalizationManager.Table` con chiave dotted (`hud.day`, `inventory.empty`, `lab.extractor.start`) e usare `LocalizationManager.GetString("chiave")`.
- Per template con token: usare `LocalizationManager.GetString("chiave", args)` oppure `LocalizationManager.Format(template,args)`.
- Per notifiche: usare `NotificationTypeSpec.TemplateIt` / `TemplateEn`; se c’è tooltip, compilare anche `TooltipIt` / `TooltipEn` quando disponibile.
- Per testi rapidi costruiti a codice: usare `LocalizationManager.Pick("Italiano", "English")` o `NotificationLocalization.Pick(...)` nei sistemi notification legacy.
- Per nomi item: non mostrare `typeId` al player; usare `PlayerInventoryPanelController.GetItemDisplayName(...)` o `ItemDisplayNameLocalization`.

## Regole operative

- Non localizzare `typeId`, save key, codici missione o dati tecnici: solo testo player-facing.
- Nei pannelli UI Toolkit, mantenere placeholder leggibili in UXML per UI Builder; il controller sostituisce i testi dinamici a runtime.
- Quando una UI resta aperta durante il cambio lingua, sottoscriversi a `GameLanguageSettings.OnLanguageChanged` e richiamare il proprio `Refresh` / `Rebuild`.
- Per nuove UI, evitare nuove stringhe hardcoded se visibili al player.

## Persistenza

La lingua è salvata in **PlayerPrefs** (`Sporium_Language`), non nel salvataggio di gioco. Resta quindi uguale per tutte le partite e dopo il riavvio del gioco.

## Checklist QA (lingua English)

1. Opzioni → **English** (non Auto), avviare VaultMap.
2. Menu principale, inventario, notifiche + tooltip.
3. HUD: TopBar, bottom bar, Dome Status HUD (tab POT/CRYO, etichette stati).
4. Food Room, Dispensa refrigerata (accendi/spegni, log, righe inventario), Seed Storage, Cryo, Pruning (potatura con toggle spray).
5. Lab: ogni macchinario + terminale Kitchen (stati macchina, testi progetto, analisi).
6. PlantCard: pannello suggerimenti rapidi e driver.
7. Fine giornata: conferma → snapshot → passi successivi (testo EN coerente).
8. Azioni vaso fallite (es. piantare senza seme) → messaggio EN da `PotActions`.
