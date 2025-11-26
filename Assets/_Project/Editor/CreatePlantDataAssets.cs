using UnityEngine;
using UnityEditor;
using System.IO;
using Sporae.Dome.PotSystem.Growth;
using _Project.Sporae.Core;

namespace Sporae.Editor
{
    /// <summary>
    /// Script editor per creare i PlantData direttamente in Unity
    /// </summary>
    public class CreatePlantDataAssets : EditorWindow
    {
        [MenuItem("Tools/Sporae/Create All PlantData Assets")]
        public static void CreateAllPlantData()
        {
            // Prova entrambe le posizioni possibili
            string[] possiblePaths = {
                "Assets/Resources/Plants",
                "Assets/_Project/Resources/Plants"
            };
            
            string plantsPath = null;
            
            // Cerca quale cartella Resources esiste
            foreach (string path in possiblePaths)
            {
                string parentPath = path.Replace("/Plants", "");
                if (AssetDatabase.IsValidFolder(parentPath))
                {
                    plantsPath = path;
                    Debug.Log($"[CreatePlantDataAssets] Trovata cartella Resources: {parentPath}");
                    break;
                }
            }
            
            // Se nessuna esiste, crea in Assets/Resources (standard Unity)
            if (plantsPath == null)
            {
                plantsPath = "Assets/Resources/Plants";
                Debug.Log($"[CreatePlantDataAssets] Nessuna cartella Resources trovata, uso: {plantsPath}");
            }
            
            // Crea la cartella Plants se non esiste
            if (!AssetDatabase.IsValidFolder(plantsPath))
            {
                string parentPath = plantsPath.Replace("/Plants", "");
                AssetDatabase.CreateFolder(parentPath, "Plants");
                Debug.Log($"[CreatePlantDataAssets] Cartella {plantsPath} creata!");
            }
            else
            {
                Debug.Log($"[CreatePlantDataAssets] Cartella {plantsPath} già esiste!");
            }
            
            // Crea i 3 PlantData (ognuno collegato a un seed diverso)
            CreatePlantData("PLT-STD-001", PlantFamily.Standard, 0f, "seed-001");  // Standard → seed-001
            CreatePlantData("PLT-PURE-001", PlantFamily.Pure, 2f, "seed-002");     // Pure → seed-002
            CreatePlantData("PLT-EVIL-001", PlantFamily.Evil, -2f, "seed-003");   // Evil → seed-003
            
            // Forza refresh aggressivo
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            
            // Seleziona la cartella Plants nel Project window
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(plantsPath);
            if (folder != null)
            {
                Selection.activeObject = folder;
                EditorUtility.FocusProjectWindow();
            }
            
            Debug.Log("[CreatePlantDataAssets] ✅ Tutti i PlantData creati con successo!");
            Debug.Log($"[CreatePlantDataAssets] 📁 Cartella: {Path.GetFullPath(plantsPath)}");
            
            // Verifica file creati
            string[] guids = AssetDatabase.FindAssets("t:PlantData", new[] { plantsPath });
            Debug.Log($"[CreatePlantDataAssets] 🔍 Trovati {guids.Length} PlantData dopo refresh");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Debug.Log($"   - {path}");
            }
            
            EditorUtility.DisplayDialog("Success", 
                $"PlantData creati con successo!\n\n" +
                $"Trovati {guids.Length} PlantData nella cartella.\n\n" +
                $"Se non li vedi nel Project window:\n" +
                $"1. Premi F5 o Assets > Refresh\n" +
                $"2. Naviga manualmente a Assets/Resources/Plants\n" +
                $"3. Cerca 'PLT-' nel Project window", 
                "OK");
        }
        
        private static void CreatePlantData(string plantCode, PlantFamily family, float phDrift, string seedTypeId)
        {
            string assetPath = $"Assets/Resources/Plants/{plantCode}.asset";
            
            // Verifica se esiste già
            PlantData existing = AssetDatabase.LoadAssetAtPath<PlantData>(assetPath);
            if (existing != null)
            {
                Debug.LogWarning($"[CreatePlantDataAssets] {plantCode} già esiste, saltato!");
                return;
            }
            
            // Crea nuovo PlantData
            PlantData plantData = ScriptableObject.CreateInstance<PlantData>();
            
            // Imposta valori usando reflection (perché i campi sono serialized private)
            SetPrivateField(plantData, "plantCode", plantCode);
            SetPrivateField(plantData, "family", family);
            SetPrivateField(plantData, "dailyPhDrift", phDrift);
            SetPrivateField(plantData, "optimalPhMin", -29f);
            SetPrivateField(plantData, "optimalPhMax", 29f);
            SetPrivateField(plantData, "rarity", PlantRarity.Common);
            
            // Cerca Seed Item Config
            ItemConfig seedConfig = Resources.Load<ItemConfig>($"Items/{seedTypeId}");
            if (seedConfig != null)
            {
                SetPrivateField(plantData, "seedItemConfig", seedConfig);
                Debug.Log($"[CreatePlantDataAssets] Seed Item Config assegnato: {seedTypeId}");
            }
            else
            {
                Debug.LogWarning($"[CreatePlantDataAssets] Seed Item Config '{seedTypeId}' non trovato! Assegnare manualmente.");
            }
            
            // Crea l'asset
            AssetDatabase.CreateAsset(plantData, assetPath);
            Debug.Log($"[CreatePlantDataAssets] ✅ Creato: {assetPath}");
        }
        
        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Debug.LogWarning($"[CreatePlantDataAssets] Campo '{fieldName}' non trovato!");
            }
        }
        
        [MenuItem("Tools/Sporae/Delete All PlantData Assets")]
        public static void DeleteAllPlantData()
        {
            if (!EditorUtility.DisplayDialog("Conferma Eliminazione", 
                "Vuoi eliminare tutti i PlantData in Assets/Resources/Plants/?", 
                "Sì", "No"))
            {
                return;
            }
            
            string[] guids = AssetDatabase.FindAssets("t:PlantData", new[] { "Assets/Resources/Plants" });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"[CreatePlantDataAssets] Eliminato: {path}");
            }
            
            AssetDatabase.Refresh();
            Debug.Log($"[CreatePlantDataAssets] ✅ Eliminati {guids.Length} PlantData");
        }
    }
}

