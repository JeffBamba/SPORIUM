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

        // ─── Colore highlight condiviso ─────────────────────────────────────

        [Header("Highlight — colore condiviso")]
        [Tooltip("Colore esadecimale (#RRGGBB) delle parole evidenziate in tutti i beat VO.")]
        public string MissionHighlightColorHex = "#E6C96F";
    }
}
