using System.Collections.Generic;
using UnityEngine;
using _Project.UI.UIToolkit.VoOverlay;

namespace _Project.Sporae.Core
{
    /// <summary>
    /// Testi e parametri VO per la demo Alpha (beat 1+). Opzionale: se assente in Resources, si usano <see cref="DemoAlphaNarrativeDefaults"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "DemoAlphaNarrativeConfig", menuName = "Sporae/Demo/Alpha Narrative Config")]
    public sealed class DemoAlphaNarrativeConfig : ScriptableObject
    {
        [TextArea(2, 8)]
        [Tooltip("Prima riga VO del beat 1 (Wake / routine). Allineare alle Narrative Red Lines.")]
        public string Beat1WakeLine;

        [Tooltip("Registro colore per la riga beat 1.")]
        public VoRegister Beat1WakeRegister = VoRegister.RegisterA;

        [Tooltip("Tra una frase e l’altra: pausa lettura automatica oppure input (consigliato per testi impersonali / missione, registro A).")]
        public VoSentenceAdvanceMode Beat1WakeSentenceAdvance = VoSentenceAdvanceMode.ClickToContinue;

        [Header("Highlight parole missione Beat 1 (VO)")]
        [Tooltip("Parole o frasi da evidenziare nel VO beat 1 (case-insensitive). Esempio: \"apri l'armadio e vestiti\".")]
        public List<string> Beat1MissionHighlightWords = new List<string>();

        // ─── Beat 2 — Cucina / Colazione ───────────────────────────────────

        [Header("Beat 2 — Cucina (Fai Colazione)")]
        [TextArea(2, 8)]
        [Tooltip("Testo VO del beat 2 (trigger cucina). Lasciare vuoto per usare il default.")]
        public string Beat2KitchenLine;

        [Tooltip("Registro colore per il VO beat 2.")]
        public VoRegister Beat2KitchenRegister = VoRegister.RegisterA;

        [Tooltip("Modalità avanzamento frasi per il VO beat 2.")]
        public VoSentenceAdvanceMode Beat2KitchenSentenceAdvance = VoSentenceAdvanceMode.ClickToContinue;

        [Tooltip("Parole o frasi da evidenziare nel VO beat 2. Esempio: \"Apri l'Inventario\", \"mangia\", \"bevi\".")]
        public List<string> Beat2MissionHighlightWords = new List<string>();

        [Header("Beat 2 — VO dopo missione «Fai Colazione» (due blocchi: click tra blocco 1 e 2)")]
        [TextArea(3, 8)]
        [Tooltip("Primo blocco VO post-colazione. Vuoto = default codice.")]
        public string Beat2PostBreakfastPart1Line;

        [TextArea(3, 8)]
        [Tooltip("Secondo blocco VO post-colazione (dopo click «continua» sul primo). Vuoto = default codice.")]
        public string Beat2PostBreakfastPart2Line;

        [Tooltip("Registro colore per i VO post-colazione.")]
        public VoRegister Beat2PostBreakfastRegister = VoRegister.RegisterA;

        [Tooltip("Modalità avanzamento frasi interne a ogni blocco (consigliato: blocco unico senza split frasi).")]
        public VoSentenceAdvanceMode Beat2PostBreakfastSentenceAdvance = VoSentenceAdvanceMode.ClickToContinue;

        [Tooltip("Highlight blocco 1. Ordine allineato a Beat2PostBreakfastPart1HighlightColorHexes.")]
        public List<string> Beat2PostBreakfastPart1HighlightWords = new List<string>();

        [Tooltip("Un #RRGGBB per voce di Part1 (stesso numero di righe).")]
        public List<string> Beat2PostBreakfastPart1HighlightColorHexes = new List<string>();

        [Tooltip("Highlight blocco 2. Ordine allineato a Beat2PostBreakfastPart2HighlightColorHexes.")]
        public List<string> Beat2PostBreakfastPart2HighlightWords = new List<string>();

        [Tooltip("Un #RRGGBB per voce di Part2 (stesso numero di righe).")]
        public List<string> Beat2PostBreakfastPart2HighlightColorHexes = new List<string>();

        [Header("Beat 3 — Avvio Seed Storage (dopo VO post-colazione)")]
        [TextArea(3, 12)]
        [Tooltip("VO che lancia il beat 3 (direzioni verso Seed Storage). Vuoto = default codice.")]
        public string Beat3SeedStorageIntroLine;

        [Tooltip("Registro colore per il VO beat 3 intro.")]
        public VoRegister Beat3SeedStorageIntroRegister = VoRegister.RegisterA;

        [Tooltip("Modalità avanzamento frasi beat 3 intro.")]
        public VoSentenceAdvanceMode Beat3SeedStorageIntroSentenceAdvance = VoSentenceAdvanceMode.ClickToContinue;

        [Tooltip("Opzionale: highlight nel VO beat 3 (es. «Seed Storage»).")]
        public List<string> Beat3SeedStorageIntroHighlightWords = new List<string>();

        [Header("Beat 3 — Seed Storage anomaly (panel aperto, autoplay)")]
        [TextArea(3, 12)]
        [Tooltip("Primo blocco VO all'apertura panel: tono sicuro, introduzione Seed Storage.")]
        public string Beat3SeedStorageAnomalyPart1Line;

        [TextArea(3, 12)]
        [Tooltip("Secondo blocco VO: switch sorpresa/incredulita su storage OFF e contenuto perduto.")]
        public string Beat3SeedStorageAnomalyPart2Line;

        [TextArea(2, 6)]
        [Tooltip("Terzo blocco VO con panel Seed Storage aperto: invito a chiudere (dettagli dopo la chiusura).")]
        public string Beat3SeedStorageAnomalyPowerOnRequestLine;

        [TextArea(3, 10)]
        [Tooltip("VO subito dopo chiusura panel Seed Storage: importanza CRY + nuova missione «Accedi al PC».")]
        public string Beat3SeedStorageCryHoverRequestLine;

        [TextArea(3, 10)]
        [Tooltip("VO con pannello di controllo aperto: pro/contro accendere o spegnere i macchinari.")]
        public string Beat3CryTooltipCostsExplainLine;

        [TextArea(3, 10)]
        [Tooltip("VO sul PC dopo: nuova missione «Accendi il Seed Storage», ACCESO dal pannello, poi esci dal PC.")]
        public string Beat3CryTooltipIncomeExplainLine;

        // ─── Colore highlight condiviso ─────────────────────────────────────

        [Header("Highlight — colore condiviso")]
        [Tooltip("Colore esadecimale (#RRGGBB) delle parole evidenziate in tutti i beat VO.")]
        public string MissionHighlightColorHex = "#E6C96F";
    }
}
