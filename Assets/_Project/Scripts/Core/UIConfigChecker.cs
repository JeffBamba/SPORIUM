using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sporae.DevTools;

/// <summary>
/// Verifica e ripara la configurazione UI per prevenire click-through.
/// </summary>
public class UIConfigChecker : MonoBehaviour
{
    [Header("UI Configuration")]
    [SerializeField] private bool checkOnStart = true;
    [SerializeField] private bool autoFix = true;
    [SerializeField] private bool showDebugLogs = true;
    
    void Start()
    {
        if (checkOnStart)
        {
            CheckUIConfiguration();
        }
    }
    
    [ContextMenu("Check UI Configuration")]
    public void CheckUIConfiguration()
    {
        if (showDebugLogs)
            SporiumLogger.LogInfo(LogCategory.UI, "Iniziando verifica configurazione UI...");
        
        bool allGood = true;
        
        // 1. Verifica EventSystem
        allGood &= CheckEventSystem();
        
        // 2. Verifica Canvas e GraphicRaycaster
        allGood &= CheckCanvasConfiguration();
        
        // 3. Verifica Pulsanti UI
        allGood &= CheckUIButtons();
        
        // 4. Verifica Layer UI
        allGood &= CheckUILayers();
        
        if (allGood)
        {
            if (showDebugLogs)
                SporiumLogger.LogInfo(LogCategory.UI, "Configurazione UI corretta!");
        }
        else
        {
            SporiumLogger.LogWarning(LogCategory.UI, "Problemi nella configurazione UI rilevati!");
        }
    }
    
    private bool CheckEventSystem()
    {
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            SporiumLogger.LogError(LogCategory.UI, "EventSystem non trovato!");
            
            if (autoFix)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystem = eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
                SporiumLogger.LogInfo(LogCategory.UI, "EventSystem creato automaticamente");
            }
            
            return false;
        }
        
        StandaloneInputModule inputModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (inputModule == null)
        {
            SporiumLogger.LogWarning(LogCategory.UI, "StandaloneInputModule mancante su EventSystem");
            
            if (autoFix)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
                SporiumLogger.LogInfo(LogCategory.UI, "StandaloneInputModule aggiunto");
            }
        }
        
        if (showDebugLogs)
            SporiumLogger.LogInfo(LogCategory.UI, $"EventSystem trovato: {eventSystem.name}");
        
        return true;
    }
    
    private bool CheckCanvasConfiguration()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        if (canvases.Length == 0)
        {
            SporiumLogger.LogError(LogCategory.UI, "Nessun Canvas trovato!");
            return false;
        }
        
        bool allGood = true;
        
        foreach (Canvas canvas in canvases)
        {
            // Verifica GraphicRaycaster
            GraphicRaycaster graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"GraphicRaycaster mancante su Canvas: {canvas.name}");
                
                if (autoFix)
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                    SporiumLogger.LogInfo(LogCategory.UI, $"GraphicRaycaster aggiunto a {canvas.name}");
                }
                else
                {
                    allGood = false;
                }
            }
            
            // Verifica CanvasScaler
            CanvasScaler canvasScaler = canvas.GetComponent<CanvasScaler>();
            if (canvasScaler == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"CanvasScaler mancante su Canvas: {canvas.name}");
                
                if (autoFix)
                {
                    CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    SporiumLogger.LogInfo(LogCategory.UI, $"CanvasScaler aggiunto a {canvas.name}");
                }
            }
            
            if (showDebugLogs)
            {
                string raycastStatus = graphicRaycaster != null ? "✅" : "❌";
                string scalerStatus = canvasScaler != null ? "✅" : "❌";
                SporiumLogger.LogDebug(LogCategory.UI, $"Canvas: {canvas.name} - Raycaster: {raycastStatus} Scaler: {scalerStatus}");
            }
        }
        
        return allGood;
    }
    
    private bool CheckUIButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>();
        if (buttons.Length == 0)
        {
            SporiumLogger.LogWarning(LogCategory.UI, "Nessun pulsante UI trovato");
            return true;
        }
        
        bool allGood = true;
        
        foreach (Button button in buttons)
        {
            // Verifica Image con raycastTarget
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null && !buttonImage.raycastTarget)
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"Button {button.name}: raycastTarget = false");
                
                if (autoFix)
                {
                    buttonImage.raycastTarget = true;
                    SporiumLogger.LogInfo(LogCategory.UI, $"raycastTarget abilitato per {button.name}");
                }
                else
                {
                    allGood = false;
                }
            }
            
            // Verifica che sia interactable
            if (!button.interactable)
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"Button {button.name}: interactable = false");
            }
            
            // Verifica OnClick events
            if (button.onClick.GetPersistentEventCount() == 0)
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"Button {button.name}: nessun OnClick event");
            }
        }
        
        if (showDebugLogs)
            SporiumLogger.LogInfo(LogCategory.UI, $"Verificati {buttons.Length} pulsanti UI");
        
        return allGood;
    }
    
    private bool CheckUILayers()
    {
        // Verifica che i Canvas siano su layer UI
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        bool allGood = true;
        
        foreach (Canvas canvas in canvases)
        {
            if (canvas.gameObject.layer != LayerMask.NameToLayer("UI"))
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"Canvas {canvas.name} non su layer UI (attuale: {LayerMask.LayerToName(canvas.gameObject.layer)})");
                
                if (autoFix)
                {
                    canvas.gameObject.layer = LayerMask.NameToLayer("UI");
                    SporiumLogger.LogInfo(LogCategory.UI, $"Layer UI assegnato a {canvas.name}");
                }
                else
                {
                    allGood = false;
                }
            }
        }
        
        return allGood;
    }
    
    /// <summary>
    /// Test rapido per verificare che UIBlocker funzioni
    /// </summary>
    [ContextMenu("Test UIBlocker")]
    public void TestUIBlocker()
    {
        SporiumLogger.LogDebug(LogCategory.UI, "=== TEST UIBLOCKER ===");
        
        bool isOverUI = UIBlocker.IsPointerOverUI();
        SporiumLogger.LogDebug(LogCategory.UI, $"IsPointerOverUI: {isOverUI}");
        
        if (isOverUI)
        {
            UIBlocker.DebugPointerOverUI();
        }
        
        SporiumLogger.LogDebug(LogCategory.UI, "=== FINE TEST ===");
    }
    
    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Disegna info sui Canvas in Editor
        if (showDebugLogs)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                string layerName = LayerMask.LayerToName(canvas.gameObject.layer);
                bool hasRaycaster = canvas.GetComponent<GraphicRaycaster>() != null;
                string status = hasRaycaster ? "✅" : "❌";
                
                UnityEditor.Handles.Label(canvas.transform.position + Vector3.up * 0.5f, 
                    $"Canvas: {canvas.name}\nLayer: {layerName}\nRaycaster: {status}");
            }
        }
    }
    #endif
}
