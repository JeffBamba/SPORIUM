# DEV REPORT 0083 — Extractor vertical slice: display in-game world-space, stati runtime, proto frutto->spore

**Data:** 2026-04-13  
**Sprint / contesto:** vertical slice **Extractor / Lab** dentro la demo gratuita; costruzione di un display in-game leggibile, authorable con UI Toolkit, con stato reale macchina e variante prototipale visiva di estrazione.  
**Riferimento piano:** `.cursor/plans/demo_gratuita_build_2_vertical_slice.md`; vincoli attivi `.cursor/rules/ui-hud-foundation-ui-builder-parity.mdc`, `architecture-runtime-services.mdc`.  
**Report precedente:** `DEV_REPORT_0082_HUD_TOOLTIP_CURSOR_LOCALIZZAZIONI_ROOM_PLAYERSTATUS_2026-04-01.md`

---

## Sommario interventi

1. Creato il **display in-game dell’Extractor** come UI Toolkit renderizzata su **RenderTexture** e mostrata in **world space** con anchor dedicato.
2. Collegati i contenuti del display agli stati reali della macchina: **Idle**, **InProgress**, **Completed**, con progress bar runtime e output coerente con `Extractor`.
3. Risolti i principali problemi di integrazione runtime: **box nero / sprite non aggiornato**, **font microscopici dopo lo scaling**, **binding rotto quando l’UXML cambia**, **HUD foreground non cliccabili**.
4. Aggiunte **marquee laterale** per il testo output, **localizzazione italiana**, terminologia coerente con il fiction (`Frutto`, `Spore`, `Estrattore`), e una passata di **display blending** per ridurre l’effetto “finto”.
5. Creato un **secondo display prototipale** separato dal primo, dedicato alla lettura visiva del processo **frutto -> spore**, senza testo duplicato.
6. Rifatto il prototipo finale per mostrare un **progresso reale**: il frutto si consuma, il flusso avanza, le spore si attivano in sequenza, il collettore si carica e vira al verde a completamento.
7. Allineati i colori di stato al linguaggio visivo desiderato: **celeste Sporium in progress**, **verde in completed**, sia sul display principale sia sul prototipo.

---

## 1. Fondazione display in-game world-space

### Problema
Serviva uno schermo sopra l’Extractor che mostrasse UI dinamica in gioco, partendo da authoring UI Toolkit e senza dipendere da una PNG statica. La prima strada con `SpriteRenderer` non garantiva una resa affidabile del contenuto runtime.

### Soluzione
- Introdotta una pipeline stabile: **`UIDocument -> RenderTexture -> Canvas world space -> RawImage`**.
- Creato un **anchor dedicato** (`ExtractorDisplayAnchor`) per controllare posizione e scala del display direttamente dalla scena.
- Esposti campi serializzati per posizione/scala anchor, dimensioni canvas e risoluzione RT, in modo da non bloccare il tuning nel codice.
- Aggiunta preview editor con `OnDrawGizmos()` per vedere ingombro e proporzioni del pannello in scena.

**File:** `ExtractorInGameDisplayRuntime.cs`, `SCN_VaultMap.unity`

---

## 2. Stati macchina reali nel display principale

### Problema
Il display doveva leggere davvero il runtime dell’Extractor, non solo simulare testi o animazioni decorative.

### Soluzione
- `ExtractorInGameDisplayRuntime` ora si aggancia a `Extractor` e legge:
  - `State`
  - `ExtractionProgress`
  - `PendingSporeCount`
  - `CompletedCount()`
- Definiti tre stati principali:
  - **Idle**: barra vuota, messaggio di attesa, output in marquee.
  - **InProgress**: progress bar guidata da `ExtractionProgress`, percentuale aggiornata, output coerente con estrazione spore.
  - **Completed**: barra piena, output raccolta, variante `xN` se ci sono più spore pronte.
- L’output inferiore usa sempre una **marquee laterale** per mantenere leggibilità anche con testi lunghi.

**File:** `Assets/_Project/Scripts/UI/UIToolkit/ExtractorDisplay/ExtractorInGameDisplayRuntime.cs`, `Assets/_Project/Resources/UI/UIToolkit/ExtractorDisplay/ExtractorDisplay.uxml`

---

## 3. Robustezza runtime e fix dei problemi emersi

### Problema
Durante la vertical slice sono emersi più problemi concreti:
- display non visibile / box nero;
- font troppo piccoli dopo lo scaling dell’anchor;
- idle animation che smetteva di funzionare dopo modifiche manuali all’UXML;
- collisione con gli input delle HUD del Lab e dell’Extractor.

### Soluzione
- Abbandonata la dipendenza da `SpriteRenderer` in favore del canvas world-space con `RawImage`.
- Separato il controllo di scala/placement nel solo **anchor GameObject**, lasciando il display runtime indipendente.
- Reso il binding UI più tollerante: alcune label (`state`, `detail`) possono mancare senza rompere l’intero runtime.
- Disattivato ogni comportamento di input sul display world-space:
  - `GraphicRaycaster` disabilitato;
  - `CanvasGroup.blocksRaycasts = false`;
  - `CanvasGroup.interactable = false`;
  - `UIDocument.sortingOrder` portato sotto gli overlay HUD;
  - `PickingMode.Ignore` applicato ricorsivamente a tutta la gerarchia UITK.

**File:** `Assets/_Project/Scripts/UI/UIToolkit/ExtractorDisplay/ExtractorInGameDisplayRuntime.cs`

---

## 4. Leggibilità, localizzazione e coerenza fiction

### Problema
Il display risultava poco leggibile e non allineato né alla terminologia italiana né al fiction di SPORAE.

### Soluzione
- Tradotti i testi runtime e placeholder in italiano.
- Terminologia aggiornata:
  - `EXTRACTOR` -> `ESTRATTORE`
  - `Sample` / `Campione` -> `Frutto`
  - output di processo orientato a **estrazione spore**
- Ridotto il churn inutile dei testi nelle fasi di attesa.
- Tenuta la marquee sull’output per consentire lettura anche a distanza o su superfici piccole.

**File:** `Assets/_Project/Resources/UI/UIToolkit/ExtractorDisplay/ExtractorDisplay.uxml`, `Assets/_Project/Scripts/UI/UIToolkit/ExtractorDisplay/ExtractorInGameDisplayRuntime.cs`

---

## 5. Display blending e resa meno “digitale”

### Problema
Una volta funzionante, il display risultava troppo nitido e “appiccicato”, con una resa più da overlay pulito che da piccolo schermo integrato nel macchinario.

### Soluzione
- Introdotte regolazioni RT e una passata di blending runtime:
  - filtro texture configurabile;
  - mipmap / anti aliasing serializzati;
  - campionamento orientato alla leggibilità del testo dove necessario.
- Aggiunto un leggero movimento organico:
  - **breathing** di opacità;
  - **variazione di brightness**;
  - **micro-jitter** in pixel sul `RawImage`.
- Smorzata la palette del pannello principale per integrarlo meglio con la grafica di scena.

**File:** `Assets/_Project/Scripts/UI/UIToolkit/ExtractorDisplay/ExtractorInGameDisplayRuntime.cs`, `Assets/_Project/Resources/UI/UIToolkit/ExtractorDisplay/ExtractorDisplay.uss`

---

## 6. Colori di stato: Sporium celeste -> verde completed

### Problema
Il feedback utente richiedeva che lo stato attivo dell’Extractor avesse una lettura chiara “Sporium celeste”, non un blu scuro generico. Lo stesso linguaggio visivo doveva valere anche per il prototipo.

### Soluzione
- Sul display principale:
  - stato `extd-progress` riallineato a un **cyan/celeste Sporium** su frame, progress track, fill, label percentuale e accenti testo;
  - stato `extd-ready` mantenuto su verde.
- Sul prototipo:
  - stato `extp-progress` portato su palette cyan;
  - stato `extp-ready` portato su verde.

**File:** `Assets/_Project/Resources/UI/UIToolkit/ExtractorDisplay/ExtractorDisplay.uss`, `Assets/_Project/Resources/UI/UIToolkit/ExtractorDisplay/ExtractorSporeProtoDisplay.uss`

---

## 7. Prototipo separato: display visivo frutto -> spore

### Problema
Il display testuale principale non bastava a raccontare visivamente l’idea di “estrazione di spore da un frutto”. Serviva una variante più grafica e leggibile come concept, senza rompere il primo display.

### Soluzione
- Creato un **display duplicato e separato** con runtime dedicato:
  - `ExtractorSporeProtoDisplay.uxml`
  - `ExtractorSporeProtoDisplay.uss`
  - `ExtractorSporeProtoDisplayRuntime.cs`
- Aggiunto in scena un secondo anchor dedicato:
  - `ExtractorDisplayAnchor_ProtoSpore`
- Mantenuta la stessa pipeline world-space del display principale per semplicità d’integrazione e tuning.
- Il prototipo nasce come visual puro, indipendente dai testi del display principale.

**File:** `Assets/_Project/Resources/UI/UIToolkit/ExtractorDisplay/ExtractorSporeProtoDisplay.uxml`, `Assets/_Project/Resources/UI/UIToolkit/ExtractorDisplay/ExtractorSporeProtoDisplay.uss`, `Assets/_Project/Scripts/UI/UIToolkit/ExtractorDisplay/ExtractorSporeProtoDisplayRuntime.cs`, `Assets/_Project/Scenes/SCN_VaultMap.unity`

---

## 8. Refactor del prototipo: niente misure hardcoded, niente testo duplicato

### Problema
La prima versione del prototipo conteneva ancora:
- misure hardcoded poco robuste allo scaling;
- un blocco `extp-content` con testo/progress bar ridondante rispetto al display principale.

### Soluzione
- Riscritta la composizione del prototipo in zone proporzionali:
  - `fruit-zone`
  - `flow-zone`
  - `collector-zone`
- Rimosse le coordinate pixel-based per i dot e sostituite da layout più elastico con percentuali e flex.
- Reso il movimento della scan-band dipendente dalla dimensione risolta del contenitore, non da valori fissi.
- Eliminato completamente `extp-content` e centrata la sola animazione visuale.

**File:** `Assets/_Project/Resources/UI/UIToolkit/ExtractorDisplay/ExtractorSporeProtoDisplay.uxml`, `Assets/_Project/Resources/UI/UIToolkit/ExtractorDisplay/ExtractorSporeProtoDisplay.uss`, `Assets/_Project/Scripts/UI/UIToolkit/ExtractorDisplay/ExtractorSporeProtoDisplayRuntime.cs`

---

## 9. Rifacimento finale dell’animazione proto come progresso reale

### Problema
La prima animazione del prototipo risultava troppo debole e non comunicava davvero il progresso dell’estrazione: sembrava un semplice elemento oscillante.

### Soluzione
- Rifatta la logica visiva del prototipo lungo `ExtractionProgress`:
  - il **frutto interno si riduce** e perde intensità durante il processo;
  - la **linea di trasferimento si riempie** da sinistra a destra;
  - i **dot-spore si accendono in sequenza** con intensità crescente lungo il percorso;
  - il **collector core cresce** progressivamente, dando una lettura materiale dell’accumulo;
  - in `Completed` il flusso risulta pieno e il collettore resta saturo/verde.
- Aggiunto un `flow-line-fill` dedicato nel prototipo per separare base del condotto e avanzamento effettivo.

**File:** `Assets/_Project/Resources/UI/UIToolkit/ExtractorDisplay/ExtractorSporeProtoDisplay.uxml`, `Assets/_Project/Resources/UI/UIToolkit/ExtractorDisplay/ExtractorSporeProtoDisplay.uss`, `Assets/_Project/Scripts/UI/UIToolkit/ExtractorDisplay/ExtractorSporeProtoDisplayRuntime.cs`

---

## 10. Note tecniche e decisioni emerse durante la sessione

### Freeform / prospettiva
L’architettura adottata in questa vertical slice produce superfici **rettangolari** o **quadrate** perché il supporto finale è un `RawImage` su canvas world-space. Per una superficie realmente **freeform** o con prospettiva aderente al profilo del macchinario, il passo successivo corretto è una variante:
- `RenderTexture` come output
- applicata a **mesh/materiale**, non a `RawImage`

### Effetto luce display
Non è stato implementato in questa sessione, ma la direzione consigliata resta:
- `Light2D` o alternativa coerente con la pipeline attiva,
- posizionata come figlia dell’anchor del display,
- con intensità e raggio piccoli per simulare spill luminoso locale, non un faro di scena.

---

## File modificati (tabella)

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/UI/UIToolkit/ExtractorDisplay/ExtractorInGameDisplayRuntime.cs` | Runtime principale world-space, binding stati macchina, marquee, blending, input safety |
| `Assets/_Project/Resources/UI/UIToolkit/ExtractorDisplay/ExtractorDisplay.uxml` | Layout display principale, placeholder e testi coerenti con runtime |
| `Assets/_Project/Resources/UI/UIToolkit/ExtractorDisplay/ExtractorDisplay.uss` | Palette/stati visivi, progress Sporium celeste, ready verde, resa blended |
| `Assets/_Project/Scripts/UI/UIToolkit/ExtractorDisplay/ExtractorSporeProtoDisplayRuntime.cs` | Runtime prototipo, animazione frutto->spore, refactor progressivo senza hardcode critici |
| `Assets/_Project/Resources/UI/UIToolkit/ExtractorDisplay/ExtractorSporeProtoDisplay.uxml` | Gerarchia visuale prototipo, aggiunta `flow-line-fill`, rimozione `extp-content` |
| `Assets/_Project/Resources/UI/UIToolkit/ExtractorDisplay/ExtractorSporeProtoDisplay.uss` | Layout proporzionale, colori `progress`/`ready`, centratura animazione |
| `Assets/_Project/Scenes/SCN_VaultMap.unity` | Wiring runtime display principale e anchor/istanza prototipo in scena |

---

## Regole / vincoli rispettati

- **Nessun duplicato authoring/runtime** sul display principale: il contenuto editabile vive nei file UXML/USS usati dal runtime.
- **Nessun `FindObjectOfType` nuovo** introdotto nel gameplay dell’Extractor; il runtime del display lavora sul riferimento serializzato o sul componente locale.
- **Input safety** preservata: il display world-space non deve bloccare HUD e pannelli foreground.
- **Parità UI Builder / runtime** mantenuta entro i vincoli dell’architettura adottata: il layout è authorable via UXML/USS, mentre il codice applica solo stati/override dinamici runtime.

---

## Note operative (Unity)

- Il display principale e il prototipo si regolano soprattutto tramite:
  - scala/posizione dell’anchor;
  - dimensione canvas world-space;
  - risoluzione RenderTexture.
- Se il prototipo dovrà seguire una superficie inclinata o non rettangolare, non conviene forzare oltre il `RawImage`: serve una variante mesh-based.
- Dopo le ultime modifiche è stato eseguito controllo sui file toccati con `ReadLints`: **nessun errore** rilevato.

---

*Fine DEV REPORT 0083.*
