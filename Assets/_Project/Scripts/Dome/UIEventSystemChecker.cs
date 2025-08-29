using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Verifica che l'EventSystem sia configurato correttamente per i click UI.
/// </summary>
public class UIEventSystemChecker : MonoBehaviour
{
    void Start()
    {
        CheckEventSystem();
    }
    
    [ContextMenu("Check Event System")]
    public void CheckEventSystem()
    {
        Debug.Log("=== CHECKING EVENT SYSTEM ===");
        
        // 1. Verifica EventSystem
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem != null)
        {
            Debug.Log($"✓ EventSystem trovato: {eventSystem.name}");
            Debug.Log($"  StandaloneInputModule: {eventSystem.GetComponent<StandaloneInputModule>() != null}");
        }
        else
        {
            Debug.LogError("✗ EventSystem NON TROVATO!");
        }
        
        // 2. Verifica Canvas e GraphicRaycaster
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Debug.Log($"Canvas trovati: {canvases.Length}");
        
        foreach (Canvas canvas in canvases)
        {
            Debug.Log($"Canvas: {canvas.name}");
            Debug.Log($"  RenderMode: {canvas.renderMode}");
            Debug.Log($"  GraphicRaycaster: {canvas.GetComponent<GraphicRaycaster>() != null}");
            Debug.Log($"  CanvasScaler: {canvas.GetComponent<CanvasScaler>() != null}");
            
            // Verifica se ha GraphicRaycaster
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                Debug.LogWarning($"  ⚠ Aggiungendo GraphicRaycaster a {canvas.name}");
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }
        
        // 3. Verifica pulsanti UI
        Button[] buttons = FindObjectsOfType<Button>();
        Debug.Log($"Pulsanti UI trovati: {buttons.Length}");
        
        foreach (Button button in buttons)
        {
            Debug.Log($"Pulsante: {button.name}");
            Debug.Log($"  Image: {button.GetComponent<Image>() != null}");
            Debug.Log($"  Interactable: {button.interactable}");
            Debug.Log($"  OnClick events: {button.onClick.GetPersistentEventCount()}");
        }
        
        Debug.Log("=== EVENT SYSTEM CHECK COMPLETED ===");
    }
    
    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, "UIEventSystemChecker");
    }
    #endif
}
