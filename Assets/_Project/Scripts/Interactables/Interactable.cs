using System;
using UnityEngine;

namespace _Project
{
    public class Interactable : MonoBehaviour
    {
        [Tooltip("Distanza massima per interagire (click o E). Se 0, non si può mai interagire.")]
        [SerializeField] private float _interactDistance = 2f;

        [SerializeField] private Color _normalColor;
        [SerializeField] private Color _highlightColor;
        
        private SpriteRenderer _spriteRenderer;
        private Transform _playerTransform;
        private PlayerInteractAdvice _playerInteractAdvice;
        
        public event Action OnInteract;

        private bool _interacted = false;

        private float EffectiveInteractDistance => _interactDistance > 0f ? _interactDistance : 2f;
        
        public bool PlayerInRange =>
            _playerTransform != null && Vector2.Distance(_playerTransform.position, transform.position) <= EffectiveInteractDistance;
        
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
#if UNITY_EDITOR
                Debug.Log($"[Interactable] {gameObject.name}: click ignorato — puntatore sopra UI (avvicinati e premi E, oppure clicca senza sovrapporre HUD)");
#endif
                return;
            }

            if (!PlayerInRange)
            {
#if UNITY_EDITOR
                Debug.Log($"[Interactable] {gameObject.name}: click ignorato — giocatore fuori portata (distanza max: {EffectiveInteractDistance}). Avvicinati o premi E quando sei a distanza.");
#endif
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