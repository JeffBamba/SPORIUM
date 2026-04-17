---
name: Terminal Pot + PlantCard v3 — decisioni congelate
overview: Decisioni di prodotto/UI per riordino comandi START, alias LINEE GUIDA, barra H2O/LED, progress bar pcv3-left; include modello “staging + Conferma tutto” e 1 AP per batch.
todos:
  - id: spec-batch-ap
    content: "Implementare enqueue da pulsante barra (= YES del flow) + 1 AP batch H2O+LED additivo col resto coda"
    status: pending
  - id: impl-start-linee-guida
    content: "Implementare START riordinato + alias LINEE GUIDA/PROTOCOL + rimozione WATERING/LED da elenco"
    status: pending
  - id: impl-shortcut-staging
    content: "Barra sotto pcv3-center: header + toggle staging H2O/LED + pulsante CONFERMA MODIFICHE (1 AP) → enqueue batch WAT+LED"
    status: pending
  - id: impl-progress-bars
    content: "pcv3-left progress bar Parametri + Stato vitale dove ha senso"
    status: pending
---

# Decisioni (congelate)

## 1. Scorciatoie H2O / LED-R / LED-B (modello scelto)

- L’utente può cambiare **ON/OFF** sulla barra **più volte** (staging) senza aver ancora “chiuso” il flusso come con il **Y** del terminale.
- Sulla barra c’è un **pulsante dedicato** che conferma le scelte su **Watering e luce**: è il **corrispettivo dello YES** quando si conclude il flow di un comando da terminale. Da quel momento l’azione entra in coda **come le altre**.
- L’**esecuzione effettiva** resta quella di oggi: **alla chiusura del terminale** (stesso comportamento attuale per le azioni in coda).
- **Costo AP**: per il batch H2O+LED confermato dalla barra vale **1 AP totale** per quel blocco (non 1 AP per toggle). Se in coda ci sono **anche** altre azioni a pagamento, il costo è **additivo** (batch sistemi + AP delle altre voci come oggi).

**Nota implementativa**: oggi il terminale accoda toggle con **ApCost = 1** ciascuno (`BeginConfirmToggleAction`). Implementare una **singola voce coda** (o equivalente) che porti lo stato target combinato con **ApCost = 1**, oppure regola di merge in enqueue — senza cambiare la fase di esecuzione a chiusura terminale.

## 2. Vaso vuoto — barra shortcut

- **Visibile ma disabilitata** + tooltip breve (es. nessuna coltura attiva).

## 3. LINEE GUIDA vs PROTOCOL

- In tutta l’**UI giocatore** (START, welcome, tooltip): nome principale **LINEE GUIDA**.
- **`PROTOCOL` resta alias** digitabile (non mostrato come nome principale in START).

## 4. START — hint comandi WATERING / LED

- **Nessuna** riga aggiuntiva che dica che si possono ancora digitare i comandi (barra + alias da soli).

## 5. Progress bar `pcv3-left`

- **Parametri** + **Stato vitale** dove ha senso (es. condizione come barra o equivalente).

---

## Chiuso (follow-up)

- **Conferma barra**: pulsante **sulla barra** (= YES del flow comando), non solo “Conferma tutto” globale senza passo intermedio.
- **Coda mista**: AP **additivi** (1 AP batch H2O+LED + altre azioni come oggi).

## 6. Pulsante conferma batch (mock UI)

**Comportamento**: alla pressione, l’azione combinata **WAT+LED** (stato irrigazione + LED risultante dagli staging toggle) viene **messa in coda** con costo **1 AP** per quel blocco, come da decisioni §1.

**Aspetto (riferimento visivo)** — fascia in basso sotto i tre toggle:

- Pulsante **a tutta larghezza** della barra, sfondo **ambra/arancio**, testo **nero** monospace.
- Copy mock: `✓ CONFIRM CHANGES (1 AP)`; in gioco preferire italiano coerente con il resto del terminale, es. **`✓ CONFERMA MODIFICHE (1 AP)`** (stesso significato del mock).
- Glow leggero ambra sul pulsante (in **USS**, non solo inline sul campione — parità Builder).

**Struttura barra completa (tre zone)**:

1. **Header** (fascia verde): a sinistra `POT-xxx`, a destra nome pianta (es. `ARCTIC HASK`).
2. **Zona toggle** (sfondo nero): etichette ciano tipo `[H2O]`, `[LED-R]`, `[LED-B]` con sotto pulsantini stato **ON** (verde) / **OFF** (rosso) per lo **staging**.
3. **Zona conferma**: pulsante ambra descritto sopra.

---

## Riferimenti codice (repo)

- Elenco `START`: [`PlantCardV3TerminalController.cs`](Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs) ~5296+
- Toggle con conferma e `ApCost = 1`: stesso file ~6057–6128
- Pot selezionato HUD: `_selectedPotIndex`, `RefreshHudFromSelectedPot()` ~2012+
- UXML center: [`PlantCardV3_Terminal.uxml`](Assets/_Project/UI/UIToolkit/PlantCardV3/PlantCardV3_Terminal.uxml) (`pcv3-center`)
