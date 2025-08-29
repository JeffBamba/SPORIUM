using UnityEngine;

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
            Debug.Log("[LayerSetup] Iniziando configurazione layer...");
        
        // Configura Player
        SetupPlayerLayer();
        
        // Configura Vasi
        SetupPotLayers();
        
        // Configura Ground
        SetupGroundLayer();
        
        if (showDebugLogs)
            Debug.Log("[LayerSetup] Configurazione layer completata!");
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
                    Debug.Log($"[LayerSetup] Player assegnato al layer: {LAYER_PLAYER}");
            }
            else
            {
                Debug.LogWarning($"[LayerSetup] Layer {LAYER_PLAYER} non trovato! Crea il layer in Project Settings > Tags and Layers");
            }
        }
        else
        {
            Debug.LogWarning("[LayerSetup] Player non trovato con tag 'Player'");
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
                Debug.Log($"[LayerSetup] {allPots.Length} vasi assegnati al layer: {LAYER_INTERACTABLE}");
        }
        else
        {
            Debug.LogWarning($"[LayerSetup] Layer {LAYER_INTERACTABLE} non trovato! Crea il layer in Project Settings > Tags and Layers");
        }
    }
    
    private void SetupGroundLayer()
    {
        // Cerca oggetti con tag "Ground" o "NAV_Plane"
        GameObject[] groundObjects = GameObject.FindGameObjectsWithTag("Ground");
        GameObject[] navPlanes = GameObject.FindGameObjectsWithTag("NAV_Plane");
        
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
                Debug.Log($"[LayerSetup] {totalGround} oggetti ground assegnati al layer: {LAYER_GROUND}");
        }
        else
        {
            Debug.LogWarning($"[LayerSetup] Layer {LAYER_GROUND} non trovato! Crea il layer in Project Settings > Tags and Layers");
        }
    }
    
    /// <summary>
    /// Verifica la configurazione dei layer
    /// </summary>
    [ContextMenu("Verify Layer Setup")]
    public void VerifyLayerSetup()
    {
        Debug.Log("=== VERIFICA CONFIGURAZIONE LAYER ===");
        
        // Verifica Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            string playerLayerName = LayerMask.LayerToName(player.layer);
            Debug.Log($"Player: Layer {player.layer} ({playerLayerName})");
        }
        
        // Verifica Vasi
        PotSlot[] allPots = FindObjectsOfType<PotSlot>();
        Debug.Log($"Vasi trovati: {allPots.Length}");
        foreach (PotSlot pot in allPots)
        {
            string potLayerName = LayerMask.LayerToName(pot.gameObject.layer);
            Debug.Log($"  {pot.PotId}: Layer {pot.gameObject.layer} ({potLayerName})");
        }
        
        // Verifica Ground
        GameObject[] groundObjects = GameObject.FindGameObjectsWithTag("Ground");
        GameObject[] navPlanes = GameObject.FindGameObjectsWithTag("NAV_Plane");
        Debug.Log($"Ground objects: {groundObjects.Length + navPlanes.Length}");
        
        Debug.Log("=== FINE VERIFICA ===");
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
