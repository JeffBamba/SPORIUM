using UnityEngine;
using System.Linq;

namespace Sporae.DevTools
{
    /// <summary>
    /// Configurazione centralizzata per sistema toast notifications
    /// Include palette colori, pixel art settings, sprites e impostazioni UI
    /// </summary>
    [CreateAssetMenu(menuName = "Spore/ToastNotificationConfig")]
    public class ToastNotificationConfig : ScriptableObject
    {
        // Palette Severità (Color32)
        public static readonly Color32 COLOR_INFO = new Color32(127, 255, 122, 255);        // #7FFF7A Verde LED
        public static readonly Color32 COLOR_WARNING = new Color32(230, 201, 111, 255);     // #E6C96F Giallo
        public static readonly Color32 COLOR_DANGER = new Color32(211, 95, 95, 255);        // #D35F5F Rosso
        public static readonly Color32 COLOR_BLUE_NEUTRAL = new Color32(93, 182, 227, 255); // #5DB6E3 Blu header neutro
        public static readonly Color32 COLOR_MISSION = new Color32(0, 255, 198, 255);       // #00FFC6 Cyan — uguale al mission recap panel
        
        // Background
        public static readonly Color BACKGROUND_COLOR = new Color(0.11f, 0.16f, 0.16f, 0.9f); // #1E282A alpha 0.9
        
        // Testo secondario
        public static readonly Color TEXT_SECONDARY_LIGHT = new Color32(192, 200, 197, 255); // #C0C8C5
        public static readonly Color TEXT_SECONDARY_DARK = new Color32(139, 152, 148, 255);  // #8B9894
        
        [System.Serializable]
        public class ToastTypeSettings
        {
            public ToastNotificationType Type;
            public Color32 Color; // Usa Color32 per pixel art
            public float DefaultDuration;
            public string CodePrefix; // "CND-", "LGT-", etc.
            public Sprite SeverityIcon; // Info (i), Warning (triangle), Danger (alert circle)
        }
        
        [Header("Type Settings")]
        public ToastTypeSettings[] TypeSettings;
        
    [Header("UI Settings")]
    public int FixedWidth = 306; // px
    public Vector2 PositionOffset = new Vector2(-24, -96); // Offset da top-right corner
    public TMPro.TMP_FontAsset MonospacedFont; // Courier New, Consolas, o custom pixel font (TMP_FontAsset per TextMeshProUGUI)
    public int FontSize = 10; // 10-11pt
    public float CharacterSpacing = 2f; // Tracking aumentato per header
        
        [Header("Pixel Art Settings")]
        public Sprite BorderSprite; // 2px solid border
        public Sprite CornerSprite; // L-shape 3×3 pixel per corner
        public Material GlowShader; // Shader custom per glow triplo (opzionale)
        public FilterMode TextureFilterMode = FilterMode.Point; // Pixel-perfect
        
        [Header("Icons")]
        public Sprite AlertCircleIcon; // Per header
        public Sprite InfoIcon; // i icon
        public Sprite WarningIcon; // triangle
        public Sprite DangerIcon; // alert circle
        public Sprite ChevronIcon; // Arrow per toggle
        
        [Header("Global Settings")]
        public float DefaultDuration = 3f;
        public int MaxHistoryEntries = 100;
        public bool EnableHistory = true;
        
        /// <summary>
        /// Ottiene il colore Color32 per un tipo toast
        /// </summary>
        public Color32 GetColor32(ToastNotificationType type)
        {
            var setting = TypeSettings?.FirstOrDefault(s => s.Type == type);
            if (setting != null)
                return setting.Color;
            
            // Tipo mission ha colore dedicato
            if (type == ToastNotificationType.Mission)
                return COLOR_MISSION;

            // Fallback basato su severità
            int severity = type.GetSeverity();
            return severity switch
            {
                0 or 1 => COLOR_INFO,
                2 => COLOR_WARNING,
                3 or 4 => COLOR_DANGER,
                _ => COLOR_BLUE_NEUTRAL
            };
        }
        
        /// <summary>
        /// Ottiene il colore Color per un tipo toast (compatibilità)
        /// </summary>
        public Color GetColor(ToastNotificationType type)
        {
            return GetColor32(type);
        }
        
        /// <summary>
        /// Ottiene l'icona di severità per un tipo toast
        /// </summary>
        public Sprite GetSeverityIcon(ToastNotificationType type)
        {
            var setting = TypeSettings?.FirstOrDefault(s => s.Type == type);
            if (setting != null && setting.SeverityIcon != null)
                return setting.SeverityIcon;
            
            // Fallback basato su severità
            int severity = type.GetSeverity();
            return severity switch
            {
                0 or 1 => InfoIcon,
                2 => WarningIcon,
                3 or 4 => DangerIcon,
                _ => InfoIcon
            };
        }
        
        /// <summary>
        /// Ottiene il prefisso codice per un tipo toast
        /// </summary>
        public string GetCodePrefix(ToastNotificationType type)
        {
            var setting = TypeSettings?.FirstOrDefault(s => s.Type == type);
            if (setting != null && !string.IsNullOrEmpty(setting.CodePrefix))
                return setting.CodePrefix;
            
            // Fallback: usa abbreviazione tipo
            return type.ToString().Substring(0, Mathf.Min(3, type.ToString().Length)).ToUpper() + "-";
        }
        
        /// <summary>
        /// Ottiene la durata default per un tipo toast
        /// </summary>
        public float GetDuration(ToastNotificationType type)
        {
            var setting = TypeSettings?.FirstOrDefault(s => s.Type == type);
            if (setting != null && setting.DefaultDuration > 0)
                return setting.DefaultDuration;
            
            return DefaultDuration;
        }
    }
}

