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

        private bool _bound;
        private VisualElement _eodVisualTreeBoundRoot;
        private bool _awaitingDawn;
        private bool _nightResearchChosen;
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
            var sb = new StringBuilder();
            if (_dayActivityLog != null)
            {
                var h = _dayActivityLog.HarvestsThisDay;
                if (h.Count > 0) sb.AppendLine(LocalizationManager.GetString("eod.diario_harvest"));
                var w = _dayActivityLog.PotIdsWateringTurnedOnThisDay;
                if (w.Count > 0) sb.AppendLine(LocalizationManager.GetString("eod.diario_water"));
            }
            sb.AppendLine(LocalizationManager.GetString("eod.diario_footer"));
            string full = sb.ToString();
            if (_diarioText != null)
            {
                _diarioText.text = "";
                StartCoroutine(Typewriter(_diarioText, full));
            }
        }

        private void PopulateForecast()
        {
            var sbToday = new StringBuilder(LocalizationManager.GetString("eod.forecast_today"));
            if (_diaryStatistics != null)
            {
                int max = _gameManager?.ActionSystem?.MaxActions ?? 5;
                sbToday.AppendLine(LocalizationManager.GetString("eod.forecast_actions_today", new Dictionary<string, string>
                {
                    ["used"] = _diaryStatistics.ActionsSpent.ToString(),
                    ["max"] = max.ToString()
                }));
                sbToday.AppendLine(LocalizationManager.GetString("eod.forecast_cry_today", new Dictionary<string, string>
                {
                    ["earned"] = _diaryStatistics.CryEarned.ToString(),
                    ["spent"] = _diaryStatistics.CrySpent.ToString()
                }));
            }
            if (_phSystem != null)
                sbToday.AppendLine(LocalizationManager.GetString("eod.forecast_ph_line", new Dictionary<string, string>
                {
                    ["ph"] = _phSystem.CurrentPh.ToString("F1"),
                    ["band"] = _phSystem.GetBandName()
                }));
            sbToday.AppendLine(LocalizationManager.GetString("eod.forecast_reputations"));
            string textToday = sbToday.ToString();

            float predictedPhDrift = _dayCycleController != null ? _dayCycleController.GetPredictedPhDriftForNextDay() : float.NaN;
            string predictedPhDriftStr = float.IsNaN(predictedPhDrift) ? "—" : predictedPhDrift.ToString("+#0.0;-#0.0;0", System.Globalization.CultureInfo.InvariantCulture);

            int maxActionsForecast = _gameManager?.ActionSystem?.MaxActions ?? 5;
            var sbTomorrow = new StringBuilder(LocalizationManager.GetString("eod.forecast_tomorrow"));
            sbTomorrow.AppendLine(LocalizationManager.GetString("eod.forecast_actions_avail", new Dictionary<string, string> { ["n"] = maxActionsForecast.ToString() }));
            sbTomorrow.AppendLine(LocalizationManager.GetString("eod.forecast_ph_drift", new Dictionary<string, string> { ["v"] = predictedPhDriftStr }));
            sbTomorrow.AppendLine(LocalizationManager.GetString("eod.forecast_risks"));
            sbTomorrow.Append(LocalizationManager.GetString("eod.forecast_missions"));
            string textTomorrow = sbTomorrow.ToString();

            string textResearch = _nightResearchChosen ? LocalizationManager.GetString("eod.forecast_research_done") : "";
            StartCoroutine(RunForecastTypewriter(textToday, textTomorrow, textResearch));
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
            if (title != null) title.text = LocalizationManager.GetString("eod.dawn_title");
            var sub = _root.Q<Label>("eod-dawn-subtitle");
            if (sub != null) sub.text = LocalizationManager.GetString("eod.dawn_sub", new Dictionary<string, string> { ["day"] = newDay.ToString() });

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

            SetDawnRow("eod-dawn-text-mutation", float.IsNaN(mutation)
                ? LocalizationManager.GetString("eod.dawn_mutation_empty")
                : LocalizationManager.GetString("eod.dawn_mutation", new Dictionary<string, string> { ["v"] = mutation.ToString("P0") }));
            SetDawnRow("eod-dawn-text-ph", LocalizationManager.GetString("eod.dawn_ph_drift", new Dictionary<string, string> { ["v"] = phDriftStr, ["trend"] = phTrend }));
            SetDawnRow("eod-dawn-text-condensation", float.IsNaN(condensation)
                ? LocalizationManager.GetString("eod.dawn_cond_empty")
                : LocalizationManager.GetString("eod.dawn_cond", new Dictionary<string, string> { ["v"] = condensation.ToString("F0") }));
            SetDawnRow("eod-dawn-text-grate", LocalizationManager.GetString("eod.dawn_grate", new Dictionary<string, string> { ["n"] = grate.ToString() }));
            SetDawnRow("eod-dawn-text-cry", LocalizationManager.GetString("eod.dawn_cry", new Dictionary<string, string> { ["cry"] = cryForecast.ToString() }));

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
                    LocalizationManager.GetString("eod.tt_mutation_title"),
                    LocalizationManager.GetString("eod.tt_mutation_desc"),
                    LocalizationManager.GetString("eod.tt_mutation_tip")),
                ["ph"] = (
                    LocalizationManager.GetString("eod.tt_ph_title"),
                    phDesc,
                    LocalizationManager.GetString("eod.tt_ph_tip")),
                ["condensation"] = (
                    LocalizationManager.GetString("eod.tt_cond_title"),
                    LocalizationManager.GetString("eod.tt_cond_desc"),
                    LocalizationManager.GetString("eod.tt_cond_tip")),
                ["grate"] = (
                    LocalizationManager.GetString("eod.tt_grate_title"),
                    LocalizationManager.GetString("eod.tt_grate_desc"),
                    LocalizationManager.GetString("eod.tt_grate_tip")),
                ["cry"] = (
                    LocalizationManager.GetString("eod.tt_cry_title"),
                    LocalizationManager.GetString("eod.tt_cry_desc"),
                    LocalizationManager.GetString("eod.tt_cry_tip"))
            };

            _dawnTooltipData = tooltips;
            RegisterDawnTooltipsOnce();
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
                ShowStep(4);
            else
                ShowStep(5);
        }

        private void OnResearchHistoricalClicked() => OnResearchChosen("Historical");
        private void OnResearchBotanicalClicked() => OnResearchChosen("Botanical");
        private void OnResearchVaultClicked() => OnResearchChosen("Vault");
        private void OnResearchSkipClicked() => OnResearchChosen(null);

        private void OnResearchChosen(string branch)
        {
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
