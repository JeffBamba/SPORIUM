using System.Linq;

using _Project.Sporae.Core;
using Sporae.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sporae.DevTools;

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
        [SerializeField] private MicroscopeHUDView _hudView;
        [SerializeField] private int _costAction;

        [SerializeField] private TextMeshProUGUI _startButtonLabel;
        
        private GameManager _gameManager;
        
        private Inventory _storage;
        private HUDItemContainer _hudItemContainer;
        private ActionSystem _actionSystem;

        public void Hide()
        {
            _inventory.Hide();
            gameObject.SetActive(false);
        }
        
        public void Show()
        {
            _inventory.Show();
            gameObject.SetActive(true);
            
            UpdateStorage();
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
            else
            {
                _actionSystem = _gameManager.ActionSystem;
            }

            _hudItemContainer = GetComponentInChildren<HUDItemContainer>();
            _storage = _microscope.GetInventory();
        }
        
        /// <summary>
        /// Late binding per GameManager quando viene registrato
        /// </summary>
        private void OnGameManagerRegistered(object service)
        {
            if (service is GameManager gameManager && _gameManager == null)
            {
                _gameManager = gameManager;
                _actionSystem = _gameManager.ActionSystem;
                
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

            var dayActivityLog = ServiceContainer.Instance.Get<DayActivityLog>(suppressWarning: true);
            if (dayActivityLog != null)
                dayActivityLog.RecordLabAction("Microscope");
            _dragDropUI.ConfirmOperation();
            _hudView.ShowTutorial();
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

            bool hasSpore = _storage.Has(Items.SporeGeneric), 
                 hasActions = _actionSystem.ActionsLeft > 0;
            _startButtonLabel.text = hasSpore && hasActions ? "Avvia" :
                                     !hasActions ? "Nessuna azione rimasta" : "Nessun campione disponibile" ;
            
            _startButton.interactable = hasSpore && hasActions;
        }
    }
}