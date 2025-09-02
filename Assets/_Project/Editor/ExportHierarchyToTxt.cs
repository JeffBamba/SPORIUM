using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class ExportHierarchyToTxt
{
    [MenuItem("Tools/Export Scene Hierarchy to TXT")]
    static void ExportHierarchy()
    {
        // Nome file e percorso di salvataggio
        string path = "Assets/_Project/Docs/SceneHierarchy.txt";
        StringBuilder sb = new StringBuilder();

        // Ciclo sugli oggetti root della scena attiva
        foreach (GameObject go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            WriteGameObject(go, sb, 0);
        }

        // Salvataggio su file
        File.WriteAllText(path, sb.ToString());
        AssetDatabase.Refresh();

        Debug.Log($"[ExportHierarchy] Hierarchy exported to: {path}");
    }

    static void WriteGameObject(GameObject go, StringBuilder sb, int indent)
    {
        string indentStr = new string(' ', indent * 2);

        // Nome del GameObject
        sb.AppendLine($"{indentStr}- {go.name}");

        // Lista componenti principali
        Component[] comps = go.GetComponents<Component>();
        foreach (var c in comps)
        {
            if (c == null) continue;
            sb.AppendLine($"{indentStr}  [Component] {c.GetType().Name}");
        }

        // Ricorsione sui figli
        foreach (Transform child in go.transform)
        {
            WriteGameObject(child.gameObject, sb, indent + 1);
        }
    }
}
