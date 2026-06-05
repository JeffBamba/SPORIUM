#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Pulizia one-shot ascensore 3.0: rimuove il pannello UI legacy non più usato.
/// Menu: Tools/Sporae/Elevator/Remove Legacy UI_ElevatorPanel
/// </summary>
public static class ElevatorLegacyCleanup
{
    private const string LegacyPanelName = "UI_ElevatorPanel";

    [MenuItem("Tools/Sporae/Elevator/Remove Legacy UI_ElevatorPanel")]
    public static void RemoveLegacyElevatorPanel()
    {
        var panel = GameObject.Find(LegacyPanelName);
        if (panel == null)
        {
            Debug.Log("[ElevatorLegacyCleanup] UI_ElevatorPanel non trovato (già rimosso).");
            return;
        }

        Undo.DestroyObjectImmediate(panel);
        Debug.Log("[ElevatorLegacyCleanup] UI_ElevatorPanel rimosso dalla scena attiva.");
    }
}
#endif
