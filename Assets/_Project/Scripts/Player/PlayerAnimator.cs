using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(PlayerClickMover2D))]
    public class PlayerAnimator : MonoBehaviour
    {
        private static readonly int k_idleAnimation = Animator.StringToHash("Idle");
        private static readonly int k_walkingAnimation = Animator.StringToHash("Walking"); 
        
        private Animator _animator;
        private Rigidbody2D _rigidbody;
        private SpriteRenderer _spriteRenderer;
        
        private int _currentAnimation;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _animator = GetComponentInChildren<Animator>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (!_animator || !_spriteRenderer || !_rigidbody)
                Debug.LogWarning("PlayerAnimator is missing a necessary components.");
            
            _currentAnimation = k_idleAnimation;
        }

        private void Update()
        {
            int updatedAnimation = k_idleAnimation;

            if (Mathf.Abs(_rigidbody.velocity.x) > 0)
            {
                _spriteRenderer.flipX = _rigidbody.velocity.x > 0;
                updatedAnimation = k_walkingAnimation;
            }

            if (_currentAnimation == updatedAnimation)
                return;
            
            _animator.Play(updatedAnimation);
            _currentAnimation = updatedAnimation;
        }
    }
}