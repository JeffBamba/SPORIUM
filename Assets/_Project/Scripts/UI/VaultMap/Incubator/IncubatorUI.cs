using _Project.Sporae.Core;
using UnityEngine;
using UnityEngine.UI;

namespace _Project
{
    public class IncubatorUI : MonoBehaviour
    {
        [SerializeField] private GameObject _eveningGroup;
        [SerializeField] private GameObject _morningGroup;

        [SerializeField] private Button _continueButton;
        
        private DayCycleSystem _dayCycleSystem;
        private bool _processLaunched;
        
        public void ShowEvening()
        {
            _processLaunched = true;
            
            _eveningGroup.SetActive(true);
            _morningGroup.SetActive(false);
            
            gameObject.SetActive(true);
        }
        
        private void Hide()
        {
            gameObject.SetActive(false);
        }
        
        private void ShowMorning()
        {
            _processLaunched = false;
            
            _eveningGroup.SetActive(false);
            _morningGroup.SetActive(true);
            
            gameObject.SetActive(true);
        }
        
        private void Awake()
        {
            _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
        }

        private void Start()
        {
            _dayCycleSystem.OnDayChanged += HandleDayChanged;
            _continueButton.onClick.AddListener(HandleContinue);
        }

        private void OnDestroy()
        {
            _dayCycleSystem.OnDayChanged -= HandleDayChanged;
        }

        private void HandleContinue()
        {
            Hide();
        }
        
        private void HandleDayChanged(int day)
        {
            if (_processLaunched)
                ShowMorning();
        }
    }
}