using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Logger NDJSON temporaneo per la sessione di debug d2269f (ascensore).
/// Scrive su debug-d2269f.log nella root del progetto. Rimuovere a fine debug.
/// </summary>
public static class DebugSessionLog_d2269f
{
    private static readonly string LogPath =
        Path.Combine(Application.dataPath, "..", "debug-d2269f.log");

    public static void Write(string hypothesisId, string location, string message, string dataJson)
    {
        try
        {
            long ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            var sb = new StringBuilder(256);
            sb.Append("{\"sessionId\":\"d2269f\",\"hypothesisId\":\"").Append(hypothesisId)
              .Append("\",\"location\":\"").Append(location)
              .Append("\",\"message\":\"").Append(message)
              .Append("\",\"data\":").Append(string.IsNullOrEmpty(dataJson) ? "{}" : dataJson)
              .Append(",\"timestamp\":").Append(ts).Append("}");
            File.AppendAllText(LogPath, sb.ToString() + "\n");
        }
        catch { }
    }
}
