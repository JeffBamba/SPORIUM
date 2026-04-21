using UnityEngine;
using UnityEngine.UIElements;
using _Project;
using _Project.Sporae.Core;
using _Project.Systems.FoodRoom;
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
        private string _lastLogMessage = "Storage chambers ready";

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
            Hide();
        }

        private void OnDestroy()
        {
            if (_gameManager?.PlayerInventory != null)
                _gameManager.PlayerInventory.OnInventoryChanged -= OnInventoryChanged;
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
            _invFungalQty = _root.Q<Label>("inv-fungal-qty");
            _invMeatQty = _root.Q<Label>("inv-meat-qty");
            _invVegetalQty = _root.Q<Label>("inv-vegetal-qty");
            _invFungalTime = _root.Q<Label>("inv-fungal-time");
            _invMeatTime = _root.Q<Label>("inv-meat-time");
            _invVegetalTime = _root.Q<Label>("inv-vegetal-time");
            _logLine1 = _root.Q<Label>("log-line-1");
            _logLine2 = _root.Q<Label>("log-line-2");

            if (_invFungal != null) _invFungal.RegisterCallback<ClickEvent>(_ => OnInventoryRowClicked(FoodProductionType.Fungus));
            if (_invMeat != null) _invMeat.RegisterCallback<ClickEvent>(_ => OnInventoryRowClicked(FoodProductionType.Meat));
            if (_invVegetal != null) _invVegetal.RegisterCallback<ClickEvent>(_ => OnInventoryRowClicked(FoodProductionType.Vegetable));

            _uiBound = true;
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
                _overlay.style.display = DisplayStyle.Flex;
                var innerOverlay = _root.Q<VisualElement>("dispensa-overlay");
                if (innerOverlay != null)
                    innerOverlay.style.display = DisplayStyle.Flex;
            }
            _isOpen = true;
            PushLog("Panel linked to FoodRoomSystem");
            Refresh();
        }

        public void Hide()
        {
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.None;
                var innerOverlay = _root.Q<VisualElement>("dispensa-overlay");
                if (innerOverlay != null)
                    innerOverlay.style.display = DisplayStyle.None;
            }
            if (_uiDocument != null) _uiDocument.sortingOrder = 420;
            _isOpen = false;
        }

        public bool IsVisible => _isOpen;

        private void OnTogglePower()
        {
            if (_foodRoom == null) return;
            _foodRoom.SetPantryPower(!_foodRoom.PantryIsOn);
            PushLog(_foodRoom.PantryIsOn ? "Refrigeration system online" : "Refrigeration system offline");
            Refresh();
        }

        private void OnChamberClicked(FoodProductionType type)
        {
            if (_foodRoom == null) return;
            if (_foodRoom.GetPantryQuantity(type) <= 0)
                return;
            _foodRoom.TryTransferFromPantry(type, 1, out _);
            PushLog($"Removed 1 {GetFoodTypeUiName(type)}");
            Refresh();
        }

        private void OnInventoryRowClicked(FoodProductionType type)
        {
            if (_foodRoom == null) return;
            if (!_foodRoom.PantryIsOn)
            {
                SporiumLogger.LogInfo(LogCategory.UI, "[DispensaPanel] Impossibile inserire: refrigerazione OFF.");
                PushLog("Insert blocked: refrigeration offline");
                return;
            }
            string typeId = GetFoodTypeId(type);
            if (string.IsNullOrEmpty(typeId) || GetInventoryQuantity(typeId) <= 0)
                return;
            _foodRoom.TryTransferToPantry(type, 1, out _);
            PushLog($"Stored 1 {GetFoodTypeUiName(type)}");
            Refresh();
        }

        private void Refresh()
        {
            EnsureSystems();
            if (_foodRoom == null) return;

            bool isOn = _foodRoom.PantryIsOn;

            if (_statusText != null)
            {
                _statusText.text = isOn ? "REFRIGERATION ONLINE" : "REFRIGERATION OFFLINE";
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
            if (_btnPower != null) _btnPower.text = isOn ? "TURN OFF" : "TURN ON";
            if (_infoPreservation != null)
            {
                _infoPreservation.text = isOn ? "PRESERVATION OPTIMAL" : "PRESERVATION OFFLINE";
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
                _infoCost.text = $"MANTENIMENTO: {_foodRoom.PantryDailyCost} CRY/giorno";

            int vegStored = _foodRoom.GetPantryQuantity(FoodProductionType.Vegetable);
            int fungStored = _foodRoom.GetPantryQuantity(FoodProductionType.Fungus);
            int meatStored = _foodRoom.GetPantryQuantity(FoodProductionType.Meat);

            SetChamberQtyUi(_chamberVegetalQty, _chamberVegetal, _chamberVegetalDot, vegStored);
            SetChamberQtyUi(_chamberFungalQty, _chamberFungal, _chamberFungalDot, fungStored);
            SetChamberQtyUi(_chamberMeatQty, _chamberMeat, _chamberMeatDot, meatStored);

            int invVeg = GetInventoryQuantity(Items.FoodVegetable);
            int invFung = GetInventoryQuantity(Items.FoodFungus);
            int invMeat = GetInventoryQuantity(Items.FoodMeat);

            if (_invVegetalQty != null) _invVegetalQty.text = $"QTY: {invVeg}";
            if (_invFungalQty != null) _invFungalQty.text = $"QTY: {invFung}";
            if (_invMeatQty != null) _invMeatQty.text = $"QTY: {invMeat}";

            if (_invVegetalTime != null) _invVegetalTime.text = FormatDeteriorationTime(Items.FoodVegetable);
            if (_invFungalTime != null) _invFungalTime.text = FormatDeteriorationTime(Items.FoodFungus);
            if (_invMeatTime != null) _invMeatTime.text = FormatDeteriorationTime(Items.FoodMeat);

            SetInvRowInteractive(_invVegetal, isOn && invVeg > 0);
            SetInvRowInteractive(_invFungal, isOn && invFung > 0);
            SetInvRowInteractive(_invMeat, isOn && invMeat > 0);

            if (_logLine1 != null)
                _logLine1.text = isOn ? "› Refrigeration system online" : "› Refrigeration system offline";
            if (_logLine2 != null)
                _logLine2.text = $"› {_lastLogMessage}";
        }

        private static void SetChamberQtyUi(Label qtyLabel, VisualElement card, VisualElement dot, int stored)
        {
            if (qtyLabel != null)
                qtyLabel.text = stored > 0 ? $"STORED × {stored}" : "EMPTY CHAMBER";
            if (dot != null)
            {
                dot.RemoveFromClassList("filled");
                if (stored > 0) dot.AddToClassList("filled");
            }
            if (card != null)
            {
                card.RemoveFromClassList("selected");
                if (stored > 0) card.AddToClassList("selected");
            }
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

        private void PushLog(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                _lastLogMessage = message;
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
                case FoodProductionType.Vegetable: return "Vegetal Synthesis";
                case FoodProductionType.Fungus: return "Fungal Synthesis";
                case FoodProductionType.Meat: return "Meat Synthesis";
                default: return "Food Item";
            }
        }
    }
}
