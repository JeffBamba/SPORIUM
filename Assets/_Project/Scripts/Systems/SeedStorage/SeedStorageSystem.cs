using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Sporae.Core;
using Sporae.Dome.PotSystem.Growth;
using Sporae.UI.UIToolkit.NotificationsFoundation;
using UnityEngine;

namespace _Project.Systems.SeedStorage
{
    /// <summary>
    /// EXT-002 — Seed Storage (camera fredda Vault): slot tipizzati, costi giornalieri a tier, power ON/OFF.
    /// </summary>
    public sealed class SeedStorageSystem
    {
        public const int SlotCount = 6;
        private const int Tier1SlotCount = 3;
        private const int CryTier1Occupied = 1;
        private const int CryTier2Occupied = 3;

        private readonly GameManager _gameManager;

        private bool _isOn = true;
        private bool _extendedSlotsUnlocked;

        private readonly List<StoredUnit>[] _slots = new List<StoredUnit>[SlotCount];

        public event Action StorageChanged;
        public event Action<bool> PowerChanged;

        public bool IsOn => _isOn;
        public bool ExtendedSlotsUnlocked => _extendedSlotsUnlocked;

        public SeedStorageSystem(GameManager gameManager)
        {
            _gameManager = gameManager ?? throw new ArgumentNullException(nameof(gameManager));
        }

        public static bool IsEligible(Item item)
        {
            if (item?.ItemConfig == null)
                return false;
            var id = item.TypeId;
            if (string.IsNullOrEmpty(id))
                return false;
            if (id == Items.FoodVegetable || id == Items.FoodFungus || id == Items.FoodMeat)
                return false;
            if (id == Items.Water || id == Items.WaterPotable)
                return false;
            if (id == Items.OrganicResidue)
                return false;
            if (id == Items.WholePlant || id == Items.SporeGeneric)
                return true;
            if (Items.IsFruitType(id, includeLegacy: true))
                return true;
            if (id == Items.PreSeed || id == Items.Seed001 || id == Items.Seed002 || id == Items.Seed003)
                return true;
            var pdb = PlantDatabase.Instance;
            return pdb != null && pdb.IsRegisteredSeedTypeId(id);
        }

        private static bool ShouldDeteriorateLikePlayerInventory(Item item)
        {
            if (item?.ItemConfig == null)
                return false;
            var id = item.TypeId;
            if (id == Items.SporeGeneric || id == Items.WholePlant)
                return true;
            if (Items.IsFruitType(id, includeLegacy: true))
                return true;
            if (id == Items.FoodVegetable || id == Items.FoodFungus || id == Items.FoodMeat)
                return true;
            var pdb = PlantDatabase.Instance;
            return pdb != null && pdb.IsRegisteredSeedTypeId(id);
        }

        public bool IsSlotUnlocked(int index)
        {
            if (index < 0 || index >= SlotCount)
                return false;
            return index < Tier1SlotCount || _extendedSlotsUnlocked;
        }

        public bool SlotIsEmpty(int index)
        {
            if (index < 0 || index >= SlotCount)
                return true;
            return _slots[index] == null || _slots[index].Count == 0;
        }

        public int GetSlotQuantity(int index)
        {
            if (index < 0 || index >= SlotCount || _slots[index] == null)
                return 0;
            return _slots[index].Count;
        }

        public string GetSlotTypeId(int index)
        {
            if (index < 0 || index >= SlotCount || _slots[index] == null || _slots[index].Count == 0)
                return null;
            return _slots[index][0].Item?.TypeId;
        }

        public float GetSlotViabilityRatio(int index)
        {
            if (index < 0 || index >= SlotCount || _slots[index] == null || _slots[index].Count == 0)
                return 0f;
            float sum = 0f;
            float maxSum = 0f;
            foreach (var u in _slots[index])
            {
                var item = u.Item;
                if (item?.ItemConfig == null)
                    continue;
                float mq = Mathf.Max(1f, item.ItemConfig.MaxQuality);
                float q = _isOn && u.FrozenQualityUi.HasValue ? u.FrozenQualityUi.Value : item.Quality;
                sum += Mathf.Clamp(q, 0f, mq);
                maxSum += mq;
            }
            return maxSum > 0.01f ? Mathf.Clamp01(sum / maxSum) : 0f;
        }

        public IReadOnlyList<StoredUnit> GetSlotUnits(int index)
        {
            if (index < 0 || index >= SlotCount || _slots[index] == null)
                return Array.Empty<StoredUnit>();
            return _slots[index];
        }

        public int CountTypeInStorage(string typeId)
        {
            if (string.IsNullOrEmpty(typeId))
                return 0;
            int n = 0;
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i] == null)
                    continue;
                foreach (var u in _slots[i])
                {
                    if (u.Item != null && string.Equals(u.Item.TypeId, typeId, StringComparison.OrdinalIgnoreCase))
                        n++;
                }
            }
            return n;
        }

        public void GetSeedSummaryCounts(out int preSeed, out Dictionary<string, int> seedByTypeId)
        {
            preSeed = 0;
            seedByTypeId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var pdb = PlantDatabase.Instance;
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i] == null)
                    continue;
                foreach (var u in _slots[i])
                {
                    var item = u.Item;
                    if (item == null)
                        continue;
                    if (item.TypeId == Items.PreSeed)
                        preSeed++;
                    else if (pdb != null && pdb.IsRegisteredSeedTypeId(item.TypeId))
                    {
                        if (!seedByTypeId.TryGetValue(item.TypeId, out int c))
                            c = 0;
                        seedByTypeId[item.TypeId] = c + 1;
                    }
                }
            }
        }

        public int ComputeDailyCryCost()
        {
            if (!_isOn)
                return 0;
            int total = 0;
            for (int i = 0; i < SlotCount; i++)
            {
                if (!IsSlotUnlocked(i) || SlotIsEmpty(i))
                    continue;
                total += i < Tier1SlotCount ? CryTier1Occupied : CryTier2Occupied;
            }
            return total;
        }

        public bool SetPower(bool isOn)
        {
            if (_isOn == isOn)
                return false;
            _isOn = isOn;
            if (_isOn)
            {
                for (int i = 0; i < SlotCount; i++)
                {
                    if (_slots[i] == null)
                        continue;
                    foreach (var u in _slots[i])
                    {
                        if (u.Item != null)
                            u.FrozenQualityUi = u.Item.Quality;
                    }
                }
            }
            else
            {
                for (int i = 0; i < SlotCount; i++)
                {
                    if (_slots[i] == null)
                        continue;
                    foreach (var u in _slots[i])
                        u.FrozenQualityUi = null;
                }
            }

            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
                foundation.PostToastImmediate(_isOn ? "VAULT-SS-ON" : "VAULT-SS-OFF");

            PowerChanged?.Invoke(_isOn);
            StorageChanged?.Invoke();
            return true;
        }

        public bool TryUnlockExtendedSlots()
        {
            if (_extendedSlotsUnlocked)
                return false;
            _extendedSlotsUnlocked = true;
            StorageChanged?.Invoke();
            return true;
        }

        public bool TryDepositItems(IReadOnlyList<Item> items, out string error)
        {
            error = null;
            if (items == null || items.Count == 0)
            {
                error = "empty";
                return false;
            }

            var inv = _gameManager.PlayerInventory;
            foreach (var item in items)
            {
                if (item == null || !IsEligible(item))
                {
                    error = "ineligible";
                    return false;
                }
                if (!InventoryContainsExact(inv, item))
                {
                    error = "not_in_inventory";
                    return false;
                }
            }

            if (!CanDepositAllInSimulation(items))
            {
                error = "no_room";
                return false;
            }

            if (!_gameManager.TrySpendAction(1))
            {
                error = "no_ap";
                return false;
            }

            var movedDetails = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                if (!inv.TryRemoveExactItem(item, out var removed) || removed == null)
                {
                    error = "remove_failed";
                    return false;
                }
                PlaceItem(removed);
                string typeId = removed.TypeId ?? "?";
                if (movedDetails.TryGetValue(typeId, out int current))
                    movedDetails[typeId] = current + 1;
                else
                    movedDetails[typeId] = 1;
            }

            var dayLog = ServiceContainer.Instance?.Get<DayActivityLog>(suppressWarning: true);
            if (dayLog != null)
            {
                string detail = string.Join(", ", movedDetails.Select(kv => $"{kv.Key} x{kv.Value}"));
                dayLog.RecordSeedStorageAction("Deposit", items.Count, detail);
            }
            StorageChanged?.Invoke();
            return true;
        }

        public bool TryWithdrawFromSlots(IReadOnlyList<int> slotIndices, out string error)
        {
            error = null;
            if (slotIndices == null || slotIndices.Count == 0)
            {
                error = "empty";
                return false;
            }

            var distinct = slotIndices.Distinct().ToList();
            foreach (var idx in distinct)
            {
                if (idx < 0 || idx >= SlotCount || !IsSlotUnlocked(idx) || SlotIsEmpty(idx))
                {
                    error = "bad_slot";
                    return false;
                }
            }

            if (!_gameManager.TrySpendAction(1))
            {
                error = "no_ap";
                return false;
            }

            var inv = _gameManager.PlayerInventory;
            var movedDetails = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int withdrawnCount = 0;
            foreach (var idx in distinct)
            {
                var list = _slots[idx];
                if (list == null)
                    continue;
                foreach (var u in list.ToList())
                {
                    if (u.Item != null)
                    {
                        inv.Add(u.Item);
                        withdrawnCount++;
                        string typeId = u.Item.TypeId ?? "?";
                        if (movedDetails.TryGetValue(typeId, out int current))
                            movedDetails[typeId] = current + 1;
                        else
                            movedDetails[typeId] = 1;
                    }
                }
                _slots[idx] = null;
            }

            var dayLog = ServiceContainer.Instance?.Get<DayActivityLog>(suppressWarning: true);
            if (dayLog != null && withdrawnCount > 0)
            {
                string detail = string.Join(", ", movedDetails.Select(kv => $"{kv.Key} x{kv.Value}"));
                dayLog.RecordSeedStorageAction("Withdraw", withdrawnCount, detail);
            }
            StorageChanged?.Invoke();
            return true;
        }

        public void ProcessDailyDecayIfPoweredOff()
        {
            if (_isOn)
                return;

            int decayed = 0;
            var inv = _gameManager.PlayerInventory;
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i] == null)
                    continue;
                for (int k = _slots[i].Count - 1; k >= 0; k--)
                {
                    var u = _slots[i][k];
                    var item = u.Item;
                    if (item == null)
                    {
                        _slots[i].RemoveAt(k);
                        continue;
                    }
                    if (!ShouldDeteriorateLikePlayerInventory(item))
                        continue;

                    item.Quality -= 1f;
                    decayed++;
                    if (item.Quality > 0f)
                        continue;

                    inv.Add(Items.OrganicResidue);
                    _slots[i].RemoveAt(k);
                }
                if (_slots[i] != null && _slots[i].Count == 0)
                    _slots[i] = null;
            }

            if (decayed <= 0)
                return;

            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
            {
                foundation.PostToastImmediate(
                    "VAULT-SS-DECAY-TICK",
                    new NotificationPayload().With("count", decayed.ToString()));
            }

            StorageChanged?.Invoke();
        }

        public void ProcessDailyCosts()
        {
            int cost = ComputeDailyCryCost();
            if (cost <= 0)
                return;
            var eco = _gameManager.EconomySystem;
            if (eco != null && eco.CanAfford(cost))
                eco.Spend(cost);
        }

        public void ClearAllSlotsForNewGame()
        {
            for (int i = 0; i < SlotCount; i++)
                _slots[i] = null;
            _isOn = true;
            _extendedSlotsUnlocked = false;
            StorageChanged?.Invoke();
        }

        public void LoadState(bool isOn, bool extended, List<StoredUnit>[] slots)
        {
            _isOn = isOn;
            _extendedSlotsUnlocked = extended;
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots != null && i < slots.Length && slots[i] != null && slots[i].Count > 0)
                    _slots[i] = slots[i];
                else
                    _slots[i] = null;
            }
            StorageChanged?.Invoke();
        }

        private int CountStoredItemUnits()
        {
            int n = 0;
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i] == null)
                    continue;
                n += _slots[i].Count;
            }
            return n;
        }

        private static bool InventoryContainsExact(Inventory inv, Item item)
        {
            if (inv == null || item == null)
                return false;
            return inv.Items
                .Where(s => s.TypeId == item.TypeId)
                .SelectMany(s => s.Items)
                .Any(i => ReferenceEquals(i, item));
        }

        private bool CanDepositAllInSimulation(IReadOnlyList<Item> items)
        {
            var ghost = CloneSlotsGhost();
            foreach (var item in items)
            {
                int? slot = FindDepositTargetInGhost(ghost, item);
                if (!slot.HasValue)
                    return false;
                AddToGhost(ghost, slot.Value, item);
            }
            return true;
        }

        private List<StoredUnit>[] CloneSlotsGhost()
        {
            var g = new List<StoredUnit>[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i] == null || _slots[i].Count == 0)
                    g[i] = null;
                else
                    g[i] = new List<StoredUnit>(_slots[i]);
            }
            return g;
        }

        private int? FindDepositTargetInGhost(List<StoredUnit>[] ghost, Item item)
        {
            // Stesso TypeId → un solo slot (come inventario cumulabile): non serve CanStack.
            for (int i = 0; i < SlotCount; i++)
            {
                if (!IsSlotUnlocked(i))
                    continue;
                if (ghost[i] == null || ghost[i].Count == 0)
                    continue;
                if (string.Equals(ghost[i][0].Item.TypeId, item.TypeId, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            for (int i = 0; i < SlotCount; i++)
            {
                if (!IsSlotUnlocked(i))
                    continue;
                if (ghost[i] == null || ghost[i].Count == 0)
                    return i;
            }
            return null;
        }

        private static void AddToGhost(List<StoredUnit>[] ghost, int slotIndex, Item item)
        {
            float? frozen = 1f;
            var u = new StoredUnit(item, frozen);
            if (ghost[slotIndex] == null)
                ghost[slotIndex] = new List<StoredUnit>();
            ghost[slotIndex].Add(u);
        }

        private int? FindDepositTargetSlot(Item item)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (!IsSlotUnlocked(i))
                    continue;
                if (_slots[i] == null || _slots[i].Count == 0)
                    continue;
                if (string.Equals(_slots[i][0].Item.TypeId, item.TypeId, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            for (int i = 0; i < SlotCount; i++)
            {
                if (!IsSlotUnlocked(i))
                    continue;
                if (_slots[i] == null || _slots[i].Count == 0)
                    return i;
            }
            return null;
        }

        private void PlaceItem(Item item)
        {
            int? idx = FindDepositTargetSlot(item);
            if (!idx.HasValue)
                return;
            float? frozen = _isOn ? item.Quality : (float?)null;
            var unit = new StoredUnit(item, frozen);
            if (_slots[idx.Value] == null)
                _slots[idx.Value] = new List<StoredUnit>();
            _slots[idx.Value].Add(unit);
        }

        public sealed class StoredUnit
        {
            public Item Item;
            public float? FrozenQualityUi;

            public StoredUnit(Item item, float? frozenQualityUi)
            {
                Item = item;
                FrozenQualityUi = frozenQualityUi;
            }
        }
    }
}
