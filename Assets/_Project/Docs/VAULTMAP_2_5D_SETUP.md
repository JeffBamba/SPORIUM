# VaultMap 2.5D (Opzione B Trapezio) — Istruzioni di Setup (con Collider + Slide)

Queste istruzioni servono per configurare **SCN_VaultMap** in modalità “2.5D” (profondità finta) usando:
- **WalkArea trapezoidale** per stanza (4 corner)
- **Collider solidi** per impedire al player di passare dentro muri/oggetti (es. letto)
- Movimento con **slide lungo i bordi**

## Prerequisiti (cosa deve esistere)
- Scena: `Assets/_Project/Scenes/SCN_VaultMap.unity`
- Player: GameObject `PLY_Player` con:
  - `Rigidbody2D`
  - `CapsuleCollider2D`
  - (già presente) `PlayerClickMover2D` (verrà sospeso automaticamente dal router)
- Background: `VaultMap_BG` (SpriteRenderer) — utile per allineare i corner.

## Script usati (dove stanno)
- WalkArea trapezio:
  - `Assets/_Project/Scripts/World/VaultMap/PerspectiveWalkArea2D.cs`
- Mover prospettico + slide:
  - `Assets/_Project/Scripts/Player/PlayerPerspectiveMover2D.cs`
- Scale + sorting in base alla profondità:
  - `Assets/_Project/Scripts/Player/PlayerDepthScaleAndSort.cs`
- Router per evitare doppio mover:
  - `Assets/_Project/Scripts/Player/PlayerMoverRouter2D.cs`

## 0) Layer consigliati (una volta sola)
1. Apri **Edit → Project Settings → Tags and Layers**.
2. Crea (se non esiste) un layer chiamato `WalkBlocker`.
3. Apri **Edit → Project Settings → Physics 2D**.
4. Nella matrice delle collisioni verifica che:
   - layer del Player (qualsiasi sia) **collida** con `WalkBlocker`.

> Nota: puoi anche non creare il layer e usare un altro layer esistente, ma poi devi impostare correttamente il `blockerMask` nel mover.

## 1) Setup WalkArea per stanza (ripeti per ogni stanza)
Esempi di stanze in gerarchia: `ROOM_Bed`, `ROOM_Dome`, `ROOM_Refrigerated`, `ROOM_Visitor`, ecc.

Per ogni stanza:
1. In Hierarchy seleziona la stanza (es. `ROOM_Bed`).
2. Crea un GameObject vuoto: **Create Empty** → rinominalo `WalkAreaPerspective`.
3. Sotto `WalkAreaPerspective` crea 4 child vuoti (solo Transform):
   - `NearLeft`
   - `NearRight`
   - `FarLeft`
   - `FarRight`
4. **Posiziona i 4 corner** sugli angoli del “pavimento” della stanza (seguendo l’immagine di background):
   - `Near*` = parte più **vicina** alla camera (di solito più in basso)
   - `Far*` = parte più **lontana** (di solito più in alto)
5. Aggiungi il componente **script** `PerspectiveWalkArea2D` su `WalkAreaPerspective`.
6. Trascina e assegna i 4 Transform nei campi:
   - NearLeft, NearRight, FarLeft, FarRight

### (Opzionale ma consigliato) Bounds collider per selezione stanza
Serve per far sì che il mover “capisca” la stanza tramite overlap/click/trigger.
1. Sul GameObject `WalkAreaPerspective` aggiungi un `BoxCollider2D` (o `PolygonCollider2D`).
2. Imposta **Is Trigger = ON**.
3. Ridimensiona il collider per coprire l’area generale della stanza.
4. Nel componente `PerspectiveWalkArea2D`, assegna questo collider nel campo **Area Bounds**.

## 2) Collider di blocco: muri (wall) + arredi non calpestabili
### A) Creare un contenitore
1. In Hierarchy crea un GameObject vuoto: `WalkColliders` (puoi metterlo in root o sotto la stanza).

### B) Muri verticali (come le linee gialle nel tuo screenshot)
Per ogni “muro”:
1. Sotto `WalkColliders` crea un child vuoto: es. `WALL_Left` / `WALL_Right` / `WALL_Split_01`.
2. Aggiungi `EdgeCollider2D`.
3. Modifica i **2 punti** dell’EdgeCollider2D per farlo coincidere con la linea (alto/basso).
4. Imposta:
   - **Is Trigger = OFF**
   - Layer = `WalkBlocker`

### C) Letto / oggetti non calpestabili
Per il letto (es. stanza `ROOM_Bed`):
1. Sotto `WalkColliders` crea un child vuoto: `BLK_Bed`.
2. Aggiungi un collider:
   - `PolygonCollider2D` (consigliato) per sagomare bene
   - oppure `BoxCollider2D` (più veloce)
3. Sagoma il collider sull’ingombro “a terra” del letto (non serve seguire tutto lo sprite, solo la zona proibita).
4. Imposta:
   - **Is Trigger = OFF**
   - Layer = `WalkBlocker`

> Nota: se il GameObject `Bed` esistente usa già un collider per interazione, puoi lasciarlo com’è e aggiungere questo collider “BLK_*” dedicato al movimento.

## 3) Setup Player (una volta, in VaultMap)
1. Seleziona `PLY_Player`.
2. Aggiungi i componenti (Add Component):
   - `PlayerPerspectiveMover2D`
   - `PlayerDepthScaleAndSort`
   - `PlayerMoverRouter2D`
3. Configura `PlayerPerspectiveMover2D`:
   - `blockerMask` → includi il layer `WalkBlocker`
   - (opzionale) `requireWalkableForClick` = OFF (default). Accendilo solo se hai un “floor collider” dedicato e vuoi validare i click.
4. Configura `PlayerDepthScaleAndSort`:
   - assegna lo `SpriteRenderer` del child `Sprite` (se non viene trovato automaticamente)
   - regola:
     - `scaleNear` / `scaleFar`
     - `baseOrder` / `range`

### WalkArea iniziale
Se il player spawna sempre nella stessa stanza puoi:
- assegnare manualmente `currentWalkArea` nel `PlayerPerspectiveMover2D`
oppure
- lasciare vuoto e usare i **bounds collider** (AreaBounds) + trigger per farlo switchare automaticamente.

## 4) Test rapido (Play Mode)
1. Premi **Play**.
2. Prova:
   - **WASD**: muovi in profondità (asse v) e lateralmente (asse u)
   - **Click**: clicca sul pavimento della stanza per far muovere il player
3. Verifica:
   - Il player **scivola** lungo muri e bordi del letto (slide)
   - Il player **non entra** dentro il letto
   - Scale e sorting cambiano con la profondità

## 5) Tuning (quello che farai spesso)
- Se la prospettiva “non vende”:
  - sposta i corner `Near*/Far*` finché il pavimento risulta credibile
- Se lo slide “gratta”:
  - controlla che i collider dei muri/letto siano **Is Trigger OFF**
  - aumenta leggermente `skinWidth` nel `PlayerPerspectiveMover2D` (es. 0.02 → 0.03)
  - semplifica il `PolygonCollider2D` del letto (meno vertici = meno incastri)
- Se il player si ferma troppo presto sul target:
  - diminuisci `stopDistance` (ma non troppo, o rischi jitter)

## Troubleshooting (problemi comuni)
- **Il player non si muove**:
  - verifica che `PlayerPerspectiveMover2D` sia abilitato
  - verifica che esista almeno una WalkArea con corner assegnati
- **Il player attraversa muri/letto**:
  - i collider devono essere **Is Trigger OFF**
  - il layer dei collider deve essere incluso in `blockerMask`
  - la collision matrix di Physics2D deve permettere Player vs WalkBlocker
- **Il click non funziona**:
  - se `requireWalkableForClick` è ON, assicurati di avere un collider “walkable” nel `walkableMask`
  - assicurati di non cliccare sopra UI (EventSystem blocca il click)

## Rollback (tornare al comportamento precedente)
In `SCN_VaultMap`:
- Disabilita `PlayerPerspectiveMover2D` (e/o `PlayerMoverRouter2D`) su `PLY_Player` → torna il mover classico.
- (Opzionale) Disabilita/elimina `WalkColliders` → rimuove blocchi fisici aggiuntivi.

