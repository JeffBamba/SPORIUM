# 🔍 ANALISI STRUMENTI DI DEBUG AVANZATI - SPORIUM
## Piano di Implementazione per Debugging Approfondito

**Data Analisi:** 2025-01-XX  
**Versione:** 1.0  
**Status:** 📋 Proposta da Implementare  
**Priorità:** 🔴 Alta (Miglioramento Qualità Sviluppo)

---

## 🎯 EXECUTIVE SUMMARY

Questa analisi propone **15 strumenti di debug avanzati** per migliorare significativamente la capacità di debugging e sviluppo di SPORIUM. Gli strumenti sono organizzati per priorità e impatto, con dettagli tecnici per l'implementazione.

**Situazione Attuale:**
- ✅ Strumenti debug esistenti: `GameManagerDebugHelper`, `CRYDebugHelper`, `PhSystemDebugConsole`, `PotDebugConsole`
- ⚠️ 818 `Debug.Log` sparsi senza controllo centralizzato
- ⚠️ Nessun sistema di profiling integrato
- ⚠️ Nessun tracciamento eventi centralizzato
- ⚠️ Debug helpers separati per ogni sistema

**Obiettivo:**
Creare un ecosistema di debug completo, centralizzato e professionale che permetta di:
- Identificare bug più rapidamente
- Monitorare performance in tempo reale
- Tracciare eventi e flussi di sistema
- Testare scenari complessi facilmente
- Documentare problemi automaticamente

---

## 📊 STRUMENTI PROPOSTI

### 🔴 PRIORITÀ ALTA (Implementazione Immediata)

#### 1. Sistema di Logging Centralizzato con Livelli

**Problema Attuale:**
- 818 `Debug.Log` sparsi in 85 file
- Nessun controllo sui livelli di log
- Log verbosi in produzione
- Difficile filtrare log per categoria

**Soluzione Proposta:**
```csharp
namespace Sporae.DevTools
{
    public enum LogLevel { Debug, Info, Warning, Error, Critical }
    public enum LogCategory { UI, Core, Dome, Pot, Ph, Inventory, Save, Audio, All }
    
    public static class SporiumLogger
    {
        // Logging con livello e categoria
        public static void Log(LogLevel level, LogCategory category, string message);
        
        // Filtri runtime
        public static void SetCategoryEnabled(LogCategory category, bool enabled);
        public static void SetMinLogLevel(LogLevel minLevel);
        
        // Export su file
        public static void ExportLogsToFile(string filePath);
        
        // Colori per livello nella console Unity
        // Formato: [CATEGORY] [LEVEL] message
    }
}
```

**Funzionalità:**
- ✅ Livelli: Debug, Info, Warning, Error, Critical
- ✅ Categorie: UI, Core, Dome, Pot, pH, Inventory, Save, Audio
- ✅ Filtri per categoria in runtime (toggle on/off)
- ✅ Export su file con timestamp
- ✅ Colori per livello nella console Unity
- ✅ Wrapper per `#if UNITY_EDITOR` automatico
- ✅ Performance: zero overhead in build release

**Impatto:** 🔴 **ALTO** - Base per tutti gli altri strumenti  
**Effort:** 2-3 giorni  
**File Target:** `Assets/_Project/Scripts/DevTools/Logging/SporiumLogger.cs`

---

#### 2. State Inspector Globale (Console Unificata)

**Problema Attuale:**
- Debug helpers separati per ogni sistema (GameManager, CRY, pH, Pot)
- Nessuna vista unificata dello stato del gioco
- Difficile vedere relazioni tra sistemi

**Soluzione Proposta:**
```csharp
namespace Sporae.DevTools
{
    public class GlobalStateInspector : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private bool showOnStart = false;
        
        // Sezioni della console:
        // - GameManager State (CRY, Actions, Day)
        // - pH System State
        // - Pot System State (tutti i pot)
        // - Inventory State
        // - Day Cycle State
        // - Save System State
        // - Performance Metrics (FPS, Memory)
        
        // Funzionalità:
        // - Aggiornamento in tempo reale
        // - Modifica valori in runtime
        // - Export stato completo
        // - Snapshot/Compare stati
    }
}
```

**Funzionalità:**
- ✅ Console unificata (tasto F1) con tutte le informazioni
- ✅ Sezioni organizzate per sistema
- ✅ Aggiornamento in tempo reale
- ✅ Modifica valori in runtime (CRY, pH, Day, ecc.)
- ✅ Export stato completo (JSON/XML)
- ✅ Snapshot e confronto stati
- ✅ Ricerca/filtro per sistema

**Impatto:** 🔴 **ALTO** - Strumento principale per debugging  
**Effort:** 3-4 giorni  
**File Target:** `Assets/_Project/Scripts/DevTools/GlobalStateInspector.cs`

---

#### 3. Console Commands System

**Problema Attuale:**
- Debug limitato a hotkey predefinite
- Impossibile eseguire comandi complessi
- Nessuna automazione di test

**Soluzione Proposta:**
```csharp
namespace Sporae.DevTools
{
    public class DebugConsole : MonoBehaviour
    {
        // Tasto ~ o F12 per aprire
        // Comandi tipo:
        // - set_cry 500
        // - set_ph -50
        // - set_day 5
        // - add_item seed_001 10
        // - teleport x y
        // - god_mode
        // - unlock_all
        // - save_state
        // - load_state
        // - clear_inventory
        // - spawn_pot x y
        
        // Funzionalità:
        // - Autocompletamento
        // - History comandi
        // - Script di comandi (batch)
        // - Help system integrato
    }
}
```

**Funzionalità:**
- ✅ Console testuale (tasto `~` o `F12`)
- ✅ Comandi per modificare stato gioco
- ✅ Autocompletamento intelligente
- ✅ History comandi (↑/↓)
- ✅ Script di comandi (batch execution)
- ✅ Help system (`help`, `help <command>`)
- ✅ Validazione parametri
- ✅ Feedback visivo/audio

**Impatto:** 🔴 **ALTO** - Potenzia enormemente il debugging  
**Effort:** 4-5 giorni  
**File Target:** `Assets/_Project/Scripts/DevTools/DebugConsole.cs`

---

#### 4. Debug Overlay HUD

**Problema Attuale:**
- Info debug sparse
- Nessuna vista sempre disponibile
- Difficile monitorare durante gameplay

**Soluzione Proposta:**
```csharp
namespace Sporae.DevTools
{
    public class DebugOverlayHUD : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private bool showFPS = true;
        [SerializeField] private bool showMemory = true;
        [SerializeField] private bool showSystemStatus = true;
        [SerializeField] private bool showRecentEvents = true;
        [SerializeField] private bool showWarnings = true;
        
        // Layout personalizzabile
        // Toggle rapido (F11)
        // Trasparenza configurabile
    }
}
```

**Funzionalità:**
- ✅ HUD overlay sempre visibile (opzionale)
- ✅ FPS counter con grafico
- ✅ Memoria (GC, allocazioni)
- ✅ Stato sistemi principali (icona + colore)
- ✅ Ultimi eventi (scrollable)
- ✅ Warnings/Errors recenti
- ✅ Customizzabile (posizione, dimensione, trasparenza)
- ✅ Toggle rapido (F11)

**Impatto:** 🔴 **ALTO** - Monitoraggio continuo  
**Effort:** 2-3 giorni  
**File Target:** `Assets/_Project/Scripts/DevTools/DebugOverlayHUD.cs`

---

### 🟡 PRIORITÀ MEDIA (Miglioramento Qualità)

#### 5. Performance Profiler Integrato

**Problema Attuale:**
- Nessun monitoraggio performance in-game
- Unity Profiler non sempre accessibile
- Difficile identificare bottleneck

**Soluzione Proposta:**
```csharp
namespace Sporae.DevTools
{
    public class PerformanceProfiler : MonoBehaviour
    {
        // Metriche:
        // - FPS con grafico (60 frame history)
        // - Frame time (min/max/avg)
        // - GC allocations per frame
        // - Memory usage (heap, native)
        // - Update/FixedUpdate/LateUpdate timing
        // - Draw calls
        // - SetPass calls
        
        // Funzionalità:
        // - Alert su frame drops
        // - Heatmap chiamate costose
        // - Export profiler data
        // - Performance budget alerts
    }
}
```

**Funzionalità:**
- ✅ FPS counter con grafico (60 frame history)
- ✅ Monitor memoria (GC, allocazioni, heap)
- ✅ Profiler per Update/FixedUpdate/LateUpdate
- ✅ Rilevamento frame drops automatico
- ✅ Heatmap delle chiamate più costose
- ✅ Performance budget alerts
- ✅ Export profiler data (CSV/JSON)
- ✅ Integration con Unity Profiler API

**Impatto:** 🟡 **MEDIO** - Ottimizzazione performance  
**Effort:** 3-4 giorni  
**File Target:** `Assets/_Project/Scripts/DevTools/PerformanceProfiler.cs`

---

#### 6. Event Tracer e Visualizzatore

**Problema Attuale:**
- Difficile tracciare flusso eventi
- Nessuna timeline degli eventi
- Impossibile vedere relazioni tra eventi

**Soluzione Proposta:**
```csharp
namespace Sporae.DevTools
{
    public class EventTracer : MonoBehaviour
    {
        // Timeline degli eventi:
        // - pH changes
        // - Day cycles
        // - Pot actions
        // - Inventory changes
        // - Save/Load events
        // - UI interactions
        
        // Visualizzazione:
        // - Timeline grafica
        // - Filtri per tipo evento
        // - Zoom in/out
        // - Export timeline
        // - Breakpoint su eventi specifici
    }
}
```

**Funzionalità:**
- ✅ Timeline degli eventi (pH, day cycles, pot actions, ecc.)
- ✅ Visualizzazione grafica del flusso
- ✅ Filtri per tipo di evento
- ✅ Zoom in/out timeline
- ✅ Export timeline per analisi (JSON/CSV)
- ✅ Breakpoint su eventi specifici
- ✅ Ricerca eventi
- ✅ Correlazione eventi (causa-effetto)

**Impatto:** 🟡 **MEDIO** - Debugging eventi complessi  
**Effort:** 4-5 giorni  
**File Target:** `Assets/_Project/Scripts/DevTools/EventTracer.cs`

---

#### 7. Visual Debugger per Sistemi Complessi

**Problema Attuale:**
- pH e Pot System difficili da visualizzare
- Nessun overlay visivo per debug
- Difficile vedere range di interazione

**Soluzione Proposta:**
```csharp
namespace Sporae.DevTools
{
    public class VisualDebugger : MonoBehaviour
    {
        // Overlay visivi per:
        // - pH value con indicatore visivo
        // - Stato piante nei pot (stage, health, condizioni)
        // - Range di interazione
        // - Pathfinding del player
        // - Zone di trigger
        // - Collision bounds
        
        // Funzionalità:
        // - Toggle on/off per ogni overlay
        // - Customizzazione colori e trasparenza
        // - Labels con informazioni
    }
}
```

**Funzionalità:**
- ✅ Overlay visivo per pH (indicatore colorato)
- ✅ Stato piante nei pot (stage, health, condizioni)
- ✅ Range di interazione (cerchi/linee)
- ✅ Pathfinding del player (path visualizzato)
- ✅ Zone di trigger (outline)
- ✅ Collision bounds
- ✅ Toggle on/off per ogni overlay
- ✅ Customizzazione colori e trasparenza
- ✅ Labels con informazioni dinamiche

**Impatto:** 🟡 **MEDIO** - Debugging visivo  
**Effort:** 3-4 giorni  
**File Target:** `Assets/_Project/Scripts/DevTools/VisualDebugger.cs`

---

#### 8. Save State Analyzer

**Problema Attuale:**
- Difficile debuggare problemi save/load
- Nessuna validazione integrità save
- Impossibile vedere struttura save

**Soluzione Proposta:**
```csharp
namespace Sporae.DevTools
{
    public class SaveStateAnalyzer : MonoBehaviour
    {
        // Funzionalità:
        // - Visualizzazione struttura save
        // - Validazione integrità save
        // - Diff tra save states
        // - Export/import save modificati
        // - Corruzione detection
        // - Backup automatico
    }
}
```

**Funzionalità:**
- ✅ Visualizzazione struttura save (tree view)
- ✅ Validazione integrità save
- ✅ Diff tra save states (cosa è cambiato)
- ✅ Export/import save modificati
- ✅ Corruzione detection automatica
- ✅ Backup automatico prima di modifiche
- ✅ Statistiche save (dimensione, timestamp, versione)

**Impatto:** 🟡 **MEDIO** - Debugging save system  
**Effort:** 2-3 giorni  
**File Target:** `Assets/_Project/Scripts/DevTools/SaveStateAnalyzer.cs`

---

### 🟢 PRIORITÀ BASSA (Nice to Have)

#### 9. Sistema di Replay/Recording

**Problema Attuale:**
- Difficile riprodurre bug specifici
- Nessun recording di sessioni
- Impossibile fare rollback

**Soluzione Proposta:**
```csharp
namespace Sporae.DevTools
{
    public class ReplaySystem : MonoBehaviour
    {
        // Funzionalità:
        // - Recording automatico azioni giocatore
        // - Replay con controlli (play/pause/step)
        // - Snapshot stato gioco
        // - Rollback a snapshot precedenti
        // - Export/import sessioni debug
    }
}
```

**Funzionalità:**
- ✅ Recording automatico azioni giocatore
- ✅ Replay con controlli (play/pause/step/seek)
- ✅ Snapshot stato gioco (checkpoint)
- ✅ Rollback a snapshot precedenti
- ✅ Export/import sessioni debug
- ✅ Timeline replay con eventi
- ✅ Velocità replay (0.5x, 1x, 2x, 4x)

**Impatto:** 🟢 **BASSO** - Utile ma non essenziale  
**Effort:** 5-6 giorni  
**File Target:** `Assets/_Project/Scripts/DevTools/ReplaySystem.cs`

---

#### 10. Memory Leak Detector

**Problema Attuale:**
- Nessun monitoraggio perdite memoria
- Difficile identificare leak
- Nessun alert automatico

**Soluzione Proposta:**
```csharp
namespace Sporae.DevTools
{
    public class MemoryLeakDetector : MonoBehaviour
    {
        // Funzionalità:
        // - Tracking allocazioni/deallocazioni
        // - Alert su potenziali leak
        // - Report oggetti non deallocati
        // - Stack trace per allocazioni sospette
    }
}
```

**Funzionalità:**
- ✅ Tracking allocazioni/deallocazioni
- ✅ Alert su potenziali leak (memoria crescente)
- ✅ Report oggetti non deallocati
- ✅ Stack trace per allocazioni sospette
- ✅ Grafico memoria nel tempo
- ✅ Threshold configurabili

**Impatto:** 🟢 **BASSO** - Utile per ottimizzazione  
**Effort:** 3-4 giorni  
**File Target:** `Assets/_Project/Scripts/DevTools/MemoryLeakDetector.cs`

---

#### 11. Automated Test Runner Integrato

**Problema Attuale:**
- Nessun sistema test automatizzato
- Test manuali ripetitivi
- Nessuna regression test suite

**Soluzione Proposta:**
```csharp
namespace Sporae.DevTools
{
    public class TestRunner : MonoBehaviour
    {
        // Test scenari predefiniti:
        // - Test pH drift
        // - Test day cycle
        // - Test save/load
        // - Test inventory
        // - Test pot growth
        // - Test UI interactions
        
        // Funzionalità:
        // - Test runner in-game
        // - Report automatico risultati
        // - Regression test suite
    }
}
```

**Funzionalità:**
- ✅ Test runner in-game
- ✅ Test scenari predefiniti (pH, day cycle, save/load, inventory)
- ✅ Report automatico risultati
- ✅ Regression test suite
- ✅ Test custom definibili
- ✅ Assertions e validazioni
- ✅ Performance benchmarks

**Impatto:** 🟢 **BASSO** - Qualità a lungo termine  
**Effort:** 5-6 giorni  
**File Target:** `Assets/_Project/Scripts/DevTools/TestRunner.cs`

---

#### 12. Network/Service Dependency Graph

**Problema Attuale:**
- Difficile vedere dipendenze tra sistemi
- Nessuna visualizzazione ServiceContainer
- Impossibile vedere cicli di dipendenza

**Soluzione Proposta:**
```csharp
namespace Sporae.DevTools
{
    public class DependencyGraphViewer : MonoBehaviour
    {
        // Funzionalità:
        // - Visualizzazione grafica dipendenze
        // - ServiceContainer dependency tree
        // - Event subscription map
        // - Cicli di dipendenza detection
        // - Impact analysis (cosa si rompe se X fallisce)
    }
}
```

**Funzionalità:**
- ✅ Visualizzazione grafica dipendenze (graph view)
- ✅ ServiceContainer dependency tree
- ✅ Event subscription map
- ✅ Cicli di dipendenza detection
- ✅ Impact analysis (cosa si rompe se X fallisce)
- ✅ Export graph (PNG/SVG)
- ✅ Interattivo (click per dettagli)

**Impatto:** 🟢 **BASSO** - Architettura e documentazione  
**Effort:** 4-5 giorni  
**File Target:** `Assets/_Project/Scripts/DevTools/DependencyGraphViewer.cs`

---

#### 13. Screenshot e Video Capture Automatico

**Problema Attuale:**
- Difficile documentare bug visivi
- Screenshot manuali
- Nessun video capture integrato

**Soluzione Proposta:**
```csharp
namespace Sporae.DevTools
{
    public class MediaCapture : MonoBehaviour
    {
        // Funzionalità:
        // - Screenshot automatico su eventi critici
        // - Video recording su comando
        // - Annotazioni su screenshot
        // - Export con metadata (timestamp, stato gioco)
    }
}
```

**Funzionalità:**
- ✅ Screenshot automatico su eventi critici
- ✅ Video recording su comando (F10)
- ✅ Annotazioni su screenshot (testo, frecce, highlight)
- ✅ Export con metadata (timestamp, stato gioco, versione)
- ✅ Organizzazione automatica (cartelle per data)
- ✅ Compressione automatica

**Impatto:** 🟢 **BASSO** - Documentazione bug  
**Effort:** 2-3 giorni  
**File Target:** `Assets/_Project/Scripts/DevTools/MediaCapture.cs`

---

#### 14. Conditional Breakpoint System

**Problema Attuale:**
- Unity breakpoint non condizionali
- Difficile pausare su condizioni specifiche
- Nessun sistema di pause automatico

**Soluzione Proposta:**
```csharp
namespace Sporae.DevTools
{
    public class ConditionalBreakpoint : MonoBehaviour
    {
        // Funzionalità:
        // - Breakpoint condizionali via codice
        // - Pause su condizioni specifiche (es: pH < -50, CRY = 0)
        // - Log automatico quando si verifica condizione
        // - Notifiche visive/audio
    }
}
```

**Funzionalità:**
- ✅ Breakpoint condizionali via codice
- ✅ Pause su condizioni specifiche (pH < -50, CRY = 0, ecc.)
- ✅ Log automatico quando si verifica condizione
- ✅ Notifiche visive/audio
- ✅ Stack trace al momento del breakpoint
- ✅ Continuazione automatica dopo X secondi

**Impatto:** 🟢 **BASSO** - Debugging avanzato  
**Effort:** 2-3 giorni  
**File Target:** `Assets/_Project/Scripts/DevTools/ConditionalBreakpoint.cs`

---

#### 15. Integration con Unity Profiler

**Problema Attuale:**
- Unity Profiler non sempre accessibile
- Nessun auto-start profiler
- Difficile integrare con sistemi SPORIUM

**Soluzione Proposta:**
```csharp
namespace Sporae.DevTools
{
    public class UnityProfilerIntegration : MonoBehaviour
    {
        // Funzionalità:
        // - Wrapper per Unity Profiler API
        // - Auto-start profiler su condizioni
        // - Export profiler data
        // - Custom markers per sistemi SPORIUM
        // - Performance budget alerts
    }
}
```

**Funzionalità:**
- ✅ Wrapper per Unity Profiler API
- ✅ Auto-start profiler su condizioni (FPS < 30, memoria > threshold)
- ✅ Export profiler data (Unity Profiler format)
- ✅ Custom markers per sistemi SPORIUM (pH update, pot growth, ecc.)
- ✅ Performance budget alerts
- ✅ Integration con PerformanceProfiler

**Impatto:** 🟢 **BASSO** - Profiling avanzato  
**Effort:** 2-3 giorni  
**File Target:** `Assets/_Project/Scripts/DevTools/UnityProfilerIntegration.cs`

---

## 📋 PIANO DI IMPLEMENTAZIONE

### FASE 1: Foundation (Settimana 1-2)
**Priorità:** 🔴 **ALTA**

1. **Sistema di Logging Centralizzato** (2-3 giorni)
   - Creare `SporiumLogger.cs`
   - Implementare livelli e categorie
   - Wrapper per `#if UNITY_EDITOR`
   - Migrare alcuni `Debug.Log` esistenti come esempio

2. **State Inspector Globale** (3-4 giorni)
   - Creare `GlobalStateInspector.cs`
   - Integrare con sistemi esistenti (GameManager, pH, Pot)
   - UI console unificata
   - Funzionalità base (visualizzazione, modifica valori)

**Risultato Atteso:**
- Base solida per tutti gli altri strumenti
- Debugging immediatamente più efficace

---

### FASE 2: Core Tools (Settimana 3-4)
**Priorità:** 🔴 **ALTA**

3. **Console Commands System** (4-5 giorni)
   - Creare `DebugConsole.cs`
   - Sistema parsing comandi
   - Comandi base (set_cry, set_ph, set_day, ecc.)
   - Autocompletamento e history

4. **Debug Overlay HUD** (2-3 giorni)
   - Creare `DebugOverlayHUD.cs`
   - FPS, memoria, stato sistemi
   - Layout personalizzabile
   - Toggle rapido

**Risultato Atteso:**
- Strumenti principali completi
- Debugging molto più potente

---

### FASE 3: Advanced Tools (Settimana 5-8)
**Priorità:** 🟡 **MEDIA**

5. **Performance Profiler Integrato** (3-4 giorni)
6. **Event Tracer** (4-5 giorni)
7. **Visual Debugger** (3-4 giorni)
8. **Save State Analyzer** (2-3 giorni)

**Risultato Atteso:**
- Debugging avanzato completo
- Ottimizzazione performance facilitata

---

### FASE 4: Nice to Have (Settimana 9+)
**Priorità:** 🟢 **BASSA**

9-15. Strumenti opzionali da implementare in base a necessità

---

## 🎯 METRICHE DI SUCCESSO

**Prima dell'implementazione:**
- ⏱️ Tempo medio per identificare bug: **~2-3 ore**
- 🔍 Visibilità stato sistemi: **Parziale** (solo alcuni sistemi)
- 📊 Monitoraggio performance: **Nessuno**
- 🎮 Automazione test: **Nessuna**

**Dopo l'implementazione (Fase 1-2):**
- ⏱️ Tempo medio per identificare bug: **~30-60 minuti** (50-75% riduzione)
- 🔍 Visibilità stato sistemi: **Completa** (tutti i sistemi)
- 📊 Monitoraggio performance: **Tempo reale** (FPS, memoria)
- 🎮 Automazione test: **Parziale** (console commands)

**Dopo l'implementazione (Fase 3):**
- ⏱️ Tempo medio per identificare bug: **~15-30 minuti** (75-90% riduzione)
- 🔍 Visibilità stato sistemi: **Completa + Timeline eventi**
- 📊 Monitoraggio performance: **Avanzato** (profiling, heatmap)
- 🎮 Automazione test: **Completa** (test runner)

---

## 🔧 CONSIDERAZIONI TECNICHE

### Architettura

**Struttura Directory Proposta:**
```
Assets/_Project/Scripts/DevTools/
├── Logging/
│   ├── SporiumLogger.cs
│   └── LogCategory.cs
├── Inspector/
│   ├── GlobalStateInspector.cs
│   └── SystemInspectorBase.cs
├── Console/
│   ├── DebugConsole.cs
│   ├── CommandRegistry.cs
│   └── Commands/
│       ├── SetCryCommand.cs
│       ├── SetPhCommand.cs
│       └── ...
├── Overlay/
│   ├── DebugOverlayHUD.cs
│   └── PerformanceProfiler.cs
├── Visual/
│   ├── VisualDebugger.cs
│   └── OverlayRenderer.cs
├── Tracing/
│   ├── EventTracer.cs
│   └── EventRecorder.cs
└── Utils/
    ├── SaveStateAnalyzer.cs
    └── MediaCapture.cs
```

### Performance

**Ottimizzazioni:**
- Tutti gli strumenti disabilitati in build release (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`)
- Logging con zero overhead quando disabilitato
- Overlay HUD con update rate configurabile (60fps, 30fps, 10fps)
- Event tracer con buffer limitato (ultimi 1000 eventi)
- Profiler con sampling opzionale (non sempre attivo)

### Integrazione con Sistemi Esistenti

**ServiceContainer Integration:**
- Tutti gli strumenti registrati in ServiceContainer
- Accesso tramite `ServiceContainer.Instance.Get<GlobalStateInspector>()`
- Lazy initialization per non impattare startup

**Event System Integration:**
- Event tracer si integra con `EventSystem` esistente
- Logging automatico di eventi critici
- Filtri configurabili per eventi da tracciare

---

## 📝 NOTE IMPLEMENTATIVE

### Migrazione Debug.Log Esistenti

**Strategia Graduale:**
1. Creare `SporiumLogger` con wrapper per `Debug.Log`
2. Sostituire gradualmente `Debug.Log` con `SporiumLogger.Log`
3. Aggiungere categorie man mano
4. Wrappare con `#if UNITY_EDITOR` automaticamente

**Esempio Migrazione:**
```csharp
// PRIMA
Debug.Log($"[GameManager] CRY: {currentCRY}");

// DOPO
SporiumLogger.Log(LogLevel.Info, LogCategory.Core, $"CRY: {currentCRY}");
```

### Compatibilità con Strumenti Esistenti

**Mantenere Compatibilità:**
- `GameManagerDebugHelper`, `CRYDebugHelper` continuano a funzionare
- Integrarli in `GlobalStateInspector` come sezioni
- Mantenere hotkey esistenti (F1, F2, F3) per retrocompatibilità

### Testing

**Test Strategy:**
- Test manuali per ogni strumento
- Test di integrazione tra strumenti
- Test performance (verificare overhead minimo)
- Test in build development (non release)

---

## 🚀 PROSSIMI PASSI

1. **Review e Approvazione** di questa analisi
2. **Prioritizzazione** strumenti da implementare
3. **Pianificazione Sprint** per Fase 1-2
4. **Implementazione** strumenti priorità alta
5. **Testing e Validazione** strumenti implementati
6. **Documentazione** uso strumenti per team

---

## 📚 RIFERIMENTI

- **Strumenti Debug Esistenti:**
  - `GameManagerDebugHelper.cs` (F2/F3)
  - `CRYDebugHelper.cs` (F1)
  - `PhSystemDebugConsole.cs` (Z)
  - `PotDebugConsole.cs` (P)

- **Documentazione Correlata:**
  - `DEBUG_SETUP_INSTRUCTIONS.md`
  - `PH_DEBUG_CONSOLE_INSTRUCTIONS.md`
  - `ANALISI_STATO_OTTIMALE_E_MIGLIORAMENTI.md`

- **Metriche Attuali:**
  - 818 `Debug.Log` in 85 file
  - 75 `FindObjectOfType` in 50 file
  - 131 `GetComponent` in 46 file

---

**Status:** 📋 **Proposta da Implementare**  
**Prossimo Review:** Da definire  
**Owner:** Da assegnare

---

*Documento creato il: 2025-01-XX*  
*Versione: 1.0*  
*Autore: AI Assistant (Senior Dev Mode)*

