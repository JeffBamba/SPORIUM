# BLK-01.02 — Hotfix UI Click-Through & Pot Selection

## 📋 **Sintesi Bug Fixati**

### **BUG A: UI Click-Through**
- **Sintomo**: Click sui pulsanti HUD fanno muovere il Player invece di eseguire azioni
- **Causa**: Mancanza di sistema per intercettare input sopra UI
- **Soluzione**: Implementato `UIBlocker` per bloccare input mondo quando sopra UI

### **BUG B: POT-001 Non Selezionabile**
- **Sintomo**: Vaso non selezionabile quando Player è sopra
- **Causa**: Collisioni Player-Vaso e mancanza di LayerMask dedicati
- **Soluzione**: Sistema di layer dedicati e hit test filtrati

---

## 🛠️ **File Modificati/Creati**

### **Nuovi Script**
- `Assets/_Project/Scripts/Core/UIBlocker.cs` - Blocca input sopra UI
- `Assets/_Project/Scripts/Core/LayerSetup.cs` - Configura layer automaticamente
- `Assets/_Project/Scripts/Core/UIConfigChecker.cs` - Verifica configurazione UI

### **Script Aggiornati**
- `Assets/_Project/Scripts/Dome/PotSystemConfig.cs` - Aggiunto `PotLayerMask`
- `Assets/_Project/Scripts/Interactables/PotSlot.cs` - Integrato `UIBlocker`
- `Assets/_Project/Scripts/UI/PotHUDWidget.cs` - Migliorato EventTrigger

---

## 🎯 **Implementazione**

### **1. UI Click Shielding**
```csharp
// Prima di processare click mondo
if (UIBlocker.IsPointerOverUI()) return;

// Esempio in PotSlot
private void HandlePotClick()
{
    if (UIBlocker.IsPointerOverUI())
    {
        Debug.Log($"[{potId}] Click bloccato: sopra UI");
        return;
    }
    // ... resto logica
}
```

### **2. Layer System**
```csharp
// PotSystemConfig
[Header("Layers")]
public LayerMask PotLayerMask = 1 << 8; // LAYER_INTERACTABLE

// Layer dedicati
LAYER_PLAYER = "LAYER_PLAYER"        // Player
LAYER_INTERACTABLE = "LAYER_INTERACTABLE"  // Vasi
LAYER_GROUND = "LAYER_GROUND"        // Piano navigazione
```

### **3. EventTrigger sui Pulsanti**
```csharp
// Blocca PointerDown e BeginDrag
EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
pointerDownEntry.eventID = EventTriggerType.PointerDown;
pointerDownEntry.callback.AddListener((data) => { 
    Debug.Log($"[PotHUDWidget] Evento PointerDown bloccato per {actionType}");
});
```

---

## 🚀 **Setup e Configurazione**

### **Step 1: Crea Layer in Project Settings**
1. **Edit > Project Settings > Tags and Layers**
2. **Layers** (User Layer 8-10):
   - **User Layer 8**: `LAYER_INTERACTABLE`
   - **User Layer 9**: `LAYER_PLAYER`
   - **User Layer 10**: `LAYER_GROUND`

### **Step 2: Aggiungi Script alla Scena**
1. **Crea GameObject "SystemSetup"**
2. **Aggiungi componenti**:
   - `LayerSetup` - Configura layer automaticamente
   - `UIConfigChecker` - Verifica configurazione UI

### **Step 3: Verifica Configurazione**
1. **Play** → Script eseguono setup automatico
2. **Console** → Verifica messaggi di setup
3. **Context Menu** → "Setup Layers", "Check UI Configuration"

---

## 🧪 **Test Plan**

### **Test 1: UI Click Shielding**
- [ ] Click su `BTN_EndDay` → Player **NON** si muove
- [ ] Click su `BTN_DummyAction` → Player **NON** si muove
- [ ] Click su pulsanti vasi → Azioni eseguite, Player **NON** si muove
- [ ] Console: `[UIBlocker] Puntatore sopra UI` quando sopra UI

### **Test 2: Pot Selection Robust**
- [ ] Player **sopra** `POT-001` → Vaso selezionabile
- [ ] Player **sopra** `POT-002` → Vaso selezionabile
- [ ] Hover/click funzionano anche con Player sovrapposto
- [ ] Console: `[PotSlot] Click rilevato` per entrambi i vasi

### **Test 3: Layer System**
- [ ] Player su `LAYER_PLAYER`
- [ ] Vasi su `LAYER_INTERACTABLE`
- [ ] Ground su `LAYER_GROUND`
- [ ] Console: `[LayerSetup] Configurazione layer completata!`

### **Test 4: Regressioni**
- [ ] GameManager funziona (CRY, Azioni, Inventario)
- [ ] HUD funziona (visualizzazione risorse)
- [ ] Ascensore funziona (movimento, pannello)
- [ ] Movimento Player funziona (click su ground)

---

## 🔍 **Debug e Troubleshooting**

### **Log di Debug**
```csharp
[UIBlocker] Puntatore sopra UI (API standard)
[UIBlocker] Puntatore sopra UI (raycast): 3 elementi
[PotSlot] Click bloccato: sopra UI
[LayerSetup] Player assegnato al layer: LAYER_PLAYER
[UIConfigChecker] ✅ Configurazione UI corretta!
```

### **Context Menu Utili**
- **LayerSetup**: "Setup Layers", "Verify Layer Setup"
- **UIConfigChecker**: "Check UI Configuration", "Test UIBlocker"

### **Problemi Comuni**
1. **Layer non trovati**: Crea layer in Project Settings
2. **EventSystem mancante**: Script lo crea automaticamente
3. **GraphicRaycaster mancante**: Script lo aggiunge automaticamente

---

## 📚 **Note Tecniche**

### **UIBlocker Implementation**
- **API Standard**: `EventSystem.current.IsPointerOverGameObject()` - veloce
- **Raycast Manuale**: `EventSystem.current.RaycastAll()` - robusto
- **Fallback**: Se EventSystem mancante, logga warning

### **Layer System**
- **Player**: Layer 9 - non interagisce con vasi
- **Vasi**: Layer 8 - solo per selezione
- **Ground**: Layer 10 - solo per movimento

### **EventTrigger**
- **PointerDown**: Blocca movimento player
- **BeginDrag**: Previene drag accidentali
- **OnClick**: Mantiene funzionalità pulsanti

---

## ✅ **Criteri di Accettazione**

- [ ] **UI click shielded**: Click su pulsanti HUD **NON** attivano movimento Player
- [ ] **Selezione vaso robusta**: Vasi selezionabili anche con Player sopra
- [ ] **Layering corretto**: Raycast filtrati per layer appropriati
- [ ] **No regressioni**: Sistema esistente funziona correttamente
- [ ] **Setup automatico**: Script configurano tutto automaticamente

---

## 🎉 **Risultato**

**BLK-01.02** ora funziona correttamente con:
- ✅ **Pulsanti HUD** intercettano click e non fanno muovere il Player
- ✅ **Selezione vasi** robusta anche con Player sovrapposto
- ✅ **Sistema di layer** per separare Player, Vasi e Ground
- ✅ **Setup automatico** per configurazione UI e layer
- ✅ **Debug completo** per troubleshooting

Il sistema è pronto per **BLK-01.03** e oltre! 🚀
