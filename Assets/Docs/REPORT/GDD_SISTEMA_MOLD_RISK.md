# Sistema Mold Risk - GDD

## 📋 Panoramica

Il sistema **Mold Risk** (Rischio Muffe) gestisce il rischio di infestazione da muffe nelle piante del Dome. Il sistema si basa principalmente su **overwatering prolungato** e introduce meccaniche di gestione del rischio che influenzano la crescita, la condizione e il livello delle piante.

---

## 🎮 Meccanica Base

### Calcolo del Rischio

Il Mold Risk viene calcolato automaticamente ogni fine giornata basandosi su **giorni consecutivi di overwatering**:

- **Soglia iniziale**: 3 giorni consecutivi di overwatering
- **Formula**: Ogni giorno oltre la soglia aumenta il livello di rischio di 1
- **Esempio**:
  - Giorni 1-3: Overwatering attivo → Nessun rischio (sotto soglia)
  - Giorno 4: 1 giorno oltre soglia → **Mold Risk Level 1** (Mild)
  - Giorno 5: 2 giorni oltre soglia → **Mold Risk Level 2** (Severe)
  - Giorno 6: 3 giorni oltre soglia → **Mold Risk Level 3** (Critical)

**Nota importante**: Se l'overwatering si interrompe, il contatore si resetta a 0 e il rischio diminuisce di conseguenza.

---

## 📊 Livelli di Rischio

Il sistema ha **4 livelli di rischio** (0-3):

### **Level 0 - None** (Nessun Rischio)
- Nessun rischio attivo
- **Bonus Condition Score**: +5 punti
- Nessun effetto negativo

### **Level 1 - Mild** (Rischio Lieve)
- Rischio presente ma gestibile
- Nessun blocco alla crescita
- Se si materializza in infestazione: -1 livello pianta, -10 Condition Score

### **Level 2 - Severe** (Rischio Grave)
- **⚠️ BLOCCA L'AVANZAMENTO** della pianta allo stadio successivo
- La pianta continua a produrre frutti (se già in HarvestReady)
- Se si materializza in infestazione: -3 livelli pianta, -30 Condition Score

### **Level 3 - Critical** (Rischio Critico)
- **⚠️ BLOCCA L'AVANZAMENTO** della pianta allo stadio successivo
- Dopo **2 giorni consecutivi** a questo livello → **Infestazione attiva**
- Se si materializza in infestazione: -3 livelli pianta, -30 Condition Score

---

## 🦠 Infestazione

### Quando si Attiva

L'infestazione si materializza quando:
- **Mold Risk Level = 3** (Critical)
- **E** la pianta resta a livello 3 per **2 giorni consecutivi**

### Effetti dell'Infestazione

Quando una pianta diventa infestata:

1. **Flag Infestata**: La pianta viene marcata come `IsInfested = true`
2. **Riduzione Livello**: 
   - Level 1 (Mild): -1 livello pianta
   - Level 2-3 (Severe/Critical): -3 livelli pianta
3. **Riduzione Condition Score**:
   - Level 1 (Mild): -10 punti
   - Level 2-3 (Severe/Critical): -30 punti
4. **Notifica**: Toast di avviso al giocatore
5. **Blocco Crescita**: Se livello ≥2, la crescita rimane bloccata

### Rimozione Infestazione

L'infestazione viene rimossa automaticamente quando:
- Il Mold Risk Level scende sotto 3
- Il giocatore esegue una **Potatura** (rimuove infestazione e riduce livello)

---

## 🎯 Interazioni con Azioni Giocatore

### 1. **Potatura (Pruning)**

**Effetti sul Mold Risk**:
- Riduce il Mold Risk Level di **1** (o azzera se era ≤1)
- **Rimuove l'infestazione** se presente
- Resetta il contatore "giorni senza potatura"

**Quando usare**: La potatura è il metodo principale per gestire il rischio muffe, specialmente quando il livello è ancora basso (1-2).

### 2. **Additivi pH**

#### **Additivo Basico** (pH Basico)
- **Riduce** il Mold Risk Level di **1** (o azzera se era ≤1)
- Se il livello scende sotto 3, rimuove automaticamente l'infestazione
- **Strategia**: Utile per prevenire o ridurre il rischio muffe

#### **Additivo Acido** (pH Acido)
- **Aumenta** il Mold Risk Level di **1**
- Se il pot è già a livello 3, **propaga il rischio al pot vicino** (se disponibile)
- **Attenzione**: Usare con cautela se il Mold Risk è già alto

**Propagazione**:
- Se un pot a livello 3 riceve un additivo acido, il rischio si propaga al pot più vicino
- Il pot vicino aumenta di 1 livello (o incrementa il contatore se già a livello 3)

---

## 🚫 Blocco Crescita

### Quando Blocca

Il Mold Risk blocca l'avanzamento della pianta quando:
- **Mold Risk Level ≥ 2** (Severe o Critical)

### Effetti del Blocco

Quando la crescita è bloccata:
- ❌ La pianta **non può avanzare** allo stadio successivo
- ✅ Continua a produrre frutti (se già in HarvestReady)
- ✅ Continua a subire gli effetti negativi del rischio
- ✅ Il giocatore può ancora eseguire azioni (potatura, additivi, etc.)

### Sblocco

La crescita viene sbloccata quando:
- Il Mold Risk Level scende sotto 2 (quindi a 0 o 1)
- Metodi per ridurre: Potatura, Additivo Basico, o interrompere l'overwatering

---

## 📈 Impatto su Condition Score

### Bonus: Nessun Mold Risk
- **+5 punti** se `MoldRiskLevel == 0`
- Contribuisce positivamente alla condizione "Rigogliosa" o "Sana"

### Malus: Infestazione
- **-10 punti** se infestata con `MoldRiskLevel == 1` (Mild)
- **Nota**: Il malus per livelli 2-3 non viene applicato perché già blocca l'avanzamento (penalità più grave)

---

## 🎨 Visualizzazione UI

### Indicatori HUD

**Mold Risk Text** (PotDetailsWidget):
- **Level 0**: "Nessuno" (verde)
- **Level 1**: "Mild (Lvl 1)" (arancione)
- **Level 2**: "Severe (Lvl 2)" (arancione)
- **Level 3**: "Critical (Lvl 3)" (rosso)

**Badge INFESTATA**:
- Mostrato solo quando `IsInfested == true`
- Colore: rosso se Level 3, arancione altrimenti
- Indica che la pianta è attualmente infestata

**PlantCardV2**:
- Mostra il livello di rischio (0-3) in un Vital Parameter Box
- Range ideale: 0
- Badge colorato in base al livello
- Messaggio di blocco avanzamento se Level ≥2

---

## 🔔 Notifiche

### Toast Notifications

Il sistema emette notifiche quando:
- **Mold Risk Level raggiunge 3** (Critical): "🚨 CRITICAL mold risk on {potId}."
- **Infestazione attivata**: "🚨 Mold infestation on {potId}."
- **Muffa rilevata**: "Muffa rilevata in {potId}"

---

## 📋 Esempio di Gameplay

### Scenario: Overwatering Accidentale

**Giorno 1-3**:
- Giocatore lascia il sistema di irrigazione attivo
- Pianta in overwatering → `DaysOverwateringConsecutive = 1, 2, 3`
- Mold Risk Level = 0 (sotto soglia)

**Giorno 4**:
- `DaysOverwateringConsecutive = 4`
- Mold Risk Level = 1 (Mild)
- Condition Score: perde bonus +5 (nessun mold risk)
- **Azione consigliata**: Potatura o Additivo Basico

**Giorno 5**:
- `DaysOverwateringConsecutive = 5`
- Mold Risk Level = 2 (Severe)
- ❌ **Avanzamento BLOCCATO**
- **Azione consigliata**: Potatura urgente o Additivo Basico

**Giorno 6**:
- `DaysOverwateringConsecutive = 6`
- Mold Risk Level = 3 (Critical)
- `DaysAtMoldRiskLevel3 = 1`
- ❌ **Avanzamento BLOCCATO**
- Toast: "🚨 CRITICAL mold risk on Pot 1"
- **Azione consigliata**: Potatura immediata o Additivo Basico

**Giorno 7**:
- `DaysOverwateringConsecutive = 7`
- Mold Risk Level = 3 (Critical)
- `DaysAtMoldRiskLevel3 = 2`
- ✅ **Infestazione applicata!**
  - `IsInfested = true`
  - Livello pianta: -3
  - Condition Score: -30
  - Toast: "🚨 Mold infestation on Pot 1"
- ❌ **Avanzamento BLOCCATO**

**Giorno 8**:
- Giocatore esegue **Potatura**
- Mold Risk Level: 3 → 2
- `IsInfested = false` (rimossa)
- `DaysAtMoldRiskLevel3 = 0`
- ❌ **Avanzamento ancora BLOCCATO** (Level 2)

**Giorno 9**:
- Giocatore applica **Additivo Basico**
- Mold Risk Level: 2 → 1
- ✅ **Avanzamento SBLOCATO** (Level 1 < 2)

---

## 🎯 Strategie di Gioco

### Prevenzione
- **Monitorare l'idratazione**: Evitare overwatering prolungato
- **Potatura regolare**: Mantiene il rischio basso
- **Additivi Basici**: Utili per prevenire accumulo di rischio

### Gestione del Rischio
- **Level 1 (Mild)**: Gestibile, ma agire prima che peggiori
- **Level 2 (Severe)**: **Azione immediata richiesta** - blocco crescita attivo
- **Level 3 (Critical)**: **Emergenza** - infestazione imminente dopo 2 giorni

### Recupero
- **Potatura**: Metodo principale per ridurre rischio
- **Additivo Basico**: Alternativa o complemento alla potatura
- **Interrompere overwatering**: Reset automatico del contatore

### Attenzioni
- **Additivi Acidi**: Aumentano il rischio - usare con cautela
- **Propagazione**: Un pot a livello 3 può infettare i vicini con additivi acidi
- **Blocco crescita**: Una volta bloccata, serve ridurre a Level <2 per sbloccare

---

## ⚙️ Configurazione

### Parametri Bilanciamento

- **Soglia Overwatering**: 3 giorni consecutivi prima che inizi il rischio
- **Soglie Livelli**: 
  - Mild: ≥1
  - Severe: ≥2
  - Critical: ≥3
- **Infestazione**: 2 giorni consecutivi a livello 3
- **Penalità Infestazione**:
  - Mild: -1 livello, -10 score
  - Severe/Critical: -3 livelli, -30 score

---

## 🔗 Integrazione con Altri Sistemi

### Watering System
- **Tracking**: Il sistema traccia automaticamente i giorni di overwatering
- **Reset**: Quando l'overwatering termina, il contatore si resetta

### Growth System
- **Blocco**: Mold Risk Level ≥2 blocca l'avanzamento
- **Verifica**: Controllato automaticamente ogni fine giornata

### Condition System
- **Bonus**: +5 punti se nessun rischio
- **Malus**: -10 punti se infestata (Level 1)

### pH System
- **Additivi**: Modificano direttamente il Mold Risk Level
- **Interazione**: Basico riduce, Acido aumenta (con propagazione)

### Pruning System
- **Rimozione**: Potatura rimuove infestazione e riduce rischio

---

## 📝 Note di Design

### Filosofia
Il sistema Mold Risk introduce un **meccanismo di gestione del rischio** che:
- **Premia l'attenzione**: Monitoraggio e azione preventiva evitano problemi
- **Punisce negligenza**: Overwatering prolungato ha conseguenze gravi
- **Offre recupero**: Il giocatore può sempre intervenire per ridurre il rischio
- **Crea tensione**: Il blocco crescita e l'infestazione sono conseguenze significative

### Bilanciamento
- **Soglia 3 giorni**: Dà tempo al giocatore di reagire
- **Blocco a Level 2**: Avvisa prima che diventi critico
- **Infestazione dopo 2 giorni a Level 3**: Ulteriore buffer prima della penalità massima
- **Rimozione con potatura**: Meccanica accessibile per recupero

### Feedback
- **UI chiara**: Indicatori visivi del livello di rischio
- **Notifiche**: Toast quando il rischio diventa critico o si attiva infestazione
- **Blocco crescita**: Feedback immediato che qualcosa non va

---

## 🎮 Conclusione

Il sistema Mold Risk aggiunge profondità strategica alla gestione delle piante, introducendo un meccanismo di rischio che richiede attenzione e gestione attiva. Il sistema è progettato per essere **prevedibile** (basato su overwatering), **gestibile** (con azioni del giocatore) e **conseguente** (con effetti significativi ma recuperabili).
