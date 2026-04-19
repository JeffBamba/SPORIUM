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

        [Header("Highlight parole missione (VO)")]
        [Tooltip("Elenco di parole o frasi da evidenziare nel testo VO del beat 1 (case-insensitive, match letterale). Esempio: \"Ora vestiti\", \"cambia outfit\".")]
        public List<string> Beat1MissionHighlightWords = new List<string>();

        [Tooltip("Colore esadecimale (#RRGGBB) delle parole evidenziate. Default: giallo ambra stile HUD missioni.")]
        public string MissionHighlightColorHex = "#E6C96F";
    }
}
