using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using _Project;
using _Project.Sporae.Core;
using Sporae.Core;
using Sporae.Dome.PotSystem.Growth;
using Sporae.UI.UIToolkit.NotificationsFoundation;
using Sporae.UI.UIToolkit.PlayerInventory;

namespace Sporae.UI.UIToolkit.Lab
{
    [RequireComponent(typeof(UIDocument))]
    public class LabIncubatorPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument _uiDocument;
        [Tooltip("Componente unico inventario (picker). Se non assegnato, viene cercato in scena.")]
        [SerializeField] private PlayerInventoryPanelController _playerInventoryPanel;

        [Header("Config")]
        [SerializeField] private int _costAction = 1;
        [SerializeField] private string _outputSeedTypeId = "seed-001";

        private VisualElement _root;
        private VisualElement _overlay;
        private Label _preseedText;
        private Label _outputText;
        private Label _reagentText;
        private VisualElement _outputSlotRow;
        private VisualElement _xConfigRow;
        private DropdownField _familyDropdown;
        private DropdownField _trait1Dropdown;
        private DropdownField _trait2Dropdown;
        private DropdownField _nameDropdown;
        private VisualElement _nameCustomRow;
        private TextField _nameCustomField;
        private Button _btnSelectPreseed;
        private Button _btnSelectReagent;
        private Button _btnClearReagent;
        private Button _btnAvvia;
        private Button _btnRitira;
        private Button _btnClose;
        private VisualElement _outputTooltip;
        private Label _outputTooltipText;

        private GameManager _gameManager;
        private DayCycleSystem _dayCycleSystem;
        /// <summary>Reagente scelto dall'inventario (Items.ReagentX, Items.ReagentY) o null se nessuno.</summary>
        private string _reagentTypeId;
        private readonly List<Item> _outputSeeds = new();
        /// <summary>0 = nessuno, 1 = giorno 1, 2 = giorno 2 (al prossimo day change si completa).</summary>
        private int _incubationDay;
        private Item _incubatingPreSeed;
        private string _selectedFamilyX;
        private string _selectedTrait1X;
        private string _selectedTrait2X;
        private string _selectedNameX;
        private bool _uiBound;

        private const string IncubatorProgressToastKey = "incubator-progress";
        private const string IncubatorDoneToastKey = "incubator-done";

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null)
            {
                if (_uiDocument.panelSettings == null)
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
                }
                _uiDocument.sortingOrder = 400;
            }

            _root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (_root != null)
                TryBindUI();
        }

        private void TryBindUI()
        {
            if (_uiDocument != null)
            {
                var currentRoot = _uiDocument.rootVisualElement;
                if (currentRoot != null && currentRoot != _root)
                {
                    _root = currentRoot;
                    _outputTooltip = null;
                    _outputTooltipText = null;
                    _uiBound = false;
                }
            }
            if (_uiBound) return;
            if (_root == null && _uiDocument != null)
                _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

            _overlay = _root.Q<VisualElement>("lab-inc-overlay");
            _preseedText = _root.Q<Label>("lab-inc-preseed-text");
            _outputText = _root.Q<Label>("lab-inc-output-text");
            _reagentText = _root.Q<Label>("lab-inc-reagent-text");
            _outputSlotRow = _root.Q<VisualElement>("lab-inc-output-row");
            _xConfigRow = _root.Q<VisualElement>("lab-inc-x-config-row");
            _familyDropdown = _root.Q<DropdownField>("lab-inc-x-family");
            _trait1Dropdown = _root.Q<DropdownField>("lab-inc-x-trait1");
            _trait2Dropdown = _root.Q<DropdownField>("lab-inc-x-trait2");
            _nameDropdown = _root.Q<DropdownField>("lab-inc-x-name");
            _nameCustomRow = _root.Q<VisualElement>("lab-inc-x-name-custom-row");
            _nameCustomField = _root.Q<TextField>("lab-inc-x-name-custom");
            _btnSelectPreseed = _root.Q<Button>("btn-select-preseed");
            _btnSelectReagent = _root.Q<Button>("btn-select-reagent");
            _btnClearReagent = _root.Q<Button>("btn-clear-reagent");
            _btnAvvia = _root.Q<Button>("btn-avvia");
            _btnRitira = _root.Q<Button>("btn-ritira");
            _btnClose = _root.Q<Button>("btn-close");
            if (_playerInventoryPanel == null)
                _playerInventoryPanel = FindObjectOfType<PlayerInventoryPanelController>();

            EnsureOutputTooltip();

            if (_btnClose != null)
            {
                foreach (var child in _btnClose.Children())
                    child.pickingMode = PickingMode.Ignore;
                _btnClose.clicked += OnCloseClicked;
                _btnClose.RegisterCallback<ClickEvent>(evt => { OnCloseClicked(); evt.StopPropagation(); }, TrickleDown.TrickleDown);
            }
            if (_btnAvvia != null) _btnAvvia.clicked += OnAvviaClicked;
            if (_btnRitira != null) _btnRitira.clicked += OnRitiraClicked;
            if (_btnSelectPreseed != null) _btnSelectPreseed.clicked += OnSelectPreseedClicked;
            if (_btnSelectReagent != null) _btnSelectReagent.clicked += OnSelectReagentClicked;
            if (_btnClearReagent != null) _btnClearReagent.clicked += () =>
            {
                _reagentTypeId = null;
                _selectedFamilyX = null;
                _selectedTrait1X = null;
                _selectedTrait2X = null;
                _selectedNameX = null;
                RefreshDisplay();
            };
            if (_familyDropdown != null)
                _familyDropdown.RegisterValueChangedCallback(evt => _selectedFamilyX = evt.newValue);
            if (_trait1Dropdown != null)
                _trait1Dropdown.RegisterValueChangedCallback(evt => _selectedTrait1X = evt.newValue);
            if (_trait2Dropdown != null)
                _trait2Dropdown.RegisterValueChangedCallback(evt => _selectedTrait2X = evt.newValue);
            if (_nameDropdown != null)
                _nameDropdown.RegisterValueChangedCallback(evt => OnNameDropdownChanged(evt.newValue));
            if (_nameCustomField != null)
                _nameCustomField.RegisterValueChangedCallback(evt => _selectedNameX = string.IsNullOrWhiteSpace(evt.newValue) ? null : evt.newValue.Trim());
            _uiBound = true;
        }

        private void OnNameDropdownChanged(string newValue)
        {
            bool isCustom = string.Equals(newValue, "Nome personalizzato", System.StringComparison.OrdinalIgnoreCase);
            if (_nameCustomRow != null)
                _nameCustomRow.style.display = isCustom ? DisplayStyle.Flex : DisplayStyle.None;
            _selectedNameX = isCustom ? (_nameCustomField?.value?.Trim()) : newValue;
        }

        private void OnCloseClicked() => Hide();

        private void EnsureOutputTooltip()
        {
            if (_outputTooltip != null || _root == null) return;
            _outputTooltip = new VisualElement();
            _outputTooltip.name = "lab-inc-output-tooltip";
            _outputTooltip.style.position = Position.Absolute;
            _outputTooltip.style.display = DisplayStyle.None;
            _outputTooltip.style.backgroundColor = new Color(0.05f, 0.07f, 0.09f, 0.96f);
            _outputTooltip.style.borderTopWidth = _outputTooltip.style.borderRightWidth = _outputTooltip.style.borderBottomWidth = _outputTooltip.style.borderLeftWidth = 2f;
            _outputTooltip.style.borderTopColor = _outputTooltip.style.borderRightColor = _outputTooltip.style.borderBottomColor = _outputTooltip.style.borderLeftColor = new Color(0.5f, 0.8f, 0.5f, 0.9f);
            _outputTooltip.style.paddingTop = _outputTooltip.style.paddingRight = _outputTooltip.style.paddingBottom = _outputTooltip.style.paddingLeft = 10f;
            _outputTooltip.style.minWidth = 280f;
            _outputTooltip.style.maxWidth = 360f;
            _outputTooltip.pickingMode = PickingMode.Ignore;
            _outputTooltipText = new Label();
            _outputTooltipText.enableRichText = true;
            _outputTooltipText.style.whiteSpace = WhiteSpace.Normal;
            _outputTooltipText.style.color = new Color(0.95f, 0.96f, 0.98f, 1f);
            _outputTooltipText.style.fontSize = 12f;
            _outputTooltip.Add(_outputTooltipText);
            _root.Add(_outputTooltip);

            if (_outputSlotRow != null)
            {
                _outputSlotRow.RegisterCallback<MouseEnterEvent>(OnOutputSlotHoverEnter);
                _outputSlotRow.RegisterCallback<MouseLeaveEvent>(OnOutputSlotHoverExit);
                _outputSlotRow.RegisterCallback<MouseMoveEvent>(OnOutputSlotHoverMove);
            }
        }

        private void OnOutputSlotHoverEnter(MouseEnterEvent evt)
        {
            if (_outputTooltip == null || _outputTooltipText == null || _outputSeeds.Count <= 0) return;
            _outputTooltipText.text = BuildOutputTooltipText();
            _outputTooltip.style.display = DisplayStyle.Flex;
            _outputTooltip.BringToFront();
            PositionOutputTooltipAtMouse(evt.mousePosition);
        }

        private void OnOutputSlotHoverExit(MouseLeaveEvent evt)
        {
            if (_outputTooltip != null)
                _outputTooltip.style.display = DisplayStyle.None;
        }

        private void OnOutputSlotHoverMove(MouseMoveEvent evt)
        {
            if (_outputTooltip == null || _outputTooltip.style.display != DisplayStyle.Flex) return;
            PositionOutputTooltipAtMouse(evt.mousePosition);
        }

        private void PositionOutputTooltipAtMouse(Vector2 mousePosPanel)
        {
            if (_outputTooltip == null || _root == null) return;
            float x = mousePosPanel.x + 16f;
            float y = mousePosPanel.y + 12f;
            const float tw = 330f;
            float th = _outputTooltip.resolvedStyle.height;
            var bounds = _root.contentRect;
            if (x + tw > bounds.width) x = mousePosPanel.x - tw - 8f;
            if (y + th > bounds.height) y = mousePosPanel.y - th - 8f;
            if (y < 0f) y = 8f;
            if (x < 0f) x = 8f;
            _outputTooltip.style.left = x;
            _outputTooltip.style.top = y;
        }

        private string BuildOutputTooltipText()
        {
            var first = _outputSeeds[0];
            string tratti = ExtractorTooltipTexts.GeneticTypeToTrattiLabel(first.GeneticTypeValue);
            if (string.IsNullOrEmpty(tratti) || tratti == "—") tratti = "Stabili";
            string family = string.IsNullOrWhiteSpace(first.FamilyMetadata) ? "STANDARD" : first.FamilyMetadata;
            string nameAndQty = $"{PlayerInventoryPanelController.GetItemDisplayName(first.TypeId, first)} x{_outputSeeds.Count}";
            var lines = new List<string>
            {
                ExtractorTooltipTexts.WrapValue(nameAndQty),
                $"Tratti: {ExtractorTooltipTexts.WrapValue(tratti)}",
                $"Famiglia: {ExtractorTooltipTexts.WrapValue(family)}"
            };
            if (!string.IsNullOrWhiteSpace(first.SelectedTraitsCsv))
                lines.Add($"Tratti selezionati: {ExtractorTooltipTexts.WrapValue(first.SelectedTraitsCsv)}");
            if (first.TraitPowerPercent > 0)
                lines.Add($"Potenza tratti: {ExtractorTooltipTexts.WrapValue(first.TraitPowerPercent.ToString() + "%")}");
            if (!string.IsNullOrWhiteSpace(first.ReagentUsedMetadata))
                lines.Add($"Reagente: {ExtractorTooltipTexts.WrapValue(first.ReagentUsedMetadata)}");
            return string.Join("\n", lines);
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy) return;
            if (_incubationDay != 1 && _incubationDay != 2) return;
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation == null || !foundation.Enabled) return;
            foundation.UpsertToast(IncubatorProgressToastKey, "LAB-INC-PROGRESS", new NotificationPayload().With("day", _incubationDay.ToString()));
        }

        private void Start()
        {
            _gameManager = ServiceContainer.Instance?.Get<GameManager>();
            _dayCycleSystem = ServiceContainer.Instance?.Get<DayCycleSystem>();
            if (_dayCycleSystem != null)
                _dayCycleSystem.OnDayChanged += HandleDayChanged;
            Hide();
        }

        private void OnDestroy()
        {
            if (_dayCycleSystem != null)
                _dayCycleSystem.OnDayChanged -= HandleDayChanged;
        }

        public void Show()
        {
            gameObject.SetActive(true);
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
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.None;
                _overlay.pickingMode = PickingMode.Ignore;
            }
            if (_root != null)
                _root.pickingMode = PickingMode.Ignore;
            gameObject.SetActive(false);
        }

        private void OnSelectPreseedClicked()
        {
            if (_playerInventoryPanel == null)
            {
                _playerInventoryPanel = FindObjectOfType<PlayerInventoryPanelController>();
                if (_playerInventoryPanel == null) return;
            }
            var allowed = IncubatorAllowedTypes();
            _playerInventoryPanel.ShowAsPicker(
                allowed,
                "Seleziona Pre-Seed per l'Incubatore",
                typeId =>
                {
                    // L'Incubatore usa direttamente l'inventario del giocatore; la selezione serve solo a confermare/disporre il picker
                    RefreshDisplay();
                },
                () => { }
            );
        }

        private static HashSet<string> IncubatorAllowedTypes()
        {
            return new HashSet<string> { Items.PreSeed };
        }

        private static HashSet<string> IncubatorReagentTypes()
        {
            return new HashSet<string> { Items.ReagentX, Items.ReagentY };
        }

        private void OnSelectReagentClicked()
        {
            if (_playerInventoryPanel == null)
            {
                _playerInventoryPanel = FindObjectOfType<PlayerInventoryPanelController>();
                if (_playerInventoryPanel == null) return;
            }
            _playerInventoryPanel.ShowAsPicker(
                IncubatorReagentTypes(),
                "Seleziona Reagente (X o Y) dall'inventario",
                typeId =>
                {
                    _reagentTypeId = typeId;
                    if (_reagentTypeId != Items.ReagentX)
                    {
                        _selectedFamilyX = null;
                        _selectedTrait1X = null;
                        _selectedTrait2X = null;
                    }
                    RefreshDisplay();
                },
                () => { }
            );
        }

        private void HandleDayChanged(int day)
        {
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (_incubationDay == 1)
            {
                _incubationDay = 2;
                if (foundation != null && foundation.Enabled)
                    foundation.UpsertToast(IncubatorProgressToastKey, "LAB-INC-PROGRESS", new NotificationPayload().With("day", "2"));
            }
            else if (_incubationDay == 2)
            {
                _incubationDay = 0;
                var seed = BuildSeedOutputFromIncubation();
                if (seed != null)
                    _outputSeeds.Add(seed);
                _incubatingPreSeed = null;
                if (foundation != null && foundation.Enabled)
                {
                    foundation.RemoveToast(IncubatorProgressToastKey);
                    foundation.UpsertToast(IncubatorDoneToastKey, "LAB-INC-DONE", new NotificationPayload().With("count", _outputSeeds.Count.ToString()));
                }
            }
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            int preseedInInventory = 0;
            if (_gameManager?.PlayerInventory != null)
            {
                var slot = _gameManager.PlayerInventory.Items.FirstOrDefault(s => s.TypeId == Items.PreSeed);
                preseedInInventory = slot?.Quantity ?? 0;
            }
            if (_preseedText != null)
                _preseedText.text = preseedInInventory > 0 ? $"In inventario: Pre-Seed x{preseedInInventory}" : "—";

            if (_reagentText != null)
            {
                if (string.IsNullOrEmpty(_reagentTypeId))
                    _reagentText.text = "—";
                else if (_reagentTypeId == Items.ReagentX)
                    _reagentText.text = "Reagente X";
                else if (_reagentTypeId == Items.ReagentY)
                    _reagentText.text = "Reagente Y";
                else
                    _reagentText.text = _reagentTypeId;
            }

            if (_outputText != null)
                _outputText.text = _outputSeeds.Count > 0
                    ? $"{PlayerInventoryPanelController.GetItemDisplayName(_outputSeeds[0].TypeId, _outputSeeds[0])} x{_outputSeeds.Count}"
                    : "—";

            if (_btnRitira != null)
                _btnRitira.SetEnabled(_outputSeeds.Count > 0);

            RefreshReagentXSelectors();

            bool inProgress = _incubationDay == 1 || _incubationDay == 2;
            bool requireXSetup = RequiresReagentXSetup();
            if (_btnAvvia != null)
            {
                bool canAvvia = !inProgress && preseedInInventory > 0
                    && _gameManager != null && _gameManager.ActionSystem != null && _gameManager.ActionSystem.ActionsLeft >= _costAction;
                if (canAvvia && !string.IsNullOrEmpty(_reagentTypeId))
                    canAvvia = _gameManager.PlayerInventory.Has(_reagentTypeId);
                if (canAvvia && requireXSetup)
                    canAvvia = IsReagentXSelectionValid();
                _btnAvvia.SetEnabled(canAvvia);
            }
            if (_btnClearReagent != null)
                _btnClearReagent.SetEnabled(!string.IsNullOrEmpty(_reagentTypeId));
        }

        private void OnAvviaClicked()
        {
            if (_gameManager?.PlayerInventory == null || !_gameManager.PlayerInventory.Has(Items.PreSeed))
                return;
            if (_gameManager.ActionSystem == null || _gameManager.ActionSystem.ActionsLeft < _costAction)
                return;
            if (_incubationDay != 0)
                return;
            if (!string.IsNullOrEmpty(_reagentTypeId) && !_gameManager.PlayerInventory.Has(_reagentTypeId))
                return;
            if (RequiresReagentXSetup() && !IsReagentXSelectionValid())
                return;

            if (!_gameManager.TrySpendAction(_costAction))
                return;

            var dayActivityLog = ServiceContainer.Instance.Get<DayActivityLog>(suppressWarning: true);
            if (dayActivityLog != null)
                dayActivityLog.RecordLabAction("Incubator");
            if (!_gameManager.PlayerInventory.TryRemoveFirst(Items.PreSeed, out _incubatingPreSeed))
                return;
            if (!string.IsNullOrEmpty(_reagentTypeId))
                _gameManager.PlayerInventory.Consume(_reagentTypeId, 1);

            _incubationDay = 1;
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
                foundation.UpsertToast(IncubatorProgressToastKey, "LAB-INC-PROGRESS", new NotificationPayload().With("day", "1"));
            RefreshDisplay();
            Hide();
        }

        private void OnRitiraClicked()
        {
            if (_outputSeeds.Count <= 0 || _gameManager?.PlayerInventory == null)
                return;

            int count = _outputSeeds.Count;
            foreach (var seed in _outputSeeds)
            {
                _gameManager.PlayerInventory.Add(seed);
                PlantDatabase.Instance?.MarkPlantCodesDiscoveredFromMetadata(seed.SourcePlantCodeMetadata);
            }
            _outputSeeds.Clear();
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
                foundation.RemoveToast(IncubatorDoneToastKey);
            RefreshDisplay();

            if (foundation != null && foundation.Enabled)
                foundation.PostToastImmediate("LAB-INC-OK", new NotificationPayload().With("count", count.ToString()));
        }

        private bool RequiresReagentXSetup()
        {
            return _reagentTypeId == Items.ReagentX;
        }

        private bool IsReagentXSelectionValid()
        {
            if (!RequiresReagentXSetup()) return true;
            if (string.IsNullOrWhiteSpace(_selectedFamilyX)) return false;
            if (string.IsNullOrWhiteSpace(_selectedTrait1X)) return false;
            if (string.IsNullOrWhiteSpace(_selectedNameX)) return false;
            if (string.Equals(_selectedNameX, CustomNameOption, System.StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrWhiteSpace(_nameCustomField?.value);
            return true;
        }

        private Item GetPreviewPreSeed()
        {
            return _gameManager?.PlayerInventory?.PeekFirst(Items.PreSeed);
        }

        private void RefreshReagentXSelectors()
        {
            bool showX = RequiresReagentXSetup();
            if (_xConfigRow != null)
                _xConfigRow.style.display = showX ? DisplayStyle.Flex : DisplayStyle.None;
            if (!showX) return;

            var preSeed = GetPreviewPreSeed();
            var familyOptions = BuildFamilyOptionsForX(preSeed);
            var traitOptions = BuildTraitOptionsForX(preSeed);
            var nameOptions = BuildNameOptionsForX(preSeed);

            SetDropdownChoices(_familyDropdown, familyOptions, ref _selectedFamilyX);
            SetDropdownChoices(_trait1Dropdown, traitOptions, ref _selectedTrait1X);
            SetDropdownChoicesWithExclude(_trait2Dropdown, traitOptions, ref _selectedTrait2X, _selectedTrait1X);
            SetDropdownChoices(_nameDropdown, nameOptions, ref _selectedNameX);

            if (_nameCustomRow != null)
                _nameCustomRow.style.display = string.Equals(_selectedNameX, "Nome personalizzato", System.StringComparison.OrdinalIgnoreCase) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Solo le famiglie possibili del Pre-Seed (A e B). Fallback STANDARD se preSeed senza famiglia.</summary>
        private static List<string> BuildFamilyOptionsForX(Item preSeed)
        {
            var options = new List<string>();
            if (preSeed == null)
                return options;
            var set = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(preSeed.ParentFamilyA))
                set.Add(ItemFabric.NormalizeFamily(preSeed.ParentFamilyA));
            if (!string.IsNullOrWhiteSpace(preSeed.ParentFamilyB))
                set.Add(ItemFabric.NormalizeFamily(preSeed.ParentFamilyB));
            options = set.ToList();
            if (options.Count == 0)
                options.Add("STANDARD");
            return options;
        }

        /// <summary>Solo i tratti esistenti per le famiglie del Pre-Seed. Fallback un tratto se lista vuota (altrimenti Avvia resterebbe disabilitato).</summary>
        private static List<string> BuildTraitOptionsForX(Item preSeed)
        {
            string csv = preSeed?.CandidateTraitsCsv;
            if (string.IsNullOrWhiteSpace(csv) && preSeed != null)
                csv = ItemFabric.BuildCandidateTraitsCsv(preSeed.ParentFamilyA, preSeed.ParentFamilyB);
            var parsed = ItemFabric.ParseTraits(csv);
            if (parsed.Count == 0 && preSeed != null)
                parsed.Add("BalancedGrowth");
            return parsed;
        }

        private const string CustomNameOption = "Nome personalizzato";

        /// <summary>Opzioni nome: stessa pianta = un solo nome madre; ibrido = mix dei due nomi + Nome personalizzato. Fallback "Seme" se metadata assente.</summary>
        private static List<string> BuildNameOptionsForX(Item preSeed)
        {
            var list = new List<string>();
            if (preSeed == null)
                return list;
            if (string.IsNullOrWhiteSpace(preSeed.SourcePlantCodeMetadata))
            {
                list.Add("Seme");
                return list;
            }
            var parts = preSeed.SourcePlantCodeMetadata.Split('|');
            string codeA = parts.Length > 0 ? parts[0]?.Trim() : null;
            string codeB = parts.Length > 1 ? parts[1]?.Trim() : null;
            string nameA = GetPlantBaseName(codeA);
            string nameB = GetPlantBaseName(codeB);
            if (string.IsNullOrEmpty(nameA)) nameA = codeA ?? "—";
            if (string.IsNullOrEmpty(nameB)) nameB = codeB ?? "—";
            if (string.IsNullOrWhiteSpace(nameA)) nameA = "Seme";
            if (string.IsNullOrWhiteSpace(nameB)) nameB = "Seme";
            bool samePlant = string.Equals(codeA, codeB, System.StringComparison.OrdinalIgnoreCase) || string.Equals(nameA, nameB, System.StringComparison.OrdinalIgnoreCase);
            if (samePlant || string.IsNullOrEmpty(codeB))
            {
                list.Add(nameA);
                if (list.Count == 1)
                    list.Add(CustomNameOption);
                return list;
            }
            var wordsA = nameA.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
            var wordsB = nameB.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
            if (wordsA.Count >= 2 && wordsB.Count >= 2)
            {
                list.Add($"{wordsB[0]} {wordsA[1]}");
                list.Add($"{wordsA[0]} {wordsB[1]}");
                list.Add($"{wordsA[1]} {wordsB[0]}");
                list.Add($"{wordsB[1]} {wordsA[0]}");
            }
            list.Add($"{nameA} × {nameB}");
            list.Add($"{nameB} × {nameA}");
            list.Add(CustomNameOption);
            return list;
        }

        private static string GetPlantBaseName(string plantCode)
        {
            if (string.IsNullOrWhiteSpace(plantCode)) return null;
            var plantData = PlantDatabase.Instance?.GetPlantDataByCode(plantCode.Trim());
            if (plantData == null) return null;
            return plantData.PlantCode switch
            {
                "PLT-STD-001" => "Ferric Fern",
                "PLT-PURE-001" => "Arctic Hask",
                "PLT-EVIL-001" => "Glasscap Fungus",
                _ => plantData.name?.Replace("PLT-", "").Replace("-", " ") ?? plantCode
            };
        }

        private static void SetDropdownChoices(DropdownField field, List<string> choices, ref string selected)
        {
            if (field == null) return;
            field.choices = choices;
            if (choices == null || choices.Count == 0)
            {
                selected = null;
                field.value = "";
                return;
            }
            if (string.IsNullOrWhiteSpace(selected) || !choices.Contains(selected))
                selected = choices[0];
            field.value = selected;
        }

        /// <summary>Come SetDropdownChoices ma preferisce un valore diverso da exclude (es. per Tratto 2 rispetto a Tratto 1).</summary>
        private static void SetDropdownChoicesWithExclude(DropdownField field, List<string> choices, ref string selected, string exclude)
        {
            if (field == null) return;
            field.choices = choices;
            if (choices == null || choices.Count == 0)
            {
                selected = null;
                field.value = "";
                return;
            }
            if (string.IsNullOrWhiteSpace(selected) || !choices.Contains(selected) || selected == exclude)
            {
                selected = choices.Count >= 2 && !string.IsNullOrEmpty(exclude)
                    ? choices.FirstOrDefault(c => !c.Equals(exclude, System.StringComparison.OrdinalIgnoreCase)) ?? choices[0]
                    : choices[0];
            }
            field.value = selected;
        }

        private Item BuildSeedOutputFromIncubation()
        {
            if (_incubatingPreSeed == null)
            {
                var fallback = ItemFabric.CreateItemByType(_outputSeedTypeId);
                if (fallback != null)
                    fallback.GeneticTypeValue = GeneticType.Stable;
                return fallback;
            }

            string familyResult;
            string traitsResult;
            int traitPower;

            if (_reagentTypeId == Items.ReagentX)
            {
                familyResult = string.IsNullOrWhiteSpace(_selectedFamilyX)
                    ? ItemFabric.NormalizeFamily(_incubatingPreSeed.ParentFamilyA)
                    : _selectedFamilyX;
                var chosen = new List<string>();
                if (!string.IsNullOrWhiteSpace(_selectedTrait1X)) chosen.Add(_selectedTrait1X);
                if (!string.IsNullOrWhiteSpace(_selectedTrait2X) && !_selectedTrait2X.Equals(_selectedTrait1X)) chosen.Add(_selectedTrait2X);
                traitsResult = string.Join(",", chosen);
                traitPower = 100;
            }
            else if (_reagentTypeId == Items.ReagentY)
            {
                familyResult = ItemFabric.ResolveFamilyWithReagentY(_incubatingPreSeed.ParentFamilyA, _incubatingPreSeed.ParentFamilyB);
                var pool = ItemFabric.ParseTraits(_incubatingPreSeed.CandidateTraitsCsv);
                if (pool.Count == 0)
                    pool = ItemFabric.ParseTraits(ItemFabric.BuildCandidateTraitsCsv(_incubatingPreSeed.ParentFamilyA, _incubatingPreSeed.ParentFamilyB));
                string picked = pool.OrderBy(t => t).FirstOrDefault() ?? "BalancedGrowth";
                traitsResult = picked;
                traitPower = 100;
            }
            else
            {
                string fa = ItemFabric.NormalizeFamily(_incubatingPreSeed.ParentFamilyA);
                string fb = ItemFabric.NormalizeFamily(_incubatingPreSeed.ParentFamilyB);
                bool sameFamily = fa == fb;
                familyResult = sameFamily ? fa : $"HYBRID-WEAK({fa}/{fb})";
                var pool = ItemFabric.ParseTraits(_incubatingPreSeed.CandidateTraitsCsv);
                string picked = pool.OrderBy(t => t).FirstOrDefault() ?? "BalancedGrowth";
                traitsResult = picked;
                traitPower = 50;
            }

            string chosenPlantName = null;
            if (_reagentTypeId == Items.ReagentX)
            {
                if (string.Equals(_selectedNameX, CustomNameOption, System.StringComparison.OrdinalIgnoreCase))
                    chosenPlantName = _nameCustomField?.value?.Trim();
                else
                    chosenPlantName = _selectedNameX;
            }
            else
            {
                var nameOpts = BuildNameOptionsForX(_incubatingPreSeed);
                if (nameOpts.Count > 0 && nameOpts[0] != CustomNameOption)
                    chosenPlantName = nameOpts[0];
            }
            return ItemFabric.CreateSeedFromPreSeed(_incubatingPreSeed, familyResult, traitsResult, traitPower, _reagentTypeId, chosenPlantName);
        }
    }
}
