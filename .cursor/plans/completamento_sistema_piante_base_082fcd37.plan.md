---
name: Completamento Sistema PIANTE Base
overview: "Piano di sviluppo per completare il sistema PIANTE base, escludendo Compost (Loop Lab), Mutazioni, Ibridi e Slot Passivi (da implementare dopo test delle altre fasi). Include: effetti pH estremi su resa/crescita, modificatori percentuali condizioni, Burn Stress completo LED, e indicatori HUD."
todos:
  - id: task_1_1
    content: Estendere ConditionGrowthModifier con modificatori percentuali crescita e produzione + aggiornare tooltip Conditions
    status: pending
  - id: task_1_2
    content: Applicare modificatori percentuali crescita in ResolveGrowthForPot
    status: pending
  - id: task_1_3
    content: Applicare modificatori produzione in DoHarvest (con arrotondamento a intero)
    status: pending
  - id: task_2_1
    content: Creare PhGrowthModifier per modificatori crescita e resa basati su pH + aggiornare tooltip Ph Drift
    status: pending
  - id: task_2_2
    content: Applicare modificatori crescita pH in ResolveGrowthForPot
    status: pending
  - id: task_2_3
    content: Applicare modificatori resa pH in DoHarvest e gestire sterilità Pure (con arrotondamento a intero)
    status: pending
  - id: task_3_1
    content: Implementare applicazione completa Burn Stress in ApplyLedEffects
    status: pending
  - id: task_3_2
    content: Aggiungere effetti Burn Stress estremi dopo 3 giorni consecutivi
    status: pending
  - id: task_4_1
    content: Aggiungere indicatore giorni consecutivi LED in PlantCardV2
    status: pending
  - id: task_4_2
    content: Aggiungere indicatore giorni consecutivi LED in AlwaysVisiblePotHUD
    status: pending
---

# Piano di Sviluppo: Completamento Sistema PIANTE Base

## Obiettivo

Completare il sistema base delle piante escludendo funzionalità avanzate (Compost, Mutazioni, Ibridi) che verranno implementate successivamente. **FASE 5 (Slot Passivi) verrà implementata solo dopo completamento e test delle Fasi 1-4.**

## Stato Attuale

- Sistema Fertilizzanti: 85% (compatibilità famiglie implementata)
- Sistema LED: 85% (scaling e consumo CRY implementati)
- Sistema Condizioni: 70% (calcolo completo, effetti parziali)
- Sistema pH Estremi: 50% (countdown morte presente)
- Sistema Slot Passivi: 10% (solo check presente)

---

## FASE 1: Modificatori Condizioni su Crescita e Produzione

### Task 1.1: Estendere ConditionGrowthModifier con modificatori percentuali + Tooltip

**File**: `Assets/_Project/Scripts/Dome/PotSystem/Growth/ConditionGrowthModifier.cs`

**Modifiche**:

- Aggiungere metodo `GetGrowthSpeedMultiplier(PlantCondition)`:
  - Rigogliosa: +20% (multiplier 1.2f)
  - Sana: 0% (multiplier 1.0f)
  - Stressata: -10% (multiplier 0.9f)
  - Appassita: -30% (multiplier 0.7f)
  - Critica: 0% (multiplier 1.0f, ma blocca avanzamento)
- Aggiungere metodo `GetProductionMultiplier(PlantCondition)`:
  - Rigogliosa: +15% (multiplier 1.15f)
  - Sana: 0% (multiplier 1.0f)
  - Stressata: -15% (multiplier 0.85f)
  - Appassita: 0% (multiplier 1.0f, ma blocca avanzamento)
  - Critica: 0% (multiplier 1.0f, ma blocca avanzamento)
  - Infestata: -50% (multiplier 0.5f) - da verificare se condizione Infestata esiste
- **Aggiornare tooltip Conditions** (verificare dove viene mostrato):
  - Aggiungere testo che spiega impatto Bonus/Malus della condizione attuale su crescita e resa
  - Esempio: "Rigogliosa: +20% velocità crescita, +15% produzione frutti"
  - Mostrare modificatori in modo chiaro e leggibile

**Dipendenze**: Nessuna

---

### Task 1.2: Applicare modificatori percentuali crescita in ResolveGrowthForPot

**File**: `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

**Modifiche**:

- In `ResolveGrowthForPot()`, dopo calcolo punti crescita giornalieri:
  - Ottenere condizione corrente: `PlantCondition currentCondition = (PlantCondition)pot.ConditionLabel`
  - Calcolare moltiplicatore: `float growthMultiplier = ConditionGrowthModifier.GetGrowthSpeedMultiplier(currentCondition)`
  - Applicare moltiplicatore ai punti: `gained *= growthMultiplier` (dove `gained` sono i punti calcolati)
- Mantenere logica esistente `GetDaysModifier()` per modificatori giorni
- Log debug: mostrare moltiplicatore applicato

**Riferimenti**:

- Metodo `ResolveGrowthForPot()` righe ~462-890
- Calcolo punti crescita righe ~1055-1074

**Dipendenze**: Task 1.1

---

### Task 1.3: Applicare modificatori produzione in DoHarvest (con arrotondamento)

**File**: `Assets/_Project/Scripts/Dome/PotActions.cs`

**Modifiche**:

- In `DoHarvest()`, dopo calcolo `baseAmount` (riga ~1329):
  - Ottenere condizione corrente: `PlantCondition currentCondition = (PlantCondition)_potState.ConditionLabel`
  - Calcolare moltiplicatore produzione: `float productionMultiplier = ConditionGrowthModifier.GetProductionMultiplier(currentCondition)`
  - Applicare moltiplicatore: `baseAmount *= productionMultiplier`
  - **IMPORTANTE**: Arrotondare a intero: `baseAmount = Mathf.RoundToInt(baseAmount)` (i frutti sono sempre interi, non decimali)
  - Esempio: se baseAmount = 3.45 → diventa 3, se baseAmount = 3.55 → diventa 4
  - Log debug: mostrare moltiplicatore applicato e valore arrotondato
- Verificare se condizione "Infestata" esiste nel sistema (potrebbe essere gestita via MoldRiskLevel)

**Riferimenti**:

- Metodo `DoHarvest()` righe ~1328-1367
- Modificatori livello già presenti righe ~1331-1337
- Arrotondamento già presente riga ~1340: `int fruitsToHarvest = Mathf.RoundToInt(baseAmount)`

**Dipendenze**: Task 1.1

---

## FASE 2: Effetti pH Estremi su Resa e Crescita

### Task 2.1: Creare PhGrowthModifier + Tooltip Ph Drift

**File**: `Assets/_Project/Scripts/Dome/PotSystem/Growth/PhGrowthModifier.cs` (NUOVO)

**Funzionalità**:

```csharp
public static class PhGrowthModifier
{
    /// <summary>
    /// Calcola moltiplicatore crescita basato su banda pH e famiglia pianta
    /// </summary>
    public static float GetGrowthMultiplier(PhSystem.PhBand phBand, PlantFamily family)
    {
        // Thriving: pH favorevole alla famiglia
        // Weakening: pH opposto ma non estremo
        // Collapsing: pH estremo opposto (già gestito da countdown morte)
        // Stable: pH neutrale
    }
    
    /// <summary>
    /// Calcola moltiplicatore resa basato su banda pH e famiglia pianta
    /// </summary>
    public static float GetYieldMultiplier(PhSystem.PhBand phBand, PlantFamily family)
    {
        // Ultra Acido: Evil +50% resa, Pure collassano (countdown)
        // Ultra Basico: Pure iper-produttive ma sterili, Evil collassano (countdown)
    }
}
```

**Valori GDD**:

- Thriving (pH favorevole): +50% crescita (1.5f)
- Stable (neutrale): 0% (1.0f)
- Weakening (pH opposto): -30% crescita (0.7f)
- Ultra Acido: Evil +50% resa (1.5f)
- Ultra Basico: Pure +100% resa ma sterili (2.0f)

**Aggiornare tooltip Ph Drift Calcolo** (verificare dove viene mostrato):

- Aggiungere testo che spiega beneficio/svantaggio del pH attuale per la famiglia pianta
- Esempio: "pH Ultra Basico: Pure +50% crescita, +100% resa (sterili), Evil collassano"
- Mostrare modificatori in modo chiaro e leggibile

**Dipendenze**: Nessuna

---

### Task 2.2: Applicare modificatori crescita pH in ResolveGrowthForPot

**File**: `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

**Modifiche**:

- In `ResolveGrowthForPot()`, dopo calcolo punti crescita:
  - Ottenere banda pH: `PhSystem.PhBand phBand = _phSystem.EvaluateState()`
  - Ottenere famiglia: `PlantFamily family = plantData.Family`
  - Calcolare moltiplicatore: `float phMultiplier = PhGrowthModifier.GetGrowthMultiplier(phBand, family)`
  - Applicare moltiplicatore: `gained *= phMultiplier`
  - Log debug: mostrare moltiplicatore pH applicato
- Integrare con modificatore condizione (moltiplicatori cumulativi)

**Riferimenti**:

- Metodo `ResolveGrowthForPot()` righe ~462-890
- Verifica pH estremo già presente righe ~692-729

**Dipendenze**: Task 2.1

---

### Task 2.3: Applicare modificatori resa pH in DoHarvest (con arrotondamento)

**File**: `Assets/_Project/Scripts/Dome/PotActions.cs`

**Modifiche**:

- In `DoHarvest()`, dopo calcolo `baseAmount` e prima di applicare modificatori livello:
  - Ottenere banda pH: `PhSystem.PhBand phBand = _phSystem.EvaluateState()`
  - Ottenere famiglia: `PlantFamily family = plantData.Family`
  - Calcolare moltiplicatore resa: `float phYieldMultiplier = PhGrowthModifier.GetYieldMultiplier(phBand, family)`
  - Applicare moltiplicatore: `baseAmount *= phYieldMultiplier`
  - **IMPORTANTE**: Arrotondare a intero dopo ogni moltiplicatore applicato
  - Log debug: mostrare moltiplicatore pH applicato
- Gestire sterilità Pure in Ultra Basico:
  - Aggiungere flag `DaysSterile` in PotStateModel (se non esiste)
  - Se Pure in Ultra Basico: attivare sterilità (3 giorni), resa x2 ma non può produrre frutti

**Riferimenti**:

- Metodo `DoHarvest()` righe ~1328-1367
- Verificare se `DaysSterile` esiste in PotStateModel
- Arrotondamento già presente riga ~1340

**Dipendenze**: Task 2.1

---

## FASE 3: Sistema Burn Stress Completo da LED

### Task 3.1: Implementare applicazione Burn Stress in ApplyLedEffects

**File**: `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

**Modifiche**:

- Rimuovere TODO riga 1547
- In `ApplyLedEffects()`, dopo calcolo `malusMultiplier`:
  - Se `consecutiveDays >= maxDaysForFullStress`:
    - Applicare malus aggiuntivo al calcolo condizione (già presente in PlantConditionSystem)
    - Verificare se serve applicare effetti aggiuntivi (regressione stage, -1 livello) per Burn Stress estremo
  - Log warning quando Burn Stress attivo
- Verificare integrazione con `PlantConditionSystem` (malus già presente righe 354-378)

**Riferimenti**:

- Metodo `ApplyLedEffects()` righe ~1504-1554
- TODO riga 1547
- `PlantConditionSystem.CalculateCondition()` già gestisce Burn Stress

**Dipendenze**: Nessuna (malus già nel calcolo condizione, serve solo applicazione completa)

---

### Task 3.2: Aggiungere effetti Burn Stress estremi dopo 3 giorni

**File**: `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

**Modifiche**:

- Se Burn Stress persiste per **3 giorni consecutivi** (non 7):
  - Regressione stage (torna allo stadio precedente)
  - Riduzione livello (-1 livello, minimo 1)
- Tracking giorni Burn Stress consecutivi:
  - Aggiungere campo `DaysBurnStressConsecutive` in PotStateModel (se non esiste)
  - Incrementare quando `consecutiveDays >= maxDaysForFullStress`
  - Reset quando LED spento o stress ridotto
- Log warning quando effetti estremi applicati

**Dipendenze**: Task 3.1

---

## FASE 4: Indicatore Giorni LED Consecutivi nell'HUD

### Task 4.1: Aggiungere indicatore giorni consecutivi in PlantCardV2

**File**: `Assets/_Project/Scripts/UI/UIToolkit/PlantCard/PlantCardV2DataBinder.cs`

**Modifiche**:

- Nella sezione LED display (verificare dove viene mostrato lo stato LED):
  - Aggiungere testo: "Giorni consecutivi: X"
  - Mostrare solo se LED è attivo (non OFF)
  - Formato: `"LED {state} ({consecutiveDays} giorni)"`
- Verificare se esiste già sezione LED display

**Riferimenti**:

- Cercare dove viene mostrato `LedSystemState` nell'HUD
- Metodo `GetConsecutiveLedDays()` in PotStateModel righe ~487-496

**Dipendenze**: Nessuna

---

### Task 4.2: Aggiungere indicatore in AlwaysVisiblePotHUD

**File**: `Assets/_Project/Scripts/UI/VaultMap/AlwaysVisiblePotHUD.cs`

**Modifiche**:

- Nella sezione tooltip crescita (righe ~1153-1193):
  - Aggiungere informazioni giorni consecutivi LED
  - Mostrare stress percentage e burn risk level
- Formato: `"LED: {state} ({consecutiveDays} giorni, Stress: {stressPercentage}%)"`

**Riferimenti**:

- Metodo tooltip crescita righe ~1153-1193
- Calcolo stress percentage già presente righe ~1189-1192

**Dipendenze**: Nessuna

---

## Ordine di Implementazione

1. **FASE 1** (Modificatori Condizioni) - Impatto immediato su gameplay
2. **FASE 2** (Effetti pH Estremi) - Completa sistema pH esistente
3. **FASE 3** (Burn Stress) - Completamento sistema LED
4. **FASE 4** (Indicatori HUD) - Miglioramento UX
5. **FASE 5** (Slot Passivi) - **RIMANDATA**: da implementare solo dopo completamento e test delle Fasi 1-4

---

## Scenari di Testing

Ogni fase implementata deve essere testata in Play mode prima di procedere alla successiva. Di seguito scenari di test specifici per verificare il corretto funzionamento.

### TEST FASE 1: Modificatori Condizioni su Crescita e Produzione

#### Test 1.1: Verifica Modificatori Crescita - Condizione Rigogliosa

**Setup**:

1. Pianta una pianta Standard in un vaso
2. Mantieni condizioni ottimali (acqua, LED, pH) per raggiungere condizione Rigogliosa
3. Verifica che la condizione sia effettivamente Rigogliosa (score 80-100)

**Test**:

1. Avanza 1 giorno con cura ideale (acqua + LED)
2. Controlla log debug: deve mostrare moltiplicatore crescita 1.2f applicato
3. Verifica che i punti crescita guadagnati siano maggiori del normale (es. 2 punti base → 2.4 punti con Rigogliosa)
4. Verifica che la pianta avanzi di stadio più velocemente rispetto a una pianta Sana

**Risultato Atteso**: Pianta Rigogliosa cresce 20% più velocemente

---

#### Test 1.2: Verifica Modificatori Crescita - Condizione Stressata

**Setup**:

1. Pianta una pianta Standard
2. Mantieni condizioni sub-ottimali per raggiungere condizione Stressata (score 40-60)

**Test**:

1. Avanza 1 giorno con cura ideale
2. Controlla log debug: deve mostrare moltiplicatore crescita 0.9f applicato
3. Verifica che i punti crescita siano ridotti del 10% rispetto a Sana

**Risultato Atteso**: Pianta Stressata cresce 10% più lentamente

---

#### Test 1.3: Verifica Modificatori Produzione - Condizione Rigogliosa

**Setup**:

1. Pianta una pianta e portala a HarvestReady
2. Mantieni condizione Rigogliosa
3. Verifica che la pianta abbia 3 frutti disponibili (base)

**Test**:

1. Esegui DoHarvest()
2. Controlla log debug: deve mostrare moltiplicatore produzione 1.15f applicato
3. Verifica quantità frutti raccolti: 3 × 1.15 = 3.45 → arrotondato a 3 o 4 frutti (sempre intero)
4. Verifica che il tooltip Conditions mostri "+15% produzione frutti"

**Risultato Atteso**: Pianta Rigogliosa produce 1 frutto extra (3 → 4) o mantiene 3 se arrotondamento verso il basso

---

#### Test 1.4: Verifica Modificatori Produzione - Condizione Stressata

**Setup**:

1. Pianta una pianta e portala a HarvestReady
2. Mantieni condizione Stressata
3. Verifica che la pianta abbia 3 frutti disponibili (base)

**Test**:

1. Esegui DoHarvest()
2. Controlla log debug: deve mostrare moltiplicatore produzione 0.85f applicato
3. Verifica quantità frutti raccolti: 3 × 0.85 = 2.55 → arrotondato a 3 o 2 frutti (sempre intero)
4. Verifica che il tooltip Conditions mostri "-15% produzione frutti"

**Risultato Atteso**: Pianta Stressata produce 1 frutto in meno (3 → 2) o mantiene 3 se arrotondamento verso l'alto

---

#### Test 1.5: Verifica Tooltip Conditions Aggiornato

**Setup**:

1. Pianta una pianta e portala a qualsiasi stadio
2. Verifica condizione corrente (Rigogliosa, Sana, Stressata, etc.)

**Test**:

1. Apri PlantCard o tooltip Conditions
2. Verifica che il tooltip mostri:

   - Condizione corrente
   - Modificatore crescita (es. "+20% velocità crescita" per Rigogliosa)
   - Modificatore produzione (es. "+15% produzione frutti" per Rigogliosa)

3. Cambia condizione e verifica che il tooltip si aggiorni

**Risultato Atteso**: Tooltip mostra chiaramente impatto Bonus/Malus su crescita e produzione

---

### TEST FASE 2: Effetti pH Estremi su Resa e Crescita

#### Test 2.1: Verifica Modificatori Crescita pH - Pure in Ultra Basico (Thriving)

**Setup**:

1. Pianta una pianta Pure
2. Porta pH a Ultra Basico (≥+80) usando Blue LED o altre azioni
3. Verifica che la pianta sia in condizione Sana o Rigogliosa (non in countdown morte)

**Test**:

1. Avanza 1 giorno con cura ideale
2. Controlla log debug: deve mostrare moltiplicatore pH crescita 1.5f applicato
3. Verifica che i punti crescita siano aumentati del 50% rispetto al normale
4. Verifica che la pianta avanzi di stadio più velocemente
5. Verifica che il tooltip Ph Drift mostri "+50% crescita" per Pure in Ultra Basico

**Risultato Atteso**: Pure in Ultra Basico cresce 50% più velocemente

---

#### Test 2.2: Verifica Modificatori Crescita pH - Evil in Ultra Acido (Thriving)

**Setup**:

1. Pianta una pianta Evil
2. Porta pH a Ultra Acido (≤-80) usando Red LED o altre azioni
3. Verifica che la pianta sia in condizione Sana o Rigogliosa

**Test**:

1. Avanza 1 giorno con cura ideale
2. Controlla log debug: deve mostrare moltiplicatore pH crescita 1.5f applicato
3. Verifica che i punti crescita siano aumentati del 50%
4. Verifica che il tooltip Ph Drift mostri "+50% crescita" per Evil in Ultra Acido

**Risultato Atteso**: Evil in Ultra Acido cresce 50% più velocemente

---

#### Test 2.3: Verifica Modificatori Crescita pH - Pure in Ultra Acido (Weakening)

**Setup**:

1. Pianta una pianta Pure
2. Porta pH a Ultra Acido (≤-80) ma NON in countdown morte (pH tra -80 e -50, StableAcid)

**Test**:

1. Avanza 1 giorno con cura ideale
2. Controlla log debug: deve mostrare moltiplicatore pH crescita 0.7f applicato
3. Verifica che i punti crescita siano ridotti del 30%
4. Verifica che il tooltip Ph Drift mostri "-30% crescita" per Pure in Ultra Acido

**Risultato Atteso**: Pure in Ultra Acido cresce 30% più lentamente (se non in countdown morte)

---

#### Test 2.4: Verifica Modificatori Resa pH - Evil in Ultra Acido (+50% resa)

**Setup**:

1. Pianta una pianta Evil
2. Porta pH a Ultra Acido (≤-80)
3. Porta la pianta a HarvestReady con 3 frutti disponibili

**Test**:

1. Esegui DoHarvest()
2. Controlla log debug: deve mostrare moltiplicatore pH resa 1.5f applicato
3. Verifica quantità frutti raccolti: 3 × 1.5 = 4.5 → arrotondato a 4 o 5 frutti (sempre intero)
4. Verifica che il tooltip Ph Drift mostri "+50% resa frutti" per Evil in Ultra Acido

**Risultato Atteso**: Evil in Ultra Acido produce 1-2 frutti extra (3 → 4 o 5)

---

#### Test 2.5: Verifica Modificatori Resa pH - Pure in Ultra Basico (+100% resa ma sterili)

**Setup**:

1. Pianta una pianta Pure
2. Porta pH a Ultra Basico (≥+80)
3. Porta la pianta a HarvestReady con 3 frutti disponibili

**Test**:

1. Esegui DoHarvest()
2. Controlla log debug: deve mostrare moltiplicatore pH resa 2.0f applicato
3. Verifica quantità frutti raccolti: 3 × 2.0 = 6 frutti (sempre intero)
4. Verifica che `DaysSterile` sia attivo (3 giorni)
5. Verifica che la pianta non possa produrre nuovi frutti per 3 giorni (sterilità)
6. Verifica che il tooltip Ph Drift mostri "+100% resa (sterili)" per Pure in Ultra Basico

**Risultato Atteso**: Pure in Ultra Basico produce 6 frutti ma diventa sterile per 3 giorni

---

#### Test 2.6: Verifica Tooltip Ph Drift Aggiornato

**Setup**:

1. Pianta una pianta Pure o Evil
2. Modifica pH a diverse bande (Ultra Acido, Stable Acid, Neutrale, Stable Basic, Ultra Basic)

**Test**:

1. Apri tooltip Ph Drift o visualizzazione pH
2. Per ogni banda pH, verifica che il tooltip mostri:

   - Banda pH corrente
   - Modificatore crescita per la famiglia pianta (es. "+50% crescita" per Pure in Ultra Basico)
   - Modificatore resa per la famiglia pianta (es. "+100% resa (sterili)" per Pure in Ultra Basico)

3. Cambia famiglia pianta (Pure/Evil) e verifica che il tooltip si aggiorni

**Risultato Atteso**: Tooltip mostra chiaramente beneficio/svantaggio del pH attuale per la famiglia pianta

---

#### Test 2.7: Verifica Modificatori Cumulativi (Condizione + pH)

**Setup**:

1. Pianta una pianta Pure
2. Porta pH a Ultra Basico (≥+80)
3. Mantieni condizione Rigogliosa

**Test**:

1. Avanza 1 giorno con cura ideale
2. Controlla log debug: deve mostrare entrambi i moltiplicatori:

   - Condizione: 1.2f (Rigogliosa)
   - pH: 1.5f (Thriving)
   - Totale: 1.2 × 1.5 = 1.8f (moltiplicativi, non additivi)

3. Verifica che i punti crescita siano aumentati dell'80% rispetto al normale

**Risultato Atteso**: Modificatori condizione e pH sono moltiplicativi (1.2 × 1.5 = 1.8), non additivi (1.2 + 1.5 = 2.7)

---

### TEST FASE 3: Sistema Burn Stress Completo da LED

#### Test 3.1: Verifica Burn Stress Attivo (100% stress)

**Setup**:

1. Pianta una pianta Standard
2. Attiva LED Red o Blue
3. Lascia LED acceso per 5+ giorni consecutivi (o `maxDaysForFullStress`)

**Test**:

1. Avanza giorni fino a raggiungere 100% stress (consecutiveDays >= maxDaysForFullStress)
2. Controlla log debug: deve mostrare Burn Stress attivo
3. Verifica che la condizione della pianta peggiori (malus applicato)
4. Verifica che il tooltip Conditions mostri "Burn Stress attivo (100%)"
5. Verifica che il calcolo condizione includa il malus Burn Stress

**Risultato Atteso**: Burn Stress attivo riduce lo score di condizione quando stress = 100%

---

#### Test 3.2: Verifica Effetti Burn Stress Estremi (3 giorni consecutivi)

**Setup**:

1. Pianta una pianta e portala almeno a Growth
2. Attiva LED Red o Blue
3. Lascia LED acceso per 5+ giorni consecutivi (100% stress)
4. Mantieni Burn Stress attivo per 3 giorni consecutivi

**Test**:

1. Avanza 3 giorni con Burn Stress attivo (100% stress)
2. Dopo il 3° giorno consecutivo:

   - Verifica regressione stage (torna allo stadio precedente)
   - Se pianta è Lvl 2+, verifica riduzione livello (-1 livello, minimo 1)

3. Controlla log debug: deve mostrare effetti estremi applicati
4. Verifica che la pianta non possa regredere sotto Seed o livello 1

**Risultato Atteso**: Dopo 3 giorni consecutivi di Burn Stress, pianta regredge di stage e perde 1 livello

---

#### Test 3.3: Verifica Reset Burn Stress

**Setup**:

1. Pianta una pianta
2. Attiva LED e raggiungi 100% stress
3. Mantieni Burn Stress per 2 giorni consecutivi

**Test**:

1. Spegni LED (OFF)
2. Avanza 1 giorno
3. Verifica che `DaysBurnStressConsecutive` si resetti a 0
4. Verifica che gli effetti estremi non si attivino
5. Verifica che la condizione migliori (malus Burn Stress rimosso)

**Risultato Atteso**: Spegnendo LED, Burn Stress si resetta e non applica effetti estremi

---

### TEST FASE 4: Indicatore Giorni LED Consecutivi nell'HUD

#### Test 4.1: Verifica Indicatore in PlantCardV2

**Setup**:

1. Pianta una pianta Standard
2. Attiva LED Red o Blue

**Test**:

1. Seleziona il vaso con la pianta
2. Apri PlantCardV2
3. Verifica che nella sezione LED display sia mostrato:

   - Stato LED corrente (BLUE o RED)
   - Giorni consecutivi: "LED BLUE (3 giorni)" o "LED RED (5 giorni)"

4. Spegni LED (OFF) e verifica che l'indicatore scompaia o mostri "LED OFF"
5. Riattiva LED e verifica che l'indicatore riappaia

**Risultato Atteso**: PlantCardV2 mostra giorni consecutivi LED quando LED è attivo

---

#### Test 4.2: Verifica Indicatore in AlwaysVisiblePotHUD Tooltip

**Setup**:

1. Pianta una pianta Standard
2. Attiva LED Red o Blue
3. Lascia LED acceso per alcuni giorni

**Test**:

1. Passa il mouse sul vaso (tooltip sempre visibile)
2. Verifica che nel tooltip crescita sia mostrato:

   - "LED: BLUE (3 giorni, Stress: 60%)" o formato simile
   - Stress percentage calcolato correttamente
   - Burn risk level se applicabile

3. Cambia giorni consecutivi e verifica che il tooltip si aggiorni
4. Verifica che stress percentage sia calcolato correttamente (consecutiveDays / maxDaysForFullStress * 100)

**Risultato Atteso**: Tooltip AlwaysVisiblePotHUD mostra giorni consecutivi, stress percentage e burn risk level

---

#### Test 4.3: Verifica Aggiornamento Real-time

**Setup**:

1. Pianta una pianta
2. Attiva LED

**Test**:

1. Verifica che l'indicatore giorni consecutivi si aggiorni automaticamente a fine giornata
2. Avanza 1 giorno e verifica che il contatore incrementi
3. Spegni LED e verifica che il contatore si azzeri o diminuisca gradualmente
4. Verifica che l'aggiornamento sia visibile senza dover riaprire PlantCard

**Risultato Atteso**: Indicatori si aggiornano automaticamente a fine giornata senza refresh manuale

---

### TEST INTEGRAZIONE: Verifica Modificatori Combinati

#### Test Integrazione 1: Condizione + pH + Burn Stress

**Setup**:

1. Pianta una pianta Pure
2. Porta pH a Ultra Basico (≥+80)
3. Mantieni condizione Rigogliosa
4. Attiva LED Blue e lascia acceso per 5+ giorni (Burn Stress attivo)

**Test**:

1. Avanza 1 giorno con cura ideale
2. Controlla log debug: deve mostrare:

   - Modificatore condizione: 1.2f (Rigogliosa)
   - Modificatore pH: 1.5f (Thriving)
   - Malus Burn Stress: applicato al calcolo condizione

3. Verifica che i punti crescita siano: base × 1.2 × 1.5 = base × 1.8
4. Verifica che la condizione peggiori a causa del Burn Stress (malus)
5. Verifica che tutti i tooltip mostrino correttamente i modificatori

**Risultato Atteso**: Tutti i modificatori funzionano correttamente insieme (moltiplicativi per crescita, additivi per condizione)

---

#### Test Integrazione 2: Produzione con Condizione + pH

**Setup**:

1. Pianta una pianta Evil
2. Porta pH a Ultra Acido (≤-80)
3. Mantieni condizione Rigogliosa
4. Porta la pianta a HarvestReady con 3 frutti

**Test**:

1. Esegui DoHarvest()
2. Controlla log debug: deve mostrare:

   - Modificatore condizione: 1.15f (Rigogliosa)
   - Modificatore pH: 1.5f (Evil in Ultra Acido)
   - Totale: 3 × 1.15 × 1.5 = 5.175 → arrotondato a 5 frutti (sempre intero)

3. Verifica che i frutti raccolti siano 5 (non 6, perché arrotondamento dopo ogni moltiplicatore o totale)
4. Verifica che entrambi i tooltip (Conditions e Ph Drift) mostrino i modificatori

**Risultato Atteso**: Produzione finale = base × condizione × pH, arrotondato a intero

---

## Checklist Testing per Fase

### FASE 1 - Checklist:

- [ ] Modificatore crescita Rigogliosa (+20%) funziona
- [ ] Modificatore crescita Stressata (-10%) funziona
- [ ] Modificatore produzione Rigogliosa (+15%) funziona e arrotonda correttamente
- [ ] Modificatore produzione Stressata (-15%) funziona e arrotonda correttamente
- [ ] Tooltip Conditions mostra modificatori crescita e produzione
- [ ] Log debug mostra moltiplicatori applicati

### FASE 2 - Checklist:

- [ ] Modificatore crescita pH Thriving (+50%) funziona per Pure/Evil
- [ ] Modificatore crescita pH Weakening (-30%) funziona
- [ ] Modificatore resa pH Ultra Acido (+50% Evil) funziona e arrotonda correttamente
- [ ] Modificatore resa pH Ultra Basico (+100% Pure) funziona e arrotonda correttamente
- [ ] Sterilità Pure in Ultra Basico funziona (3 giorni, no produzione)
- [ ] Tooltip Ph Drift mostra modificatori crescita e resa
- [ ] Modificatori condizione + pH sono moltiplicativi (non additivi)

### FASE 3 - Checklist:

- [ ] Burn Stress attivo quando stress = 100%
- [ ] Malus Burn Stress applicato al calcolo condizione
- [ ] Effetti estremi dopo 3 giorni consecutivi (regressione stage, -1 livello)
- [ ] Reset Burn Stress quando LED spento
- [ ] Log debug mostra Burn Stress attivo

### FASE 4 - Checklist:

- [ ] Indicatore giorni consecutivi visibile in PlantCardV2
- [ ] Indicatore giorni consecutivi visibile in AlwaysVisiblePotHUD tooltip
- [ ] Indicatore mostra stress percentage e burn risk level
- [ ] Indicatori si aggiornano automaticamente a fine giornata
- [ ] Indicatore scompare quando LED è OFF

---

## Note Implementative

- **Modificatori Cumulativi**: I modificatori crescita (condizione + pH) devono essere moltiplicativi, non additivi
- **Arrotondamento Frutti**: I frutti sono sempre interi, arrotondare dopo ogni moltiplicatore applicato
- **Sterilità Pure**: Il sistema sterilità per Pure in Ultra Basico può essere semplificato (flag booleano invece di countdown)
- **Burn Stress Estremi**: Effetti estremi dopo 3 giorni consecutivi (non 7)
- **Tooltip**: Aggiornare tooltip Conditions e Ph Drift per mostrare impatti gameplay
- **Testing**: Ogni fase deve essere testata prima di procedere alla successiva
- **FASE 5**: Slot Passivi verranno implementati solo dopo completamento e test delle Fasi 1-4