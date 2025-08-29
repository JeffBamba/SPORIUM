# 🚨 IMPORTANTE: Creare PlantGrowthConfig in Unity Editor

## ❌ **NON creare manualmente il file .asset!**
Unity deve generare automaticamente il GUID corretto.

## ✅ **Procedura Corretta:**

### **Step 1: Apri Unity Editor**
1. Apri il progetto Unity
2. Naviga a `Assets/_Project/Configs/` nel Project Window

### **Step 2: Crea l'Asset**
1. **Click destro** nella cartella `Configs`
2. **Create** → **Sporae** → **PlantGrowthConfig**
3. Unity creerà automaticamente `PlantGrowthConfig.asset` con GUID valido

### **Step 3: Verifica**
1. L'asset dovrebbe apparire nella cartella
2. **Nessun errore** nella Console
3. L'asset dovrebbe essere selezionabile e modificabile nell'Inspector

## 🔧 **Se il Menu "Sporae" non appare:**

### **Opzione A: Ricompila Scripts**
1. **Ctrl+Shift+R** (Windows) o **Cmd+Shift+R** (Mac)
2. Attendi la ricompilazione
3. Riprova a creare l'asset

### **Opzione B: Restart Unity**
1. Salva la scena
2. Chiudi Unity
3. Riapri il progetto
4. Riprova a creare l'asset

### **Opzione C: Verifica Script**
1. Controlla che `PlantGrowthConfig.cs` sia nella cartella `Scripts`
2. Verifica che non ci siano errori di compilazione
3. Il menu "Sporae" dovrebbe apparire dopo la compilazione

## 📋 **Configurazione Predefinita (dopo creazione):**

Una volta creato l'asset, i valori di default dovrebbero essere:
- **pointsSeedToSprout**: 2
- **pointsSproutToMature**: 3  
- **pointsIdealCare**: 2
- **pointsPartialCare**: 1
- **pointsNoCare**: 0
- **neglectThreshold**: 2
- **dailyHydrationDecay**: 1
- **resetDailyExposureFlags**: true
- **phGrowthMultiplier**: 1.0

## 🎯 **Dopo la Creazione:**

1. **Assegna l'asset** a `PotSystemConfig.GrowthConfig` (se necessario)
2. **Testa il sistema** con i test plan di BLK-01.03A
3. **Verifica** che non ci siano errori nella Console

---

**NOTA**: Non modificare manualmente i file .asset! Lascia che Unity gestisca i GUID e i metadati.
