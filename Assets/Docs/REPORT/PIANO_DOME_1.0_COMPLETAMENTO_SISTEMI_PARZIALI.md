# Dome 1.0 - Completamento Sistemi Parziali

**Data Creazione:** 2025-12-13  
**Versione GDD:** 40 v.08/12/2025  
**Versione Repository:** main (REPOMAIN)  
**Stato:** Piano da implementare

## Obiettivo
Completare tutti i sistemi parzialmente implementati della Dome (env001) per raggiungere il 100% di conformità con il GDD 40 v.08/12/2025 per quanto riguarda i sistemi legati alle piante.

## Architettura di Riferimento
- **Pattern**: ServiceContainer per dependency injection
- **Config**: ScriptableObject per dati configurabili
- **Eventi**: PotEvents per comunicazione tra sistemi
- **File chiave**: 
  - `Assets/_Project/Scripts/Dome/PotStateModel.cs` - Modello stato vaso
  - `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs` - Controller ciclo giornaliero
  - `Assets/_Project/Scripts/Dome/PotSystem/Condition/PlantConditionSystem.cs` - Sistema condizioni
  - `Assets/_Project/Scripts/Dome/PotSystem/Level/PlantLevelSystem.cs` - Sistema livelli

---

## FASE 1: Visualizzazione Sistema Condizioni Completa

### Obiettivo
Completare la visualizzazione UI del sistema condizioni pianta con tutti gli stati distinti, progress bar segmentata, badge visivi e tooltip avanzati.

### Task 1.1: Stati Condizioni Distinti
**File**: `Assets/_Project/Scripts/Dome/PotSystem/Condition/PlantCondition.cs`

- Verificare enum `PlantCondition` contiene tutti gli stati: Rigogliosa, Sana, Stressata, Appassita, Infestata, Overwatered, Thirsty, Overlit, Light-starved, Burned, Sterile
- Aggiungere placeholder per stato "Burned" se mancante
- Verificare mapping corretto da `PlantConditionSystem.CalculateCondition()` a enum

### Task 1.2: Progress Bar Segmentata
**File**: `Assets/_Project/Scripts/UI/VaultMap/PotHUDWidget.cs`

- Implementare progress bar segmentata per visualizzare contributi multipli alla condizione
- Mostrare segmenti colorati per: idratazione, luce, pH, muffe, burn stress
- Aggiornare `UpdateConditionDisplay()` per utilizzare barra segmentata

### Task 1.3: Badge Visivi Condizioni
**File**: `Assets/_Project/Scripts/UI/VaultMap/PotHUDWidget.cs`

- Aggiungere badge icona per condizioni critiche (Burned, Infestata, Sterile)
- Badge colorati: rosso (critico), arancione (stress), verde (ottimale)
- Posizionare badge sopra/sotto progress bar

### Task 1.4: Tooltip Avanzato Condizioni
**File**: `Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs`

- Espandere tooltip condizione con breakdown dettagliato:
  - Lista contributori positivi/negativi
  - Valori numerici per ogni fattore
  - Suggerimenti per migliorare condizione
- Integrare con `PlantConditionSystem.GetConditionContributors()`

### Task 1.5: Toast Notifiche Critiche
**File**: `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

- Aggiungere notifiche toast quando condizione scende sotto soglia critica
- Trigger per: Burned, Infestata, Sterile
- Integrare con `UINotification` esistente

**Criteri Accettazione**:
- Tutti gli stati condizioni visualizzati correttamente
- Progress bar segmentata mostra contributi multipli
- Badge visibili per condizioni critiche
- Tooltip mostra breakdown completo
- Toast notifiche funzionanti per condizioni critiche

---

## FASE 2: Scaling LED e Burn Stress Completo

### Obiettivo
Completare il sistema LED persistente con scaling effetti pH e sistema Burn Stress completo con regressione stage, riduzione livello e sterilità temporanea.

### Task 2.1: Verifica Scaling pH LED
**File**: `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

- Verificare metodo `ApplyLedSystemEffects()` applica scaling pH:
  - 1 giorno consecutivo: x1 (Blue +2, Red -5)
  - 2-3 giorni consecutivi: x1.5 (Blue +3, Red -7.5)
  - 4+ giorni consecutivi: x2 (Blue +4, Red -10)
- Verificare tracking `DaysLedBlueConsecutive` e `DaysLedRedConsecutive` in `PotStateModel`

### Task 2.2: Effetti Sistemici IM (Placeholder)
**File**: `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

- Aggiungere placeholder per effetti sistemici su Mutations Index (IM)
- Logica: abuso LED prolungato aumenta IM (da implementare in Dome 2.0)
- Per ora: log debug con valore calcolato

### Task 2.3: Regressione Stage per Burned
**File**: `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

- Implementare regressione stage quando condizione = Burned
- Logica: se `DaysLedBlueConsecutive >= 4` o `DaysLedRedConsecutive >= 4` → condizione Burned
- Se Burned persistente (2+ giorni): regredire di 1 stage (es. Flowering → Growth)
- Aggiornare `ApplyLedSystemEffects()` per applicare regressione

### Task 2.4: Riduzione Livello per Burned
**File**: `Assets/_Project/Scripts/Dome/PotSystem/Level/PlantLevelSystem.cs`

- Estendere `ReduceLevel()` per supportare riduzione da Burn Stress
- Se Burned persistente (3+ giorni): ridurre livello di 1 (min Lvl 1)
- Integrare con `DayCycleController` per chiamare riduzione

### Task 2.5: Sterilità Temporanea
**File**: `Assets/_Project/Scripts/Dome/PotStateModel.cs`

- Aggiungere campo `DaysSterile` per tracking sterilità temporanea
- Se Burned persistente (4+ giorni): attivare sterilità (3 giorni)
- Durante sterilità: pianta non produce frutti anche in HarvestReady
- Aggiornare `DoHarvest()` in `PotActions.cs` per verificare sterilità

### Task 2.6: Completare Stato Burned
**File**: `Assets/_Project/Scripts/Dome/PotSystem/Condition/PlantConditionSystem.cs`

- Verificare `MapScoreToCondition()` mappa correttamente a `PlantCondition.Burned`
- Soglia: Burn Risk Level 3 + persistente 2+ giorni
- Aggiornare calcolo condizione per includere Burn Stress

### Task 2.7: UI Giorni Consecutivi LED
**File**: `Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs`

- Aggiungere display giorni consecutivi LED nella sezione Light Stress
- Mostrare: "LED Blu: X giorni consecutivi" / "LED Rosso: Y giorni consecutivi"
- Colore: verde (1), giallo (2-3), rosso (4+)

**Criteri Accettazione**:
- Scaling pH LED funzionante (x1, x1.5, x2)
- Regressione stage applicata per Burned persistente
- Riduzione livello applicata per Burned persistente 3+ giorni
- Sterilità temporanea attivata e funzionante
- Stato Burned mappato correttamente
- UI mostra giorni consecutivi LED

---

## FASE 3: Visualizzazione Sistema pH Estremi

### Obiettivo
Implementare UI completa per il sistema pH estremi con badge, countdown, barra progress, tooltip e notifiche persistenti.

### Task 3.1: Badge pH Estremi
**File**: `Assets/_Project/Scripts/UI/VaultMap/PotHUDWidget.cs`

- Aggiungere badge visivo quando pianta è in pH estremo opposto alla famiglia
- Badge rosso per: Pure in Ultra Acid (≤-80) o Evil in Ultra Basic (≥+80)
- Mostrare icona warning sopra pot

### Task 3.2: Countdown Morte pH Estremo
**File**: `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

- Verificare `KillPlantFromExtremePh()` funziona correttamente
- Verificare `DaysInExtremePh` e `ExtremePhDeathCountdown` in `PotStateModel`
- Countdown: 3 giorni in pH estremo opposto → morte pianta

### Task 3.3: Barra Progress Countdown
**File**: `Assets/_Project/Scripts/UI/VaultMap/PotHUDWidget.cs`

- Aggiungere barra progress per countdown morte pH estremo
- Mostrare: "Morte tra X giorni" con barra che si riempie
- Colore: verde (3 giorni) → giallo (2 giorni) → rosso (1 giorno)

### Task 3.4: Tooltip pH Estremi
**File**: `Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs`

- Espandere tooltip pH con informazioni pH estremi:
  - Banda pH corrente
  - Giorni rimanenti prima morte (se applicabile)
  - Suggerimenti per correggere pH
- Integrare con `PhSystem.EvaluateState()`

### Task 3.5: Integrazione PotDetailsWidget
**File**: `Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs`

- Aggiungere sezione "pH Estremo" nel widget dettagli
- Mostrare: banda pH, countdown, effetti su crescita/resa
- Aggiornare `UpdatePotDetails()` per includere informazioni pH estremi

### Task 3.6: Notifica Persistente pH Estremo
**File**: `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

- Verificare `ShowExtremePhCountdownNotification()` funziona correttamente
- Notifica persistente quando countdown < 2 giorni
- Integrare con `UINotification` per toast HUD

**Criteri Accettazione**:
- Badge pH estremi visibili quando applicabile
- Countdown morte funzionante (3 giorni)
- Barra progress countdown visualizzata correttamente
- Tooltip mostra informazioni pH estremi complete
- PotDetailsWidget mostra sezione pH estremo
- Notifiche persistenti funzionanti

---

## FASE 4: Sistema Slot Passivi (AZ-16)

### Obiettivo
Implementare sistema completo Slot Passivi per piante Level 5 con struttura dati, azione sposta, bonus passivi e UI.

### Task 4.1: Struttura Dati Slot Passivi
**File**: `Assets/_Project/Scripts/Dome/PassiveSlotSystem.cs` (NUOVO)

- Creare classe `PassiveSlotSystem` singleton
- Struttura: array di 3 `PassiveSlot` (ogni slot può contenere 1 pianta Lvl 5)
- Classe `PassiveSlot`: riferimento `PotStateModel`, `PlantData`, livello pianta
- Metodi: `AddPlantToSlot()`, `RemovePlantFromSlot()`, `GetSlotPlant()`, `IsSlotEmpty()`

### Task 4.2: Azione Sposta ↔ Slot Passivo
**File**: `Assets/_Project/Scripts/Dome/PotActions.cs`

- Implementare `DoMoveToPassiveSlot(int slotIndex)`:
  - Verifica: pianta Level 5, slot disponibile
  - Sposta pianta da vaso attivo a slot passivo
  - Aggiorna `PotStateModel.IsInPassiveSlot = true`
- Implementare `DoMoveFromPassiveSlot(int slotIndex)`:
  - Verifica: slot contiene pianta, vaso disponibile
  - Sposta pianta da slot passivo a vaso attivo
  - Aggiorna `PotStateModel.IsInPassiveSlot = false`

### Task 4.3: Bonus Passivi
**File**: `Assets/_Project/Scripts/Dome/PassiveSlotSystem.cs`

- Implementare calcolo bonus passivi:
  - Leggere `PlantData.ActivePower` per descrizione bonus
  - Applicare bonus globali (es. +10% crescita tutte piante, +5% resa, etc.)
  - Bonus scalano con livello pianta (da implementare in FASE 5)
- Metodo `GetGlobalPassiveBonuses()`: ritorna lista bonus attivi

### Task 4.4: UI Slot Passivi
**File**: `Assets/_Project/Scripts/UI/VaultMap/PassiveSlotsWidget.cs` (NUOVO)

- Creare widget UI per 3 slot passivi
- Mostrare: icona pianta, livello, nome, bonus attivo
- Pulsanti: "Sposta da Slot" / "Sposta in Slot"
- Integrare con `PassiveSlotSystem`

### Task 4.5: Integrazione pH Drift Cap 20%
**File**: `Assets/_Project/Scripts/Core/PhSystem.cs`

- Modificare calcolo drift pH giornaliero:
  - Piante in slot passivi contribuiscono drift pH cappato al 20% del valore normale
  - Esempio: Pure +2/giorno → in slot passivo +0.4/giorno
- Aggiornare `ApplyQueuedDrifts()` per applicare cap

**Criteri Accettazione**:
- Struttura dati slot passivi funzionante (3 slot)
- Azione sposta vaso ↔ slot passivo funzionante
- Bonus passivi applicati globalmente
- UI slot passivi visualizzata correttamente
- pH drift cappato al 20% per piante in slot passivi

---

## FASE 5: Effetti pH Estremi su Resa/Crescita + Scaling Active/Passive

### Obiettivo
Implementare effetti pH estremi completi su resa frutti e velocità crescita, e scaling effetti Active/Passive con livello pianta.

### Task 5.1: Effetti pH su Velocità Crescita
**File**: `Assets/_Project/Scripts/Dome/PotSystem/Growth/PlantGrowthConfig.cs`

- Utilizzare campo esistente `phGrowthMultiplier` per applicare modificatori crescita
- Calcolare moltiplicatore in base a banda pH e famiglia pianta:
  - **Thriving** (pH favorevole): +50% crescita (`phGrowthMultiplier = 1.5f`)
  - **Stable** (pH neutrale): crescita normale (`phGrowthMultiplier = 1.0f`)
  - **Weakening** (pH opposto): -30% crescita (`phGrowthMultiplier = 0.7f`)
  - **Collapsing** (pH estremo opposto): -50% crescita (`phGrowthMultiplier = 0.5f`)

### Task 5.2: Applicare Modificatori Crescita
**File**: `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

- Modificare calcolo punti crescita giornalieri:
  - Leggere `phGrowthMultiplier` da `PlantGrowthConfig`
  - Applicare moltiplicatore ai punti crescita: `points *= phGrowthMultiplier`
  - Integrare con calcolo esistente `pointsIdealCare`, `pointsPartialCare`
- Metodo helper: `CalculatePhGrowthMultiplier(PotStateModel pot, PhSystem phSystem)`

### Task 5.3: Effetti pH su Resa Frutti - Ultra Acido
**File**: `Assets/_Project/Scripts/Dome/PotActions.cs`

- Modificare `DoHarvest()` per applicare modificatori resa basati su pH:
  - Se pH ≤ -80 (Ultra Acido):
    - Piante **Evil**: +50% resa frutti (`baseAmount *= 1.5f`)
    - Piante **Pure**: collassano (già gestito da sistema morte pH estremo)
- Verificare famiglia pianta da `PlantData.Family`

### Task 5.4: Effetti pH su Resa Frutti - Ultra Basico
**File**: `Assets/_Project/Scripts/Dome/PotActions.cs`

- Continuare modifiche `DoHarvest()`:
  - Se pH ≥ +80 (Ultra Basico):
    - Piante **Pure**: iper-produttive ma sterili (`baseAmount *= 2.0f`, ma `DaysSterile > 0`)
    - Piante **Evil**: collassano (già gestito da sistema morte pH estremo)
- Attivare sterilità temporanea (3 giorni) per Pure in Ultra Basico

### Task 5.5: Scaling Effetti Active con Livello
**File**: `Assets/_Project/Scripts/Dome/PotSystem/Growth/PlantData.cs`

- Aggiungere metodo `GetActivePowerEffect(int level)`:
  - Scaling lineare: Lvl 1 = 100%, Lvl 2 = 120%, Lvl 3 = 140%, Lvl 4 = 160%, Lvl 5 = 180%
  - Ritorna moltiplicatore per effetti Active Power
- Metodo helper: `CalculateActivePowerMultiplier(int level)`

### Task 5.6: Scaling Effetti Passive con Livello
**File**: `Assets/_Project/Scripts/Dome/PassiveSlotSystem.cs`

- Modificare `GetGlobalPassiveBonuses()`:
  - Applicare scaling effetti passivi basato su livello pianta
  - Scaling lineare: Lvl 5 = 180% (stesso di Active)
  - Esempio: bonus +10% crescita → Lvl 5 = +18% crescita
- Metodo helper: `CalculatePassivePowerMultiplier(int level)`

### Task 5.7: UI Scaling Effetti
**File**: `Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs`

- Aggiornare tooltip Active Power:
  - Mostrare: "Potere Attivo: [descrizione] (Lvl X: +Y% efficacia)"
  - Calcolare e mostrare moltiplicatore corrente basato su livello
- Aggiornare UI Slot Passivi:
  - Mostrare: "Bonus Passivo: [descrizione] (Lvl X: +Y% efficacia)"
  - Calcolare e mostrare moltiplicatore corrente

**Criteri Accettazione**:
- Modificatori crescita applicati correttamente (Thriving/Weakening/Collapsing)
- Resa frutti modificata per Ultra Acido (Evil +50%)
- Resa frutti modificata per Ultra Basico (Pure x2 ma sterili)
- Scaling Active Power funzionante (Lvl 1-5)
- Scaling Passive Power funzionante (Lvl 5)
- UI mostra scaling effetti correttamente

---

## Ordine Implementazione

1. **FASE 1** → Visualizzazione Condizioni (base UI)
2. **FASE 2** → LED/Burn Stress (sistema core)
3. **FASE 3** → pH Estremi UI (visualizzazione)
4. **FASE 4** → Slot Passivi (feature late-game)
5. **FASE 5** → Effetti pH Resa/Crescita + Scaling (polish finale)

## Note Tecniche

- **Pattern**: Riutilizzare pattern UI da FASE 1 e FASE 2 per FASE 3
- **Testing**: Testare ogni fase prima di passare alla successiva
- **Config**: Utilizzare ScriptableObject esistenti (`PlantLevelConfig`, `MoldConfig`, `DifficultyCalibrationConfig`)
- **Eventi**: Utilizzare `PotEvents` per comunicazione tra sistemi
- **Logging**: Utilizzare `SporiumLogger` con categoria appropriata (`LogCategory.Pot`, `LogCategory.Ph`, etc.)

## Dipendenze

- **FASE 2** dipende da **FASE 1** (condizioni Burned)
- **FASE 3** dipende da **FASE 1** (pattern UI)
- **FASE 4** dipende da sistema Livelli esistente
- **FASE 5** dipende da **FASE 4** (scaling Passive) e sistema Livelli

