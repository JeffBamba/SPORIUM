# 🎯 PIANO IMPLEMENTAZIONE SISTEMI SPORIUM
## Roadmap Strutturata per Completamento GDD

**Versione:** 1.0  
**Data Creazione:** 2025-01-XX  
**Autore:** Senior Developer Mode  
**Basato su:** Analisi GDD vs Repository Main

---

## 📋 PRINCIPI GUIDA

### Architettura da Rispettare
- ✅ **ServiceContainer Pattern** per dependency injection
- ✅ **Modularità**: ogni sistema è indipendente e testabile
- ✅ **Event-Driven**: comunicazione via EventSystem
- ✅ **ScriptableObject Config**: dati configurabili in Unity Editor
- ✅ **Namespace Organization**: `Sporae.Core`, `Sporae.Dome.PotSystem`, ecc.

### Regole di Implementazione
1. **Un sistema alla volta** - completare e testare prima di passare al successivo
2. **Backward Compatible** - non rompere sistemi esistenti
3. **Incremental** - ogni fase aggiunge valore testabile
4. **Documented** - ogni sistema deve avere README con esempi
5. **Tested** - almeno test manuali con scenari GDD

---

## 🗺️ ROADMAP GENERALE

```
FASE 0: Foundation (Sistemi Base) ──────────────────────────────── [COMPLETATA]
FASE 1: Core Dome Systems (pH, Livelli, Stadi) ─────────────────── [CRITICA]
FASE 2: Plant Systems (Catalog, Mutazioni, Ibridi) ──────────────── [ALTA]
FASE 3: Economic & Survival (Food Room, Seed Storage) ──────────── [ALTA]
FASE 4: Social Systems (Fazioni, Visitor Room) ─────────────────── [CRITICA]
FASE 5: Narrative Systems (Missioni, Atti) ─────────────────────── [MEDIA]
FASE 6: Polish & Advanced (Dome Avanzata, Late Game) ────────────── [BASSA]
```

---

## 📦 FASE 1: CORE DOME SYSTEMS
**Durata Stimata:** 3-4 settimane  
**Priorità:** CRITICA  
**Blocca:** Tutti gli altri sistemi avanzati

### Obiettivo
Implementare i sistemi fondamentali della Dome che sono prerequisiti per tutto il resto.

---

### 🎯 TASK 1.1: Sistema pH Globale Dome
**Codice Task:** `BLK-02.01`  
**Dipendenze:** Nessuna (sistema nuovo)  
**Rischio:** MEDIO

#### Implementazione

**1.1.1 - Creare pH System Core**
```
Assets/_Project/Scripts/Core/
├── PhSystem.cs                    # Sistema gestione pH globale
├── PhSystemConfig.cs              # ScriptableObject configurazione
└── PhSystemConfig.asset           # Asset configurazione default
```

**Specifiche Tecniche:**
- Range pH: -100 (Ultra Acido) → +100 (Ultra Basico)
- Valore globale unico per tutta la Dome
- Eventi: `OnPhChanged(float newPh, float delta)`
- Bande: UA (≤-80), SA (-79...-30), N (-29...+29), SB (+30...+79), UB (≥+80)

**1.1.2 - Integrare con GameManager**
- Registrare PhSystem in ServiceContainer
- Inizializzare pH neutro (0) al start
- Hook EndDay per drift pH giornaliero

**1.1.3 - Creare PhDriftCalculator**
```
Assets/_Project/Scripts/Dome/
└── PhDriftCalculator.cs           # Calcola drift pH da piante/azioni
```

**Logica Drift:**
- Per ogni pianta attiva: applicare drift specie (Pure +2, Evil -2, Standard ±0)
- Per azioni: Overwatering -5, Blue LED +5, Red LED -5, Antifungino +5
- Per eventi: Pioggia Acida -20, Muffa -10/giorno

**1.1.4 - UI pH Display**
```
Assets/_Project/Scripts/UI/VaultMap/
└── UIPhDisplay.cs                  # Widget visualizzazione pH Dome
```

**Criteri di Accettazione:**
- ✅ pH iniziale = 0 (Neutro)
- ✅ Drift giornaliero calcolato correttamente da piante attive
- ✅ Azioni modificano pH istantaneamente
- ✅ UI mostra pH corrente e banda (UA/SA/N/SB/UB)
- ✅ Eventi pH emessi correttamente

**Test Scenarios:**
1. Piantare 2 Pure → pH deve salire +4/giorno
2. Piantare 2 Evil → pH deve scendere -4/giorno
3. Overwatering → pH -5 immediato
4. Blue LED → pH +5 immediato

---

### 🎯 TASK 1.2: Sistema Livelli Piante (1-5)
**Codice Task:** `BLK-02.02`  
**Dipendenze:** Task 1.1 (pH necessario per effetti livelli)  
**Rischio:** MEDIO

#### Implementazione

**1.2.1 - Estendere PotStateModel**
```csharp
// Aggiungere a PotStateModel.cs
public int PlantLevel { get; set; } = 1;        // Livello corrente (1-5)
public int CompletedCycles { get; set; } = 0;    // Cicli completati
```

**1.2.2 - Creare PlantLevelSystem**
```
Assets/_Project/Scripts/Dome/PotSystem/
├── PlantLevelSystem.cs             # Gestione progressione livelli
└── PlantLevelConfig.cs             # Configurazione soglie livelli
```

**Logica Progressione:**
- Ciclo valido = Flowering → HarvestReady → Resting → (Fertilizzante)
- Ogni ciclo completo = +1 progress verso livello successivo
- Soglie: Lvl 1→2: 1 ciclo, Lvl 2→3: 2 cicli, Lvl 3→4: 3 cicli, Lvl 4→5: 4 cicli

**1.2.3 - Integrare con Sistema Crescita**
- Modificare `DayCycleController` per tracciare cicli completati
- Aggiungere check livello per slot passivi (solo Lvl 5)

**1.2.4 - Effetti Livelli su Resa**
- Lvl 1-2: resa invariata
- Lvl 3+: quantità -15%/livello, qualità crescente

**Criteri di Accettazione:**
- ✅ Pianta parte da Lvl 1
- ✅ Cicli completati incrementano progress livello
- ✅ Salita livello avviene al completamento soglia
- ✅ Resa frutti modifica correttamente da Lvl 3+
- ✅ Solo Lvl 5 può essere spostata in slot passivi

**Test Scenarios:**
1. Completare 1 ciclo → Lvl 1→2
2. Completare 3 cicli totali → Lvl 2→3
3. Verificare resa frutti Lvl 3 vs Lvl 1

---

### 🎯 TASK 1.3: Sistema Stadi Completi (6 Stadi)
**Codice Task:** `BLK-02.03`  
**Dipendenze:** Task 1.1 (pH necessario per transizioni)  
**Rischio:** ALTO (modifica sistema esistente)

#### Implementazione

**1.3.1 - Estendere PlantStage Enum**
```csharp
// Modificare PlantStage.cs
public enum PlantStage 
{ 
    Empty = 0,
    Seed = 1,
    Sprout = 2,
    Growth = 3,          // NUOVO
    Flowering = 4,      // NUOVO
    HarvestReady = 5,   // NUOVO
    Resting = 6         // NUOVO
}
```

**1.3.2 - Creare StageTransitionSystem**
```
Assets/_Project/Scripts/Dome/PotSystem/Growth/
├── StageTransitionSystem.cs       # Gestione transizioni stadi
└── StageTransitionConfig.cs        # Config requisiti transizioni
```

**Requisiti Transizioni:**
- Seed → Sprout: idratazione 40-60%, pH compatibile, 1-2 giorni
- Sprout → Growth: idratazione 35-55%, Blue LED opzionale, 2-3 giorni
- Growth → Flowering: idratazione 40-50%, Blue LED usato, 2 giorni consecutivi
- Flowering → HarvestReady: idratazione 40-50%, Red LED usato, 2 giorni
- HarvestReady → Resting: dopo Harvest
- Resting → Flowering: con Fertilizzante

**1.3.3 - Modificare DayCycleController**
- Sostituire logica 3 stadi con logica 6 stadi
- Aggiungere check requisiti per ogni transizione
- Gestire HarvestReady con frutti multi-giorno

**1.3.4 - Sistema Frutti Multi-Giorno**
- All'ingresso HarvestReady: +1 frutto
- Ogni giorno non raccolto: +1 frutto (max 3)
- Dopo 3 giorni: decay frutti (-1 livello/giorno)

**Criteri di Accettazione:**
- ✅ Tutti i 6 stadi funzionano correttamente
- ✅ Transizioni rispettano requisiti (idratazione, LED, pH)
- ✅ Frutti compaiono correttamente in HarvestReady
- ✅ Decay frutti funziona dopo 3 giorni
- ✅ Resting richiede Fertilizzante per riattivare

**Test Scenarios:**
1. Seed → Sprout con idratazione corretta
2. Growth → Flowering con Blue LED
3. Flowering → HarvestReady con Red LED
4. Frutti accumulano correttamente (1→2→3)
5. Decay frutti dopo 3 giorni

---

### 🎯 TASK 1.4: Sistema Slot Passivi
**Codice Task:** `BLK-02.04`  
**Dipendenze:** Task 1.2 (livelli necessari), Task 1.3 (stadi necessari)  
**Rischio:** MEDIO

#### Implementazione

**1.4.1 - Creare PassiveSlotSystem**
```
Assets/_Project/Scripts/Dome/
├── PassiveSlotSystem.cs           # Gestione 3 slot passivi
├── PassiveSlot.cs                  # Componente slot passivo
└── PassiveSlotConfig.cs            # Configurazione slot
```

**1.4.2 - Estendere PotStateModel**
```csharp
public bool IsInPassiveSlot { get; set; } = false;
public string PassiveSlotId { get; set; } = "";
```

**1.4.3 - Implementare Azione Sposta → Slot Passivo**
- Verificare livello pianta = 5
- Verificare slot passivo disponibile
- Spostare pianta da vaso attivo a slot passivo
- Applicare bonus passivi

**1.4.4 - Sistema Bonus Passivi**
- Ogni pianta ha bonus passivo unico (definito in PlantData)
- Bonus applicati quando pianta in slot passivo
- Cap pH drift al 20% per slot passivi

**1.4.5 - UI Slot Passivi**
```
Assets/_Project/Scripts/UI/VaultMap/Dome/
└── UIPassiveSlots.cs               # Visualizzazione slot passivi
```

**Criteri di Accettazione:**
- ✅ Solo piante Lvl 5 possono essere spostate
- ✅ Massimo 3 slot passivi disponibili
- ✅ Bonus passivi applicati correttamente
- ✅ pH drift cappato al 20% per slot passivi
- ✅ UI mostra slot passivi e piante contenute

**Test Scenarios:**
1. Tentare spostare Lvl 3 → deve fallire
2. Spostare Lvl 5 → deve funzionare
3. Verificare bonus passivo applicato
4. Verificare pH drift ridotto al 20%

---

## 📦 FASE 2: PLANT SYSTEMS
**Durata Stimata:** 4-5 settimane  
**Priorità:** ALTA  
**Dipendenze:** FASE 1 completata

### Obiettivo
Implementare il sistema botanico completo: catalog piante, mutazioni, ibridi.

---

### 🎯 TASK 2.1: Sistema Catalog Piante Base
**Codice Task:** `BLK-03.01`  
**Dipendenze:** FASE 1 completata  
**Rischio:** MEDIO

#### Implementazione

**2.1.1 - Creare PlantData ScriptableObject**
```
Assets/_Project/Scripts/Core/ItemsSystem/
├── PlantData.cs                    # ScriptableObject dati pianta
└── PlantFamily.cs                  # Enum: Standard, Pure, Evil
```

**Struttura PlantData:**
- Codice (PLT-STD-001, PLT-PURE-001, ecc.)
- Famiglia (Standard/Pure/Evil)
- Rarità (Comune/Non comune/Rara/Epica/Leggendaria)
- Fazione preferita
- pH Drift (per giorno)
- Range pH ottimale
- Bonus Attivi/Passivi
- Outputs (Prodotto, Frutto, Seme)
- Effetto Commestibile

**2.1.2 - Creare Prime 9 Piante**
Implementare almeno:
- **Standard:** STD-001 (Ferric Fern), STD-002 (Saltbloom Succulent), STD-003 (Blue Sedge)
- **Pure:** PURE-001 (Arctic Hask), PURE-002 (Night-Bloom Iris), PURE-003 (Dawn Orchid)
- **Evil:** EVIL-001 (Glasscap Fungus), EVIL-002 (Red Tangle Vine), EVIL-003 (Fleshblossom)

**2.1.3 - PlantDatabase System**
```
Assets/_Project/Scripts/Core/ItemsSystem/
└── PlantDatabase.cs                # Registry tutte le piante
```

**2.1.4 - Integrare con PotSystem**
- PotStateModel deve referenziare PlantData
- Crescita deve rispettare requisiti pianta specifica

**Criteri di Accettazione:**
- ✅ 9 piante base implementate con dati GDD
- ✅ PlantDatabase registra tutte le piante
- ✅ PotSystem usa PlantData per requisiti crescita
- ✅ pH drift applicato correttamente per famiglia

**Test Scenarios:**
1. Piantare STD-001 → verificare requisiti idratazione
2. Piantare PURE-001 → verificare pH drift +2
3. Piantare EVIL-001 → verificare pH drift -2

---

### 🎯 TASK 2.2: Sistema Mutazioni
**Codice Task:** `BLK-03.02`  
**Dipendenze:** Task 2.1 (piante necessarie), Task 1.1 (pH necessario)  
**Rischio:** ALTO

#### Implementazione

**2.2.1 - Creare MutationSystem**
```
Assets/_Project/Scripts/Dome/PotSystem/
├── MutationSystem.cs               # Gestione mutazioni
├── MutationData.cs                  # ScriptableObject mutazione
└── MutationType.cs                  # Enum: Armonica, Corrotta, Adattiva
```

**2.2.2 - MutationScore Calculator**
- pH mismatch: Neutral=0, Stable=+10, Ultra=+20
- Idratazione fuori banda: +5/giorno (cap +20)
- LED abuse: +10 (+5 extra se ripetuto)
- Muffa: Mild +15, Severe +30
- Concime/Pruning: Sacro +10 Armoniche, Proibito +10 Corrotte

**2.2.3 - Timing Mutazioni**
- Dawn Check (dopo EndDay)
- Event Check (eventi forti)
- Lab Check (precisione minigiochi)

**2.2.4 - Implementare Prime Mutazioni**
- MUT-101 (Respiro di Luce) - Armonica
- MUT-301 (Mildew Bloom) - Corrotta
- MUT-401 (Spiral Growth) - Adattiva

**2.2.5 - Effetti Mutazioni**
- Applicare modificatori crescita/resa/stabilità
- Durata mutazioni (temporanee vs permanenti)

**Criteri di Accettazione:**
- ✅ MutationScore calcolato correttamente
- ✅ Mutazioni si innescano a Dawn Check
- ✅ Effetti mutazioni applicati correttamente
- ✅ Solo Lvl 1-3 possono mutare
- ✅ UI mostra mutazioni attive

**Test Scenarios:**
1. pH Ultra Acido → verificare mutazione corrotta
2. LED abuse → verificare aumento MutationScore
3. Muffa → verificare mutazione corrotta

---

### 🎯 TASK 2.3: Sistema Ibridi
**Codice Task:** `BLK-03.03`  
**Dipendenze:** Task 2.1 (piante necessarie), Task 1.3 (stadi necessari)  
**Rischio:** ALTO

#### Implementazione

**2.3.1 - Creare HybridSystem**
```
Assets/_Project/Scripts/Lab/Cloning/
├── HybridSystem.cs                 # Gestione creazione ibridi
├── HybridData.cs                    # ScriptableObject ibrido
└── HybridCompatibility.cs           # Verifica compatibilità genitori
```

**2.3.2 - DNA Fusion Minigioco**
```
Assets/_Project/Scripts/UI/Lab/Cloning/
└── DNAFusionMinigame.cs            # Minigioco sequenza forme geometriche
```

**2.3.3 - Trait Selection System**
- Analizzare tratti genitori
- Permettere selezione fino a 3 tratti
- Generare HYB-xxx code

**2.3.4 - Implementare Prime Ibridi**
- HYB-201 (Ferric Tangle) - Standard × Evil
- HYB-203 (Aurablade Reed) - Standard × Pure

**2.3.5 - Integrare con PlantDatabase**
- Registrare ibridi come nuove piante
- Permettere piantumazione ibridi

**Criteri di Accettazione:**
- ✅ DNA Fusion minigioco funziona
- ✅ Selezione tratti funziona (max 3)
- ✅ Ibridi generati correttamente con HYB-xxx code
- ✅ Ibridi piantabili come piante normali
- ✅ Ibridi hanno drift pH combinato genitori

**Test Scenarios:**
1. Creare HYB-201 da STD-001 × EVIL-002
2. Verificare drift pH combinato
3. Piantare ibrido e verificare crescita

---

## 📦 FASE 3: ECONOMIC & SURVIVAL SYSTEMS
**Durata Stimata:** 2-3 settimane  
**Priorità:** ALTA  
**Dipendenze:** FASE 1 completata

### Obiettivo
Implementare Food Room e completare Seed Storage.

---

### 🎯 TASK 3.1: Sistema Food Room
**Codice Task:** `BLK-04.01`  
**Dipendenze:** Task 1.1 (pH opzionale per effetti)  
**Rischio:** MEDIO

#### Implementazione

**3.1.1 - Creare FoodRoomSystem**
```
Assets/_Project/Scripts/Systems/FoodRoom/
├── FoodRoomSystem.cs               # Gestione produzione cibo
├── FoodRoomConfig.cs                # Configurazione Food Room
└── FoodProductionType.cs           # Enum: Vegetale, Fungo, Carne, Acqua
```

**3.1.2 - Sistema Slot Produzione**
- 1 slot base, espandibile a 3
- Slot Idrico separato per acqua potabile
- Timer produzione per tipo

**3.1.3 - Implementare Produzione**
- Vegetali: 1 giorno → 3 unità → +1 Azione
- Funghi: 2 giorni → 2 unità → +2 Azioni
- Carne: 3 giorni → 1 unità → +3 Azioni + 3 RES-PROT-001
- Acqua: 1 giorno → WAT-RAW → WAT-POT

**3.1.4 - Costi CRY Giornalieri**
- Vegetali: 1 CRY/giorno
- Funghi: 1 CRY/giorno
- Carne: 2 CRY/giorno
- Acqua: 0 CRY/giorno

**3.1.5 - UI Food Room**
```
Assets/_Project/Scripts/UI/VaultMap/FoodRoom/
└── UIFoodRoom.cs                   # HUD Food Room
```

**Criteri di Accettazione:**
- ✅ Produzione funziona per tutti i tipi
- ✅ Timer corretti per ogni tipo
- ✅ Costi CRY applicati correttamente
- ✅ Bonus Azioni applicati al consumo
- ✅ Residui proteici generati da carne

**Test Scenarios:**
1. Avviare produzione vegetali → verificare output dopo 1 giorno
2. Avviare produzione carne → verificare RES-PROT-001 generati
3. Consumare cibo → verificare bonus Azioni

---

### 🎯 TASK 3.2: Completare Seed Storage
**Codice Task:** `BLK-04.02`  
**Dipendenze:** Nessuna (sistema base già presente)  
**Rischio:** BASSO

#### Implementazione

**3.2.1 - Aggiungere Costi CRY**
- 2 CRY/giorno per slot occupato
- Calcolo automatico EndDay

**3.2.2 - Sistema Espansione**
- 4 slot base → espandibile a 20
- Costo espansione crescente
- UI per espansione

**3.2.3 - Verificare Blocco Deterioramento**
- Items in storage non deteriorano
- Deterioramento riprende quando ritirati

**Criteri di Accettazione:**
- ✅ Costi CRY applicati correttamente
- ✅ Espansione funziona
- ✅ Deterioramento bloccato in storage
- ✅ UI mostra costi e slot disponibili

---

## 📦 FASE 4: SOCIAL SYSTEMS
**Durata Stimata:** 3-4 settimane  
**Priorità:** CRITICA  
**Dipendenze:** FASE 1 e FASE 2 completate

### Obiettivo
Implementare sistema Fazioni/Reputazione e completare Visitor Room.

---

### 🎯 TASK 4.1: Sistema Fazioni/Reputazione
**Codice Task:** `BLK-05.01`  
**Dipendenze:** Task 2.1 (piante necessarie per drift reputazione)  
**Rischio:** MEDIO

#### Implementazione

**4.1.1 - Creare FactionSystem**
```
Assets/_Project/Scripts/Core/Factions/
├── FactionSystem.cs                # Gestione reputazione fazioni
├── FactionData.cs                   # ScriptableObject fazione
└── FactionType.cs                   # Enum: Custodi, CultoMuffa, Mercanti, Ipnotici, Militari, Religiosi
```

**4.1.2 - Sistema Reputazione**
- Range: -100 a +100 per fazione
- Rapporti bilaterali antagonisti (Custodi ↔ Culto)
- Drift naturale basato su piante coltivate

**4.1.3 - Implementare 6 Fazioni**
- Custodi (Pure favoriscono)
- Culto della Muffa (Evil favoriscono)
- Mercanti Ombra
- Ipnotici
- Militari
- Setta Religiosa

**4.1.4 - Effetti Reputazione**
- Modificatori prezzo vendita
- Missioni disponibili
- Accesso contenuti

**4.1.5 - UI Reputazione**
```
Assets/_Project/Scripts/UI/VaultMap/Factions/
└── UIFactionReputation.cs          # Visualizzazione reputazioni
```

**Criteri di Accettazione:**
- ✅ 6 fazioni implementate
- ✅ Reputazione range -100/+100
- ✅ Drift naturale funziona (Pure → Custodi, Evil → Culto)
- ✅ Rapporti antagonisti funzionano
- ✅ Effetti reputazione applicati

**Test Scenarios:**
1. Piantare Pure → verificare reputazione Custodi ↑
2. Piantare Evil → verificare reputazione Culto ↑
3. Vendere a Custodi con alta reputazione → verificare prezzo modificato

---

### 🎯 TASK 4.2: Completare Visitor Room
**Codice Task:** `BLK-05.02`  
**Dipendenze:** Task 4.1 (reputazione necessaria)  
**Rischio:** MEDIO

#### Implementazione

**4.2.1 - Sistema CALL Visitatore**
- Terminale Fazioni nella Visitor Room
- Mostra fazioni disponibili (pesato da reputazione)
- Costo: 0 Azioni aprire, 1 Azione confermare

**4.2.2 - Sistema Baratto**
- Due pannelli: Fazione vs Player
- Sistema valori in CRY
- Negoziazione basata su reputazione

**4.2.3 - Inventario Randomizzato Fazione**
- Oggetti disponibili basati su fazione
- Prezzi modificati da reputazione

**Criteri di Accettazione:**
- ✅ CALL Visitatore funziona
- ✅ Baratto funziona con negoziazione
- ✅ Prezzi modificati da reputazione
- ✅ Inventario fazione randomizzato

---

## 📦 FASE 5: NARRATIVE SYSTEMS
**Durata Stimata:** 4-5 settimane  
**Priorità:** MEDIA  
**Dipendenze:** FASE 1-4 completate

### Obiettivo
Implementare sistema missioni completo e Atti narrativi.

---

### 🎯 TASK 5.1: Sistema Missioni Completo
**Codice Task:** `BLK-06.01`  
**Dipendenze:** Task 4.1 (fazioni necessarie)  
**Rischio:** MEDIO

#### Implementazione

**5.1.1 - Struttura MQ + 5 SQ**
- Main Quest preparata da 5 Side Quest
- Side Quest influenzano MQ
- Sistema tracking progress

**5.1.2 - Implementare Missioni GDD**
- MQ-BEN-01 → MQ-BEN-05 (Pure)
- MQ-MAL-01 → MQ-MAL-05 (Evil)

**5.1.3 - Effetti Collaterali Missioni**
- Modifiche reputazione
- Sblocchi contenuti
- Ricompense

**Criteri di Accettazione:**
- ✅ Struttura MQ+SQ funziona
- ✅ Missioni GDD implementate
- ✅ Effetti collaterali applicati

---

### 🎯 TASK 5.2: Sistema Atti Narrativi
**Codice Task:** `BLK-06.02`  
**Dipendenze:** Task 5.1 (missioni necessarie)  
**Rischio:** ALTO

#### Implementazione

**5.2.1 - Sistema Atti**
- Tracking Atto corrente
- Sblocchi progressivi
- Transizioni tra Atti

**5.2.2 - Implementare Atto I**
- "La Fame Bussa alla Porta"
- MQ-01A → MQ-01F
- Tutorial progressivo

**Criteri di Accettazione:**
- ✅ Sistema Atti funziona
- ✅ Atto I implementato completamente
- ✅ Transizioni funzionano

---

## 📦 FASE 6: POLISH & ADVANCED
**Durata Stimata:** 3-4 settimane  
**Priorità:** BASSA  
**Dipendenze:** FASE 1-5 completate

### Obiettivo
Sistemi avanzati e contenuti late-game.

---

### 🎯 TASK 6.1: Sistema Muffe/Infestazioni
**Codice Task:** `BLK-07.01`  
**Dipendenze:** Task 1.1 (pH necessario)  
**Rischio:** MEDIO

#### Implementazione

**6.1.1 - MoldSystem**
- Tracking Mold Risk per pianta
- Condizione "Infestata"
- Spray Antifungino per cura

**6.1.2 - Calcolo Rischio**
- Overwatering aumenta rischio
- pH acido aumenta rischio
- Piante Evil aumentano rischio

**Criteri di Accettazione:**
- ✅ Mold Risk calcolato correttamente
- ✅ Condizione Infestata applicata
- ✅ Spray Antifungino cura infestazione

---

### 🎯 TASK 6.2: Sistema Dome Avanzata
**Codice Task:** `BLK-07.02`  
**Dipendenze:** FASE 5 completata  
**Rischio:** BASSO

#### Implementazione

**6.2.1 - Dome Avanzata Environment**
- 3 vasi vetrati attivi
- Piante Ipnotiche
- Nessun pH, sempre Lvl 5

**6.2.2 - Cure Ipnotiche**
- Dreamroot Vein (ritmo)
- Whisper Bloom (ascolto)
- Mirror Ivy (riflesso)

**Criteri di Accettazione:**
- ✅ Dome Avanzata funziona
- ✅ Cure ipnotiche implementate

---

## 📊 CRONOPROGRAMMA SUGGERITO

```
Settimana 1-4:   FASE 1 (Core Dome Systems)
Settimana 5-9:   FASE 2 (Plant Systems)
Settimana 10-12: FASE 3 (Economic & Survival)
Settimana 13-16: FASE 4 (Social Systems)
Settimana 17-21: FASE 5 (Narrative Systems)
Settimana 22-25: FASE 6 (Polish & Advanced)
```

**Totale Stimato:** 25 settimane (~6 mesi)

---

## ✅ CHECKLIST QUALITÀ PER OGNI TASK

Prima di considerare un task completato:

- [ ] Codice implementato e funzionante
- [ ] Test manuali completati con scenari GDD
- [ ] Integrazione con sistemi esistenti verificata
- [ ] Documentazione README creata
- [ ] Config ScriptableObject creato
- [ ] UI implementata (se necessaria)
- [ ] Eventi emessi correttamente
- [ ] Nessun breaking change su sistemi esistenti
- [ ] Code review completata
- [ ] Commit con messaggio descrittivo

---

## 🚨 RISCHI E MITIGAZIONI

### Rischio: Modifiche Sistema Crescita Esistente
**Mitigazione:** Creare branch separato, testare estensivamente prima di merge

### Rischio: Complessità Sistema Mutazioni
**Mitigazione:** Implementare incrementale, iniziare con 3 mutazioni base

### Rischio: Dipendenze tra Sistemi
**Mitigazione:** Implementare in ordine FASE, verificare dipendenze prima di iniziare

### Rischio: Performance con Molte Piante
**Mitigazione:** Ottimizzare calcoli pH drift, usare pooling per eventi

---

## 📝 NOTE FINALI

- Ogni fase deve essere **completamente testabile** prima di passare alla successiva
- Mantenere **backward compatibility** con sistemi esistenti
- Usare **feature flags** per sistemi in sviluppo
- **Documentare** ogni sistema con esempi d'uso
- **Code review** obbligatoria prima di merge

---

**FINE PIANO IMPLEMENTAZIONE**

