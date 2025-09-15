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
            if (pot is { HasPlant: true })
            {
                ResolveGrowthForPot(pot, dayIndex);
            }
        }
    }

    /// <summary>
    /// Risolve la crescita per un singolo vaso
    /// BLK-01.04: Implementa sistema di crescita a 3 stadi con punti giornalieri
    /// </summary>
    private void ResolveGrowthForPot(PotStateModel pot, int dayIndex)
    {
        // BLK-01.04: Fix - Confronta con il giorno precedente perché i timestamp
        // vengono impostati con gameManager.CurrentDay, ma dayIndex è il giorno corrente
        // dopo che EndDay ha già incrementato il giorno
        int previousDay = dayIndex - 1;
        bool hadHydration = (pot.LastWateredDay == previousDay);
        bool hadLight = (pot.LastLitDay == previousDay);
        
        // BLK-01.04: Calcola punti crescita basati sulla cura ricevuta oggi
        // Cura ideale (acqua + luce) = +2 punti
        // Cura parziale (una delle due) = +1 punto  
        // Nessuna cura = +0 punti
        int gained = 0;
        if (hadHydration && hadLight)
        {
            gained = growthConfig.pointsIdealCare; // +2 punti
        }
        else if (hadHydration || hadLight)
        {
            gained = growthConfig.pointsPartialCare; // +1 punto
        }
        else
        {
            gained = growthConfig.pointsNoCare; // +0 punti
        }
        
        int oldPoints = pot.GrowthPoints;
        pot.GrowthPoints += gained;
        
        if (enableDebugLogs)
        {
            string stageName = GetStageName(pot.Stage);
            string careType = (hadHydration && hadLight) ? "ideale" : (hadHydration || hadLight) ? "parziale" : "nessuna";
            Debug.Log($"[BLK-01.04] D={dayIndex} {pot.PotId}: Cura {careType} (H={hadHydration} L={hadLight}) +{gained} punti, totali={pot.GrowthPoints}, stage={pot.Stage}({stageName}) - Timestamps: W={pot.LastWateredDay} L={pot.LastLitDay} vs giorno={previousDay}");
        }

        // BLK-01.04: Avanzamento stadi con soglie configurabili
        // Seed (Stage 1) → Sprout (Stage 2) = 2 punti
        // Sprout (Stage 2) → Mature (Stage 3) = 3 punti
        bool stageChanged = false;
        int oldStage = pot.Stage;
        
        if (pot.Stage == (int)PlantStage.Seed && pot.GrowthPoints >= growthConfig.pointsSeedToSprout)
        {
            pot.GrowthPoints -= growthConfig.pointsSeedToSprout;
            pot.Stage = (int)PlantStage.Sprout;
            stageChanged = true;
            if (enableDebugLogs)
                Debug.Log($"[BLK-01.04] {pot.PotId}: 🎉 Avanzamento Seed → Sprout! (soglia: {growthConfig.pointsSeedToSprout} punti)");
        }
        else if (pot.Stage == (int)PlantStage.Sprout && pot.GrowthPoints >= growthConfig.pointsSproutToMature)
        {
            pot.GrowthPoints -= growthConfig.pointsSproutToMature;
            pot.Stage = (int)PlantStage.Mature;
            stageChanged = true;
            if (enableDebugLogs)
                Debug.Log($"[BLK-01.04] {pot.PotId}: 🌱 Avanzamento Sprout → Mature! (soglia: {growthConfig.pointsSproutToMature} punti)");
        }

        if (pot.Stage == (int)PlantStage.Mature && !stageChanged)
            pot.AmountFruits = (pot.AmountFruits + 0.5f) % 10;
        
        // BLK-01.04: Emetti eventi per notificare crescita e/o cambio di stadio
        if (stageChanged)
        {
            // Notifica il PotGrowthController per aggiornare le visuali
            var potGrowthController = FindPotGrowthController(pot.PotId);
            if (potGrowthController != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"[BLK-01.04] {pot.PotId}: Trovato PotGrowthController, chiamando OnStageChanged...");
                potGrowthController.OnStageChanged((PlantStage)pot.Stage);
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[BLK-01.04] {pot.PotId}: PotGrowthController NON TROVATO! Le visuali non saranno aggiornate.");
            }
            
            // Emetti evento per l'UI
            PotEvents.EmitPlantStageChanged(pot.PotId, (PlantStage)pot.Stage);
            
            if (enableDebugLogs)
                Debug.Log($"[BLK-01.04] {pot.PotId}: Eventi emessi per cambio stadio {oldStage} → {pot.Stage}, punti rimanenti: {pot.GrowthPoints}");
        }
        
        // BLK-01.04: Emetti evento di crescita (sempre, per aggiornare progress bar)
        if (gained > 0 || stageChanged)
        {
            PotEvents.RaiseOnPlantGrew(pot.PotId, (PlantStage)pot.Stage, gained, pot.GrowthPoints);
            if (enableDebugLogs)
                Debug.Log($"[BLK-01.04] {pot.PotId}: Evento crescita emesso: +{gained} punti, totali: {pot.GrowthPoints}");
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
    /// BLK-01.04: Trova il PotGrowthController per un vaso specifico
    /// </summary>
    private PotGrowthController FindPotGrowthController(string potId)
    {
        // Cerca tutti i PotGrowthController nella scena
        PotGrowthController[] controllers = FindObjectsOfType<PotGrowthController>();
        foreach (var controller in controllers)
        {
            var potState = controller.GetPotState();
            if (potState != null && potState.PotId == potId)
            {
                return controller;
            }
        }
        return null;
    }

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
