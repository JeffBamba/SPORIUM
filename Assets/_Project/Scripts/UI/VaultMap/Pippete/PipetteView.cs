using System;
using _Project.Sporae.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project
{
    public class PipetteView : MonoBehaviour
    {
        [SerializeField] private UILineRenderer _lineRenderer;
        
        [SerializeField] private TextMeshProUGUI _stabilityLabel;
        [SerializeField] private TextMeshProUGUI _timerLabel;
        [SerializeField] private TextMeshProUGUI _progressLabel;
        
        [SerializeField] private PipetteGame _game;
        
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _closeButton;

        [SerializeField] private GameObject _tutorialGroup;
        [SerializeField] private GameObject _minigameGroup;
        [SerializeField] private GameObject _resultGroup;
        
        [SerializeField] private TextMeshProUGUI _resultLabel;
        
        private Inventory _inventory;

        private void Awake()
        {
            // Usa ServiceContainer invece di FindObjectOfType
            var gameManager = ServiceContainer.Instance?.Get<GameManager>();
            if (gameManager != null)
            {
                _inventory = gameManager.PlayerInventory;
            }
            else
            {
                Debug.LogWarning("[PipetteView] GameManager non disponibile via ServiceContainer!");
            }
        }
        
        private void Start()
        {
            _game.OnComplete += ShowResult;
            
            _continueButton.onClick.AddListener(HandleContinue);
            _closeButton.onClick.AddListener(HandleClose);
        }
        
        private void OnDestroy()
        {
            _game.OnComplete -= ShowResult;
        }
        
        private void HandleClose()
        {
            Hide();
        }
        
        private void HandleContinue()
        {
            ShowMinigame();
        }

        private void Update()
        {
            _stabilityLabel.text = $"stability: {_game.Stability:F1}";
            _timerLabel.text = $"timer: {_game.Timer:F1}";
            _progressLabel.text = $"progress: {_game.Progress * 100:F1}%";
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        private void ShowMinigame()
        {
            gameObject.SetActive(true);
            
            _tutorialGroup.SetActive(false);
            _minigameGroup.SetActive(true);
            _resultGroup.SetActive(false);

            _game.Run();
        }

        private void ShowResult()
        {
            gameObject.SetActive(true);
            
            _tutorialGroup.SetActive(false);
            _minigameGroup.SetActive(false);
            _resultGroup.SetActive(true);

            var seed =
                _game.Stability > 80 ? "Stable Seed" :
                _game.Stability > 60 ? "Unstable Seed" :
                "Scrap";
            
            if (_game.Stability > 60)
                _inventory.Add(Items.Seed001, 1);
            
            _resultLabel.text = $"Stability: {_game.Stability:F1}\nProduct: {seed}";   
        }
        
        public void ShowTutorial()
        {
            gameObject.SetActive(true);
            
            _tutorialGroup.SetActive(true);
            _minigameGroup.SetActive(false);
            _resultGroup.SetActive(false);
        }
    }
}