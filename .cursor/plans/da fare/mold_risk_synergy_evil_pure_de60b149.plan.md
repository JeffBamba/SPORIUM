---
name: Mold Risk Synergy EVIL PURE
overview: "Implementare meccaniche di sinergia tra Mold Risk e famiglie di piante: EVIL prospera con muffe (bonus crescita/resa/mutazioni), PURE soffre doppiamente (penalità maggiori), con bonus/penalità aumentati quando anche in pH non ottimale."
todos:
  - id: mold_growth_modifier
    content: Aggiungere GetMoldGrowthModifier() in PhGrowthModifier.cs che calcola modificatore crescita basato su Mold Risk + Famiglia + pH
    status: pending
  - id: mold_yield_modifier
    content: Aggiungere GetMoldYieldModifier() in PhGrowthModifier.cs che calcola modificatore resa basato su Mold Risk + Famiglia + pH
    status: pending
  - id: mold_mutation_bonus
    content: Aggiungere GetMoldMutationBonus() in MoldSystem.cs che calcola bonus mutazioni basato su Mold Risk + Famiglia + pH
    status: pending
  - id: integrate_growth
    content: Integrare modificatore muffe in ResolveGrowthForPot() del DayCycleController
    status: pending
  - id: remove_block_evil
    content: Rimuovere blocco crescita per EVIL da Mold Risk in ResolveGrowthForAllPots()
    status: pending
  - id: integrate_yield
    content: Integrare modificatore muffe nel calcolo resa (DoHarvest o equivalente)
    status: pending
  - id: modify_infestation
    content: "Modificare ApplyInfestation() per considerare famiglia (EVIL: -1 livello, PURE: -5 livelli)"
    status: pending
  - id: update_pure_block
    content: "Modificare blocco crescita per PURE: bloccata a Mold Risk Level ≥1 (più sensibile)"
    status: pending
---

# Piano: Mold Risk Synergy per Famiglie Piante

## Obiettivo

Implementare meccaniche che rendono il Mold Risk strategico per EVIL e penalizzante per PURE, creando differenziazione tra famiglie e strategie di gioco alternative.

**NOTA IMPORTANTE - Integrazione Condensazione:**
Il sistema di condensazione (implementato in `sistema_condensazione_completo_notion_935809cc.plan.md`) interagisce con Mold Risk:
- **Giorni Virtuali**: Condensazione >50% aggiunge giorni virtuali a `DaysOverwateringConsecutive`, accelerando l'accumulo di Mold Risk
  - 50-59%: +0.5 giorni/giorno
  - 60-79%: +1.0 giorni/giorno
  - 80-100%: +1.5 giorni/giorno
- **Accelerazione Infestazione**: Condensazione 100% riduce giorni richiesti per infestazione (2→1→0 giorni)
- Questo significa che **EVIL beneficia indirettamente dalla condensazione alta** (accelera Mold Risk che dà bonus), mentre **PURE soffre doppiamente** (condensazione accelera Mold Risk che dà penalità)

## Meccaniche da Implementare

### 1. Modificatori Crescita basati su Mold Risk + Famiglia + pH

**File**: `Assets/_Project/Scripts/Dome/PotSystem/Growth/PhGrowthModifier.cs`

Aggiungere metodo `GetMoldGrowthModifier()` che calcola modificatore crescita considerando:

- **EVIL con Mold Risk**: 
- Mold Risk Level 1-2: +20% crescita (compensa pH non ottimale)
- Mold Risk Level 3: +30% crescita (quando non bloccata)
- Bonus extra se anche in pH Basico: +10% aggiuntivo
- **PURE con Mold Risk**:
- Mold Risk Level 1-2: -20% crescita (penalità extra)
- Mold Risk Level 3: -30% crescita
- Penalità extra se anche in pH Acido: -10% aggiuntivo
- **Standard**: nessun modificatore (sistema attuale)

**NOTA Condensazione:**
La condensazione accelera l'accumulo di Mold Risk attraverso giorni virtuali, quindi:
- **EVIL**: Condensazione alta → Mold Risk si accumula più velocemente → Bonus crescita attivi prima
- **PURE**: Condensazione alta → Mold Risk si accumula più velocemente → Penalità crescita attive prima

Modificare `GetGrowthMultiplier()` per includere anche il modificatore muffe.

### 2. Modificatori Resa basati su Mold Risk + Famiglia

**File**: `Assets/_Project/Scripts/Dome/PotSystem/Growth/PhGrowthModifier.cs`

Aggiungere metodo `GetMoldYieldModifier()` che calcola modificatore resa:

- **EVIL con Mold Risk**:
- Mold Risk Level 1-2: +20% resa
- Mold Risk Level 3 (infestata): +50% resa
- Bonus extra se anche in pH Basico: +15% aggiuntivo
- **PURE con Mold Risk**:
- Mold Risk Level 1-2: -20% resa
- Mold Risk Level 3: -50% resa
- Penalità extra se anche in pH Acido: -15% aggiuntivo

Modificare `GetYieldMultiplier()` per includere anche il modificatore muffe.

### 3. Modificatori Mutazioni basati su Mold Risk + Famiglia

**File**: `Assets/_Project/Scripts/Dome/PotSystem/Mold/MoldSystem.cs` (NUOVO metodo)

Aggiungere metodo `GetMoldMutationBonus()` che calcola bonus mutazioni:

- **EVIL con Mold Risk**:
- Mold Risk Level 1-2: +15% probabilità mutazioni
- Mold Risk Level 3: +30% probabilità mutazioni
- Bonus extra se anche in pH Basico: +10% aggiuntivo
- **PURE con Mold Risk**:
- Mold Risk Level 1-2: -10% probabilità mutazioni
- Mold Risk Level 3: -20% probabilità mutazioni
- Penalità extra se anche in pH Acido: -10% aggiuntivo

Questo modificatore verrà usato quando il sistema mutazioni sarà implementato (estrazione spore + ibridazione).

### 4. Rimozione Blocco Crescita per EVIL

**File**: `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

Modificare logica blocco crescita in `ResolveGrowthForAllPots()`:

- **EVIL**: NON viene bloccata da Mold Risk (solo da altre condizioni)
- **PURE**: bloccata a Mold Risk Level ≥1 (più sensibile)
- **Standard**: bloccata a Mold Risk Level ≥2 (sistema attuale)

### 5. Modifiche a ApplyInfestation per Famiglie

**File**: `Assets/_Project/Scripts/Dome/PotSystem/Mold/MoldSystem.cs`

Modificare `ApplyInfestation()` per considerare famiglia:

- **EVIL infestata**: NO riduzione livello (o riduzione minore: -1 invece di -3)
- **PURE infestata**: riduzione livello maggiore: -5 invece di -3
- **Standard**: sistema attuale (-3)

**NOTA Condensazione:**
La condensazione al 100% accelera l'infestazione (riduce giorni richiesti da 2 a 1, o immediata se già a livello 3 da 1 giorno). Questo significa:
- **EVIL**: Infestazione più veloce → Bonus resa +50% attivi prima (strategia high risk, high reward)
- **PURE**: Infestazione più veloce → Penalità resa -50% e riduzione livello -5 attive prima (doppia penalità)

### 6. Integrazione Modificatori in DayCycleController

**File**: `Assets/_Project/Scripts/Dome/SPOR-BLK-01-03A-DayCycleController.cs`

In `ResolveGrowthForPot()`:

- Aggiungere calcolo `moldGrowthModifier` usando `PhGrowthModifier.GetMoldGrowthModifier()`
- Applicare moltiplicatore cumulativo: `totalGrowthMultiplier = conditionGrowthMultiplier * phGrowthMultiplier * moldGrowthModifier`

In `DoHarvest()` (o dove si calcola resa):

- Aggiungere calcolo `moldYieldModifier` usando `PhGrowthModifier.GetMoldYieldModifier()`
- Applicare moltiplicatore resa cumulativo

### 7. Aggiornamento MoldConfig (opzionale)

**File**: `Assets/_Project/Scripts/Dome/PotSystem/Mold/MoldConfig.cs`

Aggiungere campi configurabili per bonus/penalità:

- `evilGrowthBonusLevel1_2` (default: 0.2f = +20%)
- `evilGrowthBonusLevel3` (default: 0.3f = +30%)
- `evilYieldBonusLevel1_2` (default: 0.2f = +20%)
- `evilYieldBonusLevel3` (default: 0.5f = +50%)
- `evilMutationBonusLevel1_2` (default: 0.15f = +15%)
- `evilMutationBonusLevel3` (default: 0.3f = +30%)
- Stesso per PURE (valori negativi)

### 8. UI Feedback (opzionale)

**File**: `Assets/_Project/Scripts/UI/VaultMap/PotDetailsWidget.cs` o `PlantCardV2DataBinder.cs`

Aggiungere tooltip/indicatori che mostrano:

- "EVIL: Mold Risk aumenta resa e mutazioni"
- "PURE: Mold Risk riduce crescita e resa"
- **OPZIONALE**: "Condensazione alta accelera Mold Risk" (per EVIL/PURE quando condensazione >50%)

## Struttura Implementazione

### Fase 1: Core Modificatori

1. Aggiungere metodi in `PhGrowthModifier` per modificatori muffe
2. Modificare `GetGrowthMultiplier()` e `GetYieldMultiplier()` per includere muffe
3. Aggiungere metodo `GetMoldMutationBonus()` in `MoldSystem`

### Fase 2: Integrazione Crescita

1. Modificare `ResolveGrowthForPot()` per applicare modificatore muffe
2. Modificare logica blocco crescita per EVIL

### Fase 3: Integrazione Resa

1. Modificare calcolo resa in `DoHarvest()` per applicare modificatore muffe

### Fase 4: Modifiche Infestazione

1. Modificare `ApplyInfestation()` per considerare famiglia

### Fase 5: Config e UI (opzionale)

1. Aggiungere campi configurabili in `MoldConfig`
2. Aggiornare UI per mostrare sinergie

## Note Tecniche

- I modificatori sono cumulativi (moltiplicativi)
- EVIL non bloccata da Mold Risk crea strategia "high risk, high reward"
- PURE più sensibile crea necessità di gestione attenta
- Bonus/penalità extra per pH non ottimale crea sinergia doppia

## Integrazione con Sistema Condensazione

**Sistema Condensazione già implementato** (`sistema_condensazione_completo_notion_935809cc.plan.md`):

1. **Giorni Virtuali da Condensazione:**
   - Condensazione >50% aggiunge giorni virtuali a `DaysOverwateringConsecutive`
   - Questo accelera l'accumulo di Mold Risk (più giorni = più veloce raggiungimento livelli 1-3)
   - **Impatto su EVIL**: Condensazione alta → Mold Risk si accumula più velocemente → Bonus crescita/resa attivi prima
   - **Impatto su PURE**: Condensazione alta → Mold Risk si accumula più velocemente → Penalità crescita/resa attive prima

2. **Accelerazione Infestazione:**
   - Condensazione 100% riduce giorni richiesti per infestazione (2→1→0 giorni)
   - **Impatto su EVIL**: Infestazione più veloce → Bonus resa +50% attivi prima (strategia high risk, high reward)
   - **Impatto su PURE**: Infestazione più veloce → Penalità resa -50% e riduzione livello -5 attive prima (doppia penalità)

3. **Strategia di Gioco:**
   - **EVIL + Condensazione Alta**: Strategia "high risk, high reward" - lasciare condensazione alta accelera Mold Risk e infestazione, attivando bonus crescita/resa prima
   - **PURE + Condensazione Alta**: Strategia "high risk, high penalty" - condensazione alta è pericolosa, accelera penalità e infestazione
   - **Standard**: Condensazione alta accelera Mold Risk ma senza bonus/penalità extra (solo blocco crescita a livello ≥2)

**Considerazioni per Implementazione:**
- I modificatori Mold Risk + Famiglia + pH sono già sufficienti per creare differenziazione
- La condensazione agisce come "acceleratore" del sistema Mold Risk esistente
- Non è necessario aggiungere modificatori diretti basati su condensazione (la sinergia è indiretta attraverso accelerazione Mold Risk)
- **OPZIONALE**: Potrebbe essere interessante aggiungere tooltip che mostrano "Condensazione alta accelera Mold Risk" per EVIL/PURE