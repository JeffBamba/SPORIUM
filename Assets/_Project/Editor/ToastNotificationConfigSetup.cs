using UnityEngine;
using UnityEditor;
using Sporae.DevTools;
using System.Linq;
using TMPro;

namespace Sporae.Editor
{
    /// <summary>
    /// Script editor per configurare automaticamente ToastNotificationConfig
    /// </summary>
    public class ToastNotificationConfigSetup
    {
        [MenuItem("Tools/Sporae/Setup ToastNotificationConfig")]
        public static void SetupToastNotificationConfig()
        {
            // Cerca o crea l'asset
            string configPath = "Assets/Resources/Configs/ToastNotificationConfig.asset";
            ToastNotificationConfig config = AssetDatabase.LoadAssetAtPath<ToastNotificationConfig>(configPath);
            
            if (config == null)
            {
                Debug.LogWarning($"[ToastNotificationConfigSetup] Asset non trovato in {configPath}. Creane uno prima con Assets > Create > Spore > ToastNotificationConfig");
                EditorUtility.DisplayDialog("ToastNotificationConfig Non Trovato", 
                    $"L'asset non è stato trovato in:\n{configPath}\n\nCrea prima l'asset con:\nAssets > Create > Spore > ToastNotificationConfig", 
                    "OK");
                return;
            }
            
            // Popola TypeSettings con tutti i tipi
            System.Array allTypes = System.Enum.GetValues(typeof(ToastNotificationType));
            config.TypeSettings = new ToastNotificationConfig.ToastTypeSettings[allTypes.Length];
            
            for (int i = 0; i < allTypes.Length; i++)
            {
                ToastNotificationType type = (ToastNotificationType)allTypes.GetValue(i);
                int severity = type.GetSeverity();
                
                config.TypeSettings[i] = new ToastNotificationConfig.ToastTypeSettings
                {
                    Type = type,
                    Color = GetColorForSeverity(severity),
                    DefaultDuration = GetDurationForType(type),
                    CodePrefix = GetCodePrefixForType(type),
                    SeverityIcon = GetSeverityIconForType(severity, config)
                };
            }
            
            // Imposta UI Settings
            config.FixedWidth = 306;
            config.PositionOffset = new Vector2(-24, -96);
            config.FontSize = 10;
            config.CharacterSpacing = 2f;
            
            // Cerca automaticamente TMP_FontAsset
            if (config.MonospacedFont == null)
            {
                config.MonospacedFont = FindTMPFontAsset();
            }
            
            // Imposta Pixel Art Settings
            config.TextureFilterMode = FilterMode.Point;
            
            // Cerca automaticamente gli sprite
            if (config.BorderSprite == null)
            {
                config.BorderSprite = FindSprite("Border_2px_Solid", "Border", "border");
            }
            
            if (config.CornerSprite == null)
            {
                config.CornerSprite = FindSprite("Corner_LShape_3x3", "Corner", "corner", "LShape");
            }
            
            if (config.AlertCircleIcon == null)
            {
                config.AlertCircleIcon = FindSprite("Icon_AlertCircle", "AlertCircle", "alert", "Alert");
            }
            
            if (config.InfoIcon == null)
            {
                config.InfoIcon = FindSprite("Icon_Info", "Info", "info");
            }
            
            if (config.WarningIcon == null)
            {
                config.WarningIcon = FindSprite("Icon_Warning", "Warning", "warning", "triangle");
            }
            
            if (config.DangerIcon == null)
            {
                config.DangerIcon = FindSprite("Icon_Danger", "Danger", "danger", "alert");
            }
            
            if (config.ChevronIcon == null)
            {
                config.ChevronIcon = FindSprite("Icon_Chevron", "Chevron", "chevron", "arrow");
            }
            
            // Imposta Global Settings
            config.DefaultDuration = 3f;
            config.MaxHistoryEntries = 100;
            config.EnableHistory = true;
            
            // Marca come dirty per salvare
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[ToastNotificationConfigSetup] ✅ ToastNotificationConfig configurato automaticamente!");
            Debug.Log($"[ToastNotificationConfigSetup] ✅ {allTypes.Length} tipi configurati.");
            
            // Report su cosa è stato trovato automaticamente
            string report = "[ToastNotificationConfigSetup] Asset trovati automaticamente:\n";
            report += config.MonospacedFont != null ? "  ✓ MonospacedFont\n" : "  ✗ MonospacedFont (non trovato)\n";
            report += config.BorderSprite != null ? "  ✓ BorderSprite\n" : "  ✗ BorderSprite (non trovato)\n";
            report += config.CornerSprite != null ? "  ✓ CornerSprite\n" : "  ✗ CornerSprite (non trovato)\n";
            report += config.AlertCircleIcon != null ? "  ✓ AlertCircleIcon\n" : "  ✗ AlertCircleIcon (non trovato)\n";
            report += config.InfoIcon != null ? "  ✓ InfoIcon\n" : "  ✗ InfoIcon (non trovato)\n";
            report += config.WarningIcon != null ? "  ✓ WarningIcon\n" : "  ✗ WarningIcon (non trovato)\n";
            report += config.DangerIcon != null ? "  ✓ DangerIcon\n" : "  ✗ DangerIcon (non trovato)\n";
            report += config.ChevronIcon != null ? "  ✓ ChevronIcon\n" : "  ✗ ChevronIcon (non trovato)\n";
            report += "\n[ToastNotificationConfigSetup] ⚠️ Assegna manualmente gli asset mancanti se necessario.";
            
            Debug.Log(report);
            
            EditorUtility.DisplayDialog("ToastNotificationConfig Configurato", 
                $"Configurazione completata!\n\n{allTypes.Length} tipi configurati.\n\nControlla la Console per dettagli sugli asset trovati automaticamente.", 
                "OK");
        }
        
        private static Color32 GetColorForSeverity(int severity)
        {
            return severity switch
            {
                0 or 1 => ToastNotificationConfig.COLOR_INFO,      // #7FFF7A Verde LED
                2 => ToastNotificationConfig.COLOR_WARNING,         // #E6C96F Giallo
                3 or 4 => ToastNotificationConfig.COLOR_DANGER,    // #D35F5F Rosso
                _ => ToastNotificationConfig.COLOR_BLUE_NEUTRAL    // #5DB6E3 Blu
            };
        }
        
        private static float GetDurationForType(ToastNotificationType type)
        {
            // Durate personalizzate per tipo
            return type switch
            {
                ToastNotificationType.Success or ToastNotificationType.ActionSuccess => 3f,
                ToastNotificationType.ItemCollected or ToastNotificationType.ResourceGained => 2.5f,
                ToastNotificationType.Info or ToastNotificationType.StageUp => 3f,
                ToastNotificationType.ConditionImproved => 3f,
                ToastNotificationType.SystemEnabled => 2f,
                ToastNotificationType.Warning or ToastNotificationType.ConditionDegraded => 4f,
                ToastNotificationType.SystemDisabled => 3f,
                ToastNotificationType.CountdownAlert => 5f,
                ToastNotificationType.Error or ToastNotificationType.ActionFailed => 4f,
                ToastNotificationType.ResourceInsufficient => 3f,
                ToastNotificationType.InvalidOperation => 3f,
                ToastNotificationType.Critical or ToastNotificationType.PlantDied => 5f,
                ToastNotificationType.ExtremePhDeath => 5f,
                ToastNotificationType.SystemFailure => 6f,
                _ => 3f
            };
        }
        
        private static string GetCodePrefixForType(ToastNotificationType type)
        {
            // Prefissi codice standardizzati
            return type switch
            {
                // Pot Actions
                ToastNotificationType.ActionSuccess => "POT-ACTION-SUCCESS-",
                ToastNotificationType.ActionFailed => "POT-ACTION-FAILED-",
                
                // Day Cycle
                ToastNotificationType.StageUp => "STAGE-UP-",
                ToastNotificationType.ConditionImproved => "CND-002-",
                ToastNotificationType.ConditionDegraded => "CND-001-",
                ToastNotificationType.SystemEnabled => "SYS-ENABLED-",
                ToastNotificationType.SystemDisabled => "SYS-DISABLED-",
                
                // Mold
                ToastNotificationType.Warning => "WARNING-",
                
                // pH
                ToastNotificationType.ExtremePhDeath => "PH-DEATH-",
                ToastNotificationType.CountdownAlert => "PH-COUNTDOWN-",
                
                // Pot Details
                ToastNotificationType.Success => "SUCCESS-",
                ToastNotificationType.Error => "ERROR-",
                ToastNotificationType.Info => "INFO-",
                
                // Plant Death
                ToastNotificationType.PlantDied => "PLANT-DEATH-",
                
                // Inventory
                ToastNotificationType.ItemCollected => "INV-",
                ToastNotificationType.ResourceGained => "RES-",
                
                // Default
                _ => type.ToString().Substring(0, Mathf.Min(3, type.ToString().Length)).ToUpper() + "-"
            };
        }
        
        private static Sprite GetSeverityIconForType(int severity, ToastNotificationConfig config)
        {
            // Usa le icone globali se disponibili
            return severity switch
            {
                0 or 1 => config.InfoIcon,
                2 => config.WarningIcon,
                3 or 4 => config.DangerIcon,
                _ => config.InfoIcon
            };
        }
        
        /// <summary>
        /// Cerca uno sprite nel progetto usando vari nomi possibili
        /// </summary>
        private static Sprite FindSprite(params string[] searchNames)
        {
            string[] guids = AssetDatabase.FindAssets("t:Sprite");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
                
                foreach (string searchName in searchNames)
                {
                    if (fileName.Contains(searchName.ToLower()))
                    {
                        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                        if (sprite != null)
                        {
                            Debug.Log($"[ToastNotificationConfigSetup] ✓ Trovato sprite: {path} (cercato: {searchName})");
                            return sprite;
                        }
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Cerca un TMP_FontAsset nel progetto
        /// </summary>
        private static TMP_FontAsset FindTMPFontAsset()
        {
            // Cerca font comuni per pixel art / monospaced
            string[] commonFontNames = { "Courier", "Consolas", "Monospace", "Pixel", "Mono" };
            
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                
                foreach (string fontName in commonFontNames)
                {
                    if (fileName.Contains(fontName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                        if (font != null)
                        {
                            Debug.Log($"[ToastNotificationConfigSetup] ✓ Trovato TMP_FontAsset: {path}");
                            return font;
                        }
                    }
                }
            }
            
            // Se non trova font specifici, restituisce il primo disponibile (se esiste)
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (font != null)
                {
                    Debug.Log($"[ToastNotificationConfigSetup] ⚠️ Usato primo TMP_FontAsset trovato: {path} (non è monospaced, considera di crearne uno)");
                    return font;
                }
            }
            
            return null;
        }
    }
}

