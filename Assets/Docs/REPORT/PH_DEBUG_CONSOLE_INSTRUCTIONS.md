# 🧪 pH SYSTEM DEBUG CONSOLE - ISTRUZIONI D'USO
## Console di Debug per Test Sistema pH

**Versione:** 1.0  
**Data Creazione:** 2025-01-XX  
**Componente:** `PhSystemDebugConsole.cs`  
**Sistema:** `PhSystem.cs`

---

## 🎯 SCOPO

La console di debug permette di:
- **Modificare il valore pH** in tempo reale
- **Visualizzare lo stato corrente** del sistema pH
- **Testare le interazioni** tra pH e altri sistemi
- **Simulare drift giornaliero** e azioni
- **Monitorare gli effetti** su piante e sistemi

---

## 🚀 SETUP INIZIALE

### **Step 1: Aggiungi il Componente alla Scena**

1. **Apri Unity**
2. **Apri la scena** che stai testando (es. `SCN_VaultMap` o `SCN_Bootstrap`)
3. **Crea un GameObject vuoto**:
   - **Hierarchy** → **Click destro** → **Create Empty**
   - **Rinominalo**: `pH_DebugConsole`
4. **Aggiungi il componente**:
   - **Seleziona** `pH_DebugConsole`
   - **Inspector** → **Add Component**
   - **Cerca**: `PhSystemDebugConsole`
   - **Aggiungi** il componente

### **Step 2: Configurazione**

1. **Seleziona** `pH_DebugConsole` in Hierarchy
2. **Inspector** → **PhSystemDebugConsole**:
   - ✅ `Enable Debug Console` = **true**
   - ✅ `Toggle Key` = **Z** (default)
   - ✅ `Show On Start` = **false** (o **true** se vuoi vederla subito)
   - ⚠️ `Game Manager` = lascia vuoto (verrà trovato automaticamente)

### **Step 3: Verifica**

1. **Vai in Play Mode**
2. **Premi Z** → la console dovrebbe apparire in alto a destra
3. **Console Unity** dovrebbe mostrare:
   ```
   [pH Debug] === pH System Debug Console ===
   [pH Debug] Premi Z per aprire/chiudere la console
   [pH Debug] pH iniziale: 0.00 (Neutrale)
   ```

---

## 🎮 CONTROLLI E COMANDI

### **Tastiera**

| Tasto | Azione |
|-------|--------|
| **Z** | Apri/Chiudi console |
| **1** | Imposta pH a Ultra Acid (-100) |
| **2** | Imposta pH a Stable Acid (-50) |
| **3** | Imposta pH a Neutrale (0) |
| **4** | Imposta pH a Stable Basic (+50) |
| **5** | Imposta pH a Ultra Basic (+100) |
| **+** o **=** | Incrementa pH di +5 |
| **-** | Decrementa pH di -5 |
| **R** | Reset pH a neutro (0) |
| **D** | Simula drift giornaliero |

### **Interfaccia Console**

La console mostra:

1. **Stato Corrente**
   - pH corrente (valore numerico)
   - Banda pH (Ultra Acido, Stable Acido, Neutrale, ecc.)
   - Colore indicativo della banda

2. **Input Manuale**
   - Campo testo per inserire valore pH personalizzato
   - Pulsante "Applica" per impostare il valore

3. **Valori Rapidi**
   - 5 pulsanti per valori comuni:
     - Ultra Acid (-100)
     - Stable Acid (-50)
     - Neutral (0)
     - Stable Basic (+50)
     - Ultra Basic (+100)

4. **Modifiche Incrementali**
   - Pulsanti -10, -5, +5, +10
   - Pulsante Reset

5. **Simulazioni**
   - **Drift Giornaliero**: simula il drift da piante attive
   - **Overwatering**: applica -5 pH
   - **LED Blu**: applica +5 pH
   - **LED Rosso**: applica -5 pH

6. **Effetti su Piante**
   - Mostra come il pH corrente influenza le piante:
     - **Ultra Acid**: PURE Collapsing | EVIL Thriving
     - **Stable Acid**: PURE Weakening | EVIL Thriving
     - **Neutral**: Tutte Stable
     - **Stable Basic**: PURE Thriving | EVIL Weakening
     - **Ultra Basic**: PURE Thriving | EVIL Collapsing

7. **Log Debug**
   - Scroll view con log delle modifiche
   - Timestamp per ogni operazione
   - Ultimi 50 log visibili

---

## 🧪 SCENARI DI TEST

### **Test 1: Modifica pH Manuale**

1. Apri console (Z)
2. Inserisci valore `-75` nel campo testo
3. Clicca "Applica"
4. **Verifica**: pH dovrebbe essere -75, banda "Stable Acido"
5. **Verifica**: Effetti su piante dovrebbero mostrare "PURE: Weakening | EVIL: Thriving"

### **Test 2: Valori Rapidi**

1. Apri console (Z)
2. Clicca "Ultra Basic"
3. **Verifica**: pH = +100, banda "Ultra Basico"
4. Clicca "Ultra Acid"
5. **Verifica**: pH = -100, banda "Ultra Acido"

### **Test 3: Modifiche Incrementali**

1. Apri console (Z)
2. Reset pH (pulsante Reset o tasto R)
3. Clicca "+5" 10 volte
4. **Verifica**: pH dovrebbe essere +50
5. Clicca "-10" 5 volte
6. **Verifica**: pH dovrebbe essere 0

### **Test 4: Simulazioni Azioni**

1. Apri console (Z)
2. Reset pH
3. Clicca "Overwatering"
4. **Verifica**: pH = -5
5. Clicca "LED Blu"
6. **Verifica**: pH = 0
7. Clicca "LED Rosso"
8. **Verifica**: pH = -5

### **Test 5: Drift Giornaliero**

1. Apri console (Z)
2. Reset pH
3. Piantare alcune piante nella scena (se disponibile)
4. Clicca "Drift Giornaliero"
5. **Verifica**: pH modificato in base alle piante attive
6. **Verifica**: Log mostra conteggio piante Pure/Evil/Standard

### **Test 6: Transizioni Bande**

1. Apri console (Z)
2. Imposta pH a -29 (limite Neutrale/Stable Acid)
3. **Verifica**: Banda "Neutrale"
4. Applica -2 (pH = -31)
5. **Verifica**: Banda "Stable Acido"
6. Applica +50 (pH = +19)
7. **Verifica**: Banda "Neutrale"
8. Applica +12 (pH = +31)
9. **Verifica**: Banda "Stable Basico"

---

## 📊 INTERPRETAZIONE RISULTATI

### **Bande pH**

| Banda | Range | Colore UI | Significato |
|-------|-------|-----------|-------------|
| **Ultra Acido** | ≤ -80 | Rosso scuro | Estremo acido, PURE muoiono |
| **Stable Acido** | -79 ... -30 | Arancione | Acido stabile, EVIL prosperano |
| **Neutrale** | -29 ... +29 | Verde | Equilibrio, tutte piante stabili |
| **Stable Basico** | +30 ... +79 | Azzurro | Basico stabile, PURE prosperano |
| **Ultra Basico** | ≥ +80 | Blu scuro | Estremo basico, EVIL muoiono |

### **Effetti su Piante**

- **Thriving**: Crescita +50%, resa frutti +20%
- **Stable**: Crescita normale
- **Weakening**: Crescita -30%, resa frutti -20%
- **Collapsing**: Pianta muore in 2-3 giorni se non corretta

---

## 🔧 INTEGRAZIONE CON ALTRI SISTEMI

### **GameManager**

La console cerca automaticamente il `GameManager` nella scena. Se non trovato, funziona comunque ma alcune funzionalità potrebbero essere limitate.

### **ServiceContainer**

Il sistema pH viene registrato automaticamente nel `ServiceContainer` per essere accessibile da altri sistemi:

```csharp
var phSystem = ServiceContainer.Instance.Get<PhSystem>();
float currentPh = phSystem.CurrentPh;
```

### **Eventi**

Il sistema pH emette eventi quando cambia:

```csharp
_phSystem.OnPhChanged += (newPh, delta) => {
    Debug.Log($"pH cambiato: {newPh} (delta: {delta})");
};
```

---

## ⚠️ NOTE IMPORTANTI

1. **Solo Editor/Development Build**
   - La console è automaticamente disabilitata in build release
   - Non influisce sulle performance del gioco finale

2. **Valori Clamp**
   - Il pH è sempre limitato tra -100 e +100
   - Tentativi di superare questi limiti vengono ignorati

3. **Log Limitati**
   - Solo gli ultimi 50 log sono visibili
   - I log più vecchi vengono rimossi automaticamente

4. **Simulazione Drift**
   - Il drift giornaliero simulato è basato su piante nella scena
   - Se non ci sono piante, usa un drift casuale

---

## 🐛 TROUBLESHOOTING

### **Problema: Console non appare**

**Soluzione:**
- Verifica che `Enable Debug Console` sia **true**
- Verifica che non ci siano errori nella Console Unity
- Prova a premere Z più volte

### **Problema: Valori non si applicano**

**Soluzione:**
- Verifica che il valore inserito sia tra -100 e +100
- Controlla la Console Unity per messaggi di errore
- Verifica che il componente sia attivo nella scena

### **Problema: Hotkeys non funzionano**

**Soluzione:**
- Verifica che la console sia aperta (Z)
- Verifica che nessun altro componente stia intercettando i tasti
- Controlla che `Enable Debug Console` sia **true**

### **Problema: Sistema pH non trovato**

**Soluzione:**
- La console crea automaticamente il sistema pH
- Se necessario, verifica che `ServiceContainer` sia inizializzato
- Controlla la Console Unity per messaggi di registrazione

---

## 📚 PROSSIMI PASSI

Una volta testato il sistema pH base:

1. **Integrare con GameManager** per drift automatico al cambio giorno
2. **Collegare con sistema piante** per drift reale da famiglie
3. **Aggiungere UI pH** nella HUD principale
4. **Implementare effetti su piante** (Thriving/Weakening/Collapsing)
5. **Collegare con sistema reputazione** per drift fazioni

---

**Status**: ✅ **Console Debug Creata**  
**Prossimo**: 🧪 **Test Sistema pH**

