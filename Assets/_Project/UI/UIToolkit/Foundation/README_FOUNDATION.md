# SPORIUM UI FOUNDATION (UI Toolkit)

## Scopo
La Foundation è un set di **design tokens** e **componenti USS** riutilizzabili per mantenere coerenti colori, spacing, typography e pattern UI in tutti i pannelli UI Toolkit.

Questa implementazione è **non-distruttiva**: non modifica nessun pannello esistente finché non importi manualmente gli USS nei tuoi `.uxml`.

## Struttura
- `Foundation/SP-Foundation.uss`: tokens globali `--sp-*` + utility base
- `Foundation/SP-Panel-Base.uss`: base pannelli (`.sp-panel`) + varianti/sections
- `Foundation/Components/`: componenti riutilizzabili
  - `SP-Button.uss` (`.sp-button`)
  - `SP-Badge.uss` (`.sp-badge`)
  - `SP-StatBar.uss` (`.sp-stat-bar`)
  - `SP-StatRow.uss` (`.sp-stat-row`)
  - `SP-Header.uss` (`.sp-header`)

## Regole d’uso (consigliate)
### Import order (importante)
Nei nuovi UXML importa gli USS in questo ordine:
1. `Foundation/SP-Foundation.uss`
2. `Foundation/SP-Panel-Base.uss`
3. `Foundation/Components/...` (solo ciò che usi)
4. (opzionale) USS specifico del pannello (solo layout/override locali)

### Naming & collisioni
- Classi: `sp-*`
- Variabili: `--sp-*`
Questo evita conflitti con gli USS esistenti (es. `TopBar.uss`, `BottomNavigation.uss`, ecc.).

### Come iniziare (nuovo pannello)
1. Crea un nuovo `.uxml` (UI Builder o Project window).
2. Aggiungi gli import USS nell’ordine sopra.
3. Sul root element aggiungi classi:
   - `sp-panel` (+ eventuale variante: `sp-panel--card`, `sp-panel--dialog`, `sp-panel--bar`, ecc.)
4. Usa i componenti:
   - Bottoni: `sp-button sp-button--primary` (o `--secondary`, `--danger`)
   - Badge: `sp-badge sp-badge--green` (o `--blue`, `--violet`, `--yellow`, `--red`)
   - Barre: `sp-stat-bar` (continua con child `sp-stat-bar__fill`, o segmentata con `sp-stat-bar__segment`)

## Note su compatibilità UI Toolkit
- Evitati pseudo-selettori e feature “web-only” (es. `:last-child`, animazioni CSS complesse): molte cose sono limitate in USS.
- Effetti tipo scanlines/glow avanzati spesso rendono meglio con supporto C# o texture dedicate.

## Manual check post-implementazione (principiante)
1. Apri Unity Editor.
2. Vai in `Assets/_Project/UI/UIToolkit/` e verifica che esistano:
   - `Foundation/`
   - `Foundation/Components/`
3. Apri uno qualsiasi dei nuovi `.uss` per confermare che Unity li importa (Unity creerà i `.meta` se mancanti).


