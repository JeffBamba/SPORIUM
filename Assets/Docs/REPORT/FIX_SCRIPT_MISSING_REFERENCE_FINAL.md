# FIX: Script Missing Reference - Guida Completa

**Data:** 2025-12-09  
**Problema:** Riferimento a script mancante nella scena Unity

---

## 🔴 PROBLEMA PRINCIPALE

**"The referenced script (Unknown) on this Behaviour is missing!"**

Questo errore indica che nella scena Unity c'è ancora un riferimento a `WateringMinigame` che abbiamo rimosso.

---

## ✅ SOLUZIONE STEP-BY-STEP

### **STEP 1: Rimuovere Riferimento Script Mancante**

1. **Apri Unity Editor**
2. **Apri la scena principale** (es. `VaultMap` o la scena che stai usando)
3. **Seleziona il GameObject `UI_PotDetails`** nella Hierarchy
4. **Nel Inspector**, trova il componente `PotDetailsWidget`
5. **Cerca un campo che mostra:**
   - `Missing (Mono Script)` in rosso
   - `None (Mono Script)` con un'icona di warning
   - Un campo con nome simile a `_wateringMinigame` o `WateringMinigame`
6. **Se trovi un riferimento mancante:**
   - **Opzione A:** Clicca sul campo e seleziona **"Remove Component"** (se è un componente separato)
   - **Opzione B:** Imposta il campo a **None** (se è un campo SerializeField)
   - **Opzione C:** Se il campo è commentato nel codice ma Unity lo mostra ancora, salva la scena (Ctrl+S) e riapri Unity

### **STEP 2: Verifica Riferimenti Bottoni**

1. Nel componente `PotDetailsWidget` nell'Inspector, verifica che tutti i campi SerializeField siano assegnati:
   - ✅ `_plantButton` → Deve puntare al bottone "Plant"
   - ✅ `_wateringButton` → Deve puntare al bottone "Watering" (importante!)
   - ✅ `_blueLedButton` → Deve puntare al bottone Blue LED
   - ✅ `_redLedButton` → Deve puntare al bottone Red LED
   - ✅ `_sprayButton` → Deve puntare al bottone "Spray Antifungal"
   - ✅ `_harvestButton` → Deve puntare al bottone "Harvest"
   - ✅ `_uprootButton` → Deve puntare al bottone "Uproot"

2. **Se un bottone è None:**
   - Trova il bottone nella gerarchia (es. `UI_PotDetails/Panel/Bottom/Watering`)
   - Trascina il bottone dal **Hierarchy** al campo corrispondente nell'**Inspector**

### **STEP 3: Salva la Scena**

1. **Salva la scena** (Ctrl+S o File → Save)
2. **Chiudi e riapri Unity** (opzionale, ma aiuta a pulire i riferimenti)
3. **Riapri la scena** e verifica che l'errore sia scomparso

---

## ⚠️ WARNING NON CRITICI

### **1. PlantGrowthConfig non trovato**

**Messaggio:**
```
[PotDetailsWidget] PlantGrowthConfig non trovato in Resources/Configs/. Usando valori di default.
```

**Causa:** Il file di configurazione non esiste o non è nel percorso corretto.

**Soluzione (opzionale):**
1. Verifica che esista: `Assets/Resources/Configs/PlantGrowthConfig_Default.asset`
2. Se non esiste, il sistema usa valori di default (funziona comunque)
3. Se vuoi crearlo, usa il menu Unity: `Assets → Create → PlantGrowthConfig`

**Nota:** Questo warning non blocca il funzionamento del gioco.

---

### **2. PhSystem non trovato nel ServiceContainer**

**Messaggio:**
```
[ServiceContainer] Servizio di tipo _Project.PhSystem non trovato!
```

**Causa:** `PhSystem` viene registrato da `PhSystemDebugConsole` che potrebbe non essere ancora inizializzato quando `DayCycleController` cerca di accedervi.

**Soluzione:**
- Il sistema gestisce gracefully questo caso (usa `TryGetPhSystem()`)
- Il warning appare solo all'avvio, poi il sistema si collega automaticamente
- Se `PhSystemDebugConsole` è presente nella scena, il sistema si collegherà automaticamente

**Verifica:**
1. Controlla che `PhSystemDebugConsole` sia presente nella scena
2. Se manca, aggiungilo come GameObject nella scena
3. Il sistema funzionerà comunque, ma senza debug console

**Nota:** Questo warning non blocca il funzionamento del gioco. Il sistema overwatering funzionerà quando PhSystem sarà disponibile.

---

## 🧪 TEST FINALE

Dopo aver risolto i problemi:

1. **Avvia Play Mode**
2. **Verifica Console** - Non dovrebbero esserci errori rossi
3. **Seleziona un vaso** e verifica che:
   - Il bottone Watering mostri "Irrigazione OFF" o "Irrigazione ON"
   - Il toggle funzioni correttamente
   - Il toast mostri il messaggio corretto
4. **Testa overwatering:**
   - Attiva sistema irrigazione
   - Avvia End Day per 3-4 giorni
   - Verifica che l'idratazione aumenti
   - Verifica che il pH diminuisca quando idratazione >= 75%
   - Disattiva sistema e verifica che il pH venga corretto quando idratazione < 50%

---

## 📝 NOTE

- Il codice gestisce gracefully i riferimenti mancanti (non crasha più)
- I warning su PlantGrowthConfig e PhSystem sono non critici
- Il sistema funzionerà anche con questi warning, ma è meglio risolverli per un'esperienza pulita

---

## 🔍 DEBUG AGGIUNTIVO

Se il problema persiste:

1. **Controlla la Console** per messaggi di errore specifici
2. **Verifica che tutti i GameObject UI esistano** nella scena
3. **Controlla che i nomi dei GameObject corrispondano** a quelli cercati nel codice
4. **Verifica che il componente `PotDetailsWidget` sia presente** su `UI_PotDetails`
5. **Cerca nella scena** per riferimenti a "WateringMinigame" usando la ricerca Unity (Ctrl+F)

---

**Fix applicato:** 2025-12-09  
**Pronto per testing**

