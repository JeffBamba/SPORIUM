# Discrepanze GDD 42 v.31/01/2026 vs MAIN REPO

**Data analisi:** 01/02/2026  
**Riferimento:** GDD 42 v.31/01/2026 (Notion)  
**Branch analizzato:** MAIN

---

## 1. Dome 2.0 — Numero vasi e slot

| GDD | Codice |
|-----|--------|
| "quattro vasi attivi, tre slot passivi" (7 totali) | `PotSystemConfig.MAX_POTS_PER_ROOM = 10`; nessuna distinzione attivi/passivi nel codice |

**Discrepanza:** Il GDD fissa 4 attivi + 3 passivi; il codice permette fino a 10 vasi per stanza e non modella la distinzione attivi/passivi.

**Suggerimento GDD:** Specificare se 4+3 è il target di design per la Demo Build o il valore canonico; in caso di design confermato, aggiungere nota "Da implementare: gating 4 attivi + 3 passivi (attuale max 10 per room)."

---

## 2. Stadi di crescita — Nomenclatura

| GDD | Codice |
|-----|--------|
| Seme → Germoglio → **Crescita** → Fioritura → Raccolto → Riposo | `PlantStage`: Seed, Sprout, **Growth**, Flowering, HarvestReady, Resting (commento: "Accrescimento vegetativo") |

**Nota:** In ENV-001 (Dome Centrale) il GDD usa "Vegetative pre‑Flowering". Nel codice l’etichetta tecnica è `Growth`; in UI terminal compaiono SEED, SPROUT, GROWTH, FLOWER, RIPE, REST.

**Suggerimento GDD:** Decidere un’unica etichetta ufficiale per lo stadio 3: "Crescita" (GDD 42) o "Vegetative" (ENV-001) e indicarla nel glossario UI per coerenza con l’implementazione.

---

## 3. Mold Risk

| GDD | Codice |
|-----|--------|
| "sistema Mold Risk basato su overwatering prolungato" | `MoldSystem`, `MoldRiskLevel` 0–3, `overwateringDaysThreshold`, blocco crescita per famiglia (Pure ≥1, Standard ≥2, Evil mai) |

**Stato:** Allineato.

---

## 4. pH e drift

| GDD | Codice |
|-----|--------|
| Drift pH, Pure/Evil, influenza su mutazioni e muffe | `PhSystem`, `PhGrowthModifier`, drift per pianta, sinergia con Mold |

**Stato:** Allineato.

---

## 5. Laboratorio 2.0 — 4 step

| GDD | Codice |
|-----|--------|
| Step 1 Estrazione Spore · Step 2 Maturazione (2 fasi, Catalizzatore) · Step 3 Fusione (2 spore → Pre-Seme, FIXED/STABLE/UNSTABLE) · Step 4 Incubatore + Reagenti X/Y/nessuno | Presenti: `LabMinigameExtractor`, `LabCatalizzatore`, Pipette, Microscope, `Incubator` |

**Nota:** Non verificata la corrispondenza 1:1 degli step (ordine, tipo genetico FIXED/STABLE/UNSTABLE, reagenti X/Y).

**Suggerimento GDD:** Aggiungere una sottosezione "Stato implementazione Lab" che mappi Step 1–4 ai componenti repo (Extractor, Catalizzatore, Fusione/Pipette?, Incubatore) per facilitare audit.

---

## 6. Ciclo Notturno — Diario SPORAE e Forecast

| GDD | Codice |
|-----|--------|
| Diario SPORAE: riepilogo giornaliero + frammento narrativo; Forecast; Night Research (3 rami); Wiki/Knowledge Tree | Forecast: implementato (PlantCardV3, TopBar). Night Research: 3 opzioni (`HistoricalArchive`, `BotanicalDatabase`, `VaultProtocols`). Wiki: `WikipediaUI`. Diario: `PlantDiaryManager` (note per vaso); notifica "SPORAE Diary" (SPORAE-001) |

**Discrepanza:** Il "Diario SPORAE" come schermata con riepilogo giornaliero + frammento narrativo glitchato non risulta implementato come flusso dedicato; esistono note per vaso e riferimento SPORAE nelle notifiche.

**Suggerimento GDD:** In Ciclo Notturno distinguere: (A) Diario per vaso (implementato), (B) Diario SPORAE narrativo/riepilogo (da implementare o WIP), e aggiornare lo stato di (B).

---

## 7. Economia CRY e fazioni

| GDD | Codice |
|-----|--------|
| CRY, vendita a Custodi / Culto Muffa / Mercanti Ombra / Ipnotici, Mercato Nero | `EconomySystem`, `CurrentCRY`, consumo CRY (LED, irrigazione), TopBar CRY; cartella `BlackMarket` |

**Stato:** CRY e Black Market presenti; logica fazioni (Custodi, Culto, Mercanti, Ipnotici) non verificata nel dettaglio.

**Suggerimento GDD:** Se le fazioni sono già in codice, aggiungere riferimento ai file/sistemi; altrimenti marcare "Fazioni e reputazione: WIP (Sezione 11)."

---

## 8. Toast HUD (Feedback & Comunicazione)

| GDD | Codice |
|-----|--------|
| Info · Successo · Avviso/Pericolo · **Narrativo** · **Tutorial** | `ToastNotificationType`: Success, ActionSuccess, Info, Warning, Error, Critical + sottotipi (StageUp, ConditionImproved, ItemCollected, ecc.). Nessun tipo "Narrativo" o "Tutorial" |

**Discrepanza minore:** "Tutorial" nel codice è gestito come `ShowTutorial()` nei minigiochi (contestuale), non come tipo toast. "Narrativo" non ha un tipo toast dedicato.

**Suggerimento GDD:** Allineare il glossario: "Tutorial = aiuti contestuali nei minigiochi"; "Narrativo = messaggi diegetici (es. toast Info o tipo dedicato da definire)."

---

## 9. Sotto il cofano — Addressables e telemetria

| GDD | Codice |
|-----|--------|
| ScriptableObjects · nessuna allocazione per frame nei minigiochi · Pooling · Scene additive · **Addressables** · **Smoke test e telemetria** | ScriptableObjects e pooling (Toast, HUD) presenti. **Addressables:** nessun riferimento trovato. Smoke test/telemetria non verificati |

**Discrepanza:** GDD cita Addressables e smoke test/telemetria; in repo non risulta uso di Addressables né sistemi di smoke test/telemetria.

**Suggerimento GDD:** Aggiornare a "Addressables: previsto / da introdurre per contenuti pesanti" e "Smoke test/telemetria: previsti per sistemi critici" oppure rimuovere se non più in scope.

---

## 10. Player Hydration System (Sezione 13)

| GDD | Codice |
|-----|--------|
| Sezione 13 — Player Hydration System | `PlayerStatusPanelController`: placeholder "per integrazione con PlayerHydrationSystem", "quando PlayerHydrationSystem sarà disponibile"; evento `PlayerHydrationChangedEvent` |

**Discrepanza:** Il sistema di idratazione del giocatore non è implementato; solo UI e evento preparati.

**Suggerimento GDD:** In Sezione 13 indicare "Stato: non implementato; UI e eventi predisposti in `PlayerStatusPanelController`."

---

## 11. Condensazione e WAT-RAW

| GDD | Codice |
|-----|--------|
| GDD 42 non menziona esplicitamente la condensazione nel corpo principale; sottopagine (Sistema Condensazione, AZ-01) sì | `CondensationSystem`: accumulo %, raccolta, WAT-RAW, integrazione con Mold (es. 100% → rischio infestazione) |

**Suggerimento GDD:** Nel GDD 42, nella sezione Dome 2.0 o Ambienti, aggiungere un breve capoverso sul sistema di condensazione (raccolta WAT-RAW, accumulo, cap, effetto su Mold) con rimando alla Sezione/AZ-01.

---

## Riepilogo azioni suggerite per l’aggiornamento del GDD

1. **Dome:** Chiarire 4 attivi + 3 passivi (target vs attuale max 10) e/o marcare come da implementare.
2. **Stadi:** Unificare "Crescita" / "Vegetative" nel glossario UI.
3. **Laboratorio:** Aggiungere mappa Step 1–4 ↔ componenti repo (stato implementazione).
4. **Ciclo Notturno:** Separare Diario per vaso (OK) e Diario SPORAE narrativo (WIP/da implementare).
5. **Toast:** Chiarire Tutorial (contestuale) e Narrativo (tipo toast o Info).
6. **Sotto il cofano:** Addressables e smoke test/telemetria come "previsti" o da rimuovere.
7. **Sezione 13:** Marcare Player Hydration come non implementato (UI pronta).
8. **Condensazione:** Citare sistema WAT-RAW e condensazione nel GDD 42 con link a AZ-01.

---

*Report generato dal confronto tra GDD 42 v.31/01/2026 (Notion) e codice MAIN.*
