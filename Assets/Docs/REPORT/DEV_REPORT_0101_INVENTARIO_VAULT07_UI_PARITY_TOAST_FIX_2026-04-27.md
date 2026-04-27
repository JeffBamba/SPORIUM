# DEV REPORT 0101 — Inventario VAULT-07: parità UI Builder, fix apertura e toast consumo

**Data:** 2026-04-27  
**Sprint / contesto:** Hardening UX/UI del pannello inventario UI Toolkit (VAULT-07 CRT) con focus su stabilità layout, selezione item, feedback consumo e coerenza Builder/runtime.  
**Riferimento piano:** `.cursor/plans/demo_alpha_1_0_gap_map.plan.md` (feature Both: inventario condiviso demo/full) + richieste iterative chat-driven su inventario.  
**Report precedente:** `DEV_REPORT_0100_EOD_DIARIO_FORECAST_ALBA_DECISIONALE_2026-04-27.md`

---

## Sommario interventi

1. Stabilizzato il pannello inventario a dimensione terminale fissa e layout rigido (header/chrome/list/detail/footer) senza scaling indesiderato.
2. Rifatto il comportamento del box dettaglio item: apertura/chiusura coerente con selezione, toggle sullo stesso item, chiusura da click esterno senza uscire dal pannello inventario.
3. Confermato e consolidato il wiring consumo (`USA/BEVI/MANGIA`) con emissione toast idratazione anche in casi a delta percentuale 0.
4. Migliorata leggibilità e semantica UI (font minime, one-liner, metadata colorati label/value, riga uso/piace/valore).
5. Rimossa tutta l’instrumentation temporanea di debug dopo verifica issue riprodotte e fix confermato.

---

## Statistiche e progresso

### Righe di codice

- Scope misurato con comando:
  - `git diff --cached --numstat -- .cursor/rules/sviluppa.mdc Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/PlayerStatToastBridge.cs Assets/_Project/Scripts/UI/UIToolkit/PlayerInventory/PlayerInventoryPanelController.cs Assets/_Project/Scripts/UI/UIToolkit/PlayerStatusPanelController.cs Assets/_Project/UI/UIToolkit/PlayerInventory/PlayerInventoryPanel.uss Assets/_Project/UI/UIToolkit/PlayerInventory/PlayerInventoryPanel.uxml`
- Totale file `.cs` toccati:
  - `PlayerInventoryPanelController.cs` → **+899 / -385**
  - `PlayerStatusPanelController.cs` → **+5 / -0**
  - `PlayerStatToastBridge.cs` → **+8 / -3**
- Totale file UI (`.uxml`/`.uss`) toccati:
  - `PlayerInventoryPanel.uxml` → **+89 / -12**
  - `PlayerInventoryPanel.uss` → **+626 / -86**
- Regole workflow:
  - `.cursor/rules/sviluppa.mdc` → **+5 / -0**

### Sistemi funzionanti

- **Verificato in compilazione:** `dotnet build Sporae_Build_Beta.sln --no-restore` completata senza errori/warning.
- **Verificato da lint:** nessun errore sui file inventario toccati.
- **Verificato da riproduzione utente in chat:**
  - apertura inventario da HUD senza doppio click;
  - feedback toast su consumo acqua/cibo.
- **Da validare in Editor (Play/UI Builder):**
  - tuning finale spacing del dettaglio su risoluzioni diverse;
  - authoring diretto `inv-detail` in UI Builder con placeholder/testi di riferimento.

### Bug risolti

- **8 fix principali** in questo blocco:
  1. apertura inventario che richiedeva due click;
  2. toast mancanti su consumo acqua/cibo in alcuni casi;
  3. selezione riga inventario non affidabile con click su aree pulsanti;
  4. clipping scrollbar e testo `[CHIUDI]`;
  5. overlap `inv-list` su `inv-filters`/`inv-stats` con lista popolata o “mostra tutto”;
  6. dettaglio item che rompeva il layout del terminale;
  7. esposizione nome tecnico item nel box principale anziché nome player-facing;
  8. instrumentation debug lasciata in codice dopo fix.

### Progresso gameplay / prodotto

- Il player ora gestisce inventario in modo più prevedibile: selezione, chiusura dettaglio e navigazione sono coerenti.
- Le azioni `USA/BEVI/MANGIA` restituiscono feedback di sistema più chiaro grazie ai toast.
- L’inventario mantiene estetica CRT stabile senza salti visivi durante espansione lista o selezione item.
- Designer e dev possono modificare il box dettaglio direttamente in UI Builder con parità runtime.
- La lettura metadati è più rapida grazie a gerarchia cromatica label/value e tipografia più leggibile.

---

## 1. Stabilità layout inventario VAULT-07

### Problema

- Il terminale inventario mostrava scaling/movimenti non desiderati in base al contenuto.
- In stati “lista piena” o “mostra tutto” alcuni blocchi si sovrapponevano.

### Soluzione

- Consolidata struttura rigida del pannello (`inv-r1`, `inv-top-chrome`, `inv-list`, `inv-expand`, `inv-detail`, `inv-footer`) con altezze coerenti.
- Migliorata gestione overflow/lista/scrollbar per evitare overlap e clipping.
- Preservata parità UI Builder/runtime: modifiche di marca mantenute in USS/UXML, non in branch UI paralleli.

**File interessati:**  
`Assets/_Project/UI/UIToolkit/PlayerInventory/PlayerInventoryPanel.uxml`  
`Assets/_Project/UI/UIToolkit/PlayerInventory/PlayerInventoryPanel.uss`

---

## 2. Interazione dettaglio item e comportamento click esterno

### Problema

- La chiusura del dettaglio item dipendeva da pulsante `X`, con UX poco fluida.
- Click esterno poteva chiudere l’intero inventario invece del solo dettaglio.

### Soluzione

- Rimossa `X` dal dettaglio e introdotto toggle selezione sullo stesso item in lista.
- Click su scrim in modalità inventario standard ora chiude solo il box dettaglio (non il pannello inventario).
- `inv-detail` reso visibile in Builder per editing diretto; runtime lo gestisce via stato selezione.

**File interessati:**  
`Assets/_Project/Scripts/UI/UIToolkit/PlayerInventory/PlayerInventoryPanelController.cs`  
`Assets/_Project/UI/UIToolkit/PlayerInventory/PlayerInventoryPanel.uxml`  
`Assets/_Project/UI/UIToolkit/PlayerInventory/PlayerInventoryPanel.uss`

---

## 3. Consumo item e toast idratazione

### Problema

- In alcuni consumi (acqua/cibo/frutta) il feedback toast non appariva, specialmente con idratazione già alta.

### Soluzione

- Allineata emissione toast in `PlayerStatToastBridge` per sorgenti consumo idratante anche in condizioni con delta arrotondato a 0.
- Confermato wiring con i flussi consumo inventario e gestione apertura pannello da HUD.

**File interessati:**  
`Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/PlayerStatToastBridge.cs`  
`Assets/_Project/Scripts/UI/UIToolkit/PlayerStatusPanelController.cs`  
`Assets/_Project/Scripts/UI/UIToolkit/PlayerInventory/PlayerInventoryPanelController.cs`

---

## 4. Leggibilità UI e metadati

### Problema

- Testi secondari poco leggibili e metadata poco gerarchici.
- Informazioni utili del box dettaglio disperse su più righe.

### Soluzione

- Aggiornata riga metadata a formato compatto (`Si usa in | Piace a | Valore`) con font leggibile.
- Applicata colorazione semantica distinta label/value sia su dettaglio rapido sia su ispezione dettagliata.
- Aggiornata regola progetto `sviluppa.mdc` per codificare il vincolo cromatico metadata.

**File interessati:**  
`Assets/_Project/Scripts/UI/UIToolkit/PlayerInventory/PlayerInventoryPanelController.cs`  
`Assets/_Project/UI/UIToolkit/PlayerInventory/PlayerInventoryPanel.uss`  
`.cursor/rules/sviluppa.mdc`

---

## 5. Cleanup instrumentation post-fix

### Problema

- Erano presenti blocchi di logging temporanei usati per debug ipotesi (toast/apertura inventario).

### Soluzione

- Rimossi helper e append log dedicati, mantenendo solo il codice funzionale necessario.
- Rieseguita compilazione completa per garantire regressione nulla.

**File interessati:**  
`Assets/_Project/Scripts/UI/UIToolkit/PlayerInventory/PlayerInventoryPanelController.cs`  
`Assets/_Project/Scripts/UI/UIToolkit/PlayerStatusPanelController.cs`  
`Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/PlayerStatToastBridge.cs`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/UI/UIToolkit/PlayerInventory/PlayerInventoryPanelController.cs` | Refactor comportamento selezione/dettaglio, toggle chiusura, wiring UI detail, cleanup debug |
| `Assets/_Project/Scripts/UI/UIToolkit/PlayerStatusPanelController.cs` | Stabilizzazione apertura pannello inventario da HUD (lookup affidabile) |
| `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/PlayerStatToastBridge.cs` | Emissione toast consumo idratante in condizioni edge + cleanup debug |
| `Assets/_Project/UI/UIToolkit/PlayerInventory/PlayerInventoryPanel.uxml` | Aggiornamento struttura dettaglio e authoring surface in Builder |
| `Assets/_Project/UI/UIToolkit/PlayerInventory/PlayerInventoryPanel.uss` | Tuning layout CRT, leggibilità testi, stati dettaglio, visibilità authoring |
| `.cursor/rules/sviluppa.mdc` | Regola qualità metadata: palette label/value obbligatoria |

---

## Regole / vincoli rispettati

- UI Toolkit parity: nessun albero UI runtime parallelo per il dettaglio inventario; authoring su nodi reali UXML/USS.
- Vincolo architettura runtime: nessuna introduzione di scan distruttivi o comandi irreversibili.
- Feature Both demo/full: interventi sull’inventario applicati al binario unico con comportamento coerente.
- Cleanup post-debug: rimossa instrumentation temporanea dopo conferma fix.

---

## Note operative (Unity)

- In UI Builder, il box dettaglio è editabile in: `PlayerInventoryPanel.uxml` → `name="inv-detail"`.
- Checklist Play consigliata:
  - seleziona item → dettaglio apre;
  - clicca stesso item → dettaglio chiude;
  - clicca fuori terminale → inventario resta aperto e dettaglio chiude;
  - `USA/BEVI/MANGIA` produce feedback toast coerente.
- Se si desidera chiusura dettaglio anche con click in area vuota interna al terminale, estendere callback su container terminale mantenendo invariata la modalità picker.

---

*Fine DEV REPORT 0101.*
