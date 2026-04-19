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
    }
}
