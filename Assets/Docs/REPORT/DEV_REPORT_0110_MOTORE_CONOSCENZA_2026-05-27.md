# DEV REPORT 0110 — Motore Conoscenza (Knowledge Engine v1)

**Data:** 2026-05-27  
**Sprint / contesto:** Core gameplay — progressione biologo (Conoscenza), prerequisito LAB 4.0 Schermata 1–2; separato da Demo Alpha gap map.  
**Riferimento piano:** `.cursor/plans/motore_conoscenza_05e47971.plan.md`, `.cursor/plans/laboratorio_visione_e_repo_a60fd394.plan.md` (tier → budget progetto 8–28)  
**Report precedente:** `DEV_REPORT_0109_PLANTCARD4V_USS_PARITA_UI_BUILDER_2026-05-05.md`

---

## Sommario interventi

1. Introdotto **`KnowledgeProgressionService`** con punteggio persistente, 6 tier (label IT/EN), `GetProjectBudgetBase()` per futura Progettazione seme, floor score a 0.
2. Collegati **guadagni** Conoscenza: sblocco wiki/ricerca (EOD → categorie Historical/Botanical/Vault), completamento progetto seme Lab, milestone **10/20/30/40/50** giorni `DaysConsecutiveOptimal` per vaso (+1 ciascuna).
3. Collegati **penalità**: abbandono progetto Lab, seme **instabile** (`GeneticType.Unstable`) al completamento.
4. **TopBar CONOSCENZA**: mostra solo **label tier** (es. NEOFITA), non più G-rate `+N`.
5. **Toast Foundation** `KNW-GAIN`, `KNW-LOSS`, `KNW-TIER-UP`, `KNW-TIER-DOWN` via `KnowledgeToastBridge` (richiede Foundation notifications attive in scena).
6. **Save/load** di `knowledgeTotalScore` e chiavi evento idempotenti; fix compilazione Core (no `Resources` / no ref `ToastNotificationManager` nel layer Knowledge).

---

## Statistiche e progresso

### Righe di codice

- **Nuovi file** `Assets/_Project/Scripts/Core/Knowledge/*.cs` (8 file): **533** righe — comando PowerShell `(Get-Content … | Measure-Object -Line).Lines` su cartella, 2026-05-27.
- **Hook su file esistenti** (TopBar, SaveManager, LabTerminal, DayCycle, EOD, WikiUnlock, Localization, NotificationTypeSpecDefaults, GamePlayInstaller): delta non isolato nel diff; **non misurato riga per riga** in questo report.

### Sistemi funzionanti

- **Compilazione C#:** errori `Resources` / `ToastNotificationManager` risolti (da validare ricompilazione Unity Editor).
- **Runtime gameplay:** **da validare in Editor** (nuova partita, EOD ricerca, Lab progetto, milestone vaso, save/load, toast con Foundation ON).

### Bug risolti

- **2** — errori compilazione in `KnowledgeProgressionService` (`Resources` non risolvibile nel contesto Core) e `KnowledgeToastBridge` (riferimento `Sporae.DevTools` / `ToastNotificationType`); risolti spostando `Resources.Load` in `GamePlayInstaller` e rimuovendo fallback legacy toast dal bridge Knowledge.

### Progresso gameplay / prodotto

- La **TopBar** mostra il **livello Conoscenza** del biologo (Neofita → Maestro), non un numero G-rate astratto.
- Sbloccare ricerca wiki, completare un progetto seme o curare un vaso a lungo **aumenta** la Conoscenza (con toast se Foundation è attivo); abbandono o seme instabile la **riduce**.
- Salire o scendere di **tier** genera un toast dedicato con label e budget progetto (8–28) nel messaggio.
- Il motore espone **`GetProjectBudgetBase()`** per la futura Schermata 2 LAB 4.0 (pool punti allocabili da tier).

---

## 1. Servizio Conoscenza e persistenza

### Problema

- Label **CONOSCENZA** in TopBar collegata a legacy `_grateValue` / `UpdateGrate(+N)` senza logica gameplay.
- Nessun servizio centralizzato per tier, budget progetto Lab, save, né hook su wiki / lab / vasi.

### Soluzione

- **`KnowledgeProgressionService`** registrato in `GamePlayInstaller` via `ServiceContainer`.
- Config: `KnowledgeProgressionConfig` (ScriptableObject) con tier, soglie score, budget 8–28, punti wiki/lab/vaso, penalità; caricamento opzionale da `Resources/Configs/KnowledgeProgressionConfig` in installer, altrimenti **default runtime** `CreateRuntimeDefaults()`.
- **Save:** `knowledgeTotalScore`, `knowledgeGrantedEventKeys` in `GameSaveData`; load senza toast (suppress notifiche).

**File interessati:**  
`KnowledgeProgressionService.cs`, `KnowledgeProgressionConfig.cs`, `KnowledgeTierInfo.cs`, `KnowledgeDeltaReason.cs`, `GamePlayInstaller.cs`, `SaveManager.cs`

### Tabella tier (bozza v1, tuning Editor)

| Tier | Label (chiave) | Soglia min score | Budget progetto base |
|------|----------------|------------------|----------------------|
| 1 | Neofita | 0 | 8 |
| 2 | Praticante | 8 | 12 |
| 3 | Ricercatore | 18 | 16 |
| 4 | Botanico | 32 | 20 |
| 5 | Senior | 50 | 24 |
| 6 | Maestro | 72 | 28 |

---

## 2. Fonti punti e penalità

### Problema

- Piano Lab originale prevedeva driver notturni/pot aggregate; design rivisto: **achievement-style** (wiki, lab, cura vaso) e penalità progetto.

### Soluzione

| Evento | Meccanismo | Punti default (config) |
|--------|------------|-------------------------|
| Sblocco wiki (nodo / ramo EOD) | `WikiUnlockService` → `WikiResearchKnowledgeBridge` | +3 per ramo (Historical, Botanical, Vault) |
| Progetto seme completato | `LabTerminalPanelController` → `SeedProjectKnowledgeHooks` | +6 (idempotente per `lab:complete:{projectKey}`) |
| Seme instabile a completamento | Stesso hook, `GeneticType.Unstable` | -3 (`lab:unstable:{key}`) |
| Abbandono progetto | Cancel progetto attivo | -4 |
| 10–50 gg ottimali consecutivi / vaso | `DayCycleController` → `PotCareKnowledgeWatcher` | +1 per soglia (una volta per vaso) |

**File interessati:**  
`WikiUnlockService.cs`, `WikiResearchKnowledgeBridge.cs`, `SeedProjectKnowledgeHooks.cs`, `PotCareKnowledgeWatcher.cs`, `LabTerminalPanelController.cs`, `SPOR-BLK-01-03A-DayCycleController.cs`

---

## 3. UI TopBar e toast

### Problema

- Giocatore vedeva `+12` sotto CONOSCENZA (residuo G-rate).
- Nessun feedback toast su variazione Conoscenza o cambio tier.

### Soluzione

- **`TopBarController`:** `RefreshKnowledgeTierLabel()` / `UpdateKnowledgeTierLabel(string)`; subscribe a `OnKnowledgeChanged`; placeholder UXML **NEOFITA**.
- **`KnowledgeToastBridge`:** toast `KNW-GAIN` / `KNW-LOSS` su ogni delta; `KNW-TIER-UP` / `KNW-TIER-DOWN` su cambio tier (dopo toast punti); suppress ~2s post-load.
- Spec e copy in `NotificationTypeSpecDefaults` + chiavi `LocalizationManager` (tier, `topbar.metric.knowledge`, `eod.dawn_cry_knowledge`).
- **EoD alba:** riga economia con tier Conoscenza al posto del solo G-rate numerico.

**Nota:** i toast KNW richiedono **`FoundationNotificationService.Enabled`** in scena; senza Foundation non c’è fallback legacy (vincolo assembly Core).

**File interessati:**  
`TopBarController.cs`, `TopBar.uxml`, `KnowledgeToastBridge.cs`, `NotificationTypeSpecDefaults.cs`, `LocalizationManager.cs`, `EndOfDaySequenceController.cs`

---

## 4. Fix compilazione layer Core

### Problema

- `Resources.Load` e `ToastNotificationManager` non compilavano nei script sotto `Core/Knowledge`.

### Soluzione

- `Resources.Load<KnowledgeProgressionConfig>` solo in **`GamePlayInstaller`** (MonoBehaviour / UnityEngine).
- `KnowledgeToastBridge` usa **solo** Foundation; nessun riferimento a `Sporae.DevTools`.
- Punti lab/vaso letti da proprietà del servizio (`LabProjectCompletePoints`, ecc.), non da static load.

**File interessati:**  
`KnowledgeProgressionService.cs`, `KnowledgeToastBridge.cs`, `SeedProjectKnowledgeHooks.cs`, `PotCareKnowledgeWatcher.cs`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/Core/Knowledge/KnowledgeProgressionService.cs` | Nuovo |
| `Assets/_Project/Scripts/Core/Knowledge/KnowledgeProgressionConfig.cs` | Nuovo |
| `Assets/_Project/Scripts/Core/Knowledge/KnowledgeTierInfo.cs` | Nuovo |
| `Assets/_Project/Scripts/Core/Knowledge/KnowledgeDeltaReason.cs` | Nuovo |
| `Assets/_Project/Scripts/Core/Knowledge/KnowledgeToastBridge.cs` | Nuovo |
| `Assets/_Project/Scripts/Core/Knowledge/WikiResearchKnowledgeBridge.cs` | Nuovo |
| `Assets/_Project/Scripts/Core/Knowledge/SeedProjectKnowledgeHooks.cs` | Nuovo |
| `Assets/_Project/Scripts/Core/Knowledge/PotCareKnowledgeWatcher.cs` | Nuovo |
| `Assets/_Project/Scripts/Core/WikiUnlockService.cs` | Eventi unlock + bridge Conoscenza |
| `Assets/_Project/Scripts/Core/Installers/GamePlayInstaller.cs` | Registrazione servizio + toast bridge + load config |
| `Assets/_Project/Scripts/Core/SaveManager.cs` | Campi save knowledge |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs` | Label tier, subscribe servizio |
| `Assets/_Project/UI/UIToolkit/HUD/TopBar.uxml` | Placeholder NEOFITA |
| `Assets/_Project/Scripts/UI/UIToolkit/Lab/LabTerminalPanelController.cs` | Hook complete/abandon/instabile |
| `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs` | Hook milestone vaso |
| `Assets/_Project/Scripts/UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs` | Alba: tier in copy CRY |
| `Assets/_Project/Scripts/Core/Localization/LocalizationManager.cs` | Chiavi tier / topbar / eod |
| `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/NotificationTypeSpecDefaults.cs` | Spec KNW-* |

---

## Regole / vincoli rispettati

- **ServiceContainer** per servizio globale; niente `FindObjectOfType` nel motore Knowledge.
- **Both / Principio 0:** stesso binario demo e full game; nessuna scena Lab parallela.
- **Distinzione assi:** Conoscenza (maturità biologo) separata da Mutation Index (TopBar).
- **UI Builder:** solo label tier in TopBar; numeri budget progetto ammessi nei toast tier, non in TopBar (come da piano Lab).
- **Idempotenza:** `knowledgeGrantedEventKeys` evita doppi accredito su stesso evento.

---

## Note operative (Unity)

1. Ricompilare e aprire **SCN_VaultMap** (o scena HUD con TopBar + `GamePlayInstaller`).
2. Verificare TopBar: **NEOFITA** a nuova partita.
3. **Foundation notifications:** abilitare su `GamePlayInstaller` se si vogliono toast KNW in Play.
4. **EOD:** scegliere un ramo ricerca → +3 Conoscenza (se non già sbloccato) + toast.
5. **Lab:** avviare e completare «Crea nuovo seme» → +6; con seme instabile anche -3; cancel progetto → -4.
6. **Vaso:** portare `DaysConsecutiveOptimal` a 10+ → +1 (toast se tier/punti cambiano).
7. **Save/load:** score e tier coerenti; nessun toast al caricamento (primi ~2s suppress).
8. Opzionale: creare asset `Resources/Configs/KnowledgeProgressionConfig.asset` (menu **Spore/Knowledge/Progression Config**) per tarare soglie e punti in Editor.

**Prossimo passo prodotto:** LAB 4.0 Schermata 1–2 che consumano `GetProjectBudgetBase()` e `GetTierLabelLocalized()` — fuori scope di questo report.

---

*Fine DEV REPORT 0110.*
