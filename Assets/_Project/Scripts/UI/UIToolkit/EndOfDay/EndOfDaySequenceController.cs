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

        private const string PlaceholderOpen = "<color=#FFA500>";
        private const string PlaceholderClose = "</color>";

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

            var sb = new StringBuilder();
            if (_diaryStatistics != null)
            {
                int max = _gameManager?.ActionSystem?.MaxActions ?? 5;
                sb.AppendLine(LocalizationManager.GetString("eod.snapshot_actions", new Dictionary<string, string>
                {
                    ["used"] = _diaryStatistics.ActionsSpent.ToString(),
                    ["max"] = max.ToString()
                }));
                sb.AppendLine(LocalizationManager.GetString("eod.snapshot_cry", new Dictionary<string, string>
                {
                    ["earned"] = _diaryStatistics.CryEarned.ToString(),
                    ["spent"] = _diaryStatistics.CrySpent.ToString()
                }));
                if (_gameManager != null)
                    sb.AppendLine(LocalizationManager.GetString("eod.snapshot_balance", new Dictionary<string, string> { ["cry"] = _gameManager.CurrentCRY.ToString() }));
            }
            if (_dayActivityLog != null)
            {
                var harvests = _dayActivityLog.HarvestsThisDay;
                var domeEntries = _dayActivityLog.DomeEntriesThisDay;
                var labEntries = _dayActivityLog.LabEntriesThisDay;

                if (harvests.Count > 0)
                {
                    var byPlant = new Dictionary<string, (int total, int level)>();
                    foreach (var h in harvests)
                    {
                        string key = $"{h.PlantCode}|{h.Level}";
                        if (!byPlant.TryGetValue(key, out var t))
                            t = (0, h.Level);
                        byPlant[key] = (t.total + h.Amount, h.Level);
                    }
                    var harvestParts = new List<string>();
                    foreach (var kv in byPlant.OrderBy(x => x.Key))
                    {
                        string plantCode = kv.Key.Split('|')[0];
                        int level = kv.Value.level;
                        int amount = kv.Value.total;
                        string displayName = PlantDatabase.Instance?.GetPlantDataByCode(plantCode)?.name ?? plantCode;
                        harvestParts.Add($"{amount} {displayName} (L{level})");
                    }
                    sb.AppendLine(LocalizationManager.GetString("eod.snapshot_harvest", new Dictionary<string, string> { ["list"] = string.Join(", ", harvestParts) }));
                }

                var waterPots = new List<string>();
                foreach (var e in domeEntries)
                {
                    if (e.ActionKind == "Water" && !string.IsNullOrEmpty(e.PotId))
                        waterPots.Add(LocalizationManager.GetString("eod.snapshot_pot_prefix", new Dictionary<string, string> { ["n"] = FormatPotNumber(e.PotId) }));
                }
                if (waterPots.Count > 0)
                    sb.AppendLine(LocalizationManager.GetString("eod.snapshot_water", new Dictionary<string, string> { ["list"] = string.Join(", ", waterPots.Distinct()) }));

                var bestPerPot = new Dictionary<string, DayActivityLog.DomeActivityEntry>();
                int actionPriority(string k) => k == "Plant" ? 5 : k == "Water" ? 4 : k == "Light" ? 3 : k == "Fertilize" ? 2 : k == "Pruning" ? 1 : 0;
                foreach (var e in domeEntries)
                {
                    if (string.IsNullOrEmpty(e.PotId)) continue;
                    if (e.ActionKind == "Water") continue;
                    if (!bestPerPot.TryGetValue(e.PotId, out var existing) || actionPriority(e.ActionKind) > actionPriority(existing.ActionKind))
                        bestPerPot[e.PotId] = e;
                }
                foreach (var e in bestPerPot.Values)
                {
                    string potNum = FormatPotNumber(e.PotId);
                    string plantName = !string.IsNullOrEmpty(e.PlantDisplayName) ? e.PlantDisplayName : e.PlantCode ?? "?";
                    string line = e.ActionKind == "Plant"
                        ? LocalizationManager.GetString("eod.snapshot_plant", new Dictionary<string, string> { ["plant"] = plantName, ["pot"] = potNum })
                        : e.ActionKind == "Light"
                        ? LocalizationManager.GetString("eod.snapshot_light", new Dictionary<string, string> { ["pot"] = potNum })
                        : e.ActionKind == "Fertilize"
                        ? LocalizationManager.GetString("eod.snapshot_fertilize", new Dictionary<string, string> { ["pot"] = potNum })
                        : e.ActionKind == "Pruning"
                        ? LocalizationManager.GetString("eod.snapshot_prune", new Dictionary<string, string> { ["pot"] = potNum })
                        : e.ActionKind == "Started"
                        ? LocalizationManager.GetString("eod.snapshot_started", new Dictionary<string, string> { ["pot"] = potNum })
                        : null;
                    if (line != null)
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
                        string extracted = parts.Count > 0 ? string.Join(", ", parts) : LocalizationManager.GetString("eod.lab_extract_fallback");
                        sb.AppendLine(LocalizationManager.GetString("eod.lab_extractor", new Dictionary<string, string>
                        {
                            ["out"] = extracted,
                            ["input"] = e.InputDescription
                        }));
                    }
                    else if (e.LabType == "Fusion")
                    {
                        string sporeDesc = !string.IsNullOrEmpty(e.InputDescription) ? e.InputDescription : LocalizationManager.GetString("eod.lab_two_spores");
                        sb.AppendLine(LocalizationManager.GetString("eod.lab_fusion", new Dictionary<string, string> { ["desc"] = sporeDesc }));
                    }
                    else
                        sb.AppendLine(LocalizationManager.GetString("eod.lab_generic", new Dictionary<string, string> { ["lab"] = e.LabType }));
                }
            }

            sb.AppendLine();
            if (_dayCycleController != null)
            {
                var conditions = _dayCycleController.GetActiveConditionsForReport();
                if (conditions.Count > 0)
                {
                    sb.AppendLine(LocalizationManager.GetString("eod.conditions_header"));
                    foreach (var c in conditions)
                    {
                        string severity = c.IsInfested
                            ? LocalizationManager.GetString("eod.sev_infest")
                            : (c.MoldRiskLevel >= 2
                                ? LocalizationManager.GetString("eod.sev_mold_high")
                                : LocalizationManager.GetString("eod.sev_mold_low"));
                        sb.AppendLine(LocalizationManager.GetString("eod.condition_line", new Dictionary<string, string>
                        {
                            ["pot"] = FormatPotNumber(c.PotId),
                            ["sev"] = severity
                        }));
                    }
                }
            }

            string activityStr = sb.Length > 0 ? sb.ToString().TrimEnd() : LocalizationManager.GetString("eod.activity_none");
            if (_activitySummary != null)
            {
                _activitySummary.enableRichText = true;
                _activitySummary.text = "";
                StartCoroutine(Typewriter(_activitySummary, activityStr));
            }

            float predictedPhDrift = _dayCycleController != null ? _dayCycleController.GetPredictedPhDriftForNextDay() : float.NaN;
            string phDriftStr = float.IsNaN(predictedPhDrift) ? "—" : predictedPhDrift.ToString("+#0.0;-#0.0;0", System.Globalization.CultureInfo.InvariantCulture);
            string phTrendLine = _phSystem != null
                ? LocalizationManager.GetString("eod.drift_ph", new Dictionary<string, string>
                {
                    ["ph"] = _phSystem.CurrentPh.ToString("F1"),
                    ["band"] = _phSystem.GetBandName(),
                    ["drift"] = phDriftStr
                })
                : LocalizationManager.GetString("eod.drift_ph_empty");

            var driftSb = new StringBuilder();
            driftSb.AppendLine(LocalizationManager.GetString("eod.drift_title"));
            driftSb.AppendLine(phTrendLine);
            driftSb.AppendLine(LocalizationManager.GetString("eod.drift_breathe"));
            driftSb.AppendLine(PlaceholderOpen + LocalizationManager.GetString("eod.drift_placeholder") + PlaceholderClose);
            if (_drift != null)
            {
                _drift.enableRichText = true;
                _drift.text = driftSb.ToString().TrimEnd();
            }

            var notesSb = new StringBuilder();
            notesSb.AppendLine(LocalizationManager.GetString("eod.notes_header"));
            notesSb.AppendLine(LocalizationManager.GetString("eod.notes_inventory", new Dictionary<string, string> { ["v"] = BuildInventorySummary() }));
            notesSb.AppendLine(LocalizationManager.GetString("eod.notes_seed", new Dictionary<string, string> { ["v"] = BuildSeedStorageSummary() }));
            notesSb.AppendLine(LocalizationManager.GetString("eod.notes_research"));
            notesSb.AppendLine();
            notesSb.AppendLine(LocalizationManager.GetString("eod.notes_food", new Dictionary<string, string> { ["v"] = BuildKitchenFoodSummary() }));
            notesSb.AppendLine(LocalizationManager.GetString("eod.notes_water", new Dictionary<string, string> { ["v"] = BuildPotableWaterSummary() }));
            notesSb.AppendLine();
            notesSb.Append(LocalizationManager.GetString("eod.notes_warning_label"));
            if (_dayCycleController != null)
            {
                var moldPots = _dayCycleController.GetActiveConditionsForReport().Select(c => FormatPotNumber(c.PotId)).Distinct().ToList();
                if (moldPots.Count > 0)
                    notesSb.Append(LocalizationManager.GetString("eod.notes_mold", new Dictionary<string, string> { ["list"] = string.Join(", ", moldPots) }));
                else
                    notesSb.Append("—");
            }
            else
                notesSb.Append("—");
            if (_notes != null)
            {
                _notes.enableRichText = true;
                _notes.text = notesSb.ToString();
            }
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
