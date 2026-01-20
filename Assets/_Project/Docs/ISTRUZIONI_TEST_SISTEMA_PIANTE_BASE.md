# Istruzioni Testing - Sistema PIANTE Base (Fasi 2-4)

**Data Creazione**: 2026-01-XX  
**Stato**: Da completare  
**Fasi da testare**: FASE 2, FASE 3, FASE 4, Test Integrazione

---

## Setup Iniziale

1. Apri Unity e carica la scena principale
2. Avvia **Play mode**
3. Apri la **Console Unity** per vedere i log debug
4. Cerca i seguenti tag nei log:
   - `[GROWTH_MODIFIER]` - Modificatori crescita
   - `[BURN_STRESS]` - Burn Stress attivo
   - `[BURN_STRESS_EXTREME]` - Effetti estremi Burn Stress
   - `[ACT-005]` - Modificatori produzione/harvest

---

## TEST FASE 2: Effetti pH Estremi su Resa e Crescita

### Test 2.1: Crescita pH - Pure in Ultra Basico

**Setup**:
1. Pianta una pianta **Pure** in un vaso
2. Porta pH a **Ultra Basico (≥+80)** usando Blue LED per diversi giorni
   - Attiva Blue LED e lascia acceso per 3-4 giorni consecutivi
   - Verifica che il pH aumenti gradualmente verso +80
3. Verifica che la pianta sia in condizione **Sana** o **Rigogliosa** (non in countdown morte)

**Test**:
1. Avanza **1 giorno** con cura ideale (acqua + LED)
2. Controlla **Console**: cerca `[GROWTH_MODIFIER]`
   - Deve mostrare: `pH (x1.5f)` o simile
   - Deve mostrare moltiplicatore totale combinato
3. Verifica che i **punti crescita** siano aumentati del 50% rispetto al normale
4. Verifica che la pianta **avanzi di stadio più velocemente**

**Risultato Atteso**: Pianta Pure in Ultra Basico cresce **50% più velocemente**

**Note**: Se il pH non raggiunge +80, continua ad attivare Blue LED per più giorni. Il pH aumenta gradualmente.

---

### Test 2.2: Resa pH - Evil in Ultra Acido

**Setup**:
1. Pianta una pianta **Evil** in un vaso
2. Porta pH a **Ultra Acido (≤-80)** usando Red LED
   - Attiva Red LED e lascia acceso per 3-4 giorni consecutivi
   - Verifica che il pH diminuisca gradualmente verso -80
3. Porta la pianta a **HarvestReady** con **3 frutti disponibili**
   - Attendi che la pianta raggiunga HarvestReady
   - Verifica che `AmountFruits` sia 3

**Test**:
1. Esegui **Harvest** (raccogli frutti)
2. Controlla **Console**: cerca `[ACT-005]`
   - Deve mostrare: `Modificatore resa pH UltraAcid per Evil: 1.5f`
   - Deve mostrare calcolo: `quantità: 3.0 → 4.5` (prima dell'arrotondamento)
3. Verifica **quantità frutti raccolti**:
   - Dovrebbero essere **4 o 5 frutti** (3 × 1.5 = 4.5 → arrotondato)
   - **IMPORTANTE**: I frutti devono essere sempre interi, non decimali
4. Verifica nell'**inventario** che i frutti siano stati aggiunti correttamente

**Risultato Atteso**: Evil in Ultra Acido produce **4-5 frutti** invece di 3 (bonus +50%)

**Note**: Se il pH non raggiunge -80, continua ad attivare Red LED per più giorni.

---

### Test 2.3: Tooltip pH Drift

**Setup**:
1. Apri il gioco in Play mode
2. Verifica che la **TopBar HUD** sia visibile (in alto)

**Test**:
1. Passa il **mouse sul display pH** nella TopBar HUD
2. Verifica che il **tooltip pH Drift** si apra
3. Verifica che il tooltip mostri la sezione **"Effetti per Famiglia"** con:

   **Pure**:
   - Crescita: +50% (in Ultra Basico o Stable Basic)
   - Resa: +100% (sterili) (in Ultra Basico)
   
   **Evil**:
   - Crescita: +50% (in Ultra Acido o Stable Acid)
   - Resa: +50% (in Ultra Acido)
   
   **Standard**:
   - Nessun effetto o effetti negativi (in pH estremi)

4. Cambia il **pH** (usa LED o altre azioni) e verifica che il tooltip si **aggiorni** dinamicamente
5. Verifica che i valori mostrati siano **coerenti** con la banda pH corrente

**Risultato Atteso**: Tooltip mostra chiaramente beneficio/svantaggio del pH attuale per ogni famiglia pianta

**Note**: Il tooltip si aggiorna automaticamente quando il pH cambia. Verifica che i colori e la formattazione siano leggibili.

---

## TEST FASE 3: Sistema Burn Stress Completo da LED

### Test 3.1: Burn Stress Attivo

**Setup**:
1. Pianta una pianta **Standard** in un vaso
2. Attiva **LED Red** o **Blue**
3. Lascia LED acceso per **5+ giorni consecutivi** (o `maxDaysForFullStress` giorni)
   - Verifica `MaxDaysForFullStress` nella configurazione (default: 5 giorni)
   - Non spegnere il LED durante questi giorni

**Test**:
1. Avanza giorni fino a raggiungere **100% stress** (`consecutiveDays >= maxDaysForFullStress`)
2. Controlla **Console**: cerca `[BURN_STRESS]`
   - Deve mostrare: `Burn Stress attivo - X giorni consecutivi (max: Y)`
   - Deve mostrare: `DaysBurnStress: Z`
3. Verifica che la **condizione della pianta peggiori**:
   - Apri PlantCard e verifica il badge Conditions
   - La condizione dovrebbe essere peggiore rispetto a prima (es. da Sana a Stressata)
4. Verifica che il **malus Burn Stress** sia applicato al calcolo condizione
   - Il tooltip Conditions dovrebbe mostrare un contributo negativo

**Risultato Atteso**: Burn Stress attivo riduce lo score di condizione quando stress = 100%

**Note**: Se `maxDaysForFullStress` è 5, dopo 5 giorni consecutivi con LED attivo, Burn Stress diventa attivo.

---

### Test 3.2: Effetti Estremi (3 giorni consecutivi)

**Setup**:
1. Pianta una pianta e portala almeno a **Growth** (non Seed)
2. Attiva **LED Red** o **Blue**
3. Lascia LED acceso per **5+ giorni consecutivi** (Burn Stress attivo)
4. Mantieni **Burn Stress attivo per 3 giorni consecutivi**
   - Dopo che Burn Stress diventa attivo (100% stress), continua per altri 3 giorni

**Test**:
1. Avanza **3 giorni** con Burn Stress attivo (stress = 100%)
2. Dopo il **3° giorno consecutivo**:
   - Controlla **Console**: cerca `[BURN_STRESS_EXTREME]`
   - Deve mostrare: `Regressione stage da X a Y`
   - Deve mostrare: `Riduzione livello da Z a W`
3. Verifica **regressione stage**:
   - La pianta deve tornare allo **stadio precedente**
   - Esempio: da Growth → Sprout, da Sprout → Seed
   - Verifica che `DaysInCurrentStage` sia resettato a 0
4. Verifica **riduzione livello**:
   - Se la pianta è **Lvl 2+**, deve perdere **1 livello**
   - Esempio: Lvl 3 → Lvl 2, Lvl 2 → Lvl 1
   - Verifica che la pianta **non possa scendere sotto Lvl 1**
5. Verifica che la pianta **non possa regredere sotto Seed**:
   - Se la pianta è già Seed, non deve regredere ulteriormente

**Risultato Atteso**: Dopo 3 giorni consecutivi di Burn Stress, pianta regredisce di stage e perde 1 livello

**Note**: Gli effetti estremi si applicano solo una volta dopo 3 giorni consecutivi. Il contatore `DaysBurnStressConsecutive` si resetta dopo l'applicazione.

---

### Test 3.3: Reset Burn Stress

**Setup**:
1. Pianta una pianta
2. Attiva LED e raggiungi **100% stress** (Burn Stress attivo)
3. Mantieni Burn Stress per **2 giorni consecutivi** (non 3, per evitare effetti estremi)

**Test**:
1. **Spegni LED** (OFF)
2. Avanza **1 giorno**
3. Controlla **Console**: cerca `[BURN_STRESS]`
   - Non deve mostrare "Burn Stress attivo" (se presente, è un bug)
4. Verifica che `DaysBurnStressConsecutive` si **resetti a 0**:
   - Usa il debug console (P) per verificare il valore
   - Oppure verifica nei log
5. Verifica che gli **effetti estremi non si attivino**:
   - La pianta non deve regredere di stage
   - La pianta non deve perdere livello
6. Verifica che la **condizione migliori**:
   - Il malus Burn Stress dovrebbe essere rimosso
   - La condizione dovrebbe migliorare (es. da Stressata a Sana)

**Risultato Atteso**: Spegnendo LED, Burn Stress si resetta e non applica effetti estremi

**Note**: Il reset avviene immediatamente quando LED viene spento. Il contatore si azzera a 0.

---

## TEST FASE 4: Indicatore Giorni LED Consecutivi nell'HUD

### Test 4.1: Indicatore in PlantCardV2

**Setup**:
1. Pianta una pianta **Standard** in un vaso
2. Attiva **LED Blue** o **Red**

**Test**:
1. Seleziona il **vaso con la pianta**
2. Apri **PlantCardV2** (clicca sul vaso o usa il tasto di apertura)
3. Verifica che nella **sezione LED compatibile** sia mostrato:
   - Formato: `"BLUE (3 giorni)"` o `"RED (5 giorni)"`
   - Il numero di giorni deve corrispondere ai giorni consecutivi con LED attivo
4. **Spegni LED** (OFF) e verifica che l'indicatore:
   - Scompaia completamente, OPPURE
   - Mostri solo `"BLUE"` o `"RED"` senza giorni
5. **Riattiva LED** e verifica che l'indicatore **riappaia** con i giorni

**Risultato Atteso**: PlantCardV2 mostra giorni consecutivi LED quando LED è attivo

**Note**: L'indicatore si aggiorna automaticamente a fine giornata. Se non si aggiorna, potrebbe essere un problema di refresh UI.

---

### Test 4.2: Indicatore in AlwaysVisiblePotHUD Tooltip

**Setup**:
1. Pianta una pianta **Standard** in un vaso
2. Attiva **LED Red** o **Blue**
3. Lascia LED acceso per alcuni giorni (es. 3 giorni)

**Test**:
1. Passa il **mouse sul vaso** (tooltip sempre visibile)
2. Verifica che nel **tooltip crescita** sia mostrato:
   - Formato: `"LED: BLUE (3 giorni, Stress: 60%)"` o formato simile
   - Deve mostrare:
     - Stato LED corrente (BLUE o RED)
     - Giorni consecutivi
     - Stress percentage calcolato correttamente
3. **Cambia giorni consecutivi** (avanza giorni o spegni/riattiva LED) e verifica che il tooltip si **aggiorni**
4. Verifica che **stress percentage** sia calcolato correttamente:
   - Formula: `(consecutiveDays / maxDaysForFullStress) * 100`
   - Esempio: 3 giorni / 5 max = 60%
5. Verifica che il tooltip mostri anche il **burn risk level** se applicabile (quando stress è vicino a 100%)

**Risultato Atteso**: Tooltip AlwaysVisiblePotHUD mostra giorni consecutivi, stress percentage e burn risk level

**Note**: Il tooltip si aggiorna automaticamente quando passi il mouse. Se non si aggiorna, potrebbe essere un problema di refresh.

---

## TEST INTEGRAZIONE: Modificatori Combinati

### Test Integrazione 1: Condizione + pH su Crescita

**Setup**:
1. Pianta una pianta **Pure** in un vaso
2. Porta pH a **Ultra Basico (≥+80)** usando Blue LED
3. Mantieni condizione **Rigogliosa**:
   - Mantieni condizioni ottimali (acqua, LED, pH) per raggiungere Rigogliosa
   - Verifica che la condizione sia effettivamente Rigogliosa (score 80-100)

**Test**:
1. Avanza **1 giorno** con cura ideale (acqua + LED)
2. Controlla **Console**: cerca `[GROWTH_MODIFIER]`
   - Deve mostrare entrambi i moltiplicatori:
     - `Condizione Rigogliosa (x1.2f)`
     - `pH (x1.5f)`
     - `Totale x1.8f` (moltiplicativi, non additivi)
3. Verifica che i **punti crescita** siano aumentati dell'**80%** rispetto al normale:
   - Esempio: se normalmente guadagni 3 punti, con moltiplicatore 1.8 dovresti guadagnare ~5 punti
4. Verifica che la pianta **avanzi di stadio significativamente più velocemente** rispetto a:
   - Una pianta Sana in pH neutrale
   - Una pianta Rigogliosa in pH neutrale
   - Una pianta Sana in Ultra Basico

**Risultato Atteso**: Modificatori condizione e pH sono **moltiplicativi** (1.2 × 1.5 = 1.8), non additivi (1.2 + 1.5 = 2.7)

**Note**: Il moltiplicatore totale deve essere il prodotto dei due moltiplicatori, non la somma.

---

### Test Integrazione 2: Produzione Condizione + pH

**Setup**:
1. Pianta una pianta **Evil** in un vaso
2. Porta pH a **Ultra Acido (≤-80)** usando Red LED
3. Mantieni condizione **Rigogliosa**:
   - Mantieni condizioni ottimali per raggiungere Rigogliosa
4. Porta la pianta a **HarvestReady** con **3 frutti disponibili**

**Test**:
1. Esegui **Harvest** (raccogli frutti)
2. Controlla **Console**: cerca `[ACT-005]`
   - Deve mostrare:
     - `Modificatore produzione condizione Rigogliosa: 1.15f`
     - `Modificatore resa pH UltraAcid per Evil: 1.5f`
     - Calcolo totale: `quantità: 3.0 → 3.45 → 5.175 → 5` (dopo arrotondamento)
3. Verifica **quantità frutti raccolti**:
   - Dovrebbero essere **5 frutti** (non 6, perché arrotondamento dopo ogni moltiplicatore o totale)
   - **IMPORTANTE**: I frutti devono essere sempre interi
4. Verifica che **entrambi i tooltip** mostrino i modificatori:
   - Tooltip Conditions: deve mostrare "+15% produzione frutti"
   - Tooltip Ph Drift: deve mostrare "+50% resa" per Evil in Ultra Acido

**Risultato Atteso**: Produzione finale = base × condizione × pH, arrotondato a intero (3 × 1.15 × 1.5 = 5.175 → 5 frutti)

**Note**: L'arrotondamento avviene dopo aver applicato tutti i moltiplicatori. Il risultato finale deve essere sempre intero.

---

## Checklist Rapida Testing

Usa questa checklist per verificare rapidamente che tutto funzioni:

### FASE 1 - Modificatori Condizioni
- [ ] Tooltip Conditions mostra modificatori crescita/produzione
- [ ] Pianta Rigogliosa cresce 20% più velocemente
- [ ] Pianta Rigogliosa produce +15% frutti
- [ ] Pianta Stressata cresce 10% più lentamente
- [ ] Pianta Stressata produce -15% frutti
- [ ] Frutti raccolti sono sempre interi (arrotondamento corretto)

### FASE 2 - Effetti pH Estremi
- [ ] Tooltip pH Drift mostra effetti per famiglia
- [ ] Pure in Ultra Basico cresce 50% più velocemente
- [ ] Pure in Ultra Basico produce +100% resa (sterili)
- [ ] Evil in Ultra Acido cresce 50% più velocemente
- [ ] Evil in Ultra Acido produce +50% frutti
- [ ] Standard non ha bonus da pH estremi (o ha malus)

### FASE 3 - Burn Stress LED
- [ ] Burn Stress attivo dopo 5 giorni consecutivi LED (o maxDaysForFullStress)
- [ ] Malus Burn Stress applicato al calcolo condizione
- [ ] Effetti estremi (regressione/riduzione livello) dopo 3 giorni Burn Stress consecutivi
- [ ] Reset Burn Stress quando LED spento
- [ ] Pianta non può regredere sotto Seed o livello 1

### FASE 4 - Indicatori HUD
- [ ] Indicatore giorni LED visibile in PlantCardV2
- [ ] Indicatore giorni LED visibile in AlwaysVisiblePotHUD tooltip
- [ ] Indicatori si aggiornano automaticamente a fine giornata
- [ ] Indicatore scompare o cambia quando LED è OFF

### Test Integrazione
- [ ] Modificatori condizione + pH sono moltiplicativi (non additivi)
- [ ] Crescita combinata funziona correttamente (1.2 × 1.5 = 1.8)
- [ ] Produzione combinata funziona correttamente (3 × 1.15 × 1.5 = 5)
- [ ] Frutti raccolti sono sempre interi dopo tutti i moltiplicatori

---

## Note Importanti per il Testing

### Log Console
Cerca questi tag nei log della Console Unity:
- **`[GROWTH_MODIFIER]`** - Mostra modificatori crescita applicati (condizione + pH)
- **`[BURN_STRESS]`** - Mostra quando Burn Stress è attivo
- **`[BURN_STRESS_EXTREME]`** - Mostra quando effetti estremi vengono applicati
- **`[ACT-005]`** - Mostra modificatori produzione/harvest applicati

### Tooltip
- **Tooltip Conditions**: Apri PlantCardV2 o PlantCardV3 Terminal, passa mouse sul badge Conditions
- **Tooltip pH Drift**: Passa mouse sul display pH nella TopBar HUD
- Verifica che i tooltip mostrino le informazioni aggiornate in tempo reale

### Arrotondamento Frutti
- I frutti devono essere **sempre interi**, non decimali
- Esempio: 3.45 frutti → 3 o 4 frutti (arrotondamento standard)
- Esempio: 3.55 frutti → 4 frutti
- L'arrotondamento avviene dopo aver applicato tutti i moltiplicatori

### Moltiplicatori
- **Condizione e pH sono moltiplicativi**, non additivi:
  - ✅ Corretto: 1.2 × 1.5 = 1.8 (moltiplicativo)
  - ❌ Sbagliato: 1.2 + 1.5 = 2.7 (additivo)
- Verifica nei log che il calcolo sia moltiplicativo

### Debug Console
- Usa la **Console P** (tasto P) per verificare valori di stato:
  - `DaysBurnStressConsecutive`
  - `DaysLedBlueConsecutive` / `DaysLedRedConsecutive`
  - `ConditionScore`
  - `AmountFruits`

---

## Problemi Comuni e Soluzioni

### Il pH non raggiunge +80 o -80
- **Soluzione**: Continua ad attivare LED per più giorni. Il pH aumenta/diminuisce gradualmente.
- Blue LED aumenta pH, Red LED diminuisce pH.
- Potrebbero servire 5-7 giorni consecutivi per raggiungere pH estremi.

### Burn Stress non si attiva
- **Verifica**: `MaxDaysForFullStress` nella configurazione (default: 5 giorni)
- **Verifica**: LED deve essere attivo per giorni **consecutivi** (non interrotti)
- **Verifica**: Controlla `DaysLedBlueConsecutive` o `DaysLedRedConsecutive` nel debug console

### Tooltip non si aggiorna
- **Soluzione**: Passa il mouse via e riavvicinalo per forzare il refresh
- **Verifica**: Che il tooltip sia effettivamente quello aggiornato (non una versione cached)

### Frutti non sono interi
- **Bug**: Se vedi frutti decimali, c'è un problema nell'arrotondamento
- **Verifica**: Che `Mathf.RoundToInt()` sia applicato correttamente in `DoHarvest()`

---

## Risultati Attesi - Riepilogo

| Test | Risultato Atteso |
|------|------------------|
| Test 2.1 | Pure in Ultra Basico: +50% crescita |
| Test 2.2 | Evil in Ultra Acido: +50% resa (4-5 frutti) |
| Test 2.3 | Tooltip pH mostra effetti per famiglia |
| Test 3.1 | Burn Stress attivo dopo 5 giorni, malus condizione |
| Test 3.2 | Effetti estremi dopo 3 giorni: regressione stage + riduzione livello |
| Test 3.3 | Reset Burn Stress quando LED spento |
| Test 4.1 | PlantCardV2 mostra giorni LED consecutivi |
| Test 4.2 | AlwaysVisiblePotHUD mostra giorni LED e stress % |
| Test Integrazione 1 | Crescita: 1.2 × 1.5 = 1.8 (moltiplicativo) |
| Test Integrazione 2 | Produzione: 3 × 1.15 × 1.5 = 5 frutti (intero) |

---

**Buon testing!** 🧪

Se trovi bug o comportamenti inattesi, annotali e segnalali.
