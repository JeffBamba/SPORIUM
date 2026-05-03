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
            "Chiudi questo pannello adesso: qui dentro e' solo allarme e promemoria, non il posto dove rimetti ordine.";

        public const string Beat3SeedStorageCryHoverRequestLine =
            "Quello che hai visto ha un costo giornaliero in CRY: mantenimento, macchine accese, niente sconti di coscienza.\n" +
            "Ti arriva una nuova missione: «Accedi al PC». Scendi in Camera da letto, apri il terminale sul tavolo e il Pannello di controllo remoto.";

        public const string Beat3CryTooltipCostsExplainLine =
            "Ogni riga e' un contratto semplice: acceso paghi CRY e ottieni la funzione; spento risparmi e rinunci al servizio.\n" +
            "Il totale in basso somma solo cio' che lasci in marcia: meccanica sporca, ma chiara.";

        public const string Beat3CryTooltipIncomeExplainLine =
            "Nuova missione: «Accendi il Seed Storage». Dalla riga del deposito porta ACCESO se serve, poi esci dal PC: la routine si chiude quando lo schermo torna buio.";
    }
}
