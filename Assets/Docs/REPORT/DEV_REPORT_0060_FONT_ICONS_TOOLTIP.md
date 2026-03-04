# DEV REPORT 0060 — Font globale GNF, tooltip PH Drift, catalogo icone per tipologia

**Data:** 2026-03-04  
**Scope:** UI Toolkit — font standard unico (GNF TTF), styling tooltip PH Drift (outline, angoli arrotondati, background opaco), sistema icone centralizzato per tipologia/categoria/azione/pianta (configurabile da Unity senza codice).  
**Riferimenti:** `SporaeRuntimeTheme.tss`, `TopBar.uxml` / `TopBarController.cs`, `GlobalIconCatalog.cs`, `GlobalIconResolver.cs`, `PlayerInventoryPanelController.cs`, `README_FONTS.md`, `README_ICONS.md`  
**Report precedente:** `Assets/Docs/REPORT/DEV REPORT_0059.txt`

---

## 1. Font standard globale (GNF TTF)

### 1.1 Obiettivo
Un unico font di base per tutti i campi di testo del progetto (GNF TTF), senza fallback su temi Unity estranei. Possibilità di sovrascrivere singoli elementi da UI Builder o Inspector senza toccare codice.

### 1.2 Theme Style Sheet
- **File:** `Assets/_Project/UI/UIToolkit/SporaeRuntimeTheme.tss`
- `@import url("unity-theme://default");` con override su `:root`, `Label`, `Button`, `TextField`, `TextElement` e selettore `*` tramite `-unity-font-definition` e `-unity-font` (URL project://database verso `GNF TTF.ttf`, tipo 3, guid + `#GNF`).
- Classe opzionale `.sp-font-gnf` per forzare GNF dove serve.

### 1.3 PanelSettings
Tutti i PanelSettings usati dagli UIDocument referenziano `SporaeRuntimeTheme.tss` al posto del tema runtime di default:
- PlayerStatusPanelSettings, TopNavigationPanel, BottomNavigationPanel, FoundationNotificationsPanel, BackgroundSettingsPanel, PlantCardV2Settings, ecc.

### 1.4 Pulizia override runtime
- **TopBarController.cs:** `ApplyPhTooltipTitleFont()` — rimosso fallback `FindLoadedFont("PixelOperator_8pt")`; se `_phTooltipTitleFont` è null in Inspector si eredita il font dal tema.
- **PlantCardV3TerminalController.cs:** `ApplyConsoleFont()` — rimosso fallback `FindLoadedFont`; se `_consoleMonoFont` è null si eredita il tema. Aggiunto tooltip sul campo Inspector. Rimossa creazione runtime del tooltip forecast (ora definito in UXML/USS).

### 1.5 Documentazione
- **README_FONTS.md** (`Assets/_Project/UI/UIToolkit/README_FONTS.md`): regole rapide, override da UI Builder (classe o singolo elemento), override da Inspector, troubleshooting.

---

## 2. Tooltip PH Drift (stile box)

### 2.1 Modifiche visive
- **ph-tooltip-section-modifiers-total:** bordo (border-width, border-color), border-radius per angoli arrotondati, padding; **background-color: #1E282A** (rgb(30, 40, 42)) renderizzato sopra l’immagine di background di `ph-tooltip`.
- **ph-tooltip-section-projection:** outline/bordo e border-radius coerenti con il box modifiers.

### 2.2 File
- **TopBar.uxml:** stili inline per le due sezioni (border-width, border-color, border-radius, padding; background-color solo su modifiers-total).

---

## 3. Catalogo icone globale (per tipologia / categoria / azione / pianta)

### 3.1 Obiettivo
Un’icona per tipologia di item (es. tutte le piante, tutti i semi, tutte le azioni come Overwatering), configurabile da Unity senza modificare codice.

### 3.2 ScriptableObject e API
- **GlobalIconCatalog.cs** (`Assets/_Project/Scripts/UI/Icons/GlobalIconCatalog.cs`): ScriptableObject con liste serializzate:
  - **TypeIconEntry** (typeId, sprite) — override per typeId specifico (es. `seed-001`, `WAT-RAW`).
  - **CategoryIconEntry** (categoryKey, sprite) — es. `plant`, `seed`, `spore`, `fruit`, `water`, `fertilizer`, `additive`, `reagent`, `stemcell`, `protein`, `food`, `preseed`, `misc`, `action`.
  - **ActionIconEntry** (actionKey, sprite) — es. `overwatering`, `blueled`, `redled`, `sprayantifungal`, `water`, `light`, `fertilize`, `pruning`, `harvest`, `plant`, `uproot`.
  - **PlantCodeIconEntry** (plantCode, sprite).
  - Campi default: `_defaultItemIcon`, `_defaultActionIcon`, `_defaultPlantIcon`.
- **GlobalIconResolver.cs** (`Assets/_Project/Scripts/UI/Icons/GlobalIconResolver.cs`): classe statica che carica il catalog da `Resources/UI/GlobalIconCatalog.asset`; metodi `GetItemIcon(typeId, categoryKey)`, `GetPlantIcon(plantCode)`, `GetActionIcon(actionKey)`; normalizzazione chiavi (lowercase, alfanumerico); priorità typeId → category → default.

### 3.3 Utilizzo in gioco
- **PlayerInventoryPanelController:** in `Rebuild`, per ogni riga inventario viene creato un icon box con `BuildItemIcon(typeId)` che usa `GlobalIconResolver.GetItemIcon`; stili `.inv-row-iconbox` e `.inv-row-iconglyph` in `PlayerInventoryPanel.uss`.
- **SeedInventoryMenu:** in `BuildRow` e `AddEmptyRow` applicazione icona tramite `ApplyIconToElement` con `GlobalIconResolver.GetItemIcon`.
- **NotificationItemIconResolver:** `GetIcon` delega a `GlobalIconResolver.GetItemIcon` (rimossi path/icona locali).
- **TopBarController:** `AddPhModifierRow` accetta parametro `Sprite icon`; per modificatori pianta e azione viene passata l’icona da `GlobalIconResolver` (GetPlantIcon / GetActionIcon); iconBox con backgroundImage e unityBackgroundScaleMode.

### 3.4 Asset e documentazione
- **GlobalIconCatalog.asset** in `Assets/_Project/Resources/UI/`: istanza precompilata con icona placeholder (guid `e2c36b22fa421f74383f11b0311980d3`) per default e per voci tipo/categoria/azione indicate in README_ICONS.
- **README_ICONS.md** (`Assets/_Project/UI/UIToolkit/README_ICONS.md`): creazione catalog (Create > Sporae > UI > Global Icon Catalog), path Resources/UI, chiavi categoria e azione, punti d’uso (notifiche, inventario, seed storage, tooltip PH modifiers).

---

## 4. File modificati / creati (riepilogo)

| Area | File |
|------|------|
| Tema / font | `SporaeRuntimeTheme.tss` (nuovo/aggiornato), PanelSettings vari (.asset) |
| HUD / tooltip | `TopBar.uxml`, `TopBarController.cs` |
| Terminal | `PlantCardV3TerminalController.cs` |
| Icone | `GlobalIconCatalog.cs`, `GlobalIconResolver.cs` (nuovi), `Icons` folder |
| UI inventario / seed | `PlayerInventoryPanelController.cs`, `PlayerInventoryPanel.uss`, `SeedInventoryMenu.cs` |
| Notifiche | `NotificationItemIconResolver.cs` |
| Docs | `README_FONTS.md`, `README_ICONS.md` |
| Asset | `Resources/UI/GlobalIconCatalog.asset` |

---

## 5. Note per QA

- **Font:** In Play, tutti i testi (HUD, inventario, Lab, notifiche, terminal) devono usare GNF TTF; nessun font “sistema” o altro tema. Per sovrascrivere un singolo label: assegnare classe `sp-font-gnf` o impostare `-unity-font-definition` in UI Builder.
- **Tooltip PH Drift:** Aprendo il tooltip PH (TopBar), i box “Active Modifiers” e “Projection” devono avere bordo, angoli arrotondati e il box modifiers sfondo #1E282A sopra lo sfondo del tooltip.
- **Icone:** In inventario e seed storage ogni riga mostra un’icona (tipo/categoria); in notifiche “Added to Inventory” e nel tooltip PH sui modificatori attivi devono comparire le icone risolte dal catalog. Cambiando le sprite nel GlobalIconCatalog (Inspector) e riavviando/aggiornando la UI, le nuove icone devono apparire senza modificare codice.

---

*Fine DEV REPORT 0060.*
