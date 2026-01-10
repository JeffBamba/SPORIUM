# VAULTMAP — Player Shadows (URP 2D) Setup

Obiettivo: quando il **player** entra in una stanza del VaultMap, viene illuminato da luci 2D “di stanza” e **casta shadow sul pavimento**, migliorando blending e immersione.

Questo progetto usa già **URP + 2D Renderer** (URP 14) e già usa `Light2D` in vari punti.

---

## 0) Prerequisiti (una volta sola)

1. Apri `Project Settings > Graphics`
2. Verifica che **Scriptable Render Pipeline Settings** punti a `Assets/_Settings/URP/URP_Asset.asset`
3. Apri `URP_Asset` e verifica che il Renderer sia il **2D Renderer** (in questo repo è `Assets/_Settings/URP/URP_2DRenderer.asset`)

---

## 1) Player: abilita lo “shadow caster”

### Opzione A (consigliata): auto-setup via script (non distruttivo)

1. Seleziona il **GameObject** `PLY_Player` (o il prefab `Assets/_Project/Prefabs/Character/PLY_Player.prefab`)
2. Aggiungi lo **script** `PlayerShadowCaster2DAutoSetup`:
   - Path: `Assets/_Project/Scripts/Player/PlayerShadowCaster2DAutoSetup.cs`
3. Premi Play: lo script cercherà il `SpriteRenderer` del player (child `Sprite`) e garantirà che esista un `ShadowCaster2D` agganciato correttamente.

Note:
- Se non vedi ombre, verifica **step 2 e 3** (luci con shadows ON + pavimento lit receiver).

### Opzione B: manuale (se preferisci controllo totale)

1. Apri prefab `PLY_Player`
2. Seleziona il child `Sprite` (quello che ha `SpriteRenderer`)
3. Aggiungi componente **ShadowCaster2D**
4. (Consigliato) abilita “Use Renderer Silhouette” se presente

---

## 2) Luci di stanza: accensione/spegnimento quando il player entra/esce

Nel repo è disponibile lo **script trigger per-stanza**:
- `Assets/_Project/Scripts/World/VaultMap/RoomLight2DZone.cs`

### Setup (per ogni stanza)

1. Crea un **GameObject** stanza, es: `Room_LightZone_Bedroom`
2. Aggiungi un `BoxCollider2D` (o `PolygonCollider2D`)
   - **Is Trigger = ON**
   - Dimensiona il collider per coprire l’area “camera/stanza”
3. Aggiungi lo script `RoomLight2DZone`
4. Nel campo **Lights** trascina dentro le `Light2D` che vuoi accendere per quella stanza
   - Suggerimento: crea le luci come child della stanza e usa `autoCollectLightsFromChildren = true`
5. Verifica che il player abbia Tag `Player` (in questo repo il prefab `PLY_Player` ha già Tag Player)

Risultato:
- Entra → luci ON
- Esci → luci OFF

---

## 3) Abilitare “shadows” sulle Light2D (fondamentale)

Per ogni `Light2D` che deve generare ombre:
1. Seleziona la luce
2. Nel componente `Light2D`:
   - Abilita **Shadow Intensity** (in molte scene può risultare off di default)
   - Imposta un valore iniziale: **0.6–0.85**

Nota:
- Nel file scena `Assets/_Project/Scenes/SCN_VaultMap.unity` è stato osservato che molte luci hanno `ShadowIntensityEnabled = 0` → quindi ombre non appariranno finché non le attivi.

---

## 4) Pavimento “receiver”: il pavimento deve essere LIT (fondamentale)

Le ombre URP 2D si vedono solo su Sprite/Renderer che partecipano al sistema 2D lighting (materiale **2D Lit**).

Hai due strade:

### Strada A: rendere “lit” il Vault BG (se accettabile artisticamente)
1. Seleziona il **GameObject** del pavimento (es. `VaultMap_BG`)
2. Sul suo `SpriteRenderer`, assegna un materiale **Sprite-Lit** / **2D Lit**

### Strada B: “FloorShadowReceiver” dedicato (non tocca il BG esistente)
1. Crea un nuovo **GameObject** `FloorShadowReceiver` sotto la stanza
2. Metti uno sprite/quad molto grande che copra l’area calpestabile
3. Assegna materiale **2D Lit**
4. Imposta colore/alpha in modo che sia invisibile o quasi (dipende dal look che vuoi)

---

## 5) Checklist rapida (debug)

Se la shadow non si vede:
- Player: esiste un `ShadowCaster2D`? (in Play, controlla sul child `Sprite`)
- Luci: `Light2D` ha **Shadow Intensity enabled**?
- Pavimento: è renderizzato con **materiale 2D Lit**?
- Sorting Layers: la luce e il pavimento stanno su sorting layer compatibili?

---

## File utili (repo)
- Script stanza: `Assets/_Project/Scripts/World/VaultMap/RoomLight2DZone.cs`
- Script player: `Assets/_Project/Scripts/Player/PlayerShadowCaster2DAutoSetup.cs`

