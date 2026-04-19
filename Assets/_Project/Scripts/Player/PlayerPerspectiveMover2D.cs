using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using _Project;
using _Project.Sporae.Core;
using _Project.World.VaultMap;

namespace _Project.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public class PlayerPerspectiveMover2D : MonoBehaviour
    {
        private float _debugNextLogTime = 0f;

        private static ContactFilter2D MakeBlockerFilter(LayerMask mask)
        {
            // Use layer mask, but allow triggers and filter them manually (some projects use trigger-walls).
            var f = new ContactFilter2D();
            f.useLayerMask = true;
            f.SetLayerMask(mask);
            f.useTriggers = true;
            return f;
        }

        private static readonly RaycastHit2D[] s_CastHits = new RaycastHit2D[8];
        private static readonly Collider2D[] s_OverlapHits = new Collider2D[12];

        private static bool IsNonBlockingTrigger(Collider2D c)
        {
            if (c == null) return true;
            if (!c.isTrigger) return false;

            // Triggers that should NOT block movement (zones, walk areas, elevator use-zone, occlusion helpers).
            // Keep this narrow and evidence-driven.
            int layer = c.gameObject.layer;
            if (layer == 0) // Default: usually zones/bounds in this project
                return true;

            string n = c.name ?? "";
            if (n.Contains("WALL_profondo", StringComparison.OrdinalIgnoreCase))
                return true;
            if (n.Contains("ELEV_UseZone", StringComparison.OrdinalIgnoreCase))
                return true;
            if (n.Contains("RoomZone_", StringComparison.OrdinalIgnoreCase))
                return true;
            if (n.Contains("Room Camra Bounds", StringComparison.OrdinalIgnoreCase) || n.Contains("Room Camera Bounds", StringComparison.OrdinalIgnoreCase))
                return true;

            // WalkArea bounds are triggers and must not block.
            if (c.GetComponentInParent<PerspectiveWalkArea2D>() != null)
                return true;

            return false;
        }

        private static bool IsBlockingHit(RaycastHit2D hit)
        {
            var c = hit.collider;
            if (c == null) return false;
            if (!c.isTrigger) return true;
            return !IsNonBlockingTrigger(c);
        }

        [Header("References")]
        [Tooltip("If left null, the mover will try to auto-pick a walk area by click/trigger.")]
        [SerializeField] private PerspectiveWalkArea2D currentWalkArea;

        [Header("Movement (World Space)")]
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float stopDistance = 0.06f;

        [Header("Input")]
        [SerializeField] private bool enableClickToMove = true;
        [SerializeField] private bool enableWASD = true;
        [SerializeField] private float wasdDeadzone = 0.01f;

        [Tooltip("How fast UV changes under WASD control.")]
        [SerializeField] private float uvSpeed = 0.75f;

        [Header("WASD Speed Mode")]
        [Tooltip("DEBUG_SAFE_FIX: If true, WASD movement tries to maintain a constant world-space speed across differently-sized walk areas, instead of a constant UV speed.")]
        [SerializeField] private bool useConstantWorldSpeedForWASD = true; // DEBUG_SAFE_FIX

        [SerializeField] private float wasdWorldSpeed = 4f;

        [Tooltip("Step in UV used to estimate local world-space axes (bigger = more stable, smaller = more precise).")]
        [SerializeField] private float uvDerivativeStep = 0.03f;

        [Header("Click Validation (Optional)")]
        [Tooltip("If enabled, click targets must overlap this 'walkable' mask. Useful only if you add a floor collider per room.")]
        [SerializeField] private bool requireWalkableForClick = false;
        [SerializeField] private LayerMask walkableMask;
        [SerializeField] private float walkableCheckRadius = 0.08f;

        [Header("Collision / Slide")]
        [SerializeField] private LayerMask blockerMask;
        [Tooltip("A small extra distance kept away from colliders to avoid jitter.")]
        [SerializeField] private float skinWidth = 0.02f;
        [Tooltip("How many iterations to resolve collision+slide (2 is usually enough).")]
        [SerializeField] private int slideIterations = 2;
        [Tooltip("DEBUG_SAFE_FIX: If true, when the movement is almost purely into a surface (slide ~ 0), try sliding along the surface tangent to avoid sticking.")]
        [SerializeField] private bool enableStickySlideFix = true; // DEBUG_SAFE_FIX

        [Header("WalkArea Switching (Trigger Bounds)")]
        [Tooltip("DEBUG_SAFE_FIX: Prevent switching to a walk area if projecting current position into its UV space would result in a large world-space mismatch (can cause teleports).")]
        [SerializeField] private bool guardAreaSwitchByProjectionError = true; // DEBUG_SAFE_FIX

        [SerializeField] private float maxAreaSwitchProjectionError = 0.6f;

        private Rigidbody2D _rb;
        private Collider2D _selfCollider;

        private Vector2 _currentUV = new(0.5f, 0.0f);
        private Vector2 _targetUV;
        private bool _hasTarget;

        public float CurrentU => _currentUV.x;
        public float CurrentV => _currentUV.y;

        private static readonly List<PerspectiveWalkArea2D> s_Areas = new();

        private void Awake()
        {
            RegisterInServiceContainer();

            _rb = GetComponent<Rigidbody2D>();
            _selfCollider = GetComponent<Collider2D>();

            // Consistent with PlayerClickMover2D defaults
            _rb.gravityScale = 0f;
            _rb.drag = 0f;
            _rb.angularDrag = 0f;

            // Ensure the player doesn't rotate due to collisions
            _rb.freezeRotation = true;
        }

        private void RegisterInServiceContainer()
        {
            if (ServiceContainer.Instance == null)
                return;

            ServiceContainer.Instance.Register(this);
        }

        private void OnEnable()
        {
            CacheAllAreas();
            TryInitUVFromCurrentPosition();
        }

        /// <summary>
        /// Teleports the player to a world position in a way that stays consistent with the mover's internal UV state.
        /// This avoids "snapping back" on the next FixedUpdate.
        /// </summary>
        public void TeleportToWorld(Vector2 worldPosition, bool pickAreaByPoint = true)
        {
            _hasTarget = false;

            // Set the rigidbody position directly (kinematic body)
            if (_rb != null)
            {
                _rb.position = worldPosition;
                _rb.velocity = Vector2.zero;
            }
            else
            {
                transform.position = worldPosition;
            }

            if (pickAreaByPoint)
            {
                PerspectiveWalkArea2D area = FindAreaByWorldPoint(worldPosition);
                if (area != null)
                    currentWalkArea = area;
            }

            // Reproject UV from the requested position (do NOT rely on _rb.position being synced to transform when teleported externally)
            if (currentWalkArea != null && currentWalkArea.TryProjectWorldToUV(worldPosition, out Vector2 uv))
            {
                _currentUV = new Vector2(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y));
            }
            else
            {
                _currentUV = new Vector2(0.5f, 0.0f);
            }

            _targetUV = _currentUV;
        }

        /// <summary>
        /// Reprojects the internal UV coordinates from the current world position.
        /// Useful after external teleports (e.g. EndDay spawn).
        /// </summary>
        public void ReprojectUVFromCurrentPosition()
        {
            TryInitUVFromCurrentPosition();
        }

        /// <summary>
        /// Picks a walk area based on a world point and sets it as current (if found),
        /// then reprojects UV from current position.
        /// </summary>
        public void SetCurrentAreaByWorldPoint(Vector2 worldPoint)
        {
            PerspectiveWalkArea2D area = FindAreaByWorldPoint(worldPoint);
            if (area != null)
                SetCurrentArea(area);
            else
                TryInitUVFromCurrentPosition();
        }

        private void Update()
        {
            if (enableClickToMove)
                HandleClickInput();
        }

        private void FixedUpdate()
        {
            if (currentWalkArea == null)
                return;

            if (GameplayUiModalLock.BlocksWorldInput)
                return;

            float hSpeedMul = 1f;
            var gm = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            if (gm?.PlayerHydrationSystem != null)
                hSpeedMul = Mathf.Max(0.05f, gm.PlayerHydrationSystem.GetMovementSpeedMultiplier());

            Vector2 wasd = enableWASD ? new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")) : Vector2.zero;
            bool hasWASD = wasd.sqrMagnitude > wasdDeadzone * wasdDeadzone;

            if (hasWASD)
            {
                // Direct control in UV space
                _hasTarget = false;

                float dt = Time.fixedDeltaTime;

                Vector2 from = _rb.position;
                Vector2 desired;

                float metersPerU_dbg = 0f;
                float metersPerV_dbg = 0f;
                float duStep_dbg = 0f;
                float dvStep_dbg = 0f;

                if (useConstantWorldSpeedForWASD && currentWalkArea.HasValidCorners)
                {
                    // Estimate local world scale (meters per UV) using a symmetric (central) difference around the current UV.
                    // This avoids clamp artifacts near v=0/1 that can create "magnetic acceleration" effects.
                    float du = Mathf.Clamp(uvDerivativeStep, 0.005f, 0.15f);
                    float dv = du;

                    float u0 = Mathf.Clamp01(_currentUV.x);
                    float v0 = Mathf.Clamp01(_currentUV.y);
                    float uF = Mathf.Clamp01(u0 + du);
                    float uB = Mathf.Clamp01(u0 - du);
                    float vF = Mathf.Clamp01(v0 + dv);
                    float vB = Mathf.Clamp01(v0 - dv);

                    float duEff = Mathf.Max(0.000001f, uF - uB);
                    float dvEff = Mathf.Max(0.000001f, vF - vB);

                    Vector2 pU0 = currentWalkArea.MapToWorld(u0, v0);
                    Vector2 pU1 = currentWalkArea.MapToWorld(uF, v0);
                    Vector2 pU2 = currentWalkArea.MapToWorld(uB, v0);
                    Vector2 pV1 = currentWalkArea.MapToWorld(u0, vF);
                    Vector2 pV2 = currentWalkArea.MapToWorld(u0, vB);

                    float metersPerU = Vector2.Distance(pU1, pU2) / duEff;
                    float metersPerV = Vector2.Distance(pV1, pV2) / dvEff;

                    metersPerU_dbg = metersPerU;
                    metersPerV_dbg = metersPerV;

                    // If basis degenerates, fallback to legacy UV mode.
                    if (metersPerU < 0.0001f || metersPerV < 0.0001f)
                    {
                        float uvEff = uvSpeed * hSpeedMul;
                        _currentUV.x = Mathf.Clamp01(_currentUV.x + wasd.x * uvEff * dt);
                        _currentUV.y = Mathf.Clamp01(_currentUV.y + wasd.y * uvEff * dt);
                        desired = currentWalkArea.MapToWorld(_currentUV.x, _currentUV.y);
                    }
                    else
                    {
                        // Normalize input so diagonal doesn't exceed target speed.
                        Vector2 input = wasd.normalized;

                        float effWasd = wasdWorldSpeed * hSpeedMul;
                        float duStep = (input.x * effWasd * dt) / metersPerU;
                        float dvStep = (input.y * effWasd * dt) / metersPerV;

                        duStep_dbg = duStep;
                        dvStep_dbg = dvStep;

                        _currentUV.x = Mathf.Clamp01(_currentUV.x + duStep);
                        _currentUV.y = Mathf.Clamp01(_currentUV.y + dvStep);

                        desired = currentWalkArea.MapToWorld(_currentUV.x, _currentUV.y);
                    }
                }
                else
                {
                    // Legacy UV speed mode
                    float uvEff = uvSpeed * hSpeedMul;
                    _currentUV.x = Mathf.Clamp01(_currentUV.x + wasd.x * uvEff * dt);
                    _currentUV.y = Mathf.Clamp01(_currentUV.y + wasd.y * uvEff * dt);
                    desired = currentWalkArea.MapToWorld(_currentUV.x, _currentUV.y);
                }

                Vector2 newPos = ResolveCollisionsWithSlide(from, desired);
                _rb.MovePosition(newPos);

                // If we are trying to move but remain stuck, capture a minimal snapshot.
                float desiredDist = Vector2.Distance(from, desired);
                float movedDist = Vector2.Distance(from, newPos);
                if (desiredDist > 0.05f && movedDist < 0.001f && Time.time >= _debugNextLogTime)
                {
                    _debugNextLogTime = Time.time + 0.5f;
                }

                // Re-project after slide so UV stays consistent with actual position
                if (currentWalkArea.TryProjectWorldToUV(newPos, out Vector2 uv))
                    _currentUV = uv;
            }
            else if (_hasTarget)
            {
                Vector2 targetWorld = currentWalkArea.MapToWorld(_targetUV.x, _targetUV.y);
                float dist = Vector2.Distance(_rb.position, targetWorld);

                if (dist <= stopDistance)
                {
                    _hasTarget = false;
                    _currentUV = _targetUV;
                    _rb.MovePosition(targetWorld);
                    return;
                }

                float effMove = moveSpeed * hSpeedMul;
                Vector2 step = Vector2.MoveTowards(_rb.position, targetWorld, effMove * Time.fixedDeltaTime);
                Vector2 newPos = ResolveCollisionsWithSlide(_rb.position, step);
                _rb.MovePosition(newPos);

                if (currentWalkArea.TryProjectWorldToUV(newPos, out Vector2 uv))
                    _currentUV = uv;
            }
        }

        private void HandleClickInput()
        {
            if (!Input.GetMouseButtonDown(0))
                return;

            if (GameplayUiModalLock.BlocksWorldInput)
                return;

            Vector2 clickWorld = GetMouseWorldPosition();

            if (UIBlocker.IsPointerOverUI())
                return;

            // Se il click è su un Interactable in range, non avviare movimento (l'Interactable gestisce il click come E)
            const float interactableClickRadius = 0.35f;
            Collider2D[] atPoint = Physics2D.OverlapCircleAll(clickWorld, interactableClickRadius);
            for (int i = 0; i < atPoint.Length; i++)
            {
                var inter = atPoint[i].GetComponent<Interactable>();
                if (inter == null)
                    inter = atPoint[i].GetComponentInParent<Interactable>();
                if (inter != null && inter.PlayerInRange)
                    return;
            }
            // Interactable senza collider (es. Incubator, FoodSynthMachine): se il click è vicino al transform, non muovere
            var allInteractables = UnityEngine.Object.FindObjectsOfType<Interactable>();
            const float interactableNoColliderRadius = 0.6f;
            for (int i = 0; i < allInteractables.Length; i++)
            {
                if (allInteractables[i].PlayerInRange && Vector2.Distance(clickWorld, allInteractables[i].transform.position) <= interactableNoColliderRadius)
                    return;
            }

            if (requireWalkableForClick)
            {
                Collider2D hit = Physics2D.OverlapCircle(clickWorld, walkableCheckRadius, walkableMask);
                if (hit == null)
                    return;
            }

            // Pick walk area by click point if possible (per-room)
            PerspectiveWalkArea2D area = FindAreaByWorldPoint(clickWorld);
            if (area != null)
                SetCurrentArea(area);

            if (currentWalkArea == null)
                return;

            bool projectOk = currentWalkArea.TryProjectWorldToUV(clickWorld, out Vector2 uv);
            if (!projectOk)
                return;

            _targetUV = new Vector2(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y));
            _hasTarget = true;
        }

        /// <summary>Fired whenever the player enters a new PerspectiveWalkArea2D. Used by RoomTracker.</summary>
        public event Action<PerspectiveWalkArea2D> OnAreaChanged;

        public void SetCurrentArea(PerspectiveWalkArea2D area)
        {
            if (area == null)
                return;

            currentWalkArea = area;
            OnAreaChanged?.Invoke(area);
            TryInitUVFromCurrentPosition();
        }

        private void TryInitUVFromCurrentPosition()
        {
            if (currentWalkArea == null)
                return;

            if (currentWalkArea.TryProjectWorldToUV(_rb.position, out Vector2 uv))
            {
                _currentUV = new Vector2(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y));
                if (_hasTarget)
                    _targetUV = new Vector2(Mathf.Clamp01(_targetUV.x), Mathf.Clamp01(_targetUV.y));
            }
            else
            {
                // fallback to near center
                _currentUV = new Vector2(0.5f, 0.0f);
            }
        }

        private static void CacheAllAreas()
        {
            s_Areas.Clear();
            s_Areas.AddRange(UnityEngine.Object.FindObjectsOfType<PerspectiveWalkArea2D>(includeInactive: false));
        }

        private static PerspectiveWalkArea2D FindAreaByWorldPoint(Vector2 world)
        {
            // When multiple AreaBounds overlap (common around elevator lobbies / connectors),
            // picking the "first" match can lead to large UV reprojection errors and apparent teleports.
            // Prefer the area that best reprojects back to the same world point.
            PerspectiveWalkArea2D bestArea = null;
            float bestErr = float.PositiveInfinity;

            for (int i = 0; i < s_Areas.Count; i++)
            {
                PerspectiveWalkArea2D a = s_Areas[i];
                if (a == null)
                    continue;

                if (!a.ContainsWorldPoint(world))
                    continue;

                if (!a.HasValidCorners)
                    continue;

                if (!a.TryProjectWorldToUV(world, out Vector2 uv))
                    continue;

                Vector2 mapped = a.MapToWorld(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y));
                float err = Vector2.Distance(world, mapped);

                if (err < bestErr)
                {
                    bestErr = err;
                    bestArea = a;
                }
            }

            return bestArea;
        }

        private Vector2 ResolveCollisionsWithSlide(Vector2 from, Vector2 desired)
        {
            if (_selfCollider == null || blockerMask == 0)
                return desired;

            Vector2 delta = desired - from;
            if (delta.sqrMagnitude < 0.000001f)
                return desired;

            Vector2 pos = from;
            Vector2 remaining = delta;

            int iters = Mathf.Clamp(slideIterations, 1, 4);
            for (int i = 0; i < iters; i++)
            {
                if (remaining.sqrMagnitude < 0.000001f)
                    break;

                float dist = remaining.magnitude;
                Vector2 dir = remaining / dist;

                RaycastHit2D hit = CastSelf(pos, dir, dist + skinWidth, blockerMask);
                if (hit.collider == null)
                {
                    pos += remaining;
                    break;
                }


                // DEBUG_SAFE_FIX: When the cast reports distance ~0 while moving tangentially (dot >= 0),
                // treat it as a "touching" contact and allow motion along the tangent, otherwise we can get stuck on corners.
                float dot = Vector2.Dot(dir, hit.normal);
                if (enableStickySlideFix && hit.distance <= 0.0001f && dot >= -0.0001f)
                {
                    Vector2 tryPos = pos + remaining;
                    if (!WouldOverlapAt(tryPos))
                    {
                        pos = tryPos;
                        break;
                    }
                }

                float moveDist = Mathf.Max(0f, hit.distance - skinWidth);
                if (moveDist > 0f)
                    pos += dir * moveDist;

                // Slide: remove component into the normal
                Vector2 leftover = remaining - dir * moveDist;
                Vector2 n = hit.normal;
                Vector2 slide = leftover - Vector2.Dot(leftover, n) * n;

                // DEBUG_SAFE_FIX: When pushing almost purely into a surface (slide nearly zero),
                // try sliding along the surface tangent to avoid "sticking" after repeated contacts.
                if (enableStickySlideFix && moveDist <= 0.0001f && slide.sqrMagnitude < 0.000001f)
                {
                    Vector2 t = new Vector2(-n.y, n.x); // tangent
                    float want = leftover.magnitude;

                    float best = 0f;
                    Vector2 bestDir = Vector2.zero;

                    float a = TangentFreeDistance(pos, t, want);
                    float b = TangentFreeDistance(pos, -t, want);

                    if (a >= b)
                    {
                        best = a;
                        bestDir = t;
                    }
                    else
                    {
                        best = b;
                        bestDir = -t;
                    }

                    if (best > 0.0001f)
                    {
                        remaining = bestDir.normalized * best;
                        continue;
                    }
                }

                // Continue with the slide portion only
                remaining = slide;
            }

            return pos;
        }

        private bool WouldOverlapAt(Vector2 worldPos)
        {
            if (_selfCollider == null || blockerMask == 0)
                return false;

            // Predict overlap using geometry casts at an arbitrary position (cannot rely on OverlapCollider which uses current transform).
            if (_selfCollider is CapsuleCollider2D capsule)
            {
                Vector3 ls = capsule.transform.lossyScale;
                Vector2 size = new Vector2(
                    Mathf.Abs(capsule.size.x * ls.x),
                    Mathf.Abs(capsule.size.y * ls.y)
                );
                float angle = capsule.transform.eulerAngles.z;
                Vector2 worldOffset = (Vector2)(capsule.transform.rotation * (Vector3)capsule.offset);
                worldOffset = new Vector2(worldOffset.x * Mathf.Abs(ls.x), worldOffset.y * Mathf.Abs(ls.y));
                var filter = MakeBlockerFilter(blockerMask);
                int n = Physics2D.OverlapCapsule(worldPos + worldOffset, size, capsule.direction, angle, filter, s_OverlapHits);
                if (n <= 0) return false;
                for (int i = 0; i < n; i++)
                {
                    var c = s_OverlapHits[i];
                    if (c == null) continue;
                    if (IsNonBlockingTrigger(c)) continue;
                    return true;
                }
                return false;
            }

            float radius = Mathf.Min(_selfCollider.bounds.extents.x, _selfCollider.bounds.extents.y);
            radius = Mathf.Max(0.02f, radius);
            var filter2 = MakeBlockerFilter(blockerMask);
            int n2 = Physics2D.OverlapCircle(worldPos, radius, filter2, s_OverlapHits);
            if (n2 <= 0) return false;
            for (int i = 0; i < n2; i++)
            {
                var c = s_OverlapHits[i];
                if (c == null) continue;
                if (IsNonBlockingTrigger(c)) continue;
                return true;
            }
            return false;
        }

        private float TangentFreeDistance(Vector2 origin, Vector2 tangentDir, float wantDistance)
        {
            if (wantDistance <= 0.0001f)
                return 0f;

            RaycastHit2D h = CastSelf(origin, tangentDir.normalized, wantDistance + skinWidth, blockerMask);
            if (h.collider == null)
                return wantDistance;

            return Mathf.Max(0f, h.distance - skinWidth);
        }

        private RaycastHit2D CastSelf(Vector2 origin, Vector2 direction, float distance, LayerMask mask)
        {
            var filter = MakeBlockerFilter(mask);

            // We prefer CapsuleCast using the player's CapsuleCollider2D when available
            if (_selfCollider is CapsuleCollider2D capsule)
            {
                // DEBUG_SAFE_FIX: include capsule offset and lossyScale for correct casts
                Vector3 ls = capsule.transform.lossyScale;
                Vector2 size = new Vector2(
                    Mathf.Abs(capsule.size.x * ls.x),
                    Mathf.Abs(capsule.size.y * ls.y)
                );
                CapsuleDirection2D capDir = capsule.direction;
                float angle = capsule.transform.eulerAngles.z;

                Vector2 worldOffset = (Vector2)(capsule.transform.rotation * (Vector3)capsule.offset);
                worldOffset = new Vector2(worldOffset.x * Mathf.Abs(ls.x), worldOffset.y * Mathf.Abs(ls.y));
                int count = Physics2D.CapsuleCast(origin + worldOffset, size, capDir, angle, direction, filter, s_CastHits, distance);
                if (count <= 0) return default;

                // pick nearest BLOCKING hit
                RaycastHit2D bestHit = default;
                float bestDist = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                {
                    var h = s_CastHits[i];
                    if (!IsBlockingHit(h)) continue;
                    if (h.distance < bestDist)
                    {
                        bestDist = h.distance;
                        bestHit = h;
                    }
                }
                return bestDist < float.PositiveInfinity ? bestHit : default;
            }

            // Fallback to a circle cast using bounds extents
            float radius = Mathf.Min(_selfCollider.bounds.extents.x, _selfCollider.bounds.extents.y);
            radius = Mathf.Max(0.02f, radius);
            int ccount = Physics2D.CircleCast(origin, radius, direction, filter, s_CastHits, distance);
            if (ccount <= 0) return default;

            RaycastHit2D cbestHit = default;
            float cbestDist = float.PositiveInfinity;
            for (int i = 0; i < ccount; i++)
            {
                var h = s_CastHits[i];
                if (!IsBlockingHit(h)) continue;
                if (h.distance < cbestDist)
                {
                    cbestDist = h.distance;
                    cbestHit = h;
                }
            }
            return cbestDist < float.PositiveInfinity ? cbestHit : default;
        }

        private static bool IsPointerOverUI()
        {
            // Keep it safe even if no EventSystem exists
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private static Vector2 GetMouseWorldPosition()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                mainCamera = UnityEngine.Object.FindObjectOfType<Camera>();

            if (mainCamera == null)
                return Vector2.zero;

            Vector3 w = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            return new Vector2(w.x, w.y);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Optional per-room: if we walk into an area bounds collider, switch area
            PerspectiveWalkArea2D area = other.GetComponentInParent<PerspectiveWalkArea2D>();
            if (area != null && area.AreaBounds == other)
            {
                if (guardAreaSwitchByProjectionError && _rb != null && area.HasValidCorners)
                {
                    Vector2 w = _rb.position;
                    if (area.TryProjectWorldToUV(w, out Vector2 uv))
                    {
                        Vector2 mapped = area.MapToWorld(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y));
                        float err = Vector2.Distance(w, mapped);
                        if (err > Mathf.Max(0.001f, maxAreaSwitchProjectionError))
                        {
                            return;
                        }
                    }
                }
                SetCurrentArea(area);
            }
        }
    }
}

