using UnityEngine;
using _Project.Sporae.Core;

namespace _Project.Player
{
    /// <summary>
    /// Prevents double-movement in scenes where multiple mover components exist.
    /// In VaultMap we want PlayerPerspectiveMover2D to be the active one, so we suspend PlayerClickMover2D.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerMoverRouter2D : MonoBehaviour
    {
        [SerializeField] private PlayerPerspectiveMover2D perspectiveMover;
        [SerializeField] private PlayerClickMover2D clickMover;

        private void Awake()
        {
            RegisterInServiceContainer();

            if (perspectiveMover == null)
                perspectiveMover = GetComponent<PlayerPerspectiveMover2D>();

            if (clickMover == null)
                clickMover = GetComponent<PlayerClickMover2D>();
        }

        private void RegisterInServiceContainer()
        {
            if (ServiceContainer.Instance == null)
                return;

            ServiceContainer.Instance.Register(this);
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnDisable()
        {
            // Restore default mover if we disable the router
            if (clickMover != null)
                clickMover.SuspendMovement(false);
        }

        private void Update()
        {
            // Keep in sync if user toggles components at runtime
            Apply();
        }

        private void Apply()
        {
            if (clickMover == null)
                return;

            bool wantPerspective = perspectiveMover != null && perspectiveMover.enabled && perspectiveMover.gameObject.activeInHierarchy;
            clickMover.SuspendMovement(wantPerspective);
        }
    }
}

