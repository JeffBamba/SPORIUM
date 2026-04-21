namespace _Project.Sporae.Core
{
    /// <summary>
    /// Hook missione Armadio (Both). Il flag è idempotente: può essere inviato sia in apertura che in chiusura pannello.
    /// </summary>
    public static class WardrobeMission
    {
        public const string DemoWardrobeFlagKey = "demo_wardrobe";
        public const string DemoWardrobeMissionConfigName = "M_Demo_Wardrobe";

        /// <summary>Notifica avanzamento mission recap (0/100) per il task armadio.</summary>
        public static event System.Action ProgressChanged;

        private static bool _wardrobeAccessed;

        /// <summary>Segnala che il player ha interagito con il guardaroba.</summary>
        public static void NotifyWardrobeAccessed()
        {
            if (!_wardrobeAccessed)
            {
                _wardrobeAccessed = true;
                ProgressChanged?.Invoke();
            }
            ServiceContainer.Instance?.Get<MissionFlagTracker>()?.SetFlag(DemoWardrobeFlagKey);
        }

        /// <summary>Compatibilità chiamate esistenti.</summary>
        public static void NotifyWardrobeClosed() => NotifyWardrobeAccessed();

        public static bool IsDemoWardrobeConfig(MissionConfig cfg) =>
            cfg != null && string.Equals(cfg.name, DemoWardrobeMissionConfigName, System.StringComparison.Ordinal);

        /// <summary>Progress 0/1 della missione armadio nel recap.</summary>
        public static float GetObjectiveProgress01(MissionConfig cfg)
        {
            if (!IsDemoWardrobeConfig(cfg))
                return -1f;
            return _wardrobeAccessed ? 1f : 0f;
        }

        public static void RestoreProgressState(bool accessed)
        {
            _wardrobeAccessed = accessed;
            ProgressChanged?.Invoke();
        }
    }
}
