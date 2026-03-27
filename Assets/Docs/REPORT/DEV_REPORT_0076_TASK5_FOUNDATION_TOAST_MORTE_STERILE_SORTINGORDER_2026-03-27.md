# DEV REPORT 0076 — Task 5 completato: Foundation-only toast system + morte/sterilità + bugfix sorting order
**Data:** 2026-03-27  
**Sprint:** Dome Lab 100 — Task 5 Chiusura del core Dome  
**Riferimento Piano:** `roadmap_dome_lab_100_069d5bdb.plan.md`  
**Report Precedente:** DEV_REPORT_0075

---

## Sommario Sessione

Sessione di implementazione e verifica completa del Task 5 del roadmap.

Obiettivo principale: consolidare Foundation come **unico canale di notifica** del gameplay, implementare la logica di **morte da condizione critica** e la logica di **sterilità `IsSterile`**, con relativi toast Foundation, rimuovendo tutti i fallback legacy (`_toastManager`, `_uiNotification`).

A fine sessione è stato inoltre identificato e risolto un **bug sistemico di visibilità**: tutti i toast postati durante `HandleDayChanged` erano nascosti dall'EOD overlay a causa di un sorting order insufficiente.

---

## Stato Task 5 (Finale)

| Step | Descrizione | Stato |
|---|---|---|
| 1 | Nuove spec Foundation: PLT-LVL-UP, PLT-LVL-DOWN, BURN-STAGE-REGRESS, MLD-INFESTED (con levelLost), MLD-INFESTED-CLEARED, STERILE-001, STERILE-CLEARED | ✅ Implementato |
| 2 | Campo `IsSterile` in `PotStateModel` con reset in costruttori/PlantSeed/ResetToEmpty | ✅ Implementato |
| 3 | Sterility tracking in `DayCycleController`: wiring IsSterile + toast Foundation ON/OFF | ✅ Implementato |
| 4 | Migrazione toast infestazione ON/OFF da `_toastManager` a Foundation (con PLT-LVL-DOWN condizionale) | ✅ Implementato |
| 5 | Toast Foundation per morte da condizione critica (`DaysCritical >= 3`) | ✅ Implementato |
| 6 | Pulizia `KillPlantFromExtremePh` e `ShowExtremePhCountdownNotification`: rimossi tutti i fallback legacy | ✅ Implementato |
| 7 | Toast Foundation per burn stage-regression e level-down | ✅ Implementato |
| 8 | Toast Foundation PLT-LVL-UP in `PotActions.cs` (Resting→Flowering con fertilizzante) | ✅ Implementato |
| 9 | Rimozione sistematica di tutti i fallback `_toastManager`/`_uiNotification` dagli eventi gestiti | ✅ Implementato |

**Conclusione:** Task 5 è completo e testato.

---

## Dettaglio Implementazioni

### 1) Nuove spec Foundation

**File:** `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/NotificationTypeSpecDefaults.cs`

Aggiunte le seguenti specifiche:

| ID | Tipo | Template |
|---|---|---|
| `PLT-LVL-UP` | Success | `⬆️ {potId}: {plantCode} salita a Livello {level}!` |
| `PLT-LVL-DOWN` | Warning | `⬇️ {potId}: {plantCode} perso Livello {oldLevel} → {newLevel} ({reason})` |
| `BURN-STAGE-REGRESS` | Danger | `🔥 {potId}: Regressione stadio {oldStage} → {newStage} (Burn Stress)` |
| `MLD-INFESTED` | Danger | `🚨 Infestazione muffe su {potId} — livello -{levelLost}` |
| `MLD-INFESTED-CLEARED` | Info | `✅ Infestazione rimossa su {potId}` |
| `STERILE-001` | Warning | `⚠️ {potId}: {plantCode} sterile (pH Ultra Basico)` |
| `STERILE-CLEARED` | Info | `✅ {potId}: {plantCode} non più sterile` |

---

### 2) Campo IsSterile in PotStateModel

**File:** `Assets/_Project/Scripts/Dome/PotStateModel.cs`

- Aggiunto `public bool IsSterile = false;` con tooltip.
- Reset esplicito in: costruttore base, `PlantSeed()`, `ResetToEmpty()`.

---

### 3) Sterility tracking in DayCycleController

**File:** `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

- Snapshot `bool preSterile = pot.IsSterile` prima del calcolo condizione.
- Check post-elaborazione: piante `Pure` in `PhBand.UltraBasic` → `pot.IsSterile = true`.
- Toast `STERILE-001` al cambio `false → true`, `STERILE-CLEARED` al cambio `true → false`.
- `preSterile != pot.IsSterile` incluso nel check `anyChanged` per trigger UI.
- `pot.IsSterile = false` inserito in tutti i path di morte.

---

### 4) Migrazione infestazione / Aggiunta burn / Aggiunta morte

Tutto in `DayCycleController`:

- Toast `MLD-INFESTED` (Foundation) con `levelLost` al posto del vecchio `_toastManager`.
- Toast `PLT-LVL-DOWN` condizionale se l'infestazione causa perdita livello.
- Toast `MLD-INFESTED-CLEARED` quando `pot.IsInfested` torna `false`.
- Toast `PLANT-DEATH-001` (Foundation) per morte da `DaysCritical >= 3`.
- Toast `BURN-STAGE-REGRESS` quando lo stage regredisce per burn stress.
- Toast `PLT-LVL-DOWN` (reason: "Burn Stress") quando il livello scende per burn.
- Rimossi fallback `_toastManager`/`_uiNotification` da: infestazione, morte, pH countdown, LED notifications, STAGE-UP-001, condizione migliorata/degradata, MOLD-GAIN/MOLD-REDUCE.

---

### 5) PLT-LVL-UP in PotActions

**File:** `Assets/_Project/Scripts/Dome/PotActions.cs`

- Aggiunto toast `PLT-LVL-UP` quando `PlantLevelSystem.CheckLevelUp` ritorna true (transizione Resting→Flowering con fertilizzante compatibile).

---

### 6) Fix CS8632 in BotanicalPowerFacade

**File:** `Assets/_Project/Scripts/Dome/PotSystem/Botanical/BotanicalPowerFacade.cs`

- Corretto warning nullable: `(char[]?)null` → `new char[0]` a riga 215.

---

## Bug Fix Post-Test: Foundation Sorting Order

### Sintomo
Toast postati durante `HandleDayChanged` (infestazione, burn, morte, sterilità) non visibili al player durante il test con il Bed.

### Root Cause (confermata con runtime evidence)
`FoundationNotificationsPanelController` aveva `sortingOrder = 60`.  
L'EOD overlay usa `sortingOrder = 2000`.  
I toast venivano postati mentre l'EOD era aperto → nascosti dietro l'overlay → scadevano prima che il player chiudesse la Dawn screen.

**Evidenza log runtime:**
```
{"null":false,"enabled":true,"levelLost":0}
```
Foundation era attiva e postava correttamente, ma non visibile per il sorting order.

### Fix
**File:** `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/FoundationNotificationsPanelController.cs`

```csharp
// Prima
_uiDocument.sortingOrder = 60;

// Dopo
_uiDocument.sortingOrder = 2100;
```

Foundation è ora sempre sopra qualsiasi overlay di gioco:
- EOD overlay: 2000 → Foundation 2100 visibile sopra (inclusa Dawn screen)
- FoodRoom: 1000 → Foundation visibile sopra
- PlantCard: 600 → Foundation visibile sopra

---

## Note su Problema B (Force Level Up Debug Console)

Confermato comportamento atteso: il debug console modifica `PlantLevel` direttamente, bypassando `PlantLevelSystem.CheckLevelUp`. Il toast `PLT-LVL-UP` non si attiva perché non passa dal path gameplay reale. Nessun fix necessario.

---

## Validazione Runtime

- Toast `MLD-INFESTED` confermato visibile sopra la Dawn screen dopo fix sorting order.
- Toast `STERILE-001` e `STERILE-CLEARED` confermati funzionanti (EndOfDay con pH UltraBasic su pianta Pure).
- Infestazione triggera correttamente dopo 2 giorni consecutivi a MoldRiskLevel 3 (confermato da log runtime: `days3:2, si:true`).
- Burn stress triggera correttamente dopo `maxDaysForFullStress` + 3 giorni consecutivi (confermato da log: `daysBurnAfter:3, willTrigger:true`, level 2→1 applicato).
- Nessuna strumentazione debug residua nei sorgenti a fine sessione.

---

## File Toccati in questa Sessione

- `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/NotificationTypeSpecDefaults.cs`
- `Assets/_Project/Scripts/Dome/PotStateModel.cs`
- `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`
- `Assets/_Project/Scripts/Dome/PotActions.cs`
- `Assets/_Project/Scripts/Dome/PotSystem/Botanical/BotanicalPowerFacade.cs`
- `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/FoundationNotificationsPanelController.cs`
- `Assets/Docs/REPORT/DEV_REPORT_0076_TASK5_FOUNDATION_TOAST_MORTE_STERILE_SORTINGORDER_2026-03-27.md`

---

## Chiusura

Task 5 è chiuso sia lato implementazione che lato verifica gameplay/UI runtime.  
Foundation è ora l'unico sistema di notifica attivo per tutti gli eventi critici del gameplay Dome.
