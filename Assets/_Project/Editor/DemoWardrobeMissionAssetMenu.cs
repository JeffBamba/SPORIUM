using System.IO;
using UnityEditor;
using UnityEngine;
using _Project.Sporae.Core;

namespace Sporae.Editor
{
    /// <summary>
    /// Genera asset missione demo "Accedi all'Armadio" + goal flag (una tantum, poi versionati in Git).
    /// </summary>
    public static class DemoWardrobeMissionAssetMenu
    {
        private const string MissionsFolder = "Assets/_Project/Resources/Missions";

        [MenuItem("Sporae/Demo/Create Wardrobe Mission Assets (Goal + MissionConfig)")]
        public static void CreateAssets()
        {
            EnsureFolder("Assets/_Project/Resources", "Missions");

            string goalPath = $"{MissionsFolder}/Goal_Demo_Wardrobe.asset";
            string missionPath = $"{MissionsFolder}/M_Demo_Wardrobe.asset";

            if (AssetDatabase.LoadAssetAtPath<MissionFlagGoal>(goalPath) != null &&
                AssetDatabase.LoadAssetAtPath<MissionConfig>(missionPath) != null)
            {
                EditorUtility.DisplayDialog("Demo mission", "Asset già presenti in Resources/Missions.", "OK");
                return;
            }

            var goal = ScriptableObject.CreateInstance<MissionFlagGoal>();
            AssetDatabase.CreateAsset(goal, goalPath);

            var soGoal = new SerializedObject(goal);
            var titleG = soGoal.FindProperty("<Title>k__BackingField");
            if (titleG != null)
                titleG.stringValue = "Apri il guardaroba";
            var fk = soGoal.FindProperty("_flagKey");
            if (fk != null)
                fk.stringValue = WardrobeMission.DemoWardrobeFlagKey;
            soGoal.ApplyModifiedPropertiesWithoutUndo();

            var mission = ScriptableObject.CreateInstance<MissionConfig>();
            AssetDatabase.CreateAsset(mission, missionPath);

            var soM = new SerializedObject(mission);
            var titleM = soM.FindProperty("<Title>k__BackingField");
            if (titleM != null)
                titleM.stringValue = "Accedi all'Armadio";
            var descM = soM.FindProperty("<Description>k__BackingField");
            if (descM != null)
                descM.stringValue = "Interagisci con l'armadio nella camera da letto per aprire il guardaroba.";

            var goals = soM.FindProperty("<Goals>k__BackingField");
            if (goals != null)
            {
                goals.arraySize = 1;
                var g0 = goals.GetArrayElementAtIndex(0);
                var options = g0.FindPropertyRelative("Options");
                if (options != null)
                {
                    options.arraySize = 1;
                    options.GetArrayElementAtIndex(0).objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<MissionFlagGoal>(goalPath);
                }
            }

            soM.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Demo mission", $"Creati:\n{goalPath}\n{missionPath}", "OK");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string full = $"{parent}/{child}";
            if (AssetDatabase.IsValidFolder(full))
                return;
            if (!AssetDatabase.IsValidFolder(parent))
                return;
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
