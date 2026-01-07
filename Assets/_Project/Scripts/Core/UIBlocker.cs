using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;
using Sporae.DevTools;

/// <summary>
/// Utility per bloccare input mondo quando il puntatore è sopra UI.
/// Previene click-through sui pulsanti HUD.
/// </summary>
public static class UIBlocker
{
    static List<RaycastResult> _results = new List<RaycastResult>();
    static PointerEventData _ped;

    // DEBUG_SAFE_FIX: ignora il background UI Toolkit full-screen, altrimenti blocca tutti i click mondo.
    private static GameObject _cachedViewportBackgroundGO;
    private static bool TryIsViewportBackground(GameObject go)
    {
        if (go == null) return false;
        if (_cachedViewportBackgroundGO == null)
        {
            _cachedViewportBackgroundGO = GameObject.Find("HUD_GameViewportBackground");
        }
        if (_cachedViewportBackgroundGO == null) return false;
        return go == _cachedViewportBackgroundGO || go.transform.IsChildOf(_cachedViewportBackgroundGO.transform);
    }

    /// <summary>
    /// Verifica se il puntatore è sopra un elemento UI
    /// </summary>
    public static bool IsPointerOverUI()
    {
        // Caso 1/2: Raycast manuale (robusto e filtrabile: necessario per UI Toolkit full-screen)
        if (EventSystem.current != null)
        {
            if (_ped == null) _ped = new PointerEventData(EventSystem.current);
            _ped.position = Input.mousePosition;

            _results.Clear();
            EventSystem.current.RaycastAll(_ped, _results);

            int originalCount = _results.Count;
            if (originalCount > 0)
            {
                // DEBUG_SAFE_FIX: rimuovi hit del background viewport, altrimenti risulta sempre "sopra UI"
                _results.RemoveAll(r => TryIsViewportBackground(r.gameObject));
            }

            bool isOverUI = _results.Count > 0;
            #if UNITY_EDITOR
            if (isOverUI)
            {
                SporiumLogger.LogDebug(LogCategory.UI, $"Puntatore sopra UI (raycast): {_results.Count} elementi");
            }
            #endif
            
            return isOverUI;
        }
        
        #if UNITY_EDITOR
        SporiumLogger.LogWarning(LogCategory.UI, "EventSystem non trovato!");
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
            SporiumLogger.LogWarning(LogCategory.UI, "EventSystem non trovato per debug");
            return;
        }

        if (_ped == null) _ped = new PointerEventData(EventSystem.current);
        _ped.position = Input.mousePosition;

        _results.Clear();
        EventSystem.current.RaycastAll(_ped, _results);
        
        SporiumLogger.LogDebug(LogCategory.UI, $"Debug: {_results.Count} elementi UI sotto il puntatore:");
        for (int i = 0; i < _results.Count; i++)
        {
            var result = _results[i];
            SporiumLogger.LogDebug(LogCategory.UI, $"  {i}: {result.gameObject.name} (Layer: {LayerMask.LayerToName(result.gameObject.layer)})");
        }
    }
}
