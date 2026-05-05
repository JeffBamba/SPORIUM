using _Project.Sporae.Core;
using UnityEngine;
using Sporae.Core;
using Sporae.Dome.PotSystem;
using Sporae.Dome.PotSystem.Growth;
using Sporae.DevTools;

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
        private DayCycleSystem _dayCycleSystem;

        private void Awake()
        {
            _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
        }
        
        private void Start()
        {
            // Trova il GameManager se non assegnato
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
                if (gameManager == null)
                {
                    SporiumLogger.LogWarning(LogCategory.Core, "GrowthDebugHotkeys: GameManager non trovato nella scena!");
                }
            }
            
            SporiumLogger.LogInfo(LogCategory.Dome, "GrowthDebugHotkeys inizializzato. Hotkeys disponibili:");
            SporiumLogger.LogInfo(LogCategory.Dome, "G = Simula End Day");
            SporiumLogger.LogInfo(LogCategory.Dome, "H = Toggle sistema irrigazione (GDD AZ-11)");
            SporiumLogger.LogInfo(LogCategory.Dome, "L = Illumina vaso selezionato");
            SporiumLogger.LogInfo(LogCategory.Dome, "P = Pianta su vaso selezionato");
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
            if (_dayCycleSystem == null)
            {
                SporiumLogger.LogWarning(LogCategory.Dome, "DayCycleSystem non disponibile.");
                return;
            }

            if (!_dayCycleSystem.CanEndDay())
            {
                SporiumLogger.LogWarning(LogCategory.Dome, "End Day: CanEndDay false (CRY insufficienti). Nessun avanzamento.");
                return;
            }

            SporiumLogger.LogInfo(LogCategory.Dome, "🔄 End Day — stesso DayCycleSystem.EndDay() del letto (fade → giorno+1 al termine).");
            if (!_dayCycleSystem.EndDay())
            {
                SporiumLogger.LogWarning(LogCategory.Dome, "EndDay() ha restituito false.");
                return;
            }

            SporiumLogger.LogInfo(LogCategory.Dome, $"✅ Fade avviato. Giorno attuale finché non finisce il fade: {_dayCycleSystem.CurrentDay} (poi +1 e OnDayChanged).");
        }
        
        /// <summary>
        /// H = Toggle sistema irrigazione (GDD AZ-11 - Toggle Persistente)
        /// </summary>
        private void WaterSelectedPot()
        {
            selectedPot = FindSelectedPot();
            if (selectedPot == null || selectedPot.PotActions == null)
            {
                SporiumLogger.LogWarning(LogCategory.Pot, "❌ Nessun vaso selezionato o PotActions mancante per toggle irrigazione");
                return;
            }
            
            bool currentState = selectedPot.PotActions.IsWateringSystemOn();
            SporiumLogger.LogInfo(LogCategory.Pot, $"💧 Toggle sistema irrigazione vaso {selectedPot.PotId} (stato attuale: {(currentState ? "ON" : "OFF")})...");
            bool success = selectedPot.PotActions.DoWater();
            
            if (success)
            {
                bool newState = selectedPot.PotActions.IsWateringSystemOn();
                SporiumLogger.LogInfo(LogCategory.Pot, $"✅ Sistema irrigazione vaso {selectedPot.PotId} impostato a {(newState ? "ON" : "OFF")}!");
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.Pot, $"❌ Toggle sistema irrigazione vaso {selectedPot.PotId} fallito!");
            }
        }
        
        /// <summary>
        /// L = Illumina vaso selezionato
        /// </summary>
        private void LightSelectedPot()
        {
            selectedPot = FindSelectedPot();
            if (!selectedPot || !selectedPot.PotActions)
            {
                SporiumLogger.LogWarning(LogCategory.Pot, "❌ Nessun vaso selezionato o PotActions mancante per illuminare");
                return;
            }
            
            LedSystemState oldState = selectedPot.PotActions.GetLedSystemState();
            SporiumLogger.LogInfo(LogCategory.Pot, $"💡 Toggle LED sistema vaso {selectedPot.PotId} (stato attuale: {oldState})...");
            bool success = selectedPot.PotActions.DoLight((LedSystemState?)null);  // Toggle esplicito
            
            if (success)
            {
                LedSystemState newState = selectedPot.PotActions.GetLedSystemState();
                SporiumLogger.LogInfo(LogCategory.Pot, $"✅ LED sistema vaso {selectedPot.PotId}: {oldState} → {newState}");
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.Pot, "❌ Toggle LED sistema fallito!");
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
                SporiumLogger.LogWarning(LogCategory.Pot, "❌ Nessun vaso selezionato o PotActions mancante per piantare");
                return;
            }
            
            SporiumLogger.LogInfo(LogCategory.Pot, $"🌱 Tentativo piantagione vaso {selectedPot.PotId}...");
            bool success = selectedPot.PotActions.DoPlant();
            
            if (success)
            {
                SporiumLogger.LogInfo(LogCategory.Pot, $"✅ Vaso {selectedPot.PotId} piantato con successo!");
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.Pot, $"❌ Piantagione vaso {selectedPot.PotId} fallita!");
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
                SporiumLogger.LogWarning(LogCategory.Pot, $"⚠️ Nessun vaso selezionato. Usando primo vaso disponibile: {allPots[0].PotId}");
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
            GUILayout.Label("H = Toggle Watering");
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
            SporiumLogger.LogInfo(LogCategory.Dome, "GrowthDebugHotkeys disabilitato in build release");
            #endif
        }
    }
}
