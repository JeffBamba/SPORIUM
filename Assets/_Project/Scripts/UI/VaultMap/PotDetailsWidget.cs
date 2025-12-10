using System.Linq;
using _Project.Sporae.Core;
// GDD AZ-11: Watering namespace rimosso (minigioco deprecato)
// using _Project.Watering;
using Sporae.Dome.PotSystem.Growth;
using Sporae.Dome.PotSystem.Condition;
using Sporae.Dome.PotSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
        [SerializeField] private Button _uprootButton;

        [SerializeField] private TextMeshProUGUI _idLabel;
        [SerializeField] private TextMeshProUGUI _stageLabel;
        [SerializeField] private TextMeshProUGUI _plantDescriptionLabel;
        [SerializeField] private ProgressBar _progressBar;
        [SerializeField] private Image _stageImage;
        [SerializeField] private TextMeshProUGUI _growthLabelText;  // Label per stato crescita (IN CRESCITA, Stabile, etc.)
        
        [Header("Plant Stats UI")]
        [SerializeField] private TextMeshProUGUI _hydrationStressText;
        [SerializeField] private TextMeshProUGUI _lightStressText;
        [SerializeField] private TextMeshProUGUI _fertilizerText;  // BLK-03.01-T1
        [SerializeField] private TextMeshProUGUI _growthPointsText;  // BLK-03.01-T2
        [SerializeField] private TextMeshProUGUI _optimalDaysText;  // BLK-03.01-T2
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
        [SerializeField] private GameObject _growthTooltipPanel;
        [SerializeField] private TextMeshProUGUI _growthTooltipText;

        [SerializeField] private GameObject _page;
        
        // GDD AZ-11: WateringMinigame rimosso (sistema toggle persistente)
        // [SerializeField] private WateringMinigame _wateringMinigame; // DEPRECATO
        
        [Header("Seed Selector")]
        [SerializeField] private UISeedSelector _seedSelector;
        
        [Header("Fertilizer Selector (BLK-03.01-T1)")]
        [SerializeField] private UIFertilizerSelector _fertilizerSelector;
        
        private PotSlot _currentSelectedPot;
        private PlantGrowthConfig _growthConfig;
        private GameManager _gameManager;
        private DayCycleSystem _dayCycleSystem;
        private PhSystem _phSystem;
        private PotSystemConfig _potSystemConfig;
        
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
        }

        private void Unsubscribes()
        {
            PotSlot.OnPotSelected -= OnPotSelected;
            PotEvents.OnPotStateChanged -= OnPotStateChanged;
            PotEvents.OnPlantGrew -= OnPlantGrew;
            PotEvents.OnPlantStageChanged -= OnPlantStageChanged;
        }
        
        private void Initialize()
        {
            _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
            _dayCycleSystem.OnDayChanged += HandleDayChanged;
            
            _gameManager = FindObjectOfType<GameManager>();
            
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
                Debug.LogError("[PotDetailsWidget] ⚠️ _plantButton non assegnato! Collega il riferimento nella scena Unity.");
            
            if (_wateringButton != null)
                _wateringButton.onClick.AddListener(() => OnActionButtonClicked(PotEvents.PotActionType.Water));
            else
                Debug.LogError("[PotDetailsWidget] ⚠️ _wateringButton non assegnato! Collega il riferimento nella scena Unity.");
            
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
            
            if (_uprootButton != null)
                _uprootButton.onClick.AddListener(() => OnActionButtonClicked(PotEvents.PotActionType.Uproot));
            else
                Debug.LogError("[PotDetailsWidget] ⚠️ _uprootButton non assegnato! Collega il riferimento nella scena Unity.");
            
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
                    Debug.LogError("[PotDetailsWidget] ⚠️ UISeedSelector non trovato nella scena! " +
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
            Debug.Log($"[PotDetailsWidget] 🟢 OnSeedSelected chiamato con seedTypeId: {seedTypeId}");
            Debug.Log($"[PotDetailsWidget] 🟢 _currentSelectedPot: {_currentSelectedPot?.PotId ?? "NULL"}");
            
            if (_currentSelectedPot == null)
            {
                Debug.LogError("[PotDetailsWidget] ⚠️ _currentSelectedPot è NULL quando seme selezionato!");
                return;
            }
            
            if (_currentSelectedPot.PotActions == null)
            {
                Debug.LogError("[PotDetailsWidget] ⚠️ PotActions è NULL quando seme selezionato!");
                return;
            }
            
            Debug.Log($"[PotDetailsWidget] 🟢 Piantando seme {seedTypeId} nel vaso {_currentSelectedPot.PotId}");
            
            // Piantare il seme selezionato
            bool success = _currentSelectedPot.PotActions.DoPlant(seedTypeId);
            
            if (success)
            {
                Debug.Log($"[PotDetailsWidget] ✅ Seme {seedTypeId} piantato con successo!");
                // Aggiorna l'UI
                UpdateActionButtons(_currentSelectedPot);
                
                var growthController = _currentSelectedPot.GetComponent<PotGrowthController>();
                if (growthController != null)
                    UpdateStageAndProgressUI(_currentSelectedPot);
            }
            else
            {
                Debug.LogError($"[PotDetailsWidget] ❌ Fallito piantare seme {seedTypeId}! Verifica i log di PotActions per dettagli.");
            }
        }
        
        /// <summary>
        /// Gestisce l'annullamento della selezione seme
        /// </summary>
        private void OnSeedSelectionCancelled()
        {
            Debug.Log("[PotDetailsWidget] Selezione seme annullata");
            // Nessuna azione necessaria
        }
        
        /// <summary>
        /// Apre il selettore semi per il vaso specificato
        /// </summary>
        private void OpenSeedSelector(PotSlot targetPot)
        {
            Debug.Log($"[PotDetailsWidget] 🔵 OpenSeedSelector chiamato per vaso {targetPot?.PotId ?? "NULL"}");
            
            // Assicurati che il selettore sia inizializzato
            if (_seedSelector == null)
            {
                Debug.Log("[PotDetailsWidget] 🔵 Inizializzazione seed selector...");
                InitializeSeedSelector();
            }
            
            if (_seedSelector == null)
            {
                Debug.LogError("[PotDetailsWidget] ❌ UISeedSelector non disponibile dopo inizializzazione!");
                return;
            }
            
            // Rassicurati che gli eventi siano sempre sottoscritti (in caso di ricreazione)
            _seedSelector.OnSeedSelected -= OnSeedSelected; // Rimuovi prima per evitare duplicati
            _seedSelector.OnSeedSelected += OnSeedSelected;
            _seedSelector.OnCancelled -= OnSeedSelectionCancelled;
            _seedSelector.OnCancelled += OnSeedSelectionCancelled;
            
            Debug.Log($"[PotDetailsWidget] 🔵 Eventi sottoscritti correttamente");
            Debug.Log($"[PotDetailsWidget] 🔵 Apertura selettore semi per vaso {targetPot?.PotId}");
            
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
                    Debug.LogWarning("[PotDetailsWidget] ⚠️ UIFertilizerSelector non trovato nella scena. Creazione automatica...");
                    GameObject fertilizerSelectorGO = new GameObject("UIFertilizerSelector");
                    _fertilizerSelector = fertilizerSelectorGO.AddComponent<UIFertilizerSelector>();
                    Debug.Log("[PotDetailsWidget] ✅ UIFertilizerSelector creato automaticamente!");
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
            Debug.Log($"[PotDetailsWidget] 🌿 OpenFertilizerSelector chiamato per vaso {targetPot?.PotId ?? "NULL"}");
            
            // Assicurati che il selettore sia inizializzato
            if (_fertilizerSelector == null)
            {
                Debug.Log("[PotDetailsWidget] 🌿 Inizializzazione fertilizer selector...");
                InitializeFertilizerSelector();
            }
            
            if (_fertilizerSelector == null)
            {
                Debug.LogError("[PotDetailsWidget] ❌ UIFertilizerSelector non disponibile dopo inizializzazione!");
                return;
            }
            
            // Rassicurati che gli eventi siano sempre sottoscritti (in caso di ricreazione)
            _fertilizerSelector.OnFertilizerSelected -= OnFertilizerSelected; // Rimuovi prima per evitare duplicati
            _fertilizerSelector.OnFertilizerSelected += OnFertilizerSelected;
            _fertilizerSelector.OnCancelled -= OnFertilizerSelectionCancelled;
            _fertilizerSelector.OnCancelled += OnFertilizerSelectionCancelled;
            
            Debug.Log($"[PotDetailsWidget] 🌿 Eventi sottoscritti correttamente");
            Debug.Log($"[PotDetailsWidget] 🌿 Apertura selettore fertilizzanti per vaso {targetPot?.PotId}");
            
            // Salva il vaso corrente prima di aprire il selettore
            _currentSelectedPot = targetPot;
            
            _fertilizerSelector.Show(targetPot);
        }
        
        /// <summary>
        /// Gestisce la selezione di un fertilizzante
        /// </summary>
        private void OnFertilizerSelected(string fertilizerTypeId)
        {
            Debug.Log($"[PotDetailsWidget] 🌿 OnFertilizerSelected chiamato con fertilizerTypeId: {fertilizerTypeId}");
            Debug.Log($"[PotDetailsWidget] 🌿 _currentSelectedPot: {_currentSelectedPot?.PotId ?? "NULL"}");
            
            if (_currentSelectedPot == null)
            {
                Debug.LogError("[PotDetailsWidget] ⚠️ _currentSelectedPot è NULL quando fertilizzante selezionato!");
                return;
            }
            
            if (_currentSelectedPot.PotActions == null)
            {
                Debug.LogError("[PotDetailsWidget] ⚠️ PotActions è NULL quando fertilizzante selezionato!");
                return;
            }
            
            Debug.Log($"[PotDetailsWidget] 🌿 Applicando fertilizzante {fertilizerTypeId} al vaso {_currentSelectedPot.PotId}");
            
            // Applica il fertilizzante selezionato
            bool success = _currentSelectedPot.PotActions.DoFertilize(fertilizerTypeId);
            
            if (success)
            {
                Debug.Log($"[PotDetailsWidget] ✅ Fertilizzante applicato con successo!");
                // Aggiorna l'UI
                UpdateActionButtons(_currentSelectedPot);
                
                var growthController = _currentSelectedPot.GetComponent<PotGrowthController>();
                if (growthController != null)
                    UpdateStageAndProgressUI(_currentSelectedPot);
            }
            else
            {
                Debug.LogError($"[PotDetailsWidget] ❌ Fallito applicare fertilizzante {fertilizerTypeId}! Verifica i log di PotActions per dettagli.");
            }
        }
        
        /// <summary>
        /// Gestisce l'annullamento della selezione fertilizzante
        /// </summary>
        private void OnFertilizerSelectionCancelled()
        {
            Debug.Log("[PotDetailsWidget] Selezione fertilizzante annullata");
            // Nessuna azione necessaria
        }
        
        private void LoadGrowthConfig()
        {
            _growthConfig = Resources.Load<PlantGrowthConfig>("Configs/PlantGrowthConfig_Default");
            if (_growthConfig != null)
            {
                Debug.Log($"[PotDetailsWidget] ✅ Config caricata: pointsSeedToSprout={_growthConfig.pointsSeedToSprout}, pointsSproutToMature={_growthConfig.pointsSproutToMature}");
            }
            else
                return;
            
            Debug.LogWarning($"[{nameof(PotDetailsWidget)}] PlantGrowthConfig non trovato in Resources/Configs/. Usando valori di default.");
            _growthConfig = ScriptableObject.CreateInstance<PlantGrowthConfig>();
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
            Debug.Log($"[BLK-01.03B] Vaso {pot.PotId} selezionato. Aggiornamento UI...");
            Debug.Log($"[BLK-01.03B] PotActions presente: {pot.PotActions != null}");
            // Debug.Log($"[BLK-01.03B] Player in range: {pot.InRange}");
        
            // Salva il vaso selezionato corrente
            
            _currentSelectedPot = pot;
            _page.SetActive(true);
        
            // BLK-01.03B: Aggiorna tutti gli elementi UI del nuovo sistema
            UpdateStageAndProgressUI(pot);
        
            // Aggiorna i pulsanti di azione
            UpdateActionButtons(pot);
        
            Debug.Log($"[BLK-01.03B] UI aggiornata per vaso {pot.PotId}");
        }
        
        /// <summary>
        /// Gestisce il click su un pulsante di azione
        /// </summary>
        private void OnActionButtonClicked(PotEvents.PotActionType actionType)
        {
            Debug.Log($"[PotHUDWidget] Click su pulsante {actionType} intercettato!");
        
            // Trova il vaso selezionato
            PotSlot selectedPot = FindSelectedPot();
            if (selectedPot == null || selectedPot.PotActions == null)
            {
                Debug.LogWarning("[PotHUDWidget] Nessun vaso selezionato o PotActions mancante");
                return;
            }
        
            Debug.Log($"[PotHUDWidget] Eseguendo azione {actionType} su vaso {selectedPot.PotId}");
        
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
                
                case PotEvents.PotActionType.Uproot:
                    success = selectedPot.PotActions.DoUproot();
                    break;
            }
        
            if (success)
            {
                Debug.Log($"[PotHUDWidget] Azione {actionType} eseguita con successo!");
                // Aggiorna l'UI
                UpdateActionButtons(selectedPot);

                var growthController = selectedPot.GetComponent<PotGrowthController>();
                if (growthController != null)
                    UpdateStageAndProgressUI(selectedPot);
            }
            else
            {
                Debug.LogWarning($"[PotHUDWidget] Azione {actionType} fallita!");
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
                Debug.LogWarning("[PotDetailsWidget] Nessun vaso selezionato");
                return;
            }
            
            // Toggle LED Blu: se è già Blue, spegni. Altrimenti, accendi Blue
            bool success = selectedPot.PotActions.DoLight(LedType.Blue);
            
            if (success)
            {
                LedSystemState newState = selectedPot.PotActions.GetLedSystemState();
                Debug.Log($"[PotDetailsWidget] LED Blue: {(newState == LedSystemState.Blue ? "ON" : "OFF")}");
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
                Debug.LogWarning("[PotDetailsWidget] Nessun vaso selezionato");
                return;
            }
            
            // Toggle LED Rosso: se è già Red, spegni. Altrimenti, accendi Red
            bool success = selectedPot.PotActions.DoLight(LedType.Red);
            
            if (success)
            {
                LedSystemState newState = selectedPot.PotActions.GetLedSystemState();
                Debug.Log($"[PotDetailsWidget] LED Red: {(newState == LedSystemState.Red ? "ON" : "OFF")}");
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
                Debug.LogWarning("[PotDetailsWidget] Nessun vaso selezionato");
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
            if (_fertilizeButton != null)
                UpdateButtonState(_fertilizeButton, pot.PotActions.CanFertilize(), "Fertilizzare");  // BLK-03.01-T1
            UpdateButtonState(_uprootButton, pot.PotActions.CanUproot(), "Uproot");
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
        
            Debug.Log($"[BLK-01.03B] Pianta cresciuta su {potId}: {oldPoints} → {newPoints} punti. Aggiornamento progress bar...");
            UpdateStageAndProgressUI(_currentSelectedPot);
        }
        
        /// <summary>
        /// BLK-01.03B: Gestisce l'evento OnPlantStageChanged
        /// </summary>
        private void OnPlantStageChanged(string potId, PlantStage stage)
        {
            if (!_currentSelectedPot || _currentSelectedPot.PotId != potId)
                return;
        
            Debug.Log($"[BLK-01.03B] Stadio cambiato su {potId}: {stage}. Aggiornamento UI...");
            UpdateStageAndProgressUI(_currentSelectedPot);
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
                            Debug.Log($"[PotDetailsWidget] ✅ Descrizione pianta aggiornata per {state.PlantCode}: {plantData.Description}");
                        }
                        else
                        {
                            _plantDescriptionLabel.text = "<color=#888888>Nessuna descrizione disponibile</color>";
                            Debug.LogWarning($"[PotDetailsWidget] ⚠️ Descrizione non trovata per pianta {state.PlantCode} (PlantData null o Description vuota)");
                        }
                    }
                    else
                    {
                        _plantDescriptionLabel.text = "<color=#FF0000>Errore database piante</color>";
                        Debug.LogWarning("[PotDetailsWidget] ⚠️ PlantDatabase.Instance è null!");
                    }
                }
                else
                {
                    _plantDescriptionLabel.text = "<color=#888888>Nessuna pianta selezionata</color>";
                    Debug.LogWarning($"[PotDetailsWidget] ⚠️ PlantCode vuoto per vaso {_currentSelectedPot?.PotId ?? "Unknown"}");
                }
            }
            else
            {
                Debug.LogWarning("[PotDetailsWidget] ⚠️ _plantDescriptionLabel non trovato! Verifica che esista un GameObject 'PlantDescription' con TextMeshProUGUI nella gerarchia UI_PotDetails/Panel/Left/");
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
            int maxHydration = _currentSelectedPot?.PotActions?.GetMaxHydration() ?? 5; // 5 step = 20% ciascuno
            float hydrationPercentage = maxHydration > 0 ? (float)state.Hydration / maxHydration * 100f : 0f;
            
            // GDD AZ-11: Mostra anche lo stato del sistema irrigazione
            string wateringStatus = state.WateringSystemOn ? " <color=#00FF00>[ON]</color>" : " <color=#FF0000>[OFF]</color>";
            
            if (_hydrationStressText != null)
            {
                // Assicurati che richText sia abilitato
                _hydrationStressText.richText = true;
                
                string hydrationText = $"<color=#CCCCCC>Hydration:</color> <color=#FFFF00>{hydrationPercentage:F0}%</color>{wateringStatus}";
                
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
                                // Mostra il range ideale (min-med-max) per lo stadio corrente
                                hydrationText += $" <color=#CCCCCC>(Range:</color> <color=#00FF00>{stageReq.hydrationMin}%-{stageReq.hydrationMed}%-{stageReq.hydrationMax}%</color><color=#CCCCCC>)</color>";
                            }
                        }
                    }
                }
                
                _hydrationStressText.text = hydrationText;
                Debug.Log($"[PotDetailsWidget] ✅ Aggiornato Hydration: {hydrationPercentage:F0}% (Hydration={state.Hydration}/{maxHydration}, Sistema={state.WateringSystemOn})");
            }
            else
            {
                Debug.LogWarning("[PotDetailsWidget] _hydrationStressText non trovato! Collega il riferimento nella scena Unity.");
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
                // 1 giorno = 25%
                // 2 giorni = 50%
                // 3 giorni = 75%
                // 4+ giorni = 100% (zona rossa, malus attivo)
                const int maxDaysForFullStress = 4;
                stressPercentage = Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f;
                
                // Nota: Quando LED è spento, i giorni consecutivi decrescono gradualmente (25% al giorno)
                // seguendo la stessa logica della crescita ma al contrario
                
                // Colore in base allo stress
                string stressColor = stressPercentage switch
                {
                    >= 100f => "#FF0000",  // Rosso per stress massimo (4+ giorni)
                    >= 75f => "#FF6600",   // Arancione scuro per stress alto (3 giorni)
                    >= 50f => "#FFAA00",   // Arancione per stress medio (2 giorni)
                    >= 25f => "#FFFF00",   // Giallo per stress basso (1 giorno)
                    _ => "#00FF00"         // Verde per nessuno stress (0 giorni)
                };
                
                _lightStressText.text = $"<color=#CCCCCC>Light Stress:</color> <color={stressColor}>{stressPercentage:F0}%</color>";
                Debug.Log($"[PotDetailsWidget] Aggiornato Light Stress: {stressPercentage:F0}% (LED: {state.LedSystemState}, Giorni consecutivi: {state.GetConsecutiveLedDays()})");
            }
            else
            {
                Debug.LogWarning("[PotDetailsWidget] _lightStressText non trovato! Collega il riferimento nella scena Unity.");
            }
            
            // Nascondi la progress bar come richiesto (il player deve vedere la percentuale invece)
            if (_lightProgressBar != null)
            {
                _lightProgressBar.gameObject.SetActive(false);
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
                    string fertilizerText = $"<color=#CCCCCC>Fertilizzante:</color> <color=#FFFF00>{currentFertilizer}%</color>";
                    
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
                                    // Mostra il range ideale (min-med-max) per lo stadio corrente
                                    fertilizerText += $" <color=#CCCCCC>(Range:</color> <color=#00FF00>{stageReq.fertilizerMin}%-{stageReq.fertilizerMed}%-{stageReq.fertilizerMax}%</color><color=#CCCCCC>)</color>";
                                    
                                    // Cambia colore in base al range
                                    if (stageReq.IsFertilizerInRange(currentFertilizer))
                                    {
                                        if (stageReq.IsFertilizerOptimal(currentFertilizer))
                                        {
                                            // Verde se ottimale
                                            fertilizerText = fertilizerText.Replace("<color=#FFFF00>", "<color=#00FF00>");
                                        }
                                        // Giallo se nel range ma non ottimale (già impostato)
                                    }
                                    else
                                    {
                                        // Rosso se fuori range
                                        fertilizerText = fertilizerText.Replace("<color=#FFFF00>", "<color=#FF0000>");
                                    }
                                }
                            }
                        }
                    }
                    
                    _fertilizerText.text = fertilizerText;
                    Debug.Log($"[PotDetailsWidget] ✅ Aggiornato Fertilizzante: {currentFertilizer}%");
                }
            }
            else
            {
                Debug.LogWarning("[PotDetailsWidget] _fertilizerText non trovato! Collega il riferimento nella scena Unity.");
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
                    Debug.Log($"[PotDetailsWidget] ✅ Aggiornato Growth Points: W:{state.GrowthPointsWater} L:{state.GrowthPointsLight} F:{state.GrowthPointsFertilizer} (Tot: {totalPoints})");
                }
            }
            else
            {
                Debug.LogWarning("[PotDetailsWidget] _growthPointsText non trovato! Collega il riferimento nella scena Unity.");
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
                    Debug.Log($"[PotDetailsWidget] ✅ Aggiornato Optimal Days: {state.DaysConsecutiveOptimal}");
                }
            }
            else
            {
                Debug.LogWarning("[PotDetailsWidget] _optimalDaysText non trovato! Collega il riferimento nella scena Unity.");
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
                            _phAffinityText.text = $"<color=#CCCCCC>pH Affinity:</color> <color=#00FF00>{plantData.OptimalPhMin:F1} - {plantData.OptimalPhMax:F1}</color>";
                            Debug.Log($"[PotDetailsWidget] ✅ pH Affinity aggiornato per {state.PlantCode}: {plantData.OptimalPhMin:F1} - {plantData.OptimalPhMax:F1}");
                        }
                        else
                        {
                            _phAffinityText.text = "<color=#CCCCCC>pH Affinity:</color> <color=#FF0000>N/A</color>";
                            Debug.LogWarning($"[PotDetailsWidget] ⚠️ PlantData non trovato per PlantCode: {state.PlantCode}");
                        }
                    }
                    else
                    {
                        _phAffinityText.text = "<color=#CCCCCC>pH Affinity:</color> <color=#FF0000>{}</color>";
                        Debug.LogWarning("[PotDetailsWidget] ⚠️ PlantDatabase.Instance è null!");
                    }
                }
                else
                {
                    _phAffinityText.text = "<color=#CCCCCC>pH Affinity:</color> <color=#FF0000>{}</color>";
                    Debug.LogWarning($"[PotDetailsWidget] ⚠️ PlantCode vuoto o null per vaso {_currentSelectedPot?.PotId ?? "Unknown"}");
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
                            Debug.Log($"[PotDetailsWidget] ✅ pH Drift aggiornato per {state.PlantCode}: {phDrift:+#;-#;0}/giorno");
                        }
                        else
                        {
                            _phDriftText.text = "<color=#CCCCCC>pH Drift:</color> <color=#FF0000>N/A</color>";
                            Debug.LogWarning($"[PotDetailsWidget] ⚠️ PlantData non trovato per PlantCode: {state.PlantCode}");
                        }
                    }
                    else
                    {
                        _phDriftText.text = "<color=#CCCCCC>pH Drift:</color> <color=#FF0000>N/A</color>";
                        Debug.LogWarning("[PotDetailsWidget] ⚠️ PlantDatabase.Instance è null!");
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
                            
                            Debug.Log($"[PotDetailsWidget] 🔍 DEBUG ActivePower per {state.PlantCode}: " +
                                $"PlantData trovato={plantData != null}, " +
                                $"ActivePower null/empty={isNullOrEmpty}, " +
                                $"ActivePower length={activePowerValue?.Length ?? 0}, " +
                                $"ActivePower value='{activePowerValue}'");
                            
                            if (!isNullOrEmpty)
                            {
                                _effectsText.text = $"<color=#CCCCCC>Potere Attivo:</color>\n<color=#FFD700>{activePowerValue}</color>";
                                Debug.Log($"[PotDetailsWidget] ✅ Potere attivo aggiornato per {state.PlantCode}: {activePowerValue}");
                            }
                            else
                            {
                                _effectsText.text = "<color=#CCCCCC>Potere Attivo:</color>\n<color=#888888>Nessun potere attivo disponibile</color>";
                                Debug.LogWarning($"[PotDetailsWidget] ⚠️ Potere attivo vuoto per pianta {state.PlantCode}. Verifica l'asset PlantData in Unity Editor e ricarica l'asset (Ctrl+R o Assets > Refresh).");
                            }
                        }
                        else
                        {
                            _effectsText.text = "<color=#CCCCCC>Potere Attivo:</color>\n<color=#888888>Nessun potere attivo disponibile</color>";
                            Debug.LogWarning($"[PotDetailsWidget] ⚠️ PlantData null per PlantCode: {state.PlantCode}");
                        }
                    }
                    else
                    {
                        _effectsText.text = "<color=#CCCCCC>Potere Attivo:</color>\n<color=#FF0000>Errore database piante</color>";
                        Debug.LogWarning("[PotDetailsWidget] ⚠️ PlantDatabase.Instance è null!");
                    }
                }
                else
                {
                    _effectsText.text = "<color=#CCCCCC>Potere Attivo:</color>\n<color=#888888>Nessuna pianta selezionata</color>";
                    Debug.LogWarning($"[PotDetailsWidget] ⚠️ PlantCode vuoto per vaso {_currentSelectedPot?.PotId ?? "Unknown"}");
                }
            }
            else
            {
                Debug.LogWarning("[PotDetailsWidget] ⚠️ _effectsText non trovato! Verifica che esista un GameObject 'Effects' con TextMeshProUGUI nella gerarchia UI_PotDetails/Panel/Right/");
            }
        }
        
        private void UpdateProgressUI(PotStateModel state)
        {
            float progressPercentage = CalculateProgressPercentage(state);
            
            // GDD AZ-11: Verifica che _progressBar esista prima di aggiornarla
            if (_progressBar != null)
            {
                _progressBar.Value = progressPercentage / 100f;
                Debug.Log($"[PotDetailsWidget] ✅ Progress Bar aggiornata: {progressPercentage:F1}% (Value: {_progressBar.Value:F2})");
            }
            else
            {
                Debug.LogWarning("[PotDetailsWidget] ⚠️ _progressBar è null! Collega il riferimento nella scena Unity.");
            }
        
            Debug.Log($"[BLK-01.04] UI aggiornata: {state.PotId} - {GetStageName(state.Stage)} - {progressPercentage:F1}% - {GetProgressInfo(state)}");
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
                    Debug.Log($"[PotDetailsWidget] 🔍 DEBUG Progress Seed: points={points}, threshold={_growthConfig.pointsSeedToSprout}, progress={seedProgress:F1}%");
                    if (points >= _growthConfig.pointsSeedToSprout)
                        return 100f; // Pronto per avanzare
                    return seedProgress;
                    
                case (int)PlantStage.Sprout:
                    float sproutProgress = (float)points / _growthConfig.pointsSproutToMature * 100f;
                    Debug.Log($"[PotDetailsWidget] 🔍 DEBUG Progress Sprout: points={points}, threshold={_growthConfig.pointsSproutToMature}, progress={sproutProgress:F1}%");
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
                case 0: return "Empty";
                case 1: return "Seed";
                case 2: return "Sprout";
                case 3: return "Growth";
                case 4: return "Flowering";
                case 5: return "HarvestReady";
                case 6: return "Resting";
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
                    Debug.Log($"[PotDetailsWidget] ✅ Trovato TextMeshProUGUI per nome GameObject '{containsText}': {foundByName.name}");
                    return textComp;
                }
            }
            
            // Poi cerca per testo contenuto
            TextMeshProUGUI[] allTexts = searchRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in allTexts)
            {
                if (text != null && !string.IsNullOrEmpty(text.text) && text.text.Contains(containsText))
                {
                    Debug.Log($"[PotDetailsWidget] ✅ Trovato TextMeshProUGUI per '{containsText}': {text.name} (testo: '{text.text}')");
                    return text;
                }
            }
            
            // Ultimo tentativo: cerca per nome parziale (case-insensitive)
            foreach (var text in allTexts)
            {
                if (text != null && text.name.Contains(containsText, System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"[PotDetailsWidget] ✅ Trovato TextMeshProUGUI per nome parziale '{containsText}': {text.name}");
                    return text;
                }
            }
            
            Debug.LogWarning($"[PotDetailsWidget] ⚠️ Nessun TextMeshProUGUI trovato per '{containsText}'. Verifica che il GameObject abbia questo nome o che il testo contenga questa stringa.");
            return null;
        }
        
        /// <summary>
        /// Trova una ProgressBar nei figli cercando per nome GameObject padre o ProgressBar stesso
        /// </summary>
        private ProgressBar FindProgressBarInChildren(string containsName)
        {
            // Cerca prima nel GameObject _page se disponibile
            GameObject searchRoot = _page != null ? _page : gameObject;
            
            Debug.Log($"[PotDetailsWidget] 🔍 Cercando ProgressBar per '{containsName}' in '{searchRoot.name}'");
            
            // Prima cerca direttamente ProgressBar con nome contenente il testo cercato
            ProgressBar[] allProgressBars = searchRoot.GetComponentsInChildren<ProgressBar>(true);
            Debug.Log($"[PotDetailsWidget] 📊 Trovate {allProgressBars.Length} ProgressBar totali nella gerarchia");
            
            foreach (var progressBar in allProgressBars)
            {
                if (progressBar != null)
                {
                    Debug.Log($"[PotDetailsWidget]   - ProgressBar: '{progressBar.name}' (attivo: {progressBar.gameObject.activeSelf})");
                    if (progressBar.name.Contains(containsName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log($"[PotDetailsWidget] ✅ Trovato ProgressBar per '{containsName}': {progressBar.name}");
                        return progressBar;
                    }
                }
            }
            
            // Se non trovato direttamente, cerca ProgressBar figlio di GameObject padre con nome corrispondente
            // Esempio: cerca ProgressBar figlio di "Hydration" quando containsName = "Hydration Progress"
            string parentName = containsName.Replace(" Progress", "").Replace("Progress", "").Trim();
            Debug.Log($"[PotDetailsWidget] 🔍 Cercando GameObject padre '{parentName}' per trovare ProgressBar figlio");
            
            // Cerca ricorsivamente il GameObject padre (non solo figli diretti)
            Transform parentTransform = FindTransformRecursive(searchRoot.transform, parentName);
            if (parentTransform != null)
            {
                Debug.Log($"[PotDetailsWidget] ✅ Trovato GameObject padre '{parentName}'");
                ProgressBar progressBar = parentTransform.GetComponentInChildren<ProgressBar>(true);
                if (progressBar != null)
                {
                    Debug.Log($"[PotDetailsWidget] ✅ Trovato ProgressBar figlio di '{parentName}': {progressBar.name}");
                    return progressBar;
                }
                else
                {
                    Debug.LogWarning($"[PotDetailsWidget] ⚠️ GameObject '{parentName}' trovato ma nessuna ProgressBar figlio");
                }
            }
            else
            {
                Debug.LogWarning($"[PotDetailsWidget] ⚠️ GameObject padre '{parentName}' non trovato nella gerarchia");
            }
            
            // Cerca anche per nome parziale nel padre (es. "LightStress" per "Lighting Progress")
            if (containsName.Contains("Light", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"[PotDetailsWidget] 🔍 Cercando fallback per Light: 'LightStress' o 'Light'");
                Transform lightParent = FindTransformRecursive(searchRoot.transform, "LightStress");
                if (lightParent == null)
                {
                    // Cerca anche "Light" come fallback
                    lightParent = FindTransformRecursive(searchRoot.transform, "Light");
                }
                
                if (lightParent != null)
                {
                    Debug.Log($"[PotDetailsWidget] ✅ Trovato GameObject Light: '{lightParent.name}'");
                    ProgressBar progressBar = lightParent.GetComponentInChildren<ProgressBar>(true);
                    if (progressBar != null)
                    {
                        Debug.Log($"[PotDetailsWidget] ✅ Trovato ProgressBar figlio di 'LightStress/Light': {progressBar.name}");
                        return progressBar;
                    }
                }
            }
            
            Debug.LogWarning($"[PotDetailsWidget] ⚠️ Nessuna ProgressBar trovata per '{containsName}'. Verifica che il GameObject abbia questo nome o che ci sia un ProgressBar figlio di '{parentName}'.");
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
        /// </summary>
        private void SetupGrowthTooltip()
        {
            if (_progressBar == null)
                return;
                
            // Crea tooltip panel se non esiste
            if (_growthTooltipPanel == null)
            {
                // Trova o crea Canvas con sorting order alto per tooltip
                Canvas tooltipCanvas = GetOrCreateTooltipCanvas();
                
                GameObject tooltipGO = new GameObject("GrowthTooltipPanel");
                tooltipGO.transform.SetParent(tooltipCanvas.transform, false);
                
                RectTransform tooltipRect = tooltipGO.AddComponent<RectTransform>();
                // Centrato orizzontalmente, ancorato in alto
                tooltipRect.anchorMin = new Vector2(0.5f, 1f);
                tooltipRect.anchorMax = new Vector2(0.5f, 1f);
                tooltipRect.pivot = new Vector2(0.5f, 1f);
                // Posizionato centrato in alto dello schermo, più in basso per evitare sovrapposizione con pH
                tooltipRect.anchoredPosition = new Vector2(0, -150);
                
                // Tooltip più grande per contenuto dettagliato
                tooltipRect.sizeDelta = new Vector2(600, 300);
                
                Image tooltipBg = tooltipGO.AddComponent<Image>();
                // Colore sfondo tooltip: #3d568e
                tooltipBg.color = new Color(61f/255f, 86f/255f, 142f/255f, 1f);
                
                _growthTooltipPanel = tooltipGO;
                _growthTooltipPanel.SetActive(false);
            }
            
            // Crea testo tooltip se non esiste
            if (_growthTooltipText == null && _growthTooltipPanel != null)
            {
                GameObject textGO = new GameObject("GrowthTooltipText");
                textGO.transform.SetParent(_growthTooltipPanel.transform, false);
                
                _growthTooltipText = textGO.AddComponent<TextMeshProUGUI>();
                _growthTooltipText.color = Color.white;
                _growthTooltipText.fontSize = 16; // Testo più grande (era 12)
                _growthTooltipText.alignment = TextAlignmentOptions.Left;
                _growthTooltipText.richText = true;
                
                RectTransform textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(12, 12);
                textRect.offsetMax = new Vector2(-12, -12);
            }
            
            // Aggiungi EventTrigger alla progress bar per hover
            EventTrigger trigger = _progressBar.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = _progressBar.gameObject.AddComponent<EventTrigger>();
            
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
                            _growthTooltipPanel.SetActive(true);
                    }
                }
            });
            trigger.triggers.Add(enterEntry);
            
            // PointerExit - nascondi tooltip
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => {
                if (_growthTooltipPanel != null)
                    _growthTooltipPanel.SetActive(false);
            });
            trigger.triggers.Add(exitEntry);
        }
        
        /// <summary>
        /// Aggiorna la label Growth con lo stato (IN CRESCITA, Stabile, Difficoltà, Malata)
        /// </summary>
        private void UpdateGrowthLabel(PotStateModel state)
        {
            // Auto-trova label se non assegnata
            if (_growthLabelText == null)
            {
                _growthLabelText = FindTextInChildren("GrowthLabel");
            }
            
            if (_growthLabelText != null)
            {
                if (state == null || state.IsEmpty || !state.HasPlant)
                {
                    _growthLabelText.text = "Growth: Stabile";
                    _growthLabelText.color = new Color(0.6f, 0.6f, 0.6f); // Grigio
                }
                else
                {
                    string status = GetGrowthStatus(state);
                    _growthLabelText.text = $"Growth: {status}";
                    
                    // Colore in base allo stato
                    switch (status)
                    {
                        case "IN CRESCITA":
                            _growthLabelText.color = new Color(0.2f, 1f, 0.2f); // Verde
                            break;
                        case "Stabile":
                            _growthLabelText.color = new Color(1f, 1f, 0.2f); // Giallo
                            break;
                        case "Difficoltà":
                            _growthLabelText.color = new Color(1f, 0.5f, 0.2f); // Arancione
                            break;
                        case "Malata":
                            _growthLabelText.color = new Color(1f, 0.2f, 0.2f); // Rosso
                            break;
                        default:
                            _growthLabelText.color = Color.white;
                            break;
                    }
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
            
            // Verifica range per ogni parametro
            bool waterOk = stageReq.IsHydrationInRange(hydrationPercent);
            bool lightOk = stageReq.IsLedRequirementMet(state.LedSystemState) && 
                          stageReq.IsLightInRange(state.LightExposure);
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
            string lightStatus = lightOk ? "<color=#00FF00>OK</color>" : "<color=#FF0000>NON OK</color>";
            sb.AppendLine($"• <color=#FFD700>Luce</color>: {lightStatus}");
            if (!lightOk)
            {
                string ledRequired = stageReq.GetRequiredLed()?.ToString() ?? "Nessuno";
                sb.AppendLine($"  LED richiesto: {ledRequired}");
                sb.AppendLine($"  Range ideale: {stageReq.lightMin}% - {stageReq.lightMax}%");
                sb.AppendLine($"  Attuale: {state.LightExposure}%");
                    }
                    sb.AppendLine();
            
            // Fertilizer
            string fertilizerStatus = fertilizerOk ? "<color=#00FF00>OK</color>" : "<color=#FF0000>NON OK</color>";
            sb.AppendLine($"• <color=#90EE90>Fertilizzante</color>: {fertilizerStatus}");
            if (!fertilizerOk)
            {
                sb.AppendLine($"  Range ideale: {stageReq.fertilizerMin}% - {stageReq.fertilizerMax}%");
                sb.AppendLine($"  Attuale: {state.FertilizerLevel}%");
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
            if (plantData == null || _phSystem == null || _potSystemConfig == null)
            {
                // Fallback: mostra score base se non possiamo calcolare
                if (_conditionLabelText != null)
                    _conditionLabelText.text = $"Condizione: Sana ({state.ConditionScore}/100)";
                return;
            }
            
            ConditionResult result = PlantConditionSystem.CalculateCondition(
                state, 
                plantData, 
                _phSystem, 
                _potSystemConfig, 
                _dayCycleSystem?.CurrentDay ?? 1, 
                state.PreviousDayConditionScore);
            
            // Aggiorna label condizione con forecast
            if (_conditionLabelText != null)
            {
                _conditionLabelText.richText = true;
                string conditionName = PlantConditionSystem.GetConditionName(result.Condition, 
                    PlantConditionSystem.IsOverwatering(state, _potSystemConfig.MaxHydration));
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