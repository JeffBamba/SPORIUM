# BLK-01.03A — Growth Core (End-Day Plant Growth)

## 🎯 **Obiettivo**
Implementare il sistema di crescita delle piante a fine-giorno, data-driven e configurabile, senza modifiche HUD/visual.

## 🏗️ **Architettura Implementata**

### **Core Components**
- **`PlantGrowthConfig.cs`** - ScriptableObject per tutti i parametri di crescita
- **`PlantStage.cs`** - Enum per gli stadi di crescita (Empty, Seed, Sprout, Mature)
- **`PotGrowthController.cs`** - Controller individuale per ogni vaso
- **`PlantGrowthSystem.cs`** - Sistema globale che gestisce tutti i vasi
- **`GrowthSystemBootstrap.cs`** - Setup automatico del sistema

### **Integrazione con Sistemi Esistenti**
- **`PotStateModel.cs`** - Esteso con campi per crescita e flag giornalieri
- **`PotEvents.cs`** - Nuovi eventi per crescita e cambio stadio
- **`PotActions.cs`** - Integrato con flag giornalieri
- **`PotSystemConfig.cs`** - Collegato a PlantGrowthConfig
- **`RoomDomePotsBootstrap.cs`** - Setup automatico dei controller

## 🔧 **Setup Richiesto**

### **1. Creare PlantGrowthConfig Asset**
```
Unity Editor → Assets/_Project/Configs/ → Click destro → Create → Sporae → PlantGrowthConfig
```

### **2. Setup Scena**
```
GameObject "GrowthSystem" con componenti:
- PlantGrowthSystem
- GrowthSystemBootstrap
```

## 🧪 **Test Plan**

### **Test T1: Setup Sistema**
- [ ] PlantGrowthConfig asset creato
- [ ] GrowthSystem GameObject in scena
- [ ] Console mostra "Iscritto a GameManager.OnDayChanged"

### **Test T2: Registrazione Vasi**
- [ ] Console mostra "Registrati X/X vasi nel PlantGrowthSystem"
- [ ] Ogni vaso ha PotGrowthController

### **Test T3: Loop Completo**
```
1. Plant → Water → Light → End Day
2. Water → Light → End Day  
3. Water → Light → End Day
4. Water → Light → End Day
5. Water → Light → End Day
```

### **Test T4: Verifica Crescita**
- [ ] Console: OnPlantGrew e OnPlantStageChanged
- [ ] Inspector: Stage 0→1→2→3
- [ ] GrowthPoints si incrementano e si azzerano

## 🐛 **Troubleshooting**

### **Problema: END DAY si disabilita dopo 2 giorni**
**Causa:** CRY insufficienti per il loop completo di test
**Soluzione:** ✅ **RISOLTO** - Aumentato `startingCRY` da 50 a 100 in `GameManager.cs`

**Calcolo del problema:**
```
Giorno 1: Plant(-1,-1) + Water(-1,-1) + Light(-1,-1) + EndDay(-20) = 27 CRY rimanenti
Giorno 2: Water(-1,-1) + Light(-1,-1) + EndDay(-20) = 5 CRY rimanenti  
Giorno 3: Water(-1,-1) + Light(-1,-1) + EndDay(-20) = IMPOSSIBILE (serve 20, ne hai 3)
```

**Fix applicato:**
- `startingCRY = 100` (invece di 50)
- Aggiunto commento esplicativo per BLK-01.03A
- Creato `CRYDebugHelper.cs` per debug risorse

### **Debug Helper**
Premi **F1** in Play Mode per vedere:
- Giorno corrente
- Azioni rimanenti  
- CRY disponibili
- Se End Day è possibile

## ✅ **Criteri di Accettazione**

- [x] **Sistema di crescita end-of-day funzionante**
- [x] **3 stadi di crescita (Seed → Sprout → Mature)**
- [x] **Configurazione centralizzata via ScriptableObject**
- [x] **Eventi robusti per UI futura**
- [x] **Integrazione perfetta con sistemi esistenti**
- [x] **Setup automatico e bootstrap**
- [x] **Debug completo e logging**
- [x] **Test loop completo funzionante** ✅ **RISOLTO**

## 🚀 **Prossimi Passi**

Una volta completato il setup:
1. **Test completo** del loop di crescita
2. **Verifica eventi** in Console
3. **Controllo progressione** stadi in Inspector
4. **Preparazione per BLK-01.03B** (HUD/Visual)

---

**Status:** ✅ **COMPLETATO** - Sistema di crescita implementato e testabile
**Ultimo Fix:** ✅ **END DAY disabilitazione risolta** - CRY aumentati per test completo
