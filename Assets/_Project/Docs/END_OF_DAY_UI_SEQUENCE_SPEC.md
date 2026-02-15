# End of Day UI Sequence — Specifica testuale

**Trigger:** il player interagisce con il **BED** nella stanza **BEDROOM**. La sequenza End of Day inizia e non è skippabile nella sua struttura (il player deve attraversare le schermate nell’ordine indicato).

**Stack UI:** UIToolkit. Stile complessivo: terminale / holografico, sfondo scuro, bordi neon (verde/cyan/arancione/blu), font monospace o digital.

---

## Step 1 — Conferma "End Day?" (punto di non ritorno)

- **Schermata:** modale centrata, stile olografico: bordo neon blu, sfondo nero/grigio con texture statica sottile.
- **Titolo:** "END DAY?" in alto, font cyan/blu chiaro, leggermente pixelato/digitale.
- **Testo descrittivo (2 righe):**
  - "Close systems and enter hibernation mode."
  - "The Vault rests, but life keeps breathing underground."
  Testo grigio-bianco, sans-serif digitale.
- **Pulsanti (side-by-side):**
  - **YES:** bordo e testo neon verde; evidenziato come azione primaria/conferma.
  - **NO:** bordo e testo grigio; azione secondaria/annulla.
- **Comportamento:** solo su YES si prosegue allo Step 2 (e si può fare save qui). Su NO la modale si chiude e il gioco resta in stato giorno corrente.

---

## Step 2 — Snapshot / Riepilogo giornata (conferma dati)

- **Schermata:** finestra a bordo neon verde, stile terminale; sfondo scuro, testo verde monospace; scrollbar a destra (contenuto scrollabile).
- **Header:**
  - Logo "SPORAE" (orb viola/rosa con accenti verdi) + "SPORAE — Day N".
  - "System Date: DD.MM.YYYY" (data di gioco).
  - "Vault Status: Operational".
  - "Dome pH: valore (High/Low/Neutral) (trend, es. Stable Acid → Neutral)".
- **Sezione "Activity Summary":**
  - Azioni usate: "Actions used: X/4".
  - CRY: "CRY consumed: N (balance: ±M)".
  - Raccolti: "Harvested: [lista piante e livelli]".
  - Innaffiature: "Watered Pot X and Pot Y (critical hydration avoided)" (o simile).
  - Attività lab: "Microscope: …", "Pipette: …", "Harvested (Sold): …", "Red LED activated on Pot X (cost: N CRY/day)", ecc.
- **Sezione "Drift & Consequences":**
  - Riepilogo pH e narrativa (es. "pH stabilized thanks to … The Dome breathes better today. Maybe.").
  - Reputazione: "↑ Custodians (+N)", "↓ Mold Cult (-N)" con breve contesto.
- **Sezione "Active Conditions":**
  - Es. "Pot 3: Light Infestation (mold risk if untreated tomorrow)", "Passive Slot A: …", "Inventory: …".
- **Sezione "[NOTES & TAGS]":**
  - Bullet: "Seed Storage integrity ………. OK", "Research Activity ………. ON HOLD", "Moral Drift ………. Stable", "Warning ………. Mold risk in Dome Slot #N".
- **Footer:** "■ S.P.O.R.A.E FRAGMENT".
- **CTA:** in fondo (anche fuori view, da scroll) pulsante tipo "Confirm" / "End Day" per passare allo Step 3. Questa schermata è il riepilogo dati prima del diario narrativo.

---

## Step 3 — Diario di giornata (S.P.O.R.A.E FRAGMENT)

- **Schermata:** stesso stile terminale, bordo neon verde, testo verde su sfondo scuro; scrollbar.
- **Header "[NOTES & TAGS]":** stessi bullet di status (Seed Storage, Research Activity, Moral Drift, Warning) come nello Step 2.
- **Contenuto principale "S.P.O.R.A.E FRAGMENT" (Diary Entry):**
  - Titolo: "S.P.O.R.A.E FRAGMENT".
  - Testo narrativo typewriter: riflessioni poetiche/personali sulla giornata (es. vendita pianta, messaggi del Cult, decisioni rimandate). Più paragrafi, tono post-apocalittico e solitudine del Vault.
  - In chiusura: "SPORAE System: Recording completed.", "Memory integrity: XX% …", "Next wake in: Xh XXm", "Good night, Biologist. Or whoever you are."
- **Footer "[COMMANDS]":**
  - **Pulsante primario (verde):** "[CONTINUE → NIGHT RESEARCH]" — porta allo Step 4 (se ci sono azioni residue) o allo step Forecast (Step 5) se non c’è research.
  - **Pulsante secondario (verde sbiadito):** "[CLOSE LOG]" — chiude il pannello diario; il flusso può continuare verso Night Research o Forecast a seconda della logica (azioni residue).
- **Comportamento:** il testo del fragment può essere mostrato con typewriter lento; il player è spettatore; i CTA abilitano il passaggio allo step successivo.

---

## Step 4 — Night Research Selection

- **Schermata:** sfondo scuro, titoli e bordi a colori neon (verde, arancione, blu).
- **Header:**
  - Titolo: "NIGHT RESEARCH SELECTION" (verde, grande, maiuscolo).
  - Sottotitolo: "Select one research branch to investigate overnight".
- **Tre pannelli selezionabili (uno sotto l’altro):**
  1. **Historical Archive** — bordo/icona arancione-dorato; icona rotolo/documento; descrizione: "Study records of the Collapse and past factions."; "Effect: Unlocks new lore entries".
  2. **Botanical Database** — bordo/icona neon verde; icona piantina; descrizione: "Analyze plants and mutation patterns."; "Effect: Unlocks new plant info or bonuses".
  3. **Vault Protocols** — bordo/icona cyan; icona floppy; descrizione: "Decrypt old system files, identity fragments."; "Effect: Unlocks secret logs, SPORAE entries".
- **Pulsante:** "[SKIP RESEARCH]" in basso, centrato (sfondo scuro, testo bianco/grigio).
- **Testo esplicativo in basso (corsivo/stile diverso):** "Spend one remaining Action to study, analyze, or decrypt the past. Knowledge will bloom tomorrow."
- **Comportamento:** il player seleziona un ramo (o Skip); dopo la scelta, transizione (es. 1.5s) e poi si passa allo Step 5 (Forecast). Questa schermata è mostrata solo se ci sono azioni residue (≥ 1).

---

## Step 5 — Night Summary / Forecast System

- **Schermata:** bordo cyan/azzurro, sfondo blu scuro/nero, font monospace cyan; scrollbar.
- **Header:** "NIGHT SUMMARY - FORECAST SYSTEM", "SESSION LOG: DD -> DD+1" (transizione giorno N → N+1).
- **Sezione "[TODAY]":** elenco a bullet con label e valori allineati a destra (puntini di separazione):
  - Actions Used: X / 4
  - CRY Gained: ±N
  - pH Drift: ±N (Stable/Up/Down)
  - Dome Events: "Nome evento"
  - Reputations: Fazione ±N | Fazione ±N
  - Mutations Logged: N (tipo)
- **Sezione "[TOMORROW FORECAST]":**
  - Actions Available: 4
  - Predicted pH Drift: range (es. +1 to +3)
  - Environmental Risks: testo (es. "Mild humidity loss")
  - Missions Active: N (nomi fazioni)
  - Mood: Neutral / altro
- **Box "Research Complete:" (se è stata fatta research):** "→ New lore fragment unlocked" (o messaggio analogo).
- **Sezione "[COMMANDS]":** pulsante "[SLEEP / CONTINUE -> NEXT DAY]" (bordo cyan).
- **Footer:** citazione tematica: "Close the day and let the Vault dream. Tomorrow awaits."
- **Comportamento:** il player legge il riepilogo e la previsione; su "[SLEEP / CONTINUE -> NEXT DAY]" si conferma il passaggio notte → avvio fade to black, avanzamento giorno, e poi Step 6 (Dawn Summary).

---

## Step 6 — Dawn Summary (cambiamenti overnight)

- **Schermata:** sfondo blu scuro con griglia/sottofondo digitale, puntini bianchi tipo stelle; titoli blu chiaro monospace luminosi.
- **Header:** "DAWN SUMMARY", sottotitolo "DAY N – OVERNIGHT CHANGES".
- **Testo introduttivo (2 righe):**
  - "The Vault awakens from standby mode. Environmental sensors report overnight variations."
  - "Life has shifted in the darkness. Systems recalibrate."
- **Pannello centrale (bordo cyan luminoso, angoli a bracket):** elenco **eventi notturni**, uno per riga, ciascuno con:
  - icona a sinistra (◊, ≈, ▲, •) e colore (arancione, blu, rosso, bianco/grigio);
  - testo evento (es. "pH drifted +0.11 (alkaline trend)", "Condensation decreased by 3%", "Temperature fluctuation logged", "The Dome breathes slowly. The spores dream.");
  - a destra: icona ? e cerchio colorato (stesso colore della riga).
- **CTA in fondo al pannello:** "Press any key to continue" (o pulsante "Continue") — allineato a destra.
- **Comportamento:** gli eventi possono essere rivelati uno alla volta (es. intervallo 0.6s). Il player deve premere un tasto (o cliccare Continue) per chiudere e tornare al gioco nel nuovo giorno (agency). A quel punto la sequenza End of Day è conclusa.

---

## Riepilogo ordine e transizioni

| Ordine | Nome step           | Schermata / Allegato | Transizione verso      |
|--------|---------------------|------------------------|-------------------------|
| 0      | Trigger             | BED in BEDROOM         | Step 1                  |
| 1      | End Day?            | Allegato 1             | Step 2 (solo se YES)    |
| 2      | Snapshot giornata   | Allegato 2 (dettaglio) | Step 3 (Confirm/CTA)    |
| 3      | Diario (Fragment)   | Allegato 2 + 3         | Step 4 o Step 5*         |
| 4      | Night Research      | Allegato 4             | Step 5 (dopo scelta + delay) |
| 5      | Forecast            | Allegato 5             | Sleep → Step 6          |
| 6      | Dawn Summary        | Allegato 6             | Fine sequenza (nuovo giorno) |

\* Se azioni residue ≥ 1: Diario → Night Research (Step 4) → Forecast (Step 5). Se azioni residue = 0: Diario → Forecast (Step 5). Poi sempre Forecast → Sleep → Dawn (Step 6).

Questo documento può essere usato come riferimento per implementare la sequenza End of Day con UIToolkit (layout, sezioni, CTA e flusso tra step).
