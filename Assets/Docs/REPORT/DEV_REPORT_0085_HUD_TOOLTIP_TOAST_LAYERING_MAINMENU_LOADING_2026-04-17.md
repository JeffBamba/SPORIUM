# DEV REPORT 0085 — Layering tooltip HUD vs toast Foundation + caricamento Nuova partita nel menu

**Data:** 2026-04-17  
**Sprint / contesto:** correzione ordine di rendering UI Toolkit (HUD TopBar / Compact Bottom Bar vs notifiche toast) e rifinitura UX del blocco caricamento “Nuova partita” nel menu principale UI Toolkit.  
**Riferimento piano:** —  
**Report precedente:** `DEV_REPORT_0084_FUN_IMPROVEMENTS_WORKSTREAM_A_E_2026-04-17.md`

---

## Sommario interventi

1. Risolto il caso in cui i **toast Foundation** risultavano **sopra** ai **tooltip** della TopBar e della bottom bar compatta: la causa era l’uso di **Panel Settings diversi** tra HUD e pannello notifiche, per cui il `sortingOrder` dei `UIDocument` non era confrontabile.
2. Allineati **sorting order** relativi nella stessa “pila” HUD (`PlayerStatusPanelSettings`) e aggiornati costanti EoD / dettaglio collection box per coerenza con la nuova scala.
3. Spostato il blocco **progress bar** del caricamento scena **sotto l’ultima voce** della lista azioni del menu (stessa larghezza della colonna bottoni), rimuovendo l’overlay fullscreen che copriva i pulsanti; durante il caricamento i bottoni del menu vengono **disabilitati**.

---

## 1. Tooltip HUD sopra i toast Foundation

### Problema
I tooltip di TopBar e Compact Bottom Bar apparivano **dietro** ai toast del pannello Foundation, nonostante valori numerici di `UIDocument.sortingOrder` impostati in codice (es. HUD 200 vs toast 150).

### Soluzione
- Chiarito il comportamento Unity UI Toolkit: il **`sortingOrder` sul `UIDocument` confronta solo documenti che condividono lo stesso asset di `Panel Settings`**. Con Foundation su `FoundationNotificationsPanel.asset` e TopBar su `PlayerStatusPanelSettings`, l’ordine relativo non era definito in modo affidabile.
- In **`SCN_VaultMap.unity`**, il `UIDocument` del GameObject **Notifications Foundation** usa ora **`PlayerStatusPanelSettings`** (stesso della HUD principale), con riferimento serializzato **`_uiDocument`** corretto al componente `UIDocument` locale.
- Mantenuti in codice gli ordini relativi: toast Foundation **150**, TopBar / Compact Bottom Bar / Bottom Navigation **200**; **End of Day** portato a **2500** per restare sopra HUD e toast; dettaglio **Collection box** a **350** (sopra toast/HUD, sotto PlantCard).

**File principali:** `FoundationNotificationsPanelController.cs`, `SCN_VaultMap.unity`, `TopBarController.cs`, `CompactBottomBarController.cs`, `BottomNavigationController.cs`, `EndOfDaySequenceController.cs`, `CollectionBoxStackController.cs`

---

## 2. Menu principale — barra di caricamento sotto l’ultimo bottone

### Problema
Premendo **Nuova partita**, il blocco di caricamento (testo + progress) era in un **overlay a schermo intero** centrato, con sfondo scuro, che copriva visivamente i bottoni del menu.

### Soluzione
- Spostato il contenitore **`loading-overlay`** nel **flusso flex** di `main-menu-content`, **subito dopo** `action-list`, così compare **sotto** l’ultimo pulsante (“Esci da Sporium”), larghezza **500px** allineata alla lista.
- Aggiornato **`MainMenu.uss`**: colonna esplicita su `#main-menu-content`; `#loading-overlay` non è più fullscreen né con backdrop opaco su tutta la vista.
- In **`MainMenuUIToolkitController`**, durante `LoadNewGameAsync`, **`SetMainMenuButtonsEnabled(false)`** sui bottoni menu (incluso Impostazioni) per evitare click mentre l’overlay fullscreen non blocca più l’input.

**File principali:** `MainMenu.uxml`, `MainMenu.uss`, `MainMenuUIToolkitController.cs`

---

## File modificati (tabella)

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scenes/SCN_VaultMap.unity` | Panel Settings Notifications Foundation → `PlayerStatusPanelSettings`; serializzazione `_uiDocument` |
| `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/FoundationNotificationsPanelController.cs` | Commento su stesso Panel Settings + `sortingOrder` toast |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs` | `sortingOrder` HUD (200) |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/CompactBottomBarController.cs` | `sortingOrder` (200) |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/BottomNavigationController.cs` | `sortingOrder` (200) |
| `Assets/_Project/Scripts/UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs` | `EodSortingOrder` (2500) |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/CollectionBoxStackController.cs` | Ordine dettaglio / restore default allineati alla scala HUD |
| `Assets/_Project/Resources/UI/UIToolkit/MainMenu/MainMenu.uxml` | `loading-overlay` spostato sotto `action-list` |
| `Assets/_Project/Resources/UI/UIToolkit/MainMenu/MainMenu.uss` | Layout loading inline, no overlay fullscreen |
| `Assets/_Project/Scripts/UI/UIToolkit/MainMenu/MainMenuUIToolkitController.cs` | Disabilitazione bottoni durante caricamento |

---

## Regole / vincoli rispettati

- Nessun `FindObjectOfType` aggiunto per il layering; modifiche su `UIDocument`/`Panel Settings` e scena.
- Parità authoring: struttura menu e classi USS restano la superficie principale; il controller gestisce solo visibilità/stato e testo percentuale sul caricamento.
- End of Day mantiene priorità sopra HUD e toast tramite `sortingOrder` dedicato nello stesso stack dove applicabile.

---

## Note operative (Unity)

- Dopo il cambio **Panel Settings** sulle notifiche Foundation, verificare in **Play** su `SCN_VaultMap` posizione/scala del blocco `.nf-root` (riferimento risoluzione ora allineato a `PlayerStatusPanelSettings`); ritoccare USS se serve.
- QA: hover tooltip su metriche TopBar / bottom bar con toast visibili → tooltip leggibili sopra; **Nuova partita** → barra sotto “Esci da Sporium”, bottoni disabilitati fino al cambio scena.

---

*Fine DEV REPORT 0085.*
