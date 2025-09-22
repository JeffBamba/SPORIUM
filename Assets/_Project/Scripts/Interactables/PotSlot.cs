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
    
    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TextMeshProUGUI _amountOfFruits;
    
    // Evento statico per la selezione del vaso
    public static event Action<PotSlot> OnPotSelected;
    
    // Proprietà pubbliche
    public string PotId => potId;
    public PotState State => state;
    public bool IsEmpty => state == PotState.Empty;
    
    // Proprietà per BLK-01.02
    public PotActions PotActions => _potActions;
    public Interactable Interactable => _interactable;
    
    public bool IsSelected { get; private set; } = false;
    
    private GameObject _plantInstance;
    private PotActions _potActions;
    private GameManager _gameManager;
    private UINotification _uiNotification;
    private Interactable _interactable;
    
    
    private void Awake()
    {
        _gameManager = FindObjectOfType<GameManager>();
        _uiNotification = FindObjectOfType<UINotification>();
        _interactable = GetComponent<Interactable>();
        _potActions = GetComponent<PotActions>();
    }
    
    private void Start()
    {
        _interactable.OnInteract += HandleInteract;
        _gameManager.OnDayChanged += HandleDayChanged;   
    }

    private void OnDestroy()
    {
        _interactable.OnInteract -= HandleInteract;
        _gameManager.OnDayChanged -= HandleDayChanged;
    }

    private void HandleInteract()
    {
        SelectPot();
    }

    private void HandleDayChanged(int obj)
    {
        bool isMature = PotActions.PotState.Stage == (int)PotState.Mature;
        bool hasFruits = PotActions.PotState.AmountFruits >= 1;
        
        _amountOfFruits.text = (isMature && hasFruits) ? 
                $"{(int)PotActions.PotState.AmountFruits}+" : "";
    }

    private void CollectFruits()
    {
        if (PotActions.PotState.AmountFruits < 1)
            return;
        
        _uiNotification.ShowNotification($"New Fruit added to Inventory: {(int)PotActions.PotState.AmountFruits}", 3f, Color.green);
        _gameManager.AddItem("Fruits", (int)PotActions.PotState.AmountFruits);
        PotActions.PotState.AmountFruits -= (int)PotActions.PotState.AmountFruits;
        _amountOfFruits.text = "";
    }
    
    /// <summary>
    /// Seleziona il vaso (pubblico per testing)
    /// </summary>
    private void SelectPot()
    {
        Debug.Log($"[{potId}] Selected (state: {state})");
        
        // Pulisci selezione precedente su altri vasi
        ClearAllSelections();
        
        // Imposta questo vaso come selezionato
        IsSelected = true;

        //
        CollectFruits();

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
        _interactable.Deselect();
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
    
    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Disegna cerchio per visibilità in Editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // Disegna label con ID del vaso
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, potId);
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
