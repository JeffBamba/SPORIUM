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
        
        private bool _gameInProgress;
        private bool _isWon;

        private GameManager _gameManager;

        public void Show()
        {
            _textLabel.text = _defaultText;
            gameObject.SetActive(true);
        }
        
        private void Awake()
        {
            _gameManager = FindObjectOfType<GameManager>();
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
            if (!_gameManager.TrySpendCry(_costCry))
                return;
                
            _textLabel.text = _defaultText;
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
            
            var targetMinX = _targetBar.position.x - _targetBar.rect.width / 2;
            var targetMaxX = _targetBar.position.x + _targetBar.rect.width / 2;
            var playerMinX = _playerBar.position.x - _playerBar.rect.width / 2;
            var playerMaxX = _playerBar.position.x + _playerBar.rect.width / 2;

            _gameInProgress = false;
            _isWon =
                targetMinX < playerMinX &&
                targetMaxX > playerMaxX;

            UpdateUI();
        }

        private void UpdateUI()
        {
            _textLabel.text = _isWon ? _wonText : _loseText;
        }
    }
}