using System;
using _Project.Sporae.Core;

using System.Collections;
using UnityEngine;

namespace _Project
{
    public class Visitor : MonoBehaviour
    {
        public enum VisitorState
        {
            Despawned,
            Animating,
            WaitingForPlayer,
            WaitingForComplete,
        }

        public VisitorState State { get; set; } = VisitorState.Despawned;
        public event Action OnDisappear;
        
        [SerializeField] private Transform _appearPosition;
        [SerializeField] private Transform _disappearPosition;

        [SerializeField] private MissionConfig _missionConfig;
        [SerializeField] private float _speed;

        [SerializeField] private VisitorDialog _visitorDialog;
        
        private Interactable _interactable;
        private UINotification _notificationSystem;

        private Action _clearNotification;

        private void Awake()
        {
            _notificationSystem = ServiceContainer.Instance.Get<UINotification>();
            
            _interactable = GetComponent<Interactable>();
            _interactable.OnInteract += HandleInteract;
            _visitorDialog.OnAccept += HandleAccept;
            _visitorDialog.OnReject += HandleReject;
        }

        private void OnDestroy()
        {
            _interactable.OnInteract -= HandleInteract;
            _visitorDialog.OnAccept -= HandleAccept;
            _visitorDialog.OnReject -= HandleReject;
        }

        private void HandleAccept()
        {
            State = VisitorState.WaitingForComplete;
        }

        private void HandleReject()
        {
            Disappear();
        }
        
        private void HandleInteract()
        {
            if (State != VisitorState.WaitingForPlayer)
                return;
            
            _clearNotification?.Invoke();
            _visitorDialog.Show(_missionConfig);
        }
        
        public void Appear()
        {
            State = VisitorState.Animating;
            StartCoroutine(MoveRoutine(_appearPosition, () =>
            {
                _notificationSystem.ShowBanner("Visitor waiting for player!", Color.magenta, out _clearNotification);
                State = VisitorState.WaitingForPlayer;
            }));
        }

        public void Disappear()
        {
            State = VisitorState.Animating;
            StartCoroutine(MoveRoutine(_disappearPosition, () =>
            {
                OnDisappear?.Invoke();
                State = VisitorState.Despawned;
            }));
        }

        private IEnumerator MoveRoutine(Transform target, Action callback)
        {
            var distance = transform.position - target.position;

            while (distance.sqrMagnitude > 0.2f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    target.position,
                    _speed * Time.deltaTime
                );
                
                distance = transform.position - target.position;
                yield return null;
            }
            
            callback?.Invoke();
        }
    }
}