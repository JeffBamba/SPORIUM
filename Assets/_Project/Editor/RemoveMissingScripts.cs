using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace _Project.Editor
{
    /// <summary>
    /// Trova e rimuove componenti "Missing Script" dalla scena attiva (e opzionalmente dai prefab).
    /// Usa Tools → Remove Missing Scripts in Scene per pulire la scena aperta.
    /// </summary>
    public static class RemoveMissingScripts
    {
        [MenuItem("Tools/Remove Missing Scripts in Scene")]
        public static void RemoveInActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[RemoveMissingScripts] Nessuna scena attiva caricata.");
                return;
            }

            int totalRemoved = 0;
            var roots = scene.GetRootGameObjects();
            Undo.RegisterCompleteObjectUndo(roots, "Remove missing scripts");

            foreach (GameObject root in roots)
            {
                totalRemoved += RemoveMissingRecursive(root);
            }

            if (totalRemoved > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"[RemoveMissingScripts] Rimossi {totalRemoved} componenti 'Missing Script' dalla scena '{scene.name}'.");
            }
            else
            {
                Debug.Log("[RemoveMissingScripts] Nessun Missing Script trovato nella scena.");
            }
        }

        private static int RemoveMissingRecursive(GameObject go)
        {
            int count = 0;
            int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (missing > 0)
            {
                count += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                Debug.Log($"[RemoveMissingScripts] Rimossi {missing} da: {GetPath(go)}", go);
            }

            for (int i = 0; i < go.transform.childCount; i++)
            {
                count += RemoveMissingRecursive(go.transform.GetChild(i).gameObject);
            }

            return count;
        }

        private static string GetPath(GameObject go)
        {
            var t = go.transform;
            var path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
