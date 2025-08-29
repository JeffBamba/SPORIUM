using System;
using UnityEngine;

namespace Sporae.Core
{
    [Serializable]
    public class InventoryItem
    {
        [SerializeField] private string _id;
        [SerializeField] private int _quantity;

        public string Id => _id;
        public int Quantity 
        { 
            get => _quantity;
            private set => _quantity = Math.Max(0, value);
        }

        public InventoryItem(string id, int quantity = 1)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID non può essere null o vuoto", nameof(id));
            
            _id = id;
            Quantity = quantity;
        }

        public bool AddQuantity(int amount)
        {
            if (amount <= 0) return false;
            
            Quantity += amount;
            return true;
        }

        public bool RemoveQuantity(int amount)
        {
            if (amount <= 0 || amount > Quantity) return false;
            
            Quantity -= amount;
            return true;
        }

        public bool IsEmpty => Quantity <= 0;

        public override string ToString()
        {
            return $"{_id} x{_quantity}";
        }
    }
}
