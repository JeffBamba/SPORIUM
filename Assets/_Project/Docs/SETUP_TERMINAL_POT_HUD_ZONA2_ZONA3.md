# Setup Terminal Pot – HUD Zona 2 e Zona 3 (per principianti)

Istruzioni passo passo per configurare in Unity la preview incubator e i dati delle zone 2 e 3 del Terminal Pot.  
**Riferimento gerarchia scena:** `Assets/_Project/Docs/SceneHierarchy.txt`.

---

## Check iniziale (gerarchia scena)

Prima di iniziare, verifica che nella **Hierarchy** tu abbia:

1. **Canvas** (il Canvas principale di gioco, con sotto HUD, PlayerStatusPanel, ecc.).
2. Sotto **Canvas**, un figlio chiamato **PlantCardV3** con:
   - [Component] **RectTransform**
   - [Component] **UIDocument**
   - [Component] **PlantCardV3TerminalController**

Se **PlantCardV3** non c’è sotto Canvas, non proseguire e controlla la scena (es. SCN_VaultMap o la scena di gioco che usi).

---

## Passo 1: Creare l’asset Terminal Pot Preview Config

1. Apri la finestra **Project** (in basso).
2. Vai nella cartella dove vuoi salvare il config (es. `Assets/_Project/` o `Assets/_Project/Art/UI/` o `Assets/_Project/Configs/`).
3. **Clic destro** nella cartella → **Create** → **Sporae** → **UI** → **Terminal Pot Preview Config**.
4. Assegna un nome al file, es. `TerminalPotPreviewConfig`.
5. **Check:** nella Project deve comparire un asset con icona ScriptableObject e il nome scelto.

---

## Passo 2: Assegnare lo sprite “Status vuoto” (obbligatorio)

Lo sprite **Status Vuoto** è l’immagine mostrata nella preview quando il pot è vuoto.

1. Seleziona l’asset **TerminalPotPreviewConfig** creato al Passo 1 (clic singolo nella Project).
2. Nell’**Inspector** (pannello a destra) troverai la sezione **Terminal Pot Preview Config** con:
   - **Pot vuoto** → **Status Vuoto Sprite**
   - **Stadi condivisi** → Seed Sprite, Sprout Sprite, Dead Sprite
   - **Stadi adulti** → Adult Sprite, Flowering Sprite
3. Nel campo **Status Vuoto Sprite**:
   - Trascina uno **sprite** dalla Project (es. da `Assets/_Project/Art/` o dalla cartella delle UI) **oppure**
   - Clic sulla piccola **icona cerchio** a destra del campo e scegli uno sprite dalla finestra che si apre.
4. **Check:** il campo **Status Vuoto Sprite** non deve essere “None (Sprite)”.

Se non hai ancora uno sprite “pot vuoto”, puoi usare temporaneamente uno sprite qualsiasi (es. un’icona o una texture); potrai sostituirlo dopo con l’arte definitiva.

---

## Passo 3: (Opzionale) Assegnare gli altri sprite incubator

Per avere la preview con stile incubator per ogni stadio di crescita:

1. Con **TerminalPotPreviewConfig** ancora selezionato, nell’Inspector compila, se li hai:
   - **Seed Sprite**, **Sprout Sprite**, **Dead Sprite** (stadi condivisi)
   - **Adult Sprite**, **Flowering Sprite** (stadi adulti)
2. Se un campo resta vuoto, il gioco userà i fallback già previsti dal codice (la preview può restare vuota o usare un’immagine di default per quello stadio).
3. **Check:** almeno **Status Vuoto Sprite** deve essere assegnato (già fatto al Passo 2).

---

## Passo 4: Collegare il config al PlantCardV3

1. Nella **Hierarchy** (pannello a sinistra), espandi **Canvas** se non è già espanso.
2. Clicca su **PlantCardV3** (figlio di Canvas).
3. Nell’**Inspector**, scorri fino al componente **Plant Card V3 Terminal Controller (Script)**.
4. Trova la sezione **Terminal HUD Zona 2 (preview incubator)** con il campo:
   - **Terminal Pot Preview Config**
5. Trascina l’asset **TerminalPotPreviewConfig** dalla **Project** nel campo **Terminal Pot Preview Config**  
   **oppure** usa l’icona cerchio e seleziona `TerminalPotPreviewConfig` dalla lista.
6. **Check:** nel componente Plant Card V3 Terminal Controller il campo **Terminal Pot Preview Config** non deve essere “None (Terminal Pot Preview Config)”.

---

## Passo 5: Verifica in Play

1. Salva la scena (**File** → **Save** o Ctrl+S).
2. Premi **Play** (pulsante in alto al centro).
3. Vai nella **Room Dome** (dove ci sono i vasi e il terminale).
4. Interagisci con il **TerminalPC** (l’oggetto che apre il terminale; in gerarchia è sotto **ROOM_Dome** → **TerminalPC**, con componente **Plant Card V3 Terminal Opener**).
5. Si apre il **Terminal Pot** con tre aree:
   - **Zona 1 (destra):** terminale CRT con comandi.
   - **Zona 2 (centro):** preview pianta, nome, codice, livello, one-liner, **4 box POT** cliccabili.
   - **Zona 3 (sinistra):** due blocchi **Vital Status** con le statistiche della pianta.
6. **Check Zona 2:** clicca su uno dei 4 box sotto la preview: la preview e i Vital Status devono aggiornarsi al pot selezionato; se il pot è vuoto deve comparire l’immagine **Status vuoto** che hai assegnato al config.
7. **Check comando STATUS:** nel terminale digita **STATUS** e invia: oltre alla tabella devono apparire le **Condizioni per vaso** (testo del tooltip conditions_badge per ogni pot).
8. **Check trascinamento:** trascina con il mouse il blocco centrale (preview + 4 box) e i due blocchi Vital Status a sinistra; le posizioni devono potersi spostare e restare salvate alla chiusura/riapertura del terminale.

---

## Riepilogo check (SceneHierarchy)

| Cosa verificare | Dove (in base a SceneHierarchy.txt) |
|----------------|-------------------------------------|
| Canvas principale | Root della scena di gioco (es. sotto cui c’è HUD, PlantCardV3, ecc.) |
| PlantCardV3 | Figlio diretto di **Canvas** (righe 2354–2357: RectTransform, UIDocument, PlantCardV3TerminalController) |
| TerminalPC (apre il terminale) | Sotto **ROOM_Dome** (righe 2822–2825: Transform, SpriteRenderer, BoxCollider, Interactable, **PlantCardV3TerminalOpener**) |
| Vasi (POT-001 … POT-004) | Sotto **ROOM_Dome** → **Dome_PotsAnchor** → Pot_POT-001, Pot_POT-002, ecc. |

---

## Problemi comuni

- **La preview è sempre vuota anche con pot occupato**  
  Controlla che **Terminal Pot Preview Config** sia assegnato su **PlantCardV3** e che nello script almeno **Status Vuoto Sprite** sia impostato; per i pot pieni servono anche gli sprite degli stadi (Seed, Sprout, Adult, Flowering, Dead) se vuoi vedere l’immagine corretta.

- **PlantCardV3 non compare sotto Canvas**  
  Apri la scena di gioco corretta (es. VaultMap) e controlla che il prefab o il GameObject PlantCardV3 sia presente sotto Canvas come in SceneHierarchy.txt.

- **Il terminale non si apre**  
  Verifica che il **TerminalPC** nella Room Dome abbia il componente **PlantCardV3TerminalOpener** e che il player possa interagire (es. tasto E o click).

- **Le posizioni di Zona 2 e Zona 3 non si salvano**  
  Il salvataggio usa **PlayerPrefs**; se giochi in un build o con profilo diverso, le posizioni possono essere “resettate”. Riposiziona i blocchi trascinandoli e richiudi il terminale per salvare di nuovo.
