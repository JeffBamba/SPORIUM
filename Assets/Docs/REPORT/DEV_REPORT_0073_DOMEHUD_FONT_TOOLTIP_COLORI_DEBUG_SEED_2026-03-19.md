# DEV REPORT 0073 — DomeStatusHUD: Font, Tooltip Colorato, Debug Seed Impianto
**Data:** 2026-03-19  
**Sprint:** Dome Lab 100 — Post Task 3  
**Riferimento Piano:** `roadmap_dome_lab_100_069d5bdb.plan.md`  
**Report Precedente:** DEV_REPORT_0072

---

## Sommario Interventi

Due aree di miglioramento distinte, entrambe relative alla qualità operativa durante il development della Dome:

1. **PotDebugConsole** — aggiunto impianto diretto di semi con metadati reali (senza inventario)
2. **DomeStatusHUD** — font sizes allineati alle Toast Foundation, tooltip riscritto con colori per valore

---

## 1. PotDebugConsole — Impianto Debug Seed

### Problema
All'avvio di una nuova run non è disponibile alcun seme nell'inventario del player. Era necessario un meccanismo di debug per piantare rapidamente una pianta con metadati completi nel pot selezionato, senza passare per l'inventario o l'economy.

### Soluzione
**File modificato:** `Assets/_Project/Scripts/Debug/PotDebugConsole.cs`

#### Modifiche

**Nuova sezione GUI "Debug: Impianta Seme (senza inventario)"**  
Appare nel pannello del POT selezionato, subito dopo il blocco info corrente. Contiene 3 pulsanti affiancati:

| Pulsante | PlantCode |
|---|---|
| Ferric Fern | `PLT-STD-001` |
| Arctic Hask | `PLT-PURE-001` |
| Glasscap Fungus | `PLT-EVIL-001` |

Se il pot è già occupato viene mostrato un avviso testuale nel log; se è vuoto l'impianto procede.

**Nuovo metodo `DebugPlantSeed(string plantCode)`:**
- Risolve `PlantData` da `PlantDatabase.Instance.GetPlantDataByCode(plantCode)`
- Ottiene il giorno corrente da `DayCycleSystem.CurrentDay`
- Chiama `potState.PlantSeed(currentDay, plantCode)` per inizializzare lo stato a `Seed`
- Chiama `potState.ApplySeedMetadata(null, plantData)` per popolare tutti i metadati reali: `GeneticType`, `Family`, `SourcePlantCode`, ecc. — esattamente come farebbe una piantagione da inventario
- Aggiorna i visual del vaso via `PotGrowthController.UpdateVisuals()`
- Emette `PotEvents.EmitPlantStageChanged` e `PotEvents.EmitChanged` per notificare HUD, DomeStatusHUD, terminal

**Nuova cache `_dayCycleSystem`:**
- Aggiunto campo `private DayCycleSystem _dayCycleSystem`
- Risolto in `LoadConfigs()` via `ServiceContainer`

**Vincoli rispettati:**
- Nessun seme consumato dall'inventario
- Nessun Action Point consumato
- Nessun placeholder: i metadati sono identici a quelli di una piantagione reale
- Segue la regola architetturale: nessun `FindObjectOfType`, risoluzione via `ServiceContainer`

---

## 2. DomeStatusHUD — Font Sizes e Tooltip Colorato

### Problema
Due feedback dell'utente:
1. I font del DomeStatusHUD erano significativamente più piccoli rispetto alle Toast Notification Foundation (10–12px vs 13px), rendendo la lettura faticosa
2. Il tooltip mostrava testo monocromatico — impossibile capire rapidamente cosa non va su una pianta senza leggere tutto il testo

### Soluzione

#### 2a. Allineamento Font Sizes — `DomeStatusHUD.uss`

| Classe | Prima | Dopo |
|---|---|---|
| `.dome-pot-name` / `.dome-cryo-plant` | 12px | **13px** |
| `.dome-pot-sub` / `.dome-pot-cond` / `.dome-cryo-detail` / `.dome-cryo-id` | 10px | **12px** |
| `.dome-tab-btn` | 10px | **11px** |
| `.dome-pot-indicator` | 10px | **11px** |
| `.dome-hud-tooltip-line` | 11px (singola label) | **13px** (per-line dinamiche) |
| Tab height | 28px | **30px** |

Larghezza corpo HUD: 290px → **320px**  
Larghezza tooltip: 240px → **270px** (con `right` corretto a `347px`)

Riferimento usato: `nf-row-msg` in `NotificationsPanel.uss` = **13px**.

#### 2b. Tooltip Colorato — `DomeStatusHUDController.cs` + `DomeStatusHUD.uxml`

**Architettura precedente:** singola `Label` con testo plain, un solo colore muted.

**Nuova architettura:** container `VisualElement` (`dome-hud-tooltip-lines`) popolato dinamicamente con Label singole colorate.

**UXML:** `<ui:Label name="dome-hud-tooltip-content">` → `<ui:VisualElement name="dome-hud-tooltip-lines">`

**Controller — nuovi elementi:**

```
struct TooltipLine { string Text; Color Color; bool Bold; bool IsSep; }
static TooltipLine Sep()          → separatore dim (CSS classe dome-hud-tooltip-sep)
```

**Palette costanti:**
```
TipGreen  = rgb(127, 255, 122)   // terminal green
TipYellow = rgb(230, 201, 111)   // terminal yellow
TipRed    = rgb(211,  95,  95)   // terminal red
TipMuted  = rgb(192, 200, 197)   // muted grey
```

**Helper `RangeColor(value, min, max)`:**
- Verde se `min ≤ value ≤ max`
- Giallo se entro il 25% del margine dal range
- Rosso altrimenti

**Helper `LedColor(current, required)`:**
- Verde se il LED corrente soddisfa il requisito
- Giallo se c'è un LED attivo ma sbagliato
- Rosso se il LED è spento quando è richiesto

**`BuildPotTooltipLines`** — logica per pot attivi:

| Sezione | Elemento | Colore |
|---|---|---|
| Header | Nome pianta + livello | Verde bold |
| | PotId + stadio + giorno | Muted |
| REQUISITI E AVANZAMENTO | Intestazione | Verde bold |
| | Valori requisiti (idratazione, LED, fertilizzante, durata) | Muted (solo informativi) |
| STATO ATTUALE | Intestazione | Verde bold |
| | Idratazione corrente | Verde/Giallo/Rosso vs req |
| | Fertilizzante corrente | Verde/Giallo/Rosso vs req |
| | LED corrente | Verde/Giallo/Rosso vs req |
| | Condizione | `ConditionColor(score)` — verde ≥60, giallo 40–59, rosso <40 |
| | Giorni ottimali | Verde se >0, muted se 0 |
| Avvisi | Rischio muffa livello 1–2 | Giallo bold |
| | Rischio muffa livello 3 | Rosso bold |
| | INFESTATA DA MUFFE | Rosso bold |

**`BuildCryoTooltipLines`** — logica per slot cryo:

| Sezione | Elemento | Colore |
|---|---|---|
| Header | Nome pianta cryo + livello | Verde bold |
| | SlotId | Muted |
| POTERE PASSIVO | Intestazione | Verde bold |
| | Descrizione potere | Muted |
| EFFETTO pH | Intestazione | Verde bold |
| | Drift: positivo | Verde |
| | Drift: negativo | Giallo |
| | Drift: zero | Muted |
| NOTE | Intestazione | Verde bold |
| | Testo note | Muted |

**`SetTooltipLines(List<TooltipLine>)`:**  
Cancella il container e ricrea Label dinamicamente. Ogni Label riceve la classe USS appropriata e il colore inline via `.style.color`.

**CSS aggiunto:**
- `.dome-hud-tooltip-lines` — flex-direction column
- `.dome-hud-tooltip-line` — 13px, line-height 1.35
- `.dome-hud-tooltip-line--bold` — font-style bold
- `.dome-hud-tooltip-sep` — 10px, colore verde 35% alpha, margini verticali

---

## File Modificati

| File | Tipo modifica |
|---|---|
| `Assets/_Project/Scripts/Debug/PotDebugConsole.cs` | Aggiunta sezione debug seed impianto, metodo `DebugPlantSeed`, cache `_dayCycleSystem` |
| `Assets/_Project/UI/UIToolkit/DomeStatusHUD/DomeStatusHUD.uss` | Bump font sizes, nuove classi tooltip, widths |
| `Assets/_Project/UI/UIToolkit/DomeStatusHUD/DomeStatusHUD.uxml` | Label singola → VisualElement container tooltip |
| `Assets/_Project/Scripts/UI/UIToolkit/DomeStatusHUD/DomeStatusHUDController.cs` | Rimosso `StringBuilder`, aggiunto `TooltipLine`, `SetTooltipLines`, `BuildPotTooltipLines`, `BuildCryoTooltipLines`, `RangeColor`, `LedColor`, palette `TipGreen/Yellow/Red/Muted` |

---

## Regole Cursor Rispettate

- Nessun `FindObjectOfType` introdotto — tutti i servizi risolti via `ServiceContainer`
- `PotActions` rimane facade per comandi pot — non modificato
- Nessun placeholder: metadati seed da `PlantData` reale, valori tooltip da `PotStateModel` e `StageRequirements` reali
- Refactor incrementale: solo le parti necessarie modificate, API pubbliche invariate

---

## Note Operative (Unity Editor)

- **PotDebugConsole**: nessuna configurazione Unity necessaria, la sezione appare automaticamente nel pannello del POT selezionato quando il pot è vuoto
- **DomeStatusHUD**: nessuna configurazione Unity necessaria, le modifiche sono puramente a USS/UXML/C# — si aggiornano alla prossima compilazione
- Il `DomeStatusHUD` GameObject in scena non richiede modifiche: il `UIDocument` e le `StyleSheet` serializzate rimangono invariate
