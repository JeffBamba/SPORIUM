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
        }

        private void HandleCancel()
        {
            Hide();
        }

        private void HandleConfirm()
        {
            if (!_actionsService.CanSpendAction()) 
                return;
            
            _labMicroscope.ConsumeSpore();
            _actionsService.SpendAction();
                
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

        public void Show()
        {
            gameObject.SetActive(true);
            _confirmButton.interactable = _actionsService.ActionsLeft >= 1;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}