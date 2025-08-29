using UnityEngine;
using System.Collections.Generic;

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
    [SerializeField] private SPOR_BLK_01_03A_DayCycleController dayCycleController;
    
    private List<PotStateModel> allPots = new List<PotStateModel>();

    void Start()
    {
        // Trova il DayCycleController se non assegnato
        if (dayCycleController == null)
        {
            dayCycleController = FindObjectOfType<SPOR_BLK_01_03A_DayCycleController>();
        }
        
        if (enableDebugLogs)
        {
            Debug.Log("[BLK-01.03A] GrowthDebugger inizializzato. Premi F6 per stampare stato vasi.");
        }
    }

    void Update()
    {
        if (!enableHotkeys) return;

        // F6: Stampa stato di tutti i vasi
        if (Input.GetKeyDown(KeyCode.F6))
        {
            PrintAllPotsStatus();
        }
    }

    /// <summary>
    /// Stampa lo stato di tutti i vasi nel sistema
    /// </summary>
    [ContextMenu("Print All Pots Status")]
    public void PrintAllPotsStatus()
    {
        Debug.Log("=== STATO VASI BLK-01.03A ===");
        
        // Trova tutti i vasi nella scena
        FindAllPots();
        
        if (allPots.Count == 0)
        {
            Debug.Log("Nessun vaso trovato nella scena.");
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
        
        Debug.Log("=== FINE STATO VASI ===");
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
        
        Debug.Log($"[{index}] {pot.PotId}:");
        Debug.Log($"  - Stato: {(pot.HasPlant ? "Pianta" : "Vuoto")}");
        
        if (pot.HasPlant)
        {
            Debug.Log($"  - Stadio: {stageName} (Progresso: {pot.GrowthPoints}/{threshold})");
            Debug.Log($"  - Giorni dalla semina: {pot.DaysSincePlant}");
            Debug.Log($"  - Giorni negligenza: {pot.DaysNeglectedStreak}");
            Debug.Log($"  - Idratazione: {pot.Hydration}/3");
            Debug.Log($"  - Luce: {pot.LightExposure}/3");
            Debug.Log($"  - Timestamps:");
            Debug.Log($"    * Piantato: Giorno {pot.PlantedDay}");
            Debug.Log($"    * Ultima acqua: Giorno {pot.LastWateredDay}");
            Debug.Log($"    * Ultima luce: Giorno {pot.LastLitDay}");
        }
        
        Debug.Log("  ---");
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
                Debug.Log("=== CONFIGURAZIONE CRESCITA ===");
                Debug.Log($"  - Seed → Sprout: {config.pointsSeedToSprout} punti");
                Debug.Log($"  - Sprout → Mature: {config.pointsSproutToMature} punti");
                Debug.Log($"  - Cura ideale: {config.pointsIdealCare} punti");
                Debug.Log($"  - Cura parziale: {config.pointsPartialCare} punti");
                Debug.Log($"  - Nessuna cura: {config.pointsNoCare} punti");
                Debug.Log($"  - Decadimento idratazione: {config.dailyHydrationDecay}");
                Debug.Log($"  - Vasi registrati: {dayCycleController.GetRegisteredPotCount()}");
            }
        }
        else
        {
            Debug.LogWarning("DayCycleController non trovato!");
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
            Debug.LogError("DayCycleController non trovato!");
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

        Debug.Log($"[BLK-01.03A] Forzata registrazione di {allPots.Count} vasi nel DayCycleController");
    }

    /// <summary>
    /// Simula un tick di crescita (utile per test)
    /// </summary>
    [ContextMenu("Simulate Growth Tick")]
    public void SimulateGrowthTick()
    {
        if (dayCycleController == null)
        {
            Debug.LogError("DayCycleController non trovato!");
            return;
        }

        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            Debug.Log($"[BLK-01.03A] Simulazione tick crescita per giorno {gameManager.CurrentDay}");
            // Il DayCycleController si iscrive automaticamente a OnDayChanged
            // quindi questo è solo per debug
        }
        else
        {
            Debug.LogError("GameManager non trovato!");
        }
    }
}
