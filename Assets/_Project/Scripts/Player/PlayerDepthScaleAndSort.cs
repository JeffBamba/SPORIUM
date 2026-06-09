using _Project.World.VaultMap;
using UnityEngine;

namespace _Project.Player
{
    [DisallowMultipleComponent]
    public class PlayerDepthScaleAndSort : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("If null, the script will try to find a SpriteRenderer in children (e.g., child named 'Sprite').")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Scale (Near -> Far)")]
        [SerializeField] private float scaleNear = 1.0f;
        [SerializeField] private float scaleFar = 0.8f;

        [Header("Sorting (Near -> Far)")]
        [SerializeField] private int baseOrder = 0;
        [SerializeField] private int range = 50;

        [Header("Depth Source")]
        [Tooltip("If set, reads v from this mover; otherwise tries to infer from local scale factor (less reliable).")]
        [SerializeField] private PlayerPerspectiveMover2D mover;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (mover == null)
                mover = GetComponent<PlayerPerspectiveMover2D>();
        }

        private void LateUpdate()
        {
            float v = TryGetDepthV(out float depthV) ? depthV : 0f;

            float s = Mathf.Lerp(scaleNear, scaleFar, Mathf.Clamp01(v));
            transform.localScale = new Vector3(s, s, 1f);

            if (spriteRenderer != null)
            {
                int order = baseOrder + Mathf.RoundToInt(Mathf.Clamp01(v) * range);
                int minOrder = PerspectiveWalkArea2D.GetMaxMinPlayerSortingOrderAt(transform.position);
                PerspectiveWalkArea2D walkArea = mover != null ? mover.CurrentWalkArea : null;
                if (walkArea != null && walkArea.MinPlayerSortingOrder > minOrder)
                    minOrder = walkArea.MinPlayerSortingOrder;
                if (minOrder > 0)
                    order = Mathf.Max(order, minOrder);

                spriteRenderer.sortingOrder = order;
            }
        }

        private bool TryGetDepthV(out float v)
        {
            v = 0f;
            if (mover == null)
                return false;

            v = mover.CurrentV;
            return true;
        }
    }
}
