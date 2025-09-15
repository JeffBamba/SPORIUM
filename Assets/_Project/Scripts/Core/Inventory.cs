using System;
using System.Collections.Generic;
using System.Linq;
using Sporae.Core.Interfaces;

namespace Sporae.Core
{
    public class Inventory : IInventorySystem
    {
        private readonly Dictionary<string, InventoryItem> _items = new Dictionary<string, InventoryItem>();
        
        public IReadOnlyCollection<InventoryItem> Items => _items.Values;
        public int TotalItems => _items.Values.Sum(item => item.Quantity);
        public int UniqueItems => _items.Count;
        
        public event Action OnInventoryChanged;

        public void Add(string id, int quantity = 1)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID non può essere null o vuoto", nameof(id));
            
            if (quantity <= 0)
                throw new ArgumentException("Quantità deve essere maggiore di 0", nameof(quantity));

            if (_items.TryGetValue(id, out InventoryItem existingItem))
            {
                existingItem.AddQuantity(quantity);
            }
            else
            {
                _items[id] = new InventoryItem(id, quantity);
            }
            
            OnInventoryChanged?.Invoke();
        }

        public bool Has(string id, int quantity = 1)
        {
            if (string.IsNullOrEmpty(id) || quantity <= 0) return false;
            
            return _items.TryGetValue(id, out InventoryItem item) && item.Quantity >= quantity;
        }

        public bool Consume(string id, int quantity = 1)
        {
            if (string.IsNullOrEmpty(id) || quantity <= 0) return false;
            
            if (!_items.TryGetValue(id, out InventoryItem item)) return false;
            
            if (item.Quantity < quantity) return false;
            
            if (item.RemoveQuantity(quantity))
            {
                if (item.IsEmpty)
                {
                    _items.Remove(id);
                }
                
                OnInventoryChanged?.Invoke();
                return true;
            }
            
            return false;
        }

        public int GetQuantity(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            
            return _items.TryGetValue(id, out InventoryItem item) ? item.Quantity : 0;
        }

        public bool Remove(string id, int quantity = 1)
        {
            return Consume(id, quantity);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool IsEmpty => _items.Count == 0;

        public List<string> GetAllItemIds()
        {
            return _items.Keys.ToList();
        }

        public bool Contains(string id)
        {
            return !string.IsNullOrEmpty(id) && _items.ContainsKey(id);
        }

        // Implementazione interfaccia IInventorySystem
        public bool AddItem(string id, int quantity = 1)
        {
            try
            {
                Add(id, quantity);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool RemoveItem(string id, int quantity = 1)
        {
            return Remove(id, quantity);
        }

        public IReadOnlyCollection<InventoryItem> GetAllItems()
        {
            return Items;
        }

        // Metodi aggiuntivi per compatibilità con l'interfaccia
        public bool HasItem(string id, int quantity = 1)
        {
            return Has(id, quantity);
        }

        public int GetItemQuantity(string id)
        {
            return GetQuantity(id);
        }

        public override string ToString()
        {
            return $"Inventory: {TotalItems} items ({UniqueItems} unique)";
        }
    }
}
