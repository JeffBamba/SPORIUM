using System;
using _Project.Sporae.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project
{
    public class UIAwardPopup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _awardLabel;
        [SerializeField] private Button _collectButton;

        private MissionConfig _config;
        private MissionConfig.Reward _reward;
        private GameManager _gameManager;
        private MissionManager _missionManager;

        public event Action OnCollect;
        
        private void Awake()
        {
            _missionManager = ServiceContainer.Instance.Get<MissionManager>();
            _gameManager = FindObjectOfType<GameManager>();
            _collectButton.onClick.AddListener(HandleCollect);
        }

        private void HandleCollect()
        {
            _missionManager.Remove(_config);
            
            _gameManager.EconomySystem.Add(_reward.CryReward);
            foreach (var slot in _reward.Rewards)
                _gameManager.PlayerInventory.Add(slot.Item.TypeId, slot.Quantity);
            
            OnCollect?.Invoke();
            Hide();
        }

        public void Show(MissionConfig config)
        {
            _reward = config.QuickPathReward;
            
            var text = $"+{_reward.CryReward} /";
            foreach (var slot in _reward.Rewards)
                text += $"+{slot.Quantity} {slot.Item.TypeId} /";
            text.Remove(text.Length - 1);
            
            gameObject.SetActive(true);
            _config = config;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}