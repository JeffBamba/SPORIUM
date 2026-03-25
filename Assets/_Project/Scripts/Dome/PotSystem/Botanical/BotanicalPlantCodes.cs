namespace Sporae.Dome.PotSystem.Botanical
{
    /// <summary>Codici specie Task 4 (GDD / asset Resources/Plants).</summary>
    public static class BotanicalPlantCodes
    {
        public const string FerricFern = "PLT-STD-001";
        public const string ArcticHask = "PLT-PURE-001";
        public const string GlasscapFungus = "PLT-EVIL-001";

        public static bool IsFerricFern(string code) =>
            !string.IsNullOrEmpty(code) &&
            string.Equals(code, FerricFern, System.StringComparison.OrdinalIgnoreCase);

        public static bool IsArcticHask(string code) =>
            !string.IsNullOrEmpty(code) &&
            string.Equals(code, ArcticHask, System.StringComparison.OrdinalIgnoreCase);

        public static bool IsGlasscap(string code) =>
            !string.IsNullOrEmpty(code) &&
            string.Equals(code, GlasscapFungus, System.StringComparison.OrdinalIgnoreCase);

        /// <summary>Nome specie per UI tooltip/HUD (non il nome file asset PLT-xxx).</summary>
        public static string GetSpeciesUiDisplayName(string plantCode)
        {
            if (string.IsNullOrEmpty(plantCode))
                return null;
            if (IsFerricFern(plantCode))
                return "Ferric Fern";
            if (IsArcticHask(plantCode))
                return "Arctic Hask";
            if (IsGlasscap(plantCode))
                return "Glasscap Fungus";
            return null;
        }
    }
}
