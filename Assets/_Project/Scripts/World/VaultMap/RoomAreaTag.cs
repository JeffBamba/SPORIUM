using UnityEngine;

namespace _Project.World.VaultMap
{
    /// <summary>
    /// Aggiunge metadati di stanza a un PerspectiveWalkArea2D GO.
    /// Usato da RoomTracker per mappare l'area corrente al room ID della CompactBottomBar.
    /// </summary>
    public class RoomAreaTag : MonoBehaviour
    {
        [Tooltip("ID stanza — deve corrispondere agli ID usati in CompactBottomBarController: dome, lab, kitchen, dormitory, visitor, storage, restricted1, restricted2")]
        public string RoomId;

        [Tooltip("Nome visualizzato nel tooltip della room icon (es. DOME)")]
        public string DisplayName;

        [Tooltip("Piano mostrato sotto il nome (es. Floor -1)")]
        public string FloorName;

        [Tooltip("Testo narrativo/informativo del tooltip")]
        [TextArea(2, 4)]
        public string TooltipText;

        [Tooltip("Se true, la room icon appare nello stato room-locked")]
        public bool IsLocked;
    }
}
