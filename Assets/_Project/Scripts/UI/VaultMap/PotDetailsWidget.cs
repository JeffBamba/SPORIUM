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
        
        private PotSlot currentSelectedPot;
        private PlantGrowthConfig growthConfig;
        private GameManager gameManager;
        
        private void Awake()
        {
            LoadGrowthConfig();
            Initialize();
            Subscribes();
            
            _page.SetActive(false);
        }

        private void Update()
        {
            _page.SetActive(currentSelectedPot && currentSelectedPot.InRange);
        }
        
        private void Subscribes()
        {
            // Sottoscrivi agli eventi del sistema dei vasi
            PotSlot.OnPotSelected += OnPotSelected;
            PotEvents.OnPotStateChanged += OnPotStateChanged;
            PotEvents.OnPlantGrew += OnPlantGrew;
            PotEvents.OnPlantStageChanged += OnPlantStageChanged;
        }
        
        private void Initialize()
        {
            gameManager = FindObjectOfType<GameManager>();
            gameManager.OnDayChanged += HandleDayChanged;
            
            _plantButton.onClick.AddListener(() => OnActionButtonClicked(PotEvents.PotActionType.Plant));
            _wateringButton.onClick.AddListener(() => OnActionButtonClicked(PotEvents.PotActionType.Water));
            _lightButton.onClick.AddListener(() => OnActionButtonClicked(PotEvents.PotActionType.Light));
            _uprootButton.onClick.AddListener(() => OnActionButtonClicked(PotEvents.PotActionType.Uproot));
        }
        
        private void LoadGrowthConfig()
        {
            // Carica la configurazione di crescita
            growthConfig = Resources.Load<PlantGrowthConfig>("Configs/PlantGrowthConfig_Default");
            if (growthConfig == null)
            {
                Debug.LogWarning("[BLK-01.03B] PlantGrowthConfig non trovato in Resources/Configs/. Usando valori di default.");
                // Crea configurazione di fallback
                growthConfig = ScriptableObject.CreateInstance<PlantGrowthConfig>();
            }
        }

        private void HandleDayChanged(int obj)
        { 
            if (!currentSelectedPot)
                return;
        
            UpdateActionButtons(currentSelectedPot);
            UpdateStageAndProgressUI(currentSelectedPot);
        }

        private void OnDestroy()
        {
            // Annulla sottoscrizioni
            PotSlot.OnPotSelected -= OnPotSelected;
            PotEvents.OnPotStateChanged -= OnPotStateChanged;
            PotEvents.OnPlantGrew -= OnPlantGrew;
            PotEvents.OnPlantStageChanged -= OnPlantStageChanged;
        }
        
        private void OnPotSelected(PotSlot pot)
        {
            Debug.Log($"[BLK-01.03B] Vaso {pot.PotId} selezionato. Aggiornamento UI...");
            Debug.Log($"[BLK-01.03B] PotActions presente: {pot.PotActions != null}");
            Debug.Log($"[BLK-01.03B] Player in range: {pot.InRange}");
        
            // Salva il vaso selezionato corrente
            currentSelectedPot = pot;
        
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
                    success = selectedPot.PotActions.DoPlant();
                    break;
                case PotEvents.PotActionType.Water:
                    success = selectedPot.PotActions.DoWater();
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
        private PotSlot FindSelectedPot()
        {
            // Trova il vaso che ha emesso l'evento OnPotSelected
            // Usa il sistema di eventi per tracciare la selezione
            PotSlot[] allPots = FindObjectsOfType<PotSlot>();
            foreach (PotSlot pot in allPots)
            {
                if (pot.PotActions != null && pot.IsSelected)
                {
                    Debug.Log($"[PotHUDWidget] Trovato vaso selezionato: {pot.PotId}");
                    return pot;
                }
            }
        
            // Fallback: cerca il primo vaso con PotActions
            foreach (PotSlot pot in allPots)
            {
                if (pot.PotActions != null)
                {
                    Debug.LogWarning($"[PotHUDWidget] Fallback: usando primo vaso disponibile {pot.PotId}");
                    return pot;
                }
            }
        
            Debug.LogError("[PotHUDWidget] Nessun vaso trovato!");
            return null;
        }
        
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
        
            _page.SetActive(pot.InRange);
            
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
            if (!currentSelectedPot || currentSelectedPot.PotId != potId)
                return;
        
            Debug.Log($"[BLK-01.03B] Pianta cresciuta su {potId}: {oldPoints} → {newPoints} punti. Aggiornamento progress bar...");
            UpdateStageAndProgressUI(currentSelectedPot);
        }
        
        /// <summary>
        /// BLK-01.03B: Gestisce l'evento OnPlantStageChanged
        /// </summary>
        private void OnPlantStageChanged(string potId, PlantStage stage)
        {
            if (!currentSelectedPot || currentSelectedPot.PotId != potId)
                return;
        
            Debug.Log($"[BLK-01.03B] Stadio cambiato su {potId}: {stage}. Aggiornamento UI...");
            UpdateStageAndProgressUI(currentSelectedPot);
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
                _stageImage.color = GetStageColor(state.Stage);
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
            bool hadHydration = (state.LastWateredDay == gameManager.CurrentDay);
            bool hadLight = (state.LastLitDay == gameManager.CurrentDay);

            points += (hadHydration, hadLight) switch
            {
                (true, true)   => growthConfig.pointsIdealCare,
                (true, false)  => growthConfig.pointsPartialCare,
                (false, true)  => growthConfig.pointsPartialCare,
                (false, false) => growthConfig.pointsNoCare
            };

            return points;
        }
        
         private float CalculateProgressPercentage(PotStateModel state)
        {
            if (!growthConfig) 
                return 0f;
    
            int points = CalculateCurrentGrowthPoints(state);
            
            switch (state.Stage)
            {
                case (int)PlantStage.Empty:
                    return 0f; // Nessun progresso per vasi vuoti
                    
                case (int)PlantStage.Seed:
                    if (points >= growthConfig.pointsSeedToSprout)
                        return 100f; // Pronto per avanzare
                    return (float)points / growthConfig.pointsSeedToSprout * 100f;
                    
                case (int)PlantStage.Sprout:
                    if (points >= growthConfig.pointsSproutToMature)
                        return 100f; // Pronto per avanzare
                    return (float)points / growthConfig.pointsSproutToMature * 100f;
                    
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
            if (state.IsEmpty)
            {
                return "Pronto per piantare";
            }

            int points = CalculateCurrentGrowthPoints(state);
            int daysSincePlant = state.DaysSincePlant + 1;
        
            switch (state.Stage)
            {
                case (int)PlantStage.Seed:
                    return $"Giorno {daysSincePlant} - {Mathf.Clamp(points, 0, 2)}/2 punti";
                case (int)PlantStage.Sprout:
                    return $"Giorno {daysSincePlant} - {Mathf.Clamp(points, 0, 3)}/3 punti";
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
    }
}