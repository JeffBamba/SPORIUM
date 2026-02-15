# DEV REPORT 0055 — End of Day: sequenza Hibernation/Dawn Summary, tooltip Dawn, Snapshot Notes

**Data:** 2026-02-15  
**Scope:** Sequenza End of Day (step Hibernation, Day transition, DAWN SUMMARY con parametri e tooltip), fix posizione tooltip, Snapshot/Notes (Inventory, Seed Storage, pH trend, rimozione placeholder).  
**Riferimenti:** `END_OF_DAY_UI_SEQUENCE_SPEC.md`, `.cursor/plans/da fare/end_of_day_sequence_logic.plan.md`

---

## 1. Nuovi step sequenza EoD (dopo Forecast)

Dopo la schermata **Forecast** (step 5) e il click su **Sleep**, la sequenza non chiama più subito `EndDay()` ma mostra in ordine:

### 1.1 Step 6 — Hibernation (full screen)
- Schermata a tutto schermo su sfondo nero.
- Testo principale: **"SYSTEMS IN HIBERNATION MODE..."** (verde, stile terminale).
- Sottotesto: **"darkness feeds"** (grigio, più piccolo).
- Durata configurabile in Inspector: `_hibernationScreenDuration` (default 2,5 s).

### 1.2 Step 7 — Day transition (full screen)
- Stessa estetica full screen nera.
- Riga 1: **"DAY XX"** (blu).
- Riga 2: **"→ DAY XX+1"** (verde).
- Valori XX / XX+1 derivano dal giorno corrente al momento del click Sleep.
- Durata: `_dayTransitionScreenDuration` (default 2,5 s).
- Al termine della durata viene chiamato `_dayCycleSystem.EndDay()`.

### 1.3 Step 8 — DAWN SUMMARY (ex step 6 Dawn)
- Mostrato in `OnDayChanged` dopo il cambio giorno effettivo.
- Titolo **"DAWN SUMMARY"**, sottotitolo **"DAY N – OVERNIGHT CHANGES"**.
- Testo introduttivo: *"The Vault awakens from standby mode. Environmental sensors report overnight variations. Life has shifted in the darkness. Systems recalibrate."*
- **Elenco parametri** (allineato alla Top Bar):
  - Indice di Mutazione
  - pH Drift
  - Condensation
  - G-rate
  - CRY Balance (forecast fine giorno, solo costi fissi)
- Ogni riga: icona + testo + **"?"**; al passaggio del mouse viene mostrato un **tooltip** con titolo (es. `[PH DRIFT DETECTED]`), descrizione e sezione **TIP** (come da allegati di riferimento).
- Dati letti da: `PhSystem`, `CondensationSystem`, `GameManager.CurrentCRY`, `EndDayButton.GetDailyPowerCost()`, `TopBarController.GetMutationIndex()` / `GetGrateValue()` (getter aggiunti in TopBar per EoD).

---

## 2. Fix posizione tooltip Dawn Summary

### Problema
Il tooltip delle voci del DAWN SUMMARY si apriva in posizione errata (non in corrispondenza del mouse).

### Causa
In UI Toolkit `MouseEnterEvent` / `MouseMoveEvent` forniscono `mousePosition` in **coordinate panel (world)**. I valori `style.left` e `style.top` del tooltip sono invece relativi al **parent** del tooltip (`eod-step8`). Usare direttamente `mousePosition` come left/top produce quindi uno scostamento.

### Soluzione (come per tooltip item Lab)
- Conversione da coordinate panel a coordinate locali del parent del tooltip tramite **`parent.WorldToLocal(mousePosPanel)`** (API UI Toolkit: `VisualElementExtensions.WorldToLocal`).
- Nuovo metodo **`PositionDawnTooltipAtMouse(Vector2 mousePosPanel, VisualElement sourceRow)`** che:
  - converte in locale con `_dawnTooltip.parent.WorldToLocal(mousePosPanel)`;
  - applica offset (16, 12) e clamp entro `parent.contentRect` per tenere il tooltip dentro il pannello.
- Chiamata a `PositionDawnTooltipAtMouse(evt.mousePosition, row)` in `MouseEnterEvent` e `MouseMoveEvent` per ogni riga parametro.

Riferimento: stessa logica usata per i tooltip degli item nei pannelli Lab (inventario/output).

---

## 3. Snapshot EoD: Notes & Tags, Drift, rimozioni

### 3.1 Inventory e Seed Storage sotto Notes & Tags (c)
- **Rimossa** dalla sezione **Activity Summary** la riga `"Inventory: " + BuildInventorySummary()`.
- Nella sezione **[NOTES & TAGS]** sono state aggiunte:
  - **Inventory .........** con `BuildInventorySummary()` (spore Pure/Evil/Standard, seeds, reagents).
  - **Seed Storage ......** con il nuovo **`BuildSeedStorageSummary()`**: conteggio Pre-Seed, Seed001, Seed002, Seed003 dall’inventario.
- Eliminate le vecchie righe "Seed Storage ...... OK" e "Seed Storage — Oggetti presenti (dettaglio): —" (placeholder).

### 3.2 Rimozione Moral Drift (d)
- Rimossa dalla sezione Notes & Tags la riga **"Moral Drift ........ Stable [placeholder]"**.

### 3.3 Rimozione voce Reputation duplicata (e)
- **Activity Summary:** rimossa la riga placeholder `"Reputation: ↑ Custodians (+0) — ↓ Mold Cult (0) [placeholder...]"`.
- **Drift & Consequences:** rimossa la riga `"Reputation: — [placeholder]"` (ridondante con quanto già rimosso sopra).

### 3.4 pH trend collegato al dato reale (f)
- Nella sezione **Drift & Consequences** la riga sul pH non è più placeholder.
- Utilizzo di **PhSystem** e **DayCycleController**:
  - `_phSystem.CurrentPh` e `_phSystem.GetBandName()` per stato attuale (es. "6.2 (Neutral)").
  - `_dayCycleController.GetPredictedPhDriftForNextDay()` per il drift previsto (es. "+0.1" o "—" se non disponibile).
- Formato mostrato: **"pH trend: {CurrentPh:F1} ({GetBandName()}), drift {predicted}"** (es. `pH trend: 6.2 (Neutral), drift +0.1`).

---

## 4. File modificati

| File | Modifiche |
|------|-----------|
| `UI/UIToolkit/EndOfDay/EndOfDaySequence.uxml` | Step 6 → Hibernation (full screen, due label). Step 7 → Day transition (full screen, DAY XX / → DAY XX+1). Step 8 → DAWN SUMMARY (titolo, intro a 2 righe, lista 5 parametri con icona + testo + "?", pannello tooltip, "Press any key", pulsante Continue). |
| `UI/UIToolkit/EndOfDay/EndOfDaySequence.uss` | Stili `.eod-fullscreen`, `.eod-hibernation-main/sub`, `.eod-day-from/to`; step 7 e 8 in `SetStepVisible`; stili step 8 (righe parametro, tooltip, press key). |
| `UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs` | Step 6–8, label hibernation/day; coroutine `TransitionHibernationThenEndDay` (ShowStep 6 → wait → ShowStep 7 → wait → EndDay); `OnDayChanged` → PopulateDawn + ShowStep(8); `PopulateDawn` con 5 parametri e dati da PhSystem, CondensationSystem, GameManager, EndDayButton, TopBarController; `RegisterDawnTooltipsOnce` + `PositionDawnTooltipAtMouse` (WorldToLocal); Snapshot: Inventory/Seed Storage in Notes, rimozione Moral Drift e Reputation, pH trend reale; `BuildSeedStorageSummary()`. |
| `UI/UIToolkit/HUD/TopBarController.cs` | Getter pubblici `GetMutationIndex()` e `GetGrateValue()` per uso in Dawn Summary. |

---

## 5. Note per QA / verifiche

- **Sequenza:** Dopo Forecast, clic su Sleep → compaiono in ordine Hibernation (2,5 s), Day transition (DAY N → DAY N+1, 2,5 s), poi cambio giorno e DAWN SUMMARY con i 5 parametri. Verificare che i valori (pH, Condensation, CRY forecast, Mutation, G-rate) siano coerenti con la Top Bar / stato di gioco.
- **Tooltip Dawn:** Passando il mouse sulle voci del DAWN SUMMARY il tooltip deve aprirsi **vicino al cursore** e restare agganciato al movimento; non deve uscire dal pannello.
- **Snapshot:** In Notes & Tags devono comparire **Inventory** e **Seed Storage** con gli stessi dati di gioco; niente Moral Drift né righe Reputation duplicate; in Drift & Consequences la riga **pH trend** deve mostrare valore reale (es. "6.2 (Neutral), drift +0.1") e non placeholder.

---

*Fine DEV REPORT 0055.*
