using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Sporae.Core
{
    public class Inventory
    {
        private readonly Dictionary<string, InventorySlot> _slots = new();
        
        public IReadOnlyCollection<InventorySlot> Items => _slots.Values;
        public int UniqueItems => _slots.Count;
        public event Action OnInventoryChanged;

        public void Add(string typeId, int amount)
        {
            for (var i = 0; i < amount; i++)
                Add(typeId);
        }
        
        public void Add(string typeId)
        {
            Add(ItemFabric.CreateItemByType(typeId));
        }
        
        public void Add(Item item)
        {
            if (item == null)
                Debug.LogError("[Inventory.Add] Item is null");
            
            if (_slots.TryGetValue(item.TypeId, out InventorySlot existingItem))
                existingItem.AddItem(item);
            else
            {
                _slots[item.TypeId] = new InventorySlot(item.TypeId);
                _slots[item.TypeId].AddItem(item);
            }

            OnInventoryChanged?.Invoke();
        }

        public bool Has(string typeId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(typeId) || quantity <= 0) 
                return false;
            
            return _slots.TryGetValue(typeId, out var slot) && slot.Quantity >= quantity;
        }

        public bool Consume(string typeId, int quantity = 1)
        {
            if (!_slots.TryGetValue(typeId, out var slot)) 
                return false;
            
            if (!slot.TryRemoveQuantity(quantity))
                return false;
            
            if (slot.IsEmpty)
                _slots.Remove(typeId);
            
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        public bool IsEmpty => _slots.Count == 0;
    }
}
