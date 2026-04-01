# DEV REPORT 0082 — HUD: cursore tooltip (?), localizzazioni Compact/TopBar, Player Status; debug pick UITK

**Data:** 2026-04-01  
**Sprint / contesto:** iterazione **HUD UIToolkit** (Compact Bottom Bar, TopBar, Player Status), **localizzazione IT/EN** stanze e barra stato, **cursore OS** con hint “?” su elementi con tooltip; sessione con analisi runtime (NDJSON) su `Panel.Pick` multi-`UIDocument`.  
**Riferimento piano:** nessun piano singolo vincolante; incrocio con `.cursor/rules/ui-hud-foundation-ui-builder-parity.mdc`, `architecture-runtime-services.mdc`.  
**Report precedente:** `DEV_REPORT_0081_HUD_FOUNDATION_SPACING_NAMES_COLLAPSE_LOCATION_MUTATION_TOOLTIP_2026-03-30.md`

---

## Sommario interventi

1. **Compact Bottom Bar** — tooltip **stanze** e etichetta **Location** / **DAY** con testi **IT/EN** da `NotificationLocalization.Pick` (dizionario per `roomId` in `CompactBottomBarController`; fallback `RoomAreaTag` in scena).
2. **Player Status Panel** — etichette **IDRATAZIONE** / **Diario SPORAE** in UXML allineate al Builder; runtime `Pick` IT/EN in `PlayerStatusPanelController`.
3. **Asset** — URL icona condensa rinominata (`Icona Condenza_sil`) in `PlayerStatusPanel.uxml` per eliminare warning Unity.
4. **TopBar** — tooltip **Mutation Index** già riallineato allo stile ph (report 0081); in questa sessione: **cursore hint** e logica pick.
5. **HudTooltipCursor / HudTooltipCursorDriver** — cursore custom **freccia + ?** (`Cursor.SetCursor` con **`UnityEngine.Cursor`** qualificato); scansione **tutti** gli `UIDocument` ordinati per `sortingOrder`; su primo `Pick` senza host si **continua** al panel successivo (non si interrompe sull’overlay tipo `game-viewport-background`).
6. **Classi USS `hud-tooltip-host`** su metriche TopBar, CompactBar (`content`, `zone-center`, `zone-left`, badge CRY, room-btn), DomeStatus (header POT / righe CRYO in codice); tooltip TopBar (`ph-tooltip`, `condensation-tooltip`, `mutation-tooltip`) con `picking-mode="Ignore"` + host in UXML.
7. **`HudTooltipCursor.IsUnderTopBarTooltipHost`** — oltre alla classe, fallback su **`name`** stabile (`ph-display`, `mutation-display`, `condensation-display`, tooltip radice).
8. **Strumentazione debug** — NDJSON in `debug-416e12.log` (sessione `416e12`) per `pick_scan` / `UIBlocker`; **da rimuovere** quando la verifica sarà chiusa.
9. **Stato aperto (nota uscita sessione)** — segnalazione utente: su metriche **Condensazione** e **Mutazione** il **?** sul cursore **può ancora non comparire**; serve ulteriore passaggio (pick/geometry/ordine pannelli) o log post-fix.

---

## 1. Localizzazione stanze e barra Compact

### Problema
Tooltip stanze e location in inglese; contatore giorno e prefisso `[Location: …]` non coerenti con lingua IT.

### Soluzione
- `CompactBottomBarController`: `TryGetLocalizedRoomTooltip` per ogni `roomId` noto; `Pick` IT/EN per `GIORNO` / `DAY`, `[Posizione:]` / `[Location:]`.
- Testi scena `RoomAreaTag` non obbligatori da editare per la lingua: il codice ha priorità per gli ID noti.

**File:** `CompactBottomBarController.cs`

---

## 2. Player Status — etichette idratazione / diario

### Problema
`hydration-label` e `diary-label` in inglese nel box giocatore.

### Soluzione
- UXML: testi placeholder IT; `PlayerStatusPanelController.InitializeBars`: `NotificationLocalization.Pick` per IT/EN runtime.

**File:** `PlayerStatusPanel.uxml`, `PlayerStatusPanelController.cs`

---

## 3. Cursore tooltip HUD (`HudTooltipCursorDriver`)

### Problema
Comportamento intermittente del cursore “?”; ambiguità `Cursor` (UIElements vs `UnityEngine.Cursor`).

### Soluzione
- `LateUpdate`: `UIBlocker.IsPointerOverUI()`; per ogni `UIDocument` ordinato: `RuntimePanelUtils.ScreenToPanel` + `panel.Pick`; se primo hit senza host → **continua** (non `break` prematuro).
- `HudTooltipCursor.IsUnderTopBarTooltipHost`: classe `hud-tooltip-host` **o** antenati con `name` metriche/tooltip TopBar.
- Qualifica esplicita: `UnityEngine.Cursor.SetCursor`.

**File:** `HudTooltipCursor.cs`, `HudTooltipCursorDriver.cs`, `TopBar.uxml` (tooltip + host), `CompactBottomBar.uxml`, `DomeStatusHUDController.cs` (`AddToClassList`), scena `SCN_VaultMap.unity` (componente driver su `HUD_TopBar`).

---

## 4. Warning icona condensa (PlayerStatusPanel)

### Problema
Riferimento asset rinominato (`Condensa` → `Condenza`).

### Soluzione
- Aggiornato URL `project://` e fragment `#` in UXML.

**File:** `PlayerStatusPanel.uxml`

---

## 5. Problema residuo (da tracciare)

### Problema
Su **Condensazione** e **Mutazione** (TopBar) il cursore con **?** può **non** attivarsi ancora in modo affidabile (segnalazione a fine sessione).

### Ipotesi già emerse dai log (sessione debug)
- `Panel.Pick` che risolve su contenitori senza classe / overlay; fallback `name` e tooltip con `Ignore` + `hud-tooltip-host` applicati; potrebbero servire ulteriori prove runtime o affinamento geometria (ordine disegno, `picking-mode` figli).

### Prossimi passi suggeriti
- Riprodurre con `debug-416e12.log` pulito; analizzare `pick_scan` con `runId":"post-fix"` quando il puntatore è solo su mutazione/condensa.
- Valutare pick alternativo o hit-test dedicato solo per la fascia `top-bar-content` se necessario.

---

## File modificati (tabella)

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/HudTooltipCursor.cs` | Texture cursore; `IsUnderTopBarTooltipHost` |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/HudTooltipCursorDriver.cs` | Logica Pick multi-documento; log NDJSON debug; `UnityEngine.Cursor` |
| `Assets/_Project/UI/UIToolkit/HUD/TopBar.uxml` | `hud-tooltip-host` / `picking-mode` tooltip; `content`; allineamenti precedenti (tooltip mutation, ph) |
| `Assets/_Project/UI/UIToolkit/HUD/CompactBottomBar.uxml` | `hud-tooltip-host` zone e room-btn |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/CompactBottomBarController.cs` | Localizzazione room / giorno / posizione |
| `Assets/_Project/Scripts/UI/UIToolkit/DomeStatusHUD/DomeStatusHUDController.cs` | `hud-tooltip-host` su header POT / cryo |
| `Assets/_Project/UI/UIToolkit/PlayerStatusPanel.uxml` | Etichette IT; icona Condenza URL |
| `Assets/_Project/Scripts/UI/UIToolkit/PlayerStatusPanelController.cs` | `Pick` etichette IT/EN |
| `Assets/_Project/Scenes/SCN_VaultMap.unity` | Componente `HudTooltipCursorDriver` su `HUD_TopBar` |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/HudTooltipCursor.cs.meta` / `HudTooltipCursorDriver.cs.meta` | Meta GUID script |

*File di log locale `debug-416e12.log` (root progetto): generato a runtime dalla strumentazione; non versionato obbligatoriamente.*

---

## Regole / vincoli rispettati

- **Nessun `FindObjectOfType`** aggiunto per gameplay nel driver elenco documenti: uso `FindObjectsByType<UIDocument>` per elenco pannelli UITK.
- **Parità Builder**: classi `hud-tooltip-host` visibili in UXML; tooltip TopBar restano editabili; `picking-mode="Ignore"` sui tooltip allineato al codice esistente (`TopBarController`).
- **Localizzazione**: `NotificationLocalization` / `GameLanguageSettings` coerenti con il resto del progetto.

---

## Note operative (Unity)

- Verificare in Play: cursore **?** su CRY / icone stanze / drift pH; su **mutazione** e **condensazione** la verifica può essere ancora aperta.
- Rimuovere la region **`// #region agent log`** in `HudTooltipCursorDriver` dopo conferma definitiva e cancellazione uso del file `debug-416e12.log`.

---

*Fine DEV REPORT 0082.*
