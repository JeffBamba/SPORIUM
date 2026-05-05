using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using _Project;
using _Project.Sporae.Core;
using _Project.UI.UIToolkit.VoOverlay;
using Sporae.Core.Localization;
using Sporae.DevTools;
using Sporae.Dome;
using Sporae.Dome.PotSystem.Condition;
using Sporae.Dome.PotSystem.Fertilizer;
using Sporae.Dome.PotSystem.Growth;
using Sporae.UI.UIToolkit.NotificationsFoundation;
using Sporae.UI.UIToolkit.PlayerInventory;
using Sporae.UI.UIToolkit.HUD;
using Sporae.UI.UIToolkit;
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
        [SerializeField] private PlayerInventoryPanelController _playerInventoryPanel;
        [SerializeField] private bool _openOnOwnPotSelected = true;
        [SerializeField] private bool _showOnStart;

        [Header("VO (testo in-card: pcv4-vo-text)")]
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
        private Label _hydrationLabel;
        private Label _phValueLabel;
        private Label _phAffinityLabel;
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
        private Label _footerLightStatusLabel;
        private Label _footerIrrigationStatusLabel;
        private Label _shortIdLabel;
        private Label _plantGlyphLabel;
        private VisualElement _hydrationBar;
        private VisualElement _riskBar;
        private Label _fertilizerMeterLabel;
        private VisualElement _fertilizerBar;
        private VisualElement _needRowSummary;
        private VisualElement _needRowHydration;
        private VisualElement _needRowPh;
        private VisualElement _needRowFert;
        private VisualElement _needRowCond;
        private Label _mainNeedSubtitleLabel;
        private Label _needTitleHydration;
        private Label _needTitlePh;
        private Label _needTitleFert;
        private Label _needTitleCond;

        private static readonly CultureInfo ItPhCulture = CultureInfo.GetCultureInfo("it-IT");
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
        private Coroutine _voTextTypeRoutine;
        private Coroutine _deferredRefreshRoutine;
        private bool _deferredRefreshPlayVo;
        private Coroutine _emptyPotToastRetryRoutine;
        private VisualElement _needTooltipFlyout;
        private Label _needTooltipLabel;
        private bool _needTooltipHoverRegistered;
        private PlantCard4vVoReactionRequest _pendingVoReaction;

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
            ResolveServices();
            PotEvents.OnPotStateChanged += HandlePotStateChanged;
            PotEvents.OnPotAction += HandlePotAction;
            PotEvents.OnPotActionFailed += HandlePotActionFailed;
            PotEvents.OnPlantStageChanged += HandlePlantStageChanged;
            PotEvents.OnPlantDied += HandlePlantDied;
            PotEvents.OnPotSelected += HandlePotSelected;
            if (_phSystem != null)
                _phSystem.OnPhChanged += HandlePhSystemChanged;
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
            if (_phSystem != null)
                _phSystem.OnPhChanged -= HandlePhSystemChanged;
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
            CancelVoReaction();
            StopPlantCardVoTyping();
            if (_isVisible)
                ApplyPlantCard4vPresentation(false);
        }

        private void Update()
        {
            if (!_isVisible)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                Hide();

            if (_phSystem != null && _phValueLabel != null)
                ApplyLivePhDisplay();
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

            if (!effectiveVisible)
                HideNeedTooltipFlyout();

            ApplyPlantCard4vPresentation(effectiveVisible);

            if (effectiveVisible)
            {
                Refresh(playVo);
            }
            else
            {
                StopPlantCardVoTyping();
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
                // PlantCard4v: nasconde Dome Status HUD, Foundation Notifications e Player Status; TopBar resta visibile.
                GameplayUiModalLock.SetSuppressDomeStatusHud(true);
                GameplayUiModalLock.SetInventoryContextHudVisible(false);
                GameplayUiModalLock.SetMachineModalState(true, keepFixedHudVisible: true);
                _voOverlay?.Hide();
                _voOverlay?.SetPlantCard4vDocked(false);
                return;
            }

            StopPlantCardVoTyping();
            _voOverlay?.Hide();
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
            _footerLightStatusLabel = _document.rootVisualElement.Q<Label>("pcv4-footer-light-status");
            _footerIrrigationStatusLabel = _document.rootVisualElement.Q<Label>("pcv4-footer-irrigation-status");
            _shortIdLabel = _document.rootVisualElement.Q<Label>("pcv4-short-id");
            _plantGlyphLabel = _document.rootVisualElement.Q<Label>("pcv4-plant-glyph");
            _hydrationBar = _document.rootVisualElement.Q<VisualElement>("pcv4-hydration-bar");
            _fertilizerMeterLabel = _document.rootVisualElement.Q<Label>("pcv4-fertilizer-label");
            _fertilizerBar = _document.rootVisualElement.Q<VisualElement>("pcv4-fertilizer-bar");
            _needRowSummary = _document.rootVisualElement.Q<VisualElement>("pcv4-need-row-summary");
            _needRowHydration = _document.rootVisualElement.Q<VisualElement>("pcv4-need-row-hydration");
            _needRowPh = _document.rootVisualElement.Q<VisualElement>("pcv4-need-row-ph");
            _needRowFert = _document.rootVisualElement.Q<VisualElement>("pcv4-need-row-fert");
            _needRowCond = _document.rootVisualElement.Q<VisualElement>("pcv4-need-row-cond");
            _needTitleHydration = _document.rootVisualElement.Q<Label>("pcv4-need-title-hydration");
            _needTitlePh = _document.rootVisualElement.Q<Label>("pcv4-need-title-ph");
            _needTitleFert = _document.rootVisualElement.Q<Label>("pcv4-need-title-fert");
            _needTitleCond = _document.rootVisualElement.Q<Label>("pcv4-need-title-cond");
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

            RegisterNeedRowTooltipHoverOnce();
        }

        private void Refresh(bool playVo)
        {
            ResolveServices();
            PotSlot pot = ResolveTargetPot();
            PotStateModel state = pot != null ? pot.PotActions?.PotState : null;
            PlantData plantData = state != null ? state.GetPlantData() : null;
            int phraseSalt = ResolveCurrentDay() * 17;
            if (pot != null && !string.IsNullOrEmpty(pot.PotId))
                phraseSalt ^= pot.PotId.GetHashCode();
            _lastModel = PlantCard4vCareViewModel.Build(pot, state, plantData, _potSystemConfig, _phSystem, _pendingVoReaction, phraseSalt);
            _pendingVoReaction = null;
            BindModel(_lastModel);
            if (playVo && _isVisible)
                PlayVo(force: false);
        }

        private void BeginVoReaction(PotEvents.PotActionType action, string detail = null)
        {
            PotSlot pot = ResolveTargetPot();
            PotStateModel state = pot?.PotActions?.PotState;
            PlantCard4vCareSnapshot snap = PlantCard4vCareSnapshot.Capture(state, _potSystemConfig, _phSystem);
            if (!snap.HasPlant)
            {
                _pendingVoReaction = null;
                return;
            }

            _pendingVoReaction = new PlantCard4vVoReactionRequest(action, snap, detail, Guid.NewGuid().ToString("N"));
        }

        private void CancelVoReaction() => _pendingVoReaction = null;

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
            if (_mainNeedSubtitleLabel != null)
            {
                _mainNeedSubtitleLabel.text = model.MainNeedSubtitle ?? string.Empty;
                _mainNeedSubtitleLabel.style.display = string.IsNullOrWhiteSpace(model.MainNeedSubtitle)
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
            }

            ApplyNeedTitleSignal(_mainNeedLabel, WorstNeedSignal(
                model.HydrationNeedSignal, model.PhNeedSignal, model.FertilizerNeedSignal, model.ConditionNeedSignal));
            ApplyNeedTitleSignal(_needTitleHydration, model.HydrationNeedSignal);
            ApplyNeedTitleSignal(_needTitlePh, model.PhNeedSignal);
            ApplyNeedTitleSignal(_needTitleFert, model.FertilizerNeedSignal);
            ApplyNeedTitleSignal(_needTitleCond, model.ConditionNeedSignal);

            BindNeedRowTooltipData(model);

            if (_hydrationLabel != null) _hydrationLabel.text = model.HydrationText;
            if (_fertilizerMeterLabel != null) _fertilizerMeterLabel.text = model.FertilizerMeterLabel;
            if (_phValueLabel != null) _phValueLabel.text = model.PhDomeAmbientValueText;
            if (_phAffinityLabel != null) _phAffinityLabel.text = model.PhDomeBandShort;
            if (_conditionSummaryLabel != null) _conditionSummaryLabel.text = model.LightStressPercentLine;
            if (_conditionPhAffinitySummaryLabel != null) _conditionPhAffinitySummaryLabel.text = model.PlantPhPreferenceLabel;
            if (_conditionMoldSummaryLabel != null) _conditionMoldSummaryLabel.text = model.MoldLevelLine;
            if (_preferredLightLabel != null) _preferredLightLabel.text = model.PreferredLightLine;
            if (_mainRiskLabel != null) _mainRiskLabel.text = model.MainRisk;
            if (_riskCauseLabel != null) _riskCauseLabel.text = model.RiskCause;
            if (_riskLevelLabel != null) _riskLevelLabel.text = model.RiskLevelText;
            if (_voTextLabel != null) _voTextLabel.text = string.Empty;
            if (_footerStateLabel != null) _footerStateLabel.text = model.FooterStateLine;
            if (_footerLightStatusLabel != null)
                _footerLightStatusLabel.text = $"Status della luce: {model.FooterLightStatusText}";
            if (_footerIrrigationStatusLabel != null)
                _footerIrrigationStatusLabel.text = $"Status dell'irrigazione: {model.FooterIrrigationStatusText}";
            if (_shortIdLabel != null) _shortIdLabel.text = model.ShortPotId;
            if (_plantGlyphLabel != null) _plantGlyphLabel.text = model.IsEmpty ? "EMPTY" : model.LifeState;

            FillSegments(_hydrationBar, Mathf.RoundToInt(model.HydrationPercent / 12.5f), "pcv4-segment--on");
            FillSegments(_fertilizerBar, Mathf.RoundToInt(model.FertilizerPercent / 12.5f), "pcv4-segment--on");
            FillSegments(_riskBar, model.RiskSegments, "pcv4-segment--risk-on");
            BindRiskPanelVisibility(model);
            BindSecondaryRisk(model);
            BindActionButtons(model);
            ApplyLivePhDisplay();
        }

        private static PlantCard4vNeedSignal WorstNeedSignal(
            PlantCard4vNeedSignal a,
            PlantCard4vNeedSignal b,
            PlantCard4vNeedSignal c,
            PlantCard4vNeedSignal d)
        {
            PlantCard4vNeedSignal WorstPair(PlantCard4vNeedSignal x, PlantCard4vNeedSignal y)
            {
                int Rank(PlantCard4vNeedSignal s) => s switch
                {
                    PlantCard4vNeedSignal.Warning => 2,
                    PlantCard4vNeedSignal.Attention => 1,
                    _ => 0
                };
                return Rank(x) >= Rank(y) ? x : y;
            }

            return WorstPair(WorstPair(a, b), WorstPair(c, d));
        }

        /// <summary>Aggiorna il numero pH oscillato come TopBar; chiamare anche dopo BindModel.</summary>
        private void ApplyLivePhDisplay()
        {
            if (_phSystem == null || _phValueLabel == null)
                return;
            float valuePh = PhLiveDisplayMath.ComputeOscillatedDisplayPh(_phSystem.CurrentPh, Time.time);
            PhSystem.PhBand band = _phSystem.EvaluateBand(valuePh);
            Color bandColor = PhGradientDisplayColors.GetColorForPhBand(band);
            _phValueLabel.text = valuePh.ToString("F1", ItPhCulture);
            _phValueLabel.style.color = new StyleColor(bandColor);
            if (_phAffinityLabel != null)
            {
                _phAffinityLabel.text = PlantCard4vCareViewModel.FormatPhBandShort(band);
                _phAffinityLabel.style.color = new StyleColor(bandColor);
            }
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

        private static void ApplyNeedTitleSignal(Label label, PlantCard4vNeedSignal signal)
        {
            if (label == null)
                return;

            label.RemoveFromClassList("pcv4-need-title--signal-ok");
            label.RemoveFromClassList("pcv4-need-title--signal-attention");
            label.RemoveFromClassList("pcv4-need-title--signal-warning");
            label.AddToClassList(signal switch
            {
                PlantCard4vNeedSignal.Attention => "pcv4-need-title--signal-attention",
                PlantCard4vNeedSignal.Warning => "pcv4-need-title--signal-warning",
                _ => "pcv4-need-title--signal-ok"
            });
        }

        private void BindNeedRowTooltipData(PlantCard4vCareViewModel model)
        {
            if (model == null)
                return;

            ApplyNeedRowTooltip(_needRowSummary, model.SummaryRowTooltip);
            ApplyNeedRowTooltip(_needRowHydration, model.HydrationRowTooltip);
            ApplyNeedRowTooltip(_needRowPh, model.PhRowTooltip);
            ApplyNeedRowTooltip(_needRowFert, model.FertilizerRowTooltip);
            ApplyNeedRowTooltip(_needRowCond, model.ConditionRowTooltip);
        }

        private static void ApplyNeedRowTooltip(VisualElement row, string tooltip)
        {
            if (row == null)
                return;

            string t = string.IsNullOrWhiteSpace(tooltip) ? string.Empty : tooltip;
            row.userData = t;
            SetTooltipRecursive(row, t);
        }

        private static void SetTooltipRecursive(VisualElement ve, string tooltip)
        {
            if (ve == null)
                return;

            ve.tooltip = tooltip ?? string.Empty;
            foreach (VisualElement child in ve.hierarchy.Children())
                SetTooltipRecursive(child, tooltip);
        }

        private void RegisterNeedRowTooltipHoverOnce()
        {
            if (_needTooltipHoverRegistered)
                return;

            _needTooltipHoverRegistered = true;
            RegisterNeedRowPointerHandlers(_needRowSummary);
            RegisterNeedRowPointerHandlers(_needRowHydration);
            RegisterNeedRowPointerHandlers(_needRowPh);
            RegisterNeedRowPointerHandlers(_needRowFert);
            RegisterNeedRowPointerHandlers(_needRowCond);
        }

        private void RegisterNeedRowPointerHandlers(VisualElement row)
        {
            if (row == null)
                return;

            row.RegisterCallback<PointerEnterEvent>(OnNeedRowPointerEnter, TrickleDown.TrickleDown);
            row.RegisterCallback<PointerLeaveEvent>(OnNeedRowPointerLeave, TrickleDown.TrickleDown);
        }

        private void OnNeedRowPointerEnter(PointerEnterEvent evt)
        {
            if (!_isVisible)
                return;

            if (!(evt.currentTarget is VisualElement row))
                return;

            string text = row.userData as string;
            if (string.IsNullOrWhiteSpace(text))
            {
                HideNeedTooltipFlyout();
                return;
            }

            ShowNeedTooltipFlyout(row, text);
        }

        private void OnNeedRowPointerLeave(PointerLeaveEvent evt)
        {
            HideNeedTooltipFlyout();
        }

        private void EnsureNeedTooltipFlyout()
        {
            if (_needTooltipFlyout != null)
                return;

            VisualElement host = _root != null ? _root : _document.rootVisualElement;
            _needTooltipFlyout = new VisualElement { name = "pcv4-need-tooltip-flyout", pickingMode = PickingMode.Ignore };
            _needTooltipFlyout.AddToClassList("pcv4-need-tooltip-flyout");
            _needTooltipFlyout.style.display = DisplayStyle.None;
            _needTooltipFlyout.style.position = Position.Absolute;

            _needTooltipLabel = new Label { pickingMode = PickingMode.Ignore };
            _needTooltipFlyout.Add(_needTooltipLabel);
            host.Add(_needTooltipFlyout);
        }

        private void ShowNeedTooltipFlyout(VisualElement anchorRow, string text)
        {
            EnsureNeedTooltipFlyout();
            VisualElement host = _root != null ? _root : _document.rootVisualElement;

            _needTooltipLabel.text = text;
            Rect bounds = anchorRow.worldBound;
            const float padX = 10f;
            // A destra della riga bisogno (non sotto), così non copre le righe successive.
            Vector2 worldTopRight = new Vector2(bounds.xMax + padX, bounds.yMin);
            Vector2 local = host.WorldToLocal(worldTopRight);
            _needTooltipFlyout.style.left = local.x;
            _needTooltipFlyout.style.top = local.y;
            _needTooltipFlyout.style.display = DisplayStyle.Flex;
            _needTooltipFlyout.BringToFront();

            _needTooltipFlyout.schedule.Execute(() =>
            {
                float h = _needTooltipFlyout.resolvedStyle.height;
                if (h <= 1f || float.IsNaN(h))
                    return;

                float centerY = bounds.yMin + bounds.height * 0.5f;
                float topWorld = centerY - h * 0.5f;
                Vector2 localTop = host.WorldToLocal(new Vector2(bounds.xMax + padX, topWorld));
                _needTooltipFlyout.style.top = localTop.y;
            }).StartingIn(0);
        }

        private void HideNeedTooltipFlyout()
        {
            if (_needTooltipFlyout != null)
                _needTooltipFlyout.style.display = DisplayStyle.None;
        }

        private void BindRiskPanelVisibility(PlantCard4vCareViewModel model)
        {
            if (_riskDetailBlock != null)
                _riskDetailBlock.style.display = model.ShowRiskDetailPanel ? DisplayStyle.Flex : DisplayStyle.None;
            if (_riskCalmBlock != null)
                _riskCalmBlock.style.display = model.ShowRiskDetailPanel ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void BindActionButtons(PlantCard4vCareViewModel model)
        {
            PlantCard4vActionKind firstSlotAction = ResolveFirstActionSlot(model);
            string firstSlotLabel = firstSlotAction == PlantCard4vActionKind.Water
                ? GetWaterButtonText(model)
                : GetFirstActionSlotText(firstSlotAction);

            bool waterSlotActive = firstSlotAction == PlantCard4vActionKind.Water && model.IsWateringActive;
            SetActionButton(_waterButton, _waterActionTitleLabel, _waterActionSubtitleLabel, firstSlotAction, firstSlotLabel, model, waterSlotActive);

            PlantCard4vActionKind redKind = ResolveRedButtonAction(model);
            bool redLedOn = model.LedState == LedSystemState.Red;
            SetActionButton(_lightRedButton, _lightRedActionTitleLabel, _lightRedActionSubtitleLabel, redKind, GetRedLightButtonTitle(model), model, redLedOn);

            PlantCard4vActionKind blueKind = ResolveBlueButtonAction(model);
            bool blueLedOn = model.LedState == LedSystemState.Blue;
            SetActionButton(_lightBlueButton, _lightBlueActionTitleLabel, _lightBlueActionSubtitleLabel, blueKind, GetBlueLightButtonTitle(model), model, blueLedOn);

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

        private void SetActionButton(Button button, Label titleLabel, Label subtitleLabel, PlantCard4vActionKind action, string label, PlantCard4vCareViewModel model, bool highlightActiveSystem = false)
        {
            if (button == null || model == null)
                return;

            button.text = string.Empty;
            button.RemoveFromClassList("pcv4-action--primary");
            button.RemoveFromClassList("pcv4-action--active-system");
            button.RemoveFromClassList("pcv4-action--irrigation-on");
            button.RemoveFromClassList("pcv4-action--led-red-on");
            button.RemoveFromClassList("pcv4-action--led-blue-on");
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

            if (highlightActiveSystem && enabled && !button.ClassListContains("pcv4-action--primary"))
            {
                button.RemoveFromClassList("pcv4-action--muted");
                if (action == PlantCard4vActionKind.Water)
                    button.AddToClassList("pcv4-action--irrigation-on");
                else if (button == _lightRedButton && model.LedState == LedSystemState.Red)
                    button.AddToClassList("pcv4-action--led-red-on");
                else if (button == _lightBlueButton && model.LedState == LedSystemState.Blue)
                    button.AddToClassList("pcv4-action--led-blue-on");
                else
                    button.AddToClassList("pcv4-action--active-system");
            }
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
            if (action == PlantCard4vActionKind.Additive)
            {
                BeginAdditivePickerFlow();
                return;
            }

            if (action == PlantCard4vActionKind.Fertilize)
            {
                BeginFertilizerPickerFlow();
                return;
            }

            PotSlot pot = ResolveTargetPot();
            PotActions actions = pot != null ? pot.PotActions : null;
            if (actions == null)
                return;

            PotEvents.PotActionType voAction = action switch
            {
                PlantCard4vActionKind.Water => PotEvents.PotActionType.Water,
                PlantCard4vActionKind.Prune => PotEvents.PotActionType.Pruning,
                _ => PotEvents.PotActionType.Light
            };

            BeginVoReaction(voAction);
            bool success = action switch
            {
                PlantCard4vActionKind.Water => actions.DoWater(),
                PlantCard4vActionKind.LightBlue => actions.DoLight(LedSystemState.Blue),
                PlantCard4vActionKind.LightRed => actions.DoLight(LedSystemState.Red),
                PlantCard4vActionKind.LightOff => actions.DoLight(LedSystemState.Off),
                PlantCard4vActionKind.Prune => actions.DoPruning(),
                _ => false
            };

            if (!success)
                CancelVoReaction();

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

        private void BeginAdditivePickerFlow()
        {
            PotSlot pot = ResolveTargetPot();
            PotActions actions = pot != null ? pot.PotActions : null;
            if (actions == null)
                return;

            var allowedOrdered = new List<string> { Items.AdditiveBasic, Items.AdditiveAcid };
            if (!HasAnyOwned(ResolvePlayerInventory(), allowedOrdered))
            {
                TryPostPickerBlockedToast("PC4-PICK-NO-ADD");
                return;
            }

            PlayerInventoryPanelController panel = ResolveInventoryPanel();
            if (panel == null)
                return;

            panel.ShowAsPicker(
                allowedOrdered,
                LocalizationManager.GetString("plantcard4.picker_additive"),
                (typeId, _, __) =>
                {
                    PotSlot p = ResolveTargetPot();
                    if (p?.PotActions == null || string.IsNullOrEmpty(typeId))
                        return;
                    BeginVoReaction(PotEvents.PotActionType.Spray, typeId);
                    bool ok = p.PotActions.DoApplyAdditive(typeId);
                    if (!ok)
                        CancelVoReaction();
                    RequestRealtimeRefresh(playVo: ok);
                },
                static () => { },
                null,
                "plantcard4_additive",
                presentFullInventoryUi: true);
        }

        private void BeginFertilizerPickerFlow()
        {
            PotSlot pot = ResolveTargetPot();
            PotActions actions = pot != null ? pot.PotActions : null;
            PotStateModel state = actions != null ? actions.PotState : null;
            if (state == null || !state.HasPlant)
                return;

            PlantData plantData = state.GetPlantData();
            if (plantData == null)
                return;

            List<string> allowedOrdered = BuildCompatibleFertilizerTypeIdsOrdered(plantData.Family);
            if (allowedOrdered.Count == 0)
                return;

            if (!HasAnyOwned(ResolvePlayerInventory(), allowedOrdered))
            {
                TryPostPickerBlockedToast("PC4-PICK-NO-FERT");
                return;
            }

            PlayerInventoryPanelController panel = ResolveInventoryPanel();
            if (panel == null)
                return;

            panel.ShowAsPicker(
                allowedOrdered,
                LocalizationManager.GetString("plantcard4.picker_fertilizer"),
                (typeId, _, __) =>
                {
                    PotSlot p = ResolveTargetPot();
                    if (p?.PotActions == null || string.IsNullOrEmpty(typeId))
                        return;
                    BeginVoReaction(PotEvents.PotActionType.Fertilize, typeId);
                    bool ok = p.PotActions.DoFertilize(typeId);
                    bool morta = p.PotActions.PotState != null
                        && (PlantCondition)p.PotActions.PotState.ConditionLabel == PlantCondition.Morta;
                    if (!ok && !morta)
                        CancelVoReaction();
                    RequestRealtimeRefresh(playVo: ok || morta);
                },
                static () => { },
                null,
                "plantcard4_fertilizer",
                presentFullInventoryUi: true);
        }

        private GameManager ResolveGameManager()
        {
            GameManager gm = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            if (gm == null)
                gm = FindObjectOfType<GameManager>();
            return gm;
        }

        private Inventory ResolvePlayerInventory()
        {
            GameManager gm = ResolveGameManager();
            return gm != null ? gm.PlayerInventory : null;
        }

        private PlayerInventoryPanelController ResolveInventoryPanel()
        {
            if (_playerInventoryPanel != null)
                return _playerInventoryPanel;
            _playerInventoryPanel = FindObjectOfType<PlayerInventoryPanelController>(true);
            return _playerInventoryPanel;
        }

        private static bool HasAnyOwned(Inventory inv, IReadOnlyList<string> typeIds)
        {
            if (inv == null || typeIds == null)
                return false;

            for (int i = 0; i < typeIds.Count; i++)
            {
                if (inv.Has(typeIds[i], 1))
                    return true;
            }

            return false;
        }

        private static void TryPostPickerBlockedToast(string code)
        {
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation == null || !foundation.Enabled)
                return;

            foundation.PostToastImmediate(code, null, NotificationSeverity.Info);
        }

        private static List<string> BuildCompatibleFertilizerTypeIdsOrdered(PlantFamily family)
        {
            string[] ordered = { Items.FertilizerStandard, Items.FertilizerPure, Items.FertilizerProhibited };
            var list = new List<string>(3);
            for (int i = 0; i < ordered.Length; i++)
            {
                string id = ordered[i];
                FertilizerType ft = MapItemCodeToFertilizerType(id);
                if (FertilizerSystem.IsFertilizerCompatible(ft, family))
                    list.Add(id);
            }

            return list;
        }

        private static FertilizerType MapItemCodeToFertilizerType(string itemCode)
        {
            return itemCode switch
            {
                Items.FertilizerStandard => FertilizerType.Standard,
                Items.FertilizerPure => FertilizerType.Pure,
                Items.FertilizerProhibited => FertilizerType.Prohibited,
                _ => FertilizerType.Standard
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

            if (_voTextLabel == null)
                return;

            ResolveServices();
            _voOverlay?.Hide();
            _voOverlay?.SetPlantCard4vDocked(false);

            StopPlantCardVoTyping();
            _voTextTypeRoutine = StartCoroutine(TypeVoIntoLabelCoroutine(NormalizeVoTextForCard(line)));
        }

        /// <summary>Unisce capi e spazi così il wrapping USS è fluido (no blocchi tipo titolo centrato).</summary>
        private static string NormalizeVoTextForCard(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return line;
            var compact = line.Replace("\r\n", "\n").Trim();
            var sb = new StringBuilder(compact.Length);
            bool pendingSpace = false;
            for (int i = 0; i < compact.Length; i++)
            {
                char c = compact[i];
                if (c == '\n')
                {
                    pendingSpace = true;
                    continue;
                }

                if (char.IsWhiteSpace(c))
                {
                    pendingSpace = true;
                    continue;
                }

                if (pendingSpace && sb.Length > 0)
                    sb.Append(' ');
                pendingSpace = false;
                sb.Append(c);
            }

            return sb.ToString();
        }

        private void StopPlantCardVoTyping()
        {
            if (_voTextTypeRoutine == null)
                return;
            StopCoroutine(_voTextTypeRoutine);
            _voTextTypeRoutine = null;
        }

        private IEnumerator TypeVoIntoLabelCoroutine(string line)
        {
            _voTextLabel.text = string.Empty;
            float cps = Mathf.Max(4f, _voCharsPerSecond);
            float delay = 1f / cps;

            for (int i = 1; i <= line.Length; i++)
            {
                _voTextLabel.text = line.Substring(0, i);
                if (i < line.Length)
                    yield return new WaitForSeconds(delay);
            }

            _voTextTypeRoutine = null;
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

            CancelVoReaction();
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

        private void HandlePhSystemChanged(float newPh, float delta)
        {
            if (_isVisible)
                RequestRealtimeRefresh(playVo: false);
        }
    }
}
