using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Sporae.DevTools;

namespace Sporae.DevTools
{
    /// <summary>
    /// Console di calibrazione difficoltà DOME.
    /// Permette di modificare tutti i parametri di gameplay in runtime durante il gioco.
    /// Tasto G per aprire/chiudere.
    /// </summary>
    public class DifficultyCalibrationConsole : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugConsole = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.G;
        [SerializeField] private bool showOnStart = false;
        
        private bool _isConsoleOpen = false;
        private Vector2 _scrollPosition;
        private Dictionary<string, bool> _sectionExpanded = new Dictionary<string, bool>();
        
        // Valori temporanei per slider (backup dei valori originali per export)
        private Dictionary<string, object> _originalValues = new Dictionary<string, object>();
        
        // Posizione console (per drag)
        private float _consoleX = 0f;
        private float _consoleY = 0f;
        private bool _isDragging = false;
        private Vector2 _dragOffset = Vector2.zero;
        
        // Stili UI
        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _sectionHeaderStyle;
        private bool _stylesInitialized = false;
        
        private void Awake()
        {
            _isConsoleOpen = showOnStart;
            
            #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            enableDebugConsole = false;
            #endif
            
            // Inizializza sezioni espanse
            _sectionExpanded["Overwatering"] = false;
            _sectionExpanded["Malus"] = false;
            _sectionExpanded["Bonus"] = false;
            _sectionExpanded["PhBands"] = false;
            _sectionExpanded["PhDrift"] = false;
            _sectionExpanded["LED"] = false;
            _sectionExpanded["Growth"] = false;
            _sectionExpanded["StageMultipliers"] = false;
            _sectionExpanded["Fertilizer"] = false;
            _sectionExpanded["Mold"] = false;
            _sectionExpanded["Condition"] = false;
            _sectionExpanded["HydrationRange"] = false;
            
            // Salva valori originali
            SaveOriginalValues();
        }
        
        private void Update()
        {
            if (!enableDebugConsole) return;
            
            if (Input.GetKeyDown(toggleKey))
            {
                _isConsoleOpen = !_isConsoleOpen;
                if (_isConsoleOpen)
                {
                    // Inizializza posizione al centro se non ancora impostata
                    if (_consoleX == 0f && _consoleY == 0f)
                    {
                        float consoleWidth = 900f;
                        float consoleHeight = Screen.height * 0.9f;
                        _consoleX = (Screen.width - consoleWidth) / 2f;
                        _consoleY = (Screen.height - consoleHeight) / 2f;
                    }
                }
                SporiumLogger.LogDebug(LogCategory.Dome, $"Console {(_isConsoleOpen ? "aperta" : "chiusa")} - Tasto {toggleKey} premuto");
            }
            
            // Gestione drag
            if (_isConsoleOpen)
            {
                HandleDrag();
            }
        }
        
        private void HandleDrag()
        {
            Vector2 mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y; // Converti Y per OnGUI
            
            if (Input.GetMouseButtonDown(0))
            {
                // Verifica se click è sul titolo (area drag)
                float consoleWidth = 900f;
                Rect titleRect = new Rect(_consoleX, _consoleY, consoleWidth, 40f);
                if (titleRect.Contains(mousePos))
                {
                    _isDragging = true;
                    _dragOffset = new Vector2(mousePos.x - _consoleX, mousePos.y - _consoleY);
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;
            }
            else if (_isDragging && Input.GetMouseButton(0))
            {
                // Aggiorna posizione console
                _consoleX = mousePos.x - _dragOffset.x;
                _consoleY = mousePos.y - _dragOffset.y;
                
                // Clamp alla schermata
                float consoleWidth = 900f;
                float consoleHeight = Screen.height * 0.9f;
                _consoleX = Mathf.Clamp(_consoleX, 0f, Screen.width - consoleWidth);
                _consoleY = Mathf.Clamp(_consoleY, 0f, Screen.height - consoleHeight);
            }
        }
        
        private void SaveOriginalValues()
        {
            var allParams = DifficultyCalibrationConfig.GetAllParameters();
            foreach (var kvp in allParams)
            {
                _originalValues[kvp.Key] = kvp.Value;
            }
        }
        
        private void OnGUI()
        {
            if (!enableDebugConsole || !_isConsoleOpen) return;
            
            if (!_stylesInitialized)
            {
                InitializeStyles();
            }
            
            // Area console principale
            float consoleWidth = 900f;
            float consoleHeight = Screen.height * 0.9f;
            
            // Inizializza posizione se non ancora impostata
            if (_consoleX == 0f && _consoleY == 0f)
            {
                _consoleX = (Screen.width - consoleWidth) / 2f;
                _consoleY = (Screen.height - consoleHeight) / 2f;
            }
            
            Rect consoleRect = new Rect(_consoleX, _consoleY, consoleWidth, consoleHeight);
            
            GUI.Box(consoleRect, "", _boxStyle);
            
            // Titolo (area drag)
            Rect titleRect = new Rect(_consoleX, _consoleY, consoleWidth, 40f);
            GUI.Label(titleRect, "🎮 DOME DIFFICULTY CALIBRATION CONSOLE", _labelStyle);
            
            // Scroll view per contenuto
            _scrollPosition = GUI.BeginScrollView(
                new Rect(_consoleX + 10f, _consoleY + 50f, consoleWidth - 20f, consoleHeight - 60f),
                _scrollPosition,
                new Rect(0, 0, consoleWidth - 40f, 5000f)); // Altezza contenuto stimata
            
            float currentY = 0f;
            
            // Spazio per titolo (già disegnato fuori dalla scroll view)
            currentY += 10f;
            
            // Pulsanti azioni rapide
            GUI.BeginGroup(new Rect(10f, currentY, consoleWidth - 40f, 40f));
            if (GUI.Button(new Rect(0f, 0f, 120f, 30f), "Reset All", _buttonStyle))
            {
                DifficultyCalibrationConfig.ResetToDefaults();
                SaveOriginalValues();
                SporiumLogger.LogInfo(LogCategory.Dome, "Tutti i parametri resettati ai default");
            }
            if (GUI.Button(new Rect(130f, 0f, 120f, 30f), "Export .txt", _buttonStyle))
            {
                ExportToTxt();
            }
            if (GUI.Button(new Rect(260f, 0f, 100f, 30f), "Chiudi (G)", _buttonStyle))
            {
                _isConsoleOpen = false;
            }
            GUI.EndGroup();
            currentY += 50f;
            
            // Sezioni parametri
            {
                currentY = DrawSection("OVERWATERING SYSTEM", "Overwatering", currentY, consoleWidth - 40f, (startY) =>
                {
                    return DrawOverwateringSection(consoleWidth - 40f, startY);
                });
            
                currentY = DrawSection("MALUS CRESCITA", "Malus", currentY, consoleWidth - 40f, (startY) =>
                {
                    return DrawMalusSection(consoleWidth - 40f, startY);
                });
                
                currentY = DrawSection("BONUS CRESCITA", "Bonus", currentY, consoleWidth - 40f, (startY) =>
                {
                    return DrawBonusSection(consoleWidth - 40f, startY);
                });
                
                currentY = DrawSection("SISTEMA pH - BANDE", "PhBands", currentY, consoleWidth - 40f, (startY) =>
                {
                    return DrawPhBandsSection(consoleWidth - 40f, startY);
                });
                
                currentY = DrawSection("pH DRIFT DA AZIONI", "PhDrift", currentY, consoleWidth - 40f, (startY) =>
                {
                    return DrawPhDriftSection(consoleWidth - 40f, startY);
                });
                
                currentY = DrawSection("SISTEMA LED", "LED", currentY, consoleWidth - 40f, (startY) =>
                {
                    return DrawLedSection(consoleWidth - 40f, startY);
                });
                
                currentY = DrawSection("SISTEMA CRESCITA", "Growth", currentY, consoleWidth - 40f, (startY) =>
                {
                    return DrawGrowthSection(consoleWidth - 40f, startY);
                });
                
                currentY = DrawSection("MOLTIPLICATORI STADIO", "StageMultipliers", currentY, consoleWidth - 40f, (startY) =>
                {
                    return DrawStageMultipliersSection(consoleWidth - 40f, startY);
                });
                
                currentY = DrawSection("SISTEMA FERTILIZZANTE", "Fertilizer", currentY, consoleWidth - 40f, (startY) =>
                {
                    return DrawFertilizerSection(consoleWidth - 40f, startY);
                });
                
                currentY = DrawSection("SISTEMA MUFFE", "Mold", currentY, consoleWidth - 40f, (startY) =>
                {
                    return DrawMoldSection(consoleWidth - 40f, startY);
                });
                
                currentY = DrawSection("SOGLIE CONDIZIONE", "Condition", currentY, consoleWidth - 40f, (startY) =>
                {
                    return DrawConditionSection(consoleWidth - 40f, startY);
                });
                
                currentY = DrawSection("RANGE IDRATAZIONE", "HydrationRange", currentY, consoleWidth - 40f, (startY) =>
                {
                    return DrawHydrationRangeSection(consoleWidth - 40f, startY);
                });
            }
            
            GUI.EndScrollView();
        }
        
        private void InitializeStyles()
        {
            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.95f));
            
            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 16;
            _labelStyle.normal.textColor = Color.white;
            
            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontSize = 14;
            _buttonStyle.normal.textColor = Color.white;
            
            _sectionHeaderStyle = new GUIStyle(GUI.skin.label);
            _sectionHeaderStyle.fontSize = 18;
            _sectionHeaderStyle.fontStyle = FontStyle.Bold;
            _sectionHeaderStyle.normal.textColor = new Color(0.8f, 0.9f, 1f);
            
            _stylesInitialized = true;
        }
        
        private float DrawSection(string title, string key, float y, float width, Func<float, float> drawContent)
        {
            bool isExpanded = _sectionExpanded.ContainsKey(key) && _sectionExpanded[key];
            
            // Header sezione
            if (GUI.Button(new Rect(10f, y, width - 20f, 30f), 
                (isExpanded ? "▼ " : "▶ ") + title, _sectionHeaderStyle))
            {
                _sectionExpanded[key] = !isExpanded;
            }
            y += 35f;
            
            // Contenuto sezione (se espansa)
            if (isExpanded)
            {
                float contentHeight = drawContent(y);
                y += contentHeight + 10f;
            }
            
            return y;
        }
        
        private float DrawOverwateringSection(float width, float startY)
        {
            float y = startY;
            y += DrawSectionDescription("Controlla quando la pianta entra in overwatering e gli effetti sul pH. La soglia determina quando l'idratazione è troppo alta, mentre il pH drift applica effetti negativi al pH quando la pianta è in overwatering.", width, y);
            y += DrawSlider("Soglia Overwatering (%)", ref DifficultyCalibrationConfig.OverwateringThresholdPercent, 50f, 100f, width, y);
            y += DrawSlider("Soglia Rimozione (%)", ref DifficultyCalibrationConfig.OverwateringRemovalPercent, 30f, 70f, width, y);
            y += DrawSlider("pH Drift Overwatering", ref DifficultyCalibrationConfig.OverwateringPhDrift, -10f, 0f, width, y);
            y += DrawSlider("Watering Accumulator", ref DifficultyCalibrationConfig.WateringAccumulator, 0.1f, 1.0f, width, y);
            return y - startY; // Restituisce solo l'altezza aggiunta
        }
        
        private float DrawMalusSection(float width, float startY)
        {
            float y = startY;
            y += DrawSectionDescription("Penalità applicate al score di condizione della pianta quando i parametri sono fuori range o in condizioni negative. Valori più alti = penalità più severe. Il Base Score è il punto di partenza per il calcolo della condizione.", width, y);
            y += DrawSlider("Base Score", ref DifficultyCalibrationConfig.BaseScore, 0, 100, width, y);
            y += DrawSlider("Malus Idratazione Fuori Range", ref DifficultyCalibrationConfig.MalusHydrationOutOfRange, 0, 50, width, y);
            y += DrawSlider("Malus Luce Assente/Sbagliata", ref DifficultyCalibrationConfig.MalusLightWrongOrAbsent, 0, 50, width, y);
            y += DrawSlider("Malus pH Opposto", ref DifficultyCalibrationConfig.MalusPhOpposite, 0, 50, width, y);
            y += DrawSlider("Malus pH Ultra", ref DifficultyCalibrationConfig.MalusPhUltra, 0, 50, width, y);
            y += DrawSlider("Malus pH Fuori Range Min", ref DifficultyCalibrationConfig.MalusPhOutOfRangeMin, 0, 20, width, y);
            y += DrawSlider("Malus pH Fuori Range Max", ref DifficultyCalibrationConfig.MalusPhOutOfRangeMax, 20, 50, width, y);
            y += DrawSlider("Malus pH Estremo", ref DifficultyCalibrationConfig.MalusPhExtreme, 30, 70, width, y);
            y += DrawSlider("Malus Overwatering", ref DifficultyCalibrationConfig.MalusOverwatering, 0, 50, width, y);
            y += DrawSlider("Malus Mold Mild", ref DifficultyCalibrationConfig.MalusMoldMild, 0, 50, width, y);
            y += DrawSlider("Malus Mold Severe", ref DifficultyCalibrationConfig.MalusMoldSevere, 0, 50, width, y);
            y += DrawSlider("Malus Burn Stress", ref DifficultyCalibrationConfig.MalusBurnStress, 0, 50, width, y);
            return y - startY;
        }
        
        private float DrawBonusSection(float width, float startY)
        {
            float y = startY;
            y += DrawSectionDescription("Bonus applicati al score di condizione quando i parametri sono nel range ottimale. Valori più alti = bonus maggiori. Questi si sommano al Base Score per migliorare la condizione della pianta.", width, y);
            y += DrawSlider("Bonus Idratazione Ottimale", ref DifficultyCalibrationConfig.BonusHydrationOptimal, 0, 50, width, y);
            y += DrawSlider("Bonus Luce Corretta", ref DifficultyCalibrationConfig.BonusLightCorrect, 0, 50, width, y);
            y += DrawSlider("Bonus Watering ON", ref DifficultyCalibrationConfig.BonusWateringOn, 0, 50, width, y);
            y += DrawSlider("Bonus pH Ottimale", ref DifficultyCalibrationConfig.BonusPhOptimal, 0, 50, width, y);
            y += DrawSlider("Bonus pH Ottimale Graduale", ref DifficultyCalibrationConfig.BonusPhOptimalGradual, 0, 20, width, y);
            y += DrawSlider("Bonus No Mold", ref DifficultyCalibrationConfig.BonusNoMold, 0, 50, width, y);
            return y - startY;
        }
        
        private float DrawPhBandsSection(float width, float startY)
        {
            float y = startY;
            y += DrawSectionDescription("Definisce le bande di pH del sistema. Le soglie separano le diverse zone: Ultra Acid (estremamente acido), Stable Acid, Neutral, Stable Basic, Ultra Basic (estremamente basico). Queste bande determinano gli effetti del pH sulle piante.", width, y);
            y += DrawSlider("Soglia Ultra Acid", ref DifficultyCalibrationConfig.PhThresholdUltraAcid, -100f, -50f, width, y);
            y += DrawSlider("Soglia Stable Acid", ref DifficultyCalibrationConfig.PhThresholdStableAcid, -50f, -10f, width, y);
            y += DrawSlider("Soglia Stable Basic", ref DifficultyCalibrationConfig.PhThresholdStableBasic, 10f, 50f, width, y);
            y += DrawSlider("Soglia Ultra Basic", ref DifficultyCalibrationConfig.PhThresholdUltraBasic, 50f, 100f, width, y);
            return y - startY;
        }
        
        private float DrawPhDriftSection(float width, float startY)
        {
            float y = startY;
            y += DrawSectionDescription("Modifica quanto ogni azione influisce sul pH globale. Valori negativi rendono il pH più acido, valori positivi più basico. LED Blue e Spray aumentano il pH (basico), LED Red e Overwatering lo diminuiscono (acido).", width, y);
            y += DrawSlider("pH Drift Overwatering", ref DifficultyCalibrationConfig.PhDriftOverwatering, -10f, 0f, width, y);
            y += DrawSlider("pH Drift LED Blue", ref DifficultyCalibrationConfig.PhDriftLedBlue, 0f, 10f, width, y);
            y += DrawSlider("pH Drift LED Red", ref DifficultyCalibrationConfig.PhDriftLedRed, -10f, 0f, width, y);
            y += DrawSlider("pH Drift Spray", ref DifficultyCalibrationConfig.PhDriftSpray, 0f, 10f, width, y);
            return y - startY;
        }
        
        private float DrawLedSection(float width, float startY)
        {
            float y = startY;
            y += DrawSectionDescription("Controlla gli effetti del LED sul pH e i malus per uso prolungato. I moltiplicatori aumentano l'effetto pH del LED nei giorni consecutivi. I malus si applicano quando il LED è acceso troppo a lungo, causando Burn Stress.", width, y);
            y += DrawSlider("LED Multiplier Giorno 1", ref DifficultyCalibrationConfig.LedMultiplierDay1, 0.5f, 2.0f, width, y);
            y += DrawSlider("LED Multiplier Giorni 2-3", ref DifficultyCalibrationConfig.LedMultiplierDays2_3, 1.0f, 3.0f, width, y);
            y += DrawSlider("LED Multiplier Giorno 4+", ref DifficultyCalibrationConfig.LedMultiplierDay4Plus, 1.5f, 4.0f, width, y);
            y += DrawSlider("LED Malus Base (≤3 giorni)", ref DifficultyCalibrationConfig.LedMalusBase, 0.5f, 2.0f, width, y);
            y += DrawSlider("LED Malus Growth (≥4 giorni)", ref DifficultyCalibrationConfig.LedMalusGrowth, 1.0f, 3.0f, width, y);
            y += DrawSlider("LED Malus Increment/Giorno", ref DifficultyCalibrationConfig.LedMalusIncrementPerDay, 0.1f, 0.5f, width, y);
            return y - startY;
        }
        
        private float DrawGrowthSection(float width, float startY)
        {
            float y = startY;
            y += DrawSectionDescription("Controlla la velocità di crescita delle piante. I punti determinano quanto velocemente avanzano di stadio. Punti più alti = crescita più veloce. Il decay idratazione riduce l'idratazione giornalmente quando il sistema è OFF.", width, y);
            y += DrawSlider("Punti Seed→Sprout", ref DifficultyCalibrationConfig.PointsSeedToSprout, 1, 20, width, y);
            y += DrawSlider("Punti Sprout→Mature", ref DifficultyCalibrationConfig.PointsSproutToMature, 1, 20, width, y);
            y += DrawSlider("Punti Cura Ideale", ref DifficultyCalibrationConfig.PointsIdealCare, 0, 10, width, y);
            y += DrawSlider("Punti Cura Parziale", ref DifficultyCalibrationConfig.PointsPartialCare, 0, 10, width, y);
            y += DrawSlider("Punti Nessuna Cura", ref DifficultyCalibrationConfig.PointsNoCare, 0, 5, width, y);
            y += DrawSlider("Decay Idratazione Giornaliero", ref DifficultyCalibrationConfig.DailyHydrationDecay, 0, 5, width, y);
            y += DrawSlider("Soglia Negligenza", ref DifficultyCalibrationConfig.NeglectThreshold, 1, 10, width, y);
            y += DrawSlider("Moltiplicatore pH Crescita", ref DifficultyCalibrationConfig.PhGrowthMultiplier, 0.1f, 5.0f, width, y);
            return y - startY;
        }
        
        private float DrawStageMultipliersSection(float width, float startY)
        {
            float y = startY;
            y += DrawSectionDescription("Moltiplicatori applicati ai punti crescita in base allo stadio della pianta. Valori > 1.0 aumentano la crescita, valori < 1.0 la rallentano. Ogni stadio può avere una velocità di crescita diversa.", width, y);
            y += DrawSlider("Moltiplicatore Empty", ref DifficultyCalibrationConfig.GrowthMultiplierEmpty, 0.5f, 2.0f, width, y);
            y += DrawSlider("Moltiplicatore Seed", ref DifficultyCalibrationConfig.GrowthMultiplierSeed, 0.5f, 2.0f, width, y);
            y += DrawSlider("Moltiplicatore Sprout", ref DifficultyCalibrationConfig.GrowthMultiplierSprout, 0.5f, 2.0f, width, y);
            y += DrawSlider("Moltiplicatore HarvestReady", ref DifficultyCalibrationConfig.GrowthMultiplierHarvestReady, 0.5f, 2.0f, width, y);
            y += DrawSlider("Moltiplicatore Resting", ref DifficultyCalibrationConfig.GrowthMultiplierResting, 0.5f, 2.0f, width, y);
            return y - startY;
        }
        
        private float DrawFertilizerSection(float width, float startY)
        {
            float y = startY;
            y += DrawSectionDescription("Controlla quantità e costi dei fertilizzanti. L'Amount determina quanto fertilizzante viene applicato, il Costo quanto CRY costa applicarlo. Il Decay Rate riduce il fertilizzante giornalmente.", width, y);
            y += DrawSlider("Fertilizzante Standard Amount (%)", ref DifficultyCalibrationConfig.FertilizerStandardAmount, 10, 50, width, y);
            y += DrawSlider("Fertilizzante Pure Amount (%)", ref DifficultyCalibrationConfig.FertilizerPureAmount, 20, 60, width, y);
            y += DrawSlider("Fertilizzante Prohibited Amount (%)", ref DifficultyCalibrationConfig.FertilizerProhibitedAmount, 20, 60, width, y);
            y += DrawSlider("Costo Standard (CRY)", ref DifficultyCalibrationConfig.FertilizerStandardCost, 10, 100, width, y);
            y += DrawSlider("Costo Pure (CRY)", ref DifficultyCalibrationConfig.FertilizerPureCost, 50, 200, width, y);
            y += DrawSlider("Costo Prohibited (CRY)", ref DifficultyCalibrationConfig.FertilizerProhibitedCost, 50, 200, width, y);
            y += DrawSlider("Decay Rate (%)", ref DifficultyCalibrationConfig.FertilizerDecayRate, 1f, 20f, width, y);
            return y - startY;
        }
        
        private float DrawMoldSection(float width, float startY)
        {
            float y = startY;
            y += DrawSectionDescription("Controlla il sistema di muffe. Le soglie determinano quando il rischio muffe diventa Mild, Severe o Critical. Overwatering, pH acido e negligenza nella potatura aumentano il rischio. Le penalità si applicano quando la pianta è infestata.", width, y);
            y += DrawSlider("Mold Soglia Mild", ref DifficultyCalibrationConfig.MoldMildThreshold, 0, 5, width, y);
            y += DrawSlider("Mold Soglia Severe", ref DifficultyCalibrationConfig.MoldSevereThreshold, 1, 10, width, y);
            y += DrawSlider("Mold Soglia Critical", ref DifficultyCalibrationConfig.MoldCriticalThreshold, 2, 10, width, y);
            y += DrawSlider("Giorni Overwatering per +1 Rischio", ref DifficultyCalibrationConfig.MoldOverwateringDaysThreshold, 1, 10, width, y);
            y += DrawSlider("pH Acido Threshold", ref DifficultyCalibrationConfig.MoldAcidicPhThreshold, -50f, -10f, width, y);
            y += DrawSlider("Accumulo Negligenza Potatura", ref DifficultyCalibrationConfig.MoldPruningNeglectAccumulation, 0f, 2.0f, width, y);
            y += DrawSlider("Penalità Score Mild", ref DifficultyCalibrationConfig.MoldMildScorePenalty, 0, 50, width, y);
            y += DrawSlider("Penalità Score Severe", ref DifficultyCalibrationConfig.MoldSevereScorePenalty, 0, 100, width, y);
            y += DrawSlider("Riduzione Livelli Mild", ref DifficultyCalibrationConfig.MoldMildLevelReduction, 0, 5, width, y);
            y += DrawSlider("Riduzione Livelli Severe", ref DifficultyCalibrationConfig.MoldSevereLevelReduction, 0, 10, width, y);
            return y - startY;
        }
        
        private float DrawConditionSection(float width, float startY)
        {
            float y = startY;
            y += DrawSectionDescription("Definisce le soglie per le condizioni della pianta basate sul score totale (0-100). Rigogliosa = migliore, Appassita = peggiore. I Forecast Delta determinano quando mostrare previsioni di miglioramento/peggioramento.", width, y);
            y += DrawSlider("Soglia Rigogliosa", ref DifficultyCalibrationConfig.ConditionThresholdRigogliosa, 70, 100, width, y);
            y += DrawSlider("Soglia Sana", ref DifficultyCalibrationConfig.ConditionThresholdSana, 50, 90, width, y);
            y += DrawSlider("Soglia Stressata", ref DifficultyCalibrationConfig.ConditionThresholdStressata, 30, 70, width, y);
            y += DrawSlider("Soglia Appassita", ref DifficultyCalibrationConfig.ConditionThresholdAppassita, 10, 50, width, y);
            y += DrawSlider("Forecast Delta Up", ref DifficultyCalibrationConfig.ForecastDeltaUp, 3, 20, width, y);
            y += DrawSlider("Forecast Delta Down", ref DifficultyCalibrationConfig.ForecastDeltaDown, -20, -3, width, y);
            return y - startY;
        }
        
        private float DrawHydrationRangeSection(float width, float startY)
        {
            float y = startY;
            y += DrawSectionDescription("Definisce i range di idratazione. Sotto la soglia Dry la pianta è considerata troppo secca, sopra la soglia Wet troppo bagnata. Questi valori determinano quando applicare malus per idratazione fuori range.", width, y);
            y += DrawSlider("Soglia Dry (%)", ref DifficultyCalibrationConfig.HydrationDryThreshold, 0, 40, width, y);
            y += DrawSlider("Soglia Wet (%)", ref DifficultyCalibrationConfig.HydrationWetThreshold, 60, 100, width, y);
            return y - startY;
        }
        
        private float DrawSlider(string label, ref int value, int min, int max, float width, float y)
        {
            GUI.Label(new Rect(20f, y, 300f, 25f), label, _labelStyle);
            float newValue = GUI.HorizontalSlider(new Rect(330f, y + 5f, 300f, 20f), value, min, max);
            value = Mathf.RoundToInt(newValue);
            GUI.Label(new Rect(640f, y, 100f, 25f), value.ToString(), _labelStyle);
            return 30f;
        }
        
        private float DrawSlider(string label, ref float value, float min, float max, float width, float y)
        {
            GUI.Label(new Rect(20f, y, 300f, 25f), label, _labelStyle);
            value = GUI.HorizontalSlider(new Rect(330f, y + 5f, 300f, 20f), value, min, max);
            GUI.Label(new Rect(640f, y, 100f, 25f), value.ToString("F2"), _labelStyle);
            return 30f;
        }
        
        private void ExportToTxt()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string fileName = $"DifficultyCalibration_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string filePath = Path.Combine(Application.persistentDataPath, fileName);
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== DIFFICULTY CALIBRATION EXPORT ===");
            sb.AppendLine($"Data: {timestamp}");
            sb.AppendLine($"Note: Modifiche parametri DOME per bilanciamento gameplay");
            sb.AppendLine();
            
            var allParams = DifficultyCalibrationConfig.GetAllParameters();
            
            // Raggruppa per categoria
            sb.AppendLine("--- OVERWATERING SYSTEM ---");
            ExportParam(sb, "OverwateringThresholdPercent", allParams);
            ExportParam(sb, "OverwateringRemovalPercent", allParams);
            ExportParam(sb, "OverwateringPhDrift", allParams);
            ExportParam(sb, "WateringAccumulator", allParams);
            sb.AppendLine();
            
            sb.AppendLine("--- MALUS CRESCITA ---");
            ExportParam(sb, "BaseScore", allParams);
            ExportParam(sb, "MalusHydrationOutOfRange", allParams);
            ExportParam(sb, "MalusLightWrongOrAbsent", allParams);
            ExportParam(sb, "MalusPhOpposite", allParams);
            ExportParam(sb, "MalusPhUltra", allParams);
            ExportParam(sb, "MalusPhOutOfRangeMin", allParams);
            ExportParam(sb, "MalusPhOutOfRangeMax", allParams);
            ExportParam(sb, "MalusPhExtreme", allParams);
            ExportParam(sb, "MalusOverwatering", allParams);
            ExportParam(sb, "MalusMoldMild", allParams);
            ExportParam(sb, "MalusMoldSevere", allParams);
            ExportParam(sb, "MalusBurnStress", allParams);
            sb.AppendLine();
            
            sb.AppendLine("--- BONUS CRESCITA ---");
            ExportParam(sb, "BonusHydrationOptimal", allParams);
            ExportParam(sb, "BonusLightCorrect", allParams);
            ExportParam(sb, "BonusWateringOn", allParams);
            ExportParam(sb, "BonusPhOptimal", allParams);
            ExportParam(sb, "BonusPhOptimalGradual", allParams);
            ExportParam(sb, "BonusNoMold", allParams);
            sb.AppendLine();
            
            sb.AppendLine("--- SISTEMA pH - BANDE ---");
            ExportParam(sb, "PhThresholdUltraAcid", allParams);
            ExportParam(sb, "PhThresholdStableAcid", allParams);
            ExportParam(sb, "PhThresholdStableBasic", allParams);
            ExportParam(sb, "PhThresholdUltraBasic", allParams);
            sb.AppendLine();
            
            sb.AppendLine("--- pH DRIFT AZIONI ---");
            ExportParam(sb, "PhDriftOverwatering", allParams);
            ExportParam(sb, "PhDriftLedBlue", allParams);
            ExportParam(sb, "PhDriftLedRed", allParams);
            ExportParam(sb, "PhDriftSpray", allParams);
            sb.AppendLine();
            
            sb.AppendLine("--- SISTEMA LED ---");
            ExportParam(sb, "LedMultiplierDay1", allParams);
            ExportParam(sb, "LedMultiplierDays2_3", allParams);
            ExportParam(sb, "LedMultiplierDay4Plus", allParams);
            ExportParam(sb, "LedMalusBase", allParams);
            ExportParam(sb, "LedMalusGrowth", allParams);
            ExportParam(sb, "LedMalusIncrementPerDay", allParams);
            sb.AppendLine();
            
            sb.AppendLine("--- SISTEMA CRESCITA ---");
            ExportParam(sb, "PointsSeedToSprout", allParams);
            ExportParam(sb, "PointsSproutToMature", allParams);
            ExportParam(sb, "PointsIdealCare", allParams);
            ExportParam(sb, "PointsPartialCare", allParams);
            ExportParam(sb, "PointsNoCare", allParams);
            ExportParam(sb, "DailyHydrationDecay", allParams);
            ExportParam(sb, "NeglectThreshold", allParams);
            ExportParam(sb, "PhGrowthMultiplier", allParams);
            sb.AppendLine();
            
            sb.AppendLine("--- MOLTIPLICATORI STADIO ---");
            ExportParam(sb, "GrowthMultiplierEmpty", allParams);
            ExportParam(sb, "GrowthMultiplierSeed", allParams);
            ExportParam(sb, "GrowthMultiplierSprout", allParams);
            ExportParam(sb, "GrowthMultiplierHarvestReady", allParams);
            ExportParam(sb, "GrowthMultiplierResting", allParams);
            sb.AppendLine();
            
            sb.AppendLine("--- SISTEMA FERTILIZZANTE ---");
            ExportParam(sb, "FertilizerStandardAmount", allParams);
            ExportParam(sb, "FertilizerPureAmount", allParams);
            ExportParam(sb, "FertilizerProhibitedAmount", allParams);
            ExportParam(sb, "FertilizerStandardCost", allParams);
            ExportParam(sb, "FertilizerPureCost", allParams);
            ExportParam(sb, "FertilizerProhibitedCost", allParams);
            ExportParam(sb, "FertilizerDecayRate", allParams);
            sb.AppendLine();
            
            sb.AppendLine("--- SISTEMA MUFFE ---");
            ExportParam(sb, "MoldMildThreshold", allParams);
            ExportParam(sb, "MoldSevereThreshold", allParams);
            ExportParam(sb, "MoldCriticalThreshold", allParams);
            ExportParam(sb, "MoldOverwateringDaysThreshold", allParams);
            ExportParam(sb, "MoldAcidicPhThreshold", allParams);
            ExportParam(sb, "MoldPruningNeglectAccumulation", allParams);
            ExportParam(sb, "MoldMildScorePenalty", allParams);
            ExportParam(sb, "MoldSevereScorePenalty", allParams);
            ExportParam(sb, "MoldMildLevelReduction", allParams);
            ExportParam(sb, "MoldSevereLevelReduction", allParams);
            sb.AppendLine();
            
            sb.AppendLine("--- SOGLIE CONDIZIONE ---");
            ExportParam(sb, "ConditionThresholdRigogliosa", allParams);
            ExportParam(sb, "ConditionThresholdSana", allParams);
            ExportParam(sb, "ConditionThresholdStressata", allParams);
            ExportParam(sb, "ConditionThresholdAppassita", allParams);
            ExportParam(sb, "ForecastDeltaUp", allParams);
            ExportParam(sb, "ForecastDeltaDown", allParams);
            sb.AppendLine();
            
            sb.AppendLine("--- RANGE IDRATAZIONE ---");
            ExportParam(sb, "HydrationDryThreshold", allParams);
            ExportParam(sb, "HydrationWetThreshold", allParams);
            sb.AppendLine();
            
            try
            {
                File.WriteAllText(filePath, sb.ToString());
                SporiumLogger.LogInfo(LogCategory.Dome, $"Export completato: {filePath}");
            }
            catch (Exception ex)
            {
                SporiumLogger.LogError(LogCategory.Dome, $"Errore export: {ex.Message}");
            }
        }
        
        private void ExportParam(StringBuilder sb, string paramName, Dictionary<string, object> allParams)
        {
            if (allParams.ContainsKey(paramName))
            {
                object currentValue = allParams[paramName];
                object originalValue = _originalValues.ContainsKey(paramName) ? _originalValues[paramName] : currentValue;
                
                string currentStr = currentValue is float f ? f.ToString("F2") : currentValue.ToString();
                string originalStr = originalValue is float f2 ? f2.ToString("F2") : originalValue.ToString();
                
                if (!currentValue.Equals(originalValue))
                {
                    sb.AppendLine($"{paramName}: {originalStr} → {currentStr}");
                }
                else
                {
                    sb.AppendLine($"{paramName}: {currentStr} (default)");
                }
            }
        }
        
        private float DrawSectionDescription(string description, float width, float y)
        {
            GUIStyle descStyle = new GUIStyle(_labelStyle);
            descStyle.fontSize = 18; // Aumentato da 14 a 18 per migliore leggibilità
            descStyle.wordWrap = true;
            descStyle.normal.textColor = new Color(0.85f, 0.9f, 1f); // Azzurro chiaro per distinguere
            descStyle.fontStyle = FontStyle.Italic;
            
            // Calcola altezza in base al contenuto
            GUIContent descContent = new GUIContent(description);
            float descHeight = descStyle.CalcHeight(descContent, width - 40f);
            descHeight = Mathf.Max(descHeight, 25f); // Altezza minima aumentata per font più grande
            
            GUI.Label(new Rect(20f, y, width - 40f, descHeight), descContent, descStyle);
            return descHeight + 12f; // Spacing dopo la descrizione aumentato
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

