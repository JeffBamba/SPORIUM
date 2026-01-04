# 📋 ANALISI COMPLETA: SISTEMA LED (Red e Blue) - Sporium

**Data Analisi:** 2025-01-XX  
**BLK Code:** BLK-02.07  
**Versione Sistema:** Persistent Toggle (v2.0)

---

## 🎯 PANORAMICA GENERALE

Il sistema LED in Sporium è un **sistema persistente di illuminazione** che permette al giocatore di attivare/disattivare LED Blu o Rossi per influenzare la crescita delle piante. Il sistema è stato migrato da un sistema "click giornaliero" a un sistema "toggle persistente" (simile al sistema di irrigazione).

### **Stati Disponibili:**
- **Off** (spento): Nessun effetto
- **Blue** (LED Blu): Accelera crescita vegetativa (Growth → Flowering), pH +5
- **Red** (LED Rosso): Accelera fioritura (Flowering → HarvestReady), pH -5

---

## 🏗️ ARCHITETTURA DEL SISTEMA

### **1. Componenti Core**

#### **LedType.cs** (`Assets/_Project/Scripts/Dome/PotSystem/Growth/LedType.cs`)
Definisce gli enum del sistema:

```1:20:Assets/_Project/Scripts/Dome/PotSystem/Growth/LedType.cs
namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// Tipo di LED utilizzato per l'illuminazione delle piante
    /// </summary>
    public enum LedType
    {
        Blue = 0,   // LED Blu: accelera Growth → Flowering, pH +5
        Red = 1    // LED Rosso: accelera Flowering → HarvestReady, pH -5
    }
    
    /// <summary>
    /// BLK-02.07: Stato sistema LED persistente (toggle Off/Blue/Red)
    /// </summary>
    public enum LedSystemState
    {
        Off = 0,   // Sistema LED spento
        Blue = 1,  // LED Blu attivo (Growth/stabilità)
        Red = 2    // LED Rosso attivo (Flowering/produzione)
    }
}
```

#### **LedLightController.cs** (`Assets/_Project/Scripts/Dome/PotSystem/LedLightController.cs`)
Gestisce le **luci Unity 2D** (Light2D) associate ai LED dei vasi:

- Controlla le luci blu e rosse visibili nella scena
- Metodi principali:
  - `UpdateLights(LedSystemState)`: Aggiorna le luci in base allo stato
  - `SetBlueLight(bool)`: Attiva/disattiva luce blu
  - `SetRedLight(bool)`: Attiva/disattiva luce rossa

#### **PotStateModel.cs** (`Assets/_Project/Scripts/Dome/PotStateModel.cs`)
Contiene lo stato persistente del LED per ogni vaso:

```52:58:Assets/_Project/Scripts/Dome/PotStateModel.cs
    [Header("LED System (BLK-02.07 - Persistent Toggle)")]
    [Tooltip("Stato sistema LED: Off, Blue, Red")]
    public LedSystemState LedSystemState = LedSystemState.Off;
    [Tooltip("Giorni consecutivi con BLUE LED attivo")]
    public int DaysLedBlueConsecutive = 0;
    [Tooltip("Giorni consecutivi con RED LED attivo")]
    public int DaysLedRedConsecutive = 0;
```

Metodi chiave:
- `SetLedSystemState(LedSystemState)`: Imposta lo stato e resetta contatori quando si cambia tipo
- `GetConsecutiveLedDays()`: Ottiene giorni consecutivi per stato corrente
- `IncrementConsecutiveLedDays()`: Incrementa contatore (chiamato a fine giornata)
- `GetBurnRiskLevel()`: Calcola livello di rischio bruciatura (0-3)

#### **PotActions.cs** (`Assets/_Project/Scripts/Dome/PotActions.cs`)
Gestisce l'**azione del giocatore** per attivare/disattivare i LED:

```775:887:Assets/_Project/Scripts/Dome/PotActions.cs
    /// <summary>
    /// BLK-02.07: Toggle sistema LED persistente (Off/Blue/Red)
    /// Effetti applicati a fine giornata, non immediatamente
    /// </summary>
    /// <param name="newState">Stato desiderato. Se null, cicla: Off → Blue → Red → Off</param>
    public bool DoLight(LedSystemState? newState = null)
    {
        // DEBUG_SAFE_FIX: Guard per prevenire chiamate multiple nello stesso frame
        if (_isLightingInProgress)
        {
            SporiumLogger.LogWarning(LogCategory.Pot, $"[{potSlot?.PotId}] DoLight già in esecuzione! Ignorando chiamata duplicata.");
            return false;
        }
        
        _isLightingInProgress = true;
        
        try
        {
            if (!CanLight())
            {
                string reason = GetLightFailureReason();
                PotEvents.EmitActionFailed(PotEvents.PotActionType.Light, potSlot, reason);
                return false;
            }
            
            // DEBUG_SAFE_FIX: Log prima del consumo risorse per tracciare chiamate multiple
            int actionsBefore = _gameManager?.ActionsLeft ?? 0;
            SporiumLogger.LogDebug(LogCategory.Pot, $"[{potSlot?.PotId}] DoLight chiamato - Azioni prima: {actionsBefore}, newState: {newState}");
            
            // Consuma solo 1 Azione per il toggle (non CRY - consumo giornaliero)
            if (!TryConsumeResources())
            {
                PotEvents.EmitActionFailed(PotEvents.PotActionType.Light, potSlot, "Insufficient resources");
                return false;
            }
            
            int actionsAfter = _gameManager?.ActionsLeft ?? 0;
            SporiumLogger.LogDebug(LogCategory.Pot, $"[{potSlot?.PotId}] DoLight - Azioni dopo consumo: {actionsAfter} (consumate: {actionsBefore - actionsAfter})");
            
            // Salva stato precedente per rimuovere contributo pH se necessario
            LedSystemState oldState = _potState.LedSystemState;
            
            // Toggle o set esplicito
            if (newState.HasValue)
            {
                _potState.SetLedSystemState(newState.Value);
            }
            else
            {
                // Ciclo: Off → Blue → Red → Off
                LedSystemState nextState = (LedSystemState)(((int)_potState.LedSystemState + 1) % 3);
                _potState.SetLedSystemState(nextState);
            }
            
            // BLK-02.07 BUG FIX: Rimuovi contributo pH se LED è stato spento
            if (oldState != LedSystemState.Off && _potState.LedSystemState == LedSystemState.Off)
            {
                // LED spento: rimuovi contributo pH del LED precedente
                if (_phSystem != null)
                {
                    string actionName = oldState == LedSystemState.Blue ? "BlueLED" : "RedLED";
                    // Rimuovi tutti i contributi di questo LED per questo vaso (inclusi quelli con moltiplicatori)
                    _phSystem.RemoveActionContribution("BlueLED", potSlot.PotId);
                    _phSystem.RemoveActionContribution("RedLED", potSlot.PotId);
                    // Rimuovi anche varianti con moltiplicatori
                    _phSystem.RemoveActionContribution("BlueLED_x1.5", potSlot.PotId);
                    _phSystem.RemoveActionContribution("BlueLED_x2", potSlot.PotId);
                    _phSystem.RemoveActionContribution("RedLED_x1.5", potSlot.PotId);
                    _phSystem.RemoveActionContribution("RedLED_x2", potSlot.PotId);
                    
                    if (showDebugLogs)
                        SporiumLogger.LogDebug(LogCategory.Ph, $"{potSlot.PotId}: Contributo pH LED rimosso (LED spento: {oldState} → Off)");
                }
            }
            
            // COMPATIBILITÀ: Aggiorna LastLedType per sistemi legacy
            if (_potState.LedSystemState == LedSystemState.Blue)
                _potState.LastLedType = LedType.Blue;
            else if (_potState.LedSystemState == LedSystemState.Red)
                _potState.LastLedType = LedType.Red;
            else
                _potState.LastLedType = null;
            
            // BLK-02.07: Aggiorna luci Unity
            if (ledLightController != null)
            {
                ledLightController.UpdateLights(_potState.LedSystemState);
            }
            
            // NOTA: NON applicare effetti pH qui - vengono applicati a fine giornata
            // NOTA: NON incrementare LightExposure qui - viene fatto a fine giornata
            
            // Toast notifica cambio stato (gestito da PotNotifications tramite PotEvents.OnPotAction)
            // I toast vengono mostrati automaticamente quando viene emesso PotEvents.EmitAction()
            
            // Notifica il cambio stato
            PotEvents.EmitAction(PotEvents.PotActionType.Light, potSlot);
            PotEvents.EmitChanged(potSlot);
            
            if (showDebugLogs)
            {
                string stateMsg = _potState.LedSystemState.ToString();
                SporiumLogger.LogInfo(LogCategory.Pot, $"[ACT-003][{potSlot.PotId}] LED System Toggle: {stateMsg} (effetti a fine giornata)");
            }
            
            return true;
        }
        finally
        {
            // Reset del flag nel prossimo frame per permettere nuove chiamate
            StartCoroutine(ResetLightingFlag());
        }
    }
```

**Note importanti:**
- Consuma 1 Azione per il toggle (non CRY - viene consumato a fine giornata)
- Aggiorna immediatamente le luci Unity (feedback visivo)
- **NON applica effetti pH o crescita immediatamente** - vengono applicati a fine giornata
- Rimuove contributo pH se LED viene spento

---

## 🔄 FLUSSO DI FUNZIONAMENTO

### **Fase 1: Toggle da Giocatore**

1. Giocatore clicca pulsante LED (Blue o Red) in UI (`PotDetailsWidget`)
2. Chiamata a `PotActions.DoLight(LedType.Blue/Red)`
3. Verifica `CanLight()`: vaso ha pianta, player in range, risorse sufficienti
4. Consumo 1 Azione
5. Aggiornamento `PotStateModel.LedSystemState`
6. Aggiornamento luci Unity (feedback visivo immediato)
7. Emissione eventi (`PotEvents.EmitAction`, `PotEvents.EmitChanged`)

### **Fase 2: Applicazione Effetti a Fine Giornata**

Il `DayCycleController` applica gli effetti LED per tutti i vasi registrati:

```1180:1283:Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs
    /// <summary>
    /// BLK-02.07: Applica effetti sistema LED persistente a fine giornata
    /// </summary>
    private void ApplyLedSystemEffects()
    {
        if (_gameManager == null)
        {
            TryGetGameManager();
            if (_gameManager == null)
            {
                if (enableDebugLogs)
                    SporiumLogger.LogWarning(LogCategory.Core, "GameManager non disponibile per applicazione effetti LED");
                return;
            }
        }
        
        foreach (var pot in _registeredPots)
        {
            if (pot == null || !pot.HasPlant)
                continue;
            
            ApplyLedSystemEffectsForPot(pot);
        }
    }
    
    /// <summary>
    /// BLK-02.07: Applica effetti sistema LED persistente per un singolo vaso
    /// </summary>
    private void ApplyLedSystemEffectsForPot(PotStateModel pot)
    {
        // Salva stato precedente per verificare se è stato spento
        LedSystemState stateBeforeCheck = pot.LedSystemState;
        
        if (pot.LedSystemState == LedSystemState.Off)
        {
            // Sistema OFF: decadimento graduale se era acceso
            bool hadBlueDays = pot.DaysLedBlueConsecutive > 0;
            bool hadRedDays = pot.DaysLedRedConsecutive > 0;
            
            if (pot.DaysLedBlueConsecutive > 0)
                pot.DaysLedBlueConsecutive = Mathf.Max(0, pot.DaysLedBlueConsecutive - 1);
            if (pot.DaysLedRedConsecutive > 0)
                pot.DaysLedRedConsecutive = Mathf.Max(0, pot.DaysLedRedConsecutive - 1);
            
            if (enableDebugLogs && (hadBlueDays || hadRedDays))
            {
                SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: LED System OFF - Decadimento contatori (Blue: {pot.DaysLedBlueConsecutive}, Red: {pot.DaysLedRedConsecutive})");
            }
            return;
        }
        
        // Incrementa contatori giorni consecutivi
        pot.IncrementConsecutiveLedDays();
        int consecutiveDays = pot.GetConsecutiveLedDays();
        
        // Calcola scaling effetti
        float effectMultiplier = GetLedEffectMultiplier(consecutiveDays);
        float malusMultiplier = GetLedMalusMultiplier(consecutiveDays);
        
        // Applica effetti crescita e pH
        ApplyLedEffects(pot, pot.LedSystemState, effectMultiplier, malusMultiplier, consecutiveDays);
        
        // Consumo CRY notturno
        int cryCost = GetNightlyCryCost(pot.LedSystemState, consecutiveDays);
        if (cryCost > 0)
        {
            if (_gameManager.TrySpendCry(cryCost))
            {
                if (enableDebugLogs)
                    SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Consumo CRY notturno LED: {cryCost} CRY");
            }
            else
            {
                // CRY insufficiente: spegni sistema e notifica
                LedSystemState oldState = pot.LedSystemState;
                pot.SetLedSystemState(LedSystemState.Off);
                
                // BLK-02.07 BUG FIX: Rimuovi contributo pH quando LED viene spento per CRY insufficiente
                if (_phSystem != null && oldState != LedSystemState.Off)
                {
                    // Rimuovi tutti i contributi LED per questo vaso
                    _phSystem.RemoveActionContribution("BlueLED", pot.PotId);
                    _phSystem.RemoveActionContribution("RedLED", pot.PotId);
                    _phSystem.RemoveActionContribution("BlueLED_x1.5", pot.PotId);
                    _phSystem.RemoveActionContribution("BlueLED_x2", pot.PotId);
                    _phSystem.RemoveActionContribution("RedLED_x1.5", pot.PotId);
                    _phSystem.RemoveActionContribution("RedLED_x2", pot.PotId);
                    
                    if (enableDebugLogs)
                        SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: Contributo pH LED rimosso (CRY insufficiente, LED spento: {oldState} → Off)");
                }
                
                ShowLedNotification($"LGT-002: Sistema LED {pot.PotId} spento - CRY insufficiente", Color.yellow);
                if (enableDebugLogs)
                    SporiumLogger.LogWarning(LogCategory.Pot, $"{pot.PotId}: CRY insufficiente per LED, sistema spento");
            }
        }
        
        // Toast avviso zona rossa (4+ giorni)
        if (consecutiveDays >= 4)
        {
            ShowLedNotification($"LGT-003: LED {pot.LedSystemState} attivo {consecutiveDays} giorni - Zona rossa!", Color.red);
        }
    }
```

### **Effetti Applicati:**

1. **Scaling Effetti** (in base a giorni consecutivi):
   - Giorno 1: Multiplier base (x1.0)
   - Giorni 2-3: Multiplier x1.5
   - Giorni 4+: Multiplier x2.0

2. **Effetti pH** (con scaling):
   - Blue LED: pH +5 (base) → +7.5 (2-3 giorni) → +10 (4+ giorni)
   - Red LED: pH -5 (base) → -7.5 (2-3 giorni) → -10 (4+ giorni)

3. **Effetti Crescita** (LightExposure):
   - Incrementa `LightExposure` fino al massimo permesso

4. **Consumo CRY Notturno**:
   - Blue: 1 + (giorni / 2) → 1, 1, 2, 2, 3...
   - Red: 2 + giorni → 2, 3, 4, 5... (più costoso)

5. **Decadimento** (quando LED spento):
   - I contatori diminuiscono gradualmente (-1 per giorno)
   - Permette decrescita graduale dello stress

---

## 🔗 CONNESSIONI CON ALTRI SISTEMI

### **1. Sistema pH (PhSystem)**

**Connessione:** Il LED modifica il pH dell'acqua attraverso `PhSystem.RegisterActionDrift()`.

**Meccanismo:**
- Blue LED: pH +5 (base) con scaling fino a +10
- Red LED: pH -5 (base) con scaling fino a -10
- Il contributo viene registrato con nome azione: "BlueLED", "RedLED", "BlueLED_x1.5", "RedLED_x2", etc.
- Quando LED viene spento, il contributo viene rimosso automaticamente

**Codice chiave:**
```1325:1349:Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs
    private void ApplyLedEffects(PotStateModel pot, LedSystemState state, float effectMultiplier, float malusMultiplier, int consecutiveDays)
    {
        if (state == LedSystemState.Off) return;
        
        // Converti LedSystemState a LedType per compatibilità
        LedType ledType = state == LedSystemState.Blue ? LedType.Blue : LedType.Red;
        
        // Effetti pH (con scaling)
        if (_phSystem != null)
        {
            float basePhDelta = ledType == LedType.Blue ? Sporae.DevTools.DifficultyCalibrationConfig.PhDriftLedBlue : Sporae.DevTools.DifficultyCalibrationConfig.PhDriftLedRed;
            float phDelta = basePhDelta * effectMultiplier;
            string actionName = ledType == LedType.Blue ? "BlueLED" : "RedLED";
            
            // Aggiungi moltiplicatore al nome azione per tooltip
            if (consecutiveDays >= 4)
                actionName += "_x2";
            else if (consecutiveDays >= 2)
                actionName += "_x1.5";
            
            _phSystem.RegisterActionDrift(phDelta, actionName, pot.PotId);
            
            if (enableDebugLogs)
                SporiumLogger.LogDebug(LogCategory.Pot, $"{pot.PotId}: LED {state} giorno {consecutiveDays} - pH {(phDelta > 0 ? "+" : "")}{phDelta:F1} (mult: {effectMultiplier:F1})");
        }
```

### **2. Sistema di Crescita (Growth System)**

**Connessione:** Il LED aumenta `LightExposure`, che influisce sulla progressione degli stadi.

**Meccanismo:**
- LED aumenta `LightExposure` fino al massimo permesso (`MaxLightExposure`)
- `LightExposure` è un requisito per la progressione degli stadi
- Valore preservato se impostato manualmente (debug console)

**Codice chiave:**
```1351:1367:Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs
        // Effetti crescita (Light Exposure)
        // BUG FIX: Se LightExposure è stato impostato manualmente, preserva il valore base
        // Il LED può aumentare LightExposure sopra il valore base, ma il valore base viene preservato
        int maxLightExposure = GetMaxLightExposureForPot(pot);
        if (pot.LightExposure < maxLightExposure)
        {
            pot.IncreaseLightExposure(maxLightExposure);
            
            // Se LightExposure è stato impostato manualmente, aggiorna il valore base se è più basso del valore attuale
            // Questo permette al LED di aumentare LightExposure sopra il valore base, ma preserva il valore base per quando LED è spento
            if (pot.IsLightExposureManuallySet && pot.ManualLightExposureBase >= 0)
            {
                // Il valore base rimane quello impostato manualmente, ma LightExposure può essere aumentato dal LED
                // Quando LED è spento, LightExposure tornerà al valore base
            }
        }
```

### **3. Stage Requirements (Requisiti Stadio)**

**Connessione:** Alcuni stadi richiedono un LED specifico per la progressione.

**Meccanismo:**
- Ogni `StageRequirements` può avere un `requiredLed` (nullable)
- Verifica tramite `IsLedRequirementMet(LedSystemState)` controlla se lo stato corrente corrisponde al requisito
- Se LED richiesto non è attivo, la progressione dello stadio è bloccata

**Codice chiave:**
```138:151:Assets/_Project/Scripts/Dome/PotSystem/Growth/StageRequirements.cs
        /// <summary>
        /// BLK-02.07: Verifica se il LED richiesto è attivo (nuovo sistema - usa LedSystemState)
        /// </summary>
        public bool IsLedRequirementMet(LedSystemState currentState)
        {
            LedType? required = GetRequiredLed();
            if (!required.HasValue) return true;  // Nessun LED richiesto
            
            if (currentState == LedSystemState.Off) return false;  // Sistema spento
            
            // Converti LedSystemState a LedType per verifica
            LedType currentLedType = currentState == LedSystemState.Blue ? LedType.Blue : LedType.Red;
            return currentLedType == required.Value;
        }
```

**Esempi configurazione:**
- **Growth**: Richiede Blue LED (accelera crescita vegetativa)
- **Flowering**: Richiede Red LED (accelera fioritura)
- Altri stadi: Nessun LED richiesto (optional)

### **4. Sistema Risorse (GameManager)**

**Connessione:** Il LED consuma Azioni e CRY.

**Meccanismo:**
- **Toggle:** Consuma 1 Azione (immediato)
- **Notte:** Consuma CRY in base a giorni consecutivi (a fine giornata)
  - Blue: 1 + (giorni / 2)
  - Red: 2 + giorni (più costoso)
- Se CRY insufficiente, il sistema viene spento automaticamente

**Codice chiave:**
```1306:1320:Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs
    /// <summary>
    /// BLK-02.07: Calcola consumo CRY notturno per sistema LED
    /// </summary>
    private int GetNightlyCryCost(LedSystemState state, int consecutiveDays)
    {
        switch (state)
        {
            case LedSystemState.Blue:
                return 1 + (consecutiveDays / 2);  // 1, 1, 2, 2, 3...
            case LedSystemState.Red:
                return 2 + consecutiveDays;        // 2, 3, 4, 5... (più costoso)
            default:
                return 0;
        }
    }
```

### **5. Sistema UI (PotDetailsWidget)**

**Connessione:** UI mostra stato LED e permette toggle.

**Meccanismo:**
- Due pulsanti separati: "LED Blue ON/OFF" e "LED Red ON/OFF"
- Aggiornamento automatico quando stato cambia
- Visualizzazione stato corrente (ON/OFF)

**Codice chiave:**
```922:936:Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs
            // BLK-02.07: Due pulsanti separati per Blue e Red (ON/OFF)
            if (_blueLedButton != null)
            {
                LedSystemState currentState = pot.PotActions.GetLedSystemState();
                bool isBlueOn = currentState == LedSystemState.Blue;
                string buttonText = isBlueOn ? "LED Blue ON" : "LED Blue OFF";
                UpdateButtonState(_blueLedButton, pot.PotActions.CanLight(), buttonText);
            }
            if (_redLedButton != null)
            {
                LedSystemState currentState = pot.PotActions.GetLedSystemState();
                bool isRedOn = currentState == LedSystemState.Red;
                string buttonText = isRedOn ? "LED Red ON" : "LED Red OFF";
                UpdateButtonState(_redLedButton, pot.PotActions.CanLight(), buttonText);
            }
```

### **6. Sistema Salvataggio (SaveManager)**

**Connessione:** Lo stato LED viene salvato e caricato.

**Meccanismo:**
- Salvataggio: `LedSystemState`, `DaysLedBlueConsecutive`, `DaysLedRedConsecutive`
- Caricamento: Migrazione automatica da `LastLedType` (legacy) a `LedSystemState`
- Compatibilità retroattiva con salvataggi vecchi

**Codice chiave:**
```398:405:Assets/_Project/Scripts/Core/SaveManager.cs
                        // BLK-02.07: Applica nuovi campi sistema LED (con default se mancanti - migrazione automatica)
                        if (Enum.TryParse<LedSystemState>(potStateData.ledSystemState, out var ledState))
                            potState.LedSystemState = ledState;
                        else
                            potState.LedSystemState = LedSystemState.Off;  // Default se parsing fallisce
                        
                        potState.DaysLedBlueConsecutive = potStateData.daysLedBlueConsecutive;
                        potState.DaysLedRedConsecutive = potStateData.daysLedRedConsecutive;
```

### **7. Sistema Notifiche (ToastNotificationManager)**

**Connessione:** Notifiche quando LED viene spento per CRY insufficiente o entra in "zona rossa".

**Meccanismo:**
- LGT-002: Sistema LED spento - CRY insufficiente
- LGT-003: LED attivo 4+ giorni - Zona rossa!

### **8. Sistema Condizione Pianta (PlantConditionSystem)**

**Connessione:** (Futura - BLK-02.08) Il LED può causare stress/burn quando attivo per molti giorni consecutivi.

**Meccanismo previsto:**
- Burn Risk Level calcolato in base a giorni consecutivi
- Livello 0-3 (Nessun rischio → Critico)
- Malus alla crescita quando in zona rossa (4+ giorni)

**Codice esistente (preparazione):**
```472:493:Assets/_Project/Scripts/Dome/PotStateModel.cs
    /// <summary>
    /// BLK-02.07: Calcola livello Burn Risk in base a giorni consecutivi
    /// </summary>
    /// <returns>0 = Nessun rischio, 1 = Medio, 2 = Alto, 3 = Critico</returns>
    public int GetBurnRiskLevel()
    {
        int consecutiveDays = GetConsecutiveLedDays();
        
        if (LedSystemState == LedSystemState.Off || consecutiveDays <= 1)
            return 0;  // Nessun rischio
        
        if (consecutiveDays >= 2 && consecutiveDays <= 3)
            return 1;  // Rischio medio
        
        if (consecutiveDays >= 4 && consecutiveDays <= 5)
            return 2;  // Rischio alto
        
        if (consecutiveDays >= 6)
            return 3;  // Rischio critico (zona rossa)
        
        return 0;
    }
```

---

## 📊 RIEPILOGO CONNESSIONI

| Sistema | Tipo Connessione | Meccanismo | Direzione |
|---------|-----------------|------------|-----------|
| **pH System** | Effetto diretto | `RegisterActionDrift()` | LED → pH |
| **Growth System** | Effetto diretto | `IncreaseLightExposure()` | LED → Crescita |
| **Stage Requirements** | Verifica requisito | `IsLedRequirementMet()` | LED ↔ Progressione |
| **GameManager** | Consumo risorse | `TrySpendAction()`, `TrySpendCry()` | LED ← Risorse |
| **UI (PotDetailsWidget)** | Visualizzazione/Input | Pulsanti toggle | LED ↔ UI |
| **SaveManager** | Persistenza | Serializzazione stato | LED ↔ Salvataggi |
| **Toast Notifications** | Feedback giocatore | Notifiche eventi | LED → UI Notifiche |
| **PlantConditionSystem** | (Futuro) Stress/Burn | Calcolo rischio | LED → Condizione |

---

## 🔍 PUNTI CHIAVE DEL SISTEMA

1. **Toggle Persistente**: Il LED rimane attivo finché non viene spento o finché non finiscono le risorse
2. **Effetti Ritardati**: Gli effetti (pH, crescita) vengono applicati a fine giornata, non immediatamente
3. **Scaling Effetti**: Più giorni consecutivi = effetti maggiori (fino a x2)
4. **Costo Crescente**: CRY consumato aumenta con giorni consecutivi
5. **Decadimento Graduale**: Quando spento, i contatori diminuiscono gradualmente
6. **Rischio Bruciatura**: (Futuro) Dopo 4+ giorni consecutivi, aumenta il rischio di danni
7. **Integrazione pH**: Contributo pH viene aggiunto/rimosso automaticamente
8. **Requisiti Stadio**: Alcuni stadi richiedono LED specifici per progredire

---

## 📝 NOTE IMPLEMENTATIVE

- **BLK-02.07**: Sistema migrato da "click giornaliero" a "toggle persistente"
- **Compatibilità**: Mantiene `LastLedType` per sistemi legacy
- **Migrazione Salvataggi**: Conversione automatica da vecchi a nuovi salvataggi
- **Debug Console**: Supporto per override manuale (preserva valori base)
- **Test**: Sistema testato con salvataggi vecchi e nuovi

---

**Fine Analisi**

