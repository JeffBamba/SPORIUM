using System;
using UnityEngine;

namespace Sporae.Core
{
    [Serializable]
    public class InventoryItem
    {
        [SerializeField] private string _id;
        [SerializeField] private int _quantity;
        [SerializeField] private int _quality;
        
        public string Id => _id;
        public int Quantity 
        { 
            get => _quantity;
            private set => _quantity = Math.Max(0, value);
        }

        public int Quality
        {
            get => _quality;
            set => _quality = Math.Max(0, value);
        }

        public InventoryItem(string id, int quantity = 1, int quality = 4)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID non può essere null o vuoto", nameof(id));
            
            _id = id;
            Quantity = quantity;
            Quality = quality;
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
