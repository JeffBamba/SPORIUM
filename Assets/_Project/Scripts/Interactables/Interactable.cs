using System;
using UnityEngine;

namespace _Project
{
    public class Interactable : MonoBehaviour
    {
        [SerializeField] private float _interactDistance;

        [SerializeField] private Color _normalColor;
        [SerializeField] private Color _highlightColor;
        
        private SpriteRenderer _spriteRenderer;
        private Transform _playerTransform;
        private PlayerInteractAdvice _playerInteractAdvice;
        
        public event Action OnInteract;

        private bool _interacted = false;

        public bool PlayerInRange =>
            Vector2.Distance(_playerTransform.position, transform.position) <= _interactDistance;
        
        private void Awake()
        {
            _playerInteractAdvice = FindObjectOfType<PlayerInteractAdvice>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _playerTransform = player.transform;
        }

        private void Update()
        {
            if (PlayerInRange)
            {
                if (!_interacted)
                    _playerInteractAdvice.AddInteractable();

                if (Input.GetKeyDown(KeyCode.E))
                {
                    _interacted = true;
                    OnInteract?.Invoke();
                }
            }
            else
                _interacted = false;
        }
        
        public void OnMouseDown()
        {
            bool isOverUI = UIBlocker.IsPointerOverUI();
            
            if (isOverUI)
            {
                return;
            }

            if (!PlayerInRange)
            {
                return;
            }
            
            _interacted = true;
            OnInteract?.Invoke();
        }
        
        private void OnMouseEnter()
        {
            _spriteRenderer.color = _highlightColor;
        }
    
        private void OnMouseExit()
        {
            Deselect();
        }

        public void Deselect()
        {
            
            _spriteRenderer.color = _normalColor;
        }
        
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _interactDistance);
        }
#endif
    }
}