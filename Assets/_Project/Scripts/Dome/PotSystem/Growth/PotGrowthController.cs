using UnityEngine;
using Sporae.Core;

namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// Controller per la crescita di un singolo vaso
    /// Gestisce la logica di avanzamento stadi e calcolo punti crescita
    /// BLK-01.03B: Esteso con sistema visuale per stadi di crescita
    /// </summary>
    public class PotGrowthController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PotStateModel potState;
        
        [Header("BLK-01.03B - Visual References")]
        [SerializeField] private SpriteRenderer plantRenderer;
        [SerializeField] private Sprite s0_empty;
        [SerializeField] private Sprite s1_seed;
        [SerializeField] private Sprite s2_sprout;
        [SerializeField] private Sprite s3_mature;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private void Awake()
        {
            // Assicurati che il PotStateModel sia assegnato
            if (potState == null)
            {
                potState = GetComponent<PotStateModel>();
                if (potState == null)
                {
                    Debug.LogError($"[BLK-01.03A] PotGrowthController su {gameObject.name}: PotStateModel non trovato!");
                }
            }
            
            // BLK-01.03B: Cerca il plantRenderer se non assegnato
            if (plantRenderer == null)
            {
                plantRenderer = GetComponentInChildren<SpriteRenderer>();
                if (plantRenderer == null)
                {
                    Debug.LogWarning($"[BLK-01.03B] PotGrowthController su {gameObject.name}: SpriteRenderer non trovato. Le visuali non saranno aggiornate.");
                }
            }
        }

        /// <summary>
        /// Inizializza il vaso quando viene piantato un seme
        /// </summary>
        public void OnPlanted()
        {
            if (potState == null) return;

            potState.HasPlant = true;
            potState.Stage = (int)PlantStage.Seed; // 1 = Seed
            potState.GrowthPoints = 0;
            potState.DaysSincePlant = 0;
            potState.DaysNeglectedStreak = 0;
            // BLK-01.03A: I timestamp vengono impostati da PotActions quando si eseguono le azioni

            if (enableDebugLogs)
                Debug.Log($"[BLK-01.03A] {potState.PotId}: Seme piantato, inizializzato come Seed");
            
            // BLK-01.03B: Aggiorna le visuali
            UpdateVisuals();
        }

        /// <summary>
        /// BLK-01.03B: Aggiorna le visuali del vaso in base allo stadio corrente
        /// </summary>
        public void UpdateVisuals()
        {
            if (plantRenderer == null || potState == null) return;
            
            // Aggiorna sprite in base allo stadio
            Sprite targetSprite = GetSpriteForStage(potState.Stage);
            if (targetSprite != null)
            {
                plantRenderer.sprite = targetSprite;
            }
            
            // Aggiorna scala in base allo stadio
            float targetScale = GetScaleForStage(potState.Stage);
            plantRenderer.transform.localScale = Vector3.one * targetScale;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[BLK-01.03B] {potState.PotId}: Visuali aggiornate - Stadio: {potState.Stage}, Scala: {targetScale:F2}");
            }
        }
        
        /// <summary>
        /// BLK-01.03B: Restituisce lo sprite appropriato per lo stadio corrente
        /// </summary>
        private Sprite GetSpriteForStage(int stage)
        {
            switch (stage)
            {
                case (int)PlantStage.Empty:
                    return s0_empty;
                case (int)PlantStage.Seed:
                    return s1_seed;
                case (int)PlantStage.Sprout:
                    return s2_sprout;
                case (int)PlantStage.Mature:
                    return s3_mature;
                default:
                    return s0_empty; // Fallback
            }
        }
        
        /// <summary>
        /// BLK-01.03B: Restituisce la scala appropriata per lo stadio corrente
        /// </summary>
        private float GetScaleForStage(int stage)
        {
            switch (stage)
            {
                case (int)PlantStage.Empty:
                    return 1.00f;
                case (int)PlantStage.Seed:
                    return 1.05f;
                case (int)PlantStage.Sprout:
                    return 1.12f;
                case (int)PlantStage.Mature:
                    return 1.20f;
                default:
                    return 1.00f; // Fallback
            }
        }

        /// <summary>
        /// DEPRECATO - La crescita è ora gestita dal DayCycleController (BLK-01.03A)
        /// Questo metodo è mantenuto per compatibilità ma non dovrebbe essere chiamato
        /// </summary>
        [System.Obsolete("Sostituito da DayCycleController in BLK-01.03A")]
        public void ApplyDailyGrowth(PlantGrowthConfig growthConfig)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[BLK-01.03A] {potState?.PotId ?? "Unknown"}: ApplyDailyGrowth è deprecato! Usa DayCycleController invece.");
            
            // La crescita è ora gestita automaticamente dal DayCycleController
            // Questo metodo è mantenuto solo per compatibilità con codice esistente
        }

        /// <summary>
        /// DEPRECATO - Calcolo punti ora gestito dal DayCycleController (BLK-01.03A)
        /// </summary>
        [System.Obsolete("Sostituito da DayCycleController in BLK-01.03A")]
        private int CalculateDailyGrowthPoints(PlantGrowthConfig growthConfig)
        {
            Debug.LogWarning($"[BLK-01.03A] {potState?.PotId ?? "Unknown"}: CalculateDailyGrowthPoints è deprecato!");
            return 0; // La crescita è ora gestita automaticamente
        }

        /// <summary>
        /// DEPRECATO - Avanzamento stadi ora gestito dal DayCycleController (BLK-01.03A)
        /// </summary>
        [System.Obsolete("Sostituito da DayCycleController in BLK-01.03A")]
        private void TryAdvanceStage(PlantGrowthConfig growthConfig)
        {
            Debug.LogWarning($"[BLK-01.03A] {potState?.PotId ?? "Unknown"}: TryAdvanceStage è deprecato!");
            // L'avanzamento di stadio è ora gestito automaticamente dal DayCycleController
        }

        /// <summary>
        /// DEPRECATO - Decadimento ora gestito dal DayCycleController (BLK-01.03A)
        /// </summary>
        [System.Obsolete("Sostituito da DayCycleController in BLK-01.03A")]
        private void DecayAndReset(PlantGrowthConfig growthConfig)
        {
            Debug.LogWarning($"[BLK-01.03A] {potState?.PotId ?? "Unknown"}: DecayAndReset è deprecato!");
            // Il decadimento è ora gestito automaticamente dal DayCycleController
        }

        /// <summary>
        /// Imposta il PotStateModel (per setup runtime)
        /// </summary>
        public void SetPotState(PotStateModel state)
        {
            potState = state;
        }

        /// <summary>
        /// Ottiene il PotStateModel corrente
        /// </summary>
        public PotStateModel GetPotState()
        {
            return potState;
        }
    }
}
