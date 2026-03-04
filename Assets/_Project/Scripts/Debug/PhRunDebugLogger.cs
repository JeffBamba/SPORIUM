using System;
using System.IO;
using UnityEngine;

namespace _Project
{
    /// <summary>
    /// Scrive una riga NDJSON su debug-0a0515.log per run di verifica conteggio pH.
    /// Session ID 0a0515; path: workspace root / debug-0a0515.log
    /// </summary>
    public static class PhRunDebugLogger
    {
        private const string SessionId = "0a0515";
        private static string _logPath;

        private static string LogPath
        {
            get
            {
                if (_logPath != null) return _logPath;
                try
                {
                    string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
                    _logPath = Path.Combine(projectRoot, "debug-0a0515.log");
                }
                catch
                {
                    _logPath = Path.Combine(Application.persistentDataPath, "debug-0a0515.log");
                }
                return _logPath;
            }
        }

        /// <summary>Escape per JSON string (message).</summary>
        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        /// <summary>Scrive una riga NDJSON. dataJson = oggetto JSON già formattato, es. {"oldPh":12.5,"totalQueued":-4}</summary>
        public static void Log(string hypothesisId, string location, string message, string dataJson)
        {
            try
            {
                long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string line = "{\"sessionId\":\"" + SessionId + "\",\"hypothesisId\":\"" + Escape(hypothesisId) + "\",\"location\":\"" + Escape(location) + "\",\"message\":\"" + Escape(message) + "\",\"data\":" + (string.IsNullOrEmpty(dataJson) ? "{}" : dataJson) + ",\"timestamp\":" + ts + "}\n";
                File.AppendAllText(LogPath, line);
            }
            catch { /* no-op */ }
        }
    }
}
