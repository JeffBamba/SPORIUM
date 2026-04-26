# DEV REPORT 0096 — Localizzazione Mission Recap, header Notifiche Foundation, Food Room, Dispensa, Lab Terminal, Seed Storage

**Data:** 2026-04-26  
**Sprint / contesto:** Parità IT/EN su HUD e pannelli cucina/lab/storage; refresh su `GameLanguageSettings.OnLanguageChanged`.  
**Riferimento piano:** `.cursor/plans/demo_alpha_1_0_gap_map.plan.md` (Principio 0 — un solo prodotto)  
**Report precedente:** `DEV_REPORT_0095_LOCALIZZAZIONE_OPZIONI_UITK_INVENTARIO_2026-04-26.md`

---

## Sommario interventi

1. **Mission Recap** (`ActiveMissionsPanelController` + `ActiveMissions.uxml`): chrome, filtri, empty state, card timer, tooltip (sezioni, scadenza, fazioni, ricompensa CRY), toast e VO missione → chiavi `missions.*` + sottoscrizione lingua e refresh tooltip se aperto.
2. **Notifiche Foundation**: titolo header `nf-header-title` da `notifications.title` + cambio lingua.
3. **Synth food** (`FoodRoomPanelController`): `ApplyLocalizedFoodRoomStaticChrome()` per tutto il copy statico UXML (`food_room.chrome.*`) + refresh su cambio lingua prima di `Refresh()`.
4. **Dispensa** (`DispensaPanelController`): `ApplyLocalizedDispensaStaticChrome()` (`dispensa.chrome.*`, nomi inventario da `dispensa.food.*`) a bind, Show e cambio lingua.
5. **Terminale laboratorio** (`LabTerminalPanelController`): `ApplyLabTerminalStaticChrome()` (`lab_terminal.chrome.*` + pulsanti analisi/step dove previsto) + `OnLanguageChanged` con `RefreshDisplay()` se pannello aperto.
6. **Seed Storage** (`SeedStoragePanelController`, `SeedStoragePanel.uxml`): etichette statiche, log di sistema, errori deposito/prelievo, pulsanti e vitalità → `seed_storage.chrome.*` / `log.*` / `err.*`; `name` su UXML per binding stabile.
7. **`LocalizationManager`**: nuove chiavi per le aree sopra.

---

## Statistiche e progresso

### Righe di codice

- **Churn sui file toccati:** **N/D** in questa iterazione (snapshot locale: `git diff --numstat` sui path elencati non ha restituito righe; per un conteggio preciso rieseguire dopo `git add` o su branch con modifiche versionate).
- **Riferimento dimensione file (solo indicativo, non churn):** `LocalizationManager.cs` ~792 righe; `LabTerminalPanelController.cs` ~1783; `ActiveMissionsPanelController.cs` ~964; `FoodRoomPanelController.cs` ~600; `SeedStoragePanelController.cs` ~652; `DispensaPanelController.cs` ~409; `FoundationNotificationsPanelController.cs` ~407 (comando `Measure-Object` su working copy).

### Sistemi funzionanti

- **Compilazione / Play test:** non rieseguiti in questa sessione dopo l’ultimo blocco di patch — **da validare in Editor** (cambio lingua IT↔EN con Mission Recap, Foundation, Food Room, Dispensa, Lab Terminal, Seed Storage aperti).

### Bug risolti

- **0** (nessuno documentato come issue chiusa in questo report).

### Progresso gameplay / prodotto

- Il giocatore vede **testi coerenti con la lingua** su più superfici HUD/pannello che prima erano solo IT o solo EN in base al placeholder UXML o al codice.
- **Cambio lingua a caldo** aggiorna Mission Recap (incluso tooltip se visibile), header notifiche, chrome statico Food/Dispensa/Lab/Seed dove implementato.
- **Nota:** titoli/descrizioni missione provenienti da **ScriptableObject / `MissionConfig`** restano nella lingua degli asset finché non si introduce localizzazione dati dedicata.

---

## 1. Mission Recap e notifiche

**Problema** — Chrome e messaggi missione legati a stringhe fisse o miste; header notifiche non agganciato a `LocalizationManager`.

**Soluzione** — Chiavi `missions.*`; Foundation usa `notifications.title` sul label header.

| File | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/UI/UIToolkit/HUD/ActiveMissionsPanelController.cs` | Logica + lingua |
| `Assets/_Project/Resources/UI/UIToolkit/ActiveMissions/ActiveMissions.uxml` | Copy placeholder empty state |
| `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/FoundationNotificationsPanelController.cs` | Titolo header + evento lingua |

---

## 2. Food Room, Dispensa, Lab Terminal, Seed Storage

**Problema** — Copy statico principalmente in UXML (IT) o misto rispetto al runtime localizzato.

**Soluzione** — Metodi `ApplyLocalized*StaticChrome` / `ApplyLabTerminalStaticChrome` + chiavi `food_room.chrome.*`, `dispensa.chrome.*`, `lab_terminal.chrome.*`, `seed_storage.*`; UXML Seed Storage con `name` dove serve.

| File | Tipo modifica |
|------|----------------|
| `Assets/_Project/Scripts/UI/UIToolkit/FoodRoom/FoodRoomPanelController.cs` | Chrome statico + lingua |
| `Assets/_Project/Scripts/UI/UIToolkit/DispensaRefrigerata/DispensaPanelController.cs` | Chrome statico + lingua |
| `Assets/_Project/Scripts/UI/UIToolkit/Lab/LabTerminalPanelController.cs` | Chrome statico + lingua |
| `Assets/_Project/Scripts/UI/UIToolkit/SeedStorage/SeedStoragePanelController.cs` | Chrome, log, errori + lingua |
| `Assets/_Project/Resources/UI/UIToolkit/SeedStorage/SeedStoragePanel.uxml` | `name` etichette |
| `Assets/_Project/Scripts/Core/Localization/LocalizationManager.cs` | Nuove chiavi |

---

## Regole / vincoli

- Nessun nuovo `FindObjectOfType` in gameplay; uso di `LocalizationManager` / `GameLanguageSettings` coerente con l’architettura esistente.

## Note operative

- Verifica manuale consigliata: **Opzioni → lingua** con pannelli sopra aperti e chiusi; log Seed Storage dopo cambio lingua.
- Per **testi missione** da asset: backlog contenuti o tabella id → chiavi `missions.content.*` se si decide di estendere.

---

*Fine DEV REPORT 0096.*
