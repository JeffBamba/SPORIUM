namespace _Project.Sporae.Core
{
    /// <summary>
    /// Blocca input di movimento mondo (WASD / click-to-move) mentre un pannello HUD modale è aperto (es. Armadio).
    /// </summary>
    public static class GameplayUiModalLock
    {
        private static bool _manualHideFixedHud;
        private static bool _machineModalOpen;
        private static bool _machineModalKeepsFixedHudVisible;
        private static bool _inventoryContextHudVisible;
        private static bool _suppressDomeStatusHud;

        /// <summary>
        /// True mentre un modale gameplay blocca movimento/interazione mondo (macchinari HUD, armadio, VO, ecc.).
        /// Il menu in-game non deve consumare <c>Esc</c> nello stesso frame se questo valore è true.
        /// </summary>
        public static bool BlocksWorldInput { get; private set; }
        public static bool HidesFixedHud => _manualHideFixedHud || (_machineModalOpen && !_machineModalKeepsFixedHudVisible);
        public static bool HidesContextHud => _manualHideFixedHud || (_machineModalOpen && !_inventoryContextHudVisible);

        /// <summary>
        /// Nasconde solo l'HUD Dome Status (pots/cryo) senza togliere TopBar / CompactBottom — es. PlantCard4v.
        /// </summary>
        public static bool SuppressDomeStatusHud => _suppressDomeStatusHud;

        public static void SetBlockWorldInput(bool block)
        {
            BlocksWorldInput = block;
        }
        public static void SetHideFixedHud(bool hide) => _manualHideFixedHud = hide;

        /// <summary>
        /// Override per inventory browsing: mantiene visibili pannelli informativi
        /// (Player Status / Mission Recap / Notifications) durante un modal macchina.
        /// </summary>
        public static void SetInventoryContextHudVisible(bool visible)
        {
            _inventoryContextHudVisible = visible;
        }

        public static void SetSuppressDomeStatusHud(bool suppress)
        {
            _suppressDomeStatusHud = suppress;
        }

        public static void SetMachineModalState(bool isOpen, bool keepFixedHudVisible = false)
        {
            BlocksWorldInput = isOpen;
            _machineModalOpen = isOpen;
            _machineModalKeepsFixedHudVisible = isOpen && keepFixedHudVisible;
            if (!isOpen)
            {
                _inventoryContextHudVisible = false;
                _machineModalKeepsFixedHudVisible = false;
                _suppressDomeStatusHud = false;
            }
        }
    }
}
