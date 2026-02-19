# Istruzioni Unity – Food Room, Idratazione, Consumo inventario

Dopo l’implementazione in codice, in Unity vanno eseguite queste operazioni.

---

## 1. Asset Config Food Room

- **FoodRoomConfig**
  - In Unity: **Create → Sporae → FoodRoomConfig**.
  - Salva l’asset in **`Assets/Resources/Configs/`** con nome **`FoodRoomConfig`** (es. `FoodRoomConfig.asset`).
  - Imposta i valori (già sensati di default dall’inspector):
    - Max Slots: 1 (o 2–3 se vuoi più slot).
    - Vegetable/Fungus/Meat: giorni, quantità output, CRY/giorno, bonus azioni come da GDD.

Se l’asset non è in `Resources/Configs/FoodRoomConfig`, il gioco userà valori di fallback (1 slot, 1 CRY/giorno, ecc.).

---

## 2. ItemConfig per i nuovi item

L’inventario e l’ItemFabric usano **ItemConfig** in **`Resources/Items/`** con nome file = **typeId**.

Crea **5 nuovi ItemConfig** (tasto destro in `Assets/Resources/Items/` → Create → Game → ItemData, oppure duplica un item esistente e rinomina):

| Nome file asset (typeId) | TypeId (Inspector) | Uso |
|--------------------------|--------------------|-----|
| **FOOD-101**             | FOOD-101           | Vegetali sintetici |
| **FOOD-201**             | FOOD-201           | Funghi sintetici |
| **FOOD-301**             | FOOD-301           | Carne sintetica |
| **WAT-POT**              | WAT-POT            | Acqua potabile |
| **ORG-RES-001**          | ORG-RES-001        | Residui proteici (carne) |

In ogni ItemConfig imposta **TypeId** uguale al nome (es. `FOOD-101`), **MaxQuality**, **SellPrice/BuyPrice** e gli altri campi come per gli item esistenti.  
Se questi asset non ci sono, **aggiungere cibo/acqua/residui dall’inventario (es. Harvest/Raccogli)** fallirà (CreateItemByType restituisce null).

---

## 3. Pannello Food Room (UIDocument)

- Nella scena dove c’è la **Kitchen** (o dove vuoi la Food Room):
  - Crea un **GameObject** (es. `FoodRoomPanel`).
  - Aggiungi **UIDocument**.
  - Assegna **Source Asset** = `Assets/_Project/UI/UIToolkit/FoodRoom/FoodRoomPanel.uxml`.
  - Assegna **Panel Settings** (lo stesso usato per gli altri HUD UIToolkit, es. da PlayerInventory o Lab).
- Aggiungi lo script **FoodRoomPanelController** allo stesso GameObject.
- Se il tema/stile non si applica: nel **UIDocument** verifica che il **Style** sia risolto (es. `FoodRoomPanel.uss` nella stessa cartella del UXML). Se Unity non trova l’USS, assegna manualmente il foglio di stile nel UXML o nel Panel Settings.

---

## 4. Macchina Food Room (FoodSynthMachine) in scena

- Nella scena **Kitchen** (o stanza Food Room):
  - Scegli o crea un GameObject per la **macchina** (es. sprite/placeholder).
  - Aggiungi **Interactable** (distanza interazione a piacere, es. 2).
  - Aggiungi **FoodSynthMachine**.
  - Nel campo **Food Room Panel** di FoodSynthMachine assegna il **FoodRoomPanelController** (il GameObject con UIDocument + FoodRoomPanel.uxml).  
    Se lasci vuoto, lo script prova a trovarlo in scena con `FindObjectOfType<FoodRoomPanelController>`.

Verifica che il **Player** abbia il tag `Player` (per il check distanza di Interactable).

---

## 5. Barra idratazione (Player Status Panel)

- **PlayerStatusPanelController** è già collegato al **PlayerHydrationSystem** via codice (ServiceContainer + GameManager).
- Non è obbligatorio assegnare nulla in Inspector: alla prima apertura del pannello status, la barra **HYDRATION** e i messaggi sotto (es. “Bassa idratazione: -1 azione domani”) usano i dati reali.
- Se il pannello status non è in scena, aggiungilo dove hai gli altri HUD (es. BIOLOGO STATUS) e assicurati che abbia **UIDocument** con **PlayerStatusPanel.uxml**.

---

## 6. Inventario e consumo

- L’inventario principale (tasto **INV** / Biologo) è **PlayerInventoryPanelController** (UIToolkit).
- Per i typeId consumabili (FOOD-101/201/301, WAT-POT, WAT-RAW, Fruits) è già presente il pulsante **Mangia** / **Bevi** / **Usa** sulla riga.
- Non serve configurare nulla in più, a patto che **GameManager** e **ItemConsumptionHandler** siano attivi (già inizializzati in **GameManager.Awake**).

---

## 7. Salvataggio / caricamento

- **SaveManager** salva e ripristina **idratazione** e **stato Food Room** (slot produzione + slot idrico).
- I salvataggi **vecchi** (senza questi campi) vengono gestiti con valori di default (idratazione 100%, Food Room vuoto).  
Non serve fare nulla in Inspector per il save.

---

## 8. Riepilogo checklist

- [ ] Asset **FoodRoomConfig** in `Resources/Configs/FoodRoomConfig`.
- [ ] **5 ItemConfig** in `Resources/Items/`: FOOD-101, FOOD-201, FOOD-301, WAT-POT, ORG-RES-001.
- [ ] **UIDocument** con **FoodRoomPanel.uxml** e **FoodRoomPanelController** in scena (es. sotto Kitchen).
- [ ] **FoodSynthMachine** + **Interactable** sul GameObject della macchina; riferimento al **FoodRoomPanelController** (o lasciare vuoto per FindObjectOfType).
- [ ] **Player** con tag `Player` (per Interactable).
- [ ] **Player Status Panel** in scena con **PlayerStatusPanel.uxml** (per barra idratazione reale).

Dopo questi passi il flusso Food Room + Idratazione + Consumo inventario è utilizzabile in gioco.
