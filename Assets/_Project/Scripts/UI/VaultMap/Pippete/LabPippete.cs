using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using _Project.Sporae.Core;

namespace _Project
{
    public class LabPipette : MonoBehaviour
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _closeButton;
        
        [SerializeField] private DragDropUI _dragDropUI;
        [SerializeField] private HUDInventory _inventory;
        
        [SerializeField] private Pipette _pipette;
        [SerializeField] private int _costAction;

        [SerializeField] private PipetteView _view;
        
        private GameManager _gameManager;
        
        private Inventory _storage;
        private HUDItemContainer _hudItemContainer;

        public void Hide()
        {
            _inventory.Hide();
            gameObject.SetActive(false);
        }
        
        public void Show()
        {
            _inventory.Show();
            gameObject.SetActive(true);
        }
        
        private void Awake()
        {
            _gameManager = FindObjectOfType<GameManager>();
            if (_gameManager == null)
                Debug.LogWarning("There is no GameManager in the scene");
           
            _hudItemContainer = GetComponentInChildren<HUDItemContainer>();
            _storage = _pipette.GetInventory();
        }

        private void Start()
        {
            _inventory.OnClose += Hide;
            _storage.OnInventoryChanged += UpdateStorage;
            
            _startButton.onClick.AddListener(HandleConfirm);
            _closeButton.onClick.AddListener(() =>
            {
                _inventory.Hide();
                _dragDropUI.ConfirmOperation();
                gameObject.SetActive(false);
            });
            
            UpdateStorage();
        }

        private void HandleConfirm()
        {
            if (!_gameManager.TrySpendAction(_costAction))
                return;

            if (!_storage.Consume(Items.SporeGeneric))
                return;
            
            _dragDropUI.ConfirmOperation();
            _view.ShowTutorial();
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