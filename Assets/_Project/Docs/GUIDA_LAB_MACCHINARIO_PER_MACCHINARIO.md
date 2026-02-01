# Guida Lab: Macchinario per Macchinario (Setup Unity + Test)

Istruzioni **passo passo per principianti**. Tutti i nomi di GameObject e percorsi seguono **`Assets/_Project/Docs/SceneHierarchy.txt`**.

Per ogni macchinario: **cosa impostare in Unity** (click per click) e **cosa testare** per confermare che funziona.

---

## Riferimenti dalla gerarchia scena (SceneHierarchy.txt)

- **Canvas**: contiene tutta l’UI (HUD, inventario, pannelli Lab legacy). I nuovi pannelli UIToolkit vanno creati **sotto Canvas** o come fratelli degli altri pannelli Lab.
- **ROOM_Dome**: contiene i macchinari Lab:
  - **Extractor** (componenti: Extractor, Interactable)
  - **Catallizatore** (nome in scena con doppia “l”; componenti: Catalizzatore, Interactable)
  - **Pipette** (componenti: Pipette, Interactable)
- **Canvas** contiene anche: **UI_LabMinigame** (Extractor legacy), **UI_Catalizzatore** (Catalizzatore legacy), **UI_LabPippete** (Pipette legacy), **UI_Incubator** (Incubator legacy).
- **BTN_EndDay**: sotto Canvas, per avanzare il giorno nei test.
- **UI_Inventory**: sotto Canvas; inventario giocatore.
- **GlobalStateInspector**: in **root** della gerarchia (non sotto Canvas); console debug, tasto **F1** in Play.
- **ToastNotificationSystem**: sotto Canvas; toast di gioco.
- **Notifications Foundation**: in root; UIDocument + FoundationNotificationsPanelController (toast Foundation).

Se nella tua scena **Incubator** non esiste come GameObject, va creato (es. sotto **ROOM_Dome**) e aggiunto il componente **Incubator**; in questa guida si dà per scontato che tu lo crei dove previsto dal livello.

### Verifica gerarchia (SceneHierarchy.txt)

- In **SceneHierarchy.txt** attuale compaiono **UI_LabMinigame**, **UI_LabMicroscope**, **UI_LabPippete** (pannelli Lab legacy), ma **non** i nuovi pannelli UIToolkit (**UI_LabExtractorPanel**, **UI_LabCatalizzatorePanel**, **UI_LabFusionPanel**, **UI_LabIncubatorPanel**) finché non li crei in scena.
- Dopo aver seguito i passi per l’Extractor, in **Unity Hierarchy** sotto **Canvas** deve comparire **UI_LabExtractorPanel** con componenti **UIDocument** e **Lab Extractor Panel Controller**. Se non c’è, ripeti i passi “Creare il GameObject del pannello” e “Aggiungere UIDocument / controller”.
- Frammento atteso dopo la configurazione Extractor:
  - **Canvas**
    - … (altri figli esistenti: HUD_GameViewportBackground, UI_LabMinigame, ecc.)
    - **UI_LabExtractorPanel**  
      [Component] RectTransform, **UIDocument**, **Lab Extractor Panel Controller**
- Per Catalizzatore, Fusione e Incubatore valgono gli stessi controlli (nomi **UI_LabCatalizzatorePanel**, **UI_LabFusionPanel**, **UI_LabIncubatorPanel** sotto Canvas).

---

## Prerequisiti (una tantum)

Esegui questi passaggi **una sola volta** prima di configurare i macchinari.

### 1. Creare gli ItemConfig per il Lab

1. In Unity, apri il menu in alto.
2. Clicca **Tools** → **Sporae** → **Create Lab ItemConfig Assets (CELL, RES-PROT, REAG)**.
3. Nel Project, vai in **Assets/_Project/Resources/Items/** (o dove sono gli ItemConfig).
4. Verifica che esistano i file/asset: **CELL-001**, **CELL-002**, **CELL-003**, **RES-PROT-001**, **REAG-X**, **REAG-Y**. Se mancano, ripeti il passo 2.

### 2. Creare LabUpgradesConfig (per modulo Cellule Staminali)

1. Nel Project, tasto destro nella cartella **Assets/_Project/Resources/** (creala se non c’è).
2. **Create** → **Game** → **Lab Upgrades Config** (oppure dal menu **Assets** se il percorso è diverso).
3. Rinomina l’asset in **LabUpgradesConfig** e lascialo in **Resources** così può essere caricato a runtime.

### 3. Componente unico inventario (PlayerInventoryPanel)

1. Crea il pannello **unico inventario** usato da tutti i macchinari Lab per “Seleziona” e dal tasto **INV** (Biologo Player). Vedi **`Assets/_Project/UI/UIToolkit/PlayerInventory/README_PlayerInventory.md`**.
2. In **Hierarchy** sotto **Canvas** crea un GameObject **UI_PlayerInventoryPanel**; aggiungi **UIDocument** (Source Asset = **PlayerInventoryPanel.uxml**, Panel Settings come per gli altri pannelli) e **Player Inventory Panel Controller**.
3. Nel **Player Status Panel** (HUD Biologo), componente **Player Status Panel Controller**: assegna **Player Inventory Panel** al campo omonimo così il pulsante Inventario (INV) apre questo pannello.
4. Nei pannelli Lab (Extractor, Catalizzatore, Pipette, Incubatore) assegna **Player Inventory Panel** al campo dedicato nel rispettivo controller; altrimenti viene cercato in scena.

### 4. Console debug (GlobalStateInspector)

1. In **Hierarchy**, cerca il GameObject **GlobalStateInspector** (in root, non sotto Canvas).
2. Se non c’è, aggiungilo alla scena (dove previsto dal progetto).
3. In **Play**, premi **F1** per aprire la console; nella sezione **Inventory** usa **“Aggiungi item (typeId)”** per aggiungere item a runtime (es. frutti, spore).

---

## 1. EXTRACTOR (Step 1 – Estrazione)

### Cosa impostare in Unity (passo passo)

**Passo 1 – Creare il GameObject del pannello**

1. In **Hierarchy** seleziona **Canvas**.
2. Tasto destro su **Canvas** → **Create Empty**. Si crea un figlio “GameObject”.
3. Rinomina il figlio in **UI_LabExtractorPanel** (come gli altri pannelli Lab sotto Canvas, es. UI_LabMinigame).

**Passo 2 – Aggiungere UIDocument**

1. Con **UI_LabExtractorPanel** selezionato, in **Inspector** clicca **Add Component**.
2. Cerca **UI Document** e seleziona **UI Document** (Unity Engine).
3. Nel componente **UI Document**:
   - **Source Asset**: clicca il cerchietto a destra, cerca **LabExtractorPanel** e assegna **LabExtractorPanel.uxml** (da `Assets/_Project/UI/UIToolkit/Lab/`).
   - **Panel Settings** (obbligatorio): se è **None**, l’UI non viene mai mostrata. Clicca il cerchietto e assegna un asset Panel Settings (es. **PlayerStatusPanelSettings** da `Assets/_Project/UI/UIToolkit/`). Se nel progetto non ce n’è uno: **Create > UI Toolkit > Panel Settings Asset**, salvalo in `Assets/_Project/UI/UIToolkit/` e assegnarlo qui.
4. **Sort Order**: puoi lasciare 0; il controller lo imposta a 400 in Awake (opzionale: metti 400 subito).

**Passo 3 – Aggiungere il controller del pannello**

1. Sempre su **UI_LabExtractorPanel**, **Add Component**.
2. Cerca **Lab Extractor Panel Controller** (o **LabExtractorPanelController**) e aggiungilo.
3. Nel componente **Lab Extractor Panel Controller**:
   - **Ui Document**: puoi lasciare **None**; lo script lo prende dallo stesso GameObject con `GetComponent<UIDocument>()`. Se vuoi assegnarlo: trascina **UI_LabExtractorPanel** (o il componente UIDocument) nel campo.
   - **Extractor**: clicca il cerchietto e cerca **Extractor**, oppure trascina dalla Hierarchy il GameObject **ROOM_Dome** → **Extractor**.
   - **Player Inventory Panel**: assegna il GameObject **UI_PlayerInventoryPanel** (componente unico inventario). Se **None**, viene cercato in scena; “Seleziona” aprirà comunque il pannello inventario in modalità picker (solo item compatibili con Extractor selezionabili).
   - **Lab Upgrades Config**: opzionale; puoi lasciare **None** (verrà caricato da Resources/LabUpgradesConfig) oppure trascinare l’asset **LabUpgradesConfig** da Resources.

**Passo 4 – Collegare il pannello all’Extractor in scena**

1. In **Hierarchy** espandi **ROOM_Dome** e seleziona **Extractor**.
2. Nell’**Inspector** trova il componente **Extractor** (script).
3. Nel campo **Lab Extractor Panel** (o simile): trascina **UI_LabExtractorPanel** dalla Hierarchy, oppure usa il cerchietto e seleziona **UI_LabExtractorPanel**.
4. Salva la scena (Ctrl+S).

Riepilogo riferimenti (SceneHierarchy):

- Pannello: **Canvas** → **UI_LabExtractorPanel** (nuovo, con **UIDocument** + **Lab Extractor Panel Controller**).
- Macchinario: **ROOM_Dome** → **Extractor** (campo **Lab Extractor Panel** punta a **UI_LabExtractorPanel**).

---

### Cosa testare (passo passo)

1. Premi **Play**.
2. Vai nel Lab (stanza Dome): muovi il personaggio fino al macchinario **Extractor**.
3. **Interagisci** con l’Extractor (click o azione prevista dal gioco).
4. **Verifica**: si apre il pannello **EXTRACTOR** (UIToolkit) con titolo “EXTRACTOR”, slot Input, pulsante “AVVIA ESTRAZIONE”, slot Output, pulsante “Ritira”.
5. **Input vuoto**: lo slot Input deve mostrare “—”; il pulsante “AVVIA ESTRAZIONE” deve essere **disabilitato**.
6. **Aggiungi 1 frutto** nell’Extractor:
   - Opzione A: se c’è drag & drop dall’inventario, trascina un frutto (es. **fruits-001**) nello slot Input del pannello o sul macchinario.
   - Opzione B: premi **F1** → sezione **Inventory** → aggiungi 1 item con typeId **fruits-001**; poi metti il frutto nell’Extractor con il flusso previsto (drag o pulsante).
7. **Verifica**: nello slot **Input** compare “fruits-001 x1”; **AVVIA ESTRAZIONE** è **abilitato** (se hai almeno 1 azione).
8. Clicca **AVVIA ESTRAZIONE**: consuma 1 azione e 1 frutto; nello slot **Output** deve comparire “spore-generic x1”.
9. Clicca **Ritira**: l’inventario del giocatore riceve 1 spora; deve comparire un **toast** (es. “SPORE-001” o messaggio di conferma).
10. Chiudi il pannello con **×** e riaprilo: Output vuoto, Input vuoto se non hai più frutti.

### Test modulo Cellule Staminali (opzionale)

1. In **Resources** apri l’asset **LabUpgradesConfig** e spunta **Has Stem Cell Module**.
2. In Play, apri l’Extractor e inserisci **1 frutto**; **AVVIA** → in Output devono comparire **spore + CELL-002** (es. “spore-generic x1, CELL-002 x1”). **Ritira** → entrambi in inventario.
3. Con modulo attivo: inserisci **Whole Plant** o **Organic Scrap** (org-scr-001); **AVVIA** → Output **CELL-001**. Ritira.
4. Inserisci **RES-PROT-001** (aggiungi da console F1 se serve); **AVVIA** → Output **CELL-003**. Ritira.

---

## 2. CATALIZZATORE (Step 2 – Maturazione)

**Nota:** nella gerarchia il GameObject si chiama **Catallizatore** (con doppia “l”). Il componente script è **Catalizzatore**.

### Cosa impostare in Unity (passo passo)

**Passo 1 – Creare il GameObject del pannello**

1. In **Hierarchy** seleziona **Canvas**.
2. Tasto destro su **Canvas** → **Create Empty**.
3. Rinomina in **UI_LabCatalizzatorePanel**.

**Passo 2 – Aggiungere UIDocument**

1. Seleziona **UI_LabCatalizzatorePanel** → **Add Component** → **UI Document**.
2. **Source Asset**: cerchietto → cerca **LabCatalizzatorePanel** e assegna **LabCatalizzatorePanel.uxml** (da `Assets/_Project/UI/UIToolkit/Lab/`).

**Passo 3 – Aggiungere il controller**

1. **Add Component** → **Lab Catalizzatore Panel Controller** (o **LabCatalizzatorePanelController**).
2. Nel controller:
   - **Catalizzatore**: trascina dalla Hierarchy **ROOM_Dome** → **Catallizatore** (il GameObject con il componente **Catalizzatore**).
   - **Player Inventory Panel**: assegna **UI_PlayerInventoryPanel** (componente unico inventario). Se **None**, viene cercato in scena; il pulsante **Seleziona** aprirà il pannello inventario in modalità picker (solo spora Raw selezionabile).

**Passo 4 – Collegare il pannello al macchinario**

1. In **Hierarchy** seleziona **ROOM_Dome** → **Catallizatore**.
2. Nell’**Inspector**, componente **Catalizzatore**: campo **Lab Catalizzatore Panel** → trascina **UI_LabCatalizzatorePanel**.
3. Salva la scena.

Riepilogo riferimenti (SceneHierarchy):

- Pannello: **Canvas** → **UI_LabCatalizzatorePanel** (UIDocument + **Lab Catalizzatore Panel Controller**).
- Macchinario: **ROOM_Dome** → **Catallizatore** (componente **Catalizzatore**; campo **Lab Catalizzatore Panel** = **UI_LabCatalizzatorePanel**).

---

### Cosa testare (passo passo)

1. **Play** → vai al Lab e **interagisci con il Catallizatore**.
2. Si apre il pannello **CATALIZZATORE** (titolo “CATALIZZATORE”, Stato, “Operazione in corso” nascosto, Input spora Raw, “AVVIA MATURAZIONE”, Output, Ritira).
3. **Senza spore**: slot Input “—”; **AVVIA MATURAZIONE** disabilitato.
4. Clicca **Seleziona** (accanto allo slot Input): si apre il pannello inventario in modalità picker; seleziona **spore-generic** → 1 spora viene prelevata dall’inventario e inserita nel Catalizzatore. In alternativa: drag & drop dall’inventario. Nel pannello: Input “spore-generic x1”; **AVVIA MATURAZIONE** abilitato (se hai almeno 1 azione).
5. Clicca **AVVIA MATURAZIONE**: consuma 1 azione e 1 spora; compare la label **“Operazione in corso”** e lo stato indica giorno 1 (es. “Maturazione in corso (giorno 1)”).
6. **Avanza 1 giorno**: usa il pulsante **BTN_EndDay** (sotto **Canvas**) o la meccanica End Day del gioco. Lo stato deve passare a giorno 2.
7. **Avanza ancora 1 giorno**: l’operazione si completa; nello slot **Output** compare “spore-generic (maturata) x1”; **Ritira** abilitato.
8. Clicca **Ritira**: inventario riceve 1 spora; compare un **toast** (es. INV-SPR o conferma).
9. Chiudi e riapri: Output vuoto; “Operazione in corso” nascosta.

---

## 3. FUSIONE / PIPETTE (Step 3 – Fusione)

### Cosa impostare in Unity (passo passo)

**Passo 1 – Creare il GameObject del pannello**

1. **Hierarchy** → **Canvas** → tasto destro → **Create Empty**.
2. Rinomina in **UI_LabFusionPanel**.

**Passo 2 – UIDocument**

1. Su **UI_LabFusionPanel** → **Add Component** → **UI Document**.
2. **Source Asset** → **LabFusionPanel.uxml** (da `Assets/_Project/UI/UIToolkit/Lab/`).

**Passo 3 – Controller**

1. **Add Component** → **Lab Fusion Panel Controller** (o **LabFusionPanelController**).
2. Nel controller:
   - **Pipette**: trascina **ROOM_Dome** → **Pipette**.
   - **Player Inventory Panel**: assegna **UI_PlayerInventoryPanel**. Se **None**, viene cercato in scena; **Seleziona** aprirà il picker (solo spore mature selezionabili).

**Passo 4 – Collegare al macchinario**

1. Seleziona **ROOM_Dome** → **Pipette**.
2. Componente **Pipette**: campo **Lab Fusion Panel** → trascina **UI_LabFusionPanel**.
3. Salva la scena.

Riepilogo riferimenti (SceneHierarchy):

- Pannello: **Canvas** → **UI_LabFusionPanel** (UIDocument + **Lab Fusion Panel Controller**).
- Macchinario: **ROOM_Dome** → **Pipette** (**Lab Fusion Panel** = **UI_LabFusionPanel**).

---

### Cosa testare (passo passo)

1. **Play** → Lab → **interagisci con la Pipette**.
2. Si apre il pannello **FUSIONE** (titolo “FUSIONE”, “Seleziona 2 spore mature”, slot Spore, “CONFERMA FUSIONE”, Output Pre-Seed, Ritira).
3. **Senza 2 spore**: slot Spore “—”; **CONFERMA FUSIONE** disabilitato.
4. Clicca **Seleziona** (accanto allo slot Spore) una o due volte per inserire spore dall’inventario (picker: solo spore-generic). In alternativa: drag & drop. Slot “spore-generic x2”; **CONFERMA FUSIONE** abilitato (se hai almeno 1 azione).
5. Clicca **CONFERMA FUSIONE**: consuma 2 spore e 1 azione; in **Output** compare “Pre-Seed x1”; **Ritira** abilitato.
6. Clicca **Ritira**: inventario riceve 1 Pre-Seed; **toast** (es. LAB-GRF-OK).
7. Chiudi e riapri: Output vuoto.

---

## 4. INCUBATORE (Step 4 – Incubazione)

**Nota:** in **SceneHierarchy.txt** non è presente un GameObject “Incubator” sotto ROOM_Dome. Se nella tua scena non c’è, crea un GameObject (es. sotto **ROOM_Dome**), rinominalo **Incubator**, aggiungi **Interactable** e lo script **Incubator**, poi segui sotto.

### Cosa impostare in Unity (passo passo)

**Passo 1 – Creare il pannello UI**

1. **Canvas** → tasto destro → **Create Empty** → rinomina **UI_LabIncubatorPanel**.

**Passo 2 – UIDocument**

1. **UI_LabIncubatorPanel** → **Add Component** → **UI Document**.
2. **Source Asset** → **LabIncubatorPanel.uxml** (da `Assets/_Project/UI/UIToolkit/Lab/`).

**Passo 3 – Controller**

1. **Add Component** → **Lab Incubator Panel Controller** (o **LabIncubatorPanelController**).
2. **Player Inventory Panel**: assegna **UI_PlayerInventoryPanel**. Se **None**, viene cercato in scena; **Seleziona** (accanto a Pre-Seed) aprirà il picker per confermare/visualizzare il Pre-Seed (spore-generic) in inventario.

**Passo 4 – Collegare all’Incubatore**

1. Seleziona il GameObject **Incubator** in scena (sotto ROOM_Dome o dove l’hai creato).
2. Se non ha ancora il componente **Incubator**: **Add Component** → cerca **Incubator** (script, es. `Interactables/Incubator.cs`).
3. Nel componente **Incubator**: campo **Lab Incubator Panel** → trascina **UI_LabIncubatorPanel**. **Legacy Incubator UI** = opzionale (per fallback alla UI vecchia).
4. Salva la scena.

Riepilogo riferimenti:

- Pannello: **Canvas** → **UI_LabIncubatorPanel** (UIDocument + **Lab Incubator Panel Controller**).
- Macchinario: GameObject **Incubator** (es. sotto **ROOM_Dome**) con componente **Incubator**; **Lab Incubator Panel** = **UI_LabIncubatorPanel**.

---

### Cosa testare (passo passo)

1. **Play** → Lab → **interagisci con l’Incubatore**.
2. Si apre il pannello **INCUBATORE** (titolo “INCUBATORE”, slot Pre-Seed, Reagente Nessuno/X/Y, “AVVIA INCUBAZIONE”, Output seme, Ritira).
3. **Senza Pre-Seed**: slot Pre-Seed “—”; **AVVIA INCUBAZIONE** disabilitato.
4. Aggiungi **1 spora** in inventario (F1 → Inventory, oppure **Seleziona** → picker → spore-generic per confermare). Slot Pre-Seed mostra “Pre-Seed (1)”; **AVVIA INCUBAZIONE** abilitato (se hai almeno 1 azione).
5. Scegli **Reagente**: Nessuno / X / Y (pulsanti; quello selezionato si evidenzia).
6. Clicca **AVVIA INCUBAZIONE**: consuma 1 Pre-Seed e 1 azione; slot Pre-Seed “—”; Output ancora vuoto.
7. **Avanza 1 giorno** (usa **BTN_EndDay** sotto **Canvas**): in **Output** deve comparire il seme (es. “seed-001 x1”); **Ritira** abilitato.
8. Clicca **Ritira**: inventario riceve il seme; **toast** (es. LAB-INC-OK).
9. Chiudi e riapri: Output vuoto.

---

## Riepilogo checklist per macchinario

| Macchinario | Dove in scena (SceneHierarchy) | Pannello da creare sotto Canvas | Cosa collegare |
|-------------|--------------------------------|----------------------------------|----------------|
| **Extractor** | ROOM_Dome → **Extractor** | **UI_LabExtractorPanel** (UIDocument + Lab Extractor Panel Controller) | Extractor → Lab Extractor Panel = UI_LabExtractorPanel; Controller → Extractor = Extractor |
| **Catalizzatore** | ROOM_Dome → **Catallizatore** | **UI_LabCatalizzatorePanel** (UIDocument + Lab Catalizzatore Panel Controller) | Catallizatore → Lab Catalizzatore Panel = UI_LabCatalizzatorePanel; Controller → Catalizzatore = Catallizatore |
| **Fusione (Pipette)** | ROOM_Dome → **Pipette** | **UI_LabFusionPanel** (UIDocument + Lab Fusion Panel Controller) | Pipette → Lab Fusion Panel = UI_LabFusionPanel; Controller → Pipette = Pipette |
| **Incubatore** | Creare **Incubator** se assente (es. sotto ROOM_Dome) | **UI_LabIncubatorPanel** (UIDocument + Lab Incubator Panel Controller) | Incubator → Lab Incubator Panel = UI_LabIncubatorPanel |

---

## Se qualcosa non funziona

- **Clicco sul macchinario ma non si apre nulla** (caso tipico Extractor):
  1. **Avvicinati al macchinario** (quasi sopra) e premi il tasto **E**. Se con E si apre il pannello, il problema è il **click** (vedi sotto). Se neanche E funziona, vai al punto 2.
  2. **Distanza di interazione**: in Hierarchy seleziona **ROOM_Dome** → **Extractor**. Nell’Inspector, componente **Interactable**: il campo **Interact Distance** deve essere > 0 (es. **2**). Se è 0, il giocatore non è mai “in range”; imposta **2** e riprova.
  3. **Click bloccato dalla UI**: l’Interactable ignora il click se il puntatore è sopra un elemento UI (HUD, inventario, pulsanti). In Play, apri la **Console** (Window → General → Console): se vedi il messaggio *“click ignorato — puntatore sopra UI”*, clicca sull’Extractor **senza** che il cursore sia sopra HUD/inventario, oppure usa sempre **E** per aprire.
  4. **Pannello non assegnato**: se in Console compare *“Nessun pannello assegnato”*, sull’**Extractor** (Inspector) nel componente **Extractor** il campo **Lab Extractor Panel** deve puntare al GameObject **UI_LabExtractorPanel** (quello che ha UIDocument + Lab Extractor Panel Controller). Trascinalo dalla Hierarchy se è vuoto.
- **Pannello non si apre**: sul macchinario (Extractor / Catallizatore / Pipette / Incubator) il campo **Lab … Panel** deve puntare al GameObject del pannello (es. UI_LabExtractorPanel). Verifica che il pannello sia **attivo** in Hierarchy (checkbox attiva).
- **UIDocument vuoto / nero / avviso “assign a PanelSettings asset”**: sul componente **UI Document** il campo **Panel Settings** non deve essere **None**. Assegna un asset Panel Settings (es. **PlayerStatusPanelSettings** da `Assets/_Project/UI/UIToolkit/`). Se non ce n’è: **Create > UI Toolkit > Panel Settings Asset**, salvalo e assegnarlo al UIDocument.
- **UIDocument vuoto / nero (Source Asset ok)**: **Source Asset** del UIDocument deve essere il file `.uxml` corretto (es. LabExtractorPanel.uxml). Controlla che la cartella `Assets/_Project/UI/UIToolkit/Lab/` sia importata.
- **“Cannot find item config”**: esegui **Tools > Sporae > Create Lab ItemConfig Assets** e controlla in **Resources/Items/** che gli asset abbiano **Type Id** uguale al nome previsto (es. CELL-001).
- **Modulo Cellule Staminali non produce CELL-002/001/003**: in **LabUpgradesConfig** (in Resources) spunta **Has Stem Cell Module**; nel **Lab Extractor Panel Controller** verifica che Lab Upgrades Config sia assegnato o lasciato vuoto per caricamento da Resources.
- **Catalizzatore non avanza dopo 1 giorno**: il controller è iscritto a **DayCycleSystem.OnDayChanged**. Avanza il giorno con **BTN_EndDay** (sotto **Canvas**) o con la meccanica End Day del gioco.
- **Toast non compaiono**: in scena devono essere presenti **ToastNotificationSystem** (sotto Canvas) e/o **Notifications Foundation** (root). Il sistema Foundation Notification deve essere attivo per i toast Lab.

---

**Riferimenti:** `Assets/_Project/Docs/SceneHierarchy.txt`, `Assets/_Project/Docs/SETUP_UNITY_PRIMA_DEI_TEST.md`, `Assets/_Project/UI/UIToolkit/Lab/README_LAB_PANELS.md`.
