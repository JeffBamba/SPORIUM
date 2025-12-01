using System.Linq;
using _Project.Sporae.Core;
using _Project.Watering;
using Sporae.Dome.PotSystem.Growth;
using Sporae.Dome.PotSystem;
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
        [SerializeField] private Button _sprayButton;
        [SerializeField] private Button _uprootButton;

        [SerializeField] private TextMeshProUGUI _idLabel;
        [SerializeField] private TextMeshProUGUI _stageLabel;
        [SerializeField] private ProgressBar _progressBar;
        [SerializeField] private Image _stageImage;
        
        [Header("Plant Stats UI")]
        [SerializeField] private TextMeshProUGUI _hydrationStressText;
        [SerializeField] private TextMeshProUGUI _lightStressText;
        [SerializeField] private ProgressBar _hydrationProgressBar;
        [SerializeField] private ProgressBar _lightProgressBar;
        [SerializeField] private TextMeshProUGUI _phStateText;
        [SerializeField] private TextMeshProUGUI _phAffinityText;
        [SerializeField] private TextMeshProUGUI _rarityText;

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
            if (_sprayButton != null)
                _sprayButton.onClick.AddListener(() => OnActionButtonClicked(PotEvents.PotActionType.Spray));
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
                    Debug.Log("[PotDetailsWidget] UISeedSelector non trovato nella scena. Creazione automatica...");
                    // Crea UISeedSelector automaticamente
                    GameObject seedSelectorGO = new GameObject("UISeedSelector");
                    _seedSelector = seedSelectorGO.AddComponent<UISeedSelector>();
                    Debug.Log("[PotDetailsWidget] ✅ UISeedSelector creato automaticamente con successo.");
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
                
                case PotEvents.PotActionType.Spray:
                    success = selectedPot.PotActions.DoSprayAntifungal();
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
            
            // Aggiorna lo stato di ogni pulsante
            UpdateButtonState(_plantButton, pot.PotActions.CanPlant(), "Piantare");
            UpdateButtonState(_wateringButton, pot.PotActions.CanWater(), "Annaffiare");
            UpdateButtonState(_lightButton, pot.PotActions.CanLight(), "Illuminare");
            if (_sprayButton != null)
                UpdateButtonState(_sprayButton, pot.PotActions.CanSprayAntifungal(), "Spray");
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
    
            // Aggiorna Idratazione e Light Exposure
            UpdatePlantStatsUI(state);
    
            UpdateProgressUI(state);
        }
        
        /// <summary>
        /// Aggiorna gli elementi UI per Idratazione, Light Exposure e altri stats
        /// </summary>
        private void UpdatePlantStatsUI(PotStateModel state)
        {
            if (state == null) return;
            
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
            
            if (_phStateText == null)
            {
                _phStateText = FindTextInChildren("pH State");
            }
            
            if (_phAffinityText == null)
            {
                _phAffinityText = FindTextInChildren("pH Affinity");
            }
            
            if (_rarityText == null)
            {
                _rarityText = FindTextInChildren("Rarity");
            }
            
            // Aggiorna Hydration Stress (mostra idratazione)
            int maxHydration = _currentSelectedPot?.PotActions?.GetMaxHydration() ?? 3;
            if (_hydrationStressText != null)
            {
                _hydrationStressText.text = $"Hydration stress: {state.Hydration}/{maxHydration}";
                Debug.Log($"[PotDetailsWidget] Aggiornato Hydration stress: {state.Hydration}/{maxHydration}");
            }
            else
            {
                Debug.LogWarning("[PotDetailsWidget] _hydrationStressText non trovato! Collega il riferimento nella scena Unity.");
            }
            
            // Aggiorna Hydration Progress Bar
            if (_hydrationProgressBar != null)
            {
                float hydrationValue = maxHydration > 0 ? (float)state.Hydration / maxHydration : 0f;
                _hydrationProgressBar.Value = hydrationValue;
            }
            
            // Aggiorna Light Stress (mostra light exposure)
            int maxLight = _currentSelectedPot?.PotActions?.GetMaxLightExposure() ?? 3;
            if (_lightStressText != null)
            {
                _lightStressText.text = $"Light stress: {state.LightExposure}/{maxLight}";
                Debug.Log($"[PotDetailsWidget] Aggiornato Light stress: {state.LightExposure}/{maxLight}");
            }
            else
            {
                Debug.LogWarning("[PotDetailsWidget] _lightStressText non trovato! Collega il riferimento nella scena Unity.");
            }
            
            // Aggiorna Light Progress Bar
            if (_lightProgressBar != null)
            {
                float lightValue = maxLight > 0 ? (float)state.LightExposure / maxLight : 0f;
                _lightProgressBar.Value = lightValue;
            }
            
            // Aggiorna pH State (mostra pH corrente se disponibile)
            if (_phStateText != null)
            {
                var phSystem = ServiceContainer.Instance?.Get<PhSystem>();
                if (phSystem != null)
                {
                    float currentPh = phSystem.CurrentPh;
                    string bandName = phSystem.GetBandName();
                    _phStateText.text = $"pH State: {currentPh:F1} ({bandName})";
                }
                else
                {
                    _phStateText.text = "pH State: N/A";
                }
            }
            
            // Aggiorna pH Affinity (mostra pH ottimale della pianta se disponibile)
            if (_phAffinityText != null)
            {
                if (!string.IsNullOrEmpty(state.PlantCode))
                {
                    var plantDatabase = PlantDatabase.Instance;
                    if (plantDatabase != null)
                    {
                        var plantData = plantDatabase.GetPlantDataByCode(state.PlantCode);
                        if (plantData != null)
                        {
                            _phAffinityText.text = $"pH Affinity: {plantData.OptimalPhMin:F1} - {plantData.OptimalPhMax:F1}";
                            Debug.Log($"[PotDetailsWidget] ✅ pH Affinity aggiornato per {state.PlantCode}: {plantData.OptimalPhMin:F1} - {plantData.OptimalPhMax:F1}");
                        }
                        else
                        {
                            _phAffinityText.text = "pH Affinity: N/A";
                            Debug.LogWarning($"[PotDetailsWidget] ⚠️ PlantData non trovato per PlantCode: {state.PlantCode}");
                        }
                    }
                    else
                    {
                        _phAffinityText.text = "pH Affinity: {}";
                        Debug.LogWarning("[PotDetailsWidget] ⚠️ PlantDatabase.Instance è null!");
                    }
                }
                else
                {
                    _phAffinityText.text = "pH Affinity: {}";
                    Debug.LogWarning($"[PotDetailsWidget] ⚠️ PlantCode vuoto o null per vaso {_currentSelectedPot?.PotId ?? "Unknown"}");
                }
            }
            
            // Aggiorna Rarity (mostra rarità della pianta se disponibile)
            if (_rarityText != null)
            {
                if (!string.IsNullOrEmpty(state.PlantCode))
                {
                    var plantDatabase = PlantDatabase.Instance;
                    if (plantDatabase != null)
                    {
                        var plantData = plantDatabase.GetPlantDataByCode(state.PlantCode);
                        if (plantData != null)
                        {
                            _rarityText.text = $"Rarity: {plantData.Rarity}";
                        }
                        else
                        {
                            _rarityText.text = "Rarity: {}";
                        }
                    }
                    else
                    {
                        _rarityText.text = "Rarity: {}";
                    }
                }
                else
                {
                    _rarityText.text = "Rarity: {}";
                }
            }
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
                    return $"Giorno {daysSincePlant} - {Mathf.Clamp(points, 0, _growthConfig?.pointsSeedToSprout ?? 2)}/{_growthConfig?.pointsSeedToSprout ?? 2} punti";
                case (int)PlantStage.Sprout:
                    return $"Giorno {daysSincePlant} - {Mathf.Clamp(points, 0, _growthConfig?.pointsSproutToMature ?? 3)}/{_growthConfig?.pointsSproutToMature ?? 3} punti";
                case (int)PlantStage.Mature:
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
        
        /// <summary>
        /// Trova un TextMeshProUGUI nei figli cercando per testo contenuto
        /// </summary>
        private TextMeshProUGUI FindTextInChildren(string containsText)
        {
            // Cerca prima nel GameObject _page se disponibile
            GameObject searchRoot = _page != null ? _page : gameObject;
            
            TextMeshProUGUI[] allTexts = searchRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in allTexts)
            {
                if (text != null && !string.IsNullOrEmpty(text.text) && text.text.Contains(containsText))
                {
                    Debug.Log($"[PotDetailsWidget] ✅ Trovato TextMeshProUGUI per '{containsText}': {text.name} (testo: '{text.text}')");
                    return text;
                }
            }
            
            // Se non trovato per testo, cerca per nome GameObject
            Transform found = searchRoot.transform.Find(containsText.Replace(" ", ""));
            if (found != null)
            {
                TextMeshProUGUI textComp = found.GetComponent<TextMeshProUGUI>();
                if (textComp != null)
                {
                    Debug.Log($"[PotDetailsWidget] ✅ Trovato TextMeshProUGUI per nome GameObject '{containsText}': {found.name}");
                    return textComp;
                }
            }
            
            Debug.LogWarning($"[PotDetailsWidget] ⚠️ Nessun TextMeshProUGUI trovato per '{containsText}'. Verifica che il testo contenga questa stringa o che il GameObject abbia questo nome.");
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
    }
}