using System.Linq;
using _Project.Sporae.Core;
using Sporae.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sporae.DevTools;

namespace _Project
{
    public class MicroscopeHUDView : MonoBehaviour
    {
        [SerializeField] private RectTransform _arrow;
         
        [SerializeField] private Image _inRangeArc; 
        [SerializeField] private TextMeshProUGUI _precisionBanner;
        
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _resultContinueButton;
        
        [SerializeField] private GameObject _tutorialGroup;
        [SerializeField] private GameObject _minigameGroup;
        [SerializeField] private GameObject _resultGroup;
        
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private TextMeshProUGUI _confirmText;
        
        [SerializeField] private LabMicroscope _labMicroscope;

        [SerializeField] private PlayerClickMover2D _player;
        
        private MicroscopeMinigameController _controller;

        private ActionSystem _actionsService;

        private Inventory _playerInventory;
        private UINotification _notification;
        
        private void Awake()
        {
            // Usa ServiceContainer invece di FindObjectOfType
            var gameManager = ServiceContainer.Instance?.Get<GameManager>();
            if (gameManager != null)
            {
                _actionsService = gameManager.ActionSystem;
                _playerInventory = gameManager.PlayerInventory;
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "GameManager non disponibile via ServiceContainer!");
            }
            
            _notification = ServiceContainer.Instance?.Get<UINotification>();
            
            _controller = GetComponent<MicroscopeMinigameController>();
        }

        private void Start()
        {
            _confirmButton.onClick.AddListener(HandleConfirm);
            _cancelButton.onClick.AddListener(HandleCancel);
            _continueButton.onClick.AddListener(ShowGame);
            _resultContinueButton.onClick.AddListener(Hide);
        }

        private void Update()
        {
            if (
                Input.GetMouseButtonDown(1) || 
                Input.GetKeyDown(KeyCode.Escape)
                )
                HandleCancel();
        }
        
        private void HandleCancel()
        {
            _player.SuspendMovement(false);
            Hide();
        }

        private void HandleConfirm()
        {
            if (_controller.CurrentLevel == 0)
            {
                _confirmText.text = "Confirm";
                _actionsService.SpendAction();
            }
            
            if (!_controller.NextLevel())
                _labMicroscope.ConsumeSpore();
        }

        public void UpdateArrow(float angle)
        {
            if (_arrow) 
                _arrow.localEulerAngles = new Vector3(0, 0, angle); 
        }

        public void UpdateInRangeArc(float targetAngle, float tolerance)
        {
            _inRangeArc.fillAmount = tolerance;
            _inRangeArc.transform.parent.localEulerAngles = new Vector3(0, 0, targetAngle);
        }

        public void SetPrecision(float precision)
        {
            _precisionBanner.text = $"Precision: {precision:F0}%";
        }

        private void ShowGame()
        {
            _confirmText.text = "Confirm (-1 Action)";
            _confirmButton.interactable = _actionsService.ActionsLeft >= 1;
            
            _tutorialGroup.SetActive(false);
            _minigameGroup.SetActive(true);
            _resultGroup.SetActive(false);
            
            gameObject.SetActive(true);
            _controller.StartRun();
        }
        
        public void ShowTutorial()
        {
            _tutorialGroup.SetActive(true);
            _minigameGroup.SetActive(false);
            _resultGroup.SetActive(false);
            
            gameObject.SetActive(true);
        }

        public void ShowResult()
        {
            gameObject.SetActive(true);
            
            _tutorialGroup.SetActive(false);
            _minigameGroup.SetActive(false);
            _resultGroup.SetActive(true);

            var count = _controller.LevelResults.Count;
            var hits = _controller.LevelResults.Count(levelResult => levelResult.Hit);
            var precision = _controller.LevelResults.Average(item => item.Precision);
            var result = hits == 3 ? (
                    precision > 95 ? "Full traits + bonus quality tag" :
                    precision > 80 ? "Full traits" :
                    precision > 60 ? "Partial traits" :
                    "Unknown traits"
                ) :
                hits > 0 ? (
                    precision > 70 ? "Partial traits" : "Unknown traits"
                ) : "Spore becomes Corrupted";
            
            _resultText.text = $"hits: {hits}/{count}\naverage precision: {precision:F0}%\nresult: {result}";

            if (hits == 0)
            {
                _playerInventory.Add(Items.OrganicScrap001, 1);
                _notification.ShowNotification("Spore becomes Corrupted", 2, Color.red);
            }
            else
            {
                _playerInventory.Add(Items.SporeGeneric, 1);
                _notification.ShowNotification("You got spore with traits", 2, Color.green);
            }
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}