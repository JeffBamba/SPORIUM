namespace Sporae.DevTools
{
    /// <summary>
    /// Livelli di log disponibili per SporiumLogger
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,      // Informazioni di debug dettagliate
        Info = 1,     // Informazioni generali
        Warning = 2,  // Avvisi (non critici)
        Error = 3,    // Errori
        Critical = 4  // Errori critici che bloccano il sistema
    }
}

