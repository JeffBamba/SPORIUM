using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project
{
    public class HUDItemContainer : MonoBehaviour
    {
        [SerializeField] private List<HUDInventoryItem> _items;
        
        private HUDInventoryItem _selectedItem;
        
        public int SelectedId => _items.IndexOf(_selectedItem);
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
            for (var i = 0; i < _items.Count; i++)
                DisableItemSlot(i);
        }
        
        public void DisableItemSlot(int id)
        {
            _items[id].gameObject.SetActive(false);
        }
        
        public void SetItemData(int id, string name, int quantity)
        {
            _items[id].gameObject.SetActive(true);
            _items[id].SetItem(name, quantity);
        }
    }
}