using UnityEngine;
using Sporae.DevTools;
using _Project.Player;

namespace _Project
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerAnimator : MonoBehaviour
    {
        private static readonly int k_idleAnimation = Animator.StringToHash("Idle");
        private static readonly int k_walkingAnimation = Animator.StringToHash("Walking");
        private static readonly int k_idleBackAnimation = Animator.StringToHash("IdleBack");
        private static readonly int k_walkingBackAnimation = Animator.StringToHash("WalkingBack");

        [Header("DEBUG_SAFE_FIX - Movement Detection")]
        [Tooltip("DEBUG_SAFE_FIX: If true, when Rigidbody2D.velocity is ~0 (e.g. MovePosition movement), infer movement from delta position to drive walking animation.")]
        [SerializeField] private bool useDeltaPositionForWalking = true; // DEBUG_SAFE_FIX

        [Tooltip("Minimum absolute X velocity to consider the player walking (legacy behavior).")]
        [SerializeField] private float minAbsVelocityXForWalk = 0.001f;

        [Tooltip("Minimum delta position magnitude per frame to consider the player walking when using delta-position inference.")]
        [SerializeField] private float minDeltaMagnitudeForWalk = 0.0015f;

        [Tooltip("DEBUG_SAFE_FIX: Keeps the Walking animation active for a short time after movement is detected to avoid flicker (Update vs FixedUpdate sampling).")]
        [SerializeField] private bool useWalkHoldTime = true; // DEBUG_SAFE_FIX

        [SerializeField] private float walkHoldSeconds = 0.15f;
        
        private Animator _animator;
        private Rigidbody2D _rigidbody;
        private SpriteRenderer _spriteRenderer;
        private PlayerClickMover2D _clickMover;
        private PlayerPerspectiveMover2D _perspectiveMover;
        
        private int _currentAnimation;
        private Vector2 _lastPos;
        private float _walkHoldRemaining = 0f;
        private bool _facingBack = false;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _animator = GetComponentInChildren<Animator>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _clickMover = GetComponent<PlayerClickMover2D>();
            _perspectiveMover = GetComponent<PlayerPerspectiveMover2D>();

            if (!_animator || !_spriteRenderer || !_rigidbody)
                SporiumLogger.LogWarning(LogCategory.Core, "PlayerAnimator is missing a necessary components.");
            
            _currentAnimation = k_idleAnimation;
            _lastPos = _rigidbody != null ? _rigidbody.position : (Vector2)transform.position;
        }

        private void Update()
        {
            Vector2 pos = _rigidbody != null ? _rigidbody.position : (Vector2)transform.position;
            Vector2 delta = pos - _lastPos;
            _lastPos = pos;

            float velX = _rigidbody != null ? _rigidbody.velocity.x : 0f;
            bool walkByVel = Mathf.Abs(velX) > Mathf.Max(0.000001f, minAbsVelocityXForWalk);
            bool walkByDelta = false;

            // Update facing based on movement direction (prefer Y for "back" when moving up).
            if (walkByVel)
            {
                _facingBack = false;
            }
            else if (useDeltaPositionForWalking)
            {
                float absX = Mathf.Abs(delta.x);
                float absY = Mathf.Abs(delta.y);
                if (absX > 0.00001f || absY > 0.00001f)
                {
                    if (absY > absX)
                        _facingBack = delta.y > 0f;
                    else if (absX > 0.00001f)
                        _facingBack = false;
                }
            }

            int updatedAnimation = _facingBack ? k_idleBackAnimation : k_idleAnimation;

            if (walkByVel)
            {
                _spriteRenderer.flipX = velX > 0;
                updatedAnimation = _facingBack ? k_walkingBackAnimation : k_walkingAnimation;
            }
            else if (useDeltaPositionForWalking)
            {
                // DEBUG_SAFE_FIX: MovePosition often yields rb.velocity ~0. Use delta-position to detect actual movement.
                walkByDelta = delta.magnitude > Mathf.Max(0.000001f, minDeltaMagnitudeForWalk);
                if (walkByDelta)
                {
                    if (!_facingBack && Mathf.Abs(delta.x) > 0.00001f)
                        _spriteRenderer.flipX = delta.x > 0;
                    updatedAnimation = _facingBack ? k_walkingBackAnimation : k_walkingAnimation;
                }
            }

            // DEBUG_SAFE_FIX: Prevent Idle<->Walking flicker caused by Update sampling while movement happens in FixedUpdate (MovePosition).
            if (useWalkHoldTime)
            {
                bool movingNow = walkByVel || walkByDelta;
                if (movingNow)
                    _walkHoldRemaining = Mathf.Max(_walkHoldRemaining, walkHoldSeconds);
                else
                    _walkHoldRemaining = Mathf.Max(0f, _walkHoldRemaining - Time.deltaTime);

                if (_walkHoldRemaining > 0f)
                    updatedAnimation = _facingBack ? k_walkingBackAnimation : k_walkingAnimation;
            }

            if (_currentAnimation == updatedAnimation)
                return;
            
            _animator.Play(updatedAnimation);
            _currentAnimation = updatedAnimation;
        }
    }
}