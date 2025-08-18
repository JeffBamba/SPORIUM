#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PlayFromBootstrap
{
    private const string BootstrapPath = "Assets/_Project/Scenes/Bootstrap/SCN_Bootstrap.unity";

    static PlayFromBootstrap()
    {
        // Imposta automaticamente la scena di avvio del Play Mode su Bootstrap
        var bootstrap = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapPath);
        if (bootstrap != null && EditorSceneManager.playModeStartScene != bootstrap)
        {
            EditorSceneManager.playModeStartScene = bootstrap;
            // Facoltativo: log una sola volta
            // Debug.Log("[Editor] Play Mode Start Scene -> Bootstrap");
        }
    }
}
#endif
