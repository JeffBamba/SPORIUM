# 🔧 ISTRUZIONI MANUALI: SISTEMA LED PERSISTENTE (BLK-02.07)
## Guida Step-by-Step per Completare l'Implementazione in Unity

**Data:** 2025-01-XX  
**Versione:** 1.0  
**BLK Code:** BLK-02.07  
**Status:** ✅ Implementazione Codice Completata - Istruzioni Manuali Unity

---

## 📋 OVERVIEW

Il sistema LED è stato migrato da **click giornaliero** a **toggle persistente** (Off/Blue/Red). Il codice è stato implementato, ma alcune configurazioni UI devono essere completate manualmente in Unity Editor.

---

## ✅ COSA È STATO FATTO AUTOMATICAMENTE

- ✅ Enum `LedSystemState` creato
- ✅ Campi LED persistenti aggiunti a `PotStateModel`
- ✅ Metodo `DoLight()` riscritto con toggle
- ✅ Calcolo effetti a fine giornata implementato
- ✅ Salvataggio/caricamento con migrazione automatica
- ✅ Aggiornamento UI code (PotDetailsWidget, PotHUDWidget, GrowthDebugHotkeys)
- ✅ Tooltip pH con moltiplicatori

---

## 🎯 COSA DEVI FARE MANUALMENTE IN UNITY

### **STEP 1: Verifica Compilazione (5 minuti)**

1. **Apri Unity Editor**
   - Apri il progetto `Sporae_Build_Beta`
   - Attendi che Unity compili tutti gli script

2. **Controlla Console per Errori**
   - Vai su `Window > General > Console` (o premi `Ctrl+Shift+C`)
   - Verifica che **NON ci siano errori di compilazione**
   - Se ci sono errori, segnalali prima di procedere

3. **Test Rapido**
   - Premi `Play` per avviare la scena
   - Se il gioco si avvia senza crash, procedi

---

### **STEP 2: Configurazione UI PotDetailsWidget (10-15 minuti)**

Il widget dei dettagli del vaso deve essere aggiornato per mostrare lo stato LED corrente.

#### **2.1: Trova PotDetailsWidget nella Scena**

1. **Apri la scena principale** (es. `Assets/_Settings/Scenes/` o la scena che usi)
2. **Cerca PotDetailsWidget** nella Hierarchy:
   - Usa la ricerca in alto: digita "PotDetailsWidget"
   - Oppure naviga manualmente: `Canvas > UI > PotDetailsWidget` (o simile)

3. **Seleziona il GameObject PotDetailsWidget**
   - Dovresti vedere l'Inspector con lo script `PotDetailsWidget`

#### **2.2: Verifica Riferimenti Pulsanti**

1. **Nell'Inspector**, trova la sezione **Script PotDetailsWidget**
2. **Verifica i campi SerializeField**:
   - `_blueLedButton` - dovrebbe essere assegnato a un Button
   - `_redLedButton` - dovrebbe essere assegnato a un Button (opzionale, può essere disabilitato)

3. **Se i pulsanti NON sono assegnati**:
   - Trova i pulsanti LED nella Hierarchy (sotto PotDetailsWidget)
   - **Trascina** i pulsanti dai figli di PotDetailsWidget ai campi nell'Inspector
   - Oppure usa il cerchio a destra del campo per selezionare il pulsante

#### **2.3: Configurazione Pulsante LED (Opzionale - Consigliato)**

**Opzione A: Un Solo Pulsante Toggle (Consigliato)**
- Il pulsante `_blueLedButton` ora funziona come toggle
- Mostra lo stato corrente: "LED OFF", "LED BLUE", "LED RED"
- Il pulsante `_redLedButton` viene automaticamente disabilitato dal codice

**Opzione B: Mantenere Due Pulsanti (Non Consigliato)**
- Se preferisci mantenere due pulsanti separati, devi modificare il codice
- Per ora, entrambi i pulsanti fanno toggle (ciclo: Off → Blue → Red → Off)

#### **2.4: Test UI**

1. **Salva la scena**: `Ctrl+S`
2. **Premi Play**
3. **Seleziona un vaso** con una pianta
4. **Clicca il pulsante LED**:
   - Dovrebbe cambiare da "LED OFF" → "LED BLUE" → "LED RED" → "LED OFF"
   - Verifica che il testo del pulsante si aggiorni correttamente

---

### **STEP 3: Verifica Toast Messages (5 minuti)**

I toast messages sono già implementati nel codice, ma devi verificare che il sistema UI li mostri.

1. **Trova UINotification o Toast System** nella scena
   - Cerca "Toast", "Notification", o "UINotification" nella Hierarchy
   - Se non esiste, potrebbe essere gestito da un altro sistema UI

2. **Test Toast**:
   - Avvia il gioco
   - Attiva LED su un vaso
   - Dovresti vedere un toast: "LGT-001: Luce BLUE attiva (POT-001)"
   - Se non vedi toast, verifica che `PotEvents.EmitToast()` sia collegato al sistema UI

3. **Se i toast non funzionano**:
   - Controlla `PotEvents.cs` per vedere come vengono gestiti
   - Potrebbe essere necessario collegare manualmente il sistema toast

---

### **STEP 4: Verifica Salvataggio/Caricamento (10 minuti)**

Il sistema di salvataggio è stato aggiornato con migrazione automatica, ma devi testarlo.

1. **Crea un Salvataggio di Test**:
   - Avvia il gioco
   - Attiva LED su un vaso (es. Blue)
   - Salva il gioco (usa il sistema di salvataggio esistente)

2. **Carica il Salvataggio**:
   - Esci dal gioco o ricarica la scena
   - Carica il salvataggio
   - Verifica che lo stato LED sia corretto (Blue dovrebbe essere ancora attivo)

3. **Test Migrazione Salvataggi Vecchi**:
   - Se hai salvataggi vecchi (prima di BLK-02.07):
   - Caricali e verifica che funzionino
   - Il sistema dovrebbe migrare automaticamente `LastLedType` a `LedSystemState`

---

### **STEP 5: Verifica Tooltip pH (5 minuti)**

Il tooltip pH dovrebbe mostrare i moltiplicatori LED quando attivi.

1. **Avvia il gioco**
2. **Attiva LED Blue** su un vaso
3. **Passa il mouse sul pH indicator** (dove viene mostrato il pH)
4. **Verifica il tooltip**:
   - Dovrebbe mostrare: "LED Blu: +5,0 (POT-001)" per 1 giorno
   - Dopo 2-3 giorni consecutivi: "LED Blu: +7,5 (×1.5) (POT-001)"
   - Dopo 4+ giorni: "LED Blu: +10,0 (×2) (POT-001)"

5. **Se il tooltip non mostra moltiplicatori**:
   - Verifica che il sistema pH sia collegato correttamente
   - Controlla che `PhSystem.GetCalculationBreakdown()` venga chiamato

---

### **STEP 6: Test Funzionale Completo (15-20 minuti)**

Esegui questi test per verificare che tutto funzioni:

#### **Test 1: Toggle Base**
- [ ] Vaso vuoto → Piantare seme
- [ ] Toggle LED → Stato cambia Off → Blue → Red → Off
- [ ] Consumo: 1 Azione per toggle (non CRY immediato)

#### **Test 2: Effetti Fine Giornata**
- [ ] Attiva Blue LED → End Day → pH aumenta (+5 base)
- [ ] Attiva Red LED → End Day → pH diminuisce (-5 base)
- [ ] Light Exposure aumenta

#### **Test 3: Scaling Cumulativo**
- [ ] Blue LED 1 giorno → pH +5 (x1)
- [ ] Blue LED 2-3 giorni → pH +7.5 (x1.5)
- [ ] Blue LED 4+ giorni → pH +10 (x2)

#### **Test 4: Consumo CRY Notturno**
- [ ] Blue LED giorno 1 → 1 CRY consumato a fine giornata
- [ ] Blue LED giorno 2 → 1 CRY consumato
- [ ] Blue LED giorno 3 → 2 CRY consumato
- [ ] Red LED giorno 1 → 2 CRY consumato
- [ ] Red LED giorno 2 → 3 CRY consumato

#### **Test 5: Requisiti Stage**
- [ ] Pianta in Growth → Richiede Blue LED
- [ ] Attiva Blue LED → Avanza a Flowering (se altri requisiti OK)
- [ ] Attiva Red LED → Non avanza (LED sbagliato)

#### **Test 6: CRY Insufficiente**
- [ ] CRY = 0 → LED attivo → End Day → Sistema spento automaticamente
- [ ] Toast: "LGT-002: Sistema LED POT-001 spento - CRY insufficiente"

#### **Test 7: Zona Rossa**
- [ ] LED 4+ giorni → Toast: "LGT-003: LED Blue attivo 4 giorni - Zona rossa!"

---

## 🐛 TROUBLESHOOTING

### **Problema: Pulsante LED non funziona**

**Soluzione:**
1. Verifica che il pulsante sia assegnato nell'Inspector di PotDetailsWidget
2. Controlla che il metodo `OnLedToggleClicked()` sia collegato al pulsante
3. Verifica che il vaso abbia una pianta (LED funziona solo con piante)

### **Problema: Toast non appaiono**

**Soluzione:**
1. Verifica che `PotEvents.EmitToast()` sia collegato al sistema UI
2. Controlla che il sistema toast sia attivo nella scena
3. Verifica la Console per errori

### **Problema: Salvataggi vecchi non funzionano**

**Soluzione:**
1. Il sistema dovrebbe migrare automaticamente
2. Se non funziona, verifica che `SaveManager.ApplyPotStates()` contenga la logica di migrazione
3. Controlla la Console per errori durante il caricamento

### **Problema: pH non cambia con LED**

**Soluzione:**
1. Verifica che `DayCycleController.ApplyLedSystemEffects()` venga chiamato
2. Controlla che `PhSystem` sia registrato nel ServiceContainer
3. Verifica che End Day venga chiamato correttamente

---

## 📝 NOTE FINALI

### **Compatibilità Temporanea**

- Il metodo `DoLight(LedType?)` è ancora disponibile ma deprecato
- `LastLedType` è mantenuto per compatibilità salvataggi
- Rimozione completa prevista per BLK-02.08

### **Future Implementazioni (BLK-02.08+)**

- Burn Stress system (effetti reali su crescita)
- Mold Risk system
- Visual effects LED (glow, colori dinamici)
- Audio feedback
- Animazioni UI

---

## ✅ CHECKLIST FINALE

Prima di considerare completata l'implementazione:

- [ ] Compilazione senza errori
- [ ] UI PotDetailsWidget configurata correttamente
- [ ] Toast messages funzionanti
- [ ] Salvataggio/caricamento testato
- [ ] Tooltip pH mostra moltiplicatori
- [ ] Test funzionali completati
- [ ] Nessun crash durante il gioco

---

**Status:** 🟢 IMPLEMENTAZIONE COMPLETATA - PRONTA PER TEST

*Documento creato da Senior Developer Mode (AI Assistant)*  
*Data: 2025-01-XX*  
*Versione: 1.0*

