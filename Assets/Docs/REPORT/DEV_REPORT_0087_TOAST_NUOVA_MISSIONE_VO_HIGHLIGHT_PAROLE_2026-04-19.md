# DEV REPORT 0087 — Toast nuova missione + highlight parole “missione” nel VO (Demo Alpha)

**Data:** 2026-04-19  
**Sprint / contesto:** polish HUD missioni e narrativa demo Alpha: feedback quando viene **assegnata** una missione (non solo al completamento) e **evidenziazione visiva** nel testo VO delle parole/frasi che comunicano istruzioni missione, configurabile dall’asset narrativo.  
**Riferimento piano:** `demo_alpha_1_0_gap_map` (missioni, VO, notifiche)  
**Report precedente:** `DEV_REPORT_0086_VO_OVERLAY_UI_TOOLKIT_TYPING_GLITCH_2026-04-18.md`

---

## Sommario interventi

1. **MissionManager**: evento **`OnMissionAdded`** emesso in **`Append`** dopo la creazione del `MissionChecker`, prima di **`OnMissionsChanged`**.
2. **Notifiche**: spec Foundation **`MIS-NEW`** (Info, 2s) con template IT/EN e payload `{title}`.
3. **HUD missioni** (`ActiveMissionsPanelController`): subscribe a **`OnMissionAdded`** → toast **legacy** `ToastNotificationManager.ShowInfo` con codice `MIS-NEW`, altrimenti **`FoundationNotificationServiceAccessor`** + `PostToast("MIS-NEW", ...)`.
4. **VO**: **`VoLinePresentationOptions`** esteso con **`HighlightWords`** e **`HighlightColorHex`**; **`VoOverlayController`** applica tag rich-text `<color=#...>` durante il typing (plain text in buffer separato; **`_lastPlainTypedText`** per lo switch al layout organico a parole).
5. **Demo Alpha**: **`DemoAlphaNarrativeConfig`** — lista **`Beat1MissionHighlightWords`** e **`MissionHighlightColorHex`**; **`DemoStoryDirector`** passa i valori a **`VoLinePresentationOptions.ForDemoBeat`**.

---

## 1. Toast quando viene assegnata una nuova missione

### Problema
Esisteva feedback toast/VO sul **completamento** missione; mancava un segnale chiaro quando una missione **entra** in lista (es. append da demo o gameplay).

### Soluzione
- **`MissionManager`**: `public event Action<MissionChecker> OnMissionAdded` invocato in **`Append`** con il checker appena creato.
- **`NotificationTypeSpecDefaults`**: tipo **`MIS-NEW`** (categoria System, severità Info, canale Gameplay, cooldown 2s, chiavi `notif.mis_new`).
- **`ActiveMissionsPanelController`**: stesso pattern già usato per **`MIS-DONE`**: prima **ToastNotificationManager**, fallback **Foundation** con **`NotificationPayload.With("title", title)`** dove `title` deriva dalla missione (stesso helper **`GetMissionTitle`** usato per il completamento).

**File principali:** `MissionManager.cs`, `NotificationTypeSpecDefaults.cs`, `ActiveMissionsPanelController.cs`

---

## 2. Parole “missione” evidenziate nel VO (configurabile)

### Problema
Nel beat 1 (e in generale nei VO che danno istruzioni) alcune parole devono saltare all’occhio (es. “Ora vestiti”) senza cambiare il flusso di typing o le frasi.

### Soluzione
- **Authoring**: su **`DemoAlphaNarrativeConfig`** — **`Beat1MissionHighlightWords`** (`List<string>`): frasi o parole, match **letterale** e **case-insensitive**; **`MissionHighlightColorHex`** (default suggerito `#E6C96F`, allineato al giallo missioni HUD).
- **Presentazione**: struct **`VoLinePresentationOptions`** — campi opzionali **`HighlightWords`**, **`HighlightColorHex`**; overload **`ForDemoBeat(advance, highlightWords, highlightColorHex)`**.
- **Runtime VO**: metodo statico **`ApplyHighlight`**: regex con alternation ordinata per **lunghezza decrescente** (match più lunghi prima), escape Regex sulle frasi; output con `<color=#HEX>…</color>`.
- **Typing multi-frase**: `StringBuilder` **plain** aggiornato carattere per carattere; `Label.text` = `ApplyHighlight(plain, …)` ad ogni step.
- **Typing single-block** (`TypeLineRoutine`): stesso schema + **`_lastPlainTypedText`** a fine riga per **`SwitchToOrganicLayout()`** (split parole sul testo **senza** tag colore).

**File principali:** `DemoAlphaNarrativeConfig.cs`, `DemoAlphaNarrativeDefaults.cs`, `DemoStoryDirector.cs`, `VoOverlayController.cs`

---

## File modificati (tabella)

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/Core/MissionSystem/MissionManager.cs` | Evento `OnMissionAdded`; `Append` invoca evento + `OnMissionsChanged` |
| `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/NotificationTypeSpecDefaults.cs` | Spec `MIS-NEW` |
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/ActiveMissionsPanelController.cs` | Subscribe `OnMissionAdded`; `HandleMissionAdded` (toast legacy + fallback Foundation) |
| `Assets/_Project/Scripts/Core/DemoAlphaNarrativeConfig.cs` | `Beat1MissionHighlightWords`, `MissionHighlightColorHex` |
| `Assets/_Project/Scripts/Core/DemoAlphaNarrativeDefaults.cs` | Fallback colore + lista highlight vuota |
| `Assets/_Project/Scripts/Core/DemoStoryDirector.cs` | Passa highlight da config a `VoLinePresentationOptions.ForDemoBeat` |
| `Assets/_Project/Scripts/UI/UIToolkit/VoOverlay/VoOverlayController.cs` | Opzioni highlight; `ApplyHighlight`; typing plain + rich; `_lastPlainTypedText` |

---

## Regole / vincoli rispettati

- Notifiche: riuso **ServiceContainer** per `ToastNotificationManager` e accessor Foundation già presente nel progetto; nessun accoppiamento diretto a scene.
- VO: highlight **opzionale** (lista null/vuota = comportamento precedente invariato per altri chiamanti).
- Match highlight: **non** interpreta wildcard; stringhe author-side devono combaciare col testo VO (spazi inclusi per frasi intere).

---

## Note operative (Unity)

- **Asset**: aprire `Assets/Resources/Demo/DemoAlphaNarrativeConfig.asset` e compilare **`Beat1MissionHighlightWords`** (es. `"Ora vestiti"`, `"armadio"`) e verificare **`MissionHighlightColorHex`**.
- **Verifica Play**: avviare demo / flusso che chiama `MissionManager.Append` → deve comparire toast **Nuova missione: &lt;titolo&gt;** (codice `MIS-NEW` dove supportato).
- **VO**: durante il typing, le sottostringhe configurate devono colorarsi non appena completate nel buffer plain.

---

*Fine DEV REPORT 0087.*
