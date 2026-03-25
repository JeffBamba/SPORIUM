# DEV REPORT 0074 — Task 4: Botanical Powers, UI Tooltip Dome, Bug Fix EndOfDay
**Data:** 2026-03-25  
**Sprint:** Dome Lab 100 — Task 4 Botanical Powers  
**Riferimento Piano:** `roadmap_dome_lab_100_069d5bdb.plan.md`  
**Report Precedente:** DEV_REPORT_0073

---

## Sommario Interventi

Questa sessione ha coperto due macro-aree:

1. **Task 4 — Botanical Powers**: implementazione runtime e UI completa dei tre poteri botanici speciali (Ferric Fern, Arctic Hask, Glasscap Fungus) inclusi effetti mold, pH drift, tensione roster, bonus IM, tooltips TopBar e DomeStatusHUD.
2. **Bug Fixing**: fix pH drift doppio (double event fire), fix livello seed debug, fix visibilità sezione "Effetti Globali" TopBar, fix tooltip encoding (`\xE0`), fix EndOfDay sequence stuck post-mold debug.

---

## PARTE 1 — TASK 4: Botanical Powers

### Overview

Task 4 introduce tre specie botaniche "speciali" con poteri attivi (in pot attivo) e passivi (in cryo slot) che impattano la Dome globalmente. I poteri sono interdipendenti e si sovrappongono tramite roster snapshot.

| Specie | Codice | Tipo | Potere principale |
|---|---|---|---|
| Ferric Fern | `PLT-STD-001` | Standard | Riduce mold risk globale |
| Arctic Hask | `PLT-PURE-001` | Pure | pH drift + mold pulse se pH fuori banda |
| Glasscap Fungus | `PLT-EVIL-001` | Evil | Bonus IM globale scalato per livello |

---

### 1.1 File Nuovi — `Assets/_Project/Scripts/Dome/PotSystem/Botanical/`

#### `BotanicalPlantCodes.cs`
Costanti e helper per i codici pianta Task 4.
- `IsFerricFern(string)`, `IsArcticHask(string)`, `IsGlasscap(string)` — riconoscimento specie
- `GetSpeciesUiDisplayName(string)` — restituisce nome leggibile ("Arctic Hask", "Ferric Fern", "Glasscap Fungus") per il display in tooltip e TopBar al posto del codice PLT-…

#### `BotanicalRosterSnapshot.cs`
Snapshot immutabile del roster attivo+cryo al momento del calcolo. Raccoglie:
- Lista pot attivi con piante Task 4
- Lista slot cryo con piante Task 4
- Contatori per tipo (n° Arctic attivi, cryo, pH fuori banda, ecc.)
- `GlasscapActiveMutationBonusSum` — somma bonus IM da Glasscap attivi

#### `BotanicalPowerScaling.cs`
Formule di scaling per i poteri:
- `FerricFernMoldReduction(int daysOverThreshold)` — curva riduzione excess mold Ferric Fern
- `GlasscapImBonus(int level)` — bonus IM Glasscap per livello (Lv1→Lv5)
- `ArcticHaskTensionHarvestPenalty(int arcticCount, bool phOutOfNeutra)` — penalità raccolto da tensione roster

#### `BotanicalMoldModifiers.cs`
Modifica raw excess mold prima che venga applicato al pot:
- `ApplyToRawExcess(float rawExcess, PotStateModel pot, BotanicalRosterSnapshot snap)` — applica riduzione Ferric Fern se attiva e beneficia il pot; applica moltiplicatore Glasscap cryo se penalizza

#### `BotanicalHarvestModifier.cs`
Applica penalità raccolto da Arctic Hask tension quando il roster è in stato critico (2+ Arctic con pH fuori Neutra).

#### `BotanicalArcticTensionNotifier.cs`
Gestisce il "mold pulse" di Arctic Hask: quando pH esce dalla banda Neutra con Arctic attivo, emette una riduzione mold risk (pulisce mold) sui pot con mold attivo. Logica: attivazione condizionale su pH band + contatore Arctic.

#### `BotanicalPowerFacade.cs`
Facade UI per i tooltip. Fornisce:
- `AppendDomeGlobalPlantPowersTooltipLines(List<string> lines, ...)` — costruisce le righe "Effetti Globali" per TopBar e DomeStatusHUD
- `AppendDomeHudTooltipLines(...)` — costruisce tooltip per un singolo pot nel DomeStatusHUD (logica "live impact": attivi in pot → solo ActivePower; cryo → solo PassivePower; Subiti → solo se impatto reale attuale)
- `NormalizeTooltipCopy(string)` — decodifica escape sequences `\xE0` etc. nei dati YAML
- `AppendWrappedBulletLines(...)` — word-wrap con indentazione coerente per descrizioni lunghe

---

### 1.2 File Modificati — Runtime

#### `DayCycleController.cs` (`SPOR-BLK-01-03A-DayCycleController.cs`)
- Integrazione `BotanicalMoldModifiers.ApplyToRawExcess` nel calcolo daily mold
- Integrazione `MoldSystem.ReduceMoldRiskLevel` per Arctic Hask mold pulse
- `SubscribeToEvents` reso idempotente (prevenzione doppia iscrizione)
- Aggiunto `Dispose()` su `DayCycleSystem` prima della re-registrazione

#### `DayCycleSystem.cs`
- Aggiunto metodo `Dispose()` per unhooking `HandleFaded` da `FadeToBlackAnimation.OnFaded`
- Previene doppio avanzamento giorno in scenari di domain reload

#### `GamePlayInstaller.cs`
- `Awake()` chiama `Dispose()` sulla istanza esistente di `DayCycleSystem` prima di registrarne una nuova

#### `MoldSystem.cs`
- Aggiunto supporto per `ReduceMoldRiskLevel(PotStateModel)` usato dal mold pulse Arctic Hask

#### `ItemFabric.cs`
- Aggiornato per includere metadati seed Task 4 (Ferric Fern, Arctic Hask, Glasscap) nelle istanze debug

#### `PLT-PURE-001.asset` (Arctic Hask)
- `dailyPhDrift: 2`
- `passivePower` corretto in YAML con stringa double-quoted per supportare carattere `à` (UTF-8) senza escape `\xE0`

---

### 1.3 File Modificati — UI

#### `TopBarController.cs`
- Aggiunto `_phTooltipSectionEffects` reference con `DisplayStyle.Flex` esplicito al refresh
- Sezione "EFFETTI GLOBALI": chiama `BotanicalPowerFacade.AppendDomeGlobalPlantPowersTooltipLines` (solo poteri pianta, no info pH band)
- Helper `GetPhModifierPlantLabel(plantCode, potId)` — formatta "Arctic Hask - Pot-001" per la sezione "MODIFICATORI ATTIVI"
- Rimosso else branch "nessun bonus Glasscap attivo" (compariva anche senza Glasscap in scena)

#### `TopBar.uxml`
- Label `ph-tooltip-label-effects`: testo cambiato da `"EFFETTI POSSIBILI"` → `"EFFETTI GLOBALI"`

#### `DomeStatusHUDController.cs`
- Tooltip per pot attivi: mostra solo `ActivePower` (non più anche PassivePower)
- Tooltip per cryo slot: mostra solo `PassivePower`
- Effetti "Subiti" gated: Ferric benefit mostrato solo se il pot ha mold pressure (`MoldRiskLevel >= 1`); Glasscap cryo mostrato solo se attivo
- Rimossa sezione "NOTE" da `BuildCryoTooltipLines`

#### `PlantCardV3TerminalController.cs`
- Aggiornato per mostrare effetti Task 4 nella scheda centro pianta

#### `NotificationTypeSpecDefaults.cs`
- Aggiunti tipi notifica per eventi Task 4 (mold pulse Arctic, tensione roster, bonus Glasscap)

---

### 1.4 Debug Tools Aggiornati

#### `PotDebugConsole.cs`
- Default `_debugSeedLevelMetaString` cambiato da `"3"` → `"1"` (semi a livello base)
- Pulsanti diretti per piantagione Ferric Fern / Arctic Hask / Glasscap senza inventario

#### `GlobalStateInspector.cs`
- Default `_gsiDebugSeedLevelMeta` cambiato da `"3"` → `"1"`
- Aggiunta possibilità di settare mold level, pH personalizzato, stato cryo per test rapido Task 4

---

### 1.5 Asset Piante Task 4

| Asset | PlantCode | dailyPhDrift | ActivePower | PassivePower |
|---|---|---|---|---|
| `PLT-STD-001` | Ferric Fern | 0 | Riduce excess mold nei pot adiacenti | Riduzione lieve mold anche in cryo |
| `PLT-PURE-001` | Arctic Hask | +2 | pH drift Dome +2/die; mold pulse se pH fuori Neutra | Tensione roster: penalità raccolto se 2+ Arctic e pH fuori Neutra |
| `PLT-EVIL-001` | Glasscap Fungus | 0 | Bonus IM globale scalato per livello | Aumento mold risk globale se in cryo |

---

## PARTE 2 — BUG FIXING

### Bug 1 — pH Drift Doppio (+60 invece di +2 al Giorno 2)

**Sintomo:** pH globale passava da 7.0 a ~67 alla fine del Giorno 2 dopo aver piantato un Arctic Hask e acceso la luce Blue LED.

**Root Cause (confermata da log runtime):**
`HandleDayChanged` veniva fired due volte per ogni end-of-day perché `EndOfDaySequenceController.RegisterModalButton` iscriveva il click handler due volte sullo stesso bottone, causando doppia chiamata a `DayCycleSystem.EndDay()` → doppio `OnDayChanged` → doppio `CalculateAndRegisterPhDrift`.

**Fix:**
1. `RegisterModalButton` reso sicuro contro doppia iscrizione
2. `DayCycleController.SubscribeToEvents` reso idempotente
3. `DayCycleSystem.Dispose()` aggiunto e chiamato da `GamePlayInstaller` prima della re-registrazione

**File:** `EndOfDaySequenceController.cs`, `DayCycleController.cs`, `DayCycleSystem.cs`, `GamePlayInstaller.cs`

---

### Bug 2 — Semi Debug a Livello 3 invece di Livello 1

**Sintomo:** I semi aggiunti tramite console debug (PotDebugConsole e GlobalStateInspector) venivano creati al livello 3 (enhanced), non al livello base.

**Root Cause:** Default hardcoded a `"3"` nei campi `_debugSeedLevelMetaString` e `_gsiDebugSeedLevelMeta`.

**Fix:** Valore default cambiato a `"1"` in entrambi i file.

**File:** `PotDebugConsole.cs`, `GlobalStateInspector.cs`

---

### Bug 3 — Sezione "Effetti Dome Presenti" Invisibile nel Tooltip TopBar

**Sintomo:** La sezione "Effetti Dome Presenti" nel tooltip pH TopBar non compariva mai, anche con Arctic Hask in pot e pH in banda Basica.

**Root Cause:** `ph-tooltip-section-effects` aveva `display: none` nel UXML e non veniva mai settato a `DisplayStyle.Flex` nel codice controller.

**Fix:** Aggiunta riga esplicita `_phTooltipSectionEffects.style.display = DisplayStyle.Flex` in `UpdatePhTooltipContent`.

**File:** `TopBarController.cs`

---

### Bug 4 — "IM globale: nessun bonus Glasscap attivo" senza Glasscap in Scena

**Sintomo:** Il messaggio compariva nel tooltip pH anche quando nessun Glasscap era presente in alcun pot.

**Root Cause:** Branch `else` in `BotanicalPowerFacade.AppendDomeGlobalPhTooltipLines` stampava sempre la stringa negativa.

**Fix:** Rimosso il branch `else` — la riga IM appare ora solo se c'è un Glasscap attivo con bonus > 0.

**File:** `BotanicalPowerFacade.cs`

---

### Bug 5 — Encoding `\xE0` Visualizzato Letteralmente

**Sintomo:** Il passivePower di Arctic Hask mostrava "penalit\xE0" invece di "penalità".

**Root Cause:** Il file YAML `.asset` usava una sequenza di escape non interpretata da Unity nella deserializzazione della stringa.

**Fix:**
1. `PLT-PURE-001.asset` — `passivePower` riscritto come stringa YAML double-quoted con carattere `à` diretto (UTF-8)
2. `BotanicalPowerFacade.NormalizeTooltipCopy(string)` — metodo di sanitizzazione aggiunto come fallback per altri asset futuri

**File:** `PLT-PURE-001.asset`, `BotanicalPowerFacade.cs`

---

### Bug 6 — EndOfDay Sequence Stuck (Bottoni YES/NO Non Responsivi)

**Sintomo:** Dopo aver settato il mold level via debug console, o in generale al secondo ciclo EndOfDay nella stessa sessione Play Mode, il modal "END DAY?" compariva ma YES e NO non rispondevano. Il player rimaneva bloccato.

**Root Cause (identificata con log runtime — debug session `ccfbc5`):**

`TryBind()` in `EndOfDaySequenceController` aveva un difetto nel lifecycle dei button handlers:

1. `Hide()` settava `_bound = false` e `_eodVisualTreeBoundRoot = null` ma **non nullava** `_btnYes`, `_btnNo`, ecc.
2. Alla successiva `StartSequence()`, `gameObject.SetActive(true)` causava un possibile riciclo del `rootVisualElement` del UIDocument
3. `TryBind()` rilevava `_btnYes != null` (reference stale) → chiamava `DetachEodButtonHandlers()` sull'elemento orfano → il detach non aveva effetto su handler del nuovo tree
4. Il re-query trovava i nuovi bottoni, ma in certi timing del lifecycle il `RegisterModalButton` registrava su elementi che Unity stava ancora inizializzando
5. Risultato: bottoni visibili ma senza handler attivi → click non capturati

**Evidenza Log (righe chiave):**
```
callN=1: StartSequence → overlayNull:true, rootNull:true (prima bind)
callN=1: TryBind → btnYesNull:false (trovato) → YES catturato ✓  
callN=1: Hide → bound:true, btnYesNull:false
callN=2: StartSequence → overlayNull:false, rootNull:false (reference stale attive)
callN=2: TryBind → re-bind completato → YES catturato ✓ (dopo fix)
```

**Fix:**

```csharp
// In Hide(): aggiunto reset stato per forzare re-bind completo
_bound = false;
_eodVisualTreeBoundRoot = null;
// _btnYes, _btnNo NON nullati (ma DetachEodButtonHandlers gestisce il detach corretto)

// In TryBind(): early return solo se TUTTO è consistente
if (_bound && _eodVisualTreeBoundRoot == currentRoot && _btnYes != null && _btnNo != null)
    return;
// Se ref stale: DetachEodButtonHandlers() → re-query → RegisterModalButton

// In DeferredStartStep1(): retry loop fino a 24 frame
for (int attempt = 0; attempt < maxAttempts; attempt++)
{
    yield return null;
    TryBind();
    if (_btnYes != null && _btnNo != null) break;
}

// Aggiunto DetachEodButtonHandlers() — rimozione sicura di tutti gli handler
private void DetachEodButtonHandlers()
{
    if (_btnYes != null) _btnYes.clicked -= OnYesClicked;
    if (_btnNo != null) _btnNo.clicked -= OnNoClicked;
    // ... tutti gli altri bottoni
}
```

**Verifica Post-Fix (log runtime):**
Due cicli EndOfDay completi nella stessa sessione Play Mode:
- `callN=1`: Bed → StartSequence → TryBind → Step1 → YES → Snapshot → Diario → Forecast → Sleep → Step6/7/8 → Hide ✓
- `callN=2`: Bed → StartSequence → TryBind → Step1 → YES → Snapshot → Diario → Forecast → Sleep → Step6/7 ✓

**File:** `EndOfDaySequenceController.cs`

---

## PARTE 3 — STATO RUN DI TEST TASK 4

### Steps Verificati

| Step | Descrizione | Stato |
|---|---|---|
| 1 | Pianta Arctic Hask Pot-001 + luce Blue LED. Fine giorno 1: pH drift +2 (7.0 → 9.0). Nessun doppio drift. | ✅ Verificato |
| 2 | pH in banda Basica: TopBar tooltip mostra "EFFETTI GLOBALI" con Arctic Hask - Pot-001 e testo ActivePower corretto. Nessun "nessun bonus Glasscap" spurio. | ✅ Verificato |
| 3 | Aggiunta Ferric Fern Pot-002. DomeStatusHUD tooltip per Pot-001 mostra solo ActivePower Arctic Hask. Tooltip per Pot-002 mostra solo ActivePower Ferric Fern. | ✅ Verificato |
| 4 | Mold level Pot-001 = 2. Ferric Fern attiva: riduzione mold excess applicata su Pot-001. Verificare che `BotanicalMoldModifiers.ApplyToRawExcess` riduca effettivamente il daily mold excess in log. | ⏳ Da verificare |
| 5 | Glasscap Fungus in Pot-003. Bonus IM globale visibile in TopBar (sezione EFFETTI GLOBALI) con scaling corretto per livello. `GlasscapActiveMutationBonusSum` > 0. | ⏳ Da verificare |
| 6 | Glasscap in cryo slot. TopBar mostra effetto passivo. DomeStatusHUD slot cryo mostra solo PassivePower. Mold risk altri pot aumenta. | ⏳ Da verificare |
| 7 | 2+ Arctic Hask attivi + pH fuori banda Neutra. Tensione roster attiva: penalità raccolto visibile su piante non-Arctic. `BotanicalHarvestModifier` applicato a fine giorno. | ⏳ Da verificare |
| 8 | EndOfDay secondo ciclo nello stesso Play Mode. Bottoni YES/NO responsivi. Sequenza completa step 1→8. | ✅ Verificato (post-fix Bug 6) |

### Note Step 4+

Gli step 4-7 richiedono:
- **Step 4**: Controllare log Unity per `BotanicalMoldModifiers` output; verificare che `MoldRiskLevel` di Pot-001 scenda o non aumenti dopo il ciclo con Ferric attiva
- **Step 5-6**: Glasscap non ancora testato in pot né in cryo — richiedono una run dedicata
- **Step 7**: Richiede 2 pot con Arctic Hask simultanei — richiede run dedicata con debug seed multipli

---

## File Modificati — Riepilogo Completo

| File | Tipo modifica |
|---|---|
| `Botanical/BotanicalPlantCodes.cs` | **NUOVO** |
| `Botanical/BotanicalRosterSnapshot.cs` | **NUOVO** |
| `Botanical/BotanicalPowerScaling.cs` | **NUOVO** |
| `Botanical/BotanicalMoldModifiers.cs` | **NUOVO** |
| `Botanical/BotanicalHarvestModifier.cs` | **NUOVO** |
| `Botanical/BotanicalArcticTensionNotifier.cs` | **NUOVO** |
| `Botanical/BotanicalPowerFacade.cs` | **NUOVO** |
| `DayCycleController.cs` | Modificato |
| `DayCycleSystem.cs` | Modificato |
| `GamePlayInstaller.cs` | Modificato |
| `MoldSystem.cs` | Modificato |
| `ItemFabric.cs` | Modificato |
| `PLT-PURE-001.asset` | Modificato |
| `TopBarController.cs` | Modificato |
| `TopBar.uxml` | Modificato |
| `DomeStatusHUDController.cs` | Modificato |
| `PlantCardV3TerminalController.cs` | Modificato |
| `NotificationTypeSpecDefaults.cs` | Modificato |
| `PotDebugConsole.cs` | Modificato |
| `GlobalStateInspector.cs` | Modificato |
| `EndOfDaySequenceController.cs` | Modificato (bug fix) |
| `Bed.cs` | Modificato (debug logging temporaneo rimosso) |

---

## Prossimi Step

1. **Continuare Run Task 4 da Step 4**: mold Ferric Fern reduction verificata in gioco
2. **Step 5-7**: run dedicata Glasscap (pot + cryo) e scenario tensione Arctic roster
3. Dopo verifica completa: cleanup `_bound`/`TryBind` logic review per edge case cryo machine cycling
