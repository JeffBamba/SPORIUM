using System;
using System.Collections.Generic;
using System.Linq;
using Sporae.Core;

using UnityEngine;
using UnityEngine.UI;

namespace _Project
{
    [RequireComponent(typeof(HUDItemContainer))]
    public class HUDInventory : MonoBehaviour
    {
        [SerializeField] private GameObject _inventoryPage;
        
        [SerializeField] private Button _showInventoryButton;
        [SerializeField] private Button _closeInventoryButton;
        
        private GameManager _gameManager;
        private Inventory _inventory;
        private HUDItemContainer _hudItemContainer;
        
        public event Action OnClose;
        
        private void Awake()
        {
            _hudItemContainer = GetComponent<HUDItemContainer>();
            _gameManager = FindObjectOfType<GameManager>();
            _inventory = _gameManager.GetInventory();
        }

        private void Start()
        {
            _showInventoryButton.onClick.AddListener(Toggle);
            _closeInventoryButton.onClick.AddListener(Close);
            
            _inventory.OnInventoryChanged += UpdateInventory;
        }

        private void OnDestroy()
        {
            _inventory.OnInventoryChanged -= UpdateInventory;
        }

        private void Toggle()
        {
            if (_inventoryPage.activeSelf)
                Close();
            else 
                Show();
        }
        
        private void Close()
        {
            OnClose?.Invoke();
            _inventoryPage.SetActive(false);
        }
        
        public void Show()
        {
            _inventoryPage.SetActive(true);
            UpdateInventory();
        } 

        private void UpdateInventory()
        {   
            _hudItemContainer.DisableAllSlots();
            
            for (var i = 0; i < _inventory.UniqueItems; i++)
            {
                var item = _inventory.Items.ElementAt(i);
               _hudItemContainer.SetItemData(i, item.Id, item.Quantity);
            }
        }
    }
}