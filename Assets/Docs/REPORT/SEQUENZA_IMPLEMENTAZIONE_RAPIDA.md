# 🎯 SEQUENZA IMPLEMENTAZIONE RAPIDA
## Guida Sintetica per Sviluppo Incrementale

**Versione:** 1.0  
**Uso:** Riferimento rapido per ordine di implementazione

---

## 📊 MAPPA DIPENDENZE VISUALE

```
┌─────────────────────────────────────────────────────────────┐
│                    FASE 0: FOUNDATION                      │
│  ✅ EconomySystem  ✅ ActionSystem  ✅ Inventory             │
│  ✅ Deterioration  ✅ DayCycle      ✅ EventSystem           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│              FASE 1: CORE DOME SYSTEMS (CRITICA)            │
│                                                              │
│  [1.1] pH System ──────────────┐                            │
│         │                      │                            │
│         ▼                      │                            │
│  [1.2] Livelli Piante          │                            │
│         │                      │                            │
│         ▼                      │                            │
│  [1.3] Stadi Completi ─────────┘                            │
│         │                                                  │
│         ▼                                                  │
│  [1.4] Slot Passivi                                         │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│            FASE 2: PLANT SYSTEMS (ALTA)                     │
│                                                              │
│  [2.1] Catalog Piante Base ────┐                            │
│         │                      │                            │
│         ▼                      │                            │
│  [2.2] Mutazioni ──────────────┘                            │
│         │                                                  │
│         ▼                                                  │
│  [2.3] Ibridi                                              │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│       FASE 3: ECONOMIC & SURVIVAL (ALTA)                   │
│                                                              │
│  [3.1] Food Room                                            │
│  [3.2] Seed Storage Completo                                │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│          FASE 4: SOCIAL SYSTEMS (CRITICA)                   │
│                                                              │
│  [4.1] Fazioni/Reputazione ────┐                            │
│         │                      │                            │
│         ▼                      │                            │
│  [4.2] Visitor Room Completo ──┘                            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│         FASE 5: NARRATIVE SYSTEMS (MEDIA)                    │
│                                                              │
│  [5.1] Missioni Completo                                    │
│  [5.2] Atti Narrativi                                       │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│         FASE 6: POLISH & ADVANCED (BASSA)                    │
│                                                              │
│  [6.1] Muffe/Infestazioni                                   │
│  [6.2] Dome Avanzata                                        │
└─────────────────────────────────────────────────────────────┘
```

---

## 🚀 ORDINE IMPLEMENTAZIONE CONSIGLIATO

### PRIORITÀ IMMEDIATA (Sprint 1-2)

**1. BLK-02.01: Sistema pH Globale Dome**
- **Perché:** Blocca tutti gli altri sistemi avanzati
- **Tempo:** 1 settimana
- **Output:** pH globale funzionante, drift giornaliero, UI display

**2. BLK-02.02: Sistema Livelli Piante**
- **Perché:** Necessario per slot passivi
- **Tempo:** 1 settimana
- **Output:** Progressione Lvl 1-5, effetti su resa

**3. BLK-02.03: Sistema Stadi Completi**
- **Perché:** Completa il sistema crescita base
- **Tempo:** 1.5 settimane
- **Output:** 6 stadi funzionanti, frutti multi-giorno

**4. BLK-02.04: Sistema Slot Passivi**
- **Perché:** Completa Dome 2.0
- **Tempo:** 1 settimana
- **Output:** 3 slot passivi, bonus applicati

**✅ Milestone FASE 1:** Dome completamente funzionale con pH, livelli, stadi, slot passivi

---

### PRIORITÀ ALTA (Sprint 3-5)

**5. BLK-03.01: Catalog Piante Base**
- **Perché:** Contenuto botanico necessario per testare tutto
- **Tempo:** 1.5 settimane
- **Output:** 9 piante base (3 Standard, 3 Pure, 3 Evil)

**6. BLK-03.02: Sistema Mutazioni**
- **Perché:** Aggiunge profondità gameplay
- **Tempo:** 2 settimane
- **Output:** 3 mutazioni base, sistema completo

**7. BLK-03.03: Sistema Ibridi**
- **Perché:** Contenuto late-game
- **Tempo:** 2 settimane
- **Output:** 2 ibridi base, sistema creazione funzionante

**✅ Milestone FASE 2:** Sistema botanico completo con varietà piante

---

### PRIORITÀ ALTA (Sprint 6-7)

**8. BLK-04.01: Food Room**
- **Perché:** Sistema sopravvivenza e gestione azioni
- **Tempo:** 1.5 settimane
- **Output:** Produzione cibo/acqua, bonus azioni

**9. BLK-04.02: Seed Storage Completo**
- **Perché:** Completare sistema esistente
- **Tempo:** 0.5 settimane
- **Output:** Costi CRY, espansione slot

**✅ Milestone FASE 3:** Sopravvivenza e economia complete

---

### PRIORITÀ CRITICA (Sprint 8-10)

**10. BLK-05.01: Fazioni/Reputazione**
- **Perché:** Blocca missioni e narrativa
- **Tempo:** 2 settimane
- **Output:** 6 fazioni, sistema reputazione completo

**11. BLK-05.02: Visitor Room Completo**
- **Perché:** Completare sistema sociale
- **Tempo:** 1.5 settimane
- **Output:** CALL, baratto, negoziazione

**✅ Milestone FASE 4:** Sistema sociale completo

---

### PRIORITÀ MEDIA (Sprint 11-13)

**12. BLK-06.01: Missioni Completo**
- **Perché:** Contenuto narrativo principale
- **Tempo:** 2 settimane
- **Output:** Struttura MQ+SQ, missioni GDD

**13. BLK-06.02: Atti Narrativi**
- **Perché:** Contenuto narrativo avanzato
- **Tempo:** 2 settimane
- **Output:** Atto I implementato

**✅ Milestone FASE 5:** Narrativa base funzionante

---

### PRIORITÀ BASSA (Sprint 14+)

**14. BLK-07.01: Muffe/Infestazioni**
- **Perché:** Sistema pressione gestionale
- **Tempo:** 1 settimana

**15. BLK-07.02: Dome Avanzata**
- **Perché:** Contenuto late-game avanzato
- **Tempo:** 2 settimane

---

## ⚡ QUICK START: Primi 3 Task

Se vuoi iniziare subito, ecco i primi 3 task in ordine:

### Task 1: pH System (BLK-02.01)
```csharp
// 1. Creare PhSystem.cs
// 2. Creare PhSystemConfig.cs (ScriptableObject)
// 3. Registrare in ServiceContainer
// 4. Integrare con GameManager.EndDay
// 5. Creare UI display
```

### Task 2: Livelli Piante (BLK-02.02)
```csharp
// 1. Estendere PotStateModel con PlantLevel
// 2. Creare PlantLevelSystem.cs
// 3. Integrare con DayCycleController
// 4. Aggiungere check Lvl 5 per slot passivi
```

### Task 3: Stadi Completi (BLK-02.03)
```csharp
// 1. Estendere PlantStage enum (6 stadi)
// 2. Creare StageTransitionSystem.cs
// 3. Modificare DayCycleController
// 4. Implementare frutti multi-giorno
```

---

## 📋 CHECKLIST AVVIO IMPLEMENTAZIONE

Prima di iniziare un nuovo task:

- [ ] Leggere documentazione GDD relativa
- [ ] Verificare dipendenze completate
- [ ] Creare branch Git: `feature/BLK-XX.XX-description`
- [ ] Creare struttura file secondo piano
- [ ] Implementare sistema core
- [ ] Creare ScriptableObject config
- [ ] Integrare con ServiceContainer
- [ ] Creare UI se necessaria
- [ ] Test manuali con scenari GDD
- [ ] Documentazione README
- [ ] Code review
- [ ] Merge in main

---

## 🎯 CRITERI DI SUCCESSO FASE

### FASE 1 Completata quando:
- ✅ pH globale funziona e influenza crescita
- ✅ Piante progrediscono Lvl 1-5
- ✅ Tutti i 6 stadi funzionano
- ✅ Slot passivi funzionano con bonus

### FASE 2 Completata quando:
- ✅ 9 piante base implementate e testabili
- ✅ Mutazioni si innescano correttamente
- ✅ Ibridi creabili e piantabili

### FASE 3 Completata quando:
- ✅ Food Room produce cibo/acqua
- ✅ Seed Storage completo con costi

### FASE 4 Completata quando:
- ✅ 6 fazioni con reputazione funzionante
- ✅ Visitor Room completo con CALL e baratto

### FASE 5 Completata quando:
- ✅ Missioni MQ+SQ funzionano
- ✅ Atto I implementato

---

## 💡 CONSIGLI PRATICI

1. **Inizia Piccolo:** Implementa prima la versione base, poi aggiungi complessità
2. **Testa Spesso:** Dopo ogni feature, testa manualmente
3. **Documenta Subito:** Non rimandare la documentazione
4. **Usa Config:** Tutti i valori devono essere configurabili via ScriptableObject
5. **Event-Driven:** Usa eventi per comunicazione tra sistemi
6. **Backward Compatible:** Non rompere sistemi esistenti

---

**FINE SEQUENZA RAPIDA**

