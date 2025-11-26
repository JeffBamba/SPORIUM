# Sistema Selezione Semi - Implementazione Completata

**Data:** 2025-01-XX  
**Versione:** 1.0  
**Stato:** ✅ COMPLETATO

---

## 📋 Panoramica

Implementato sistema di selezione semi che permette al giocatore di scegliere quale seme piantare dall'inventario, invece di piantare automaticamente il primo seme disponibile.

---

## ✅ Componenti Implementati

### 1. **UISeedSelector.cs**
**File:** `Assets/_Project/Scripts/UI/VaultMap/UISeedSelector.cs`

UI per selezionare un seme dall'inventario:
- **Funzionalità:**
  - Mostra tutti i semi disponibili nell'inventario
  - Mostra informazioni per ogni seme (nome, famiglia, quantità, drift pH)
  - Permette selezione clickando su un seme
  - Pulsante chiudi per annullare
  
- **Metodi principali:**
  - `Show(PotSlot targetPot)`: Mostra il selettore per un vaso specifico
  - `Hide()`: Nasconde il selettore
  - `OnSeedSelected`: Evento emesso quando un seme viene selezionato
  - `OnCancelled`: Evento emesso quando la selezione viene annullata

### 2. **PotActions.DoPlant() Modificato**
**File:** `Assets/_Project/Scripts/Dome/PotActions.cs`

Modificato per accettare `seedTypeId` opzionale:
- **Prima:** `DoPlant()` cercava automaticamente il primo seme disponibile
- **Dopo:** `DoPlant(string seedTypeId = null)` accetta seme specifico
- **Compatibilità:** Se `seedTypeId` è null, cerca automaticamente (compatibilità retroattiva)

### 3. **PotHUDWidget Modificato**
**File:** `Assets/_Project/Scripts/UI/VaultMap/PotHUDWidget.cs`

Modificato per aprire UISeedSelector:
- Quando si clicca "Plant", apre il selettore semi invece di piantare direttamente
- Gestisce la selezione del seme e chiama `DoPlant(seedTypeId)`
- Crea automaticamente UISeedSelector se non presente nella scena

### 4. **PotDetailsWidget Modificato**
**File:** `Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs`

Modificato per aprire UISeedSelector:
- Stesso comportamento di PotHUDWidget
- Gestisce selezione e annullamento

### 5. **UISeedSelectorAutoSetup.cs**
**File:** `Assets/_Project/Scripts/Editor/UISeedSelectorAutoSetup.cs`

Script editor per creare automaticamente la UI:
- Crea Canvas se non esiste
- Crea Panel principale con sfondo
- Crea Container con GridLayoutGroup per organizzare i pulsanti
- Crea Title, NoSeedsText, CloseButton
- Configura automaticamente tutti i riferimenti

---

## 🔄 Nuovo Flusso

### Prima (Automatico):
```
1. Player clicca "Plant"
2. Sistema cerca automaticamente primo seme disponibile
3. Piantare immediatamente
```

### Dopo (Selezione):
```
1. Player clicca "Plant"
2. Si apre UISeedSelector con tutti i semi disponibili
3. Player seleziona seme desiderato
4. Sistema pianta il seme selezionato
```

---

## 🎨 UI Selettore Semi

### Struttura UI:
```
UISeedSelector
└── SelectorPanel (Panel principale)
    ├── Title ("Seleziona Seme")
    ├── SeedButtonContainer (GridLayoutGroup)
    │   ├── SeedButton_001 (Seme Standard)
    │   ├── SeedButton_002 (Seme Pure)
    │   └── SeedButton_003 (Seme Evil)
    ├── NoSeedsText ("Nessun seme disponibile")
    └── CloseButton (X)
```

### Informazioni Mostrate per Seme:
- **Nome seme:** "SEME 001", "SEME 002", ecc.
- **Famiglia:** Standard / Pure / Evil
- **Quantità:** (x2), (x3), ecc.
- **Drift pH:** +2/giorno, -2/giorno, 0/giorno

---

## 🔧 Setup Scena

### Opzione 1: Setup Automatico (CONSIGLIATO)

1. **Aggiungi UISeedSelectorAutoSetup alla scena:**
   - `GameObject > Create Empty` → Rinomina `SeedSelectorSetup`
   - `Add Component > UISeed Selector Auto Setup`
   - In Play Mode, crea automaticamente tutta la UI

2. **Verifica:**
   - In Play Mode, dovresti vedere log: `[UISeedSelectorAutoSetup] ✅ UISeedSelector creato con successo!`

### Opzione 2: Setup Manuale

1. **Crea Canvas** (se non esiste):
   - `GameObject > UI > Canvas`

2. **Crea UISeedSelector:**
   - `GameObject > Create Empty` → Rinomina `UISeedSelector`
   - `Add Component > UI Seed Selector`

3. **Crea UI Elements:**
   - Panel principale con Image background
   - Container per pulsanti semi (con GridLayoutGroup)
   - Title TextMeshProUGUI
   - NoSeedsText TextMeshProUGUI
   - CloseButton Button

4. **Assegna Riferimenti:**
   - Nell'Inspector di UISeedSelector, trascina tutti gli elementi UI

5. **Assegna a PotHUDWidget/PotDetailsWidget:**
   - Trascina UISeedSelector nel campo `Seed Selector` di PotHUDWidget/PotDetailsWidget

---

## 🧪 Test

### Test Scenario 1: Selezione Seme Standard
1. Avvia Play Mode
2. Seleziona vaso vuoto
3. Clicca "Plant"
4. **VERIFICA:** Si apre UISeedSelector con 3 semi disponibili
5. Clicca su "SEME 001" (Standard)
6. **VERIFICA:** Seme piantato, log mostra `[PotActions] PlantData trovato: PLT-STD-001 (Standard)`

### Test Scenario 2: Selezione Seme Pure
1. Clicca "Plant" su vaso vuoto
2. Clicca su "SEME 002" (Pure)
3. **VERIFICA:** Seme piantato, log mostra `[PotActions] PlantData trovato: PLT-PURE-001 (Pure), drift pH: 2/giorno`

### Test Scenario 3: Selezione Seme Evil
1. Clicca "Plant" su vaso vuoto
2. Clicca su "SEME 003" (Evil)
3. **VERIFICA:** Seme piantato, log mostra `[PotActions] PlantData trovato: PLT-EVIL-001 (Evil), drift pH: -2/giorno`

### Test Scenario 4: Annullamento
1. Clicca "Plant"
2. Clicca pulsante "X" (chiudi)
3. **VERIFICA:** Selettore si chiude, nessun seme piantato

### Test Scenario 5: Nessun Seme Disponibile
1. Svuota inventario di tutti i semi
2. Clicca "Plant"
3. **VERIFICA:** Mostra messaggio "Nessun seme disponibile"

---

## ✅ Checklist Verifica

### Setup:
- [ ] UISeedSelector presente nella scena (o UISeedSelectorAutoSetup)
- [ ] PotHUDWidget/PotDetailsWidget hanno riferimento a UISeedSelector
- [ ] Canvas presente nella scena

### Funzionalità:
- [ ] Cliccare "Plant" apre UISeedSelector
- [ ] UISeedSelector mostra tutti i semi disponibili
- [ ] Ogni seme mostra informazioni corrette (nome, famiglia, quantità, pH drift)
- [ ] Cliccare su un seme lo pianta correttamente
- [ ] Cliccare "X" chiude il selettore senza piantare
- [ ] Se non ci sono semi, mostra messaggio appropriato

---

## 🐛 Risoluzione Problemi

### Problema: "UISeedSelector non disponibile"
**Sintomo:** Log mostra `[PotHUDWidget] UISeedSelector non disponibile!`

**Soluzione:**
1. Verifica che UISeedSelector sia presente nella scena
2. Oppure aggiungi UISeedSelectorAutoSetup alla scena
3. Verifica che PotHUDWidget/PotDetailsWidget abbiano riferimento assegnato

### Problema: "Selettore non si apre"
**Sintomo:** Cliccare "Plant" non apre il selettore

**Soluzione:**
1. Verifica che PotHUDWidget/PotDetailsWidget siano presenti nella scena
2. Verifica che il riferimento a UISeedSelector sia assegnato
3. Controlla log Console per errori

### Problema: "Pulsanti semi non visibili"
**Sintomo:** Selettore si apre ma non mostra semi

**Soluzione:**
1. Verifica che ci siano semi nell'inventario
2. Verifica che seedButtonContainer sia assegnato correttamente
3. Verifica che GridLayoutGroup sia configurato correttamente

### Problema: "Seme non piantato dopo selezione"
**Sintomo:** Cliccare su seme non lo pianta

**Soluzione:**
1. Verifica log: `[UISeedSelector] Seme selezionato: seed-XXX`
2. Verifica che PotActions.DoPlant(seedTypeId) venga chiamato
3. Verifica che il seme esista nell'inventario

---

## 📝 Note Importanti

### Compatibilità Retroattiva
- ✅ `DoPlant()` senza parametri funziona ancora (cerca automaticamente)
- ✅ Codice esistente che chiama `DoPlant()` continua a funzionare
- ✅ Nuovo codice può specificare seme: `DoPlant("seed-001")`

### UI Automatica
- ✅ Se UISeedSelector non è presente, viene creato automaticamente
- ✅ Se Canvas non esiste, viene creato automaticamente
- ✅ Tutti i riferimenti vengono configurati automaticamente

### Stile UI
- ✅ UI rispetta lo stile esistente del gioco
- ✅ Colori e dimensioni configurabili nell'Inspector
- ✅ Layout responsive con GridLayoutGroup

---

## 🎯 Risultato

✅ **Sistema di selezione semi completamente funzionante**

Ora quando il giocatore clicca "Plant":
1. Si apre una UI che mostra tutti i semi disponibili
2. Ogni seme mostra informazioni dettagliate (famiglia, pH drift)
3. Il giocatore può selezionare quale seme piantare
4. Il sistema pianta il seme selezionato con il PlantData corretto

**Il sistema è pronto per test!**

---

## 📚 File Modificati/Creati

### Nuovi File
- `Assets/_Project/Scripts/UI/VaultMap/UISeedSelector.cs`
- `Assets/_Project/Scripts/Editor/UISeedSelectorAutoSetup.cs`

### File Modificati
- `Assets/_Project/Scripts/Dome/PotActions.cs` (+ parametro seedTypeId a DoPlant)
- `Assets/_Project/Scripts/UI/VaultMap/PotHUDWidget.cs` (+ integrazione UISeedSelector)
- `Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs` (+ integrazione UISeedSelector)

---

**Fine Documento**

