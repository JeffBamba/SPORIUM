# DEV REPORT 0094 — Icone varianti inventario, Seed Storage e allineamenti scena VaultMap

**Data:** 2026-04-25  
**Sprint / contesto:** Demo Alpha / UI & UX inventory readability — consolidamento pipeline icone item tra HUD runtime e pannelli UI Toolkit.  
**Riferimento piano:** `.cursor/plans/demo_alpha_1_0_gap_map.plan.md`  
**Report precedente:** `DEV_REPORT_0093_FIX_HUD_VO_E_INVENTARIO_DEMO_2026-04-23.md`

---

## Sommario interventi

1. Introdotto supporto a icone per **categoria + variante** nel catalogo globale icone, riducendo dipendenza da override per `typeId` specifico.
2. Aggiornata la risoluzione icone item con priorita coerente (`typeId` -> `categoria+variante` -> `categoria` -> fallback `Resources`).
3. Portate le icone item nel pannello `Seed Storage` (UI Toolkit) con rendering reale in lista inventario deposito.
4. Estesa la riga inventario HUD (`HUDInventoryItem`) con icona item dedicata (serializzata o auto-creata a runtime) e riallineamento testo.
5. Applicati aggiornamenti serializzati in `SCN_VaultMap.unity` coerenti con il salvataggio scena corrente.

---

## 1. Catalogo icone: supporto categoria + variante

### Problema
- Le icone erano mappate principalmente per `typeId` o per categoria generica.
- Varianti semantiche della stessa famiglia (es. acqua potabile vs grezza, fertilizzante standard vs pure vs prohibited) non avevano una chiave dedicata comune.

### Soluzione
- In `GlobalIconCatalog` e stato aggiunto `CategoryVariantIconEntry` (`CategoryKey`, `VariantKey`, `Icon`) e la lista serializzata `_categoryVariantIcons`.
- Aggiunto il metodo `TryGetCategoryVariantIcon(string categoryKey, string variantKey, out Sprite icon)`.
- In `GlobalIconCatalog.asset` sono stati riallineati gli override: ridotti alcuni mapping puntuali per `typeId` e introdotti mapping espliciti per `categoria+variante`.

**File interessati:**  
`Assets/_Project/Scripts/UI/Icons/GlobalIconCatalog.cs`,  
`Assets/_Project/Resources/UI/GlobalIconCatalog.asset`

---

## 2. Resolver icone item unificato

### Problema
- La risoluzione icone non copriva in modo nativo la granularita per variante e rendeva meno riusabile la stessa famiglia item.

### Soluzione
- In `GlobalIconResolver.GetItemIcon(...)` e stata introdotta risoluzione progressiva:
  - `TryGetTypeIcon(typeId)`
  - `TryGetCategoryVariantIcon(category, variant)`
  - `TryGetCategoryIcon(category)`
  - fallback `Resources` su `Icons/Items/{typeId}` e `Icons/Items/{category}-{variant}`
  - default finale `Icons/Items/item-default`
- Aggiunto `ResolveItemVariantKey(string typeId)` per le varianti principali (`water`, `fertilizer`, `additive`, `reagent`, `stemcell`).

**File interessato:**  
`Assets/_Project/Scripts/UI/Icons/GlobalIconResolver.cs`

---

## 3. Seed Storage: icona reale nelle righe inventario

### Problema
- La lista inventario nel pannello `Seed Storage` mostrava box icona placeholder, senza sprite item coerente con l'inventario player.

### Soluzione
- In `SeedStoragePanelController` e stato introdotto `BuildInvRowIcon(typeId)` che risolve lo sprite via `GlobalIconResolver` e lo applica al glyph UI Toolkit.
- `BuildInvGroupRow(...)` ora usa `BuildInvRowIcon(...)` invece di un box vuoto.
- In USS sono stati definiti centraggio e dimensioni del glyph (`seedstorage-inv-iconglyph` 20x20), mantenendo leggibilita e consistenza visiva.

**File interessati:**  
`Assets/_Project/Scripts/UI/UIToolkit/SeedStorage/SeedStoragePanelController.cs`,  
`Assets/_Project/Resources/UI/UIToolkit/SeedStorage/SeedStoragePanel.uss`

---

## 4. HUD inventory row: icona item su prefab runtime

### Problema
- Le righe inventario HUD non avevano un percorso robusto per mostrare icona item, con rischio di dipendere dalla sola configurazione prefab/scena.

### Soluzione
- In `HUDInventoryItem` e stato aggiunto `_itemIconImage` opzionale.
- Se non assegnato, il componente crea automaticamente un figlio `ItemIcon` a runtime (`EnsureItemIconImage()`), garantendo resilienza su istanze legacy.
- `ApplyItemIconSprite(typeId)` applica sprite da `GlobalIconResolver`, abilita/disabilita icona in base alla disponibilita e adatta offset del label nome per evitare overlap.

**File interessato:**  
`Assets/_Project/Scripts/UI/VaultMap/HUDInventoryItem.cs`

---

## 5. Allineamenti serializzazione scena VaultMap

### Problema
- La scena `SCN_VaultMap` conteneva differenze serializzate non ancora registrate nel report corrente.

### Soluzione
- Incluse nel changeset le modifiche serializzate della scena, in linea con lo stato salvato corrente del livello.
- Le modifiche rappresentano riallineamenti di oggetti/strutture scena necessari a mantenere coerenza tra authoring e runtime della build locale.

**File interessato:**  
`Assets/_Project/Scenes/SCN_VaultMap.unity`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/UI/Icons/GlobalIconCatalog.cs` | Nuova struttura `CategoryVariantIconEntry` + lookup `TryGetCategoryVariantIcon` |
| `Assets/_Project/Resources/UI/GlobalIconCatalog.asset` | Riorganizzazione mapping icone con varianti `categoria+variante` |
| `Assets/_Project/Scripts/UI/Icons/GlobalIconResolver.cs` | Pipeline risoluzione icona estesa + `ResolveItemVariantKey` |
| `Assets/_Project/Scripts/UI/UIToolkit/SeedStorage/SeedStoragePanelController.cs` | Rendering icone item reali nelle righe inventario |
| `Assets/_Project/Resources/UI/UIToolkit/SeedStorage/SeedStoragePanel.uss` | Stili glyph icona inventario (`seedstorage-inv-iconglyph`) |
| `Assets/_Project/Scripts/UI/VaultMap/HUDInventoryItem.cs` | Supporto icona item con fallback runtime e offset label |
| `Assets/_Project/Scenes/SCN_VaultMap.unity` | Aggiornamenti serializzazione scena |
| `Assets/Docs/REPORT/DEV_REPORT_0093_FIX_HUD_VO_E_INVENTARIO_DEMO_2026-04-23.md.meta` | Meta file tracciato nel changeset corrente |

---

## Regole / vincoli rispettati

- Evoluzione incrementale dei sistemi esistenti (`GlobalIconCatalog` / `GlobalIconResolver`) senza fork paralleli demo/full.
- Nessun uso di scene duplicate per feature inventory UI: integrazione nel runtime unico.
- Styling UI mantenuto in USS dove pertinente (dimensioni/allineamento glyph), con runtime usato per soli dati dinamici (sprite item).

---

## Note operative (Unity)

- Verificare in play mode che le righe inventario `Seed Storage` mostrino l'icona corretta per `WAT-RAW`, `WAT-POT`, famiglie fertilizzanti/additivi/reagenti/stem cell.
- Verificare in HUD inventory che l'icona item venga mostrata sia su prefab aggiornati (campo serializzato) sia su istanze legacy (fallback auto-create).
- Validare fallback: in assenza sprite variante, usare categoria; in assenza categoria, default icon.
- Eseguire smoke test rapido in `SCN_VaultMap` per confermare assenza regressioni visive/interattive dovute ai riallineamenti serializzati scena.

---

*Fine DEV REPORT 0094.*
