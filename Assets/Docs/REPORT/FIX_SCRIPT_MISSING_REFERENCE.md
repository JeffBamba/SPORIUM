# FIX: Script Missing Reference e NullReferenceException

**Data:** 2025-12-09  
**Problema:** Riferimento a script mancante e NullReferenceException in PotDetailsWidget

---

## 🔴 PROBLEMI IDENTIFICATI

### 1. **"The referenced script (Unknown) on this Behaviour is missing!"**

**Causa:** Il riferimento a `WateringMinigame` nella scena Unity non è stato rimosso dopo la rimozione dello script.

**Soluzione:** Rimuovere manualmente il riferimento nella scena Unity.

---

### 2. **NullReferenceException in PotDetailsWidget.Initialize()**

**Causa:** `_wateringButton` (o altri bottoni) sono null quando viene chiamato `Initialize()`.

**Soluzione:** Aggiunti controlli null nel codice per evitare il crash.

---

## ✅ MODIFICHE APPLICATE AL CODICE

### `PotDetailsWidget.cs` - Initialize()

Aggiunti controlli null per tutti i bottoni prima di aggiungere listener:

```csharp
if (_plantButton != null)
    _plantButton.onClick.AddListener(...);
else
    Debug.LogError("[PotDetailsWidget] ⚠️ _plantButton non assegnato!");

if (_wateringButton != null)
    _wateringButton.onClick.AddListener(...);
else
    Debug.LogError("[PotDetailsWidget] ⚠️ _wateringButton non assegnato!");
```

### `PotDetailsWidget.cs` - UpdateActionButtons()

Aggiunti controlli null prima di aggiornare i bottoni:

```csharp
if (_wateringButton != null)
{
    bool isWateringOn = pot.PotActions != null && pot.PotActions.IsWateringSystemOn();
    string waterButtonText = isWateringOn ? "Irrigazione ON" : "Irrigazione OFF";
    UpdateButtonState(_wateringButton, pot.PotActions.CanWater(), waterButtonText);
}
```

---

## 📋 STEP MANUALI IN UNITY

### **STEP 1: Rimuovere Riferimento Script Mancante**

1. Apri la scena principale (es. `VaultMap`)
2. Seleziona il GameObject `UI_PotDetails` nella gerarchia
3. Nel **Inspector**, cerca il componente `PotDetailsWidget`
4. Cerca un campo che mostra **"Missing (Mono Script)"** o **"None (Mono Script)"**
5. Se trovi un riferimento a `WateringMinigame` o uno script sconosciuto:
   - Clicca sul campo
   - Seleziona **"Remove Component"** o imposta il campo a **None**
   - Oppure elimina il componente intero se non serve

**Nota:** Se non vedi riferimenti mancanti, il problema potrebbe essere risolto automaticamente al prossimo avvio.

---

### **STEP 2: Verificare Riferimenti Bottoni**

1. Seleziona `UI_PotDetails` nella gerarchia
2. Nel **Inspector**, trova il componente `PotDetailsWidget`
3. Verifica che tutti i campi SerializeField siano assegnati:
   - `_plantButton` → Deve puntare al bottone "Plant"
   - `_wateringButton` → Deve puntare al bottone "Watering"
   - `_blueLedButton` → Deve puntare al bottone Blue LED
   - `_redLedButton` → Deve puntare al bottone Red LED
   - `_sprayButton` → Deve puntare al bottone "Spray Antifungal"
   - `_harvestButton` → Deve puntare al bottone "Harvest"
   - `_uprootButton` → Deve puntare al bottone "Uproot"

4. Se un bottone è **None** o **Missing**:
   - Trova il bottone corrispondente nella gerarchia (es. `UI_PotDetails/Panel/Bottom/Watering`)
   - Trascina il bottone dal **Hierarchy** al campo corrispondente nell'**Inspector**

---

### **STEP 3: Verificare PlantGrowthConfig**

Il warning `PlantGrowthConfig non trovato` è meno critico, ma puoi risolverlo:

1. Verifica che esista il file:
   - `Assets/Resources/Configs/PlantGrowthConfig_Default.asset`
2. Se non esiste, crealo o rimuovi il caricamento dal codice (il sistema usa valori di default)

---

### **STEP 4: Test**

1. Salva la scena (Ctrl+S)
2. Avvia Play Mode
3. Verifica che non ci siano più errori nella Console
4. Seleziona un vaso e verifica che i bottoni funzionino correttamente

---

## ⚠️ NOTE

- Il codice ora gestisce gracefully i bottoni mancanti (non crasha più)
- Se un bottone è null, vedrai un messaggio di errore nella Console che ti indica quale bottone manca
- I bottoni null non causeranno più NullReferenceException, ma non funzioneranno finché non vengono assegnati

---

## 🔍 DEBUG

Se il problema persiste:

1. Controlla la **Console** per messaggi di errore specifici
2. Verifica che tutti i GameObject UI esistano nella scena
3. Controlla che i nomi dei GameObject corrispondano a quelli cercati nel codice
4. Verifica che il componente `PotDetailsWidget` sia presente su `UI_PotDetails`

---

**Fix applicato:** 2025-12-09

