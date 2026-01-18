using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace Sporae.Editor
{
    /// <summary>
    /// Editor script per pulire i RendererFeatures mancanti dal URP_2DRenderer.
    /// </summary>
    public static class CleanupURPRendererFeatures
    {
        [MenuItem("Tools/URP/Clean Missing Renderer Features")]
        public static void CleanMissingRendererFeatures()
        {
            // Prova prima con il percorso diretto
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
                "Assets/_Settings/URP/URP_2DRenderer.asset");

            // Se non trovato, prova con il GUID
            if (rendererData == null)
            {
                rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
                    AssetDatabase.GUIDToAssetPath("ee763a04672b21b4d84c5a129aa6df22"));
            }

            // Se ancora non trovato, cerca tutti i UniversalRendererData
            if (rendererData == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:UniversalRendererData");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                    Debug.Log($"[CleanupURP] Trovato UniversalRendererData: {path}");
                }
            }

            if (rendererData == null)
            {
                Debug.LogError("[CleanupURP] URP_2DRenderer.asset non trovato! Verifica che il file esista in Assets/_Settings/URP/");
                return;
            }

            // Verifica se ci sono RendererFeatures mancanti
            bool hasMissing = false;
            int initialCount = rendererData.rendererFeatures.Count;
            
            for (int i = rendererData.rendererFeatures.Count - 1; i >= 0; i--)
            {
                if (rendererData.rendererFeatures[i] == null)
                {
                    Debug.Log($"[CleanupURP] Rimosso RendererFeature mancante all'indice {i}");
                    rendererData.rendererFeatures.RemoveAt(i);
                    hasMissing = true;
                }
            }

            // Forza pulizia anche se la lista sembra vuota (potrebbe esserci un problema di serializzazione)
            if (rendererData.rendererFeatures.Count > 0 && rendererData.rendererFeatures.TrueForAll(x => x == null))
            {
                Debug.Log("[CleanupURP] Tutti i RendererFeatures sono null, pulizia completa della lista.");
                rendererData.rendererFeatures.Clear();
                hasMissing = true;
            }

            if (hasMissing || initialCount > 0)
            {
                // Forza il refresh del renderer
                EditorUtility.SetDirty(rendererData);
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(rendererData), ImportAssetOptions.ForceUpdate);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Debug.Log($"[CleanupURP] Pulizia completata! Rimossi {initialCount - rendererData.rendererFeatures.Count} RendererFeatures mancanti.");
                Debug.Log($"[CleanupURP] RendererFeatures rimanenti: {rendererData.rendererFeatures.Count}");
            }
            else
            {
                Debug.Log("[CleanupURP] Nessun RendererFeature mancante trovato. Lista già pulita.");
            }
        }
    }
}
