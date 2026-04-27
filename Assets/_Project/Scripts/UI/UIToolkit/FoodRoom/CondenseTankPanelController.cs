using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using _Project;
using _Project.Sporae.Core;
using _Project.Systems.FoodRoom;
using Sporae.Core.Localization;
using Sporae.UI.UIToolkit.NotificationsFoundation;

namespace Sporae.UI.UIToolkit.FoodRoom
{
    [RequireComponent(typeof(UIDocument))]
    public class CondenseTankPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _root;
        private VisualElement _overlay;
        private Button _btnClose;
        private Button _btnPurify;
        private Button _btnPurifyMinus;
        private Button _btnPurifyPlus;
        private Label _hydrationUnitsValue;
        private Label _hydrationStatusText;
        private Label _bottomHint;
        private VisualElement _waterProgressBlock;
        private VisualElement _waterProgressFill;
        private VisualElement _waterProgressTrack;
        private VisualElement _waterProgressShine;
        private Label _condensationValue;
        private Label _condensationStatusText;
        private VisualElement _condensationProgressFill;
        private Button _btnCollectDirtyWater;

        private int _purifyAmount;
        private const int PurifyAmountMax = 99;
        private float _waterShinePhase;
        private float _toastRefreshAccumulator;

        private GameManager _gameManager;
        private FoodRoomSystem _foodRoom;
        private bool _uiBound;
        private bool _condensationSubscribed;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null)
                _uiDocument.sortingOrder = 420;
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
            GameLanguageSettings.OnLanguageChanged -= OnLanguageChanged;
            if (_gameManager?.PlayerInventory != null)
                _gameManager.PlayerInventory.OnInventoryChanged -= OnInventoryChanged;
            if (_gameManager != null && _condensationSubscribed)
                _gameManager.OnCondensationChanged -= OnCondensationChanged;
        }

        private void OnLanguageChanged(GameLanguage _)
        {
            ApplyLocalizedCondenseStaticChrome();
            Refresh();
        }

        private void OnInventoryChanged()
        {
            Refresh();
        }

        private void EnsureSystems()
        {
            if (_gameManager == null)
                _gameManager = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            if (_foodRoom == null)
                _foodRoom = _gameManager?.FoodRoomSystem;
            if (_gameManager != null && !_condensationSubscribed)
            {
                _gameManager.OnCondensationChanged += OnCondensationChanged;
                _condensationSubscribed = true;
            }
            if (_gameManager?.PlayerInventory != null)
            {
                _gameManager.PlayerInventory.OnInventoryChanged -= OnInventoryChanged;
                _gameManager.PlayerInventory.OnInventoryChanged += OnInventoryChanged;
            }
        }

        private void OnCondensationChanged(float _)
        {
            RefreshCondensationCollectUi();
        }

        private void BindAndSubscribe()
        {
            if (_uiBound || _root == null) return;

            _overlay = _root.Q<VisualElement>("condense-tank-root");
            _btnClose = _root.Q<Button>("btn-close");
            _btnPurify = _root.Q<Button>("btn-purify-bottom");
            _btnPurifyMinus = _root.Q<Button>("btn-purify-minus");
            _btnPurifyPlus = _root.Q<Button>("btn-purify-plus");
            _hydrationUnitsValue = _root.Q<Label>("hydration-units-value");
            _hydrationStatusText = _root.Q<Label>("hydration-status-text");
            _bottomHint = _root.Q<Label>("condense-bottom-hint");
            _waterProgressBlock = _root.Q<VisualElement>("water-progress-block");
            _waterProgressFill = _root.Q<VisualElement>("water-progress-fill");
            _waterProgressTrack = _root.Q<VisualElement>("water-progress-track");
            _waterProgressShine = _root.Q<VisualElement>("water-progress-shine");
            _condensationValue = _root.Q<Label>("condensation-value");
            _condensationStatusText = _root.Q<Label>("condensation-status-text");
            _condensationProgressFill = _root.Q<VisualElement>("condensation-progress-fill");
            _btnCollectDirtyWater = _root.Q<Button>("btn-collect-dirty-water");

            if (_btnClose != null) _btnClose.clicked += Hide;
            if (_btnPurify != null) _btnPurify.clicked += OnPurify;
            if (_btnPurifyMinus != null) _btnPurifyMinus.clicked += OnPurifyAmountDecrease;
            if (_btnPurifyPlus != null) _btnPurifyPlus.clicked += OnPurifyAmountIncrease;
            if (_btnCollectDirtyWater != null) _btnCollectDirtyWater.clicked += OnCollectDirtyWater;

            _uiBound = true;
            if (_waterProgressBlock != null)
                _waterProgressBlock.style.display = DisplayStyle.None;
            ApplyLocalizedCondenseStaticChrome();
        }

        private void ApplyLocalizedCondenseStaticChrome()
        {
            if (_root == null) return;

            var status = _root.Q<Label>("condense-status");
            if (status != null) status.text = LocalizationManager.GetString("condense_tank.chrome.status");
            var title = _root.Q<Label>("condense-title");
            if (title != null) title.text = LocalizationManager.GetString("condense_tank.chrome.title");
            var subtitle = _root.Q<Label>("condense-subtitle");
            if (subtitle != null) subtitle.text = LocalizationManager.GetString("condense_tank.chrome.subtitle");

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

            var condensationTitle = _root.Q<Label>("condensation-section-title");
            if (condensationTitle != null) condensationTitle.text = LocalizationManager.GetString("condense_tank.condensation.title");
            var condensationLabel = _root.Q<Label>("condensation-label");
            if (condensationLabel != null) condensationLabel.text = LocalizationManager.GetString("condense_tank.condensation.label");
            var condensationFlavor = _root.Q<Label>("condensation-flavor");
            if (condensationFlavor != null) condensationFlavor.text = LocalizationManager.GetString("condense_tank.condensation.flavor");
            if (_btnCollectDirtyWater != null)
                _btnCollectDirtyWater.text = LocalizationManager.GetString("condense_tank.condensation.collect");
        }

        public void Show()
        {
            if (_root == null) _root = _uiDocument?.rootVisualElement;
            if (_root != null && !_uiBound) BindAndSubscribe();
            EnsureSystems();
            GameplayUiModalLock.SetMachineModalState(true);
            if (_uiDocument != null) _uiDocument.sortingOrder = 1000;
            if (_overlay != null) _overlay.style.display = DisplayStyle.Flex;
            if (_foodRoom != null && !_foodRoom.WaterSlot.IsActive)
                _purifyAmount = 0;
            Refresh();
        }

        public void Hide()
        {
            GameplayUiModalLock.SetMachineModalState(false);
            if (_overlay != null) _overlay.style.display = DisplayStyle.None;
            if (_uiDocument != null) _uiDocument.sortingOrder = 420;
        }

        public bool IsVisible => _overlay != null && _overlay.style.display == DisplayStyle.Flex;

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
            if (_purifyAmount <= 0) return;
            _purifyAmount--;
            UpdatePurifyAmountDisplay();
            Refresh();
        }

        private void OnPurifyAmountIncrease()
        {
            if (_purifyAmount >= PurifyAmountMax) return;
            _purifyAmount++;
            UpdatePurifyAmountDisplay();
            Refresh();
        }

        private int GetMaxRawWater()
        {
            if (_gameManager?.PlayerInventory == null) return 0;
            int count = 0;
            foreach (var slot in _gameManager.PlayerInventory.Items)
            {
                if (slot.TypeId == Items.Water)
                {
                    count = slot.Quantity;
                    break;
                }
            }
            return count;
        }

        private void UpdatePurifyAmountDisplay()
        {
            if (_hydrationUnitsValue != null)
                _hydrationUnitsValue.text = _purifyAmount.ToString();
        }

        private void Refresh()
        {
            EnsureSystems();
            if (_foodRoom == null) return;

            bool hasWater = _gameManager != null && _gameManager.PlayerInventory != null && _gameManager.PlayerInventory.Has(Items.Water, 1);
            bool waterProcessActive = _foodRoom.WaterSlot.IsActive && _foodRoom.WaterSlot.RawWaterInput > 0;
            bool waterReadyToCollect = _foodRoom.WaterSlot.PotableWaterOutput > 0;
            bool showCollectButton = waterProcessActive || waterReadyToCollect;

            _purifyAmount = Mathf.Clamp(_purifyAmount, 0, PurifyAmountMax);
            int maxRawWater = GetMaxRawWater();
            if (_purifyAmount > maxRawWater)
                _purifyAmount = maxRawWater;
            UpdatePurifyAmountDisplay();

            bool canPurify = !showCollectButton && hasWater && _purifyAmount >= 1;
            if (_btnPurify != null)
            {
                if (showCollectButton)
                {
                    _btnPurify.text = LocalizationManager.GetString("food_room.btn_collect");
                    _btnPurify.SetEnabled(waterReadyToCollect);
                    _btnPurify.EnableInClassList("btn-purify--enabled", waterReadyToCollect);
                }
                else
                {
                    _btnPurify.text = LocalizationManager.GetString("food_room.btn_purify");
                    _btnPurify.SetEnabled(canPurify);
                    _btnPurify.EnableInClassList("btn-purify--enabled", canPurify);
                }
            }

            bool allowPurifyControls = !showCollectButton;
            if (_btnPurifyMinus != null) _btnPurifyMinus.SetEnabled(allowPurifyControls && _purifyAmount > 0);
            if (_btnPurifyPlus != null) _btnPurifyPlus.SetEnabled(allowPurifyControls && _purifyAmount < PurifyAmountMax && _purifyAmount < maxRawWater);

            if (_hydrationStatusText != null)
            {
                if (waterReadyToCollect)
                    _hydrationStatusText.text = LocalizationManager.GetString("condense_tank.status.ready", new Dictionary<string, string> { ["count"] = _foodRoom.WaterSlot.PotableWaterOutput.ToString() });
                else if (waterProcessActive)
                    _hydrationStatusText.text = LocalizationManager.GetString("condense_tank.status.progress", new Dictionary<string, string> { ["count"] = _foodRoom.WaterSlot.RawWaterInput.ToString() });
                else if (hasWater)
                    _hydrationStatusText.text = LocalizationManager.GetString("condense_tank.status.available", new Dictionary<string, string> { ["count"] = maxRawWater.ToString() });
                else
                    _hydrationStatusText.text = LocalizationManager.GetString("condense_tank.status.empty");
            }

            if (_bottomHint != null)
            {
                _bottomHint.text = showCollectButton
                    ? LocalizationManager.GetString("condense_tank.hint_collect")
                    : LocalizationManager.GetString("condense_tank.hint_select");
            }

            RefreshWaterProgress(waterProcessActive, waterReadyToCollect);
            RefreshCondensationCollectUi();
        }

        private void OnCollectDirtyWater()
        {
            EnsureSystems();
            if (_gameManager == null || _gameManager.CondensationSystem == null || _gameManager.PlayerInventory == null)
                return;
            if (_gameManager.CondensationSystem.CurrentAccumulation <= 0f)
                return;

            int reward = _gameManager.CollectCondensation();
            if (reward <= 0)
                return;

            _gameManager.PlayerInventory.Add(Items.Water, reward);

            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
                foundation.PostToast("WATER-001", new NotificationPayload().With("amount", reward.ToString()));

            Refresh();
        }

        private void RefreshCondensationCollectUi()
        {
            EnsureSystems();
            float condensation = _gameManager?.CondensationSystem?.CurrentAccumulation ?? 0f;
            int rounded = Mathf.RoundToInt(condensation);

            if (_condensationValue != null)
                _condensationValue.text = $"{rounded}%";
            if (_condensationProgressFill != null)
                _condensationProgressFill.style.width = new Length(Mathf.Clamp01(condensation / 100f) * 100f, LengthUnit.Percent);
            if (_condensationStatusText != null)
            {
                _condensationStatusText.text = condensation > 0f
                    ? LocalizationManager.GetString("condense_tank.condensation.status_ready")
                    : LocalizationManager.GetString("condense_tank.condensation.status_empty");
            }
            if (_btnCollectDirtyWater != null)
                _btnCollectDirtyWater.SetEnabled(condensation > 0f);
        }

        private void RefreshWaterProgress(bool waterProcessActive, bool waterReadyToCollect)
        {
            bool showWaterProgressBlock = waterProcessActive || waterReadyToCollect;
            if (_waterProgressBlock != null)
                _waterProgressBlock.style.display = showWaterProgressBlock ? DisplayStyle.Flex : DisplayStyle.None;
            if (!showWaterProgressBlock || _waterProgressFill == null)
                return;

            var ws = _foodRoom.WaterSlot;
            float totalProgress = waterReadyToCollect && !waterProcessActive
                ? 1f
                : ws.RawWaterInput > 0 ? (ws.PotableWaterOutput + ws.CurrentUnitProgress) / ws.RawWaterInput : 0f;
            _waterProgressFill.style.width = new Length(Mathf.Clamp01(totalProgress) * 100f, LengthUnit.Percent);

            var label = _waterProgressBlock?.Q<Label>("water-progress-label");
            if (label != null)
                label.text = waterReadyToCollect && !waterProcessActive
                    ? LocalizationManager.GetString("food_room.bar_water_ready")
                    : LocalizationManager.GetString("food_room.bar_water_progress");
        }

        private void Update()
        {
            EnsureSystems();
            if (_foodRoom == null) return;

            _foodRoom.TickWaterProduction(Time.deltaTime);

            bool waterActive = _foodRoom.WaterSlot.IsActive;
            if (waterActive)
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

            Refresh();
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
        }
    }
}
