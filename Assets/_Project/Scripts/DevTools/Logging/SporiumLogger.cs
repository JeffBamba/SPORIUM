using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Sporae.DevTools
{
    /// <summary>
    /// Sistema di logging centralizzato con livelli e categorie
    /// Sostituisce Debug.Log sparsi con sistema controllato
    /// </summary>
    public static class SporiumLogger
    {
        /// <summary>
        /// Entry di log con tutte le informazioni
        /// </summary>
        [Serializable]
        public class LogEntry
        {
            public LogLevel Level;
            public LogCategory Category;
            public string Message;
            public object Data;
            public DateTime Timestamp;
            public string StackTrace;
            
            public LogEntry(LogLevel level, LogCategory category, string message, object data = null)
            {
                Level = level;
                Category = category;
                Message = message;
                Data = data;
                Timestamp = DateTime.Now;
                StackTrace = System.Environment.StackTrace;
            }
        }
        
        private static Dictionary<LogCategory, bool> _categoryEnabled = new Dictionary<LogCategory, bool>();
        private static LogLevel _minLogLevel = LogLevel.Debug;
        private static List<LogEntry> _logHistory = new List<LogEntry>();
        private const int MAX_HISTORY = 1000;
        
        static SporiumLogger()
        {
            // Inizializza tutte le categorie come abilitate
            foreach (LogCategory category in Enum.GetValues(typeof(LogCategory)))
            {
                if (category != LogCategory.All)
                {
                    _categoryEnabled[category] = true;
                }
            }
        }
        
        /// <summary>
        /// Log principale con livello, categoria e messaggio
        /// </summary>
        public static void Log(LogLevel level, LogCategory category, string message, object data = null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Verifica filtro livello
            if (level < _minLogLevel)
                return;
            
            // Verifica filtro categoria
            if (category != LogCategory.All && !_categoryEnabled[category])
                return;
            
            // Crea entry
            var entry = new LogEntry(level, category, message, data);
            
            // Aggiungi a history
            _logHistory.Add(entry);
            if (_logHistory.Count > MAX_HISTORY)
            {
                _logHistory.RemoveAt(0); // FIFO
            }
            
            // Formato messaggio per Unity Console
            string formattedMessage = FormatMessage(level, category, message);
            
            // Log in Unity Console con colore appropriato
            Color logColor = GetLogColor(level);
            string colorTag = ColorUtility.ToHtmlStringRGB(logColor);
            string coloredMessage = $"<color=#{colorTag}>{formattedMessage}</color>";
            
            // Usa il metodo Unity appropriato per il livello
            switch (level)
            {
                case LogLevel.Debug:
                    Debug.Log(coloredMessage);
                    break;
                case LogLevel.Info:
                    Debug.Log(coloredMessage);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(coloredMessage);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    Debug.LogError(coloredMessage);
                    break;
            }
#endif
        }
        
        /// <summary>
        /// Formatta il messaggio con categoria e livello
        /// </summary>
        private static string FormatMessage(LogLevel level, LogCategory category, string message)
        {
            return $"[{category}] [{level}] {message}";
        }
        
        /// <summary>
        /// Ottiene il colore per il livello di log
        /// </summary>
        private static Color GetLogColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return Color.gray;
                case LogLevel.Info:
                    return Color.white;
                case LogLevel.Warning:
                    return Color.yellow;
                case LogLevel.Error:
                    return Color.red;
                case LogLevel.Critical:
                    return Color.magenta;
                default:
                    return Color.white;
            }
        }
        
        /// <summary>
        /// Abilita/disabilita una categoria di log
        /// </summary>
        public static void SetCategoryEnabled(LogCategory category, bool enabled)
        {
            if (category == LogCategory.All)
            {
                // Abilita/disabilita tutte le categorie
                foreach (LogCategory cat in Enum.GetValues(typeof(LogCategory)))
                {
                    if (cat != LogCategory.All)
                    {
                        _categoryEnabled[cat] = enabled;
                    }
                }
            }
            else
            {
                _categoryEnabled[category] = enabled;
            }
        }
        
        /// <summary>
        /// Verifica se una categoria è abilitata
        /// </summary>
        public static bool IsCategoryEnabled(LogCategory category)
        {
            if (category == LogCategory.All)
                return true;
            
            return _categoryEnabled.ContainsKey(category) && _categoryEnabled[category];
        }
        
        /// <summary>
        /// Imposta il livello minimo di log (solo log >= minLevel vengono mostrati)
        /// </summary>
        public static void SetMinLogLevel(LogLevel minLevel)
        {
            _minLogLevel = minLevel;
        }
        
        /// <summary>
        /// Ottiene il livello minimo corrente
        /// </summary>
        public static LogLevel GetMinLogLevel()
        {
            return _minLogLevel;
        }
        
        /// <summary>
        /// Esporta i log su file JSON
        /// </summary>
        public static void ExportLogsToFile(string filePath, bool includeStackTrace = false)
        {
            try
            {
                var exportData = new
                {
                    ExportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    TotalLogs = _logHistory.Count,
                    MinLogLevel = _minLogLevel.ToString(),
                    CategoryStates = _categoryEnabled,
                    Logs = _logHistory.Select(entry => new
                    {
                        Timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        Level = entry.Level.ToString(),
                        Category = entry.Category.ToString(),
                        Message = entry.Message,
                        Data = entry.Data,
                        StackTrace = includeStackTrace ? entry.StackTrace : null
                    }).ToArray()
                };
                
                string json = JsonUtility.ToJson(exportData, true);
                File.WriteAllText(filePath, json);
                
                Debug.Log($"[SporiumLogger] Log esportati in {filePath} ({_logHistory.Count} entry)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SporiumLogger] Errore export log: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Esporta i log su file CSV
        /// </summary>
        public static void ExportLogsToCSV(string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    // Header
                    writer.WriteLine("Timestamp,Level,Category,Message,Data");
                    
                    // Log entries
                    foreach (var entry in _logHistory)
                    {
                        string dataStr = entry.Data != null ? JsonUtility.ToJson(entry.Data) : "";
                        string message = entry.Message.Replace(",", ";").Replace("\n", " ").Replace("\r", "");
                        writer.WriteLine($"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{entry.Level},{entry.Category},\"{message}\",\"{dataStr}\"");
                    }
                }
                
                Debug.Log($"[SporiumLogger] Log esportati in CSV {filePath} ({_logHistory.Count} entry)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SporiumLogger] Errore export CSV: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Pulisce la history dei log
        /// </summary>
        public static void ClearHistory()
        {
            _logHistory.Clear();
        }
        
        /// <summary>
        /// Ottiene la history dei log (read-only)
        /// </summary>
        public static IReadOnlyList<LogEntry> GetHistory()
        {
            return _logHistory.AsReadOnly();
        }
        
        /// <summary>
        /// Ottiene il numero di log nella history
        /// </summary>
        public static int GetHistoryCount()
        {
            return _logHistory.Count;
        }
        
        // ============================================
        // METODI HELPER PER LIVELLI
        // ============================================
        
        public static void LogDebug(LogCategory category, string message, object data = null)
        {
            Log(LogLevel.Debug, category, message, data);
        }
        
        public static void LogInfo(LogCategory category, string message, object data = null)
        {
            Log(LogLevel.Info, category, message, data);
        }
        
        public static void LogWarning(LogCategory category, string message, object data = null)
        {
            Log(LogLevel.Warning, category, message, data);
        }
        
        public static void LogError(LogCategory category, string message, object data = null)
        {
            Log(LogLevel.Error, category, message, data);
        }
        
        public static void LogCritical(LogCategory category, string message, object data = null)
        {
            Log(LogLevel.Critical, category, message, data);
        }
        
        // ============================================
        // METODI HELPER PER CATEGORIE
        // ============================================
        
        public static void LogUI(LogLevel level, string message, object data = null)
        {
            Log(level, LogCategory.UI, message, data);
        }
        
        public static void LogCore(LogLevel level, string message, object data = null)
        {
            Log(level, LogCategory.Core, message, data);
        }
        
        public static void LogDome(LogLevel level, string message, object data = null)
        {
            Log(level, LogCategory.Dome, message, data);
        }
        
        public static void LogPot(LogLevel level, string message, object data = null)
        {
            Log(level, LogCategory.Pot, message, data);
        }
        
        public static void LogPh(LogLevel level, string message, object data = null)
        {
            Log(level, LogCategory.Ph, message, data);
        }
        
        public static void LogInventory(LogLevel level, string message, object data = null)
        {
            Log(level, LogCategory.Inventory, message, data);
        }
        
        public static void LogSave(LogLevel level, string message, object data = null)
        {
            Log(level, LogCategory.Save, message, data);
        }
        
        public static void LogAudio(LogLevel level, string message, object data = null)
        {
            Log(level, LogCategory.Audio, message, data);
        }
    }
}

