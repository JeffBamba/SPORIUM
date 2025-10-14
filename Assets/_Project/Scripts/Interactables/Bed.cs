using _Project.Sporae.Core;
using UnityEngine;

namespace _Project
{
    public class Bed : MonoBehaviour
    {
        [SerializeField] private DiaryUI _diaryUI;
        
        private Interactable _interactable;
        private DayCycleSystem _dayCycleSystem;
        
        private void Awake()
        {
            _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
            _interactable = GetComponent<Interactable>();
        }

        private void Start()
        {
            _interactable.OnInteract += HandleInteract;
        }

        private void OnDestroy()
        {
            _interactable.OnInteract -= HandleInteract;
        }
        
        private void HandleInteract()
        {
            if (_dayCycleSystem.CanEndDay())
                _diaryUI.Show();
        }
    }
}