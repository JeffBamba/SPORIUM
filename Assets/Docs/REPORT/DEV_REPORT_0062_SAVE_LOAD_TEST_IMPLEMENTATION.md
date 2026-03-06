# DEV REPORT 0062 — Implementazione SAVE_LOAD_TEST e allineamento Save/Toast

**Data:** 2026-03-06  
**Oggetto:** Implementazione delle istruzioni di test Save/Load (SAVE_LOAD_TEST.md): toast SYS-003 dopo Save manuale e in End of Day, pulsante Delete save nell’inspector F1, log e doc aggiornati.  
**Riferimenti:** `SAVE_LOAD_TEST.md`, `GlobalStateInspector.cs`, `EndOfDaySequenceController.cs`, `SaveManager`, Foundation Notifications (SYS-003).

---

## 1. Contesto

- La guida **SAVE_LOAD_TEST.md** descrive come verificare il sistema di salvataggio/caricamento (caricamento automatico all’avvio, Save/Load da inspector F1, End Day, pause/focus/quit, eliminazione save).
- In sede di implementazione della guida sono state verificate le parti già presenti (SaveManager, GamePlayInstaller, AppRoot, EndOfDaySequenceController, MainMenuOptions, Menu prefab) e aggiunte le parti mancanti per allineare il comportamento al documento: toast "Gioco salvato correttamente" (SYS-003) dopo Save manuale (F1), log e toast dopo save in sequenza End of Day, pulsante per eliminare il save da test (Step 4), aggiornamento del doc.

---

## 2. Lavoro svolto

### 2.1 GlobalStateInspector (F1 — Save System State)

- **Toast dopo Save:** dopo un salvataggio riuscito con il pulsante **Save** viene inviato il toast **SYS-003** ("Gioco salvato correttamente") se le Foundation Notifications sono abilitate nel GamePlayInstaller (`FoundationNotificationServiceAccessor.Get()` + `foundation.Enabled` + `PostToast("SYS-003", new NotificationPayload())`).
- **Pulsante Delete save:** aggiunto il pulsante **Delete save** nella sezione "Save System State"; elimina lo slot "default" (chiamata `SaveManager.DeleteSave("default")`) e scrive in console l’esito. Consente di eseguire lo Step 4 della guida (tornare a partita nuova) senza uscire dall’Editor né cancellare manualmente file/PlayerPrefs.
- **Using:** aggiunto `Sporae.UI.UIToolkit.NotificationsFoundation` per `FoundationNotificationServiceAccessor` e `NotificationPayload`.

### 2.2 EndOfDaySequenceController (End of Day)

- **OnYesClicked (conferma "END DAY?"):** dopo la chiamata a `SaveManager.SaveGame("default")` sono stati aggiunti:
  - Log in Console (solo in Editor): **"Salvataggio automatico eseguito con successo"** (`SporiumLogger.LogInfo(LogCategory.Save, ...)`).
  - Invio toast **SYS-003** se le Foundation Notifications sono abilitate (`FoundationNotificationServiceAccessor.Get()` + `PostToast("SYS-003", new NotificationPayload())`).
- **Using:** aggiunto `Sporae.UI.UIToolkit.NotificationsFoundation` per il toast.

### 2.3 Documentazione SAVE_LOAD_TEST.md

- **Step 4 (Eliminare un salvataggio):** nella voce "Da codice (solo per test)" è stata aggiunta l’opzione in Play: aprire l’inspector con **F1** → sezione **Save System State** → pulsante **Delete save** (elimina lo slot "default").
- **Riepilogo comandi (tabella finale):** nella riga "Eliminare save" è stato indicato anche il pulsante **Delete save** nella sezione Save System dell’inspector (F1), oltre a `DeleteSave("default")` e cancellazione file/PlayerPrefs.

---

## 3. File modificati

| File | Modifica |
|------|----------|
| `Assets/_Project/Scripts/DevTools/Inspector/GlobalStateInspector.cs` | Using NotificationsFoundation; dopo Save riuscito: log + toast SYS-003 se notifiche abilitate; pulsante "Delete save" con log esito. |
| `Assets/_Project/Scripts/UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs` | Using NotificationsFoundation; in OnYesClicked: dopo SaveGame log "Salvataggio automatico eseguito con successo" (Editor) e PostToast SYS-003 se notifiche abilitate. |
| `Assets/_Project/Docs/SAVE_LOAD_TEST.md` | Step 4: aggiunta opzione Delete save da F1; tabella riepilogo: eliminare save tramite pulsante inspector F1. |

---

## 4. Verifica

- Nessun errore di lint sui file modificati.
- Comportamento atteso: Save da F1 → Console "Salvataggio completato (slot default)" + toast SYS-003 se abilitato; End Day → conferma YES → Console "Salvataggio automatico eseguito con successo" + toast SYS-003 se abilitato; Delete save da F1 → slot "default" eliminato, messaggio in Console.

---

## 5. Note per QA

- **Step 2 (SAVE_LOAD_TEST.md):** In Play, F1 → Save System State → Save: verificare messaggio in Console e, con Foundation Notifications abilitate nel GamePlayInstaller, toast "Gioco salvato correttamente". Load: stato ripristinato senza riavvio. Delete save: eliminazione slot default e messaggio in Console.
- **Step 3 (End Day):** Clic su End Day, conferma nella sequenza EoD (YES): in Console deve comparire "Salvataggio automatico eseguito con successo" e, se le notifiche sono abilitate, toast SYS-003.
- **Step 4:** Per resettare a partita nuova si può usare il pulsante **Delete save** nell’inspector (F1) senza chiudere Unity né cancellare file a mano.

---

## 6. Riferimenti

- Guida test: `Assets/_Project/Docs/SAVE_LOAD_TEST.md`
- Save/Load multi-slot e popup: DEV REPORT 0053
- Foundation Notifications e SYS-003: `FoundationNotificationServiceAccessor`, `NotificationTypeSpecDefaults` (tipo SYS-003)

---

*Fine DEV REPORT 0062.*
