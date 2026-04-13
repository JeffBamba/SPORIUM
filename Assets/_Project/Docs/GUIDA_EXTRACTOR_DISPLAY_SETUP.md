# Guida: Extractor Display In-Game

Questa versione del display `Extractor` non richiede un setup manuale complesso in scena.

Il sistema:

- usa **`ExtractorDisplayAnchor`** come GameObject dedicato per **posizione e scala** del display;
- crea a runtime, sotto quell'anchor, una superficie `World Space Canvas + RawImage`;
- renderizza sopra quella superficie il layout UI Toolkit definito in:
  - `Assets/_Project/Resources/UI/UIToolkit/ExtractorDisplay/ExtractorDisplay.uxml`
  - `Assets/_Project/Resources/UI/UIToolkit/ExtractorDisplay/ExtractorDisplay.uss`

## Dove agisce in scena

Gerarchia di riferimento:

- `ROOM_Dome`
  - `Extractor`
    - `ExtractorDisplayAnchor`

`ExtractorDisplayAnchor` e il punto che puoi spostare e scalare in scena.
La superficie tecnica del display viene creata automaticamente sotto quell'anchor a runtime.

Il componente **`ExtractorInGameDisplayRuntime`** e ora presente direttamente su `Extractor`, quindi in Inspector puoi regolare anche:

- `Anchor Default Local Position`
- `Anchor Default Local Scale`
- `Surface Canvas Size`
- `Surface Sorting Order`
- `Render Texture Size`

Questi parametri appartengono solo allo spin-off `ExtractorDisplay` e non toccano la HUD Foundation condivisa.

## Cosa fa il runtime

All'avvio:

1. `Extractor` aggiunge/recupera `ExtractorInGameDisplayRuntime`.
2. Il runtime cerca `ExtractorDisplayAnchor` sotto `Extractor`.
3. Sotto quell'anchor crea o riusa:
   - `ExtractorDisplayCanvas`
   - `ExtractorDisplaySurface` (`RawImage`)
4. Crea un host `UIDocument` runtime (`ExtractorDisplayUI_Runtime`).
5. Collega la `RenderTexture` del `UIDocument` alla `RawImage`.

## Stati visivi

- `Idle`
  - display acceso
  - testo standby
  - animazione testuale lieve
- `InProgress`
  - testo `PROCESSING SAMPLE`
  - progress bar
  - percentuale aggiornata
- `Completed`
  - testo `COLLECTION READY`
  - riepilogo output pronto

## Asset da modificare in UI Builder

Per cambiare l'estetica del display:

1. Apri `Assets/_Project/Resources/UI/UIToolkit/ExtractorDisplay/ExtractorDisplay.uxml`
2. Apri `Assets/_Project/Resources/UI/UIToolkit/ExtractorDisplay/ExtractorDisplay.uss`
3. Modifica testi placeholder, colori, background e struttura

Le modifiche si riflettono nel display runtime senza dover rifare la scena.

## Verifica veloce

1. Apri `SCN_VaultMap`
2. Entra in Play
3. Guarda `Extractor`
4. Verifica che il display sia acceso anche in idle
5. Interagisci con l'Extractor e avvia un'estrazione
6. Verifica la barra di progresso
7. A completamento, verifica il messaggio di raccolta pronta

## Note

- Questo approccio evita il path fragile `SpriteRenderer + MaterialPropertyBlock`.
- Il display è pensato come vertical slice su `Extractor` e può diventare il pattern standard per gli altri macchinari.
