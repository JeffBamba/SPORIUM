using System;
using _Project.Sporae.Core;
using Sporae.DevTools;

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
            WaitingForFinish,
        }

        public VisitorState State { get; set; } = VisitorState.Despawned;
        public event Action OnDisappear;
        
        [SerializeField] private Transform _appearPosition;
        [SerializeField] private Transform _disappearPosition;

        [SerializeField] private MissionConfig _missionConfig;
        [SerializeField] private float _speed;

        [SerializeField] private VisitorDialog _visitorDialog;
        [SerializeField] private UIAwardPopup _uiAwardPopup;
        
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
            _uiAwardPopup.OnCollect += HandleCollect;
        }

        private void HandleCollect()
        {
            Disappear(VisitorState.Despawned);
        }

        private void OnDestroy()
        {
            _interactable.OnInteract -= HandleInteract;
            _visitorDialog.OnAccept -= HandleAccept;
            _visitorDialog.OnReject -= HandleReject;
            _uiAwardPopup.OnCollect -= HandleCollect;
        }
        
        private void HandleAccept()
        {
            Disappear(VisitorState.WaitingForComplete); 
        }

        private void HandleReject()
        {
            Disappear(VisitorState.Despawned);
        }
        
        private void HandleInteract()
        {
            if (State is not (VisitorState.WaitingForPlayer or VisitorState.WaitingForFinish))
                return;
            
            _clearNotification?.Invoke();

            switch (State)
            {
                case VisitorState.WaitingForPlayer:
                    _visitorDialog.Show(_missionConfig);
                    break;
                case VisitorState.WaitingForFinish:
                    _uiAwardPopup.Show(_missionConfig);
                    break;
            }
        }
        
        public void Appear(VisitorState stateAfterAppear)
        {
            State = VisitorState.Animating;
            StartCoroutine(MoveRoutine(_appearPosition, () =>
            {
                // Usa nuovo sistema toast per banner se disponibile
                var toastManager = ServiceContainer.Instance?.Get<ToastNotificationManager>(suppressWarning: true);
                if (toastManager != null)
                {
                    toastManager.ShowBanner("Visitor waiting for player!", ToastNotificationType.Info, out _clearNotification);
                }
                else if (_notificationSystem != null)
                {
                    _notificationSystem.ShowBanner("Visitor waiting for player!", Color.magenta, out _clearNotification);
                }
                State = stateAfterAppear;
            }));
        }

        private void Disappear(VisitorState stateAfterDisappear)
        {
            State = VisitorState.Animating;
            StartCoroutine(MoveRoutine(_disappearPosition, () =>
            {
                State = stateAfterDisappear;
                OnDisappear?.Invoke();
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