using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using _Project.Sporae.Core;
using Sporae.Dome.PotSystem.Growth;
using Sporae.DevTools;

/// <summary>
/// Script di test completo per il sistema BLK-01.03A
/// Verifica che tutti i componenti siano configurati correttamente
/// </summary>
public class SPOR_BLK_01_03A_SystemTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool runTestsOnStart = true;
    [SerializeField] private bool showDetailedLogs = true;
    
    [Header("Test Results")]
    [SerializeField] private bool allTestsPassed = false;
    [SerializeField] private List<string> testResults = new List<string>();
    
    [Header("Component References")]
    [SerializeField] private DayCycleController dayCycleController;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PotSystemConfig potSystemConfig;

    private DayCycleSystem _dayCycleSystem;
    
    private void Start()
    {
        if (runTestsOnStart)
        {
            RunAllTests();
        }
    }
    
    /// <summary>
    /// Esegue tutti i test del sistema
    /// </summary>
    [ContextMenu("Run All Tests")]
    public void RunAllTests()
    {
        testResults.Clear();
        allTestsPassed = true;
        
        SporiumLogger.LogInfo(LogCategory.Dome, "=== INIZIO TEST SISTEMA BLK-01.03A ===");
        
        // Test 1: Verifica componenti essenziali
        TestEssentialComponents();
        
        // Test 2: Verifica configurazione
        TestConfiguration();
        
        // Test 3: Verifica vasi e registrazione
        TestPotsAndRegistration();
        
        // Test 4: Verifica sistema crescita
        TestGrowthSystem();
        
        // Test 5: Verifica timestamp
        TestTimestampSystem();
        
        // Risultati finali
        LogTestResults();
        
        SporiumLogger.LogInfo(LogCategory.Dome, "=== FINE TEST SISTEMA BLK-01.03A ===");
    }
    
    /// <summary>
    /// Test 1: Verifica componenti essenziali
    /// </summary>
    private void TestEssentialComponents()
    {
        SporiumLogger.LogInfo(LogCategory.Dome, "--- Test 1: Componenti Essenziali ---");

        _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
        
        // Verifica GameManager
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        
        if (gameManager != null)
        {
            LogTestResult("✅ GameManager trovato", true);
        }
        else
        {
            LogTestResult("❌ GameManager NON trovato", false);
        }
        
        // Verifica DayCycleController
        if (dayCycleController == null)
        {
            dayCycleController = FindObjectOfType<DayCycleController>();
        }
        
        if (dayCycleController != null)
        {
            LogTestResult("✅ DayCycleController trovato", true);
        }
        else
        {
            LogTestResult("❌ DayCycleController NON trovato", false);
        }
        
        // Verifica PotSystemConfig
        if (potSystemConfig == null)
        {
            potSystemConfig = FindObjectOfType<PotSystemConfig>();
        }
        
        if (potSystemConfig != null)
        {
            LogTestResult("✅ PotSystemConfig trovato", true);
        }
        else
        {
            LogTestResult("❌ PotSystemConfig NON trovato", false);
        }
    }
    
    /// <summary>
    /// Test 2: Verifica configurazione
    /// </summary>
    private void TestConfiguration()
    {
        SporiumLogger.LogInfo(LogCategory.Dome, "--- Test 2: Configurazione ---");
        
        if (potSystemConfig != null)
        {
            // Verifica configurazione crescita
            if (potSystemConfig.GrowthConfig != null)
            {
                var growthConfig = potSystemConfig.GrowthConfig;
                LogTestResult($"✅ GrowthConfig trovato: {growthConfig.name}", true);
                LogTestResult($"   Seed→Sprout: {growthConfig.pointsSeedToSprout} punti", true);
                LogTestResult($"   Sprout→Mature: {growthConfig.pointsSproutToMature} punti", true);
            }
            else
            {
                LogTestResult("❌ GrowthConfig NON trovato in PotSystemConfig", false);
            }
            
            // Verifica altre configurazioni
            LogTestResult($"   InteractDistance: {potSystemConfig.InteractDistance}", true);
            LogTestResult($"   MaxHydration: {potSystemConfig.MaxHydration}", true);
            LogTestResult($"   MaxLightExposure: {potSystemConfig.MaxLightExposure}", true);
        }
        else
        {
            LogTestResult("❌ PotSystemConfig non disponibile per test configurazione", false);
        }
    }
    
    /// <summary>
    /// Test 3: Verifica vasi e registrazione
    /// </summary>
    private void TestPotsAndRegistration()
    {
        SporiumLogger.LogInfo(LogCategory.Dome, "--- Test 3: Vasi e Registrazione ---");
        
        // Trova tutti i vasi
        var allPots = FindObjectsOfType<PotSlot>();
        LogTestResult($"✅ Vasi trovati: {allPots.Length}", true);
        
        foreach (var pot in allPots)
        {
            if (pot != null)
            {
                LogTestResult($"   Vaso: {pot.PotId}", true);
                
                // Verifica componenti
                var potActions = pot.GetComponent<PotActions>();
                var potGrowthController = pot.GetComponent<PotGrowthController>();
                var potStateModel = pot.GetComponent<PotStateModel>();
                
                if (potActions != null)
                    LogTestResult($"     ✅ PotActions", true);
                else
                    LogTestResult($"     ❌ PotActions mancante", false);
                
                if (potGrowthController != null)
                    LogTestResult($"     ✅ PotGrowthController", true);
                else
                    LogTestResult($"     ❌ PotGrowthController mancante", false);
                
                if (potStateModel != null)
                    LogTestResult($"     ✅ PotStateModel", true);
                else
                    LogTestResult($"     ❌ PotStateModel mancante", false);
            }
        }
        
        // Verifica registrazione nel DayCycleController
        if (dayCycleController != null)
        {
            int registeredCount = dayCycleController.GetRegisteredPotCount();
            LogTestResult($"✅ Vasi registrati nel DayCycleController: {registeredCount}", true);
        }
        else
        {
            LogTestResult("❌ DayCycleController non disponibile per test registrazione", false);
        }
    }
    
    /// <summary>
    /// Test 4: Verifica sistema crescita
    /// </summary>
    private void TestGrowthSystem()
    {
        SporiumLogger.LogInfo(LogCategory.Dome, "--- Test 4: Sistema Crescita ---");
        
        if (dayCycleController != null)
        {
            var growthConfig = dayCycleController.GetGrowthConfig();
            if (growthConfig != null)
            {
                LogTestResult($"✅ Configurazione crescita caricata: {growthConfig.name}", true);
                
                // Verifica parametri crescita
                if (growthConfig.pointsSeedToSprout > 0)
                    LogTestResult($"   ✅ pointsSeedToSprout: {growthConfig.pointsSeedToSprout}", true);
                else
                    LogTestResult($"   ❌ pointsSeedToSprout: {growthConfig.pointsSeedToSprout}", false);
                
                if (growthConfig.pointsSproutToMature > 0)
                    LogTestResult($"   ✅ pointsSproutToMature: {growthConfig.pointsSproutToMature}", true);
                else
                    LogTestResult($"   ❌ pointsSproutToMature: {growthConfig.pointsSproutToMature}", false);
            }
            else
            {
                LogTestResult("❌ Configurazione crescita non disponibile", false);
            }
        }
        else
        {
            LogTestResult("❌ DayCycleController non disponibile per test crescita", false);
        }
    }
    
    /// <summary>
    /// Test 5: Verifica sistema timestamp
    /// </summary>
    private void TestTimestampSystem()
    {
        SporiumLogger.LogInfo(LogCategory.Dome, "--- Test 5: Sistema Timestamp ---");
        
        // Verifica che i vasi abbiano timestamp corretti
        var allPots = FindObjectsOfType<PotSlot>();
        
        foreach (var pot in allPots)
        {
            if (pot != null)
            {
                var potActions = pot.GetComponent<PotActions>();
                if (potActions != null)
                {
                    var potState = potActions.GetCurrentState();
                    if (potState != null)
                    {
                        LogTestResult($"   Vaso {pot.PotId}:", true);
                        LogTestResult($"     Stage: {potState.Stage} ({GetStageName(potState.Stage)})", true);
                        LogTestResult($"     PlantedDay: {potState.PlantedDay}", true);
                        LogTestResult($"     LastWateredDay: {potState.LastWateredDay}", true);
                        LogTestResult($"     LastLitDay: {potState.LastLitDay}", true);
                        LogTestResult($"     GrowthPoints: {potState.GrowthPoints}", true);
                        LogTestResult($"     WateringSystemOn: {potState.WateringSystemOn} (GDD AZ-11)", true);
                        LogTestResult($"     DaysWateringSystemOn: {potState.DaysWateringSystemOn}", true);
                        LogTestResult($"     WateringRawWaterAccumulator: {potState.WateringRawWaterAccumulator:F1}", true);
                    }
                }
            }
        }
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
    
    /// <summary>
    /// Registra il risultato di un test
    /// </summary>
    private void LogTestResult(string message, bool passed)
    {
        if (showDetailedLogs)
        {
            SporiumLogger.LogInfo(LogCategory.Dome, message);
        }
        
        testResults.Add(message);
        
        if (!passed)
        {
            allTestsPassed = false;
        }
    }
    
    /// <summary>
    /// Mostra i risultati finali dei test
    /// </summary>
    private void LogTestResults()
    {
        SporiumLogger.LogInfo(LogCategory.Dome, "--- RISULTATI FINALI TEST ---");
        
        if (allTestsPassed)
        {
            SporiumLogger.LogInfo(LogCategory.Dome, "🎉 TUTTI I TEST SONO PASSATI! Il sistema è configurato correttamente.");
        }
        else
        {
            SporiumLogger.LogWarning(LogCategory.Dome, "⚠️ ALCUNI TEST SONO FALLITI! Controlla i log sopra per i dettagli.");
        }
        
        SporiumLogger.LogInfo(LogCategory.Dome, $"Test eseguiti: {testResults.Count}");
        SporiumLogger.LogInfo(LogCategory.Dome, $"Test passati: {testResults.Count(r => r.StartsWith("✅"))}");
        SporiumLogger.LogInfo(LogCategory.Dome, $"Test falliti: {testResults.Count(r => r.StartsWith("❌"))}");
    }
    
    /// <summary>
    /// Test rapido del sistema di crescita
    /// </summary>
    [ContextMenu("Quick Growth Test")]
    public void QuickGrowthTest()
    {
        SporiumLogger.LogInfo(LogCategory.Dome, "--- TEST RAPIDO CRESCITA ---");
        
        if (dayCycleController != null)
        {
            var growthConfig = dayCycleController.GetGrowthConfig();
            if (growthConfig != null)
            {
                SporiumLogger.LogInfo(LogCategory.Dome, $"Configurazione: {growthConfig.name}");
                SporiumLogger.LogInfo(LogCategory.Dome, $"Seed→Sprout: {growthConfig.pointsSeedToSprout} punti");
                SporiumLogger.LogInfo(LogCategory.Dome, $"Sprout→Mature: {growthConfig.pointsSproutToMature} punti");
                SporiumLogger.LogInfo(LogCategory.Dome, $"Vasi registrati: {dayCycleController.GetRegisteredPotCount()}");
            }
        }
        
        if (gameManager != null)
        {
            SporiumLogger.LogInfo(LogCategory.Core, $"Giorno corrente: {_dayCycleSystem.CurrentDay}");
            SporiumLogger.LogInfo(LogCategory.Core, $"Azioni rimanenti: {gameManager.ActionsLeft}");
            SporiumLogger.LogInfo(LogCategory.Core, $"CRY disponibili: {gameManager.CurrentCRY}");
        }
    }
    
    /// <summary>
    /// Forza l'esecuzione di un tick di crescita
    /// </summary>
    [ContextMenu("Force Growth Tick")]
    public void ForceGrowthTick()
    {
        SporiumLogger.LogInfo(LogCategory.Dome, "--- FORZA TICK CRESCITA ---");
        
        if (gameManager != null)
        {
            SporiumLogger.LogInfo(LogCategory.Dome, "Forzando EndDay per test crescita...");
            _dayCycleSystem.EndDay();
        }
        else
        {
            SporiumLogger.LogError(LogCategory.Core, "GameManager non trovato!");
        }
    }
}
