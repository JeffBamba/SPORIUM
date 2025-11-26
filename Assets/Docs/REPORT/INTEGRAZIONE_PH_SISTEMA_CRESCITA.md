# Integrazione pH Sistema Crescita - Implementazione Completata

**Data:** 2025-11-26  
**Versione:** 1.1  
**Stato:** ✅ COMPLETATO + SELEZIONE SEMI INTEGRATA

---

## 📋 Panoramica

Implementazione completa dell'integrazione tra il sistema pH globale e il sistema di crescita delle piante. Ora le piante influenzano automaticamente il pH della Dome in base alla loro famiglia (PURE/EVIL/STANDARD).

---

## ✅ Componenti Implementati

### 1. **PlantFamily Enum**
**File:** `Assets/_Project/Scripts/Dome/PotSystem/Growth/PlantFamily.cs`

Enum che definisce le famiglie di piante:
- `Standard` (0): Piante neutre, drift pH minimo o nullo
- `Pure` (1): Piante pure, drift pH positivo (+2/giorno, range +2 a +3)
- `Evil` (2): Piante evil, drift pH negativo (-2/giorno, range -1 a -3)

### 2. **PlantData ScriptableObject**
**File:** `Assets/_Project/Scripts/Dome/PotSystem/Growth/PlantData.cs`

ScriptableObject che contiene i dati specifici di una pianta:
- **Identificazione:**
  - `PlantCode`: Codice univoco (es. PLT-STD-001)
  - `SeedItemConfig`: Riferimento all'ItemConfig del seme
  
- **Famiglia e Caratteristiche:**
  - `Family`: PlantFamily (Standard/Pure/Evil)
  - `Rarity`: PlantRarity (Common/Uncommon/Rare/Epic/Legendary)
  
- **pH System:**
  - `DailyPhDrift`: Drift pH giornaliero base
  - `OptimalPhMin/Max`: Range pH ottimale per la pianta
  
- **Metodi:**
  - `IsPhInOptimalRange(float currentPh)`: Verifica se pH è ottimale
  - `GetDailyPhDrift()`: Restituisce drift pH giornaliero

### 3. **PlantDatabase Singleton**
**File:** `Assets/_Project/Scripts/Dome/PotSystem/Growth/PlantDatabase.cs`

Database centrale per tutte le piante:
- **Funzionalità:**
  - Mappa `ItemConfig.TypeId` → `PlantData` per lookup veloce
  - Mappa `PlantCode` → `PlantData` per lookup diretto
  - Caricamento automatico da `Resources/Plants/`
  - Registrazione manuale runtime
  
- **Metodi principali:**
  - `GetPlantDataBySeedTypeId(string seedTypeId)`: Trova PlantData dal TypeId del seme
  - `GetPlantDataByCode(string plantCode)`: Trova PlantData dal PlantCode
  - `GetPlantsByFamily(PlantFamily family)`: Ottiene tutte le piante di una famiglia
  - `RegisterPlantData(PlantData plantData)`: Registra manualmente un PlantData

### 4. **PotStateModel Modificato**
**File:** `Assets/_Project/Scripts/Dome/PotStateModel.cs`

Aggiunto campo `PlantCode` per tracciare il tipo di pianta:
- Campo serializzabile `PlantCode` (string)
- Metodo `GetPlantData()`: Ottiene PlantData dal PlantDatabase usando PlantCode
- Metodo `PlantSeed()` modificato per accettare `plantCode` opzionale

### 5. **PotActions Modificato**
**File:** `Assets/_Project/Scripts/Dome/PotActions.cs`

Modificato `DoPlant()` per assegnare PlantData:
- Cerca PlantData dal PlantDatabase usando il TypeId del seme
- Assegna `PlantCode` al `PotStateModel` quando si pianta un seme
- Logging dettagliato per debug

### 6. **DayCycleController Modificato**
**File:** `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

Aggiunto calcolo e registrazione pH drift:
- Metodo `CalculateAndRegisterPhDrift()`:
  - Itera tutti i vasi registrati con piante
  - Calcola drift pH totale sommando drift di ogni pianta
  - Registra drift totale nel PhSystem usando `RegisterPlantDrift()`
- Chiamato automaticamente in `HandleDayChanged()` dopo `ResolveGrowthForAllPots()`

---

## 🔄 Flusso di Integrazione

### 1. Setup PlantData
```
1. Creare PlantData ScriptableObject
2. Assegnare SeedItemConfig (riferimento al seme)
3. Impostare Family (Pure/Evil/Standard)
4. Impostare DailyPhDrift (+2 per Pure, -2 per Evil, 0 per Standard)
5. Salvare in Resources/Plants/ o assegnare manualmente a PlantDatabase
```

### 2. Piantare Seme (CON SELEZIONE SEMI)
```
1. Player clicca "Plant" su vaso vuoto
2. Si apre UISeedSelector con tutti i semi disponibili dall'inventario
3. Player seleziona seme desiderato (vede famiglia, quantità, drift pH)
4. UISeedSelector emette OnSeedSelected(seedTypeId)
5. PotActions.DoPlant(seedTypeId) viene chiamato con seme specifico
6. PotActions cerca PlantData dal PlantDatabase usando seedTypeId
7. PotActions assegna PlantCode al PotStateModel
8. Vaso registrato in DayCycleController
```

### 3. Fine Giorno (pH Drift)
```
1. GameManager chiama EndDay()
2. DayCycleController.HandleDayChanged(day) viene chiamato
3. ResolveGrowthForAllPots(day) calcola crescita
4. CalculateAndRegisterPhDrift() viene chiamato:
   - Per ogni vaso con pianta:
     - Ottiene PlantData da PotStateModel
     - Somma DailyPhDrift alla somma totale
   - Registra drift totale in PhSystem.RegisterPlantDrift()
5. PhSystem applica drift al pH globale
```

---

## 🧪 Come Testare

### Prerequisiti
1. ✅ PhSystem deve essere registrato nel ServiceContainer
   - Se `PhSystemDebugConsole` è presente nella scena, PhSystem viene registrato automaticamente
   - Altrimenti aggiungere registrazione manuale in `GamePlayInstaller`

2. ✅ PlantDatabase deve essere presente nella scena
   - Creare GameObject "PlantDatabase" con componente `PlantDatabase`
   - Oppure assegnare PlantData manualmente nella lista `allPlantData`

3. ✅ Creare almeno un PlantData ScriptableObject
   - Creare nuovo ScriptableObject: `Create > Sporae > PlantData`
   - Assegnare SeedItemConfig (es. seed-001)
   - Impostare Family (Pure/Evil/Standard)
   - Impostare DailyPhDrift (+2 per Pure, -2 per Evil, 0 per Standard)
   - Salvare in `Resources/Plants/` o assegnare a PlantDatabase

### Test Scenario 1: Pianta Pure
```
1. Creare PlantData per seme Pure:
   - SeedItemConfig: seed-001
   - Family: Pure
   - DailyPhDrift: +2.0
   
2. In Play Mode:
   - Piantare seme seed-001
   - Verificare che pH HUD mostri pH iniziale (es. 0.0)
   - Eseguire End Day
   - Verificare che pH aumenti di +2.0
   - Verificare tooltip pH mostra "Piante: +2.00"
```

### Test Scenario 2: Pianta Evil
```
1. Creare PlantData per seme Evil:
   - SeedItemConfig: seed-002
   - Family: Evil
   - DailyPhDrift: -2.0
   
2. In Play Mode:
   - Piantare seme seed-002
   - Verificare pH iniziale
   - Eseguire End Day
   - Verificare che pH diminuisca di -2.0
   - Verificare tooltip pH mostra "Piante: -2.00"
```

### Test Scenario 3: Piante Multiple
```
1. Creare PlantData per Pure e Evil
2. In Play Mode:
   - Piantare 2 piante Pure (+2 ciascuna = +4 totale)
   - Piantare 1 pianta Evil (-2 totale)
   - Eseguire End Day
   - Verificare che pH aumenti di +2 (+4 - 2 = +2)
```

### Debug Logs
Il sistema produce log dettagliati:
- `[PotActions] PlantData trovato: PLT-PURE-001 (Pure), drift pH: 2/giorno`
- `[DayCycleController] POT-001: PLT-PURE-001 (Pure) → drift pH: 2.00/giorno`
- `[DayCycleController] pH Drift totale da 3 piante: 2.00 → pH attuale: 2.00`

---

## ⚠️ Note Importanti

### Compatibilità Retroattiva
- ✅ Sistema retrocompatibile: se PlantData non trovato, pianta funziona normalmente senza drift pH
- ✅ Warning log quando PlantData non trovato per debugging

### Performance
- ✅ Lookup PlantData è O(1) tramite Dictionary
- ✅ Calcolo drift pH avviene solo una volta per giorno (End Day)
- ✅ Nessun overhead significativo

### Limitazioni Attuali
- ⚠️ Solo drift pH base implementato (variazioni casuali ±1 non ancora implementate)
- ⚠️ Effetti pH su crescita/produzione non ancora implementati
- ⚠️ Sistema livelli (1-5) non ancora implementato
- ⚠️ Sistema mutazioni non ancora implementato

### Nuove Funzionalità (BLK-02.02)
- ✅ Sistema selezione semi da inventario implementato
- ✅ UISeedSelector mostra famiglia, quantità, drift pH per ogni seme
- ✅ Creazione automatica UI se mancante (CreateUI())
- ✅ Aggiornamento automatico quantità quando inventario cambia
- ✅ PlantData assets creati: PLT-STD-001, PLT-PURE-001, PLT-EVIL-001
- ✅ seed-003 aggiunto all'inventario iniziale

---

## 📝 Prossimi Passi

1. **Creare PlantData per tutte le piante del GDD**
   - Implementare almeno 9 piante base (3 Standard, 3 Pure, 3 Evil)
   - Creare ScriptableObject per ogni pianta

2. **Estendere stadi crescita**
   - Aggiungere Growth, Flowering, HarvestReady, Resting
   - Implementare requisiti pH per avanzamento stadi

3. **Implementare effetti pH su crescita**
   - Ultra Acido: Pure collassano, Evil +50% resa
   - Ultra Basico: Evil collassano, Pure iper-produttive ma sterili

4. **Integrare azioni player con pH**
   - Overwatering → -5 pH
   - Blue LED → +5 pH
   - Red LED → -5 pH

---

## 🎯 Risultato

✅ **Sistema pH completamente integrato con sistema crescita**

Ora quando si pianta un seme:
1. Il sistema cerca automaticamente PlantData dal PlantDatabase
2. Assegna PlantCode al vaso
3. Al fine giornata, calcola drift pH totale da tutte le piante
4. Registra drift nel PhSystem
5. pH globale viene aggiornato automaticamente

**Il sistema è funzionante e pronto per test!**

---

## 📚 File Modificati/Creati

### Nuovi File
- `Assets/_Project/Scripts/Dome/PotSystem/Growth/PlantFamily.cs`
- `Assets/_Project/Scripts/Dome/PotSystem/Growth/PlantData.cs`
- `Assets/_Project/Scripts/Dome/PotSystem/Growth/PlantDatabase.cs`

### File Modificati
- `Assets/_Project/Scripts/Dome/PotStateModel.cs` (+ PlantCode, GetPlantData())
- `Assets/_Project/Scripts/Dome/PotActions.cs` (+ ricerca PlantData in DoPlant(), + parametro seedTypeId)
- `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs` (+ CalculateAndRegisterPhDrift())
- `Assets/_Project/Scripts/UI/VaultMap/PotHUDWidget.cs` (+ integrazione UISeedSelector)
- `Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs` (+ integrazione UISeedSelector)
- `Assets/_Project/Scripts/Core/GameManager.cs` (+ seed-003 inventario iniziale)
- `Assets/_Project/Scripts/Core/ItemsSystem/Items.cs` (+ Seed003 costante)

### Nuovi File (BLK-02.02)
- `Assets/_Project/Scripts/UI/VaultMap/UISeedSelector.cs` (UI selezione semi)
- `Assets/_Project/Scripts/UI/VaultMap/UISeedItem.cs` (Componente UI singolo seme)
- `Assets/_Project/Scripts/Debug/UISeedSelectorAutoSetup.cs` (Auto-setup runtime)
- `Assets/Resources/Plants/PLT-STD-001.asset` (PlantData Standard)
- `Assets/Resources/Plants/PLT-PURE-001.asset` (PlantData Pure)
- `Assets/Resources/Plants/PLT-EVIL-001.asset` (PlantData Evil)
- `Assets/Resources/Items/seed-003.asset` (ItemConfig seme Evil)

---

**Fine Documento**

