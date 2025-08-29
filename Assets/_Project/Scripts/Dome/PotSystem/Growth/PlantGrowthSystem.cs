using System.Collections.Generic;
using UnityEngine;
using Sporae.Core;

namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// Sistema globale per la gestione della crescita delle piante
    /// Si iscrive a GameManager.OnDayChanged e applica la crescita a tutti i vasi registrati
    /// </summary>
    public class PlantGrowthSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PlantGrowthConfig growthConfig;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool logRegisteredPots = true;

        private List<PotGrowthController> registeredPots = new List<PotGrowthController>();
        private bool isInitialized = false;

        private void Awake()
        {
            // Cerca la configurazione se non assegnata
            if (growthConfig == null)
            {
                growthConfig = Resources.Load<PlantGrowthConfig>("PlantGrowthConfig");
                if (growthConfig == null)
                {
                    Debug.LogWarning("[BLK-01.03A] PlantGrowthConfig non trovato in Resources, verrà cercato in PotSystemConfig");
                }
            }
        }

        private void Start()
        {
            InitializeSystem();
        }

        private void OnEnable()
        {
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.OnDayChanged += HandleDayChanged;
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// Inizializza il sistema e si iscrive agli eventi
        /// </summary>
        private void InitializeSystem()
        {
            if (isInitialized) return;

            // Cerca GameManager e si iscrive
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.OnDayChanged += HandleDayChanged;
                if (enableDebugLogs)
                    Debug.Log("[BLK-01.03A] PlantGrowthSystem: Iscritto a GameManager.OnDayChanged");
            }
            else
            {
                Debug.LogError("[BLK-01.03A] PlantGrowthSystem: GameManager non trovato!");
            }

            // Cerca configurazione in PotSystemConfig se non trovata
            if (growthConfig == null)
            {
                var potSystemConfig = FindObjectOfType<PotSystemConfig>();
                if (potSystemConfig != null && potSystemConfig.GrowthConfig != null)
                {
                    growthConfig = potSystemConfig.GrowthConfig;
                    if (enableDebugLogs)
                        Debug.Log("[BLK-01.03A] PlantGrowthSystem: Configurazione caricata da PotSystemConfig");
                }
            }

            // Verifica configurazione
            if (growthConfig == null)
            {
                Debug.LogError("[BLK-01.03A] PlantGrowthSystem: Nessuna configurazione di crescita trovata!");
                return;
            }

            isInitialized = true;
            if (enableDebugLogs)
                Debug.Log($"[BLK-01.03A] PlantGrowthSystem: Inizializzato con config '{growthConfig.name}'");
        }

        /// <summary>
        /// Registra un vaso nel sistema di crescita
        /// </summary>
        public void RegisterPot(PotGrowthController pot)
        {
            if (pot == null) return;

            if (!registeredPots.Contains(pot))
            {
                registeredPots.Add(pot);
                if (enableDebugLogs)
                    Debug.Log($"[BLK-01.03A] PlantGrowthSystem: Registrato vaso {pot.name}");
                
                if (logRegisteredPots)
                    LogRegisteredPots();
            }
        }

        /// <summary>
        /// Rimuove un vaso dal sistema di crescita
        /// </summary>
        public void UnregisterPot(PotGrowthController pot)
        {
            if (pot == null) return;

            if (registeredPots.Remove(pot))
            {
                if (enableDebugLogs)
                    Debug.Log($"[BLK-01.03A] PlantGrowthSystem: Rimosso vaso {pot.name}");
                
                if (logRegisteredPots)
                    LogRegisteredPots();
            }
        }

        /// <summary>
        /// Gestisce il cambio di giorno dal GameManager
        /// </summary>
        private void HandleDayChanged(int dayIndex)
        {
            Debug.Log($"[BLK-01.03A] HandleDayChanged chiamato per Day {dayIndex}");
            
            if (!growthConfig) 
            {
                var potSystemConfig = FindObjectOfType<PotSystemConfig>();
                if (potSystemConfig != null && potSystemConfig.GrowthConfig != null)
                {
                    growthConfig = potSystemConfig.GrowthConfig;
                    Debug.Log($"[BLK-01.03A] Configurazione caricata: {growthConfig.name}");
                }
                else
                {
                    Debug.LogError("[BLK-01.03A] Nessuna configurazione trovata!");
                    return;
                }
            }
            
            Debug.Log($"[BLK-01.03A] Applicazione crescita a {registeredPots.Count} vasi");
            foreach (var p in registeredPots) 
            {
                if (p != null)
                {
                    Debug.Log($"[BLK-01.03A] Applicando crescita a vaso: {p.name}");
                    p.ApplyDailyGrowth(growthConfig);
                }
            }
            Debug.Log($"[BLK-01.03A] Growth tick completato per Day {dayIndex}");
        }

        /// <summary>
        /// Rimuove le iscrizioni agli eventi
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.OnDayChanged -= HandleDayChanged;
                if (enableDebugLogs)
                    Debug.Log("[BLK-01.03A] PlantGrowthSystem: Disiscritto da GameManager.OnDayChanged");
            }
        }

        /// <summary>
        /// Logga i vasi registrati (per debug)
        /// </summary>
        private void LogRegisteredPots()
        {
            if (!logRegisteredPots) return;

            Debug.Log($"[BLK-01.03A] PlantGrowthSystem: Vasi registrati ({registeredPots.Count}):");
            for (int i = 0; i < registeredPots.Count; i++)
            {
                var pot = registeredPots[i];
                if (pot != null)
                {
                    var potState = pot.GetPotState();
                    string plantInfo = potState != null && potState.HasPlant 
                        ? $" - {((PlantStage)potState.Stage)} (Giorno {potState.DaysSincePlant})" 
                        : " - Vuoto";
                    Debug.Log($"  [{i}] {pot.name}{plantInfo}");
                }
                else
                {
                    Debug.Log($"  [{i}] NULL (da rimuovere)");
                }
            }
        }

        /// <summary>
        /// Pulisce i vasi null dalla lista (per sicurezza)
        /// </summary>
        public void CleanupNullPots()
        {
            registeredPots.RemoveAll(pot => pot == null);
            if (enableDebugLogs)
                Debug.Log($"[BLK-01.03A] PlantGrowthSystem: Cleanup completato, {registeredPots.Count} vasi validi");
        }

        /// <summary>
        /// Ottiene il numero di vasi registrati
        /// </summary>
        public int GetRegisteredPotCount()
        {
            return registeredPots.Count;
        }

        /// <summary>
        /// Ottiene la configurazione di crescita corrente
        /// </summary>
        public PlantGrowthConfig GetGrowthConfig()
        {
            return growthConfig;
        }

        /// <summary>
        /// Imposta la configurazione di crescita
        /// </summary>
        public void SetGrowthConfig(PlantGrowthConfig config)
        {
            growthConfig = config;
            if (enableDebugLogs)
                Debug.Log($"[BLK-01.03A] PlantGrowthSystem: Nuova configurazione impostata: {config?.name ?? "NULL"}");
        }

        #if UNITY_EDITOR
        [ContextMenu("Log Registered Pots")]
        private void EditorLogRegisteredPots()
        {
            LogRegisteredPots();
        }

        [ContextMenu("Cleanup Null Pots")]
        private void EditorCleanupNullPots()
        {
            CleanupNullPots();
        }
        #endif
    }
}
