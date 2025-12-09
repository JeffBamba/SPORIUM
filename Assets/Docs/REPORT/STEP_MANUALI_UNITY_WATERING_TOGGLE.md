# STEP MANUALI UNITY - MIGRAZIONE WATERING TOGGLE

**Data:** 2025-12-09  
**Versione GDD:** 40 v.08/12/2025  
**Sezione GDD:** AZ-11 — Watering System

---

## ✅ IMPLEMENTAZIONE COMPLETATA

Tutte le modifiche al codice sono state completate. Segui questi step manuali per finalizzare l'integrazione in Unity.

---

## 📋 STEP MANUALI DA COMPLETARE IN UNITY

### **1. VERIFICA CONFIGURAZIONE CONDENSAZIONE** ✅

**File:** `Assets/Resources/Configs/CondensationConfig.asset`

**Azione:**
- Apri il file in Unity Editor
- Verifica che `CondensationGrowthPerDay` sia impostato a **3** (era 2)
- Se non è aggiornato, modifica manualmente a **3**

**Verifica:**
```
CondensationGrowthPerDay: 3
MaxCondensation: 10
```

---

### **2. RIMOZIONE RIFERIMENTI WATERINGMINIGAME NELLA SCENA** ⚠️

**File:** Scene con PotDetailsWidget

**Azione:**
1. Apri la scena principale (es. `VaultMap` o scena con i vasi)
2. Seleziona il GameObject con `PotDetailsWidget` component
3. Nel Inspector, verifica che il campo `_wateringMinigame` sia vuoto/null
4. Se c'è un riferimento, rimuovilo (lasciare vuoto)

**Nota:** Il codice è già stato aggiornato per non usare più questo campo, ma Unity potrebbe ancora mostrare il riferimento nel prefab/scene.

---

### **3. VERIFICA PREFAB UI** ⚠️

**File:** Prefab di PotHUDWidget e PotDetailsWidget (se esistono)

**Azione:**
1. Se esistono prefab per questi widget, aprili
2. Verifica che non ci siano riferimenti a `WateringMinigame`
3. Se ci sono, rimuovili

---

### **4. TEST FUNZIONALITÀ** 🧪

**Test da eseguire:**

#### **Test 1: Toggle Sistema Irrigazione**
1. Avvia il gioco in Play Mode
2. Seleziona un vaso con pianta
3. Clicca sul bottone "Irrigazione OFF" (o "Annaffiare")
4. **Verifica:** Il bottone cambia a "Irrigazione ON"
5. Clicca di nuovo
6. **Verifica:** Il bottone cambia a "Irrigazione OFF"

#### **Test 2: Consumo Risorse a Fine Giornata**
1. Attiva sistema irrigazione su 1 vaso (ON)
2. Avvia End Day (o aspetta fine giornata)
3. **Verifica:** 
   - Idratazione aumenta di +1
   - Se accumulatore >= 1.0, WAT-RAW consumato (1 ogni 2 giorni)
   - CRY consumato (2 per vaso ON)
4. Controlla Console per log: `[DayCycleController] Sistema ON - ...`

#### **Test 3: Fallback Automatico**
1. Attiva sistema irrigazione su 4 vasi (ON)
2. Rimuovi tutto WAT-RAW dall'inventario
3. Avvia End Day
4. **Verifica:**
   - Warning toast: "⚠️ WAT-RAW insufficiente. X sistemi irrigazione verranno disattivati"
   - Sistemi si disattivano automaticamente
   - Log: "Sistema disattivato: WAT-RAW insufficiente"

#### **Test 4: Overwatering Detection**
1. Attiva sistema irrigazione su vaso con idratazione già alta (3/4)
2. Avvia End Day
3. **Verifica:**
   - Idratazione aumenta a 4/4
   - pH drift -5 applicato (overwatering)
   - Log: "OVERWATERING rilevato! pH -5 applicato"

#### **Test 5: Evaporazione**
1. Disattiva sistema irrigazione (OFF)
2. Avvia End Day
3. **Verifica:**
   - Idratazione diminuisce di -1 (evaporazione)
   - Nessun consumo risorse
   - Log: "Sistema OFF - Evaporazione applicata"

#### **Test 6: Save/Load**
1. Attiva sistema irrigazione su alcuni vasi
2. Salva il gioco
3. Carica il salvataggio
4. **Verifica:**
   - Stato sistema irrigazione (ON/OFF) viene ripristinato correttamente
   - Accumulatore WAT-RAW viene ripristinato
   - Giorni ON vengono ripristinati

#### **Test 7: Migration Salvataggi Vecchi**
1. Carica un salvataggio creato PRIMA di questa modifica
2. **Verifica:**
   - Il gioco non crasha
   - Tutti i vasi hanno sistema irrigazione OFF (default)
   - Nessun errore in console

---

### **5. VERIFICA UI BUTTONS** 🎨

**File:** `PotHUDWidget` e `PotDetailsWidget`

**Azione:**
1. In Play Mode, verifica che i bottoni mostrino:
   - "Irrigazione ON" quando sistema è attivo
   - "Irrigazione OFF" quando sistema è disattivo
2. Verifica che il colore/icona del bottone cambi in base allo stato (se implementato)

**Nota:** Il testo del bottone è gestito automaticamente dal codice, ma potresti voler aggiungere icone/colori diversi per ON/OFF.

---

### **6. VERIFICA CONSOLE LOGS** 📝

**Azione:**
1. In Play Mode, attiva/disattiva sistema irrigazione
2. Controlla Console per log:
   - `[ACT-002] Watering System Toggle: ON/OFF`
   - `[DayCycleController] Sistema ON/OFF - ...`
3. Verifica che non ci siano errori o warning

---

### **7. VERIFICA BILANCIAMENTO RISORSE** ⚖️

**Test:**
1. Attiva sistema irrigazione su 4 vasi (ON)
2. Raccogli condensazione ogni giorno (3 WAT-RAW/giorno)
3. Gioca per 5-10 giorni
4. **Verifica:**
   - WAT-RAW non va mai in negativo
   - Surplus WAT-RAW aumenta gradualmente (+2/giorno con 4 vasi ON)
   - Se dimentichi di raccogliere, fallback automatico previene deficit

---

### **8. DOCUMENTAZIONE FINALE** 📚

**Azione:**
1. Aggiorna documentazione di design se necessario
2. Aggiorna changelog/versioni
3. Comunica al team le modifiche al sistema watering

---

## ⚠️ PROBLEMI NOTI E SOLUZIONI

### **Problema: Bottone mostra sempre "Annaffiare" invece di "Irrigazione ON/OFF"**

**Causa:** Il metodo `UpdateActionButtons()` potrebbe non essere chiamato dopo toggle.

**Soluzione:** Verifica che `PotEvents.EmitChanged()` sia chiamato dopo toggle (già implementato).

---

### **Problema: Sistema non si disattiva automaticamente quando WAT-RAW insufficiente**

**Causa:** `GameManager` non disponibile in `DayCycleController`.

**Soluzione:** Verifica che `ServiceContainer` registri correttamente `GameManager` prima di `DayCycleController`.

---

### **Problema: Salvataggi vecchi causano errori**

**Causa:** Campi nuovi non presenti in salvataggi vecchi.

**Soluzione:** I default nella classe `PotStateData` gestiscono automaticamente la migration. Se ci sono errori, verifica che i default siano corretti.

---

## ✅ CHECKLIST FINALE

- [ ] CondensationConfig.asset: `CondensationGrowthPerDay = 3`
- [ ] Scene: Riferimenti WateringMinigame rimossi
- [ ] Prefab: Riferimenti WateringMinigame rimossi
- [ ] Test 1: Toggle funziona correttamente
- [ ] Test 2: Consumo risorse a fine giornata
- [ ] Test 3: Fallback automatico
- [ ] Test 4: Overwatering detection
- [ ] Test 5: Evaporazione
- [ ] Test 6: Save/Load
- [ ] Test 7: Migration salvataggi vecchi
- [ ] UI: Bottoni mostrano stato ON/OFF
- [ ] Console: Nessun errore o warning
- [ ] Bilanciamento: Risorse non vanno in negativo

---

## 📝 NOTE FINALI

- Il sistema `LastWateredDay` è ancora presente nel codice per compatibilità, ma non è più usato per la crescita (ora usa `WateringSystemOn`)
- Il minigioco `WateringMinigame` è stato rimosso completamente
- Tutti i tool debug sono stati aggiornati per mostrare i nuovi campi
- Il sistema è retrocompatibile con salvataggi vecchi (migration automatica)

---

**Implementazione completata:** 2025-12-09  
**Pronto per testing e deploy**

