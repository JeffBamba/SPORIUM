# PIANO IMPLEMENTAZIONE - Sistema Crescita Basato su Valori nel Range

**Data Creazione:** 2024-12-19  
**Data Aggiornamento:** 2025-12-10  
**Versione:** 1.5  
**BLK Code:** BLK-03.01 (diviso in BLK-03.01-T1 e BLK-03.01-T2)  
**Stato:** 📋 PIANIFICAZIONE

**Changelog v1.1:**
- ✅ Aggiunti dettagli completi sistema fertilizzanti (3 tipi, costi, percentuali)
- ✅ Specificato range fertilizzante 0-100% per ogni stadio
- ✅ Aggiunta verifica coerenza genetica
- ✅ Aggiunti test scenarios per fertilizzanti

**Changelog v1.2:**
- ✅ Aggiornate regole critiche compatibilità fertilizzanti (morte immediata se incompatibile)
- ✅ Specificati valori fissi fertilizzante per tutti gli stadi (identici per tutte le piante):
  - Seed: 60-75-90
  - Growth: 40-60-80
  - Flowering: 20-40-60
  - HarvestReady: non richiesto
  - Resting: 30-50-70
- ✅ Aggiornata implementazione morte immediata pianta se fertilizzante incompatibile
- ✅ Aggiornati test scenarios con casi di morte immediata

**Changelog v1.3:**
- ✅ **Divisione implementazione in due trance**:
  - **TRANCE 1**: Sistema Fertilizzante (feature completa e isolata)
  - **TRANCE 2**: Sistema Crescita Basato su Valori (refactoring completo)
- ✅ Riorganizzate fasi di implementazione per trance
- ✅ Aggiornata checklist con separazione trance
- ✅ Timeline aggiornata con stime separate per trance
- ✅ Identificate dipendenze tra trance

**Changelog v1.4:**
- ✅ Aggiunti dettagli specifici UI/UX sistema fertilizzante:
  - Bottone singolo nella HUD Piante (pattern Watering/LED)
  - Popup inventario per selezione (pattern SEED/PIANTA)
  - Campo testuale con range ideale e percentuale attuale
- ✅ Specificato che fertilizzante è collegato ai conteggi GROWTH
- ✅ Specificato che fertilizzante NON ha effetto sul pH DRIFT
- ✅ Aggiunto task per creazione ItemConfig fertilizzanti (2x ogni tipo)
- ✅ Aggiornata checklist con dettagli implementazione UI

**Changelog v1.5:**
- ✅ Aggiunta sezione esplicita **"Filosofia di Implementazione: Integrazione Non-Distruttiva"**
- ✅ Chiarito principio fondamentale: integrazione senza rompere funzionalità esistenti
- ✅ Specificata strategia: estensione (non sostituzione), compatibilità retroattiva, struttura mantenuta
- ✅ Aggiunti criteri di verifica non-breaking changes
- ✅ Aggiornata sezione rischi con focus su regressioni funzionalità esistenti

---

## 📋 **PANORAMICA**

Questo piano descrive l'implementazione del nuovo sistema di crescita delle piante basato su:
- **Due mondi separati**: Stadio di crescita vs Condizione della pianta
- **Sistema di punti basato su valori nel range ideale** (non attivazione sistemi)
- **Tracking giorni consecutivi** con parametri ottimali
- **Integrazione condizione → crescita** (rigogliosa -1 giorno, critica blocca)

---

## 🛡️ **FILOSOFIA DI IMPLEMENTAZIONE: INTEGRAZIONE NON-DISTRUTTIVA**

### **Principio Fondamentale**

⚠️ **Questo piano è progettato per INTEGRARSI nel sistema esistente SENZA ROMPERE le funzionalità attuali.**

### **Strategia di Integrazione**

1. **Estensione, Non Sostituzione**:
   - ✅ Aggiungere nuovi campi ai modelli esistenti (non rimuovere)
   - ✅ Creare nuovi sistemi che si integrano con quelli esistenti
   - ✅ Mantenere tutti i metodi e le funzionalità attuali funzionanti
   - ❌ **NON** rimuovere o sostituire logica esistente

2. **Compatibilità Retroattiva**:
   - ✅ Valori default per tutti i nuovi campi
   - ✅ Salvataggi vecchi continuano a funzionare
   - ✅ Migrazione automatica al caricamento se necessario
   - ✅ Sistema esistente continua a funzionare anche senza nuove feature

3. **Struttura Esistente Mantenuta**:
   - ✅ `PotStateModel`: esteso con nuovi campi, struttura esistente invariata
   - ✅ `StageRequirements`: aggiunti nuovi range, metodi esistenti mantenuti
   - ✅ `DayCycleController`: nuova logica aggiunta, logica esistente preservata
   - ✅ `PotActions`: nuovo metodo `DoFertilize()`, metodi esistenti invariati
   - ✅ UI: nuovi elementi aggiunti, elementi esistenti mantenuti

4. **Sistema di Crescita Esistente**:
   - ✅ **TRANCE 1**: Fertilizzante tracciato ma **NON modifica** il sistema crescita attuale
   - ✅ **TRANCE 2**: Refactoring crescita ma con **fallback** al sistema esistente se necessario
   - ✅ Sistema esistente continua a funzionare durante la transizione

5. **Pattern di Integrazione**:
   - ✅ Usare pattern esistenti (es. bottone HUD come Watering/LED)
   - ✅ Usare sistemi esistenti (es. popup inventario come SEED selector)
   - ✅ Seguire architettura esistente (es. PotActions, PotEvents)
   - ✅ Non introdurre nuovi pattern che rompono la coerenza

### **Verifica Non-Breaking Changes**

Ogni modifica deve rispettare:
- ✅ **Test retrocompatibilità**: salvataggi vecchi funzionano
- ✅ **Test funzionalità esistenti**: Watering, LED, Plant, Harvest continuano a funzionare
- ✅ **Test UI esistente**: HUD esistente non viene rotto
- ✅ **Test sistema crescita**: crescita esistente continua a funzionare

### **Gestione Rischi**

- **Rischio Breaking Changes**: Mitigato con valori default e migrazione automatica
- **Rischio Regressioni**: Mitigato con test estensivi su funzionalità esistenti
- **Rischio Incompatibilità**: Mitigato mantenendo struttura esistente invariata

---

## 🎯 **DIVISIONE IN TRANCE**

L'implementazione è stata divisa in **due trance** per ridurre i rischi e permettere un rilascio incrementale:

### **TRANCE 1: Sistema Fertilizzante** 🌿
**BLK Code:** BLK-03.01-T1  
**Priorità:** ALTA  
**Obiettivo:** Implementare il sistema fertilizzante completo e integrarlo con i sistemi esistenti

**Scope:**
- Sistema fertilizzante base (3 tipi, applicazione, decadimento)
- Verifica coerenza genetica con morte immediata se incompatibile
- Range fertilizzante fissi per tutti gli stadi
- Integrazione con sistemi esistenti (inventario, azioni, UI)
- **NON modifica** il sistema di crescita esistente (usa quello attuale)

**Deliverable:**
- ✅ Sistema fertilizzante funzionante e testato
- ✅ Integrazione completa con UI e inventario
- ✅ Documentazione e test manuali

**Vantaggi:**
- Feature completa e rilasciabile indipendentemente
- Test isolato del sistema fertilizzante
- Riduce complessità del refactoring successivo

---

### **TRANCE 2: Sistema Crescita Basato su Valori** 📊
**BLK Code:** BLK-03.01-T2  
**Priorità:** ALTA  
**Dipendenze:** TRANCE 1 completata  
**Obiettivo:** Refactoring completo del sistema di crescita con punti basati su valori

**Scope:**
- Sistema punti giornalieri basato su valori nel range (non attivazione)
- Tracking giorni consecutivi con parametri ottimali
- Integrazione condizione → crescita (modificatori giorni)
- Logica avanzamento stadio completa
- Range luce e fertilizzante in StageRequirements
- Aggiornamento UI per nuovi indicatori

**Deliverable:**
- ✅ Sistema crescita completamente refactorizzato
- ✅ Integrazione fertilizzante nel calcolo punti
- ✅ UI aggiornata con nuovi indicatori
- ✅ Documentazione completa

**Vantaggi:**
- Può utilizzare il sistema fertilizzante già implementato
- Focus esclusivo sul refactoring crescita
- Test più semplici (fertilizzante già validato)

---

## 🌿 **TRANCE 1: SISTEMA FERTILIZZANTE**

### **Panoramica Trance 1**

Questa trance implementa il sistema fertilizzante completo senza modificare il sistema di crescita esistente. Il fertilizzante viene applicato e tracciato, ma **non influisce ancora** sul calcolo dei punti di crescita (questo sarà nella Trance 2).

**Componenti principali:**
1. Sistema fertilizzante base (FertilizerSystem)
2. Azione fertilizzante (DoFertilize)
3. Tracking livello fertilizzante (PotStateModel)
4. Decadimento giornaliero
5. Verifica coerenza genetica con morte immediata
6. Range fertilizzante fissi in StageRequirements
7. Integrazione UI e inventario

---

## 🌿 **SISTEMA FERTILIZZANTI (TRANCE 1)**

### **Tipi di Fertilizzanti**

| Tipo | Costo | Percentuale | Fonti di Acquisto |
|------|-------|-------------|-------------------|
| **Standard** | 25 CRY | +25% | Fazioni neutrali |
| **Puri** | 75 CRY | +40% | Fazione Custode, Mercato Nero |
| **Proibiti** | 75 CRY | +40% | Fazione Culto della Muffa, Mercato Nero |

### **Coerenza Genetica (REGOLA CRITICA)**

⚠️ **L'uso di fertilizzanti incompatibili causa la MORTE IMMEDIATA della pianta!**

#### **Regole di Compatibilità**

| Famiglia Pianta | Fertilizzanti Compatibili | Fertilizzanti Incompatibili (MORTE IMMEDIATA) |
|-----------------|---------------------------|-----------------------------------------------|
| **Standard** | ✅ Solo **Standard** | ❌ **Pure** → MUORE SUBITO<br>❌ **Proibito** → MUORE SUBITO |
| **Pure** | ✅ **Pure**<br>✅ **Standard** | ❌ **Proibito** → MUORE SUBITO |
| **Evil** | ✅ **Proibito**<br>✅ **Standard** | ❌ **Pure** → MUORE SUBITO |

#### **Regola Generale**
- **Standard** = solo Standard (più restrittiva)
- **Pure** = Pure o Standard (tollerante verso Standard)
- **Evil** = Proibito o Standard (tollerante verso Standard)
- **MAI** usare fertilizzante opposto (Pure ↔ Proibito = morte certa)

#### **Implementazione**
Quando si applica un fertilizzante incompatibile:
- 🚨 **Morte immediata** della pianta (rimuove pianta dal vaso)
- ⚠️ Warning log con dettagli incompatibilità
- 📢 Notifica evento morte pianta per UI/feedback

### **Range Fertilizzante per Stadio (VALORI FISSI)**

⚠️ **IMPORTANTE**: I valori di fertilizzante sono **FISSI per tutte le piante** (Standard, Pure, Evil) e **identici per ogni stadio**.

Ogni scheda pianta (`PlantData`) avrà un **range di fertilizzante (0-100%)** necessario per ogni stadio di crescita:
- `fertilizerMin`: Percentuale minima richiesta
- `fertilizerMed`: Percentuale ottimale/mediana
- `fertilizerMax`: Percentuale massima tollerata

#### **Valori Fissi per Tutti gli Stadi**

| Stadio | Min | Opt | Max | Note |
|--------|-----|-----|-----|------|
| **Seed** | 60% | 75% | 90% | Range alto per germinazione |
| **Growth** | 40% | 60% | 80% | Range medio per crescita |
| **Flowering** | 20% | 40% | 60% | Range basso per fioritura |
| **HarvestReady** | — | — | — | Non richiesto (stadio raccolta) |
| **Resting** | 30% | 50% | 70% | Range medio per riattivazione |

**Punto giornaliero**: Il punto per fertilizzante viene assegnato quando `FertilizerLevel` (0-100%) è nel range consigliato per quello stadio (valori fissi sopra).

### **Decadimento**

- **Decadimento giornaliero**: -5% al giorno
- **Clamp**: 0-100% (non può scendere sotto 0 o salire sopra 100%)

### **Transizione Resting → Flowering**

Quando si applica fertilizzante a una pianta in **Resting**:
- ✅ Transizione automatica a **Flowering**
- ✅ Reset contatori giorni

---

## 🎯 **OBIETTIVI**

### Sistema Desiderato

1. **Stadio di Crescita**:
   - Ogni stadio ha fattori per passare al prossimo
   - **1 punto** per water nel range ideale
   - **1 punto** per luce nel range ideale  
   - **1 punto** per fertilizzante nel range ideale
   - **+ tot giorni** in cui mantenere i tre parametri nel range ottimale
   - Avanzamento quando tutti i requisiti sono soddisfatti

2. **Condizione della Pianta**:
   - Impatto solo sui giorni necessari per stadio
   - **Rigogliosa**: -1 giorno (guadagna 1 giorno)
   - **Sana**: giorni normali
   - **Critica/Malata**: non può passare al prossimo stadio a meno che non venga curata

### Differenze Chiave con Sistema Attuale

| Aspetto | Sistema Attuale | Sistema Desiderato |
|---------|----------------|-------------------|
| **Assegnazione Punti** | Quando sistema attivato (`WateringSystemOn = true`) | Quando valore nel range ideale (`hydrationPercent` nel range) |
| **Tracking Parametri** | Solo verifica istantanea | Tracking giorni consecutivi con parametri ottimali |
| **Fertilizzante** | Non implementato | Range ideale + punto giornaliero |
| **Condizione → Crescita** | Separati (non integrati) | Integrati (condizione modifica giorni) |
| **Range Luce** | Solo LED richiesto | Range ideale per luce (da definire) |

---

## 📊 **ANALISI SISTEMI ESISTENTI**

### ✅ **Sistemi Già Implementati**

#### 1. **StageRequirements** (`Assets/_Project/Scripts/Dome/PotSystem/Growth/StageRequirements.cs`)
- ✅ Range idratazione: `hydrationMin`, `hydrationMed`, `hydrationMax`
- ✅ LED richiesto: `requiredLed`
- ✅ Durata giorni: `durationDays`
- ✅ Metodo `IsHydrationOptimal()` già presente
- ❌ **Manca**: Range ideale per luce (oltre LED)
- ❌ **Manca**: Range ideale per fertilizzante

#### 2. **PotStateModel** (`Assets/_Project/Scripts/Dome/PotStateModel.cs`)
- ✅ `Hydration` (valore attuale)
- ✅ `LedSystemState` (stato LED corrente)
- ✅ `DaysInCurrentStage` (giorni nello stadio)
- ✅ `ConditionScore`, `ConditionLabel` (condizione)
- ❌ **Manca**: Tracking fertilizzante
- ❌ **Manca**: Tracking giorni consecutivi con parametri ottimali
- ❌ **Manca**: Punti giornalieri per water/light/fertilizer

#### 3. **PlantConditionSystem** (`Assets/_Project/Scripts/Dome/PotSystem/Condition/PlantConditionSystem.cs`)
- ✅ Calcolo condizione (0-100)
- ✅ Mappatura a Rigogliosa/Sana/Stressata/Appassita/Critica
- ❌ **Manca**: Integrazione con sistema crescita
- ❌ **Manca**: Modifica giorni necessari in base a condizione

#### 4. **DayCycleController** (`Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`)
- ✅ `ResolveGrowthForPot()` - verifica requisiti
- ✅ `CalculatePlantConditions()` - calcola condizione
- ❌ **Manca**: Assegnazione punti basata su valori nel range
- ❌ **Manca**: Tracking giorni consecutivi con parametri ottimali
- ❌ **Manca**: Integrazione condizione → crescita

### ❌ **Sistemi Non Implementati**

1. **Sistema Fertilizzante**:
   - ❌ Tracking livello fertilizzante nel vaso (0-100%)
   - ❌ Range ideale per fertilizzante in `StageRequirements` (0-100% per ogni stadio)
   - ❌ Azione `DoFertilize()` (menzionata ma non implementata)
   - ❌ Decadimento fertilizzante nel tempo
   - ❌ Tre tipi fertilizzanti:
     - **Standard**: 25 CRY, +25% fertilizzante, fazioni neutrali
     - **Puri**: 75 CRY, +40% fertilizzante, Custode + Mercato Nero
     - **Proibiti**: 75 CRY, +40% fertilizzante, Culto Muffa + Mercato Nero
   - ❌ Verifica coerenza genetica (fertilizzante deve corrispondere a famiglia pianta)

2. **Range Ideale Luce**:
   - ❌ Range ideale per luce (oltre al LED richiesto)
   - ❌ Tracking intensità luce nel range ottimale

3. **Sistema Punti Giornalieri**:
   - ❌ Assegnazione punti basata su valori nel range
   - ❌ Tracking giorni consecutivi con tutti i parametri ottimali

---

## 🏗️ **ARCHITETTURA DEL NUOVO SISTEMA**

### **Flusso End Day**

```
GameManager.EndDay()
    ↓
DayCycleController.HandleDayChanged(day)
    ↓
1. CalculatePlantConditions(day)          ← Calcola condizione (esistente)
    ↓
2. ResolveGrowthForAllPots(day)           ← NUOVO: Sistema crescita basato su valori
    ├─> Per ogni vaso:
    │   ├─> Verifica valori attuali (hydration, light, fertilizer)
    │   ├─> Confronta con range ideali da StageRequirements
    │   ├─> Assegna punti giornalieri (1 punto per parametro nel range)
    │   ├─> Incrementa giorni consecutivi con parametri ottimali
    │   ├─> Applica modificatore condizione ai giorni necessari
    │   └─> Verifica avanzamento stadio
    ↓
3. ApplyDecayAndCleanup(day)              ← Decadimento risorse (esistente)
```

### **Logica Avanzamento Stadio**

```csharp
// Per ogni stadio:
int requiredPoints = 3;  // 1 water + 1 light + 1 fertilizer
int requiredDays = stageReq.durationDays;  // Giorni minimi

// Modificatore condizione:
int daysModifier = 0;
if (condition == Rigogliosa) daysModifier = -1;  // -1 giorno
if (condition == Critica || condition == Appassita) {
    // Blocca avanzamento
    return;
}

int effectiveRequiredDays = requiredDays + daysModifier;

// Verifica avanzamento:
bool canAdvance = 
    pot.GrowthPoints >= requiredPoints &&
    pot.DaysInCurrentStage >= effectiveRequiredDays &&
    pot.DaysConsecutiveOptimal >= requiredDays;  // Giorni con tutti i parametri ottimali
```

---

## 📝 **FASI DI IMPLEMENTAZIONE - TRANCE 1**

### **FASE 1: Estensione Modelli Dati per Fertilizzante** 🔧
**BLK Code:** BLK-03.01.01  
**Priorità:** ALTA  
**Dipendenze:** Nessuna

#### Task 1.1: Estendere StageRequirements (Solo Fertilizzante)
**File:** `Assets/_Project/Scripts/Dome/PotSystem/Growth/StageRequirements.cs`

⚠️ **IMPORTANTE - Non Breaking Changes:**
- ✅ **Aggiungere** nuovi campi, NON rimuovere o modificare esistenti
- ✅ Mantenere tutti i metodi esistenti (`IsHydrationInRange`, `IsHydrationOptimal`, etc.)
- ✅ Valori default per retrocompatibilità
- ✅ Testare che funzionalità esistenti continuino a funzionare

**Modifiche (TRANCE 1 - Solo Fertilizzante):**
```csharp
[Header("Fertilizer Requirements (BLK-03.01-T1)")]
[Tooltip("Range minimo fertilizzante (%) - VALORI FISSI per tutti gli stadi")]
[Range(0, 100)]
public int fertilizerMin = 0;

[Tooltip("Fertilizzante ottimale/mediano (%) - VALORI FISSI per tutti gli stadi")]
[Range(0, 100)]
public int fertilizerMed = 50;

[Tooltip("Range massimo fertilizzante (%) - VALORI FISSI per tutti gli stadi")]
[Range(0, 100)]
public int fertilizerMax = 100;

// Metodi helper
public bool IsFertilizerInRange(int currentFertilizer) 
{
    return currentFertilizer >= fertilizerMin && currentFertilizer <= fertilizerMax;
}

public bool IsFertilizerOptimal(int currentFertilizer, int tolerance = 5) 
{
    return Mathf.Abs(currentFertilizer - fertilizerMed) <= tolerance;
}
```

**NOTA TRANCE 1:** Range luce sarà aggiunto nella TRANCE 2.

**Criteri di Accettazione:**
- ✅ Range fertilizzante aggiunto a `StageRequirements`
- ✅ Metodi helper per verifica range fertilizzante
- ✅ Compatibilità retroattiva (valori default)
- ⏸️ Range luce: **NON incluso in Trance 1** (sarà in Trance 2)

---

#### Task 1.2: Estendere PotStateModel (Solo Fertilizzante)
**File:** `Assets/_Project/Scripts/Dome/PotStateModel.cs`

⚠️ **IMPORTANTE - Non Breaking Changes:**
- ✅ **Aggiungere** nuovi campi, NON rimuovere o modificare esistenti
- ✅ Mantenere tutti i campi esistenti invariati (Hydration, LightExposure, Stage, etc.)
- ✅ Valori default nei costruttori per retrocompatibilità
- ✅ Testare che salvataggi vecchi continuino a funzionare
- ✅ Testare che funzionalità esistenti (Watering, LED, Plant) continuino a funzionare

**Modifiche (TRANCE 1 - Solo Fertilizzante):**
```csharp
[Header("Fertilizer System (BLK-03.01-T1)")]
[Tooltip("Livello fertilizzante attuale (0-100)")]
public int FertilizerLevel = 0;  // 0 = nessun fertilizzante

[Tooltip("Giorni consecutivi con fertilizzante applicato")]
public int DaysFertilizerActive = 0;
```

**NOTA TRANCE 1:** 
- Campi punti giornalieri (GrowthPointsWater, GrowthPointsLight, GrowthPointsFertilizer) → **TRANCE 2**
- Tracking giorni consecutivi ottimali → **TRANCE 2**

**Criteri di Accettazione:**
- ✅ Campi fertilizzante aggiunti
- ✅ Metodi di reset nei costruttori
- ⏸️ Campi punti crescita: **NON inclusi in Trance 1** (saranno in Trance 2)

---

### **FASE 2: Sistema Fertilizzante Base** 🌿
**BLK Code:** BLK-03.01-T1.02  
**Priorità:** ALTA  
**Dipendenze:** Fase 1 (Task 1.2)

**NOTA IMPORTANTE:**
- ✅ **Collegato ai conteggi GROWTH**: Il fertilizzante influisce sulla crescita della pianta
  - Il livello fertilizzante viene verificato nel sistema di crescita esistente
  - Se il fertilizzante è nel range ottimale per lo stadio, contribuisce positivamente alla crescita
  - **TRANCE 1**: Il fertilizzante viene tracciato ma non ancora integrato nel calcolo punti (sarà in Trance 2)
  - **TRANCE 2**: Il fertilizzante sarà integrato nel sistema punti giornalieri
- ❌ **NON ha effetto sul pH DRIFT**: Il fertilizzante non modifica il pH globale della Dome
  - Il fertilizzante è un nutriente per la pianta, non un modificatore chimico del terreno
  - Solo le piante stesse modificano il pH Dome (attraverso il loro `dailyPhDrift`)

#### Task 2.1: Creare FertilizerSystem
**File:** `Assets/_Project/Scripts/Dome/PotSystem/Fertilizer/FertilizerSystem.cs` (NUOVO)

**Funzionalità:**
```csharp
public enum FertilizerType
{
    Standard = 0,    // 25 CRY, +25% fertilizzante
    Pure = 1,        // 75 CRY, +40% fertilizzante
    Prohibited = 2   // 75 CRY, +40% fertilizzante
}

public static class FertilizerSystem
{
    // Costanti percentuali fertilizzante per tipo
    private const int FERTILIZER_STANDARD_AMOUNT = 25;   // 25%
    private const int FERTILIZER_PURE_AMOUNT = 40;       // 40%
    private const int FERTILIZER_PROHIBITED_AMOUNT = 40; // 40%
    
    // Costanti costi
    private const int COST_STANDARD = 25;   // CRY
    private const int COST_PURE = 75;       // CRY
    private const int COST_PROHIBITED = 75; // CRY
    
    // Decadimento fertilizzante giornaliero (es. -5% al giorno)
    public static void ApplyDailyDecay(PotStateModel pot, float decayRate = 5f);
    
    // Verifica se fertilizzante è nel range ottimale per lo stadio
    public static bool IsFertilizerInOptimalRange(
        PotStateModel pot, 
        StageRequirements stageReq);
    
    // Calcola livello fertilizzante dopo applicazione
    public static int CalculateFertilizerLevel(
        int currentLevel, 
        FertilizerType fertilizerType);
    
    // Ottiene percentuale fertilizzante per tipo
    public static int GetFertilizerAmount(FertilizerType type);
    
    // Verifica coerenza genetica (REGOLA CRITICA: MORTE IMMEDIATA se incompatibile)
    // Standard → solo Standard
    // Pure → Pure o Standard
    // Evil → Prohibited o Standard
    // Pure ↔ Prohibited = incompatibile (morte)
    public static bool IsFertilizerCompatible(
        FertilizerType fertilizerType, 
        PlantFamily plantFamily)
    {
        return plantFamily switch
        {
            PlantFamily.Standard => fertilizerType == FertilizerType.Standard,
            PlantFamily.Pure => fertilizerType == FertilizerType.Pure || fertilizerType == FertilizerType.Standard,
            PlantFamily.Evil => fertilizerType == FertilizerType.Prohibited || fertilizerType == FertilizerType.Standard,
            _ => false
        };
    }
}
```

**Criteri di Accettazione:**
- ✅ Sistema decadimento fertilizzante (-5% al giorno)
- ✅ Verifica range ottimale (0-100%)
- ✅ Calcolo livello dopo applicazione (clamp 0-100%)
- ✅ Costanti per i tre tipi di fertilizzante
- ✅ Verifica coerenza genetica con regole critiche:
  - Standard → solo Standard
  - Pure → Pure o Standard
  - Evil → Prohibited o Standard
  - Pure ↔ Prohibited = incompatibile (morte)

---

#### Task 2.2: Implementare DoFertilize in PotActions
**File:** `Assets/_Project/Scripts/Dome/PotActions.cs`

⚠️ **IMPORTANTE - Non Breaking Changes:**
- ✅ **Aggiungere** nuovo metodo `DoFertilize()`, NON modificare metodi esistenti
- ✅ Mantenere tutti i metodi esistenti invariati (`DoWater()`, `DoLight()`, `DoPlant()`, `DoHarvest()`, etc.)
- ✅ Testare che tutte le azioni esistenti continuino a funzionare
- ✅ Seguire pattern esistenti (stesso stile di `DoWater()`, `DoLight()`, etc.)

**UI/UX:**
- **Bottone singolo** nella HUD Piante (stesso pattern di Watering e LED)
- **Popup inventario** che si apre quando si clicca il bottone (stesso pattern del popup SEED per azione PIANTA)
- Permette di selezionare fertilizzante dall'inventario
- Inventario deve contenere **2x ogni tipo** di fertilizzante (Standard, Pure, Prohibited)

**Modifiche:**
```csharp
public bool DoFertilize(string potId, string fertilizerItemCode)
{
    // 1. Verifica vaso e pianta
    var potState = GetPotState(potId);
    if (potState == null || !potState.HasPlant)
        return false;
    
    // 2. Verifica fertilizzante nell'inventario
    var fertilizerItem = Inventory.FindItem(fertilizerItemCode);
    if (fertilizerItem == null)
        return false;
    
    // 3. Determina tipo fertilizzante da ItemCode
    FertilizerType fertilizerType = GetFertilizerTypeFromItemCode(fertilizerItemCode);
    
    // 4. Ottieni PlantData per verificare famiglia
    var plantData = potState.GetPlantData();
    if (plantData == null)
        return false;
    
    // 5. Verifica coerenza genetica (REGOLA CRITICA: MORTE IMMEDIATA)
    if (!FertilizerSystem.IsFertilizerCompatible(fertilizerType, plantData.Family))
    {
        // 🚨 MORTE IMMEDIATA della pianta
        Debug.LogError($"[PotActions] Fertilizzante incompatibile! Pianta MUORE IMMEDIATAMENTE. Famiglia: {plantData.Family}, Fertilizzante: {fertilizerType}");
        
        // Rimuovi pianta dal vaso (morte)
        potState.HasPlant = false;
        potState.PlantCode = null;
        potState.Stage = 0;
        potState.Hydration = 0;
        potState.LightExposure = 0;
        potState.FertilizerLevel = 0;
        // Reset tutti i contatori
        potState.DaysSincePlant = 0;
        potState.DaysInCurrentStage = 0;
        potState.GrowthPoints = 0;
        
        // Notifica evento morte pianta
        PotEvents.EmitPlantDied(potId, $"Fertilizzante incompatibile: {fertilizerType} su pianta {plantData.Family}");
        
        // Consuma comunque il fertilizzante (già usato)
        Inventory.RemoveItem(fertilizerItemCode, 1);
        
        return false; // Operazione fallita (pianta morta)
    }
    
    // 6. Applica fertilizzante (aumenta FertilizerLevel)
    int fertilizerAmount = FertilizerSystem.GetFertilizerAmount(fertilizerType);
    potState.FertilizerLevel = Mathf.Clamp(
        potState.FertilizerLevel + fertilizerAmount, 
        0, 100);
    
    // 7. Se Resting → Flowering
    if (potState.Stage == (int)PlantStage.Resting)
    {
        potState.Stage = (int)PlantStage.Flowering;
        potState.DaysInCurrentStage = 0;
        // Notifica cambio stadio
        PotEvents.EmitPlantStageChanged(potId, PlantStage.Flowering);
    }
    
    // 8. Consuma fertilizzante dall'inventario
    Inventory.RemoveItem(fertilizerItemCode, 1);
    
    // 9. Aggiorna tracking
    potState.DaysFertilizerActive = 0; // Reset contatore
    
    return true;
}

private FertilizerType GetFertilizerTypeFromItemCode(string itemCode)
{
    // Mappa ItemCode → FertilizerType
    // Esempio: "fertilizer-standard" → Standard
    //          "fertilizer-pure" → Pure
    //          "fertilizer-prohibited" → Prohibited
    return itemCode switch
    {
        "fertilizer-standard" => FertilizerType.Standard,
        "fertilizer-pure" => FertilizerType.Pure,
        "fertilizer-prohibited" => FertilizerType.Prohibited,
        _ => FertilizerType.Standard
    };
}
```

**Criteri di Accettazione:**
- ✅ Azione fertilizzante implementata
- ✅ Bottone singolo nella HUD Piante (pattern Watering/LED)
- ✅ Popup inventario per selezione fertilizzante (pattern SEED/PIANTA)
- ✅ Verifica coerenza genetica con morte immediata se incompatibile
- ✅ Morte immediata pianta se fertilizzante incompatibile (rimozione dal vaso)
- ✅ Notifica evento morte pianta per UI/feedback
- ✅ Applicazione percentuale corretta (25% Standard, 40% Pure/Prohibited)
- ✅ Clamp fertilizzante 0-100%
- ✅ Transizione Resting → Flowering
- ✅ Consumo inventario
- ✅ Error log dettagliato se fertilizzante incompatibile
- ✅ **Collegato ai conteggi GROWTH** (influisce sulla crescita)
- ✅ **NON ha effetto sul pH DRIFT** (fertilizzante non modifica pH Dome)

---

#### Task 2.3: Creare ItemConfig per Fertilizzanti
**File:** Assets esistenti o nuovi ItemConfig ScriptableObject

**Modifiche:**
- Creare 3 ItemConfig per fertilizzanti:
  - `ItemConfig_Fertilizer_Standard.asset`
  - `ItemConfig_Fertilizer_Pure.asset`
  - `ItemConfig_Fertilizer_Prohibited.asset`
- Configurare ItemCode:
  - `"fertilizer-standard"`
  - `"fertilizer-pure"`
  - `"fertilizer-prohibited"`
- Aggiungere **2x ogni tipo** all'inventario iniziale (o sistema di acquisto)

**Criteri di Accettazione:**
- ✅ 3 ItemConfig creati e configurati
- ✅ ItemCode corretti per mapping
- ✅ 2x ogni tipo disponibile nell'inventario

---

#### Task 2.4: Integrare Decadimento in DayCycleController
**File:** `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

**Modifiche:**
```csharp
private void ApplyDecayAndCleanup(int dayIndex)
{
    // ... esistente ...
    
    // NUOVO: Decadimento fertilizzante
    foreach (var pot in _registeredPots)
    {
        if (pot.HasPlant && pot.FertilizerLevel > 0)
        {
            FertilizerSystem.ApplyDailyDecay(pot, decayRate: 5f);
        }
    }
}
```

**Criteri di Accettazione:**
- ✅ Decadimento fertilizzante giornaliero
- ✅ Reset quando raggiunge 0

---

---

## 📊 **TRANCE 2: SISTEMA CRESCITA BASATO SU VALORI**

### **Panoramica Trance 2**

Questa trance implementa il refactoring completo del sistema di crescita, utilizzando il sistema fertilizzante già implementato nella Trance 1.

**Componenti principali:**
1. Sistema punti giornalieri basato su valori nel range
2. Tracking giorni consecutivi con parametri ottimali
3. Integrazione condizione → crescita
4. Logica avanzamento stadio completa
5. Range luce in StageRequirements
6. Aggiornamento UI

---

### **FASE 3: Sistema Punti Basato su Valori** 📊
**BLK Code:** BLK-03.01-T2.03  
**Priorità:** ALTA  
**Dipendenze:** TRANCE 1 completata, Fase 1 (estendere PotStateModel con punti)

#### Task 3.1: Creare GrowthPointsCalculator
**File:** `Assets/_Project/Scripts/Dome/PotSystem/Growth/GrowthPointsCalculator.cs` (NUOVO)

**Funzionalità:**
```csharp
public static class GrowthPointsCalculator
{
    /// <summary>
    /// Calcola e assegna punti giornalieri basati su valori nel range ideale
    /// </summary>
    public static GrowthPointsResult CalculateDailyPoints(
        PotStateModel pot,
        PlantData plantData,
        PotSystemConfig potConfig)
    {
        var result = new GrowthPointsResult();
        
        // Ottieni requisiti per lo stadio corrente
        PlantStage currentStage = (PlantStage)pot.Stage;
        StageRequirements stageReq = plantData.GetStageRequirements(currentStage);
        
        if (stageReq == null)
        {
            // Se non ci sono requisiti, nessun punto
            return result;
        }
        
        // 1. Verifica water nel range ideale (hydrationPercent nel range)
        int maxHydration = potConfig != null ? potConfig.MaxHydration : 4;
        int hydrationPercent = maxHydration > 0 ? 
            Mathf.RoundToInt((float)pot.Hydration / maxHydration * 100f) : 0;
        
        if (stageReq.IsHydrationInRange(hydrationPercent))
        {
            result.WaterPoint = 1;
            pot.GrowthPointsWater += 1;
        }
        
        // 2. Verifica light nel range ideale (LED corretto + intensità nel range)
        if (IsLightInOptimalRange(pot, plantData, stageReq))
        {
            result.LightPoint = 1;
            pot.GrowthPointsLight += 1;
        }
        
        // 3. Verifica fertilizer nel range ideale (FertilizerLevel 0-100% nel range)
        if (stageReq.IsFertilizerInRange(pot.FertilizerLevel))
        {
            result.FertilizerPoint = 1;
            pot.GrowthPointsFertilizer += 1;
        }
        
        return result;
    }
    
    private static bool IsLightInOptimalRange(
        PotStateModel pot, 
        PlantData plantData, 
        StageRequirements stageReq)
    {
        // Verifica LED richiesto
        if (!stageReq.IsLedRequirementMet(pot.LedSystemState))
            return false;
        
        // Verifica intensità luce nel range (se implementato)
        // Per ora: solo verifica LED corretto
        // TODO: Aggiungere verifica intensità luce quando sistema sarà implementato
        return true;
    }
}

public struct GrowthPointsResult
{
    public int WaterPoint;      // 0 o 1
    public int LightPoint;       // 0 o 1
    public int FertilizerPoint;  // 0 o 1
    public int TotalPoints => WaterPoint + LightPoint + FertilizerPoint;
}
```

**Criteri di Accettazione:**
- ✅ Calcolo punti basato su valori nel range
- ✅ Assegnazione 1 punto per parametro nel range
- ✅ Verifica range ideali da `StageRequirements`

---

#### Task 3.2: Integrare in DayCycleController
**File:** `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

**Modifiche:**
```csharp
private void ResolveGrowthForPot(PotStateModel pot, int dayIndex)
{
    // ... esistente ...
    
    // NUOVO: Calcola punti giornalieri basati su valori nel range
    var pointsResult = GrowthPointsCalculator.CalculateDailyPoints(
        pot, plantData, _potSystemConfig);
    
    // NUOVO: Aggiorna tracking giorni consecutivi ottimali
    if (pointsResult.TotalPoints == 3)  // Tutti i parametri ottimali
    {
        pot.DaysConsecutiveOptimal++;
        if (pot.DayOptimalParametersStarted < 0)
        {
            pot.DayOptimalParametersStarted = dayIndex;
        }
    }
    else
    {
        // Reset se non tutti i parametri sono ottimali
        pot.DaysConsecutiveOptimal = 0;
        pot.DayOptimalParametersStarted = -1;
    }
    
    // ... resto logica esistente ...
}
```

**Criteri di Accettazione:**
- ✅ Punti calcolati a fine giornata
- ✅ Tracking giorni consecutivi ottimali
- ✅ Reset quando parametri escono dal range

---

### **FASE 4: Integrazione Condizione → Crescita** 🔗
**BLK Code:** BLK-03.01-T2.04  
**Priorità:** MEDIA  
**Dipendenze:** Fase 3

#### Task 4.1: Creare ConditionGrowthModifier
**File:** `Assets/_Project/Scripts/Dome/PotSystem/Growth/ConditionGrowthModifier.cs` (NUOVO)

**Funzionalità:**
```csharp
public static class ConditionGrowthModifier
{
    /// <summary>
    /// Calcola modificatore giorni in base alla condizione
    /// </summary>
    public static int GetDaysModifier(PlantCondition condition)
    {
        return condition switch
        {
            PlantCondition.Rigogliosa => -1,  // -1 giorno (guadagna 1 giorno)
            PlantCondition.Sana => 0,         // Nessun modificatore
            PlantCondition.Stressata => 0,     // Nessun modificatore
            PlantCondition.Appassita => 0,    // Nessun modificatore (ma blocca avanzamento)
            PlantCondition.Critica => 0,      // Nessun modificatore (ma blocca avanzamento)
            _ => 0
        };
    }
    
    /// <summary>
    /// Verifica se la condizione blocca l'avanzamento
    /// </summary>
    public static bool BlocksAdvancement(PlantCondition condition)
    {
        return condition == PlantCondition.Critica || 
               condition == PlantCondition.Appassita;
    }
}
```

**Criteri di Accettazione:**
- ✅ Modificatore giorni per Rigogliosa (-1)
- ✅ Blocco avanzamento per Critica/Appassita
- ✅ Nessun modificatore per Sana/Stressata

---

#### Task 4.2: Integrare in ResolveGrowthForPot
**File:** `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

**Modifiche:**
```csharp
private void ResolveGrowthForPot(PotStateModel pot, int dayIndex)
{
    // ... esistente ...
    
    // NUOVO: Ottieni condizione corrente
    PlantCondition currentCondition = (PlantCondition)pot.ConditionLabel;
    
    // NUOVO: Verifica se condizione blocca avanzamento
    if (ConditionGrowthModifier.BlocksAdvancement(currentCondition))
    {
        if (enableDebugLogs)
            Debug.Log($"[BLK-03.01] {pot.PotId}: Avanzamento bloccato - Condizione: {currentCondition}");
        return;  // Non può avanzare
    }
    
    // NUOVO: Applica modificatore giorni
    int daysModifier = ConditionGrowthModifier.GetDaysModifier(currentCondition);
    int effectiveRequiredDays = currentStageReq.durationDays + daysModifier;
    
    // Verifica avanzamento con giorni modificati
    bool durationOk = pot.DaysInCurrentStage >= effectiveRequiredDays;
    
    // ... resto logica avanzamento ...
}
```

**Criteri di Accettazione:**
- ✅ Blocco avanzamento per Critica/Appassita
- ✅ Modificatore -1 giorno per Rigogliosa
- ✅ Logging debug per modificatori

---

### **FASE 5: Logica Avanzamento Stadio Completa** 🎯
**BLK Code:** BLK-03.01-T2.05  
**Priorità:** ALTA  
**Dipendenze:** Fase 3, Fase 4

#### Task 5.1: Modificare Logica Avanzamento
**File:** `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

**Modifiche:**
```csharp
// NUOVO: Verifica requisiti avanzamento
bool canAdvance = false;

if (currentStageReq != null)
{
    // 1. Verifica punti accumulati (3 punti = 1 water + 1 light + 1 fertilizer)
    int totalPoints = pot.GrowthPointsWater + 
                      pot.GrowthPointsLight + 
                      pot.GrowthPointsFertilizer;
    int requiredPoints = 3;  // 1 per ogni parametro
    
    bool pointsOk = totalPoints >= requiredPoints;
    
    // 2. Verifica giorni minimi (con modificatore condizione)
    int daysModifier = ConditionGrowthModifier.GetDaysModifier(
        (PlantCondition)pot.ConditionLabel);
    int effectiveRequiredDays = currentStageReq.durationDays + daysModifier;
    bool durationOk = pot.DaysInCurrentStage >= effectiveRequiredDays;
    
    // 3. Verifica giorni consecutivi con parametri ottimali
    bool optimalDaysOk = pot.DaysConsecutiveOptimal >= currentStageReq.durationDays;
    
    // 4. Verifica range parametri (solo per validazione finale)
    bool hydrationOk = currentStageReq.IsHydrationInRange(hydrationPercent);
    bool ledOk = currentStageReq.IsLedRequirementMet(pot.LedSystemState);
    bool fertilizerOk = currentStageReq.IsFertilizerInRange(pot.FertilizerLevel);
    
    canAdvance = pointsOk && durationOk && optimalDaysOk && 
                 hydrationOk && ledOk && fertilizerOk;
    
    if (enableDebugLogs)
    {
        Debug.Log($"[BLK-03.01] {pot.PotId}: Verifica avanzamento - " +
                  $"Points: {totalPoints}/{requiredPoints} [{pointsOk}], " +
                  $"Days: {pot.DaysInCurrentStage}/{effectiveRequiredDays} [{durationOk}], " +
                  $"OptimalDays: {pot.DaysConsecutiveOptimal}/{currentStageReq.durationDays} [{optimalDaysOk}]");
    }
}

if (canAdvance)
{
    // Avanzamento stadio
    // Reset contatori
    pot.GrowthPointsWater = 0;
    pot.GrowthPointsLight = 0;
    pot.GrowthPointsFertilizer = 0;
    pot.DaysConsecutiveOptimal = 0;
    pot.DaysInCurrentStage = 0;
    // ... avanzamento ...
}
```

**Criteri di Accettazione:**
- ✅ Verifica punti accumulati (3 punti)
- ✅ Verifica giorni minimi con modificatore condizione
- ✅ Verifica giorni consecutivi ottimali
- ✅ Reset contatori dopo avanzamento

---

### **FASE 6: Aggiornamento Editor e Configurazione** ⚙️
**BLK Code:** BLK-03.01-T1.06 (Trance 1) / BLK-03.01-T2.06 (Trance 2)  
**Priorità:** MEDIA  
**Dipendenze:** 
- **Trance 1**: Fase 1 (solo range fertilizzante)
- **Trance 2**: Fase 1 (range luce + punti)

#### Task 6.1: Aggiornare PopulateStageRequirements
**File:** `Assets/_Project/Editor/PopulateStageRequirements.cs`

**Modifiche TRANCE 1:**
- Aggiungere valori default per `fertilizerMin/Med/Max` (valori fissi)
- Aggiornare metodi `GetStandardRequirements()`, `GetPureRequirements()`, `GetEvilRequirements()`

**Modifiche TRANCE 2:**
- Aggiungere valori default per `lightMin/Med/Max`
- Completare configurazione range luce

**Criteri di Accettazione TRANCE 1:**
- ✅ Range fertilizzante aggiunto a tutti gli stadi con valori fissi
- ✅ Valori default corretti (Seed: 60-75-90, Growth: 40-60-80, Flowering: 20-40-60, Resting: 30-50-70)

**Criteri di Accettazione TRANCE 2:**
- ✅ Range luce aggiunto a tutti gli stadi
- ✅ Valori default sensati

---

#### Task 6.2: Aggiornare PlantData Assets
**File:** Assets esistenti (PLT-STD-001.asset, PLT-PURE-001.asset, PLT-EVIL-001.asset)

**Modifiche:**
- Aggiungere range luce a tutti gli stadi (lightMin, lightMed, lightMax)
- Aggiungere range fertilizzante a tutti gli stadi (fertilizerMin, fertilizerMed, fertilizerMax)
- **Range fertilizzante**: 0-100% per ogni stadio (da configurare per ogni pianta/stadio)
- Valori da definire in base a design

**Range Fertilizzante FISSI (identici per tutte le piante):**
```
Seed: fertilizerMin=60, fertilizerMed=75, fertilizerMax=90
Sprout: fertilizerMin=60, fertilizerMed=75, fertilizerMax=90 (stesso di Seed)
Growth: fertilizerMin=40, fertilizerMed=60, fertilizerMax=80
Flowering: fertilizerMin=20, fertilizerMed=40, fertilizerMax=60
HarvestReady: fertilizerMin=0, fertilizerMed=0, fertilizerMax=0 (non richiesto)
Resting: fertilizerMin=30, fertilizerMed=50, fertilizerMax=70
```

⚠️ **IMPORTANTE**: Questi valori sono **FISSI** e devono essere identici per tutte le piante (Standard, Pure, Evil).

**Criteri di Accettazione:**
- ✅ Tutti i PlantData aggiornati
- ✅ Range luce configurati per tutti gli stadi
- ✅ Range fertilizzante configurati per tutti gli stadi con **valori fissi**:
  - Seed: 60-75-90
  - Growth: 40-60-80
  - Flowering: 20-40-60
  - HarvestReady: 0-0-0 (non richiesto)
  - Resting: 30-50-70
- ✅ Range identici per tutte le piante (Standard, Pure, Evil)

---

### **FASE 7: UI e Visualizzazione** 🎨
**BLK Code:** BLK-03.01-T1.07 (Trance 1) / BLK-03.01-T2.07 (Trance 2)  
**Priorità:** MEDIA (Trance 1) / BASSA (Trance 2)  
**Dipendenze:** 
- **Trance 1**: Fase 2 (indicatore fertilizzante base)
- **Trance 2**: Fase 3, Fase 4 (indicatori punti e giorni ottimali)

#### Task 7.1: Aggiornare PotHUDWidget
**File:** `Assets/_Project/Scripts/UI/VaultMap/PotHUDWidget.cs`

⚠️ **IMPORTANTE - Non Breaking Changes:**
- ✅ **Aggiungere** nuovo bottone fertilizzante, NON rimuovere bottoni esistenti
- ✅ **Aggiungere** nuovo campo testuale, NON modificare campi esistenti
- ✅ Mantenere tutti i bottoni esistenti (btnPlant, btnWater, btnLight, btnSpray, btnHarvest) invariati
- ✅ Mantenere tutti i campi testuali esistenti (hydrationText, lightExposureText, etc.) invariati
- ✅ Testare che UI esistente continui a funzionare correttamente
- ✅ Seguire pattern esistenti (stesso stile di btnWater, btnLight, etc.)

**Modifiche TRANCE 1:**
- **Bottone fertilizzante** nella HUD Piante (stesso pattern di Watering e LED)
  - Posizionato accanto ai bottoni Watering e LED
  - Icona fertilizzante distintiva
  - Apre popup inventario quando cliccato
- **Campo testuale fertilizzante** nella HUD:
  - Mostra **range ideale** per lo stadio corrente (es. "40-60-80%")
  - Mostra **percentuale attuale** (es. "45%")
  - Formato suggerito: `"Fertilizzante: 45% (Range: 40-60-80%)"`
  - Colore verde se nel range ottimale, giallo se fuori range ma accettabile, rosso se critico
- Indicatore visivo se fertilizzante nel range ottimale

**Modifiche TRANCE 2:**
- Mostrare punti giornalieri (water/light/fertilizer)
- Mostrare giorni consecutivi ottimali
- Mostrare modificatore condizione sui giorni

**Criteri di Accettazione TRANCE 1:**
- ✅ Bottone fertilizzante nella HUD Piante (pattern Watering/LED)
- ✅ Popup inventario si apre correttamente
- ✅ Campo testuale mostra range ideale (es. "40-60-80%")
- ✅ Campo testuale mostra percentuale attuale (es. "45%")
- ✅ Colori indicativi (verde/giallo/rosso) per stato fertilizzante
- ✅ Indicatore visivo range ottimale

**Criteri di Accettazione TRANCE 2:**
- ✅ UI mostra punti giornalieri
- ✅ UI mostra giorni consecutivi ottimali
- ✅ UI mostra modificatore condizione

---

#### Task 7.2: Aggiornare PotDetailsWidget
**File:** `Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs`

**Modifiche:**
- Sezione dettagliata punti crescita
- Sezione dettagliata range ideali
- Indicatore fertilizzante

**Criteri di Accettazione:**
- ✅ Dettagli punti crescita visibili
- ✅ Range ideali mostrati
- ✅ Indicatore fertilizzante funzionante

---

## 🔄 **MIGRAZIONE DATI ESISTENTI**

### **Save System Compatibility**

**File:** `Assets/_Project/Scripts/Core/SaveManager.cs`

**Modifiche necessarie:**
- Aggiungere serializzazione nuovi campi `PotStateModel`:
  - `FertilizerLevel`
  - `DaysFertilizerActive`
  - `GrowthPointsWater`
  - `GrowthPointsLight`
  - `GrowthPointsFertilizer`
  - `DaysConsecutiveOptimal`
  - `DayOptimalParametersStarted`

**Strategia:**
- Valori default per salvataggi vecchi (retrocompatibilità)
- Migrazione automatica al caricamento

---

## 🧪 **TEST E VALIDAZIONE**

### **Test Scenarios**

#### Test 1: Assegnazione Punti Basata su Valori
1. Imposta `hydrationPercent = 50%` (range ideale: 40-60%)
2. Imposta LED corretto attivo
3. Imposta fertilizzante nel range
4. **Risultato atteso**: 3 punti assegnati (1+1+1)

#### Test 2: Tracking Giorni Consecutivi
1. Mantieni tutti i parametri ottimali per 3 giorni
2. **Risultato atteso**: `DaysConsecutiveOptimal = 3`
3. Esci dal range per 1 giorno
4. **Risultato atteso**: `DaysConsecutiveOptimal = 0` (reset)

#### Test 3: Modificatore Condizione
1. Pianta in condizione Rigogliosa
2. `durationDays = 3`
3. **Risultato atteso**: `effectiveRequiredDays = 2` (-1 giorno)

#### Test 4: Blocco Avanzamento Critica
1. Pianta in condizione Critica
2. Tutti i requisiti soddisfatti
3. **Risultato atteso**: Avanzamento bloccato

#### Test 5: Fertilizzante - Applicazione
1. Pianta Standard, applica Fertilizzante Standard
2. **Risultato atteso**: `FertilizerLevel` aumenta di 25%
3. Pianta Pure, applica Fertilizzante Pure
4. **Risultato atteso**: `FertilizerLevel` aumenta di 40%
5. Pianta Evil, applica Fertilizzante Proibito
6. **Risultato atteso**: `FertilizerLevel` aumenta di 40%

#### Test 6: Fertilizzante - Coerenza Genetica (MORTE IMMEDIATA)
1. Pianta Standard, applica Fertilizzante Pure
2. **Risultato atteso**: 🚨 **MORTE IMMEDIATA** della pianta (rimossa dal vaso)
3. Pianta Standard, applica Fertilizzante Proibito
4. **Risultato atteso**: 🚨 **MORTE IMMEDIATA** della pianta (rimossa dal vaso)
5. Pianta Pure, applica Fertilizzante Proibito
6. **Risultato atteso**: 🚨 **MORTE IMMEDIATA** della pianta (rimossa dal vaso)
7. Pianta Evil, applica Fertilizzante Pure
8. **Risultato atteso**: 🚨 **MORTE IMMEDIATA** della pianta (rimossa dal vaso)
9. Pianta Pure, applica Fertilizzante Standard
10. **Risultato atteso**: ✅ Fertilizzante applicato correttamente (compatibile)
11. Pianta Evil, applica Fertilizzante Standard
12. **Risultato atteso**: ✅ Fertilizzante applicato correttamente (compatibile)

#### Test 7: Fertilizzante - Decadimento
1. Applica fertilizzante (es. +25%)
2. `FertilizerLevel = 25%`
3. Aspetta 1 giorno
4. **Risultato atteso**: `FertilizerLevel = 20%` (decadimento -5%)
5. Aspetta 4 giorni
6. **Risultato atteso**: `FertilizerLevel = 0%` (clamp minimo)

#### Test 8: Fertilizzante - Punto Giornaliero (Valori Fissi)
1. Stadio **Seed**: Imposta `FertilizerLevel = 70%` (range 60-90%)
2. Fine giornata
3. **Risultato atteso**: `GrowthPointsFertilizer += 1` (70% è nel range 60-90%)
4. Stadio **Growth**: Imposta `FertilizerLevel = 50%` (range 40-80%)
5. Fine giornata
6. **Risultato atteso**: `GrowthPointsFertilizer += 1` (50% è nel range 40-80%)
7. Stadio **Flowering**: Imposta `FertilizerLevel = 30%` (range 20-60%)
8. Fine giornata
9. **Risultato atteso**: `GrowthPointsFertilizer += 1` (30% è nel range 20-60%)
10. Stadio **Growth**: Imposta `FertilizerLevel = 10%` (fuori range 40-80%)
11. Fine giornata
12. **Risultato atteso**: Nessun punto assegnato (fuori range)

#### Test 9: Fertilizzante - Resting → Flowering
1. Pianta in Resting
2. Applica fertilizzante corretto
3. **Risultato atteso**: Transizione automatica a Flowering

---

## 📋 **CHECKLIST IMPLEMENTAZIONE**

### **Fase 1: Modelli Dati**
- [ ] Estendere `StageRequirements` con range luce
- [ ] Estendere `StageRequirements` con range fertilizzante
- [ ] Estendere `PotStateModel` con campi fertilizzante
- [ ] Estendere `PotStateModel` con campi punti giornalieri
- [ ] Estendere `PotStateModel` con tracking giorni ottimali

### **Fase 2: Sistema Fertilizzante**
- [ ] Creare `FertilizerSystem` con enum e costanti (Standard 25%, Pure/Prohibited 40%)
- [ ] Implementare `DoFertilize()` in `PotActions` con verifica coerenza genetica
- [ ] Implementare **morte immediata** se fertilizzante incompatibile
- [ ] Creare evento `PotEvents.EmitPlantDied()` per notifiche UI
- [ ] Creare ItemConfig per i tre tipi di fertilizzante (2x ogni tipo nell'inventario)
- [ ] Integrare decadimento in `DayCycleController` (-5% al giorno)
- [ ] Configurare range fertilizzante **fissi** in tutti i PlantData (valori identici)
- [ ] **Collegare ai conteggi GROWTH** (fertilizzante influisce sulla crescita)
- [ ] **Verificare che NON modifichi pH DRIFT** (fertilizzante non altera pH Dome)

#### **Fase 3: Sistema Punti**
- [ ] Creare `GrowthPointsCalculator`
- [ ] Integrare calcolo punti in `DayCycleController` (usa fertilizzante da Trance 1)
- [ ] Implementare tracking giorni consecutivi ottimali

#### **Fase 4: Integrazione Condizione**
- [ ] Creare `ConditionGrowthModifier`
- [ ] Integrare modificatore giorni in `ResolveGrowthForPot`
- [ ] Implementare blocco avanzamento per Critica/Appassita

#### **Fase 5: Logica Avanzamento**
- [ ] Modificare logica avanzamento con nuovi requisiti
- [ ] Integrare fertilizzante nel calcolo punti (usa sistema Trance 1)
- [ ] Implementare reset contatori dopo avanzamento
- [ ] Testare tutte le transizioni stadi

#### **Fase 6: Editor e Config (Trance 2)**
- [ ] Aggiornare `PopulateStageRequirements` con range luce
- [ ] Validare range configurati

#### **Fase 7: UI (Trance 2)**
- [ ] Aggiornare `PotHUDWidget` con indicatori punti e giorni ottimali
- [ ] Aggiornare `PotDetailsWidget` con sezione dettagliata punti crescita
- [ ] Testare visualizzazione

### **Migrazione e Test**
- [ ] Aggiornare `SaveManager` per nuovi campi (valori default per retrocompatibilità)
- [ ] Test retrocompatibilità salvataggi (salvataggi vecchi devono funzionare)
- [ ] **Test funzionalità esistenti** (Watering, LED, Plant, Harvest devono continuare a funzionare)
- [ ] **Test UI esistente** (HUD esistente non deve essere rotto)
- [ ] **Test sistema crescita esistente** (crescita attuale deve continuare a funzionare)
- [ ] Eseguire tutti i test scenarios
- [ ] Documentazione aggiornata

### **Checklist Verifica Non-Breaking Changes**
- [ ] ✅ Tutti i metodi esistenti continuano a funzionare
- [ ] ✅ Tutti i campi esistenti non sono stati modificati
- [ ] ✅ Salvataggi vecchi funzionano (valori default applicati)
- [ ] ✅ UI esistente non è stata rotta
- [ ] ✅ Funzionalità esistenti (Watering, LED, Plant, Harvest) funzionano
- [ ] ✅ Sistema crescita esistente continua a funzionare
- [ ] ✅ Nessun errore di compilazione introdotto
- [ ] ✅ Nessun warning nuovo introdotto

---

## 🚨 **RISCHI E MITIGAZIONI**

### **Rischio 1: Breaking Changes**
**Problema:** Modifiche a `PotStateModel` potrebbero rompere salvataggi esistenti  
**Mitigazione:** 
- ✅ Valori default per retrocompatibilità
- ✅ Migrazione automatica al caricamento
- ✅ Test estensivi su salvataggi vecchi
- ✅ **NON rimuovere** campi esistenti, solo aggiungere nuovi
- ✅ **NON modificare** struttura esistente, solo estendere

### **Rischio 1b: Regressioni Funzionalità Esistenti**
**Problema:** Nuove modifiche potrebbero rompere funzionalità esistenti (Watering, LED, Plant, Harvest)  
**Mitigazione:**
- ✅ Mantenere tutti i metodi esistenti invariati
- ✅ Test estensivi su tutte le funzionalità esistenti prima e dopo
- ✅ Refactoring incrementale (Trance 1 prima, Trance 2 dopo)
- ✅ Fallback al sistema esistente se nuovo sistema fallisce

### **Rischio 2: Performance**
**Problema:** Calcoli aggiuntivi a fine giornata potrebbero rallentare  
**Mitigazione:**
- Ottimizzare calcoli (cache range ottimali)
- Profiling e ottimizzazione se necessario

### **Rischio 3: Complessità Logica**
**Problema:** Sistema più complesso = più bug potenziali  
**Mitigazione:**
- Test estensivi per ogni fase
- Logging dettagliato per debug
- Code review attenta

---

## 📚 **DOCUMENTAZIONE**

### **File da Aggiornare**

1. **README_BLK-03.01.md** (NUOVO)
   - Documentazione completa nuovo sistema
   - Esempi di utilizzo
   - Troubleshooting
   - **Sezione Fertilizzanti**:
     - Tipi di fertilizzanti (Standard, Pure, Prohibited)
     - Costi e fonti di acquisto
     - Percentuali applicate (25%, 40%, 40%)
     - Coerenza genetica e malus incompatibilità

2. **ANALISI_SISTEMA_CRESCITA_PIANTE.txt** (AGGIORNARE)
   - Sezione nuovo sistema
   - Confronto vecchio vs nuovo
   - Sezione fertilizzanti con regole critiche di compatibilità
   - Valori fissi fertilizzante per tutti gli stadi

3. **ISTRUZIONI_MANUALI_*.md** (AGGIORNARE)
   - Istruzioni per test manuali
   - Scenari di test
   - **ISTRUZIONI_MANUALI_FERTILIZZANTI.md** (NUOVO)
     - Come applicare fertilizzante
     - Verifica coerenza genetica (regole critiche)
     - **Morte immediata** se fertilizzante incompatibile
     - Range ideali fissi per ogni stadio (valori identici per tutte le piante)

---

## 🎯 **CRITERI DI COMPLETAMENTO**

Il sistema è considerato completo quando:

1. ✅ Tutti i punti vengono assegnati basandosi su valori nel range (non attivazione)
2. ✅ Tracking giorni consecutivi con parametri ottimali funzionante
3. ✅ Sistema fertilizzante implementato e funzionante:
   - ✅ Tre tipi di fertilizzanti (Standard, Pure, Prohibited)
   - ✅ Percentuali corrette (25%, 40%, 40%)
   - ✅ Verifica coerenza genetica con **morte immediata** se incompatibile
   - ✅ Range fertilizzante **fissi** per ogni stadio (valori identici per tutte le piante):
     - Seed: 60-75-90
     - Growth: 40-60-80
     - Flowering: 20-40-60
     - HarvestReady: non richiesto
     - Resting: 30-50-70
   - ✅ Punto giornaliero quando FertilizerLevel nel range ideale
4. ✅ Integrazione condizione → crescita funzionante
5. ✅ Tutti i test scenarios passano (inclusi test fertilizzante)
6. ✅ UI aggiornata e funzionante
7. ✅ Documentazione completa (inclusa sezione fertilizzanti)
8. ✅ Retrocompatibilità salvataggi verificata

---

## 📅 **TIMELINE STIMATA**

### **TRANCE 1: Sistema Fertilizzante**
- **Fase 1**: 1-2 giorni (solo fertilizzante, no luce/punti)
- **Fase 2**: 3-4 giorni
- **Fase 6**: 1 giorno (configurazione range fissi)
- **Fase 7**: 1-2 giorni (UI base fertilizzante)
- **Test e Bug Fix**: 2-3 giorni

**Totale Trance 1 stimato**: 8-12 giorni lavorativi

### **TRANCE 2: Sistema Crescita Basato su Valori**
- **Fase 1**: 2-3 giorni (estendere con luce e punti)
- **Fase 3**: 3-4 giorni
- **Fase 4**: 2-3 giorni
- **Fase 5**: 3-4 giorni
- **Fase 6**: 1 giorno (range luce)
- **Fase 7**: 2-3 giorni (UI completa)
- **Test e Bug Fix**: 3-5 giorni

**Totale Trance 2 stimato**: 16-23 giorni lavorativi

**Totale complessivo stimato**: 24-35 giorni lavorativi

**Vantaggio divisione in trance:**
- ✅ Trance 1 può essere rilasciata e testata indipendentemente
- ✅ Riduce rischio breaking changes
- ✅ Permette feedback incrementale

---

**Fine Documento**

