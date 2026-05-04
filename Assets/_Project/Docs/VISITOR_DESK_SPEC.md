# Visitor Desk — specifica concettuale (UI + logica)

**Riferimenti progetto**

- Piano demo Alpha (beat Visitor / Mercante Ombra, flag, toast): [`.cursor/plans/demo_alpha_1_0_gap_map.plan.md`](../../../.cursor/plans/demo_alpha_1_0_gap_map.plan.md) — **§6**, **§6.3** (copy dialoghi).
- Parità UI Builder / runtime: `.cursor/rules/ui-hud-foundation-ui-builder-parity.mdc` — i colori qui sotto vanno mappati a **classi USS** (es. `.visitor-desk-*`), evitando stile solo-inline sul campione authoring.

**Strategia di consegna**

1. Implementare il **Visitor Desk completo** (MISSION + TRADE, conversazione, trade math, CRT, header) come prodotto nel binario unico.
2. In una fase successiva, definire il **trim demo** (cosa disattivare o scriptare in versione ridotta) — vedi sezione *Demo vs full Visitor Desk* in fondo.

---

## Panoramica

Il **Visitor Desk** è un pannello interattivo (`UI Toolkit`) dove il giocatore incontra **visitatori di diverse fazioni**. Il pannello ha **due modalità**:

| Modalità | Scopo |
|----------|--------|
| **MISSION** | Ritratto visitatore, dati biometrici/fazione, dettaglio missione, bottoni Accept / Decline / Postpone, conversazione strutturata. |
| **TRADE** | Due colonne (offerte fazione vs inventario player), bilancio, previsione accordo, risposta visitatore, Propose / Cancel. |

**Visitor Room** (stanza / `ROOM_Visitor`) è il **contesto diegetico** in mondo; **Visitor Desk** è il **nome prodotto del terminale UI** aperto in quella stanza (o da interazione equivalente).

---

## Sistema colori

### Fazioni

| Fazione | Colore | Hex |
|---------|--------|-----|
| Mercanti dell’Ombra | Oro / giallo | `#FFCC66` |

### Sistema (reputazione, UI, valuta)

| Uso | Hex |
|-----|-----|
| Reputazione alta (50+) | `#7FFF7A` |
| Reputazione media (0–49) | `#E6C96F` |
| Reputazione bassa (negativa) | `#D35F5F` |
| Valuta CRY | `#FFCC66` |
| Interfaccia UI (accenti / bordi desk) | `#00E0C2` |

### Rarità oggetti *(placeholder prodotto — introduzione successiva)*

| Rarità | Hex |
|--------|-----|
| Legendary | `#FF4F4F` |
| Rare | `#00E0C2` |
| Common | `#FFCC66` |

**USS:** definire variabili o classi dedicate (es. `.visitor-desk-faction-shadow`, `.visitor-desk-rep-high`) nel foglio di stile del Desk; non duplicare hex come `style=""` sui nodi runtime/campione.

---

## Modalità 1 — MISSION

### Pannello sinistro — ritratto visitatore

- Ritratto grande con effetto **ologramma**.
- **Linea di scansione verticale** continua dall’alto al basso.
- **Nome visitatore** in oro (`#FFCC66`).
- **Nome fazione** nel colore della fazione.
- **Pannello biometrico:**
  - Status: es. `STABLE` (verde).
  - **Trust level** derivato dalla reputazione corrente (mapping a fascia colore sistema).

### Pannello destro — dettagli missione

- **Box missione principale** con sfondo **dati esadecimali scrollanti**.
- **Titolo missione** in oro (`#FFCC66`) con glow.
- **Icona / emoji** oggetto richiesto in box dorato.
- **Descrizione** con **cursore lampeggiante** a fine testo.

**Quattro box informativi**

| Box | Bordo | Contenuto |
|-----|-------|-----------|
| REQUESTED ITEM | Ciano (`#00E0C2`) | Nome oggetto |
| DEADLINE | Oro (`#FFCC66`) | Giorni rimanenti |
| REPUTATION REWARD | Verde (`#7FFF7A`) | Punti reputazione |
| CRY REWARD | Oro (`#FFCC66`) | Valuta in palio |

**Bottoni azione**

- **ACCEPT** — verde `#7FFF7A`, pulsazione luminosa.
- **DECLINE** — rosso `#FF4F4F`, pulsazione.
- **POSTPONE** — oro `#FFCC66`, pulsazione.

### Pannello conversazione (sotto la missione)

**Flusso (modello UI)**

1. Il visitatore pronuncia una frase (**lettera per lettera**, effetto macchina da scrivere).
2. A fine battuta compaiono **2 scelte** per il giocatore.
3. Il giocatore seleziona; la risposta appare **lettera per lettera**.
4. Si passa allo scambio successivo.

**Elementi visivi**

- Cursore lampeggiante `|` durante la scrittura.
- Storia conversazione **scorrevole** in alto: scambi completati in semi-trasparenza.
- Ritratto si **illumina** quando si sceglie opzione “friendly” (parametrizzabile).
- **Icone speaker** distinte visitatore vs giocatore.

> **Nota vs copy demo (§6.3 gap map):** il modello “N scambi × 2 opzioni” è **data-driven**. I dialoghi Mercante Ombra nel piano possono richiedere **più nodi** di 6; in implementazione usare struttura `{ visitorLine, playerOptions[2], next }` oppure ridurre il copy in fase **trim demo**.

---

## Modalità 2 — TRADE

### Layout — due colonne centrali

**Colonna sinistra — FACTION OFFERS**

- Bordo ciano `#00E0C2`.
- Lista oggetti offerti dalla fazione.
- Per oggetto: icona/emoji, nome, quantità, valore CRY, badge rarità.
- Click → bordo selezione **verde** `#7FFF7A`.

**Colonna destra — YOUR INVENTORY**

- Bordo oro `#FFCC66`.
- Stessa struttura righe; click seleziona/deseleziona.

### Sidebar destra — controlli trade

1. **Ritratto compatto** (es. 16×16 con scan), nome e fazione inline.
2. **Pannello bilancio trade** — valore totale offerta player vs offerta fazione.
   - **TRADE BALANCE** colorato:
     - Player dà **più** di quanto riceve → rosso `#FF4F4F`
     - Riceve **più** di quanto dà → verde `#7FFF7A`
     - **Zero** (pareggio) → oro `#FFCC66`
3. **FORECAST** — visibile solo con almeno un oggetto selezionato **per parte**; probabilità / qualità accordo:
   - Equilibrio ±15% → verde: `PERFECT BALANCE` / `GOOD DEAL`
   - Squilibrio 15–40% → oro: `OVERPAYING` / `UNDERPAYING`
   - Squilibrio >40% → rosso: `MASSIVELY OVERPAYING` / `INSULTING OFFER`
4. **Pannello risposta visitatore** — bordo per stato:
   - **Idle:** blu `#5DB6E3` — messaggio `SELECT ITEMS AND PROPOSE TRADE`
   - **Accepted:** verde `#7FFF7A`
   - **Refused:** rosso `#FF4F4F`
   - **Doubtful:** oro `#FFCC66`

**Bottoni**

- **PROPOSE TRADE** — verde, icona freccia.
- **CANCEL** — rosso, icona X, reset selezione.

---

## Sistema negoziazione — regole (dopo PROPOSE TRADE)

Definizione: **differenza percentuale** = valore assoluto della differenza tra le due offerte / valore **più alto** tra le due.

| Scenario | Condizione | Esito visitatore | Reputazione | UI |
|----------|------------|-------------------|-------------|-----|
| **1 — Accettazione** | ±15% | Frasi neutre/positive (es. equilibrio raggiunto) | +1 | Chiusura pannello ~5 s |
| **2 — Accettazione entusiasta** | Player offre >40% in più | Gratitudine/sorpresa | +2 | Chiusura ~5.5 s |
| **3 — Rifiuto** | Player offre >40% in meno | Frasi dure | −1 | Reset selezione, riprova |
| **4 — Dubbioso** | 15–40% squilibrio | Esita (chiedi troppo / offri troppo) | 0 | Trade **non** completato; hint `ADJUST YOUR OFFER AND TRY AGAIN` |

---

## Effetti atmosferici CRT

**Sempre attivi**

- Vignettatura radiale (bordi scuri, centro più luminoso).
- Rumore/grana semi-trasparente.
- Scanline orizzontali (es. ogni 3 px verticali).
- Particelle: ~12 puntini ciano che salgono/scendono lentamente.

**Frame metallico**

- Angoli superiori: doppio bordo ciano `#00E0C2` + glow interno.
- Angoli inferiori: doppio bordo oro `#FFCC66` + glow interno.
- Aspetto “danneggiato” futuristico.

**Glitch**

- Random ogni 5–8 s: flash ciano semi-trasparente ~150 ms su tutto il pannello.
- Anche su **proposta trade**.

---

## Barra superiore (header)

- **LED:** due LED lampeggianti alternati (ciano / oro), velocità diverse (“sistema attivo”).
- **REP:** colore per fascia reputazione (alto verde, medio oro, basso rosso).
- **CRY:** oro con glow.
- **DAY:** ciano con puntino pulsante.
- **Chiusura:** X alto-destra, bordo oro; hover sfondo oro semi-trasparente.

---

## Dati esempio (placeholder UI — non canonici demo Alpha)

### Missione di esempio (schermata mock)

| Campo | Valore esempio |
|-------|----------------|
| Titolo | `THE GOLDEN CUCUMBER` |
| Descrizione | *We need a Bioluminescent Iris within 2 days.* |
| Item | Bioluminescent Iris |
| Deadline | 2 giorni |
| REP reward | +10 |
| CRY reward | 150¢ |

> **Avviso:** questo blocco serve **solo** come **mock layout / copy inglese**. **Non** è la missione canonica della **Demo Alpha** nel gap map, che usa **Il Piacere Dimenticato**, **Cetriolo d’Oro**, patto **acqua/cibo** (`WAT-POT` + `FOOD-101`) e dialoghi **§6.3** in italiano.

### Trade di esempio (liste mock)

**Faction offers (esempio)**

| Item | Rarità | Qty | Valore |
|------|--------|-----|--------|
| Nutrient Pack | common | 5 | 30¢ |
| Rare Seeds | rare | 2 | 120¢ |
| pH Stabilizer | rare | 3 | 80¢ |
| Gene Fragment | legendary | 1 | 250¢ |

**Player inventory (esempio)**

| Item | Rarità | Qty | Valore |
|------|--------|-----|--------|
| Spores STABLE | rare | 8 | 50¢ |
| Plant Extract | common | 12 | 25¢ |
| Mutated Fruit | rare | 3 | 100¢ |

---

## Allineamento implementazione

| Tema | Azione |
|------|--------|
| **ServiceContainer** | Logica trade/reputazione visitatore in servizi registrati; niente scene scan ad-hoc (coerente con `architecture-runtime-services`). |
| **`DA10-T009`** | Shell Visitor Desk + script Mercante demo + motore trade possono restare un unico task o essere spezzati in sotto-task in roadmap. |
| **Dialoghi §6.3** | Binding a nodi conversazione Desk o a layer VO esistente: decisione in design tecnico unico. |

---

## Demo vs full Visitor Desk

### Principio prodotto (vertical slice demo)

Per la **demo Alpha**, il Visitor Desk **in questa forma non è “un sistema” da esporre per intero**: è la **cabina narrativa del Mercante Ombra** — superficie per **contratto → scelta → conseguenza → finale**. Obiettivo: **trailer giocabile** del potenziale del gioco completo, non tutorial di tutti i sottosistemi.

**Lab e Dome in demo** restano **scriptati / guidati**: non insegnano tutto Sporium; devono far pensare *«qui sotto c’è un sistema enorme»* senza aprire sandbox estesa.

| Area | Demo (vertical slice) | Full game (Desk come prodotto) |
|------|------------------------|--------------------------------|
| **Visitor Desk** | Solo **Mercante Ombra**; **niente trade libero**; **niente reputazione sistemica** — solo **flag narrativi** leggibili (`prepare_payment`, `meet_or_avoid_merchant`, esiti patto, `golden_cucumber_outcome`, ecc.). UI può riusare shell MISSION/conversazione + scambi **scriptati** dove serve. | MISSION + TRADE liberi, più fazioni, motore trade, reputazione persistente come da sezioni precedenti di questo doc. |
| **Lab** | **Ricetta guidata** per ottenere **Il Piacere Dimenticato** / seme demo; niente sandbox Lab. | Sequenze complete, cataloghi, esplorazione sistemi. |
| **Dome** | **Piantare → crescita compressa → raccogliere Cetriolo d’Oro**; niente gestione estesa (pH/azioni profonde fuori dal minimo necessario). | Loop cupola pieno. |
| **Scelte** | Poche, **esplicite**, conseguenze **corte ma leggibili**. | Ramificazioni e sistemi più lunghi. |
| **Finale** | **Stesso glitch** (spine comune), **colorato** dal comportamento del player (rami copy / tint). | Estensioni post-demo. |

Con questa impostazione la demo evita la confusione da **“troppi sistemi”**: diventa una **vertical slice** chiara — strana, satirica, memorabile — con **assaggio vero** del gioco completo sotto il cofano.

### Checklist implementazione (post Desk “full” in codice)

Quando la shell MISSION/TRADE e il motore trade saranno **stabili** nel binario, usare la tabella sopra come criterio di **gate demo** (cosa spegnere o non esporre in `IsDemo`). Esempi operativi:

- [ ] Disattivare **FORECAST** e **TRADE libero** in sessione demo; scambi come **script** + UI minima.
- [ ] Nessun loop reputazione **sistemico** in demo: solo **flag** + copy Mercante.
- [ ] CRT/header: ridurre solo se necessario per performance o leggibilità playtest.

*La parte “full” di questo documento resta la specifica per il prodotto nel tempo; la demo consuma un **sottoinsieme narrativo** del Desk, non il contrario.*
