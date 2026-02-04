---
name: Camera zoom profondità 2.5D
overview: "Aggiungere allo zoom della camera il follow \"in profondità\": quando il player si avvicina alle stanze in fondo (v alto), la camera riduce l'orthographic size per mostrare più dettaglio; quando è in primo piano (v basso), mantiene la vista più ampia."
todos: []
isProject: false
---

# Piano: Camera zoom in profondità (2.5D)

## Contesto

- **[CameraFollow2D.cs](Assets/_Project/Scripts/CameraFollow2D.cs)** segue già il player su X e Y; la Z della camera è fissa. Non modifica `orthographicSize`.
- **[PlayerPerspectiveMover2D.cs](Assets/_Project/Scripts/Player/PlayerPerspectiveMover2D.cs)** espone `**CurrentV**` (0 = primo piano, 1 = verso le stanze in fondo). La profondità è già usata da [PlayerDepthScaleAndSort.cs](Assets/_Project/Scripts/Player/PlayerDepthScaleAndSort.cs) per scala e sorting.
- **[PerspectiveWalkArea2D.cs](Assets/_Project/Scripts/World/VaultMap/PerspectiveWalkArea2D.cs)** definisce l’area trapezoidale UV (u = sinistra-destra, v = vicino-lontano).

## Obiettivo

Far sì che la camera “segua” il player anche in profondità: **v alto** → zoom in (orthographic size minore, più dettaglio sulla stanza); **v basso** → zoom out (size maggiore, vista ampia). Transizione smooth per evitare salti.

## Approccio consigliato

Estendere **CameraFollow2D** (un solo punto di logica camera) invece di introdurre un componente separato, a meno che non si voglia tenere zoom e follow completamente disaccoppiati.

## Implementazione

### 1. Estendere CameraFollow2D

- Aggiungere **riferimento opzionale** a `PlayerPerspectiveMover2D` (o a un’interfaccia `IDepthProvider` con `float GetDepthV()`). Se null, il comportamento zoom non viene applicato (retrocompatibilità).
- Aggiungere **parametri Inspector**:
  - `enableDepthZoom` (bool, default true se mover assegnato).
  - `orthoSizeNear` (float): orthographic size quando il player è “in fondo” (v = 1) — zoom in, valore **minore**.
  - `orthoSizeFar` (float): orthographic size quando il player è in primo piano (v = 0) — zoom out, valore **maggiore**.
  - `zoomSmoothTime` (float): tempo di smoothing per `orthographicSize` (es. 0.2–0.4 s).
- In **LateUpdate**, dopo aver aggiornato la posizione:
  - Se `enableDepthZoom` e riferimento al mover valido, leggere `CurrentV` (clamp 0..1).
  - `targetSize = Mathf.Lerp(orthoSizeFar, orthoSizeNear, v)`.
  - Applicare a `Camera.orthographicSize` con **SmoothDamp** (variabile di stato per velocity dello zoom).
- Validare in Start: se `enableDepthZoom` è true ma il mover è null, tentare `GetComponent` sul target o `FindObjectOfType<PlayerPerspectiveMover2D>()` una tantum; in caso di fallback disabilitare solo lo zoom e loggare un warning.

### 2. Gestione camera

- Verificare che la camera usata sia **ortografica** (tipico per 2.5D). Se lo script è sulla Main Camera, usare `GetComponent<Camera>()` e controllare `camera.orthographic` prima di scrivere `orthographicSize`; se non ortografica, saltare l’aggiornamento dello zoom.

### 3. Valori di default e tuning

- Leggere l’`orthographicSize` corrente della camera in **Start** e usarla come `orthoSizeFar` di default (primo piano). Per `orthoSizeNear` usare una frazione (es. 0.6–0.75) di quel valore, regolabile in Inspector.
- Documentare in header/tooltip: “orthoSizeNear = zoom quando il player è verso le stanze in fondo (v=1); orthoSizeFar = vista ampia in primo piano (v=0)”.

### 4. Opzionale (non obbligatorio per il primo rilascio)

- **Offset XY in base a v**: se in seguito si vuole “centrare” meglio la stanza quando si è in fondo, si può aggiungere un offset opzionale alla posizione target in `CalculateTargetPosition()` in funzione di v (es. spostamento verso il centro della stanza quando v > soglia).

## File da modificare


| File                                                                                   | Modifica                                                                                              |
| -------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| [Assets/_Project/Scripts/CameraFollow2D.cs](Assets/_Project/Scripts/CameraFollow2D.cs) | Aggiungere riferimento a depth provider, parametri zoom, logica in LateUpdate e validazione in Start. |


## Dipendenze

- Nessuna modifica a `PlayerPerspectiveMover2D`, `PerspectiveWalkArea2D` o `PlayerDepthScaleAndSort`: si usa solo l’API esistente (`CurrentV`).
- La Main Camera deve essere ortografica; in caso contrario lo zoom viene disabilitato senza errori.

## Test suggeriti

- Con player in primo piano (v ≈ 0): orthographic size deve restare o tornare a `orthoSizeFar`.
- Muovendo il player verso le stanze in fondo (v → 1): size deve diminuire in modo fluido verso `orthoSizeNear`.
- Con `enableDepthZoom` false o mover non assegnato: nessun cambiamento di zoom, comportamento identico all’attuale.

