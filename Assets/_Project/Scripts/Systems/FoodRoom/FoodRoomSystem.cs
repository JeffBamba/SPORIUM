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
        private readonly Dictionary<FoodProductionType, List<Item>> _pantryByType = new Dictionary<FoodProductionType, List<Item>>
        {
            { FoodProductionType.Vegetable, new List<Item>() },
            { FoodProductionType.Fungus, new List<Item>() },
            { FoodProductionType.Meat, new List<Item>() }
        };
        private bool _pantryIsOn = true;

        private const string ToastKeyFoodProgress = "food-room-progress";
        private const string ToastKeyFoodDone = "food-room-done";
        private const string ToastKeyWaterProgress = "water-room-progress";
        private const string ToastKeyWaterDone = "water-room-done";
        private const int PantryDailyCryCost = 5;

        public IReadOnlyList<FoodProductionSlot> ProductionSlots => _productionSlots;
        public WaterProductionSlot WaterSlot => _waterSlot;
        public bool PantryIsOn => _pantryIsOn;
        public int PantryDailyCost => PantryDailyCryCost;

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

        public int GetPantryQuantity(FoodProductionType type)
        {
            if (!_pantryByType.TryGetValue(type, out var list))
                return 0;
            return list.Count;
        }

        public bool SetPantryPower(bool isOn)
        {
            if (_pantryIsOn == isOn)
                return false;

            _pantryIsOn = isOn;
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
            {
                foundation.PostToastImmediate(isOn ? "KTCH-PTRY-ON" : "KTCH-PTRY-OFF");
            }

            return true;
        }

        public bool TryTransferToPantry(FoodProductionType type, int amount, out int moved)
        {
            moved = 0;
            if (amount <= 0)
                return false;
            if (!_pantryByType.TryGetValue(type, out var pantryBucket))
                return false;

            string foodTypeId = GetFoodTypeIdByPantryType(type);
            if (string.IsNullOrEmpty(foodTypeId))
                return false;

            for (int i = 0; i < amount; i++)
            {
                if (!_inventory.TryRemoveFirst(foodTypeId, out var removedItem) || removedItem == null)
                    break;
                pantryBucket.Add(removedItem);
                moved++;
            }

            if (moved > 0)
            {
                PostPantryTransferToast("KTCH-PTRY-IN", type, moved);
                return true;
            }

            return false;
        }

        public bool TryTransferFromPantry(FoodProductionType type, int amount, out int moved)
        {
            moved = 0;
            if (amount <= 0)
                return false;
            if (!_pantryByType.TryGetValue(type, out var pantryBucket) || pantryBucket.Count <= 0)
                return false;

            for (int i = 0; i < amount; i++)
            {
                if (pantryBucket.Count <= 0)
                    break;
                var item = pantryBucket[0];
                pantryBucket.RemoveAt(0);
                _inventory.Add(item);
                moved++;
            }

            if (moved > 0)
            {
                PostPantryTransferToast("KTCH-PTRY-OUT", type, moved);
                return true;
            }

            return false;
        }

        public void ExportPantryState(out bool pantryIsOn, out List<(int typeInt, float quality)> pantryItems)
        {
            pantryIsOn = _pantryIsOn;
            pantryItems = new List<(int typeInt, float quality)>();

            foreach (var kvp in _pantryByType)
            {
                int typeInt = (int)kvp.Key;
                var list = kvp.Value;
                if (list == null) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    if (item == null) continue;
                    pantryItems.Add((typeInt, item.Quality));
                }
            }
        }

        private void PostPantryTransferToast(string code, FoodProductionType type, int moved)
        {
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation == null || !foundation.Enabled || moved <= 0)
                return;

            foundation.PostToastImmediate(
                code,
                new NotificationPayload()
                    .With("count", moved.ToString())
                    .With("itemName", GetFoodTypeDisplayName(type)));
        }

        private static string GetFoodTypeIdByPantryType(FoodProductionType type)
        {
            switch (type)
            {
                case FoodProductionType.Vegetable: return Items.FoodVegetable;
                case FoodProductionType.Fungus: return Items.FoodFungus;
                case FoodProductionType.Meat: return Items.FoodMeat;
                default: return null;
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
            if (!_gameManager.TrySpendAction(1)) return;
            if (!_inventory.Has(Items.Water, rawWaterAmount)) return;
            _inventory.Consume(Items.Water, rawWaterAmount);
            _waterSlot.RawWaterInput = rawWaterAmount;
            _waterSlot.PotableWaterOutput = 0;
            _waterSlot.CurrentUnitProgress = 0f;
            _waterSlot.IsActive = true;
            RefreshWaterToasts();
        }

        /// <summary>Call every frame with Time.deltaTime to advance real-time purification (2 min per unit).</summary>
        public void TickWaterProduction(float deltaTime)
        {
            if (!_waterSlot.IsActive || _waterSlot.RawWaterInput <= 0) return;
            _waterSlot.CurrentUnitProgress += deltaTime / WaterProductionSlot.SecondsPerUnit;
            while (_waterSlot.CurrentUnitProgress >= 1f && _waterSlot.PotableWaterOutput < _waterSlot.RawWaterInput)
            {
                _waterSlot.PotableWaterOutput++;
                _waterSlot.CurrentUnitProgress -= 1f;
                if (_waterSlot.PotableWaterOutput >= _waterSlot.RawWaterInput)
                {
                    _waterSlot.IsActive = false;
                    _waterSlot.CurrentUnitProgress = 0f;
                    var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                    if (foundation != null && foundation.Enabled)
                    {
                        foundation.RemoveToast(ToastKeyWaterProgress);
                        foundation.PostToastImmediate("KTCH-WAT-DONE", new NotificationPayload().With("count", _waterSlot.PotableWaterOutput.ToString()));
                    }
                    return;
                }
            }
            if (_waterSlot.CurrentUnitProgress >= 1f)
                _waterSlot.CurrentUnitProgress = 1f;
            RefreshWaterToasts();
        }

        /// <summary>Avanza la potabilizzazione di un numero di secondi reali (es. notte dopo End of Day). Chiamare quando il giorno cambia così al mattino i processi sono completati.</summary>
        public void AdvanceWaterProductionByRealSeconds(float seconds)
        {
            if (seconds <= 0) return;
            TickWaterProduction(seconds);
        }

        public bool Harvest(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _productionSlots.Count) return false;
            var slot = _productionSlots[slotIndex];
            if (slot.State != SlotState.Ready) return false;
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
            {
                foundation.RemoveToast(ToastKeyFoodProgress);
                foundation.RemoveToast(ToastKeyFoodDone);
            }
            int qty = _config != null ? _config.GetOutputQuantityFor(slot.Type) : 1;
            string typeId = _config != null ? _config.GetOutputTypeIdFor(slot.Type) : Items.FoodVegetable;
            if (typeId == null) return false;
            string foodTypeName = GetFoodTypeDisplayName(slot.Type);
            _inventory.Add(typeId, qty);
            if (foundation != null && foundation.Enabled)
                foundation.PostAddedToInventory(typeId, foodTypeName, qty, RoomNames.Kitchen);
            /* Prodotti aggiuntivi dall'harvest (es. Res Protein dalla carne): aggiungi a inventario e toast separato per ognuno */
            AddHarvestBonusItemsAndToasts(slot.Type, foundation);
            slot.State = SlotState.Free;
            slot.Type = FoodProductionType.None;
            slot.DaysRemaining = 0;
            slot.HasStemCell = false;
            slot.StemCellTypeId = null;
            RefreshFoodToasts();
            return true;
        }

        private void AddHarvestBonusItemsAndToasts(FoodProductionType type, FoundationNotificationService foundation)
        {
            if (foundation == null || !foundation.Enabled) return;
            switch (type)
            {
                case FoodProductionType.Meat:
                    int resProtQty = 1;
                    _inventory.Add(Items.ProteinResidue, resProtQty);
                    foundation.PostAddedToInventory(Items.ProteinResidue, "Proteina residua", resProtQty, RoomNames.Kitchen);
                    break;
                default:
                    break;
            }
        }

        public bool HarvestWater()
        {
            if (_waterSlot.PotableWaterOutput <= 0) return false;
            int amount = _waterSlot.PotableWaterOutput;
            _inventory.Add(Items.WaterPotable, amount);
            _waterSlot.PotableWaterOutput = 0;
            _waterSlot.RawWaterInput = 0;
            _waterSlot.IsActive = false;
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
                foundation.PostAddedToInventory(Items.WaterPotable, "Acqua potabile", amount, RoomNames.Kitchen);
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
            if (_pantryIsOn)
                totalCry += PantryDailyCryCost;
            if (totalCry > 0 && _gameManager != null && _gameManager.EconomySystem != null && _gameManager.EconomySystem.CanAfford(totalCry))
                _gameManager.EconomySystem.Spend(totalCry);
        }

        /// <summary>
        /// Chiamare all'alba se la dispensa è spenta: il cibo nel pantry deperisce come in inventario (-1 Quality, 0 → residuo organico in inventario).
        /// </summary>
        public void ProcessDailyDecayIfPantryOff()
        {
            if (_pantryIsOn)
                return;

            int count = 0;
            foreach (var kvp in _pantryByType)
            {
                if (kvp.Value != null)
                    count += kvp.Value.Count;
            }
            if (count <= 0)
                return;

            foreach (var kvp in _pantryByType)
            {
                var list = kvp.Value;
                if (list == null)
                    continue;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var item = list[i];
                    if (item == null)
                    {
                        list.RemoveAt(i);
                        continue;
                    }
                    item.Quality -= 1f;
                    if (item.Quality > 0f)
                        continue;
                    _inventory.Add(Items.OrganicResidue);
                    list.RemoveAt(i);
                }
            }

            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
            {
                foundation.PostToastImmediate(
                    "KTCH-PTRY-DECAY-TICK",
                    new NotificationPayload().With("count", count.ToString()));
            }
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
            if (ready > 0)
            {
                foundation.RemoveToast(ToastKeyFoodProgress);
                foundation.UpsertToast(ToastKeyFoodDone, "KTCH-FOOD-DONE", new NotificationPayload().With("count", ready.ToString()));
            }
            else if (growing > 0)
            {
                int daysRemaining = 0;
                foreach (var s in _productionSlots)
                {
                    if (s.State == SlotState.Growing)
                    {
                        daysRemaining = s.DaysRemaining;
                        break;
                    }
                }
                foundation.UpsertToast(ToastKeyFoodProgress, "KTCH-FOOD-PROGRESS",
                    new NotificationPayload().With("count", growing.ToString()).With("daysRemaining", daysRemaining.ToString()));
            }
        }

        private static string GetFoodTypeDisplayName(FoodProductionType type)
        {
            switch (type)
            {
                case FoodProductionType.Vegetable: return "Vegetal Synthesis";
                case FoodProductionType.Fungus: return "Fungal Synthesis";
                case FoodProductionType.Meat: return "Meat Synthesis";
                default: return "Cibo";
            }
        }

        /// <summary>Chiamato periodicamente dal panel o da tick per mantenere il toast PROGRESS visibile per tutta la durata.</summary>
        public void RefreshToasts()
        {
            RefreshFoodToasts();
            RefreshWaterToasts();
        }

        private void RefreshWaterToasts()
        {
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation == null || !foundation.Enabled) return;
            if (_waterSlot.IsActive && _waterSlot.RawWaterInput > 0)
            {
                float totalProgress = (_waterSlot.PotableWaterOutput + _waterSlot.CurrentUnitProgress) / _waterSlot.RawWaterInput;
                int percent = Mathf.Clamp(Mathf.RoundToInt(totalProgress * 100f), 0, 100);
                foundation.UpsertToast(ToastKeyWaterProgress, "KTCH-WAT-PROGRESS",
                    new NotificationPayload().With("percent", percent.ToString()).With("count", _waterSlot.RawWaterInput.ToString()));
            }
        }

        /// <summary>Ripristina stato da save. typeInt/stateInt sono valori enum come int.</summary>
        public void RestoreState(
            List<(int typeInt, int daysRemaining, int startDay, bool hasStemCell, string stemCellTypeId, int stateInt)> slots,
            int waterRawInput,
            int waterPotableOutput,
            float waterCurrentProgress,
            bool waterActive,
            bool pantryIsOn,
            List<(int typeInt, float quality)> pantryItems)
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
            _waterSlot.CurrentUnitProgress = waterCurrentProgress;
            _waterSlot.IsActive = waterActive;

            _pantryIsOn = pantryIsOn;
            foreach (var key in _pantryByType.Keys)
                _pantryByType[key].Clear();

            if (pantryItems == null)
                return;

            for (int i = 0; i < pantryItems.Count; i++)
            {
                var entry = pantryItems[i];
                var type = (FoodProductionType)entry.typeInt;
                if (!_pantryByType.TryGetValue(type, out var bucket))
                    continue;

                string typeId = GetFoodTypeIdByPantryType(type);
                if (string.IsNullOrWhiteSpace(typeId))
                    continue;

                var item = ItemFabric.CreateItemWithQuality(typeId, Mathf.Max(0.1f, entry.quality));
                if (item != null)
                    bucket.Add(item);
            }
        }
    }
}
