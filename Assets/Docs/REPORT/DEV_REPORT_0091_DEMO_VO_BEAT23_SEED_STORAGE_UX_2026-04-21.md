# DEV REPORT 0091 — Demo Alpha: VO post-colazione (beat 2/3), missione Seed Storage, fix UX overlay e blocchi narrativi

**Data:** 2026-04-21  
**Sprint / contesto:** Demo Alpha — allineamento narrativa `DemoStoryDirector` / `DemoAlphaNarrativeConfig`, VO overlay (`VoOverlayController`), missioni demo (`MissionManager` + flag), UX lettura typewriter e sequenza beat 3.  
**Riferimento piano:** `demo_alpha_1_0_gap_map` (traccia 8 beat; beat 3 Seed Storage anomaly; task missioni demo).  
**Report precedente:** `DEV_REPORT_0090_DISPENSA_REFRIGERATA_UI_SEPARATA_WIRING_FIX_2026-04-21.md`

---

## Sommario interventi

1. **VO post missione «Fai Colazione»**: due blocchi testuali distinti (Part1 / Part2) con click solo **tra** blocco 1 e 2; highlight **multi-colore** per parola (`VoLinePresentationOptions.HighlightColorHexes` + `ApplyHighlight` in `VoOverlayController`).
2. **Beat 3 — VO intro Seed Storage** + **missione** `M_Demo_SeedStorage` («Vai al Seed Storage»): goal flag `demo_seed_storage_visited`; completamento entrando in room id `storage` (`DemoSeedStorageMission` in `WardrobeMission.cs`); append missione **dopo** chiusura VO beat 3 (non all’inizio del typewriter).
3. **`SaveManager`**: ripristino progresso missione Seed Storage post-load (come armadio).
4. **`ActiveMissionsPanelController`**: recap progress, fazione Routine, evento `ProgressChanged`; in demo **nessun** VO generico «Missione completata» per `M_Demo_Breakfast` (evita sovrapposizione al blocco narrativo).
5. **`VoOverlayController` — UX «nessun continua durante il typewriter»**: flag `_textRevealInProgress`; `WaitForContinueInput` attende fine rivelazione e scarta mouse/tasti ancora premuti; **`onComplete` spostato dopo `ExitAnimRoutine` + `Hide()`** quando `ForceContinueAtEnd` (evita secondo `ShowLine` mentre la prima uscita è ancora in animazione).
6. **`DemoStoryDirector` — un solo blocco typewriter per VO**: `useMultiSentenceWhenSplit: false` per beat 1, beat 2 cucina, beat 3 intro (niente click intermedio tra frasi spezzate da `.` `!` `?`).
7. **Factory** `VoLinePresentationOptions.ForDemoBeat`: default allineato a blocco unico (`useMultiSentenceWhenSplit: false`).

---

## 1. Narrativa: config e default

### Problema
- Servivano testi beat 2 post-colazione e beat 3 con parametri VO e highlight; la missione Seed Storage non doveva apparire prima della lettura del VO beat 3.

### Soluzione
- `DemoAlphaNarrativeConfig` / `DemoAlphaNarrativeDefaults`: campi `Beat2PostBreakfastPart1Line` / `Part2Line`, liste highlight e colori per parte; `Beat3SeedStorageIntroLine` e highlight opzionali.
- Asset: `Assets/Resources/Demo/DemoAlphaNarrativeConfig.asset` aggiornato coerentemente.

**File:** `DemoAlphaNarrativeConfig.cs`, `DemoAlphaNarrativeDefaults.cs`, `DemoAlphaNarrativeConfig.asset`

---

## 2. VoOverlay — highlight multi-colore e sequenza «continua»

### Problema
- Più parole con **colori diversi** nello stesso blocco: serviva lista hex parallela alle frasi.
- Click accettato troppo presto dopo la digitazione; `onComplete` prima dell’uscita animata causava **sovrapposizione** tra due `ShowLine` consecutivi (secondo blocco che sparisce o non si legge).

### Soluzione
- `VoLinePresentationOptions`: proprietà opzionale `HighlightColorHexes` (stesso conteggio di `HighlightWords` → colore per frase; altrimenti colore unico `MissionHighlightColorHex`).
- `ApplyHighlight`: sostituzioni ordinate per lunghezza frase decrescente.
- `_textRevealInProgress` durante i loop carattere; `WaitForContinueInput`: attesa `!_textRevealInProgress`, flush tasti/mouse tenuti, poi attesa nuova pressione.
- `TypeLineRoutine` / `MultiSentenceRoutine` (rami `ForceContinueAtEnd` e timer): ordine **`ExitAnimRoutine` → `Hide` → `onComplete`**.

**File:** `VoOverlayController.cs`

---

## 3. DemoStoryDirector — flusso post-colazione e beat 3

### Problema
- VO cucina e beat 1/3 con **multi-frase** (`useMultiSentenceWhenSplit: true`) generavano **click tra frasi** (percepito come skip).
- Missione Seed Storage assegnata a **`SetBeat(3)`** prima del VO beat 3 → missione in lista mentre il testo intro è ancora in composizione.

### Soluzione
- Post-colazione: due `ShowLine` con `useMultiSentenceWhenSplit: false` per Part1 e Part2.
- `SetBeat(3)` prima del VO intro Seed Storage; **`AppendDemoSeedStorageMissionIfPossible()`** invocato **solo dopo** `onComplete` del VO beat 3 (dopo typing + click + exit + hide, per fix §2). Se `VoOverlayController` non è disponibile, il coroutine fa comunque `SetBeat(3)` e append missione (percorso fallback senza VO).
- Beat 1 Wake, `RunKitchenBreakfastBeat`, beat 3 intro: tutti con **`useMultiSentenceWhenSplit: false`**.
- `HandleRoomChanged`: notifica `DemoSeedStorageMission.NotifyEnteredStorageRoom()` su room `storage`; coroutine differita se già in storage al momento dell’append missione.

**File:** `DemoStoryDirector.cs`

---

## 4. Missione demo Seed Storage

### Problema
- Serve missione «Vai al Seed Storage» con obiettivo visita stanza, coerente con `MissionFlagGoal` e `MissionManager.Check()`.

### Soluzione
- Classe statica **`DemoSeedStorageMission`** (stesso file `WardrobeMission.cs` per inclusione stabile in `Assembly-CSharp` senza dipendere da rigenerazione `.csproj` per file nuovi isolati).
- Asset: `Goal_Demo_SeedStorage.asset` (`_flagKey: demo_seed_storage_visited`), `M_Demo_SeedStorage.asset` in `Assets/_Project/Resources/Missions/`.
- `NotifyEnteredStorageRoom()` solo se esiste missione attiva non completata; `RestoreProgressState` da lista missioni completate al load.

**File:** `WardrobeMission.cs` (include `DemoSeedStorageMission`), `Goal_Demo_SeedStorage.asset`, `M_Demo_SeedStorage.asset`, `SaveManager.cs`, `ActiveMissionsPanelController.cs`

---

## 5. Missione colazione — niente VO «completata» generico in demo

### Problema
- Al completamento `M_Demo_Breakfast`, il VO generico missione completata poteva sovrapporsi al blocco narrativo gestito dal Director.

### Soluzione
- In `HandleMissionComplete`, se `DemoSessionState.IsDemo` e config `M_Demo_Breakfast`, **non** chiamare `ShowLine` del messaggio generico (toast completamento resta).

**File:** `ActiveMissionsPanelController.cs`

---

## File modificati (principali)

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/UI/UIToolkit/VoOverlay/VoOverlayController.cs` | Highlight multi-colore, `_textRevealInProgress`, ordine `onComplete`, `WaitForContinueInput`, `ForDemoBeat` |
| `Assets/_Project/Scripts/Core/DemoStoryDirector.cs` | Part1/Part2 VO, beat 3, missione Seed Storage timing, `useMultiSentenceWhenSplit` beat 1/2/3 |
| `Assets/_Project/Scripts/Core/DemoAlphaNarrativeConfig.cs` | Campi Part1/Part2 e highlight |
| `Assets/_Project/Scripts/Core/DemoAlphaNarrativeDefaults.cs` | Default testi e liste highlight |
| `Assets/Resources/Demo/DemoAlphaNarrativeConfig.asset` | Dati serializzati narrativa |
| `Assets/_Project/Scripts/Core/MissionSystem/WardrobeMission.cs` | `DemoSeedStorageMission` |
| `Assets/_Project/Resources/Missions/Goal_Demo_SeedStorage.asset` | Nuovo goal flag |
| `Assets/_Project/Resources/Missions/M_Demo_SeedStorage.asset` | Nuova missione |
| `Assets/_Project/Scripts/Core/SaveManager.cs` | Restore `DemoSeedStorageMission` |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/ActiveMissionsPanelController.cs` | Seed Storage recap + skip VO completamento breakfast demo |

---

## Regole / vincoli rispettati

- **Principio 0 demo/full:** stesso binario; missioni e VO condizionati da `DemoSessionState` / contenuti config, senza fork inventario dedicato.
- **ServiceContainer** per servizi già esistenti (`MissionManager`, `RoomTracker`, `VoOverlayController`); nessun nuovo scene scan per logica missione Seed Storage.
- **UI:** VO su stack Panel Settings già usato dal progetto (`DEV REPORT 0085` — layering).

---

## Note operative (Unity)

- Verificare in scena che l’area Seed Storage abbia `RoomAreaTag.RoomId` coerente con **`storage`** (come `CompactBottomBarController`), altrimenti il goal non si completa.
- `Resources.Load("Missions/M_Demo_SeedStorage")` richiede asset sotto cartella `Resources` del progetto (`Assets/_Project/Resources/Missions/`).

---

*Fine DEV REPORT 0091.*
