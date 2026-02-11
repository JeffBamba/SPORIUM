using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sporae.DevTools;

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

        /// <summary>Aggiunge spore con metadata Raw + Stabile (es. output Extractor).</summary>
        public void AddSporeRaw(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var item = ItemFabric.CreateSporeWithFallbackMetadata();
                if (item != null) Add(item);
            }
        }

        /// <summary>Aggiunge spore con metadata Maturata + Stabile (es. output Catalizzatore).</summary>
        public void AddSporeMatured(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var item = ItemFabric.CreateSporeMatured();
                if (item != null) Add(item);
            }
        }
        
        public void Add(string typeId)
        {
            Add(ItemFabric.CreateItemByType(typeId));
        }
        
        public void Add(Item item)
        {
            if (item == null)
            {
                SporiumLogger.LogError(LogCategory.Inventory, "Item is null");
                return;  // BUG FIX: Esce subito per evitare NullReferenceException
            }
            
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

        /// <summary>Restituisce il primo item dello slot senza rimuoverlo. Usato dall'Extractor per leggere i metadata del frutto prima di Consume.</summary>
        public Item PeekFirst(string typeId)
        {
            if (string.IsNullOrEmpty(typeId) || !_slots.TryGetValue(typeId, out var slot) || slot.Quantity == 0)
                return null;
            return slot.Items.FirstOrDefault();
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

        /// <summary>Rimuove e restituisce il primo item dello slot indicato (preserva metadata).</summary>
        public bool TryRemoveFirst(string typeId, out Item removedItem)
        {
            removedItem = null;
            if (string.IsNullOrEmpty(typeId) || !_slots.TryGetValue(typeId, out var slot) || slot.Quantity <= 0)
                return false;

            var first = slot.Items.FirstOrDefault();
            if (first == null || !slot.RemoveItem(first))
                return false;

            if (slot.IsEmpty)
                _slots.Remove(typeId);
            removedItem = first;
            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>Rimuove e restituisce la prima spora con lo stage richiesto (preserva metadata).</summary>
        public bool TryRemoveFirstSporeByStage(SporeStage stage, out Item removedItem)
        {
            removedItem = null;
            string sporeTypeId = _Project.Sporae.Core.Items.SporeGeneric;
            if (!_slots.TryGetValue(sporeTypeId, out var slot))
                return false;

            var target = slot.Items.FirstOrDefault(i => i.SporeStageValue == stage);
            if (target == null || !slot.RemoveItem(target))
                return false;

            if (slot.IsEmpty)
                _slots.Remove(sporeTypeId);
            removedItem = target;
            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>Rimuove fino a count spore con lo stadio indicato (es. Matured per Fusione). Restituisce quante sono state rimosse.</summary>
        public int ConsumeSporeByStage(SporeStage stage, int count)
        {
            if (count <= 0)
                return 0;
            int removed = 0;
            for (int i = 0; i < count; i++)
            {
                if (!TryRemoveFirstSporeByStage(stage, out _))
                    break;
                removed++;
            }
            return removed;
        }
        
        public bool IsEmpty => _slots.Count == 0;
        
        /// <summary>
        /// Pulisce completamente l'inventario.
        /// </summary>
        public void Clear()
        {
            _slots.Clear();
            OnInventoryChanged?.Invoke();
        }
    }
}
