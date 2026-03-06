# Test Save/Load – Istruzioni Unity

Guida per verificare il funzionamento del sistema di salvataggio e caricamento (save/load).

---

## Prerequisiti

1. **Scene di gioco**: apri la scena principale del vault (es. `SCN_VaultMap` o quella in cui giochi).
2. **Oggetti in scena**:
   - `GamePlayInstaller` (registra servizi e avvia il caricamento ritardato del save).
   - `GameManager` (in scena o creato da AppRoot).
   - `AppRoot` (opzionale; gestisce salvataggio su pause/focus/quit).
   - Pulsante **End Day** collegato a `EndDayButton` (salvataggio prima di finire il giorno).
3. **Foundation Notifications** (opzionale): se abilitate nel GamePlayInstaller, al salvataggio compare il toast "Gioco salvato correttamente" (SYS-003).

---

## Dove viene salvato

- **File**: `Application.persistentDataPath/Saves/sporium_save.json_default`  
  In Editor: ad es.  
  `C:\Users\<Utente>\AppData\LocalLow\<CompanyName>\<ProductName>\Saves\`
- **Backup**: stessi dati anche in PlayerPrefs con chiave `Sporium_Save_default` e timestamp `Sporium_Save_default_timestamp`.

---

## Step 1 – Caricamento automatico all’avvio

**Obiettivo**: verificare che, se esiste un save, il gioco carichi quello invece di partire da zero.

1. **Prima partita (nessun save)**  
   - Elimina il save esistente (vedi Step 4).  
   - Avvia il Play in Unity.  
   - **Atteso**: partita nuova (giorno 1, inventario starter, CRY iniziali, azioni piene).  
   - In Console (se abilitato): messaggio tipo "Nessun salvataggio trovato, partita nuova".

2. **Seconda partita (con save)**  
   - Con il Play ancora attivo, fai qualche azione (usa azioni, cambia CRY, pianta qualcosa, avanza un giorno se vuoi).  
   - Salva manualmente (Step 2) o lascia che salvi End Day / AppRoot.  
   - **Ferma il Play**.  
   - **Riavvia il Play**.  
   - **Atteso**:  
     - Giorno, CRY, azioni rimanenti, inventario, stato vasi, condensazione e modulo staminali come al momento del save.  
     - In Console: "Salvataggio caricato automaticamente".  
   - Controlla HUD: giorno, CRY e azioni devono coincidere con il save.

---

## Step 2 – Salvataggio manuale (Inspector F1)

**Obiettivo**: usare i pulsanti Save/Load della debug console.

1. In scena deve esserci un GameObject con **GlobalStateInspector** (tasto **F1** per aprire/chiudere).
2. Avvia il Play.
3. Premi **F1** per aprire l’inspector.
4. Espandi la sezione **"Save System State"**.
5. **Save**  
   - Clicca **Save**.  
   - **Atteso**: in Console "Salvataggio completato (slot default)". Se le notifiche sono abilitate, toast "Gioco salvato correttamente".
6. Modifica qualcosa in gioco (es. spendi CRY o azioni).
7. **Load**  
   - Clicca **Load**.  
   - **Atteso**: stato ripristinato senza riavviare il Play (stesso slot "default"). In Console "Caricamento completato (slot default)".
8. Verifica che CRY, azioni e inventario siano tornati allo stato del save.

---

## Step 3 – Salvataggio da End Day e da chiusura

**Obiettivo**: salvataggio automatico prima di “finisci giorno” e in uscita.

1. **End Day**  
   - In Play, assicurati di avere almeno 20 CRY (costo fine giornata).  
   - Clicca il pulsante **End Day** (o il flusso che mostra il diario e fa finire il giorno).  
   - **Atteso**: prima del cambio giorno viene chiamato il save; in Console "Salvataggio automatico eseguito con successo" e, se abilitate, toast "Gioco salvato correttamente".
2. **Pausa / Focus / Quit**  
   - Con **AppRoot** in scena:  
     - **Build**: metti l’app in background (pause) o chiudila: deve salvare e, se le notifiche sono abilitate, mostrare il toast.  
     - **Editor**: simula “perso focus” (clicca fuori dalla Game view) o ferma il Play: in Console dovresti vedere i log di salvataggio (e toast se abilitato).

---

## Step 4 – Eliminare un salvataggio

**Obiettivo**: tornare a “partita nuova” senza modificare il codice.

1. **Da codice (solo per test)**  
   - Puoi chiamare `SaveManager.Instance.DeleteSave("default")` da uno script di debug o da un pulsante temporaneo.
   - **In Play**: apri l’inspector con **F1** → sezione **Save System State** → pulsante **Delete save** (elimina lo slot "default").
2. **Manualmente**  
   - Chiudi Unity (o almeno ferma il Play).  
   - Vai in `Application.persistentDataPath` (in Editor: vedi sopra).  
   - Nella cartella **Saves** elimina il file `sporium_save.json_default`.  
   - (Opzionale) In PlayerPrefs rimuovi le chiavi `Sporium_Save_default` e `Sporium_Save_default_timestamp` (es. con RegEdit su Windows o strumenti per PlayerPrefs).
3. Alla prossima partita non ci sarà save: il gioco partirà da zero.

---

## Step 5 – Cosa è incluso nel save (checklist)

Dopo un **Save** e un **Load** (o riavvio Play), verifica che siano ripristinati:

| Dato                    | Come verificare                                      |
|-------------------------|------------------------------------------------------|
| Giorno corrente         | HUD / UI che mostra il giorno                       |
| CRY                     | Valuta in HUD                                        |
| Azioni rimanenti        | Contatore azioni in HUD                              |
| Condensazione           | Indicatore condensazione (es. TopBar)                |
| Inventario              | Pannello inventario (oggetti e quantità)             |
| Stato vasi              | Piante, idratazione, LED, irrigazione, stadio        |
| Modulo staminali        | Funzionalità Extractor con Whole Plant              |
| Note diario piante      | Note per vaso nel diario piante                     |
| Missioni                | Solo se le MissionConfig sono in una cartella Resources (nome asset = configName) |

---

## Step 6 – Note diario e missioni (opzionale)

- **Note diario**: aggiungi una o più note a un vaso dal diario piante, salva, carica (o riavvia). Le note devono essere ancora presenti per quel vaso.
- **Missioni**: il restore usa `Resources.LoadAll<MissionConfig>("")`. Se le MissionConfig non sono in una cartella **Resources**, le missioni non verranno ripristinate. Per testare, metti almeno un asset MissionConfig in `Assets/Resources/` (o in una sottocartella) e assegna la missione in gioco; dopo save e load dovrebbe riapparire con stato completata/non completata.

---

## Risoluzione problemi

- **Dopo il load il giorno è 1**: di solito succedeva se il load avveniva prima del GameManager; ora il load è posticipato (coroutine in GamePlayInstaller). Se persiste, controlla che `GameManager` sia in scena e con `DefaultExecutionOrder(-50)` e che non ci siano errori in Console al caricamento.
- **CRY/azioni/inventario non ripristinati**: come sopra; assicurati che non ci siano errori durante `ApplySaveData` e che il save sia stato scritto dopo le modifiche (verifica timestamp del file o di PlayerPrefs).
- **Condensazione sempre 0 dopo load**: nel save deve esserci `gameState.condensationAmount`; il load chiama `CondensationSystem.SetCurrentAccumulation`. Controlla che il save contenga il campo e che non ci siano errori in load.
- **Toast "Gioco salvato" non appare**: abilita **Foundation Notifications** nel GamePlayInstaller e verifica che il tipo SYS-003 sia registrato (es. in NotificationTypeSpecDefaults).
- **Missioni non si ripristinano**: le MissionConfig devono essere caricabili da `Resources.LoadAll<MissionConfig>("")` (es. in una cartella Resources). Il save usa `MissionConfig.name`; al load si fa match per nome.

---

## Menu ESC – pulsante Save

Il menu che si apre con **ESC** (New game, Continue, Load, Options, Quit) supporta un pulsante **Save**. In `MainMenuOptions` sono già presenti `_saveButton` e `HandleSave()` (salvataggio slot "default" + toast se le notifiche sono attive). Per mostrare il pulsante in scena:

1. Apri il prefab **Menu** (`Assets/_Project/Prefabs/UI/Menu.prefab`).
2. Sotto **Menu > Pages > Buttons** duplica il pulsante **Load** (tasto destro → Duplicate).
3. Rinomina il duplicato in **SaveButton** e nel **Text (TMP)** imposta il testo a **Save**.
4. Sul GameObject **Menu** (con MainMenuOptions) assegna **Save Button** trascinando **SaveButton** dalla gerarchia.
5. Salva il prefab.

---

## Riepilogo comandi / punti di ingresso

| Azione           | Dove / Come                                                |
|------------------|------------------------------------------------------------|
| Caricamento      | Automatico all’avvio (GamePlayInstaller, dopo GameManager) |
| Salvataggio      | End Day, AppRoot (pause/focus/quit), pulsante Save (F1), menu ESC Save |
| Load manuale     | Sezione Save System nell’inspector (F1), pulsante Load      |
| Eliminare save   | Sezione Save System nell'inspector (F1) pulsante **Delete save**, oppure `SaveManager.Instance.DeleteSave("default")` o cancellare file/PlayerPrefs |
| Verifica esistenza | `SaveManager.Instance.SaveExists("default")`            |
| Timestamp        | `SaveManager.Instance.GetSaveTimestamp("default")`         |
