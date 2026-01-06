# Fix Gerarchia UI Toolkit - Sorting Order

## Problema Identificato

I GameObject UI Toolkit (`PlantCardV2`, `PotActionsMenu`, `SeedInventoryMenu`, `IrrigationDialog`, `UIAdditiveSelector`) sono attualmente alla **radice della scena** invece che sotto il **Canvas principale**. Questo causa problemi di sorting order perché Unity crea automaticamente un Canvas separato per loro.

## Soluzione: Riorganizzare la Gerarchia

### Struttura Corretta

Tutti i GameObject UI Toolkit devono essere **figli diretti del Canvas principale**:

```
Canvas (quello con EventSystem come child)
├── EventSystem
├── HUD_TopBar (già corretto ✅)
├── HUD_BottomNavigation (già corretto ✅)
├── PlayerStatusPanel (già corretto ✅)
├── PlantCardV2 (DA SPOSTARE)
├── PotActionsMenu (DA SPOSTARE)
├── SeedInventoryMenu (DA SPOSTARE)
├── IrrigationDialog (DA SPOSTARE)
└── UIAdditiveSelector (DA SPOSTARE)
```

## Istruzioni Manuali (Principiante)

### Passo 1: Trovare il Canvas Principale

1. Apri Unity
2. Nella **Hierarchy**, cerca il GameObject chiamato `Canvas`
3. **Verifica** che abbia `EventSystem` come figlio diretto (questo conferma che è il Canvas principale)
4. Se non lo trovi, cerca un GameObject con:
   - Componente `Canvas`
   - Componente `CanvasScaler`
   - Componente `GraphicRaycaster`
   - Un figlio chiamato `EventSystem`

### Passo 2: Spostare PlantCardV2

1. Nella **Hierarchy**, trova `PlantCardV2` (dovrebbe essere alla radice, stesso livello di `GameManager`)
2. **Click e tieni premuto** sul GameObject `PlantCardV2`
3. **Trascina** `PlantCardV2` sopra il GameObject `Canvas`
4. **Rilascia** quando vedi che `Canvas` si evidenzia
5. **Verifica** che `PlantCardV2` sia ora un figlio di `Canvas` (deve essere indentato sotto `Canvas`)

### Passo 3: Spostare PotActionsMenu

1. Nella **Hierarchy**, trova `PotActionsMenu`
2. **Trascina** `PotActionsMenu` sopra `Canvas` (come fatto per `PlantCardV2`)
3. **Rilascia** quando `Canvas` si evidenzia
4. **Verifica** che sia un figlio di `Canvas`

### Passo 4: Spostare SeedInventoryMenu

1. Nella **Hierarchy**, trova `SeedInventoryMenu`
2. **Trascina** `SeedInventoryMenu` sopra `Canvas`
3. **Rilascia** quando `Canvas` si evidenzia
4. **Verifica** che sia un figlio di `Canvas`

### Passo 5: Spostare IrrigationDialog

1. Nella **Hierarchy**, trova `IrrigationDialog`
2. **Trascina** `IrrigationDialog` sopra `Canvas`
3. **Rilascia** quando `Canvas` si evidenzia
4. **Verifica** che sia un figlio di `Canvas`

### Passo 6: Spostare UIAdditiveSelector

1. Nella **Hierarchy**, trova `UIAdditiveSelector`
2. **Trascina** `UIAdditiveSelector` sopra `Canvas`
3. **Rilascia** quando `Canvas` si evidenzia
4. **Verifica** che sia un figlio di `Canvas`

### Passo 7: Verifica Finale

La struttura finale dovrebbe essere:

```
Canvas
├── EventSystem
├── HUD_GameViewportBackground
├── BTN_EndDay
├── UISeedSelector
├── UI_Inventory
├── HUD
├── ... (altri UI esistenti)
├── PlayerStatusPanel
├── HUD_TopBar
├── HUD_BottomNavigation
├── PlantCardV2 ✅
├── PotActionsMenu ✅
├── SeedInventoryMenu ✅
├── IrrigationDialog ✅
└── UIAdditiveSelector ✅
```

**Come verificare:**
1. Espandi `Canvas` nella Hierarchy
2. Controlla che tutti i GameObject UI Toolkit siano figli di `Canvas`
3. Nessun GameObject UI Toolkit dovrebbe essere alla radice (stesso livello di `GameManager`)

### Passo 8: Test

1. Premi **Play** in Unity
2. Apri **Inspect** su un pot
3. **Verifica** che `PlantCardV2` sia sopra `TopBar` e `BottomNav`
4. Se il problema persiste, controlla che il `Canvas` principale non abbia un `sortingOrder` impostato manualmente nell'Inspector

## Nota Importante

Dopo aver spostato i GameObject, Unity potrebbe creare automaticamente un `Canvas` per loro se non sono sotto un Canvas. Se vedi un nuovo `Canvas` creato automaticamente:

1. **Seleziona** il nuovo Canvas
2. **Eliminalo** (Delete)
3. **Verifica** che i GameObject siano ora sotto il Canvas principale

## Perché Questo Risolve il Problema?

Quando un `UIDocument` è alla radice della scena, Unity crea automaticamente un `Canvas` per renderizzarlo. Questo Canvas ha un `sortingOrder` di default (solitamente 0) che può confliggere con il sorting order del Canvas principale.

Spostando tutti i GameObject UI Toolkit sotto lo stesso Canvas, tutti condividono lo stesso `sortingOrder` del Canvas, e il `sortingOrder` del `UIDocument` (impostato nel codice) diventa determinante per l'ordine di rendering.

