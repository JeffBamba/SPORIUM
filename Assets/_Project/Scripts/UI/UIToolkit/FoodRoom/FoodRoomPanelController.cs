using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using _Project;
using _Project.Sporae.Core;
using _Project.Systems.FoodRoom;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.PlayerInventory;
using Sporae.Core.Localization;

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

        private VisualElement _residualProteinPanel;

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
        private Label _bottomHint;
        private VisualElement _cultivationProgressBlock;
        private VisualElement _cultivationProgressFill;
        private VisualElement _cultivationProgressTrack;
        private VisualElement _cultivationProgressShine;
        private float _progressShinePhase;
        private VisualElement _waterProgressBlock;
        private VisualElement _waterProgressFill;
        private VisualElement _waterProgressTrack;
        private VisualElement _waterProgressShine;
        private float _waterShinePhase;
        private Button _btnPurifyMinus;
        private Button _btnPurifyPlus;
        private Label _hydrationUnitsValue;
        private int _purifyAmount = 0;
        private const int PurifyAmountMax = 99;

        private VisualElement _tankCircleSpinner;
        private float _toastRefreshAccumulator;
        private float _tankRotationAngle;

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
            if (_gameManager?.PlayerInventory != null)
                _gameManager.PlayerInventory.OnInventoryChanged += OnPlayerInventoryChanged;
            GameLanguageSettings.OnLanguageChanged += OnLanguageChanged;
            Hide();
        }

        private void OnLanguageChanged(GameLanguage _)
        {
            ApplyLocalizedFoodRoomStaticChrome();
            Refresh();
        }

        private void OnPlayerInventoryChanged()
        {
            Refresh();
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

            _residualProteinPanel = _root.Q<VisualElement>("residual-protein-panel");

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
            _btnPurify = _root.Q<Button>("btn-purify-bottom");
            _btnHarvest = _root.Q<Button>("btn-harvest");
            _btnPurifyMinus = _root.Q<Button>("btn-purify-minus");
            _btnPurifyPlus = _root.Q<Button>("btn-purify-plus");
            _hydrationUnitsValue = _root.Q<Label>("hydration-units-value");
            _bottomHint = _root.Q<Label>("bottom-hint");
            _tankCircleSpinner = _root.Q<VisualElement>("tank-circle-spinner");
            _cultivationProgressBlock = _root.Q<VisualElement>("cultivation-progress-block");
            _cultivationProgressFill = _root.Q<VisualElement>("cultivation-progress-fill");
            _cultivationProgressTrack = _root.Q<VisualElement>("cultivation-progress-track");
            _cultivationProgressShine = _root.Q<VisualElement>("cultivation-progress-shine");
            _waterProgressBlock = _root.Q<VisualElement>("water-progress-block");
            _waterProgressFill = _root.Q<VisualElement>("water-progress-fill");
            _waterProgressTrack = _root.Q<VisualElement>("water-progress-track");
            _waterProgressShine = _root.Q<VisualElement>("water-progress-shine");
            if (_btnAdvanceDay != null) _btnAdvanceDay.clicked += OnAdvanceDayDebug;
            if (_btnStartGrowth != null) _btnStartGrowth.clicked += OnStartGrowth;
            if (_btnPurify != null) _btnPurify.clicked += OnPurify;
            if (_btnPurifyMinus != null) _btnPurifyMinus.clicked += OnPurifyAmountDecrease;
            if (_btnPurifyPlus != null) _btnPurifyPlus.clicked += OnPurifyAmountIncrease;
            if (_btnHarvest != null) _btnHarvest.clicked += OnHarvest;

            ApplyLocalizedFoodRoomStaticChrome();
        }

        private void ApplyLocalizedFoodRoomStaticChrome()
        {
            if (_root == null)
                return;

            var status = _root.Q<Label>("status");
            if (status != null) status.text = LocalizationManager.GetString("food_room.chrome.header_status");
            var title = _root.Q<Label>("title");
            if (title != null) title.text = LocalizationManager.GetString("food_room.chrome.title");
            var subtitle = _root.Q<Label>("subtitle");
            if (subtitle != null) subtitle.text = LocalizationManager.GetString("food_room.chrome.subtitle");

            var chambersTitleRow = _root.Q<VisualElement>("chambers-title-row");
            var chambersTitle = chambersTitleRow?.Q<Label>(className: "section-title");
            if (chambersTitle != null) chambersTitle.text = LocalizationManager.GetString("food_room.chrome.section_growth");

            void SetChamberCard(VisualElement card, string nameKey, string detailsKey, string descKey)
            {
                if (card == null) return;
                var nm = card.Q<Label>(className: "chamber-name");
                if (nm != null) nm.text = LocalizationManager.GetString(nameKey);
                var det = card.Q<Label>(className: "chamber-details");
                if (det != null) det.text = LocalizationManager.GetString(detailsKey);
                var desc = card.Q<Label>(className: "chamber-desc");
                if (desc != null) desc.text = LocalizationManager.GetString(descKey);
                var progLab = card.Q<Label>(className: "chamber-progress-label");
                if (progLab != null) progLab.text = LocalizationManager.GetString("food_room.chrome.progress_label");
            }

            SetChamberCard(_chamberVegetal, "food_room.chrome.veg_name", "food_room.chrome.veg_details", "food_room.chrome.veg_desc");
            SetChamberCard(_chamberFungal, "food_room.chrome.fung_name", "food_room.chrome.fung_details", "food_room.chrome.fung_desc");
            SetChamberCard(_chamberMeat, "food_room.chrome.meat_name", "food_room.chrome.meat_details", "food_room.chrome.meat_desc");

            var hydrationTitleRow = _root.Q<VisualElement>("hydration-title-row");
            var hydrationSectionTitle = hydrationTitleRow?.Q<Label>(className: "section-title");
            if (hydrationSectionTitle != null) hydrationSectionTitle.text = LocalizationManager.GetString("food_room.chrome.section_hydration");

            var hydrationLabel = _root.Q<Label>("hydration-label");
            if (hydrationLabel != null) hydrationLabel.text = LocalizationManager.GetString("food_room.chrome.hydration_label");
            var hydrationFlavor = _root.Q<Label>("hydration-flavor");
            if (hydrationFlavor != null) hydrationFlavor.text = LocalizationManager.GetString("food_room.chrome.hydration_flavor");

            var unitsRow = _root.Q<VisualElement>("hydration-units-row");
            var unitsCaption = unitsRow?.Q<Label>(className: "hydration-units-label");
            if (unitsCaption != null) unitsCaption.text = LocalizationManager.GetString("food_room.chrome.units_label");

            var residualPanel = _root.Q<VisualElement>("residual-protein-panel");
            var residualTitle = residualPanel?.Q<Label>(className: "residual-protein-title");
            if (residualTitle != null) residualTitle.text = LocalizationManager.GetString("food_room.chrome.residual_title");
            var residualHint = _root.Q<Label>("residual-protein-hint");
            if (residualHint != null) residualHint.text = LocalizationManager.GetString("food_room.chrome.residual_hint");

            var comment = _root.Q<Label>("comment-text");
            if (comment != null) comment.text = LocalizationManager.GetString("food_room.chrome.comment");

            var lifeRow = _root.Q<VisualElement>("life-support-title-row");
            var lifeTitle = lifeRow?.Q<Label>(className: "section-title");
            if (lifeTitle != null) lifeTitle.text = LocalizationManager.GetString("food_room.chrome.section_life_support");

            var indE = _root.Q<Label>("ind-electricity");
            if (indE != null) indE.text = LocalizationManager.GetString("food_room.chrome.ls_electric");
            var indC = _root.Q<Label>("ind-coretemp");
            if (indC != null) indC.text = LocalizationManager.GetString("food_room.chrome.ls_core_temp");
            var indR = _root.Q<Label>("ind-reservoir");
            if (indR != null) indR.text = LocalizationManager.GetString("food_room.chrome.ls_reservoir");
            var indN = _root.Q<Label>("ind-nutrient");
            if (indN != null) indN.text = LocalizationManager.GetString("food_room.chrome.ls_nutrient");

            var maintRow = _root.Q<VisualElement>("life-support-maintenance");
            var maintCaption = maintRow?.Q<Label>(className: "life-support-label");
            if (maintCaption != null) maintCaption.text = LocalizationManager.GetString("food_room.chrome.ls_maint_label");
            var maintVal = _root.Q<Label>("ind-maintenance");
            if (maintVal != null) maintVal.text = LocalizationManager.GetString("food_room.chrome.ls_maint_value");

            if (_btnStartGrowth != null) _btnStartGrowth.text = LocalizationManager.GetString("food_room.chrome.btn_start");
            if (_btnHarvest != null) _btnHarvest.text = LocalizationManager.GetString("food_room.chrome.btn_harvest");
        }

        private void OnStemCellSlotClick()
        {
            var allowed = new[] { Items.StemCellVegetable, Items.StemCellFungus, Items.StemCellAnimal };
            if (_playerInventoryPanel != null)
            {
                _playerInventoryPanel.ShowAsPicker(allowed, LocalizationManager.GetString("food_room.picker_stem_title"), OnStemCellSelected, () => { });
                return;
            }
            SporiumLogger.LogWarning(LogCategory.Core, "FoodRoomPanel: PlayerInventoryPanel non assegnato per picker stem cell.");
        }

        private void OnStemCellSelected(string typeId)
        {
            _selectedStemCellTypeId = typeId;
            if (_stemCellLabel != null)
                _stemCellLabel.text = LocalizationManager.GetString("food_room.stem_selected");
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
                if (_stemCellLabel != null) _stemCellLabel.text = LocalizationManager.GetString("food_room.stem_insert");
            }
            Refresh();
        }

        private void OnPurify()
        {
            if (_foodRoom == null) return;
            if (_foodRoom.WaterSlot.PotableWaterOutput > 0)
            {
                _foodRoom.HarvestWater();
                Refresh();
                return;
            }
            int amount = Mathf.Clamp(_purifyAmount, 1, GetMaxRawWater());
            if (amount < 1) return;
            if (_gameManager?.PlayerInventory != null && _gameManager.PlayerInventory.Has(Items.Water, amount))
            {
                _foodRoom.StartWaterProduction(amount);
                Refresh();
            }
        }

        private void OnPurifyAmountDecrease()
        {
            if (_purifyAmount > 0) { _purifyAmount--; UpdatePurifyAmountDisplay(); Refresh(); }
        }

        private void OnPurifyAmountIncrease()
        {
            if (_purifyAmount < PurifyAmountMax) { _purifyAmount++; UpdatePurifyAmountDisplay(); Refresh(); }
        }

        private int GetMaxRawWater()
        {
            if (_gameManager?.PlayerInventory == null) return 0;
            int count = 0;
            foreach (var slot in _gameManager.PlayerInventory.Items)
                if (slot.TypeId == Items.Water) { count = slot.Quantity; break; }
            return count;
        }

        private void UpdatePurifyAmountDisplay()
        {
            if (_hydrationUnitsValue != null) _hydrationUnitsValue.text = _purifyAmount.ToString();
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
            EnsureGameManagerAndFoodRoom();
            GameplayUiModalLock.SetMachineModalState(true);
            if (_uiDocument != null) _uiDocument.sortingOrder = 1000;
            if (_overlay != null) _overlay.style.display = DisplayStyle.Flex;
            _selectedChamberType = FoodProductionType.None;
            _selectedStemCellTypeId = null;
            if (_stemCellLabel != null) _stemCellLabel.text = LocalizationManager.GetString("food_room.stem_insert");
            /* Counter unità acqua a 0 all'apertura (così al ritorno dopo una purificazione completata si vede 0) */
            if (_foodRoom != null && !_foodRoom.WaterSlot.IsActive)
                _purifyAmount = 0;
            UpdateChamberSelectionVisual();
            Refresh();
        }

        public void Hide()
        {
            GameplayUiModalLock.SetMachineModalState(false);
            if (_overlay != null) _overlay.style.display = DisplayStyle.None;
            if (_uiDocument != null) _uiDocument.sortingOrder = 420;
        }

        public bool IsVisible => _overlay != null && _overlay.style.display == DisplayStyle.Flex;

        private void EnsureGameManagerAndFoodRoom()
        {
            if (_gameManager != null && _foodRoom != null) return;
            _gameManager = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            _foodRoom = _gameManager?.FoodRoomSystem;
            if (_gameManager?.PlayerInventory != null)
            {
                _gameManager.PlayerInventory.OnInventoryChanged -= OnPlayerInventoryChanged;
                _gameManager.PlayerInventory.OnInventoryChanged += OnPlayerInventoryChanged;
            }
        }

        private void Refresh()
        {
            EnsureGameManagerAndFoodRoom();
            if (_foodRoom == null) return;

            bool anyGrowing = false;
            bool anyReady = false;
            FoodProductionSlot activeSlot = null;
            FoodProductionSlot readySlot = null;
            for (int i = 0; i < _foodRoom.ProductionSlots.Count; i++)
            {
                var slot = _foodRoom.ProductionSlots[i];
                if (slot.State == SlotState.Growing) { anyGrowing = true; activeSlot = slot; }
                if (slot.State == SlotState.Ready) { anyReady = true; if (readySlot == null) readySlot = slot; }
            }

            /* Tank status / production info: prefer growing slot, else ready, else idle */
            FoodProductionSlot displaySlot = activeSlot ?? readySlot;
            if (displaySlot != null && _config != null)
            {
                int totalDays = _config.GetDaysFor(displaySlot.Type);
                int elapsed = displaySlot.State == SlotState.Ready ? totalDays : (totalDays - displaySlot.DaysRemaining);
                if (_tankStatus != null) _tankStatus.text = displaySlot.State == SlotState.Ready
                    ? LocalizationManager.GetString("food_room.tank.ready")
                    : LocalizationManager.GetString("food_room.tank.growing");
                if (_tankMessage != null) _tankMessage.text = displaySlot.State == SlotState.Ready
                    ? LocalizationManager.GetString("food_room.tank.msg_ready")
                    : LocalizationManager.GetString("food_room.tank.msg_growing");
                if (_prodTimer != null) _prodTimer.text = LocalizationManager.GetString("food_room.timer_growth", new Dictionary<string, string>
                    { ["elapsed"] = elapsed.ToString(), ["total"] = totalDays.ToString() });
                int cryPerDay = _config.GetCryPerDayFor(displaySlot.Type);
                if (_prodEnergy != null) _prodEnergy.text = displaySlot.State == SlotState.Growing
                    ? LocalizationManager.GetString("food_room.energy_growing", new Dictionary<string, string> { ["cry"] = cryPerDay.ToString() })
                    : LocalizationManager.GetString("food_room.energy_zero");
                if (_prodQuality != null) _prodQuality.text = LocalizationManager.GetString("food_room.quality_common");
            }
            else
            {
                if (_tankStatus != null) _tankStatus.text = LocalizationManager.GetString("food_room.tank.inactive");
                if (_tankMessage != null) _tankMessage.text = LocalizationManager.GetString("food_room.tank.msg_idle");
                if (_prodTimer != null) _prodTimer.text = LocalizationManager.GetString("food_room.timer_inactive");
                if (_prodEnergy != null) _prodEnergy.text = LocalizationManager.GetString("food_room.energy_zero");
                if (_prodQuality != null) _prodQuality.text = LocalizationManager.GetString("food_room.quality_na");
            }

            if (_indElectricity != null) _indElectricity.text = "87.1%";
            if (_indCoreTemp != null) _indCoreTemp.text = "42.8°C";
            if (_indReservoir != null) _indReservoir.text = "64%";
            if (_indNutrient != null) _indNutrient.text = "2.4 L/min";

            bool hasFreeSlot = false;
            foreach (var s in _foodRoom.ProductionSlots)
                if (s.State == SlotState.Free) { hasFreeSlot = true; break; }
            bool canStart = hasFreeSlot && _selectedChamberType != FoodProductionType.None && !anyGrowing;
            if (_btnStartGrowth != null)
            {
                _btnStartGrowth.SetEnabled(canStart);
                _btnStartGrowth.tooltip = (!canStart && anyGrowing) ? LocalizationManager.GetString("food_room.tooltip_process_busy") : null;
            }

            bool hasWater = _gameManager != null && _gameManager.PlayerInventory != null && _gameManager.PlayerInventory.Has(Items.Water, 1);
            bool waterProcessActive = _foodRoom.WaterSlot.IsActive && _foodRoom.WaterSlot.RawWaterInput > 0;
            bool waterReadyToCollect = _foodRoom.WaterSlot.PotableWaterOutput > 0;
            bool showCollectButton = waterProcessActive || waterReadyToCollect;
            bool canPurify = !showCollectButton && hasWater && _purifyAmount >= 1;
            if (_btnPurify != null)
            {
                if (showCollectButton)
                {
                    _btnPurify.text = LocalizationManager.GetString("food_room.btn_collect");
                    if (waterReadyToCollect)
                    {
                        _btnPurify.SetEnabled(true);
                        _btnPurify.AddToClassList("btn-purify--enabled");
                    }
                    else
                    {
                        _btnPurify.SetEnabled(false);
                        _btnPurify.RemoveFromClassList("btn-purify--enabled");
                    }
                }
                else
                {
                    _btnPurify.text = LocalizationManager.GetString("food_room.btn_purify");
                    _btnPurify.SetEnabled(canPurify);
                    if (canPurify) _btnPurify.AddToClassList("btn-purify--enabled");
                    else _btnPurify.RemoveFromClassList("btn-purify--enabled");
                }
            }
            _purifyAmount = Mathf.Clamp(_purifyAmount, 0, PurifyAmountMax);
            if (_purifyAmount > GetMaxRawWater()) _purifyAmount = GetMaxRawWater();
            UpdatePurifyAmountDisplay();
            bool allowPurifyControls = !showCollectButton;
            if (_btnPurifyMinus != null) _btnPurifyMinus.SetEnabled(allowPurifyControls && _purifyAmount > 0);
            if (_btnPurifyPlus != null) _btnPurifyPlus.SetEnabled(allowPurifyControls && _purifyAmount < PurifyAmountMax);

            bool canHarvest = false;
            foreach (var s in _foodRoom.ProductionSlots)
                if (s.State == SlotState.Ready) { canHarvest = true; break; }
            if (_btnHarvest != null) _btnHarvest.SetEnabled(canHarvest);

            /* Bottom: hint normale o barra "Cultivation in Progress" (mostrata quando c'è un processo in corso O ready per harvest) o barra acqua */
            bool showProgressBlock = anyGrowing || anyReady;
            bool waterInProgress = _foodRoom.WaterSlot.IsActive && _foodRoom.WaterSlot.RawWaterInput > 0;
            bool waterReadyToCollectForBar = _foodRoom.WaterSlot.PotableWaterOutput > 0;
            bool showWaterProgressBlock = waterInProgress || waterReadyToCollectForBar;
            if (_bottomHint != null)
            {
                _bottomHint.style.display = (showProgressBlock || showWaterProgressBlock) ? DisplayStyle.None : DisplayStyle.Flex;
                _bottomHint.text = LocalizationManager.GetString("food_room.hint_chamber");
            }

            if (_cultivationProgressBlock != null)
                _cultivationProgressBlock.style.display = showProgressBlock ? DisplayStyle.Flex : DisplayStyle.None;

            /* Progress bar: valore reale da slot (giorni avanzati = più piena). Quando Ready = 100% piena. */
            if (showProgressBlock && _cultivationProgressFill != null && _config != null)
            {
                FoodProductionSlot progressSlot = activeSlot ?? readySlot;
                if (progressSlot != null)
                {
                    int totalDays = _config.GetDaysFor(progressSlot.Type);
                    int remaining = progressSlot.State == SlotState.Ready ? 0 : progressSlot.DaysRemaining;
                    float progress = totalDays > 0 ? (totalDays - remaining) / (float)totalDays : 0f;
                    _cultivationProgressFill.style.width = new Length(Mathf.Clamp01(progress) * 100f, LengthUnit.Percent);
                }
                if (_cultivationProgressBlock != null)
                {
                    var label = _cultivationProgressBlock.Q<Label>("cultivation-progress-label");
                    if (label != null)
                        label.text = anyReady && !anyGrowing
                            ? LocalizationManager.GetString("food_room.bar_food_ready")
                            : LocalizationManager.GetString("food_room.bar_food_growing");
                }
            }

            /* Water progress bar: overall % (PotableWaterOutput + CurrentUnitProgress) / RawWaterInput. When ready to collect, show 100% and "Ready for collection" like Food bar. */
            if (_waterProgressBlock != null)
                _waterProgressBlock.style.display = showWaterProgressBlock ? DisplayStyle.Flex : DisplayStyle.None;
            if (showWaterProgressBlock && _waterProgressFill != null)
            {
                var ws = _foodRoom.WaterSlot;
                float totalProgress;
                if (waterReadyToCollectForBar && !waterInProgress)
                    totalProgress = 1f;
                else
                    totalProgress = ws.RawWaterInput > 0 ? (ws.PotableWaterOutput + ws.CurrentUnitProgress) / (float)ws.RawWaterInput : 0f;
                _waterProgressFill.style.width = new Length(Mathf.Clamp01(totalProgress) * 100f, LengthUnit.Percent);
                var wLabel = _waterProgressBlock?.Q<Label>("water-progress-label");
                if (wLabel != null)
                    wLabel.text = (waterReadyToCollectForBar && !waterInProgress)
                        ? LocalizationManager.GetString("food_room.bar_water_ready")
                        : LocalizationManager.GetString("food_room.bar_water_progress");
            }

            /* KitchenHome: active chamber card (usa displaySlot per evidenziare quale camera è attiva/ready) */
            if (_chamberVegetal != null) _chamberVegetal.RemoveFromClassList("active");
            if (_chamberFungal != null) _chamberFungal.RemoveFromClassList("active");
            if (_chamberMeat != null) _chamberMeat.RemoveFromClassList("active");
            if (displaySlot != null)
            {
                switch (displaySlot.Type)
                {
                    case FoodProductionType.Vegetable: _chamberVegetal?.AddToClassList("active"); break;
                    case FoodProductionType.Fungus: _chamberFungal?.AddToClassList("active"); break;
                    case FoodProductionType.Meat: _chamberMeat?.AddToClassList("active"); break;
                }
            }

            /* KitchenHome: residual protein panel — nascosto in UI (info RES-PROT data dal toast harvest) */
            if (_residualProteinPanel != null)
                _residualProteinPanel.style.display = DisplayStyle.None;

            /* Stem cell slot filled state */
            if (_stemCellSlot != null)
            {
                if (!string.IsNullOrEmpty(_selectedStemCellTypeId))
                    _stemCellSlot.AddToClassList("stem-cell-slot--filled");
                else
                    _stemCellSlot.RemoveFromClassList("stem-cell-slot--filled");
            }

            /* Tank circle animation state */
            if (_tankCircle != null)
            {
                if (anyGrowing)
                    _tankCircle.AddToClassList("tank-circle--active");
                else
                    _tankCircle.RemoveFromClassList("tank-circle--active");
            }
            if (_tankCircleSpinner != null)
                _tankCircleSpinner.style.display = anyGrowing ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Update()
        {
            if (_foodRoom == null) return;

            _foodRoom.TickWaterProduction(Time.deltaTime);

            bool anyGrowing = false;
            foreach (var s in _foodRoom.ProductionSlots)
                if (s.State == SlotState.Growing) { anyGrowing = true; break; }
            bool waterActive = _foodRoom.WaterSlot.IsActive;

            /* Keep progress toast visible and updated even when panel is closed */
            if (anyGrowing || waterActive)
            {
                _toastRefreshAccumulator += Time.deltaTime;
                if (_toastRefreshAccumulator >= 1.5f)
                {
                    _toastRefreshAccumulator = 0f;
                    _foodRoom.RefreshToasts();
                }
            }
            else
            {
                _toastRefreshAccumulator = 0f;
            }

            /* Tank spinner only when panel is visible */
            if (!IsVisible) return;
            if (anyGrowing && _tankCircleSpinner != null)
            {
                _tankRotationAngle += 180f * Time.deltaTime;
                if (_tankRotationAngle >= 360f) _tankRotationAngle -= 360f;
                _tankCircleSpinner.style.rotate = new Rotate(_tankRotationAngle);
            }

            /* Progress bar shine: move left-to-right when cultivation in progress */
            bool showProgressBar = _cultivationProgressBlock != null && _cultivationProgressBlock.resolvedStyle.display == DisplayStyle.Flex;
            if (showProgressBar && anyGrowing && _cultivationProgressTrack != null && _cultivationProgressShine != null)
            {
                _progressShinePhase += Time.deltaTime * 0.6f;
                if (_progressShinePhase > 1f) _progressShinePhase -= 1f;
                float leftPct = _progressShinePhase * 70f;
                _cultivationProgressShine.style.left = new Length(leftPct, LengthUnit.Percent);
                _cultivationProgressShine.style.display = DisplayStyle.Flex;
            }
            else if (_cultivationProgressShine != null)
            {
                _cultivationProgressShine.style.display = DisplayStyle.None;
            }

            /* Water progress bar shine: same left-to-right animation when water purification in progress */
            bool showWaterBar = _waterProgressBlock != null && _waterProgressBlock.resolvedStyle.display == DisplayStyle.Flex;
            if (showWaterBar && waterActive && _waterProgressTrack != null && _waterProgressShine != null)
            {
                _waterShinePhase += Time.deltaTime * 0.6f;
                if (_waterShinePhase > 1f) _waterShinePhase -= 1f;
                float leftPct = _waterShinePhase * 70f;
                _waterProgressShine.style.left = new Length(leftPct, LengthUnit.Percent);
                _waterProgressShine.style.display = DisplayStyle.Flex;
            }
            else if (_waterProgressShine != null)
            {
                _waterProgressShine.style.display = DisplayStyle.None;
            }
            if (showWaterBar && waterActive && _waterProgressFill != null)
            {
                var ws = _foodRoom.WaterSlot;
                float totalProgress = ws.RawWaterInput > 0 ? (ws.PotableWaterOutput + ws.CurrentUnitProgress) / (float)ws.RawWaterInput : 0f;
                _waterProgressFill.style.width = new Length(Mathf.Clamp01(totalProgress) * 100f, LengthUnit.Percent);
            }
        }

        private void OnDestroy()
        {
            GameLanguageSettings.OnLanguageChanged -= OnLanguageChanged;
            if (_gameManager?.PlayerInventory != null)
                _gameManager.PlayerInventory.OnInventoryChanged -= OnPlayerInventoryChanged;
        }
    }
}
