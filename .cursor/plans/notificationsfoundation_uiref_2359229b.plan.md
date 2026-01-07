---
name: NotificationsFoundation_UIRef
overview: "PLAN DEFINITIVO — Nuovo sistema Notifications ex novo (UI Toolkit Foundation) allineato ai reference UI: stack unico max 3 con DANGER persistenti pinnati che occupano slot; toast temporanei sotto; lore/ambient ibrido preemptabile; debug console runtime IMGUI."
todos:
  - id: typespec-registry
    content: Definire TypeSpecRegistry (39 tipologie + lore topics + watcher codes), con codici univoci, category, severity default, LocKey IT/EN e schema payload
    status: completed
  - id: notification-service
    content: Implementare FoundationNotificationService (dedup/cooldown/rate-limit/preemption/stagger) e API unica (toast/item/danger)
    status: completed
    dependencies:
      - typespec-registry
  - id: notifications-uitk-ui
    content: Creare UI Toolkit Notifications (header + lista unica max3) che replica i reference estetici/posizione e usa SP-Foundation.uss + SP-Panel-Base.uss + classi sp-*
    status: completed
    dependencies:
      - notification-service
  - id: danger-watchers
    content: Implementare watchers state-driven (pH ULTRA ±80, overwatering, light stress 100%, fertilizer missing/out-of-range, mold risk/infested, countdown pH per pot) con dedup per key e auto-resolve
    status: completed
    dependencies:
      - notification-service
  - id: lore-scheduler
    content: Implementare scheduler lore/ambient (ibrido) con gating/cooldown e gameplay-preempt (lore ritardate/scartate in presenza di alert utili)
    status: completed
    dependencies:
      - notification-service
      - typespec-registry
  - id: debug-console
    content: Implementare debug console IMGUI runtime (dev-only, session-only, hotkey configurabile) per push/edit/tuning e viewer stato
    status: completed
    dependencies:
      - notification-service
  - id: coexistence-flag
    content: Aggiungere feature flag (dev) per attivare/disattivare il nuovo sistema mantenendo i legacy intatti durante la migrazione
    status: completed
    dependencies:
      - notifications-uitk-ui
      - danger-watchers
      - debug-console
  - id: migrate-call-sites
    content: Migrare call-site gradualmente (Pot/Lab/TopBar/Inventory/Research/Market) verso la nuova API, verificando ogni step in scena
    status: completed
    dependencies:
      - coexistence-flag
  - id: deprecate-legacy
    content: Deprecare/disabilitare i sistemi legacy (ToastNotificationManager, HUDNotifications2.0, UINotification) solo dopo migrazione completa e validazione
    status: completed
    dependencies:
      - migrate-call-sites
---

## Notifications Foundation (unico sistema) — UI come reference (PLAN DEFINITIVO)

### Obiettivo

Creare un **nuovo sistema di Notifications ex novo** basato su **UI Toolkit Foundation** che diventi l’unico sistema di notifiche in game, mantenendo una fase di **coexistence** per non rompere nulla durante la migrazione.

### Risultato UI (vincolante: reference immagini)

- **Posizione**: Top-Right, sotto TopBar, larghezza fissa ~306px, offset coerenti.
- **Header sempre visibile** con badge count e chevron:
- **Compatto**: solo header.
- **Espanso**: header + lista.
- **Lista unica** max **3 righe visibili** (come screenshot).
- **Header color** = severità massima tra le notifiche attive/visibili (DANGER > WARNING > INFO/SUCCESS > idle).
- **Ordering**: DANGER in alto (pinned), sotto WARNING/INFO/SUCCESS.
- **Overflow**: se DANGER >= 3, la UI mostra solo DANGER; le temporanee vanno in coda/log e non occupano slot visibili finché non si libera spazio.

### Requisiti funzionali (confermati)

- **Sistema unico**: un solo “entry point” (service) e una sola UI.
- **Tipologie**: set esteso (39+), non solo “quando serve”, ma finestra viva sul Vault (info + lore + sistemi).
- **Codici univoci per tipologia**: es. `LAB-INC-OK` vs `LAB-INC-FAIL`.
- **Localizzazione**: notifiche basate su **LocKey + parametri** (IT/EN minimo).
- **Lore/ambient**: generazione ibrida (event-driven + scheduler), ma **gameplay-preempt** su lore.
- **Debug console** runtime: push e tuning (cooldown/rate limit/preempt/stagger), dev-only, session-only, hotkey configurabile.

### Regole DANGER (state-driven, persistenti)

- **Persistenti state-driven**: rimossi solo quando la condizione rientra.
- **Dedup per causa**: una sola notifica per key (aggiornabile).
- **Stacking**: possono esistere più DANGER contemporaneamente.
- **pH ULTRA**: basato su `PhSystem.EvaluateState()` (soglie ±80 già presenti in codice).
- **Esempi watcher richiesti**:
- pH ULTRA (acido/basico)
- Overwatering
- Light stress 100%
- Fertilizzante mancante / fuori range (crescita bloccata)
- Mold risk/infested
- Countdown pH estremo per pianta (dove applicabile)

### Architettura proposta (alto livello)

**1) TypeSpecRegistry**

- Definisce: `Code`, `Category`, `DefaultSeverity`, `LocKey`, `Cooldown`, `DedupPolicy`, `PayloadSchema`.

**2) FoundationNotificationService (single source of truth)**

- API unica:
- `PostToast(code, payload, overrides?)`
- `PostItem(code, itemPayload)` (layout item)
- `UpsertDanger(key, code, payload)`
- `ResolveDanger(key)` (invocato dal layer watcher quando rientra)
- Pipeline:
- dedup (per key)
- cooldown/rate limit
- priority + gameplay-preempt lore
- stagger opzionale (per burst)

**3) Watchers (state-driven)**

- Hook su stati reali (es. `PhSystem.OnPhChanged`, e polling/subscribe su PotStateModel tramite eventi già presenti).

**4) UI Toolkit Foundation**

- UXML/USS nuovi, con import `SP-Foundation.uss` + `SP-Panel-Base.uss` e classi `sp-*` [[memory:13017660]].
- Layout che replica gli screenshot (compatto/espanso).

**5) Debug Console (IMGUI)**

- Dev-only; session-only; hotkey configurabile runtime.
- Sezioni: push toast/danger, tuning cooldown/rate-limit/preempt/stagger, viewer attivi e history.

### Ex novo senza rompere nulla (strategia di rollout)

- **Coexistence**: il nuovo sistema vive in cartelle/namespace dedicati e non modifica i legacy in prima battuta.
- **Feature flag (dev)**: abilita/disabilita il nuovo sistema per scene/test.
- **Migrazione graduale**: portare i call-site uno a uno.
- **Deprecazione finale**: disabilitare/rimuovere legacy solo dopo validazione completa.

### File/Cartelle (previste)

- Nuovi (Foundation Notifications):
- `Assets/_Project/Scripts/UI/UIToolkit/NotificationsFoundation/` (service, specs, watchers, scheduler, debug console)
- `Assets/_Project/UI/UIToolkit/NotificationsFoundation/` (UXML/USS)
- Legacy (da deprecare a fine migrazione):
- `Assets/_Project/Scripts/DevTools/Notification/*`
- `Assets/_Project/Scripts/UI/HUDNotifications2.0/*`
- `Assets/_Project/Scripts/UI/VaultMap/UINotification.cs`

### Operazioni manuali (Unity) — step-by-step

1. Apri Unity Editor e la scena principale (es. VaultMap).
2. Aggiungi il nuovo UI Document Notifications Foundation (senza rimuovere i sistemi legacy).
3. Abilita il **feature flag dev** “UseFoundationNotifications”.
4. Entra in Play Mode.
5. Apri la **debug console** (hotkey configurabile) e:

- Pusha 2 INFO + 1 WARNING e verifica che l’header diventi giallo.
- Upsert 1–3 DANGER e verifica che siano pinnati in alto e occupino slot.
- Verifica compatto/espanso (chevron).
- Modifica cooldown/rate limit e verifica comportamento.

6. Migra un singolo call-site (es. PotNotifications) alla nuova API e verifica che il gameplay non cambi.