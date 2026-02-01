using UnityEngine;
using UnityEditor;
using System.IO;
using _Project.Sporae.Core;

namespace Sporae.Editor
{
    /// <summary>
    /// Crea ItemConfig per Lab GDD42: cellule staminali, residui proteici, reagenti.
    /// Path: Resources/Items/ (o Assets/_Project/Resources/Items).
    /// </summary>
    public static class CreateLabItemConfigs
    {
        private const string ItemsFolder = "Assets/_Project/Resources/Items";
        private const string ItemsFolderAlt = "Assets/Resources/Items";

        [MenuItem("Tools/Sporae/Create Lab ItemConfig Assets (CELL, RES-PROT, REAG)")]
        public static void CreateLabItemConfigsMenu()
        {
            string path = ResolveItemsPath();
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[CreateLabItemConfigs] Cartella Resources/Items non trovata. Crea Assets/_Project/Resources/Items o Assets/Resources/Items.");
                return;
            }

            CreateItemConfig(path, Items.StemCellVegetable, "CELL-001", 0, 0, 0, false, 1f, false, true);
            CreateItemConfig(path, Items.StemCellFungus, "CELL-002", 0, 0, 0, false, 1f, false, true);
            CreateItemConfig(path, Items.StemCellAnimal, "CELL-003", 0, 0, 0, false, 1f, false, true);
            CreateItemConfig(path, Items.ProteinResidue, "RES-PROT-001", 0, 0, 0, false, 1f, false, true);
            CreateItemConfig(path, Items.ReagentX, "REAG-X", 0, 0, 0, false, 1f, false, true);
            CreateItemConfig(path, Items.ReagentY, "REAG-Y", 0, 0, 0, false, 1f, false, true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CreateLabItemConfigs] ItemConfig creati in " + path);
        }

        private static string ResolveItemsPath()
        {
            if (AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
            {
                EnsureFolder("Assets/_Project/Resources", "Items");
                return ItemsFolder;
            }
            if (AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                EnsureFolder("Assets/Resources", "Items");
                return ItemsFolderAlt;
            }
            return null;
        }

        private static void EnsureFolder(string parent, string name)
        {
            string full = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(full))
            {
                string[] parts = full.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
        }

        private static void CreateItemConfig(string folderPath, string typeId, string displayName, int maxQuality, int sellPrice, int buyPrice, bool isPerishable, float stability, bool isSeed, bool canStack)
        {
            string assetPath = folderPath + "/" + typeId + ".asset";
            if (AssetDatabase.LoadAssetAtPath<ItemConfig>(assetPath) != null)
            {
                Debug.Log($"[CreateLabItemConfigs] Esiste già: {typeId}");
                return;
            }

            var config = ScriptableObject.CreateInstance<ItemConfig>();
            var so = new SerializedObject(config);
            SerializedProperty typeIdProp = so.FindProperty("<TypeId>k__BackingField") ?? so.FindProperty("TypeId");
            if (typeIdProp != null) typeIdProp.stringValue = typeId;
            SerializedProperty mq = so.FindProperty("<MaxQuality>k__BackingField") ?? so.FindProperty("MaxQuality");
            if (mq != null) mq.intValue = maxQuality;
            SerializedProperty sp = so.FindProperty("<SellPrice>k__BackingField") ?? so.FindProperty("SellPrice");
            if (sp != null) sp.intValue = sellPrice;
            SerializedProperty bp = so.FindProperty("<BuyPrice>k__BackingField") ?? so.FindProperty("BuyPrice");
            if (bp != null) bp.intValue = buyPrice;
            SerializedProperty ip = so.FindProperty("<IsPerishable>k__BackingField") ?? so.FindProperty("IsPerishable");
            if (ip != null) ip.boolValue = isPerishable;
            SerializedProperty st = so.FindProperty("<Stability>k__BackingField") ?? so.FindProperty("Stability");
            if (st != null) st.floatValue = stability;
            SerializedProperty isd = so.FindProperty("<IsSeed>k__BackingField") ?? so.FindProperty("IsSeed");
            if (isd != null) isd.boolValue = isSeed;
            SerializedProperty cs = so.FindProperty("<CanStack>k__BackingField") ?? so.FindProperty("CanStack");
            if (cs != null) cs.boolValue = canStack;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(config, assetPath);
        }
    }
}
