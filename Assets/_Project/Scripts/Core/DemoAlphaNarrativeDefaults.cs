using System.Collections.Generic;
using _Project.UI.UIToolkit.VoOverlay;

namespace _Project.Sporae.Core
{
    /// <summary>Fallback quando non esiste asset <see cref="DemoAlphaNarrativeConfig"/> in Resources.</summary>
    public static class DemoAlphaNarrativeDefaults
    {
        /// <summary>Placeholder beat 1 — ambiguo, no spoiler; sostituibile da asset.</summary>
        public const string Beat1WakeLine =
            "Protocollo 01 riattivato.\n" +
            "Vault-07...\n" +
            "Operativo.\n\n" +
            "La Cupola tiene ancora......\n\n" +
            "Tu no da quello che vedo.\n" +
            "Ma... stai comunque respirando.\n\n" +
            "Buone notizie: il sistema funziona.\n" +
            "Cattive notizie: ora devi farlo anche tu.\n\n" +
            "Quindi ascolta bene, Biologo:\n" +
            "apri quell'armadio e vestiti.\n" +
            "La fine del mondo e' gia abbastanza complicata...\n" +
            "senza affrontarla in mutande. :)";

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

        public const string Beat2PostBreakfastPart1Line =
            "Molto meglio. Hai di nuovo un colorito… quasi umano.\n" +
            "Vedi quel serbatoio al centro? È la condensa della Dome: qui la ripuliamo e la chiamiamo acqua potabile.";

        public const string Beat2PostBreakfastPart2Line =
            "E la Cucina fa l'altro miracolo: fibre e cellule dentro, cibo fuori—carburante per le tue azioni.\n" +
            "Se non vuoi ritrovarti l'inventario pieno di compost, metti tutto nella Dispensa Refrigerata. Te lo ricordi, sì?";

        public const VoRegister Beat2PostBreakfastRegister = VoRegister.RegisterA;

        public const VoSentenceAdvanceMode Beat2PostBreakfastSentenceAdvance = VoSentenceAdvanceMode.ClickToContinue;

        public static IReadOnlyList<string> Beat2PostBreakfastPart1HighlightWords { get; } = new[]
        {
            "condensa",
            "acqua potabile"
        };

        public static IReadOnlyList<string> Beat2PostBreakfastPart1HighlightColorHexes { get; } = new[]
        {
            "#FFB86B",
            "#6BCBFF"
        };

        public static IReadOnlyList<string> Beat2PostBreakfastPart2HighlightWords { get; } = new[]
        {
            "cibo",
            "azioni",
            "Dispensa Refrigerata"
        };

        public static IReadOnlyList<string> Beat2PostBreakfastPart2HighlightColorHexes { get; } = new[]
        {
            "#A8FF98",
            "#E6C96F",
            "#D4A5FF"
        };

        public const string Beat3SeedStorageIntroLine =
            "Bene. Adesso puoi fare l'unica cosa che ti rende più utile di un sacco di carne: lavorare.\n" +
            "Sali al piano +1. Stanza di destra: Seed Storage.\n" +
            "Vai a controllare le promesse congelate… e prova a trovare un motivo decente per la tua esistenza mentre ci sei.";

        public const VoRegister Beat3SeedStorageIntroRegister = VoRegister.RegisterA;

        public const VoSentenceAdvanceMode Beat3SeedStorageIntroSentenceAdvance = VoSentenceAdvanceMode.ClickToContinue;

        public static IReadOnlyList<string> Beat3SeedStorageIntroHighlightWords { get; } = new[] { "Seed Storage" };

        public const string Beat3SeedStorageAnomalyPart1Line =
            "Qui congeliamo semi e spore vitali: quando il nodo resta attivo, il patrimonio botanico sopravvive ai cicli peggiori.";

        public const string Beat3SeedStorageAnomalyPart2Line =
            "ASPETTA... NO?! STORAGE OFF! SOLO RESIDUI ORGANICI!\n" +
            "CONTENUTO PERSO. Qualcuno ha spento il sistema troppo a lungo.";

        public const string Beat3SeedStorageAnomalyPowerOnRequestLine =
            "Riaccendi subito il Seed Storage. Se lo lasci morto, qui dentro resta solo scarto.";

        public const string Beat3SeedStorageCryHoverRequestLine =
            "Quello che hai visto qui dentro ha un costo, e la parte peggiore è che non ricordi nemmeno di averlo causato.\n" +
            "Chiudi il pannello e passa il mouse sul box CRY in basso a sinistra: ti faccio vedere i costi fissi di mantenimento.";

        public const string Beat3CryTooltipCostsExplainLine =
            "Quello nel box CRY e' il conto che corre ogni giorno: energia minima, supporti vitali e macchine attive.\n" +
            "Questi sono costi fissi: li paghi anche quando non produci nulla.";

        public const string Beat3CryTooltipIncomeExplainLine =
            "Per sostenere la Dome devi far entrare CRY: completa missioni, vendi ai mercanti, tratta nel black market e usa il trading per trasformare eccedenze in margine.\n" +
            "Se tieni in equilibrio entrate e mantenimento, resti operativo.";
    }
}
