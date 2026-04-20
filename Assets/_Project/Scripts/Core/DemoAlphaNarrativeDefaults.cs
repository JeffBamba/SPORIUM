using System.Collections.Generic;
using _Project.UI.UIToolkit.VoOverlay;

namespace _Project.Sporae.Core
{
    /// <summary>Fallback quando non esiste asset <see cref="DemoAlphaNarrativeConfig"/> in Resources.</summary>
    public static class DemoAlphaNarrativeDefaults
    {
        /// <summary>Placeholder beat 1 — ambiguo, no spoiler; sostituibile da asset.</summary>
        public const string Beat1WakeLine =
            "Feed ambiente… sincronizzazione mattina. Chi parla non è una sola voce. Muoviti quando vuoi.";

        public const VoRegister Beat1WakeRegister = VoRegister.RegisterA;

        public const VoSentenceAdvanceMode Beat1WakeSentenceAdvance = VoSentenceAdvanceMode.ClickToContinue;

        public const string MissionHighlightColorHex = "#E6C96F";

        public static IReadOnlyList<string> Beat1MissionHighlightWords { get; } = new List<string>();

        // Beat 2 — Cucina
        public const string Beat2KitchenLine =
            "Questa è la Cucina: qui trasformi scorte e acqua potabile in energia per affrontare la giornata.\n" +
            "Apri l'Inventario, mangia un item e poi bevi un po' di acqua.";

        public const VoRegister Beat2KitchenRegister = VoRegister.RegisterA;

        public const VoSentenceAdvanceMode Beat2KitchenSentenceAdvance = VoSentenceAdvanceMode.ClickToContinue;

        public static IReadOnlyList<string> Beat2MissionHighlightWords { get; } = new List<string>
        {
            "Apri l'Inventario", "mangia", "bevi"
        };
    }
}
