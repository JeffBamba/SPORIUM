# Lab UIToolkit Panels (Foundation UI)

Pannelli Lab in UI Toolkit per Estrazione, Catalizzatore, Fusione e Incubatore.

## Scene setup

1. Per ogni macchinario (Extractor, Catalizzatore, Pipette, Incubator):
   - Crea un GameObject con componente **UIDocument** e il rispettivo **Lab*PanelController** (es. `LabExtractorPanelController`).
   - Nel UIDocument: assegna **Source Asset** al file `.uxml` del pannello (es. `LabExtractorPanel.uxml`).
   - Nel controller: assegna **Extractor** / **Catalizzatore** / **Pipette** (e sul GameObject dell’Incubator assegna nulla se non usa storage).

2. Sui GameObject dei macchinari che hanno **Interactable**:
   - **Extractor**: nel componente `Extractor`, assegna **Lab Extractor Panel** al nuovo `LabExtractorPanelController` (opzionale: lascia **Lab Mini Game** per fallback legacy).
   - **Catalizzatore**: assegna **Lab Catalizzatore Panel** al `LabCatalizzatorePanelController`.
   - **Pipette**: assegna **Lab Fusion Panel** al `LabFusionPanelController`.
   - **Incubator**: aggiungi il componente `Incubator` (script in `Interactables/Incubator.cs`) e assegna **Lab Incubator Panel** al `LabIncubatorPanelController`.

3. I pannelli usano **Foundation Notification** (toast) per esito operazioni (SPORE-001, INV-SPR, LAB-GRF-OK, LAB-INC-OK).
