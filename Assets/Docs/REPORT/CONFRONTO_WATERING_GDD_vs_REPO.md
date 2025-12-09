# CONFRONTO WATERING: GDD vs REPOSITORY (AGGIORNATO)

**Data Analisi:** 2025-12-09  
**Versione GDD:** 40 v.08/12/2025 (aggiornato)  
**Azione Analizzata:** AZ-11 — Watering (Sistema Irrigazione)

---

## 📋 PANORAMICA

Il sistema **Watering** descritto nel GDD è **architetturalmente diverso** dall'implementazione attuale nel repository. Il GDD definisce un sistema di **irrigazione persistente a toggle ON/OFF con consumo giornaliero**, mentre il repository implementa un sistema di **azione istantanea con consumo per click**.

---

## 🎯 GDD — AZ-11: WATERING (Sistema Irrigazione a Goccia Automatico) - AGGIORNATO

### **Definizione GDD:**
> **AZ-11 — Watering · Sistema Irrigazione a Goccia Automatico [Azione generica]**

### **Caratteristiche GDD (Versione Aggiornata):**

#### **1. Tipo di Interazione**
- **Toggle ON/OFF** per singolo vaso
- Sistema automatico di irrigazione a goccia
- **Persistente**: lo stato rimane fino a quando il giocatore non lo cambia
- **Opera a fine giornata**: gli effetti vengono calcolati solo a End of Day

#### **2. Funzionamento**
- **ON**: +25% idratazione automatica applicata alla pianta **a fine giornata**
- **OFF**: -25% riduzione idratazione per giorno dovuta all'evaporazione naturale

#### **3. Costi**
- **1 Azione** per aprire o chiudere il sistema di un vaso (toggle ON/OFF)
- **Consumo risorse giornaliero per ogni vaso ON**:
  - **0.5 WAT-RAW** a fine giornata
  - **2 CRY** a fine giornata
  - Esempio: 4 vasi attivi = 2 WAT-RAW + 8 CRY/giorno
- **Vasi OFF non consumano risorse**

#### **4. Strategia**
- Il giocatore deve ricordare di configurare ogni vaso in base allo stadio di crescita
- Lasciare il sistema aperto su piante che non necessitano acqua può causare:
  - Saturazione
  - Muffe
  - Drift pH negativo
  - Consumo inutile di risorse (0.5 WAT-RAW + 2 CRY per vaso)

#### **5. HUD**
- Indicatore visivo ON/OFF per ogni vaso
- Toast di conferma al cambio stato

#### **6. Note GDD**
- Sostituisce completamente la precedente logica **MG‑04** (minigioco hold-to-water)
- Sistema semplificato coerente con **Micro-Operations 2.0**
- Sistema persistente che si **accumula a fine giornata** in base a quanti giorni l'impianto è rimasto ON/OFF

---

## 💻 REPOSITORY — Implementazione Attuale

### **File Principali:**
- `Assets/_Project/Scripts/Dome/PotActions.cs` (metodo `DoWater()`)
- `Assets/_Project/Scripts/UI/VaultMap/Watering/WateringMinigame.cs`
- `Assets/_Project/Scripts/Dome/PotStateModel.cs` (proprietà `Hydration`, `LastWateredDay`)

### **Caratteristiche Implementazione:**

#### **1. Tipo di Interazione**
- **Azione istantanea** (click singolo)
- **Effetto immediato**: l'idratazione aumenta subito
- **Non persistente**: ogni click è un'azione separata

#### **2. Funzionamento**
```csharp
// PotActions.DoWater()
- Consuma Items.Water dall'inventario (1 unità intera)
- Aumenta Hydration di +1 (non +25%)
- MaxHydration = 4 (quindi +1 = 25% se max=4)
- Overwatering detection: se Hydration >= max-1 → pH -5
- Imposta LastWateredDay = CurrentDay (timestamp per crescita)
```

#### **3. Costi**
- **1 Azione** (consumata da GameManager)
- **1 CRY** (consumato da TryConsumeResources)
- **1 Items.Water** (consumato dall'inventario) - **consumo immediato per click**

#### **4. Decay Idratazione**
```csharp
// DayCycleController.ApplyDecayAndCleanup()
- Hydration -= dailyHydrationDecay (default: -1)
- Applicato a fine giornata
- Indipendente da toggle ON/OFF
```

#### **5. Minigioco Opzionale**
- `WateringMinigame.cs` presente nel repository
- Minigioco di pittura del terreno (coverage > 50% = successo)
- **Non obbligatorio**: può essere bypassato

#### **6. Overwatering Detection**
```csharp
// PotActions.DoWater()
bool isOverwatering = _potState.Hydration >= maxHydration - 1;
if (isOverwatering && _phSystem != null)
{
    _phSystem.RegisterActionDrift(-5f, "Overwatering", potSlot.PotId);
}
```

---

## ⚠️ DIFFERENZE PRINCIPALI

### **1. ARCHITETTURA DEL SISTEMA**

| Aspetto | GDD | REPOSITORY |
|---------|-----|------------|
| **Tipo** | Toggle persistente ON/OFF | Azione istantanea click |
| **Timing** | Effetti a fine giornata | Effetti immediati |
| **Persistenza** | Stato mantiene fino a cambio | Ogni click è separato |
| **Accumulo** | Si accumula a fine giornata | Non si accumula |

### **2. MECCANICA IDRATAZIONE**

| Aspetto | GDD | REPOSITORY |
|---------|-----|------------|
| **ON**: Effetto | +25% a fine giornata | +1 punto immediato (25% se max=4) |
| **OFF**: Effetto | -25% a fine giornata | Decay -1 a fine giornata (sempre) |
| **Calcolo** | Basato su giorni consecutivi ON/OFF | Basato su azioni istantanee |
| **Saturazione** | Gestita da toggle OFF | Gestita da overwatering detection |

### **3. COSTI E RISORSE** ⚠️ **DIFFERENZA CRITICA**

| Aspetto | GDD | REPOSITORY |
|---------|-----|------------|
| **Costo Azioni** | 1 Azione per toggle ON/OFF | 1 Azione per click |
| **Costo CRY** | **2 CRY/giorno per vaso ON** (a fine giornata) | **1 CRY per click** (immediato) |
| **Consumo Acqua** | **0.5 WAT-RAW/giorno per vaso ON** (a fine giornata) | **1 Items.Water per click** (immediato) |
| **Frequenza** | Una volta per configurare (toggle) | Ogni volta che si annaffia (click) |
| **Consumo Vasi OFF** | **Nessun consumo** | N/A (sistema non ha toggle) |

**Esempio Pratico:**
- **GDD**: 4 vasi ON = 2 WAT-RAW + 8 CRY/giorno (consumo a fine giornata)
- **REPO**: 4 annaffiature = 4 WAT-RAW + 4 CRY (consumo immediato per click)

### **4. STRATEGIA DI GAMEPLAY**

| Aspetto | GDD | REPOSITORY |
|---------|-----|------------|
| **Pianificazione** | Configurare al mattino, effetti a fine giornata | Azione reattiva immediata |
| **Gestione Risorse** | Consumo prevedibile (0.5 WAT-RAW + 2 CRY per vaso ON) | Consumo variabile (dipende da quante volte si annaffia) |
| **Rischi** | Dimenticare toggle OFF → consumo inutile + saturazione | Overwatering → pH -5 |
| **Memoria** | Deve ricordare di configurare | Deve ricordare di annaffiare |

### **5. INTEGRAZIONE CON ALTRI SISTEMI**

| Aspetto | GDD | REPOSITORY |
|---------|-----|------------|
| **pH Drift** | Implicito (saturazione → pH negativo) | Esplicito (overwatering → pH -5) |
| **Crescita** | Basata su giorni ON/OFF | Basata su timestamp LastWateredDay |
| **Minigioco** | **Deprecato (MG-04)** | **Presente (opzionale)** |
| **Consumo Risorse** | **Giornaliero a fine giornata** | **Immediato per click** |

---

## 🔍 ANALISI DETTAGLIATA

### **GDD: Sistema Persistente con Consumo Giornaliero**

Il GDD descrive un sistema dove:
1. Il giocatore **configura** il sistema di irrigazione (toggle ON/OFF) - **1 Azione per toggle**
2. Il sistema **opera automaticamente** durante la giornata
3. A **fine giornata**, gli effetti vengono calcolati:
   - Se ON: +25% idratazione + consumo 0.5 WAT-RAW + 2 CRY
   - Se OFF: -25% idratazione (evaporazione) + nessun consumo
4. Il sistema si **accumula** in base a quanti giorni è rimasto ON/OFF

**Vantaggi GDD:**
- Gestione strategica a lungo termine
- Consumo prevedibile e controllabile
- Sistema automatico che richiede pianificazione
- Consumo efficiente (0.5 WAT-RAW invece di 1 intero)
- Coerente con filosofia "Micro-Operations 2.0"

**Esempio GDD:**
- Giorno 1: Attivo toggle ON su 4 vasi (4 Azioni spese)
- Giorno 1-7: Sistema opera automaticamente
- Fine ogni giornata: 2 WAT-RAW + 8 CRY consumati
- Totale 7 giorni: 14 WAT-RAW + 56 CRY (consumo automatico)

### **REPO: Sistema Istantaneo con Consumo per Click**

Il repository implementa:
1. Il giocatore **clicca** per annaffiare
2. L'effetto è **immediato** (+1 idratazione)
3. **Consuma risorse immediatamente** (1 Azione + 1 CRY + 1 Water per click)
4. Timestamp per crescita giornaliera

**Vantaggi REPO:**
- Feedback immediato
- Controllo diretto del giocatore
- Sistema più semplice da implementare
- Overwatering detection esplicita

**Esempio REPO:**
- Giorno 1: 4 click per annaffiare 4 vasi (4 Azioni + 4 CRY + 4 Water)
- Giorno 2: 4 click per annaffiare 4 vasi (4 Azioni + 4 CRY + 4 Water)
- Totale 7 giorni: 28 Azioni + 28 CRY + 28 Water (se annaffi ogni giorno)

---

## 📊 TABELLA COMPARATIVA COMPLETA

| Caratteristica | GDD (AZ-11) | REPOSITORY (DoWater) | Gap |
|----------------|-------------|----------------------|-----|
| **Tipo Interazione** | Toggle ON/OFF | Click istantaneo | ❌ **DIVERSA** |
| **Timing Effetti** | Fine giornata | Immediato | ❌ **DIVERSA** |
| **Persistenza** | Stato mantiene | Ogni click separato | ❌ **DIVERSA** |
| **Idratazione ON** | +25% a fine giornata | +1 punto immediato | ⚠️ **SIMILE** (se max=4) |
| **Idratazione OFF** | -25% a fine giornata | -1 punto a fine giornata | ⚠️ **SIMILE** |
| **Costo Azioni** | 1 per toggle | 1 per click | ✅ **UGUALE** |
| **Costo CRY** | **2 CRY/giorno per vaso ON** | **1 CRY per click** | ❌ **DIVERSA** |
| **Consumo Acqua** | **0.5 WAT-RAW/giorno per vaso ON** | **1 Items.Water per click** | ❌ **DIVERSA** |
| **Timing Consumo** | **A fine giornata** | **Immediato** | ❌ **DIVERSA** |
| **Consumo Vasi OFF** | **Nessuno** | N/A | ❌ **DIVERSA** |
| **Overwatering** | Saturazione (implicita) | pH -5 (esplicita) | ⚠️ **SIMILE** |
| **Minigioco** | **Deprecato** | **Presente (opzionale)** | ❌ **DIVERSA** |
| **Accumulo Giorni** | Sì (giorni ON/OFF) | No | ❌ **DIVERSA** |

---

## 🎯 CONCLUSIONI

### **Differenze Critiche:**

1. **❌ ARCHITETTURA COMPLETAMENTE DIVERSA**
   - GDD: Sistema persistente toggle ON/OFF
   - REPO: Sistema istantaneo click

2. **❌ CONSUMO RISORSE - QUANTITÀ E TIMING**
   - GDD: **0.5 WAT-RAW + 2 CRY/giorno per vaso ON** (a fine giornata)
   - REPO: **1 Items.Water + 1 CRY per click** (immediato)

3. **❌ TIMING EFFETTI**
   - GDD: Effetti a fine giornata
   - REPO: Effetti immediati

4. **❌ FREQUENZA CONSUMO**
   - GDD: Consumo giornaliero automatico (una volta al giorno)
   - REPO: Consumo per ogni click (può essere multiplo al giorno)

5. **⚠️ MINIGIOCO**
   - GDD: Deprecato (MG-04)
   - REPO: Presente (opzionale)

### **Punti in Comune:**

1. **✅ Costo Azioni**: 1 Azione in entrambi (ma per scopi diversi: toggle vs click)
2. **⚠️ Idratazione**: Simile se MaxHydration=4 (25% per azione)
3. **⚠️ Decay**: Simile (-1 punto a fine giornata)

### **Impatto Economico:**

**Scenario: 4 vasi attivi per 7 giorni**

- **GDD**:
  - Azioni: 4 (solo per toggle iniziale)
  - WAT-RAW: 14 (0.5 × 4 vasi × 7 giorni)
  - CRY: 56 (2 × 4 vasi × 7 giorni)

- **REPO** (se annaffi ogni giorno):
  - Azioni: 28 (4 click × 7 giorni)
  - Water: 28 (1 × 4 click × 7 giorni)
  - CRY: 28 (1 × 4 click × 7 giorni)

**Differenza**: Il sistema REPO consuma **2x più acqua** e richiede **7x più azioni**, ma consuma **2x meno CRY** rispetto al GDD.

---

## 🔧 RACCOMANDAZIONI

### **Opzione 1: Allineare REPO al GDD** (Raccomandato)
Implementare sistema toggle persistente:
- Aggiungere stato `WateringSystemOn` in `PotStateModel`
- Calcolare effetti e consumo a fine giornata in `DayCycleController`
- Consumo: 0.5 WAT-RAW + 2 CRY per vaso ON (a fine giornata)
- Rimuovere consumo immediato per click
- Aggiungere UI toggle ON/OFF
- Rimuovere minigioco (deprecato)

### **Opzione 2: Sistema Ibrido**
Combinare entrambi:
- Toggle ON/OFF per sistema automatico (0.5 WAT-RAW + 2 CRY/giorno)
- Click manuale per annaffiatura extra (1 Water + 1 CRY per click)
- Il click manuale bypassa il sistema automatico per quel giorno

### **Opzione 3: Aggiornare GDD**
Se il sistema istantaneo è preferito:
- Aggiornare GDD per riflettere consumo per click
- Documentare sistema istantaneo come design finale
- Rimuovere riferimento a toggle persistente

---

## 📝 NOTE FINALI

Il sistema attuale nel repository è **funzionalmente diverso** dal GDD aggiornato. Mentre il GDD descrive un sistema di **configurazione persistente** con **consumo giornaliero efficiente** (0.5 WAT-RAW + 2 CRY per vaso), il repository implementa un sistema di **azione diretta** con **consumo immediato** (1 Water + 1 CRY per click).

**La differenza principale è:**
- **GDD**: Consumo **giornaliero automatico** basato su toggle (più efficiente in acqua, più costoso in CRY)
- **REPO**: Consumo **immediato per click** (meno efficiente in acqua, meno costoso in CRY)

**La scelta di quale sistema mantenere dipende dalla visione di design finale del gioco:**
- **GDD**: Enfatizza pianificazione strategica e gestione risorse a lungo termine
- **REPO**: Enfatizza controllo diretto e feedback immediato

---

**Documento generato:** 2025-12-09  
**Versione GDD analizzata:** 40 v.08/12/2025 (aggiornato)  
**Sezione GDD:** AZ-11 — Watering (Sezione 6 — AZIONI Esistenti Lista & Dettagli)  
**Cambiamento principale:** GDD ora specifica consumo giornaliero (0.5 WAT-RAW + 2 CRY per vaso ON)

