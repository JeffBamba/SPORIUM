# Player Inventory Panel — componente unico inventario

Pannello **unico e definitivo** per visualizzare e selezionare gli oggetti dell'inventario del giocatore.

## Ruolo

- **Modalità view (tasto INV / Biologo Player)**: mostra tutti gli oggetti in inventario, nessuna selezione. Apertura/chiusura con il pulsante Inventario nella HUD Biologo (o tasto INV se collegato).
- **Modalità picker (Lab e altre interazioni)**: stessa finestra; gli item **compatibili con il contesto** (es. Extractor: frutti, scarti) sono **selezionabili**; gli altri sono visibili ma **non selezionabili** (riga disabilitata).

Usare **sempre** questo componente quando serve:
- aprire l'inventario dal tasto INV / sezione Biologo;
- far scegliere un item da inserire in un macchinario Lab (Extractor, Catalizzatore, Pipette, Incubatore, ecc.);
- future interazioni che richiedono "scegli un item dall'inventario".

## Setup in Unity

1. **GameObject** (es. sotto Canvas): crea un figlio di **Canvas**, rinominalo **UI_PlayerInventoryPanel**.
2. **UIDocument**: aggiungi **UI Document**; **Source Asset** = `PlayerInventoryPanel.uxml` (da `Assets/_Project/UI/UIToolkit/PlayerInventory/`); **Panel Settings** = stesso usato per gli altri pannelli (es. PlayerStatusPanelSettings).
3. **Controller**: aggiungi **Player Inventory Panel Controller** (script).
4. **Sincronizzazione INV**: nel **Player Status Panel** (HUD Biologo), nel componente **Player Status Panel Controller**, assegna **Player Inventory Panel** al campo omonimo. Il pulsante Inventario aprirà questo pannello.
5. **Lab**: in ogni pannello Lab che ha "Seleziona" (Extractor, Catalizzatore, Pipette, ecc.), nel rispettivo controller assegna **Player Inventory Panel** al campo dedicato; se non assegnato, viene cercato in scena con `FindObjectOfType`.

## Uso da codice

- **Solo visualizzazione (INV)**  
  `playerInventoryPanel.Show();` oppure `playerInventoryPanel.Toggle();`

- **Selezione item per un contesto (Lab "Seleziona")**  
  `playerInventoryPanel.ShowAsPicker(allowedTypeIds, "Seleziona item per...", onSelected, onCancel);`  
  - `allowedTypeIds`: insieme di typeId che in quel contesto sono validi (es. Extractor: Fruits, WholePlant, …).  
  - Item nell'inventario ma non in `allowedTypeIds`: riga visibile ma non selezionabile.  
  - `onSelected(typeId)`: chiamato quando l'utente seleziona un item valido.  
  - `onCancel`: chiamato su Annulla/chiudi senza selezione.

## Riferimenti

- `Assets/_Project/Docs/GUIDA_LAB_MACCHINARIO_PER_MACCHINARIO.md` — setup Lab e uso del picker.
- `Assets/_Project/Docs/SceneHierarchy.txt` — dove collocare **UI_PlayerInventoryPanel** (sotto Canvas).
