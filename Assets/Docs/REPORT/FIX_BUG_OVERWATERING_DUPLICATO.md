# FIX: BUG - Overwatering Duplicato per lo Stesso Vaso

**Data:** 2025-12-09  
**Bug:** Il sistema contava 2 (o più) contributi overwatering per lo stesso vaso invece di 1

---

## 🐛 PROBLEMA

Il conteggio del pH generale mostrava **2 overwatering per lo stesso POT** quando invece dovrebbe essercene **solo 1**. Non si possono avere 2 overwatering per un singolo POT.

### **Causa**
Quando `RegisterActionDrift` veniva chiamato per registrare overwatering, aggiungeva sempre una **nuova entry** alla lista `_actionContributions`, anche se esisteva già un contributo overwatering per quel vaso. Questo causava duplicati se il sistema veniva chiamato più volte per lo stesso vaso (ad esempio, se l'idratazione rimaneva >= 75% per più giorni consecutivi).

**Esempio del bug:**
- Giorno 1: Idratazione = 3/4 (75%) → Registra overwatering -5 ✅
- Giorno 2: Idratazione = 4/4 (100%) → Registra overwatering -5 (duplicato!) ❌
- **Risultato:** 2 contributi overwatering per lo stesso vaso = -10 pH invece di -5

---

## ✅ SOLUZIONE

L'overwatering è uno **stato binario**: o c'è o non c'è, non può esserci "doppio overwatering" per lo stesso vaso.

**Fix applicato:** Prima di registrare un nuovo contributo overwatering, rimuovere eventuali contributi overwatering esistenti per quel vaso.

### **Modifica in `ApplyWateringSystemEffects()`:**

```csharp
// Applica overwatering se idratazione >= 75%
else if (pot.Hydration >= overwateringThreshold)
{
    // BUG FIX: Rimuovi eventuali contributi overwatering esistenti prima di registrarne uno nuovo
    // L'overwatering è uno stato binario: o c'è o non c'è, non può esserci "doppio overwatering" per lo stesso vaso
    _phSystem.RemoveActionContribution("Overwatering", pot.PotId);
    _phSystem.RegisterActionDrift(-5f, "Overwatering", pot.PotId);
    if (enableDebugLogs)
        Debug.Log($"[DayCycleController] {pot.PotId}: OVERWATERING rilevato! pH -5 applicato (Hydration: {pot.Hydration}/{maxHydration} = {hydrationPercent:F0}%)");
}
```

### **Logica Corretta:**
1. Se idratazione >= 75%: Rimuovi eventuali contributi overwatering esistenti → Registra nuovo contributo -5
2. Se idratazione < 50%: Rimuovi contributo overwatering se presente
3. Se 50% <= idratazione < 75%: Nessuna azione (mantiene stato corrente)

---

## 📝 FILE MODIFICATO

- `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`
  - Modificato `ApplyWateringSystemEffects()` per rimuovere contributi overwatering esistenti prima di registrarne uno nuovo

---

## ✅ TEST DI VERIFICA

### **Test 1: Overwatering Singolo**
1. Attiva sistema irrigazione su 1 vaso (ON)
2. Avvia End Day fino a idratazione = 3/4 (75%)
3. **Verifica:** 1 contributo overwatering -5 per quel vaso
4. Avvia End Day (idratazione = 4/4, 100%)
5. **Verifica:** Ancora 1 contributo overwatering -5 (non duplicato)

### **Test 2: Overwatering Multi-Vaso**
1. Attiva sistema irrigazione su 3 vasi (ON)
2. Avvia End Day fino a tutti e 3 con idratazione >= 75%
3. **Verifica:** 3 contributi overwatering -5 (uno per vaso)
4. **Verifica:** pH totale = -15 (3 × -5)

### **Test 3: Overwatering Rimosso**
1. Vaso con overwatering attivo (idratazione = 4/4)
2. Disattiva sistema irrigazione
3. Avvia End Day (idratazione scende a 3/4, poi 2/4)
4. **Verifica:** Quando idratazione < 50%, contributo overwatering viene rimosso
5. **Verifica:** pH totale corretto (nessun contributo overwatering residuo)

### **Test 4: Overwatering Riattivato**
1. Vaso con overwatering rimosso (idratazione < 50%)
2. Attiva sistema irrigazione
3. Avvia End Day fino a idratazione >= 75%
4. **Verifica:** 1 contributo overwatering -5 registrato (non duplicato)

---

## 🔍 NOTE TECNICHE

- L'overwatering è uno **stato binario** per vaso: o presente o assente
- Non può esserci accumulo di overwatering per lo stesso vaso
- Ogni vaso può avere **al massimo 1 contributo overwatering** alla volta
- Il contributo viene rimosso quando idratazione scende sotto 50%
- Il contributo viene registrato/aggiornato quando idratazione sale sopra 75%

---

**Fix completato:** 2025-12-09  
**Pronto per testing**

