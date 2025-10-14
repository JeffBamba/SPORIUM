using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Sporae.Core;
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
        
        private GameManager _gameManager;
        private Inventory _inventory;
        private HUDItemContainer _hudItemContainer;
        
        public event Action OnClose;
        
        private void Awake()
        {
            _hudItemContainer = GetComponent<HUDItemContainer>();
            _gameManager = FindObjectOfType<GameManager>();
            _inventory = _gameManager.PlayerInventory;
        }

        private void Start()
        {
            _showInventoryButton.onClick.AddListener(Toggle);
            
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
            Hide();
        }

        public void Hide()
        {
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

            int index = 0;
            for (var i = 0; i < _inventory.UniqueItems; i++)
            {
                var slot = _inventory.Items.ElementAt(i);

                if (slot.Items.Count > 0 && slot.Items.ElementAt(0).ItemConfig.CanStack)
                    _hudItemContainer.SetItemData(index++, slot.TypeId, slot.Quantity);
                else 
                    foreach (var item in slot.Items)
                        _hudItemContainer.SetItemData(index++, item.TypeId, -1);
            }
        }
    }
}