---
name: CRT Bunker Effect
overview: Aggiunge un effetto CRT bunker (vignetta, ombre laterali, scanlines animate, tinta verde) applicato solo al world/gameplay via URP Fullscreen Pass, escludendo HUD/UI.
todos:
  - id: scan-existing-rendering
    content: Individuare la camera di gameplay nelle scene principali e verificare se c’è camera stacking (Base/Overlay) per impostare correttamente i filtri del renderer feature.
    dependencies: []
  - id: crt-shader
    content: Creare shader full-screen URP (usa _BlitTexture) con vignetta + ombre laterali + scanlines animate + tinta verde (+ noise opzionale).
    dependencies: []
  - id: renderer-feature
    content: Implementare ScriptableRendererFeature wrapper che esegue il full-screen pass solo su Base camera e (opzionalmente) solo su tag MainCamera.
    dependencies:
      - crt-shader
  - id: material-and-hookup
    content: Creare materiale e aggiungerlo al renderer feature su URP_2DRenderer; impostare injection point e default parametri.
    dependencies:
      - renderer-feature
  - id: playmode-verify
    content: Verificare in Play Mode che world abbia l’effetto e HUD/UI sia esclusa; aggiustare parametri default per resa ‘bunker’ coerente.
    dependencies:
      - material-and-hookup
---

# Effetto CRT “Bunker” (solo world, HUD esclusa)

## Obiettivo
- Applicare un overlay CRT (scanlines animate + vignetta + ombre laterali + tinta verde) **solo al rendering del gameplay/world**.
- **HUD/UI esclusa**: l’effetto non deve toccare `Canvas`, `UIDocument` e menu UI.

## Scelta tecnica (URP 14)
- Useremo **URP Fullscreen Pass** tramite `FullScreenPassRendererFeature` (presente in URP 14.0.12) per applicare un materiale full-screen.
- Compatibilità con **Bloom via Camera Stack (Overlay)**: per far sì che il CRT colpisca anche il glow Bloom, l’effetto verrà eseguito da una **Overlay camera finale** dedicata (es. `CRT_PostFX_Camera`) posta **in fondo allo stack**.\n+  - In questo modo il CRT lavora sul frame già compositato (Base + Bloom Overlay) ma la HUD resta fuori (tipicamente Screen Space Overlay).\n+- Per evitare di “sporcare” menu/camere non gameplay, manterremo un filtro semplice (tag e/o presenza nello stack della Main Camera di gameplay).

## File/asset coinvolti
- URP:
  - `Assets/_Settings/URP/URP_Asset.asset` (già punta a `URP_2DRenderer`)
  - `Assets/_Settings/URP/URP_2DRenderer.asset` (oggi `m_RendererFeatures: []`)
- Nuovi (da creare):
  - `Assets/_Project/Scripts/Rendering/SporaeCrtBunkerRendererFeature.cs`
  - `Assets/_Project/Rendering/Shaders/SP_CRT_BunkerFullscreen.shader`
  - `Assets/_Project/Rendering/Materials/MAT_SP_CRT_BunkerFullscreen.mat`

## Implementazione (alto livello)
1. **Shader full-screen** `Hidden/SP_CRT_BunkerFullscreen` che legge `_BlitTexture` e applica:
   - **Scanlines**: pattern orizzontale + scorrimento verticale leggero (param: intensità, densità, speed)
   - **Vignetta**: scurisce bordi con falloff morbido (param: intensity, power)
   - **Ombre laterali**: left/right quasi neri (param: width, intensity)
   - **Tinta/“fosforo verde”**: grading semplice verso verde (param: amount + colore)
   - (Opzionale) **noise leggero** per “pellicola” (param: amount)

2. **Materiale** `MAT_SP_CRT_BunkerFullscreen.mat` con i parametri esposti per tuning rapido.

3. **RendererFeature wrapper** `SporaeCrtBunkerRendererFeature`:
   - Internamente usa la logica del `FullScreenPassRendererFeature` (copy color + draw full screen)
   - Condizioni di esecuzione:
     - `cameraData.renderType == Overlay` (per la sola `CRT_PostFX_Camera`)
     - (Opzionale) `camera.name == "CRT_PostFX_Camera"` o `camera.CompareTag(requiredTag)` per mantenere l’effetto confinato al gameplay.

4. **Aggancio al renderer**: aggiungere la feature al `URP_2DRenderer.asset` e assegnare il materiale.\n+   - La feature verrà eseguita solo dalla camera `CRT_PostFX_Camera` (Overlay finale nello stack).
   - Injection point consigliato: **AfterRenderingPostProcessing**
   - `fetchColorBuffer = true`

## Operazioni manuali in Unity (step-by-step, beginner)
1. Apri il progetto in Unity.
2. Vai in `Project` → apri `Assets/_Settings/URP/URP_2DRenderer.asset`.
3. In Inspector, sezione **Renderer Features** → `Add Renderer Feature` → seleziona **SporaeCrtBunkerRendererFeature**.
4. Nel renderer feature appena aggiunto:
   - Assegna `MAT_SP_CRT_BunkerFullscreen.mat`.
   - Imposta `Injection Point` su `After Rendering Post Processing`.
   - Lascia `Fetch Color Buffer` attivo.
   - Se presente, imposta filtro camera su nome/tag della **CRT_PostFX_Camera**.\n+5. In scena gameplay (es. `SCN_Bootstrap`), crea una camera `CRT_PostFX_Camera`:\n+   - `Render Type = Overlay`\n+   - `Culling Mask = Nothing`\n+   - `Post Processing = ON`\n+   - Aggiungila **in fondo** al `Camera Stack` della Main Camera di gameplay (dopo Bloom Camera se esiste).\n+6. Entra in Play Mode in una scena gameplay.\n+7. Verifica che:
   - il world (player, ambiente) abbia l’effetto CRT
   - la HUD/UI (Canvas, UIDocument) **NON** sia alterata
8. Tuning rapido:
   - aumenta/diminuisci `ScanlinesIntensity`
   - regola `SideShadowWidth`/`SideShadowIntensity` per lati più “incassati”
   - regola `VignetteIntensity`/`VignettePower` per focalizzare al centro
   - regola `GreenAmount` per la resa fosforo

## Note e rischi
- Se il world usa **camera stacking** (Base + Bloom Overlay), la camera `CRT_PostFX_Camera` finale garantisce che il CRT colpisca **anche** il glow.\n+- Se una parte di UI venisse renderizzata via camera (Screen Space Camera/World Space), andrà esclusa da stacking o resa overlay per non essere filtrata.

## Criteri di completamento
- Effetto visibile solo su world/gameplay.
- HUD/UI invariata.
- Parametri facilmente regolabili da Inspector sul materiale.
