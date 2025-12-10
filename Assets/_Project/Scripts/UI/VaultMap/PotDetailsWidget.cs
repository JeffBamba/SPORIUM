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
        [SerializeField] private Button _uprootButton;

        [SerializeField] private TextMeshProUGUI _idLabel;
        [SerializeField] private TextMeshProUGUI _stageLabel;
        [SerializeField] private TextMeshProUGUI _plantDescriptionLabel;
        [SerializeField] private ProgressBar _progressBar;
        [SerializeField] private Image _stageImage;
        
        [Header("Plant Stats UI")]
        [SerializeField] private TextMeshProUGUI _hydrationStressText;
        [SerializeField] private TextMeshProUGUI _lightStressText;
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
        }
        
        private void Update()
        {
            if (_currentSelectedPot && !_currentSelectedPot.Interactable.PlayerInRange)
            {
                _currentSelectedPot = null;
                _page.SetActive(false);
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
            
            if (_uprootButton != null)
                _uprootButton.onClick.AddListener(() => OnActionButtonClicked(PotEvents.PotActionType.Uproot));
            else
                Debug.LogError("[PotDetailsWidget] ⚠️ _uprootButton non assegnato! Collega il riferimento nella scena Unity.");
            
            InitializeSeedSelector();
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
                tooltipRect.anchorMin = new Vector2(0, 1);
                tooltipRect.anchorMax = new Vector2(0, 1);
                tooltipRect.pivot = new Vector2(0, 1);
                
                // Posiziona il tooltip in base alla posizione della progress bar
                Vector3 worldPos = _progressBar.transform.position;
                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(tooltipCanvas.worldCamera ?? Camera.main, worldPos);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    tooltipCanvas.transform as RectTransform, screenPos, tooltipCanvas.worldCamera ?? Camera.main, out Vector2 localPos);
                tooltipRect.anchoredPosition = new Vector2(localPos.x, localPos.y - 60);
                
                // Tooltip più grande per contenuto dettagliato
                tooltipRect.sizeDelta = new Vector2(450, 300);
                
                Image tooltipBg = tooltipGO.AddComponent<Image>();
                // Verde scuro solido (non trasparente)
                tooltipBg.color = new Color(0f, 0.3f, 0.15f, 1f);
                
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
        /// Aggiorna tooltip Growth con dati attuali
        /// </summary>
        private void UpdateGrowthTooltip(PotStateModel state)
        {
            if (_growthTooltipText == null || state == null)
                return;
                
            _growthTooltipText.text = BuildGrowthTooltip(state);
        }
        
        /// <summary>
        /// Costruisce il tooltip di crescita (progress bar Growth)
        /// </summary>
        private string BuildGrowthTooltip(PotStateModel state)
        {
            var sb = new System.Text.StringBuilder();
            
            if (_growthConfig == null)
            {
                sb.AppendLine("<b>Growth: Config non disponibile</b>");
                return sb.ToString();
            }
            
            string stageName = GetStageName(state.Stage);
            float perc = CalculateProgressPercentage(state);
            sb.AppendLine($"<b>Growth: {stageName} ({perc:F1}%)</b>");
            sb.AppendLine();
            
            // Calcolo dettagliato passo-passo
            int basePoints = state.GrowthPoints;
            bool hadHydration = state.WateringSystemOn;
            bool hadLight = (state.LedSystemState != LedSystemState.Off);
            
            int dailyBonus = (hadHydration, hadLight) switch
            {
                (true, true)   => _growthConfig.pointsIdealCare,
                (true, false)  => _growthConfig.pointsPartialCare,
                (false, true)  => _growthConfig.pointsPartialCare,
                (false, false) => _growthConfig.pointsNoCare
            };
            
            int totalPoints = basePoints + dailyBonus;
            string threshold = GetStageThreshold(state.Stage);
            int thresholdValue = GetStageThresholdValue(state.Stage);
            
            // Mostra calcolo dettagliato
            sb.AppendLine("<color=#FFFF00>Calcolo Progresso:</color>");
            sb.AppendLine();
            sb.AppendLine($"<color=#CCCCCC>1. GrowthPoints accumulati:</color> <color=#FFFFFF>{basePoints}</color>");
            sb.AppendLine();
            
            // Bonus giornaliero dettagliato
            sb.AppendLine("<color=#CCCCCC>2. Bonus giornaliero (oggi):</color>");
            if (dailyBonus > 0)
            {
                string bonusColor = (hadHydration && hadLight) ? "#00FF00" : "#FFFF00";
                string bonusType = (hadHydration, hadLight) switch
                {
                    (true, true)   => "Cura Ideale",
                    (true, false)  => "Cura Parziale",
                    (false, true)  => "Cura Parziale",
                    (false, false) => "Nessuna Cura"
                };
                sb.AppendLine($"   <color={bonusColor}>+{dailyBonus} punti</color> ({bonusType})");
                sb.AppendLine($"   • Idratazione: <color={(hadHydration ? "#00FF00" : "#FF0000")}>{(hadHydration ? "ON" : "OFF")}</color>");
                sb.AppendLine($"   • Luce LED: <color={(hadLight ? "#00FF00" : "#FF0000")}>{(hadLight ? "Accesa" : "Spenta")}</color>");
            }
            else
            {
                sb.AppendLine($"   <color=#FF0000>+{dailyBonus} punti</color> (Nessuna cura attiva)");
                sb.AppendLine($"   • Idratazione: <color=#FF0000>OFF</color>");
                sb.AppendLine($"   • Luce LED: <color=#FF0000>Spenta</color>");
            }
            sb.AppendLine();
            
            sb.AppendLine($"<color=#CCCCCC>3. Totale punti:</color> <color=#FFFFFF>{basePoints} + {dailyBonus} = <b>{totalPoints}</b></color>");
            sb.AppendLine();
            sb.AppendLine($"<color=#CCCCCC>4. Soglia per avanzare:</color> <color=#FFFFFF>{thresholdValue} punti</color>");
            sb.AppendLine();
            
            // Calcolo percentuale
            if (thresholdValue > 0)
            {
                float calculatedPerc = (float)totalPoints / thresholdValue * 100f;
                sb.AppendLine($"<color=#CCCCCC>5. Progresso:</color> <color=#FFFFFF>{totalPoints} / {thresholdValue} × 100% = <b>{calculatedPerc:F1}%</b></color>");
            }
            else
            {
                sb.AppendLine($"<color=#CCCCCC>5. Progresso:</color> <color=#FFFFFF>N/A (soglia non definita)</color>");
            }
            sb.AppendLine();
            
            // Spiegazione impatti
            sb.AppendLine("<color=#FFFF00>Impatti:</color>");
            if (hadHydration && hadLight)
            {
                sb.AppendLine($"<color=#00FF00>• Cura Ideale:</color> +{_growthConfig.pointsIdealCare} punti/giorno");
                sb.AppendLine("  (Watering ON + LED corretto)");
            }
            else if (hadHydration || hadLight)
            {
                sb.AppendLine($"<color=#FFFF00>• Cura Parziale:</color> +{_growthConfig.pointsPartialCare} punti/giorno");
                sb.AppendLine($"  (Solo {(hadHydration ? "Watering" : "LED")} attivo)");
            }
            else
            {
                sb.AppendLine($"<color=#FF0000>• Nessuna Cura:</color> +{_growthConfig.pointsNoCare} punti/giorno");
                sb.AppendLine("  (Watering OFF + LED spento)");
            }
            
            int pointsNeeded = Mathf.Max(0, thresholdValue - totalPoints);
            if (pointsNeeded > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"<color=#CCCCCC>Punti mancanti per avanzare:</color> <color=#FFFFFF>{pointsNeeded}</color>");
                if (dailyBonus > 0)
                {
                    int daysNeeded = Mathf.CeilToInt((float)pointsNeeded / dailyBonus);
                    sb.AppendLine($"<color=#CCCCCC>Giorni stimati (con cura attuale):</color> <color=#FFFFFF>~{daysNeeded}</color>");
                }
            }
            else if (totalPoints >= thresholdValue)
            {
                sb.AppendLine();
                sb.AppendLine("<color=#00FF00>✓ Pronto per avanzare al prossimo stadio!</color>");
            }
            
            // Sezione Calcolo Condizione
            sb.AppendLine();
            sb.AppendLine("<color=#FFFF00>━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
            sb.AppendLine();
            sb.AppendLine("<color=#FFFF00>Calcolo Condizione (0-100):</color>");
            sb.AppendLine();
            
            // Calcola condizione per mostrare i contributi
            PlantData plantData = state.GetPlantData();
            if (plantData != null && _phSystem != null && _potSystemConfig != null)
            {
                ConditionResult conditionResult = PlantConditionSystem.CalculateCondition(
                    state,
                    plantData,
                    _phSystem,
                    _potSystemConfig,
                    _dayCycleSystem?.CurrentDay ?? 1,
                    state.PreviousDayConditionScore >= 0 ? state.PreviousDayConditionScore : state.ConditionScore);
                
                string conditionName = PlantConditionSystem.GetConditionName(
                    conditionResult.Condition,
                    PlantConditionSystem.IsOverwatering(state, _potSystemConfig.MaxHydration));
                
                sb.AppendLine($"<color=#CCCCCC>Condizione attuale:</color> <color=#FFFFFF><b>{conditionName} ({conditionResult.Score}/100)</b></color>");
                sb.AppendLine();
                
                // Contributi positivi
                var positiveContribs = System.Array.FindAll(conditionResult.Contributors, c => c.IsPositive);
                if (positiveContribs.Length > 0)
                {
                    sb.AppendLine("<color=#00FF00>Contributi positivi:</color>");
                    foreach (var contrib in positiveContribs)
                    {
                        sb.AppendLine($"  • {contrib.Source}: <color=#00FF00>+{contrib.Value}</color>");
                    }
                    sb.AppendLine();
                }
                
                // Contributi negativi
                var negativeContribs = System.Array.FindAll(conditionResult.Contributors, c => !c.IsPositive);
                if (negativeContribs.Length > 0)
                {
                    sb.AppendLine("<color=#FF0000>Contributi negativi:</color>");
                    foreach (var contrib in negativeContribs)
                    {
                        sb.AppendLine($"  • {contrib.Source}: <color=#FF0000>{contrib.Value}</color>");
                    }
                    sb.AppendLine();
                }
                
                // Calcolo totale
                int baseScore = 50; // BASE_SCORE
                int totalPositive = 0;
                int totalNegative = 0;
                foreach (var contrib in conditionResult.Contributors)
                {
                    if (contrib.IsPositive)
                        totalPositive += contrib.Value;
                    else
                        totalNegative += Mathf.Abs(contrib.Value);
                }
                
                sb.AppendLine("<color=#CCCCCC>Calcolo totale:</color>");
                sb.AppendLine($"  Base: <color=#FFFFFF>{baseScore}</color>");
                if (totalPositive > 0)
                    sb.AppendLine($"  Bonus: <color=#00FF00>+{totalPositive}</color>");
                if (totalNegative > 0)
                    sb.AppendLine($"  Malus: <color=#FF0000>-{totalNegative}</color>");
                sb.AppendLine($"  Totale: <color=#FFFFFF><b>{baseScore + totalPositive - totalNegative}</b></color> → clampato a <color=#FFFFFF><b>{conditionResult.Score}/100</b></color>");
                
                // Forecast
                if (conditionResult.ScoreDelta != 0)
                {
                    string forecastSymbol = PlantConditionSystem.GetForecastSymbol(conditionResult.Forecast);
                    string forecastColor = conditionResult.Forecast switch
                    {
                        ForecastDirection.Up => "#00FF00",
                        ForecastDirection.Down => "#FF0000",
                        _ => "#CCCCCC"
                    };
                    sb.AppendLine();
                    sb.AppendLine($"<color=#CCCCCC>Forecast:</color> <color={forecastColor}>{forecastSymbol}</color> (Δ {conditionResult.ScoreDelta:+0;-0} dal giorno precedente)");
                }
            }
            else
            {
                sb.AppendLine("<color=#FF0000>Dati non disponibili per calcolo condizione</color>");
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