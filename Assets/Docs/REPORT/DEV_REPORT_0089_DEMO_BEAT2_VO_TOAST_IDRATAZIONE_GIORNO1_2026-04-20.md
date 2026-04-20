# DEV REPORT 0089 — Demo beat cucina, VO polish, toast idratazione/missione, split Giorno 1 demo vs full

**Data:** 2026-04-20  
**Sprint / contesto:** Demo Alpha — percorso early survival (beat 2 cucina), feedback UX su VO overlay, allineamento toast missioni al recap, feedback bere acqua.  
**Riferimento piano:** `demo_alpha_1_0_gap_map` (`DA10-T002`, `DA10-T003`, `DA10-T006`)  
**Report precedente:** `DEV_REPORT_0088_TASK4_INTERAZIONI_MISSION_RECAP_AZIONI_FAME_2026-04-19.md`

---

## Sommario interventi

1. **VO overlay (`VoOverlayController`)**: animazione ingresso/uscita (slide + fade ~0.5s), typewriter ~33 car/s, cursore lampeggiante, durata messaggio totale configurabile (~10s default), shake “CRT” sul blocco testo in idle senza reflow DOM che causa salti layout; hint di continuazione reso **implicito** (`SetContinueHint` senza testo visibile — click/Spazio/E restano attivi).
2. **Narrativa demo beat 2**: `DemoAlphaNarrativeConfig` esteso con campi **Beat 2 — Cucina** (testo VO, registro, modalità avanzamento frasi, highlight parole, colore highlight condiviso); `DemoStoryDirector.RunKitchenBreakfastBeat()` legge asset `Resources/Demo/DemoAlphaNarrativeConfig` con fallback a `DemoAlphaNarrativeDefaults`.
3. **Missione “Fai Colazione” (demo)**: classe `DemoBreakfastMission` — traccia consumo cibo solido + acqua; flag completamento su `MissionFlagTracker` solo dopo **chiusura inventario** con entrambe le condizioni soddisfatte; append missione da `DemoStoryDirector` dopo il VO cucina.
4. **Toast idratazione**: spec foundation `PLY-HYD-GAIN` + `PlayerStatToastBridge` mostra toast su guadagno H significativo (Δ ≥ 0.5%).
5. **Toast missione**: `ToastNotificationType.Mission` con colore `#00FFC6` (allineato al mission recap); `ToastNotificationManager.ShowMission`; `ActiveMissionsPanelController` usa `ShowMission` per `MIS-NEW` / `MIS-DONE`.
6. **Gameplay demo Giorno 1 vs full (`GameManager`)**: flag `_demoTutorialDayActive` — in sessione demo, Giorno 1 parte con **1/5** azioni e boost immediato a **5** dopo pasto; dal **Giorno 2** si disattiva il flag e `_dailyActionsFromBreakfast = 5` con stessa logica fame/disidratazione del full game.

---

## 1. VO overlay — timing, animazioni, stabilità layout

### Problema
- Servivano ingresso/uscita più leggibili e tempi coerenti con lettura.
- Il layout “organico” post-typing poteva causare **salti** visivi indesiderati.

### Soluzione
- Parametri serializzati: `_defaultCharsPerSecond` (~33), `_totalMessageDuration` (~10s), `_enterExitDuration` (0.5s).
- Idle: shake sul wrapper testo senza cambiare struttura DOM durante la fase stabile.
- Continuazione a fine blocco con `ForceContinueAtEnd`: nessuna etichetta hint visibile; input mouse/Spazio/E invariato.

**File principali:** `VoOverlayController.cs`, `VoOverlay.uxml`, `VoOverlay.uss`

---

## 2. Beat 2 cucina — dati e Director

### Problema
- Testo e parametri VO del secondo beat non erano configurabili come asset unico; il Director non leggeva beat 2 da config.

### Soluzione
- Estensione `DemoAlphaNarrativeConfig` con sezione Beat 2 (linea, registro, `VoSentenceAdvanceMode`, lista highlight).
- `RunKitchenBreakfastBeat()` costruisce `VoLinePresentationOptions` da config/defaults, poi `ShowLine` e infine append missione colazione + `DemoBreakfastMission.BeginTracking`.

**File principali:** `DemoAlphaNarrativeConfig.cs`, `DemoAlphaNarrativeDefaults.cs`, `Assets/Resources/Demo/DemoAlphaNarrativeConfig.asset`, `DemoStoryDirector.cs`

---

## 3. Missione colazione demo — tracking inventario

### Problema
- Serve completamento coerente con UX “mangia e bevi” senza chiudere la missione al solo consumo parziale.

### Soluzione
- Eventi `Inventory.OnItemConsumed` per segnare cibo vs acqua; `PlayerInventoryPanelController.OnClosed` per completare e settare flag solo se entrambi veri.

**File principali:** `DemoBreakfastMission.cs`, `MissionConfig` / goal in `Assets/_Project/Resources/Missions/` (`M_Demo_Breakfast`, `Goal_Demo_Breakfast`)

---

## 4. Toast — idratazione e missioni

### Problema
- Mancava feedback toast esplicito sul bere; i toast missione non erano distinti cromaticamente dal recap HUD.

### Soluzione
- `PLY-HYD-GAIN` in `NotificationTypeSpecDefaults`; bridge idratazione con soglia Δ.
- Nuovo tipo toast `Mission` + `COLOR_MISSION` e API `ShowMission`.

**File principali:** `NotificationTypeSpecDefaults.cs`, `PlayerStatToastBridge.cs`, `ToastNotificationType.cs`, `ToastNotificationConfig.cs`, `ToastNotificationManager.cs`, `ActiveMissionsPanelController.cs`

---

## 5. Demo — Giorno 1 tutorial azioni vs Giorno 2+

### Problema
- Allineare il piano: tutorial primo giorno con 1/5 e transizione al modello “full” senza fork di scena.

### Soluzione
- `InitializeSystems`: se `DemoSessionState.IsDemo` → `_demoTutorialDayActive = true`, `_dailyActionsFromBreakfast = 1`, H iniziale 75% (già presente).
- `NotifySolidFoodConsumed`: con flag attivo, reset immediato azioni a 5 e voce ledger “Pasto (bonus demo)”.
- `HandleDayChanged`: a `day >= 2` → flag off, breakfast 5.

**File principali:** `GameManager.cs`

---

## File modificati (tabella)

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/UI/UIToolkit/VoOverlay/VoOverlayController.cs` | Timing, enter/exit, shake idle, hint implicito |
| `Assets/_Project/Resources/UI/UIToolkit/VoOverlay/VoOverlay.uxml` | Layout VO (se applicabile alla iterazione) |
| `Assets/_Project/Resources/UI/UIToolkit/VoOverlay/VoOverlay.uss` | Stili VO (se applicabile) |
| `Assets/_Project/Scripts/Core/DemoAlphaNarrativeConfig.cs` | Campi Beat 2 cucina |
| `Assets/_Project/Scripts/Core/DemoAlphaNarrativeDefaults.cs` | Default beat 2 |
| `Assets/Resources/Demo/DemoAlphaNarrativeConfig.asset` | Dati authoring beat 2 |
| `Assets/_Project/Scripts/Core/DemoStoryDirector.cs` | `RunKitchenBreakfastBeat`, append missione + tracking |
| `Assets/_Project/Scripts/Core/MissionSystem/DemoBreakfastMission.cs` | Nuovo — tracking colazione demo |
| `Assets/_Project/Resources/Missions/M_Demo_Breakfast.asset` | Asset missione (demo) |
| `Assets/_Project/Resources/Missions/Goal_Demo_Breakfast.asset` | Goal missione (demo) |
| `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/NotificationTypeSpecDefaults.cs` | `PLY-HYD-GAIN` |
| `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/PlayerStatToastBridge.cs` | Toast guadagno H |
| `Assets/_Project/Scripts/DevTools/Notification/ToastNotificationType.cs` | Tipo `Mission` |
| `Assets/_Project/Scripts/DevTools/Notification/ToastNotificationConfig.cs` | `COLOR_MISSION` |
| `Assets/_Project/Scripts/DevTools/Notification/ToastNotificationManager.cs` | `ShowMission` |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/ActiveMissionsPanelController.cs` | `ShowMission` per nuova/completata |
| `Assets/_Project/Scripts/Core/GameManager.cs` | `_demoTutorialDayActive`, demo day 1/2+ |

---

## Regole / vincoli rispettati

- **Nessun fork scena**: stesso `GameManager` e `DemoStoryDirector` sulla sessione demo.
- **ServiceContainer** per risoluzione `VoOverlayController`, `MissionManager`, `GameManager`, `MissionFlagTracker`, `PlayerInventoryPanelController` dove già previsto.
- **UI Toolkit / Foundation**: toast missione e spec notifiche allineate al sistema esistente.

---

## Note operative (Unity / QA)

- **Play demo**: Giorno 1 — verificare 1/5, VO cucina dopo ingresso kitchen post-armadio, missione dopo VO, chiusura inventario dopo mangiare + bere; toast H al bere.
- **Giorno 2+ (demo)**: dopo cambio giorno, breakfast 5 e niente boost “Pasto (bonus demo)” — regressione vs full game.
- **Config**: `DemoAlphaNarrativeConfig` in `Resources/Demo/` per edit testi beat 2 senza rebuild codice.

---

*Fine DEV REPORT 0089.*
