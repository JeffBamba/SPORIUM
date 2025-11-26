# Come Creare PlantData in Unity

## Problema: Menu "Create > Sporae > PlantData" non visibile

Se il menu non appare, potrebbe essere che Unity non ha ancora compilato il nuovo script. Ecco le soluzioni:

---

## ✅ Soluzione 1: Aspettare Compilazione Unity

1. **Verifica Console Unity:**
   - Apri la Console (`Ctrl+Shift+C` o `Window > General > Console`)
   - Verifica che non ci siano errori di compilazione
   - Se ci sono errori, risolvili prima

2. **Forza Ricompilazione:**
   - Vai su `Assets > Reimport All` (opzionale)
   - Oppure modifica qualsiasi script e salva per forzare ricompilazione

3. **Verifica Menu:**
   - Nel Project window, click destro in una cartella
   - Dovresti vedere `Create > Sporae > PlantData`
   - Se non appare, usa Soluzione 2

---

## ✅ Soluzione 2: Usare PlantData Esistenti (CONSIGLIATO)

Ho già creato 3 PlantData di esempio pronti all'uso:

### File Creati:
- `Assets/Resources/Plants/PLT-STD-001.asset` - Pianta Standard (drift pH = 0)
- `Assets/Resources/Plants/PLT-PURE-001.asset` - Pianta Pure (drift pH = +2)
- `Assets/Resources/Plants/PLT-EVIL-001.asset` - Pianta Evil (drift pH = -2)

### Come Usarli:

1. **Apri Unity Editor**
2. **Vai in Project Window:**
   - Naviga a `Assets/Resources/Plants/`
   - Dovresti vedere i 3 file PlantData

3. **Configura PlantData:**
   - Seleziona `PLT-STD-001` (o altri)
   - Nell'Inspector, verifica:
     - **Plant Code:** PLT-STD-001
     - **Seed Item Config:** Deve essere assegnato a `seed-001`
     - **Family:** Standard (0) / Pure (1) / Evil (2)
     - **Daily Ph Drift:** 0 / +2 / -2

4. **IMPORTANTE - Assegnare Seed Item Config:**
   - Se `Seed Item Config` è NULL, trascina `seed-001` dalla cartella `Assets/Resources/Items/`
   - Questo collega il PlantData al seme nell'inventario

---

## ✅ Soluzione 3: Creare PlantData Manualmente

Se preferisci crearne di nuovi:

### Metodo A: Tramite Script (Avanzato)
```csharp
// In Unity Editor, crea uno script temporaneo:
[MenuItem("Tools/Create PlantData Example")]
static void CreatePlantData()
{
    var plantData = ScriptableObject.CreateInstance<PlantData>();
    plantData.name = "PLT-STD-001";
    // Configura proprietà...
    AssetDatabase.CreateAsset(plantData, "Assets/Resources/Plants/PLT-STD-001.asset");
}
```

### Metodo B: Duplicare Esistente
1. Seleziona un PlantData esistente (es. `PLT-STD-001`)
2. `Ctrl+D` per duplicare
3. Rinomina e modifica proprietà nell'Inspector

---

## 🔧 Setup PlantDatabase

Dopo aver creato/configurato i PlantData:

1. **Aggiungi PlantDatabase alla scena:**
   - Crea GameObject vuoto: `GameObject > Create Empty`
   - Rinomina: `PlantDatabase`
   - Aggiungi componente: `Add Component > Plant Database`

2. **Configura PlantDatabase:**
   - Nell'Inspector, vedrai lista `All Plant Data`
   - **OPZIONE 1:** Trascina i PlantData dalla cartella `Resources/Plants/` nella lista
   - **OPZIONE 2:** Lascia vuoto - il sistema caricherà automaticamente da `Resources/Plants/`

3. **Verifica:**
   - In Play Mode, il PlantDatabase caricherà automaticamente tutti i PlantData da `Resources/Plants/`
   - Verifica log: `[PlantDatabase] Caricati X PlantData da Resources/Plants/`

---

## 🧪 Test Rapido

1. **Setup:**
   - PlantDatabase presente nella scena
   - Almeno un PlantData configurato con `Seed Item Config` assegnato

2. **In Play Mode:**
   - Piantare seme `seed-001`
   - Verifica log: `[PotActions] PlantData trovato: PLT-STD-001...`
   - Eseguire End Day
   - Verifica log: `[DayCycleController] pH Drift totale...`
   - Verifica pH HUD si aggiorna

---

## ⚠️ Note Importanti

### Seed Item Config Deve Essere Assegnato
- **CRITICO:** Ogni PlantData deve avere `Seed Item Config` assegnato
- Questo collega il PlantData al seme nell'inventario
- Senza questo, il sistema non troverà il PlantData quando pianti il seme

### TypeId Deve Corrispondere
- Il `TypeId` del `Seed Item Config` deve corrispondere al seme nell'inventario
- Esempio: se `seed-001` ha `TypeId = "seed-001"`, il PlantData deve referenziare quel seed

### Cartella Resources/Plants/
- Il PlantDatabase carica automaticamente da `Resources/Plants/`
- Assicurati che i PlantData siano salvati lì
- Oppure assegnali manualmente nella lista `All Plant Data` del PlantDatabase

---

## 📝 Esempio Configurazione Completa

### PlantData per seed-001 (Standard):
```
Plant Code: PLT-STD-001
Seed Item Config: seed-001 (trascinato da Resources/Items/)
Family: Standard (0)
Daily Ph Drift: 0
Optimal Ph Min: -29
Optimal Ph Max: 29
```

### PlantData per seed-001 (Pure):
```
Plant Code: PLT-PURE-001
Seed Item Config: seed-001 (stesso seed, ma famiglia diversa)
Family: Pure (1)
Daily Ph Drift: 2
Optimal Ph Min: -29
Optimal Ph Max: 79
```

**Nota:** Puoi avere più PlantData per lo stesso seme se vuoi varianti diverse!

---

## 🎯 Risoluzione Problemi

### Menu non appare ancora?
1. Chiudi e riapri Unity
2. Verifica che `PlantData.cs` sia nella cartella corretta
3. Verifica che non ci siano errori di compilazione

### PlantData non trovato quando pianti seme?
1. Verifica che `Seed Item Config` sia assegnato
2. Verifica che `TypeId` del seed corrisponda
3. Verifica log: `[PotActions] PlantData trovato...` o warning se non trovato

### pH non cambia dopo End Day?
1. Verifica che PhSystem sia registrato nel ServiceContainer
2. Verifica che PlantDatabase sia presente nella scena
3. Verifica log: `[DayCycleController] pH Drift totale...`

---

**Fine Documento**

