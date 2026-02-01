---
name: Lab GDD42 Dimenticanze
overview: "Integrazione nel piano Lab/GDD 42 delle quattro dimenticanze: modulo Cellule Staminali per l'Extractor, item inventario e console debug per tutti gli input/output (inclusi reagenti), metadata frutto a harvest in Fase 0, e versioning save per fallback spore."
todos: []
isProject: false
---

# Piano: Dimenticanze Lab GDD 42

Integrazione delle quattro dimenticanze nel piano di allineamento Botanical Lab / GDD 42 (Fase 0–6, singolo item Spore con metadata, 4 step: Estrazione, Maturazione, Fusione, Incubatore).

---

## 1. Modulo Cellule Staminali (Extractor)

**Contesto:** Il modulo "Cellule Staminali" è acquistabile (mercato nero, fazioni o ricompensa Seed Spore Extractor) e aggiunge funzioni al macchinario oltre all’estrazione spore dai frutti.

**Comportamento:**


| Input                               | Output                         |
| ----------------------------------- | ------------------------------ |
| Frutto                              | Spore + **CELL-002** (fungine) |
| Pianta o residui organici           | **CELL-001** (vegetali)        |
| Residui proteici (**RES-PROT-001**) | **CELL-003** (animali)         |


**Implementazione:**

- Aggiungere in [Items.cs](Assets/_Project/Scripts/Core/ItemsSystem/Items.cs) le costanti mancanti (se non già presenti da Food Room): `CELL-001`, `CELL-002`, `CELL-003`, `RES-PROT-001` (residui proteici).
- Creare **ItemConfig** (asset in `Resources/Items/`) per ogni codice: CELL-001, CELL-002, CELL-003, RES-PROT-001.
- Nel flusso dell’Extractor (o componente che sostituirà [LabMinigameExtractor.cs](Assets/_Project/Scripts/UI/VaultMap/LabMinigameExtractor.cs)): se il modulo Cellule Staminali è installato, in base al tipo di input (frutto / pianta o residui organici / RES-PROT-001) produrre oltre alle spore (da frutto) anche l’output cellula corrispondente e aggiungerlo all’inventario.
- Modello “modulo acquistato”: flag o stato (es. da GameManager/UpgradeSystem o ScriptableObject) che indica se il modulo Cellule Staminali è attivo; da usare in fase di implementazione Lab.

**Fase suggerita:** Fase 5c (dopo Compost) o integrazione in Fase 1/2 se l’Extractor viene rifatto subito.

---

## 2. Item da inventario e console debug

**Regola:** Per ogni input/output citato nel piano (reagenti, cellule, residui, spore, semi, ecc.) devono esistere **oggetti Item da inventario** (ItemConfig + eventuale costante in `Items.cs`). Se un codice non esiste in game, va creato.

**Cosa fare:**

- **Items e ItemConfig:** Creare/verificare item per: CELL-001, CELL-002, CELL-003, RES-PROT-001, Reagente X, Reagente Y (e qualsiasi altro codice usato da Lab/Compost/Food Room). Tutti devono avere un asset ItemConfig in `Resources/Items/` (o path usato da [ItemFabric](Assets/_Project/Scripts/Core/ItemsSystem/ItemFabric.cs): `Resources.Load<ItemConfig>("Items/" + typeId)`).
- **Console debug inventario:** La sezione inventario in [GlobalStateInspector.cs](Assets/_Project/Scripts/DevTools/Inspector/GlobalStateInspector.cs) (righe ~430–476) oggi permette solo **+1/-1 sugli slot già presenti** (`inventory.Items`). Va estesa per consentire di **aggiungere a runtime un item per typeId** anche quando non è ancora in inventario (es. dropdown o campo testo con typeId + pulsante “Aggiungi”, popolato da tutti gli ItemConfig disponibili o da una lista di costanti `Items.*`). In questo modo si possono testare reagenti, cellule, RES-PROT-001, ecc.
- **Reagenti (Reagente X / Y):**
  - Acquisizione: mercato nero, fazioni, e in seguito modulo **“Reagenti Sintetici”** per il macchinario Compost (LAB-CMP-001).
  - Per ora: **placeholder** — i reagenti sono **solo acquistabili** (mercato nero / fazioni); la produzione tramite modulo Compost non va implementata ancora, ma il modulo va citato in design e placeholder (es. “Reagenti Sintetici” per Compost da implementare in seguito).

---

## 3. Fase 0 — Metadata su Item (frutto a harvest)

Aggiungere esplicitamente nel bullet di Fase 0:

- **Frutto:** stesso pattern metadata su `Item` usato per le spore, ma **solo per `Items.Fruits**` al momento del harvest. I metadata (es. GeneticType, Family, SourcePlantCode) vanno **valorizzati in `DoHarvest**` da [PotActions.cs](Assets/_Project/Scripts/Dome/PotActions.cs) leggendo da `PotStateModel` (stesso modello che in Fase 0 si usa per il seed/pianta). Così il frutto raccolto porta le informazioni genetiche della pianta e l’Extractor può ereditarle per la spore.

Riferimento: [PotActions.DoHarvest](Assets/_Project/Scripts/Dome/PotActions.cs) e [PotStateModel](Assets/_Project/Scripts/Dome/PotStateModel.cs).

---

## 4. SaveManager — Versione formato save e fallback spore

**Obiettivo:** Distinguere save vecchi da save nuovi e applicare **fallback per le spore** (es. considerare spore senza metadata come Raw + STABLE).

**Modifiche in [SaveManager.cs](Assets/_Project/Scripts/Core/SaveManager.cs):**

- Introdurre un campo di **versione formato save** dedicato all’inventario/dati lab, ad es.:
  - `inventoryVersion` (int o string) in [GameSaveData](Assets/_Project/Scripts/Core/SaveManager.cs) (classe privata ~488–499), **oppure**
  - uso esplicito di `gameVersion` (già presente) con valori/range noti per “formato con metadata spore”.
- In **DeserializeInventory** (e in qualsiasi deserializzazione slot spore con metadata): se il save è “vecchio” (assenza di `inventoryVersion` o valore inferiore a una soglia), applicare **fallback**: spore senza metadata vengono trattate come **Raw + STABLE** (o come definito dal GDD).
- Salvataggio: in `CollectSaveData`, impostare sempre la versione scelta (es. `inventoryVersion = 1` per il nuovo formato con metadata spore/frutto).

In questo modo i save esistenti continuano a caricarsi e le spore legacy ricevono un tipo genetico e stage coerente senza richiedere migrazione dati complessa.

---

## Riepilogo file toccati


| Area                     | File                                                                                                                                                                                                              |
| ------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Modulo Cellule Staminali | [Items.cs](Assets/_Project/Scripts/Core/ItemsSystem/Items.cs), ItemConfig assets, logica Extractor (LabMinigameExtractor / nuovo componente)                                                                      |
| Item e debug             | [Items.cs](Assets/_Project/Scripts/Core/ItemsSystem/Items.cs), ItemConfig per CELL-xxx, RES-PROT-001, Reagenti X/Y; [GlobalStateInspector.cs](Assets/_Project/Scripts/DevTools/Inspector/GlobalStateInspector.cs) |
| Frutto metadata          | [PotActions.cs](Assets/_Project/Scripts/Dome/PotActions.cs) (`DoHarvest`), [PotStateModel.cs](Assets/_Project/Scripts/Dome/PotStateModel.cs)                                                                      |
| Save e fallback          | [SaveManager.cs](Assets/_Project/Scripts/Core/SaveManager.cs) (`GameSaveData`, `DeserializeInventory`, `CollectSaveData`)                                                                                         |


---

## Ordine consigliato

1. **Fase 0:** Metadata frutto in `DoHarvest` + versioning save e fallback spore (punti 3 e 4).
2. **Item e console:** Creare tutti gli item mancanti e estendere GlobalStateInspector (punto 2).
3. **Modulo Cellule Staminali:** Dopo o insieme al redesign Estrazione (punto 1); reagenti come placeholder acquistabili (punto 2).

Questo piano si integra con il piano esistente Food Room / Lab (es. [food_room_player_hydration_inventory_consumption.plan.md](.cursor/plans/food_room_player_hydration_inventory_consumption.plan.md)) dove sono già citati CELL-001/002/003 e ORG-RES-001; qui si aggiungono RES-PROT-001, Reagenti X/Y, modulo Cellule Staminali sull’Extractor, console debug e versioning save.