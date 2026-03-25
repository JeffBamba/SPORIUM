# Analisi tecnica completa Sporium e cosa può fare il giocatore
## Documento unico — Build vs GDD, stato infrastruttura e gameplay

**Data documento:** 2026-03-19  
**Riferimento precedente:** 2025-03-18 (ANALISI_TECNICA_COMPLETA_SPORIUM_2025-03-18.md + COSA_IL_GIOCATORE_PUO_FARE_BUILD_vs_GDD_2025-03-18.txt)  
**Roadmap:** `roadmap_dome_lab_100_069d5bdb.plan.md`  
**DEV Report di riferimento:** 0071, 0072, 0073

---

## Modifiche rispetto a ieri (18 → 19 marzo 2026)

- **DomeStatusHUD unificato e tooltip colorato:** in `DomeStatusHUDController.cs` esistono `BuildPotTooltipLines` e un container tooltip `dome-hud-tooltip-lines`; in `DomeStatusHUD.uss` la classe `.dome-hud-tooltip-line` imposta `font-size: 13px`. Inoltre `AlwaysVisiblePotHUD` non risulta più referenziato in `Assets/_Project/Scripts` (0 occorrenze).
- **PotDebugConsole (debug seed “senza inventario”):** esiste `DebugPlantSeed` in `PotDebugConsole.cs`.
- **Servizi/registry presenti nel codice:** dentro gli script risultano riferimenti a `DomePotRegistry`, `CryoMachineController` e `PhSystem` (conteggi verificati oggi).
- **Stato generale anti-critici:** non posso confermare “assenza di FindObjectOfType” (esiste ancora), ma oggi ho misurato le occorrenze dentro `Assets/_Project/Scripts` per aggiornare l’analisi senza supposizioni.

---

# PARTE I — ANALISI TECNICA COMPLETA

## Executive summary

**Valutazione complessiva: 7.x/10 (aggiornata con evidenze verificabili oggi)**

Oggi ho aggiornato la parte “tecnica” con controlli diretti su `Assets/_Project/Scripts` (niente supposizioni):
- **264** file `.cs` sotto `Assets/_Project/Scripts`
- `ServiceContainer.Instance?.Get` occorre **144** volte
- `FindObjectOfType` occorre **100** volte in **64** file
- `FindObjectsOfType` occorre **39** volte in **21** file
- `AlwaysVisiblePotHUD` non risulta più referenziato in `Assets/_Project/Scripts` (**0** occorrenze)

Per i “god class”, oggi ho verificato le dimensioni:
- `PotActions.cs` = **1932** righe
- `SPOR-BLK-01-03A-DayCycleController.cs` = **2722** righe
- `PlantCardV3TerminalController.cs` = **7105** righe
- `DomeStatusHUDController.cs` = **786** righe

---

## Metriche e pattern

| Metrica / Pattern | Valore (oggi, verificato) | Note |
|---|---|---|
| File `.cs` in `Assets/_Project/Scripts` | **264** | dominio su cui misuro anti-pattern |
| `ServiceContainer.Instance?.Get` | **144 occorrenze** | uso service locator |
| `FindObjectOfType` | **100 occorrenze** in **64 file** | anti-pattern ancora presente |
| `FindObjectsOfType` | **39 occorrenze** in **21 file** | anti-pattern ancora presente |
| `AlwaysVisiblePotHUD` | **0 occorrenze** | non referenziato negli scripts |

---

## Architettura aggiornata (Dome)

- `DomePotRegistry`: presente (18 occorrenze in 7 file).
- `CryoMachineController`: presente (28 occorrenze in 10 file).
- `PhSystem`: presente (236 occorrenze in 22 file).
- `DomeStatusHUDController`: tooltip colorato via `BuildPotTooltipLines` e righe tooltip con `font-size: 13px` in `DomeStatusHUD.uss`.

---

## Code smells (stato 2026-03-19)

| Elemento              | Stato        | Raccomandazione                          |
|-----------------------|-------------|------------------------------------------|
| PotActions            | 1932 righe | Dividere validator/executor/state        |
| DayCycleController    | 2722 righe | Estrarre processor separati             |
| PlantCardV3Terminal   | 7105 righe | Moduli per sezioni                      |
| FindObjectOfType      | presente | ridurre progressivamente; preferire cache/registry |

---

## Valutazione per categoria

| Categoria      | Score (stima) | Motivo (oggi) |
|----------------|-----------------|----------------|
| Architettura   | 8/10            | servizi/registry Dome presenti nel codice |
| Code quality   | 7/10            | anti-pattern ancora presente (FindObjectOfType/FindObjectsOfType) |
| Performance    | 7/10            | anti-pattern ancora presente (100/39 occorrenze) |
| Manutenibilità | 6.5/10          | file molto grandi (PotActions/DayCycle/PlantCardV3Terminal) |
| Testabilità    | 6/10            | nessuna valutazione automatica fatta oggi (non assumo miglioramenti) |
| Scalabilità    | 7.5/10          | rischio anti-pattern, ma dominio modulare |
| Robustezza     | 7/10            | evidenze statiche positive su rimozione HUD legacy (AlwaysVisiblePotHUD=0) |
| Documentazione | 7/10            | presenza DEV report e documentazione (non ricalcolata oggi) |

**Score complessivo: 7.x/10**

---

## Raccomandazioni prioritarie

- **Alta:** Ridurre FindObjectOfType; cache GetComponent dove ripetuti; scomporre god class.
- **Media:** Interfacce per servizi (test/DI); rimozione elementi UI obsoleti residui.
- **Bassa:** ScriptableObject per valori configurabili; XML su API pubbliche.

---

# PARTE II — COSA PUÒ FARE IL GIOCATORE (Build attuale)

*Descrizione in storytelling, allineata al codice e alla build del 2026-03-19.*

---

## Inizio partita

Il giocatore vede il menu principale. Può avviare una nuova partita, aprire la schermata di caricamento (con riepilogo per slot), salvare da lì se in gioco, aprire opzioni o uscire. In assenza di semi in inventario può usare la **Pot Debug Console** (tasto P, solo development): seleziona un vaso vuoto e con “Debug: Impianta Seme” pianta uno dei tre semi (Ferric Fern, Arctic Hask, Glasscap Fungus) con metadati completi, senza consumare inventario né azioni.

---

## La giornata in Vault

In scena il giocatore si muove nella Vault con un budget di azioni e la risorsa CRY. In barra in alto vede azioni rimanenti, pH globale della cupola, condensazione, indice di mutazione, CRY e altri indicatori; il **tooltip del pH** include ora una sezione **Cryo Pot** con drift e cap delle piante negli slot passivi. In basso può passare tra le stanze (es. cupola).

Può interagire con: letto (fine giornata), ascensore (cambio piano, costo CRY), **terminale vasi** (gestione piante), **CryoMachine** (pannello con i 3 slot passivi), macchine lab, distributore cibo/acqua, terminale mercato nero, deposito semi. Cliccando su un vaso lo seleziona visivamente; le azioni sul vaso (piantare, raccogliere, irrigazione, LED, potatura, ecc.) si eseguono dal terminale.

Un **HUD unico Dome Status** (angolo basso-destra) mostra sempre lo stato di vasi attivi e slot Cryo: si espande/contrae orizzontalmente, con tab **POTS** e **CRYO**. Per ogni pot/slot compaiono mini-preview, nome pianta e condizione; al passaggio del mouse un tooltip mostra “Requisiti e Avanzamento” con valori colorati (verde = ok, giallo = attenzione, rosso = problema) per capire rapidamente cosa non va.

---

## Terminale vasi (Plant Card V3)

Il giocatore apre il terminale e digita comandi testuali. Oltre a piantare, raccogliere, potare, fertilizzare, spray, **WATERING [POT-ID]**, LED, sradicare, può:

- **PASSIVE** — Overview degli slot passivi (Cryo): quali sono occupati, protocollo su come funzionano i poteri attivi vs passivi e i comandi disponibili.
- **CRYO SEND POT-XXX** — Trasferisce la pianta di livello 5 dal vaso indicato in uno slot Cryo (con conferma e avvertenza sugli effetti).
- **CRYO EXTRACT** — Sceglie da quale slot Cryo estrarre la pianta verso l’inventario (con nota su deperimento organico).
- **CRYO RESTORE** — Sceglie da quale slot Cryo riportare la pianta in un vaso attivo (con avvertenza sulla perdita degli effetti passivi).

Dopo trasferimenti o estrazioni, notifiche toast e aggiornamento visivo nel terminale confermano l’esito. L’irrigazione a goccia (ON/OFF per vaso) e lo stato LED si vedono nel terminale e nel Dome Status HUD.

---

## Crescita e stato delle piante

Le piante attraversano stadi (seme, germoglio, crescita, fioritura, raccolto pronto, riposo). Lo stato dipende da idratazione, luce (LED), fertilizzante e pH. Il giocatore vede condizioni (rigogliosa, sana, stressata, appassita), rischio muffa e avvisi; i LED prolungati causano stress da burn. Il pH della cupola e il drift per pianta (incluse le **tre piante in Cryo**, i cui effetti passivi contribuiscono al pH con drift e cap) influenzano crescita e resa. Le piante di livello 5 possono essere spostate negli slot Cryo: lì i **poteri passivi** sono attivi (e quelli attivi sospesi), senza manutenzione quotidiana; se riportate in un vaso attivo riacquistano i poteri attivi.

Frutti si accumulano fino a un massimo (es. 3) e se non raccolti in tempo marciscono. Raccogliendo si va in riposo; con fertilizzante e cicli completati la pianta sale di livello (1–5), con effetti su quantità e qualità.

---

## Fine giornata

Interagendo con il letto parte la sequenza “fine giornata”: conferma, riepilogo (azioni, CRY, raccolti, irrigazioni, lab, pH, condizioni), frammento diario typewriter, ricerca notturna (se azione disponibile), previsione giorno successivo, transizione notte → alba. Poi nuova giornata con azioni rinnovate.

---

## Laboratorio

Estrattore (frutto → spore, minigame), incubatore (spore → semi), catalizzatore (maturazione spore), pipetta/fusione (due spore → pre-seme), microscopio (precisione, consuma spore e azioni). Inventario condiviso; dal mercato nero si può sbloccare il modulo stem-cell. Accesso tramite postazioni in scena.

---

## Resto della Vault

Pannello inventario e stato personaggio, macchina cibo/acqua, deposito semi, notifiche (pannello e toast Foundation). Missioni con obiettivi tracciati e salvati. Condensazione (WAT-RAW) con effetti su rischio muffa. Save/load multi-slot con riepilogo (giorno, piante in cupola, CRY, timestamp).

---

## Cosa c’è nel GDD ma non (o non pienamente) nella build

**Ora in build (aggiornato 2026-03-19):**
- **Slot passivi per piante livello 5:** spostamento vaso ↔ Cryo, effetti passivi reali su pH (drift, cap), comandi PASSIVE / CRYO SEND / CRYO EXTRACT / CRYO RESTORE, pannello CryoMachine e HUD Dome Status con tab Cryo.

**Ancora previsti nel GDD ma non implementati o parziali:**
- Sistema mutazioni (score e trigger)
- Compatibilità fertilizzanti per famiglia (morte se sbagliato)
- Creazione compost in lab da prodotto pianta
- Sistema ibridi completo
- Codifica spore (STABLE/STANDARD/UNSTABLE) e prodotti botanici (foglie, resine, ecc.)
- Diario SPORAE come schermata narrativa dedicata
- Tipi toast “narrativo” e “tutorial”
- Idratazione del giocatore come sistema completo
- Fazioni e reputazione
- Addressables e telemetria “sotto il cofano”

---

**Fine documento.**  
Unico file: analisi tecnica + cosa può fare il giocatore, aggiornato al 2026-03-19 rispetto alla situazione del 2025-03-18.  
Riferimenti: ANALISI_TECNICA_COMPLETA_SPORIUM_2025-03-18.md, COSA_IL_GIOCATORE_PUO_FARE_BUILD_vs_GDD_2025-03-18.txt, DEV_REPORT_0071–0073, roadmap_dome_lab_100.
