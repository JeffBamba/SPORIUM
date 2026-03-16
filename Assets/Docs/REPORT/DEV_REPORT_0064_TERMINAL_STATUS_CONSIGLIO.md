# DEV REPORT 0064 — TerminalPot STATUS: sezione Consiglio e legenda Condizioni per Vaso

**Data:** 2026-03-16  
**Oggetto:** Estensione del comando STATUS del TerminalPot con la sezione **▸ CONSIGLIO** per vaso (consigli contestuali in stile black humor Sporium) e conferma della legenda **▸ CONDIZIONI PER VASO** nel tooltip condizioni.  
**Riferimenti:** `PlantCardV3TerminalController.cs`, comando STATUS, `PrintStatusTable()`, `BuildConsiglioForPot()`, `BuildGrowthTooltipLikePlantCardV2()`.  
**Report precedente:** `Assets/Docs/REPORT/DEV_REPORT_0063_UI_GAP_E_RIMOZIONE_LOG.md`

---

## 1. Contesto

- Il comando **STATUS** del TerminalPot stampa già una tabella riepilogativa (ID, Stato, Nome, Stadio, Condizione, Idratazione) e, per ogni vaso con pianta, un blocco **Condizioni per Vaso** con il testo del tooltip (equivalente al conditions_badge di PlantCardV2): stato idratazione, stress luminoso, fertilizzante, giorni nello stadio, stato impianti (acqua/LED).
- Era stata aggiunta una **legenda** per interpretare i valori (OK/NON OK, impianto acceso/spento, LED OFF/Blue/Red). L’utente ha confermato di lasciare la legenda così com’è.
- È stata richiesta una **sezione "Consiglio"** che, in base ai valori attuali (idratazione, luce, fertilizzante, impianti accesi/spenti, giorni nello stadio), fornisca suggerimenti al giocatore (es. aspettare se i parametri sono fuori range ma gli impianti sono accesi; accendere un impianto se spento; consigli generali di crescita). Il testo deve essere in **stile black humor Sporium**.

---

## 2. Lavoro svolto

### 2.1 Logica consigli: `BuildConsiglioForPot()`

- **Metodo:** `BuildConsiglioForPot(PotStateModel state, PlantData plantData)` in `PlantCardV3TerminalController.cs`.
- **Firma:** restituisce `List<string>` (una riga per ogni consiglio), per consentire output su più righe in console.
- **Input usati:** `_potSystemConfig`, stato del vaso (idratazione, giorni LED consecutivi, fertilizzante, giorni nello stadio, `ConditionScore`), requisiti dello stadio (`StageRequirements`), flag `WateringSystemOn`, `LedSystemState`.
- **Casi gestiti (in ordine):**
  - Dati insufficienti / vaso vuoto / requisiti stadio mancanti → messaggio unico (WARN).
  - **Condizione critica** (`ConditionScore < 40`) → invito ad accendere acqua/LED se spenti, altrimenti aspettare.
  - **Acqua:** fuori range + impianto spento → consiglio di accendere (comando WATERING [POT-ID]); fuori range + impianto acceso → aspettare un paio di giorni.
  - **Luce:** fuori range + LED spento → accendere LED BLUE o RED; fuori range + LED acceso → aspettare.
  - **Fertilizzante** fuori range → consiglio FERTILIZE [POT-ID].
  - **Tutto in range:** se giorni sufficienti → “potrebbe avanzare di stadio”; altrimenti → attendere i giorni richiesti.
  - Fallback: “Monitora i valori…”.
- **Tag colore terminale:** `§WARN§`, `§INFO§`, `§TITLE§`, `§CMD§`, `§END§` per coerenza con il resto dell’output STATUS.

### 2.2 Integrazione in STATUS: `PrintStatusTable()`

- **Posizione:** per ogni vaso con pianta, dopo il blocco del tooltip condizioni (righe generate da `BuildGrowthTooltipLikePlantCardV2`), prima della riga vuota che separa un vaso dal successivo.
- **Sequenza:** riga vuota → titolo `§TITLE§▸ CONSIGLIO§END§` → una `AppendRawLine` per ogni elemento restituito da `BuildConsiglioForPot(state, plantData)`.
- **Legenda:** invariata; titolo della sezione condizioni per vaso resta **▸ CONDIZIONI PER VASO** (senza “conditions_badge” nel titolo).

---

## 3. File modificati

| File | Modifica |
|------|----------|
| `Assets/_Project/Scripts/UI/UIToolkit/PlantCardV3/PlantCardV3TerminalController.cs` | `BuildConsiglioForPot`: signature da `string` a `List<string>`, early return con lista singola per dati insufficienti/stadio null; costruzione lista di consigli per acqua/luce/fertilizzante/condizione critica/tutto OK/fallback. In `PrintStatusTable()`: dopo le righe tooltip per ogni pot, append riga vuota + "▸ CONSIGLIO" + righe da `BuildConsiglioForPot`. |

---

## 4. Verifica

- Nessun errore di lint su `PlantCardV3TerminalController.cs`.
- In esecuzione: comando **STATUS** con almeno un vaso con pianta deve mostrare, per quel vaso, il blocco **▸ CONDIZIONI PER VASO** (invariato) e subito sotto **▸ CONSIGLIO** con uno o più messaggi contestuali in base a idratazione, luce, fertilizzante e stato impianti.

---

## 5. Note per QA

- **STATUS:** Aprire TerminalPot, digitare **STATUS**, verificare che per ogni vaso con pianta compaiano le sezioni Condizioni per Vaso e Consiglio; i consigli devono variare al variare di impianti accesi/spenti e valori fuori/in range.
- **Stile:** I testi in Consiglio sono in italiano, tono black humor Sporium; i comandi suggeriti (WATERING, LED BLUE/RED, FERTILIZE) sono evidenziati con tag §CMD§.

---

## 6. Riferimenti

- Piano HUD Zona 2 e 3 + STATUS: `.cursor/plans/terminalpot_hud_zone_2_e_3_d4f87a28.plan.md`
- Tooltip condizioni e `BuildGrowthTooltipLikePlantCardV2`: stesso controller; legenda già presente in output STATUS.

---

*Fine DEV REPORT 0064.*
