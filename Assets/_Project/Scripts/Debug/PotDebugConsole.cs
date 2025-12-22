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
        
        // BLK-03.01-T2: Valori debug per Fertilizzante, Watering e Luce %
        private int _fertilizerInputValue = 0;
        private int _hydrationInputValue = 0;
        private int _lightPercentInputValue = 0;
        
        // Valori debug per nuovi sistemi
        private int _plantLevelInputValue = 1;
        private int _completedCyclesInputValue = 0;
        private int _moldRiskLevelInputValue = 0;
        private int _conditionScoreInputValue = 50;
        private int _ledStateInputValue = 0; // 0=Off, 1=Blue, 2=Red
        private int _ledDaysInputValue = 0;
        private bool _wateringSystemToggle = false;
        
        // Cache per sistemi
        private PhSystem _phSystem;
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
                }
            }
            catch
            {
                // PhSystem non disponibile
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
        
        // BLK-03.01-T2: Imposta luce percentuale
        private void SetLightPercent(int newPercent)
        {
            if (_selectedPot == null || _selectedPot.PotState == null)
            {
                AddLog("⚠️ Nessun POT selezionato!");
                return;
            }
            
            var potState = _selectedPot.PotState;
            int maxLightExposure = _selectedPot.GetMaxLightExposure();
            int oldLightExposure = potState.LightExposure;
            
            // Converti percentuale in valore LightExposure
            int newLightExposure = Mathf.RoundToInt((newPercent / 100f) * maxLightExposure);
            potState.LightExposure = Mathf.Clamp(newLightExposure, 0, maxLightExposure);
            
            // BUG FIX: Marca come impostato manualmente e salva valore base per decay
            potState.IsLightExposureManuallySet = true;
            potState.ManualLightExposureBase = potState.LightExposure;
            
            float oldPercent = maxLightExposure > 0 ? (float)oldLightExposure / maxLightExposure * 100f : 0f;
            float newPercentActual = maxLightExposure > 0 ? (float)potState.LightExposure / maxLightExposure * 100f : 0f;
            
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
            
            AddLog($"✅ {potState.PotId}: Luce cambiata {oldLightExposure}/{maxLightExposure} ({oldPercent:F0}%) → {potState.LightExposure}/{maxLightExposure} ({newPercentActual:F0}%) (MANUALE - decay partirà da questo valore)");
            SporiumLogger.LogInfo(LogCategory.Pot, $"{potState.PotId}: Luce cambiata {oldLightExposure}/{maxLightExposure} ({oldPercent:F0}%) → {potState.LightExposure}/{maxLightExposure} ({newPercentActual:F0}%) (MANUALE)");
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
                    _selectedPotId = potState.PotId;
                    _stageInputValue = potState.Stage;
                    // BLK-03.01-T2: Inizializza valori debug quando si seleziona un POT
                    _fertilizerInputValue = potState.FertilizerLevel;
                    _hydrationInputValue = potState.Hydration;
                    int maxLight = pot.GetMaxLightExposure();
                    _lightPercentInputValue = maxLight > 0 ? 
                        Mathf.RoundToInt((float)potState.LightExposure / maxLight * 100f) : 0;
                    
                    // Inizializza valori nuovi sistemi
                    _plantLevelInputValue = potState.PlantLevel;
                    _completedCyclesInputValue = potState.CompletedCycles;
                    _moldRiskLevelInputValue = potState.MoldRiskLevel;
                    _conditionScoreInputValue = potState.ConditionScore;
                    _ledStateInputValue = (int)potState.LedSystemState;
                    _ledDaysInputValue = potState.GetConsecutiveLedDays();
                    _wateringSystemToggle = potState.WateringSystemOn;
                    
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
                // pH Affinity: 30+~170=200, Growth Points: 30+30+35=95, LED: 30+30+30+35=125, Watering: 30+30+35=95,
                // Pulsanti stadi: 30+30+40=100
                // Totale: ~1250px
                float editingContentHeight = 1250f;
                
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
                
                // Input stadio manuale
                GUI.Label(new Rect(10f, relativeY, 180f, 25f), "Nuovo Stadio (0-6):", labelStyle);
                string stageInput = GUI.TextField(new Rect(200f, relativeY, 100f, 25f), _stageInputValue.ToString());
                if (int.TryParse(stageInput, out int parsedStage))
                {
                    _stageInputValue = Mathf.Clamp(parsedStage, 0, 6);
                }
                
                if (GUI.Button(new Rect(310f, relativeY, 120f, 25f), "Imposta Stadio", buttonStyle))
                {
                    SetPotStage(_stageInputValue);
                }
                relativeY += 35f; // Aumentato spazio
                
                // BLK-03.01-T2: Sezione Debug Fertilizzante, Watering e Luce %
                GUI.Label(new Rect(10f, relativeY, consoleWidth - 40f, 25f), 
                    "=== Debug Parametri ===", labelStyle);
                relativeY += 30f;
                
                // Fertilizzante (0-100%)
                GUI.Label(new Rect(10f, relativeY, 150f, 25f), "Fertilizzante (0-100%):", labelStyle);
                string fertilizerInput = GUI.TextField(new Rect(170f, relativeY, 100f, 25f), _fertilizerInputValue.ToString());
                if (int.TryParse(fertilizerInput, out int parsedFertilizer))
                {
                    _fertilizerInputValue = Mathf.Clamp(parsedFertilizer, 0, 100);
                }
                if (GUI.Button(new Rect(280f, relativeY, 120f, 25f), "Imposta Fert.", buttonStyle))
                {
                    SetFertilizerLevel(_fertilizerInputValue);
                }
                // Mostra valore corrente
                GUI.Label(new Rect(410f, relativeY, 200f, 25f), 
                    $"Corrente: {potState.FertilizerLevel}%", labelStyle);
                relativeY += 35f;
                
                // Watering (0-MaxHydration)
                GUI.Label(new Rect(10f, relativeY, 150f, 25f), $"Watering (0-{maxHydration}):", labelStyle);
                string hydrationInput = GUI.TextField(new Rect(170f, relativeY, 100f, 25f), _hydrationInputValue.ToString());
                if (int.TryParse(hydrationInput, out int parsedHydration))
                {
                    _hydrationInputValue = Mathf.Clamp(parsedHydration, 0, maxHydration);
                }
                if (GUI.Button(new Rect(280f, relativeY, 120f, 25f), "Imposta Water", buttonStyle))
                {
                    SetHydration(_hydrationInputValue);
                }
                // Mostra valore corrente
                GUI.Label(new Rect(410f, relativeY, 200f, 25f), 
                    $"Corrente: {potState.Hydration}/{maxHydration}", labelStyle);
                relativeY += 35f;
                
                // Luce % (0-100%)
                GUI.Label(new Rect(10f, relativeY, 150f, 25f), "Luce % (0-100%):", labelStyle);
                string lightPercentInput = GUI.TextField(new Rect(170f, relativeY, 100f, 25f), _lightPercentInputValue.ToString());
                if (int.TryParse(lightPercentInput, out int parsedLightPercent))
                {
                    _lightPercentInputValue = Mathf.Clamp(parsedLightPercent, 0, 100);
                }
                if (GUI.Button(new Rect(280f, relativeY, 120f, 25f), "Imposta Luce %", buttonStyle))
                {
                    SetLightPercent(_lightPercentInputValue);
                }
                // Mostra valore corrente
                float currentLightPercent = maxLightExposure > 0 ? 
                    (float)potState.LightExposure / maxLightExposure * 100f : 0f;
                GUI.Label(new Rect(410f, relativeY, 200f, 25f), 
                    $"Corrente: {potState.LightExposure}/{maxLightExposure} ({currentLightPercent:F0}%)", labelStyle);
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
                string conditionInput = GUI.TextField(new Rect(170f, relativeY, 100f, 25f), _conditionScoreInputValue.ToString());
                if (int.TryParse(conditionInput, out int parsedCondition))
                {
                    _conditionScoreInputValue = Mathf.Clamp(parsedCondition, 0, 100);
                }
                if (GUI.Button(new Rect(280f, relativeY, 120f, 25f), "Imposta Score", buttonStyle))
                {
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
                    $"Days w/o Pruning: {potState.DaysWithoutPruning}", labelStyle);
                GUI.Label(new Rect(430f, relativeY, 200f, 25f), 
                    $"Days Overwatering: {potState.DaysOverwateringConsecutive}", labelStyle);
                relativeY += 30f;
                
                // Calcola rischio in tempo reale se possibile
                if (_phSystem != null && _moldConfig != null && potState.HasPlant)
                {
                    var plantData = potState.GetPlantData();
                    float moldRisk = MoldSystem.CalculateMoldRisk(potState, _phSystem, plantData, _moldConfig);
                    int calculatedLevel = MoldSystem.GetMoldRiskLevel(potState, _phSystem, plantData, _moldConfig);
                    GUI.Label(new Rect(10f, relativeY, 300f, 25f), 
                        $"Rischio calcolato: {moldRisk:F1} (Level: {calculatedLevel})", labelStyle);
                    relativeY += 30f;
                }
                
                GUI.Label(new Rect(10f, relativeY, 150f, 25f), "Set Mold Risk (0-3):", labelStyle);
                string moldInput = GUI.TextField(new Rect(170f, relativeY, 100f, 25f), _moldRiskLevelInputValue.ToString());
                if (int.TryParse(moldInput, out int parsedMold))
                {
                    _moldRiskLevelInputValue = Mathf.Clamp(parsedMold, 0, 3);
                }
                if (GUI.Button(new Rect(280f, relativeY, 120f, 25f), "Imposta Risk", buttonStyle))
                {
                    SetMoldRiskLevel(_moldRiskLevelInputValue);
                }
                if (GUI.Button(new Rect(410f, relativeY, 120f, 25f), "Rimuovi Infest.", buttonStyle))
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
                string levelInput = GUI.TextField(new Rect(170f, relativeY, 100f, 25f), _plantLevelInputValue.ToString());
                if (int.TryParse(levelInput, out int parsedLevel))
                {
                    _plantLevelInputValue = Mathf.Clamp(parsedLevel, 1, 5);
                }
                if (GUI.Button(new Rect(280f, relativeY, 120f, 25f), "Imposta Level", buttonStyle))
                {
                    SetPlantLevel(_plantLevelInputValue);
                }
                relativeY += 30f;
                
                GUI.Label(new Rect(10f, relativeY, 150f, 25f), "Set Cycles:", labelStyle);
                string cyclesInput = GUI.TextField(new Rect(170f, relativeY, 100f, 25f), _completedCyclesInputValue.ToString());
                if (int.TryParse(cyclesInput, out int parsedCycles))
                {
                    _completedCyclesInputValue = Mathf.Max(0, parsedCycles);
                }
                if (GUI.Button(new Rect(280f, relativeY, 120f, 25f), "Imposta Cycles", buttonStyle))
                {
                    SetCompletedCycles(_completedCyclesInputValue);
                }
                relativeY += 35f;
                
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
                    var plantData = potState.GetPlantData();
                    if (plantData != null)
                    {
                        float currentPh = _phSystem.CurrentPh;
                        bool inRange = plantData.IsPhInOptimalRange(currentPh);
                        float phDistance = plantData.GetPhDistanceFromOptimal(currentPh);
                        PhSystem.PhBand phBand = _phSystem.EvaluateState();
                        
                        // Range Ottimale
                        GUI.Label(new Rect(10f, relativeY, 300f, 25f), 
                            $"Range Ottimale: {plantData.OptimalPhMin:F1} - {plantData.OptimalPhMax:F1}", labelStyle);
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
                int burnRiskLevel = potState.GetBurnRiskLevel();
                string burnRiskName = GetBurnRiskName(burnRiskLevel);
                
                GUI.Label(new Rect(10f, relativeY, 200f, 25f), 
                    $"LED State: {ledStateName}", labelStyle);
                GUI.Label(new Rect(220f, relativeY, 200f, 25f), 
                    $"Days Consecutive: {potState.GetConsecutiveLedDays()}", labelStyle);
                GUI.Label(new Rect(430f, relativeY, 200f, 25f), 
                    $"Burn Risk: {burnRiskLevel} ({burnRiskName})", labelStyle);
                relativeY += 30f;
                
                GUI.Label(new Rect(10f, relativeY, 150f, 25f), "Set LED State:", labelStyle);
                string ledStateInput = GUI.TextField(new Rect(170f, relativeY, 100f, 25f), _ledStateInputValue.ToString());
                if (int.TryParse(ledStateInput, out int parsedLedState))
                {
                    _ledStateInputValue = Mathf.Clamp(parsedLedState, 0, 2);
                }
                if (GUI.Button(new Rect(280f, relativeY, 120f, 25f), "Imposta LED", buttonStyle))
                {
                    SetLedSystemState((LedSystemState)_ledStateInputValue);
                }
                relativeY += 30f;
                
                GUI.Label(new Rect(10f, relativeY, 150f, 25f), "Set LED Days:", labelStyle);
                string ledDaysInput = GUI.TextField(new Rect(170f, relativeY, 100f, 25f), _ledDaysInputValue.ToString());
                if (int.TryParse(ledDaysInput, out int parsedLedDays))
                {
                    _ledDaysInputValue = Mathf.Max(0, parsedLedDays);
                }
                if (GUI.Button(new Rect(280f, relativeY, 120f, 25f), "Imposta Days", buttonStyle))
                {
                    SetLedDays(_ledDaysInputValue);
                }
                relativeY += 35f;
                
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

