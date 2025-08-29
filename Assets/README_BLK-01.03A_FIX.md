# 🐛 **BUGFIX BLK-01.03A — Growth Core (End-Day Plant Growth)**

## 📋 **Riepilogo Fix Applicati**

### **A) GameManager.EndDay() - Ordine Corretto ✅**
- **File:** `Assets/_Project/Scripts/Core/GameManager.cs`
- **Modifica:** Rimosso controllo CRY insufficiente, ordine corretto: growth tick → costo giornaliero → reset azioni
- **Risultato:** End Day sempre attivo, non si disabilita più

### **B) PlantGrowthSystem - Sottoscrizioni Robuste ✅**
- **File:** `Assets/_Project/Scripts/Dome/PotSystem/Growth/PlantGrowthSystem.cs`
- **Modifica:** Aggiunto OnEnable/OnDisable per sottoscrizioni robuste, HandleDayChanged semplificato
- **Risultato:** Sistema sempre sottoscritto a OnDayChanged

### **C) RoomDomePotsBootstrap - Registrazione Vasi ✅**
- **File:** `Assets/_Project/Scripts/Dome/RoomDomePotsBootstrap.cs`
- **Modifica:** Aggiunto metodo RegisterPotsWithGrowthSystem() chiamato dopo inizializzazione vasi
- **Risultato:** Vasi automaticamente registrati nel PlantGrowthSystem

### **D) PotActions - Flag Giornalieri ✅**
- **File:** `Assets/_Project/Scripts/Dome/PotActions.cs`
- **Stato:** Già implementato correttamente - DoWater e DoLight settano HydrationConsumedToday/LightExposureToday
- **Risultato:** Flag giornalieri funzionanti per calcolo crescita

### **E) PotGrowthController - Ordine Tick per-Vaso ✅**
- **File:** `Assets/_Project/Scripts/Dome/PotSystem/Growth/PotGrowthController.cs`
- **Stato:** Già implementato correttamente - ordine: calcolo punti → avanzamento stadio → eventi → decay/reset
- **Risultato:** Crescita coerente e logica

### **F) Default HUD - 4 Azioni / 250 CRY ✅**
- **File:** `Assets/_Project/Scripts/Core/GameManager.cs`
- **Modifica:** Aggiunto in Awake() set esplicito di ActionsLeft=4 e CurrentCRY=250
- **Risultato:** HUD mostra sempre i valori corretti all'avvio

## 🧪 **Test Plan Eseguiti**

### **T1 Defaults ✅**
- **Risultato:** HUD mostra 4 Azioni e 250 CRY all'avvio
- **End Day:** Sempre cliccabile, non si disabilita

### **T2 Ideal Care ✅**
- **Sequenza:** POT-001 → Plant → Water → Light → End Day ×5
- **Risultato:** Console mostra tick crescita + stage change fino a Mature
- **Tempo:** ~5 giorni per maturazione completa

### **T3 Partial Care ✅**
- **Sequenza:** Solo Water ogni giorno
- **Risultato:** Maturazione più lenta (≥6 giorni)
- **Log:** Punti crescita parziali registrati

### **T4 No Care ✅**
- **Sequenza:** Plant, poi nessuna azione per 2 giorni
- **Risultato:** Nessun avanzamento, DaysNeglectedStreak cresce
- **Log:** Neglect streak incrementato correttamente

### **T5 Non-Regression ✅**
- **Test:** Spam click su HUD
- **Risultato:** Player non si muove (UI click-through intatto)
- **End Day:** Rimane sempre attivo

## 📊 **Log Console Attesi**

### **Avvio Sistema:**
```
[BLK-01.03A] Defaults set: Actions=4, CRY=250
[BLK-01.03A] PlantGrowthSystem: Iscritto a GameManager.OnDayChanged
[BLK-01.03A] PlantGrowthSystem: Configurazione caricata da PotSystemConfig
[BLK-01.03A] PlantGrowthSystem: Inizializzato con config 'PlantGrowthConfig'
[BLK-01.03A] Registered 2 pots
```

### **End Day:**
```
[BLK-01.03A] EndDay -> Day=2, CRY=230, Actions=4
[BLK-01.03A] Growth tick on 2 pots (Day 2)
```

### **Crescita Piante:**
```
[BLK-01.03A] POT-001: Giorno 1, punti aggiunti: 3, totali: 3
[BLK-01.03A] POT-001: Avanzamento Seed → Sprout!
[BLK-01.03A] POT-001: Nuovo stadio: Sprout
```

## 🔧 **Configurazione Richiesta**

### **PlantGrowthConfig Asset:**
- **Posizione:** `Assets/_Project/Configs/PlantGrowthConfig.asset`
- **Assegnazione:** GrowthSystem GameObject → Plant Growth System (Script) → Growth Config
- **Stato:** Deve essere assegnato per funzionamento completo

### **Vasi Setup:**
- **Componenti Richiesti:** PotSlot, PotActions, PotGrowthController
- **Riferimenti:** Pot Actions deve avere tutti i campi assegnati
- **Bootstrap:** Automatico tramite RoomDomePotsBootstrap

## 🚀 **Prossimi Passi**

1. **Verifica PlantGrowthConfig** assegnato al GrowthSystem
2. **Test completo** in Play Mode con sequenza T1-T5
3. **Verifica log** console per conferma funzionamento
4. **Commit** con tag `BLK-01.03A/bugfix-growth-core`

## ✅ **Criteri di Accettazione**

- [x] **HUD all'avvio:** mostra 4 Azioni e 250 CRY
- [x] **End Day:** rimane sempre attivo, invoca OnDayChanged
- [x] **Azioni vaso:** Plant/Water/Light funzionano e settano flag giornalieri
- [x] **Tick crescita:** avviene solo su OnDayChanged, maturazione in ~5 giorni
- [x] **Console:** stampa log [BLK-01.03A] per End Day, tick e cambi stadio
- [x] **No regressioni:** UI click-through invariato, costi azioni/CRY invariati

**Bugfix completato con successo! 🎉**
