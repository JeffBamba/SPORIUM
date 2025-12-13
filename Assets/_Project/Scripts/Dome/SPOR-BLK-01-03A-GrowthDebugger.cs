using UnityEngine;
using System.Collections.Generic;
using _Project.Sporae.Core;
using Sporae.DevTools;

/// <summary>
/// Debug script per il sistema di crescita BLK-01.03A.
/// Fornisce comandi per testare e debuggare il sistema di crescita basato su timestamp.
/// </summary>
public class SPOR_BLK_01_03A_GrowthDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool enableHotkeys = true;
    
    [Header("References")]
    [SerializeField] private DayCycleController dayCycleController;
    
    private List<PotStateModel> allPots = new List<PotStateModel>();

    private DayCycleSystem _dayCycleSystem;

    private void Start()
    {
        _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
        
        // Trova il DayCycleController se non assegnato
        if (dayCycleController == null)
        {
            dayCycleController = FindObjectOfType<DayCycleController>();
        }
        
        if (enableDebugLogs)
        {
            SporiumLogger.LogInfo(LogCategory.Dome, "GrowthDebugger inizializzato. Premi F6 per stampare stato vasi.");
        }
    }

    private void Update()
    {
        if (!enableHotkeys) 
            return;

        // F6: Stampa stato di tutti i vasi
        if (Input.GetKeyDown(KeyCode.F6))
            PrintAllPotsStatus();
    }

    /// <summary>
    /// Stampa lo stato di tutti i vasi nel sistema
    /// </summary>
    [ContextMenu("Print All Pots Status")]
    public void PrintAllPotsStatus()
    {
        SporiumLogger.LogInfo(LogCategory.Dome, "=== STATO VASI BLK-01.03A ===");
        
        // Trova tutti i vasi nella scena
        FindAllPots();
        
        if (allPots.Count == 0)
        {
            SporiumLogger.LogWarning(LogCategory.Dome, "Nessun vaso trovato nella scena.");
            return;
        }

        // Stampa stato di ogni vaso
        for (int i = 0; i < allPots.Count; i++)
        {
            var pot = allPots[i];
            if (pot != null)
            {
                PrintPotStatus(pot, i);
            }
        }

        // Stampa informazioni sul sistema di crescita
        PrintGrowthSystemInfo();
        
        SporiumLogger.LogInfo(LogCategory.Dome, "=== FINE STATO VASI ===");
    }

    /// <summary>
    /// Trova tutti i vasi nella scena
    /// </summary>
    private void FindAllPots()
    {
        allPots.Clear();
        
        // Cerca PotSlot e estrai PotStateModel
        PotSlot[] potSlots = FindObjectsOfType<PotSlot>();
        foreach (var potSlot in potSlots)
        {
            if (potSlot.PotActions != null)
            {
                var potState = potSlot.PotActions.GetCurrentState();
                if (potState != null)
                {
                    allPots.Add(potState);
                }
            }
        }
    }

    /// <summary>
    /// Stampa lo stato di un singolo vaso
    /// </summary>
    private void PrintPotStatus(PotStateModel pot, int index)
    {
        string stageName = GetStageName(pot.Stage);
        string threshold = GetStageThreshold(pot.Stage);
        
        SporiumLogger.LogInfo(LogCategory.Pot, $"[{index}] {pot.PotId}:");
        SporiumLogger.LogInfo(LogCategory.Pot, $"  - Stato: {(pot.HasPlant ? "Pianta" : "Vuoto")}");
        
        if (pot.HasPlant)
        {
            SporiumLogger.LogInfo(LogCategory.Pot, $"  - Stadio: {stageName} (Progresso: {pot.GrowthPoints}/{threshold})");
            SporiumLogger.LogInfo(LogCategory.Pot, $"  - Giorni dalla semina: {pot.DaysSincePlant}");
            SporiumLogger.LogInfo(LogCategory.Pot, $"  - Giorni negligenza: {pot.DaysNeglectedStreak}");
            SporiumLogger.LogInfo(LogCategory.Pot, $"  - Idratazione: {pot.Hydration}/3");
            SporiumLogger.LogInfo(LogCategory.Pot, $"  - Luce: {pot.LightExposure}/3");
            SporiumLogger.LogInfo(LogCategory.Pot, $"  - Timestamps:");
            SporiumLogger.LogInfo(LogCategory.Pot, $"    * Piantato: Giorno {pot.PlantedDay}");
            SporiumLogger.LogInfo(LogCategory.Pot, $"    * Ultima acqua: Giorno {pot.LastWateredDay}");
            SporiumLogger.LogInfo(LogCategory.Pot, $"    * Ultima luce: Giorno {pot.LastLitDay}");
            SporiumLogger.LogInfo(LogCategory.Pot, $"  - Sistema Irrigazione (GDD AZ-11):");
            SporiumLogger.LogInfo(LogCategory.Pot, $"    * Stato: {(pot.WateringSystemOn ? "ON" : "OFF")}");
            SporiumLogger.LogInfo(LogCategory.Pot, $"    * Giorni ON: {pot.DaysWateringSystemOn}");
            SporiumLogger.LogInfo(LogCategory.Pot, $"    * Accumulatore WAT-RAW: {pot.WateringRawWaterAccumulator:F1}");
        }
        
        SporiumLogger.LogInfo(LogCategory.Pot, "  ---");
    }

    /// <summary>
    /// Stampa informazioni sul sistema di crescita
    /// </summary>
    private void PrintGrowthSystemInfo()
    {
        if (dayCycleController != null)
        {
            var config = dayCycleController.GetGrowthConfig();
            if (config != null)
            {
                SporiumLogger.LogInfo(LogCategory.Dome, "=== CONFIGURAZIONE CRESCITA ===");
                SporiumLogger.LogInfo(LogCategory.Dome, $"  - Seed → Sprout: {config.pointsSeedToSprout} punti");
                SporiumLogger.LogInfo(LogCategory.Dome, $"  - Sprout → Mature: {config.pointsSproutToMature} punti");
                SporiumLogger.LogInfo(LogCategory.Dome, $"  - Cura ideale: {config.pointsIdealCare} punti");
                SporiumLogger.LogInfo(LogCategory.Dome, $"  - Cura parziale: {config.pointsPartialCare} punti");
                SporiumLogger.LogInfo(LogCategory.Dome, $"  - Nessuna cura: {config.pointsNoCare} punti");
                SporiumLogger.LogInfo(LogCategory.Dome, $"  - Decadimento idratazione: {config.dailyHydrationDecay}");
                SporiumLogger.LogInfo(LogCategory.Dome, $"  - Vasi registrati: {dayCycleController.GetRegisteredPotCount()}");
            }
        }
        else
        {
            SporiumLogger.LogWarning(LogCategory.Dome, "DayCycleController non trovato!");
        }
    }

    /// <summary>
    /// Restituisce il nome localizzato per uno stadio
    /// </summary>
    private string GetStageName(int stage)
    {
        switch (stage)
        {
            case 0: return "Seed";
            case 1: return "Sprout";
            case 2: return "Mature";
            default: return $"Stadio {stage}";
        }
    }

    /// <summary>
    /// Restituisce la soglia di punti per lo stadio corrente
    /// </summary>
    private string GetStageThreshold(int stage)
    {
        switch (stage)
        {
            case 0: return "2"; // Seed to Sprout
            case 1: return "3"; // Sprout to Mature
            case 2: return "∞"; // Mature (nessun avanzamento)
            default: return "?";
        }
    }

    /// <summary>
    /// Forza la registrazione di tutti i vasi nel DayCycleController
    /// </summary>
    [ContextMenu("Force Register All Pots")]
    public void ForceRegisterAllPots()
    {
        if (dayCycleController == null)
        {
            SporiumLogger.LogError(LogCategory.Dome, "DayCycleController non trovato!");
            return;
        }

        FindAllPots();
        foreach (var pot in allPots)
        {
            if (pot != null)
            {
                dayCycleController.RegisterPot(pot);
            }
        }

        SporiumLogger.LogInfo(LogCategory.Dome, $"Forzata registrazione di {allPots.Count} vasi nel DayCycleController");
    }

    /// <summary>
    /// Simula un tick di crescita (utile per test)
    /// </summary>
    [ContextMenu("Simulate Growth Tick")]
    public void SimulateGrowthTick()
    {
        if (dayCycleController == null)
        {
            SporiumLogger.LogError(LogCategory.Dome, "DayCycleController non trovato!");
            return;
        }

        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            SporiumLogger.LogInfo(LogCategory.Dome, $"Simulazione tick crescita per giorno {_dayCycleSystem.CurrentDay}");
            // Il DayCycleController si iscrive automaticamente a OnDayChanged
            // quindi questo è solo per debug
        }
        else
        {
            SporiumLogger.LogError(LogCategory.Core, "GameManager non trovato!");
        }
    }
}
