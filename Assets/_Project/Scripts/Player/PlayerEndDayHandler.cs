using _Project.Sporae.Core;
using _Project.Player;
using UnityEngine;

namespace _Project
{
    public class PlayerEndDayHandler : MonoBehaviour
    {
        [Header("Spawn")]
        [Tooltip("If true, the player will also be moved to the spawn point on scene start (first Play).")]
        [SerializeField] private bool applySpawnOnStart = true;

        [Tooltip("If true, logs debug info about which spawn point is used.")]
        [SerializeField] private bool showDebugLogs = false;

        private DayCycleSystem _dayCycleSystem;
        private ElevatorSystem _elevatorSystem;
        private GameManager _gameManager;
        private Vector3 _fallbackStartPosition;
        private _Project.Player.PlayerPerspectiveMover2D _perspectiveMover;

        private void Awake()
        {
            _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
            _elevatorSystem = FindObjectOfType<ElevatorSystem>();
            _gameManager = FindObjectOfType<GameManager>();
            _perspectiveMover = GetComponent<_Project.Player.PlayerPerspectiveMover2D>();
            
            _fallbackStartPosition = transform.position;
        }

        private void Start()
        {
            if (_dayCycleSystem != null)
                _dayCycleSystem.OnDayChanged += HandleDayChanged;

            if (applySpawnOnStart)
                ApplySpawnNow();
        }

        private void OnDestroy()
        {
            if (_dayCycleSystem != null)
                _dayCycleSystem.OnDayChanged -= HandleDayChanged;
        }

        private void HandleDayChanged(int obj)
        {
            ApplySpawnNow();
        }

        private void ApplySpawnNow()
        {
            // Re-find every time to support moving/adding/changing spawn points without requiring a code reload.
            PlayerSpawnPoint spawnPoint = PlayerSpawnPoint.FindBest();

            Vector3 targetPos = spawnPoint != null ? spawnPoint.transform.position : _fallbackStartPosition;

            int targetLevel = 0;
            bool shouldSetLevel = true;
            if (spawnPoint != null)
            {
                shouldSetLevel = spawnPoint.SetElevatorLevelOnSpawn;
                targetLevel = spawnPoint.ElevatorLevel;
            }

            // DEBUG_SAFE_FIX: Never block spawning if elevator setup is broken.
            if (_elevatorSystem != null && shouldSetLevel)
            {
                try
                {
                    _elevatorSystem.SetLevel(targetLevel);
                }
                catch (System.Exception ex)
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"[PlayerEndDayHandler] ElevatorSystem.SetLevel failed: {ex.GetType().Name}: {ex.Message}", this);
                }
            }

            // Use mover-aware teleport to avoid snapping back in FixedUpdate.
            if (_perspectiveMover != null && _perspectiveMover.enabled)
            {
                _perspectiveMover.TeleportToWorld(targetPos, pickAreaByPoint: true);
            }
            else
            {
                transform.position = targetPos;
            }

            if (showDebugLogs)
            {
                string spName = spawnPoint != null ? spawnPoint.gameObject.name : "<fallbackStartPosition>";
                Debug.Log($"[PlayerEndDayHandler] Spawn applied. SpawnPoint={spName}, pos={targetPos}", this);
            }
        }
    }
}