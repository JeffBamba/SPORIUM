using System;
using System.Collections.Generic;
using UnityEngine;
using _Project;
using _Project.Sporae.Core;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.NotificationsFoundation;

namespace _Project.Systems.FoodRoom
{
    public class FoodRoomSystem
    {
        private readonly Inventory _inventory;
        private readonly GameManager _gameManager;
        private FoodRoomConfig _config;
        private readonly List<FoodProductionSlot> _productionSlots = new List<FoodProductionSlot>();
        private readonly WaterProductionSlot _waterSlot = new WaterProductionSlot();

        private const string ToastKeyFoodProgress = "food-room-progress";
        private const string ToastKeyFoodDone = "food-room-done";
        private const string ToastKeyWaterProgress = "water-room-progress";
        private const string ToastKeyWaterDone = "water-room-done";

        public IReadOnlyList<FoodProductionSlot> ProductionSlots => _productionSlots;
        public WaterProductionSlot WaterSlot => _waterSlot;

        public FoodRoomSystem(Inventory inventory, GameManager gameManager)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _gameManager = gameManager ?? throw new ArgumentNullException(nameof(gameManager));
            _config = Resources.Load<FoodRoomConfig>("Configs/FoodRoomConfig");
            if (_config == null)
                SporiumLogger.LogWarning(LogCategory.Core, "FoodRoomConfig non trovato in Resources/Configs/. Userò valori default.");
            EnsureSlots();
        }

        private void EnsureSlots()
        {
            int max = _config != null ? _config.MaxSlots : 1;
            while (_productionSlots.Count < max)
            {
                _productionSlots.Add(new FoodProductionSlot { State = SlotState.Free });
            }
            while (_productionSlots.Count > max)
            {
                _productionSlots.RemoveAt(_productionSlots.Count - 1);
            }
        }

        public bool StartProduction(FoodProductionType type, string stemCellTypeId = null)
        {
            if (type == FoodProductionType.None) return false;
            if (!_gameManager.TrySpendAction(1))
                return false;
            if (stemCellTypeId != null && !_inventory.Consume(stemCellTypeId, 1))
                return false;
            var slot = GetFirstFreeSlot();
            if (slot == null) return false;
            int days = _config != null ? _config.GetDaysFor(type) : (type == FoodProductionType.Vegetable ? 1 : type == FoodProductionType.Fungus ? 2 : 3);
            slot.Type = type;
            slot.DaysRemaining = days;
            slot.StartDay = -1;
            slot.HasStemCell = !string.IsNullOrEmpty(stemCellTypeId);
            slot.StemCellTypeId = stemCellTypeId;
            slot.State = SlotState.Growing;
            RefreshFoodToasts();
            return true;
        }

        public void StartWaterProduction(int rawWaterAmount)
        {
            if (rawWaterAmount <= 0) return;
            if (!_inventory.Has(Items.Water, rawWaterAmount)) return;
            _inventory.Consume(Items.Water, rawWaterAmount);
            _waterSlot.RawWaterInput = rawWaterAmount;
            _waterSlot.PotableWaterOutput = rawWaterAmount;
            _waterSlot.IsActive = true;
            RefreshWaterToasts();
        }

        public bool Harvest(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _productionSlots.Count) return false;
            var slot = _productionSlots[slotIndex];
            if (slot.State != SlotState.Ready) return false;
            int qty = _config != null ? _config.GetOutputQuantityFor(slot.Type) : 1;
            string typeId = _config != null ? _config.GetOutputTypeIdFor(slot.Type) : Items.FoodVegetable;
            if (typeId == null) return false;
            _inventory.Add(typeId, qty);
            slot.State = SlotState.Free;
            slot.Type = FoodProductionType.None;
            slot.DaysRemaining = 0;
            slot.HasStemCell = false;
            slot.StemCellTypeId = null;
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
                foundation.PostToastImmediate("KTCH-FOOD-RITIRA", new NotificationPayload().With("count", qty.ToString()));
            RefreshFoodToasts();
            return true;
        }

        public bool HarvestWater()
        {
            if (!_waterSlot.IsActive || _waterSlot.PotableWaterOutput <= 0) return false;
            int amount = _waterSlot.PotableWaterOutput;
            _inventory.Add(Items.WaterPotable, amount);
            _waterSlot.PotableWaterOutput = 0;
            _waterSlot.RawWaterInput = 0;
            _waterSlot.IsActive = false;
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
                foundation.PostToastImmediate("KTCH-WAT-RITIRA", new NotificationPayload().With("count", amount.ToString()));
            RefreshWaterToasts();
            return true;
        }

        public void ProcessDailyProduction(int currentDay)
        {
            int days = _config != null ? _config.GetDaysFor(FoodProductionType.Meat) : 3;
            for (int i = 0; i < _productionSlots.Count; i++)
            {
                var slot = _productionSlots[i];
                if (slot.State != SlotState.Growing) continue;
                if (slot.StartDay < 0)
                    slot.StartDay = currentDay;
                slot.DaysRemaining--;
                if (slot.Type == FoodProductionType.Meat && slot.State == SlotState.Growing)
                {
                    if (slot.DaysRemaining == 2 || slot.DaysRemaining == 1)
                        _inventory.Add(Items.OrganicResidue, 1);
                    if (slot.DaysRemaining <= 0)
                        _inventory.Add(Items.OrganicResidue, 1);
                }
                if (slot.DaysRemaining <= 0)
                {
                    slot.State = SlotState.Ready;
                    slot.DaysRemaining = 0;
                }
            }
            RefreshFoodToasts();
            RefreshWaterToasts();
        }

        public void ProcessDailyCosts()
        {
            int totalCry = 0;
            foreach (var slot in _productionSlots)
            {
                if (slot.State == SlotState.Growing || slot.State == SlotState.Ready)
                    totalCry += _config != null ? _config.GetCryPerDayFor(slot.Type) : 1;
            }
            if (totalCry > 0 && _gameManager != null && _gameManager.EconomySystem != null && _gameManager.EconomySystem.CanAfford(totalCry))
                _gameManager.EconomySystem.Spend(totalCry);
        }

        private FoodProductionSlot GetFirstFreeSlot()
        {
            foreach (var s in _productionSlots)
                if (s.State == SlotState.Free) return s;
            return null;
        }

        private void RefreshFoodToasts()
        {
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation == null || !foundation.Enabled) return;
            int growing = 0, ready = 0;
            foreach (var s in _productionSlots)
            {
                if (s.State == SlotState.Growing) growing++;
                if (s.State == SlotState.Ready) ready++;
            }
            if (growing > 0)
                foundation.UpsertToast(ToastKeyFoodProgress, "KTCH-FOOD-PROGRESS", new NotificationPayload().With("count", growing.ToString()));
            if (ready > 0)
                foundation.UpsertToast(ToastKeyFoodDone, "KTCH-FOOD-DONE", new NotificationPayload().With("count", ready.ToString()));
        }

        private void RefreshWaterToasts()
        {
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation == null || !foundation.Enabled) return;
            if (_waterSlot.IsActive && _waterSlot.RawWaterInput > 0)
                foundation.UpsertToast(ToastKeyWaterProgress, "KTCH-WAT-PROGRESS", new NotificationPayload().With("count", _waterSlot.RawWaterInput.ToString()));
            if (_waterSlot.IsActive && _waterSlot.PotableWaterOutput > 0)
                foundation.UpsertToast(ToastKeyWaterDone, "KTCH-WAT-DONE", new NotificationPayload().With("count", _waterSlot.PotableWaterOutput.ToString()));
        }

        /// <summary>Ripristina stato da save. typeInt/stateInt sono valori enum come int.</summary>
        public void RestoreState(List<(int typeInt, int daysRemaining, int startDay, bool hasStemCell, string stemCellTypeId, int stateInt)> slots, int waterRawInput, int waterPotableOutput, bool waterActive)
        {
            _productionSlots.Clear();
            int max = _config != null ? _config.MaxSlots : 3;
            for (int i = 0; i < max; i++)
            {
                var slot = new FoodProductionSlot { State = SlotState.Free };
                if (slots != null && i < slots.Count)
                {
                    var s = slots[i];
                    slot.Type = (FoodProductionType)s.typeInt;
                    slot.DaysRemaining = s.daysRemaining;
                    slot.StartDay = s.startDay;
                    slot.HasStemCell = s.hasStemCell;
                    slot.StemCellTypeId = s.stemCellTypeId;
                    slot.State = (SlotState)s.stateInt;
                }
                _productionSlots.Add(slot);
            }
            _waterSlot.RawWaterInput = waterRawInput;
            _waterSlot.PotableWaterOutput = waterPotableOutput;
            _waterSlot.IsActive = waterActive;
        }
    }
}
