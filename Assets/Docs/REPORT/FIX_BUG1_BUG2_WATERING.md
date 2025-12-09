# FIX: BUG1 e BUG2 - Sistema Watering

**Data:** 2025-12-09  
**Bug Fixati:** 2

---

## 🐛 BUG1: Messaggio Errato quando WAT-RAW Insufficiente

### **Problema**
Quando finisce RAW WATER e il sistema irrigazione viene disattivato automaticamente, il messaggio mostrato era:
```
"You failed to water the plant"
```
Invece del messaggio concordato:
```
"Sistema disattivato: WAT-RAW insufficiente"
```

### **Causa**
Il metodo `HandlePotFailed` in `PotNotifications.cs` ignorava il parametro `message` e usava sempre un messaggio generico hardcoded.

### **Soluzione**
Modificato `PotNotifications.HandlePotFailed()` per usare il messaggio specifico passato come parametro quando disponibile:

```csharp
private void HandlePotFailed(PotEvents.PotActionType type, PotSlot pot, string message)
{
    string text;
    
    // GDD AZ-11: Usa il messaggio specifico se disponibile, altrimenti usa messaggio generico
    if (!string.IsNullOrEmpty(message))
    {
        text = message;
    }
    else
    {
        // Fallback a messaggi generici se message è vuoto
        text = type switch
        {
            PotEvents.PotActionType.Light => "You cannot illuminate the plant.",
            PotEvents.PotActionType.Plant => "You cannot plant the plant.",
            PotEvents.PotActionType.Water => "You failed to water the plant",
            _ => "You cannot uproot the plant."
        };
    }

    _notification.ShowNotification(text, 2, Color.red);
}
```

### **File Modificato**
- `Assets/_Project/Scripts/UI/VaultMap/Pot/PotNotifications.cs`

---

## 🐛 BUG2: Conteggio pH Generale Funzionava Male

### **Problema**
Il conteggio del pH generale mostrava valori errati quando venivano rimossi contributi di overwatering.

### **Causa**
Quando veniva chiamato `RemoveActionContribution`, il codice:
1. Sottraeva manualmente `totalDriftToRemove` da `_actionsDrift`
2. Poi chiamava `ApplyInstantDelta(-totalDriftToRemove, ...)`
3. `ApplyInstantDelta` chiamava `TrackContribution` che aggiungeva di nuovo il delta a `_actionsDrift`

Questo causava una **doppia sottrazione** da `_actionsDrift`, rendendo il conteggio errato.

**Esempio:**
- Overwatering registrato: -5, -5, -5 = -15 totale
- `_actionsDrift` = -15
- Quando rimuovo: 
  - Sottraggo manualmente: `_actionsDrift = -15 - (-15) = 0` ✅
  - Chiamo `ApplyInstantDelta(-(-15), ...)` = `ApplyInstantDelta(+15, ...)`
  - `TrackContribution` aggiunge: `_actionsDrift = 0 + 15 = +15` ❌ (dovrebbe rimanere 0)

### **Soluzione**
Modificato `PhSystem.RemoveActionContribution()` per gestire manualmente la sottrazione da `_actionsDrift` e l'aggiornamento di `_currentPh` senza passare per `TrackContribution`:

```csharp
// BUG2 FIX: Sottrai manualmente da _actionsDrift PRIMA di chiamare ApplyInstantDelta
// perché TrackContribution aggiunge il delta, ma noi vogliamo rimuovere il contributo
float oldActionsDrift = _actionsDrift;
_actionsDrift -= totalDriftToRemove;

// Applica correzione istantanea al pH (ma NON tracciare come action per evitare doppia sottrazione)
// Usiamo un source che NON viene tracciato come action
float oldPh = _currentPh;
_currentPh = Mathf.Clamp(_currentPh - totalDriftToRemove, MIN_PH, MAX_PH);
float actualDelta = _currentPh - oldPh;

// Notifica il cambio pH
OnPhChanged?.Invoke(CurrentPh, actualDelta);
```

### **File Modificato**
- `Assets/_Project/Scripts/Core/PhSystem.cs`

### **Note Aggiuntive**
- Aumentato limite `_actionContributions` da 20 a 50 per gestire più vasi con overwatering simultaneo
- Aggiunto log dettagliato per debug del pH drift

---

## ✅ TEST DI VERIFICA

### **Test BUG1:**
1. Attiva sistema irrigazione su 4 vasi (ON)
2. Rimuovi tutto WAT-RAW dall'inventario
3. Avvia End Day
4. **Verifica:** Toast mostra "Sistema disattivato: WAT-RAW insufficiente" (non "You failed to water the plant")

### **Test BUG2:**
1. Attiva sistema irrigazione su 1 vaso
2. Avvia End Day per 3 giorni (idratazione sale a 4/4 = overwatering)
3. **Verifica:** pH diminuisce di -5 per ogni giorno = -15 totale
4. Disattiva sistema irrigazione
5. Avvia End Day (idratazione scende sotto 50%)
6. **Verifica:** 
   - pH aumenta di +15 (correzione completa)
   - `_actionsDrift` torna a 0
   - Breakdown pH mostra correttamente i contributi

### **Test Multi-Vaso:**
1. Attiva sistema irrigazione su 3 vasi
2. Avvia End Day fino a overwatering su tutti e 3
3. **Verifica:** pH diminuisce di -5 × 3 = -15 totale (un contributo per vaso)
4. Disattiva tutti i sistemi
5. Avvia End Day
6. **Verifica:** pH aumenta di +15 (correzione completa per tutti i vasi)

---

## 📝 NOTE TECNICHE

- Il sistema ora gestisce correttamente l'accumulo di overwatering per vaso
- La rimozione del contributo overwatering è corretta e sincronizzata con `_actionsDrift`
- Il messaggio di errore è ora localizzato e specifico per il contesto

---

**Fix completato:** 2025-12-09  
**Pronto per testing**

