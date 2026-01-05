---
name: Update GAMEPLAY WATER&Light
overview: Aumentare MaxHydration da 5 a 10 e MaxDaysForFullStress da 4 a 5 per migliorare la granularità dei sistemi Watering e LED. Con MaxHydration=10 ogni punto vale 10% invece di 20%, con MaxDaysForFullStress=5 ogni giorno LED vale 20% invece di 25%. Mantenere 1 punto/giorno come incremento/decremento. Include fix per bug HUD che non si aggiorna quando cambiano i parametri.
todos:
  - id: update-asset
    content: ""
    status: completed
  - id: update-config-script
    content: ""
    status: completed
  - id: update-daycycle-fallbacks
    content: ""
    status: completed
  - id: update-condition-system
    content: ""
    status: completed
  - id: update-pot-actions
    content: ""
    status: completed
  - id: update-ui-widgets
    content: ""
    status: completed
  - id: update-growth-calculator
    content: ""
    status: completed
  - id: fix-hud-alwaysvisible
    content: Aggiungere metodo RefreshPotSystemConfig() in AlwaysVisiblePotHUD.cs per ricaricare config e forzare aggiornamento HUD
    status: completed
    dependencies:
      - update-ui-widgets
  - id: fix-hud-potdetails
    content: Aggiungere metodo RefreshPotSystemConfig() in PotDetailsWidget.cs per ricaricare config e forzare aggiornamento UI
    status: completed
    dependencies:
      - update-ui-widgets
  - id: verify-hud-update
    content: Verificare che tutte le HUD si aggiornino correttamente dopo modifica MaxHydration (test manuale e tramite eventi)
    status: completed
    dependencies:
      - fix-hud-alwaysvisible
      - fix-hud-potdetails
  - id: verify-calculations
    content: Verificare che tutti i calcoli percentuali funzionino correttamente con MaxHydration=10
    status: completed
    dependencies:
      - update-asset
      - update-config-script
      - update-daycycle-fallbacks
      - update-condition-system
      - update-pot-actions
      - update-ui-widgets
      - update-growth-calculator
  - id: add-maxdays-config
    content: Aggiungere MaxDaysForFullStress a PotSystemConfig.cs e asset (default 5)
    status: completed
  - id: replace-hardcoded-light-stress
    content: Sostituire tutte le occorrenze hardcoded maxDaysForFullStress=4 con valore configurabile da PotSystemConfig
    status: completed
    dependencies:
      - add-maxdays-config
  - id: add-getter-potactions
    content: Aggiungere metodo GetMaxDaysForFullStress() in PotActions.cs
    status: completed
    dependencies:
      - add-maxdays-config
  - id: verify-light-stress-calculations
    content: Verificare che tutti i calcoli Light Stress funzionino correttamente con MaxDaysForFullStress=5
    status: completed
    dependencies:
      - replace-hardcoded-light-stress
      - add-getter-potactions
  - id: fix-led-thresholds
    content: Aggiornare soglie hardcoded LED (IsInRedZone, toast zona rossa, GetBurnRiskLevel) per usare maxDaysForFullStress invece di 4 (Opzione B - percentuali)
    status: completed
    dependencies:
      - add-maxdays-config
  - id: update-potactions-check
    content: Aggiornare o rimuovere check hardcoded MaxHydration==4 in PotActions.cs
    status: completed
  - id: update-debug-console
    content: "Aggiornare PotDebugConsole.cs per usare GetMaxDaysForFullStress() invece di valore hardcoded 4 (3 occorrenze: linee 327, 723, 957)"
    status: completed
    dependencies:
      - add-getter-potactions
---

# Piano: Update GAMEPLAY WATER&Light - Aumento MaxHydration e MaxDaysForFullStress (con Fix Bug HUD)

## Obiettivo

Aumentare `MaxHydration` da 5 a 10 per migliorare la granularità del sistema di idratazione. Con MaxHydration=10, ogni punto vale 10% invece di 20%, permettendo una gestione più precisa e strategica del sistema Watering ON/OFF.

Rendere configurabile `maxDaysForFullStress` e aumentarlo da 4 a 5. Con maxDaysForFullStress=5, ogni giorno consecutivo LED aggiunge 20% di stress invece di 25%, permettendo una gestione più precisa del sistema LED ON/OFF.

## Impatto

### Watering (Idratazione)

- **Prima**: 1 punto = 20% (MaxHydration=5)
- **Dopo**: 1 punto = 10% (MaxHydration=10)
- **Incremento/decremento**: Rimane 1 punto (non cambia)
- **Beneficio**: Controllo più fine dell'idratazione, più facile rimanere nel range ottimale

### LED (Light Stress)

- **Prima**: 1 giorno = 25% (maxDaysForFullStress=4)
- **Dopo**: 1 giorno = 20% (maxDaysForFullStress=5)
- **Incremento/decremento**: Rimane 1 giorno (non cambia)
- **Beneficio**: Controllo più fine dello stress luminoso, più facile gestire il sistema LED

## Bug HUD Identificato (da Fixare)

### Problema

Quando MaxHydration viene modificato, le HUD potrebbero non aggiornarsi automaticamente perché:

1. `_potSystemConfig` viene caricato una volta all'inizializzazione con `Resources.Load<PotSystemConfig>()`
2. Se il file asset viene modificato, il riferimento già caricato potrebbe non riflettere il nuovo valore
3. Le HUD calcolano le percentuali usando `_potSystemConfig.MaxHydration` che potrebbe essere obsoleto

### Soluzione

Forzare il ricaricamento del config e l'aggiornamento di tutte le HUD quando MaxHydration cambia:

1. **AlwaysVisiblePotHUD.cs**: Aggiungere metodo per ricaricare config e forzare refresh
2. **PotDetailsWidget.cs**: Assicurarsi che ricarichi il config quando necessario
3. **PlantCardV2Controller.cs**: Forzare refresh quando viene aperto
4. **PotActions.cs**: Emettere evento `PotEvents.EmitChanged()` dopo modifiche che potrebbero influenzare MaxHydration

## File da Modificare

### 1. File Asset (Valore Principale)

**File**: `Assets/Resources/Configs/PotSystemConfig.asset`

- **Linea 18**: Cambiare `MaxHydration: 5` → `MaxHydration: 10`
- **Aggiungere dopo MaxLightExposure**: `MaxDaysForFullStress: 5`

### 2. Script PotSystemConfig.cs

**File**: `Assets/_Project/Scripts/Dome/PotSystemConfig.cs`

- **Linea 44**: Cambiare `MaxHydration = 5; // 5 step = 20% per punto` → `MaxHydration = 10; // 10 step = 10% per punto`
- **Aggiungere dopo MaxLightExposure** (linea ~45): `[SerializeField] public int MaxDaysForFullStress = 5; // 5 giorni = 20% per giorno`
- **Linea 191**: Cambiare `config.MaxHydration = 5; // 5 step = 20% per punto` → `config.MaxHydration = 10; // 10 step = 10% per punto`
- **Aggiungere dopo config.MaxLightExposure** (linea ~192): `config.MaxDaysForFullStress = 5; // 5 giorni = 20% per giorno`

### 3. Fallback Hardcoded da Aggiornare

#### DayCycleController.cs

**File**: `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

- **Linea 465**: Cambiare fallback da `: 4` → `: 10` (aggiornare anche commento se presente)
- **Linea 1057**: Cambiare fallback da `: 4` → `: 10`
- **Linea 1123**: Aggiornare commento da `// Applica +20% idratazione (1 punto se max=5)` → `// Applica +10% idratazione (1 punto se max=10)`
- **Linea 1430**: Cambiare fallback da `: 4` → `: 10`
- **Linea 1803**: Cambiare fallback da `: 5` → `: 10`

#### PlantConditionSystem.cs

**File**: `Assets/_Project/Scripts/Dome/PotSystem/Condition/PlantConditionSystem.cs`

- **Linea 56**: Cambiare fallback da `: 4` → `: 10`

#### PotActions.cs

**File**: `Assets/_Project/Scripts/Dome/PotActions.cs`

- **Linea 1552**: Cambiare fallback da `: 5` → `: 10` e commento da `// Fallback a 5 step = 20% ciascuno` → `// Fallback a 10 step = 10% ciascuno`

#### PotDetailsWidget.cs

**File**: `Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs`

- **Linea 1299**: Cambiare fallback da `?? 5` → `?? 10` e commento da `// 5 step = 20% ciascuno` → `// 10 step = 10% ciascuno`
- **Aggiungere**: Metodo `RefreshPotSystemConfig()` per ricaricare il config quando necessario

#### PlantCardV2DataBinder.cs

**File**: `Assets/_Project/Scripts/UI/UIToolkit/PlantCard/PlantCardV2DataBinder.cs`

- **Linea 640**: Cambiare fallback da `: 5` → `: 10`
- **Linea 1040**: Cambiare fallback da `: 5` → `: 10`
- **Linea 1264**: Cambiare fallback da `: 5` → `: 10`

#### GrowthPointsCalculator.cs

**File**: `Assets/_Project/Scripts/Dome/PotSystem/Growth/GrowthPointsCalculator.cs`

- **Linea 37**: Cambiare fallback da `: 4` → `: 10`

#### AlwaysVisiblePotHUD.cs

**File**: `Assets/_Project/Scripts/UI/VaultMap/AlwaysVisiblePotHUD.cs`

- **Linea 636**: Cambiare fallback da `?? 5` → `?? 10`
- **Aggiungere**: Metodo `RefreshPotSystemConfig()` per ricaricare il config e forzare aggiornamento HUD
- **Modificare**: Metodo `Initialize()` o aggiungere metodo pubblico per ricaricare config

### 4. Fix Bug HUD - Forzare Aggiornamento

#### AlwaysVisiblePotHUD.cs

**File**: `Assets/_Project/Scripts/UI/VaultMap/AlwaysVisiblePotHUD.cs`

- **Aggiungere metodo pubblico**:
```csharp
/// <summary>
/// Ricarica PotSystemConfig e forza aggiornamento di tutte le HUD
/// Utile quando MaxHydration o altri parametri vengono modificati
/// </summary>
public void RefreshPotSystemConfig()
{
    _potSystemConfig = Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");
    if (_potSystemConfig != null)
    {
        UpdateAllAlwaysVisibleHUDs();
        SporiumLogger.LogInfo(LogCategory.UI, "PotSystemConfig ricaricato e HUD aggiornate");
    }
}
```


#### PotDetailsWidget.cs

**File**: `Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs`

- **Aggiungere metodo pubblico**:
```csharp
/// <summary>
/// Ricarica PotSystemConfig e forza aggiornamento UI
/// </summary>
public void RefreshPotSystemConfig()
{
    _potSystemConfig = Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");
    if (_currentSelectedPot != null)
    {
        UpdatePotDetails(_currentSelectedPot);
    }
}
```


#### PlantCardV2Controller.cs

**File**: `Assets/_Project/Scripts/UI/UIToolkit/PlantCard/PlantCardV2Controller.cs`

- **Modificare**: Assicurarsi che `RefreshData()` ricarichi anche il config se necessario
- **Verificare**: Che `OnPotStateChanged` forzi sempre il refresh completo

### 5. Rendere maxDaysForFullStress Configurabile

#### PotSystemConfig.cs

**File**: `Assets/_Project/Scripts/Dome/PotSystemConfig.cs`

- **Aggiungere campo** dopo `MaxLightExposure` (linea ~45):
  ```csharp
  [SerializeField] public int MaxDaysForFullStress = 5; // 5 giorni = 20% per giorno
  ```

- **Aggiornare CreateDefaultConfig()** (linea ~192):
  ```csharp
  config.MaxDaysForFullStress = 5; // 5 giorni = 20% per giorno
  ```


#### PotSystemConfig.asset

**File**: `Assets/Resources/Configs/PotSystemConfig.asset`

- **Aggiungere dopo MaxLightExposure**:
  ```
  MaxDaysForFullStress: 5
  ```


#### Sostituire tutte le occorrenze hardcoded

**PlantCardV2DataBinder.cs** (3 occorrenze: linee 648, 1047, 1293)

- Sostituire `const int maxDaysForFullStress = 4;` con:
  ```csharp
  int maxDaysForFullStress = _potSystemConfig != null ? _potSystemConfig.MaxDaysForFullStress : 5;
  ```


**AlwaysVisiblePotHUD.cs** (2 occorrenze: linee 853, 1143)

- Sostituire `const int maxDaysForFullStress = 4;` con:
  ```csharp
  int maxDaysForFullStress = _potSystemConfig != null ? _potSystemConfig.MaxDaysForFullStress : 5;
  ```


**PotDetailsWidget.cs** (2 occorrenze: linee 1385, 2520)

- Sostituire `const int maxDaysForFullStress = 4;` con:
  ```csharp
  int maxDaysForFullStress = _potSystemConfig != null ? _potSystemConfig.MaxDaysForFullStress : 5;
  ```


**PotDebugConsole.cs** (3 occorrenze: linee 327, 723, 957)

- **Linea 327** (in `SetLightStressPercent()`): Sostituire `const int maxDaysForFullStress = 4;` con:
  ```csharp
  int maxDaysForFullStress = _selectedPot?.GetMaxDaysForFullStress() ?? 5;
  ```

- **Linea 723** (quando carica valori iniziali): Sostituire `const int maxDaysForFullStress = 4;` con:
  ```csharp
  int maxDaysForFullStress = _selectedPot?.GetMaxDaysForFullStress() ?? 5;
  ```

- **Linea 957** (quando mostra valore corrente): Sostituire `const int maxDaysForFullStress = 4;` con:
  ```csharp
  int maxDaysForFullStress = _selectedPot?.GetMaxDaysForFullStress() ?? 5;
  ```

- **Nota**: `GetMaxHydration()` è già usato correttamente (linea 294, 809) e funzionerà automaticamente

**PotActions.cs**

- **Aggiungere metodo** dopo `GetMaxLightExposure()` (linea ~1560):
  ```csharp
  public int GetMaxDaysForFullStress()
  {
      return config ? config.MaxDaysForFullStress : 5;
  }
  ```

- **Aggiornare check MaxHydration** (linee 82, 91): Cambiare da `MaxHydration == 4` a `MaxHydration <= 5` per rilevare sistemi vecchi/intermedi, oppure rimuovere se non più necessario

**PlantConditionSystem.cs** (1 occorrenza: linea 50)

- Sostituire `const int maxDaysForFullStress = 4;` con:
  ```csharp
  int maxDaysForFullStress = potConfig != null ? potConfig.MaxDaysForFullStress : 5;
  ```


### 6. Fix Soglie LED - Adattare a Percentuali (Opzione B)

#### DayCycleController.cs

**File**: `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

- **Aggiungere metodo helper** dopo `GetMaxLightExposureForPot()` (linea ~1382):
  ```csharp
  private int GetMaxDaysForFullStress()
  {
      return _potSystemConfig != null ? _potSystemConfig.MaxDaysForFullStress : 5;
  }
  ```

- **Linea 1279**: Cambiare `if (consecutiveDays >= 4)` → `if (consecutiveDays >= GetMaxDaysForFullStress())`
- **Linea 1370**: Cambiare `if (consecutiveDays >= 4 && enableDebugLogs)` → `if (consecutiveDays >= GetMaxDaysForFullStress() && enableDebugLogs)`

#### PotStateModel.cs

**File**: `Assets/_Project/Scripts/Dome/PotStateModel.cs`

- **IsInRedZone()** (linea 500): Modificare per accettare parametro:
  ```csharp
  public bool IsInRedZone(int maxDaysForFullStress)
  {
      return GetConsecutiveLedDays() >= maxDaysForFullStress && LedSystemState != LedSystemState.Off;
  }
  ```

- **GetBurnRiskLevel()** (linea 476-493): Modificare per accettare parametro e usare percentuali:
  ```csharp
  public int GetBurnRiskLevel(int maxDaysForFullStress)
  {
      int consecutiveDays = GetConsecutiveLedDays();
      
      if (LedSystemState == LedSystemState.Off || consecutiveDays <= 1)
          return 0;  // Nessun rischio
      
      // Calcola percentuale stress
      float stressPercent = (float)consecutiveDays / maxDaysForFullStress * 100f;
      
      if (stressPercent < 40f)  // < 2 giorni
          return 0;
      if (stressPercent < 80f)  // 2-3 giorni (40-60%)
          return 1;  // Rischio medio
      if (stressPercent < 100f)  // 4 giorni (80%)
          return 2;  // Rischio alto
      return 3;  // 5+ giorni (100%+) - Rischio critico
  }
  ```


#### Cercare e aggiornare tutti i chiamanti

**Cercare occorrenze di**:

- `IsInRedZone()` senza parametri
- `GetBurnRiskLevel()` senza parametri

**File da verificare**:

- `DayCycleController.cs`
- `PlantConditionSystem.cs`
- `PotActions.cs`
- UI components che usano questi metodi

**Aggiornare chiamate** per passare `maxDaysForFullStress`:

```csharp
// Prima
bool inRedZone = pot.IsInRedZone();
int burnRisk = pot.GetBurnRiskLevel();

// Dopo
int maxDays = GetMaxDaysForFullStress(); // o da config
bool inRedZone = pot.IsInRedZone(maxDays);
int burnRisk = pot.GetBurnRiskLevel(maxDays);
```

### 7. Aggiornare Console di Debug

#### PotDebugConsole.cs

**File**: `Assets/_Project/Scripts/Debug/PotDebugConsole.cs`

**⚠️ IMPORTANTE**: Questa console usa valori hardcoded per `maxDaysForFullStress` che devono essere aggiornati per funzionare correttamente con il nuovo sistema.

- **Linea 327** (in `SetLightStressPercent()`): Sostituire `const int maxDaysForFullStress = 4;` con:
  ```csharp
  int maxDaysForFullStress = _selectedPot?.GetMaxDaysForFullStress() ?? 5;
  ```

- **Linea 723** (quando carica valori iniziali del POT selezionato): Sostituire `const int maxDaysForFullStress = 4;` con:
  ```csharp
  int maxDaysForFullStress = _selectedPot?.GetMaxDaysForFullStress() ?? 5;
  ```

- **Linea 957** (quando mostra valore corrente Light Stress): Sostituire `const int maxDaysForFullStress = 4;` con:
  ```csharp
  int maxDaysForFullStress = _selectedPot?.GetMaxDaysForFullStress() ?? 5;
  ```

- **Nota**: `GetMaxHydration()` è già usato correttamente (linee 294, 809) e funzionerà automaticamente con il nuovo valore

#### DifficultyCalibrationConsole.cs

**File**: `Assets/_Project/Scripts/Debug/DifficultyCalibrationConsole.cs`

**⚠️ OPZIONALE**: Questa console non mostra direttamente `MaxHydration` o `MaxDaysForFullStress` perché sono in `PotSystemConfig`, non in `DifficultyCalibrationConfig`.

**Considerazione**: Potrebbe essere utile aggiungere una sezione per modificare questi valori in runtime, ma non è strettamente necessario se vengono modificati solo tramite asset Unity.

**Raccomandazione**: Non aggiungere per ora, ma documentare che questi valori sono modificabili solo tramite asset `PotSystemConfig.asset`.

## ⚠️ PUNTI CRITICI - Verifiche Necessarie

### Problemi Identificati che Potrebbero Rompersi

#### 1. Soglie Hardcoded LED (Giorni Assoluti vs Percentuali)

**Problema**: I moltiplicatori LED e le soglie di rischio usano giorni assoluti hardcoded, non percentuali:

- `GetLedEffectMultiplier()`: soglie `1`, `2-3`, `4+` giorni
- `GetLedMalusMultiplier()`: soglia `<= 3` vs `>= 4` giorni
- `IsInRedZone()`: soglia `>= 4` giorni
- `GetBurnRiskLevel()`: soglie `2-3`, `4-5`, `6+` giorni
- Toast "Zona rossa": soglia `>= 4` giorni

**Analisi**: Queste soglie sono progettate per essere basate su giorni assoluti, non percentuali. Con maxDaysForFullStress=5:

- **Giorno 4** = 80% stress (prima era 100%)
- **Giorno 5** = 100% stress (nuova zona rossa)

**Decisione Necessaria**:

- **Opzione A**: Mantenere soglie assolute (4 giorni = zona rossa) → più facile da raggiungere
- **Opzione B**: Adattare soglie a percentuali (5 giorni = zona rossa) → più difficile da raggiungere

**✅ DECISIONE UTENTE: Opzione B** - Adattare a percentuali per coerenza con il nuovo sistema. La zona rossa sarà quando si raggiunge 100% stress (5 giorni con maxDaysForFullStress=5).

#### 2. PotActions.cs - Check Hardcoded MaxHydration

**File**: `Assets/_Project/Scripts/Dome/PotActions.cs`

- **Linee 82, 91**: Check `config.MaxHydration == 4` per rilevare vecchio sistema
- **Problema**: Questo check non rileverà più il vecchio sistema se MaxHydration è già stato aggiornato
- **Soluzione**: Aggiornare il check per rilevare anche `MaxHydration == 5` (sistema intermedio) o rimuoverlo se non più necessario

#### 3. Colori UI Light Stress

**File**: `Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs`

- **Linee 1393-1399**: Soglie percentuali per colori (75%, 50%)
- **Analisi**: Queste soglie percentuali continueranno a funzionare correttamente:
  - `> 75%` = Rosso (prima: 4 giorni, dopo: 4 giorni = 80%)
  - `> 50%` = Viola (prima: 3 giorni, dopo: 3 giorni = 60%)
  - `> 0%` = Arancione (prima: 1-2 giorni, dopo: 1-2 giorni)
- **Verdetto**: ✅ OK - Le soglie percentuali sono appropriate

### File da Modificare per Fix Soglie LED

#### DayCycleController.cs

**File**: `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

- **Linea 1279**: Cambiare `if (consecutiveDays >= 4)` → `if (consecutiveDays >= maxDaysForFullStress)` (usare valore da config)
- **Linea 1370**: Cambiare `if (consecutiveDays >= 4)` → `if (consecutiveDays >= maxDaysForFullStress)`
- **GetLedEffectMultiplier()** (linea 1288-1294): 
  - **Opzione A**: Mantenere soglie assolute (1, 2-3, 4+) → OK se vogliamo mantenere comportamento attuale
  - **Opzione B**: Adattare a percentuali usando `maxDaysForFullStress` → Richiede refactoring
  - **Raccomandazione**: Mantenere soglie assolute per ora (Opzione A), ma documentare che potrebbero essere adattate in futuro
- **GetLedMalusMultiplier()** (linea 1299-1304): Stessa considerazione

#### PotStateModel.cs

**File**: `Assets/_Project/Scripts/Dome/PotStateModel.cs`

- **Linea 500**: `IsInRedZone()` - Cambiare `>= 4` → `>= maxDaysForFullStress` (richiede accesso a config)
- **GetBurnRiskLevel()** (linea 476-493): 
  - **Opzione A**: Mantenere soglie assolute (2-3, 4-5, 6+)
  - **Opzione B**: Adattare a percentuali
  - **Raccomandazione**: Mantenere soglie assolute per ora, ma considerare adattamento futuro

#### PotActions.cs

**File**: `Assets/_Project/Scripts/Dome/PotActions.cs`

- **Linee 82, 91**: Aggiornare check da `MaxHydration == 4` a `MaxHydration <= 5` per rilevare sistemi vecchi/intermedi, oppure rimuovere se non più necessario

## Verifiche Post-Modifica

### Calcoli Percentuali

Tutti i calcoli percentuali usano la formula `hydration / maxHydration * 100`, quindi funzioneranno automaticamente con il nuovo valore. Nessuna modifica necessaria a:

- `PlantCardCalculators.CalculateHydrationPercent()`
- Calcoli in `PlantConditionSystem`
- Calcoli in UI vari

### Incremento/Decremento

- **IncreaseHydration()**: Già incrementa di 1 punto (non cambia)
- **Decay giornaliero**: Già decrementa di 1 punto (`dailyHydrationDecay = 1`, non cambia)
- **Watering ON**: Già incrementa di 1 punto (non cambia)
- **Watering OFF**: Già decrementa di 1 punto tramite decay (non cambia)

### Soglie Overwatering

Le soglie overwatering sono calcolate dinamicamente usando percentuali:

- `overwateringThreshold = Mathf.CeilToInt(maxHydration * OverwateringThresholdPercent / 100f)`
- Funzioneranno automaticamente con MaxHydration=10

### Fix HUD - Verifiche Specifiche

1. **Dopo modifica asset**: Verificare che tutte le HUD mostrino percentuali corrette (10% per punto idratazione, 20% per giorno LED)
2. **Test manuale**: Chiamare `RefreshPotSystemConfig()` su AlwaysVisiblePotHUD e verificare aggiornamento
3. **Test eventi**: Verificare che `PotEvents.EmitChanged()` aggiorni correttamente le HUD
4. **Test runtime**: Modificare MaxHydration e MaxDaysForFullStress in Unity Editor e verificare che le HUD si aggiornino

### Light Stress - Verifiche Specifiche

1. Verificare che ogni giorno consecutivo LED aggiunga 20% di stress (1/5)
2. Verificare che 5 giorni consecutivi raggiungano 100% di stress
3. Verificare che quando LED è OFF, lo stress decresca di 20% al giorno (1 giorno)
4. Verificare che tutte le UI mostrino correttamente le percentuali aggiornate
5. **⚠️ CRITICO**: Verificare che la "zona rossa" venga attivata correttamente a 5 giorni (100% stress) invece di 4 giorni
6. **⚠️ CRITICO**: Verificare che IsInRedZone() funzioni correttamente con maxDaysForFullStress=5
7. **⚠️ CRITICO**: Verificare che GetBurnRiskLevel() funzioni correttamente con le nuove soglie percentuali (40%, 80%, 100%)
8. **⚠️ CRITICO**: Verificare che tutti i chiamanti di IsInRedZone() e GetBurnRiskLevel() passino correttamente il parametro maxDaysForFullStress
9. **⚠️ CRITICO**: Verificare che PotDebugConsole mostri correttamente i valori Light Stress con maxDaysForFullStress=5
10. **⚠️ CRITICO**: Verificare che PotDebugConsole permetta di impostare correttamente Light Stress usando il nuovo valore maxDaysForFullStress=5

## ⚠️ Compatibilità e Disponibilità Automatica

### Disponibilità Automatica dei Nuovi Valori

✅ **MaxHydration**:

- Disponibile automaticamente tramite `PotSystemConfig.MaxHydration`
- Tutti i sistemi che usano `potConfig.MaxHydration` o `GetMaxHydration()` riceveranno automaticamente il nuovo valore
- I calcoli percentuali (`hydration / maxHydration * 100`) funzioneranno automaticamente

✅ **MaxDaysForFullStress**:

- Dopo l'implementazione, sarà disponibile tramite `PotSystemConfig.MaxDaysForFullStress`
- I sistemi che usano `potConfig.MaxDaysForFullStress` o `GetMaxDaysForFullStress()` riceveranno automaticamente il nuovo valore
- I calcoli percentuali (`consecutiveDays / maxDaysForFullStress * 100`) funzioneranno automaticamente

### Sistemi che Funzioneranno Automaticamente

✅ **Overwatering System**: Usa percentuali, funzionerà automaticamente

✅ **PlantConditionSystem**: Usa percentuali per idratazione e light stress, funzionerà automaticamente

✅ **UI Components**: Dopo fix HUD, si aggiorneranno automaticamente

✅ **Calcoli Percentuali**: Tutti i calcoli basati su percentuali funzioneranno automaticamente

### Sistemi che Richiedono Fix (Opzione B - Implementati)

✅ **IsInRedZone()**: Fix implementato - ora accetta maxDaysForFullStress come parametro (Sezione 6)

✅ **Toast "Zona rossa"**: Fix implementato - ora usa GetMaxDaysForFullStress() (DayCycleController.cs)

✅ **GetBurnRiskLevel()**: Fix implementato - ora usa percentuali basate su maxDaysForFullStress (Sezione 6)

⚠️ **LED Multipliers**: Mantenuti con soglie assolute (1, 2-3, 4+) per gameplay balance - OK, non richiedono fix (moltiplicatori effetti/malus basati su giorni assoluti, non percentuali)

## Note Importanti

1. **Compatibilità Salvataggi**: I salvataggi esistenti manterranno i valori di idratazione (es. Hydration=3), ma con MaxHydration=10, 3 punti = 30% invece di 60%. Questo è intenzionale e migliorerà la granularità.
2. **Light Stress**: I giorni consecutivi LED esistenti (es. DaysLedBlueConsecutive=3) rimangono invariati, ma con MaxDaysForFullStress=5, 3 giorni = 60% invece di 75%. Questo è intenzionale e migliorerà la granularità.
3. **Range Ottimali**: I range ottimali definiti in `StageRequirements` (es. 50-75%) rimangono invariati e funzioneranno correttamente con la nuova granularità.
4. **UI**: Le barre di progresso e visualizzazioni percentuali si aggiorneranno automaticamente mostrando valori più precisi.
5. **Fix HUD**: Il metodo `RefreshPotSystemConfig()` può essere chiamato manualmente o automaticamente quando si rileva che il config è cambiato. In Unity Editor, potrebbe essere necessario ricaricare la scena o chiamare manualmente il refresh.

## Testing Consigliato

### Watering (Idratazione)

1. Verificare che l'idratazione si incrementi correttamente di 1 punto (10%) quando Watering è ON
2. Verificare che l'idratazione si decrementi correttamente di 1 punto (10%) quando Watering è OFF
3. Verificare che i range ottimali funzionino correttamente con la nuova granularità
4. Verificare che le soglie overwatering si calcolino correttamente

### LED (Light Stress)

5. Verificare che il Light Stress si incrementi correttamente di 1 giorno (20%) quando LED è ON
6. Verificare che il Light Stress si decrementi correttamente di 1 giorno (20%) quando LED è OFF
7. Verificare che 5 giorni consecutivi raggiungano 100% di stress (zona rossa)

### UI e HUD

8. **Verificare che l'UI mostri correttamente le percentuali aggiornate dopo modifica MaxHydration e MaxDaysForFullStress**
9. **Test fix HUD**: Modificare MaxHydration e MaxDaysForFullStress in Unity Editor, chiamare `RefreshPotSystemConfig()` e verificare che tutte le HUD si aggiornino