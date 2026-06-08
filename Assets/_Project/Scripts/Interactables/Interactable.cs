using System;
using System.Collections.Generic;
using _Project.Sporae.Core;
using UnityEngine;

namespace _Project
{
    public class Interactable : MonoBehaviour
    {
        /// <summary>Per disambiguare [E] quando più oggetti sono in range: si sceglie il più vicino al player se il mouse non punta un collider.</summary>
        private static readonly List<Interactable> Registry = new List<Interactable>(32);

        [Tooltip("Distanza massima per interagire (click o E). Se 0, non si può mai interagire.")]
        [SerializeField] private float _interactDistance = 2f;

        [Tooltip("Raggio (in unità mondo) entro cui un click conta come \"sul\" questo Interactable. Aumenta se il click funziona solo in un punto.")]
        [SerializeField] private float _clickRadius = 0.35f;

        [Tooltip("Distanza massima per mostrare il prompt testuale [E]. Se 0 usa la stessa distanza di interazione.")]
        [SerializeField] private float _promptDistance = 1.5f;

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
        private bool _interactionAvailable = true;

        public bool IsInteractionAvailable => _interactionAvailable;

        private float EffectiveInteractDistance => _interactDistance > 0f ? _interactDistance : 2f;
        private float EffectivePromptDistance => _promptDistance > 0f ? Mathf.Min(_promptDistance, EffectiveInteractDistance) : EffectiveInteractDistance;
        
        public bool PlayerInRange
        {
            get
            {
                if (!TryGetPlayerPosition(out Vector2 pp))
                    return false;
                return GetDistanceToPlayer(pp) <= EffectiveInteractDistance;
            }
        }

        private bool PlayerInPromptRange
        {
            get
            {
                if (!TryGetPlayerPosition(out Vector2 pp))
                    return false;
                return GetDistanceToPlayer(pp) <= EffectivePromptDistance;
            }
        }

        public void SetRepeatInteractionWhileInRange(bool value) => _repeatInteractionWhileInRange = value;

        /// <summary>Gate runtime per prompt e input (es. display ascensore durante viaggio).</summary>
        public void SetInteractionAvailable(bool available) => _interactionAvailable = available;

        public void SetInteractDistance(float meters) => _interactDistance = Mathf.Max(0.25f, meters);

        public string GetInteractionDisplayName()
        {
            string fallback = gameObject.name ?? string.Empty;
            if (fallback.EndsWith("(Clone)", StringComparison.Ordinal))
                fallback = fallback.Replace("(Clone)", string.Empty).Trim();
            return fallback;
        }

        /// <summary>
        /// Converte il mouse in coordinate mondo 2D. Senza z corretto, <see cref="Camera.ScreenToWorldPoint"/>
        /// può collassare i click in una zona sbagliata e far “vincere” sempre lo stesso Interactable.
        /// </summary>
        private static Vector2 GetMouseWorld2D(Camera cam)
        {
            if (cam == null)
                return Vector2.zero;
            Vector3 p = Input.mousePosition;
            if (cam.orthographic)
                p.z = Mathf.Abs(cam.transform.position.z) > 0.01f ? Mathf.Abs(cam.transform.position.z) : 10f;
            else
                p.z = cam.nearClipPlane;
            Vector3 w = cam.ScreenToWorldPoint(p);
            return new Vector2(w.x, w.y);
        }

        /// <summary>
        /// Tra tutti gli Interactable sotto il puntatore, sceglie quello con collider più “specifico”
        /// (area bounds minima) così letto/armadio non rubano il click a letto/terminale quando i raggi si sovrappongono.
        /// </summary>
        private static Interactable ResolveInteractableUnderPointer(Vector2 worldPoint, float probeRadius)
        {
            Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);
            Interactable best = null;
            float bestArea = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D h = hits[i];
                if (h == null) continue;
                var inter = h.GetComponent<Interactable>() ?? h.GetComponentInParent<Interactable>();
                if (inter == null) continue;
                float area = h.bounds.size.x * h.bounds.size.y;
                if (area < bestArea)
                {
                    bestArea = area;
                    best = inter;
                }
            }

            if (best != null)
                return best;

            hits = Physics2D.OverlapCircleAll(worldPoint, probeRadius);
            best = null;
            bestArea = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D h = hits[i];
                if (h == null) continue;
                var inter = h.GetComponent<Interactable>() ?? h.GetComponentInParent<Interactable>();
                if (inter == null) continue;
                float area = h.bounds.size.x * h.bounds.size.y;
                if (area < bestArea)
                {
                    bestArea = area;
                    best = inter;
                }
            }

            return best;
        }

        /// <summary>
        /// [E]: priorità al target sotto il cursore (come il click); se il mouse non è su nessun Interactable,
        /// si usa il più vicino al player tra quelli in range.
        /// </summary>
        private bool TryResolveKeyboardInteract()
        {
            Interactable resolvedTarget = ResolveKeyboardTargetForCurrentState(out _, out _, out _, out _, out _);
            return resolvedTarget == this;
        }

        private static Interactable ResolveKeyboardTargetForCurrentState(out string reason, out Interactable focused, out Interactable nearest, out Vector2 worldPoint, out string cameraName)
        {
            Camera cam = Camera.main != null ? Camera.main : UnityEngine.Object.FindObjectOfType<Camera>();
            worldPoint = GetMouseWorld2D(cam);
            float r = 0.35f;
            focused = ResolveInteractableUnderPointer(worldPoint, r);
            if (focused != null && !focused.IsInteractionAvailable)
                focused = null;
            nearest = null;
            cameraName = cam != null ? cam.gameObject.name : "<null>";

            if (focused != null)
            {
                if (focused.PlayerInRange)
                {
                    reason = "focused-self";
                    return focused;
                }

                reason = "focused-out-of-range";
                return null;
            }

            nearest = GetNearestInteractableInRangeToPlayer();
            reason = "nearest-fallback";
            return nearest;
        }

        /// <summary>Click sinistro: stessa risoluzione “focus” usata in precedenza.</summary>
        private bool TryResolveMouseClickInteract()
        {
            if (!_interactionAvailable)
                return false;

            Camera cam = Camera.main != null ? Camera.main : UnityEngine.Object.FindObjectOfType<Camera>();
            Vector2 worldPoint = GetMouseWorld2D(cam);
            float r = _clickRadius > 0f ? _clickRadius : 0.35f;

            Interactable focused = ResolveInteractableUnderPointer(worldPoint, r);
            if (focused != null && focused != this)
                return false;

            bool hit = false;
            if (_collider2D != null)
            {
                if (focused == this)
                {
                    hit = true;
                }
                else if (focused == null)
                {
                    hit = _collider2D.OverlapPoint(worldPoint);
                    if (!hit)
                    {
                        Collider2D[] near = Physics2D.OverlapCircleAll(worldPoint, r);
                        for (int i = 0; i < near.Length; i++)
                        {
                            if (near[i] == _collider2D)
                            {
                                hit = true;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                float radiusNoCollider = Mathf.Max(r, 0.6f);
                hit = Vector2.Distance(worldPoint, transform.position) <= radiusNoCollider;
            }

            return hit;
        }

        /// <summary>
        /// Tra tutti gli Interactable con <see cref="PlayerInRange"/> true, quello col pivot più vicino al player.
        /// </summary>
        private static Interactable GetNearestInteractableInRangeToPlayer()
        {
            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo == null)
                return null;
            var rb = playerGo.GetComponent<Rigidbody2D>();
            Vector2 pp = rb != null ? rb.position : (Vector2)playerGo.transform.position;

            Interactable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < Registry.Count; i++)
            {
                Interactable it = Registry[i];
                if (it == null || !it.PlayerInRange || !it.IsInteractionAvailable)
                    continue;
                float d = it.GetDistanceToPlayer(pp);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = it;
                }
            }

            return best;
        }

        private void OnEnable()
        {
            if (!Registry.Contains(this))
                Registry.Add(this);
        }

        private void OnDisable()
        {
            Registry.Remove(this);
        }

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
            if (GameplayUiModalLock.BlocksWorldInput)
                return;

            bool currentlyInRange = PlayerInRange;
            if (!currentlyInRange)
            {
                _interacted = false;
                return;
            }

            if (!_interactionAvailable)
                return;

            if (_playerInteractAdvice != null && (!_interacted || _repeatInteractionWhileInRange))
            {
                Interactable promptTarget = ResolveKeyboardTargetForCurrentState(out _, out _, out _, out _, out _);
                if (promptTarget == this && PlayerInPromptRange)
                    _playerInteractAdvice.AddInteractable(GetInteractionDisplayName());
            }

            // [E]: stessa logica di “focus” del click (puntatore) oppure, se il mouse non è su nessun collider Interactable,
            // un solo oggetto vince: il più vicino al player tra quelli in range (evita che tutti gli E in Update sparino insieme).
            if (Input.GetKeyDown(KeyCode.E) && !UIBlocker.IsPointerOverUI())
            {
                if (TryResolveKeyboardInteract())
                {
                    if (!_repeatInteractionWhileInRange)
                        _interacted = true;
                    OnInteract?.Invoke();
                    return;
                }
            }

            // Click: un solo Interactable “vincente” sotto il cursore (evita che un oggetto con raggio grande / fallback rubi il click).
            if (Input.GetMouseButtonDown(0) && !UIBlocker.IsPointerOverUI())
            {
                if (TryResolveMouseClickInteract())
                {
                    if (!_repeatInteractionWhileInRange)
                        _interacted = true;
                    OnInteract?.Invoke();
                }
            }
        }

        private float GetDistanceToPlayer(Vector2 playerPosition)
        {
            if (_collider2D == null)
                return Vector2.Distance(playerPosition, transform.position);
            Vector2 closest = _collider2D.ClosestPoint(playerPosition);
            return Vector2.Distance(playerPosition, closest);
        }

        private bool TryGetPlayerPosition(out Vector2 playerPosition)
        {
            if (_playerTransform == null)
            {
                playerPosition = default;
                return false;
            }

            playerPosition = _playerRigidbody != null ? _playerRigidbody.position : (Vector2)_playerTransform.position;
            return true;
        }
        
        public void OnMouseDown()
        {
            if (GameplayUiModalLock.BlocksWorldInput)
                return;

            bool isOverUI = UIBlocker.IsPointerOverUI();

            if (isOverUI)
                return;

            if (!PlayerInRange || !_interactionAvailable)
                return;

            if (!TryResolveMouseClickInteract())
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