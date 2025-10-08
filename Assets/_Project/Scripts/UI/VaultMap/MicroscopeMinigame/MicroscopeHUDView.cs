using _Project.Sporae.Core;
using Sporae.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project
{
    public class MicroscopeHUDView : MonoBehaviour
    {
        [SerializeField] private RectTransform _arrow;
        
        [SerializeField] private RectTransform _inRangeArc; 
        [SerializeField] private TextMeshProUGUI _precisionBanner;
        
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _continueButton;
        
        [SerializeField] private GameObject _tutorialGroup;
        [SerializeField] private GameObject _minigameGroup;
        
        [SerializeField] private LabMicroscope _labMicroscope;
        
        private MicroscopeMinigameController _controller;

        private ActionSystem _actionsService;
        
        private void Awake()
        {
            var gameManager = FindObjectOfType<GameManager>();
            _actionsService = gameManager.ActionSystem;
            
            _controller = GetComponent<MicroscopeMinigameController>();
        }

        private void Start()
        {
            _confirmButton.onClick.AddListener(HandleConfirm);
            _cancelButton.onClick.AddListener(HandleCancel);
            _continueButton.onClick.AddListener(ShowGame);
        }

        private void HandleCancel()
        {
            Hide();
        }

        private void HandleConfirm()
        {
            _labMicroscope.ConsumeSpore();
            _controller.CancelRun();
        }

        public void UpdateArrow(float angle)
        {
            if (_arrow) 
                _arrow.localEulerAngles = new Vector3(0, 0, angle); 
        }

        public void UpdateInRangeArc(float targetAngle)
        {
            _inRangeArc.transform.localEulerAngles = new Vector3(0, 0, targetAngle);
        }

        public void SetPrecision(float precision)
        {
            _precisionBanner.text = $"Precision: {precision:F0}%";
        }

        private void ShowGame()
        {
            _confirmButton.interactable = _actionsService.ActionsLeft >= 1;
            
            _tutorialGroup.SetActive(false);
            _minigameGroup.SetActive(true);
            
            gameObject.SetActive(true);
            _controller.StartRun();
        }
        
        public void ShowTutorial()
        {
            _tutorialGroup.SetActive(true);
            _minigameGroup.SetActive(false);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}