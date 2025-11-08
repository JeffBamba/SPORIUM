# SPORIUM — Game Design Document Unificato
# PARTE 2: Items, Sistemi Avanzati, Ibridi, Mutazioni e Atti Narrativi

**Versione:** 1.0 (08/10/2025)  
**Progetto:** Sporium - Build Beta  
**Autore:** Game Design Team

---

# INDICE PARTE 2

1. [Sezione 7 — ITEM Livelli e COSTO IN CRY](#sezione-7)
2. [Sezione 8 — Azioni Giornaliere: Bonus e Incrementi](#sezione-8)
3. [Sezione 9 — Eventi Random Bonus/Malus](#sezione-9)
4. [Sezione 10 — Missioni](#sezione-10)
5. [Sezione 11 — Fazioni e Reputazione](#sezione-11)
6. [Sezione 12 — Toast HUD System](#sezione-12)
7. [Catalogo Piante HYBRID](#catalogo-hybrid)
8. [Sistema MUTAZIONI](#sistema-mutazioni)
9. [Piante Dome Avanzata](#piante-avanzata)
10. [Creature Clonabili](#creature-clonabili)
11. [ATTI NARRATIVI](#atti-narrativi)

---

<a name="sezione-7"></a>
# Sezione 7 — ITEM Livelli e COSTO IN CRY

## Tassonomia codici item — riferimento (ITA)

- **REA-xxx** — Reagenti
    - REA-001 Medium Nutritivo A
    - REA-101 Catalizzatore Alpha
    - REA-102 Catalizzatore Beta
    - REA-103 Catalizzatore Delta
- **STR-xxx** — Strumenti di laboratorio
    - STR-001 Microscopio
    - STR-002 Seed Extraction Machine
    - STR-003 Pipetta
    - STR-004 Spray Antifungino
- **MOD-xxx** — Moduli / Upgrade di sistema
    - MOD-201 Modulo Turno Esteso
    - MOD-202 Crono‑Lamp
    - MOD-203 Dome Autowatering I
- **WAT-xxx** — Acque e idratazione
    - WAT-RAW Acqua Grezza
    - WAT-POT Acqua Potabile
- **SPO-xxx** — Spore
    - SPO-001 Spora Pura
    - SPO-002 Spora Corrotta
    - SPO-003 Spora Neutra
    - SPO-004 Spora Onirica
- **SDE-xxx** — Semi
    - SDE-001 Seme Generico (Base)

---

## Tabella Riepilogo Oggetti

| Oggetto | Descrizione | Acquistabile | Costo CRY |
|---|---|---|---|
| SPO-001 — Spora Pura | Spora estratta perfettamente. Tratti stabili. | Raramente — Custodi | 120 |
| SPO-002 — Spora Corrotta | Spora da estrazione fallita. Instabile. | Culto della Muffa / Mercato Nero | 120 |
| SPO-003 — Spora Neutra | Esito casuale di estrazioni standard. | Ricompense/eventi. Mercato Nero | 50 |
| SPO-004 — Spora Onirica | Derivata da missioni Ipnotici. | Solo ricompense Ipnotici | — |
| SDE-001 — Seme Generico | Fusione di due spore. Base per piantumazione. | Sì — Fazioni / Mercato Nero | 30 |
| WAT-RAW — Acqua Grezza | Risorsa primaria per irrigazione. | Sì — Fazioni neutrali | 5 |
| WAT-POT — Acqua Potabile | Prodotta in Food Room da WAT-RAW. | No — solo produzione | — |
| STR-004 — Spray Antifungino | Cura infestazioni da muffe. | Sì — Custodi | 35 |
| Fertilizzanti Standard | Boost crescita. | Sì — Fazioni neutrali | 25 |
| Fertilizzanti Puri | Boost crescita. | Sì — Custodi / Mercato Nero | 75 |
| Fertilizzanti Proibiti | Boost crescita. | Sì — Culto Muffa / Mercato Nero | 75 |
| REA-101 — Catalizzatore Alpha | Attiva seme. | Sì — Fazioni / Mercato Nero | 40–100 |
| REA-102 — Catalizzatore Beta | Attiva seme. | Sì — Fazioni / Mercato Nero | 50–120 |
| REA-103 — Catalizzatore Delta | Attiva seme. | Sì — Fazioni / Mercato Nero | 60–150 |
| LED Blu | Accelera crescita. | Sì — Custodi / Mercato Nero | 45 |
| LED Rosso | Favorisce fioritura. | Sì — Culto Muffa / Mercato Nero | 45 |
| MOD-201 — Modulo Turno Esteso | +1 Azione base. | Sì — Solo Mercato Nero | 1250 |
| MOD-202 — Crono‑Lamp | +1 Azione base finché attiva. | Sì — Solo Mercato Nero | 1250 |
| MOD-203 — Dome Autowatering I | Automazione irrigazione. | Sì — Solo Mercato Nero | 2000 |

---

## Formula Prezzi — Sistema di Vendita

**Prezzo base** = definito nella scheda specie (Prodotto, Frutto).

**Valore di vendita** = Prezzo base × Moltiplicatore Fazione × Qualità × Reputazione

**Moltiplicatore Fazione:**
- Custodi: +30% su PURE e prodotti "luminosi"
- Mercanti: +5% su STANDARD industriali
- Culto della Muffa: +30% su EVIL e psicoattivi

**Qualità:**
- Comune ×1.00
- Non comune ×1.10
- Rara ×1.25
- Epica ×1.40
- Leggendaria ×1.60

**Reputazione:**
- Sfiducia −10%
- Neutro ×1.00
- Fiducia +10%
- Alleanza +20%

---

## Listino Vendita Piante — Sistema CRY

| Famiglia | Fazione Principale | Lvl 1→5 Valore Base (CRY) | Bonus Reputazione | Note |
|---|---|---|---|---|
| **PURE** | Custodi (+30%) | 60 · 80 · 100 · 125 · 150 | +30% Custodi | Pure Lvl 5 = risorsa rara |
| **EVIL** | Culto Muffa (+30%) | 70 · 90 · 115 · 140 · 170 | +30% Culto | Alto rischio → alto rendimento |
| **STANDARD** | Mercanti (+20%) | 40 · 55 · 70 · 90 · 110 | +20% Mercanti | Valuta neutra nel baratto |
| **HYBRID (PURE)** | Custodi/Religiosi (+25%) | 90 · 115 · 145 · 175 · 210 | +25% Custodi | Complessità genetica |
| **HYBRID (EVIL)** | Culto/Ipnotici (+25%) | 95 · 120 · 150 · 185 · 225 | +25% Culto | Materiali "corrotti" |
| **MUTAZIONI** | Mercato Nero (+40%) | 100 · 130 · 160 · 190 · 230 | +40% Mercato | Non vendibili a fazioni regolari |

---

<a name="sezione-8"></a>
# Sezione 8 — Azioni Giornaliere: Bonus e Incrementi

## Principi di Design

Il numero di **Azioni giornaliere** rappresenta la capacità operativa del biologo in un singolo ciclo di lavoro nel Vault.

Le Azioni si **resettano ogni notte**, al termine dell'**End of Day**.

**Numero base di Azioni:** **4 al giorno**.

---

## 1. Bonus da Cibo e Frutti Commestibili

### a) Cibo sintetico (Food Room)

| Tipo alimento | Bonus Azioni | Costo Produzione | Note |
|---|---|---|---|
| Vegetali sintetici | +1 | 5 CRY | Effetto leggero |
| Funghi sintetici | +2 | 8 CRY | Bonus medio |
| Carne sintetica | +3 | 12 CRY | Effetto forte |
| Cellule "Super Energia" | +1 extra | +4 CRY | Cumulabile |
| Cellule "Indigestione" | −1 | — | Malus casuale |

**Regole:**
- Bonus si applica **solo al Giorno corrente**.
- Il primo alimento consumato garantisce **bonus +1 Azione** ("Piano Colazione").

### b) Frutti commestibili della Dome

- Mangiare un frutto **non comporta alcun costo in CRY**.
- Solo **il primo frutto consumato in un Giorno** genera il suo effetto.
- Effetti sempre temporanei (durano fino all'End of Day).

---

## 2. Potenziamenti e Migliorie Strutturali

| Potenziamento | Dominio | Bonus Azioni | Costo Acquisto | Costo Manutenzione | Note |
|---|---|---|---|---|---|
| Slot Passivo "Efficienza Botanica" | Dome | +1 | — | 0 CRY | Da pianta specifica |
| Modulo Turno Esteso | Bedroom | +1 | 1250 CRY | — | Permanente |
| Crono‑Lamp | Bedroom | +1 | 1250 CRY | — | Finché alimentata |
| Permesso Operativo (fazione) | Visitor Room | +1 | — | 0 CRY | Perso se reputazione scende |

---

## 3. Eventi Random e Condizioni Straordinarie

**Esempio – Evento "Adrenalina Notturna"**
- Giorno seguente: **+2 Azioni temporanee**
- Giorno successivo: **−1 Azione** (effetto rebound)

---

## 4. Riduzioni di costo equivalenti a +Azioni

| Sistema | Effetto | Costo Acquisto | Costo Manutenzione | Equivalenza |
|---|---|---|---|---|
| Dome Autowatering I | 1 Watering gratuito/giorno | 2000 CRY | — | ≈ +1 Azione |
| Auto-Lighting Module I | 1 Light gratuito/giorno | 500 CRY | +5 CRY/giorno | ≈ +1 Azione |
| Seed Extractor Auto-Cycle | −50% costo Azioni minigiochi Lab | 2000 CRY | +20 CRY/giorno | Cumulabile |

---

<a name="sezione-9"></a>
# Sezione 9 — Eventi Random Bonus/Malus

Gli **Eventi Randomici** rappresentano il modo in cui il Vault "respira" e reagisce all'azione del Biologo.

## Regole di Attivazione

- Eventi selezionati in modo **pseudo-randomico** ma **contestuale**.
- Ogni Atto possiede un pool dedicato.
- GameManager li valuta ogni 2–4 giorni reali.
- Durata variabile (3–8 giorni).

---

## ATTO I — "Routine del Biologo"

**Difficoltà:** Bassa

| Tipo | Riferimento | Descrizione | Durata |
|---|---|---|---|
| Evento | EVT-017 – Condensa Ridotta | Prime fluttuazioni idratazione | 5 giorni |
| Evento | EVT-018 – Muffa Improvvisa | Introduzione Spray Antifungino | 3 giorni |
| Evento | EVT-002 – Pioggia Acida | Test controllo pH | 5 giorni |
| Evento | EVT-005 – Visitatore Affamato | Dilemma: dare 3 frutti o negare | — |
| VO | VO-CH-01 – "Senza acqua" | Non innaffiare per 3 giorni | 3 giorni |
| VO | VO-CH-03 – "Senza sprechi" | Giorno senza spese CRY | 1 giorno |
| VO | VO-CH-04 – "Ordine perfetto" | pH tra −5 e +5 per 5 giorni | 5 giorni |

---

## ATTO II — "La Serra che Respira"

**Difficoltà:** Media

| Tipo | Riferimento | Descrizione | Durata |
|---|---|---|---|
| Evento | EVT-019 – Drift pH | Sbilanciamento serio del pH | 5 giorni |
| Evento | EVT-010 – Rottura Filtro | Blocca concimi fino a riparazione | Persistente |
| Evento | EVT-020 – Scarsità d'Acqua | Pressione gestionale | 8 giorni |
| VO | VO-CH-02 – "Solo luce rossa" | Usa solo LED Red per 3 giorni | 3 giorni |
| VO | VO-CH-05 – "Solo la muffa capisce" | Non rimuovere muffa per 3 giorni | 3 giorni |

---

## ATTO III — "Il Vault si Ricorda"

**Difficoltà:** Alta

| Tipo | Riferimento | Descrizione | Durata |
|---|---|---|---|
| Evento | EVT-012 – Reazione a Catena | Conflitto piante incompatibili | Finché risolto |
| Evento | EVT-013 – Seed Storage Infetto | Eliminare oggetti contaminati | Finché svuotato |
| VO | VO-CH-07 – "Silenzio operativo" | Giorno senza azioni Dome | 1 giorno |
| VO | VO-CH-08 – "Equilibrio perfetto" | 2 Pure + 2 Evil per 10 giorni | 10 giorni |

---

## ATTO IV — "Il Ciclo del Ritorno"

**Difficoltà:** Molto Alta

| Tipo | Riferimento | Descrizione | Durata |
|---|---|---|---|
| Evento | EVT-019 – Drift pH (amplificato) | ±40 punti; crisi equilibrio | 8 giorni |
| Evento | EVT-020 – Scarsità Critica | Riserve quasi nulle | 8 giorni |
| VO | VO-CH-10 – "Chiudi tutto" | Distruggi tutte le piante attive | — |
| VO | VO-CH-11 – "Equilibrio instabile" | pH tra −2 e +2 per 7 giorni | 7 giorni |

---

<a name="sezione-10"></a>
# Sezione 10 — Missioni

## Struttura Generale

- Ogni **Main Quest (MQ)** è preparata da **5 Side Quest (SQ)**.
- Le scelte nelle SQ influenzano reputazioni, risorse e conseguenze della MQ successiva.
- Le missioni sono **dilemmi morali** mascherati da obiettivi botanici.

---

## Missioni PURE | Bene

**MQ-BEN-01 — Bloom of Hope**
- Fai fiorire Night-Bloom Iris in una Dome pura.
- **Reward:** Petalo Luminoso (item craft raro).
- **Reputazione:** +20 Custodi.

**MQ-BEN-02 — Breath of Life**
- Distribuisci frutti Arctic Hask a visitatori deboli.
- **Reward:** Favori Custodi + Semi Purificati.
- **Reputazione:** +15 Custodi, –10 Culto.

**MQ-BEN-03 — Lantern Vigil**
- Coltiva Lantern Moss fino a piena bioluminescenza.
- **Reward:** +20 CRY, Biolumina concentrata.
- **Reputazione:** +20 Custodi.

**MQ-BEN-04 — Purifier's Task**
- Usa Ferric Fern per creare antifungino e liberare Dome infestata.
- **Reward:** Craft Antifungino Potenziato.
- **Reputazione:** +15 Custodi, –15 Culto.

**MQ-BEN-05 — Seeds of Tomorrow**
- Consegna fibre di Blue Sedge per rinforzare colonia esterna.
- **Reward:** 100 CRY + Semi custodi.
- **Reputazione:** +20 Custodi, –10 Culto.

---

## Missioni EVIL | Male

**MQ-MAL-01 — Banquet of Flesh**
- Nutri pianta carnivora con animali vivi.
- **Reward:** Frutto predatorio raro.
- **Reputazione:** +20 Culto.

**MQ-MAL-02 — Ashes of the Forgotten**
- Usa cenere umana su PLT-106.
- **Reward:** Semi Blightseed (ibridi unici).
- **Reputazione:** +15 Culto, –20 Custodi.

**MQ-MAL-03 — Hall of Mirrors**
- Consuma frutto allucinogeno → missione glitchata con HUD distorta.
- **Reward:** Semi onirici + 30 CRY.
- **Reputazione:** +20 Ipnotici.

**MQ-MAL-04 — Harvest of Pain**
- Corrompi Arctic Hask con concime di sangue.
- **Reward:** Corrupted Hask Fruit (solo Culto).
- **Reputazione:** +15 Culto, –10 Custodi.

**MQ-MAL-05 — The Gift That Consumes**
- Consegna volontariamente pianta tossica a insediamento affamato.
- **Reward:** 120 CRY, favori Black Market.
- **Reputazione:** +20 Culto, –20 Custodi.

---

<a name="sezione-11"></a>
# Sezione 11 — Fazioni e Reputazione

## Sistema di Reputazione Fazioni

Ogni fazione ha un punteggio di **reputazione** compreso tra **−100 e +100**.

I rapporti sono **bilaterali e antagonisti**: aumentare la fiducia di una riduce quella dell'opposta.

| Fazione | Opposta | Drift Naturale | Note |
|---|---|---|---|
| **Custodi** | Culto della Muffa | +2 / −2 | Ogni pianta PURE incrementa Custodi |
| **Culto della Muffa** | Custodi | −2 / +2 | Piante EVIL favoriscono il Culto |
| **Ipnotici** | Tutte | ± variabile | Stati ipnotici alterano rapporti |
| **Mercato Nero** | Tutte | − costante | Ogni scambio riduce reputazione globale |

> **Regola fondamentale:** La reputazione dei **Custodi** e del **Culto della Muffa non possono mai pareggiare.** Se una sale, l'altra scende proporzionalmente.

---

## Fazioni Dettagliate

### 🏛️ Custodi

- **Ideologia:** Rinascita, purezza, rigenerazione del mondo naturale.
- **Credo:** "La natura non ha bisogno di noi, siamo noi che dobbiamo chiederle perdono."
- **Caratteristiche:** Devoti alle Spore Pure (SPO-001) e alle piante PURE.
- **Effetti di Gioco:** Aumenta reputazione Pure, sblocca missioni di bonifica.

### 🦠 Culto della Muffa

- **Ideologia:** La decomposizione è la vera forma di vita.
- **Credo:** "Solo ciò che marcisce può davvero rinascere."
- **Caratteristiche:** Favoriscono Spore Corrotte (SPO-002) e piante EVIL.
- **Effetti di Gioco:** Reputazione Toxic ↑; sblocca mutazioni e fertilizzanti corrotti.

### ⚖️ Mercato Nero (Mercanti Ombra)

- **Ideologia:** Sopravvivere a qualunque costo.
- **Credo:** "La vita è l'unica valuta che resta."
- **Caratteristiche:** Acquistano e vendono **qualsiasi cosa**, senza restrizioni.
- **Impatto:** Ogni uso genera impatto negativo minimo su tutte le fazioni.

### 🧘 Ipnotici

- **Ideologia:** La coscienza come portale biologico.
- **Credo:** "Solo nel sogno si può ricordare la verità."
- **Caratteristiche:** Legati alle Spore Oniriche (SPO-004).
- **Effetti:** Accesso a missioni "Ipnotiche" dall'Atto III.

---

## Sistema CALL — Visitor Room

**Funzioni Principali:**

1. **CALL (Fazione)** → Apre canale di scambio.
2. **CALL (Non interessata)** → Risposta neutra.
3. **Nessuna fazione interessata** → "Contatta il Mercante Ombra" → malus reputazione globale.

---

<a name="sezione-12"></a>
# Sezione 12 — Toast HUD System

## Scopo & Principi

Il **Toast HUD System** è il canale di notifica diegetico che informa il player di **cambiamenti di stato** senza interrompere il flusso di gioco.

## UX & Posizionamento

- **Stack verticale** in **alto-destra**
- **Max 3 toast visibili**
- **Gerarchia visiva**: colore + icona per canale

## Tassonomia dei messaggi (Canali)

1. **Info** — aggiornamenti non critici
2. **Success** — azione riuscita / ricompensa
3. **Warning** — rischio o stato negativo gestibile
4. **Danger** — condizione grave/bloccante
5. **Narrative** — frammenti diegetici/VO
6. **Tutorial** — solo durante tutorial
7. **System/Debug** — disattivo di default

## Esempi di Template

| Codice | Severity | Messaggio | Source |
|---|---|---|---|
| RES-001 | Warning | `Not enough Raw Water` | Dome/Inventory |
| PH-003 | Danger | `Dome pH in {band}: {affinity} plants collapsing` | pH |
| HYD-001 | Success | `Hydrated — in optimal range` | Plant |
| MLD-201 | Warning | `Mold detected ({severity}) — treat soon` | Plant |
| MUT-300 | Warning/Danger | `Mutation ({polarity}) on {plant}` | Dawn Check |

---

<a name="catalogo-hybrid"></a>
# CATALOGO PIANTE HYBRID

## Gruppo 1 — Ibridi Primari (Standard × Pure/Evil)

### HYB-201 · Ferric Tangle

**Genitori:** Ferric Fern (Std +1) × Red Tangle Vine (Evil −3)  
**Drift pH:** −2 Acid / giorno

**Poteri:**
- **Active:** Emocatalitica → cura Muffe +50%; +10% crescita 2gg; −5 pH giornaliero
- **Passive:** Corrosione Controllata+ → oscillazioni pH −10%; +10% mutazioni adattive

**Effetto Commestibile:** Blood Iron Surge → +4 Azioni; +20% resistenza Muffe 2gg; **Food Room disabilitata 2gg**

**Outputs:**
- Prodotto (PRD-HYB-201A): Tralci Ferrosi (75 CRY)
- Frutto (FRG-HYB-201B): Bacca Ematica (110 CRY)

---

### HYB-202 · Saltshade Orchid

**Genitori:** Saltbloom (Std +1) × Umbral Orchid (Evil −2)  
**Drift pH:** 0 (neutro instabile)

**Poteri:**
- **Active:** Idrovampirica Evoluta → assorbe 100% condensa; +50% rischio Muffe
- **Passive:** Evaporazione Onirica → rimuove Stress Idrico; disidrata Player −100%

**Effetto Commestibile:** Nebbia Salina Forte → +3 Azioni; Visione Sfocata (−40% precisione 1gg)

**Outputs:**
- Prodotto (PRD-HYB-202A): Foglie Saline (65 CRY)
- Frutto (FRG-HYB-202B): Bulbo d'Ombra Salato (95 CRY)

---

### HYB-203 · Aurablade Reed

**Genitori:** Ambergrain Reed (Std +1) × Dawn Orchid (Pure +1)  
**Drift pH:** +2 Basic / giorno

**Poteri:**
- **Active:** Fotosintesi Risonante+ → +15% crescita; +5% resistenza Burn
- **Passive:** Conduttiva Estesa → efficienza LED +10%; energia Vault +10%; CRY +10%

**Effetto Commestibile:** Luce Liquida+ → +2 Azioni per 3gg; +100% idratazione; 50% chance 0 Azioni nei 3gg successivi

**Outputs:**
- Prodotto (PRD-HYB-203A): Fibre Aurorali (70 CRY)
- Frutto (FRG-HYB-203B): Capsula di Luce (105 CRY)

---

### HYB-204 · Celesthorn

**Genitori:** Ironroot Shrub (Std) × Celestial Vine (Pure +2)  
**Drift pH:** +1 Basic / giorno

**Poteri:**
- **Active:** Corazzante Avanzato → −25% Burn/Muffe; +30% crescita Pure
- **Passive:** Geostabile+ → −30% probabilità Eventi; −5% drift pH

**Effetto Commestibile:** Pelle di Luce Avanzata → Costo CRY = 0 per 3gg; **blocco LAB 2gg**

**Outputs:**
- Prodotto (PRD-HYB-204A): Radici di Luce (80 CRY)
- Frutto (FRG-HYB-204B): Frutto Corazzato (115 CRY)

---

## Gruppo 2 — Ibridi Avanzati (Pure × Evil)

### HYB-205 · Sanguine Lotus

**Genitori:** Hallowed Lotus (Pure +3) × Vitis Sanguinea (Evil −3)  
**Drift pH:** ±3 oscillante

**Poteri:**
- **Active:** Emo-Divina → cura Dome + reset Condizioni; −30 pH; +1 Mutazione/pianta
- **Passive:** Rituale Sacrilego → +30% resa Pure/Evil; Black Market +100% prezzi

**Effetto Commestibile:** Lacrima Sangue-Sacro → +5 Azioni; +25% crescita 2gg; **Muffa globale al 3° giorno; −30 pH istantaneo**

**Outputs:**
- Prodotto (PRD-HYB-205A): Petali Ematici Sacri (90 CRY)
- Frutto (FRG-HYB-205B): Bacca Rituale (130 CRY)

---

### HYB-206 · Umbral Iris

**Genitori:** Night-Bloom Iris (Pure +1) × Crystal Bloom (Evil −1)  
**Drift pH:** −2 Acid / giorno

**Poteri:**
- **Active:** Fotopsicotica → polarità giornaliera (+25% Pure/Evil; +15% mutazioni)
- **Passive:** Onda Mentale → +50% precisione Lab; ogni 3gg giorno "storto" (−80% precisione)

**Effetto Commestibile:** Sogno Inverso → −4 Azioni; Visione Ipnotica (+35% mutazioni Lab); Apatia

**Outputs:**
- Prodotto (PRD-HYB-206A): Petali Ombra-Cristallo (80 CRY)
- Frutto (FRG-HYB-206B): Fiore Ipnotico Inverso (120 CRY)

---

### HYB-207 · Arctic Weaver

**Genitori:** Arctic Hask (Pure +2) × Fleshblossom (Evil −2)  
**Drift pH:** −3 Acid / giorno

**Poteri:**
- **Active:** Crio-Digestiva → assorbe Muffe +20% crescita; −10% idratazione globale; −5 pH
- **Passive:** Predazione Fredda → −30% Burn Stress; +20% rischio Infestazioni Evil

**Effetto Commestibile:** Morsi Gelidi → idratazione Player 100%; stato FROZEN 1gg; **Seed Storage bloccato 1gg**

**Outputs:**
- Prodotto (PRD-HYB-207A): Foglie Criotiche (85 CRY)
- Frutto (FRG-HYB-207B): Bacca Congelata (120 CRY)

---

### HYB-208 · Celestial Orchid

**Genitori:** Celestial Vine (Pure +2) × Umbral Orchid (Evil −2)  
**Drift pH:** ±3 oscillante giornaliero

**Poteri:**
- **Active:** Dualità Luminosa → pH > 0 = +25% efficienza Azioni; pH < 0 = +25% mutazioni
- **Passive:** Risonanza Caotica → ogni alba randomizza bonus pianta (±30%)

**Effetto Commestibile:** Eclissi Totale → +6 Azioni immediate; −50% resa piante 3gg; pH oscillazione ±30; blocco LAB 2gg

**Outputs:**
- Prodotto (PRD-HYB-208A): Tralci Eclittici (95 CRY)
- Frutto (FRG-HYB-208B): Fiore d'Eclisse (140 CRY)

---

<a name="sistema-mutazioni"></a>
# Sistema MUTAZIONI

Le **Mutazioni** sono **deviazioni genetiche rare** (casuali o indotte) che **potenziano o degradano** tratti esistenti.

> "In Sporium, nessuna mutazione è davvero positiva o negativa: è solo una risposta della natura al modo in cui il giocatore plasma la Dome."

---

## Come Si Innescano (Timing)

1. **Dawn Check** (dopo End Day)
2. **Event Check** (evento forte / reagente)
3. **Lab Check** (sul seme: la precisione modifica seed stability)

## Le 5 Fonti che Caricano il Rischio (MutationScore)

- **pH mismatch** (Neutral/Stable/Ultra → 0 / +10 / +20)
- **Idratazione fuori banda** (Wet/Dry) → +5/giorno (cap +20)
- **LED abuse** → +10 (+5 extra se ripetuto)
- **Muffa** → +15 (Mild) / +30 (Severe)
- **Concime & Pruning** → Sacro +10 verso Armoniche; Proibito +10 verso Corrotte

---

## Categorie di Mutazione

### 🌸 Mutazioni Armoniche (Pure / Basiche)

Evoluzioni **stabilizzanti/rigenerative** — tipiche in **pH basico** con buona cura.

| Cod. | Nome | Effetto Principale | Range Base | Durata |
|---|---|---|---|---|
| MUT-101 | Respiro di Luce | +rigenerazione, −muffe, +longevità frutti | +15–30% | 5–7gg |
| MUT-102 | Vena Cristallina | +seed viability, +stabilità pH | +10–20% | permanente |
| MUT-103 | Pure Overgrowth | +crescita, −consumo acqua | +15–25% | 5gg |
| MUT-104 | Halo Bloom | +efficienza LED, +photosynthesis | +10–20% | permanente |

---

### 💀 Mutazioni Corrotte (Evil / Acide)

Evoluzioni **espansive/aggressive** — tipiche in **pH acido** con reagenti proibiti.

| Cod. | Nome | Effetto Principale | Range Base | Durata |
|---|---|---|---|---|
| MUT-301 | Mildew Bloom | muffe↑, crescita↑, drift− | +20–35% | 3–6gg |
| MUT-302 | Ferric Rot | resa↑, stabilità↓ | +20–40% | 5gg |
| MUT-303 | Spore Hunger | produzione↑, frutti durano meno | +15–25% | temp |
| MUT-304 | Toxic Synergy | mutation rate Lab↑ | +20–35% | perm |

---

### ⚙️ Mutazioni Adattive (Neutre / Ibride)

Evoluzioni **contestuali/instabili** — da Neutro, Nullseed o ambienti mutevoli.

| Cod. | Nome | Effetto Principale | Range Base | Durata |
|---|---|---|---|---|
| MUT-401 | Spiral Growth | crescita↑, stabilità↓ | +10–20% | 4–6gg |
| MUT-402 | Radice Errante | diffusione↑, drift ± casuale | +10–15% | 5gg |
| MUT-403 | Nullphase Bloom | output oscillante giorno/notte | ±20% | 3gg |
| MUT-404 | Chromatic Shift | sensibilità LED variabile | swap casuale | temp |

---

<a name="piante-avanzata"></a>
# Piante Dome Avanzata

## Master Table

| Code | Pianta | Tipo | Frutto | Usi | Effetto Permanenza Frutti |
|---|---|---|---|---|---|
| **PLT-AV101** | Fangbloom | Carnivora | Frutto Enzimatico | Lab → spore corrotte; Consumo → bonus Predatore; Scambio → alto valore Culto | Nessuno |
| **PLT-AV103** | Vermis Trap | Carnivora | Frutto Digestivo | Lab → spore corrotte speciali; Consumo → −1 giorno crescita pianta attiva | Nessuno |
| **PLT-AV201** | Pharma Iris | Farmaceutica | Frutto Sedativo | Lab → spore neutre oniriche; Consumo → rivela tratti + 1 azione | **+1 Azione giorno successivo** se maturi |
| **PLT-AV202** | Mycoheal | Farmaceutica | Frutto Curativo | Lab → spore pure rare; Consumo → rimuove stati negativi | Nessuno |
| **PLT-AV301** | Lumencore | Energetica | Frutto-Batteria | Lab → spore corrotte energetiche; Consumo → +X CRY o +1 azione | **Nessun costo CRY** finché frutti maturi |
| **PLT-AV302** | Solaris Bloom | Energetica | Frutto Cristallino | Lab → spore pure instabili; Consumo → riduce tempo incubazione | Nessuno |

---

<a name="creature-clonabili"></a>
# Creature Clonabili

## Topi Clonati (CLN-101)

### Procedura Operativa

**Step 1 — Preparazione materiali**
- Food Room: produci Carne Sintetica → ottieni 3× RES-PROT-001
- Lab CLN-201: 3× RES-PROT-001 → 1× CELL-001
- Preleva REA-204 (Medium) e REA-202 (Catalizzatore Beta)

**Step 2 — Dome Sperimentale**
- Inserisci CELL-001 + REA-204 in capsula
- Attendi 1 giorno → Prototipo Larvale (CLN-L1)

**Step 3 — Lab CLN-201**
- Trasferisci CLN-L1 nell'Incubatore Animale
- Avvia DNA Fusion Puzzle
- Applica 1× REA-202 per stabilizzazione
- **Esito:** nascita Topi Clonati (CLN-101)

**Step 4 — Output e Uso**
- **Output:** 1–3× CLN-101
- **Usi:** Nutrimento per piante carnivore; Contrabbando al Mercato Nero
- **Fazioni:** Culto favorevole; Custodi contrari

---

## Insettibridi (CLN-201)

### Materiali necessari

1. **CELL-001** — Cellula Staminale Animale
2. **PLT-106** — Red Tangle Vine (fibre parassitarie)
3. **REA-202** — Catalizzatore Beta

### Procedura

**Step 1:** Raccogli materiali (3 ORG-SCR-001 → 1 CELL-001; coltiva Red Tangle Vine)  
**Step 2:** Dome Sperimentale — CELL-001 + fibre PLT-106 → Prototipo Larvale Instabile (1 giorno)  
**Step 3:** Lab CLN-201 — DNA Fusion Puzzle + REA-202 → Insettibridi  
**Step 4:** Output 1–2 Insettibridi vegeto-insetto

**Usi:**
- Nutrimento per piante carnivore/fungine
- Richiesto dal Mercato Nero
- Culto: "insetti sacri della corruzione"

---

## Carne Coltivata (FOOD-301 Alt.)

### Materiali necessari

1. **RES-PROT-001** — Residuo Proteico (da Food Room)
2. **CELL-001** — Cellula Staminale Animale (3 RES-PROT → 1 CELL-001)
3. **REA-204** — Medium Nutritivo A

### Procedura

**Step 1:** Raccogli 3 RES-PROT-001 → Lab CLN-201 → 1 CELL-001  
**Step 2:** Dome Sperimentale — CELL-001 + REA-204 → Massa Embrionale Pulsante (1 giorno)  
**Step 3:** Lab CLN-201 — DNA Fusion Puzzle (nessun catalizzatore) → Carne Coltivata  
**Step 4:** Output 1–2 blocchi di Carne Coltivata

**Usi:**
- Fonte FOOD alternativa (FOOD-301)
- Materia prima per esperimenti complessi
- Vendibile al Mercato Nero

---

<a name="atti-narrativi"></a>
# ATTI NARRATIVI

## ATTO I — La Fame Bussa alla Porta

### Premessa

Il biologo si risveglia, la Dome prende vita. Il primo contatto col mondo esterno è un Mercante Ombra: ti tratta da fornitore potenziale.

### Main Quest — "Il Cetriolo d'Oro"

**Obiettivo:** Consegna al Mercante un frutto luminoso, cresciuto in pH neutro e fresco.

### Timeline STEP 0 → 6

**STEP 0 — Risveglio e VO Introduttivo**
- VO glitchato introduce la Visitor Room.

**STEP 1 — Il Test del Mercante (MQ-01A)**
- Obiettivo: coltivare e consegnare un frutto comune.
- Reward: seme raro con tratto luminoso.

**STEP 2 — La Pianta Luminosa (MQ-01B)**
- Obiettivo: piantare e coltivare il seme raro fino a Harvest.
- Reward: frutto luminoso.

**STEP 3 — Analisi delle Spore (MQ-01C)**
- Obiettivo: estrarre spore + analisi al Microscopio.
- Reward: accesso a spore luminose.

**STEP 4 — Creazione del Seme Cetriolo (MQ-01D)**
- Obiettivo: Pipetta + Catalizzatore → seme Cetriolo d'Oro.

**STEP 5 — Coltivazione del Cetriolo (MQ-01E)**
- Obiettivo: pH Neutro 2 giorni in Flowering con LED Blu.

**STEP 6 — Consegna finale (MQ-01F)**
- Obiettivo: consegnare il frutto.
- Reward: CRY + reputazione Mercato Nero + sblocco Research Tree.

---

## ATTO II — Radici di Ferro

### Premessa

I Militari entrano in scena come forza coercitiva. Chiedono fibra antifungina che resista alla muffa.

### Timeline STEP 0 → 6

**STEP 0 — L'arrivo dei Militari**
- Dialogo Ufficiale: "Ci serve fibra che non marcisca. Fallisci, e ti seppelliamo."

**STEP 1 — Ricerca notturna su Blue Sedge (5 notti minime)**
- Sequenza obbligatoria di 5 notti: Macro-Botanica → Categoria Fibrose → Habitat → Proprietà → Identificazione Blue Sedge.
- Reward: sblocco voce Wiki "Blue Sedge".

**STEP 2 — Ottenere semi Blue Sedge**
- **Opzione A:** Black Market (costo CRY alto)
- **Opzione B:** Side Quest "Vecchio Tessitore" (consegna 3 piante → Spore rare fibrose)

**STEP 3 — Analisi e sblocco nodo "Fibra Antifungo"**
- Minigiochi Lab: Microscopio + Tensile Test + Colorazione.
- Richiesto: 3 report + RP → sblocco protocollo.

**STEP 4 — Coltivazione Blue Sedge fino a Maturazione 5**
- Obiettivo: 3 piante Blue Sedge Lvl 5.
- Reward: Raw Fibrous Biomass.

**STEP 5 — Scambio al Black Market**
- Raw Fibrous Biomass → Fibre Conciate Antifungine.

**STEP 6 — Consegna finale ai Militari**
- Consegna Fibre Conciate.
- Reward: CRY + reputazione militare + nuovi nodi ricerca.

---

## ATTO III — Il Rumore del Vault

### Overview

L'Atto III segna il punto di rottura tra scienza e coscienza. Il Biologo inizia a percepire distorsioni sensoriali, glitch visivi e messaggi sussurrati dal Vault.

Consumare un **frutto ipnotico** innesca lo **stato Ipnotico**, avviando la Main Quest dell'Atto III.

### Condizioni di Accesso

| Tipo | Condizione | Effetto |
|---|---|---|
| Narrativa | Inizio Atto III | `HYP_UNLOCK = TRUE` |
| Ambientale | pH Dome ≥ +70 o ≤ −70 | Attiva instabilità percettiva |
| Biologica | 1 pianta PURE/EVIL/IBRIDA ≥ Lv 3 | Possibilità frutti ipnotici |

### Attivazione — "IL PRIMO MORSO"

Per accedere all'Atto III, il Biologo deve **consumare un frutto con potenza ipnotica**.

Al momento del primo morso:
- Dissolvenza bianca + VO glitchata.
- Nuova voce Diario: Main Quest "Sistema Ipnotico e Glitch Cognitivi"
- `HYP_ACTIVE = TRUE`

### Main Quest — "Sistema Ipnotico e Glitch Cognitivi"

L'obiettivo è **resistere** abbastanza a lungo da completare le cinque missioni imposte dagli Ipnotici.

---

## Side Quest Ipnotiche — "Le Prove del Vault"

| Codice | Titolo | Obiettivo | Effetto Collaterale | Lore |
|---|---|---|---|---|
| **HYP-SQ-001** | La Coppia Impossibile | Mantieni 1 Pure + 1 Evil per 3 giorni senza collasso pH | +25% drift pH; +15% consumo acqua | *"Il Vault sa che la purezza non esiste."* |
| **HYP-SQ-002** | Il Fiore Spezzato | Distruggi pianta sana prima della fioritura | Rep Custodi −10; +1 mutazione casuale | *"Solo chi tronca la bellezza capisce il dolore."* |
| **HYP-SQ-003** | Il Seme Senza Nome | Usa 2 spore senza tratti comuni → seme anomalo | −10 CRY; +15% instabilità Lab | *"Così nascono gli dèi o i mostri."* |
| **HYP-SQ-004** | Il Giardino Silente | Lascia piante senza acqua/luce 3gg → pieno recupero | Tutte Stressate; resa −30% per 2gg | *"Nel silenzio il Vault ascolta meglio."* |
| **HYP-SQ-005** | Memoria del Sogno | Ricerca Notturna "Lore Ipnotici" + quiz 3 domande | Fallire → −10 rep; Successo → +1 frammento | *"Hai ricordato ciò che non era accaduto."* |

### Conclusione Atto III

- pH torna neutro.
- Tutti i glitch cessano.
- Ricompensa: **2× SPO-004 Spore Oniriche**.

---

## ATTO IV — Il Giardino dei Sogni

### Concetto Generale

Dopo l'Atto III, il Biologo può accedere al **LAB-CLN-201** e alla **Dome Avanzata**, dove germogliano le **3 Piante Ipnotiche**.

### Pipeline Onirica

1. Ottenere 2 Spore Oniriche (fine Atto III)
2. Usare le Spore nel Lab Clonazione → generano 3 Semi Ipnotici
3. Piantare i Semi nella **Dome Avanzata** (3 vasi vetrati)
4. Cura giornaliera (stimolo mentale o offerta energetica)

### Regole Dome Avanzata

- **Nessun pH**: piante vivono "fuori dal sistema"
- **Nessun frutto, nessuna riproduzione**: entità perfette Livello 5
- **3 vasi vetrati attivi** (nessun passivo)
- **Spore Oniriche non si distruggono**
- **Ciclo di cura psico-botanica** giornaliero

---

## Le Tre Cure Ipnotiche (Routine Giornaliera)

### 1. Dreamroot Vein — "Sincronizzazione"

**Minigioco:** Ritmo pulsante visivo/sonoro. Click al picco del ritmo, 10 volte.

**Esiti:**
- ≥8 successi → +6 Azioni/giorno
- 5–7 → +3 Azioni/giorno
- ≤4 → −3 Azioni/giorno, HUD sfocato

**Incuria:** Dopo 3 giorni → Trance Inversa (auto-azioni casuali)

---

### 2. Whisper Bloom — "Ascolto"

**Minigioco:** 3 voci sovrapposte, identificare la parola chiave corretta.

**Esiti:**
- 3/3 → +30% precisione tutti minigiochi; doppia estrazione
- 2/3 → +15% precisione
- ≤1 → Risonanza Caotica (comandi invertiti)

**Incuria:** Dopo 3 giorni → Ciclo Disarmonia (VO distorta 48h)

---

### 3. Mirror Ivy — "Riflesso"

**Minigioco:** Sequenze visive di eventi passati + 3 risposte, solo 1 corretta.

**Esiti:**
- 3/3 → −50% costi End Day; +2 reputazione tutte fazioni
- 2/3 → −25% costi End Day
- ≤1 → Dissonanza Cronica (azioni invertite; −10 CRY)

**Incuria:** Dopo 3 giorni → Rottura Specchio (UI invertite 24h)

---

### Conclusione Atto IV

L'Atto IV si conclude in due modi:

**A) Collasso (incuria 3 giorni):**
- Tutte piante Ipnotiche rimosse
- Reputazione Ipnotici = −100
- Ricompensa: 500 CRY

**B) Resilienza (30 giorni consecutivi):**
- Visitatori Ipnotici comunicano fine fase
- Ricompensa: Reputazione Ipnotici +100 + 2000 CRY

---

## ATTO V — La Guerra delle Spore

### Transizione verso ATTO V

All'**inizio dell'Atto V**, nella Visitor Room arriva **la fazione con cui il Biologo ha la reputazione più alta** per **avvertirlo** che **l'altra** sta preparando un attacco.

- **Se RepCustodi > RepCulto** → arriva rappresentante dei Custodi (avvisa di attacco del Culto)
- **Se RepCulto > RepCustodi** → arriva rappresentante del Culto (avvisa di blitz dei Custodi)

L'incontro **non apre una scelta**: **constata** l'allineamento e innesca la Main Quest di Atto V.

---

### Struttura Generale

- **Attivazione:** automatica dopo Atto IV.
- **Fazione dominante:** determinata dalla reputazione più alta.
- **Main Quest:** "Guerra delle Spore" (WAR-01).
- **Side Quest:** 5 missioni consecutive.
- **Laboratorio attivo:** LAB-CLN-201 (modalità "Bellica").

---

### Main Quest — "Guerra delle Spore"

| ID | Titolo | Tipo | Obiettivo | Esito |
|---|---|---|---|---|
| **WAR-01** | Guerra delle Spore | Main | Completare le 5 missioni di fazione e creare linea cloni HYB-P/E-101→401 | Attiva cutscene finale "Mega Glitch del Vault" |

---

## Filone CUSTODI (PURE PATH)

| Codice | Titolo Missione | Obiettivo | Ricompensa | Materiali per | Lore |
|---|---|---|---|---|---|
| **WAR-P-001** | Rinascita nel Silenzio | Raccogli 3 piante PURE mature | 2× SPO-001 + 1× Cat. Alpha | **HYB-P-101 Sentinel Bloom** | Custodi "riaccendono la luce" |
| **WAR-P-002** | La Luce che Purifica | Crea e consegna 3 Sentinel Bloom | 2× SPO-003 + Medium A | **HYB-P-102 Glass Mycel** | Fibre cristalline come reagente |
| **WAR-P-003** | Radici del Sacrificio | Crea e consegna 2 Glass Mycel | 1× SPO-002 + Cat. Beta | **HYB-P-201 Aegis Fern** | Spore impure per "resistenza" |
| **WAR-P-004** | Sigillo delle Spore | Crea e consegna 2 Aegis Fern | 1× SPO-004 + Cat. Delta | **HYB-P-301 Ward Vine** | Spore illusione per controllo |
| **WAR-P-005** | Il Coro della Cupola | Crea e consegna 1 Ward Vine | Consente **HYB-P-401 Choir Bulb** | Attiva **cutscene PURE** | Voci Custodi si fondono |

---

## Filone CULTO DELLA MUFFA (EVIL PATH)

| Codice | Titolo Missione | Obiettivo | Ricompensa | Materiali per | Lore |
|---|---|---|---|---|---|
| **WAR-E-001** | Fiato della Muffa | Lascia marcire 3 piante EVIL | 2× SPO-002 + Cat. Alpha | **HYB-E-101 Blight Crawler** | Culto "risveglia" resti fungini |
| **WAR-E-002** | Nebbia d'Acido | Crea e consegna 3 Blight Crawler | 2× SPO-003 + Medium A | **HYB-E-102 Mire Bloom** | Emissioni distillate in reagente |
| **WAR-E-003** | Radici del Sangue | Crea e consegna 2 Mire Bloom | 1× SPO-001 + Cat. Beta | **HYB-E-201 Leech Ivy** | Spore pure corrotte |
| **WAR-E-004** | Corrotto il Canto | Crea e consegna 2 Leech Ivy | 1× SPO-004 + Cat. Delta | **HYB-E-301 Chorus Rot** | Muffa canta e Vault risponde |
| **WAR-E-005** | La Marea di Spore | Crea e consegna 1 Chorus Rot | Permette **HYB-E-401 Apex Mould** | Attiva **cutscene EVIL** | Vault diventa organismo unico |

---

### Cutscene Finale — "Il Delirio del Biologo"

**Trigger:** completamento WAR-P-005 o WAR-E-005.

**Ambiente:** Bedroom.

Il Biologo siede su una sedia metallica, testa reclinata, bottiglia vuota. Le luci LED lampeggiano come un ECG che si spegne.

**Voice Over (glitchato):**

> "Parlo… parlo… come se potessi salvarlo.
> Ma tutto questo… era prima del Vault.
> Prima che la speranza diventasse esperimento.
> Non sono vero. Non lo sono più.
> Sono l'ultimo pensiero che mi attraversa la testa
> prima che il veleno completi la mia presenza…"

**Sequenza visiva:**
- Tutte le Dome diventano trasparenti.
- I cloni si dissolvono in polvere di luce o muffa.
- Il Biologo cade all'indietro.
- Flash: esterno del Vault, tundra ghiacciata, silenzio.

**Ultima riga Diario SP.O.R.E.:**

> "Le fazioni, le piante, la guerra. Nulla era reale.
> L'unica rinascita della natura è la morte dell'uomo."

**Schermo nero → Logo SPORIUM glitchato → fade out.**

---

# APPENDICE TECNICA

## Legenda Valori e Sigle per il DEV

### Bande di pH (gioco)

| Sigla | Significato | Descrizione |
|---|---|---|
| **UA** | Ultra Acid | pH ≤ −80 — ambiente letale/corrosivo |
| **SA** | Strong Acid | −79…−30 — crescita difficoltosa, tratti corrotti |
| **N** | Neutral | −29…+29 — equilibrio stabile |
| **SB** | Strong Basic | +30…+79 — terreno ricco, tratti puri |
| **UB** | Ultra Basic | ≥ +80 — sovraccarico alcalino, sterilità |

### Idratazione (0–100%)

| Campo | Descrizione |
|---|---|
| **min–opt–max** | Valori minimi, ottimali e massimi |
| **Evaporazione/day** | Acqua persa ogni giorno (5–10) |
| **Dry / Opt / Wet** | Fasce idratazione HUD |
| **WetExp** | Giorni consecutivi sopra "Wet" |

### Crescita e Stadi

| Sigla | Nome Stage | Descrizione |
|---|---|---|
| **Seed** | Semina | Germinazione iniziale |
| **Growth** | Crescita | Espansione fogliare e radicamento |
| **Flowering** | Fioritura | Produzione enzimi/spore/fiori |
| **HarvestReady** | Raccolta | Resa massima; decay dopo 3 giorni |
| **Resting** | Riposo | Pausa fisiologica |

### Illuminazione

| Campo | Descrizione |
|---|---|
| **LED Blue / Red** | Luci per crescita o fioritura |
| **safe/day** | Numero massimo sicuro LED/giorno |
| **Burn Stress** | Danno da luce eccessiva |

### pH Drift

| Simbolo | Descrizione |
|---|---|
| **+X Basic/day** | Pianta rende pH più basico |
| **−X Acid/day** | Pianta rende pH più acido |
| **±0 Neutral/day** | Non altera pH globale |

### Pruning & Mold

| Campo | Descrizione |
|---|---|
| **Clean > X%** | Precisione minima taglio |
| **Mild / Severe** | Intensità infestazione |
| **Spray** | Oggetto per rimuovere muffe |
| **NoPrune ≥ Xg** | Giorni senza potatura → rischio muffa |

### Telemetry & QA

| Sigla | Evento Monitorato |
|---|---|
| **stage_change** | Cambio stadio |
| **hydration_band** | Cambio fascia idrica |
| **mold_event** | Comparsa o cura muffe |
| **purity_event / corruption_event** | Evento Purezza/Corruzione |
| **harvest** | Raccolta completata |

---

# MOCKUP HUD

## HUD Pianta — Dome Centrale

```
╔══════════════════════════════════════════════════════════════╗
║                         PLANT HUD                            ║
╠══════════════════════════════════════════════════════════════╣
║ 🌿 Nome: Arctic Hask (PLT-101)   Level: II   Affinità: Pura ║
║ Codice: PLT-101   Stato: Rigogliosa                          ║
╠══════════════════════════════════════════════════════════════╣
║ 📈 Stadio di Crescita: Fioritura (3/5)                       ║
║ [████████░░░░] → Seed → Sprout → Flower → Harvest → Rest     ║
╠══════════════════════════════════════════════════════════════╣
║ 🔬 Condizioni                                                ║
║ - Idratazione: 72% (OTTIMALE)                                ║
║ - Esposizione Luce: 60% (Blu attivo)                         ║
║ - Potatura: NON Potata (–10% resa potenziale)                ║
║ - Infestazioni: Sana                                         ║
║ - Fertilizzante: Nessuno                                     ║
╠══════════════════════════════════════════════════════════════╣
║ 🎮 Azioni Disponibili                                        ║
║ - 💧 Watering                                                ║
║ - ✂️ Pruning                                                 ║
║ - 🔦 LED Control                                             ║
║ - 🌸 Harvest: NON DISPONIBILE (non matura)                   ║
╠══════════════════════════════════════════════════════════════╣
║ 🍎 Produzione Attesa                                         ║
║ - Frutti previsti: 2 (decadono tra 3 giorni)                 ║
║ - Semi generabili: 1x SDE-001 (50% probabilità)              ║
╠══════════════════════════════════════════════════════════════╣
║ ⚠️ Rischi Attuali                                            ║
║ - pH globale Dome: 6.8 (Salubre)                             ║
║ - Muffa: 0% rischio                                          ║
║ - Stress Luce: 10%                                           ║
╚══════════════════════════════════════════════════════════════╝
```

---

## HUD Food Room — FOOD-SYNTH-001

```
╔════════════════════════════════════════════════════════════════╗
║                        FOOD-SYNTH-001                          ║
╠════════════════════════════════════════════════════════════════╣
║ [Slot 1]  Stato: OCCUPATO                                      ║
║ Coltura: Carne Sintetica                                       ║
║ Giorni restanti: 2 (Harvest Giorno 4, mattino)                 ║
║ Output previsto: 1x Cibo Carne (Valore: Alto, +3 AP)           ║
║ Residui generati: 1/3 (RES-PROT-001)                           ║
║ Costo giornaliero: 2 CRY                                       ║
╠════════════════════════════════════════════════════════════════╣
║ [Slot 2]  Stato: IN CRESCITA                                   ║
║ Coltura: Funghi Sintetici                                      ║
║ Giorni restanti: 1                                             ║
║ Output: 2x Cibo Fungo (+2 AP)                                  ║
║ Costo: 1 CRY                                                   ║
╠════════════════════════════════════════════════════════════════╣
║ [Slot Idrico] Stato: IN PRODUZIONE                             ║
║ Input: RAW WATER x5                                            ║
║ Output previsto domani: Acqua Potabile x5                      ║
║ Costo: 0 CRY                                                   ║
╠════════════════════════════════════════════════════════════════╣
║ COSTO TOTALE GIORNALIERO: 3 CRY                                ║
║ PROSSIMI HARVEST: Giorno 3 (Funghi), Giorno 4 (Carne)          ║
╚════════════════════════════════════════════════════════════════╝
```

---

## HUD Seed Storage — EXT-002

```
╔════════════════════════════════════════════════════════════════════╗
║                       SEED STORAGE — EXT-002                        ║
╠════════════════════════════════════════════════════════════════════╣
║ 📦 Inventario Player (sinistra)                                    ║
║ - SDE-001 Seed Vegetale Lvl 1 (x2)                                 ║
║ - HYB-203 Ferric Purifier Lvl 2 (x1)                               ║
║ - PLT-102 Night-Bloom Iris (frutto) Lvl 1 (x4)                     ║
║                                                                    ║
║ ⇨ [Trasferisci →]   (Costo: 1 Action)                              ║
╠════════════════════════════════════════════════════════════════════╣
║ 🧊 Seed Storage Slots (destra)                                     ║
║ Slots: 4/4   |   Capacità max: 20                                  ║
║ Costo giornaliero: 2 CRY (≥1 slot occupato)                        ║
║                                                                    ║
║ [Slot 1] SST-001 — SDE-001 Seed Vegetale Lvl 1 (x10)               ║
║ [Slot 2] SST-002 — HYB-203 Ferric Purifier Lvl 2 (x1)              ║
║ [Slot 3] SST-003 — PLT-102 Night-Bloom Iris Lvl 1 (x4)             ║
║ [Slot 4] SST-004 — SPO-002 Spora Corrotta Lvl 3 (x1)               ║
╠════════════════════════════════════════════════════════════════════╣
║ 🔍 Filtri: [ALL] [SEMI] [PIANTE] [SPORE] [FRUTTI]                  ║
╚════════════════════════════════════════════════════════════════════╝
```

---

## HUD Night Summary — Bedroom

```
+------------------------------------------------------+
|                 Night Summary — Day 4                |
+------------------------------------------------------+

[ Today — Economy (CRY) ]
Expenses:
- Electricity / Base Upkeep: –20 CRY
- Seed Storage (2 slots): –10 CRY
- Other Costs: –5 CRY
Total Expenses: –35 CRY

Revenues:
- Sold 1 Fruit to Visitor: +15 CRY
- Black Market Transaction: +25 CRY
- Mission Reward (Custodi): +30 CRY
Total Revenues: +70 CRY

Net Balance Today: +35 CRY
Current CRY Balance: 285

--------------------------------------------------------

[ Today — Actions Used ]
- Total: 4/4 Actions
  • Plant: 1
  • Water: 2
  • Microscope: 1

--------------------------------------------------------

[ Tomorrow — Forecast ]
Baseline: 4 Actions Available
Forecast pH Change: –12 (Acid Drift) ±2
Plant Status:
- PLT-101 Arctic Hask → Growth (90%)
- PLT-102 Night-Bloom Iris → Stable (85%)

Event Probability:
- "Acid Rain Risk: 43%"

--------------------------------------------------------

[ Controls ]
[ Archive ]    [ Open Wiki ]    [ Confirm & Sleep ]
```

---

## HUD Visitor Room — EXT-001

```
╔══════════════════════════════════════════════════════════════════╗
║                    VISITOR ROOM — EXT-001                        ║
╠══════════════════════════════════════════════════════════════════╣
║ 👤 Visitor: Wandering Custodian                                   ║
║ Faction: Custodians (Reputation: +10 → Friendly)                  ║
║ Request: Blue LED                                                 ║
║ Standard Offer: Pruning Kit (STR-002)                             ║
╠══════════════════════════════════════════════════════════════════╣
║ 📦 Player Inventory                                               ║
║ - 10 CRY                                                          ║
║ - SPO-001 Pure Spore (x2)                                         ║
║ - PLT-101 Arctic Hask (fruit) (x3)                                ║
╠══════════════════════════════════════════════════════════════════╣
║ ⚖️ Barter / Negotiation                                           ║
║ [Player Offer Field]                                              ║
║ Insert: [10 CRY] + [PLT-101 (x1)]                                 ║
║                                                                   ║
║ Visitor required value: 12                                        ║
║ Your offer value: 13 (Acceptable)                                 ║
║                                                                   ║
║ ✔ Trade Possible                                                  ║
║ → Result: Player receives STR-002 Pruning Kit                     ║
╠══════════════════════════════════════════════════════════════════╣
║ 🗣️ Reputation Bonus/Malus                                         ║
║ - Exact items → +Rep                                              ║
║ - Undervalued → –Rep                                              ║
║ - Overpay → Small +Rep                                            ║
╚══════════════════════════════════════════════════════════════════╝
```

---

## HUD Watering Minigioco — MG-04

```
╔════════════════════════════════════════════════════════════════════╗
║                          💧 WATERING HUD                           ║
╠════════════════════════════════════════════════════════════════════╣
║ 🌿 PIANTA: Night-Bloom Iris (PLT-PURE-002)    STADIO: Flowering   ║
║ pH Dome: +42 (Stable Basic)   Umidità attuale: 52%                ║
╠════════════════════════════════════════════════════════════════════╣
║            ╔═════════════════════════════════════╗                 ║
║            ║   💦 BAR IDRATAZIONE DINAMICA      ║                 ║
║            ║  Dry ░░░░░░░🟩 OPTIMAL 🟩░░░░░ Wet  ║                 ║
║            ║                  ▲                  ║                 ║
║            ║           Idratazione = 52%         ║                 ║
║            ╚═════════════════════════════════════╝                 ║
║                                                                    ║
║ 💧 Dosatore: [⬅] 0.1u  [⬆] 0.5u  [➡] 1.0u                         ║
║ Azione: tieni premuto [SPACE] per versare                          ║
╠════════════════════════════════════════════════════════════════════╣
║ 📊 RANGE SPECIE/STADIO: 48–56% (Flowering)                         ║
║ 🔹 Dentro range → "Soil Balanced"                                  ║
║ 🔸 Sotto 48% → "Soil Dry" (Stress +1 / pH +5)                      ║
║ 🔺 Sopra 56% → "Soil Saturated" (Muffa ↑ / pH –5)                  ║
╠════════════════════════════════════════════════════════════════════╣
║ [Z] Interrompi   [R] Reset   [SPACE] Versa   [ENTER] Conferma      ║
╚════════════════════════════════════════════════════════════════════╝
```

---

# DIARIO SP.O.R.E. — Esempi

## Mockup Diario

```
+------------------------------------------------------+
|               Day 4 — SPORAE Diary                   |
+------------------------------------------------------+

[ Biologist Report ]  (scientific / objective)
--------------------------------------------------------
- 3 Actions used
- 2 Plants watered
- 1 Fruit harvested
- 1 SDE-001 created

--------------------------------------------------------

[ Narrating Voice ]  (poetic / glitch)
--------------------------------------------------------
"...the roots drank more than I remember.
Or is it me who remembers nothing?"

--------------------------------------------------------

                 [ Go to Sleep ]
--------------------------------------------------------

(Secondary Note:)
"New Knowledge Unlocked: 2 items → check Wiki"
```

---

## Estratto Fine Atto I

> [Entry 1.6 — Segni di vita?]
>
> Bussano alla porta. Non so se sono vivi, o solo ombre affamate che imitano la vita.
>
> Chiedono semi, frutti, cose che brillano come miracoli da vendere al buio.
>
> Ho incontrato un uomo che si fa chiamare Mercante. Non gli interessa se respiro ancora, gli interessa se le mie piante respirano.
>
> Gli ho dato un frutto. Mi ha sorriso come si sorride a un cane che impara un trucco.
>
> Poi mi ha chiesto di creare qualcosa che brillasse. E io l'ho fatto. Un cetriolo d'oro.
>
> Rideva. Diceva che la gente là fuori ci dormirà accanto, pur di illudersi che la luce non sia morta.
>
> Io non ridevo. Io piantavo, annaffiava, correggevo il pH finché il respiro della serra non si fermava su un punto neutro.
>
> Alla fine, ho consegnato il cetriolo. Lui lo ha preso, e con lui il mondo intero.
>
> Io ho solo un nuovo ramo nel mio diario: un Albero di Ricerca che non so se cresce verso il cielo o verso l'abisso.
>
> [glitch statico]
>
> …forse questa non è sopravvivenza. Forse è soltanto un altro esperimento.

---

## Estratto Fine Atto II

> [Entry 2.7 — Radici di Ferro]
>
> Hanno bussato ancora. Non erano affamati, non erano disperati. Erano soldati.
>
> Dicevano di aver pestato un contrabbandiere. Dalla sua bocca hanno estratto il mio nome.
>
> Cinque notti di ricerca. Macro-botanica. Fibre. Habitat. Blue Sedge.
>
> Ho pagato caro. Ho scambiato spore come ossa. Ho cucito semi da reliquie.
>
> Le ho fatte crescere, le ho piegate, le ho spezzate.
>
> Biomassa. Scarti. Pelle vegetale data in pasto a un mercato che non fa domande.
>
> I soldati hanno sorriso. "Con queste uniformi marciremo più lentamente."
>
> Non so se ho dato loro protezione o un nuovo modo di uccidere.
>
> [glitch statico]
>
> Forse il seme che cresce in me non è botanico. Forse è un seme di guerra.

---

# NOTE LORE PIANTE — Esempi

## Arctic Hask (PURE-001)

> "Cresce solo dove il gelo ha memoria.
> Resiste a tutto, tranne al calore umano.
> Era la pianta simbolo delle serre polari prima del collasso.
> Oggi vive solo qui, nelle Dome, in un mondo dove il gelo è un lusso e la neve una leggenda."

📓 *Appunto del biologo:*
*Ho provato il suo estratto. Il giorno dopo, non ricordavo più cosa mi avesse spinto a farlo. La pianta era più grande. Io… più calmo. O forse solo più vuoto.*

---

## Glasscap Fungus (EVIL-001)

> "È il fungo che sognava di diventare vetro.
> Ora guarda il mondo attraverso se stesso,
> ma non ricorda più se sta crescendo o marcendo."

📓 *Appunto del Biologo:*
*Ho inalato le sue spore. Per un istante ho visto tutte le Dome del Vault respirare insieme. Poi, solo silenzio. Forse era solo il mio respiro che rallentava.*

---

## Ferric Fern (STD-001)

> "Cresce dove nulla dovrebbe crescere: tra ruggine, ossa e silenzio.
> Purifica il terreno, come se cercasse di cancellare qualcosa che non dovrebbe esserci.
> Si dice che le prime siano nate nei corridoi dei reattori spenti.
> Da allora, chi le coltiva a lungo comincia a parlare più piano, come se avesse paura di disturbare l'aria."

📓 *Appunto del biologo:*
*Ho notato che assorbe perfino il ferro del sangue se la si maneggia troppo. Alcuni la usavano come cura… altri come punizione.*

---

# FINE PARTE 2

**Documenti collegati:**
- SPORIUM_GDD_UNIFICATO_Part1.md

**Data creazione:** 08/10/2025  
**Versione:** 1.0 Beta  
**Status:** Completo

---

## 📝 Note per il Team di Sviluppo

Questo GDD unificato contiene tutte le specifiche di design per SPORIUM Build Beta.

**Priorità implementazione:**
1. Dome 2.0 (vasi attivi + slot passivi)
2. Sistema pH globale
3. Minigiochi Lab (MG-01, 02, 03, 07)
4. Food Room + Slot Idrico
5. Visitor Room + Sistema Fazioni
6. Night Summary + Diario SP.O.R.E.
7. Seed Storage + Deterioramento
8. Piante Base (3 Standard + 3 Pure + 3 Evil)
9. Sistema Mutazioni
10. Atti Narrativi I-III

**Task BLK aperti:** vedi riferimenti specifici nelle sezioni ambientali.

---

