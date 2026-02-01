using System.Collections.Generic;
using System.Linq;
using _Project.Sporae.Core;
using Sporae.Core;
using UnityEngine;
using Sporae.DevTools;

namespace _Project.BlackMarket
{
    public class UIBlackMarketBuy : MonoBehaviour
    {
        /// <summary>TypeId da usare su un ItemConfig nel catalogo acquisti: sblocca il Modulo Cellule Staminali (Extractor).</summary>
        public const string LabUpgradeStemCellTypeId = "lab-upgrade-stem-cell";

        [SerializeField] private List<ItemConfig> _itemsCatalog;
        
        [SerializeField] private GameObject _catalyst;
        
        private HUDItemContainer _hudItemContainer;
        private readonly List<UIBlackMarketBuyItem> _items = new();
        private List<ItemConfig> _displayCatalog = new();

        private Inventory _storage;
        private EconomySystem _economySystem;
        private GameManager _gameManager;
        
        private void Awake()
        {
            _gameManager = ServiceContainer.Instance?.Get<GameManager>();
            _hudItemContainer = GetComponent<HUDItemContainer>();

            if (_gameManager != null)
            {
                _storage = _gameManager.PlayerInventory;
                _economySystem = _gameManager.EconomySystem;
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "GameManager non disponibile via ServiceContainer!");
            }
            
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
            if (index < 0 || index >= _displayCatalog.Count) return;
            var selectedItem = _displayCatalog[index];

            if (!_economySystem.Spend(selectedItem.BuyPrice))
                return;

            if (selectedItem.TypeId == "black-market")
            {
                _itemsCatalog.Remove(selectedItem);
                _catalyst.gameObject.SetActive(true);
                UpdateStorage();
                return;
            }

            if (selectedItem.TypeId == LabUpgradeStemCellTypeId)
            {
                if (_gameManager != null)
                    _gameManager.UnlockStemCellModule();
                UpdateStorage();
                return;
            }

            _storage.Add(selectedItem.TypeId, 1);
        }

        private void BuildDisplayCatalog()
        {
            _displayCatalog.Clear();
            if (_itemsCatalog != null)
                _displayCatalog.AddRange(_itemsCatalog);
            if (_gameManager != null && !_gameManager.IsStemCellModuleUnlocked
                && !_displayCatalog.Any(c => c.TypeId == LabUpgradeStemCellTypeId)
                && _displayCatalog.Count < _hudItemContainer.Capacity)
            {
                var stemConfig = Resources.Load<ItemConfig>("Items/" + LabUpgradeStemCellTypeId);
                if (stemConfig != null)
                    _displayCatalog.Add(stemConfig);
            }
        }

        private void UpdateStorage()
        {
            BuildDisplayCatalog();
            _hudItemContainer.DisableAllSlots();

            for (var i = 0; i < _displayCatalog.Count; i++)
            {
                var itemSlot = _displayCatalog[i];
                _hudItemContainer.SetItemData(i, itemSlot.TypeId, -1);
                _items[i].SetData(itemSlot.BuyPrice);
            }
        }
    }
}