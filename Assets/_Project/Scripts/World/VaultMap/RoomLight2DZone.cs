using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace _Project.World.VaultMap
{
    /// <summary>
    /// Room trigger that enables/disables a set of Light2D when the player enters/exits.
    /// Intended for VaultMap "per-room lighting" to enhance immersion (player lit + shadows).
    /// </summary>
    [DisallowMultipleComponent]
    public class RoomLight2DZone : MonoBehaviour
    {
        [Header("Trigger")]
        [Tooltip("Optional: if null, will use the first Collider2D on this GameObject.")]
        [SerializeField] private Collider2D triggerCollider;

        [Header("Lights")]
        [Tooltip("Lights to enable when the player is inside this zone.")]
        [SerializeField] private List<Light2D> lights = new();

        [Tooltip("If true, auto-collect Light2D from children on Awake (adds to the list).")]
        [SerializeField] private bool autoCollectLightsFromChildren = false;

        [Header("Behavior")]
        [SerializeField] private bool enableOnEnter = true;
        [SerializeField] private bool disableOnExit = true;

        [Tooltip("If true, will start with all referenced lights disabled (useful if you rely on enter triggers).")]
        [SerializeField] private bool disableLightsOnStart = true;

        [Header("Filtering")]
        [Tooltip("Only colliders with this tag will trigger. Leave empty to accept any tag.")]
        [SerializeField] private string requiredTag = "Player";

        [Tooltip("Optional additional layer filtering (if 0, ignored).")]
        [SerializeField] private LayerMask requiredLayerMask = 0;

        private int _insideCount = 0;

        private void Reset()
        {
            triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider != null)
                triggerCollider.isTrigger = true;
        }

        private void Awake()
        {
            if (triggerCollider == null)
                triggerCollider = GetComponent<Collider2D>();

            if (autoCollectLightsFromChildren)
            {
                Light2D[] found = GetComponentsInChildren<Light2D>(includeInactive: true);
                for (int i = 0; i < found.Length; i++)
                {
                    if (found[i] != null && !lights.Contains(found[i]))
                        lights.Add(found[i]);
                }
            }
        }

        private void Start()
        {
            if (disableLightsOnStart)
                SetLightsEnabled(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!PassesFilter(other))
                return;

            _insideCount++;
            if (_insideCount == 1 && enableOnEnter)
                SetLightsEnabled(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!PassesFilter(other))
                return;

            _insideCount = Mathf.Max(0, _insideCount - 1);
            if (_insideCount == 0 && disableOnExit)
                SetLightsEnabled(false);
        }

        private bool PassesFilter(Collider2D other)
        {
            if (other == null)
                return false;

            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
                return false;

            if (requiredLayerMask.value != 0)
            {
                int otherMask = 1 << other.gameObject.layer;
                if ((requiredLayerMask.value & otherMask) == 0)
                    return false;
            }

            return true;
        }

        public void SetLightsEnabled(bool enabled)
        {
            for (int i = 0; i < lights.Count; i++)
            {
                Light2D l = lights[i];
                if (l != null)
                    l.enabled = enabled;
            }
        }
    }
}

