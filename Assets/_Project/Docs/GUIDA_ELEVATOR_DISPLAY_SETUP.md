# Guida: Elevator Display In-Game (benchmark piano -1)

Stesso pattern dell'Extractor display (`GUIDA_EXTRACTOR_DISPLAY_SETUP.md`).

## Gerarchia scena

```
ELEV_Elevator
  └── ELEV_Display_LVL_-1
        ├── ElevatorDisplayAnchor    ← sposti/scali QUI in Unity
        ├── Label Text               (TMP legacy, nascosto a runtime)
        └── Arrow                    (TMP legacy, nascosto a runtime)
```

**`ElevatorDisplayAnchor`** è l'unico punto che controlli a mano per posizione e scala del pannello sopra la grafica dello schermo ascensore.

A runtime, sotto quell'anchor, il codice crea automaticamente:

- `ElevatorDisplayCanvas`
- `ElevatorDisplaySurface` (RawImage)
- `ElevatorDisplayUI_Runtime` (UIDocument → RenderTexture)

## Cosa fare in Unity (posizionamento)

1. Apri **`SCN_VaultMap`**
2. Seleziona **`ELEV_Display_LVL_-1`** → figlio **`ElevatorDisplayAnchor`**
3. Con **Scene view** + ascensore visibile:
   - muovi **Transform → Position** finché il **gizmo blu** (wireframe rettangolo) copre lo schermo disegnato sopra le porte
   - regola **Transform → Scale** se il pannello è troppo grande/piccolo (parti da ~`0.004` su tutti e assi, come l'Extractor)
4. **Non spostare** la root `ELEV_Display_LVL_-1` per il display: serve anche all'**Interactable** (tasto E)

Parametri tecnici sul componente **`Elevator In Game Display Runtime`** (solo se serve fine-tuning):

| Campo | Uso |
|--------|-----|
| `Surface Canvas Size` | dimensioni logiche del pannello (default `420 × 240`) |
| `Render Texture Size` | risoluzione RT (default `1680 × 960`) |
| `Anchor Default Local Position/Scale` | fallback solo se manca l'anchor in scena |

## Cosa modificare in UI Builder (font, colori, layout)

Apri e modifica **solo tramite classi USS** (pannello StyleSheet), così le modifiche propagano in game:

- `Assets/_Project/Resources/UI/UIToolkit/ElevatorDisplay/ElevatorDisplay.uxml`
- `Assets/_Project/Resources/UI/UIToolkit/ElevatorDisplay/ElevatorDisplay.uss`

Classi principali:

| Classe | Elemento |
|--------|----------|
| `.elevd-floor-label` | etichetta piano (es. "Floor -1") |
| `.elevd-room-label` | nome ambienti |
| `.elevd-dir-label` | GOING UP / GOING DOWN |
| `.elevd-row-highlight` | riga selezionata (verde) |
| `.elevd-row-denied` | riga -2 / accesso negato |

**Non usare stili inline** sul UXML per font/colori: in UI Builder finirebbero solo sul placeholder e non sul runtime.

Il placeholder in UXML mostra lo **stato a riposo al piano -1** (riga -1 evidenziata) per l'editing visivo; a runtime il codice aggiorna highlight e direzione.

## Verifica

1. Play → vai al piano -1
2. Il pannello UITK è sopra la grafica dello schermo (allineato all'anchor)
3. Apri UI Builder → modifica `font-size` in `.elevd-floor-label` → Play → stesso cambiamento in game
4. Entra in cabina → hint bianco nella compact bottom bar (`zone-post-center`), non sul display

## Duplicazione su altri piani

Per ogni `ELEV_Display_LVL_*`:

1. Duplica la gerarchia (o aggiungi figlio **`ElevatorDisplayAnchor`**)
2. Aggiungi **`Elevator In Game Display Runtime`** + collega anchor
3. Posiziona l'anchor sullo schermo di quel piano

Tutti i display mostrano lo **stesso contenuto sincronizzato** (logica `ElevatorSystem`).
