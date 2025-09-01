using UnityEngine;
using Sporae.Core;
using Sporae.Dome.PotSystem;

namespace Sporae.Dev
{
    /// <summary>
    /// BLK-01.03B: Debug hotkeys per testare il sistema di crescita piante
    /// Solo per Editor/Development build
    /// </summary>
    public class GrowthDebugHotkeys : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugHotkeys = true;
        [SerializeField] private bool showDebugInfo = true;
        
        [Header("References")]
        [SerializeField] private GameManager gameManager;
        
        private PotSlot selectedPot;
        
        void Start()
        {
            // Trova il GameManager se non assegnato
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
                if (gameManager == null)
                {
                    Debug.LogWarning("[BLK-01.03B] GrowthDebugHotkeys: GameManager non trovato nella scena!");
                }
            }
            
            Debug.Log("[BLK-01.03B] GrowthDebugHotkeys inizializzato. Hotkeys disponibili:");
            Debug.Log("[BLK-01.03B] G = Simula End Day");
            Debug.Log("[BLK-01.03B] H = Annaffia vaso selezionato");
            Debug.Log("[BLK-01.03B] L = Illumina vaso selezionato");
            Debug.Log("[BLK-01.03B] P = Pianta su vaso selezionato");
        }
        
        void Update()
        {
            if (!enableDebugHotkeys) return;
            
            // G = Simula End Day
            if (Input.GetKeyDown(KeyCode.G))
            {
                SimulateEndDay();
            }
            
            // H = Annaffia vaso selezionato
            if (Input.GetKeyDown(KeyCode.H))
            {
                WaterSelectedPot();
            }
            
            // L = Illumina vaso selezionato
            if (Input.GetKeyDown(KeyCode.L))
            {
                LightSelectedPot();
            }
            
            // P = Pianta su vaso selezionato
            if (Input.GetKeyDown(KeyCode.P))
            {
                PlantSelectedPot();
            }
        }
        
        /// <summary>
        /// G = Simula End Day (raise evento)
        /// </summary>
        private void SimulateEndDay()
        {
            if (gameManager == null)
            {
                Debug.LogWarning("[BLK-01.03B] GameManager non disponibile per End Day");
                return;
            }
            
            Debug.Log("[BLK-01.03B] 🔄 Simulazione End Day...");
            gameManager.EndDay();
            Debug.Log($"[BLK-01.03B] ✅ End Day completato. Nuovo giorno: {gameManager.CurrentDay}");
        }
        
        /// <summary>
        /// H = Annaffia vaso selezionato
        /// </summary>
        private void WaterSelectedPot()
        {
            selectedPot = FindSelectedPot();
            if (selectedPot == null || selectedPot.PotActions == null)
            {
                Debug.LogWarning("[BLK-01.03B] ❌ Nessun vaso selezionato o PotActions mancante per annaffiare");
                return;
            }
            
            Debug.Log($"[BLK-01.03B] 💧 Tentativo annaffiatura vaso {selectedPot.PotId}...");
            bool success = selectedPot.PotActions.DoWater();
            
            if (success)
            {
                Debug.Log($"[BLK-01.03B] ✅ Vaso {selectedPot.PotId} annaffiato con successo!");
            }
            else
            {
                Debug.LogWarning($"[BLK-01.03B] ❌ Annaffiatura vaso {selectedPot.PotId} fallita!");
            }
        }
        
        /// <summary>
        /// L = Illumina vaso selezionato
        /// </summary>
        private void LightSelectedPot()
        {
            selectedPot = FindSelectedPot();
            if (selectedPot == null || selectedPot.PotActions == null)
            {
                Debug.LogWarning("[BLK-01.03B] ❌ Nessun vaso selezionato o PotActions mancante per illuminare");
                return;
            }
            
            Debug.Log($"[BLK-01.03B] 💡 Tentativo illuminazione vaso {selectedPot.PotId}...");
            bool success = selectedPot.PotActions.DoLight();
            
            if (success)
            {
                Debug.Log($"[BLK-01.03B] ✅ Vaso {selectedPot.PotId} illuminato con successo!");
            }
            else
            {
                Debug.LogWarning($"[BLK-01.03B] ❌ Illuminazione vaso {selectedPot.PotId} fallita!");
            }
        }
        
        /// <summary>
        /// P = Pianta su vaso selezionato
        /// </summary>
        private void PlantSelectedPot()
        {
            selectedPot = FindSelectedPot();
            if (selectedPot == null || selectedPot.PotActions == null)
            {
                Debug.LogWarning("[BLK-01.03B] ❌ Nessun vaso selezionato o PotActions mancante per piantare");
                return;
            }
            
            Debug.Log($"[BLK-01.03B] 🌱 Tentativo piantagione vaso {selectedPot.PotId}...");
            bool success = selectedPot.PotActions.DoPlant();
            
            if (success)
            {
                Debug.Log($"[BLK-01.03B] ✅ Vaso {selectedPot.PotId} piantato con successo!");
            }
            else
            {
                Debug.LogWarning($"[BLK-01.03B] ❌ Piantagione vaso {selectedPot.PotId} fallita!");
            }
        }
        
        /// <summary>
        /// Trova il vaso attualmente selezionato
        /// </summary>
        private PotSlot FindSelectedPot()
        {
            PotSlot[] allPots = FindObjectsOfType<PotSlot>();
            foreach (PotSlot pot in allPots)
            {
                if (pot.IsSelected)
                {
                    return pot;
                }
            }
            
            // Fallback: usa il primo vaso disponibile
            if (allPots.Length > 0)
            {
                Debug.LogWarning($"[BLK-01.03B] ⚠️ Nessun vaso selezionato. Usando primo vaso disponibile: {allPots[0].PotId}");
                return allPots[0];
            }
            
            return null;
        }
        
        /// <summary>
        /// Mostra informazioni di debug nella scena
        /// </summary>
        void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("[BLK-01.03B] Growth Debug Hotkeys", GUI.skin.box);
            GUILayout.Label("G = End Day");
            GUILayout.Label("H = Water pot");
            GUILayout.Label("L = Light pot");
            GUILayout.Label("P = Plant pot");
            
            if (selectedPot != null)
            {
                GUILayout.Label($"Vaso selezionato: {selectedPot.PotId}");
                if (selectedPot.PotActions != null)
                {
                    var state = selectedPot.PotActions.GetCurrentState();
                    if (state != null)
                    {
                        GUILayout.Label($"Stadio: {state.Stage}");
                        GUILayout.Label($"Punti crescita: {state.GrowthPoints}");
                    }
                }
            }
            else
            {
                GUILayout.Label("Nessun vaso selezionato");
            }
            
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// Disabilita gli hotkeys in build release
        /// </summary>
        void OnEnable()
        {
            #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            enableDebugHotkeys = false;
            showDebugInfo = false;
            Debug.Log("[BLK-01.03B] GrowthDebugHotkeys disabilitato in build release");
            #endif
        }
    }
}
