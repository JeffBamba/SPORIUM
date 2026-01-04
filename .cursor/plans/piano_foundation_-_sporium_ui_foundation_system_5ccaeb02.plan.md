---
name: PIANO FOUNDATION - SPORIUM UI Foundation System
overview: Creazione di un sistema Foundation universale per tutte le UI di SPORIUM, standardizzando colori, spacing, typography e componenti riutilizzabili. La Foundation sarà il sistema base utilizzato da tutti i pannelli UIToolkit esistenti e futuri. Include anche istruzioni dettagliate per operazioni manuali post-implementazione.
todos:
  - id: foundation-folder
    content: Creare struttura cartelle Foundation/ e Foundation/Components/
    status: pending
  - id: foundation-tokens
    content: Creare SP-Foundation.uss con design tokens (colori, spacing, typography, borders)
    status: pending
    dependencies:
      - foundation-folder
  - id: panel-base
    content: Creare SP-Panel-Base.uss con sistema pannelli modulare (varianti dimensione/formato, decorazioni, sezioni)
    status: pending
    dependencies:
      - foundation-tokens
  - id: component-button
    content: Creare SP-Button.uss con varianti e stati
    status: pending
    dependencies:
      - foundation-tokens
  - id: component-badge
    content: Creare SP-Badge.uss con varianti colore
    status: pending
    dependencies:
      - foundation-tokens
  - id: component-statbar
    content: Creare SP-StatBar.uss con supporto orizzontale/verticale e segmentata/continua
    status: pending
    dependencies:
      - foundation-tokens
  - id: component-statrow
    content: Creare SP-StatRow.uss per righe stat complete
    status: pending
    dependencies:
      - foundation-tokens
      - component-statbar
  - id: component-header
    content: Creare SP-Header.uss per header modulari
    status: pending
    dependencies:
      - foundation-tokens
      - component-badge
  - id: foundation-readme
    content: Creare README_FOUNDATION.md con documentazione utilizzo
    status: pending
    dependencies:
      - panel-base
  - id: migrate-topbar
    content: Migrare TopBar.uxml/uss a Foundation (aggiungere import, sostituire classi, rimuovere duplicati)
    status: pending
    dependencies:
      - panel-base
      - component-button
      - component-statbar
  - id: migrate-bottomnav
    content: Migrare BottomNavigation.uxml/uss a Foundation
    status: pending
    dependencies:
      - panel-base
      - component-button
  - id: migrate-playerstatus
    content: Migrare PlayerStatusPanel.uxml/uss a Foundation
    status: pending
    dependencies:
      - panel-base
      - component-statbar
      - component-statrow
  - id: migrate-plantcard
    content: Migrare PlantCardV2.uxml/uss a Foundation (gestire CSS variables esistenti)
    status: pending
    dependencies:
      - panel-base
      - component-badge
      - component-statbar
      - component-header
---

# PIANO FOUNDATION - SPORIUM UI Foundation System

## Obiettivo

Creare un sistema Foundation universale che standardizzi tutti gli elementi UI di SPORIUM, eliminando duplicazioni, hardcoding di colori/stili, e creando componenti riutilizzabili modulari. La Foundation sarà il sistema master utilizzato da tutti i pannelli UIToolkit (TopBar, BottomNavigation, PlayerStatusPanel, PlantCardV2, e futuri).

## Analisi Stato Attuale

### UI Toolkit Esistenti

- **TopBar** (`Assets/_Project/UI/UIToolkit/HUD/TopBar.uxml/uss`) - Barra superiore con metriche
- **BottomNavigation** (`Assets/_Project/UI/UIToolkit/HUD/BottomNavigation.uxml/uss`) - Barra inferiore navigazione
- **PlayerStatusPanel** (`Assets/_Project/UI/UIToolkit/PlayerStatusPanel.uxml/uss`) - Pannello laterale player stats
- **PlantCardV2** (`Assets/_Project/UI/UIToolkit/PlantCard/PlantCardV2.uxml/uss`) - Pannello dettaglio pianta
- **GameViewportBackground** (`Assets/_Project/UI/UIToolkit/GameViewportBackground.uxml/uss`) - Background gradiente

### Problemi Identificati

1. **Duplicazione stili**: `.pixel-corner` definito in almeno 3 file USS (TopBar, PlayerStatusPanel, PlantCardV2) con variazioni
2. **Colori hardcoded**: Valori RGB sparsi in tutti i file USS invece di CSS variables
3. **Incoerenza palette**: Colori leggermente diversi tra file (es. `rgba(30, 40, 42, 0.9)` vs `rgb(27, 27, 27)`)
4. **Spacing non standardizzato**: Padding/margin con valori diversi tra pannelli
5. **Componenti non consolidati**: Esistono già componenti riutilizzabili ma non standardizzati

### Componenti Riutilizzabili Esistenti

- `SegmentedBarUI.cs` - Barre segmentate
- `PixelCornersUI.cs` - Decorazioni angoli
- `MutationOrbitUI.cs` - Animazione orbit
- `VitalParameterBox.cs` - Parametri vitali
- `SegmentedStatBarController.cs` - Controller barre segmentate

### Config Esistenti

- `PlantCardV2Config.cs` - Palette colori e thresholds per PlantCard
- `ToastNotificationConfig.cs` - Palette colori per toast (usa stessi colori base)

## Struttura Foundation

### 1. Foundation Layer (Design Tokens)

**File**: `Assets/_Project/UI/UIToolkit/Foundation/SP-Foundation.uss`Design tokens universali per tutto il gioco:

- **Palette Colori**: Verde LED (#7FFF7A), Blu Info (#5DB6E3), Rosso Warning (#D35F5F), Giallo Standard (#E6C96F), Violetto Growth (#B580D1), Grigio Text (#C0C8C5)
- **Backgrounds**: Panel (rgba(30, 40, 42, 0.9)), Dark (#0a1216), Darker (#0f1416), Metal Light/Dark
- **Spacing**: xs(4px), sm(8px), md(12px), lg(16px), xl(24px), xxl(32px)
- **Typography**: Font sizes (8px-24px), letter-spacing
- **Borders**: Width (1px, 2px, 4px), colors

### 2. Panel Base System

**File**: `Assets/_Project/UI/UIToolkit/Foundation/SP-Panel-Base.uss`Sistema pannelli modulare con varianti:

- `.sp-panel` - Base comune
- Varianti dimensione: `--compact`, `--large`
- Varianti formato: `--bar` (TopBar/BottomNav), `--card`, `--tooltip`, `--dialog`
- Decorazioni: Pixel corners, CRT effect, scanlines
- Sezioni interne: Header, Content, Footer, Sidebar

### 3. Componenti Universali

**Cartella**: `Assets/_Project/UI/UIToolkit/Foundation/Components/`

#### SP-Button.uss

Sistema bottoni con varianti (primary, secondary, danger) e stati (hover, active, disabled)

#### SP-Badge.uss

Badge/Tag modulari con varianti colore (green, blue, violet, yellow, red)

#### SP-StatBar.uss

Barre progresso universali:

- Orientamento: orizzontale (default) e verticale (`--vertical`)
- Tipo: segmentata (10 segmenti) e continua (fill-based)
- Supporta entrambi i layout (da REF: orizzontali, da PlantCardV2: verticali)

#### SP-StatRow.uss

Riga stat completa (Label + Icon + Value + Bar + Range Info)

#### SP-Header.uss

Header modulare con badge Specimen ID e titoli

### 4. Migrazione Graduale

**Strategia non-distruttiva**: I pannelli esistenti continueranno a funzionare. La migrazione sarà graduale:

1. **Fase 1**: Creare Foundation (non tocca codice esistente)
2. **Fase 2**: Migrare TopBar e BottomNavigation (più semplici)
3. **Fase 3**: Migrare PlayerStatusPanel
4. **Fase 4**: Migrare PlantCardV2 (più complesso)
5. **Fase 5**: Rifattorizzare componenti C# per usare Foundation

## File da Creare

### Foundation Layer

- `Assets/_Project/UI/UIToolkit/Foundation/SP-Foundation.uss` - Design tokens
- `Assets/_Project/UI/UIToolkit/Foundation/SP-Panel-Base.uss` - Sistema pannelli
- `Assets/_Project/UI/UIToolkit/Foundation/Components/SP-Button.uss`
- `Assets/_Project/UI/UIToolkit/Foundation/Components/SP-Badge.uss`
- `Assets/_Project/UI/UIToolkit/Foundation/Components/SP-StatBar.uss`
- `Assets/_Project/UI/UIToolkit/Foundation/Components/SP-StatRow.uss`
- `Assets/_Project/UI/UIToolkit/Foundation/Components/SP-Header.uss`

### Documentazione

- `Assets/_Project/UI/UIToolkit/Foundation/README_FOUNDATION.md` - Guida utilizzo Foundation

## Modifiche a File Esistenti

### TopBar.uxml

- Aggiungere import Foundation: `<Style src="Foundation/SP-Foundation.uss" />`, `<Style src="Foundation/SP-Panel-Base.uss" />`
- Sostituire classe `.top-bar` con `.sp-panel sp-panel--bar sp-panel--bar-top`
- Rimuovere stili duplicati (pixel-corner, colors) e usare Foundation

### BottomNavigation.uxml

- Stessa strategia di TopBar

### PlayerStatusPanel.uxml

- Aggiungere import Foundation
- Migrare a classi Foundation
- Rimuovere stili duplicati

### PlantCardV2.uxml

- Aggiungere import Foundation
- Mantenere CSS variables locali se necessario, ma allineare valori a Foundation
- Migrare gradualmente componenti

## Note Tecniche

1. **CSS Variables in UI Toolkit**: Supportate, ma scope limitato. Le variabili in `:root` sono globali
2. **Backward Compatibility**: I pannelli esistenti continueranno a funzionare durante la migrazione
3. **PanelSettings**: Nessuna modifica necessaria, i pannelli condividono già PanelSettings
4. **Componenti C#**: I controller esistenti continueranno a funzionare, eventuali refactoring saranno in fasi successive

## Fasi di Implementazione

### Fase 1: Creazione Foundation (Base)

- Creare cartella `Foundation/` e `Foundation/Components/`
- Creare `SP-Foundation.uss` con tutti i design tokens
- Creare `SP-Panel-Base.uss` con sistema pannelli
- Documentazione README

### Fase 2: Componenti Base

- Creare `SP-Button.uss`, `SP-Badge.uss`, `SP-StatBar.uss`, `SP-StatRow.uss`, `SP-Header.uss`
- Testare componenti isolati

### Fase 3: Migrazione TopBar/BottomNavigation

- Migrare TopBar a Foundation
- Migrare BottomNavigation a Foundation
- Testare funzionalità

### Fase 4: Migrazione PlayerStatusPanel

- Migrare PlayerStatusPanel
- Testare funzionalità

### Fase 5: Migrazione PlantCardV2

- Migrare PlantCardV2 (più complesso per CSS variables esistenti)
- Testare funzionalità

## Rischi e Mitigazione

1. **Breaking Changes**: Migrazione graduale preserva funzionalità esistente
2. **Conflitti CSS**: Usare naming convention `sp-` per evitare collisioni
3. **Performance**: CSS variables non impattano performance significativamente
4. **Testing**: Ogni fase include testing del pannello migrato

---

# ISTRUZIONI OPERAZIONI MANUALI POST-IMPLEMENTAZIONE

## Fase 1: Verifica Creazione Foundation

1. **Verifica Struttura Cartelle**

- Apri Unity Editor
- Naviga in `Assets/_Project/UI/UIToolkit/`
- Verifica che esista cartella `Foundation/`
- Verifica che esista `Foundation/Components/`

2. **Verifica File USS Creati**

Controlla che esistano tutti questi file:

- `Foundation/SP-Foundation.uss`
- `Foundation/SP-Panel-Base.uss`
- `Foundation/Components/SP-Button.uss`
- `Foundation/Components/SP-Badge.uss`
- `Foundation/Components/SP-StatBar.uss`
- `Foundation/Components/SP-StatRow.uss`
- `Foundation/Components/SP-Header.uss`
- `Foundation/README_FOUNDATION.md`

## Fase 2: Test in Unity Editor

1. **Test TopBar (se migrata)**

- Apri scena principale
- Seleziona GameObject `HUD_TopBar`
- Verifica che UI sia visibile e funzionante
- Controlla Console (Window → General → Console) per errori
- Verifica che metriche siano aggiornate correttamente

2. **Test BottomNavigation (se migrata)**

- Seleziona GameObject `HUD_BottomNavigation`
- Verifica visibilità e funzionalità
- Testa navigazione stanze
- Controlla Console

3. **Test PlayerStatusPanel (se migrato)**

- Seleziona GameObject `PlayerStatusPanel`
- Verifica visibilità
- Controlla barre stat
- Verifica Console

4. **Test PlantCardV2 (se migrata)**

- Apri pannello pianta (click su vaso)
- Verifica layout e componenti
- Controlla parametri vitali
- Verifica Console

## Fase 3: Verifica Migrazione

1. **Apri UI Builder per TopBar**

- Seleziona `TopBar.uxml` in Project window
- Doppio click per aprire UI Builder
- Nel pannello Styles (lato destro), verifica che ci siano:
    - `SP-Foundation.uss`
    - `SP-Panel-Base.uss`
    - `Components/SP-Button.uss` (se usato)
- Verifica che classi CSS usino prefisso `sp-` dove necessario

2. **Controlla PlayerStatusPanel.uss**

- Apri file `PlayerStatusPanel.uss` come testo
- Verifica che NON ci siano più definizioni duplicate di `.pixel-corner`
- Verifica che colori hardcoded siano stati sostituiti con CSS variables (es. `var(--sp-color-green-led)`)

3. **Verifica Classi nel Codice**

- Apri `TopBar.uxml` come testo
- Cerca `<ui:VisualElement class="top-bar"` (o simile)
- Verifica che sia stato sostituito con `class="sp-panel sp-panel--bar sp-panel--bar-top"` (se migrato)

## Fase 4: Test Funzionalità

1. **Test Interattività**

- Avvia Play Mode (premi Play)
- Verifica che TopBar mostri metriche corrette
- Verifica che BottomNavigation permetta navigazione stanze
- Verifica che PlayerStatusPanel si aggiorni correttamente
- Verifica che PlantCardV2 si apra correttamente quando clicchi su un vaso

2. **Test Visivo/Estetico**

- Confronta aspetto prima/dopo migrazione
- Verifica che colori siano coerenti
- Verifica che spacing sia uniforme
- Verifica che pixel corners siano presenti (se previsti)
- Verifica che glow/effetti siano corretti

## Fase 5: Uso Foundation per Nuovi Pannelli

Quando crei un nuovo pannello UIToolkit:

1. **Crea Nuovo UXML**

- Right click in `Assets/_Project/UI/UIToolkit/`
- Create → UI Toolkit → UI Document
- Rinomina (es. `NewPanel.uxml`)

2. **Aggiungi Foundation al Nuovo UXML**

- Apri `NewPanel.uxml` in UI Builder (doppio click)
- Nel pannello Styles (lato destro), clicca "+" o "Add USS"
- Aggiungi in ordine:
    - `Foundation/SP-Foundation.uss`
    - `Foundation/SP-Panel-Base.uss`
    - `Foundation/Components/SP-Button.uss` (se servono bottoni)
    - Altri componenti necessari

3. **Usa Classi Foundation nel Nuovo Pannello**

- Nel Hierarchy (pannello sinistro), seleziona root VisualElement
- Nel pannello Style (lato destro), in "Classes" aggiungi: `sp-panel`
- Aggiungi variante: `sp-panel--large` (o `--compact`, `--card`, etc.)
- Per bottoni: usa `sp-button sp-button--primary`
- Per badge: usa `sp-badge sp-badge--green`
- Per barre: usa `sp-stat-bar`

## Fase 6: Debugging (se necessario)

1. **Se UI non appare**

- Verifica PanelSettings assegnato nel componente UIDocument (Inspector)
- Verifica che file USS siano nella cartella corretta
- Controlla Console per errori compilazione USS

2. **Se colori non sono corretti**

- Verifica che `SP-Foundation.uss` sia importato nel UXML
- Verifica sintassi CSS variables: `var(--sp-color-green-led)`
- Controlla che nomi variabili siano corretti (rispetto maiuscole/minuscole)

3. **Se ci sono conflitti di stili**

- Verifica che classi Foundation abbiano prefisso `sp-`
- Controlla che non ci siano classi duplicate con nomi diversi
- Usa UI Builder per vedere quale stile viene applicato (pannello Style Inspector)

4. **Se componenti non funzionano**

- Verifica che controller C# siano collegati al GameObject
- Controlla che nomi elementi nel UXML corrispondano a quelli cercati nel codice
- Verifica Console per errori C#

## Fase 7: Documentazione Personale

1. **Leggi README_FOUNDATION.md**

- Apri `Foundation/README_FOUNDATION.md`
- Studia esempi d'uso
- Prendi nota convenzioni

2. **Crea Riferimento Veloce**

- Crea file note personali con:
    - Palette colori Foundation
    - Classi principali e varianti
    - Esempi utilizzo frequenti

## Checklist Finale

- [ ] Foundation creata correttamente
- [ ] Tutti i file USS presenti
- [ ] TopBar migrata e funzionante
- [ ] BottomNavigation migrata e funzionante
- [ ] PlayerStatusPanel migrato e funzionante
- [ ] PlantCardV2 migrata e funzionante
- [ ] Nessun errore in Console
- [ ] UI visivamente coerente
- [ ] Interattività preservata
- [ ] README letto e compreso

## Prossimi Passi (Opzionale)

1. **Crea Pannello di Test** usando solo Foundation per familiarizzare