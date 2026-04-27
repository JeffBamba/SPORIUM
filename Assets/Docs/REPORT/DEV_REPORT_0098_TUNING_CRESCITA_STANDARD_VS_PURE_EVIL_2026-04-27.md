# DEV REPORT 0098 — Tuning crescita piante: Standard più rapide, Pure/Evil allineate

**Data:** 2026-04-27  
**Sprint / contesto:** Fine tuning bilanciamento crescita piante per supportare economia early game (item rapidi da Standard) e mantenere Pure/Evil come percorso di cura dedicato.  
**Riferimento piano:** tuning gameplay richiesto in chat (vincolo: accelerazione controllata, differenziazione tra famiglie).  
**Report precedente:** `DEV_REPORT_0097_LED_PH_DRIFT_RIMOSSO_2026-04-27.md`

---

## Sommario interventi

1. Aggiornate le durate stage della specie **Standard** su profilo rapido: `1 + 1 + 1 + 2` (Seed->Sprout->Growth->Flowering).
2. Uniformate le durate stage di **Pure** e **Evil** su profilo comune: `1 + 2 + 2 + 3`.
3. Consolidata una differenziazione funzionale chiara: Standard come fonte veloce di item, Pure/Evil come linee più lente e curate.

---

## Statistiche e progresso

### Righe di codice

- **0 righe `.cs` modificate** in questa iterazione (intervento solo su asset dati `PlantData` in YAML).  
- Misurazione scope: verifica file toccati nella cartella piante (`Assets/Resources/Plants`).

### Sistemi funzionanti

- **Da validare in Editor** (playtest consigliato):
- Avanzamento stage per Standard con timing ridotto e ciclo più rapido verso produzione.
- Avanzamento stage Pure/Evil con velocità uguale tra le due famiglie.
- Persistenza e lettura corretta dei `durationDays` da `PlantData` nel `DayCycleController`.

### Bug risolti

- **0 bug fix espliciti** (iterazione di tuning/bilanciamento, non di correzione difetti runtime).

### Progresso gameplay / prodotto

- Il player può usare le Standard come leva di cassa rapida con tempi di crescita più brevi.
- Pure ed Evil restano scelte più “curate”, con stesso pacing di base per confronto più leggibile.
- Il pacing complessivo diventa più comprensibile: linea veloce (Standard) vs linee specialistiche (Pure/Evil).
- Ridotta la frizione iniziale per ottenere item vendibili senza appiattire l’identità delle famiglie avanzate.

---

## 1. Bilanciamento durate stage per famiglia

### Problema

- Le durate precedenti producevano una separazione meno netta tra uso economico rapido (Standard) e linee Pure/Evil.
- Era richiesta una semplificazione del pacing crescita con differenza intenzionale tra famiglie.

### Soluzione

- Aggiornati i `durationDays` negli asset specie con regole target:
- **Standard:** `1 + 1 + 1 + 2`
- **Pure:** `1 + 2 + 2 + 3`
- **Evil:** `1 + 2 + 2 + 3`
- L’intervento è data-driven: nessuna modifica alla logica C#, solo parametri di crescita per stadio.

**File interessati:**  
`Assets/Resources/Plants/PLT-STD-001.asset`  
`Assets/Resources/Plants/PLT-PURE-001.asset`  
`Assets/Resources/Plants/PLT-EVIL-001.asset`

---

## 2. Impatto di design su economia e progressione

### Problema

- Serviva un flusso chiaro per il player: ottenere item vendibili rapidamente senza rendere equivalenti tutte le famiglie.

### Soluzione

- Definita una baseline di progressione dove le Standard coprono il bisogno di rotazione veloce inventario/vendita.
- Mantenute Pure/Evil su velocità identica per ridurre complessità comparativa in questa fase di tuning.
- Preservata la possibilità di future iterazioni su resa/frutti senza cambiare il framework stage-based esistente.

**File interessati:**  
`Assets/Resources/Plants/PLT-STD-001.asset`  
`Assets/Resources/Plants/PLT-PURE-001.asset`  
`Assets/Resources/Plants/PLT-EVIL-001.asset`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/Resources/Plants/PLT-STD-001.asset` | Tuning `durationDays` stage 1-4 su profilo rapido `1+1+1+2` |
| `Assets/Resources/Plants/PLT-PURE-001.asset` | Tuning `durationDays` stage 1-4 su profilo `1+2+2+3` |
| `Assets/Resources/Plants/PLT-EVIL-001.asset` | Tuning `durationDays` stage 1-4 su profilo `1+2+2+3` |

---

## Regole / vincoli rispettati

- Approccio data-driven: nessuna introduzione di nuove dipendenze runtime o scan scena.
- Nessuna duplicazione di sistemi demo/full: tuning applicato al binario unico tramite asset condivisi.
- Modifica limitata e incrementale, senza alterare API o orchestrazione (`DayCycleController` invariato).

---

## Note operative (Unity)

- Verifica consigliata in Play Mode (SCN_VaultMap):
- Piantare una Standard e confermare transizioni più rapide su stage iniziali.
- Piantare Pure ed Evil e confermare timing identico tra famiglie.
- Controllare che l’avanzamento rispetti ancora i vincoli già in uso (parametri ottimali, punti, condizione, mold risk).
- Se il pacing risulta ancora troppo lento/veloce, prossimo step consigliato: micro-tuning solo su stage `Flowering` mantenendo invariati i primi tre step.

---

*Fine DEV REPORT 0098.*
