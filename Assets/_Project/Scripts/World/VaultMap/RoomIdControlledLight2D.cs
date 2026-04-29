using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using _Project.Player;
using _Project.Sporae.Core;

namespace _Project.World.VaultMap
{
    /// <summary>
    /// Abilita una <see cref="Light2D"/> solo mentre l'area di cammino corrente del player (<see cref="PlayerPerspectiveMover2D"/>)
    /// ha lo stesso <see cref="RoomAreaTag.RoomId"/> (stessa fonte di verità dell'HUD / <see cref="RoomTracker"/>).
    /// Evita mismatch con collider delle <see cref="RoomLight2DZone"/> disallineati dalle PerspectiveWalkArea2D.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomIdControlledLight2D : MonoBehaviour
    {
        [SerializeField] private Light2D _targetLight;
        [Tooltip("Must match RoomAreaTag.RoomId on the PerspectiveWalkArea2D that should light this lamp (e.g. bed).")]
        [SerializeField] private string _roomIdWhenLit = "bed";

        private PlayerPerspectiveMover2D _mover;

        private void Awake()
        {
            if (_targetLight == null)
                _targetLight = GetComponent<Light2D>();
            // Evita un frame flash con stato serializzato "on" mentre la stanza reale viene risolta dopo il primo FixedUpdate.
            if (_targetLight != null && Application.isPlaying)
                _targetLight.enabled = false;
        }

        private IEnumerator Start()
        {
            _mover = ServiceContainer.Instance != null
                ? ServiceContainer.Instance.Get<PlayerPerspectiveMover2D>(suppressWarning: true)
                : null;

            if (_mover == null)
                _mover = FindObjectOfType<PlayerPerspectiveMover2D>();

            if (_mover != null)
                _mover.OnAreaChanged += OnWalkAreaChanged;

            yield return null;

            ApplyWalkArea(_mover != null ? _mover.CurrentWalkArea : null);
        }

        private void OnDestroy()
        {
            if (_mover != null)
                _mover.OnAreaChanged -= OnWalkAreaChanged;
        }

        private void OnWalkAreaChanged(PerspectiveWalkArea2D area)
        {
            ApplyWalkArea(area);
        }

        private void ApplyWalkArea(PerspectiveWalkArea2D area)
        {
            if (_targetLight == null)
                return;

            bool lit = false;
            if (area != null)
            {
                var tag = area.GetComponent<RoomAreaTag>();
                if (tag != null && tag.RoomId == _roomIdWhenLit)
                    lit = true;
            }

            _targetLight.enabled = lit;
        }
    }
}
