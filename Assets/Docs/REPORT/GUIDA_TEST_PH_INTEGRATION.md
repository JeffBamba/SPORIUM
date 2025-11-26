# Guida Test Integrazione pH - 3 Piante Famiglie Diverse

**Obiettivo:** Verificare che le 3 piante (Standard, Pure, Evil) influenzino correttamente il pH della Dome.

---

## 📋 PREREQUISITI

### 1. PlantData Configurati
✅ I 3 PlantData sono già creati:
- `PLT-STD-001` (Standard, drift pH = 0)
- `PLT-PURE-001` (Pure, drift pH = +2)
- `PLT-EVIL-001` (Evil, drift pH = -2)

**VERIFICA:** 
- Cerca "PLT-STD-001" nel Project window
- Seleziona il file
- Nell'Inspector, verifica che **Seed Item Config** sia assegnato
- Se è NULL, trascina `seed-001` da `Assets/Resources/Items/seed-001`
- Ripeti per PLT-PURE-001 e PLT-EVIL-001

---

## 🔧 SETUP SCENA

### Step 1: Aggiungi PlantDatabase alla Scena

1. **Apri la scena principale** (es. RoomDome o scena di gioco)

2. **Crea GameObject:**
   - `GameObject > Create Empty`
   - Rinomina: `PlantDatabase`

3. **Aggiungi Componente:**
   - Seleziona `PlantDatabase`
   - `Add Component > Plant Database`
   - (Cerca "Plant Database" nella barra di ricerca)

4. **Configurazione (OPZIONALE):**
   - Nell'Inspector, vedrai lista `All Plant Data`
   - **OPZIONE A:** Lascia vuoto - il sistema caricherà automaticamente da `Resources/Plants/`
   - **OPZIONE B:** Trascina i 3 PlantData nella lista manualmente

### Step 2: Verifica PhSystem nella Scena

**Il PhSystem deve essere registrato nel ServiceContainer.**

**OPZIONE A - Se hai già PhSystemDebugConsole:**
- Se `PhSystemDebugConsole` è presente nella scena, PhSystem viene registrato automaticamente
- ✅ Niente da fare

**OPZIONE B - Se NON hai PhSystemDebugConsole:**
- Aggiungi `PhSystemAutoSetup` alla scena:
  - `GameObject > Create Empty`
  - Rinomina: `pH_SystemSetup`
  - `Add Component > Ph System Auto Setup`
  - Questo creerà automaticamente PhSystemDebugConsole e HUDPhDisplay

**OPZIONE C - Setup Manuale:**
- Crea GameObject `pH_DebugConsole`
- Aggiungi componente `Ph System Debug Console`
- Questo registrerà PhSystem nel ServiceContainer

### Step 3: Verifica HUD pH

**Per vedere il pH in-game:**

- Se `PhSystemAutoSetup` è presente, crea automaticamente HUDPhDisplay
- Oppure aggiungi manualmente:
  - Crea GameObject `pH_HUDDisplay`
  - Aggiungi componente `HUD Ph Display`
  - Il pH verrà mostrato in alto al centro dello schermo

---

## 🧪 TEST IN PLAY MODE

### Test Scenario 1: Pianta Pure (+2 pH/giorno)

1. **Avvia Play Mode**

2. **Verifica Setup:**
   - Console Unity dovrebbe mostrare: `[PlantDatabase] Inizializzato: X piante registrate`
   - Se vedi warning `[DayCycleController] PhSystem non trovato`, il PhSystem non è registrato

3. **Piantare Seme Pure:**
   - Vai al vaso nella scena
   - Piantare seme `seed-001` (se PLT-PURE-001 è collegato a seed-001)
   - **VERIFICA LOG:**
     ```
     [PotActions] PlantData trovato: PLT-PURE-001 (Pure), drift pH: 2/giorno
     [ACT-001][POT-XXX] Plant OK: seed planted, state=..., PlantData: PLT-PURE-001 (Pure)
     ```

4. **Verifica pH Iniziale:**
   - Guarda HUD pH in alto (dovrebbe mostrare pH = 0.0 o valore corrente)
   - Passa mouse sopra HUD pH per vedere tooltip con breakdown

5. **Esegui End Day:**
   - Vai in Bedroom o usa comando End Day
   - **VERIFICA LOG:**
     ```
     [DayCycleController] POT-XXX: PLT-PURE-001 (Pure) → drift pH: 2.00/giorno
     [DayCycleController] pH Drift totale da 1 piante: 2.00 → pH attuale: 2.00
     ```

6. **Verifica Risultato:**
   - HUD pH dovrebbe mostrare pH aumentato di +2
   - Tooltip pH dovrebbe mostrare "Piante: +2.00"

---

### Test Scenario 2: Pianta Evil (-2 pH/giorno)

1. **Piantare Seme Evil:**
   - Piantare seme collegato a PLT-EVIL-001
   - **VERIFICA LOG:**
     ```
     [PotActions] PlantData trovato: PLT-EVIL-001 (Evil), drift pH: -2/giorno
     ```

2. **Esegui End Day:**
   - **VERIFICA LOG:**
     ```
     [DayCycleController] POT-XXX: PLT-EVIL-001 (Evil) → drift pH: -2.00/giorno
     [DayCycleController] pH Drift totale da 1 piante: -2.00 → pH attuale: 0.00
     ```
   - (Se avevi pH = 2.00 dalla pianta Pure, ora dovrebbe essere 0.00)

3. **Verifica Risultato:**
   - HUD pH dovrebbe mostrare pH diminuito di -2
   - Tooltip pH dovrebbe mostrare "Piante: -2.00"

---

### Test Scenario 3: Pianta Standard (0 pH/giorno)

1. **Piantare Seme Standard:**
   - Piantare seme collegato a PLT-STD-001
   - **VERIFICA LOG:**
     ```
     [PotActions] PlantData trovato: PLT-STD-001 (Standard), drift pH: 0/giorno
     ```

2. **Esegui End Day:**
   - **VERIFICA LOG:**
     ```
     [DayCycleController] Nessun drift pH da X piante (tutte Standard o drift = 0)
     ```
   - Oppure se ci sono altre piante:
     ```
     [DayCycleController] pH Drift totale da X piante: Y.XX → pH attuale: Z.ZZ
     ```
   - (La pianta Standard non contribuisce al drift)

3. **Verifica Risultato:**
   - pH non dovrebbe cambiare per la pianta Standard
   - Tooltip pH non dovrebbe mostrare contributo dalla Standard

---

### Test Scenario 4: Piante Multiple (Bilanciamento)

**Test più completo:** Verificare che il drift pH totale sia la somma di tutte le piante.

1. **Setup:**
   - Piantare 2 piante Pure (+2 ciascuna = +4 totale)
   - Piantare 1 pianta Evil (-2 totale)
   - Piantare 1 pianta Standard (0 totale)

2. **Esegui End Day:**
   - **VERIFICA LOG:**
     ```
     [DayCycleController] POT-001: PLT-PURE-001 (Pure) → drift pH: 2.00/giorno
     [DayCycleController] POT-002: PLT-PURE-001 (Pure) → drift pH: 2.00/giorno
     [DayCycleController] POT-003: PLT-EVIL-001 (Evil) → drift pH: -2.00/giorno
     [DayCycleController] POT-004: PLT-STD-001 (Standard) → drift pH: 0.00/giorno
     [DayCycleController] pH Drift totale da 4 piante: 2.00 → pH attuale: 2.00
     ```
   - **Calcolo:** (+2) + (+2) + (-2) + (0) = +2

3. **Verifica Risultato:**
   - pH dovrebbe aumentare di +2 (non +4, perché Evil bilancia)
   - Tooltip pH dovrebbe mostrare "Piante: +2.00"

---

## ✅ CHECKLIST VERIFICA

### Prima del Test:
- [ ] PlantDatabase presente nella scena
- [ ] PhSystem registrato (PhSystemDebugConsole o PhSystemAutoSetup presente)
- [ ] HUDPhDisplay presente (per vedere pH)
- [ ] 3 PlantData configurati con Seed Item Config assegnato

### Durante il Test:
- [ ] Log mostra "PlantData trovato" quando pianti seme
- [ ] Log mostra "pH Drift totale" dopo End Day
- [ ] HUD pH si aggiorna correttamente
- [ ] Tooltip pH mostra breakdown corretto

### Risultati Attesi:
- [ ] Pianta Pure aumenta pH di +2/giorno
- [ ] Pianta Evil diminuisce pH di -2/giorno
- [ ] Pianta Standard non cambia pH (drift = 0)
- [ ] Piante multiple: drift totale = somma drift individuali

---

## 🐛 RISOLUZIONE PROBLEMI

### Problema: "PlantData non trovato"
**Sintomo:** Log mostra `[PotActions] Nessun PlantData trovato per seme TypeId 'seed-001'`

**Soluzione:**
1. Verifica che PlantDatabase sia presente nella scena
2. Verifica che PlantData abbia Seed Item Config assegnato
3. Verifica che TypeId del seed corrisponda (es. seed-001)
4. Controlla log: `[PlantDatabase] Inizializzato: X piante registrate`

### Problema: "PhSystem non trovato"
**Sintomo:** Log mostra `[DayCycleController] PhSystem non trovato nel ServiceContainer`

**Soluzione:**
1. Aggiungi PhSystemDebugConsole o PhSystemAutoSetup alla scena
2. Verifica che PhSystem sia registrato: log dovrebbe mostrare `[pH Debug Console] Sistema pH registrato nel ServiceContainer`

### Problema: "pH non cambia dopo End Day"
**Sintomo:** End Day eseguito ma pH rimane uguale

**Soluzione:**
1. Verifica che ci siano piante piantate (non Standard con drift = 0)
2. Verifica log: `[DayCycleController] pH Drift totale...`
3. Verifica che PhSystem sia registrato
4. Verifica che HUDPhDisplay sia presente e aggiornato

### Problema: "PlantDatabase non carica piante"
**Sintomo:** Log mostra `[PlantDatabase] Caricati 0 PlantData da Resources/Plants/`

**Soluzione:**
1. Verifica che i PlantData siano in `Assets/Resources/Plants/`
2. Oppure assegna manualmente i PlantData nella lista `All Plant Data` del PlantDatabase
3. Verifica che i file .meta esistano

---

## 📊 LOG ATTESI (Esempio Completo)

```
[PlantDatabase] Inizializzato: 3 piante registrate
[pH Debug Console] Sistema pH registrato nel ServiceContainer
[PotActions] PlantData trovato: PLT-PURE-001 (Pure), drift pH: 2/giorno
[ACT-001][POT-001] Plant OK: seed planted, state=..., PlantData: PLT-PURE-001 (Pure)
[PotActions] PlantData trovato: PLT-EVIL-001 (Evil), drift pH: -2/giorno
[ACT-001][POT-002] Plant OK: seed planted, state=..., PlantData: PLT-EVIL-001 (Evil)
[BLK-01.03A] DayCycleController: HandleDayChanged chiamato per Day 1
[DayCycleController] POT-001: PLT-PURE-001 (Pure) → drift pH: 2.00/giorno
[DayCycleController] POT-002: PLT-EVIL-001 (Evil) → drift pH: -2.00/giorno
[DayCycleController] pH Drift totale da 2 piante: 0.00 → pH attuale: 0.00
```

---

## 🎯 RISULTATO FINALE

Se tutti i test passano:
- ✅ Sistema pH completamente integrato con sistema crescita
- ✅ Piante influenzano pH in base alla famiglia
- ✅ Drift pH calcolato correttamente per tutte le piante
- ✅ Sistema pronto per espansioni future (livelli, mutazioni, effetti pH)

---

**Fine Guida**

