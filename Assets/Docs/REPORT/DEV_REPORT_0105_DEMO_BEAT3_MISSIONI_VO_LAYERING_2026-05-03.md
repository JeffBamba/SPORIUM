# DEV REPORT 0105 — Demo beat 3: missioni Resources, fix auto-completamento Seed Storage, layering VO vs PC camera

**Data:** 2026-05-03  
**Sprint / contesto:** Polish demo Alpha — flusso Seed Storage → PC camera → pannello di controllo; caricamento `MissionConfig` da `Resources`; layering UI Toolkit tra VO overlay e Bedroom PC.  
**Riferimento piano:** `.cursor/plans/demo_alpha_1_0_gap_map.plan.md` (Principio 0 — un solo prodotto; flussi demo = full)  
**Report precedente:** `DEV_REPORT_0104_BEDROOM_PC_ESC_MODALI_COSTI_2026-05-03.md`

---

## Sommario interventi

1. **Missioni demo PC** (`M_Demo_PcAccess`, `M_Demo_PcSeedPower` e goal collegati) consolidate sotto **`Assets/Resources/Missions/`** (stesso albero visibile di `FirstMission`), con rimozione delle copie da `_Project/Resources/Missions/` per evitare path `Resources` duplicati.
2. **`DemoStoryDirector`:** caricamento missioni con **`Resources.Load`** + fallback **`Resources.LoadAll<MissionConfig>("")`** per nome asset; messaggi di errore aggiornati; **guard** sul completamento demo Seed Storage (flag solo se il pannello era aperto dopo il VO anomaly; interruzione del flusso CRY/PC se l’iter non è valido); reset **flag + stato static** all’append della missione Seed Storage.
3. **`MissionChecker`:** correzione logica **`Check()`** quando non esistono obiettivi istanziati (evita auto-completamento su `Enumerable.All` vuoto); costruttore e `CheckOptions` più difensivi (`Goals` / `Options` null, checker null).
4. **Layering VO vs PC camera:** `UIDocument.sortingOrder` VO (1100) e PC (1000); **`m_SortingOrder`** su **`MainMenuPanelSettings`** (200) vs **`BedroomPcPanelSettings`** (80) per ordinare **panel diversi**; ri-sync `sortingOrder` VO in **`ShowLine`**.
5. Commenti di stack HUD aggiornati (**Wardrobe**, **MainMenu** gameplay).

---

## Statistiche e progresso

### Righe di codice

- **Ambito dichiarato:** file `.cs` elencati in **File modificati** (versione working copy al report).
- **Comando:** PowerShell `Get-Content <file> | Measure-Object -Line` (una riga = una linea fisica file).
- **Totale righe sui 6 file .cs:** **3066** (Mission 68, DemoStoryDirector 872, VoOverlayController 866, BedroomPcDisplayController 498, WardrobePanelController 147, MainMenuUIToolkitController 615).
- **Delta `git diff` (righe +/-):** **non misurato in questa iterazione** (N/D).

### Sistemi funzionanti

- **Da validare in Editor** da parte del team: Play Mode demo beat 2→3 (colazione → missione Seed Storage → ascensore **senza** auto-completamento fantasmi); apertura Seed Storage → VO anomaly → chiusura pannello → missione e flusso CRY/PC; VO leggibile **sopra** terminale e pannello di controllo PC.

### Bug risolti

- **3** (documentati in sessione di sviluppo):
  1. **`Resources.Load`** missione PC assente nel percorso atteso → asset spostati in `Assets/Resources/Missions/` + fallback `LoadAll`.
  2. Missione **«Vai al Seed Storage»** che risultava **completata senza** iter (incluso caso ascensore): **`MissionChecker`** con lista obiettivi vuota + guard su `NotifySeedStoragePanelClosed` / reset in append.
  3. **VO sopra/sotto** il PC: **PanelSettings** con uguale `m_SortingOrder` (0) e stack non confrontabile solo con `UIDocument.sortingOrder` → ordine esplicito tra asset panel.

### Progresso gameplay / prodotto

- Le missioni demo **Accedi al PC** / **Accendi Seed Storage** sono **individuabili** in progetto sotto `Resources/Missions` e caricabili in modo stabile.
- La missione Seed Storage **non** dovrebbe più chiudersi “da sola” appena cambi contesto/mappa se l’obiettivo non è stato soddisfatto.
- Il **testo VO** della demo dovrebbe restare **leggibile** mentre il terminale camera e il control plane sono aperti.
- Il flusso beat 3 **non prosegue** verso VO/missioni PC se l’iter di chiusura Seed Storage non è coerente (meno stati narrativi incoerenti).

---

## 1. Risorse missioni demo PC in `Assets/Resources/Missions`

### Problema

- In Project la missione **non compariva** dove il team cerca di solito (`Assets/Resources/...`), oppure **`Resources.Load`** falliva per sync/path; rischio di **due** asset con lo stesso path logico `Missions/...` in cartelle `Resources` diverse.

### Soluzione

- Creazione (o mantenimento) degli asset sotto **`Assets/Resources/Missions/`** con GUID dedicati; rimozione delle copie PC da **`Assets/_Project/Resources/Missions/`** per evitare ambiguità runtime.

**File interessati:**  
`Assets/Resources/Missions/M_Demo_PcAccess.asset`, `Goal_Demo_PcAccess.asset`, `M_Demo_PcSeedPower.asset`, `Goal_Demo_PcSeedPower.asset` (+ `.meta`)

---

## 2. Caricamento missioni e narrative demo (`DemoStoryDirector`)

### Problema

- Dipendenza stretta da `Resources.Load(path)` soltanto; messaggi d’errore poco actionabili.
- Completamento missione Seed Storage e proseguimento beat 3 **senza** pannello effettivamente in iter.

### Soluzione

- Helper **`LoadMissionConfigFromResources`**: load per path, poi ricerca per **`Object.name`** tra tutti i `MissionConfig` in Resources.
- **`AppendDemoSeedStorageMissionIfPossible`:** dopo append riuscito → **`ClearFlag(demo_seed_storage_visited)`** e **`DemoSeedStorageMission.RestoreProgressState(false)`**.
- **`RunBeat3SeedStorageAnomalyAutoplay`:** dopo i tre blocchi VO anomaly, **`NotifySeedStoragePanelClosed`** solo se il pannello era **aperto** in quel punto; altrimenti **`yield break`** prima del VO post–Seed Storage / append **Accedi al PC**. Variabile **`missionManager`** riallineata per uso nel resto della coroutine.

**File interessati:**  
`Assets/_Project/Scripts/Core/DemoStoryDirector.cs`

---

## 3. `MissionChecker` — obiettivi vuoti e check null-safe

### Problema

- `return _optionCheckers.All(CheckOptions)` con **`_optionCheckers` vuoto** restituiva **true** (semantica LINQ di `All` su sequenza vuota), permettendo **completamento immediato** al primo `MissionManager.Check()` in Update.

### Soluzione

- Se non ci sono blocchi obiettivo → **`false`**.
- Costruttore: skip sicuro se **`Goals`** è null; **`CreateOptionChecker`** gestisce **`Options`** null; **`CheckOptions`** ignora checker null e tratta liste vuote come fallimento della singola option.

**File interessati:**  
`Assets/_Project/Scripts/Core/MissionSystem/Mission.cs`

---

## 4. Layering VoOverlay vs Bedroom PC (UI Toolkit)

### Problema

- VO e PC usano **PanelSettings** diversi (`MainMenuPanelSettings` vs `BedroomPcPanelSettings`): con entrambi **`m_SortingOrder: 0`**, l’ordine globale non garantiva il VO sopra al terminale/pannello UXML del PC. Solo l’aumento di `UIDocument.sortingOrder` sul VO non bastava.

### Soluzione

- **`MainMenuPanelSettings.asset`:** `m_SortingOrder` portato a **200** (stack HUD/VO condiviso).
- **`BedroomPcPanelSettings.asset`:** `m_SortingOrder` portato a **80** (sotto lo stack principale).
- **`VoOverlayController.ShowLine`:** reimposta **`_document.sortingOrder`** al valore costante del controller ad ogni messaggio.
- **`BedroomPcDisplayController.Awake`:** `sortingOrder = 1000` (allineamento modali full-screen); commento stack.

**File interessati:**  
`VoOverlayController.cs`, `BedroomPcDisplayController.cs`, `MainMenuPanelSettings.asset`, `BedroomPcPanelSettings.asset`, `WardrobePanelController.cs` (commento), `MainMenuUIToolkitController.cs` (commento)

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/Resources/Missions/M_Demo_PcAccess.asset` (+ meta) | Creazione / posizionamento canonico Resources |
| `Assets/Resources/Missions/Goal_Demo_PcAccess.asset` (+ meta) | Creazione / posizionamento canonico Resources |
| `Assets/Resources/Missions/M_Demo_PcSeedPower.asset` (+ meta) | Creazione / posizionamento canonico Resources |
| `Assets/Resources/Missions/Goal_Demo_PcSeedPower.asset` (+ meta) | Creazione / posizionamento canonico Resources |
| `Assets/_Project/Resources/Missions/M_Demo_PcAccess.asset` (e goal PC correlati) | Rimossi (duplicati path Resources) |
| `Assets/_Project/Scripts/Core/DemoStoryDirector.cs` | Load missioni, guard beat3, reset flag missione Seed Storage |
| `Assets/_Project/Scripts/Core/MissionSystem/Mission.cs` | Fix `Check()` lista obiettivi vuota, null-safety |
| `Assets/_Project/Scripts/UI/UIToolkit/VoOverlay/VoOverlayController.cs` | Sorting VO + `ShowLine` |
| `Assets/_Project/Scripts/UI/UIToolkit/BedroomPc/BedroomPcDisplayController.cs` | `sortingOrder` esplicito modale |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/WardrobePanelController.cs` | Commento riferimento VO |
| `Assets/_Project/Scripts/UI/UIToolkit/MainMenu/MainMenuUIToolkitController.cs` | Commento stack sorting |
| `Assets/_Project/Resources/UI/UIToolkit/MainMenu/MainMenuPanelSettings.asset` | `m_SortingOrder` panel |
| `Assets/_Project/UI/UIToolkit/BedroomPc/BedroomPcPanelSettings.asset` | `m_SortingOrder` panel |

---

## Regole / vincoli rispettati

- **Both** (demo + full): stesso binario, logica condizionata da sessione demo dove applicabile (`.cursor/rules/feature-both-demo-full-parity.mdc`).
- **ServiceContainer / architecture:** nessun nuovo `FindObjectOfType` introdotto per questi interventi (`.cursor/rules/architecture-runtime-services.mdc`).
- **DEV REPORT:** sezione **Statistiche e progresso** con metriche reali o **N/D** (`.cursor/rules/dev-report.mdc`).

---

## Note operative (Unity)

- Dopo pull: verificare che esistano solo le copie PC in **`Assets/Resources/Missions/`** (nessun duplicato stesso nome sotto altre `Resources`).
- **Reimport** cartelle `Missions` e `PanelSettings` se l’Editor mostra riferimenti stale.
- Checklist Play: post-colazione → missione Seed Storage → **nessun** completamente finché non si chiude il pannello dopo il VO anomaly; PC camera → VO **sopra** il control panel.

---

*Fine DEV REPORT 0105.*
