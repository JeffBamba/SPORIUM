using System.Collections;
using _Project;
using _Project.Sporae.Core;
using _Project.UI.UIToolkit.VoOverlay;
using Sporae.DevTools;
using Sporae.Dome;
using Sporae.Dome.PotSystem.Condition;
using Sporae.Dome.PotSystem.Growth;
using Sporae.UI.UIToolkit.NotificationsFoundation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sporae.UI.UIToolkit.PlantCard4v
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class PlantCard4vController : MonoBehaviour
    {
        [Header("Binding")]
        [SerializeField] private string _potId = "POT-001";
        [SerializeField] private PotSlot _targetPot;
        [SerializeField] private PotSystemConfig _potSystemConfig;
        [SerializeField] private bool _openOnOwnPotSelected = true;
        [SerializeField] private bool _showOnStart;

        [Header("VO")]
        [SerializeField] private VoRegister _internalVoRegister = VoRegister.RegisterB;
        [SerializeField, Range(12f, 80f)] private float _voCharsPerSecond = 33f;

        private UIDocument _document;
        private VisualElement _root;
        private Label _potIdLabel;
        private Label _plantNameLabel;
        private Label _plantSubtitleLabel;
        private Label _speciesLabel;
        private Label _lifeStateLabel;
        private Label _stageDetailLabel;
        private Label _mainNeedLabel;
        private Label _mainNeedSubtitleLabel;
        private Label _hydrationLabel;
        private Label _phValueLabel;
        private Label _phAffinityLabel;
        private Label _fertilizerLabel;
        private Label _conditionLabel;
        private Label _conditionSummaryLabel;
        private Label _conditionPhAffinitySummaryLabel;
        private Label _conditionMoldSummaryLabel;
        private Label _preferredLightLabel;
        private Label _mainRiskLabel;
        private Label _riskCauseLabel;
        private Label _riskLevelLabel;
        private VisualElement _secondaryRiskItem;
        private Label _secondaryRiskTitleLabel;
        private Label _secondaryRiskCauseLabel;
        private Label _voTextLabel;
        private Label _footerStateLabel;
        private Label _shortIdLabel;
        private Label _plantGlyphLabel;
        private VisualElement _hydrationBar;
        private VisualElement _riskBar;
        private VisualElement _riskCalmBlock;
        private VisualElement _riskDetailBlock;
        private Button _closeButton;
        private Button _repeatVoButton;
        private Button _waterButton;
        private Button _lightRedButton;
        private Button _lightBlueButton;
        private Button _additiveButton;
        private Button _pruneButton;
        private Button _fertilizeButton;
        private Label _waterActionTitleLabel;
        private Label _waterActionSubtitleLabel;
        private Label _lightRedActionTitleLabel;
        private Label _lightRedActionSubtitleLabel;
        private Label _lightBlueActionTitleLabel;
        private Label _lightBlueActionSubtitleLabel;
        private Label _additiveActionTitleLabel;
        private Label _additiveActionSubtitleLabel;
        private Label _pruneActionTitleLabel;
        private Label _pruneActionSubtitleLabel;
        private Label _fertilizeActionTitleLabel;
        private Label _fertilizeActionSubtitleLabel;

        private DomePotRegistry _potRegistry;
        private PhSystem _phSystem;
        private VoOverlayController _voOverlay;
        private PlantCard4vCareViewModel _lastModel;
        private string _lastVoHintId;
        private int _lastVoDay = -1;
        private bool _isVisible;
        private bool _plantCardVoActive;
        private Coroutine _deferredRefreshRoutine;
        private bool _deferredRefreshPlayVo;
        private Coroutine _emptyPotToastRetryRoutine;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (_potSystemConfig == null)
                _potSystemConfig = Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");
            ResolveServices();
            BindUi();
            SetVisible(_showOnStart, playVo: _showOnStart);
        }

        private void OnEnable()
        {
            PotEvents.OnPotStateChanged += HandlePotStateChanged;
            PotEvents.OnPotAction += HandlePotAction;
            PotEvents.OnPotActionFailed += HandlePotActionFailed;
            PotEvents.OnPlantStageChanged += HandlePlantStageChanged;
            PotEvents.OnPlantDied += HandlePlantDied;
            PotEvents.OnPotSelected += HandlePotSelected;
            if (_isVisible)
                RequestRealtimeRefresh(playVo: false);
        }

        private void OnDisable()
        {
            PotEvents.OnPotStateChanged -= HandlePotStateChanged;
            PotEvents.OnPotAction -= HandlePotAction;
            PotEvents.OnPotActionFailed -= HandlePotActionFailed;
            PotEvents.OnPlantStageChanged -= HandlePlantStageChanged;
            PotEvents.OnPlantDied -= HandlePlantDied;
            PotEvents.OnPotSelected -= HandlePotSelected;
            if (_deferredRefreshRoutine != null)
            {
                StopCoroutine(_deferredRefreshRoutine);
                _deferredRefreshRoutine = null;
                _deferredRefreshPlayVo = false;
            }
            if (_emptyPotToastRetryRoutine != null)
            {
                StopCoroutine(_emptyPotToastRetryRoutine);
                _emptyPotToastRetryRoutine = null;
            }
            if (_isVisible)
                ApplyPlantCard4vPresentation(false);
        }

        private void Update()
        {
            if (!_isVisible)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                Hide();
        }

        public void Show()
        {
            SetVisible(true, playVo: true);
        }

        public void Hide()
        {
            SetVisible(false, playVo: false);
        }

        private void SetVisible(bool visible, bool playVo)
        {
            bool effectiveVisible = visible;
            if (effectiveVisible)
            {
                PotSlot pot = ResolveTargetPot();
                if (pot != null && IsPotWithoutCarePlant(pot))
                {
                    string id = string.IsNullOrWhiteSpace(pot.PotId) ? _potId : pot.PotId;
                    ShowEmptyPotToast(string.IsNullOrWhiteSpace(id) ? "POT" : id);
                    effectiveVisible = false;
                    playVo = false;
                }
            }

            _isVisible = effectiveVisible;
            if (_root != null)
                _root.style.display = effectiveVisible ? DisplayStyle.Flex : DisplayStyle.None;

            ApplyPlantCard4vPresentation(effectiveVisible);

            if (effectiveVisible)
            {
                Refresh(playVo);
            }
            else
            {
                if (_voTextLabel != null)
                    _voTextLabel.text = string.Empty;
            }
        }

        /// <summary>
        /// Allineato a PlantCard4vCareViewModel: nessun dato di cura senza pianta nel vaso.
        /// pot null = non ancora risolto: non blocca l'apertura.
        /// </summary>
        private static bool IsPotWithoutCarePlant(PotSlot pot)
        {
            if (pot == null)
                return false;

            PotStateModel state = pot.PotActions != null ? pot.PotActions.PotState : null;
            return state == null || state.IsEmpty || !state.HasPlant;
        }

        private void ApplyPlantCard4vPresentation(bool visible)
        {
            ResolveServices();

            if (visible)
            {
                // true = stesso comportamento "inventario": il pannello Notifications resta visibile
                // durante il modale, così il toast POT-EMPTY (vaso senza pianta) non viene emesso "al buio".
                GameplayUiModalLock.SetInventoryContextHudVisible(true);
                GameplayUiModalLock.SetMachineModalState(true, keepFixedHudVisible: true);
                _voOverlay?.SetPlantCard4vDocked(true);
                return;
            }

            if (_plantCardVoActive)
            {
                _voOverlay?.Hide();
                _plantCardVoActive = false;
            }

            _voOverlay?.SetPlantCard4vDocked(false);
            GameplayUiModalLock.SetInventoryContextHudVisible(false);
            GameplayUiModalLock.SetMachineModalState(false);
        }

        private void ResolveServices()
        {
            _potRegistry = ServiceContainer.Instance?.Get<DomePotRegistry>(suppressWarning: true);
            _phSystem = ServiceContainer.Instance?.Get<PhSystem>(suppressWarning: true);
            _voOverlay = ServiceContainer.Instance?.Get<VoOverlayController>(suppressWarning: true);
        }

        private void BindUi()
        {
            if (_document == null || _document.rootVisualElement == null)
                return;

            _root = _document.rootVisualElement.Q<VisualElement>("pcv4-root");
            _potIdLabel = _document.rootVisualElement.Q<Label>("pcv4-pot-id");
            _plantNameLabel = _document.rootVisualElement.Q<Label>("pcv4-plant-name");
            _plantSubtitleLabel = _document.rootVisualElement.Q<Label>("pcv4-plant-subtitle");
            _speciesLabel = _document.rootVisualElement.Q<Label>("pcv4-species-text");
            _lifeStateLabel = _document.rootVisualElement.Q<Label>("pcv4-life-state");
            _stageDetailLabel = _document.rootVisualElement.Q<Label>("pcv4-stage-detail");
            _mainNeedLabel = _document.rootVisualElement.Q<Label>("pcv4-main-need");
            _mainNeedSubtitleLabel = _document.rootVisualElement.Q<Label>("pcv4-main-need-subtitle");
            _hydrationLabel = _document.rootVisualElement.Q<Label>("pcv4-hydration-label");
            _phValueLabel = _document.rootVisualElement.Q<Label>("pcv4-ph-value");
            _phAffinityLabel = _document.rootVisualElement.Q<Label>("pcv4-ph-affinity");
            _fertilizerLabel = _document.rootVisualElement.Q<Label>("pcv4-fertilizer-value");
            _conditionLabel = _document.rootVisualElement.Q<Label>("pcv4-condition-value");
            _conditionSummaryLabel = _document.rootVisualElement.Q<Label>("pcv4-condition-summary-value");
            _conditionPhAffinitySummaryLabel = _document.rootVisualElement.Q<Label>("pcv4-condition-ph-affinity");
            _conditionMoldSummaryLabel = _document.rootVisualElement.Q<Label>("pcv4-condition-mold-risk");
            _preferredLightLabel = _document.rootVisualElement.Q<Label>("pcv4-preferred-light");
            _mainRiskLabel = _document.rootVisualElement.Q<Label>("pcv4-main-risk");
            _riskCauseLabel = _document.rootVisualElement.Q<Label>("pcv4-risk-cause");
            _riskLevelLabel = _document.rootVisualElement.Q<Label>("pcv4-risk-level");
            _secondaryRiskItem = _document.rootVisualElement.Q<VisualElement>("pcv4-secondary-risk-item");
            _secondaryRiskTitleLabel = _document.rootVisualElement.Q<Label>("pcv4-secondary-risk-title");
            _secondaryRiskCauseLabel = _document.rootVisualElement.Q<Label>("pcv4-secondary-risk-cause");
            _voTextLabel = _document.rootVisualElement.Q<Label>("pcv4-vo-text");
            _footerStateLabel = _document.rootVisualElement.Q<Label>("pcv4-footer-state");
            _shortIdLabel = _document.rootVisualElement.Q<Label>("pcv4-short-id");
            _plantGlyphLabel = _document.rootVisualElement.Q<Label>("pcv4-plant-glyph");
            _hydrationBar = _document.rootVisualElement.Q<VisualElement>("pcv4-hydration-bar");
            _riskBar = _document.rootVisualElement.Q<VisualElement>("pcv4-risk-bar");
            _riskCalmBlock = _document.rootVisualElement.Q<VisualElement>("pcv4-risk-calm-block");
            _riskDetailBlock = _document.rootVisualElement.Q<VisualElement>("pcv4-risk-detail-block");
            _closeButton = _document.rootVisualElement.Q<Button>("pcv4-close");
            _repeatVoButton = _document.rootVisualElement.Q<Button>("pcv4-repeat-vo");
            _waterButton = _document.rootVisualElement.Q<Button>("pcv4-action-water");
            _lightRedButton = _document.rootVisualElement.Q<Button>("pcv4-action-light-red");
            _lightBlueButton = _document.rootVisualElement.Q<Button>("pcv4-action-light-blue");
            _additiveButton = _document.rootVisualElement.Q<Button>("pcv4-action-additive");
            _pruneButton = _document.rootVisualElement.Q<Button>("pcv4-action-prune");
            _fertilizeButton = _document.rootVisualElement.Q<Button>("pcv4-action-fertilize");
            _waterActionTitleLabel = _document.rootVisualElement.Q<Label>("pcv4-action-water-title");
            _waterActionSubtitleLabel = _document.rootVisualElement.Q<Label>("pcv4-action-water-subtitle");
            _lightRedActionTitleLabel = _document.rootVisualElement.Q<Label>("pcv4-action-light-red-title");
            _lightRedActionSubtitleLabel = _document.rootVisualElement.Q<Label>("pcv4-action-light-red-subtitle");
            _lightBlueActionTitleLabel = _document.rootVisualElement.Q<Label>("pcv4-action-light-blue-title");
            _lightBlueActionSubtitleLabel = _document.rootVisualElement.Q<Label>("pcv4-action-light-blue-subtitle");
            _additiveActionTitleLabel = _document.rootVisualElement.Q<Label>("pcv4-action-additive-title");
            _additiveActionSubtitleLabel = _document.rootVisualElement.Q<Label>("pcv4-action-additive-subtitle");
            _pruneActionTitleLabel = _document.rootVisualElement.Q<Label>("pcv4-action-prune-title");
            _pruneActionSubtitleLabel = _document.rootVisualElement.Q<Label>("pcv4-action-prune-subtitle");
            _fertilizeActionTitleLabel = _document.rootVisualElement.Q<Label>("pcv4-action-fertilize-title");
            _fertilizeActionSubtitleLabel = _document.rootVisualElement.Q<Label>("pcv4-action-fertilize-subtitle");

            if (_closeButton != null)
                _closeButton.clicked += Hide;
            if (_repeatVoButton != null)
                _repeatVoButton.clicked += () => PlayVo(force: true);
            if (_waterButton != null)
                _waterButton.clicked += () => ExecuteAction(PlantCard4vActionKind.Water);
            if (_lightRedButton != null)
                _lightRedButton.clicked += ExecuteRedLightAction;
            if (_lightBlueButton != null)
                _lightBlueButton.clicked += ExecuteBlueLightAction;
            if (_additiveButton != null)
                _additiveButton.clicked += () => ExecuteAction(PlantCard4vActionKind.Additive);
            if (_pruneButton != null)
                _pruneButton.clicked += () => ExecuteAction(PlantCard4vActionKind.Prune);
            if (_fertilizeButton != null)
                _fertilizeButton.clicked += () => ExecuteAction(PlantCard4vActionKind.Fertilize);
        }

        private void Refresh(bool playVo)
        {
            ResolveServices();
            PotSlot pot = ResolveTargetPot();
            PotStateModel state = pot != null ? pot.PotActions?.PotState : null;
            PlantData plantData = state != null ? state.GetPlantData() : null;
            _lastModel = PlantCard4vCareViewModel.Build(pot, state, plantData, _potSystemConfig, _phSystem);
            BindModel(_lastModel);
            if (playVo && _isVisible)
                PlayVo(force: false);
        }

        private void BindModel(PlantCard4vCareViewModel model)
        {
            if (model == null)
                return;

            if (model.IsEmpty && _isVisible)
            {
                ShowEmptyPotToast(model.PotId);
                Hide();
                return;
            }

            if (_potIdLabel != null) _potIdLabel.text = model.PotId;
            if (_plantNameLabel != null) _plantNameLabel.text = model.PlantName;
            if (_plantSubtitleLabel != null) _plantSubtitleLabel.text = model.PlantSubtitle;
            if (_speciesLabel != null) _speciesLabel.text = model.SpeciesLine;
            if (_lifeStateLabel != null) _lifeStateLabel.text = model.LifeState;
            if (_stageDetailLabel != null) _stageDetailLabel.text = model.StageDetail;
            if (_mainNeedLabel != null) _mainNeedLabel.text = model.MainNeed;
            if (_mainNeedSubtitleLabel != null) _mainNeedSubtitleLabel.text = model.MainNeedSubtitle;
            if (_hydrationLabel != null) _hydrationLabel.text = model.HydrationText;
            if (_phValueLabel != null) _phValueLabel.text = model.PhDomeDriftText;
            if (_phAffinityLabel != null) _phAffinityLabel.text = model.PhDomeBandShort;
            if (_fertilizerLabel != null) _fertilizerLabel.text = model.FertilizerText;
            if (_conditionLabel != null)
            {
                _conditionLabel.text = model.ConditionText;
                _conditionLabel.RemoveFromClassList("pcv4-need-subtitle--risk");
                if (ShouldHighlightCondition(model))
                    _conditionLabel.AddToClassList("pcv4-need-subtitle--risk");
            }
            if (_conditionSummaryLabel != null) _conditionSummaryLabel.text = model.LightStressPercentLine;
            if (_conditionPhAffinitySummaryLabel != null) _conditionPhAffinitySummaryLabel.text = model.PlantPhPreferenceLabel;
            if (_conditionMoldSummaryLabel != null) _conditionMoldSummaryLabel.text = model.MoldLevelLine;
            if (_preferredLightLabel != null) _preferredLightLabel.text = model.PreferredLightLine;
            if (_mainRiskLabel != null) _mainRiskLabel.text = model.MainRisk;
            if (_riskCauseLabel != null) _riskCauseLabel.text = model.RiskCause;
            if (_riskLevelLabel != null) _riskLevelLabel.text = model.RiskLevelText;
            if (_voTextLabel != null) _voTextLabel.text = string.Empty;
            if (_footerStateLabel != null) _footerStateLabel.text = model.FooterStateLine;
            if (_shortIdLabel != null) _shortIdLabel.text = model.ShortPotId;
            if (_plantGlyphLabel != null) _plantGlyphLabel.text = model.IsEmpty ? "EMPTY" : model.LifeState;

            FillSegments(_hydrationBar, Mathf.RoundToInt(model.HydrationPercent / 12.5f), "pcv4-segment--on");
            FillSegments(_riskBar, model.RiskSegments, "pcv4-segment--risk-on");
            BindRiskPanelVisibility(model);
            BindSecondaryRisk(model);
            BindActionButtons(model);
        }

        private void ShowEmptyPotToast(string potId)
        {
            string safePotId = string.IsNullOrWhiteSpace(potId) ? "POT" : potId;
            if (TryPostEmptyPotToast(safePotId))
                return;

            if (_emptyPotToastRetryRoutine != null)
                StopCoroutine(_emptyPotToastRetryRoutine);
            _emptyPotToastRetryRoutine = StartCoroutine(RetryEmptyPotToastRoutine(safePotId));
        }

        private bool TryPostEmptyPotToast(string safePotId)
        {
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation == null || !foundation.Enabled)
                return false;

            foundation.PostToastImmediate(
                "POT-EMPTY",
                new NotificationPayload().With("potId", safePotId),
                NotificationSeverity.Warning);
            return true;
        }

        private IEnumerator RetryEmptyPotToastRoutine(string safePotId)
        {
            for (int i = 0; i < 45; i++)
            {
                yield return null;
                if (TryPostEmptyPotToast(safePotId))
                    break;
            }

            _emptyPotToastRetryRoutine = null;
        }

        private void BindRiskPanelVisibility(PlantCard4vCareViewModel model)
        {
            if (_riskDetailBlock != null)
                _riskDetailBlock.style.display = model.ShowRiskDetailPanel ? DisplayStyle.Flex : DisplayStyle.None;
            if (_riskCalmBlock != null)
                _riskCalmBlock.style.display = model.ShowRiskDetailPanel ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private static bool ShouldHighlightCondition(PlantCard4vCareViewModel model)
        {
            if (model == null)
                return false;
            return model.ConditionText switch
            {
                "STRESSATA" or "CRITICA" or "DEBOLE" or "MORTA" => true,
                _ => false
            };
        }

        private void BindActionButtons(PlantCard4vCareViewModel model)
        {
            PlantCard4vActionKind firstSlotAction = ResolveFirstActionSlot(model);
            string firstSlotLabel = firstSlotAction == PlantCard4vActionKind.Water
                ? GetWaterButtonText(model)
                : GetFirstActionSlotText(firstSlotAction);

            SetActionButton(_waterButton, _waterActionTitleLabel, _waterActionSubtitleLabel, firstSlotAction, firstSlotLabel, model);

            PlantCard4vActionKind redKind = ResolveRedButtonAction(model);
            SetActionButton(_lightRedButton, _lightRedActionTitleLabel, _lightRedActionSubtitleLabel, redKind, GetRedLightButtonTitle(model), model);

            PlantCard4vActionKind blueKind = ResolveBlueButtonAction(model);
            SetActionButton(_lightBlueButton, _lightBlueActionTitleLabel, _lightBlueActionSubtitleLabel, blueKind, GetBlueLightButtonTitle(model), model);

            SetActionButton(_additiveButton, _additiveActionTitleLabel, _additiveActionSubtitleLabel, PlantCard4vActionKind.Additive, "ADDITIVO pH", model);
            SetActionButton(_pruneButton, _pruneActionTitleLabel, _pruneActionSubtitleLabel, PlantCard4vActionKind.Prune, "POTARE", model);
            SetActionButton(_fertilizeButton, _fertilizeActionTitleLabel, _fertilizeActionSubtitleLabel, PlantCard4vActionKind.Fertilize, "FERTILIZZARE", model);
        }

        private void BindSecondaryRisk(PlantCard4vCareViewModel model)
        {
            if (_secondaryRiskItem == null || model == null)
                return;

            _secondaryRiskItem.style.display = model.HasSecondaryRisk ? DisplayStyle.Flex : DisplayStyle.None;
            if (!model.HasSecondaryRisk)
                return;

            if (_secondaryRiskTitleLabel != null)
                _secondaryRiskTitleLabel.text = model.SecondaryRiskTitle;
            if (_secondaryRiskCauseLabel != null)
                _secondaryRiskCauseLabel.text = model.SecondaryRiskCause;
        }

        private void SetActionButton(Button button, Label titleLabel, Label subtitleLabel, PlantCard4vActionKind action, string label, PlantCard4vCareViewModel model)
        {
            if (button == null || model == null)
                return;

            button.text = string.Empty;
            button.RemoveFromClassList("pcv4-action--primary");
            button.RemoveFromClassList("pcv4-action--muted");
            button.RemoveFromClassList("pcv4-action--disabled");

            bool terminalOnly = action == PlantCard4vActionKind.TerminalPlant
                || action == PlantCard4vActionKind.TerminalHarvest
                || action == PlantCard4vActionKind.TerminalUproot;

            if (action != PlantCard4vActionKind.None && action == model.PrimaryAction)
            {
                SetActionButtonText(button, titleLabel, subtitleLabel, terminalOnly ? "TERMINALE POT" : label, terminalOnly ? "Richiede Terminale POT" : "INTERVENTO PRIORITARIO");
                button.AddToClassList("pcv4-action--primary");
            }
            else
            {
                SetActionButtonText(button, titleLabel, subtitleLabel, label, BuildActionSubtitle(action, model, terminalOnly));
                button.AddToClassList("pcv4-action--muted");
            }

            bool enabled = action != PlantCard4vActionKind.None
                && !model.IsEmpty
                && !model.IsDead
                && !terminalOnly;
            button.SetEnabled(enabled);
            if (!enabled)
                button.AddToClassList("pcv4-action--disabled");
        }

        private static void SetActionButtonText(Button button, Label titleLabel, Label subtitleLabel, string title, string subtitle)
        {
            if (titleLabel == null || subtitleLabel == null)
            {
                button.text = $"{title}\n{subtitle}";
                return;
            }

            titleLabel.text = title;
            subtitleLabel.text = subtitle;
        }

        private static string BuildActionSubtitle(PlantCard4vActionKind action, PlantCard4vCareViewModel model, bool terminalOnly)
        {
            if (terminalOnly)
                return "Procedura: Terminale POT";
            if (model.IsEmpty)
                return "Bloccato: POT vuoto";
            if (model.IsDead)
                return "Bloccato: stato finale";
            if (action == PlantCard4vActionKind.None)
                return "Parametro stabile";

            return action switch
            {
                PlantCard4vActionKind.Additive => "Gestione acidita'",
                PlantCard4vActionKind.Prune => "Cura tessuti",
                PlantCard4vActionKind.Fertilize => "Fertilizzante",
                PlantCard4vActionKind.Observe => "Ispezione ravvicinata",
                PlantCard4vActionKind.LightBlue => "Controllo LED",
                PlantCard4vActionKind.LightRed => "Controllo LED",
                PlantCard4vActionKind.LightOff => "Controllo LED",
                PlantCard4vActionKind.Water => model.IsWateringActive ? "Irrigazione attiva" : "Gestione acqua",
                _ => "Procedura disponibile"
            };
        }

        private static PlantCard4vActionKind ResolveFirstActionSlot(PlantCard4vCareViewModel model)
        {
            if (model == null)
                return PlantCard4vActionKind.Water;

            return model.PrimaryAction switch
            {
                PlantCard4vActionKind.TerminalPlant => PlantCard4vActionKind.TerminalPlant,
                PlantCard4vActionKind.TerminalHarvest => PlantCard4vActionKind.TerminalHarvest,
                PlantCard4vActionKind.TerminalUproot => PlantCard4vActionKind.TerminalUproot,
                _ => PlantCard4vActionKind.Water
            };
        }

        private static string GetFirstActionSlotText(PlantCard4vActionKind action)
        {
            return action switch
            {
                PlantCard4vActionKind.TerminalPlant => "TERMINALE POT",
                PlantCard4vActionKind.TerminalHarvest => "TERMINALE POT",
                PlantCard4vActionKind.TerminalUproot => "TERMINALE POT",
                _ => "IRRIGARE"
            };
        }

        private static string GetWaterButtonText(PlantCard4vCareViewModel model)
        {
            return model != null && model.IsWateringActive ? "SPEGNI IRRIGAZIONE" : "IRRIGARE";
        }

        private static PlantCard4vActionKind ResolveRedButtonAction(PlantCard4vCareViewModel model)
        {
            if (model == null)
                return PlantCard4vActionKind.LightRed;
            return model.LedState == LedSystemState.Red ? PlantCard4vActionKind.LightOff : PlantCard4vActionKind.LightRed;
        }

        private static PlantCard4vActionKind ResolveBlueButtonAction(PlantCard4vCareViewModel model)
        {
            if (model == null)
                return PlantCard4vActionKind.LightBlue;
            return model.LedState == LedSystemState.Blue ? PlantCard4vActionKind.LightOff : PlantCard4vActionKind.LightBlue;
        }

        private static string GetRedLightButtonTitle(PlantCard4vCareViewModel model)
        {
            if (model == null)
                return "LUCE ROSSA";
            return model.LedState == LedSystemState.Red ? "SPEGNI LUCE ROSSA" : "ACCENDI LUCE ROSSA";
        }

        private static string GetBlueLightButtonTitle(PlantCard4vCareViewModel model)
        {
            if (model == null)
                return "LUCE BLU";
            return model.LedState == LedSystemState.Blue ? "SPEGNI LUCE BLU" : "ACCENDI LUCE BLU";
        }

        private void ExecuteRedLightAction()
        {
            if (_lastModel == null)
                Refresh(playVo: false);
            ExecuteAction(ResolveRedButtonAction(_lastModel));
        }

        private void ExecuteBlueLightAction()
        {
            if (_lastModel == null)
                Refresh(playVo: false);
            ExecuteAction(ResolveBlueButtonAction(_lastModel));
        }

        private void ExecuteAction(PlantCard4vActionKind action)
        {
            PotSlot pot = ResolveTargetPot();
            PotActions actions = pot != null ? pot.PotActions : null;
            if (actions == null)
                return;

            bool success = action switch
            {
                PlantCard4vActionKind.Water => actions.DoWater(),
                PlantCard4vActionKind.LightBlue => actions.DoLight(LedSystemState.Blue),
                PlantCard4vActionKind.LightRed => actions.DoLight(LedSystemState.Red),
                PlantCard4vActionKind.LightOff => actions.DoLight(LedSystemState.Off),
                PlantCard4vActionKind.Additive => actions.DoApplyAdditive(ChooseAdditiveTypeId(actions.PotState)),
                PlantCard4vActionKind.Prune => actions.DoPruning(),
                PlantCard4vActionKind.Fertilize => actions.DoFertilize(ChooseFertilizerTypeId(actions.PotState)),
                _ => false
            };

            RequestRealtimeRefresh(playVo: success);
        }

        private void RequestRealtimeRefresh(bool playVo)
        {
            if (!_isVisible)
                return;

            _deferredRefreshPlayVo |= playVo;
            if (_deferredRefreshRoutine == null)
                _deferredRefreshRoutine = StartCoroutine(DeferredRefreshRoutine());
        }

        private IEnumerator DeferredRefreshRoutine()
        {
            yield return null;

            bool playVo = _deferredRefreshPlayVo;
            _deferredRefreshPlayVo = false;
            _deferredRefreshRoutine = null;

            if (_isVisible)
                Refresh(playVo);
        }

        private string ChooseAdditiveTypeId(PotStateModel state)
        {
            if (state == null)
                return Items.AdditiveBasic;

            PlantData plantData = state.GetPlantData();
            if (_phSystem != null && plantData != null)
            {
                if (_phSystem.CurrentPh < plantData.OptimalPhMin)
                    return Items.AdditiveBasic;
                if (_phSystem.CurrentPh > plantData.OptimalPhMax)
                    return Items.AdditiveAcid;
            }

            return state.MoldRiskLevel > 0 || state.IsInfested ? Items.AdditiveBasic : Items.AdditiveAcid;
        }

        private string ChooseFertilizerTypeId(PotStateModel state)
        {
            PlantData plantData = state != null ? state.GetPlantData() : null;
            if (plantData == null)
                return Items.FertilizerStandard;

            return plantData.Family switch
            {
                PlantFamily.Pure => Items.FertilizerPure,
                PlantFamily.Evil => Items.FertilizerProhibited,
                _ => Items.FertilizerStandard
            };
        }

        private void FillSegments(VisualElement bar, int filled, string fillClass)
        {
            if (bar == null)
                return;

            var segments = bar.Query<VisualElement>(className: "pcv4-segment").ToList();
            int clamped = Mathf.Clamp(filled, 0, segments.Count);
            for (int i = 0; i < segments.Count; i++)
            {
                segments[i].RemoveFromClassList("pcv4-segment--on");
                segments[i].RemoveFromClassList("pcv4-segment--risk-on");
                if (i < clamped)
                    segments[i].AddToClassList(fillClass);
            }
        }

        private void PlayVo(bool force)
        {
            if (_lastModel == null || string.IsNullOrWhiteSpace(_lastModel.VoHintLine))
                return;

            int day = ResolveCurrentDay();
            if (!force && _lastVoDay == day && _lastVoHintId == _lastModel.VoHintId)
                return;

            _lastVoDay = day;
            _lastVoHintId = _lastModel.VoHintId;

            ShowOfficialVoLine(_lastModel.VoHintLine);
        }

        private void ShowOfficialVoLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            if (_voOverlay == null)
                _voOverlay = ServiceContainer.Instance?.Get<VoOverlayController>(suppressWarning: true);

            if (_voTextLabel != null)
                _voTextLabel.text = string.Empty;

            _voOverlay?.SetPlantCard4vDocked(_isVisible);
            _voOverlay?.ShowLine(
                line,
                _internalVoRegister,
                _voCharsPerSecond,
                null,
                hideAfterTypingWithoutIdle: false,
                VoLinePresentationOptions.LegacySingleBlock);
            _plantCardVoActive = true;
        }

        private int ResolveCurrentDay()
        {
            var dayCycle = ServiceContainer.Instance?.Get<DayCycleSystem>(suppressWarning: true);
            return dayCycle != null ? dayCycle.CurrentDay : -1;
        }

        private PotSlot ResolveTargetPot()
        {
            if (_targetPot != null)
                return _targetPot;

            if (_potRegistry == null)
                _potRegistry = ServiceContainer.Instance?.Get<DomePotRegistry>(suppressWarning: true);

            _targetPot = _potRegistry?.FindPotById(_potId);
            return _targetPot;
        }

        private bool IsOwnPot(PotSlot pot)
        {
            if (pot == null)
                return false;

            string ownId = !string.IsNullOrWhiteSpace(_potId) ? _potId : _targetPot?.PotId;
            return string.Equals(pot.PotId, ownId, System.StringComparison.OrdinalIgnoreCase);
        }

        private bool IsOwnPotId(string potId)
        {
            string ownId = !string.IsNullOrWhiteSpace(_potId) ? _potId : _targetPot?.PotId;
            return string.Equals(potId, ownId, System.StringComparison.OrdinalIgnoreCase);
        }

        private void HandlePotStateChanged(PotSlot pot)
        {
            if (_isVisible && IsOwnPot(pot))
                RequestRealtimeRefresh(playVo: true);
        }

        private void HandlePotAction(PotEvents.PotActionType actionType, PotSlot pot)
        {
            if (_isVisible && IsOwnPot(pot))
                RequestRealtimeRefresh(playVo: true);
        }

        private void HandlePotActionFailed(PotEvents.PotActionType actionType, PotSlot pot, string reason)
        {
            if (!_isVisible || !IsOwnPot(pot))
                return;

            ShowOfficialVoLine(string.IsNullOrWhiteSpace(reason) ? "Il Pot non accetta questa procedura." : reason);
        }

        private void HandlePlantStageChanged(string potId, PlantStage stage)
        {
            if (_isVisible && IsOwnPotId(potId))
                RequestRealtimeRefresh(playVo: true);
        }

        private void HandlePlantDied(string potId, string reason)
        {
            if (_isVisible && IsOwnPotId(potId))
                RequestRealtimeRefresh(playVo: true);
        }

        private void HandlePotSelected(PotSlot pot)
        {
            if (!_openOnOwnPotSelected || pot == null || !IsOwnPot(pot))
                return;

            // Apri la card solo se c'è una pianta da curare; altrimenti solo toast (gestito in SetVisible).
            Show();
        }
    }
}
