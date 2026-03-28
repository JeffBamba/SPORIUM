using System;
using UnityEngine;
using _Project.Player;
using _Project.World.VaultMap;

namespace _Project.Sporae.Core
{
    /// <summary>
    /// Servizio che traccia in quale stanza si trova il player correntemente.
    /// Registrato in ServiceContainer da GamePlayInstaller.
    /// Si iscrive a PlayerPerspectiveMover2D.OnAreaChanged e risolve il RoomAreaTag sul GO dell'area.
    /// </summary>
    public class RoomTracker : MonoBehaviour
    {
        public string CurrentRoomId { get; private set; } = string.Empty;
        public string CurrentDisplayName { get; private set; } = string.Empty;

        /// <summary>Invocato ogni volta che il player entra in una nuova stanza. Parametro = roomId.</summary>
        public event Action<string> OnRoomChanged;

        private PlayerPerspectiveMover2D _mover;

        private void Start()
        {
            _mover = FindObjectOfType<PlayerPerspectiveMover2D>();
            if (_mover != null)
                _mover.OnAreaChanged += HandleAreaChanged;
            else
                Debug.LogWarning("[RoomTracker] PlayerPerspectiveMover2D non trovato in scena.");

            ServiceContainer.Instance?.Register(this);
        }

        private void OnDestroy()
        {
            if (_mover != null)
                _mover.OnAreaChanged -= HandleAreaChanged;
            // ServiceContainer non espone Unregister; la prossima Register in scena sovrascrive se necessario.
        }

        private void HandleAreaChanged(PerspectiveWalkArea2D area)
        {
            if (area == null) return;

            var tag = area.GetComponent<RoomAreaTag>();
            if (tag == null)
            {
                // Area senza tag: non aggiornare il room ID corrente
                return;
            }

            if (tag.RoomId == CurrentRoomId) return;

            CurrentRoomId = tag.RoomId;
            CurrentDisplayName = tag.DisplayName;
            OnRoomChanged?.Invoke(CurrentRoomId);
        }
    }
}
