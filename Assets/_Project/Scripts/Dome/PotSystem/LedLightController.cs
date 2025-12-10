using UnityEngine;
using UnityEngine.Rendering.Universal;
using Sporae.Dome.PotSystem.Growth;

/// <summary>
/// BLK-02.07: Controller per gestire le luci Unity associate ai LED dei vasi
/// </summary>
public class LedLightController : MonoBehaviour
{
    [Header("LED Lights")]
    [Tooltip("Luce Unity per LED Blu (Light2D)")]
    [SerializeField] private Light2D _blueLight;
    
    [Tooltip("Luce Unity per LED Rosso (Light2D)")]
    [SerializeField] private Light2D _redLight;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    private void Awake()
    {
        // Cerca le luci se non assegnate
        if (_blueLight == null)
        {
            _blueLight = transform.Find("BlueLight")?.GetComponent<Light2D>();
            if (_blueLight == null && showDebugLogs)
                Debug.LogWarning($"[LedLightController] {gameObject.name}: BlueLight non trovata. Assegna manualmente nella scena Unity.");
        }
        
        if (_redLight == null)
        {
            _redLight = transform.Find("RedLight")?.GetComponent<Light2D>();
            if (_redLight == null && showDebugLogs)
                Debug.LogWarning($"[LedLightController] {gameObject.name}: RedLight non trovata. Assegna manualmente nella scena Unity.");
        }
        
        // Inizializza: tutte le luci spente
        SetBlueLight(false);
        SetRedLight(false);
    }
    
    /// <summary>
    /// Aggiorna le luci in base allo stato del LED
    /// </summary>
    public void UpdateLights(LedSystemState ledState)
    {
        switch (ledState)
        {
            case LedSystemState.Off:
                SetBlueLight(false);
                SetRedLight(false);
                break;
            case LedSystemState.Blue:
                SetBlueLight(true);
                SetRedLight(false);
                break;
            case LedSystemState.Red:
                SetBlueLight(false);
                SetRedLight(true);
                break;
        }
    }
    
    /// <summary>
    /// Attiva/disattiva la luce blu
    /// </summary>
    public void SetBlueLight(bool enabled)
    {
        if (_blueLight != null)
        {
            _blueLight.enabled = enabled;
            if (showDebugLogs)
                Debug.Log($"[LedLightController] {gameObject.name}: BlueLight {(enabled ? "accesa" : "spenta")}");
        }
    }
    
    /// <summary>
    /// Attiva/disattiva la luce rossa
    /// </summary>
    public void SetRedLight(bool enabled)
    {
        if (_redLight != null)
        {
            _redLight.enabled = enabled;
            if (showDebugLogs)
                Debug.Log($"[LedLightController] {gameObject.name}: RedLight {(enabled ? "accesa" : "spenta")}");
        }
    }
    
    /// <summary>
    /// Verifica se la luce blu è accesa
    /// </summary>
    public bool IsBlueLightOn() => _blueLight != null && _blueLight.enabled;
    
    /// <summary>
    /// Verifica se la luce rossa è accesa
    /// </summary>
    public bool IsRedLightOn() => _redLight != null && _redLight.enabled;
}

