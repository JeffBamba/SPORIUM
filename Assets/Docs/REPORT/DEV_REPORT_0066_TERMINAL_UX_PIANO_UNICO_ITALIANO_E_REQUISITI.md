# DEV REPORT 0066 — Terminal POT: piano unico (italiano, UX requisiti, condizione/trend, luce 20–80%, rotellina)

**Data:** 2026-03-17  
**Oggetto:** Implementazione piano unico chat: HUD pot slots con colori famiglia, toast Foundation (cambio stadio + italiano), consiglio con comandi e logica LED/acqua, parametro luce in range 20–80% anche con LED spento, stadio GROWTH → ADULTA, requisito condizione slegato dal trend, righe esplicative in grigio sotto ogni requisito STATUS, rimozione rotellina di caricamento.  
**Riferimenti:** `PlantCardV3TerminalController.cs`, `PlantCardV3_Terminal.uss`, `GrowthPointsCalculator.cs`, `NotificationTypeSpecDefaults.cs`, `PlantCardFormatters.cs`, `ConditionGrowthModifier.cs`.  
**Report precedente:** `Assets/Docs/REPORT/DEV_REPORT_0065_TERMINAL_STATUS_FLOW_E_TYPEWRITER_FLUSH.md`

---

## 1. Contesto

- In chat è stato richiesto di applicare un **piano unico** di modifiche al terminale POT e alla logica di crescita/avanzamento, con due chiarimenti importanti:
  - **Punto 5 (luce):** Se il parametro stress luce è **già in range** (20%–80%), conta per la crescita **anche con LED spento**. Range corretto: 20%–80% (0–20% no, 80–100% burn risk). Es.: 60% con LED spento = parametro OK.
  - **Punto 7 (condizione/trend):** Il **trend** è informativo (migliora/peggiora/stabile), non un obiettivo da “matchare”. La **condizione** guida il trend. Anche con trend negativo, se la condizione è **nel range ammesso** per avanzare allo stadio successivo, la pianta **deve poter** avanzare. Il requisito avanzamento non deve dipendere dal trend.
- In seguito sono state richieste: **righe esplicative** sotto ogni parametro in REQUISITI E AVANZAMENTO (in grigio chiaro), che spieghino perché è OK/non OK e se la situazione sta migliorando (es. “Impianto goccia attivo, situazione in miglioramento” / “Impianto spento, usa comando per attivarlo”); **rimozione della rotellina** che ruota quando si lancia un comando.

---

## 2. Lavoro svolto

### 2.1 HUD pot slots (Zona 2/3)

- **Slot vuoto:** Label con testo `POT-xxx` (da `PotId` del vaso assegnato allo slot); box e label in **grigio** (classe `pcv3-hud-pot-slot-empty`; in USS aggiunta regola `.pcv3-hud-pot-slot-empty .pcv3-hud-pot-slot-label` con colore grigio).
- **Slot con pianta:** Label `POT-xxx`; colore **per famiglia** (verde Standard, giallo Pure, rosso Evil) su bordo dello slot e sulla label. Aggiunte classi USS: `pcv3-hud-pot-slot-standard`, `pcv3-hud-pot-slot-pure`, `pcv3-hud-pot-slot-evil` con rispettivi colori bordo e label.
- In **UpdateHudSlotVisuals()**: per ogni slot si aggiorna il testo della label (child con classe `pcv3-hud-pot-slot-label`); se vuoto si applica empty e si rimuovono le classi famiglia; se con pianta si rimuove empty e si aggiunge la classe famiglia in base a `plantData.Family`.

**File:** `PlantCardV3_Terminal.uss`, `PlantCardV3TerminalController.cs`.

### 2.2 Toast Foundation — cambio stadio e italiano

- **STAGE-UP-001:** Severity cambiata da `Warning` a **Success**; template IT da `"Stage up: {potId} → {stage}"` a **"Cambio stadio: {potId} → {stage}"**.
- **VIS-001:** Template IT da `"Visitor waiting for player!"` a **"Visitatore in attesa"**.

**File:** `NotificationTypeSpecDefaults.cs`.

### 2.3 Consiglio (CONSIGLIO) — comandi e logica LED/acqua

- **Solo se serve:** Il consiglio “accendi il LED” viene mostrato **solo** quando `!ledOn && !lightOk` (se lo stress è già in range con LED spento non si suggerisce di accendere).
- **Comando esplicito:** Quando si suggerisce di accendere il LED si mostra il comando con colorazione §CMD§, includendo il PotId (es. `§CMD§LED BLUE POT-001§END§`). Quando si suggerisce di **spegner**e il LED (stress > 80%) si aggiunge il comando: `§CMD§LED OFF {state.PotId}§END§`.
- Range stress luce nei testi consiglio allineato a **20%–80%**.

**File:** `PlantCardV3TerminalController.cs` (BuildConsiglioForPot).

### 2.4 Parametro luce in range 20–80% anche con LED spento

- **GrowthPointsCalculator.IsLightInOptimalRange:** Quando `LedSystemState == Off`, il punto luce viene assegnato se lo **stress è nel range 20%–80%** (non più 0–100%). Costanti `LightStressOkMin = 20f`, `LightStressOkMax = 80f`.
- **GetStatusForecastForPot (LedOk):** Stesso range 20–80%. Se LED spento, `result.LedOk = stressInRange`; se LED acceso, `result.LedOk = IsLedRequirementMet && stressInRange`. Calcolo `stressPercentage` unificato.
- **BuildConsiglioForPot:** `lightOk` e messaggi con range **20–80%** (costanti 20 e 80).
- **Testi UI:** Tutte le occorrenze “20%-70%” / “20-70%” sostituite con **“20%-80%”** / “20-80%” (requisiti STATUS, Come leggere i dati, help stress luce, ecc.).

**File:** `GrowthPointsCalculator.cs`, `PlantCardV3TerminalController.cs`.

### 2.5 Stadio GROWTH in italiano = “ADULTA”

- **PlantStageLabel:** `PlantStage.Growth` da `"CRESCITA"` a **"ADULTA"**.
- **PlantCardFormatters.FormatGrowthStage:** `PlantStage.Growth` da `"GROWTH"` a **"ADULTA"** (coerenza contesto italiano).

**File:** `PlantCardV3TerminalController.cs`, `PlantCardFormatters.cs`.

### 2.6 Requisito condizione slegato dal trend

- **Requisito condizione per avanzamento:** Non dipende più dal trend (Up/Down/Stable). Il requisito è **solo** “condizione non bloccante”.
- **Implementazione:** In entrambi i blocchi (dettaglio “REQUISITI AVANZAMENTO” e STATUS “REQUISITI E AVANZAMENTO”) si usa `conditionReqOk = !f.BlockedByCondition`. Testo requisito: “Richiesto: non critica/appassita”.
- **Messaggio requisito non soddisfatto:** Da “Condizione non in range per trend” a **“Condizione critica o appassita (avanzamento bloccato)”**.

**File:** `PlantCardV3TerminalController.cs`.

### 2.7 Righe esplicative sotto ogni requisito (STATUS)

- Sotto **ogni** riga di requisito in **REQUISITI E AVANZAMENTO** (STATUS) è stata aggiunta una riga in **grigio chiaro** (§DIM§, colore `#A8A8A8`) che spiega:
  - **Condizione:** “------ Condizione non bloccante” / “------ Condizione critica o appassita: avanzamento bloccato”.
  - **Idratazione:** Se impianto goccia **attivo** → “------ Impianto goccia attivo, parametro in range” oppure “------ Impianto goccia attivo, situazione in miglioramento.” Se **spento** → “------ Impianto spento, usa comando WATERING per attivarlo.”
  - **Fertilizzante:** “------ Fertilizzante in range” / “------ Fuori range, usa comando FERTILIZE [POT-ID] se necessario.”
  - **Stress luce:** Se **LED acceso** → “------ LED acceso, stress in range” oppure “------ LED acceso, situazione in miglioramento (o spegni se stress >80%).” Se **LED spento** → “------ LED spento ma stress già in range” oppure “------ LED spento, usa comando LED BLUE/RED per attivarlo.”
  - **Rischio muffa:** “------ Rischio sotto soglia” / “------ Rischio elevato: ventila o riduci umidità.”
  - **Giorni ottimali:** “------ Giorni consecutivi ottimali sufficienti” / “------ Servono più giorni con tutti i parametri in range.”
  - **Punti crescita:** “------ Punti W+L+F sufficienti” / “------ Servono più punti (acqua, luce, fertilizzante in range).”
- Aggiunto il tag **§DIM§** nella funzione **ParseColors** con colore grigio chiaro.

**File:** `PlantCardV3TerminalController.cs`.

### 2.8 Rimozione rotellina di caricamento

- **ShowLoadingSpinner(bool show):** Se `show == true` la funzione termina subito senza mostrare l’indicatore (nessuna aggiunta di `pcv3-loading-visible`, nessuna schedulazione della rotazione). Se `show == false` si rimuove comunque la classe per nascondere l’indicatore. La rotellina che ruotava durante l’esecuzione dei comandi **non appare più**. I messaggi “[SYS] Richiesta registrata…” e il lampeggio restano invariati.

**File:** `PlantCardV3TerminalController.cs`.

---

## 3. File modificati

| File | Modifica |
|------|----------|
| `Assets/_Project/UI/UIToolkit/PlantCardV3/PlantCardV3_Terminal.uss` | Classi `.pcv3-hud-pot-slot-empty .pcv3-hud-pot-slot-label` (grigio); `.pcv3-hud-pot-slot-standard`, `.pcv3-hud-pot-slot-pure`, `.pcv3-hud-pot-slot-evil` per bordo e label (verde/giallo/rosso). |
| `Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs` | UpdateHudSlotVisuals: label POT-xxx, classi famiglia/empty. BuildConsiglioForPot: range 20–80%, solo “accendi LED” se !ledOn && !lightOk, comando LED OFF con PotId. GetStatusForecastForPot: LedOk 20–80% con LED off. Requisiti condizione: conditionReqOk = !BlockedByCondition, testo “non critica/appassita”, messaggio “Condizione critica o appassita (avanzamento bloccato)”. PlantStageLabel Growth => "ADULTA". Tutti i testi 20%-70% → 20%-80%. REQUISITI E AVANZAMENTO: riga §DIM§ sotto ogni requisito (condizione, idratazione, fertilizzante, stress luce, muffa, giorni ottimali, punti). ParseColors: §DIM§ → grigio #A8A8A8. ShowLoadingSpinner: se show true return (rotellina mai mostrata). |
| `Assets/_Project/Scripts/Dome/PotSystem/Growth/GrowthPointsCalculator.cs` | IsLightInOptimalRange: con LED off, range 20–80% per stress (non 0–100%). |
| `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/NotificationTypeSpecDefaults.cs` | STAGE-UP-001: Severity Success, template IT “Cambio stadio: {potId} → {stage}". VIS-001: template IT “Visitatore in attesa”. |
| `Assets/_Project/Scripts/UI/UIToolkit/PlantCard/Helpers/PlantCardFormatters.cs` | FormatGrowthStage: PlantStage.Growth => "ADULTA". |

---

## 4. Riepilogo per QA

- **HUD pot slots:** Slot vuoto = label POT-xxx e box/label grigi. Con pianta = label POT-xxx e colore per famiglia (verde Standard, giallo Pure, rosso Evil).
- **Toast:** Cambio stadio = toast **verde** (Success) con testo “Cambio stadio: {potId} → {stage}". Visitatore = “Visitatore in attesa” in italiano.
- **Consiglio:** Se stress luce già in range con LED spento non viene suggerito di accendere il LED. Quando si suggerisce di spegnere il LED viene mostrato il comando §CMD§LED OFF PotId§END§.
- **Luce 20–80%:** Con LED spento, se stress è tra 20% e 80% il parametro conta come OK per crescita e per requisiti STATUS. Range mostrato ovunque: 20%-80%.
- **Stadio crescita:** In italiano lo stadio GROWTH è mostrato come **ADULTA** (non CRESCITA/GROWTH).
- **Condizione e avanzamento:** Il trend non blocca l’avanzamento. Requisito condizione = “non critica/appassita”; se condizione è bloccante il messaggio è “Condizione critica o appassita (avanzamento bloccato)”.
- **STATUS requisiti:** Sotto ogni riga (Condizione, Idratazione, Fertilizzante, Stress luce, Rischio muffa, Giorni ottimali, Punti crescita) compare una riga in **grigio chiaro** che spiega perché è OK/non OK e se l’impianto (goccia/LED) è attivo o spento e se la situazione sta migliorando.
- **Rotellina:** Non compare più quando si lancia un comando; restano i messaggi di sistema e il delay prima dell’esecuzione.

---

## 5. Note tecniche

- **Condizione e trend:** `ConditionGrowthModifier.BlocksAdvancement(condition)` restituisce true per Critica/Appassita/Morta. Il forecast già calcola `BlockedByCondition`; il requisito “Condizione” in STATUS ora è solo “non bloccante”, indipendente da `f.Trend`.
- **§DIM§:** Nuovo tag per testo secondario (grigio); usato solo nelle righe esplicative sotto i requisiti. In altri contesti (es. StripSectionTags / FormatConsiglioLineWithCommands) non viene usato.

---

*Fine DEV REPORT 0066.*
