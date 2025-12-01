# 🧪 GUIDA TEST COMPLETA - BLK-02.03: Integrazione pH con Azioni Giocatore

**Versione:** 1.0  
**Data:** 2025-11-26  
**BLK Code:** BLK-02.03  
**Status:** ✅ IMPLEMENTATO - Pronto per Testing

---

## 📋 PREREQUISITI

Prima di iniziare i test, verifica che:

- ✅ Unity Editor aperto con progetto Sporae Build Beta
- ✅ Scena `SCN_VaultMap` caricata
- ✅ PhSystem registrato nel ServiceContainer (verifica con PhSystemDebugConsole - tasto Z)
- ✅ HUD pH Display visibile in-game (top center)
- ✅ Almeno 1 vaso presente nella scena Dome
- ✅ Inventario giocatore contiene:
  - Almeno 1 seme (seed-001, seed-002, o seed-003)
  - Almeno 5 unità di acqua (Items.Water)
  - CRY sufficienti (almeno 50 CRY)
  - Azioni disponibili (almeno 4 azioni)

---

## 🔧 SETUP INIZIALE

### Step 1: Verifica PhSystem Attivo

1. **Apri Console Debug pH:**
   - Premi **Z** durante Play Mode
   - Verifica che la console si apra correttamente

2. **Verifica pH Iniziale:**
   - Console dovrebbe mostrare: `pH: 0.0 (Neutrale)`
   - HUD pH dovrebbe mostrare: `pH: 0.0` con colore verde

3. **Verifica Tooltip HUD:**
   - Passa mouse sopra HUD pH
   - Tooltip dovrebbe mostrare: `pBase: 0,0` e `(Nessun contributo attivo)`

### Step 2: Prepara Vaso per Test

1. **Seleziona un vaso vuoto** nella Dome
2. **Piantare un seme:**
   - Clicca pulsante "Plant" sul vaso
   - Seleziona un seme dal selettore (es. seed-001)
   - Verifica che il seme sia piantato correttamente

3. **Verifica stato vaso:**
   - Vaso dovrebbe mostrare: Stage = Seed (1)
   - Idratazione = 0
   - Light Exposure = 0

---

## 🧪 TEST CASE 1: OVERWATERING DETECTION → pH -5

### Obiettivo
Verificare che annaffiare una pianta già idratata al massimo causi overwatering e applicare pH -5.

### Procedura

1. **Prepara pianta idratata:**
   ```
   - Piantare seme se non già fatto
   - Annaffiare pianta 3 volte (idratazione 0 → 1 → 2 → 3)
   - Verifica idratazione = 3/3 (massimo)
   ```

2. **Esegui Overwatering:**
   - Clicca pulsante "Water" sul vaso (idratazione già al massimo)
   - Verifica che l'azione venga eseguita (consuma azioni/CRY)

3. **Verifica pH Modificato:**
   - **HUD pH:** Dovrebbe mostrare `pH: -5.0` (o valore negativo)
   - **Console Debug (Z):** Dovrebbe mostrare `pH: -5.0`
   - **Tooltip HUD:** Passa mouse sopra HUD pH
     - Dovrebbe mostrare: `Azioni: -5,0` in colore cyan

4. **Verifica Log Console:**
   ```
   [ACT-002][POT-XXX] Overwatering rilevato! pH -5 applicato
   [ACT-002][POT-XXX] Water OK: hydration=3/3, timestamp aggiornato (OVERWATERING - pH -5)
   ```

### Risultati Attesi

- ✅ pH diminuisce di -5 quando si verifica overwatering
- ✅ Tooltip mostra contributo "Azioni: -5,0"
- ✅ Log console mostra messaggio overwatering
- ✅ Idratazione rimane al massimo (3/3)

### Troubleshooting

**Problema:** pH non cambia dopo overwatering
- **Causa:** PhSystem non trovato nel ServiceContainer
- **Soluzione:** Verifica che PhSystemDebugConsole sia presente nella scena e registrato

**Problema:** Overwatering non rilevato
- **Causa:** Idratazione non al massimo
- **Soluzione:** Annaffia pianta fino a idratazione 3/3 prima di testare

---

## 🧪 TEST CASE 2: BLUE LED → pH +5

### Obiettivo
Verificare che utilizzare Blue LED aumenti il pH di +5.

### Procedura

1. **Prepara pianta:**
   ```
   - Piantare seme se non già fatto
   - Verifica che pianta abbia almeno Stage = Seed (1)
   ```

2. **Reset pH (opzionale):**
   - Se pH è già modificato, usa Console Debug (Z) per resettare a 0
   - Oppure usa comando "Set pH" → 0

3. **Esegui Blue LED:**
   - **Metodo 1 (Codice):** Modifica temporaneamente `PotHUDWidget.cs` o `PotDetailsWidget.cs`:
     ```csharp
     selectedPot.PotActions.DoLight(LedType.Blue);
     ```
   - **Metodo 2 (Console Debug):** Usa Console Debug per simulare Blue LED
   - **Metodo 3 (Default):** Chiama `DoLight()` senza parametri (default = Blue)

4. **Verifica pH Modificato:**
   - **HUD pH:** Dovrebbe mostrare `pH: +5.0` (o valore positivo)
   - **Console Debug (Z):** Dovrebbe mostrare `pH: 5.0`
   - **Tooltip HUD:** Passa mouse sopra HUD pH
     - Dovrebbe mostrare: `Azioni: +5,0` in colore cyan

5. **Verifica Log Console:**
   ```
   [ACT-003][POT-XXX] Blue LED utilizzato: pH +5
   [ACT-003][POT-XXX] Light OK: light=1/3, timestamp aggiornato (Blue LED, pH +5)
   ```

6. **Verifica Tracking LED:**
   - Verifica che `PotStateModel.LastLedType` sia `LedType.Blue`
   - (Puoi verificare con debugger o log aggiuntivo)

### Risultati Attesi

- ✅ pH aumenta di +5 quando si usa Blue LED
- ✅ Tooltip mostra contributo "Azioni: +5,0"
- ✅ Log console mostra "Blue LED utilizzato"
- ✅ Light Exposure aumenta di 1
- ✅ LastLedType è Blue

### Troubleshooting

**Problema:** pH non cambia dopo Blue LED
- **Causa:** PhSystem non trovato
- **Soluzione:** Verifica late binding PhSystem in PotActions

**Problema:** Default LED non è Blue
- **Causa:** Parametro ledType passato esplicitamente
- **Soluzione:** Chiama `DoLight()` senza parametri per default Blue

---

## 🧪 TEST CASE 3: RED LED → pH -5

### Obiettivo
Verificare che utilizzare Red LED diminuisca il pH di -5.

### Procedura

1. **Prepara pianta:**
   ```
   - Piantare seme se non già fatto
   - Verifica che pianta abbia almeno Stage = Seed (1)
   ```

2. **Reset pH:**
   - Usa Console Debug (Z) per resettare pH a 0
   - Oppure usa comando "Set pH" → 0

3. **Esegui Red LED:**
   - **Metodo 1 (Codice):** Modifica temporaneamente widget UI:
     ```csharp
     selectedPot.PotActions.DoLight(LedType.Red);
     ```
   - **Metodo 2 (Console Debug):** Usa Console Debug per simulare Red LED

4. **Verifica pH Modificato:**
   - **HUD pH:** Dovrebbe mostrare `pH: -5.0` (o valore negativo)
   - **Console Debug (Z):** Dovrebbe mostrare `pH: -5.0`
   - **Tooltip HUD:** Passa mouse sopra HUD pH
     - Dovrebbe mostrare: `Azioni: -5,0` in colore cyan

5. **Verifica Log Console:**
   ```
   [ACT-003][POT-XXX] Red LED utilizzato: pH -5
   [ACT-003][POT-XXX] Light OK: light=1/3, timestamp aggiornato (Red LED, pH -5)
   ```

6. **Verifica Tracking LED:**
   - Verifica che `PotStateModel.LastLedType` sia `LedType.Red`

### Risultati Attesi

- ✅ pH diminuisce di -5 quando si usa Red LED
- ✅ Tooltip mostra contributo "Azioni: -5,0"
- ✅ Log console mostra "Red LED utilizzato"
- ✅ Light Exposure aumenta di 1
- ✅ LastLedType è Red

---

## 🧪 TEST CASE 4: SPRAY ANTIFUNGINO → pH +5

### Obiettivo
Verificare che utilizzare Spray Antifungino aumenti il pH di +5.

### Procedura

1. **Prepara pianta:**
   ```
   - Piantare seme se non già fatto
   - Verifica che pianta abbia almeno Stage = Seed (1)
   ```

2. **Reset pH:**
   - Usa Console Debug (Z) per resettare pH a 0

3. **Esegui Spray Antifungino:**
   - **Metodo 1 (Codice):** Aggiungi temporaneamente pulsante UI o chiama direttamente:
     ```csharp
     selectedPot.PotActions.DoSprayAntifungal();
     ```
   - **Metodo 2 (Console Debug):** Usa Console Debug per simulare Spray

4. **Verifica pH Modificato:**
   - **HUD pH:** Dovrebbe mostrare `pH: +5.0` (o valore positivo)
   - **Console Debug (Z):** Dovrebbe mostrare `pH: 5.0`
   - **Tooltip HUD:** Passa mouse sopra HUD pH
     - Dovrebbe mostrare: `Azioni: +5,0` in colore cyan

5. **Verifica Log Console:**
   ```
   [ACT-014][POT-XXX] Spray Antifungino applicato: pH +5
   [ACT-014][POT-XXX] Spray Antifungino OK: muffe rimosse (se presenti), pH +5 applicato
   ```

### Risultati Attesi

- ✅ pH aumenta di +5 quando si usa Spray Antifungino
- ✅ Tooltip mostra contributo "Azioni: +5,0"
- ✅ Log console mostra "Spray Antifungino applicato"
- ✅ Azione consuma risorse (azioni/CRY)

### Troubleshooting

**Problema:** Spray Antifungino non disponibile
- **Causa:** UI non ancora implementata per questa azione
- **Soluzione:** Usa chiamata diretta codice o Console Debug per testare

---

## 🧪 TEST CASE 5: SEQUENZA COMBINATA

### Obiettivo
Verificare che multiple azioni modifichino correttamente il pH in sequenza.

### Procedura

1. **Reset pH a 0:**
   - Usa Console Debug (Z) → "Set pH" → 0

2. **Esegui sequenza azioni:**
   ```
   Step 1: Blue LED → pH dovrebbe essere +5
   Step 2: Red LED → pH dovrebbe essere 0 (+5 -5 = 0)
   Step 3: Overwatering → pH dovrebbe essere -5
   Step 4: Spray Antifungino → pH dovrebbe essere 0 (-5 +5 = 0)
   ```

3. **Verifica pH dopo ogni step:**
   - Dopo Blue LED: `pH: +5.0`
   - Dopo Red LED: `pH: 0.0`
   - Dopo Overwatering: `pH: -5.0`
   - Dopo Spray: `pH: 0.0`

4. **Verifica Tooltip Finale:**
   - Passa mouse sopra HUD pH
   - Tooltip dovrebbe mostrare: `Azioni: 0,0` (contributi si annullano)
   - Oppure mostra contributi netti se non si annullano perfettamente

### Risultati Attesi

- ✅ pH si modifica correttamente dopo ogni azione
- ✅ Contributi si sommano/sottraggono correttamente
- ✅ Tooltip mostra contributi corretti

---

## 🧪 TEST CASE 6: RETROCOMPATIBILITÀ

### Obiettivo
Verificare che chiamate esistenti a `DoLight()` senza parametri funzionino ancora.

### Procedura

1. **Reset pH a 0**

2. **Chiama DoLight() senza parametri:**
   ```csharp
   selectedPot.PotActions.DoLight(); // Nessun parametro
   ```

3. **Verifica comportamento default:**
   - pH dovrebbe aumentare di +5 (default = Blue LED)
   - Log dovrebbe mostrare "Blue LED utilizzato"
   - LastLedType dovrebbe essere Blue

### Risultati Attesi

- ✅ `DoLight()` senza parametri usa Blue LED di default
- ✅ Nessun breaking change su codice esistente
- ✅ UI esistente continua a funzionare

---

## 🧪 TEST CASE 7: VERIFICA TOOLTIP HUD pH

### Obiettivo
Verificare che il tooltip HUD pH mostri correttamente i contributi delle azioni.

### Procedura

1. **Esegui azioni che modificano pH:**
   ```
   - Blue LED → +5
   - Red LED → -5
   - Overwatering → -5
   - Spray Antifungino → +5
   ```

2. **Verifica Tooltip:**
   - Passa mouse sopra HUD pH
   - Tooltip dovrebbe mostrare:
     ```
     pH Calculation:
     pBase: 0,0
     Azioni: -5,0  (in colore cyan)
     Total: -5.0
     ```

3. **Verifica Formato:**
   - Numeri con virgole (formato italiano)
   - Colore cyan per "Azioni"
   - Solo contributi significativi (>0.01) mostrati

### Risultati Attesi

- ✅ Tooltip mostra contributi azioni corretti
- ✅ Formato italiano con virgole
- ✅ Colori corretti per categoria
- ✅ Solo contributi significativi mostrati

---

## 🧪 TEST CASE 8: EDGE CASES

### Test 8.1: PhSystem Non Disponibile

**Procedura:**
1. Rimuovi temporaneamente PhSystem dal ServiceContainer
2. Esegui azioni (Water, Light, Spray)
3. Verifica che azioni funzionino comunque (senza modificare pH)

**Risultati Attesi:**
- ✅ Azioni funzionano anche senza PhSystem
- ✅ Nessun crash o errore
- ✅ Log mostra warning se PhSystem non trovato

### Test 8.2: Multiple Overwatering

**Procedura:**
1. Annaffia pianta fino a idratazione massima
2. Esegui overwatering multipli (3-4 volte)
3. Verifica che ogni overwatering applichi pH -5

**Risultati Attesi:**
- ✅ Ogni overwatering applica pH -5
- ✅ pH totale diminuisce correttamente (-5, -10, -15, -20)
- ✅ Tooltip mostra contributi cumulativi

### Test 8.3: Azioni Multiple Stesso Giorno

**Procedura:**
1. Esegui Blue LED 3 volte nello stesso giorno
2. Verifica che ogni LED applichi pH +5
3. Verifica che Light Exposure non superi il massimo

**Risultati Attesi:**
- ✅ Ogni LED applica pH +5
- ✅ Light Exposure rispetta limite massimo (3/3)
- ✅ pH totale aumenta correttamente (+5, +10, +15)

---

## 📊 CHECKLIST TEST COMPLETI

Prima di considerare i test completati, verifica:

- [ ] **Test 1:** Overwatering → pH -5 ✅
- [ ] **Test 2:** Blue LED → pH +5 ✅
- [ ] **Test 3:** Red LED → pH -5 ✅
- [ ] **Test 4:** Spray Antifungino → pH +5 ✅
- [ ] **Test 5:** Sequenza combinata ✅
- [ ] **Test 6:** Retrocompatibilità ✅
- [ ] **Test 7:** Tooltip HUD pH ✅
- [ ] **Test 8:** Edge cases ✅

---

## 🐛 TROUBLESHOOTING GENERALE

### Problema: PhSystem Non Trovato

**Sintomi:**
- Log mostra: `[PotActions] PhSystem non trovato`
- pH non si modifica dopo azioni

**Soluzioni:**
1. Verifica che PhSystemDebugConsole sia presente nella scena
2. Verifica che PhSystem sia registrato nel ServiceContainer
3. Usa Console Debug (Z) per verificare registrazione
4. Aggiungi PhSystem manualmente se necessario:
   ```csharp
   var phSystem = new PhSystem();
   ServiceContainer.Instance.Register(phSystem);
   ```

### Problema: Tooltip Non Mostra Contributi Azioni

**Sintomi:**
- Tooltip mostra solo "pBase: 0,0"
- Contributi azioni non visibili

**Soluzioni:**
1. Verifica che azioni siano state eseguite correttamente
2. Verifica che PhSystem tracci contributi correttamente
3. Controlla che contributi siano > 0.01 (soglia minimo)
4. Verifica formato tooltip in `PhSystem.GetCalculationBreakdown()`

### Problema: Azioni Non Modificano pH

**Sintomi:**
- Azioni funzionano ma pH non cambia
- Log non mostra messaggi pH

**Soluzioni:**
1. Verifica che `_phSystem` non sia null in PotActions
2. Verifica late binding PhSystem funzioni correttamente
3. Controlla log per errori ServiceContainer
4. Verifica che `RegisterActionDrift()` sia chiamato correttamente

---

## 📝 NOTE FINALI

### Comportamento Atteso

- **Overwatering:** Rilevato quando idratazione >= maxHydration - 1
- **Blue LED:** Default quando `DoLight()` chiamato senza parametri
- **Red LED:** Richiede parametro esplicito `LedType.Red`
- **Spray Antifungino:** Azione separata, non ancora disponibile in UI

### Limitazioni Attuali

- ⚠️ UI selezione LED tipo non ancora implementata (default Blue)
- ⚠️ Rimozione muffe in Spray non ancora implementata (solo pH)
- ⚠️ Overwatering detection basico (può essere raffinato con soglie configurabili)

### Prossimi Passi Dopo Test

1. Implementare UI selezione LED tipo (Blue/Red) per stadi completi
2. Integrare rimozione muffe in Spray quando sistema muffe disponibile
3. Aggiungere soglie configurabili per overwatering detection

---

## ✅ CRITERI DI ACCETTAZIONE

Il sistema è considerato **funzionante** quando:

- ✅ Overwatering applica pH -5 correttamente
- ✅ Blue LED applica pH +5 correttamente
- ✅ Red LED applica pH -5 correttamente
- ✅ Spray Antifungino applica pH +5 correttamente
- ✅ Tooltip HUD mostra contributi azioni corretti
- ✅ Nessun breaking change su codice esistente
- ✅ Log console mostra messaggi corretti
- ✅ Edge cases gestiti correttamente

---

**Fine Guida Test**

