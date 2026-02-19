using UnityEngine;
using UnityEngine.UIElements;
using _Project;
using _Project.Sporae.Core;
using _Project.Systems.FoodRoom;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.PlayerInventory;

namespace Sporae.UI.UIToolkit.FoodRoom
{
    [RequireComponent(typeof(UIDocument))]
    public class FoodRoomPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private PlayerInventoryPanelController _playerInventoryPanel;

        private VisualElement _root;
        private VisualElement _overlay;
        private Button _btnClose;

        private VisualElement _stemCellSlot;
        private Label _stemCellLabel;
        private string _selectedStemCellTypeId;

        private VisualElement _chamberVegetal;
        private VisualElement _chamberFungal;
        private VisualElement _chamberMeat;
        private FoodProductionType _selectedChamberType = FoodProductionType.None;

        private VisualElement _tankDisplay;
        private Label _tankStatus;
        private Label _tankMessage;
        private VisualElement _tankCircle;
        private Label _indElectricity;
        private Label _indCoreTemp;
        private Label _indReservoir;
        private Label _indNutrient;

        private Label _prodTimer;
        private Label _prodEnergy;
        private Label _prodQuality;
        private Button _btnAdvanceDay;
        private Button _btnStartGrowth;
        private Button _btnPurify;
        private Button _btnHarvest;
        private Button _btnAbort;
        private Label _bottomHint;

        private GameManager _gameManager;
        private FoodRoomSystem _foodRoom;
        private FoodRoomConfig _config;

        private void Awake()
        {
            if (_uiDocument == null) _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null) _uiDocument.sortingOrder = 420;
        }

        private void Start()
        {
            _gameManager = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            _foodRoom = _gameManager?.FoodRoomSystem;
            _config = Resources.Load<FoodRoomConfig>("Configs/FoodRoomConfig");
            _root = _uiDocument?.rootVisualElement;
            if (_root != null)
                BindAndSubscribe();
            Hide();
        }

        private void BindAndSubscribe()
        {
            /* Nascondi tutto il root (food-room-root), non solo l'overlay: altrimenti il root resta visibile e copre la game view con il box grigio */
            _overlay = _root.Q<VisualElement>("food-room-root");
            _btnClose = _root.Q<Button>("btn-close");
            if (_btnClose != null) _btnClose.clicked += Hide;

            _stemCellSlot = _root.Q<VisualElement>("stem-cell-slot");
            _stemCellLabel = _root.Q<Label>("stem-cell-label");
            if (_stemCellSlot != null)
                _stemCellSlot.RegisterCallback<ClickEvent>(_ => OnStemCellSlotClick());

            _chamberVegetal = _root.Q<VisualElement>("chamber-vegetal");
            _chamberFungal = _root.Q<VisualElement>("chamber-fungal");
            _chamberMeat = _root.Q<VisualElement>("chamber-meat");
            if (_chamberVegetal != null) _chamberVegetal.RegisterCallback<ClickEvent>(_ => SelectChamber(FoodProductionType.Vegetable));
            if (_chamberFungal != null) _chamberFungal.RegisterCallback<ClickEvent>(_ => SelectChamber(FoodProductionType.Fungus));
            if (_chamberMeat != null) _chamberMeat.RegisterCallback<ClickEvent>(_ => SelectChamber(FoodProductionType.Meat));

            _tankDisplay = _root.Q<VisualElement>("tank-display");
            _tankStatus = _root.Q<Label>("tank-status");
            _tankMessage = _root.Q<Label>("tank-message");
            _tankCircle = _root.Q<VisualElement>("tank-circle");
            _indElectricity = _root.Q<Label>("ind-electricity-val");
            _indCoreTemp = _root.Q<Label>("ind-coretemp-val");
            _indReservoir = _root.Q<Label>("ind-reservoir-val");
            _indNutrient = _root.Q<Label>("ind-nutrient-val");

            _prodTimer = _root.Q<Label>("prod-timer");
            _prodEnergy = _root.Q<Label>("prod-energy");
            _prodQuality = _root.Q<Label>("prod-quality");
            _btnAdvanceDay = _root.Q<Button>("btn-advance-day");
            _btnStartGrowth = _root.Q<Button>("btn-start-growth");
            _btnPurify = _root.Q<Button>("btn-purify");
            _btnHarvest = _root.Q<Button>("btn-harvest");
            _btnAbort = _root.Q<Button>("btn-abort");
            _bottomHint = _root.Q<Label>("bottom-hint");

            if (_btnAdvanceDay != null) _btnAdvanceDay.clicked += OnAdvanceDayDebug;
            if (_btnStartGrowth != null) _btnStartGrowth.clicked += OnStartGrowth;
            if (_btnPurify != null) _btnPurify.clicked += OnPurify;
            if (_btnHarvest != null) _btnHarvest.clicked += OnHarvest;
            if (_btnAbort != null) _btnAbort.clicked += OnAbort;
        }

        private void OnStemCellSlotClick()
        {
            var allowed = new[] { Items.StemCellVegetable, Items.StemCellFungus, Items.StemCellAnimal };
            if (_playerInventoryPanel != null)
            {
                _playerInventoryPanel.ShowAsPicker(allowed, "Seleziona cellula staminale", OnStemCellSelected, () => { });
                return;
            }
            SporiumLogger.LogWarning(LogCategory.Core, "FoodRoomPanel: PlayerInventoryPanel non assegnato per picker stem cell.");
        }

        private void OnStemCellSelected(string typeId)
        {
            _selectedStemCellTypeId = typeId;
            if (_stemCellLabel != null)
                _stemCellLabel.text = "STEM CELL SELECTED";
            Refresh();
        }

        private void SelectChamber(FoodProductionType type)
        {
            _selectedChamberType = type;
            UpdateChamberSelectionVisual();
            Refresh();
        }

        private void UpdateChamberSelectionVisual()
        {
            if (_chamberVegetal != null) _chamberVegetal.RemoveFromClassList("selected");
            if (_chamberFungal != null) _chamberFungal.RemoveFromClassList("selected");
            if (_chamberMeat != null) _chamberMeat.RemoveFromClassList("selected");
            switch (_selectedChamberType)
            {
                case FoodProductionType.Vegetable: _chamberVegetal?.AddToClassList("selected"); break;
                case FoodProductionType.Fungus: _chamberFungal?.AddToClassList("selected"); break;
                case FoodProductionType.Meat: _chamberMeat?.AddToClassList("selected"); break;
            }
        }

        private void OnStartGrowth()
        {
            if (_foodRoom == null) return;
            if (_selectedChamberType == FoodProductionType.None) return;
            bool ok = _foodRoom.StartProduction(_selectedChamberType, _selectedStemCellTypeId);
            if (ok)
            {
                _selectedStemCellTypeId = null;
                if (_stemCellLabel != null) _stemCellLabel.text = "INSERT STEM CELL";
            }
            Refresh();
        }

        private void OnPurify()
        {
            if (_foodRoom == null) return;
            const int amount = 1;
            if (_gameManager?.PlayerInventory != null && _gameManager.PlayerInventory.Has(Items.Water, amount))
            {
                _foodRoom.StartWaterProduction(amount);
                Refresh();
            }
        }

        private void OnHarvest()
        {
            if (_foodRoom == null) return;
            for (int i = 0; i < _foodRoom.ProductionSlots.Count; i++)
            {
                if (_foodRoom.ProductionSlots[i].State == SlotState.Ready)
                {
                    _foodRoom.Harvest(i);
                    Refresh();
                    return;
                }
            }
            if (_foodRoom.WaterSlot.IsActive && _foodRoom.WaterSlot.PotableWaterOutput > 0)
            {
                _foodRoom.HarvestWater();
                Refresh();
            }
        }

        private void OnAbort()
        {
            Hide();
        }

        private void OnAdvanceDayDebug()
        {
            var dayCycle = ServiceContainer.Instance?.Get<DayCycleSystem>(suppressWarning: true);
            if (dayCycle != null && _gameManager != null && _gameManager.EconomySystem != null && _gameManager.EconomySystem.CanAfford(dayCycle.DailyPowerCost))
                dayCycle.EndDay();
            Refresh();
        }

        public void Show()
        {
            if (_root == null) _root = _uiDocument?.rootVisualElement;
            if (_root != null && _overlay == null) BindAndSubscribe();
            if (_uiDocument != null) _uiDocument.sortingOrder = 1000;
            if (_overlay != null) _overlay.style.display = DisplayStyle.Flex;
            _selectedChamberType = FoodProductionType.None;
            _selectedStemCellTypeId = null;
            if (_stemCellLabel != null) _stemCellLabel.text = "INSERT STEM CELL";
            UpdateChamberSelectionVisual();
            Refresh();
        }

        public void Hide()
        {
            if (_overlay != null) _overlay.style.display = DisplayStyle.None;
            if (_uiDocument != null) _uiDocument.sortingOrder = 420;
        }

        public bool IsVisible => _overlay != null && _overlay.style.display == DisplayStyle.Flex;

        private void Refresh()
        {
            if (_foodRoom == null) return;

            bool anyGrowing = false;
            FoodProductionSlot activeSlot = null;
            int activeIndex = -1;
            for (int i = 0; i < _foodRoom.ProductionSlots.Count; i++)
            {
                var slot = _foodRoom.ProductionSlots[i];
                if (slot.State == SlotState.Growing) { anyGrowing = true; activeSlot = slot; activeIndex = i; }
            }

            if (activeSlot != null)
            {
                int totalDays = _config != null ? _config.GetDaysFor(activeSlot.Type) : 1;
                if (_tankStatus != null) _tankStatus.text = "CULTIVATION IN PROGRESS";
                if (_tankMessage != null) _tankMessage.text = "Cellular proliferation in progress...";
                if (_prodTimer != null) _prodTimer.text = $"GROWTH TIMER: {totalDays - activeSlot.DaysRemaining} / {totalDays} days";
                int cryPerDay = _config != null ? _config.GetCryPerDayFor(activeSlot.Type) : 1;
                if (_prodEnergy != null) _prodEnergy.text = $"ENERGY COST: +{cryPerDay} CRY/day";
                if (_prodQuality != null) _prodQuality.text = "BIOMASS QUALITY: Common";
            }
            else
            {
                if (_tankStatus != null) _tankStatus.text = "GROWTH TANKS IDLE";
                if (_tankMessage != null) _tankMessage.text = "Select a synthesis protocol to begin growth cycle.";
                if (_prodTimer != null) _prodTimer.text = "GROWTH TIMER: IDLE";
                if (_prodEnergy != null) _prodEnergy.text = "ENERGY COST: 0 CRY";
                if (_prodQuality != null) _prodQuality.text = "BIOMASS QUALITY: N/A";
            }

            if (_indElectricity != null) _indElectricity.text = "87.1%";
            if (_indCoreTemp != null) _indCoreTemp.text = "42.8°C";
            if (_indReservoir != null) _indReservoir.text = "64%";
            if (_indNutrient != null) _indNutrient.text = "2.4 L/min";

            bool hasFreeSlot = false;
            foreach (var s in _foodRoom.ProductionSlots)
                if (s.State == SlotState.Free) { hasFreeSlot = true; break; }
            bool canStart = hasFreeSlot && _selectedChamberType != FoodProductionType.None && !anyGrowing;
            if (_btnStartGrowth != null) _btnStartGrowth.SetEnabled(canStart);
            if (_bottomHint != null)
                _bottomHint.text = anyGrowing ? "Cultivation in progress" : "Select a growth chamber to begin cultivation";
        }

    }
}
