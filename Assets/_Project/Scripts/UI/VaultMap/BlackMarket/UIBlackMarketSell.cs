using System.Collections.Generic;
using System.Linq;
using _Project.Sporae.Core;
using Sporae.Core;
using UnityEngine;

namespace _Project.BlackMarket
{
    public class UIBlackMarketSell : MonoBehaviour
    {
        private HUDItemContainer _hudItemContainer;
        private readonly List<UIBlackMarketSellItem> _items = new();

        private Inventory _storage;
        private EconomySystem _economySystem;
        
        private void Awake()
        {
            var gameManager = FindObjectOfType<GameManager>();
            
            _hudItemContainer = GetComponent<HUDItemContainer>();

            _storage = gameManager.PlayerInventory;
            _economySystem = gameManager.EconomySystem;
            
            foreach (var item in _hudItemContainer.Items)
                _items.Add(item.GetComponent<UIBlackMarketSellItem>());
        }

        private void Start()
        {
            foreach (var item in _items)
                item.OnSellOne += HandleSellOne;
            
            foreach (var item in _items)
                item.OnSellAll += HandleSellAll;

            _storage.OnInventoryChanged += UpdateStorage;
            UpdateStorage();
        }

        private void HandleSellAll(UIBlackMarketSellItem item)
        {
            var index = _items.IndexOf(item);
            var selectedItem = _storage.Items.ElementAt(index);

            int price = 0;
            if (selectedItem.Items.Count > 0)
                price = selectedItem.Items.ElementAt(0).ItemConfig.SellPrice;
                
            int quantity = selectedItem.Quantity;
            if (_storage.Consume(selectedItem.TypeId, quantity))
                _economySystem.Add(price * quantity);
        }

        private void HandleSellOne(UIBlackMarketSellItem item)
        {
            var index = _items.IndexOf(item);
            var selectedItem = _storage.Items.ElementAt(index);
            
            int price = 0;
            if (selectedItem.Items.Count > 0)
                price = selectedItem.Items.ElementAt(0).ItemConfig.SellPrice;
            
            if (_storage.Consume(selectedItem.TypeId, 1))
                _economySystem.Add(price);
        }
        
        private void UpdateStorage()
        {
            _hudItemContainer.DisableAllSlots();
            
            for (var i = 0; i < _storage.UniqueItems; i++)
            {
                var itemSlot = _storage.Items.ElementAt(i);
                _hudItemContainer.SetItemData(i, itemSlot.TypeId, itemSlot.Quantity);

                if (itemSlot.Items.Count > 0)
                {
                    var item = itemSlot.Items.ElementAt(0);
                    _items[i].SetData(item.ItemConfig.SellPrice, item.ItemConfig.SellPrice * itemSlot.Quantity);
                }
            }
        }
    }
}