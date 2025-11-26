using UnityEngine;
using _Project.Sporae.Core;

namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// ScriptableObject che contiene i dati specifici di una pianta.
    /// Referenzia ItemConfig per compatibilità con sistema inventario esistente.
    /// </summary>
    [CreateAssetMenu(fileName = "PlantData", menuName = "Sporae/PlantData")]
    public class PlantData : ScriptableObject
    {
        [Header("Identificazione")]
        [Tooltip("Codice univoco pianta (es. PLT-STD-001, PLT-PURE-001, PLT-EVIL-001)")]
        [SerializeField] private string plantCode;
        
        [Tooltip("Riferimento all'ItemConfig del seme corrispondente")]
        [SerializeField] private ItemConfig seedItemConfig;
        
        [Header("Famiglia e Caratteristiche")]
        [Tooltip("Famiglia di appartenenza (Standard/Pure/Evil)")]
        [SerializeField] private PlantFamily family = PlantFamily.Standard;
        
        [Tooltip("Rarità della pianta")]
        [SerializeField] private PlantRarity rarity = PlantRarity.Common;
        
        [Header("pH System")]
        [Tooltip("Drift pH giornaliero base (es. +2 per Pure, -2 per Evil, 0 per Standard)")]
        [SerializeField] private float dailyPhDrift = 0f;
        
        [Tooltip("Range minimo pH ottimale")]
        [SerializeField] private float optimalPhMin = -29f;
        
        [Tooltip("Range massimo pH ottimale")]
        [SerializeField] private float optimalPhMax = 29f;
        
        [Header("Futuro (placeholder per feature avanzate)")]
        [Tooltip("Fazione preferita (per vendita/baratto)")]
        [SerializeField] private string preferredFaction = "";
        
        // Proprietà pubbliche
        public string PlantCode => plantCode;
        public ItemConfig SeedItemConfig => seedItemConfig;
        public PlantFamily Family => family;
        public PlantRarity Rarity => rarity;
        public float DailyPhDrift => dailyPhDrift;
        public float OptimalPhMin => optimalPhMin;
        public float OptimalPhMax => optimalPhMax;
        public string PreferredFaction => preferredFaction;
        
        /// <summary>
        /// Verifica se il pH è nella banda ottimale per questa pianta
        /// </summary>
        public bool IsPhInOptimalRange(float currentPh)
        {
            return currentPh >= optimalPhMin && currentPh <= optimalPhMax;
        }
        
        /// <summary>
        /// Restituisce il drift pH giornaliero con variazioni casuali se necessario
        /// </summary>
        public float GetDailyPhDrift()
        {
            // Per ora restituisce il valore base
            // In futuro si può aggiungere variazione casuale (±1 per Pure/Evil)
            return dailyPhDrift;
        }
        
        private void OnValidate()
        {
            // Validazione parametri
            if (string.IsNullOrEmpty(plantCode))
            {
                Debug.LogWarning($"[PlantData] {name}: PlantCode non impostato!");
            }
            
            if (seedItemConfig == null)
            {
                Debug.LogWarning($"[PlantData] {name}: SeedItemConfig non assegnato!");
            }
            
            if (seedItemConfig != null && !seedItemConfig.IsSeed)
            {
                Debug.LogWarning($"[PlantData] {name}: SeedItemConfig assegnato non è un seme (IsSeed=false)!");
            }
            
            // Validazione pH drift in base alla famiglia
            if (family == PlantFamily.Pure && dailyPhDrift <= 0)
            {
                Debug.LogWarning($"[PlantData] {name}: Pianta Pure dovrebbe avere drift pH positivo!");
            }
            
            if (family == PlantFamily.Evil && dailyPhDrift >= 0)
            {
                Debug.LogWarning($"[PlantData] {name}: Pianta Evil dovrebbe avere drift pH negativo!");
            }
            
            if (family == PlantFamily.Standard && dailyPhDrift != 0)
            {
                Debug.LogWarning($"[PlantData] {name}: Pianta Standard dovrebbe avere drift pH = 0!");
            }
            
            // Validazione range pH ottimale
            if (optimalPhMin >= optimalPhMax)
            {
                Debug.LogWarning($"[PlantData] {name}: Range pH ottimale non valido (min >= max)!");
            }
        }
    }
    
    /// <summary>
    /// Rarità della pianta (per vendita/prezzo)
    /// </summary>
    public enum PlantRarity
    {
        Common = 0,        // Comune
        Uncommon = 1,      // Non comune
        Rare = 2,          // Rara
        Epic = 3,          // Epica
        Legendary = 4     // Leggendaria
    }
}

