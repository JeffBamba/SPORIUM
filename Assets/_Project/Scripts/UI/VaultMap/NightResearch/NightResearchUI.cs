using System.Collections.Generic;
using _Project.Sporae.Core;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.NightResearch
{
    public class NightResearchUI : MonoBehaviour
    {
        [SerializeField] private List<NightResearchOption> _options;

        [SerializeField] private Button _skipButton;
        [SerializeField] private Button _confirmButton;
        
        private NightResearchOption _selectedOption;
        private DayCycleSystem _dayCycleSystem;
        
        private void Awake()
        {
            _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
            
            foreach (var item in _options)
                item.OnClick += SelectOption;
            
            _skipButton.onClick.AddListener(HandleSkip);
            _confirmButton.onClick.AddListener(HandleConfirm);
        }
        
        private void OnDestroy()
        {
            foreach (var item in _options)
                item.OnClick -= SelectOption;
        }
        
        private void SelectOption(NightResearchOption option)
        {
            _selectedOption?.Deselect();
            _selectedOption = option;
            _selectedOption.Select();
        }

        private void HandleSkip()
        {
            _dayCycleSystem.EndDay();
            Hide();
        }

        private void HandleConfirm()
        {
            _dayCycleSystem.EndDay();     
            Hide();       
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}