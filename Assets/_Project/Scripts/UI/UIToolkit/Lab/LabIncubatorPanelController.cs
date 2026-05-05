using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using _Project;
using _Project.Sporae.Core;
using Sporae.Core;
using Sporae.Dome.PotSystem.Growth;
using Sporae.DevTools;
using Sporae.Core.Localization;
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
        private DropdownField _activePowerDropdown;
        private DropdownField _passivePowerDropdown;
        private DropdownField _careProfileDropdown;
        private DropdownField _nameDropdown;
        private VisualElement _nameCustomRow;
        private TextField _nameCustomField;
        private VisualElement _dominantGenomeRow;
        private DropdownField _dominantGenomeDropdown;
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
        private string _selectedActivePowerX;
        private string _selectedPassivePowerX;
        private string _selectedCareProfileValue = "BLEND";
        private string _selectedNameX;
        private bool _nameModeIsCustom;
        /// <summary>AUTO | PARENT_A | PARENT_B — quale PlantCode usare quando il nome è libero (Reagente X).</summary>
        private string _dominantGenomeForCustomName = DominantGenomeAuto;
        /// <summary>Incubatore X: etichetta nome → PlantCode dominante per TypeId/drift (allineato al mix scelto).</summary>
        private readonly Dictionary<string, string> _nameChoiceToReferencePlantCode = new(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>Valori UI catturati all'Avvia: dopo il consumo del pre-seed <see cref="RefreshReagentXSelectors"/> non ha più un Peek valido e azzerava nome/poteri.</summary>
        private sealed class PendingReagentXSnapshot
        {
            public string Family;
            public string ActivePower;
            public string PassivePower;
            public string CareProfile;
            public bool NameModeCustom;
            public string CustomName;
            public string SelectedMixName;
            public string DominantGenome;
        }

        private PendingReagentXSnapshot _pendingReagentX;

        private const string NoPowerChoice = "— nessuno —";
        private static readonly string[] CareProfileValues = { "BLEND", "PARENT_A", "PARENT_B" };
        private static readonly string[] CareProfileLabels =
        {
            "Specie del seme (range cure default)",
            "Come genitore A (acqua / LED / fertilizzante)",
            "Come genitore B (acqua / LED / fertilizzante)"
        };
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
            _activePowerDropdown = _root.Q<DropdownField>("lab-inc-x-active-power");
            _passivePowerDropdown = _root.Q<DropdownField>("lab-inc-x-passive-power");
            _careProfileDropdown = _root.Q<DropdownField>("lab-inc-x-care-profile");
            _nameDropdown = _root.Q<DropdownField>("lab-inc-x-name");
            _nameCustomRow = _root.Q<VisualElement>("lab-inc-x-name-custom-row");
            _nameCustomField = _root.Q<TextField>("lab-inc-x-name-custom");
            _dominantGenomeRow = _root.Q<VisualElement>("lab-inc-x-dominant-row");
            _dominantGenomeDropdown = _root.Q<DropdownField>("lab-inc-x-dominant-genome");
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
                _selectedActivePowerX = null;
                _selectedPassivePowerX = null;
                _selectedCareProfileValue = "BLEND";
                _selectedNameX = null;
                _nameModeIsCustom = false;
                _dominantGenomeForCustomName = DominantGenomeAuto;
                _pendingReagentX = null;
                RefreshDisplay();
            };
            if (_familyDropdown != null)
                _familyDropdown.RegisterValueChangedCallback(evt => _selectedFamilyX = evt.newValue);
            if (_activePowerDropdown != null)
                _activePowerDropdown.RegisterValueChangedCallback(evt => _selectedActivePowerX = evt.newValue);
            if (_passivePowerDropdown != null)
                _passivePowerDropdown.RegisterValueChangedCallback(evt => _selectedPassivePowerX = evt.newValue);
            if (_careProfileDropdown != null)
                _careProfileDropdown.RegisterValueChangedCallback(OnCareProfileChanged);
            if (_nameDropdown != null)
                _nameDropdown.RegisterValueChangedCallback(evt => OnNameDropdownChanged(evt.newValue));
            if (_nameCustomField != null)
                _nameCustomField.RegisterValueChangedCallback(evt =>
                {
                    if (_nameModeIsCustom)
                        _selectedNameX = string.IsNullOrWhiteSpace(evt.newValue) ? null : evt.newValue.Trim();
                });
            if (_dominantGenomeDropdown != null)
                _dominantGenomeDropdown.RegisterValueChangedCallback(OnDominantGenomeChanged);
            _uiBound = true;
        }

        private void OnNameDropdownChanged(string newValue)
        {
            _nameModeIsCustom = string.Equals(newValue, CustomNameOption, System.StringComparison.OrdinalIgnoreCase);
            if (_nameCustomRow != null)
                _nameCustomRow.style.display = _nameModeIsCustom ? DisplayStyle.Flex : DisplayStyle.None;
            _selectedNameX = _nameModeIsCustom ? (_nameCustomField?.value?.Trim()) : newValue;
            SyncDominantGenomeUi();
        }

        private void OnDominantGenomeChanged(ChangeEvent<string> evt)
        {
            int i = _dominantGenomeDropdown != null ? _dominantGenomeDropdown.index : -1;
            if (i >= 0 && i < DominantGenomeValueOrder.Length)
                _dominantGenomeForCustomName = DominantGenomeValueOrder[i];
        }

        private void OnCareProfileChanged(ChangeEvent<string> evt)
        {
            if (_careProfileDropdown == null) return;
            int i = _careProfileDropdown.index;
            if (i >= 0 && i < CareProfileValues.Length)
                _selectedCareProfileValue = CareProfileValues[i];
        }

        private void OnCloseClicked() => Hide();

        private void EnsureOutputTooltip()
        {
            if (_outputTooltip != null || _root == null) return;
            _outputTooltip = _root.Q<VisualElement>("lab-inc-output-tooltip");
            _outputTooltipText = _outputTooltip?.Q<Label>("lab-inc-output-tooltip-text");
            if (_outputTooltip != null)
                _outputTooltip.pickingMode = PickingMode.Ignore;

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
            string provenienza = ExtractorTooltipTexts.GetOriginTraceLabel(first);
            var lines = new List<string>
            {
                ExtractorTooltipTexts.WrapValue(nameAndQty),
                $"Tratti: {ExtractorTooltipTexts.WrapValue(tratti)}",
                $"Famiglia: {ExtractorTooltipTexts.WrapValue(family)}",
                $"Provenienza: {ExtractorTooltipTexts.WrapValue(provenienza)}"
            };
            if (!string.IsNullOrWhiteSpace(first.SelectedTraitsCsv))
                lines.Add($"Tag gameplay (Task 6): {ExtractorTooltipTexts.WrapValue(first.SelectedTraitsCsv)}");
            if (!string.IsNullOrWhiteSpace(first.LabCareProfileMetadata))
                lines.Add($"Profilo cure: {ExtractorTooltipTexts.WrapValue(first.LabCareProfileMetadata)}");
            if (!string.IsNullOrWhiteSpace(first.ActivePowerLabel))
                lines.Add($"Attivo: {ExtractorTooltipTexts.WrapValue(first.ActivePowerLabel)}");
            if (!string.IsNullOrWhiteSpace(first.PassivePowerLabel))
                lines.Add($"Passivo: {ExtractorTooltipTexts.WrapValue(first.PassivePowerLabel)}");
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
            GameLanguageSettings.OnLanguageChanged += OnLanguageChanged;
            Hide();
        }

        private void OnLanguageChanged(GameLanguage _) => RefreshDisplay();

        private void OnDestroy()
        {
            GameLanguageSettings.OnLanguageChanged -= OnLanguageChanged;
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

        public bool IsIncubationInProgress => _incubationDay == 1 || _incubationDay == 2;
        public int IncubationDay => _incubationDay;
        public int ReadySeedCount => _outputSeeds.Count;
        public bool HasWorkPending => IsIncubationInProgress || ReadySeedCount > 0;
        public Item ReadySeedPreview => _outputSeeds.Count > 0 ? _outputSeeds[0] : null;

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
                LocalizationManager.GetString("lab_incubator.picker_preseed"),
                typeId =>
                {
                    // L'Incubatore usa direttamente l'inventario del giocatore; la selezione serve solo a confermare/disporre il picker
                    RefreshDisplay();
                },
                () => { },
                null,
                presentFullInventoryUi: true);
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
                LocalizationManager.GetString("lab_incubator.picker_reagent"),
                typeId =>
                {
                    _reagentTypeId = typeId;
                    if (_reagentTypeId != Items.ReagentX)
                    {
                        _selectedFamilyX = null;
                        _selectedActivePowerX = null;
                        _selectedPassivePowerX = null;
                        _selectedCareProfileValue = "BLEND";
                        _selectedNameX = null;
                        _nameModeIsCustom = false;
                        _dominantGenomeForCustomName = DominantGenomeAuto;
                        _pendingReagentX = null;
                    }
                    RefreshDisplay();
                },
                () => { },
                null,
                presentFullInventoryUi: true);
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
                _pendingReagentX = null;
                if (seed != null)
                    _outputSeeds.Add(seed);
                _incubatingPreSeed = null;
                if (foundation != null && foundation.Enabled)
                {
                    foundation.RemoveToast(IncubatorProgressToastKey);
                    var first = _outputSeeds.Count > 0 ? _outputSeeds[0] : null;
                    int qty = _outputSeeds.Count;
                    string displayName = first != null
                        ? PlayerInventoryPanelController.GetItemDisplayName(first.TypeId, first)
                        : "Seme";
                    BuildIncubatorCollectedProfile(first, out string profile, out string detail);
                    foundation.UpsertToast(IncubatorDoneToastKey, "LAB-INC-DONE", new NotificationPayload()
                        .With("itemName", string.IsNullOrWhiteSpace(displayName) ? "Seme" : displayName)
                        .With("quantity", Mathf.Max(1, qty).ToString())
                        .With("profile", profile ?? string.Empty)
                        .With("detail", detail ?? string.Empty));
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
                _preseedText.text = preseedInInventory > 0
                    ? LocalizationManager.GetString("lab_incubator.inv_preseed", new Dictionary<string, string> { ["count"] = preseedInInventory.ToString() })
                    : "—";

            if (_reagentText != null)
            {
                if (string.IsNullOrEmpty(_reagentTypeId))
                    _reagentText.text = "—";
                else if (ItemDisplayNameLocalization.TryGetByTypeId(_reagentTypeId, out var reagentLabel))
                    _reagentText.text = reagentLabel;
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
            {
                var f0 = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                if (f0 != null && f0.Enabled)
                    f0.PostToastImmediate("LAB-INC-X-INCOMPLETE");
                return;
            }

            PendingReagentXSnapshot capX = null;
            if (RequiresReagentXSetup())
            {
                capX = new PendingReagentXSnapshot
                {
                    Family = _selectedFamilyX,
                    ActivePower = _selectedActivePowerX,
                    PassivePower = _selectedPassivePowerX,
                    CareProfile = _selectedCareProfileValue,
                    NameModeCustom = _nameModeIsCustom,
                    CustomName = _nameCustomField?.value?.Trim(),
                    SelectedMixName = _selectedNameX,
                    DominantGenome = _dominantGenomeForCustomName ?? DominantGenomeAuto
                };
            }

            if (!_gameManager.TrySpendAction(_costAction))
                return;

            _pendingReagentX = capX;

            var dayActivityLog = ServiceContainer.Instance.Get<DayActivityLog>(suppressWarning: true);
            if (dayActivityLog != null)
                dayActivityLog.RecordLabAction("Incubator");
            if (!_gameManager.PlayerInventory.TryRemoveFirst(Items.PreSeed, out _incubatingPreSeed))
            {
                _pendingReagentX = null;
                return;
            }
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
            var firstSeed = _outputSeeds[0];
            string seedTypeId = firstSeed.TypeId;
            string seedDisplayName = PlayerInventoryPanelController.GetItemDisplayName(seedTypeId, firstSeed);

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
                foundation.PostAddedToInventory(seedTypeId, seedDisplayName, count, RoomNames.Laboratory);
        }

        private bool RequiresReagentXSetup()
        {
            return _reagentTypeId == Items.ReagentX;
        }

        private bool IsReagentXSelectionValid()
        {
            if (!RequiresReagentXSetup()) return true;
            if (string.IsNullOrWhiteSpace(_selectedFamilyX)) return false;
            if (_nameModeIsCustom)
            {
                if (string.IsNullOrWhiteSpace(_nameCustomField?.value?.Trim()))
                    return false;
            }
            else if (string.IsNullOrWhiteSpace(_selectedNameX))
                return false;

            var pre = GetPreviewPreSeed();
            if (pre != null)
            {
                bool needActive = BuildActivePowerOptions(pre).Exists(s => s != NoPowerChoice);
                bool needPassive = BuildPassivePowerOptions(pre).Exists(s => s != NoPowerChoice);
                if (needActive && (string.IsNullOrWhiteSpace(_selectedActivePowerX) ||
                                   string.Equals(_selectedActivePowerX, NoPowerChoice, StringComparison.Ordinal)))
                    return false;
                if (needPassive && (string.IsNullOrWhiteSpace(_selectedPassivePowerX) ||
                                    string.Equals(_selectedPassivePowerX, NoPowerChoice, StringComparison.Ordinal)))
                    return false;
            }
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

            if (_incubationDay != 0 && _pendingReagentX != null)
                return;

            var preSeed = GetPreviewPreSeed();
            var familyOptions = BuildFamilyOptionsForX(preSeed);
            var activeOpts = BuildActivePowerOptions(preSeed);
            var passiveOpts = BuildPassivePowerOptions(preSeed);
            var nameOptions = BuildNameOptionsForX(preSeed, _nameChoiceToReferencePlantCode);

            SetDropdownChoices(_familyDropdown, familyOptions, ref _selectedFamilyX);
            SetDropdownChoices(_activePowerDropdown, activeOpts, ref _selectedActivePowerX);
            SetDropdownChoices(_passivePowerDropdown, passiveOpts, ref _selectedPassivePowerX);
            SetDropdownChoices(_nameDropdown, nameOptions, ref _selectedNameX);
            _nameModeIsCustom = string.Equals(_nameDropdown?.value, CustomNameOption, StringComparison.OrdinalIgnoreCase);
            if (_nameModeIsCustom)
                _selectedNameX = _nameCustomField?.value?.Trim();
            SyncCareProfileDropdown();
            SyncDominantGenomeUi();
        }

        private void SyncDominantGenomeUi()
        {
            var preSeed = GetPreviewPreSeed();
            var codes = ItemFabric.ParseParentPlantCodes(preSeed?.SourcePlantCodeMetadata ?? "");
            bool twoParents = codes.Count >= 2 &&
                !string.Equals(codes[0], codes[1], StringComparison.OrdinalIgnoreCase);
            bool showDominant = RequiresReagentXSetup() && _nameModeIsCustom && twoParents;

            if (_nameCustomRow != null)
                _nameCustomRow.style.display = _nameModeIsCustom ? DisplayStyle.Flex : DisplayStyle.None;
            if (_dominantGenomeRow != null)
                _dominantGenomeRow.style.display = showDominant ? DisplayStyle.Flex : DisplayStyle.None;

            if (_dominantGenomeDropdown == null || !showDominant)
                return;

            string la = string.IsNullOrWhiteSpace(codes[0]) ? "—" : (GetPlantBaseName(codes[0]) ?? codes[0]);
            string lb = codes.Count > 1 && !string.IsNullOrWhiteSpace(codes[1])
                ? (GetPlantBaseName(codes[1]) ?? codes[1])
                : "—";
            var labels = new List<string>
            {
                "Automatico (specie da famiglia scelta)",
                $"Genitore A — {la}",
                $"Genitore B — {lb}"
            };
            _dominantGenomeDropdown.choices = labels;
            int idx = 0;
            for (int j = 0; j < DominantGenomeValueOrder.Length; j++)
            {
                if (string.Equals(DominantGenomeValueOrder[j], _dominantGenomeForCustomName, StringComparison.OrdinalIgnoreCase))
                {
                    idx = j;
                    break;
                }
            }
            if (idx < 0 || idx >= labels.Count) idx = 0;
            _dominantGenomeForCustomName = DominantGenomeValueOrder[idx];
            _dominantGenomeDropdown.index = idx;
        }

        private void SyncCareProfileDropdown()
        {
            if (_careProfileDropdown == null) return;
            _careProfileDropdown.choices = CareProfileLabels.ToList();
            int idx = Array.IndexOf(CareProfileValues, _selectedCareProfileValue);
            if (idx < 0) idx = 0;
            _selectedCareProfileValue = CareProfileValues[idx];
            _careProfileDropdown.index = idx;
        }

        private static string FirstDescriptorLine(string multilineOrSingle)
        {
            if (string.IsNullOrWhiteSpace(multilineOrSingle)) return null;
            int cut = multilineOrSingle.IndexOfAny(new[] { '\r', '\n' });
            string s = cut < 0 ? multilineOrSingle.Trim() : multilineOrSingle.Substring(0, cut).Trim();
            return string.IsNullOrEmpty(s) ? null : s;
        }

        private static List<string> BuildActivePowerOptions(Item preSeed)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var code in ItemFabric.ParseParentPlantCodes(preSeed?.SourcePlantCodeMetadata))
            {
                var pd = PlantDatabase.Instance?.GetPlantDataByCode(code);
                if (pd == null) continue;
                string line = FirstDescriptorLine(pd.ActivePower);
                if (!string.IsNullOrEmpty(line)) set.Add(line);
            }
            var list = set.ToList();
            list.Sort(StringComparer.OrdinalIgnoreCase);
            if (list.Count == 0)
                return new List<string> { NoPowerChoice };
            list.Insert(0, NoPowerChoice);
            return list;
        }

        private static List<string> BuildPassivePowerOptions(Item preSeed)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var code in ItemFabric.ParseParentPlantCodes(preSeed?.SourcePlantCodeMetadata))
            {
                var pd = PlantDatabase.Instance?.GetPlantDataByCode(code);
                if (pd == null) continue;
                string line = FirstDescriptorLine(pd.PassivePower);
                if (!string.IsNullOrEmpty(line)) set.Add(line);
            }
            var list = set.ToList();
            list.Sort(StringComparer.OrdinalIgnoreCase);
            if (list.Count == 0)
                return new List<string> { NoPowerChoice };
            list.Insert(0, NoPowerChoice);
            return list;
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

        private const string CustomNameOption = "Nome personalizzato";
        private const string DominantGenomeAuto = "AUTO";
        private const string DominantGenomeParentA = "PARENT_A";
        private const string DominantGenomeParentB = "PARENT_B";
        private static readonly string[] DominantGenomeValueOrder = { DominantGenomeAuto, DominantGenomeParentA, DominantGenomeParentB };

        /// <summary>Opzioni nome: stessa pianta = un solo nome madre; ibrido = mix dei due nomi + Nome personalizzato. Fallback "Seme" se metadata assente.</summary>
        /// <param name="referencePlantCodeByLabel">Se non null, per ogni voce (tranne nome personalizzato) viene impostato il <see cref="PlantData.PlantCode"/> usato come specie del seme.</param>
        private static List<string> BuildNameOptionsForX(Item preSeed, Dictionary<string, string> referencePlantCodeByLabel = null)
        {
            void MapRef(string label, string code)
            {
                if (referencePlantCodeByLabel == null || string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(code))
                    return;
                referencePlantCodeByLabel[label.Trim()] = code.Trim();
            }

            var list = new List<string>();
            referencePlantCodeByLabel?.Clear();
            if (preSeed == null)
                return list;
            if (string.IsNullOrWhiteSpace(preSeed.SourcePlantCodeMetadata))
            {
                list.Add("Seme");
                return list;
            }
            var codes = ItemFabric.ParseParentPlantCodes(preSeed.SourcePlantCodeMetadata);
            string codeA = codes.Count > 0 ? codes[0] : null;
            string codeB = codes.Count > 1 ? codes[1] : null;
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
                MapRef(nameA, codeA);
                if (list.Count == 1)
                    list.Add(CustomNameOption);
                return list;
            }
            var wordsA = nameA.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
            var wordsB = nameB.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
            if (wordsA.Count >= 2 && wordsB.Count >= 2)
            {
                string o1 = $"{wordsB[0]} {wordsA[1]}";
                list.Add(o1);
                MapRef(o1, codeB);
                string o2 = $"{wordsA[0]} {wordsB[1]}";
                list.Add(o2);
                MapRef(o2, codeA);
                string o3 = $"{wordsA[1]} {wordsB[0]}";
                list.Add(o3);
                MapRef(o3, codeA);
                string o4 = $"{wordsB[1]} {wordsA[0]}";
                list.Add(o4);
                MapRef(o4, codeB);
            }
            string xa = $"{nameA} × {nameB}";
            list.Add(xa);
            MapRef(xa, codeA);
            string xb = $"{nameB} × {nameA}";
            list.Add(xb);
            MapRef(xb, codeB);
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

        /// <summary>True se il seme deriva da incrocio (due linee in metadata o due famiglie genitrici distinte).</summary>
        public static bool IsLabHybridProfileSeed(Item seed)
        {
            if (seed == null) return false;
            if (!string.IsNullOrEmpty(seed.SourcePlantCodeMetadata) && seed.SourcePlantCodeMetadata.Contains("|"))
                return true;
            if (!string.IsNullOrWhiteSpace(seed.ParentFamilyA) && !string.IsNullOrWhiteSpace(seed.ParentFamilyB)
                && !string.Equals(seed.ParentFamilyA, seed.ParentFamilyB, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        private static void BuildIncubatorCollectedProfile(Item firstSeed, out string profile, out string detail)
        {
            string baseDetail = LocalizationManager.GetString("lab_incubator.profile_base");
            profile = string.Empty;
            detail = baseDetail;
            if (firstSeed == null || !IsLabHybridProfileSeed(firstSeed))
                return;
            string fam = string.IsNullOrWhiteSpace(firstSeed.FamilyMetadata) ? "—" : firstSeed.FamilyMetadata.Trim();
            string traits = string.IsNullOrWhiteSpace(firstSeed.SelectedTraitsCsv) ? "—" : firstSeed.SelectedTraitsCsv.Trim();
            string reag = string.IsNullOrWhiteSpace(firstSeed.ReagentUsedMetadata) ? "—" : firstSeed.ReagentUsedMetadata.Trim();
            profile = $" — {fam} | {traits}";
            detail = LocalizationManager.GetString("lab_incubator.profile_hybrid_detail", new Dictionary<string, string>
            {
                ["fam"] = fam,
                ["traits"] = traits,
                ["reag"] = reag,
                ["base"] = baseDetail
            });
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

            string activePick = null;
            string passivePick = null;
            string careProfile = null;

            PendingReagentXSnapshot xSnap = (_reagentTypeId == Items.ReagentX) ? _pendingReagentX : null;
            bool useXSnap = xSnap != null;

            if (_reagentTypeId == Items.ReagentX)
            {
                string famPick = useXSnap ? xSnap.Family : _selectedFamilyX;
                familyResult = string.IsNullOrWhiteSpace(famPick)
                    ? ItemFabric.NormalizeFamily(_incubatingPreSeed.ParentFamilyA)
                    : famPick;
                string actRaw = useXSnap ? xSnap.ActivePower : _selectedActivePowerX;
                string pasRaw = useXSnap ? xSnap.PassivePower : _selectedPassivePowerX;
                activePick = string.IsNullOrWhiteSpace(actRaw) ||
                             string.Equals(actRaw, NoPowerChoice, StringComparison.Ordinal)
                    ? null
                    : actRaw;
                passivePick = string.IsNullOrWhiteSpace(pasRaw) ||
                              string.Equals(pasRaw, NoPowerChoice, StringComparison.Ordinal)
                    ? null
                    : pasRaw;
                traitsResult = ItemFabric.BuildSelectedTraitsCsvFromPowerChoices(activePick, passivePick);
                traitPower = 100;
                string careSrc = useXSnap ? xSnap.CareProfile : _selectedCareProfileValue;
                careProfile = string.IsNullOrWhiteSpace(careSrc) ? "BLEND" : careSrc.Trim();
            }
            else if (_reagentTypeId == Items.ReagentY)
            {
                familyResult = ItemFabric.ResolveFamilyWithReagentY(_incubatingPreSeed.ParentFamilyA, _incubatingPreSeed.ParentFamilyB);
                var pool = ItemFabric.ParseTraits(_incubatingPreSeed.CandidateTraitsCsv);
                if (pool.Count == 0)
                    pool = ItemFabric.ParseTraits(ItemFabric.BuildCandidateTraitsCsv(_incubatingPreSeed.ParentFamilyA, _incubatingPreSeed.ParentFamilyB));
                string picked = pool.OrderBy(t => t).FirstOrDefault() ?? "BalancedGrowth";
                traitsResult = ItemFabric.NormalizeTraitsRowToGameplayTagCsv(picked);
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
                traitsResult = ItemFabric.NormalizeTraitsRowToGameplayTagCsv(picked);
                traitPower = 50;
            }

            string chosenPlantName = null;
            bool nameModeCustom = false;
            string dominantGenomeValue = DominantGenomeAuto;
            if (_reagentTypeId == Items.ReagentX)
            {
                nameModeCustom = useXSnap ? xSnap.NameModeCustom : _nameModeIsCustom;
                dominantGenomeValue = useXSnap
                    ? (string.IsNullOrWhiteSpace(xSnap.DominantGenome) ? DominantGenomeAuto : xSnap.DominantGenome)
                    : _dominantGenomeForCustomName;
                chosenPlantName = nameModeCustom
                    ? (useXSnap ? xSnap.CustomName : _nameCustomField?.value?.Trim())
                    : (useXSnap ? xSnap.SelectedMixName : _selectedNameX);
            }
            else
            {
                var nameOpts = BuildNameOptionsForX(_incubatingPreSeed, null);
                if (nameOpts.Count > 0 && nameOpts[0] != CustomNameOption)
                    chosenPlantName = nameOpts[0];
            }

            if (_reagentTypeId == Items.ReagentX)
            {
                string refPlantOverride = null;
                if (nameModeCustom)
                {
                    var pcodes = ItemFabric.ParseParentPlantCodes(_incubatingPreSeed.SourcePlantCodeMetadata);
                    if (string.Equals(dominantGenomeValue, DominantGenomeParentA, StringComparison.OrdinalIgnoreCase) &&
                        pcodes.Count > 0)
                        refPlantOverride = pcodes[0];
                    else if (string.Equals(dominantGenomeValue, DominantGenomeParentB, StringComparison.OrdinalIgnoreCase))
                    {
                        if (pcodes.Count > 1) refPlantOverride = pcodes[1];
                        else if (pcodes.Count > 0) refPlantOverride = pcodes[0];
                    }
                    else
                        refPlantOverride = ItemFabric.TryResolveReferencePlantCodeFromPowerChoices(
                            _incubatingPreSeed, activePick, passivePick);
                }
                // Per i nomi non-custom (preset/mix) NON forziamo la specie:
                // la specie resta determinata da famiglia selezionata + risoluzione standard in ItemFabric.
                // Questo evita mismatch tipo "nome mix Evil/Pure" che forza accidentalmente PLT-PURE-001.
                return ItemFabric.CreateSeedFromPreSeed(
                    _incubatingPreSeed, familyResult, traitsResult, traitPower, _reagentTypeId, chosenPlantName,
                    refPlantOverride, activePick, passivePick, careProfile);
            }
            return ItemFabric.CreateSeedFromPreSeed(_incubatingPreSeed, familyResult, traitsResult, traitPower, _reagentTypeId, chosenPlantName);
        }
    }
}
