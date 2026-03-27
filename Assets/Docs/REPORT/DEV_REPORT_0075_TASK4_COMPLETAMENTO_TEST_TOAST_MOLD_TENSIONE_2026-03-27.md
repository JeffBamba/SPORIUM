# DEV REPORT 0075 — Task 4 completato: test finali + fix toast/tooltip (Mold e Tensione Arctic)
**Data:** 2026-03-27  
**Sprint:** Dome Lab 100 — Task 4 Botanical Powers  
**Riferimento Piano:** `roadmap_dome_lab_100_069d5bdb.plan.md`  
**Report Precedente:** DEV_REPORT_0074

---

## Sommario Sessione

Sessione di chiusura Task 4 con focus su validazione end-to-end e rifinitura UX notifiche/tooltip.

In questa run sono stati:

1. completati e verificati gli step rimanenti (4, 5, 6, 7) del Task 4;
2. corretti i comportamenti UI/tooltip legati a riduzione mold e tensione Arctic Hask;
3. rimossa tutta la strumentazione debug temporanea usata per la diagnosi runtime.

---

## Stato Task 4 (Finale)

| Step | Stato finale |
|---|---|
| 1 | ✅ Verificato |
| 2 | ✅ Verificato |
| 3 | ✅ Verificato |
| 4 | ✅ Verificato |
| 5 | ✅ Verificato |
| 6 | ✅ Verificato |
| 7 | ✅ Verificato |
| 8 | ✅ Verificato |

**Conclusione:** Task 4 è completo e testato.

---

## Dettaglio Fix Applicati in questa Sessione

### 1) Mold debug override: evitato reset immediato a 0 al primo EndOfDay

**Sintomo osservato durante Step 4:** con mold impostato via debug (`MoldRiskLevel=2`) e nessun overwatering reale, il rischio scendeva a 0 nello stesso ciclo (percepito come riduzione eccessiva).

**Root cause:** il ricalcolo giornaliero del mold usava `rawExcess=0` e riassegnava subito il livello a 0 anche nei casi di override manuale debug.

**Fix:** in `DayCycleController` è stato introdotto un gate per preservare temporaneamente il livello quando il valore sembra provenire da override debug senza overwatering storico/reale.  
Il consumo/riduzione avviene poi in modo coerente tramite i sistemi esistenti (es. Arctic pulse).

**File:**  
- `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

---

### 2) Toast Mold Level gain/reduce (con motivazione)

**Richiesta gameplay:** avere un toast per ogni livello mold guadagnato e un toast per ogni livello perso con causa comprensibile.

**Fix implementato:**
- nuovo toast `MOLD-GAIN` (1 toast per ciascun livello acquisito nel delta);
- nuovo toast `MOLD-REDUCE` con `cause` (es. `Arctic Hask Effect`, `Ferric Fern Effect`, fallback overwatering rientrato).

**File:**  
- `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/NotificationTypeSpecDefaults.cs`  
- `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

---

### 3) Tooltip notification Foundation: token payload risolti anche nel tooltip

**Sintomo:** nel tooltip della toast comparivano token letterali (es. `{cause}`) invece del testo risolto.

**Root cause:** il tooltip riga notifica usava `Spec.TooltipIt` raw, senza passare dalla formattazione con `payload.Args`.

**Fix:** applicata la stessa pipeline di format usata per il messaggio principale (`NotificationLocalization.Format(..., entry.Payload?.Args)`), così i token vengono risolti correttamente anche nel tooltip.

**File:**  
- `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/FoundationNotificationsPanelController.cs`

---

### 4) Tensione Arctic Hask visibile anche in EFFETTI GLOBALI (TopBar tooltip)

**Problema UX:** il toast `PLT-ARCTIC-TENSION-ON` appariva, ma la sezione `EFFETTI GLOBALI` non riportava lo stato tensione; se il player perdeva il toast non aveva stato persistente leggibile.

**Fix:** `BotanicalPowerFacade.AppendDomeGlobalPlantPowersTooltipLines` ora valuta anche lo snapshot roster+pH e, quando tensione è attiva (`>=2 Arctic` e pH non Neutra), aggiunge blocco warning persistente con:
- numero esemplari Arctic;
- penalità raccolto `%` stimata (`SterilityPressurePercent`);
- indicazione di mitigazione (rientro pH neutro o riduzione Arctic).

**File:**  
- `Assets/_Project/Scripts/Dome/PotSystem/Botanical/BotanicalPowerFacade.cs`  
- `Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs`

---

### 5) PotDebugConsole: chiarito comportamento “+inv” vs “impianta diretto”

Nel corso test è emerso un falso negativo dovuto all’uso dei pulsanti `+inv` (aggiunta inventario) con inventario pieno, invece dei pulsanti diretti nella sezione scrollabile “Impianta Seme (senza inventario)”.

**Nota:** nessun fix runtime necessario sul planting core; issue di utilizzo UI debug.

---

## Validazione Runtime (evidenze)

- Toast tensione Arctic confermato in condizioni corrette (`2 Arctic attivi + pH fuori Neutra`).
- Sezione `EFFETTI GLOBALI` ora mostra warning tensione persistente.
- Toast mold gain/reduce mostrati con contenuti coerenti.
- Tooltip toast mostra la causa risolta (niente token raw).
- Nessuna strumentazione debug residua nei sorgenti a fine sessione.

---

## File Toccati in questa Sessione

- `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`
- `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/NotificationTypeSpecDefaults.cs`
- `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/FoundationNotificationsPanelController.cs`
- `Assets/_Project/Scripts/Dome/PotSystem/Botanical/BotanicalPowerFacade.cs`
- `Assets/_Project/Scripts/UI/UIToolkit/HUD/TopBarController.cs`
- `Assets/Docs/REPORT/DEV_REPORT_0075_TASK4_COMPLETAMENTO_TEST_TOAST_MOLD_TENSIONE_2026-03-27.md`

---

## Chiusura

Task 4 è chiuso sia lato implementazione che lato verifica gameplay/UI runtime.
