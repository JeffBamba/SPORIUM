# Piano fix — player invisibile/flicker ascensore

**Data piano:** 2026-06-10  
**Creato da:** indagine read-only del 2026-06-09  
**Scope:** player che sparisce/flickera vicino ai bordi walk area in `SCN_VaultMap`; sintomi ascensore da trattare come secondari/separati finche non sono riprodotti indipendentemente.

---

## Sintesi

Il bug non sembra legato al loop di animazione del player.

**Aggiornamento critico del 2026-06-10:** il bug del player che sparisce/flickera vicino al bordo della walk area accade anche con l'oggetto root `ELEV_Elevator` disattivato in Unity. Quindi l'ascensore NON puo essere la causa primaria di quel sintomo specifico. La priorita passa a:

1. `PlayerPerspectiveMover2D` e cambio/oscillazione `CurrentWalkArea` al bordo.
2. `PlayerDepthScaleAndSort` e sorting order ricalcolato da `CurrentV`.
3. Occluder/foreground/mask/sprite della scena che possono coprire il player.
4. Collider/bounds walk area e proiezione UV instabile.

L'ascensore resta una pista valida solo per eventuali sparizioni legate a viaggio/arrivo cabina, ma va separato dal bug bordo walk area.

Il video allegato e stato campionato a 1 secondo e 3 secondi: nei frame estratti il player resta visibile, quindi la sparizione sembra intermittente o sub-secondo. Serve una passata diagnostica in Play Mode per catturare il frame esatto.

---

## Evidenze principali

- `PlayerPerspectiveMover2D` cambia walk area con trigger e outward-probe; al bordo puo oscillare tra aree sovrapposte o scegliere un'area con proiezione UV diversa.
- `PlayerDepthScaleAndSort` ricalcola ogni `LateUpdate` scala e sorting in base a `CurrentV`; se `CurrentV` salta, anche lo `sortingOrder` salta.
- Il player parte con uno SpriteRenderer a sorting 10, ma in scena `PlayerDepthScaleAndSort` lo ricalcola con `baseOrder: 0` e `range: 50`.
- In scena ci sono sprite/occluder/mask con sorting molto alti; se il player scende sotto foreground/wall, puo sembrare sparire senza che il renderer venga disabilitato.
- `ElevatorSystem` puo nascondere direttamente il player, ma il test con `ELEV_Elevator` disattivato esclude questa causa per il bug bordo.

---

## Ranking cause probabili

1. Oscillazione `CurrentWalkArea` / UV al bordo in `PlayerPerspectiveMover2D`.
2. Sorting order instabile in `PlayerDepthScaleAndSort` per variazione improvvisa di `CurrentV`.
3. Player coperto da foreground/wall/mask/occluder della scena quando il sorting scende o cambia area.
4. Collider/bounds walk area non allineati al visual, con proiezione UV valida ma semanticamente sbagliata.
5. Ascensore/hide player: causa separata da verificare solo per bug durante viaggio/arrivo, non per il bug bordo con elevator spento.

---

## Piano operativo per domani

1. Riprodurre con `ELEV_Elevator` disattivato e tenere l'ascensore fuori dall'indagine del bug bordo.
   - Confermare che il bug resta.
   - Annotare punto esatto della walk area, direzione di movimento e area corrente prima/dopo il flicker.

2. Test discriminante sorting.
   - Forzare temporaneamente lo SpriteRenderer del player a sorting order molto alto, es. `999`.
   - Se il flicker sparisce: causa sorting/occlusione.
   - Se resta: causa movimento/posizione/collider/mask non legata al sorting del renderer.

3. Test discriminante `PlayerDepthScaleAndSort`.
   - Disattivare temporaneamente `PlayerDepthScaleAndSort`.
   - Se il bug cambia o sparisce: il problema e nella relazione `CurrentV -> sorting/scale`.
   - Se non cambia: guardare mover/bounds/occluder.

4. Aggiungere diagnostica temporanea e rimovibile.
   - Log `CurrentWalkArea`, `CurrentU`, `CurrentV`, world position, z, sortingOrder.
   - Log ogni cambio area da `TryCommitAreaSwitch`, con source (`position`, `outward-probe`, `trigger`), area precedente, area nuova, projection error.
   - Log `SpriteRenderer.enabled`, `sortingLayer`, `sortingOrder`, `MaskInteraction`.
   - Log eventuali `OnTriggerEnter2D` su AreaBounds coinvolti.

5. Aggiungere overlay debug sopra il player.
   - active state.
   - renderer enabled.
   - sorting order.
   - z position.
   - current walk area.
   - UV.
   - ultimo source dello switch area.
   - projection error su area corrente e area candidata.

6. Riprodurre i casi minimi.
   - Bordo sinistro/destro di `ElevatorFrontWalkArea_LVL_0`.
   - Seam DOME <-> front walk.
   - Seam front walk <-> LAB.
   - Bordo superiore/inferiore del front walk.
   - Stesso test con `PlayerDepthScaleAndSort` off.
   - Stesso test con player sorting forzato alto.

7. Se i log confermano switch area instabile.
   - Aggiungere isteresi/lock breve sul cambio area vicino ai bordi.
   - Accettare il cambio area solo se il nuovo projection error resta migliore e stabile.
   - Non usare il probe outward se l'area candidata crea un salto di UV/sorting non coerente.

8. Se i log confermano sorting/occlusione.
   - Correggere `minPlayerSortingOrder` sulle walk area coinvolte o applicare un floor minimo dedicato.
   - Evitare fix globale su tutto il player se il problema e locale al front walk.
   - Verificare gli sprite/foreground che coprono fisicamente il player.

9. Solo dopo: riaprire il capitolo ascensore.
   - Verificare se esiste un secondo bug durante viaggio/arrivo.
   - In quel caso indagare `SetPlayerHidden`, `ForceShowPlayer`, interior zone e porte come bug separato.

---

## Test di accettazione

- Il player non sparisce ai bordi della walk area ascensore.
- Nessun flicker visibile vicino ai confini del front walk.
- Con `ELEV_Elevator` disattivato, il player non sparisce ai bordi.
- Con sorting order normale, il player resta visibile davanti a foreground/wall previsti.
- `CurrentWalkArea`, `CurrentU`, `CurrentV` non oscillano frame-by-frame al bordo.
- Il cambio area non produce salti improvvisi di sorting/scala.
- Seam DOME <-> elevator <-> LAB e bedroom <-> elevator <-> cucina non regrediscono.
- Eventuale bug ascensore viene testato separatamente dopo aver chiuso il bug bordo.

---

## File da guardare per primi

- `Assets/_Project/Scripts/World/Elevator/ElevatorSystem.cs`
- `Assets/_Project/Scripts/World/Elevator/ElevatorCabinInteriorZone.cs`
- `Assets/_Project/Scripts/World/Elevator/ElevatorDoorPair.cs`
- `Assets/_Project/Scripts/Player/PlayerPerspectiveMover2D.cs`
- `Assets/_Project/Scripts/Player/PlayerDepthScaleAndSort.cs`
- `Assets/_Project/Scripts/World/VaultMap/PerspectiveWalkArea2D.cs`
- `Assets/_Project/Scenes/SCN_VaultMap.unity`

Priorita aggiornata: leggere/modificare prima `PlayerPerspectiveMover2D`, `PlayerDepthScaleAndSort`, `PerspectiveWalkArea2D` e la scena. I file Elevator sono secondari finche `ELEV_Elevator` spento riproduce il bug bordo.
