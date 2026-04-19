# DEV REPORT 0088 — Task 4: interazioni E, Mission Recap UI Toolkit, barra Azioni e fame (fix + polish)

**Data:** 2026-04-19  
**Sprint / contesto:** Demo Alpha / gameplay loop — Task 4 (interazioni, mission recap HUD, economia azioni giornaliere) e bugfix emersi in playtest (BlackMarket, tooltip missioni, barra azioni vs tooltip, cap 5/5, recupero fame).  
**Riferimento piano:** `demo_alpha_1_0_gap_map` (sezioni missioni, HUD, sopravvivenza / azioni)  
**Report precedente:** `DEV_REPORT_0087_TOAST_NUOVA_MISSIONE_VO_HIGHLIGHT_PAROLE_2026-04-19.md`

---

## Sommario interventi

1. **Interazione tasto E / prompt**: risoluzione target più stabile (incluso caso “BlackMarket” e competizione tra interactable); prompt dinamico con nome oggetto **solo** quando il player è **davvero** nel range visivo “prompt” (sotto-range rispetto all’interazione larga).
2. **Mission Recap (UI Toolkit)**: ripristino struttura UXML completa + USS CRT; eliminato `ScrollView` che causava scroll-jump su hover; tooltip HoverCard bindato e posizionato correttamente.
3. **Missione armadio / recap**: notifica flag missione all’apertura pannello armadio; `UIMission` legacy disattivato se presente `ActiveMissionsPanelController`.
4. **Barra Azioni (TopBar)**: allineamento **scala visiva fissa 5 segmenti** (`1/5` quando il cap runtime è 1) coerente col tooltip breakdown; guardia `max==0` in `SegmentedBarUI`.
5. **Fame / cap azioni (`GameManager`)**: nuova partita **5/5** (baseline colazione 5 + enforcement su valori serializzati vecchi); **recupero immediato** del cap quando si mangia in stato di penalità fame (senza attendere EndOfDay); valido anche per sessione **DEMO** (stesso `GameManager`).

---

## 1. Interazioni — tasto E, competizione tra oggetti, BlackMarket

### Problema
- Con più interactable in scena, il target tasto E poteva “rubare” focus ad altri oggetti.
- Il prompt poteva mostrare il nome oggetto anche quando il player era ancora troppo lontano visivamente, pur dentro un range di interazione più largo.

### Soluzione
- **`Interactable`**: distanza effettiva al player tramite `Collider2D.ClosestPoint` per `PlayerInRange`; introdotto **`PlayerInPromptRange`** basato su `_promptDistance` (clampato al range interazione).
- **Prompt**: `PlayerInteractAdvice` riceve il nome oggetto **solo** se il target prompt è `this` **e** `PlayerInPromptRange` è vero.
- **Keyboard resolve**: logica centralizzata per scegliere l’interactable “giusto” nello stato corrente (riduce ambiguità tra oggetti sovrapposti / registry).
- **`WardrobeStation`**: tuning distanza interazione e repeat mentre in range dove necessario per non “perdere” l’interazione sul guardaroba.

**File principali:** `Interactable.cs`, `PlayerInteractAdvice.cs`, `WardrobeStation.cs`

---

## 2. Mission Recap — UXML/USS, tooltip, scroll-jump

### Problema
- UXML ridotto/minimale: mancavano nodi attesi dal controller (`active-mission-tooltip`, breakdown, count, ecc.) → tooltip assente o non stilizzato.
- `ScrollView` su lista missioni: su hover UI Toolkit poteva **auto-scrollare** → sensazione di “scatto”/scroll indesiderato.

### Soluzione
- **`ActiveMissions.uxml`**: ricostruzione layout: header (title + count + chevron), content collassabile (empty + lista), tooltip come sibling (non dentro scroll), `picking-mode="Ignore"` su elementi non interattivi.
- **`ActiveMissions.uss`**: stile terminal/CRT allineato alla specifica (280px, posizione, colori stato, tooltip 320px, sezioni OBJECTIVE/TASK/REWARD/DEADLINE).
- **`ActiveMissionsPanelController`**: posizionamento tooltip semplificato (coordinate relative al root); rimossi fallback inline non necessari dopo ripristino USS; pulizia strumentazione debug temporanea.

**File principali:** `ActiveMissions.uxml`, `ActiveMissions.uss`, `ActiveMissionsPanelController.cs`

---

## 3. Missione iniziale armadio — stato missione e UI legacy

### Problema
- Missione assegnata ma recap non aggiornato / doppia UI possibile.

### Soluzione
- Hook missione armadio: `WardrobeMission.NotifyWardrobeAccessed()` idempotente; chiamata coerente con apertura UI armadio (`WardrobePanelController`).
- `UIMission` (legacy) disattivato se esiste `ActiveMissionsPanelController` (evita doppioni).

**File principali:** `WardrobeMission.cs`, `WardrobePanelController.cs`, `UIMission.cs`

---

## 4. Barra Azioni — mismatch barra vs tooltip (malnutrizione)

### Problema
- Tooltip corretto (es. `1/5` disponibili) ma barra segmentata mostrava **5/5 pieni** e colorazione incoerente: la barra normalizzava su `max` runtime (es. `max=1` → riempimento completo dei 5 segmenti).

### Soluzione
- **`TopBarController.UpdateActions`**: scala visiva fissa **`ActionsVisualSlots = 5`** per label e fill; soglie colore basate sul valore “visibile” clampato.
- **`SegmentedBarUI.UpdateValue`**: guardia `max > 0` per evitare edge cases.

**File principali:** `TopBarController.cs`, `SegmentedBarUI.cs`

---

## 5. Fame — nuova partita 5/5 e recupero immediato dopo pasto

### Problema
- Nuova partita poteva partire con cap serializzato a `4` (valore scena/prefab vecchio) invece di `5`.
- Dopo giorni senza cibo, mangiare ripristinava il cap solo dopo **EndOfDay** (UX errata rispetto al design atteso).

### Soluzione
- Default `_dailyActionsFromBreakfast = 5` + enforcement in `InitializeSystems()` (se `< 5` → `5`).
- `NotifySolidFoodConsumed()`: se il player è penalizzato (`MaxActions` sotto il breakfast base), reset immediato:
  - azzera streak fame rilevanti
  - `ActionSystem.ResetActions(rawBreakfast)`
  - `SeedActionBudgetLedgerForDawn(..., penaltySteps: 0, ...)`

**Nota DEMO:** la sessione demo usa gli stessi sistemi core (`GameManager`/`ActionSystem`); `DemoStoryDirector` non sostituisce questa logica.

**File principali:** `GameManager.cs`, `ItemConsumptionHandler.cs` (trigger pasto → `NotifySolidFoodConsumed`)

---

## File modificati (tabella)

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/Interactables/Interactable.cs` | Prompt range, distanza, resolve tasto E, nome display oggetto |
| `Assets/_Project/Scripts/Player/PlayerInteractAdvice.cs` | Prompt dinamico con nome target |
| `Assets/_Project/Scripts/Interactables/WardrobeStation.cs` | Distanza/repeat interazione armadio |
| `Assets/_Project/Scripts/Core/MissionSystem/WardrobeMission.cs` | Hook flag missione armadio (idempotente) |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/WardrobePanelController.cs` | Notifica accesso armadio / integrazione missione |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/ActiveMissionsPanelController.cs` | Mission recap UI Toolkit: bind, card, tooltip, cleanup debug |
| `Assets/_Project/Resources/UI/UIToolkit/ActiveMissions/ActiveMissions.uxml` | Ricostruzione layout (no ScrollView tooltip-safe) |
| `Assets/_Project/Resources/UI/UIToolkit/ActiveMissions/ActiveMissions.uss` | Stili CRT/terminal mission recap + tooltip |
| `Assets/_Project/Scripts/UI/VaultMap/UIMission.cs` | Disabilitazione legacy se ActiveMissions attivo |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs` | Barra azioni scala fissa 5 segmenti + colori coerenti |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/Components/SegmentedBarUI.cs` | Guard `max>0` nel fill segmenti |
| `Assets/_Project/Scripts/Core/GameManager.cs` | Baseline 5/5; recupero fame immediato su pasto in deficit |
| `Assets/_Project/Scripts/Core/ItemsSystem/ItemConsumptionHandler.cs` | Consumo cibo → notifica pasto solido |

---

## Regole / vincoli rispettati

- **ServiceContainer** per accesso a servizi UI/controller dove già previsto dal progetto.
- **UI Toolkit**: struttura UXML coerente con query `Q(...)` del controller; niente “fix” a sleep/timer come workaround UI.
- **Gameplay**: logica fame/azionabile centralizzata in `GameManager` + `ActionSystem` (eventi `OnActionsChanged` per HUD).

---

## Note operative (Unity)

- Verificare eventuali **scene/prefab** con `GameManager._dailyActionsFromBreakfast` ancora a 4 in Inspector: l’enforcement runtime lo porta a 5, ma per chiarezza designer conviene allineare anche l’asset serializzato.
- Mission recap: aprire `ActiveMissions` in UI Builder solo se necessario; la source of truth runtime è `Resources/.../ActiveMissions.uxml`.

---

*Fine DEV REPORT 0088.*
