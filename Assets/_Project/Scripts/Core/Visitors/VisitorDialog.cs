using _Project.Sporae.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project
{
    public class VisitorDialog : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleLabel;
        [SerializeField] private TextMeshProUGUI _descriptionLabel;
        [SerializeField] private TextMeshProUGUI _goalsLabel;
        
        [SerializeField] private Button _acceptButton;
        [SerializeField] private Button _rejectButton;

        private MissionConfig _missionConfig;
        private MissionManager _missionManager;
        
        private void Awake()
        {
            _missionManager = ServiceContainer.Instance.Get<MissionManager>();
            
            _acceptButton.onClick.AddListener(HandleAccept);
            _rejectButton.onClick.AddListener(HandleReject);
        }

        private void HandleAccept()
        {
            _missionManager.Append(_missionConfig);
            Hide();
        }

        private void HandleReject()
        {
            Hide();
        }

        public void Show(MissionConfig missionConfig)
        {
            gameObject.SetActive(true);
            _missionConfig = missionConfig;
            _titleLabel.text = missionConfig.Title;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}