using UnityEngine;
using _Project.Sporae.Core;
using Sporae.Dome;
using Sporae.Core;
using Sporae.DevTools;
using Sporae.Dome.PotSystem.Condition;

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
        
        [Header("VaultMap BG Mode (Window Overlay)")]
        [Tooltip("Se true: quando il vaso è Empty non renderizza niente (si vede il vetro OFF già nel BG).")]
        [SerializeField] private bool useVaultMapBgForEmpty = true;
        [Tooltip("Se false (consigliato per WindowContent): non applica scaling per stage (evita che HarvestReady/Resting escano dalla finestra).")]
        [SerializeField] private bool useStageScaling = false;
        
        [Header("BLK-POT-VISUALS (data-driven)")]
        [SerializeField] private PotSharedVisualsConfig sharedVisuals;
        [SerializeField] private string sharedVisualsResourcesPath = "Configs/PotSharedVisualsConfig";
        
        [Header("Legacy Fallback Sprites (pre-data-driven)")]
        [SerializeField] private Sprite s0_empty;
        [SerializeField] private Sprite s1_seed;
        [SerializeField] private Sprite s2_sprout;
        [SerializeField] private Sprite s3_mature;
        
        [Header("Overlay Renderers (optional)")]
        [SerializeField] private SpriteRenderer conditionTintOverlayRenderer;
        [SerializeField] private SpriteRenderer infestedOverlayRenderer;
        [SerializeField] private SpriteRenderer fruitOverlayRenderer;
        
        [Header("Overlay Colors")]
        [SerializeField] private Color appassitaTint = new Color(1f, 1f, 0.2f, 0.35f); // giallo
        [SerializeField] private Color criticaTint = new Color(0.55f, 0.3f, 0.12f, 0.45f); // marrone
        [SerializeField] private Color infestedTint = new Color(0.25f, 0.75f, 0.25f, 0.35f); // verde malato
        
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
                    SporiumLogger.LogError(LogCategory.Pot, $"PotGrowthController su {gameObject.name}: PotStateModel non trovato!");
                }
            }
            
            // BLK-01.03B: Cerca il plantRenderer se non assegnato
            if (plantRenderer == null)
            {
                // Priorità: child "WindowContent" (setup VaultMap BG)
                Transform windowContent = transform.Find("WindowContent");
                if (windowContent != null)
                {
                    plantRenderer = windowContent.GetComponent<SpriteRenderer>();
                }
                
                if (plantRenderer == null)
                {
                    plantRenderer = GetComponentInChildren<SpriteRenderer>();
                }
                
                if (plantRenderer == null)
                {
                    SporiumLogger.LogWarning(LogCategory.Pot, $"PotGrowthController su {gameObject.name}: SpriteRenderer non trovato. Le visuali non saranno aggiornate.");
                }
            }

            if (sharedVisuals == null && !string.IsNullOrEmpty(sharedVisualsResourcesPath))
            {
                sharedVisuals = Resources.Load<PotSharedVisualsConfig>(sharedVisualsResourcesPath);
            }
            
            EnsureOverlayRenderers();

            ServiceContainer.Instance?.Get<DomePotRegistry>(suppressWarning: true)?.RegisterGrowthController(this);
        }

        private void OnDestroy()
        {
            ServiceContainer.Instance?.Get<DomePotRegistry>(suppressWarning: true)?.UnregisterGrowthController(this);
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
                SporiumLogger.LogInfo(LogCategory.Pot, $"{potState.PotId}: Seme piantato, inizializzato come Seed");
            
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

            bool isEmpty = !potState.HasPlant || potState.Stage == (int)PlantStage.Empty;
            if (useVaultMapBgForEmpty && isEmpty)
            {
                // In modalità VaultMap BG: lo stato vuoto è già nel background, quindi disattiva overlay/renderers.
                plantRenderer.enabled = false;
                
                // Spegni overlay (tint/fruit/infested)
                UpdateOverlays(null);
                return;
            }

            // Pot attivo: assicurati che il contenuto sia visibile
            plantRenderer.enabled = true;
            
            // Aggiorna sprite in base a stadio + condizione
            Sprite targetSprite = ResolveBaseSprite();
            
            // BUG FIX: Usa sprite default se null invece di lasciare null
            if (targetSprite == null)
            {
                targetSprite = GetSharedOrLegacyEmpty(); // Fallback a sprite vuoto
                if (enableDebugLogs)
                {
                    SporiumLogger.LogWarning(LogCategory.Pot, $"{potState.PotId}: Sprite NULL per stadio {potState.Stage}! Usando sprite default (empty).");
                }
            }
            else if (enableDebugLogs)
            {
                SporiumLogger.LogDebug(LogCategory.Pot, $"{potState.PotId}: Sprite aggiornato a {targetSprite.name} per stadio {potState.Stage}");
            }
            
            plantRenderer.sprite = targetSprite;
            
            // Aggiorna scala in base allo stadio
            float targetScale = useStageScaling ? GetScaleForStage(potState.Stage) : 1.0f;
            plantRenderer.transform.localScale = Vector3.one * targetScale;
            
            if (enableDebugLogs)
            {
                SporiumLogger.LogDebug(LogCategory.Pot, $"{potState.PotId}: Visuali aggiornate - Stadio: {potState.Stage}, Scala: {targetScale:F2}, Sprite: {(targetSprite != null ? targetSprite.name : "NULL")}");
            }

            // Overlays (tint + fruit + infested)
            UpdateOverlays(targetSprite);
        }
        
        /// <summary>
        /// Resolver base sprite: shared (Empty/Seed/Sprout/Morta) + per-specie (Adult/Flowering/FruitOverlay)
        /// </summary>
        private Sprite ResolveBaseSprite()
        {
            // Morta ha priorità sullo stadio
            PlantCondition condition = (PlantCondition)potState.ConditionLabel;
            if (condition == PlantCondition.Morta)
                return GetSharedOrLegacyDead();

            int stage = potState.Stage;
            switch (stage)
            {
                case (int)PlantStage.Empty:
                    return GetSharedOrLegacyEmpty();
                case (int)PlantStage.Seed:
                    return GetSharedOrLegacySeed();
                case (int)PlantStage.Sprout:
                    return GetSharedOrLegacySprout();
                case (int)PlantStage.Growth:
                case (int)PlantStage.Resting:
                {
                    var plantData = potState.GetPlantData();
                    var vs = plantData != null ? plantData.VisualSet : null;
                    return vs != null ? vs.adultSprite : s3_mature;
                }
                case (int)PlantStage.Flowering:
                {
                    var plantData = potState.GetPlantData();
                    var vs = plantData != null ? plantData.VisualSet : null;
                    return vs != null ? vs.floweringSprite : s3_mature;
                }
                case (int)PlantStage.HarvestReady:
                {
                    // base = Adult, frutto in overlay separato
                    var plantData = potState.GetPlantData();
                    var vs = plantData != null ? plantData.VisualSet : null;
                    return vs != null ? vs.adultSprite : s3_mature;
                }
                default:
                    return GetSharedOrLegacyEmpty();
            }
        }
        
        private Sprite GetSharedOrLegacyEmpty() => sharedVisuals != null && sharedVisuals.emptyPotSprite != null ? sharedVisuals.emptyPotSprite : s0_empty;
        private Sprite GetSharedOrLegacySeed() => sharedVisuals != null && sharedVisuals.seedSprite != null ? sharedVisuals.seedSprite : s1_seed;
        private Sprite GetSharedOrLegacySprout() => sharedVisuals != null && sharedVisuals.sproutSprite != null ? sharedVisuals.sproutSprite : s2_sprout;
        private Sprite GetSharedOrLegacyDead() => sharedVisuals != null && sharedVisuals.deadSprite != null ? sharedVisuals.deadSprite : s0_empty;
        
        private void EnsureOverlayRenderers()
        {
            if (plantRenderer == null)
                return;
            
            // Crea overlay a runtime se mancanti: non distruttivo verso prefab/scene.
            if (conditionTintOverlayRenderer == null)
                conditionTintOverlayRenderer = FindOrCreateOverlayRenderer("SR_ConditionTintOverlay", plantRenderer.sortingOrder + 1);
            if (infestedOverlayRenderer == null)
                infestedOverlayRenderer = FindOrCreateOverlayRenderer("SR_InfestedOverlay", plantRenderer.sortingOrder + 2);
            if (fruitOverlayRenderer == null)
                fruitOverlayRenderer = FindOrCreateOverlayRenderer("SR_FruitOverlay", plantRenderer.sortingOrder + 3);
        }
        
        private SpriteRenderer FindOrCreateOverlayRenderer(string name, int sortingOrder)
        {
            // 1) prova a riusare overlay già presenti in scena (es. in gerarchia sotto Pot_POT-00x)
            var existing = FindExistingOverlayRenderer(name);
            if (existing != null)
            {
                // Allinea sorting al plantRenderer attuale
                existing.sortingLayerID = plantRenderer.sortingLayerID;
                existing.sortingOrder = sortingOrder;
                
                // Opzionale: sposta sotto il plantRenderer per coerenza (mantiene pos world)
                if (existing.transform.parent != plantRenderer.transform)
                    existing.transform.SetParent(plantRenderer.transform, worldPositionStays: true);
                
                existing.enabled = false;
                return existing;
            }
            
            return CreateOverlayRenderer(name, sortingOrder);
        }
        
        private SpriteRenderer FindExistingOverlayRenderer(string name)
        {
            // Cerca prima sotto root pot (transform), poi sotto plantRenderer
            Transform t = transform.Find(name);
            if (t == null && plantRenderer != null)
                t = plantRenderer.transform.Find(name);
            return t != null ? t.GetComponent<SpriteRenderer>() : null;
        }
        
        
        private SpriteRenderer CreateOverlayRenderer(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(plantRenderer.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerID = plantRenderer.sortingLayerID;
            sr.sortingOrder = sortingOrder;
            sr.enabled = false;
            return sr;
        }
        
        private void UpdateOverlays(Sprite baseSprite)
        {
            if (potState == null)
                return;
            
            if (conditionTintOverlayRenderer != null)
                conditionTintOverlayRenderer.enabled = false;
            if (infestedOverlayRenderer != null)
                infestedOverlayRenderer.enabled = false;
            if (fruitOverlayRenderer != null)
                fruitOverlayRenderer.enabled = false;
            
            PlantCondition condition = (PlantCondition)potState.ConditionLabel;
            if (condition == PlantCondition.Morta)
            {
                // Morta: niente overlay, sprite base già dead
                return;
            }
            
            // Condition tint
            if (conditionTintOverlayRenderer != null && baseSprite != null)
            {
                if (condition == PlantCondition.Appassita)
                {
                    conditionTintOverlayRenderer.sprite = baseSprite;
                    conditionTintOverlayRenderer.color = appassitaTint;
                    conditionTintOverlayRenderer.enabled = true;
                }
                else if (condition == PlantCondition.Critica)
                {
                    conditionTintOverlayRenderer.sprite = baseSprite;
                    conditionTintOverlayRenderer.color = criticaTint;
                    conditionTintOverlayRenderer.enabled = true;
                }
            }
            
            // Infested overlay (verde malato)
            if (infestedOverlayRenderer != null && baseSprite != null && potState.IsInfested)
            {
                infestedOverlayRenderer.sprite = baseSprite;
                infestedOverlayRenderer.color = infestedTint;
                infestedOverlayRenderer.enabled = true;
            }
            
            // Fruit overlay (HarvestReady)
            if (fruitOverlayRenderer != null && potState.Stage == (int)PlantStage.HarvestReady)
            {
                var plantData = potState.GetPlantData();
                var vs = plantData != null ? plantData.VisualSet : null;
                Sprite fruitSprite = vs != null ? vs.fruitOverlaySprite : null;
                if (fruitSprite != null)
                {
                    fruitOverlayRenderer.sprite = fruitSprite;
                    fruitOverlayRenderer.color = Color.white;
                    fruitOverlayRenderer.enabled = true;
                }
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
                case (int)PlantStage.HarvestReady:
                    return 1.20f;
                case (int)PlantStage.Resting:
                    // BLK-02.05: Resting mantiene la stessa scala di HarvestReady
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
                SporiumLogger.LogWarning(LogCategory.Pot, "OnStageChanged chiamato ma potState è NULL!");
                return;
            }
            
            if (enableDebugLogs)
                SporiumLogger.LogInfo(LogCategory.Pot, $"{potState.PotId}: Stadio cambiato a {newStage}. Aggiornamento visuali...");
            
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
