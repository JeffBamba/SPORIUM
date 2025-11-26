using System.Linq;
using _Project.Sporae.Core;
using _Project.Watering;
using Sporae.Dome.PotSystem.Growth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project
{
    public class PotDetailsWidget : MonoBehaviour
    {
        [SerializeField] private Button _plantButton;
        [SerializeField] private Button _wateringButton;
        [SerializeField] private Button _lightButton;
        [SerializeField] private Button _uprootButton;

        [SerializeField] private TextMeshProUGUI _idLabel;
        [SerializeField] private TextMeshProUGUI _stageLabel;
        [SerializeField] private ProgressBar _progressBar;
        [SerializeField] private Image _stageImage;

        [SerializeField] private GameObject _page;

        [SerializeField] private WateringMinigame _wateringMinigame;
        
        [Header("Seed Selector")]
        [SerializeField] private UISeedSelector _seedSelector;
        
        private PotSlot _currentSelectedPot;
        private PlantGrowthConfig _growthConfig;
        private GameManager _gameManager;
        private DayCycleSystem _dayCycleSystem;
        
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
            
            _plantButton.onClick.AddListener(() => OnActionButtonClicked(PotEvents.PotActionType.Plant));
            _wateringButton.onClick.AddListener(() => OnActionButtonClicked(PotEvents.PotActionType.Water));
            _lightButton.onClick.AddListener(() => OnActionButtonClicked(PotEvents.PotActionType.Light));
            _uprootButton.onClick.AddListener(() => OnActionButtonClicked(PotEvents.PotActionType.Uproot));
            
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
                    Debug.LogWarning("[PotDetailsWidget] UISeedSelector non trovato nella scena. Creazione automatica...");
                    // Crea UISeedSelector automaticamente
                    GameObject seedSelectorGO = new GameObject("UISeedSelector");
                    _seedSelector = seedSelectorGO.AddComponent<UISeedSelector>();
                }
            }
            
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
            if (_currentSelectedPot == null || _currentSelectedPot.PotActions == null)
            {
                Debug.LogWarning("[PotDetailsWidget] Nessun vaso selezionato quando seme selezionato!");
                return;
            }
            
            Debug.Log($"[PotDetailsWidget] Piantando seme {seedTypeId} nel vaso {_currentSelectedPot.PotId}");
            
            // Piantare il seme selezionato
            bool success = _currentSelectedPot.PotActions.DoPlant(seedTypeId);
            
            if (success)
            {
                Debug.Log($"[PotDetailsWidget] Seme {seedTypeId} piantato con successo!");
                // Aggiorna l'UI
                UpdateActionButtons(_currentSelectedPot);
                
                var growthController = _currentSelectedPot.GetComponent<PotGrowthController>();
                if (growthController != null)
                    UpdateStageAndProgressUI(_currentSelectedPot);
            }
            else
            {
                Debug.LogWarning($"[PotDetailsWidget] Fallito piantare seme {seedTypeId}!");
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
            if (_seedSelector == null)
            {
                Debug.LogError("[PotDetailsWidget] UISeedSelector non disponibile!");
                return;
            }
            
            _seedSelector.Show(targetPot);
        }
        
        private void LoadGrowthConfig()
        {
            _growthConfig = Resources.Load<PlantGrowthConfig>("Configs/PlantGrowthConfig_Default");
            if (_growthConfig != null)
                return;
            
            Debug.LogWarning($"[{nameof(PotDetailsWidget)}] PlantGrowthConfig non trovato in Resources/Configs/. Usando valori di default.");
            _growthConfig = ScriptableObject.CreateInstance<PlantGrowthConfig>();
        }

        private void HandleDayChanged(int obj)
        { 
            if (!_currentSelectedPot)
                return;
        
            UpdateActionButtons(_currentSelectedPot);
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
                    
                    success = selectedPot.PotActions.CanWater();
                    if (success)
                        _wateringMinigame.Show(selectedPot);
                    
                    break;
                
                case PotEvents.PotActionType.Light:
                    success = selectedPot.PotActions.DoLight();
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
        }
        
        private void UpdateActionButtons(PotSlot pot)
        {
            if (!pot || !pot.PotActions)
                return;
        
            // _page.SetActive(pot.InRange);
            
            // Aggiorna lo stato di ogni pulsante
            UpdateButtonState(_plantButton, pot.PotActions.CanPlant(), "Piantare");
            UpdateButtonState(_wateringButton, pot.PotActions.CanWater(), "Annaffiare");
            UpdateButtonState(_lightButton, pot.PotActions.CanLight(), "Illuminare");
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
            
            // Aggiorna PotId
            if (_idLabel)
                _idLabel.text = pot.PotId;
            
            // BLK-01.04: Aggiorna Stage Label con informazioni dettagliate
            if (_stageLabel)
            {
                string stageName = GetStageName(state.Stage);
                string stageInfo = GetStageInfo(state);
                _stageLabel.text = $"{stageName} - {stageInfo}";
            }
            
            // BLK-01.04: Aggiorna Stage Icon con colore appropriato
            if (_stageImage != null)
            {
                _stageImage.sprite = GetStageSprite();
                // TODO: Sostituire con sprite reali quando disponibili
            }
    
            UpdateProgressUI(state);
        }
        
        private void UpdateProgressUI(PotStateModel state)
        {
            float progressPercentage = CalculateProgressPercentage(state);
           _progressBar.Value = progressPercentage / 100f;
        
            Debug.Log($"[BLK-01.04] UI aggiornata: {state.PotId} - {GetStageName(state.Stage)} - {progressPercentage:F1}% - {GetProgressInfo(state)}");
        }
        
        private int CalculateCurrentGrowthPoints(PotStateModel state)
        {
            int points = state.GrowthPoints;
            bool
                hadHydration = (state.LastWateredDay == _dayCycleSystem.CurrentDay),
                hadLight = (state.LastLitDay == _dayCycleSystem.CurrentDay);

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
                    if (points >= _growthConfig.pointsSeedToSprout)
                        return 100f; // Pronto per avanzare
                    return (float)points / _growthConfig.pointsSeedToSprout * 100f;
                    
                case (int)PlantStage.Sprout:
                    if (points >= _growthConfig.pointsSproutToMature)
                        return 100f; // Pronto per avanzare
                    return (float)points / _growthConfig.pointsSproutToMature * 100f;
                    
                case (int)PlantStage.Mature:
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
                case (int)PlantStage.Mature:
                    return Color.yellow;
                default:
                    return Color.white;
            }
        }
        
        private string GetStageInfo(PotStateModel state)
        {
            return "Pronto per piantare";
        }
        
        private string GetStageName(int stage)
        {
            switch (stage)
            {
                case 0: return "Empty";
                case 1: return "Seed";
                case 2: return "Sprout";
                case 3: return "Mature";
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
                    return $"{Mathf.RoundToInt(percentage)}% → Mature";
                case (int)PlantStage.Mature:
                    return "100% - Mature!";
                default:
                    return $"{Mathf.RoundToInt(percentage)}%";
            }
        }
    }
}