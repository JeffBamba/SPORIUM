using System.Collections;
using System.Linq;
using _Project.Sporae.Core;
using Sporae.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.HUD;
using Sporae.UI.UIToolkit.NotificationsFoundation;

namespace _Project
{
    public class LabMinigameExtractor : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private RectTransform _playerBar;
        [SerializeField] private RectTransform _targetBar;

        [SerializeField] private TextMeshProUGUI _textLabel;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _closeButton;
        
        [SerializeField] private float _playerDuration;

        [SerializeField] private string _defaultText;
        [SerializeField] private string _wonText;
        [SerializeField] private string _loseText;

        [SerializeField] private int _costCry;
        [SerializeField] private int _costAction;

        [SerializeField] private string _firstAttemptButtonText;
        [SerializeField] private string _anotherAttemptButtonText;
        
        [SerializeField] private DragDropUI _dragDropUI;
        [SerializeField] private HUDInventory _inventory;
        [FormerlySerializedAs("_microscope")] [SerializeField] private Extractor _extractor;

        [SerializeField] private GameObject _gameView;

        [SerializeField] private GameObject _minigameGroup;
        [SerializeField] private GameObject _tutorialGroup;

        [SerializeField] private Button _continueButton;
        
        private bool _gameInProgress;
        private bool _isWon;
        private int _lastPlayingDay;

        private TextMeshProUGUI _startButtonLabel;
        private GameManager _gameManager;
        private DayCycleSystem _dayCycleSystem;
        private DiaryStatistics _diaryStatistics;
        
        private Inventory _playerInventory;
        private Inventory _storage;
        private HUDItemContainer _hudItemContainer;
        
        private UINotification _notification;
        private Item _consumedFruitForRun;
        
        public void Show()
        {
            _textLabel.text = _defaultText;
            _inventory.Show();
            gameObject.SetActive(true);
        }

        private void Hide()
        {
            _gameInProgress = false;
            gameObject.SetActive(false);
            _inventory.Hide();
        }
        
        private void Awake()
        {
            _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
            _diaryStatistics = ServiceContainer.Instance.Get<DiaryStatistics>();
            // Usa ServiceContainer invece di FindObjectOfType
            _notification = ServiceContainer.Instance?.Get<UINotification>();
            
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
                _playerInventory = _gameManager.PlayerInventory;
            }
            
            _startButtonLabel = _startButton.GetComponentInChildren<TextMeshProUGUI>();

            _hudItemContainer = GetComponentInChildren<HUDItemContainer>();
            _storage = _extractor.GetInventory();
            _playerInventory = _gameManager.PlayerInventory;
        }
        
        private void Start()
        {
            _storage.OnInventoryChanged += UpdateStorage;
            _inventory.OnClose += Hide;
            
            _continueButton.onClick.AddListener(ShowMinigame);
            _startButton.onClick.AddListener(TryLaunch);
            _closeButton.onClick.AddListener(() =>
            {
                _dragDropUI.ConfirmOperation();
                Hide();
            });

            UpdateStorage();
        }
        
        private void TryLaunch()
        {
            var wasTryingInThisDay = _lastPlayingDay == _dayCycleSystem.CurrentDay;

            if (!HasAnyFruitInStorage())
                return;
            
            if (!_gameManager.TrySpendActionAndCry(_costAction, wasTryingInThisDay ? _costCry : 0))
                return;

            var dayActivityLog = ServiceContainer.Instance.Get<DayActivityLog>(suppressWarning: true);
            if (dayActivityLog != null)
                dayActivityLog.RecordLabAction(new DayActivityLog.LabActivityEntry { LabType = "Extractor", InputDescription = "frutto", SporeOut = 1, Cell001Out = 0, Cell002Out = 1, Cell003Out = 0 });
            _dragDropUI.ConfirmOperation();
            TryConsumeAnyFruit(out _consumedFruitForRun);

            _textLabel.text = _defaultText;

            _lastPlayingDay = _dayCycleSystem.CurrentDay;
            
            ShowTutorial();
        }

        private void ShowTutorial()
        {
            _tutorialGroup.SetActive(true);
            _minigameGroup.SetActive(false);
            
            _gameView.SetActive(true);   
        }
        
        private void ShowMinigame()
        {
            _gameInProgress = true;
            
            _tutorialGroup.SetActive(false);
            _minigameGroup.SetActive(true);
            
            _gameView.SetActive(true);
        }
        
        private void Update()
        {
            UpdateUI();
            
            if (!_gameInProgress)
                return;
            
            MovePlayer();
            HandleInput();
        }

        private void MovePlayer()
        {
            float startX = _panel.anchoredPosition.x - _panel.rect.width / 2 + _playerBar.rect.width / 1.5f;
            float endX = _panel.anchoredPosition.x +_panel.rect.width / 2 - _playerBar.rect.width / 1.5f;
            
            _playerBar.anchoredPosition = new Vector2(
                Mathf.Lerp(startX, endX, Mathf.PingPong(Time.timeSinceLevelLoad, _playerDuration) / _playerDuration),
                _playerBar.anchoredPosition.y
            );
        }

        private void HandleInput()
        {
            if (!Input.GetMouseButtonDown(0))
                return;
            
            var targetMinX = _targetBar.anchoredPosition.x - _targetBar.rect.width / 2;
            var targetMaxX = _targetBar.anchoredPosition.x + _targetBar.rect.width / 2;
            var playerMinX = _playerBar.anchoredPosition.x - _playerBar.rect.width / 2;
            var playerMaxX = _playerBar.anchoredPosition.x + _playerBar.rect.width / 2;

            _gameInProgress = false;
            _isWon =
                targetMinX < playerMinX &&
                targetMaxX > playerMaxX;

            if (_isWon)
            {
                var spore = ItemFabric.CreateSporeRawFromFruit(_consumedFruitForRun);
                if (spore != null)
                    _playerInventory.Add(spore);
                // Usa Foundation se attivo, altrimenti legacy
                var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                if (foundation != null && foundation.Enabled)
                {
                    if (spore != null)
                        foundation.PostAddedToInventory(CollectionPayloadFactory.FromItem(spore, 1, RoomNames.Laboratory));
                    else
                        foundation.PostAddedToInventory(Items.SporeGeneric, "Spora", 1, RoomNames.Laboratory);
                }
                else
                {
                    var toastManager = ServiceContainer.Instance?.Get<ToastNotificationManager>(suppressWarning: true);
                    if (toastManager != null)
                    {
                        toastManager.ShowToast(ToastNotificationType.ItemCollected, "You got a spore!", "SPORE-001");
                    }
                    else if (_notification != null)
                    {
                        _notification.ShowNotification("You got a spore!", 2, Color.green);
                    }
                }
            }

            _consumedFruitForRun = null;
            StartCoroutine(HideRoutine());
            _textLabel.text = _isWon ? _wonText : _loseText;
        }

        private bool HasAnyFruitInStorage()
        {
            foreach (var typeId in Items.AllFruitTypeIds)
            {
                if (_storage.Has(typeId))
                    return true;
            }

            return false;
        }

        private bool TryConsumeAnyFruit(out Item consumedFruit)
        {
            consumedFruit = null;
            foreach (var typeId in Items.AllFruitTypeIds)
            {
                if (_storage.TryRemoveFirst(typeId, out consumedFruit) && consumedFruit != null)
                    return true;
            }

            return false;
        }

        private void UpdateUI()
        {
            _startButton.interactable = !_gameInProgress;

            var wasTryingInThisDay = _lastPlayingDay == _dayCycleSystem.CurrentDay;
            _startButtonLabel.text = wasTryingInThisDay ? _anotherAttemptButtonText : _firstAttemptButtonText; 
        }
        
        /// <summary>
        /// Late binding per GameManager quando viene registrato
        /// </summary>
        private void OnGameManagerRegistered(object service)
        {
            if (service is GameManager gameManager && _gameManager == null)
            {
                _gameManager = gameManager;
                _playerInventory = _gameManager.PlayerInventory;
                
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
        
        private void UpdateStorage()
        {
            _hudItemContainer.DisableAllSlots();
            
            for (var i = 0; i < _storage.UniqueItems; i++)
            {
                var item = _storage.Items.ElementAt(i);
                _hudItemContainer.SetItemData(i, item.TypeId, item.Quantity);
            }
        }

        private IEnumerator HideRoutine()
        {
            yield return new WaitForSeconds(_playerDuration);
            _gameView.SetActive(false);
            Hide();
        }
    }
}