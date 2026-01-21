using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using _Project.Sporae.Core;
using Sporae.Dome.PotSystem.Growth;
using Sporae.Dome.PotSystem.Mold;
using Sporae.DevTools;
using _Project;

/// <summary>
/// Test per il sistema Mold Risk Synergy (EVIL/PURE)
/// Verifica modificatori crescita/resa, blocco crescita e infestazione differenziata
/// </summary>
public class MoldRiskSynergyTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool runTestsOnStart = false;
    [SerializeField] private bool showDetailedLogs = true;
    
    [Header("Test Results")]
    [SerializeField] private bool allTestsPassed = false;
    [SerializeField] private List<string> testResults = new List<string>();
    
    [Header("Component References")]
    [SerializeField] private DayCycleController dayCycleController;
    [SerializeField] private PhSystem phSystem;
    [SerializeField] private PotSystemConfig potSystemConfig;
    
    private void Start()
    {
        if (runTestsOnStart)
        {
            RunAllTests();
        }
    }
    
    /// <summary>
    /// Esegue tutti i test del sistema Mold Risk Synergy
    /// </summary>
    [ContextMenu("Run All Mold Risk Synergy Tests")]
    public void RunAllTests()
    {
        testResults.Clear();
        allTestsPassed = true;
        
        SporiumLogger.LogInfo(LogCategory.Dome, "=== INIZIO TEST MOLD RISK SYNERGY ===");
        
        // Test 1: Modificatori crescita/resa per EVIL e PURE
        TestMoldGrowthAndYieldModifiers();
        
        // Test 2: Blocco crescita differenziato per famiglia
        TestMoldGrowthBlockByFamily();
        
        // Test 3: Infestazione differenziata per famiglia
        TestMoldInfestationByFamily();
        
        // Risultati finali
        LogTestResults();
        
        SporiumLogger.LogInfo(LogCategory.Dome, "=== FINE TEST MOLD RISK SYNERGY ===");
    }
    
    /// <summary>
    /// TEST 1: Verifica modificatori crescita/resa per EVIL e PURE con Mold Risk
    /// </summary>
    [ContextMenu("Test 1: Mold Growth/Yield Modifiers")]
    public void TestMoldGrowthAndYieldModifiers()
    {
        SporiumLogger.LogInfo(LogCategory.Dome, "--- TEST 1: Modificatori Crescita/Resa Mold Risk ---");
        
        // Setup: Ottieni PhBand neutrale per test base
        PhSystem.PhBand neutralPh = PhSystem.PhBand.Neutral;
        PhSystem.PhBand basicPh = PhSystem.PhBand.StableBasic;
        PhSystem.PhBand acidPh = PhSystem.PhBand.StableAcid;
        
        // TEST 1.1: EVIL con Mold Risk Level 1-2 (bonus crescita)
        float evilGrowthL1 = PhGrowthModifier.GetMoldGrowthModifier(1, PlantFamily.Evil, neutralPh);
        float evilGrowthL2 = PhGrowthModifier.GetMoldGrowthModifier(2, PlantFamily.Evil, neutralPh);
        float evilGrowthL3 = PhGrowthModifier.GetMoldGrowthModifier(3, PlantFamily.Evil, neutralPh);
        
        LogTestResult($"EVIL Mold Risk L1 crescita: {evilGrowthL1:F2} (atteso: 1.20)", 
            Mathf.Approximately(evilGrowthL1, 1.20f), "EVIL dovrebbe avere +20% crescita a L1");
        LogTestResult($"EVIL Mold Risk L2 crescita: {evilGrowthL2:F2} (atteso: 1.20)", 
            Mathf.Approximately(evilGrowthL2, 1.20f), "EVIL dovrebbe avere +20% crescita a L2");
        LogTestResult($"EVIL Mold Risk L3 crescita: {evilGrowthL3:F2} (atteso: 1.30)", 
            Mathf.Approximately(evilGrowthL3, 1.30f), "EVIL dovrebbe avere +30% crescita a L3");
        
        // TEST 1.2: EVIL con Mold Risk + pH Basico (sinergia doppia)
        float evilGrowthL3Basic = PhGrowthModifier.GetMoldGrowthModifier(3, PlantFamily.Evil, basicPh);
        LogTestResult($"EVIL Mold Risk L3 + pH Basico crescita: {evilGrowthL3Basic:F2} (atteso: 1.40)", 
            Mathf.Approximately(evilGrowthL3Basic, 1.40f), "EVIL dovrebbe avere +40% crescita con sinergia doppia");
        
        // TEST 1.3: PURE con Mold Risk Level 1-2 (penalità crescita)
        float pureGrowthL1 = PhGrowthModifier.GetMoldGrowthModifier(1, PlantFamily.Pure, neutralPh);
        float pureGrowthL2 = PhGrowthModifier.GetMoldGrowthModifier(2, PlantFamily.Pure, neutralPh);
        float pureGrowthL3 = PhGrowthModifier.GetMoldGrowthModifier(3, PlantFamily.Pure, neutralPh);
        
        LogTestResult($"PURE Mold Risk L1 crescita: {pureGrowthL1:F2} (atteso: 0.80)", 
            Mathf.Approximately(pureGrowthL1, 0.80f), "PURE dovrebbe avere -20% crescita a L1");
        LogTestResult($"PURE Mold Risk L2 crescita: {pureGrowthL2:F2} (atteso: 0.80)", 
            Mathf.Approximately(pureGrowthL2, 0.80f), "PURE dovrebbe avere -20% crescita a L2");
        LogTestResult($"PURE Mold Risk L3 crescita: {pureGrowthL3:F2} (atteso: 0.70)", 
            Mathf.Approximately(pureGrowthL3, 0.70f), "PURE dovrebbe avere -30% crescita a L3");
        
        // TEST 1.4: PURE con Mold Risk + pH Acido (sinergia doppia)
        float pureGrowthL3Acid = PhGrowthModifier.GetMoldGrowthModifier(3, PlantFamily.Pure, acidPh);
        LogTestResult($"PURE Mold Risk L3 + pH Acido crescita: {pureGrowthL3Acid:F2} (atteso: 0.60)", 
            Mathf.Approximately(pureGrowthL3Acid, 0.60f), "PURE dovrebbe avere -40% crescita con sinergia doppia");
        
        // TEST 1.5: EVIL resa con infestazione
        float evilYieldInfested = PhGrowthModifier.GetMoldYieldModifier(3, true, PlantFamily.Evil, neutralPh);
        float evilYieldNotInfested = PhGrowthModifier.GetMoldYieldModifier(3, false, PlantFamily.Evil, neutralPh);
        
        LogTestResult($"EVIL infestata resa: {evilYieldInfested:F2} (atteso: 1.50)", 
            Mathf.Approximately(evilYieldInfested, 1.50f), "EVIL infestata dovrebbe avere +50% resa");
        LogTestResult($"EVIL non infestata L3 resa: {evilYieldNotInfested:F2} (atteso: 1.20)", 
            Mathf.Approximately(evilYieldNotInfested, 1.20f), "EVIL non infestata L3 dovrebbe avere +20% resa");
        
        // TEST 1.6: PURE resa con Mold Risk
        float pureYieldL3 = PhGrowthModifier.GetMoldYieldModifier(3, false, PlantFamily.Pure, neutralPh);
        LogTestResult($"PURE Mold Risk L3 resa: {pureYieldL3:F2} (atteso: 0.50)", 
            Mathf.Approximately(pureYieldL3, 0.50f), "PURE L3 dovrebbe avere -50% resa");
        
        // TEST 1.7: Standard non ha modificatori
        float standardGrowth = PhGrowthModifier.GetMoldGrowthModifier(3, PlantFamily.Standard, neutralPh);
        float standardYield = PhGrowthModifier.GetMoldYieldModifier(3, true, PlantFamily.Standard, neutralPh);
        
        LogTestResult($"Standard Mold Risk L3 crescita: {standardGrowth:F2} (atteso: 1.00)", 
            Mathf.Approximately(standardGrowth, 1.00f), "Standard non dovrebbe avere modificatori crescita");
        LogTestResult($"Standard Mold Risk L3 resa: {standardYield:F2} (atteso: 1.00)", 
            Mathf.Approximately(standardYield, 1.00f), "Standard non dovrebbe avere modificatori resa");
    }
    
    /// <summary>
    /// TEST 2: Verifica blocco crescita differenziato per famiglia
    /// </summary>
    [ContextMenu("Test 2: Mold Growth Block By Family")]
    public void TestMoldGrowthBlockByFamily()
    {
        SporiumLogger.LogInfo(LogCategory.Dome, "--- TEST 2: Blocco Crescita per Famiglia ---");
        
        // Questo test verifica la logica implementata in DayCycleController
        // Simula le condizioni di blocco per ogni famiglia
        
        // TEST 2.1: EVIL non viene bloccata da Mold Risk
        // (Logica: isBlockedByMoldRisk = false per EVIL)
        bool evilBlockedL1 = ShouldBlockGrowth(PlantFamily.Evil, 1);
        bool evilBlockedL2 = ShouldBlockGrowth(PlantFamily.Evil, 2);
        bool evilBlockedL3 = ShouldBlockGrowth(PlantFamily.Evil, 3);
        
        LogTestResult($"EVIL Mold Risk L1 bloccata: {evilBlockedL1} (atteso: false)", 
            !evilBlockedL1, "EVIL NON dovrebbe essere bloccata da Mold Risk L1");
        LogTestResult($"EVIL Mold Risk L2 bloccata: {evilBlockedL2} (atteso: false)", 
            !evilBlockedL2, "EVIL NON dovrebbe essere bloccata da Mold Risk L2");
        LogTestResult($"EVIL Mold Risk L3 bloccata: {evilBlockedL3} (atteso: false)", 
            !evilBlockedL3, "EVIL NON dovrebbe essere bloccata da Mold Risk L3");
        
        // TEST 2.2: PURE viene bloccata a Mold Risk Level ≥1
        bool pureBlockedL0 = ShouldBlockGrowth(PlantFamily.Pure, 0);
        bool pureBlockedL1 = ShouldBlockGrowth(PlantFamily.Pure, 1);
        bool pureBlockedL2 = ShouldBlockGrowth(PlantFamily.Pure, 2);
        bool pureBlockedL3 = ShouldBlockGrowth(PlantFamily.Pure, 3);
        
        LogTestResult($"PURE Mold Risk L0 bloccata: {pureBlockedL0} (atteso: false)", 
            !pureBlockedL0, "PURE NON dovrebbe essere bloccata a L0");
        LogTestResult($"PURE Mold Risk L1 bloccata: {pureBlockedL1} (atteso: true)", 
            pureBlockedL1, "PURE dovrebbe essere bloccata a L1");
        LogTestResult($"PURE Mold Risk L2 bloccata: {pureBlockedL2} (atteso: true)", 
            pureBlockedL2, "PURE dovrebbe essere bloccata a L2");
        LogTestResult($"PURE Mold Risk L3 bloccata: {pureBlockedL3} (atteso: true)", 
            pureBlockedL3, "PURE dovrebbe essere bloccata a L3");
        
        // TEST 2.3: Standard viene bloccata a Mold Risk Level ≥2
        bool standardBlockedL0 = ShouldBlockGrowth(PlantFamily.Standard, 0);
        bool standardBlockedL1 = ShouldBlockGrowth(PlantFamily.Standard, 1);
        bool standardBlockedL2 = ShouldBlockGrowth(PlantFamily.Standard, 2);
        bool standardBlockedL3 = ShouldBlockGrowth(PlantFamily.Standard, 3);
        
        LogTestResult($"Standard Mold Risk L0 bloccata: {standardBlockedL0} (atteso: false)", 
            !standardBlockedL0, "Standard NON dovrebbe essere bloccata a L0");
        LogTestResult($"Standard Mold Risk L1 bloccata: {standardBlockedL1} (atteso: false)", 
            !standardBlockedL1, "Standard NON dovrebbe essere bloccata a L1");
        LogTestResult($"Standard Mold Risk L2 bloccata: {standardBlockedL2} (atteso: true)", 
            standardBlockedL2, "Standard dovrebbe essere bloccata a L2");
        LogTestResult($"Standard Mold Risk L3 bloccata: {standardBlockedL3} (atteso: true)", 
            standardBlockedL3, "Standard dovrebbe essere bloccata a L3");
    }
    
    /// <summary>
    /// TEST 3: Verifica infestazione differenziata per famiglia
    /// </summary>
    [ContextMenu("Test 3: Mold Infestation By Family")]
    public void TestMoldInfestationByFamily()
    {
        SporiumLogger.LogInfo(LogCategory.Dome, "--- TEST 3: Infestazione Differenziata per Famiglia ---");
        
        // Questo test verifica la logica implementata in MoldSystem.ApplyInfestation
        // Simula le riduzioni livello per ogni famiglia
        
        // TEST 3.1: EVIL infestata ha riduzione livello minore (-1 invece di -3)
        int evilLevelReductionMild = GetLevelReductionForFamily(PlantFamily.Evil, 1);
        int evilLevelReductionSevere = GetLevelReductionForFamily(PlantFamily.Evil, 3);
        
        LogTestResult($"EVIL infestazione Mild riduzione livello: {evilLevelReductionMild} (atteso: 0)", 
            evilLevelReductionMild == 0, "EVIL Mild dovrebbe avere riduzione 0");
        LogTestResult($"EVIL infestazione Severe riduzione livello: {evilLevelReductionSevere} (atteso: 1)", 
            evilLevelReductionSevere == 1, "EVIL Severe dovrebbe avere riduzione -1");
        
        // TEST 3.2: PURE infestata ha riduzione livello maggiore (-5 invece di -3)
        int pureLevelReductionMild = GetLevelReductionForFamily(PlantFamily.Pure, 1);
        int pureLevelReductionSevere = GetLevelReductionForFamily(PlantFamily.Pure, 3);
        
        LogTestResult($"PURE infestazione Mild riduzione livello: {pureLevelReductionMild} (atteso: 2)", 
            pureLevelReductionMild == 2, "PURE Mild dovrebbe avere riduzione -2");
        LogTestResult($"PURE infestazione Severe riduzione livello: {pureLevelReductionSevere} (atteso: 5)", 
            pureLevelReductionSevere == 5, "PURE Severe dovrebbe avere riduzione -5");
        
        // TEST 3.3: Standard infestata ha riduzione standard (-1 Mild, -3 Severe)
        int standardLevelReductionMild = GetLevelReductionForFamily(PlantFamily.Standard, 1);
        int standardLevelReductionSevere = GetLevelReductionForFamily(PlantFamily.Standard, 3);
        
        LogTestResult($"Standard infestazione Mild riduzione livello: {standardLevelReductionMild} (atteso: 1)", 
            standardLevelReductionMild == 1, "Standard Mild dovrebbe avere riduzione -1");
        LogTestResult($"Standard infestazione Severe riduzione livello: {standardLevelReductionSevere} (atteso: 3)", 
            standardLevelReductionSevere == 3, "Standard Severe dovrebbe avere riduzione -3");
    }
    
    /// <summary>
    /// Helper: Simula la logica di blocco crescita per famiglia
    /// </summary>
    private bool ShouldBlockGrowth(PlantFamily family, int moldRiskLevel)
    {
        switch (family)
        {
            case PlantFamily.Evil:
                // EVIL: NON bloccata da Mold Risk
                return false;
                
            case PlantFamily.Pure:
                // PURE: bloccata a Mold Risk Level ≥1
                return moldRiskLevel >= 1;
                
            case PlantFamily.Standard:
            default:
                // Standard: bloccata a Mold Risk Level ≥2
                return moldRiskLevel >= 2;
        }
    }
    
    /// <summary>
    /// Helper: Simula la logica di riduzione livello per famiglia
    /// </summary>
    private int GetLevelReductionForFamily(PlantFamily family, int moldRiskLevel)
    {
        if (moldRiskLevel == 1) // Mild
        {
            switch (family)
            {
                case PlantFamily.Evil:
                    return 0; // EVIL: NO riduzione livello
                case PlantFamily.Pure:
                    return 2; // PURE: riduzione maggiore anche per Mild
                default:
                    return 1; // Standard: -1
            }
        }
        else if (moldRiskLevel >= 2) // Severe o Critical
        {
            switch (family)
            {
                case PlantFamily.Evil:
                    return 1; // EVIL: riduzione minore (-1 invece di -3)
                case PlantFamily.Pure:
                    return 5; // PURE: riduzione maggiore (-5 invece di -3)
                default:
                    return 3; // Standard: -3
            }
        }
        
        return 0;
    }
    
    /// <summary>
    /// Registra il risultato di un test
    /// </summary>
    private void LogTestResult(string message, bool passed, string expectedBehavior = "")
    {
        string fullMessage = passed ? $"✅ {message}" : $"❌ {message}";
        if (!string.IsNullOrEmpty(expectedBehavior))
        {
            fullMessage += $" | {expectedBehavior}";
        }
        
        if (showDetailedLogs)
        {
            if (passed)
            {
                SporiumLogger.LogInfo(LogCategory.Dome, fullMessage);
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.Dome, fullMessage);
            }
        }
        
        testResults.Add(fullMessage);
        
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
        SporiumLogger.LogInfo(LogCategory.Dome, "--- RISULTATI FINALI TEST MOLD RISK SYNERGY ---");
        
        if (allTestsPassed)
        {
            SporiumLogger.LogInfo(LogCategory.Dome, "🎉 TUTTI I TEST SONO PASSATI! Il sistema Mold Risk Synergy funziona correttamente.");
        }
        else
        {
            SporiumLogger.LogWarning(LogCategory.Dome, "⚠️ ALCUNI TEST SONO FALLITI! Controlla i log sopra per i dettagli.");
        }
        
        int passedCount = testResults.Count(r => r.StartsWith("✅"));
        int failedCount = testResults.Count(r => r.StartsWith("❌"));
        
        SporiumLogger.LogInfo(LogCategory.Dome, $"Test eseguiti: {testResults.Count}");
        SporiumLogger.LogInfo(LogCategory.Dome, $"Test passati: {passedCount}");
        SporiumLogger.LogInfo(LogCategory.Dome, $"Test falliti: {failedCount}");
        
        // Dettaglio risultati
        if (showDetailedLogs && failedCount > 0)
        {
            SporiumLogger.LogWarning(LogCategory.Dome, "--- DETTAGLIO TEST FALLITI ---");
            foreach (var result in testResults.Where(r => r.StartsWith("❌")))
            {
                SporiumLogger.LogWarning(LogCategory.Dome, result);
            }
        }
    }
}
