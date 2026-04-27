using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using _Project.Sporae.Core;
using _Project.Systems.SeedStorage;
using _Project.Systems.FoodRoom;
using Sporae.Core;
using Sporae.Core.Localization;
using Sporae.DevTools;
using Sporae.Dome;
using Sporae.Dome.PotSystem.Growth;
using Sporae.Dome.PotSystem.Condition;
using Sporae.UI.UIToolkit.HUD;
using Sporae.UI.UIToolkit.FoodRoom;
using Sporae.UI.UIToolkit.NotificationsFoundation;

namespace _Project
{
    /// <summary>
    /// Controller della sequenza Fine giornata: Conferma → Snapshot → Diario → (ricerca notturna se azioni≥1) → Previsione → Riposo → Alba → chiusura.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class EndOfDaySequenceController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private float _nightResearchTransitionDelay = 1f;
        [Tooltip("Caratteri al secondo per l'effetto typewriter su Diario e Forecast.")]
        [SerializeField] private float _typewriterCharsPerSecond = 35f;

        private VisualElement _root;
        private VisualElement _overlay;
        private VisualElement _step1, _step2, _step3, _step4, _step5, _step6, _step7, _step8;
        private Label _snapshotTitle, _snapshotDate, _snapshotVault, _snapshotPh, _activitySummary, _drift, _notes;
        private Label _diarioText, _forecastToday, _forecastTomorrow, _forecastResearch;
        private Label _eodHibernationLine1, _eodHibernationLine2, _eodDayFrom, _eodDayTo;
        private VisualElement _dawnParamsList, _dawnTooltip;
        private Label _dawnTooltipTitle, _dawnTooltipDesc, _dawnTooltipTip;
        private Button _btnYes, _btnNo, _btnSnapshotConfirm, _btnDiarioContinue, _btnResearchHistorical, _btnResearchBotanical, _btnResearchVault, _btnResearchSkip, _btnSleep, _btnDawnContinue;

        private DayCycleSystem _dayCycleSystem;
        private SaveManager _saveManager;
        private DayActivityLog _dayActivityLog;
        private DiaryStatistics _diaryStatistics;
        private PhSystem _phSystem;
        private GameManager _gameManager;
        private NightEventsGenerator _nightEventsGenerator;
        private WikiUnlockService _wikiUnlockService;
        private DayCycleController _dayCycleController;
        private MissionManager _missionManager;
        private DomePotRegistry _potRegistry;
        private DemoSessionState _demoSessionState;

        private bool _bound;
        private VisualElement _eodVisualTreeBoundRoot;
        private bool _awaitingDawn;
        private bool _nightResearchChosen;
        private bool _researchLockedByNoActions;
        private int _dayBeforeTransition;
        private Dictionary<string, (string title, string desc, string tip)> _dawnTooltipData;
        private bool _dawnTooltipsRegistered;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null && _uiDocument.panelSettings == null)
            {
                var all = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
                foreach (var other in all)
                {
                    if (other != _uiDocument && other.panelSettings != null)
                    {
                        _uiDocument.panelSettings = other.panelSettings;
                        break;
                    }
                }
                _uiDocument.sortingOrder = 500;
            }

            _dayCycleSystem = ServiceContainer.Instance?.Get<DayCycleSystem>(suppressWarning: true);
            _saveManager = ServiceContainer.Instance?.Get<SaveManager>(suppressWarning: true);
            _dayActivityLog = ServiceContainer.Instance?.Get<DayActivityLog>(suppressWarning: true);
            _diaryStatistics = ServiceContainer.Instance?.Get<DiaryStatistics>(suppressWarning: true);
            _phSystem = ServiceContainer.Instance?.Get<PhSystem>(suppressWarning: true);
            _gameManager = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            _nightEventsGenerator = ServiceContainer.Instance?.Get<NightEventsGenerator>(suppressWarning: true);
            _wikiUnlockService = ServiceContainer.Instance?.Get<WikiUnlockService>(suppressWarning: true);
            _dayCycleController = FindObjectOfType<DayCycleController>();
            _missionManager = ServiceContainer.Instance?.Get<MissionManager>(suppressWarning: true);
            _potRegistry = ServiceContainer.Instance?.Get<DomePotRegistry>(suppressWarning: true);
            _demoSessionState = ServiceContainer.Instance?.Get<DemoSessionState>(suppressWarning: true);
        }

        private void OnEnable()
        {
            if (_dayCycleSystem != null)
                _dayCycleSystem.OnDayChanged += OnDayChanged;
        }

        private void OnDisable()
        {
            if (_dayCycleSystem != null)
                _dayCycleSystem.OnDayChanged -= OnDayChanged;
        }

        private void OnDayChanged(int newDay)
        {
            if (!_awaitingDawn) return;
            _awaitingDawn = false;
            PopulateDawn(newDay);
            ShowStep(8);
        }

        private void TryBind()
        {
            if (_uiDocument == null) return;
            var currentRoot = _uiDocument.rootVisualElement;
            if (currentRoot == null) return;

            // Stesso albero UIToolkit già collegato correttamente
            if (_bound && _eodVisualTreeBoundRoot == currentRoot && _btnYes != null && _btnNo != null)
                return;

            // Nuovo root o re-bind dopo bind incompleto: stacca handler vecchi per evitare doppie iscrizioni / ref stale
            if (_btnYes != null || _btnNo != null || _btnSnapshotConfirm != null)
                DetachEodButtonHandlers();

            _eodVisualTreeBoundRoot = currentRoot;
            _root = currentRoot;
            _overlay = _root.Q<VisualElement>("eod-overlay");
            _step1 = _root.Q<VisualElement>("eod-step1");
            _step2 = _root.Q<VisualElement>("eod-step2");
            _step3 = _root.Q<VisualElement>("eod-step3");
            _step4 = _root.Q<VisualElement>("eod-step4");
            _step5 = _root.Q<VisualElement>("eod-step5");
            _step6 = _root.Q<VisualElement>("eod-step6");
            _step7 = _root.Q<VisualElement>("eod-step7");
            _step8 = _root.Q<VisualElement>("eod-step8");

            _eodHibernationLine1 = _root.Q<Label>("eod-hibernation-line1");
            _eodHibernationLine2 = _root.Q<Label>("eod-hibernation-line2");
            _eodDayFrom = _root.Q<Label>("eod-day-from");
            _eodDayTo = _root.Q<Label>("eod-day-to");

            _dawnParamsList = _root.Q<VisualElement>("eod-dawn-params-list");
            _dawnTooltip = _root.Q<VisualElement>("eod-dawn-tooltip");
            _dawnTooltipTitle = _root.Q<Label>("eod-dawn-tooltip-title");
            _dawnTooltipDesc = _root.Q<Label>("eod-dawn-tooltip-desc");
            _dawnTooltipTip = _root.Q<Label>("eod-dawn-tooltip-tip");

            _snapshotTitle = _root.Q<Label>("eod-snapshot-title");
            _snapshotDate = _root.Q<Label>("eod-snapshot-date");
            _snapshotVault = _root.Q<Label>("eod-snapshot-vault");
            _snapshotPh = _root.Q<Label>("eod-snapshot-ph");
            _activitySummary = _root.Q<Label>("eod-activity-summary");
            _drift = _root.Q<Label>("eod-drift");
            _notes = _root.Q<Label>("eod-notes");

            _diarioText = _root.Q<Label>("eod-diario-text");
            _forecastToday = _root.Q<Label>("eod-forecast-today");
            _forecastTomorrow = _root.Q<Label>("eod-forecast-tomorrow");
            _forecastResearch = _root.Q<Label>("eod-forecast-research");

            _btnYes = _root.Q<Button>("btn-eod-yes");
            _btnNo = _root.Q<Button>("btn-eod-no");
            _btnSnapshotConfirm = _root.Q<Button>("btn-eod-snapshot-confirm");
            _btnDiarioContinue = _root.Q<Button>("btn-eod-diario-continue");
            _btnResearchHistorical = _root.Q<Button>("btn-eod-research-historical");
            _btnResearchBotanical = _root.Q<Button>("btn-eod-research-botanical");
            _btnResearchVault = _root.Q<Button>("btn-eod-research-vault");
            _btnResearchSkip = _root.Q<Button>("btn-eod-research-skip");
            _btnSleep = _root.Q<Button>("btn-eod-sleep");
            _btnDawnContinue = _root.Q<Button>("btn-eod-dawn-continue");

            RegisterModalButton(_btnYes, OnYesClicked);
            RegisterModalButton(_btnNo, OnNoClicked);
            RegisterModalButton(_btnSnapshotConfirm, OnSnapshotConfirmClicked);
            RegisterModalButton(_btnDiarioContinue, OnDiarioContinueClicked);
            RegisterModalButton(_btnResearchHistorical, OnResearchHistoricalClicked);
            RegisterModalButton(_btnResearchBotanical, OnResearchBotanicalClicked);
            RegisterModalButton(_btnResearchVault, OnResearchVaultClicked);
            RegisterModalButton(_btnResearchSkip, OnResearchSkipClicked);
            RegisterModalButton(_btnSleep, OnSleepClicked);
            RegisterModalButton(_btnDawnContinue, OnDawnContinueClicked);

            _bound = _btnYes != null && _btnNo != null;
            if (!_bound)
                SporiumLogger.LogWarning(LogCategory.UI,
                    "EndOfDaySequenceController: TryBind incompleto (btn-eod-yes/no assenti). Riprova al frame successivo.");
        }

        private void DetachEodButtonHandlers()
        {
            if (_btnYes != null) _btnYes.clicked -= OnYesClicked;
            if (_btnNo != null) _btnNo.clicked -= OnNoClicked;
            if (_btnSnapshotConfirm != null) _btnSnapshotConfirm.clicked -= OnSnapshotConfirmClicked;
            if (_btnDiarioContinue != null) _btnDiarioContinue.clicked -= OnDiarioContinueClicked;
            if (_btnResearchHistorical != null) _btnResearchHistorical.clicked -= OnResearchHistoricalClicked;
            if (_btnResearchBotanical != null) _btnResearchBotanical.clicked -= OnResearchBotanicalClicked;
            if (_btnResearchVault != null) _btnResearchVault.clicked -= OnResearchVaultClicked;
            if (_btnResearchSkip != null) _btnResearchSkip.clicked -= OnResearchSkipClicked;
            if (_btnSleep != null) _btnSleep.clicked -= OnSleepClicked;
            if (_btnDawnContinue != null) _btnDawnContinue.clicked -= OnDawnContinueClicked;
        }

        private static void RegisterModalButton(Button button, System.Action handler)
        {
            if (button == null || handler == null)
                return;

            foreach (var child in button.Children())
                child.pickingMode = PickingMode.Ignore;

            button.clicked += handler;
            // StopPropagation only — handler must NOT be called here a second time
            button.RegisterCallback<ClickEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
        }

        /// <summary>Sopra HUD, toast Foundation (150) e pannelli (Food/Lab/PlantCard); full-screen EoD in primo piano.</summary>
        private const int EodSortingOrder = 2500;

        public void StartSequence()
        {
            _nightResearchChosen = false;
            _awaitingDawn = false;
            gameObject.SetActive(true);
            if (_uiDocument != null)
            {
                _uiDocument.sortingOrder = EodSortingOrder;
                if (_uiDocument.rootVisualElement != null)
                {
                    _uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
                    _uiDocument.rootVisualElement.SetEnabled(true);
                    _uiDocument.rootVisualElement.pickingMode = PickingMode.Position;
                    _uiDocument.rootVisualElement.BringToFront();
                }
            }
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.Flex;
                _overlay.pickingMode = PickingMode.Position;
            }
            if (_root != null)
                _root.pickingMode = PickingMode.Position;
            // Chiudi il pannello Food/Kitchen se aperto, così non intercetta i click (stesso sorting order 1000 → conflitto)
            var foodPanel = FindObjectOfType<FoodRoomPanelController>();
            if (foodPanel != null && foodPanel.IsVisible)
                foodPanel.Hide();
            // Binding e ShowStep al frame successivo: rootVisualElement può non essere pronto nello stesso frame dopo SetActive(true)
            StartCoroutine(DeferredStartStep1());
        }

        private IEnumerator DeferredStartStep1()
        {
            const int maxAttempts = 24;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                yield return null;
                TryBind();
                if (_btnYes != null && _btnNo != null)
                    break;
            }
                ShowStep(1);
        }

        /// <summary>Chiude la sequenza e torna al vault (es. su NO in Step 1).</summary>
        public void Hide()
        {
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.None;
                _overlay.pickingMode = PickingMode.Ignore;
            }
            if (_root != null)
            {
                _root.style.display = DisplayStyle.None;
                _root.pickingMode = PickingMode.Ignore;
            }
            if (_uiDocument != null && _uiDocument.rootVisualElement != null)
            {
                _uiDocument.rootVisualElement.style.display = DisplayStyle.None;
                _uiDocument.rootVisualElement.pickingMode = PickingMode.Ignore;
            }
            // Prossima apertura: forza re-bind se Unity ricrea l’albero UIToolkit
            _bound = false;
            _eodVisualTreeBoundRoot = null;
            gameObject.SetActive(false);
        }

        private void ShowStep(int step)
        {
            SetStepVisible(_step1, step == 1);
            SetStepVisible(_step2, step == 2);
            SetStepVisible(_step3, step == 3);
            SetStepVisible(_step4, step == 4);
            SetStepVisible(_step5, step == 5);
            SetStepVisible(_step6, step == 6);
            SetStepVisible(_step7, step == 7);
            SetStepVisible(_step8, step == 8);
            if (step == 2) PopulateSnapshot();
            if (step == 3) PopulateDiario();
            if (step == 4) { /* optional: disable if no actions */ }
            if (step == 5) PopulateForecast();
            if (step == 7)
            {
                if (_eodDayFrom != null) _eodDayFrom.text = LocalizationManager.GetString("eod.day_from", new Dictionary<string, string> { ["n"] = _dayBeforeTransition.ToString("D2") });
                if (_eodDayTo != null) _eodDayTo.text = LocalizationManager.GetString("eod.day_to", new Dictionary<string, string> { ["n"] = (_dayBeforeTransition + 1).ToString("D2") });
            }
        }

        private static void SetStepVisible(VisualElement el, bool visible)
        {
            if (el != null) el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private const string ColorGood = "#7FFF7A";
        private const string ColorWarn = "#FFB347";
        private const string ColorBad = "#FF6B6B";
        private const string ColorInfo = "#7FD9FF";
        private const string ColorMuted = "#9AA7B0";

        private struct SnapshotMetrics
        {
            public int Day;
            public int ActionsUsed;
            public int ActionsMax;
            public int CryEarned;
            public int CrySpent;
            public int CurrentCry;
            public int HarvestCount;
            public int WaterCount;
            public int StageChangesCount;
            public int ActiveAlerts;
            public int ActiveMissionCount;
            public int CompletedMissionCount;
        }

        private void PopulateSnapshot()
        {
            int day = _dayCycleSystem != null ? _dayCycleSystem.CurrentDay : 0;
            if (_snapshotTitle != null) _snapshotTitle.text = LocalizationManager.GetString("eod.snapshot_title", new Dictionary<string, string> { ["day"] = day.ToString() });
            if (_snapshotDate != null) _snapshotDate.text = LocalizationManager.GetString("eod.snapshot_date", new Dictionary<string, string> { ["date"] = System.DateTime.Now.ToString("dd.MM.yyyy") });
            if (_snapshotVault != null) _snapshotVault.text = LocalizationManager.GetString("eod.snapshot_vault");

            string phLine = LocalizationManager.GetString("eod.snapshot_ph_empty");
            if (_phSystem != null)
                phLine = LocalizationManager.GetString("eod.snapshot_ph", new Dictionary<string, string>
                {
                    ["ph"] = _phSystem.CurrentPh.ToString("F1"),
                    ["band"] = _phSystem.GetBandName()
                });
            if (_snapshotPh != null) _snapshotPh.text = phLine;

            var metrics = CollectSnapshotMetrics(day);
            string trendLabel = BuildTrendLabel(metrics);
            string trendColor = trendLabel == "MEGLIO" ? ColorGood : trendLabel == "PEGGIO" ? ColorBad : trendLabel == "SIMILE" ? ColorInfo : ColorWarn;
            string trendRich = $"<color={trendColor}><b>{trendLabel}</b></color>";

            if (_snapshotVault != null)
            {
                _snapshotVault.enableRichText = true;
                _snapshotVault.text = $"Stato Vault: operativo  •  Trend vs ieri: {trendRich}";
            }

            var conditions = _dayCycleController != null
                ? _dayCycleController.GetActiveConditionsForReport()
                : new List<(string PotId, int MoldRiskLevel, bool IsInfested)>();

            var alerts = BuildCriticalAlerts(conditions);
            string previousNightResearch = BuildPreviousNightResearchSummary(day);
            string narrative = BuildNarrativeParagraph(metrics, alerts, previousNightResearch);

            var sb = new StringBuilder();
            sb.AppendLine("<b><color=#7FD9FF>SNAPSHOT OPERATIVO</color></b>");
            sb.AppendLine(narrative);
            sb.AppendLine();
            sb.AppendLine("<b><color=#7FD9FF>[ ] ALERT</color></b>");
            if (alerts.Count > 0)
            {
                foreach (var alert in alerts)
                    sb.AppendLine($"• <color={ColorBad}>{alert}</color>");
            }
            else
            {
                sb.AppendLine($"• <color={ColorGood}>Nessun alert critico attivo.</color>");
            }
            sb.AppendLine();
            sb.AppendLine("<b><color=#7FD9FF>[ ] PANORAMICA STANZE</color></b>");
            AppendDomeSection(sb, conditions);
            sb.AppendLine();
            AppendKitchenSection(sb);
            sb.AppendLine();
            AppendBiologoSection(sb);
            sb.AppendLine();
            AppendInventorySection(sb);
            sb.AppendLine();
            AppendSeedStorageSection(sb);

            string activityStr = sb.Length > 0 ? sb.ToString().TrimEnd() : LocalizationManager.GetString("eod.activity_none");
            if (_activitySummary != null)
            {
                _activitySummary.enableRichText = true;
                _activitySummary.text = "";
                StartCoroutine(TerminalChunkReveal(_activitySummary, activityStr));
            }

            if (_drift != null)
                _drift.style.display = DisplayStyle.None;
            if (_notes != null)
                _notes.style.display = DisplayStyle.None;

            if (_diaryStatistics != null)
            {
                _diaryStatistics.StorePreviousSnapshot(new DiaryStatistics.SnapshotMetricsData
                {
                    Day = metrics.Day,
                    ActionsUsed = metrics.ActionsUsed,
                    ActionsMax = metrics.ActionsMax,
                    CryEarned = metrics.CryEarned,
                    CrySpent = metrics.CrySpent,
                    CurrentCry = metrics.CurrentCry,
                    HarvestCount = metrics.HarvestCount,
                    WaterCount = metrics.WaterCount,
                    StageChangesCount = metrics.StageChangesCount,
                    ActiveAlerts = metrics.ActiveAlerts,
                    ActiveMissionCount = metrics.ActiveMissionCount,
                    CompletedMissionCount = metrics.CompletedMissionCount
                });
            }
        }

        private SnapshotMetrics CollectSnapshotMetrics(int day)
        {
            int harvestCount = _dayActivityLog?.HarvestsThisDay?.Count ?? 0;
            int waterCount = _dayActivityLog?.PotIdsWateringTurnedOnThisDay?.Count ?? 0;
            int stageChanges = _dayActivityLog?.StageChangesThisDay?.Count ?? 0;
            int actionsMax = _gameManager?.ActionSystem?.MaxActions ?? 5;

            int alerts = 0;
            if (_gameManager?.SeedStorageSystem != null && !_gameManager.SeedStorageSystem.IsOn)
                alerts++;
            if (_gameManager?.FoodRoomSystem != null && !_gameManager.FoodRoomSystem.PantryIsOn)
                alerts++;
            if (_dayCycleController != null)
                alerts += _dayCycleController.GetActiveConditionsForReport().Count(c => c.IsInfested || c.MoldRiskLevel >= 2);

            return new SnapshotMetrics
            {
                Day = day,
                ActionsUsed = _diaryStatistics?.ActionsSpent ?? 0,
                ActionsMax = actionsMax,
                CryEarned = _diaryStatistics?.CryEarned ?? 0,
                CrySpent = _diaryStatistics?.CrySpent ?? 0,
                CurrentCry = _gameManager?.CurrentCRY ?? 0,
                HarvestCount = harvestCount,
                WaterCount = waterCount,
                StageChangesCount = stageChanges,
                ActiveAlerts = alerts,
                ActiveMissionCount = _missionManager?.CurrentMissions.Count ?? 0,
                CompletedMissionCount = _missionManager?.CompletedMissions.Count ?? 0
            };
        }

        private string BuildTrendLabel(SnapshotMetrics current)
        {
            if (_diaryStatistics == null || !_diaryStatistics.TryGetPreviousSnapshot(out var previous))
                return "N/D";

            float CurrentScore(int cryEarned, int crySpent, int harvestCount, int stageChangesCount, int actionsUsed, int activeAlerts) =>
                (cryEarned - crySpent) +
                (harvestCount * 2f) +
                (stageChangesCount * 1.5f) +
                (actionsUsed * 0.5f) -
                (activeAlerts * 2.5f);

            float delta =
                CurrentScore(current.CryEarned, current.CrySpent, current.HarvestCount, current.StageChangesCount, current.ActionsUsed, current.ActiveAlerts) -
                CurrentScore(previous.CryEarned, previous.CrySpent, previous.HarvestCount, previous.StageChangesCount, previous.ActionsUsed, previous.ActiveAlerts);
            if (delta > 1.5f) return "MEGLIO";
            if (delta < -1.5f) return "PEGGIO";
            return "SIMILE";
        }

        private List<string> BuildCriticalAlerts(IReadOnlyList<(string PotId, int MoldRiskLevel, bool IsInfested)> conditions)
        {
            var alerts = new List<string>();
            if (_gameManager?.SeedStorageSystem != null && !_gameManager.SeedStorageSystem.IsOn)
                alerts.Add("Seed Storage spento: gli item conservati possono deperire.");
            if (_gameManager?.FoodRoomSystem != null && !_gameManager.FoodRoomSystem.PantryIsOn)
                alerts.Add("Dispensa refrigerata spenta: il cibo può deteriorarsi.");

            if (conditions != null)
            {
                foreach (var c in conditions.Where(x => x.IsInfested || x.MoldRiskLevel >= 2))
                {
                    string reason = c.IsInfested ? "infestazione attiva" : "rischio muffa alto";
                    alerts.Add($"POT {FormatPotNumber(c.PotId)}: {reason}.");
                }
            }

            int fixedTomorrowCost = ComputeEstimatedFixedCostsForTomorrow();
            if (_gameManager != null && _gameManager.CurrentCRY < fixedTomorrowCost)
                alerts.Add($"CRY insufficienti per coprire i costi fissi stimati di domani ({fixedTomorrowCost}).");

            return alerts;
        }

        private string BuildPreviousNightResearchSummary(int currentDay)
        {
            int previousDay = Mathf.Max(1, currentDay - 1);
            if (_wikiUnlockService != null && _wikiUnlockService.TryGetNightResearchForDay(previousDay, out var branch))
            {
                if (string.Equals(branch, "Historical", System.StringComparison.OrdinalIgnoreCase))
                    return "Archivio storico";
                if (string.Equals(branch, "Botanical", System.StringComparison.OrdinalIgnoreCase))
                    return "Database botanico";
                if (string.Equals(branch, "Vault", System.StringComparison.OrdinalIgnoreCase))
                    return "Protocolli Vault";
                return branch;
            }

            return "nessuna ricerca registrata";
        }

        private string BuildNarrativeParagraph(SnapshotMetrics metrics, IReadOnlyList<string> alerts, string previousNightResearch)
        {
            var text = new StringBuilder();
            text.Append($"Giorno {metrics.Day}: hai usato {metrics.ActionsUsed}/{metrics.ActionsMax} azioni, ");
            text.Append($"con <color={ColorGood}>+{metrics.CryEarned} CRY</color> in entrata e <color={ColorWarn}>-{metrics.CrySpent} CRY</color> in uscita. ");
            text.Append($"Nel Dome risultano {metrics.HarvestCount} raccolti, {metrics.WaterCount} irrigazioni e {metrics.StageChangesCount} avanzamenti di stadio. ");
            text.Append($"Missioni: {metrics.ActiveMissionCount} attive / {metrics.CompletedMissionCount} completate totali. ");
            text.Append($"Ricerca della notte precedente: <color={ColorInfo}>{previousNightResearch}</color>. ");
            text.Append(alerts.Count > 0
                ? $"Sono presenti <color={ColorBad}>{alerts.Count} alert</color>."
                : $"Nessun alert critico rilevato.");
            return text.ToString();
        }

        private IEnumerator TerminalChunkReveal(Label label, string fullText)
        {
            if (label == null || string.IsNullOrEmpty(fullText))
                yield break;

            label.text = string.Empty;
            string[] lines = fullText.Split('\n');
            var finalOutput = new StringBuilder();
            float minTick = 0.018f;
            float maxTick = 0.055f;

            bool IsSectionHeader(string line)
            {
                if (string.IsNullOrWhiteSpace(line))
                    return false;
                return line.Contains("<b><color=#7FD9FF>") || line.StartsWith("<b>");
            }

            string BuildMaskedLine(string source, int visibleCount)
            {
                // Mantieni intatti i tag rich text; "rivela" solo il testo effettivo.
                var outLine = new StringBuilder();
                int shown = 0;
                bool inTag = false;
                for (int i = 0; i < source.Length; i++)
                {
                    char ch = source[i];
                    if (ch == '<')
                    {
                        inTag = true;
                        outLine.Append(ch);
                        continue;
                    }
                    if (inTag)
                    {
                        outLine.Append(ch);
                        if (ch == '>')
                            inTag = false;
                        continue;
                    }

                    if (shown < visibleCount)
                    {
                        outLine.Append(ch);
                        shown++;
                    }
                    else
                        break;
                }

                return outLine.ToString();
            }

            int VisibleTextLength(string source)
            {
                int len = 0;
                bool inTag = false;
                for (int i = 0; i < source.Length; i++)
                {
                    char ch = source[i];
                    if (ch == '<')
                    {
                        inTag = true;
                        continue;
                    }
                    if (inTag)
                    {
                        if (ch == '>')
                            inTag = false;
                        continue;
                    }
                    len++;
                }
                return len;
            }

            for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
            {
                string line = lines[lineIdx];
                int lineLen = VisibleTextLength(line);

                // Header: quasi istantaneo, blocco unico.
                if (IsSectionHeader(line) || string.IsNullOrWhiteSpace(line))
                {
                    finalOutput.Append(line);
                    if (lineIdx < lines.Length - 1)
                        finalOutput.Append('\n');
                    label.text = finalOutput.ToString();
                    yield return new WaitForSeconds(UnityEngine.Random.Range(minTick, maxTick));
                    continue;
                }

                // Corpo: reveal "DOS" a gruppi (2-3-4-6 char) con piccoli scatti.
                int visible = 0;
                while (visible < lineLen)
                {
                    int chunk = UnityEngine.Random.Range(2, 7);
                    visible = Mathf.Min(lineLen, visible + chunk);

                    var preview = new StringBuilder(finalOutput.ToString());
                    preview.Append(BuildMaskedLine(line, visible));
                    if (lineIdx < lines.Length - 1)
                        preview.Append('\n');
                    label.text = preview.ToString();

                    yield return new WaitForSeconds(UnityEngine.Random.Range(minTick, maxTick));
                }

                finalOutput.Append(line);
                if (lineIdx < lines.Length - 1)
                    finalOutput.Append('\n');
                label.text = finalOutput.ToString();
            }
        }

        private int ComputeEstimatedFixedCostsForTomorrow()
        {
            int total = 0;
            var endDayButton = FindObjectOfType<EndDayButton>();
            total += endDayButton != null ? endDayButton.GetDailyPowerCost() : (_dayCycleSystem?.DailyPowerCost ?? 20);

            if (_gameManager?.SeedStorageSystem != null)
                total += _gameManager.SeedStorageSystem.ComputeDailyCryCost();

            if (_gameManager?.FoodRoomSystem != null)
            {
                var food = _gameManager.FoodRoomSystem;
                if (food.FoodSynthIsOn)
                    total += food.FoodSynthDailyCost;
                if (food.PantryIsOn)
                    total += food.PantryDailyCost;
            }

            return total;
        }

        private void AppendDomeSection(StringBuilder sb, IReadOnlyList<(string PotId, int MoldRiskLevel, bool IsInfested)> conditions)
        {
            sb.AppendLine($"<b><color={ColorInfo}>[ ] Dome</color></b>");
            string phText = _phSystem != null ? $"{_phSystem.CurrentPh:F1} ({_phSystem.GetBandName()})" : "—";
            sb.AppendLine($"• Andamento pH: <color={ColorInfo}><b>{phText}</b></color>");
            sb.AppendLine("• Avvenimenti nella Dome:");

            int lines = 0;
            if (_potRegistry != null)
            {
                var pots = _potRegistry.GetPotsSnapshot();
                foreach (var pot in pots)
                {
                    if (pot == null || pot.PotActions?.PotState == null)
                        continue;

                    var state = pot.PotActions.PotState;
                    if (!state.HasPlant || state.Stage == (int)PlantStage.Empty)
                        continue;

                    string plantName = PlantDatabase.Instance?.GetPlantDataByCode(state.PlantCode)?.name ?? state.PlantCode ?? "Pianta";
                    string stageNow = ((PlantStage)state.Stage).ToString();
                    string stagePrev = state.Stage > (int)PlantStage.Seed ? ((PlantStage)(state.Stage - 1)).ToString() : stageNow;
                    string conditionName = PlantConditionSystem.GetConditionName((PlantCondition)state.ConditionLabel);

                    int stressPct = Mathf.Clamp(Mathf.RoundToInt(state.GetConsecutiveLedDays() / 5f * 100f), 0, 100);
                    string stressAdvice = stressPct >= 80
                        ? $"<color={ColorWarn}>Light Stress {stressPct}%: se non cambi LED, rischio light burn domani.</color>"
                        : stressPct >= 40
                            ? $"Light Stress {stressPct}%: monitorare LED."
                            : "Stress luce sotto soglia critica.";

                    sb.AppendLine($"  - {plantName} in POT-{FormatPotNumber(state.PotId)}: {stagePrev} → <color={ColorGood}><b>{stageNow}</b></color>, condizione <color={ColorInfo}>{conditionName}</color>. {stressAdvice}");
                    lines++;
                }
            }

            if (lines == 0)
                sb.AppendLine($"  - <color={ColorMuted}>Nessun evento Dome rilevante nel giorno appena passato.</color>");

            float predictedPhDrift = _dayCycleController != null ? _dayCycleController.GetPredictedPhDriftForNextDay() : float.NaN;
            string driftText = float.IsNaN(predictedPhDrift) ? "—" : predictedPhDrift.ToString("+#0.0;-#0.0;0", System.Globalization.CultureInfo.InvariantCulture);
            sb.AppendLine("• Conseguenze previste:");
            sb.AppendLine($"  - Deriva pH prevista domani: <color={ColorWarn}><b>{driftText}</b></color>");
            if (conditions != null && conditions.Count > 0)
            {
                var risky = conditions.Where(c => c.IsInfested || c.MoldRiskLevel >= 2)
                    .Select(c => $"POT-{FormatPotNumber(c.PotId)}")
                    .Distinct()
                    .ToList();
                if (risky.Count > 0)
                    sb.AppendLine($"  - Alert biologici attivi su: <color={ColorBad}><b>{string.Join(", ", risky)}</b></color>");
            }
        }

        private void AppendKitchenSection(StringBuilder sb)
        {
            sb.AppendLine($"<b><color={ColorInfo}>[ ] Cucina</color></b>");
            var food = _gameManager?.FoodRoomSystem;
            if (food == null)
            {
                sb.AppendLine("• Sistema cucina non disponibile.");
                return;
            }

            string prep = "nessuna";
            var growing = food.ProductionSlots.FirstOrDefault(s => s != null && s.State == SlotState.Growing);
            if (growing != null)
            {
                prep = growing.Type == FoodProductionType.Meat ? "Carne sintetica" :
                    growing.Type == FoodProductionType.Fungus ? "Funghi" :
                    growing.Type == FoodProductionType.Vegetable ? "Ortaggi" : "Produzione";
                prep += $" ({growing.DaysRemaining} giorno/i rimanenti)";
            }

            string waterStatus = !food.WaterSlot.IsActive ? "no" : "sì";
            int pantryTotal = food.GetPantryQuantity(FoodProductionType.Vegetable) + food.GetPantryQuantity(FoodProductionType.Fungus) + food.GetPantryQuantity(FoodProductionType.Meat);
            string pantryState = food.PantryIsOn ? $"ON, {pantryTotal} item conservati" : "OFF (rischio deperimento)";

            sb.AppendLine($"• Preparazione in corso: <color={ColorInfo}><b>{prep}</b></color>");
            sb.AppendLine($"• Potabilizzazione in corso: <color={(food.WaterSlot.IsActive ? ColorWarn : ColorGood)}><b>{waterStatus}</b></color>");
            sb.AppendLine($"• Dispensa Refrigerata: <color={(food.PantryIsOn ? ColorGood : ColorBad)}><b>{pantryState}</b></color>");
        }

        private void AppendBiologoSection(StringBuilder sb)
        {
            sb.AppendLine($"<b><color={ColorInfo}>[ ] Biologo</color></b>");
            float hydration = _gameManager?.PlayerHydrationSystem?.HydrationPercent ?? -1f;
            float hydrationLostToday = _gameManager?.HydrationLostTodayPercent ?? -1f;
            int actions = _gameManager?.ActionsLeft ?? 0;
            int maxActions = _gameManager?.ActionSystem?.MaxActions ?? 0;
            bool ateMeal = _gameManager != null && _gameManager.AteMealSincePreviousDawn;
            int noMealDays = _gameManager?.ConsecutiveDaysWithoutMeal ?? 0;
            int daysUntilMalus = Mathf.Max(0, 2 - noMealDays);

            string hydrationText = hydration < 0f ? "—" : $"{hydration:F0}%";
            string hydrationLostText = hydrationLostToday < 0f ? "—" : $"{hydrationLostToday:F0}%";
            string eatText = ateMeal ? "ha mangiato oggi" : "non ha mangiato oggi";
            string malusText = ateMeal ? "nessun malus fame previsto domani" : (daysUntilMalus == 0 ? "malus fame al prossimo giorno se non mangia" : $"mangiare entro {daysUntilMalus} giorno/i per evitare malus");

            sb.AppendLine($"• Condizioni biologo: Idratazione <color={ColorInfo}><b>{hydrationText}</b></color> | Azioni <color={ColorInfo}><b>{actions}/{maxActions}</b></color>");
            sb.AppendLine($"• Idratazione persa oggi: <color={(hydrationLostToday > 0f ? ColorWarn : ColorGood)}><b>{hydrationLostText}</b></color>");
            sb.AppendLine($"• Nutrizione: <color={(ateMeal ? ColorGood : ColorWarn)}><b>{eatText}</b></color>");
            sb.AppendLine($"• Finestra sicurezza fame: <color={(ateMeal ? ColorGood : ColorWarn)}>{malusText}</color>");
        }

        private void AppendInventorySection(StringBuilder sb)
        {
            sb.AppendLine($"<b><color={ColorInfo}>[ ] Inventario</color></b>");
            string overview = BuildInventorySummary();
            int deteriorating = CountLowQualityItemsInInventory();
            sb.AppendLine($"• Overview item: <color={ColorInfo}>{overview}</color>");
            sb.AppendLine(deteriorating > 0
                ? $"• Item in deperimento: <color={ColorWarn}><b>{deteriorating}</b></color>"
                : $"• Item in deperimento: <color={ColorGood}><b>nessuno</b></color>");
        }

        private void AppendSeedStorageSection(StringBuilder sb)
        {
            sb.AppendLine($"<b><color={ColorInfo}>[ ] Seed Storage</color></b>");
            var ss = _gameManager?.SeedStorageSystem;
            if (ss == null)
            {
                sb.AppendLine("• Sistema non disponibile.");
                return;
            }

            int occupied = 0;
            var occupiedDetails = new List<string>();
            for (int i = 0; i < SeedStorageSystem.SlotCount; i++)
            {
                if (!ss.IsSlotUnlocked(i) || ss.SlotIsEmpty(i))
                    continue;

                occupied++;
                string typeId = ss.GetSlotTypeId(i) ?? "?";
                int qty = ss.GetSlotQuantity(i);
                occupiedDetails.Add($"S{i + 1}:{typeId} x{qty}");
            }

            string statusText = ss.IsOn ? "OK" : "OFF (rischio deperimento)";
            string statusColor = ss.IsOn ? ColorGood : ColorBad;
            sb.AppendLine($"• Stato: <color={statusColor}><b>{statusText}</b></color>");
            sb.AppendLine($"• Slot occupati: <color={ColorInfo}><b>{occupied}/{SeedStorageSystem.SlotCount}</b></color>");
            sb.AppendLine($"• Dettaglio: <color={(occupiedDetails.Count > 0 ? ColorInfo : ColorMuted)}>{(occupiedDetails.Count > 0 ? string.Join(", ", occupiedDetails) : "nessuno")}</color>");
            AppendSeedStorageDayTransfers(sb);
        }

        private void AppendSeedStorageDayTransfers(StringBuilder sb)
        {
            var entries = _dayActivityLog?.SeedStorageEntriesThisDay;
            if (entries == null || entries.Count == 0)
            {
                sb.AppendLine($"• Operazioni giornata: <color={ColorMuted}>nessuna</color>");
                return;
            }

            foreach (var entry in entries)
            {
                string verb = string.Equals(entry.Action, "Deposit", System.StringComparison.OrdinalIgnoreCase)
                    ? "Depositati"
                    : string.Equals(entry.Action, "Withdraw", System.StringComparison.OrdinalIgnoreCase)
                        ? "Prelevati"
                        : entry.Action;
                sb.AppendLine($"• Operazioni giornata: <color={ColorInfo}>{verb} {entry.Count} item ({entry.Detail})</color>");
            }
        }

        private int CountLowQualityItemsInInventory()
        {
            var inv = _gameManager?.PlayerInventory;
            if (inv == null)
                return 0;

            int count = 0;
            foreach (var slot in inv.Items)
            {
                if (slot == null)
                    continue;

                foreach (var item in slot.Items)
                {
                    if (item?.ItemConfig == null)
                        continue;
                    if (item.Quality <= 1f)
                        count++;
                }
            }
            return count;
        }

        private string BuildInventorySummary()
        {
            var inv = _gameManager?.PlayerInventory;
            if (inv == null) return "—";
            int pure = 0, evil = 0, standard = 0, spores = 0, seeds = 0, reagents = 0;
            foreach (var slot in inv.Items)
            {
                if (slot == null) continue;
                foreach (var item in slot.Items)
                {
                    if (slot.TypeId == Items.SporeGeneric)
                    {
                        spores++;
                        var fam = (item?.FamilyMetadata ?? "").ToUpperInvariant();
                        if (fam.Contains("PURE")) pure++;
                        else if (fam.Contains("EVIL")) evil++;
                        else standard++;
                    }
                    else if (slot.TypeId == Items.PreSeed ||
                             (PlantDatabase.Instance != null && PlantDatabase.Instance.IsRegisteredSeedTypeId(slot.TypeId)))
                        seeds++;
                    else if (slot.TypeId == Items.ReagentX || slot.TypeId == Items.ReagentY)
                        reagents++;
                }
            }
            var parts = new List<string>();
            if (spores > 0)
            {
                if (pure > 0 || evil > 0 || standard > 0)
                    parts.Add(LocalizationManager.GetString("eod.inv_spores_detail", new Dictionary<string, string>
                    {
                        ["n"] = spores.ToString(),
                        ["pure"] = pure.ToString(),
                        ["evil"] = evil.ToString(),
                        ["standard"] = standard.ToString()
                    }));
                else
                    parts.Add(LocalizationManager.GetString("eod.inv_spores", new Dictionary<string, string> { ["n"] = spores.ToString() }));
            }
            if (seeds > 0) parts.Add(LocalizationManager.GetString("eod.inv_seeds", new Dictionary<string, string> { ["n"] = seeds.ToString() }));
            if (reagents > 0) parts.Add(LocalizationManager.GetString("eod.inv_reagents", new Dictionary<string, string> { ["n"] = reagents.ToString() }));
            return parts.Count > 0 ? string.Join(", ", parts) + "." : "—";
        }

        private string BuildSeedStorageSummary()
        {
            var seedParts = new List<string>();
            int preSeed = 0;
            var seedByTypeId = new Dictionary<string, int>();
            var pdb = PlantDatabase.Instance;

            void AccumulateFromInventory(Inventory inv)
            {
                if (inv == null) return;
                foreach (var slot in inv.Items)
                {
                    if (slot == null) continue;
                    foreach (var item in slot.Items)
                    {
                        if (slot.TypeId == Items.PreSeed) preSeed++;
                        else if (pdb != null && pdb.IsRegisteredSeedTypeId(slot.TypeId))
                        {
                            if (seedByTypeId.TryGetValue(slot.TypeId, out int c))
                                seedByTypeId[slot.TypeId] = c + 1;
                            else
                                seedByTypeId[slot.TypeId] = 1;
                        }
                    }
                }
            }

            AccumulateFromInventory(_gameManager?.PlayerInventory);

            var ss = _gameManager?.SeedStorageSystem;
            if (ss != null)
            {
                ss.GetSeedSummaryCounts(out int preInVault, out var seedInVault);
                preSeed += preInVault;
                foreach (var kv in seedInVault)
                {
                    if (seedByTypeId.TryGetValue(kv.Key, out int c))
                        seedByTypeId[kv.Key] = c + kv.Value;
                    else
                        seedByTypeId[kv.Key] = kv.Value;
                }
            }

            if (preSeed > 0) seedParts.Add(LocalizationManager.GetString("eod.seed_preseed", new Dictionary<string, string> { ["n"] = preSeed.ToString() }));
            foreach (var kv in seedByTypeId.OrderBy(k => k.Key))
                seedParts.Add($"{kv.Value} {kv.Key}");
            return seedParts.Count > 0 ? string.Join(", ", seedParts) + "." : "—";
        }

        private string BuildKitchenFoodSummary()
        {
            var foodRoom = _gameManager?.FoodRoomSystem;
            if (foodRoom == null) return "—";
            var parts = new List<string>();
            foreach (var slot in foodRoom.ProductionSlots)
            {
                if (slot.State == SlotState.Growing)
                {
                    string typeName = slot.Type == FoodProductionType.Vegetable
                        ? LocalizationManager.GetString("eod.kitchen_veg")
                        : slot.Type == FoodProductionType.Fungus
                            ? LocalizationManager.GetString("eod.kitchen_fungus")
                            : slot.Type == FoodProductionType.Meat
                                ? LocalizationManager.GetString("eod.kitchen_meat")
                                : LocalizationManager.GetString("eod.kitchen_food");
                    parts.Add(LocalizationManager.GetString("eod.kitchen_growing", new Dictionary<string, string>
                    {
                        ["type"] = typeName,
                        ["days"] = slot.DaysRemaining.ToString()
                    }));
                }
                else if (slot.State == SlotState.Ready)
                {
                    string typeName = slot.Type == FoodProductionType.Vegetable
                        ? LocalizationManager.GetString("eod.kitchen_veg")
                        : slot.Type == FoodProductionType.Fungus
                            ? LocalizationManager.GetString("eod.kitchen_fungus")
                            : slot.Type == FoodProductionType.Meat
                                ? LocalizationManager.GetString("eod.kitchen_meat")
                                : LocalizationManager.GetString("eod.kitchen_food");
                    parts.Add(LocalizationManager.GetString("eod.kitchen_ready", new Dictionary<string, string> { ["type"] = typeName }));
                }
            }
            return parts.Count > 0 ? string.Join("; ", parts) + "." : LocalizationManager.GetString("eod.kitchen_none");
        }

        private string BuildPotableWaterSummary()
        {
            var foodRoom = _gameManager?.FoodRoomSystem;
            if (foodRoom == null) return "—";
            var water = foodRoom.WaterSlot;
            if (!water.IsActive)
                return LocalizationManager.GetString("eod.water_none");
            if (water.PotableWaterOutput > 0)
                return LocalizationManager.GetString("eod.water_ready", new Dictionary<string, string> { ["n"] = water.PotableWaterOutput.ToString() });
            return LocalizationManager.GetString("eod.water_progress");
        }

        private static string FormatPotNumber(string potId)
        {
            if (string.IsNullOrEmpty(potId)) return "?";
            if (potId.Length > 4 && potId.StartsWith("POT-", System.StringComparison.OrdinalIgnoreCase))
                return potId.Substring(4);
            return potId;
        }

        private void PopulateDiario()
        {
            int day = _dayCycleSystem != null ? _dayCycleSystem.CurrentDay : 1;
            string full = BuildDiarioNarrative(day);

            var title = _root.Q<Label>("eod-diario-title");
            if (title != null)
                title.text = "DIARIO S.P.O.R.A.E // FRAMMENTO PERSONALE";

            var close = _root.Q<Label>("eod-diario-close");
            if (close != null)
                close.text = "Fine frammento. O forse inizio febbre.";

            if (_diarioText != null)
            {
                _diarioText.text = "";
                StartCoroutine(Typewriter(_diarioText, full));
            }
        }

        private string BuildDiarioNarrative(int day)
        {
            float distortion = ComputeDiaryDistortion(day);
            float phAlignment = ComputeDiaryPhAlignment();
            bool isLateDistortion = distortion >= 0.7f;
            bool isMediumDistortion = distortion >= 0.4f;

            var paragraphs = new List<string>
            {
                BuildDiaryOpening(day, distortion, phAlignment),
                BuildDiaryBody(day, isMediumDistortion, isLateDistortion, phAlignment),
                BuildDiaryLoreFragment(day, distortion, phAlignment),
                BuildDiaryClosing(distortion, phAlignment)
            };

            return string.Join("\n\n", paragraphs.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        private float ComputeDiaryDistortion(int day)
        {
            // Se disponibile, il beat demo (1..8) offre una progressione narrativa più aderente.
            if (_demoSessionState != null && _demoSessionState.IsDemo)
            {
                float beat01 = Mathf.Clamp01((_demoSessionState.CurrentBeat - 1) / 7f);
                return Mathf.Clamp01(0.15f + beat01 * 0.85f);
            }

            // Fallback full game: incremento morbido con i giorni.
            return Mathf.Clamp01((day - 1) / 24f);
        }

        private float ComputeDiaryPhAlignment()
        {
            // -1 = ultra acido/EVIL, +1 = ultra basico/PURE
            if (_phSystem == null)
                return 0f;
            return Mathf.Clamp(_phSystem.CurrentPh / 100f, -1f, 1f);
        }

        private string BuildDiaryOpening(int day, float distortion, float phAlignment)
        {
            int v = Variant(day, 13);
            bool evil = phAlignment <= -0.35f;
            bool pure = phAlignment >= 0.35f;

            if (evil)
            {
                string[] evilLines =
                {
                    "Registro serale. L'aria sa di ferro e giudizio: il Vault oggi ha ringhiato piano.",
                    "Chiudo il turno con i nervi stretti. Le pareti acide hanno imparato i miei dubbi.",
                    "Fine giornata. Il Dome sembra un dente scoperto e io ci metto ancora le mani.",
                    "Scrivo per non mordere. Oggi il buio aveva intenzioni molto precise."
                };
                return evilLines[v % evilLines.Length];
            }

            if (pure)
            {
                string[] pureLines =
                {
                    "Registro serale. Oggi il Vault ha respirato meno storto del solito.",
                    "Fine turno. Per un momento i corridoi hanno smesso di sembrare una minaccia.",
                    "Chiudo la giornata con una calma fragile, ma almeno reale.",
                    "Stasera la luce non consola, pero chiarisce: gia tanto."
                };
                return pureLines[v % pureLines.Length];
            }

            if (distortion < 0.35f)
            {
                string[] lines =
                {
                    "Registro serale. Il Vault finge ordine, io fingo di credergli.",
                    "Ho chiuso la giornata con le mani sporche e la coscienza lucidata male.",
                    "La routine dice che sono vivo. Il resto e un dettaglio amministrativo.",
                    "Fine turno. Le luci verdi mentono con gentilezza professionale."
                };
                return lines[v % lines.Length];
            }

            if (distortion < 0.75f)
            {
                string[] lines =
                {
                    "Registro serale. Oggi il Vault ha parlato sottovoce, ma usava la mia voce.",
                    "Chiudo il turno e mi resta addosso un ronzio che non viene dai macchinari.",
                    "La giornata e finita, dice il sistema. Io non sono ancora convinto.",
                    "I corridoi fanno eco ai miei passi con qualche parola di troppo."
                };
                return lines[v % lines.Length];
            }

            string[] lateLines =
            {
                "Registro serale. Credo di aver lavorato io, ma il Vault firma al posto mio.",
                "Chiudo il turno. I monitor sorridono, e non ho mai insegnato loro i denti.",
                "Fine giornata, almeno sulla carta. Nella testa e ancora alba tossica.",
                "Scrivo per ricordare; ogni riga cancella la precedente con cortesia militare."
            };
            return lateLines[v % lateLines.Length];
        }

        private string BuildDiaryBody(int day, bool mediumDistortion, bool lateDistortion, float phAlignment)
        {
            bool evil = phAlignment <= -0.35f;
            bool pure = phAlignment >= 0.35f;
            bool ultraEvil = phAlignment <= -0.8f;
            bool ultraPure = phAlignment >= 0.8f;

            bool hadDomeWork = (_dayActivityLog?.DomeEntriesThisDay?.Count ?? 0) > 0;
            bool hadWatering = (_dayActivityLog?.PotIdsWateringTurnedOnThisDay?.Count ?? 0) > 0;
            bool hadHarvest = (_dayActivityLog?.HarvestsThisDay?.Count ?? 0) > 0;
            bool hadStageChanges = (_dayActivityLog?.StageChangesThisDay?.Count ?? 0) > 0;
            bool hadLab = (_dayActivityLog?.LabEntriesThisDay?.Count ?? 0) > 0;
            bool hadSeedStorageOps = (_dayActivityLog?.SeedStorageEntriesThisDay?.Count ?? 0) > 0;
            bool pantryOff = _gameManager?.FoodRoomSystem != null && !_gameManager.FoodRoomSystem.PantryIsOn;
            bool seedStorageOff = _gameManager?.SeedStorageSystem != null && !_gameManager.SeedStorageSystem.IsOn;
            bool ateMeal = _gameManager != null && _gameManager.AteMealSincePreviousDawn;
            float hydrationLoss = _gameManager?.HydrationLostTodayPercent ?? 0f;
            int activeMissions = _missionManager?.CurrentMissions.Count ?? 0;
            int actionsLeft = _gameManager?.ActionsLeft ?? 0;
            int actionsMax = _gameManager?.ActionSystem?.MaxActions ?? 0;

            var beats = new List<string>();
            if (hadDomeWork || hadWatering)
            {
                if (evil)
                    beats.Add(Pick(day, 31,
                        "Mi sono occupato delle piante con prudenza chirurgica: oggi ogni foglia sembrava trattenere il fiato prima di colpire.",
                        "Nel Dome ho fatto il mio giro con la sensazione di essere osservato dai vasi, non il contrario.",
                        "Ho rimesso ordine tra i pot, ma era un ordine da trincea, non da serra."));
                else if (pure)
                    beats.Add(Pick(day, 31,
                        "Mi sono occupato delle piante con pazienza: oggi hanno risposto senza aggredire il silenzio.",
                        "Nel Dome ho fatto il mio giro da custode stanco, ma senza guerra aperta.",
                        "Ho rimesso ordine tra i pot e, per qualche minuto, ha funzionato davvero."));
                else
                    beats.Add(Pick(day, 31,
                        "Mi sono occupato delle piante come si parla a un testimone: con calma e senza fare domande sbagliate.",
                        "Nel Dome ho fatto il mio giro da custode stanco: mani operative, testa altrove.",
                        "Ho rimesso ordine tra i vasi. O almeno ho negoziato una tregua con loro."));
            }

            if (hadHarvest || hadStageChanges)
            {
                if (evil)
                    beats.Add(Pick(day, 37,
                        "Qualcosa e cresciuto, ma con un appetito che non mi piace nominare.",
                        "Ho visto la vita cambiare forma come una minaccia educata.",
                        "Gli stadi avanzano anche quando il buon senso arretra."));
                else if (pure)
                    beats.Add(Pick(day, 37,
                        "Qualcosa e cresciuto davvero, quasi con grazia.",
                        "Ho visto la vita cambiare forma senza pretendere sangue in anticipo.",
                        "Gli stadi avanzano e, per oggi, la biologia sembra ancora una promessa."));
                else
                    beats.Add(Pick(day, 37,
                        "Qualcosa e cresciuto davvero. In questo posto e quasi una forma di ironia cosmica.",
                        "Ho visto vita cambiare forma in silenzio: nessun applauso, solo lavoro in piu domani.",
                        "Alcuni stadi sono avanzati. La biologia non fa promesse, ma oggi ha strizzato l'occhio."));
            }

            if (hadLab)
                beats.Add(Pick(day, 41,
                    "In laboratorio ho smontato il caos in pezzi piu piccoli e li ho chiamati metodo.",
                    "Ho passato ore tra reagenti e congetture: il profumo dell'apocalisse filtrata.",
                    "Il banco del Lab oggi sembrava un altare tecnico. Nessun santo in vista."));

            if (hadSeedStorageOps)
                beats.Add(Pick(day, 43,
                    "Ho messo mano alle scorte come chi conta munizioni durante una tregua fragile.",
                    "Tra depositi e prelievi ho fatto archeologia preventiva sul domani.",
                    "Le riserve hanno cambiato posto, come i pensieri quando arriva la notte."));

            if (pantryOff || seedStorageOff)
                beats.Add(Pick(day, 47,
                    "Ho lasciato qualche sistema in bilico. In questo mondo, il bilico e solo un altro nome per il rischio.",
                    "Una parte del Vault oggi ha lavorato a luci spente: scelta tecnica, conseguenze poetiche.",
                    "Quando una stanza tace troppo, di solito sta preparando il conto."));

            if (!ateMeal)
                beats.Add(Pick(day, 53,
                    "Ho rimandato il pasto. Il corpo protesta in dialetto, la disciplina traduce male.",
                    "La fame e tornata puntuale, piu affidabile di certe procedure operative.",
                    "Stasera lo stomaco scrive note a margine che non voglio rileggere."));

            if (hydrationLoss >= 20f)
                beats.Add(Pick(day, 59,
                    "Ho lasciato troppa acqua per strada, e ogni passo nel corridoio lo ha fatto notare.",
                    "Mi sento asciutto come i filtri vecchi: funziono, ma graffio.",
                    "La sete oggi ha avuto l'ultima parola. Io ho firmato in fondo."));

            if (activeMissions > 0)
                beats.Add(Pick(day, 61,
                    "Le missioni attive restano appese come neon che non vogliono spegnersi.",
                    "Ci sono ancora consegne in sospeso: piccole guerre burocratiche contro la fine del mondo.",
                    "L'elenco delle cose da fare cresce piu in fretta del mio ottimismo."));

            if (actionsMax > 0)
            {
                if (actionsLeft <= Mathf.Max(1, actionsMax / 3))
                    beats.Add(Pick(day, 63,
                        "A fine turno avevo poche mosse rimaste, ma abbastanza per fingere controllo davanti ai monitor.",
                        "Le energie oggi si sono consumate presto: il Vault prende sempre la sua quota in anticipo.",
                        "Sono arrivato a sera corto di margine, lungo di conseguenze."));
                else
                    beats.Add(Pick(day, 65,
                        "Ho chiuso con un filo di margine operativo: un lusso temporaneo che non intendo sprecare.",
                        "Stavolta sono arrivato a sera senza svuotarmi del tutto. Piccola vittoria, nessuna fanfara.",
                        "Mi resta abbastanza respiro per domani, che qui equivale a una benedizione burocratica."));
            }

            if (beats.Count == 0)
            {
                beats.Add(Pick(day, 67,
                    "Giornata stranamente quieta. La quiete, qui sotto, ha sempre secondi fini.",
                    "Poche azioni visibili, molte crepe invisibili. Routine standard dello Sporium.",
                    "Oggi il silenzio ha fatto quasi tutto il lavoro e si e preso il merito."));
            }

            if (ultraEvil)
                beats.Add(Pick(day, 69,
                    "Il pH oggi pende verso l'ultra acido: i pensieri arrivano a denti stretti e non chiedono permesso.",
                    "La deriva acida spinge tutto verso il nero: anche le battute finiscono in minaccia.",
                    "Se continuo a scrivere in questo tono e perche il Vault lo preferisce violento."));
            else if (ultraPure)
                beats.Add(Pick(day, 69,
                    "Il pH oggi si avvicina all'ultra basico: non e pace, ma e una tregua leggibile.",
                    "Con la deriva verso il puro riesco ancora a distinguere i fatti dalle ombre.",
                    "Quando il Dome respira piu basico, la mente smette almeno di mordersi da sola."));

            if (mediumDistortion)
            {
                if (evil)
                    beats.Add(Pick(day, 71,
                        "Continuo a chiamarle note operative, ma oggi suonano come verbali di interrogatorio.",
                        "Ci sono dettagli che ricordo due volte e dettagli che provano a ricordarmi loro.",
                        "Le verita qui non hanno bordi: hanno schegge."));
                else if (pure)
                    beats.Add(Pick(day, 71,
                        "Continuo a chiamarle note operative, ma almeno oggi non urlano.",
                        "Ci sono dettagli confusi, ma riesco ancora a tenerli in fila.",
                        "Le verita qui restano scomode, non necessariamente ostili."));
                else
                    beats.Add(Pick(day, 71,
                        "Continuo a chiamarle note operative, ma sembrano confessioni con il camice addosso.",
                        "Ci sono dettagli che ricordo due volte e dettagli che non ricordano me.",
                        "Le verita qui hanno bordi morbidi: tagliano comunque."));
            }

            if (lateDistortion)
            {
                if (evil)
                    beats.Add(Pick(day, 73,
                        "A volte penso che il Biologo sia solo un alias utile al Vault quando vuole farsi male da solo.",
                        "Non giuro che sia andata cosi: giuro solo che la versione peggiore suona piu vera.",
                        "Se queste righe mentono, e per insegnarmi come si sopravvive al prossimo morso."));
                else if (pure)
                    beats.Add(Pick(day, 73,
                        "A volte penso che il Biologo sia un ruolo. Oggi, almeno, il ruolo non mi ha divorato.",
                        "Non giuro che sia andata cosi, ma questa versione non mi odia apertamente.",
                        "Se queste righe mentono, lo fanno per lasciarmi un appiglio fino a domani."));
                else
                    beats.Add(Pick(day, 73,
                        "A volte penso che il Biologo sia un ruolo, non una persona. E oggi il ruolo ha recitato bene.",
                        "Non giuro che sia andata davvero cosi. Giuro solo che stanotte ci credo.",
                        "Se queste righe mentono, lo fanno per proteggere qualcuno. Spero non me."));
            }

            return string.Join(" ", beats);
        }

        private string BuildDiaryLoreFragment(int day, float distortion, float phAlignment)
        {
            bool evil = phAlignment <= -0.35f;
            bool pure = phAlignment >= 0.35f;
            bool late = distortion >= 0.7f;

            if (evil)
            {
                var lines = new List<string>
                {
                    Pick(day, 79,
                        "Nei registri pre-caduta chiamavano questa zona Cintura Verde 9. Oggi di verde e rimasto solo il led delle procedure d'emergenza.",
                        "Dicono che prima della Pioggia Nera le serre fossero trasparenti. Io conosco solo vetri opachi e filtri saturi.",
                        "Prima del collasso, qui sopra passavano treni alimentari. Adesso passano solo storie che cambiano ogni volta."),
                    Pick(day, 83,
                        "Il Protocollo Helix prometteva raccolti eterni e citta autosufficienti. Poi hanno acceso i reattori sbagliati e spento le coscienze giuste.",
                        "Nel dossier ORPHEUS si legge che il pH del mondo e impazzito in 43 giorni. Nessuno scrive cosa e successo al giorno 44.",
                        "Le cronache ufficiali parlano di evento climatico. Le cronache non ufficiali parlano di fame organizzata."),
                    Pick(day, 89,
                        "Quando l'acido sale, i ricordi diventano armi improprie: utili, imprecise, pericolose.",
                        "Ogni frammento di storia ha due versioni: quella archiviata e quella che non ti lascia dormire.",
                        "Mi ripeto che queste sono note operative. Ma ogni riga sembra una deposizione senza tribunale.")
                };

                if (late)
                    lines.Add(Pick(day, 97,
                        "Se domani rileggo questo pezzo e non mi riconosco, vuol dire che il Vault ha gia corretto la mia autobiografia.",
                        "Ho il sospetto che la memoria qui venga sterilizzata come un banco da laboratorio: finche non resta nulla di umano.",
                        "Le menzogne utili sopravvivono meglio della verita. Non e un'aforisma: e una policy."));

                return string.Join(" ", lines);
            }

            if (pure)
            {
                var lines = new List<string>
                {
                    Pick(day, 79,
                        "Nei vecchi archivi la chiamavano Fascia Aurora: colture in quota, acqua pulita, turni di ricerca aperti ai civili.",
                        "Prima del collasso, la rete agro-biologica era una promessa concreta: meno frontiere, piu serre condivise.",
                        "La storia ufficiale dice che c'era un piano per salvare tutti. La parte non ufficiale dice che il piano costava troppo."),
                    Pick(day, 83,
                        "Il Programma S.P.O.R.A.E nasceva per curare il suolo, non per amministrare rovine. Da qualche parte quella versione deve ancora esistere.",
                        "Nei manuali antichi il Biologo era chiamato Custode di Cicli, non Operatore di Emergenza. Mi piace ricordarlo.",
                        "Le prime cupole erano scuole e laboratori insieme. Oggi sono fortezze con badge e silenzi."),
                    Pick(day, 89,
                        "Quando il pH sale verso il basico, la mente trova un corridoio piu largo tra paura e lucidita.",
                        "In queste notti meno tossiche riesco ancora a credere che ricostruire non sia solo propaganda.",
                        "Ci sono giorni in cui la memoria non graffia: insegna.")
                };

                if (late)
                    lines.Add(Pick(day, 97,
                        "Forse e questa la vera menzogna gentile del Vault: lasciarti vedere un futuro quel tanto che basta per tornare al turno.",
                        "Anche quando tutto sembra piu chiaro, il dubbio resta: sto ricordando il mondo com'era o come avrei voluto fosse?",
                        "Se domani torno nel fango, almeno stanotte so ancora nominare la speranza senza vergognarmi."));

                return string.Join(" ", lines);
            }

            var neutralLines = new List<string>
            {
                Pick(day, 79,
                    "Le mappe pre-caduta raccontano un pianeta ordinato. Le mappe post-caduta raccontano solo dove non morire subito.",
                    "Prima, il mondo produceva eccedenza. Poi ha prodotto confini, razionamenti e memoriali.",
                    "I vecchi atlanti hanno colori allegri. I nuovi hanno note a margine e aree proibite."),
                Pick(day, 83,
                    "Di quello che e successo davvero esistono versioni ufficiali, versioni utili e versioni sopravvissute.",
                    "Ogni generazione ha ricevuto una storia diversa sul collasso. Nessuna include i nomi dei responsabili.",
                    "S.P.O.R.A.E doveva essere un ponte tra scienza e comunità. Noi ne usiamo i resti come stampella."),
                Pick(day, 89,
                    "A meta deriva, la verita resta una bestia nervosa: si lascia avvicinare solo da chi non la idealizza.",
                    "Non so se queste note siano confessione o propaganda interna. So che mi tengono in asse.",
                    "Tra verita e menzogna, scelgo ogni notte la versione che mi fa arrivare a domani.")
            };

            if (late)
                neutralLines.Add(Pick(day, 97,
                    "Quando il rumore aumenta, il passato sembra un corridoio senza uscite d'emergenza.",
                    "Forse la storia non e persa: e solo frammentata nei posti sbagliati.",
                    "Se qualcuno trovera questo diario, sapra almeno che abbiamo provato a restare umani."));

            return string.Join(" ", neutralLines);
        }

        private string BuildDiaryClosing(float distortion, float phAlignment)
        {
            if (phAlignment <= -0.35f)
            {
                if (distortion < 0.35f)
                    return "Promemoria per domani: tenere le mani ferme, anche quando la testa vuole rompere tutto.";
                if (distortion < 0.75f)
                    return "Promemoria per domani: non confondere paranoia e istinto. Qui spesso vestono uguale.";
                return "Promemoria per domani: se la notte mi parla col coltello, rispondo con sarcasmo e guanti spessi.";
            }

            if (phAlignment >= 0.35f)
            {
                if (distortion < 0.35f)
                    return "Promemoria per domani: conservare questa chiarezza prima che il Vault la metabolizzi.";
                if (distortion < 0.75f)
                    return "Promemoria per domani: seguire i segnali buoni senza innamorarsi delle illusioni pulite.";
                return "Promemoria per domani: difendere la parte lucida, anche se trema.";
            }

            if (distortion < 0.35f)
                return "Promemoria per domani: sopravvivere con stile mediocre e sarcasmo sufficiente.";
            if (distortion < 0.75f)
                return "Promemoria per domani: distinguere i fatti dai racconti, poi scegliere i racconti piu utili.";
            return "Promemoria per domani: se trovo la verita, la metto in quarantena prima che contagi il resto.";
        }

        private static int Variant(int day, int salt)
        {
            int raw = day * 97 + salt * 31;
            return raw < 0 ? -raw : raw;
        }

        private static string Pick(int day, int salt, params string[] options)
        {
            if (options == null || options.Length == 0)
                return string.Empty;
            int idx = Variant(day, salt) % options.Length;
            return options[idx];
        }

        private void PopulateForecast()
        {
            float predictedPhDrift = _dayCycleController != null ? _dayCycleController.GetPredictedPhDriftForNextDay() : float.NaN;
            string predictedPhDriftStr = float.IsNaN(predictedPhDrift)
                ? "—"
                : predictedPhDrift.ToString("+#0.0;-#0.0;0", System.Globalization.CultureInfo.InvariantCulture);
            float currentPh = _phSystem != null ? _phSystem.CurrentPh : float.NaN;
            float tomorrowPh = float.IsNaN(currentPh) || float.IsNaN(predictedPhDrift) ? float.NaN : currentPh + predictedPhDrift;

            int maxActionsForecast = _gameManager?.ActionSystem?.MaxActions ?? 5;
            var sbTomorrow = new StringBuilder();
            sbTomorrow.AppendLine("<b><color=#7FD9FF>COSA SUCCEDE DOMANI</color></b>");
            sbTomorrow.AppendLine($"• Azioni disponibili: <b><color={ColorInfo}>{maxActionsForecast}</color></b>");
            sbTomorrow.AppendLine($"• Deriva pH prevista: <color={ColorWarn}><b>{predictedPhDriftStr}</b></color>");
            if (!float.IsNaN(tomorrowPh))
                sbTomorrow.AppendLine($"• pH atteso a fine giornata: <color={ColorInfo}><b>{tomorrowPh:F1}</b></color>");

            var conditions = _dayCycleController != null
                ? _dayCycleController.GetActiveConditionsForReport()
                : new List<(string PotId, int MoldRiskLevel, bool IsInfested)>();

            var potForecast = BuildTomorrowPotForecastLines(conditions, out var actionPlan);
            sbTomorrow.AppendLine();
            sbTomorrow.AppendLine("<b><color=#7FD9FF>POT PRIORITARI</color></b>");
            if (potForecast.Count == 0)
            {
                sbTomorrow.AppendLine($"• <color={ColorMuted}>Nessun rischio alto rilevato. Mantieni parametri stabili e controlla almeno un ciclo completo nel Dome.</color>");
            }
            else
            {
                foreach (var line in potForecast)
                    sbTomorrow.AppendLine($"• {line}");
            }

            sbTomorrow.AppendLine();
            sbTomorrow.AppendLine("<b><color=#7FD9FF>MISSIONI E OBIETTIVI</color></b>");
            if (_missionManager != null && _missionManager.CurrentMissions.Count > 0)
            {
                foreach (var mission in _missionManager.CurrentMissions.Take(3))
                {
                    string title = mission?.Config != null && !string.IsNullOrWhiteSpace(mission.Config.Title)
                        ? mission.Config.Title
                        : "Missione senza titolo";
                    sbTomorrow.AppendLine($"• <color={ColorInfo}>{title}</color>");
                }
                sbTomorrow.AppendLine($"• <color={ColorMuted}>Nota: scadenze missione non ancora tracciate dal sistema missioni corrente.</color>");
            }
            else
            {
                sbTomorrow.AppendLine($"• <color={ColorMuted}>Nessuna missione attiva da monitorare.</color>");
            }

            var sbActions = new StringBuilder();
            sbActions.AppendLine("<b><color=#7FD9FF>PIANO OPERATIVO CONSIGLIATO</color></b>");
            if (actionPlan.Count == 0)
            {
                sbActions.AppendLine($"• <color={ColorInfo}>Apri il turno con un controllo Dome generale e verifica pH dopo le prime interazioni.</color>");
            }
            else
            {
                int step = 1;
                foreach (var action in actionPlan.Take(5))
                    sbActions.AppendLine($"• {step++}) {action}");
            }

            string textResearch = _nightResearchChosen
                ? "Ricerca notturna: completata (frammento lore aggiunto ai registri)."
                : "Ricerca notturna: nessuna selezione (nessun bonus informativo per domani).";

            StartCoroutine(RunForecastTypewriter(sbTomorrow.ToString(), sbActions.ToString(), textResearch));
        }

        private List<string> BuildTomorrowPotForecastLines(
            IReadOnlyList<(string PotId, int MoldRiskLevel, bool IsInfested)> conditions,
            out List<string> actionPlan)
        {
            actionPlan = new List<string>();
            var forecast = new List<(int Score, string Line, string Action)>();

            Dictionary<string, (int MoldRiskLevel, bool IsInfested)> byPot = new();
            if (conditions != null)
            {
                foreach (var c in conditions)
                    byPot[c.PotId ?? string.Empty] = (c.MoldRiskLevel, c.IsInfested);
            }

            if (_potRegistry == null)
                return new List<string>();

            var pots = _potRegistry.GetPotsSnapshot();
            foreach (var pot in pots)
            {
                var state = pot?.PotActions?.PotState;
                if (state == null || !state.HasPlant || state.Stage == (int)PlantStage.Empty)
                    continue;

                string potLabel = $"POT-{FormatPotNumber(state.PotId)}";
                int score = 0;
                var fragments = new List<string>();
                string primaryAction = null;

                if (byPot.TryGetValue(state.PotId ?? string.Empty, out var cond))
                {
                    if (cond.IsInfested)
                    {
                        score += 100;
                        fragments.Add($"<color={ColorBad}>infestazione attiva</color>");
                        primaryAction ??= $"Priorita alta: tratta {potLabel} per contenere infestazione.";
                    }
                    else if (cond.MoldRiskLevel >= 2)
                    {
                        score += 70;
                        fragments.Add($"<color={ColorWarn}>rischio muffa alto</color>");
                        primaryAction ??= $"Riduci rischio muffa su {potLabel} (potatura/parametri entro range).";
                    }
                }

                int stressPct = Mathf.Clamp(Mathf.RoundToInt(state.GetConsecutiveLedDays() / 5f * 100f), 0, 100);
                if (stressPct >= 80)
                {
                    score += 60;
                    fragments.Add($"<color={ColorWarn}>light stress {stressPct}%</color>");
                    primaryAction ??= $"Spegni o alterna LED su {potLabel} per evitare light burn.";
                }

                if (state.ConditionScore <= 25)
                {
                    score += 65;
                    fragments.Add($"<color={ColorBad}>condizione critica ({state.ConditionScore})</color>");
                    primaryAction ??= $"Recupera {potLabel}: correggi acqua/luce/fertilizzante subito a inizio turno.";
                }
                else if (state.ConditionScore <= 40)
                {
                    score += 35;
                    fragments.Add($"<color={ColorWarn}>condizione in calo ({state.ConditionScore})</color>");
                    primaryAction ??= $"Stabilizza {potLabel} prima delle attività secondarie.";
                }

                var plantData = PlantDatabase.Instance?.GetPlantDataByCode(state.PlantCode);
                if (plantData != null)
                {
                    var stageReq = plantData.GetStageRequirements((PlantStage)state.Stage);
                    if (stageReq != null)
                    {
                        int daysToStage = Mathf.Max(0, stageReq.durationDays - state.DaysInCurrentStage);
                        if (daysToStage <= 1)
                        {
                            score += 40;
                            fragments.Add($"<color={ColorGood}>vicino al cambio stadio</color>");
                            primaryAction ??= $"Mantieni {potLabel} nel range ottimale: possibile transizione stadio domani.";
                        }
                    }
                }

                if (score <= 0)
                    continue;

                string line = $"{potLabel}: {string.Join(", ", fragments)}.";
                forecast.Add((score, line, primaryAction));
            }

            var top = forecast
                .OrderByDescending(f => f.Score)
                .Take(5)
                .ToList();

            var actionSeen = new HashSet<string>();
            foreach (var item in top)
            {
                if (!string.IsNullOrWhiteSpace(item.Action) && actionSeen.Add(item.Action))
                    actionPlan.Add(item.Action);
            }

            return top.Select(f => f.Line).ToList();
        }

        private IEnumerator Typewriter(Label label, string fullText)
        {
            if (label == null || string.IsNullOrEmpty(fullText))
                yield break;
            label.text = "";
            float delay = 1f / Mathf.Max(1f, _typewriterCharsPerSecond);
            for (int i = 0; i < fullText.Length; i++)
            {
                label.text = fullText.Substring(0, i + 1);
                yield return new WaitForSeconds(delay);
            }
        }

        private IEnumerator RunForecastTypewriter(string textToday, string textTomorrow, string textResearch)
        {
            if (_forecastToday != null)
            {
                _forecastToday.text = "";
                yield return Typewriter(_forecastToday, textToday);
            }
            if (_forecastTomorrow != null)
            {
                _forecastTomorrow.text = "";
                yield return Typewriter(_forecastTomorrow, textTomorrow);
            }
            if (_forecastResearch != null && !string.IsNullOrEmpty(textResearch))
            {
                _forecastResearch.text = "";
                yield return Typewriter(_forecastResearch, textResearch);
            }
        }

        private void PopulateDawn(int newDay)
        {
            var title = _root.Q<Label>("eod-dawn-title");
            if (title != null) title.text = "BRIEF OPERATIVO ALBA";
            var sub = _root.Q<Label>("eod-dawn-subtitle");
            if (sub != null) sub.text = $"GIORNO {newDay} — COSA FARE SUBITO";

            int dailyCost = 20;
            var endDayBtn = FindObjectOfType<EndDayButton>();
            if (endDayBtn != null) dailyCost = endDayBtn.GetDailyPowerCost();

            float ph = _phSystem != null ? _phSystem.CurrentPh : float.NaN;
            float phDrift = _dayCycleController != null ? _dayCycleController.GetPredictedPhDriftForNextDay() : float.NaN;
            float condensation = _gameManager?.CondensationSystem != null ? _gameManager.CondensationSystem.CurrentAccumulation : float.NaN;
            int cry = _gameManager != null ? _gameManager.CurrentCRY : 0;
            int cryForecast = cry - dailyCost;

            var topBar = FindObjectOfType<TopBarController>();
            var mutSvc = ServiceContainer.Instance?.Get<DomeMutationRuntimeService>(suppressWarning: true);
            float mutation = float.NaN;
            if (mutSvc != null && mutSvc.HasAuthoritativeSnapshot)
                mutation = mutSvc.DisplayNormalized;
            else if (topBar != null)
                mutation = topBar.GetMutationIndex();
            int grate = topBar != null ? topBar.GetGrateValue() : 0;

            string phDriftStr = float.IsNaN(phDrift) ? "—" : phDrift.ToString("+#0.00;-#0.00;0", System.Globalization.CultureInfo.InvariantCulture);
            string phTrend = float.IsNaN(phDrift)
                ? ""
                : (phDrift < 0 ? LocalizationManager.GetString("eod.dawn_trend_acid") : LocalizationManager.GetString("eod.dawn_trend_alk"));

            var conditions = _dayCycleController != null
                ? _dayCycleController.GetActiveConditionsForReport()
                : new List<(string PotId, int MoldRiskLevel, bool IsInfested)>();
            BuildDawnActionBrief(
                conditions,
                out var topRisks,
                out var topOpportunities,
                out var actionPlan);

            string globalRisk = ComputeGlobalDawnRiskLabel(topRisks.Count);
            string mutationText = float.IsNaN(mutation) ? "—" : mutation.ToString("P0");
            string condText = float.IsNaN(condensation) ? "—" : condensation.ToString("F0");

            SetDawnRow(
                "eod-dawn-text-mutation",
                $"STATO NOTTE: pH {(float.IsNaN(ph) ? "—" : ph.ToString("F1"))} ({(_phSystem != null ? _phSystem.GetBandName() : "N/D")}), deriva {phDriftStr}{phTrend}, mutazione {mutationText}, condensa {condText}, rischio globale {globalRisk}.");

            if (topRisks.Count == 0)
            {
                SetDawnRow("eod-dawn-text-ph", "PRIORITA IMMEDIATE: nessun POT in criticita alta. Mantieni monitoraggio Dome e stabilita pH.");
            }
            else
            {
                SetDawnRow("eod-dawn-text-ph", $"PRIORITA IMMEDIATE: {string.Join(" | ", topRisks.Take(3))}");
            }

            if (topOpportunities.Count == 0)
            {
                SetDawnRow("eod-dawn-text-condensation", "FINESTRA OPPORTUNITA: nessun avanzamento evidente. Punta prima su stabilizzazione e prevenzione.");
            }
            else
            {
                SetDawnRow("eod-dawn-text-condensation", $"FINESTRA OPPORTUNITA: {string.Join(" | ", topOpportunities.Take(2))}");
            }

            if (actionPlan.Count == 0)
            {
                SetDawnRow("eod-dawn-text-grate", "PIANO AVVIO TURNO: 1) check Dome completo, 2) correggi pH/LED dove serve, 3) poi missioni.");
            }
            else
            {
                SetDawnRow("eod-dawn-text-grate", $"PIANO AVVIO TURNO: {string.Join(" ", actionPlan.Take(3).Select((x, i) => $"{i + 1}) {x}"))}");
            }

            SetDawnRow("eod-dawn-text-cry", $"ECONOMIA: baseline CRY stimata {cryForecast} (costi fissi). G-rate +{grate}.");

            var press = _root.Q<Label>("eod-dawn-press-key");
            if (press != null)
                press.text = "Premi Continua per iniziare il turno";

            string phDesc = float.IsNaN(phDrift)
                ? LocalizationManager.GetString("eod.tt_ph_desc_neutral")
                : LocalizationManager.GetString("eod.tt_ph_desc_shift", new Dictionary<string, string>
                {
                    ["dir"] = phDrift < 0
                        ? LocalizationManager.GetString("eod.tt_ph_dir_acid")
                        : LocalizationManager.GetString("eod.tt_ph_dir_alk")
                });
            var tooltips = new Dictionary<string, (string title, string desc, string tip)>
            {
                ["mutation"] = (
                    "[STATO NOTTE]",
                    "Riepilogo sintetico delle variabili che influenzano il prossimo turno (pH, mutazione, condensazione, rischio globale).",
                    "Usalo come orientamento iniziale: le decisioni vere sono nelle priorita POT."),
                ["ph"] = (
                    "[PRIORITA IMMEDIATE]",
                    "Elenco dei POT con rischio piu alto da trattare prima di tutto il resto.",
                    "Se ignori questa riga, aumentano probabilita di peggioramento o blocco crescita."),
                ["condensation"] = (
                    "[FINESTRA OPPORTUNITA]",
                    "POT o condizioni favorevoli che possono convertire il turno in progresso.",
                    "Dopo aver messo in sicurezza i rischi, sfrutta queste opportunita."),
                ["grate"] = (
                    "[PIANO AVVIO TURNO]",
                    "Sequenza consigliata delle prime azioni per evitare errori di priorita.",
                    "E una guida operativa: adattala alle missioni in corso."),
                ["cry"] = (
                    "[ECONOMIA BASELINE]",
                    "Stima CRY legata ai costi fissi. Non include tutte le spese variabili del turno.",
                    "Evita di aprire il giorno in deficit quando hai sistemi ad alto consumo.")
            };

            _dawnTooltipData = tooltips;
            RegisterDawnTooltipsOnce();
        }

        private string ComputeGlobalDawnRiskLabel(int criticalPots)
        {
            if (criticalPots >= 3) return "ALTO";
            if (criticalPots >= 1) return "MEDIO";
            return "BASSO";
        }

        private void BuildDawnActionBrief(
            IReadOnlyList<(string PotId, int MoldRiskLevel, bool IsInfested)> conditions,
            out List<string> topRisks,
            out List<string> topOpportunities,
            out List<string> actionPlan)
        {
            topRisks = new List<string>();
            topOpportunities = new List<string>();
            actionPlan = new List<string>();

            if (_potRegistry == null)
                return;

            Dictionary<string, (int MoldRiskLevel, bool IsInfested)> byPot = new();
            if (conditions != null)
            {
                foreach (var c in conditions)
                    byPot[c.PotId ?? string.Empty] = (c.MoldRiskLevel, c.IsInfested);
            }

            var riskRows = new List<(int Score, string Line, string Action)>();
            var opportunityRows = new List<(int Score, string Line, string Action)>();
            var pots = _potRegistry.GetPotsSnapshot();

            foreach (var pot in pots)
            {
                var state = pot?.PotActions?.PotState;
                if (state == null || !state.HasPlant || state.Stage == (int)PlantStage.Empty)
                    continue;

                string potLabel = $"POT-{FormatPotNumber(state.PotId)}";
                int riskScore = 0;
                var riskTags = new List<string>();
                string riskAction = null;

                if (byPot.TryGetValue(state.PotId ?? string.Empty, out var cond))
                {
                    if (cond.IsInfested)
                    {
                        riskScore += 100;
                        riskTags.Add("infestazione");
                        riskAction ??= $"Tratta subito {potLabel} (infestazione).";
                    }
                    else if (cond.MoldRiskLevel >= 2)
                    {
                        riskScore += 70;
                        riskTags.Add("muffa alta");
                        riskAction ??= $"Riduci muffa su {potLabel} con intervento mirato.";
                    }
                }

                int stressPct = Mathf.Clamp(Mathf.RoundToInt(state.GetConsecutiveLedDays() / 5f * 100f), 0, 100);
                if (stressPct >= 80)
                {
                    riskScore += 60;
                    riskTags.Add($"stress LED {stressPct}%");
                    riskAction ??= $"Ribilancia LED su {potLabel} per evitare danni.";
                }

                if (state.ConditionScore <= 25)
                {
                    riskScore += 65;
                    riskTags.Add($"condizione critica {state.ConditionScore}");
                    riskAction ??= $"Recupera parametri vitali su {potLabel}.";
                }
                else if (state.ConditionScore <= 40)
                {
                    riskScore += 35;
                    riskTags.Add($"condizione bassa {state.ConditionScore}");
                }

                if (riskScore > 0)
                    riskRows.Add((riskScore, $"{potLabel}: {string.Join(", ", riskTags)}", riskAction));

                var plantData = PlantDatabase.Instance?.GetPlantDataByCode(state.PlantCode);
                if (plantData == null)
                    continue;

                var req = plantData.GetStageRequirements((PlantStage)state.Stage);
                if (req == null)
                    continue;

                int daysToStage = Mathf.Max(0, req.durationDays - state.DaysInCurrentStage);
                if (daysToStage <= 1 && state.ConditionScore >= 40)
                {
                    int oppScore = 50 - daysToStage * 10 + state.ConditionScore / 4;
                    string line = $"{potLabel}: vicino al cambio stadio (≈{daysToStage} giorno/i)";
                    string action = $"Mantieni {potLabel} in range: possibile avanzamento domani.";
                    opportunityRows.Add((oppScore, line, action));
                }
            }

            foreach (var row in riskRows.OrderByDescending(r => r.Score).Take(3))
            {
                topRisks.Add(row.Line);
                if (!string.IsNullOrWhiteSpace(row.Action) && !actionPlan.Contains(row.Action))
                    actionPlan.Add(row.Action);
            }

            foreach (var row in opportunityRows.OrderByDescending(o => o.Score).Take(2))
            {
                topOpportunities.Add(row.Line);
                if (!string.IsNullOrWhiteSpace(row.Action) && !actionPlan.Contains(row.Action))
                    actionPlan.Add(row.Action);
            }
        }

        private void SetDawnRow(string labelName, string text)
        {
            var label = _root.Q<Label>(labelName);
            if (label != null) label.text = text;
        }

        private void RegisterDawnTooltipsOnce()
        {
            if (_dawnTooltip == null || _dawnTooltipsRegistered) return;
            _dawnTooltipsRegistered = true;
            string[] paramIds = { "mutation", "ph", "condensation", "grate", "cry" };
            foreach (var paramId in paramIds)
            {
                var row = _root.Q<VisualElement>($"eod-dawn-row-{paramId}");
                if (row == null) continue;
                var paramIdCopy = paramId;
                row.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    if (_dawnTooltipData == null || !_dawnTooltipData.TryGetValue(paramIdCopy, out var t)) return;
                    if (_dawnTooltipTitle != null) _dawnTooltipTitle.text = t.title;
                    if (_dawnTooltipDesc != null) _dawnTooltipDesc.text = t.desc;
                    if (_dawnTooltipTip != null)
                    {
                        var tip = t.tip;
                        _dawnTooltipTip.text = LocalizationManager.GetString("eod.tip_prefix") + tip;
                    }
                    _dawnTooltip.style.display = DisplayStyle.Flex;
                    _dawnTooltip.BringToFront();
                    PositionDawnTooltipAtMouse(evt.mousePosition, row);
                });
                row.RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    _dawnTooltip.style.display = DisplayStyle.None;
                });
                row.RegisterCallback<MouseMoveEvent>(evt =>
                {
                    PositionDawnTooltipAtMouse(evt.mousePosition, row);
                });
            }
        }

        /// <summary>Posiziona il tooltip Dawn vicino al mouse. Coordinate panel → spazio locale del parent del tooltip (come Lab inventory tooltip).</summary>
        private void PositionDawnTooltipAtMouse(Vector2 mousePosPanel, VisualElement sourceRow)
        {
            if (_dawnTooltip == null || _dawnTooltip.parent == null) return;
            var parent = _dawnTooltip.parent;
            var local = parent.WorldToLocal(mousePosPanel);
            float x = local.x + 16f;
            float y = local.y + 12f;
            const float tw = 320f;
            float th = _dawnTooltip.resolvedStyle.height;
            if (th <= 0f) th = 120f;
            var bounds = parent.contentRect;
            if (x + tw > bounds.width) x = local.x - tw - 8f;
            if (y + th > bounds.height) y = local.y - th - 8f;
            if (y < 0f) y = 8f;
            if (x < 0f) x = 8f;
            _dawnTooltip.style.left = x;
            _dawnTooltip.style.top = y;
        }

        private void OnYesClicked()
        {
            if (_saveManager != null)
            {
                bool saveSuccess = _saveManager.SaveGame("default");
#if UNITY_EDITOR
                if (saveSuccess)
                    SporiumLogger.LogInfo(LogCategory.Save, "Salvataggio automatico eseguito con successo");
#endif
                if (saveSuccess)
                {
                    var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                    if (foundation != null && foundation.Enabled)
                        foundation.PostToast("SYS-003", new NotificationPayload());
                }
            }
            ShowStep(2);
        }

        private void OnNoClicked() => Hide();

        private void OnSnapshotConfirmClicked() => ShowStep(3);

        private void OnDiarioContinueClicked()
        {
            int actionsLeft = _gameManager?.ActionsLeft ?? 0;
            if (actionsLeft >= 1)
            {
                PrepareResearchStep(allowResearch: true);
                ShowStep(4);
            }
            else
            {
                PrepareResearchStep(allowResearch: false);
                ShowStep(4);
            }
        }

        private void OnResearchHistoricalClicked() => OnResearchChosen("Historical");
        private void OnResearchBotanicalClicked() => OnResearchChosen("Botanical");
        private void OnResearchVaultClicked() => OnResearchChosen("Vault");
        private void OnResearchSkipClicked() => OnResearchChosen(null);

        private void OnResearchChosen(string branch)
        {
            if (_researchLockedByNoActions)
            {
                StartCoroutine(TransitionToForecast());
                return;
            }

            if (!string.IsNullOrEmpty(branch))
            {
                _nightResearchChosen = true;
                _wikiUnlockService?.UnlockCategory(branch);
                if (_dayCycleSystem != null)
                    _wikiUnlockService?.RecordNightResearch(_dayCycleSystem.CurrentDay, branch);
            }
            StartCoroutine(TransitionToForecast());
        }

        private IEnumerator TransitionToForecast()
        {
            yield return new WaitForSeconds(_nightResearchTransitionDelay);
            ShowStep(5);
        }

        private void PrepareResearchStep(bool allowResearch)
        {
            _researchLockedByNoActions = !allowResearch;

            var title = _root.Q<Label>("eod-research-title");
            var subtitle = _root.Q<Label>("eod-research-subtitle");

            if (allowResearch)
            {
                if (title != null)
                    title.text = "SELEZIONE RICERCA NOTTURNA";
                if (subtitle != null)
                    subtitle.text = "Scegli un ramo di ricerca da approfondire durante la notte.";

                SetButtonVisible(_btnResearchHistorical, true);
                SetButtonVisible(_btnResearchBotanical, true);
                SetButtonVisible(_btnResearchVault, true);
                SetButtonVisible(_btnResearchSkip, true);
                if (_btnResearchSkip != null)
                    _btnResearchSkip.text = "Salta la ricerca";
                return;
            }

            if (title != null)
                title.text = "RICERCA NOTTURNA NON DISPONIBILE";
            if (subtitle != null)
                subtitle.text = "Non si possono effettuare letture di ricerca stanotte: non hai piu punti azione disponibili.";

            SetButtonVisible(_btnResearchHistorical, false);
            SetButtonVisible(_btnResearchBotanical, false);
            SetButtonVisible(_btnResearchVault, false);
            SetButtonVisible(_btnResearchSkip, true);
            if (_btnResearchSkip != null)
                _btnResearchSkip.text = "Continua → Previsione";
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button == null)
                return;
            button.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            button.SetEnabled(visible);
        }

        private void OnSleepClicked()
        {
            if (_dayCycleSystem == null || !_dayCycleSystem.CanEndDay()) return;
            _awaitingDawn = true;
            _dayBeforeTransition = _dayCycleSystem.CurrentDay;
            StartCoroutine(TransitionHibernationThenEndDay());
        }

        [SerializeField] private float _hibernationScreenDuration = 2.5f;
        [SerializeField] private float _dayTransitionScreenDuration = 2.5f;

        private IEnumerator TransitionHibernationThenEndDay()
        {
            ShowStep(6);
            yield return new WaitForSeconds(_hibernationScreenDuration);
            ShowStep(7);
            yield return new WaitForSeconds(_dayTransitionScreenDuration);
            if (_dayCycleSystem != null)
                _dayCycleSystem.EndDay();
        }

        private void OnDawnContinueClicked() => Hide();

        private void OnDestroy()
        {
            DetachEodButtonHandlers();
        }
    }
}
