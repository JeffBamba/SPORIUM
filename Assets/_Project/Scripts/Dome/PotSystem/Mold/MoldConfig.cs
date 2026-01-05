using UnityEngine;

namespace Sporae.Dome.PotSystem.Mold
{
    /// <summary>
    /// Configurazione per il sistema muffe (BLK-07.01).
    /// Contiene soglie rischio e fattori di calcolo.
    /// </summary>
    [CreateAssetMenu(menuName = "Sporae/MoldConfig", fileName = "MoldConfig")]
    public class MoldConfig : ScriptableObject
    {
        [Header("Soglie Rischio")]
        [Tooltip("Soglia rischio Mild (≥1)")]
        public int mildRiskThreshold = 1;
        
        [Tooltip("Soglia rischio Severe (≥2)")]
        public int severeRiskThreshold = 2;
        
        [Tooltip("Soglia rischio Critical (≥3)")]
        public int criticalRiskThreshold = 3;
        
        [Header("Fattori Rischio")]
        [Tooltip("Giorni consecutivi overwatering prima che inizi il rischio muffe. Dopo questa soglia, ogni giorno aggiuntivo aumenta il livello di 1 (es. soglia 3: 4 giorni = Level 1, 5 giorni = Level 2, 6 giorni = Level 3)")]
        public int overwateringDaysThreshold = 3;
        
        [Tooltip("Valore pH acido per +1 rischio (≤-20) - DEPRECATO: non più usato nel calcolo")]
        public float acidicPhThreshold = -20f;
        
        [Tooltip("Accumulo rischio per giorno senza potatura (es. 0.5 per giorno) - DEPRECATO: non più usato nel calcolo")]
        public float pruningNeglectAccumulation = 0.5f;
        
        [Header("Effetti Infestazione")]
        [Tooltip("Riduzione score per infestazione Mild")]
        public int mildScorePenalty = 10;
        
        [Tooltip("Riduzione score per infestazione Severe")]
        public int severeScorePenalty = 30;
        
        [Tooltip("Riduzione livelli per infestazione Mild")]
        public int mildLevelReduction = 1;
        
        [Tooltip("Riduzione livelli per infestazione Severe")]
        public int severeLevelReduction = 3;
    }
}

