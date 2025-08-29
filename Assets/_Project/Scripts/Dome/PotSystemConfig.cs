using UnityEngine;

/// <summary>
/// Configurazione globale per il sistema dei vasi.
/// Contiene costanti e impostazioni condivise tra tutti i componenti del sistema.
/// </summary>
public static class PotSystemConfig
{
    [Header("Pot Configuration")]
    public const string POT_ID_PREFIX = "POT-";
    public const int MAX_POTS_PER_ROOM = 10;
    
    [Header("Interaction Settings")]
    public const float DEFAULT_INTERACT_DISTANCE = 1.5f;
    public const float MIN_INTERACT_DISTANCE = 0.5f;
    public const float MAX_INTERACT_DISTANCE = 5.0f;
    
    [Header("Visual Settings")]
    public static readonly Color DEFAULT_HIGHLIGHT_COLOR = Color.yellow;
    public static readonly Color DEFAULT_BASE_COLOR = Color.white;
    public static readonly Color DEFAULT_POT_COLOR = new Color(0.6f, 0.4f, 0.2f); // Marrone
    
    [Header("UI Settings")]
    public const float DEFAULT_WIDGET_WIDTH = 300f;
    public const float DEFAULT_WIDGET_HEIGHT = 80f;
    public static readonly Vector2 DEFAULT_WIDGET_POSITION = new Vector2(12f, 12f);
    public static readonly Color DEFAULT_WIDGET_BACKGROUND = new Color(0f, 0f, 0f, 0.8f);
    public static readonly Color DEFAULT_WIDGET_TEXT = Color.white;
    
    [Header("Layer Settings")]
    public const int DEFAULT_POT_LAYER = 0; // Default layer
    public const string POT_LAYER_NAME = "Default";
    
    [Header("Debug Settings")]
    public const bool DEFAULT_SHOW_DEBUG_LOGS = true;
    public const bool DEFAULT_DRAW_GIZMOS = true;
    
    /// <summary>
    /// Genera un ID univoco per un vaso
    /// </summary>
    /// <param name="potNumber">Numero progressivo del vaso</param>
    /// <returns>ID nel formato POT-XXX</returns>
    public static string GeneratePotId(int potNumber)
    {
        return $"{POT_ID_PREFIX}{potNumber:D3}";
    }
    
    /// <summary>
    /// Valida la distanza di interazione
    /// </summary>
    /// <param name="distance">Distanza da validare</param>
    /// <returns>Distanza validata entro i limiti</returns>
    public static float ValidateInteractDistance(float distance)
    {
        return Mathf.Clamp(distance, MIN_INTERACT_DISTANCE, MAX_INTERACT_DISTANCE);
    }
    
    /// <summary>
    /// Valida il numero di vasi per stanza
    /// </summary>
    /// <param name="potCount">Numero di vasi</param>
    /// <returns>Numero validato</returns>
    public static int ValidatePotCount(int potCount)
    {
        return Mathf.Clamp(potCount, 1, MAX_POTS_PER_ROOM);
    }
    
    /// <summary>
    /// Restituisce il colore per un determinato stato del vaso
    /// </summary>
    /// <param name="state">Stato del vaso</param>
    /// <returns>Colore appropriato per lo stato</returns>
    public static Color GetStateColor(PotState state)
    {
        switch (state)
        {
            case PotState.Empty:
                return Color.gray;
            case PotState.Occupied:
                return Color.blue;
            case PotState.Growing:
                return Color.green;
            case PotState.Mature:
                return Color.yellow;
            default:
                return Color.white;
        }
    }
    
    /// <summary>
    /// Restituisce il nome localizzato per uno stato
    /// </summary>
    /// <param name="state">Stato del vaso</param>
    /// <returns>Nome localizzato</returns>
    public static string GetStateName(PotState state)
    {
        switch (state)
        {
            case PotState.Empty:
                return "Vuoto";
            case PotState.Occupied:
                return "Occupato";
            case PotState.Growing:
                return "In crescita";
            case PotState.Mature:
                return "Maturo";
            default:
                return "Sconosciuto";
        }
    }
    
    /// <summary>
    /// Restituisce la descrizione per uno stato
    /// </summary>
    /// <param name="state">Stato del vaso</param>
    /// <returns>Descrizione dello stato</returns>
    public static string GetStateDescription(PotState state)
    {
        switch (state)
        {
            case PotState.Empty:
                return "Vaso vuoto, pronto per piantare";
            case PotState.Occupied:
                return "Vaso occupato da una pianta";
            case PotState.Growing:
                return "Pianta in fase di crescita";
            case PotState.Mature:
                return "Pianta completamente matura";
            default:
                return "Stato non riconosciuto";
        }
    }
    
    /// <summary>
    /// Restituisce le impostazioni predefinite per un nuovo vaso
    /// </summary>
    /// <returns>Configurazione predefinita</returns>
    public static PotDefaultSettings GetDefaultSettings()
    {
        return new PotDefaultSettings
        {
            interactDistance = DEFAULT_INTERACT_DISTANCE,
            highlightColor = DEFAULT_HIGHLIGHT_COLOR,
            baseColor = DEFAULT_BASE_COLOR,
            potColor = DEFAULT_POT_COLOR,
            showDebugLogs = DEFAULT_SHOW_DEBUG_LOGS,
            drawGizmos = DEFAULT_DRAW_GIZMOS
        };
    }
}

/// <summary>
/// Impostazioni predefinite per un vaso
/// </summary>
[System.Serializable]
public struct PotDefaultSettings
{
    public float interactDistance;
    public Color highlightColor;
    public Color baseColor;
    public Color potColor;
    public bool showDebugLogs;
    public bool drawGizmos;
}
