# Dome 2.0 - Sistema Mutazioni e Ibridi

**Data Creazione:** 2025-12-13  
**Versione GDD:** 40 v.08/12/2025  
**Versione Repository:** main (REPOMAIN)  
**Stato:** Piano da implementare (successivo a Dome 1.0)

## Obiettivo
Implementare i sistemi avanzati di Mutazioni e Ibridi per completare l'ecosistema Dome con feature late-game. Questo piano include anche il sistema Mutations Index (IM) per tracking instabilità genetica globale.

## Architettura di Riferimento
- **Pattern**: ServiceContainer per dependency injection
- **Config**: ScriptableObject per dati configurabili
- **Eventi**: PotEvents per comunicazione tra sistemi
- **File chiave**: 
  - `Assets/_Project/Scripts/Dome/PotStateModel.cs` - Modello stato vaso
  - `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs` - Controller ciclo giornaliero
  - `Assets/_Project/Scripts/Core/PhSystem.cs` - Sistema pH globale

---

## FASE 1: Mutations Index (IM) - Sistema Globale

### Obiettivo
Implementare sistema Mutations Index (IM) per tracking instabilità genetica globale della Dome, che influenza probabilità mutazioni e stabilità piante.

### Task 1.1: Struttura Dati Mutations Index
**File**: `Assets/_Project/Scripts/Dome/MutationsIndexSystem.cs` (NUOVO)

- Creare classe `MutationsIndexSystem` singleton
- Campo `_mutationsIndex` (float, range 0-100)
- Metodi: `GetMutationsIndex()`, `AddToMutationsIndex(float delta)`, `ResetMutationsIndex()`
- Evento: `OnMutationsIndexChanged(float newValue, float delta)`

### Task 1.2: Fonti Mutations Index
**File**: `Assets/_Project/Scripts/Dome/MutationsIndexSystem.cs`

- Implementare calcolo IM da varie fonti:
  - **Abuso LED**: +2 per giorno consecutivo oltre 3 giorni
  - **pH Estremi**: +1 per giorno in Ultra Acid/Basic opposto alla famiglia
  - **Muffe**: +3 per infestazione Mild, +5 per Severe
  - **Burn Stress**: +2 per giorno in stato Burned
  - **Decadimento**: -1 per giorno se nessuna fonte attiva

### Task 1.3: Effetti Mutations Index
**File**: `Assets/_Project/Scripts/Dome/MutationsIndexSystem.cs`

- Implementare effetti IM su probabilità mutazioni:
  - IM 0-20: probabilità base mutazioni
  - IM 21-50: +10% probabilità mutazioni
  - IM 51-75: +25% probabilità mutazioni
  - IM 76-100: +50% probabilità mutazioni + rischio mutazioni spontanee
- Metodo: `GetMutationProbabilityMultiplier()`

### Task 1.4: UI Mutations Index
**File**: `Assets/_Project/Scripts/UI/VaultMap/MutationsIndexWidget.cs` (NUOVO)

- Creare widget UI per visualizzare IM globale
- Mostrare: barra progress (0-100), valore corrente, effetti attivi
- Colore: verde (0-20), giallo (21-50), arancione (51-75), rosso (76-100)
- Posizionare in HUD Dome

**Criteri Accettazione**:
- Mutations Index calcolato correttamente da tutte le fonti
- Effetti IM applicati a probabilità mutazioni
- UI mostra IM globale con colori appropriati
- Eventi emessi correttamente per cambiamenti IM

---

## FASE 2: Sistema Mutazioni Base

### Obiettivo
Implementare sistema mutazioni completo con calcolo MutationScore, trigger mutazioni, e applicazione effetti.

### Task 2.1: Struttura Dati Mutazioni
**File**: `Assets/_Project/Scripts/Dome/PotSystem/Mutation/MutationSystem.cs` (NUOVO)

- Creare classe `MutationSystem` statica
- Enum `MutationType`: Armonica, Corrotta, Adattiva
- Classe `MutationData`: ScriptableObject con dati mutazione
- Metodi: `CalculateMutationScore()`, `TriggerMutation()`, `ApplyMutationEffects()`

### Task 2.2: MutationScore Calculator
**File**: `Assets/_Project/Scripts/Dome/PotSystem/Mutation/MutationSystem.cs`

- Implementare calcolo MutationScore:
  - **pH mismatch**: Neutral=0, Stable=+10, Ultra=+20
  - **Idratazione fuori banda**: +5/giorno (cap +20)
  - **LED abuse**: +10 (+5 extra se ripetuto)
  - **Muffa**: Mild +15, Severe +30
  - **Concime/Pruning**: Sacro +10 Armoniche, Proibito +10 Corrotte
  - **Mutations Index**: moltiplicatore basato su IM
- Metodo: `CalculateMutationScore(PotStateModel pot, PhSystem phSystem, MutationsIndexSystem imSystem)`

### Task 2.3: Determinazione Tipo Mutazione
**File**: `Assets/_Project/Scripts/Dome/PotSystem/Mutation/MutationSystem.cs`

- Implementare logica determinazione tipo mutazione:
  - **pH Acido** (Stable/Ultra Acid): bias verso Mutazioni Corrotte
  - **pH Basico** (Stable/Ultra Basic): bias verso Mutazioni Armoniche
  - **pH Neutrale**: bias verso Mutazioni Adattive
- Metodo: `DetermineMutationType(float mutationScore, PhSystem.PhBand phBand)`

### Task 2.4: Timing Mutazioni
**File**: `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

- Implementare check mutazioni a Dawn (dopo EndDay):
  - Verificare MutationScore > soglia (es. 30)
  - Verificare livello pianta (solo Lvl 1-3 possono mutare)
  - Applicare probabilità basata su IM
  - Trigger mutazione se condizioni soddisfatte
- Metodo: `CheckForMutationsAtDawn()`

### Task 2.5: Prime Mutazioni Implementate
**File**: `Assets/Resources/Mutations/` (NUOVO)

- Creare ScriptableObject per prime mutazioni:
  - **MUT-101** (Respiro di Luce) - Armonica: +20% crescita, drift pH controllato
  - **MUT-301** (Mildew Bloom) - Corrotta: +30% muffe, drift pH negativo extra
  - **MUT-401** (Spiral Growth) - Adattiva: compromessi variabili
- Configurare effetti mutazioni in `MutationData`

### Task 2.6: Applicazione Effetti Mutazioni
**File**: `Assets/_Project/Scripts/Dome/PotSystem/Mutation/MutationSystem.cs`

- Implementare applicazione effetti mutazioni:
  - Modificatori crescita/resa/stabilità
  - Modificatori drift pH
  - Modificatori rischio muffe
- Metodo: `ApplyMutationEffects(PotStateModel pot, MutationData mutation)`

### Task 2.7: UI Mutazioni
**File**: `Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs`

- Aggiungere sezione "Mutazioni" nel widget dettagli
- Mostrare: nome mutazione, tipo, effetti attivi
- Badge colorato: verde (Armonica), rosso (Corrotta), blu (Adattiva)
- Aggiornare `UpdatePotDetails()` per includere mutazioni

**Criteri Accettazione**:
- MutationScore calcolato correttamente da tutte le fonti
- Tipo mutazione determinato correttamente in base a pH
- Mutazioni si innescano a Dawn Check con probabilità corretta
- Solo Lvl 1-3 possono mutare
- Effetti mutazioni applicati correttamente
- UI mostra mutazioni attive

---

## FASE 3: Sistema Ibridi

### Obiettivo
Implementare sistema completo per creazione ibridi tramite DNA Fusion minigioco e selezione tratti.

### Task 3.1: Struttura Dati Ibridi
**File**: `Assets/_Project/Scripts/Lab/Cloning/HybridSystem.cs` (NUOVO)

- Creare classe `HybridSystem` singleton
- Classe `HybridData`: ScriptableObject con dati ibrido
- Metodi: `CheckHybridCompatibility()`, `CreateHybrid()`, `GetHybridTraits()`

### Task 3.2: Verifica Compatibilità Genitori
**File**: `Assets/_Project/Scripts/Lab/Cloning/HybridCompatibility.cs` (NUOVO)

- Implementare logica compatibilità:
  - Verificare famiglie genitori (Standard/Pure/Evil)
  - Verificare livello genitori (min Lvl 3 per ibridi avanzati)
  - Verificare mutazioni incompatibili
- Metodo: `CanCreateHybrid(PlantData parent1, PlantData parent2)`

### Task 3.3: DNA Fusion Minigioco
**File**: `Assets/_Project/Scripts/UI/Lab/Cloning/DNAFusionMinigame.cs` (NUOVO)

- Creare minigioco sequenza forme geometriche:
  - Mostrare sequenza di forme (cerchio, quadrato, triangolo)
  - Player deve replicare sequenza
  - Precisione determina qualità ibrido
- Metodo: `StartMinigame(PlantData parent1, PlantData parent2)`

### Task 3.4: Trait Selection System
**File**: `Assets/_Project/Scripts/Lab/Cloning/HybridSystem.cs`

- Implementare selezione tratti:
  - Analizzare tratti genitori (pH drift, resa, crescita, etc.)
  - Permettere selezione fino a 3 tratti
  - Generare codice ibrido (HYB-xxx)
- Metodo: `SelectTraits(PlantData parent1, PlantData parent2, int[] selectedTraits)`

### Task 3.5: Prime Ibridi Implementate
**File**: `Assets/Resources/Hybrids/` (NUOVO)

- Creare ScriptableObject per prime ibridi:
  - **HYB-201** (Ferric Tangle) - Standard × Evil: drift pH variabile, resa media
  - **HYB-203** (Aurablade Reed) - Standard × Pure: drift pH positivo, crescita accelerata
- Configurare tratti ibridi in `HybridData`

### Task 3.6: Integrazione con PlantDatabase
**File**: `Assets/_Project/Scripts/Dome/PotSystem/Growth/PlantDatabase.cs`

- Estendere `PlantDatabase` per supportare ibridi:
  - Metodo `RegisterHybrid(HybridData hybrid)`
  - Lookup ibridi per codice HYB-xxx
  - Integrare con sistema piantagione esistente

### Task 3.7: UI Creazione Ibridi
**File**: `Assets/_Project/Scripts/UI/Lab/Cloning/HybridCreationWidget.cs` (NUOVO)

- Creare widget UI per creazione ibridi:
  - Selezione genitori (2 piante)
  - Visualizzazione compatibilità
  - Avvio DNA Fusion minigioco
  - Selezione tratti
  - Conferma creazione ibrido
- Integrare con `HybridSystem`

**Criteri Accettazione**:
- Compatibilità genitori verificata correttamente
- DNA Fusion minigioco funzionante
- Selezione tratti funzionante (max 3)
- Ibridi creati correttamente con codice HYB-xxx
- Ibridi integrati con PlantDatabase
- UI creazione ibridi completa

---

## FASE 4: Integrazione e Polish

### Obiettivo
Completare integrazione tra Mutazioni, Ibridi e sistemi esistenti, e aggiungere polish finale.

### Task 4.1: Integrazione Mutazioni con Livelli
**File**: `Assets/_Project/Scripts/Dome/PotSystem/Level/PlantLevelSystem.cs`

- Verificare restrizione mutazioni solo Lvl 1-3:
  - Metodo `CanMutate(int level)`: ritorna `level >= 1 && level <= 3`
  - Aggiornare `CheckForMutationsAtDawn()` per verificare livello

### Task 4.2: Integrazione Mutazioni con pH
**File**: `Assets/_Project/Scripts/Dome/PotSystem/Mutation/MutationSystem.cs`

- Verificare effetti mutazioni su drift pH:
  - Mutazioni Corrotte: drift pH extra negativo
  - Mutazioni Armoniche: drift pH più controllato
  - Aggiornare calcolo drift pH giornaliero

### Task 4.3: Integrazione Ibridi con Mutazioni
**File**: `Assets/_Project/Scripts/Lab/Cloning/HybridSystem.cs`

- Verificare mutazioni incompatibili per ibridi:
  - Alcune mutazioni impediscono creazione ibridi
  - Verificare mutazioni genitori prima di creare ibrido

### Task 4.4: Eventi Sistema Mutazioni
**File**: `Assets/_Project/Scripts/Dome/PotEvents.cs`

- Aggiungere eventi mutazioni:
  - `OnMutationTriggered(PotStateModel pot, MutationData mutation)`
  - `OnMutationApplied(PotStateModel pot, MutationData mutation)`
- Integrare con sistemi UI esistenti

### Task 4.5: Eventi Sistema Ibridi
**File**: `Assets/_Project/Scripts/Dome/PotEvents.cs`

- Aggiungere eventi ibridi:
  - `OnHybridCreated(HybridData hybrid, PlantData parent1, PlantData parent2)`
  - `OnHybridPlanted(PotStateModel pot, HybridData hybrid)`
- Integrare con sistemi UI esistenti

### Task 4.6: Documentazione e Test
**File**: `Assets/Docs/REPORT/` (NUOVO)

- Creare documentazione sistema mutazioni:
  - README con esempi calcolo MutationScore
  - Guida test mutazioni
- Creare documentazione sistema ibridi:
  - README con esempi creazione ibridi
  - Guida test DNA Fusion minigioco

**Criteri Accettazione**:
- Mutazioni integrate correttamente con livelli e pH
- Ibridi integrati correttamente con mutazioni
- Eventi emessi correttamente per mutazioni e ibridi
- Documentazione completa creata
- Test manuali completati

---

## Ordine Implementazione

1. **FASE 1** → Mutations Index (sistema globale base)
2. **FASE 2** → Sistema Mutazioni (feature core)
3. **FASE 3** → Sistema Ibridi (feature late-game)
4. **FASE 4** → Integrazione e Polish (completamento)

## Note Tecniche

- **Pattern**: Riutilizzare pattern ServiceContainer e ScriptableObject
- **Testing**: Testare ogni fase prima di passare alla successiva
- **Config**: Utilizzare ScriptableObject per MutationData e HybridData
- **Eventi**: Utilizzare PotEvents per comunicazione tra sistemi
- **Logging**: Utilizzare `SporiumLogger` con categoria appropriata

## Dipendenze

- **FASE 2** dipende da **FASE 1** (Mutations Index necessario per probabilità mutazioni)
- **FASE 3** dipende da sistema Livelli esistente (min Lvl 3 per ibridi avanzati)
- **FASE 4** dipende da **FASE 2** e **FASE 3** (integrazione)

## Requisiti Pre-Implementazione

- **Dome 1.0 completata**: Slot Passivi, Burn Stress, pH Estremi devono essere implementati
- **Sistema Livelli funzionante**: Progressione Lvl 1-5 deve essere completa
- **Sistema pH funzionante**: pH globale e drift devono essere operativi

