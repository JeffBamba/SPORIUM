using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using _Project.Sporae.Core;
using _Project.Systems.FoodRoom;
using Sporae.Core;
using Sporae.DevTools;
using Sporae.Dome;
using Sporae.Dome.PotSystem.Growth;
using Sporae.UI.UIToolkit.HUD;
using Sporae.UI.UIToolkit.FoodRoom;
using Sporae.UI.UIToolkit.NotificationsFoundation;

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

        /// <summary>Sorting order sopra tutti i pannelli (Food 1000, PlantCard 600, Lab 400) così EoD resta sempre in primo piano e non compete con Kitchen/Food.</summary>
        private const int EodSortingOrder = 2000;

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
                if (_eodDayFrom != null) _eodDayFrom.text = $"DAY {_dayBeforeTransition:D2}";
                if (_eodDayTo != null) _eodDayTo.text = $"→ DAY {_dayBeforeTransition + 1:D2}";
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
                    sb.AppendLine("Harvested: " + string.Join(", ", harvestParts) + ".");
                }

                var waterPots = new List<string>();
                foreach (var e in domeEntries)
                {
                    if (e.ActionKind == "Water" && !string.IsNullOrEmpty(e.PotId))
                        waterPots.Add("Pot " + FormatPotNumber(e.PotId));
                }
                if (waterPots.Count > 0)
                    sb.AppendLine("Watered " + string.Join(", ", waterPots.Distinct()) + ".");

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
                    string line = e.ActionKind == "Plant"
                        ? $"Hai piantato un seme di {(!string.IsNullOrEmpty(e.PlantDisplayName) ? e.PlantDisplayName : e.PlantCode ?? "?")} nel POT {potNum}."
                        : e.ActionKind == "Light"
                        ? $"Hai modificato le luci LED nel POT {potNum}."
                        : e.ActionKind == "Fertilize"
                        ? $"Hai applicato fertilizzante nel POT {potNum}."
                        : e.ActionKind == "Pruning"
                        ? $"Hai potato la pianta nel POT {potNum}."
                        : e.ActionKind == "Started"
                        ? $"Azione avviata sul POT {potNum}."
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
                        string extracted = parts.Count > 0 ? string.Join(", ", parts) : "estrazione";
                        sb.AppendLine($"Hai estratto {extracted} da {e.InputDescription}.");
                    }
                    else if (e.LabType == "Fusion")
                    {
                        string sporeDesc = !string.IsNullOrEmpty(e.InputDescription) ? e.InputDescription : "due Spore";
                        sb.AppendLine($"Hai completato la fusione di due Spore ({sporeDesc}) nella Stazione di Fusione.");
                    }
                    else
                        sb.AppendLine($"Hai usato il {e.LabType}.");
                }
            }

            sb.AppendLine();
            if (_dayCycleController != null)
            {
                var conditions = _dayCycleController.GetActiveConditionsForReport();
                if (conditions.Count > 0)
                {
                    sb.AppendLine("Active conditions:");
                    foreach (var c in conditions)
                    {
                        string severity = c.IsInfested ? "Infestation" : (c.MoldRiskLevel >= 2 ? "Severe Mold Risk" : "Light Mold Risk");
                        sb.AppendLine($"  Pot {FormatPotNumber(c.PotId)}: {severity} (mold risk if untreated tomorrow).");
                    }
                }
            }

            string activityStr = sb.Length > 0 ? sb.ToString().TrimEnd() : "No activity recorded.";
            if (_activitySummary != null)
            {
                _activitySummary.enableRichText = true;
                _activitySummary.text = "";
                StartCoroutine(Typewriter(_activitySummary, activityStr));
            }

            float predictedPhDrift = _dayCycleController != null ? _dayCycleController.GetPredictedPhDriftForNextDay() : float.NaN;
            string phDriftStr = float.IsNaN(predictedPhDrift) ? "—" : predictedPhDrift.ToString("+#0.0;-#0.0;0", System.Globalization.CultureInfo.InvariantCulture);
            string phTrendLine = _phSystem != null
                ? $"pH trend: {_phSystem.CurrentPh:F1} ({_phSystem.GetBandName()}), drift {phDriftStr}"
                : "pH trend: —";

            var driftSb = new StringBuilder();
            driftSb.AppendLine("Drift & Consequences:");
            driftSb.AppendLine(phTrendLine);
            driftSb.AppendLine("The Dome breathes.");
            driftSb.AppendLine(PlaceholderOpen + "Consequence: pH stabilized / environmental note [placeholder: collegare a eventi]" + PlaceholderClose);
            if (_drift != null)
            {
                _drift.enableRichText = true;
                _drift.text = driftSb.ToString().TrimEnd();
            }

            var notesSb = new StringBuilder();
            notesSb.AppendLine("[NOTES & TAGS]");
            notesSb.AppendLine("Inventory ......... " + BuildInventorySummary());
            notesSb.AppendLine("Seed Storage ...... " + BuildSeedStorageSummary());
            notesSb.AppendLine("Research .......... ON HOLD");
            notesSb.AppendLine();
            notesSb.AppendLine("Kitchen Food ...... " + BuildKitchenFoodSummary());
            notesSb.AppendLine("Potable Water ..... " + BuildPotableWaterSummary());
            notesSb.AppendLine();
            notesSb.Append("Warning ............ ");
            if (_dayCycleController != null)
            {
                var moldPots = _dayCycleController.GetActiveConditionsForReport().Select(c => FormatPotNumber(c.PotId)).Distinct().ToList();
                if (moldPots.Count > 0)
                    notesSb.Append($"Mold risk in Pot {string.Join(", ", moldPots)}.");
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
                    parts.Add($"{spores} spores (Pure: {pure}, Evil: {evil}, Standard: {standard})");
                else
                    parts.Add($"{spores} spores");
            }
            if (seeds > 0) parts.Add($"{seeds} seeds");
            if (reagents > 0) parts.Add($"{reagents} reagents");
            return parts.Count > 0 ? string.Join(", ", parts) + "." : "—";
        }

        private string BuildSeedStorageSummary()
        {
            var inv = _gameManager?.PlayerInventory;
            if (inv == null) return "—";
            var seedParts = new List<string>();
            int preSeed = 0;
            var seedByTypeId = new Dictionary<string, int>();
            var pdb = PlantDatabase.Instance;
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
            if (preSeed > 0) seedParts.Add($"{preSeed} Pre-Seed");
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
                    string typeName = slot.Type == FoodProductionType.Vegetable ? "Vegetable" : slot.Type == FoodProductionType.Fungus ? "Fungal" : slot.Type == FoodProductionType.Meat ? "Meat" : "Food";
                    parts.Add($"{typeName} in progress ({slot.DaysRemaining} day(s) left)");
                }
                else if (slot.State == SlotState.Ready)
                {
                    string typeName = slot.Type == FoodProductionType.Vegetable ? "Vegetable" : slot.Type == FoodProductionType.Fungus ? "Fungal" : slot.Type == FoodProductionType.Meat ? "Meat" : "Food";
                    parts.Add($"{typeName} ready for harvest");
                }
            }
            return parts.Count > 0 ? string.Join("; ", parts) + "." : "No food in production.";
        }

        private string BuildPotableWaterSummary()
        {
            var foodRoom = _gameManager?.FoodRoomSystem;
            if (foodRoom == null) return "—";
            var water = foodRoom.WaterSlot;
            if (!water.IsActive)
                return "No purification in progress.";
            if (water.PotableWaterOutput > 0)
                return $"{water.PotableWaterOutput} unit(s) potable water ready for collection.";
            return "Purification in progress.";
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
            string full = sb.ToString();
            if (_diarioText != null)
            {
                _diarioText.text = "";
                StartCoroutine(Typewriter(_diarioText, full));
            }
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
            string textToday = sbToday.ToString();

            float predictedPhDrift = _dayCycleController != null ? _dayCycleController.GetPredictedPhDriftForNextDay() : float.NaN;
            string predictedPhDriftStr = float.IsNaN(predictedPhDrift) ? "—" : predictedPhDrift.ToString("+#0.0;-#0.0;0", System.Globalization.CultureInfo.InvariantCulture);

            var sbTomorrow = new StringBuilder("[TOMORROW FORECAST]\n");
            sbTomorrow.AppendLine("Actions Available: 4");
            sbTomorrow.AppendLine($"Predicted pH Drift: {predictedPhDriftStr}");
            sbTomorrow.AppendLine("Environmental Risks: —");
            sbTomorrow.Append("Missions Active: —");
            string textTomorrow = sbTomorrow.ToString();

            string textResearch = _nightResearchChosen ? "Research Complete: → New lore fragment unlocked." : "";
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
            if (title != null) title.text = "DAWN SUMMARY";
            var sub = _root.Q<Label>("eod-dawn-subtitle");
            if (sub != null) sub.text = $"DAY {newDay} – OVERNIGHT CHANGES";

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
            string phTrend = float.IsNaN(phDrift) ? "" : (phDrift < 0 ? " (acidic trend)" : " (alkaline trend)");

            SetDawnRow("eod-dawn-text-mutation", float.IsNaN(mutation) ? "Indice di Mutazione: —" : $"Indice di Mutazione: {mutation:P0}");
            SetDawnRow("eod-dawn-text-ph", $"pH Drift: {phDriftStr}{phTrend}");
            SetDawnRow("eod-dawn-text-condensation", float.IsNaN(condensation) ? "Condensation: —" : $"Condensation: {condensation:F0}%");
            SetDawnRow("eod-dawn-text-grate", $"G-rate: +{grate}");
            SetDawnRow("eod-dawn-text-cry", $"CRY Balance (forecast fine giorno): {cryForecast} (costi fissi only)");

            var tooltips = new Dictionary<string, (string title, string desc, string tip)>
            {
                ["mutation"] = ("[MUTATION INDEX]", "The mutation index reflects genetic drift in the Dome. Overnight conditions can shift mutation probability.", "TIP: Monitor high mutation zones and use stabilizers in the Lab if needed."),
                ["ph"] = ("[PH DRIFT DETECTED]", float.IsNaN(phDrift) ? "pH trend is monitored overnight. Drift affects plant growth and mutation probability." : "The Dome environment has shifted toward " + (phDrift < 0 ? "acidity" : "alkalinity") + ". This affects plant growth patterns and mutation probability.", "TIP: Use alkaline substrates or activate pH stabilizers in the Lab to counteract drift."),
                ["condensation"] = ("[CONDENSATION LEVEL CHANGE]", "Condensation has accumulated overnight. Excess humidity can encourage mold growth but benefits water-dependent species.", "TIP: Monitor plants for mold signs. Consider harvesting condensation-sensitive species soon."),
                ["grate"] = ("[G-RATE UPDATE]", "Daily growth rate contribution from systems. Affects resource accumulation and plant development.", "TIP: Maximize G-rate through balanced Dome and Lab activities."),
                ["cry"] = ("[CRY BALANCE FORECAST]", "Projected CRY at end of day considering fixed costs only (e.g. power). Does not include variable actions.", "TIP: Ensure sufficient CRY before night to cover fixed costs.")
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
                    if (_dawnTooltipTip != null) _dawnTooltipTip.text = t.tip.StartsWith("TIP:", System.StringComparison.OrdinalIgnoreCase) ? t.tip : "TIP: " + t.tip;
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
