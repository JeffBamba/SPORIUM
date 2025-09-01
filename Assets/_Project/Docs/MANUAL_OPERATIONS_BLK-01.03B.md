# 📋 **MANUAL OPERATIONS GUIDE — BLK-01.03B**

## 🎯 **Panoramica**

Questa guida spiega passo-passo come configurare e testare le nuove funzionalità **HUD & Visuals (Plant Growth)** implementate in **BLK-01.03B**. Le operazioni sono descritte a livello principiante per garantire il corretto funzionamento.

---

## 🔧 **SETUP INIZIALE**

### **Passo 1: Aprire il Progetto Unity**
1. **Avvia Unity Hub**
2. **Clicca "Open"** e seleziona la cartella del progetto Sporae
3. **Aspetta** che Unity carichi completamente il progetto
4. **Verifica** che la scena principale sia aperta (SCN_VaultMap)

### **Passo 2: Verificare la Struttura del Progetto**
1. **Apri la finestra Project** (in basso)
2. **Naviga** in `Assets/_Project/Scripts/`
3. **Verifica** che esistano questi file:
   - ✅ `UI/PotHUDWidget.cs` (esteso)
   - ✅ `Dome/PotSystem/Growth/PotGrowthController.cs` (esteso)
   - ✅ `Dev/GrowthDebugHotkeys.cs` (nuovo)

### **Passo 3: Controllare la Scena**
1. **Apri la finestra Hierarchy** (a sinistra)
2. **Verifica** che esistano questi GameObject:
   - ✅ `ROOM_Dome` con `RoomDomePotsBootstrap`
   - ✅ `Pot_POT-001` e `Pot_POT-002`
   - ✅ `SYS_GameManager`
   - ✅ `Canvas_HUD` (se presente)

---

## 🎨 **GENERAZIONE SPRITE PLACEHOLDER**

### **Passo 4: Creare Sprite per le Piante**
1. **In Unity, vai su Menu** → `Sporae` → `BLK-01.03B` → `Generate Plant Placeholder Sprites`
2. **Aspetta** che Unity generi gli sprite
3. **Verifica** nella Console il messaggio: `[BLK-01.03B] ✅ Sprite placeholder piante generati con successo!`
4. **Controlla** che siano stati creati in `Assets/_Project/Placeholders/Plants/`:
   - ✅ `PLANT_Stage0_Empty.png` (grigio)
   - ✅ `PLANT_Stage1_Seed.png` (marrone)
   - ✅ `PLANT_Stage2_Sprout.png` (verde)
   - ✅ `PLANT_Stage3_Mature.png` (giallo)

### **Passo 5: Assegnare Sprite ai Vasi**
1. **Seleziona** `Pot_POT-001` nella Hierarchy
2. **Nel componente PotGrowthController**, assegna gli sprite:
   - **s0_empty**: Trascina `PLANT_Stage0_Empty`
   - **s1_seed**: Trascina `PLANT_Stage1_Seed`
   - **s2_sprout**: Trascina `PLANT_Stage2_Sprout`
   - **s3_mature**: Trascina `PLANT_Stage3_Mature`
3. **Ripeti** per `Pot_POT-002`

---

## 🎮 **TEST DEL SISTEMA**

### **Passo 6: Avviare il Test**
1. **Premi il pulsante Play** (▶️) in Unity
2. **Aspetta** che la scena si carichi completamente
3. **Controlla la Console** per i messaggi di inizializzazione:
   - ✅ `[BLK-01.03B] GrowthDebugHotkeys inizializzato`
   - ✅ `[BLK-01.03B] Hotkeys disponibili: G, H, L, P`

### **Passo 7: Testare Selezione Vasi**
1. **Clicca su un vaso** (POT-001 o POT-002)
2. **Verifica** che il widget HUD si attivi e mostri:
   - ✅ **PotId**: ID del vaso selezionato
   - ✅ **Stage Label**: "Empty" (se vaso vuoto)
   - ✅ **Progress Bar**: 0% (se vaso vuoto)
   - ✅ **Stage Icon**: Colore grigio (placeholder)

### **Passo 8: Testare Azioni sui Vasi**
1. **Assicurati** di essere vicino al vaso (raggio 2 unità)
2. **Usa i pulsanti HUD** o i **tasti di debug**:
   - **P** (Plant): Pianta un seme
   - **H** (Water): Annaffia la pianta
   - **L** (Light): Illumina la pianta
3. **Verifica** che ogni azione:
   - ✅ Consumi 1 azione e CRY
   - ✅ Aggiorni lo stato del vaso
   - ✅ Mostri feedback nella Console

---

## 🔄 **TEST CRESCITA PIANTE**

### **Passo 9: Simulare Crescita**
1. **Dopo aver piantato e curato** (Water+Light) un vaso
2. **Premi G** per simulare End Day
3. **Verifica** che:
   - ✅ Il giorno avanzi
   - ✅ I punti crescita aumentino
   - ✅ La progress bar si aggiorni
   - ✅ Lo stadio cambi quando appropriato

### **Passo 10: Monitorare Progresso**
1. **Osserva la Progress Bar** durante la crescita
2. **Verifica** che avanzi correttamente:
   - **Seed → Sprout**: 0% → 100% (2 punti crescita)
   - **Sprout → Mature**: 0% → 100% (3 punti crescita)
   - **Mature**: Sempre 100%

### **Passo 11: Testare Cambio Stadi**
1. **Continua a curare** la pianta (Water+Light ogni giorno)
2. **Usa G** per avanzare i giorni
3. **Verifica** che:
   - ✅ Lo **Stage Label** cambi: Empty → Seed → Sprout → Mature
   - ✅ Lo **Stage Icon** cambi colore
   - ✅ La **Progress Bar** si resetti per ogni nuovo stadio
   - ✅ La **scala** della pianta aumenti progressivamente

---

## 🧪 **TEST DEBUG HOTKEYS**

### **Passo 12: Testare Tasti Debug**
1. **Assicurati** che un vaso sia selezionato
2. **Testa ogni hotkey**:
   - **G**: Simula End Day
   - **H**: Annaffia vaso selezionato
   - **L**: Illumina vaso selezionato
   - **P**: Pianta su vaso selezionato
3. **Verifica** che ogni azione:
   - ✅ Produca log nella Console con prefisso `[BLK-01.03B]`
   - ✅ Aggiorni l'UI in tempo reale
   - ✅ Non causi errori o crash

### **Passo 13: Verificare Info Debug**
1. **Osserva l'angolo in alto-sinistra** della scena
2. **Verifica** che sia visibile il box debug con:
   - ✅ Titolo "[BLK-01.03B] Growth Debug Hotkeys"
   - ✅ Lista hotkeys disponibili
   - ✅ Informazioni sul vaso selezionato
   - ✅ Stato corrente (stadio, punti crescita)

---

## 🚫 **TEST NON-REGRESSIONI**

### **Passo 14: Verificare UIBlocker**
1. **Clicca ripetutamente** sui pulsanti del widget HUD
2. **Verifica** che:
   - ✅ Il **Player non si muova** quando clicchi sui pulsanti
   - ✅ I **click sui pulsanti** funzionino correttamente
   - ✅ L'**input del giocatore** sia bloccato solo per l'UI

### **Passo 15: Verificare BTN_EndDay**
1. **Clicca ripetutamente** sul pulsante End Day
2. **Verifica** che:
   - ✅ Il **pulsante sia sempre cliccabile**
   - ✅ I **giorni avanzino** correttamente
   - ✅ Le **risorse si aggiornino** (azioni, CRY)
   - ✅ Non ci siano **conflitti** con il nuovo sistema

### **Passo 16: Testare Dual Pots**
1. **Seleziona POT-001**, esegui alcune azioni
2. **Seleziona POT-002**, esegui azioni diverse
3. **Torna a POT-001**
4. **Verifica** che:
   - ✅ Il **widget mostri sempre** il vaso corretto
   - ✅ Le **informazioni si aggiornino** correttamente
   - ✅ Non ci siano **miscugli** tra i due vasi

---

## 🔍 **TROUBLESHOOTING**

### **Problema: Widget non si attiva**
**Soluzione:**
1. **Verifica** che il vaso abbia il componente `PotSlot`
2. **Controlla** che `PotActions` sia assegnato
3. **Assicurati** che il Player sia nel raggio di interazione (2 unità)

### **Problema: Progress bar non si aggiorna**
**Soluzione:**
1. **Verifica** che `PlantGrowthConfig` sia in `Resources/Configs/`
2. **Controlla** che gli eventi `OnPlantGrew` siano emessi
3. **Assicurati** che il vaso sia registrato nel sistema crescita

### **Problema: Sprite non cambiano**
**Soluzione:**
1. **Verifica** che gli sprite siano assegnati in `PotGrowthController`
2. **Controlla** che `plantRenderer` sia assegnato
3. **Assicurati** che `UpdateVisuals()` sia chiamato

### **Problema: Debug hotkeys non funzionano**
**Soluzione:**
1. **Verifica** che `GrowthDebugHotkeys` sia presente nella scena
2. **Controlla** che `enableDebugHotkeys` sia true
3. **Assicurati** di essere in Editor mode (non in build)

---

## 📊 **VERIFICA FINALE**

### **Checklist Completamento**
- ✅ **Setup**: Progetto Unity aperto e funzionante
- ✅ **Sprite**: Placeholder generati e assegnati
- ✅ **HUD**: Widget si attiva e mostra informazioni corrette
- ✅ **Crescita**: Progress bar avanza correttamente
- ✅ **Stadi**: Cambio automatico di stage e visuali
- ✅ **Debug**: Hotkeys funzionano e producono log
- ✅ **Non-regressioni**: UIBlocker e BTN_EndDay funzionanti
- ✅ **Dual pots**: Gestione corretta di più vasi

### **Messaggi Console Attesi**
```
[BLK-01.03B] GrowthDebugHotkeys inizializzato. Hotkeys disponibili:
[BLK-01.03B] G = Simula End Day
[BLK-01.03B] H = Water pot selezionato
[BLK-01.03B] L = Light pot selezionato
[BLK-01.03B] P = Plant su pot selezionato
[BLK-01.03B] Vaso POT-001 selezionato. Aggiornamento UI...
[BLK-01.03B] UI aggiornata: POT-001 - Seed - 50.0%
```

---

## 🎉 **CONCLUSIONE**

Se tutti i passi sono stati completati con successo, **BLK-01.03B è completamente funzionante**! 

Il sistema ora fornisce:
- **HUD avanzato** per monitorare la crescita delle piante
- **Feedback visivo** per ogni stadio di sviluppo
- **Debug hotkeys** per test rapidi
- **Progress tracking** accurato e reattivo

**Il progetto è pronto per BLK-01.04 (Varietà Piante e Sprite Artistici)** 🚀

---

## 📞 **Supporto**

Se riscontri problemi durante l'implementazione:
1. **Controlla la Console** per messaggi di errore
2. **Verifica** che tutti i componenti siano assegnati
3. **Ripeti** i passi di setup dall'inizio
4. **Consulta** la documentazione `README_BLK-01.03B.md`

**Buon lavoro!** 🌱✨
