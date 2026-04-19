namespace _Project.Sporae.Core
{
    /// <summary>
    /// Hook missione Armadio (Both). Il flag è idempotente: può essere inviato sia in apertura che in chiusura pannello.
    /// </summary>
    public static class WardrobeMission
    {
        public const string DemoWardrobeFlagKey = "demo_wardrobe";

        /// <summary>Segnala che il player ha interagito con il guardaroba.</summary>
        public static void NotifyWardrobeAccessed()
        {
            ServiceContainer.Instance?.Get<MissionFlagTracker>()?.SetFlag(DemoWardrobeFlagKey);
        }

        /// <summary>Compatibilità chiamate esistenti.</summary>
        public static void NotifyWardrobeClosed() => NotifyWardrobeAccessed();
    }
}
