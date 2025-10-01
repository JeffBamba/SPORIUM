using System;
using System.Collections;
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
        
        public event Action OnAccept;
        public event Action OnReject;
        
        private void Awake()
        {
            _missionManager = ServiceContainer.Instance.Get<MissionManager>();
            
            _acceptButton.onClick.AddListener(HandleAccept);
            _rejectButton.onClick.AddListener(HandleReject);
        }

        private void HandleAccept()
        {
            OnAccept?.Invoke();
            _missionManager.Append(_missionConfig);
            Hide();
        }

        private void HandleReject()
        {
            OnReject?.Invoke();
            Hide();
        }

        public void Show(MissionConfig missionConfig)
        {
            gameObject.SetActive(true);
            _missionConfig = missionConfig;
            _titleLabel.text = missionConfig.Title;

            StartCoroutine(TypewriteRoutine(missionConfig.Description));
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private IEnumerator TypewriteRoutine(string text)
        {
            string currentText = "";
            int index = 0;
            
            while (currentText != text && index < text.Length)
            {
                currentText += text[index];
                index++;
                
                _descriptionLabel.text = currentText;

                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}