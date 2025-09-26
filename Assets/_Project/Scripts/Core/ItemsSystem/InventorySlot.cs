using System;
using System.Collections.Generic;
using System.Linq;

namespace _Project.Sporae.Core
{
    [Serializable]
    public class InventorySlot
    {
        private List<Item> _items = new();
        private string _typeId;

        public IReadOnlyCollection<Item> Items => _items.AsReadOnly();
        public int Quantity => _items.Count;
        public string TypeId => _typeId;

        public InventorySlot(string typeId)
        {
            _typeId = typeId;
        }
        
        public bool AddItemRange(List<Item> items)
        {
            if (
                TypeId != string.Empty &&
                items.Any(item => item.TypeId != TypeId)
                )
                return false;
            
            _items.AddRange(items);
            return true;
        }
        
        public bool AddItem(Item item)
        {
            return AddItemRange(new(){ item });
        }

        public bool RemoveItem(Item item)
        {
            if (item.TypeId != TypeId)
                return false;
            
            _items.Remove(item);
            return true;
        }

        public bool TryRemoveQuantity(int quantity)
        {
            if (_items.Count < quantity)
                return false;
            
            _items.RemoveRange(0, quantity);
            return true;
        }
        
        public bool TryRemoveFirst()
        {
            if (_items.Count <= 0)
                return false;
            
            _items.RemoveAt(0);
            return true;
        } 
        
        public bool IsEmpty => Quantity <= 0;
    }
}
