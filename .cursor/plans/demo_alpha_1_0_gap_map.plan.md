---
name: Demo Alpha 1.0 — gap map e decisioni
overview: "Demo Alpha 1.0: stesso binario della full version; integrazione incrementale beat/Director task per task verso l’obiettivo finale; decisioni UX/prodotto chat 2026-04-18."
todos: []
isProject: true
---

# Demo Alpha 1.0 — piano aggiornato (decisioni chat)

*Ultimo allineamento stato (2026-05-03):* oltre al consolidamento 2026-04-28, chiuso un **passaggio beat 3** su **missioni demo** (`M_Demo_PcAccess`, `M_Demo_PcSeedPower` sotto `Assets/Resources/Missions/`), **caricamento `MissionConfig`** con fallback `Resources.LoadAll`, **fix `MissionChecker.Check()`** (niente auto-completamento con lista obiettivi vuota — bug `Enumerable.All` su sequenza vuota), **guard** in `DemoStoryDirector` su completamento missione Seed Storage + reset flag in append, **layering VO vs Bedroom PC** regolato con **`m_SortingOrder` differenziato** su `MainMenuPanelSettings` vs `BedroomPcPanelSettings` e ri-sync `UIDocument.sortingOrder` in `VoOverlayController.ShowLine`. Evidenza: `DEV_REPORT_0105_DEMO_BEAT3_MISSIONI_VO_LAYERING_2026-05-03.md`.

*Decisione team (2026-05-03):* quanto presente su **main** per il **percorso demo** si considera **testato e chiuso per l’iterazione corrente**; in **roadmap** si marcano **`DONE`** i task coperti dall’implementazione, lasciando espliciti i **residui di prodotto** (es. **FAQ/AI**, dialoghi di conferma OFF) senza riaprire il filo narrativo beat 3.

*Decisione UX (2026-05):* il **PC in camera** non avvia la fine giornata — il player usa il **letto** per l’EoD. Il terminale resta dedicato a **pannello di controllo energia**, **consultazione analitica** (ricerche/wiki sbloccate) e **accessori** (FAQ, Black Market) come da §5 aggiornato.

*Decisione prodotto (2026-05-04):* da **Beat 4** in avanti la demo procede con struttura **choice-driven**: ogni dubbio del VO interno diventa una **micro-scelta esplicita** (`VO prompt → choice → flag → conseguenza → next objective`). I Beat **1–3 restano implementati e chiusi**; il nuovo lavoro parte dall’handoff dopo accensione Seed Storage.

*Decisione contenuto demo-only (2026-05-04):* il **Cetriolo d’Oro** è un **item Fruit** ottenuto dalla pianta/specie **Il Piacere Dimenticato** (item Plants / seed + plant data demo). Entrambi sono contenuto **solo DEMO**: devono vivere sotto flag/sessione demo e non contaminare Nuova partita full, cataloghi full o save non demo.

**Aggiornamento roadmap:** `DA10-T017`, `DA10-T007`, `DA10-T008`, `DA10-T018` (placeholder), `DA10-T020`, `DA10-T015`, `DA10-T022` → **`DONE`** per scope demo su main; `DA10-T019` → **`DONE (placeholder)`** con **contenuti/AI in backlog** (vedi legenda sotto la tabella roadmap); `DA10-T021` → **`N/A`** (scope ritirato: sonno solo dal letto).

## Principio 0 — Un solo prodotto

- **Stessa build / stesso `SCN_VaultMap`**: si lavora alla demo **e** alla full version **senza** doppioni o binari distanti.
- Orchestrazione demo: `DemoSessionState` + `DemoStoryDirector` (contenuti e gating), non fork di economia o inventario.

### Obiettivo finale e integrazione incrementale (vincolo di processo)

**Obiettivo finale del piano** (direzione non negoziabile):

- Demo Alpha **giocabile end-to-end** sul **medesimo binario** di **Nuova partita** (`SCN_VaultMap`), con la **traccia master a 9 milestone** no-spoiler, **Narrative Red Lines** rispettate, orchestrazione via `DemoSessionState` / `DemoStoryDirector`, **senza** fork di economia, inventario o scene parallele.

**Cosa questo piano non è:**

- **Non** è una roadmap del tipo «prima si completano tutte le funzioni, poi in un secondo momento si integra tutto il flusso demo». Il percorso demo **non** va rinviato a un’unica fase finale dopo aver svuotato il backlog funzionale.

**Evoluzione task per task (senza perdere il nord):**

- Ogni singolo task deve **portare avanti** il piano nella sua parte **e** restare **allineato all’obiettivo finale**: non basta chiudere il sotto-scope tecnico se si **tronca** il filo che collega al demo giocabile e alla **traccia master (9 milestone)**.
- Dove le dipendenze lo consentono, ogni consegna dovrebbe includere un **incremento verificabile** sul **percorso demo** (anche minimo: beat parziale, trigger, step in `DemoStoryDirector`, asset beat, VO collegato a uno step). I blocchi puramente propedeutici vanno esplicitati come tali e **sbloccanno** il beat successivo appena pronti.
- Le **fondamenta trasversali** (ingresso demo, VO overlay, save/flag, layering, servizi registrati) abilitano i beat; le **feature di gioco** (H/F, armadio, Visitor, PC elettricità, Lab, …) si sviluppano **in parallelo** con l’**estensione dei beat** nel Director/dati, non al posto della progressione narrativa.

**Regola operativa:** evitare periodi lunghi in cui si accumulano solo implementazioni **senza** almeno un **aggancio** misurabile alla demo (beat, Director, o checklist Play della tabella *traccia beat — 9 milestone* aggiornata).

**Nota su funzionalità Both:** per definizione progetto, le feature **Both** sono una sola implementazione per demo e Nuova partita; vedi regola Cursor `feature-both-demo-full-parity.mdc`.

---

## Traccia master — Alpha no-spoiler (9 milestone)

1. **Wake / routine guidata** (Bedroom → Kitchen, VO bicolore ambiguo).
2. **Survival baseline** (H/F/azioni e tutorial breve).
3. **Seed Storage anomaly** (vuoto/frigo/costi CRY, niente reveal espliciti).
4. **Mercante in Visitor Room + contratto a credito**: dopo accensione Seed Storage, VO annuncia arrivo al Visitor Center; chat con Mercante Ombra (sarcasmo, identità ambigua, amnesia del Biologo); grant **4 frutti demo** a credito; promessa “domani mi paghi con acqua e cibo”; **Choice 1 `prepare_payment`**: preparare o non preparare.
5. **Mattina dopo: pagamento o rottura patto**: EoD dal letto, processi acqua/cibo se avviati; al risveglio **Choice 2 `meet_or_avoid_merchant`**. Quando gli item necessari al pagamento (es. **`1x WAT-POT` + `1x FOOD-101`**) sono **effettivamente in inventario**, il sistema emette una **toast Foundation** (notifiche HUD): primo richiamo esplicito a **leggere e usare la HUD** (notifiche, inventario, indicatori rilevanti) senza elenco prescriptivo click-per-click. Se pagamento riuscito: scambio acqua+cibo ↔ debito saldato. Se pagamento fallito/evitato: confronto rapido, reputazione giù, canale mercante peggiorato/chiuso, frutti restano come prova del patto rotto.
6. **Uso dei frutti: Lab o consumo informato**: il mercante (o il suo disprezzo nel ramo rotto) rivela il valore nascosto; **Choice 3 `fruit_use`**. `lab` porta direttamente al Laboratorio; `eat_then_lab` consuma 1 frutto, cambia VO/flag, ma lascia abbastanza frutti per riconvergere sul Lab.
7. **Signature Lab: Il Piacere Dimenticato**: sequenza reale `Crea nuovo seme` nell’area Lab di `SCN_VaultMap`; output = seme demo-only della pianta **Il Piacere Dimenticato**, che produrrà il fruit item **Cetriolo d’Oro**.
8. **Dome + crescita compressa**: VO indirizza alla Dome; tutorial POT minimale solo sulle azioni necessarie; pianta il seme, cresce fino a stadio 2, poi EoD con testo esatto **“A few days later”** e maturazione del **Cetriolo d’Oro**.
9. **Scelta finale + glitch demo**: raccolta Cetriolo d’Oro; **Choice finale `golden_cucumber_outcome`**: `keep`, `sell`, `give`. Il framing cambia per ramo (cooperazione vs riparazione/rottura/isolamento), registra esito morale/sociale e converge nel glitch finale controllato.

---

## 1 — Come orchestrare i beat: ScriptableObject vs Timeline (pro / contro)

**Opzione A — Beat in ScriptableObject + codice (Director)**

- **Pro:** iterazione veloce per designer (testi, ordine beat, flag); diff Git leggibili; branching condizionale semplice (`if demo_day == 3`); leggero.
- **Contro:** meno “WYSIWYG” per regia camera; animazioni complesse o lip-sync vanno comunque orchestrate a mano o con sotto-Timeline.

**Opzione B — Unity Timeline (+ script di glue)**

- **Pro:** timing visivo, camera, attivazione oggetti molto chiari in editor; ottimo per **cutscene lunghe** o sequenze cinematografiche.
- **Contro:** merge più fastidiosi; più peso per beat corti (VO + linea); branching narrativo richiede più boilerplate o Timeline varianti.

**Indicazione per Sporium:** per Demo Alpha (molti **VO + gating + trigger**), tendenza **A come spina dorsale**; usare **Timeline solo dove serve** (intro / finale caos / un hook forte), non per ogni battuta.

---

## 2 — VO (overlay testo)

- **Stato codice (2026-04-28):** animazioni ingresso/uscita, typewriter ~33 c/s, durata blocco configurabile, highlight parole missione da `DemoAlphaNarrativeConfig`; aggiunto supporto `holdAfterTypingSeconds` in `VoOverlayController` per pacing a gruppi senza troncare testo, usato nel nuovo Beat 1 Wake timed in `DemoStoryDirector` (vedi `DEV REPORT 0103`).
- **Stato codice (2026-05-03):** VO deve restare leggibile **sopra** il **terminale / Control Panel** Bedroom (`BedroomPc*`); richiede ordinamento **tra `PanelSettings` diversi** (`m_SortingOrder` su asset: stack principale `MainMenuPanelSettings` sopra `BedroomPcPanelSettings`) oltre a `UIDocument.sortingOrder` per documenti che condividono lo stesso settings — vedi `DEV REPORT 0105`.
- Il player **può camminare** mentre il VO è attivo (nessun lock movimento obbligatorio).
- **Non è doppiaggio audio obbligatorio sul testo:** VO = **testo in sovraimpressione** con animazione **typing** (stile terminale / CRT: glow cyan, linea, scanline opzionale — ref. allegato utente).
- **Audio:** suono dedicato all’**inizio** e alla **fine** di ogni blocco VO (oltre al typing tick se previsto).
- **Bicolore:** due registri (manutentore vs pragmatico), senza spiegazione in-game.
- **Regola cutscene Alpha (2026-04-18):** per ora le cutscene sono **sequenze di immagini statiche** con elementi in quinta animati via **panning/zooming** (Ken Burns-like) + testo/VO overlay.
- **Aggiornamento Beat 1 (2026-04-28):** sequenza wake divisa in gruppi consecutivi (intro protocollo -> stato operativo verde -> cupola -> stacco ironico -> call to action missione), con passaggio al gruppo successivo solo dopo completamento typing + hold.

### 2.1 — VO Prompt Choice (pattern Beat 4-9)

- **Nuova superficie UI Toolkit:** `VO Prompt Choice`, integrata al layering VO esistente e conforme alla regola UI Builder parity. Può essere un controller sibling del VO overlay o un’estensione esplicita, ma deve esporre un’interfaccia equivalente a `ShowChoice(choiceId, prompt, options, callback)`.
- **Evento standard:** ogni scelta emette `OnVOChoiceMade(choiceId, optionId)`, poi il Director setta flag, lancia un VO follow-up brevissimo e pusha il prossimo objective.
- **Pattern obbligatorio:** `VO_INTERNAL_PROMPT_X` → `CHOICE_X` → `FLAG_X` → `NEXT_BEAT`. Il VO non è solo testo: è anche trigger leggibile della decisione.
- **Flag demo post Beat 3:** `willPreparePayment`, `willMeetMerchantToday`, `merchantPaymentOutcome = paid | broken | avoided_then_confronted`, `fruitsUse = lab | eat_then_lab`, `finalOutcome = keep | sell | give`, `merchantTrustDelta`, `merchantChannelClosed`, `lateDebtAccepted`.
- **Persistenza:** flag e outcome devono essere salvati/ricostruiti solo per sessione demo (`IsDemo` / `isDemoSession`). Nuova partita full non deve vedere scelte, item o stato mercante demo.
- **Agency controllata:** le scelte cambiano VO, reputazione e una conseguenza sistemica leggibile; il contenuto riconverge sulle spine principali **Visitor → Lab → Dome → Finale**.
- **Toast HUD (Beat 5+ dove applicabile):** quando il **Director** rileva che gli item richiesti dalla beat (es. risorse per il patto mercante) sono **reperibili / presenti nell’inventario**, deve innescare una **toast** via **Foundation notifications** (stesso stack/layering già usato per missioni / `PLY-HYD-GAIN`, vedi `DA10-T003` / DEV 0087–0089). Obiettivo: il player **inizia a capire la HUD** (dove compaiono gli avvisi, come aprire ciò che serve) **senza** sostituire il VO con wall of text.

---

## 3 — Armadio / guardaroba

- Interazione con **Armadio** → UI guardaroba aperto.
- Con guardaroba aperto: **rotella mouse** o **frecce su/giù** per cambiare **skin** del player (palette minima 2–3 varianti, coerente con scelta “A”).

---

## 4 — Idratazione (H), cibo (F) e azioni: sensi separati (decisione aggiornata)

**Principio:** `H` e il **cibo / colazione** non condividono lo stesso effetto sulle azioni. Sistema **evolutivo** rispetto all’attuale (stesso binario **demo + full game**).

### Inventory iniziale Demo (lock)

- In sessione **Demo**, l’inventario player iniziale deve contenere **solo**:
  - `5x Acqua potabile`
  - `2x Vegetali sintetici`
- Non devono essere pre-caricati altri oggetti nell’inventario Demo.
- Vincolo applicato al bootstrap Demo (`IsDemo`), senza alterare l’inventario iniziale della Nuova partita full game.

### H — Idratazione → movimento (e game over prolungato)

- `H` bassa **non** toglie direttamente il **conteggio** azioni; modifica la **velocità di movimento** del player (in tempo di gioco, lettura continua o a fascia):
  - **100%** — movimento **normale / veloce** (baseline design)
  - **50%** — circa **metà** velocità
  - **25%** — **molto lento**
  - **0%** — **lentissimo**
- Resta legata a **condensa / umidità Dome**, **calore**, disidratazione nel corso della giornata (`PlayerHydrationSystem` + condensa, come già previsto).
- Se `H` resta a **0%** per **N giorni consecutivi** (N da bilanciare) → **game over**. Contatore `dehydration_gameover_streak` o equivalente.

### F / colazione → numero di azioni al giorno

- Il **numero di azioni** disponibili **quel giorno** è determinato dal **tipo di cibo mangiato a colazione** (tabella per item: quante azioni assegna, es. 1–5).
- **Cap giornaliero:** **5 azioni** (prima bozza era 4; **nuovo tetto 5**).
- **Demo:** si inizia con **1 azione su 5** (tutorial: migliorare colazione → più azioni).
- Le azioni si **resettano all’alba** dopo **End of Day**, come oggi.
- **Pre-flight lock (2026-04-18):** baseline numerica **strict**:
  - `H` speed tiers attivi (100/50/25/0);
  - **game over dopo 2 giorni consecutivi** a `H = 0`;
  - tabella colazione iniziale con **3 tipi cibo** che assegnano azioni 1..5 (tuning valori in sviluppo, struttura bloccata).

### Top Bar — tooltip “Actions”

- Aggiungere **tooltip** (e testi localizzabili) sulla voce **Actions** che spiega:
  - **perché** il player ha quel numero di azioni oggi (colazione, tipi di cibo);
  - **legame con la camminata** (riduzione velocità per `H` bassa);
  - struttura estensibile per **effetti strani** da alimenti futuri (full game).

### Note implementative

- `PlayerPerspectiveMover2D` (o equivalente): moltiplicatore velocità da `H` (curve o step).
- `GameManager` / `ActionSystem`: `actionsPerDay` massimo **5**; assegnazione mattutina da **BreakfastResolver** (nome indicativo) che legge consumo colazione.
- Vecchi modelli “F1/F2/F3 streak unificato” su **azioni** sono **sostituiti** da questo split **H = movimento**, **F/colazione = azioni**.

---

## 5 — Bedroom PC: hub operativo (Power + analisi Wiki/Ricerca + FAQ + Black Market)

**UX (sviluppo aggiornato — 2026-05)**

- **Non** un toggle ON/OFF ripetuto su ogni HUD macchinario.
- Il **PC in Bedroom** è un **terminale diegetico unico** per l’operatore: **comando energie**, **consultazione** di quanto già studiato/sbloccato (wiki, ricerche), **assistente FAQ** e accesso al **Black Market**. **Non** sostituisce il **letto** per chiudere la giornata.
- **Flow serale tipico (diegetico, non obbligatorio)**: check opzionale `Pannello Controllo` → mercato opzionale → lettura `Wiki`/ricerche → FAQ se serve → **chiusura PC** → **interazione con il letto** per avviare l’EoD esistente.
- **Implementazione power invariata nel principio**: ON/OFF resta centralizzato dentro `Pannello Controllo`, non distribuito su HUD macchina.

### 5.1 — Information architecture desktop (Alpha)

- Desktop con **4 icone fisse** (nessuna CTA sonno sul PC):
  - `Pannello Controllo`
  - `Centro di ricerca` / `Wiki` (consultazione archivi, ricerche notturne sbloccate, indirizzo verso DB botanico/report — vedi evoluzione §5.5 allegato 2)
  - `Bot FAQ`
  - `Black Market`
- `Wiki` (target di prodotto) contiene 3 sezioni interne:
  - `Diario Report` (report giorni precedenti + report corrente)
  - `Wiki Plants` (DB scoperte piante)
  - `Research` (albero ricerca / nodi disponibili)
- `Pannello Controllo` resta il **solo punto autorizzato** per ON/OFF circuiti e warning di rischio.

### 5.2 — Scope funzionale Alpha (v1)

- **Pannello Controllo**: toggle circuiti + impatto CRY + warning conferma su `Seed Storage OFF`.
- **Wiki**: visualizzazione coerente con progressione demo (contenuto anche placeholder/fake dove previsto dal piano).
- **Bot FAQ**: set iniziale 8-12 FAQ contestuali (es. piantare, reputazione, azioni, idratazione).
- **Black Market**: accesso rapido al flusso market gia` previsto, senza duplicare logiche in Wiki.
- **Fine giornata**: **solo dal letto** (stesso `EndOfDaySequence` / flusso già presente in progetto). Il PC **non** espone un pulsante di sonno.

### 5.3 — Flow serale guidato (diegetico)

1. Player apre PC Bedroom (home hub 4 tile).
2. Check opzionale su `Pannello Controllo`.
3. Passaggio opzionale su `Black Market`.
4. Consultazione `Wiki` / ricerche (archivio, stato sblocchi).
5. Supporto rapido `Bot FAQ` se richiesto.
6. Chiusura PC → player va al **letto** → trigger EoD esistente.

Nota UX: eventuali reminder copy («/controllato il report?») restano **soft** (FAQ o VO), senza obbligare il passaggio dal PC prima del letto.

### 5.4 — Gate tecnici e vincoli architetturali del task

- UI desktop e app interne sullo **stesso stack Panel Settings** (layering coerente HUD/toast/modali).
- Nessuna duplicazione runtime: demo/full condividono stesso flusso e stessi servizi.
- Servizi nuovi solo via `ServiceContainer` (no scene scan aggiuntivi).
- **Pannello di Controllo** come **unica superficie autorizzata** nel terminale per ON/OFF e costi visibili lì; le altre app (FAQ, mercato, archivio) **non** replicano regole energetiche.

#### 5.4.1 — `MachinePowerState`: cosa diceva il piano originale vs cosa c’è nel repo

**Piano originale (gap map / breakdown §5, decisioni 2026-04):**

- Il documento assumeva un **backend unificato** indicato come **`MachinePowerState`**: un unico posto (servizio o aggregato) che tiene lo stato energetico dei circuiti, coordina **costi CRY giornalieri**, **deperimento Seed Storage quando OFF**, **stall/sanità Dome (CRYO/vasi)** e **Food room**, così economia e HUD non divergono.
- Il breakdown suggerito nominava esplicitamente **`BedroomPcAppPowerControl` (UI + binding su `MachinePowerState`)** e testi tipo *“Backend power invariato: `MachinePowerState` condiviso…”*.

**Implementazione attuale su main:**

- **Non esiste** una classe o servizio registrato col nome `MachinePowerState`.
- `BedroomPcDisplayController` lega il **Pannello di Controllo** direttamente a:
  - **`SeedStorageSystem`** (toggle potenza, costo da `ComputeDailyCryCost`, ecc.),
  - **`FoodRoomSystem`** (sintetizzatore alimentare / dispensa),
  - **flag locali** per **CRYO macchina** e **compost** nel ramo control-panel (comportamento utile alla **demo** senza aver ancora estratto un modello macchina unico da tutto il Vault).
- **Seed Storage** UI (`SeedStoragePanelController`) continua a parlare con **`SeedStorageSystem`**; l’evento `SeedStoragePowerSetFromControlPanel` collega Director/missioni al toggle da PC.
- **Interpretazione:** è una **composizione pragmatica** “dai servizi esistenti verso l’UI” invece di un **facade** dedicato `MachinePowerState`. Funzionalmente il vincolo di prodotto *“un solo posto nel terminale per accendere/spegnere circuiti”* è rispettato; il vincolo architetturale *“una sola classe fonte di verità globale”* è **parzialmente** soddisfatto (verità spalmata tra sistemi + controller) finché non si introduce un servizio unico o si rinomina/refactora in `MachinePowerState` (o equivalente in `ServiceContainer`).

### 5.5 — Concept visivo lock (allegati 2026-04-20)

- **Allegato 1**: homepage desktop `Bedroom PC Desktop` con 4 tile/app (`Control Panel`, `Wiki`, `Bot FAQ`, `Black Market`).
- **Allegato 2**: schermata `Wiki` con 3 sezioni interne (`Diary Report`, `Wiki Plants`, `Research`).
- **Allegato 3**: schermata `Control Panel` (overview energia + blocchi machinery/health/costi/warning).
- **Allegato 4**: schermata `Bot FAQ` (search + categorie + lista Q/A contestuali).
- **Allegato 5**: schermata `Black Market` (sell inventory + catalogo acquisto + system log).
- **Vincolo UX (aggiornato 2026-05):** **nessuna** CTA “vai a dormire” sul PC; **CTA primaria serale** = **letto** in scena. Il terminale resta leggibile in home con hint di chiusura (es. ESC) e le 4 tile funzionali.
- Direzione art/UI: tema terminale retro-CRT consentito come evoluzione stilistica, mantenendo IA e gerarchia funzionale di questo task (4 app + sonno fuori dal PC).

### 5.6 — Tracking sviluppo per allegato (task board rapida)

Legenda stato: `TODO` | `IN PROGRESS` | `DONE` | `DONE (placeholder)` | `N/A`

| Codice      | Allegato | Schermata / modulo                                   | Scope di sviluppo (Alpha)                                                    | Stato  | Owner | Evidenza                 |
| ----------- | -------- | ---------------------------------------------------- | ---------------------------------------------------------------------------- | ------ | ----- | ------------------------ |
| `DA10-T017` | **1**    | `Bedroom PC Desktop` (home hub)                      | Shell desktop, **4 icone** app (nessun sonno sul PC)                        | `DONE` | `TBD` | `BedroomPcDisplay.uxml` — griglia 4 tile + hint chiusura |
| `DA10-T018` | **2**    | `Wiki` (`Diary Report` / `Wiki Plants` / `Research`) | Navigazione tab + binding contenuti demo/fake dove previsto                  | `DONE` | `TBD` | **Scope demo:** vista “Centro di ricerca” + `WikiUnlockService`; **residuo:** 3 tab allegato 2 |
| `DA10-T007` | **3**    | `Control Panel`                                      | ON/OFF circuiti, costi CRY, warning Seed Storage OFF, riepilogo health/costi | `DONE` | `TBD` | `BedroomPcDisplayController` + servizi; **residuo:** modale conferma OFF (§5.2); backend vs §5.4.1 |
| `DA10-T019` | **4**    | `Bot FAQ`                                            | Search, categorie, lista Q/A (8-12); backlog **contenuti/AI**                  | `DONE (placeholder)` | `TBD` | `HandleFaqBot` copy statico; **contenuti/UI allegato 4** = backlog prodotto (AI opzionale) |
| `DA10-T020` | **5**    | `Black Market`                                       | Entry dal desktop, sell/buy base, log operazioni                             | `DONE` | `TBD` | `BedroomPcTerminal` → `UIBlackMarket.Show` |
| `DA10-T021` | —        | *(archiviato)*                                       | Era: CTA sonno su hub — **ritirato**: EoD **solo letto**                      | `N/A`  | —     | Codice task conservato per storia; nessun lavoro richiesto sul PC |


**Regola aggiornamento board:** a ogni consegna task, aggiornare almeno `Stato` + `Evidenza` (piu`eventuale`Owner`) in questa tabella.

**Regole gameplay (confermate)**


| Circuito                              | OFF — effetto                                                                                                                                                        | Risparmio CRY/giorno (bozza)  |
| ------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------- |
| **Seed Storage**                      | Tutti gli item in storage subiscono **deperimento** come gli **organici in inventario** (logica già esistente). **Warning esplicito** in UI prima di confermare OFF. | **5 CRY** (tuning successivo) |
| **Terminal POT & CRYO** (Dome / vasi) | Tutte le piante in **blocco condizione crescita** (perenne finché OFF — per **demo** basta stallo globale; **full game** si raffina).                                | **10 CRY**                    |
| **Food Kitchen**                      | Tutti i **processi** di cibo e **potabilizzazione** si **annullano** / non completano.                                                                               | **5 CRY**                     |


- Costi CRY quando accesi = stessi valori come **costo fisso giornaliero** da consolidare in bilanciamento.
- **Lab** e altri circuiti: aggiungere in tabella quando definite (piano precedente includeva Lab a parte).

**Implementazione (breakdown suggerito — rispetto al repo attuale):**

- Shell + app confluite in **`BedroomPcDisplay` (UXML/USS) + `BedroomPcDisplayController` + `BedroomPcTerminal`** (entry mondo + Black Market), invece dei nomi modulari sotto (ancora utili come checklist).
- `BedroomPcDesktopUI` (shell: icone, focus, navigazione — **nessun** sleep).
- `BedroomPcAppPowerControl` → **effettivo:** binding diretto a **`SeedStorageSystem` / `FoodRoomSystem` / flag locali** (vedi §5.4.1); *piano storico:* binding su un unico `MachinePowerState`.
- `BedroomPcAppWiki` (tab `Diario Report` / `Wiki Plants` / `Research` — **evoluzione** rispetto alla vista singola demo).
- `BedroomPcAppFaqBot` (FAQ — **placeholder** ok per demo; contenuti ricchi = backlog).
- `BedroomPcAppBlackMarketEntry` (entrypoint market).
- **`BedroomPcSleepAction`**: **non previsto** — EoD resta sul **letto** / flusso interazione camera da scena.

Backend power: vedi **§5.4.1** (nessun tipo `MachinePowerState` in codice oggi).

---

## 6 — Visitor Room + Visitor Desk (UI terminale)

**Nomenclatura:** **Visitor Room** = stanza diegetica / contesto (`ROOM_Visitor`, mondo). **Visitor Desk** = **pannello UI Toolkit** unificato (due modalità **MISSION** / **TRADE**) che il player apre in Visitor Room (o equivalente). Specifica concettuale completa (palette, layout, trade math, CRT, header, esempi non canonici): [**`Assets/_Project/Docs/VISITOR_DESK_SPEC.md`**](d:/Sporae_Build_Beta/Assets/_Project/Docs/VISITOR_DESK_SPEC.md).

**Riferimento Visitor Desk (full scope) — sintesi**

- Due modalità: **MISSION** (ritratto ologramma, biometrico, box missione, Accept/Decline/Postpone, conversazione typing + **2 opzioni** per turno) e **TRADE** (Faction Offers vs Your Inventory, bilancio, **FORECAST** se selezioni entrambe le parti, stati risposta visitatore, Propose/Cancel).
- **Trade:** regole percentuali (±15% accetto; 15–40% dubbioso; >40% accetto entusiasta se overpay / rifiuto se underpay) con effetti **reputazione** come da spec.
- **CRT:** vignetta, grana, scanline, particelle, frame metallico, glitch periodico + su propose.
- **Header:** LED ciano/oro, REP/CRY/DAY, chiusura X.
- **Implementazione:** colori come **token USS** (`.visitor-desk-*`), parity UI Builder; servizi via `ServiceContainer` (vedi spec § Allineamento).
- **Strategia implementazione:** la **capacità** Desk (UXML/USS, due modalità, trade math, CRT…) vive nel **binario unico** e si espande col full game; vedi spec. **Strategia demo:** il Desk in Alpha è **cabina narrativa del Mercante** (vertical slice), non l’esposizione del sistema economico — dettaglio in **`VISITOR_DESK_SPEC.md` → “Demo vs full Visitor Desk”** e §**6.4** sotto.

- **Da creare** il Visitor Desk (ref. mockup + spec: tab **MISSION** / **TRADE**, profilo NPC, mission details, conversazione typing, motore trade per full game, ecc.).
- **Demo (beat 4–9):** **solo Mercante Ombra**, scambi **scriptati**, **flag narrativi**; allineare copy §6.3 alla **superficie** Desk senza trade libero né reputazione sistemica (vedi tabella §6.3 e §6.4).
- **Full game:** espansione cataloghi, fazioni, trade complessi, stato trust, ecc. — stesso layout evoluto.
- **Pre-flight lock (2026-04-18):** scope `visitor_mid` aggiornato al vertical slice:
  - UI Desk coerente con spec; in **demo** niente **trade libero** — solo flussi **scriptati** (es. acqua/cibo ↔ patto) dove serve gameplay;
  - variazioni copy legate al ramo scelta;
  - nessuna reputazione **sistemica** in demo: **flag** + copy Mercante; eventuale REP in header solo **decorative** se presenti in layout.
- **Beat 4-5 — flow aggiornato (2026-05-04):**
  - Trigger: completamento Beat 3 / Seed Storage acceso dal PC Bedroom.
  - VO: “C’è qualcuno al Visitor Center.” Objective: raggiungi Visitor Center / Visitor Room.
  - Chat Mercante Ombra: sarcasmo, identità ambigua, commenti sull’amnesia del Biologo, zero conferme lore profonde.
  - Contract beat: grant **4 frutti demo** a credito; pagamento promesso il giorno dopo = **`1x WAT-POT` + `1x FOOD-101`**.
  - Choice `prepare_payment`: “Lo prepariamo?” Sì → abilita/illumina processi acqua+cibo come next step; No → VO tagliente, objective soft verso letto/esplorazione, flag ramo rotto.
  - **Toast Foundation (HUD):** appena l’inventario contiene gli item **obiettivo** del pagamento (stessa logica che abilita lo scambio al Visitor), `DemoStoryDirector` (o watcher demo registrato) emette una **toast** che punta alla **HUD** (notifica + invito a verificare inventario / stato) — **alfabetizzazione** al canale notifiche, non secondo tutorial a lista di pulsanti.
  - Mattina dopo, Choice `meet_or_avoid_merchant`: se risorse pronte, “Lo chiamiamo?”; se assenti, “Lo affrontiamo lo stesso?” Evitare non salta contenuto: il confronto si triggera entrando nel Visitor Center o richiamandolo dalla Visitor Room.
- **Risoluzione incontro:**
  - `merchantPaymentOutcome = paid`: scambio acqua+cibo, debito saldato, reputazione positiva/nessuna penalità, reveal pulito verso Lab.
  - `merchantPaymentOutcome = broken`: pagamento assente o rifiutato, tono freddo/deluso, `merchantTrustDelta < 0`, condizioni scambio peggiori / `merchantChannelClosed` se ramo richiede chiusura.
  - `merchantPaymentOutcome = avoided_then_confronted`: evitare sposta tono e timing, ma converge sul confronto e sulla stessa progressione Lab.
- **Mission hook:** nel ramo pagato il mercante riformatta il valore dei frutti (“non mangiarli, portali al Lab”); nel ramo rotto lascia una traccia sporca di disprezzo che nomina il Lab e il **Cetriolo d’Oro** senza tutorial extra.

### 6.3 — Dialoghi Mercante Ombra ↔ Player (copy bloccato demo)

*Comportamento UI (layout MISSION/TRADE, typing, trade math, CRT, header): **`Assets/_Project/Docs/VISITOR_DESK_SPEC.md`**. I dialoghi sotto sono **copy**; il binding a nodi conversazione Desk (modello *N turni × 2 risposte*) o a layer VO è decisione implementativa — vedi tabella **Scene ↔ modalità Desk**.*

| Scene / beat (narrativa) | Modalità Visitor Desk predominante | Note |
| ------------------------- | ----------------------------------- | ------ |
| **SCENA 1** (credito + frutti) | **MISSION** + pannello conversazione sotto (o tab dedicata “Transmission”) | Grant 4 frutti + `prepare_payment` può restare **VO Prompt Choice** esterno o integrato nel Desk; allineare gating. |
| **SCENA 2A** (pagato) | **TRADE** (scambio `WAT-POT` + `FOOD-101`) **oppure** scambio scriptato one-click | Full Desk: usare colonna inventari + bilancio; demo trim: possibile **script** senza FORECAST. |
| **SCENA 2B** (patto rotto) | **MISSION** conversazione + eventuale stato **Refused** / copy cold in pannello risposta | Stesso layout; nessun trade completato. |
| **SCENA 3** (Cetriolo d’Oro) | **MISSION** (box scelta `keep` / `sell` / `give`) **oppure** `VO Prompt Choice` + Desk solo display | `golden_cucumber_outcome` + varianti A/B tono mercante. |

**Nota di tono:** il Mercante parla sempre troppo per qualcuno che “non è nessuno”, usa **metafore commerciali** e **allusioni fisiche** senza mai nominare esplicitamente atti sessuali. Vena **erotica nascosta**, **doppio senso spinto**, **satirico**, **non volgare**. Il Player può scegliere risposte più **“puro professionale”** oppure più **“sta al gioco”** (branching risposte: da mappare in `VO Prompt Choice` / Visitor UI insieme al flusso tecnico).

#### SCENA 1 — Primo incontro al Visitor Center (il “credito” e i frutti)

**MERCANTE OMBRA:**  
Oh. Guarda un po’. Il Dome ha pure un Visitor Center… e dentro c’è un visitatore. Che coincidenza romantica.

**PLAYER:**  
Sei tu il visitatore?

**MERCANTE OMBRA:**  
Dipende da chi me lo chiede. Tu sembri… nuovo. Nuovo o smemorato: stessa faccia da scaffale vuoto.

**PLAYER:**  
Non ricordo molte cose. Ma ricordo che non ti ho invitato.

**MERCANTE OMBRA:**  
E io ricordo che fuori la gente paga per essere invitata. Qui invece tutto gratis: aria filtrata, pareti calde, e un biologo che non sa neanche se gli piace il proprio nome.

**PLAYER:**  
Cosa vuoi?

**MERCANTE OMBRA:**  
Io? Nulla. Io passo. Annuso. Valuto. E quando vedo qualcuno spaesato, ogni tanto faccio beneficenza… ma solo per sport.  
*(pausa)*  
Ti vedo secco, amico. Secco in testa e forse anche altrove.

**PLAYER:**  
Altrove dove?

**MERCANTE OMBRA:**  
Nelle riserve. Nelle scorte. Nel… carattere. Non fare il letterale, ti invecchia.

**PLAYER:**  
Parla chiaro.

**MERCANTE OMBRA:**  
Chiaro è noioso. Però va bene. Ho della frutta. Roba scarsa, a dirla tutta. Naturalisticamente parlando, è quasi un insulto all’agricoltura.  
Ma… in certi posti… anche gli insulti hanno un mercato.

**PLAYER:**  
Che tipo di frutta?

**MERCANTE OMBRA:**  
Frutta che non dovrebbe interessarti. E proprio per questo ti interesserà.  
*(fa scorrere l’immagine / mostra i frutti)*  
Vedi? Forma discutibile. Superficie… “caratterizzata”. Durezza… sorprendente.

**PLAYER:**  
Sembra… un cetriolo.

**MERCANTE OMBRA:**  
Sembra tante cose, se uno ha fantasia. E la fantasia, fuori, è una valuta più stabile dell’acqua.

**PLAYER:**  
Vuoi venderla?

**MERCANTE OMBRA:**  
No. Te la do. A credito.  
Non perché ti meriti qualcosa—ma perché sei il tipo che, domani, può diventare utile. O divertente.  
*(sorriso)*  
Non pagarmi adesso. Pagami domani.

**PLAYER:**  
E cosa vuoi domani?

**MERCANTE OMBRA:**  
Acqua. Cibo. Niente di… intimo.  
Solo il minimo per dimostrare che sei capace di mantenere una promessa senza farti venire l’orticaria morale.

**PLAYER:**  
E se non lo faccio?

**MERCANTE OMBRA:**  
Allora io non mi offendo. Io registro.  
E credimi: io ho una memoria bellissima.

**PLAYER:**  
Tornerai davvero?

**MERCANTE OMBRA:**  
Se mi conviene. E se mi fai venire voglia.  
Domani. Qui. Non farmi aspettare: fuori la pazienza costa cara.

#### SCENA 2A — Giorno dopo, Player ha preparato acqua e cibo (scambio “pulito”)

**MERCANTE OMBRA:**  
Ah. Guardalo. Ha funzionato: sei riuscito a produrre qualcosa senza farti esplodere.

**PLAYER:**  
Ecco acqua e cibo. Come promesso.

**MERCANTE OMBRA:**  
Promesso… che parola elegante per un mondo che baratta con le unghie.  
*(controlla)*  
Mmh. Idratazione decente. Nutrienti accettabili. Sai fare il minimo sindacale. È già raro.

**PLAYER:**  
Quindi siamo pari.

**MERCANTE OMBRA:**  
“Pari” è una favola che raccontano i contabili. Tra noi due non esiste pari: esiste leva.  
Però sì: il tuo debito è pagato.

**PLAYER:**  
E ora? Che ci faccio con quei frutti?

**MERCANTE OMBRA:**  
Li mangi e ti senti vivo per cinque minuti… oppure li tratti come meritano.  
Non sono cibo. Sono un biglietto. Un biglietto per un pubblico… particolare.

**PLAYER:**  
Che pubblico?

**MERCANTE OMBRA:**  
Uno che non fa troppe domande, ma paga bene se la merce è… resistente.  
E quella roba lì, amico mio, è famosa per due cose: dura e piena di… “personalità”.

**PLAYER:**  
Stai parlando di mercato nero.

**MERCANTE OMBRA:**  
Io parlo di mercato e basta. Il colore lo mette la coscienza di chi compra.  
Senti bene: se li porti in laboratorio, non ti esce “più cibo”. Ti esce una cosa che quasi nessuno sa più fare. Un seme nuovo.  
Un seme per un frutto che ha un nome ridicolo e un potere ridicolo: il Cetriolo d’Oro.

**PLAYER:**  
Perché “d’Oro”? Vale davvero qualcosa?

**MERCANTE OMBRA:**  
Naturalisticamente? Quasi niente.  
Socialmente? È una chiave.  
E ci sono persone là fuori che pagherebbero per… sentirsi potenti, anche solo per qualche minuto.  
Tu non devi capire. Tu devi produrlo.

**PLAYER:**  
E tu cosa ci guadagni?

**MERCANTE OMBRA:**  
Io? Io guadagno che tu smetti di essere un soprammobile.  
E magari, domani, mi porti qualcosa che merita di essere toccato.

#### SCENA 2B — Giorno dopo, Player NON ha preparato (delusione fredda, politicamente scorretta ma non volgare)

**MERCANTE OMBRA:**  
Sono tornato. Guarda che disciplina: io mantengo la parola.

**PLAYER:**  
Non ho acqua e cibo.

**MERCANTE OMBRA:**  
*(silenzio corto)*  
Ah. Perfetto.  
Sai cosa sei? Sei un esperimento riuscitissimo: un essere umano capace di trasformare un favore in una figuraccia.

**PLAYER:**  
Non ti devo niente.

**MERCANTE OMBRA:**  
Giusto. Tecnicamente.  
E tecnicamente un coltello è solo metallo. Poi qualcuno lo mette nel punto sbagliato e diventa una scelta di vita.

**PLAYER:**  
Ti stai arrabbiando?

**MERCANTE OMBRA:**  
No, no. Io non mi arrabbio. Io declasso.  
Tu ieri eri “potenziale”. Oggi sei “rischio”.

**PLAYER:**  
Allora riprenditi i frutti.

**MERCANTE OMBRA:**  
No. Tienili.  
Così ogni volta che li guardi ti ricordi che, nel post-mondo, la reputazione è l’unica cosa che non puoi filtrare con una macchina.

**PLAYER:**  
E tu cosa farai?

**MERCANTE OMBRA:**  
Io? Io farò quello che faccio sempre: troverò qualcuno che sa mantenere un patto senza sentirsi violato nell’orgoglio.

**PLAYER:**  
Quindi niente affari.

**MERCANTE OMBRA:**  
Affari sì. Con te… condizioni peggiori. Molto peggiori.  
E un consiglio gratis, perché oggi mi sento misericordioso: non mangiare quella roba come se fosse insalata.  
Portala in laboratorio. Se hai ancora un briciolo di curiosità in quel cranio vuoto, capirai perché là fuori la gente paga per oggetti… imbarazzanti.

**PLAYER:**  
Perché la pagherebbero?

**MERCANTE OMBRA:**  
Perché quando non hai più futuro, ti compri almeno una fantasia.  
E certe fantasie vogliono una cosa sola: durezza e forma.  
*(si avvicina quel tanto che basta a infastidire)*  
Tu però non venderai fantasia. Tu venderai controllo.  
Se sei capace di produrlo.

**PLAYER:**  
E se lo produco?

**MERCANTE OMBRA:**  
Allora magari, un giorno, smetti di essere una delusione e diventi… una tentazione.  
Ci vediamo. O non ci vediamo. Dipende da te. E da quanto impari in fretta.

#### SCENA 3 — Dopo il laboratorio: “Che facciamo col Cetriolo d’Oro?” (ricucire / vendere / tenere)

Questa scena parte **dopo** la sequenza Lab che produce il Cetriolo d’Oro; può innescarsi sia nel ramo **pagato** sia nel ramo **patto rotto**, cambiando l’atteggiamento del mercante (allineare a `merchantPaymentOutcome` e a `golden_cucumber_outcome`).

**Variante A — Mercante “neutrale” (hai pagato)**

**MERCANTE OMBRA:**  
Lo sento da qui. Hai tirato fuori qualcosa che non dovrebbe esistere più.

**PLAYER:**  
È questo il Cetriolo d’Oro.

**MERCANTE OMBRA:**  
Sì. È brutto. È duro. È pieno di bozze come una brutta verità.  
E per questo… è perfetto.

**PLAYER:**  
E ora?

**MERCANTE OMBRA:**  
Ora fai la tua prima scelta adulta: lo tieni, lo vendi, o lo usi per comprare una relazione.

**Variante B — Mercante “freddo” (non hai pagato)**

**MERCANTE OMBRA:**  
Non pensavo che avessi abbastanza spina dorsale per arrivarci.

**PLAYER:**  
Ce l’ho.

**MERCANTE OMBRA:**  
Mmh. Allora vediamo se ce l’hai anche per chiudere un conto.  
Quella cosa può valere poco… o molto. Dipende da chi la stringe e perché.

**PLAYER:**  
Vuoi che te la dia.

**MERCANTE OMBRA:**  
Io voglio vedere se capisci il gioco.  
Dammi il frutto e io “dimentico” un pezzetto della tua figuraccia.  
Vendilo e io ti considero definitivamente… inaffidabile.  
Tienilo e dimmi che tipo sei: prudente o solo… geloso.

### 6.4 — Demo vs full Visitor Desk (trim)

**Strategia codice:** costruire la **shell Visitor Desk** (MISSION/TRADE, layout, CRT, ecc.) come capacità del prodotto unico — vedi [`VISITOR_DESK_SPEC.md`](d:/Sporae_Build_Beta/Assets/_Project/Docs/VISITOR_DESK_SPEC.md).

**Strategia demo (core narrativo — non confondere col “mini-sistema”):** in Alpha il Desk **non** espone il gioco economico completo: è la **cabina del Mercante Ombra** (contratto, scelte, conseguenze, finale). **Demo:** solo Mercante Ombra, **no trade libero**, **no reputazione sistemica**, solo **flag narrativi**; scambi **scriptati** dove servono acqua/cibo. **Lab** = ricetta guidata **Il Piacere Dimenticato** (no sandbox). **Dome** = piantare, crescita compressa, raccolta **Cetriolo d’Oro** (no gestione estesa). **Scelte** = poche, esplicite, esiti corti ma leggibili. **Finale** = stesso **glitch**, variante **cromatica/copy** sul comportamento. Obiettivo: **vertical slice** — trailer giocabile del potenziale (“sotto c’è un sistema enorme”), non tutorial di tutto Sporium — dettaglio tabella e checklist nello spec § *Demo vs full Visitor Desk*.

---

## 6.1 — Inventory UI revamp (allegato 1)

**Obiettivo:** sostituire la GUI placeholder con terminale inventory in stile Sporium (UI Toolkit Foundation), mantenendo i sistemi item esistenti.

- Layout target: header terminale (`H2O`, `Actions`, `Items`), filtri categoria, lista item con righe ad alto contrasto, pulsanti `VIEW`/`USE`, quantità evidenziata.
- Lato gameplay: i bottoni `USE` agganciati al sistema reale consumo (acqua/cibo/consumabili), coerente con nuovo modello `H/F`.
- Tooltips: conformi alla regola `item-tooltip.mdc` (titolo/sottotitolo + dettagli strutturati, no typeId grezzi).
- Vincoli Foundation parity: nessun binario separato authoring/runtime; placeholder realistici in UXML, override dato-dipendenti da controller.
- Demo vs full: stessa UI base; in Alpha è consentito limitare categorie/azioni ma senza creare inventario alternativo.

---

## 6.2 — Seed Storage UI revamp (allegato 2)

**Obiettivo:** portare Seed Storage da interfaccia placeholder a pannello terminale completo, coerente con Inventory e con le regole deperimento/power.

- Layout target: doppia colonna (`Player Inventory` vs `Seed Storage Interface`), slot storage con stato (viability, quantità, lock slot), system log.
- Indicatori runtime chiari: connessione, temperatura, power%, costo giornaliero e warning deperimento quando Seed Storage è OFF.
- Azioni minime Alpha: transfer to storage / retrieve from storage, con feedback visivo + log locale.
- Integrazione con pannello elettrico Bedroom PC: stato ON/OFF e costi riflessi in Seed Storage UI.
- Demo vs full: stessa base UI; lock slot e metriche scriptabili in Alpha ma strutturate come design definitivo.

---

## 7 — Ricerca notturna (EoD)

- Struttura **OK come oggi** (Historical / Botanical / Vault).
- Per la demo: **contenuti fake** (testi risultato, rami, wiki) — poi sostituiti in full game.
- Wiki unlock: `WikiUnlockService` + voci placeholder.

---

## 8 — Dawn Summary

- Base: **DAWN SUMMARY esistente** (`EndOfDaySequenceController`).
- Per la demo: **integrare** ciò che manca (eventi/copy specifici); in full game si espande dopo Alpha.

---

## 9 — Reputazione

- **Sistema fake** per demo, ma con conseguenze percepibili: `merchantTrustDelta`, `merchantChannelClosed`, condizioni scambio peggiori, o reputazione positiva se il patto è rispettato.
- I valori non devono aprire un sistema full di fazioni: servono solo a rendere leggibili cooperazione, rottura e riparazione tardiva nel flow Visitor → finale.

---

## 10 — Lab — “Crea nuovo seme”

- Far provare l’**intera sequenza attualmente esistente** “Crea nuovo seme” (feedback community + validazione flusso).
- **Nessuna scena Lab separata**: il loop Lab resta nell’**area Lab di `SCN_VaultMap`** (macchinari già presenti in mappa).
- **Output demo-only:** la sequenza deve produrre un seme reale della pianta **Il Piacere Dimenticato**. La pianta è contenuto Plants/PlantData demo-only; il frutto raccolto dalla pianta è l’item Fruit **Cetriolo d’Oro**.
- **Choice `fruit_use`:** prima del Lab il VO chiede se usare i frutti o consumarli. `lab` entra direttamente nel Lab; `eat_then_lab` consuma **1 frutto**, registra il flag, cambia il VO (“forse abbiamo buttato via qualcosa”), ma lascia abbastanza frutti per completare la sequenza.
- **Vincolo asset/dati:** niente alias puramente narrativo e niente grant finale finto come percorso primario; seed, plant e fruit devono attraversare Lab/Dome come dati demo reali sotto gate `IsDemo`.

---

## 11 — Crescita pianta (demo time-skip)

- Usare il sistema **reale** almeno fino allo **stadio 2** della pianta **Il Piacere Dimenticato**.
- Dopo il cambio giorno: in **End of Day**, il contatore/testo giorni è **scriptato** per mostrare esattamente **“A few days later”** (non il giorno numerico successivo reale), così al risveglio la pianta è **matura con Cetriolo d’Oro raccoglibile**.
- Richiede ramo demo nel `DayCycleSystem` / Director che **non corrompa** il save full game: solo sotto `IsDemo` o flag sessione demo.
- Tutorial POT/Dome: spiegazione minimale, limitata alle azioni necessarie per piantare e portare la pianta alla soglia di compressione temporale.

---

## 12 — Scelta finale + EoD no-spoiler

- In Alpha la scelta finale è una **choice a 3 opzioni** sul Cetriolo d’Oro: `keep`, `sell`, `give`.
- Ramo A / cooperazione: `give` è reputazione, `sell` è valore immediato, `keep` è conservazione/curiosità privata.
- Ramo B / patto rotto: `give` è riparazione tardiva (`lateDebtAccepted = true`, accetta ma non perdona), `sell` chiude il canale mercante, `keep` diventa isolamento/occultamento.
- Le micro-sequenze EoD **sostituiscono lo step Snapshot**.
- Qualunque opzione registra `finalOutcome` e converge nel **glitch finale**, con colorazione diversa ma senza moltiplicare contenuto post-demo.

---

## 13 — Finale demo (chiusura)

1. **~10 secondi** di controllo personaggio con **musica**.
2. **Fade out** a schermo nero.
3. Schermata **Demo complete**: **wishlist**, **invito Discord** (feedback, opinioni, aggiornamenti, contatto con Jeff).
4. **Modulo feedback** in-demo: cosa è piaciuto / non piaciuto (e perché), cosa manca, cosa vorresti, **voto generale**.
5. **Testo chiaro:** gioco sviluppato da **un solo dev** con **supporto AI per la parte di coding**; **Vittorio “Jeff” Conti**, ex senior artist Rockstar Games, passione per giochi nello stile di Sporium.

- **Pre-flight lock (2026-04-18):** feedback module scope = `**in_game_form`** (form completo in Alpha con submit endpoint).
- Chiusura narrativa: mostrare **effetto** (IM/mutazione) e lasciare **una domanda aperta**, senza spiegare la causa completa.

---

## 14 — Integrazione piano ascensore (demo + full game)

Integrazione del piano `[elevator_ux_doors_display_68f104c1.plan.md](c:/Users/UTENTE/.cursor/plans/elevator_ux_doors_display_68f104c1.plan.md)` nel percorso principale.

**Scope integrato (obbligatorio):**

- Call button per piano con arrivo cabina (~2s) e apertura porte.
- Porte doppie (`PortelloneSx`, `PortelloneDx`) con stato open/close.
- Display piano corrente sopra cabina.
- Comandi dentro cabina: **Su/Giù un piano per volta**.
- Player nascosto quando dentro cabina durante viaggio, visibile alla riapertura.

**Vincoli architetturali:**

- Nessuna scena alternativa: tutto in `SCN_VaultMap`.
- Refactor incrementale su `ElevatorSystem` + eventuale `ElevatorCallButton`, senza rompere interazioni correnti.
- Applicare regole runtime/UI correnti del progetto (ServiceContainer per nuovi servizi, no scene scans nuovi in logiche centrali, layering coerente).

---

## Narrative Red Lines (anti-spoiler)

- Massimo **1 informazione lore nuova per beat**.
- Ombra comunica obiettivi e pressione esterna, non verita storiche complete.
- Vietate frasi che confermano esplicitamente il twist dell'Atto V.
- Il VO bicolore deve suggerire conflitto interno, non spiegare identita/causa.
- Finale: evidenziare mutazione + hint Piano -3, evitare spiegazioni causali esaustive.

---

## Audit compliance (repo + report + regole Cursor)

### Verdetto sintetico

**Compliant with gates** (eseguibile se i gate sotto restano attivi durante implementazione e QA).

### Elementi compliant

- **Un solo binario demo/full**: coerente con regola no-duplicazioni e con direzione repo (stesse scene/sistemi, orchestrazione scriptata).
- **Riuso del Lab esistente** (`CREA NUOVO SEME` completo): allineato a DEV REPORT 0084 (orchestrazione UI senza duplicare simulazione).
- **EOD/Dawn come perno**: coerente con implementazione documentata in DEV REPORT 0055.
- **Pannello unico di controllo elettrico in Bedroom PC**: riduce rischio di duplicare logiche su molte HUD macchina.

### Gate di compliance (obbligatori)

1. **Regola “analysis-no-suppositions”**
  - Ogni revisione/analisi in corso d'opera deve citare check freschi su repo corrente.
2. **Runtime architecture (`ServiceContainer`, no scene scans)**
  - Vincolo operativo: nuove feature (`DemoSessionState`, **facade** unificato macchinari se introdotto — *cf.* §5.4.1 e nome storico `MachinePowerState`, `BreakfastResolver`, ecc.) **solo** via service bootstrap e `ServiceContainer`.
  - Vietato introdurre nuovi `FindObjectOfType`/`FindObjectsOfType` nei nuovi task demo.
3. **UI HUD Foundation parity**
  - Per nuove UI Toolkit (VO overlay, Visitor Room, Bedroom PC panel) evitare binari preview/runtime separati, inline style non propagabili e sample divergenti.
  - Servono placeholder authoring in UXML coerenti con runtime.
4. **Layering / Panel Settings**
  - DEV REPORT 0085: stacking tra UIDocument sullo **stesso** `PanelSettings`.
  - **Aggiornamento (DEV 0105):** con `PanelSettings` **diversi** (es. VO/main HUD vs monitor Bedroom PC), definire **`m_SortingOrder`** esplicito sugli asset e verificare in Play che VO/toast critici non finiscano dietro il pannello world/stack dedicato.
5. **Hydration/Food redesign impact**
  - Decisione confermata: l’attuale modello è placeholder; migrazione obbligatoria a `H`=movimento e `F/colazione`=azioni.
  - Il repo attuale usa `PlayerHydrationSystem.GetActionModifier()` in `GameManager.HandleDayChanged`: serve refactor esplicito e test regressione.
6. **Cap azioni 5 (da 4)**
  - Decisione design confermata (demo + full): propagare a `ActionSystem`/TopBar/tooltip e QA regressione.
7. **Area Lab dentro VaultMap**
  - Nessun riferimento operativo a scena `SCN_Lab_Main`: il piano usa solo `SCN_VaultMap`.
8. **No-spoiler enforcement**
  - Traccia master (9 milestone) + red lines obbligatorie su VO/Ombra/finale.

### Azioni operative (prima implementazione)

- Aggiungere una mini-checklist “architettura” per ogni task runtime: `service registration`, `no scene scans`, `save/load impact`.
- Aggiungere una mini-checklist “UI parity” per ogni task UI Toolkit: `stesso Panel Settings stack`, `no inline style authoring-only`, `runtime == builder surface`.
- Inserire nel task gameplay una sottosezione “migrazione H/F” con fallback temporaneo e test di non regressione.
- Inserire nel task Lab uno step esplicito “**area Lab in SCN_VaultMap**”, senza scene aggiuntive.
- Agganciare i task narrativi alla **traccia master (9 milestone)** no-spoiler e alle relative red lines.

---

## Riferimenti visivi (workspace)

- VO typing / terminale: `assets/c__Users_UTENTE_..._image-b3a557e5-0c89-496f-b903-bc63f78315ee.png`
- Visitor Room UI mockup: `assets/c__Users_UTENTE_..._image-2c18b160-06f3-4f18-a571-370a6f0edc8e.png`
- Inventory UI reference: `assets/c__Users_UTENTE_..._image-79c4f635-e79a-4123-8c8d-c294d67c25c7.png`
- Seed Storage UI reference: `assets/c__Users_UTENTE_..._image-10f71c3e-a129-46d8-a3fb-d3e15475d149.png`
- Bedroom PC Desktop home (allegato 1): `assets/c__Users_UTENTE_AppData_Roaming_Cursor_User_workspaceStorage_7d505af11f74c176bd6aaca32b1b671b_images_image-0fedf8f9-9266-44bf-9ce6-f91e77ea182e.png`
- Bedroom PC Wiki 3 sezioni (allegato 2): `assets/c__Users_UTENTE_AppData_Roaming_Cursor_User_workspaceStorage_7d505af11f74c176bd6aaca32b1b671b_images_image-3d879ce5-4a16-46f3-b515-74b9b440c037.png`
- Bedroom PC Control Panel (allegato 3): `assets/c__Users_UTENTE_AppData_Roaming_Cursor_User_workspaceStorage_7d505af11f74c176bd6aaca32b1b671b_images_image-5d1cb3c5-55fe-4dfe-a9eb-027560446718.png`
- Bedroom PC Bot FAQ (allegato 4): `assets/c__Users_UTENTE_AppData_Roaming_Cursor_User_workspaceStorage_7d505af11f74c176bd6aaca32b1b671b_images_image-7057723a-ae36-41b6-b7b1-cdef45cb87db.png`
- Bedroom PC Black Market (allegato 5): `assets/c__Users_UTENTE_AppData_Roaming_Cursor_User_workspaceStorage_7d505af11f74c176bd6aaca32b1b671b_images_image-19823c62-ea95-40aa-aca0-1b917bb0e721.png`

---

## Verifica Play / Unity — checklist per step

**Regola:** a ogni task completato dal piano, la consegna include **cosa controllare in Editor** (Hierarchy/Inspector/Console/Project) e **in Play Mode** (comportamento, regressione, layering). Qui sotto la mappa per macro-area.

### Principio 0 — un solo prodotto


| Dove       | Cosa verificare                                                                                                                             |
| ---------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| **Editor** | Build Settings: scena di gioco principale = `SCN_VaultMap` (o quella effettiva del menu); nessuna scena “demo only” obbligatoria parallela. |
| **Play**   | Da menu, Nuova partita e Gioca demo caricano la **stessa** scena mappa; nessun branch che carica una scena Lab separata.                    |


### Traccia beat — checklist Play (9 milestone)

*Allineata alla sezione **Traccia master — Alpha no-spoiler (9 milestone)** sopra: una riga per ogni voce numerata 1–9.*

| Beat             | Play                                                                                           | Editor / Console                            |
| ---------------- | ---------------------------------------------------------------------------------------------- | ------------------------------------------- |
| 1 Wake / routine | Bedroom → Kitchen; VO visibile; movimento **non** bloccato dal VO.                             | Log dev assenti o attesi; nessun errore VO. |
| 2 Survival H/F   | Tutorial breve; azioni e movimento coerenti con nuovo split.                                   | —                                           |
| 3 Seed Storage   | UI + stato storage; copy senza spoiler; **missione «vai al Seed Storage»** completata solo dopo **iter** (apertura + VO anomaly + chiusura coerente); niente auto-completamento da bug checker. | Warning CRY/deperimento; asset missioni in `Resources` coerenti. |
| 4 Mercante post-Storage | Dopo Seed Storage ON, VO annuncia Visitor Center; chat Mercante Ombra; grant **4 frutti demo** a credito; contratto “domani acqua+cibo”; Choice `prepare_payment` visibile e funzionante. | `DemoStoryDirector` passa a Beat 4; `OnVOChoiceMade(prepare_payment, yes/no)` setta `willPreparePayment`; dipendenza `DA10-T009` + choice overlay. |
| 5 Pagamento / rottura | Dopo letto/EoD: se processi completati, inventario con `WAT-POT` + `FOOD-101` → **toast Foundation** che richiama la HUD; poi ritiro/scambio; Choice `meet_or_avoid_merchant`; pagamento riuscito o confronto per patto rotto, incluso ramo “evita” con soft-converge. | `merchantPaymentOutcome = paid \| broken \| avoided_then_confronted`; toast demo su condizione inventario (id stabile, anti-spam); reputazione/canale mercante aggiornati; nessun contenuto saltato. |
| 6 Uso frutti     | Choice `fruit_use`: Lab diretto oppure `eat_then_lab` che consuma 1 frutto e riconverge; hook Lab leggibile in entrambi i rami. | Inventory count coerente; restano frutti sufficienti; flag `fruitsUse` salvato solo demo. |
| 7 Lab / seme     | Sequenza completa nell’**area Lab su VaultMap**; crea seme demo-only della pianta **Il Piacere Dimenticato**. | Scena attiva = VaultMap; item/PlantData demo-only; no `SCN_Lab_Main`; no alias/grant finto come percorso primario. |
| 8 Dome / time-skip | Pianta **Il Piacere Dimenticato** nella Dome; tutorial POT minimale; crescita fino a stadio 2; EoD mostra esattamente **“A few days later”**; raccolta **Cetriolo d’Oro**. | `IsDemo` gate; save full pulito; fruit item **Cetriolo d’Oro** prodotto dalla pianta. |
| 9 Finale teaser  | Choice `golden_cucumber_outcome`: tenere/vendere/dare; esito morale/sociale registrato; glitch finale controllato + schermata demo. | `finalOutcome = keep \| sell \| give`; ramo A/B cambia copy/outcome, non moltiplica finale; niente spiegone causale. |


### Sezioni piano (macro-task)


| Sezione                      | Verifica Editor                                                                                                             | Verifica Play                                                                                                                                                         |
| ---------------------------- | --------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **1 Orchestrazione beat**    | Presenza asset dati (SO beat list o Timeline) coerenti con scelta; cartella/versioning Git se SO.                           | Beat si susseguono; branch condizionali; Timeline solo dove serve (timing camera).                                                                                    |
| **2 VO overlay**             | UIDocument / Panel Settings: stack condiviso quando possibile (DEV 0085); con **settings multipli** ordinare anche `m_SortingOrder` sugli asset (DEV 0105); `VO Prompt Choice` con UXML/USS parity. | Typing visibile; suono inizio/fine blocco; player cammina durante VO; VO leggibile **sopra** terminale/control plane Bedroom; choice Sì/No o 3 opzioni emette evento. |
| **3 Armadio**                | UI guardaroba + binding skin/tinte.                                                                                         | Interazione armadio → UI; rotella / frecce cambiano skin; chiusura senza soft-lock input.                                                                             |
| **4 H / F / azioni**         | `PlayerHydrationSystem`, mover, `GameManager`/ActionSystem: valori tier (100/50/25/0), cap azioni **5**, tabella colazione. | Variazione **velocità** con H bassa; azioni giornaliere da colazione; game over dopo **2** giorni a H=0% (tuning); reset all’alba; tooltip Top Bar Actions leggibile. |
| **5 Bedroom PC desktop hub** | Desktop con **4 icone**; `Pannello Controllo` via servizi (`SeedStorageSystem`, `FoodRoomSystem`, …); **nessun** avvio EoD dal PC. | Navigazione hub; ON/OFF circuiti; accesso archivio ricerche/FAQ/BM; **EoD solo dal letto**.                                                           |
| **6 Visitor Room / Desk**  | UXML/USS **Visitor Desk** per [`VISITOR_DESK_SPEC.md`](d:/Sporae_Build_Beta/Assets/_Project/Docs/VISITOR_DESK_SPEC.md); Mission+Trade; dati fake reputazione; toast su inventario pagamento. | Desk MISSION/TRADE; chat Mercante; `prepare_payment` / `meet_or_avoid_merchant`; toast Foundation; pagamento o patto rotto; §6.4 trim quando full pronto. |
| **6.1 Inventory UI**         | Foundation parity: niente stile solo authoring; `VIEW`/`USE` collegati.                                                     | Apertura inventario; consumo coerente H/F; tooltip item conformi regola progetto.                                                                                     |
| **6.2 Seed Storage UI**      | Doppia colonna, log, indicatori power/temp.                                                                                 | Transfer/retrieve; warning se circuito OFF; allineamento a pannello PC Bedroom.                                                                                       |
| **7 Ricerca notturna EoD**   | Placeholder wiki/testi se demo.                                                                                             | Flusso notte → risultati fake; unlock wiki se previsto.                                                                                                               |
| **8 Dawn Summary**           | Controller EoD/Dawn esistente.                                                                                              | Copy/eventi demo; nessuna eccezione Console ripetuta.                                                                                                                 |
| **9 Reputazione**            | UI numeri coerenti script; stato demo `merchantTrustDelta` / `merchantChannelClosed`.                                      | Patto rispettato, patto rotto ed evitamento danno feedback sociale leggibile senza sistema fazioni full.                                                              |
| **10 Lab Crea seme**         | Macchinari Lab **in scena VaultMap**; asset demo-only per **Il Piacere Dimenticato** e **Cetriolo d’Oro**.                 | Flusso end-to-end “Crea nuovo seme”; seed reale demo-only; salvataggio stato coerente.                                                                                |
| **11 Time-skip pianta**      | Flag `IsDemo` / sessione non corrompe save (backup slot test).                                                              | Dopo EoD testo esatto “A few days later”; pianta pronta con **Cetriolo d’Oro**; **nuova partita full** senza residui demo (test slot pulito).                         |
| **12 Morale + EoD**          | Choice finale `golden_cucumber_outcome` con 3 opzioni e copy condizionale ramo A/B.                                        | Tenere/vendere/dare registra `finalOutcome`; micro-sequenze al posto Snapshot; glitch finale riconvergente.                                                           |
| **13 Finale demo**           | Scene/UI schermata fine + form.                                                                                             | ~10s gameplay + musica; fade; schermata wishlist/Discord; form invio (o mock); copy crediti/Jeff.                                                                     |
| **14 Ascensore**             | `ElevatorSystem` + prefabs porte/display.                                                                                   | Call; attesa ~2s; porte; display piano; Su/Giù un piano; player nascosto in cabina.                                                                                   |


## Roadmap implementazione codificata (single source of truth)

Legenda stato: `DONE` | `DONE (placeholder)` | `IN_PROGRESS` | `TODO` | `N/A` | `BLOCKED`

- **`DONE (placeholder)`**: consegna shell/demo ok; **contenuti** (es. FAQ estese, AI) restano **backlog prodotto** senza tenere il task codice in `TODO`.
- **`N/A`**: scope **ritirato** (ID conservato per storico).

Nota: i codici `DA10-Txxx` restano identita` task; la tabella seguente rappresenta l'**ordine logico di esecuzione** per il flow demo.


| Ordine | Codice      | Task                                                                      | Stato         | Dipende da  | Evidenza / riferimento                                                                                                                                                                                                                      |
| ------ | ----------- | ------------------------------------------------------------------------- | ------------- | ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 01     | `DA10-T001` | Ingresso demo + `DemoSessionState` + `DemoStoryDirector`                  | `DONE`        | —           | Check codice runtime + Principio 0                                                                                                                                                                                                          |
| 02     | `DA10-T002` | VO overlay (`VoOverlayController`) typing/registri/layering               | `DONE`        | `DA10-T001` | DEV `0086` + polish `0089` (enter/exit ~0.5s, ~33 c/s, ~10s blocco, shake idle, hint continuazione implicito)                                                                                                                               |
| 03     | `DA10-T003` | Toast nuova missione + highlight parole missione nel VO                   | `DONE`        | `DA10-T002` | DEV `0087` + toast tipo `Mission` cyan `#00FFC6` / `ShowMission` (`0089`)                                                                                                                                                                   |
| 04     | `DA10-T004` | Interazioni E/prompt + Mission Recap UITK + barra azioni + fix fame       | `DONE`        | `DA10-T001` | DEV REPORT `0088`                                                                                                                                                                                                                           |
| 05     | `DA10-T005` | Armadio (UI/input) + hook missione iniziale                               | `DONE`        | `DA10-T001` | Verifica codice runtime: trigger missione su chiusura armadio                                                                                                                                                                               |
| 06     | `DA10-T006` | Gameplay `H/F/azioni` completo (tier H + breakfast + GO streak + tooltip) | `DONE`        | `DA10-T001` | DEV `0089` + **playtest in-game ok**: demo Giorno 1 `1/5` + boost pasto; da Giorno 2 breakfast/full come full game; toast `PLY-HYD-GAIN`; beat2 cucina + `DemoBreakfastMission`; tier H, tooltip Actions, GO disidratazione/fame verificati |
| 07     | `DA10-T007` | Bedroom PC app `Control Panel` (power centralizzato)                      | `DONE`        | `DA10-T017` | `BedroomPcDisplayController` + `BedroomPcDisplay.uxml` (circuiti Seed/Food/CRYO/compost); DEV **0105** beat 3/layering; **residuo:** warning conferma OFF                                                                                                                                                    |
| 08     | `DA10-T008` | Seed Storage UI revamp + integrazione power warning                       | `DONE`        | `DA10-T007` | `SeedStoragePanel.uxml` + `SeedStoragePanelController`; evento `SeedStoragePowerSetFromControlPanel` in `DemoStoryDirector` / PC; **residuo:** modale warning come da §5.2/§6.2                                                                                                                                 |
| 09     | `DA10-T009` | **Visitor Desk** (MISSION/TRADE per spec) + Mercante Ombra choice flow (`prepare_payment`, `meet_or_avoid_merchant`) + **toast Foundation** su inventario con item pagamento | `TODO`        | `DA10-T001` | [`VISITOR_DESK_SPEC.md`](d:/Sporae_Build_Beta/Assets/_Project/Docs/VISITOR_DESK_SPEC.md) + §6–§6.4; §2.1; toast HUD; trim demo post full                                                                                                     |
| 10     | `DA10-T010` | Inventory UI revamp (Foundation + `VIEW/USE`)                             | `TODO`        | `DA10-T001` | Sezione 6.1                                                                                                                                                                                                                                 |
| 11     | `DA10-T011` | EoD branch demo no-spoiler (`skip Snapshot`, micro-sequenze, “A few days later”) | `TODO`        | `DA10-T001` | Sezioni 11-12; time-skip solo demo dopo stadio 2 de **Il Piacere Dimenticato**                                                                                                                                                              |
| 12     | `DA10-T012` | Dawn estensioni demo e state check pagamento                              | `TODO`        | `DA10-T011` | Sezioni 6 e 8; mattina dopo: risorse disponibili/assenti, prompt chiamata o confronto mercante                                                                                                                                              |
| 13     | `DA10-T013` | Content pack no-spoiler choice-driven (VO bicolore + rami A/B + finale)   | `TODO`        | `DA10-T003` | Traccia master 9 milestone + Red Lines; copy `prepare_payment`, `meet_or_avoid_merchant`, `fruit_use`, `golden_cucumber_outcome`                                                                                                           |
| 14     | `DA10-T014` | Lab `Crea nuovo seme` + item demo-only **Il Piacere Dimenticato / Cetriolo d’Oro** | `TODO`        | `DA10-T001` | Sezione 10; seed/PlantData/Fruit reali ma gated demo, nessuna scena Lab separata                                                                                                                                                            |
| 15     | `DA10-T015` | Ascensore (call/doors/display/cabina) integrato in `SCN_VaultMap`         | `DONE`        | `DA10-T001` | `ElevatorSystem` + `UI_ElevatorPanel` / riferimenti in `SCN_VaultMap.unity` (integrazione scena); regression gameplay E2E = N/D in questo passaggio                                                                                                                                                            |
| 16     | `DA10-T016` | Finale demo (choice Cetriolo d’Oro + glitch + fade + schermata + feedback form) | `TODO`        | `DA10-T011` | Sezioni 12-13; `finalOutcome = keep \| sell \| give` con copy condizionale ramo A/B                                                                                                                                                          |
| 17     | `DA10-T017` | Bedroom PC Desktop home (4 icone + shell)                                 | `DONE`        | `DA10-T001` | `BedroomPcDisplay.uxml` — 4 tile; sonno **non** nel PC                                                                                                                                                                                     |
| 18     | `DA10-T018` | Bedroom PC app `Wiki` (`Diary Report` / `Wiki Plants` / `Research`)       | `DONE`        | `DA10-T017` | Placeholder “Centro di ricerca” + notti; **residuo:** Wiki 3 tab §5.1                                                                                                                                                                        |
| 19     | `DA10-T019` | Bedroom PC app `Bot FAQ` contestuale                                      | `DONE (placeholder)` | `DA10-T017` | UI/codice demo ok (`HandleFaqBot`); **contenuti** (search, 8–12 FAQ, **AI**): backlog / trattare come lavoro contenuti, non gate shell PC                                                                                                                                                                              |
| 20     | `DA10-T020` | Bedroom PC app `Black Market` entry + flusso base                         | `DONE`        | `DA10-T017` | `BedroomPcTerminal` + `UIBlackMarket`                                                                                                                                                                                                         |
| 21     | `DA10-T021` | ~~CTA sonno su hub~~ *(ritirato)*                                          | `N/A`         | —           | **Decisione 2026-05:** EoD **solo letto**; ID conservato                                                                                                                                                                                      |
| 22     | `DA10-T022` | Save safety (`isDemo`, `demo_completed`, no contamination full run)       | `DONE`        | `DA10-T001` | `SaveManager` + `gameState.isDemoSession` / inferenza demo; **residuo:** test anti-contaminazione su slot dedicati (gate DoR)                                                                                                               |


### Decisioni consolidate da DEV REPORT 0086-0089 e 0105

- **DEV REPORT 0086 (`DA10-T002`)**: VO overlay UITK completato con typing, registri colore, integrazione installer/runtime e layering coerente.
- **DEV REPORT 0087 (`DA10-T003`)**: evento `OnMissionAdded`, toast `MIS-NEW`, highlight parole missione nel VO configurabile da narrativa demo.
- **DEV REPORT 0088 (`DA10-T004`)**: stabilizzazione interazioni tasto E/prompt; Mission Recap UITK ripristinato; barra azioni con scala visiva fissa 5 slot; fix fame/cap azioni in runtime.
- **DEV REPORT 0089 (`DA10-T002` / `T003` / `DA10-T006`)**: polish VO (animazioni, tempi, stabilità layout); beat 2 cucina data-driven (`DemoAlphaNarrativeConfig` + `DemoStoryDirector`); missione demo colazione con `DemoBreakfastMission`; toast idratazione `PLY-HYD-GAIN`; toast missione dedicato (`Mission`, `#00FFC6`, `ShowMission`); split **demo Giorno 1** (`_demoTutorialDayActive`) vs **Giorno 2+** allineato al full game — **DA10-T006** verificato in playtest (2026-04-20).
- **DEV REPORT 0105 (beat 3 / missioni / layering 2026-05-03):** missioni demo PC in `Assets/Resources/Missions/`; `MissionChecker` senza auto-win su obiettivi vuoti; guard `DemoStoryDirector` su Seed Storage + proseguimento CRY/PC; VO sopra stack Bedroom tramite `PanelSettings` / `UIDocument` sorting — allineato a **Beat 3**; **`DA10-T007`** considerato chiuso su main per iterazione demo (vedi §5.6).

### Divergenze da riallineare nel piano (aperte)

- *(nessuna su `DA10-T006`; chiusura confermata in-game.)*
- **`DA10-T019` (contenuti):** oltre il placeholder `HandleFaqBot`, restano search/categorie/8–12 voci/AI — **backlog prodotto** (stato task codice: `DONE (placeholder)`, vedi legenda roadmap).
- **§5.2 warning Seed Storage OFF:** toggle in `ToggleSeedStoragePower()` senza modale di conferma esplicita; allineare a tabella circuiti §5 se richiesto da QA.
- **Backend `MachinePowerState`:** piano storico vs composizione attuale — documentato in **§5.4.1**; refactoring opzionale se serve una sola fonte di verità.

### Gate compliance (veloce)


| Gate             | Come verificare                                                                                                |
| ---------------- | -------------------------------------------------------------------------------------------------------------- |
| ServiceContainer | Breakpoint o log: servizi demo registrati in `GamePlayInstaller` (o installer dedicato).                       |
| No scene scan    | Ricerca progetto sui nuovi file demo: assenza `FindObjectOfType` / `FindObjectsOfType` dove vietato dal piano. |
| UI layering      | Play: tooltip, toast, modale VO — ordine z/sorting; preferire **stesso** `PanelSettings` dove possibile; con **settings multipli**, allineare `m_SortingOrder` sugli asset (ref. DEV 0105: VO/main HUD sopra Bedroom PC). |
| Save safety      | Due slot: run demo completata vs nuova partita; nessun flag demo che contamina full.                           |


---

## Definition of Ready (Go / No-Go) mappata al flow

Spuntare per blocchi in ordine di avanzamento reale.

- **Blocco A (fondamenta)**: `DA10-T001..DA10-T004` chiusi e stabili in regressione.
- **Blocco B (sopravvivenza early flow)**: `DA10-T005`, `DA10-T006` chiusi su comportamento + UX (playtest `T006` ok 2026-04-20).
- **Blocco C (anomalia + sistemi)**: **`DA10-T007`, `DA10-T008` chiusi** (percorso demo su main); **`DA10-T009`, `DA10-T010` ancora aperti** — blocco non completo fino a Visitor choice flow + Inventory revamp.
- **Blocco D (narrativa notte/giorno)**: `DA10-T011`, `DA10-T012`, `DA10-T013` chiusi solo quando i prompt `prepare_payment`, `meet_or_avoid_merchant`, `fruit_use` e i relativi follow-up sono integrati.
- **Blocco E (chiusura run)**: **`DA10-T015` chiuso** su main (ascensore in `SCN_VaultMap`); **`DA10-T014`, `DA10-T016` ancora aperti** — blocco non E2E finché Il Piacere Dimenticato / Cetriolo d’Oro / scelta finale non sono verificati.
- **Blocco F (hub Bedroom PC)**: **`DA10-T017`–`T020` chiusi** (scope demo); **`DA10-T019`**: codice **`DONE (placeholder)`**, contenuti/AI in backlog; **`DA10-T021`**: `N/A` (sonno sul letto).
- **Blocco G (safety)**: codice flag demo in save **`DONE`** su main; **test anti-contaminazione** da calendarizzare se non ancora eseguiti (gate DoR).

### Esito pre-flight lock (decisioni gia` chiuse)

- Baseline gameplay split `H/F` bloccata come direzione (`DA10-T006` implementato e validato in playtest).
- Scope Visitor v1 `visitor_mid` aggiornato al flow Mercante Ombra choice-driven (`DA10-T009`).
- Save mode `shared_slot_with_flags` bloccato (`DA10-T022`).
- Feedback module scope `in_game_form` bloccato (`DA10-T016`).
- UI layering rule `same_panel_stack` bloccata (gate compliance).
- Architecture enforcement `strict_service_container` bloccata (gate compliance).
- QA gate minimo `full_gate` bloccato (criterio di uscita release Alpha).

### Stato DoR

**READY TO EXECUTE** — usare questa roadmap ordinata come unica fonte stato e di priorita`.

**Prossima priorità implementazione (post Beat 3 chiuso):** **`DA10-T009`** Visitor Room + `VO Prompt Choice` + Mercante Ombra; poi **`DA10-T011`–`T013`** per notte/mattina/copy rami; poi **`DA10-T014`** per Lab + demo-only **Il Piacere Dimenticato / Cetriolo d’Oro**; poi **`DA10-T016`** per choice finale e glitch. **`DA10-T019`** resta backlog contenuti/AI, non gate del flow demo. Opzionale: hardening **`MachinePowerState`** / modale OFF (§5.4.1, §5.2).
