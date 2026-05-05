# DEV REPORT 0108 — PlantCard4v: VO reazioni post-intervento e allineamento pH ambiente

**Data:** 2026-05-05  
**Sprint / contesto:** UI Toolkit PlantCard4v — feedback narrativo dopo azioni sul POT e coerenza numerica pH cupola vs TopBar.  
**Riferimento piano:** `.cursor/plans/plantcard4v.plan.md`  
**Report precedente:** `DEV_REPORT_0107_PLANTCARD4V_CARE_VIEW_UI_LOGIC_2026-05-05.md`

---

## Sommario interventi

1. Aggiunto feedback **VO in-card** dopo interventi (acqua, LED, additivo, potatura, fertilizzante): testi satirici / black humor che commentano utilità, rischio o errore, come “coscienza del biologo”.
2. Introdotto **snapshot pre-azione** e richiesta `PlantCard4vVoReactionRequest` consumata nel `Build` del view model, con `VoHintId` prefisso `react|` per evitare dedup giornaliero sul messaggio diagnostico iniziale.
3. Gestito il caso **fertilizzante incompatibile** (`DoFertilize` fallisce ma la pianta muore): refresh con VO anche quando `ok == false` e stato **Morta**.
4. Su **`EmitActionFailed`** annullata la reazione VO pendente per non mescolare messaggi.
5. Corretto il wiring **Ambiente pH** nella riga bisogni: il valore numerico mostra **`PhSystem.CurrentPh`** (scala -100/+100, come TopBar), non più il solo drift giornaliero accodato.
6. Registrato il nuovo script in **`Assembly-CSharp.csproj`** (progetto con elenco esplicito dei file).

---

## Statistiche e progresso

### Righe di codice

- **Comando:** PowerShell `(Get-Content <path>).Count` sui file `.cs` interessati da questa iterazione (functionalità VO reazione + pH ambiente).
- **File nuovo:** `PlantCard4vBiologistReactionVo.cs` — **264** righe.
- **File aggiornati (totale righe file al momento della misura):**  
  - `PlantCard4vCareViewModel.cs` — **1116** righe  
  - `PlantCard4vController.cs` — **1167** righe  
- **Delta +/- isolato solo a questa feature:** non riclassificato (worktree/stage può contenere altre modifiche sugli stessi file).

### Sistemi funzionanti

- **Build C#:** `dotnet build Assembly-CSharp.csproj --no-restore` completata con **0 errori / 0 avvisi** (verifica in iterazione su agent).
- **PlantCard4v (VO reazione + pH):** da validare in **Editor Play Mode** su tutti i flussi (picker additivo/fertilizzante, toggle irrigazione/LED, potatura, morte da fertilizzante errato).

### Bug risolti

- **1** (UX/dati): etichetta **Ambiente pH** mostrava il **drift accodato** (`GetTotalDailyDrift`) invece del **pH cupola corrente**, disallineato dalla TopBar “DRIFT pH” (che espone `CurrentPh`).

### Progresso gameplay / prodotto

- Dopo un intervento, il testo VO nella card può **confermare, smontare o giudicare** la scelta con tono da biologo cinico.
- Il giocatore vede **subito** un riscontro testuale anche negli esiti estremi (es. chimica sbagliata letale).
- Il numero **Ambiente pH** sulla card è **allineato** al valore cupola che il player associa alla barra principale.
- Il **drift giornaliero** resta disponibile nei **tooltip** riga pH, senza confonderlo con la lettura istantanea.

---

## 1. VO reazioni post-intervento (biologo)

### Problema

- Il VO in-card descriveva soprattutto l’**apertura** / stato; mancava un feedback narrativo ** immediatamente dopo** azioni sul POT (acqua, luce, spray, potatura, fertilizzante).

### Soluzione

- Aggiunti `PlantCard4vCareSnapshot`, `PlantCard4vVoReactionRequest` e `PlantCard4vBiologistReactionVo` con righe VO per tipo `PotActionType` (Water, Light, Spray, Pruning, Fertilize), confrontando **prima/dopo** dove serve (compatibilità LED, muffa, pH additivi, range fertilizzante, morte da incompatibilità).
- `PlantCard4vCareViewModel.Build(..., reactionRequest)`: se presente, `TryBuildLine` **sovrascrive** `VoHintLine` / `VoHintId`.
- `PlantCard4vController`: `BeginVoReaction` prima dell’azione, `Refresh` passa `_pendingVoReaction` e lo azzera; `CancelVoReaction` su fallimenti e su `HandlePotActionFailed`.
- Picker fertilizzante: `RequestRealtimeRefresh(playVo: ok || morta)` per coprire `DoFertilize == false` con pianta **Morta**.

**File interessati:**  
`PlantCard4vBiologistReactionVo.cs`, `PlantCard4vCareViewModel.cs`, `PlantCard4vController.cs`

---

## 2. Ambiente pH: lettura cupola vs drift

### Problema

- `pcv4-ph-value` era bindato a `PhDomeDriftText` (solo drift accodato), mentre la TopBar mostra **`CurrentPh`**: risultato tipico **0,0** sulla card con pH reale positivo e banda “Neutro” già coerente.

### Soluzione

- Aggiunta `PhDomeAmbientValueText` da `phSystem.CurrentPh` (`F1`, `it-IT`) in `ResolveDomePhRow`.
- Controller: `_phValueLabel.text = model.PhDomeAmbientValueText`.
- `PhDomeDriftText` resta per testi tooltip che citano il drift.

**File interessati:**  
`PlantCard4vCareViewModel.cs`, `PlantCard4vController.cs`

---

## 3. Progetto C#

### Problema

- `Assembly-CSharp.csproj` elenca esplicitamente i sorgenti: il nuovo file non compariva → errori `CS0246` in build da IDE/dotnet finché non incluso.

### Soluzione

- Aggiunta voce `<Compile Include="...\PlantCard4vBiologistReactionVo.cs" />`.

**File interessati:**  
`Assembly-CSharp.csproj`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/UI/UIToolkit/PlantCard4v/PlantCard4vBiologistReactionVo.cs` | Nuovo (snapshot, request, righe VO reazione) |
| `Assets/_Project/Scripts/UI/UIToolkit/PlantCard4v/PlantCard4vBiologistReactionVo.cs.meta` | Nuovo (Unity) |
| `Assets/_Project/Scripts/UI/UIToolkit/PlantCard4v/PlantCard4vCareViewModel.cs` | Build con `reactionRequest`, override VO, `PhDomeAmbientValueText` |
| `Assets/_Project/Scripts/UI/UIToolkit/PlantCard4v/PlantCard4vController.cs` | Pending reaction, picker fert, fail handler, bind pH ambiente |
| `Assembly-CSharp.csproj` | Include compile nuovo script |

---

## Regole / vincoli rispettati

- Parità narrativa: un solo albero PlantCard4v UXML; niente pannello VO “parallelo” solo Builder.
- Interazione gameplay tramite `PotActions` / eventi `PotEvents`; nessun `FindObjectOfType` aggiunto nel flusso PlantCard4v di questa iterazione.
- Localizzazione tono: copy italiana intentional nel modulo biologo (coerente con VO esistente in italiano).

---

## Note operative (Unity)

- Verificare in Play: sequenza apertura card → azione → typing VO aggiornato; ripetere VO con “RIPETI” se necessario.
- Verificare morte da fertilizzante incompatibile: messaggio reazione e stato **Morta** coerente.
- Confronto visivo: numero riga **Ambiente pH** vs valore numerico TopBar pH.

---

*Fine DEV REPORT 0108.*
