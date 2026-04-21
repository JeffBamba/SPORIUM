using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using _Project.Sporae.Core;
using Sporae.DevTools;
using _Project.UI.UIToolkit.VoOverlay;

namespace Sporae.UI.UIToolkit.HUD
{
    /// <summary>
    /// Mission recap HUD (UI Toolkit): header collassabile, card missioni e tooltip contestuale.
    /// Spec: 280px, verde ciano CRT, tooltip HoverCard a destra, sezioni OBJECTIVE/TASK/REWARD/DEADLINE.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-38)]
    public sealed class ActiveMissionsPanelController : MonoBehaviour
    {
        private const string VisualTreeResourcePath = "UI/UIToolkit/ActiveMissions/ActiveMissions";
        private const string PanelSettingsResourcePath = "UI/UIToolkit/MainMenu/MainMenuPanelSettings";
        private const int SortingOrder = 210;
        private const float ProgressAnimDuration = 0.6f;
        private const float EmptyPulseDuration = 2f;
        private const float WarningPulseDuration = 1.5f;
        private const float TooltipShowDelayMs = 200f;
        private const float TooltipHideDelayMs = 100f;
        private const float TooltipHorizontalOffsetPx = 12f;
        private const float CompletedLingerSeconds = 10f;
        private const float CompletedFadeSeconds = 2.5f;

        private UIDocument _document;
        private VisualElement _root;
        private VisualElement _header;
        private Label _titleLabel;
        private Label _countLabel;
        private VisualElement _toggleChevron;
        private Button _filterActiveButton;
        private Button _filterCompletedButton;
        private VisualElement _content;
        private VisualElement _list;
        private Label _emptyLabel;

        private VisualElement _tooltip;
        private Label _tooltipEmoji;
        private Label _tooltipTitle;
        private Label _tooltipFaction;
        private Label _tooltipObjective;
        private Label _tooltipTaskSummary;
        private Label _tooltipReward;
        private Label _tooltipRep;
        private VisualElement _tooltipRepRow;
        private VisualElement _tooltipDeadline;
        private Label _tooltipDeadlineText;

        private MissionManager _missionManager;
        private DayCycleSystem _dayCycleSystem;
        private bool _collapsed;
        private bool _uiBound;
        private MissionFilterMode _filterMode = MissionFilterMode.Active;

        private IVisualElementScheduledItem _tooltipShowSchedule;
        private IVisualElementScheduledItem _tooltipHideSchedule;
        private VisualElement _pendingTooltipCard;
        private MissionChecker _pendingTooltipMission;
        private MissionMeta _pendingTooltipMeta;
        private MissionVisualStatus _pendingTooltipStatus;

        private readonly Dictionary<MissionChecker, MissionMeta> _missionMeta = new Dictionary<MissionChecker, MissionMeta>();
        private readonly List<MissionChecker> _completedMissions = new List<MissionChecker>();
        private readonly Dictionary<MissionChecker, float> _activeCompletedLingerUntil = new Dictionary<MissionChecker, float>();
        private readonly Dictionary<MissionChecker, Coroutine> _completedLingerCoroutines = new Dictionary<MissionChecker, Coroutine>();
        private readonly HashSet<MissionChecker> _completedProgressAnimationPlayed = new HashSet<MissionChecker>();
        private Coroutine _emptyPulseRoutine;
        private Coroutine _tooltipWarningPulseRoutine;

        private readonly struct MissionCardRow
        {
            public MissionCardRow(MissionChecker mission, bool isLingeredCompletion)
            {
                Mission = mission;
                IsLingeredCompletion = isLingeredCompletion;
            }

            public MissionChecker Mission { get; }
            public bool IsLingeredCompletion { get; }
        }

        private enum MissionFilterMode
        {
            Active,
            Completed
        }

        private enum MissionVisualStatus
        {
            Active,
            Completed,
            Expiring,
            Failed
        }

        private enum MissionFaction
        {
            Routine,
            Custodi,
            Mercanti,
            Cult
        }

        private readonly struct MissionMeta
        {
            public MissionMeta(int startDay, int plannedDays, MissionFaction faction)
            {
                StartDay = startDay;
                PlannedDays = plannedDays;
                Faction = faction;
            }

            public int StartDay { get; }
            public int PlannedDays { get; }
            public MissionFaction Faction { get; }
        }

        private void Awake()
        {
            BuildDocument();

            _missionManager = ServiceContainer.Instance?.Get<MissionManager>(suppressWarning: true);
            _dayCycleSystem = ServiceContainer.Instance?.Get<DayCycleSystem>(suppressWarning: true);

            if (_missionManager != null)
            {
                _missionManager.OnMissionsChanged += HandleMissionsChanged;
                _missionManager.OnMissionComplete += HandleMissionComplete;
                _missionManager.OnMissionAdded += HandleMissionAdded;
            }

            DemoBreakfastMission.ProgressChanged += OnDemoBreakfastProgressChanged;
            WardrobeMission.ProgressChanged += OnWardrobeMissionProgressChanged;
            DemoSeedStorageMission.ProgressChanged += OnDemoSeedStorageProgressChanged;

            ServiceContainer.Instance?.Register(this);
        }

        private void Start()
        {
            StartCoroutine(InitializeUiWhenReady());
        }

        private IEnumerator InitializeUiWhenReady()
        {
            int frames = 0;
            const int maxFrames = 90;
            while (frames < maxFrames &&
                   (_document == null || _document.rootVisualElement == null))
            {
                frames++;
                yield return null;
            }

            if (_document == null || _document.rootVisualElement == null)
            {
                SporiumLogger.LogError(LogCategory.UI,
                    "[ActiveMissions] UIDocument.rootVisualElement non disponibile: bind UI saltato.");
                yield break;
            }

            if (_uiBound)
                yield break;

            BindUi();
            _uiBound = _root != null && _list != null;

            if (!_uiBound)
            {
                SporiumLogger.LogError(LogCategory.UI,
                    "[ActiveMissions] BindUi non ha popolato root/list (UXML mancante o nomi errati).");
                yield break;
            }

            if (_missionManager != null)
                HandleMissionsChanged();

            if (_root != null)
            {
                _root.AddToClassList("active-missions-root--enter");
                _root.schedule.Execute(() => _root.RemoveFromClassList("active-missions-root--enter")).ExecuteLater(16);
            }
        }

        private void OnDestroy()
        {
            if (_missionManager != null)
            {
                _missionManager.OnMissionsChanged -= HandleMissionsChanged;
                _missionManager.OnMissionComplete -= HandleMissionComplete;
                _missionManager.OnMissionAdded -= HandleMissionAdded;
            }

            DemoBreakfastMission.ProgressChanged -= OnDemoBreakfastProgressChanged;
            WardrobeMission.ProgressChanged -= OnWardrobeMissionProgressChanged;
            DemoSeedStorageMission.ProgressChanged -= OnDemoSeedStorageProgressChanged;

            if (_filterActiveButton != null)
                _filterActiveButton.UnregisterCallback<ClickEvent>(HandleFilterActiveClicked);
            if (_filterCompletedButton != null)
                _filterCompletedButton.UnregisterCallback<ClickEvent>(HandleFilterCompletedClicked);

            StopEmptyPulse();
            StopTooltipWarningPulse();
            foreach (var kv in _completedLingerCoroutines.ToList())
            {
                if (kv.Value != null)
                    StopCoroutine(kv.Value);
            }
            _completedLingerCoroutines.Clear();
            CancelTooltipSchedules();
        }

        private void OnDemoBreakfastProgressChanged()
        {
            HandleMissionsChanged();
        }

        private void OnWardrobeMissionProgressChanged()
        {
            HandleMissionsChanged();
        }

        private void OnDemoSeedStorageProgressChanged()
        {
            HandleMissionsChanged();
        }

        private void HandleMissionAdded(MissionChecker mission)
        {
            if (mission?.Config == null)
                return;

            string title = GetMissionTitle(mission);

            var toastManager = ServiceContainer.Instance?.Get<ToastNotificationManager>(suppressWarning: true);
            if (toastManager != null)
            {
                toastManager.ShowMission($"Nuova missione: {title}", "MIS-NEW");
                return;
            }

            var foundation = Sporae.UI.UIToolkit.NotificationsFoundation.FoundationNotificationServiceAccessor.Get();
            if (foundation != null)
            {
                var payload = new Sporae.UI.UIToolkit.NotificationsFoundation.NotificationPayload()
                    .With("title", title);
                foundation.PostToast("MIS-NEW",
                    payload,
                    Sporae.UI.UIToolkit.NotificationsFoundation.NotificationSeverity.Info);
            }
        }

        private void HandleMissionComplete(MissionChecker mission)
        {
            if (mission?.Config == null)
                return;

            if (!_completedMissions.Contains(mission))
                _completedMissions.Add(mission);
            _activeCompletedLingerUntil[mission] = Time.unscaledTime + CompletedLingerSeconds;
            _completedProgressAnimationPlayed.Remove(mission);
            StartCompletedLingerSequence(mission);

            string title = GetMissionTitle(mission);

            var toastManager = ServiceContainer.Instance?.Get<ToastNotificationManager>(suppressWarning: true);
            if (toastManager != null)
            {
                toastManager.ShowMission($"Missione completata: {title}", "MIS-DONE");
            }
            else
            {
                var foundation = Sporae.UI.UIToolkit.NotificationsFoundation.FoundationNotificationServiceAccessor.Get();
                if (foundation != null)
                {
                    var payload = new Sporae.UI.UIToolkit.NotificationsFoundation.NotificationPayload()
                        .With("title", title);
                    foundation.PostToast("MIS-DONE",
                        payload,
                        Sporae.UI.UIToolkit.NotificationsFoundation.NotificationSeverity.Success);
                }
            }

            var demoSession = ServiceContainer.Instance?.Get<DemoSessionState>(suppressWarning: true);
            bool skipGenericCompletionVo = demoSession != null && demoSession.IsDemo
                && DemoBreakfastMission.IsDemoBreakfastConfig(mission.Config);

            var vo = ServiceContainer.Instance?.Get<VoOverlayController>(suppressWarning: true);
            if (vo != null && !skipGenericCompletionVo)
            {
                string line = $"Ottimo lavoro. Missione completata: {title}.";
                var presentation = new VoLinePresentationOptions(
                    useMultiSentenceWhenSplit: true,
                    advanceMode: VoSentenceAdvanceMode.AutoReadingPause,
                    minReadSeconds: 0.7f,
                    readSecondsPerChar: 0.048f,
                    continueHintText: string.Empty);
                vo.ShowLine(line, VoRegister.RegisterB, null, null, false, presentation);
            }

            HandleMissionsChanged();
        }

        private void BuildDocument()
        {
            var vta = Resources.Load<VisualTreeAsset>(VisualTreeResourcePath);
            if (vta == null)
            {
                Debug.LogError($"[ActiveMissions] VisualTreeAsset non trovato: {VisualTreeResourcePath}");
                return;
            }

            var panelSettings = Resources.Load<PanelSettings>(PanelSettingsResourcePath);
            if (panelSettings == null)
            {
                Debug.LogError($"[ActiveMissions] PanelSettings non trovato: {PanelSettingsResourcePath}");
                return;
            }

            _document = GetComponent<UIDocument>();
            if (_document == null)
                _document = gameObject.AddComponent<UIDocument>();

            _document.panelSettings = panelSettings;
            _document.visualTreeAsset = vta;
            _document.sortingOrder = SortingOrder;
        }

        private void BindUi()
        {
            if (_document == null || _document.rootVisualElement == null)
                return;

            var rootVe = _document.rootVisualElement;
            _root = rootVe.Q<VisualElement>("active-missions-root");
            _header = rootVe.Q<VisualElement>("active-missions-header");
            _titleLabel = rootVe.Q<Label>("active-missions-title");
            _countLabel = rootVe.Q<Label>("active-missions-count");
            _toggleChevron = rootVe.Q<VisualElement>("active-missions-toggle-chevron");
            _filterActiveButton = rootVe.Q<Button>("active-missions-filter-active");
            _filterCompletedButton = rootVe.Q<Button>("active-missions-filter-completed");
            _content = rootVe.Q<VisualElement>("active-missions-content");
            _list = rootVe.Q<VisualElement>("active-missions-list");
            _emptyLabel = rootVe.Q<Label>("active-missions-empty");

            _tooltip = rootVe.Q<VisualElement>("active-mission-tooltip");
            _tooltipEmoji = rootVe.Q<Label>("am-tooltip-emoji");
            _tooltipTitle = rootVe.Q<Label>("am-tooltip-title");
            _tooltipFaction = rootVe.Q<Label>("am-tooltip-faction");
            _tooltipObjective = rootVe.Q<Label>("am-tooltip-objective");
            _tooltipTaskSummary = rootVe.Q<Label>("am-tooltip-task-summary");
            _tooltipReward = rootVe.Q<Label>("am-tooltip-reward");
            _tooltipRep = rootVe.Q<Label>("am-tooltip-rep");
            _tooltipRepRow = rootVe.Q<VisualElement>("am-tooltip-rep-row");
            _tooltipDeadline = rootVe.Q<VisualElement>("am-tooltip-deadline");
            _tooltipDeadlineText = rootVe.Q<Label>("am-tooltip-deadline-text");

            if (_root != null)
                _root.style.display = DisplayStyle.Flex;

            if (_tooltip != null)
                _tooltip.style.display = DisplayStyle.None;

            if (_header != null)
                _header.RegisterCallback<ClickEvent>(_ => ToggleCollapsed());
            if (_filterActiveButton != null)
                _filterActiveButton.RegisterCallback<ClickEvent>(HandleFilterActiveClicked);
            if (_filterCompletedButton != null)
                _filterCompletedButton.RegisterCallback<ClickEvent>(HandleFilterCompletedClicked);

            ApplyFilterButtonState();
        }

        private void ToggleCollapsed()
        {
            _collapsed = !_collapsed;
            if (_root == null)
                return;

            if (_collapsed)
            {
                _root.AddToClassList("active-missions--collapsed");
                _toggleChevron?.AddToClassList("active-missions-toggle-chevron--collapsed");
            }
            else
            {
                _root.RemoveFromClassList("active-missions--collapsed");
                _toggleChevron?.RemoveFromClassList("active-missions-toggle-chevron--collapsed");
            }
        }

        private void HandleFilterActiveClicked(ClickEvent evt)
        {
            evt.StopPropagation();
            SetFilterMode(MissionFilterMode.Active);
        }

        private void HandleFilterCompletedClicked(ClickEvent evt)
        {
            evt.StopPropagation();
            SetFilterMode(MissionFilterMode.Completed);
        }

        private void SetFilterMode(MissionFilterMode mode)
        {
            if (_filterMode == mode)
                return;

            _filterMode = mode;
            ApplyFilterButtonState();
            HandleMissionsChanged();
        }

        private void ApplyFilterButtonState()
        {
            ToggleSelectedClass(_filterActiveButton, _filterMode == MissionFilterMode.Active);
            ToggleSelectedClass(_filterCompletedButton, _filterMode == MissionFilterMode.Completed);
            ToggleCompletedSelectedClass(_filterCompletedButton, _filterMode == MissionFilterMode.Completed);
        }

        private static void ToggleSelectedClass(VisualElement element, bool selected)
        {
            if (element == null)
                return;

            const string selectedClass = "active-missions-filter-button--selected";
            if (selected)
                element.AddToClassList(selectedClass);
            else
                element.RemoveFromClassList(selectedClass);
        }

        private static void ToggleCompletedSelectedClass(VisualElement element, bool selected)
        {
            if (element == null)
                return;

            const string selectedDoneClass = "active-missions-filter-button--selected-done";
            if (selected)
                element.AddToClassList(selectedDoneClass);
            else
                element.RemoveFromClassList(selectedDoneClass);
        }

        private void HandleMissionsChanged()
        {
            if (_root == null || _list == null || _titleLabel == null)
                return;

            PruneExpiredCompletedLinger();

            var activeMissions = _missionManager?.CurrentMissions
                .Where(m => m != null && m.Config != null)
                .ToList() ?? new List<MissionChecker>();

            var completedMissions = _completedMissions
                .Where(m => m != null && m.Config != null && m.IsCompleted)
                .ToList();

            if (_missionManager?.CompletedMissions != null)
            {
                foreach (var completed in _missionManager.CompletedMissions)
                {
                    if (completed == null || completed.Config == null || !completed.IsCompleted)
                        continue;
                    if (!completedMissions.Contains(completed))
                        completedMissions.Add(completed);
                }
            }

            var missionRows = BuildRowsForFilter(activeMissions, completedMissions);

            var allKnownMissions = activeMissions
                .Concat(completedMissions)
                .Distinct()
                .ToList();
            SyncMissionMeta(allKnownMissions);

            if (_countLabel != null)
            {
                _titleLabel.text = _filterMode == MissionFilterMode.Completed ? "MISSIONI COMPLETATE" : "MISSIONI";
                _countLabel.text = $"[{missionRows.Count}]";
            }
            else
            {
                string title = _filterMode == MissionFilterMode.Completed ? "MISSIONI COMPLETATE" : "MISSIONI";
                _titleLabel.text = $"{title} [{missionRows.Count}]";
            }

            RebuildList(missionRows);
            bool hasMissions = missionRows.Count > 0;
            if (_emptyLabel != null)
            {
                _emptyLabel.text = _filterMode == MissionFilterMode.Completed ? "Nessuna missione completata_" : "Nessuna missione_";
                _emptyLabel.style.display = hasMissions ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (hasMissions)
                StopEmptyPulse();
            else
                StartEmptyPulse();

            if (!hasMissions)
                HideTooltipImmediate();
        }

        private void SyncMissionMeta(List<MissionChecker> missions)
        {
            var liveSet = new HashSet<MissionChecker>(missions);
            var toRemove = _missionMeta.Keys.Where(k => !liveSet.Contains(k)).ToList();
            foreach (var k in toRemove)
                _missionMeta.Remove(k);
            foreach (var k in toRemove)
                _completedProgressAnimationPlayed.Remove(k);

            int currentDay = Mathf.Max(1, _dayCycleSystem?.CurrentDay ?? 1);
            foreach (var mission in missions)
            {
                if (_missionMeta.ContainsKey(mission))
                    continue;

                var cfg = mission.Config;
                var faction = GuessFaction(cfg);
                int plannedDays = GuessPlannedDays(cfg);
                _missionMeta[mission] = new MissionMeta(currentDay, plannedDays, faction);
            }
        }

        private List<MissionCardRow> BuildRowsForFilter(List<MissionChecker> activeMissions, List<MissionChecker> completedMissions)
        {
            var rows = new List<MissionCardRow>();
            if (_filterMode == MissionFilterMode.Completed)
            {
                foreach (var mission in completedMissions)
                    rows.Add(new MissionCardRow(mission, false));
                return rows;
            }

            var activeSet = new HashSet<MissionChecker>(activeMissions);
            foreach (var mission in activeMissions)
                rows.Add(new MissionCardRow(mission, false));

            foreach (var mission in completedMissions)
            {
                if (mission == null || !mission.IsCompleted)
                    continue;
                if (activeSet.Contains(mission))
                    continue;
                if (!_activeCompletedLingerUntil.TryGetValue(mission, out var until))
                    continue;
                if (Time.unscaledTime >= until)
                    continue;
                rows.Add(new MissionCardRow(mission, true));
            }
            return rows;
        }

        private void RebuildList(List<MissionCardRow> rows)
        {
            _list.Clear();

            foreach (var row in rows)
            {
                var mission = row.Mission;
                if (!_missionMeta.TryGetValue(mission, out var meta))
                    continue;

                BuildMissionCard(mission, meta, row.IsLingeredCompletion);
            }
        }

        private void BuildMissionCard(MissionChecker mission, MissionMeta meta, bool isLingeredCompletion)
        {
            int daysLeft = GetDaysRemaining(meta);
            var status = GetVisualStatus(meta, mission.IsCompleted);
            float progress = GetProgress(mission, meta, mission.IsCompleted);
            string statusClass = GetStatusClass(status);
            string factionClass = GetFactionClass(meta.Faction);

            var card = new VisualElement();
            card.AddToClassList("active-mission-card");
            card.AddToClassList(statusClass);
            card.AddToClassList(factionClass);
            if (isLingeredCompletion && _activeCompletedLingerUntil.TryGetValue(mission, out var lingerUntil))
            {
                float remain = lingerUntil - Time.unscaledTime;
                if (remain <= CompletedFadeSeconds)
                    card.AddToClassList("active-mission-card--fade-out");
            }

            var topRow = new VisualElement();
            topRow.AddToClassList("active-mission-card-top");

            var emoji = new Label("★");
            emoji.AddToClassList("active-mission-card-emoji");
            topRow.Add(emoji);

            var title = new Label(GetMissionTitle(mission));
            title.AddToClassList("active-mission-card-title");
            topRow.Add(title);

            var timerWrap = new VisualElement();
            timerWrap.AddToClassList("active-mission-card-timer-wrap");
            var timerIcon = new Label("o");
            timerIcon.AddToClassList("active-mission-card-timer-icon");
            string timerValue = mission.IsCompleted ? "FATTA" : $"{Mathf.Max(0, daysLeft)}g";
            var timerText = new Label(timerValue);
            timerText.AddToClassList("active-mission-card-timer");
            if (!mission.IsCompleted && daysLeft <= 2)
                timerText.AddToClassList("active-mission-card-timer--warn");
            timerWrap.Add(timerIcon);
            timerWrap.Add(timerText);
            topRow.Add(timerWrap);

            card.Add(topRow);

            var progressWrap = new VisualElement();
            progressWrap.AddToClassList("active-mission-card-progress");
            var progressFill = new VisualElement();
            progressFill.AddToClassList("active-mission-card-progress-fill");
            progressFill.style.width = Length.Percent(0f);

            var percentLabel = new Label($"{Mathf.RoundToInt(progress * 100f)}%");
            percentLabel.AddToClassList("active-mission-card-progress-percent");
            progressWrap.Add(progressFill);
            progressWrap.Add(percentLabel);
            card.Add(progressWrap);

            _list.Add(card);

            bool shouldAnimateCompletedProgress = mission.IsCompleted &&
                !_completedProgressAnimationPlayed.Contains(mission);
            if (shouldAnimateCompletedProgress)
            {
                _completedProgressAnimationPlayed.Add(mission);
                StartCoroutine(AnimateProgress(progressFill, progress));
            }
            else if (mission.IsCompleted)
            {
                progressFill.style.width = Length.Percent(progress * 100f);
            }
            else
            {
                StartCoroutine(AnimateProgress(progressFill, progress));
            }

            card.RegisterCallback<PointerEnterEvent>(_ => QueueShowTooltip(card, mission, meta, status));
            card.RegisterCallback<PointerLeaveEvent>(_ => QueueHideTooltip());
        }

        private void QueueShowTooltip(VisualElement card, MissionChecker mission, MissionMeta meta, MissionVisualStatus status)
        {
            _pendingTooltipCard = card;
            _pendingTooltipMission = mission;
            _pendingTooltipMeta = meta;
            _pendingTooltipStatus = status;

            _tooltipHideSchedule?.Pause();
            _tooltipHideSchedule = null;

            _tooltipShowSchedule?.Pause();
            _tooltipShowSchedule = _root?.schedule.Execute(() =>
            {
                _tooltipShowSchedule = null;
                ShowTooltipNow(_pendingTooltipCard, _pendingTooltipMission, _pendingTooltipMeta, _pendingTooltipStatus);
            });
            _tooltipShowSchedule?.ExecuteLater((long)TooltipShowDelayMs);
        }

        private void QueueHideTooltip()
        {
            _tooltipShowSchedule?.Pause();
            _tooltipShowSchedule = null;

            _tooltipHideSchedule?.Pause();
            _tooltipHideSchedule = _root?.schedule.Execute(() =>
            {
                _tooltipHideSchedule = null;
                HideTooltipImmediate();
            });
            _tooltipHideSchedule?.ExecuteLater((long)TooltipHideDelayMs);
        }

        private void CancelTooltipSchedules()
        {
            _tooltipShowSchedule?.Pause();
            _tooltipShowSchedule = null;
            _tooltipHideSchedule?.Pause();
            _tooltipHideSchedule = null;
        }

        private void ShowTooltipNow(VisualElement card, MissionChecker mission, MissionMeta meta, MissionVisualStatus status)
        {
            if (_tooltip == null || _root == null || card == null || mission == null || mission.Config == null)
                return;

            _tooltip.RemoveFromClassList("am-status-active");
            _tooltip.RemoveFromClassList("am-status-completed");
            _tooltip.RemoveFromClassList("am-status-expiring");
            _tooltip.RemoveFromClassList("am-status-failed");
            _tooltip.AddToClassList(GetStatusClass(status));

            _tooltipEmoji.text = GetFactionEmoji(meta.Faction);
            _tooltipTitle.text = GetMissionTitle(mission);
            _tooltipFaction.text = GetFactionName(meta.Faction);
            _tooltipObjective.text = string.IsNullOrWhiteSpace(mission.Config.Description)
                ? "Nessuna descrizione dettagliata."
                : mission.Config.Description.Trim();
            _tooltipTaskSummary.text = $"-> {GetPrimaryObjectiveLine(mission)}";
            _tooltipReward.text = BuildRewardLine(mission.Config);

            string rep = BuildRepLine(mission.Config);
            bool hasRep = !string.IsNullOrEmpty(rep);
            _tooltipRepRow.style.display = hasRep ? DisplayStyle.Flex : DisplayStyle.None;
            _tooltipRep.text = rep;

            int daysLeft = Mathf.Max(0, GetDaysRemaining(meta));
            bool showDeadline = status != MissionVisualStatus.Completed && daysLeft <= 2;
            _tooltipDeadline.style.display = showDeadline ? DisplayStyle.Flex : DisplayStyle.None;
            _tooltipDeadlineText.text = $"SCADE TRA {daysLeft} GIORNI";

            StopTooltipWarningPulse();
            if (showDeadline)
                StartTooltipWarningPulse();

            // Tooltip è figlio diretto di active-missions-root (position:absolute in USS).
            // left = larghezza del root + offset; top = offset verticale della card dal root.
            float left = _root.resolvedStyle.width + TooltipHorizontalOffsetPx;
            float top = card.layout.yMin;
            _tooltip.style.left = left;
            _tooltip.style.top = top;
            _tooltip.style.display = DisplayStyle.Flex;
        }

        private void HideTooltipImmediate()
        {
            if (_tooltip != null)
                _tooltip.style.display = DisplayStyle.None;
            StopTooltipWarningPulse();
        }

        private void PruneExpiredCompletedLinger()
        {
            if (_activeCompletedLingerUntil.Count == 0)
                return;

            float now = Time.unscaledTime;
            var expired = _activeCompletedLingerUntil
                .Where(kv => now >= kv.Value)
                .Select(kv => kv.Key)
                .ToList();
            foreach (var mission in expired)
            {
                _activeCompletedLingerUntil.Remove(mission);
                if (_completedLingerCoroutines.TryGetValue(mission, out var co) && co != null)
                    StopCoroutine(co);
                _completedLingerCoroutines.Remove(mission);
            }
        }

        private void StartCompletedLingerSequence(MissionChecker mission)
        {
            if (mission == null)
                return;
            if (_completedLingerCoroutines.TryGetValue(mission, out var running) && running != null)
                StopCoroutine(running);
            _completedLingerCoroutines[mission] = StartCoroutine(CompletedLingerSequence(mission));
        }

        private IEnumerator CompletedLingerSequence(MissionChecker mission)
        {
            float waitBeforeFade = Mathf.Max(0f, CompletedLingerSeconds - CompletedFadeSeconds);
            if (waitBeforeFade > 0f)
                yield return new WaitForSecondsRealtime(waitBeforeFade);

            // Trigger singolo redraw per applicare la classe fade-out quando manca poco alla scadenza.
            HandleMissionsChanged();

            if (CompletedFadeSeconds > 0f)
                yield return new WaitForSecondsRealtime(CompletedFadeSeconds);

            _activeCompletedLingerUntil.Remove(mission);
            _completedLingerCoroutines.Remove(mission);
            HandleMissionsChanged();
        }

        private static string GetMissionTitle(MissionChecker mission)
        {
            if (mission?.Config == null || string.IsNullOrWhiteSpace(mission.Config.Title))
                return "Missione senza titolo";
            return mission.Config.Title.Trim();
        }

        private static string GetPrimaryObjectiveLine(MissionChecker mission)
        {
            if (mission?.Config == null || mission.Config.Goals == null)
                return "Nessun obiettivo";

            foreach (var goal in mission.Config.Goals)
            {
                if (goal.Options == null)
                    continue;

                foreach (var option in goal.Options)
                {
                    if (option == null || string.IsNullOrWhiteSpace(option.Title))
                        continue;
                    return option.Title.Trim();
                }
            }

            return "Nessun obiettivo";
        }

        private static string BuildRewardLine(MissionConfig cfg)
        {
            if (cfg == null)
                return "-";

            var reward = cfg.QuickPathReward;
            string cry = reward.CryReward > 0 ? $"+{reward.CryReward} CRY" : string.Empty;

            string item = string.Empty;
            if (reward.Rewards != null && reward.Rewards.Count > 0 && reward.Rewards[0].Item != null)
            {
                var slot = reward.Rewards[0];
                item = $"{slot.Quantity}x {slot.Item.TypeId}";
            }

            if (!string.IsNullOrEmpty(cry) && !string.IsNullOrEmpty(item))
                return $"{cry} + {item}";
            if (!string.IsNullOrEmpty(cry))
                return cry;
            if (!string.IsNullOrEmpty(item))
                return item;

            return "Nessuna ricompensa";
        }

        private static string BuildRepLine(MissionConfig cfg)
        {
            if (cfg == null || cfg.Title == null)
                return string.Empty;

            string title = cfg.Title.ToLowerInvariant();
            if (title.Contains("rep+") || title.Contains("reputation+"))
                return "+REP";
            if (title.Contains("rep-") || title.Contains("reputation-"))
                return "-REP";
            return string.Empty;
        }

        private MissionFaction GuessFaction(MissionConfig cfg)
        {
            if (cfg != null && (WardrobeMission.IsDemoWardrobeConfig(cfg) || DemoBreakfastMission.IsDemoBreakfastConfig(cfg) ||
                                DemoSeedStorageMission.IsDemoSeedStorageConfig(cfg)))
                return MissionFaction.Routine;

            string key = $"{cfg?.Title} {cfg?.Description}".ToLowerInvariant();
            if (key.Contains("merc") || key.Contains("cry") || key.Contains("trade"))
                return MissionFaction.Mercanti;
            if (key.Contains("cult") || key.Contains("ritual") || key.Contains("fire"))
                return MissionFaction.Cult;
            return MissionFaction.Custodi;
        }

        private static int GuessPlannedDays(MissionConfig cfg)
        {
            string key = $"{cfg?.Title} {cfg?.Description}".ToLowerInvariant();
            if (key.Contains("urgent") || key.Contains("now") || key.Contains("subito"))
                return 2;
            if (key.Contains("long") || key.Contains("extended"))
                return 6;
            return 4;
        }

        private int GetDaysRemaining(MissionMeta meta)
        {
            int day = Mathf.Max(1, _dayCycleSystem?.CurrentDay ?? 1);
            int elapsed = Mathf.Max(0, day - meta.StartDay);
            return Mathf.Max(0, meta.PlannedDays - elapsed);
        }

        private int GetElapsedDays(MissionMeta meta)
        {
            int day = Mathf.Max(1, _dayCycleSystem?.CurrentDay ?? 1);
            return Mathf.Max(0, day - meta.StartDay);
        }

        private MissionVisualStatus GetVisualStatus(MissionMeta meta, bool isCompleted)
        {
            if (isCompleted)
                return MissionVisualStatus.Completed;

            int remaining = GetDaysRemaining(meta);
            if (remaining <= 0)
                return MissionVisualStatus.Failed;
            if (remaining <= 2)
                return MissionVisualStatus.Expiring;
            return MissionVisualStatus.Active;
        }

        private float GetProgress(MissionChecker mission, MissionMeta meta, bool isCompleted)
        {
            if (isCompleted)
                return 1f;

            if (mission?.Config != null && DemoBreakfastMission.IsDemoBreakfastConfig(mission.Config))
            {
                float p = DemoBreakfastMission.GetObjectiveProgress01(mission.Config);
                if (p >= 0f)
                    return Mathf.Clamp01(p);
            }
            if (mission?.Config != null && WardrobeMission.IsDemoWardrobeConfig(mission.Config))
            {
                float p = WardrobeMission.GetObjectiveProgress01(mission.Config);
                if (p >= 0f)
                    return Mathf.Clamp01(p);
            }
            if (mission?.Config != null && DemoSeedStorageMission.IsDemoSeedStorageConfig(mission.Config))
            {
                float p = DemoSeedStorageMission.GetObjectiveProgress01(mission.Config);
                if (p >= 0f)
                    return Mathf.Clamp01(p);
            }

            int elapsed = GetElapsedDays(meta);
            float ratio = meta.PlannedDays <= 0 ? 0f : elapsed / (float)meta.PlannedDays;
            return Mathf.Clamp01(ratio);
        }

        private static string GetStatusClass(MissionVisualStatus status)
        {
            return status switch
            {
                MissionVisualStatus.Completed => "am-status-completed",
                MissionVisualStatus.Expiring => "am-status-expiring",
                MissionVisualStatus.Failed => "am-status-failed",
                _ => "am-status-active"
            };
        }

        private static string GetFactionClass(MissionFaction faction)
        {
            return faction switch
            {
                MissionFaction.Routine => "am-faction-routine",
                MissionFaction.Mercanti => "am-faction-mercanti",
                MissionFaction.Cult => "am-faction-cult",
                _ => "am-faction-custodi"
            };
        }

        private static string GetFactionEmoji(MissionFaction faction)
        {
            return faction switch
            {
                MissionFaction.Routine => "★",
                MissionFaction.Mercanti => "\uD83D\uDCB0",
                MissionFaction.Cult => "\uD83D\uDD25",
                _ => "\uD83C\uDF3F"
            };
        }

        private static string GetFactionName(MissionFaction faction)
        {
            return faction switch
            {
                MissionFaction.Routine => "Routine",
                MissionFaction.Mercanti => "Mercanti",
                MissionFaction.Cult => "Cult",
                _ => "Custodi"
            };
        }

        private IEnumerator AnimateProgress(VisualElement fill, float target)
        {
            if (fill == null)
                yield break;

            float t = 0f;
            while (t < ProgressAnimDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / ProgressAnimDuration);
                k = 1f - Mathf.Pow(1f - k, 3f);
                fill.style.width = Length.Percent(target * 100f * k);
                yield return null;
            }

            fill.style.width = Length.Percent(target * 100f);
        }

        private void StartEmptyPulse()
        {
            if (_emptyLabel == null)
                return;
            if (_emptyPulseRoutine != null)
            {
                StopCoroutine(_emptyPulseRoutine);
                _emptyPulseRoutine = null;
            }
            // Nessun tint runtime: rispetta il colore impostato in USS/UI Builder.
            _emptyLabel.style.opacity = 1f;
        }

        private void StopEmptyPulse()
        {
            if (_emptyPulseRoutine != null)
            {
                StopCoroutine(_emptyPulseRoutine);
                _emptyPulseRoutine = null;
            }
            if (_emptyLabel != null)
                _emptyLabel.style.opacity = 1f;
        }

        private IEnumerator EmptyPulseRoutine()
        {
            float t = 0f;
            while (_emptyLabel != null && _emptyLabel.style.display != DisplayStyle.None)
            {
                t += Time.unscaledDeltaTime;
                float phase = Mathf.PingPong(t / EmptyPulseDuration * 2f, 1f);
                _emptyLabel.style.opacity = Mathf.Lerp(0.4f, 0.7f, phase);
                yield return null;
            }
            _emptyPulseRoutine = null;
        }

        private void StartTooltipWarningPulse()
        {
            if (_tooltipWarningPulseRoutine != null || _tooltipDeadline == null)
                return;
            _tooltipWarningPulseRoutine = StartCoroutine(TooltipWarningPulseRoutine());
        }

        private void StopTooltipWarningPulse()
        {
            if (_tooltipWarningPulseRoutine == null)
                return;
            StopCoroutine(_tooltipWarningPulseRoutine);
            _tooltipWarningPulseRoutine = null;
            if (_tooltipDeadline != null)
                _tooltipDeadline.style.opacity = 1f;
        }

        private IEnumerator TooltipWarningPulseRoutine()
        {
            float t = 0f;
            while (_tooltipDeadline != null && _tooltip.style.display == DisplayStyle.Flex)
            {
                t += Time.unscaledDeltaTime;
                float phase = Mathf.PingPong(t / WarningPulseDuration * 2f, 1f);
                _tooltipDeadline.style.opacity = Mathf.Lerp(0.6f, 1f, phase);
                yield return null;
            }
            _tooltipWarningPulseRoutine = null;
        }
    }
}
