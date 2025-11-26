# 🧪 MAPPA INTERAZIONI SISTEMA pH CON ALTRI SISTEMI
## Analisi Completa GDD v.39 - Visione Generale

**Versione:** 1.0  
**Data Creazione:** 2025-01-XX  
**Basato su:** GDD v.39 del 04/11/2025  
**Scopo:** Documentare tutte le interazioni del sistema pH con gli altri sistemi del gioco

---

## 📋 INDICE INTERAZIONI

1. [pH ↔ Piante](#1-ph--piante)
2. [pH ↔ Azioni](#2-ph--azioni)
3. [pH ↔ Eventi](#3-ph--eventi)
4. [pH ↔ Mutazioni](#4-ph--mutazioni)
5. [pH ↔ Reputazione Fazioni](#5-ph--reputazione-fazioni)
6. [pH ↔ Missioni](#6-ph--missioni)
7. [pH ↔ Economia](#7-ph--economia)
8. [pH ↔ Stati Piante](#8-ph--stati-piante)
9. [pH ↔ UI/HUD](#9-ph--uihud)
10. [pH ↔ Night Summary/Forecast](#10-ph--night-summaryforecast)
11. [pH ↔ Night Research](#11-ph--night-research)
12. [pH ↔ Food Room](#12-ph--food-room)
13. [pH ↔ Seed Storage](#13-ph--seed-storage)
14. [pH ↔ Visitor Room](#14-ph--visitor-room)
15. [pH ↔ Laboratorio Botanico](#15-ph--laboratorio-botanico)

---

## 1. pH ↔ Piante

### **Drift Giornaliero per Famiglia**
- **PURE**: +2 pH/giorno (spingono verso basico)
- **EVIL**: -2 pH/giorno (spingono verso acido)
- **STANDARD**: ±1 pH/giorno (variabile)
- **HYBRID**: drift combinato in base alla base genetica

### **Affinità pH per Famiglia**
- **PURE**: preferiscono pH **Stable Basic** (+50 a +100)
  - In Ultra Acid: **Collapsing** (pianta muore)
  - In Stable Acid: **Weakening** (crescita rallentata)
  - In Neutrale: **Stable** (normale)
  - In Stable Basic: **Thriving** (crescita accelerata)
  
- **EVIL**: preferiscono pH **Stable Acid** (-50 a -100)
  - In Ultra Basic: **Collapsing** (pianta muore)
  - In Stable Basic: **Weakening** (crescita rallentata)
  - In Neutrale: **Stable** (normale)
  - In Stable Acid: **Thriving** (crescita accelerata)

- **STANDARD**: tollerano range più ampio (±30)
  - Meno sensibili alle variazioni estreme

### **Slot Passivi**
- Piante Lvl 5 in slot passivi possono modificare drift pH globale
- Alcune piante passive hanno bonus "Global pH influence" (+1 Basic/day con cap)

---

## 2. pH ↔ Azioni

### **AZ-11 — Watering (Irrigazione)**
- **Overwatering** (saturazione terreno):
  - **pH -5** immediato
  - Rischio muffe ↑
  - Reputazione: Custodi -1, Culto +1
  
- **Underwatering** (carenza prolungata):
  - Nessun drift diretto pH
  - Crescita bloccata
  - Possibile mutazione idrica

- **Optimal Watering**:
  - Nessun drift pH
  - Crescita +1 giorno
  - Reputazione: Custodi +1/settimana se gestione pulita

### **AZ-12 — LED Giornaliero**
- **LED Blu** (uso corretto diurno):
  - **pH +5** (basico)
  - Accelera transizione Growth → Flowering
  
- **LED Rosso** (uso corretto diurno):
  - **pH -5** (acido)
  - Accelera transizione Flowering → HarvestReady

- **LED lasciato acceso di notte**:
  - **pH -5** (acido)
  - **Burn Stress ↑**
  - **Consumo CRY** per ora notturna
  - Reputazione: Custodi -1/giorno, Culto +1/giorno

### **AZ-14 — Spray Antifungino**
- **pH +5** (basico)
- Rimuove muffe (Mild/Severe)
- Non ripristina livelli persi

### **AZ-13 — Pruning (Potatura)**
- Nessun drift diretto pH
- Può prevenire muffe (che causano drift acido)
- Well-Pruned: -10 MutationScore
- Poorly-Pruned: +10 MutationScore

---

## 3. pH ↔ Eventi

### **Eventi che Modificano pH**
- **EVT-002 — Pioggia Acida**: pH -10 per 5 giorni
- **EVT-019 — Drift pH**: ±20-40 punti (evento critico)
- **EVT-005 — Visitatore Affamato**: nessun drift diretto, ma scelte influenzano reputazione

### **Eventi Influenzati da pH**
- **EVT-018 — Muffa Improvvisa**: più probabile in pH acido
- **EVT-012 — Reazione a Catena**: conflitto tra piante incompatibili (Pure vs Evil in pH estremo)

### **VO Challenges legate a pH**
- **VO-CH-04**: "Ordine perfetto" — mantenere pH tra -5 e +5 per 5 giorni
- **VO-CH-08**: "Trova l'equilibrio perfetto" — mantenere 2 Pure + 2 Evil per 10 giorni
- **VO-CH-11**: "Equilibrio instabile" — mantenere pH tra -2 e +2 per 7 giorni

---

## 4. pH ↔ Mutazioni

### **Polarità Mutazioni Determinata da pH**
- **pH Acido** (Stable/Ultra Acid):
  - Bias verso **Mutazioni Corrotte**
  - MutationScore +10/+20 per pH mismatch
  
- **pH Basico** (Stable/Ultra Basic):
  - Bias verso **Mutazioni Armoniche**
  - MutationScore +10/+20 per pH mismatch
  
- **pH Neutrale** (-10 a +10):
  - Bias verso **Mutazioni Adattive**
  - MutationScore 0 per pH match

### **MutationScore Calcolo**
- **pH mismatch**: Neutral/Stable/Ultra → 0/+10/+20
- **Idratazione fuori banda**: +5/giorno (cap +20)
- **LED abuse**: +10 (+5 extra se ripetuto)
- **Muffa**: +15 (Mild) / +30 (Severe)
- **Concimi & Pruning**: Sacro +10 Armoniche, Proibito +10 Corrotte

### **Effetti Mutazioni su pH**
- **Mutazioni Corrotte**: drift pH extra negativo, muffe ↑
- **Mutazioni Armoniche**: drift pH più controllato, muffe ↓
- **Mutazioni Adattive**: compromessi variabili

---

## 5. pH ↔ Reputazione Fazioni

### **Drift Naturale Reputazione**
- **Custodi**: +2/giorno se pH basico, -2/giorno se pH acido
- **Culto della Muffa**: +2/giorno se pH acido, -2/giorno se pH basico
- **Impossibilità di pareggio**: se Custodi sale, Culto scende proporzionalmente

### **Vendita Piante**
- **Piante PURE vendute a Custodi**:
  - Prezzo base × (1 + Bonus Reputazione)
  - Bonus +30% se reputazione alta
  - pH basico aumenta disponibilità offerte
  
- **Piante EVIL vendute a Culto**:
  - Prezzo base × (1 + Bonus Reputazione)
  - Bonus +30% se reputazione alta
  - pH acido aumenta disponibilità offerte

### **Azioni che Influenzano Reputazione via pH**
- **LED lasciato acceso di notte**: Custodi -1/giorno, Culto +1/giorno
- **Overwatering ripetuto**: Custodi -1, Culto +1
- **Gestione equilibrata**: Custodi +1/settimana

### **Black Market**
- Ogni uso: -2 reputazione globale (tutte le fazioni)
- Non influenzato direttamente da pH, ma isolamento progressivo

---

## 6. pH ↔ Missioni

### **Requisiti pH per Missioni**
- **Atto III**: richiede pH estremo (≥±70) + almeno 1 pianta PURE/EVIL/IBRIDA Lv3+
- Alcune Side Quest richiedono pH specifici per completamento

### **Effetti Missioni su pH**
- Missioni Custodi possono richiedere stabilizzazione pH basico
- Missioni Culto possono richiedere acidificazione pH
- Completamento missioni può modificare drift pH temporaneo

### **Missioni Ipnotiche**
- Trigger: consumo frutto ipnotico (chance crescente con livello pianta)
- Stato Ipnotico: 1-3 giorni, altera temporaneamente pH e percezione

---

## 7. pH ↔ Economia

### **Prezzi Vendita Influenzati da pH**
- **Piante PURE vendute a Custodi**:
  - Prezzo base × (1 + Bonus Reputazione) × Qualità
  - Reputazione alta (+75%) → +10% extra valore
  - pH basico aumenta probabilità offerte migliori

- **Piante EVIL vendute a Culto**:
  - Prezzo base × (1 + Bonus Reputazione) × Qualità
  - Reputazione alta (+75%) → +10% extra valore
  - pH acido aumenta probabilità offerte migliori

### **Costi CRY Influenzati da pH**
- Nessun costo diretto CRY per pH
- Ma pH estremi possono causare:
  - Perdita piante (riduzione produzione frutti)
  - Necessità di interventi correttivi (Spray Antifungino, LED, ecc.)

---

## 8. pH ↔ Stati Piante

### **Bande pH e Stati Piante**
- **Ultra Acid** (-100 a -80):
  - PURE: **Collapsing** (muoiono)
  - EVIL: **Thriving** (crescita accelerata)
  
- **Stable Acid** (-80 a -50):
  - PURE: **Weakening** (crescita rallentata)
  - EVIL: **Thriving** (crescita accelerata)
  
- **Neutrale** (-10 a +10):
  - Tutte: **Stable** (crescita normale)
  
- **Stable Basic** (+50 a +80):
  - PURE: **Thriving** (crescita accelerata)
  - EVIL: **Weakening** (crescita rallentata)
  
- **Ultra Basic** (+80 a +100):
  - PURE: **Thriving** (crescita accelerata)
  - EVIL: **Collapsing** (muoiono)

### **Effetti Stati su Gameplay**
- **Thriving**: crescita +50%, resa frutti +20%
- **Stable**: crescita normale
- **Weakening**: crescita -30%, resa frutti -20%
- **Collapsing**: pianta muore in 2-3 giorni se non corretta

---

## 9. pH ↔ UI/HUD

### **Toast HUD System**
- **PH-003 — Danger**: "Dome pH in {band}: {affinity} plants collapsing"
- **Warning**: drift pH significativo (±10+)
- **Info**: cambiamenti pH minori

### **Plant HUD**
- Mostra banda pH corrente
- Indica stato pianta (Thriving/Weakening/Collapsing) basato su pH
- Tooltip con affinità pH della pianta

### **Dome HUD**
- Barra pH globale visibile sempre
- Colori indicativi:
  - Rosso: Ultra Acid
  - Arancione: Stable Acid
  - Verde: Neutrale
  - Azzurro: Stable Basic
  - Blu: Ultra Basic

---

## 10. pH ↔ Night Summary/Forecast

### **Forecast pH**
- Mostra **drift pH atteso** per il giorno successivo
- Formato: "Forecast pH Change: -12 (Acid Drift) ±2"
- Calcolato da:
  - Piante attive (drift giornaliero)
  - Azioni previste (LED, Watering)
  - Eventi probabili

### **Night Summary**
- Registra **pH drift effettivo** del giorno
- Confronto con forecast (reconciliation)
- Log nel Diario SPORAE

### **Archive**
- Conserva storico pH per ogni giorno
- Mostra differenze tra forecast e realtà
- Utile per pattern recognition

---

## 11. pH ↔ Night Research

### **Vault Protocols Research**
- **Bilanciamento pH** accelera ricerca Vault Protocols
- Boost: +25% progress se pH bilanciato (±5)
- Sblocca conoscenze su sistemi Dome e protocolli

### **Altri Percorsi**
- **Historical Archive**: non influenzato da pH
- **Botanical Database**: non influenzato direttamente da pH, ma mutazioni sì

---

## 12. pH ↔ Food Room

### **Nessuna Interazione Diretta**
- Food Room produce cibo sintetico e acqua potabile
- Non modifica direttamente pH Dome
- Ma:
  - Cibo sintetico può dare bonus azioni (più azioni = più controllo pH)
  - Acqua potabile mantiene biologo idratato (nessun malus azioni)

---

## 13. pH ↔ Seed Storage

### **Nessuna Interazione Diretta**
- Seed Storage preserva oggetti organici
- Non modifica pH Dome
- Ma:
  - Conservazione semi/piante permette pianificazione pH futura
  - Piante conservate possono essere usate per bilanciare pH quando necessario

---

## 14. pH ↔ Visitor Room

### **Disponibilità Fazioni**
- **pH basico** aumenta probabilità visite Custodi
- **pH acido** aumenta probabilità visite Culto
- **pH neutrale** aumenta probabilità visite Mercanti Ombra

### **Prezzi Scambi**
- Reputazione influenzata da pH modifica prezzi
- Offerte migliori se pH allineato con fazione

### **CALL System**
- Fazioni rispondono meglio se pH favorevole
- Rifiuti più probabili se pH opposto

---

## 15. pH ↔ Laboratorio Botanico

### **Nessuna Modifica Diretta pH**
- Minigiochi (Microscopio, Pipetta, Catalizzatore) non modificano pH
- Ma:
  - **Seed Stability** influenzata da precisione minigiochi
  - Semi instabili possono generare piante con drift pH imprevedibile
  - Mutazioni da Lab Check influenzate da pH Dome

### **Compost (LAB-CMP-001)**
- Trasforma prodotti pianta in fertilizzanti
- Fertilizzanti applicati possono modificare drift pH (seguendo regole famiglia)

---

## 🎯 CONCLUSIONI

### **pH come Sistema Centrale**
Il pH è il **metronomo** di tutto il gioco. Ogni sistema converge sul pH:
- **Piante** modificano pH e sono modificate da pH
- **Azioni** modificano pH e sono influenzate da pH
- **Eventi** modificano pH e sono triggerati da pH
- **Mutazioni** sono determinate da pH
- **Reputazione** è influenzata da pH
- **Missioni** richiedono pH specifici
- **Economia** è modificata da pH via reputazione
- **UI/HUD** mostra e avvisa su pH
- **Forecast** predice pH futuro
- **Research** è accelerata da pH bilanciato

### **Design Philosophy**
> "pH acts as a hidden dialogue between systems — influencing plant behavior, mission outcomes, reputation drift, and even the tone of the SPORAE"

Il pH non è solo un valore numerico, ma un **indicatore morale e narrativo** che:
- Riflette le scelte del giocatore
- Influenza la percezione delle fazioni
- Determina il tono della narrazione
- Guida la progressione verso l'Atto V

---

## 📚 RIFERIMENTI GDD

- **Sezione 1**: Visione & Concept — pH come pilastro gameplay
- **Sezione 3**: Le PIANTE — regole pH drift e affinità
- **Sezione 5**: LOOPS & Economia — costi e azioni
- **Sezione 6**: AZIONI — modifiche pH da azioni
- **Sezione 8**: Azioni Giornaliere — bonus e incrementi
- **Sezione 9**: Eventi Random — eventi pH
- **Sezione 10**: Missioni — requisiti pH
- **Sezione 11**: Fazioni & Reputazione — drift reputazione da pH
- **Sezione 12**: Toast HUD — notifiche pH
- **MUTAZIONI**: polarità determinata da pH

---

**Fine Documento**

