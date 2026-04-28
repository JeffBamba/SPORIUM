using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using _Project;
using _Project.Sporae.Core;
using _Project.Systems.FoodRoom;
using Sporae.Core.Localization;
using Sporae.DevTools;

namespace Sporae.UI.UIToolkit.DispensaRefrigerata
{
    /// <summary>
    /// Controller del pannello HUD della Dispensa Refrigerata (cucina).
    /// Si appoggia a <see cref="FoodRoomSystem"/> (già esistente) per stato pantry, transfer e costi.
    /// L'UXML/USS sono dedicati, separati da FoodRoomPanel.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class DispensaPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _root;
        private VisualElement _overlay;
        private VisualElement _panel;
        private Button _btnClose;

        private VisualElement _powerIndicator;
        private Label _statusText;
        private Button _btnPower;
        private Label _infoPreservation;
        private VisualElement _infoDot;
        private Label _infoCost;

        private VisualElement _chamberFungal;
        private VisualElement _chamberMeat;
        private VisualElement _chamberVegetal;
        private Label _chamberFungalQty;
        private Label _chamberMeatQty;
        private Label _chamberVegetalQty;
        private VisualElement _chamberFungalDot;
        private VisualElement _chamberMeatDot;
        private VisualElement _chamberVegetalDot;

        private VisualElement _invFungal;
        private VisualElement _invMeat;
        private VisualElement _invVegetal;
        private Button _btnStore;
        private Button _btnRetrieve;
        private Label _invFungalQty;
        private Label _invMeatQty;
        private Label _invVegetalQty;
        private Label _invFungalTime;
        private Label _invMeatTime;
        private Label _invVegetalTime;
        private Label _logLine1;
        private Label _logLine2;

        private GameManager _gameManager;
        private FoodRoomSystem _foodRoom;
        private bool _uiBound;
        private bool _isOpen;
        private string _lastLogMsgKey = "dispensa.log.chambers_ready";
        private IReadOnlyDictionary<string, string> _lastLogMsgArgs;
        private FoodProductionType _selectedChamberType = FoodProductionType.None;
        private FoodProductionType _selectedInventoryType = FoodProductionType.None;

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
                _uiDocument.sortingOrder = 420;
            }
        }

        private void Start()
        {
            EnsureSystems();
            _root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (_root != null)
                BindAndSubscribe();
            if (_gameManager?.PlayerInventory != null)
                _gameManager.PlayerInventory.OnInventoryChanged += OnInventoryChanged;
            GameLanguageSettings.OnLanguageChanged += OnLanguageChanged;
            Hide();
        }

        private void OnDestroy()
        {
            if (_isOpen)
                GameplayUiModalLock.SetMachineModalState(false);
            GameLanguageSettings.OnLanguageChanged -= OnLanguageChanged;
            if (_gameManager?.PlayerInventory != null)
                _gameManager.PlayerInventory.OnInventoryChanged -= OnInventoryChanged;
        }

        private void OnLanguageChanged(GameLanguage _)
        {
            ApplyLocalizedDispensaStaticChrome();
            if (_isOpen)
                Refresh();
        }

        private void Update()
        {
            if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
                Hide();
        }

        private void OnInventoryChanged() => Refresh();

        private void EnsureSystems()
        {
            if (_gameManager == null)
                _gameManager = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            if (_foodRoom == null)
                _foodRoom = _gameManager?.FoodRoomSystem;
        }

        private void BindAndSubscribe()
        {
            if (_uiBound) return;
            if (_root == null) return;

            _overlay = _root.Q<VisualElement>("dispensa-root");
            _panel = _root.Q<VisualElement>("dispensa-panel");
            _btnClose = _root.Q<Button>("btn-close");
            if (_btnClose != null) _btnClose.clicked += Hide;

            _powerIndicator = _root.Q<VisualElement>("dispensa-power-indicator");
            _statusText = _root.Q<Label>("dispensa-status-text");
            _btnPower = _root.Q<Button>("btn-dispensa-power");
            if (_btnPower != null) _btnPower.clicked += OnTogglePower;

            _infoPreservation = _root.Q<Label>("dispensa-info-preservation");
            _infoDot = _root.Q<VisualElement>("dispensa-info-dot");
            _infoCost = _root.Q<Label>("dispensa-info-cost");

            _chamberFungal = _root.Q<VisualElement>("chamber-fungal");
            _chamberMeat = _root.Q<VisualElement>("chamber-meat");
            _chamberVegetal = _root.Q<VisualElement>("chamber-vegetal");
            _chamberFungalQty = _root.Q<Label>("chamber-fungal-qty");
            _chamberMeatQty = _root.Q<Label>("chamber-meat-qty");
            _chamberVegetalQty = _root.Q<Label>("chamber-vegetal-qty");
            _chamberFungalDot = _root.Q<VisualElement>("chamber-fungal-dot");
            _chamberMeatDot = _root.Q<VisualElement>("chamber-meat-dot");
            _chamberVegetalDot = _root.Q<VisualElement>("chamber-vegetal-dot");

            if (_chamberFungal != null) _chamberFungal.RegisterCallback<ClickEvent>(_ => OnChamberClicked(FoodProductionType.Fungus));
            if (_chamberMeat != null) _chamberMeat.RegisterCallback<ClickEvent>(_ => OnChamberClicked(FoodProductionType.Meat));
            if (_chamberVegetal != null) _chamberVegetal.RegisterCallback<ClickEvent>(_ => OnChamberClicked(FoodProductionType.Vegetable));

            _invFungal = _root.Q<VisualElement>("inv-fungal");
            _invMeat = _root.Q<VisualElement>("inv-meat");
            _invVegetal = _root.Q<VisualElement>("inv-vegetal");
            _btnStore = _root.Q<Button>("btn-dispensa-store");
            _btnRetrieve = _root.Q<Button>("btn-dispensa-retrieve");
            _invFungalQty = _root.Q<Label>("inv-fungal-qty");
            _invMeatQty = _root.Q<Label>("inv-meat-qty");
            _invVegetalQty = _root.Q<Label>("inv-vegetal-qty");
            _invFungalTime = _root.Q<Label>("inv-fungal-time");
            _invMeatTime = _root.Q<Label>("inv-meat-time");
            _invVegetalTime = _root.Q<Label>("inv-vegetal-time");
            _logLine1 = _root.Q<Label>("log-line-1");
            _logLine2 = _root.Q<Label>("log-line-2");

            if (_invFungal != null) _invFungal.RegisterCallback<ClickEvent>(_ => OnInventoryRowSelected(FoodProductionType.Fungus));
            if (_invMeat != null) _invMeat.RegisterCallback<ClickEvent>(_ => OnInventoryRowSelected(FoodProductionType.Meat));
            if (_invVegetal != null) _invVegetal.RegisterCallback<ClickEvent>(_ => OnInventoryRowSelected(FoodProductionType.Vegetable));
            if (_btnStore != null) _btnStore.clicked += OnStoreClicked;
            if (_btnRetrieve != null) _btnRetrieve.clicked += OnRetrieveClicked;

            _uiBound = true;
            ApplyLocalizedDispensaStaticChrome();
        }

        private void ApplyLocalizedDispensaStaticChrome()
        {
            if (_overlay == null) return;

            var panelTitle = _overlay.Q<VisualElement>("dispensa-title-row")?.Q<Label>(className: "dispensa-title");
            if (panelTitle != null) panelTitle.text = LocalizationManager.GetString("dispensa.chrome.title");

            var subtitle = _overlay.Q<Label>(className: "dispensa-subtitle");
            if (subtitle != null) subtitle.text = LocalizationManager.GetString("dispensa.chrome.subtitle");

            var chambersCol = _overlay.Q<VisualElement>("dispensa-chambers-column");
            var chambersSectionTitle = chambersCol?.Q<Label>(className: "dispensa-section-title");
            if (chambersSectionTitle != null) chambersSectionTitle.text = LocalizationManager.GetString("dispensa.chrome.section_chambers");

            var invTitle = _overlay.Q<Label>(className: "dispensa-inventory-title");
            if (invTitle != null) invTitle.text = LocalizationManager.GetString("dispensa.chrome.section_inv");

            var tempInfo = _overlay.Q<Label>("dispensa-info-temp");
            if (tempInfo != null) tempInfo.text = LocalizationManager.GetString("dispensa.chrome.info_temp");
            var humInfo = _overlay.Q<Label>("dispensa-info-humidity");
            if (humInfo != null) humInfo.text = LocalizationManager.GetString("dispensa.chrome.info_humidity");

            var nameF = _chamberFungal?.Q<Label>(className: "dispensa-chamber-name");
            if (nameF != null) nameF.text = LocalizationManager.GetString("dispensa.chrome.chamber_fungal");
            var nameM = _chamberMeat?.Q<Label>(className: "dispensa-chamber-name");
            if (nameM != null) nameM.text = LocalizationManager.GetString("dispensa.chrome.chamber_meat");
            var nameV = _chamberVegetal?.Q<Label>(className: "dispensa-chamber-name");
            if (nameV != null) nameV.text = LocalizationManager.GetString("dispensa.chrome.chamber_vegetal");

            var invF = _invFungal?.Q<Label>(className: "dispensa-inv-name");
            if (invF != null) invF.text = LocalizationManager.GetString("dispensa.food.fungus");
            var invM = _invMeat?.Q<Label>(className: "dispensa-inv-name");
            if (invM != null) invM.text = LocalizationManager.GetString("dispensa.food.meat");
            var invV = _invVegetal?.Q<Label>(className: "dispensa-inv-name");
            if (invV != null) invV.text = LocalizationManager.GetString("dispensa.food.vegetable");
            if (_btnStore != null) _btnStore.text = LocalizationManager.GetString("dispensa.btn.store");
            if (_btnRetrieve != null) _btnRetrieve.text = LocalizationManager.GetString("dispensa.btn.retrieve");
        }

        public void Show()
        {
            if (_root == null) _root = _uiDocument?.rootVisualElement;
            if (_root != null && !_uiBound) BindAndSubscribe();
            EnsureSystems();
            if (_gameManager?.PlayerInventory != null)
            {
                _gameManager.PlayerInventory.OnInventoryChanged -= OnInventoryChanged;
                _gameManager.PlayerInventory.OnInventoryChanged += OnInventoryChanged;
            }
            if (_uiDocument != null) _uiDocument.sortingOrder = 1000;
            if (_overlay != null)
            {
                GameplayUiModalLock.SetMachineModalState(true);
                _overlay.style.display = DisplayStyle.Flex;
                var innerOverlay = _root.Q<VisualElement>("dispensa-overlay");
                if (innerOverlay != null)
                    innerOverlay.style.display = DisplayStyle.Flex;
            }
            _foodRoom?.BeginPantryInteraction();
            _isOpen = true;
            _selectedInventoryType = FoodProductionType.None;
            _selectedChamberType = FoodProductionType.None;
            ApplyLocalizedDispensaStaticChrome();
            PushLoc("dispensa.log.panel_linked");
            Refresh();
        }

        public void Hide()
        {
            bool wasOpen = _isOpen;
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.None;
                var innerOverlay = _root.Q<VisualElement>("dispensa-overlay");
                if (innerOverlay != null)
                    innerOverlay.style.display = DisplayStyle.None;
            }
            if (_uiDocument != null) _uiDocument.sortingOrder = 420;
            _foodRoom?.EndPantryInteraction();
            _isOpen = false;
            _selectedInventoryType = FoodProductionType.None;
            _selectedChamberType = FoodProductionType.None;
            if (wasOpen)
                GameplayUiModalLock.SetMachineModalState(false);
        }

        public bool IsVisible => _isOpen;

        private void OnTogglePower()
        {
            if (_foodRoom == null) return;
            _foodRoom.SetPantryPower(!_foodRoom.PantryIsOn);
            PushLoc(_foodRoom.PantryIsOn ? "dispensa.log.ref_online" : "dispensa.log.ref_offline");
            Refresh();
        }

        private void OnChamberClicked(FoodProductionType type)
        {
            if (_foodRoom == null) return;
            _selectedChamberType = type;
            Refresh();
        }

        private void OnInventoryRowSelected(FoodProductionType type)
        {
            if (_foodRoom == null) return;
            _selectedInventoryType = type;
            Refresh();
        }

        private void OnStoreClicked()
        {
            if (_foodRoom == null) return;
            if (_selectedInventoryType == FoodProductionType.None)
                return;
            if (!_foodRoom.PantryIsOn)
            {
                SporiumLogger.LogInfo(LogCategory.UI, "[DispensaPanel] Impossibile inserire: refrigerazione OFF.");
                PushLoc("dispensa.log.insert_blocked");
                Refresh();
                return;
            }
            var type = _selectedInventoryType;
            string typeId = GetFoodTypeId(type);
            if (string.IsNullOrEmpty(typeId) || GetInventoryQuantity(typeId) <= 0)
                return;
            if (_foodRoom.TryTransferToPantry(type, 1, out _))
                PushLoc("dispensa.log.stored", new Dictionary<string, string> { { "name", GetFoodTypeUiName(type) } });
            else
                PushLoc("dispensa.log.ap_blocked");
            Refresh();
        }

        private void OnRetrieveClicked()
        {
            if (_foodRoom == null) return;
            if (_selectedChamberType == FoodProductionType.None)
                return;
            var type = _selectedChamberType;
            if (_foodRoom.GetPantryQuantity(type) <= 0)
                return;
            if (_foodRoom.TryTransferFromPantry(type, 1, out _))
                PushLoc("dispensa.log.removed", new Dictionary<string, string> { { "name", GetFoodTypeUiName(type) } });
            else
                PushLoc("dispensa.log.ap_blocked");
            Refresh();
        }

        private void Refresh()
        {
            EnsureSystems();
            if (_foodRoom == null) return;

            bool isOn = _foodRoom.PantryIsOn;

            if (_statusText != null)
            {
                _statusText.text = isOn
                    ? LocalizationManager.GetString("dispensa.status.online")
                    : LocalizationManager.GetString("dispensa.status.offline");
                _statusText.RemoveFromClassList("dispensa-status-text--on");
                _statusText.RemoveFromClassList("dispensa-status-text--off");
                _statusText.AddToClassList(isOn ? "dispensa-status-text--on" : "dispensa-status-text--off");
            }
            if (_powerIndicator != null)
            {
                _powerIndicator.RemoveFromClassList("dispensa-power-indicator--on");
                _powerIndicator.RemoveFromClassList("dispensa-power-indicator--off");
                _powerIndicator.AddToClassList(isOn ? "dispensa-power-indicator--on" : "dispensa-power-indicator--off");
            }
            if (_btnPower != null)
                _btnPower.text = isOn
                    ? LocalizationManager.GetString("dispensa.btn.turn_off")
                    : LocalizationManager.GetString("dispensa.btn.turn_on");
            if (_infoPreservation != null)
            {
                _infoPreservation.text = isOn
                    ? LocalizationManager.GetString("dispensa.preservation.optimal")
                    : LocalizationManager.GetString("dispensa.preservation.offline");
                _infoPreservation.RemoveFromClassList("dispensa-info-text--ok");
                if (isOn) _infoPreservation.AddToClassList("dispensa-info-text--ok");
            }
            if (_infoDot != null)
            {
                _infoDot.RemoveFromClassList("dispensa-info-dot--ok");
                _infoDot.RemoveFromClassList("dispensa-info-dot--off");
                _infoDot.AddToClassList(isOn ? "dispensa-info-dot--ok" : "dispensa-info-dot--off");
            }
            if (_infoCost != null)
                _infoCost.text = LocalizationManager.GetString("dispensa.maintenance",
                    new Dictionary<string, string> { { "cry", _foodRoom.PantryDailyCost.ToString() } });

            ApplyOfflineVisualState(isOn);

            int vegStored = _foodRoom.GetPantryQuantity(FoodProductionType.Vegetable);
            int fungStored = _foodRoom.GetPantryQuantity(FoodProductionType.Fungus);
            int meatStored = _foodRoom.GetPantryQuantity(FoodProductionType.Meat);

            SetChamberQtyUi(_chamberVegetalQty, _chamberVegetal, _chamberVegetalDot, vegStored);
            SetChamberQtyUi(_chamberFungalQty, _chamberFungal, _chamberFungalDot, fungStored);
            SetChamberQtyUi(_chamberMeatQty, _chamberMeat, _chamberMeatDot, meatStored);

            int invVeg = GetInventoryQuantity(Items.FoodVegetable);
            int invFung = GetInventoryQuantity(Items.FoodFungus);
            int invMeat = GetInventoryQuantity(Items.FoodMeat);

            if (_invVegetalQty != null)
                _invVegetalQty.text = LocalizationManager.GetString("dispensa.inv.qty", new Dictionary<string, string> { { "n", invVeg.ToString() } });
            if (_invFungalQty != null)
                _invFungalQty.text = LocalizationManager.GetString("dispensa.inv.qty", new Dictionary<string, string> { { "n", invFung.ToString() } });
            if (_invMeatQty != null)
                _invMeatQty.text = LocalizationManager.GetString("dispensa.inv.qty", new Dictionary<string, string> { { "n", invMeat.ToString() } });

            if (_invVegetalTime != null) _invVegetalTime.text = FormatDeteriorationTime(Items.FoodVegetable);
            if (_invFungalTime != null) _invFungalTime.text = FormatDeteriorationTime(Items.FoodFungus);
            if (_invMeatTime != null) _invMeatTime.text = FormatDeteriorationTime(Items.FoodMeat);

            SetInvRowInteractive(_invVegetal, isOn && invVeg > 0);
            SetInvRowInteractive(_invFungal, isOn && invFung > 0);
            SetInvRowInteractive(_invMeat, isOn && invMeat > 0);
            UpdateSelectionVisuals(invVeg, invFung, invMeat);
            RefreshTransferButtons(isOn, invVeg, invFung, invMeat, vegStored, fungStored, meatStored);

            if (_logLine1 != null)
                _logLine1.text = isOn
                    ? LocalizationManager.GetString("dispensa.footer.system_online")
                    : LocalizationManager.GetString("dispensa.footer.system_offline");
            if (_logLine2 != null)
                _logLine2.text = "› " + LocalizationManager.GetString(_lastLogMsgKey, _lastLogMsgArgs);
        }

        private void ApplyOfflineVisualState(bool isOn)
        {
            _panel?.EnableInClassList("dispensa-panel--offline", !isOn);

            _chamberFungal?.SetEnabled(isOn);
            _chamberMeat?.SetEnabled(isOn);
            _chamberVegetal?.SetEnabled(isOn);
            _invFungal?.SetEnabled(isOn);
            _invMeat?.SetEnabled(isOn);
            _invVegetal?.SetEnabled(isOn);

            // Restano sempre operativi anche in OFF.
            _btnPower?.SetEnabled(true);
            _btnClose?.SetEnabled(true);
        }

        private void RefreshTransferButtons(
            bool isOn,
            int invVeg,
            int invFung,
            int invMeat,
            int pantryVeg,
            int pantryFung,
            int pantryMeat)
        {
            bool canStore = isOn && _selectedInventoryType switch
            {
                FoodProductionType.Vegetable => invVeg > 0,
                FoodProductionType.Fungus => invFung > 0,
                FoodProductionType.Meat => invMeat > 0,
                _ => false
            };
            bool canRetrieve = isOn && _selectedChamberType switch
            {
                FoodProductionType.Vegetable => pantryVeg > 0,
                FoodProductionType.Fungus => pantryFung > 0,
                FoodProductionType.Meat => pantryMeat > 0,
                _ => false
            };

            if (_btnStore != null)
            {
                _btnStore.EnableInClassList("dispensa-transfer-btn--active", canStore);
                _btnStore.SetEnabled(canStore);
            }
            if (_btnRetrieve != null)
            {
                _btnRetrieve.EnableInClassList("dispensa-retrieve-btn--active", canRetrieve);
                _btnRetrieve.SetEnabled(canRetrieve);
            }
        }

        private void UpdateSelectionVisuals(int invVeg, int invFung, int invMeat)
        {
            UpdateInvSelection(_invVegetal, _selectedInventoryType == FoodProductionType.Vegetable && invVeg > 0);
            UpdateInvSelection(_invFungal, _selectedInventoryType == FoodProductionType.Fungus && invFung > 0);
            UpdateInvSelection(_invMeat, _selectedInventoryType == FoodProductionType.Meat && invMeat > 0);

            UpdateChamberSelection(_chamberVegetal, _selectedChamberType == FoodProductionType.Vegetable);
            UpdateChamberSelection(_chamberFungal, _selectedChamberType == FoodProductionType.Fungus);
            UpdateChamberSelection(_chamberMeat, _selectedChamberType == FoodProductionType.Meat);
        }

        private static void SetChamberQtyUi(Label qtyLabel, VisualElement card, VisualElement dot, int stored)
        {
            if (qtyLabel != null)
                qtyLabel.text = stored > 0
                    ? LocalizationManager.GetString("dispensa.chamber.stored", new Dictionary<string, string> { { "n", stored.ToString() } })
                    : LocalizationManager.GetString("dispensa.chamber.empty");
            if (dot != null)
            {
                dot.RemoveFromClassList("filled");
                if (stored > 0) dot.AddToClassList("filled");
            }
            if (card != null)
            {
                card.EnableInClassList("dispensa-chamber-card--filled", stored > 0);
            }
        }

        private static void UpdateInvSelection(VisualElement row, bool selected)
        {
            if (row == null) return;
            row.EnableInClassList("dispensa-inv-row--selected", selected);
        }

        private static void UpdateChamberSelection(VisualElement card, bool selected)
        {
            if (card == null) return;
            card.EnableInClassList("selected", selected);
        }

        private static void SetInvRowInteractive(VisualElement row, bool interactive)
        {
            if (row == null) return;
            row.RemoveFromClassList("dispensa-inv-row--disabled");
            if (!interactive) row.AddToClassList("dispensa-inv-row--disabled");
        }

        private int GetInventoryQuantity(string typeId)
        {
            if (_gameManager?.PlayerInventory == null || string.IsNullOrWhiteSpace(typeId))
                return 0;
            foreach (var slot in _gameManager.PlayerInventory.Items)
            {
                if (slot.TypeId == typeId)
                    return slot.Quantity;
            }
            return 0;
        }

        private int GetMinQualityInInventory(string typeId)
        {
            if (_gameManager?.PlayerInventory == null || string.IsNullOrWhiteSpace(typeId))
                return 0;
            int min = int.MaxValue;
            foreach (var slot in _gameManager.PlayerInventory.Items)
            {
                if (slot.TypeId != typeId) continue;
                foreach (var item in slot.Items)
                {
                    if (item == null) continue;
                    int q = (int)item.Quality;
                    if (q < min) min = q;
                }
            }
            return min == int.MaxValue ? 0 : min;
        }

        private string FormatDeteriorationTime(string typeId)
        {
            int qty = GetInventoryQuantity(typeId);
            if (qty <= 0) return "—";
            int days = GetMinQualityInInventory(typeId);
            if (days <= 0) return "—";
            return $"+{days}d";
        }

        private void PushLoc(string key, IReadOnlyDictionary<string, string> args = null)
        {
            if (string.IsNullOrEmpty(key))
                return;
            _lastLogMsgKey = key;
            _lastLogMsgArgs = args;
        }

        private static string GetFoodTypeId(FoodProductionType type)
        {
            switch (type)
            {
                case FoodProductionType.Vegetable: return Items.FoodVegetable;
                case FoodProductionType.Fungus: return Items.FoodFungus;
                case FoodProductionType.Meat: return Items.FoodMeat;
                default: return null;
            }
        }

        private static string GetFoodTypeUiName(FoodProductionType type)
        {
            switch (type)
            {
                case FoodProductionType.Vegetable: return LocalizationManager.GetString("dispensa.food.vegetable");
                case FoodProductionType.Fungus: return LocalizationManager.GetString("dispensa.food.fungus");
                case FoodProductionType.Meat: return LocalizationManager.GetString("dispensa.food.meat");
                default: return LocalizationManager.GetString("dispensa.food.generic");
            }
        }
    }
}
