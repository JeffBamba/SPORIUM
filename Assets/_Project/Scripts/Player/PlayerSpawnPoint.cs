using UnityEngine;

namespace _Project.Player
{
    /// <summary>
    /// Place this GameObject in the scene to define where the player should spawn (e.g. after EndDay).
    /// You can freely move it in the editor to change the spawn location.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerSpawnPoint : MonoBehaviour
    {
        [Header("Selection")]
        [Tooltip("If multiple spawn points exist, the system prefers active ones. If none active, it uses the highest priority.")]
        [SerializeField] private bool isActive = true;

        [Tooltip("Higher priority wins when multiple spawn points are valid.")]
        [SerializeField] private int priority = 0;

        [Header("Elevator (optional)")]
        [Tooltip("If true, EndDay will also set the elevator level when spawning.")]
        [SerializeField] private bool setElevatorLevelOnSpawn = true;

        [SerializeField] private int elevatorLevel = 0;

        public bool IsActive => isActive;
        public int Priority => priority;
        public bool SetElevatorLevelOnSpawn => setElevatorLevelOnSpawn;
        public int ElevatorLevel => elevatorLevel;

        public static PlayerSpawnPoint FindBest()
        {
            // includeInactive = true so the spawn can be disabled for iteration without breaking references
            PlayerSpawnPoint[] all = Object.FindObjectsOfType<PlayerSpawnPoint>(includeInactive: true);
            if (all == null || all.Length == 0)
                return null;

            PlayerSpawnPoint best = null;
            for (int i = 0; i < all.Length; i++)
            {
                PlayerSpawnPoint sp = all[i];
                if (sp == null)
                    continue;

                if (best == null)
                {
                    best = sp;
                    continue;
                }

                // Prefer active over inactive
                if (sp.isActive && !best.isActive)
                {
                    best = sp;
                    continue;
                }
                if (!sp.isActive && best.isActive)
                    continue;

                // Same active state: higher priority wins
                if (sp.priority > best.priority)
                    best = sp;
            }

            return best;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = isActive ? new Color(0.1f, 1f, 0.1f, 0.9f) : new Color(1f, 0.6f, 0.1f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, 0.25f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.6f);
        }
#endif
    }
}

