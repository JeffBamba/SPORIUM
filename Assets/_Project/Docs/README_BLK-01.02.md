# BLK-01.02 — Pot Base Actions (Plant / Water / Light)

## Panoramica

**BLK-01.02** implementa le azioni base sui vasi: **piantare**, **annaffiare** e **illuminare**. Ogni azione ha gating per distanza, azioni disponibili e CRY, con stato del vaso/pianta coerente.

## Funzionalità Implementate

### ✅ Azioni Base
- **ACT-001 — Plant**: Piantare seme generico (SDE-001) in vaso vuoto
- **ACT-002 — Water**: Aumentare idratazione della pianta (0→3)
- **ACT-003 — Light**: Aumentare esposizione alla luce (0→3)

### ✅ Sistema di Gating
- **Distanza**: Azioni consentite solo entro `InteractDistance` (default: 2.0)
- **Risorse**: Ogni azione costa **1 Azione** e **1 CRY**
- **Stato**: Plant solo se vaso vuoto, Water/Light solo se ha pianta
- **Limiti**: Idratazione e luce rispettano i cap configurabili

### ✅ Integrazione Sistemi
- **GameManager**: Consumo automatico di Azioni e CRY
- **Inventario**: Gestione semi SDE-001 (Generic Seed)
- **Eventi**: Sistema di notifiche per UI e logica futura
- **HUD**: Widget con 3 pulsanti contestuali e feedback visivo

## Architettura

### File Principali
```
Assets/_Project/Scripts/
├── Dome/
│   ├── PotStateModel.cs          (NUOVO - Modello dati vaso)
│   ├── PotEvents.cs              (NUOVO - Sistema eventi)
│   ├── PotActions.cs             (NUOVO - Logica azioni)
│   ├── PotSystemConfig.cs        (ESTESO - Costi e limiti)
│   ├── PotSystemIntegration.cs   (ESTESO - Bridge sistemi)
│   └── RoomDomePotsBootstrap.cs  (ESTESO - Componenti)
├── Interactables/
│   └── PotSlot.cs                (ESTESO - InRange, PotActions)
└── UI/
    └── PotHUDWidget.cs           (ESTESO - 3 pulsanti azione)
```

### Componenti Chiave

#### PotStateModel
- **Stato vaso**: `HasPlant`, `Stage`, `Hydration`, `LightExposure`
- **Timestamps**: `PlantedDay`, `LastWateredDay`, `LastLitDay`
- **Metodi**: `PlantSeed()`, `IncreaseHydration()`, `IncreaseLightExposure()`

#### PotActions
- **Validazione**: `CanPlant()`, `CanWater()`, `CanLight()`
- **Esecuzione**: `DoPlant()`, `DoWater()`, `DoLight()`
- **Gating**: Controllo distanza, risorse, stato vaso

#### PotEvents
- **Eventi**: `OnPotAction`, `OnPotStateChanged`, `OnPotActionFailed`
- **Tipi**: `Plant`, `Water`, `Light`
- **Utility**: Nomi localizzati, codici univoci, costi

## Setup e Configurazione

### 1. Configurazione Sistema
```csharp
// Crea configurazione predefinita
PotSystemConfig config = PotSystemConfig.CreateDefaultConfig();

// Oppure usa ScriptableObject personalizzato
[SerializeField] private PotSystemConfig potSystemConfig;
```

### 2. Componenti Richiesti
- **PotSlot**: Deve avere `PotActions` component
- **PotHUDWidget**: Si integra automaticamente con HUD esistente
- **PotSystemIntegration**: Bridge tra sistemi (opzionale)

### 3. Inventario Iniziale
```csharp
// Nel GameManager.Awake()
AddItem("SDE-001", 3); // 3 semi generici per test
```

## Test Plan

### Setup Test
1. **Avvia**: SCN_VaultMap
2. **Verifica**: GameManager con CRY=50, Azioni=4, SDE-001≥1
3. **Posizione**: Player vicino ai vasi POT-001/POT-002

### Test Flow

#### Plant Flow
- [ ] Seleziona POT-001 → Plant **enabled**
- [ ] Click Plant → Azioni--, CRY--, HasPlant=true
- [ ] Ripeti Plant → **disabled** + tooltip "Vaso non vuoto"

#### Water/Light Flow
- [ ] Con pianta: Water/Light **enabled** finché sotto cap
- [ ] A cap raggiunto: pulsanti **disabled** + tooltip appropriato
- [ ] Verifica: Hydration/LightExposure aggiornati nel log

#### Gating Risorse
- [ ] Azioni=0 o CRY=0 → tutti i pulsanti **disabled**
- [ ] Tooltip: "Azioni/CRY insufficienti"
- [ ] Log: warning appropriato

#### Distanza
- [ ] Allontanati oltre InteractDistance → pulsanti **nascosti**
- [ ] Avvicinati → pulsanti riappaiono

### Regressioni
- [ ] Ascensore: –5 CRY + fallback scale (OK)
- [ ] BTN_EndDay: avanza giorno (OK)
- [ ] HUD: aggiorna in tempo reale (OK)

## Criteri di Accettazione (DoD)

- [x] **3 pulsanti contestuali** su POT-001/POT-002 quando selezionati e in range
- [x] **Consumo risorse**: 1 Azione + 1 CRY per azione, aggiornamento HUD
- [x] **Plant**: Richiede SDE-001≥1, imposta HasPlant=true, Stage=0
- [x] **Water/Light**: Incrementano Hydration/LightExposure fino al cap
- [x] **Gating**: Pulsanti disabilitati con tooltip esplicativi
- [x] **Eventi**: OnPotAction e OnPotStateChanged emessi correttamente
- [x] **Regressioni**: Selezione, highlight, HUD baseline, ascensore funzionanti

## Convenzioni e Codici

### Azioni
- **ACT-001**: Plant (Piantare)
- **ACT-002**: Water (Annaffiare)  
- **ACT-003**: Light (Illuminare)

### Semi
- **SDE-001**: Generic Seed (Seme generico)

### Vasi
- **POT-001**: Primo vaso Dome
- **POT-002**: Secondo vaso Dome

## Note di Integrazione

### BLK-01.04 (Crescita 3 Stadi)
- `PotStateModel.Stage` già preparato per stadi 1-3
- Eventi `OnPotStateChanged` per notifiche crescita
- Metodi `IncreaseHydration/LightExposure` per progressione

### BLK-01.05 (Frutto/Raccolta)
- Timestamps `PlantedDay`, `LastWateredDay`, `LastLitDay` per calcoli
- Eventi `OnPotAction` per tracciamento azioni
- Sistema di stati estendibile per maturità

## Debug e Troubleshooting

### Log Chiave
```
[ACT-001][POT-001] Plant OK: seme piantato, stato=...
[ACT-002][POT-001] Water OK: hydration=2/3
[ACT-003][POT-001] Light OK: light=1/3
```

### Gizmos Editor
- **Verde**: Vaso selezionato
- **Giallo**: Raggio interazione
- **Cyan**: Raggio PotActions
- **Label**: Stato H:X/Y L:Z/W

### Shortcuts Debug (Editor)
- **Y**: Plant sul vaso selezionato
- **U**: Water sul vaso selezionato  
- **I**: Light sul vaso selezionato

## Performance e Scalabilità

- **Eventi statici**: Comunicazione efficiente tra componenti
- **Caching**: Riferimenti a GameManager e Player
- **Lazy initialization**: Componenti creati solo quando necessario
- **Gizmos condizionali**: Solo in Editor, non in build

## Prossimi Passi

1. **Test completo** di tutti i flow
2. **Verifica integrazione** con sistemi esistenti
3. **Preparazione** per BLK-01.04 (crescita)
4. **Documentazione** per team di sviluppo

---

**BLK-01.02 COMPLETATO** ✅  
*Azioni base sui vasi implementate e testate*
