using System.Collections.Generic;
using UnityEngine;
using Sporae.Core;
using Sporae.Dome.PotSystem.Growth;

/// <summary>
/// Controller per il ciclo giornaliero del sistema di crescita delle piante.
/// Implementa il sistema deterministico basato su timestamp invece di flag volatili.
/// Si iscrive a GameManager.OnDayChanged e gestisce la crescita di tutti i vasi registrati.
/// </summary>
public class SPOR_BLK_01_03A_DayCycleController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PlantGrowthConfig growthConfig;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Lista dei vasi registrati per la crescita
    private List<PotStateModel> registeredPots = new List<PotStateModel>();
    private bool isInitialized = false;

    private void Awake()
    {
        // Cerca la configurazione se non assegnata
        if (growthConfig == null)
        {
            growthConfig = Resources.Load<PlantGrowthConfig>("Configs/PlantGrowthConfig");
            if (growthConfig == null)
            {
                Debug.LogWarning("[BLK-01.03A] PlantGrowthConfig non trovato in Resources/Configs/, verrà cercato in PotSystemConfig");
            }
        }
    }

    private void Start()
    {
        InitializeSystem();
    }

    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    /// <summary>
    /// Inizializza il sistema e si iscrive agli eventi
    /// </summary>
    private void InitializeSystem()
    {
        if (isInitialized) return;

        // Cerca GameManager e si iscrive
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnDayChanged += HandleDayChanged;
            if (enableDebugLogs)
                Debug.Log("[BLK-01.03A] DayCycleController: Iscritto a GameManager.OnDayChanged");
        }
        else
        {
            Debug.LogError("[BLK-01.03A] DayCycleController: GameManager non trovato!");
        }

        // Cerca configurazione in PotSystemConfig se non trovata
        if (growthConfig == null)
        {
            var potSystemConfig = FindObjectOfType<PotSystemConfig>();
            if (potSystemConfig != null && potSystemConfig.GrowthConfig != null)
            {
                growthConfig = potSystemConfig.GrowthConfig;
                if (enableDebugLogs)
                    Debug.Log("[BLK-01.03A] DayCycleController: Configurazione caricata da PotSystemConfig");
            }
        }

        // Verifica configurazione
        if (growthConfig == null)
        {
            Debug.LogError("[BLK-01.03A] DayCycleController: Nessuna configurazione di crescita trovata!");
            return;
        }

        isInitialized = true;
        if (enableDebugLogs)
            Debug.Log($"[BLK-01.03A] DayCycleController: Inizializzato con config '{growthConfig.name}'");
    }

    /// <summary>
    /// Si iscrive agli eventi necessari
    /// </summary>
    private void SubscribeToEvents()
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnDayChanged += HandleDayChanged;
        }
    }

    /// <summary>
    /// Rimuove le iscrizioni agli eventi
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnDayChanged -= HandleDayChanged;
            if (enableDebugLogs)
                Debug.Log("[BLK-01.03A] DayCycleController: Disiscritto da GameManager.OnDayChanged");
        }
    }

    /// <summary>
    /// Registra un vaso nel sistema di crescita
    /// </summary>
    public void RegisterPot(PotStateModel pot)
    {
        if (pot == null) return;

        if (!registeredPots.Contains(pot))
        {
            registeredPots.Add(pot);
            if (enableDebugLogs)
                Debug.Log($"[BLK-01.03A] DayCycleController: Registrato vaso {pot.PotId}");
        }
    }

    /// <summary>
    /// Rimuove un vaso dal sistema di crescita
    /// </summary>
    public void UnregisterPot(PotStateModel pot)
    {
        if (pot == null) return;

        if (registeredPots.Remove(pot))
        {
            if (enableDebugLogs)
                Debug.Log($"[BLK-01.03A] DayCycleController: Rimosso vaso {pot.PotId}");
        }
    }

    /// <summary>
    /// Gestisce il cambio di giorno dal GameManager
    /// </summary>
    private void HandleDayChanged(int dayIndex)
    {
        if (enableDebugLogs)
            Debug.Log($"[BLK-01.03A] DayCycleController: HandleDayChanged chiamato per Day {dayIndex}");
        
        if (growthConfig == null)
        {
            Debug.LogError("[BLK-01.03A] DayCycleController: Nessuna configurazione di crescita trovata!");
            return;
        }
        
        // Pipeline End Day per il giorno D:
        // 1. ResolveGrowthForAllPots(D)
        ResolveGrowthForAllPots(dayIndex);
        
        // 2. ApplyDecayAndCleanup(D)
        ApplyDecayAndCleanup(dayIndex);
        
        // 3. AdvanceDayHUD() - gestito automaticamente dal GameManager esistente
        
        if (enableDebugLogs)
            Debug.Log($"[BLK-01.03A] DayCycleController: Growth tick completato per Day {dayIndex}");
    }

    /// <summary>
    /// Risolve la crescita per tutti i vasi registrati
    /// </summary>
    private void ResolveGrowthForAllPots(int dayIndex)
    {
        if (enableDebugLogs)
            Debug.Log($"[BLK-01.03A] DayCycleController: Applicazione crescita a {registeredPots.Count} vasi per Day {dayIndex}");

        foreach (var pot in registeredPots)
        {
            if (pot != null && pot.HasPlant)
            {
                ResolveGrowthForPot(pot, dayIndex);
            }
        }
    }

    /// <summary>
    /// Risolve la crescita per un singolo vaso
    /// </summary>
    private void ResolveGrowthForPot(PotStateModel pot, int dayIndex)
    {
        // Calcola se il vaso ha ricevuto acqua e luce oggi usando i timestamp
        bool hadHydration = (pot.LastWateredDay == dayIndex);
        bool hadLight = (pot.LastLitDay == dayIndex);
        
        // Calcola punti crescita basati sulla cura ricevuta oggi
        int gained = (hadHydration ? 1 : 0) + (hadLight ? 1 : 0);
        pot.GrowthPoints += gained;

        if (enableDebugLogs)
        {
            string stageName = GetStageName(pot.Stage);
            Debug.Log($"[Growth] D={dayIndex} pot={pot.PotId} H={hadHydration} L={hadLight} +{gained} gp={pot.GrowthPoints} stage={pot.Stage}({stageName})");
        }

        // Avanzamento stadio (Stage 1 = Seed, 2 = Sprout, 3 = Mature)
        if (pot.Stage == 1 && pot.GrowthPoints >= growthConfig.pointsSeedToSprout)
        {
            pot.GrowthPoints -= growthConfig.pointsSeedToSprout;
            pot.Stage = 2; // Sprout
            if (enableDebugLogs)
                Debug.Log($"[BLK-01.03A] {pot.PotId}: Avanzamento Seed → Sprout!");
        }
        
        if (pot.Stage == 2 && pot.GrowthPoints >= growthConfig.pointsSproutToMature)
        {
            pot.GrowthPoints -= growthConfig.pointsSproutToMature;
            pot.Stage = 3; // Mature
            if (enableDebugLogs)
                Debug.Log($"[BLK-01.03A] {pot.PotId}: Avanzamento Sprout → Mature!");
        }

        // Aggiorna contatori
        pot.DaysSincePlant++;
        if (gained == 0)
        {
            pot.DaysNeglectedStreak++;
        }
        else
        {
            pot.DaysNeglectedStreak = 0;
        }
    }

    /// <summary>
    /// Applica decadimento e pulizia (SENZA reset dei timestamp!)
    /// </summary>
    private void ApplyDecayAndCleanup(int dayIndex)
    {
        foreach (var pot in registeredPots)
        {
            if (pot != null && pot.HasPlant)
            {
                // Decadimento idratazione
                pot.Hydration = Mathf.Max(0, pot.Hydration - growthConfig.dailyHydrationDecay);
                
                // Reset esposizione luce (ma NON i timestamp!)
                pot.LightExposure = 0;

                if (enableDebugLogs)
                {
                    Debug.Log($"[BLK-01.03A] {pot.PotId}: Decay applicato - Hydration: {pot.Hydration}, Light: {pot.LightExposure}");
                }
            }
        }
    }

    /// <summary>
    /// Ottiene il numero di vasi registrati
    /// </summary>
    public int GetRegisteredPotCount()
    {
        return registeredPots.Count;
    }

    /// <summary>
    /// Ottiene la configurazione di crescita corrente
    /// </summary>
    public PlantGrowthConfig GetGrowthConfig()
    {
        return growthConfig;
    }

    /// <summary>
    /// Imposta la configurazione di crescita
    /// </summary>
    public void SetGrowthConfig(PlantGrowthConfig config)
    {
        growthConfig = config;
        if (enableDebugLogs)
            Debug.Log($"[BLK-01.03A] DayCycleController: Nuova configurazione impostata: {config?.name ?? "NULL"}");
    }

    #if UNITY_EDITOR
    [ContextMenu("Log Registered Pots")]
    private void EditorLogRegisteredPots()
    {
        Debug.Log($"[BLK-01.03A] DayCycleController: Vasi registrati ({registeredPots.Count}):");
        for (int i = 0; i < registeredPots.Count; i++)
        {
            var pot = registeredPots[i];
            if (pot != null)
            {
                string plantInfo = pot.HasPlant 
                    ? $" - {GetStageName(pot.Stage)} (Giorno {pot.DaysSincePlant})" 
                    : " - Vuoto";
                Debug.Log($"  [{i}] {pot.PotId}{plantInfo}");
            }
            else
            {
                Debug.Log($"  [{i}] NULL (da rimuovere)");
            }
        }
    }

    [ContextMenu("Cleanup Null Pots")]
    private void EditorCleanupNullPots()
    {
        registeredPots.RemoveAll(pot => pot == null);
        if (enableDebugLogs)
            Debug.Log($"[BLK-01.03A] DayCycleController: Cleanup completato, {registeredPots.Count} vasi validi");
    }
    #endif
    
    /// <summary>
    /// Restituisce il nome localizzato per uno stadio
    /// </summary>
    private string GetStageName(int stage)
    {
        switch (stage)
        {
            case 0: return "Empty";
            case 1: return "Seed";
            case 2: return "Sprout";
            case 3: return "Mature";
            default: return $"Stadio {stage}";
        }
    }
}
