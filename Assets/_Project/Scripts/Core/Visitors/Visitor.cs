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
            Waiting,
        }

        public VisitorState State { get; set; } = VisitorState.Despawned;

        [SerializeField] private Transform _appearPosition;
        [SerializeField] private Transform _disappearPosition;

        [SerializeField] private MissionConfig _missionConfig;
        [SerializeField] private float _speed;

        [SerializeField] private VisitorDialog _visitorDialog;
        
        private Interactable _interactable;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
            _interactable.OnInteract += HandleInteract;
        }

        private void OnDestroy()
        {
            _interactable.OnInteract -= HandleInteract;
        }

        private void HandleInteract()
        {
            if (State == VisitorState.Waiting)
                _visitorDialog.Show(_missionConfig);
        }
        
        public void Appear()
        {
            State = VisitorState.Animating;
            StartCoroutine(MoveRoutine(_appearPosition, () =>
            {
                State = VisitorState.Waiting;
            }));
        }

        public void Disappear()
        {
            State = VisitorState.Animating;
            StartCoroutine(MoveRoutine(_appearPosition, () =>
            {
                State = VisitorState.Despawned;
            }));
        }

        private IEnumerator MoveRoutine(Transform target, System.Action callback)
        {
            var distance = transform.position - target.position;

            while (distance.sqrMagnitude > 1)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    target.position,
                    _speed * Time.deltaTime
                );

                yield return null;
            }
            
            callback?.Invoke();
        }
    }
}