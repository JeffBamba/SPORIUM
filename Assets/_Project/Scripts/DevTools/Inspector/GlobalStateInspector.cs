using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using _Project.Sporae.Core;
using Sporae.Core;
using Sporae.Dome.PotSystem;
using Sporae.Dome.PotSystem.Growth;
using Sporae.Dome.PotSystem.Condition;
using Sporae.Dome.PotSystem.Level;
using Sporae.Dome.PotSystem.Mold;
using Sporae.UI.UIToolkit.NotificationsFoundation;
using _Project;

namespace Sporae.DevTools
{
    /// <summary>
    /// State Inspector Globale - Console unificata per visualizzare e modificare lo stato di tutti i sistemi
    /// Tasto F1 per aprire/chiudere
    /// Solo per Editor/Development build
    /// </summary>
    public class GlobalStateInspector : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugConsole = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private bool showOnStart = false;
        
        private bool _isConsoleOpen = false;
        private Vector2 _scrollPosition;
        private Dictionary<string, bool> _sectionExpanded = new Dictionary<string, bool>();
        
        // Input fields per modifica valori
        private string _cryInputValue = "250";
        private string _actionsInputValue = "4";
        private string _dayInputValue = "1";
        private string _phInputValue = "0";
        private string _potSearchFilter = "";
        
        // Cache sistemi
        private GameManager _gameManager;
        private PhSystem _phSystem;
        private DayCycleSystem _dayCycleSystem;
        private DayCycleController _dayCycleController;
        private SaveManager _saveManager;
        
        // Performance metrics
        private float _fps;
        private float _frameTime;
        private int _gcAlloc;
        private int _memoryUsage;
        private float _updateInterval = 0.5f;
        private float _lastUpdateTime;
        
        // Snapshot system
        private string _snapshotPath = "";
        private string _snapshotData = "";
        
        // Rettangolo della console per bloccare input
        private Rect _consoleRect;
        
        private void Awake()
        {
            _isConsoleOpen = showOnStart;
            
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            enableDebugConsole = false;
#endif
            
            SporiumLogger.LogInfo(LogCategory.Core, $"GlobalStateInspector Awake - enableDebugConsole: {enableDebugConsole}, toggleKey: {toggleKey}");
        }
        
        private void Start()
        {
            TryGetSystems();
            
            // Sottoscrivi a eventi per late binding
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered += OnServiceRegistered;
            }
        }
        
        private void OnDestroy()
        {
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered -= OnServiceRegistered;
            }
        }
        
        private void Update()
        {
            if (!enableDebugConsole) return;
            
            if (Input.GetKeyDown(toggleKey))
            {
                _isConsoleOpen = !_isConsoleOpen;
                SporiumLogger.LogInfo(LogCategory.Core, $"GlobalStateInspector {(_isConsoleOpen ? "aperto" : "chiuso")}");
            }
            
            // Update performance metrics
            if (_isConsoleOpen && Time.time - _lastUpdateTime >= _updateInterval)
            {
                UpdatePerformanceMetrics();
                _lastUpdateTime = Time.time;
            }
        }
        
        private void TryGetSystems()
        {
            _gameManager = ServiceContainer.Instance?.Get<GameManager>();
            _phSystem = ServiceContainer.Instance?.Get<PhSystem>(suppressWarning: true);
            _dayCycleSystem = ServiceContainer.Instance?.Get<DayCycleSystem>();
            _dayCycleController = FindObjectOfType<DayCycleController>();
            _saveManager = ServiceContainer.Instance?.Get<SaveManager>(suppressWarning: true);
        }
        
        private void OnServiceRegistered(object service)
        {
            if (service is GameManager && _gameManager == null)
                _gameManager = service as GameManager;
            else if (service is PhSystem && _phSystem == null)
                _phSystem = service as PhSystem;
            else if (service is DayCycleSystem && _dayCycleSystem == null)
                _dayCycleSystem = service as DayCycleSystem;
            else if (service is SaveManager && _saveManager == null)
                _saveManager = service as SaveManager;
        }
        
        private void UpdatePerformanceMetrics()
        {
            _fps = 1f / Time.deltaTime;
            _frameTime = Time.deltaTime * 1000f; // ms
            _gcAlloc = (int)(GC.GetTotalMemory(false) / 1024 / 1024); // MB
            _memoryUsage = (int)(System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024); // MB
        }
        
        private void OnGUI()
        {
            if (!enableDebugConsole || !_isConsoleOpen) return;
            
            // Stile della console
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.9f));
            
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 18;
            
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 16;
            
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.normal.textColor = Color.cyan;
            headerStyle.fontSize = 20;
            headerStyle.fontStyle = FontStyle.Bold;
            
            // Area console principale
            float consoleWidth = 900f;
            float consoleHeight = Mathf.Min(Screen.height - 20, 1000f);
            Rect consoleRect = new Rect(Screen.width - consoleWidth - 10, 10, consoleWidth, consoleHeight);
            _consoleRect = consoleRect;
            
            GUILayout.BeginArea(consoleRect, boxStyle);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true);
            GUILayout.BeginVertical();
            
            // Header
            GUILayout.Label("🔍 GLOBAL STATE INSPECTOR", headerStyle);
            GUILayout.Space(10);
            
            // Sezioni
            DrawGameManagerSection(labelStyle, buttonStyle, headerStyle);
            GUILayout.Space(10);
            
            DrawPhSystemSection(labelStyle, buttonStyle, headerStyle);
            GUILayout.Space(10);
            
            DrawPotSystemSection(labelStyle, buttonStyle, headerStyle);
            GUILayout.Space(10);
            
            DrawInventorySection(labelStyle, buttonStyle, headerStyle);
            GUILayout.Space(10);
            
            DrawDayCycleSection(labelStyle, buttonStyle, headerStyle);
            GUILayout.Space(10);
            
            DrawSaveSystemSection(labelStyle, buttonStyle, headerStyle);
            GUILayout.Space(10);
            
            DrawPerformanceSection(labelStyle, headerStyle);
            GUILayout.Space(10);
            
            // Export e Snapshot
            DrawExportSection(labelStyle, buttonStyle, headerStyle);
            
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        
        private void DrawGameManagerSection(GUIStyle labelStyle, GUIStyle buttonStyle, GUIStyle headerStyle)
        {
            bool expanded = GetSectionExpanded("GameManager");
            expanded = DrawSectionHeader("GameManager State", expanded, headerStyle);
            SetSectionExpanded("GameManager", expanded);
            
            if (!expanded) return;
            
            if (_gameManager == null)
            {
                GUILayout.Label("GameManager non disponibile", labelStyle);
                return;
            }
            
            GUILayout.BeginVertical(GUI.skin.box);
            
            // CRY
            GUILayout.BeginHorizontal();
            GUILayout.Label($"CRY: {_gameManager.CurrentCRY}", labelStyle, GUILayout.Width(200));
            _cryInputValue = GUILayout.TextField(_cryInputValue, GUILayout.Width(100));
            if (GUILayout.Button("Set", buttonStyle, GUILayout.Width(80)))
            {
                if (int.TryParse(_cryInputValue, out int cry))
                {
                    _gameManager.EconomySystem.SetCRY(cry);
                    SporiumLogger.LogInfo(LogCategory.Core, $"CRY impostato a {cry}");
                }
            }
            if (GUILayout.Button("+100", buttonStyle, GUILayout.Width(80)))
            {
                _gameManager.EconomySystem.Add(100);
            }
            if (GUILayout.Button("-100", buttonStyle, GUILayout.Width(80)))
            {
                _gameManager.EconomySystem.Spend(100);
            }
            GUILayout.EndHorizontal();
            
            // Actions
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Actions: {_gameManager.ActionsLeft} / {_gameManager.ActionSystem.MaxActions}", labelStyle, GUILayout.Width(200));
            _actionsInputValue = GUILayout.TextField(_actionsInputValue, GUILayout.Width(100));
            if (GUILayout.Button("Add", buttonStyle, GUILayout.Width(80)))
            {
                if (int.TryParse(_actionsInputValue, out int actions))
                {
                    _gameManager.ActionSystem.AddActions(actions);
                    SporiumLogger.LogInfo(LogCategory.Core, $"Aggiunte {actions} azioni");
                }
            }
            GUILayout.EndHorizontal();
            
            // Day
            if (_dayCycleSystem != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Day: {_dayCycleSystem.CurrentDay}", labelStyle, GUILayout.Width(200));
                _dayInputValue = GUILayout.TextField(_dayInputValue, GUILayout.Width(100));
                if (GUILayout.Button("Set", buttonStyle, GUILayout.Width(80)))
                {
                    if (int.TryParse(_dayInputValue, out int day) && day > 0)
                    {
                        // Nota: DayCycleSystem potrebbe non avere metodo SetDay pubblico
                        // Per ora solo visualizzazione
                        SporiumLogger.LogWarning(LogCategory.Core, "SetDay non implementato in DayCycleSystem");
                    }
                }
                GUILayout.EndHorizontal();
            }
            
            // Condensation
            if (_gameManager.CondensationSystem != null)
            {
                GUILayout.Label($"Condensation: {_gameManager.CondensationSystem.CondensationAmount:F2}", labelStyle);
            }
            
            GUILayout.EndVertical();
        }
        
        private void DrawPhSystemSection(GUIStyle labelStyle, GUIStyle buttonStyle, GUIStyle headerStyle)
        {
            bool expanded = GetSectionExpanded("PhSystem");
            expanded = DrawSectionHeader("pH System State", expanded, headerStyle);
            SetSectionExpanded("PhSystem", expanded);
            
            if (!expanded) return;
            
            if (_phSystem == null)
            {
                GUILayout.Label("PhSystem non disponibile", labelStyle);
                return;
            }
            
            GUILayout.BeginVertical(GUI.skin.box);
            
            // pH corrente
            var bandColor = _phSystem.GetBandColor();
            var oldColor = GUI.color;
            GUI.color = bandColor;
            GUILayout.Label($"pH Corrente: {_phSystem.CurrentPh:F2}", labelStyle);
            GUI.color = oldColor;
            GUILayout.Label($"Banda: {_phSystem.GetBandName()}", labelStyle);
            
            // Input pH
            GUILayout.BeginHorizontal();
            GUILayout.Label("Imposta pH:", labelStyle, GUILayout.Width(150));
            _phInputValue = GUILayout.TextField(_phInputValue, GUILayout.Width(100));
            if (GUILayout.Button("Set", buttonStyle, GUILayout.Width(80)))
            {
                if (float.TryParse(_phInputValue, out float ph))
                {
                    _phSystem.SetPh(ph);
                    SporiumLogger.LogInfo(LogCategory.Ph, $"pH impostato a {ph:F2}");
                }
            }
            GUILayout.EndHorizontal();
            
            // Quick values
            GUILayout.Label("Valori Rapidi:", labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ultra Acid", buttonStyle, GUILayout.Width(120))) _phSystem.SetPh(-100f);
            if (GUILayout.Button("Stable Acid", buttonStyle, GUILayout.Width(120))) _phSystem.SetPh(-50f);
            if (GUILayout.Button("Neutral", buttonStyle, GUILayout.Width(120))) _phSystem.SetPh(0f);
            if (GUILayout.Button("Stable Basic", buttonStyle, GUILayout.Width(120))) _phSystem.SetPh(50f);
            if (GUILayout.Button("Ultra Basic", buttonStyle, GUILayout.Width(120))) _phSystem.SetPh(100f);
            GUILayout.EndHorizontal();
            
            // Modifiche incrementali
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-10", buttonStyle, GUILayout.Width(80))) _phSystem.ApplyInstantDelta(-10f, "Debug_Manual");
            if (GUILayout.Button("-5", buttonStyle, GUILayout.Width(80))) _phSystem.ApplyInstantDelta(-5f, "Debug_Manual");
            if (GUILayout.Button("+5", buttonStyle, GUILayout.Width(80))) _phSystem.ApplyInstantDelta(5f, "Debug_Manual");
            if (GUILayout.Button("+10", buttonStyle, GUILayout.Width(80))) _phSystem.ApplyInstantDelta(10f, "Debug_Manual");
            if (GUILayout.Button("Reset", buttonStyle, GUILayout.Width(100))) _phSystem.Reset();
            GUILayout.EndHorizontal();
            
            // Breakdown
            var contrib = _phSystem.GetContributions();
            GUILayout.Label($"Base: {contrib.BasePh:F2}", labelStyle);
            GUILayout.Label($"Plants: {contrib.PlantsDrift:F2}", labelStyle);
            GUILayout.Label($"Actions: {contrib.ActionsDrift:F2}", labelStyle);
            GUILayout.Label($"Events: {contrib.EventsDrift:F2}", labelStyle);
            GUILayout.Label($"Daily: {contrib.DailyDrift:F2}", labelStyle);
            GUILayout.Label($"Total: {contrib.Total:F2}", labelStyle);
            
            GUILayout.EndVertical();
        }
        
        private void DrawPotSystemSection(GUIStyle labelStyle, GUIStyle buttonStyle, GUIStyle headerStyle)
        {
            bool expanded = GetSectionExpanded("PotSystem");
            expanded = DrawSectionHeader("Pot System State", expanded, headerStyle);
            SetSectionExpanded("PotSystem", expanded);
            
            if (!expanded) return;
            
            if (_dayCycleController == null)
            {
                GUILayout.Label("DayCycleController non disponibile", labelStyle);
                return;
            }
            
            GUILayout.BeginVertical(GUI.skin.box);
            
            // Search filter
            GUILayout.BeginHorizontal();
            GUILayout.Label("Filtra:", labelStyle, GUILayout.Width(80));
            _potSearchFilter = GUILayout.TextField(_potSearchFilter, GUILayout.Width(200));
            GUILayout.EndHorizontal();
            
            // Lista pot (trova tutti i PotActions nella scena)
            var potActions = FindObjectsOfType<PotActions>();
            if (potActions == null || potActions.Length == 0)
            {
                GUILayout.Label("Nessun pot trovato nella scena", labelStyle);
            }
            else
            {
                foreach (var potAction in potActions)
                {
                    if (potAction == null || potAction.PotState == null) continue;
                    var pot = potAction.PotState;
                    
                    if (!string.IsNullOrEmpty(_potSearchFilter) && 
                        !pot.PotId.Contains(_potSearchFilter, StringComparison.OrdinalIgnoreCase) &&
                        !(pot.PlantCode?.Contains(_potSearchFilter, StringComparison.OrdinalIgnoreCase) ?? false))
                        continue;
                    
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label($"Pot: {pot.PotId}", labelStyle);
                    
                    if (pot.HasPlant)
                    {
                        var plantData = pot.GetPlantData();
                        GUILayout.Label($"Plant: {pot.PlantCode} (Stage: {pot.Stage})", labelStyle);
                        if (plantData != null)
                        {
                            GUILayout.Label($"Family: {plantData.Family}", labelStyle);
                        }
                        GUILayout.Label($"Hydration: {pot.Hydration}", labelStyle);
                        GUILayout.Label($"Light: {pot.LightExposure}", labelStyle);
                        GUILayout.Label($"Fertilizer: {pot.FertilizerLevel}%", labelStyle);
                        
                        // Condition
                        if (_phSystem != null && plantData != null)
                        {
                            var potConfig = Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");
                            if (potConfig != null)
                            {
                                var condition = PlantConditionSystem.CalculateCondition(
                                    pot, plantData, _phSystem, potConfig, _dayCycleSystem?.CurrentDay ?? 1);
                                GUILayout.Label($"Condition: {condition.Score}/100", labelStyle);
                            }
                        }
                    }
                    else
                    {
                        GUILayout.Label("Vuoto", labelStyle);
                    }
                    
                    GUILayout.EndVertical();
                }
            }
            
            GUILayout.EndVertical();
        }
        
        private static readonly string[] KnownItemTypeIds = new[]
        {
            Items.FruitFerricPod, Items.FruitArcticPod, Items.FruitGlassPod,
            Items.Water, Items.WholePlant, Items.OrganicScrap001,
            Items.FertilizerStandard, Items.FertilizerPure, Items.FertilizerProhibited,
            Items.SprayAntifungal, Items.AdditiveBasic, Items.AdditiveAcid,
            Items.StemCellVegetable, Items.StemCellFungus, Items.StemCellAnimal,
            Items.ProteinResidue, Items.ReagentX, Items.ReagentY
        };

        private int _addItemTypeIndex;
        private string _addItemTypeIdCustom = "";

        private void DrawInventorySection(GUIStyle labelStyle, GUIStyle buttonStyle, GUIStyle headerStyle)
        {
            bool expanded = GetSectionExpanded("Inventory");
            expanded = DrawSectionHeader("Inventory State", expanded, headerStyle);
            SetSectionExpanded("Inventory", expanded);
            
            if (!expanded) return;
            
            if (_gameManager == null)
            {
                GUILayout.Label("GameManager non disponibile", labelStyle);
                return;
            }
            
            GUILayout.BeginVertical(GUI.skin.box);
            
            var inventory = _gameManager.PlayerInventory;
            if (inventory == null)
            {
                GUILayout.Label("Inventory non disponibile", labelStyle);
            }
            else
            {
                var items = inventory.Items;
                if (items == null || items.Count == 0)
                {
                    GUILayout.Label("Inventory vuoto", labelStyle);
                }
                else
                {
                    foreach (var slot in items)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"{slot.TypeId}: {slot.Quantity}", labelStyle, GUILayout.Width(300));
                        if (GUILayout.Button("+1", buttonStyle, GUILayout.Width(60)))
                        {
                            inventory.Add(slot.TypeId, 1);
                        }
                        if (GUILayout.Button("-1", buttonStyle, GUILayout.Width(60)))
                        {
                            inventory.Consume(slot.TypeId, 1);
                        }
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.Space(8);
                GUILayout.Label("Aggiungi item (typeId):", labelStyle);
                GUILayout.BeginHorizontal();
                _addItemTypeIndex = Mathf.Clamp(_addItemTypeIndex, 0, KnownItemTypeIds.Length - 1);
                _addItemTypeIndex = Mathf.Clamp(GUILayout.SelectionGrid(_addItemTypeIndex, KnownItemTypeIds, 4, buttonStyle, GUILayout.MaxWidth(400)), 0, KnownItemTypeIds.Length - 1);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                _addItemTypeIdCustom = GUILayout.TextField(_addItemTypeIdCustom, 32, GUILayout.Width(180));
                if (GUILayout.Button("Aggiungi 1", buttonStyle, GUILayout.Width(90)))
                {
                    string typeId = string.IsNullOrWhiteSpace(_addItemTypeIdCustom) ? KnownItemTypeIds[_addItemTypeIndex] : _addItemTypeIdCustom.Trim();
                    if (!string.IsNullOrEmpty(typeId))
                    {
                        inventory.Add(typeId, 1);
                        SporiumLogger.LogInfo(LogCategory.UI, $"Debug: aggiunto 1x {typeId}");
                    }
                }
                GUILayout.EndHorizontal();
            }
            
            GUILayout.EndVertical();
        }
        
        private void DrawDayCycleSection(GUIStyle labelStyle, GUIStyle buttonStyle, GUIStyle headerStyle)
        {
            bool expanded = GetSectionExpanded("DayCycle");
            expanded = DrawSectionHeader("Day Cycle State", expanded, headerStyle);
            SetSectionExpanded("DayCycle", expanded);
            
            if (!expanded) return;
            
            if (_dayCycleSystem == null)
            {
                GUILayout.Label("DayCycleSystem non disponibile", labelStyle);
                return;
            }
            
            GUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.Label($"Current Day: {_dayCycleSystem.CurrentDay}", labelStyle);
            GUILayout.Label($"Can End Day: {_gameManager?.CurrentCRY >= 20}", labelStyle);
            
            if (GUILayout.Button("End Day", buttonStyle, GUILayout.Width(150)))
            {
                if (_gameManager != null && _gameManager.CurrentCRY >= 20)
                {
                    // Nota: EndDay potrebbe non essere pubblico, per ora solo log
                    SporiumLogger.LogInfo(LogCategory.Dome, "End Day button premuto (funzionalità da implementare)");
                }
                else
                {
                    SporiumLogger.LogWarning(LogCategory.Dome, "Non puoi terminare il giorno: CRY insufficienti (< 20)");
                }
            }
            
            GUILayout.EndVertical();
        }
        
        private void DrawSaveSystemSection(GUIStyle labelStyle, GUIStyle buttonStyle, GUIStyle headerStyle)
        {
            bool expanded = GetSectionExpanded("SaveSystem");
            expanded = DrawSectionHeader("Save System State", expanded, headerStyle);
            SetSectionExpanded("SaveSystem", expanded);
            
            if (!expanded) return;
            
            if (_saveManager == null)
            {
                GUILayout.Label("SaveManager non disponibile", labelStyle);
                return;
            }
            
            GUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.Label("Save System disponibile", labelStyle);
            if (_saveManager.SaveExists("default"))
                GUILayout.Label($"Ultimo salvataggio: {_saveManager.GetSaveTimestamp("default")}", labelStyle);
            
            if (GUILayout.Button("Save", buttonStyle, GUILayout.Width(150)))
            {
                bool ok = _saveManager.SaveGame("default");
                if (ok)
                {
                    SporiumLogger.LogInfo(LogCategory.Save, "Salvataggio completato (slot default)");
                    var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                    if (foundation != null && foundation.Enabled)
                        foundation.PostToast("SYS-003", new NotificationPayload());
                }
                else
                    SporiumLogger.LogWarning(LogCategory.Save, "Salvataggio fallito");
            }
            
            if (GUILayout.Button("Load", buttonStyle, GUILayout.Width(150)))
            {
                if (!_saveManager.SaveExists("default"))
                {
                    SporiumLogger.LogWarning(LogCategory.Save, "Nessun salvataggio da caricare");
                }
                else
                {
                    bool ok = _saveManager.LoadGame("default");
                    if (ok)
                        SporiumLogger.LogInfo(LogCategory.Save, "Caricamento completato (slot default)");
                    else
                        SporiumLogger.LogWarning(LogCategory.Save, "Caricamento fallito");
                }
            }
            
            if (GUILayout.Button("Delete save", buttonStyle, GUILayout.Width(150)))
            {
                if (_saveManager.SaveExists("default"))
                {
                    bool ok = _saveManager.DeleteSave("default");
                    if (ok)
                        SporiumLogger.LogInfo(LogCategory.Save, "Salvataggio eliminato (slot default)");
                    else
                        SporiumLogger.LogWarning(LogCategory.Save, "Eliminazione fallita");
                }
                else
                    SporiumLogger.LogWarning(LogCategory.Save, "Nessun salvataggio da eliminare");
            }
            
            GUILayout.EndVertical();
        }
        
        private void DrawPerformanceSection(GUIStyle labelStyle, GUIStyle headerStyle)
        {
            bool expanded = GetSectionExpanded("Performance");
            expanded = DrawSectionHeader("Performance Metrics", expanded, headerStyle);
            SetSectionExpanded("Performance", expanded);
            
            if (!expanded) return;
            
            GUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.Label($"FPS: {_fps:F1}", labelStyle);
            GUILayout.Label($"Frame Time: {_frameTime:F2} ms", labelStyle);
            GUILayout.Label($"GC Memory: {_gcAlloc} MB", labelStyle);
            GUILayout.Label($"Total Memory: {_memoryUsage} MB", labelStyle);
            
            GUILayout.EndVertical();
        }
        
        private void DrawExportSection(GUIStyle labelStyle, GUIStyle buttonStyle, GUIStyle headerStyle)
        {
            bool expanded = GetSectionExpanded("Export");
            expanded = DrawSectionHeader("Export & Snapshot", expanded, headerStyle);
            SetSectionExpanded("Export", expanded);
            
            if (!expanded) return;
            
            GUILayout.BeginVertical(GUI.skin.box);
            
            if (GUILayout.Button("Export State (JSON)", buttonStyle, GUILayout.Width(200)))
            {
                ExportStateToJSON();
            }
            
            if (GUILayout.Button("Take Snapshot", buttonStyle, GUILayout.Width(200)))
            {
                TakeSnapshot();
            }
            
            if (!string.IsNullOrEmpty(_snapshotData))
            {
                GUILayout.Label($"Snapshot salvato: {_snapshotPath}", labelStyle);
                if (GUILayout.Button("Load Snapshot", buttonStyle, GUILayout.Width(200)))
                {
                    LoadSnapshot();
                }
            }
            
            GUILayout.EndVertical();
        }
        
        private bool DrawSectionHeader(string title, bool expanded, GUIStyle headerStyle)
        {
            GUILayout.BeginHorizontal();
            string symbol = expanded ? "▼" : "▶";
            if (GUILayout.Button($"{symbol} {title}", headerStyle, GUILayout.ExpandWidth(true)))
            {
                return !expanded;
            }
            GUILayout.EndHorizontal();
            return expanded;
        }
        
        private bool GetSectionExpanded(string sectionName)
        {
            if (!_sectionExpanded.ContainsKey(sectionName))
                _sectionExpanded[sectionName] = true; // Default expanded
            return _sectionExpanded[sectionName];
        }
        
        private void SetSectionExpanded(string sectionName, bool expanded)
        {
            _sectionExpanded[sectionName] = expanded;
        }
        
        private void ExportStateToJSON()
        {
            try
            {
                var state = new
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    GameManager = _gameManager != null ? new
                    {
                        CRY = _gameManager.CurrentCRY,
                        Actions = _gameManager.ActionsLeft
                    } : null,
                    PhSystem = _phSystem != null ? new
                    {
                        CurrentPh = _phSystem.CurrentPh,
                        Band = _phSystem.GetBandName()
                    } : null,
                    DayCycle = _dayCycleSystem != null ? new
                    {
                        CurrentDay = _dayCycleSystem.CurrentDay
                    } : null
                };
                
                string json = JsonUtility.ToJson(state, true);
                string path = Path.Combine(Application.persistentDataPath, $"state_export_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                File.WriteAllText(path, json);
                
                SporiumLogger.LogInfo(LogCategory.Core, $"Stato esportato in {path}");
            }
            catch (Exception ex)
            {
                SporiumLogger.LogError(LogCategory.Core, $"Errore export stato: {ex.Message}");
            }
        }
        
        private void TakeSnapshot()
        {
            try
            {
                var snapshot = new
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    GameManager = _gameManager != null ? new
                    {
                        CRY = _gameManager.CurrentCRY,
                        Actions = _gameManager.ActionsLeft
                    } : null,
                    PhSystem = _phSystem != null ? new
                    {
                        CurrentPh = _phSystem.CurrentPh
                    } : null,
                    DayCycle = _dayCycleSystem != null ? new
                    {
                        CurrentDay = _dayCycleSystem.CurrentDay
                    } : null
                };
                
                _snapshotData = JsonUtility.ToJson(snapshot, true);
                _snapshotPath = Path.Combine(Application.persistentDataPath, $"snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                File.WriteAllText(_snapshotPath, _snapshotData);
                
                SporiumLogger.LogInfo(LogCategory.Core, $"Snapshot salvato in {_snapshotPath}");
            }
            catch (Exception ex)
            {
                SporiumLogger.LogError(LogCategory.Core, $"Errore snapshot: {ex.Message}");
            }
        }
        
        private void LoadSnapshot()
        {
            if (string.IsNullOrEmpty(_snapshotData))
            {
                SporiumLogger.LogWarning(LogCategory.Core, "Nessuno snapshot da caricare");
                return;
            }
            
            try
            {
                // Nota: Implementare caricamento snapshot se necessario
                SporiumLogger.LogInfo(LogCategory.Core, "Load snapshot (funzionalità da implementare completamente)");
            }
            catch (Exception ex)
            {
                SporiumLogger.LogError(LogCategory.Core, $"Errore load snapshot: {ex.Message}");
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

