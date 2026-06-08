using UnityEngine;

namespace _Project.World.VaultMap
{
    /// <summary>
    /// Defines a trapezoidal walkable area used to simulate depth in 2D (2.5D).
    /// The mapping is UV (u: left-right 0..1, v: near-far 0..1) to world space.
    /// </summary>
    public class PerspectiveWalkArea2D : MonoBehaviour
    {
        [Header("Corners (Trapezoid)")]
        [SerializeField] private Transform nearLeft;
        [SerializeField] private Transform nearRight;
        [SerializeField] private Transform farLeft;
        [SerializeField] private Transform farRight;

        [Header("Optional Bounds (for per-room selection)")]
        [Tooltip("Optional Collider2D used to pick this area by point overlap / triggers. Should usually be IsTrigger=ON.")]
        [SerializeField] private Collider2D areaBounds;

        [Header("Optional lateral clamp (cabin shaft)")]
        [Tooltip("When enabled, narrows walkable u as v increases so the player cannot leave the visual cabin width.")]
        [SerializeField] private bool limitLateralUWhenDeep;
        [SerializeField] [Range(0f, 1f)] private float lateralClampStartV = 0.35f;
        [SerializeField] [Range(0f, 1f)] private float lateralUMinAtNear = 0.14f;
        [SerializeField] [Range(0f, 1f)] private float lateralUMaxAtNear = 0.44f;
        [SerializeField] [Range(0f, 1f)] private float lateralUMinAtFar = 0.08f;
        [SerializeField] [Range(0f, 1f)] private float lateralUMaxAtFar = 0.49f;

        public Collider2D AreaBounds => areaBounds;
        public bool HasLateralDepthClamp => limitLateralUWhenDeep;

        public bool HasValidCorners =>
            nearLeft != null && nearRight != null && farLeft != null && farRight != null;

        public Vector2 MapToWorld(float u, float v)
        {
            u = Mathf.Clamp01(u);
            v = Mathf.Clamp01(v);

            Vector2 nL = nearLeft.position;
            Vector2 nR = nearRight.position;
            Vector2 fL = farLeft.position;
            Vector2 fR = farRight.position;

            Vector2 near = Vector2.Lerp(nL, nR, u);
            Vector2 far = Vector2.Lerp(fL, fR, u);
            return Vector2.Lerp(near, far, v);
        }

        public bool TryProjectWorldToUV(Vector2 world, out Vector2 uv)
        {
            // Invert bilinear mapping with Newton iterations.
            // Works well for convex quads / trapezoids used here.
            uv = new Vector2(0.5f, 0.5f);

            if (!HasValidCorners)
                return false;

            Vector2 p00 = nearLeft.position;  // (0,0)
            Vector2 p10 = nearRight.position; // (1,0)
            Vector2 p01 = farLeft.position;   // (0,1)
            Vector2 p11 = farRight.position;  // (1,1)

            // Quick early-out: if degenerate, avoid division issues
            float quadArea = Mathf.Abs(Cross(p10 - p00, p01 - p00)) + Mathf.Abs(Cross(p11 - p10, p01 - p10));
            if (quadArea < 0.0001f)
                return false;

            const int kIterations = 10;
            const float kEps = 1e-4f;

            float u = 0.5f;
            float v = 0.5f;

            for (int i = 0; i < kIterations; i++)
            {
                Vector2 f = Bilinear(p00, p10, p01, p11, u, v) - world;

                if (f.sqrMagnitude < kEps * kEps)
                    break;

                // Partial derivatives (Jacobian)
                Vector2 du = dBilinear_du(p00, p10, p01, p11, v);
                Vector2 dv = dBilinear_dv(p00, p10, p01, p11, u);

                float det = du.x * dv.y - du.y * dv.x;
                if (Mathf.Abs(det) < 1e-8f)
                    break;

                // Solve J * delta = -f
                float invDet = 1f / det;
                float deltaU = (-f.x * dv.y + f.y * dv.x) * invDet;
                float deltaV = (du.y * f.x - du.x * f.y) * invDet;

                u += deltaU;
                v += deltaV;

                // keep stable
                u = Mathf.Clamp01(u);
                v = Mathf.Clamp01(v);
            }

            uv = new Vector2(u, v);
            return true;
        }

        public bool ContainsWorldPoint(Vector2 world)
        {
            if (areaBounds != null)
                return areaBounds.OverlapPoint(world);

            // Fallback: project and check uv inside [0..1]
            if (!TryProjectWorldToUV(world, out Vector2 uv))
                return false;

            return uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
        }

        /// <summary>
        /// Optionally clamps u when the player is deep enough (v) inside a narrow cabin shaft.
        /// Returns true if u was modified.
        /// </summary>
        public bool TryClampUVAtDepth(ref Vector2 uv)
        {
            if (!limitLateralUWhenDeep || uv.y < lateralClampStartV)
                return false;

            float t = Mathf.InverseLerp(lateralClampStartV, 1f, uv.y);
            float uMin = Mathf.Lerp(lateralUMinAtNear, lateralUMinAtFar, t);
            float uMax = Mathf.Lerp(lateralUMaxAtNear, lateralUMaxAtFar, t);
            float before = uv.x;
            uv.x = Mathf.Clamp(uv.x, uMin, uMax);
            return Mathf.Abs(uv.x - before) > 0.0001f;
        }

        private static Vector2 Bilinear(Vector2 p00, Vector2 p10, Vector2 p01, Vector2 p11, float u, float v)
        {
            Vector2 a = Vector2.Lerp(p00, p10, u);
            Vector2 b = Vector2.Lerp(p01, p11, u);
            return Vector2.Lerp(a, b, v);
        }

        private static Vector2 dBilinear_du(Vector2 p00, Vector2 p10, Vector2 p01, Vector2 p11, float v)
        {
            // d/du [ (1-v)((1-u)p00 + u p10) + v((1-u)p01 + u p11) ]
            // = (1-v)(p10 - p00) + v(p11 - p01)
            return (1f - v) * (p10 - p00) + v * (p11 - p01);
        }

        private static Vector2 dBilinear_dv(Vector2 p00, Vector2 p10, Vector2 p01, Vector2 p11, float u)
        {
            // d/dv = ((1-u)p01 + u p11) - ((1-u)p00 + u p10)
            return Vector2.Lerp(p01, p11, u) - Vector2.Lerp(p00, p10, u);
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private void OnDrawGizmosSelected()
        {
            if (!HasValidCorners)
                return;

            Vector3 nL = nearLeft.position;
            Vector3 nR = nearRight.position;
            Vector3 fL = farLeft.position;
            Vector3 fR = farRight.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(nL, nR);
            Gizmos.DrawLine(fL, fR);
            Gizmos.DrawLine(nL, fL);
            Gizmos.DrawLine(nR, fR);

            // Simple grid preview
            Gizmos.color = new Color(0.1f, 1f, 1f, 0.6f);
            const int steps = 6;
            for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps;
                Vector3 a = MapToWorld(0f, t);
                Vector3 b = MapToWorld(1f, t);
                Gizmos.DrawLine(a, b);

                Vector3 c = MapToWorld(t, 0f);
                Vector3 d = MapToWorld(t, 1f);
                Gizmos.DrawLine(c, d);
            }
        }
    }
}

