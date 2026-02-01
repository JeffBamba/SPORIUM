# 🍄 ANALISI FOOD ROOM - GDD vs Repository

**Data Analisi:** 2025-01-20  
**GDD di Riferimento:** ENV-010 — Food Room + Loop Food Room  
**Versione Repository:** Build_Beta (10/2025)

---

## 📋 SOMMARIO ESECUTIVO

| Componente | Stato GDD | Stato Repository | Gap |
|------------|-----------|-----------------|-----|
| **FoodRoomSystem** | ✅ Richiesto | ❌ **NON IMPLEMENTATO** | 🔴 CRITICO |
| **Produzione Cibo** | ✅ Richiesto | ❌ **NON IMPLEMENTATO** | 🔴 CRITICO |
| **Slot Produzione** | ✅ Richiesto | ❌ **NON IMPLEMENTATO** | 🔴 CRITICO |
| **Slot Idrico** | ✅ Richiesto | ❌ **NON IMPLEMENTATO** | 🔴 CRITICO |
| **Item FOOD-xxx** | ✅ Richiesto | ❌ **NON DEFINITI** | 🔴 CRITICO |
| **Sistema Idratazione Player** | ✅ Richiesto | ❌ **NON IMPLEMENTATO** | 🔴 CRITICO |
| **Ambiente Kitchen** | ✅ Richiesto | ⚠️ **PARZIALE** (solo struttura) | 🟡 PARZIALE |
| **UI/Navigazione** | ✅ Richiesto | ⚠️ **PARZIALE** (solo navigazione) | 🟡 PARZIALE |

**Priorità:** 🔴 **ALTA** (blocca survival loop e gestione risorse giocatore)

---

## 📖 GDD - REQUISITI COMPLETI

### **ENV-010 — Food Room**

#### **1. Macchinario FOOD-SYNTH-001**
- **Funzione:** Laboratorio alimentare sintetico
- **Tipologie Produzione:**
  - Vegetali sintetici
  - Funghi sintetici
  - Carne sintetica
- **Punto di Interazione:** Macchinario centrale

#### **2. Sistema Slot Produzione**
- **Slot Base:** 1 disponibile nel tutorial
- **Espansione:** Fino a 3 slot totali (tramite moduli o missioni)
- **Slot Idrico:** Separato per produzione acqua potabile

#### **3. Produzione Cibo Sintetico**

| Tipo | Timer | Output | Valore | Bonus Azioni | Costo CRY/Giorno |
|------|-------|--------|--------|--------------|------------------|
| **Vegetali** | 1 giorno | 3 unità | Basso | +1 Azione | 1 CRY |
| **Funghi** | 2 giorni | 2 unità | Medio | +2 Azioni | 1 CRY |
| **Carne** | 3 giorni | 1 unità | Alto | +3 Azioni | 2 CRY |

#### **4. Residui Proteici (Carne)**
- **Produzione:** 3 residui organici (ORG-RES-001) durante i 3 giorni
  - Giorno 2 → 1 residuo
  - Giorno 3 → 1 residuo
  - Giorno 4 → 1 residuo (insieme alla carne)
- **Processamento:** 3 residui → 1 Cellula Staminale Carne (Laboratorio Botanico)

#### **5. Slot Idrico — Acqua Potabile**
- **Input:** WAT-RAW xN dall'inventario
- **Output:** Acqua Potabile xN (1:1 ratio)
- **Timer:** 1 giorno
- **Costo:** 0 CRY giornalieri
- **Azione Avvio:** Non consuma Action
- **Harvest:** Gratuito al mattino

#### **6. Integrazione Cellule Staminali**
- **Fonti:** Materiale organico, piante morte, frutti processati, residui carne, acquisti/scambi
- **Uso:** Opzionale su avvio coltura
- **Effetti:**
  - Cellule normali → produzione standard
  - Cellule speciali → effetti casuali (Indigestione -1 AP / Super Energia +1 AP)
  - Cellule frutti Ipnotici → evento narrativo fazione Ipnotica

#### **7. HUD Macchinario**
- Schermata con slot attivi
- Menu scelta tipologia (Vegetale/Fungo/Carne)
- Campo opzionale cellula staminale
- Info slot: tipo coltura, giorni restanti, costo CRY, output previsto
- Nota "EFFETTI casuali possibili" se cellula inserita
- Stato slot idrico con input/output previsto

#### **8. Operazioni**
- **Avvio Coltura:** Costa 1 Action, scelta tipologia, opzionale cellula
- **Avvio Produzione Acqua:** Non consuma Action, input WAT-RAW
- **Harvest:** Gratuito al mattino, cibo/acqua → inventario
- **Costo Giornaliero:** CRY per slot occupati a fine giornata

#### **9. Sistema Idratazione Player**
- **Stati (0-100%):**
  - **Dehydrated (0-25):** -2 Azioni/giorno, perdita salute, deriva psicologica
  - **Low Hydration (26-50):** -1 Azione/giorno
  - **Normal (51-75):** Nessun bonus/malus
  - **Well-Hydrated (76-100):** +2 Azioni/giorno, mente stabile
- **Consumo:**
  - Passivo giornaliero (traspirazione)
  - Attivo per azioni fisiche
  - Fattori ambientali (condensazione, LED)
- **Recupero:**
  - Acqua Potabile (Food Room) → recupero massimo
  - Raw Water → recupero parziale, rischio contaminazione
  - Frutti Dome → idratazione parziale (PURE più idratanti)
  - Cibo Sintetico → piccole quantità

#### **10. Integrazione Fazioni**
- **Custodi:** Favoriscono vegetali → bonus reputazione
- **Culto della Muffa:** Apprezzano funghi → bonus reputazione
- **Mercanti:** Cercano carne → prezzi alti, risorse rare

---

## 💻 REPOSITORY - STATO ATTUALE

### ✅ **COSA C'È GIÀ**

#### **1. Struttura Ambiente**
- **File:** `SceneHierarchy.txt` (linee 2965-3017)
- **ROOM_Kitchen** presente nella scena con:
  - WalkAreaPerspective
  - RoomZone_Kitchen
  - Lighting (3 luci)
  - WalkColliders (pareti)
- **Stato:** ⚠️ Solo struttura grafica, nessuna logica

#### **2. Navigazione UI**
- **File:** `BottomNavigationController.cs` (linea 148)
- **Riferimento:** `{ "btn-kitchen", "kitchen" }`
- **File:** `RoomNavigationAutoSetup.cs` (linea 253)
- **Room Names:** Include "Kitchen" nell'array
- **Stato:** ⚠️ Solo navigazione, nessuna interazione

#### **3. Notifiche**
- **File:** `NotificationTypeSpecDefaults.cs` (linea 71)
- **Messaggio:** "Kitchen Terminal accessed"
- **Stato:** ⚠️ Solo placeholder, nessuna logica

#### **4. Sistema Idratazione Piante** (RELATIVO)
- **File:** `PotStateModel.cs`, `PotActions.cs`, `DayCycleController.cs`
- **Funzionalità:** Sistema completo per idratazione piante nella Dome
- **Nota:** ⚠️ Questo è per le PIANTE, non per il PLAYER

---

### ❌ **COSA MANCA**

#### **1. FoodRoomSystem**
- ❌ `FoodRoomSystem.cs` non esiste
- ❌ `FoodRoomConfig.cs` non esiste
- ❌ `FoodProductionType.cs` (enum) non esiste
- ❌ Nessun sistema di gestione produzione cibo

#### **2. Item FOOD-xxx**
- **File:** `Items.cs` (linee 1-25)
- **Mancanti:**
  - ❌ `FOOD-101` (Vegetali sintetici)
  - ❌ `FOOD-201` (Funghi sintetici)
  - ❌ `FOOD-301` (Carne sintetica)
  - ❌ `WAT-POT` (Acqua Potabile) - presente solo `wat-raw`
  - ❌ `ORG-RES-001` (Residui Proteici)

#### **3. Sistema Slot Produzione**
- ❌ Nessun sistema di slot produzione
- ❌ Nessun timer per produzione
- ❌ Nessun tracking stato slot (LIBERO/OCCUPATO/IN CRESCITA)
- ❌ Nessun sistema espansione slot (1→3)

#### **4. Sistema Slot Idrico**
- ❌ Nessun sistema slot idrico separato
- ❌ Nessuna conversione WAT-RAW → WAT-POT
- ❌ Nessun tracking input/output acqua

#### **5. Sistema Produzione**
- ❌ Nessun timer produzione (1/2/3 giorni)
- ❌ Nessun output cibo
- ❌ Nessun sistema residui proteici
- ❌ Nessun calcolo costi CRY giornalieri

#### **6. Sistema Harvest**
- ❌ Nessuna azione Harvest
- ❌ Nessun trasferimento cibo → inventario
- ❌ Nessun sistema raccolta mattutina

#### **7. Integrazione Cellule Staminali**
- ❌ Nessun sistema inserimento cellule
- ❌ Nessun calcolo effetti casuali
- ❌ Nessun evento narrativo frutti Ipnotici

#### **8. Sistema Idratazione Player**
- ❌ `PlayerHydrationSystem.cs` non esiste
- ❌ Nessun tracking idratazione giocatore (0-100%)
- ❌ Nessun sistema stati (Dehydrated/Low/Normal/Well-Hydrated)
- ❌ Nessun consumo passivo/attivo
- ❌ Nessun recupero tramite acqua/cibo/frutti
- ❌ Nessuna integrazione con ActionSystem (bonus/malus azioni)

#### **9. UI Food Room**
- ❌ `UIFoodRoom.cs` non esiste
- ❌ Nessun HUD macchinario FOOD-SYNTH-001
- ❌ Nessun menu scelta tipologia
- ❌ Nessun campo cellula staminale
- ❌ Nessun display info slot
- ❌ Nessun display slot idrico

#### **10. Interactable Macchinario**
- ❌ Nessun componente `Interactable` sul macchinario
- ❌ Nessun script `FoodSynthMachine.cs` o simile
- ❌ Nessuna interazione con FOOD-SYNTH-001

#### **11. Integrazione Sistemi**
- ❌ Nessuna integrazione con `EconomySystem` (costi CRY)
- ❌ Nessuna integrazione con `ActionSystem` (bonus azioni)
- ❌ Nessuna integrazione con `Inventory` (item FOOD-xxx)
- ❌ Nessuna integrazione con `DayCycleSystem` (timer produzione)
- ❌ Nessuna integrazione con `FactionSystem` (preferenze fazioni)

---

## 📊 CONFRONTO DETTAGLIATO

### **Tabella Requisiti vs Implementazione**

| Requisito GDD | File/Componente Atteso | Stato Repository | Note |
|---------------|------------------------|------------------|------|
| **FoodRoomSystem** | `FoodRoomSystem.cs` | ❌ Non esiste | Sistema core mancante |
| **FoodRoomConfig** | `FoodRoomConfig.cs` | ❌ Non esiste | Configurazione mancante |
| **FoodProductionType** | `FoodProductionType.cs` | ❌ Non esiste | Enum mancante |
| **Item FOOD-101** | `Items.cs` | ❌ Non definito | Item vegetali mancante |
| **Item FOOD-201** | `Items.cs` | ❌ Non definito | Item funghi mancante |
| **Item FOOD-301** | `Items.cs` | ❌ Non definito | Item carne mancante |
| **Item WAT-POT** | `Items.cs` | ❌ Non definito | Solo WAT-RAW presente |
| **Item ORG-RES-001** | `Items.cs` | ❌ Non definito | Residui proteici mancanti |
| **UIFoodRoom** | `UIFoodRoom.cs` | ❌ Non esiste | UI mancante |
| **FoodSynthMachine** | `FoodSynthMachine.cs` | ❌ Non esiste | Interactable mancante |
| **PlayerHydrationSystem** | `PlayerHydrationSystem.cs` | ❌ Non esiste | Sistema idratazione player mancante |
| **ROOM_Kitchen** | Scena Unity | ⚠️ Parziale | Solo struttura grafica |
| **Navigazione Kitchen** | `BottomNavigationController.cs` | ⚠️ Parziale | Solo routing UI |

---

## 🎯 GAP ANALYSIS

### **Gap Critici (Bloccanti)**

1. **🔴 FoodRoomSystem Core**
   - **Impatto:** Blocca tutto il sistema Food Room
   - **Priorità:** CRITICA
   - **File Richiesti:**
     - `FoodRoomSystem.cs`
     - `FoodRoomConfig.cs`
     - `FoodProductionType.cs`

2. **🔴 Item Definitions**
   - **Impatto:** Blocca inventario e produzione
   - **Priorità:** CRITICA
   - **Item Richiesti:**
     - `FOOD-101`, `FOOD-201`, `FOOD-301`
     - `WAT-POT`
     - `ORG-RES-001`

3. **🔴 Sistema Produzione**
   - **Impatto:** Blocca produzione cibo
   - **Priorità:** CRITICA
   - **Funzionalità Richieste:**
     - Timer produzione (1/2/3 giorni)
     - Output cibo
     - Residui proteici
     - Costi CRY giornalieri

4. **🔴 Sistema Idratazione Player**
   - **Impatto:** Blocca survival loop
   - **Priorità:** CRITICA
   - **File Richiesti:**
     - `PlayerHydrationSystem.cs`
     - Integrazione con `ActionSystem`

5. **🔴 UI Food Room**
   - **Impatto:** Blocca interazione giocatore
   - **Priorità:** CRITICA
   - **File Richiesti:**
     - `UIFoodRoom.cs`
     - HUD macchinario

### **Gap Alti (Importanti)**

6. **🟡 Interactable Macchinario**
   - **Impatto:** Blocca interazione fisica
   - **Priorità:** ALTA
   - **File Richiesti:**
     - `FoodSynthMachine.cs`
     - Componente `Interactable` nella scena

7. **🟡 Sistema Harvest**
   - **Impatto:** Blocca raccolta cibo
   - **Priorità:** ALTA
   - **Funzionalità Richieste:**
     - Azione Harvest gratuita
     - Trasferimento inventario

8. **🟡 Integrazione Cellule Staminali**
   - **Impatto:** Blocca meccanica avanzata
   - **Priorità:** MEDIA
   - **Funzionalità Richieste:**
     - Inserimento cellule
     - Calcolo effetti casuali

---

## 📝 RACCOMANDAZIONI IMPLEMENTAZIONE

### **Fase 1: Foundation (Settimana 1)**
1. Creare `FoodProductionType.cs` (enum)
2. Aggiungere item FOOD-xxx in `Items.cs`
3. Creare `FoodRoomConfig.cs` (ScriptableObject)
4. Creare `FoodRoomSystem.cs` (sistema core)

### **Fase 2: Produzione Base (Settimana 2)**
1. Implementare sistema slot produzione
2. Implementare timer produzione
3. Implementare output cibo
4. Implementare costi CRY giornalieri
5. Integrare con `DayCycleSystem`

### **Fase 3: Slot Idrico (Settimana 2-3)**
1. Implementare slot idrico separato
2. Implementare conversione WAT-RAW → WAT-POT
3. Implementare sistema input/output

### **Fase 4: UI e Interazione (Settimana 3)**
1. Creare `UIFoodRoom.cs`
2. Creare HUD macchinario
3. Creare `FoodSynthMachine.cs` (Interactable)
4. Integrare con navigazione esistente

### **Fase 5: Sistema Idratazione Player (Settimana 4)**
1. Creare `PlayerHydrationSystem.cs`
2. Implementare stati (4 livelli)
3. Implementare consumo passivo/attivo
4. Implementare recupero multi-fonte
5. Integrare con `ActionSystem` (bonus/malus)

### **Fase 6: Feature Avanzate (Settimana 5)**
1. Integrazione cellule staminali
2. Sistema residui proteici
3. Integrazione fazioni
4. Eventi narrativi

---

## 🔗 RIFERIMENTI

- **GDD Food Room:** `ENV-010 — Food Room`
- **GDD Loop Food Room:** `Loop Food Room — Produzione di Cibo Sintetico`
- **GDD Idratazione Player:** `Sezione 13 — Player Hydration System`
- **Report Analisi Repository:** `ANALISI_FINE_ANNO_GDD40_vs_REPO_2025.txt`
- **Piano Implementazione:** `PIANO_IMPLEMENTAZIONE_SISTEMI.md` (FASE 3, Task 3.1)

---

**Conclusione:** Il sistema Food Room è **completamente non implementato** nel repository. Esiste solo la struttura grafica dell'ambiente Kitchen e la navigazione UI base. Tutti i sistemi core, produzione, UI e integrazioni sono mancanti e richiedono implementazione completa da zero.
