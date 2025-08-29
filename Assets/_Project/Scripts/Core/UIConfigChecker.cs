using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
            Debug.Log("[UIConfigChecker] Iniziando verifica configurazione UI...");
        
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
                Debug.Log("[UIConfigChecker] ✅ Configurazione UI corretta!");
        }
        else
        {
            Debug.LogWarning("[UIConfigChecker] ⚠️ Problemi nella configurazione UI rilevati!");
        }
    }
    
    private bool CheckEventSystem()
    {
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("[UIConfigChecker] ❌ EventSystem non trovato!");
            
            if (autoFix)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystem = eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
                Debug.Log("[UIConfigChecker] ✅ EventSystem creato automaticamente");
            }
            
            return false;
        }
        
        StandaloneInputModule inputModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (inputModule == null)
        {
            Debug.LogWarning("[UIConfigChecker] ⚠️ StandaloneInputModule mancante su EventSystem");
            
            if (autoFix)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
                Debug.Log("[UIConfigChecker] ✅ StandaloneInputModule aggiunto");
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"[UIConfigChecker] ✅ EventSystem trovato: {eventSystem.name}");
        
        return true;
    }
    
    private bool CheckCanvasConfiguration()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        if (canvases.Length == 0)
        {
            Debug.LogError("[UIConfigChecker] ❌ Nessun Canvas trovato!");
            return false;
        }
        
        bool allGood = true;
        
        foreach (Canvas canvas in canvases)
        {
            // Verifica GraphicRaycaster
            GraphicRaycaster graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster == null)
            {
                Debug.LogWarning($"[UIConfigChecker] ⚠️ GraphicRaycaster mancante su Canvas: {canvas.name}");
                
                if (autoFix)
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log($"[UIConfigChecker] ✅ GraphicRaycaster aggiunto a {canvas.name}");
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
                Debug.LogWarning($"[UIConfigChecker] ⚠️ CanvasScaler mancante su Canvas: {canvas.name}");
                
                if (autoFix)
                {
                    CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    Debug.Log($"[UIConfigChecker] ✅ CanvasScaler aggiunto a {canvas.name}");
                }
            }
            
            if (showDebugLogs)
            {
                string raycastStatus = graphicRaycaster != null ? "✅" : "❌";
                string scalerStatus = canvasScaler != null ? "✅" : "❌";
                Debug.Log($"[UIConfigChecker] Canvas: {canvas.name} - Raycaster: {raycastStatus} Scaler: {scalerStatus}");
            }
        }
        
        return allGood;
    }
    
    private bool CheckUIButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>();
        if (buttons.Length == 0)
        {
            Debug.LogWarning("[UIConfigChecker] ⚠️ Nessun pulsante UI trovato");
            return true;
        }
        
        bool allGood = true;
        
        foreach (Button button in buttons)
        {
            // Verifica Image con raycastTarget
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null && !buttonImage.raycastTarget)
            {
                Debug.LogWarning($"[UIConfigChecker] ⚠️ Button {button.name}: raycastTarget = false");
                
                if (autoFix)
                {
                    buttonImage.raycastTarget = true;
                    Debug.Log($"[UIConfigChecker] ✅ raycastTarget abilitato per {button.name}");
                }
                else
                {
                    allGood = false;
                }
            }
            
            // Verifica che sia interactable
            if (!button.interactable)
            {
                Debug.LogWarning($"[UIConfigChecker] ⚠️ Button {button.name}: interactable = false");
            }
            
            // Verifica OnClick events
            if (button.onClick.GetPersistentEventCount() == 0)
            {
                Debug.LogWarning($"[UIConfigChecker] ⚠️ Button {button.name}: nessun OnClick event");
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"[UIConfigChecker] ✅ Verificati {buttons.Length} pulsanti UI");
        
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
                Debug.LogWarning($"[UIConfigChecker] ⚠️ Canvas {canvas.name} non su layer UI (attuale: {LayerMask.LayerToName(canvas.gameObject.layer)})");
                
                if (autoFix)
                {
                    canvas.gameObject.layer = LayerMask.NameToLayer("UI");
                    Debug.Log($"[UIConfigChecker] ✅ Layer UI assegnato a {canvas.name}");
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
        Debug.Log("[UIConfigChecker] === TEST UIBLOCKER ===");
        
        bool isOverUI = UIBlocker.IsPointerOverUI();
        Debug.Log($"[UIConfigChecker] IsPointerOverUI: {isOverUI}");
        
        if (isOverUI)
        {
            UIBlocker.DebugPointerOverUI();
        }
        
        Debug.Log("[UIConfigChecker] === FINE TEST ===");
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
