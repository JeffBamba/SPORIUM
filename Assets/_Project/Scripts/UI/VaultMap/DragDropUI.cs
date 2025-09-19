using System;
using System.Collections.Generic;
using System.Linq;
using Sporae.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project
{
    public class DragDropUI : MonoBehaviour
    {
        [SerializeField] private Button _toLeftButton;
        [SerializeField] private Button _toRightButton;

        [SerializeField] private Storage _storage;
        
        [SerializeField] private List<string> _allowedItemsIds;
        [SerializeField] private int _containerCapacity; 
        
        [SerializeField] private HUDItemContainer _hudPlayerContainer;
        [SerializeField] private HUDItemContainer _hudStorageContainer;
        
        [SerializeField] private Button _confirmOperation;
        
        private Inventory _playerInventory;
        private Inventory _storageInventory;
        
        private TextMeshProUGUI _confirmOperationLabel;
        private string _confirmLabelPattern;
        
        private readonly Dictionary<string, int> _movedToLeft = new();
        private readonly Dictionary<string, int> _movedToRight = new();

        
        private void Awake()
        {
            _storageInventory = _storage.GetInventory();
                
            var gameManager = FindObjectOfType<GameManager>();
            _playerInventory = gameManager.GetInventory();

            if (!_confirmOperation)
                return;
            
            _confirmOperationLabel = _confirmOperation.GetComponentInChildren<TextMeshProUGUI>();
            _confirmOperation.onClick.AddListener(ConfirmOperation);
            _confirmLabelPattern = _confirmOperationLabel.text;
        }

        public void ConfirmOperation()
        {
            _movedToLeft.Clear();
            _movedToRight.Clear();
        }

        private void Start()
        {
            _toLeftButton.onClick.AddListener(HandleLeft);
            _toRightButton.onClick.AddListener(HandleRight);
        }

        private void Update()
        {
            if (!_confirmOperationLabel) 
                return;
            
            var totalCryCost = _movedToRight.Sum(item => item.Value);
            _confirmOperationLabel.text = _confirmLabelPattern.Replace("{}", totalCryCost.ToString());
        }

        private void OnEnable()
        {
            _movedToLeft.Clear();
            _movedToRight.Clear();
        }

        private void OnDisable()
        {
            foreach (var item in _movedToLeft.ToList())
                ExecuteCommand(item.Key, item.Value, false);
            
            foreach (var item in _movedToRight.ToList())
                ExecuteCommand(item.Key, item.Value, true);
        }

        private void HandleRight()
        {
            var id = _hudPlayerContainer.SelectedId;
            if (id < 0 || id >= _playerInventory.Items.Count)
                return;
            
            var item = _playerInventory.Items.ElementAt(id);
            if (!_allowedItemsIds.Contains(item.Id) && _allowedItemsIds.Count > 0)
                return;
                
            ExecuteCommand(item.Id, 1, false);
        }

        private void HandleLeft()
        {
            var id = _hudStorageContainer.SelectedId;
            if (id < 0 || id >= _storageInventory.Items.Count)
                return;
            
            var item = _storageInventory.Items.ElementAt(id);
            ExecuteCommand(item.Id, 1, true);
        }

        private void ExecuteCommand(string itemId, int amount, bool isMoveToLeft)
        {
            var inventoryFrom = isMoveToLeft ? _storageInventory : _playerInventory;
            var inventoryTo = isMoveToLeft ? _playerInventory : _storageInventory;

            if (!isMoveToLeft && inventoryTo.UniqueItems >= _containerCapacity)
                return;
            
            if (!inventoryFrom.Consume(itemId, amount))
            {
                Debug.LogError($"Cannot consume item from inventory: {itemId}");
                return;
            }

            inventoryTo.Add(itemId, amount);
            AddOrUpdateItem(itemId, amount, isMoveToLeft);
        }

        private void AddOrUpdateItem(string itemId, int amount, bool isMoveToLeft)
        {
            var collectionTo = isMoveToLeft ? _movedToLeft : _movedToRight;
            var collectionFrom = isMoveToLeft ? _movedToRight : _movedToLeft;
            
            if (collectionFrom.ContainsKey(itemId))
                collectionFrom[itemId] -= amount;
            else if (!collectionTo.TryAdd(itemId, amount))
                collectionTo[itemId] += amount;
        }
    }
}