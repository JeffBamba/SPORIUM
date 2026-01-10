# Player Spawn Point System (EndDay) — Setup

Questo progetto resetta la posizione del player a fine giornata tramite `PlayerEndDayHandler`.
Ora puoi controllare lo spawn creando/spostando un GameObject con `PlayerSpawnPoint`.

---

## 1) Crea lo Spawn Point in scena

1. Apri la scena dove vuoi controllare lo spawn (es. VaultMap).
2. Nel Hierarchy: **Create Empty**
3. Rinomina il GameObject in `PLY_SpawnPoint` (nome libero, è solo per chiarezza).
4. **Add Component** → `PlayerSpawnPoint`
5. Sposta `PLY_SpawnPoint` dove vuoi che il player compaia dopo EndDay.

Se hai più spawn point:
- lascia `isActive = true` solo su quello che vuoi usare
- oppure usa `priority` (più alto = scelto)

---

## 2) (Opzionale) Elevator level

Nel componente `PlayerSpawnPoint`:
- abilita `Set Elevator Level On Spawn`
- scegli `Elevator Level` (default 0)

---

## 3) Player: EndDay handler

Il player `PLY_Player` in scena ha già `PlayerEndDayHandler`.
A fine giornata lo script:
- cerca il miglior `PlayerSpawnPoint`
- teleporta il player lì
- in VaultMap 2.5D aggiorna anche walk-area e UV (per evitare scale/sort errati dopo teleport)

---

## Debug

Lo spawn point disegna un gizmo:
- verde = attivo
- arancio = non attivo

