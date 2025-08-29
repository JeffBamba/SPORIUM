# 🐛 **DEBUG SETUP INSTRUCTIONS - BLK-01.03A**

## 🎯 **Problema Attuale**
L'HUD mostra sempre 50 CRY e 3 azioni invece di 250 CRY e 4 azioni, anche dopo aver modificato i parametri.

## 🔧 **Setup Debug Helper**

### **Step 1: Aggiungi GameManagerDebugHelper alla Scena**
1. **Apri Unity**
2. **Apri la scena** che stai testando (probabilmente `SCN_Bootstrap` o `SCN_VaultMap`)
3. **Crea un GameObject vuoto**:
   - **Hierarchy** → **Click destro** → **Create Empty**
   - **Rinominalo**: `DEBUG_Helper`
4. **Aggiungi il componente**:
   - **Seleziona** `DEBUG_Helper`
   - **Inspector** → **Add Component**
   - **Cerca**: `GameManagerDebugHelper`
   - **Aggiungi** il componente

### **Step 2: Verifica Configurazione**
1. **Seleziona** `DEBUG_Helper` in Hierarchy
2. **Inspector** → **GameManagerDebugHelper**:
   - ✅ `Enable Debug` = **true**
   - ✅ `Debug Key` = **F2**
   - ✅ `Force Update Key` = **F3**

### **Step 3: Verifica GameManager**
1. **Trova il GameObject** con `GameManager` in scena
2. **Inspector** → **GameManager**:
   - ✅ `Starting CRY` = **250** (non 50!)
   - ✅ `Actions Per Day` = **4** (non 3!)
   - ✅ `Show Debug Logs` = **true**

### **Step 4: Verifica HUDController**
1. **Trova il GameObject** con `HUDController` in scena
2. **Inspector** → **HUDController**:
   - ✅ `Show Debug Logs` = **true**

## 🧪 **Test del Debug**

### **Test 1: Verifica Inizializzazione**
1. **Vai in Play Mode**
2. **Console dovrebbe mostrare**:
   ```
   [GameManagerDebugHelper] Start() chiamato - Inizializzazione...
   [GameManagerDebugHelper] GameManager trovato: [nome]
   [GameManagerDebugHelper] HUDController trovato: [nome]
   [GameManagerDebugHelper] Inizializzazione completata. Premi F2 per debug, F3 per sync.
   ```

### **Test 2: Verifica Tasti**
1. **In Play Mode**, premi e **tieni premuto**:
   - **F2** → Console dovrebbe mostrare "TASTO F2 TENUTO!"
   - **F3** → Console dovrebbe mostrare "TASTO F3 TENUTO!"

### **Test 3: Debug Completo**
1. **Premi F2** → Console dovrebbe mostrare:
   ```
   === GAMEMANAGER DEBUG HELPER ===
   GameManager - Starting CRY: 250, Current CRY: 250
   GameManager - Starting Actions: 4, Current Actions: 4
   GameManager - Current Day: 1
   HUDController: Trovato
   ================================
   ```

### **Test 4: Forzatura Sincronizzazione**
1. **Premi F3** → Console dovrebbe mostrare:
   ```
   === FORZATURA SINCRONIZZAZIONE ===
   GameManager.ForceUIUpdate() chiamato
   HUDController.ForceUpdateAllUI() chiamato
   ==================================
   ```

## ❌ **Se Non Funziona**

### **Problema 1: Tasti non rilevati**
- **Verifica**: Console non mostra "TASTO F2/F3 TENUTO!"
- **Soluzione**: Il componente non è in scena o non è attivo

### **Problema 2: GameManager non trovato**
- **Verifica**: Console mostra "GameManager: NULL"
- **Soluzione**: GameManager non è in scena o ha un nome diverso

### **Problema 3: HUDController non trovato**
- **Verifica**: Console mostra "HUDController: NULL"
- **Soluzione**: HUDController non è in scena o ha un nome diverso

### **Problema 4: Valori ancora sbagliati**
- **Verifica**: Console mostra valori corretti ma HUD no
- **Soluzione**: Problema di sincronizzazione tra sistemi

## 🚀 **Prossimi Passi**
Una volta che il debug funziona:
1. **Identifica il problema** dai log della console
2. **Applica il fix** appropriato
3. **Testa la soluzione** in Play Mode

---

**Status**: 🔧 **Setup Debug in Corso**
**Prossimo**: 🧪 **Test Debug Helper**
