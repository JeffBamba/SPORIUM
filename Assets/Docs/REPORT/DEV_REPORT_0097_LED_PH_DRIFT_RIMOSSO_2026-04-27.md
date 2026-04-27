# DEV REPORT 0097 — pH botanico scalato, parità debug/runtime e azioni 5/5 full game

**Data:** 2026-04-27  
**Sprint / contesto:** Hardening runtime e UX HUD/console dopo allineamenti pH/LED: poteri botanici scalati per livello, parity debug con flusso reale, baseline azioni full game a 5.  
**Riferimento piano:** piano operativo chat (consolidamento end-to-end gameplay/testing: Pot Debug, pH drift botanico, tooltip TopBar, cap azioni full game).  
**Report precedente:** `DEV_REPORT_0096_LOCALIZZAZIONE_MISSION_RECAP_FOUNDATION_FOOD_DISPENSA_LAB_SEED_2026-04-26.md`

---

## Sommario interventi

1. **Parità PLANT debug/runtime:** `PotDebugConsole` ora usa il path canonico `PotActions.DoPlant(...)` (in automation context) invece di mutare direttamente `PotState`.
2. **Poteri attivi pH scalati per livello:** `DayCycleController` aggiorna il drift giornaliero botanico con regola `+1/Lv` Arctic e `-1/Lv` Glasscap (Lv1..Lv5).
3. **Tooltip pH TopBar:** fix colore specie (`PURE` blu, `EVIL` rosso) nei modificatori piante.
4. **Allineamento testi/tooltip botanici:** aggiornati `BotanicalPowerFacade`, `DomeStatusHUD`, `PlantCardV3`, `ExtractorTooltipTexts` e asset `PlantData` (`PLT-PURE-001`, `PLT-EVIL-001`) con wording esplicito dello scaling.
5. **Azioni full game 5/5 coerenti:** fix in scena (`SCN_VaultMap`), UI (`PlantCardV3`, `TopBar` default), fallback save e migrazione load full-game legacy da cap 4 a 5.
6. **Load full vs demo:** in `GameManager.SetDemoTutorialStateForLoad` riallineato il ramo non-demo a 5 e reseed immediato del ledger tooltip azioni.

---

## Statistiche e progresso

### Righe di codice

- **N/D (non misurato in questa iterazione):** working tree già estesa con modifiche precedenti non isolate; churn puntuale per sottoinsieme non calcolata in modo affidabile in questo passaggio.

### Sistemi funzionanti

- **Da validare in Editor** (passata integrata richiesta):  
 - PLANT da Pot Debug equivalente al flusso in-game (`DoPlant`)  
 - Drift pH botanico scalato per livello (Arctic/Glasscap)  
 - Tooltip TopBar pH (colori specie PURE/EVIL)  
 - Tooltip/HUD botanici con copy coerente allo scaling  
 - Tooltip Azioni TopBar e cap full game 5/5 anche su save legacy non-demo.

### Bug risolti

- **6** (documentati in questa iterazione):
- Pot Debug PLANT non allineato ai side-effect runtime.
- Drift pH attivo botanico non scalato per livello.
- Colori tooltip pH TopBar invertiti per PURE/EVIL.
- Copy tooltip/pannelli ancora ancorata a valori pH fissi.
- Full game ancora a 4/4 in alcuni path UI/config.
- Collisione scope variabili in `SaveManager` (`CS0136`) dopo migrazione load.

### Progresso gameplay / prodotto

- Il tester può usare Pot Debug per piantare senza perdere fedeltà rispetto al gameplay reale.
- Le piante Task 4 hanno impatto pH leggibile e progressivo con il livello (potenziamento/deperimento percepibile).
- La lettura pH in TopBar è più chiara: PURE/EVIL distinguibili visivamente a colpo d’occhio.
- I testi di poteri/tooltip riflettono il comportamento effettivo del runtime (niente promesse obsolete).
- Full game converge a baseline 5/5 azioni anche su salvataggi storici non-demo.

---

## 1. Pot Debug: PLANT sul flusso canonico

### Problema

- La console POT effettuava piantagione con mutazioni dirette di `PotState`, saltando parte degli hook/eventi del flusso ufficiale.
- In testing, comportamento potenzialmente divergente rispetto all’azione PLANT in game.

### Soluzione

- Refactor di `DebugPlantSeed`: usa `BeginAutomationContext()` + `DoPlant(seedTypeId, ..., seedItem)` su `PotActions`.
- Mantenuti metadata lab-like del seme debug, ma esecuzione delegata al percorso runtime canonico.

**File interessati:**  
`Assets/_Project/Scripts/Debug/PotDebugConsole.cs`

---

## 2. Drift pH botanico scalato per livello (Arctic/Glasscap)

### Problema

- Potere attivo pH non scalava con il livello della pianta.
- UX/gameplay richiedevano progressione/depotenziamento coerente con Lvl.

### Soluzione

- Introdotto calcolo centralizzato attivo botanico nel `DayCycleController`:
 - Arctic Hask: `+1 pH/g` per livello (`+1..+5`)
 - Glasscap: `-1 pH/g` per livello (`-1..-5`)
- Applicato sia a `GetPredictedPhDriftForNextDay()` sia alla registrazione drift effettiva giornaliera.
- Allineato anche `DomeStatusHUDController` per mostrare lo stesso numero runtime.

**File interessati:**  
`Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`  
`Assets/_Project/Scripts/UI/UIToolkit/DomeStatusHUD/DomeStatusHUDController.cs`

---

## 3. Tooltip pH e copy botanica coerenti

### Problema

- Colori drift piante nel tooltip TopBar invertiti rispetto alla convenzione richiesta.
- Diversi testi UI/asset riportavano ancora valori fissi legacy (`+5`, `+10%` isolato, ecc.).

### Soluzione

- `TopBarController`: mapping colore piante basato su specie (`Arctic` blu, `Glasscap` rosso).
- `BotanicalPowerFacade`: copy dinamica con livello e delta attuale, note esplicite “scala ±1/Lv”.
- `PlantCardV3` e `ExtractorTooltipTexts`: highlight/testi aggiornati al nuovo wording.
- Asset `PlantData` di `PLT-PURE-001` e `PLT-EVIL-001` aggiornati con descrizioni attive coerenti.
- Script editor `UpdatePlantDataActivePower` aggiornato per non ripristinare copy legacy.

**File interessati:**  
`Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs`  
`Assets/_Project/Scripts/Dome/PotSystem/Botanical/BotanicalPowerFacade.cs`  
`Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs`  
`Assets/_Project/Scripts/UI/UIToolkit/Lab/ExtractorTooltipTexts.cs`  
`Assets/Resources/Plants/PLT-PURE-001.asset`  
`Assets/Resources/Plants/PLT-EVIL-001.asset`  
`Assets/_Project/Editor/UpdatePlantDataActivePower.cs`

---

## 4. Azioni full game: convergenza a 5/5 (runtime, UI, save/load)

### Problema

- In full game risultavano ancora visual/tooltip a 4/4 in alcuni path (scene defaults, UI hardcoded, save/load legacy).

### Soluzione

- `SCN_VaultMap`: `_dailyActionsFromBreakfast` portato a `5`.
- UI:
 - `PlantCardV3TerminalController`: header azioni da `4` a `5`.
 - `TopBarController`: default serializzato `_maxActions` allineato a `5`.
- `SaveManager`:
 - fallback gameState legacy: `actionsLeft` da `4` a `5`.
 - migrazione load non-demo: se `maxActions < 5`, riallinea cap a 5 (con gestione `actionsLeft` coerente).
 - fix compilazione `CS0136` su naming locali introdotti.
- `GameManager.SetDemoTutorialStateForLoad`:
 - ramo non-demo: forza baseline `5`, riallinea cap action system e reseed ledger tooltip.
 - ramo demo: reseed ledger coerente con stato tutorial.

**File interessati:**  
`Assets/_Project/Scenes/SCN_VaultMap.unity`  
`Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs`  
`Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs`  
`Assets/_Project/Scripts/Core/SaveManager.cs`  
`Assets/_Project/Scripts/Core/GameManager.cs`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/Debug/PotDebugConsole.cs` | PLANT debug instradato su `DoPlant` runtime |
| `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs` | Drift attivo botanico scalato per livello (Arctic/Glasscap) |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs` | Colori tooltip pH specie PURE/EVIL + default `_maxActions` 5 |
| `Assets/_Project/Scripts/Dome/PotSystem/Botanical/BotanicalPowerFacade.cs` | Copy dinamica poteri con scaling e livello |
| `Assets/_Project/Scripts/UI/UIToolkit/DomeStatusHUD/DomeStatusHUDController.cs` | Drift mostrato allineato a scaling livello |
| `Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs` | Header azioni 5/5 + highlight copy scaling |
| `Assets/_Project/Scripts/UI/UIToolkit/Lab/ExtractorTooltipTexts.cs` | Demo tooltip potere attivo aggiornato |
| `Assets/_Project/Scripts/Core/SaveManager.cs` | Migrazione cap azioni full-game + fix `CS0136` |
| `Assets/_Project/Scripts/Core/GameManager.cs` | Forzatura/riallineamento baseline 5 in non-demo + reseed ledger |
| `Assets/_Project/Scenes/SCN_VaultMap.unity` | `_dailyActionsFromBreakfast` a 5 |
| `Assets/Resources/Plants/PLT-PURE-001.asset` | `activePower` coerente con scala `+1/Lv` |
| `Assets/Resources/Plants/PLT-EVIL-001.asset` | `activePower` coerente con scala `-1/Lv` |
| `Assets/_Project/Editor/UpdatePlantDataActivePower.cs` | Dizionario editor aggiornato ai nuovi testi |

---

## Regole / vincoli rispettati

- Nessuna introduzione di nuovi scan globali gameplay oltre i pattern già presenti.
- Refactor incrementale: mantenuta API/facade esistente (`PotActions`, `DayCycleController`) con estensioni locali.
- Coerenza Demo/Full: fix azioni indirizzato al full game senza rompere il path tutorial demo.

---

## Note operative (Unity)

- Verifica consigliata (Play):
 - **Pot Debug PLANT vs PLANT in-game:** stesso comportamento post-azione.
 - **Arctic/Glasscap Lvl 1..5:** drift pH progressivo atteso (`+1..+5`, `-1..-5` oltre drift specie base).
 - **TopBar pH tooltip:** Arctic in blu, Glasscap in rosso.
 - **TopBar azioni tooltip:** full game a `5/5` anche dopo load save storico non-demo.
- Se necessario, rigenerare i testi PlantData con tool editor aggiornato (`Sporae/Update Plant Active Powers`) senza reintrodurre copy legacy.

---

*Fine DEV REPORT 0097.*
