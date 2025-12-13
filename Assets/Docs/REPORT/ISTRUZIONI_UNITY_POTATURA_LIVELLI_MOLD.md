# 🎮 Istruzioni Unity Editor - Sistema Potatura, Livelli e Mold

## 📋 Panoramica

Questa guida descrive le operazioni manuali necessarie in Unity Editor per rendere funzionanti e visibili i sistemi implementati:
- **Potatura (AZ-13)**: Azione con RNG, reroll Spray, bonus resa
- **Sistema Livelli (1-5)**: Progressione basata su cicli completati
- **Mold System**: Calcolo rischio, infestazione, effetti

---

## 🔧 FASE 1: Creazione ScriptableObject Config

### 1.1 PruningConfig

1. **Apri Unity Editor** e naviga nella cartella `Assets/Resources/Configs/`
   - Se la cartella non esiste, creala: `Assets/Resources/Configs/`

2. **Crea PruningConfig:**
   - Click destro in `Assets/Resources/Configs/`
   - Seleziona: `Create → Sporae → PruningConfig`
   - Rinomina: `PruningConfig`

3. **Configura valori nell'Inspector:**
   - **Base Success Rate By Stage** (array 6 elementi):
     - Element 0 (Seed): `10`
     - Element 1 (Sprout): `15`
     - Element 2 (Growth): `80`
     - Element 3 (Flowering): `10`
     - Element 4 (HarvestReady): `12`
     - Element 5 (Resting): `10`
   
   - **Spray Bonus By Stage** (array 6 elementi):
     - Element 0 (Seed): `15`
     - Element 1 (Sprout): `15`
     - Element 2 (Growth): `10`
     - Element 3 (Flowering): `10`
     - Element 4 (HarvestReady): `5`
     - Element 5 (Resting): `10`
   
   - **Use Percentage Bonus**: `false` (usa +1 frutto invece di +10%)
   - **Action Cost**: `1`

4. **Salva** l'asset (Ctrl+S)

### 1.2 PlantLevelConfig

1. **Crea PlantLevelConfig:**
   - Click destro in `Assets/Resources/Configs/`
   - Seleziona: `Create → Sporae → PlantLevelConfig`
   - Rinomina: `PlantLevelConfig`

2. **Configura valori nell'Inspector:**
   - **Cycles Thresholds** (array 4 elementi):
     - Element 0 (Lvl 1→2): `1`
     - Element 1 (Lvl 2→3): `2`
     - Element 2 (Lvl 3→4): `3`
     - Element 3 (Lvl 4→5): `4`
   
   - **Quantity Reduction Per Level**: `15` (riduzione % per livello oltre 2)

3. **Salva** l'asset

### 1.3 MoldConfig

1. **Crea MoldConfig:**
   - Click destro in `Assets/Resources/Configs/`
   - Seleziona: `Create → Sporae → MoldConfig`
   - Rinomina: `MoldConfig`

2. **Configura valori nell'Inspector:**
   - **Mild Risk Threshold**: `1`
   - **Severe Risk Threshold**: `2`
   - **Critical Risk Threshold**: `3`
   - **Overwatering Days Threshold**: `3`
   - **Acidic Ph Threshold**: `-20`
   - **Pruning Neglect Accumulation**: `0.5`
   - **Mild Score Penalty**: `10`
   - **Severe Score Penalty**: `30`
   - **Mild Level Reduction**: `1`
   - **Severe Level Reduction**: `3`

3. **Salva** l'asset

---

## 🎨 FASE 2: Creazione UI PruningDialog

### 2.1 Creare Prefab PruningDialog

1. **Apri scena** `SCN_VaultMap.unity` (o scena principale)

2. **Crea struttura UI:**
   - Nel Canvas principale, click destro → `UI → Panel` → Rinomina: `PruningDialog`
   - Seleziona `PruningDialog` e nell'Inspector:
     - **RectTransform**: Anchor presets (centro), Width: `400`, Height: `300`
     - **Image**: Color `(0, 0, 0, 200)` (nero semi-trasparente)
     - **Canvas Group**: Interactable `true`, Blocks Raycasts `true`

3. **Crea Panel contenuto:**
   - Click destro su `PruningDialog` → `UI → Panel` → Rinomina: `ContentPanel`
   - **RectTransform**: Anchor presets (centro), Width: `350`, Height: `250`
   - **Image**: Color `(50, 50, 50, 255)` (grigio scuro)

4. **Aggiungi elementi UI dentro ContentPanel:**
   
   **a) Titolo:**
   - Click destro su `ContentPanel` → `UI → Text - TextMeshPro` → Rinomina: `TitleText`
   - **Text**: `✂️ Potatura`
   - **Font Size**: `24`
   - **Alignment**: Center, Middle
   - **RectTransform**: Pos Y `100`, Width `300`, Height `40`

   **b) Body:**
   - Click destro su `ContentPanel` → `UI → Text - TextMeshPro` → Rinomina: `BodyText`
   - **Text**: `Eseguire la potatura su questa pianta?`
   - **Font Size**: `16`
   - **Alignment**: Center, Middle
   - **RectTransform**: Pos Y `50`, Width `300`, Height `40`

   **c) Toggle Spray:**
   - Click destro su `ContentPanel` → `UI → Toggle` → Rinomina: `SprayToggle`
   - **RectTransform**: Pos Y `0`, Width `300`, Height `30`
   - Nel child `Label` (TextMeshPro):
     - **Text**: `Aggiungi Spray Antifungino (consuma STR-004)`
     - **Font Size**: `14`

   **d) Button Conferma:**
   - Click destro su `ContentPanel` → `UI → Button - TextMeshPro` → Rinomina: `ConfirmButton`
   - **RectTransform**: Pos X `-80`, Pos Y `-60`, Width `120`, Height `40`
   - Nel child `Text (TMP)`:
     - **Text**: `Conferma`
     - **Font Size**: `16`

   **e) Button Annulla:**
   - Click destro su `ContentPanel` → `UI → Button - TextMeshPro` → Rinomina: `CancelButton`
   - **RectTransform**: Pos X `80`, Pos Y `-60`, Width `120`, Height `40`
   - Nel child `Text (TMP)`:
     - **Text**: `Annulla`
     - **Font Size**: `16`

5. **Aggiungi componente PruningDialog:**
   - Seleziona `PruningDialog` (root)
   - Click `Add Component` → Cerca `PruningDialog` → Aggiungi
   - **Linka riferimenti:**
     - `Dialog Panel` → `PruningDialog` (root GameObject)
     - `Title Text` → `TitleText`
     - `Body Text` → `BodyText`
     - `Spray Toggle` → `SprayToggle`
     - `Confirm Button` → `ConfirmButton`
     - `Cancel Button` → `CancelButton`

6. **Nascondi dialog inizialmente:**
   - Seleziona `PruningDialog` → **Inspector** → Disabilita checkbox `Active` (nascondi)

7. **Crea Prefab:**
   - Trascina `PruningDialog` da Hierarchy a `Assets/_Project/Prefabs/UI/`
   - Se la cartella non esiste, creala prima
   - Rinomina prefab: `PruningDialog`

---

## 🔘 FASE 3: Aggiungere Bottone Pruning ai Widget

### 3.1 PotHUDWidget

1. **Nella scena**, trova il GameObject che contiene `PotHUDWidget`

2. **Aggiungi Button Pruning:**
   - Duplica un bottone esistente (es. `btnSpray`) come riferimento
   - Rinomina: `btnPruning`
   - **RectTransform**: Posiziona accanto agli altri bottoni (es. X `550`, Y `50`)
   - Nel child `Text (TMP)`:
     - **Text**: `Potatura`

3. **Linka riferimento:**
   - Seleziona GameObject con `PotHUDWidget`
   - **Inspector** → `PotHUDWidget` component:
     - `Btn Pruning` → `btnPruning`
     - `Pruning Dialog Prefab` → `PruningDialog` prefab (da `Assets/_Project/Prefabs/UI/`)

### 3.2 PotDetailsWidget

1. **Nella scena**, trova il GameObject che contiene `PotDetailsWidget`

2. **Aggiungi Button Pruning:**
   - Duplica un bottone esistente (es. `_sprayButton`)
   - Rinomina: `_pruningButton`
   - Posiziona nel pannello dettagli

3. **Linka riferimento:**
   - Seleziona GameObject con `PotDetailsWidget`
   - **Inspector** → `PotDetailsWidget` component:
     - `Pruning Button` → `_pruningButton`
     - `Pruning Dialog Prefab` → `PruningDialog` prefab

---

## 📊 FASE 4: Aggiungere Indicatori UI (Livello, Mold Risk, Infestazione)

### 4.1 PotHUDWidget

1. **Aggiungi Text Plant Level:**
   - Nel GameObject `PotHUDWidget`, trova il pannello stats
   - Click destro → `UI → Text - TextMeshPro` → Rinomina: `PlantLevelText`
   - **Text**: `📈 Livello: -`
   - **Font Size**: `14`
   - Posiziona sotto `OptimalDaysText`

2. **Aggiungi Text Mold Risk:**
   - Click destro → `UI → Text - TextMeshPro` → Rinomina: `MoldRiskText`
   - **Text**: `✅ Mold Risk: Nessuno`
   - **Font Size**: `14`
   - Posiziona sotto `PlantLevelText`

3. **Aggiungi Badge Infestazione:**
   - Click destro → `UI → Panel` → Rinomina: `InfestationBadge`
   - Seleziona `InfestationBadge` e nell'Inspector:
     - **RectTransform** (componente in alto): 
       - Seleziona anchor preset (es. "Top-Left" o "Center")
       - Nel campo **Width**: `120`
       - Nel campo **Height**: `30`
     - **Image** (componente): Color `(200, 0, 0, 255)` (rosso)
   - Aggiungi child `Text - TextMeshPro`:
     - Click destro su `InfestationBadge` → `UI → Text - TextMeshPro`
     - **Text**: `⚠️ INFESTATA`
     - **Font Size**: `16`, **Bold**
     - **Color**: Bianco
   - **Disabilita** inizialmente (uncheck Active nella checkbox in alto dell'Inspector)

4. **Linka riferimenti:**
   - Seleziona GameObject con `PotHUDWidget`
   - **Inspector** → `PotHUDWidget` component:
     - `Plant Level Text` → `PlantLevelText`
     - `Mold Risk Text` → `MoldRiskText`
     - `Infestation Badge` → `InfestationBadge`

### 4.2 PotDetailsWidget

1. **Aggiungi Text Plant Level:**
   - Nel pannello stats di `PotDetailsWidget`
   - Click destro → `UI → Text - TextMeshPro` → Rinomina: `Plant Level`
   - **Text**: `Livello: -`
   - Posiziona sotto `Optimal Days`

2. **Aggiungi Text Mold Risk:**
   - Click destro → `UI → Text - TextMeshPro` → Rinomina: `Mold Risk`
   - **Text**: `Mold Risk: -`
   - Posiziona sotto `Plant Level`

3. **Aggiungi Badge Infestazione:**
   - Click destro → `UI → Panel` → Rinomina: `Infestation Badge`
   - Configura come sopra
   - **Disabilita** inizialmente

4. **Linka riferimenti:**
   - Seleziona GameObject con `PotDetailsWidget`
   - **Inspector** → `PotDetailsWidget` component:
     - `Plant Level Text` → `Plant Level`
     - `Mold Risk Text` → `Mold Risk`
     - `Infestation Badge` → `Infestation Badge`

---

## ✅ FASE 5: Verifica e Test

### 5.1 Verifica Config Assets

1. **Verifica che tutti i config siano in `Assets/Resources/Configs/`:**
   - `PruningConfig`
   - `PlantLevelConfig`
   - `MoldConfig`

2. **Verifica nomi esatti** (case-sensitive):
   - I nomi devono corrispondere esattamente a quelli usati nel codice

### 5.2 Test Funzionalità

1. **Play Mode** in Unity

2. **Test Potatura:**
   - Seleziona un vaso con pianta
   - Click su bottone "Potatura"
   - Verifica che si apra il dialog
   - Testa con/senza toggle Spray
   - Verifica feedback in console

3. **Test Livelli:**
   - Completa un ciclo (pianta → raccolta → Resting)
   - Verifica che `CompletedCycles` incrementi
   - Verifica che livello aumenti dopo soglia cicli
   - Verifica modificatori resa in Harvest (Lvl 3+)

4. **Test Mold System:**
   - Overwatering 3+ giorni consecutivi
   - Verifica che `MoldRiskLevel` aumenti
   - Verifica che infestazione si applichi (Mild/Severe)
   - Testa potatura per rimuovere infestazione
   - Testa Spray per rimuovere muffe

5. **Test UI:**
   - Verifica che indicatori Livello, Mold Risk, Infestazione si aggiornino
   - Verifica che badge "INFESTATA" appaia quando `MoldRiskLevel >= 1`

---

## 🐛 Troubleshooting

### Config non trovato
- **Errore**: `PruningConfig non trovato in Resources/Configs/PruningConfig`
- **Soluzione**: Verifica che il file sia in `Assets/Resources/Configs/` e si chiami esattamente `PruningConfig`

### Dialog non si apre
- **Errore**: Dialog non appare quando si clicca "Potatura"
- **Soluzione**: 
  - Verifica che `PruningDialogPrefab` sia assegnato nel widget
  - Verifica che il prefab abbia il componente `PruningDialog`
  - Verifica che tutti i riferimenti UI siano linkati

### UI non si aggiorna
- **Errore**: Indicatori Livello/Mold non si aggiornano
- **Soluzione**:
  - Verifica che i riferimenti Text siano linkati nei widget
  - Verifica che i GameObject UI abbiano i nomi corretti (per auto-find)
  - Controlla console per errori

### Bottoni non funzionano
- **Errore**: Click su bottone Pruning non fa nulla
- **Soluzione**:
  - Verifica che il bottone abbia `OnClick` event configurato (dovrebbe essere automatico)
  - Verifica che `PotActions.CanPruning()` ritorni `true`
  - Controlla console per errori

---

## 📝 Note Finali

- **Tutti i config devono essere in `Resources/Configs/`** per essere caricati dinamicamente
- **I nomi dei GameObject UI devono corrispondere** a quelli cercati dal codice (o essere linkati manualmente)
- **Il prefab PruningDialog deve essere salvato** prima di essere assegnato ai widget
- **Testa sempre in Play Mode** per verificare che tutto funzioni correttamente

---

## ✅ Checklist Completa

- [ ] PruningConfig creato e configurato
- [ ] PlantLevelConfig creato e configurato
- [ ] MoldConfig creato e configurato
- [ ] PruningDialog prefab creato con tutti i riferimenti
- [ ] Bottone Pruning aggiunto a PotHUDWidget
- [ ] Bottone Pruning aggiunto a PotDetailsWidget
- [ ] Indicatori Livello/Mold/Infestazione aggiunti a PotHUDWidget
- [ ] Indicatori Livello/Mold/Infestazione aggiunti a PotDetailsWidget
- [ ] Tutti i riferimenti linkati nei componenti
- [ ] Test funzionalità completato
- [ ] Nessun errore in console

---

**🎉 Implementazione Completa!**

Tutti i sistemi sono ora funzionanti e visibili. Buon testing! 🚀

