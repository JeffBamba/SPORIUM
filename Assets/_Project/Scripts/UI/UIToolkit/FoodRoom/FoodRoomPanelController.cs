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
        private VisualElement _panel;
        private Button _btnClose;

        private VisualElement _stemCellSlot;
        private Label _stemCellLabel;
        private string _selectedStemCellTypeId;

        private VisualElement _chamberVegetal;
        private VisualElement _chamberFungal;
        private VisualElement _chamberMeat;
        private FoodProductionType _selectedChamberType = FoodProductionType.None;

        private VisualElement _residualProteinPanel;

        private Label _prodTimer;
        private Label _prodEnergy;
        private Label _prodQuality;
        private Button _btnAdvanceDay;
        private VisualElement _foodSynthPowerIndicator;
        private Label _foodSynthStatusText;
        private Label _foodSynthCost;
        private Button _btnFoodSynthPower;
        private Button _btnStartGrowth;
        private Button _btnHarvest;
        private Label _bottomHint;
        private VisualElement _cultivationProgressBlock;
        private VisualElement _cultivationProgressFill;
        private VisualElement _cultivationProgressTrack;
        private VisualElement _cultivationProgressShine;
        private float _progressShinePhase;

        private float _toastRefreshAccumulator;

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
            _panel = _root.Q<VisualElement>("food-room-panel");
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

            _prodTimer = _root.Q<Label>("prod-timer");
            _prodEnergy = _root.Q<Label>("prod-energy");
            _prodQuality = _root.Q<Label>("prod-quality");
            _btnAdvanceDay = _root.Q<Button>("btn-advance-day");
            _foodSynthPowerIndicator = _root.Q<VisualElement>("food-synth-power-indicator");
            _foodSynthStatusText = _root.Q<Label>("food-synth-status-text");
            _foodSynthCost = _root.Q<Label>("food-synth-cost");
            _btnFoodSynthPower = _root.Q<Button>("btn-food-synth-power");
            _btnStartGrowth = _root.Q<Button>("btn-start-growth");
            _btnHarvest = _root.Q<Button>("btn-harvest");
            _bottomHint = _root.Q<Label>("bottom-hint");
            _cultivationProgressBlock = _root.Q<VisualElement>("cultivation-progress-block");
            _cultivationProgressFill = _root.Q<VisualElement>("cultivation-progress-fill");
            _cultivationProgressTrack = _root.Q<VisualElement>("cultivation-progress-track");
            _cultivationProgressShine = _root.Q<VisualElement>("cultivation-progress-shine");
            if (_btnAdvanceDay != null) _btnAdvanceDay.clicked += OnAdvanceDayDebug;
            if (_btnFoodSynthPower != null) _btnFoodSynthPower.clicked += OnToggleFoodSynthPower;
            if (_btnStartGrowth != null) _btnStartGrowth.clicked += OnStartGrowth;
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

            var residualPanel = _root.Q<VisualElement>("residual-protein-panel");
            var residualTitle = residualPanel?.Q<Label>(className: "residual-protein-title");
            if (residualTitle != null) residualTitle.text = LocalizationManager.GetString("food_room.chrome.residual_title");
            var residualHint = _root.Q<Label>("residual-protein-hint");
            if (residualHint != null) residualHint.text = LocalizationManager.GetString("food_room.chrome.residual_hint");

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

        private void OnToggleFoodSynthPower()
        {
            if (_foodRoom == null) return;
            _foodRoom.SetFoodSynthPower(!_foodRoom.FoodSynthIsOn);
            Refresh();
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
                if (_prodTimer != null) _prodTimer.text = LocalizationManager.GetString("food_room.timer_inactive");
                if (_prodEnergy != null) _prodEnergy.text = LocalizationManager.GetString("food_room.energy_zero");
                if (_prodQuality != null) _prodQuality.text = LocalizationManager.GetString("food_room.quality_na");
            }

            bool synthOn = _foodRoom.FoodSynthIsOn;
            if (_foodSynthPowerIndicator != null)
            {
                _foodSynthPowerIndicator.EnableInClassList("food-synth-power-indicator--on", synthOn);
                _foodSynthPowerIndicator.EnableInClassList("food-synth-power-indicator--off", !synthOn);
            }
            if (_foodSynthStatusText != null)
            {
                _foodSynthStatusText.text = synthOn
                    ? LocalizationManager.GetString("food_room.synth.status_on")
                    : LocalizationManager.GetString("food_room.synth.status_off");
                _foodSynthStatusText.EnableInClassList("food-synth-status-text--on", synthOn);
                _foodSynthStatusText.EnableInClassList("food-synth-status-text--off", !synthOn);
            }
            if (_foodSynthCost != null)
            {
                _foodSynthCost.text = synthOn
                    ? LocalizationManager.GetString("food_room.synth.cost_on", new Dictionary<string, string> { ["cry"] = _foodRoom.FoodSynthDailyCost.ToString() })
                    : LocalizationManager.GetString("food_room.synth.cost_off");
            }
            if (_btnFoodSynthPower != null)
                _btnFoodSynthPower.text = synthOn
                    ? LocalizationManager.GetString("food_room.synth.btn_off")
                    : LocalizationManager.GetString("food_room.synth.btn_on");

            ApplyOfflineVisualState(synthOn);

            bool hasFreeSlot = false;
            foreach (var s in _foodRoom.ProductionSlots)
                if (s.State == SlotState.Free) { hasFreeSlot = true; break; }
            bool canStart = synthOn && hasFreeSlot && _selectedChamberType != FoodProductionType.None && !anyGrowing;
            if (_btnStartGrowth != null)
            {
                _btnStartGrowth.SetEnabled(canStart);
                _btnStartGrowth.tooltip = !synthOn
                    ? LocalizationManager.GetString("food_room.tooltip_synth_off")
                    : (!canStart && anyGrowing) ? LocalizationManager.GetString("food_room.tooltip_process_busy") : null;
            }

            bool canHarvest = false;
            foreach (var s in _foodRoom.ProductionSlots)
                if (s.State == SlotState.Ready) { canHarvest = true; break; }
            canHarvest = synthOn && canHarvest;
            if (_btnHarvest != null) _btnHarvest.SetEnabled(canHarvest);

            /* Bottom: hint normale o barra "Cultivation in Progress" (mostrata quando c'è un processo in corso O ready per harvest) */
            bool showProgressBlock = anyGrowing || anyReady;
            if (_bottomHint != null)
            {
                _bottomHint.style.display = showProgressBlock ? DisplayStyle.None : DisplayStyle.Flex;
                _bottomHint.text = synthOn
                    ? LocalizationManager.GetString("food_room.hint_chamber")
                    : LocalizationManager.GetString("food_room.hint_synth_off");
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

        }

        private void ApplyOfflineVisualState(bool synthOn)
        {
            _panel?.EnableInClassList("food-room-panel--offline", !synthOn);

            _stemCellSlot?.SetEnabled(synthOn);
            _chamberVegetal?.SetEnabled(synthOn);
            _chamberFungal?.SetEnabled(synthOn);
            _chamberMeat?.SetEnabled(synthOn);
            _btnStartGrowth?.SetEnabled(synthOn && _btnStartGrowth.enabledSelf);
            _btnHarvest?.SetEnabled(synthOn && _btnHarvest.enabledSelf);
            _btnAdvanceDay?.SetEnabled(synthOn);

            // Restano sempre operativi anche in OFF.
            _btnFoodSynthPower?.SetEnabled(true);
            _btnClose?.SetEnabled(true);
        }

        private void Update()
        {
            if (_foodRoom == null) return;

            bool anyGrowing = false;
            foreach (var s in _foodRoom.ProductionSlots)
                if (s.State == SlotState.Growing) { anyGrowing = true; break; }

            /* Keep progress toast visible and updated even when panel is closed */
            if (anyGrowing)
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

            if (!IsVisible) return;

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
        }

        private void OnDestroy()
        {
            GameLanguageSettings.OnLanguageChanged -= OnLanguageChanged;
            if (_gameManager?.PlayerInventory != null)
                _gameManager.PlayerInventory.OnInventoryChanged -= OnPlayerInventoryChanged;
        }
    }
}
