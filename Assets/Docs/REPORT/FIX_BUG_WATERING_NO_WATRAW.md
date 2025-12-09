# FIX: BUG - Sistema Watering Funziona Senza WAT-RAW

**Data:** 2025-12-09  
**Bug:** Sistema irrigazione continua a funzionare anche quando WAT-RAW è esaurito

---

## 🐛 PROBLEMA

Il sistema di irrigazione continuava a funzionare e ad aumentare l'idratazione anche quando WAT-RAW era esaurito da 2 giorni, senza mostrare warning né messaggi di disattivazione.

### **Causa**
Il sistema controllava la disponibilità di WAT-RAW **solo quando l'accumulatore era >= 1.0**, ma applicava gli effetti (idratazione +1) **anche quando l'accumulatore era < 1.0**, senza verificare se WAT-RAW era disponibile.

**Esempio del bug:**
- Giorno 1: Accumulatore = 0.5, WAT-RAW = 0 → Sistema applica idratazione +1 ❌
- Giorno 2: Accumulatore = 1.0, WAT-RAW = 0 → Sistema controlla WAT-RAW e disattiva ✅
- **Ma** il sistema ha già funzionato per 1 giorno senza WAT-RAW!

---

## ✅ SOLUZIONE

### **1. Controllo WAT-RAW PRIMA di Applicare Effetti**

Modificato `ApplyWateringSystemEffects()` per controllare **PRIMA** se c'è WAT-RAW disponibile, **indipendentemente dall'accumulatore**:

```csharp
if (pot.WateringSystemOn)
{
    // BUG FIX: Controlla PRIMA se c'è WAT-RAW disponibile (anche se accumulatore < 1.0)
    // Se non c'è WAT-RAW, disattiva immediatamente il sistema
    if (!_gameManager.PlayerInventory.Has(Items.Water))
    {
        // FALLBACK: Disattiva sistema automaticamente - WAT-RAW insufficiente
        pot.WateringSystemOn = false;
        pot.WateringRawWaterAccumulator = 0f;
        pot.DaysWateringSystemOn = 0;
        
        // Emetti evento per UI
        PotEvents.EmitActionFailed(PotEvents.PotActionType.Water, 
            FindPotSlot(pot.PotId), 
            "Sistema disattivato: WAT-RAW insufficiente");
        
        // Salta al prossimo vaso (sistema disattivato)
        continue;
    }
    
    // Sistema ON: accumula WAT-RAW e applica idratazione
    pot.WateringRawWaterAccumulator += 0.5f;
    
    // Se accumulatore >= 1.0, consuma 1 WAT-RAW
    if (pot.WateringRawWaterAccumulator >= 1.0f)
    {
        // WAT-RAW già verificato sopra, quindi consuma
        _gameManager.PlayerInventory.Consume(Items.Water, 1);
        pot.WateringRawWaterAccumulator -= 1.0f;
    }
    
    // Applica effetti (WAT-RAW già verificato e disponibile)
    // ...
}
```

### **2. Warning Preventivo Migliorato**

Modificato `CheckWateringSystemResources()` per verificare WAT-RAW disponibile **indipendentemente dall'accumulatore** e mostrare toast warning:

```csharp
// BUG FIX: Verifica WAT-RAW disponibile (non solo se accumulatore >= 1.0)
// Se non c'è WAT-RAW, il sistema verrà disattivato
if (!_gameManager.PlayerInventory.Has(Items.Water))
{
    vasiDaDisattivare++;
    vasiDaDisattivareList.Add(pot.PotId);
}

if (vasiDaDisattivare > 0)
{
    string message = $"⚠️ WAT-RAW insufficiente. {vasiDaDisattivare} sistemi irrigazione verranno disattivati.";
    
    // Emetti evento per UI (mostra toast warning)
    if (_uiNotification != null)
    {
        _uiNotification.ShowNotification(message, 3f, Color.yellow);
    }
}
```

### **3. Aggiunto Supporto UINotification**

Aggiunto campo `_uiNotification` e metodo `TryGetUINotification()` per mostrare toast warning:

```csharp
private UINotification _uiNotification;

private void TryGetUINotification()
{
    // Prova prima dal ServiceContainer
    if (ServiceContainer.Instance != null)
    {
        try
        {
            _uiNotification = ServiceContainer.Instance.Get<UINotification>(suppressWarning: true);
            if (_uiNotification != null)
                return;
        }
        catch { }
    }
    
    // Fallback: cerca nella scena
    _uiNotification = Object.FindObjectOfType<UINotification>();
}
```

---

## 📝 FILE MODIFICATI

- `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`
  - Modificato `ApplyWateringSystemEffects()` per controllare WAT-RAW PRIMA di applicare effetti
  - Modificato `CheckWateringSystemResources()` per verificare WAT-RAW indipendentemente dall'accumulatore
  - Aggiunto campo `_uiNotification` e metodo `TryGetUINotification()`
  - Aggiornato `OnServiceRegistered()` per collegare UINotification quando registrato

---

## ✅ TEST DI VERIFICA

### **Test 1: Sistema Disattivato Immediatamente**
1. Attiva sistema irrigazione su 1 vaso (ON)
2. Rimuovi tutto WAT-RAW dall'inventario
3. Avvia End Day
4. **Verifica:**
   - Sistema si disattiva immediatamente
   - Toast mostra: "Sistema disattivato: WAT-RAW insufficiente"
   - Idratazione NON aumenta
   - Accumulatore resettato a 0

### **Test 2: Warning Preventivo**
1. Attiva sistema irrigazione su 3 vasi (ON)
2. Rimuovi tutto WAT-RAW dall'inventario
3. Avvia End Day
4. **Verifica:**
   - Warning toast: "⚠️ WAT-RAW insufficiente. 3 sistemi irrigazione verranno disattivati."
   - Tutti i sistemi si disattivano
   - Toast mostra: "Sistema disattivato: WAT-RAW insufficiente" (per ogni vaso)

### **Test 3: Accumulatore < 1.0**
1. Attiva sistema irrigazione su 1 vaso (ON)
2. Accumulatore = 0.5 (dopo 1 giorno)
3. Rimuovi tutto WAT-RAW dall'inventario
4. Avvia End Day
5. **Verifica:**
   - Sistema si disattiva immediatamente (anche se accumulatore < 1.0)
   - Idratazione NON aumenta
   - Toast mostra messaggio di disattivazione

### **Test 4: Funzionamento Normale**
1. Attiva sistema irrigazione su 1 vaso (ON)
2. Assicurati di avere WAT-RAW disponibile
3. Avvia End Day per 2 giorni
4. **Verifica:**
   - Giorno 1: Accumulatore = 0.5, idratazione +1, nessun consumo WAT-RAW
   - Giorno 2: Accumulatore = 1.0, idratazione +1, consumo 1 WAT-RAW
   - Sistema rimane ON

---

## 🔍 NOTE TECNICHE

- Il controllo WAT-RAW viene fatto **prima** di accumulare e applicare effetti
- Il sistema si disattiva **immediatamente** se WAT-RAW non è disponibile
- Il warning viene mostrato **indipendentemente dall'accumulatore**
- Il toast warning viene mostrato tramite `UINotification` se disponibile

---

**Fix completato:** 2025-12-09  
**Pronto per testing**

