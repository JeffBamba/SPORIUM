namespace _Project.Sporae.Core
{
    /// <summary>
    /// Hook missione Armadio (Both). Chiamare quando il player CHIUDE il guardaroba — evita di
    /// accavallare il completamento mentre la HUD è aperta.
    /// </summary>
    public static class WardrobeMission
    {
        public const string DemoWardrobeFlagKey = "demo_wardrobe";

        /// <summary>Segnala che la sessione guardaroba è terminata (pannello chiuso).</summary>
        public static void NotifyWardrobeClosed()
        {
            ServiceContainer.Instance?.Get<MissionFlagTracker>()?.SetFlag(DemoWardrobeFlagKey);
        }
    }
}
