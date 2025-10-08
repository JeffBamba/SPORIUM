using System.Linq;

using _Project.Sporae.Core;

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
        
        private GameManager _gameManager;
        
        private Inventory _storage;
        private HUDItemContainer _hudItemContainer;
        
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
        }
    }
}