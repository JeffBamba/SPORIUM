# DEV REPORT 0103 — Demo Beat3, UX modali/macchine, fix input camera runtime

**Data:** 2026-04-28  
**Sprint / contesto:** Demo Alpha 1.0 — consolidamento flusso narrativo Beat 3, coerenza UX panel runtime, tooling debug demo, stabilizzazione input/UI e follow camera in mappa Vault.  
**Riferimento piano:** `.cursor/plans/demo_alpha_1_0_gap_map.plan.md`  
**Report precedente:** `DEV_REPORT_0102_INVENTARIO_BAR_CONDIZIONE_TRAFFIC_LIGHT_2026-04-27.md`

---

## Sommario interventi

- Rifinita e data-driven la narrativa Demo (Beat 1/Beat 3): testi VO finali autore, highlight mission-critical, sequenza Seed Storage anomalia + CRY tooltip education.
- Aggiornata la progressione missione Beat 3: completamento su **riaccensione Seed Storage + chiusura panel**, non più su ingresso area.
- Uniformata UX machine panel: stato OFF visuale/funzionale su Seed Storage, Dispensa Refrigerata e Sintetizzatore Alimentare.
- Dispensa Refrigerata riallineata al pattern UX “seleziona + bottone trasferimento” (coerenza con Seed Storage).
- Introdotta `DemoDebugConsole` (`ALT + D`) con jump rapidi di checkpoint demo, force complete missione corrente, reset flag missione demo.
- Risolto il multi-click intermittente su Inventario/macchinari (intercettazione input da UI invisibile) con cleanup completo della strumentazione.
- Ultimo blocco chat: analisi runtime camera e passaggio lens a 5 in Vault con bootstrap locale e disattivazione confiner runtime per evitare camera “piantata”.
- Aggiornamento finale Beat 1 Wake VO: sequenza convertita in gruppi timed, blocco anti-skip durante typing, split dell'ultimo messaggio missione in due schermate consecutive.

---

## Statistiche e progresso

### Righe di codice

- Misurazione su modifiche **staged** con comando:
  - `git diff --cached --numstat`
- Totale variazioni staged rilevate:
  - **+1944** righe aggiunte
  - **-179** righe rimosse
- Ambito calcolo: include script C#, UXML/USS, scene, asset/config e metadati toccati durante la chat.

### Sistemi funzionanti

- **Verificato con riproduzione runtime utente:**
  - Sequenze demo Beat 3 con gating missione e trigger VO post-interazione Seed Storage.
  - Debug console demo in build editor/dev con controlli di avanzamento/reset mission flags.
  - Input UI inventory/macchinari nuovamente a click singolo dopo fix su layer/picking invisibile.
  - Follow camera con lens 5 in Vault: issue dichiarata risolta in debug mode prima del cleanup instrumentation.
- **Verificato da lint:**
  - Nessun errore lint sui file C# toccati nei passaggi finali (`VaultCameraRuntimeBootstrap`, `GamePlayInstaller` e file correlati già controllati durante iterazioni).

### Bug risolti

- **6 bug principali documentati e chiusi in sessione:**
  1. VO CRY tooltip non avviato a completamento missione Seed Storage.
  2. Richiesta multipli click su inventario/apertura-chiusura e interazioni macchinari.
  3. Stato OFF machine non coerente visivamente/funzionalmente tra panel.
  4. Dispensa con trasferimento click-to-item poco chiaro/non allineato a UX comune.
  5. Camera runtime in Vault non seguiva correttamente il player dopo cambio lens.
  6. Sequenza Beat 1 Wake che saltava/troncava blocchi VO invece di completare il testo prima del gruppo successivo.

### Progresso gameplay / prodotto

- Il Beat 3 comunica meglio il “perché” economico del mantenimento impianti (costi fissi CRY) in modo contestuale all’evento Seed Storage.
- Il giocatore ha un linguaggio UX più leggibile e coerente su tutte le macchine ON/OFF.
- L’inventario mantiene HUD di contesto visibile (stato player, missioni, notifiche), migliorando decisioni sull’uso item.
- I tester hanno strumenti debug diretti per avanzare/resettare la demo senza editing manuale di stato.
- L’onboarding narrativa Beat 1 è stato reso più incisivo con intro VO a gruppi temporizzati e finale missione spezzato in due schermate leggibili.
- Stabilità percepita in input/camera migliorata nei passaggi critici di gameplay.

---

## 1. Narrativa Demo: Beat 1 + Beat 3 (data-driven)

### Problema

- Copy e trigger VO erano parzialmente hardcoded/non completi in asset.
- Sequenza Beat 3 richiedeva tono, pacing, highlight parole missione e step di apprendimento costi CRY più robusti.

### Soluzione

- Estesa configurazione narrativa con nuovi campi dedicati in `DemoAlphaNarrativeConfig` + fallback in `DemoAlphaNarrativeDefaults`.
- Popolato `DemoAlphaNarrativeConfig.asset` con testi finali autore (Beat 3 anomalia Seed Storage + blocchi esplicativi costi/entrate CRY, Beat 1 intro in 3 blocchi).
- Aggiornata orchestrazione in `DemoStoryDirector`:
  - autoplay sequenza Beat 3 su apertura Seed Storage,
  - evidenziazioni multi-colore parole chiave,
  - trigger VO successivi a chiusura panel + hover tooltip CRY,
  - nuova regia Beat 1 a blocchi con timing espliciti e anti-skip.
- Estesa `VoOverlayController` con supporto `holdAfterTypingSeconds` per mantenere la logica VO esistente e ritardare il passaggio solo dopo completamento typing.

---

## 2. Mission flow Beat 3 e sistemi di supporto

### Problema

- Missione “Vai al Seed Storage” era completata troppo presto (su ingresso area), non coerente col nuovo design esperienziale.

### Soluzione

- Spostato completamento a evento composto: **power ON Seed Storage + chiusura panel**.
- Aggiornata logica missione demo in `WardrobeMission` (`DemoSeedStorageMission`) e integrazione nel `DemoStoryDirector`.
- Evitata sovrapposizione VO generiche mission complete su questa missione specifica in `ActiveMissionsPanelController`.

---

## 3. UX modali/HUD e coerenza pannelli machine

### Problema

- Inventario nascondeva HUD di contesto in modo penalizzante.
- Stato OFF non uniforme tra machine panel.
- Dispensa aveva transfer UX poco chiaro rispetto agli altri sistemi.

### Soluzione

- `GameplayUiModalLock` rifattorizzato per distinguere HUD fisso e HUD contestuale; inventario con override visibilità contestuale.
- `PlayerInventoryPanelController`, `PlayerStatusPanelController`, `ActiveMissionsPanelController`, `FoundationNotificationsPanelController` aggiornati per nuova semantica `HidesContextHud`.
- Implementato stato OFF visuale/funzionale in:
  - `SeedStoragePanelController` + USS/UXML,
  - `DispensaPanelController` + UXML/USS,
  - `FoodRoomPanelController` + USS.
- Dispensa portata a paradigma “seleziona sorgente + bottone azione” con nuovi bottoni localizzati.

---

## 4. Tooling demo e stabilizzazione input/click

### Problema

- Mancanza di strumenti rapidi per QA demo beats.
- Regressione click multipli (prima apertura/chiusura Inventario e alcune interazioni macchina).

### Soluzione

- Creato `DemoDebugConsole` con:
  - toggle `ALT + D`,
  - jump checkpoint demo,
  - complete missione corrente,
  - reset mission flags demo.
- Registrazione console/director in `GamePlayInstaller`.
- Corretto layer di intercettazione input in `MainMenuUIToolkitController` (`pickingMode` root menu quando hidden/visible).
- Fallback pointer handling su pulsanti inventory/close in `PlayerStatusPanelController` e `PlayerInventoryPanelController`.
- Rimossa strumentazione temporanea a issue conclusa (incluso cleanup helper runtime logging precedente).

---

## 5. Camera runtime Vault: analisi + fix lens 5

### Problema

- Con lens impostata a 5 in editor, la camera non seguiva correttamente il player in Vault (comportamento diverso rispetto a modifica live in runtime).

### Soluzione

- Introdotto componente locale `VaultCameraRuntimeBootstrap` sulla `Virtual Camera` di `SCN_VaultMap`:
  - attende stato live su `CinemachineBrain`,
  - forza lens target (5),
  - invalida cache pipeline (`PreviousStateIsValid=false`),
  - invalida cache confiner (metodi compatibili versioni),
  - disattiva confiner runtime (`_disableConfinerAtRuntime`) per evitare lock/clamp che bloccava inquadratura.
- Approccio circoscritto alla scena Vault, senza workaround globali in installer.

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `.cursor/skills/sviluppa/SKILL.md` | aggiornamento skill interna |
| `Assets/Resources/Demo/DemoAlphaNarrativeConfig.asset` | testi VO finali e nuovi campi beat demo |
| `Assets/_Project/Resources/UI/UIToolkit/SeedStorage/SeedStoragePanel.uss` | stile OFF/disabled panel |
| `Assets/_Project/Resources/UI/UIToolkit/SeedStorage/SeedStoragePanel.uxml` | struttura/supporto stato panel aggiornato |
| `Assets/_Project/Scenes/SCN_VaultMap.unity` | wiring camera/lens e componenti runtime |
| `Assets/_Project/Scripts/Camera.meta` | nuovo folder script camera |
| `Assets/_Project/Scripts/Camera/VaultCameraRuntimeBootstrap.cs` | bootstrap camera runtime locale Vault |
| `Assets/_Project/Scripts/Camera/VaultCameraRuntimeBootstrap.cs.meta` | meta nuovo script |
| `Assets/_Project/Scripts/Core/DemoAlphaNarrativeConfig.cs` | nuovi campi config narrativa |
| `Assets/_Project/Scripts/Core/DemoAlphaNarrativeDefaults.cs` | fallback testi narrativa |
| `Assets/_Project/Scripts/Core/DemoStoryDirector.cs` | orchestrazione beat/VO/checkpoint demo |
| `Assets/_Project/Scripts/UI/UIToolkit/VoOverlay/VoOverlayController.cs` | supporto hold post-typing per gruppi VO timed |
| `Assets/_Project/Scripts/Core/GameplayUiModalLock.cs` | gestione HUD contestuale modali |
| `Assets/_Project/Scripts/Core/Installers/GamePlayInstaller.cs` | registrazione servizi/demo tooling |
| `Assets/_Project/Scripts/Core/Localization/LocalizationManager.cs` | nuove chiavi localizzazione UI |
| `Assets/_Project/Scripts/Core/MissionSystem/WardrobeMission.cs` | mission flow Seed Storage aggiornato |
| `Assets/_Project/Scripts/Core/UIBlocker.cs` | aggiustamenti gestione blocco input |
| `Assets/_Project/Scripts/Debug/DemoDebugConsole.cs` | nuova console debug demo |
| `Assets/_Project/Scripts/Debug/DemoDebugConsole.cs.meta` | meta nuovo script |
| `Assets/_Project/Scripts/Systems/SeedStorage/SeedStorageSystem.cs` | stato anomalia beat3 e reset sessione |
| `Assets/_Project/Scripts/UI/UIToolkit/DispensaRefrigerata/DispensaPanelController.cs` | transfer UX + stato OFF + selection flow |
| `Assets/_Project/Scripts/UI/UIToolkit/FoodRoom/FoodRoomPanelController.cs` | stato OFF sintetizzatore |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/ActiveMissionsPanelController.cs` | visibilità HUD contestuale + skip VO overlap |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/CompactBottomBarController.cs` | hook tooltip CRY/eventi |
| `Assets/_Project/Scripts/UI/UIToolkit/MainMenu/MainMenuUIToolkitController.cs` | fix picking mode/intercettazione click |
| `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/FoundationNotificationsPanelController.cs` | allineamento hide context HUD |
| `Assets/_Project/Scripts/UI/UIToolkit/PlayerInventory/PlayerInventoryPanelController.cs` | gestione HUD contestuale + close reliability |
| `Assets/_Project/Scripts/UI/UIToolkit/PlayerStatusPanelController.cs` | open inventory reliability/pointer fallback |
| `Assets/_Project/Scripts/UI/UIToolkit/SeedStorage/SeedStoragePanelController.cs` | offline visual state + gating interazioni |
| `Assets/_Project/UI/UIToolkit/DispensaRefrigerata/DispensaPanel.uss` | stili transfer button e OFF |
| `Assets/_Project/UI/UIToolkit/DispensaRefrigerata/DispensaPanel.uxml` | nuovi bottoni trasferimento |
| `Assets/_Project/UI/UIToolkit/FoodRoom/FoodRoomPanel.uss` | stili OFF/disabilitazione pannello |

---

## Regole / vincoli rispettati

- **Feature Both**: nessun fork demo/full per le funzionalità condivise; gating gestito via stato/sessione.
- **Runtime architecture**: integrazione mantenuta sul `ServiceContainer`; nessuna introduzione di scan runtime distruttivi nei fix principali.
- **UI Builder parity**: modifiche UXML/USS coerenti con comportamento runtime (stati dinamici in controller, stile base in USS).
- **Debug cleanup**: strumentazione runtime rimossa dopo conferma utente di issue risolta.

---

## Note operative (Unity)

- `SCN_VaultMap` usa camera bootstrap persistente (`CinemachineBrain` in Bootstrap/AppRoot): i test camera vanno eseguiti sempre nel flusso reale da menu/bootstrap.
- Per validare regressioni camera:
  1. movimento laterale estremo piano 0,
  2. passaggio ascensore piano +1/-1,
  3. verifica inquadratura continua player-centered.
- `DemoDebugConsole` è disponibile in `UNITY_EDITOR`/`DEVELOPMENT_BUILD`.

---

*Fine DEV REPORT 0103.*
