# DEV REPORT 0081 — HUD Foundation: spacing DomeStatus, notifiche 400px, nomi piante toast/collection, detail collection, location bar, collasso HUD, tooltip mutation in stile ph

**Data:** 2026-03-30  
**Sprint / contesto:** iterazione **HUD UIToolkit** (DomeStatus, TopBar, Compact Bottom Bar, Notifications Foundation, Collection) — richieste UI/UX della sessione di chat e correzioni successive (collasso HUD, parità layout).  
**Riferimento piano:** non vincolato a un singolo piano; incrocio con regole `.cursor/rules/ui-hud-foundation-ui-builder-parity.mdc`, `architecture-runtime-services.mdc`.  
**Report precedente:** `DEV_REPORT_0080_TASK7_INDICE_MUTAZIONE_SPONTANEA_TOAST_LAB_2026-03-30.md`

---

## Sommario interventi

1. **DomeStatusHUD — spacing** — distanza tra `dome-pot-card` e tra tab e card r**addoppiata** (3px → 6px) via USS.
2. **Notifications Foundation — larghezza pannello** — `.nf-root` portato da **306px** a **400px**; header/list/row restano **100%** sul nuovo contenitore (evitata la modifica errata iniziale a `140px` che rimpiccioliva il pannello).
3. **Toast e Collection — nomi specie leggibili** — spore e schede non devono mostrare codici tipo `PLT-STD-001`: introdotto **`PlantSpeciesDisplayNames`**, rafforzato **`ItemFabric`** (`SourcePlantDisplayName`, risoluzione via DB/Resources, `ResolveSourcePlantDisplayNameForUi`), **`CollectionPayloadFactory`**, **`Extractor`** (testo "Spore Grezza" coerente).
4. **Collection detail HUD** — **`CollectionDetail.uss`**: larghezza ~**400px**, righe key/value con wrapping, overflow contenuto per descrizioni poteri attivi/passivi.
5. **Compact Bottom Bar — `[Location]`** — `zone-location` tra `zone-left` e `zone-center`; nome stanza da **`RoomTracker`** con **typewriter**; colore testo **#5DB6E3**; layout con fill flessibile + label **assoluta** per non far “ballare” `zone-center` durante la scrittura; `OnDestroy` stop coroutine.
6. **DomeStatusHUD — collassabile (correzione design)** — rimossa barra separata in cima; toggle **`btn-dome-hud-toggle`** come ultimo figlio di **`dome-hud-tabs`** (come chevron notifiche); collassato = **solo barra tab** POT/CRYO + chevron; sezioni nascoste via **classe + inline `display`** (perché `SwitchTab` imposta inline e batte il solo USS); ordine `SetupUI`: `SwitchTab(false)` poi `ApplyHudBodyExpandedState`.
7. **TopBar — tooltip Mutation Index** — **`mutation-tooltip`** riallineato allo **stile CRT / sezioni boxed** del **`ph-tooltip`** (stesso asset background, struttura sezioni, scan lines), **mantenendo palette mutation** (violet/bordi, copy stable/warn/high, meccaniche verdi, tip gold).

---

## 1. DomeStatusHUD — spacing tra card e tab

### Problema
Spaziatura verticale tra le card POT e tra tab e prima card troppo stretta (3px).

### Soluzione
- **`DomeStatusHUD.uss`**: `margin-bottom` su `.dome-pot-card` **6px**; `padding-top` su `.dome-hud-section` **6px** (coerente con raddoppio richiesto).

**File:** `Assets/_Project/UI/UIToolkit/DomeStatusHUD/DomeStatusHUD.uss`

---

## 2. Notifications Panel — larghezza 400px

### Problema
Allargare la fascia notifiche; un primo tentativo (`width: 140px` su header/list) ha **rimpicciolito** il pannello perché **100%** era relativo al contenitore **306px**.

### Soluzione
- Ripristino **100%** / **stretch** su `.nf-header`, `.nf-list`, `.nf-row`.
- **`NotificationsPanel.uss`**: `.nf-root` **width: 400px** (contenitore effettivo più largo).

**File:** `Assets/_Project/UI/UIToolkit/NotificationsFoundation/NotificationsPanel.uss`

---

## 3. Toast collect spore e Collection — nomi piante (non codici)

### Problema
Messaggi tipo "Hai estratto Spore … **PLT-STD-001**" e titoli collection con codice asset / codice pianta invece di nomi umani (es. Ferric Fern, Arctic Hask).

### Soluzione
- **`PlantSpeciesDisplayNames`** (static helper): mapping codice → nome UI + fallback da `PlantData` senza usare `plantData.name` come display (in Unity spesso = nome file asset = codice).
- **`ItemFabric`**: risoluzione `PlantDatabase` / `Resources`, `ApplyPlantMetadataFromCode` / fruit metadata, `EnsureSourcePlantDisplayIsHumanReadable`, `ResolveSourcePlantDisplayNameForUi` per UI centralizzata.
- **`CollectionPayloadFactory`**: titoli spore tipo "**Spore Grezza di {nome}**" / matura con resolver item.
- **`Extractor`**: stringa fallback allineata ("Spore Grezza").

**File:** `PlantSpeciesDisplayNames.cs` (nuovo), `ItemFabric.cs`, `CollectionPayloadFactory.cs`, `Extractor.cs`

**Nota:** item già in save con `SourcePlantDisplayName` = codice possono comunque essere mostrati correttamente dal resolver UI al momento toast/collection.

---

## 4. Collection detail — overflow testo

### Problema
Pannello detail troppo stretto; testi poteri uscivano dal box.

### Soluzione
- **`CollectionDetail.uss`**: root più largo (~400px), colonne title/key/value con `min-width: 0`, wrapping, hint a capo; allineamento righe `flex-start`.

**File:** `Assets/_Project/UI/UIToolkit/HUD/CollectionDetail.uss`

---

## 5. Compact Bottom Bar — `[Location: …]` + typewriter

### Problema
Mostrare la stanza corrente tra icone sinistra e centro; aggiornamento al cambio area con effetto macchina da scrivere; colore **rgb(93,182,227)**; evitare che **`zone-center`** si sposti mentre il testo cresce (flex).

### Soluzione
- **UXML**: `zone-location` con `location-layout-fill` + `location-label`; `zone-post-center` per bilanciare il flex; margini da USS (no offset inline fragile su zone-left/cry-badge).
- **USS**: label **position absolute** dentro `zone-location` **relative**; fill con `flex-grow`; `zone-center` **flex-grow/shrink 0**.
- **Controller**: coroutine typewriter, delay configurabile; `OnRoomTrackerChanged`; init in `Start` per evitare doppio typewriter alla sottoscrizione evento; `OnDestroy` ferma coroutine.

**File:** `CompactBottomBar.uxml`, `CompactBottomBar.uss`, `CompactBottomBarController.cs`

---

## 6. DomeStatusHUD — collasso come riferimento utente (tab + chevron)

### Problema
Prima implementazione con **`dome-hud-collapse-bar`** separata sopra le tab: aspetto non conforme (Allegato 3 vs 1–2 utente). Richiesto: **box/chevron a destra nella stessa riga delle tab**; collassato = **solo** riga POT / CRYO / toggle.

### Soluzione
- **UXML**: rimossi `dome-hud-collapse-bar` e wrapper `dome-hud-body`; `btn-dome-hud-toggle` **dentro** `dome-hud-tabs` come ultimo elemento.
- **USS**: stile bottone compatto; `.dome-hud--collapsed` nasconde sezioni/tooltip/builder-ref (supporto); container root mutation-style spacing già definito in precedenti commit.
- **Controller**: `ApplyHudBodyExpandedState` imposta **`display: None`** su `_sectionPots` / `_sectionCryo` quando collassato e **`SwitchTab(_showingCryo)`** quando espanso; **`SetupUI`**: `SwitchTab(false)` **prima** di applicare stato iniziale espanso/collassato per non essere sovrascritto.

**File:** `DomeStatusHUD.uxml`, `DomeStatusHUD.uss`, `DomeStatusHUDController.cs`

---

## 7. TopBar — tooltip Mutation in stile ph drift

### Problema
Tooltip **mutation** visivamente distaccato dal **ph-tooltip** (manca CRT, sezioni boxed coerenti, righe scan).

### Soluzione
- **UXML**: root `mutation-tooltip-root` + **`ph-tooltip-crt`**, stesso **`Crt_background_tooltip.png`** del ph-tooltip; header row con titolo violet + **`mutation-tooltip-current-level`** a destra (status); paragrafi IM in box violet subtle; meccaniche / breakdown / footer come box analoghi a sezioni ph; tre elementi **`ph-tooltip-crt-refresh`** con tint viola leggera.
- **USS**: `.mutation-tooltip-root` allineato a container ph (`background-color rgba(0,0,0,0.9)`, `padding 8px`); **colori mutation** su classi copy (stable/warn/high) invariati.

**File:** `TopBar.uxml`, `TopBar.uss`  
**Controller:** nessun cambiamento necessario ai `name` delle label già query-ate (`mutation-tooltip-current-level`, breakdown labels).

---

## File modificati (tabella)

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/UI/UIToolkit/DomeStatusHUD/DomeStatusHUD.uss` | Spacing card/section; stili toggle; collasso selettori |
| `Assets/_Project/UI/UIToolkit/DomeStatusHUD/DomeStatusHUD.uxml` | Toggle nella barra tab; rimozione collapse bar / body wrapper |
| `Assets/_Project/Scripts/UI/UIToolkit/DomeStatusHUD/DomeStatusHUDController.cs` | Collapse: classe root + inline sezioni; ordine SetupUI |
| `Assets/_Project/UI/UIToolkit/NotificationsFoundation/NotificationsPanel.uss` | Larghezza `.nf-root` 400px |
| `Assets/_Project/Scripts/Dome/PotSystem/Growth/PlantSpeciesDisplayNames.cs` | **Nuovo** — nomi UI da codice / PlantData |
| `Assets/_Project/Scripts/Core/ItemsSystem/ItemFabric.cs` | Metadata spore, risoluzione nome umano, API UI |
| `Assets/_Project/Scripts/Interactables/Extractor.cs` | Copy toast allineata |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/CollectionPayloadFactory.cs` | Titoli/metadata collection spore |
| `Assets/_Project/UI/UIToolkit/HUD/CollectionDetail.uss` | Layout detail anti-overflow |
| `Assets/_Project/UI/UIToolkit/HUD/CompactBottomBar.uxml` | zone-location, fill, post-center |
| `Assets/_Project/UI/UIToolkit/HUD/CompactBottomBar.uss` | Posizionamento location, zone-center fisso |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/CompactBottomBarController.cs` | RoomTracker, typewriter, cleanup |
| `Assets/_Project/UI/UIToolkit/HUD/TopBar.uxml` | Tooltip mutation struttura ph-style |
| `Assets/_Project/UI/UIToolkit/HUD/TopBar.uss` | Container mutation allineato a ph-tooltip |

*Asset grafici PNG citati in UXML (es. CRT tooltip) erano già in progetto; eventuali modifiche binarie PNG non elencate qui se esterne al perimetro codice.*

---

## Regole / vincoli rispettati

- **Parità UI Builder / runtime** (ove applicabile): struttura DomeStatus e CompactBar editabile in UXML/USS; dati dinamici dal controller; niente “binario parallelo” campione/runtime per il collasso (toggle è elemento runtime reale nella barra tab).
- **ServiceContainer / architettura**: location via servizi già in uso (`RoomTracker`); nessun `FindObjectOfType` aggiunto per gameplay in questi file.
- **USS vs inline**: preferenza a classi per authoring; dove `SwitchTab` imposta `style.display` su sezioni, il collasso gestisce anche C# per coerenza con la cascata USS/inline.

---

## Note operative (Unity)

- Verificare in Play: **collasso DomeStatus** — solo tab visibile; espansione ripristina tab POT/CRYO corretta; tooltip POT nascosto al collasso.
- **Compact bar**: cambio stanza aggiorna location con typewriter senza shift delle icone centrali.
- **Toast/collection spore**: estrazione e catalogo mostrano **nomi specie** attesi.
- **TopBar**: hover su modulo mutazione → tooltip con aspetto coerente al ph-tooltip ma **accenti violet/gold/green** mutation.

---

*Fine DEV REPORT 0081.*
