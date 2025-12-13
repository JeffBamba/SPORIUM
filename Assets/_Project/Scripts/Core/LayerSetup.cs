#if UNITY_EDITOR
using UnityEngine;
using Sporae.DevTools;

/// <summary>
/// Configura automaticamente i layer necessari per il sistema dei vasi.
/// Da eseguire una volta per progetto.
/// </summary>
public class LayerSetup : MonoBehaviour
{
    [Header("Layer Configuration")]
    [SerializeField] private bool setupOnStart = true;
    [SerializeField] private bool showDebugLogs = true;
    
    // Nomi dei layer (devono corrispondere a quelli definiti in Project Settings)
    private const string LAYER_PLAYER = "LAYER_PLAYER";
    private const string LAYER_INTERACTABLE = "LAYER_INTERACTABLE";
    private const string LAYER_GROUND = "LAYER_GROUND";
    
    void Start()
    {
        if (setupOnStart)
        {
            SetupLayers();
        }
    }
    
    [ContextMenu("Setup Layers")]
    public void SetupLayers()
    {
        if (showDebugLogs)
            SporiumLogger.LogInfo(LogCategory.Core, "Iniziando configurazione layer...");

        // Configura Player
        SetupPlayerLayer();
        
        // Configura Vasi
        SetupPotLayers();
        
        // Configura Ground
        SetupGroundLayer();
        
        if (showDebugLogs)
            SporiumLogger.LogInfo(LogCategory.Core, "Configurazione layer completata!");
    }
    
    private void SetupPlayerLayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            int playerLayer = LayerMask.NameToLayer(LAYER_PLAYER);
            if (playerLayer != -1)
            {
                player.layer = playerLayer;
                if (showDebugLogs)
                    SporiumLogger.LogInfo(LogCategory.Core, $"Player assegnato al layer: {LAYER_PLAYER}");
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.Core, $"Layer {LAYER_PLAYER} non trovato! Crea il layer in Project Settings > Tags and Layers");
            }
        }
        else
        {
            SporiumLogger.LogWarning(LogCategory.Core, "Player non trovato con tag 'Player'");
        }
    }
    
    private void SetupPotLayers()
    {
        PotSlot[] allPots = FindObjectsOfType<PotSlot>();
        int interactableLayer = LayerMask.NameToLayer(LAYER_INTERACTABLE);
        
        if (interactableLayer != -1)
        {
            foreach (PotSlot pot in allPots)
            {
                pot.gameObject.layer = interactableLayer;
                
                // Configura anche il collider se presente
                Collider2D collider = pot.GetComponent<Collider2D>();
                if (collider != null)
                {
                    collider.gameObject.layer = interactableLayer;
                }
            }
            
            if (showDebugLogs)
                SporiumLogger.LogInfo(LogCategory.Core, $"{allPots.Length} vasi assegnati al layer: {LAYER_INTERACTABLE}");
        }
        else
        {
            SporiumLogger.LogWarning(LogCategory.Core, $"Layer {LAYER_INTERACTABLE} non trovato! Crea il layer in Project Settings > Tags and Layers");
        }
    }
    
    private void SetupGroundLayer()
    {
        // Verifica se i tag esistono prima di usarli
        string[] availableTags = UnityEditorInternal.InternalEditorUtility.tags;
        bool hasGroundTag = System.Array.Exists(availableTags, tag => tag == "Ground");
        bool hasNavPlaneTag = System.Array.Exists(availableTags, tag => tag == "NAV_Plane");
        
        if (!hasGroundTag && !hasNavPlaneTag)
        {
            if (showDebugLogs)
                SporiumLogger.LogInfo(LogCategory.Core, "Tag 'Ground' e 'NAV_Plane' non definiti. Salto setup ground layer.");
            return;
        }
        
        // Cerca oggetti con tag "Ground" o "NAV_Plane"
        GameObject[] groundObjects = new GameObject[0];
        GameObject[] navPlanes = new GameObject[0];
        
        try
        {
            if (hasGroundTag)
                groundObjects = GameObject.FindGameObjectsWithTag("Ground");
            if (hasNavPlaneTag)
                navPlanes = GameObject.FindGameObjectsWithTag("NAV_Plane");
        }
        catch (System.Exception e)
        {
            if (showDebugLogs)
                SporiumLogger.LogWarning(LogCategory.Core, $"Errore nel cercare oggetti ground: {e.Message}");
            return;
        }
        
        int groundLayer = LayerMask.NameToLayer(LAYER_GROUND);
        
        if (groundLayer != -1)
        {
            int totalGround = 0;
            
            foreach (GameObject ground in groundObjects)
            {
                ground.layer = groundLayer;
                totalGround++;
            }
            
            foreach (GameObject navPlane in navPlanes)
            {
                navPlane.layer = groundLayer;
                totalGround++;
            }
            
            if (showDebugLogs)
                SporiumLogger.LogInfo(LogCategory.Core, $"{totalGround} oggetti ground assegnati al layer: {LAYER_GROUND}");
        }
        else
        {
            SporiumLogger.LogWarning(LogCategory.Core, $"Layer {LAYER_GROUND} non trovato! Crea il layer in Project Settings > Tags and Layers");
        }
    }
    
    /// <summary>
    /// Verifica la configurazione dei layer
    /// </summary>
    [ContextMenu("Verify Layer Setup")]
    public void VerifyLayerSetup()
    {
        SporiumLogger.LogInfo(LogCategory.Core, "=== VERIFICA CONFIGURAZIONE LAYER ===");
        
        // Verifica Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            string playerLayerName = LayerMask.LayerToName(player.layer);
            SporiumLogger.LogInfo(LogCategory.Core, $"Player: Layer {player.layer} ({playerLayerName})");
        }
        
        // Verifica Vasi
        PotSlot[] allPots = FindObjectsOfType<PotSlot>();
        SporiumLogger.LogInfo(LogCategory.Core, $"Vasi trovati: {allPots.Length}");
        foreach (PotSlot pot in allPots)
        {
            string potLayerName = LayerMask.LayerToName(pot.gameObject.layer);
            SporiumLogger.LogInfo(LogCategory.Core, $"  {pot.PotId}: Layer {pot.gameObject.layer} ({potLayerName})");
        }
        
        // Verifica Ground
        GameObject[] groundObjects = new GameObject[0];
        GameObject[] navPlanes = new GameObject[0];
        
        try
        {
            string[] availableTags = UnityEditorInternal.InternalEditorUtility.tags;
            if (System.Array.Exists(availableTags, tag => tag == "Ground"))
                groundObjects = GameObject.FindGameObjectsWithTag("Ground");
            if (System.Array.Exists(availableTags, tag => tag == "NAV_Plane"))
                navPlanes = GameObject.FindGameObjectsWithTag("NAV_Plane");
        }
        catch (System.Exception e)
        {
            SporiumLogger.LogWarning(LogCategory.Core, $"Errore nel verificare tag ground: {e.Message}");
        }
        
        SporiumLogger.LogInfo(LogCategory.Core, $"Ground objects: {groundObjects.Length + navPlanes.Length}");
        
        SporiumLogger.LogInfo(LogCategory.Core, "=== FINE VERIFICA ===");
    }
    
    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Disegna info sui layer in Editor
        if (showDebugLogs)
        {
            PotSlot[] allPots = FindObjectsOfType<PotSlot>();
            foreach (PotSlot pot in allPots)
            {
                string layerName = LayerMask.LayerToName(pot.gameObject.layer);
                UnityEditor.Handles.Label(pot.transform.position + Vector3.up * 1.5f, 
                    $"Layer: {layerName}");
            }
        }
    }
    #endif
}
#endif