using UnityEngine;

using System;

using _Project;
using _Project.Sporae.Core;
using Sporae.Dome.PotSystem.Growth;

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
    private SpriteRenderer _spriteRenderer;
    [SerializeField] private TextMeshProUGUI _amountOfFruits;
    
    public Sprite Sprite => _spriteRenderer.sprite;
    
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
    private Inventory _inventory;
    private DayCycleSystem _dayCycleSystem;
    private DiaryStatistics _diaryStatistics;
    
    private void Awake()
    {
        _gameManager = FindObjectOfType<GameManager>();
        _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
        _diaryStatistics = ServiceContainer.Instance.Get<DiaryStatistics>();
        
        _inventory = _gameManager.PlayerInventory;
        
        _uiNotification = FindObjectOfType<UINotification>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _interactable = GetComponent<Interactable>();
        _potActions = GetComponent<PotActions>();
    }
    
    private void Start()
    {
        if (_interactable != null)
            _interactable.OnInteract += HandleInteract;
        
        if (_dayCycleSystem != null)
            _dayCycleSystem.OnDayChanged += HandleDayChanged;
        
        // BLK-02.05: Sottoscrivi agli eventi per aggiornare il display dei frutti
        PotEvents.OnPotStateChanged += OnPotStateChanged;
        PotEvents.OnPotAction += OnPotAction;
        PotEvents.OnPlantStageChanged += OnPlantStageChanged;
        
        // Aggiorna il display iniziale
        UpdateFruitDisplay();
    }

    private void OnDestroy()
    {
        if (_interactable != null)
            _interactable.OnInteract -= HandleInteract;
        
        if (_dayCycleSystem != null)
            _dayCycleSystem.OnDayChanged -= HandleDayChanged;
        
        // BLK-02.05: Annulla sottoscrizioni eventi
        PotEvents.OnPotStateChanged -= OnPotStateChanged;
        PotEvents.OnPotAction -= OnPotAction;
        PotEvents.OnPlantStageChanged -= OnPlantStageChanged;
    }

    private void HandleInteract()
    {
        SelectPot();
    }

    private void HandleDayChanged(int obj)
    {
        // BLK-02.05: Aggiorna il display dei frutti quando cambia il giorno
        UpdateFruitDisplay();
    }
    
    /// <summary>
    /// BLK-02.05: Gestisce il cambio di stato del vaso
    /// </summary>
    private void OnPotStateChanged(PotSlot pot)
    {
        // Aggiorna solo se è questo vaso
        if (pot == this)
        {
            UpdateFruitDisplay();
        }
    }
    
    /// <summary>
    /// BLK-02.05: Gestisce le azioni sul vaso (es. Harvest)
    /// </summary>
    private void OnPotAction(PotEvents.PotActionType actionType, PotSlot pot)
    {
        // Aggiorna solo se è questo vaso e l'azione è Harvest
        if (pot == this && actionType == PotEvents.PotActionType.Harvest)
        {
            UpdateFruitDisplay();
        }
    }
    
    /// <summary>
    /// BLK-02.05: Gestisce il cambio di stadio della pianta
    /// </summary>
    private void OnPlantStageChanged(string potId, PlantStage stage)
    {
        // Aggiorna solo se è questo vaso
        if (potId == this.potId)
        {
            UpdateFruitDisplay();
        }
    }
    
    /// <summary>
    /// BLK-02.05: Aggiorna il display del numero di frutti sopra il pot
    /// Mostra "+1", "+2", "+3" quando la pianta è in HarvestReady e ha frutti disponibili
    /// </summary>
    private void UpdateFruitDisplay()
    {
        if (_amountOfFruits == null || _potActions == null || _potActions.PotState == null)
        {
            return;
        }
        
        var potState = _potActions.PotState;
        
        // Mostra il numero di frutti solo se:
        // 1. La pianta è in HarvestReady
        // 2. Ci sono frutti disponibili (>= 1)
        bool isHarvestReady = potState.Stage == (int)PlantStage.HarvestReady;
        bool hasFruits = potState.AmountFruits >= 1f;
        
        if (isHarvestReady && hasFruits)
        {
            int fruitCount = Mathf.RoundToInt(potState.AmountFruits);
            fruitCount = Mathf.Clamp(fruitCount, 1, 3); // Limita a max 3
            
            // Mostra "+1", "+2", "+3" sopra il pot
            _amountOfFruits.text = $"+{fruitCount}";
            _amountOfFruits.gameObject.SetActive(true);
        }
        else
        {
            // Nascondi il testo se non ci sono frutti o non è in HarvestReady
            _amountOfFruits.text = "";
            _amountOfFruits.gameObject.SetActive(false);
        }
    }

    private void CollectFruits()
    {
        if (PotActions.PotState.AmountFruits < 1)
            return;

        int amount = (int)PotActions.PotState.AmountFruits;

        _diaryStatistics.FruitsHarvested += amount;
        
        _uiNotification.ShowNotification($"New Fruit added to Inventory: {amount}", 3f, Color.green);
        _inventory.Add(Items.Fruits, amount);
        PotActions.PotState.AmountFruits -= amount;
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

        // NOTA: Non raccogliere automaticamente i frutti qui!
        // L'harvest deve essere gestito tramite il widget UI (PotHUDWidget/PotDetailsWidget)
        // quando l'utente clicca esplicitamente sul pulsante "Harvest"
        // CollectFruits() è stato rimosso per permettere all'utente di scegliere se fare harvest o vedere i dettagli

        // Notifica la selezione - questo farà apparire il widget con i pulsanti di azione
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
