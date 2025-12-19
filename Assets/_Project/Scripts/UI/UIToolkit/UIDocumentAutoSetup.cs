using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Sporae.DevTools;

namespace Sporae.UI.UIToolkit
{
    /// <summary>
    /// Script helper che verifica e configura automaticamente UIDocument con PanelSettings se mancante.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIDocumentAutoSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool _autoCreatePanelSettings = true;
        [SerializeField] private bool _showDebugLogs = false;
        
        private void Awake()
        {
            if (_autoCreatePanelSettings)
            {
                SetupPanelSettings();
            }
        }
        
        private void SetupPanelSettings()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                if (_showDebugLogs)
                    SporiumLogger.LogWarning(LogCategory.UI, "UIDocument non trovato su UIDocumentAutoSetup");
                return;
            }
            
            // Se PanelSettings è già assegnato, non fare nulla
            if (uiDocument.panelSettings != null)
            {
                if (_showDebugLogs)
                    SporiumLogger.LogInfo(LogCategory.UI, "PanelSettings già assegnato");
                return;
            }
            
            // Cerca un PanelSettings esistente nel progetto
            var existingPanelSettings = FindPanelSettingsInProject();
            if (existingPanelSettings != null)
            {
                uiDocument.panelSettings = existingPanelSettings;
                if (_showDebugLogs)
                    SporiumLogger.LogInfo(LogCategory.UI, $"PanelSettings trovato e assegnato: {existingPanelSettings.name}");
                return;
            }
            
            // Se non trovato, avvisa l'utente (non possiamo creare asset a runtime)
            if (_showDebugLogs)
            {
                SporiumLogger.LogWarning(LogCategory.UI, 
                    "PanelSettings non trovato! Crea un PanelSettings asset: Create > UI Toolkit > Panel Settings Asset");
            }
        }
        
        private PanelSettings FindPanelSettingsInProject()
        {
            #if UNITY_EDITOR
            // Cerca in Assets/_Project/UI/UIToolkit/ prima
            var path = "Assets/_Project/UI/UIToolkit/";
            var guids = AssetDatabase.FindAssets("t:PanelSettings", new[] { path });
            
            if (guids.Length > 0)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<PanelSettings>(assetPath);
            }
            
            // Se non trovato, cerca in tutto il progetto
            guids = AssetDatabase.FindAssets("t:PanelSettings");
            if (guids.Length > 0)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<PanelSettings>(assetPath);
            }
            #endif
            
            return null;
        }
        
        #if UNITY_EDITOR
        [ContextMenu("Create PanelSettings Asset")]
        private void CreatePanelSettingsAsset()
        {
            var path = "Assets/_Project/UI/UIToolkit/";
            if (!AssetDatabase.IsValidFolder(path))
            {
                SporiumLogger.LogError(LogCategory.UI, $"Cartella non trovata: {path}");
                return;
            }
            
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            var assetPath = $"{path}PlayerStatusPanelSettings.asset";
            AssetDatabase.CreateAsset(panelSettings, assetPath);
            AssetDatabase.SaveAssets();
            
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument != null)
            {
                uiDocument.panelSettings = panelSettings;
                EditorUtility.SetDirty(uiDocument);
            }
            
            SporiumLogger.LogInfo(LogCategory.UI, $"PanelSettings creato: {assetPath}");
        }
        #endif
    }
}

