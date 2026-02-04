---
name: Food Room + Player Hydration + Inventory Consumption
overview: "Implementazione completa del sistema Food Room con produzione cibo sintetico, sistema idratazione player, e modifica inventario per consumo item (mangiare/bere). I tre sistemi sono integrati: Food Room produce cibo/acqua, Player li consuma dall'inventario, e gli effetti si riflettono su azioni disponibili e idratazione."
todos:
  - id: food-room-foundation
    content: "Fase 1: Foundation - Creare enum FoodProductionType, aggiungere item FOOD-xxx in Items.cs, creare FoodRoomConfig ScriptableObject"
    status: pending
  - id: food-room-core
    content: "Fase 2: FoodRoomSystem Core - Creare FoodProductionSlot, WaterProductionSlot, FoodRoomSystem con logica produzione, timer, costi CRY"
    status: pending
  - id: player-hydration
    content: "Fase 3: PlayerHydrationSystem - Creare sistema idratazione player con 4 stati, consumo passivo/attivo, recupero multi-fonte, integrazione ActionSystem"
    status: pending
  - id: inventory-consumption
    content: "Fase 4: Inventory Consumption - Estendere Inventory con ConsumeItem(), creare ItemConsumptionHandler, modificare HUDInventoryItem per consumo"
    status: pending
  - id: food-room-ui
    content: "Fase 5: UI Food Room - Creare FoodSynthMachine Interactable, UIFoodRoom con HUD completo slot produzione e idrico"
    status: pending
  - id: daycycle-integration
    content: "Fase 6: Integrazione DayCycleSystem - Modificare DayCycleController e GameManager per processare Food Room e idratazione a fine giornata"
    status: pending
  - id: advanced-features
    content: "Fase 7: Feature Avanzate - Implementare residui proteici, effetti cellule staminali, integrazione fazioni (futuro)"
    status: pending
---

# Piano Implementazione: Food Room + Player Hydration + Inventory Consumption

## Panoramica

Implementazione integrata di tre sistemi collegati:

1. **FoodRoomSystem**: Produzione cibo sintetico (Vegetali/Funghi/Carne) e acqua potabile
2. **PlayerHydrationSystem**: Gestione idratazione player (0-100%) con 4 stati e effetti su azioni
3. **Inventory Consumption**: Modifica inventario per permettere consumo item (mangiare/bere)

## Architettura Generale

```
FoodRoomSystem
  ├─> Produce FOOD-101/201/301, WAT-POT, ORG-RES-001
  └─> Output → Inventory

PlayerHydrationSystem
  ├─> Consumo passivo/attivo idratazione
  ├─> Recupero da WAT-POT, WAT-RAW, frutti, cibo
  └─> Effetti su ActionSystem (bonus/malus azioni)

Inventory
  ├─> Metodo ConsumeItem() per mangiare/bere
  └─> Integrazione con PlayerHydrationSystem e ActionSystem
```

## Fase 1: Foundation - Item Definitions e Enum

### 1.1 Aggiungere Item FOOD-xxx in Items.cs

**File:** `Assets/_Project/Scripts/Core/ItemsSystem/Items.cs`

Aggiungere dopo linea 24:

```csharp
// BLK-04.01: Food Room Items
public const string FoodVegetable = "FOOD-101";      // Vegetali sintetici (+1 Azione)
public const string FoodFungus = "FOOD-201";         // Funghi sintetici (+2 Azioni)
public const string FoodMeat = "FOOD-301";           // Carne sintetica (+3 Azioni)
public const string WaterPotable = "WAT-POT";       // Acqua Potabile
public const string OrganicResidue = "ORG-RES-001";  // Residui Proteici (da carne)

// BLK-04.01: Cellule Staminali (utilizzabili in Food Room, produzione futura LAB-BIO)
public const string StemCellVegetable = "CELL-001";  // Cellula Staminale Vegetale
public const string StemCellFungus = "CELL-002";     // Cellula Staminale Fungina
public const string StemCellAnimal = "CELL-003";     // Cellula Staminale Animale
```

### 1.2 Creare FoodProductionType.cs

**File:** `Assets/_Project/Scripts/Systems/FoodRoom/FoodProductionType.cs` (NUOVO)

```csharp
namespace _Project.Sporae.Systems.FoodRoom
{
    public enum FoodProductionType
    {
        None,
        Vegetable,  // 1 giorno, 3 unità, +1 Azione, 1 CRY/giorno
        Fungus,     // 2 giorni, 2 unità, +2 Azioni, 1 CRY/giorno
        Meat        // 3 giorni, 1 unità, +3 Azioni, 2 CRY/giorno
    }
}
```

### 1.3 Creare FoodRoomConfig.cs

**File:** `Assets/_Project/Scripts/Systems/FoodRoom/FoodRoomConfig.cs` (NUOVO)

ScriptableObject con:

- MaxSlots (default: 1, max: 3)
- Costi CRY per tipo produzione
- Timer produzione (giorni)
- Output per tipo
- Bonus azioni per tipo

## Fase 2: FoodRoomSystem Core

### 2.1 Input Materiali per Produzione

**IMPORTANTE:** Secondo GDD, la produzione base di cibo sintetico NON richiede input materiali:

- **Avvio Coltura:** Richiede solo **1 Action** + scelta tipo (Vegetale/Fungo/Carne)
- **Cellule Staminali:** Opzionali, aggiungono effetti casuali ma NON sono necessarie per produzione base
- **Slot Idrico:** Richiede WAT-RAW dall'inventario (già prodotto da CondensationSystem)

**Item esistenti nel repository:**

- ✅ `Items.Water` = "wat-raw" (WAT-RAW) - prodotto da CondensationSystem
- ✅ `Items.WholePlant` = "whole-plant" - prodotto da PotActions.DoUproot()
- ✅ `Items.Fruits` = "fruits-001" - prodotto da PotActions.DoHarvest()
- ✅ `Items.OrganicScrap001` = "org-scr-001" - già definito
- ⚠️ Cellule staminali (CELL-001/002/003) - **da creare come item, utilizzabili ma non producibili** (produzione futura LAB-BIO)

**Integrazione CondensationSystem:**

- WAT-RAW viene prodotto automaticamente da CondensationSystem a fine giornata
- Food Room consuma WAT-RAW dall'inventario per slot idrico
- Nessuna modifica necessaria a CondensationSystem (già funzionante)

### 2.2 Creare FoodProductionSlot.cs

**File:** `Assets/_Project/Scripts/Systems/FoodRoom/FoodProductionSlot.cs` (NUOVO)

Modello dati per slot produzione:

- `FoodProductionType Type`
- `int DaysRemaining`
- `int StartDay`
- `bool HasStemCell` (opzionale cellula staminale)
- `string StemCellTypeId` (CELL-001/002/003 dall'inventario)
- `SlotState State` (Free, Growing, Ready)

**Nota Cellule Staminali:**

- Item CELL-001/002/003 creati in Items.cs e utilizzabili in Food Room
- Possono essere inserite opzionalmente durante avvio produzione
- **Produzione cellule:** NON implementata (verrà con LAB-BIO)
- Per testing: possono essere aggiunte manualmente all'inventario tramite editor/cheat

### 2.3 Creare WaterProductionSlot.cs

**File:** `Assets/_Project/Scripts/Systems/FoodRoom/WaterProductionSlot.cs` (NUOVO)

Modello dati per slot idrico:

- `int RawWaterInput` (WAT-RAW consumato oggi)
- `int PotableWaterOutput` (WAT-POT prodotto domani)
- `bool IsActive`

### 2.4 Creare FoodRoomSystem.cs

**File:** `Assets/_Project/Scripts/Systems/FoodRoom/FoodRoomSystem.cs` (NUOVO)

Sistema core con:

- Lista `FoodProductionSlot` (max 3)
- `WaterProductionSlot` (1 slot separato)
- Metodi:
  - `StartProduction(FoodProductionType, string stemCellTypeId = null)` - costa 1 Action, **NON richiede input materiali base**. Se `stemCellTypeId` fornito, consuma cellula dall'inventario
  - `StartWaterProduction(int rawWaterAmount)` - non costa Action, **consuma WAT-RAW dall'inventario**
  - `Harvest(int slotIndex)` - raccoglie cibo pronto (gratuito, non consuma Action)
  - `HarvestWater()` - raccoglie acqua potabile (gratuito, non consuma Action)
  - `ProcessDailyCosts()` - applica costi CRY giornalieri per slot occupati
  - `ProcessDailyProduction(int currentDay)` - aggiorna timer, genera residui proteici, applica effetti cellule staminali

**Integrazione:**

- `GameManager.TrySpendAction()` per avvio coltura (1 Action)
- `EconomySystem.Spend()` per costi giornalieri (1-2 CRY per slot)
- `Inventory.Consume(Items.Water)` per slot idrico (consuma WAT-RAW)
- `Inventory.Add()` per output cibo/acqua (FOOD-xxx, WAT-POT, ORG-RES-001)
- `DayCycleSystem.OnDayChanged` per timer produzione
- `CondensationSystem` (già implementato) produce WAT-RAW → inventario → Food Room

**Nota Input Materiali:**

- Produzione cibo base: **Nessun input materiale richiesto** (solo 1 Action)
- Slot idrico: Richiede WAT-RAW dall'inventario (già prodotto da CondensationSystem)
- Cellule staminali: **Opzionali, consumate dall'inventario se inserite** (CELL-001/002/003). Produzione cellule: sistema futuro (LAB-BIO non implementato)

## Fase 3: PlayerHydrationSystem

### 3.1 Creare PlayerHydrationSystem.cs

**File:** `Assets/_Project/Scripts/Core/PlayerHydrationSystem.cs` (NUOVO)

Sistema idratazione player con:

- `float HydrationPercent` (0-100%)
- `HydrationState CurrentState` (Dehydrated/Low/Normal/Well-Hydrated)
- Metodi:
  - `ConsumePassive()` - consumo giornaliero base
  - `ConsumeActive(int actionCount)` - consumo per azioni fisiche
  - `RecoverFromWater(int amount, bool isPotable)` - recupero da acqua
  - `RecoverFromFood(int amount)` - recupero da cibo
  - `RecoverFromFruit(int amount, bool isPure)` - recupero da frutti
  - `GetActionModifier()` - ritorna bonus/malus azioni (-2/-1/0/+2)
  - `ProcessDailyConsumption()` - chiamato a fine giornata

**Stati e Effetti:**

- Dehydrated (0-25%): -2 Azioni, rischio salute
- Low (26-50%): -1 Azione
- Normal (51-75%): 0
- Well-Hydrated (76-100%): +2 Azioni

### 3.2 Integrazione con ActionSystem

**File:** `Assets/_Project/Scripts/Core/GameManager.cs`

Modificare `HandleDayChanged()` (linea 166):

```csharp
private void HandleDayChanged(int day)
{   
    _economySystem.Spend(_dailyPowerCost);
    
    // Processa idratazione player PRIMA di resettare azioni
    if (_playerHydrationSystem != null)
    {
        _playerHydrationSystem.ProcessDailyConsumption();
        int hydrationModifier = _playerHydrationSystem.GetActionModifier();
        int baseActions = _actionsPerDay;
        int totalActions = baseActions + hydrationModifier; // Può essere negativo
        _actionSystem.ResetActions(Mathf.Max(1, totalActions)); // Minimo 1 azione
    }
    else
    {
        _actionSystem.ResetActions(_actionsPerDay);
    }
    
    // ... resto del codice
}
```

## Fase 4: Inventory Consumption

### 4.1 Estendere Inventory.cs

**File:** `Assets/_Project/Scripts/Core/ItemsSystem/Inventory.cs`

Aggiungere dopo `Consume()` (linea 67):

```csharp
/// <summary>
/// Consuma un item e applica effetti (cibo/bevande)
/// </summary>
public bool ConsumeItem(string typeId, int quantity = 1)
{
    if (!Consume(typeId, quantity))
        return false;
    
    // Notifica consumo per sistemi esterni (PlayerHydrationSystem, ActionSystem)
    OnItemConsumed?.Invoke(typeId, quantity);
    
    return true;
}

public event Action<string, int> OnItemConsumed;
```

### 4.2 Creare ItemConsumptionHandler.cs

**File:** `Assets/_Project/Scripts/Core/ItemsSystem/ItemConsumptionHandler.cs` (NUOVO)

Handler che ascolta `Inventory.OnItemConsumed` e:

- Identifica tipo item (FOOD-xxx, WAT-POT, WAT-RAW, frutti)
- Chiama `PlayerHydrationSystem.RecoverFromWater/Food/Fruit()`
- Chiama `ActionSystem.AddActions()` per bonus cibo
- Gestisce effetti casuali da cellule staminali (se presente)

**Integrazione in GameManager:**

- Registrare handler in `Awake()`
- Collegare a `_playerInventory.OnItemConsumed`

### 4.3 Modificare HUDInventoryItem.cs

**File:** `Assets/_Project/Scripts/UI/VaultMap/HUDInventoryItem.cs`

Aggiungere logica per distinguere item consumabili:

- Aggiungere campo `bool IsConsumable`
- Aggiungere metodo `SetConsumable(bool, Action<string> onConsume)`
- Modificare `OnPointerClick` per mostrare menu "Usa" se consumabile

### 4.4 Creare InventoryConsumeUI.cs

**File:** `Assets/_Project/Scripts/UI/VaultMap/InventoryConsumeUI.cs` (NUOVO)

UI per conferma consumo:

- Pulsante "Mangia" / "Bevi" contestuale
- Mostra effetti previsti (es. "+1 Azione", "+20% Idratazione")
- Conferma consumo tramite `Inventory.ConsumeItem()`

**Integrazione in HUDInventory:**

- Mostrare pulsante "Usa" quando item selezionato è consumabile
- Aprire `InventoryConsumeUI` al click

## Fase 5: UI Food Room

### 5.1 Creare FoodSynthMachine.cs

**File:** `Assets/_Project/Scripts/Interactables/FoodSynthMachine.cs` (NUOVO)

Componente Interactable per macchinario:

- Estende `Interactable`
- Riferimento a `FoodRoomSystem`
- Al click, apre `UIFoodRoom`

### 5.2 Creare UIFoodRoom.cs

**File:** `Assets/_Project/Scripts/UI/VaultMap/FoodRoom/UIFoodRoom.cs` (NUOVO)

UI completa HUD macchinario:

- Display 3 slot produzione (stato, tipo, giorni restanti, output previsto)
- Display slot idrico (input WAT-RAW, output WAT-POT previsto)
- Pulsanti "Avvia Coltura" per slot liberi
- Menu scelta tipo (Vegetale/Fungo/Carne)
- Campo opzionale cellula staminale (seleziona CELL-001/002/003 dall'inventario, consumata se presente)
- Pulsanti "Harvest" per slot pronti
- Display costi CRY giornalieri totali

**Layout HUD:**

```
[Slot 1] Stato: OCCUPATO
  Coltura: Carne Sintetica
  Giorni restanti: 2
  Output: 1x FOOD-301 (+3 AP)
  Costo: 2 CRY/giorno

[Slot 2] Stato: LIBERO
  [Avvia Coltura] → Menu scelta tipo

[Slot Idrico] Stato: IN PRODUZIONE
  Input oggi: WAT-RAW x5
  Output domani: WAT-POT x5
```

## Fase 6: Integrazione DayCycleSystem

### 6.1 Modificare DayCycleController.cs

**File:** `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

Aggiungere in `HandleDayChanged()` dopo linea 427:

```csharp
// 7. Processa Food Room (produzione, costi, harvest disponibili)
if (_foodRoomSystem != null)
{
    _foodRoomSystem.ProcessDailyProduction(dayIndex);
    _foodRoomSystem.ProcessDailyCosts();
}
```

### 6.2 Modificare GameManager.cs

**File:** `Assets/_Project/Scripts/Core/GameManager.cs`

Aggiungere in `Awake()`:

- Inizializzazione `FoodRoomSystem`
- Inizializzazione `PlayerHydrationSystem`
- Registrazione in `ServiceContainer`

## Fase 7: Residui Proteici e Cellule Staminali

### 7.1 Sistema Residui Proteici

**In FoodRoomSystem.ProcessDailyProduction():**

- Per carne in crescita, genera 1 ORG-RES-001 al giorno 2, 3, 4
- Output finale: 1 FOOD-301 + 3 ORG-RES-001
- Residui vengono aggiunti all'inventario automaticamente durante la crescita

**Processamento Residui (Futuro - LAB-BIO):**

- 3× ORG-RES-001 → 1× CELL-003 (Cellula Staminale Animale) nel Laboratorio Botanico
- Sistema LAB-BIO non ancora implementato, ma struttura pronta per integrazione futura

### 7.2 Sistema Cellule Staminali

**Implementazione Attuale:**

- Item CELL-001/002/003 creati in Items.cs
- Utilizzabili in Food Room durante avvio produzione
- Consumate dall'inventario se inserite
- Effetti applicati al consumo del cibo prodotto

**In FoodRoomSystem.StartProduction():**

- Se `stemCellTypeId` fornito (CELL-001/002/003), verifica disponibilità in inventario
- Consuma cellula dall'inventario: `Inventory.Consume(stemCellTypeId)`
- Salva `StemCellTypeId` nel slot produzione

**In FoodRoomSystem.ProcessDailyProduction() / al Harvest:**

- Se slot ha cellula staminale, applica effetti al cibo prodotto:
  - **CELL-001 (Vegetale):** Nessun effetto (produzione standard)
  - **CELL-002 (Fungina):** 50% Indigestione (-1 AP al consumo), 50% Super Energia (+1 AP al consumo)
  - **CELL-003 (Animale):** Effetti speciali (da definire, possibilmente legati a carne)
- Effetti applicati quando il cibo viene consumato dall'inventario

**Produzione Cellule Staminali (Futuro - LAB-BIO):**

- **NON implementata in questa fase**
- Quando LAB-BIO sarà disponibile:
  - WholePlant → CELL-001 (Vegetale)
  - Fruits → CELL-002 (Fungina)
  - ORG-RES-001 (3×) → CELL-003 (Animale)
  - OrganicScrap001 → CELL-001 (Vegetale)
- Per testing: cellule possono essere aggiunte manualmente all'inventario tramite editor/cheat

**Nota:** Cellule sono utilizzabili ma non producibili. Sistema di produzione verrà implementato con LAB-BIO.

## File da Creare

1. `Assets/_Project/Scripts/Systems/FoodRoom/FoodProductionType.cs`
2. `Assets/_Project/Scripts/Systems/FoodRoom/FoodRoomConfig.cs`
3. `Assets/_Project/Scripts/Systems/FoodRoom/FoodProductionSlot.cs`
4. `Assets/_Project/Scripts/Systems/FoodRoom/WaterProductionSlot.cs`
5. `Assets/_Project/Scripts/Systems/FoodRoom/FoodRoomSystem.cs`
6. `Assets/_Project/Scripts/Core/PlayerHydrationSystem.cs`
7. `Assets/_Project/Scripts/Core/ItemsSystem/ItemConsumptionHandler.cs`
8. `Assets/_Project/Scripts/Interactables/FoodSynthMachine.cs`
9. `Assets/_Project/Scripts/UI/VaultMap/FoodRoom/UIFoodRoom.cs`
10. `Assets/_Project/Scripts/UI/VaultMap/InventoryConsumeUI.cs`

## File da Modificare

1. `Assets/_Project/Scripts/Core/ItemsSystem/Items.cs` - Aggiungere item FOOD-xxx e CELL-001/002/003
2. `Assets/_Project/Scripts/Core/ItemsSystem/Inventory.cs` - Aggiungere `ConsumeItem()` e evento
3. `Assets/_Project/Scripts/Core/GameManager.cs` - Integrazione sistemi e HandleDayChanged
4. `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs` - Processo Food Room giornaliero
5. `Assets/_Project/Scripts/UI/VaultMap/HUDInventoryItem.cs` - Logica consumabili
6. `Assets/_Project/Scripts/UI/VaultMap/HUDInventory.cs` - Integrazione UI consumo

## Dipendenze e Ordine Implementazione

1. **Fase 1** (Foundation): Item definitions, enum, config
2. **Fase 2** (FoodRoomSystem): Sistema produzione base
3. **Fase 3** (PlayerHydrationSystem): Sistema idratazione
4. **Fase 4** (Inventory Consumption): Consumo item
5. **Fase 5** (UI Food Room): Interfaccia utente
6. **Fase 6** (DayCycle Integration): Integrazione ciclo giornaliero
7. **Fase 7** (Advanced Features): Residui, cellule staminali

## Integrazioni con Sistemi Esistenti

### CondensationSystem (Già Implementato)

- **Produzione WAT-RAW:** Automatica a fine giornata basata su piante attive
- **Output:** WAT-RAW aggiunto all'inventario
- **Food Room:** Consuma WAT-RAW dall'inventario per slot idrico
- **Nessuna modifica necessaria** a CondensationSystem

### PotActions.DoUproot() (Già Implementato)

- **Output:** WholePlant aggiunto all'inventario
- **Food Room:** WholePlant può essere usato per cellule staminali (futuro LAB-BIO)
- **Nessuna modifica necessaria** a PotActions

### PotActions.DoHarvest() (Già Implementato)

- **Output:** Fruits aggiunti all'inventario
- **PlayerHydrationSystem:** Fruits possono essere consumati per idratazione parziale
- **Food Room:** Fruits possono essere processati per cellule staminali (futuro LAB-BIO)
- **Nessuna modifica necessaria** a PotActions

### ItemConsumptionHandler

- **Identifica item consumabili:** FOOD-101/201/301, WAT-POT, WAT-RAW, Fruits
- **Effetti cibo:** Bonus azioni immediato (+1/+2/+3)
- **Effetti acqua:** Recupero idratazione (WAT-POT > WAT-RAW)
- **Effetti frutti:** Idratazione parziale (piante PURE più idratanti)

## Testing Scenarios

1. **Produzione Base:** Avviare produzione vegetali (solo 1 Action, nessun input) → verificare timer 1 giorno → harvest → +1 azione
2. **Slot Idrico:** Consumare WAT-RAW dall'inventario → verificare conversione WAT-POT → consumare WAT-POT → verificare recupero idratazione → verificare bonus azioni se Well-Hydrated
3. **Residui Proteici:** Produzione carne → verificare residui proteici giornalieri (ORG-RES-001) → verificare output finale 1 FOOD-301 + 3 ORG-RES-001
4. **Consumo Cibo:** Consumare FOOD-xxx da inventario → verificare bonus azioni immediato (+1/+2/+3) → verificare recupero idratazione parziale
5. **Idratazione Stati:** Idratazione Dehydrated (0-25%) → verificare -2 azioni al giorno successivo → consumare WAT-POT → verificare transizione a Normal/Well-Hydrated
6. **Integrazione CondensationSystem:** Verificare che WAT-RAW prodotto da CondensationSystem sia disponibile in inventario per Food Room
7. **Costi CRY:** Verificare costi giornalieri (1 CRY vegetali/funghi, 2 CRY carne, 0 CRY slot idrico) applicati correttamente a fine giornata
8. **Cellule Staminali - Utilizzo:** Aggiungere CELL-001 manualmente all'inventario → avviare produzione con cellula → verificare consumo cellula dall'inventario → verificare che cellula sia salvata nel slot
9. **Cellule Staminali - Effetti:** Produzione con CELL-002 → harvest cibo → consumare cibo prodotto → verificare effetti casuali (Indigestione -1 AP o Super Energia +1 AP)
10. **Cellule Staminali - Non Produzione:** Verificare che non esista sistema di produzione cellule (solo utilizzo da inventario)
