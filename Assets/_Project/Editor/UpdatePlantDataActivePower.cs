using UnityEngine;
using UnityEditor;
using Sporae.Dome.PotSystem.Growth;

namespace _Project.Editor
{
    /// <summary>
    /// Editor script per aggiornare i valori ActivePower delle piante esistenti
    /// </summary>
    public class UpdatePlantDataActivePower : EditorWindow
    {
        [MenuItem("Sporae/Update Plant Active Powers")]
        public static void ShowWindow()
        {
            UpdateActivePowers();
        }
        
        private static void UpdateActivePowers()
        {
            // Valori ActivePower dal Notion
            var activePowers = new System.Collections.Generic.Dictionary<string, string>
            {
                { "PLT-STD-001", "Purificatrice: −10% rischio muffe Dome" },
                { "PLT-PURE-001", "Arctic Purification: rigenera pH della Dome con scala +1/Lv (fino a +5) e cura muffe ogni 2 giorni" },
                { "PLT-EVIL-001", "Allucinogeno: altera il pH Dome con scala -1/Lv (fino a -5) e aumenta la probabilità di mutazione Spore globali" }
            };
            
            int updated = 0;
            int errors = 0;
            
            foreach (var kvp in activePowers)
            {
                string plantCode = kvp.Key;
                string activePower = kvp.Value;
                
                string assetPath = $"Assets/Resources/Plants/{plantCode}.asset";
                PlantData plantData = AssetDatabase.LoadAssetAtPath<PlantData>(assetPath);
                
                if (plantData == null)
                {
                    Debug.LogError($"[UpdatePlantDataActivePower] ❌ PlantData non trovato: {assetPath}");
                    errors++;
                    continue;
                }
                
                // Usa SerializedObject per modificare il campo serializzato
                SerializedObject serializedObject = new SerializedObject(plantData);
                SerializedProperty activePowerProp = serializedObject.FindProperty("activePower");
                
                if (activePowerProp == null)
                {
                    Debug.LogError($"[UpdatePlantDataActivePower] ❌ Campo 'activePower' non trovato in {plantCode}");
                    errors++;
                    continue;
                }
                
                // Imposta il valore
                activePowerProp.stringValue = activePower;
                
                // Forza l'aggiornamento
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                
                // Salva l'asset
                EditorUtility.SetDirty(plantData);
                AssetDatabase.SaveAssets();
                
                // Forza il ricaricamento dell'asset
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                
                Debug.Log($"[UpdatePlantDataActivePower] ✅ Aggiornato {plantCode}: '{activePower}'");
                updated++;
            }
            
            // Forza il ricaricamento degli asset
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Update Complete", 
                $"Active Powers aggiornati!\n\n" +
                $"✅ Aggiornati: {updated}\n" +
                $"❌ Errori: {errors}\n\n" +
                $"Verifica nell'Inspector che i valori siano corretti.",
                "OK");
        }
    }
}

