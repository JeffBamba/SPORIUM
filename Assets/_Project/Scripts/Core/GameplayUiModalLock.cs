namespace _Project.Sporae.Core
{
    /// <summary>
    /// Blocca input di movimento mondo (WASD / click-to-move) mentre un pannello HUD modale è aperto (es. Armadio).
    /// </summary>
    public static class GameplayUiModalLock
    {
        private static bool _manualHideFixedHud;
        private static bool _machineModalOpen;
        private static bool _inventoryContextHudVisible;

        public static bool BlocksWorldInput { get; private set; }
        public static bool HidesFixedHud => _manualHideFixedHud || _machineModalOpen;
        public static bool HidesContextHud => _manualHideFixedHud || (_machineModalOpen && !_inventoryContextHudVisible);

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

        public static void SetMachineModalState(bool isOpen)
        {
            BlocksWorldInput = isOpen;
            _machineModalOpen = isOpen;
            if (!isOpen)
                _inventoryContextHudVisible = false;
        }
    }
}
