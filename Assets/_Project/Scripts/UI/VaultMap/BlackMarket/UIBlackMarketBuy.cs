using System.Collections.Generic;
using System.Linq;
using _Project.Sporae.Core;
using Sporae.Core;
using UnityEngine;

namespace _Project.BlackMarket
{
    public class UIBlackMarketBuy : MonoBehaviour
    {
        [SerializeField] private List<ItemConfig> _itemsCatalog;
        
        private HUDItemContainer _hudItemContainer;
        private readonly List<UIBlackMarketBuyItem> _items = new();

        private Inventory _storage;
        private EconomySystem _economySystem;
        
        private void Awake()
        {
            var gameManager = FindObjectOfType<GameManager>();
            _hudItemContainer = GetComponent<HUDItemContainer>();

            _storage = gameManager.PlayerInventory;
            _economySystem = gameManager.EconomySystem;
            
            foreach (var item in _hudItemContainer.Items)
                _items.Add(item.GetComponent<UIBlackMarketBuyItem>());
        }
        
        private void Start()
        {
            foreach (var item in _items)
                item.OnBuy += HandleBuy;
            
            _storage.OnInventoryChanged += UpdateStorage;
            UpdateStorage();
        }

        private void HandleBuy(UIBlackMarketBuyItem item)
        {
            var index = _items.IndexOf(item);
            var selectedItem = _itemsCatalog.ElementAt(index);

            if (_economySystem.Spend(selectedItem.BuyPrice))
                _storage.Add(selectedItem.TypeId, 1);
        }

        private void UpdateStorage()
        {
            _hudItemContainer.DisableAllSlots();

            for (var i = 0; i < _itemsCatalog.Count; i++)
            {
                var itemSlot = _itemsCatalog.ElementAt(i);
                
                _hudItemContainer.SetItemData(i, itemSlot.TypeId, -1);
                _items[i].SetData(itemSlot.BuyPrice);
            }
        }
    }
}