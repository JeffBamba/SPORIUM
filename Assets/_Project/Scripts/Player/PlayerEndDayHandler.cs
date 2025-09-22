using System;
using UnityEngine;

namespace _Project
{
    public class PlayerEndDayHandler : MonoBehaviour
    {
        private ElevatorSystem _elevatorSystem;
        private GameManager _gameManager;
        private Vector3 _startPosition;

        private void Awake()
        {
            _gameManager = FindObjectOfType<GameManager>();
            _elevatorSystem = FindObjectOfType<ElevatorSystem>();
            
            _startPosition = transform.position;
        }

        private void Start()
        {
            _gameManager.OnDayChanged += HandleDayChanged;
        }

        private void OnDestroy()
        {
            _gameManager.OnDayChanged -= HandleDayChanged;
        }

        private void HandleDayChanged(int obj)
        {
            _elevatorSystem.SetLevel(0);
            transform.position = _startPosition;
        }
    }
}