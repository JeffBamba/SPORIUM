using UnityEngine;
using UnityEditor;
using System.IO;

namespace Sporae.Editor
{
    /// <summary>
    /// BLK-01.03B: Generatore di sprite placeholder per le piante
    /// Crea sprite semplici per i diversi stadi di crescita
    /// </summary>
    public class PlantSpriteGenerator : EditorWindow
    {
        [MenuItem("Sporae/BLK-01.03B/Generate Plant Placeholder Sprites")]
        public static void GeneratePlantSprites()
        {
            string folderPath = "Assets/_Project/Placeholders/Plants";
            
            // Crea la cartella se non esiste
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }
            
            // Genera sprite per ogni stadio
            GenerateSprite(folderPath, "PLANT_Stage0_Empty", Color.gray, 32, 32);
            GenerateSprite(folderPath, "PLANT_Stage1_Seed", new Color(0.6f, 0.4f, 0.2f), 32, 32);
            GenerateSprite(folderPath, "PLANT_Stage2_Sprout", Color.green, 32, 32);
            GenerateSprite(folderPath, "PLANT_Stage3_Mature", Color.yellow, 32, 32);
            
            AssetDatabase.Refresh();
            Debug.Log("[BLK-01.03B] ✅ Sprite placeholder piante generati con successo!");
        }
        
        private static void GenerateSprite(string folderPath, string spriteName, Color color, int width, int height)
        {
            // Crea una texture colorata
            Texture2D texture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            // Salva la texture come PNG
            string filePath = Path.Combine(folderPath, spriteName + ".png");
            byte[] pngData = texture.EncodeToPNG();
            File.WriteAllBytes(filePath, pngData);
            
            // Imposta le impostazioni di importazione per lo sprite
            AssetDatabase.ImportAsset(filePath);
            TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            
            // Pulisci la texture temporanea
            DestroyImmediate(texture);
            
            Debug.Log($"[BLK-01.03B] Generato sprite: {spriteName}");
        }
        
        [MenuItem("Sporae/BLK-01.03B/Cleanup Generated Sprites")]
        public static void CleanupGeneratedSprites()
        {
            string folderPath = "Assets/_Project/Placeholders/Plants";
            
            if (Directory.Exists(folderPath))
            {
                string[] files = Directory.GetFiles(folderPath, "PLANT_Stage*.png");
                foreach (string file in files)
                {
                    AssetDatabase.DeleteAsset(file);
                }
                
                AssetDatabase.Refresh();
                Debug.Log("[BLK-01.03B] ✅ Sprite placeholder piante rimossi!");
            }
        }
    }
}
