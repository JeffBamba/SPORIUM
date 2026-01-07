## Linking Toast — Piano di Implementazione (fase successiva)

Questo documento è la **fase 2** dopo l’audit/matrice/proposte. Non cambia codice adesso: serve come checklist e roadmap per quando passeremo all’implementazione.

### P0 (impatto alto, sorgenti dati già presenti)
1. **ACT-050 (low actions)**
   - Target: `TopBarController.OnActionsChanged` + dedup/isteresi.
   - Verifica: scendere a 1 azione → toast; risalire ≥2 → riarm.
2. **CRY-777 (delta CRY)**
   - Target: `TopBarController.OnCRYChanged` calcolo delta + ignore first update.
   - Verifica: compra/vendi → toast con delta corretto.
3. **SYS-001 (Wiki opened)**
   - Target: `WikipediaToggle.ToggleWikipedia` quando chiama `Show()`.
   - Verifica: apri/chiudi wiki → 1 toast, cooldown ok.
4. **SYS-003 (save ok)**
   - Target: `EndDayButton.EndDay` e/o `AppRoot` (autosave) con policy anti-spam.
   - Verifica: save success → toast; save fail → nessun toast SYS-003.
5. **SYS-999 (quit)**
   - Target: `AppRoot.QuitApplication` e `MainMenuOptions.HandleQuit`.
   - Verifica: quit → toast e poi uscita.
6. **INV-000 (inventory open)**
   - Target: `HUDInventory.Show()`.
   - Verifica: open inv → toast.
7. **SPORAE-001 (diary open)**
   - Target: `DiaryUI.Show()`.
   - Verifica: end day → show diary → toast.
8. **LAB-001 (lab accessed)**
   - Target: `LabMicroscope.Show()`, `LabPipette.Show()`, `LabCatalizzatore.Show()`, `LabMinigameExtractor` (punto open).
   - Verifica: apri un pannello lab → toast.
### P1 (richiede piccoli adattamenti dati o decisioni di design)
1. **INV-SPR (spore amount)**
   - Serve esporre quantità `amount` dal reward extractor.
2. **LAB-MIC / LAB-PIP (loaded with {sporeCode})**
   - Serve decidere come rappresentare `sporeCode` (typeId specifico o metadata).
3. **SYS-100 (condensation optimal)**
   - Serve definire la regola “optimal” e individuare il sistema sorgente (condensation metric/event).
4. **POT-* alternativi (POT-W01/POT-F01, POT-PLANT-OK ecc.)**
   - Decisione: mantenere schema passthrough `{message}` o passare allo schema “structured payload”.
### P2 (stub: richiede sistemi non presenti o non agganciati)
1. **SYS-002 (settings opened)**: manca un controller settings chiaro.
2. **REP-CHANGE**: manca un sistema reputazione visibile.
3. **RES-001 / RES-UNLOCK / WIKI-UNLOCK**: manca sistema research/unlock (oggi UI è placeholder).
4. **LAB-INC-* / LAB-GRF-***: manca logica incubazione/graft finale.
5. **HYD-001**: hydration è ancora mock; serve sistema reale + call-site.
### Checklist di verifica (per ogni nuovo trigger)
- Il trigger non emette al boot (se non desiderato), e rispetta warmup/lore policy.
- Nessuno spam: cooldown/dedup funziona.
- Colore severità corretto (Info/Success/Warning/Danger).
- Nessun doppio trigger tra legacy e foundation (feature flag / coexistence).
- Test manuale riproducibile (step minimi) documentato.


