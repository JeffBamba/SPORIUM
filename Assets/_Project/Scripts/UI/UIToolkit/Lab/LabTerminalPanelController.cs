using System;
using System.Linq;
using System.Collections.Generic;
using _Project;
using _Project.Sporae.Core;
using _Project.Sporae.Core.Knowledge;
using _Project.Sporae.Core.LabBlueprint;
using _Project.Systems.SeedStorage;
using Sporae.Core;
using Sporae.Core.Localization;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.PlayerInventory;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sporae.UI.UIToolkit.Lab
{
    [RequireComponent(typeof(UIDocument))]
    public class LabTerminalPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private Extractor _extractor;
        [SerializeField] private LabExtractorPanelController _extractorPanel;
        [SerializeField] private LabCatalizzatorePanelController _catalizzatorePanel;
        [SerializeField] private LabFusionPanelController _fusionPanel;
        [SerializeField] private LabIncubatorPanelController _incubatorPanel;
        [SerializeField] private LabBlueprintMaterialGateController _lab40MaterialGate;
        [SerializeField] private float _analysisDurationSeconds = 1.8f;

        private VisualElement _root;
        private VisualElement _overlay;
        private VisualElement _projectBoard;
        private VisualElement _machinesContent;
        private Button _btnClose;
        private Button _btnToggleMachinesSection;
        private Button _btnOpenExtractor;
        private Button _btnOpenCatalizzatore;
        private Button _btnOpenFusion;
        private Button _btnOpenIncubator;
        private Button _btnCreateProject;
        private Button _btnCancelProject;
        private Button _btnOpenCurrentStep;
        private Label _machineExtractorStatus;
        private Label _machineCatalizzatoreStatus;
        private Label _machineFusionStatus;
        private Label _machineIncubatorStatus;
        private Label _projectStatusLabel;
        private Label _projectStepExtractor;
        private Label _projectStepCatalizzatore;
        private Label _projectStepFusion;
        private Label _projectStepIncubator;
        private Label _projectQuickIntroLabel;
        private Label _projectQuickWhatNowLabel;
        private Label _projectQuickLiveLabel;
        private Label _projectQuickOutcomeLabel;
        private VisualElement _analysisBlock;
        private VisualElement _analysisProgressFill;
        private Label _analysisStatusLabel;
        private Label _analysisSelectedTitleLabel;
        private Label _analysisSelectedProjectLabel;
        private Label _analysisRequiredItemsLabel;
        private Label _analysisExecutionStatusLabel;
        private Label _analysisChangeWarningLabel;
        private Button _btnAnalysisOpenCurrentStep;
        private Button _btnAnalysisCancelSelection;
        private Button _btnProjectTypeReplica;
        private Button _btnProjectTypeHybrid;
        private Button _btnProjectTypeNewProfile;

        // LAB 4.0 — Schermata 1
        private VisualElement _lab40Screen1;
        private VisualElement _lab40KnowledgeBarFill;
        private Label _lab40KnowledgeScoreLabel;
        private Label _lab40RegistryStatusLabel;
        private Label _lab40VoTextLabel;
        private Button _btnLab40CtaGenoscrittore;
        private Button _btnLab40Screen1Back;
        private bool _lab40Screen1Open;

        private bool _uiBound;
        private bool _projectActive;
        private bool _projectCompletedSinceLastOpen;
        private bool _machinesSectionCollapsed;
        private bool _machinesAutoCollapsedForCurrentProject;
        private GameManager _gameManager;
        private int _projectBaseRawSporeCount;
        private int _projectBaseMaturedSporeCount;
        private int _projectBasePreSeedCount;
        private int _projectBaseSeedCount;
        private float _analysisStartTime;
        private bool _analysisRunning;
        private bool _analysisCompleted;
        private bool _analysisScreenOpen;
        private SeedProjectType _selectedProjectType;
        private SeedProjectType _analysisFocusedProjectType;
        private SeedProjectType _initialProjectType;
        private ProjectTypeAnalysis _projectTypeAnalysis;
        private string _projectDirectionChangedMessage;
        private string _activeLabProjectKey;
        private bool _labKnowledgeCompletionHandled;

        private enum ProjectStep
        {
            Extractor,
            Catalizzatore,
            Fusion,
            Incubator,
            Completed
        }

        private enum StepVisualState
        {
            Blocked,
            Todo,
            InProgress,
            Completed
        }

        private enum SeedProjectType
        {
            None,
            Replica,
            Hybrid,
            NewProfile
        }

        private readonly struct ProjectTypeAdvice
        {
            public readonly bool AvailableNow;
            public readonly string Advice;

            public ProjectTypeAdvice(bool availableNow, string advice)
            {
                AvailableNow = availableNow;
                Advice = advice;
            }
        }

        private readonly struct ProjectTypeAnalysis
        {
            public readonly int PlayerFruitTotal;
            public readonly int StorageFruitTotal;
            public readonly int DistinctFruitTypes;
            public readonly int BestDuplicateFruitCount;
            public readonly string BestDuplicateFruitTypeId;
            public readonly int ReagentXCount;
            public readonly int ReagentYCount;
            public readonly bool HasReagentX;
            public readonly bool HasReagentY;
            public readonly ProjectTypeAdvice Replica;
            public readonly ProjectTypeAdvice Hybrid;
            public readonly ProjectTypeAdvice NewProfile;

            public ProjectTypeAnalysis(
                int playerFruitTotal,
                int storageFruitTotal,
                int distinctFruitTypes,
                int bestDuplicateFruitCount,
                string bestDuplicateFruitTypeId,
                int reagentXCount,
                int reagentYCount,
                bool hasReagentX,
                bool hasReagentY,
                ProjectTypeAdvice replica,
                ProjectTypeAdvice hybrid,
                ProjectTypeAdvice newProfile)
            {
                PlayerFruitTotal = playerFruitTotal;
                StorageFruitTotal = storageFruitTotal;
                DistinctFruitTypes = distinctFruitTypes;
                BestDuplicateFruitCount = bestDuplicateFruitCount;
                BestDuplicateFruitTypeId = bestDuplicateFruitTypeId;
                ReagentXCount = reagentXCount;
                ReagentYCount = reagentYCount;
                HasReagentX = hasReagentX;
                HasReagentY = hasReagentY;
                Replica = replica;
                Hybrid = hybrid;
                NewProfile = newProfile;
            }
        }

        private static readonly Color StatusColorIdle = new Color(0.66f, 0.85f, 1f, 0.95f);
        private static readonly Color StatusColorTodo = new Color(0.63f, 0.9f, 1f, 1f);
        private static readonly Color StatusColorBlocked = new Color(0.43f, 0.48f, 0.53f, 0.96f);
        private static readonly Color StatusColorInProgress = new Color(0.79f, 0.55f, 1f, 1f); // viola
        private static readonly Color StatusColorCompleted = new Color(0.5f, 1f, 0.48f, 1f); // verde

        private readonly struct ProjectRuntimeSnapshot
        {
            public readonly bool ExtractorInProgress;
            public readonly bool ExtractorStepDone;
            public readonly bool ExtractorCollectedForProject;
            public readonly int ExtractorProgressPct;
            public readonly int ExtractorPendingRawCount;
            public readonly bool CatalizzatoreInProgress;
            public readonly bool CatalizzatoreStepDone;
            public readonly bool CatalizzatoreCollectedForProject;
            public readonly int CatalizzatoreReadyCount;
            public readonly bool FusionInProgress;
            public readonly bool FusionStepDone;
            public readonly bool FusionCollectedForProject;
            public readonly int FusionProgressPct;
            public readonly int FusionReadyCount;
            public readonly bool IncubatorInProgress;
            public readonly bool IncubatorStepDone;
            public readonly bool IncubatorCollectedForProject;
            public readonly int IncubatorDay;
            public readonly int IncubatorReadyCount;

            public ProjectRuntimeSnapshot(
                bool extractorInProgress,
                bool extractorStepDone,
                bool extractorCollectedForProject,
                int extractorProgressPct,
                int extractorPendingRawCount,
                bool catalizzatoreInProgress,
                bool catalizzatoreStepDone,
                bool catalizzatoreCollectedForProject,
                int catalizzatoreReadyCount,
                bool fusionInProgress,
                bool fusionStepDone,
                bool fusionCollectedForProject,
                int fusionProgressPct,
                int fusionReadyCount,
                bool incubatorInProgress,
                bool incubatorStepDone,
                bool incubatorCollectedForProject,
                int incubatorDay,
                int incubatorReadyCount)
            {
                ExtractorInProgress = extractorInProgress;
                ExtractorStepDone = extractorStepDone;
                ExtractorCollectedForProject = extractorCollectedForProject;
                ExtractorProgressPct = extractorProgressPct;
                ExtractorPendingRawCount = extractorPendingRawCount;
                CatalizzatoreInProgress = catalizzatoreInProgress;
                CatalizzatoreStepDone = catalizzatoreStepDone;
                CatalizzatoreCollectedForProject = catalizzatoreCollectedForProject;
                CatalizzatoreReadyCount = catalizzatoreReadyCount;
                FusionInProgress = fusionInProgress;
                FusionStepDone = fusionStepDone;
                FusionCollectedForProject = fusionCollectedForProject;
                FusionProgressPct = fusionProgressPct;
                FusionReadyCount = fusionReadyCount;
                IncubatorInProgress = incubatorInProgress;
                IncubatorStepDone = incubatorStepDone;
                IncubatorCollectedForProject = incubatorCollectedForProject;
                IncubatorDay = incubatorDay;
                IncubatorReadyCount = incubatorReadyCount;
            }
        }

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null)
                _uiDocument.sortingOrder = 430;

            // LAB 4.0: auto-risoluzione gate se non assegnato via Inspector.
            // FindObjectOfType è accettabile qui (UI init, non gameplay loop) —
            // LabBlueprintMaterialGateController si trova su LabTerminal in ROOM_Dome,
            // ramo separato rispetto a questo pannello UI.
            if (_lab40MaterialGate == null)
                _lab40MaterialGate = FindObjectOfType<LabBlueprintMaterialGateController>(includeInactive: true);

            _root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (_root != null)
                TryBindUI();
        }

        private void Start()
        {
            EnsureGameManager();
            GameLanguageSettings.OnLanguageChanged += OnLanguageChanged;
            Hide();
        }

        private void OnLanguageChanged(GameLanguage _)
        {
            if (_analysisCompleted && !_analysisRunning)
                _projectTypeAnalysis = BuildProjectTypeAnalysis();
            ApplyLabTerminalStaticChrome();
            if (gameObject.activeInHierarchy && _overlay != null && _overlay.resolvedStyle.display == DisplayStyle.Flex)
                RefreshDisplay();
        }

        private void OnDestroy()
        {
            GameLanguageSettings.OnLanguageChanged -= OnLanguageChanged;

            if (_lab40MaterialGate != null)
            {
                _lab40MaterialGate.DraftStarted -= OnLab40DraftStarted;
                _lab40MaterialGate.MaterialSelectionCancelled -= OnLab40MaterialCancelled;
            }
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (Input.GetKeyDown(KeyCode.Escape) && TryConsumeLabTerminalEscape())
                return;

            if (_analysisRunning)
            {
                float elapsed = Time.unscaledTime - _analysisStartTime;
                if (elapsed >= Mathf.Max(0.15f, _analysisDurationSeconds))
                {
                    _analysisRunning = false;
                    _analysisCompleted = true;
                    _projectTypeAnalysis = BuildProjectTypeAnalysis();
                    _selectedProjectType = SeedProjectType.None;
                    _analysisFocusedProjectType = SeedProjectType.None;
                    _initialProjectType = SeedProjectType.None;
                }
            }

            RefreshDisplay();
        }

        /// <summary>
        /// Gate LAB 4.0 post-scansione: readiness inventario → picker frutto XOR spora → draft blueprint.
        /// Chiamata dalla Schermata 1 (Task 3) o da test/debug.
        /// </summary>
        public void BeginLab40MaterialGate()
        {
            if (_lab40MaterialGate == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "[LabTerminal] LabBlueprintMaterialGateController non assegnato.");
                return;
            }

            _lab40MaterialGate.BeginMaterialSelection();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            GameplayUiModalLock.SetMachineModalState(true);
            TryBindUI();

            if (_root != null)
                _root.pickingMode = PickingMode.Position;

            // LAB 4.0: il terminale apre sempre Schermata 1.
            // L'overlay è solo il wrapper modale che centra il pannello 1480x900.
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.Flex;
                _overlay.pickingMode = PickingMode.Position;
            }

            ShowLab40Screen1();
        }

        public void Hide()
        {
            GameplayUiModalLock.SetMachineModalState(false);
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.None;
                _overlay.pickingMode = PickingMode.Ignore;
            }

            // Nasconde anche Schermata 1 se aperta
            if (_lab40Screen1 != null)
            {
                _lab40Screen1.style.display = DisplayStyle.None;
                _lab40Screen1Open = false;
            }

            if (_root != null)
                _root.pickingMode = PickingMode.Ignore;

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Esc: durante analisi in corso annulla il flusso progetto; sulla scheda scelta tipo torna al board;
        /// altrimenti chiude il terminale (stesso effetto del pulsante chiudi).
        /// </summary>
        private bool TryConsumeLabTerminalEscape()
        {
            // Esc sulla Schermata 1 chiude il terminale
            if (_lab40Screen1Open)
            {
                HideLab40Screen1();
                return true;
            }

            if (_overlay == null || _overlay.style.display != DisplayStyle.Flex)
                return false;

            if (_analysisRunning)
            {
                CancelProjectTypeSelection();
                return true;
            }

            if (_projectActive && _analysisCompleted && _analysisScreenOpen)
            {
                _analysisScreenOpen = false;
                _selectedProjectType = SeedProjectType.None;
                _analysisFocusedProjectType = SeedProjectType.None;
                _initialProjectType = SeedProjectType.None;
                _projectDirectionChangedMessage = string.Empty;
                RefreshDisplay();
                return true;
            }

            Hide();
            return true;
        }

        private void TryBindUI()
        {
            if (_uiDocument != null)
            {
                var currentRoot = _uiDocument.rootVisualElement;
                if (currentRoot != null && currentRoot != _root)
                {
                    _root = currentRoot;
                    _uiBound = false;
                }
            }

            if (_uiBound)
                return;

            if (_root == null && _uiDocument != null)
                _root = _uiDocument.rootVisualElement;
            if (_root == null)
                return;

            _overlay = _root.Q<VisualElement>("lab-terminal-overlay");
            _projectBoard = _root.Q<VisualElement>("lab-terminal-project-board");
            _machinesContent = _root.Q<VisualElement>("lab-terminal-machines-content");
            _btnClose = _root.Q<Button>("btn-close");
            _btnToggleMachinesSection = _root.Q<Button>("btn-toggle-machines-section");
            _btnOpenExtractor = _root.Q<Button>("btn-open-extractor");
            _btnOpenCatalizzatore = _root.Q<Button>("btn-open-catalizzatore");
            _btnOpenFusion = _root.Q<Button>("btn-open-fusion");
            _btnOpenIncubator = _root.Q<Button>("btn-open-incubator");
            _btnCreateProject = _root.Q<Button>("btn-create-project");
            _btnCancelProject = _root.Q<Button>("btn-cancel-project");
            _btnOpenCurrentStep = _root.Q<Button>("btn-open-current-step");
            _machineExtractorStatus = _root.Q<Label>("lab-terminal-machine-extractor-status");
            _machineCatalizzatoreStatus = _root.Q<Label>("lab-terminal-machine-catalizzatore-status");
            _machineFusionStatus = _root.Q<Label>("lab-terminal-machine-fusion-status");
            _machineIncubatorStatus = _root.Q<Label>("lab-terminal-machine-incubator-status");
            _projectStatusLabel = _root.Q<Label>("lab-terminal-project-status");
            _projectStepExtractor = _root.Q<Label>("lab-terminal-step-extractor");
            _projectStepCatalizzatore = _root.Q<Label>("lab-terminal-step-catalizzatore");
            _projectStepFusion = _root.Q<Label>("lab-terminal-step-fusion");
            _projectStepIncubator = _root.Q<Label>("lab-terminal-step-incubator");
            _projectQuickIntroLabel = _root.Q<Label>("lab-terminal-quick-intro");
            _projectQuickWhatNowLabel = _root.Q<Label>("lab-terminal-quick-what-now");
            _projectQuickLiveLabel = _root.Q<Label>("lab-terminal-quick-live");
            _projectQuickOutcomeLabel = _root.Q<Label>("lab-terminal-quick-outcome");
            _analysisBlock = _root.Q<VisualElement>("lab-terminal-analysis-block");
            _analysisProgressFill = _root.Q<VisualElement>("lab-terminal-analysis-progress-fill");
            _analysisStatusLabel = _root.Q<Label>("lab-terminal-analysis-status");
            _analysisSelectedTitleLabel = _root.Q<Label>("lab-terminal-analysis-selected-title");
            _analysisSelectedProjectLabel = _root.Q<Label>("lab-terminal-analysis-selected-project");
            _analysisRequiredItemsLabel = _root.Q<Label>("lab-terminal-analysis-required-items");
            _analysisExecutionStatusLabel = _root.Q<Label>("lab-terminal-analysis-execution-status");
            _analysisChangeWarningLabel = _root.Q<Label>("lab-terminal-analysis-change-warning");
            _btnAnalysisOpenCurrentStep = _root.Q<Button>("btn-analysis-open-current-step");
            _btnAnalysisCancelSelection = _root.Q<Button>("btn-analysis-cancel-selection");
            _btnProjectTypeReplica = _root.Q<Button>("btn-project-type-replica");
            _btnProjectTypeHybrid = _root.Q<Button>("btn-project-type-hybrid");
            _btnProjectTypeNewProfile = _root.Q<Button>("btn-project-type-new-profile");
            if (_projectQuickIntroLabel != null) _projectQuickIntroLabel.enableRichText = true;
            if (_projectQuickWhatNowLabel != null) _projectQuickWhatNowLabel.enableRichText = true;
            if (_projectQuickLiveLabel != null) _projectQuickLiveLabel.enableRichText = true;
            if (_projectQuickOutcomeLabel != null) _projectQuickOutcomeLabel.enableRichText = true;
            if (_analysisStatusLabel != null) _analysisStatusLabel.enableRichText = true;
            if (_analysisSelectedTitleLabel != null) _analysisSelectedTitleLabel.enableRichText = true;
            if (_analysisSelectedProjectLabel != null) _analysisSelectedProjectLabel.enableRichText = true;
            if (_analysisRequiredItemsLabel != null) _analysisRequiredItemsLabel.enableRichText = true;
            if (_analysisExecutionStatusLabel != null) _analysisExecutionStatusLabel.enableRichText = true;
            if (_analysisChangeWarningLabel != null) _analysisChangeWarningLabel.enableRichText = true;

            if (_btnClose != null)
                _btnClose.clicked += Hide;
            if (_btnToggleMachinesSection != null)
                _btnToggleMachinesSection.clicked += ToggleMachinesSection;

            if (_btnOpenExtractor != null)
                _btnOpenExtractor.clicked += OpenExtractorPanel;
            if (_btnOpenCatalizzatore != null)
                _btnOpenCatalizzatore.clicked += OpenCatalizzatorePanel;
            if (_btnOpenFusion != null)
                _btnOpenFusion.clicked += OpenFusionPanel;
            if (_btnOpenIncubator != null)
                _btnOpenIncubator.clicked += OpenIncubatorPanel;

            if (_btnCreateProject != null)
            {
                _btnCreateProject.clicked += () =>
                {
                    ShowLab40Screen1();
                };
            }

            if (_btnCancelProject != null)
            {
                _btnCancelProject.clicked += () =>
                {
                    if (_projectActive && !string.IsNullOrEmpty(_activeLabProjectKey))
                        SeedProjectKnowledgeHooks.NotifyProjectAbandoned(_activeLabProjectKey);
                    _projectActive = false;
                    _analysisRunning = false;
                    _analysisCompleted = false;
                    _analysisScreenOpen = false;
                    _selectedProjectType = SeedProjectType.None;
                    _analysisFocusedProjectType = SeedProjectType.None;
                    _initialProjectType = SeedProjectType.None;
                    _projectDirectionChangedMessage = string.Empty;
                    _machinesAutoCollapsedForCurrentProject = false;
                    SetMachinesSectionCollapsed(false);
                    RefreshDisplay();
                };
            }

            if (_btnOpenCurrentStep != null)
                _btnOpenCurrentStep.clicked += OpenCurrentProjectStep;
            if (_btnProjectTypeReplica != null)
                _btnProjectTypeReplica.clicked += () => SelectProjectType(SeedProjectType.Replica);
            if (_btnProjectTypeHybrid != null)
                _btnProjectTypeHybrid.clicked += () => SelectProjectType(SeedProjectType.Hybrid);
            if (_btnProjectTypeNewProfile != null)
                _btnProjectTypeNewProfile.clicked += () => SelectProjectType(SeedProjectType.NewProfile);
            // LAB 4.0 — Schermata 1
            _lab40Screen1 = _root.Q<VisualElement>("lab40-screen1");
            _lab40KnowledgeBarFill = _root.Q<VisualElement>("lab40-s1-know-bar-fill");
            _lab40KnowledgeScoreLabel = _root.Q<Label>("lab40-s1-know-score");
            _lab40RegistryStatusLabel = _root.Q<Label>("lab40-s1-registry-status");
            _lab40VoTextLabel = _root.Q<Label>("lab40-s1-vo-text");
            _btnLab40CtaGenoscrittore = _root.Q<Button>("btn-lab40-cta-genoscrittore");
            _btnLab40Screen1Back = _root.Q<Button>("btn-lab40-screen1-back");

            if (_btnLab40Screen1Back != null)
                _btnLab40Screen1Back.clicked += HideLab40Screen1;
            if (_btnLab40CtaGenoscrittore != null)
                _btnLab40CtaGenoscrittore.clicked += OnLab40CtaClicked;

            // Nasconde la Schermata 1 all'avvio (builder la mostra per authoring)
            if (_lab40Screen1 != null)
                _lab40Screen1.style.display = DisplayStyle.None;

            // Subscribe agli eventi del gate (se assegnato)
            if (_lab40MaterialGate != null)
            {
                _lab40MaterialGate.DraftStarted += OnLab40DraftStarted;
                _lab40MaterialGate.MaterialSelectionCancelled += OnLab40MaterialCancelled;
            }

            if (_btnAnalysisOpenCurrentStep != null)
                _btnAnalysisOpenCurrentStep.clicked += OpenCurrentStepFromAnalysis;
            if (_btnAnalysisCancelSelection != null)
                _btnAnalysisCancelSelection.clicked += CancelProjectTypeSelection;

            _uiBound = true;
            ApplyLabTerminalStaticChrome();
        }

        private void ApplyLabTerminalStaticChrome()
        {
            if (_root == null)
                return;

            var panel = _root.Q<VisualElement>("lab-terminal-panel");
            if (panel == null)
                return;

            var mainTitle = panel.Q<Label>(className: "lab-terminal-title");
            if (mainTitle != null) mainTitle.text = LocalizationManager.GetString("lab_terminal.chrome.title");

            var subtitle = panel.Q<Label>(className: "lab-terminal-subtitle");
            if (subtitle != null) subtitle.text = LocalizationManager.GetString("lab_terminal.chrome.subtitle");

            var standalone = panel.Q<VisualElement>(className: "lab-terminal-section-standalone");
            var machinesHeader = standalone?.Q<VisualElement>(className: "lab-terminal-section-header")?.Q<Label>(className: "lab-terminal-section-title");
            if (machinesHeader != null) machinesHeader.text = LocalizationManager.GetString("lab_terminal.chrome.section_machines");

            if (_machinesContent != null)
            {
                var hint = _machinesContent.Q<Label>(className: "lab-terminal-section-hint");
                if (hint != null) hint.text = LocalizationManager.GetString("lab_terminal.chrome.machines_hint");
            }

            void StyleMachineRow(Button openBtn, string machineNameKey)
            {
                if (openBtn == null) return;
                var row = openBtn.parent;
                var nm = row?.Q<Label>(className: "lab-terminal-machine-name");
                if (nm != null) nm.text = LocalizationManager.GetString(machineNameKey);
                openBtn.text = LocalizationManager.GetString("lab_terminal.chrome.btn_open");
            }

            StyleMachineRow(_btnOpenExtractor, "lab_terminal.chrome.machine_extractor");
            StyleMachineRow(_btnOpenCatalizzatore, "lab_terminal.chrome.machine_catalizzatore");
            StyleMachineRow(_btnOpenFusion, "lab_terminal.chrome.machine_fusion");
            StyleMachineRow(_btnOpenIncubator, "lab_terminal.chrome.machine_incubator");

            var projectSection = panel.Q<VisualElement>(className: "lab-terminal-section-project");
            if (projectSection != null && projectSection.childCount > 0 && projectSection[0] is Label projectHead)
                projectHead.text = LocalizationManager.GetString("lab_terminal.chrome.project_title");

            var quickStaticHead = panel.Q<Label>(className: "lab-terminal-quick-title-static");
            if (quickStaticHead != null) quickStaticHead.text = LocalizationManager.GetString("lab_terminal.chrome.quick_static_title");
            var quickDynHead = panel.Q<Label>(className: "lab-terminal-quick-title-dynamic");
            if (quickDynHead != null) quickDynHead.text = LocalizationManager.GetString("lab_terminal.chrome.quick_dynamic_title");

            if (_projectQuickIntroLabel != null)
                _projectQuickIntroLabel.text = LocalizationManager.GetString("lab_terminal.chrome.quick_intro_placeholder");
            if (_projectQuickWhatNowLabel != null)
                _projectQuickWhatNowLabel.text = LocalizationManager.GetString("lab_terminal.chrome.quick_what_placeholder");
            if (_projectQuickLiveLabel != null)
                _projectQuickLiveLabel.text = LocalizationManager.GetString("lab_terminal.chrome.quick_live_placeholder");
            if (_projectQuickOutcomeLabel != null)
                _projectQuickOutcomeLabel.text = LocalizationManager.GetString("lab_terminal.chrome.quick_outcome_placeholder");

            if (_btnCreateProject != null) _btnCreateProject.text = LocalizationManager.GetString("lab_terminal.chrome.btn_create");
            if (_btnCancelProject != null) _btnCancelProject.text = LocalizationManager.GetString("lab_terminal.chrome.btn_cancel");

            var ph = panel.Q<VisualElement>("lab-terminal-project-image-placeholder")?.Q<Label>(className: "lab-terminal-project-image-placeholder-label");
            if (ph != null) ph.text = LocalizationManager.GetString("lab_terminal.chrome.placeholder_image");

            if (_projectBoard != null)
            {
                var boardTitle = _projectBoard.Q<Label>(className: "lab-terminal-board-title");
                if (boardTitle != null) boardTitle.text = LocalizationManager.GetString("lab_terminal.chrome.board_title");
                var legend = _projectBoard.Q<VisualElement>(className: "lab-terminal-board-legend");
                if (legend != null)
                {
                    var legTitle = legend.Q<Label>(className: "lab-terminal-legend-title");
                    if (legTitle != null) legTitle.text = LocalizationManager.GetString("lab_terminal.chrome.legend_title");
                    var li = legend.Q<Label>(className: "lab-terminal-legend-inprogress");
                    if (li != null) li.text = LocalizationManager.GetString("lab_terminal.chrome.legend_inprogress");
                    var lc = legend.Q<Label>(className: "lab-terminal-legend-completed");
                    if (lc != null) lc.text = LocalizationManager.GetString("lab_terminal.chrome.legend_completed");
                    var lt = legend.Q<Label>(className: "lab-terminal-legend-todo");
                    if (lt != null) lt.text = LocalizationManager.GetString("lab_terminal.chrome.legend_todo");
                    var lb = legend.Q<Label>(className: "lab-terminal-legend-blocked");
                    if (lb != null) lb.text = LocalizationManager.GetString("lab_terminal.chrome.legend_blocked");
                }
            }

            if (_analysisBlock != null)
            {
                var at = _analysisBlock.Q<Label>(className: "lab-terminal-analysis-title");
                if (at != null) at.text = LocalizationManager.GetString("lab_terminal.chrome.analysis_title");
                if (_analysisStatusLabel != null && !_analysisRunning)
                    _analysisStatusLabel.text = LocalizationManager.GetString("lab_terminal.chrome.analysis_status_prep");
                var sub = _analysisBlock.Q<Label>(className: "lab-terminal-analysis-subtitle");
                if (sub != null) sub.text = LocalizationManager.GetString("lab_terminal.chrome.analysis_project_type_caption");
                if (_btnProjectTypeReplica != null) _btnProjectTypeReplica.text = LocalizationManager.GetString("lab_terminal.chrome.type_replica");
                if (_btnProjectTypeHybrid != null) _btnProjectTypeHybrid.text = LocalizationManager.GetString("lab_terminal.chrome.type_hybrid");
                if (_btnProjectTypeNewProfile != null) _btnProjectTypeNewProfile.text = LocalizationManager.GetString("lab_terminal.chrome.type_new_profile");
                if (_analysisSelectedTitleLabel != null)
                    _analysisSelectedTitleLabel.text = LocalizationManager.GetString("lab_terminal.chrome.analysis_sheet_title");
                if (_analysisSelectedProjectLabel != null)
                    _analysisSelectedProjectLabel.text = LocalizationManager.GetString("lab_terminal.chrome.analysis_project_line");
                if (_analysisRequiredItemsLabel != null)
                    _analysisRequiredItemsLabel.text = LocalizationManager.GetString("lab_terminal.chrome.analysis_required_placeholder");
                if (_analysisExecutionStatusLabel != null)
                    _analysisExecutionStatusLabel.text = LocalizationManager.GetString("lab_terminal.chrome.analysis_exec_placeholder");
                if (_btnAnalysisCancelSelection != null)
                    _btnAnalysisCancelSelection.text = LocalizationManager.GetString("lab_terminal.chrome.analysis_cancel");
                if (_btnAnalysisOpenCurrentStep != null)
                    _btnAnalysisOpenCurrentStep.text = LocalizationManager.GetString("lab_terminal.btn_open_step");
            }

            if (_btnOpenCurrentStep != null)
                _btnOpenCurrentStep.text = LocalizationManager.GetString("lab_terminal.btn_open_step");
        }

        private void OpenExtractorPanel()
        {
            if (_extractorPanel == null)
                return;
            _extractorPanel.Show();
            Hide();
        }

        private void OpenCatalizzatorePanel()
        {
            if (_catalizzatorePanel == null)
                return;
            _catalizzatorePanel.Show();
            Hide();
        }

        private void OpenFusionPanel()
        {
            if (_fusionPanel == null)
                return;
            _fusionPanel.Show();
            Hide();
        }

        private void OpenIncubatorPanel()
        {
            if (_incubatorPanel == null)
                return;
            _incubatorPanel.Show();
            Hide();
        }

        private void OpenCurrentProjectStep()
        {
            if (!_analysisCompleted)
                return;

            ProjectRuntimeSnapshot snapshot = BuildProjectRuntimeSnapshot();
            switch (GetCurrentProjectStep(snapshot))
            {
                case ProjectStep.Extractor:
                    OpenExtractorPanel();
                    break;
                case ProjectStep.Catalizzatore:
                    OpenCatalizzatorePanel();
                    break;
                case ProjectStep.Fusion:
                    OpenFusionPanel();
                    break;
                case ProjectStep.Incubator:
                    OpenIncubatorPanel();
                    break;
            }
        }

        private void OpenCurrentStepFromAnalysis()
        {
            if (!_analysisCompleted || _selectedProjectType == SeedProjectType.None)
                return;

            _analysisScreenOpen = false;
            OpenCurrentProjectStep();
        }

        private void StartProjectWithAnalysis()
        {
            EnsureGameManager();
            CaptureProjectBaselineCounts();
            _labKnowledgeCompletionHandled = false;
            _projectActive = true;
            _projectCompletedSinceLastOpen = false;
            _machinesAutoCollapsedForCurrentProject = false;
            _analysisRunning = true;
            _analysisCompleted = false;
            _analysisScreenOpen = true;
            _analysisStartTime = Time.unscaledTime;
            _selectedProjectType = SeedProjectType.None;
            _analysisFocusedProjectType = SeedProjectType.None;
            _initialProjectType = SeedProjectType.None;
            _projectDirectionChangedMessage = string.Empty;
            _projectTypeAnalysis = default;
            RefreshDisplay();
        }

        // ──────────────────────────────────────────────────────────────
        // LAB 4.0 — Schermata 1 (Genoscrittore onboarding)
        // ──────────────────────────────────────────────────────────────

        private void ShowLab40Screen1()
        {
            if (_lab40Screen1 == null)
                return;

            RefreshLab40Screen1Data();
            _lab40Screen1.style.display = DisplayStyle.Flex;
            _lab40Screen1Open = true;
        }

        private void HideLab40Screen1()
        {
            if (_lab40Screen1 == null)
                return;

            _lab40Screen1.style.display = DisplayStyle.None;
            _lab40Screen1Open = false;
            // INDIETRO dalla Schermata 1 chiude il terminale
            Hide();
        }

        private void RefreshLab40Screen1Data()
        {
            var knowledge = ServiceContainer.Instance?.Get<KnowledgeProgressionService>(suppressWarning: true);
            if (knowledge != null)
            {
                var tier = knowledge.CurrentTier;
                int score = knowledge.TotalScore;
                // Rank va da 0 (Neofita) a 5 (Maestro): usa (rank+1)/6 come fill visuale
                const int maxRank = 5;
                float pct = Mathf.Clamp01((tier.Rank + 1) / (float)(maxRank + 1));

                if (_lab40KnowledgeBarFill != null)
                    _lab40KnowledgeBarFill.style.width = Length.Percent(Mathf.RoundToInt(pct * 100f));
                if (_lab40KnowledgeScoreLabel != null)
                    _lab40KnowledgeScoreLabel.text = $"{score}  [{knowledge.GetTierLabelLocalized()}]";
            }

            var blueprint = ServiceContainer.Instance?.Get<LabBlueprintService>(suppressWarning: true);
            bool hasActive = blueprint != null && blueprint.HasDraftOrActiveProject;
            if (_lab40RegistryStatusLabel != null)
                _lab40RegistryStatusLabel.text = hasActive
                    ? LocalizationManager.GetString("lab40.s1.registry_active")
                    : LocalizationManager.GetString("lab40.s1.registry_idle");

            if (_lab40VoTextLabel != null)
                _lab40VoTextLabel.text = LocalizationManager.GetString("lab40.s1.vo_text");

            if (_btnLab40CtaGenoscrittore != null)
                _btnLab40CtaGenoscrittore.text = LocalizationManager.GetString("lab40.s1.btn_cta");
            if (_btnLab40Screen1Back != null)
                _btnLab40Screen1Back.text = LocalizationManager.GetString("lab40.s1.btn_back");
        }

        private void OnLab40CtaClicked()
        {
            HideLab40Screen1();
            BeginLab40MaterialGate();
        }

        private void OnLab40DraftStarted(LabBlueprintState state)
        {
            // Bozza avviata: il picker ha già chiuso, ora aggiorna il board.
            // Task 4 aprirà la Schermata 2 qui.
            RefreshDisplay();
        }

        private void OnLab40MaterialCancelled()
        {
            // Il giocatore ha chiuso il picker senza scegliere: nessun avanzamento.
        }

        private void SelectProjectType(SeedProjectType type)
        {
            if (!_analysisCompleted)
                return;

            _analysisFocusedProjectType = type;
            bool available = IsProjectTypeAvailable(type);

            if (!available)
            {
                _selectedProjectType = SeedProjectType.None;
                _projectDirectionChangedMessage = string.Empty;
                RefreshDisplay();
                return;
            }

            if (_selectedProjectType != SeedProjectType.None && _selectedProjectType != type)
            {
                _projectDirectionChangedMessage = LocalizationManager.GetString("lab_terminal.direction_changed", new Dictionary<string, string>
                {
                    ["from"] = ProjectTypeLabel(_selectedProjectType),
                    ["to"] = ProjectTypeLabel(type)
                });
            }

            _selectedProjectType = type;
            if (_initialProjectType == SeedProjectType.None)
                _initialProjectType = type;
            RefreshDisplay();
        }

        private void CancelProjectTypeSelection()
        {
            _projectActive = false;
            _analysisRunning = false;
            _analysisCompleted = false;
            _analysisScreenOpen = false;
            _selectedProjectType = SeedProjectType.None;
            _analysisFocusedProjectType = SeedProjectType.None;
            _initialProjectType = SeedProjectType.None;
            _projectDirectionChangedMessage = string.Empty;
            _projectTypeAnalysis = default;
            _machinesAutoCollapsedForCurrentProject = false;
            SetMachinesSectionCollapsed(false);
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            EnsureGameManager();
            ProjectRuntimeSnapshot snapshot = BuildProjectRuntimeSnapshot();

            if (_projectActive && !_machinesAutoCollapsedForCurrentProject)
            {
                SetMachinesSectionCollapsed(true);
                _machinesAutoCollapsedForCurrentProject = true;
            }

            if (_projectActive && IsProjectFlowCompleted(snapshot))
            {
                if (!_labKnowledgeCompletionHandled && !string.IsNullOrEmpty(_activeLabProjectKey))
                {
                    _labKnowledgeCompletionHandled = true;
                    var seed = GetLatestPlayerSeed();
                    SeedProjectKnowledgeHooks.NotifyProjectCompleted(_activeLabProjectKey, seed?.GeneticTypeValue);
                }

                _projectActive = false;
                _projectCompletedSinceLastOpen = true;
                _analysisRunning = false;
                _analysisCompleted = false;
                _analysisScreenOpen = false;
                _selectedProjectType = SeedProjectType.None;
                _analysisFocusedProjectType = SeedProjectType.None;
                _initialProjectType = SeedProjectType.None;
                _projectDirectionChangedMessage = string.Empty;
                _machinesAutoCollapsedForCurrentProject = false;
                SetMachinesSectionCollapsed(false);
            }

            if (_machineExtractorStatus != null)
            {
                _machineExtractorStatus.text = snapshot.ExtractorInProgress
                    ? LocalizationManager.GetString("lab_terminal.machine.extractor_progress", new Dictionary<string, string> { ["pct"] = snapshot.ExtractorProgressPct.ToString() })
                    : snapshot.ExtractorStepDone
                        ? LocalizationManager.GetString("lab_terminal.machine.extractor_done", new Dictionary<string, string> { ["n"] = Mathf.Max(snapshot.ExtractorPendingRawCount, 1).ToString() })
                        : LocalizationManager.GetString("lab_terminal.machine.idle");
                ApplyMachineStatusStyle(_machineExtractorStatus, snapshot.ExtractorInProgress, snapshot.ExtractorStepDone);
            }

            if (_machineCatalizzatoreStatus != null)
            {
                _machineCatalizzatoreStatus.text = snapshot.CatalizzatoreInProgress
                    ? LocalizationManager.GetString("lab_terminal.machine.cat_progress")
                    : snapshot.CatalizzatoreStepDone
                        ? LocalizationManager.GetString("lab_terminal.machine.cat_done", new Dictionary<string, string> { ["n"] = Mathf.Max(snapshot.CatalizzatoreReadyCount, 1).ToString() })
                        : LocalizationManager.GetString("lab_terminal.machine.idle");
                ApplyMachineStatusStyle(_machineCatalizzatoreStatus, snapshot.CatalizzatoreInProgress, snapshot.CatalizzatoreStepDone);
            }

            if (_machineFusionStatus != null)
            {
                _machineFusionStatus.text = snapshot.FusionInProgress
                    ? LocalizationManager.GetString("lab_terminal.machine.fusion_progress", new Dictionary<string, string> { ["pct"] = snapshot.FusionProgressPct.ToString() })
                    : snapshot.FusionStepDone
                        ? LocalizationManager.GetString("lab_terminal.machine.fusion_done", new Dictionary<string, string> { ["n"] = Mathf.Max(snapshot.FusionReadyCount, 1).ToString() })
                        : LocalizationManager.GetString("lab_terminal.machine.idle");
                ApplyMachineStatusStyle(_machineFusionStatus, snapshot.FusionInProgress, snapshot.FusionStepDone);
            }

            if (_machineIncubatorStatus != null)
            {
                _machineIncubatorStatus.text = snapshot.IncubatorInProgress
                    ? LocalizationManager.GetString("lab_terminal.machine.inc_progress", new Dictionary<string, string> { ["day"] = snapshot.IncubatorDay.ToString() })
                    : snapshot.IncubatorStepDone
                        ? LocalizationManager.GetString("lab_terminal.machine.inc_done", new Dictionary<string, string> { ["n"] = Mathf.Max(snapshot.IncubatorReadyCount, 1).ToString() })
                        : LocalizationManager.GetString("lab_terminal.machine.idle");
                ApplyMachineStatusStyle(_machineIncubatorStatus, snapshot.IncubatorInProgress, snapshot.IncubatorStepDone);
            }

            if (_projectBoard != null)
                _projectBoard.style.display = _projectActive && _analysisCompleted && !_analysisScreenOpen ? DisplayStyle.Flex : DisplayStyle.None;
            if (_analysisBlock != null)
                _analysisBlock.style.display = _projectActive && _analysisScreenOpen ? DisplayStyle.Flex : DisplayStyle.None;

            if (_btnCreateProject != null)
                _btnCreateProject.style.display = _projectActive ? DisplayStyle.None : DisplayStyle.Flex;
            if (_btnCancelProject != null)
                _btnCancelProject.style.display = _projectActive ? DisplayStyle.Flex : DisplayStyle.None;
            if (_btnOpenCurrentStep != null)
                _btnOpenCurrentStep.style.display = _projectActive && _analysisCompleted && !_analysisScreenOpen ? DisplayStyle.Flex : DisplayStyle.None;

            ProjectStep currentStep = GetCurrentProjectStep(snapshot);
            ProjectStep? pendingCollectionStep = GetPendingCollectionStep(snapshot);
            bool collectGate = pendingCollectionStep.HasValue;

            if (_btnOpenCurrentStep != null && _projectActive && _analysisCompleted && !_analysisScreenOpen)
            {
                _btnOpenCurrentStep.SetEnabled(!collectGate);
                _btnOpenCurrentStep.text = collectGate
                    ? LocalizationManager.GetString("lab_terminal.btn_collect_output", new Dictionary<string, string> { ["machine"] = StepMachineName(pendingCollectionStep.Value) })
                    : LocalizationManager.GetString("lab_terminal.btn_open_step");
            }

            if (_projectStatusLabel != null)
            {
                if (_projectActive)
                {
                    if (_analysisRunning)
                    {
                        _projectStatusLabel.text = LocalizationManager.GetString("lab_terminal.status.analysis_resources");
                    }
                    else if (!_analysisCompleted)
                    {
                        _projectStatusLabel.text = LocalizationManager.GetString("lab_terminal.status.analysis_waiting");
                    }
                    else
                    {
                        _projectStatusLabel.text = collectGate
                            ? LocalizationManager.GetString("lab_terminal.status.collect_gate", new Dictionary<string, string> { ["machine"] = StepMachineName(pendingCollectionStep.Value) })
                            : currentStep == ProjectStep.Completed
                            ? LocalizationManager.GetString("lab_terminal.status.completed_seed")
                            : LocalizationManager.GetString("lab_terminal.status.active_step", new Dictionary<string, string>
                            {
                                ["type"] = ProjectTypeLabel(_selectedProjectType),
                                ["step"] = StepLabel(currentStep)
                            });
                    }
                    _projectStatusLabel.EnableInClassList("lab-terminal-project-status-active", currentStep != ProjectStep.Completed);
                    _projectStatusLabel.EnableInClassList("lab-terminal-project-status-complete", currentStep == ProjectStep.Completed);
                    _projectStatusLabel.style.color = _analysisRunning
                        ? StatusColorTodo
                        : currentStep == ProjectStep.Completed ? StatusColorCompleted : StatusColorInProgress;
                }
                else
                {
                    _projectStatusLabel.text = _projectCompletedSinceLastOpen
                        ? LocalizationManager.GetString("lab_terminal.status.last_done")
                        : LocalizationManager.GetString("lab_terminal.status.no_project");
                    _projectStatusLabel.EnableInClassList("lab-terminal-project-status-active", false);
                    _projectStatusLabel.EnableInClassList("lab-terminal-project-status-complete", _projectCompletedSinceLastOpen);
                    _projectStatusLabel.style.color = _projectCompletedSinceLastOpen ? StatusColorCompleted : StatusColorIdle;
                }
            }

            if (_projectActive)
            {
                bool extractorCollectGate = pendingCollectionStep == ProjectStep.Extractor;
                bool catalizzatoreCollectGate = pendingCollectionStep == ProjectStep.Catalizzatore;
                bool fusionCollectGate = pendingCollectionStep == ProjectStep.Fusion;
                bool incubatorCollectGate = pendingCollectionStep == ProjectStep.Incubator;

                bool catUnlocked = !extractorCollectGate && (snapshot.ExtractorStepDone || snapshot.CatalizzatoreInProgress || snapshot.CatalizzatoreStepDone || snapshot.FusionInProgress || snapshot.FusionStepDone || snapshot.IncubatorInProgress || snapshot.IncubatorStepDone);
                bool fusionUnlocked = !extractorCollectGate && !catalizzatoreCollectGate && (snapshot.CatalizzatoreStepDone || snapshot.FusionInProgress || snapshot.FusionStepDone || snapshot.IncubatorInProgress || snapshot.IncubatorStepDone);
                bool incubatorUnlocked = !extractorCollectGate && !catalizzatoreCollectGate && !fusionCollectGate && (snapshot.FusionStepDone || snapshot.IncubatorInProgress || snapshot.IncubatorStepDone);
                StepVisualState extractorState = ResolveStepState(snapshot.ExtractorStepDone, snapshot.ExtractorInProgress || extractorCollectGate, true);
                StepVisualState catalizzatoreState = ResolveStepState(snapshot.CatalizzatoreStepDone, snapshot.CatalizzatoreInProgress || catalizzatoreCollectGate, catUnlocked);
                StepVisualState fusionState = ResolveStepState(snapshot.FusionStepDone, snapshot.FusionInProgress || fusionCollectGate, fusionUnlocked);
                StepVisualState incubatorState = ResolveStepState(snapshot.IncubatorStepDone, snapshot.IncubatorInProgress || incubatorCollectGate, incubatorUnlocked);

                if (_projectStepExtractor != null)
                {
                    _projectStepExtractor.text = LocalizationManager.GetString("lab_terminal.step_row.extractor", new Dictionary<string, string> { ["state"] = StepStateLabel(extractorState) });
                    ApplyStepLabelStyle(_projectStepExtractor, extractorState);
                }
                if (_projectStepCatalizzatore != null)
                {
                    _projectStepCatalizzatore.text = LocalizationManager.GetString("lab_terminal.step_row.catalizzatore", new Dictionary<string, string> { ["state"] = StepStateLabel(catalizzatoreState) });
                    ApplyStepLabelStyle(_projectStepCatalizzatore, catalizzatoreState);
                }
                if (_projectStepFusion != null)
                {
                    _projectStepFusion.text = LocalizationManager.GetString("lab_terminal.step_row.fusion", new Dictionary<string, string> { ["state"] = StepStateLabel(fusionState) });
                    ApplyStepLabelStyle(_projectStepFusion, fusionState);
                }
                if (_projectStepIncubator != null)
                {
                    _projectStepIncubator.text = LocalizationManager.GetString("lab_terminal.step_row.incubator", new Dictionary<string, string> { ["state"] = StepStateLabel(incubatorState) });
                    ApplyStepLabelStyle(_projectStepIncubator, incubatorState);
                }
            }

            UpdateAnalysisPanel(currentStep);
            UpdateQuickOutcomePanel(snapshot, currentStep);
            UpdateGuidanceButtonPulse(currentStep, snapshot);
            UpdateMachineCollectGuidance(snapshot);
        }

        private bool IsProjectFlowCompleted(ProjectRuntimeSnapshot snapshot)
        {
            return snapshot.IncubatorCollectedForProject;
        }

        private ProjectStep GetCurrentProjectStep(ProjectRuntimeSnapshot snapshot)
        {
            ProjectStep? pendingCollectionStep = GetPendingCollectionStep(snapshot);
            if (pendingCollectionStep.HasValue)
                return pendingCollectionStep.Value;

            if (!(snapshot.ExtractorStepDone || snapshot.ExtractorInProgress || snapshot.CatalizzatoreInProgress || snapshot.CatalizzatoreStepDone || snapshot.FusionInProgress || snapshot.FusionStepDone || snapshot.IncubatorInProgress || snapshot.IncubatorStepDone))
                return ProjectStep.Extractor;
            if (!(snapshot.CatalizzatoreStepDone || snapshot.CatalizzatoreInProgress || snapshot.FusionInProgress || snapshot.FusionStepDone || snapshot.IncubatorInProgress || snapshot.IncubatorStepDone))
                return ProjectStep.Catalizzatore;
            if (!(snapshot.FusionStepDone || snapshot.FusionInProgress || snapshot.IncubatorInProgress || snapshot.IncubatorStepDone))
                return ProjectStep.Fusion;
            if (!(snapshot.IncubatorStepDone || snapshot.IncubatorInProgress))
                return ProjectStep.Incubator;
            return ProjectStep.Completed;
        }

        private bool NeedsExtractorCollection(ProjectRuntimeSnapshot snapshot)
        {
            if (!_projectActive || !_analysisCompleted)
                return false;

            if (snapshot.ExtractorInProgress)
                return false;

            bool laterStepAlreadyRunning =
                snapshot.CatalizzatoreInProgress || snapshot.CatalizzatoreStepDone ||
                snapshot.FusionInProgress || snapshot.FusionStepDone ||
                snapshot.IncubatorInProgress || snapshot.IncubatorStepDone;

            if (laterStepAlreadyRunning)
                return false;

            return snapshot.ExtractorPendingRawCount > 0 && !snapshot.ExtractorCollectedForProject;
        }

        private bool NeedsCatalizzatoreCollection(ProjectRuntimeSnapshot snapshot)
        {
            if (!_projectActive || !_analysisCompleted)
                return false;

            if (snapshot.CatalizzatoreInProgress)
                return false;

            bool laterStepAlreadyRunning =
                snapshot.FusionInProgress || snapshot.FusionStepDone ||
                snapshot.IncubatorInProgress || snapshot.IncubatorStepDone;

            if (laterStepAlreadyRunning)
                return false;

            return snapshot.CatalizzatoreReadyCount > 0 && !snapshot.CatalizzatoreCollectedForProject;
        }

        private bool NeedsFusionCollection(ProjectRuntimeSnapshot snapshot)
        {
            if (!_projectActive || !_analysisCompleted)
                return false;

            if (snapshot.FusionInProgress)
                return false;

            bool laterStepAlreadyRunning = snapshot.IncubatorInProgress || snapshot.IncubatorStepDone;
            if (laterStepAlreadyRunning)
                return false;

            return snapshot.FusionReadyCount > 0 && !snapshot.FusionCollectedForProject;
        }

        private bool NeedsIncubatorCollection(ProjectRuntimeSnapshot snapshot)
        {
            if (!_projectActive || !_analysisCompleted)
                return false;

            if (snapshot.IncubatorInProgress)
                return false;

            return snapshot.IncubatorReadyCount > 0 && !snapshot.IncubatorCollectedForProject;
        }

        private ProjectStep? GetPendingCollectionStep(ProjectRuntimeSnapshot snapshot)
        {
            if (NeedsExtractorCollection(snapshot))
                return ProjectStep.Extractor;
            if (NeedsCatalizzatoreCollection(snapshot))
                return ProjectStep.Catalizzatore;
            if (NeedsFusionCollection(snapshot))
                return ProjectStep.Fusion;
            if (NeedsIncubatorCollection(snapshot))
                return ProjectStep.Incubator;
            return null;
        }

        private static StepVisualState ResolveStepState(bool isCompleted, bool isInProgress, bool isUnlocked)
        {
            if (isCompleted)
                return StepVisualState.Completed;
            if (isInProgress)
                return StepVisualState.InProgress;
            return isUnlocked ? StepVisualState.Todo : StepVisualState.Blocked;
        }

        private static string StepStateLabel(StepVisualState state) =>
            state switch
            {
                StepVisualState.Completed => LocalizationManager.GetString("lab_terminal.step_state.completed"),
                StepVisualState.InProgress => LocalizationManager.GetString("lab_terminal.step_state.in_progress"),
                StepVisualState.Todo => LocalizationManager.GetString("lab_terminal.step_state.todo"),
                _ => LocalizationManager.GetString("lab_terminal.step_state.blocked")
            };

        private static void ApplyMachineStatusStyle(Label label, bool inProgress, bool completed)
        {
            if (label == null)
                return;

            if (inProgress)
                label.style.color = new StyleColor(StatusColorInProgress);
            else if (completed)
                label.style.color = new StyleColor(StatusColorCompleted);
            else
                label.style.color = new StyleColor(StatusColorIdle);
        }

        private static void ApplyStepLabelStyle(Label label, StepVisualState state)
        {
            if (label == null)
                return;

            Color color = state switch
            {
                StepVisualState.Completed => StatusColorCompleted,
                StepVisualState.InProgress => StatusColorInProgress,
                StepVisualState.Todo => StatusColorTodo,
                _ => StatusColorBlocked
            };

            label.style.color = new StyleColor(color);
        }

        private static string StepLabel(ProjectStep step) =>
            step switch
            {
                ProjectStep.Extractor => LocalizationManager.GetString("lab_terminal.step.extractor"),
                ProjectStep.Catalizzatore => LocalizationManager.GetString("lab_terminal.step.catalizzatore"),
                ProjectStep.Fusion => LocalizationManager.GetString("lab_terminal.step.fusion"),
                ProjectStep.Incubator => LocalizationManager.GetString("lab_terminal.step.incubator"),
                _ => LocalizationManager.GetString("lab_terminal.step.completed")
            };

        private static string StepMachineName(ProjectStep step) =>
            step switch
            {
                ProjectStep.Extractor => LocalizationManager.GetString("lab_terminal.machine.extractor"),
                ProjectStep.Catalizzatore => LocalizationManager.GetString("lab_terminal.machine.catalizzatore"),
                ProjectStep.Fusion => LocalizationManager.GetString("lab_terminal.machine.fusion"),
                ProjectStep.Incubator => LocalizationManager.GetString("lab_terminal.machine.incubator"),
                _ => LocalizationManager.GetString("lab_terminal.machine.generic")
            };

        private void EnsureGameManager()
        {
            _gameManager = _gameManager ?? ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
        }

        private void CaptureProjectBaselineCounts()
        {
            _projectBaseRawSporeCount = CountPlayerSporeStage(SporeStage.Raw);
            _projectBaseMaturedSporeCount = CountPlayerSporeStage(SporeStage.Matured);
            _projectBasePreSeedCount = CountPlayerType(Items.PreSeed);
            _projectBaseSeedCount = CountPlayerAllSeeds();

            var dayCycle = ServiceContainer.Instance?.Get<DayCycleSystem>(suppressWarning: true);
            int day = dayCycle?.CurrentDay ?? 1;
            _activeLabProjectKey = $"d{day}_s{_projectBaseSeedCount}";
        }

        private ProjectRuntimeSnapshot BuildProjectRuntimeSnapshot()
        {
            bool extractorInProgress = _extractor != null && _extractor.State == ExtractorProcessState.InProgress;
            int extractorPendingRaw = _extractor != null ? _extractor.PendingSporeCount : 0;
            int extractorPct = _extractor != null ? Mathf.RoundToInt(_extractor.ExtractionProgress * 100f) : 0;

            bool catInProgress = _catalizzatorePanel != null && _catalizzatorePanel.HasProcessInProgress;
            int catReadyCount = _catalizzatorePanel != null ? _catalizzatorePanel.ReadyOutputCount : 0;

            bool fusionInProgress = _fusionPanel != null && _fusionPanel.IsFusionInProgress;
            int fusionReadyCount = _fusionPanel != null ? _fusionPanel.ReadyPreSeedCount : 0;
            int fusionPct = _fusionPanel != null ? Mathf.RoundToInt(_fusionPanel.FusionProgress01 * 100f) : 0;

            bool incubatorInProgress = _incubatorPanel != null && _incubatorPanel.IsIncubationInProgress;
            int incubatorReadyCount = _incubatorPanel != null ? _incubatorPanel.ReadySeedCount : 0;
            int incubatorDay = _incubatorPanel != null ? _incubatorPanel.IncubationDay : 0;

            bool extractorDoneByInventory = _projectActive && CountPlayerSporeStage(SporeStage.Raw) > _projectBaseRawSporeCount;
            bool catDoneByInventory = _projectActive && CountPlayerSporeStage(SporeStage.Matured) > _projectBaseMaturedSporeCount;
            bool fusionDoneByInventory = _projectActive && CountPlayerType(Items.PreSeed) > _projectBasePreSeedCount;
            bool incubatorDoneByInventory = _projectActive && CountPlayerAllSeeds() > _projectBaseSeedCount;

            bool incubatorStepDone = incubatorReadyCount > 0 || incubatorDoneByInventory;
            bool fusionStepDone = fusionReadyCount > 0 || fusionDoneByInventory || incubatorInProgress || incubatorStepDone;
            bool catStepDone = catReadyCount > 0 || catDoneByInventory || fusionInProgress || fusionStepDone || incubatorInProgress || incubatorStepDone;
            bool extractorStepDone = extractorPendingRaw > 0 || extractorDoneByInventory || catInProgress || catStepDone || fusionInProgress || fusionStepDone || incubatorInProgress || incubatorStepDone;

            return new ProjectRuntimeSnapshot(
                extractorInProgress,
                extractorStepDone,
                extractorDoneByInventory,
                extractorPct,
                extractorPendingRaw,
                catInProgress,
                catStepDone,
                catDoneByInventory,
                catReadyCount,
                fusionInProgress,
                fusionStepDone,
                fusionDoneByInventory,
                fusionPct,
                fusionReadyCount,
                incubatorInProgress,
                incubatorStepDone,
                incubatorDoneByInventory,
                incubatorDay,
                incubatorReadyCount);
        }

        private void UpdateQuickOutcomePanel(ProjectRuntimeSnapshot snapshot, ProjectStep currentStep)
        {
            if (_projectQuickIntroLabel != null)
            {
                if (_projectActive && _analysisCompleted && _selectedProjectType != SeedProjectType.None)
                {
                    string intent = _selectedProjectType switch
                    {
                        SeedProjectType.Replica => LocalizationManager.GetString("lab_terminal.intent.replica"),
                        SeedProjectType.Hybrid => LocalizationManager.GetString("lab_terminal.intent.hybrid"),
                        SeedProjectType.NewProfile => LocalizationManager.GetString("lab_terminal.intent.new_profile"),
                        _ => LocalizationManager.GetString("lab_terminal.intent.default")
                    };
                    ProjectStep? pendingCollect = GetPendingCollectionStep(snapshot);
                    string phase = pendingCollect.HasValue
                        ? LocalizationManager.GetString("lab_terminal.phase.collect", new Dictionary<string, string> { ["machine"] = StepMachineName(pendingCollect.Value) })
                        : LocalizationManager.GetString("lab_terminal.phase.current", new Dictionary<string, string> { ["step"] = StepLabel(currentStep) });
                    _projectQuickIntroLabel.text = LocalizationManager.GetString("lab_terminal.quick_intro_active", new Dictionary<string, string>
                    {
                        ["type"] = ProjectTypeLabel(_selectedProjectType),
                        ["intent"] = intent,
                        ["phase"] = phase
                    });
                }
                else
                {
                    _projectQuickIntroLabel.text = LocalizationManager.GetString("lab_terminal.quick_intro_idle");
                }
            }

            if (_projectQuickWhatNowLabel != null)
            {
                if (_projectActive && _analysisRunning)
                {
                    _projectQuickWhatNowLabel.text = LocalizationManager.GetString("lab_terminal.quick_what_analysis");
                }
                else if (_projectActive && !_analysisCompleted)
                {
                    _projectQuickWhatNowLabel.text = LocalizationManager.GetString("lab_terminal.quick_what_pick_type");
                }
                else if (_projectActive)
                {
                    string detail = currentStep switch
                    {
                        ProjectStep.Extractor => LocalizationManager.GetString("lab_terminal.quick_detail.extractor"),
                        ProjectStep.Catalizzatore => LocalizationManager.GetString("lab_terminal.quick_detail.catalizzatore"),
                        ProjectStep.Fusion => LocalizationManager.GetString("lab_terminal.quick_detail.fusion"),
                        ProjectStep.Incubator => LocalizationManager.GetString("lab_terminal.quick_detail.incubator"),
                        _ => LocalizationManager.GetString("lab_terminal.quick_detail.done")
                    };
                    string keywordColor = currentStep == ProjectStep.Completed ? "#78F27A" : "#98CFFF";
                    string reminder = BuildStepReminderByProjectType(currentStep);
                    _projectQuickWhatNowLabel.text = LocalizationManager.GetString("lab_terminal.quick_what_active", new Dictionary<string, string>
                    {
                        ["kw"] = keywordColor,
                        ["detail"] = detail,
                        ["type"] = ProjectTypeLabel(_selectedProjectType),
                        ["reminder"] = reminder
                    });
                }
                else
                {
                    _projectQuickWhatNowLabel.text = LocalizationManager.GetString("lab_terminal.quick_what_idle");
                }
            }

            if (_projectQuickLiveLabel != null)
            {
                if (!_projectActive)
                {
                    _projectQuickLiveLabel.text = LocalizationManager.GetString("lab_terminal.quick_live_standalone");
                }
                else if (_analysisRunning)
                {
                    float pct = Mathf.Clamp01((Time.unscaledTime - _analysisStartTime) / Mathf.Max(0.15f, _analysisDurationSeconds));
                    _projectQuickLiveLabel.text = LocalizationManager.GetString("lab_terminal.quick_live_analysis", new Dictionary<string, string> { ["pct"] = Mathf.RoundToInt(pct * 100f).ToString() });
                }
                else if (snapshot.ExtractorInProgress)
                {
                    _projectQuickLiveLabel.text = LocalizationManager.GetString("lab_terminal.quick_live_extractor", new Dictionary<string, string> { ["pct"] = snapshot.ExtractorProgressPct.ToString() });
                }
                else if (snapshot.CatalizzatoreInProgress)
                {
                    _projectQuickLiveLabel.text = LocalizationManager.GetString("lab_terminal.quick_live_catalizzatore");
                }
                else if (snapshot.FusionInProgress)
                {
                    _projectQuickLiveLabel.text = LocalizationManager.GetString("lab_terminal.quick_live_fusion", new Dictionary<string, string> { ["pct"] = snapshot.FusionProgressPct.ToString() });
                }
                else if (snapshot.IncubatorInProgress)
                {
                    _projectQuickLiveLabel.text = LocalizationManager.GetString("lab_terminal.quick_live_incubator", new Dictionary<string, string> { ["day"] = snapshot.IncubatorDay.ToString() });
                }
                else
                {
                    _projectQuickLiveLabel.text = LocalizationManager.GetString("lab_terminal.quick_live_idle");
                }
            }

            if (_projectQuickOutcomeLabel == null)
                return;

            if (_projectActive && !_analysisCompleted)
            {
                _projectQuickOutcomeLabel.text = LocalizationManager.GetString("lab_terminal.outcome_wait_analysis");
                return;
            }

            if (snapshot.IncubatorStepDone)
            {
                Item seed = _incubatorPanel?.ReadySeedPreview ?? GetLatestPlayerSeed();
                _projectQuickOutcomeLabel.text = LocalizationManager.GetString("lab_terminal.outcome_prefix") + BuildItemOutcomeLine(LocalizationManager.GetString("lab_terminal.label.seed"), seed);
                return;
            }

            if (snapshot.FusionStepDone)
            {
                Item preSeed = _fusionPanel?.ReadyPreSeedPreview ?? GetLatestPlayerItemByType(Items.PreSeed);
                _projectQuickOutcomeLabel.text = LocalizationManager.GetString("lab_terminal.outcome_prefix") + BuildItemOutcomeLine(LocalizationManager.GetString("lab_terminal.label.preseed"), preSeed);
                return;
            }

            if (snapshot.CatalizzatoreStepDone)
            {
                Item matured = _catalizzatorePanel?.ReadyMaturedPreviewSource ?? GetLatestPlayerSporeByStage(SporeStage.Matured);
                _projectQuickOutcomeLabel.text = LocalizationManager.GetString("lab_terminal.outcome_prefix") + BuildItemOutcomeLine(LocalizationManager.GetString("lab_terminal.label.mature_spore"), matured);
                return;
            }

            if (snapshot.ExtractorStepDone)
            {
                Item raw = GetLatestPlayerSporeByStage(SporeStage.Raw);
                if (raw != null)
                {
                    _projectQuickOutcomeLabel.text = LocalizationManager.GetString("lab_terminal.outcome_prefix") + BuildItemOutcomeLine(LocalizationManager.GetString("lab_terminal.label.raw_spore"), raw);
                }
                else
                {
                    var snap = _extractor?.GetFirstCompletedResultSnapshot();
                    _projectQuickOutcomeLabel.text = snap != null
                        ? LocalizationManager.GetString("lab_terminal.outcome_raw_line", new Dictionary<string, string>
                        {
                            ["tratti"] = ExtractorTooltipTexts.GeneticTypeToTrattiLabel(snap.GeneticTypeValue),
                            ["fam"] = snap.Famiglia ?? "—"
                        })
                        : LocalizationManager.GetString("lab_terminal.outcome_raw_fallback");
                }

                return;
            }

            _projectQuickOutcomeLabel.text = LocalizationManager.GetString("lab_terminal.outcome_none");
        }

        private string BuildItemOutcomeLine(string stageLabel, Item item)
        {
            if (item == null)
                return LocalizationManager.GetString("lab_terminal.outcome_line_ready_no_meta", new Dictionary<string, string> { ["stage"] = stageLabel });

            string displayName = PlayerInventoryPanelController.GetItemDisplayName(item.TypeId, item);
            string tratti = ExtractorTooltipTexts.GeneticTypeToTrattiLabel(item.GeneticTypeValue);
            string percentMutare = ExtractorTooltipTexts.GeneticTypeToPercentMutare(item.GeneticTypeValue);
            string family = string.IsNullOrWhiteSpace(item.FamilyMetadata) ? "STANDARD" : item.FamilyMetadata;
            string origine = ExtractorTooltipTexts.GetOriginTraceLabel(item);

            string metadata = LocalizationManager.GetString("lab_terminal.outcome_meta", new Dictionary<string, string>
            {
                ["tratti"] = tratti,
                ["mut"] = percentMutare,
                ["fam"] = family,
                ["orig"] = origine
            });
            return LocalizationManager.GetString("lab_terminal.outcome_line_produced", new Dictionary<string, string>
            {
                ["stage"] = stageLabel,
                ["name"] = displayName,
                ["meta"] = metadata
            });
        }

        private int CountPlayerType(string typeId)
        {
            EnsureGameManager();
            if (_gameManager?.PlayerInventory == null || string.IsNullOrWhiteSpace(typeId))
                return 0;

            var slot = _gameManager.PlayerInventory.Items.FirstOrDefault(s => string.Equals(s.TypeId, typeId, StringComparison.OrdinalIgnoreCase));
            return slot?.Quantity ?? 0;
        }

        private int CountPlayerSporeStage(SporeStage stage)
        {
            EnsureGameManager();
            if (_gameManager?.PlayerInventory == null)
                return 0;

            var slot = _gameManager.PlayerInventory.Items.FirstOrDefault(s => string.Equals(s.TypeId, Items.SporeGeneric, StringComparison.OrdinalIgnoreCase));
            if (slot == null)
                return 0;

            return slot.Items.Count(i => i.SporeStageValue == stage);
        }

        private int CountPlayerAllSeeds()
        {
            EnsureGameManager();
            if (_gameManager?.PlayerInventory == null)
                return 0;

            int total = 0;
            foreach (var slot in _gameManager.PlayerInventory.Items)
            {
                if (slot == null || string.IsNullOrWhiteSpace(slot.TypeId))
                    continue;

                if (slot.TypeId.StartsWith("seed-", StringComparison.OrdinalIgnoreCase))
                    total += slot.Quantity;
            }

            return total;
        }

        private Item GetLatestPlayerSporeByStage(SporeStage stage)
        {
            EnsureGameManager();
            if (_gameManager?.PlayerInventory == null)
                return null;

            var slot = _gameManager.PlayerInventory.Items.FirstOrDefault(s => string.Equals(s.TypeId, Items.SporeGeneric, StringComparison.OrdinalIgnoreCase));
            return slot?.Items.LastOrDefault(i => i.SporeStageValue == stage);
        }

        private Item GetLatestPlayerItemByType(string typeId)
        {
            EnsureGameManager();
            if (_gameManager?.PlayerInventory == null || string.IsNullOrWhiteSpace(typeId))
                return null;

            var slot = _gameManager.PlayerInventory.Items.FirstOrDefault(s => string.Equals(s.TypeId, typeId, StringComparison.OrdinalIgnoreCase));
            return slot?.Items.LastOrDefault();
        }

        private Item GetLatestPlayerSeed()
        {
            EnsureGameManager();
            if (_gameManager?.PlayerInventory == null)
                return null;

            Item latest = null;
            foreach (var slot in _gameManager.PlayerInventory.Items)
            {
                if (slot == null || string.IsNullOrWhiteSpace(slot.TypeId))
                    continue;
                if (!slot.TypeId.StartsWith("seed-", StringComparison.OrdinalIgnoreCase))
                    continue;

                var candidate = slot.Items.LastOrDefault();
                if (candidate != null)
                    latest = candidate;
            }

            return latest;
        }

        private void UpdateAnalysisPanel(ProjectStep currentStep)
        {
            float analysisPct = _analysisRunning
                ? Mathf.Clamp01((Time.unscaledTime - _analysisStartTime) / Mathf.Max(0.15f, _analysisDurationSeconds))
                : _analysisCompleted ? 1f : 0f;

            if (_analysisProgressFill != null)
                _analysisProgressFill.style.width = new Length(analysisPct * 100f, LengthUnit.Percent);

            if (!_projectActive)
            {
                if (_analysisStatusLabel != null)
                    _analysisStatusLabel.text = LocalizationManager.GetString("lab_terminal.analysis_cta_idle");
                SetSelectedProjectSchema(
                    LocalizationManager.GetString("lab_terminal.schema.project_none"),
                    LocalizationManager.GetString("lab_terminal.schema.items_pick"),
                    LocalizationManager.GetString("lab_terminal.schema.status_pick"));
                SetProjectTypeButtonsInteractable(false);
                UpdateProjectTypeButtonsState();
                if (_btnAnalysisOpenCurrentStep != null)
                {
                    _btnAnalysisOpenCurrentStep.SetEnabled(false);
                    _btnAnalysisOpenCurrentStep.text = LocalizationManager.GetString("lab_terminal.btn_open_step");
                }
                if (_btnAnalysisCancelSelection != null)
                    _btnAnalysisCancelSelection.SetEnabled(false);
                if (_analysisChangeWarningLabel != null)
                    _analysisChangeWarningLabel.text = string.Empty;
                return;
            }

            if (_analysisRunning)
            {
                if (_analysisStatusLabel != null)
                {
                    _analysisStatusLabel.text = LocalizationManager.GetString("lab_terminal.analysis_running", new Dictionary<string, string>
                    {
                        ["pct"] = Mathf.RoundToInt(analysisPct * 100f).ToString()
                    });
                }
                SetSelectedProjectSchema(
                    LocalizationManager.GetString("lab_terminal.schema.project_none"),
                    LocalizationManager.GetString("lab_terminal.schema.items_eval"),
                    LocalizationManager.GetString("lab_terminal.schema.status_running"));
                SetProjectTypeButtonsInteractable(false);
                UpdateProjectTypeButtonsState();
                if (_btnAnalysisOpenCurrentStep != null)
                {
                    _btnAnalysisOpenCurrentStep.SetEnabled(false);
                    _btnAnalysisOpenCurrentStep.text = LocalizationManager.GetString("lab_terminal.btn_analysis_running");
                }
                if (_btnAnalysisCancelSelection != null)
                    _btnAnalysisCancelSelection.SetEnabled(false);
                if (_analysisChangeWarningLabel != null)
                    _analysisChangeWarningLabel.text = string.Empty;
                return;
            }

            if (!_analysisCompleted)
            {
                if (_analysisStatusLabel != null)
                    _analysisStatusLabel.text = LocalizationManager.GetString("lab_terminal.analysis_waiting_restart");
                SetSelectedProjectSchema(
                    LocalizationManager.GetString("lab_terminal.schema.project_none"),
                    LocalizationManager.GetString("lab_terminal.schema.items_pick"),
                    LocalizationManager.GetString("lab_terminal.schema.status_pick"));
                SetProjectTypeButtonsInteractable(false);
                UpdateProjectTypeButtonsState();
                if (_btnAnalysisOpenCurrentStep != null)
                {
                    _btnAnalysisOpenCurrentStep.SetEnabled(false);
                    _btnAnalysisOpenCurrentStep.text = LocalizationManager.GetString("lab_terminal.btn_open_step");
                }
                if (_btnAnalysisCancelSelection != null)
                    _btnAnalysisCancelSelection.SetEnabled(false);
                return;
            }

            int totalFruits = _projectTypeAnalysis.PlayerFruitTotal + _projectTypeAnalysis.StorageFruitTotal;
            if (_analysisStatusLabel != null)
            {
                _analysisStatusLabel.text = LocalizationManager.GetString("lab_terminal.analysis_done", new Dictionary<string, string>
                {
                    ["total"] = totalFruits.ToString(),
                    ["inv"] = _projectTypeAnalysis.PlayerFruitTotal.ToString(),
                    ["stor"] = _projectTypeAnalysis.StorageFruitTotal.ToString(),
                    ["x"] = _projectTypeAnalysis.ReagentXCount.ToString(),
                    ["y"] = _projectTypeAnalysis.ReagentYCount.ToString()
                });
            }

            SetProjectTypeButtonsInteractable(true);

            UpdateProjectTypeButtonsState();
            var schema = BuildSelectedProjectSchema();
            SetSelectedProjectSchema(schema.projectLine, schema.requiredItemsLine, schema.statusLine);
            if (_btnAnalysisOpenCurrentStep != null)
            {
                bool canOpen = _selectedProjectType != SeedProjectType.None;
                _btnAnalysisOpenCurrentStep.SetEnabled(canOpen);
                _btnAnalysisOpenCurrentStep.text = canOpen
                    ? LocalizationManager.GetString("lab_terminal.btn_open_step")
                    : LocalizationManager.GetString("lab_terminal.btn_select_type");
            }
            if (_btnAnalysisCancelSelection != null)
                _btnAnalysisCancelSelection.SetEnabled(_selectedProjectType != SeedProjectType.None);

            if (_analysisChangeWarningLabel != null)
            {
                if (!string.IsNullOrWhiteSpace(_projectDirectionChangedMessage))
                {
                    _analysisChangeWarningLabel.text = _projectDirectionChangedMessage;
                }
                else
                {
                    string stepHint = currentStep == ProjectStep.Completed
                        ? LocalizationManager.GetString("lab_terminal.analysis_hint_done")
                        : LocalizationManager.GetString("lab_terminal.analysis_hint_step", new Dictionary<string, string>
                        {
                            ["text"] = BuildStepReminderByProjectType(currentStep)
                        });
                    string initialHint = _initialProjectType != SeedProjectType.None
                        ? LocalizationManager.GetString("lab_terminal.analysis_initial", new Dictionary<string, string> { ["type"] = ProjectTypeLabel(_initialProjectType) })
                        : string.Empty;
                    _analysisChangeWarningLabel.text = LocalizationManager.GetString("lab_terminal.analysis_direction", new Dictionary<string, string>
                    {
                        ["initial"] = initialHint,
                        ["type"] = ProjectTypeLabel(_selectedProjectType),
                        ["hint"] = stepHint
                    });
                }
            }
        }

        private ProjectTypeAnalysis BuildProjectTypeAnalysis()
        {
            var fruitCounts = BuildAccessibleFruitCounts();
            int playerFruitTotal = CountPlayerFruitTotal();
            int storageFruitTotal = CountStorageFruitTotal();
            int distinctFruitTypes = fruitCounts.Count(kv => kv.Value > 0);
            var bestDuplicate = fruitCounts.Count == 0
                ? default(KeyValuePair<string, int>)
                : fruitCounts.OrderByDescending(kv => kv.Value).First();
            int bestDuplicateCount = bestDuplicate.Value;
            string bestDuplicateTypeId = string.IsNullOrWhiteSpace(bestDuplicate.Key) ? "n/a" : bestDuplicate.Key;
            int reagentXCount = CountAccessibleType(Items.ReagentX);
            int reagentYCount = CountAccessibleType(Items.ReagentY);
            bool hasReagentX = reagentXCount > 0;
            bool hasReagentY = reagentYCount > 0;
            int totalFruit = playerFruitTotal + storageFruitTotal;

            bool replicaNow = bestDuplicateCount >= 2;
            bool hybridNow = distinctFruitTypes >= 2;
            bool newProfileNow = hybridNow && (hasReagentX || hasReagentY);

            string fruitTypesSummary = BuildFruitTypeSummary(fruitCounts);
            int sameNeed = Mathf.Max(0, 2 - bestDuplicateCount);
            string sameFruitRequirement = replicaNow
                ? LocalizationManager.GetString("lab_terminal.analysis_same_ok", new Dictionary<string, string> { ["type"] = bestDuplicateTypeId, ["count"] = bestDuplicateCount.ToString() })
                : LocalizationManager.GetString("lab_terminal.analysis_same_no", new Dictionary<string, string>
                {
                    ["need"] = sameNeed.ToString(),
                    ["type"] = bestDuplicateTypeId,
                    ["count"] = bestDuplicateCount.ToString()
                });
            string twoFruitTypesRequirement = hybridNow
                ? LocalizationManager.GetString("lab_terminal.analysis_two_ok", new Dictionary<string, string>
                {
                    ["types"] = distinctFruitTypes.ToString(),
                    ["summary"] = fruitTypesSummary
                })
                : totalFruit < 2
                    ? LocalizationManager.GetString("lab_terminal.analysis_two_no_total", new Dictionary<string, string>
                    {
                        ["have"] = totalFruit.ToString(),
                        ["need"] = (2 - totalFruit).ToString()
                    })
                    : LocalizationManager.GetString("lab_terminal.analysis_two_no_types", new Dictionary<string, string>
                    {
                        ["types"] = distinctFruitTypes.ToString(),
                        ["summary"] = fruitTypesSummary
                    });
            string reagentRequirement = (hasReagentX || hasReagentY)
                ? LocalizationManager.GetString("lab_terminal.analysis_reagent_ok", new Dictionary<string, string> { ["x"] = reagentXCount.ToString(), ["y"] = reagentYCount.ToString() })
                : LocalizationManager.GetString("lab_terminal.analysis_reagent_no");

            var replica = new ProjectTypeAdvice(
                replicaNow,
                $"{sameFruitRequirement} {LocalizationManager.GetString("lab_terminal.advice_replica_tail")}");
            var hybrid = new ProjectTypeAdvice(
                hybridNow,
                $"{twoFruitTypesRequirement} {LocalizationManager.GetString("lab_terminal.advice_hybrid_tail")}");
            var newProfile = new ProjectTypeAdvice(
                newProfileNow,
                $"{twoFruitTypesRequirement} {reagentRequirement} {LocalizationManager.GetString("lab_terminal.advice_new_tail")}");

            return new ProjectTypeAnalysis(
                playerFruitTotal,
                storageFruitTotal,
                distinctFruitTypes,
                bestDuplicateCount,
                bestDuplicateTypeId,
                reagentXCount,
                reagentYCount,
                hasReagentX,
                hasReagentY,
                replica,
                hybrid,
                newProfile);
        }

        private static string BuildFruitTypeSummary(Dictionary<string, int> fruitCounts)
        {
            if (fruitCounts == null || fruitCounts.Count == 0)
                return LocalizationManager.GetString("lab_terminal.fruit_summary_none");

            return string.Join(", ",
                fruitCounts
                    .Where(kv => kv.Value > 0)
                    .OrderByDescending(kv => kv.Value)
                    .Take(3)
                    .Select(kv => $"{kv.Key} x{kv.Value}"));
        }

        private Dictionary<string, int> BuildAccessibleFruitCounts()
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            AddFruitCountsFromInventory(_gameManager?.PlayerInventory, counts);

            AddFruitCountsFromSeedStorage(_gameManager?.SeedStorageSystem, counts);

            return counts;
        }

        private static void AddFruitCountsFromSeedStorage(SeedStorageSystem seedStorage, Dictionary<string, int> counts)
        {
            if (seedStorage == null || counts == null)
                return;
            for (int s = 0; s < SeedStorageSystem.SlotCount; s++)
            {
                foreach (var u in seedStorage.GetSlotUnits(s))
                {
                    if (u?.Item == null)
                        continue;
                    var id = u.Item.TypeId;
                    if (!Items.IsFruitType(id, includeLegacy: true))
                        continue;
                    if (!counts.TryGetValue(id, out int current))
                        current = 0;
                    counts[id] = current + 1;
                }
            }
        }

        private static void AddFruitCountsFromInventory(Inventory inventory, Dictionary<string, int> counts)
        {
            if (inventory == null || counts == null)
                return;

            foreach (var slot in inventory.Items)
            {
                if (slot == null || string.IsNullOrWhiteSpace(slot.TypeId))
                    continue;
                if (!Items.IsFruitType(slot.TypeId, includeLegacy: true))
                    continue;

                if (!counts.TryGetValue(slot.TypeId, out int current))
                    current = 0;
                counts[slot.TypeId] = current + Mathf.Max(0, slot.Quantity);
            }
        }

        private int CountPlayerFruitTotal()
        {
            EnsureGameManager();
            if (_gameManager?.PlayerInventory == null)
                return 0;
            return CountFruitInInventory(_gameManager.PlayerInventory);
        }

        private static int CountFruitInInventory(Inventory inventory)
        {
            if (inventory == null)
                return 0;

            int total = 0;
            foreach (var slot in inventory.Items)
            {
                if (slot == null || string.IsNullOrWhiteSpace(slot.TypeId))
                    continue;
                if (!Items.IsFruitType(slot.TypeId, includeLegacy: true))
                    continue;
                total += Mathf.Max(0, slot.Quantity);
            }
            return total;
        }

        private int CountStorageFruitTotal()
        {
            var ss = _gameManager?.SeedStorageSystem;
            if (ss == null)
                return 0;
            int total = 0;
            for (int s = 0; s < SeedStorageSystem.SlotCount; s++)
            {
                foreach (var u in ss.GetSlotUnits(s))
                {
                    if (u?.Item == null)
                        continue;
                    if (Items.IsFruitType(u.Item.TypeId, includeLegacy: true))
                        total++;
                }
            }
            return total;
        }

        private int CountAccessibleType(string typeId)
        {
            int total = CountPlayerType(typeId);
            var ss = _gameManager?.SeedStorageSystem;
            if (ss != null)
                total += ss.CountTypeInStorage(typeId);
            return total;
        }

        private string BuildStepReminderByProjectType(ProjectStep currentStep)
        {
            string replicaAdvice = currentStep switch
            {
                ProjectStep.Extractor => LocalizationManager.GetString("lab_terminal.reminder.replica.extractor"),
                ProjectStep.Catalizzatore => LocalizationManager.GetString("lab_terminal.reminder.replica.cat"),
                ProjectStep.Fusion => LocalizationManager.GetString("lab_terminal.reminder.replica.fusion"),
                ProjectStep.Incubator => LocalizationManager.GetString("lab_terminal.reminder.replica.inc"),
                _ => LocalizationManager.GetString("lab_terminal.reminder.replica.done")
            };

            string hybridAdvice = currentStep switch
            {
                ProjectStep.Extractor => LocalizationManager.GetString("lab_terminal.reminder.hybrid.extractor"),
                ProjectStep.Catalizzatore => LocalizationManager.GetString("lab_terminal.reminder.hybrid.cat"),
                ProjectStep.Fusion => LocalizationManager.GetString("lab_terminal.reminder.hybrid.fusion"),
                ProjectStep.Incubator => LocalizationManager.GetString("lab_terminal.reminder.hybrid.inc"),
                _ => LocalizationManager.GetString("lab_terminal.reminder.hybrid.done")
            };

            string newProfileAdvice = currentStep switch
            {
                ProjectStep.Extractor => LocalizationManager.GetString("lab_terminal.reminder.new.extractor"),
                ProjectStep.Catalizzatore => LocalizationManager.GetString("lab_terminal.reminder.new.cat"),
                ProjectStep.Fusion => LocalizationManager.GetString("lab_terminal.reminder.new.fusion"),
                ProjectStep.Incubator => LocalizationManager.GetString("lab_terminal.reminder.new.inc"),
                _ => LocalizationManager.GetString("lab_terminal.reminder.new.done")
            };

            return _selectedProjectType switch
            {
                SeedProjectType.Replica => replicaAdvice,
                SeedProjectType.Hybrid => hybridAdvice,
                SeedProjectType.NewProfile => newProfileAdvice,
                _ => LocalizationManager.GetString("lab_terminal.reminder.pick_type")
            };
        }

        private static string ProjectTypeLabel(SeedProjectType type) =>
            type switch
            {
                SeedProjectType.Replica => LocalizationManager.GetString("lab_terminal.type.replica"),
                SeedProjectType.Hybrid => LocalizationManager.GetString("lab_terminal.type.hybrid"),
                SeedProjectType.NewProfile => LocalizationManager.GetString("lab_terminal.type.new_profile"),
                _ => LocalizationManager.GetString("lab_terminal.type.undefined")
            };

        private bool IsProjectTypeAvailable(SeedProjectType type)
        {
            return type switch
            {
                SeedProjectType.Replica => _projectTypeAnalysis.Replica.AvailableNow,
                SeedProjectType.Hybrid => _projectTypeAnalysis.Hybrid.AvailableNow,
                SeedProjectType.NewProfile => _projectTypeAnalysis.NewProfile.AvailableNow,
                _ => false
            };
        }

        private void SetSelectedProjectSchema(string projectLine, string requiredItemsLine, string statusLine)
        {
            if (_analysisSelectedProjectLabel != null)
                _analysisSelectedProjectLabel.text = projectLine;
            if (_analysisRequiredItemsLabel != null)
                _analysisRequiredItemsLabel.text = requiredItemsLine;
            if (_analysisExecutionStatusLabel != null)
                _analysisExecutionStatusLabel.text = statusLine;
        }

        private (string projectLine, string requiredItemsLine, string statusLine) BuildSelectedProjectSchema()
        {
            SeedProjectType type = _analysisFocusedProjectType != SeedProjectType.None
                ? _analysisFocusedProjectType
                : _selectedProjectType;
            if (type == SeedProjectType.None)
            {
                return (
                    LocalizationManager.GetString("lab_terminal.schema.project_none"),
                    LocalizationManager.GetString("lab_terminal.schema.items_pick"),
                    LocalizationManager.GetString("lab_terminal.schema.status_pick"));
            }

            int totalFruits = _projectTypeAnalysis.PlayerFruitTotal + _projectTypeAnalysis.StorageFruitTotal;
            bool hasAnyReagent = _projectTypeAnalysis.ReagentXCount > 0 || _projectTypeAnalysis.ReagentYCount > 0;

            switch (type)
            {
                case SeedProjectType.Replica:
                {
                    bool executable = _projectTypeAnalysis.Replica.AvailableNow;
                    string projectLine = LocalizationManager.GetString("lab_terminal.schema.project_replica");
                    string requiredItemsLine = LocalizationManager.GetString("lab_terminal.schema.replica_items", new Dictionary<string, string>
                    {
                        ["type"] = _projectTypeAnalysis.BestDuplicateFruitTypeId,
                        ["count"] = _projectTypeAnalysis.BestDuplicateFruitCount.ToString()
                    });
                    int needReplica = Mathf.Max(0, 2 - _projectTypeAnalysis.BestDuplicateFruitCount);
                    string itemStatus = executable
                        ? LocalizationManager.GetString("lab_terminal.schema.replica_ok")
                        : LocalizationManager.GetString("lab_terminal.schema.replica_missing", new Dictionary<string, string> { ["need"] = needReplica.ToString() });
                    string projectStatus = executable
                        ? LocalizationManager.GetString("lab_terminal.schema.exec_ok")
                        : LocalizationManager.GetString("lab_terminal.schema.exec_no");
                    return (projectLine, requiredItemsLine, LocalizationManager.GetString("lab_terminal.schema.status_pair", new Dictionary<string, string>
                    {
                        ["items"] = itemStatus,
                        ["proj"] = projectStatus
                    }));
                }
                case SeedProjectType.Hybrid:
                {
                    bool executable = _projectTypeAnalysis.Hybrid.AvailableNow;
                    string projectLine = LocalizationManager.GetString("lab_terminal.schema.project_hybrid");
                    string requiredItemsLine = LocalizationManager.GetString("lab_terminal.schema.hybrid_items");
                    string itemStatus = executable
                        ? LocalizationManager.GetString("lab_terminal.schema.replica_ok")
                        : totalFruits < 2
                            ? LocalizationManager.GetString("lab_terminal.schema.hybrid_missing_total", new Dictionary<string, string> { ["have"] = totalFruits.ToString() })
                            : LocalizationManager.GetString("lab_terminal.schema.hybrid_missing_types", new Dictionary<string, string>
                            {
                                ["have"] = _projectTypeAnalysis.DistinctFruitTypes.ToString()
                            });
                    string projectStatus = executable
                        ? LocalizationManager.GetString("lab_terminal.schema.exec_ok")
                        : LocalizationManager.GetString("lab_terminal.schema.exec_no");
                    return (projectLine, requiredItemsLine, LocalizationManager.GetString("lab_terminal.schema.status_pair", new Dictionary<string, string>
                    {
                        ["items"] = itemStatus,
                        ["proj"] = projectStatus
                    }));
                }
                case SeedProjectType.NewProfile:
                {
                    bool executable = _projectTypeAnalysis.NewProfile.AvailableNow;
                    string projectLine = LocalizationManager.GetString("lab_terminal.schema.project_new");
                    string requiredItemsLine = LocalizationManager.GetString("lab_terminal.schema.new_items", new Dictionary<string, string>
                    {
                        ["x"] = _projectTypeAnalysis.ReagentXCount.ToString(),
                        ["y"] = _projectTypeAnalysis.ReagentYCount.ToString()
                    });
                    bool fruitReady = _projectTypeAnalysis.Hybrid.AvailableNow;
                    string missingReasonKey = !fruitReady
                        ? "lab_terminal.schema.new_missing_fruit"
                        : !hasAnyReagent
                            ? "lab_terminal.schema.new_missing_reagent"
                            : "lab_terminal.schema.new_ok_reason";
                    string missingReason = LocalizationManager.GetString(missingReasonKey);
                    string itemStatus = executable
                        ? LocalizationManager.GetString("lab_terminal.schema.replica_ok")
                        : LocalizationManager.GetString("lab_terminal.schema.new_missing_wrap", new Dictionary<string, string> { ["reason"] = missingReason });
                    string projectStatus = executable
                        ? LocalizationManager.GetString("lab_terminal.schema.exec_ok")
                        : LocalizationManager.GetString("lab_terminal.schema.exec_no");
                    return (projectLine, requiredItemsLine, LocalizationManager.GetString("lab_terminal.schema.status_pair", new Dictionary<string, string>
                    {
                        ["items"] = itemStatus,
                        ["proj"] = projectStatus
                    }));
                }
                default:
                    return (
                        LocalizationManager.GetString("lab_terminal.schema.project_none"),
                        LocalizationManager.GetString("lab_terminal.schema.items_pick"),
                        LocalizationManager.GetString("lab_terminal.schema.status_pick"));
            }
        }

        private void SetProjectTypeButtonsInteractable(bool enabled)
        {
            SetButtonInteractable(_btnProjectTypeReplica, enabled);
            SetButtonInteractable(_btnProjectTypeHybrid, enabled);
            SetButtonInteractable(_btnProjectTypeNewProfile, enabled);
        }

        private static void SetButtonInteractable(Button button, bool enabled)
        {
            if (button == null)
                return;
            button.SetEnabled(enabled);
        }

        private void UpdateProjectTypeButtonsState()
        {
            if (_btnProjectTypeReplica != null)
            {
                bool isFocused = _analysisFocusedProjectType == SeedProjectType.Replica;
                _btnProjectTypeReplica.EnableInClassList("lab-terminal-project-type-btn-selected", isFocused);
                _btnProjectTypeReplica.EnableInClassList("lab-terminal-project-type-btn-disabled", _analysisCompleted && !_projectTypeAnalysis.Replica.AvailableNow);
            }
            if (_btnProjectTypeHybrid != null)
            {
                bool isFocused = _analysisFocusedProjectType == SeedProjectType.Hybrid;
                _btnProjectTypeHybrid.EnableInClassList("lab-terminal-project-type-btn-selected", isFocused);
                _btnProjectTypeHybrid.EnableInClassList("lab-terminal-project-type-btn-disabled", _analysisCompleted && !_projectTypeAnalysis.Hybrid.AvailableNow);
            }
            if (_btnProjectTypeNewProfile != null)
            {
                bool isFocused = _analysisFocusedProjectType == SeedProjectType.NewProfile;
                _btnProjectTypeNewProfile.EnableInClassList("lab-terminal-project-type-btn-selected", isFocused);
                _btnProjectTypeNewProfile.EnableInClassList("lab-terminal-project-type-btn-disabled", _analysisCompleted && !_projectTypeAnalysis.NewProfile.AvailableNow);
            }
        }

        private void ToggleMachinesSection()
        {
            SetMachinesSectionCollapsed(!_machinesSectionCollapsed);
        }

        private void SetMachinesSectionCollapsed(bool collapsed)
        {
            _machinesSectionCollapsed = collapsed;
            if (_machinesContent != null)
                _machinesContent.EnableInClassList("lab-terminal-machines-content-collapsed", collapsed);
            if (_btnToggleMachinesSection != null)
                _btnToggleMachinesSection.text = collapsed ? ">" : "v";
        }

        private void UpdateGuidanceButtonPulse(ProjectStep currentStep, ProjectRuntimeSnapshot snapshot)
        {
            if (_btnOpenCurrentStep == null)
                return;

            if (GetPendingCollectionStep(snapshot).HasValue)
            {
                _btnOpenCurrentStep.EnableInClassList("lab-terminal-guidance-pulse", false);
                _btnOpenCurrentStep.style.backgroundColor = StyleKeyword.Null;
                _btnOpenCurrentStep.style.color = new StyleColor(new Color(0.58f, 0.63f, 0.68f, 0.95f));
                _btnOpenCurrentStep.style.borderTopColor = new StyleColor(new Color(0.46f, 0.5f, 0.55f, 0.95f));
                _btnOpenCurrentStep.style.borderRightColor = new StyleColor(new Color(0.46f, 0.5f, 0.55f, 0.95f));
                _btnOpenCurrentStep.style.borderBottomColor = new StyleColor(new Color(0.46f, 0.5f, 0.55f, 0.95f));
                _btnOpenCurrentStep.style.borderLeftColor = new StyleColor(new Color(0.46f, 0.5f, 0.55f, 0.95f));
                return;
            }

            bool pulse = _projectActive && _analysisCompleted && currentStep != ProjectStep.Completed && !AnyStepInProgress();
            _btnOpenCurrentStep.EnableInClassList("lab-terminal-guidance-pulse", pulse);

            if (!pulse)
            {
                _btnOpenCurrentStep.style.backgroundColor = StyleKeyword.Null;
                _btnOpenCurrentStep.style.color = StyleKeyword.Null;
                _btnOpenCurrentStep.style.borderTopColor = StyleKeyword.Null;
                _btnOpenCurrentStep.style.borderRightColor = StyleKeyword.Null;
                _btnOpenCurrentStep.style.borderBottomColor = StyleKeyword.Null;
                _btnOpenCurrentStep.style.borderLeftColor = StyleKeyword.Null;
                return;
            }

            float t = Mathf.PingPong(Time.unscaledTime * 1.6f, 1f);
            Color baseBg = new Color(0.13f, 0.27f, 0.37f, 0.92f);
            Color pulseBg = new Color(0.42f, 0.24f, 0.63f, 0.96f);
            Color baseText = new Color(0.78f, 0.89f, 0.99f, 1f);
            Color pulseText = new Color(1f, 0.94f, 1f, 1f);
            Color border = Color.Lerp(new Color(0.62f, 0.78f, 0.96f, 1f), StatusColorInProgress, t);

            _btnOpenCurrentStep.style.backgroundColor = new StyleColor(Color.Lerp(baseBg, pulseBg, t));
            _btnOpenCurrentStep.style.color = new StyleColor(Color.Lerp(baseText, pulseText, t));
            _btnOpenCurrentStep.style.borderTopColor = new StyleColor(border);
            _btnOpenCurrentStep.style.borderRightColor = new StyleColor(border);
            _btnOpenCurrentStep.style.borderBottomColor = new StyleColor(border);
            _btnOpenCurrentStep.style.borderLeftColor = new StyleColor(border);
        }

        private void UpdateMachineCollectGuidance(ProjectRuntimeSnapshot snapshot)
        {
            ApplyMachineCollectPulse(_btnOpenExtractor, NeedsExtractorCollection(snapshot));
            ApplyMachineCollectPulse(_btnOpenCatalizzatore, NeedsCatalizzatoreCollection(snapshot));
            ApplyMachineCollectPulse(_btnOpenFusion, NeedsFusionCollection(snapshot));
            ApplyMachineCollectPulse(_btnOpenIncubator, NeedsIncubatorCollection(snapshot));
        }

        private void ApplyMachineCollectPulse(Button button, bool shouldPulse)
        {
            if (button == null)
                return;

            button.EnableInClassList("lab-terminal-machine-collect-pulse", shouldPulse);
            if (!shouldPulse)
            {
                button.style.backgroundColor = StyleKeyword.Null;
                button.style.color = StyleKeyword.Null;
                button.style.borderTopColor = StyleKeyword.Null;
                button.style.borderRightColor = StyleKeyword.Null;
                button.style.borderBottomColor = StyleKeyword.Null;
                button.style.borderLeftColor = StyleKeyword.Null;
                return;
            }

            float t = Mathf.PingPong(Time.unscaledTime * 1.8f, 1f);
            Color baseBg = new Color(0.23f, 0.14f, 0.34f, 0.93f);
            Color pulseBg = new Color(0.52f, 0.29f, 0.73f, 0.98f);
            Color baseText = new Color(0.9f, 0.82f, 1f, 1f);
            Color pulseText = new Color(1f, 0.97f, 1f, 1f);
            Color border = Color.Lerp(new Color(0.72f, 0.58f, 0.93f, 1f), StatusColorInProgress, t);

            button.style.backgroundColor = new StyleColor(Color.Lerp(baseBg, pulseBg, t));
            button.style.color = new StyleColor(Color.Lerp(baseText, pulseText, t));
            button.style.borderTopColor = new StyleColor(border);
            button.style.borderRightColor = new StyleColor(border);
            button.style.borderBottomColor = new StyleColor(border);
            button.style.borderLeftColor = new StyleColor(border);
        }

        private bool AnyStepInProgress()
        {
            ProjectRuntimeSnapshot snapshot = BuildProjectRuntimeSnapshot();
            return snapshot.ExtractorInProgress || snapshot.CatalizzatoreInProgress || snapshot.FusionInProgress || snapshot.IncubatorInProgress;
        }
    }
}
