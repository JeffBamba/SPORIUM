# DEV REPORT 0077 — Compact Bottom Bar: Collection stack, notifiche inventario, Lab collect, room tooltip
**Data:** 2026-03-28  
**Contesto:** sessione chat (HUD Vault / UI Toolkit)  
**Riferimento piano:** `.cursor/plans/compact_bottom_bar_6169e487.plan.md`  
**Report precedente:** DEV_REPORT_0076

---

## Sommario

Questo documento riassume **tutto il lavoro** discusso e implementato nel thread della chat relativo alla **Compact Bottom Bar**, ai **Collection box**, al collegamento con **FoundationNotificationService**, al **Lab Extractor**, ai **metadati** in scheda raccolta, e infine agli aggiustamenti **UX/animazione/dismiss** e alla **parità visiva della room-tooltip** tra UI Builder e gioco.

Obiettivi raggiunti in sintesi:

1. Ogni item raccolto che passa da `PostAddedToInventory` genera un **box** nella barra; click sinistro apre la **scheda dettaglio** (centrata, overlay, sorting order elevato).
2. **Layout:** zona dedicata **collectors** tra `zone-center` e `zone-right`, fino a 5 box in orizzontale.
3. **Lab:** raccolta spore con payload ricco da `Item` (niente confusione con cellula staminale da fruit extract); `CollectionPayloadFactory` per metadati.
4. **Animazione** ingresso box da destra; **visibilità** corretta (stylesheet del template); **tasto destro** per chiudere il singolo box con fade-out.
5. **Room tooltip:** aspetto allineato al design (verde, senza CRT ereditata), posizionamento centrato sul bottone e clamp a schermo.

---

## Parte A — Thread iniziale: perché non compariva il Collection box

### Problema

Dopo harvest (o azioni analoghe) compariva il toast Foundation ma **non** il box nella Compact Bottom Bar.

### Cause individuate

1. **`CollectionBoxStackController`** si abbona a `FoundationNotificationService.OnItemAdded`, ma alcuni percorsi chiamavano solo `PostToast` e non `PostAddedToInventory`, quindi l’evento non scattava.
2. **Timing UI:** `Start()` poteva interrogare `rootVisualElement` prima che l’albero fosse pronto → `collection-box-stack` null e nessuna subscription efficace.
3. **`PostAddedToInventory`:** è stato consolidato un overload che accetta `NotificationPayload`, risolve icona se mancante, e notifica `OnItemAdded`.

### File e interventi principali (Parte A)

| Area | File | Intervento |
|------|------|------------|
| Notifiche | `FoundationNotificationService.cs` | `PostAddedToInventory(NotificationPayload)` con risoluzione icona e invio toast + `OnItemAdded`. |
| HUD stack | `CollectionBoxStackController.cs` | `DefaultExecutionOrder(20)`, `Start` come coroutine con `yield return null` (deferred init), subscription idempotente, overlay dettaglio, `sortingOrder` temporaneo per la scheda. |
| Harvest / gameplay | Percorsi inventario | Allineamento a `PostAddedToInventory` dove serve creare il box (es. flussi raccolta collegati al piano). |
| Lab UI | `LabExtractorPanelController.cs` | Rimossa doppia notifica su ritiro; la raccolta è centralizzata in `Extractor.CollectOutput`. |
| Lab gameplay | `Extractor.cs` | Fruit extract: output `CELL-002` portato a 0 (solo spore attese); `CollectOutput` notifica per slot con `CollectionPayloadFactory.FromItem` dove possibile. |
| Minigame | `LabMinigameExtractor.cs` | Win: stesso pattern payload ricco da `Item`. |
| Factory | `CollectionPayloadFactory.cs` (nuovo) | Costruzione `NotificationPayload` + `Args` metadati da `Item` (spore / generico). |
| UXML/USS | `CompactBottomBar.uxml/.uss` | `zone-collectors`, overlay `collection-detail-overlay`, regole flex per non schiacciare gli slot. |
| Dettaglio | `CollectionDetail.uxml/.uss` | Righe metadati nominate, icon box con outliner, root centrato nel flex dell’overlay. |
| Tipi | `NotificationPayload`, `NotificationItemIconResolver`, `RoomNames` | Uso coerente per titolo, icona, stanza. |

### Dettaglio scheda e layering

- Overlay full-screen sotto la scheda, click fuori chiude.
- `UIDocument.sortingOrder` portato temporaneamente a valore alto per la scheda, poi ripristinato.

---

## Parte B — Animazione Collection box e visibilità piena

### Richiesta

Il box deve **entrare da destra verso sinistra** e fermarsi nel suo slot; più box in sequenza. Inoltre il box non era **interamente visibile** (solo una porzione).

### Interventi

1. **`CompactBottomBar.uxml`**  
   - Rimosso `style="width: 238px;"` inline su `zone-collectors`, che **sovrascriveva** i 264px del USS e tagliava gli slot.

2. **`CompactBottomBar.uss`**  
   - `overflow: visible` su `.cbb-zone-collectors` per non clippare durante la traslazione.

3. **`CollectionBoxStackController.CreateBox`**  
   - Dopo `Add` allo stack: `translate(56px, 0)`, `opacity 0`, transizioni USS via codice, `schedule.ExecuteLater(0)` per portare a `translate(0,0)` e `opacity 1`.

---

## Parte C — Box “fantasma” / non leggibili vs icone stanza

### Problema

In game i Collection box apparivano come **frammenti** (quasi punti), mentre in UI Builder il template sembrava corretto.

### Causa tecnica

`VisualTreeAsset.Instantiate()` produce un **TemplateContainer** che **porta con sé** il foglio `CollectionBox.uss`. Estraendo il figlio `collection-box` con `Q` e aggiungendolo **direttamente** allo stack, il nodo usciva dall’albero del template → **perdeva lo stylesheet** → classi `.cbox-root` non risolte → collasso visivo.

### Fix

- Aggiungere allo stack l’**intera istanza** del template (`container`), non il solo figlio.
- Query su `inner` (`collection-box`) per icona, qty, click; `userData` e animazione sul `container`.
- `flex-shrink: 0` / `flex-grow: 0` sul container per non comprimere il layout.

---

## Parte D — Dismiss con tasto destro

### Richiesta

- **Sinistro:** apre la scheda (comportamento esistente).  
- **Destro:** il singolo box **scompare subito** (non solo al rilascio / menu contestuale OS).

### Implementazione

- Sostituito `ContextClickEvent` con **`PointerDownEvent`**, filtro `evt.button == 1` (tasto destro), `StopPropagation`.
- Nuovo **`DismissBox(container)`:** rimozione dalla lista logica, `HideDetail()`, transizione breve opacity + translate, `schedule.ExecuteLater(170)` → `_stack.Remove(container)`.

---

## Parte E — Room tooltip: UI Builder ≠ gioco

### Problema

La `room-tooltip` progettata in UI Builder (verde, sfondo scuro, senza texture CRT) **non coincideva** con quanto mostrato a runtime (palette ambra, possibile CRT da `.cbb-tooltip` base, posizione solo `left = worldBounds.x`).

### Cause

1. **Conflitto stili:** `.cbb-room-tooltip` in USS usava colori **ambra**; il UXML aveva inline **verde** e `background-image: none`. La risoluzione inline vs classe può differire tra editor e play mode.
2. **Ereditarietà:** `.cbb-tooltip` definisce `background-image` CRT; senza override esplicito la room eredita la texture.
3. **Posizione:** allineare solo al bordo sinistro del bottone non centrava la tooltip e poteva far uscire il pannello a destra.

### Fix

| File | Modifica |
|------|----------|
| `CompactBottomBar.uss` | `.cbb-room-tooltip`: bordo e titolo `rgb(127,255,122)`, sfondo `rgba(17,34,23,0.95)`, **`background-image: none`**, testi secondari e separatore in tonalità verdi coerenti con i room button. |
| `CompactBottomBar.uxml` | Rimossi inline ridondanti su `room-tooltip` e label; resta `display: flex` per preview in Builder. |
| `CompactBottomBarController.cs` | `ShowRoomTooltip`: dopo `display = Flex`, `schedule.ExecuteLater(0)` calcola `idealLeft = btn.center.x - tooltipW/2`, **clamp** tra margini e larghezza pannello. |

---

## Elenco file toccati (checklist rapida)

- `Assets/_Project/Scripts/UI/UIToolkit/HUD/CollectionBoxStackController.cs` — animazione, template container, dismiss destro.  
- `Assets/_Project/Scripts/UI/UIToolkit/HUD/CompactBottomBarController.cs` — posizionamento room tooltip.  
- `Assets/_Project/UI/UIToolkit/HUD/CompactBottomBar.uxml` — zone-collectors senza width inline errata; room-tooltip pulita.  
- `Assets/_Project/UI/UIToolkit/HUD/CompactBottomBar.uss` — collectors overflow; room-tooltip palette e `background-image: none`.  

*(Oltre a questi, nella stessa linea di prodotto del thread restano rilevanti: `FoundationNotificationService`, `Extractor`, `LabExtractorPanelController`, `LabMinigameExtractor`, `CollectionPayloadFactory`, `CollectionDetail.*`, `CollectionBox.*` — vedi Parte A.)*

---

## Note per QA / design

- Verificare in **1920×1080** e risoluzioni basse: clamp della room-tooltip e 5 Collection box nella fascia 264px.  
- **Tasto destro** su Collection box: niente apertura scheda; solo dismiss (sinistro resta dettaglio).  
- **RoomAreaTag** in scena: testi tooltip runtime da `DisplayName`, `FloorName`, `TooltipText`; senza tag, fallback nome room in maiuscolo.

---

## Chiusura

Il sistema Compact Bottom Bar + Collection è ora **coerente** con il piano (box per evento raccolta, dettaglio modale, metadati da item dove disponibili), **leggibile** (stylesheet legato al template), **animato** in ingresso, **dismissabile** col destro, e la **room-tooltip** è **allineata** tra authoring (USS) e runtime, con posizionamento più stabile sullo schermo.
