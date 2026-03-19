using System.Collections.Generic;
using UnityEngine;
using Sporae.Dome.PotSystem.Growth;
using Sporae.Dome.PotSystem.Condition;
using Sporae.Dome.PotSystem.Mold;
using Sporae.Dome.PotSystem.Level;
using _Project;
using _Project.Sporae.Core;

namespace Sporae.DevTools
{
    /// <summary>
    /// Console di debug per i POT - Permette di editare gli stadi delle piante
    /// Tasto P per aprire/chiudere
    /// Solo per Editor/Development build
    /// </summary>
    public class PotDebugConsole : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugConsole = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.P;
        [SerializeField] private bool showOnStart = false;
        
        private bool _isConsoleOpen = false;
        private Vector2 _scrollPosition;
        private Vector2 _potListScrollPosition;
        private Vector2 _editingScrollPosition; // Scroll per sezione editing POT
        private List<string> _debugLog = new List<string>();
        private const int MAX_LOG_ENTRIES = 50;
        
        // POT selezionato per editing
        private PotActions _selectedPot = null;
        private string _selectedPotId = "";
        
        // Lista di tutti i POT nella scena
        private PotActions[] _allPots = new PotActions[0];
        
        // Valore stadio da impostare
        private int _stageInputValue = 0;
        private string _stageInputString = "0";
        
        // BLK-03.01-T2: Valori debug per Fertilizzante, Watering e Light Stress
        private int _fertilizerInputValue = 0;
        private string _fertilizerInputString = "0";
        private int _hydrationInputValue = 0;
        private string _hydrationInputString = "0";
        private int _lightStressPercentInputValue = 0;
        private string _lightStressPercentInputString = "0";
        
        // Valori debug per nuovi sistemi
        private int _plantLevelInputValue = 1;
        private string _plantLevelInputString = "1";
        private int _completedCyclesInputValue = 0;
        private string _completedCyclesInputString = "0";
        private int _moldRiskLevelInputValue = 0;
        private string _moldRiskLevelInputString = "0";
        private int _conditionScoreInputValue = 50;
        private string _conditionScoreInputString = "50";
        private int _ledStateInputValue = 0; // 0=Off, 1=Blue, 2=Red
        private int _ledDaysInputValue = 0;
        private string _ledDaysInputString = "0";
        private bool _wateringSystemToggle = false;
        
        // Valori debug per FRUITS
        private float _amountFruitsInputValue = 0f;
        private string _amountFruitsInputString = "0";
        private int _maxFruitsInputValue = 3;
        private string _maxFruitsInputString = "3";
        private float _fruitQualityInputValue = 0f;
        private string _fruitQualityInputString = "0";
        
        // Cache per sistemi
        private PhSystem _phSystem;
        private DayCycleSystem _dayCycleSystem;
        private PotSystemConfig _potConfig;
        private PlantLevelConfig _plantLevelConfig;
        private MoldConfig _moldConfig;
        
        // Rettangolo della console per bloccare input
        private Rect _consoleRect;
        
        // Proprietà statica per verificare se la console è aperta (usata da PlayerClickMover2D)
        private static PotDebugConsole _instance;
        public static bool IsConsoleOpen => _instance != null && _instance._isConsoleOpen;
        
        // Verifica se il mouse è dentro l'area della console
        public static bool IsMouseOverConsole()
        {
            if (!IsConsoleOpen || _instance == null) return false;
            
            // OnGUI usa coordinate con (0,0) in alto a sinistra
            // Input.mousePosition ha (0,0) in basso a sinistra
            Vector2 mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y; // Converti Y per OnGUI
            
            return _instance._consoleRect.Contains(mousePos);
        }
        
        private void Awake()
        {
            _instance = this;
            _isConsoleOpen = showOnStart;
            
            #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            enableDebugConsole = false;
            #endif
            
            SporiumLogger.LogDebug(LogCategory.Dome, $"Awake - enableDebugConsole: {enableDebugConsole}, toggleKey: {toggleKey}, showOnStart: {showOnStart}");
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
        
        private void Start()
        {
            RefreshPotList();
            LoadConfigs();
            AddLog("=== Pot Debug Console ===");
            AddLog("Premi P per aprire/chiudere la console");
            AddLog($"Trovati {_allPots.Length} POT nella scena");
        }
        
        private void LoadConfigs()
        {
            // Carica configurazioni
            _potConfig = Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");
            _plantLevelConfig = Resources.Load<PlantLevelConfig>("Configs/PlantLevelConfig");
            _moldConfig = Resources.Load<MoldConfig>("Configs/MoldConfig");
            
            // Ottieni PhSystem
            try
            {
                var serviceContainer = ServiceContainer.Instance;
                if (serviceContainer != null)
                {
                    _phSystem = serviceContainer.Get<PhSystem>(suppressWarning: true);
                    _dayCycleSystem = serviceContainer.Get<DayCycleSystem>(suppressWarning: true);
                }
            }
            catch
            {
                // Sistemi non disponibili
            }
        }
        
        private void Update()
        {
            if (!enableDebugConsole)
            {
                return;
            }
            
            if (Input.GetKeyDown(toggleKey))
            {
                _isConsoleOpen = !_isConsoleOpen;
                AddLog(_isConsoleOpen ? "Console aperta" : "Console chiusa");
                SporiumLogger.LogDebug(LogCategory.Dome, $"Console {(_isConsoleOpen ? "aperta" : "chiusa")} - Tasto {toggleKey} premuto");
                
                if (_isConsoleOpen)
                {
                    RefreshPotList();
                }
            }
            
            // Hotkeys rapide (solo se console aperta)
            if (_isConsoleOpen && _selectedPot != null)
            {
                // 0-6: Cambia stadio direttamente
                if (Input.GetKeyDown(KeyCode.Alpha0)) SetPotStage((int)PlantStage.Empty);
                if (Input.GetKeyDown(KeyCode.Alpha1)) SetPotStage((int)PlantStage.Seed);
                if (Input.GetKeyDown(KeyCode.Alpha2)) SetPotStage((int)PlantStage.Sprout);
                if (Input.GetKeyDown(KeyCode.Alpha3)) SetPotStage((int)PlantStage.Growth);
                if (Input.GetKeyDown(KeyCode.Alpha4)) SetPotStage((int)PlantStage.Flowering);
                if (Input.GetKeyDown(KeyCode.Alpha5)) SetPotStage((int)PlantStage.HarvestReady);
                if (Input.GetKeyDown(KeyCode.Alpha6)) SetPotStage((int)PlantStage.Resting);
                
                // R: Refresh lista POT
                if (Input.GetKeyDown(KeyCode.R))
                {
                    RefreshPotList();
                    AddLog("Lista POT aggiornata");
                }
            }
        }
        
        private void RefreshPotList()
        {
            _allPots = FindObjectsOfType<PotActions>();
            AddLog($"Lista POT aggiornata: {_allPots.Length} POT trovati");
        }
        
        private void SetPotStage(int newStage)
        {
            if (_selectedPot == null || _selectedPot.PotState == null)
            {
                AddLog("⚠️ Nessun POT selezionato!");
                return;
            }
            
            var potState = _selectedPot.PotState;
            int oldStage = potState.Stage;
            
            // Verifica che il POT abbia una pianta (tranne se si vuole impostare Empty)
            if (newStage != (int)PlantStage.Empty && !potState.HasPlant)
            {
                AddLog($"⚠️ {potState.PotId}: Nessuna pianta nel vaso! Impossibile impostare stadio {newStage}");
                return;
            }
            
            // Imposta nuovo stadio
            potState.Stage = newStage;
            
            // Se si imposta Empty, resetta anche HasPlant
            if (newStage == (int)PlantStage.Empty)
            {
                potState.HasPlant = false;
                potState.PlantCode = null;
            }
            else
            {
                potState.HasPlant = true;
            }
            
            // Reset contatori se necessario
            if (newStage == (int)PlantStage.HarvestReady)
            {
                potState.DaysInHarvestReady = 0;
                potState.DaysFruitsUnharvested = 0;
            }
            else
            {
                potState.DaysInHarvestReady = 0;
                potState.DaysFruitsUnharvested = 0;
            }
            
            // Notifica cambio stadio
            var potGrowthController = _selectedPot.GetComponent<PotGrowthController>();
            if (potGrowthController != null)
            {
                potGrowthController.OnStageChanged((PlantStage)newStage);
            }
            
            PotEvents.EmitPlantStageChanged(potState.PotId, (PlantStage)newStage);
            PotEvents.EmitChanged(_selectedPot.PotSlot);
            
            AddLog($"✅ {potState.PotId}: Stadio cambiato {oldStage} → {newStage} ({GetStageName(newStage)})");
            SporiumLogger.LogInfo(LogCategory.Pot, $"{potState.PotId}: Stadio cambiato {oldStage} → {newStage}");
        }
        
        private string GetStageName(int stage)
        {
            return ((PlantStage)stage).ToString();
        }
        
        // BLK-03.01-T2: Imposta livello fertilizzante
        private void SetFertilizerLevel(int newLevel)
        {
            if (_selectedPot == null || _selectedPot.PotState == null)
            {
                AddLog("⚠️ Nessun POT selezionato!");
                return;
            }
            
            var potState = _selectedPot.PotState;
            int oldLevel = potState.FertilizerLevel;
            potState.FertilizerLevel = Mathf.Clamp(newLevel, 0, 100);
            
            // BUG FIX: Marca come impostato manualmente e salva valore base per decay
            potState.IsFertilizerManuallySet = true;
            potState.ManualFertilizerBase = potState.FertilizerLevel;
            
            // BUG FIX: Forza aggiornamento UI tramite eventi
            if (_selectedPot.PotSlot != null)
            {
                PotEvents.EmitChanged(_selectedPot.PotSlot);
                // Le HUD sempre visibili si aggiornano automaticamente tramite eventi
            }
            
            AddLog($"✅ {potState.PotId}: Fertilizzante cambiato {oldLevel}% → {potState.FertilizerLevel}% (MANUALE - decay partirà da questo valore)");
            SporiumLogger.LogInfo(LogCategory.Pot, $"{potState.PotId}: Fertilizzante cambiato {oldLevel}% → {potState.FertilizerLevel}% (MANUALE)");
        }
        
        // BLK-03.01-T2: Imposta idratazione
        private void SetHydration(int newHydration)
        {
            if (_selectedPot == null || _selectedPot.PotState == null)
            {
                AddLog("⚠️ Nessun POT selezionato!");
                return;
            }
            
            var potState = _selectedPot.PotState;
            int maxHydration = _selectedPot.GetMaxHydration();
            int oldHydration = potState.Hydration;
            potState.Hydration = Mathf.Clamp(newHydration, 0, maxHydration);
            
            // BUG FIX: Marca come impostato manualmente e salva valore base per decay
            potState.IsHydrationManuallySet = true;
            potState.ManualHydrationBase = potState.Hydration;
            
            // BUG FIX: Forza aggiornamento UI tramite eventi
            if (_selectedPot.PotSlot != null)
            {
                PotEvents.EmitChanged(_selectedPot.PotSlot);
                // Le HUD sempre visibili si aggiornano automaticamente tramite eventi
            }
            
            AddLog($"✅ {potState.PotId}: Idratazione cambiata {oldHydration}/{maxHydration} → {potState.Hydration}/{maxHydration} (MANUALE - decay partirà da questo valore)");
            SporiumLogger.LogInfo(LogCategory.Pot, $"{potState.PotId}: Idratazione cambiata {oldHydration}/{maxHydration} → {potState.Hydration}/{maxHydration} (MANUALE)");
        }
        
        // BUG FIX: Imposta Light Stress percentuale (come visualizzato nella HUD)
        private void SetLightStressPercent(int newStressPercent)
        {
            if (_selectedPot == null || _selectedPot.PotState == null)
            {
                AddLog("⚠️ Nessun POT selezionato!");
                return;
            }
            
            var potState = _selectedPot.PotState;
            int oldConsecutiveDays = potState.GetConsecutiveLedDays();
            
            // Calcola giorni consecutivi necessari per raggiungere la percentuale di stress desiderata
            // Formula: consecutiveDays = (stressPercent / 100f) * maxDaysForFullStress
            int maxDaysForFullStress = _selectedPot?.GetMaxDaysForFullStress() ?? 5;
            int newConsecutiveDays = Mathf.RoundToInt((newStressPercent / 100f) * maxDaysForFullStress);
            newConsecutiveDays = Mathf.Clamp(newConsecutiveDays, 0, maxDaysForFullStress);
            
            // Calcola percentuale effettiva dopo clamp
            float actualStressPercent = Mathf.Clamp01((float)newConsecutiveDays / maxDaysForFullStress) * 100f;
            
            // Imposta i giorni consecutivi in base allo stato LED corrente
            if (potState.LedSystemState == LedSystemState.Blue)
            {
                potState.DaysLedBlueConsecutive = newConsecutiveDays;
            }
            else if (potState.LedSystemState == LedSystemState.Red)
            {
                potState.DaysLedRedConsecutive = newConsecutiveDays;
            }
            else
            {
                // Se LED è spento, imposta entrambi (GetConsecutiveLedDays ritorna il massimo)
                potState.DaysLedBlueConsecutive = newConsecutiveDays;
                potState.DaysLedRedConsecutive = newConsecutiveDays;
            }
            
            // BUG FIX: Assicurati che PotSlot non sia null prima di emettere evento
            if (_selectedPot.PotSlot != null)
            {
                PotEvents.EmitChanged(_selectedPot.PotSlot);
                // Le HUD sempre visibili si aggiornano automaticamente tramite eventi
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.Pot, $"PotSlot è null per {potState.PotId}! Impossibile emettere evento OnPotStateChanged.");
                AddLog($"⚠️ PotSlot null - evento non emesso");
            }
            
            AddLog($"✅ {potState.PotId}: Light Stress cambiato {oldConsecutiveDays} giorni ({Mathf.RoundToInt((float)oldConsecutiveDays / maxDaysForFullStress * 100f)}%) → {newConsecutiveDays} giorni ({actualStressPercent:F0}%)");
            SporiumLogger.LogInfo(LogCategory.Pot, $"{potState.PotId}: Light Stress cambiato {oldConsecutiveDays} giorni → {newConsecutiveDays} giorni ({actualStressPercent:F0}%)");
        }
        
        // Metodi per nuovi sistemi
        private void SetPlantLevel(int newLevel)
        {
            if (_selectedPot == null || _selectedPot.PotState == null)
            {
                AddLog("⚠️ Nessun POT selezionato!");
                return;
            }
            
            var potState = _selectedPot.PotState;
            int oldLevel = potState.PlantLevel;
            potState.PlantLevel = Mathf.Clamp(newLevel, 1, 5);
            
            PotEvents.EmitChanged(_selectedPot.PotSlot);
            AddLog($"✅ {potState.PotId}: Livello cambiato {oldLevel} → {potState.PlantLevel}");
        }
        
        private void SetCompletedCycles(int newCycles)
        {
            if (_selectedPot == null || _selectedPot.PotState == null)
            {
                AddLog("⚠️ Nessun POT selezionato!");
                return;
            }
            
            var potState = _selectedPot.PotState;
            int oldCycles = potState.CompletedCycles;
            potState.CompletedCycles = Mathf.Max(0, newCycles);
            
            PotEvents.EmitChanged(_selectedPot.PotSlot);
            AddLog($"✅ {potState.PotId}: Cicli completati cambiati {oldCycles} → {potState.CompletedCycles}");
        }
        
        private void ForceLevelUp()
        {
            if (_selectedPot == null || _selectedPot.PotState == null)
            {
                AddLog("⚠️ Nessun POT selezionato!");
                return;
            }
            
            var potState = _selectedPot.PotState;
            if (potState.PlantLevel >= 5)
            {
                AddLog("⚠️ Livello massimo già raggiunto (Lvl 5)!");
                return;
            }
            
            int oldLevel = potState.PlantLevel;
            potState.PlantLevel++;
            
            PotEvents.EmitChanged(_selectedPot.PotSlot);
            AddLog($"✅ {potState.PotId}: Livello aumentato {oldLevel} → {potState.PlantLevel} (Force Level Up)");
        }
        
        private void SetMoldRiskLevel(int newLevel)
        {
            if (_selectedPot == null || _selectedPot.PotState == null)
            {
                AddLog("⚠️ Nessun POT selezionato!");
                return;
            }
            
            var potState = _selectedPot.PotState;
            int oldLevel = potState.MoldRiskLevel;
            potState.MoldRiskLevel = Mathf.Clamp(newLevel, 0, 3);
            
            PotEvents.EmitChanged(_selectedPot.PotSlot);
            AddLog($"✅ {potState.PotId}: Mold Risk Level cambiato {oldLevel} → {potState.MoldRiskLevel}");
        }
        
        private void SetConditionScore(int newScore)
        {
            if (_selectedPot == null || _selectedPot.PotState == null)
            {
                AddLog("⚠️ Nessun POT selezionato!");
                return;
            }
            
            var potState = _selectedPot.PotState;
            int oldScore = potState.ConditionScore;
            potState.ConditionScore = Mathf.Clamp(newScore, 0, 100);
            
            PotEvents.EmitChanged(_selectedPot.PotSlot);
            AddLog($"✅ {potState.PotId}: Condition Score cambiato {oldScore} → {potState.ConditionScore}");
        }
        
        private void SetLedSystemState(LedSystemState newState)
        {
            if (_selectedPot == null || _selectedPot.PotState == null)
            {
                AddLog("⚠️ Nessun POT selezionato!");
                return;
            }
            
            var potState = _selectedPot.PotState;
            LedSystemState oldState = potState.LedSystemState;
            potState.SetLedSystemState(newState);
            
            PotEvents.EmitChanged(_selectedPot.PotSlot);
            AddLog($"✅ {potState.PotId}: LED State cambiato {oldState} → {newState}");
        }
        
        private void SetLedBlue(bool isOn)
        {
            if (_selectedPot == null || _selectedPot.PotState == null)
            {
                AddLog("⚠️ Nessun POT selezionato!");
                return;
            }
            
            var potState = _selectedPot.PotState;
            LedSystemState newState = isOn ? LedSystemState.Blue : LedSystemState.Off;
            LedSystemState oldState = potState.LedSystemState;
            
            // Se Red è attivo, spegnilo prima
            if (isOn && potState.LedSystemState == LedSystemState.Red)
            {
                potState.SetLedSystemState(LedSystemState.Blue);
            }
            else if (!isOn && potState.LedSystemState == LedSystemState.Blue)
            {
                potState.SetLedSystemState(LedSystemState.Off);
            }
            else if (isOn)
            {
                potState.SetLedSystemState(LedSystemState.Blue);
            }
            
            // Aggiorna le luci Unity di scena
            var ledLightController = _selectedPot.GetComponent<LedLightController>();
            if (ledLightController != null)
            {
                ledLightController.UpdateLights(potState.LedSystemState);
            }
            
            PotEvents.EmitChanged(_selectedPot.PotSlot);
            AddLog($"✅ {potState.PotId}: LED Blue {(isOn ? "ON" : "OFF")} (State: {oldState} → {potState.LedSystemState})");
        }
        
        private void SetLedRed(bool isOn)
        {
            if (_selectedPot == null || _selectedPot.PotState == null)
            {
                AddLog("⚠️ Nessun POT selezionato!");
                return;
            }
            
            var potState = _selectedPot.PotState;
            LedSystemState newState = isOn ? LedSystemState.Red : LedSystemState.Off;
            LedSystemState oldState = potState.LedSystemState;
            
            // Se Blue è attivo, spegnilo prima
            if (isOn && potState.LedSystemState == LedSystemState.Blue)
            {
                potState.SetLedSystemState(LedSystemState.Red);
            }
            else if (!isOn && potState.LedSystemState == LedSystemState.Red)
            {
                potState.SetLedSystemState(LedSystemState.Off);
            }
            else if (isOn)
            {
                potState.SetLedSystemState(LedSystemState.Red);
            }
            
            // Aggiorna le luci Unity di scena
            var ledLightController = _selectedPot.GetComponent<LedLightController>();
            if (ledLightController != null)
            {
                ledLightController.UpdateLights(potState.LedSystemState);
            }
            
            PotEvents.EmitChanged(_selectedPot.PotSlot);
            AddLog($"✅ {potState.PotId}: LED Red {(isOn ? "ON" : "OFF")} (State: {oldState} → {potState.LedSystemState})");
        }
        
        private void SetAmountFruits(float newAmount)
        {
            if (_selectedPot == null || _selectedPot.PotState == null)
            {
                AddLog("⚠️ Nessun POT selezionato!");
                return;
            }
            
            var potState = _selectedPot.PotState;
            float oldAmount = potState.AmountFruits;
            potState.AmountFruits = Mathf.Clamp(newAmount, 0f, _maxFruitsInputValue);
            
            PotEvents.EmitChanged(_selectedPot.PotSlot);
            AddLog($"✅ {potState.PotId}: Frutti presenti cambiati {oldAmount:F1} → {potState.AmountFruits:F1}");
        }
        
        private void SetLedDays(int newDays)
        {
            if (_selectedPot == null || _selectedPot.PotState == null)
            {
                AddLog("⚠️ Nessun POT selezionato!");
                return;
            }
            
            var potState = _selectedPot.PotState;
            if (potState.LedSystemState == LedSystemState.Blue)
            {
                potState.DaysLedBlueConsecutive = Mathf.Max(0, newDays);
            }
            else if (potState.LedSystemState == LedSystemState.Red)
            {
                potState.DaysLedRedConsecutive = Mathf.Max(0, newDays);
            }
            
            PotEvents.EmitChanged(_selectedPot.PotSlot);
            AddLog($"✅ {potState.PotId}: LED Days consecutivi cambiati a {newDays}");
        }
        
        private void ToggleWateringSystem()
        {
            if (_selectedPot == null || _selectedPot.PotState == null)
            {
                AddLog("⚠️ Nessun POT selezionato!");
                return;
            }
            
            var potState = _selectedPot.PotState;
            potState.WateringSystemOn = !potState.WateringSystemOn;
            
            PotEvents.EmitChanged(_selectedPot.PotSlot);
            AddLog($"✅ {potState.PotId}: Watering System {(potState.WateringSystemOn ? "ON" : "OFF")}");
        }
        
        private void ResetGrowthPoints()
        {
            if (_selectedPot == null || _selectedPot.PotState == null)
            {
                AddLog("⚠️ Nessun POT selezionato!");
                return;
            }
            
            var potState = _selectedPot.PotState;
            potState.GrowthPointsWater = 0;
            potState.GrowthPointsLight = 0;
            potState.GrowthPointsFertilizer = 0;
            potState.DaysConsecutiveOptimal = 0;
            potState.DayOptimalParametersStarted = -1;
            
            PotEvents.EmitChanged(_selectedPot.PotSlot);
            AddLog($"✅ {potState.PotId}: Growth Points resettati");
        }
        
        private void RemoveMoldInfestation()
        {
            if (_selectedPot == null || _selectedPot.PotState == null)
            {
                AddLog("⚠️ Nessun POT selezionato!");
                return;
            }
            
            var potState = _selectedPot.PotState;
            MoldSystem.RemoveInfestation(potState);
            
            PotEvents.EmitChanged(_selectedPot.PotSlot);
            AddLog($"✅ {potState.PotId}: Infestazione muffe rimossa");
        }
        
        private void DebugPlantSeed(string plantCode)
        {
            if (_selectedPot == null || _selectedPot.PotState == null)
            {
                AddLog("⚠️ Nessun POT selezionato!");
                return;
            }
            
            var potState = _selectedPot.PotState;
            
            if (potState.HasPlant)
            {
                AddLog($"⚠️ {potState.PotId}: Già occupato! Imposta prima stadio Empty.");
                return;
            }
            
            PlantData plantData = PlantDatabase.Instance?.GetPlantDataByCode(plantCode);
            if (plantData == null)
            {
                AddLog($"⚠️ PlantData non trovato per codice: {plantCode}");
                return;
            }
            
            int currentDay = _dayCycleSystem?.CurrentDay ?? 1;
            potState.PlantSeed(currentDay, plantCode);
            potState.ApplySeedMetadata(null, plantData);
            
            var potGrowthController = _selectedPot.GetComponent<PotGrowthController>();
            potGrowthController?.UpdateVisuals();
            
            PotEvents.EmitPlantStageChanged(potState.PotId, PlantStage.Seed);
            PotEvents.EmitChanged(_selectedPot.PotSlot);
            
            AddLog($"✅ {potState.PotId}: Piantato {plantData.name} ({plantCode}) — Giorno {currentDay}");
        }
        
        private void AddLog(string message)
        {
            _debugLog.Add($"[{System.DateTime.Now:HH:mm:ss}] {message}");
            if (_debugLog.Count > MAX_LOG_ENTRIES)
            {
                _debugLog.RemoveAt(0);
            }
        }
        
        private void OnGUI()
        {
            if (!enableDebugConsole || !_isConsoleOpen) return;
            
            // Stile della console
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.95f));
            
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 18; // Aumentato da 12 a 18 per migliore leggibilità
            labelStyle.normal.textColor = Color.white;
            labelStyle.wordWrap = true; // Permette il wrapping del testo
            
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 16; // Aumentato da 11 a 16 per migliore leggibilità
            buttonStyle.normal.textColor = Color.white;
            
            // Area console principale (aumentata per testo più grande)
            float consoleWidth = 700f; // Aumentato da 600 a 700
            float consoleHeight = Screen.height * 0.85f; // Aumentato da 0.8 a 0.85
            float consoleX = Screen.width - consoleWidth - 10f;
            float consoleY = 10f;
            
            // Salva il rettangolo della console per bloccare input
            _consoleRect = new Rect(consoleX, consoleY, consoleWidth, consoleHeight);
            
            GUI.Box(_consoleRect, "", boxStyle);
            
            // Titolo
            GUI.Label(new Rect(consoleX + 10f, consoleY + 10f, consoleWidth - 20f, 35f), 
                "🌱 POT DEBUG CONSOLE", labelStyle);
            
            float currentY = consoleY + 50f; // Aumentato spazio per titolo più grande
            
            // Sezione selezione POT
            GUI.Label(new Rect(consoleX + 10f, currentY, 250f, 25f), "Seleziona POT:", labelStyle);
            currentY += 30f; // Aumentato spazio tra elementi
            
            // Lista POT scrollabile
            float listHeight = 180f; // Aumentato per pulsanti più grandi
            _potListScrollPosition = GUI.BeginScrollView(
                new Rect(consoleX + 10f, currentY, consoleWidth - 20f, listHeight),
                _potListScrollPosition,
                new Rect(0, 0, consoleWidth - 40f, _allPots.Length * 30f + 10f)); // Aumentato da 25f a 30f
            
            for (int i = 0; i < _allPots.Length; i++)
            {
                var pot = _allPots[i];
                if (pot == null || pot.PotState == null) continue;
                
                var potState = pot.PotState;
                bool isSelected = (_selectedPot == pot);
                
                string potInfo = $"{potState.PotId}";
                if (potState.HasPlant)
                {
                    potInfo += $" - Stage: {potState.Stage} ({GetStageName(potState.Stage)})";
                    if (!string.IsNullOrEmpty(potState.PlantCode))
                    {
                        potInfo += $" - {potState.PlantCode}";
                    }
                    if (potState.Stage == (int)PlantStage.HarvestReady && potState.AmountFruits > 0f)
                    {
                        potInfo += $" - Frutti: {potState.AmountFruits:F1}";
                    }
                }
                else
                {
                    potInfo += " - Vuoto";
                }
                
                if (GUI.Button(new Rect(0, i * 30f, consoleWidth - 40f, 28f), potInfo, buttonStyle)) // Aumentato da 25f/23f a 30f/28f
                {
                    _selectedPot = pot;
                    _editingScrollPosition = Vector2.zero; // Reset scroll così sono visibili Nuovo Stadio e pulsanti stadi
                    _selectedPotId = potState.PotId;
                    _stageInputValue = potState.Stage;
                    _stageInputString = potState.Stage.ToString();
                    // BLK-03.01-T2: Inizializza valori debug quando si seleziona un POT
                    _fertilizerInputValue = potState.FertilizerLevel;
                    _fertilizerInputString = potState.FertilizerLevel.ToString();
                    _hydrationInputValue = potState.Hydration;
                    _hydrationInputString = potState.Hydration.ToString();
                    // Calcola Light Stress percentuale da giorni consecutivi LED (come nella HUD)
                    int consecutiveDays = potState.GetConsecutiveLedDays();
                    int maxDaysForFullStress = _selectedPot?.GetMaxDaysForFullStress() ?? 5;
                    float stressPercentage = Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f;
                    _lightStressPercentInputValue = Mathf.RoundToInt(stressPercentage);
                    _lightStressPercentInputString = _lightStressPercentInputValue.ToString();
                    
                    // Inizializza valori nuovi sistemi
                    _plantLevelInputValue = potState.PlantLevel;
                    _plantLevelInputString = potState.PlantLevel.ToString();
                    _completedCyclesInputValue = potState.CompletedCycles;
                    _completedCyclesInputString = potState.CompletedCycles.ToString();
                    _moldRiskLevelInputValue = potState.MoldRiskLevel;
                    _moldRiskLevelInputString = potState.MoldRiskLevel.ToString();
                    _conditionScoreInputValue = potState.ConditionScore;
                    _conditionScoreInputString = potState.ConditionScore.ToString();
                    _ledStateInputValue = (int)potState.LedSystemState;
                    _ledDaysInputValue = potState.GetConsecutiveLedDays();
                    _ledDaysInputString = _ledDaysInputValue.ToString();
                    _wateringSystemToggle = potState.WateringSystemOn;
                    
                    // Inizializza valori FRUITS
                    _amountFruitsInputValue = potState.AmountFruits;
                    _amountFruitsInputString = _amountFruitsInputValue.ToString("F1");
                    _maxFruitsInputValue = 3; // Max fisso a 3
                    _maxFruitsInputString = "3";
                    
                    // Calcola qualità attesa frutti
                    string fruitTypeId = ItemFabric.ResolveFruitTypeIdForPlant(potState.PlantCode, potState.PlantFamilyMetadata);
                    ItemConfig fruitConfig = Resources.Load<ItemConfig>("Items/" + fruitTypeId);
                    if (fruitConfig != null)
                    {
                        float baseQuality = fruitConfig.MaxQuality;
                        float expectedQuality = baseQuality;
                        if (_plantLevelConfig != null && potState.PlantLevel >= 3)
                        {
                            float qualityModifier = _plantLevelConfig.GetQualityModifier(potState.PlantLevel);
                            expectedQuality = baseQuality * (1f + qualityModifier / 100f);
                            expectedQuality = Mathf.Clamp(expectedQuality, baseQuality, baseQuality * 2f);
                        }
                        _fruitQualityInputValue = expectedQuality;
                        _fruitQualityInputString = expectedQuality.ToString("F1");
                    }
                    else
                    {
                        _fruitQualityInputValue = 0f;
                        _fruitQualityInputString = "0";
                    }
                    
                    AddLog($"POT selezionato: {potState.PotId}");
                }
            }
            
            GUI.EndScrollView();
            currentY += listHeight + 15f; // Aumentato spazio
            
            // Sezione editing POT selezionato - SCROLLABILE
            if (_selectedPot != null && _selectedPot.PotState != null)
            {
                var potState = _selectedPot.PotState;
                
                // Calcola altezza disponibile per sezione editing (spazio rimanente prima dei log)
                float editingSectionStartY = currentY;
                float logSectionHeight = 120f; // Spazio riservato per log (aumentato da 100f)
                float logSectionStartY = consoleY + consoleHeight - logSectionHeight; // Posizione assoluta
                float availableEditingHeight = logSectionStartY - editingSectionStartY - 15f; // Margine ridotto
                
                // Altezza totale contenuto editing (calcolata sommando tutti gli elementi)
                // Titolo: 30, Info: 75, Input Stadio: 35, Debug Parametri: 3*35+40=145, 
                // Plant Condition: 30+30+35=95, Mold: 30+30+30+35=125, Plant Level: 30+30+30+35=125,
                // pH Affinity: 30+~170=200, Growth Points: 30+30+35=95, LED: 30+30+30+35=125, 
                // FRUITS: 30+35+35+40=140, Watering: 30+30+35=95, Pulsanti stadi: 30+30+40=100
                // Totale: ~1390px (aumentato per includere sezione FRUITS e margine)
                float editingContentHeight = 1530f;
                
                // Inizia scroll view per sezione editing
                _editingScrollPosition = GUI.BeginScrollView(
                    new Rect(consoleX + 10f, currentY, consoleWidth - 20f, availableEditingHeight),
                    _editingScrollPosition,
                    new Rect(0, 0, consoleWidth - 40f, editingContentHeight));
                
                // Variabile per posizione relativa all'interno dello scroll view
                float relativeY = 0f;
                
                GUI.Label(new Rect(10f, relativeY, consoleWidth - 40f, 25f), 
                    $"POT Selezionato: {potState.PotId}", labelStyle);
                relativeY += 30f; // Aumentato spazio
                
                // Info POT corrente
                int maxHydration = _selectedPot.GetMaxHydration();
                int maxLightExposure = _selectedPot.GetMaxLightExposure();
                float lightPercent = maxLightExposure > 0 ? (float)potState.LightExposure / maxLightExposure * 100f : 0f;
                
                string currentInfo = $"Stadio Attuale: {potState.Stage} ({GetStageName(potState.Stage)})";
                if (potState.HasPlant && !string.IsNullOrEmpty(potState.PlantCode))
                {
                    currentInfo += $"\nPlantCode: {potState.PlantCode}";
                }
                if (potState.Stage == (int)PlantStage.HarvestReady)
                {
                    currentInfo += $"\nFrutti: {potState.AmountFruits:F1}";
                }
                // BLK-03.01-T2: Aggiungi info Fertilizzante, Watering e Luce %
                currentInfo += $"\nFertilizzante: {potState.FertilizerLevel}%";
                currentInfo += $"\nIdratazione: {potState.Hydration}/{maxHydration}";
                currentInfo += $"\nLuce: {potState.LightExposure}/{maxLightExposure} ({lightPercent:F0}%)";
                
                GUI.Label(new Rect(10f, relativeY, consoleWidth - 40f, 70f), currentInfo, labelStyle);
                relativeY += 75f; // Aumentato spazio
                
                // === SEZIONE DEBUG: IMPIANTA SEME ===
                GUI.Label(new Rect(10f, relativeY, consoleWidth - 40f, 25f),
                    "=== Debug: Impianta Seme (senza inventario) ===", labelStyle);
                relativeY += 30f;
                
                if (potState.HasPlant)
                {
                    GUI.Label(new Rect(10f, relativeY, consoleWidth - 40f, 25f),
                        "POT occupato — imposta Empty prima di piantare", labelStyle);
                    relativeY += 30f;
                }
                else
                {
                    float seedBtnW = (consoleWidth - 30f) / 3f;
                    if (GUI.Button(new Rect(10f, relativeY, seedBtnW - 2f, 28f), "Ferric Fern\n(PLT-STD-001)", buttonStyle))
                        DebugPlantSeed("PLT-STD-001");
                    if (GUI.Button(new Rect(10f + seedBtnW, relativeY, seedBtnW - 2f, 28f), "Arctic Hask\n(PLT-PURE-001)", buttonStyle))
                        DebugPlantSeed("PLT-PURE-001");
                    if (GUI.Button(new Rect(10f + seedBtnW * 2f, relativeY, seedBtnW - 2f, 28f), "Glasscap Fungus\n(PLT-EVIL-001)", buttonStyle))
                        DebugPlantSeed("PLT-EVIL-001");
                    relativeY += 35f;
                }
                
                // Input stadio manuale
                GUI.Label(new Rect(10f, relativeY, 180f, 25f), "Nuovo Stadio (0-6):", labelStyle);
                _stageInputString = GUI.TextField(new Rect(170f, relativeY, 80f, 25f), _stageInputString);
                if (int.TryParse(_stageInputString, out int parsedStage))
                {
                    _stageInputValue = Mathf.Clamp(parsedStage, 0, 6);
                    _stageInputString = _stageInputValue.ToString();
                }
                if (GUI.Button(new Rect(260f, relativeY, 25f, 25f), "-", buttonStyle))
                {
                    _stageInputValue = Mathf.Max(0, _stageInputValue - 1);
                    _stageInputString = _stageInputValue.ToString();
                }
                if (GUI.Button(new Rect(290f, relativeY, 25f, 25f), "+", buttonStyle))
                {
                    _stageInputValue = Mathf.Min(6, _stageInputValue + 1);
                    _stageInputString = _stageInputValue.ToString();
                }
                if (GUI.Button(new Rect(325f, relativeY, 120f, 25f), "Imposta Stadio", buttonStyle))
                {
                    if (int.TryParse(_stageInputString, out int finalStage))
                    {
                        _stageInputValue = Mathf.Clamp(finalStage, 0, 6);
                        _stageInputString = _stageInputValue.ToString();
                    }
                    SetPotStage(_stageInputValue);
                }
                relativeY += 35f; // Aumentato spazio
                
                // BLK-03.01-T2: Sezione Debug Fertilizzante, Watering e Luce %
                GUI.Label(new Rect(10f, relativeY, consoleWidth - 40f, 25f), 
                    "=== Debug Parametri ===", labelStyle);
                relativeY += 30f;
                
                // Fertilizzante (0-100%)
                GUI.Label(new Rect(10f, relativeY, 150f, 25f), "Fertilizzante (0-100%):", labelStyle);
                _fertilizerInputString = GUI.TextField(new Rect(170f, relativeY, 80f, 25f), _fertilizerInputString);
                if (int.TryParse(_fertilizerInputString, out int parsedFertilizer))
                {
                    _fertilizerInputValue = Mathf.Clamp(parsedFertilizer, 0, 100);
                    _fertilizerInputString = _fertilizerInputValue.ToString();
                }
                if (GUI.Button(new Rect(260f, relativeY, 25f, 25f), "-", buttonStyle))
                {
                    _fertilizerInputValue = Mathf.Max(0, _fertilizerInputValue - 1);
                    _fertilizerInputString = _fertilizerInputValue.ToString();
                }
                if (GUI.Button(new Rect(290f, relativeY, 25f, 25f), "+", buttonStyle))
                {
                    _fertilizerInputValue = Mathf.Min(100, _fertilizerInputValue + 1);
                    _fertilizerInputString = _fertilizerInputValue.ToString();
                }
                if (GUI.Button(new Rect(325f, relativeY, 120f, 25f), "Imposta Fert.", buttonStyle))
                {
                    if (int.TryParse(_fertilizerInputString, out int finalFertilizer))
                    {
                        _fertilizerInputValue = Mathf.Clamp(finalFertilizer, 0, 100);
                        _fertilizerInputString = _fertilizerInputValue.ToString();
                    }
                    SetFertilizerLevel(_fertilizerInputValue);
                }
                // Mostra valore corrente
                GUI.Label(new Rect(455f, relativeY, 200f, 25f), 
                    $"Corrente: {potState.FertilizerLevel}%", labelStyle);
                relativeY += 35f;
                
                // Watering (0-MaxHydration)
                GUI.Label(new Rect(10f, relativeY, 150f, 25f), $"Watering (0-{maxHydration}):", labelStyle);
                _hydrationInputString = GUI.TextField(new Rect(170f, relativeY, 80f, 25f), _hydrationInputString);
                if (int.TryParse(_hydrationInputString, out int parsedHydration))
                {
                    _hydrationInputValue = Mathf.Clamp(parsedHydration, 0, maxHydration);
                    _hydrationInputString = _hydrationInputValue.ToString();
                }
                if (GUI.Button(new Rect(260f, relativeY, 25f, 25f), "-", buttonStyle))
                {
                    _hydrationInputValue = Mathf.Max(0, _hydrationInputValue - 1);
                    _hydrationInputString = _hydrationInputValue.ToString();
                }
                if (GUI.Button(new Rect(290f, relativeY, 25f, 25f), "+", buttonStyle))
                {
                    _hydrationInputValue = Mathf.Min(maxHydration, _hydrationInputValue + 1);
                    _hydrationInputString = _hydrationInputValue.ToString();
                }
                if (GUI.Button(new Rect(325f, relativeY, 120f, 25f), "Imposta Water", buttonStyle))
                {
                    if (int.TryParse(_hydrationInputString, out int finalHydration))
                    {
                        _hydrationInputValue = Mathf.Clamp(finalHydration, 0, maxHydration);
                        _hydrationInputString = _hydrationInputValue.ToString();
                    }
                    SetHydration(_hydrationInputValue);
                }
                // Mostra valore corrente
                GUI.Label(new Rect(455f, relativeY, 200f, 25f), 
                    $"Corrente: {potState.Hydration}/{maxHydration}", labelStyle);
                relativeY += 35f;
                
                // Light Stress (0-100%) - come visualizzato nella HUD
                GUI.Label(new Rect(10f, relativeY, 150f, 25f), "Light Stress (0-100%):", labelStyle);
                _lightStressPercentInputString = GUI.TextField(new Rect(170f, relativeY, 80f, 25f), _lightStressPercentInputString);
                if (int.TryParse(_lightStressPercentInputString, out int parsedLightStress))
                {
                    _lightStressPercentInputValue = Mathf.Clamp(parsedLightStress, 0, 100);
                    _lightStressPercentInputString = _lightStressPercentInputValue.ToString();
                }
                if (GUI.Button(new Rect(260f, relativeY, 25f, 25f), "-", buttonStyle))
                {
                    _lightStressPercentInputValue = Mathf.Max(0, _lightStressPercentInputValue - 1);
                    _lightStressPercentInputString = _lightStressPercentInputValue.ToString();
                }
                if (GUI.Button(new Rect(290f, relativeY, 25f, 25f), "+", buttonStyle))
                {
                    _lightStressPercentInputValue = Mathf.Min(100, _lightStressPercentInputValue + 1);
                    _lightStressPercentInputString = _lightStressPercentInputValue.ToString();
                }
                if (GUI.Button(new Rect(325f, relativeY, 120f, 25f), "Imposta Stress", buttonStyle))
                {
                    if (int.TryParse(_lightStressPercentInputString, out int finalLightStress))
                    {
                        _lightStressPercentInputValue = Mathf.Clamp(finalLightStress, 0, 100);
                        _lightStressPercentInputString = _lightStressPercentInputValue.ToString();
                    }
                    SetLightStressPercent(_lightStressPercentInputValue);
                }
                // Mostra valore corrente (calcolato come nella HUD)
                int currentConsecutiveDays = potState.GetConsecutiveLedDays();
                int maxDaysForFullStress = _selectedPot?.GetMaxDaysForFullStress() ?? 5;
                float currentStressPercent = Mathf.Clamp01((float)currentConsecutiveDays / maxDaysForFullStress) * 100f;
                GUI.Label(new Rect(455f, relativeY, 200f, 25f), 
                    $"Corrente: {currentStressPercent:F0}% ({currentConsecutiveDays}/{maxDaysForFullStress} giorni)", labelStyle);
                relativeY += 40f;
                
                // === SEZIONE PLANT CONDITION ===
                GUI.Label(new Rect(10f, relativeY, consoleWidth - 40f, 25f), 
                    "=== Plant Condition System ===", labelStyle);
                relativeY += 30f;
                
                string conditionName = GetConditionName((PlantCondition)potState.ConditionLabel);
                string forecastSymbol = GetForecastSymbol((ForecastDirection)potState.ForecastDirection);
                GUI.Label(new Rect(10f, relativeY, 200f, 25f), 
                    $"Score: {potState.ConditionScore}/100", labelStyle);
                GUI.Label(new Rect(220f, relativeY, 200f, 25f), 
                    $"Condizione: {conditionName}", labelStyle);
                GUI.Label(new Rect(430f, relativeY, 200f, 25f), 
                    $"Forecast: {forecastSymbol}", labelStyle);
                relativeY += 30f;
                
                GUI.Label(new Rect(10f, relativeY, 150f, 25f), "Set Score (0-100):", labelStyle);
                _conditionScoreInputString = GUI.TextField(new Rect(170f, relativeY, 80f, 25f), _conditionScoreInputString);
                if (int.TryParse(_conditionScoreInputString, out int parsedCondition))
                {
                    _conditionScoreInputValue = Mathf.Clamp(parsedCondition, 0, 100);
                    _conditionScoreInputString = _conditionScoreInputValue.ToString();
                }
                if (GUI.Button(new Rect(260f, relativeY, 25f, 25f), "-", buttonStyle))
                {
                    _conditionScoreInputValue = Mathf.Max(0, _conditionScoreInputValue - 1);
                    _conditionScoreInputString = _conditionScoreInputValue.ToString();
                }
                if (GUI.Button(new Rect(290f, relativeY, 25f, 25f), "+", buttonStyle))
                {
                    _conditionScoreInputValue = Mathf.Min(100, _conditionScoreInputValue + 1);
                    _conditionScoreInputString = _conditionScoreInputValue.ToString();
                }
                if (GUI.Button(new Rect(325f, relativeY, 120f, 25f), "Imposta Score", buttonStyle))
                {
                    if (int.TryParse(_conditionScoreInputString, out int finalCondition))
                    {
                        _conditionScoreInputValue = Mathf.Clamp(finalCondition, 0, 100);
                        _conditionScoreInputString = _conditionScoreInputValue.ToString();
                    }
                    SetConditionScore(_conditionScoreInputValue);
                }
                relativeY += 35f;
                
                // === SEZIONE MOLD SYSTEM ===
                GUI.Label(new Rect(10f, relativeY, consoleWidth - 40f, 25f), 
                    "=== Mold System ===", labelStyle);
                relativeY += 30f;
                
                string moldRiskName = GetMoldRiskName(potState.MoldRiskLevel);
                GUI.Label(new Rect(10f, relativeY, 200f, 25f), 
                    $"Mold Risk Level: {potState.MoldRiskLevel} ({moldRiskName})", labelStyle);
                GUI.Label(new Rect(220f, relativeY, 200f, 25f), 
                    $"Days Overwatering: {potState.DaysOverwateringConsecutive}", labelStyle);
                if (_moldConfig != null)
                {
                    GUI.Label(new Rect(430f, relativeY, 200f, 25f), 
                        $"Threshold: {_moldConfig.overwateringDaysThreshold} giorni", labelStyle);
                }
                relativeY += 30f;
                
                // NOTA: Mold Risk ora calcolato SOLO da overwatering (1 livello per ogni giorno oltre soglia)
                // Days w/o Pruning non influisce più sul calcolo
                GUI.Label(new Rect(10f, relativeY, 400f, 25f), 
                    $"Days w/o Pruning: {potState.DaysWithoutPruning} (non più usato per Mold Risk)", labelStyle);
                relativeY += 30f;
                
                // Calcola rischio in tempo reale se possibile
                if (_phSystem != null && _moldConfig != null && potState.HasPlant)
                {
                    var moldPlantData = potState.GetPlantData();
                    float moldRisk = MoldSystem.CalculateMoldRisk(potState, _phSystem, moldPlantData, _moldConfig);
                    int calculatedLevel = MoldSystem.GetMoldRiskLevel(potState, _phSystem, moldPlantData, _moldConfig);
                    int daysOverThreshold = Mathf.Max(0, potState.DaysOverwateringConsecutive - _moldConfig.overwateringDaysThreshold);
                    GUI.Label(new Rect(10f, relativeY, 400f, 25f), 
                        $"Rischio calcolato: {moldRisk:F1} (Level: {calculatedLevel}) - Giorni oltre soglia: {daysOverThreshold}", labelStyle);
                    relativeY += 30f;
                }
                
                GUI.Label(new Rect(10f, relativeY, 150f, 25f), "Set Mold Risk (0-3):", labelStyle);
                _moldRiskLevelInputString = GUI.TextField(new Rect(170f, relativeY, 80f, 25f), _moldRiskLevelInputString);
                if (int.TryParse(_moldRiskLevelInputString, out int parsedMold))
                {
                    _moldRiskLevelInputValue = Mathf.Clamp(parsedMold, 0, 3);
                    _moldRiskLevelInputString = _moldRiskLevelInputValue.ToString();
                }
                if (GUI.Button(new Rect(260f, relativeY, 25f, 25f), "-", buttonStyle))
                {
                    _moldRiskLevelInputValue = Mathf.Max(0, _moldRiskLevelInputValue - 1);
                    _moldRiskLevelInputString = _moldRiskLevelInputValue.ToString();
                }
                if (GUI.Button(new Rect(290f, relativeY, 25f, 25f), "+", buttonStyle))
                {
                    _moldRiskLevelInputValue = Mathf.Min(3, _moldRiskLevelInputValue + 1);
                    _moldRiskLevelInputString = _moldRiskLevelInputValue.ToString();
                }
                if (GUI.Button(new Rect(325f, relativeY, 120f, 25f), "Imposta Risk", buttonStyle))
                {
                    if (int.TryParse(_moldRiskLevelInputString, out int finalMold))
                    {
                        _moldRiskLevelInputValue = Mathf.Clamp(finalMold, 0, 3);
                        _moldRiskLevelInputString = _moldRiskLevelInputValue.ToString();
                    }
                    SetMoldRiskLevel(_moldRiskLevelInputValue);
                }
                if (GUI.Button(new Rect(455f, relativeY, 120f, 25f), "Rimuovi Infest.", buttonStyle))
                {
                    RemoveMoldInfestation();
                }
                relativeY += 35f;
                
                // === SEZIONE PLANT LEVEL ===
                GUI.Label(new Rect(10f, relativeY, consoleWidth - 40f, 25f), 
                    "=== Plant Level System ===", labelStyle);
                relativeY += 30f;
                
                int cyclesRequired = _plantLevelConfig != null ? 
                    _plantLevelConfig.GetCyclesRequired(potState.PlantLevel) : 999;
                float levelProgress = _plantLevelConfig != null ? 
                    PlantLevelSystem.GetLevelProgress(potState, _plantLevelConfig) : 0f;
                
                GUI.Label(new Rect(10f, relativeY, 200f, 25f), 
                    $"Livello: {potState.PlantLevel}/5", labelStyle);
                GUI.Label(new Rect(220f, relativeY, 200f, 25f), 
                    $"Cicli: {potState.CompletedCycles}/{cyclesRequired}", labelStyle);
                GUI.Label(new Rect(430f, relativeY, 200f, 25f), 
                    $"Progress: {levelProgress * 100f:F0}%", labelStyle);
                relativeY += 30f;
                
                GUI.Label(new Rect(10f, relativeY, 150f, 25f), "Set Level (1-5):", labelStyle);
                _plantLevelInputString = GUI.TextField(new Rect(170f, relativeY, 80f, 25f), _plantLevelInputString);
                if (int.TryParse(_plantLevelInputString, out int parsedLevel))
                {
                    _plantLevelInputValue = Mathf.Clamp(parsedLevel, 1, 5);
                    _plantLevelInputString = _plantLevelInputValue.ToString();
                }
                if (GUI.Button(new Rect(260f, relativeY, 25f, 25f), "-", buttonStyle))
                {
                    _plantLevelInputValue = Mathf.Max(1, _plantLevelInputValue - 1);
                    _plantLevelInputString = _plantLevelInputValue.ToString();
                }
                if (GUI.Button(new Rect(290f, relativeY, 25f, 25f), "+", buttonStyle))
                {
                    _plantLevelInputValue = Mathf.Min(5, _plantLevelInputValue + 1);
                    _plantLevelInputString = _plantLevelInputValue.ToString();
                }
                if (GUI.Button(new Rect(325f, relativeY, 120f, 25f), "Imposta Level", buttonStyle))
                {
                    if (int.TryParse(_plantLevelInputString, out int finalLevel))
                    {
                        _plantLevelInputValue = Mathf.Clamp(finalLevel, 1, 5);
                        _plantLevelInputString = _plantLevelInputValue.ToString();
                    }
                    SetPlantLevel(_plantLevelInputValue);
                }
                relativeY += 30f;
                
                GUI.Label(new Rect(10f, relativeY, 150f, 25f), "Set Cycles:", labelStyle);
                _completedCyclesInputString = GUI.TextField(new Rect(170f, relativeY, 80f, 25f), _completedCyclesInputString);
                if (int.TryParse(_completedCyclesInputString, out int parsedCycles))
                {
                    _completedCyclesInputValue = Mathf.Max(0, parsedCycles);
                    _completedCyclesInputString = _completedCyclesInputValue.ToString();
                }
                if (GUI.Button(new Rect(260f, relativeY, 25f, 25f), "-", buttonStyle))
                {
                    _completedCyclesInputValue = Mathf.Max(0, _completedCyclesInputValue - 1);
                    _completedCyclesInputString = _completedCyclesInputValue.ToString();
                }
                if (GUI.Button(new Rect(290f, relativeY, 25f, 25f), "+", buttonStyle))
                {
                    _completedCyclesInputValue = _completedCyclesInputValue + 1;
                    _completedCyclesInputString = _completedCyclesInputValue.ToString();
                }
                if (GUI.Button(new Rect(325f, relativeY, 120f, 25f), "Imposta Cycles", buttonStyle))
                {
                    if (int.TryParse(_completedCyclesInputString, out int finalCycles))
                    {
                        _completedCyclesInputValue = Mathf.Max(0, finalCycles);
                        _completedCyclesInputString = _completedCyclesInputValue.ToString();
                    }
                    SetCompletedCycles(_completedCyclesInputValue);
                }
                relativeY += 35f;
                
                // Modificatori Resa (Lvl X):
                if (_plantLevelConfig != null)
                {
                    float quantityModifier = _plantLevelConfig.GetQuantityModifier(potState.PlantLevel);
                    float qualityModifier = _plantLevelConfig.GetQualityModifier(potState.PlantLevel);
                    
                    GUI.Label(new Rect(10f, relativeY, 250f, 25f), 
                        $"Modificatori Resa (Lvl {potState.PlantLevel}):", labelStyle);
                    relativeY += 25f;
                    
                    GUI.Label(new Rect(20f, relativeY, 300f, 25f), 
                        $"  - Quantità: {quantityModifier:0}%", labelStyle);
                    relativeY += 25f;
                    
                    GUI.Label(new Rect(20f, relativeY, 300f, 25f), 
                        $"  - Qualità: +{qualityModifier:0}%", labelStyle);
                    relativeY += 30f;
                    
                    // Qualità frutti attesa
                    string levelFruitTypeId = ItemFabric.ResolveFruitTypeIdForPlant(potState.PlantCode, potState.PlantFamilyMetadata);
                    ItemConfig levelFruitConfig = Resources.Load<ItemConfig>("Items/" + levelFruitTypeId);
                    if (levelFruitConfig != null)
                    {
                        float baseQuality = levelFruitConfig.MaxQuality;
                        float expectedQuality = baseQuality;
                        if (potState.PlantLevel >= 3)
                        {
                            expectedQuality = baseQuality * (1f + qualityModifier / 100f);
                            expectedQuality = Mathf.Clamp(expectedQuality, baseQuality, baseQuality * 2f);
                        }
                        GUI.Label(new Rect(10f, relativeY, 400f, 25f), 
                            $"Qualità frutti attesa: {expectedQuality:F1} (base: {baseQuality:F1} + {qualityModifier:0}%)", labelStyle);
                        relativeY += 30f;
                    }
                    
                    // Check slot passivi
                    bool canMoveToPassive = PlantLevelSystem.CanMoveToPassiveSlot(potState);
                    string passiveSlotStatus = canMoveToPassive 
                        ? "Slot Passivi: Disponibile" 
                        : "Slot Passivi: Non disponibile (richiede Lvl 5)";
                    GUI.Label(new Rect(10f, relativeY, 400f, 25f), passiveSlotStatus, labelStyle);
                    relativeY += 30f;
                    
                    // Pulsante Force Level Up
                    if (GUI.Button(new Rect(10f, relativeY, 150f, 25f), "Force Level Up", buttonStyle))
                    {
                        ForceLevelUp();
                    }
                    relativeY += 35f;
                }
                
                // === SEZIONE GROWTH POINTS ===
                GUI.Label(new Rect(10f, relativeY, consoleWidth - 40f, 25f), 
                    "=== Growth Points System ===", labelStyle);
                relativeY += 30f;
                
                GUI.Label(new Rect(10f, relativeY, 200f, 25f), 
                    $"Water Points: {potState.GrowthPointsWater}", labelStyle);
                GUI.Label(new Rect(220f, relativeY, 200f, 25f), 
                    $"Light Points: {potState.GrowthPointsLight}", labelStyle);
                GUI.Label(new Rect(430f, relativeY, 200f, 25f), 
                    $"Fertilizer Points: {potState.GrowthPointsFertilizer}", labelStyle);
                relativeY += 30f;
                
                GUI.Label(new Rect(10f, relativeY, 250f, 25f), 
                    $"Days Consecutive Optimal: {potState.DaysConsecutiveOptimal}", labelStyle);
                if (GUI.Button(new Rect(270f, relativeY, 120f, 25f), "Reset Points", buttonStyle))
                {
                    ResetGrowthPoints();
                }
                relativeY += 35f;
                
                // === SEZIONE pH AFFINITY ===
                GUI.Label(new Rect(10f, relativeY, consoleWidth - 40f, 25f), 
                    "=== pH Affinity System ===", labelStyle);
                relativeY += 30f;
                
                if (potState.HasPlant && !string.IsNullOrEmpty(potState.PlantCode) && _phSystem != null)
                {
                    var phPlantData = potState.GetPlantData();
                    if (phPlantData != null)
                    {
                        float currentPh = _phSystem.CurrentPh;
                        bool inRange = phPlantData.IsPhInOptimalRange(currentPh);
                        float phDistance = phPlantData.GetPhDistanceFromOptimal(currentPh);
                        PhSystem.PhBand phBand = _phSystem.EvaluateState();
                        
                        // Range Ottimale
                        GUI.Label(new Rect(10f, relativeY, 300f, 25f), 
                            $"Range Ottimale: {phPlantData.OptimalPhMin:F1} - {phPlantData.OptimalPhMax:F1}", labelStyle);
                        relativeY += 25f;
                        
                        // pH Corrente Dome
                        GUI.Label(new Rect(10f, relativeY, 300f, 25f), 
                            $"pH Corrente Dome: {currentPh:F1} ({phBand})", labelStyle);
                        relativeY += 25f;
                        
                        // In Range
                        Color inRangeColor = inRange ? Color.green : Color.red;
                        GUI.color = inRangeColor;
                        GUI.Label(new Rect(10f, relativeY, 200f, 25f), 
                            $"In Range: {(inRange ? "Sì" : "No")}", labelStyle);
                        GUI.color = Color.white;
                        relativeY += 25f;
                        
                        // Distanza dal Range
                        GUI.Label(new Rect(10f, relativeY, 300f, 25f), 
                            $"Distanza dal Range: {phDistance:F2} (0=in range, 1=molto lontano)", labelStyle);
                        relativeY += 25f;
                        
                        // Effetto Crescita
                        string growthEffect = inRange ? "-1 giorno" : "Nessuno";
                        GUI.Label(new Rect(10f, relativeY, 300f, 25f), 
                            $"Effetto Crescita: {growthEffect}", labelStyle);
                        relativeY += 25f;
                        
                        // Countdown Morte
                        if (potState.ExtremePhDeathCountdown >= 0)
                        {
                            GUI.color = Color.red;
                            GUI.Label(new Rect(10f, relativeY, 400f, 25f), 
                                $"⚠️ Countdown Morte: {potState.ExtremePhDeathCountdown} giorni rimanenti (Giorni in pH estremo: {potState.DaysInExtremePh})", labelStyle);
                            GUI.color = Color.white;
                            relativeY += 25f;
                            
                            GUI.Label(new Rect(10f, relativeY, 400f, 25f), 
                                $"Stato Morte Imminente: Tra {potState.ExtremePhDeathCountdown} giorni morirà", labelStyle);
                            relativeY += 30f;
                        }
                        else
                        {
                            GUI.Label(new Rect(10f, relativeY, 300f, 25f), 
                                $"Countdown Morte: Non attivo", labelStyle);
                            relativeY += 30f;
                        }
                    }
                    else
                    {
                        GUI.Label(new Rect(10f, relativeY, 300f, 25f), 
                            "PlantData non trovato", labelStyle);
                        relativeY += 30f;
                    }
                }
                else
                {
                    if (!potState.HasPlant)
                    {
                        GUI.Label(new Rect(10f, relativeY, 300f, 25f), 
                            "Nessuna pianta nel vaso", labelStyle);
                    }
                    else if (_phSystem == null)
                    {
                        GUI.Label(new Rect(10f, relativeY, 300f, 25f), 
                            "PhSystem non disponibile", labelStyle);
                    }
                    relativeY += 30f;
                }
                
                // === SEZIONE LED SYSTEM ===
                GUI.Label(new Rect(10f, relativeY, consoleWidth - 40f, 25f), 
                    "=== LED System ===", labelStyle);
                relativeY += 30f;
                
                string ledStateName = GetLedStateName(potState.LedSystemState);
                // Riutilizza maxDaysForFullStress già definito sopra (linea 957)
                int burnRiskLevel = potState.GetBurnRiskLevel(maxDaysForFullStress);
                string burnRiskName = GetBurnRiskName(burnRiskLevel);
                
                GUI.Label(new Rect(10f, relativeY, 200f, 25f), 
                    $"LED State: {ledStateName}", labelStyle);
                GUI.Label(new Rect(220f, relativeY, 200f, 25f), 
                    $"Days Consecutive: {potState.GetConsecutiveLedDays()}", labelStyle);
                GUI.Label(new Rect(430f, relativeY, 200f, 25f), 
                    $"Burn Risk: {burnRiskLevel} ({burnRiskName})", labelStyle);
                relativeY += 30f;
                
                // LED CONSIGLIATO basato su famiglia pianta
                PlantData plantData = potState.GetPlantData();
                if (plantData != null)
                {
                    LedCompatibility compatible = LedCompatibilityHelper.GetCompatibleLedTypes(plantData.Family);
                    string compatibleDisplay = LedCompatibilityHelper.GetCompatibleLedDisplay(compatible);
                    GUI.Label(new Rect(10f, relativeY, 300f, 25f), 
                        $"LED CONSIGLIATO: {compatibleDisplay} (Famiglia: {plantData.Family})", labelStyle);
                    relativeY += 25f;
                }
                
                // Controlli LED Blue e Red separati
                bool isBlueOn = potState.LedSystemState == LedSystemState.Blue;
                bool isRedOn = potState.LedSystemState == LedSystemState.Red;
                
                GUI.Label(new Rect(10f, relativeY, 120f, 25f), "LED Blue:", labelStyle);
                if (GUI.Button(new Rect(140f, relativeY, 80f, 25f), isBlueOn ? "ON" : "OFF", buttonStyle))
                {
                    SetLedBlue(!isBlueOn);
                }
                
                GUI.Label(new Rect(230f, relativeY, 120f, 25f), "LED Red:", labelStyle);
                if (GUI.Button(new Rect(360f, relativeY, 80f, 25f), isRedOn ? "ON" : "OFF", buttonStyle))
                {
                    SetLedRed(!isRedOn);
                }
                relativeY += 30f;
                
                GUI.Label(new Rect(10f, relativeY, 150f, 25f), "Set LED Days:", labelStyle);
                _ledDaysInputString = GUI.TextField(new Rect(170f, relativeY, 80f, 25f), _ledDaysInputString);
                if (int.TryParse(_ledDaysInputString, out int parsedLedDays))
                {
                    _ledDaysInputValue = Mathf.Max(0, parsedLedDays);
                    _ledDaysInputString = _ledDaysInputValue.ToString();
                }
                if (GUI.Button(new Rect(260f, relativeY, 25f, 25f), "-", buttonStyle))
                {
                    _ledDaysInputValue = Mathf.Max(0, _ledDaysInputValue - 1);
                    _ledDaysInputString = _ledDaysInputValue.ToString();
                }
                if (GUI.Button(new Rect(290f, relativeY, 25f, 25f), "+", buttonStyle))
                {
                    _ledDaysInputValue = _ledDaysInputValue + 1;
                    _ledDaysInputString = _ledDaysInputValue.ToString();
                }
                if (GUI.Button(new Rect(325f, relativeY, 120f, 25f), "Imposta Days", buttonStyle))
                {
                    if (int.TryParse(_ledDaysInputString, out int finalLedDays))
                    {
                        _ledDaysInputValue = Mathf.Max(0, finalLedDays);
                        _ledDaysInputString = _ledDaysInputValue.ToString();
                    }
                    SetLedDays(_ledDaysInputValue);
                }
                relativeY += 35f;
                
                // === SEZIONE FRUITS ===
                GUI.Label(new Rect(10f, relativeY, consoleWidth - 40f, 25f), 
                    "=== FRUITS Production ===", labelStyle);
                relativeY += 30f;
                
                // Frutti presenti
                GUI.Label(new Rect(10f, relativeY, 150f, 25f), "Frutti Presenti (0-3):", labelStyle);
                _amountFruitsInputString = GUI.TextField(new Rect(170f, relativeY, 80f, 25f), _amountFruitsInputString);
                if (float.TryParse(_amountFruitsInputString, out float parsedAmountFruits))
                {
                    _amountFruitsInputValue = Mathf.Clamp(parsedAmountFruits, 0f, _maxFruitsInputValue);
                    _amountFruitsInputString = _amountFruitsInputValue.ToString("F1");
                }
                if (GUI.Button(new Rect(260f, relativeY, 25f, 25f), "-", buttonStyle))
                {
                    _amountFruitsInputValue = Mathf.Max(0f, _amountFruitsInputValue - 0.5f);
                    _amountFruitsInputString = _amountFruitsInputValue.ToString("F1");
                }
                if (GUI.Button(new Rect(290f, relativeY, 25f, 25f), "+", buttonStyle))
                {
                    _amountFruitsInputValue = Mathf.Min(_maxFruitsInputValue, _amountFruitsInputValue + 0.5f);
                    _amountFruitsInputString = _amountFruitsInputValue.ToString("F1");
                }
                if (GUI.Button(new Rect(325f, relativeY, 120f, 25f), "Imposta Frutti", buttonStyle))
                {
                    if (float.TryParse(_amountFruitsInputString, out float finalAmountFruits))
                    {
                        _amountFruitsInputValue = Mathf.Clamp(finalAmountFruits, 0f, _maxFruitsInputValue);
                        _amountFruitsInputString = _amountFruitsInputValue.ToString("F1");
                    }
                    SetAmountFruits(_amountFruitsInputValue);
                }
                GUI.Label(new Rect(455f, relativeY, 200f, 25f), 
                    $"Corrente: {potState.AmountFruits:F1}", labelStyle);
                relativeY += 35f;
                
                // Frutti massimi
                GUI.Label(new Rect(10f, relativeY, 150f, 25f), "Frutti Massimi:", labelStyle);
                _maxFruitsInputString = GUI.TextField(new Rect(170f, relativeY, 80f, 25f), _maxFruitsInputString);
                if (int.TryParse(_maxFruitsInputString, out int parsedMaxFruits))
                {
                    _maxFruitsInputValue = Mathf.Clamp(parsedMaxFruits, 1, 10);
                    _maxFruitsInputString = _maxFruitsInputValue.ToString();
                }
                if (GUI.Button(new Rect(260f, relativeY, 25f, 25f), "-", buttonStyle))
                {
                    _maxFruitsInputValue = Mathf.Max(1, _maxFruitsInputValue - 1);
                    _maxFruitsInputString = _maxFruitsInputValue.ToString();
                }
                if (GUI.Button(new Rect(290f, relativeY, 25f, 25f), "+", buttonStyle))
                {
                    _maxFruitsInputValue = Mathf.Min(10, _maxFruitsInputValue + 1);
                    _maxFruitsInputString = _maxFruitsInputValue.ToString();
                }
                GUI.Label(new Rect(325f, relativeY, 200f, 25f), 
                    $"Max: {_maxFruitsInputValue} (default: 3)", labelStyle);
                relativeY += 35f;
                
                // Qualità frutti
                GUI.Label(new Rect(10f, relativeY, 150f, 25f), "Qualità Frutti:", labelStyle);
                _fruitQualityInputString = GUI.TextField(new Rect(170f, relativeY, 80f, 25f), _fruitQualityInputString);
                if (float.TryParse(_fruitQualityInputString, out float parsedQuality))
                {
                    _fruitQualityInputValue = Mathf.Max(0f, parsedQuality);
                    _fruitQualityInputString = _fruitQualityInputValue.ToString("F1");
                }
                string debugFruitTypeId = ItemFabric.ResolveFruitTypeIdForPlant(potState.PlantCode, potState.PlantFamilyMetadata);
                ItemConfig fruitsFruitConfig = Resources.Load<ItemConfig>("Items/" + debugFruitTypeId);
                if (fruitsFruitConfig != null)
                {
                    float baseQuality = fruitsFruitConfig.MaxQuality;
                    float expectedQuality = baseQuality;
                    if (_plantLevelConfig != null && potState.PlantLevel >= 3)
                    {
                        float qualityModifier = _plantLevelConfig.GetQualityModifier(potState.PlantLevel);
                        expectedQuality = baseQuality * (1f + qualityModifier / 100f);
                        expectedQuality = Mathf.Clamp(expectedQuality, baseQuality, baseQuality * 2f);
                    }
                    GUI.Label(new Rect(260f, relativeY, 300f, 25f), 
                        $"Attesa: {expectedQuality:F1} (base: {baseQuality:F1}, Lvl {potState.PlantLevel})", labelStyle);
                }
                relativeY += 40f;
                
                // === SEZIONE WATERING SYSTEM ===
                GUI.Label(new Rect(10f, relativeY, consoleWidth - 40f, 25f), 
                    "=== Watering System ===", labelStyle);
                relativeY += 30f;
                
                GUI.Label(new Rect(10f, relativeY, 200f, 25f), 
                    $"Watering System: {(potState.WateringSystemOn ? "ON" : "OFF")}", labelStyle);
                GUI.Label(new Rect(220f, relativeY, 200f, 25f), 
                    $"Days ON: {potState.DaysWateringSystemOn}", labelStyle);
                GUI.Label(new Rect(430f, relativeY, 200f, 25f), 
                    $"WAT-RAW Accumulator: {potState.WateringRawWaterAccumulator:F2}", labelStyle);
                relativeY += 30f;
                
                if (GUI.Button(new Rect(10f, relativeY, 150f, 25f), 
                    potState.WateringSystemOn ? "Turn OFF" : "Turn ON", buttonStyle))
                {
                    ToggleWateringSystem();
                }
                relativeY += 35f;
                
                // Pulsanti rapidi per stadi
                GUI.Label(new Rect(10f, relativeY, consoleWidth - 40f, 25f), "Hotkeys: 0=Empty, 1=Seed, 2=Sprout, 3=Growth, 4=Flowering, 5=HarvestReady, 6=Resting", labelStyle);
                relativeY += 30f; // Aumentato spazio
                
                float buttonWidth = (consoleWidth - 30f) / 7f;
                float buttonX = 10f;
                float buttonHeight = 30f; // Aumentato da 25f a 30f
                
                if (GUI.Button(new Rect(buttonX, relativeY, buttonWidth - 2f, buttonHeight), "Empty", buttonStyle))
                    SetPotStage((int)PlantStage.Empty);
                buttonX += buttonWidth;
                
                if (GUI.Button(new Rect(buttonX, relativeY, buttonWidth - 2f, buttonHeight), "Seed", buttonStyle))
                    SetPotStage((int)PlantStage.Seed);
                buttonX += buttonWidth;
                
                if (GUI.Button(new Rect(buttonX, relativeY, buttonWidth - 2f, buttonHeight), "Sprout", buttonStyle))
                    SetPotStage((int)PlantStage.Sprout);
                buttonX += buttonWidth;
                
                if (GUI.Button(new Rect(buttonX, relativeY, buttonWidth - 2f, buttonHeight), "Growth", buttonStyle))
                    SetPotStage((int)PlantStage.Growth);
                buttonX += buttonWidth;
                
                if (GUI.Button(new Rect(buttonX, relativeY, buttonWidth - 2f, buttonHeight), "Flowering", buttonStyle))
                    SetPotStage((int)PlantStage.Flowering);
                buttonX += buttonWidth;
                
                if (GUI.Button(new Rect(buttonX, relativeY, buttonWidth - 2f, buttonHeight), "Harvest", buttonStyle))
                    SetPotStage((int)PlantStage.HarvestReady);
                buttonX += buttonWidth;
                
                if (GUI.Button(new Rect(buttonX, relativeY, buttonWidth - 2f, buttonHeight), "Resting", buttonStyle))
                    SetPotStage((int)PlantStage.Resting);
                
                relativeY += 40f; // Aumentato spazio
                
                // Fine scroll view per sezione editing
                GUI.EndScrollView();
                
                // Aggiorna currentY per il log (usa la posizione calcolata per la sezione log)
                currentY = logSectionStartY;
            }
            else
            {
                GUI.Label(new Rect(consoleX + 10f, currentY, consoleWidth - 20f, 25f), 
                    "Seleziona un POT dalla lista sopra", labelStyle);
                currentY += 30f; // Aumentato spazio
            }
            
            // Log debug (assicurati che sia dentro la console)
            if (currentY < consoleY + consoleHeight - 10f)
            {
                // Sezione log
                GUI.Label(new Rect(consoleX + 10f, currentY, consoleWidth - 20f, 25f), 
                    "=== LOG ===", labelStyle);
                currentY += 30f;
                
                // Scroll view per log
                float logScrollHeight = consoleY + consoleHeight - currentY - 10f; // Altezza rimanente
                if (logScrollHeight > 0)
                {
                    _scrollPosition = GUI.BeginScrollView(
                        new Rect(consoleX + 10f, currentY, consoleWidth - 20f, logScrollHeight),
                        _scrollPosition,
                        new Rect(0, 0, consoleWidth - 40f, _debugLog.Count * 20f + 10f));
                    
                    float logY = 0f;
                    for (int i = 0; i < _debugLog.Count; i++)
                    {
                        GUI.Label(new Rect(0, logY, consoleWidth - 40f, 20f), _debugLog[i], labelStyle);
                        logY += 20f;
                    }
                    
                    GUI.EndScrollView();
                }
            }
            
            // Pulsante chiudi (più grande)
            if (GUI.Button(new Rect(consoleX + consoleWidth - 100f, consoleY + 10f, 90f, 30f), "Chiudi (P)", buttonStyle)) // Aumentato da 70f/25f a 90f/30f
            {
                _isConsoleOpen = false;
            }
        }
        
        private string GetConditionName(PlantCondition condition)
        {
            return condition switch
            {
                PlantCondition.Rigogliosa => "Rigogliosa",
                PlantCondition.Sana => "Sana",
                PlantCondition.Stressata => "Stressata",
                PlantCondition.Appassita => "Appassita",
                PlantCondition.Critica => "Critica",
                _ => "Sconosciuta"
            };
        }
        
        private string GetForecastSymbol(ForecastDirection forecast)
        {
            return forecast switch
            {
                ForecastDirection.Up => "↑",
                ForecastDirection.Stable => "→",
                ForecastDirection.Down => "↓",
                _ => "?"
            };
        }
        
        private string GetMoldRiskName(int level)
        {
            return level switch
            {
                0 => "None",
                1 => "Mild",
                2 => "Severe",
                3 => "Critical",
                _ => "Unknown"
            };
        }
        
        private string GetLedStateName(LedSystemState state)
        {
            return state switch
            {
                LedSystemState.Off => "Off",
                LedSystemState.Blue => "Blue",
                LedSystemState.Red => "Red",
                _ => "Unknown"
            };
        }
        
        private string GetBurnRiskName(int level)
        {
            return level switch
            {
                0 => "None",
                1 => "Medium",
                2 => "High",
                3 => "Critical",
                _ => "Unknown"
            };
        }
        
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}

