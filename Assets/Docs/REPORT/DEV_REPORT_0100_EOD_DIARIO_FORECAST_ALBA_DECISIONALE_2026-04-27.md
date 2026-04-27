# DEV REPORT 0100 — EOD: Diario narrativo pH-driven, Forecast domani-only e Alba decisionale

**Data:** 2026-04-27  
**Sprint / contesto:** Iterazione UX/gameplay su End Of Day per ridurre ridondanza informativa, aumentare leggibilità operativa e rafforzare tono narrativo Sporium nella sequenza Snapshot → Diario → Forecast → Alba.  
**Riferimento piano:** refinements EOD emersi in chat (focus: valore decisionale e coerenza narrativa).  
**Report precedente:** `DEV_REPORT_0099_EOD_SNAPSHOT_STRUTTURA_DATI_2026-04-27.md`

---

## Sommario interventi

1. **DIARIO** trasformato da mini-report a frammento narrativo del Biologo: testo lungo, variabile, romanzato e guidato da eventi reali di giornata.
2. Tono diario agganciato al **drift pH**: asse `EVIL (acido)` vs `PURE (basico)` con modulazione paranoia/dissociazione vs lucidità/malinconia.
3. **Forecast** riscritto in modalità **solo domani**: rimosso blocco `[OGGI]`, introdotti `POT prioritari`, `Missioni e obiettivi`, `Piano operativo consigliato`.
4. Gestito caso `azioni = 0` tra Diario e Forecast: visualizzazione esplicita di blocco ricerca notturna non disponibile con messaggio dedicato.
5. **Panoramica Alba** convertita in brief operativo: stato notte, priorità immediate, opportunità, piano avvio turno, baseline economia.

---

## Statistiche e progresso

### Righe di codice

- Scope misurato con comando:
  - `git diff --numstat -- Assets/_Project/Scripts/UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs`
- Totale modifiche `.cs` in questa iterazione:
  - `Assets/_Project/Scripts/UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs` → **+814 / -66**
- Nessun altro file di codice toccato in questo blocco.

### Sistemi funzionanti

- **Verificato da codice/lint:** nessun errore linter nel file modificato.
- **Da validare in Editor (Play):**
  - Diario pH-driven (tono `EVIL/PURE` coerente con `CurrentPh`).
  - Gating step ricerca quando `ActionsLeft == 0` con messaggio intermedio.
  - Forecast domani-only con ranking POT prioritari e piano azioni.
  - Alba decisionale con top rischi/opportunità e piano avvio turno.

### Bug risolti

- **1 bug compilazione corretto** durante iterazione:
  - errore tuple nella mappa condizioni POT (`(PotId, MoldRiskLevel, IsInfested)` assegnata a tuple ridotta) risolto con mapping esplicito `(MoldRiskLevel, IsInfested)`.

### Progresso gameplay / prodotto

- Il player non legge più blocchi ridondanti tra Snapshot e Forecast: ogni schermata ha un ruolo chiaro.
- Il Diario diventa elemento identitario di Sporium (voce del Biologo), non report meccanico.
- Forecast e Alba ora guidano decisioni pratiche del turno successivo con priorità esplicite.
- Migliora la trasparenza nei casi senza azioni: ricerca notturna non “sparisce”, ma viene comunicata.

---

## 1. Diario EOD: da report tecnico a frammento narrativo

### Problema

- Il vecchio `PopulateDiario()` mostrava poche righe statiche (raccolta/irrigazione + footer), percepite come deboli e non distintive.

### Soluzione

- Rifattorizzato `PopulateDiario()` verso builder narrativo con:
  - apertura + corpo + frammento lore + chiusura;
  - varianti deterministiche per giorno (coerenza intra-sessione);
  - contenuto romanzato da eventi reali (`DayActivityLog`, stato biologo, missioni, storage, ecc.).
- Aggiornati titolo e chiusura step 3 in chiave “frammento personale”.

**File interessato:**  
`Assets/_Project/Scripts/UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs`

---

## 2. Tono Diario guidato dal pH (EVIL ↔ PURE)

### Problema

- La distorsione narrativa era legata solo a progressione tempo/beat; mancava legame diretto con stato biochimico reale della run.

### Soluzione

- Introdotto asse narrativo pH con `ComputeDiaryPhAlignment()` su `PhSystem.CurrentPh`:
  - `acido/EVIL` → testo più oscuro, paranoide, dissociato;
  - `basico/PURE` → testo più lucido, malinconico, speranza fragile;
  - neutro → tono ambiguo.
- Integrazione con livello di distorsione già esistente (giorni/beat) per escalation narrativa verso late game.

**File interessato:**  
`Assets/_Project/Scripts/UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs`

---

## 3. Forecast domani-only (orientato al fare)

### Problema

- Blocco `[OGGI]` duplicava quanto già presente nello Snapshot e riduceva il valore decisionale dello step Forecast.

### Soluzione

- `PopulateForecast()` riscritto con struttura orientata al giorno successivo:
  - `COSA SUCCEDE DOMANI` (azioni, deriva pH, pH atteso)
  - `POT PRIORITARI` (rischi ordinati)
  - `MISSIONI E OBIETTIVI` (missioni attive actionable)
  - `PIANO OPERATIVO CONSIGLIATO` (checklist prime mosse)
- Introdotto ranking rischi POT con scoring composito (infestazione, muffa, stress LED, condizione, prossimità cambio stadio).

**File interessato:**  
`Assets/_Project/Scripts/UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs`

---

## 4. Ricerca notturna: messaggio esplicito quando azioni finite

### Problema

- Con `ActionsLeft == 0` il flow saltava direttamente alla previsione senza spiegazione, creando buco comunicativo.

### Soluzione

- Inserito passaggio intermedio nello step ricerca:
  - titolo/subtitle dedicati alla non disponibilità;
  - pulsanti ramo nascosti;
  - solo bottone `Continua → Previsione`.
- Conservata logica standard quando azioni disponibili.

**File interessato:**  
`Assets/_Project/Scripts/UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs`

---

## 5. Panoramica Alba decisionale

### Problema

- Alba presentava metriche utili ma poco operative; mancava priorità immediata sui POT e piano d’avvio turno.

### Soluzione

- `PopulateDawn()` riconfigurato in brief operativo:
  - `STATO NOTTE`
  - `PRIORITA IMMEDIATE`
  - `FINESTRA OPPORTUNITA`
  - `PIANO AVVIO TURNO`
  - `ECONOMIA baseline`
- Tooltip riallineati alla nuova semantica decisionale.

**File interessato:**  
`Assets/_Project/Scripts/UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs`

---

## File modificati

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/UI/UIToolkit/EndOfDay/EndOfDaySequenceController.cs` | Refactor sostanziale step 3/4/5/8: diario narrativo pH-driven, ricerca no-azioni con messaggio esplicito, forecast domani-only, alba decisionale |

---

## Regole / vincoli rispettati

- Integrazione su controller esistente EOD senza duplicare pannelli o flow paralleli.
- Utilizzo servizi runtime già presenti (`ServiceContainer`, `DayActivityLog`, `MissionManager`, `PhSystem`, `DomePotRegistry`) senza introdurre scan distruttivi.
- Coerenza con prodotto unico demo/full: stessa base logica, con modulazione narrativa tramite stato sessione/beat dove disponibile.

---

## Note operative (Unity)

- Verificare in Play Mode tre scenari:
  - `ActionsLeft > 0` (ricerca notturna disponibile);
  - `ActionsLeft == 0` (messaggio blocco ricerca e passaggio a Forecast);
  - pH in bande opposte (`Ultra Acido` vs `Ultra Basico`) per conferma cambio tono Diario.
- QA consigliata su leggibilità testi lunghi in Step 3 (scroll + pacing typewriter).
- Possibile miglioramento successivo: colorazione semantica esplicita nel brief Alba (rosso rischio / verde opportunità) per scansione ancora più rapida.

---

*Fine DEV REPORT 0100.*
