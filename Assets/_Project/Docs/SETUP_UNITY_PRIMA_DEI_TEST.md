# Configurazione Unity prima di testare (Lab + Dimenticanze)

Segui questi passi **in ordine** prima di avviare i test step-by-step.

---

## 1. Creare gli ItemConfig per Lab (CELL, RES-PROT, Reagenti)

1. Apri Unity e il progetto.
2. Menu: **Tools > Sporae > Create Lab ItemConfig Assets (CELL, RES-PROT, REAG)**.
3. Verifica che in **Assets/_Project/Resources/Items/** (o **Assets/Resources/Items/**) siano stati creati:
   - `CELL-001.asset`, `CELL-002.asset`, `CELL-003.asset`
   - `RES-PROT-001.asset`
   - `REAG-X.asset`, `REAG-Y.asset`
4. Se il menu non crea gli asset, creali manualmente: **Assets > Create > Game > ItemData**, rinomina il file come sopra e imposta **Type Id** uguale al nome del file (es. `CELL-001`).

---

## 2. Creare LabUpgradesConfig (modulo Cellule Staminali)

1. **Assets > Create > Game > Lab Upgrades Config**.
2. Salva l’asset in **Assets/_Project/Resources/** con nome **LabUpgradesConfig** (così viene caricato da `Resources.Load<LabUpgradesConfig>("LabUpgradesConfig")`).
3. Nell’Inspector:
   - **Has Stem Cell Module**: spunta per abilitare il modulo Cellule Staminali sull’Extractor (frutto → spore + CELL-002; pianta/residui → CELL-001; RES-PROT-001 → CELL-003).

---

## 3. Pannelli Lab (Foundation UIToolkit)

Per ogni macchinario (Extractor, Catalizzatore, Pipette, Incubator):

1. Crea un **GameObject** (es. `UI_LabExtractorPanel`).
2. Aggiungi **UIDocument** e il controller corrispondente:
   - Extractor: **LabExtractorPanelController**
   - Catalizzatore: **LabCatalizzatorePanelController**
   - Fusione (Pipette): **LabFusionPanelController**
   - Incubatore: **LabIncubatorPanelController**
3. Sul **UIDocument**:
   - **Source Asset**: assegna il file `.uxml` del pannello (es. `LabExtractorPanel.uxml` da `Assets/_Project/UI/UIToolkit/Lab/`).
4. Sul **controller**:
   - **Extractor**: assegna il riferimento all’**Extractor** (il componente sull’oggetto del macchinario).
   - **Catalizzatore**: assegna il **Catalizzatore**.
   - **Fusion**: assegna la **Pipette**.
   - **Incubator**: nessun riferimento obbligatorio (usa inventario player).
   - **Lab Extractor Panel**: opzionale **Lab Upgrades Config** (se non assegnato, viene caricato da `Resources/LabUpgradesConfig`).
5. Sui **GameObject dei macchinari** (quelli con **Interactable**):
   - **Extractor**: nel componente Extractor, assegna **Lab Extractor Panel** al `LabExtractorPanelController` creato sopra (così si apre il pannello UIToolkit invece del legacy).
   - **Catalizzatore**: assegna **Lab Catalizzatore Panel** al `LabCatalizzatorePanelController`.
   - **Pipette**: assegna **Lab Fusion Panel** al `LabFusionPanelController`.
   - **Incubator**: aggiungi il componente **Incubator** (script in `Interactables/Incubator.cs`) e assegna **Lab Incubator Panel** al `LabIncubatorPanelController`.

---

## 4. Console debug inventario (GlobalStateInspector)

1. Assicurati che **GlobalStateInspector** sia presente in scena (o attivo nel prefab di debug).
2. In Play, apri la console con **F1** (o il tasto configurato).
3. Nella sezione **Inventory**: oltre a +1/-1 sugli slot esistenti, trovi la griglia di **typeId** (Items.*) e il campo **Aggiungi item (typeId)** con pulsante **Aggiungi 1** per aggiungere a runtime qualsiasi item anche se non è ancora in inventario.

---

## 5. Salvataggio e versione inventario

- **SaveManager** salva sempre **inventoryVersion = 1**.
- I save **vecchi** (senza `inventoryVersion` o con valore &lt; 1) al caricamento trattano le spore senza metadata come **Raw + STABLE** (fallback).
- Non serve alcuna azione in Unity: il comportamento è già gestito dal codice.

---

## 6. Ordine consigliato per i test

1. **ItemConfig e LabUpgradesConfig**: crea gli asset (passi 1 e 2) e fai un Play per verificare che non ci siano errori di caricamento (es. `Resources/Items/CELL-001`).
2. **Extractor**: configura il pannello UIToolkit e il riferimento sull’Extractor; interagisci con il macchinario e verifica input (frutto), Avvia, output (spore), Ritira e toast.
3. **Modulo Cellule Staminali**: con **Has Stem Cell Module** attivo, inserisci nell’Extractor frutto / pianta o residui / RES-PROT-001 (usa la console debug per aggiungere item) e verifica output spore+CELL-002, CELL-001, CELL-003.
4. **Catalizzatore**: pannello, Avvia maturazione, “Operazione in corso”, Ritira spora maturata dopo 2 giorni.
5. **Fusione**: inserisci 2 spore nello storage Pipette, Conferma fusione, Ritira Pre-Seed.
6. **Incubatore**: Pre-Seed + reagente (Nessuno/X/Y), Avvia incubazione, giorno successivo Ritira seme.
7. **Harvest e metadata frutto**: pianta un seme, raccogli frutti e verifica (in futuro in UI) che i frutti abbiano metadata GeneticType/Family/SourcePlantCode da `PotStateModel`.
8. **Save/Load**: salva, esci, ricarica e verifica che l’inventario (e le spore con fallback Raw+STABLE per save vecchi) sia corretto.

---

## Riferimenti file

- Pannelli Lab: `Assets/_Project/UI/UIToolkit/Lab/` (UXML, USS, README_LAB_PANELS.md).
- Controller: `Assets/_Project/Scripts/UI/UIToolkit/Lab/`.
- ItemConfig path usato da gioco: `Resources/Items/{typeId}` (es. `Resources/Items/CELL-001`).
- LabUpgradesConfig: `Resources/LabUpgradesConfig`.
