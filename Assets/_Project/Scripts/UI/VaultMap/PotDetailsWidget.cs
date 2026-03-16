using System.Linq;
using System.IO;
using _Project.Sporae.Core;
// GDD AZ-11: Watering namespace rimosso (minigioco deprecato)
// using _Project.Watering;
using Sporae.Dome.PotSystem.Growth;
using Sporae.Dome.PotSystem.Condition;
using Sporae.Dome.PotSystem;
using Sporae.Dome.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.NotificationsFoundation;

namespace _Project
{
    public class PotDetailsWidget : MonoBehaviour
    {
        [SerializeField] private Button _plantButton;
        [SerializeField] private Button _wateringButton;
        [SerializeField] private Button _blueLedButton;
        [SerializeField] private Button _redLedButton;
        [SerializeField] private Button _sprayButton;
        [SerializeField] private Button _harvestButton;
        [SerializeField] private Button _fertilizeButton;  // BLK-03.01-T1
        [SerializeField] private Button _pruningButton;  // AZ-13
        [SerializeField] private Button _uprootButton;
        
        [Header("Pruning Dialog (AZ-13)")]
        [SerializeField] private PruningDialog _pruningDialogPrefab;
        private PruningDialog _pruningDialogInstance;

        [SerializeField] private TextMeshProUGUI _idLabel;
        [SerializeField] private TextMeshProUGUI _stageLabel;
        [SerializeField] private TextMeshProUGUI _plantDescriptionLabel;
        [SerializeField] private ProgressBar _progressBar;
        [SerializeField] private Image _stageImage;
        [SerializeField] private TextMeshProUGUI _growthLabelText;  // Label per stato crescita (IN CRESCITA, Stabile, etc.)
        
        [Header("Plant Stats UI")]
        [SerializeField] private TextMeshProUGUI _hydrationStressText;
        [SerializeField] private TextMeshProUGUI _lightStressText;
        [SerializeField] private TextMeshProUGUI _ledCompatibleText;  // BLK-02.08: LED Compatibile (Blue/Red/ALL)
        [SerializeField] private TextMeshProUGUI _fertilizerText;  // BLK-03.01-T1
        [SerializeField] private TextMeshProUGUI _growthPointsText;  // BLK-03.01-T2
        [SerializeField] private TextMeshProUGUI _optimalDaysText;  // BLK-03.01-T2
        [SerializeField] private TextMeshProUGUI _plantLevelText;  // BLK-02.02: Livello pianta (1-5)
        [SerializeField] private TextMeshProUGUI _moldRiskText;  // BLK-07.01: Mold risk indicator
        [SerializeField] private GameObject _infestationBadge;  // BLK-07.01: Badge "INFESTATA"
        [SerializeField] private ProgressBar _hydrationProgressBar;
        [SerializeField] private ProgressBar _lightProgressBar;
        [SerializeField] private TextMeshProUGUI _phAffinityText;
        [SerializeField] private TextMeshProUGUI _phDriftText;
        [SerializeField] private TextMeshProUGUI _rarityText;
        [SerializeField] private TextMeshProUGUI _effectsText;
        
        [Header("Plant Condition UI")]
        [SerializeField] private TextMeshProUGUI _conditionLabelText;
        [SerializeField] private ProgressBar _conditionBar;
        [SerializeField] private TextMeshProUGUI _conditionForecastText;
        [SerializeField] private GameObject _conditionTooltipPanel;
        [SerializeField] private TextMeshProUGUI _conditionTooltipText;
        
        [Header("Growth Tooltip UI")]
        [Header("Growth Tooltip UI (assegna manualmente in Unity)")]
        [SerializeField] private GameObject _growthTooltipPanel;
        [SerializeField] private TextMeshProUGUI _growthTooltipText;

        [SerializeField] private GameObject _page;
        
        // GDD AZ-11: WateringMinigame rimosso (sistema toggle persistente)
        // [SerializeField] private WateringMinigame _wateringMinigame; // DEPRECATO
        
        [Header("Seed Selector")]
        [SerializeField] private UISeedSelector _seedSelector;
        
        [Header("Fertilizer Selector (BLK-03.01-T1)")]
        [SerializeField] private UIFertilizerSelector _fertilizerSelector;
        
        [Header("UI System Selection")]
        [Tooltip("Se false, disabilita questa UI legacy e usa PlantCardV2 UIToolkit")]
        [SerializeField] private bool _useLegacyUI = true;
        
        private PotSlot _currentSelectedPot;
        private PlantGrowthConfig _growthConfig;
        private GameManager _gameManager;
        private DayCycleSystem _dayCycleSystem;
        private PhSystem _phSystem;
        private PotSystemConfig _potSystemConfig;
        
        // DEBUG_SAFE_FIX: Guard per prevenire chiamate multiple a DoPlant nello stesso frame
        private bool _isProcessingSeedSelection = false;
        
        private void Awake()
        {
            LoadGrowthConfig();
            Initialize();
            Subscribes();
            
            _page.SetActive(false);
        }

        private void OnDestroy()
        {
            Unsubscribes();
            
            // Rimuovi sottoscrizioni seed selector
            if (_seedSelector != null)
            {
                _seedSelector.OnSeedSelected -= OnSeedSelected;
                _seedSelector.OnCancelled -= OnSeedSelectionCancelled;
            }
            
            // BLK-03.01-T1: Rimuovi sottoscrizioni fertilizer selector
            if (_fertilizerSelector != null)
            {
                _fertilizerSelector.OnFertilizerSelected -= OnFertilizerSelected;
                _fertilizerSelector.OnCancelled -= OnFertilizerSelectionCancelled;
            }
        }
        
        private void Update()
        {
            if (_currentSelectedPot && !_currentSelectedPot.Interactable.PlayerInRange)
            {
                _currentSelectedPot = null;
                _page.SetActive(false);
                // Chiudi il tooltip quando si chiude la HUD
                if (_growthTooltipPanel != null)
                {
                    _growthTooltipPanel.SetActive(false);
                }
            }
        }
        
        private void Subscribes()
        {
            PotSlot.OnPotSelected += OnPotSelected;
            PotEvents.OnPotStateChanged += OnPotStateChanged;
            PotEvents.OnPlantGrew += OnPlantGrew;
            PotEvents.OnPlantStageChanged += OnPlantStageChanged;
            PotEvents.OnPlantDied += OnPlantDied;
        }

        private void Unsubscribes()
        {
            PotSlot.OnPotSelected -= OnPotSelected;
            PotEvents.OnPotStateChanged -= OnPotStateChanged;
            PotEvents.OnPlantGrew -= OnPlantGrew;
            PotEvents.OnPlantStageChanged -= OnPlantStageChanged;
            PotEvents.OnPlantDied -= OnPlantDied;
            
            // DEBUG_SAFE_FIX: Rimuovi sottoscrizione a OnActionsChanged
            if (_gameManager != null && _gameManager.ActionSystem != null)
            {
                _gameManager.ActionSystem.OnActionsChanged -= OnActionsChanged;
            }
        }
        
        /// <summary>
        /// DEBUG_SAFE_FIX: Chiamato quando le azioni disponibili cambiano
        /// </summary>
        private void OnActionsChanged(int actionsLeft)
        {
            // Aggiorna i bottoni del pot selezionato quando le azioni cambiano
            // Usa _currentSelectedPot se disponibile, altrimenti trova il pot selezionato
            PotSlot potToUpdate = _currentSelectedPot;
            if (potToUpdate == null)
            {
                potToUpdate = FindSelectedPot();
            }
            
            if (potToUpdate != null)
            {
                UpdateActionButtons(potToUpdate);
            }
        }
        
        private void Initialize()
        {
            _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
            _dayCycleSystem.OnDayChanged += HandleDayChanged;
            
            _gameManager = FindObjectOfType<GameManager>();
            
            // DEBUG_SAFE_FIX: Sottoscrivi a OnActionsChanged se ActionSystem è disponibile
            if (_gameManager != null && _gameManager.ActionSystem != null)
            {
                _gameManager.ActionSystem.OnActionsChanged += OnActionsChanged;
            }
            
            // Inizializza PhSystem e PotSystemConfig per calcolo condizione
            _phSystem = ServiceContainer.Instance.Get<PhSystem>(suppressWarning: true);
            _potSystemConfig = Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");
            if (_potSystemConfig == null)
            {
                var allConfigs = Resources.LoadAll<PotSystemConfig>("Configs");
                if (allConfigs != null && allConfigs.Length > 0)
                {
                    _potSystemConfig = allConfigs[0];
                }
            }
            
            // Setup tooltip per progress bar Growth
            SetupGrowthTooltip();
            
            // Verifica che i bottoni siano assegnati prima di aggiungere listener
            if (_plantButton != null)
                _plantButton.onClick.AddListener(() => OnActionButtonClicked(PotEvents.PotActionType.Plant));
            else
                SporiumLogger.LogError(LogCategory.UI, "_plantButton non assegnato! Collega il riferimento nella scena Unity.");
            
            if (_wateringButton != null)
                _wateringButton.onClick.AddListener(() => OnActionButtonClicked(PotEvents.PotActionType.Water));
            else
                SporiumLogger.LogError(LogCategory.UI, "_wateringButton non assegnato! Collega il riferimento nella scena Unity.");
            
            // BLK-02.07: Due pulsanti separati per Blue e Red (ON/OFF)
            if (_blueLedButton != null)
                _blueLedButton.onClick.AddListener(() => OnBlueLedClicked());
            if (_redLedButton != null)
                _redLedButton.onClick.AddListener(() => OnRedLedClicked());
            if (_sprayButton != null)
                _sprayButton.onClick.AddListener(() => OnActionButtonClicked(PotEvents.PotActionType.Spray));
            if (_harvestButton != null)
                _harvestButton.onClick.AddListener(() => OnActionButtonClicked(PotEvents.PotActionType.Harvest));
            
            // BLK-03.01-T1: Bottone fertilizzante
            if (_fertilizeButton != null)
                _fertilizeButton.onClick.AddListener(() => OnActionButtonClicked(PotEvents.PotActionType.Fertilize));
            
            // AZ-13: Bottone potatura
            if (_pruningButton != null)
                _pruningButton.onClick.AddListener(() => OnActionButtonClicked(PotEvents.PotActionType.Pruning));
            
            if (_uprootButton != null)
                _uprootButton.onClick.AddListener(() => OnActionButtonClicked(PotEvents.PotActionType.Uproot));
            else
                SporiumLogger.LogError(LogCategory.UI, "_uprootButton non assegnato! Collega il riferimento nella scena Unity.");
            
            InitializeSeedSelector();
            InitializeFertilizerSelector();  // BLK-03.01-T1
        }
        
        /// <summary>
        /// Inizializza il selettore semi se non assegnato
        /// </summary>
        private void InitializeSeedSelector()
        {
            if (_seedSelector == null)
            {
                // Cerca UISeedSelector nella scena
                _seedSelector = FindObjectOfType<UISeedSelector>();
                
                if (_seedSelector == null)
                {
                    SporiumLogger.LogError(LogCategory.UI, "UISeedSelector non trovato nella scena! " +
                        "Devi creare un GameObject 'UISeedSelector' nella scena con il componente UISeedSelector " +
                        "e collegare tutti i riferimenti UI necessari. Vedi le istruzioni in Assets/Docs/UISeedSelector_Setup.md");
                    return;
                }
            }
            
            // Nota: La verifica dei riferimenti UI viene fatta direttamente in UISeedSelector.Show()
            // Se i riferimenti non sono assegnati, vedrai un errore nella Console quando provi ad aprire il selettore
            
            // Sottoscrivi agli eventi
            if (_seedSelector != null)
            {
                _seedSelector.OnSeedSelected += OnSeedSelected;
                _seedSelector.OnCancelled += OnSeedSelectionCancelled;
            }
        }
        
        /// <summary>
        /// Gestisce la selezione di un seme
        /// </summary>
        private void OnSeedSelected(string seedTypeId)
        {
            // DEBUG_SAFE_FIX: Guard per prevenire chiamate multiple nello stesso frame
            if (_isProcessingSeedSelection)
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"OnSeedSelected già in esecuzione! Ignorando chiamata duplicata per seedTypeId: {seedTypeId}");
                return;
            }
            
            _isProcessingSeedSelection = true;
            
            try
            {
                SporiumLogger.LogDebug(LogCategory.UI, $"OnSeedSelected chiamato con seedTypeId: {seedTypeId}");
                SporiumLogger.LogDebug(LogCategory.UI, $"_currentSelectedPot: {_currentSelectedPot?.PotId ?? "NULL"}");
                
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
            finally
            {
                // Reset del flag nel prossimo frame per permettere nuove chiamate
                StartCoroutine(ResetSeedSelectionFlag());
            }
        }
        
        /// <summary>
        /// Reset del flag di processing nel prossimo frame
        /// </summary>
        private System.Collections.IEnumerator ResetSeedSelectionFlag()
        {
            yield return null; // Aspetta un frame
            _isProcessingSeedSelection = false;
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
        /// Apre il selettore semi per il vaso specificato
        /// </summary>
        private void OpenSeedSelector(PotSlot targetPot)
        {
            SporiumLogger.LogDebug(LogCategory.UI, $"OpenSeedSelector chiamato per vaso {targetPot?.PotId ?? "NULL"}");
            var dayActivityLog = ServiceContainer.Instance.Get<DayActivityLog>(suppressWarning: true);
            if (dayActivityLog != null && targetPot != null)
                dayActivityLog.RecordDomeActionStarted(targetPot.PotId);
            // Assicurati che il selettore sia inizializzato
            if (_seedSelector == null)
            {
                SporiumLogger.LogDebug(LogCategory.UI, "Inizializzazione seed selector...");
                InitializeSeedSelector();
            }
            
            if (_seedSelector == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "UISeedSelector non disponibile dopo inizializzazione!");
                return;
            }
            
            // Rassicurati che gli eventi siano sempre sottoscritti (in caso di ricreazione)
            _seedSelector.OnSeedSelected -= OnSeedSelected; // Rimuovi prima per evitare duplicati
            _seedSelector.OnSeedSelected += OnSeedSelected;
            _seedSelector.OnCancelled -= OnSeedSelectionCancelled;
            _seedSelector.OnCancelled += OnSeedSelectionCancelled;
            
            SporiumLogger.LogDebug(LogCategory.UI, "Eventi sottoscritti correttamente");
            SporiumLogger.LogDebug(LogCategory.UI, $"Apertura selettore semi per vaso {targetPot?.PotId}");
            
            // Salva il vaso corrente prima di aprire il selettore
            _currentSelectedPot = targetPot;
            
            _seedSelector.Show(targetPot);
        }
        
        /// <summary>
        /// Inizializza il selettore fertilizzante se non assegnato
        /// BLK-03.01-T1: Crea automaticamente se non esiste nella scena
        /// </summary>
        private void InitializeFertilizerSelector()
        {
            if (_fertilizerSelector == null)
            {
                // Cerca UIFertilizerSelector nella scena
                _fertilizerSelector = FindObjectOfType<UIFertilizerSelector>();
                
                if (_fertilizerSelector == null)
                {
                    // Crea automaticamente il GameObject con il componente
                    SporiumLogger.LogInfo(LogCategory.UI, "UIFertilizerSelector non trovato nella scena. Creazione automatica...");
                    GameObject fertilizerSelectorGO = new GameObject("UIFertilizerSelector");
                    _fertilizerSelector = fertilizerSelectorGO.AddComponent<UIFertilizerSelector>();
                    SporiumLogger.LogInfo(LogCategory.UI, "UIFertilizerSelector creato automaticamente!");
                }
            }
            
            // Sottoscrivi agli eventi
            if (_fertilizerSelector != null)
            {
                _fertilizerSelector.OnFertilizerSelected += OnFertilizerSelected;
                _fertilizerSelector.OnCancelled += OnFertilizerSelectionCancelled;
            }
        }
        
        /// <summary>
        /// BLK-03.01-T1: Apre selettore fertilizzante (mostra inventario fertilizzanti)
        /// </summary>
        private void OpenFertilizerSelector(PotSlot targetPot)
        {
            SporiumLogger.LogDebug(LogCategory.UI, $"OpenFertilizerSelector chiamato per vaso {targetPot?.PotId ?? "NULL"}");
            var dayActivityLog = ServiceContainer.Instance.Get<DayActivityLog>(suppressWarning: true);
            if (dayActivityLog != null && targetPot != null)
                dayActivityLog.RecordDomeActionStarted(targetPot.PotId);
            // Assicurati che il selettore sia inizializzato
            if (_fertilizerSelector == null)
            {
                SporiumLogger.LogDebug(LogCategory.UI, "Inizializzazione fertilizer selector...");
                InitializeFertilizerSelector();
            }
            
            if (_fertilizerSelector == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "UIFertilizerSelector non disponibile dopo inizializzazione!");
                return;
            }
            
            // Rassicurati che gli eventi siano sempre sottoscritti (in caso di ricreazione)
            _fertilizerSelector.OnFertilizerSelected -= OnFertilizerSelected; // Rimuovi prima per evitare duplicati
            _fertilizerSelector.OnFertilizerSelected += OnFertilizerSelected;
            _fertilizerSelector.OnCancelled -= OnFertilizerSelectionCancelled;
            _fertilizerSelector.OnCancelled += OnFertilizerSelectionCancelled;
            
            SporiumLogger.LogDebug(LogCategory.UI, "Eventi sottoscritti correttamente");
            SporiumLogger.LogDebug(LogCategory.UI, $"Apertura selettore fertilizzanti per vaso {targetPot?.PotId}");
            
            // Salva il vaso corrente prima di aprire il selettore
            _currentSelectedPot = targetPot;
            
            _fertilizerSelector.Show(targetPot);
        }
        
        /// <summary>
        /// AZ-13: Apre il dialog di potatura con opzione Spray
        /// </summary>
        private void OpenPruningDialog(PotSlot targetPot)
        {
            SporiumLogger.LogDebug(LogCategory.UI, $"OpenPruningDialog chiamato per vaso {targetPot?.PotId ?? "NULL"}");
            var dayActivityLog = ServiceContainer.Instance.Get<DayActivityLog>(suppressWarning: true);
            if (dayActivityLog != null && targetPot != null)
                dayActivityLog.RecordDomeActionStarted(targetPot.PotId);
            // Crea istanza dialog se non esiste
            if (_pruningDialogInstance == null)
            {
                if (_pruningDialogPrefab != null)
                {
                    // BUG FIX: Istanzia nel Canvas root invece che nel transform corrente
                    Canvas canvas = GetComponentInParent<Canvas>();
                    if (canvas == null)
                        canvas = FindObjectOfType<Canvas>();
                    
                    if (canvas != null)
                    {
                        _pruningDialogInstance = Instantiate(_pruningDialogPrefab, canvas.transform);
                    }
                    else
                    {
                        _pruningDialogInstance = Instantiate(_pruningDialogPrefab, transform);
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
                
                // BUG FIX: Mostra toast con esito potatura
                var uiNotification = UnityEngine.Object.FindObjectOfType<UINotification>();
                // Usa nuovo sistema toast se disponibile
                var toastManager = ServiceContainer.Instance?.Get<ToastNotificationManager>(suppressWarning: true);
                if (toastManager != null)
                {
                    string toastMessage = useSpray ? 
                        "✂️ Potatura completata con successo! Spray Antifungino utilizzato." : 
                        "✂️ Potatura completata con successo!";
                    toastManager.ShowSuccess(toastMessage, "PRUNE-SUCCESS-001");
                }
                else if (uiNotification != null)
                {
                    string toastMessage = useSpray ? 
                        "✂️ Potatura completata con successo! Spray Antifungino utilizzato." : 
                        "✂️ Potatura completata con successo!";
                    uiNotification.ShowNotification(toastMessage, 3f, new Color(0.2f, 1f, 0.2f)); // Verde per successo
                }
                
                // Aggiorna UI
                UpdateActionButtons(_currentSelectedPot);
                
                var growthController = _currentSelectedPot.GetComponent<PotGrowthController>();
                if (growthController != null)
                    UpdateStageAndProgressUI(_currentSelectedPot);
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "Potatura fallita");
                
                // BUG FIX: Mostra toast con esito potatura fallita
                var toastManager = ServiceContainer.Instance?.Get<ToastNotificationManager>(suppressWarning: true);
                if (toastManager != null)
                {
                    string toastMessage = useSpray ? 
                        "✂️ Potatura fallita. Spray Antifungino consumato ma potatura non riuscita." : 
                        "✂️ Potatura fallita. Riprova più tardi.";
                    toastManager.ShowError(toastMessage, "PRUNE-FAILED-001");
                }
                else
                {
                    var uiNotification = UnityEngine.Object.FindObjectOfType<UINotification>();
                    if (uiNotification != null)
                    {
                        string toastMessage = useSpray ? 
                            "✂️ Potatura fallita. Spray Antifungino consumato ma potatura non riuscita." : 
                            "✂️ Potatura fallita. Riprova più tardi.";
                        uiNotification.ShowNotification(toastMessage, 3f, new Color(1f, 0.2f, 0.2f)); // Rosso per fallimento
                    }
                }
            }
        }
        
        /// <summary>
        /// Gestisce la selezione di un fertilizzante
        /// </summary>
        private void OnFertilizerSelected(string fertilizerTypeId)
        {
            // DEBUG_SAFE_FIX: Se la nuova UI è attiva o non c'è un pot selezionato, ignora la chiamata
            // Questo previene errori quando il fertilizer selector viene aperto da PlantCardV2Controller
            if (!_useLegacyUI || _currentSelectedPot == null)
            {
                SporiumLogger.LogDebug(LogCategory.UI, $"OnFertilizerSelected ignorato: useLegacyUI={_useLegacyUI}, currentPot={(_currentSelectedPot?.PotId ?? "NULL")}");
                return;
            }
            
            SporiumLogger.LogDebug(LogCategory.UI, $"OnFertilizerSelected chiamato con fertilizerTypeId: {fertilizerTypeId}");
            SporiumLogger.LogDebug(LogCategory.UI, $"_currentSelectedPot: {_currentSelectedPot?.PotId ?? "NULL"}");
            
            if (_currentSelectedPot.PotActions == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "PotActions è NULL quando fertilizzante selezionato!");
                return;
            }
            
            SporiumLogger.LogInfo(LogCategory.UI, $"Applicando fertilizzante {fertilizerTypeId} al vaso {_currentSelectedPot.PotId}");
            
            // Applica il fertilizzante selezionato
            bool success = _currentSelectedPot.PotActions.DoFertilize(fertilizerTypeId);
            
            // Aggiorna sempre l'UI, anche se fallito (potrebbe essere morte pianta)
            UpdateActionButtons(_currentSelectedPot);
            UpdateStageAndProgressUI(_currentSelectedPot);
            
            if (success)
            {
                SporiumLogger.LogInfo(LogCategory.UI, "Fertilizzante applicato con successo!");
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
            // DEBUG_SAFE_FIX: Se la nuova UI è attiva, ignora la chiamata
            if (!_useLegacyUI)
            {
                return;
            }
            
            SporiumLogger.LogDebug(LogCategory.UI, "Selezione fertilizzante annullata");
            // Nessuna azione necessaria
        }
        
        private void LoadGrowthConfig()
        {
            // Prova prima con il nome completo, poi con il nome senza suffisso
            _growthConfig = Resources.Load<PlantGrowthConfig>("Configs/PlantGrowthConfig_Default");
            if (_growthConfig == null)
            {
                _growthConfig = Resources.Load<PlantGrowthConfig>("Configs/PlantGrowthConfig");
            }
            
            if (_growthConfig != null)
            {
                SporiumLogger.LogDebug(LogCategory.UI, $"Config caricata: pointsSeedToSprout={_growthConfig.pointsSeedToSprout}, pointsSproutToMature={_growthConfig.pointsSproutToMature}");
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "PlantGrowthConfig non trovato in Resources/Configs/. Usando valori di default.");
                _growthConfig = ScriptableObject.CreateInstance<PlantGrowthConfig>();
            }
        }

        private void HandleDayChanged(int obj)
        { 
            if (!_currentSelectedPot)
                return;
        
            UpdateActionButtons(_currentSelectedPot);
            // Aggiorna UI completa per riflettere cambiamenti giorno (es. LastLitDay)
            UpdateStageAndProgressUI(_currentSelectedPot);
        }
        
        private void OnPotSelected(PotSlot pot)
        {
            // Se nuova UI è attiva, non aprire legacy UI
            if (!_useLegacyUI)
            {
                SporiumLogger.LogDebug(LogCategory.UI, $"Vaso {pot.PotId} selezionato, ma legacy UI disabilitata");
                return;
            }
            
            SporiumLogger.LogDebug(LogCategory.UI, $"Vaso {pot.PotId} selezionato. PotDetailsWidget: apro automaticamente il pannello");
            SporiumLogger.LogDebug(LogCategory.UI, $"PotActions presente: {pot.PotActions != null}");
        
            // Salva il vaso selezionato corrente
            _currentSelectedPot = pot;
            
            // Apri il pannello automaticamente quando si seleziona un pot
            // (dato che PotHUDWidget è stato rimosso, questo è l'unico modo per aprire il pannello)
            ShowDetails(pot);
        
            SporiumLogger.LogDebug(LogCategory.UI, $"PotDetailsWidget: Vaso {pot.PotId} salvato, pannello aperto");
        }
        
        /// <summary>
        /// Mostra il pannello dettagliato per il vaso selezionato
        /// Chiamato quando l'utente clicca su "Dettagli" nell'HUD minimale
        /// </summary>
        public void ShowDetails(PotSlot pot = null)
        {
            // Se nuova UI è attiva, non mostrare legacy UI
            if (!_useLegacyUI)
            {
                SporiumLogger.LogDebug(LogCategory.UI, "Legacy UI disabilitata, usa PlantCardV2 UIToolkit");
                return;
            }
            
            // Usa il vaso passato o quello già selezionato
            PotSlot targetPot = pot ?? _currentSelectedPot;
            if (targetPot == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "ShowDetails: Nessun vaso selezionato!");
                return;
            }
            
            SporiumLogger.LogDebug(LogCategory.UI, $"ShowDetails: Apertura pannello dettagliato per vaso {targetPot.PotId}");
            
            _currentSelectedPot = targetPot;
            
            if (_page == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "ShowDetails: _page è NULL! Assicurati che il campo Page sia assegnato nell'Inspector.");
                return;
            }
            
            _page.SetActive(true);
        
            // Aggiorna tutti gli elementi UI
            UpdateStageAndProgressUI(targetPot);
            UpdateActionButtons(targetPot);
        
            SporiumLogger.LogDebug(LogCategory.UI, $"ShowDetails: Pannello dettagliato aperto per vaso {targetPot.PotId}");
        }
        
        /// <summary>
        /// Nasconde il pannello dettagliato
        /// </summary>
        public void HideDetails()
        {
            
            if (_page != null)
            {
                _page.SetActive(false);
                SporiumLogger.LogDebug(LogCategory.UI, "HideDetails: Pannello dettagliato chiuso");
            }
        }
        
        /// <summary>
        /// Ricarica PotSystemConfig e forza aggiornamento UI
        /// </summary>
        public void RefreshPotSystemConfig()
        {
            _potSystemConfig = Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");
            if (_potSystemConfig == null)
            {
                var allConfigs = Resources.LoadAll<PotSystemConfig>("Configs");
                if (allConfigs != null && allConfigs.Length > 0)
                {
                    _potSystemConfig = allConfigs[0];
                }
            }
            if (_currentSelectedPot != null)
            {
                UpdateStageAndProgressUI(_currentSelectedPot);
            }
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
                    // GDD AZ-11: Toggle sistema irrigazione (minigioco rimosso)
                    success = selectedPot.PotActions.DoWater();
                    break;
                
                // Light action gestita separatamente con OnLedButtonClicked
                
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
                
                case PotEvents.PotActionType.Uproot:
                    success = selectedPot.PotActions.DoUproot();
                    break;
            }
        
            if (success)
            {
                SporiumLogger.LogInfo(LogCategory.UI, $"Azione {actionType} eseguita con successo!");
                // Aggiorna l'UI
                UpdateActionButtons(selectedPot);

                var growthController = selectedPot.GetComponent<PotGrowthController>();
                if (growthController != null)
                    UpdateStageAndProgressUI(selectedPot);
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"Azione {actionType} fallita!");
            }
        }
        
        /// <summary>
        /// BLK-02.07: Gestisce il click su pulsante LED (toggle: Off → Blue → Red → Off)
        /// </summary>
        /// <summary>
        /// BLK-02.07: Gestisce il click sul pulsante LED Blu (ON/OFF)
        /// </summary>
        private void OnBlueLedClicked()
        {
            PotSlot selectedPot = FindSelectedPot();
            if (selectedPot == null || selectedPot.PotActions == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "Nessun vaso selezionato");
                return;
            }
            
            // Toggle LED Blu: se è già Blue, spegni. Altrimenti, accendi Blue
            bool success = selectedPot.PotActions.DoLight(LedType.Blue);
            
            if (success)
            {
                LedSystemState newState = selectedPot.PotActions.GetLedSystemState();
                SporiumLogger.LogDebug(LogCategory.UI, $"LED Blue: {(newState == LedSystemState.Blue ? "ON" : "OFF")}");
                UpdateActionButtons(selectedPot);
                UpdateStageAndProgressUI(selectedPot);
            }
        }
        
        /// <summary>
        /// BLK-02.07: Gestisce il click sul pulsante LED Rosso (ON/OFF)
        /// </summary>
        private void OnRedLedClicked()
        {
            PotSlot selectedPot = FindSelectedPot();
            if (selectedPot == null || selectedPot.PotActions == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "Nessun vaso selezionato");
                return;
            }
            
            // Toggle LED Rosso: se è già Red, spegni. Altrimenti, accendi Red
            bool success = selectedPot.PotActions.DoLight(LedType.Red);
            
            if (success)
            {
                LedSystemState newState = selectedPot.PotActions.GetLedSystemState();
                SporiumLogger.LogDebug(LogCategory.UI, $"LED Red: {(newState == LedSystemState.Red ? "ON" : "OFF")}");
                UpdateActionButtons(selectedPot);
                UpdateStageAndProgressUI(selectedPot);
            }
        }
        
        /// <summary>
        /// DEPRECATO (BLK-02.07): Mantenuto per compatibilità temporanea
        /// </summary>
        [System.Obsolete("Usare OnBlueLedClicked() o OnRedLedClicked() per nuovo sistema")]
        private void OnLedToggleClicked()
        {
            // Fallback al toggle ciclico se ancora chiamato
            OnBlueLedClicked();
        }
        
        /// <summary>
        /// DEPRECATO (BLK-02.07): Mantenuto per compatibilità temporanea
        /// </summary>
        [System.Obsolete("Usare OnLedToggleClicked() per nuovo sistema toggle")]
        private void OnLedButtonClicked(LedType ledType)
        {
            // Migrazione automatica: converti LedType a LedSystemState
            LedSystemState? newState = ledType == LedType.Blue ? LedSystemState.Blue : LedSystemState.Red;
            PotSlot selectedPot = FindSelectedPot();
            if (selectedPot == null || selectedPot.PotActions == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "Nessun vaso selezionato");
                return;
            }
            
            bool success = selectedPot.PotActions.DoLight(newState);
            
            if (success)
            {
                UpdateActionButtons(selectedPot);
                UpdateStageAndProgressUI(selectedPot);
            }
        }
        
        /// <summary>
        /// Trova il vaso attualmente selezionato
        /// </summary>
        private PotSlot FindSelectedPot() =>
            FindObjectsOfType<PotSlot>().FirstOrDefault(
                pot => pot.PotActions != null && pot.IsSelected
            );
        
        
        /// <summary>
        /// Gestisce il cambio di stato di un vaso
        /// </summary>
        private void OnPotStateChanged(PotSlot pot)
        {
            // Aggiorna i pulsanti se questo è il vaso selezionato
            UpdateActionButtons(pot);
            
            // Aggiorna anche Stage, Idratazione e Light Exposure se è il vaso selezionato
            if (_currentSelectedPot != null && _currentSelectedPot.PotId == pot.PotId)
            {
                UpdateStageAndProgressUI(pot);
            }
        }
        
        private void UpdateActionButtons(PotSlot pot)
        {
            if (!pot || !pot.PotActions)
                return;
        
            // _page.SetActive(pot.InRange);
            
            // Aggiorna lo stato di ogni pulsante (verifica che esistano prima)
            if (_plantButton != null)
                UpdateButtonState(_plantButton, pot.PotActions.CanPlant(), "Piantare");
            
            // GDD AZ-11: Mostra stato toggle ON/OFF per sistema irrigazione
            if (_wateringButton != null)
            {
                bool isWateringOn = pot.PotActions != null && pot.PotActions.IsWateringSystemOn();
                string waterButtonText = isWateringOn ? "Irrigazione ON" : "Irrigazione OFF";
                UpdateButtonState(_wateringButton, pot.PotActions.CanWater(), waterButtonText);
            }
            // BLK-02.07: Due pulsanti separati per Blue e Red (ON/OFF)
            if (_blueLedButton != null)
            {
                LedSystemState currentState = pot.PotActions.GetLedSystemState();
                bool isBlueOn = currentState == LedSystemState.Blue;
                string buttonText = isBlueOn ? "LED Blue ON" : "LED Blue OFF";
                UpdateButtonState(_blueLedButton, pot.PotActions.CanLight(), buttonText);
            }
            if (_redLedButton != null)
            {
                LedSystemState currentState = pot.PotActions.GetLedSystemState();
                bool isRedOn = currentState == LedSystemState.Red;
                string buttonText = isRedOn ? "LED Red ON" : "LED Red OFF";
                UpdateButtonState(_redLedButton, pot.PotActions.CanLight(), buttonText);
            }
            if (_sprayButton != null)
                UpdateButtonState(_sprayButton, pot.PotActions.CanSprayAntifungal(), "Spray");
            if (_harvestButton != null)
                UpdateButtonState(_harvestButton, pot.PotActions.CanHarvest(), "Raccogli");
            if (_pruningButton != null)
                UpdateButtonState(_pruningButton, pot.PotActions.CanPruning(), "Potatura");
            if (_fertilizeButton != null)
                UpdateButtonState(_fertilizeButton, pot.PotActions.CanFertilize(), "Fertilizzare");  // BLK-03.01-T1
            UpdateButtonState(_uprootButton, pot.PotActions.CanUproot(), "Estirpa");
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
        /// BLK-01.03B: Gestisce l'evento OnPlantGrew
        /// </summary>
        private void OnPlantGrew(string potId, PlantStage stage, int oldPoints, int newPoints)
        {
            if (!_currentSelectedPot || _currentSelectedPot.PotId != potId)
                return;
        
            SporiumLogger.LogDebug(LogCategory.UI, $"Pianta cresciuta su {potId}: {oldPoints} → {newPoints} punti. Aggiornamento progress bar...");
            UpdateStageAndProgressUI(_currentSelectedPot);
        }
        
        /// <summary>
        /// BLK-01.03B: Gestisce l'evento OnPlantStageChanged
        /// </summary>
        private void OnPlantStageChanged(string potId, PlantStage stage)
        {
            if (!_currentSelectedPot || _currentSelectedPot.PotId != potId)
                return;
        
            SporiumLogger.LogDebug(LogCategory.UI, $"Stadio cambiato su {potId}: {stage}. Aggiornamento UI...");
            UpdateStageAndProgressUI(_currentSelectedPot);
        }
        
        /// <summary>
        /// Gestisce l'evento di morte della pianta
        /// </summary>
        private void OnPlantDied(string potId, string reason)
        {
            // Mostra Notification (Foundation se attivo, altrimenti legacy)
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
            {
                foundation.PostToast(
                    "PLANT-DEATH-001",
                    new NotificationPayload().With("reason", reason ?? string.Empty));
            }
            else
            {
                var toastManager = ServiceContainer.Instance?.Get<ToastNotificationManager>(suppressWarning: true);
                if (toastManager != null)
                {
                    string toastMessage = $"🚨 Pianta morta! {reason}";
                    toastManager.ShowToast(ToastNotificationType.PlantDied, toastMessage, "PLANT-DEATH-001");
                }
                else
                {
                    var uiNotification = UnityEngine.Object.FindObjectOfType<UINotification>();
                    if (uiNotification != null)
                    {
                        string toastMessage = $"🚨 Pianta morta! {reason}";
                        uiNotification.ShowNotification(toastMessage, 4f, new Color(1f, 0.2f, 0.2f)); // Rosso per morte
                    }
                }
            }
            
            // Aggiorna UI solo se il vaso morto è quello attualmente selezionato
            if (_currentSelectedPot != null && _currentSelectedPot.PotId == potId)
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"Pianta morta su {potId}: {reason}. Aggiornamento UI...");
                UpdateStageAndProgressUI(_currentSelectedPot);
                UpdateActionButtons(_currentSelectedPot);
                
                // Aggiorna anche le visuali del PotGrowthController
                var growthController = _currentSelectedPot.GetComponent<PotGrowthController>();
                if (growthController != null)
                {
                    growthController.UpdateVisuals();
                }
            }
        }

        private Sprite GetStageSprite()
        {
            return _currentSelectedPot.Sprite;
        }
        
        /// <summary>
        /// BLK-01.04: Aggiorna tutti gli elementi UI per stage e progresso
        /// </summary>
        private void UpdateStageAndProgressUI(PotSlot pot)
        {
            if (!pot || !pot.PotActions) 
                return;
            
            PotStateModel state = pot.PotActions.GetCurrentState();
            if (state == null) 
                return;
            
            // Aggiorna PotId con nome pianta se presente
            if (_idLabel)
            {
                string potIdText = pot.PotId;
                
                // Se c'è una pianta piantata (da Seed in avanti), aggiungi il nome della pianta
                if (!state.IsEmpty && state.Stage >= (int)PlantStage.Seed)
                {
                    string plantName = GetPlantDisplayName(state.PlantCode);
                    if (!string.IsNullOrEmpty(plantName))
                    {
                        potIdText = $"{pot.PotId} - {plantName}";
                    }
                }
                
                _idLabel.text = potIdText;
            }

            // Aggiorna descrizione pianta
            // Auto-trova PlantDescription se non è stato collegato manualmente
            if (_plantDescriptionLabel == null)
            {
                _plantDescriptionLabel = FindTextInChildren("PlantDescription");
            }
            
            if (_plantDescriptionLabel != null)
            {
                // Assicurati che richText sia abilitato
                _plantDescriptionLabel.richText = true;

                if (!string.IsNullOrEmpty(state.PlantCode))
                {
                    var plantDatabase = PlantDatabase.Instance;
                    if (plantDatabase != null)
                    {
                        var plantData = plantDatabase.GetPlantDataByCode(state.PlantCode);
                        if (plantData != null && !string.IsNullOrEmpty(plantData.Description))
                        {
                            _plantDescriptionLabel.text = $"<color=#FFFFFF>{plantData.Description}</color>";
                            SporiumLogger.LogDebug(LogCategory.UI, $"Descrizione pianta aggiornata per {state.PlantCode}: {plantData.Description}");
                        }
                        else
                        {
                            _plantDescriptionLabel.text = "<color=#888888>Nessuna descrizione disponibile</color>";
                            SporiumLogger.LogWarning(LogCategory.UI, $"Descrizione non trovata per pianta {state.PlantCode} (PlantData null o Description vuota)");
                        }
                    }
                    else
                    {
                        _plantDescriptionLabel.text = "<color=#FF0000>Errore database piante</color>";
                        SporiumLogger.LogWarning(LogCategory.UI, "PlantDatabase.Instance è null!");
                    }
                }
                else
                {
                    _plantDescriptionLabel.text = "<color=#888888>Nessuna pianta selezionata</color>";
                    SporiumLogger.LogWarning(LogCategory.UI, $"PlantCode vuoto per vaso {_currentSelectedPot?.PotId ?? "Unknown"}");
                }
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "_plantDescriptionLabel non trovato! Verifica che esista un GameObject 'PlantDescription' con TextMeshProUGUI nella gerarchia UI_PotDetails/Panel/Left/");
            }

            // BLK-01.04: Aggiorna Stage Label con informazioni dettagliate
            if (_stageLabel)
            {
                // Assicurati che richText sia abilitato
                _stageLabel.richText = true;
                
                string stageName = GetStageName(state.Stage);
                string stageInfo = GetStageInfo(state);
                _stageLabel.text = $"<color=#CCCCCC>Stage:</color> <color=#00FF00>{stageName}</color> <color=#CCCCCC>-</color> <color=#FFFF00>{stageInfo}</color>";
            }
            
            // BLK-01.04: Aggiorna Stage Icon con colore appropriato
            if (_stageImage != null)
            {
                _stageImage.sprite = GetStageSprite();
                // TODO: Sostituire con sprite reali quando disponibili
            }
    
            // Aggiorna Idratazione e Light Exposure
            UpdatePlantStatsUI(state);
    
            UpdateProgressUI(state);
            
            // Aggiorna Label Growth con stato (IN CRESCITA, Stabile, Difficoltà, Malata)
            UpdateGrowthLabel(state);
            
            // Aggiorna Condizione pianta
            UpdateConditionUI(state);
        }
        
        /// <summary>
        /// Aggiorna gli elementi UI per Idratazione, Light Exposure e altri stats
        /// </summary>
        private void UpdatePlantStatsUI(PotStateModel state)
        {
            if (state == null) return;
            
            // Auto-trova PlantDescription se non è stato collegato manualmente
            if (_plantDescriptionLabel == null)
            {
                _plantDescriptionLabel = FindTextInChildren("PlantDescription");
            }
            
            // Auto-trova i riferimenti se non sono stati collegati manualmente
            if (_hydrationStressText == null)
            {
                _hydrationStressText = FindTextInChildren("Hydration stress");
            }
            
            if (_lightStressText == null)
            {
                _lightStressText = FindTextInChildren("Light stress");
            }
            
            // BLK-02.08: Auto-trova LED Compatible text se non assegnato
            if (_ledCompatibleText == null)
            {
                _ledCompatibleText = FindTextInChildren("LED Compatible");
            }
            
            // BLK-03.01-T1: Auto-trova fertilizer text se non assegnato
            if (_fertilizerText == null)
            {
                _fertilizerText = FindTextInChildren("Fertilizer");
            }
            
            // BLK-03.01-T2: Auto-trova growth points text se non assegnato
            if (_growthPointsText == null)
            {
                _growthPointsText = FindTextInChildren("Growth Points");
            }
            
            // BLK-03.01-T2: Auto-trova optimal days text se non assegnato
            if (_optimalDaysText == null)
            {
                _optimalDaysText = FindTextInChildren("Optimal Days");
            }
            
            // BLK-02.02: Auto-trova plant level text se non assegnato
            if (_plantLevelText == null)
            {
                _plantLevelText = FindTextInChildren("Plant Level");
            }
            
            // BLK-07.01: Auto-trova mold risk text se non assegnato
            if (_moldRiskText == null)
            {
                _moldRiskText = FindTextInChildren("Rischio muffa");
            }
            
            // Fix: Rimuovi caratteri Unicode non supportati dal testo iniziale (es. ✅ \u2705)
            // Questo previene il warning di TextMeshPro quando il font non supporta questi caratteri
            if (_moldRiskText != null)
            {
                string originalText = _moldRiskText.text;
                // Rimuovi caratteri emoji comuni non supportati da LiberationSans SDF
                string cleanedText = originalText
                    .Replace("\u2705", "")  // ✅ checkmark
                    .Replace("\u26A0", "")  // ⚠️ warning
                    .Replace("\u274C", "")  // ❌ cross mark
                    .Replace("\u2713", "[OK]")  // ✓ checkmark (sostituisci con testo)
                    .Trim();
                
                if (cleanedText != originalText)
                {
                    _moldRiskText.text = cleanedText;
                }
            }
            
            // BLK-07.01: Auto-trova infestation badge se non assegnato
            if (_infestationBadge == null)
            {
                var badge = FindObjectInChildren("Infestation Badge");
                if (badge != null) _infestationBadge = badge;
            }
            
            // Auto-trova le progress bar se non sono state collegate manualmente
            if (_hydrationProgressBar == null)
            {
                _hydrationProgressBar = FindProgressBarInChildren("Hydration Progress");
            }
            
            if (_lightProgressBar == null)
            {
                _lightProgressBar = FindProgressBarInChildren("Lighting Progress");
            }
            
            if (_phAffinityText == null)
            {
                _phAffinityText = FindTextInChildren("pH Affinity");
            }
            
            if (_phDriftText == null)
            {
                _phDriftText = FindTextInChildren("pH Drift");
            }
            
            if (_rarityText == null)
            {
                _rarityText = FindTextInChildren("Rarity");
            }

            // Auto-trova Effects se non è stato collegato manualmente
            if (_effectsText == null)
            {
                _effectsText = FindTextInChildren("Effects");
            }
            
            // Aggiorna Hydration Stress (mostra percentuale e range ideale per lo stadio corrente)
            int maxHydration = _currentSelectedPot?.PotActions?.GetMaxHydration() ?? 10; // 10 step = 10% ciascuno
            float hydrationPercentage = maxHydration > 0 ? (float)state.Hydration / maxHydration * 100f : 0f;
            
            // GDD AZ-11: Mostra anche lo stato del sistema irrigazione
            string wateringStatus = state.WateringSystemOn ? " <color=#00FF00>[ON]</color>" : " <color=#FF0000>[OFF]</color>";
            
            if (_hydrationStressText != null)
            {
                // Assicurati che richText sia abilitato
                _hydrationStressText.richText = true;
                
                // MODIFICA 1: Determina colore in base al range ideale
                string hydrationColor = "#FFFF00"; // Default: giallo
                string rangeText = "";
                
                // Se c'è una pianta, verifica se è nel range ideale
                if (!string.IsNullOrEmpty(state.PlantCode))
                {
                    var plantDatabase = PlantDatabase.Instance;
                    if (plantDatabase != null)
                    {
                        var plantData = plantDatabase.GetPlantDataByCode(state.PlantCode);
                        if (plantData != null)
                        {
                            var stageReq = plantData.GetStageRequirements((PlantStage)state.Stage);
                            if (stageReq != null)
                            {
                                // MODIFICA 1: Logica colori: 0% o 100% = rosso, nei parametri ideali = verde, altre = arancione
                                int hydrationInt = (int)hydrationPercentage;
                                if (hydrationInt == 0 || hydrationInt == 100)
                                {
                                    hydrationColor = "#FF0000"; // Rosso per 0% o 100%
                                }
                                else if (stageReq.IsHydrationOptimal(hydrationInt))
                                {
                                    hydrationColor = "#00FF00"; // Verde se ottimale
                                }
                                else if (stageReq.IsHydrationInRange(hydrationInt))
                                {
                                    hydrationColor = "#FF6600"; // Arancione se nel range ma non ottimale
                                }
                                else
                                {
                                    hydrationColor = "#FF6600"; // Arancione se fuori range
                                }
                                
                                // Mostra il range ideale (min-med-max) per lo stadio corrente
                                rangeText = $" <color=#CCCCCC>(Range:</color> <color=#00FF00>{stageReq.hydrationMin}%-{stageReq.hydrationMed}%-{stageReq.hydrationMax}%</color><color=#CCCCCC>)</color>";
                            }
                        }
                    }
                }
                
                string hydrationText = $"<color=#CCCCCC>Hydration:</color> <color={hydrationColor}>{hydrationPercentage:F0}%</color>{wateringStatus}{rangeText}";
                
                _hydrationStressText.text = hydrationText;
                SporiumLogger.LogDebug(LogCategory.UI, $"Aggiornato Hydration: {hydrationPercentage:F0}% (Hydration={state.Hydration}/{maxHydration}, Sistema={state.WateringSystemOn})");
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "_hydrationStressText non trovato! Collega il riferimento nella scena Unity.");
            }
            
            // Nascondi la progress bar come richiesto (il player deve vedere la percentuale invece)
            if (_hydrationProgressBar != null)
            {
                _hydrationProgressBar.gameObject.SetActive(false);
            }
            
            // Aggiorna Light Stress (mostra percentuale abuso luce: decresce gradualmente quando LED spento)
            if (_lightStressText != null)
            {
                _lightStressText.richText = true;
                
                float stressPercentage = 0f;
                
                // Calcola stress basato su giorni consecutivi (sia LED acceso che spento)
                // Quando LED è spento, i giorni consecutivi decrescono di 1 al giorno (25% di stress)
                int consecutiveDays = state.GetConsecutiveLedDays();
                
                // Stress calcolato sempre in base ai giorni consecutivi residui
                // 0 giorni = 0% (nessuno stress)
                // 1 giorno = 20%
                // 2 giorni = 40%
                // 3 giorni = 60%
                // 4 giorni = 80%
                // 5+ giorni = 100% (zona rossa, malus attivo)
                int maxDaysForFullStress = _potSystemConfig != null ? _potSystemConfig.MaxDaysForFullStress : 5;
                stressPercentage = Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f;

                // Nota: Quando LED è spento, i giorni consecutivi decrescono gradualmente (20% al giorno)
                // seguendo la stessa logica della crescita ma al contrario
                
                // MODIFICA: Colore in base allo stress con nuova scala colori
                // 0%: grigio neutro, 1-50%: arancione, 51-75%: viola, 76-100%: rosso
                string stressColor = stressPercentage switch
                {
                    > 75f => "#FF0000",   // Rosso per stress alto (76-100%)
                    > 50f => "#800080",   // Viola per stress medio-alto (51-75%)
                    > 0f => "#FF6600",    // Arancione per stress basso-medio (1-50%)
                    _ => "#808080"        // Grigio neutro per nessuno stress (0%)
                };
                
                // MODIFICA 2: Aggiungere range ideale a Light Stress (come per Hydration e Fertilizzante)
                // MODIFICA: Applica lo stesso colore dello stress anche al range
                string rangeText = "";
                if (!string.IsNullOrEmpty(state.PlantCode))
                {
                    var plantDatabase = PlantDatabase.Instance;
                    if (plantDatabase != null)
                    {
                        var plantData = plantDatabase.GetPlantDataByCode(state.PlantCode);
                        if (plantData != null)
                        {
                            var stageReq = plantData.GetStageRequirements((PlantStage)state.Stage);
                            if (stageReq != null)
                            {
                                rangeText = $" <color=#CCCCCC>(Range:</color> <color={stressColor}>{stageReq.lightMin}%-{stageReq.lightMed}%-{stageReq.lightMax}%</color><color=#CCCCCC>)</color>";
                            }
                        }
                    }
                }
                
                _lightStressText.text = $"<color=#CCCCCC>Light Stress:</color> <color={stressColor}>{stressPercentage:F0}%</color>{rangeText}";
                SporiumLogger.LogDebug(LogCategory.UI, $"Aggiornato Light Stress: {stressPercentage:F0}% (LED: {state.LedSystemState}, Giorni consecutivi: {state.GetConsecutiveLedDays()})");
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "_lightStressText non trovato! Collega il riferimento nella scena Unity.");
            }
            
            // Nascondi la progress bar come richiesto (il player deve vedere la percentuale invece)
            if (_lightProgressBar != null)
            {
                _lightProgressBar.gameObject.SetActive(false);
            }
            
            // BLK-02.08: Aggiorna LED Compatibile (mostra LED compatibili per famiglia)
            if (_ledCompatibleText != null)
            {
                _ledCompatibleText.richText = true;
                
                string ledCompatibleDisplay = "ALL";
                if (!string.IsNullOrEmpty(state.PlantCode))
                {
                    var plantDatabase = PlantDatabase.Instance;
                    if (plantDatabase != null)
                    {
                        var plantData = plantDatabase.GetPlantDataByCode(state.PlantCode);
                        if (plantData != null)
                        {
                            LedCompatibility compatible = LedCompatibilityHelper.GetCompatibleLedTypes(plantData.Family);
                            ledCompatibleDisplay = LedCompatibilityHelper.GetCompatibleLedDisplay(compatible);
                        }
                    }
                }
                
                _ledCompatibleText.text = $"<color=#CCCCCC>LED Compatibile:</color> <color=#00FF00>{ledCompatibleDisplay}</color>";
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "_ledCompatibleText non trovato! Collega il riferimento nella scena Unity o aggiungi un GameObject 'LED Compatible' con TextMeshProUGUI.");
            }
            
            // BLK-03.01-T1: Aggiorna Fertilizzante (mostra livello corrente e range ideale per lo stadio)
            if (_fertilizerText != null)
            {
                _fertilizerText.richText = true;
                
                if (state.IsEmpty || !state.HasPlant)
                {
                    _fertilizerText.text = "<color=#CCCCCC>Fertilizzante:</color> <color=#888888>-</color>";
                }
                else
                {
                    int currentFertilizer = state.FertilizerLevel;
                    // MODIFICA 1: Determina colore in base al range ideale
                    string fertilizerColor = "#FFFF00"; // Default: giallo
                    string rangeText = "";
                    
                    // Se c'è una pianta, mostra anche il range ideale per lo stadio corrente
                    if (!string.IsNullOrEmpty(state.PlantCode))
                    {
                        var plantDatabase = PlantDatabase.Instance;
                        if (plantDatabase != null)
                        {
                            var plantData = plantDatabase.GetPlantDataByCode(state.PlantCode);
                            if (plantData != null)
                            {
                                var stageReq = plantData.GetStageRequirements((PlantStage)state.Stage);
                                if (stageReq != null)
                                {
                                    // MODIFICA: Nuova logica colori fertilizzante
                                    // Da 0 a soglia minima: grigio
                                    // Dalla soglia minima alla massima: verde
                                    // Oltre la massima: grigio
                                    if (currentFertilizer < stageReq.fertilizerMin)
                                    {
                                        fertilizerColor = "#808080"; // Grigio se sotto il minimo
                                    }
                                    else if (currentFertilizer >= stageReq.fertilizerMin && currentFertilizer <= stageReq.fertilizerMax)
                                    {
                                        fertilizerColor = "#00FF00"; // Verde se nel range (min-max incluso)
                                    }
                                    else // currentFertilizer > stageReq.fertilizerMax
                                    {
                                        fertilizerColor = "#808080"; // Grigio se oltre il massimo
                                    }
                                    
                                    // Mostra il range ideale (min-med-max) per lo stadio corrente
                                    // MODIFICA: Applica lo stesso colore del fertilizzante anche al range
                                    rangeText = $" <color=#CCCCCC>(Range:</color> <color={fertilizerColor}>{stageReq.fertilizerMin}%-{stageReq.fertilizerMed}%-{stageReq.fertilizerMax}%</color><color=#CCCCCC>)</color>";
                                }
                            }
                        }
                    }
                    
                    string fertilizerText = $"<color=#CCCCCC>Fertilizzante:</color> <color={fertilizerColor}>{currentFertilizer}%</color>{rangeText}";
                    
                    _fertilizerText.text = fertilizerText;
                    SporiumLogger.LogDebug(LogCategory.UI, $"Aggiornato Fertilizzante: {currentFertilizer}%");
                }
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "_fertilizerText non trovato! Collega il riferimento nella scena Unity.");
            }
            
            // BLK-03.01-T2: Aggiorna Growth Points (mostra punti accumulati per ogni parametro)
            if (_growthPointsText != null)
            {
                _growthPointsText.richText = true;
                
                if (state.IsEmpty || !state.HasPlant)
                {
                    _growthPointsText.text = "<color=#CCCCCC>Punti Crescita:</color> <color=#888888>-</color>";
                }
                else
                {
                    int totalPoints = state.GrowthPointsWater + state.GrowthPointsLight + state.GrowthPointsFertilizer;
                    string pointsText = $"<color=#CCCCCC>Punti Crescita:</color> " +
                                       $"<color=#3F6FFF>W:{state.GrowthPointsWater}</color> " +
                                       $"<color=#FFD700>L:{state.GrowthPointsLight}</color> " +
                                       $"<color=#90EE90>F:{state.GrowthPointsFertilizer}</color> " +
                                       $"<color=#CCCCCC>(Tot: {totalPoints}/3)</color>";
                    
                    // Cambia colore totale in base ai punti
                    if (totalPoints >= 3)
                        pointsText = pointsText.Replace("<color=#CCCCCC>(Tot:", "<color=#00FF00>(Tot:");
                    else if (totalPoints >= 2)
                        pointsText = pointsText.Replace("<color=#CCCCCC>(Tot:", "<color=#FFFF00>(Tot:");
                    else if (totalPoints >= 1)
                        pointsText = pointsText.Replace("<color=#CCCCCC>(Tot:", "<color=#FFA500>(Tot:");
                    else
                        pointsText = pointsText.Replace("<color=#CCCCCC>(Tot:", "<color=#FF0000>(Tot:");
                    
                    _growthPointsText.text = pointsText;
                    SporiumLogger.LogDebug(LogCategory.UI, $"Aggiornato Growth Points: W:{state.GrowthPointsWater} L:{state.GrowthPointsLight} F:{state.GrowthPointsFertilizer} (Tot: {totalPoints})");
                }
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "_growthPointsText non trovato! Collega il riferimento nella scena Unity.");
            }
            
            // BLK-03.01-T2: Aggiorna Optimal Days (mostra giorni consecutivi con parametri ottimali)
            if (_optimalDaysText != null)
            {
                _optimalDaysText.richText = true;
                
                if (state.IsEmpty || !state.HasPlant)
                {
                    _optimalDaysText.text = "<color=#CCCCCC>Giorni Ottimali:</color> <color=#888888>-</color>";
                }
                else
                {
                    string optimalText = $"<color=#CCCCCC>Giorni Ottimali:</color> <color=#FFFF00>{state.DaysConsecutiveOptimal}</color>";
                    
                    // Cambia colore in base ai giorni ottimali
                    if (state.DaysConsecutiveOptimal >= 3)
                        optimalText = optimalText.Replace("<color=#FFFF00>", "<color=#00FF00>");
                    else if (state.DaysConsecutiveOptimal >= 2)
                        optimalText = optimalText.Replace("<color=#FFFF00>", "<color=#FFD700>");
                    else if (state.DaysConsecutiveOptimal >= 1)
                        optimalText = optimalText.Replace("<color=#FFFF00>", "<color=#FFA500>");
                    else
                        optimalText = optimalText.Replace("<color=#FFFF00>", "<color=#888888>");
                    
                    _optimalDaysText.text = optimalText;
                    SporiumLogger.LogDebug(LogCategory.UI, $"Aggiornato Optimal Days: {state.DaysConsecutiveOptimal}");
                }
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "_optimalDaysText non trovato! Collega il riferimento nella scena Unity.");
            }
            
            // BLK-02.02: Aggiorna Plant Level
            if (_plantLevelText != null)
            {
                _plantLevelText.richText = true;
                if (state.IsEmpty || !state.HasPlant)
                {
                    _plantLevelText.text = "<color=#CCCCCC>Livello:</color> <color=#888888>-</color>";
                }
                else
                {
                    string levelColor = state.PlantLevel >= 3 ? "#00FF00" : "#FFFFFF";
                    _plantLevelText.text = $"<color=#CCCCCC>Livello:</color> <color={levelColor}>{state.PlantLevel}/5</color> <color=#888888>(Cicli: {state.CompletedCycles})</color>";
                }
            }
            
            // BLK-07.01: Aggiorna Mold Risk
            if (_moldRiskText != null)
            {
                _moldRiskText.richText = true;
                if (state.IsEmpty || !state.HasPlant)
                {
                    _moldRiskText.text = "<color=#CCCCCC>Rischio muffa:</color> <color=#888888>-</color>";
                }
                else if (state.MoldRiskLevel > 0)
                {
                    string riskLevel = state.MoldRiskLevel switch
                    {
                        1 => "Lieve",
                        2 => "Severo",
                        3 => "Critico",
                        _ => "Sconosciuto"
                    };
                    // MODIFICA 1: Logica colori: 0% o 100% = rosso, nei parametri ideali = verde, altre = arancione
                    // Per Mold Risk: Lvl 0 = verde (nessuno), Lvl 1-2 = arancione, Lvl 3 = rosso
                    string riskColor = state.MoldRiskLevel == 3 ? "#FF0000" : "#FF6600"; // Rosso solo per Critical (Lvl 3), arancione per altri
                    _moldRiskText.text = $"<color=#CCCCCC>Rischio muffa:</color> <color={riskColor}>{riskLevel} (Liv. {state.MoldRiskLevel})</color>";
                }
                else
                {
                    _moldRiskText.text = "<color=#CCCCCC>Rischio muffa:</color> <color=#00FF00>Nessuno</color>";
                }
            }
            
            // BLK-07.01: Aggiorna Infestation Badge
            if (_infestationBadge != null)
            {
                // BUG FIX 2: Badge INFESTATA solo se IsInfested = true (dopo 2 giorni a livello 3)
                bool showBadge = !state.IsEmpty && state.HasPlant && state.IsInfested;
                _infestationBadge.SetActive(showBadge);
                
                // Cambia colore del badge in base al livello
                if (showBadge && _infestationBadge != null)
                {
                    var badgeImage = _infestationBadge.GetComponent<UnityEngine.UI.Image>();
                    if (badgeImage != null)
                    {
                        // Arancione per lvl 2, rosso per lvl 3
                        badgeImage.color = state.MoldRiskLevel >= 3 ? new Color(1f, 0.2f, 0.2f) : new Color(1f, 0.5f, 0.2f);
                    }
                }
            }
            
            // Aggiorna pH Affinity (mostra pH ottimale della pianta se disponibile)
            if (_phAffinityText != null)
            {
                // Assicurati che richText sia abilitato
                _phAffinityText.richText = true;
                
                if (!string.IsNullOrEmpty(state.PlantCode))
                {
                    var plantDatabase = PlantDatabase.Instance;
                    if (plantDatabase != null)
                    {
                        var plantData = plantDatabase.GetPlantDataByCode(state.PlantCode);
                        if (plantData != null)
                        {
                            // MODIFICA 4: Rimuovere duplicazione "pH Affinity:" - tenere solo valore numerico (la label è già nella HUD)
                            _phAffinityText.text = $"<color=#00FF00>{plantData.OptimalPhMin:F1} - {plantData.OptimalPhMax:F1}</color>";
                            SporiumLogger.LogDebug(LogCategory.UI, $"pH Affinity aggiornato per {state.PlantCode}: {plantData.OptimalPhMin:F1} - {plantData.OptimalPhMax:F1}");
                        }
                        else
                        {
                            // MODIFICA 4: Rimuovere duplicazione "pH Affinity:" - tenere solo valore numerico
                            _phAffinityText.text = "<color=#FF0000>N/D</color>";
                            SporiumLogger.LogWarning(LogCategory.UI, $"PlantData non trovato per PlantCode: {state.PlantCode}");
                        }
                    }
                    else
                    {
                        // MODIFICA 4: Rimuovere duplicazione "pH Affinity:" - tenere solo valore numerico
                        _phAffinityText.text = "<color=#FF0000>N/D</color>";
                        SporiumLogger.LogWarning(LogCategory.UI, "PlantDatabase.Instance è null!");
                    }
                }
                else
                {
                    // MODIFICA 4: Rimuovere duplicazione "pH Affinity:" - tenere solo valore numerico
                    _phAffinityText.text = "<color=#FF0000>N/D</color>";
                    SporiumLogger.LogWarning(LogCategory.UI, $"PlantCode vuoto o null per vaso {_currentSelectedPot?.PotId ?? "Unknown"}");
                }
            }
            
            // Aggiorna pH Drift (mostra drift pH giornaliero della pianta se disponibile)
            if (_phDriftText != null)
            {
                // Assicurati che richText sia abilitato
                _phDriftText.richText = true;
                
                if (!string.IsNullOrEmpty(state.PlantCode) && !state.IsEmpty)
                {
                    var plantDatabase = PlantDatabase.Instance;
                    if (plantDatabase != null)
                    {
                        var plantData = plantDatabase.GetPlantDataByCode(state.PlantCode);
                        if (plantData != null)
                        {
                            float phDrift = plantData.GetDailyPhDrift();
                            
                            // Determina il colore in base al valore del drift
                            string colorTag;
                            if (phDrift > 0)
                                colorTag = "<color=#4DCC4D>"; // Verde per drift positivo (Pure)
                            else if (phDrift < 0)
                                colorTag = "<color=#CC4D4D>"; // Rosso per drift negativo (Evil)
                            else
                                colorTag = "<color=#999999>"; // Grigio per drift zero (Standard)
                            
                            _phDriftText.text = $"<color=#CCCCCC>pH Drift:</color> {colorTag}{phDrift:+#;-#;0}/giorno</color>";
                            SporiumLogger.LogDebug(LogCategory.UI, $"pH Drift aggiornato per {state.PlantCode}: {phDrift:+#;-#;0}/giorno");
                        }
                        else
                        {
                            _phDriftText.text = "<color=#CCCCCC>pH Drift:</color> <color=#FF0000>N/D</color>";
                            SporiumLogger.LogWarning(LogCategory.UI, $"PlantData non trovato per PlantCode: {state.PlantCode}");
                        }
                    }
                    else
                    {
                        _phDriftText.text = "<color=#CCCCCC>pH Drift:</color> <color=#FF0000>N/D</color>";
                        SporiumLogger.LogWarning(LogCategory.UI, "PlantDatabase.Instance è null!");
                    }
                }
                else
                {
                    _phDriftText.text = "<color=#CCCCCC>pH Drift:</color> <color=#888888>-/giorno</color>";
                }
            }
            
            // Aggiorna Rarity (mostra rarità della pianta se disponibile)
            if (_rarityText != null)
            {
                // Assicurati che richText sia abilitato
                _rarityText.richText = true;
                
                if (!string.IsNullOrEmpty(state.PlantCode))
                {
                    var plantDatabase = PlantDatabase.Instance;
                    if (plantDatabase != null)
                    {
                        var plantData = plantDatabase.GetPlantDataByCode(state.PlantCode);
                        if (plantData != null)
                        {
                            _rarityText.text = $"<color=#CCCCCC>Rarity:</color> <color=#FFD700>{plantData.Rarity}</color>";
                        }
                        else
                        {
                            _rarityText.text = "<color=#CCCCCC>Rarity:</color> <color=#FF0000>{}</color>";
                        }
                    }
                    else
                    {
                        _rarityText.text = "<color=#CCCCCC>Rarity:</color> <color=#FF0000>{}</color>";
                    }
                }
                else
                {
                    _rarityText.text = "<color=#CCCCCC>Rarity:</color> <color=#FF0000>{}</color>";
                }
            }

            // Aggiorna Effects (mostra potere attivo della pianta)
            if (_effectsText != null)
            {
                // Assicurati che richText sia abilitato
                _effectsText.richText = true;

                if (!string.IsNullOrEmpty(state.PlantCode))
                {
                    var plantDatabase = PlantDatabase.Instance;
                    if (plantDatabase != null)
                    {
                        var plantData = plantDatabase.GetPlantDataByCode(state.PlantCode);
                        if (plantData != null)
                        {
                            // Debug dettagliato per capire il problema
                            string activePowerValue = plantData.ActivePower;
                            bool isNullOrEmpty = string.IsNullOrEmpty(activePowerValue);
                            
                            SporiumLogger.LogDebug(LogCategory.UI, $"DEBUG ActivePower per {state.PlantCode}: " +
                                $"PlantData trovato={plantData != null}, " +
                                $"ActivePower null/empty={isNullOrEmpty}, " +
                                $"ActivePower length={activePowerValue?.Length ?? 0}, " +
                                $"ActivePower value='{activePowerValue}'");
                            
                            if (!isNullOrEmpty)
                            {
                                _effectsText.text = $"<color=#CCCCCC>Potere Attivo:</color>\n<color=#FFD700>{activePowerValue}</color>";
                                SporiumLogger.LogDebug(LogCategory.UI, $"Potere attivo aggiornato per {state.PlantCode}: {activePowerValue}");
                            }
                            else
                            {
                                _effectsText.text = "<color=#CCCCCC>Potere Attivo:</color>\n<color=#888888>Nessun potere attivo disponibile</color>";
                                SporiumLogger.LogWarning(LogCategory.UI, $"Potere attivo vuoto per pianta {state.PlantCode}. Verifica l'asset PlantData in Unity Editor e ricarica l'asset (Ctrl+R o Assets > Refresh).");
                            }
                        }
                        else
                        {
                            _effectsText.text = "<color=#CCCCCC>Potere Attivo:</color>\n<color=#888888>Nessun potere attivo disponibile</color>";
                            SporiumLogger.LogWarning(LogCategory.UI, $"PlantData null per PlantCode: {state.PlantCode}");
                        }
                    }
                    else
                    {
                        _effectsText.text = "<color=#CCCCCC>Potere Attivo:</color>\n<color=#FF0000>Errore database piante</color>";
                        SporiumLogger.LogWarning(LogCategory.UI, "PlantDatabase.Instance è null!");
                    }
                }
                else
                {
                    _effectsText.text = "<color=#CCCCCC>Potere Attivo:</color>\n<color=#888888>Nessuna pianta selezionata</color>";
                    SporiumLogger.LogWarning(LogCategory.UI, $"PlantCode vuoto per vaso {_currentSelectedPot?.PotId ?? "Unknown"}");
                }
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "_effectsText non trovato! Verifica che esista un GameObject 'Effects' con TextMeshProUGUI nella gerarchia UI_PotDetails/Panel/Right/");
            }
        }
        
        private void UpdateProgressUI(PotStateModel state)
        {
            float progressPercentage = CalculateProgressPercentage(state);
            
            // MODIFICA 3: Rimuovere la barra Growth e tenere solo la label
            if (_progressBar != null)
            {
                _progressBar.gameObject.SetActive(false); // Nascondi la barra
            }
        
            SporiumLogger.LogDebug(LogCategory.UI, $"UI aggiornata: {state.PotId} - {GetStageName(state.Stage)} - {progressPercentage:F1}% - {GetProgressInfo(state)}");
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
        
         private float CalculateProgressPercentage(PotStateModel state)
        {
            if (!_growthConfig) 
                return 0f;
    
            int points = CalculateCurrentGrowthPoints(state);
            
            switch (state.Stage)
            {
                case (int)PlantStage.Empty:
                    return 0f; // Nessun progresso per vasi vuoti
                    
                case (int)PlantStage.Seed:
                    float seedProgress = (float)points / _growthConfig.pointsSeedToSprout * 100f;
                    SporiumLogger.LogDebug(LogCategory.UI, $"DEBUG Progress Seed: points={points}, threshold={_growthConfig.pointsSeedToSprout}, progress={seedProgress:F1}%");
                    if (points >= _growthConfig.pointsSeedToSprout)
                        return 100f; // Pronto per avanzare
                    return seedProgress;
                    
                case (int)PlantStage.Sprout:
                    float sproutProgress = (float)points / _growthConfig.pointsSproutToMature * 100f;
                    SporiumLogger.LogDebug(LogCategory.UI, $"DEBUG Progress Sprout: points={points}, threshold={_growthConfig.pointsSproutToMature}, progress={sproutProgress:F1}%");
                    if (points >= _growthConfig.pointsSproutToMature)
                        return 100f; // Pronto per avanzare
                    return sproutProgress;
                    
                case (int)PlantStage.HarvestReady:
                    return 100f; // Pianta completamente matura
                    
                default:
                    return 0f;
            }
        }
         
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
                default:
                    return Color.white;
            }
        }
        
        private string GetStageInfo(PotStateModel state)
        {
            if (state == null) return "";
            
            if (state.IsEmpty)
            {
                return "Pronto per piantare";
            }

            int points = CalculateCurrentGrowthPoints(state);
            int daysSincePlant = state.DaysSincePlant + 1;
            
            switch (state.Stage)
            {
                case (int)PlantStage.Seed:
                    return $"Giorno {daysSincePlant}";
                case (int)PlantStage.Sprout:
                    return $"Giorno {daysSincePlant}";
                case (int)PlantStage.HarvestReady:
                    return $"Giorno {daysSincePlant} - Pronta per raccolta!";
                default:
                    return $"Stadio {state.Stage}";
            }
        }
        
        private string GetStageName(int stage)
        {
            switch (stage)
            {
                case 0: return "Vuoto";
                case 1: return "Seme";
                case 2: return "Germoglio";
                case 3: return "Crescita";
                case 4: return "Fioritura";
                case 5: return "Pronto raccolto";
                case 6: return "Riposo";
                default: return $"Stadio {stage}";
            }
        }
        
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
                default:
                    return $"{Mathf.RoundToInt(percentage)}%";
            }
        }
        
        /// <summary>
        /// Restituisce la soglia (threshold) di punti per lo stadio corrente
        /// </summary>
        private string GetStageThreshold(int stage)
        {
            if (_growthConfig == null)
                return "?";
                
            switch (stage)
            {
                case (int)PlantStage.Seed:
                    return _growthConfig.pointsSeedToSprout.ToString();
                case (int)PlantStage.Sprout:
                    return _growthConfig.pointsSproutToMature.ToString();
                case (int)PlantStage.HarvestReady:
                    return "MAX";
                default:
                    return "?";
            }
        }
        
        /// <summary>
        /// Trova un TextMeshProUGUI nei figli cercando per testo contenuto o nome GameObject
        /// </summary>
        private GameObject FindObjectInChildren(string containsText)
        {
            // Cerca prima nel GameObject _page se disponibile
            GameObject searchRoot = _page != null ? _page : gameObject;
            
            // Cerca per nome GameObject
            Transform found = FindTransformRecursive(searchRoot.transform, containsText);
            if (found != null)
                return found.gameObject;
            
            return null;
        }
        
        private TextMeshProUGUI FindTextInChildren(string containsText)
        {
            // Cerca prima nel GameObject _page se disponibile
            GameObject searchRoot = _page != null ? _page : gameObject;
            
            // Prima cerca per nome GameObject (più affidabile)
            Transform foundByName = FindTransformRecursive(searchRoot.transform, containsText.Replace(" ", ""));
            if (foundByName != null)
            {
                TextMeshProUGUI textComp = foundByName.GetComponent<TextMeshProUGUI>();
                if (textComp != null)
                {
                    SporiumLogger.LogDebug(LogCategory.UI, $"Trovato TextMeshProUGUI per nome GameObject '{containsText}': {foundByName.name}");
                    return textComp;
                }
            }
            
            // Poi cerca per testo contenuto
            TextMeshProUGUI[] allTexts = searchRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in allTexts)
            {
                if (text != null && !string.IsNullOrEmpty(text.text) && text.text.Contains(containsText))
                {
                    SporiumLogger.LogDebug(LogCategory.UI, $"Trovato TextMeshProUGUI per '{containsText}': {text.name} (testo: '{text.text}')");
                    return text;
                }
            }
            
            // Ultimo tentativo: cerca per nome parziale (case-insensitive)
            foreach (var text in allTexts)
            {
                if (text != null && text.name.Contains(containsText, System.StringComparison.OrdinalIgnoreCase))
                {
                    SporiumLogger.LogDebug(LogCategory.UI, $"Trovato TextMeshProUGUI per nome parziale '{containsText}': {text.name}");
                    return text;
                }
            }
            
            // BUG FIX: Non loggare warning per GrowthLabel (è opzionale e può non esistere nella scena)
            if (containsText != "GrowthLabel")
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"Nessun TextMeshProUGUI trovato per '{containsText}'. Verifica che il GameObject abbia questo nome o che il testo contenga questa stringa.");
            }
            return null;
        }
        
        /// <summary>
        /// Trova una ProgressBar nei figli cercando per nome GameObject padre o ProgressBar stesso
        /// </summary>
        private ProgressBar FindProgressBarInChildren(string containsName)
        {
            // Cerca prima nel GameObject _page se disponibile
            GameObject searchRoot = _page != null ? _page : gameObject;
            
            SporiumLogger.LogDebug(LogCategory.UI, $"Cercando ProgressBar per '{containsName}' in '{searchRoot.name}'");
            
            // Prima cerca direttamente ProgressBar con nome contenente il testo cercato
            ProgressBar[] allProgressBars = searchRoot.GetComponentsInChildren<ProgressBar>(true);
            SporiumLogger.LogDebug(LogCategory.UI, $"Trovate {allProgressBars.Length} ProgressBar totali nella gerarchia");
            
            foreach (var progressBar in allProgressBars)
            {
                if (progressBar != null)
                {
                    SporiumLogger.LogDebug(LogCategory.UI, $"  - ProgressBar: '{progressBar.name}' (attivo: {progressBar.gameObject.activeSelf})");
                    if (progressBar.name.Contains(containsName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        SporiumLogger.LogDebug(LogCategory.UI, $"Trovato ProgressBar per '{containsName}': {progressBar.name}");
                        return progressBar;
                    }
                }
            }
            
            // Se non trovato direttamente, cerca ProgressBar figlio di GameObject padre con nome corrispondente
            // Esempio: cerca ProgressBar figlio di "Hydration" quando containsName = "Hydration Progress"
            string parentName = containsName.Replace(" Progress", "").Replace("Progress", "").Trim();
            SporiumLogger.LogDebug(LogCategory.UI, $"Cercando GameObject padre '{parentName}' per trovare ProgressBar figlio");
            
            // Cerca ricorsivamente il GameObject padre (non solo figli diretti)
            Transform parentTransform = FindTransformRecursive(searchRoot.transform, parentName);
            if (parentTransform != null)
            {
                SporiumLogger.LogDebug(LogCategory.UI, $"Trovato GameObject padre '{parentName}'");
                ProgressBar progressBar = parentTransform.GetComponentInChildren<ProgressBar>(true);
                if (progressBar != null)
                {
                    SporiumLogger.LogDebug(LogCategory.UI, $"Trovato ProgressBar figlio di '{parentName}': {progressBar.name}");
                    return progressBar;
                }
                else
                {
                    SporiumLogger.LogWarning(LogCategory.UI, $"GameObject '{parentName}' trovato ma nessuna ProgressBar figlio");
                }
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"GameObject padre '{parentName}' non trovato nella gerarchia");
            }
            
            // Cerca anche per nome parziale nel padre (es. "LightStress" per "Lighting Progress")
            if (containsName.Contains("Light", System.StringComparison.OrdinalIgnoreCase))
            {
                SporiumLogger.LogDebug(LogCategory.UI, "Cercando fallback per Light: 'LightStress' o 'Light'");
                Transform lightParent = FindTransformRecursive(searchRoot.transform, "LightStress");
                if (lightParent == null)
                {
                    // Cerca anche "Light" come fallback
                    lightParent = FindTransformRecursive(searchRoot.transform, "Light");
                }
                
                if (lightParent != null)
                {
                    SporiumLogger.LogDebug(LogCategory.UI, $"Trovato GameObject Light: '{lightParent.name}'");
                    ProgressBar progressBar = lightParent.GetComponentInChildren<ProgressBar>(true);
                    if (progressBar != null)
                    {
                        SporiumLogger.LogDebug(LogCategory.UI, $"Trovato ProgressBar figlio di 'LightStress/Light': {progressBar.name}");
                        return progressBar;
                    }
                }
            }
            
            SporiumLogger.LogWarning(LogCategory.UI, $"Nessuna ProgressBar trovata per '{containsName}'. Verifica che il GameObject abbia questo nome o che ci sia un ProgressBar figlio di '{parentName}'.");
            return null;
        }
        
        /// <summary>
        /// Ottiene il nome visualizzabile della pianta dal PlantCode
        /// </summary>
        private string GetPlantDisplayName(string plantCode)
        {
            if (string.IsNullOrEmpty(plantCode))
                return null;
            
            // Mappa PlantCode -> Nome pianta (dal GDD)
            switch (plantCode)
            {
                case "PLT-STD-001":
                    return "Ferric Fern";
                case "PLT-PURE-001":
                    return "Arctic Hask";
                case "PLT-EVIL-001":
                    return "Glasscap Fungus";
                // Aggiungi altri PlantCode quando disponibili
                default:
                    // Fallback: prova a ottenere il nome dal PlantData se disponibile
                    var plantDatabase = PlantDatabase.Instance;
                    if (plantDatabase != null)
                    {
                        var plantData = plantDatabase.GetPlantDataByCode(plantCode);
                        if (plantData != null && plantData.SeedItemConfig != null)
                        {
                            // Usa il nome dell'asset PlantData o del SeedItemConfig come fallback
                            return plantData.name.Replace("PLT-", "").Replace("-", " ");
                        }
                    }
                    return null;
            }
        }
        
        /// <summary>
        /// Cerca ricorsivamente un Transform per nome nella gerarchia completa
        /// </summary>
        private Transform FindTransformRecursive(Transform parent, string name)
        {
            // Cerca prima nei figli diretti
            Transform found = parent.Find(name);
            if (found != null)
                return found;
            
            // Cerca ricorsivamente in tutti i figli
            foreach (Transform child in parent)
            {
                found = FindTransformRecursive(child, name);
                if (found != null)
                    return found;
            }
            
            return null;
        }
        
        /// <summary>
        /// Setup tooltip per la progress bar Growth
        /// BUG FIX: Sposta EventTrigger sulla label Growth invece che sulla progress bar (che è nascosta)
        /// </summary>
        private void SetupGrowthTooltip()
        {
            // BUG FIX: Prova a trovare la label Growth se non è ancora assegnata
            if (_growthLabelText == null)
            {
                // Cerca prima "GrowthLabel" per nome
                _growthLabelText = FindTextInChildren("GrowthLabel");
                
                // Se non trovato, cerca per testo "Growth:"
                if (_growthLabelText == null)
                {
                    _growthLabelText = FindTextInChildren("Growth:");
                }
            }
            
            // BUG FIX: Usa la label Growth invece della progress bar per l'EventTrigger
            // perché la progress bar è nascosta e gli EventTrigger non funzionano su oggetti disattivati
            if (_growthLabelText == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "_growthLabelText è null! Tooltip Growth non può essere configurato. Verifica che esista un GameObject con TextMeshProUGUI contenente 'GrowthLabel'.");
                return;
            }
                
            // BUG FIX: Il tooltip panel deve essere assegnato manualmente in Unity, non creato in runtime
            if (_growthTooltipPanel == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "_growthTooltipPanel non assegnato! Assegna manualmente il GameObject del tooltip Growth nell'Inspector di PotDetailsWidget.");
                return;
            }
            
            // Verifica che il testo del tooltip sia assegnato
            if (_growthTooltipText == null && _growthTooltipPanel != null)
            {
                // Prova a trovare il testo automaticamente come child del panel
                _growthTooltipText = _growthTooltipPanel.GetComponentInChildren<TextMeshProUGUI>();
                if (_growthTooltipText == null)
                {
                    SporiumLogger.LogWarning(LogCategory.UI, "_growthTooltipText non trovato! Assicurati che il tooltip panel abbia un child TextMeshProUGUI o assegnalo manualmente nell'Inspector.");
                }
            }
            
            // Assicurati che il tooltip sia inizialmente nascosto
            if (_growthTooltipPanel != null)
            {
                _growthTooltipPanel.SetActive(false);
            }
            
            // BUG FIX: Aggiungi EventTrigger alla label Growth invece che alla progress bar
            // perché la progress bar è nascosta e gli EventTrigger non funzionano su oggetti disattivati
            EventTrigger trigger = _growthLabelText.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = _growthLabelText.gameObject.AddComponent<EventTrigger>();
            }
            
            // Rimuovi trigger esistenti per evitare duplicati
            trigger.triggers.Clear();
            
            // BUG FIX: Assicurati che la label sia attiva e abilitata per ricevere eventi
            if (!_growthLabelText.gameObject.activeSelf)
            {
                _growthLabelText.gameObject.SetActive(true);
                SporiumLogger.LogWarning(LogCategory.UI, "GrowthLabel era disattivata! Attivata per permettere tooltip.");
            }
            
            // BUG FIX: Assicurati che ci sia un GraphicRaycaster nel Canvas per permettere eventi
            Canvas parentCanvas = _growthLabelText.GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            {
                parentCanvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                SporiumLogger.LogWarning(LogCategory.UI, "Canvas non aveva GraphicRaycaster! Aggiunto per permettere tooltip.");
            }
            
            // PointerEnter - mostra tooltip (aggiorna sempre con dati più recenti)
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => {
                if (_currentSelectedPot != null && _currentSelectedPot.PotActions != null)
                {
                    // Ottieni stato aggiornato (non cached)
                    PotStateModel state = _currentSelectedPot.PotActions.GetCurrentState();
                    if (state != null)
                    {
                        // Aggiorna tooltip con dati più recenti prima di mostrarlo
                        UpdateGrowthTooltip(state);
                        if (_growthTooltipPanel != null)
                        {
                            _growthTooltipPanel.SetActive(true);
                        }
                    }
                }
            });
            trigger.triggers.Add(enterEntry);
            
            // PointerExit - nascondi tooltip
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => {
                if (_growthTooltipPanel != null)
                {
                    _growthTooltipPanel.SetActive(false);
                }
            });
            trigger.triggers.Add(exitEntry);
        }
        
        /// <summary>
        /// Aggiorna la label Growth con la CONDIZIONE della pianta (Rigogliosa, Sana, Stressata, Appassita, Critica)
        /// BUG FIX: Cambiato da stato crescita a condizione della pianta (lo stadio viene già mostrato da Stage)
        /// </summary>
        private void UpdateGrowthLabel(PotStateModel state)
        {
            // Auto-trova label se non assegnata
            if (_growthLabelText == null)
            {
                // BUG FIX: Cerca prima "GrowthLabel" per nome, poi "Growth:" nel testo
                _growthLabelText = FindTextInChildren("GrowthLabel");
                
                // Se non trovato, cerca per testo "Growth:"
                if (_growthLabelText == null)
                {
                    _growthLabelText = FindTextInChildren("Growth:");
                }
                
                // Se ancora non trovato, crea dinamicamente la label come fa PotHUDWidget
                if (_growthLabelText == null)
                {
                    // Trova il container Right o Progress per posizionare la label
                    GameObject searchRoot = _page != null ? _page : gameObject;
                    Transform progressTransform = FindTransformRecursive(searchRoot.transform, "Progress");
                    if (progressTransform == null)
                        progressTransform = FindTransformRecursive(searchRoot.transform, "Right");
                    
                    if (progressTransform != null)
                    {
                        GameObject growthLabelGO = new GameObject("GrowthLabel");
                        growthLabelGO.transform.SetParent(progressTransform, false);
                        
                        _growthLabelText = growthLabelGO.AddComponent<TextMeshProUGUI>();
                        _growthLabelText.color = Color.white;
                        _growthLabelText.fontSize = 14;
                        _growthLabelText.alignment = TextAlignmentOptions.Left;
                        _growthLabelText.text = "Condizione della Pianta:";
                        
                        RectTransform growthLabelRect = growthLabelGO.GetComponent<RectTransform>();
                        growthLabelRect.anchorMin = new Vector2(0, 1f);
                        growthLabelRect.anchorMax = new Vector2(0, 1f);
                        growthLabelRect.pivot = new Vector2(0, 1f);
                        growthLabelRect.anchoredPosition = new Vector2(0, 0);
                        growthLabelRect.sizeDelta = new Vector2(200, 20);
                        
                        SporiumLogger.LogInfo(LogCategory.UI, "GrowthLabel creata dinamicamente perché non trovata nella scena.");
                    }
                }
            }
            
            // BUG FIX: Se ancora null dopo la ricerca, non loggare warning (è opzionale)
            if (_growthLabelText != null)
            {
                // BUG FIX: Assicurati che il tooltip sia configurato quando la label viene trovata
                if (_growthTooltipPanel == null)
                {
                    SetupGrowthTooltip();
                }
                
                if (state == null || state.IsEmpty || !state.HasPlant)
                {
                    _growthLabelText.text = "Condizione della Pianta: Sana";
                    _growthLabelText.color = new Color(0.6f, 0.6f, 0.6f); // Grigio
                }
                else
                {
                    // BUG FIX: Mostra la CONDIZIONE invece dello stato di crescita
                    // Usa la stessa logica di UpdateConditionUI per calcolare la condizione
                    PlantData plantData = state.GetPlantData();
                    string conditionName;
                    
                    if (plantData != null && _phSystem != null && _potSystemConfig != null)
                    {
                        int currentDay = _dayCycleSystem?.CurrentDay ?? 1;
                        // BUG FIX: Usa lo stesso fallback di DayCycleController quando PreviousDayConditionScore è -1
                        int previousDayScore = state.PreviousDayConditionScore >= 0 ? state.PreviousDayConditionScore : state.ConditionScore;
                        
                        ConditionResult result = PlantConditionSystem.CalculateCondition(
                            state,
                            plantData,
                            _phSystem,
                            _potSystemConfig,
                            currentDay,
                            previousDayScore);
                        
                        bool isOverwatering = PlantConditionSystem.IsOverwatering(state, _potSystemConfig.MaxHydration);
                        conditionName = PlantConditionSystem.GetConditionName(result.Condition, isOverwatering);
                        
                        // Colore in base alla condizione (stesso sistema di UpdateConditionUI)
                        _growthLabelText.color = result.ConditionColor;
                    }
                    else
                    {
                        // Fallback: usa ConditionLabel direttamente
                        PlantCondition condition = (PlantCondition)state.ConditionLabel;
                        int maxHydration = _potSystemConfig?.MaxHydration ?? 5;
                        bool isOverwatering = PlantConditionSystem.IsOverwatering(state, maxHydration);
                        conditionName = PlantConditionSystem.GetConditionName(condition, isOverwatering);
                        
                        // Colore in base alla condizione
                        // NOTA: Stressata rimosso dalla logica, mantenuto solo l'enum per retrocompatibilità
                        // Se arriva Stressata (dati vecchi), viene mappato a Sana
                        _growthLabelText.color = condition switch
                        {
                            PlantCondition.Rigogliosa => new Color(0f, 0.5f, 0f),      // Verde scuro
                            PlantCondition.Sana => new Color(0f, 0.8f, 0f),          // Verde
                            PlantCondition.Stressata => new Color(0f, 0.8f, 0f),      // Verde (retrocompatibilità: Stressata → Sana)
                            PlantCondition.Appassita => new Color(1f, 0.5f, 0f),     // Arancione
                            PlantCondition.Critica => new Color(0.8f, 0f, 0f),        // Rosso
                            _ => Color.white
                        };
                    }
                    
                    _growthLabelText.text = $"Condizione della Pianta: {conditionName}";
                }
            }
        }
        
        /// <summary>
        /// Aggiorna tooltip Growth con dati attuali
        /// </summary>
        private void UpdateGrowthTooltip(PotStateModel state)
        {
            if (_growthTooltipText == null || state == null)
                return;
                
            _growthTooltipText.text = BuildGrowthTooltip(state);
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
            
            // BUG 3 FIX: Light OK basato su stress percentage (0% = OK) invece di LightExposure
            // Calcola stress percentage (come nella HUD Light Stress)
            int consecutiveDays = state.GetConsecutiveLedDays();
            int maxDaysForFullStress = _potSystemConfig != null ? _potSystemConfig.MaxDaysForFullStress : 5;
            float stressPercentage = Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f;
            
            // BUG FIX: Light è OK se stress è tra 0% e 100% (esclusi gli estremi)
            // NOT OK solo quando stress è esattamente 0% (nessuna luce) o 100% (troppa luce)
            // Quando lo stress è nel range, è OK anche se le luci sono spente (seguendo la logica del fix)
            bool lightOk = stressPercentage > 0f && stressPercentage < 100f;
            
            
            bool fertilizerOk = stageReq.IsFertilizerInRange(state.FertilizerLevel);
            
            // BUG FIX: Mostra la CONDIZIONE invece dello stato di crescita
            // Calcola la condizione usando la stessa logica di UpdateConditionUI
            string conditionName;
            if (_phSystem != null)
            {
                int currentDay = _dayCycleSystem?.CurrentDay ?? 1;
                // BUG FIX: Usa lo stesso fallback di DayCycleController quando PreviousDayConditionScore è -1
                int previousDayScore = state.PreviousDayConditionScore >= 0 ? state.PreviousDayConditionScore : state.ConditionScore;
                
                ConditionResult result = PlantConditionSystem.CalculateCondition(
                    state,
                    plantData,
                    _phSystem,
                    _potSystemConfig,
                    currentDay,
                    previousDayScore);
                
                bool isOverwatering = PlantConditionSystem.IsOverwatering(state, _potSystemConfig.MaxHydration);
                conditionName = PlantConditionSystem.GetConditionName(result.Condition, isOverwatering);
            }
            else
            {
                // Fallback: usa ConditionLabel direttamente
                PlantCondition condition = (PlantCondition)state.ConditionLabel;
                bool isOverwatering = PlantConditionSystem.IsOverwatering(state, maxHydration);
                conditionName = PlantConditionSystem.GetConditionName(condition, isOverwatering);
            }
            
            sb.AppendLine($"<b>Condizione della Pianta: {conditionName}</b>");
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
            // BUG 3 FIX: Light OK basato su stress percentage (0% = OK) invece di LightExposure
            string lightStatus = lightOk ? "<color=#00FF00>OK</color>" : "<color=#FF0000>NON OK</color>";
            sb.AppendLine($"• <color=#FFD700>Luce</color>: {lightStatus}");
            // BUG FIX: Mostra sempre il range ideale (come nella HUD Light Stress), non solo quando NON OK
            sb.AppendLine($"  Range ideale: <color=#00FF00>{stageReq.lightMin}%-{stageReq.lightMed}%-{stageReq.lightMax}%</color>");
            
            // BLK-02.08: Aggiungi informazione LED compatibile con famiglia
            if (plantData != null)
            {
                LedCompatibility compatible = LedCompatibilityHelper.GetCompatibleLedTypes(plantData.Family);
                string compatibleDisplay = LedCompatibilityHelper.GetCompatibleLedDisplay(compatible);
                sb.AppendLine($"  LED compatibile con famiglia: <color=#00FF00>{compatibleDisplay}</color>");
                
                // Verifica se LED attuale è incompatibile con famiglia
                if (state.LedSystemState != LedSystemState.Off)
                {
                    bool isLedCompatibleWithFamily = LedCompatibilityHelper.IsLedCompatible(state.LedSystemState, compatible);
                    if (!isLedCompatibleWithFamily)
                    {
                        string currentLedName = state.LedSystemState == LedSystemState.Blue ? "Blue" : "Red";
                        sb.AppendLine($"  <color=#FF0000>⚠️ LED {currentLedName} attivo è INCOMPATIBILE con famiglia {plantData.Family}!</color>");
                        sb.AppendLine($"  <color=#FF0000>   Malus condizione: -5 per ogni giorno che è acceso</color>");
                    }
                }
            }
            
            if (!lightOk)
            {
                // BUG FIX: Mostra lo stress percentage (come nella HUD) invece di LightExposure
                sb.AppendLine($"  Attuale: {stressPercentage:F0}%");
                // BUG FIX: Mostra LED richiesto solo quando lo stress è fuori range (0% o 100%)
                // Quando lo stress è nel range, non mostrare "LED richiesto: NON OK" anche se il LED è spento
                if (stressPercentage == 0f || stressPercentage >= 100f)
                {
                    string ledRequired = stageReq.GetRequiredLed()?.ToString() ?? "Nessuno";
                    bool ledRequirementMet = stageReq.IsLedRequirementMet(state.LedSystemState);
                    if (ledRequired != "Nessuno" && !ledRequirementMet)
                    {
                        sb.AppendLine($"  LED richiesto: {ledRequired} (<color=#FF0000>NON OK</color>)");
                    }
                }
            }
            else
            {
                // BUG 3 FIX: Mostra lo stress percentage anche quando è OK per coerenza con HUD
                sb.AppendLine($"  Attuale: <color=#00FF00>{stressPercentage:F0}%</color>");
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
        
        /// <summary>
        /// Aggiorna UI Condizione pianta (label, barra, forecast)
        /// </summary>
        private void UpdateConditionUI(PotStateModel state)
        {
            if (state == null || !state.HasPlant)
            {
                // Nascondi UI condizione se vaso vuoto
                if (_conditionLabelText != null)
                    _conditionLabelText.text = "";
                if (_conditionBar != null)
                    _conditionBar.gameObject.SetActive(false);
                if (_conditionForecastText != null)
                    _conditionForecastText.text = "";
                return;
            }
            
            // Auto-trova riferimenti se non assegnati
            if (_conditionLabelText == null)
                _conditionLabelText = FindTextInChildren("Condition");
            if (_conditionForecastText == null)
                _conditionForecastText = FindTextInChildren("ConditionForecast");
            if (_conditionBar == null)
            {
                var barTransform = FindTransformRecursive(_page.transform, "ConditionBar");
                if (barTransform != null)
                    _conditionBar = barTransform.GetComponent<ProgressBar>();
            }
            
            // Calcola condizione
            PlantData plantData = state.GetPlantData();
            
            // BUG FIX: Se _phSystem è null, prova a recuperarlo di nuovo dal ServiceContainer
            // (potrebbe essere stato registrato dopo l'inizializzazione di PotDetailsWidget)
            if (_phSystem == null)
            {
                _phSystem = ServiceContainer.Instance?.Get<PhSystem>(suppressWarning: true);
            }
            
            if (plantData == null || _phSystem == null || _potSystemConfig == null)
            {
                // Fallback: mostra score base se non possiamo calcolare
                if (_conditionLabelText != null)
                    _conditionLabelText.text = $"Condizione: Sana ({state.ConditionScore}/100)";
                return;
            }
            
            int currentDay = _dayCycleSystem?.CurrentDay ?? 1;
            // BUG FIX: Usa lo stesso fallback di DayCycleController quando PreviousDayConditionScore è -1
            int previousDayScore = state.PreviousDayConditionScore >= 0 ? state.PreviousDayConditionScore : state.ConditionScore;
            
            ConditionResult result = PlantConditionSystem.CalculateCondition(
                state, 
                plantData, 
                _phSystem, 
                _potSystemConfig, 
                currentDay, 
                previousDayScore);
            
            // Aggiorna label condizione con forecast
            if (_conditionLabelText != null)
            {
                _conditionLabelText.richText = true;
                bool isOverwatering = PlantConditionSystem.IsOverwatering(state, _potSystemConfig.MaxHydration);
                string conditionName = PlantConditionSystem.GetConditionName(result.Condition, isOverwatering);
                string forecastSymbol = PlantConditionSystem.GetForecastSymbol(result.Forecast);
                string forecastColor = result.Forecast switch
                {
                    ForecastDirection.Up => "#00FF00",
                    ForecastDirection.Down => "#FF0000",
                    _ => "#CCCCCC"
                };
                _conditionLabelText.text = $"<color=#CCCCCC>Condizione:</color> <color=#FFFF00>{conditionName}</color> <color=#CCCCCC>({result.Score}/100)</color> <color={forecastColor}>{forecastSymbol}</color>";
            }
            
            // Aggiorna barra condizione
            if (_conditionBar != null)
            {
                _conditionBar.Value = result.Score / 100f;
                // Colore barra in base alla condizione
                var fillImage = _conditionBar.GetComponentInChildren<Image>();
                if (fillImage != null)
                {
                    fillImage.color = result.ConditionColor;
                }
                _conditionBar.gameObject.SetActive(true);
            }
            
            // Setup tooltip condizione se non fatto
            if (_conditionTooltipPanel == null && _progressBar != null)
            {
                SetupConditionTooltip();
            }
        }
        
        /// <summary>
        /// Setup tooltip per la condizione
        /// </summary>
        private void SetupConditionTooltip()
        {
            if (_conditionBar == null)
                return;
                
            // Crea tooltip panel se non esiste
            if (_conditionTooltipPanel == null)
            {
                // Trova o crea Canvas con sorting order alto per tooltip
                Canvas tooltipCanvas = GetOrCreateTooltipCanvas();
                
                GameObject tooltipGO = new GameObject("ConditionTooltipPanel");
                tooltipGO.transform.SetParent(tooltipCanvas.transform, false);
                
                RectTransform tooltipRect = tooltipGO.AddComponent<RectTransform>();
                tooltipRect.anchorMin = new Vector2(0, 1);
                tooltipRect.anchorMax = new Vector2(0, 1);
                tooltipRect.pivot = new Vector2(0, 1);
                
                // Posiziona il tooltip in base alla posizione della barra condizione
                Vector3 worldPos = _conditionBar.transform.position;
                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(tooltipCanvas.worldCamera ?? Camera.main, worldPos);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    tooltipCanvas.transform as RectTransform, screenPos, tooltipCanvas.worldCamera ?? Camera.main, out Vector2 localPos);
                tooltipRect.anchoredPosition = new Vector2(localPos.x, localPos.y - 80);
                
                // Tooltip più grande
                tooltipRect.sizeDelta = new Vector2(450, 250);
                
                Image tooltipBg = tooltipGO.AddComponent<Image>();
                // Verde scuro solido (non trasparente)
                tooltipBg.color = new Color(0f, 0.3f, 0.15f, 1f);
                
                _conditionTooltipPanel = tooltipGO;
                _conditionTooltipPanel.SetActive(false);
            }
            
            // Crea testo tooltip se non esiste
            if (_conditionTooltipText == null && _conditionTooltipPanel != null)
            {
                GameObject textGO = new GameObject("ConditionTooltipText");
                textGO.transform.SetParent(_conditionTooltipPanel.transform, false);
                
                _conditionTooltipText = textGO.AddComponent<TextMeshProUGUI>();
                _conditionTooltipText.color = Color.white;
                _conditionTooltipText.fontSize = 16; // Testo più grande (era 12)
                _conditionTooltipText.alignment = TextAlignmentOptions.Left;
                _conditionTooltipText.richText = true;
                
                RectTransform textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(12, 12);
                textRect.offsetMax = new Vector2(-12, -12);
            }
            
            // Aggiungi EventTrigger alla barra condizione per hover
            EventTrigger trigger = _conditionBar.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = _conditionBar.gameObject.AddComponent<EventTrigger>();
            
            // PointerEnter - mostra tooltip
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => {
                if (_currentSelectedPot != null && _currentSelectedPot.PotActions != null)
                {
                    PotStateModel state = _currentSelectedPot.PotActions.GetCurrentState();
                    if (state != null && state.HasPlant)
                    {
                        PlantData plantData = state.GetPlantData();
                        if (plantData != null && _phSystem != null && _potSystemConfig != null)
                        {
                            ConditionResult result = PlantConditionSystem.CalculateCondition(
                                state, plantData, _phSystem, _potSystemConfig, 
                                _dayCycleSystem?.CurrentDay ?? 1, state.PreviousDayConditionScore);
                            if (_conditionTooltipText != null)
                                _conditionTooltipText.text = BuildConditionTooltip(result);
                            if (_conditionTooltipPanel != null)
                                _conditionTooltipPanel.SetActive(true);
                        }
                    }
                }
            });
            trigger.triggers.Add(enterEntry);
            
            // PointerExit - nascondi tooltip
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => {
                if (_conditionTooltipPanel != null)
                    _conditionTooltipPanel.SetActive(false);
            });
            trigger.triggers.Add(exitEntry);
        }
        
        /// <summary>
        /// Costruisce il tooltip di condizione con contributi
        /// </summary>
        private string BuildConditionTooltip(ConditionResult result)
        {
            var sb = new System.Text.StringBuilder();
            
            string conditionName = PlantConditionSystem.GetConditionName(result.Condition, false);
            string forecastSymbol = PlantConditionSystem.GetForecastSymbol(result.Forecast);
            string forecastText = result.Forecast switch
            {
                ForecastDirection.Up => "Tendenza positiva",
                ForecastDirection.Down => "Tendenza negativa",
                _ => "Stabile"
            };
            
            sb.AppendLine($"<b>Condizione: {conditionName} ({result.Score}/100) {forecastSymbol}</b>");
            sb.AppendLine();
            
            // Contributi positivi
            var positiveContribs = System.Array.FindAll(result.Contributors, c => c.IsPositive);
            if (positiveContribs.Length > 0)
            {
                sb.AppendLine("<color=#00FF00>Contributi positivi:</color>");
                foreach (var contrib in positiveContribs)
                {
                    sb.AppendLine($"• {contrib.Source}: +{contrib.Value}");
                }
                sb.AppendLine();
            }
            
            // Contributi negativi
            var negativeContribs = System.Array.FindAll(result.Contributors, c => !c.IsPositive);
            if (negativeContribs.Length > 0)
            {
                sb.AppendLine("<color=#FF0000>Contributi negativi:</color>");
                foreach (var contrib in negativeContribs)
                {
                    sb.AppendLine($"• {contrib.Source}: {contrib.Value}");
                }
                sb.AppendLine();
            }
            
            sb.AppendLine($"<color=#FFFF00>Forecast:</color> {forecastText}. {(result.ScoreDelta != 0 ? $"Δ {result.ScoreDelta:+0;-0}" : "")}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Trova o crea un Canvas con sorting order alto per i tooltip (sopra tutto)
        /// </summary>
        private Canvas GetOrCreateTooltipCanvas()
        {
            // Cerca un Canvas esistente con tag "TooltipCanvas" o nome specifico
            GameObject existingCanvas = GameObject.Find("TooltipCanvas");
            if (existingCanvas != null)
            {
                Canvas existingCanvasComponent = existingCanvas.GetComponent<Canvas>();
                if (existingCanvasComponent != null)
                    return existingCanvasComponent;
            }
            
            // Crea nuovo Canvas per tooltip
            GameObject canvasGO = new GameObject("TooltipCanvas");
            Canvas tooltipCanvas = canvasGO.AddComponent<Canvas>();
            tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tooltipCanvas.sortingOrder = 9999; // Sorting order molto alto per essere sopra tutto
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            return tooltipCanvas;
        }
    }
}