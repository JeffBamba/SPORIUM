using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project
{
    public class LabMinigame : MonoBehaviour
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
        
        
        private bool _gameInProgress;
        private bool _isWon;
        private int _lastPlayingDay;

        private TextMeshProUGUI _startButtonLabel;
        private GameManager _gameManager;

        public void Show()
        {
            _textLabel.text = _defaultText;
            gameObject.SetActive(true);
        }
        
        private void Awake()
        {
            _gameManager = FindObjectOfType<GameManager>();
            if (_gameManager == null)
                Debug.LogWarning("There is no GameManager in the scene");
            
            _startButtonLabel = _startButton.GetComponentInChildren<TextMeshProUGUI>();
        }
        
        private void Start()
        {
            _startButton.onClick.AddListener(TryLaunch);
            _closeButton.onClick.AddListener(() =>
            {
                _gameInProgress = false;
                gameObject.SetActive(false);
            });
        }

        private void TryLaunch()
        {
            var wasTryingInThisDay = _lastPlayingDay == _gameManager.CurrentDay;

            if (!_gameManager.TrySpendActionAndCry(_costAction, wasTryingInThisDay ? _costCry : 0))
                return;

            _textLabel.text = _defaultText;
            _lastPlayingDay = _gameManager.CurrentDay;
            _gameInProgress = true;
        }
        
        private void Update()
        {
            _startButton.interactable = !_gameInProgress;
            
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

            UpdateUI();
        }

        private void UpdateUI()
        {
            var wasTryingInThisDay = _lastPlayingDay == _gameManager.CurrentDay;
            _startButtonLabel.text = wasTryingInThisDay ? _anotherAttemptButtonText : _firstAttemptButtonText; 
            
            _textLabel.text = _isWon ? _wonText : _loseText;
        }
    }
}