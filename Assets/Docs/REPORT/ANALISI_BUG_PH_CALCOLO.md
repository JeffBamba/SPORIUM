# ANALISI: BUG Calcolo pH Generale

**Data:** 2025-12-09  
**Problema:** Il calcolo del pH totale si sballa quando viene rimosso l'overwatering

---

## 🐛 PROBLEMA OSSERVATO

**Scenario:**
- Giorno 2: pH = +2, idratazione = 25%
- Giorno 3: pH = +4, idratazione = 50%
- Giorno 4: pH = 0 (overwatering -5 + drift pianta +2 = -3, ma mostra 0)
- Giorno 5: pH = +2.5 (dovrebbe essere -3: overwatering -5 + drift pianta +2 = -3)
- Giorno 6: pH = +9.5 (overwatering rimosso, ma pH schizza in alto)

**Problemi identificati:**
1. Il pH non riflette correttamente la somma dei contributi
2. Quando viene rimosso l'overwatering, il pH viene corretto in modo errato
3. Il fallback del sistema irrigazione non viene attivato correttamente quando WAT-RAW finisce

---

## 🔍 ANALISI TECNICA

### **Ordine Operazioni End Day:**
1. `CheckWateringSystemResources()` - Warning preventivo
2. `ResolveGrowthForAllPots()` - Calcola crescita
3. `ApplyWateringSystemEffects()` - Applica effetti watering (qui viene rimosso/aggiunto overwatering)
4. `CalculateAndRegisterPhDrift()` - Registra drift delle piante
5. `ApplyDecayAndCleanup()` - Decay naturale

### **Problema nella Rimozione Overwatering:**

Quando viene rimosso l'overwatering in `RemoveActionContribution()`:
```csharp
_currentPh = Mathf.Clamp(_currentPh - totalDriftToRemove, MIN_PH, MAX_PH);
```

Dove `totalDriftToRemove` è negativo (es. -5), quindi:
`_currentPh = _currentPh - (-5) = _currentPh + 5` ✅

**MA** il problema è che `_currentPh` potrebbe non essere sincronizzato con la somma dei contributi (`BasePh + PlantsDrift + ActionsDrift + EventsDrift + DailyDrift`).

Se `_currentPh` non è sincronizzato, la correzione è sbagliata.

---

## ✅ SOLUZIONE PROPOSTA

Il problema principale è che `_currentPh` non è sempre uguale alla somma dei contributi. Quando rimuovo l'overwatering, devo correggere `_currentPh` in modo che rifletta la rimozione del contributo, ma se `_currentPh` non è sincronizzato, la correzione è sbagliata.

**Soluzione:** Quando rimuovo l'overwatering, devo solo correggere `_actionsDrift` e poi aggiungere il valore positivo al pH (per annullare il negativo). Ma devo assicurarmi che `_currentPh` sia sincronizzato.

**Alternativa:** Ricalcolare `_currentPh` dalla somma dei contributi dopo ogni modifica, ma questo richiederebbe una refactorizzazione importante.

---

## 📝 NOTE

- Il problema potrebbe essere anche nell'ordine delle operazioni
- Quando viene registrato il drift della pianta dopo aver rimosso l'overwatering, potrebbe causare problemi di sincronizzazione
- Il fallback del sistema irrigazione potrebbe non essere attivato correttamente quando WAT-RAW finisce

---

**Analisi completata:** 2025-12-09  
**In attesa di fix**

