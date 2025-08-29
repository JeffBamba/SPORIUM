using UnityEngine;
using Sporae.Core;

namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// Bootstrap per configurare automaticamente il sistema di crescita
    /// Si occupa di trovare e registrare tutti i vasi nel PlantGrowthSystem
    /// </summary>
    public class GrowthSystemBootstrap : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool autoFindPots = true;
        [SerializeField] private bool autoRegisterWithGrowthSystem = true;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private PlantGrowthSystem growthSystem;
        private PotGrowthController[] foundPots;

        void Start()
        {
            if (autoFindPots)
            {
                FindAllPots();
            }
            
            if (autoRegisterWithGrowthSystem)
            {
                RegisterPotsWithGrowthSystem();
            }
        }

        /// <summary>
        /// Trova tutti i vasi con PotGrowthController nella scena
        /// </summary>
        private void FindAllPots()
        {
            foundPots = FindObjectsOfType<PotGrowthController>();
            
            if (showDebugLogs)
            {
                Debug.Log($"[BLK-01.03A] GrowthSystemBootstrap: Trovati {foundPots.Length} vasi con PotGrowthController");
                
                for (int i = 0; i < foundPots.Length; i++)
                {
                    var pot = foundPots[i];
                    if (pot != null)
                    {
                        var potState = pot.GetPotState();
                        string info = potState != null ? $" - {potState.PotId}" : " - Stato non valido";
                        Debug.Log($"  [{i}] {pot.name}{info}");
                    }
                }
            }
        }

        /// <summary>
        /// Registra tutti i vasi trovati nel PlantGrowthSystem
        /// </summary>
        private void RegisterPotsWithGrowthSystem()
        {
            // Trova il PlantGrowthSystem
            growthSystem = FindObjectOfType<PlantGrowthSystem>();
            
            if (growthSystem == null)
            {
                Debug.LogError("[BLK-01.03A] GrowthSystemBootstrap: PlantGrowthSystem non trovato nella scena!");
                return;
            }

            if (foundPots == null || foundPots.Length == 0)
            {
                Debug.LogWarning("[BLK-01.03A] GrowthSystemBootstrap: Nessun vaso da registrare!");
                return;
            }

            // Registra ogni vaso
            int registeredCount = 0;
            foreach (var pot in foundPots)
            {
                if (pot != null)
                {
                    growthSystem.RegisterPot(pot);
                    registeredCount++;
                }
            }

            if (showDebugLogs)
            {
                Debug.Log($"[BLK-01.03A] GrowthSystemBootstrap: Registrati {registeredCount}/{foundPots.Length} vasi nel PlantGrowthSystem");
            }
        }

        /// <summary>
        /// Ricerca manuale dei vasi (utile per debugging)
        /// </summary>
        [ContextMenu("Find All Pots")]
        public void ManualFindPots()
        {
            FindAllPots();
        }

        /// <summary>
        /// Registrazione manuale (utile per debugging)
        /// </summary>
        [ContextMenu("Register With Growth System")]
        public void ManualRegister()
        {
            RegisterPotsWithGrowthSystem();
        }

        /// <summary>
        /// Verifica lo stato del sistema
        /// </summary>
        [ContextMenu("Check System Status")]
        public void CheckSystemStatus()
        {
            if (growthSystem == null)
            {
                growthSystem = FindObjectOfType<PlantGrowthSystem>();
            }

            if (growthSystem != null)
            {
                int registeredCount = growthSystem.GetRegisteredPotCount();
                var config = growthSystem.GetGrowthConfig();
                
                Debug.Log($"[BLK-01.03A] GrowthSystemBootstrap: Status Check");
                Debug.Log($"  PlantGrowthSystem: {growthSystem.name}");
                Debug.Log($"  Vasi registrati: {registeredCount}");
                Debug.Log($"  Configurazione: {config?.name ?? "NULL"}");
                
                if (config != null)
                {
                    Debug.Log($"  Seed→Sprout: {config.pointsSeedToSprout} punti");
                    Debug.Log($"  Sprout→Mature: {config.pointsSproutToMature} punti");
                    Debug.Log($"  Cura ideale: {config.pointsIdealCare} punti/giorno");
                    Debug.Log($"  Cura parziale: {config.pointsPartialCare} punti/giorno");
                }
            }
            else
            {
                Debug.LogError("[BLK-01.03A] GrowthSystemBootstrap: PlantGrowthSystem non trovato!");
            }
        }

        #if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (foundPots != null)
            {
                Gizmos.color = Color.green;
                foreach (var pot in foundPots)
                {
                    if (pot != null)
                    {
                        Gizmos.DrawWireSphere(pot.transform.position, 0.3f);
                    }
                }
            }
        }
        #endif
    }
}
