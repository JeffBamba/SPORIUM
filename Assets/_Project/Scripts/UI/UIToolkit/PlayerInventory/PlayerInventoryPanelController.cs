using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using _Project;
using _Project.Sporae.Core;
using Sporae.Core;
using Sporae.Dome.PotSystem.Growth;
using Sporae.UI.Icons;
using Sporae.UI.UIToolkit.Lab;
using Sporae.UI.UIToolkit.PlantCard.Helpers;
using Sporae.Core.Localization;

namespace Sporae.UI.UIToolkit.PlayerInventory
{
    /// <summary> Inventario giocatore (VAULT-07) — modalità browse con filtri/scheda, modalità picker lab. </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PlayerInventoryPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument _uiDocument;
        [Tooltip("Solo UI Builder: mostra blocco riferimento classi; in Play nascosto salvo true qui.")]
        [SerializeField] private bool _showBuilderReferenceDuringPlay;

        private VisualElement _root;
        private VisualElement _overlay;
        private VisualElement _terminal;
        private VisualElement _scrim;
        private VisualElement _stats;
        private VisualElement _filters;
        private VisualElement _detail;
        private VisualElement _footer;
        private Label _dbTitle;
        private Label _pickSubtitle;
        private Label _statH2o;
        private Label _statActions;
        private Label _statItems;
        private ScrollView _list;
        private Button _btnClose;
        private Button _btnCancel;
        private Button _btnExpand;
        private Button _btnDetailUse;
        private Button _btnDetailInspect;
        private VisualElement _detailDecayWrap;
        private VisualElement _detailDecayFill;
        private Label _detailDecayPct;
        private Label _detailDecayConditionLbl;
        private Label _detailIdLine;
        private Label _detailSummary;
        private Label _detailQty;
        private Label _detailMeta;
        private VisualElement _detailIcon;
        private Label _detailPrompt;
        private VisualElement _invTooltip;
        private Label _invTooltipText;
        private VisualElement _confirm;
        private Label _confirmTitle;
        private Label _confirmBody;
        private Button _btnConfirmYes;
        private Button _btnConfirmNo;
        private VisualElement _inspectOverlay;
        private Label _inspectTitle;
        private Label _inspectBody;
        private Button _btnInspectClose;
        private VisualElement _builderRef;

        private readonly List<Button> _filterButtons = new List<Button>();
        private ItemInventoryCategoryId _activeFilter = ItemInventoryCategoryId.All;
        private bool _listExpanded;
        private const int MaxCollapsedItemRows = 10;

        private Inventory _playerInventory;
        private GameManager _gameManager;
        private HashSet<string> _pickerAllowedTypes;
        private List<string> _pickerAllowedTypesOrdered;
        private Action<string, SporeStage?, Item> _onSelectedWithStage;
        private SporeStage? _pickerFilterSporeStage;
        private Action _onCancel;
        private string _pickerContext;
        private bool _uiBound;
        private Action<int> _onActionsChangedUi;
        private float _ignoreScrimClickUntil;

        private string _selectedRowKey;
        private List<RowModel> _allRows = new List<RowModel>();
        private RowModel _selectedModel;

        private struct RowModel
        {
            public string Key;
            public string TypeId;
            public Item ItemOrNull;
            public int Qty;
            public string DisplayName;
            public string Sub;
            public string Tooltip;
            public bool PickerCanSelect;
            public bool IsPerItemRow;
        }

        public event Action OnClosed;
        public bool IsVisible => _overlay != null && _overlay.style.display != DisplayStyle.None;

        private static bool IsPickerMode(HashSet<string> t) => t != null && t.Count > 0;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null)
                _uiDocument.sortingOrder = 450;
            _root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (_root != null)
                TryBindUI();
            TryBindGameManager();
            TryBindInventory();
            ServiceContainer.Instance?.Register(this);
        }

        private void Update()
        {
            if (!IsVisible) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_confirm != null && _confirm.resolvedStyle.display == DisplayStyle.Flex)
                {
                    HideUseConfirm();
                    return;
                }
                if (IsPickerMode(_pickerAllowedTypes))
                {
                    _onCancel?.Invoke();
                    Hide();
                }
                else
                    Hide();
            }
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
            if (_uiBound) return;
            if (_root == null && _uiDocument != null)
                _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

            _overlay = _root.Q<VisualElement>("inv-overlay");
            _terminal = _root.Q<VisualElement>("inv-terminal");
            _scrim = _root.Q<VisualElement>("inv-scrim");
            _stats = _root.Q<VisualElement>("inv-stats");
            _filters = _root.Q<VisualElement>("inv-filters");
            _dbTitle = _root.Q<Label>("inv-db-title");
            _pickSubtitle = _root.Q<Label>("inv-subtitle");
            _statH2o = _root.Q<Label>("inv-stat-h2o");
            _statActions = _root.Q<Label>("inv-stat-actions");
            _statItems = _root.Q<Label>("inv-stat-items");
            _list = _root.Q<ScrollView>("inv-list");
            _btnClose = _root.Q<Button>("btn-close");
            _btnCancel = _root.Q<Button>("btn-cancel");
            _btnExpand = _root.Q<Button>("inv-expand");
            _detail = _root.Q<VisualElement>("inv-detail");
            _detailIdLine = _root.Q<Label>("inv-detail-id-line");
            _detailSummary = _root.Q<Label>("inv-detail-summary");
            _detailQty = _root.Q<Label>("inv-detail-qty");
            _detailMeta = _root.Q<Label>("inv-detail-meta");
            _detailIcon = _root.Q<VisualElement>("inv-detail-icon");
            _detailPrompt = _root.Q<Label>("inv-detail-prompt");
            _btnDetailUse = _root.Q<Button>("btn-detail-use");
            _btnDetailInspect = _root.Q<Button>("btn-detail-inspect");
            _detailDecayWrap = _root.Q<VisualElement>("inv-detail-decay-wrap");
            _detailDecayFill = _root.Q<VisualElement>("inv-detail-decay-fill");
            _detailDecayPct = _root.Q<Label>("inv-detail-decay-pct");
            _detailDecayConditionLbl = _root.Q<Label>("inv-detail-decay-condition-lbl");
            _footer = _root.Q<VisualElement>("inv-footer");
            _invTooltip = _root.Q<VisualElement>("inv-tooltip");
            _invTooltipText = _root.Q<Label>("inv-tooltip-text");
            _confirm = _root.Q<VisualElement>("inv-confirm");
            _confirmTitle = _root.Q<Label>("inv-confirm-title");
            _confirmBody = _root.Q<Label>("inv-confirm-body");
            _btnConfirmYes = _root.Q<Button>("btn-confirm-yes");
            _btnConfirmNo = _root.Q<Button>("btn-confirm-no");
            _inspectOverlay = _root.Q<VisualElement>("inv-inspect");
            _inspectTitle = _root.Q<Label>("inv-inspect-title");
            _inspectBody = _root.Q<Label>("inv-inspect-body");
            _btnInspectClose = _root.Q<Button>("btn-inspect-close");
            _builderRef = _root.Q<VisualElement>("inv-builder-reference");

            if (_scrim != null)
                _scrim.RegisterCallback<ClickEvent>(_ =>
                {
                    if (Time.unscaledTime < _ignoreScrimClickUntil) return;
                    if (IsPickerMode(_pickerAllowedTypes))
                    {
                        _onCancel?.Invoke();
                        Hide();
                        return;
                    }

                    // In modalità inventario standard, click fuori terminale:
                    // chiude solo il box dettaglio selezionato, non l'intero inventario.
                    if (!string.IsNullOrEmpty(_selectedRowKey))
                        ClearDetailSelection(rebuild: false);
                });
            if (_btnClose != null) _btnClose.clicked += Hide;
            if (_btnCancel != null) _btnCancel.clicked += OnCancelClicked;
            if (_btnExpand != null) _btnExpand.clicked += OnExpandClicked;
            if (_btnDetailUse != null) _btnDetailUse.clicked += OnDetailUseClicked;
            if (_btnDetailInspect != null) _btnDetailInspect.clicked += OnDetailInspectClicked;
            if (_btnConfirmNo != null) _btnConfirmNo.clicked += HideUseConfirm;
            if (_btnConfirmYes != null) _btnConfirmYes.clicked += OnConfirmUseYes;
            if (_btnInspectClose != null) _btnInspectClose.clicked += HideInspectModal;

            _filterButtons.Clear();
            RegisterFilter("inv-filter-all", ItemInventoryCategoryId.All);
            RegisterFilter("inv-filter-spores", ItemInventoryCategoryId.Spores);
            RegisterFilter("inv-filter-seeds", ItemInventoryCategoryId.Seeds);
            RegisterFilter("inv-filter-organic", ItemInventoryCategoryId.Organic);
            RegisterFilter("inv-filter-reagents", ItemInventoryCategoryId.Reagents);
            RegisterFilter("inv-filter-plants", ItemInventoryCategoryId.Plants);
            RegisterFilter("inv-filter-tools", ItemInventoryCategoryId.Tools);
            RegisterFilter("inv-filter-food", ItemInventoryCategoryId.Food);
            RegisterFilter("inv-filter-bio", ItemInventoryCategoryId.BioMaterials);

            ApplyStaticChrome();
            if (_footer != null)
            {
                var fv = _root.Q<Label>("inv-footer-version");
                var fh = _root.Q<Label>("inv-footer-hint");
                if (fv != null) fv.text = LocalizationManager.GetString("inventory.footer_version");
                if (fh != null) fh.text = LocalizationManager.GetString("inventory.footer_hint");
            }
            if (_btnDetailInspect != null)
                _btnDetailInspect.SetEnabled(true);

            if (_builderRef != null)
                _builderRef.style.display = _showBuilderReferenceDuringPlay && Application.isPlaying ? DisplayStyle.Flex : DisplayStyle.None;

            _uiBound = true;
        }

        private void RegisterFilter(string name, ItemInventoryCategoryId cat)
        {
            var b = _root?.Q<Button>(name);
            if (b == null) return;
            _filterButtons.Add(b);
            b.clicked += () => SetFilter(cat);
        }

        private void SetFilter(ItemInventoryCategoryId c)
        {
            _activeFilter = c;
            _listExpanded = false;
            _selectedRowKey = null;
            _selectedModel = default;
            UpdateFilterChips();
            Rebuild();
        }

        private void UpdateFilterChips()
        {
            var names = new[] { "inv-filter-all", "inv-filter-spores", "inv-filter-seeds", "inv-filter-organic", "inv-filter-reagents", "inv-filter-plants", "inv-filter-tools", "inv-filter-food", "inv-filter-bio" };
            var cats = new[]
            {
                ItemInventoryCategoryId.All, ItemInventoryCategoryId.Spores, ItemInventoryCategoryId.Seeds,
                ItemInventoryCategoryId.Organic, ItemInventoryCategoryId.Reagents, ItemInventoryCategoryId.Plants,
                ItemInventoryCategoryId.Tools, ItemInventoryCategoryId.Food, ItemInventoryCategoryId.BioMaterials
            };
            for (int i = 0; i < names.Length; i++)
            {
                var b = _root?.Q<Button>(names[i]);
                if (b == null) continue;
                if (cats[i] == _activeFilter)
                    b.AddToClassList("inv-filter-chip--active");
                else
                    b.RemoveFromClassList("inv-filter-chip--active");
            }
        }

        private void ApplyStaticChrome()
        {
            if (_dbTitle != null) _dbTitle.text = LocalizationManager.GetString("inventory.database_title");
            var vl = _root?.Q<Label>("inv-vault-label");
            if (vl != null) vl.text = LocalizationManager.GetString("inventory.vault_badge");
            if (_btnClose != null) _btnClose.text = LocalizationManager.GetString("inventory.close");
            if (_btnExpand != null) _btnExpand.text = LocalizationManager.GetString("inventory.expand_show", new Dictionary<string, string> { { "n", "0" } });
            if (_btnDetailUse != null) _btnDetailUse.text = LocalizationManager.GetString("inventory.detail.use_item");
            if (_btnDetailInspect != null) _btnDetailInspect.text = LocalizationManager.GetString("inventory.detail.inspect");
            if (_detailDecayConditionLbl != null)
                _detailDecayConditionLbl.text = LocalizationManager.GetString("inventory.detail.condition_label");
            if (_confirmTitle != null) _confirmTitle.text = LocalizationManager.GetString("inventory.confirm_action");
            if (_btnConfirmYes != null) _btnConfirmYes.text = LocalizationManager.GetString("inventory.confirm_yes");
            if (_btnConfirmNo != null) _btnConfirmNo.text = LocalizationManager.GetString("inventory.confirm_no");
            if (_inspectTitle != null) _inspectTitle.text = LocalizationManager.GetString("inventory.detail.inspect");
            if (_btnInspectClose != null) _btnInspectClose.text = LocalizationManager.GetString("inventory.close");
            var cl = _root?.Q<Label>("inv-cancel-lbl");
            if (cl != null) cl.text = LocalizationManager.GetString("inventory.cancel");
            ApplyFilterLabels();
        }

        private void ApplyFilterLabels()
        {
            if (_root == null) return;
            void Set(string name, string key)
            {
                var b = _root.Q<Button>(name);
                if (b != null) b.text = LocalizationManager.GetString(key);
            }
            Set("inv-filter-all", "inventory.filter_all");
            Set("inv-filter-spores", "inventory.filter_spores");
            Set("inv-filter-seeds", "inventory.filter_seeds");
            Set("inv-filter-organic", "inventory.filter_organic");
            Set("inv-filter-reagents", "inventory.filter_reagents");
            Set("inv-filter-plants", "inventory.filter_plants");
            Set("inv-filter-tools", "inventory.filter_tools");
            Set("inv-filter-food", "inventory.filter_food");
            Set("inv-filter-bio", "inventory.filter_bio");
        }

        private void DetailPanelEnterAwaiting()
        {
            if (_detail == null) return;
            if (IsPickerMode(_pickerAllowedTypes)) return;
            _detail.style.display = DisplayStyle.None;
            _terminal?.RemoveFromClassList("inv-terminal--detail-open");
            foreach (var catCls in ItemInventoryCategoryMap.AllDetailAccentClassNames)
                _detail.RemoveFromClassList(catCls);
        }

        private void DetailPanelEnterHasSelection(string typeId)
        {
            if (_detail == null) return;
            _detail.style.display = DisplayStyle.Flex;
            _terminal?.AddToClassList("inv-terminal--detail-open");
            foreach (var catCls in ItemInventoryCategoryMap.AllDetailAccentClassNames)
                _detail.RemoveFromClassList(catCls);
            ItemInventoryCategoryMap.TryGetCategory(typeId, out var catEnum);
            _detail.AddToClassList(ItemInventoryCategoryMap.GetDetailAccentClass(catEnum));
        }

        private void OnExpandClicked()
        {
            var visible = _allRows?.Where(r => PassesFilter(r)).ToList() ?? new List<RowModel>();
            bool canCollapse = _listExpanded && visible.Count > MaxCollapsedItemRows;
            if (canCollapse)
                _listExpanded = false;
            else
                _listExpanded = true;
            Rebuild();
        }

        private void OnEnable()
        {
            GameLanguageSettings.OnLanguageChanged += OnLanguageChanged;
            TryBindGameManager();
            TryBindInventory();
            if (_playerInventory != null)
                _playerInventory.OnInventoryChanged += OnInventoryChanged;
            _onActionsChangedUi = _ => { if (IsVisible) UpdateStats(); };
            if (_onActionsChangedUi != null)
            {
                if (_gameManager == null) TryBindGameManager();
                if (_gameManager?.ActionSystem != null)
                    _gameManager.ActionSystem.OnActionsChanged += _onActionsChangedUi;
            }
        }

        private void OnDisable()
        {
            GameLanguageSettings.OnLanguageChanged -= OnLanguageChanged;
            if (_playerInventory != null)
                _playerInventory.OnInventoryChanged -= OnInventoryChanged;
            if (_gameManager?.ActionSystem != null && _onActionsChangedUi != null)
                _gameManager.ActionSystem.OnActionsChanged -= _onActionsChangedUi;
        }

        private void OnLanguageChanged(GameLanguage _)
        {
            ApplyStaticChrome();
            if (IsVisible) Rebuild();
        }

        private void OnInventoryChanged()
        {
            if (IsVisible) Rebuild();
        }

        private void TryBindGameManager()
        {
            _gameManager = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            if (_gameManager == null)
                _gameManager = FindObjectOfType<GameManager>();
        }

        private void TryBindInventory()
        {
            var gm = _gameManager ?? ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true) ?? FindObjectOfType<GameManager>();
            _playerInventory = gm != null ? gm.PlayerInventory : null;
        }

        private void OnCancelClicked()
        {
            _onCancel?.Invoke();
            Hide();
        }

        public void Show()
        {
            _pickerAllowedTypes = null;
            _pickerAllowedTypesOrdered = null;
            _onSelectedWithStage = null;
            _pickerFilterSporeStage = null;
            _onCancel = null;
            _activeFilter = ItemInventoryCategoryId.All;
            _listExpanded = false;
            _selectedRowKey = null;
            _selectedModel = default;
            ShowInternal();
            ApplyModeUi(isPicker: false);
            if (_pickSubtitle != null) { _pickSubtitle.text = ""; _pickSubtitle.style.display = DisplayStyle.None; }
            if (_dbTitle != null) _dbTitle.style.display = DisplayStyle.Flex;
            Rebuild();
        }

        public void Toggle()
        {
            if (IsVisible) Hide();
            else Show();
        }

        public void ShowAsPicker(IEnumerable<string> allowedTypeIds, string subtitle, Action<string> onSelected, Action onCancel, string pickerContext = null) =>
            ShowAsPicker(allowedTypeIds, subtitle, (id, _, __) => onSelected(id), onCancel, null, pickerContext);

        public void ShowAsPicker(IEnumerable<string> allowedTypeIds, string subtitle, Action<string, SporeStage?> onSelectedWithStage, Action onCancel, SporeStage? filterSporeStage = null, string pickerContext = null) =>
            ShowAsPicker(allowedTypeIds, subtitle, (id, st, _) => onSelectedWithStage(id, st), onCancel, filterSporeStage, pickerContext);

        public void ShowAsPicker(IEnumerable<string> allowedTypeIds, string subtitle, Action<string, SporeStage?, Item> onSelectedWithItem, Action onCancel, SporeStage? filterSporeStage = null, string pickerContext = null)
        {
            _pickerAllowedTypes = allowedTypeIds != null ? new HashSet<string>(allowedTypeIds) : new HashSet<string>();
            _pickerAllowedTypesOrdered = allowedTypeIds != null ? new List<string>(allowedTypeIds) : new List<string>();
            _onSelectedWithStage = onSelectedWithItem;
            _pickerFilterSporeStage = filterSporeStage;
            _onCancel = onCancel;
            _pickerContext = pickerContext;
            _selectedRowKey = null;
            _selectedModel = default;
            _listExpanded = true;
            ShowInternal();
            ApplyModeUi(isPicker: true);
            if (_pickSubtitle != null)
            {
                _pickSubtitle.text = string.IsNullOrEmpty(subtitle) ? LocalizationManager.GetString("inventory.subtitle") : subtitle;
                _pickSubtitle.style.display = DisplayStyle.Flex;
            }
            if (_dbTitle != null) _dbTitle.style.display = DisplayStyle.None;
            Rebuild();
        }

        private void ApplyModeUi(bool isPicker)
        {
            if (_stats != null) _stats.style.display = isPicker ? DisplayStyle.None : DisplayStyle.Flex;
            if (_filters != null) _filters.style.display = isPicker ? DisplayStyle.None : DisplayStyle.Flex;
            if (_footer != null) _footer.style.display = isPicker ? DisplayStyle.None : DisplayStyle.Flex;
            if (_detail != null)
                _detail.style.display = isPicker ? DisplayStyle.None :
                    (!string.IsNullOrEmpty(_selectedRowKey) ? DisplayStyle.Flex : DisplayStyle.None);
            if (_btnExpand != null) _btnExpand.style.display = DisplayStyle.None;
            if (_btnCancel != null) _btnCancel.style.display = isPicker ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Hide()
        {
            HideUseConfirm();
            HideInspectModal();
            if (_invTooltip != null) _invTooltip.style.display = DisplayStyle.None;
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.None;
            }
            gameObject.SetActive(false);
            OnClosed?.Invoke();
        }

        private void ShowInternal()
        {
            gameObject.SetActive(true);
            TryBindUI();
            _ignoreScrimClickUntil = Time.unscaledTime + 0.12f;
            if (_overlay != null) _overlay.style.display = DisplayStyle.Flex;
        }

        private void Rebuild()
        {
            if (_list == null) return;
            TryBindGameManager();
            TryBindInventory();
            _list.Clear();
            bool isPicker = IsPickerMode(_pickerAllowedTypes);
            if (_playerInventory == null)
            {
                _list.Add(MkEmpty(LocalizationManager.GetString("inventory.empty")));
                if (!isPicker) DetailPanelEnterAwaiting();
                return;
            }

            _allRows = BuildRowModels();
            if (_allRows.Count == 0)
            {
                _list.Add(MkEmpty(LocalizationManager.GetString("inventory.empty")));
                UpdateStats();
                if (!isPicker) DetailPanelEnterAwaiting();
                return;
            }

            var visible = _allRows.Where(r => PassesFilter(r)).ToList();
            if (visible.Count == 0)
            {
                _list.Add(MkEmpty(LocalizationManager.GetString("inventory.empty")));
                UpdateStats();
                if (!isPicker) DetailPanelEnterAwaiting();
                return;
            }
            bool usePaging = !isPicker && !(_listExpanded || visible.Count <= MaxCollapsedItemRows);
            var toRender = usePaging ? visible.Take(MaxCollapsedItemRows).ToList() : visible;
            if (usePaging && _btnExpand != null)
            {
                int more = visible.Count - MaxCollapsedItemRows;
                _btnExpand.text = LocalizationManager.GetString("inventory.expand_show", new Dictionary<string, string> { { "n", more.ToString() } });
                _btnExpand.style.display = DisplayStyle.Flex;
            }
            else if (_btnExpand != null)
            {
                if (!isPicker && _listExpanded && visible.Count > MaxCollapsedItemRows)
                {
                    _btnExpand.text = LocalizationManager.GetString("inventory.expand_less");
                    _btnExpand.style.display = DisplayStyle.Flex;
                }
                else
                    _btnExpand.style.display = DisplayStyle.None;
            }

            foreach (var m in toRender)
                _list.Add(BuildRowElement(m, isPicker));

            UpdateFilterChips();
            UpdateStats();
            if (isPicker)
            {
                if (_detail != null) _detail.style.display = DisplayStyle.None;
            }
            else
                RefreshSelectionAfterRebuild(visible, toRender);
        }

        private void RefreshSelectionAfterRebuild(List<RowModel> visible, List<RowModel> rendered)
        {
            if (string.IsNullOrEmpty(_selectedRowKey) || !rendered.Any(r => r.Key == _selectedRowKey))
            {
                _selectedModel = default;
                _selectedRowKey = null;
                DetailPanelEnterAwaiting();
                ApplyRowSelectionVisual();
            }
            else
            {
                _selectedModel = visible.FirstOrDefault(r => r.Key == _selectedRowKey);
                if (string.IsNullOrEmpty(_selectedModel.Key))
                {
                    _selectedRowKey = null;
                    DetailPanelEnterAwaiting();
                    ApplyRowSelectionVisual();
                    return;
                }
                DetailPanelEnterHasSelection(_selectedModel.TypeId);
                UpdateDetailPanel();
                ApplyRowSelectionVisual();
            }
        }

        private void ApplyRowSelectionVisual()
        {
            if (_list == null) return;
            foreach (var r in _list.Query<VisualElement>(className: "inv-row").ToList())
            {
                r.RemoveFromClassList("inv-row--selected");
                if (!string.IsNullOrEmpty(_selectedRowKey) && r.userData is string k && k == _selectedRowKey)
                    r.AddToClassList("inv-row--selected");
            }
        }

        private static VisualElement MkEmpty(string msg)
        {
            var empty = new Label(msg);
            empty.AddToClassList("inv-empty");
            return empty;
        }

        private bool PassesFilter(RowModel m)
        {
            if (IsPickerMode(_pickerAllowedTypes)) return true;
            if (_activeFilter == ItemInventoryCategoryId.All) return true;
            ItemInventoryCategoryMap.TryGetCategory(m.TypeId, out var c);
            return c == _activeFilter;
        }

        private List<RowModel> BuildRowModels()
        {
            var list = new List<RowModel>();
            if (_playerInventory == null) return list;
            bool isPicker = IsPickerMode(_pickerAllowedTypes);
            var slots = _playerInventory.Items.Where(s => s != null && s.Items != null && s.Items.Count > 0 && IsSlotVisibleInList(s)).ToList();
            if (isPicker && _pickerAllowedTypesOrdered != null && _pickerAllowedTypesOrdered.Count > 0)
            {
                var reordered = new List<InventorySlot>();
                var by = slots.ToDictionary(s => s.TypeId, s => s);
                foreach (var t in _pickerAllowedTypesOrdered)
                {
                    if (by.TryGetValue(t, out var sl)) reordered.Add(sl);
                }
                foreach (var s in slots)
                {
                    if (!_pickerAllowedTypes.Contains(s.TypeId)) reordered.Add(s);
                }
                slots = reordered;
            }

            foreach (var slot in slots)
            {
                if (slot.TypeId == Items.SporeGeneric)
                {
                    foreach (var it in slot.Items)
                    {
                        if (_pickerFilterSporeStage.HasValue && it.SporeStageValue != _pickerFilterSporeStage) continue;
                        var gen = it.GeneticTypeValue;
                        var title = ItemDisplayNameLocalization.GetSporeTitle(it.SporeStageValue);
                        var st = new RowModel
                        {
                            Key = Items.SporeGeneric + "|" + it.ItemId,
                            TypeId = Items.SporeGeneric,
                            ItemOrNull = it,
                            Qty = 1,
                            DisplayName = title,
                            Sub = GetSporeSubText(it.SporeStageValue, gen),
                            Tooltip = BuildSporeItemTooltip(title, it),
                            PickerCanSelect = !isPicker || (_pickerAllowedTypes != null && _pickerAllowedTypes.Contains(Items.SporeGeneric)),
                            IsPerItemRow = true
                        };
                        list.Add(st);
                    }
                }
                else if (IsFruitType(slot.TypeId))
                {
                    foreach (var it in slot.Items)
                    {
                        var dn = GetItemDisplayName(slot.TypeId, it);
                        list.Add(new RowModel
                        {
                            Key = slot.TypeId + "|" + it.ItemId,
                            TypeId = slot.TypeId,
                            ItemOrNull = it,
                            Qty = 1,
                            DisplayName = dn,
                            Sub = "",
                            Tooltip = BuildFruitTooltip(slot.TypeId, it),
                            PickerCanSelect = !isPicker || (_pickerAllowedTypes != null && _pickerAllowedTypes.Contains(slot.TypeId)),
                            IsPerItemRow = true
                        });
                    }
                }
                else
                {
                    int qty = slot.Quantity;
                    var first = slot.Items.FirstOrDefault();
                    var dn = GetItemDisplayName(slot.TypeId, first);
                    list.Add(new RowModel
                    {
                        Key = slot.TypeId,
                        TypeId = slot.TypeId,
                        ItemOrNull = first,
                        Qty = qty,
                        DisplayName = dn,
                        Sub = GetSporeInfoText(slot) ?? "",
                        Tooltip = slot.TypeId == Items.PreSeed
                            ? BuildPreSeedItemTooltip(first)
                            : BuildGenericItemTooltip(slot.TypeId, dn, qty, first),
                        PickerCanSelect = !isPicker || (_pickerAllowedTypes != null && _pickerAllowedTypes.Contains(slot.TypeId)),
                        IsPerItemRow = false
                    });
                }
            }
            return list;
        }

        private string BuildFruitTooltip(string typeId, Item it)
        {
            if (typeId == Items.FruitsKnown)
                return ExtractorTooltipTexts.IsUnknownFruit(it)
                    ? ExtractorTooltipTexts.BuildFruitKnownDemoTooltip()
                    : ExtractorTooltipTexts.BuildFruitPreviewTooltip(it);
            return ExtractorTooltipTexts.IsUnknownFruit(it)
                ? ExtractorTooltipTexts.BuildFruitUnknownPreviewTooltip(it)
                : ExtractorTooltipTexts.BuildFruitPreviewTooltip(it);
        }

        private bool IsSlotVisibleInList(InventorySlot slot)
        {
            if (slot == null || slot.Items == null || slot.Items.Count == 0) return false;
            if (ShouldHideStemCells() && (slot.TypeId == Items.StemCellVegetable || slot.TypeId == Items.StemCellFungus || slot.TypeId == Items.StemCellAnimal))
            {
                if (IsPickerMode(_pickerAllowedTypes))
                {
                    foreach (var t in _pickerAllowedTypes)
                        if (t == slot.TypeId) return true;
                }
                return false;
            }
            return true;
        }

        private bool ShouldHideStemCells()
        {
            if (IsPickerMode(_pickerAllowedTypes))
            {
                foreach (var t in _pickerAllowedTypes)
                {
                    if (t == Items.StemCellVegetable || t == Items.StemCellFungus || t == Items.StemCellAnimal)
                        return false;
                }
            }
            return true;
        }

        private static bool IsFruitType(string typeId) => Items.IsFruitType(typeId);

        private VisualElement BuildRowElement(RowModel m, bool isPicker)
        {
            ItemInventoryCategoryMap.TryGetCategory(m.TypeId, out var cat);
            var acc = ItemInventoryCategoryMap.GetRowAccentClass(cat);

            var row = new VisualElement();
            row.AddToClassList("inv-row");
            row.AddToClassList(acc);
            if (m.Key == _selectedRowKey) row.AddToClassList("inv-row--selected");
            if (isPicker && !m.PickerCanSelect)
                row.AddToClassList("inv-row-disabled");

            VisualElement iconEl;
            if (m.TypeId == Items.SporeGeneric && m.ItemOrNull != null)
                iconEl = BuildItemIconBox(Items.SporeGeneric, m.ItemOrNull.SporeStageValue);
            else
                iconEl = BuildItemIconBox(m.TypeId, null);
            row.userData = m.Key;

            var mid = new VisualElement();
            mid.AddToClassList("inv-row-mid");
            var top = new VisualElement();
            top.AddToClassList("inv-row-topline");
            var nameL = new Label(m.DisplayName);
            nameL.AddToClassList("inv-row-name");
            var qBadge = new Label($"x{m.Qty}");
            qBadge.AddToClassList("inv-row-qty-badge");
            top.Add(nameL);
            top.Add(qBadge);
            mid.Add(top);
            if (!string.IsNullOrEmpty(m.Sub))
            {
                var s = new Label(m.Sub);
                s.AddToClassList("inv-row-sub");
                mid.Add(s);
            }

            var btns = new VisualElement();
            btns.AddToClassList("inv-row-btns");

            if (isPicker)
            {
                if (m.PickerCanSelect)
                {
                    var b = new Button(() =>
                    {
                        var st = m.ItemOrNull != null && m.TypeId == Items.SporeGeneric ? m.ItemOrNull.SporeStageValue : (SporeStage?)null;
                        _onSelectedWithStage?.Invoke(m.TypeId, st, m.ItemOrNull);
                        Hide();
                    })
                    { text = LocalizationManager.GetString("inventory.select") };
                    b.AddToClassList("inv-select");
                    btns.Add(b);
                }
            }
            else
            {
                var bView = new Button(() => SelectRow(m, row, allowToggleOff: true)) { text = LocalizationManager.GetString("inventory.row.view") };
                bView.AddToClassList("inv-chip-btn");
                var useOk = ItemConsumptionHandler.IsConsumable(m.TypeId);
                var bUse = new Button(() => { SelectRow(m, row, allowToggleOff: false); RequestUse(m); })
                { text = LocalizationManager.GetString("inventory.row.use") };
                bUse.AddToClassList("inv-chip-btn");
                bUse.SetEnabled(useOk);
                if (!useOk) RegisterButtonTooltip(bUse, LocalizationManager.GetString("inventory.use_disabled_tt"));
                row.RegisterCallback<ClickEvent>(evt =>
                {
                    if (evt.target is VisualElement target && IsInside(target, btns)) return;
                    SelectRow(m, row, allowToggleOff: true);
                });
                btns.Add(bView);
                btns.Add(bUse);
            }

            row.Add(iconEl);
            row.Add(mid);
            row.Add(btns);
            RegisterRowTooltip(row, m.Tooltip);
            return row;
        }

        private void SelectRow(RowModel m, VisualElement row, bool allowToggleOff)
        {
            if (IsPickerMode(_pickerAllowedTypes)) return;
            if (allowToggleOff && _selectedRowKey == m.Key)
            {
                ClearDetailSelection(rebuild: false);
                return;
            }
            _selectedRowKey = m.Key;
            _selectedModel = m;
            if (_list != null)
            {
                foreach (var ve in _list.Query<VisualElement>(className: "inv-row").ToList())
                    ve.RemoveFromClassList("inv-row--selected");
            }
            row.AddToClassList("inv-row--selected");
            DetailPanelEnterHasSelection(m.TypeId);
            UpdateDetailPanel();
        }

        private void ClearDetailSelection(bool rebuild)
        {
            _selectedRowKey = null;
            _selectedModel = default;
            HideUseConfirm();
            HideInspectModal();
            DetailPanelEnterAwaiting();
            ApplyRowSelectionVisual();
            if (rebuild)
                Rebuild();
        }

        private void UpdateDetailPanel()
        {
            if (_detail == null) return;
            if (string.IsNullOrEmpty(_selectedRowKey)) return;
            var m = _selectedModel;
            if (string.IsNullOrEmpty(m.Key)) return;
            var typeId = m.TypeId;
            var item = m.ItemOrNull;
            if (_detailIdLine != null)
            {
                _detailIdLine.text = m.DisplayName;
            }
            if (_detailQty != null) _detailQty.text = $"x{m.Qty}";
            if (_detailSummary != null) _detailSummary.text = GetItemShortSummary(typeId, item, m);
            if (_detailMeta != null)
            {
                _detailMeta.enableRichText = true;
                _detailMeta.text = BuildDetailUsageBlock(typeId, item);
                _detailMeta.style.display = DisplayStyle.Flex;
            }
            if (_detailIcon != null)
            {
                _detailIcon.style.backgroundImage = null;
                var spr = m.TypeId == Items.SporeGeneric && item != null
                    ? GlobalIconResolver.GetItemIcon(Items.SporeGeneric, item.SporeStageValue)
                    : GlobalIconResolver.GetItemIcon(m.TypeId, null);
                if (spr != null) _detailIcon.style.backgroundImage = new StyleBackground(spr);
            }
            if (_btnDetailUse != null)
            {
                var ok = ItemConsumptionHandler.IsConsumable(typeId);
                _btnDetailUse.SetEnabled(ok);
                if (!ok) RegisterButtonTooltip(_btnDetailUse, LocalizationManager.GetString("inventory.use_disabled_tt"));
            }

            UpdateDecayBarUi(typeId, item);
        }

        private void UpdateDecayBarUi(string typeId, Item item)
        {
            if (_detailDecayWrap == null || _detailDecayFill == null || _detailDecayPct == null)
                return;

            var cfg = Resources.Load<ItemConfig>("Items/" + typeId);
            if (item == null || cfg == null || !ShouldShowDecayBar(cfg, typeId))
            {
                _detailDecayWrap.style.display = DisplayStyle.None;
                return;
            }

            float conditionPct = ComputeConditionPercent(item, cfg);
            _detailDecayWrap.style.display = DisplayStyle.Flex;
            var rounded = Mathf.Clamp(Mathf.RoundToInt(conditionPct), 0, 100);
            _detailDecayPct.text = $"{rounded}%";
            _detailDecayFill.style.width = Length.Percent(Mathf.Clamp(conditionPct, 0f, 100f));
            _detailDecayFill.style.backgroundColor = new StyleColor(GetConditionBarFillColor(rounded));
        }

        private static bool ShouldShowDecayBar(ItemConfig cfg, string typeId)
        {
            if (cfg == null || cfg.MaxQuality <= 0)
                return false;
            if (cfg.IsPerishable)
                return true;
            if (IsOrganicDeterioratingType(typeId))
                return true;
            return Items.IsFruitType(typeId);
        }

        /// <summary>Condizione residua 0–100%: 100 = Quality a max (fresco), 0 = Quality esaurita.</summary>
        private static float ComputeConditionPercent(Item item, ItemConfig cfg)
        {
            if (item == null || cfg == null || cfg.MaxQuality <= 0)
                return 0f;
            float max = Mathf.Max(1f, cfg.MaxQuality);
            return Mathf.Clamp01(item.Quality / max) * 100f;
        }

        /// <summary>Colore barra condizione: &gt;50% verde, 50% giallo, sotto 50% rosso (sulla percentuale arrotondata).</summary>
        private static Color GetConditionBarFillColor(int roundedPercent)
        {
            if (roundedPercent > 50)
                return new Color(0.32f, 0.88f, 0.48f, 0.98f);
            if (roundedPercent == 50)
                return new Color(0.95f, 0.82f, 0.22f, 0.98f);
            return new Color(0.92f, 0.32f, 0.32f, 0.98f);
        }

        private static string BuildExpectedUseEffectLine(string typeId, Item _)
        {
            if (ItemConsumptionHandler.IsConsumable(typeId))
            {
                if (typeId == Items.WaterPotable)
                    return LocalizationManager.GetString("inventory.detail.use_effect_water_potable");
                if (typeId == Items.Water)
                    return LocalizationManager.GetString("inventory.detail.use_effect_water_raw");
                if (typeId == Items.FoodVegetable || typeId == Items.FoodFungus || typeId == Items.FoodMeat)
                    return LocalizationManager.GetString("inventory.detail.use_effect_food");
                if (Items.IsFruitType(typeId))
                {
                    bool pure = typeId == Items.FruitArcticPod || typeId == Items.FruitsKnown;
                    return LocalizationManager.GetString(pure
                        ? "inventory.detail.use_effect_fruit_pure"
                        : "inventory.detail.use_effect_fruit_standard");
                }
            }

            return LocalizationManager.GetString("inventory.detail.use_effect_context");
        }

        private void OnDetailInspectClicked()
        {
            if (string.IsNullOrEmpty(_selectedRowKey)) return;
            var m = _selectedModel;
            if (string.IsNullOrEmpty(m.Key)) return;
            if (_inspectOverlay == null || _inspectBody == null) return;
            _inspectBody.enableRichText = true;
            var text = BuildDetailMetaBlock(m.TypeId, m.ItemOrNull, m);
            _inspectBody.text = text;
            _inspectOverlay.RemoveFromClassList("inv-inspect--hidden");
            _inspectOverlay.style.display = DisplayStyle.Flex;
            _inspectOverlay.BringToFront();
        }

        private void HideInspectModal()
        {
            if (_inspectOverlay == null) return;
            _inspectOverlay.style.display = DisplayStyle.None;
            _inspectOverlay.AddToClassList("inv-inspect--hidden");
        }

        private string GetItemShortSummary(string typeId, Item item, RowModel m)
        {
            if (typeId == Items.Water) return LocalizationManager.GetString("inventory.item_summary_water_raw");
            if (typeId == Items.WaterPotable) return LocalizationManager.GetString("inventory.item_summary_water_potable");
            if (typeId == Items.SporeGeneric) return LocalizationManager.GetString("inventory.item_summary_spore");
            if (typeId == Items.PreSeed) return LocalizationManager.GetString("inventory.item_summary_seed");
            if (typeId == Items.WholePlant) return LocalizationManager.GetString("inventory.item_summary_whole_plant");
            if (Items.IsFruitType(typeId)) return LocalizationManager.GetString("inventory.item_summary_fruit");
            if (typeId == Items.FoodVegetable || typeId == Items.FoodFungus || typeId == Items.FoodMeat) return LocalizationManager.GetString("inventory.item_summary_synth_food");
            if (typeId == Items.FertilizerStandard || typeId == Items.FertilizerPure || typeId == Items.FertilizerProhibited) return LocalizationManager.GetString("inventory.item_summary_tool");
            if (typeId == Items.AdditiveAcid || typeId == Items.AdditiveBasic || typeId == Items.ReagentX || typeId == Items.ReagentY) return LocalizationManager.GetString("inventory.item_summary_reagent");
            if (PlantDatabase.Instance != null && PlantDatabase.Instance.GetPlantDataBySeedTypeId(typeId) != null) return LocalizationManager.GetString("inventory.item_summary_seed");
            if (typeId is Items.StemCellVegetable or Items.StemCellFungus or Items.StemCellAnimal
                or Items.ProteinResidue or Items.OrganicResidue) return LocalizationManager.GetString("inventory.item_summary_bio");
            return LocalizationManager.GetString("inventory.item_summary_default");
        }

        private string BuildDetailMetaBlock(string typeId, Item it, RowModel m)
        {
            var na = LocalizationManager.GetString("inventory.detail.na");
            var lines = new List<string>();
            static string K(string txt) => $"<color=#7FAF97>{txt}</color>";
            static string V(string txt) => $"<color=#D8F5E3>{txt}</color>";

            // 1) Effetto atteso all'uso
            lines.Add($"{K(LocalizationManager.GetString("inventory.detail.use_effect_label"))}: {V(BuildExpectedUseEffectLine(typeId, it))}");

            // 3) Provenienza operativa
            var provenance = it != null ? ExtractorTooltipTexts.GetOriginTraceLabel(it) : na;
            lines.Add($"{K(LocalizationManager.GetString("inventory.detail.provenance"))}: {V(string.IsNullOrWhiteSpace(provenance) ? na : provenance)}");

            if (it != null)
            {
                // 4) Genetica
                if (it.GeneticTypeValue.HasValue)
                {
                    lines.Add($"{K(LocalizationManager.GetString("inventory.detail.genetics"))}: " +
                              V(ExtractorTooltipTexts.GeneticTypeToTrattiLabel(it.GeneticTypeValue) + " / " +
                                ExtractorTooltipTexts.GeneticTypeToPercentMutare(it.GeneticTypeValue) + " mut."));
                }

                // 6) Poteri
                if (!string.IsNullOrWhiteSpace(it.ActivePowerLabel) || !string.IsNullOrWhiteSpace(it.PassivePowerLabel))
                {
                    lines.Add($"{K(LocalizationManager.GetString("inventory.detail.powers"))}: " +
                              V((it.ActivePowerLabel ?? na) + " / " + (it.PassivePowerLabel ?? na)));
                }

                // 7) Famiglia / fazione
                var fam = ExtractorTooltipTexts.GetDisplayFamilyAlignment(it);
                lines.Add($"{K(LocalizationManager.GetString("inventory.detail.family_alignment"))}: {V(string.IsNullOrWhiteSpace(fam) || fam == "—" ? na : fam)}");
            }
            else
            {
                lines.Add($"{K(LocalizationManager.GetString("inventory.detail.family_alignment"))}: {V(na)}");
            }

            // 8) Economia config + stabilità
            var config = Resources.Load<ItemConfig>("Items/" + typeId);
            if (config != null)
            {
                lines.Add($"{K(LocalizationManager.GetString("inventory.detail.sell"))}: {V(config.SellPrice + " CRY")} / " +
                          $"{K(LocalizationManager.GetString("inventory.detail.buy"))}: {V(config.BuyPrice + " CRY")}");
                lines.Add($"{K(LocalizationManager.GetString("inventory.detail.stability"))}: {V(config.Stability.ToString("0.#"))}");
            }

            return string.Join("\n", lines);
        }

        private string BuildDetailUsageBlock(string typeId, Item it)
        {
            static string K(string txt) => $"<color=#7FAF97>{txt}</color>";
            static string V(string txt) => $"<color=#D8F5E3>{txt}</color>";

            string usedIn = GetUsageRoomLabel(typeId);
            string likes = GameLanguageSettings.GetEffectiveLanguage() == GameLanguage.Italian ? "tutti" : "everyone";

            string valueCry = "N/D";
            var cfg = Resources.Load<ItemConfig>("Items/" + typeId);
            if (cfg != null)
                valueCry = $"{cfg.SellPrice} CRY";

            if (GameLanguageSettings.GetEffectiveLanguage() == GameLanguage.Italian)
            {
                return $"{K("Si usa in")}: {V(usedIn)}  <color=#7FAF97>|</color>  " +
                       $"{K("Piace a")}: {V(likes)}  <color=#7FAF97>|</color>  " +
                       $"{K("Valore")}: {V(valueCry)}";
            }

            return $"{K("Used in")}: {V(usedIn)}  <color=#7FAF97>|</color>  " +
                   $"{K("Likes")}: {V(likes)}  <color=#7FAF97>|</color>  " +
                   $"{K("Value")}: {V(valueCry)}";
        }

        private static string GetUsageRoomLabel(string typeId)
        {
            bool it = GameLanguageSettings.GetEffectiveLanguage() == GameLanguage.Italian;
            if (typeId == Items.Water || typeId == Items.WaterPotable)
                return it ? "Food Room / Dome" : "Food Room / Dome";
            if (typeId == Items.FoodVegetable || typeId == Items.FoodFungus || typeId == Items.FoodMeat)
                return it ? "Food Room" : "Food Room";
            if (typeId == Items.ReagentX || typeId == Items.ReagentY || typeId == Items.AdditiveAcid || typeId == Items.AdditiveBasic)
                return it ? "Laboratorio" : "Lab";
            if (Items.IsFruitType(typeId) || typeId == Items.WholePlant || typeId == Items.PreSeed || typeId == Items.SporeGeneric)
                return it ? "Laboratorio / Dome" : "Lab / Dome";
            return it ? "Vault" : "Vault";
        }

        private void OnDetailUseClicked()
        {
            if (string.IsNullOrEmpty(_selectedRowKey)) return;
            RequestUse(_selectedModel);
        }

        private void RequestUse(RowModel m)
        {
            if (!ItemConsumptionHandler.IsConsumable(m.TypeId)) return;
            if (_confirm != null) _confirm.RemoveFromClassList("inv-confirm--hidden");
            if (_confirm != null) _confirm.style.display = DisplayStyle.Flex;
            if (_confirmBody != null) _confirmBody.text = LocalizationManager.GetString("inventory.confirm_use", new Dictionary<string, string> { { "name", m.DisplayName } });
        }

        private void OnConfirmUseYes()
        {
            if (string.IsNullOrEmpty(_selectedRowKey)) { HideUseConfirm(); return; }
            var m = _selectedModel;
            if (_playerInventory == null) { HideUseConfirm(); return; }
            if (m.IsPerItemRow && m.ItemOrNull != null)
                _playerInventory.ConsumeItemInstance(m.ItemOrNull);
            else
                _playerInventory.ConsumeItem(m.TypeId, 1);
            HideUseConfirm();
            _selectedRowKey = null;
            _selectedModel = default;
            DetailPanelEnterAwaiting();
            Rebuild();
        }

        private void HideUseConfirm()
        {
            if (_confirm == null) return;
            _confirm.style.display = DisplayStyle.None;
            _confirm.AddToClassList("inv-confirm--hidden");
        }

        private void UpdateStats()
        {
            if (_statH2o == null || _playerInventory == null) return;
            int l = GetSlotQuantity(_playerInventory, Items.Water) + GetSlotQuantity(_playerInventory, Items.WaterPotable);
            _statH2o.text = LocalizationManager.GetString("inventory.stats.h2o", new Dictionary<string, string> { { "n", l.ToString() } });

            int left = 0, max = 0;
            if (_gameManager != null && _gameManager.ActionSystem != null)
            {
                left = _gameManager.ActionSystem.ActionsLeft;
                max = _gameManager.ActionSystem.MaxActions;
            }
            if (_statActions != null) _statActions.text = LocalizationManager.GetString("inventory.stats.actions", new Dictionary<string, string> { { "l", left.ToString() }, { "m", max > 0 ? max.ToString() : "0" } });

            int total = 0;
            foreach (var s in _playerInventory.Items)
            {
                if (s == null) continue;
                total += s.Quantity;
            }
            if (_statItems != null) _statItems.text = LocalizationManager.GetString("inventory.stats.items", new Dictionary<string, string> { { "n", total.ToString() } });
        }

        private static int GetSlotQuantity(Inventory inv, string typeId)
        {
            if (inv == null) return 0;
            foreach (var s in inv.Items)
            {
                if (s != null && s.TypeId == typeId) return s.Quantity;
            }
            return 0;
        }

        private static bool IsInside(VisualElement candidate, VisualElement container)
        {
            if (candidate == null || container == null) return false;
            var current = candidate;
            while (current != null)
            {
                if (current == container) return true;
                current = current.parent;
            }
            return false;
        }

        private void RegisterButtonTooltip(Button b, string text)
        {
            b.tooltip = text;
        }

        private void RegisterRowTooltip(VisualElement row, string tooltipContent)
        {
            EnsureInvTooltip();
            if (_invTooltip == null || _invTooltipText == null) return;
            _invTooltip.pickingMode = PickingMode.Ignore;
            row.RegisterCallback<MouseEnterEvent>(evt =>
            {
                if (string.IsNullOrEmpty(tooltipContent)) return;
                _invTooltipText.text = tooltipContent;
                _invTooltip.style.display = DisplayStyle.Flex;
                _invTooltip.BringToFront();
                PositionTooltipAtMouse(row, evt.mousePosition);
            });
            row.RegisterCallback<MouseLeaveEvent>(_ => { _invTooltip.style.display = DisplayStyle.None; });
            row.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (_invTooltip.style.display != DisplayStyle.Flex) return;
                PositionTooltipAtMouse(row, evt.mousePosition);
            });
        }

        private void EnsureInvTooltip()
        {
            if (_invTooltip != null || _root == null) return;
            _invTooltip = _root.Q<VisualElement>("inv-tooltip");
            _invTooltipText = _invTooltip?.Q<Label>("inv-tooltip-text");
        }

        private void PositionTooltipAtMouse(VisualElement row, Vector2 mousePosPanel)
        {
            if (_invTooltip == null || _root == null) return;
            float x = mousePosPanel.x + 16f;
            float y = mousePosPanel.y + 12f;
            const float tw = 300f;
            float th = 80f;
            var bounds = _root.contentRect;
            if (x + tw > bounds.width) x = mousePosPanel.x - tw - 8f;
            if (y + th > bounds.height) y = mousePosPanel.y - th - 8f;
            if (y < 0f) y = 8f;
            if (x < 0f) x = 8f;
            _invTooltip.style.left = x;
            _invTooltip.style.top = y;
        }

        private void Start() => Hide();

        public static string GetItemDisplayName(string typeId, Item item = null)
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.CustomPlantName))
            {
                if (typeId == Items.WholePlant)
                    return ItemDisplayNameLocalization.GetWholePlantWithSpecies(item.CustomPlantName);
                return ItemDisplayNameLocalization.GetSeedWithSpecies(item.CustomPlantName);
            }
            if (typeId == Items.SporeGeneric && item != null && item.SporeStageValue.HasValue)
                return ItemDisplayNameLocalization.GetSporeTitle(item.SporeStageValue);
            return GetItemDisplayNameInternal(typeId);
        }

        private static string GetItemDisplayNameInternal(string typeId)
        {
            if (string.IsNullOrEmpty(typeId)) return typeId;
            if (ItemDisplayNameLocalization.TryGetByTypeId(typeId, out var localized)) return localized;
            if (Items.IsFruitType(typeId)) return ItemFabric.GetFruitDisplayNameByTypeId(typeId);
            if (PlantDatabase.Instance != null)
            {
                if (PlantDatabase.Instance.GetPlantDataBySeedTypeId(typeId) != null)
                    return PlantCardFormatters.GetSeedDisplayName(typeId);
            }
            return typeId;
        }

        private static string Tv(string value) => ExtractorTooltipTexts.WrapValue(value ?? "—");

        private static string BuildPreSeedItemTooltip(Item item)
        {
            if (item == null) return Tv(ItemDisplayNameLocalization.GetPreSeedTooltipTitleFallback());
            string trattiLabel = ExtractorTooltipTexts.GeneticTypeToTrattiLabel(item.GeneticTypeValue);
            string fa = string.IsNullOrWhiteSpace(item.ParentFamilyA) ? "—" : item.ParentFamilyA;
            string fb = string.IsNullOrWhiteSpace(item.ParentFamilyB) ? "—" : item.ParentFamilyB;
            string famiglie = $"{fa} + {fb}";
            string trattiCompat = string.IsNullOrWhiteSpace(item.CandidateTraitsCsv) ? "—" : item.CandidateTraitsCsv;
            string provenienza = ExtractorTooltipTexts.GetOriginTraceLabel(item);
            var lines = new List<string>
            {
                $"Tratti (fissati Step 3): {Tv(trattiLabel)}",
                $"Famiglie sorgente: {Tv(famiglie)}",
                $"Provenienza: {Tv(provenienza)}",
                $"Tratti compatibili: {Tv(trattiCompat)}"
            };
            if (!string.IsNullOrWhiteSpace(item.ActivePowerLabel))
                lines.Add($"Potere attivo ereditato: {Tv(item.ActivePowerLabel)}");
            if (!string.IsNullOrWhiteSpace(item.PassivePowerLabel))
                lines.Add($"Potere passivo ereditato: {Tv(item.PassivePowerLabel)}");
            return string.Join("\n", lines);
        }

        private static string BuildGenericItemTooltip(string typeId, string displayName, int qty, Item firstItem)
        {
            bool isSeed = PlantDatabase.Instance != null && PlantDatabase.Instance.GetPlantDataBySeedTypeId(typeId) != null;
            var lines = new List<string>
            {
                Tv(displayName),
                $"Quantità: {Tv(qty.ToString())}"
            };
            if (!isSeed)
                lines.Insert(1, $"Tipo: {Tv(typeId)}");
            if (firstItem != null)
            {
                if (firstItem.GeneticTypeValue.HasValue)
                {
                    string tratti = ExtractorTooltipTexts.GeneticTypeToTrattiLabel(firstItem.GeneticTypeValue);
                    lines.Add($"Tratti: {Tv(tratti)}");
                    lines.Add($"% di mutare: {Tv(ExtractorTooltipTexts.GeneticTypeToPercentMutare(firstItem.GeneticTypeValue))}");
                }
                if (!string.IsNullOrWhiteSpace(firstItem.FamilyMetadata))
                    lines.Add($"Famiglia: {Tv(firstItem.FamilyMetadata)}");
                if (!string.IsNullOrWhiteSpace(firstItem.SourcePlantDisplayName))
                    lines.Add($"Pianta sorgente: {Tv(firstItem.SourcePlantDisplayName)}");
                if (!string.IsNullOrWhiteSpace(firstItem.ActivePowerLabel))
                    lines.Add($"Potere attivo: {Tv(firstItem.ActivePowerLabel)}");
                if (!string.IsNullOrWhiteSpace(firstItem.PassivePowerLabel))
                    lines.Add($"Potere passivo: {Tv(firstItem.PassivePowerLabel)}");
                if (!string.IsNullOrWhiteSpace(firstItem.SelectedTraitsCsv))
                    lines.Add($"Tratti selezionati: {Tv(firstItem.SelectedTraitsCsv)}");
                if (firstItem.TraitPowerPercent > 0 && firstItem.TraitPowerPercent < 100)
                    lines.Add($"Potenza tratti: {Tv(firstItem.TraitPowerPercent.ToString() + "%")}");
                if (IsOrganicDeterioratingType(typeId))
                {
                    int days = Mathf.Max(0, Mathf.CeilToInt(firstItem.Quality));
                    lines.Add(Tv(LocalizationManager.GetString("inventory.decay_line",
                        new Dictionary<string, string> { { "days", days.ToString() } })));
                }
            }
            return string.Join("\n", lines);
        }

        private static bool IsOrganicDeterioratingType(string typeId)
        {
            if (string.IsNullOrWhiteSpace(typeId)) return false;
            if (typeId == Items.SporeGeneric
                || typeId == Items.WholePlant
                || typeId == Items.FoodVegetable
                || typeId == Items.FoodFungus
                || typeId == Items.FoodMeat)
                return true;
            return PlantDatabase.Instance != null && PlantDatabase.Instance.IsRegisteredSeedTypeId(typeId);
        }

        private static string BuildSporeItemTooltip(string displayName, Item item)
        {
            if (item == null)
                return Tv(displayName ?? "Spora");

            string tratti = ExtractorTooltipTexts.GeneticTypeToTrattiLabel(item.GeneticTypeValue);
            string percentMutare = ExtractorTooltipTexts.GeneticTypeToPercentMutare(item.GeneticTypeValue);
            string family = string.IsNullOrWhiteSpace(item.FamilyMetadata) ? "—" : item.FamilyMetadata;
            bool isRaw = item.SporeStageValue == SporeStage.Raw;
            string stato = isRaw ? "Raw (non combinabile)" : "Matura ✓ (pronta per fusione)";
            string provenienza = ExtractorTooltipTexts.GetOriginTraceLabel(item);

            var lines = new List<string>
            {
                $"Tratti: {Tv(tratti)}",
                $"% di mutare: {Tv(percentMutare)}",
                $"Famiglia: {Tv(family)}",
                $"Stato: {Tv(stato)}",
                $"Provenienza: {Tv(provenienza)}"
            };
            if (!string.IsNullOrWhiteSpace(item.ActivePowerLabel))
                lines.Add($"Potere attivo sorgente: {Tv(item.ActivePowerLabel)}");
            if (!string.IsNullOrWhiteSpace(item.PassivePowerLabel))
                lines.Add($"Potere passivo sorgente: {Tv(item.PassivePowerLabel)}");
            return string.Join("\n", lines);
        }

        private static string GetSporeInfoText(InventorySlot slot)
        {
            if (slot == null || slot.TypeId != Items.SporeGeneric || slot.Items.Count == 0) return "";
            var first = slot.Items.FirstOrDefault();
            if (first == null) return "";
            var parts = new List<string>();
            if (first.SporeStageValue.HasValue)
                parts.Add(ItemDisplayNameLocalization.GetSporeStageSubLabel(first.SporeStageValue.Value));
            if (first.GeneticTypeValue.HasValue)
                parts.Add(ExtractorTooltipTexts.GeneticTypeToTrattiLabel(first.GeneticTypeValue));
            if (parts.Count > 0)
                return string.Join(", ", parts);
            return GameLanguageSettings.GetEffectiveLanguage() == GameLanguage.Italian
                ? "Spora generica"
                : "Generic spore";
        }

        private static string GetSporeSubText(SporeStage? stage, GeneticType? genetic)
        {
            var parts = new List<string>();
            if (genetic.HasValue)
                parts.Add(ExtractorTooltipTexts.GeneticTypeToTrattiLabel(genetic));
            return parts.Count > 0 ? string.Join(", ", parts) : "";
        }

        private static VisualElement BuildItemIconBox(string typeId, SporeStage? sporeStage = null)
        {
            var box = new VisualElement();
            box.AddToClassList("inv-row-iconbox");
            box.focusable = false;
            var sprite = GlobalIconResolver.GetItemIcon(typeId, sporeStage);
            if (sprite != null)
                box.style.backgroundImage = new StyleBackground(sprite);
            return box;
        }
    }
}
