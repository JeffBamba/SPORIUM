# Guida Test: Selezione Semi da Inventario

## Setup Automatico (Consigliato)

Lo script `UISeedSelectorAutoSetup` crea automaticamente la UI del selettore semi all'avvio del Play Mode.

### Passo 1: Aggiungi lo Script AutoSetup alla Scena

1. Apri la scena principale (es. `SCN_Bootstrap` o la scena del Dome)
2. Nella Hierarchy, crea un nuovo GameObject vuoto:
   - Click destro nella Hierarchy → `Create Empty`
   - Rinominalo: `SeedSelectorAutoSetup`
3. Aggiungi il componente `UISeedSelectorAutoSetup`:
   - Seleziona `SeedSelectorAutoSetup`
   - Inspector → `Add Component`
   - Cerca: `UISeedSelector Auto Setup`
   - Aggiungi il componente

### Passo 2: Verifica le Impostazioni

Nell'Inspector di `UISeedSelectorAutoSetup`, verifica:
- ✅ **Create On Start**: deve essere `true` (default)
- ✅ **Show Debug Logs**: può essere `true` per vedere i log

### Passo 3: Avvia Play Mode

1. Premi **Play** in Unity
2. Controlla la Console per vedere:
   ```
   [UISeedSelectorAutoSetup] ✅ UISeedSelector creato con successo!
   ```

---

## Setup Manuale (Alternativa)

Se preferisci configurare manualmente:

1. Crea un GameObject `UISeedSelector` nella Hierarchy
2. Aggiungi il componente `UISeedSelector`
3. Crea la struttura UI manualmente (Panel, Container, Button, ecc.)
4. Assegna i riferimenti nell'Inspector

**Nota**: Il sistema cerca automaticamente `UISeedSelector` nella scena, quindi se esiste già, lo userà.

---

## Come Testare la Funzionalità

### Prerequisiti

Assicurati di avere semi nell'inventario:
- `seed-001` (Standard)
- `seed-002` (Pure)  
- `seed-003` (Evil)

Questi vengono aggiunti automaticamente all'inventario iniziale da `GameManager.cs`.

### Test Step-by-Step

1. **Avvia il gioco** (Play Mode)

2. **Vai nella stanza Dome** dove ci sono i vasi

3. **Seleziona un vaso vuoto**:
   - Click sul vaso
   - Dovresti vedere il widget HUD del vaso

4. **Clicca sul pulsante "Plant"**:
   - Si apre il pannello `UISeedSelector`
   - Dovresti vedere i 3 semi disponibili:
     - **Seme 001** (Standard, pH: 0/giorno)
     - **Seme 002** (Pure, pH: +2/giorno)
     - **Seme 003** (Evil, pH: -2/giorno)

5. **Seleziona un seme**:
   - Click su uno dei semi nel pannello
   - Il pannello si chiude automaticamente
   - Il seme viene piantato nel vaso selezionato

6. **Verifica il risultato**:
   - Il vaso ora contiene la pianta corrispondente al seme selezionato
   - Controlla la Console per i log di conferma:
     ```
     [UISeedSelector] Seme selezionato: seed-001
     [PotHUDWidget] Piantando seme seed-001 nel vaso POT-001
     [PotActions] PlantData trovato: PLT-STD-001 (Standard), drift pH: 0/giorno
     ```

### Test con pH System

Per verificare che il pH drift funzioni:

1. **Piantare una pianta Pure** (`seed-002`):
   - Dovrebbe aumentare il pH di +2/giorno

2. **Piantare una pianta Evil** (`seed-003`):
   - Dovrebbe diminuire il pH di -2/giorno

3. **Piantare una pianta Standard** (`seed-001`):
   - Non dovrebbe modificare il pH (drift = 0)

4. **Avanzare il giorno** (End Day):
   - Il pH dovrebbe cambiare in base alle piante piantate
   - Controlla la Console per vedere il calcolo del drift:
     ```
     [DayCycleController] pH Drift totale da 1 piante: +2.00
     ```

---

## Troubleshooting

### Il pannello non si apre quando clicco "Plant"

**Possibili cause:**
- `UISeedSelector` non esiste nella scena
- `PotHUDWidget` non trova `UISeedSelector`

**Soluzione:**
- Aggiungi `UISeedSelectorAutoSetup` alla scena (vedi Setup Automatico sopra)
- Oppure verifica che `UISeedSelector` esista nella Hierarchy

### Non vedo i semi nel pannello

**Possibili cause:**
- Non hai semi nell'inventario
- `GameManager` non ha aggiunto i semi iniziali

**Soluzione:**
- Verifica che `GameManager.cs` aggiunga i semi all'inventario iniziale
- Controlla la Console per errori relativi all'inventario

### Il seme viene piantato ma non vedo PlantData

**Possibili cause:**
- `PlantDatabase` non è inizializzato
- I `PlantData` assets non sono nella cartella `Resources/Plants`

**Soluzione:**
- Verifica che `PlantDatabase.Instance` esista nella scena
- Controlla che i file `.asset` siano in `Assets/Resources/Plants/`:
  - `PLT-STD-001.asset`
  - `PLT-PURE-001.asset`
  - `PLT-EVIL-001.asset`

### Errore: "UISeedSelector non trovato"

**Soluzione:**
- Aggiungi `UISeedSelectorAutoSetup` alla scena
- Oppure crea manualmente `UISeedSelector` nella Hierarchy

---

## Struttura UI Creata Automaticamente

Quando `UISeedSelectorAutoSetup` viene eseguito, crea:

```
Canvas (se non esiste)
└── UISeedSelector
    └── SelectorPanel
        ├── Title (TextMeshProUGUI)
        ├── CloseButton (Button)
        ├── SeedButtonContainer (GridLayoutGroup)
        │   └── [Seed Buttons creati dinamicamente]
        └── NoSeedsText (TextMeshProUGUI)
```

---

## Note Tecniche

- `UISeedSelector` viene cercato automaticamente da `PotHUDWidget` e `PotDetailsWidget`
- Se non esiste, viene creato automaticamente (ma senza UI completa)
- Lo script `UISeedSelectorAutoSetup` crea la UI completa automaticamente
- I semi vengono mostrati solo se presenti nell'inventario del giocatore
- Ogni seme mostra: nome, famiglia, quantità, drift pH giornaliero

