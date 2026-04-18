# DEV REPORT 0086 — Vo overlay (UI Toolkit): testo, glow, typing, glitch idle e integrazione

**Data:** 2026-04-18  
**Sprint / contesto:** chiusura workstream **VO overlay** per demo Alpha: presentazione testuale in basso, due registri colore, effetto macchina da scrivere, micro-movimento “organico” in idle; integrazione runtime e debug.  
**Riferimento piano:** `demo_alpha_1_0_gap_map` (VoOverlay / demo VO)  
**Report precedente:** `DEV_REPORT_0085_HUD_TOOLTIP_TOAST_LAYERING_MAINMENU_LOADING_2026-04-17.md`

---

## Sommario interventi

1. **UI**: overlay VO **senza box** (solo testo), **glow** a strati sui registri A/B in USS.
2. **Runtime** (`VoOverlayController`): **typing** con `chars/s` configurabile, audio opzionale; **`sortingOrder` 650** (sotto menu pausa, sopra HUD base); **`ShowLine` / `Hide`**; registri **cyan / verde**.
3. **Idle post-riga**: dopo il typing il testo passa a layout **parole** (`vo-organic-host`); **drift sul blocco** (`vo-text-wrap`) + micro-glitch **solo su ~18% delle parole** (`Wiggles`), con intervalli **lunghi** e offset **piccoli** (movimento non protagonista).
4. **Integrazione**: `GamePlayInstaller` crea GameObject `VoOverlay` + `VoOverlayController`; `DemoStoryDirector` risolve il servizio; **Pot Debug Console** sezione test VO (testo, registro A/B, Mostra/Nascondi).

---

## 1. Aspetto visivo (UXML / USS)

### Problema
Serviva un VO leggibile sul gameplay **senza cornice** da terminale, in linea col mockup “solo testo”.

### Soluzione
- **`VoOverlay.uxml`**: root → `vo-text-wrap` → `vo-body` (typing) + **`vo-organic-host`** (nascosto in authoring; popolato a runtime dopo il typing con una `Label` per parola).
- **`VoOverlay.uss`**: glow multi-strato (`text-shadow`) per `.vo-body--register-a` e `.vo-body--register-b`; host **flex row + wrap**, `.vo-fragment` senza margini che rompono il flusso.

---

## 2. Controller — typing, idle, glitch

### Problema
Il testo non doveva restare statico dopo l’ingresso; serviva varietà **organica** senza competere con il gameplay.

### Soluzione
- **Durante il typing**: una sola `Label` (`vo-body`).
- **Fine riga**: `SwitchToOrganicLayout()` spezza il testo per **parole**, applica le stesse classi di registro; **`FragmentGlitch.Wiggles`** assegnato con probabilità **`FragmentWiggleProbability` (0.18)**.
- **Update**: drift **blocco** (costanti `BlockGlitch*`) su `_textWrap`; sulle sole parole con `Wiggles`, drift **frammento** (`FragmentGlitch*`). Offset **snap** a 0,5 px; **`Time.unscaledDeltaTime`** per indipendenza dal time scale.
- Valori attuali (tenue): blocco ~**0,38 px**, intervalli ~**1,75–3,25 s**; frammenti ~**0,48 px**, intervalli ~**1,45–2,95 s**.

**File principali:** `VoOverlayController.cs`

---

## 3. Integrazione e debug

### Problema
Il VO deve essere risolvibile come servizio e testabile senza flusso demo completo.

### Soluzione
- **`GamePlayInstaller`**: creazione runtime `VoOverlay` + componente.
- **`DemoStoryDirector`**: verifica presenza `VoOverlayController` via `ServiceContainer` (log di supporto).
- **`PotDebugConsole`**: pannello **VO Overlay** con testo, scelta registro, pulsanti Mostra/Nascondi.

---

## File modificati (tabella)

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Resources/UI/UIToolkit/VoOverlay/VoOverlay.uxml` | Struttura solo testo + `vo-organic-host` |
| `Assets/_Project/Resources/UI/UIToolkit/VoOverlay/VoOverlay.uss` | Glow registri; stili host / fragment |
| `Assets/_Project/Scripts/UI/UIToolkit/VoOverlay/VoOverlayController.cs` | Typing, Hide/ShowLine, idle blocco + parole, soglia 18%, costanti glitch |
| `Assets/_Project/Scripts/Core/Installers/GamePlayInstaller.cs` | Registrazione / creazione `VoOverlayController` |
| `Assets/_Project/Scripts/Core/DemoStoryDirector.cs` | Risoluzione VO da `ServiceContainer` |
| `Assets/_Project/Scripts/Debug/PotDebugConsole.cs` | Sezione debug VO |

---

## Regole / vincoli rispettati

- Nessun `FindObjectOfType` per il VO; uso **`ServiceContainer`** dove serve risoluzione da debug/demo.
- **Panel Settings** condivisi con Main Menu (`MainMenuPanelSettings`) come da stack UI esistente.
- Modifiche **mirate** al workstream VO; nessun refactor parallelo non necessario.

---

## Note operative (Unity)

- Verificare in **Play** su scena gameplay: VO in basso, leggibilità glow su biomi chiari/scuri; dopo una riga, **solo alcune parole** si spostano leggermente.
- **Pot Debug Console**: testare `ShowLine` / `Hide` e registri A/B senza dipendere dalla demo.
- Eventuali ritocchi futuri: costanti in cima a `VoOverlayController` (`FragmentWiggleProbability`, `BlockGlitch*`, `FragmentGlitch*`) senza cambiare UXML.

---

*Fine DEV REPORT 0086.*
