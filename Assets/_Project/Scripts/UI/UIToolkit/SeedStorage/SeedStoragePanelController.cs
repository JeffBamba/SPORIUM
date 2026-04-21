using System;
using System.Collections.Generic;
using System.Linq;
using _Project;
using _Project.Sporae.Core;
using _Project.Systems.SeedStorage;
using Sporae.Core;
using Sporae.DevTools;
using Sporae.Dome.PotSystem.Growth;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sporae.UI.UIToolkit.SeedStorage
{
    /// <summary>Pannello HUD Seed Storage (EXT-002) — NODO VAULT EXT-002.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-38)]
    public sealed class SeedStoragePanelController : MonoBehaviour
    {
        private const string VisualTreeResourcePath    = "UI/UIToolkit/SeedStorage/SeedStoragePanel";
        private const string PanelSettingsResourcePath = "UI/UIToolkit/MainMenu/MainMenuPanelSettings";
        private const int SortingOrder = 425;
        private const int LogMaxLines  = 6;

        private static SeedStoragePanelController _instance;

        // Root
        private UIDocument    _document;
        private VisualElement _root;
        private VisualElement _overlay;

        // Left panel
        private ScrollView    _invScroll;
        private VisualElement _catBotanical;
        private VisualElement _catSeeds;
        private VisualElement _catSpores;
        private Button        _btnDeposit;
        private Button        _btnWithdraw;

        // Right header
        private Button        _btnClose;
        private Button        _btnPower;
        private VisualElement _powerIndicator;
        private Label         _statusText;
        private Label         _tempLabel;
        private Label         _powerLabel;
        private Label         _systemStatusLabel;
        private Label         _occupiedLabel;
        private Label         _availableLabel;
        private Label         _dailyCostLabel;
        private Label         _apLabel;

        // Slot elements — length 6
        private readonly VisualElement[] _slotEls      = new VisualElement[SeedStorageSystem.SlotCount];
        private readonly Label[]         _slotTags     = new Label[SeedStorageSystem.SlotCount];
        private readonly Label[]         _slotBodies   = new Label[SeedStorageSystem.SlotCount];
        private readonly Label[]         _slotSubs     = new Label[SeedStorageSystem.SlotCount];
        private readonly VisualElement[] _slotIcons    = new VisualElement[SeedStorageSystem.SlotCount];
        private readonly VisualElement[] _slotViaRows  = new VisualElement[SeedStorageSystem.SlotCount];
        private readonly VisualElement[] _slotFills    = new VisualElement[SeedStorageSystem.SlotCount];
        private readonly Label[]         _slotViaPcts  = new Label[SeedStorageSystem.SlotCount];
        // Unlock buttons for tier-2 slots (index 0 → slot 3, etc.)
        private readonly Button[] _slotUnlockBtns = new Button[3];

        // Log
        private ScrollView         _logScroll;
        private readonly Queue<string> _logBuffer = new Queue<string>();

        // Selection
        private readonly HashSet<string> _depositSelectionTypeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<int>    _withdrawSlots           = new HashSet<int>();

        private GameManager       _gameManager;
        private SeedStorageSystem _seed;
        private bool _uiBound;
        private bool _systemsBound;

        public bool IsOpen { get; private set; }

        // ─── Category helpers ────────────────────────────────────────────────────

        private enum InvCategory { Botanical, Seeds, Spores }

        private static InvCategory GetCategory(string typeId)
        {
            if (typeId == Items.SporeGeneric) return InvCategory.Spores;
            var pdb = PlantDatabase.Instance;
            if (typeId == Items.PreSeed || typeId == Items.Seed001 ||
                typeId == Items.Seed002 || typeId == Items.Seed003 ||
                (pdb != null && pdb.IsRegisteredSeedTypeId(typeId)))
                return InvCategory.Seeds;
            return InvCategory.Botanical;
        }

        // "fruit-ferrio-pod" → "Fruit Ferrio Pod"
        private static string FormatItemName(string typeId)
        {
            if (string.IsNullOrEmpty(typeId)) return typeId;
            return string.Join(" ",
                typeId.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(w => char.ToUpperInvariant(w[0]) + w.Substring(1).ToLowerInvariant()));
        }

        // ─── Singleton ───────────────────────────────────────────────────────────

        public static SeedStoragePanelController EnsureInstance()
        {
            if (_instance != null) return _instance;
            var fromService = ServiceContainer.Instance?.Get<SeedStoragePanelController>(suppressWarning: true);
            if (fromService != null) { _instance = fromService; return _instance; }
            var go = new GameObject("[SeedStorageUI]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SeedStoragePanelController>();
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;

            var vta = Resources.Load<VisualTreeAsset>(VisualTreeResourcePath);
            if (vta == null) { SporiumLogger.LogError(LogCategory.UI, $"[SeedStorage] VisualTreeAsset mancante: {VisualTreeResourcePath}"); return; }

            var ps = Resources.Load<PanelSettings>(PanelSettingsResourcePath);
            if (ps == null) { SporiumLogger.LogError(LogCategory.UI, $"[SeedStorage] PanelSettings mancanti: {PanelSettingsResourcePath}"); return; }

            _document = GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();
            _document.panelSettings  = ps;
            _document.visualTreeAsset = vta;
            _document.sortingOrder   = SortingOrder;

            QueryElements();
            BindUi();
            InitLog();
            RefreshButtonStates();

            if (_root != null) _root.style.display = DisplayStyle.None;
            IsOpen = false;

            ServiceContainer.Instance?.Register(this);
        }

        private void OnDestroy()
        {
            UnbindSystems();
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Hide();
        }

        // ─── Query ───────────────────────────────────────────────────────────────

        private void QueryElements()
        {
            var ve = _document.rootVisualElement;
            _root    = ve.Q<VisualElement>("seedstorage-root");
            _overlay = ve.Q<VisualElement>("seedstorage-overlay");

            _invScroll    = ve.Q<ScrollView>("seedstorage-inv-scroll");
            _catBotanical = ve.Q<VisualElement>("seedstorage-cat-botanical");
            _catSeeds     = ve.Q<VisualElement>("seedstorage-cat-seeds");
            _catSpores    = ve.Q<VisualElement>("seedstorage-cat-spores");
            _btnDeposit   = ve.Q<Button>("btn-deposit");
            _btnWithdraw  = ve.Q<Button>("btn-withdraw");

            _btnClose          = ve.Q<Button>("btn-close");
            _btnPower          = ve.Q<Button>("btn-seedstorage-power");
            _powerIndicator    = ve.Q<VisualElement>("seedstorage-power-indicator");
            _statusText        = ve.Q<Label>("seedstorage-status-text");
            _tempLabel         = ve.Q<Label>("seedstorage-metric-temp");
            _powerLabel        = ve.Q<Label>("seedstorage-metric-humidity");
            _systemStatusLabel = ve.Q<Label>("seedstorage-metric-viability");
            _occupiedLabel     = ve.Q<Label>("seedstorage-occupied");
            _availableLabel    = ve.Q<Label>("seedstorage-available");
            _dailyCostLabel    = ve.Q<Label>("seedstorage-daily-cost");
            _apLabel           = ve.Q<Label>("seedstorage-ap");
            _logScroll         = ve.Q<ScrollView>("seedstorage-log-scroll");

            for (int i = 0; i < SeedStorageSystem.SlotCount; i++)
            {
                _slotEls[i]     = ve.Q<VisualElement>($"slot-{i}");
                _slotTags[i]    = ve.Q<Label>($"slot-{i}-title");
                _slotBodies[i]  = ve.Q<Label>($"slot-{i}-body");
                _slotSubs[i]    = ve.Q<Label>($"slot-{i}-sub");
                _slotIcons[i]   = ve.Q<VisualElement>($"slot-{i}-icon");
                _slotViaRows[i] = ve.Q<VisualElement>($"slot-{i}-via-row");
                _slotFills[i]   = ve.Q<VisualElement>($"slot-{i}-fill");
                _slotViaPcts[i] = ve.Q<Label>($"slot-{i}-via-pct");
            }

            for (int k = 0; k < 3; k++)
                _slotUnlockBtns[k] = ve.Q<Button>($"slot-{k + 3}-unlock-btn");
        }

        // ─── Bind ────────────────────────────────────────────────────────────────

        private void BindUi()
        {
            if (_uiBound) return;
            _uiBound = true;

            if (_btnClose    != null) _btnClose.clicked    += Hide;
            if (_btnPower    != null) _btnPower.clicked    += TogglePower;
            if (_btnDeposit  != null) _btnDeposit.clicked  += OnDepositClicked;
            if (_btnWithdraw != null) _btnWithdraw.clicked += OnWithdrawClicked;

            for (int k = 0; k < _slotUnlockBtns.Length; k++)
                if (_slotUnlockBtns[k] != null) _slotUnlockBtns[k].clicked += OnUnlockClicked;

            for (int i = 0; i < _slotEls.Length; i++)
            {
                int idx = i;
                if (_slotEls[i] != null)
                    _slotEls[i].RegisterCallback<ClickEvent>(_ => ToggleWithdrawSlot(idx));
            }
        }

        // ─── System binding (lazy) ────────────────────────────────────────────────

        private void EnsureSystems()
        {
            if (_gameManager == null)
            {
                UnbindSystems();
                _seed = null;
                _gameManager = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            }
            if (_seed == null)
                _seed = _gameManager?.SeedStorageSystem;

            if (_systemsBound || _seed == null || _gameManager == null) return;

            _seed.StorageChanged += OnStorageChanged;
            _seed.PowerChanged   += OnPowerChanged;
            if (_gameManager.PlayerInventory != null)
                _gameManager.PlayerInventory.OnInventoryChanged += OnStorageChanged;
            _systemsBound = true;
        }

        private void UnbindSystems()
        {
            if (!_systemsBound) return;
            if (_seed != null) { _seed.StorageChanged -= OnStorageChanged; _seed.PowerChanged -= OnPowerChanged; }
            if (_gameManager?.PlayerInventory != null)
                _gameManager.PlayerInventory.OnInventoryChanged -= OnStorageChanged;
            _systemsBound = false;
        }

        private void OnStorageChanged() => RefreshIfOpen();
        private void OnPowerChanged(bool _) => RefreshIfOpen();
        private void RefreshIfOpen() { if (IsOpen) Refresh(); }

        // ─── Show / Hide ─────────────────────────────────────────────────────────

        public void Show()
        {
            if (_root == null) return;
            EnsureSystems();
            _root.style.display = DisplayStyle.Flex;
            IsOpen = true;
            GameplayUiModalLock.SetBlockWorldInput(true);
            Refresh();
        }

        public void Hide()
        {
            if (_root != null) _root.style.display = DisplayStyle.None;
            IsOpen = false;
            GameplayUiModalLock.SetBlockWorldInput(false);
        }

        // ─── Actions ─────────────────────────────────────────────────────────────

        private void TogglePower()
        {
            if (_seed == null) return;
            _seed.SetPower(!_seed.IsOn);
            AppendLog(_seed.IsOn ? "> Alimentazione ON — crioconservazione attiva." : "> Alimentazione OFF — decadimento attivo sui contenuti.");
        }

        private void OnUnlockClicked()
        {
            if (_seed == null) return;
            if (_seed.TryUnlockExtendedSlots())
                AppendLog("> Slot 4-6 sbloccati (tier 2 · 3 CRY/giorno per slot occupato).");
            Refresh();
        }

        private void OnDepositClicked()
        {
            if (_seed == null || _gameManager == null) return;
            var items = CollectSelectedItems();
            if (items.Count == 0) { AppendLog("> Nessun item selezionato."); return; }
            if (_seed.TryDepositItems(items, out var err))
            {
                AppendLog($"> Depositati {items.Count} item (1 AP).");
                _depositSelectionTypeIds.Clear();
            }
            else
                AppendLog($"> Deposito fallito: {DescribeError(err)}.");
            Refresh();
        }

        private void OnWithdrawClicked()
        {
            if (_seed == null) return;
            if (_withdrawSlots.Count == 0) { AppendLog("> Nessuno slot selezionato."); return; }
            var list = new List<int>(_withdrawSlots);
            if (_seed.TryWithdrawFromSlots(list, out var err))
            {
                AppendLog($"> Prelevati {list.Count} slot (1 AP).");
                _withdrawSlots.Clear();
            }
            else
                AppendLog($"> Prelievo fallito: {DescribeError(err)}.");
            Refresh();
        }

        private void ToggleWithdrawSlot(int index)
        {
            if (_seed == null || !_seed.IsSlotUnlocked(index) || _seed.SlotIsEmpty(index)) return;
            if (_withdrawSlots.Contains(index)) _withdrawSlots.Remove(index);
            else _withdrawSlots.Add(index);
            RefreshSlotChrome();
            RefreshButtonStates();
        }

        // ─── Refresh ─────────────────────────────────────────────────────────────

        private void Refresh()
        {
            EnsureSystems();
            if (_seed == null || _gameManager == null) return;

            bool on   = _seed.IsOn;
            int  cost = _seed.ComputeDailyCryCost();

            if (_powerIndicator != null)
            {
                _powerIndicator.EnableInClassList("seedstorage-power-indicator--on",  on);
                _powerIndicator.EnableInClassList("seedstorage-power-indicator--off", !on);
            }
            if (_statusText != null)
            {
                _statusText.text = on ? "CONNESSIONE: STABILE" : "CONNESSIONE: OFFLINE";
                _statusText.EnableInClassList("seedstorage-status-text--on",  on);
                _statusText.EnableInClassList("seedstorage-status-text--off", !on);
            }
            if (_tempLabel         != null) _tempLabel.text         = on ? "❄ TEMP: -18°C" : "TEMP: —";
            if (_powerLabel        != null) _powerLabel.text        = on ? "⚡ ALIMENT.: ON" : "⚡ ALIMENT.: OFF";
            if (_systemStatusLabel != null) _systemStatusLabel.text = on ? "STATO SISTEMA: NOMINALE" : "STATO SISTEMA: OFFLINE";

            int occupied  = CountOccupied();
            int available = CountAvailable();
            if (_occupiedLabel  != null) _occupiedLabel.text  = $"OCCUPATI: {occupied}/6";
            if (_availableLabel != null) _availableLabel.text = $"DISPONIBILI: {available}";
            if (_dailyCostLabel != null) _dailyCostLabel.text = on ? $"COSTO GIORN.: {cost} CRY" : "COSTO GIORN.: 0 CRY (OFF)";
            if (_apLabel        != null) _apLabel.text        = $"AP: {_gameManager.ActionsLeft}";
            if (_btnPower       != null) _btnPower.text       = on ? "SPEGNI" : "ACCENDI";

            RebuildInventoryRows();
            RebuildSlots();
            RefreshSlotChrome();
            RefreshButtonStates();
        }

        private int CountOccupied()
        {
            if (_seed == null) return 0;
            int n = 0;
            for (int i = 0; i < SeedStorageSystem.SlotCount; i++)
                if (_seed.IsSlotUnlocked(i) && !_seed.SlotIsEmpty(i)) n++;
            return n;
        }

        private int CountAvailable()
        {
            if (_seed == null) return 0;
            int n = 0;
            for (int i = 0; i < SeedStorageSystem.SlotCount; i++)
                if (_seed.IsSlotUnlocked(i) && _seed.SlotIsEmpty(i)) n++;
            return n;
        }

        // ─── Button state (active highlight) ────────────────────────────────────

        private void RefreshButtonStates()
        {
            bool depositReady  = _depositSelectionTypeIds.Count > 0;
            bool withdrawReady = _withdrawSlots.Count > 0;

            if (_btnDeposit != null)
            {
                _btnDeposit.EnableInClassList("seedstorage-transfer-btn--active", depositReady);
                _btnDeposit.SetEnabled(depositReady);
            }
            if (_btnWithdraw != null)
            {
                _btnWithdraw.EnableInClassList("seedstorage-retrieve-btn--active", withdrawReady);
                _btnWithdraw.SetEnabled(withdrawReady);
            }
        }

        // ─── Inventory rows ───────────────────────────────────────────────────────

        private void RebuildInventoryRows()
        {
            if (_catBotanical == null || _catSeeds == null || _catSpores == null) return;

            ClearCategory(_catBotanical);
            ClearCategory(_catSeeds);
            ClearCategory(_catSpores);

            var inv = _gameManager?.PlayerInventory;
            if (inv == null)
            {
                AddEmptyPlaceholder(_catBotanical);
                AddEmptyPlaceholder(_catSeeds);
                AddEmptyPlaceholder(_catSpores);
                return;
            }

            var groups = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var slot in inv.Items)
                foreach (var item in slot.Items)
                    if (SeedStorageSystem.IsEligible(item))
                    {
                        groups.TryGetValue(item.TypeId, out int c);
                        groups[item.TypeId] = c + 1;
                    }

            int bCount = 0, sCount = 0, spCount = 0;
            foreach (var kvp in groups)
            {
                var cat = GetCategory(kvp.Key);
                var row = BuildInvGroupRow(kvp.Key, kvp.Value);
                switch (cat)
                {
                    case InvCategory.Seeds:   _catSeeds.Add(row);     sCount++;  break;
                    case InvCategory.Spores:  _catSpores.Add(row);    spCount++; break;
                    default:                  _catBotanical.Add(row); bCount++;  break;
                }
            }

            if (bCount  == 0) AddEmptyPlaceholder(_catBotanical);
            if (sCount  == 0) AddEmptyPlaceholder(_catSeeds);
            if (spCount == 0) AddEmptyPlaceholder(_catSpores);
        }

        private static void ClearCategory(VisualElement container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                var child = container[i];
                if (child.ClassListContains("seedstorage-inv-row") || child.ClassListContains("seedstorage-inv-empty"))
                    container.RemoveAt(i);
            }
        }

        private VisualElement BuildInvGroupRow(string typeId, int count)
        {
            var row = new VisualElement();
            row.AddToClassList("seedstorage-inv-row");
            if (_depositSelectionTypeIds.Contains(typeId))
                row.AddToClassList("seedstorage-inv-row--selected");

            // Icon box placeholder
            var icon = new VisualElement();
            icon.AddToClassList("seedstorage-icon-box");
            row.Add(icon);

            // Item name
            var nameLabel = new Label(FormatItemName(typeId));
            nameLabel.AddToClassList("seedstorage-inv-name");
            row.Add(nameLabel);

            // Qty on right
            var qtyLabel = new Label($"(x{count})");
            qtyLabel.AddToClassList("seedstorage-inv-qty");
            row.Add(qtyLabel);

            // Click to toggle selection
            string capturedId = typeId;
            row.RegisterCallback<ClickEvent>(_ =>
            {
                if (_depositSelectionTypeIds.Contains(capturedId))
                {
                    _depositSelectionTypeIds.Remove(capturedId);
                    row.RemoveFromClassList("seedstorage-inv-row--selected");
                }
                else
                {
                    _depositSelectionTypeIds.Add(capturedId);
                    row.AddToClassList("seedstorage-inv-row--selected");
                }
                RefreshButtonStates();
            });

            return row;
        }

        private List<Item> CollectSelectedItems()
        {
            var result = new List<Item>();
            if (_gameManager?.PlayerInventory == null) return result;
            foreach (var slot in _gameManager.PlayerInventory.Items)
                foreach (var item in slot.Items)
                    if (SeedStorageSystem.IsEligible(item) && _depositSelectionTypeIds.Contains(item.TypeId))
                        result.Add(item);
            return result;
        }

        private static void AddEmptyPlaceholder(VisualElement container)
        {
            var lbl = new Label("— vuoto");
            lbl.AddToClassList("seedstorage-inv-empty");
            container.Add(lbl);
        }

        // ─── Slot grid ────────────────────────────────────────────────────────────

        private void RebuildSlots()
        {
            if (_seed == null) return;
            for (int i = 0; i < _slotEls.Length; i++)
            {
                var el = _slotEls[i];
                if (el == null) continue;

                el.RemoveFromClassList("seedstorage-slot--empty");
                el.RemoveFromClassList("seedstorage-slot--locked");
                el.RemoveFromClassList("seedstorage-slot--occupied");

                bool isUnlocked = _seed.IsSlotUnlocked(i);
                bool isLocked   = !isUnlocked;
                bool isEmpty    = isUnlocked && _seed.SlotIsEmpty(i);

                if (_slotTags[i] != null)
                {
                    _slotTags[i].text = $"SLOT {i + 1}";
                    _slotTags[i].EnableInClassList("seedstorage-slot-tag--locked", isLocked);
                }

                int unlockBtnIdx = i - 3;
                if (unlockBtnIdx >= 0 && unlockBtnIdx < _slotUnlockBtns.Length && _slotUnlockBtns[unlockBtnIdx] != null)
                    _slotUnlockBtns[unlockBtnIdx].style.display = isLocked ? DisplayStyle.Flex : DisplayStyle.None;

                if (isLocked)
                {
                    el.AddToClassList("seedstorage-slot--locked");
                    if (_slotBodies[i] != null)
                    {
                        _slotBodies[i].text = "🔒";
                        _slotBodies[i].AddToClassList("seedstorage-slot-lock-icon");
                        _slotBodies[i].RemoveFromClassList("seedstorage-slot-body");
                    }
                    if (_slotSubs[i] != null) { _slotSubs[i].text = "SLOT BLOCCATO"; _slotSubs[i].style.display = DisplayStyle.Flex; }
                    SetSlotIconState(i, "locked");
                    SetViaRowVisible(i, false);
                    continue;
                }

                if (_slotBodies[i] != null)
                {
                    _slotBodies[i].RemoveFromClassList("seedstorage-slot-lock-icon");
                    _slotBodies[i].AddToClassList("seedstorage-slot-body");
                }

                if (isEmpty)
                {
                    el.AddToClassList("seedstorage-slot--empty");
                    if (_slotBodies[i] != null) _slotBodies[i].text = "SLOT VUOTO";
                    if (_slotSubs[i]   != null) { _slotSubs[i].text = "Pronto per lo storage"; _slotSubs[i].style.display = DisplayStyle.Flex; }
                    SetSlotIconState(i, "empty");
                    SetFill(i, 0f);
                    if (_slotViaPcts[i] != null) _slotViaPcts[i].text = "—";
                    SetViaRowVisible(i, false);
                    continue;
                }

                // Occupied
                el.AddToClassList("seedstorage-slot--occupied");
                string tid = _seed.GetSlotTypeId(i);
                int    qty = _seed.GetSlotQuantity(i);
                float  via = _seed.GetSlotViabilityRatio(i);
                if (_slotBodies[i] != null) _slotBodies[i].text = FormatItemName(tid ?? "—");
                if (_slotSubs[i]   != null) { _slotSubs[i].text = $"Quantità: {qty}"; _slotSubs[i].style.display = DisplayStyle.Flex; }
                SetSlotIconState(i, "occupied");
                SetFill(i, via);
                if (_slotViaPcts[i] != null) _slotViaPcts[i].text = $"{Mathf.RoundToInt(via * 100f)}%";
                SetViaRowVisible(i, true);
            }
        }

        private void SetSlotIconState(int i, string state)
        {
            var icon = _slotIcons[i];
            if (icon == null) return;
            icon.RemoveFromClassList("seedstorage-slot-icon--empty");
            icon.RemoveFromClassList("seedstorage-slot-icon--locked");
            icon.RemoveFromClassList("seedstorage-slot-icon--occupied");
            icon.AddToClassList($"seedstorage-slot-icon--{state}");
        }

        private void SetViaRowVisible(int i, bool visible)
        {
            if (_slotViaRows[i] != null)
                _slotViaRows[i].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetFill(int i, float ratio)
        {
            if (_slotFills[i] != null)
                _slotFills[i].style.width = Length.Percent(Mathf.Clamp01(ratio) * 100f);
        }

        private void RefreshSlotChrome()
        {
            for (int i = 0; i < _slotEls.Length; i++)
                if (_slotEls[i] != null)
                    _slotEls[i].EnableInClassList("seedstorage-slot--selected", _withdrawSlots.Contains(i));
        }

        // ─── Log ─────────────────────────────────────────────────────────────────

        private void InitLog()
        {
            if (_logScroll == null) return;
            _logScroll.Clear();
            _logBuffer.Clear();
            AddLogLabel("> Sistema inizializzato — Camere crioniche stabili");
            AddLogLabel("> Connessione stabilita — Terminale pronto");
        }

        private void AppendLog(string line)
        {
            if (_logScroll == null) return;
            _logBuffer.Enqueue(line);
            while (_logBuffer.Count > LogMaxLines) _logBuffer.Dequeue();
            _logScroll.Clear();
            foreach (var ln in _logBuffer) AddLogLabel(ln);
            _logScroll.scrollOffset = new Vector2(0, float.MaxValue);
        }

        private void AddLogLabel(string text)
        {
            var lbl = new Label(text);
            lbl.AddToClassList("seedstorage-log-line");
            _logScroll?.Add(lbl);
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private static string DescribeError(string err) => err switch
        {
            "no_ap"            => "AP insufficienti",
            "no_room"          => "spazio esaurito",
            "ineligible"       => "item non ammesso",
            "not_in_inventory" => "item non più disponibile",
            "remove_failed"    => "errore interno",
            "bad_slot"         => "slot non valido",
            "empty"            => "selezione vuota",
            _                  => err ?? "errore"
        };
    }
}
