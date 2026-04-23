using System;
using System.Linq;
using System.Collections.Generic;
using _Project;
using _Project.Sporae.Core;
using _Project.Systems.SeedStorage;
using Sporae.Core;
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

            _root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (_root != null)
                TryBindUI();
        }

        private void Start()
        {
            EnsureGameManager();
            Hide();
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy)
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

        public void Show()
        {
            gameObject.SetActive(true);
            GameplayUiModalLock.SetMachineModalState(true);
            TryBindUI();
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.Flex;
                _overlay.pickingMode = PickingMode.Position;
            }

            if (_root != null)
                _root.pickingMode = PickingMode.Position;

            RefreshDisplay();
        }

        public void Hide()
        {
            GameplayUiModalLock.SetMachineModalState(false);
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.None;
                _overlay.pickingMode = PickingMode.Ignore;
            }

            if (_root != null)
                _root.pickingMode = PickingMode.Ignore;

            gameObject.SetActive(false);
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
                    StartProjectWithAnalysis();
                };
            }

            if (_btnCancelProject != null)
            {
                _btnCancelProject.clicked += () =>
                {
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
            if (_btnAnalysisOpenCurrentStep != null)
                _btnAnalysisOpenCurrentStep.clicked += OpenCurrentStepFromAnalysis;
            if (_btnAnalysisCancelSelection != null)
                _btnAnalysisCancelSelection.clicked += CancelProjectTypeSelection;

            _uiBound = true;
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
                _projectDirectionChangedMessage =
                    $"<color=#AFC8D8>Hai cambiato direzione in corso:</color> da {ProjectTypeLabel(_selectedProjectType)} a {ProjectTypeLabel(type)}. Nessuna penalita, il sistema aggiorna solo i consigli.";
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
                    ? $"In corso ({snapshot.ExtractorProgressPct}%)"
                    : snapshot.ExtractorStepDone
                        ? $"Completato (Spore RAW rilevate: {Mathf.Max(snapshot.ExtractorPendingRawCount, 1)})"
                        : "Idle";
                ApplyMachineStatusStyle(_machineExtractorStatus, snapshot.ExtractorInProgress, snapshot.ExtractorStepDone);
            }

            if (_machineCatalizzatoreStatus != null)
            {
                _machineCatalizzatoreStatus.text = snapshot.CatalizzatoreInProgress
                    ? "In corso (maturazione 1 giorno)"
                    : snapshot.CatalizzatoreStepDone
                        ? $"Completato (Spore mature: {Mathf.Max(snapshot.CatalizzatoreReadyCount, 1)})"
                        : "Idle";
                ApplyMachineStatusStyle(_machineCatalizzatoreStatus, snapshot.CatalizzatoreInProgress, snapshot.CatalizzatoreStepDone);
            }

            if (_machineFusionStatus != null)
            {
                _machineFusionStatus.text = snapshot.FusionInProgress
                    ? $"In corso ({snapshot.FusionProgressPct}%)"
                    : snapshot.FusionStepDone
                        ? $"Completato (Pre-seed: {Mathf.Max(snapshot.FusionReadyCount, 1)})"
                        : "Idle";
                ApplyMachineStatusStyle(_machineFusionStatus, snapshot.FusionInProgress, snapshot.FusionStepDone);
            }

            if (_machineIncubatorStatus != null)
            {
                _machineIncubatorStatus.text = snapshot.IncubatorInProgress
                    ? $"In corso (giorno {snapshot.IncubatorDay}/2)"
                    : snapshot.IncubatorStepDone
                        ? $"Completato (Semi: {Mathf.Max(snapshot.IncubatorReadyCount, 1)})"
                        : "Idle";
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
                _btnOpenCurrentStep.text = collectGate ? $"RITIRA OUTPUT DA {StepMachineName(pendingCollectionStep.Value)}" : "APRI STEP CORRENTE";
            }

            if (_projectStatusLabel != null)
            {
                if (_projectActive)
                {
                    if (_analysisRunning)
                    {
                        _projectStatusLabel.text = "Progetto attivo: analisi risorse in corso...";
                    }
                    else if (!_analysisCompleted)
                    {
                        _projectStatusLabel.text = "Progetto attivo: analisi iniziale in attesa.";
                    }
                    else
                    {
                        _projectStatusLabel.text = collectGate
                            ? $"Progetto attivo: ritira output pronto da {StepMachineName(pendingCollectionStep.Value)} prima di continuare."
                            : currentStep == ProjectStep.Completed
                            ? "Progetto completato: seed pronto per il ritiro."
                            : $"Progetto attivo ({ProjectTypeLabel(_selectedProjectType)}): step corrente {StepLabel(currentStep)}.";
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
                        ? "Ultimo progetto completato. Avvia un nuovo ciclo quando vuoi."
                        : "Nessun progetto attivo. Usa i macchinari in modalità standalone o avvia Crea Nuovo Seme.";
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
                    _projectStepExtractor.text = $"Frutto -> Spora: {StepStateLabel(extractorState)}";
                    ApplyStepLabelStyle(_projectStepExtractor, extractorState);
                }
                if (_projectStepCatalizzatore != null)
                {
                    _projectStepCatalizzatore.text = $"Spora -> Maturazione: {StepStateLabel(catalizzatoreState)}";
                    ApplyStepLabelStyle(_projectStepCatalizzatore, catalizzatoreState);
                }
                if (_projectStepFusion != null)
                {
                    _projectStepFusion.text = $"Maturazione -> Pre-seed: {StepStateLabel(fusionState)}";
                    ApplyStepLabelStyle(_projectStepFusion, fusionState);
                }
                if (_projectStepIncubator != null)
                {
                    _projectStepIncubator.text = $"Pre-seed -> Incubazione: {StepStateLabel(incubatorState)}";
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

        private static string StepStateLabel(StepVisualState state)
        {
            return state switch
            {
                StepVisualState.Completed => "Completato",
                StepVisualState.InProgress => "In corso",
                StepVisualState.Todo => "Da fare",
                _ => "Bloccato"
            };
        }

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

        private static string StepLabel(ProjectStep step)
        {
            return step switch
            {
                ProjectStep.Extractor => "Frutto -> Spora",
                ProjectStep.Catalizzatore => "Spora -> Maturazione",
                ProjectStep.Fusion => "Maturazione -> Pre-seed",
                ProjectStep.Incubator => "Pre-seed -> Incubazione",
                _ => "Completato"
            };
        }

        private static string StepMachineName(ProjectStep step)
        {
            return step switch
            {
                ProjectStep.Extractor => "EXTRACTOR",
                ProjectStep.Catalizzatore => "CATALIZZATORE",
                ProjectStep.Fusion => "FUSION",
                ProjectStep.Incubator => "INCUBATOR",
                _ => "MACCHINA"
            };
        }

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
                        SeedProjectType.Replica => "replicare la linea di origine con coerenza.",
                        SeedProjectType.Hybrid => "combinare due linee per tratti misti.",
                        SeedProjectType.NewProfile => "costruire un outcome nuovo orientato da reagenti.",
                        _ => "definire un risultato operativo."
                    };
                    ProjectStep? pendingCollect = GetPendingCollectionStep(snapshot);
                    string phase = pendingCollect.HasValue
                        ? $"Output pronto da {StepMachineName(pendingCollect.Value)}: ritira prima di continuare."
                        : $"Fase attuale: {StepLabel(currentStep)}.";
                    _projectQuickIntroLabel.text =
                        $"<color=#97A7B2>Progetto Seme ({ProjectTypeLabel(_selectedProjectType)}):</color> obiettivo {intent} {phase}";
                }
                else
                {
                    _projectQuickIntroLabel.text =
                        "<color=#97A7B2>Progetto Seme:</color> scegli un intento (Replica / Ibrido / Nuovo Profilo). E una bussola strategica, non un vincolo: il sistema consiglia, il player decide.";
                }
            }

            if (_projectQuickWhatNowLabel != null)
            {
                if (_projectActive && _analysisRunning)
                {
                    _projectQuickWhatNowLabel.text = "<color=#98CFFF>Step corrente:</color> analisi iniziale in corso. Il terminale sta valutando frutti in inventario e Seed Storage.";
                }
                else if (_projectActive && !_analysisCompleted)
                {
                    _projectQuickWhatNowLabel.text = "<color=#98CFFF>Step corrente:</color> completa la fase Analisi e scegli una tipologia progetto per ricevere consigli contestuali.";
                }
                else if (_projectActive)
                {
                    string detail = currentStep switch
                    {
                        ProjectStep.Extractor => "estrai una Spora RAW da un frutto (Extractor).",
                        ProjectStep.Catalizzatore => "matura la Spora RAW in Spora Maturata (Catalizzatore).",
                        ProjectStep.Fusion => "combina due Spore Maturate per ottenere un Pre-seed (Fusion).",
                        ProjectStep.Incubator => "incuba il Pre-seed per ottenere il Seme finale (Incubator).",
                        _ => "progetto completato. Ritira il seed e avvia un nuovo ciclo se vuoi."
                    };
                    string keywordColor = currentStep == ProjectStep.Completed ? "#78F27A" : "#98CFFF";
                    string reminder = BuildStepReminderByProjectType(currentStep);
                    _projectQuickWhatNowLabel.text = $"<color={keywordColor}>Step corrente:</color> {detail} <color=#A4B7C5>Intento:</color> {ProjectTypeLabel(_selectedProjectType)}. {reminder}";
                }
                else
                {
                    _projectQuickWhatNowLabel.text = "<color=#98CFFF>Step corrente:</color> nessun progetto attivo. Premi CREA NUOVO SEME per avviare il flow guidato.";
                }
            }

            if (_projectQuickLiveLabel != null)
            {
                if (!_projectActive)
                {
                    _projectQuickLiveLabel.text = "<color=#9EC8E4>Live:</color> modalità standalone. Apri i macchinari singolarmente dal terminale.";
                }
                else if (_analysisRunning)
                {
                    float pct = Mathf.Clamp01((Time.unscaledTime - _analysisStartTime) / Mathf.Max(0.15f, _analysisDurationSeconds));
                    _projectQuickLiveLabel.text = $"<color=#B98DFF>Live:</color> analisi combinazioni in corso ({Mathf.RoundToInt(pct * 100f)}%).";
                }
                else if (snapshot.ExtractorInProgress)
                {
                    _projectQuickLiveLabel.text = $"<color=#B98DFF>Live:</color> Extractor in corso ({snapshot.ExtractorProgressPct}%).";
                }
                else if (snapshot.CatalizzatoreInProgress)
                {
                    _projectQuickLiveLabel.text = "<color=#B98DFF>Live:</color> Catalizzatore in corso (1 giorno di maturazione).";
                }
                else if (snapshot.FusionInProgress)
                {
                    _projectQuickLiveLabel.text = $"<color=#B98DFF>Live:</color> Fusion in corso ({snapshot.FusionProgressPct}%).";
                }
                else if (snapshot.IncubatorInProgress)
                {
                    _projectQuickLiveLabel.text = $"<color=#B98DFF>Live:</color> Incubazione in corso (giorno {snapshot.IncubatorDay}/2).";
                }
                else
                {
                    _projectQuickLiveLabel.text = "<color=#9EC8E4>Live:</color> nessun processo attivo, pronto allo step successivo.";
                }
            }

            if (_projectQuickOutcomeLabel == null)
                return;

            if (_projectActive && !_analysisCompleted)
            {
                _projectQuickOutcomeLabel.text = "<color=#76828C>Outcome:</color> attendi la fine dell'analisi iniziale per vedere combinazioni consigliate e vincoli reali disponibili.";
                return;
            }

            if (snapshot.IncubatorStepDone)
            {
                Item seed = _incubatorPanel?.ReadySeedPreview ?? GetLatestPlayerSeed();
                _projectQuickOutcomeLabel.text = "<color=#7EFD80>Outcome:</color> " + BuildItemOutcomeLine("Seed", seed);
                return;
            }

            if (snapshot.FusionStepDone)
            {
                Item preSeed = _fusionPanel?.ReadyPreSeedPreview ?? GetLatestPlayerItemByType(Items.PreSeed);
                _projectQuickOutcomeLabel.text = "<color=#7EFD80>Outcome:</color> " + BuildItemOutcomeLine("Pre-seed", preSeed);
                return;
            }

            if (snapshot.CatalizzatoreStepDone)
            {
                Item matured = _catalizzatorePanel?.ReadyMaturedPreviewSource ?? GetLatestPlayerSporeByStage(SporeStage.Matured);
                _projectQuickOutcomeLabel.text = "<color=#7EFD80>Outcome:</color> " + BuildItemOutcomeLine("Spora Maturata", matured);
                return;
            }

            if (snapshot.ExtractorStepDone)
            {
                Item raw = GetLatestPlayerSporeByStage(SporeStage.Raw);
                if (raw != null)
                {
                    _projectQuickOutcomeLabel.text = "<color=#7EFD80>Outcome:</color> " + BuildItemOutcomeLine("Spora RAW", raw);
                }
                else
                {
                    var snap = _extractor?.GetFirstCompletedResultSnapshot();
                    _projectQuickOutcomeLabel.text = snap != null
                        ? $"<color=#7EFD80>Outcome:</color> <color=#98CFFF>Spora RAW</color> pronta al ritiro | TRATTI <color=#7EFD80>{ExtractorTooltipTexts.GeneticTypeToTrattiLabel(snap.GeneticTypeValue)}</color> | FAMIGLIA <color=#98CFFF>{snap.Famiglia ?? "—"}</color>."
                        : "<color=#7EFD80>Outcome:</color> estrazione completata. Raccogli la Spora RAW per vedere metadata e tratti.";
                }

                return;
            }

            _projectQuickOutcomeLabel.text = "<color=#76828C>Outcome:</color> nessun risultato ancora. Completa il primo step per produrre la prima <color=#98CFFF>Spora RAW</color>.";
        }

        private string BuildItemOutcomeLine(string stageLabel, Item item)
        {
            if (item == null)
                return $"<color=#98CFFF>{stageLabel}</color> pronto, ma metadata non disponibili finche non viene ritirato nell'inventario.";

            string displayName = PlayerInventoryPanelController.GetItemDisplayName(item.TypeId, item);
            string tratti = ExtractorTooltipTexts.GeneticTypeToTrattiLabel(item.GeneticTypeValue);
            string percentMutare = ExtractorTooltipTexts.GeneticTypeToPercentMutare(item.GeneticTypeValue);
            string family = string.IsNullOrWhiteSpace(item.FamilyMetadata) ? "STANDARD" : item.FamilyMetadata;
            string origine = ExtractorTooltipTexts.GetOriginTraceLabel(item);

            string metadata =
                $"TRATTI <color=#7EFD80>{tratti}</color> | MUTARE <color=#B98DFF>{percentMutare}</color> | FAMIGLIA <color=#98CFFF>{family}</color> | ORIGINE <color=#98CFFF>{origine}</color>";
            return $"<color=#98CFFF>{stageLabel}</color> prodotto: <color=#7EFD80>{displayName}</color> | {metadata}";
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
                {
                    _analysisStatusLabel.text =
                        "Premi <color=#98CFFF>CREA NUOVO SEME</color> per analizzare frutti disponibili e combinazioni possibili.";
                }
                SetSelectedProjectSchema(
                    "Progetto: --",
                    "Item necessari: seleziona una tipologia.",
                    "Status: in attesa selezione.");
                SetProjectTypeButtonsInteractable(false);
                UpdateProjectTypeButtonsState();
                if (_btnAnalysisOpenCurrentStep != null)
                {
                    _btnAnalysisOpenCurrentStep.SetEnabled(false);
                    _btnAnalysisOpenCurrentStep.text = "APRI STEP CORRENTE";
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
                    _analysisStatusLabel.text =
                        $"Analisi in corso... <color=#B98DFF>{Mathf.RoundToInt(analysisPct * 100f)}%</color> | scanning inventario player + Seed Storage.";
                }
                SetSelectedProjectSchema(
                    "Progetto: --",
                    "Item necessari: in valutazione...",
                    "Status: analisi in corso.");
                SetProjectTypeButtonsInteractable(false);
                UpdateProjectTypeButtonsState();
                if (_btnAnalysisOpenCurrentStep != null)
                {
                    _btnAnalysisOpenCurrentStep.SetEnabled(false);
                    _btnAnalysisOpenCurrentStep.text = "ANALISI IN CORSO...";
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
                    _analysisStatusLabel.text = "Analisi in attesa. Riavvia progetto per calcolare suggerimenti.";
                SetSelectedProjectSchema(
                    "Progetto: --",
                    "Item necessari: seleziona una tipologia.",
                    "Status: in attesa selezione.");
                SetProjectTypeButtonsInteractable(false);
                UpdateProjectTypeButtonsState();
                if (_btnAnalysisOpenCurrentStep != null)
                {
                    _btnAnalysisOpenCurrentStep.SetEnabled(false);
                    _btnAnalysisOpenCurrentStep.text = "APRI STEP CORRENTE";
                }
                if (_btnAnalysisCancelSelection != null)
                    _btnAnalysisCancelSelection.SetEnabled(false);
                return;
            }

            int totalFruits = _projectTypeAnalysis.PlayerFruitTotal + _projectTypeAnalysis.StorageFruitTotal;
            if (_analysisStatusLabel != null)
            {
                _analysisStatusLabel.text =
                    $"Analisi completata: frutti disponibili <color=#98CFFF>{totalFruits}</color> (inventario {_projectTypeAnalysis.PlayerFruitTotal} + storage {_projectTypeAnalysis.StorageFruitTotal}) | reagenti X/Y: <color=#98CFFF>{_projectTypeAnalysis.ReagentXCount}</color>/<color=#98CFFF>{_projectTypeAnalysis.ReagentYCount}</color>.";
            }

            SetProjectTypeButtonsInteractable(true);

            UpdateProjectTypeButtonsState();
            var schema = BuildSelectedProjectSchema();
            SetSelectedProjectSchema(schema.projectLine, schema.requiredItemsLine, schema.statusLine);
            if (_btnAnalysisOpenCurrentStep != null)
            {
                bool canOpen = _selectedProjectType != SeedProjectType.None;
                _btnAnalysisOpenCurrentStep.SetEnabled(canOpen);
                _btnAnalysisOpenCurrentStep.text = canOpen ? "APRI STEP CORRENTE" : "SELEZIONA TIPOLOGIA";
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
                        ? "Progetto completato."
                        : $"Suggerimento step: {BuildStepReminderByProjectType(currentStep)}";
                    string initialHint = _initialProjectType != SeedProjectType.None
                        ? $"<color=#7E90A0>Intento iniziale:</color> {ProjectTypeLabel(_initialProjectType)}. "
                        : string.Empty;
                    _analysisChangeWarningLabel.text =
                        $"{initialHint}<color=#9CB3C4>Direzione attuale:</color> {ProjectTypeLabel(_selectedProjectType)}. {stepHint}";
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
            string sameFruitRequirement = replicaNow
                ? $"Frutti uguali x2: <color=#7EFD80>PRESENTE</color> ({bestDuplicateTypeId} x{bestDuplicateCount})."
                : $"Frutti uguali x2: <color=#76828C>ASSENTE</color> (manca almeno {Mathf.Max(0, 2 - bestDuplicateCount)} unita sul tipo piu disponibile: {bestDuplicateTypeId} x{bestDuplicateCount}).";
            string twoFruitTypesRequirement = hybridNow
                ? $"Frutti diversi x2: <color=#7EFD80>PRESENTE</color> (tipologie rilevate: {distinctFruitTypes}; {fruitTypesSummary})."
                : totalFruit < 2
                    ? $"Frutti diversi x2: <color=#76828C>ASSENTE</color> (frutti totali {totalFruit}/2, ne manca {2 - totalFruit})."
                    : $"Frutti diversi x2: <color=#76828C>ASSENTE</color> (manca seconda tipologia: presenti {distinctFruitTypes}; {fruitTypesSummary}).";
            string reagentRequirement = (hasReagentX || hasReagentY)
                ? $"Reagente X/Y: <color=#7EFD80>PRESENTE</color> (X:{reagentXCount}, Y:{reagentYCount})."
                : "Reagente X/Y: <color=#76828C>ASSENTE</color> (manca almeno REAG-X o REAG-Y).";

            var replica = new ProjectTypeAdvice(
                replicaNow,
                $"{sameFruitRequirement} Consiglio: usa 2 frutti della stessa origine per massima coerenza.");
            var hybrid = new ProjectTypeAdvice(
                hybridNow,
                $"{twoFruitTypesRequirement} Consiglio: usa 2 frutti diversi per aprire tratti combinati.");
            var newProfile = new ProjectTypeAdvice(
                newProfileNow,
                $"{twoFruitTypesRequirement} {reagentRequirement} Consiglio: combina frutti diversi e usa reagente X/Y per orientare outcome.");

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
                return "nessun frutto disponibile";

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
                ProjectStep.Extractor => "Reminder Replica: seleziona frutti stessa origine.",
                ProjectStep.Catalizzatore => "Reminder Replica: mantieni coerenza materiale nelle spore.",
                ProjectStep.Fusion => "Reminder Replica: combina 2 spore allineate per output stabile.",
                ProjectStep.Incubator => "Reminder Replica: evita deviazioni se vuoi fedelta al genitore.",
                _ => "Replica completata: confronta outcome con l'origine."
            };

            string hybridAdvice = currentStep switch
            {
                ProjectStep.Extractor => "Reminder Ibrido: prepara 2 origini diverse.",
                ProjectStep.Catalizzatore => "Reminder Ibrido: matura entrambe le linee prima della fusion.",
                ProjectStep.Fusion => "Reminder Ibrido: mixa spore differenti per tratti combinati.",
                ProjectStep.Incubator => "Reminder Ibrido: reagente Y puo aiutare la convergenza famiglia.",
                _ => "Ibrido completato: verifica sinergia tratti."
            };

            string newProfileAdvice = currentStep switch
            {
                ProjectStep.Extractor => "Reminder Nuovo Profilo: seleziona frutti con identita lontane.",
                ProjectStep.Catalizzatore => "Reminder Nuovo Profilo: conserva variabilita utile al design finale.",
                ProjectStep.Fusion => "Reminder Nuovo Profilo: qui nasce il nuovo outcome mentale.",
                ProjectStep.Incubator => "Reminder Nuovo Profilo: usa reagente X/Y per orientare nome/poteri.",
                _ => "Nuovo Profilo completato: documenta il risultato ottenuto."
            };

            return _selectedProjectType switch
            {
                SeedProjectType.Replica => replicaAdvice,
                SeedProjectType.Hybrid => hybridAdvice,
                SeedProjectType.NewProfile => newProfileAdvice,
                _ => "Scegli una tipologia per ricevere suggerimenti mirati."
            };
        }

        private static string ProjectTypeLabel(SeedProjectType type)
        {
            return type switch
            {
                SeedProjectType.Replica => "Replica",
                SeedProjectType.Hybrid => "Ibrido",
                SeedProjectType.NewProfile => "Nuovo Profilo",
                _ => "Non definito"
            };
        }

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
                    "Progetto: --",
                    "Item necessari: seleziona una tipologia.",
                    "Status: in attesa selezione.");
            }

            int totalFruits = _projectTypeAnalysis.PlayerFruitTotal + _projectTypeAnalysis.StorageFruitTotal;
            bool hasAnyReagent = _projectTypeAnalysis.ReagentXCount > 0 || _projectTypeAnalysis.ReagentYCount > 0;

            switch (type)
            {
                case SeedProjectType.Replica:
                {
                    bool executable = _projectTypeAnalysis.Replica.AvailableNow;
                    string projectLine = "Progetto: REPLICA";
                    string requiredItemsLine =
                        $"Item necessari: 2x frutto uguale (best: {_projectTypeAnalysis.BestDuplicateFruitTypeId} x{_projectTypeAnalysis.BestDuplicateFruitCount}).";
                    string itemStatus = executable
                        ? "<color=#7EFD80>Item presenti</color>"
                        : $"<color=#76828C>Item mancanti</color> (serve ancora {Mathf.Max(0, 2 - _projectTypeAnalysis.BestDuplicateFruitCount)} unita dello stesso frutto).";
                    string projectStatus = executable
                        ? "<color=#7EFD80>Progetto eseguibile</color>"
                        : "<color=#76828C>Progetto non eseguibile</color>";
                    return (projectLine, requiredItemsLine, $"Status: {itemStatus} | {projectStatus}.");
                }
                case SeedProjectType.Hybrid:
                {
                    bool executable = _projectTypeAnalysis.Hybrid.AvailableNow;
                    string projectLine = "Progetto: IBRIDO";
                    string requiredItemsLine = "Item necessari: 2x frutti diversi.";
                    string missingReason = totalFruits < 2
                        ? $"(frutti totali {totalFruits}/2)"
                        : $"(tipologie disponibili {_projectTypeAnalysis.DistinctFruitTypes}/2)";
                    string itemStatus = executable
                        ? "<color=#7EFD80>Item presenti</color>"
                        : $"<color=#76828C>Item mancanti</color> {missingReason}";
                    string projectStatus = executable
                        ? "<color=#7EFD80>Progetto eseguibile</color>"
                        : "<color=#76828C>Progetto non eseguibile</color>";
                    return (projectLine, requiredItemsLine, $"Status: {itemStatus} | {projectStatus}.");
                }
                case SeedProjectType.NewProfile:
                {
                    bool executable = _projectTypeAnalysis.NewProfile.AvailableNow;
                    string projectLine = "Progetto: NUOVO PROFILO";
                    string requiredItemsLine =
                        $"Item necessari: 2x frutti diversi + 1x reagente (X o Y). Reagenti ora: X={_projectTypeAnalysis.ReagentXCount}, Y={_projectTypeAnalysis.ReagentYCount}.";
                    bool fruitReady = _projectTypeAnalysis.Hybrid.AvailableNow;
                    string missingReason = !fruitReady
                        ? "mancano frutti diversi"
                        : !hasAnyReagent
                            ? "manca REAG-X o REAG-Y"
                            : "ok";
                    string itemStatus = executable
                        ? "<color=#7EFD80>Item presenti</color>"
                        : $"<color=#76828C>Item mancanti</color> ({missingReason})";
                    string projectStatus = executable
                        ? "<color=#7EFD80>Progetto eseguibile</color>"
                        : "<color=#76828C>Progetto non eseguibile</color>";
                    return (projectLine, requiredItemsLine, $"Status: {itemStatus} | {projectStatus}.");
                }
                default:
                    return (
                        "Progetto: --",
                        "Item necessari: seleziona una tipologia.",
                        "Status: in attesa selezione.");
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
