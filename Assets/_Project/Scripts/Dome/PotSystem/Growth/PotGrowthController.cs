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
        
        public void OnUprooted()
        {
            if (potState == null)
                return;
            
            potState.HasPlant = false;
            potState.Stage = (int)PlantStage.Empty;
            potState.GrowthPoints = 0;
            potState.DaysSincePlant = 0;
            potState.DaysNeglectedStreak = 0;
            
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
            plantRenderer.sprite = targetSprite;
            if (targetSprite != null)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[BLK-01.04] {potState.PotId}: Sprite aggiornato a {targetSprite.name} per stadio {potState.Stage}");
                }
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"[BLK-01.04] {potState.PotId}: Sprite NULL per stadio {potState.Stage}! Controlla assegnazione sprite nel PotGrowthController.");
                }
            }
            
            // Aggiorna scala in base allo stadio
            float targetScale = GetScaleForStage(potState.Stage);
            plantRenderer.transform.localScale = Vector3.one * targetScale;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[BLK-01.04] {potState.PotId}: Visuali aggiornate - Stadio: {potState.Stage}, Scala: {targetScale:F2}, Sprite: {(targetSprite != null ? targetSprite.name : "NULL")}");
            }
        }
        
        /// <summary>
        /// BLK-01.03B: Restituisce lo sprite appropriato per lo stadio corrente
        /// </summary>
        private Sprite GetSpriteForStage(int stage)
        {
            Sprite result = null;
            string stageName = "";
            
            switch (stage)
            {
                case (int)PlantStage.Empty:
                    result = s0_empty;
                    stageName = "Empty";
                    break;
                case (int)PlantStage.Seed:
                    result = s1_seed;
                    stageName = "Seed";
                    break;
                case (int)PlantStage.Sprout:
                    result = s2_sprout;
                    stageName = "Sprout";
                    break;
                case (int)PlantStage.Mature:
                    result = s3_mature;
                    stageName = "Mature";
                    break;
                default:
                    result = s0_empty; // Fallback
                    stageName = "Fallback";
                    break;
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"[BLK-01.04] {potState?.PotId ?? "Unknown"}: GetSpriteForStage({stage}={stageName}) = {(result != null ? result.name : "NULL")}");
            }
            
            return result;
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
        /// BLK-01.04: Aggiorna le visuali quando lo stadio cambia
        /// Chiamato automaticamente dal DayCycleController quando avviene una transizione
        /// </summary>
        public void OnStageChanged(PlantStage newStage)
        {
            if (potState == null) 
            {
                Debug.LogWarning($"[BLK-01.04] OnStageChanged chiamato ma potState è NULL!");
                return;
            }
            
            if (enableDebugLogs)
                Debug.Log($"[BLK-01.04] {potState.PotId}: Stadio cambiato a {newStage}. Aggiornamento visuali...");
            
            // Aggiorna le visuali
            UpdateVisuals();
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
