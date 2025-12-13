# Istruzioni Unity: Creazione Tooltip Growth Manuale

## Obiettivo
Creare un tooltip UI per la label Growth che appare al passaggio del mouse, senza creazione dinamica in runtime.

## Fase 1: Creare il Tooltip Panel

### 1.1 Posizionamento nella Gerarchia
1. Apri la scena principale
2. Nella Hierarchy, trova `Canvas` (il Canvas principale della scena - quello con `GraphicRaycaster` e `EventSystem` come child)
3. Click destro su `Canvas` → `UI → Panel`
4. Rinomina il Panel: `GrowthTooltipPanel`

### 1.2 Configurazione RectTransform
1. Seleziona `GrowthTooltipPanel`
2. Nell'Inspector, trova il componente **RectTransform**:
   - **Anchor Presets**: Premi `Alt + Shift` e clicca su `Top Center` (per ancorare in alto al centro)
   - **Pos X**: `0`
   - **Pos Y**: `-150` (offset dall'alto)
   - **Width**: `600`
   - **Height**: `300`

### 1.3 Configurazione Image (Sfondo)
1. Seleziona `GrowthTooltipPanel`
2. Nell'Inspector, trova il componente **Image**:
   - **Color**: `(61, 86, 142, 255)` oppure esadecimale `#3D568E`
   - **Raycast Target**: ✅ **Disattivato** (uncheck) - importante per non bloccare i click

### 1.4 Disattivazione Iniziale
1. Seleziona `GrowthTooltipPanel`
2. Nell'Inspector, in alto, **uncheck** la checkbox `Active` (il tooltip deve essere nascosto inizialmente)

---

## Fase 2: Creare il Testo del Tooltip

### 2.1 Creare il GameObject Testo
1. Click destro su `GrowthTooltipPanel` → `UI → Text - TextMeshPro`
2. Rinomina: `GrowthTooltipText`

### 2.2 Configurazione RectTransform del Testo
1. Seleziona `GrowthTooltipText`
2. Nell'Inspector, **RectTransform**:
   - **Anchor Presets**: Premi `Alt + Shift` e clicca su `Stretch Stretch` (per riempire tutto il panel)
   - **Left**: `12`
   - **Right**: `-12`
   - **Top**: `-12`
   - **Bottom**: `12`

### 2.3 Configurazione TextMeshProUGUI
1. Seleziona `GrowthTooltipText`
2. Nell'Inspector, componente **TextMeshProUGUI**:
   - **Text**: `Growth info` (testo placeholder, verrà aggiornato in runtime)
   - **Font Size**: `16`
   - **Alignment**: `Left` (allineamento a sinistra)
   - **Color**: `(255, 255, 255, 255)` (bianco)
   - **Rich Text**: ✅ **Attivato** (check) - importante per i colori nel tooltip
   - **Raycast Target**: ✅ **Disattivato** (uncheck)

---

## Fase 3: Collegare il Tooltip al PotDetailsWidget

### 3.1 Assegnare il Tooltip Panel
1. Nella Hierarchy, trova `UI_PotDetails`
2. Seleziona `UI_PotDetails`
3. Nell'Inspector, trova il componente **PotDetailsWidget**
4. Espandi la sezione **"Growth Tooltip UI (assegna manualmente in Unity)"**
5. Trascina `GrowthTooltipPanel` dal Canvas alla slot **"Growth Tooltip Panel"**

### 3.2 Assegnare il Testo del Tooltip
1. Nella stessa sezione **"Growth Tooltip UI"**
2. Trascina `GrowthTooltipText` (child di `GrowthTooltipPanel`) alla slot **"Growth Tooltip Text"**

---

## Fase 4: Verificare la Label Growth

### 4.1 Trovare la Label Growth
✅ **La label Growth ESISTE GIÀ nella scena!**

Percorso esatto nella gerarchia:
```
Canvas
└── UI_PotDetails
    └── Panel
        └── Right
            └── Progress
                └── GrowthLabel (TextMeshProUGUI)
```

### 4.2 Verificare che la Label sia Configurata Correttamente
1. Nella Hierarchy, naviga: `Canvas` → `UI_PotDetails` → `Panel` → `Right` → `Progress` → `GrowthLabel`
2. Seleziona `GrowthLabel`
3. Verifica nell'Inspector:
   - Il GameObject è **attivo** (checkbox `Active` checked)
   - Il componente **TextMeshProUGUI** è **enabled**
   - Il testo può essere qualsiasi (verrà aggiornato in runtime dal codice)

### 4.3 Se la Label Non Esiste (Raro)
Se per qualche motivo `GrowthLabel` non esiste:
1. Nella Hierarchy, trova `UI_PotDetails` → `Panel` → `Right` → `Progress`
2. Click destro su `Progress` → `UI → Text - TextMeshPro`
3. Rinomina: `GrowthLabel`
4. Configura il testo:
   - **Text**: `Growth: Stabile` (placeholder, verrà aggiornato in runtime)
   - **Font Size**: `14`
   - **Color**: `(255, 255, 255, 255)` (bianco)
   - Posiziona dove preferisci nel panel `Progress`

---

## Fase 5: Verificare il Canvas

### 5.1 Verificare GraphicRaycaster
✅ **Il Canvas principale ha già il GraphicRaycaster!**

1. Seleziona il `Canvas` principale (quello con `GraphicRaycaster` e `EventSystem` come child)
2. Nell'Inspector, verifica che ci sia il componente **GraphicRaycaster**
3. Se per qualche motivo manca, click su `Add Component` → `Event → Graphic Raycaster`

### 5.2 Verificare EventSystem
✅ **L'EventSystem esiste già come child del Canvas!**

1. Nella Hierarchy, sotto `Canvas`, verifica che esista un GameObject `EventSystem`
2. Se per qualche motivo manca:
   - Click destro su `Canvas` → `UI → Event System`
   - Oppure Unity lo creerà automaticamente quando aggiungi un UI Button

---

## Fase 6: Test in Play Mode

### 6.1 Avviare Play Mode
1. Premi `Play` in Unity
2. Seleziona un vaso con una pianta
3. Passa il mouse sulla label `Growth: [stato]`

### 6.2 Verificare il Tooltip
- Il tooltip dovrebbe apparire centrato in alto dello schermo
- Dovrebbe mostrare informazioni dettagliate su Acqua, Luce, Fertilizzante
- Quando sposti il mouse fuori dalla label, il tooltip dovrebbe scomparire

### 6.3 Debug se Non Funziona
1. Controlla la Console di Unity per eventuali warning
2. Verifica che:
   - `GrowthTooltipPanel` sia assegnato nell'Inspector di `PotDetailsWidget`
   - `GrowthTooltipText` sia assegnato (o trovato automaticamente come child)
   - La label Growth esista e sia attiva
   - Il Canvas abbia un `GraphicRaycaster`

---

## Note Importanti

- **Il tooltip NON viene più creato in runtime**: deve essere creato manualmente in Unity
- **Il tooltip è inizialmente nascosto**: viene mostrato solo al passaggio del mouse sulla label Growth
- **L'EventTrigger viene aggiunto automaticamente**: il codice aggiunge l'EventTrigger alla label Growth quando trova il tooltip panel
- **Il testo viene aggiornato in runtime**: il contenuto del tooltip viene generato dinamicamente in base allo stato della pianta

---

## Struttura Gerarchica Finale

### Struttura Completa (basata sulla gerarchia reale della scena)

```
Canvas
├── EventSystem
├── BTN_EndDay
├── UISeedSelector
├── UI_Inventory
├── HUD
├── UI_PotDetails
│   └── Panel
│       ├── Left
│       │   ├── _blueLedButtonLight
│       │   ├── _redLedButtonLight
│       │   ├── Id
│       │   ├── PlantDescription
│       │   ├── FertilizerLevel:
│       │   ├── Hydration
│       │   ├── Lighting
│       │   ├── PhDrift
│       │   ├── PlantLevelText
│       │   ├── MoldRiskText
│       │   └── InfestationBadge
│       ├── Image
│       └── Right
│           ├── Progress
│           │   └── GrowthLabel ✅ (ESISTE GIÀ)
│           ├── Stage
│           ├── phAffinity
│           ├── Rarity
│           ├── Enviroments Effects
│           ├── Condition
│           ├── ConditionBar
│           └── ConditionForecast
│       └── Bottom
│           ├── Plant
│           ├── btnPruning
│           ├── Watering
│           ├── Uproot
│           ├── Spray Antifungal
│           ├── HarvestButton
│           └── Fertilizer
├── ... (altri elementi UI)
└── GrowthTooltipPanel (DA CREARE - inizialmente disattivato)
    └── GrowthTooltipText (DA CREARE)
```

### Struttura Minima per il Tooltip

```
Canvas
└── GrowthTooltipPanel (inizialmente disattivato)
    └── GrowthTooltipText
```

---

## Troubleshooting

### Il tooltip non appare
1. Verifica che `GrowthTooltipPanel` sia assegnato nell'Inspector
2. Verifica che la label Growth esista e sia attiva
3. Controlla la Console per warning su `_growthLabelText` o `_growthTooltipPanel`

### Il tooltip appare sempre visibile
1. Verifica che `GrowthTooltipPanel` sia disattivato inizialmente (uncheck Active)
2. Il codice lo attiva/disattiva automaticamente al passaggio del mouse

### Il tooltip non si posiziona correttamente
1. Verifica le impostazioni di **Anchor** e **Pivot** del RectTransform
2. Il tooltip dovrebbe essere ancorato in alto al centro (`Top Center`)
3. Posizione Y: `-150` per offset dall'alto

