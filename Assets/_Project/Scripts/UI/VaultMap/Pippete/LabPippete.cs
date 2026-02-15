using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using _Project.Sporae.Core;
using Sporae.DevTools;

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
            // Usa ServiceContainer invece di FindObjectOfType
            _gameManager = ServiceContainer.Instance?.Get<GameManager>();
            if (_gameManager == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "GameManager non disponibile via ServiceContainer. Tentativo late binding...");
                if (ServiceContainer.Instance != null)
                {
                    ServiceContainer.Instance.OnServiceRegistered += OnGameManagerRegistered;
                }
            }
           
            _hudItemContainer = GetComponentInChildren<HUDItemContainer>();
            _storage = _pipette.GetInventory();
        }
        
        /// <summary>
        /// Late binding per GameManager quando viene registrato
        /// </summary>
        private void OnGameManagerRegistered(object service)
        {
            if (service is GameManager gameManager && _gameManager == null)
            {
                _gameManager = gameManager;
                
                if (ServiceContainer.Instance != null)
                {
                    ServiceContainer.Instance.OnServiceRegistered -= OnGameManagerRegistered;
                }
            }
        }
        
        private void OnDestroy()
        {
            // Cleanup ServiceContainer subscriptions
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered -= OnGameManagerRegistered;
            }
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

            var dayActivityLog = ServiceContainer.Instance.Get<DayActivityLog>(suppressWarning: true);
            if (dayActivityLog != null)
                dayActivityLog.RecordLabAction("Pipette");
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