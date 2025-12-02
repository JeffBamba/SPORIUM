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
        [SerializeField] private Button _blueLedButton;
        [SerializeField] private Button _redLedButton;
        [SerializeField] private Button _sprayButton;
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
        [SerializeField] private TextMeshProUGUI _rarityText;
        [SerializeField] private TextMeshProUGUI _effectsText;

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
            if (_blueLedButton != null)
                _blueLedButton.onClick.AddListener(() => OnLedButtonClicked(LedType.Blue));
            if (_redLedButton != null)
                _redLedButton.onClick.AddListener(() => OnLedButtonClicked(LedType.Red));
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
                
                // Light action gestita separatamente con OnLedButtonClicked
                
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
        /// Gestisce il click su un pulsante LED (Blue o Red)
        /// </summary>
        private void OnLedButtonClicked(LedType ledType)
        {
            Debug.Log($"[PotDetailsWidget] Click su pulsante LED {ledType} intercettato!");
            
            // Trova il vaso selezionato
            PotSlot selectedPot = FindSelectedPot();
            if (selectedPot == null || selectedPot.PotActions == null)
            {
                Debug.LogWarning("[PotDetailsWidget] Nessun vaso selezionato o PotActions mancante");
                return;
            }
            
            Debug.Log($"[PotDetailsWidget] Eseguendo {ledType} LED su vaso {selectedPot.PotId}");
            
            // Esegui l'azione LED con il tipo specificato
            bool success = selectedPot.PotActions.DoLight(ledType);
            
            if (success)
            {
                Debug.Log($"[PotDetailsWidget] {ledType} LED eseguito con successo!");
                // Aggiorna l'UI
                UpdateActionButtons(selectedPot);

                var growthController = selectedPot.GetComponent<PotGrowthController>();
                if (growthController != null)
                    UpdateStageAndProgressUI(selectedPot);
            }
            else
            {
                Debug.LogWarning($"[PotDetailsWidget] {ledType} LED fallito!");
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
            if (_blueLedButton != null)
                UpdateButtonState(_blueLedButton, pot.PotActions.CanLight(), "Blue LED");
            if (_redLedButton != null)
                UpdateButtonState(_redLedButton, pot.PotActions.CanLight(), "Red LED");
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
            int maxHydration = _currentSelectedPot?.PotActions?.GetMaxHydration() ?? 3;
            float hydrationPercentage = maxHydration > 0 ? (float)state.Hydration / maxHydration * 100f : 0f;
            
            if (_hydrationStressText != null)
            {
                // Assicurati che richText sia abilitato
                _hydrationStressText.richText = true;
                
                string hydrationText = $"<color=#CCCCCC>Hydration:</color> <color=#FFFF00>{hydrationPercentage:F0}%</color>";
                
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
                Debug.Log($"[PotDetailsWidget] Aggiornato Hydration: {hydrationPercentage:F0}%");
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
            
            // Aggiorna Light Stress (mostra percentuale e LED richiesto per lo stadio corrente)
            int maxLight = _currentSelectedPot?.PotActions?.GetMaxLightExposure() ?? 3;
            float lightPercentage = maxLight > 0 ? (float)state.LightExposure / maxLight * 100f : 0f;
            
            if (_lightStressText != null)
            {
                // Assicurati che richText sia abilitato
                _lightStressText.richText = true;
                
                string lightText = $"<color=#CCCCCC>Light Exposure:</color> <color=#FFFF00>{lightPercentage:F0}%</color>";
                
                // Se c'è una pianta, mostra anche il LED richiesto per lo stadio corrente
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
                                var requiredLed = stageReq.GetRequiredLed();
                                if (requiredLed.HasValue)
                                {
                                    // Mostra il LED richiesto per lo stadio corrente
                                    lightText += $" <color=#CCCCCC>(Required LED:</color> <color=#00FFFF>{requiredLed.Value}</color><color=#CCCCCC>)</color>";
                                }
                                else
                                {
                                    // Nessun LED richiesto
                                    lightText += $" <color=#CCCCCC>(No LED required)</color>";
                                }
                            }
                        }
                    }
                }
                
                _lightStressText.text = lightText;
                Debug.Log($"[PotDetailsWidget] Aggiornato Light Exposure: {lightPercentage:F0}%");
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
                    return $"Giorno {daysSincePlant} - {Mathf.Clamp(points, 0, _growthConfig?.pointsSeedToSprout ?? 4)}/{_growthConfig?.pointsSeedToSprout ?? 4} punti";
                case (int)PlantStage.Sprout:
                    return $"Giorno {daysSincePlant} - {Mathf.Clamp(points, 0, _growthConfig?.pointsSproutToMature ?? 4)}/{_growthConfig?.pointsSproutToMature ?? 4} punti";
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
    }
}