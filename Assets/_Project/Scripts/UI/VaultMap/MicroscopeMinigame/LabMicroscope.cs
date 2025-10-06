using System.Linq;

using _Project.Sporae.Core;

using UnityEngine;
using UnityEngine.UI;

namespace _Project
{
    public class LabMicroscope : MonoBehaviour
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _closeButton;
        
        [SerializeField] private DragDropUI _dragDropUI;
        [SerializeField] private HUDInventory _inventory;
        
        [SerializeField] private Microscope _microscope;
        [SerializeField] private MicroscopeMinigameController _microscopeMinigameController;
        
        private GameManager _gameManager;
        
        private Inventory _storage;
        private HUDItemContainer _hudItemContainer;
        
        private void Awake()
        {
            _gameManager = FindObjectOfType<GameManager>();
            if (_gameManager == null)
                Debug.LogWarning("There is no GameManager in the scene");
           
            _hudItemContainer = GetComponentInChildren<HUDItemContainer>();
            _storage = _microscope.GetInventory();
        }

        private void Start()
        {
            _storage.OnInventoryChanged += UpdateStorage;
            _startButton.onClick.AddListener(HandleConfirm);
            _closeButton.onClick.AddListener(() =>
            {
                _dragDropUI.ConfirmOperation();
                gameObject.SetActive(false);
            });
            
            UpdateStorage();
        }

        private void HandleConfirm()
        {
            _dragDropUI.ConfirmOperation();
            _microscopeMinigameController.StartRun();
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

        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        public void Show()
        {
            _inventory.Show();
            gameObject.SetActive(true);
        }
    }
}