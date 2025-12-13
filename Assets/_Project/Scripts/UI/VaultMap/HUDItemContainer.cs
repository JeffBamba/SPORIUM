using System;
using System.Collections.Generic;
using UnityEngine;
using Sporae.DevTools;

namespace _Project
{
    public class HUDItemContainer : MonoBehaviour
    {
        [SerializeField] private List<HUDInventoryItem> _items;
        
        private HUDInventoryItem _selectedItem;
        
        public List<HUDInventoryItem> Items => _items;
        public int SelectedId => _items.IndexOf(_selectedItem);
        public string SelectedItemName => 
            SelectedId < 0 || SelectedId >= Items.Count ? "" : _items[SelectedId].ItemName;
        public int Capacity => _items.Count;
        
        private void Start()
        {
            foreach (var item in _items)
                item.OnClick += SelectHandler;
        }

        private void OnDestroy()
        {
            foreach (var item in _items)
                item.OnClick -= SelectHandler;
        }
        
        private void SelectHandler(HUDInventoryItem item)
        {
            _selectedItem?.Deselect();
            _selectedItem = item;
            _selectedItem.Select();
        }

        public void DisableAllSlots()
        {
            if (_items == null) return;
            
            for (var i = 0; i < _items.Count; i++)
            {
                if (_items[i] != null)
                    DisableItemSlot(i);
            }
        }
        
        public void DisableItemSlot(int id)
        {
            if (_items == null || id < 0 || id >= _items.Count)
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"⚠️ Tentativo di disabilitare slot invalido: {id} (capacità: {_items?.Count ?? 0})");
                return;
            }
            
            if (_items[id] != null)
            {
                _items[id].gameObject.SetActive(false);
            }
        }
        
        public void SetItemData(int id, string itemName, int quantity)
        {
            if (_items == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "⚠️ Lista _items è null! Assicurati che gli item siano assegnati nell'Inspector.");
                return;
            }
            
            if (id < 0 || id >= _items.Count)
            {
                SporiumLogger.LogError(LogCategory.UI, $"⚠️ Tentativo di impostare item con indice invalido: {id} (capacità: {_items.Count}). Item: {itemName}");
                return;
            }
            
            if (_items[id] == null)
            {
                SporiumLogger.LogError(LogCategory.UI, $"⚠️ Item slot {id} è null! Assicurati che tutti gli slot siano assegnati nell'Inspector.");
                return;
            }
            
            _items[id].gameObject.SetActive(true);
            _items[id].SetItem(itemName, quantity);
            
            // Assicurati che l'item sia visibile
            CanvasGroup canvasGroup = _items[id].GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }
    }
}