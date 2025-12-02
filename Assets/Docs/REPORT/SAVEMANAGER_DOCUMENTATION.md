# 📚 Documentazione SaveManager - Sistema di Salvataggio Sporium

**Versione:** 1.0  
**Data:** 2025-01-XX  
**Autore:** Senior Developer Mode

---

## 📋 Indice

1. [Panoramica](#panoramica)
2. [Architettura](#architettura)
3. [Utilizzo Base](#utilizzo-base)
4. [Salvataggio Automatico](#salvataggio-automatico)
5. [Caricamento Manuale](#caricamento-manuale)
6. [Struttura Dati Salvati](#struttura-dati-salvati)
7. [Estendere il Sistema](#estendere-il-sistema)
8. [Troubleshooting](#troubleshooting)

---

## 🎯 Panoramica

Il `SaveManager` è il sistema centralizzato per il salvataggio e caricamento dello stato del gioco in Sporium. Gestisce automaticamente:

- ✅ Stato del gioco (giorno, CRY, azioni)
- ✅ Inventario del giocatore
- ✅ Stato di tutti i vasi (piante, crescita, PH, ecc.)
- ✅ Statistiche del diario
- ✅ Missioni completate

### Caratteristiche Principali

- **Doppio Storage**: File system + PlayerPrefs (backup automatico)
- **Salvataggio Automatico**: Fine giornata, pausa app, chiusura
- **Caricamento Automatico**: All'avvio se esiste un salvataggio
- **Multi-Slot**: Supporto per più slot di salvataggio
- **Thread-Safe**: Gestione sicura delle operazioni I/O

---

## 🏗️ Architettura

### Componenti Principali

```
SaveManager (Singleton)
├── ServiceContainer Integration
├── File System Storage (Application.persistentDataPath/Saves/)
├── PlayerPrefs Backup
└── JSON Serialization
```

### Flusso di Salvataggio

```
1. CollectSaveData() → Raccoglie dati da tutti i sistemi
2. SerializeInventory() → Converte inventario in formato JSON
3. CollectPotStates() → Raccoglie stato di tutti i vasi
4. File.WriteAllText() → Salva su file system
5. PlayerPrefs.SetString() → Backup su PlayerPrefs
```

### Flusso di Caricamento

```
1. File.ReadAllText() → Carica da file system (o PlayerPrefs fallback)
2. JsonUtility.FromJson() → Deserializza JSON
3. ApplySaveData() → Applica dati ai sistemi
4. DeserializeInventory() → Ripristina inventario
5. ApplyPotStates() → Ripristina stato vasi
```

---

## 💻 Utilizzo Base

### Accesso al SaveManager

```csharp
// Ottieni istanza (auto-creata se non esiste)
var saveManager = SaveManager.Instance;

// Oppure tramite ServiceContainer
var saveManager = ServiceContainer.Instance.Get<SaveManager>();
```

### Salvataggio Manuale

```csharp
// Salva nello slot "default"
bool success = SaveManager.Instance.SaveGame("default");

// Salva in slot personalizzato
bool success = SaveManager.Instance.SaveGame("slot_1");
```

### Caricamento Manuale

```csharp
// Carica dallo slot "default"
bool success = SaveManager.Instance.LoadGame("default");

// Carica da slot personalizzato
bool success = SaveManager.Instance.LoadGame("slot_1");
```

### Verifica Esistenza Salvataggio

```csharp
if (SaveManager.Instance.SaveExists("default"))
{
    Debug.Log("Salvataggio trovato!");
    string timestamp = SaveManager.Instance.GetSaveTimestamp("default");
    Debug.Log($"Ultimo salvataggio: {timestamp}");
}
```

### Eliminazione Salvataggio

```csharp
bool success = SaveManager.Instance.DeleteSave("default");
```

---

## 🔄 Salvataggio Automatico

Il sistema salva automaticamente in questi momenti:

### 1. Fine Giornata
**File:** `EndDayButton.cs`

```csharp
public void EndDay()
{
    // ... logica fine giornata ...
    
    // Salvataggio automatico
    var saveManager = ServiceContainer.Instance?.Get<SaveManager>();
    if (saveManager != null)
    {
        saveManager.SaveGame("default");
    }
}
```

### 2. Pausa Applicazione (Mobile)
**File:** `AppRoot.cs`

```csharp
void OnApplicationPause(bool pauseStatus)
{
    if (pauseStatus)
    {
        var saveManager = ServiceContainer.Instance?.Get<SaveManager>();
        saveManager?.SaveGame("default");
    }
}
```

### 3. Perdita Focus (Desktop/Mobile)
**File:** `AppRoot.cs`

```csharp
void OnApplicationFocus(bool hasFocus)
{
    if (!hasFocus)
    {
        var saveManager = ServiceContainer.Instance?.Get<SaveManager>();
        saveManager?.SaveGame("default");
    }
}
```

### 4. Chiusura Applicazione
**File:** `AppRoot.cs`

```csharp
void OnApplicationQuit()
{
    var saveManager = ServiceContainer.Instance?.Get<SaveManager>();
    saveManager?.SaveGame("default");
}
```

---

## 📥 Caricamento Automatico

Il sistema carica automaticamente all'avvio se esiste un salvataggio:

**File:** `GamePlayInstaller.cs`

```csharp
public void Awake()
{
    // ... registrazione servizi ...
    
    var saveManager = SaveManager.Instance;
    if (saveManager != null && saveManager.SaveExists("default"))
    {
        saveManager.LoadGame("default");
    }
}
```

---

## 📊 Struttura Dati Salvati

### GameStateData
```csharp
{
    currentDay: int,           // Giorno corrente
    currentCRY: int,           // CRY attuali
    actionsLeft: int,          // Azioni rimanenti
    condensationAmount: float  // Condensa accumulata
}
```

### InventoryData
```csharp
{
    items: [
        {
            typeId: string,    // ID item (es. "Seed001")
            quantity: int      // Quantità
        }
    ]
}
```

### PotStateData
```csharp
{
    potId: string,                    // ID vaso (es. "POT-001")
    hasPlant: bool,                   // Ha pianta
    stage: int,                       // Stadio crescita
    plantCode: string,                // Codice pianta
    hydration: int,                   // Idratazione
    lightExposure: int,               // Esposizione luce
    growthPoints: int,                // Punti crescita
    daysSincePlant: int,              // Giorni dalla semina
    plantedDay: int,                  // Giorno semina
    lastWateredDay: int,              // Ultimo giorno annaffiatura
    lastLitDay: int,                  // Ultimo giorno illuminazione
    lastLedType: string               // Tipo LED usato
}
```

### Posizione File

**File System:**
```
Windows: C:\Users\<User>\AppData\LocalLow\<Company>\<Game>\Saves\sporium_save.json_default
Mac: ~/Library/Application Support/<Company>/<Game>/Saves/sporium_save.json_default
Linux: ~/.config/unity3d/<Company>/<Game>/Saves/sporium_save.json_default
```

**PlayerPrefs:**
```
Chiave: "Sporium_Save_default"
Chiave Timestamp: "Sporium_Save_default_timestamp"
```

---

## 🔧 Estendere il Sistema

### Aggiungere Nuovi Dati da Salvare

1. **Estendi `CollectSaveData()`:**

```csharp
private GameSaveData CollectSaveData()
{
    var saveData = new GameSaveData();
    
    // ... dati esistenti ...
    
    // NUOVO: Aggiungi i tuoi dati
    saveData.customData = CollectCustomData();
    
    return saveData;
}
```

2. **Aggiungi Struttura Dati:**

```csharp
[Serializable]
private class CustomData
{
    public int customValue;
    public string customString;
}
```

3. **Implementa Serializzazione:**

```csharp
private CustomData CollectCustomData()
{
    // Raccogli i tuoi dati
    return new CustomData
    {
        customValue = someSystem.GetValue(),
        customString = someSystem.GetString()
    };
}
```

4. **Implementa Deserializzazione:**

```csharp
private void ApplySaveData(GameSaveData saveData)
{
    // ... applicazione dati esistenti ...
    
    // NUOVO: Applica i tuoi dati
    if (saveData.customData != null)
    {
        ApplyCustomData(saveData.customData);
    }
}
```

### Aggiungere Metodi di Ripristino nei Sistemi

Per sistemi che devono essere ripristinati durante il caricamento:

```csharp
// Esempio: EconomySystem
public void RestoreState(int cryAmount)
{
    SetCRY(cryAmount);
}

// Esempio: ActionSystem
public void RestoreState(int actionsLeft, int maxActions)
{
    MaxActions = maxActions;
    ActionsLeft = actionsLeft;
    OnActionsChanged?.Invoke(ActionsLeft);
}
```

Poi chiama nel `SaveManager.ApplySaveData()`:

```csharp
if (gameManager.EconomySystem != null)
{
    gameManager.EconomySystem.RestoreState(saveData.gameState.currentCRY);
}
```

---

## 🐛 Troubleshooting

### Problema: Salvataggio non viene creato

**Causa Possibile:** Permessi file system  
**Soluzione:** Verifica che `Application.persistentDataPath` sia accessibile

```csharp
Debug.Log($"Save Path: {Application.persistentDataPath}/Saves/");
```

### Problema: Caricamento non ripristina lo stato

**Causa Possibile:** Sistemi non inizializzati  
**Soluzione:** Assicurati che i sistemi siano registrati nel ServiceContainer prima del caricamento

### Problema: File di salvataggio corrotto

**Causa Possibile:** JSON malformato  
**Soluzione:** Il sistema usa PlayerPrefs come fallback automatico

### Problema: Inventario non viene ripristinato

**Causa Possibile:** `Inventory.Clear()` non chiamato  
**Soluzione:** Verifica che `DeserializeInventory()` chiami `inventory.Clear()` prima di aggiungere item

---

## 📝 Best Practices

1. **Sempre verificare esistenza salvataggio prima di caricare:**
   ```csharp
   if (saveManager.SaveExists("default"))
       saveManager.LoadGame("default");
   ```

2. **Gestire errori durante salvataggio:**
   ```csharp
   bool success = saveManager.SaveGame("default");
   if (!success)
       Debug.LogError("Errore durante il salvataggio!");
   ```

3. **Non salvare durante operazioni critiche:**
   ```csharp
   // ❌ NON fare questo durante animazioni o transizioni
   saveManager.SaveGame("default");
   
   // ✅ Fai questo dopo che l'operazione è completata
   StartCoroutine(SaveAfterDelay());
   ```

4. **Usare slot multipli per test:**
   ```csharp
   // Slot di test separato
   saveManager.SaveGame("test_slot");
   ```

---

## 🔗 Integrazione con Altri Sistemi

### ServiceContainer

Il `SaveManager` è registrato automaticamente nel `ServiceContainer`:

```csharp
// Accesso tramite ServiceContainer
var saveManager = ServiceContainer.Instance.Get<SaveManager>();
```

### GameManager

Il `SaveManager` accede a `GameManager` tramite ServiceContainer:

```csharp
var gameManager = ServiceContainer.Instance?.Get<GameManager>();
```

### AssetManager

Il `SaveManager` può essere esteso per salvare anche configurazioni asset se necessario.

---

## 📞 Supporto

Per problemi o domande sul sistema di salvataggio:

1. Verifica i log di Unity per messaggi `[SaveManager]`
2. Controlla la struttura JSON del file di salvataggio
3. Verifica che tutti i sistemi siano registrati nel ServiceContainer
4. Consulta questa documentazione per esempi

---

**Ultimo Aggiornamento:** 2025-01-XX  
**Versione SaveManager:** 1.0

