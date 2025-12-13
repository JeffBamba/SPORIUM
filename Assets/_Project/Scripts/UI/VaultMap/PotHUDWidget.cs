using System.IO;
using _Project.Sporae.Core;
using _Project;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sporae.Dome.PotSystem;
using Sporae.Dome.PotSystem.Growth;
using Sporae.Dome.PotSystem.Condition;
using Sporae.Dome.UI;
using Sporae.DevTools;

/// <summary>
/// Widget UI minimale che mostra informazioni sul vaso selezionato.
/// Si integra con l'HUD esistente o crea un fallback se necessario.
/// BLK-01.03B: Esteso con Stage label, Stage icon, Progress bar e PotId attivo.
/// </summary>
public class PotHUDWidget : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI potInfoText;
    [SerializeField] private Image backgroundImage;
    
    [Header("BLK-01.03B - Stage & Progress UI")]
    [SerializeField] private Image stageIcon;
    [SerializeField] private TextMeshProUGUI stageLabel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI potIdText;
    [SerializeField] private TextMeshProUGUI progressText;
    
    [Header("Plant Stats UI")]
    [SerializeField] private TextMeshProUGUI hydrationText;
    [SerializeField] private TextMeshProUGUI lightExposureText;
    [SerializeField] private TextMeshProUGUI phDriftText;
    [SerializeField] private TextMeshProUGUI fertilizerText;  // BLK-03.01-T1
    [SerializeField] private TextMeshProUGUI growthPointsText;  // BLK-03.01-T2
    [SerializeField] private TextMeshProUGUI optimalDaysText;  // BLK-03.01-T2
    [SerializeField] private TextMeshProUGUI plantLevelText;  // BLK-02.02: Livello pianta (1-5)
    [SerializeField] private TextMeshProUGUI moldRiskText;  // BLK-07.01: Mold risk indicator
    [SerializeField] private GameObject infestationBadge;  // BLK-07.01: Badge "INFESTATA"
    
    [Header("Growth Tooltip UI")]
    [SerializeField] private GameObject growthTooltipPanel;
    [SerializeField] private TextMeshProUGUI growthTooltipText;
    [SerializeField] private TextMeshProUGUI growthLabelText;
    
    [Header("Plant Condition UI")]
    [SerializeField] private TextMeshProUGUI conditionLabelText;
    [SerializeField] private Slider conditionBar;
    [SerializeField] private TextMeshProUGUI conditionTooltipText;
    [SerializeField] private GameObject conditionTooltipPanel;
    
    [Header("Action Buttons (BLK-01.02)")]
    [SerializeField] private Button btnPlant;
    [SerializeField] private Button btnWater;
    [SerializeField] private Button btnLight;
    [SerializeField] private Button btnSpray;
    [SerializeField] private Button btnHarvest;
    [SerializeField] private Button btnFertilize;  // BLK-03.01-T1
    [SerializeField] private Button btnPruning;  // AZ-13
    [SerializeField] private TextMeshProUGUI txtCosts;
    
    [Header("Pruning Dialog (AZ-13)")]
    [SerializeField] private PruningDialog pruningDialogPrefab;
    private PruningDialog _pruningDialogInstance;
    
    [Header("Seed Selector")]
    [SerializeField] private UISeedSelector seedSelector;
    
    [Header("Fertilizer Selector (BLK-03.01-T1)")]
    [SerializeField] private UIFertilizerSelector fertilizerSelector;
    
    [Header("Widget Settings")]
    [SerializeField] private Vector2 widgetPosition = new Vector2(12, 12);
    [SerializeField] private Vector2 widgetSize = new Vector2(460, 120); // Aumentato per contenere pulsanti Spray e Harvest
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.8f);
    [SerializeField] private Color textColor = Color.white;
    
    [Header("Fallback Settings")]
    [SerializeField] private bool createFallbackCanvas = true;
    [SerializeField] private string fallbackCanvasName = "PotHUD_Fallback";
    
    private GameObject _widgetContainer;
    private Canvas _parentCanvas;
    private bool _isInitialized;
    private PotSlot _currentSelectedPot;
    private PlantGrowthConfig _growthConfig;
    private GameManager _gameManager;
    private DayCycleSystem _dayCycleSystem;
    private PhSystem _phSystem;
    private PotSystemConfig _potSystemConfig;
    
    private void Start()
    {
        gameObject.SetActive(false);
        InitializeWidget();
        LoadGrowthConfig();
        InitializeSeedSelector();
        InitializeFertilizerSelector();  // BLK-03.01-T1
    }
    
    /// <summary>
    /// Inizializza il selettore semi se non assegnato
    /// </summary>
    private void InitializeSeedSelector()
    {
        if (seedSelector == null)
        {
            // Cerca UISeedSelector nella scena
            seedSelector = FindObjectOfType<UISeedSelector>();
            
            if (seedSelector == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "UISeedSelector non trovato nella scena! " +
                    "Devi creare un GameObject 'UISeedSelector' nella scena con il componente UISeedSelector " +
                    "e collegare tutti i riferimenti UI necessari. Vedi le istruzioni in Assets/Docs/UISeedSelector_Setup.md");
                return;
            }
            
            // Sottoscrivi agli eventi se già esistente
            seedSelector.OnSeedSelected += OnSeedSelected;
            seedSelector.OnCancelled += OnSeedSelectionCancelled;
        }
        else
        {
            // Sottoscrivi agli eventi
            seedSelector.OnSeedSelected += OnSeedSelected;
            seedSelector.OnCancelled += OnSeedSelectionCancelled;
        }
    }
    
    /// <summary>
    /// Apre il selettore semi per il vaso specificato
    /// </summary>
    private void OpenSeedSelector(PotSlot targetPot)
    {
        SporiumLogger.LogDebug(LogCategory.UI, $"OpenSeedSelector chiamato per vaso {targetPot?.PotId ?? "NULL"}");
        
        // Assicurati che il selettore sia inizializzato
        if (seedSelector == null)
        {
            SporiumLogger.LogDebug(LogCategory.UI, "Inizializzazione seed selector...");
            InitializeSeedSelector();
        }
        
        if (seedSelector == null)
        {
            SporiumLogger.LogError(LogCategory.UI, "UISeedSelector non disponibile dopo inizializzazione!");
            return;
        }
        
        // Rassicurati che gli eventi siano sempre sottoscritti (in caso di ricreazione)
        seedSelector.OnSeedSelected -= OnSeedSelected; // Rimuovi prima per evitare duplicati
        seedSelector.OnSeedSelected += OnSeedSelected;
        seedSelector.OnCancelled -= OnSeedSelectionCancelled;
        seedSelector.OnCancelled += OnSeedSelectionCancelled;
        
        SporiumLogger.LogDebug(LogCategory.UI, "Eventi sottoscritti correttamente");
        SporiumLogger.LogDebug(LogCategory.UI, $"Apertura selettore semi per vaso {targetPot?.PotId}");
        
        // Salva il vaso corrente prima di aprire il selettore
        _currentSelectedPot = targetPot;
        
        seedSelector.Show(targetPot);
    }
    
    /// <summary>
    /// Gestisce la selezione di un seme
    /// </summary>
    private void OnSeedSelected(string seedTypeId)
    {
        SporiumLogger.LogDebug(LogCategory.UI, $"OnSeedSelected chiamato con seedTypeId: {seedTypeId}");
        SporiumLogger.LogDebug(LogCategory.UI, $"_currentSelectedPot: {_currentSelectedPot?.PotId ?? "NULL"}");
        
        // Usa _currentSelectedPot invece di FindSelectedPot() per evitare problemi di timing
        if (_currentSelectedPot == null)
        {
            SporiumLogger.LogError(LogCategory.UI, "_currentSelectedPot è NULL quando seme selezionato!");
            return;
        }
        
        if (_currentSelectedPot.PotActions == null)
        {
            SporiumLogger.LogError(LogCategory.UI, "PotActions è NULL quando seme selezionato!");
            return;
        }
        
        SporiumLogger.LogInfo(LogCategory.UI, $"Piantando seme {seedTypeId} nel vaso {_currentSelectedPot.PotId}");
        
        // Piantare il seme selezionato
        bool success = _currentSelectedPot.PotActions.DoPlant(seedTypeId);
        
        if (success)
        {
            SporiumLogger.LogInfo(LogCategory.UI, $"Seme {seedTypeId} piantato con successo!");
            // Aggiorna l'UI
            UpdateActionButtons(_currentSelectedPot);
            
            var growthController = _currentSelectedPot.GetComponent<PotGrowthController>();
            if (growthController != null)
                UpdateStageAndProgressUI(_currentSelectedPot);
        }
        else
        {
            SporiumLogger.LogError(LogCategory.UI, $"Fallito piantare seme {seedTypeId}! Verifica i log di PotActions per dettagli.");
        }
    }
    
    /// <summary>
    /// Gestisce l'annullamento della selezione seme
    /// </summary>
    private void OnSeedSelectionCancelled()
    {
        SporiumLogger.LogDebug(LogCategory.UI, "Selezione seme annullata");
        // Nessuna azione necessaria
    }
    
    /// <summary>
    /// Inizializza il selettore fertilizzante se non assegnato
    /// BLK-03.01-T1: Crea automaticamente se non esiste nella scena
    /// </summary>
    private void InitializeFertilizerSelector()
    {
        if (fertilizerSelector == null)
        {
            // Cerca UIFertilizerSelector nella scena
            fertilizerSelector = FindObjectOfType<UIFertilizerSelector>();
            
            if (fertilizerSelector == null)
            {
                // Crea automaticamente il GameObject con il componente
                SporiumLogger.LogWarning(LogCategory.UI, "UIFertilizerSelector non trovato nella scena. Creazione automatica...");
                GameObject fertilizerSelectorGO = new GameObject("UIFertilizerSelector");
                fertilizerSelector = fertilizerSelectorGO.AddComponent<UIFertilizerSelector>();
                SporiumLogger.LogInfo(LogCategory.UI, "UIFertilizerSelector creato automaticamente!");
            }
        }
        
        // Sottoscrivi agli eventi
        if (fertilizerSelector != null)
        {
            fertilizerSelector.OnFertilizerSelected += OnFertilizerSelected;
            fertilizerSelector.OnCancelled += OnFertilizerSelectionCancelled;
        }
    }
    
    /// <summary>
    /// BLK-03.01-T1: Apre selettore fertilizzante (mostra inventario fertilizzanti)
    /// </summary>
    private void OpenFertilizerSelector(PotSlot targetPot)
    {
        SporiumLogger.LogDebug(LogCategory.UI, $"OpenFertilizerSelector chiamato per vaso {targetPot?.PotId ?? "NULL"}");
        
        // Assicurati che il selettore sia inizializzato
        if (fertilizerSelector == null)
        {
            SporiumLogger.LogDebug(LogCategory.UI, "Inizializzazione fertilizer selector...");
            InitializeFertilizerSelector();
        }
        
        if (fertilizerSelector == null)
        {
            SporiumLogger.LogError(LogCategory.UI, "UIFertilizerSelector non disponibile dopo inizializzazione!");
            return;
        }
        
        // Rassicurati che gli eventi siano sempre sottoscritti (in caso di ricreazione)
        fertilizerSelector.OnFertilizerSelected -= OnFertilizerSelected; // Rimuovi prima per evitare duplicati
        fertilizerSelector.OnFertilizerSelected += OnFertilizerSelected;
        fertilizerSelector.OnCancelled -= OnFertilizerSelectionCancelled;
        fertilizerSelector.OnCancelled += OnFertilizerSelectionCancelled;
        
        SporiumLogger.LogDebug(LogCategory.UI, "Eventi sottoscritti correttamente");
        SporiumLogger.LogDebug(LogCategory.UI, $"Apertura selettore fertilizzanti per vaso {targetPot?.PotId}");
        
        // Salva il vaso corrente prima di aprire il selettore
        _currentSelectedPot = targetPot;
        
        fertilizerSelector.Show(targetPot);
    }
    
    /// <summary>
    /// AZ-13: Apre il dialog di potatura con opzione Spray
    /// </summary>
    private void OpenPruningDialog(PotSlot targetPot)
    {
        SporiumLogger.LogDebug(LogCategory.UI, $"OpenPruningDialog chiamato per vaso {targetPot?.PotId ?? "NULL"}");
        
        // Crea istanza dialog se non esiste
        if (_pruningDialogInstance == null)
        {
            if (pruningDialogPrefab != null)
            {
                // BUG FIX: Istanzia nel Canvas root invece che nel transform corrente
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas == null)
                    canvas = FindObjectOfType<Canvas>();
                
                if (canvas != null)
                {
                    _pruningDialogInstance = Instantiate(pruningDialogPrefab, canvas.transform);
                }
                else
                {
                    _pruningDialogInstance = Instantiate(pruningDialogPrefab, transform);
                }
                
                // BUG FIX: Assicurati che il GameObject root sia attivo
                if (_pruningDialogInstance != null)
                {
                    _pruningDialogInstance.gameObject.SetActive(true);
                }
            }
            else
            {
                // Cerca dialog nella scena
                _pruningDialogInstance = FindObjectOfType<PruningDialog>();
                if (_pruningDialogInstance == null)
                {
                    SporiumLogger.LogError(LogCategory.UI, "PruningDialog non trovato! Assicurati di avere il prefab assegnato o un'istanza nella scena.");
                    return;
                }
            }
        }
        
        // BUG FIX: Assicurati che il dialog sia attivo prima di mostrarlo
        if (_pruningDialogInstance != null)
        {
            _pruningDialogInstance.gameObject.SetActive(true);
        }
        
        // Verifica disponibilità STR-004
        bool hasSpray = targetPot?.PotActions?.HasSprayAntifungal() ?? false;
        
        // Sottoscrivi eventi
        _pruningDialogInstance.OnDialogResult -= OnPruningDialogResult; // Rimuovi prima per evitare duplicati
        _pruningDialogInstance.OnDialogResult += OnPruningDialogResult;
        
        // Salva il vaso corrente
        _currentSelectedPot = targetPot;
        
        // Mostra dialog
        _pruningDialogInstance.Show(hasSpray);
    }
    
    /// <summary>
    /// AZ-13: Gestisce il risultato del dialog potatura
    /// </summary>
    private void OnPruningDialogResult(bool confirmed, bool useSpray)
    {
        SporiumLogger.LogDebug(LogCategory.UI, $"OnPruningDialogResult: confirmed={confirmed}, useSpray={useSpray}");
        
        if (!confirmed)
        {
            SporiumLogger.LogDebug(LogCategory.UI, "Potatura annullata dall'utente");
            return;
        }
        
        if (_currentSelectedPot == null || _currentSelectedPot.PotActions == null)
        {
            SporiumLogger.LogError(LogCategory.UI, "_currentSelectedPot è NULL quando potatura confermata!");
            return;
        }
        
        // Esegui potatura
        bool success = _currentSelectedPot.PotActions.DoPruning(useSpray);
        
        if (success)
        {
            SporiumLogger.LogInfo(LogCategory.UI, $"Potatura eseguita con successo (useSpray={useSpray})");
            // Aggiorna UI
            UpdateActionButtons(_currentSelectedPot);
            UpdateStageAndProgressUI(_currentSelectedPot);
        }
        else
        {
            SporiumLogger.LogWarning(LogCategory.UI, "Potatura fallita");
        }
    }
    
    /// <summary>
    /// Gestisce la selezione di un fertilizzante
    /// </summary>
    private void OnFertilizerSelected(string fertilizerTypeId)
    {
        SporiumLogger.LogDebug(LogCategory.UI, $"OnFertilizerSelected chiamato con fertilizerTypeId: {fertilizerTypeId}");
        SporiumLogger.LogDebug(LogCategory.UI, $"_currentSelectedPot: {_currentSelectedPot?.PotId ?? "NULL"}");
        
        // Usa _currentSelectedPot invece di FindSelectedPot() per evitare problemi di timing
        if (_currentSelectedPot == null)
        {
            SporiumLogger.LogError(LogCategory.UI, "_currentSelectedPot è NULL quando fertilizzante selezionato!");
            return;
        }
        
        if (_currentSelectedPot.PotActions == null)
        {
            SporiumLogger.LogError(LogCategory.UI, "PotActions è NULL quando fertilizzante selezionato!");
            return;
        }
        
        SporiumLogger.LogInfo(LogCategory.UI, $"Applicando fertilizzante {fertilizerTypeId} al vaso {_currentSelectedPot.PotId}");
        
        // Applica il fertilizzante selezionato
        bool success = _currentSelectedPot.PotActions.DoFertilize(fertilizerTypeId);
        
        if (success)
        {
            SporiumLogger.LogInfo(LogCategory.UI, "Fertilizzante applicato con successo!");
            // Aggiorna l'UI
            UpdateActionButtons(_currentSelectedPot);
            UpdateStageAndProgressUI(_currentSelectedPot);
        }
        else
        {
            SporiumLogger.LogError(LogCategory.UI, $"Fallito applicare fertilizzante {fertilizerTypeId}! Verifica i log di PotActions per dettagli.");
        }
    }
    
    /// <summary>
    /// Gestisce l'annullamento della selezione fertilizzante
    /// </summary>
    private void OnFertilizerSelectionCancelled()
    {
        SporiumLogger.LogDebug(LogCategory.UI, "Selezione fertilizzante annullata");
        // Nessuna azione necessaria
    }
    
    private void OnEnable()
    {
        // Sottoscrivi agli eventi del sistema dei vasi
        PotSlot.OnPotSelected += OnPotSelected;
        PotEvents.OnPotStateChanged += OnPotStateChanged;
        PotEvents.OnPotActionFailed += OnPotActionFailed;
        PotEvents.OnPlantGrew += OnPlantGrew;
        PotEvents.OnPlantStageChanged += OnPlantStageChanged;
    }
    
    private void OnDisable()
    {
        // Annulla sottoscrizioni
        PotSlot.OnPotSelected -= OnPotSelected;
        PotEvents.OnPotStateChanged -= OnPotStateChanged;
        PotEvents.OnPotActionFailed -= OnPotActionFailed;
        PotEvents.OnPlantGrew -= OnPlantGrew;
        PotEvents.OnPlantStageChanged -= OnPlantStageChanged;
    }
    
    private void LoadGrowthConfig()
    {
        // Carica la configurazione di crescita
        _growthConfig = Resources.Load<PlantGrowthConfig>("Configs/PlantGrowthConfig_Default");
        if (_growthConfig != null)
        {
            SporiumLogger.LogDebug(LogCategory.UI, $"Config caricata: pointsSeedToSprout={_growthConfig.pointsSeedToSprout}, pointsSproutToMature={_growthConfig.pointsSproutToMature}");
        }
        else
        {
            SporiumLogger.LogWarning(LogCategory.UI, "PlantGrowthConfig non trovato in Resources/Configs/. Usando valori di default.");
            // Crea configurazione di fallback
            _growthConfig = ScriptableObject.CreateInstance<PlantGrowthConfig>();
        }
    }
    
    private void InitializeWidget()
    {
        _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
        _phSystem = ServiceContainer.Instance.Get<PhSystem>(suppressWarning: true);
        _potSystemConfig = Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");
        
        _gameManager = FindObjectOfType<GameManager>();
        if (_gameManager != null)
            _dayCycleSystem.OnDayChanged += HandleDayChanged;
        else
        {
            SporiumLogger.LogError(LogCategory.UI, "DayCycleController: GameManager non trovato!");
        }
        
        _parentCanvas = FindParentCanvas();
        
        if (_parentCanvas == null && createFallbackCanvas)
        {
            // Crea un Canvas di fallback se non ne trova nessuno
            CreateFallbackCanvas();
        }
        
        if (_parentCanvas == null)
        {
            SporiumLogger.LogError(LogCategory.UI, "Impossibile trovare o creare un Canvas. Widget disabilitato.");
            enabled = false;
            return;
        }
        
        // Crea il widget UI
        CreateWidgetUI();
        
        // Imposta testo iniziale
        UpdatePotInfo("Nessun POT selezionato");
        
        _isInitialized = true;
        SporiumLogger.LogInfo(LogCategory.UI, "Widget inizializzato correttamente.");
    }

    private void HandleDayChanged(int currentDay)
    {
        if (!_currentSelectedPot)
            return;
        
        UpdateActionButtons(_currentSelectedPot);
        UpdateStageAndProgressUI(_currentSelectedPot);
    }

    private Canvas FindParentCanvas()
    {
        // Cerca prima nell'HUD esistente
        HUDController hudController = FindObjectOfType<HUDController>();
        if (hudController != null)
        {
            Canvas hudCanvas = hudController.GetComponentInParent<Canvas>();
            if (hudCanvas != null)
            {
                SporiumLogger.LogDebug(LogCategory.UI, "Trovato Canvas dell'HUD esistente.");
                return hudCanvas;
            }
        }
        
        // Cerca qualsiasi Canvas nella scena
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || 
                canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                SporiumLogger.LogDebug(LogCategory.UI, $"Trovato Canvas: {canvas.name}");
                return canvas;
            }
        }
        
        SporiumLogger.LogWarning(LogCategory.UI, "Nessun Canvas trovato nella scena.");
        return null;
    }
    
    private void CreateFallbackCanvas()
    {
        // Crea un GameObject per il Canvas
        GameObject canvasGO = new GameObject(fallbackCanvasName);
        _parentCanvas = canvasGO.AddComponent<Canvas>();
        _parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _parentCanvas.sortingOrder = 100; // Alto z-order per essere sopra tutto
        
        // Aggiungi CanvasScaler per responsive design
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Aggiungi GraphicRaycaster per interazioni UI
        canvasGO.AddComponent<GraphicRaycaster>();
        
        SporiumLogger.LogInfo(LogCategory.UI, "Creato Canvas di fallback.");
    }
    
    private void CreateWidgetUI()
    {
        // Crea il container del widget
        _widgetContainer = new GameObject("UI_PotInfo");
        _widgetContainer.transform.SetParent(_parentCanvas.transform, false);
        
        // Aggiungi RectTransform
        RectTransform rectTransform = _widgetContainer.AddComponent<RectTransform>();
        
        // Posiziona in basso-sinistra
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.pivot = new Vector2(0, 0);
        rectTransform.anchoredPosition = widgetPosition;
        rectTransform.sizeDelta = widgetSize;
        
        // IMPORTANTE: Assicurati che il Canvas abbia GraphicRaycaster per i click UI
        if (_parentCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            SporiumLogger.LogWarning(LogCategory.UI, "Aggiungendo GraphicRaycaster al Canvas per i click UI");
            _parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }
        
        // Crea background (opzionale)
        if (backgroundImage == null)
        {
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(_widgetContainer.transform, false);
            
            backgroundImage = bgGO.AddComponent<Image>();
            backgroundImage.color = backgroundColor;
            
            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
        }
        
        // Crea testo se non assegnato
        if (potInfoText == null)
        {
            GameObject textGO = new GameObject("PotInfoText");
            textGO.transform.SetParent(_widgetContainer.transform, false);
            
            potInfoText = textGO.AddComponent<TextMeshProUGUI>();
            potInfoText.color = textColor;
            potInfoText.fontSize = 16;
            potInfoText.alignment = TextAlignmentOptions.Left;
            potInfoText.text = "Nessun vaso selezionato";
            
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);
        }
        
        // Crea pulsanti di azione se non assegnati
        CreateActionButtons();
        
        // BLK-01.03B: Crea i nuovi elementi UI per stage e progresso
        CreateStageAndProgressUI();
        
        // Crea elementi UI per Idratazione, Light Exposure e pH Drift
        CreatePlantStatsUI();

        // Crea elementi UI per Condizione pianta
        CreateConditionUI();
    }
    
    /// <summary>
    /// BLK-01.03B: Crea gli elementi UI per stage e progresso
    /// </summary>
    private void CreateStageAndProgressUI()
    {
        // Crea PotId Text
        if (potIdText == null)
        {
            GameObject potIdGO = new GameObject("PotIdText");
            potIdGO.transform.SetParent(_widgetContainer.transform, false);
            
            potIdText = potIdGO.AddComponent<TextMeshProUGUI>();
            potIdText.color = textColor;
            potIdText.fontSize = 14;
            potIdText.alignment = TextAlignmentOptions.Left;
            potIdText.text = "POT-ID";
            
            RectTransform potIdRect = potIdGO.GetComponent<RectTransform>();
            potIdRect.anchorMin = new Vector2(0, 1);
            potIdRect.anchorMax = new Vector2(0, 1);
            potIdRect.pivot = new Vector2(0, 1);
            potIdRect.anchoredPosition = new Vector2(10, -10);
            potIdRect.sizeDelta = new Vector2(100, 20);
        }
        
        // Crea Stage Icon
        if (stageIcon == null)
        {
            GameObject stageIconGO = new GameObject("StageIcon");
            stageIconGO.transform.SetParent(_widgetContainer.transform, false);
            
            stageIcon = stageIconGO.AddComponent<Image>();
            stageIcon.color = Color.white;
            stageIcon.sprite = null; // Sarà impostato dinamicamente
            
            RectTransform stageIconRect = stageIconGO.GetComponent<RectTransform>();
            stageIconRect.anchorMin = new Vector2(1, 1);
            stageIconRect.anchorMax = new Vector2(1, 1);
            stageIconRect.pivot = new Vector2(1, 1);
            stageIconRect.anchoredPosition = new Vector2(-10, -10);
            stageIconRect.sizeDelta = new Vector2(32, 32);
        }
        
        // Crea Stage Label
        if (stageLabel == null)
        {
            GameObject stageLabelGO = new GameObject("StageLabel");
            stageLabelGO.transform.SetParent(_widgetContainer.transform, false);
            
            stageLabel = stageLabelGO.AddComponent<TextMeshProUGUI>();
            stageLabel.color = textColor;
            stageLabel.fontSize = 16;
            stageLabel.alignment = TextAlignmentOptions.Center;
            stageLabel.text = "Empty";
            stageLabel.fontStyle = FontStyles.Bold;
            
            RectTransform stageLabelRect = stageLabelGO.GetComponent<RectTransform>();
            stageLabelRect.anchorMin = new Vector2(0.5f, 1);
            stageLabelRect.anchorMax = new Vector2(0.5f, 1);
            stageLabelRect.pivot = new Vector2(0.5f, 1);
            stageLabelRect.anchoredPosition = new Vector2(0, -10);
            stageLabelRect.sizeDelta = new Vector2(150, 20);
        }
        
        // Crea Progress Bar
        if (progressBar == null)
        {
            GameObject progressBarGO = new GameObject("ProgressBar");
            progressBarGO.transform.SetParent(_widgetContainer.transform, false);
            
            progressBar = progressBarGO.AddComponent<Slider>();
            progressBar.minValue = 0f;
            progressBar.maxValue = 100f;
            progressBar.value = 0f;
            progressBar.interactable = false;
            
            // Background della progress bar
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(progressBarGO.transform, false);
            Image bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            // Fill della progress bar
            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(progressBarGO.transform, false);
            Image fillImage = fillGO.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            
            RectTransform fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            progressBar.fillRect = fillRect;
            
            RectTransform progressBarRect = progressBarGO.GetComponent<RectTransform>();
            progressBarRect.anchorMin = new Vector2(0, 0.5f);
            progressBarRect.anchorMax = new Vector2(1, 0.5f);
            progressBarRect.pivot = new Vector2(0.5f, 0.5f);
            progressBarRect.anchoredPosition = new Vector2(0, -25);
            progressBarRect.sizeDelta = new Vector2(-20, 15);

            // Eventi hover per tooltip crescita
            EventTrigger trigger = progressBarGO.AddComponent<EventTrigger>();
            AddHoverEvent(trigger, EventTriggerType.PointerEnter, _ => ShowGrowthTooltip(true));
            AddHoverEvent(trigger, EventTriggerType.PointerExit, _ => ShowGrowthTooltip(false));
        }
        else
        {
            // Se il progressBar è già assegnato da scena/prefab, garantisci eventi hover
            var trigger = progressBar.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = progressBar.gameObject.AddComponent<EventTrigger>();
            }
            AddHoverEvent(trigger, EventTriggerType.PointerEnter, _ => ShowGrowthTooltip(true));
            AddHoverEvent(trigger, EventTriggerType.PointerExit, _ => ShowGrowthTooltip(false));
        }
        
        // Crea Progress Text
        if (progressText == null)
        {
            GameObject progressTextGO = new GameObject("ProgressText");
            progressTextGO.transform.SetParent(_widgetContainer.transform, false);
            
            progressText = progressTextGO.AddComponent<TextMeshProUGUI>();
            progressText.color = textColor;
            progressText.fontSize = 12;
            progressText.alignment = TextAlignmentOptions.Center;
            progressText.text = "0%";
            
            RectTransform progressTextRect = progressTextGO.GetComponent<RectTransform>();
            progressTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            progressTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            progressTextRect.pivot = new Vector2(0.5f, 0.5f);
            progressTextRect.anchoredPosition = new Vector2(0, -42); // sotto la barra per visibilità
            progressTextRect.sizeDelta = new Vector2(100, 20);
        }

        // Crea Growth Label
        if (growthLabelText == null)
        {
            GameObject growthLabelGO = new GameObject("GrowthLabel");
            growthLabelGO.transform.SetParent(_widgetContainer.transform, false);
            
            growthLabelText = growthLabelGO.AddComponent<TextMeshProUGUI>();
            growthLabelText.color = textColor;
            growthLabelText.fontSize = 14;
            growthLabelText.alignment = TextAlignmentOptions.Left;
            growthLabelText.text = "Growth:";
            
            RectTransform growthLabelRect = growthLabelGO.GetComponent<RectTransform>();
            growthLabelRect.anchorMin = new Vector2(0, 0.5f);
            growthLabelRect.anchorMax = new Vector2(0, 0.5f);
            growthLabelRect.pivot = new Vector2(0, 0.5f);
            growthLabelRect.anchoredPosition = new Vector2(10, -5);
            growthLabelRect.sizeDelta = new Vector2(120, 20);
        }

        // Tooltip Growth (centrato in alto dello schermo, inizialmente nascosto)
        if (growthTooltipPanel == null)
        {
            // Crea il tooltip come child del Canvas principale per centrarlo in alto dello schermo
            GameObject tooltipPanelGO = new GameObject("GrowthTooltipPanel");
            tooltipPanelGO.transform.SetParent(_parentCanvas.transform, false);
            growthTooltipPanel = tooltipPanelGO;
            
            Image panelImage = tooltipPanelGO.AddComponent<Image>();
            // Colore sfondo tooltip: #3d568e
            panelImage.color = new Color(61f/255f, 86f/255f, 142f/255f, 1f);
            
            RectTransform panelRect = tooltipPanelGO.GetComponent<RectTransform>();
            // Centrato orizzontalmente, ancorato in alto
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0, -150); // Offset dall'alto, più in basso per evitare sovrapposizione con pH
            panelRect.sizeDelta = new Vector2(600, 300); // Tooltip più alto per contenuto dettagliato
            
            GameObject tooltipTextGO = new GameObject("GrowthTooltipText");
            tooltipTextGO.transform.SetParent(tooltipPanelGO.transform, false);
            
            growthTooltipText = tooltipTextGO.AddComponent<TextMeshProUGUI>();
            growthTooltipText.color = new Color(0.9f, 0.9f, 0.9f);
            growthTooltipText.fontSize = 16; // Testo più grande per contenuto dettagliato
            growthTooltipText.alignment = TextAlignmentOptions.Left;
            growthTooltipText.richText = true; // Abilita rich text per colori
            growthTooltipText.text = "Growth info";
            
            RectTransform textRect = tooltipTextGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(8, 6);
            textRect.offsetMax = new Vector2(-8, -6);
            
            growthTooltipPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Crea gli elementi UI per Idratazione, Light Exposure e pH Drift
    /// </summary>
    private void CreatePlantStatsUI()
    {
        // Crea Hydration Text
        if (hydrationText == null)
        {
            GameObject hydrationGO = new GameObject("HydrationText");
            hydrationGO.transform.SetParent(_widgetContainer.transform, false);
            
            hydrationText = hydrationGO.AddComponent<TextMeshProUGUI>();
            hydrationText.color = new Color(0.3f, 0.6f, 1f); // Blu acqua
            hydrationText.fontSize = 14;
            hydrationText.alignment = TextAlignmentOptions.Left;
            hydrationText.text = "💧 Idratazione: 0/3";
            
            RectTransform hydrationRect = hydrationGO.GetComponent<RectTransform>();
            hydrationRect.anchorMin = new Vector2(0, 0.5f);
            hydrationRect.anchorMax = new Vector2(0.5f, 0.5f);
            hydrationRect.pivot = new Vector2(0, 0.5f);
            hydrationRect.anchoredPosition = new Vector2(10, -45);
            hydrationRect.sizeDelta = new Vector2(140, 20);
        }
        
        // Crea Light Exposure Text
        if (lightExposureText == null)
        {
            GameObject lightGO = new GameObject("LightExposureText");
            lightGO.transform.SetParent(_widgetContainer.transform, false);
            
            lightExposureText = lightGO.AddComponent<TextMeshProUGUI>();
            lightExposureText.color = new Color(1f, 0.9f, 0.3f); // Giallo
            lightExposureText.fontSize = 14;
            lightExposureText.alignment = TextAlignmentOptions.Left;
            lightExposureText.text = "💡 Luce: 0/3";
            
            RectTransform lightRect = lightGO.GetComponent<RectTransform>();
            lightRect.anchorMin = new Vector2(0.5f, 0.5f);
            lightRect.anchorMax = new Vector2(1f, 0.5f);
            lightRect.pivot = new Vector2(0, 0.5f);
            lightRect.anchoredPosition = new Vector2(10, -45);
            lightRect.sizeDelta = new Vector2(140, 20);
        }
        
        // Crea pH Drift Text
        if (phDriftText == null)
        {
            GameObject phDriftGO = new GameObject("PhDriftText");
            phDriftGO.transform.SetParent(_widgetContainer.transform, false);
            
            phDriftText = phDriftGO.AddComponent<TextMeshProUGUI>();
            phDriftText.color = new Color(0.8f, 0.3f, 0.8f); // Viola
            phDriftText.fontSize = 14;
            phDriftText.alignment = TextAlignmentOptions.Left;
            phDriftText.text = "⚗️ pH Drift: 0/giorno";
            
            RectTransform phDriftRect = phDriftGO.GetComponent<RectTransform>();
            phDriftRect.anchorMin = new Vector2(0, 0.3f);
            phDriftRect.anchorMax = new Vector2(1f, 0.3f);
            phDriftRect.pivot = new Vector2(0, 0.5f);
            phDriftRect.anchoredPosition = new Vector2(10, 0);
            phDriftRect.sizeDelta = new Vector2(-20, 20);
        }
        
        // BLK-03.01-T1: Crea Fertilizzante Text
        if (fertilizerText == null)
        {
            GameObject fertilizerGO = new GameObject("FertilizerText");
            fertilizerGO.transform.SetParent(_widgetContainer.transform, false);
            
            fertilizerText = fertilizerGO.AddComponent<TextMeshProUGUI>();
            fertilizerText.fontSize = 12;
            fertilizerText.color = new Color(0.6f, 0.6f, 0.6f);
            fertilizerText.text = "🌿 Fertilizzante: -";
            
            RectTransform fertilizerRect = fertilizerGO.GetComponent<RectTransform>();
            fertilizerRect.anchorMin = new Vector2(0, 0);
            fertilizerRect.anchorMax = new Vector2(1f, 0.5f);
            fertilizerRect.pivot = new Vector2(0, 0);
            fertilizerRect.anchoredPosition = new Vector2(10, -20);
            fertilizerRect.sizeDelta = new Vector2(-20, 20);
        }
    }
    
    /// <summary>
    /// Aggiunge eventi hover a un EventTrigger
    /// </summary>
    private void AddHoverEvent(EventTrigger trigger, EventTriggerType type, System.Action<BaseEventData> callback)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener((data) => { callback?.Invoke(data); });
        trigger.triggers.Add(entry);
    }

    /// <summary>
    /// Crea gli elementi UI per la Condizione della pianta (score + forecast)
    /// </summary>
    private void CreateConditionUI()
    {
        // Label condizione
        if (conditionLabelText == null)
        {
            GameObject conditionLabelGO = new GameObject("ConditionLabelText");
            conditionLabelGO.transform.SetParent(_widgetContainer.transform, false);
            
            conditionLabelText = conditionLabelGO.AddComponent<TextMeshProUGUI>();
            conditionLabelText.color = Color.white;
            conditionLabelText.fontSize = 14;
            conditionLabelText.alignment = TextAlignmentOptions.Left;
            conditionLabelText.text = "Condizione: Sana (50/100) →";
            
            RectTransform conditionLabelRect = conditionLabelGO.GetComponent<RectTransform>();
            conditionLabelRect.anchorMin = new Vector2(0, 0.15f);
            conditionLabelRect.anchorMax = new Vector2(1f, 0.15f);
            conditionLabelRect.pivot = new Vector2(0, 0.5f);
            conditionLabelRect.anchoredPosition = new Vector2(10, -10);
            conditionLabelRect.sizeDelta = new Vector2(-20, 20);
        }
        
        // Barra condizione
        if (conditionBar == null)
        {
            GameObject conditionBarGO = new GameObject("ConditionBar");
            conditionBarGO.transform.SetParent(_widgetContainer.transform, false);
            
            conditionBar = conditionBarGO.AddComponent<Slider>();
            conditionBar.minValue = 0f;
            conditionBar.maxValue = 100f;
            conditionBar.value = 50f;
            conditionBar.interactable = false;
            
            // Background
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(conditionBarGO.transform, false);
            Image bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            
            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            // Fill
            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(conditionBarGO.transform, false);
            Image fillImage = fillGO.AddComponent<Image>();
            fillImage.color = new Color(0f, 0.8f, 0f, 1f);
            
            RectTransform fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            conditionBar.fillRect = fillRect;
            
            RectTransform barRect = conditionBarGO.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 0.1f);
            barRect.anchorMax = new Vector2(1f, 0.1f);
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.anchoredPosition = new Vector2(0, -5);
            barRect.sizeDelta = new Vector2(-20, 15);
        }
        
        // Tooltip pannello
        if (conditionTooltipPanel == null)
        {
            GameObject tooltipPanelGO = new GameObject("ConditionTooltipPanel");
            tooltipPanelGO.transform.SetParent(_widgetContainer.transform, false);
            conditionTooltipPanel = tooltipPanelGO;
            
            Image panelImage = tooltipPanelGO.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.6f);
            
            RectTransform panelRect = tooltipPanelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0, -30);
            panelRect.sizeDelta = new Vector2(-20, 45);
            
            GameObject tooltipTextGO = new GameObject("ConditionTooltipText");
            tooltipTextGO.transform.SetParent(tooltipPanelGO.transform, false);
            
            conditionTooltipText = tooltipTextGO.AddComponent<TextMeshProUGUI>();
            conditionTooltipText.color = new Color(0.9f, 0.9f, 0.9f);
            conditionTooltipText.fontSize = 12;
            conditionTooltipText.alignment = TextAlignmentOptions.Left;
            conditionTooltipText.text = "Condizione: Sana (50/100) →";
            
            RectTransform textRect = tooltipTextGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(8, 4);
            textRect.offsetMax = new Vector2(-8, -4);
        }
    }
    
    private void OnPotSelected(PotSlot pot)
    {
        if (!_isInitialized) return;
        
        SporiumLogger.LogDebug(LogCategory.UI, $"Vaso {pot.PotId} selezionato. Aggiornamento UI...");
        SporiumLogger.LogDebug(LogCategory.UI, $"PotActions presente: {pot.PotActions != null}");
        // Debug.Log($"[BLK-01.03B] Player in range: {pot.InRange}");
        
        // Salva il vaso selezionato corrente
        _currentSelectedPot = pot;
        
        // BLK-01.03B: Aggiorna tutti gli elementi UI del nuovo sistema
        UpdateStageAndProgressUI(pot);
        
        // Aggiorna i pulsanti di azione
        UpdateActionButtons(pot);
        
        // Mostra il widget
        SetWidgetVisible(true);
        
        SporiumLogger.LogDebug(LogCategory.UI, $"UI aggiornata per vaso {pot.PotId}");
    }
    
    private void UpdatePotInfo(string info)
    {
        if (potInfoText != null)
        {
            potInfoText.text = info;
        }
    }
    
    private string GetPotStatusText(PotSlot pot)
    {
        if (pot.PotActions == null) return "Errore: PotActions mancante";
        
        PotStateModel state = pot.PotActions.GetCurrentState();
        if (state == null) return "Errore: Stato vaso mancante";
        
        if (state.IsEmpty)
        {
            return "Vuoto - Pronto per piantare";
        }
        else
        {
            string stageName = GetStageName(state.Stage);
            string threshold = GetStageThreshold(state.Stage);
            return $"Pianta ({stageName}) - H:{state.Hydration}/3 L:{state.LightExposure}/3 - Progresso: {state.GrowthPoints}/{threshold}";
        }
    }
    
    /// <summary>
    /// Forza l'aggiornamento del widget con un messaggio personalizzato
    /// </summary>
    public void SetCustomMessage(string message)
    {
        UpdatePotInfo(message);
    }
    
    /// <summary>
    /// Nasconde il widget
    /// </summary>
    public void HideWidget()
    {
        if (_widgetContainer != null)
        {
            _widgetContainer.SetActive(false);
        }
        
        // Chiudi il tooltip quando si chiude la HUD
        if (growthTooltipPanel != null)
        {
            growthTooltipPanel.SetActive(false);
        }
        
        // BLK-01.03B: Reset selezione corrente
        _currentSelectedPot = null;
    }
    
    /// <summary>
    /// Mostra il widget
    /// </summary>
    public void ShowWidget()
    {
        if (_widgetContainer != null)
        {
            _widgetContainer.SetActive(true);
        }
    }
    
    /// <summary>
    /// Mostra/nasconde il widget
    /// </summary>
    public void SetWidgetVisible(bool visible)
    {
        if (_widgetContainer != null)
        {
            _widgetContainer.SetActive(visible);
        }
        
        // Chiudi il tooltip quando si nasconde la HUD
        if (!visible && growthTooltipPanel != null)
        {
            growthTooltipPanel.SetActive(false);
        }
        
        // BLK-01.03B: Nascondi anche il widget se non c'è selezione
        if (!visible && _currentSelectedPot == null)
        {
            // Reset UI elements
            if (potIdText != null) potIdText.text = "POT-ID";
            if (stageLabel != null) stageLabel.text = "Empty";
            if (stageIcon != null) stageIcon.color = Color.gray;
            if (progressBar != null) progressBar.value = 0f;
            if (progressText != null) progressText.text = "0%";
            if (hydrationText != null) hydrationText.text = "💧 Idratazione: 0/3";
            if (lightExposureText != null) lightExposureText.text = "💡 Luce: 0/3";
            if (phDriftText != null) phDriftText.text = "⚗️ pH Drift: -/giorno";
            if (fertilizerText != null) fertilizerText.text = "🌿 Fertilizzante: -";  // BLK-03.01-T1
        }
    }
    
    /// <summary>
    /// Cambia la posizione del widget
    /// </summary>
    public void SetWidgetPosition(Vector2 newPosition)
    {
        widgetPosition = newPosition;
        if (_widgetContainer != null)
        {
            RectTransform rectTransform = _widgetContainer.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = widgetPosition;
            }
        }
    }

    public void DeselectPot()
    {
        _currentSelectedPot = null;
        
        potIdText.text = "POT-ID";

        SetCustomMessage("Nessun POT selezionato");

        SetActionButtonsVisible(false);
        
        // Chiudi il tooltip quando si deseleziona il pot
        if (growthTooltipPanel != null)
        {
            growthTooltipPanel.SetActive(false);
        }
    }
    
    #region Action Buttons (BLK-01.02)
    
    /// <summary>
    /// Crea i pulsanti di azione per il vaso
    /// </summary>
    private void CreateActionButtons()
    {
        if (btnPlant == null)
        {
            btnPlant = CreateActionButton("Plant", "Piantare", PotEvents.PotActionType.Plant);
        }
        
        if (btnWater == null)
        {
            btnWater = CreateActionButton("Water", "Annaffiare", PotEvents.PotActionType.Water);
        }
        
        if (btnLight == null)
        {
            btnLight = CreateActionButton("Light", "Illuminare", PotEvents.PotActionType.Light);
        }
        
        if (btnSpray == null)
        {
            btnSpray = CreateActionButton("Spray", "Spray", PotEvents.PotActionType.Spray);
        }
        
        if (btnHarvest == null)
        {
            btnHarvest = CreateActionButton("Harvest", "Raccogli", PotEvents.PotActionType.Harvest);
        }
        
        // BLK-03.01-T1: Bottone fertilizzante
        if (btnFertilize == null)
        {
            btnFertilize = CreateActionButton("Fertilize", "Fertilizzare", PotEvents.PotActionType.Fertilize);
        }
        
        // AZ-13: Bottone potatura
        if (btnPruning == null)
        {
            btnPruning = CreateActionButton("Pruning", "Potatura", PotEvents.PotActionType.Pruning);
        }
        
        // Crea il testo dei costi
        if (txtCosts == null)
        {
            GameObject costsGO = new GameObject("CostsText");
            costsGO.transform.SetParent(_widgetContainer.transform, false);
            
            txtCosts = costsGO.AddComponent<TextMeshProUGUI>();
            txtCosts.color = textColor;
            txtCosts.fontSize = 12;
            txtCosts.alignment = TextAlignmentOptions.Center;
            txtCosts.text = "Costo: -1 Azione";
            
            RectTransform costsRect = costsGO.GetComponent<RectTransform>();
            costsRect.anchorMin = new Vector2(0, 0);
            costsRect.anchorMax = new Vector2(1, 0);
            costsRect.pivot = new Vector2(0.5f, 0);
            costsRect.anchoredPosition = new Vector2(0, 5);
            costsRect.sizeDelta = new Vector2(0, 20);
        }
        
        // Nascondi tutti i pulsanti inizialmente
        SetActionButtonsVisible(false);
    }
    
    /// <summary>
    /// Crea un singolo pulsante di azione
    /// </summary>
    private Button CreateActionButton(string buttonName, string buttonText, PotEvents.PotActionType actionType)
    {
        GameObject buttonGO = new GameObject($"Btn_{buttonName}");
        buttonGO.transform.SetParent(_widgetContainer.transform, false);
        
        // Aggiungi Button
        Button button = buttonGO.AddComponent<Button>();
        
        // Aggiungi Image per il background
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        // Aggiungi testo
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        
        TextMeshProUGUI buttonTextComponent = textGO.AddComponent<TextMeshProUGUI>();
        buttonTextComponent.text = buttonText;
        buttonTextComponent.color = Color.white;
        buttonTextComponent.fontSize = 14;
        buttonTextComponent.alignment = TextAlignmentOptions.Center;
        
        // Posiziona il testo
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Posiziona il pulsante
        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0, 0);
        buttonRect.anchorMax = new Vector2(0, 0);
        buttonRect.pivot = new Vector2(0, 0);
        buttonRect.sizeDelta = new Vector2(80, 30);
        
        // Posiziona in base al tipo di azione (Y aumentato per evitare sovrapposizioni)
        switch (actionType)
        {
            case PotEvents.PotActionType.Plant:
                buttonRect.anchoredPosition = new Vector2(10, 50);
                break;
            case PotEvents.PotActionType.Water:
                buttonRect.anchoredPosition = new Vector2(100, 50);
                break;
            case PotEvents.PotActionType.Light:
                buttonRect.anchoredPosition = new Vector2(190, 50);
                break;
            case PotEvents.PotActionType.Spray:
                buttonRect.anchoredPosition = new Vector2(280, 50);
                break;
            case PotEvents.PotActionType.Harvest:
                buttonRect.anchoredPosition = new Vector2(370, 50);
                break;
            case PotEvents.PotActionType.Fertilize:
                buttonRect.anchoredPosition = new Vector2(460, 50);
                break;
            case PotEvents.PotActionType.Pruning:
                buttonRect.anchoredPosition = new Vector2(550, 50);
                break;
        }
        
        // Aggiungi listener per l'azione
        button.onClick.AddListener(() => OnActionButtonClicked(actionType));
        
        // IMPORTANTE: Configura il pulsante per intercettare correttamente i click UI
        button.transition = Selectable.Transition.ColorTint;
        button.navigation = new Navigation() { mode = Navigation.Mode.None };
        
        // Aggiungi Image con raycast target per intercettare meglio i click
        if (buttonImage != null)
        {
            buttonImage.raycastTarget = true;
        }
        
        // Aggiungi EventTrigger per intercettare tutti gli eventi e prevenire movimento player
        EventTrigger eventTrigger = buttonGO.AddComponent<EventTrigger>();
        
        // PointerDown - blocca movimento player
        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
        pointerDownEntry.eventID = EventTriggerType.PointerDown;
        pointerDownEntry.callback.AddListener((data) => { 
            SporiumLogger.LogDebug(LogCategory.UI, $"Evento PointerDown bloccato per {actionType}");
            // Blocca la propagazione dell'evento
        });
        eventTrigger.triggers.Add(pointerDownEntry);
        
        // BeginDrag - previene drag accidentali
        EventTrigger.Entry beginDragEntry = new EventTrigger.Entry();
        beginDragEntry.eventID = EventTriggerType.BeginDrag;
        beginDragEntry.callback.AddListener((data) => { 
            SporiumLogger.LogDebug(LogCategory.UI, $"Evento BeginDrag bloccato per {actionType}");
        });
        eventTrigger.triggers.Add(beginDragEntry);
        
        return button;
    }
    
    /// <summary>
    /// Gestisce il click su un pulsante di azione
    /// </summary>
    private void OnActionButtonClicked(PotEvents.PotActionType actionType)
    {
        SporiumLogger.LogDebug(LogCategory.UI, $"Click su pulsante {actionType} intercettato!");
        
        // Trova il vaso selezionato
        PotSlot selectedPot = FindSelectedPot();
        if (selectedPot == null || selectedPot.PotActions == null)
        {
            SporiumLogger.LogWarning(LogCategory.UI, "Nessun vaso selezionato o PotActions mancante");
            return;
        }
        
        SporiumLogger.LogInfo(LogCategory.UI, $"Eseguendo azione {actionType} su vaso {selectedPot.PotId}");
        
        // Esegui l'azione appropriata
        bool success = false;
        switch (actionType)
        {
            case PotEvents.PotActionType.Plant:
                // Apri selettore semi invece di piantare direttamente
                OpenSeedSelector(selectedPot);
                return; // Esci subito, DoPlant verrà chiamato quando l'utente seleziona un seme
                
            case PotEvents.PotActionType.Water:
                success = selectedPot.PotActions.DoWater();
                break;
            case PotEvents.PotActionType.Light:
                success = selectedPot.PotActions.DoLight((LedSystemState?)null);  // Toggle esplicito
                break;
            case PotEvents.PotActionType.Spray:
                success = selectedPot.PotActions.DoSprayAntifungal();
                break;
            case PotEvents.PotActionType.Harvest:
                success = selectedPot.PotActions.DoHarvest();
                break;
            case PotEvents.PotActionType.Fertilize:
                // BLK-03.01-T1: Apri selettore fertilizzante invece di applicare direttamente
                OpenFertilizerSelector(selectedPot);
                return; // Esci subito, DoFertilize verrà chiamato quando l'utente seleziona un fertilizzante
            case PotEvents.PotActionType.Pruning:
                // AZ-13: Apri dialog potatura con opzione Spray
                OpenPruningDialog(selectedPot);
                return; // Esci subito, DoPruning verrà chiamato quando l'utente conferma
        }
        
        if (success)
        {
            SporiumLogger.LogInfo(LogCategory.UI, $"Azione {actionType} eseguita con successo!");
            // Aggiorna l'UI - inclusi Stage, Idratazione e Light Exposure
            UpdateActionButtons(selectedPot);
            UpdateStageAndProgressUI(selectedPot);
        }
        else
        {
            SporiumLogger.LogWarning(LogCategory.UI, $"Azione {actionType} fallita!");
        }
    }
    
    /// <summary>
    /// Trova il vaso attualmente selezionato
    /// </summary>
    private PotSlot FindSelectedPot()
    {
        // Trova il vaso che ha emesso l'evento OnPotSelected
        // Usa il sistema di eventi per tracciare la selezione
        PotSlot[] allPots = FindObjectsOfType<PotSlot>();
        foreach (PotSlot pot in allPots)
        {
            if (pot.PotActions != null && pot.IsSelected)
            {
                SporiumLogger.LogDebug(LogCategory.UI, $"Trovato vaso selezionato: {pot.PotId}");
                return pot;
            }
        }
        
        // Fallback: cerca il primo vaso con PotActions
        foreach (PotSlot pot in allPots)
        {
            if (pot.PotActions != null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"Fallback: usando primo vaso disponibile {pot.PotId}");
                return pot;
            }
        }
        
        SporiumLogger.LogError(LogCategory.UI, "Nessun vaso trovato!");
        return null;
    }
    
    /// <summary>
    /// Aggiorna i pulsanti di azione in base al vaso selezionato
    /// </summary>
    private void UpdateActionButtons(PotSlot pot)
    {
        if (pot == null || pot.PotActions == null)
        {
            SetActionButtonsVisible(false);
            return;
        }
        
        // Mostra sempre i pulsanti quando un POT è selezionato
        // Il controllo del range e delle condizioni è gestito da CanXxx() per ogni azione
        // Questo permette all'utente di vedere tutte le azioni disponibili e capire perché alcune sono disabilitate
        SetActionButtonsVisible(true);
        
        // Aggiorna lo stato di ogni pulsante (abilitato/disabilitato in base alle condizioni)
        UpdateButtonState(btnPlant, pot.PotActions.CanPlant(), "Piantare");
        
        // GDD AZ-11: Mostra stato toggle ON/OFF per sistema irrigazione
        bool isWateringOn = pot.PotActions != null && pot.PotActions.IsWateringSystemOn();
        string waterButtonText = isWateringOn ? "Irrigazione ON" : "Irrigazione OFF";
        UpdateButtonState(btnWater, pot.PotActions.CanWater(), waterButtonText);
        
        UpdateButtonState(btnLight, pot.PotActions.CanLight(), "Illuminare");
        UpdateButtonState(btnSpray, pot.PotActions.CanSprayAntifungal(), "Spray");
        UpdateButtonState(btnHarvest, pot.PotActions.CanHarvest(), "Raccogli");
        UpdateButtonState(btnFertilize, pot.PotActions.CanFertilize(), "Fertilizzare");  // BLK-03.01-T1
        UpdateButtonState(btnPruning, pot.PotActions.CanPruning(), "Potatura");  // AZ-13
    }
    
    /// <summary>
    /// Aggiorna lo stato di un singolo pulsante
    /// </summary>
    private void UpdateButtonState(Button button, bool canExecute, string actionName)
    {
        if (button == null) return;
        
        button.interactable = canExecute;
        
        // Aggiorna il colore e il tooltip
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = canExecute ? 
                new Color(0.2f, 0.8f, 0.2f, 0.9f) : // Verde se abilitato
                new Color(0.5f, 0.5f, 0.5f, 0.9f);   // Grigio se disabilitato
        }
        
        // Aggiorna il testo
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = canExecute ? actionName : $"{actionName} (N/A)";
        }
    }
    
    /// <summary>
    /// Mostra/nasconde i pulsanti di azione
    /// </summary>
    private void SetActionButtonsVisible(bool visible)
    {
        if (btnPlant != null) btnPlant.gameObject.SetActive(visible);
        if (btnWater != null) btnWater.gameObject.SetActive(visible);
        if (btnLight != null) btnLight.gameObject.SetActive(visible);
        if (btnSpray != null) btnSpray.gameObject.SetActive(visible);
        if (btnHarvest != null) btnHarvest.gameObject.SetActive(visible);
        if (btnFertilize != null) btnFertilize.gameObject.SetActive(visible);
        if (btnPruning != null) btnPruning.gameObject.SetActive(visible);
        if (txtCosts != null) txtCosts.gameObject.SetActive(visible);

        if (visible) SetCustomMessage("");
    }
    
    /// <summary>
    /// Gestisce il cambio di stato di un vaso
    /// </summary>
    private void OnPotStateChanged(PotSlot pot)
    {
        if (!_isInitialized) return;
        
        // #region agent log
        try {
            var logData = new { 
                potId = pot != null ? pot.PotId : "null",
                currentSelectedPotId = _currentSelectedPot != null ? _currentSelectedPot.PotId : "null",
                isMatch = _currentSelectedPot != null && _currentSelectedPot.PotId == pot?.PotId,
                lightExposure = pot?.PotActions?.GetCurrentState()?.LightExposure ?? -1
            };
            var logJson = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"BUG-LIGHT-HUD\",\"location\":\"PotHUDWidget.cs:OnPotStateChanged\",\"message\":\"OnPotStateChanged: Event received\",\"data\":{JsonUtility.ToJson(logData)},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"d:\Sporae_Build_Beta\.cursor\debug.log", logJson);
        } catch { }
        // #endregion
        
        // Aggiorna i pulsanti se questo è il vaso selezionato
        UpdateActionButtons(pot);
        
        // Aggiorna anche Stage, Idratazione e Light Exposure se è il vaso selezionato
        if (_currentSelectedPot != null && _currentSelectedPot.PotId == pot.PotId)
        {
            UpdateStageAndProgressUI(pot);
        }
    }
    
    /// <summary>
    /// Gestisce il fallimento di un'azione
    /// </summary>
    private void OnPotActionFailed(PotEvents.PotActionType actionType, PotSlot pot, string reason)
    {
        if (!_isInitialized) return;
        
        // Mostra il motivo del fallimento
        string failureMessage = $"Azione {PotEvents.GetActionName(actionType)} fallita: {reason}";
        UpdatePotInfo(failureMessage);
        
        SporiumLogger.LogWarning(LogCategory.UI, failureMessage);
    }
    
    /// <summary>
    /// BLK-01.03B: Gestisce l'evento OnPlantGrew
    /// </summary>
    private void OnPlantGrew(string potId, PlantStage stage, int oldPoints, int newPoints)
    {
        if (!_isInitialized || _currentSelectedPot == null || _currentSelectedPot.PotId != potId) return;
        
        SporiumLogger.LogDebug(LogCategory.UI, $"Pianta cresciuta su {potId}: {oldPoints} → {newPoints} punti. Aggiornamento progress bar...");
        UpdateStageAndProgressUI(_currentSelectedPot);
    }
    
    /// <summary>
    /// BLK-01.03B: Gestisce l'evento OnPlantStageChanged
    /// </summary>
    private void OnPlantStageChanged(string potId, PlantStage stage)
    {
        if (!_isInitialized || _currentSelectedPot == null || _currentSelectedPot.PotId != potId) return;
        
        SporiumLogger.LogDebug(LogCategory.UI, $"Stadio cambiato su {potId}: {stage}. Aggiornamento UI...");
        UpdateStageAndProgressUI(_currentSelectedPot);
    }
    
    /// <summary>
    /// BLK-01.04: Aggiorna tutti gli elementi UI per stage e progresso
    /// BUG FIX: Reso pubblico per permettere aggiornamento forzato da console debug
    /// </summary>
    public void UpdateStageAndProgressUI(PotSlot pot)
    {
        if (pot == null || pot.PotActions == null) return;
        
        PotStateModel state = pot.PotActions.GetCurrentState();
        if (state == null) return;
        
        // #region agent log
        try {
            var logData = new { 
                potId = pot.PotId,
                lightExposure = state.LightExposure,
                hydration = state.Hydration
            };
            var logJson = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"BUG-LIGHT-HUD\",\"location\":\"PotHUDWidget.cs:UpdateStageAndProgressUI\",\"message\":\"UpdateStageAndProgressUI: State read\",\"data\":{JsonUtility.ToJson(logData)},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"d:\Sporae_Build_Beta\.cursor\debug.log", logJson);
        } catch { }
        // #endregion
        
        // Aggiorna PotId
        if (potIdText != null)
        {
            potIdText.text = pot.PotId;
        }
        
        // BLK-01.04: Aggiorna Stage Label con informazioni dettagliate
        if (stageLabel != null)
        {
            string stageName = GetStageName(state.Stage);
            string stageInfo = GetStageInfo(state);
            stageLabel.text = $"{stageName} - {stageInfo}";
        }
        
        // BLK-01.04: Aggiorna Stage Icon con colore appropriato
        if (stageIcon != null)
        {
            stageIcon.color = GetStageColor(state.Stage);
            // TODO: Sostituire con sprite reali quando disponibili
        }

        // Aggiorna Idratazione, Light Exposure e pH Drift
        UpdatePlantStatsUI(state);

        UpdateProgressUI(state);

        UpdateConditionUI(state);
        
        UpdateGrowthTooltip(state);
    }
    
    /// <summary>
    /// Aggiorna gli elementi UI per Idratazione, Light Exposure e pH Drift
    /// </summary>
    private void UpdatePlantStatsUI(PotStateModel state)
    {
        if (state == null) return;
        
        // Aggiorna Label Growth con stato (IN CRESCITA, Stabile, Difficoltà, Malata)
        if (growthLabelText != null)
        {
            if (state.IsEmpty || !state.HasPlant)
            {
                growthLabelText.text = "Growth: Stabile";
                growthLabelText.color = new Color(0.6f, 0.6f, 0.6f); // Grigio
            }
            else
            {
                string status = GetGrowthStatus(state);
                growthLabelText.text = $"Growth: {status}";
                
                // Colore in base allo stato
                switch (status)
                {
                    case "IN CRESCITA":
                        growthLabelText.color = new Color(0.2f, 1f, 0.2f); // Verde
                        break;
                    case "Stabile":
                        growthLabelText.color = new Color(1f, 1f, 0.2f); // Giallo
                        break;
                    case "Difficoltà":
                        growthLabelText.color = new Color(1f, 0.5f, 0.2f); // Arancione
                        break;
                    case "Malata":
                        growthLabelText.color = new Color(1f, 0.2f, 0.2f); // Rosso
                        break;
                    default:
                        growthLabelText.color = Color.white;
                        break;
                }
            }
        }
        
        // Aggiorna Hydration Text
        if (hydrationText != null)
        {
            int maxHydration = _currentSelectedPot?.PotActions?.GetMaxHydration() ?? 5; // 5 step = 20% ciascuno
            hydrationText.text = $"💧 Idratazione: {state.Hydration}/{maxHydration}";
            
            // Cambia colore in base al livello di idratazione
            if (state.Hydration >= maxHydration)
                hydrationText.color = new Color(0.2f, 1f, 0.2f); // Verde quando al massimo
            else if (state.Hydration == 0)
                hydrationText.color = new Color(1f, 0.3f, 0.3f); // Rosso quando vuoto
            else
                hydrationText.color = new Color(0.3f, 0.6f, 1f); // Blu normale
        }
        
        // Aggiorna Light Exposure Text
        if (lightExposureText != null)
        {
            int maxLight = _currentSelectedPot?.PotActions?.GetMaxLightExposure() ?? 3;
            lightExposureText.text = $"💡 Luce: {state.LightExposure}/{maxLight}";
            
            // Cambia colore in base al livello di luce
            if (state.LightExposure >= maxLight)
                lightExposureText.color = new Color(1f, 1f, 0.2f); // Giallo brillante quando al massimo
            else if (state.LightExposure == 0)
                lightExposureText.color = new Color(0.5f, 0.5f, 0.5f); // Grigio quando vuoto
            else
                lightExposureText.color = new Color(1f, 0.9f, 0.3f); // Giallo normale
        }
        
        // Aggiorna pH Drift Text
        if (phDriftText != null)
        {
            // Ottieni PlantData dal vaso selezionato
            PlantData plantData = null;
            if (_currentSelectedPot != null && _currentSelectedPot.PotActions != null)
            {
                PotStateModel potState = _currentSelectedPot.PotActions.PotState;
                if (potState != null)
                {
                    plantData = potState.GetPlantData();
                }
            }
            
            if (plantData != null && !state.IsEmpty)
            {
                float phDrift = plantData.GetDailyPhDrift();
                phDriftText.text = $"⚗️ pH Drift: {phDrift:+#;-#;0}/giorno";
                
                // Cambia colore in base al valore del drift
                if (phDrift > 0)
                    phDriftText.color = new Color(0.3f, 0.8f, 0.3f); // Verde per drift positivo (Pure)
                else if (phDrift < 0)
                    phDriftText.color = new Color(0.8f, 0.3f, 0.3f); // Rosso per drift negativo (Evil)
                else
                    phDriftText.color = new Color(0.6f, 0.6f, 0.6f); // Grigio per drift zero (Standard)
            }
            else
            {
                // Nessuna pianta o PlantData non disponibile
                phDriftText.text = "⚗️ pH Drift: -/giorno";
                phDriftText.color = new Color(0.5f, 0.5f, 0.5f); // Grigio
            }
        }
        
        // BLK-03.01-T1: Aggiorna Fertilizzante Text
        if (fertilizerText != null)
        {
            if (!state.IsEmpty && state.HasPlant)
            {
                // Ottieni StageRequirements per lo stadio corrente
                PlantData plantData = state.GetPlantData();
                if (plantData != null)
                {
                    PlantStage currentStage = (PlantStage)state.Stage;
                    StageRequirements stageReq = plantData.GetStageRequirements(currentStage);
                    
                    if (stageReq != null)
                    {
                        int currentFertilizer = state.FertilizerLevel;
                        int min = stageReq.fertilizerMin;
                        int med = stageReq.fertilizerMed;
                        int max = stageReq.fertilizerMax;
                        
                        // Formato: "🌿 Fertilizzante: 45% (Range: 40-60-80%)"
                        fertilizerText.text = $"🌿 Fertilizzante: {currentFertilizer}% (Range: {min}-{med}-{max}%)";
                        
                        // Colore in base al range
                        if (stageReq.IsFertilizerInRange(currentFertilizer))
                        {
                            if (stageReq.IsFertilizerOptimal(currentFertilizer))
                                fertilizerText.color = new Color(0.2f, 1f, 0.2f); // Verde se ottimale
                            else
                                fertilizerText.color = new Color(1f, 1f, 0.2f); // Giallo se nel range ma non ottimale
                        }
                        else
                        {
                            fertilizerText.color = new Color(1f, 0.3f, 0.3f); // Rosso se fuori range
                        }
                    }
                    else
                    {
                        fertilizerText.text = $"🌿 Fertilizzante: {state.FertilizerLevel}%";
                        fertilizerText.color = new Color(0.6f, 0.6f, 0.6f); // Grigio
                    }
                }
                else
                {
                    fertilizerText.text = $"🌿 Fertilizzante: {state.FertilizerLevel}%";
                    fertilizerText.color = new Color(0.6f, 0.6f, 0.6f); // Grigio
                }
            }
            else
            {
                fertilizerText.text = "🌿 Fertilizzante: -";
                fertilizerText.color = new Color(0.5f, 0.5f, 0.5f); // Grigio
            }
        }
        
        // BLK-03.01-T2: Aggiorna Growth Points Text
        if (growthPointsText != null)
        {
            if (!state.IsEmpty && state.HasPlant)
            {
                int totalPoints = state.GrowthPointsWater + state.GrowthPointsLight + state.GrowthPointsFertilizer;
                growthPointsText.text = $"📊 Punti: W:{state.GrowthPointsWater} L:{state.GrowthPointsLight} F:{state.GrowthPointsFertilizer} (Tot: {totalPoints}/3)";
                
                // Colore in base ai punti totali
                if (totalPoints >= 3)
                    growthPointsText.color = new Color(0.2f, 1f, 0.2f); // Verde se tutti i punti
                else if (totalPoints >= 2)
                    growthPointsText.color = new Color(1f, 1f, 0.2f); // Giallo se 2 punti
                else if (totalPoints >= 1)
                    growthPointsText.color = new Color(1f, 0.7f, 0.2f); // Arancione se 1 punto
                else
                    growthPointsText.color = new Color(1f, 0.3f, 0.3f); // Rosso se nessun punto
            }
            else
            {
                growthPointsText.text = "📊 Punti: -";
                growthPointsText.color = new Color(0.5f, 0.5f, 0.5f); // Grigio
            }
        }
        
        // BLK-03.01-T2: Aggiorna Optimal Days Text
        if (optimalDaysText != null)
        {
            if (!state.IsEmpty && state.HasPlant)
            {
                optimalDaysText.text = $"⭐ Giorni Ottimali: {state.DaysConsecutiveOptimal}";
                
                // Colore in base ai giorni ottimali
                if (state.DaysConsecutiveOptimal >= 3)
                    optimalDaysText.color = new Color(0.2f, 1f, 0.2f); // Verde se 3+ giorni
                else if (state.DaysConsecutiveOptimal >= 2)
                    optimalDaysText.color = new Color(1f, 1f, 0.2f); // Giallo se 2 giorni
                else if (state.DaysConsecutiveOptimal >= 1)
                    optimalDaysText.color = new Color(1f, 0.7f, 0.2f); // Arancione se 1 giorno
                else
                    optimalDaysText.color = new Color(0.6f, 0.6f, 0.6f); // Grigio se 0 giorni
            }
            else
            {
                optimalDaysText.text = "⭐ Giorni Ottimali: -";
                optimalDaysText.color = new Color(0.5f, 0.5f, 0.5f); // Grigio
            }
        }
        
        // BLK-02.02: Aggiorna Plant Level Text
        if (plantLevelText != null)
        {
            if (!state.IsEmpty && state.HasPlant)
            {
                plantLevelText.text = $"📈 Livello: {state.PlantLevel}/5 (Cicli: {state.CompletedCycles})";
                plantLevelText.color = state.PlantLevel >= 3 ? new Color(0.2f, 1f, 0.2f) : Color.white;
            }
            else
            {
                plantLevelText.text = "📈 Livello: -";
                plantLevelText.color = new Color(0.6f, 0.6f, 0.6f);
            }
        }
        
        // BLK-07.01: Aggiorna Mold Risk Text
        if (moldRiskText != null)
        {
            if (!state.IsEmpty && state.HasPlant && state.MoldRiskLevel > 0)
            {
                string riskLevel = state.MoldRiskLevel switch
                {
                    1 => "Lieve",
                    2 => "Severo",
                    3 => "Critico",
                    _ => "Sconosciuto"
                };
                moldRiskText.text = $"⚠️ Mold Risk: {riskLevel} (Lvl {state.MoldRiskLevel})";
                moldRiskText.color = state.MoldRiskLevel >= 2 ? new Color(1f, 0.2f, 0.2f) : new Color(1f, 0.7f, 0.2f);
            }
            else
            {
                moldRiskText.text = "✅ Mold Risk: Nessuno";
                moldRiskText.color = new Color(0.2f, 1f, 0.2f);
            }
        }
        
        // BLK-07.01: Aggiorna Infestation Badge
        if (infestationBadge != null)
        {
            // #region agent log
            try {
                var logData = new { moldRiskLevel = state.MoldRiskLevel, conditionResult = !state.IsEmpty && state.HasPlant && state.MoldRiskLevel >= 2 };
                var logJson = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"BUG2-A\",\"location\":\"PotHUDWidget.cs:1847\",\"message\":\"UpdatePlantStatsUI: Infestation badge condition\",\"data\":{JsonUtility.ToJson(logData)},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
                System.IO.File.AppendAllText(@"d:\Sporae_Build_Beta\.cursor\debug.log", logJson);
            } catch { }
            // #endregion
            // BUG FIX 2: Badge INFESTATA solo se IsInfested = true (dopo 2 giorni a livello 3)
            infestationBadge.SetActive(!state.IsEmpty && state.HasPlant && state.IsInfested);
        }
    }

    private void UpdateProgressUI(PotStateModel state)
    {
        // BLK-01.04: Calcola e aggiorna Progress con informazioni dettagliate
        float progressPercentage = CalculateProgressPercentage(state);
        if (progressBar != null)
        {
            progressBar.value = progressPercentage;
        }
        
        if (progressText != null)
        {
            string progressInfo = GetProgressInfo(state);
            progressText.text = progressInfo;
        }
        
        SporiumLogger.LogDebug(LogCategory.UI, $"UI aggiornata: {state.PotId} - {GetStageName(state.Stage)} - {progressPercentage:F1}% - {GetProgressInfo(state)}");

    }
    
    /// <summary>
    /// Aggiorna tooltip crescita
    /// </summary>
    private void UpdateGrowthTooltip(PotStateModel state)
    {
        if (growthTooltipText == null || state == null)
            return;
        
        growthTooltipText.text = BuildGrowthTooltip(state);
    }

    /// <summary>
    /// Aggiorna la UI della condizione (score + forecast + tooltip)
    /// BUG FIX: Ricalcola sempre la condizione invece di usare valori salvati per evitare label non aggiornate
    /// </summary>
    private void UpdateConditionUI(PotStateModel state)
    {
        if (state == null)
            return;

        // Ricalcola sempre la condizione per avere dati aggiornati (usa state direttamente come nel tooltip)
        PlantData plantData = state?.GetPlantData();
        ConditionResult result;
        int maxHydration;
        bool isOverwatering;
        string conditionName;
        string forecastSymbol;
        
        if (plantData == null || _phSystem == null || _potSystemConfig == null)
        {
            // Fallback: usa valori salvati se non possiamo calcolare
            int score = state.ConditionScore;
            PlantCondition condition = (PlantCondition)state.ConditionLabel;
            ForecastDirection forecast = (ForecastDirection)state.ForecastDirection;
            maxHydration = _potSystemConfig?.MaxHydration ?? 5;
            isOverwatering = PlantConditionSystem.IsOverwatering(state, maxHydration);
            conditionName = PlantConditionSystem.GetConditionName(condition, isOverwatering);
            forecastSymbol = PlantConditionSystem.GetForecastSymbol(forecast);
            
            if (conditionLabelText != null)
            {
                conditionLabelText.text = $"Condizione: {conditionName} ({score}/100) {forecastSymbol}";
            }
            return;
        }
        
        // Calcola condizione aggiornata
        result = PlantConditionSystem.CalculateCondition(
            state,
            plantData,
            _phSystem,
            _potSystemConfig,
            _dayCycleSystem != null ? _dayCycleSystem.CurrentDay : 0,
            state.PreviousDayConditionScore >= 0 ? state.PreviousDayConditionScore : state.ConditionScore);
        
        maxHydration = _potSystemConfig?.MaxHydration ?? 5;
        isOverwatering = PlantConditionSystem.IsOverwatering(state, maxHydration);
        conditionName = PlantConditionSystem.GetConditionName(result.Condition, isOverwatering);
        forecastSymbol = PlantConditionSystem.GetForecastSymbol(result.Forecast);

        if (conditionLabelText != null)
        {
            conditionLabelText.text = $"Condizione: {conditionName} ({result.Score}/100) {forecastSymbol}";
        }

        if (conditionBar != null)
        {
            conditionBar.value = result.Score;
            if (conditionBar.fillRect != null)
            {
                var fillImage = conditionBar.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = GetConditionColor(result.Condition);
                }
            }
        }

        // Tooltip con contributi (usa result già calcolato)
        if (conditionTooltipText != null)
        {
            conditionTooltipText.text = BuildConditionTooltip(result);
        }
    }

    private static Color GetConditionColor(PlantCondition condition)
    {
        return condition switch
        {
            PlantCondition.Rigogliosa => new Color(0f, 0.5f, 0f),   // Verde scuro
            PlantCondition.Sana => new Color(0f, 0.8f, 0f),         // Verde
            PlantCondition.Stressata => new Color(1f, 0.8f, 0f),    // Giallo
            PlantCondition.Appassita => new Color(1f, 0.5f, 0f),    // Arancione
            PlantCondition.Critica => new Color(0.8f, 0f, 0f),      // Rosso
            _ => Color.gray
        };
    }

    private string BuildConditionTooltip(ConditionResult result)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        string forecastSymbol = PlantConditionSystem.GetForecastSymbol(result.Forecast);
        sb.AppendLine($"Condizione: {PlantConditionSystem.GetConditionName(result.Condition)} ({result.Score}/100) {forecastSymbol}");
        sb.AppendLine();
        sb.AppendLine("Contributi positivi:");
        bool hasPositive = false;
        foreach (var c in result.Contributors)
        {
            if (c.IsPositive && c.Value != 0)
            {
                sb.AppendLine($"• {c.Source}: +{c.Value}");
                hasPositive = true;
            }
        }
        if (!hasPositive)
            sb.AppendLine("• Nessuno");

        sb.AppendLine();
        sb.AppendLine("Contributi negativi:");
        bool hasNegative = false;
        foreach (var c in result.Contributors)
        {
            if (!c.IsPositive && c.Value != 0)
            {
                sb.AppendLine($"• {c.Source}: {c.Value}");
                hasNegative = true;
            }
        }
        if (!hasNegative)
            sb.AppendLine("• Nessuno");

        sb.AppendLine();
        sb.AppendLine($"Forecast: {forecastSymbol} (Δ {result.ScoreDelta:+#;-#;0})");

        return sb.ToString();
    }

    /// <summary>
    /// Determina lo stato di crescita della pianta (IN CRESCITA, Stabile, Difficoltà, Malata)
    /// </summary>
    private string GetGrowthStatus(PotStateModel state)
    {
        if (state == null || state.IsEmpty || !state.HasPlant)
            return "Stabile";
        
        // Verifica se ha muffa (sistema muffa da implementare)
        // TODO: Implementare verifica muffa quando sistema sarà disponibile
        // if (state.HasMold)
        //     return "Malata";
        
        // Ottieni PlantData e StageRequirements
        PlantData plantData = state.GetPlantData();
        if (plantData == null || _potSystemConfig == null)
            return "Stabile";
        
        PlantStage currentStage = (PlantStage)state.Stage;
        StageRequirements stageReq = plantData.GetStageRequirements(currentStage);
        if (stageReq == null)
            return "Stabile";
        
        // Calcola percentuale idratazione
        int maxHydration = _potSystemConfig.MaxHydration;
        int hydrationPercent = maxHydration > 0 ? 
            Mathf.RoundToInt((float)state.Hydration / maxHydration * 100f) : 0;
        
        // Verifica range per ogni parametro
        bool waterOk = stageReq.IsHydrationInRange(hydrationPercent);
        bool lightOk = stageReq.IsLedRequirementMet(state.LedSystemState) && 
                      stageReq.IsLightInRange(state.LightExposure);
        bool fertilizerOk = stageReq.IsFertilizerInRange(state.FertilizerLevel);
        
        // Determina stato in base ai parametri
        int paramsOk = (waterOk ? 1 : 0) + (lightOk ? 1 : 0) + (fertilizerOk ? 1 : 0);
        
        if (paramsOk == 3)
            return "IN CRESCITA";
        else if (paramsOk == 2 || paramsOk == 1)
            return "Stabile";
        else // paramsOk == 0
            return "Difficoltà";
    }
    
    /// <summary>
    /// Costruisce il tooltip di crescita (progress bar Growth) - versione semplificata per player
    /// </summary>
    private string BuildGrowthTooltip(PotStateModel state)
    {
        var sb = new System.Text.StringBuilder();
        
        if (_growthConfig == null || state == null || state.IsEmpty || !state.HasPlant)
        {
            sb.AppendLine("<b>Crescita: Informazioni non disponibili</b>");
            return sb.ToString();
        }
        
        // Ottieni PlantData e StageRequirements
        PlantData plantData = state.GetPlantData();
        if (plantData == null || _potSystemConfig == null)
        {
            sb.AppendLine("<b>Crescita: Dati pianta non disponibili</b>");
            return sb.ToString();
        }
        
        PlantStage currentStage = (PlantStage)state.Stage;
        StageRequirements stageReq = plantData.GetStageRequirements(currentStage);
        if (stageReq == null)
        {
            sb.AppendLine("<b>Crescita: Requisiti stadio non disponibili</b>");
            return sb.ToString();
        }
        
        // Calcola percentuale idratazione
        int maxHydration = _potSystemConfig.MaxHydration;
        int hydrationPercent = maxHydration > 0 ? 
            Mathf.RoundToInt((float)state.Hydration / maxHydration * 100f) : 0;
        
        // BUG 2 FIX: Verifica range per ogni parametro basato SOLO sui valori attuali, non sullo stato delle azioni
        bool waterOk = stageReq.IsHydrationInRange(hydrationPercent);
        // BUG 2 FIX: Light OK solo se LightExposure è nel range (non dipende da LED attivo o meno per il tooltip)
        bool lightOk = stageReq.IsLightInRange(state.LightExposure);
        // Nota: IsLedRequirementMet viene verificato separatamente per il requisito LED, ma non influisce su "OK/NOT OK" nel tooltip
        bool ledRequirementMet = stageReq.IsLedRequirementMet(state.LedSystemState);
        bool fertilizerOk = stageReq.IsFertilizerInRange(state.FertilizerLevel);
        
        // Determina stato
        string growthStatus = GetGrowthStatus(state);
        sb.AppendLine($"<b>Crescita: {growthStatus}</b>");
        sb.AppendLine();
        
        // Spiegazione semplice per il player
        sb.AppendLine("La pianta cresce quando si trova nel <color=#00FF00>range giusto</color> di:");
        sb.AppendLine();
        
        // Water
        string waterStatus = waterOk ? "<color=#00FF00>OK</color>" : "<color=#FF0000>NON OK</color>";
        sb.AppendLine($"• <color=#3F6FFF>Acqua (Water)</color>: {waterStatus}");
        if (!waterOk)
        {
            sb.AppendLine($"  Range ideale: {stageReq.hydrationMin}% - {stageReq.hydrationMax}%");
            sb.AppendLine($"  Attuale: {hydrationPercent}%");
        }
        sb.AppendLine();
        
        // Light
        // BUG 2 FIX: Light OK basato solo su LightExposure nel range, non su LED attivo
        string lightStatus = lightOk ? "<color=#00FF00>OK</color>" : "<color=#FF0000>NON OK</color>";
        sb.AppendLine($"• <color=#FFD700>Luce</color>: {lightStatus}");
        if (!lightOk)
        {
            sb.AppendLine($"  Range ideale: {stageReq.lightMin}% - {stageReq.lightMax}%");
            sb.AppendLine($"  Attuale: {state.LightExposure}%");
        }
        // Mostra requisito LED separatamente (non influisce su OK/NOT OK)
        string ledRequired = stageReq.GetRequiredLed()?.ToString() ?? "Nessuno";
        if (ledRequired != "Nessuno")
        {
            string ledStatus = ledRequirementMet ? "<color=#00FF00>OK</color>" : "<color=#FF0000>NON OK</color>";
            sb.AppendLine($"  LED richiesto: {ledRequired} ({ledStatus})");
        }
        sb.AppendLine();
        
        // Fertilizer
        // BUG FIX: Per Seed e Sprout, il fertilizzante è opzionale
        bool isFertilizerOptional = (currentStage == PlantStage.Seed || currentStage == PlantStage.Sprout);
        string fertilizerStatus = fertilizerOk ? "<color=#00FF00>OK</color>" : "<color=#FF0000>NON OK</color>";
        string fertilizerLabel = isFertilizerOptional ? 
            $"• <color=#90EE90>Fertilizzante</color> (opzionale): {fertilizerStatus}" :
            $"• <color=#90EE90>Fertilizzante</color>: {fertilizerStatus}";
        sb.AppendLine(fertilizerLabel);
        if (!fertilizerOk)
        {
            sb.AppendLine($"  Range ideale: {stageReq.fertilizerMin}% - {stageReq.fertilizerMax}%");
            sb.AppendLine($"  Attuale: {state.FertilizerLevel}%");
        }
        if (isFertilizerOptional)
        {
            sb.AppendLine($"  <color=#FFFF00>Nota: Negli stadi Seed e Sprout, il fertilizzante è opzionale per avanzare.</color>");
        }
        sb.AppendLine();
        
        // Giorni mancanti per avanzare
        int daysInStage = state.DaysInCurrentStage;
        int requiredDays = stageReq.durationDays;
        int daysRemaining = Mathf.Max(0, requiredDays - daysInStage);
        
        if (daysRemaining > 0)
        {
            sb.AppendLine($"<color=#FFFF00>Giorni mancanti per avanzare:</color> <color=#FFFFFF>{daysRemaining}</color>");
            sb.AppendLine($"  (Giorni nello stadio: {daysInStage} / {requiredDays})");
        }
        else
        {
            sb.AppendLine("<color=#00FF00>✓ Giorni minimi raggiunti!</color>");
            if (waterOk && lightOk && fertilizerOk)
        {
                sb.AppendLine("<color=#00FF00>✓ Tutti i parametri sono nel range ideale!</color>");
                sb.AppendLine("<color=#00FF00>La pianta può avanzare al prossimo stadio.</color>");
        }
            else
            {
                sb.AppendLine("<color=#FFFF00>⚠️ Metti tutti i parametri nel range ideale per avanzare.</color>");
            }
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Ottiene il valore numerico della soglia per lo stadio corrente
    /// </summary>
    private int GetStageThresholdValue(int stage)
    {
        if (_growthConfig == null)
            return 0;
            
        switch (stage)
        {
            case (int)PlantStage.Seed:
                return _growthConfig.pointsSeedToSprout;
            case (int)PlantStage.Sprout:
                return _growthConfig.pointsSproutToMature;
            default:
                return 0;
        }
    }

    private void ShowGrowthTooltip(bool show)
    {
        if (growthTooltipPanel == null)
            return;
        
        if (show)
        {
            var state = _currentSelectedPot?.PotActions?.GetCurrentState();
            if (state != null)
            {
                UpdateGrowthTooltip(state);
            }
        }
        
        growthTooltipPanel.SetActive(show);
    }

    private int CalculateCurrentGrowthPoints(PotStateModel state)
    {
        int points = state.GrowthPoints;
        // GDD AZ-11: Usa WateringSystemOn invece di LastWateredDay
        // BLK-02.07: Usa LedSystemState invece di LastLitDay per verificare stato corrente
        bool 
            hadHydration = state.WateringSystemOn,
            hadLight = (state.LedSystemState != LedSystemState.Off);

        points += (hadHydration, hadLight) switch
        {
            (true, true)   => _growthConfig.pointsIdealCare,
            (true, false)  => _growthConfig.pointsPartialCare,
            (false, true)  => _growthConfig.pointsPartialCare,
            (false, false) => _growthConfig.pointsNoCare
        };

        return points;
    }
    
    /// <summary>
    /// BLK-01.03B: Calcola la percentuale di progresso per lo stadio corrente
    /// </summary>
    private float CalculateProgressPercentage(PotStateModel state)
    {
        if (_growthConfig == null) return 0f;

        int points = CalculateCurrentGrowthPoints(state);
        
        switch (state.Stage)
        {
            case (int)PlantStage.Empty:
                return 0f; // Nessun progresso per vasi vuoti
                
            case (int)PlantStage.Seed:
                if (points >= _growthConfig.pointsSeedToSprout)
                    return 100f; // Pronto per avanzare
                return (float)points / _growthConfig.pointsSeedToSprout * 100f;
                
            case (int)PlantStage.Sprout:
                if (points >= _growthConfig.pointsSproutToMature)
                    return 100f; // Pronto per avanzare
                return (float)points / _growthConfig.pointsSproutToMature * 100f;
                
            case (int)PlantStage.HarvestReady:
                return 100f; // Pianta completamente matura
                
            case (int)PlantStage.Resting:
                // BLK-02.05: Pianta in riposo, nessun progresso fino a fertilizzazione
                return 0f;
                
            default:
                return 0f;
        }
    }
    
    /// <summary>
    /// BLK-01.03B: Restituisce il colore per lo stadio corrente (placeholder per sprite)
    /// </summary>
    private Color GetStageColor(int stage)
    {
        switch (stage)
        {
            case (int)PlantStage.Empty:
                return Color.gray;
            case (int)PlantStage.Seed:
                return new Color(0.6f, 0.4f, 0.2f); // Brown color
            case (int)PlantStage.Sprout:
                return Color.green;
            case (int)PlantStage.HarvestReady:
                return Color.yellow;
            case (int)PlantStage.Resting:
                // BLK-02.05: Resting usa un colore grigio-bluastro per indicare riposo
                return new Color(0.6f, 0.6f, 0.8f); // Grigio-blu
            default:
                return Color.white;
        }
    }
    
    #endregion

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
            case 3: return "HarvestReady";
            case 4: return "Flowering";
            case 5: return "HarvestReady";
            case 6: return "Resting";
            default: return $"Stadio {stage}";
        }
    }

    /// <summary>
    /// Restituisce la soglia di punti per lo stadio corrente
    /// </summary>
    private string GetStageThreshold(int stage)
    {
        switch (stage)
        {
            case 0: return "0"; // Empty (nessun avanzamento)
            case 1: return "2"; // Seed to Sprout
            case 2: return "3"; // Sprout to HarvestReady (temporaneo)
            case 3: return "?"; // Growth (da implementare)
            case 4: return "?"; // Flowering (da implementare)
            case 5: return "∞"; // HarvestReady (nessun avanzamento)
            case 6: return "∞"; // Resting (nessun avanzamento)
            default: return "?";
        }
    }
    
    /// <summary>
    /// BLK-01.04: Restituisce informazioni dettagliate sullo stadio
    /// </summary>
    private string GetStageInfo(PotStateModel state)
    {
        if (state.IsEmpty)
        {
            return "Pronto per piantare";
        }

        int points = CalculateCurrentGrowthPoints(state);
        int daysSincePlant = state.DaysSincePlant + 1;
        
        switch (state.Stage)
        {
            case (int)PlantStage.Seed:
                int seedThreshold = _growthConfig != null ? _growthConfig.pointsSeedToSprout : 4;
                return $"Giorno {daysSincePlant} - {Mathf.Clamp(points, 0, seedThreshold)}/{seedThreshold} punti";
            case (int)PlantStage.Sprout:
                int sproutThreshold = _growthConfig != null ? _growthConfig.pointsSproutToMature : 4;
                return $"Giorno {daysSincePlant} - {Mathf.Clamp(points, 0, sproutThreshold)}/{sproutThreshold} punti";
            case (int)PlantStage.HarvestReady:
                return $"Giorno {daysSincePlant} - Pronta per raccolta!";
            case (int)PlantStage.Resting:
                // BLK-02.05: Pianta in riposo dopo la raccolta
                return $"Giorno {daysSincePlant} - In riposo (usa Fertilizzante per riattivare)";
            default:
                return $"Stadio {state.Stage}";
        }
    }
    
    /// <summary>
    /// BLK-01.04: Restituisce informazioni dettagliate sul progresso
    /// </summary>
    private string GetProgressInfo(PotStateModel state)
    {
        if (state.IsEmpty)
        {
            return "0%";
        }
        
        float percentage = CalculateProgressPercentage(state);
        
        switch (state.Stage)
        {
            case (int)PlantStage.Seed:
                return $"{Mathf.RoundToInt(percentage)}% → Sprout";
            case (int)PlantStage.Sprout:
                return $"{Mathf.RoundToInt(percentage)}% → HarvestReady";
            case (int)PlantStage.HarvestReady:
                return "100% - HarvestReady!";
            case (int)PlantStage.Resting:
                // BLK-02.05: Pianta in riposo, nessun progresso fino a fertilizzazione
                return "Riposo";
            default:
                return $"{Mathf.RoundToInt(percentage)}%";
        }
    }
}
