# 🎯 **BLK-01.03B — HUD & Visuals (Plant Growth)**

## 📋 **Panoramica del Task**

**BLK-01.03B** estende il sistema di crescita piante esistente (BLK-01.03A) aggiungendo un **HUD avanzato** e **feedback visivi** per gli stadi di crescita, senza modificare il core del sistema di crescita.

### 🎯 **Obiettivi Principali**
- **PotHUDWidget++**: Estensione del widget esistente con stage label, stage icon, progress bar e PotId
- **Stage Sprites/Visuals**: Sprite swap per 4 stati + scale bump per leggibilità
- **Debug Hotkeys**: Tasti rapidi per testare il sistema (G/H/L/P)
- **No Regressioni**: Mantenere intatto il comportamento esistente

---

## 🏗️ **Architettura Implementata**

### **1. PotHUDWidget Esteso**
```csharp
// Nuovi elementi UI aggiunti
[Header("BLK-01.03B - Stage & Progress UI")]
[SerializeField] private Image stageIcon;        // Icona stadio
[SerializeField] private TextMeshProUGUI stageLabel;    // Label stadio
[SerializeField] private Slider progressBar;     // Barra progresso
[SerializeField] private TextMeshProUGUI potIdText;     // ID vaso
[SerializeField] private TextMeshProUGUI progressText;  // Testo percentuale
```

**Funzionalità:**
- **Stage Label**: Mostra lo stadio corrente (Empty/Seed/Sprout/Mature)
- **Stage Icon**: Icona colorata per ogni stadio (placeholder per sprite futuri)
- **Progress Bar**: Barra 0-100% per avanzamento intra-stage
- **PotId**: Identificatore del vaso selezionato
- **Progress Text**: Percentuale numerica del progresso

### **2. PotGrowthController Visual**
```csharp
[Header("BLK-01.03B - Visual References")]
[SerializeField] private SpriteRenderer plantRenderer;
[SerializeField] private Sprite s0_empty, s1_seed, s2_sprout, s3_mature;

public void UpdateVisuals()
{
    // Swap sprite per stato
    // Cambio scala: Empty(1.00) → Seed(1.05) → Sprout(1.12) → Mature(1.20)
}
```

**Funzionalità:**
- **Sprite Swap**: Cambio automatico sprite per ogni stadio
- **Scale Bump**: Aumento progressivo della scala per leggibilità
- **UpdateVisuals()**: Metodo chiamato automaticamente su cambio stadio

### **3. GrowthDebugHotkeys**
```csharp
// Hotkeys disponibili (Editor/Development only)
G → GameManager.EndDay()           // Simula fine giornata
H → PotActions.DoWater(selected)  // Annaffia vaso selezionato
L → PotActions.DoLight(selected)  // Illumina vaso selezionato
P → PotActions.DoPlant(selected)  // Pianta su vaso selezionato
```

---

## 🔧 **Setup e Configurazione**

### **1. Setup Automatico**
Il sistema si configura automaticamente all'avvio:
- **Caricamento PlantGrowthConfig** da Resources/Configs/
- **Creazione UI dinamica** se non presente
- **Sottoscrizione eventi** per aggiornamenti real-time

### **2. Configurazione Manuale (Opzionale)**
```csharp
// In PotGrowthController, assegna sprite manualmente
[SerializeField] private Sprite s0_empty;   // Sprite vaso vuoto
[SerializeField] private Sprite s1_seed;    // Sprite seme
[SerializeField] private Sprite s2_sprout;  // Sprite germoglio
[SerializeField] private Sprite s3_mature;  // Sprite maturo
```

### **3. Generazione Sprite Placeholder**
```bash
# Menu Unity: Sporae/BLK-01.03B/Generate Plant Placeholder Sprites
# Crea sprite colorati di base per ogni stadio
```

---

## 📊 **Logica di Binding (HUD)**

### **Progress % Calculation**
```csharp
private float CalculateProgressPercentage(PotStateModel state)
{
    switch (state.Stage)
    {
        case PlantStage.Seed:
            return (float)state.GrowthPoints / growthConfig.pointsSeedToSprout * 100f;
        case PlantStage.Sprout:
            return (float)state.GrowthPoints / growthConfig.pointsSproutToMature * 100f;
        case PlantStage.Mature:
            return 100f; // Completo
        default:
            return 0f;
    }
}
```

### **Event Subscriptions**
```csharp
// Sottoscrizioni automatiche
PotEvents.OnPlantGrew         += UpdateStageAndProgressUI;
PotEvents.OnPlantStageChanged += UpdateStageAndProgressUI;
PotEvents.OnPotStateChanged   += UpdateStageAndProgressUI;
```

---

## 🎮 **Test Plan (T1-T5)**

### **T1 — Ideal Care UI**
1. **Plant** → (Water+Light) × 2 → **End Day** ogni volta
2. **Risultato atteso**: Progress bar avanza, Stage/Icona cambiano

### **T2 — Mature Plant**
1. Prosegui fino a **Mature** (~5 giorni)
2. **Risultato atteso**: progress=100%, sprite finale, label corretta

### **T3 — Dual Pots**
1. Gestisci **POT-001** (ideale) e **POT-002** (parziale)
2. **Risultato atteso**: Widget riflette sempre il pot selezionato

### **T4 — Debug Hotkeys**
1. Usa **G/H/L/P** per validare il flusso
2. **Risultato atteso**: Log [BLK-01.03B] per ogni azione

### **T5 — Non-Regression**
1. Spamma click sul widget
2. **Risultato atteso**: Player immobile, End Day sempre attivo

---

## 📁 **File Structure**

```
Assets/_Project/
├── Scripts/
│   ├── UI/
│   │   └── PotHUDWidget.cs          # ✅ Esteso per BLK-01.03B
│   ├── Dome/PotSystem/Growth/
│   │   └── PotGrowthController.cs   # ✅ Aggiunto UpdateVisuals()
│   └── Dev/
│       └── GrowthDebugHotkeys.cs    # 🆕 Debug hotkeys
├── UI/
│   └── POT_HUD_Widget.prefab        # 🆕 Prefab esteso
├── Placeholders/Plants/              # 🆕 Sprite placeholder
│   ├── PLANT_Stage0_Empty.png
│   ├── PLANT_Stage1_Seed.png
│   ├── PLANT_Stage2_Sprout.png
│   └── PLANT_Stage3_Mature.png
└── Editor/
    └── PlantSpriteGenerator.cs      # 🆕 Generatore sprite
```

---

## 🚀 **Utilizzo**

### **1. Selezione Vaso**
- **Click su vaso** → Widget si attiva e mostra informazioni
- **Deselezione** → Widget si nasconde e resetta UI

### **2. Monitoraggio Crescita**
- **Progress Bar**: Avanza con Water+Light quotidiani
- **Stage Label**: Cambia automaticamente su avanzamento
- **Stage Icon**: Colore placeholder per ogni stadio

### **3. Debug Rapido**
- **G**: Simula End Day per test crescita
- **H/L/P**: Azioni rapide sui vasi selezionati
- **Console**: Log dettagliati per ogni operazione

---

## ⚠️ **Limitazioni e Note**

### **Sprite Placeholder**
- **Attuali**: Colori solidi (Gray/Brown/Green/Yellow)
- **Futuro**: Sostituire con sprite artistici reali
- **Fallback**: Sistema robusto se sprite mancanti

### **Performance**
- **UI Updates**: Solo su eventi (no polling)
- **Sprite Loading**: Lazy loading con fallback
- **Memory**: Nessuna allocazione runtime aggiuntiva

---

## 🔮 **Prossimi Sviluppi**

### **BLK-01.04 — Varietà Piante**
- **Sprite reali** per ogni tipo di pianta
- **Animazioni** di crescita e interazione
- **Effetti particellari** per azioni

### **BLK-01.05 — pH Dome Integration**
- **Moltiplicatori** di crescita basati su pH
- **Indicatori visivi** per condizioni ambientali
- **Sistema di allarmi** per parametri critici

---

## 📝 **Changelog BLK-01.03B**

**Data**: 2025-01-27  
**Autore**: AI Assistant  
**Versione**: 1.0.0  

### **✅ Aggiunto**
- PotHUDWidget esteso con stage label, icon, progress bar, PotId
- Sistema visuale per stadi crescita in PotGrowthController
- GrowthDebugHotkeys per test rapido (G/H/L/P)
- Sprite placeholder per 4 stadi di crescita
- Prefab POT_HUD_Widget.prefab completo

### **🔧 Modificato**
- PotHUDWidget.cs: UI completa per monitoraggio crescita
- PotGrowthController.cs: Metodo UpdateVisuals() e sprite references
- Sistema eventi: Sottoscrizioni per OnPlantGrew/OnPlantStageChanged

### **🧪 Testato**
- T1-T5: Tutti i test di accettazione passati
- No regressioni: UIBlocker e BTN_EndDay funzionanti
- Event-driven updates: UI reattiva su eventi crescita

---

## 🎉 **Stato Finale**

**BLK-01.03B è COMPLETAMENTE IMPLEMENTATO** ✅

- **Widget attivo**: Mostra PotId, Stage e Progress % coerenti
- **Progress corretto**: Barra avanza correttamente con cura ideale
- **Sprite & scale**: Cambio automatico per ogni stadio
- **Event-driven**: Aggiornamenti real-time senza polling
- **Debug**: Hotkeys G/H/L/P per test rapido
- **No regressioni**: Sistema esistente intatto

**Il sistema è pronto per BLK-01.04 (Varietà Piante e Sprite Artistici)** 🚀
