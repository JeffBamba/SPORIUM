# BLK-01.01 — Implementazione Vasi Interattivi nella Dome

## Descrizione
Implementazione di **2 vasi interattivi** nella stanza **Dome** come base per le azioni di Fase 1 (piantare/annaffiare/illuminare arriveranno con BLK-01.02).

## File Creati

### 1. Scripts
- `Assets/_Project/Scripts/Interactables/PotSlot.cs` - Script principale per i vasi interattivi
- `Assets/_Project/Scripts/UI/PotHUDWidget.cs` - Widget UI per mostrare info sui vasi
- `Assets/_Project/Scripts/Dome/RoomDomePotsBootstrap.cs` - Bootstrap per creare i vasi nella Dome
- `Assets/_Project/Scripts/Dome/PotSystemConfig.cs` - Configurazione globale del sistema
- `Assets/_Project/Scripts/Dome/PotSystemTester.cs` - Script di test per debugging
- `Assets/_Project/Scripts/Dome/PotSystemIntegration.cs` - Esempio integrazione con GameManager (BLK-01.02+)

### 2. Prefab
- `Assets/_Project/Prefabs/Interactables/PREF_POT_Slot.prefab` - Prefab del vaso (opzionale)

## Setup Richiesto

### Passo 1: Attaccare RoomDomePotsBootstrap
1. Apri la scena **SCN_VaultMap**
2. Trova la stanza **ROOM_Dome** (o creala se mancante)
3. **Attacca** il componente `RoomDomePotsBootstrap` alla stanza Dome
4. **Configura** le posizioni dei vasi se necessario (default: -1.5 e +1.5 sull'asse X)

### Passo 2: Aggiungere PotHUDWidget
1. **Crea un GameObject vuoto** chiamato `PotHUDWidget` nella scena
2. **Attacca** il componente `PotHUDWidget` a questo GameObject
3. Il widget si integrerà automaticamente con l'HUD esistente o creerà un fallback

### Passo 3: (Opzionale) Configurare Prefab
1. **Assegna** il prefab `PREF_POT_Slot` al campo `Pot Prefab` nel `RoomDomePotsBootstrap`
2. Se non assegnato, i vasi verranno creati automaticamente a runtime

### Passo 4: (Opzionale) Aggiungere Tester
1. **Crea un GameObject vuoto** chiamato `PotSystemTester` nella scena
2. **Attacca** il componente `PotSystemTester` per debugging e testing
3. **Usa i tasti T** (test) e **R** (reset) per verificare il funzionamento

## Funzionalità Implementate

### ✅ Vasi Interattivi
- **2 vasi** con ID univoci: **POT-001** e **POT-002**
- **Click handling** con controllo distanza dal Player
- **Evidenziazione** al passaggio del mouse
- **Stato iniziale**: Empty (vuoto)

### ✅ UI Widget
- **Widget HUD** che mostra info sul vaso selezionato
- **Integrazione automatica** con Canvas esistenti
- **Fallback** se non trova Canvas (crea nuovo Canvas)
- **Posizionamento** in basso-sinistra (configurabile)

### ✅ Bootstrap Automatico
- **Crea automaticamente** i vasi se mancanti
- **Anchor Dome_PotsAnchor** per organizzare i vasi
- **Posizioni configurabili** via Inspector
- **Gizmos** per visibilità in Editor

## Test di Accettazione

### 1. Test Base
- [ ] In **Play** su **SCN_VaultMap**, in **ROOM_Dome** compaiono **2 vasi**
- [ ] Passando il mouse sui vasi: si **evidenziano** (colore giallo)
- [ ] Click sui vasi: in **Console** appare `"[POT-001] Selected (state: Empty)"`

### 2. Test UI
- [ ] Il **widget HUD** mostra: `Selected: POT-001 — Stato: Empty`
- [ ] Il widget si aggiorna quando si seleziona un vaso diverso

### 3. Test Distanza
- [ ] Se il Player è **lontano** (> 1.5 unità): log `"Troppo lontano"`
- [ ] Se il Player è **vicino**: selezione permessa

### 4. Test Regressione
- [ ] **HUD esistente** (TXT_Day/Azioni/CRY) funziona
- [ ] **EndDay** e **Ascensore** funzionano
- [ ] **Click-to-move** del Player funziona

### 5. Test Avanzati (con Tester)
- [ ] **Premi T** per eseguire tutti i test automatici
- [ ] **Premi R** per resettare stati e widget
- [ ] **Controlla Console** per risultati dettagliati dei test

## Configurazione Avanzata

### Posizioni Vasi
```csharp
// Nel RoomDomePotsBootstrap
pot1Offset = new Vector2(-1.5f, 0f);  // Vaso sinistro
pot2Offset = new Vector2(1.5f, 0f);   // Vaso destro
```

### Colori Evidenziazione
```csharp
// Nel PotSlot
highlightColor = Color.yellow;  // Colore evidenziazione
baseColor = Color.white;        // Colore normale
```

### Distanza Interazione
```csharp
// Nel PotSlot
interactDistance = 1.5f;  // Distanza massima per interazione
```

## Debug e Troubleshooting

### Log Console
- `[RoomDomePotsBootstrap] Inizializzazione vasi Dome...`
- `[PotSlot] Player non trovato con tag 'Player' (fallback)`
- `[POT-001] Selected (state: Empty)`
- `[POT-001] Troppo lontano (>= 1.5)`

### Gizmos Editor
- **Cerchio verde**: posizione vaso
- **Cerchio giallo**: raggio interazione
- **Label**: ID del vaso

### Comandi Context Menu
- **Recreate Pots**: ricrea i vasi (utile per debugging)

## Prossimi Passi (BLK-01.02+)

### BLK-01.02: Azioni Base
- Piantare semi (costo: 5 CRY, 1 azione)
- Annaffiare piante (costo: 2 CRY, 1 azione)
- Illuminare vasi (costo: 3 CRY, 1 azione)
- **File di riferimento**: `PotSystemIntegration.cs` (esempio completo)

### BLK-01.04: Sistema Crescita
- 3 stadi di crescita (Empty → Occupied → Growing → Mature)
- Timer di crescita automatici
- Visualizzazione progressi e stati

### BLK-01.05: Economia e pH
- Costi in CRY per tutte le azioni
- Sistema pH Dome con effetti sulle piante
- Missioni e obiettivi di coltivazione

## Note Tecniche

### Eventi
- `PotSlot.OnPotSelected(PotSlot pot)` - Evento statico per selezione vaso

### Layer e Masks
- **Default Layer**: vasi creati a runtime
- **Configurabile**: via Inspector nel bootstrap

### Performance
- **Null-check** per componenti opzionali
- **Eventi** invece di Update() per efficienza
- **Gizmos** solo in Editor

## Commit Message Suggerito
```
BLK-01.01: Added interactive pot slots (POT-001/002) + HUD widget

- Created PotSlot.cs for interactive pot management
- Added PotHUDWidget.cs for UI integration
- Implemented RoomDomePotsBootstrap.cs for automatic setup
- Added PotSystemConfig.cs for global configuration
- Added PotSystemTester.cs for debugging and testing
- Added PREF_POT_Slot.prefab (optional)
- Integrated with existing HUD system
- Added distance-based interaction validation
- Included Editor gizmos for visibility
```

## Supporto
Per problemi o domande, controllare:
1. **Console** per errori e warning
2. **Gizmos** per posizionamento vasi
3. **Inspector** per configurazioni
4. **Log** per debug e troubleshooting
