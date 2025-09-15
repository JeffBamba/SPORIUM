using UnityEngine;
using System;
using _Project;
using Sporae.Core;
using TMPro;

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
    [SerializeField] private float interactDistance = 2.0f; // Sincronizzato con PotSystemConfig
    
    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private TextMeshProUGUI _amountOfFruits;
    
    // Evento statico per la selezione del vaso
    public static event Action<PotSlot> OnPotSelected;
    
    // Riferimento alla pianta (null per ora, da implementare in BLK-01.04)
    private GameObject plantInstance;
    
    // Riferimento al PotActions (BLK-01.02)
    private PotActions potActions;
    
    // Proprietà pubbliche
    public string PotId => potId;
    public PotState State => state;
    public bool IsEmpty => state == PotState.Empty;
    
    // Proprietà per BLK-01.02
    public PotActions PotActions => potActions;
    public bool InRange => IsPlayerInRange();
    public bool IsSelected { get; private set; } = false;
    
    // Cache del player per controllo distanza
    private Transform playerTransform;
    
    private GameManager gameManager;
    private UINotification uiNotification;
    
    void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
        uiNotification = FindObjectOfType<UINotification>();
        
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
        
        // Trova PotActions se presente
        potActions = GetComponent<PotActions>();
        
        // Imposta colore base
        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }
    }
    
    private void Start() {
        gameManager.OnDayChanged += HandleDayChanged;   
    }

    private void HandleDayChanged(int obj)
    {
        bool isMature = PotActions.PotState.Stage == (int)PotState.Mature;
        bool hasFruits = PotActions.PotState.AmountFruits > 0;
        
        _amountOfFruits.text = (isMature && hasFruits) ? 
                $"{PotActions.PotState.AmountFruits.ToString()}+" : "";
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
        // IMPORTANTE: Blocca selezione se il click è sopra UI
        if (UIBlocker.IsPointerOverUI())
        {
            Debug.Log($"[{potId}] Click bloccato: sopra UI");
            return;
        }
        
        // Controlla distanza dal player se disponibile
        if (playerTransform != null)
        {
            float distance = Vector2.Distance(playerTransform.position, transform.position);
            Debug.Log($"[{potId}] Click rilevato - Distanza: {distance:F2}, Max: {interactDistance}");
            if (distance > interactDistance)
            {
                Debug.Log($"[{potId}] Troppo lontano (>= {interactDistance})");
                return;
            }
        }
        else
        {
            Debug.Log($"[{potId}] Click rilevato - Player non trovato, selezione permessa");
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
        
        // Pulisci selezione precedente su altri vasi
        ClearAllSelections();
        
        // Imposta questo vaso come selezionato
        IsSelected = true;
        
        // Evidenzia visivamente
        if (spriteRenderer != null)
        {
            spriteRenderer.color = highlightColor;
        }

        if (PotActions.PotState.AmountFruits != 0)
        {
            uiNotification.ShowNotification($"New Fruit added to Inventory: {PotActions.PotState.AmountFruits}", 3f, Color.green);
            gameManager.AddItem("Fruits", PotActions.PotState.AmountFruits);
            PotActions.PotState.AmountFruits = 0;
            _amountOfFruits.text = "";
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
        IsSelected = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }
    }
    
    /// <summary>
    /// Pulisce la selezione di tutti i vasi
    /// </summary>
    private void ClearAllSelections()
    {
        PotSlot[] allPots = FindObjectsOfType<PotSlot>();
        foreach (PotSlot pot in allPots)
        {
            if (pot != this)
            {
                pot.ClearSelection();
            }
        }
    }
    
    /// <summary>
    /// Verifica se il player è in range per interagire
    /// </summary>
    public bool IsPlayerInRange()
    {
        if (playerTransform == null) return true; // Se non c'è player, sempre in range
        
        float distance = Vector2.Distance(playerTransform.position, transform.position);
        return distance <= interactDistance;
    }
    
    /// <summary>
    /// Restituisce la distanza dal player
    /// </summary>
    public float GetDistanceFromPlayer()
    {
        if (playerTransform == null) return 0f;
        
        return Vector2.Distance(playerTransform.position, transform.position);
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
