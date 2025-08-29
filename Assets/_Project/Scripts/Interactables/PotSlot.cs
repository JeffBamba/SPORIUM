using UnityEngine;
using System;

/// <summary>
/// Rappresenta uno slot vaso interagibile nella Dome.
/// Stato iniziale: Empty (nessuna pianta).
/// Gestisce selezione, evidenziazione e eventi per il sistema di piante.
/// </summary>
public class PotSlot : MonoBehaviour
{
    [Header("Pot Configuration")]
    [SerializeField] private string potId = "POT-001";
    [SerializeField] private PotState state = PotState.Empty;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color baseColor = Color.white;
    [SerializeField] private float interactDistance = 1.5f;
    
    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    // Evento statico per la selezione del vaso
    public static event Action<PotSlot> OnPotSelected;
    
    // Riferimento alla pianta (null per ora, da implementare in BLK-01.04)
    private GameObject plantInstance;
    
    // Proprietà pubbliche
    public string PotId => potId;
    public PotState State => state;
    public bool IsEmpty => state == PotState.Empty;
    
    // Cache del player per controllo distanza
    private Transform playerTransform;
    
    void Awake()
    {
        // Trova il player se presente
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning($"[PotSlot] Player non trovato con tag 'Player'. Selezione sempre permessa.");
        }
        
        // Trova SpriteRenderer se non assegnato
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        // Imposta colore base
        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }
    }
    
    void OnMouseEnter()
    {
        // Evidenzia il vaso al passaggio del mouse
        if (spriteRenderer != null)
        {
            spriteRenderer.color = highlightColor;
        }
    }
    
    void OnMouseExit()
    {
        // Ripristina colore base
        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }
    }
    
    void OnMouseDown()
    {
        // Gestisce il click sul vaso
        HandlePotClick();
    }
    
    private void HandlePotClick()
    {
        // Controlla distanza dal player se disponibile
        if (playerTransform != null)
        {
            float distance = Vector2.Distance(playerTransform.position, transform.position);
            if (distance > interactDistance)
            {
                Debug.Log($"[{potId}] Troppo lontano (>= {interactDistance})");
                return;
            }
        }
        
        // Seleziona il vaso
        SelectPot();
    }
    
    /// <summary>
    /// Seleziona il vaso (pubblico per testing)
    /// </summary>
    public void SelectPot()
    {
        Debug.Log($"[{potId}] Selected (state: {state})");
        
        // Evidenzia visivamente
        if (spriteRenderer != null)
        {
            spriteRenderer.color = highlightColor;
        }
        
        // Notifica la selezione
        OnPotSelected?.Invoke(this);
    }
    
    /// <summary>
    /// Imposta lo stato del vaso (da usare in BLK-01.02+)
    /// </summary>
    public void SetState(PotState newState)
    {
        state = newState;
        Debug.Log($"[{potId}] Stato cambiato a: {state}");
    }
    
    /// <summary>
    /// Imposta l'ID del vaso (da usare nel bootstrap)
    /// </summary>
    public void SetPotId(string newId)
    {
        potId = newId;
    }
    
    /// <summary>
    /// Pulisce la selezione (ripristina colore base)
    /// </summary>
    public void ClearSelection()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }
    }
    
    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Disegna cerchio per visibilità in Editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // Disegna label con ID del vaso
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, potId);
        
        // Disegna raggio di interazione
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
    #endif
}

/// <summary>
/// Stati possibili del vaso (da estendere in BLK-01.04)
/// </summary>
public enum PotState
{
    Empty,      // Vaso vuoto
    Occupied,   // Vaso occupato (da implementare)
    Growing,    // Pianta in crescita (da implementare)
    Mature      // Pianta matura (da implementare)
}
