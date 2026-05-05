using System.Collections.Generic;
using System.Linq;
using _Project.Sporae.Core;
using Sporae.Core;
using UnityEngine;
using Sporae.Core;
using Sporae.DevTools;

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
            // Usa ServiceContainer invece di FindObjectOfType
            var gameManager = ServiceContainer.Instance?.Get<GameManager>();
            
            _hudItemContainer = GetComponent<HUDItemContainer>();

            if (gameManager != null)
            {
                _storage = gameManager.PlayerInventory;
                _economySystem = gameManager.EconomySystem;
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "GameManager non disponibile via ServiceContainer!");
            }
            
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
            if (index < 0 || index >= _storage.UniqueItems)
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"⚠️ Indice invalido per HandleSellAll: {index} (UniqueItems: {_storage.UniqueItems})");
                return;
            }
            
            var selectedItem = _storage.Items.ElementAt(index);

            int price = 0;
            if (selectedItem.Items.Count > 0)
                price = selectedItem.Items.ElementAt(0).ItemConfig.SellPrice;
                
            int quantity = selectedItem.Quantity;
            if (_storage.Consume(selectedItem.TypeId, quantity))
                _economySystem.Add(price * quantity, CryIncomeLedgerCategory.BlackMarketSell);
        }

        private void HandleSellOne(UIBlackMarketSellItem item)
        {
            var index = _items.IndexOf(item);
            if (index < 0 || index >= _storage.UniqueItems)
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"⚠️ Indice invalido per HandleSellOne: {index} (UniqueItems: {_storage.UniqueItems})");
                return;
            }
            
            var selectedItem = _storage.Items.ElementAt(index);
            
            int price = 0;
            if (selectedItem.Items.Count > 0)
                price = selectedItem.Items.ElementAt(0).ItemConfig.SellPrice;
            
            if (_storage.Consume(selectedItem.TypeId, 1))
                _economySystem.Add(price, CryIncomeLedgerCategory.BlackMarketSell);
        }
        
        private void UpdateStorage()
        {
            if (_storage == null || _hudItemContainer == null)
                return;
            
            _hudItemContainer.DisableAllSlots();
            
            // BUG FIX: Limita il loop al numero di slot disponibili nel container
            // per evitare IndexOutOfRangeException quando ci sono più item unici che slot
            int maxItems = Mathf.Min(_storage.UniqueItems, _hudItemContainer.Capacity, _items.Count);
            
            for (var i = 0; i < maxItems; i++)
            {
                var itemSlot = _storage.Items.ElementAt(i);
                _hudItemContainer.SetItemData(i, itemSlot.TypeId, itemSlot.Quantity);

                if (itemSlot.Items.Count > 0 && i < _items.Count)
                {
                    var item = itemSlot.Items.ElementAt(0);
                    _items[i].SetData(item.ItemConfig.SellPrice, item.ItemConfig.SellPrice * itemSlot.Quantity);
                }
            }
        }
    }
}