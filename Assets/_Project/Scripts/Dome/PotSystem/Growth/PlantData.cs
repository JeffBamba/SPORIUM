using UnityEngine;
using _Project.Sporae.Core;
using Sporae.DevTools;

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

        [Tooltip("Descrizione one-liner della pianta (per UI e tooltip)")]
        [SerializeField, TextArea(2, 4)] private string description;

        [Tooltip("Potere attivo della pianta (effetto speciale quando attiva)")]
        [SerializeField, TextArea(2, 4)] private string activePower;
        
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
        
        [Header("Stage Requirements (BLK-02.05)")]
        [Tooltip("Requisiti di crescita per ogni stadio (Seed, Sprout, Growth, Flowering, HarvestReady, Resting)")]
        [SerializeField] private StageRequirements[] stageRequirements = new StageRequirements[0];

        [Header("Visuals (POT System)")]
        [Tooltip("Set visual per specie (Adult/Flowering/FruitOverlay). Seed/Sprout/Empty/Morta sono gestiti da config shared.")]
        [SerializeField] private PlantVisualSet visualSet;
        
        [Header("GDD 42 - Genetic type default")]
        [Tooltip("Tipo genetico di default per questa pianta (usato a PlantSeed; metadata frutto a harvest)")]
        [SerializeField] private GeneticType defaultGeneticType = GeneticType.Stable;

        [Header("Futuro (placeholder per feature avanzate)")]
        [Tooltip("Fazione preferita (per vendita/baratto)")]
        [SerializeField] private string preferredFaction = "";
        
        // Proprietà pubbliche
        public string PlantCode => plantCode;
        public GeneticType DefaultGeneticType => defaultGeneticType;
        public ItemConfig SeedItemConfig => seedItemConfig;
        public PlantFamily Family => family;
        public PlantRarity Rarity => rarity;
        public float DailyPhDrift => dailyPhDrift;
        public float OptimalPhMin => optimalPhMin;
        public float OptimalPhMax => optimalPhMax;
        public string PreferredFaction => preferredFaction;
        public string Description => description;
        public string ActivePower => activePower;
        public StageRequirements[] StageRequirements => stageRequirements;
        public PlantVisualSet VisualSet => visualSet;
        
        /// <summary>
        /// Ottiene i requisiti per uno stadio specifico
        /// </summary>
        public StageRequirements GetStageRequirements(PlantStage stage)
        {
            if (stageRequirements == null || stageRequirements.Length == 0)
                return null;
            
            foreach (var req in stageRequirements)
            {
                if (req != null && req.stage == stage)
                    return req;
            }
            
            return null;
        }
        
        /// <summary>
        /// Verifica se i requisiti per uno stadio sono soddisfatti
        /// </summary>
        public bool AreStageRequirementsMet(PlantStage stage, int currentHydration, LedType? lastUsedLed)
        {
            var req = GetStageRequirements(stage);
            if (req == null)
                return true; // Se non ci sono requisiti specifici, considera soddisfatti
            
            bool hydrationOk = req.IsHydrationInRange(currentHydration);
            bool ledOk = req.IsLedRequirementMet(lastUsedLed);
            
            return hydrationOk && ledOk;
        }
        
        /// <summary>
        /// Ottiene la durata tipica in giorni per uno stadio specifico
        /// </summary>
        public int GetStageDurationDays(PlantStage stage)
        {
            var req = GetStageRequirements(stage);
            return req != null ? req.durationDays : 2; // Default 2 giorni
        }
        
        /// <summary>
        /// Verifica se il pH è nella banda ottimale per questa pianta
        /// </summary>
        public bool IsPhInOptimalRange(float currentPh)
        {
            return currentPh >= optimalPhMin && currentPh <= optimalPhMax;
        }
        
        /// <summary>
        /// Calcola distanza normalizzata dal range ottimale (0-1)
        /// 0 = dentro range, 1 = molto lontano
        /// </summary>
        public float GetPhDistanceFromOptimal(float currentPh)
        {
            if (IsPhInOptimalRange(currentPh))
                return 0f;
            
            float distance;
            if (currentPh < optimalPhMin)
                distance = optimalPhMin - currentPh;
            else
                distance = currentPh - optimalPhMax;
            
            // Normalizza su range 0-100 (pH va da -100 a +100)
            return Mathf.Clamp01(distance / 100f);
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
            // Validazione e correzione valori pH drift in base alla famiglia
            // Assicura che Pure abbia drift positivo e Evil abbia drift negativo
            if (family == PlantFamily.Pure && dailyPhDrift <= 0)
            {
                // Se Pure ma drift non positivo, imposta valore di default +2
                if (dailyPhDrift == 0 || dailyPhDrift < 0)
                {
                    dailyPhDrift = 2f;
                }
            }
            else if (family == PlantFamily.Evil && dailyPhDrift >= 0)
            {
                // Se Evil ma drift non negativo, imposta valore di default -2
                if (dailyPhDrift == 0 || dailyPhDrift > 0)
                {
                    dailyPhDrift = -2f;
                }
            }
            else if (family == PlantFamily.Standard && dailyPhDrift != 0)
            {
                // Standard dovrebbe avere drift 0
                dailyPhDrift = 0f;
            }
            
            // Correzione valori comuni errati (0,2 invece di 2, -0,2 invece di -2)
            // Se il valore assoluto è tra 0,1 e 0,9, probabilmente è un errore di input
            if (Mathf.Abs(dailyPhDrift) > 0.1f && Mathf.Abs(dailyPhDrift) < 1f)
            {
                // Moltiplica per 10 se sembra essere un errore di virgola decimale
                if (family == PlantFamily.Pure && dailyPhDrift > 0)
                {
                    dailyPhDrift = 2f; // Corregge 0,2 -> 2
                }
                else if (family == PlantFamily.Evil && dailyPhDrift < 0)
                {
                    dailyPhDrift = -2f; // Corregge -0,2 -> -2
                }
            }
            // Validazione parametri
            if (string.IsNullOrEmpty(plantCode))
            {
                SporiumLogger.LogWarning(LogCategory.Pot, $"{name}: PlantCode non impostato!");
            }
            
            if (seedItemConfig == null)
            {
                SporiumLogger.LogWarning(LogCategory.Pot, $"{name}: SeedItemConfig non assegnato!");
            }
            
            if (seedItemConfig != null && !seedItemConfig.IsSeed)
            {
                SporiumLogger.LogWarning(LogCategory.Pot, $"{name}: SeedItemConfig assegnato non è un seme (IsSeed=false)!");
            }
            
            // Validazione pH drift in base alla famiglia
            if (family == PlantFamily.Pure && dailyPhDrift <= 0)
            {
                SporiumLogger.LogWarning(LogCategory.Ph, $"{name}: Pianta Pure dovrebbe avere drift pH positivo!");
            }
            
            if (family == PlantFamily.Evil && dailyPhDrift >= 0)
            {
                SporiumLogger.LogWarning(LogCategory.Ph, $"{name}: Pianta Evil dovrebbe avere drift pH negativo!");
            }
            
            if (family == PlantFamily.Standard && dailyPhDrift != 0)
            {
                SporiumLogger.LogWarning(LogCategory.Ph, $"{name}: Pianta Standard dovrebbe avere drift pH = 0!");
            }
            
            // Validazione range pH ottimale
            if (optimalPhMin >= optimalPhMax)
            {
                SporiumLogger.LogWarning(LogCategory.Ph, $"{name}: Range pH ottimale non valido (min >= max)!");
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

