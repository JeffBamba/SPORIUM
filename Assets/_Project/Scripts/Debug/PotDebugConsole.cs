using System.Collections.Generic;
using UnityEngine;
using Sporae.Dome.PotSystem.Growth;

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
        private List<string> _debugLog = new List<string>();
        private const int MAX_LOG_ENTRIES = 50;
        
        // POT selezionato per editing
        private PotActions _selectedPot = null;
        private string _selectedPotId = "";
        
        // Lista di tutti i POT nella scena
        private PotActions[] _allPots = new PotActions[0];
        
        // Valore stadio da impostare
        private int _stageInputValue = 0;
        
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
            
            Debug.Log($"[Pot Debug Console] Awake - enableDebugConsole: {enableDebugConsole}, toggleKey: {toggleKey}, showOnStart: {showOnStart}");
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
        
        private void Start()
        {
            RefreshPotList();
            AddLog("=== Pot Debug Console ===");
            AddLog("Premi P per aprire/chiudere la console");
            AddLog($"Trovati {_allPots.Length} POT nella scena");
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
                Debug.Log($"[Pot Debug Console] Console {(_isConsoleOpen ? "aperta" : "chiusa")} - Tasto {toggleKey} premuto");
                
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
            Debug.Log($"[Pot Debug Console] {potState.PotId}: Stadio cambiato {oldStage} → {newStage}");
        }
        
        private string GetStageName(int stage)
        {
            return ((PlantStage)stage).ToString();
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
                    AddLog($"POT selezionato: {potState.PotId}");
                }
            }
            
            GUI.EndScrollView();
            currentY += listHeight + 15f; // Aumentato spazio
            
            // Sezione editing POT selezionato
            if (_selectedPot != null && _selectedPot.PotState != null)
            {
                var potState = _selectedPot.PotState;
                
                GUI.Label(new Rect(consoleX + 10f, currentY, consoleWidth - 20f, 25f), 
                    $"POT Selezionato: {potState.PotId}", labelStyle);
                currentY += 30f; // Aumentato spazio
                
                // Info POT corrente
                string currentInfo = $"Stadio Attuale: {potState.Stage} ({GetStageName(potState.Stage)})";
                if (potState.HasPlant && !string.IsNullOrEmpty(potState.PlantCode))
                {
                    currentInfo += $"\nPlantCode: {potState.PlantCode}";
                }
                if (potState.Stage == (int)PlantStage.HarvestReady)
                {
                    currentInfo += $"\nFrutti: {potState.AmountFruits:F1}";
                }
                
                GUI.Label(new Rect(consoleX + 10f, currentY, consoleWidth - 20f, 70f), currentInfo, labelStyle);
                currentY += 75f; // Aumentato spazio
                
                // Input stadio manuale
                GUI.Label(new Rect(consoleX + 10f, currentY, 180f, 25f), "Nuovo Stadio (0-6):", labelStyle);
                string stageInput = GUI.TextField(new Rect(consoleX + 200f, currentY, 100f, 25f), _stageInputValue.ToString()); // Aumentato da 80f/20f a 100f/25f
                if (int.TryParse(stageInput, out int parsedStage))
                {
                    _stageInputValue = Mathf.Clamp(parsedStage, 0, 6);
                }
                
                if (GUI.Button(new Rect(consoleX + 310f, currentY, 120f, 25f), "Imposta Stadio", buttonStyle)) // Aumentato da 100f/20f a 120f/25f
                {
                    SetPotStage(_stageInputValue);
                }
                currentY += 35f; // Aumentato spazio
                
                // Pulsanti rapidi per stadi
                GUI.Label(new Rect(consoleX + 10f, currentY, consoleWidth - 20f, 25f), "Hotkeys: 0=Empty, 1=Seed, 2=Sprout, 3=Growth, 4=Flowering, 5=HarvestReady, 6=Resting", labelStyle);
                currentY += 30f; // Aumentato spazio
                
                float buttonWidth = (consoleWidth - 30f) / 7f;
                float buttonX = consoleX + 10f;
                float buttonHeight = 30f; // Aumentato da 25f a 30f
                
                if (GUI.Button(new Rect(buttonX, currentY, buttonWidth - 2f, buttonHeight), "Empty", buttonStyle))
                    SetPotStage((int)PlantStage.Empty);
                buttonX += buttonWidth;
                
                if (GUI.Button(new Rect(buttonX, currentY, buttonWidth - 2f, buttonHeight), "Seed", buttonStyle))
                    SetPotStage((int)PlantStage.Seed);
                buttonX += buttonWidth;
                
                if (GUI.Button(new Rect(buttonX, currentY, buttonWidth - 2f, buttonHeight), "Sprout", buttonStyle))
                    SetPotStage((int)PlantStage.Sprout);
                buttonX += buttonWidth;
                
                if (GUI.Button(new Rect(buttonX, currentY, buttonWidth - 2f, buttonHeight), "Growth", buttonStyle))
                    SetPotStage((int)PlantStage.Growth);
                buttonX += buttonWidth;
                
                if (GUI.Button(new Rect(buttonX, currentY, buttonWidth - 2f, buttonHeight), "Flowering", buttonStyle))
                    SetPotStage((int)PlantStage.Flowering);
                buttonX += buttonWidth;
                
                if (GUI.Button(new Rect(buttonX, currentY, buttonWidth - 2f, buttonHeight), "Harvest", buttonStyle))
                    SetPotStage((int)PlantStage.HarvestReady);
                buttonX += buttonWidth;
                
                if (GUI.Button(new Rect(buttonX, currentY, buttonWidth - 2f, buttonHeight), "Resting", buttonStyle))
                    SetPotStage((int)PlantStage.Resting);
                
                currentY += 40f; // Aumentato spazio
            }
            else
            {
                GUI.Label(new Rect(consoleX + 10f, currentY, consoleWidth - 20f, 25f), 
                    "Seleziona un POT dalla lista sopra", labelStyle);
                currentY += 30f; // Aumentato spazio
            }
            
            // Log debug
            currentY += 15f; // Aumentato spazio
            GUI.Label(new Rect(consoleX + 10f, currentY, consoleWidth - 20f, 25f), "Log:", labelStyle);
            currentY += 30f; // Aumentato spazio
            
            float logHeight = consoleHeight - (currentY - consoleY) - 10f;
            _scrollPosition = GUI.BeginScrollView(
                new Rect(consoleX + 10f, currentY, consoleWidth - 20f, logHeight),
                _scrollPosition,
                new Rect(0, 0, consoleWidth - 40f, _debugLog.Count * 20f + 10f)); // Aumentato da 15f a 20f
            
            for (int i = 0; i < _debugLog.Count; i++)
            {
                GUI.Label(new Rect(0, i * 20f, consoleWidth - 40f, 20f), _debugLog[i], labelStyle); // Aumentato da 15f a 20f
            }
            
            GUI.EndScrollView();
            
            // Pulsante chiudi (più grande)
            if (GUI.Button(new Rect(consoleX + consoleWidth - 100f, consoleY + 10f, 90f, 30f), "Chiudi (P)", buttonStyle)) // Aumentato da 70f/25f a 90f/30f
            {
                _isConsoleOpen = false;
            }
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

