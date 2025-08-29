using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Utility per bloccare input mondo quando il puntatore è sopra UI.
/// Previene click-through sui pulsanti HUD.
/// </summary>
public static class UIBlocker
{
    static List<RaycastResult> _results = new List<RaycastResult>();
    static PointerEventData _ped;

    /// <summary>
    /// Verifica se il puntatore è sopra un elemento UI
    /// </summary>
    public static bool IsPointerOverUI()
    {
        // Caso 1: API standard (mouse) - più veloce
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            #if UNITY_EDITOR
            Debug.Log("[UIBlocker] Puntatore sopra UI (API standard)");
            #endif
            return true;
        }

        // Caso 2: Raycast manuale (robusto per new input system)
        if (EventSystem.current != null)
        {
            if (_ped == null) _ped = new PointerEventData(EventSystem.current);
            _ped.position = Input.mousePosition;

            _results.Clear();
            EventSystem.current.RaycastAll(_ped, _results);
            
            bool isOverUI = _results.Count > 0;
            #if UNITY_EDITOR
            if (isOverUI)
            {
                Debug.Log($"[UIBlocker] Puntatore sopra UI (raycast): {_results.Count} elementi");
            }
            #endif
            
            return isOverUI;
        }
        
        #if UNITY_EDITOR
        Debug.LogWarning("[UIBlocker] EventSystem non trovato!");
        #endif
        return false;
    }

    /// <summary>
    /// Verifica se il puntatore è sopra un elemento UI specifico
    /// </summary>
    public static bool IsPointerOverUI(GameObject targetUI)
    {
        if (EventSystem.current == null) return false;
        
        if (_ped == null) _ped = new PointerEventData(EventSystem.current);
        _ped.position = Input.mousePosition;

        _results.Clear();
        EventSystem.current.RaycastAll(_ped, _results);
        
        foreach (var result in _results)
        {
            if (result.gameObject == targetUI || result.gameObject.transform.IsChildOf(targetUI.transform))
            {
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Debug: mostra tutti gli elementi UI sotto il puntatore
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DebugPointerOverUI()
    {
        if (EventSystem.current == null)
        {
            Debug.LogWarning("[UIBlocker] EventSystem non trovato per debug");
            return;
        }

        if (_ped == null) _ped = new PointerEventData(EventSystem.current);
        _ped.position = Input.mousePosition;

        _results.Clear();
        EventSystem.current.RaycastAll(_ped, _results);
        
        Debug.Log($"[UIBlocker] Debug: {_results.Count} elementi UI sotto il puntatore:");
        for (int i = 0; i < _results.Count; i++)
        {
            var result = _results[i];
            Debug.Log($"  {i}: {result.gameObject.name} (Layer: {LayerMask.LayerToName(result.gameObject.layer)})");
        }
    }
}
