using UnityEngine;
using UnityEditor;
using System.IO;

namespace Sporae.Editor
{
    /// <summary>
    /// Editor script per creare la cartella Plants se non esiste
    /// e spostare i PlantData nella posizione corretta
    /// </summary>
    public class CreatePlantsFolder : EditorWindow
    {
        [MenuItem("Tools/Sporae/Create Plants Folder")]
        public static void CreateFolder()
        {
            string plantsPath = "Assets/Resources/Plants";
            
            // Crea la cartella se non esiste
            if (!AssetDatabase.IsValidFolder(plantsPath))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Plants");
                Debug.Log($"[CreatePlantsFolder] Cartella {plantsPath} creata!");
            }
            else
            {
                Debug.Log($"[CreatePlantsFolder] Cartella {plantsPath} già esiste!");
            }
            
            // Forza refresh
            AssetDatabase.Refresh();
            
            // Verifica se ci sono file PlantData nella cartella
            string[] guids = AssetDatabase.FindAssets("t:PlantData", new[] { plantsPath });
            Debug.Log($"[CreatePlantsFolder] Trovati {guids.Length} PlantData nella cartella Plants");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Debug.Log($"  - {path}");
            }
            
            EditorUtility.DisplayDialog("Create Plants Folder", 
                $"Cartella creata/verificata!\nTrovati {guids.Length} PlantData.", 
                "OK");
        }
        
        [MenuItem("Tools/Sporae/Refresh Plants Folder")]
        public static void RefreshFolder()
        {
            AssetDatabase.Refresh();
            Debug.Log("[CreatePlantsFolder] Refresh completato!");
            EditorUtility.DisplayDialog("Refresh", "Refresh completato!", "OK");
        }
    }
}

