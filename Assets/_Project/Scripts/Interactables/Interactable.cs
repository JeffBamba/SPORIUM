using System;
using UnityEngine;

namespace _Project
{
    public class Interactable : MonoBehaviour
    {
        [Tooltip("Distanza massima per interagire (click o E). Se 0, non si può mai interagire.")]
        [SerializeField] private float _interactDistance = 2f;

        [Tooltip("Raggio (in unità mondo) entro cui un click conta come \"sul\" questo Interactable. Aumenta se il click funziona solo in un punto.")]
        [SerializeField] private float _clickRadius = 0.35f;

        [Tooltip("Se true, il prompt [E] resta attivo e si può riaprire l'interazione senza uscire dalla zona (es. Armadio).")]
        [SerializeField] private bool _repeatInteractionWhileInRange;

        [SerializeField] private Color _normalColor;
        [SerializeField] private Color _highlightColor;
        
        private SpriteRenderer _spriteRenderer;
        private Transform _playerTransform;
        private Rigidbody2D _playerRigidbody;
        private PlayerInteractAdvice _playerInteractAdvice;
        private Collider2D _collider2D;
        
        public event Action OnInteract;

        private bool _interacted = false;

        private float EffectiveInteractDistance => _interactDistance > 0f ? _interactDistance : 2f;
        
        public bool PlayerInRange
        {
            get
            {
                if (_playerTransform == null)
                    return false;
                Vector2 pp = _playerRigidbody != null ? _playerRigidbody.position : (Vector2)_playerTransform.position;
                return Vector2.Distance(pp, transform.position) <= EffectiveInteractDistance;
            }
        }

        public void SetRepeatInteractionWhileInRange(bool value) => _repeatInteractionWhileInRange = value;

        public void SetInteractDistance(float meters) => _interactDistance = Mathf.Max(0.25f, meters);
        
        private void Awake()
        {
            _playerInteractAdvice = FindObjectOfType<PlayerInteractAdvice>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider2D = GetComponent<Collider2D>();
            if (_collider2D == null)
                _collider2D = GetComponentInChildren<Collider2D>();
            
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
                _playerRigidbody = player.GetComponent<Rigidbody2D>();
            }

            if (GetComponent<WardrobeStation>() != null)
                _repeatInteractionWhileInRange = true;
        }

        private void Update()
        {
            if (PlayerInRange)
            {
                if (_playerInteractAdvice != null && (!_interacted || _repeatInteractionWhileInRange))
                    _playerInteractAdvice.AddInteractable();

                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (!_repeatInteractionWhileInRange)
                        _interacted = true;
                    OnInteract?.Invoke();
                    return;
                }

                // Click come alternativa a E: se il click è su questo Interactable e non sulla UI, interagisci
                if (Input.GetMouseButtonDown(0) && !UIBlocker.IsPointerOverUI())
                {
                    Camera cam = Camera.main != null ? Camera.main : UnityEngine.Object.FindObjectOfType<Camera>();
                    Vector2 worldPoint = cam != null
                        ? (Vector2)cam.ScreenToWorldPoint(Input.mousePosition)
                        : (Vector2)transform.position;
                    float r = _clickRadius > 0f ? _clickRadius : 0.35f;
                    bool hit = false;
                    if (_collider2D != null)
                    {
                        hit = _collider2D.OverlapPoint(worldPoint);
                        if (!hit)
                        {
                            Collider2D[] near = Physics2D.OverlapCircleAll(worldPoint, r);
                            for (int i = 0; i < near.Length; i++)
                                if (near[i] == _collider2D) { hit = true; break; }
                        }
                    }
                    // Se non c'è collider (es. Incubator, FoodSynthMachine con solo UI/sprite), usa distanza dal transform
                    if (!hit)
                    {
                        float radiusNoCollider = Mathf.Max(r, 0.6f);
                        hit = Vector2.Distance(worldPoint, transform.position) <= radiusNoCollider;
                    }
                    if (hit)
                    {
                        if (!_repeatInteractionWhileInRange)
                            _interacted = true;
                        OnInteract?.Invoke();
                    }
                }
            }
            else
                _interacted = false;
        }
        
        public void OnMouseDown()
        {
            bool isOverUI = UIBlocker.IsPointerOverUI();

            if (isOverUI)
                return;

            if (!PlayerInRange)
                return;
            
            if (!_repeatInteractionWhileInRange)
                _interacted = true;
            OnInteract?.Invoke();
        }
        
        private void OnMouseEnter()
        {
            if (_spriteRenderer != null)
                _spriteRenderer.color = _highlightColor;
        }
    
        private void OnMouseExit()
        {
            Deselect();
        }

        public void Deselect()
        {
            if (_spriteRenderer != null)
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