# DEV REPORT — Tooltip di contesto per notifiche Toast

**Data:** 2025-03-04  
**Oggetto:** Assegnazione tooltip di contesto (`TooltipIt`) a tutte le notifiche toast esistenti.

---

## 1. Contesto

- In **NotificationTypeSpec** è presente il campo opzionale **`TooltipIt`** (stringa, max ~3 righe, `\n` per andare a capo).
- La funzione **`Spec()`** in `NotificationTypeSpecDefaults.cs` espone il 12° parametro opzionale **`tooltipIt = null`**.
- Il **mini-tooltip** al passaggio del mouse sulle righe toast è già implementato in **FoundationNotificationsPanelController** (hover → popup con `entry.Spec?.TooltipIt` in stile Foundation).

## 2. Lavoro svolto

È stato assegnato un **tooltip di contesto in italiano** (2–3 righe) a **tutte** le chiamate `Spec(...)` che ne erano prive, in modo che ogni toast abbia un testo esplicativo al hover.

### 2.1 Categorie coperte

| Categoria | Codici / note |
|-----------|----------------|
| **Pot legacy (success)** | POT-WATER/LIGHT/PLANT/FERTILIZE/HARVEST/SPRAY/UPROOT/ACTION-SUCCESS |
| **Pot legacy (failed)** | POT-WATER/LIGHT/PLANT/FERTILIZE/HARVEST/SPRAY/UPROOT/ACTION-FAILED |
| **PH / Piante / Acqua** | PH-COUNTDOWN-001, PH-DEATH-001, PLANT-DEATH-001, WATER-001 |
| **Condensazione** | COND-001 … COND-008 |
| **Inventario / Sistema** | SPORE-001, INV-FRUIT-001, LGT-001/003/004, VIS-001, INV-000, ITEM-GET, ADDED-TO-INVENTORY, INV-REM, HYD-001 |
| **Diario / Ricerca / Wiki** | SPORAE-001, RES-001, WIKI-UNLOCK, RES-UNLOCK |
| **Reputazione** | REP-CHANGE |
| **Acqua / Fertilizzante / Muffe** | WAT-OVR-WARN, WAT-OVR-DANGER, FRT-MISSING-BLOCK, FRT-OUT-RANGE, MLD-RISK-CRIT, MLD-INFESTED, MLD-201 |
| **Lore** | LOR-VLT-001, LOR-ECO-001, LOR-MKT-001 |

(Le notifiche che avevano già il tooltip in precedenza sono state lasciate invariate.)

### 2.2 Contenuto dei tooltip

- **Scopo:** spiegare perché appare la toast e/o cosa fare.
- **Lingua:** italiano.
- **Formato:** 2–3 righe, con `\n` per andare a capo.
- **Stile:** contestuale e coerente con il canale (es. "Raccogli l'acqua per evitare perdite", "Controlla il vaso per dettagli").

## 3. File modificati

| File | Modifica |
|------|----------|
| `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/NotificationTypeSpecDefaults.cs` | Aggiunto il 12° argomento `tooltipIt` a tutte le `Spec(...)` che non lo avevano. |

Nessun altro file è stato toccato (modello `NotificationTypeSpec`, helper `Spec()` e UI del tooltip erano già pronti).

## 4. Verifica

- Nessun errore di lint su `NotificationTypeSpecDefaults.cs`.
- Ogni `Spec(...)` ha ora fino a 12 parametri; ove il tooltip non era necessario in passato, è stato comunque aggiunto un testo di contesto per uniformità.

## 5. Note per QA

- In gioco, aprire il pannello notifiche Foundation e passare il mouse sulle righe delle toast: deve comparire il popup con il testo di contesto in italiano.
- Verificare che non ci siano toast senza tooltip (tutte le spec in defaults ora ne hanno uno).

## 6. Riferimenti

- Specifica tooltip: campo `TooltipIt` in `NotificationTypeSpec.cs`.
- UI tooltip: `FoundationNotificationsPanelController.cs` (assegnazione `TooltipText = entry.Spec?.TooltipIt`).
- Defaults: `NotificationTypeSpecDefaults.BuildDefaults()`.
