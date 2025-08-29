using UnityEngine;

/// <summary>
/// Script di test per verificare il funzionamento del sistema dei vasi.
/// Da usare solo per debugging e testing.
/// </summary>
public class PotSystemTester : MonoBehaviour
{
    [Header("Test Controls")]
    [SerializeField] private bool enableTesting = true;
    [SerializeField] private KeyCode testKey = KeyCode.T;
    [SerializeField] private KeyCode resetKey = KeyCode.R;
    
    [Header("Test Results")]
    [SerializeField] private bool potsFound = false;
    [SerializeField] private bool widgetFound = false;
    [SerializeField] private int totalPots = 0;
    
    private PotSlot[] allPots;
    private PotHUDWidget hudWidget;
    private RoomDomePotsBootstrap bootstrap;
    
    void Start()
    {
        if (!enableTesting) return;
        
        // Cerca i componenti necessari
        FindComponents();
        
        // Esegui test iniziale
        RunInitialTests();
    }
    
    void Update()
    {
        if (!enableTesting) return;
        
        // Test con tasto T
        if (Input.GetKeyDown(testKey))
        {
            RunAllTests();
        }
        
        // Reset con tasto R
        if (Input.GetKeyDown(resetKey))
        {
            ResetTestResults();
        }
    }
    
    private void FindComponents()
    {
        // Cerca il bootstrap
        bootstrap = FindObjectOfType<RoomDomePotsBootstrap>();
        
        // Cerca tutti i vasi
        allPots = FindObjectsOfType<PotSlot>();
        totalPots = allPots.Length;
        
        // Cerca il widget HUD
        hudWidget = FindObjectOfType<PotHUDWidget>();
        
        // Aggiorna stati
        potsFound = totalPots > 0;
        widgetFound = hudWidget != null;
    }
    
    private void RunInitialTests()
    {
        Debug.Log("=== TEST INIZIALE SISTEMA VASI ===");
        
        // Test bootstrap
        if (bootstrap != null)
        {
            Debug.Log("✅ Bootstrap trovato: " + bootstrap.name);
        }
        else
        {
            Debug.LogWarning("❌ Bootstrap NON trovato!");
        }
        
        // Test vasi
        if (potsFound)
        {
            Debug.Log($"✅ Vasi trovati: {totalPots}");
            foreach (PotSlot pot in allPots)
            {
                Debug.Log($"  - {pot.PotId} (Stato: {pot.State})");
            }
        }
        else
        {
            Debug.LogWarning("❌ Nessun vaso trovato!");
        }
        
        // Test widget
        if (widgetFound)
        {
            Debug.Log("✅ Widget HUD trovato: " + hudWidget.name);
        }
        else
        {
            Debug.LogWarning("❌ Widget HUD NON trovato!");
        }
        
        Debug.Log("=== FINE TEST INIZIALE ===\n");
    }
    
    private void RunAllTests()
    {
        Debug.Log("=== TEST COMPLETO SISTEMA VASI ===");
        
        // Ricerca componenti aggiornata
        FindComponents();
        
        // Test 1: Bootstrap
        TestBootstrap();
        
        // Test 2: Vasi
        TestPots();
        
        // Test 3: Widget
        TestWidget();
        
        // Test 4: Interazioni
        TestInteractions();
        
        // Test 5: Integrazione
        TestIntegration();
        
        Debug.Log("=== FINE TEST COMPLETO ===\n");
    }
    
    private void TestBootstrap()
    {
        Debug.Log("--- Test Bootstrap ---");
        
        if (bootstrap != null)
        {
            Debug.Log("✅ Bootstrap funzionante");
            
            // Test metodi pubblici
            PotSlot pot1 = bootstrap.GetPot1();
            PotSlot pot2 = bootstrap.GetPot2();
            
            if (pot1 != null) Debug.Log($"✅ Pot1: {pot1.PotId}");
            else Debug.LogWarning("❌ Pot1 null");
            
            if (pot2 != null) Debug.Log($"✅ Pot2: {pot2.PotId}");
            else Debug.LogWarning("❌ Pot2 null");
        }
        else
        {
            Debug.LogError("❌ Bootstrap mancante!");
        }
    }
    
    private void TestPots()
    {
        Debug.Log("--- Test Vasi ---");
        
        if (potsFound)
        {
            Debug.Log($"✅ {totalPots} vasi trovati");
            
            foreach (PotSlot pot in allPots)
            {
                // Test proprietà
                Debug.Log($"  Vaso {pot.PotId}:");
                Debug.Log($"    - Stato: {pot.State}");
                Debug.Log($"    - Vuoto: {pot.IsEmpty}");
                Debug.Log($"    - Attivo: {pot.enabled}");
                
                // Test componenti
                SpriteRenderer sr = pot.GetComponent<SpriteRenderer>();
                BoxCollider2D col = pot.GetComponent<BoxCollider2D>();
                
                if (sr != null) Debug.Log("    - SpriteRenderer: ✅");
                else Debug.LogWarning("    - SpriteRenderer: ❌");
                
                if (col != null) Debug.Log("    - BoxCollider2D: ✅");
                else Debug.LogWarning("    - BoxCollider2D: ❌");
            }
        }
        else
        {
            Debug.LogError("❌ Nessun vaso trovato!");
        }
    }
    
    private void TestWidget()
    {
        Debug.Log("--- Test Widget HUD ---");
        
        if (widgetFound)
        {
            Debug.Log("✅ Widget HUD funzionante");
            
            // Test messaggio personalizzato
            hudWidget.SetCustomMessage("Test: Widget funzionante");
            Debug.Log("✅ Test messaggio personalizzato");
            
            // Test show/hide
            hudWidget.HideWidget();
            Debug.Log("✅ Widget nascosto");
            
            hudWidget.ShowWidget();
            Debug.Log("✅ Widget mostrato");
        }
        else
        {
            Debug.LogError("❌ Widget HUD mancante!");
        }
    }
    
    private void TestInteractions()
    {
        Debug.Log("--- Test Interazioni ---");
        
        if (potsFound)
        {
            // Simula selezione del primo vaso
            PotSlot firstPot = allPots[0];
            if (firstPot != null)
            {
                Debug.Log($"✅ Test interazione con {firstPot.PotId}");
                
                // Test selezione
                firstPot.SelectPot();
                
                // Test cambio stato
                firstPot.SetState(PotState.Occupied);
                Debug.Log($"✅ Stato cambiato a: {firstPot.State}");
                
                // Test pulizia selezione
                firstPot.ClearSelection();
                Debug.Log("✅ Selezione pulita");
            }
        }
        else
        {
            Debug.LogWarning("❌ Nessun vaso per test interazioni");
        }
    }
    
    private void TestIntegration()
    {
        Debug.Log("--- Test Integrazione ---");
        
        // Test GameManager
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            Debug.Log("✅ GameManager trovato");
            Debug.Log($"  - Giorno: {gameManager.CurrentDay}");
            Debug.Log($"  - Azioni: {gameManager.ActionsLeft}");
            Debug.Log($"  - CRY: {gameManager.CurrentCRY}");
        }
        else
        {
            Debug.LogWarning("❌ GameManager non trovato");
        }
        
        // Test HUD esistente
        HUDController hudController = FindObjectOfType<HUDController>();
        if (hudController != null)
        {
            Debug.Log("✅ HUDController trovato");
        }
        else
        {
            Debug.LogWarning("❌ HUDController non trovato");
        }
        
        // Test Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log("✅ Player trovato");
            Debug.Log($"  - Posizione: {player.transform.position}");
        }
        else
        {
            Debug.LogWarning("❌ Player non trovato (tag 'Player')");
        }
    }
    
    private void ResetTestResults()
    {
        Debug.Log("=== RESET TEST RESULTS ===");
        
        // Reset stati vasi
        if (potsFound)
        {
            foreach (PotSlot pot in allPots)
            {
                pot.SetState(PotState.Empty);
                pot.ClearSelection();
            }
            Debug.Log("✅ Stati vasi resettati");
        }
        
        // Reset widget
        if (widgetFound)
        {
            hudWidget.SetCustomMessage("Nessun vaso selezionato");
            hudWidget.ShowWidget();
            Debug.Log("✅ Widget resettato");
        }
        
        Debug.Log("=== RESET COMPLETATO ===\n");
    }
    
    /// <summary>
    /// Forza l'esecuzione di tutti i test
    /// </summary>
    [ContextMenu("Run All Tests")]
    public void ForceRunTests()
    {
        RunAllTests();
    }
    
    /// <summary>
    /// Resetta i risultati dei test
    /// </summary>
    [ContextMenu("Reset Test Results")]
    public void ForceReset()
    {
        ResetTestResults();
    }
    
    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!enableTesting) return;
        
        // Disegna indicatori per i test
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        
        // Label
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.8f, "PotSystemTester");
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f, $"Pots: {totalPots}");
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.4f, $"Widget: {(widgetFound ? "OK" : "NO")}");
    }
    #endif
}
