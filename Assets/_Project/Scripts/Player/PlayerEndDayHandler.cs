using _Project.Sporae.Core;
using UnityEngine;

namespace _Project
{
    public class PlayerEndDayHandler : MonoBehaviour
    {
        private DayCycleSystem _dayCycleSystem;
        private ElevatorSystem _elevatorSystem;
        private GameManager _gameManager;
        private Vector3 _startPosition;

        private void Awake()
        {
            _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
            _elevatorSystem = FindObjectOfType<ElevatorSystem>();
            
            _startPosition = transform.position;
        }

        private void Start()
        {
            _dayCycleSystem.OnDayChanged += HandleDayChanged;
        }

        private void OnDestroy()
        {
            _dayCycleSystem.OnDayChanged -= HandleDayChanged;
        }

        private void HandleDayChanged(int obj)
        {
            _elevatorSystem.SetLevel(0);
            transform.position = _startPosition;
        }
    }
}