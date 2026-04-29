using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace _Project.World.VaultMap
{
    /// <summary>
    /// Room trigger that enables/disables a set of Light2D when the player enters/exits.
    /// Intended for VaultMap "per-room lighting" to enhance immersion (player lit + shadows).
    /// Several <see cref="RoomLight2DZone"/> may reference the same <see cref="Light2D"/>
    /// (overlapping floor strips or copy-paste in the scene). A shared per-light reference count
    /// ensures that exiting one zone does not turn off the light while the player is still
    /// inside another zone that uses the same lamp.
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

        private static readonly Dictionary<int, int> s_ReferenceCountPerLightInstanceId = new();

        private static int GetSharedRefCount(Light2D light)
        {
            if (light == null)
                return 0;

            int id = light.GetInstanceID();
            return s_ReferenceCountPerLightInstanceId.TryGetValue(id, out int c) ? c : 0;
        }

        private static void AddReference(Light2D light, int delta)
        {
            if (light == null)
                return;

            int id = light.GetInstanceID();
            int next = Mathf.Max(0, GetSharedRefCount(light) + delta);

            if (next == 0)
            {
                s_ReferenceCountPerLightInstanceId.Remove(id);
                light.enabled = false;
            }
            else
            {
                s_ReferenceCountPerLightInstanceId[id] = next;
                light.enabled = true;
            }
        }

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
            if (!disableLightsOnStart || _insideCount != 0)
                return;

            foreach (Light2D light in lights)
            {
                if (light == null)
                    continue;

                // Do not darken a lamp already claimed by enter-triggers fired before Start (spawn inside zone).
                if (GetSharedRefCount(light) > 0)
                    continue;

                light.enabled = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!PassesFilter(other))
                return;

            _insideCount++;
            if (_insideCount != 1 || !enableOnEnter)
                return;

            foreach (Light2D light in lights)
            {
                if (light != null)
                    AddReference(light, 1);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!PassesFilter(other))
                return;

            _insideCount = Mathf.Max(0, _insideCount - 1);
            if (_insideCount != 0 || !disableOnExit)
                return;

            foreach (Light2D light in lights)
            {
                if (light != null)
                    AddReference(light, -1);
            }
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

        /// <summary>
        /// Direct override — does not use shared reference counting. Prefer triggers for authored lights.
        /// </summary>
        public void SetLightsEnabled(bool enabled)
        {
            for (int i = 0; i < lights.Count; i++)
            {
                Light2D light = lights[i];
                if (light == null)
                    continue;

                int id = light.GetInstanceID();
                if (!enabled)
                    s_ReferenceCountPerLightInstanceId.Remove(id);

                light.enabled = enabled;
            }
        }

        private void OnDestroy()
        {
            if (_insideCount <= 0)
                return;

            for (int t = 0; t < _insideCount; t++)
            {
                foreach (Light2D light in lights)
                {
                    if (light != null)
                        AddReference(light, -1);
                }
            }

            _insideCount = 0;
        }
    }
}
