using _Project.Sporae.Core;
using UnityEngine;

namespace _Project
{
    public class VisitorEventController : MonoBehaviour
    {
        [SerializeField] private Visitor _visitor;
        
        [SerializeField] private Vector2Int _interval;
        [SerializeField] private int _firstAppear;
        
        private DayCycleSystem _dayCycleSystem;
        private int _nextDayAppear;
        
        private void Awake()
        {
            _nextDayAppear = _firstAppear;
            
            _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
            _dayCycleSystem.OnDayChanged += HandleDayChanged;
            _visitor.OnDisappear += HandleDisappear;
        }

        private void HandleDisappear()
        {
            _nextDayAppear += Random.Range(_interval.x, _interval.y);
        }

        private void OnDestroy()
        {
            _dayCycleSystem.OnDayChanged -= HandleDayChanged;
        }
        
        private void HandleDayChanged(int day)
        {
            if (day != _nextDayAppear)
                return;
            
            Appear();
        }

        private void Appear()
        {
            _visitor.Appear();   
        }
    }
}