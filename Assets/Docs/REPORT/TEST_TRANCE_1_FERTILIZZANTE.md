# TEST TRANCE 1 - Sistema Fertilizzante (BLK-03.01-T1)

**Data Creazione:** 2025-01-XX  
**Versione:** 1.0  
**Stato:** 📋 LISTA TEST

---

## 📋 **PANORAMICA TEST**

Questa lista contiene tutti i test da eseguire per verificare il corretto funzionamento del Sistema Fertilizzante implementato nella TRANCE 1.

**Obiettivo:** Verificare che il sistema fertilizzante funzioni correttamente senza rompere le funzionalità esistenti.

---

## ✅ **TEST BASE - FUNZIONALITÀ ESISTENTI (Non-Breaking Changes)**

### **Test 1.1: Verifica Funzionalità Esistenti Non Rotte**
**Priorità:** 🔴 CRITICA  
**Obiettivo:** Verificare che le modifiche non abbiano rotto funzionalità esistenti

**Azioni:**
1. ✅ Piantare un seme (DoPlant)
2. ✅ Attivare/disattivare sistema irrigazione (DoWater - toggle)
3. ✅ Attivare/disattivare sistema LED (DoLight - toggle)
4. ✅ Applicare spray antifungino (DoSprayAntifungal)
5. ✅ Raccogliere frutti (DoHarvest)
6. ✅ Verificare che la crescita delle piante continui a funzionare
7. ✅ Verificare che il sistema pH continui a funzionare
8. ✅ Verificare che l'UI esistente (HUD Piante) non sia rotta

**Risultato Atteso:**
- ✅ Tutte le funzionalità esistenti continuano a funzionare normalmente
- ✅ Nessun errore in console
- ✅ UI esistente visibile e funzionante

---

## 🌿 **TEST FERTILIZZANTE - APPLICAZIONE BASE**

### **Test 2.1: Applicazione Fertilizzante Standard**
**Priorità:** 🟢 ALTA  
**Obiettivo:** Verificare applicazione corretta fertilizzante Standard

**Setup:**
- Pianta Standard (es. PLT-STD-001) in stadio Seed o Growth
- Inventario contiene almeno 1x `fertilizer-standard`

**Azioni:**
1. Selezionare vaso con pianta Standard
2. Cliccare bottone "Fertilizzare" nella HUD
3. Verificare che fertilizzante venga applicato

**Risultato Atteso:**
- ✅ Fertilizzante Standard applicato correttamente
- ✅ `FertilizerLevel` aumenta di +25% (es. 0% → 25%)
- ✅ Fertilizzante consumato dall'inventario
- ✅ Campo testuale fertilizzante aggiornato con nuovo livello
- ✅ Colore campo testuale verde se nel range ottimale

---

### **Test 2.2: Applicazione Fertilizzante Pure**
**Priorità:** 🟢 ALTA  
**Obiettivo:** Verificare applicazione corretta fertilizzante Pure

**Setup:**
- Pianta Pure (es. PLT-PURE-001) in stadio Seed o Growth
- Inventario contiene almeno 1x `fertilizer-pure`

**Azioni:**
1. Selezionare vaso con pianta Pure
2. Cliccare bottone "Fertilizzare"
3. Verificare che fertilizzante venga applicato

**Risultato Atteso:**
- ✅ Fertilizzante Pure applicato correttamente
- ✅ `FertilizerLevel` aumenta di +40% (es. 0% → 40%)
- ✅ Fertilizzante consumato dall'inventario

---

### **Test 2.3: Applicazione Fertilizzante Prohibited**
**Priorità:** 🟢 ALTA  
**Obiettivo:** Verificare applicazione corretta fertilizzante Prohibited

**Setup:**
- Pianta Evil (es. PLT-EVIL-001) in stadio Seed o Growth
- Inventario contiene almeno 1x `fertilizer-prohibited`

**Azioni:**
1. Selezionare vaso con pianta Evil
2. Cliccare bottone "Fertilizzare"
3. Verificare che fertilizzante venga applicato

**Risultato Atteso:**
- ✅ Fertilizzante Prohibited applicato correttamente
- ✅ `FertilizerLevel` aumenta di +40% (es. 0% → 40%)
- ✅ Fertilizzante consumato dall'inventario

---

### **Test 2.4: Clamp Fertilizzante 0-100%**
**Priorità:** 🟡 MEDIA  
**Obiettivo:** Verificare che fertilizzante non superi 100%

**Setup:**
- Pianta con `FertilizerLevel = 80%`
- Inventario contiene fertilizzante compatibile

**Azioni:**
1. Applicare fertilizzante (+25% o +40%)

**Risultato Atteso:**
- ✅ `FertilizerLevel` viene clampato a 100% (non supera 100%)
- ✅ Nessun errore in console

---

## 🚨 **TEST COERENZA GENETICA - MORTE IMMEDIATA**

### **Test 3.1: Standard + Pure = MORTE IMMEDIATA**
**Priorità:** 🔴 CRITICA  
**Obiettivo:** Verificare morte immediata pianta Standard con fertilizzante Pure

**Setup:**
- Pianta Standard (es. PLT-STD-001) in qualsiasi stadio
- Inventario contiene almeno 1x `fertilizer-pure`

**Azioni:**
1. Selezionare vaso con pianta Standard
2. Cliccare bottone "Fertilizzare"
3. Selezionare fertilizzante Pure

**Risultato Atteso:**
- 🚨 **MORTE IMMEDIATA** della pianta
- ✅ Pianta rimossa dal vaso (`HasPlant = false`)
- ✅ Tutti i contatori resettati (Stage = 0, Hydration = 0, etc.)
- ✅ Evento `PotEvents.EmitPlantDied()` emesso
- ✅ Error log in console con dettagli incompatibilità
- ✅ Fertilizzante comunque consumato (già usato)
- ✅ Campo testuale fertilizzante mostra "-" (vaso vuoto)

---

### **Test 3.2: Standard + Prohibited = MORTE IMMEDIATA**
**Priorità:** 🔴 CRITICA  
**Obiettivo:** Verificare morte immediata pianta Standard con fertilizzante Prohibited

**Setup:**
- Pianta Standard in qualsiasi stadio
- Inventario contiene almeno 1x `fertilizer-prohibited`

**Azioni:**
1. Applicare fertilizzante Prohibited a pianta Standard

**Risultato Atteso:**
- 🚨 **MORTE IMMEDIATA** della pianta
- ✅ Pianta rimossa dal vaso
- ✅ Evento morte emesso

---

### **Test 3.3: Pure + Prohibited = MORTE IMMEDIATA**
**Priorità:** 🔴 CRITICA  
**Obiettivo:** Verificare morte immediata pianta Pure con fertilizzante Prohibited

**Setup:**
- Pianta Pure in qualsiasi stadio
- Inventario contiene almeno 1x `fertilizer-prohibited`

**Azioni:**
1. Applicare fertilizzante Prohibited a pianta Pure

**Risultato Atteso:**
- 🚨 **MORTE IMMEDIATA** della pianta
- ✅ Pianta rimossa dal vaso
- ✅ Evento morte emesso

---

### **Test 3.4: Evil + Pure = MORTE IMMEDIATA**
**Priorità:** 🔴 CRITICA  
**Obiettivo:** Verificare morte immediata pianta Evil con fertilizzante Pure

**Setup:**
- Pianta Evil in qualsiasi stadio
- Inventario contiene almeno 1x `fertilizer-pure`

**Azioni:**
1. Applicare fertilizzante Pure a pianta Evil

**Risultato Atteso:**
- 🚨 **MORTE IMMEDIATA** della pianta
- ✅ Pianta rimossa dal vaso
- ✅ Evento morte emesso

---

### **Test 3.5: Compatibilità Corrette**
**Priorità:** 🟢 ALTA  
**Obiettivo:** Verificare che fertilizzanti compatibili funzionino correttamente

**Test Cases:**
1. **Pure + Standard**: ✅ Deve funzionare (Pure tollera Standard)
2. **Pure + Pure**: ✅ Deve funzionare
3. **Evil + Standard**: ✅ Deve funzionare (Evil tollera Standard)
4. **Evil + Prohibited**: ✅ Deve funzionare
5. **Standard + Standard**: ✅ Deve funzionare

**Risultato Atteso:**
- ✅ Fertilizzante applicato correttamente
- ✅ Nessuna morte della pianta
- ✅ `FertilizerLevel` aumenta correttamente

---

## 📉 **TEST DECADIMENTO**

### **Test 4.1: Decadimento Giornaliero Base**
**Priorità:** 🟢 ALTA  
**Obiettivo:** Verificare decadimento -5% al giorno

**Setup:**
- Pianta con `FertilizerLevel = 50%`
- Fine giornata (EndDay chiamato)

**Azioni:**
1. Applicare fertilizzante (es. +25% → `FertilizerLevel = 25%`)
2. Avanzare al giorno successivo (EndDay)
3. Verificare decadimento

**Risultato Atteso:**
- ✅ `FertilizerLevel` diminuisce di 5% (es. 25% → 20%)
- ✅ Log in console: `"[BLK-03.01-T1] {PotId}: Decadimento fertilizzante - 25% → 20%"`

---

### **Test 4.2: Decadimento fino a 0%**
**Priorità:** 🟡 MEDIA  
**Obiettivo:** Verificare che fertilizzante non scenda sotto 0%

**Setup:**
- Pianta con `FertilizerLevel = 3%`

**Azioni:**
1. Avanzare al giorno successivo

**Risultato Atteso:**
- ✅ `FertilizerLevel` viene clampato a 0% (non scende sotto 0)
- ✅ `DaysFertilizerActive` resettato a 0 quando raggiunge 0%

---

### **Test 4.3: Tracking Giorni Consecutivi**
**Priorità:** 🟡 MEDIA  
**Obiettivo:** Verificare incremento `DaysFertilizerActive`

**Setup:**
- Pianta con `FertilizerLevel > 0%`

**Azioni:**
1. Avanzare più giorni consecutivi mantenendo fertilizzante > 0%

**Risultato Atteso:**
- ✅ `DaysFertilizerActive` incrementa ogni giorno che `FertilizerLevel > 0`
- ✅ Reset quando `FertilizerLevel` raggiunge 0%

---

## 🔄 **TEST TRANSIZIONE RESTING → FLOWERING**

### **Test 5.1: Transizione Automatica Resting → Flowering**
**Priorità:** 🟢 ALTA  
**Obiettivo:** Verificare transizione quando si applica fertilizzante a pianta in Resting

**Setup:**
- Pianta in stadio **Resting** (Stage = 6)
- Inventario contiene fertilizzante compatibile

**Azioni:**
1. Applicare fertilizzante compatibile

**Risultato Atteso:**
- ✅ Transizione automatica a **Flowering** (Stage = 4)
- ✅ `DaysInCurrentStage` resettato a 0
- ✅ Evento `PotEvents.EmitPlantStageChanged()` emesso
- ✅ UI aggiornata con nuovo stadio

---

## 📊 **TEST RANGE FERTILIZZANTE - VALORI FISSI**

### **Test 6.1: Range Seed (60-75-90)**
**Priorità:** 🟡 MEDIA  
**Obiettivo:** Verificare range fertilizzante per stadio Seed

**Setup:**
- Pianta in stadio **Seed** (Stage = 1)
- Verificare `StageRequirements` per Seed

**Azioni:**
1. Controllare valori range fertilizzante in PlantData

**Risultato Atteso:**
- ✅ `fertilizerMin = 60`
- ✅ `fertilizerMed = 75`
- ✅ `fertilizerMax = 90`
- ✅ Valori identici per tutte le piante (Standard, Pure, Evil)

---

### **Test 6.2: Range Growth (40-60-80)**
**Priorità:** 🟡 MEDIA  
**Obiettivo:** Verificare range fertilizzante per stadio Growth

**Setup:**
- Pianta in stadio **Growth** (Stage = 3)

**Risultato Atteso:**
- ✅ `fertilizerMin = 40`
- ✅ `fertilizerMed = 60`
- ✅ `fertilizerMax = 80`

---

### **Test 6.3: Range Flowering (20-40-60)**
**Priorità:** 🟡 MEDIA  
**Obiettivo:** Verificare range fertilizzante per stadio Flowering

**Setup:**
- Pianta in stadio **Flowering** (Stage = 4)

**Risultato Atteso:**
- ✅ `fertilizerMin = 20`
- ✅ `fertilizerMed = 40`
- ✅ `fertilizerMax = 60`

---

### **Test 6.4: Range HarvestReady (0-0-0)**
**Priorità:** 🟡 MEDIA  
**Obiettivo:** Verificare che HarvestReady non richieda fertilizzante

**Setup:**
- Pianta in stadio **HarvestReady** (Stage = 5)

**Risultato Atteso:**
- ✅ `fertilizerMin = 0`
- ✅ `fertilizerMed = 0`
- ✅ `fertilizerMax = 0`

---

### **Test 6.5: Range Resting (30-50-70)**
**Priorità:** 🟡 MEDIA  
**Obiettivo:** Verificare range fertilizzante per stadio Resting

**Setup:**
- Pianta in stadio **Resting** (Stage = 6)

**Risultato Atteso:**
- ✅ `fertilizerMin = 30`
- ✅ `fertilizerMed = 50`
- ✅ `fertilizerMax = 70`

---

## 🎨 **TEST UI**

### **Test 7.1: Bottone Fertilizzante Visibile**
**Priorità:** 🟢 ALTA  
**Obiettivo:** Verificare che bottone fertilizzante sia visibile nella HUD

**Azioni:**
1. Selezionare vaso con pianta
2. Verificare HUD Piante

**Risultato Atteso:**
- ✅ Bottone "Fertilizzare" visibile nella HUD
- ✅ Posizionato accanto ai bottoni Water e Light
- ✅ Bottone abilitato se `CanFertilize()` = true

---

### **Test 7.2: Campo Testuale Fertilizzante**
**Priorità:** 🟢 ALTA  
**Obiettivo:** Verificare campo testuale mostra range e percentuale

**Setup:**
- Pianta in stadio Growth con `FertilizerLevel = 50%`

**Azioni:**
1. Selezionare vaso
2. Verificare campo testuale fertilizzante

**Risultato Atteso:**
- ✅ Campo mostra: `"🌿 Fertilizzante: 50% (Range: 40-60-80%)"`
- ✅ Colore verde se nel range ottimale (vicino a 60%)
- ✅ Colore giallo se nel range ma non ottimale
- ✅ Colore rosso se fuori range

---

### **Test 7.3: Campo Testuale Aggiornato dopo Applicazione**
**Priorità:** 🟡 MEDIA  
**Obiettivo:** Verificare che campo testuale si aggiorni dopo applicazione

**Azioni:**
1. Applicare fertilizzante
2. Verificare campo testuale

**Risultato Atteso:**
- ✅ Campo testuale aggiornato immediatamente con nuovo livello
- ✅ Range mostrato corretto per stadio corrente

---

### **Test 7.4: Campo Testuale Reset quando Vaso Vuoto**
**Priorità:** 🟡 MEDIA  
**Obiettivo:** Verificare che campo testuale mostri "-" quando vaso vuoto

**Azioni:**
1. Selezionare vaso vuoto
2. Verificare campo testuale

**Risultato Atteso:**
- ✅ Campo mostra: `"🌿 Fertilizzante: -"`
- ✅ Colore grigio

---

### **Test 7.5: Selettore Fertilizzante**
**Priorità:** 🟡 MEDIA  
**Obiettivo:** Verificare che selettore fertilizzante funzioni

**Setup:**
- Inventario contiene fertilizzanti disponibili

**Azioni:**
1. Cliccare bottone "Fertilizzare"
2. Verificare che fertilizzante venga applicato

**Risultato Atteso:**
- ✅ Selettore trova fertilizzante disponibile
- ✅ Fertilizzante applicato correttamente
- ✅ Messaggio se nessun fertilizzante disponibile

---

## 🔧 **TEST INVENTARIO**

### **Test 8.1: Consumo Fertilizzante dall'Inventario**
**Priorità:** 🟢 ALTA  
**Obiettivo:** Verificare che fertilizzante venga consumato

**Setup:**
- Inventario contiene 2x `fertilizer-standard`

**Azioni:**
1. Applicare fertilizzante
2. Verificare inventario

**Risultato Atteso:**
- ✅ Quantità fertilizzante diminuisce di 1
- ✅ Inventario aggiornato correttamente

---

### **Test 8.2: Verifica Fertilizzante Disponibile**
**Priorità:** 🟡 MEDIA  
**Obiettivo:** Verificare che bottone sia disabilitato se nessun fertilizzante disponibile

**Setup:**
- Inventario NON contiene fertilizzanti

**Azioni:**
1. Selezionare vaso con pianta
2. Verificare bottone fertilizzante

**Risultato Atteso:**
- ✅ Bottone disabilitato o mostra messaggio appropriato

---

## 💾 **TEST SALVATAGGIO E CARICAMENTO**

### **Test 9.1: Salvataggio con Fertilizzante**
**Priorità:** 🟡 MEDIA  
**Obiettivo:** Verificare che fertilizzante venga salvato correttamente

**Azioni:**
1. Applicare fertilizzante a pianta
2. Salvare gioco
3. Caricare gioco

**Risultato Atteso:**
- ✅ `FertilizerLevel` salvato e caricato correttamente
- ✅ `DaysFertilizerActive` salvato e caricato correttamente
- ✅ Nessun errore in console

---

### **Test 9.2: Retrocompatibilità Salvataggi Vecchi**
**Priorità:** 🟡 MEDIA  
**Obiettivo:** Verificare che salvataggi vecchi (senza fertilizzante) funzionino

**Azioni:**
1. Caricare salvataggio vecchio (prima di TRANCE 1)
2. Verificare che gioco funzioni

**Risultato Atteso:**
- ✅ Salvataggio caricato senza errori
- ✅ `FertilizerLevel = 0` (valore default)
- ✅ `DaysFertilizerActive = 0` (valore default)
- ✅ Nessun errore in console

---

## 🧪 **TEST SCENARI COMPLESSI**

### **Test 10.1: Applicazione Multipla Fertilizzante**
**Priorità:** 🟡 MEDIA  
**Obiettivo:** Verificare applicazione multipla di fertilizzante

**Azioni:**
1. Applicare fertilizzante Standard (+25%)
2. Applicare di nuovo fertilizzante Standard (+25%)
3. Verificare livello totale

**Risultato Atteso:**
- ✅ `FertilizerLevel = 50%` (25% + 25%)
- ✅ Clamp a 100% se supera

---

### **Test 10.2: Fertilizzante + Decadimento Multi-Giorno**
**Priorità:** 🟡 MEDIA  
**Obiettivo:** Verificare decadimento su più giorni

**Setup:**
- Pianta con `FertilizerLevel = 50%`

**Azioni:**
1. Avanzare 5 giorni consecutivi
2. Verificare decadimento

**Risultato Atteso:**
- ✅ Dopo 5 giorni: `FertilizerLevel = 25%` (50% - 5*5%)
- ✅ Dopo 10 giorni: `FertilizerLevel = 0%`

---

### **Test 10.3: Fertilizzante + Transizione Stadio**
**Priorità:** 🟡 MEDIA  
**Obiettivo:** Verificare che fertilizzante persista durante transizione stadio

**Setup:**
- Pianta in Growth con `FertilizerLevel = 50%`

**Azioni:**
1. Far avanzare pianta a Flowering
2. Verificare fertilizzante

**Risultato Atteso:**
- ✅ `FertilizerLevel` mantiene valore (50%)
- ✅ Range fertilizzante aggiornato per nuovo stadio (20-40-60)

---

## 📝 **CHECKLIST TEST COMPLETAMENTO**

### **Test Critici (Must Pass)**
- [ ] Test 1.1: Funzionalità esistenti non rotte
- [ ] Test 3.1-3.4: Morte immediata per incompatibilità
- [ ] Test 2.1-2.3: Applicazione fertilizzanti base
- [ ] Test 4.1: Decadimento giornaliero
- [ ] Test 5.1: Transizione Resting → Flowering
- [ ] Test 7.1-7.2: UI bottone e campo testuale

### **Test Importanti (Should Pass)**
- [ ] Test 3.5: Compatibilità corrette
- [ ] Test 4.2-4.3: Decadimento fino a 0% e tracking giorni
- [ ] Test 6.1-6.5: Range fertilizzante valori fissi
- [ ] Test 7.3-7.5: UI completa
- [ ] Test 8.1-8.2: Inventario

### **Test Opzionali (Nice to Have)**
- [ ] Test 9.1-9.2: Salvataggio e retrocompatibilità
- [ ] Test 10.1-10.3: Scenari complessi

---

## 🐛 **TEST BUG COMUNI**

### **Test Bug 1: Fertilizzante Applicato a Vaso Vuoto**
**Priorità:** 🟡 MEDIA  
**Obiettivo:** Verificare che bottone sia disabilitato per vaso vuoto

**Risultato Atteso:**
- ✅ Bottone disabilitato
- ✅ `CanFertilize()` ritorna false

---

### **Test Bug 2: Fertilizzante Applicato senza Inventario**
**Priorità:** 🟡 MEDIA  
**Obiettivo:** Verificare gestione errore se inventario null

**Risultato Atteso:**
- ✅ Nessun crash
- ✅ Messaggio errore appropriato

---

### **Test Bug 3: Fertilizzante Applicato con PlantData Null**
**Priorità:** 🟡 MEDIA  
**Obiettivo:** Verificare gestione se PlantData non trovato

**Risultato Atteso:**
- ✅ Nessun crash
- ✅ Messaggio errore appropriato
- ✅ Operazione fallisce gracefully

---

## 📊 **REPORT TEST**

Dopo aver eseguito i test, compilare:

**Test Eseguiti:** ___ / ___  
**Test Passati:** ___ / ___  
**Test Falliti:** ___ / ___  
**Bug Trovati:** ___  

**Note:**
- [Inserire note sui test falliti]
- [Inserire bug trovati]
- [Inserire osservazioni]

---

**Fine Documento**

