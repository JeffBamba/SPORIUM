using UnityEngine;
using UnityEngine.UIElements;
using Sporae.Dome.PotSystem.Growth;
using Sporae.Dome.PotSystem.Condition;
using Sporae.Dome.PotSystem.Mold;
using Sporae.UI.UIToolkit.PlantCard.Components;
using Sporae.UI.UIToolkit.PlantCard.Helpers;
using _Project;
using _Project.Sporae.Core;
using System.IO;

namespace Sporae.UI.UIToolkit.PlantCard
{
    /// <summary>
    /// Helper per data binding pulito tra PotStateModel/PlantData e UI elements.
    /// Separazione netta tra logica e presentazione.
    /// </summary>
    public class PlantCardV2DataBinder
    {
        private VisualElement _root;
        private PlantCardV2Config _config;
        private PhSystem _phSystem;
        private PotSystemConfig _potSystemConfig;
        private MoldConfig _moldConfig;
        private PlantGrowthConfig _growthConfig;
        private DayCycleSystem _dayCycleSystem;
        
        // UI Elements - Header
        private Label _specimenIdLabel;
        private Label _plantNameLabel;
        private Label _plantSubtitleLabel;
        private Label _plantDescriptionLabel;
        private VisualElement _conditionBadge;
        private Label _conditionValueLabel;
        private VisualElement _conditionsBadge; // conditions_bagde (typo nel UXML) - deve mostrare Conditions, non Growth Stage
        private Label _conditionsBadgeText; // growth-stage-text dentro conditions_bagde
        private VisualElement _growthStageBadge;
        private Label _growthStageTextLabel;
        private VisualElement _growthProgressBar;
        private Label _growthCounterLabel;
        
        // UI Elements - Left Column
        private VisualElement _plantImage;
        private VisualElement _liveIndicator;
        private Label _phDriftValueLabel;
        private Label _ledCompatibleLabel;  // BLK-02.08: LED Compatibile (Blue/Red/ALL)
        private Button _plantButton;
        private Button _removeButton;
        
        // UI Elements - Vital Parameters
        private VitalParameterBox _hydrationBox;
        private VitalParameterBox _fertilizerBox;
        private VitalParameterBox _lightStressBox;
        private VitalParameterBox _condizioneBox;
        private VitalParameterBox _moldRiskBox;
        private VitalParameterBox _phAffinityBox;
        private Label _activePowerNameLabel;
        private VisualElement _activePowerList;
        private Label _fruitCyclesValueLabel;
        
        // UI Elements - Control Panels
        private RotaryKnobUI _irrigationKnob;
        private RotaryKnobUI _illuminazioneKnob;
        
        // UI Elements - Diary
        private PlantDiaryNotes _diaryNotes;
        
        // Callback references per rimuoverli quando necessario
        private EventCallback<MouseEnterEvent> _conditionsBadgeMouseEnterCallback;
        private EventCallback<MouseLeaveEvent> _conditionsBadgeMouseLeaveCallback;
        private EventCallback<MouseEnterEvent> _growthProgressBarMouseEnterCallback;
        private EventCallback<MouseLeaveEvent> _growthProgressBarMouseLeaveCallback;
        
        // FIX FLICKERING: Flag per tracciare se il tooltip deve essere nascosto
        private bool _shouldHideConditionTooltip = false;
        private bool _shouldHideGrowthTooltip = false;
        
        // BUG FIX: Callback per recuperare lo stato corrente quando i tooltip vengono mostrati
        private System.Func<(PotStateModel state, PlantData plantData)?> _getCurrentState;
        
        public PlantCardV2DataBinder(VisualElement root, PlantCardV2Config config)
        {
            _root = root;
            _config = config;
            
            // Ottieni sistemi
            _phSystem = ServiceContainer.Instance?.Get<PhSystem>(suppressWarning: true);
            _potSystemConfig = Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");
            _moldConfig = Resources.Load<MoldConfig>("Configs/MoldConfig");
            
            // Carica PlantGrowthConfig per il tooltip (come PotDetailsWidget)
            _growthConfig = Resources.Load<PlantGrowthConfig>("Configs/PlantGrowthConfig_Default");
            if (_growthConfig == null)
            {
                _growthConfig = Resources.Load<PlantGrowthConfig>("Configs/PlantGrowthConfig");
            }
            if (_growthConfig == null)
            {
                _growthConfig = ScriptableObject.CreateInstance<PlantGrowthConfig>();
            }
            
            // Ottieni DayCycleSystem per il tooltip
            _dayCycleSystem = ServiceContainer.Instance?.Get<DayCycleSystem>();
            
            InitializeUIElements();
        }
        
        private void InitializeUIElements()
        {
            // Header
            _specimenIdLabel = _root.Q<Label>("specimen-id");
            _plantNameLabel = _root.Q<Label>("plant-name");
            _plantSubtitleLabel = _root.Q<Label>("plant-subtitle");
            _plantDescriptionLabel = _root.Q<Label>("plant-description");
            _conditionBadge = _root.Q<VisualElement>("condition-badge");
            _conditionValueLabel = _root.Q<Label>("condition-value");
            
            // conditions_bagde (typo nel UXML) - deve mostrare Conditions, non Growth Stage
            _conditionsBadge = _root.Q<VisualElement>("conditions_bagde");
            if (_conditionsBadge != null)
            {
                _conditionsBadgeText = _conditionsBadge.Q<Label>("growth-stage-text");
            }
            
            // BUG3 FIX: L'elemento si chiama "crescita-stage-badge" nell'UXML, non "growth-stage-badge"
            _growthStageBadge = _root.Q<VisualElement>("crescita-stage-badge");
            if (_growthStageBadge != null)
            {
                _growthStageTextLabel = _growthStageBadge.Q<Label>("growth-stage-text");
            }
            _growthProgressBar = _root.Q<VisualElement>("growth-progress-bar");
            _growthCounterLabel = _root.Q<Label>("growth-counter");
            
            // Left Column
            _plantImage = _root.Q<VisualElement>("plant-image");
            _liveIndicator = _root.Q<VisualElement>("live-indicator");
            _phDriftValueLabel = _root.Q<Label>("ph-drift-value");
            _ledCompatibleLabel = _root.Q<Label>("led-compatible-value");  // BLK-02.08
            _plantButton = _root.Q<Button>("plant-button");
            _removeButton = _root.Q<Button>("remove-button");
            
            // Vital Parameters - Inizializza componenti
            var hydrationContainer = _root.Q<VisualElement>("parameter-hydration");
            if (hydrationContainer != null)
            {
                _hydrationBox = new VitalParameterBox(hydrationContainer, VitalParameterBox.ParameterType.Hydration, _config);
            }
            
            var fertilizerContainer = _root.Q<VisualElement>("parameter-fertilizer");
            if (fertilizerContainer != null)
            {
                _fertilizerBox = new VitalParameterBox(fertilizerContainer, VitalParameterBox.ParameterType.Fertilizer, _config);
            }
            
            // BUG FIX: Nomi corretti dal UXML
            var lightStressContainer = _root.Q<VisualElement>("parameter-lightstress");
            if (lightStressContainer != null)
            {
                _lightStressBox = new VitalParameterBox(lightStressContainer, VitalParameterBox.ParameterType.LightStress, _config);
            }
            
            var condizioneContainer = _root.Q<VisualElement>("parameter-conditions");
            if (condizioneContainer != null)
            {
                _condizioneBox = new VitalParameterBox(condizioneContainer, VitalParameterBox.ParameterType.Condizione, _config);
            }
            
            var moldRiskContainer = _root.Q<VisualElement>("parameter-mold");
            if (moldRiskContainer != null)
            {
                _moldRiskBox = new VitalParameterBox(moldRiskContainer, VitalParameterBox.ParameterType.MoldRisk, _config);
            }
            
            var phAffinityContainer = _root.Q<VisualElement>("parameter-affinity");
            if (phAffinityContainer != null)
            {
                _phAffinityBox = new VitalParameterBox(phAffinityContainer, VitalParameterBox.ParameterType.PhAffinity, _config);
            }
            
            _activePowerNameLabel = _root.Q<Label>("active-power-name");
            _activePowerList = _root.Q<VisualElement>("active-power-list");
            _fruitCyclesValueLabel = _root.Q<Label>("fruit-cycles-value");
            
            // Rotary Knobs - Cerca per nome "rotary-knob" all'interno dei container
            var irrigationContainer = _root.Q<VisualElement>("irrigation-container");
            var irrigationKnobElement = irrigationContainer != null ? irrigationContainer.Q<VisualElement>("rotary-knob") : null;
            if (irrigationKnobElement != null)
            {
                _irrigationKnob = new RotaryKnobUI(irrigationKnobElement, RotaryKnobUI.KnobType.Irrigation, _config);
            }
            
            var illuminazioneContainer = _root.Q<VisualElement>("illuminazione-container");
            var illuminazioneKnobElement = illuminazioneContainer != null ? illuminazioneContainer.Q<VisualElement>("rotary-knob") : null;
            if (illuminazioneKnobElement != null)
            {
                _illuminazioneKnob = new RotaryKnobUI(illuminazioneKnobElement, RotaryKnobUI.KnobType.Illuminazione, _config);
            }
            
            // Diary
            var notesList = _root.Q<ScrollView>("notes-list");
            var addNotePanel = _root.Q<VisualElement>("add-note-panel");
            if (notesList != null && addNotePanel != null)
            {
                _diaryNotes = new PlantDiaryNotes(notesList, addNotePanel);
            }
        }
        
        /// <summary>
        /// Binding completo di tutti i dati
        /// </summary>
        public void BindAllData(PotStateModel state, PlantData plantData, Sprite plantSprite = null)
        {
            BindHeaderData(state, plantData);
            BindPlantPreview(state, plantData, plantSprite);
            BindVitalParameters(state, plantData);
            BindControlPanels(state);
            BindDiaryTab(state, plantData);
        }
        
        /// <summary>
        /// BUG FIX: Imposta callback per recuperare lo stato corrente quando i tooltip vengono mostrati
        /// </summary>
        public void SetStateGetter(System.Func<(PotStateModel state, PlantData plantData)?> getter)
        {
            _getCurrentState = getter;
        }
        
        /// <summary>
        /// Binding header section
        /// </summary>
        public void BindHeaderData(PotStateModel state, PlantData plantData)
        {
            if (state == null) return;
            
            // Specimen ID - DEBUG_SAFE_FIX: Usa PlantCode invece di PotId
            if (_specimenIdLabel != null)
            {
                // Usa PlantCode se disponibile, altrimenti fallback a PotId formattato
                string specimenId = !string.IsNullOrEmpty(state.PlantCode) ? state.PlantCode : PlantCardFormatters.FormatSpecimenId(state.PotId);
                _specimenIdLabel.text = specimenId;
            }
            
            var conditionLabel = _root.Q<Label>("condition-label");
            var conditionValue = _root.Q<Label>("condition-value");
            
            // Plant Name - DEBUG_SAFE_FIX: Usa nome pianta invece di PlantCode
            if (_plantNameLabel != null)
            {
                string plantName = GetPlantDisplayName(plantData);
                _plantNameLabel.text = plantName;
            }
            
            // Subtitle (Family · Growth Stage · Level)
            if (_plantSubtitleLabel != null)
            {
                if (plantData != null)
                {
                    PlantStage stage = (PlantStage)state.Stage;
                    string subtitle = PlantCardFormatters.FormatPlantSubtitle(plantData.Family, stage, state.PlantLevel);
                    _plantSubtitleLabel.text = subtitle;
                }
                else
                {
                    _plantSubtitleLabel.text = "Unknown · Unknown · Level 1";
                }
            }
            
            // Description
            if (_plantDescriptionLabel != null)
            {
                _plantDescriptionLabel.text = plantData?.Description ?? "Nessuna descrizione disponibile";
            }
            
            // Rarity (se disponibile)
            if (plantData != null)
            {
                var rarityLabel = _root.Q<Label>("rarity-label");
                if (rarityLabel != null)
                {
                    rarityLabel.text = PlantCardFormatters.FormatRarity(plantData.Rarity);
                }
            }
            
            // Condition - BUG3 FIX: condition-badge deve mostrare Conditions, non Growth Stage
            BindCondition(state, plantData);
            
            // Growth Stage
            BindGrowthStage(state, plantData);
            
            // BUG C FIX: Se condition-label dice "CICLI COMPLETI", condition-value deve SEMPRE mostrare i cicli completi
            // La condizione viene mostrata da conditions_badge invece
            if (conditionValue != null && conditionLabel != null && conditionLabel.text.Contains("CICLI"))
            {
                // Mostra sempre i cicli completi quando il label dice "CICLI COMPLETI"
                conditionValue.text = state.CompletedCycles.ToString();
            }
            else if (conditionValue != null && state.HasPlant && plantData != null)
            {
                // Fallback: se condition-label NON dice "CICLI", mostra la percentuale di condizione
                ConditionResult conditionResult = PlantConditionSystem.CalculateCondition(
                    state,
                    plantData,
                    _phSystem,
                    _potSystemConfig,
                    ServiceContainer.Instance?.Get<DayCycleSystem>()?.CurrentDay ?? 1,
                    state.PreviousDayConditionScore
                );
                int conditionPercent = PlantCardCalculators.CalculateConditionPercent(conditionResult.Score);
                conditionValue.text = $"{conditionPercent}%";
            }
        }
        
        /// <summary>
        /// Calcola condizione usando lo stesso metodo di PotDetailsWidget.UpdateConditionUI
        /// METODO CENTRALE: tutti i punti UI devono usare questo metodo per garantire coerenza
        /// </summary>
        private (ConditionResult result, string conditionName) CalculateConditionForUI(PotStateModel state, PlantData plantData)
        {
            // BUG FIX: Calcola SEMPRE la condizione, non usare mai state.ConditionLabel (che è solo un valore pre-settato al giorno 1)
            if (state == null || !state.HasPlant || plantData == null)
            {
                // Solo se state è null, usa un fallback
                return (new ConditionResult(50, PlantCondition.Sana, ForecastDirection.Stable, 0, new ConditionContributor[0]), "Sana");
            }
            
            // Determina quali sistemi usare (preferisci i campi privati, altrimenti recupera dal ServiceContainer)
            PhSystem phSystemToUse = _phSystem ?? ServiceContainer.Instance?.Get<PhSystem>(suppressWarning: true);
            PotSystemConfig potConfigToUse = _potSystemConfig ?? Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");
            
            // Se i sistemi non sono disponibili, usa un fallback (non usare state.ConditionLabel)
            if (phSystemToUse == null || potConfigToUse == null)
            {
                return (new ConditionResult(50, PlantCondition.Sana, ForecastDirection.Stable, 0, new ConditionContributor[0]), "Sana");
            }
            
            int currentDay = _dayCycleSystem?.CurrentDay ?? 1;
            // BUG FIX: Usa lo stesso fallback di DayCycleController e PotDetailsWidget quando PreviousDayConditionScore è -1
            int previousDayScore = state.PreviousDayConditionScore >= 0 ? state.PreviousDayConditionScore : state.ConditionScore;
            
            ConditionResult result = PlantConditionSystem.CalculateCondition(
                state,
                plantData,
                phSystemToUse,
                potConfigToUse,
                currentDay,
                previousDayScore);
            
            bool isOverwatering = PlantConditionSystem.IsOverwatering(state, potConfigToUse.MaxHydration);
            string conditionName = PlantConditionSystem.GetConditionName(result.Condition, isOverwatering);
            
            return (result, conditionName);
        }
        
        /// <summary>
        /// Binding condizione (usa PlantConditionSystem)
        /// </summary>
        private void BindCondition(PotStateModel state, PlantData plantData)
        {
            // BUG A FIX: Se non c'è pianta, nascondi il tooltip se esiste e esci
            if (state == null || !state.HasPlant)
            {
                // Nascondi il tooltip se esiste per evitare errori
                if (_conditionsBadge != null)
                {
                    var existingTooltip = _conditionsBadge.Q<VisualElement>("condition-tooltip");
                    if (existingTooltip != null)
                    {
                        existingTooltip.style.display = DisplayStyle.None;
                    }
                }
                return;
            }
            
            // Usa metodo centrale per calcolare condizione (stesso di PotDetailsWidget.UpdateConditionUI)
            var (conditionResult, conditionName) = CalculateConditionForUI(state, plantData);
            
            // BUG5 FIX: condition-value NON deve essere aggiornato qui se condition-label dice "CICLI COMPLETI"
            // condition-value viene aggiornato in BindHeaderData per mostrare i cicli completi
            // La condizione viene mostrata da conditions_bagde invece
            // Rimuoviamo l'aggiornamento di condition-value qui
            
            // BUG3 FIX: conditions_bagde (typo nel UXML) deve mostrare Conditions, non Growth Stage
            if (_conditionsBadgeText != null)
            {
                _conditionsBadgeText.text = conditionName;
            }
            
            // Aggiorna colori di conditions_bagde
            if (_conditionsBadge != null && _config != null)
            {
                Color conditionColor = PlantCardColorCalculator.GetConditionColor(conditionResult.Score, _config);
                _conditionsBadge.style.borderTopColor = conditionColor;
                _conditionsBadge.style.borderRightColor = conditionColor;
                _conditionsBadge.style.borderBottomColor = conditionColor;
                _conditionsBadge.style.borderLeftColor = conditionColor;
                _conditionsBadge.style.backgroundColor = new Color(conditionColor.r, conditionColor.g, conditionColor.b, 0.2f);
                
                // Aggiorna colore del testo
                if (_conditionsBadgeText != null)
                {
                    _conditionsBadgeText.style.color = conditionColor;
                }
            }
            
            if (_conditionBadge != null && _config != null)
            {
                Color conditionColor = PlantCardColorCalculator.GetConditionColor(conditionResult.Score, _config);
                _conditionBadge.style.borderTopColor = conditionColor;
                _conditionBadge.style.borderRightColor = conditionColor;
                _conditionBadge.style.borderBottomColor = conditionColor;
                _conditionBadge.style.borderLeftColor = conditionColor;
                _conditionBadge.style.backgroundColor = new Color(conditionColor.r, conditionColor.g, conditionColor.b, 0.2f);
            }
            
            // Aggiorna condizione box se esiste
            if (_condizioneBox != null)
            {
                _condizioneBox.UpdateValue(conditionResult.Score, 100);
            }
            
            // Setup tooltip condition
            SetupConditionTooltip(state, plantData, conditionResult);
        }
        
        /// <summary>
        /// Setup tooltip per condition badge
        /// MODIFICA: Il tooltip deve essere sul conditions_badge e mostrare le stesse informazioni del tooltip Growth della vecchia UI
        /// FIX FLICKERING: Disabilita picking sul tooltip e aggiungi delay per evitare flickering
        /// FIX POSITIONING: Posiziona il tooltip dentro conditions_bagde con background e posizionamento relativo
        /// </summary>
        private void SetupConditionTooltip(PotStateModel state, PlantData plantData, ConditionResult conditionResult)
        {
            // Il tooltip deve essere sul conditions_badge stesso
            if (_conditionsBadge == null)
                return;
            
            // BUG B FIX: Cerca il tooltip nel root invece che dentro conditions_bagde per evitare problemi di layering
            // Il tooltip deve essere nel root per essere sopra tutti gli altri elementi
            var conditionTooltip = _root.Q<VisualElement>("condition-tooltip-conditions-badge");
            var conditionTooltipText = conditionTooltip?.Q<Label>("condition-tooltip-text");
            
            // Se non esiste, crealo nel root
            if (conditionTooltip == null)
            {
                conditionTooltip = new VisualElement();
                conditionTooltip.name = "condition-tooltip-conditions-badge";
                conditionTooltip.AddToClassList("condition-tooltip");
                
                // FIX BACKGROUND: Aggiungi background scuro con bordi (colore #0d1519 con opacità 95%)
                conditionTooltip.style.backgroundColor = new Color(13f/255f, 21f/255f, 25f/255f, 0.95f); // #0d1519 con opacità 95%
                conditionTooltip.style.borderTopWidth = 1f;
                conditionTooltip.style.borderRightWidth = 1f;
                conditionTooltip.style.borderBottomWidth = 1f;
                conditionTooltip.style.borderLeftWidth = 1f;
                conditionTooltip.style.borderTopColor = new Color(0f, 0.8f, 0.4f, 1f); // Verde brillante
                conditionTooltip.style.borderRightColor = new Color(0f, 0.8f, 0.4f, 1f);
                conditionTooltip.style.borderBottomColor = new Color(0f, 0.8f, 0.4f, 1f);
                conditionTooltip.style.borderLeftColor = new Color(0f, 0.8f, 0.4f, 1f);
                
                // FIX POSITIONING: Posiziona in modo assoluto rispetto al root
                conditionTooltip.style.position = Position.Absolute;
                conditionTooltip.style.left = 0f;
                conditionTooltip.style.top = 0f;
                conditionTooltip.style.width = 450f;
                conditionTooltip.style.maxWidth = 450f;
                conditionTooltip.style.minHeight = 200f;
                conditionTooltip.style.paddingTop = 8f;
                conditionTooltip.style.paddingRight = 8f;
                conditionTooltip.style.paddingBottom = 8f;
                conditionTooltip.style.paddingLeft = 8f;
                
                // FIX LAYERING: Il tooltip sarà portato in primo piano quando viene mostrato
                
                // Aggiungi padding al testo
                conditionTooltipText = new Label();
                conditionTooltipText.name = "condition-tooltip-text";
                conditionTooltipText.AddToClassList("tooltip-text");
                conditionTooltipText.style.whiteSpace = WhiteSpace.Normal;
                conditionTooltipText.style.color = new Color(0.961f, 0.969f, 0.980f, 1f); // Bianco
                conditionTooltipText.style.fontSize = 12f;
                conditionTooltipText.style.unityTextAlign = TextAnchor.UpperLeft;
                conditionTooltipText.enableRichText = true;
                conditionTooltipText.style.marginTop = 4f;
                conditionTooltipText.style.marginRight = 4f;
                conditionTooltipText.style.marginBottom = 4f;
                conditionTooltipText.style.marginLeft = 4f;
                
                conditionTooltip.Add(conditionTooltipText);
                _root.Add(conditionTooltip); // Aggiungi al root invece che al badge
                
                // BUG A FIX: Nascondi inizialmente il tooltip per evitare che appaia prima che sia necessario
                conditionTooltip.style.display = DisplayStyle.None;
            }
            
            if (conditionTooltipText == null)
                return;
            
            // BUG A FIX: Assicurati che il tooltip sia nascosto se non c'è pianta
            if (state == null || !state.HasPlant || plantData == null)
            {
                conditionTooltip.style.display = DisplayStyle.None;
                return;
            }
            
            // FIX LAYERING: Assicurati che il tooltip sia sempre in primo piano quando viene aggiornato
            conditionTooltip.BringToFront();
            
            // FIX BACKGROUND: Aggiorna sempre il background per assicurarsi che sia visibile
            conditionTooltip.style.backgroundColor = new Color(13f/255f, 21f/255f, 25f/255f, 0.95f); // #0d1519 con opacità 95%
            
            // FIX FLICKERING: Disabilita picking sul tooltip per evitare che interferisca con gli eventi del badge
            conditionTooltip.pickingMode = PickingMode.Ignore;
            
            // Rimuovi callback precedenti per evitare duplicati
            if (_conditionsBadgeMouseEnterCallback != null)
            {
                _conditionsBadge.UnregisterCallback<MouseEnterEvent>(_conditionsBadgeMouseEnterCallback);
            }
            if (_conditionsBadgeMouseLeaveCallback != null)
            {
                _conditionsBadge.UnregisterCallback<MouseLeaveEvent>(_conditionsBadgeMouseLeaveCallback);
            }
            
            // BUG FIX: Non salvare lo stato in closure, recuperalo quando il tooltip viene mostrato
            // Setup hover events sul conditions_badge
            _conditionsBadgeMouseEnterCallback = evt => {
                // Cancella il flag di nascondere tooltip
                _shouldHideConditionTooltip = false;
                
                // BUG FIX: Recupera lo stato corrente quando il tooltip viene mostrato (non usare closure)
                var currentStateData = _getCurrentState?.Invoke();
                if (currentStateData.HasValue && currentStateData.Value.state != null && currentStateData.Value.plantData != null)
                {
                    string tooltipText = BuildGrowthTooltipForConditionsBadge(currentStateData.Value.state, currentStateData.Value.plantData);
                    conditionTooltipText.text = tooltipText;
                }
                
                // Mostra il tooltip prima di calcolare la posizione (per avere il layout corretto)
                conditionTooltip.style.display = DisplayStyle.Flex;
                
                // FIX LAYERING: Porta sempre il tooltip in primo piano quando viene mostrato
                conditionTooltip.BringToFront();
                
                // FIX BACKGROUND: Assicurati che il background sia sempre visibile
                conditionTooltip.style.backgroundColor = new Color(13f/255f, 21f/255f, 25f/255f, 0.95f); // #0d1519 con opacità 95%
                
                // FIX POSITIONING: Posiziona il tooltip in coordinate assolute rispetto al root (non al badge)
                conditionTooltip.schedule.Execute(() => {
                    // FIX LAYERING: Porta di nuovo in primo piano dopo il layout
                    conditionTooltip.BringToFront();
                    
                    // FIX BACKGROUND: Ri-applica sempre il background
                    conditionTooltip.style.backgroundColor = new Color(13f/255f, 21f/255f, 25f/255f, 0.95f);
                    
                    // BUG B FIX: Posiziona in coordinate assolute rispetto al root invece che relativamente al badge
                    var badgeWorldBounds = _conditionsBadge.worldBound;
                    var rootWorldBounds = _root.worldBound;
                    
                    float tooltipWidth = 450f;
                    float tooltipHeight = conditionTooltip.resolvedStyle.height > 0 ? conditionTooltip.resolvedStyle.height : 250f;
                    
                    // Calcola posizione assoluta rispetto al root
                    float tooltipX = badgeWorldBounds.xMin + (badgeWorldBounds.width - tooltipWidth) / 2f; // Centrato orizzontalmente rispetto al badge
                    float tooltipY = badgeWorldBounds.yMin - tooltipHeight - 10f; // Sopra il badge con margine di 10px
                    
                    // Converti da coordinate mondo a coordinate locali del root
                    float localX = tooltipX - rootWorldBounds.xMin;
                    float localY = tooltipY - rootWorldBounds.yMin;
                    
                    // Se non c'è spazio sopra, posiziona sotto
                    if (tooltipY < rootWorldBounds.yMin)
                    {
                        localY = (badgeWorldBounds.yMax - rootWorldBounds.yMin) + 10f; // Sotto il badge
                    }
                    
                    // Assicurati che il tooltip non esca dai bordi del root
                    if (localX + tooltipWidth > rootWorldBounds.width)
                    {
                        localX = rootWorldBounds.width - tooltipWidth - 10f;
                    }
                    if (localX < 0)
                    {
                        localX = 10f;
                    }
                    
                    // Imposta posizione assoluta rispetto al root
                    conditionTooltip.style.left = localX;
                    conditionTooltip.style.top = localY;
                });
            };
            
            _conditionsBadgeMouseLeaveCallback = evt => {
                // FIX FLICKERING: Usa un delay prima di nascondere il tooltip per evitare flickering
                // quando il mouse si muove rapidamente o quando il tooltip si sovrappone al badge
                _shouldHideConditionTooltip = true;
                conditionTooltip.schedule.Execute(() => {
                    if (_shouldHideConditionTooltip)
                    {
                        conditionTooltip.style.display = DisplayStyle.None;
                        _shouldHideConditionTooltip = false;
                    }
                }).ExecuteLater(100); // Delay di 100ms prima di nascondere
            };
            
            _conditionsBadge.RegisterCallback<MouseEnterEvent>(_conditionsBadgeMouseEnterCallback);
            _conditionsBadge.RegisterCallback<MouseLeaveEvent>(_conditionsBadgeMouseLeaveCallback);
            
            // FIX FLICKERING: Il tooltip ha pickingMode.Ignore, quindi non riceve eventi del mouse
            // Non possiamo usare eventi sul tooltip stesso, ma possiamo usare il delay sul MouseLeave del badge
        }
        
        /// <summary>
        /// Build growth tooltip text per conditions_badge (stessa logica di PotDetailsWidget.BuildGrowthTooltip)
        /// </summary>
        private string BuildGrowthTooltipForConditionsBadge(PotStateModel state, PlantData plantData)
        {
            var sb = new System.Text.StringBuilder();
            
            if (_growthConfig == null || state == null || state.IsEmpty || !state.HasPlant)
            {
                sb.AppendLine("<b>Crescita: Informazioni non disponibili</b>");
                return sb.ToString();
            }
            
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
            
            // Calcola percentuale idratazione (usa lo stesso metodo della HUD)
            int maxHydration = _potSystemConfig != null ? _potSystemConfig.MaxHydration : 10;
            int hydrationPercent = PlantCardCalculators.CalculateHydrationPercent(state.Hydration, maxHydration);
            
            // Verifica range per ogni parametro basato SOLO sui valori attuali
            bool waterOk = stageReq.IsHydrationInRange(hydrationPercent);
            
            // Light OK basato su stress percentage (0% = OK) invece di LightExposure
            int consecutiveDays = state.GetConsecutiveLedDays();
            int maxDaysForFullStress = _potSystemConfig != null ? _potSystemConfig.MaxDaysForFullStress : 5;
            float stressPercentage = Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f;
            int lightStressPercent = Mathf.RoundToInt(stressPercentage);
            
            // Light è OK se stress è tra 0% e 100% (esclusi gli estremi)
            bool lightOk = stressPercentage > 0f && stressPercentage < 100f;
            
            bool fertilizerOk = stageReq.IsFertilizerInRange(state.FertilizerLevel);
            
            // Usa metodo centrale per calcolare condizione (stesso di PotDetailsWidget.UpdateConditionUI)
            var (_, conditionName) = CalculateConditionForUI(state, plantData);
            
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
            
            // Light (mostra sempre Light Stress, come nella HUD)
            string lightStatus = lightOk ? "<color=#00FF00>OK</color>" : "<color=#FF0000>NON OK</color>";
            sb.AppendLine($"• <color=#FFD700>Luce</color>: {lightStatus}");
            sb.AppendLine($"  Range ideale: <color=#00FF00>{stageReq.lightMin}%-{stageReq.lightMed}%-{stageReq.lightMax}%</color>");
            sb.AppendLine($"  Attuale: {(lightOk ? $"<color=#00FF00>{lightStressPercent}%</color>" : $"{lightStressPercent}%")}");
            sb.AppendLine();
            
            // Fertilizer
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
        /// Build condition tooltip text (mantenuto per compatibilità, ma non più usato per conditions_badge)
        /// </summary>
        private string BuildConditionTooltipText(ConditionResult result)
        {
            var sb = new System.Text.StringBuilder();
            
            string conditionName = PlantConditionSystem.GetConditionName(result.Condition);
            string forecastSymbol = result.Forecast switch
            {
                Sporae.Dome.PotSystem.Condition.ForecastDirection.Up => "↑",
                Sporae.Dome.PotSystem.Condition.ForecastDirection.Down => "↓",
                _ => "→"
            };
            string forecastText = result.Forecast switch
            {
                Sporae.Dome.PotSystem.Condition.ForecastDirection.Up => "Tendenza positiva",
                Sporae.Dome.PotSystem.Condition.ForecastDirection.Down => "Tendenza negativa",
                _ => "Stabile"
            };
            
            sb.AppendLine($"Condizione: {conditionName} ({result.Score}/100) {forecastSymbol}");
            sb.AppendLine();
            
            // Contributi positivi
            var positiveContribs = System.Array.FindAll(result.Contributors, c => c.IsPositive);
            if (positiveContribs.Length > 0)
            {
                sb.AppendLine("Contributi positivi:");
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
                sb.AppendLine("Contributi negativi:");
                foreach (var contrib in negativeContribs)
                {
                    sb.AppendLine($"• {contrib.Source}: {contrib.Value}");
                }
                sb.AppendLine();
            }
            
            sb.AppendLine($"Forecast: {forecastText}. {(result.ScoreDelta != 0 ? $"Δ {result.ScoreDelta:+0;-0}" : "")}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Binding growth stage
        /// </summary>
        private void BindGrowthStage(PotStateModel state, PlantData plantData)
        {
            
            if (state == null || !state.HasPlant) return;
            
            PlantStage stage = (PlantStage)state.Stage;
            
            // Growth Stage Text
            if (_growthStageTextLabel != null)
            {
                _growthStageTextLabel.text = PlantCardFormatters.FormatGrowthStage(stage);
            }
            
            // Growth Progress Bar (7 segmenti)
            if (_growthProgressBar != null && plantData != null)
            {
                int maxDays = plantData.GetStageDurationDays(stage);
                int filledCount = PlantCardCalculators.CalculateGrowthProgressFilled(state.DaysInCurrentStage, maxDays, 7);
                
                // Aggiorna segmenti
                for (int i = 0; i < 7; i++)
                {
                    var segment = _growthProgressBar.Q<VisualElement>($"growth-segment-{i}");
                    if (segment != null)
                    {
                        if (i < filledCount)
                        {
                            segment.style.backgroundColor = _config?.VioletGrowth ?? Color.magenta;
                        }
                        else
                        {
                            segment.style.backgroundColor = new Color(181f/255f, 128f/255f, 209f/255f, 0.3f);
                        }
                    }
                }
            }
            
            // Growth Counter
            if (_growthCounterLabel != null && plantData != null)
            {
                int maxDays = plantData.GetStageDurationDays(stage);
                _growthCounterLabel.text = $"{state.DaysInCurrentStage}/{maxDays}";
            }
            
            // Setup tooltip growth
            SetupGrowthTooltip(state, plantData);
        }
        
        /// <summary>
        /// Setup tooltip per growth progress bar
        /// </summary>
        private void SetupGrowthTooltip(PotStateModel state, PlantData plantData)
        {
            if (_growthProgressBar == null)
                return;
            
            // Cerca il tooltip nel root (creato dinamicamente)
            var growthTooltip = _root.Q<VisualElement>("growth-tooltip-dynamic");
            var growthTooltipText = growthTooltip?.Q<Label>("growth-tooltip-text");
            
            // Se non esiste, crealo nel root
            if (growthTooltip == null)
            {
                growthTooltip = new VisualElement();
                growthTooltip.name = "growth-tooltip-dynamic";
                growthTooltip.AddToClassList("growth-tooltip");
                
                // Stili identici al tooltip Conditions
                growthTooltip.style.backgroundColor = new Color(13f/255f, 21f/255f, 25f/255f, 0.95f); // #0d1519 con opacità 95%
                growthTooltip.style.borderTopWidth = 1f;
                growthTooltip.style.borderRightWidth = 1f;
                growthTooltip.style.borderBottomWidth = 1f;
                growthTooltip.style.borderLeftWidth = 1f;
                // Border viola per coerenza con growth stage
                Color violetBorder = _config?.VioletGrowth ?? new Color(181f/255f, 128f/255f, 209f/255f, 1f);
                growthTooltip.style.borderTopColor = violetBorder;
                growthTooltip.style.borderRightColor = violetBorder;
                growthTooltip.style.borderBottomColor = violetBorder;
                growthTooltip.style.borderLeftColor = violetBorder;
                
                // Posizionamento assoluto rispetto al root
                growthTooltip.style.position = Position.Absolute;
                growthTooltip.style.left = 0f;
                growthTooltip.style.top = 0f;
                growthTooltip.style.width = 450f;
                growthTooltip.style.maxWidth = 450f;
                growthTooltip.style.minHeight = 200f;
                growthTooltip.style.paddingTop = 8f;
                growthTooltip.style.paddingRight = 8f;
                growthTooltip.style.paddingBottom = 8f;
                growthTooltip.style.paddingLeft = 8f;
                
                // Aggiungi label per il testo
                growthTooltipText = new Label();
                growthTooltipText.name = "growth-tooltip-text";
                growthTooltipText.AddToClassList("tooltip-text");
                growthTooltipText.style.whiteSpace = WhiteSpace.Normal;
                growthTooltipText.style.color = new Color(0.961f, 0.969f, 0.980f, 1f); // Bianco
                growthTooltipText.style.fontSize = 12f;
                growthTooltipText.style.unityTextAlign = TextAnchor.UpperLeft;
                growthTooltipText.enableRichText = true;
                growthTooltipText.style.marginTop = 4f;
                growthTooltipText.style.marginRight = 4f;
                growthTooltipText.style.marginBottom = 4f;
                growthTooltipText.style.marginLeft = 4f;
                
                growthTooltip.Add(growthTooltipText);
                _root.Add(growthTooltip); // Aggiungi al root
                
                // Nascondi inizialmente
                growthTooltip.style.display = DisplayStyle.None;
            }
            
            if (growthTooltipText == null)
                return;
            
            // Assicurati che il tooltip sia nascosto se non c'è pianta
            if (state == null || !state.HasPlant || plantData == null)
            {
                growthTooltip.style.display = DisplayStyle.None;
                return;
            }
            
            // Porta in primo piano
            growthTooltip.BringToFront();
            growthTooltip.style.backgroundColor = new Color(13f/255f, 21f/255f, 25f/255f, 0.95f);
            
            // Disabilita picking per evitare flickering
            growthTooltip.pickingMode = PickingMode.Ignore;
            
            // Rimuovi callback precedenti per evitare duplicati
            if (_growthProgressBarMouseEnterCallback != null)
            {
                _growthProgressBar.UnregisterCallback<MouseEnterEvent>(_growthProgressBarMouseEnterCallback);
            }
            if (_growthProgressBarMouseLeaveCallback != null)
            {
                _growthProgressBar.UnregisterCallback<MouseLeaveEvent>(_growthProgressBarMouseLeaveCallback);
            }
            
            // BUG FIX: Non salvare lo stato in closure, recuperalo quando il tooltip viene mostrato
            // Setup hover events
            _growthProgressBarMouseEnterCallback = evt => {
                _shouldHideGrowthTooltip = false;
                
                // BUG FIX: Recupera lo stato corrente quando il tooltip viene mostrato (non usare closure)
                var currentStateData = _getCurrentState?.Invoke();
                if (currentStateData.HasValue && currentStateData.Value.state != null && currentStateData.Value.plantData != null)
                {
                    string tooltipText = BuildGrowthTooltipText(currentStateData.Value.state, currentStateData.Value.plantData);
                    growthTooltipText.text = tooltipText;
                }
                
                // Mostra il tooltip
                growthTooltip.style.display = DisplayStyle.Flex;
                growthTooltip.BringToFront();
                growthTooltip.style.backgroundColor = new Color(13f/255f, 21f/255f, 25f/255f, 0.95f);
                
                // Posizionamento dinamico
                growthTooltip.schedule.Execute(() => {
                    growthTooltip.BringToFront();
                    growthTooltip.style.backgroundColor = new Color(13f/255f, 21f/255f, 25f/255f, 0.95f);
                    
                    var progressBarWorldBounds = _growthProgressBar.worldBound;
                    var rootWorldBounds = _root.worldBound;
                    
                    float tooltipWidth = 450f;
                    float tooltipHeight = growthTooltip.resolvedStyle.height > 0 ? growthTooltip.resolvedStyle.height : 250f;
                    
                    // Calcola posizione assoluta rispetto al root
                    float tooltipX = progressBarWorldBounds.xMin + (progressBarWorldBounds.width - tooltipWidth) / 2f; // Centrato orizzontalmente
                    float tooltipY = progressBarWorldBounds.yMin - tooltipHeight - 10f; // Sopra con margine di 10px
                    
                    // Converti da coordinate mondo a coordinate locali del root
                    float localX = tooltipX - rootWorldBounds.xMin;
                    float localY = tooltipY - rootWorldBounds.yMin;
                    
                    // Se non c'è spazio sopra, posiziona sotto
                    if (tooltipY < rootWorldBounds.yMin)
                    {
                        localY = (progressBarWorldBounds.yMax - rootWorldBounds.yMin) + 10f; // Sotto con margine
                    }
                    
                    // Assicurati che il tooltip non esca dai bordi
                    if (localX + tooltipWidth > rootWorldBounds.width)
                    {
                        localX = rootWorldBounds.width - tooltipWidth - 10f;
                    }
                    if (localX < 0)
                    {
                        localX = 10f;
                    }
                    
                    // Imposta posizione assoluta
                    growthTooltip.style.left = localX;
                    growthTooltip.style.top = localY;
                });
            };
            
            _growthProgressBarMouseLeaveCallback = evt => {
                // Delay prima di nascondere per evitare flickering
                _shouldHideGrowthTooltip = true;
                growthTooltip.schedule.Execute(() => {
                    if (_shouldHideGrowthTooltip)
                    {
                        growthTooltip.style.display = DisplayStyle.None;
                        _shouldHideGrowthTooltip = false;
                    }
                }).ExecuteLater(100); // Delay di 100ms
            };
            
            _growthProgressBar.RegisterCallback<MouseEnterEvent>(_growthProgressBarMouseEnterCallback);
            _growthProgressBar.RegisterCallback<MouseLeaveEvent>(_growthProgressBarMouseLeaveCallback);
        }
        
        /// <summary>
        /// Helper: Ottiene il prossimo stadio di crescita
        /// </summary>
        private PlantStage? GetNextStage(PlantStage currentStage)
        {
            return currentStage switch
            {
                PlantStage.Seed => PlantStage.Sprout,
                PlantStage.Sprout => PlantStage.Growth,
                PlantStage.Growth => PlantStage.Flowering,
                PlantStage.Flowering => PlantStage.HarvestReady,
                PlantStage.HarvestReady => PlantStage.Resting,
                PlantStage.Resting => null, // Ciclo completo o richiede fertilizzante
                _ => null
            };
        }
        
        /// <summary>
        /// Build growth tooltip text
        /// </summary>
        private string BuildGrowthTooltipText(PotStateModel state, PlantData plantData)
        {
            var sb = new System.Text.StringBuilder();
            
            if (state == null || !state.HasPlant || plantData == null)
            {
                sb.AppendLine("<b>Crescita: Informazioni non disponibili</b>");
                return sb.ToString();
            }
            
            PlantStage currentStage = (PlantStage)state.Stage;
            StageRequirements stageReq = plantData.GetStageRequirements(currentStage);
            
            if (stageReq == null)
            {
                sb.AppendLine("<b>Crescita: Requisiti stadio non disponibili</b>");
                return sb.ToString();
            }
            
            // Calcola percentuale idratazione (usa lo stesso metodo della HUD)
            int maxHydration = _potSystemConfig != null ? _potSystemConfig.MaxHydration : 10;
            int hydrationPercent = PlantCardCalculators.CalculateHydrationPercent(state.Hydration, maxHydration);
            
            bool waterOk = stageReq.IsHydrationInRange(hydrationPercent);
            
            // BUG A FIX: Light OK basato su stress percentage nel range (stessa logica del tooltip Conditions)
            int consecutiveDays = state.GetConsecutiveLedDays();
            int maxDaysForFullStress = _potSystemConfig != null ? _potSystemConfig.MaxDaysForFullStress : 5;
            float stressPercentage = Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f;
            int lightStressPercent = Mathf.RoundToInt(stressPercentage);
            // Light è OK se stress è nel range ottimale (stessa logica del tooltip Conditions)
            bool lightOk = stressPercentage > 0f && stressPercentage < 100f;
            
            // Fertilizzante: opzionale per Seed/Sprout
            bool fertilizerOk = false;
            if (currentStage == PlantStage.Seed || currentStage == PlantStage.Sprout)
            {
                fertilizerOk = stageReq.IsFertilizerInRange(state.FertilizerLevel) || state.FertilizerLevel == 0;
            }
            else
            {
                fertilizerOk = stageReq.IsFertilizerInRange(state.FertilizerLevel);
            }
            
            // Calcola punti e requisiti avanzamento
            int totalPoints = state.GrowthPointsWater + state.GrowthPointsLight + state.GrowthPointsFertilizer;
            int requiredPoints = (currentStage == PlantStage.Seed || currentStage == PlantStage.Sprout) ? 2 : 3;
            bool pointsOk = totalPoints >= requiredPoints;
            
            // Calcola giorni richiesti con modificatori
            PlantCondition currentCondition = (PlantCondition)state.ConditionLabel;
            int daysModifier = ConditionGrowthModifier.GetDaysModifier(currentCondition);
            int phDaysModifier = 0;
            if (_phSystem != null && plantData != null)
            {
                float currentPh = _phSystem.CurrentPh;
                if (plantData.IsPhInOptimalRange(currentPh))
                {
                    phDaysModifier = -1;
                }
            }
            int effectiveRequiredDays = stageReq.durationDays + daysModifier + phDaysModifier;
            bool durationOk = state.DaysInCurrentStage >= effectiveRequiredDays;
            
            // Giorni consecutivi ottimali richiesti
            int optimalDaysRequired = (currentStage == PlantStage.Seed) ? 1 : stageReq.durationDays;
            bool optimalDaysOk = state.DaysConsecutiveOptimal >= optimalDaysRequired;
            
            // Verifica blocchi avanzamento
            bool isBlockedByCondition = ConditionGrowthModifier.BlocksAdvancement(currentCondition);
            bool isBlockedByMold = state.MoldRiskLevel >= 2; // Severe o Critical
            
            // Calcola se può avanzare (BUG A FIX: usa lightOk invece di ledOk)
            bool canAdvance = !isBlockedByCondition && !isBlockedByMold &&
                             waterOk && lightOk && durationOk && optimalDaysOk && fertilizerOk && pointsOk;
            
            // Prossimo stadio
            PlantStage? nextStage = GetNextStage(currentStage);
            
            // Header: Stadio corrente e prossimo
            sb.AppendLine($"<b>Stadio Corrente: {PlantCardFormatters.FormatGrowthStage(currentStage)}</b>");
            if (nextStage.HasValue)
            {
                sb.AppendLine($"<b>Prossimo Stadio: {PlantCardFormatters.FormatGrowthStage(nextStage.Value)}</b>");
            }
            else
            {
                sb.AppendLine("<b>Prossimo Stadio: Ciclo completo</b>");
            }
            sb.AppendLine();
            
            // Progresso giorni
            int maxDaysDisplay = plantData.GetStageDurationDays(currentStage);
            sb.AppendLine($"<color=#00FFFF>Giorni nello stadio:</color> <color=#FFFFFF>{state.DaysInCurrentStage}/{maxDaysDisplay}</color> (richiesti: {effectiveRequiredDays})");
            if (daysModifier != 0 || phDaysModifier != 0)
            {
                string modifierText = "";
                if (daysModifier != 0) modifierText += $"condizione: {daysModifier:+0;-0}";
                if (phDaysModifier != 0)
                {
                    if (modifierText != "") modifierText += ", ";
                    modifierText += $"pH: {phDaysModifier:+0;-0}";
                }
                sb.AppendLine($"  <color=#888888>(modificatori: {modifierText})</color>");
            }
            sb.AppendLine();
            
            // Requisiti espliciti per avanzamento
            sb.AppendLine("<b>Requisiti per Avanzamento:</b>");
            sb.AppendLine();
            
            // Idratazione
            string waterStatus = waterOk ? "<color=#00FF00>✓ OK</color>" : "<color=#FF0000>✗ NON OK</color>";
            sb.AppendLine($"• <color=#3F6FFF>Idratazione:</color> {waterStatus}");
            sb.AppendLine($"  Range richiesto: <color=#FFFFFF>{stageReq.hydrationMin}%-{stageReq.hydrationMed}%-{stageReq.hydrationMax}%</color>");
            sb.AppendLine($"  Attuale: <color=#FFFFFF>{hydrationPercent}%</color>");
            sb.AppendLine();
            
            // Luce (BUG A FIX: mostra Light Stress invece di LED requirement)
            string lightStatus = lightOk ? "<color=#00FF00>✓ OK</color>" : "<color=#FF0000>✗ NON OK</color>";
            sb.AppendLine($"• <color=#FFD700>Luce:</color> {lightStatus}");
            sb.AppendLine($"  Range richiesto: <color=#FFFFFF>{stageReq.lightMin}%-{stageReq.lightMed}%-{stageReq.lightMax}%</color>");
            sb.AppendLine($"  Attuale: <color=#FFFFFF>{lightStressPercent}%</color>");
            sb.AppendLine();
            
            // Fertilizzante
            string fertilizerStatus = fertilizerOk ? "<color=#00FF00>✓ OK</color>" : "<color=#FF0000>✗ NON OK</color>";
            sb.AppendLine($"• <color=#9B59B6>Fertilizzante:</color> {fertilizerStatus}");
            if (currentStage == PlantStage.Seed || currentStage == PlantStage.Sprout)
            {
                sb.AppendLine($"  Range richiesto: <color=#FFFFFF>{stageReq.fertilizerMin}%-{stageReq.fertilizerMed}%-{stageReq.fertilizerMax}%</color> <color=#888888>(opzionale)</color>");
            }
            else
            {
                sb.AppendLine($"  Range richiesto: <color=#FFFFFF>{stageReq.fertilizerMin}%-{stageReq.fertilizerMed}%-{stageReq.fertilizerMax}%</color>");
            }
            sb.AppendLine($"  Attuale: <color=#FFFFFF>{state.FertilizerLevel}%</color>");
            sb.AppendLine();
            
            // Punti crescita
            string pointsStatus = pointsOk ? "<color=#00FF00>✓ OK</color>" : "<color=#FF0000>✗ NON OK</color>";
            sb.AppendLine($"• <color=#00FFFF>Punti Crescita:</color> {pointsStatus}");
            sb.AppendLine($"  Richiesti: <color=#FFFFFF>{requiredPoints}</color> (W:Water + L:Light{(requiredPoints == 3 ? " + F:Fertilizer" : "")})");
            sb.AppendLine($"  Attuali: <color=#FFFFFF>{totalPoints}</color> (W:{state.GrowthPointsWater} L:{state.GrowthPointsLight} F:{state.GrowthPointsFertilizer})");
            sb.AppendLine();
            
            // Giorni consecutivi ottimali
            string optimalDaysStatus = optimalDaysOk ? "<color=#00FF00>✓ OK</color>" : "<color=#FF0000>✗ NON OK</color>";
            sb.AppendLine($"• <color=#00FFFF>Giorni Consecutivi Ottimali:</color> {optimalDaysStatus}");
            sb.AppendLine($"  Richiesti: <color=#FFFFFF>{optimalDaysRequired}</color>");
            sb.AppendLine($"  Attuali: <color=#FFFFFF>{state.DaysConsecutiveOptimal}</color>");
            sb.AppendLine();
            
            // Stato avanzamento
            if (isBlockedByCondition)
            {
                sb.AppendLine("<color=#FF0000>⚠️ Avanzamento BLOCCATO: Condizione critica o appassita</color>");
            }
            else if (isBlockedByMold)
            {
                sb.AppendLine("<color=#FF0000>⚠️ Avanzamento BLOCCATO: Infestazione muffa grave</color>");
            }
            else if (canAdvance)
            {
                sb.AppendLine("<color=#00FF00>✓ Tutti i requisiti soddisfatti - Pronta per avanzare!</color>");
            }
            else
            {
                sb.AppendLine("<color=#FFAA00>⏳ Avanzamento in corso - Alcuni requisiti non ancora soddisfatti</color>");
                sb.AppendLine();
                sb.AppendLine("Requisiti mancanti:");
                if (!waterOk) sb.AppendLine("  • Idratazione fuori range");
                if (!lightOk) sb.AppendLine("  • Luce fuori range");
                if (!durationOk) sb.AppendLine($"  • Giorni insufficienti ({state.DaysInCurrentStage}/{effectiveRequiredDays})");
                if (!optimalDaysOk) sb.AppendLine($"  • Giorni ottimali consecutivi insufficienti ({state.DaysConsecutiveOptimal}/{optimalDaysRequired})");
                if (!fertilizerOk) sb.AppendLine("  • Fertilizzante fuori range");
                if (!pointsOk) sb.AppendLine($"  • Punti crescita insufficienti ({totalPoints}/{requiredPoints})");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Binding plant preview (left column)
        /// </summary>
        public void BindPlantPreview(PotStateModel state, PlantData plantData, Sprite plantSprite = null)
        {
            if (state == null) return;
            
            // Live Indicator
            if (_liveIndicator != null)
            {
                _liveIndicator.style.display = state.HasPlant ? DisplayStyle.Flex : DisplayStyle.None;
            }
            
            // pH Drift
            if (_phDriftValueLabel != null && plantData != null)
            {
                _phDriftValueLabel.text = PlantCardFormatters.FormatPhDrift(plantData.DailyPhDrift);
            }
            
            // BLK-02.08: LED Compatibile (mostra LED compatibili per famiglia)
            if (_ledCompatibleLabel != null && plantData != null)
            {
                LedCompatibility compatible = LedCompatibilityHelper.GetCompatibleLedTypes(plantData.Family);
                string displayText = LedCompatibilityHelper.GetCompatibleLedDisplay(compatible);
                _ledCompatibleLabel.text = displayText;
            }
            
            // BUG C FIX: Plant Image - mostra gli status visivi della pianta (vaso vuoto, sprout, etc)
            if (_plantImage != null)
            {
                if (plantSprite != null && plantSprite.texture != null)
                {
                    // Converti Sprite in Texture2D per UIToolkit
                    Texture2D spriteTexture = plantSprite.texture;
                    if (spriteTexture != null)
                    {
                        // Crea un StyleBackground da Texture2D
                        var background = new StyleBackground(spriteTexture);
                        _plantImage.style.backgroundImage = background;
                    }
                }
                else
                {
                    // Nessuno sprite disponibile - rimuovi background image
                    _plantImage.style.backgroundImage = StyleKeyword.None;
                }
            }
            
            // Plant Button (disabled se ha pianta)
            if (_plantButton != null)
            {
                _plantButton.SetEnabled(!state.HasPlant);
            }
        }
        
        /// <summary>
        /// Binding vital parameters tab
        /// </summary>
        public void BindVitalParameters(PotStateModel state, PlantData plantData)
        {
            if (state == null || !state.HasPlant) return;
            
            int maxHydration = _potSystemConfig != null ? _potSystemConfig.MaxHydration : 10;
            int maxLightExposure = _potSystemConfig != null ? _potSystemConfig.MaxLightExposure : 3;
            
            // Hydration
            if (_hydrationBox != null)
            {
                int hydrationPercent = PlantCardCalculators.CalculateHydrationPercent(state.Hydration, maxHydration);
                _hydrationBox.UpdateValue(hydrationPercent, 100); // Usa percentuale, non valore raw
                
                // Range info - BUG0 FIX: Usa StageRequirements invece di range fisso
                string rangeText = PlantCardCalculators.GetHydrationOptimalRangeText(plantData, state, _potSystemConfig);
                _hydrationBox.UpdateRangeInfo(rangeText);
            }
            
            // Fertilizer Level - BUG1.1 FIX: Aggiungi range info
            if (_fertilizerBox != null)
            {
                _fertilizerBox.UpdateValue(state.FertilizerLevel, 100);
                
                // Range info - BUG5 FIX: Usa StageRequirements invece di range fisso
                string rangeText = PlantCardCalculators.GetFertilizationOptimalRangeText(plantData, state);
                _fertilizerBox.UpdateRangeInfo(rangeText);
            }
            
            // Light Stress - BUG1 FIX: Calcola da GetConsecutiveLedDays invece di LightExposure
            if (_lightStressBox != null)
            {
                // BUG1 FIX: Light Stress deve essere calcolato da giorni consecutivi LED, non da LightExposure
                int consecutiveDays = state.GetConsecutiveLedDays();
                int maxDaysForFullStress = _potSystemConfig != null ? _potSystemConfig.MaxDaysForFullStress : 5;
                float stressPercentage = Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f;
                int lightStressPercent = Mathf.RoundToInt(stressPercentage);
                _lightStressBox.UpdateValue(lightStressPercent, 100); // Usa percentuale, non valore raw
                
                // Range info - BUG1 FIX: Usa StageRequirements invece di range fisso
                string rangeText = PlantCardCalculators.GetLightStressOptimalRangeText(plantData, state, _potSystemConfig);
                _lightStressBox.UpdateRangeInfo(rangeText);
            }
            
            // Condizione - BUG2 FIX: Assicurati che sia aggiornato correttamente
            if (_condizioneBox != null)
            {
                int conditionPercent = PlantCardCalculators.CalculateConditionPercent(state.ConditionScore);
                _condizioneBox.UpdateValue(state.ConditionScore, 100);
            }
            
            // Mold Risk - BUG3 FIX: Assicurati che sia collegato correttamente
            if (_moldRiskBox != null && _moldConfig != null && plantData != null)
            {
                int moldRiskLevel = MoldSystem.GetMoldRiskLevel(state, _phSystem, plantData, _moldConfig);
                // BUG3 FIX: Passa level (0-3) come value e 3 come max per la barra
                // La barra mostrerà level/3 * 10 segmenti (0-10 segmenti)
                _moldRiskBox.UpdateValue(moldRiskLevel, 3);
                
                // Range info - BUG3 FIX: Mostra range per mold risk (0-3)
                string rangeText = $"Range Ideale: 0 (None) - 1 (Mild) - 2 (Severe) - 3 (Critical)";
                _moldRiskBox.UpdateRangeInfo(rangeText);
                
                // Badge text
                var badgeLabel = _root.Q<Label>("mold-risk-badge");
                if (badgeLabel != null)
                {
                    badgeLabel.text = PlantCardColorCalculator.GetMoldRiskBadgeText(moldRiskLevel, _config);
                }
            }
            
            // pH Affinity - BUG4 FIX: Assicurati che sia collegato correttamente
            if (_phAffinityBox != null && plantData != null)
            {
                string phRange = PlantCardFormatters.FormatPhRange(plantData.OptimalPhMin, plantData.OptimalPhMax);
                // BUG4 FIX: pH Affinity mostra range, non valore singolo
                // Usa il valore minimo come base per la barra (0-100% mappato a range pH)
                int phMinInt = Mathf.RoundToInt(plantData.OptimalPhMin);
                int phMaxInt = Mathf.RoundToInt(plantData.OptimalPhMax);
                // Per la barra, mostra quanto il pH attuale è vicino al range ottimale
                float currentPh = _phSystem != null ? _phSystem.CurrentPh : 0f;
                // Calcola percentuale di affinità (0-100%) basata su distanza dal range
                float distanceFromOptimal = plantData.GetPhDistanceFromOptimal(currentPh);
                int affinityPercent = Mathf.RoundToInt((1f - distanceFromOptimal) * 100f);
                _phAffinityBox.UpdateValue(affinityPercent, 100);
                
                // Range info - BUG4 FIX: Mostra range pH ottimale
                string rangeText = $"Optimal PH Range: {phRange}";
                _phAffinityBox.UpdateRangeInfo(rangeText);
                
                // Aggiorna label valore con range
                var phValueLabel = _root.Q<Label>("ph-affinity-value");
                if (phValueLabel != null)
                {
                    phValueLabel.text = phRange;
                }
            }
            
            // Active Power
            if (_activePowerNameLabel != null && plantData != null)
            {
                _activePowerNameLabel.text = plantData.ActivePower ?? "Nessun potere attivo";
            }
            
            // Fruit Cycles
            if (_fruitCyclesValueLabel != null)
            {
                _fruitCyclesValueLabel.text = state.CompletedCycles.ToString();
            }
            
            // Growth Points
            var growthPointsValue = _root.Q<Label>("growth-points-value");
            if (growthPointsValue != null)
            {
                int totalPoints = state.GrowthPointsWater + state.GrowthPointsLight + state.GrowthPointsFertilizer;
                growthPointsValue.text = $"W:{state.GrowthPointsWater} L:{state.GrowthPointsLight} F:{state.GrowthPointsFertilizer} (Tot: {totalPoints}/3)";
            }
            
            // Optimal Days
            var optimalDaysValue = _root.Q<Label>("optimal-days-value");
            if (optimalDaysValue != null)
            {
                optimalDaysValue.text = state.DaysConsecutiveOptimal.ToString();
            }
        }
        
        /// <summary>
        /// Binding control panels (quick actions e manual systems)
        /// </summary>
        public void BindControlPanels(PotStateModel state)
        {
            if (state == null) return;
            
            // Irrigation Knob
            if (_irrigationKnob != null)
            {
                _irrigationKnob.SetIrrigationState(state.WateringSystemOn);
            }
            
            // Illuminazione Knob
            if (_illuminazioneKnob != null)
            {
                _illuminazioneKnob.SetLedState(state.LedSystemState);
            }
        }
        
        /// <summary>
        /// Ottiene il nome visualizzabile della pianta dal PlantData
        /// </summary>
        private string GetPlantDisplayName(PlantData plantData)
        {
            if (plantData == null)
                return "Pianta Sconosciuta";
            
            // Prova a ottenere il nome dal nome del PlantData (rimuovi prefisso PLT- e sostituisci - con spazi)
            // Esempio: "PLT-PURE-001" → "PURE 001" → "Pure 001" → "Arctic Hask" (se mappato)
            string plantName = plantData.name.Replace("PLT-", "").Replace("-", " ");
            
            // Mappa manuale per piante conosciute (basato su PlantCode)
            switch (plantData.PlantCode)
            {
                case "PLT-STD-001":
                    return "Ferric Fern";
                case "PLT-PURE-001":
                    return "Arctic Hask";
                case "PLT-EVIL-001":
                    return "Glasscap Fungus";
                default:
                    // Fallback: usa il nome formattato dal PlantData
                    return plantName;
            }
        }
        
        /// <summary>
        /// Binding diary tab
        /// </summary>
        public void BindDiaryTab(PotStateModel state, PlantData plantData)
        {
            if (state == null || _diaryNotes == null) return;
            
            // Carica note per questo pot
            _diaryNotes.LoadNotesForPot(state.PotId);
            
            // Aggiorna prodotti section
            UpdateProdottiSection(plantData);
        }
        
        /// <summary>
        /// Aggiorna sezione prodotti
        /// </summary>
        private void UpdateProdottiSection(PlantData plantData)
        {
            if (plantData == null) return;
            
            // Seeds
            var seedsTitle = _root.Q<Label>("prodotti-seeds-title");
            if (seedsTitle != null && plantData.SeedItemConfig != null)
            {
                seedsTitle.text = $"🌱 {plantData.SeedItemConfig.TypeId}";
            }
            
            // Nota: Fruit e Leaves sono placeholder per ora, da implementare quando disponibili in PlantData
        }
        
        /// <summary>
        /// Ottiene rotary knob per irrigazione
        /// </summary>
        public RotaryKnobUI GetIrrigationKnob()
        {
            return _irrigationKnob;
        }
        
        /// <summary>
        /// Ottiene rotary knob per illuminazione
        /// </summary>
        public RotaryKnobUI GetIlluminazioneKnob()
        {
            return _illuminazioneKnob;
        }
        
        /// <summary>
        /// Ottiene sistema note diario
        /// </summary>
        public PlantDiaryNotes GetDiaryNotes()
        {
            return _diaryNotes;
        }
    }
}

