# End of Day Sequence (UIToolkit)

Sequenza EoD completa: Conferma → Snapshot → Diario → (Night Research se azioni ≥ 1) → Forecast → Sleep → Dawn → fine. Foundation + stile terminale/neon.

## Setup in scena

1. **Crea il pannello EoD**
   - Crea un GameObject (es. `EndOfDaySequence`).
   - Aggiungi **UIDocument**: in *Source Asset* assegna `EndOfDaySequence.uxml`.
   - Imposta *Panel Settings* come gli altri pannelli (es. copia da TopBar o da un altro UIDocument in scena).
   - Aggiungi il componente **EndOfDaySequenceController**.
   - Lascia il GameObject **disattivato** di default (il controller lo attiva con `StartSequence()`).

2. **Collega il trigger**
   - Su **Bed**: nel campo *Eod Controller* assegna il GameObject con `EndOfDaySequenceController`. Opzionale: *Diary UI* come fallback.
   - Su **EndDayButton**: stesso riferimento in *Eod Controller*.

## Flusso

- **Step 1 (Conferma):** "END DAY?" con YES/NO. YES → save, Step 2. NO → Hide.
- **Step 2 (Snapshot):** SPORAE — Day N, System Date, Vault Status, Dome pH, Activity Summary (da DayActivityLog + DiaryStatistics), Drift, Notes. CTA "Confirm → Diario" → Step 3.
- **Step 3 (Diario):** S.P.O.R.A.E FRAGMENT (testo da attività del giorno). CTA "Continue" → Step 4 se azioni ≥ 1, altrimenti Step 5.
- **Step 4 (Night Research):** Historical Archive / Botanical Database / Vault Protocols / Skip. Scelta → WikiUnlockService + transizione → Step 5.
- **Step 5 (Forecast):** [TODAY] e [TOMORROW FORECAST], eventuale "Research Complete". CTA "Sleep → Next Day" → EndDay() (fade), poi OnDayChanged → Step 6.
- **Step 6 (Dawn):** DAWN SUMMARY, eventi da NightEventsGenerator. CTA "Continue" → Hide, fine sequenza.
