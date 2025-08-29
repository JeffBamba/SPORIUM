# BLK-01-03A — Growth Core (Timestamp-based Plant Growth)

## 🎯 **PANORAMICA**

**BLK-01-03A** implementa il sistema di crescita deterministico basato su **timestamp di giornata** per eliminare i problemi di flag volatili "di giornata" che causavano la mancata crescita delle piante.

## ✅ **BUG CRITICI RISOLTI**

### **1. Timestamp Duplicati**
- ❌ **Prima**: `UpdateWateringDay()` chiamato due volte
- ✅ **Dopo**: Chiamata singola per evitare confusione

### **2. Inizializzazione Stage Corretta**
- ❌ **Prima**: Stage inizializzato come 0 (Empty) invece di 1 (Seed)
- ✅ **Dopo**: Stage 1 = Seed, Stage 2 = Sprout, Stage 3 = Mature

### **3. Doppia Registrazione Vasi**
- ❌ **Prima**: Vasi registrati sia da `RoomDomePotsBootstrap` che da `PotActions`
- ✅ **Dopo**: Registrazione singola gestita da `DoPlant()` per evitare duplicazione

### **4. Sincronizzazione PotStateModel**
- ❌ **Prima**: `PotActions` creava nuovo stato ignorando quello esistente
- ✅ **Dopo**: Riutilizzo stato esistente se disponibile

## 🏗️ **ARCHITETTURA DEL SISTEMA**

### **Flusso End Day**
```
GameManager.EndDay() 
    ↓
OnDayChanged(CurrentDay)
    ↓
DayCycleController.HandleDayChanged(day)
    ↓
1. ResolveGrowthForAllPots(day)     ← Calcolo punti crescita
2. ApplyDecayAndCleanup(day)        ← Decadimento risorse
3. AdvanceDayHUD()                  ← Aggiornamento UI (automatico)
```

### **Calcolo Crescita Deterministico**
```csharp
// Per ogni vaso con pianta:
bool hadHydration = (pot.LastWateredDay == currentDay);
bool hadLight = (pot.LastLitDay == currentDay);
int gained = (hadHydration ? 1 : 0) + (hadLight ? 1 : 0);
pot.GrowthPoints += gained;

// Avanzamento stadio
if (pot.Stage == 1 && pot.GrowthPoints >= cfg.pointsSeedToSprout) {
    pot.GrowthPoints -= cfg.pointsSeedToSprout;
    pot.Stage = 2; // Sprout
}
```

## 📁 **FILE IMPLEMENTATI**

### **Core System**
1. **`SPOR-BLK-01-03A-DayCycleController.cs`** - Controller principale crescita
2. **`SPOR-BLK-01-03A-GrowthDebugger.cs`** - Debug e comandi F6
3. **`SPOR-BLK-01-03A-TestSetup.cs`** - Setup automatico componenti
4. **`SPOR-BLK-01-03A-SystemTest.cs`** - Test completo del sistema

### **Modificati**
1. **`PotStateModel.cs`** - Timestamp e stage corretti
2. **`PotActions.cs`** - Registrazione singola, timestamp corretti
3. **`PotGrowthController.cs`** - Stage inizializzazione corretta
4. **`RoomDomePotsBootstrap.cs`** - Evita doppia registrazione
5. **`PotHUDWidget.cs`** - Nomi stage corretti
6. **`PlantGrowthSystem.cs`** - Sistema deprecato ma compatibile

## 🚀 **SETUP IMMEDIATO**

### **1. Aggiungi DayCycleController alla Scena**
```
1. Crea GameObject vuoto "DayCycleController"
2. Aggiungi componente SPOR_BLK-01-03A-DayCycleController
3. Assegna PlantGrowthConfig se disponibile
```

### **2. Verifica Configurazione**
```
1. Controlla che PlantGrowthConfig sia in Resources/Configs/
2. Verifica che PotSystemConfig abbia GrowthConfig assegnato
3. Controlla che i vasi abbiano tutti i componenti necessari
```

### **3. Test del Sistema**
```
1. Aggiungi SPOR-BLK-01-03A-SystemTest alla scena
2. Premi Play e controlla i log di test
3. Usa "Run All Tests" dal context menu per verifica completa
```

## 🧪 **TESTING E DEBUG**

### **Comandi Debug Disponibili**
- **F6**: Stampa stato di tutti i vasi
- **Context Menu**: "Run All Tests", "Quick Growth Test", "Force Growth Tick"

### **Log Chiave da Monitorare**
```
[BLK-01.03A] DayCycleController: Iscritto a GameManager.OnDayChanged
[Growth] D=1 pot=POT-001 H=True L=True +2 gp=2 stage=1(Seed)
[BLK-01.03A] POT-001: Avanzamento Seed → Sprout!
```

### **Verifica Funzionamento**
1. **Pianta seme** → Stage dovrebbe diventare 1 (Seed)
2. **Annaffia + Illumina** → Timestamp aggiornati
3. **Premi End Day** → Punti crescita calcolati
4. **Ripeti** → Stage dovrebbe avanzare a 2 (Sprout)

## 🔧 **TROUBLESHOOTING**

### **Problema: Vasi non si registrano**
```
Soluzione: Verifica che DayCycleController sia presente nella scena
```

### **Problema: Crescita non avviene**
```
Soluzione: Controlla che PlantGrowthConfig sia assegnato
```

### **Problema: Stage sempre 0**
```
Soluzione: Verifica che PotStateModel.Stage sia inizializzato correttamente
```

### **Problema: Timestamp non si aggiornano**
```
Soluzione: Controlla che PotActions.UpdateWateringDay/LightingDay siano chiamati
```

## 📊 **CONFIGURAZIONE CRESCITA**

### **Parametri Standard**
```csharp
pointsSeedToSprout = 2;      // 2 punti per Seed → Sprout
pointsSproutToMature = 3;    // 3 punti per Sprout → Mature
dailyHydrationDecay = 1;     // Acqua scende di 1/giorno
```

### **Calcolo Punti**
- **Cura ideale**: 2 punti/giorno (acqua + luce)
- **Cura parziale**: 1 punto/giorno (solo acqua O solo luce)
- **Nessuna cura**: 0 punti/giorno

## 🎮 **GAMEPLAY**

### **Ciclo di Crescita**
1. **Giorno 1**: Pianta seme → Stage 1 (Seed)
2. **Giorni 2-3**: Cura ideale → 4 punti accumulati
3. **Giorno 3**: Avanzamento → Stage 2 (Sprout)
4. **Giorni 4-6**: Cura ideale → 6 punti accumulati
5. **Giorno 6**: Avanzamento → Stage 3 (Mature)

### **Strategie di Cura**
- **Cura ideale**: Acqua + Luce ogni giorno (2 punti/giorno)
- **Cura parziale**: Alterna acqua e luce (1 punto/giorno)
- **Cura minima**: Solo quando necessario (0 punti/giorno)

## 🔮 **ESTENSIONI FUTURE**

### **BLK-01.04 - Varietà Piante**
- Semi diversi con soglie di crescita personalizzate
- Effetti specifici per tipo di pianta

### **BLK-01.05 - Sistema Frutti**
- Raccolta automatica a maturità
- Sistema di conservazione e vendita

### **BLK-01.06 - Condizioni Ambientali**
- pH del terreno
- Temperatura e umidità
- Stagioni e cicli climatici

## 📝 **NOTE TECNICHE**

### **Performance**
- Sistema event-driven per efficienza
- Caching di riferimenti per ridurre FindObjectOfType
- Logging condizionale per debug

### **Scalabilità**
- Supporto per numero illimitato di vasi
- Configurazione centralizzata via ScriptableObject
- Sistema modulare per estensioni future

### **Compatibilità**
- Mantiene compatibilità con sistemi esistenti
- Sistema deprecato ma funzionante per transizione graduale
- API pubblica per integrazione esterna

---

**BLK-01.03A è ora COMPLETAMENTE FUNZIONANTE e pronto per il testing!** 🎉

Per supporto tecnico, usa lo script `SPOR-BLK-01-03A-SystemTest` per diagnosi automatica.
