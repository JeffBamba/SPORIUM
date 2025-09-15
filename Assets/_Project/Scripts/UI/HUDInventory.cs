using System;
using System.Collections.Generic;
using System.Linq;
using Sporae.Core;

using UnityEngine;
using UnityEngine.UI;

namespace _Project
{
    public class HUDInventory : MonoBehaviour
    {
        [SerializeField] private GameObject _inventoryPage;
        [SerializeField] private List<HUDInventoryItem> _items;

        [SerializeField] private Button _showInventoryButton;
        [SerializeField] private Button _closeInventoryButton;
        
        private GameManager _gameManager;
        private Inventory _inventory;
        
        private void Awake()
        {
            _gameManager = FindObjectOfType<GameManager>();
            _inventory = _gameManager.GetInventory();
        }

        private void Start()
        {
            _showInventoryButton.onClick.AddListener(Show);
            _closeInventoryButton.onClick.AddListener(Close);
            _inventory.OnInventoryChanged += UpdateInventory;
        }

        private void OnDestroy()
        {
            _inventory.OnInventoryChanged -= UpdateInventory;
        }

        private void Close()
        {
            _inventoryPage.SetActive(false);
        }
        
        private void Show()
        {
            _inventoryPage.SetActive(!_inventoryPage.activeSelf);
            UpdateInventory();
        }

        private void UpdateInventory()
        {
            foreach (HUDInventoryItem item in _items)
                item.gameObject.SetActive(false);

            for (int i = 0; i < _inventory.UniqueItems; i++)
            {
                _items[i].gameObject.SetActive(true);
                
                var item = _inventory.Items.ElementAt(i);
                _items[i].SetItem(item.Id, item.Quantity);
            }
        }
    }
}