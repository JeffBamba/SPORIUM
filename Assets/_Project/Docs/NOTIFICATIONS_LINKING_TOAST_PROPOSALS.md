## Linking Toast — Proposte Trigger (pre-implementazione)

Questo documento assegna un **trigger proposto** (1-per-1) ai codici che oggi risultano **MISSING** nella matrice, e chiarisce i casi **REFERENCED** (codice presente in stringhe ma non necessariamente emesso).

### Convenzioni
- **Trigger attuale**: già emesso in game (call-site `PostToast`, watcher `UpsertDanger`, o Lore scheduler).
- **MISSING**: nessun emission point in game oggi.
- **REFERENCED**: il codice compare in uno script, ma potrebbe essere passato come variabile o usato solo come stringa; va verificato se diventa una vera emissione.

### Proposte trigger per codici MISSING

#### ACT-050 — Punti azione bassi (Warning)
- **Testo**: IT `Punti azione bassi` | EN `Low action points`
- **Trigger proposto**: quando `ActionSystem.ActionsLeft` scende sotto soglia.
- **Fonte dati**: `ActionSystem` (event `OnActionsChanged`).
- **Location candidata (call-site)**: `TopBarController.OnActionsChanged(int actionsLeft)` in `Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs`.
- **Regola**: emetti quando `actionsLeft <= 1` (o `actionsLeft/MaxActions <= 0.25`).
- **Gating**: `dedupKey="low-actions"` + cooldown spec (20s). Suggerita isteresi: riabilita notifica solo quando `actionsLeft >= 2`.

#### CRY-777 — Scambio completato {delta} CRY (Info)
- **Testo**: IT `Scambio completato {delta} CRY` | EN `Trade completed {delta} CRY`
- **Trigger proposto**: ogni volta che cambia il saldo CRY per transazione (delta != 0).
- **Fonte dati**: `EconomySystem` (event `OnCRYChanged`).
- **Location candidata**: `TopBarController.OnCRYChanged(int cryAmount)` in `Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs`.
- **Regola**: calcola `delta = cryAmount - previousCry`; se `delta != 0` posta `CRY-777` con payload `{delta}`.
- **Gating**: ignorare il primo update dopo bind (per non toastare lo “stato iniziale”). Cooldown 3s.

#### SYS-100 — Sistema di condensazione ottimale (Info)
- **Testo**: IT `Sistema di condensazione ottimale` | EN `Condensation system optimal`
- **Trigger proposto**: quando il “sistema condensazione” entra in stato *OK/Optimal* (es. dopo essere stato sotto-soglia o in allerta).
- **Fonte dati**: (da definire) metrica condensazione/efficienza nel sistema che aggiorna TopBar o HUD.
- **Location candidata**: se esiste un evento tipo `OnCondensationChanged`, emettere lì; altrimenti in `TopBarController` quando aggiorna `_condensation`.
- **Regola**: emetti solo quando attraversa una soglia verso “optimal” (es. `condensation >= 80%` e prima era `<80%`).
- **Gating**: cooldown 30s + dedupKey `"condensation-optimal"`.

#### SYS-001 — Database WIKI aperto (Info)
- **Testo**: IT `Database WIKI aperto` | EN `Wiki database accessed`
- **Trigger proposto**: quando il player apre la WIKI.
- **Fonte dati**: click su bottone WIKI.
- **Location candidata**: `WikipediaToggle.ToggleWikipedia()` in `Assets/_Project/Scripts/UI/VaultMap/Wikipedia/WikipediaToggle.cs` quando chiama `_wikipediaUI.Show()`.
- **Gating**: cooldown 10s + dedupKey `"wiki-open"`.

#### SYS-002 — Pannello impostazioni aperto (Info)
- **Testo**: IT `Pannello impostazioni aperto` | EN `Settings panel opened`
- **Trigger proposto**: quando viene aperto il pannello impostazioni.
- **Fonte dati**: bottone/settings menu (non trovato un controller dedicato in repo UI al momento).
- **Location candidata**: il metodo `Show()` del futuro `SettingsUI` oppure il bottone “Options/Settings” del menu (se presente).
- **Note**: attualmente sembra **non esistere** una UI settings specifica in `Assets/_Project/Scripts/UI/**`; questo code rimane “stub” finché il pannello non è implementato.

#### SYS-003 — Gioco salvato correttamente (Success)
- **Testo**: IT `Gioco salvato correttamente` | EN `Game saved successfully`
- **Trigger proposto**: quando `SaveManager.SaveGame()` ritorna `true`.
- **Fonte dati**: `SaveManager`.
- **Location candidata**:\n  - `EndDayButton.EndDay()` in `Assets/_Project/Scripts/UI/VaultMap/EndDayButton.cs` dopo `saveSuccess == true`.\n  - `AppRoot` nei punti dove fa autosave (vedi `Assets/_Project/Scripts/Core/AppRoot.cs`).
- **Gating**: cooldown 3s + dedupKey `"save-ok"`. Evitare spam su autosave ripetuti (emetti solo su “manual save” se introdotto).

#### SYS-999 — Uscita da SPORIUM... (Warning)
- **Testo**: IT `Uscita da SPORIUM...` | EN `Exiting SPORIUM...`
- **Trigger proposto**: quando viene richiesta la quit.
- **Fonte dati**: call a `QuitApplication()` o handler quit del menu.
- **Location candidata**:\n  - `AppRoot.QuitApplication()` in `Assets/_Project/Scripts/Core/AppRoot.cs`.\n  - `MainMenuOptions.HandleQuit()` in `Assets/_Project/Scripts/UI/MainMenu/MainMenuOptions.cs`.
- **Gating**: cooldown 10s + dedupKey `"quit"`.

#### INV-000 — Inventario aperto (Info)
- **Testo**: IT `Inventario aperto` | EN `Inventory accessed`
- **Trigger proposto**: quando l’inventario viene mostrato.
- **Fonte dati**: UI Inventory.
- **Location candidata**: `HUDInventory.Show()` in `Assets/_Project/Scripts/UI/VaultMap/HUDInventory.cs`.
- **Gating**: cooldown 10s + dedupKey `"inventory-open"`.

#### INV-REM — Rimosso dall’inventario: {item} {delta} (Info)
- **Testo**: IT `Rimosso dall’inventario: {item} {delta}` | EN `Removed from Inventory: {item} {delta}`
- **Trigger proposto**: quando un item viene consumato/rimosso e la quantità cala.
- **Fonte dati**: `Inventory.Consume(...)` (oggi l’evento `OnInventoryChanged` non porta dettagli).
- **Opzione A (consigliata)**: estendere `Inventory` con un evento dettagliato (es. `OnInventoryDelta(typeId, delta)`).
- **Opzione B (zero refactor)**: nel consumer (es. Black Market / Lab) emettere direttamente la notifica nel call-site dove si chiama `Consume(...)`.
- **Payload**: `{item}` = `typeId` o displayName; `{delta}` = `-1`, `-3`, ecc.

#### ITEM-GET — Ottenuto da {location} (Info)
- **Testo**: IT `Ottenuto da {location}` | EN `Obtained from {location}`
- **Trigger proposto**: quando viene aggiunto un item in inventario (evento “pickup/reward”), con contesto “da dove”.
- **Fonte dati**: call-site che fa `Inventory.Add(...)` (non c’è un evento dettagliato).
- **Location candidata**: nei punti che fanno reward (es. minigame, raccolta frutti, black market buy).
- **Payload**: `{location}` = `"Black Market" / "Extractor" / "Pot {id}" / "Condensation"`.

#### HYD-001 — Hai bevuto: Idratazione +{deltaHyd}% — +{deltaAct} Azione (Info)
- **Testo**: IT `Hai bevuto: Idratazione +{deltaHyd}% — +{deltaAct} Azione` | EN `You drank: Hydration +{deltaHyd}% — +{deltaAct} Action`
- **Trigger proposto**: quando si esegue l’azione “bevi” che aumenta hydration e/o actions.
- **Fonte dati**: sistema player hydration (non ancora integrato nel PlayerStatusPanel; lì sono mock).
- **Location candidata**: lo script che gestisce il consumo di acqua (non identificato qui: serve trovare il punto in cui hydration/actions vengono incrementate).
- **Note**: finché hydration rimane mock UI, questo code resta “stub”.

#### REP-CHANGE — Reputazione {faction} {delta} (Info)
- **Testo**: IT `Reputazione {faction} {delta}` | EN `{faction} reputation {delta}`
- **Trigger proposto**: quando un sistema reputazione cambia valore.
- **Fonte dati**: un `ReputationSystem`/`FactionSystem` (non individuato nei file letti).
- **Location candidata**: nel metodo che applica delta reputazione, subito dopo l’update.
- **Note**: attualmente sembra “stub” finché non esiste una sorgente reputazione.

#### SPORAE-001 — SPORAE Diary aperto (Info)
- **Testo**: IT `SPORAE Diary aperto` | EN `SPORAE Diary accessed`
- **Trigger proposto**: quando si apre `DiaryUI`.
- **Fonte dati**: UI Diary.
- **Location candidata**: `DiaryUI.Show()` in `Assets/_Project/Scripts/UI/VaultMap/Diary/DiaryUI.cs`.
- **Gating**: cooldown 15s + dedupKey `"diary-open"`.

#### RES-001 — Ricerca avviata: {nodeTitle} (Info)
- **Testo**: IT `Ricerca avviata: {nodeTitle}` | EN `Research started: {nodeTitle}`
- **Trigger proposto**: quando il player conferma un’opzione ricerca.
- **Fonte dati**: UI research (`NightResearchUI`) + sistema che applica la scelta.
- **Location candidata**: `NightResearchUI.HandleConfirm()` in `Assets/_Project/Scripts/UI/VaultMap/NightResearch/NightResearchUI.cs` *se* lì viene avviata la ricerca reale (oggi chiama solo `EndDay()`).
- **Note**: al momento è “stub” perché non c’è `nodeTitle` né applicazione ricerca.

#### RES-UNLOCK — Specie individuale sbloccata per studio (Success)
- **Testo**: IT `Specie individuale sbloccata per studio` | EN `Individual species unlocked for study`
- **Trigger proposto**: quando viene sbloccata una specie per la prima volta.
- **Fonte dati**: sistema research unlock (non identificato qui).
- **Location candidata**: nel punto che aggiunge la specie alla lista sbloccata.

#### WIKI-UNLOCK — {plantType} aggiunta a WIKI PLANTS (Success)
- **Testo**: IT `{plantType} aggiunta a WIKI PLANTS` | EN `{plantType} added to WIKI PLANTS`
- **Trigger proposto**: quando un item WIKI viene sbloccato (nuova pianta o entry).
- **Fonte dati**: sistema unlock/collectopedia (non identificato nei file letti).
- **Location candidata**: dove viene aggiunta la voce alla WIKI.
- **Note**: la UI WIKI (`WikipediaUI`) gestisce show/hide, ma non l’unlock.

#### LAB-001 — Laboratorio accesso effettuato (Info)
- **Testo**: IT `Laboratorio accesso effettuato` | EN `Laboratory accessed`
- **Trigger proposto**: quando il player apre uno dei pannelli lab (Microscope/Pipette/Catalizzatore/Extractor).
- **Fonte dati**: UI Lab.
- **Location candidata**: `Show()` di `LabMicroscope`, `LabPipette`, `LabCatalizzatore`, `LabMinigameExtractor` (o un “LabHub” se esiste).
- **Gating**: cooldown 8s + dedupKey `"lab-open"`.

#### LAB-MIC — Microscopio caricato con {sporeCode} (Info)
- **Testo**: IT `Microscopio caricato con {sporeCode}` | EN `Microscope loaded with {sporeCode}`
- **Trigger proposto**: quando viene inserito un sample nel microscopio.
- **Fonte dati**: inventory del microscopio (`Microscope.GetInventory()`).
- **Location candidata**: in `LabMicroscope.UpdateStorage()` quando la slot del sample passa da “vuoto” a “ha spore”.
- **Note**: oggi il sample è `Items.SporeGeneric`; per avere `{sporeCode}` serve un item typeId specifico o metadata.

#### LAB-PIP — Pipetta caricata con {sporeCode} (Info)
- **Testo**: IT `Pipetta caricata con {sporeCode}` | EN `Pipette loaded with {sporeCode}`
- **Trigger proposto**: quando viene consumato un sample per caricare la pipetta.
- **Fonte dati**: `Pipette` / storage pipetta.
- **Location candidata**: `LabPipette.HandleConfirm()` o quando `_storage` cambia.

#### LAB-INC-OK / LAB-INC-FAIL — Incubazione riuscita/fallita (Success/Danger)
- **Testi**: IT `Incubazione riuscita!` / `Incubazione fallita. Seme scartato.` (EN analoghi)
- **Trigger proposto**: alla fine del processo “incubazione” (risultato true/false).
- **Fonte dati**: sistema incubatore (non presente oltre `IncubatorUI`).
- **Location candidata**: nel metodo che finalizza l’incubazione e crea/scarta il seed.
- **Note**: attualmente “stub”: serve identificare/creare il punto dove l’incubazione produce output.

#### LAB-GRF-OK / LAB-GRF-FAIL — Graft riuscito/fallito (Success/Danger)
- **Testi**: IT `Graft riuscito! Creato {seedCode}` / `Graft fallito. Residuo organico formato.` (EN analoghi)
- **Trigger proposto**: alla fine dell’operazione di graft (success/fail).
- **Fonte dati**: sistema graft (non individuato).
- **Location candidata**: nel metodo che produce il nuovo seed o residuo organico.

#### INV-SPR — Raccolte {amount} spore (Info)
- **Testo**: IT `Raccolte {amount} spore` | EN `Collected {amount} spores`
- **Trigger proposto**: quando il minigame extractor aggiunge spore all’inventario/risorsa.
- **Fonte dati**: reward extractor.
- **Location candidata**: `LabMinigameExtractor` nel punto dove assegna il reward (oggi emette `SPORE-001` senza quantità).
- **Note**: serve sapere la quantità reale `{amount}`.

#### PLT-101 — Dati pianta consultati (Info)
- **Testo**: IT `Dati pianta consultati` | EN `Plant data accessed`
- **Trigger proposto**: quando si apre la schermata “plant data” (PlantCard, details, ecc.).
- **Fonte dati**: UI plant details.
- **Location candidata**: il metodo `Show()` del controller della PlantCard/PlantDetails (es. `PlantCardV2Controller` se ha uno show esplicito).

#### PLT-ACT — {plantName} {actionName} — {details} (Info)
- **Testo**: IT/EN `{plantName} {actionName} — {details}`
- **Trigger proposto**: come wrapper “generico” per azioni/feedback su pianta (non-pot-actions).
- **Fonte dati**: call-site delle azioni (es. potature, cure, analisi, ecc.).
- **Note**: oggi non c’è un call-site naturale; rischia sovrapposizione con `POT-*-SUCCESS`.

#### POT-F01 / POT-W01 — Vaso irrigato +{delta}% / Fertilizzante aggiunto +{delta}% (Info)
- **Trigger proposto**: quando hydration/fertilizer cambia di delta percentuale.
- **Fonte dati**: `PotActions` / state del pot.
- **Location candidata**: nel punto in cui l’azione applica realmente la variazione (oggi i toast pot actions usano `POT-*-SUCCESS` con `{message}`).
- **Note**: questi due codici sono “alternativi” ai passthrough `POT-*-SUCCESS`; decidere quale schema mantenere nella fase implementativa.

#### POT-PLANT-OK / POT-PLANT-IRR-OK — Plantata / plantata e irrigata (Success)
- **Trigger proposto**: quando il plant action va a buon fine (con o senza irrigazione immediata).
- **Fonte dati**: `PotEvents` (Plant action) + stato watering.
- **Location candidata**: `PotNotifications.HandlePotAction()` oppure call-site dove si esegue il plant (se ha info su irrigazione).
- **Note**: oggi esiste già `POT-PLANT-SUCCESS` passthrough: questi sono “più specifici” e richiedono integrazione.

#### POT-REMOVE — Pianta rimossa dal vaso (Info)
- **Trigger proposto**: quando l’azione Uproot/Remove va a buon fine.
- **Fonte dati**: `PotEvents.PotActionType.Uproot` success.
- **Location candidata**: `PotNotifications.HandlePotAction()` quando type == Uproot (in alternativa un code dedicato).

#### POT-HARVEST-OK — Raccolto ottenuto (Success)
- **Trigger proposto**: quando si completa un harvest e si ottiene almeno 1 fruit/item.
- **Fonte dati**: `PotSlot.CollectFruits` (già emette `INV-FRUIT-001`) + risultato harvest.
- **Location candidata**: `PotSlot.CollectFruits` o `PotNotifications` su action harvest.
- **Note**: possibile sovrapposizione con `INV-FRUIT-001` e `POT-HARVEST-SUCCESS`.

#### DOR-001 / SEED-001 / KTCH-001 / MKT-666 — Accesso a Dormitorio / Archivio semi / Cucina / Mercato Nero
- **Testi**: presenti in TypeSpecDefaults.
- **Trigger proposto**: quando i relativi pannelli/room UI vengono aperti.
- **Fonte dati**: rispettivi UI controller (es. `SeedStorageUI`, `SeedInventoryMenu`, `UIBlackMarketBuy/Sell`, ecc.).
- **Location candidata**: nei metodi `Show()`/`Open()` dei pannelli.
- **Note**: alcuni sono già presenti come UI ma non hanno un punto centralizzato “room entered”; nella fase implementativa decideremo se usare call-site UI o un “RoomNavigationSystem”.

#### PH-COUNTDOWN-001 — legacy countdown toast (Warning)
- **Testo**: IT `⚠️ {plant} tra {days} giorni morirà (pH estremo).`
- **Stato attuale**: appare nei fallback legacy (`_toastManager.ShowToast(..., \"PH-COUNTDOWN-001\")`) ma con Foundation abilitato viene usato `PH-RISK-COUNTDOWN` (DANGER persistent) via `UpsertDanger`.
- **Decisione da prendere (fase successiva)**:\n  - a) Deprecare `PH-COUNTDOWN-001` (resta solo legacy)\n  - b) Linkarlo anche in Foundation come toast non-persistente (ma rischia duplicazione con watcher)


