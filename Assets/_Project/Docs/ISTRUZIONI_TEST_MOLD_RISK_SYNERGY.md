# Istruzioni Testing - Sistema Mold Risk Synergy (EVIL/PURE)

**Data Creazione**: 2026-01-XX  
**Stato**: ✅ Implementato  
**Sistema**: Mold Risk Synergy con famiglie EVIL e PURE

---

## 📋 Overview

Il sistema Mold Risk Synergy introduce meccaniche differenziate per le famiglie di piante:
- **EVIL**: Prospera con muffe (bonus crescita/resa), non bloccata da Mold Risk
- **PURE**: Soffre doppiamente (penalità crescita/resa), bloccata a Mold Risk Level ≥1
- **Standard**: Comportamento attuale (bloccata a Level ≥2)

---

## 🧪 TEST 1: Modificatori Crescita/Resa

### Obiettivo
Verificare che i modificatori crescita e resa funzionino correttamente per EVIL e PURE con diversi livelli di Mold Risk.

### Setup
1. Apri Unity e carica la scena principale
2. Aggiungi il componente `MoldRiskSynergyTest` a un GameObject nella scena
3. Apri la **Console Unity** per vedere i log

### Esecuzione
1. Click destro sul componente `MoldRiskSynergyTest` → **"Test 1: Mold Growth/Yield Modifiers"**
2. Oppure usa il Context Menu: **"Run All Mold Risk Synergy Tests"**

### Risultati Attesi

**EVIL:**
- Mold Risk L1-L2: +20% crescita (moltiplicatore 1.20)
- Mold Risk L3: +30% crescita (moltiplicatore 1.30)
- Mold Risk L3 + pH Basico: +40% crescita (moltiplicatore 1.40) - sinergia doppia
- Infestata: +50% resa (moltiplicatore 1.50)
- Non infestata L3: +20% resa (moltiplicatore 1.20)

**PURE:**
- Mold Risk L1-L2: -20% crescita (moltiplicatore 0.80)
- Mold Risk L3: -30% crescita (moltiplicatore 0.70)
- Mold Risk L3 + pH Acido: -40% crescita (moltiplicatore 0.60) - sinergia doppia
- Mold Risk L3: -50% resa (moltiplicatore 0.50)

**Standard:**
- Nessun modificatore (moltiplicatore 1.00)

### Verifica Manuale (Opzionale)
1. Pianta una pianta **EVIL** in un vaso
2. Porta Mold Risk a **Level 3** (overwatering prolungato)
3. Avanza 1 giorno e controlla i log `[GROWTH_MODIFIER]`
4. Verifica che il moltiplicatore totale includa il bonus Mold Risk
5. Ripeti con una pianta **PURE** e verifica le penalità

---

## 🧪 TEST 2: Blocco Crescita Differenziato

### Obiettivo
Verificare che il blocco crescita per Mold Risk sia differenziato per famiglia.

### Esecuzione
1. Click destro sul componente → **"Test 2: Mold Growth Block By Family"**

### Risultati Attesi

**EVIL:**
- Mold Risk L0-L3: **NON bloccata** (false)

**PURE:**
- Mold Risk L0: **NON bloccata** (false)
- Mold Risk L1-L3: **Bloccata** (true)

**Standard:**
- Mold Risk L0-L1: **NON bloccata** (false)
- Mold Risk L2-L3: **Bloccata** (true)

### Verifica Manuale (Opzionale)
1. Pianta una pianta **EVIL** e porta Mold Risk a **Level 3**
2. Verifica che la pianta **possa ancora avanzare** di stadio
3. Pianta una pianta **PURE** e porta Mold Risk a **Level 1**
4. Verifica che la pianta **NON possa avanzare** di stadio
5. Pianta una pianta **Standard** e porta Mold Risk a **Level 2**
6. Verifica che la pianta **NON possa avanzare** di stadio

---

## 🧪 TEST 3: Infestazione Differenziata

### Obiettivo
Verificare che l'infestazione applichi riduzioni livello diverse per famiglia.

### Esecuzione
1. Click destro sul componente → **"Test 3: Mold Infestation By Family"**

### Risultati Attesi

**EVIL:**
- Infestazione Mild (L1): **-0 livelli** (nessuna riduzione)
- Infestazione Severe (L3): **-1 livello** (riduzione minore)

**PURE:**
- Infestazione Mild (L1): **-2 livelli** (riduzione maggiore anche per Mild)
- Infestazione Severe (L3): **-5 livelli** (riduzione maggiore)

**Standard:**
- Infestazione Mild (L1): **-1 livello** (standard)
- Infestazione Severe (L3): **-3 livelli** (standard)

### Verifica Manuale (Opzionale)
1. Pianta una pianta **EVIL** e porta a **Level 5**
2. Porta Mold Risk a **Level 3** e aspetta infestazione
3. Verifica che il livello scenda a **4** (riduzione -1)
4. Ripeti con una pianta **PURE** a **Level 5**
5. Verifica che il livello scenda a **0** (riduzione -5)

---

## 🎯 Test Completo

### Esecuzione Tutti i Test
1. Click destro sul componente → **"Run All Mold Risk Synergy Tests"**
2. Controlla la Console per i risultati

### Interpretazione Risultati
- ✅ **Verde**: Test passato
- ❌ **Rosso**: Test fallito (controlla i log per dettagli)

### Risultato Finale
- **Tutti i test passati**: Sistema funziona correttamente
- **Alcuni test falliti**: Controlla i log per identificare il problema

---

## 📝 Note Importanti

1. **Test Unitari vs Integrazione**: Questi test verificano la logica dei modificatori, non l'integrazione completa con il sistema di crescita
2. **Test Manuali**: Per testare l'integrazione completa, usa i test manuali descritti sopra
3. **Log Debug**: Attiva `showDetailedLogs` per vedere tutti i dettagli dei test
4. **Context Menu**: Tutti i test sono disponibili tramite Context Menu (click destro sul componente)

---

## 🔍 Troubleshooting

### Test Falliscono
1. Verifica che `PhGrowthModifier` e `MoldSystem` siano correttamente implementati
2. Controlla che i valori attesi nei test corrispondano all'implementazione
3. Verifica che non ci siano errori di compilazione

### Test Non Appaiono
1. Verifica che il componente `MoldRiskSynergyTest` sia aggiunto a un GameObject
2. Controlla che il file `.cs` sia stato compilato correttamente
3. Ricompila il progetto se necessario

---

## 📚 Riferimenti

- **File Test**: `Assets/_Project/Scripts/Dome/MoldRiskSynergyTest.cs`
- **File Modificatori**: `Assets/_Project/Scripts/Dome/PotSystem/Growth/PhGrowthModifier.cs`
- **File Sistema Muffe**: `Assets/_Project/Scripts/Dome/PotSystem/Mold/MoldSystem.cs`
- **Piano Implementazione**: `.cursor/plans/da fare/mold_risk_synergy_evil_pure_de60b149.plan.md`
