using System.Linq;

using _Project.Sporae.Core;
using Sporae.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project
{
    public class LabCatalizzatore : MonoBehaviour
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _closeButton;
        
        [SerializeField] private DragDropUI _dragDropUI;
        [SerializeField] private HUDInventory _inventory;
        
        [SerializeField] private Catalizzatore _catalizzatore;
        [SerializeField] private CatalizzatoreUI _catalizzatoreUI;
        
        [SerializeField] private TextMeshProUGUI _startButtonLabel;
        
        private GameManager _gameManager;
        
        private Inventory _storage;
        private HUDItemContainer _hudItemContainer;
        private ActionSystem _actionSystem;
        
        public void Show()
        {
            _inventory.Show();
            gameObject.SetActive(true);
        }
        
        private void Hide()
        {
            gameObject.SetActive(false);
            _inventory.Hide();
        }
        
        private void Awake()
        {
            _gameManager = FindObjectOfType<GameManager>();
            if (_gameManager == null)
                Debug.LogWarning("There is no GameManager in the scene");

            _actionSystem = _gameManager.ActionSystem;
            _hudItemContainer = GetComponentInChildren<HUDItemContainer>();
            _storage = _catalizzatore.GetInventory();
        }

        private void Start()
        {
            _storage.OnInventoryChanged += UpdateStorage;
            _inventory.OnClose += Hide;
            
            _startButton.onClick.AddListener(HandleConfirm);
            _closeButton.onClick.AddListener(() =>
            {
                _inventory.Hide();
                _dragDropUI.ConfirmOperation();
                Hide();
            });
            
            UpdateStorage();
        }

        private void HandleConfirm()
        {
            _catalizzatoreUI.ShowTutorial();
        }

        public void ConsumeSpore()
        {
            _storage.Consume(Items.SporeGeneric, 1);
        }
        
        private void UpdateStorage()
        {
            _hudItemContainer.DisableAllSlots();
            
            for (var i = 0; i < _storage.UniqueItems; i++)
            {
                var item = _storage.Items.ElementAt(i);
                _hudItemContainer.SetItemData(i, item.TypeId, item.Quantity);
            }
            
            bool hasSpore = _storage.Has(Items.Seed001), 
                 hasActions = _actionSystem.ActionsLeft > 0;
            _startButtonLabel.text = hasSpore && hasActions ? "Start" :
                !hasActions ? "No Actions remaining" : "No sample available";
            
            _startButton.interactable = hasSpore && hasActions;
        }
    }
}