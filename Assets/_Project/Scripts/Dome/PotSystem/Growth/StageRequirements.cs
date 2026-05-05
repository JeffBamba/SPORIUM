using UnityEngine;

namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// Wrapper serializzabile per LedType nullable (Unity non serializza direttamente nullable types)
    /// </summary>
    [System.Serializable]
    public class SerializableLedType
    {
        public bool hasValue = false;
        public LedType value = LedType.Blue;
        
        public LedType? ToNullable()
        {
            return hasValue ? (LedType?)value : null;
        }
        
        public void FromNullable(LedType? ledType)
        {
            hasValue = ledType.HasValue;
            if (ledType.HasValue)
                value = ledType.Value;
        }
    }
    
    /// <summary>
    /// Requisiti di crescita per uno stadio specifico di una pianta.
    /// Definisce idratazione, LED richiesti, durata e note per ogni stadio.
    /// </summary>
    [System.Serializable]
    public class StageRequirements
    {
        [Header("Stage Info")]
        [Tooltip("Stadio di crescita a cui si applicano questi requisiti")]
        public PlantStage stage;
        
        [Header("Hydration Requirements")]
        [Tooltip("Idratazione minima richiesta (%)")]
        [Range(0, 100)]
        public int hydrationMin = 0;
        
        [Tooltip("Idratazione ottimale/mediana (%)")]
        [Range(0, 100)]
        public int hydrationMed = 50;
        
        [Tooltip("Idratazione massima tollerata (%)")]
        [Range(0, 100)]
        public int hydrationMax = 100;
        
        [Header("LED Requirements")]
        [Tooltip("Tipo LED richiesto per questo stadio (hasValue=false = nessun LED richiesto)")]
        public SerializableLedType requiredLed = new SerializableLedType();
        
        [Header("Light Requirements (BLK-03.01-T2)")]
        [Tooltip("Range minimo luce (%)")]
        [Range(0, 100)]
        public int lightMin = 0;
        
        [Tooltip("Luce ottimale/mediana (%)")]
        [Range(0, 100)]
        public int lightMed = 50;
        
        [Tooltip("Range massimo luce (%)")]
        [Range(0, 100)]
        public int lightMax = 100;
        
        [Header("Fertilizer Requirements (BLK-03.01-T1)")]
        [Tooltip("Range minimo fertilizzante (%). Per stadio: vedi curve su PlantData (es. 75 early, 50 Growth, 25 Flowering/Harvest).")]
        [Range(0, 100)]
        public int fertilizerMin = 0;
        
        [Tooltip("Fertilizzante ottimale/mediano (%)")]
        [Range(0, 100)]
        public int fertilizerMed = 50;
        
        [Tooltip("Range massimo fertilizzante (%)")]
        [Range(0, 100)]
        public int fertilizerMax = 100;
        
        [Header("Duration")]
        [Tooltip("Durata tipica in giorni per questo stadio")]
        [Range(1, 10)]
        public int durationDays = 2;
        
        [Header("Notes")]
        [Tooltip("Note aggiuntive per questo stadio")]
        [TextArea(2, 4)]
        public string notes = "";
        
        /// <summary>
        /// Verifica se l'idratazione è nel range accettabile per questo stadio
        /// </summary>
        public bool IsHydrationInRange(int currentHydration)
        {
            return currentHydration >= hydrationMin && currentHydration <= hydrationMax;
        }
        
        /// <summary>
        /// Verifica se l'idratazione è ottimale (vicina al valore mediano)
        /// </summary>
        public bool IsHydrationOptimal(int currentHydration, int tolerance = 5)
        {
            return Mathf.Abs(currentHydration - hydrationMed) <= tolerance;
        }
        
        /// <summary>
        /// Ottiene il LED richiesto come nullable
        /// </summary>
        public LedType? GetRequiredLed()
        {
            return requiredLed.ToNullable();
        }
        
        /// <summary>
        /// Imposta il LED richiesto
        /// </summary>
        public void SetRequiredLed(LedType? ledType)
        {
            requiredLed.FromNullable(ledType);
        }
        
        /// <summary>
        /// Verifica se il LED richiesto è stato utilizzato (legacy - usa LastLedType)
        /// </summary>
        public bool IsLedRequirementMet(LedType? lastUsedLed)
        {
            LedType? required = GetRequiredLed();
            
            // Se non è richiesto LED, il requisito è sempre soddisfatto
            if (!required.HasValue)
                return true;
            
            // Se è richiesto un LED specifico, verifica che sia stato usato
            return lastUsedLed.HasValue && lastUsedLed.Value == required.Value;
        }
        
        /// <summary>
        /// BLK-02.07: Verifica se il LED richiesto è attivo (nuovo sistema - usa LedSystemState)
        /// </summary>
        public bool IsLedRequirementMet(LedSystemState currentState)
        {
            LedType? required = GetRequiredLed();
            if (!required.HasValue) return true;  // Nessun LED richiesto
            
            if (currentState == LedSystemState.Off) return false;  // Sistema spento
            
            // Converti LedSystemState a LedType per verifica
            LedType currentLedType = currentState == LedSystemState.Blue ? LedType.Blue : LedType.Red;
            return currentLedType == required.Value;
        }
        
        /// <summary>
        /// BLK-03.01-T1: Verifica se il fertilizzante è nel range accettabile per questo stadio
        /// </summary>
        public bool IsFertilizerInRange(int currentFertilizer)
        {
            return currentFertilizer >= fertilizerMin && currentFertilizer <= fertilizerMax;
        }
        
        /// <summary>
        /// BLK-03.01-T1: Verifica se il fertilizzante è ottimale (vicino al valore mediano)
        /// </summary>
        public bool IsFertilizerOptimal(int currentFertilizer, int tolerance = 5)
        {
            return Mathf.Abs(currentFertilizer - fertilizerMed) <= tolerance;
        }
        
        /// <summary>
        /// BLK-03.01-T2: Verifica se la luce è nel range accettabile per questo stadio
        /// </summary>
        public bool IsLightInRange(int currentLight)
        {
            return currentLight >= lightMin && currentLight <= lightMax;
        }
        
        /// <summary>
        /// BLK-03.01-T2: Verifica se la luce è ottimale (vicino al valore mediano)
        /// </summary>
        public bool IsLightOptimal(int currentLight, int tolerance = 5)
        {
            return Mathf.Abs(currentLight - lightMed) <= tolerance;
        }
        
        /// <summary>
        /// Restituisce una descrizione testuale dei requisiti
        /// </summary>
        public string GetRequirementsDescription()
        {
            string desc = $"Stage: {stage}\n";
            desc += $"Idratazione: {hydrationMin}% - {hydrationMed}% - {hydrationMax}%\n";
            
            LedType? required = GetRequiredLed();
            if (required.HasValue)
            {
                desc += $"LED richiesto: {required.Value}\n";
            }
            else
            {
                desc += "LED: Nessuno richiesto\n";
            }
            
            desc += $"Fertilizzante: {fertilizerMin}% - {fertilizerMed}% - {fertilizerMax}%\n";
            desc += $"Durata: {durationDays} giorni\n";
            
            if (!string.IsNullOrEmpty(notes))
            {
                desc += $"Note: {notes}";
            }
            
            return desc;
        }
    }
}

