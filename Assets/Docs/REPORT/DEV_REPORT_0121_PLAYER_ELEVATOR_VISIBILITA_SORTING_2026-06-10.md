# DEV REPORT 0121 — Player visibility: sprite back e sorting portelloni elevator

**Data:** 2026-06-10  
**Sprint / contesto:** Debug visibilità player su `SCN_VaultMap`: flicker/scomparsa su animazioni back e regole di render davanti/inside cabina per portelloni elevator.  
**Riferimento piano:** N/D — fix emersi da debug live con verifica Play Mode autore  
**Report precedente:** `DEV_REPORT_0120_ELEVATOR_4_CAMERA_VIAGGIO_SMOOTH_2026-06-08.md`

---

## Sommario interventi

1. Sostituite le sprite obsolete nelle animazioni back del player: `IdleBack.anim` e `WalkBack.anim` ora puntano al nuovo frame `Player_back.png`.
2. Estesa la logica `ElevatorDoorPair.Open(...)` per distinguere apertura da fuori cabina e apertura con player gia' dentro cabina.
3. Mantenuta l'occlusione portelloni sopra player durante le transizioni da cabina: chiusura ingresso e apertura arrivo.
4. Configurate le `ElevatorFrontWalkArea` del piano -1 e 0 con `minPlayerSortingOrder: 13`, cosi' il player resta sopra portelloni/maschera anche sul bordo vicino alla camera.
5. Verificata build C# e confermata in Play Mode la regola visiva finale.

---

## Statistiche e progresso

### Righe di codice

- **File `.cs` toccati (2):** `ElevatorDoorPair.cs`, `ElevatorSystem.cs` — **2326 righe totali** (`Get-Content ... | Measure-Object -Line`, 2026-06-10).
- **Diff operativo tracciato (6 file):** **77 inserimenti / 40 rimozioni** — `git diff --stat` su animazioni player, scena e script elevator.
- **Asset nuovi non tracciati nel diff stat:** `Player_back.png`, `Player_back.png.meta`, `Player_idle.png`, `Player_idle.png.meta` presenti in working tree.

### Sistemi funzionanti

- Player davanti ai portelloni a porte ferme e fuori cabina — **verificato in Play Mode dall'autore**.
- Player davanti ai portelloni durante apertura da fuori cabina — **verificato in Play Mode dall'autore**.
- Portelloni sopra player durante chiusura ingresso cabina e apertura arrivo cabina — **verificato in Play Mode dall'autore**.
- Animazioni back senza scomparsa/flicker da sprite vecchia — **verificato in Play Mode dall'autore**.
- Compilazione C# — **verificata** con `dotnet build Sporae_Build_Beta.sln --no-restore`: 0 errori, 4 warning preesistenti/non bloccanti.

### Bug risolti

- **3** — elenco:
  1. Player che spariva/flickerava in posizioni di movimento back perche' `IdleBack.anim` / `WalkBack.anim` referenziavano ancora sprite vecchie.
  2. Player fuori cabina che poteva finire sotto i portelloni sul bordo vicino della walk area: sorting dinamico a `v=0` produceva order `0`, sotto portelloni/mask.
  3. Apertura portelloni non distingueva correttamente player fuori vs player dentro cabina: `Open()` non riceveva la condizione `IsPlayerInsideCabinOnFloor`.

### Progresso gameplay / prodotto

- Il player resta visibile davanti all'elevator in tutte le posizioni della fascia camminabile esterna.
- Le porte dell'ascensore non tagliano piu' il corpo del player quando il player e' fuori cabina.
- Le transizioni cabina conservano l'effetto corretto: i portelloni coprono il player in chiusura e lo rivelano in apertura.
- Il bug di scomparsa legato alle sprite back e' stato ricondotto agli asset animazione, non a collider/elevator runtime.
- La regola di sorting e' esplicita e localizzata: non cambia il depth globale del player nelle altre stanze.

---

## 1. Animazioni back con sprite vecchia

### Problema

- Il player spariva o flickerava in alcune posizioni perche' la direzione/animazione adottata cambiava sprite.
- `IdleBack.anim` e `WalkBack.anim` referenziavano frame di vecchie sprite sheet (`guid` precedenti), quindi il bug sembrava legato a posizione/collider ma nasceva dagli asset animazione.

### Soluzione

- Aggiornati i keyframe `m_Sprite` delle animazioni back per puntare al nuovo frame singolo `Player_back.png` (`guid: 477d31a7e9328b440a4693d04a9b0943`).
- Il cambio sprite ora resta coerente con il nuovo player anche quando il movimento seleziona idle/walk back.

**File interessati:**  
`Assets/_Project/Animations/Player/IdleBack.anim`, `Assets/_Project/Animations/Player/WalkBack.anim`, `Assets/_Project/Animations/Player/Player_back.png`

---

## 2. Regola portelloni/player fuori cabina

### Problema

- Il player usa sorting dinamico:
  - `baseOrder: 0`
  - `range: 50`
  - `order = baseOrder + Round(v * range)`
- Sul bordo vicino alla camera (`scaleNear: 1.78`, `v=0`) il player scendeva a sorting `0`.
- I portelloni fuori cabina restavano sopra (`closedSortingOrderWhenPlayerOutside: 5`) e la maschera di apertura poteva arrivare a `12`.
- Risultato: a posizioni intermedie il player era sopra, ma al bordo vicino i portelloni coprivano il corpo lasciando visibili solo parti non sovrapposte.

### Soluzione

- Configurate le aree dedicate davanti all'elevator con minimo sorting player:
  - `ElevatorFrontWalkArea_LVL_-1` -> `minPlayerSortingOrder: 13`
  - `ElevatorFrontWalkArea_LVL_0` -> `minPlayerSortingOrder: 13`
- `PerspectiveWalkArea2D.GetMaxMinPlayerSortingOrderAt(...)` e `PlayerDepthScaleAndSort` applicano ora il minimo quando il player e' nel bounds dell'area elevator front.
- `13` e' intenzionale: batte portelloni `5/11` e maschera `12`, senza alzare il player globalmente nelle altre stanze.

**File interessati:**  
`Assets/_Project/Scenes/SCN_VaultMap.unity`

---

## 3. Apertura/chiusura portelloni con stato cabina

### Problema

- `ElevatorDoorPair.Open()` non riceveva alcuna informazione sul fatto che il player fosse dentro o fuori cabina.
- La regola richiesta non e' "porte sopra durante ogni apertura", ma:
  - player fuori cabina -> player sempre sopra portelloni;
  - player dentro cabina -> portelloni sopra durante chiusura/apertura per coprire o rivelare il player.

### Soluzione

- `ElevatorDoorPair.Open(bool occludePlayer = false)` ora accetta il flag di occlusione.
- `ElevatorSystem.OpenDoors(...)` passa `IsPlayerInsideCabinOnFloor(floorIndex)`.
- `ElevatorSystem.CloseDoors(...)` mantiene lo stesso criterio per la chiusura da cabina.
- `RaiseSortingForAnimation()` alza portelloni/maschera a `animationSortingOrder` solo quando `occludePlayer` e' vero.

**File interessati:**  
`Assets/_Project/Scripts/World/Elevator/ElevatorDoorPair.cs`, `Assets/_Project/Scripts/World/Elevator/ElevatorSystem.cs`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Animations/Player/Idle.anim` | Aggiornamento riferimenti sprite player idle |
| `Assets/_Project/Animations/Player/IdleBack.anim` | Sostituzione keyframe sprite back obsoleti con nuovo frame |
| `Assets/_Project/Animations/Player/WalkBack.anim` | Sostituzione keyframe sprite back obsoleti con nuovo frame |
| `Assets/_Project/Animations/Player/Player_back.png` | Nuovo asset sprite back player |
| `Assets/_Project/Animations/Player/Player_back.png.meta` | Meta asset sprite back |
| `Assets/_Project/Animations/Player/Player_idle.png` | Nuovo asset sprite idle player |
| `Assets/_Project/Animations/Player/Player_idle.png.meta` | Meta asset sprite idle |
| `Assets/_Project/Scenes/SCN_VaultMap.unity` | `minPlayerSortingOrder: 13` su `ElevatorFrontWalkArea_LVL_-1` e `ElevatorFrontWalkArea_LVL_0`; serializzazione campi sorting |
| `Assets/_Project/Scripts/World/Elevator/ElevatorDoorPair.cs` | API `Open(bool occludePlayer)`, sorting animazione condizionato da transizione cabina |
| `Assets/_Project/Scripts/World/Elevator/ElevatorSystem.cs` | `OpenDoors(...)` passa `IsPlayerInsideCabinOnFloor(floorIndex)` |
| `Assets/Docs/REPORT/DEV_REPORT_0121_PLAYER_ELEVATOR_VISIBILITA_SORTING_2026-06-10.md` | **Nuovo** — questo report |

---

## Regole / vincoli rispettati

- **Evidenze fresche:** analisi basata su lettura corrente di script, scena e diff; nessuna supposizione su sorting.
- **Scope:** fix limitato a player/elevator visibility; nessun refactor globale del depth system.
- **Architettura runtime:** nessun nuovo `FindObjectOfType`; uso di stato esistente `IsPlayerInsideCabinOnFloor`.
- **Both (demo + full):** modifica su `SCN_VaultMap` unica, senza fork scena.
- **UI Toolkit parity:** non applicabile (nessuna modifica UI Toolkit).

---

## Note operative (Unity)

1. Se Unity e' aperto in Play Mode, uscire e rientrare in Play per ricaricare i serialized values della scena.
2. Smoke test consigliato:
   - camminare davanti ai portelloni a meta' profondita';
   - camminare sul bordo vicino alla camera;
   - chiamare apertura da fuori cabina;
   - entrare in cabina e verificare chiusura occludente;
   - viaggiare e verificare apertura/rivelazione al piano di arrivo.
3. `dotnet build Sporae_Build_Beta.sln --no-restore` completato con 0 errori; warning residui non introdotti da questo intervento.
4. `SCN_VaultMap.unity` contiene anche serializzazioni Unity pregresse nel working tree: durante review isolare il controllo sui valori `ElevatorFrontWalkArea_*`.

---

*Fine DEV REPORT 0121.*
