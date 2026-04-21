# DEV REPORT 0090 — Dispensa Refrigerata separata da FoodRoom: UI dedicata, wiring scena e fix regressioni input/interazioni

**Data:** 2026-04-21  
**Sprint / contesto:** Iterazione Kitchen UX + integrazione macchina Dispensa Refrigerata con runtime esistente (`FoodRoomSystem`) senza introdurre nuovi framework core.  
**Riferimento piano:** Task operativo da chat utente (nessun nuovo piano `.cursor/plans` formalizzato in questa iterazione).  
**Report precedente:** `DEV_REPORT_0089_DEMO_BEAT2_VO_TOAST_IDRATAZIONE_GIORNO1_2026-04-20.md`

---

## Sommario interventi

1. Ripristinato `FoodRoomPanel` allo stato pre-task (rimozione sezione pantry da UXML/USS/controller).
2. Introdotto pannello **dedicato** Dispensa (`DispensaPanel.uxml/.uss`) con layout cyber coerente al mockup richiesto.
3. Creati controller/opener dedicati (`DispensaPanelController`, `DispensaRefrigerataOpener`) con riuso API runtime pantry già presenti in `FoodRoomSystem`.
4. Aggiornata la scena `SCN_VaultMap` con oggetto `DispensaPanel` e wiring macchina `DispensaRefrigerata` -> pannello dedicato.
5. Risolti bug regressivi emersi in test utente:
   - blocco input globale (panel root always-on)
   - doppia apertura panel Dispensa + FoodSynthMachine per wiring residuo/duplicato.

---

## 1. Separazione UI: FoodRoom vs Dispensa

### Problema
L’interfaccia Dispensa era stata inglobata nel `FoodRoomPanel`, creando duplicazione funzionale e violando la richiesta di separazione netta tra macchinari.

### Soluzione
- **Ripristino FoodRoom pre-task**:
  - rimosso blocco `pantry-section` da `FoodRoomPanel.uxml`
  - rimosse classi `.pantry-*` da `FoodRoomPanel.uss`
  - rimossa logica pantry da `FoodRoomPanelController`
- **Creazione UI dedicata Dispensa**:
  - nuovo `DispensaPanel.uxml`
  - nuovo `DispensaPanel.uss` (poi rifinito in pass visuale successivo)

**Effetto:** `FoodSynthMachine` torna a gestire solo il proprio panel storico, `DispensaRefrigerata` ha UI autonoma.

---

## 2. Runtime Dispensa con sistemi esistenti

### Problema
Serviva una macchina separata ma con logica gameplay allineata al piano: transfer item, ON/OFF, costo giornaliero, preservazione qualità e toast, senza creare un sistema core parallelo.

### Soluzione
`DispensaPanelController` usa direttamente `FoodRoomSystem` esistente:
- `SetPantryPower(...)`
- `TryTransferToPantry(...)`
- `TryTransferFromPantry(...)`
- `GetPantryQuantity(...)`
- `PantryIsOn`, `PantryDailyCost`

Inoltre:
- click righe inventory -> inserimento 1 unità nel bucket corretto
- click card camera -> estrazione 1 unità
- refresh dinamico di qty/stato/costo/log

**Nota:** qualità item e toast restano delegati all’implementazione pantry in `FoodRoomSystem` già introdotta.

---

## 3. Wiring scena Unity

### Problema
Dopo i cambi strutturali era necessario collegare correttamente GameObject, `UIDocument` e opener in scena.

### Soluzione
In `SCN_VaultMap.unity`:
- aggiunto GO `DispensaPanel` con:
  - `UIDocument` (`sourceAsset` = `DispensaPanel.uxml` guid `3223efe86dd24444b9ccb779db1079c5`)
  - `DispensaPanelController`
- su GO `DispensaRefrigerata`:
  - sostituito `FoodSynthMachine` con `DispensaRefrigerataOpener`
  - assegnato `_dispensaPanel` al controller corretto
- mantenuto GO `FoodSynthMachine` originale con opener/panel legacy.

---

## 4. Regressioni emerse e fix

### 4.1 Input globale bloccato (nessun panel apribile con E)

**Problema**  
Il root della UI Dispensa rimaneva fullscreen attivo e intercettava input anche quando il panel era chiuso.

**Soluzione**
- `.dispensa-root` impostato `display: none` di default in USS
- `DispensaPanelController` aggiornato per mostrare/nascondere il root corretto (`dispensa-root`) e overlay interno in `Show()/Hide()`

### 4.2 Apertura doppia panel su interazione Dispensa

**Problema**  
`DispensaRefrigerata` apriva sia panel Dispensa sia panel FoodRoom per wiring errato residuo in scena (script/componenti duplicati).

**Soluzione**
- ripristinato `FoodSynthMachine` sul GO macchina originale
- mantenuto `DispensaRefrigerataOpener` solo su `DispensaRefrigerata`
- rimosso componente opener duplicato residuo dal GO Dispensa
- verificato mapping campi:
  - `_foodRoomPanel` sul GO FoodSynth
  - `_dispensaPanel` sul GO Dispensa

---

## 5. File modificati (tabella)

| Path | Tipo modifica |
|------|----------------|
| `Assets/_Project/UI/UIToolkit/FoodRoom/FoodRoomPanel.uxml` | Ripristino pre-task (rimozione sezione pantry) |
| `Assets/_Project/UI/UIToolkit/FoodRoom/FoodRoomPanel.uss` | Ripristino pre-task (rimozione classi pantry) |
| `Assets/_Project/Scripts/UI/UIToolkit/FoodRoom/FoodRoomPanelController.cs` | Ripristino controller pre-task (rimozione logica pantry) |
| `Assets/_Project/UI/UIToolkit/DispensaRefrigerata/DispensaPanel.uxml` | Nuovo pannello UI dedicato Dispensa |
| `Assets/_Project/UI/UIToolkit/DispensaRefrigerata/DispensaPanel.uss` | Stili dedicati + rifinitura visuale |
| `Assets/_Project/Scripts/UI/UIToolkit/DispensaRefrigerata/DispensaPanelController.cs` | Nuovo controller pannello Dispensa |
| `Assets/_Project/Scripts/Interactables/DispensaRefrigerataOpener.cs` | Nuovo opener interazione Dispensa |
| `Assets/_Project/Scenes/SCN_VaultMap.unity` | Wiring scena: GO panel, opener, riferimenti, fix regressioni |

---

## 6. Regole / vincoli rispettati

- **Nessun framework core nuovo**: estensione su runtime pantry già presente in `FoodRoomSystem`.
- **Separazione chiara per macchinario**: `FoodSynthMachine` e `DispensaRefrigerata` con opener/panel distinti.
- **UI Toolkit dedicato**: UXML/USS separati per Dispensa, senza duplicazione strutturale dentro `FoodRoomPanel`.
- **Compatibilità save/runtime**: mantenuta integrazione con stato pantry ON/OFF e contenuti già serializzati.

---

## 7. Note operative (Unity)

- Aprire `SCN_VaultMap.unity`.
- In `ROOM_Kitchen` devono risultare:
  - `FoodSynthMachine` -> `FoodSynthMachine` script -> `_foodRoomPanel`
  - `DispensaRefrigerata` -> `DispensaRefrigerataOpener` -> `_dispensaPanel`
  - `DispensaPanel` con `UIDocument` su `DispensaPanel.uxml`.
- Se l’editor mostra stato vecchio: `Reimport` di `SCN_VaultMap.unity`, `DispensaPanel.uxml`, `DispensaPanel.uss`.

---

*Fine DEV REPORT 0090.*
