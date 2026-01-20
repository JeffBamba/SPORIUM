using UnityEngine;

namespace _Project
{
    /// <summary>
    /// Configurazione sistema condensazione.
    /// Sistema basato su calcolo dinamico (piante/stage/salute) invece di accumulo fisso.
    /// </summary>
    [CreateAssetMenu(menuName = "Sporae/CondensationConfig")]
    public class CondensationConfig : ScriptableObject
    {
        [Header("Collection Cap")]
        [Tooltip("Cap base di raccolta WAT-RAW per giorno (default: 8)")]
        [SerializeField] private int _baseCap = 8;
        
        [Tooltip("Cap con upgrade Bacino di Raccolta esteso (default: 12)")]
        [SerializeField] private int _extendedBasinCap = 12;
        
        [Header("Plant Contributions")]
        [Tooltip("Contributo base per pianta sana/rigogliosa (default: 2)")]
        [SerializeField] private float _baseContributionSana = 2f;
        
        [Tooltip("Contributo base per pianta stressata/appassita (default: 1)")]
        [SerializeField] private float _baseContributionStressata = 1f;
        
        [Header("LED Bonus")]
        [Tooltip("Bonus WAT-RAW se almeno un LED è attivo (default: 2)")]
        [SerializeField] private float _ledBonus = 2f;
        
        // Proprietà pubbliche per accesso
        public int BaseCap => _baseCap;
        public int ExtendedBasinCap => _extendedBasinCap;
        public float BaseContributionSana => _baseContributionSana;
        public float BaseContributionStressata => _baseContributionStressata;
        public float LedBonus => _ledBonus;
    }
}