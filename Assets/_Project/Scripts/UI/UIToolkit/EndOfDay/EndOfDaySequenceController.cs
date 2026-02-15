using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using _Project.Sporae.Core;
using Sporae.Core;
using Sporae.DevTools;

namespace _Project
{
    /// <summary>
    /// Controller della sequenza End of Day: Conferma → Snapshot → Diario → (Night Research se azioni≥1) → Forecast → Sleep → Dawn → Hide.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class EndOfDaySequenceController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private float _nightResearchTransitionDelay = 1f;

        private VisualElement _root;
        private VisualElement _step1, _step2, _step3, _step4, _step5, _step6;
        private Label _snapshotTitle, _snapshotDate, _snapshotVault, _snapshotPh, _activitySummary, _drift, _notes;
        private Label _diarioText, _forecastToday, _forecastTomorrow, _forecastResearch, _dawnEventsText;
        private Button _btnYes, _btnNo, _btnSnapshotConfirm, _btnDiarioContinue, _btnResearchHistorical, _btnResearchBotanical, _btnResearchVault, _btnResearchSkip, _btnSleep, _btnDawnContinue;

        private DayCycleSystem _dayCycleSystem;
        private SaveManager _saveManager;
        private DayActivityLog _dayActivityLog;
        private DiaryStatistics _diaryStatistics;
        private PhSystem _phSystem;
        private GameManager _gameManager;
        private NightEventsGenerator _nightEventsGenerator;
        private WikiUnlockService _wikiUnlockService;

        private bool _bound;
        private bool _awaitingDawn;
        private bool _nightResearchChosen;

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
            ShowStep(6);
        }

        private void TryBind()
        {
            if (_bound) return;
            if (_uiDocument == null) return;
            var currentRoot = _uiDocument.rootVisualElement;
            if (currentRoot == null) return;

            _root = currentRoot;
            _step1 = _root.Q<VisualElement>("eod-step1");
            _step2 = _root.Q<VisualElement>("eod-step2");
            _step3 = _root.Q<VisualElement>("eod-step3");
            _step4 = _root.Q<VisualElement>("eod-step4");
            _step5 = _root.Q<VisualElement>("eod-step5");
            _step6 = _root.Q<VisualElement>("eod-step6");

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
            _dawnEventsText = _root.Q<Label>("eod-dawn-events-text");

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

            if (_btnYes != null) _btnYes.clicked += OnYesClicked;
            if (_btnNo != null) _btnNo.clicked += OnNoClicked;
            if (_btnSnapshotConfirm != null) _btnSnapshotConfirm.clicked += OnSnapshotConfirmClicked;
            if (_btnDiarioContinue != null) _btnDiarioContinue.clicked += OnDiarioContinueClicked;
            if (_btnResearchHistorical != null) _btnResearchHistorical.clicked += OnResearchHistoricalClicked;
            if (_btnResearchBotanical != null) _btnResearchBotanical.clicked += OnResearchBotanicalClicked;
            if (_btnResearchVault != null) _btnResearchVault.clicked += OnResearchVaultClicked;
            if (_btnResearchSkip != null) _btnResearchSkip.clicked += OnResearchSkipClicked;
            if (_btnSleep != null) _btnSleep.clicked += OnSleepClicked;
            if (_btnDawnContinue != null) _btnDawnContinue.clicked += OnDawnContinueClicked;

            _bound = true;
        }

        /// <summary>Sorting order sopra PlantCard (600) e altri pannelli Lab (400) così EoD resta sempre in primo piano.</summary>
        private const int EodSortingOrder = 1000;

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
                }
            }
            TryBind();
            ShowStep(1);
        }

        /// <summary>Chiude la sequenza e torna al vault (es. su NO in Step 1).</summary>
        public void Hide()
        {
            if (_root != null)
                _root.style.display = DisplayStyle.None;
            if (_uiDocument != null && _uiDocument.rootVisualElement != null)
                _uiDocument.rootVisualElement.style.display = DisplayStyle.None;
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
            if (step == 2) PopulateSnapshot();
            if (step == 3) PopulateDiario();
            if (step == 4) { /* optional: disable if no actions */ }
            if (step == 5) PopulateForecast();
        }

        private static void SetStepVisible(VisualElement el, bool visible)
        {
            if (el != null) el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void PopulateSnapshot()
        {
            int day = _dayCycleSystem != null ? _dayCycleSystem.CurrentDay : 0;
            if (_snapshotTitle != null) _snapshotTitle.text = $"SPORAE — Day {day}";
            if (_snapshotDate != null) _snapshotDate.text = "System Date: " + System.DateTime.Now.ToString("dd.MM.yyyy");
            if (_snapshotVault != null) _snapshotVault.text = "Vault Status: Operational";

            string phLine = "Dome pH: —";
            if (_phSystem != null)
                phLine = $"Dome pH: {_phSystem.CurrentPh:F1} ({_phSystem.GetBandName()})";
            if (_snapshotPh != null) _snapshotPh.text = phLine;

            var sb = new StringBuilder();
            if (_diaryStatistics != null)
            {
                int max = _gameManager?.ActionSystem?.MaxActions ?? 4;
                sb.AppendLine($"Actions used: {_diaryStatistics.ActionsSpent}/{max}");
                sb.AppendLine($"CRY earned: {_diaryStatistics.CryEarned}, spent: {_diaryStatistics.CrySpent}");
                if (_gameManager != null)
                    sb.AppendLine($"Balance: {_gameManager.CurrentCRY} CRY");
            }
            if (_dayActivityLog != null)
            {
                var harvests = _dayActivityLog.HarvestsThisDay;
                var domeEntries = _dayActivityLog.DomeEntriesThisDay;
                var labEntries = _dayActivityLog.LabEntriesThisDay;
                foreach (var h in harvests)
                {
                    string potNum = FormatPotNumber(h.PotId);
                    sb.AppendLine($"Hai raccolto {h.Amount} frutti di {h.PlantCode} (L{h.Level}) dal POT {potNum}.");
                }
                var bestPerPot = new Dictionary<string, DayActivityLog.DomeActivityEntry>();
                int actionPriority(string k) => k == "Plant" ? 5 : k == "Water" ? 4 : k == "Light" ? 3 : k == "Fertilize" ? 2 : k == "Pruning" ? 1 : 0;
                foreach (var e in domeEntries)
                {
                    if (string.IsNullOrEmpty(e.PotId)) continue;
                    if (!bestPerPot.TryGetValue(e.PotId, out var existing) || actionPriority(e.ActionKind) > actionPriority(existing.ActionKind))
                        bestPerPot[e.PotId] = e;
                }
                foreach (var e in bestPerPot.Values)
                {
                    string potNum = FormatPotNumber(e.PotId);
                    string line = e.ActionKind == "Plant"
                        ? $"Hai piantato un seme di {(!string.IsNullOrEmpty(e.PlantDisplayName) ? e.PlantDisplayName : e.PlantCode ?? "?")} nel POT {potNum}."
                        : e.ActionKind == "Water"
                        ? $"Hai attivato l'irrigazione nel POT {potNum}."
                        : e.ActionKind == "Light"
                        ? $"Hai modificato le luci LED nel POT {potNum}."
                        : e.ActionKind == "Fertilize"
                        ? $"Hai applicato fertilizzante nel POT {potNum}."
                        : e.ActionKind == "Pruning"
                        ? $"Hai potato la pianta nel POT {potNum}."
                        : $"Azione avviata sul POT {potNum}.";
                    sb.AppendLine(line);
                }
                foreach (var e in labEntries)
                {
                    if (e.LabType == "Extractor" && !string.IsNullOrEmpty(e.InputDescription))
                    {
                        var parts = new List<string>();
                        if (e.SporeOut > 0) parts.Add($"{e.SporeOut} spore");
                        if (e.Cell001Out > 0) parts.Add($"{e.Cell001Out} Cell001");
                        if (e.Cell002Out > 0) parts.Add($"{e.Cell002Out} Cell002");
                        if (e.Cell003Out > 0) parts.Add($"{e.Cell003Out} Cell003");
                        string extracted = parts.Count > 0 ? string.Join(", ", parts) : "estrazione";
                        sb.AppendLine($"Hai estratto {extracted} da {e.InputDescription}.");
                    }
                    else
                        sb.AppendLine($"Hai usato il {e.LabType}.");
                }
            }
            if (_activitySummary != null) _activitySummary.text = sb.Length > 0 ? sb.ToString().TrimEnd() : "No activity recorded.";
            if (_drift != null) _drift.text = "Drift & Consequences: The Dome breathes. (Reputation: —)";
            if (_notes != null) _notes.text = "[NOTES & TAGS] Seed Storage … OK. Research … ON HOLD.";
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
                if (h.Count > 0) sb.AppendLine("Today you harvested. The spores remember.");
                var w = _dayActivityLog.PotIdsWateringTurnedOnThisDay;
                if (w.Count > 0) sb.AppendLine("Water flowed. Life sustained.");
            }
            sb.AppendLine("SPORAE System: Recording completed.");
            sb.AppendLine("Memory integrity: 100%. Next wake in: —");
            sb.Append("Good night, Biologist. Or whoever you are.");
            if (_diarioText != null) _diarioText.text = sb.ToString();
        }

        private void PopulateForecast()
        {
            var sbToday = new StringBuilder("[TODAY]\n");
            if (_diaryStatistics != null)
            {
                int max = _gameManager?.ActionSystem?.MaxActions ?? 4;
                sbToday.AppendLine($"Actions Used: {_diaryStatistics.ActionsSpent} / {max}");
                sbToday.AppendLine($"CRY Gained: {_diaryStatistics.CryEarned}, Spent: {_diaryStatistics.CrySpent}");
            }
            if (_phSystem != null)
                sbToday.AppendLine($"pH: {_phSystem.CurrentPh:F1} ({_phSystem.GetBandName()})");
            sbToday.Append("Reputations: —");
            if (_forecastToday != null) _forecastToday.text = sbToday.ToString();

            var sbTomorrow = new StringBuilder("[TOMORROW FORECAST]\n");
            sbTomorrow.AppendLine("Actions Available: 4");
            sbTomorrow.AppendLine("Predicted pH Drift: —");
            sbTomorrow.AppendLine("Environmental Risks: —");
            sbTomorrow.Append("Missions Active: —");
            if (_forecastTomorrow != null) _forecastTomorrow.text = sbTomorrow.ToString();

            if (_forecastResearch != null)
                _forecastResearch.text = _nightResearchChosen ? "Research Complete: → New lore fragment unlocked." : "";
        }

        private void PopulateDawn(int newDay)
        {
            var title = _root.Q<Label>("eod-dawn-title");
            if (title != null) title.text = "DAWN SUMMARY";
            var sub = _root.Q<Label>("eod-dawn-subtitle");
            if (sub != null) sub.text = $"DAY {newDay} – OVERNIGHT CHANGES";

            var events = _nightEventsGenerator != null ? _nightEventsGenerator.Generate(newDay) : new List<string> { "Night passed." };
            if (_dawnEventsText != null)
                _dawnEventsText.text = events.Count > 0 ? string.Join("\n", events) : "Night passed.";
        }

        private void OnYesClicked()
        {
            if (_saveManager != null)
                _saveManager.SaveGame("default");
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
            _dayCycleSystem.EndDay();
        }

        private void OnDawnContinueClicked() => Hide();

        private void OnDestroy()
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
    }
}
