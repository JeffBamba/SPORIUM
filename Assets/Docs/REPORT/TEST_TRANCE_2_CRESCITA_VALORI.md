# TEST TRANCE 2 - Sistema Crescita Basato su Valori (BLK-03.01-T2)

**Data Creazione:** 2025-01-XX  
**Versione:** 1.0  
**BLK Code:** BLK-03.01-T2  
**Stato:** 🧪 TESTING

---

## 📋 **PANORAMICA**

Questo documento descrive i test step-by-step per verificare che il sistema di crescita basato su valori (TRANCE 2) funzioni correttamente. Il sistema calcola punti giornalieri basati su valori nel range ideale per idratazione, luce e fertilizzante.

---

## 🎯 **OBIETTIVI DI TEST**

1. ✅ Verificare che i punti giornalieri vengano calcolati correttamente
2. ✅ Verificare che i giorni consecutivi ottimali vengano tracciati correttamente
3. ✅ Verificare che la condizione della pianta influenzi l'avanzamento
4. ✅ Verificare che l'avanzamento stadio richieda tutti i requisiti (punti, giorni, giorni ottimali, range)
5. ✅ Verificare che l'UI mostri correttamente punti e giorni ottimali

---

## 🧪 **TEST SCENARIOS**

### **TEST 1: Calcolo Punti Giornalieri - Tutti i Parametri Ottimali**

**Obiettivo:** Verificare che quando tutti i parametri sono nel range ideale, vengono assegnati 3 punti (1 per ogni parametro).

**Setup:**
1. Avvia il gioco
2. Pianta un seme Standard (PLT-STD-001) in un vaso
3. Attendi che la pianta raggiunga lo stadio **Growth** (richiede Blue LED)

**Azioni:**
1. Imposta idratazione al 55% (range ideale Growth: 35-55-75%)
2. Attiva Blue LED (richiesto per Growth)
3. Applica fertilizzante Standard per raggiungere 50% (range ideale Growth: 40-60-80%)
4. Avanza al giorno successivo (usa comando End Day o aspetta)

**Risultato Atteso:**
- ✅ `GrowthPointsWater = 1` (idratazione nel range)
- ✅ `GrowthPointsLight = 1` (LED corretto)
- ✅ `GrowthPointsFertilizer = 1` (fertilizzante nel range)
- ✅ `DaysConsecutiveOptimal = 1` (tutti i parametri ottimali)
- ✅ UI mostra: "Punti: W:1 L:1 F:1 (Tot: 3/3)" in verde
- ✅ UI mostra: "Giorni Ottimali: 1" in giallo/arancione

**Verifica:**
- Controlla i log di debug: `[BLK-03.01-T2] Punti giornalieri - Water: 1, Light: 1, Fertilizer: 1, Total: 3`
- Controlla la UI del vaso (PotHUDWidget o PotDetailsWidget)

---

### **TEST 2: Calcolo Punti Giornalieri - Parametri Parziali**

**Obiettivo:** Verificare che quando solo alcuni parametri sono nel range, vengono assegnati solo i punti corrispondenti.

**Setup:**
1. Pianta un seme Standard in un vaso
2. Attendi che raggiunga lo stadio **Growth**

**Azioni:**
1. Imposta idratazione al 20% (fuori range: 35-55-75%)
2. Attiva Blue LED (corretto)
3. Applica fertilizzante per raggiungere 50% (nel range: 40-60-80%)
4. Avanza al giorno successivo

**Risultato Atteso:**
- ✅ `GrowthPointsWater = 0` (idratazione fuori range)
- ✅ `GrowthPointsLight = 1` (LED corretto)
- ✅ `GrowthPointsFertilizer = 1` (fertilizzante nel range)
- ✅ `DaysConsecutiveOptimal = 0` (non tutti i parametri ottimali)
- ✅ UI mostra: "Punti: W:0 L:1 F:1 (Tot: 2/3)" in giallo

**Verifica:**
- Controlla i log di debug
- Controlla la UI del vaso

---

### **TEST 3: Tracking Giorni Consecutivi Ottimali**

**Obiettivo:** Verificare che i giorni consecutivi ottimali vengano incrementati correttamente e resettati quando i parametri escono dal range.

**Setup:**
1. Pianta un seme Standard in un vaso
2. Attendi che raggiunga lo stadio **Growth**

**Azioni:**
1. **Giorno 1:** Imposta tutti i parametri ottimali (idratazione 55%, Blue LED, fertilizzante 50%)
2. Avanza al giorno successivo
3. **Giorno 2:** Mantieni tutti i parametri ottimali
4. Avanza al giorno successivo
5. **Giorno 3:** Mantieni tutti i parametri ottimali
6. Avanza al giorno successivo
7. **Giorno 4:** Imposta idratazione al 20% (fuori range)
8. Avanza al giorno successivo

**Risultato Atteso:**
- ✅ Giorno 1: `DaysConsecutiveOptimal = 1`
- ✅ Giorno 2: `DaysConsecutiveOptimal = 2`
- ✅ Giorno 3: `DaysConsecutiveOptimal = 3`
- ✅ Giorno 4: `DaysConsecutiveOptimal = 0` (reset perché idratazione fuori range)
- ✅ UI mostra i giorni ottimali incrementare e poi resettare

**Verifica:**
- Controlla i log di debug per ogni giorno
- Controlla la UI del vaso

---

### **TEST 4: Modificatore Condizione - Rigogliosa (-1 giorno)**

**Obiettivo:** Verificare che una pianta in condizione Rigogliosa richieda 1 giorno in meno per avanzare.

**Setup:**
1. Pianta un seme Standard in un vaso
2. Attendi che raggiunga lo stadio **Growth**
3. Mantieni la pianta in condizioni ottimali per raggiungere condizione **Rigogliosa** (score 90-100)

**Azioni:**
1. Verifica che la condizione sia Rigogliosa (controlla UI condizione)
2. Imposta tutti i parametri ottimali
3. Verifica `durationDays` per Growth (es. 3 giorni)
4. Avanza giorni fino a raggiungere `DaysInCurrentStage = 2` (1 giorno in meno del normale)
5. Verifica che l'avanzamento avvenga

**Risultato Atteso:**
- ✅ `effectiveRequiredDays = durationDays - 1` (es. 3 - 1 = 2 giorni)
- ✅ Avanzamento avviene dopo 2 giorni invece di 3
- ✅ Log mostra: `Durata: 2/2 giorni (mod: -1)`

**Verifica:**
- Controlla i log di debug per il modificatore giorni
- Controlla che l'avanzamento avvenga prima del normale

---

### **TEST 5: Blocco Avanzamento - Condizione Critica/Appassita**

**Obiettivo:** Verificare che una pianta in condizione Critica o Appassita non possa avanzare anche se tutti gli altri requisiti sono soddisfatti.

**Setup:**
1. Pianta un seme Standard in un vaso
2. Attendi che raggiunga lo stadio **Growth**
3. Porta la pianta in condizione **Critica** o **Appassita** (score basso, idratazione fuori range per giorni, etc.)

**Azioni:**
1. Verifica che la condizione sia Critica o Appassita (controlla UI condizione)
2. Imposta tutti gli altri parametri ottimali (idratazione, LED, fertilizzante)
3. Accumula 3 punti (W:1, L:1, F:1)
4. Raggiungi i giorni minimi richiesti
5. Raggiungi i giorni consecutivi ottimali richiesti
6. Avanza al giorno successivo

**Risultato Atteso:**
- ✅ Avanzamento **BLOCCATO** anche se tutti gli altri requisiti sono soddisfatti
- ✅ Log mostra: `[BLK-03.01-T2] Avanzamento bloccato - Condizione: Critica` (o Appassita)
- ✅ La pianta rimane nello stadio corrente

**Verifica:**
- Controlla i log di debug per il blocco avanzamento
- Controlla che la pianta non avanzi

---

### **TEST 6: Avanzamento Stadio - Requisiti Completi**

**Obiettivo:** Verificare che l'avanzamento stadio richieda tutti i requisiti: punti, giorni, giorni ottimali, range parametri.

**Setup:**
1. Pianta un seme Standard in un vaso
2. Attendi che raggiunga lo stadio **Growth**

**Azioni:**
1. **Requisito 1 - Punti:** Accumula 3 punti (W:1, L:1, F:1)
2. **Requisito 2 - Giorni:** Raggiungi i giorni minimi richiesti (es. 3 giorni)
3. **Requisito 3 - Giorni Ottimali:** Raggiungi i giorni consecutivi ottimali richiesti (es. 3 giorni)
4. **Requisito 4 - Range Parametri:** Verifica che idratazione, LED e fertilizzante siano nel range
5. **Requisito 5 - Condizione:** Verifica che la condizione non sia Critica/Appassita
6. Avanza al giorno successivo

**Risultato Atteso:**
- ✅ Avanzamento avviene solo quando **TUTTI** i requisiti sono soddisfatti
- ✅ Log mostra: `[BLK-03.01-T2] Stage Growth requisiti - Points: 3/3 [True], Days: 3/3 [True], OptimalDays: 3/3 [True], ...`
- ✅ Dopo avanzamento, i contatori punti vengono resettati:
  - ✅ `GrowthPointsWater = 0`
  - ✅ `GrowthPointsLight = 0`
  - ✅ `GrowthPointsFertilizer = 0`
  - ✅ `DaysConsecutiveOptimal = 0`
  - ✅ `DayOptimalParametersStarted = -1`

**Verifica:**
- Controlla i log di debug per tutti i requisiti
- Controlla che i contatori vengano resettati dopo avanzamento

---

### **TEST 7: Avanzamento Stadio - Requisiti Mancanti**

**Obiettivo:** Verificare che l'avanzamento non avvenga se anche solo uno dei requisiti non è soddisfatto.

**Setup:**
1. Pianta un seme Standard in un vaso
2. Attendi che raggiunga lo stadio **Growth**

**Azioni:**
1. **Caso A - Punti Mancanti:** Accumula solo 2 punti (W:1, L:1, F:0)
   - Raggiungi giorni minimi e giorni ottimali
   - Avanza al giorno successivo
2. **Caso B - Giorni Mancanti:** Accumula 3 punti ma non raggiungi giorni minimi
   - Avanza al giorno successivo
3. **Caso C - Giorni Ottimali Mancanti:** Accumula 3 punti e raggiungi giorni minimi ma non giorni ottimali
   - Avanza al giorno successivo
4. **Caso D - Range Parametri Fuori:** Accumula 3 punti ma idratazione fuori range
   - Avanza al giorno successivo

**Risultato Atteso:**
- ✅ **Caso A:** Avanzamento bloccato - log mostra `Points: 2/3 [False]`
- ✅ **Caso B:** Avanzamento bloccato - log mostra `Days: 2/3 [False]`
- ✅ **Caso C:** Avanzamento bloccato - log mostra `OptimalDays: 2/3 [False]`
- ✅ **Caso D:** Avanzamento bloccato - log mostra `Hydration: 20% (range: 35-75) [False]`

**Verifica:**
- Controlla i log di debug per ogni caso
- Controlla che l'avanzamento non avvenga

---

### **TEST 8: Integrazione Fertilizzante - Range Ideale**

**Obiettivo:** Verificare che il fertilizzante venga considerato nel calcolo punti solo se nel range ideale per lo stadio.

**Setup:**
1. Pianta un seme Standard in un vaso
2. Attendi che raggiunga lo stadio **Seed** (range fertilizzante: 60-75-90%)

**Azioni:**
1. **Giorno 1:** Applica fertilizzante Standard (+25%) → `FertilizerLevel = 25%` (fuori range 60-90%)
   - Avanza al giorno successivo
2. **Giorno 2:** Applica fertilizzante Standard (+25%) → `FertilizerLevel = 50%` (fuori range 60-90%)
   - Avanza al giorno successivo
3. **Giorno 3:** Applica fertilizzante Standard (+25%) → `FertilizerLevel = 75%` (nel range 60-90%, ottimale)
   - Avanza al giorno successivo

**Risultato Atteso:**
- ✅ Giorno 1: `GrowthPointsFertilizer = 0` (25% fuori range)
- ✅ Giorno 2: `GrowthPointsFertilizer = 0` (50% fuori range)
- ✅ Giorno 3: `GrowthPointsFertilizer = 1` (75% nel range, ottimale)
- ✅ UI mostra il fertilizzante in rosso (giorni 1-2) e verde (giorno 3)

**Verifica:**
- Controlla i log di debug per ogni giorno
- Controlla la UI del fertilizzante

---

### **TEST 9: UI - Visualizzazione Punti e Giorni Ottimali**

**Obiettivo:** Verificare che l'UI mostri correttamente i punti e i giorni ottimali.

**Setup:**
1. Pianta un seme Standard in un vaso
2. Attendi che raggiunga lo stadio **Growth**

**Azioni:**
1. Imposta tutti i parametri ottimali
2. Avanza 2 giorni
3. Apri la UI del vaso (PotHUDWidget o PotDetailsWidget)

**Risultato Atteso:**
- ✅ **PotHUDWidget:**
  - Mostra: "📊 Punti: W:2 L:2 F:2 (Tot: 6/3)" in verde
  - Mostra: "⭐ Giorni Ottimali: 2" in giallo
- ✅ **PotDetailsWidget:**
  - Mostra: "Punti Crescita: W:2 L:2 F:2 (Tot: 6/3)" con colori appropriati
  - Mostra: "Giorni Ottimali: 2" con colore appropriato

**Verifica:**
- Controlla entrambe le UI
- Verifica che i colori cambino in base ai valori

---

### **TEST 10: Range Luce - Configurazione Editor**

**Obiettivo:** Verificare che i range luce siano stati configurati correttamente nei PlantData assets.

**Setup:**
1. Apri Unity Editor
2. Vai a menu: `Sporae > Populate Stage Requirements`

**Azioni:**
1. Clicca "Popola Requisiti per Tutte le Piante"
2. Verifica che i PlantData assets siano stati aggiornati
3. Apri un PlantData asset (es. PLT-STD-001)
4. Verifica che ogni StageRequirements abbia i campi `lightMin`, `lightMed`, `lightMax` configurati

**Risultato Atteso:**
- ✅ Tutti i PlantData assets aggiornati
- ✅ Range luce configurati per tutti gli stadi:
  - Seed/Sprout: `lightMin=0, lightMed=50, lightMax=100`
  - Growth/Flowering: `lightMin=50, lightMed=75, lightMax=100` (quando LED richiesto)
  - HarvestReady/Resting: `lightMin=0, lightMed=50, lightMax=100`

**Verifica:**
- Controlla gli assets PlantData in `Assets/Resources/Plants/`
- Verifica i valori nei campi `lightMin`, `lightMed`, `lightMax`

---

## ✅ **CHECKLIST TEST COMPLETAMENTO**

### **Test Funzionalità Base**
- [ ] TEST 1: Calcolo punti giornalieri - tutti i parametri ottimali
- [ ] TEST 2: Calcolo punti giornalieri - parametri parziali
- [ ] TEST 3: Tracking giorni consecutivi ottimali
- [ ] TEST 4: Modificatore condizione - Rigogliosa (-1 giorno)
- [ ] TEST 5: Blocco avanzamento - condizione Critica/Appassita
- [ ] TEST 6: Avanzamento stadio - requisiti completi
- [ ] TEST 7: Avanzamento stadio - requisiti mancanti
- [ ] TEST 8: Integrazione fertilizzante - range ideale
- [ ] TEST 9: UI - visualizzazione punti e giorni ottimali
- [ ] TEST 10: Range luce - configurazione editor

### **Test Regressione**
- [ ] Verifica che il sistema fertilizzante (TRANCE 1) continui a funzionare
- [ ] Verifica che le funzionalità esistenti (Watering, LED, Plant, Harvest) continuino a funzionare
- [ ] Verifica che l'UI esistente non sia stata rotta
- [ ] Verifica che i salvataggi vecchi continuino a funzionare (valori default applicati)

---

## 🐛 **PROBLEMI NOTI**

Nessun problema noto al momento.

---

## 📝 **NOTE**

- I test devono essere eseguiti in ordine per garantire che ogni funzionalità sia testata correttamente
- I log di debug sono essenziali per verificare il corretto funzionamento
- L'UI può essere verificata sia in PotHUDWidget che in PotDetailsWidget

---

## 🔄 **AGGIORNAMENTI**

- **v1.0 (2025-01-XX):** Creazione documento test TRANCE 2

