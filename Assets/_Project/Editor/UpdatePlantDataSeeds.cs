using UnityEngine;
using UnityEditor;
using Sporae.Dome.PotSystem.Growth;
using _Project.Sporae.Core;

namespace Sporae.Editor
{
    /// <summary>
    /// Script editor per aggiornare i PlantData esistenti con i seed corretti
    /// </summary>
    public class UpdatePlantDataSeeds : EditorWindow
    {
        [MenuItem("Tools/Sporae/Update PlantData Seeds")]
        public static void UpdateSeeds()
        {
            string plantsPath = "Assets/Resources/Plants";
            
            // Carica i seed
            ItemConfig seed001 = Resources.Load<ItemConfig>("Items/seed-001");
            ItemConfig seed002 = Resources.Load<ItemConfig>("Items/seed-002");
            ItemConfig seed003 = Resources.Load<ItemConfig>("Items/seed-003");
            
            if (seed001 == null || seed002 == null || seed003 == null)
            {
                Debug.LogError("[UpdatePlantDataSeeds] Alcuni seed non trovati! Verifica che esistano in Resources/Items/");
                return;
            }
            
            // Carica i PlantData
            PlantData std001 = AssetDatabase.LoadAssetAtPath<PlantData>($"{plantsPath}/PLT-STD-001.asset");
            PlantData pure001 = AssetDatabase.LoadAssetAtPath<PlantData>($"{plantsPath}/PLT-PURE-001.asset");
            PlantData evil001 = AssetDatabase.LoadAssetAtPath<PlantData>($"{plantsPath}/PLT-EVIL-001.asset");
            
            int updated = 0;
            
            // Aggiorna PLT-STD-001 → seed-001
            if (std001 != null)
            {
                SetPrivateField(std001, "seedItemConfig", seed001);
                EditorUtility.SetDirty(std001);
                updated++;
                Debug.Log("[UpdatePlantDataSeeds] ✅ PLT-STD-001 → seed-001");
            }
            
            // Aggiorna PLT-PURE-001 → seed-002
            if (pure001 != null)
            {
                SetPrivateField(pure001, "seedItemConfig", seed002);
                EditorUtility.SetDirty(pure001);
                updated++;
                Debug.Log("[UpdatePlantDataSeeds] ✅ PLT-PURE-001 → seed-002");
            }
            
            // Aggiorna PLT-EVIL-001 → seed-003
            if (evil001 != null)
            {
                SetPrivateField(evil001, "seedItemConfig", seed003);
                EditorUtility.SetDirty(evil001);
                updated++;
                Debug.Log("[UpdatePlantDataSeeds] ✅ PLT-EVIL-001 → seed-003");
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[UpdatePlantDataSeeds] ✅ Aggiornati {updated} PlantData!");
            EditorUtility.DisplayDialog("Success", 
                $"PlantData aggiornati!\n\n" +
                $"PLT-STD-001 → seed-001 (Standard)\n" +
                $"PLT-PURE-001 → seed-002 (Pure)\n" +
                $"PLT-EVIL-001 → seed-003 (Evil)", 
                "OK");
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
                Debug.LogWarning($"[UpdatePlantDataSeeds] Campo '{fieldName}' non trovato!");
            }
        }
    }
}

