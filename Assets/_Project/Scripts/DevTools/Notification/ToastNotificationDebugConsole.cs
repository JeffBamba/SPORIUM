using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _Project.Sporae.Core;
using Sporae.DevTools;
using _Project.UI.HUDNotifications2_0;

namespace Sporae.DevTools
{
    /// <summary>
    /// Console di debug per toast notifications (F9)
    /// Permette di triggerare toast manualmente, visualizzare history, filtri, statistics
    /// Solo per Editor/Development build
    /// </summary>
    public class ToastNotificationDebugConsole : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugConsole = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F9;
        [SerializeField] private bool showOnStart = false;
        
        private bool _isConsoleOpen = false;
        private Vector2 _scrollPosition;
        private Vector2 _historyScrollPosition;
        private Dictionary<string, bool> _sectionExpanded = new Dictionary<string, bool>();
        
        // Input fields per trigger toast
        private ToastNotificationType _selectedType = ToastNotificationType.Info;
        private string _messageInput = "Test message";
        private string _codeInput = "TEST-001";
        
        // Filtri history
        private ToastNotificationType _filterType = ToastNotificationType.Info;
        private string _filterCode = "";
        private bool _useTypeFilter = false;
        private bool _useCodeFilter = false;
        
        // Cache
        private ToastNotificationManager _manager;
        private ToastNotificationHistory _history;
        
        // Cache HUD Notifications 2.0
        private HUDNotificationFeedManager2_0 _manager2_0;
        private HUDNotificationConfig2_0 _config2_0;
        
        private Rect _consoleRect;
        
        private void Awake()
        {
            _isConsoleOpen = showOnStart;
            
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            enableDebugConsole = false;
#endif
            
            SporiumLogger.LogInfo(LogCategory.UI, $"ToastNotificationDebugConsole Awake - enableDebugConsole: {enableDebugConsole}, toggleKey: {toggleKey}");
        }
        
        private void Update()
        {
            if (!enableDebugConsole) return;
            
            if (Input.GetKeyDown(toggleKey))
            {
                _isConsoleOpen = !_isConsoleOpen;
                SporiumLogger.LogInfo(LogCategory.UI, $"ToastNotificationDebugConsole {(_isConsoleOpen ? "aperto" : "chiuso")}");
            }
            
            // Aggiorna cache
            if (_manager == null)
            {
                _manager = ServiceContainer.Instance?.Get<ToastNotificationManager>();
                if (_manager != null)
                    _history = _manager.GetHistory();
            }
            
            // Aggiorna cache HUD Notifications 2.0
            if (_manager2_0 == null)
            {
                _manager2_0 = ServiceContainer.Instance?.Get<HUDNotificationFeedManager2_0>(suppressWarning: true);
                if (_manager2_0 != null)
                    _config2_0 = _manager2_0.GetConfig();
            }
        }
        
        private void OnGUI()
        {
            if (!enableDebugConsole || !_isConsoleOpen) return;
            
            // Stile della console
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.95f));
            
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 14;
            
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 12;
            
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.normal.textColor = Color.cyan;
            headerStyle.fontSize = 16;
            headerStyle.fontStyle = FontStyle.Bold;
            
            // Console window
            float width = 600f;
            float height = 700f;
            float x = Screen.width - width - 20f;
            float y = 20f;
            
            _consoleRect = new Rect(x, y, width, height);
            GUILayout.BeginArea(_consoleRect, boxStyle);
            
            // Header
            GUILayout.BeginHorizontal();
            GUILayout.Label("Toast Notification Debug Console (F9)", headerStyle);
            if (GUILayout.Button("X", buttonStyle, GUILayout.Width(30)))
            {
                _isConsoleOpen = false;
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            
            // Sezione 1: Trigger Toast
            DrawTriggerSection(labelStyle, buttonStyle, headerStyle);
            
            GUILayout.Space(10);
            
            // Sezione 2: Quick Actions
            DrawQuickActionsSection(labelStyle, buttonStyle, headerStyle);
            
            GUILayout.Space(10);
            
            // Sezione 3: History Viewer
            DrawHistorySection(labelStyle, buttonStyle, headerStyle);
            
            GUILayout.Space(10);
            
            // Sezione 4: Statistics
            DrawStatisticsSection(labelStyle, buttonStyle, headerStyle);
            
            GUILayout.Space(10);
            
            // Sezione 5: Settings
            DrawSettingsSection(labelStyle, buttonStyle, headerStyle);
            
            GUILayout.Space(10);
            
            // Sezione 6: HUD Notifications 2.0 Runtime Editor
            DrawHUDNotifications2_0RuntimeEditor(labelStyle, buttonStyle, headerStyle);
            
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        
        private void DrawTriggerSection(GUIStyle labelStyle, GUIStyle buttonStyle, GUIStyle headerStyle)
        {
            bool expanded = GetSectionExpanded("Trigger");
            expanded = DrawSectionHeader("1. Trigger Toast", expanded, headerStyle);
            SetSectionExpanded("Trigger", expanded);
            
            if (!expanded) return;
            
            GUILayout.BeginVertical(GUI.skin.box);
            
            // Tipo toast
            GUILayout.BeginHorizontal();
            GUILayout.Label("Type:", labelStyle, GUILayout.Width(100));
            string[] typeNames = System.Enum.GetNames(typeof(ToastNotificationType));
            int currentIndex = System.Array.IndexOf(typeNames, _selectedType.ToString());
            int newIndex = GUILayout.SelectionGrid(currentIndex, typeNames, 3);
            if (newIndex != currentIndex && newIndex >= 0 && newIndex < typeNames.Length)
            {
                _selectedType = (ToastNotificationType)System.Enum.Parse(typeof(ToastNotificationType), typeNames[newIndex]);
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Messaggio
            GUILayout.BeginHorizontal();
            GUILayout.Label("Message:", labelStyle, GUILayout.Width(100));
            _messageInput = GUILayout.TextField(_messageInput, GUILayout.Width(300));
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Codice
            GUILayout.BeginHorizontal();
            GUILayout.Label("Code:", labelStyle, GUILayout.Width(100));
            _codeInput = GUILayout.TextField(_codeInput, GUILayout.Width(300));
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Pulsante Show
            if (GUILayout.Button("Show Toast", buttonStyle))
            {
                if (_manager != null)
                {
                    _manager.ShowToast(_selectedType, _messageInput, _codeInput);
                    SporiumLogger.LogInfo(LogCategory.UI, $"Toast triggered: {_selectedType} - {_messageInput} ({_codeInput})");
                }
                else
                {
                    SporiumLogger.LogWarning(LogCategory.UI, "ToastNotificationManager non disponibile!");
                }
            }
            
            GUILayout.EndVertical();
        }
        
        private void DrawQuickActionsSection(GUIStyle labelStyle, GUIStyle buttonStyle, GUIStyle headerStyle)
        {
            bool expanded = GetSectionExpanded("QuickActions");
            expanded = DrawSectionHeader("2. Quick Actions", expanded, headerStyle);
            SetSectionExpanded("QuickActions", expanded);
            
            if (!expanded) return;
            
            GUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Success", buttonStyle))
                _manager?.ShowSuccess("Test success message", "TEST-SUCCESS");
            if (GUILayout.Button("Error", buttonStyle))
                _manager?.ShowError("Test error message", "TEST-ERROR");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Warning", buttonStyle))
                _manager?.ShowWarning("Test warning message", "TEST-WARNING");
            if (GUILayout.Button("Info", buttonStyle))
                _manager?.ShowInfo("Test info message", "TEST-INFO");
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
        }
        
        private void DrawHistorySection(GUIStyle labelStyle, GUIStyle buttonStyle, GUIStyle headerStyle)
        {
            bool expanded = GetSectionExpanded("History");
            expanded = DrawSectionHeader("3. History Viewer", expanded, headerStyle);
            SetSectionExpanded("History", expanded);
            
            if (!expanded) return;
            
            GUILayout.BeginVertical(GUI.skin.box);
            
            // Filtri
            GUILayout.BeginHorizontal();
            _useTypeFilter = GUILayout.Toggle(_useTypeFilter, "Filter Type", labelStyle);
            if (_useTypeFilter)
            {
                string[] typeNames = System.Enum.GetNames(typeof(ToastNotificationType));
                int currentIndex = System.Array.IndexOf(typeNames, _filterType.ToString());
                int newIndex = GUILayout.SelectionGrid(currentIndex, typeNames, 4, GUILayout.Width(400));
                if (newIndex != currentIndex && newIndex >= 0 && newIndex < typeNames.Length)
                {
                    _filterType = (ToastNotificationType)System.Enum.Parse(typeof(ToastNotificationType), typeNames[newIndex]);
                }
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            _useCodeFilter = GUILayout.Toggle(_useCodeFilter, "Filter Code:", labelStyle);
            if (_useCodeFilter)
            {
                _filterCode = GUILayout.TextField(_filterCode, GUILayout.Width(200));
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Lista history
            if (_history != null)
            {
                List<ToastNotificationHistory.HistoryEntry> entries;
                
                if (_useTypeFilter && _useCodeFilter)
                {
                    entries = _history.GetHistoryByType(_filterType)
                        .Where(e => !string.IsNullOrEmpty(e.Code) && e.Code.StartsWith(_filterCode))
                        .ToList();
                }
                else if (_useTypeFilter)
                {
                    entries = _history.GetHistoryByType(_filterType);
                }
                else if (_useCodeFilter)
                {
                    entries = _history.GetHistoryByCode(_filterCode);
                }
                else
                {
                    entries = _history.GetHistory(50);
                }
                
                _historyScrollPosition = GUILayout.BeginScrollView(_historyScrollPosition, GUILayout.Height(200));
                
                foreach (var entry in entries.OrderByDescending(e => e.Timestamp))
                {
                    GUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.Label($"[{entry.Id}] {entry.Code}", labelStyle, GUILayout.Width(120));
                    GUILayout.Label($"{entry.Type}", labelStyle, GUILayout.Width(100));
                    GUILayout.Label($"{entry.Message}", labelStyle, GUILayout.Width(200));
                    GUILayout.Label($"{entry.Timestamp:HH:mm:ss}", labelStyle, GUILayout.Width(80));
                    GUILayout.EndHorizontal();
                }
                
                GUILayout.EndScrollView();
                
                GUILayout.Label($"Total entries: {_history.Count}", labelStyle);
            }
            else
            {
                GUILayout.Label("History non disponibile", labelStyle);
            }
            
            GUILayout.EndVertical();
        }
        
        private void DrawStatisticsSection(GUIStyle labelStyle, GUIStyle buttonStyle, GUIStyle headerStyle)
        {
            bool expanded = GetSectionExpanded("Statistics");
            expanded = DrawSectionHeader("4. Statistics", expanded, headerStyle);
            SetSectionExpanded("Statistics", expanded);
            
            if (!expanded) return;
            
            GUILayout.BeginVertical(GUI.skin.box);
            
            if (_history != null && _history.Count > 0)
            {
                var allEntries = _history.GetAllEntries();
                
                // Contatori per tipo
                GUILayout.Label("Count by Type:", labelStyle);
                var typeGroups = allEntries.GroupBy(e => e.Type);
                foreach (var group in typeGroups.OrderByDescending(g => g.Count()))
                {
                    GUILayout.Label($"  {group.Key}: {group.Count()}", labelStyle);
                }
                
                GUILayout.Space(5);
                
                // Toast più frequenti
                GUILayout.Label("Most Frequent Codes:", labelStyle);
                var codeGroups = allEntries
                    .Where(e => !string.IsNullOrEmpty(e.Code))
                    .GroupBy(e => e.Code)
                    .OrderByDescending(g => g.Count())
                    .Take(5);
                
                foreach (var group in codeGroups)
                {
                    GUILayout.Label($"  {group.Key}: {group.Count()}x", labelStyle);
                }
                
                // Ultimo toast
                var lastEntry = allEntries.OrderByDescending(e => e.Timestamp).FirstOrDefault();
                if (lastEntry != null)
                {
                    GUILayout.Space(5);
                    GUILayout.Label($"Last Toast: {lastEntry.Code} - {lastEntry.Message}", labelStyle);
                }
            }
            else
            {
                GUILayout.Label("Nessuna statistica disponibile", labelStyle);
            }
            
            GUILayout.EndVertical();
        }
        
        private void DrawSettingsSection(GUIStyle labelStyle, GUIStyle buttonStyle, GUIStyle headerStyle)
        {
            bool expanded = GetSectionExpanded("Settings");
            expanded = DrawSectionHeader("5. Settings", expanded, headerStyle);
            SetSectionExpanded("Settings", expanded);
            
            if (!expanded) return;
            
            GUILayout.BeginVertical(GUI.skin.box);
            
            if (GUILayout.Button("Clear History", buttonStyle))
            {
                _history?.Clear();
                SporiumLogger.LogInfo(LogCategory.UI, "History cleared");
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Export History (JSON)", buttonStyle))
            {
                ExportHistoryJSON();
            }
            
            if (GUILayout.Button("Export History (CSV)", buttonStyle))
            {
                ExportHistoryCSV();
            }
            
            GUILayout.EndVertical();
        }
        
        private void ExportHistoryJSON()
        {
            if (_history == null) return;
            
            var entries = _history.GetAllEntries();
            var json = JsonUtility.ToJson(entries, true);
            
            string path = System.IO.Path.Combine(Application.persistentDataPath, $"toast_history_{System.DateTime.Now:yyyyMMdd_HHmmss}.json");
            System.IO.File.WriteAllText(path, json);
            
            SporiumLogger.LogInfo(LogCategory.UI, $"History exported to: {path}");
        }
        
        private void ExportHistoryCSV()
        {
            if (_history == null) return;
            
            var entries = _history.GetAllEntries();
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Id,Code,Type,Message,Timestamp,Source");
            
            foreach (var entry in entries)
            {
                csv.AppendLine($"{entry.Id},{entry.Code},{entry.Type},{entry.Message.Replace(",", ";")},{entry.Timestamp:yyyy-MM-dd HH:mm:ss},{entry.Source}");
            }
            
            string path = System.IO.Path.Combine(Application.persistentDataPath, $"toast_history_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");
            System.IO.File.WriteAllText(path, csv.ToString());
            
            SporiumLogger.LogInfo(LogCategory.UI, $"History exported to: {path}");
        }
        
        private bool GetSectionExpanded(string section)
        {
            if (!_sectionExpanded.ContainsKey(section))
                _sectionExpanded[section] = true;
            return _sectionExpanded[section];
        }
        
        private void SetSectionExpanded(string section, bool expanded)
        {
            _sectionExpanded[section] = expanded;
        }
        
        private bool DrawSectionHeader(string title, bool expanded, GUIStyle headerStyle)
        {
            GUILayout.BeginHorizontal();
            string arrow = expanded ? "▼" : "▶";
            if (GUILayout.Button($"{arrow} {title}", headerStyle, GUILayout.ExpandWidth(true)))
            {
                expanded = !expanded;
            }
            GUILayout.EndHorizontal();
            return expanded;
        }
        
        private void DrawHUDNotifications2_0RuntimeEditor(GUIStyle labelStyle, GUIStyle buttonStyle, GUIStyle headerStyle)
        {
            bool expanded = GetSectionExpanded("HUD2.0Editor");
            expanded = DrawSectionHeader("6. HUD Notifications 2.0 Runtime Editor", expanded, headerStyle);
            SetSectionExpanded("HUD2.0Editor", expanded);
            
            if (!expanded) return;
            
            GUILayout.BeginVertical(GUI.skin.box);
            
            if (_config2_0 == null)
            {
                GUILayout.Label("HUDNotificationConfig2.0 non disponibile. Assicurati che il sistema 2.0 sia inizializzato.", labelStyle);
                GUILayout.EndVertical();
                return;
            }
            
            // Container Settings
            GUILayout.Label("Container Settings", headerStyle);
            _config2_0.ContainerWidth = EditorFloatField("Width (px)", _config2_0.ContainerWidth, labelStyle);
            _config2_0.ContainerTopOffset = EditorFloatField("Top Offset (px)", _config2_0.ContainerTopOffset, labelStyle);
            _config2_0.ContainerRightOffset = EditorFloatField("Right Offset (px)", _config2_0.ContainerRightOffset, labelStyle);
            
            GUILayout.Space(5);
            
            // Header Settings
            GUILayout.Label("Header Settings", headerStyle);
            _config2_0.HeaderPadding = EditorFloatField("Padding (px)", _config2_0.HeaderPadding, labelStyle);
            _config2_0.HeaderBorderWidth = EditorFloatField("Border Width (px)", _config2_0.HeaderBorderWidth, labelStyle);
            _config2_0.HeaderMarginBottom = EditorFloatField("Margin Bottom (px)", _config2_0.HeaderMarginBottom, labelStyle);
            _config2_0.HeaderFontSize = EditorFloatField("Font Size (px)", _config2_0.HeaderFontSize, labelStyle);
            _config2_0.HeaderIconSize = EditorFloatField("Icon Size (px)", _config2_0.HeaderIconSize, labelStyle);
            _config2_0.HeaderChevronSize = EditorFloatField("Chevron Size (px)", _config2_0.HeaderChevronSize, labelStyle);
            
            GUILayout.Space(5);
            
            // Toast Settings
            GUILayout.Label("Toast Settings", headerStyle);
            _config2_0.ToastPadding = EditorFloatField("Padding (px)", _config2_0.ToastPadding, labelStyle);
            _config2_0.ToastBorderWidth = EditorFloatField("Border Width (px)", _config2_0.ToastBorderWidth, labelStyle);
            _config2_0.ToastGap = EditorFloatField("Gap (px)", _config2_0.ToastGap, labelStyle);
            _config2_0.ToastIconSize = EditorFloatField("Icon Size (px)", _config2_0.ToastIconSize, labelStyle);
            _config2_0.ToastCodeFontSize = EditorFloatField("Code Font Size (px)", _config2_0.ToastCodeFontSize, labelStyle);
            _config2_0.ToastMessageFontSize = EditorFloatField("Message Font Size (px)", _config2_0.ToastMessageFontSize, labelStyle);
            
            GUILayout.Space(5);
            
            // Item Notification Settings
            GUILayout.Label("Item Notification Settings", headerStyle);
            _config2_0.ItemIconSize = EditorFloatField("Item Icon Size (px)", _config2_0.ItemIconSize, labelStyle);
            _config2_0.ItemIconGap = EditorFloatField("Item Icon Gap (px)", _config2_0.ItemIconGap, labelStyle);
            _config2_0.ItemHeaderFontSize = EditorFloatField("Item Header Font Size (px)", _config2_0.ItemHeaderFontSize, labelStyle);
            _config2_0.ItemNameFontSize = EditorFloatField("Item Name Font Size (px)", _config2_0.ItemNameFontSize, labelStyle);
            _config2_0.ItemLocationFontSize = EditorFloatField("Item Location Font Size (px)", _config2_0.ItemLocationFontSize, labelStyle);
            
            GUILayout.Space(5);
            
            // Timing Settings
            GUILayout.Label("Timing Settings", headerStyle);
            _config2_0.AutoDismissDuration = EditorFloatField("Auto Dismiss Duration (s)", _config2_0.AutoDismissDuration, labelStyle);
            _config2_0.OverflowDismissDuration = EditorFloatField("Overflow Dismiss Duration (s)", _config2_0.OverflowDismissDuration, labelStyle);
            _config2_0.MaxVisibleNotifications = EditorIntField("Max Visible Notifications", _config2_0.MaxVisibleNotifications, labelStyle);
            
            GUILayout.Space(10);
            
            // Pulsanti
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to Defaults", buttonStyle))
            {
                ResetToDefaults();
            }
            if (GUILayout.Button("Apply Changes", buttonStyle))
            {
                ApplyChanges();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
        }
        
        private float EditorFloatField(string label, float value, GUIStyle labelStyle)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle, GUILayout.Width(200));
            string str = GUILayout.TextField(value.ToString("F1"), GUILayout.Width(100));
            float result = value;
            if (float.TryParse(str, out float parsed))
                result = parsed;
            GUILayout.EndHorizontal();
            return result;
        }
        
        private int EditorIntField(string label, int value, GUIStyle labelStyle)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle, GUILayout.Width(200));
            string str = GUILayout.TextField(value.ToString(), GUILayout.Width(100));
            int result = value;
            if (int.TryParse(str, out int parsed))
                result = parsed;
            GUILayout.EndHorizontal();
            return result;
        }
        
        private void ResetToDefaults()
        {
            if (_config2_0 == null) return;
            
            // Reset ai valori di default
            _config2_0.ContainerWidth = 306f;
            _config2_0.ContainerTopOffset = 96f;
            _config2_0.ContainerRightOffset = 24f;
            _config2_0.HeaderPadding = 8f;
            _config2_0.HeaderBorderWidth = 2f;
            _config2_0.HeaderMarginBottom = 6f;
            _config2_0.HeaderFontSize = 10f;
            _config2_0.HeaderIconSize = 14f;
            _config2_0.HeaderChevronSize = 16f;
            _config2_0.ToastPadding = 8f;
            _config2_0.ToastBorderWidth = 2f;
            _config2_0.ToastGap = 6f;
            _config2_0.ToastIconSize = 14f;
            _config2_0.ToastCodeFontSize = 10f;
            _config2_0.ToastMessageFontSize = 11f;
            _config2_0.ItemIconSize = 40f;
            _config2_0.ItemIconGap = 8f;
            _config2_0.ItemHeaderFontSize = 10f;
            _config2_0.ItemNameFontSize = 11f;
            _config2_0.ItemLocationFontSize = 9f;
            _config2_0.AutoDismissDuration = 8f;
            _config2_0.OverflowDismissDuration = 5f;
            _config2_0.MaxVisibleNotifications = 3;
            
            SporiumLogger.LogInfo(LogCategory.UI, "HUD Notifications 2.0 config reset to defaults");
        }
        
        private void ApplyChanges()
        {
            if (_manager2_0 == null) return;
            
            // Notifica al manager di ricaricare layout
            _manager2_0.RefreshLayout();
            SporiumLogger.LogInfo(LogCategory.UI, "HUD Notifications 2.0 layout refreshed");
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

